using System;

namespace WorkStatusLight
{
internal sealed class SessionSnapshot
    {
        public SessionSnapshot(bool isAvailable, int workingCount, int confirmCount, int doneCount, DateTime latestCompletionAt)
        {
            IsAvailable = isAvailable;
            WorkingCount = workingCount;
            ConfirmCount = confirmCount;
            DoneCount = doneCount;
            LatestCompletionAt = latestCompletionAt;
        }

        public bool IsAvailable { get; private set; }
        public int WorkingCount { get; private set; }
        public int ConfirmCount { get; private set; }
        public int DoneCount { get; private set; }
        public DateTime LatestCompletionAt { get; private set; }

        public static SessionSnapshot Unavailable()
        {
            return new SessionSnapshot(false, 0, 0, 0, DateTime.MinValue);
        }
    }
}
