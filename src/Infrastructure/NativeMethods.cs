using System;
using System.Runtime.InteropServices;

namespace WorkStatusLight
{

    internal static class NativeMethods
    {
        public const int WsExLayered = 0x00080000;
        public const byte AcSrcOver = 0x00;
        public const byte AcSrcAlpha = 0x01;
        public const UInt32 UlwAlpha = 0x00000002;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static readonly IntPtr HwndTopMost = new IntPtr(-1);
        public const UInt32 SwpNoActivate = 0x0010;
        public const UInt32 SwpShowWindow = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, UInt32 flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDc);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hDc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hDcDst, ref NativePoint pptDst, ref NativeSize psize, IntPtr hDcSrc, ref NativePoint pptSrc, int crKey, ref BlendFunction pblend, UInt32 dwFlags);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        public NativePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeSize
    {
        public NativeSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MinMaxInfo
    {
        public NativePoint ptReserved;
        public NativePoint ptMaxSize;
        public NativePoint ptMaxPosition;
        public NativePoint ptMinTrackSize;
        public NativePoint ptMaxTrackSize;
    }
}
