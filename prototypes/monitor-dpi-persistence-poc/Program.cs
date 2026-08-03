// Phase 3, Prototype 9: Monitor and DPI configuration persistence.
//
// Purpose: a workspace has to restore container/widget/wallpaper placement
// onto "the same monitor" after a reconnect, dock/undock, or resolution
// change (Job #1 in docs/product/jobs-to-be-done.md, and the acceptance bar
// for the Multi-Monitor Power User persona). That requires a monitor
// identity key that is STABLE across those events. This probe tests which
// available identifiers are actually stable-looking versus which are
// obviously positional/ordinal and therefore unsafe to persist.
//
// Key hypothesis under test: the GDI device name (\\.\DISPLAY1) and the
// HMONITOR handle are NOT stable identities — they are assigned by position
// and enumeration order — whereas the device interface path returned by
// EnumDisplayDevices with EDD_GET_DEVICE_INTERFACE_NAME embeds the monitor's
// hardware/EDID identity and is a far better persistence key.
//
// Read-only: this probe changes no display settings.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplayDevicesW(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    public const int MDT_EFFECTIVE_DPI = 0;
    public const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;
    public const uint MONITORINFOF_PRIMARY = 0x00000001;
    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAY_DEVICE
    {
        public uint cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }
}

[SupportedOSPlatform("windows")]
static class Program
{
    static int Main()
    {
        // Must be per-monitor DPI aware BEFORE querying, or Windows reports
        // virtualized (scaled) coordinates and the persistence data would be wrong.
        bool dpiAwareSet = NativeMethods.SetProcessDpiAwarenessContext(
            NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        Console.WriteLine("Phase 3 Prototype 9: monitor + DPI persistence probe");
        Console.WriteLine($"OS: {Environment.OSVersion.VersionString}");
        Console.WriteLine($"SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2): {dpiAwareSet}");
        Console.WriteLine();

        var monitors = new List<IntPtr>();
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (h, _, _, _) => { monitors.Add(h); return true; }, IntPtr.Zero);

        Console.WriteLine($"Monitors found: {monitors.Count}");
        Console.WriteLine();

        int index = 0;
        foreach (var hMonitor in monitors)
        {
            index++;
            var info = new NativeMethods.MONITORINFOEX
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
            };

            if (!NativeMethods.GetMonitorInfoW(hMonitor, ref info))
            {
                Console.WriteLine($"[Monitor {index}] GetMonitorInfo FAILED for handle 0x{hMonitor:X}");
                continue;
            }

            bool isPrimary = (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0;
            int width = info.rcMonitor.Right - info.rcMonitor.Left;
            int height = info.rcMonitor.Bottom - info.rcMonitor.Top;

            Console.WriteLine($"[Monitor {index}]{(isPrimary ? "  (PRIMARY)" : "")}");
            Console.WriteLine($"  HMONITOR handle      : 0x{hMonitor:X}          <- NOT stable, runtime handle only");
            Console.WriteLine($"  GDI device name      : {info.szDevice}         <- NOT stable, positional/ordinal");
            Console.WriteLine($"  Bounds               : {width}x{height} at ({info.rcMonitor.Left},{info.rcMonitor.Top})");
            Console.WriteLine($"  Work area            : {info.rcWork.Right - info.rcWork.Left}x{info.rcWork.Bottom - info.rcWork.Top}");

            int hr = NativeMethods.GetDpiForMonitor(hMonitor, NativeMethods.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
            if (hr == 0)
            {
                double scalePercent = dpiX / 96.0 * 100.0;
                Console.WriteLine($"  Effective DPI        : {dpiX}x{dpiY}  (scale {scalePercent:0}%)");
            }
            else
            {
                Console.WriteLine($"  Effective DPI        : GetDpiForMonitor failed, hr=0x{hr:X}");
            }

            // The candidate stable identity: device interface path (embeds hardware/EDID id).
            var dd = new NativeMethods.DISPLAY_DEVICE { cb = (uint)Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };
            if (NativeMethods.EnumDisplayDevicesW(info.szDevice, 0, ref dd, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME))
            {
                Console.WriteLine($"  Monitor friendly name: {dd.DeviceString}");
                Console.WriteLine($"  Device interface path: {dd.DeviceID}");
                Console.WriteLine($"                         ^- CANDIDATE STABLE KEY (embeds hardware id)");
            }
            else
            {
                Console.WriteLine("  Device interface path: EnumDisplayDevices failed (no stable key available for this monitor)");
            }

            Console.WriteLine();
        }

        Console.WriteLine("RESULT: see REPORT.md — this probe records which identifiers exist and which are");
        Console.WriteLine("structurally safe to persist. Confirming true reconnect-stability requires physically");
        Console.WriteLine("disconnecting/reconnecting a monitor and re-running, which this probe does not automate.");

        return monitors.Count > 0 ? 0 : 1;
    }
}
