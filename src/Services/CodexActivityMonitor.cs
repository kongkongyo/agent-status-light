using System;
using System.Diagnostics;

namespace WorkStatusLight
{
    internal sealed class CodexActivityMonitor
    {
        private const double BusyPercent = 2.0;
        private const double AppServerBusyPercent = 3.0;
        private const double MinBusyAboveBaseline = 1.2;
        private const double MinAppAboveBaseline = 1.5;
        private const double StrongBusyPercent = 4.0;
        private const double StrongAppServerBusyPercent = 5.0;
        private const int BaselineWarmupSamples = 6;

        private CpuSample lastCpuSample;
        private CpuSample lastAppServerCpuSample;
        private int baselineSamples;
        private double totalCpuBaseline = 1.0;
        private double appServerCpuBaseline = 1.0;

        public double TotalCpuBaseline
        {
            get { return totalCpuBaseline; }
        }

        public double AppServerCpuBaseline
        {
            get { return appServerCpuBaseline; }
        }

        public ActivitySnapshot GetSnapshot()
        {
            double totalPercent = GetCodexCpuPercent();
            double appServerPercent = GetCpuPercentForProcesses(IsCodexAppServerProcess, ref lastAppServerCpuSample);

            bool baselineReady = baselineSamples >= BaselineWarmupSamples;
            bool totalBusy = totalPercent >= BusyPercent && (!baselineReady || totalPercent >= totalCpuBaseline + MinBusyAboveBaseline);
            bool appBusy = appServerPercent >= AppServerBusyPercent &&
                totalPercent >= 1.5 &&
                (!baselineReady || appServerPercent >= appServerCpuBaseline + MinAppAboveBaseline);
            bool busy = totalBusy || appBusy;
            bool strongBusy = totalPercent >= StrongBusyPercent ||
                (appServerPercent >= StrongAppServerBusyPercent && totalPercent >= BusyPercent);
            return new ActivitySnapshot(totalPercent, appServerPercent, busy, strongBusy);
        }

        public void UpdateBaseline(ActivitySnapshot activity, bool hasSeenBusy)
        {
            if (activity.IsBusy || hasSeenBusy)
            {
                return;
            }

            double alpha = baselineSamples < BaselineWarmupSamples ? 0.35 : 0.08;
            totalCpuBaseline = Smooth(totalCpuBaseline, activity.TotalCpuPercent, alpha);
            appServerCpuBaseline = Smooth(appServerCpuBaseline, activity.AppServerCpuPercent, alpha);
            baselineSamples++;
        }

        private double GetCodexCpuPercent()
        {
            return GetCpuPercentForProcesses(process =>
                IsCodexProcessName(process.ProcessName),
                ref lastCpuSample);
        }

        private static bool IsCodexProcessName(string processName)
        {
            return String.Equals(processName, "Codex", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(processName, "codex", StringComparison.OrdinalIgnoreCase);
        }

        private static double Smooth(double current, double next, double alpha)
        {
            if (Double.IsNaN(current) || Double.IsInfinity(current))
            {
                return next;
            }

            return current + ((next - current) * alpha);
        }

        private static bool IsCodexAppServerProcess(Process process)
        {
            try
            {
                return String.Equals(process.ProcessName, "codex", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private double GetCpuPercentForProcesses(Func<Process, bool> predicate, ref CpuSample sample)
        {
            DateTime now = DateTime.Now;
            double cpuSeconds = 0;

            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (predicate(process))
                    {
                        cpuSeconds += process.TotalProcessorTime.TotalSeconds;
                    }
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (sample == null)
            {
                sample = new CpuSample(now, cpuSeconds);
                return 0;
            }

            double elapsed = (now - sample.Time).TotalSeconds;
            if (elapsed <= 0) return 0;

            double delta = Math.Max(0, cpuSeconds - sample.CpuSeconds);
            sample = new CpuSample(now, cpuSeconds);
            return (delta / elapsed / Environment.ProcessorCount) * 100.0;
        }
    }
}
