using System;

namespace WorkStatusLight
{
internal sealed class ForegroundSnapshot
    {
        public ForegroundSnapshot(string processName, bool isExternalApp)
        {
            ProcessName = String.IsNullOrWhiteSpace(processName) ? "external app" : processName;
            IsExternalApp = isExternalApp;
        }

        public string ProcessName { get; private set; }
        public bool IsExternalApp { get; private set; }
    }
}
