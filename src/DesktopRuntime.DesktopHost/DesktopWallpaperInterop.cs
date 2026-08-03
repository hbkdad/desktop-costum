using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopRuntime.DesktopHost;

/// <summary>
/// The supported shell interface for desktop wallpaper, which — unlike
/// <c>SystemParametersInfo</c> — can address individual monitors.
/// <para>
/// Documented and stable, so this is <b>not</b> in the same category as the undocumented
/// attachment technique in <see cref="WindowsAttachmentProbe"/>. It is still created
/// defensively, because a shell interface can fail to activate in unusual sessions.
/// </para>
/// <para>
/// Method order below is the vtable order and must not be rearranged.
/// </para>
/// </summary>
[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows")]
internal interface IDesktopWallpaper
{
    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId,
                      [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMonitorDevicePathAt(uint monitorIndex);

    uint GetMonitorDevicePathCount();

    // Remaining members are declared only to preserve vtable layout; they are not called.
    [PreserveSig] int GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId, out RECT displayRect);
    [PreserveSig] int SetBackgroundColor(uint color);
    [PreserveSig] int GetBackgroundColor(out uint color);
    [PreserveSig] int SetPosition(int position);
    [PreserveSig] int GetPosition(out int position);
    [PreserveSig] int SetSlideshow(IntPtr items);
    [PreserveSig] int GetSlideshow(out IntPtr items);
    [PreserveSig] int SetSlideshowOptions(int options, uint slideshowTick);
    [PreserveSig] int GetSlideshowOptions(out int options, out uint slideshowTick);
    [PreserveSig] int AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorId, int direction);
    [PreserveSig] int GetStatus(out int state);
    [PreserveSig] int Enable([MarshalAs(UnmanagedType.Bool)] bool enable);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { public int Left, Top, Right, Bottom; }
}

[SupportedOSPlatform("windows")]
internal static class DesktopWallpaperFactory
{
    private static readonly Guid ClassId = new("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");

    /// <summary>
    /// Creates the shell wallpaper object, or returns null if it is unavailable.
    /// <para>
    /// Returning null rather than throwing is deliberate: the caller has a working
    /// <c>SystemParametersInfo</c> fallback, so an unavailable interface should quietly
    /// reduce capability rather than break wallpaper entirely.
    /// </para>
    /// </summary>
    internal static IDesktopWallpaper? TryCreate()
    {
        try
        {
            Type? type = Type.GetTypeFromCLSID(ClassId);
            if (type is null)
            {
                return null;
            }

            return Activator.CreateInstance(type) as IDesktopWallpaper;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or NotSupportedException or PlatformNotSupportedException)
        {
            return null;
        }
    }
}
