using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal static class WindowsNativeNotifier
    {
        private const int BalloonTimeoutMilliseconds = 6000;
        private const int DisposeDelayMilliseconds = 10000;
        private static readonly object SyncRoot = new object();
        private static NotifyIcon activeIcon;
        private static Timer disposeTimer;

        public static void SendAsync(string state, string title, string body, bool showResult, Form owner)
        {
            if (owner == null || owner.IsDisposed)
            {
                Logger.Write("Windows native notification skipped state=" + state + " reason=missing-owner");
                return;
            }

            try
            {
                owner.BeginInvoke((MethodInvoker)delegate
                {
                    ShowNotification(state, title, body, showResult, owner);
                });
            }
            catch (Exception ex)
            {
                Logger.Write("Windows native notification queue failed state=" + state + " error=" + ex.Message);
                if (showResult)
                {
                    ShowResult(owner, T.WindowsNativeSendFailed, MessageBoxIcon.Warning);
                }
            }
        }

        private static void ShowNotification(string state, string title, string body, bool showResult, Form owner)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                NotifyIcon notifyIcon = GetOrCreateNotifyIcon(owner);

                string safeTitle = String.IsNullOrWhiteSpace(title) ? T.AppTitle : title.Trim();
                string safeBody = body ?? String.Empty;
                notifyIcon.ShowBalloonTip(BalloonTimeoutMilliseconds, safeTitle, safeBody, ToolTipIcon.Info);
                ScheduleDispose();

                Logger.Write("Windows native notification shown state=" + state + " elapsedMs=" + stopwatch.ElapsedMilliseconds);
                if (showResult)
                {
                    ShowResult(owner, T.WindowsNativeTestSent, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                DisposeActiveIcon();
                Logger.Write("Windows native notification failed state=" + state + " elapsedMs=" + stopwatch.ElapsedMilliseconds + " error=" + ex.Message);
                if (showResult)
                {
                    ShowResult(owner, T.WindowsNativeSendFailed, MessageBoxIcon.Warning);
                }
            }
        }

        private static Icon GetNotificationIcon(Form owner)
        {
            if (owner != null && owner.Icon != null)
            {
                return owner.Icon;
            }

            return SystemIcons.Information;
        }

        private static NotifyIcon GetOrCreateNotifyIcon(Form owner)
        {
            lock (SyncRoot)
            {
                if (activeIcon == null)
                {
                    activeIcon = new NotifyIcon();
                }

                activeIcon.Icon = GetNotificationIcon(owner);
                activeIcon.Text = T.AppTitle;
                activeIcon.Visible = true;
                return activeIcon;
            }
        }

        private static void ScheduleDispose()
        {
            lock (SyncRoot)
            {
                if (disposeTimer == null)
                {
                    disposeTimer = new Timer { Interval = DisposeDelayMilliseconds };
                    disposeTimer.Tick += delegate
                    {
                        DisposeActiveIcon();
                    };
                }

                disposeTimer.Stop();
                disposeTimer.Start();
            }
        }

        private static void DisposeActiveIcon()
        {
            NotifyIcon iconToDispose = null;
            Timer timerToDispose = null;

            lock (SyncRoot)
            {
                timerToDispose = disposeTimer;
                disposeTimer = null;
                iconToDispose = activeIcon;
                activeIcon = null;
            }

            try
            {
                if (timerToDispose != null)
                {
                    timerToDispose.Stop();
                    timerToDispose.Dispose();
                }
            }
            catch
            {
            }

            try
            {
                if (iconToDispose != null)
                {
                    iconToDispose.Visible = false;
                    iconToDispose.Dispose();
                }
            }
            catch
            {
            }
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
