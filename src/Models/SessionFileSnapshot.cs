using System;

namespace WorkStatusLight
{
internal sealed class SessionFileSnapshot
    {
        public SessionFileSnapshot(long length, DateTime lastWriteTimeUtc, DateTime latestUserAt, DateTime latestCompletionAt, DateTime latestEndAt, DateTime latestEventAt, DateTime latestPendingConfirmationAt, bool hasPendingConfirmation)
        {
            Length = length;
            LastWriteTimeUtc = lastWriteTimeUtc;
            LatestUserAt = latestUserAt;
            LatestCompletionAt = latestCompletionAt;
            LatestEndAt = latestEndAt;
            LatestEventAt = latestEventAt;
            LatestPendingConfirmationAt = latestPendingConfirmationAt;
            HasPendingConfirmation = hasPendingConfirmation;
        }

        public long Length { get; private set; }
        public DateTime LastWriteTimeUtc { get; private set; }
        public DateTime LatestUserAt { get; private set; }
        public DateTime LatestCompletionAt { get; private set; }
        public DateTime LatestEndAt { get; private set; }
        public DateTime LatestEventAt { get; private set; }
        public DateTime LatestPendingConfirmationAt { get; private set; }
        public bool HasPendingConfirmation { get; private set; }
    }
}
