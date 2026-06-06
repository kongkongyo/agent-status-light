using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace WorkStatusLight
{
    internal static class ClaudeEventsFile
    {
        public const int RecentLineLimit = 2000;
        private const long MaxBytes = 1024L * 1024L;
        private const string MutexName = "AgentStatusLight.ClaudeEventsFile";

        public static void AppendLine(string path, string line)
        {
            bool locked = false;
            using (var mutex = new Mutex(false, MutexName))
            {
                locked = TryWait(mutex);
                try
                {
                    AppendLineUnlocked(path, line);
                    if (locked)
                    {
                        TrimIfNeededUnlocked(path);
                    }
                }
                finally
                {
                    if (locked)
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
        }

        public static string[] ReadRecentLines(string path)
        {
            TrimIfNeeded(path);
            return ReadRecentLines(path, RecentLineLimit);
        }

        public static void TrimIfNeeded(string path)
        {
            if (!IsTooLarge(path))
            {
                return;
            }

            bool locked = false;
            using (var mutex = new Mutex(false, MutexName))
            {
                locked = TryWait(mutex);
                if (!locked)
                {
                    return;
                }

                try
                {
                    TrimIfNeededUnlocked(path);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        private static void AppendLineUnlocked(string path, string line)
        {
            string directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine(line);
            }
        }

        private static void TrimIfNeededUnlocked(string path)
        {
            try
            {
                if (!IsTooLarge(path))
                {
                    return;
                }

                string[] lines = ReadRecentLines(path, RecentLineLimit);
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    foreach (string line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }

                Logger.Write("Claude event file trimmed lines=" + lines.Length);
            }
            catch (Exception ex)
            {
                Logger.Write("Claude event file trim failed: " + ex.Message);
            }
        }

        private static bool IsTooLarge(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists && info.Length > MaxBytes;
            }
            catch
            {
                return false;
            }
        }

        private static string[] ReadRecentLines(string path, int limit)
        {
            var lines = new Queue<string>();

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (lines.Count >= limit)
                    {
                        lines.Dequeue();
                    }

                    lines.Enqueue(line);
                }
            }

            return lines.ToArray();
        }

        private static bool TryWait(Mutex mutex)
        {
            try
            {
                return mutex.WaitOne(TimeSpan.FromMilliseconds(300));
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
        }
    }
}
