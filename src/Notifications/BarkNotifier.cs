using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WorkStatusLight
{

    internal static class BarkNotifier
    {
        public static void SendAsync(string serverUrl, string deviceKey, string state, string title, string body, bool showResult, Form owner)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | (SecurityProtocolType)3072;
                    string url = BuildUrl(serverUrl, deviceKey, title, body);
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        client.DownloadString(url);
                    }

                    Logger.Write("Bark sent state=" + state + " server=" + serverUrl + " key=" + MaskSecret(deviceKey) + " elapsedMs=" + stopwatch.ElapsedMilliseconds);
                    if (showResult)
                    {
                        ShowResult(owner, T.BarkTestSent, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write("Bark send failed state=" + state + " server=" + serverUrl + " key=" + MaskSecret(deviceKey) + " elapsedMs=" + stopwatch.ElapsedMilliseconds + " error=" + ex.Message);
                    if (showResult)
                    {
                        ShowResult(owner, T.BarkSendFailed, MessageBoxIcon.Warning);
                    }
                }
            });
        }

        private static string BuildUrl(string serverUrl, string deviceKey, string title, string body)
        {
            return serverUrl.TrimEnd('/') +
                "/" + Uri.EscapeDataString((deviceKey ?? String.Empty).Trim()) +
                "/" + Uri.EscapeDataString(String.IsNullOrWhiteSpace(title) ? T.AppTitle : title) +
                "/" + Uri.EscapeDataString(body ?? String.Empty) +
                "?group=" + Uri.EscapeDataString(T.AppTitle);
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
    }
}
