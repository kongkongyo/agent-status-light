using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WorkStatusLight
{

    internal sealed partial class StatusLightForm : Form
    {
        private const float UiScale = 0.88f;
        private const string LightOrientationHorizontalValue = "horizontal";
        private const string LightOrientationVerticalValue = "vertical";
        private static readonly Size HorizontalLightWindowSize = new Size(ScaleDimension(167), ScaleDimension(96));
        private static readonly Size VerticalLightWindowSize = new Size(ScaleDimension(96), ScaleDimension(167));
        private const int StrongBusySamplesAfterDone = 3;
        private const int QuietSamplesBeforeDone = 5;
        private const int QuietBeforeDoneSeconds = 10;
        private const int DoneHoldSeconds = 25;
        private const int ExternalStatusHoldSeconds = 12;
        private const int ExternalForegroundHoldSeconds = 90;
        private const int BreathingTimerIntervalMilliseconds = 60;
        private const int BreathingCycleTicks = 36;
        private const float BreathingMinimumIntensity = 0.65f;
        private static readonly Color DefaultConfirmLightColor = Color.FromArgb(255, 82, 82);
        private static readonly Color DefaultWorkingLightColor = Color.FromArgb(255, 200, 69);
        private static readonly Color DefaultDoneLightColor = Color.FromArgb(32, 195, 106);

        private readonly string statusPath;
        private readonly string settingsPath;
        private readonly AgentSessionAggregator sessionAggregator;
        private readonly CodexActivityMonitor activityMonitor;
        private readonly ForegroundAppDetector foregroundAppDetector;
        private readonly System.Windows.Forms.Timer statusTimer;
        private readonly System.Windows.Forms.Timer topMostTimer;
        private readonly System.Windows.Forms.Timer flashTimer;
        private readonly System.Windows.Forms.Timer breathingTimer;
        private readonly ToolTip tooltip;
        private readonly ToolStripMenuItem restoreAutoItem;
        private readonly ToolStripMenuItem skinSystemItem;
        private readonly ToolStripMenuItem skinDarkItem;
        private readonly ToolStripMenuItem skinLightItem;
        private readonly ToolStripMenuItem skinTransparentItem;
        private readonly ToolStripMenuItem lightOrientationHorizontalItem;
        private readonly ToolStripMenuItem lightOrientationVerticalItem;
        private readonly ToolStripMenuItem breathingLightItem;
        private readonly ToolStripMenuItem aboutItem;

        private string currentState = "waiting";
        private string currentLabel = "Idle";
        private string currentMessage = "Agent is idle and waiting for a new task";
        private string currentSkin = "system";
        private string currentLightOrientation = LightOrientationHorizontalValue;
        private string barkServerUrl = "https://api.day.app";
        private string barkDeviceKey = String.Empty;
        private string pushPlusToken = String.Empty;
        private string telegramBotToken = String.Empty;
        private string telegramChatId = String.Empty;
        private string telegramProxyUrl = String.Empty;
        private string notificationTitleTemplate = T.NotificationDefaultTitleTemplate;
        private string notificationBodyTemplate = T.NotificationDefaultBodyTemplate;
        private string confirmSoundFilePath = String.Empty;
        private string doneSoundFilePath = String.Empty;
        private Color confirmLightColor = DefaultConfirmLightColor;
        private Color workingLightColor = DefaultWorkingLightColor;
        private Color doneLightColor = DefaultDoneLightColor;
        private Color? waitingLightColor;
        private int currentWorkingCount;
        private int currentConfirmCount;
        private int currentDoneCount;
        private bool barkEnabled;
        private bool barkNotifyConfirm;
        private bool barkNotifyDone = true;
        private bool windowsNativeEnabled;
        private bool windowsNativeNotifyConfirm;
        private bool windowsNativeNotifyDone = true;
        private bool pushPlusEnabled;
        private bool pushPlusNotifyConfirm;
        private bool pushPlusNotifyDone = true;
        private bool telegramEnabled;
        private bool telegramNotifyConfirm;
        private bool telegramNotifyDone = true;
        private bool soundEnabled;
        private bool breathingLightEnabled = true;
        private bool autoDetect;
        private bool dragging;
        private bool hasSeenBusy;
        private bool doneAnnounced;
        private bool doneFlashVisible = true;
        private bool notificationsReady;
        private Point dragStartMouse;
        private Point dragStartWindow;
        private DateTime lastBusyAt = DateTime.MinValue;
        private DateTime lastExternalForegroundAt = DateTime.MinValue;
        private DateTime doneAnnouncedAt = DateTime.MinValue;
        private DateTime lastExternalStatusAt = DateTime.MinValue;
        private DateTime lastSessionCompletionSeen = DateTime.MinValue;
        private Point savedWindowLocation;
        private int doneFlashTicksRemaining;
        private int breathingTick;
        private int quietSampleCount;
        private int strongBusyAfterDoneCount;
        private bool sessionTrackingInitialized;
        private bool hasSavedWindowLocation;
        private bool updateCheckStarted;
        private bool updateAvailable;
        private string latestAvailableVersion = String.Empty;
        private Image updateAvailableImage;

        public StatusLightForm(bool autoDetect)
        {
            this.autoDetect = autoDetect;
            statusPath = Path.Combine(Paths.AppDirectory, "data", "status.json");
            settingsPath = Path.Combine(Paths.AppDirectory, "data", "settings.json");
            sessionAggregator = new AgentSessionAggregator();
            activityMonitor = new CodexActivityMonitor();
            foregroundAppDetector = new ForegroundAppDetector();

            Text = T.AppTitle;
            ApplyApplicationIcon();
            ClientSize = HorizontalLightWindowSize;
            MinimumSize = new Size(1, 1);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Logger.Write("Form constructed");

            tooltip = new ToolTip();
            tooltip.SetToolTip(this, T.AppTitle);
            ReadSettings();
            ApplyLightWindowSize();
            Location = GetStartupLocation(ClientSize);

            var menu = new ContextMenuStrip();
            restoreAutoItem = new ToolStripMenuItem(T.RestoreAutoDetect);
            var skinItem = new ToolStripMenuItem(T.Skin);
            skinSystemItem = new ToolStripMenuItem(T.SkinSystem);
            skinDarkItem = new ToolStripMenuItem(T.SkinDark);
            skinLightItem = new ToolStripMenuItem(T.SkinLight);
            skinTransparentItem = new ToolStripMenuItem(T.SkinTransparent);
            var lightOrientationItem = new ToolStripMenuItem(T.LightOrientation);
            lightOrientationHorizontalItem = new ToolStripMenuItem(T.LightOrientationHorizontal);
            lightOrientationVerticalItem = new ToolStripMenuItem(T.LightOrientationVertical);
            var notificationSettingsItem = new ToolStripMenuItem(T.NotificationSettings);
            var colorSettingsItem = new ToolStripMenuItem(T.ColorSettings);
            breathingLightItem = new ToolStripMenuItem(T.BreathingLightEffect);
            var soundSettingsItem = new ToolStripMenuItem(T.SoundSettings);
            var claudeHooksItem = new ToolStripMenuItem(T.ClaudeHooks);
            var configureClaudeHooksItem = new ToolStripMenuItem(T.ConfigureClaudeHooks);
            var removeClaudeHooksItem = new ToolStripMenuItem(T.RemoveClaudeHooks);
            var workItem = new ToolStripMenuItem(T.YellowWorking);
            var confirmItem = new ToolStripMenuItem(T.RedConfirm);
            var doneItem = new ToolStripMenuItem(T.GreenDone);
            aboutItem = new ToolStripMenuItem(T.AboutSoftware);
            var exitItem = new ToolStripMenuItem(T.Exit);

            menu.Opening += delegate
            {
                UpdateRestoreAutoDetectMenu();
                UpdateBreathingLightMenuCheck();
            };
            restoreAutoItem.Click += delegate { RestoreAutoDetect(); };
            skinSystemItem.Click += delegate { SetSkin("system"); };
            skinDarkItem.Click += delegate { SetSkin("dark"); };
            skinLightItem.Click += delegate { SetSkin("light"); };
            skinTransparentItem.Click += delegate { SetSkin("transparent"); };
            lightOrientationHorizontalItem.Click += delegate { SetLightOrientation(LightOrientationHorizontalValue); };
            lightOrientationVerticalItem.Click += delegate { SetLightOrientation(LightOrientationVerticalValue); };
            notificationSettingsItem.Click += delegate { ShowNotificationSettingsDialog(); };
            colorSettingsItem.Click += delegate { ShowColorSettingsDialog(); };
            breathingLightItem.Click += delegate { ToggleBreathingLightEffect(); };
            soundSettingsItem.Click += delegate { ShowSoundSettingsDialog(); };
            configureClaudeHooksItem.Click += delegate { ConfigureClaudeHooksForCurrentUser(); };
            removeClaudeHooksItem.Click += delegate { RemoveClaudeHooksForCurrentUser(); };
            workItem.Click += delegate { ManualState("working", T.ManualWorking); };
            confirmItem.Click += delegate { ManualState("confirm", T.ManualConfirm); };
            doneItem.Click += delegate { ManualState("done", T.ManualDone); };
            aboutItem.Click += delegate { ShowAboutDialog(); };
            exitItem.Click += delegate { Close(); };
            skinItem.DropDownItems.Add(skinSystemItem);
            skinItem.DropDownItems.Add(skinDarkItem);
            skinItem.DropDownItems.Add(skinLightItem);
            skinItem.DropDownItems.Add(skinTransparentItem);
            lightOrientationItem.DropDownItems.Add(lightOrientationHorizontalItem);
            lightOrientationItem.DropDownItems.Add(lightOrientationVerticalItem);
            claudeHooksItem.DropDownItems.Add(configureClaudeHooksItem);
            claudeHooksItem.DropDownItems.Add(removeClaudeHooksItem);
            UpdateSkinMenuChecks();
            UpdateLightOrientationMenuChecks();
            UpdateBreathingLightMenuCheck();
            UpdateRestoreAutoDetectMenu();

            menu.Items.Add(restoreAutoItem);
            menu.Items.Add(skinItem);
            menu.Items.Add(colorSettingsItem);
            menu.Items.Add(notificationSettingsItem);
            menu.Items.Add(soundSettingsItem);
            menu.Items.Add(breathingLightItem);
            menu.Items.Add(lightOrientationItem);
            menu.Items.Add(claudeHooksItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(confirmItem);
            menu.Items.Add(workItem);
            menu.Items.Add(doneItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(aboutItem);
            menu.Items.Add(exitItem);
            ContextMenuStrip = menu;

            flashTimer = new System.Windows.Forms.Timer { Interval = 180 };
            flashTimer.Tick += delegate
            {
                if (doneFlashTicksRemaining <= 0)
                {
                    doneFlashVisible = true;
                    flashTimer.Stop();
                    UpdateBreathingTimer();
                    RenderLayeredWindow();
                    return;
                }

                doneFlashVisible = !doneFlashVisible;
                doneFlashTicksRemaining--;
                RenderLayeredWindow();
            };

            breathingTimer = new System.Windows.Forms.Timer { Interval = BreathingTimerIntervalMilliseconds };
            breathingTimer.Tick += delegate
            {
                if (!HasBreathingLight())
                {
                    breathingTick = 0;
                    breathingTimer.Stop();
                    RenderLayeredWindow();
                    return;
                }

                breathingTick = (breathingTick + 1) % BreathingCycleTicks;
                RenderLayeredWindow();
            };

            ReadStatus();
            UpdateAutoStatus();
            notificationsReady = true;
            statusTimer = new System.Windows.Forms.Timer { Interval = 800 };
            statusTimer.Tick += delegate
            {
                ReadStatus();
                UpdateAutoStatus();
                tooltip.SetToolTip(this, currentLabel + " - " + currentMessage);
                RenderLayeredWindow();
            };
            statusTimer.Start();

            topMostTimer = new System.Windows.Forms.Timer { Interval = 250 };
            topMostTimer.Tick += delegate { ApplyTopMost(); };
            topMostTimer.Start();
        }

        private void ApplyApplicationIcon()
        {
            try
            {
                using (Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath))
                {
                    if (appIcon != null)
                    {
                        Icon = (Icon)appIcon.Clone();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Application icon load failed: " + ex.Message);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Logger.Write("Form shown at " + Location.X + "," + Location.Y + " handle=" + Handle);
            RenderLayeredWindow();
            ApplyTopMost();
            BeginUpdateCheck();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Logger.Write("Handle created: " + Handle);
            RenderLayeredWindow();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WsExLayered;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            RenderLayeredWindow();
        }

        protected override void WndProc(ref Message m)
        {
            const int WmGetMinMaxInfo = 0x0024;
            if (m.Msg == WmGetMinMaxInfo)
            {
                base.WndProc(ref m);
                var info = (MinMaxInfo)Marshal.PtrToStructure(m.LParam, typeof(MinMaxInfo));
                info.ptMinTrackSize.X = 1;
                info.ptMinTrackSize.Y = 1;
                Marshal.StructureToPtr(info, m.LParam, false);
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            dragging = true;
            dragStartMouse = Cursor.Position;
            dragStartWindow = Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;

            Point mouse = Cursor.Position;
            Location = new Point(dragStartWindow.X + mouse.X - dragStartMouse.X, dragStartWindow.Y + mouse.Y - dragStartMouse.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                dragging = false;
                SaveWindowLocation();
                ApplyTopMost();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveWindowLocation();
            if (updateAvailableImage != null)
            {
                updateAvailableImage.Dispose();
                updateAvailableImage = null;
            }
            base.OnFormClosing(e);
        }

        private void ManualState(string state, string message)
        {
            autoDetect = false;
            UpdateRestoreAutoDetectMenu();
            WriteStatus(state, message, "manual");
            RenderLayeredWindow();
        }

        private void RestoreAutoDetect()
        {
            autoDetect = true;
            lastExternalStatusAt = DateTime.MinValue;
            UpdateRestoreAutoDetectMenu();
            UpdateAutoStatus();
            tooltip.SetToolTip(this, currentLabel + " - " + currentMessage);
            RenderLayeredWindow();
        }

        private void SetSkin(string skin)
        {
            string normalized = NormalizeSkin(skin);
            if (String.Equals(currentSkin, normalized, StringComparison.OrdinalIgnoreCase))
            {
                UpdateSkinMenuChecks();
                return;
            }

            currentSkin = normalized;
            UpdateSkinMenuChecks();
            WriteSettings();
            RenderLayeredWindow();
        }

        private void SetLightOrientation(string orientation)
        {
            string normalized = NormalizeLightOrientation(orientation);
            if (String.Equals(currentLightOrientation, normalized, StringComparison.OrdinalIgnoreCase))
            {
                UpdateLightOrientationMenuChecks();
                return;
            }

            currentLightOrientation = normalized;
            UpdateLightOrientationMenuChecks();
            ApplyLightWindowSize();
            WriteSettings();
            RenderLayeredWindow();
            ApplyTopMost();
        }

        private void ToggleBreathingLightEffect()
        {
            breathingLightEnabled = !breathingLightEnabled;
            UpdateBreathingLightMenuCheck();
            WriteSettings();
            UpdateBreathingTimer();
            RenderLayeredWindow();
        }

        private void ApplyLightWindowSize()
        {
            Size targetSize = GetLightWindowSize();
            if (ClientSize.Width != targetSize.Width || ClientSize.Height != targetSize.Height)
            {
                ClientSize = targetSize;
            }
        }

        private Size GetLightWindowSize()
        {
            return IsVerticalLightOrientation() ? VerticalLightWindowSize : HorizontalLightWindowSize;
        }

        private bool IsVerticalLightOrientation()
        {
            return String.Equals(currentLightOrientation, LightOrientationVerticalValue, StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateSkinMenuChecks()
        {
            skinSystemItem.Checked = String.Equals(currentSkin, "system", StringComparison.OrdinalIgnoreCase);
            skinDarkItem.Checked = String.Equals(currentSkin, "dark", StringComparison.OrdinalIgnoreCase);
            skinLightItem.Checked = String.Equals(currentSkin, "light", StringComparison.OrdinalIgnoreCase);
            skinTransparentItem.Checked = String.Equals(currentSkin, "transparent", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateLightOrientationMenuChecks()
        {
            lightOrientationHorizontalItem.Checked = !IsVerticalLightOrientation();
            lightOrientationVerticalItem.Checked = IsVerticalLightOrientation();
        }

        private void UpdateBreathingLightMenuCheck()
        {
            breathingLightItem.Checked = breathingLightEnabled;
        }

        private void UpdateRestoreAutoDetectMenu()
        {
            restoreAutoItem.Visible = !autoDetect;
        }
    }
}
