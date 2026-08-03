using DesktopRuntime.Core.Hosting;
using DesktopRuntime.Core.Wallpapers;
using DesktopRuntime.Core.Workspaces;

namespace DesktopRuntime.Core.Tests.Workspaces;

public class WorkspaceActivatorTests
{
    private const string LaptopScreen = @"\\?\DISPLAY#AOP0806#4&1427843b&0&UID198147#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
    private const string DockedMonitor = @"\\?\DISPLAY#DEL4321#5&2f8a91c2&0&UID257#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";

    private sealed class FakeMonitors(params string[] paths) : IMonitorProvider
    {
        public IReadOnlyList<MonitorInfo> GetMonitors() =>
            [.. paths.Select(p => new MonitorInfo(p, "Fake monitor", new Rect(0, 0, 1920, 1080), 96, true))];
    }

    private sealed class FakeAttachment(bool available) : IDesktopAttachmentProbe
    {
        public bool IsAttachmentSurfaceAvailable() => available;
    }

    private sealed class FakeWallpaperSurface(bool perMonitor = false, string? failWith = null) : IWallpaperSurface
    {
        public bool SupportsPerMonitor { get; } = perMonitor;

        public List<(string Monitor, string Image)> Applied { get; } = [];

        public string? SetStaticWallpaper(string monitorDeviceInterfacePath, string imagePath)
        {
            if (failWith is not null) return failWith;

            Applied.Add((monitorDeviceInterfacePath, imagePath));
            return null;
        }
    }

    private static Workspace WorkspaceWith(params MonitorLayout[] monitors) => new()
    {
        Name = "Test",
        Monitors = [.. monitors]
    };

    private static MonitorLayout Monitor(string path, WallpaperKind? kind = null, string source = @"C:\wallpapers\a.jpg") =>
        new()
        {
            DeviceInterfacePath = path,
            FriendlyName = "Fake monitor",
            Wallpaper = kind is null ? null : new WallpaperAssignment { Kind = kind.Value, SourcePath = source }
        };

    private static WorkspaceActivator CreateActivator(
        IMonitorProvider monitors,
        IWallpaperSurface surface,
        bool attachmentAvailable = false,
        Func<string, bool>? fileExists = null) =>
        new(monitors, new FakeAttachment(attachmentAvailable), surface, fileExists ?? (_ => true));

    [Fact]
    public void StaticWallpaper_IsApplied_AndReportsNoWarnings()
    {
        var surface = new FakeWallpaperSurface();
        var activator = CreateActivator(new FakeMonitors(LaptopScreen), surface);

        var result = activator.Activate(WorkspaceWith(Monitor(LaptopScreen, WallpaperKind.Static)));

        Assert.True(result.AppliedExactlyAsConfigured, string.Join("; ", result.Warnings));
        Assert.Equal(WallpaperApplication.Applied, Assert.Single(result.Monitors).Outcome);
        Assert.Equal(LaptopScreen, Assert.Single(surface.Applied).Monitor);
    }

    [Fact]
    public void VideoWallpaper_WithoutAttachment_DegradesAndSaysSo()
    {
        // The case Phase 3 found on this Windows build. PRD §13.7 requires it be visible.
        var surface = new FakeWallpaperSurface();
        var activator = CreateActivator(new FakeMonitors(LaptopScreen), surface, attachmentAvailable: false);

        var result = activator.Activate(WorkspaceWith(Monitor(LaptopScreen, WallpaperKind.Video)));

        Assert.False(result.AppliedExactlyAsConfigured);
        Assert.Contains(result.Warnings, w => w.Contains("Animated wallpaper is not available"));

        var monitor = Assert.Single(result.Monitors);
        Assert.True(monitor.TierDecision!.IsDegraded);
        Assert.Equal(WallpaperTier.StaticImage, monitor.TierDecision.SelectedTier);
        Assert.Equal(WallpaperApplication.Applied, monitor.Outcome);
    }

    [Fact]
    public void VideoWallpaper_WithAttachment_IsReportedAsPendingHostSupport_NotSilentlySkipped()
    {
        var surface = new FakeWallpaperSurface();
        var activator = CreateActivator(new FakeMonitors(LaptopScreen), surface, attachmentAvailable: true);

        var result = activator.Activate(WorkspaceWith(Monitor(LaptopScreen, WallpaperKind.Video)));

        var monitor = Assert.Single(result.Monitors);
        Assert.Equal(WallpaperTier.AttachedSurface, monitor.TierDecision!.SelectedTier);
        Assert.False(monitor.TierDecision.IsDegraded);
        Assert.Equal(WallpaperApplication.PendingHostSupport, monitor.Outcome);
        Assert.Empty(surface.Applied);
    }

    [Fact]
    public void MissingWallpaperFile_IsReported_AndDoesNotAbortActivation()
    {
        // Applying a missing path can leave a blank desktop with no explanation, which
        // reads as a bug rather than a missing file.
        var surface = new FakeWallpaperSurface();
        var activator = CreateActivator(
            new FakeMonitors(LaptopScreen, DockedMonitor), surface,
            fileExists: path => !path.Contains("missing"));

        var result = activator.Activate(WorkspaceWith(
            Monitor(LaptopScreen, WallpaperKind.Static, @"C:\wallpapers\missing.jpg"),
            Monitor(DockedMonitor, WallpaperKind.Static, @"C:\wallpapers\present.jpg")));

        Assert.Contains(result.Warnings, w => w.Contains("was not found"));

        // The second monitor still got its wallpaper: one failure must not abandon the rest.
        Assert.Equal(WallpaperApplication.SourceMissing,
            result.Monitors.Single(m => m.MonitorDeviceInterfacePath == LaptopScreen).Outcome);
        Assert.Equal(WallpaperApplication.Applied,
            result.Monitors.Single(m => m.MonitorDeviceInterfacePath == DockedMonitor).Outcome);
        Assert.Single(surface.Applied);
    }

