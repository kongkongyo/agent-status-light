using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace WorkStatusLight
{
    internal static class ClaudeHookRecorder
    {
        private const string EventsPathEnvironmentVariable = "CODEX_STATUS_CLAUDE_EVENTS_PATH";

        public static int RecordFromStandardInput()
        {
            return Record(ReadStandardInput());
        }

        internal static int Record(string raw)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(raw))
                {
                    Logger.Write("Claude hook skipped: empty input");
                    return 0;
                }

                var serializer = new JavaScriptSerializer();
                string hookEventName = String.Empty;
                string sessionId = String.Empty;
                string transcriptPath = String.Empty;
                string cwd = String.Empty;
                string notificationType = String.Empty;
                bool hasBackgroundTasks = false;
                bool messageFinal = false;
                bool parseError = false;

                try
                {
                    var values = serializer.DeserializeObject(raw) as Dictionary<string, object>;
                    hookEventName = ReadString(values, "hook_event_name");
                    sessionId = ReadString(values, "session_id");
                    transcriptPath = ReadString(values, "transcript_path");
                    cwd = ReadString(values, "cwd");
                    notificationType = ReadString(values, "notification_type");
                    hasBackgroundTasks = HasItems(values, "background_tasks") ||
                        HasItems(values, "session_crons");
                    messageFinal = ReadBool(values, "final");
                }
                catch (Exception ex)
                {
                    Logger.Write("Claude hook input parse unavailable: " + ex.GetType().Name);
                    parseError = true;
                }

                var record = new Dictionary<string, object>
                {
                    { "recordedAt", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
                    { "source", "claude" },
                    { "hookEventName", hookEventName },
                    { "sessionId", sessionId },
                    { "transcriptPath", transcriptPath },
                    { "cwd", cwd },
                    { "notificationType", notificationType },
                    { "hasBackgroundTasks", hasBackgroundTasks },
                    { "messageFinal", messageFinal },
                    { "parseError", parseError }
                };

                string path = GetEventsPath();
                ClaudeEventsFile.AppendLine(path, serializer.Serialize(record));

                Logger.Write("Claude hook event recorded: " + (String.IsNullOrWhiteSpace(hookEventName) ? "unknown" : hookEventName) +
                    " session=" + sessionId);
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Write("Claude hook record failed: " + ex);
                return 1;
            }
        }

        public static string GetEventsPath()
        {
            string configured = Environment.GetEnvironmentVariable(EventsPathEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return Path.Combine(Paths.AppDirectory, "data", "claude-events.jsonl");
        }

        private static string ReadStandardInput()
        {
            using (var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
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
    }
}
