using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WorkStatusLight
{

    internal static class PushPlusNotifier
    {
        private const string SendUrl = "https://www.pushplus.plus/send";

        public static void SendAsync(string token, string state, string title, string body, bool showResult, Form owner)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | (SecurityProtocolType)3072;
                    var serializer = new JavaScriptSerializer();
                    string payload = serializer.Serialize(new PushPlusRequest
                    {
                        token = (token ?? String.Empty).Trim(),
                        title = String.IsNullOrWhiteSpace(title) ? T.AppTitle : title,
                        content = body ?? String.Empty
                    });

                    string response;
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        response = client.UploadString(SendUrl, "POST", payload);
                    }

                    PushPlusResponse result = serializer.Deserialize<PushPlusResponse>(response);
                    if (result == null || result.code != 200)
                    {
                        string message = result == null ? "empty response" : ("code=" + result.code + " msg=" + (result.msg ?? String.Empty));
                        throw new InvalidOperationException(message);
                    }

                    Logger.Write("PushPlus sent state=" + state + " token=" + MaskSecret(token) + " elapsedMs=" + stopwatch.ElapsedMilliseconds);
                    if (showResult)
                    {
                        ShowResult(owner, T.PushPlusTestSent, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write("PushPlus send failed state=" + state + " token=" + MaskSecret(token) + " elapsedMs=" + stopwatch.ElapsedMilliseconds + " error=" + ex.Message);
                    if (showResult)
                    {
                        ShowResult(owner, T.PushPlusSendFailed, MessageBoxIcon.Warning);
                    }
                }
            });
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

        private sealed class PushPlusRequest
        {
            public string token { get; set; }
            public string title { get; set; }
            public string content { get; set; }
        }

        private sealed class PushPlusResponse
        {
            public int code { get; set; }
            public string msg { get; set; }
        }
    }
}
