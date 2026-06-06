using System;
using System.Drawing;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {
        private void ShowNotificationSettingsDialog()
        {
            using (var dialog = new Form())
            using (var tabs = new TabControl())
            using (var barkPage = new TabPage("Bark"))
            using (var pushPlusPage = new TabPage("PushPlus"))
            using (var telegramPage = new TabPage("Telegram"))
            using (var templatePage = new TabPage(T.NotificationTemplateTab))
            using (var barkEnabledBox = new CheckBox())
            using (var barkServerLabel = new Label())
            using (var barkKeyLabel = new Label())
            using (var barkServerBox = new TextBox())
            using (var barkKeyBox = new TextBox())
            using (var barkNotifyGroup = new GroupBox())
            using (var barkConfirmBox = new CheckBox())
            using (var barkDoneBox = new CheckBox())
            using (var barkTestButton = new Button())
            using (var pushPlusEnabledBox = new CheckBox())
            using (var pushPlusTokenLabel = new Label())
            using (var pushPlusTokenBox = new TextBox())
            using (var pushPlusNotifyGroup = new GroupBox())
            using (var pushPlusConfirmBox = new CheckBox())
            using (var pushPlusDoneBox = new CheckBox())
            using (var pushPlusTestButton = new Button())
            using (var telegramEnabledBox = new CheckBox())
            using (var telegramTokenLabel = new Label())
            using (var telegramChatIdLabel = new Label())
            using (var telegramProxyLabel = new Label())
            using (var telegramTokenBox = new TextBox())
            using (var telegramChatIdBox = new TextBox())
            using (var telegramProxyBox = new TextBox())
            using (var telegramNotifyGroup = new GroupBox())
            using (var telegramConfirmBox = new CheckBox())
            using (var telegramDoneBox = new CheckBox())
            using (var telegramTestButton = new Button())
            using (var titleLabel = new Label())
            using (var titleBox = new TextBox())
            using (var bodyLabel = new Label())
            using (var bodyBox = new TextBox())
            using (var previewLabel = new Label())
            using (var previewBox = new TextBox())
            using (var templateHintLabel = new Label())
            using (var okButton = new Button())
            using (var cancelButton = new Button())
            {
                dialog.Text = T.NotificationSettingsTitle;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(496, 404);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;

                tabs.Location = new Point(12, 12);
                tabs.Size = new Size(468, 342);
                barkPage.Text = "Bark";
                pushPlusPage.Text = "PushPlus";
                telegramPage.Text = "Telegram";
                templatePage.Text = T.NotificationTemplateTab;

                barkEnabledBox.Text = T.BarkEnabled;
                barkEnabledBox.Checked = barkEnabled;
                barkEnabledBox.Location = new Point(16, 16);
                barkEnabledBox.Size = new Size(160, 24);

                barkServerLabel.Text = T.BarkServerLabel;
                barkServerLabel.AutoSize = true;
                barkServerLabel.Location = new Point(16, 54);

                barkServerBox.Text = barkServerUrl;
                barkServerBox.Location = new Point(124, 50);
                barkServerBox.Size = new Size(318, 23);

                barkKeyLabel.Text = T.BarkDeviceKeyLabel;
                barkKeyLabel.AutoSize = true;
                barkKeyLabel.Location = new Point(16, 88);

                barkKeyBox.Text = barkDeviceKey;
                barkKeyBox.Location = new Point(124, 84);
                barkKeyBox.Size = new Size(318, 23);

                barkNotifyGroup.Text = T.BarkNotifyStates;
                barkNotifyGroup.Location = new Point(16, 120);
                barkNotifyGroup.Size = new Size(426, 66);

                barkConfirmBox.Text = T.RedConfirm;
                barkConfirmBox.Checked = barkNotifyConfirm;
                ConfigureNotifyStateCheckBox(barkConfirmBox, 16);

                barkDoneBox.Text = T.GreenDone;
                barkDoneBox.Checked = barkNotifyDone;
                ConfigureNotifyStateCheckBox(barkDoneBox, 172);

                barkTestButton.Text = T.BarkTest;
                barkTestButton.Location = new Point(16, 216);
                barkTestButton.Size = new Size(118, 26);

                pushPlusEnabledBox.Text = T.PushPlusEnabled;
                pushPlusEnabledBox.Checked = pushPlusEnabled;
                pushPlusEnabledBox.Location = new Point(16, 16);
                pushPlusEnabledBox.Size = new Size(180, 24);

                pushPlusTokenLabel.Text = T.PushPlusTokenLabel;
                pushPlusTokenLabel.AutoSize = true;
                pushPlusTokenLabel.Location = new Point(16, 54);

                pushPlusTokenBox.Text = pushPlusToken;
                pushPlusTokenBox.Location = new Point(124, 50);
                pushPlusTokenBox.Size = new Size(318, 23);

                pushPlusNotifyGroup.Text = T.BarkNotifyStates;
                pushPlusNotifyGroup.Location = new Point(16, 88);
                pushPlusNotifyGroup.Size = new Size(426, 66);

                pushPlusConfirmBox.Text = T.RedConfirm;
                pushPlusConfirmBox.Checked = pushPlusNotifyConfirm;
                ConfigureNotifyStateCheckBox(pushPlusConfirmBox, 16);

                pushPlusDoneBox.Text = T.GreenDone;
                pushPlusDoneBox.Checked = pushPlusNotifyDone;
                ConfigureNotifyStateCheckBox(pushPlusDoneBox, 172);

                pushPlusTestButton.Text = T.PushPlusTest;
                pushPlusTestButton.Location = new Point(16, 216);
                pushPlusTestButton.Size = new Size(118, 26);

                telegramEnabledBox.Text = T.TelegramEnabled;
                telegramEnabledBox.Checked = telegramEnabled;
                telegramEnabledBox.Location = new Point(16, 16);
                telegramEnabledBox.Size = new Size(180, 24);

                telegramTokenLabel.Text = T.TelegramBotTokenLabel;
                telegramTokenLabel.AutoSize = true;
                telegramTokenLabel.Location = new Point(16, 54);

                telegramTokenBox.Text = telegramBotToken;
                telegramTokenBox.Location = new Point(124, 50);
                telegramTokenBox.Size = new Size(318, 23);

                telegramChatIdLabel.Text = T.TelegramChatIdLabel;
                telegramChatIdLabel.AutoSize = true;
                telegramChatIdLabel.Location = new Point(16, 88);

                telegramChatIdBox.Text = telegramChatId;
                telegramChatIdBox.Location = new Point(124, 84);
                telegramChatIdBox.Size = new Size(318, 23);

                telegramProxyLabel.Text = T.TelegramProxyLabel;
                telegramProxyLabel.AutoSize = true;
                telegramProxyLabel.Location = new Point(16, 122);

                telegramProxyBox.Text = telegramProxyUrl;
                telegramProxyBox.Location = new Point(124, 118);
                telegramProxyBox.Size = new Size(318, 23);

                telegramNotifyGroup.Text = T.BarkNotifyStates;
                telegramNotifyGroup.Location = new Point(16, 152);
                telegramNotifyGroup.Size = new Size(426, 66);

                telegramConfirmBox.Text = T.RedConfirm;
                telegramConfirmBox.Checked = telegramNotifyConfirm;
                ConfigureNotifyStateCheckBox(telegramConfirmBox, 16);

                telegramDoneBox.Text = T.GreenDone;
                telegramDoneBox.Checked = telegramNotifyDone;
                ConfigureNotifyStateCheckBox(telegramDoneBox, 172);

                telegramTestButton.Text = T.TelegramTest;
                telegramTestButton.Location = new Point(16, 244);
                telegramTestButton.Size = new Size(118, 26);

                ConfigureNotificationTemplateLabel(titleLabel, T.NotificationTitleTemplateLabel, 22);
                ConfigureNotificationTemplateTextBox(titleBox, notificationTitleTemplate, 18, false);

                ConfigureNotificationTemplateLabel(bodyLabel, T.NotificationBodyTemplateLabel, 58);
                ConfigureNotificationTemplateTextBox(bodyBox, notificationBodyTemplate, 54, true);

                previewLabel.Text = T.NotificationPreviewLabel;
                previewLabel.AutoSize = true;
                previewLabel.Location = new Point(16, 144);

                previewBox.Location = new Point(104, 140);
                previewBox.Size = new Size(338, 84);
                previewBox.Multiline = true;
                previewBox.ReadOnly = true;
                previewBox.ScrollBars = ScrollBars.Vertical;

                templateHintLabel.Text = T.NotificationTemplateHint;
                templateHintLabel.AutoSize = false;
                templateHintLabel.ForeColor = SystemColors.GrayText;
                templateHintLabel.Location = new Point(104, 236);
                templateHintLabel.Size = new Size(338, 70);

                EventHandler updatePreview = delegate
                {
                    UpdateNotificationTemplatePreview(titleBox, bodyBox, previewBox);
                };
                titleBox.TextChanged += updatePreview;
                bodyBox.TextChanged += updatePreview;
                UpdateNotificationTemplatePreview(titleBox, bodyBox, previewBox);

                okButton.Text = T.Save;
                okButton.Location = new Point(314, 368);
                okButton.Size = new Size(75, 26);

                cancelButton.Text = T.Cancel;
                cancelButton.Location = new Point(397, 368);
                cancelButton.Size = new Size(75, 26);
                cancelButton.DialogResult = DialogResult.Cancel;

                barkTestButton.Click += delegate
                {
                    string serverUrl;
                    string deviceKey;
                    if (!TryReadBarkDialogInput(dialog, barkServerBox, barkKeyBox, true, out serverUrl, out deviceKey))
                    {
                        tabs.SelectedTab = barkPage;
                        return;
                    }

                    BarkNotifier.SendAsync(serverUrl, deviceKey, "test", T.BarkTestTitle, T.BarkTestBody, true, this);
                };

                pushPlusTestButton.Click += delegate
                {
                    string token;
                    if (!TryReadPushPlusDialogInput(dialog, pushPlusTokenBox, true, out token))
                    {
                        tabs.SelectedTab = pushPlusPage;
                        return;
                    }

                    PushPlusNotifier.SendAsync(token, "test", T.PushPlusTestTitle, T.PushPlusTestBody, true, this);
                };

                telegramTestButton.Click += delegate
                {
                    string botToken;
                    string chatId;
                    string proxyUrl;
                    if (!TryReadTelegramDialogInput(dialog, telegramTokenBox, telegramChatIdBox, telegramProxyBox, true, out botToken, out chatId, out proxyUrl))
                    {
                        tabs.SelectedTab = telegramPage;
                        return;
                    }

                    TelegramNotifier.SendAsync(botToken, chatId, proxyUrl, "test", T.TelegramTestTitle, T.TelegramTestBody, true, this);
                };

                okButton.Click += delegate
                {
                    string serverUrl;
                    string deviceKey;
                    string token;
                    string botToken;
                    string chatId;
                    string proxyUrl;
                    if (!TryReadBarkDialogInput(dialog, barkServerBox, barkKeyBox, barkEnabledBox.Checked, out serverUrl, out deviceKey))
                    {
                        tabs.SelectedTab = barkPage;
                        return;
                    }
                    if (!TryReadPushPlusDialogInput(dialog, pushPlusTokenBox, pushPlusEnabledBox.Checked, out token))
                    {
                        tabs.SelectedTab = pushPlusPage;
                        return;
                    }
                    if (!TryReadTelegramDialogInput(dialog, telegramTokenBox, telegramChatIdBox, telegramProxyBox, telegramEnabledBox.Checked, out botToken, out chatId, out proxyUrl))
                    {
                        tabs.SelectedTab = telegramPage;
                        return;
                    }

                    barkServerUrl = serverUrl;
                    barkDeviceKey = deviceKey;
                    barkEnabled = barkEnabledBox.Checked && !String.IsNullOrWhiteSpace(barkDeviceKey);
                    barkNotifyConfirm = barkConfirmBox.Checked;
                    barkNotifyDone = barkDoneBox.Checked;
                    pushPlusToken = token;
                    pushPlusEnabled = pushPlusEnabledBox.Checked && !String.IsNullOrWhiteSpace(pushPlusToken);
                    pushPlusNotifyConfirm = pushPlusConfirmBox.Checked;
                    pushPlusNotifyDone = pushPlusDoneBox.Checked;
                    telegramBotToken = botToken;
                    telegramChatId = chatId;
                    telegramProxyUrl = proxyUrl;
                    telegramEnabled = telegramEnabledBox.Checked && !String.IsNullOrWhiteSpace(telegramBotToken) && !String.IsNullOrWhiteSpace(telegramChatId);
                    telegramNotifyConfirm = telegramConfirmBox.Checked;
                    telegramNotifyDone = telegramDoneBox.Checked;
                    notificationTitleTemplate = NormalizeNotificationTemplate(titleBox.Text, T.NotificationDefaultTitleTemplate);
                    notificationBodyTemplate = NormalizeNotificationTemplate(bodyBox.Text, T.NotificationDefaultBodyTemplate);
                    WriteSettings();
                    Logger.Write("Notification configured barkEnabled=" + barkEnabled + " barkKey=" + MaskSecret(barkDeviceKey) + " pushPlusEnabled=" + pushPlusEnabled + " pushPlusToken=" + MaskSecret(pushPlusToken) + " telegramEnabled=" + telegramEnabled + " telegramToken=" + MaskSecret(telegramBotToken) + " telegramChatId=" + MaskSecret(telegramChatId) + " telegramProxy=" + (String.IsNullOrWhiteSpace(telegramProxyUrl) ? "none" : "set"));
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };

                barkNotifyGroup.Controls.Add(barkConfirmBox);
                barkNotifyGroup.Controls.Add(barkDoneBox);
                barkPage.Controls.Add(barkEnabledBox);
                barkPage.Controls.Add(barkServerLabel);
                barkPage.Controls.Add(barkServerBox);
                barkPage.Controls.Add(barkKeyLabel);
                barkPage.Controls.Add(barkKeyBox);
                barkPage.Controls.Add(barkNotifyGroup);
                barkPage.Controls.Add(barkTestButton);

                pushPlusNotifyGroup.Controls.Add(pushPlusConfirmBox);
                pushPlusNotifyGroup.Controls.Add(pushPlusDoneBox);
                pushPlusPage.Controls.Add(pushPlusEnabledBox);
                pushPlusPage.Controls.Add(pushPlusTokenLabel);
                pushPlusPage.Controls.Add(pushPlusTokenBox);
                pushPlusPage.Controls.Add(pushPlusNotifyGroup);
                pushPlusPage.Controls.Add(pushPlusTestButton);

                telegramNotifyGroup.Controls.Add(telegramConfirmBox);
                telegramNotifyGroup.Controls.Add(telegramDoneBox);
                telegramPage.Controls.Add(telegramEnabledBox);
                telegramPage.Controls.Add(telegramTokenLabel);
                telegramPage.Controls.Add(telegramTokenBox);
                telegramPage.Controls.Add(telegramChatIdLabel);
                telegramPage.Controls.Add(telegramChatIdBox);
                telegramPage.Controls.Add(telegramProxyLabel);
                telegramPage.Controls.Add(telegramProxyBox);
                telegramPage.Controls.Add(telegramNotifyGroup);
                telegramPage.Controls.Add(telegramTestButton);

                templatePage.Controls.Add(titleLabel);
                templatePage.Controls.Add(titleBox);
                templatePage.Controls.Add(bodyLabel);
                templatePage.Controls.Add(bodyBox);
                templatePage.Controls.Add(previewLabel);
                templatePage.Controls.Add(previewBox);
                templatePage.Controls.Add(templateHintLabel);

                tabs.TabPages.Add(barkPage);
                tabs.TabPages.Add(pushPlusPage);
                tabs.TabPages.Add(telegramPage);
                tabs.TabPages.Add(templatePage);
                dialog.Controls.Add(tabs);
                dialog.Controls.Add(okButton);
                dialog.Controls.Add(cancelButton);
                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;
                dialog.ShowDialog(this);
            }
        }


        private static void ConfigureNotifyStateCheckBox(CheckBox checkBox, int x)
        {
            checkBox.Location = new Point(x, 24);
            checkBox.Size = new Size(148, 28);
            checkBox.TextAlign = ContentAlignment.MiddleLeft;
            checkBox.CheckAlign = ContentAlignment.MiddleLeft;
            checkBox.AutoEllipsis = false;
        }


        private static void ConfigureNotificationTemplateLabel(Label label, string text, int y)
        {
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(16, y);
        }


        private static void ConfigureNotificationTemplateTextBox(TextBox textBox, string text, int y, bool multiline)
        {
            textBox.Text = text;
            textBox.Location = new Point(104, y);
            textBox.Size = multiline ? new Size(338, 64) : new Size(338, 23);
            textBox.Multiline = multiline;
            textBox.ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None;
        }


        private static void UpdateNotificationTemplatePreview(TextBox titleBox, TextBox bodyBox, TextBox previewBox)
        {
            string confirmTitle = ApplyNotificationTemplate(titleBox.Text, T.NotificationDefaultTitleTemplate, "confirm", 1, T.NeedsConfirmation);
            string confirmBody = ApplyNotificationTemplate(bodyBox.Text, T.NotificationDefaultBodyTemplate, "confirm", 1, T.NeedsConfirmation);
            string doneTitle = ApplyNotificationTemplate(titleBox.Text, T.NotificationDefaultTitleTemplate, "done", 1, T.WorkJustFinished);
            string doneBody = ApplyNotificationTemplate(bodyBox.Text, T.NotificationDefaultBodyTemplate, "done", 1, T.WorkJustFinished);

            previewBox.Text =
                T.RedConfirm + Environment.NewLine +
                T.NotificationTitleTemplateLabel + "\uff1a" + confirmTitle + Environment.NewLine +
                T.NotificationBodyTemplateLabel + "\uff1a" + confirmBody + Environment.NewLine +
                Environment.NewLine +
                T.GreenDone + Environment.NewLine +
                T.NotificationTitleTemplateLabel + "\uff1a" + doneTitle + Environment.NewLine +
                T.NotificationBodyTemplateLabel + "\uff1a" + doneBody;
        }
    }
}
