using System;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {

        private static bool TryReadPushPlusDialogInput(Form owner, TextBox tokenBox, bool requireToken, out string token)
        {
            token = (tokenBox.Text ?? String.Empty).Trim();
            if (!String.IsNullOrWhiteSpace(token) && !IsValidPushPlusToken(token))
            {
                MessageBox.Show(owner, T.PushPlusInvalidConfig, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (requireToken && String.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show(owner, T.PushPlusTokenRequired, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }


        private static bool IsValidPushPlusToken(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return false;

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
