using System;
using System.IO;

namespace WorkStatusLight
{

    internal static class Paths
    {
        public static readonly string AppDirectory = GetAppDirectory();

        private static string GetAppDirectory()
        {
            return new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).FullName;
        }
    }
}
