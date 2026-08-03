// Phase 3, Prototype 3: Explorer restart recovery.
//
// Purpose: prove (a) that an explorer.exe restart can be detected
// programmatically, (b) that window handles obtained before the restart
// (Progman, and by extension any Tier-1 WorkerW attachment per ADR-0003)
// become stale/invalid and must be re-acquired afterward, and (c) measure
// how long recovery takes, so the recovery/adapter design in the desktop
// host module has real numbers instead of an assumption.
//
// This probe deliberately restarts explorer.exe (Stop-Process + relaunch)
// on the machine it runs on. That is a common, fully-reversible Windows
// troubleshooting action (no data loss), but it is more disruptive than the
// earlier prototypes: the taskbar/desktop disappear briefly, and any open
// File Explorer (CabinetWClass) windows are closed and do NOT reopen
// automatically — this probe checks for open Explorer windows first and
// reports the before/after count so that's visible in the results rather
// than silently lost. Downtime is minimized by relaunching explorer.exe
// immediately after stopping it rather than waiting for any OS auto-restart.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hWnd);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}

[SupportedOSPlatform("windows")]
static class Program
{
    static int Main()
    {
        Console.WriteLine("Phase 3 Prototype 3: Explorer restart recovery probe");
        Console.WriteLine();

        IntPtr progmanBefore = NativeMethods.FindWindow("Progman", null);
        Console.WriteLine($"Progman handle BEFORE restart: 0x{progmanBefore:X}");

        int cabinetWindowsBefore = CountWindowsOfClass("CabinetWClass");
        Console.WriteLine($"Open File Explorer (CabinetWClass) windows BEFORE restart: {cabinetWindowsBefore}");

        var explorerProcsBefore = Process.GetProcessesByName("explorer");
        Console.WriteLine($"explorer.exe process count BEFORE: {explorerProcsBefore.Length}, PID(s): {string.Join(", ", Array.ConvertAll(explorerProcsBefore, p => p.Id))}");

        Console.WriteLine();
        Console.WriteLine("Restarting explorer.exe now...");
        var sw = Stopwatch.StartNew();

        foreach (var proc in explorerProcsBefore)
        {
            try { proc.Kill(); } catch (Exception ex) { Console.WriteLine($"  (kill error, continuing: {ex.Message})"); }
        }

        // Relaunch immediately rather than waiting for any OS auto-restart, to
        // minimize how long the user's taskbar/desktop is gone.
        Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });

        IntPtr progmanAfter = IntPtr.Zero;
        int timeoutMs = 15000;
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            progmanAfter = NativeMethods.FindWindow("Progman", null);
            if (progmanAfter != IntPtr.Zero) break;
            Thread.Sleep(200);
        }
        sw.Stop();

        Console.WriteLine();
        Console.WriteLine($"Progman handle AFTER restart:  0x{progmanAfter:X}");
        Console.WriteLine($"Time to new Progman window appearing: {sw.ElapsedMilliseconds} ms");

        bool handleChanged = progmanAfter != IntPtr.Zero && progmanAfter != progmanBefore;
        Console.WriteLine(handleChanged
            ? "RESULT: Progman handle CHANGED after restart, as expected — any code holding the old handle (or an old Tier-1 WorkerW attachment) must detect this and re-acquire, not assume the handle stays valid."
            : "RESULT: unexpected — handle did not change or new Progman was not found within timeout.");

        bool oldHandleStillValid = progmanBefore != IntPtr.Zero && NativeMethods.IsWindow(progmanBefore);
        Console.WriteLine($"Old Progman handle still valid (IsWindow) after restart: {oldHandleStillValid} (expected: false)");

        // Give the new shell a moment to finish initializing before the final counts.
        Thread.Sleep(2000);
        int cabinetWindowsAfter = CountWindowsOfClass("CabinetWClass");
        Console.WriteLine();
        Console.WriteLine($"Open File Explorer (CabinetWClass) windows AFTER restart: {cabinetWindowsAfter} (expected 0 regardless of the BEFORE count — Explorer windows do not auto-reopen)");

        var explorerProcsAfter = Process.GetProcessesByName("explorer");
        Console.WriteLine($"explorer.exe process count AFTER: {explorerProcsAfter.Length}, PID(s): {string.Join(", ", Array.ConvertAll(explorerProcsAfter, p => p.Id))}");

        Console.WriteLine();
        Console.WriteLine("Probe complete. Desktop/taskbar/icons should be fully restored by explorer.exe's own startup.");

        return handleChanged && !oldHandleStillValid ? 0 : 1;
    }

    static int CountWindowsOfClass(string className)
    {
        int count = 0;
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            var sb = new System.Text.StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString() == className) count++;
            return true;
        }, IntPtr.Zero);
        return count;
    }

    [DllImport("user32.dll")]
    static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
}
