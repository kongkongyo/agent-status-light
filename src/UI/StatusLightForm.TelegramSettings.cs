using System;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {

        private static bool TryReadTelegramDialogInput(Form owner, TextBox tokenBox, TextBox chatIdBox, TextBox proxyBox, bool requireConfig, out string botToken, out string chatId, out string proxyUrl)
        {
            botToken = (tokenBox.Text ?? String.Empty).Trim();
            chatId = (chatIdBox.Text ?? String.Empty).Trim();
            proxyUrl = String.Empty;

            if (!String.IsNullOrWhiteSpace(botToken) && !IsValidTelegramBotToken(botToken))
            {
                MessageBox.Show(owner, T.TelegramInvalidConfig, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!String.IsNullOrWhiteSpace(chatId) && !IsValidTelegramChatId(chatId))
            {
                MessageBox.Show(owner, T.TelegramInvalidConfig, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryNormalizeTelegramProxyUrl(proxyBox.Text, out proxyUrl))
            {
                MessageBox.Show(owner, T.TelegramInvalidConfig, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (requireConfig && String.IsNullOrWhiteSpace(botToken))
            {
                MessageBox.Show(owner, T.TelegramTokenRequired, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (requireConfig && String.IsNullOrWhiteSpace(chatId))
            {
                MessageBox.Show(owner, T.TelegramChatIdRequired, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }


        private static bool TryNormalizeTelegramProxyUrl(string value, out string proxyUrl)
        {
            proxyUrl = String.Empty;
            value = (value ?? String.Empty).Trim().TrimEnd('/');
            if (String.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (value.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                value = "http://" + value;
            }

            Uri uri;
            if (Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                !String.IsNullOrWhiteSpace(uri.Host) &&
                (String.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                proxyUrl = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                return true;
            }

            return false;
        }


        private static bool IsValidTelegramBotToken(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.IndexOf(':') <= 0)
            {
                return false;
            }

            foreach (char c in value)
            {
                if (Char.IsWhiteSpace(c))
                {
                    return false;
                }
            }

            return true;
        }


        private static bool IsValidTelegramChatId(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (char c in value)
            {
                if (Char.IsWhiteSpace(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
