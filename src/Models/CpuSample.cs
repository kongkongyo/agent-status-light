using System;

namespace WorkStatusLight
{
internal sealed class CpuSample
    {
        public CpuSample(DateTime time, double cpuSeconds)
        {
            Time = time;
            CpuSeconds = cpuSeconds;
        }

        public DateTime Time { get; private set; }
        public double CpuSeconds { get; private set; }
    }
}
