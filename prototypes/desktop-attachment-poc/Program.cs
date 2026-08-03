// Phase 3, Prototype 1: Desktop attachment feasibility.
//
// Purpose: prove whether the well-known (undocumented) Progman/WorkerW technique
// can reliably locate the desktop-icon-layer sibling window on this Windows build,
// as a read-only probe — this program does NOT attach, reparent, or draw into
// anything. It only proves the handle can be found, and reports findings in the
// format required by the `desktop-host-prototype` skill.
//
// Technique: send the undocumented 0x052C message to Progman, which causes
// explorer.exe to spawn an extra top-level WorkerW window as a sibling of the
// WorkerW that hosts SHELLDLL_DefView (the desktop icons). That extra WorkerW is
// the conventional attachment point used by Wallpaper Engine, Lively Wallpaper,
// and similar tools. This adapter must never be a hard dependency — see the
// mandatory overlay fallback (Prototype 2) in backlog/prototype-backlog.md.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

[SupportedOSPlatform("windows")]
static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public const uint SMTO_NORMAL = 0x0000;
}

[SupportedOSPlatform("windows")]
static class Program
{
    static int Main()
    {
        Console.WriteLine("Phase 3 Prototype 1: desktop attachment feasibility probe");
        Console.WriteLine($"OS: {Environment.OSVersion.VersionString}");
        Console.WriteLine();

        IntPtr progman = NativeMethods.FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            Console.WriteLine("RESULT: FAIL — could not find Progman window.");
            return 1;
        }
        Console.WriteLine($"Found Progman: 0x{progman:X}");

        // Undocumented message that causes explorer.exe to spawn the extra WorkerW.
        NativeMethods.SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
            NativeMethods.SMTO_NORMAL, 1000, out _);

        IntPtr workerWForIcons = IntPtr.Zero;
        IntPtr targetWorkerW = IntPtr.Zero;

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            IntPtr shellDefView = NativeMethods.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellDefView != IntPtr.Zero)
            {
                workerWForIcons = hWnd;
                // Search top-level windows (hwndParent = NULL) for the next window of
                // class "WorkerW" appearing after `hWnd` in Z-order — more robust than
                // GetWindow(GW_HWNDNEXT), which only checks the single immediate sibling
                // and can miss the target if another window sits between them.
                targetWorkerW = NativeMethods.FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
            }
            return true; // keep enumerating
        }, IntPtr.Zero);

        Console.WriteLine(workerWForIcons != IntPtr.Zero
            ? $"Found icon-hosting window (parent of SHELLDLL_DefView): 0x{workerWForIcons:X}"
            : "Did not find a window hosting SHELLDLL_DefView.");

        if (targetWorkerW == IntPtr.Zero)
        {
            Console.WriteLine("RESULT: FAIL — target WorkerW sibling not found. Fallback (overlay) mode is required on this build.");
            Console.WriteLine();
            Console.WriteLine("Diagnostic — all top-level windows with a class name containing \"Worker\" or \"Progman\":");
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                var cls = new StringBuilder(256);
                NativeMethods.GetClassName(hWnd, cls, cls.Capacity);
                var name = cls.ToString();
                if (name.Contains("Worker", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Progman", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  0x{hWnd:X}  class=\"{name}\"");
                }
                return true;
            }, IntPtr.Zero);
            return 1;
        }

        var className = new StringBuilder(256);
        NativeMethods.GetClassName(targetWorkerW, className, className.Capacity);

        Console.WriteLine($"Found candidate attach-target window: 0x{targetWorkerW:X}, class=\"{className}\"");
        Console.WriteLine(className.ToString() == "WorkerW"
            ? "RESULT: PASS — target window class matches expected \"WorkerW\"."
            : "RESULT: PARTIAL — window found but class name did not match \"WorkerW\"; technique may have changed on this build.");

        Console.WriteLine();
        Console.WriteLine("No window was reparented or drawn into — this probe is read-only by design.");
        return 0;
    }
}
