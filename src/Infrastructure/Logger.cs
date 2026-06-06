using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace WorkStatusLight
{

    internal static class Logger
    {
        private const long MaxLogBytes = 512L * 1024L;
        private static readonly string PathValue = Path.Combine(Paths.AppDirectory, "logs", "agent-status-light.log");
        private static readonly string BackupPathValue = PathValue + ".1";
        private static readonly object SyncRoot = new object();

        public static void Write(string message)
        {
            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(PathValue));
                    RotateIfNeeded();
                    using (var stream = new FileStream(PathValue, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.WriteLine(DateTime.Now.ToString("s", CultureInfo.InvariantCulture) + " " + message);
                    }
                }
            }
            catch
            {
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                var info = new FileInfo(PathValue);
                if (!info.Exists || info.Length < MaxLogBytes)
                {
                    return;
                }

                if (File.Exists(BackupPathValue))
                {
                    File.Delete(BackupPathValue);
                }

                File.Move(PathValue, BackupPathValue);
            }
            catch
            {
            }
        }
    }
}
