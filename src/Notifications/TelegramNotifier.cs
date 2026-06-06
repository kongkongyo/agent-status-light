using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WorkStatusLight
{

    internal static class TelegramNotifier
    {
        private const string SendUrlFormat = "https://api.telegram.org/bot{0}/sendMessage";

        public static void SendAsync(string botToken, string chatId, string proxyUrl, string state, string title, string body, bool showResult, Form owner)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | (SecurityProtocolType)3072;
                    string token = (botToken ?? String.Empty).Trim();
                    string chat = (chatId ?? String.Empty).Trim();
                    string message = BuildMessage(title, body);
                    string payload = "chat_id=" + Uri.EscapeDataString(chat) +
                        "&text=" + Uri.EscapeDataString(message);

                    string response;
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        if (!String.IsNullOrWhiteSpace(proxyUrl))
                        {
                            client.Proxy = new WebProxy(proxyUrl);
                        }

                        response = client.UploadString(String.Format(SendUrlFormat, token), "POST", payload);
                    }

                    var serializer = new JavaScriptSerializer();
                    TelegramResponse result = serializer.Deserialize<TelegramResponse>(response);
                    if (result == null || !result.ok)
                    {
                        string messageText = result == null ? "empty response" : ("description=" + (result.description ?? String.Empty));
                        throw new InvalidOperationException(messageText);
                    }

                    Logger.Write("Telegram sent state=" + state + " token=" + MaskSecret(token) + " chatId=" + MaskSecret(chat) + " proxy=" + (String.IsNullOrWhiteSpace(proxyUrl) ? "none" : "set") + " elapsedMs=" + stopwatch.ElapsedMilliseconds);
                    if (showResult)
                    {
                        ShowResult(owner, T.TelegramTestSent, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write("Telegram send failed state=" + state + " token=" + MaskSecret(botToken) + " chatId=" + MaskSecret(chatId) + " proxy=" + (String.IsNullOrWhiteSpace(proxyUrl) ? "none" : "set") + " elapsedMs=" + stopwatch.ElapsedMilliseconds + " error=" + ex.Message);
                    if (showResult)
                    {
                        ShowResult(owner, T.TelegramSendFailed, MessageBoxIcon.Warning);
                    }
                }
            });
        }

        private static string BuildMessage(string title, string body)
        {
            string safeTitle = String.IsNullOrWhiteSpace(title) ? T.AppTitle : title.Trim();
            string safeBody = body ?? String.Empty;
            if (String.IsNullOrWhiteSpace(safeBody))
            {
                return safeTitle;
            }

            return safeTitle + Environment.NewLine + safeBody;
        }

        private static string MaskSecret(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "(empty)";
            value = value.Trim();
            if (value.Length <= 6) return "***";
            return value.Substring(0, 3) + "***" + value.Substring(value.Length - 3);
        }

        private static void ShowResult(Form owner, string message, MessageBoxIcon icon)
        {
            if (owner == null || owner.IsDisposed) return;

            try
            {
                owner.BeginInvoke((MethodInvoker)delegate
                {
                    MessageBox.Show(owner, message, T.AppTitle, MessageBoxButtons.OK, icon);
                });
            }
            catch
            {
            }
        }

        private sealed class TelegramResponse
        {
            public bool ok { get; set; }
            public string description { get; set; }
        }
    }
}
