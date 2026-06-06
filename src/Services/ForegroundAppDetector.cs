using System;
using System.Diagnostics;

namespace WorkStatusLight
{
    internal sealed class ForegroundAppDetector
    {
        public ForegroundSnapshot GetSnapshot()
        {
            IntPtr foregroundHandle = NativeMethods.GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero)
            {
                return new ForegroundSnapshot(null, false);
            }

            uint processId;
            NativeMethods.GetWindowThreadProcessId(foregroundHandle, out processId);

            try
            {
                using (Process process = Process.GetProcessById((int)processId))
                using (Process current = Process.GetCurrentProcess())
                {
                    string name = process.ProcessName;
                    bool isOurLight = process.Id == current.Id;
                    bool isCodex = String.Equals(name, "Codex", StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(name, "codex", StringComparison.OrdinalIgnoreCase);
                    return new ForegroundSnapshot(name, !isCodex && !isOurLight);
                }
            }
            catch
            {
                return new ForegroundSnapshot(null, false);
            }
        }
    }
}
