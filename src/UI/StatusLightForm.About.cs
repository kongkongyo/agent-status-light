using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {
        private void BeginUpdateCheck()
        {
            if (updateCheckStarted)
            {
                return;
            }

            updateCheckStarted = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    Logger.Write("Checking latest release from " + AppInfo.LatestReleaseApiUrl);
                    UpdateCheckResult result = UpdateChecker.CheckLatestRelease();
                    if (IsDisposed)
                    {
                        return;
                    }

                    BeginInvoke(new MethodInvoker(delegate
                    {
                        ApplyUpdateCheckResult(result);
                    }));
                }
                catch (Exception ex)
                {
                    Logger.Write("Update check failed: " + ex.Message);
                }
            });
        }

        private void ApplyUpdateCheckResult(UpdateCheckResult result)
        {
            latestAvailableVersion = result.LatestVersion;
            updateAvailable = result.HasUpdate;
            aboutItem.Image = updateAvailable ? GetUpdateAvailableImage() : null;
            aboutItem.ToolTipText = updateAvailable ? String.Format(T.UpdateAvailableTooltipFormat, latestAvailableVersion) : String.Empty;
            Logger.Write("Update check completed hasUpdate=" + updateAvailable + " latestVersion=" + (String.IsNullOrWhiteSpace(latestAvailableVersion) ? "unknown" : latestAvailableVersion));
        }

        private Image GetUpdateAvailableImage()
        {
            if (updateAvailableImage != null)
            {
                return updateAvailableImage;
            }

            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (GraphicsPath path = new GraphicsPath())
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(32, 195, 106)))
            using (Pen border = new Pen(Color.FromArgb(22, 145, 78)))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                path.AddEllipse(4, 4, 8, 8);
                graphics.FillPath(brush, path);
                graphics.DrawPath(border, path);
            }

            updateAvailableImage = bitmap;
            return updateAvailableImage;
        }

        private void ShowAboutDialog()
        {
            using (var dialog = new Form())
            using (var titleLabel = new Label())
            using (var descriptionLabel = new Label())
            using (var updateLabel = new Label())
            using (var repositoryLabel = new Label())
            using (var repositoryLink = new LinkLabel())
            using (var closeButton = new Button())
            {
                const int margin = 18;
                const int dialogWidth = 480;
                const int contentWidth = dialogWidth - margin * 2;
                const int closeButtonWidth = 78;
                const int closeButtonHeight = 26;
                int y = margin;

                dialog.Text = T.AboutSoftwareTitle;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;

                titleLabel.Text = T.AppTitle + "  " + T.VersionLabel + ": " + AppInfo.Version;
                titleLabel.Font = new Font(dialog.Font, FontStyle.Bold);
                titleLabel.Location = new Point(margin, y);
                titleLabel.Size = new Size(contentWidth, 24);
                y += 34;

                descriptionLabel.Text = T.AboutSoftwareDescription;
                descriptionLabel.Location = new Point(margin, y);
                descriptionLabel.Size = new Size(contentWidth, 34);
                y += 44;

                if (updateAvailable)
                {
                    updateLabel.Text = String.Format(T.UpdateAvailableFormat, latestAvailableVersion);
                    updateLabel.ForeColor = Color.FromArgb(22, 145, 78);
                    updateLabel.Location = new Point(margin, y);
                    updateLabel.Size = new Size(contentWidth, 22);
                    y += 30;
                }

                repositoryLabel.Text = T.ProjectAddressLabel + ":";
                repositoryLabel.Location = new Point(margin, y);
                repositoryLabel.Size = new Size(contentWidth, 22);
                y += 24;

                repositoryLink.Text = FormatRepositoryUrlForDisplay(AppInfo.RepositoryUrl);
                repositoryLink.Location = new Point(margin, y);
                repositoryLink.Size = new Size(contentWidth, 22);
                repositoryLink.LinkArea = new LinkArea(0, repositoryLink.Text.Length);
                repositoryLink.LinkClicked += delegate { OpenRepositoryLink(); };
                y += 38;

                closeButton.Text = T.Close;
                closeButton.Location = new Point(dialogWidth - margin - closeButtonWidth, y);
                closeButton.Size = new Size(closeButtonWidth, closeButtonHeight);
                closeButton.DialogResult = DialogResult.OK;

                dialog.ClientSize = new Size(dialogWidth, y + closeButtonHeight + 12);
                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(descriptionLabel);
                if (updateAvailable)
                {
                    dialog.Controls.Add(updateLabel);
                }
                dialog.Controls.Add(repositoryLabel);
                dialog.Controls.Add(repositoryLink);
                dialog.Controls.Add(closeButton);
                dialog.AcceptButton = closeButton;
                dialog.CancelButton = closeButton;
                dialog.ShowDialog(this);
            }
        }

        private void OpenRepositoryLink()
        {
            try
            {
                Logger.Write("Opening repository link: " + AppInfo.RepositoryUrl);
                Process.Start(AppInfo.RepositoryUrl);
            }
            catch (Exception ex)
            {
                Logger.Write("Opening repository link failed: " + ex.Message);
                MessageBox.Show(this, AppInfo.RepositoryUrl, T.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string FormatRepositoryUrlForDisplay(string url)
        {
            const string GitHubPrefix = "https://github.com/";
            if (!String.IsNullOrWhiteSpace(url) &&
                url.StartsWith(GitHubPrefix, StringComparison.OrdinalIgnoreCase) &&
                url.Length > GitHubPrefix.Length)
            {
                return "github.com/" + url.Substring(GitHubPrefix.Length);
            }

            return url ?? String.Empty;
        }
    }
}
