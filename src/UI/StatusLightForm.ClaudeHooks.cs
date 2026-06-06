using System;
using System.Drawing;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {
        private void ConfigureClaudeHooksForCurrentUser()
        {
            string executablePath = Application.ExecutablePath;
            string settingsPath = ClaudeHooksConfigurator.GetCurrentUserSettingsPath();
            if (!ShowClaudeStatusLightConfirmation(settingsPath))
            {
                return;
            }

            try
            {
                ClaudeHooksConfigurationResult result = ClaudeHooksConfigurator.ConfigureCurrentUser(executablePath);
                string message = BuildClaudeHooksConfigurationMessage(result);
                MessageBox.Show(this, message, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Write("Claude hooks configure failed: " + ex);
                MessageBox.Show(this, T.ClaudeHooksConfigureFailed + ex.Message, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RemoveClaudeHooksForCurrentUser()
        {
            string executablePath = Application.ExecutablePath;
            string settingsPath = ClaudeHooksConfigurator.GetCurrentUserSettingsPath();
            if (!ShowClaudeStatusLightRemoveConfirmation(settingsPath))
            {
                return;
            }

            try
            {
                ClaudeHooksConfigurationResult result = ClaudeHooksConfigurator.RemoveCurrentUser(executablePath);
                string message = BuildClaudeHooksRemoveMessage(result);
                MessageBox.Show(this, message, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Write("Claude hooks remove failed: " + ex);
                MessageBox.Show(this, T.ClaudeHooksRemoveFailed + ex.Message, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string BuildClaudeHooksConfigurationMessage(ClaudeHooksConfigurationResult result)
        {
            string title = result.Changed ? T.ClaudeHooksConfigured : T.ClaudeHooksAlreadyConfigured;
            string detail = result.Changed ? T.ClaudeStatusLightConfiguredDetail : T.ClaudeStatusLightReconfigureIfMoved;
            string backupPath = String.IsNullOrWhiteSpace(result.BackupPath) ? T.None : result.BackupPath;

            return title + Environment.NewLine +
                Environment.NewLine +
                detail + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeHooksSettingsPathLabel + Environment.NewLine +
                result.SettingsPath + Environment.NewLine +
                T.ClaudeHooksBackupPathLabel + Environment.NewLine +
                backupPath + Environment.NewLine + Environment.NewLine +
                T.ClaudeHooksRestartHint;
        }

        private static string BuildClaudeHooksRemoveMessage(ClaudeHooksConfigurationResult result)
        {
            if (!result.Changed)
            {
                return T.ClaudeHooksRemoveNotFound + Environment.NewLine +
                    Environment.NewLine +
                    T.ClaudeHooksSettingsPathLabel + Environment.NewLine +
                    result.SettingsPath;
            }

            return T.ClaudeHooksRemoved + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeHooksRemovedDetail + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeHooksSettingsPathLabel + Environment.NewLine +
                result.SettingsPath + Environment.NewLine +
                T.ClaudeHooksBackupPathLabel + Environment.NewLine +
                result.BackupPath + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeHooksRestartHint;
        }

        private bool ShowClaudeStatusLightConfirmation(string settingsPath)
        {
            using (var dialog = new Form())
            using (var messageBox = new TextBox())
            using (var enableButton = new Button())
            using (var cancelButton = new Button())
            {
                dialog.Text = T.ClaudeStatusLightTitle;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(ScaleDimension(560), ScaleDimension(430));
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;

                messageBox.Multiline = true;
                messageBox.ReadOnly = true;
                messageBox.BorderStyle = BorderStyle.None;
                messageBox.BackColor = SystemColors.Control;
                messageBox.ScrollBars = ScrollBars.Vertical;
                messageBox.Location = new Point(ScaleDimension(16), ScaleDimension(16));
                messageBox.Size = new Size(ScaleDimension(528), ScaleDimension(350));
                messageBox.Text = BuildClaudeStatusLightConfirmationText(settingsPath);

                enableButton.Text = T.Enable;
                enableButton.Location = new Point(ScaleDimension(376), ScaleDimension(386));
                enableButton.Size = new Size(ScaleDimension(80), ScaleDimension(28));
                enableButton.DialogResult = DialogResult.OK;

                cancelButton.Text = T.Cancel;
                cancelButton.Location = new Point(ScaleDimension(464), ScaleDimension(386));
                cancelButton.Size = new Size(ScaleDimension(80), ScaleDimension(28));
                cancelButton.DialogResult = DialogResult.Cancel;

                dialog.Controls.Add(messageBox);
                dialog.Controls.Add(enableButton);
                dialog.Controls.Add(cancelButton);
                dialog.AcceptButton = enableButton;
                dialog.CancelButton = cancelButton;

                return dialog.ShowDialog(this) == DialogResult.OK;
            }
        }

        private bool ShowClaudeStatusLightRemoveConfirmation(string settingsPath)
        {
            using (var dialog = new Form())
            using (var messageBox = new TextBox())
            using (var removeButton = new Button())
            using (var cancelButton = new Button())
            {
                dialog.Text = T.ClaudeStatusLightRemoveTitle;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(ScaleDimension(560), ScaleDimension(360));
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;

                messageBox.Multiline = true;
                messageBox.ReadOnly = true;
                messageBox.BorderStyle = BorderStyle.None;
                messageBox.BackColor = SystemColors.Control;
                messageBox.ScrollBars = ScrollBars.Vertical;
                messageBox.Location = new Point(ScaleDimension(16), ScaleDimension(16));
                messageBox.Size = new Size(ScaleDimension(528), ScaleDimension(280));
                messageBox.Text = BuildClaudeStatusLightRemoveText(settingsPath);

                removeButton.Text = T.Remove;
                removeButton.Location = new Point(ScaleDimension(376), ScaleDimension(316));
                removeButton.Size = new Size(ScaleDimension(80), ScaleDimension(28));
                removeButton.DialogResult = DialogResult.OK;

                cancelButton.Text = T.Cancel;
                cancelButton.Location = new Point(ScaleDimension(464), ScaleDimension(316));
                cancelButton.Size = new Size(ScaleDimension(80), ScaleDimension(28));
                cancelButton.DialogResult = DialogResult.Cancel;

                dialog.Controls.Add(messageBox);
                dialog.Controls.Add(removeButton);
                dialog.Controls.Add(cancelButton);
                dialog.AcceptButton = removeButton;
                dialog.CancelButton = cancelButton;

                return dialog.ShowDialog(this) == DialogResult.OK;
            }
        }

        private static string BuildClaudeStatusLightConfirmationText(string settingsPath)
        {
            return T.ClaudeStatusLightWhy + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightWritesConfig + Environment.NewLine +
                settingsPath + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightScopeLabel + Environment.NewLine +
                T.ClaudeStatusLightAllProjects + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightWriteContentLabel + Environment.NewLine +
                T.ClaudeStatusLightReceiver + Environment.NewLine +
                "AgentStatusLight.exe --claude-hook" + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightSafetyLabel + Environment.NewLine +
                T.ClaudeStatusLightNoSecrets + Environment.NewLine +
                T.ClaudeStatusLightNoOverwrite + Environment.NewLine +
                T.ClaudeStatusLightBackupBeforeWrite + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightEnableQuestion;
        }

        private static string BuildClaudeStatusLightRemoveText(string settingsPath)
        {
            return T.ClaudeStatusLightRemoveIntro + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightRemoveAfter + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightWritesConfig + Environment.NewLine +
                settingsPath + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightSafetyLabel + Environment.NewLine +
                T.ClaudeStatusLightRemoveNoSecrets + Environment.NewLine +
                T.ClaudeStatusLightRemoveKeepOthers + Environment.NewLine +
                T.ClaudeStatusLightBackupBeforeRemove + Environment.NewLine +
                Environment.NewLine +
                T.ClaudeStatusLightRemoveQuestion;
        }
    }
}
