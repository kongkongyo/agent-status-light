using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WorkStatusLight
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Any(a => String.Equals(a, "--claude-hook", StringComparison.OrdinalIgnoreCase)))
            {
                Environment.ExitCode = ClaudeHookRecorder.RecordFromStandardInput();
                return;
            }

            bool createdNew;
            using (var mutex = new Mutex(true, "AgentStatusLight.SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    return;
                }

                Run(args);
                GC.KeepAlive(mutex);
            }
        }

        private static void Run(string[] args)
        {
            try
            {
                Logger.Write("Program starting");
                bool manual = args.Any(a => String.Equals(a, "--manual", StringComparison.OrdinalIgnoreCase));

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new StatusLightForm(!manual));
                Logger.Write("Program exiting normally");
            }
            catch (Exception ex)
            {
                Logger.Write("Fatal: " + ex);
                MessageBox.Show(ex.ToString(), "AgentStatusLight", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
