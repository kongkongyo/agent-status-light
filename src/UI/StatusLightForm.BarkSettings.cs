using System;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {

        private static bool TryReadBarkDialogInput(Form owner, TextBox serverBox, TextBox keyBox, bool requireKey, out string serverUrl, out string deviceKey)
        {
            serverUrl = "https://api.day.app";
            deviceKey = String.Empty;

            string keyValue = keyBox.Text.Trim();
            if (Uri.IsWellFormedUriString(keyValue, UriKind.Absolute))
            {
                if (!TryParseBarkConfig(keyValue, out serverUrl, out deviceKey))
                {
                    MessageBox.Show(owner, T.BarkInvalidConfig, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            else
            {
                if (!TryNormalizeBarkServerUrl(serverBox.Text, out serverUrl))
                {
                    MessageBox.Show(owner, T.BarkInvalidConfig, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                deviceKey = keyValue;
                if (!String.IsNullOrWhiteSpace(deviceKey) && !IsValidBarkDeviceKey(deviceKey))
                {
                    MessageBox.Show(owner, T.BarkInvalidConfig, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            if (requireKey && String.IsNullOrWhiteSpace(deviceKey))
            {
                MessageBox.Show(owner, T.BarkKeyRequired, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }


        private static bool TryParseBarkConfig(string value, out string serverUrl, out string deviceKey)
        {
            serverUrl = "https://api.day.app";
            deviceKey = String.Empty;
            value = (value ?? String.Empty).Trim();
            if (String.IsNullOrWhiteSpace(value)) return false;

            Uri uri;
            if (Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                (String.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                string[] segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0) return false;

                deviceKey = segments[0].Trim();
                serverUrl = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                return IsValidBarkDeviceKey(deviceKey);
            }

            deviceKey = value;
            return IsValidBarkDeviceKey(deviceKey);
        }


        private static string NormalizeBarkServerUrl(string value)
        {
            string normalized;
            return TryNormalizeBarkServerUrl(value, out normalized) ? normalized : "https://api.day.app";
        }


        private static bool TryNormalizeBarkServerUrl(string value, out string serverUrl)
        {
            serverUrl = "https://api.day.app";
            value = (value ?? String.Empty).Trim().TrimEnd('/');
            if (String.IsNullOrWhiteSpace(value)) return true;

            Uri uri;
            if (Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                (String.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                serverUrl = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                return true;
            }

            return false;
        }


        private static bool IsValidBarkDeviceKey(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return false;

            foreach (char c in value)
            {
                if (!(Char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }


        private static string MaskSecret(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "(empty)";
            value = value.Trim();
            if (value.Length <= 6) return "***";
            return value.Substring(0, 3) + "***" + value.Substring(value.Length - 3);
        }
    }
}
