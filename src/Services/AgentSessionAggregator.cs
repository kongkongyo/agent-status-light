using System;

namespace WorkStatusLight
{
    internal sealed class AgentSessionAggregator
    {
        private readonly CodexSessionScanner codexSessionScanner;
        private readonly ClaudeSessionScanner claudeSessionScanner;

        public AgentSessionAggregator()
            : this(new CodexSessionScanner(), new ClaudeSessionScanner())
        {
        }

        internal AgentSessionAggregator(CodexSessionScanner codexSessionScanner, ClaudeSessionScanner claudeSessionScanner)
        {
            this.codexSessionScanner = codexSessionScanner;
            this.claudeSessionScanner = claudeSessionScanner;
        }

        public SessionSnapshot GetSnapshot(DateTime now, int doneHoldSeconds)
        {
            SessionSnapshot codex = codexSessionScanner.GetSnapshot(now, doneHoldSeconds);
            SessionSnapshot claude = claudeSessionScanner.GetSnapshot(now, doneHoldSeconds);

            if (!codex.IsAvailable && !claude.IsAvailable)
            {
                return SessionSnapshot.Unavailable();
            }

            return new SessionSnapshot(
                codex.IsAvailable || claude.IsAvailable,
                Count(codex.WorkingCount) + Count(claude.WorkingCount),
                Count(codex.ConfirmCount) + Count(claude.ConfirmCount),
                Count(codex.DoneCount) + Count(claude.DoneCount),
                Max(codex.LatestCompletionAt, claude.LatestCompletionAt));
        }

        private static int Count(int value)
        {
            return Math.Max(0, value);
        }

        private static DateTime Max(DateTime first, DateTime second)
        {
            return first >= second ? first : second;
        }
    }
}
