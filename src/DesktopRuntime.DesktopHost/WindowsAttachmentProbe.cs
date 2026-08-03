using System.Runtime.Versioning;
using DesktopRuntime.Core.Hosting;

namespace DesktopRuntime.DesktopHost;

/// <summary>
/// Asks, at runtime, whether the Tier 1 attachment surface from ADR-0003 is usable.
/// <para>
/// This is the one place in the product that touches undocumented shell behaviour, and it
/// is deliberately confined to <b>asking a question</b> — it never attaches, reparents or
/// draws. Phase 3 Prototype 1 found the technique unreliable on current Windows 11
/// builds, so the whole design treats a <c>true</c> answer as a bonus and a <c>false</c>
/// answer as the ordinary case.
/// </para>
/// <para>
/// Because the underlying technique is undocumented, any failure — including an
/// unexpected exception — is reported as "unavailable" rather than propagated. A probe
/// that throws would turn an opportunistic optimisation into an outage.
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsAttachmentProbe : IDesktopAttachmentProbe
{
    private const int SendMessageTimeoutMs = 1000;

    public bool IsAttachmentSurfaceAvailable()
    {
        try
        {
            IntPtr progman = NativeMethods.FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                return false;
            }

            // Ask Explorer to spawn the attachment window. Undocumented; may do nothing.
            NativeMethods.SendMessageTimeout(
                progman, NativeMethods.WM_SPAWN_WORKER, IntPtr.Zero, IntPtr.Zero,
                NativeMethods.SMTO_NORMAL, SendMessageTimeoutMs, out _);

            IntPtr candidate = IntPtr.Zero;

            // Find the window hosting the desktop icons, then look for a WorkerW sibling
            // after it in Z-order — the conventional attachment point.
            NativeMethods.EnumWindows((hWnd, _) =>
            {
                if (NativeMethods.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                {
                    candidate = NativeMethods.FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                }

                return true;
            }, IntPtr.Zero);

            return candidate != IntPtr.Zero;
        }
        catch (Exception)
        {
            // Undocumented behaviour must never take the application down with it.
            return false;
        }
    }
}
