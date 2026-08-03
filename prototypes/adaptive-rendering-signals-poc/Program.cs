// Phase 3, Prototypes 10 + 11: adaptive rendering signals.
//
// Covers two backlog items together because they are the two inputs to the
// same decision — "should the wallpaper/widget renderer pause or degrade
// right now?":
//   Prototype 10: fullscreen detection (pause during games/presentations)
//   Prototype 11: power state (degrade on battery / battery saver)
//
// Both are the difference between clearing the bar every competitor already
// clears (a fullscreen-pause checkbox) and the resource-discipline gap
// ranked #2 in docs/research/market-gap-report.md. Every animated-wallpaper
// competitor researched draws battery/GPU complaints, so these signals need
// to be proven cheap and reliable, not assumed.
//
// Read-only: this probe changes no system state. It samples for a few
// seconds so a state change (e.g. alt-tabbing to a fullscreen window) would
// be visible in the output if it happened during the run.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

[SupportedOSPlatform("windows")]
static class NativeMethods
{
    // --- Prototype 10: fullscreen detection ---

    [DllImport("shell32.dll")]
    public static extern int SHQueryUserNotificationState(out int pquns);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    // --- Prototype 11: power state ---

    [DllImport("kernel32.dll")]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public uint length;
        public uint flags;
        public uint showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    public const uint SW_SHOWMAXIMIZED = 3;

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;        // 0=offline(battery), 1=online(AC), 255=unknown
        public byte BatteryFlag;         // 1=high,2=low,4=critical,8=charging,128=no battery,255=unknown
        public byte BatteryLifePercent;  // 0-100, 255=unknown
        public byte SystemStatusFlag;    // 1 = battery saver ON
        public uint BatteryLifeTime;     // seconds remaining, 0xFFFFFFFF = unknown
        public uint BatteryFullLifeTime;
    }
}

[SupportedOSPlatform("windows")]
static class Program
{
    static int Main()
    {
        NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        Console.WriteLine("Phase 3 Prototypes 10+11: adaptive rendering signals probe");
        Console.WriteLine($"OS: {Environment.OSVersion.VersionString}");
        Console.WriteLine();

        Console.WriteLine("=== Prototype 11: power state (GetSystemPowerStatus) ===");
        ReportPowerState();
        Console.WriteLine();

        Console.WriteLine("=== Prototype 10: fullscreen detection ===");
        Console.WriteLine("Sampling 5 times over ~5s (state changes during the run would show here):");
        Console.WriteLine();

        var costs = new List<double>();
        for (int i = 1; i <= 5; i++)
        {
            var sw = Stopwatch.StartNew();
            var (notificationState, stateName) = QueryNotificationState();
            var (rectSaysFullscreen, detail) = ForegroundWindowCoversMonitor();
            sw.Stop();
            costs.Add(sw.Elapsed.TotalMilliseconds);

            bool shouldPause = notificationState == 3 /* QUNS_RUNNING_D3D_FULL_SCREEN */
                            || notificationState == 4 /* QUNS_PRESENTATION_MODE */
                            || rectSaysFullscreen;

            Console.WriteLine($"  [{i}] SHQueryUserNotificationState = {notificationState} ({stateName})");
            Console.WriteLine($"      foreground-covers-monitor    = {rectSaysFullscreen}  [{detail}]");
            Console.WriteLine($"      => renderer should pause     : {shouldPause}");
            Console.WriteLine($"      sample cost                  : {sw.Elapsed.TotalMilliseconds:0.00} ms");

            if (i < 5) Thread.Sleep(1000);
        }

        Console.WriteLine();
        Console.WriteLine($"Average cost of one combined fullscreen check: {Average(costs):0.00} ms");
        Console.WriteLine("(Relevant because polling this signal must be far cheaper than the rendering it gates.)");

        return 0;
    }

    static double Average(List<double> values)
    {
        double total = 0;
        foreach (var v in values) total += v;
        return values.Count > 0 ? total / values.Count : 0;
    }

