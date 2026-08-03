using DesktopRuntime.Core.Hosting;
using DesktopRuntime.Core.Wallpapers;

namespace DesktopRuntime.Core.Workspaces;

/// <summary>
/// Applies a saved workspace to the live desktop.
/// <para>
/// This is where the tested pieces meet: <see cref="WorkspaceResolver"/> decides what
/// belongs on the monitors that are actually attached, <see cref="WallpaperTierResolver"/>
/// decides how each wallpaper can be rendered, and the <see cref="IWallpaperSurface"/>
/// applies it.
/// </para>
/// <para>
/// Activation is deliberately <b>best-effort and fully reported</b>. A missing wallpaper
/// file on one monitor must not abandon the whole activation and leave the desktop in a
/// half-applied state nobody can describe; partial success with an explicit account of
/// what did and did not happen is more useful, and more honest, than all-or-nothing.
/// </para>
/// </summary>
public sealed class WorkspaceActivator(
    IMonitorProvider monitorProvider,
    IDesktopAttachmentProbe attachmentProbe,
    IWallpaperSurface wallpaperSurface,
    Func<string, bool>? fileExists = null)
{
    private readonly IMonitorProvider _monitorProvider =
        monitorProvider ?? throw new ArgumentNullException(nameof(monitorProvider));

    private readonly IDesktopAttachmentProbe _attachmentProbe =
        attachmentProbe ?? throw new ArgumentNullException(nameof(attachmentProbe));

    private readonly IWallpaperSurface _wallpaperSurface =
        wallpaperSurface ?? throw new ArgumentNullException(nameof(wallpaperSurface));

    // Injectable so activation logic can be tested without touching the filesystem.
    private readonly Func<string, bool> _fileExists = fileExists ?? File.Exists;

    public WorkspaceActivationResult Activate(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var connected = _monitorProvider.GetMonitors();
        var resolution = WorkspaceResolver.Resolve(
            workspace, connected.Select(monitor => monitor.DeviceInterfacePath));

        var capabilities = new WallpaperHostCapabilities(_attachmentProbe.IsAttachmentSurfaceAvailable());

        var warnings = new List<string>();
        var applied = new List<MonitorActivation>();

        WarnIfPerMonitorWallpaperIsUnsupported(resolution, warnings);

        foreach (var layout in resolution.PresentMonitors)
        {
            applied.Add(ActivateMonitor(layout, capabilities, warnings));
        }

        if (resolution.HasDeferredContent)
        {
            warnings.Add(
                $"{resolution.DeferredContainers.Count} container(s) and {resolution.DeferredWidgets.Count} " +
                $"widget(s) belong to {resolution.AbsentMonitors.Count} monitor(s) that are not connected. " +
                "They are preserved and will return when those monitors do.");
        }

        return new WorkspaceActivationResult(workspace.Id, resolution, applied, warnings);
    }

    private MonitorActivation ActivateMonitor(
        MonitorLayout layout,
        WallpaperHostCapabilities capabilities,
        List<string> warnings)
    {
        if (layout.Wallpaper is null)
        {
            return new MonitorActivation(layout.DeviceInterfacePath, null, WallpaperApplication.NotRequested, null);
        }

        var decision = WallpaperTierResolver.Resolve(layout.Wallpaper, capabilities);

        if (decision.IsDegraded)
        {
            // PRD §13.7: a degraded outcome must be visible, never silently different
            // from what the user configured.
            warnings.Add($"{Describe(layout)}: {decision.DegradationReason}");
        }

        if (!_fileExists(layout.Wallpaper.SourcePath))
        {
            // Applying a missing path can leave a blank desktop with no explanation,
            // which reads as a bug rather than a missing file.
            string message = $"{Describe(layout)}: the wallpaper file '{layout.Wallpaper.SourcePath}' was not found.";
            warnings.Add(message);
            return new MonitorActivation(
                layout.DeviceInterfacePath, decision, WallpaperApplication.SourceMissing, message);
        }

        if (decision.SelectedTier == WallpaperTier.AttachedSurface)
        {
            // The animated path needs the desktop host's rendering surface, which does
            // not exist yet. Reported as pending rather than quietly skipped.
            return new MonitorActivation(
                layout.DeviceInterfacePath, decision, WallpaperApplication.PendingHostSupport,
                "Animated rendering requires the desktop host, which is not implemented yet.");
        }

        string? error = _wallpaperSurface.SetStaticWallpaper(
            layout.DeviceInterfacePath, layout.Wallpaper.SourcePath);

        if (error is not null)
        {
            warnings.Add($"{Describe(layout)}: {error}");
            return new MonitorActivation(
                layout.DeviceInterfacePath, decision, WallpaperApplication.Failed, error);
        }

        return new MonitorActivation(
            layout.DeviceInterfacePath, decision, WallpaperApplication.Applied, null);
    }

    private void WarnIfPerMonitorWallpaperIsUnsupported(WorkspaceResolution resolution, List<string> warnings)
    {
        if (_wallpaperSurface.SupportsPerMonitor)
        {
            return;
        }

        var distinctSources = resolution.PresentMonitors
            .Where(monitor => monitor.Wallpaper is not null)
            .Select(monitor => monitor.Wallpaper!.SourcePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (distinctSources > 1)
        {
            warnings.Add(
                "This workspace sets different wallpapers per monitor, but the current wallpaper " +
                "surface applies one image to the whole desktop. Only one will take effect.");
        }
    }

    private static string Describe(MonitorLayout layout) =>
        string.IsNullOrWhiteSpace(layout.FriendlyName) ? layout.DeviceInterfacePath : layout.FriendlyName;
}

public enum WallpaperApplication
{
    /// <summary>The workspace assigns no wallpaper to this monitor.</summary>
    NotRequested,

    Applied,

    /// <summary>The configured source file does not exist.</summary>
    SourceMissing,

    /// <summary>The surface rejected the request.</summary>
    Failed,

    /// <summary>Needs the animated rendering host, which is not built yet.</summary>
    PendingHostSupport
}

/// <param name="TierDecision">Null when no wallpaper was requested for this monitor.</param>
public readonly record struct MonitorActivation(
    string MonitorDeviceInterfacePath,
    WallpaperTierDecision? TierDecision,
    WallpaperApplication Outcome,
    string? Detail);

public sealed class WorkspaceActivationResult(
    Guid workspaceId,
    WorkspaceResolution resolution,
    IReadOnlyList<MonitorActivation> monitors,
    IReadOnlyList<string> warnings)
{
    public Guid WorkspaceId { get; } = workspaceId;

    public WorkspaceResolution Resolution { get; } = resolution;

    public IReadOnlyList<MonitorActivation> Monitors { get; } = monitors;

    /// <summary>
    /// Everything the user should be told about. Empty means the workspace was applied
    /// exactly as configured.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; } = warnings;

    public bool AppliedExactlyAsConfigured => Warnings.Count == 0;
}
