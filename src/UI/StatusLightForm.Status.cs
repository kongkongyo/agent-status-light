using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {

        private void ReadStatus()
        {
            if (!File.Exists(statusPath))
            {
                WriteStatus("waiting", T.CodexWaiting, "auto");
                return;
            }

            try
            {
                var serializer = new JavaScriptSerializer();
                StatusPayload payload = serializer.Deserialize<StatusPayload>(File.ReadAllText(statusPath, Encoding.UTF8));
                StatusPayload normalized = StatusPayload.For(payload == null ? "waiting" : payload.state, payload == null ? null : payload.message);
                string source = payload == null ? "auto" : payload.source;
                if (!IsAutomaticSource(source) &&
                    (normalized.state != currentState || normalized.message != currentMessage))
                {
                    lastExternalStatusAt = DateTime.Now;
                }
                int previousDoneCount = currentDoneCount;
                int previousConfirmCount = currentConfirmCount;
                currentState = normalized.state;
                currentLabel = normalized.label;
                currentMessage = normalized.message;
                SetDisplayCountsFromPayload(payload, normalized.state);
                if (previousDoneCount == 0 && currentDoneCount > 0)
                {
                    StartDoneFlash();
                }
                NotifyStateIfNeeded(previousConfirmCount, previousDoneCount);
            }
            catch
            {
                currentState = "waiting";
                currentLabel = T.Waiting;
                currentMessage = T.CodexWaiting;
                SetDisplayCounts(0, 0, 0);
            }
        }


        private void WriteStatus(string state, string message, string source)
        {
            StatusPayload payload = StatusPayload.For(state, message);
            payload.source = source;
            WriteStatusPayload(payload);
        }


        private void WriteStatusCounts(int workingCount, int confirmCount, int doneCount, string message, string source)
        {
            workingCount = Math.Max(0, workingCount);
            confirmCount = Math.Max(0, confirmCount);
            doneCount = Math.Max(0, doneCount);

            StatusPayload payload = StatusPayload.For(DominantState(workingCount, confirmCount, doneCount), message);
            payload.source = source;
            payload.workingCount = workingCount;
            payload.confirmCount = confirmCount;
            payload.doneCount = doneCount;
            WriteStatusPayload(payload);
        }


        private void WriteStatusPayload(StatusPayload payload)
        {
            if (currentState == payload.state &&
                currentMessage == payload.message &&
                currentWorkingCount == payload.workingCount &&
                currentConfirmCount == payload.confirmCount &&
                currentDoneCount == payload.doneCount)
            {
                return;
            }

            int previousDoneCount = currentDoneCount;
            int previousConfirmCount = currentConfirmCount;
            currentState = payload.state;
            currentLabel = payload.label;
            currentMessage = payload.message;
            SetDisplayCounts(payload.workingCount, payload.confirmCount, payload.doneCount);

            if (previousDoneCount == 0 && currentDoneCount > 0)
            {
                StartDoneFlash();
            }

            NotifyStateIfNeeded(previousConfirmCount, previousDoneCount);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(statusPath));
                var serializer = new JavaScriptSerializer();
                File.WriteAllText(statusPath, serializer.Serialize(payload), Encoding.UTF8);
            }
            catch
            {
            }
        }


        private void NotifyStateIfNeeded(int previousConfirmCount, int previousDoneCount)
        {
            if (!notificationsReady)
            {
                return;
            }

            if (currentConfirmCount > previousConfirmCount)
            {
                PlayNotificationSound("confirm");
                SendSelectedStateNotification("confirm", currentConfirmCount);
            }

            if (currentDoneCount > previousDoneCount)
            {
                PlayNotificationSound("done");
                SendSelectedStateNotification("done", currentDoneCount);
            }
        }


        private void SendSelectedStateNotification(string state, int count)
        {
            if (!notificationsReady)
            {
                return;
            }

            string title = BuildConfiguredNotificationTitle(state, count);
            string body = BuildConfiguredNotificationBody(state, count, currentMessage);

            if (state == "confirm")
            {
                if (barkEnabled && !String.IsNullOrWhiteSpace(barkDeviceKey) && barkNotifyConfirm)
                {
                    SendBarkNotification("confirm", title, body, false);
                }
                if (pushPlusEnabled && !String.IsNullOrWhiteSpace(pushPlusToken) && pushPlusNotifyConfirm)
                {
                    SendPushPlusNotification("confirm", title, body, false);
                }
                if (telegramEnabled && !String.IsNullOrWhiteSpace(telegramBotToken) && !String.IsNullOrWhiteSpace(telegramChatId) && telegramNotifyConfirm)
                {
                    SendTelegramNotification("confirm", title, body, false);
                }
            }
            else if (state == "done")
            {
                if (barkEnabled && !String.IsNullOrWhiteSpace(barkDeviceKey) && barkNotifyDone)
                {
                    SendBarkNotification("done", title, body, false);
                }
                if (pushPlusEnabled && !String.IsNullOrWhiteSpace(pushPlusToken) && pushPlusNotifyDone)
                {
                    SendPushPlusNotification("done", title, body, false);
                }
                if (telegramEnabled && !String.IsNullOrWhiteSpace(telegramBotToken) && !String.IsNullOrWhiteSpace(telegramChatId) && telegramNotifyDone)
                {
                    SendTelegramNotification("done", title, body, false);
                }
            }
        }


        private string BuildConfiguredNotificationTitle(string state, int count)
        {
            string template = String.IsNullOrWhiteSpace(notificationTitleTemplate)
                ? T.NotificationDefaultTitleTemplate
                : notificationTitleTemplate;
            string title = ApplyNotificationTemplate(template, T.NotificationDefaultTitleTemplate, state, count, currentMessage);

            if (count > 1 &&
                (String.IsNullOrWhiteSpace(template) ||
                 template.IndexOf("{count}", StringComparison.OrdinalIgnoreCase) < 0))
            {
                return BuildNotificationTitle(title, count);
            }

            return title;
        }


        private string BuildConfiguredNotificationBody(string state, int count, string message)
        {
            string template = String.IsNullOrWhiteSpace(notificationBodyTemplate)
                ? T.NotificationDefaultBodyTemplate
                : notificationBodyTemplate;
            return ApplyNotificationTemplate(template, T.NotificationDefaultBodyTemplate, state, count, message);
        }


        private static string BuildNotificationTitle(string title, int count)
        {
            if (count <= 1) return title;
            return String.Format(CultureInfo.InvariantCulture, "{0} x{1}", title, count);
        }


        private static string ApplyNotificationTemplate(string template, string fallback, string state, int count, string message)
        {
            string value = String.IsNullOrWhiteSpace(template) ? fallback : template.Trim();
            if (String.IsNullOrWhiteSpace(value))
            {
                value = T.AppTitle;
            }

            return value
                .Replace("{message}", message ?? String.Empty)
                .Replace("{count}", count.ToString(CultureInfo.InvariantCulture))
                .Replace("{state}", NotificationStateLabel(state));
        }


        private static string NotificationStateLabel(string state)
        {
            if (String.Equals(state, "confirm", StringComparison.OrdinalIgnoreCase))
            {
                return T.NeedsConfirmationLabel;
            }

            if (String.Equals(state, "done", StringComparison.OrdinalIgnoreCase))
            {
                return T.Done;
            }

            return T.Working;
        }


        private void SendBarkNotification(string state, string title, string body, bool showResult)
        {
            if (String.IsNullOrWhiteSpace(barkDeviceKey))
            {
                if (showResult)
                {
                    MessageBox.Show(T.BarkKeyRequired, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            string serverUrl = NormalizeBarkServerUrl(barkServerUrl);
            string deviceKey = barkDeviceKey.Trim();
            string message = String.IsNullOrWhiteSpace(body) ? T.AppTitle : body;
            BarkNotifier.SendAsync(serverUrl, deviceKey, state, title, message, showResult, this);
        }


        private void SendPushPlusNotification(string state, string title, string body, bool showResult)
        {
            if (String.IsNullOrWhiteSpace(pushPlusToken))
            {
                if (showResult)
                {
                    MessageBox.Show(T.PushPlusTokenRequired, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            string token = pushPlusToken.Trim();
            string message = String.IsNullOrWhiteSpace(body) ? T.AppTitle : body;
            PushPlusNotifier.SendAsync(token, state, title, message, showResult, this);
        }


        private void SendTelegramNotification(string state, string title, string body, bool showResult)
        {
            if (String.IsNullOrWhiteSpace(telegramBotToken))
            {
                if (showResult)
                {
                    MessageBox.Show(T.TelegramTokenRequired, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            if (String.IsNullOrWhiteSpace(telegramChatId))
            {
                if (showResult)
                {
                    MessageBox.Show(T.TelegramChatIdRequired, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            string botToken = telegramBotToken.Trim();
            string chatId = telegramChatId.Trim();
            string proxyUrl = NormalizeTelegramProxyUrl(telegramProxyUrl);
            string message = String.IsNullOrWhiteSpace(body) ? T.AppTitle : body;
            TelegramNotifier.SendAsync(botToken, chatId, proxyUrl, state, title, message, showResult, this);
        }


        private static bool IsAutomaticSource(string source)
        {
            return String.Equals(source, "auto", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(source, "session", StringComparison.OrdinalIgnoreCase);
        }


        private static string DominantState(int workingCount, int confirmCount, int doneCount)
        {
            if (confirmCount > 0) return "confirm";
            if (workingCount > 0) return "working";
            if (doneCount > 0) return "done";
            return "waiting";
        }


        private static string BuildCountsMessage(int workingCount, int confirmCount, int doneCount)
        {
            if (workingCount <= 0 && confirmCount <= 0 && doneCount <= 0)
            {
                return T.CodexWaiting;
            }

            return String.Format(CultureInfo.InvariantCulture, T.CountsStatusFormat, workingCount, confirmCount, doneCount);
        }


        private void SetDisplayCountsFromPayload(StatusPayload payload, string normalizedState)
        {
            int workingCount = payload == null ? 0 : Math.Max(0, payload.workingCount);
            int confirmCount = payload == null ? 0 : Math.Max(0, payload.confirmCount);
            int doneCount = payload == null ? 0 : Math.Max(0, payload.doneCount);

            if (workingCount > 0 || confirmCount > 0 || doneCount > 0)
            {
                SetDisplayCounts(workingCount, confirmCount, doneCount);
                return;
            }

            SetDisplayCountsForState(normalizedState);
        }


        private void SetDisplayCountsForState(string state)
        {
            SetDisplayCounts(
                String.Equals(state, "working", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                String.Equals(state, "confirm", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                String.Equals(state, "done", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        }


        private void SetDisplayCounts(int workingCount, int confirmCount, int doneCount)
        {
            currentWorkingCount = Math.Max(0, workingCount);
            currentConfirmCount = Math.Max(0, confirmCount);
            currentDoneCount = Math.Max(0, doneCount);
        }


        private void StartDoneFlash()
        {
            doneFlashTicksRemaining = 10;
            doneFlashVisible = true;
            flashTimer.Stop();
            flashTimer.Start();
            RenderLayeredWindow();
        }
    }
}
