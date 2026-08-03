using DesktopRuntime.Core.Workspaces;

namespace DesktopRuntime.Core.Wallpapers;

/// <summary>
/// The rendering tiers from ADR-0003, in order of preference.
/// <para>
/// Phase 3 prototypes found that rendering behind desktop icons is not reliably
/// achievable on current Windows 11 builds: WorkerW attachment is opportunistic, and
/// z-order overlay placement is structurally impossible. Only the static wallpaper API
/// is guaranteed.
/// </para>
/// </summary>
public enum WallpaperTier
{
    /// <summary>
    /// Content rendered into a WorkerW window behind the desktop icons. Supports
    /// animation. Opportunistic: unavailable on builds where attachment fails.
    /// </summary>
    AttachedSurface,

    /// <summary>
    /// The image set through the supported OS wallpaper API. Always available, but
    /// static only.
    /// </summary>
    StaticImage
}

/// <summary>What the runtime can currently do on a given machine.</summary>
/// <param name="AttachedSurfaceAvailable">
/// Whether a usable attachment surface was detected. Determined at runtime by the
/// desktop host, never assumed.
/// </param>
public readonly record struct WallpaperHostCapabilities(bool AttachedSurfaceAvailable);

/// <summary>
/// The tier chosen for a requested wallpaper, and whether that constitutes a
/// degradation the user must be told about.
/// </summary>
public sealed class WallpaperTierDecision
{
    internal WallpaperTierDecision(
        WallpaperKind requestedKind,
        WallpaperTier selectedTier,
        bool isDegraded,
        string? degradationReason)
    {
        RequestedKind = requestedKind;
        SelectedTier = selectedTier;
        IsDegraded = isDegraded;
        DegradationReason = degradationReason;
    }

    public WallpaperKind RequestedKind { get; }

    public WallpaperTier SelectedTier { get; }

    /// <summary>
    /// True when the user asked for something this machine cannot deliver. PRD §13.7
    /// requires this to be surfaced, not applied silently.
    /// </summary>
    public bool IsDegraded { get; }

    /// <summary>Plain-language reason, non-null exactly when <see cref="IsDegraded"/> is true.</summary>
    public string? DegradationReason { get; }
}

/// <summary>
/// Chooses the rendering tier for a requested wallpaper. This makes ADR-0003 executable
/// rather than merely documented — the fallback chain and the "degrade visibly, never
/// silently" rule are enforced here and covered by tests.
/// </summary>
public static class WallpaperTierResolver
{
    public static WallpaperTierDecision Resolve(WallpaperAssignment assignment, WallpaperHostCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return assignment.Kind switch
        {
            // Static content is served best by the OS wallpaper API regardless of whether
            // an attachment surface exists: it is the more robust path and costs nothing
            // to render. Using an attached surface for a still image would burn a scarce,
            // fragile resource for no benefit.
            WallpaperKind.Static => new WallpaperTierDecision(
                assignment.Kind, WallpaperTier.StaticImage, isDegraded: false, degradationReason: null),

            WallpaperKind.Video when capabilities.AttachedSurfaceAvailable => new WallpaperTierDecision(
                assignment.Kind, WallpaperTier.AttachedSurface, isDegraded: false, degradationReason: null),

            WallpaperKind.Video => new WallpaperTierDecision(
                assignment.Kind,
                WallpaperTier.StaticImage,
                isDegraded: true,
                degradationReason:
                    "Animated wallpaper is not available on this system because the desktop " +
                    "rendering surface could not be attached. A still image is being shown instead."),

            // A new WallpaperKind must make an explicit decision here. Falling back to a
            // silent default would be exactly the silent degradation this type prevents.
            _ => throw new NotSupportedException(
                $"No tier decision is defined for wallpaper kind '{assignment.Kind}'.")
        };
    }
}
