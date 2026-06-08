using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

namespace WorkStatusLight
{
    internal sealed class UpdateCheckResult
    {
        public UpdateCheckResult(bool hasUpdate, string latestVersion)
        {
            HasUpdate = hasUpdate;
            LatestVersion = latestVersion ?? String.Empty;
        }

        public bool HasUpdate { get; private set; }
        public string LatestVersion { get; private set; }
    }

    internal static class UpdateChecker
    {
        public static UpdateCheckResult CheckLatestRelease()
        {
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | (SecurityProtocolType)3072;

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = T.AppTitle;
                client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                string json = client.DownloadString(AppInfo.LatestReleaseApiUrl);
                var serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<Dictionary<string, object>>(json);

                string tagName = ReadString(data, "tag_name");
                bool hasUpdate = IsNewerVersion(tagName, AppInfo.Version);
                return new UpdateCheckResult(hasUpdate, tagName);
            }
        }

        private static string ReadString(Dictionary<string, object> data, string key)
        {
            object value;
            if (data != null && data.TryGetValue(key, out value) && value != null)
            {
                return Convert.ToString(value);
            }

            return String.Empty;
        }

        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            int latestDateVersion;
            int currentDateVersion;
            if (TryReadDateVersion(latestVersion, out latestDateVersion) &&
                TryReadDateVersion(currentVersion, out currentDateVersion))
            {
                return latestDateVersion > currentDateVersion;
            }

            int[] latestParts;
            int[] currentParts;
            if (!TryReadVersionParts(latestVersion, out latestParts) || !TryReadVersionParts(currentVersion, out currentParts))
            {
                return false;
            }

            for (int i = 0; i < latestParts.Length; i++)
            {
                if (latestParts[i] > currentParts[i])
                {
                    return true;
                }
                if (latestParts[i] < currentParts[i])
                {
                    return false;
                }
            }

            return false;
        }

        private static bool TryReadDateVersion(string value, out int version)
        {
            version = 0;
            value = (value ?? String.Empty).Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(1);
            }

            if (value.Length != 6)
            {
                return false;
            }

            foreach (char c in value)
            {
                if (!Char.IsDigit(c))
                {
                    return false;
                }
            }

            int year;
            int month;
            int day;
            if (!Int32.TryParse(value.Substring(0, 2), out year) ||
                !Int32.TryParse(value.Substring(2, 2), out month) ||
                !Int32.TryParse(value.Substring(4, 2), out day))
            {
                return false;
            }

            if (month < 1 || month > 12 || day < 1 || day > 31)
            {
                return false;
            }

            return Int32.TryParse(value, out version);
        }

        private static bool TryReadVersionParts(string value, out int[] parts)
        {
            parts = new int[] { 0, 0, 0, 0 };
            value = (value ?? String.Empty).Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(1);
            }

            int suffixIndex = value.IndexOfAny(new char[] { '-', '+', ' ' });
            if (suffixIndex >= 0)
            {
                value = value.Substring(0, suffixIndex);
            }

            string[] segments = value.Split('.');
            if (segments.Length == 0 || segments.Length > parts.Length)
            {
                return false;
            }

            for (int i = 0; i < segments.Length; i++)
            {
                int part;
                if (!Int32.TryParse(segments[i], out part) || part < 0)
                {
                    return false;
                }
                parts[i] = part;
            }

            return true;
        }
    }
}
