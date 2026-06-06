using System;
using System.Globalization;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {

        private void UpdateAutoStatus()
        {
            if (!autoDetect) return;

            if ((DateTime.Now - lastExternalStatusAt).TotalSeconds < ExternalStatusHoldSeconds)
            {
                return;
            }

            DateTime now = DateTime.Now;
            SessionSnapshot sessions = sessionAggregator.GetSnapshot(now, DoneHoldSeconds);
            if (sessions.IsAvailable)
            {
                UpdateSessionAwareStatus(sessions, now);
                return;
            }

            ActivitySnapshot activity = activityMonitor.GetSnapshot();

            activityMonitor.UpdateBaseline(activity, hasSeenBusy);

            if (doneAnnounced)
            {
                if (activity.IsStrongBusy)
                {
                    strongBusyAfterDoneCount++;
                }
                else
                {
                    strongBusyAfterDoneCount = 0;
                }

                if (strongBusyAfterDoneCount >= StrongBusySamplesAfterDone)
                {
                    doneAnnounced = false;
                    strongBusyAfterDoneCount = 0;
                }
                else if ((now - doneAnnouncedAt).TotalSeconds < DoneHoldSeconds)
                {
                    WriteStatus("done", T.WorkJustFinished, "auto");
                    return;
                }
                else
                {
                    doneAnnounced = false;
                    hasSeenBusy = false;
                    quietSampleCount = 0;
                    strongBusyAfterDoneCount = 0;
                    WriteStatus("waiting", T.CodexWaiting, "auto");
                    return;
                }
            }

            if (activity.IsBusy)
            {
                lastBusyAt = now;
                hasSeenBusy = true;
                doneAnnounced = false;
                quietSampleCount = 0;
                WriteStatus("working", String.Format(CultureInfo.InvariantCulture, T.HybridWorkingFormat, activity.TotalCpuPercent, activity.AppServerCpuPercent, activityMonitor.TotalCpuBaseline, activityMonitor.AppServerCpuBaseline), "auto");
                return;
            }

            if (hasSeenBusy)
            {
                quietSampleCount++;
            }

            ForegroundSnapshot foreground = foregroundAppDetector.GetSnapshot();
            if (hasSeenBusy && foreground.IsExternalApp && (now - lastBusyAt).TotalSeconds < ExternalForegroundHoldSeconds)
            {
                lastExternalForegroundAt = now;
                WriteStatus("working", String.Format(CultureInfo.InvariantCulture, T.OperatingComputerFormat, foreground.ProcessName), "auto");
                return;
            }

            if (hasSeenBusy && (now - lastExternalForegroundAt).TotalSeconds < QuietBeforeDoneSeconds)
            {
                WriteStatus("working", T.OperatingComputer, "auto");
                return;
            }

            bool stillThinking = hasSeenBusy &&
                ((now - lastBusyAt).TotalSeconds < QuietBeforeDoneSeconds ||
                 quietSampleCount < QuietSamplesBeforeDone);

            if (stillThinking)
            {
                WriteStatus("working", T.CodexThinking, "auto");
                return;
            }

            if (hasSeenBusy && (now - lastBusyAt).TotalSeconds < QuietBeforeDoneSeconds + DoneHoldSeconds)
            {
                doneAnnounced = true;
                doneAnnouncedAt = now;
                WriteStatus("done", T.WorkJustFinished, "auto");
                return;
            }

            hasSeenBusy = false;
            quietSampleCount = 0;
            WriteStatus("waiting", T.CodexWaiting, "auto");
        }


        private void UpdateSessionAwareStatus(SessionSnapshot sessions, DateTime now)
        {
            bool hasNewCompletion = sessions.LatestCompletionAt > lastSessionCompletionSeen;

            if (!sessionTrackingInitialized)
            {
                sessionTrackingInitialized = true;
                lastSessionCompletionSeen = sessions.LatestCompletionAt;
                hasNewCompletion = false;
            }
            else if (hasNewCompletion)
            {
                lastSessionCompletionSeen = sessions.LatestCompletionAt;
            }

            if (hasNewCompletion)
            {
                doneAnnouncedAt = now;
            }

            doneAnnounced = sessions.DoneCount > 0;
            hasSeenBusy = sessions.WorkingCount > 0 || sessions.ConfirmCount > 0;
            int previousDoneCount = currentDoneCount;
            WriteStatusCounts(sessions.WorkingCount, sessions.ConfirmCount, sessions.DoneCount, BuildCountsMessage(sessions.WorkingCount, sessions.ConfirmCount, sessions.DoneCount), "session");
            if (hasNewCompletion)
            {
                StartDoneFlash();
                if (sessions.DoneCount <= previousDoneCount)
                {
                    PlayNotificationSound("done");
                    SendSelectedStateNotification("done", Math.Max(1, sessions.DoneCount));
                }
            }
        }
    }
}
