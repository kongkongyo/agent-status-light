using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace WorkStatusLight
{
    internal sealed class ClaudeSessionScanner
    {
        private const int SessionActiveFreshSeconds = 30;
        private const int EventFileFreshHours = 24;

        private readonly string eventsPath;
        private DateTime lastSessionScanAt = DateTime.MinValue;
        private SessionSnapshot cachedSessionSnapshot;

        public ClaudeSessionScanner()
            : this(ClaudeHookRecorder.GetEventsPath())
        {
        }

        internal ClaudeSessionScanner(string eventsPath)
        {
            this.eventsPath = eventsPath;
        }

        public SessionSnapshot GetSnapshot(DateTime now, int doneHoldSeconds)
        {
            if ((now - lastSessionScanAt).TotalMilliseconds < 700 && cachedSessionSnapshot != null)
            {
                return cachedSessionSnapshot;
            }

            lastSessionScanAt = now;
            if (String.IsNullOrWhiteSpace(eventsPath) || !File.Exists(eventsPath))
            {
                cachedSessionSnapshot = SessionSnapshot.Unavailable();
                return cachedSessionSnapshot;
            }

            try
            {
                DateTime eventCutoff = now.AddHours(-EventFileFreshHours);
                string[] lines = ClaudeEventsFile.ReadRecentLines(eventsPath);
                var sessions = new Dictionary<string, ClaudeSessionState>(StringComparer.OrdinalIgnoreCase);

                foreach (string line in lines)
                {
                    ClaudeHookEvent hookEvent;
                    if (!TryParseEvent(line, out hookEvent)) continue;
                    if (hookEvent.Timestamp < eventCutoff) continue;

                    ClaudeSessionState state;
                    if (!sessions.TryGetValue(hookEvent.SessionKey, out state))
                    {
                        state = new ClaudeSessionState();
                        sessions[hookEvent.SessionKey] = state;
                    }

                    state.Apply(hookEvent);
                }

                DateTime latestCompletionAt = DateTime.MinValue;
                int workingCount = 0;
                int confirmCount = 0;
                int doneCount = 0;

                foreach (ClaudeSessionState state in sessions.Values)
                {
                    if (state.LatestDoneAt > latestCompletionAt)
                    {
                        latestCompletionAt = state.LatestDoneAt;
                    }

                    bool pendingConfirmation = state.HasPendingConfirmation;
                    bool unfinished = state.LatestWorkingAt > state.LatestDoneAt &&
                        state.LatestWorkingAt > state.LatestEndAt;
                    bool fresh = state.LatestEventAt != DateTime.MinValue &&
                        (now - state.LatestEventAt).TotalSeconds < SessionActiveFreshSeconds;
                    bool recentlyDone = state.LatestDoneAt != DateTime.MinValue &&
                        (now - state.LatestDoneAt).TotalSeconds < doneHoldSeconds;

                    if (pendingConfirmation)
                    {
                        confirmCount++;
                    }
                    else if (unfinished && fresh)
                    {
                        workingCount++;
                    }
                    else if (recentlyDone)
                    {
                        doneCount++;
                    }
                }

                cachedSessionSnapshot = new SessionSnapshot(true, workingCount, confirmCount, doneCount, latestCompletionAt);
                return cachedSessionSnapshot;
            }
            catch (Exception ex)
            {
                Logger.Write("Claude session scan unavailable: " + ex.Message);
                cachedSessionSnapshot = SessionSnapshot.Unavailable();
                return cachedSessionSnapshot;
            }
        }

        private static bool TryParseEvent(string line, out ClaudeHookEvent hookEvent)
        {
            hookEvent = null;
            if (String.IsNullOrWhiteSpace(line)) return false;

            try
            {
                var serializer = new JavaScriptSerializer();
                var root = serializer.DeserializeObject(line) as Dictionary<string, object>;
                if (root == null) return false;

                var payload = ReadDictionary(root, "payload") ?? root;
                string eventName = FirstNonEmpty(
                    ReadString(root, "hookEventName"),
                    ReadString(root, "hook_event_name"),
                    ReadString(payload, "hook_event_name"));
                if (String.IsNullOrWhiteSpace(eventName)) return false;

                DateTime timestamp = ReadTimestamp(root, payload);
                if (timestamp == DateTime.MinValue) return false;

                string sessionId = FirstNonEmpty(
                    ReadString(root, "sessionId"),
                    ReadString(root, "session_id"),
                    ReadString(payload, "session_id"));
                string transcriptPath = FirstNonEmpty(
                    ReadString(root, "transcriptPath"),
                    ReadString(root, "transcript_path"),
                    ReadString(payload, "transcript_path"));
                string cwd = FirstNonEmpty(
                    ReadString(root, "cwd"),
                    ReadString(payload, "cwd"));
                string notificationType = FirstNonEmpty(
                    ReadString(root, "notificationType"),
                    ReadString(root, "notification_type"),
                    ReadString(payload, "notification_type"),
                    ReadString(payload, "type"));
                bool hasBackgroundTasks = ReadBool(root, "hasBackgroundTasks") ||
                    HasItems(payload, "background_tasks") ||
                    HasItems(payload, "session_crons");
                bool messageFinal = ReadBool(root, "messageFinal") ||
                    ReadBool(root, "final") ||
                    ReadBool(payload, "final");

                hookEvent = new ClaudeHookEvent(
                    timestamp,
                    eventName,
                    BuildSessionKey(sessionId, transcriptPath, cwd),
                    notificationType,
                    hasBackgroundTasks,
                    messageFinal);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static DateTime ReadTimestamp(Dictionary<string, object> root, Dictionary<string, object> payload)
        {
            string value = FirstNonEmpty(
                ReadString(root, "recordedAt"),
                ReadString(root, "timestamp"),
                ReadString(payload, "timestamp"));
            if (String.IsNullOrWhiteSpace(value)) return DateTime.MinValue;

            DateTime parsed;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            {
                return parsed.ToLocalTime();
            }

            return DateTime.MinValue;
        }

        private static string BuildSessionKey(string sessionId, string transcriptPath, string cwd)
        {
            if (!String.IsNullOrWhiteSpace(sessionId)) return "session:" + sessionId;
            if (!String.IsNullOrWhiteSpace(transcriptPath)) return "transcript:" + transcriptPath;
            if (!String.IsNullOrWhiteSpace(cwd)) return "cwd:" + cwd;
            return "default";
        }

        private static Dictionary<string, object> ReadDictionary(Dictionary<string, object> values, string key)
        {
            if (values == null) return null;

            object value;
            if (!values.TryGetValue(key, out value)) return null;
            return value as Dictionary<string, object>;
        }

        private static string ReadString(Dictionary<string, object> values, string key)
        {
            if (values == null) return String.Empty;

            object value;
            if (!values.TryGetValue(key, out value) || value == null) return String.Empty;
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? String.Empty;
        }

        private static bool ReadBool(Dictionary<string, object> values, string key)
        {
            if (values == null) return false;

            object value;
            if (!values.TryGetValue(key, out value) || value == null) return false;
            if (value is bool) return (bool)value;

            bool parsed;
            return Boolean.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out parsed) && parsed;
        }

        private static bool HasItems(Dictionary<string, object> values, string key)
        {
            if (values == null) return false;

            object value;
            if (!values.TryGetValue(key, out value) || value == null) return false;

            var collection = value as ICollection;
            if (collection != null) return collection.Count > 0;

            return false;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!String.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return String.Empty;
        }

        private sealed class ClaudeHookEvent
        {
            public ClaudeHookEvent(DateTime timestamp, string eventName, string sessionKey, string notificationType, bool hasBackgroundTasks, bool messageFinal)
            {
                Timestamp = timestamp;
                EventName = eventName;
                SessionKey = sessionKey;
                NotificationType = notificationType;
                HasBackgroundTasks = hasBackgroundTasks;
                MessageFinal = messageFinal;
            }

            public DateTime Timestamp { get; private set; }
            public string EventName { get; private set; }
            public string SessionKey { get; private set; }
            public string NotificationType { get; private set; }
            public bool HasBackgroundTasks { get; private set; }
            public bool MessageFinal { get; private set; }
        }

        private sealed class ClaudeSessionState
        {
            public DateTime LatestEventAt { get; private set; }
            public DateTime LatestWorkingAt { get; private set; }
            public DateTime LatestDoneAt { get; private set; }
            public DateTime LatestEndAt { get; private set; }
            public bool HasPendingConfirmation { get; private set; }

            public void Apply(ClaudeHookEvent hookEvent)
            {
                if (hookEvent.Timestamp > LatestEventAt)
                {
                    LatestEventAt = hookEvent.Timestamp;
                }

                if (IsConfirmationEvent(hookEvent))
                {
                    HasPendingConfirmation = true;
                }
                else if (ClearsConfirmation(hookEvent.EventName))
                {
                    HasPendingConfirmation = false;
                }

                if (IsWorkingEvent(hookEvent) && hookEvent.Timestamp > LatestWorkingAt)
                {
                    LatestWorkingAt = hookEvent.Timestamp;
                }

                if (IsDoneEvent(hookEvent) && hookEvent.Timestamp > LatestDoneAt)
                {
                    LatestDoneAt = hookEvent.Timestamp;
                }

                if (IsEndEvent(hookEvent) && hookEvent.Timestamp > LatestEndAt)
                {
                    LatestEndAt = hookEvent.Timestamp;
                }
            }

            private static bool IsWorkingEvent(ClaudeHookEvent hookEvent)
            {
                return !hookEvent.MessageFinal && (hookEvent.HasBackgroundTasks ||
                    IsWorkingEvent(hookEvent.EventName));
            }

            private static bool IsWorkingEvent(string eventName)
            {
                return String.Equals(eventName, "UserPromptSubmit", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "UserPromptExpansion", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "PreToolUse", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "PostToolUse", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "PostToolUseFailure", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "PostToolBatch", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "PermissionDenied", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "SubagentStart", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "TaskCreated", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "MessageDisplay", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "ElicitationResult", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsDoneEvent(ClaudeHookEvent hookEvent)
            {
                return !hookEvent.HasBackgroundTasks &&
                    (IsDoneEvent(hookEvent.EventName) ||
                     (hookEvent.MessageFinal && String.Equals(hookEvent.EventName, "MessageDisplay", StringComparison.OrdinalIgnoreCase)));
            }

            private static bool IsDoneEvent(string eventName)
            {
                return String.Equals(eventName, "Stop", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(eventName, "StopFailure", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsEndEvent(ClaudeHookEvent hookEvent)
            {
                return (!hookEvent.HasBackgroundTasks && IsDoneEvent(hookEvent.EventName)) ||
                    (hookEvent.MessageFinal && String.Equals(hookEvent.EventName, "MessageDisplay", StringComparison.OrdinalIgnoreCase)) ||
                    String.Equals(hookEvent.EventName, "SubagentStop", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(hookEvent.EventName, "TaskCompleted", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(hookEvent.EventName, "TeammateIdle", StringComparison.OrdinalIgnoreCase) ||
                    (String.Equals(hookEvent.EventName, "Notification", StringComparison.OrdinalIgnoreCase) &&
                     String.Equals(hookEvent.NotificationType, "idle_prompt", StringComparison.OrdinalIgnoreCase)) ||
                    String.Equals(hookEvent.EventName, "SessionEnd", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsConfirmationEvent(ClaudeHookEvent hookEvent)
            {
                if (String.Equals(hookEvent.EventName, "PermissionRequest", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(hookEvent.EventName, "Elicitation", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!String.Equals(hookEvent.EventName, "Notification", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return String.Equals(hookEvent.NotificationType, "permission_prompt", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(hookEvent.NotificationType, "elicitation_dialog", StringComparison.OrdinalIgnoreCase);
            }

            private static bool ClearsConfirmation(string eventName)
            {
                return IsWorkingEvent(eventName) ||
                    IsDoneEvent(eventName) ||
                    String.Equals(eventName, "SessionEnd", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
