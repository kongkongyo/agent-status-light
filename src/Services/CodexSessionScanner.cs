using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace WorkStatusLight
{
    internal sealed class CodexSessionScanner
    {
        private const int SessionActiveFreshSeconds = 900;
        private const int SessionFileFreshHours = 24;
        private const int PendingConfirmationHoldHours = 6;
        private const int SessionFileLimit = 30;
        private const int SessionScanRefreshMilliseconds = 1200;
        private static readonly Regex ApprovalGatedCommandPattern = new Regex(
            @"(^|[;&|{(]\s*)(remove-item|rm|del|erase|rmdir|rd|stop-process)\b|(^|[;&|{(]\s*)git\s+(reset|clean)\b|(^|[;&|{(]\s*)git\s+checkout\s+--\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex TrustedReadOnlyCommandPattern = new Regex(
            @"^\s*(cat|type|get-content|ls|dir|get-childitem|pwd|get-location|rg|grep|findstr|select-string|where|where\.exe|get-process|git\s+(status|diff|show|log|branch|remote|rev-parse)\b)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly Dictionary<string, SessionFileSnapshot> sessionFileCache = new Dictionary<string, SessionFileSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly object syncRoot = new object();
        private readonly string sessionsRoot;

        private DateTime lastSessionScanAt = DateTime.MinValue;
        private SessionSnapshot cachedSessionSnapshot;
        private bool sessionScanInProgress;

        public CodexSessionScanner()
        {
            sessionsRoot = Environment.GetEnvironmentVariable("CODEX_STATUS_SESSIONS_ROOT");
            if (String.IsNullOrWhiteSpace(sessionsRoot))
            {
                sessionsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
            }
        }

        public SessionSnapshot GetSnapshot(DateTime now, int doneHoldSeconds)
        {
            bool scanSynchronously = false;
            bool queueScan = false;
            SessionSnapshot snapshot;

            lock (syncRoot)
            {
                if (cachedSessionSnapshot == null)
                {
                    if (!sessionScanInProgress)
                    {
                        sessionScanInProgress = true;
                        lastSessionScanAt = now;
                        scanSynchronously = true;
                    }
                }
                else if (!sessionScanInProgress &&
                    (now - lastSessionScanAt).TotalMilliseconds >= SessionScanRefreshMilliseconds)
                {
                    sessionScanInProgress = true;
                    lastSessionScanAt = now;
                    queueScan = true;
                }

                snapshot = cachedSessionSnapshot;
            }

            if (scanSynchronously)
            {
                return ScanAndCache(now, doneHoldSeconds);
            }

            if (queueScan)
            {
                QueueScan(now, doneHoldSeconds);
            }

            return snapshot ?? SessionSnapshot.Unavailable();
        }

        private void QueueScan(DateTime now, int doneHoldSeconds)
        {
            try
            {
                if (!ThreadPool.QueueUserWorkItem(delegate { ScanAndCache(now, doneHoldSeconds); }))
                {
                    lock (syncRoot)
                    {
                        sessionScanInProgress = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Session scan queue failed: " + ex.Message);
                lock (syncRoot)
                {
                    sessionScanInProgress = false;
                }
            }
        }

        private SessionSnapshot ScanAndCache(DateTime now, int doneHoldSeconds)
        {
            SessionSnapshot snapshot = ScanSessions(now, doneHoldSeconds);
            lock (syncRoot)
            {
                cachedSessionSnapshot = snapshot;
                lastSessionScanAt = DateTime.Now;
                sessionScanInProgress = false;
            }

            return snapshot;
        }

        private SessionSnapshot ScanSessions(DateTime now, int doneHoldSeconds)
        {
            if (!Directory.Exists(sessionsRoot))
            {
                return SessionSnapshot.Unavailable();
            }

            try
            {
                DateTime fileCutoff = now.AddHours(-SessionFileFreshHours);
                string[] files = GetRecentSessionFiles(fileCutoff);
                PruneSessionFileCache(files);

                DateTime latestCompletionAt = DateTime.MinValue;
                int workingCount = 0;
                int confirmCount = 0;
                int doneCount = 0;

                foreach (string file in files)
                {
                    SessionFileSnapshot fileSnapshot;
                    try
                    {
                        fileSnapshot = GetFileSnapshot(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("Session file skipped: " + ex.Message);
                        continue;
                    }

                    if (fileSnapshot == null) continue;

                    if (fileSnapshot.LatestCompletionAt > latestCompletionAt)
                    {
                        latestCompletionAt = fileSnapshot.LatestCompletionAt;
                    }

                    bool unfinished = fileSnapshot.LatestUserAt > fileSnapshot.LatestCompletionAt &&
                        fileSnapshot.LatestUserAt > fileSnapshot.LatestEndAt;
                    bool fresh = fileSnapshot.LatestEventAt != DateTime.MinValue &&
                        (now - fileSnapshot.LatestEventAt).TotalSeconds < SessionActiveFreshSeconds;
                    bool recentlyDone = fileSnapshot.LatestCompletionAt != DateTime.MinValue &&
                        (now - fileSnapshot.LatestCompletionAt).TotalSeconds < doneHoldSeconds;

                    bool pendingConfirmation = fileSnapshot.HasPendingConfirmation &&
                        fileSnapshot.LatestPendingConfirmationAt > fileSnapshot.LatestCompletionAt &&
                        fileSnapshot.LatestPendingConfirmationAt > fileSnapshot.LatestEndAt &&
                        (now - fileSnapshot.LatestPendingConfirmationAt).TotalHours < PendingConfirmationHoldHours;

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

                return new SessionSnapshot(true, workingCount, confirmCount, doneCount, latestCompletionAt);
            }
            catch (Exception ex)
            {
                Logger.Write("Session scan unavailable: " + ex.Message);
                return SessionSnapshot.Unavailable();
            }
        }

        private string[] GetRecentSessionFiles(DateTime fileCutoff)
        {
            var candidates = new List<SessionFileCandidate>();
            foreach (string path in EnumerateSessionFiles(sessionsRoot))
            {
                DateTime lastWriteTime;
                try
                {
                    lastWriteTime = File.GetLastWriteTime(path);
                }
                catch (Exception ex)
                {
                    Logger.Write("Session file timestamp skipped: " + ex.Message);
                    continue;
                }

                if (lastWriteTime < fileCutoff)
                {
                    continue;
                }

                AddRecentSessionFile(candidates, new SessionFileCandidate(path, lastWriteTime));
            }

            return candidates.Select(candidate => candidate.Path).ToArray();
        }

        private static IEnumerable<string> EnumerateSessionFiles(string root)
        {
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                string[] files;
                try
                {
                    files = Directory.GetFiles(directory, "rollout-*.jsonl", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    Logger.Write("Session scan directory skipped: " + ex.Message);
                    files = new string[0];
                }

                foreach (string file in files)
                {
                    yield return file;
                }

                string[] directories;
                try
                {
                    directories = Directory.GetDirectories(directory);
                }
                catch (Exception ex)
                {
                    Logger.Write("Session scan child directories skipped: " + ex.Message);
                    directories = new string[0];
                }

                foreach (string child in directories)
                {
                    pending.Push(child);
                }
            }
        }

        private static void AddRecentSessionFile(List<SessionFileCandidate> candidates, SessionFileCandidate candidate)
        {
            int index = 0;
            while (index < candidates.Count && candidates[index].LastWriteTime >= candidate.LastWriteTime)
            {
                index++;
            }

            if (index >= SessionFileLimit)
            {
                return;
            }

            candidates.Insert(index, candidate);
            if (candidates.Count > SessionFileLimit)
            {
                candidates.RemoveAt(SessionFileLimit);
            }
        }

        private SessionFileSnapshot GetFileSnapshot(string path)
        {
            FileInfo info = new FileInfo(path);
            SessionFileSnapshot cached;
            if (sessionFileCache.TryGetValue(path, out cached) &&
                cached.Length == info.Length &&
                cached.LastWriteTimeUtc == info.LastWriteTimeUtc)
            {
                return cached;
            }

            DateTime latestUserAt = DateTime.MinValue;
            DateTime latestCompletionAt = DateTime.MinValue;
            DateTime latestEndAt = DateTime.MinValue;
            DateTime latestEventAt = DateTime.MinValue;
            var pendingConfirmations = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            string approvalPolicy = String.Empty;

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    DateTime timestamp = GetJsonTimestamp(line);
                    if (timestamp == DateTime.MinValue) continue;
                    if (timestamp > latestEventAt) latestEventAt = timestamp;

                    string currentApprovalPolicy;
                    if (TryGetApprovalPolicy(line, out currentApprovalPolicy))
                    {
                        approvalPolicy = currentApprovalPolicy;
                    }

                    bool userMessage = line.IndexOf("\"type\":\"user_message\"", StringComparison.Ordinal) >= 0 ||
                        (line.IndexOf("\"type\":\"message\"", StringComparison.Ordinal) >= 0 &&
                         line.IndexOf("\"role\":\"user\"", StringComparison.Ordinal) >= 0);
                    if (userMessage && timestamp > latestUserAt)
                    {
                        latestUserAt = timestamp;
                    }

                    bool completion = line.IndexOf("\"type\":\"task_complete\"", StringComparison.Ordinal) >= 0 ||
                        line.IndexOf("\"phase\":\"final_answer\"", StringComparison.Ordinal) >= 0;
                    if (completion && timestamp > latestCompletionAt)
                    {
                        latestCompletionAt = timestamp;
                    }
                    if (completion && timestamp > latestEndAt)
                    {
                        latestEndAt = timestamp;
                    }

                    bool end = line.IndexOf("\"type\":\"turn_aborted\"", StringComparison.Ordinal) >= 0 ||
                        line.IndexOf("\"type\":\"thread_rolled_back\"", StringComparison.Ordinal) >= 0;
                    if (end && timestamp > latestEndAt)
                    {
                        latestEndAt = timestamp;
                    }

                    string callId;
                    if (TryGetPendingConfirmationCallId(line, approvalPolicy, out callId))
                    {
                        pendingConfirmations[callId] = timestamp;
                    }
                    else if (TryGetFunctionCallOutputCallId(line, out callId))
                    {
                        pendingConfirmations.Remove(callId);
                    }
                }
            }

            DateTime latestPendingConfirmationAt = pendingConfirmations.Count == 0
                ? DateTime.MinValue
                : pendingConfirmations.Values.Max();
            var snapshot = new SessionFileSnapshot(info.Length, info.LastWriteTimeUtc, latestUserAt, latestCompletionAt, latestEndAt, latestEventAt, latestPendingConfirmationAt, pendingConfirmations.Count > 0);
            sessionFileCache[path] = snapshot;
            return snapshot;
        }

        private void PruneSessionFileCache(string[] activeFiles)
        {
            var active = new HashSet<string>(activeFiles, StringComparer.OrdinalIgnoreCase);
            foreach (string path in sessionFileCache.Keys.ToArray())
            {
                if (!active.Contains(path))
                {
                    sessionFileCache.Remove(path);
                }
            }
        }

        private static bool TryGetPendingConfirmationCallId(string line, string approvalPolicy, out string callId)
        {
            callId = String.Empty;
            if (line.IndexOf("function_call", StringComparison.Ordinal) < 0) return false;

            Dictionary<string, object> payload;
            if (!TryGetPayload(line, out payload)) return false;
            if (!String.Equals(ReadString(payload, "type"), "function_call", StringComparison.Ordinal)) return false;

            string candidateCallId = ReadString(payload, "call_id");
            if (String.IsNullOrEmpty(candidateCallId)) return false;

            string name = ReadString(payload, "name");
            if (String.Equals(name, "request_user_input", StringComparison.OrdinalIgnoreCase))
            {
                callId = candidateCallId;
                return true;
            }

            if (IsEscalatedToolCall(payload))
            {
                callId = candidateCallId;
                return true;
            }

            if (IsApprovalGatedToolCall(payload, approvalPolicy))
            {
                callId = candidateCallId;
                return true;
            }

            return false;
        }

        private static bool TryGetFunctionCallOutputCallId(string line, out string callId)
        {
            callId = String.Empty;
            if (line.IndexOf("function_call_output", StringComparison.Ordinal) < 0) return false;

            Dictionary<string, object> payload;
            if (!TryGetPayload(line, out payload)) return false;
            if (!String.Equals(ReadString(payload, "type"), "function_call_output", StringComparison.Ordinal)) return false;

            callId = ReadString(payload, "call_id");
            return !String.IsNullOrEmpty(callId);
        }

        private static bool IsEscalatedToolCall(Dictionary<string, object> payload)
        {
            Dictionary<string, object> arguments;
            if (!TryReadArguments(payload, out arguments)) return false;

            return ContainsEscalatedSandboxPermission(arguments);
        }

        private static bool IsApprovalGatedToolCall(Dictionary<string, object> payload, string approvalPolicy)
        {
            Dictionary<string, object> arguments;
            if (!TryReadArguments(payload, out arguments)) return false;

            return ContainsApprovalGatedShellCommand(arguments, IsShellCommandToolName(ReadString(payload, "name")), IsUntrustedApprovalPolicy(approvalPolicy));
        }

        private static bool ContainsEscalatedSandboxPermission(object value)
        {
            var values = value as Dictionary<string, object>;
            if (values != null)
            {
                if (String.Equals(ReadString(values, "sandbox_permissions"), "require_escalated", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                foreach (object child in values.Values)
                {
                    if (ContainsEscalatedSandboxPermission(child))
                    {
                        return true;
                    }
                }

                return false;
            }

            var list = value as object[];
            if (list != null)
            {
                foreach (object child in list)
                {
                    if (ContainsEscalatedSandboxPermission(child))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsApprovalGatedShellCommand(object value, bool shellContext, bool untrustedApproval)
        {
            var values = value as Dictionary<string, object>;
            if (values != null)
            {
                bool currentShellContext = shellContext ||
                    IsShellCommandToolName(ReadString(values, "name")) ||
                    IsShellCommandToolName(ReadString(values, "recipient_name"));

                string command;
                if (currentShellContext &&
                    TryReadNonEmptyString(values, "command", out command) &&
                    (IsApprovalGatedCommand(command) ||
                     (untrustedApproval && !IsTrustedReadOnlyCommand(command))))
                {
                    return true;
                }

                object parameters;
                if (values.TryGetValue("parameters", out parameters) &&
                    ContainsApprovalGatedShellCommand(parameters, currentShellContext, untrustedApproval))
                {
                    return true;
                }

                foreach (object child in values.Values)
                {
                    if (ReferenceEquals(child, parameters))
                    {
                        continue;
                    }

                    if (ContainsApprovalGatedShellCommand(child, currentShellContext, untrustedApproval))
                    {
                        return true;
                    }
                }

                return false;
            }

            var list = value as object[];
            if (list != null)
            {
                foreach (object child in list)
                {
                    if (ContainsApprovalGatedShellCommand(child, shellContext, untrustedApproval))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsShellCommandToolName(string value)
        {
            return String.Equals(value, "shell_command", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(value, "functions.shell_command", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsApprovalGatedCommand(string command)
        {
            return !String.IsNullOrWhiteSpace(command) &&
                ApprovalGatedCommandPattern.IsMatch(command);
        }

        private static bool IsUntrustedApprovalPolicy(string approvalPolicy)
        {
            return String.Equals(approvalPolicy, "untrusted", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrustedReadOnlyCommand(string command)
        {
            if (String.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            string trimmed = command.Trim();
            if (trimmed.IndexOf('\n') >= 0 ||
                trimmed.IndexOf(';') >= 0 ||
                trimmed.IndexOf('|') >= 0 ||
                trimmed.IndexOf("&&", StringComparison.Ordinal) >= 0 ||
                trimmed.IndexOf("||", StringComparison.Ordinal) >= 0 ||
                trimmed.IndexOf('>') >= 0 ||
                trimmed.IndexOf('<') >= 0)
            {
                return false;
            }

            return TrustedReadOnlyCommandPattern.IsMatch(trimmed);
        }

        private static bool TryReadNonEmptyString(Dictionary<string, object> values, string key, out string result)
        {
            result = ReadString(values, key);
            return !String.IsNullOrWhiteSpace(result);
        }

        private static bool TryGetPayload(string line, out Dictionary<string, object> payload)
        {
            payload = null;

            try
            {
                var serializer = new JavaScriptSerializer();
                var root = serializer.DeserializeObject(line) as Dictionary<string, object>;
                if (root == null) return false;

                object value;
                if (!root.TryGetValue("payload", out value)) return false;
                payload = value as Dictionary<string, object>;
                return payload != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetApprovalPolicy(string line, out string approvalPolicy)
        {
            approvalPolicy = String.Empty;
            if (line.IndexOf("\"type\":\"turn_context\"", StringComparison.Ordinal) < 0) return false;

            try
            {
                var serializer = new JavaScriptSerializer();
                var root = serializer.DeserializeObject(line) as Dictionary<string, object>;
                if (root == null) return false;

                object value;
                if (!root.TryGetValue("payload", out value)) return false;
                approvalPolicy = ReadString(value as Dictionary<string, object>, "approval_policy");
                return !String.IsNullOrWhiteSpace(approvalPolicy);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadArguments(Dictionary<string, object> payload, out Dictionary<string, object> arguments)
        {
            arguments = null;
            string rawArguments = ReadString(payload, "arguments");
            if (String.IsNullOrWhiteSpace(rawArguments)) return false;

            try
            {
                var serializer = new JavaScriptSerializer();
                arguments = serializer.DeserializeObject(rawArguments) as Dictionary<string, object>;
                return arguments != null;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadString(Dictionary<string, object> values, string key)
        {
            if (values == null) return String.Empty;

            object value;
            if (!values.TryGetValue(key, out value) || value == null) return String.Empty;
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? String.Empty;
        }

        private static DateTime GetJsonTimestamp(string line)
        {
            const string prefix = "\"timestamp\":\"";
            int start = line.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0) return DateTime.MinValue;
            start += prefix.Length;
            int end = line.IndexOf('"', start);
            if (end <= start) return DateTime.MinValue;

            DateTime parsed;
            if (DateTime.TryParse(line.Substring(start, end - start), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            {
                return parsed.ToLocalTime();
            }

            return DateTime.MinValue;
        }

        private sealed class SessionFileCandidate
        {
            public SessionFileCandidate(string path, DateTime lastWriteTime)
            {
                Path = path;
                LastWriteTime = lastWriteTime;
            }

            public string Path { get; private set; }
            public DateTime LastWriteTime { get; private set; }
        }
    }
}
