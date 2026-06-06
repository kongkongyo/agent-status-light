using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Threading;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {
        private void ShowSoundSettingsDialog()
        {
            using (var dialog = new Form())
            using (var enabledBox = new CheckBox())
            using (var formatHintLabel = new Label())
            using (var confirmLabel = new Label())
            using (var confirmBox = new TextBox())
            using (var confirmBrowseButton = new Button())
            using (var confirmDefaultButton = new Button())
            using (var confirmTestButton = new Button())
            using (var doneLabel = new Label())
            using (var doneBox = new TextBox())
            using (var doneBrowseButton = new Button())
            using (var doneDefaultButton = new Button())
            using (var doneTestButton = new Button())
            using (var okButton = new Button())
            using (var cancelButton = new Button())
            {
                dialog.Text = T.SoundSettingsTitle;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(500, 244);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;

                enabledBox.Text = T.SoundEnabled;
                enabledBox.Checked = soundEnabled;
                enabledBox.Location = new Point(16, 14);
                enabledBox.Size = new Size(180, 24);

                formatHintLabel.Text = T.SoundFormatHint;
                formatHintLabel.AutoSize = true;
                formatHintLabel.ForeColor = SystemColors.GrayText;
                formatHintLabel.Location = new Point(264, 18);

                ConfigureSoundRow(dialog, confirmLabel, confirmBox, confirmBrowseButton, confirmDefaultButton, confirmTestButton, T.ConfirmSoundFileLabel, confirmSoundFilePath, 50, "confirm");
                ConfigureSoundRow(dialog, doneLabel, doneBox, doneBrowseButton, doneDefaultButton, doneTestButton, T.DoneSoundFileLabel, doneSoundFilePath, 118, "done");

                okButton.Text = T.Save;
                okButton.Location = new Point(324, 204);
                okButton.Size = new Size(75, 26);

                cancelButton.Text = T.Cancel;
                cancelButton.Location = new Point(405, 204);
                cancelButton.Size = new Size(75, 26);
                cancelButton.DialogResult = DialogResult.Cancel;

                okButton.Click += delegate
                {
                    string confirmPath;
                    string donePath;
                    if (!TryReadSoundDialogInput(dialog, confirmBox, out confirmPath) ||
                        !TryReadSoundDialogInput(dialog, doneBox, out donePath))
                    {
                        return;
                    }

                    soundEnabled = enabledBox.Checked;
                    confirmSoundFilePath = confirmPath;
                    doneSoundFilePath = donePath;
                    WriteSettings();
                    Logger.Write("Sound configured enabled=" + soundEnabled + " confirmSource=" + SoundSourceName(confirmSoundFilePath) + " doneSource=" + SoundSourceName(doneSoundFilePath));
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };

                dialog.Controls.Add(enabledBox);
                dialog.Controls.Add(formatHintLabel);
                dialog.Controls.Add(confirmLabel);
                dialog.Controls.Add(confirmBox);
                dialog.Controls.Add(confirmBrowseButton);
                dialog.Controls.Add(confirmDefaultButton);
                dialog.Controls.Add(confirmTestButton);
                dialog.Controls.Add(doneLabel);
                dialog.Controls.Add(doneBox);
                dialog.Controls.Add(doneBrowseButton);
                dialog.Controls.Add(doneDefaultButton);
                dialog.Controls.Add(doneTestButton);
                dialog.Controls.Add(okButton);
                dialog.Controls.Add(cancelButton);
                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;
                dialog.ShowDialog(this);
            }
        }

        private static void ConfigureSoundRow(Form owner, Label label, TextBox fileBox, Button browseButton, Button defaultButton, Button testButton, string labelText, string currentPath, int y, string state)
        {
            label.Text = labelText;
            label.AutoSize = true;
            label.Location = new Point(16, y + 4);

            fileBox.Text = currentPath;
            fileBox.Location = new Point(96, y);
            fileBox.Size = new Size(288, 23);

            browseButton.Text = T.Browse;
            browseButton.Location = new Point(392, y - 1);
            browseButton.Size = new Size(88, 26);

            defaultButton.Text = T.UseDefaultSound;
            defaultButton.Location = new Point(96, y + 34);
            defaultButton.Size = new Size(96, 26);

            testButton.Text = T.TestSound;
            testButton.Location = new Point(200, y + 34);
            testButton.Size = new Size(75, 26);

            browseButton.Click += delegate
            {
                using (var openDialog = new OpenFileDialog())
                {
                    openDialog.Title = T.SoundSettingsTitle;
                    openDialog.Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*";
                    openDialog.CheckFileExists = true;
                    openDialog.Multiselect = false;
                    if (openDialog.ShowDialog(owner) == DialogResult.OK)
                    {
                        fileBox.Text = openDialog.FileName;
                    }
                }
            };

            defaultButton.Click += delegate
            {
                fileBox.Text = String.Empty;
            };

            testButton.Click += delegate
            {
                string selectedPath;
                if (TryReadSoundDialogInput(owner, fileBox, out selectedPath))
                {
                    PlayNotificationSound(selectedPath, true, state + "-test");
                }
            };
        }

        private static bool TryReadSoundDialogInput(Form owner, TextBox fileBox, out string soundPath)
        {
            soundPath = (fileBox.Text ?? String.Empty).Trim();
            if (String.IsNullOrWhiteSpace(soundPath))
            {
                return true;
            }

            if (!IsValidSoundFile(soundPath))
            {
                MessageBox.Show(owner, T.SoundInvalidFile, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void PlayNotificationSound(string state)
        {
            PlayNotificationSound(GetSoundFilePathForState(state), soundEnabled, state);
        }

        private string GetSoundFilePathForState(string state)
        {
            if (String.Equals(state, "confirm", StringComparison.OrdinalIgnoreCase))
            {
                return confirmSoundFilePath;
            }

            if (String.Equals(state, "done", StringComparison.OrdinalIgnoreCase))
            {
                return doneSoundFilePath;
            }

            return String.Empty;
        }

        private static void PlayNotificationSound(string customPath, bool enabled, string state)
        {
            if (!enabled)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                string source = "default";
                try
                {
                    string path = (customPath ?? String.Empty).Trim();
                    if (IsValidSoundFile(path))
                    {
                        source = "custom";
                        using (var player = new SoundPlayer(path))
                        {
                            player.PlaySync();
                        }
                    }
                    else
                    {
                        SystemSounds.Exclamation.Play();
                    }

                    Logger.Write("Sound played state=" + state + " source=" + source);
                }
                catch (Exception ex)
                {
                    Logger.Write("Sound play failed state=" + state + " source=" + source + " error=" + ex.Message);
                    try
                    {
                        SystemSounds.Exclamation.Play();
                    }
                    catch
                    {
                    }
                }
            });
        }

        private static bool IsValidSoundFile(string path)
        {
            return !String.IsNullOrWhiteSpace(path) &&
                File.Exists(path) &&
                String.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase);
        }

        private static string SoundSourceName(string path)
        {
            return String.IsNullOrWhiteSpace(path) ? "default" : "custom";
        }
    }
}
