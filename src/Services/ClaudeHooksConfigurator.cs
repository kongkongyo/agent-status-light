using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace WorkStatusLight
{
    internal static class ClaudeHooksConfigurator
    {
        private static readonly Encoding NoBomUtf8 = new UTF8Encoding(false);
        private static readonly string[] HookEvents = new[]
        {
            "UserPromptSubmit",
            "UserPromptExpansion",
            "PreToolUse",
            "PostToolUse",
            "PostToolUseFailure",
            "PostToolBatch",
            "PermissionDenied",
            "SubagentStart",
            "TaskCreated",
            "MessageDisplay",
            "ElicitationResult",
            "PermissionRequest",
            "Elicitation",
            "Notification",
            "Stop",
            "StopFailure",
            "SubagentStop",
            "TaskCompleted",
            "TeammateIdle",
            "SessionEnd"
        };

        public static string GetCurrentUserSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude",
                "settings.json");
        }

        public static string BuildHookCommand(string executablePath)
        {
            if (String.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path is required.", "executablePath");
            }

            return "\"" + executablePath + "\" --claude-hook";
        }

        public static ClaudeHooksConfigurationResult ConfigureCurrentUser(string executablePath)
        {
            return Configure(GetCurrentUserSettingsPath(), executablePath);
        }

        public static ClaudeHooksConfigurationResult RemoveCurrentUser(string executablePath)
        {
            return Remove(GetCurrentUserSettingsPath(), executablePath);
        }

        internal static ClaudeHooksConfigurationResult Configure(string settingsPath, string executablePath)
        {
            if (String.IsNullOrWhiteSpace(settingsPath))
            {
                throw new ArgumentException("Settings path is required.", "settingsPath");
            }

            Logger.Write("Claude hooks configure requested path=" + settingsPath);

            var settings = ReadSettings(settingsPath);
            Dictionary<string, object> hooks = EnsureObject(settings, "hooks");
            int addedEventCount = 0;

            foreach (string eventName in HookEvents)
            {
                if (EnsureEventHandler(hooks, eventName, executablePath))
                {
                    addedEventCount++;
                }
            }

            if (addedEventCount == 0)
            {
                Logger.Write("Claude hooks configure skipped: already configured path=" + settingsPath);
                return new ClaudeHooksConfigurationResult(settingsPath, String.Empty, 0, false);
            }

            string backupPath = String.Empty;
            if (File.Exists(settingsPath))
            {
                backupPath = settingsPath + ".bak-" + DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                File.Copy(settingsPath, backupPath, false);
                Logger.Write("Claude hooks settings backed up path=" + settingsPath + " backup=" + backupPath);
            }

            string directory = Path.GetDirectoryName(settingsPath);
            if (!String.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serializer = new JavaScriptSerializer();
            File.WriteAllText(settingsPath, serializer.Serialize(settings), NoBomUtf8);
            Logger.Write("Claude hooks configured path=" + settingsPath + " eventsAdded=" + addedEventCount);

            return new ClaudeHooksConfigurationResult(settingsPath, backupPath, addedEventCount, true);
        }

        internal static ClaudeHooksConfigurationResult Remove(string settingsPath, string executablePath)
        {
            if (String.IsNullOrWhiteSpace(settingsPath))
            {
                throw new ArgumentException("Settings path is required.", "settingsPath");
            }

            Logger.Write("Claude hooks remove requested path=" + settingsPath);

            if (!File.Exists(settingsPath))
            {
                Logger.Write("Claude hooks remove skipped: settings missing path=" + settingsPath);
                return new ClaudeHooksConfigurationResult(settingsPath, String.Empty, 0, false);
            }

            var settings = ReadSettings(settingsPath);
            object hooksValue;
            if (!settings.TryGetValue("hooks", out hooksValue) || hooksValue == null)
            {
                Logger.Write("Claude hooks remove skipped: hooks missing path=" + settingsPath);
                return new ClaudeHooksConfigurationResult(settingsPath, String.Empty, 0, false);
            }

            var hooks = hooksValue as Dictionary<string, object>;
            if (hooks == null)
            {
                throw new InvalidOperationException("Claude settings 'hooks' must be an object.");
            }

            int removedCount = RemoveAgentStatusLightHooks(hooks, executablePath);
            if (removedCount == 0)
            {
                Logger.Write("Claude hooks remove skipped: no matching hooks path=" + settingsPath);
                return new ClaudeHooksConfigurationResult(settingsPath, String.Empty, 0, false);
            }

            if (hooks.Count == 0)
            {
                settings.Remove("hooks");
            }

            string backupPath = settingsPath + ".bak-" + DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            File.Copy(settingsPath, backupPath, false);
            Logger.Write("Claude hooks settings backed up before remove path=" + settingsPath + " backup=" + backupPath);

            var serializer = new JavaScriptSerializer();
            File.WriteAllText(settingsPath, serializer.Serialize(settings), NoBomUtf8);
            Logger.Write("Claude hooks removed path=" + settingsPath + " handlersRemoved=" + removedCount);

            return new ClaudeHooksConfigurationResult(settingsPath, backupPath, removedCount, true);
        }

        private static Dictionary<string, object> ReadSettings(string settingsPath)
        {
            if (!File.Exists(settingsPath))
            {
                return new Dictionary<string, object>(StringComparer.Ordinal);
            }

            string raw = File.ReadAllText(settingsPath, Encoding.UTF8);
            if (String.IsNullOrWhiteSpace(raw))
            {
                return new Dictionary<string, object>(StringComparer.Ordinal);
            }

            var serializer = new JavaScriptSerializer();
            var settings = serializer.DeserializeObject(raw) as Dictionary<string, object>;
            if (settings == null)
            {
                throw new InvalidOperationException("Claude settings JSON root must be an object.");
            }

            return settings;
        }

        private static Dictionary<string, object> EnsureObject(Dictionary<string, object> parent, string key)
        {
            object existing;
            if (!parent.TryGetValue(key, out existing) || existing == null)
            {
                var created = new Dictionary<string, object>(StringComparer.Ordinal);
                parent[key] = created;
                return created;
            }

            var value = existing as Dictionary<string, object>;
            if (value == null)
            {
                throw new InvalidOperationException("Claude settings '" + key + "' must be an object.");
            }

            return value;
        }

        private static int RemoveAgentStatusLightHooks(Dictionary<string, object> hooks, string executablePath)
        {
            int removedCount = 0;
            var emptyEvents = new ArrayList();

            foreach (string eventName in new List<string>(hooks.Keys))
            {
                object groupsValue = hooks[eventName];
                ArrayList groups = ToArrayList(groupsValue, "Claude hooks event '" + eventName + "'");
                hooks[eventName] = groups;

                for (int groupIndex = groups.Count - 1; groupIndex >= 0; groupIndex--)
                {
                    var group = groups[groupIndex] as Dictionary<string, object>;
                    if (group == null) continue;

                    object handlersValue;
                    if (!group.TryGetValue("hooks", out handlersValue)) continue;

                    ArrayList handlers = ToArrayList(handlersValue, "Claude hooks handler list for '" + eventName + "'");
                    group["hooks"] = handlers;

                    for (int handlerIndex = handlers.Count - 1; handlerIndex >= 0; handlerIndex--)
                    {
                        var handler = handlers[handlerIndex] as Dictionary<string, object>;
                        if (IsAgentStatusLightHook(handler, executablePath))
                        {
                            handlers.RemoveAt(handlerIndex);
                            removedCount++;
                        }
                    }

                    if (handlers.Count == 0)
                    {
                        groups.RemoveAt(groupIndex);
                    }
                }

                if (groups.Count == 0)
                {
                    emptyEvents.Add(eventName);
                }
            }

            foreach (string eventName in emptyEvents)
            {
                hooks.Remove(eventName);
            }

            return removedCount;
        }

        private static bool IsAgentStatusLightHook(Dictionary<string, object> handler, string executablePath)
        {
            if (handler == null ||
                !String.Equals(ReadString(handler, "type"), "command", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string command = ReadString(handler, "command");
            if (String.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            if (String.Equals(command, executablePath, StringComparison.OrdinalIgnoreCase) &&
                HasClaudeHookArgument(handler))
            {
                return true;
            }

            return LooksLikeAgentStatusLightCommand(command) &&
                (HasClaudeHookArgument(handler) ||
                 command.IndexOf("--claude-hook", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool LooksLikeAgentStatusLightCommand(string command)
        {
            string trimmed = command.Trim().Trim('"');
            string fileName = String.Empty;

            try
            {
                fileName = Path.GetFileName(trimmed);
            }
            catch
            {
                fileName = String.Empty;
            }

            return String.Equals(fileName, "AgentStatusLight.exe", StringComparison.OrdinalIgnoreCase) ||
                command.IndexOf("AgentStatusLight.exe", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool EnsureEventHandler(Dictionary<string, object> hooks, string eventName, string executablePath)
        {
            string shellCommand = BuildHookCommand(executablePath);
            object existing;
            ArrayList groups;
            if (hooks.TryGetValue(eventName, out existing))
            {
                groups = ToArrayList(existing, "Claude hooks event '" + eventName + "'");
            }
            else
            {
                groups = new ArrayList();
            }

            hooks[eventName] = groups;
            bool changed = UpgradeCommandHooks(groups, shellCommand, executablePath);

            if (ContainsCommandHook(groups, executablePath))
            {
                return changed;
            }

            Dictionary<string, object> emptyMatcherGroup = FindEmptyMatcherGroup(groups);
            if (emptyMatcherGroup == null)
            {
                groups.Add(CreateHookGroup(executablePath));
                return true;
            }

            object hookListValue;
            ArrayList hookList = emptyMatcherGroup.TryGetValue("hooks", out hookListValue)
                ? ToArrayList(hookListValue, "Claude hooks handler list for '" + eventName + "'")
                : new ArrayList();
            emptyMatcherGroup["hooks"] = hookList;
            hookList.Add(CreateCommandHook(executablePath));
            return true;
        }

        private static ArrayList ToArrayList(object value, string description)
        {
            if (value == null)
            {
                return new ArrayList();
            }

            ArrayList list = value as ArrayList;
            if (list != null)
            {
                return list;
            }

            object[] array = value as object[];
            if (array != null)
            {
                return new ArrayList(array);
            }

            throw new InvalidOperationException(description + " must be an array.");
        }

        private static bool UpgradeCommandHooks(ArrayList groups, string shellCommand, string executablePath)
        {
            bool changed = false;
            foreach (object groupValue in groups)
            {
                var group = groupValue as Dictionary<string, object>;
                if (group == null) continue;

                object hooksValue;
                if (!group.TryGetValue("hooks", out hooksValue)) continue;

                ArrayList handlers = ToArrayList(hooksValue, "Claude hooks handler list");
                group["hooks"] = handlers;
                foreach (object handlerValue in handlers)
                {
                    var handler = handlerValue as Dictionary<string, object>;
                    if (handler == null) continue;

                    if (!String.Equals(ReadString(handler, "type"), "command", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (String.Equals(ReadString(handler, "command"), shellCommand, StringComparison.Ordinal))
                    {
                        handler["command"] = executablePath;
                        handler["args"] = new ArrayList { "--claude-hook" };
                        handler["async"] = true;
                        changed = true;
                    }
                    else if (String.Equals(ReadString(handler, "command"), executablePath, StringComparison.Ordinal) &&
                        HasClaudeHookArgument(handler) &&
                        !ReadBool(handler, "async"))
                    {
                        handler["async"] = true;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool ContainsCommandHook(ArrayList groups, string executablePath)
        {
            foreach (object groupValue in groups)
            {
                var group = groupValue as Dictionary<string, object>;
                if (group == null) continue;

                object hooksValue;
                if (!group.TryGetValue("hooks", out hooksValue)) continue;

                ArrayList handlers = ToArrayList(hooksValue, "Claude hooks handler list");
                foreach (object handlerValue in handlers)
                {
                    var handler = handlerValue as Dictionary<string, object>;
                    if (handler == null) continue;

                    if (String.Equals(ReadString(handler, "type"), "command", StringComparison.OrdinalIgnoreCase) &&
                        String.Equals(ReadString(handler, "command"), executablePath, StringComparison.Ordinal) &&
                        HasClaudeHookArgument(handler))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasClaudeHookArgument(Dictionary<string, object> handler)
        {
            object argsValue;
            if (!handler.TryGetValue("args", out argsValue))
            {
                return false;
            }

            ArrayList args;
            try
            {
                args = ToArrayList(argsValue, "Claude hooks command args");
            }
            catch
            {
                return false;
            }

            return args.Count == 1 &&
                String.Equals(Convert.ToString(args[0], CultureInfo.InvariantCulture), "--claude-hook", StringComparison.Ordinal);
        }

        private static Dictionary<string, object> FindEmptyMatcherGroup(ArrayList groups)
        {
            foreach (object groupValue in groups)
            {
                var group = groupValue as Dictionary<string, object>;
                if (group == null) continue;

                if (String.IsNullOrEmpty(ReadString(group, "matcher")))
                {
                    return group;
                }
            }

            return null;
        }

        private static Dictionary<string, object> CreateHookGroup(string executablePath)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "matcher", String.Empty },
                { "hooks", new ArrayList { CreateCommandHook(executablePath) } }
            };
        }

        private static Dictionary<string, object> CreateCommandHook(string executablePath)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "type", "command" },
                { "command", executablePath },
                { "args", new ArrayList { "--claude-hook" } },
                { "async", true }
            };
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
    }

    internal sealed class ClaudeHooksConfigurationResult
    {
        public ClaudeHooksConfigurationResult(string settingsPath, string backupPath, int addedEventCount, bool changed)
        {
            SettingsPath = settingsPath;
            BackupPath = backupPath;
            AddedEventCount = addedEventCount;
            Changed = changed;
        }

        public string SettingsPath { get; private set; }
        public string BackupPath { get; private set; }
        public int AddedEventCount { get; private set; }
        public bool Changed { get; private set; }
    }
}
