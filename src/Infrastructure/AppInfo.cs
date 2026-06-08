using System;
using System.Reflection;

namespace WorkStatusLight
{
    internal static class AppInfo
    {
        public const string RepositoryUrl = "https://github.com/kongkongyo/agent-status-light";
        public const string LatestReleaseApiUrl = "https://api.github.com/repos/kongkongyo/agent-status-light/releases/latest";

        public static string Version
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                if (attributes.Length > 0)
                {
                    var versionAttribute = attributes[0] as AssemblyInformationalVersionAttribute;
                    if (versionAttribute != null && !String.IsNullOrWhiteSpace(versionAttribute.InformationalVersion))
                    {
                        return versionAttribute.InformationalVersion;
                    }
                }

                Version version = assembly.GetName().Version;
                if (version == null)
                {
                    return "1.0.0";
                }

                return version.Major + "." + version.Minor + "." + version.Build;
            }
        }
    }
}
