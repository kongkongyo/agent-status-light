namespace WorkStatusLight
{
internal sealed class ActivitySnapshot
    {
        public ActivitySnapshot(double totalCpuPercent, double appServerCpuPercent, bool isBusy, bool isStrongBusy)
        {
            TotalCpuPercent = totalCpuPercent;
            AppServerCpuPercent = appServerCpuPercent;
            IsBusy = isBusy;
            IsStrongBusy = isStrongBusy;
        }

        public double TotalCpuPercent { get; private set; }
        public double AppServerCpuPercent { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsStrongBusy { get; private set; }
    }
}
