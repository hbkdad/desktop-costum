// Phase 3, Prototype 2: Desktop overlay fallback.
//
// Purpose: prove a rendering path that does NOT depend on the undocumented
// WorkerW/Progman technique (Prototype 1 found that technique unreliable on
// the current build) — a plain, supported top-level window, click-through,
// non-activating, pushed to the bottom of the Z-order, covering the primary
// monitor. This is the non-negotiable "fallback overlay mode" required
// regardless of whether WorkerW attachment ever works reliably.
//
// This probe auto-closes after a few seconds and reports, programmatically
// (no screenshot / no human-in-the-loop needed), where it ended up in
// Z-order relative to Progman — the empirical pass/fail signal for whether
// this approach behaves like wallpaper (behind icons and behind real app
// windows) rather than obscuring the desktop.

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
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020; // click-through
    public const int WS_EX_NOACTIVATE = 0x08000000;  // never takes focus
    public const int WS_EX_TOOLWINDOW = 0x00000080;  // hidden from Alt+Tab
}

[SupportedOSPlatform("windows")]
static class Program
{
    [STAThread]
    static void Main()
    {
        Console.WriteLine("Phase 3 Prototype 2: desktop overlay fallback probe");

        ApplicationConfiguration.Initialize();

        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

        var form = new OverlayForm(bounds);
        form.Shown += (_, _) => ReportZOrder("immediately after show + SetWindowPos(Progman handle)", form.Handle);

        // Re-assert position once more after 1.5s (real wallpaper apps periodically
        // re-assert bottom position since other windows/Explorer can re-order things),
        // then report again, then auto-close after 4s. This is a probe, not a persistent app.
        var reassertTimer = new System.Windows.Forms.Timer { Interval = 1500 };
        reassertTimer.Tick += (_, _) =>
        {
            reassertTimer.Stop();
            form.RepositionBehindProgman();
            ReportZOrder("1.5s later, after re-asserting position", form.Handle);
        };
        reassertTimer.Start();

        var timer = new System.Windows.Forms.Timer { Interval = 4000 };
        timer.Tick += (_, _) =>
        {
            ReportZOrder("just before close", form.Handle);
            timer.Stop();
            form.Close();
        };
        timer.Start();

        Application.Run(form);

        Console.WriteLine();
        Console.WriteLine("Overlay closed. No window was left running.");
    }

    static void ReportZOrder(string label, IntPtr ourHandle)
    {
        IntPtr progman = NativeMethods.FindWindow("Progman", null);
        var order = new List<IntPtr>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            order.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        int progmanIndex = order.IndexOf(progman);
        int ourIndex = order.IndexOf(ourHandle);

        Console.WriteLine($"[{label}]");
        Console.WriteLine($"  Progman index in Z-order (0=topmost): {progmanIndex} of {order.Count}");
        Console.WriteLine($"  Overlay index in Z-order (0=topmost): {ourIndex} of {order.Count}");

        if (ourIndex < 0)
        {
            Console.WriteLine("  RESULT: overlay window not found in top-level enumeration (unexpected).");
        }
        else if (progmanIndex >= 0 && ourIndex > progmanIndex)
        {
            Console.WriteLine("  RESULT: overlay is BEHIND Progman — icons/desktop would render on top of it, like real wallpaper. GOOD.");
        }
        else if (progmanIndex >= 0)
        {
            Console.WriteLine("  RESULT: overlay is IN FRONT OF Progman — would obscure desktop icons. NOT acceptable as-is.");
        }
        else
        {
            Console.WriteLine("  RESULT: Progman not found at all (unexpected).");
        }
    }
}

[SupportedOSPlatform("windows")]
class OverlayForm : Form
{
    public OverlayForm(Rectangle bounds)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        ShowInTaskbar = false;
        TopMost = false;
        BackColor = Color.FromArgb(18, 32, 58); // placeholder "wallpaper" content
        DoubleBuffered = true;

        var label = new Label
        {
            Text = "Desktop Runtime — overlay fallback prototype (auto-closes in ~4s)",
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(40, 40),
            Font = new Font("Segoe UI", 14f)
        };
        Controls.Add(label);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Click-through + never-activates + hidden from Alt+Tab: behave like wallpaper,
            // not like a normal application window.
            cp.ExStyle |= NativeMethods.WS_EX_TRANSPARENT
                        | NativeMethods.WS_EX_NOACTIVATE
                        | NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RepositionBehindProgman();
    }

    public void RepositionBehindProgman()
    {
        // Spike A: use Progman's real handle as hWndInsertAfter instead of the
        // HWND_BOTTOM pseudo-value (Prototype 2's first pass) — SetWindowPos's
        // hWndInsertAfter places this window immediately behind the given handle.
        IntPtr progman = NativeMethods.FindWindow("Progman", null);
        IntPtr insertAfter = progman != IntPtr.Zero ? progman : NativeMethods.HWND_BOTTOM;
        NativeMethods.SetWindowPos(Handle, insertAfter, 0, 0, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE);
    }
}