    static void ReportPowerState()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            Console.WriteLine("  GetSystemPowerStatus FAILED.");
            return;
        }

        string ac = status.ACLineStatus switch
        {
            0 => "on battery",
            1 => "plugged in (AC)",
            _ => "unknown"
        };

        bool noBattery = (status.BatteryFlag & 128) != 0;
        bool charging = (status.BatteryFlag & 8) != 0;
        bool batterySaver = status.SystemStatusFlag == 1;

        Console.WriteLine($"  AC line status       : {status.ACLineStatus} ({ac})");
        Console.WriteLine($"  Battery present      : {!noBattery}");
        Console.WriteLine($"  Charging             : {charging}");
        Console.WriteLine($"  Battery life percent : {(status.BatteryLifePercent == 255 ? "unknown" : status.BatteryLifePercent + "%")}");
        Console.WriteLine($"  Battery saver ON     : {batterySaver}   <- the key signal for degrading render quality");

        bool shouldDegrade = status.ACLineStatus == 0 || batterySaver;
        Console.WriteLine($"  => renderer should degrade: {shouldDegrade}");
        if (noBattery)
        {
            Console.WriteLine("  NOTE: no battery detected on this machine (desktop/VM) — the on-battery");
            Console.WriteLine("        degradation path CANNOT be validated here, only the API plumbing.");
        }
    }

    static (int state, string name) QueryNotificationState()
    {
        int hr = NativeMethods.SHQueryUserNotificationState(out int quns);
        if (hr != 0) return (-1, $"query failed hr=0x{hr:X}");

        string name = quns switch
        {
            1 => "QUNS_NOT_PRESENT",
            2 => "QUNS_BUSY",
            3 => "QUNS_RUNNING_D3D_FULL_SCREEN",
            4 => "QUNS_PRESENTATION_MODE",
            5 => "QUNS_ACCEPTS_NOTIFICATIONS",
            6 => "QUNS_QUIET_TIME",
            7 => "QUNS_APP",
            _ => "unknown"
        };
        return (quns, name);
    }

    static (bool isFullscreen, string detail) ForegroundWindowCoversMonitor()
    {
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return (false, "no foreground window");

        var cls = new StringBuilder(256);
        NativeMethods.GetClassNameW(hwnd, cls, cls.Capacity);
        string className = cls.ToString();

        // The desktop/shell is not a "fullscreen app" — never pause because of it.
        if (className is "Progman" or "WorkerW" or "Shell_TrayWnd")
            return (false, $"shell window ({className}), ignored");

        string procName = "?";
        try
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            procName = Process.GetProcessById((int)pid).ProcessName;
        }
        catch { /* process may exit between calls; not fatal for a probe */ }

        if (!NativeMethods.GetWindowRect(hwnd, out var wr))
            return (false, "GetWindowRect failed");

        IntPtr hMon = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var mi = new NativeMethods.MONITORINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfoW(hMon, ref mi))
            return (false, "GetMonitorInfo failed");

        bool covers = wr.Left <= mi.rcMonitor.Left && wr.Top <= mi.rcMonitor.Top
                   && wr.Right >= mi.rcMonitor.Right && wr.Bottom >= mi.rcMonitor.Bottom;

        // A MAXIMIZED window also covers the whole monitor rect — especially when the
        // taskbar is auto-hidden and the work area equals the monitor bounds. Treating
        // that as "fullscreen" is a false positive that would pause the renderer during
        // ordinary desktop use (first run of this probe did exactly that on a maximized
        // browser window). Borderless-fullscreen games are normally NOT maximized, so
        // excluding maximized windows removes the false positive while still catching them.
        var wp = new NativeMethods.WINDOWPLACEMENT { length = (uint)Marshal.SizeOf<NativeMethods.WINDOWPLACEMENT>() };
        bool isMaximized = NativeMethods.GetWindowPlacement(hwnd, ref wp)
                        && wp.showCmd == NativeMethods.SW_SHOWMAXIMIZED;

        if (covers && isMaximized)
            return (false, $"{procName} ({className}) — maximized, not fullscreen");

        return (covers, $"{procName} ({className})");
    }
}