    [Fact]
    public void SurfaceFailure_IsReportedRatherThanThrown()
    {
        var surface = new FakeWallpaperSurface(failWith: "the wallpaper API refused the request");
        var activator = CreateActivator(new FakeMonitors(LaptopScreen), surface);

        var result = activator.Activate(WorkspaceWith(Monitor(LaptopScreen, WallpaperKind.Static)));

        var monitor = Assert.Single(result.Monitors);
        Assert.Equal(WallpaperApplication.Failed, monitor.Outcome);
        Assert.Contains("refused", monitor.Detail);
        Assert.Contains(result.Warnings, w => w.Contains("refused"));
    }

    [Fact]
    public void DisconnectedMonitors_ProduceAWarning_AndTheirContentIsPreserved()
    {
        var surface = new FakeWallpaperSurface();
        // The docked monitor is in the workspace but not currently attached.
        var activator = CreateActivator(new FakeMonitors(LaptopScreen), surface);

        var workspace = WorkspaceWith(
            Monitor(LaptopScreen, WallpaperKind.Static),
            Monitor(DockedMonitor, WallpaperKind.Static));
        workspace.Containers.Add(new DesktopContainer
        {
            Title = "OnDock",
            MonitorDeviceInterfacePath = DockedMonitor
        });

        var result = activator.Activate(workspace);

        Assert.Contains(result.Warnings, w => w.Contains("not connected"));
        Assert.Equal("OnDock", Assert.Single(result.Resolution.DeferredContainers).Title);
        Assert.Single(result.Monitors);   // only the attached one was acted on
    }

    [Fact]
    public void MonitorWithNoWallpaperConfigured_IsNotTouched()
    {
        var surface = new FakeWallpaperSurface();
        var activator = CreateActivator(new FakeMonitors(LaptopScreen), surface);

        var result = activator.Activate(WorkspaceWith(Monitor(LaptopScreen)));

        Assert.Equal(WallpaperApplication.NotRequested, Assert.Single(result.Monitors).Outcome);
        Assert.Empty(surface.Applied);
        Assert.True(result.AppliedExactlyAsConfigured);
    }

    [Fact]
    public void DifferentWallpapersPerMonitor_OnASurfaceThatCannotDoIt_WarnsTheUser()
    {
        // Silently applying one monitor's choice everywhere would look like a bug.
        var surface = new FakeWallpaperSurface(perMonitor: false);
        var activator = CreateActivator(new FakeMonitors(LaptopScreen, DockedMonitor), surface);

        var result = activator.Activate(WorkspaceWith(
            Monitor(LaptopScreen, WallpaperKind.Static, @"C:\wallpapers\a.jpg"),
            Monitor(DockedMonitor, WallpaperKind.Static, @"C:\wallpapers\b.jpg")));

        Assert.Contains(result.Warnings, w => w.Contains("one image to the whole desktop"));
    }

    [Fact]
    public void SameWallpaperOnEveryMonitor_DoesNotWarnAboutPerMonitorSupport()
    {
        var surface = new FakeWallpaperSurface(perMonitor: false);
        var activator = CreateActivator(new FakeMonitors(LaptopScreen, DockedMonitor), surface);

        var result = activator.Activate(WorkspaceWith(
            Monitor(LaptopScreen, WallpaperKind.Static, @"C:\wallpapers\same.jpg"),
            Monitor(DockedMonitor, WallpaperKind.Static, @"C:\wallpapers\same.jpg")));

        Assert.DoesNotContain(result.Warnings, w => w.Contains("one image to the whole desktop"));
    }

    [Fact]
    public void PerMonitorCapableSurface_NeverWarnsAboutPerMonitorSupport()
    {
        var surface = new FakeWallpaperSurface(perMonitor: true);
        var activator = CreateActivator(new FakeMonitors(LaptopScreen, DockedMonitor), surface);

        var result = activator.Activate(WorkspaceWith(
            Monitor(LaptopScreen, WallpaperKind.Static, @"C:\wallpapers\a.jpg"),
            Monitor(DockedMonitor, WallpaperKind.Static, @"C:\wallpapers\b.jpg")));

        Assert.True(result.AppliedExactlyAsConfigured, string.Join("; ", result.Warnings));
        Assert.Equal(2, surface.Applied.Count);
    }

    [Fact]
    public void ActivatingAWorkspaceWithNoConnectedMonitors_ReportsRatherThanCrashes()
    {
        var surface = new FakeWallpaperSurface();
        var activator = CreateActivator(new FakeMonitors(), surface);

        var result = activator.Activate(WorkspaceWith(Monitor(LaptopScreen, WallpaperKind.Static)));

        Assert.Empty(result.Monitors);
        Assert.Empty(surface.Applied);
        Assert.Contains(result.Warnings, w => w.Contains("not connected"));
    }

    [Fact]
    public void Activate_RejectsNullWorkspace()
    {
        var activator = CreateActivator(new FakeMonitors(), new FakeWallpaperSurface());

        Assert.Throws<ArgumentNullException>(() => activator.Activate(null!));
    }
}
