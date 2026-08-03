using DesktopRuntime.Core.Workspaces;

namespace DesktopRuntime.Core.Tests.Workspaces;

public class WorkspaceResolverTests
{
    private const string LaptopScreen = @"\\?\DISPLAY#AOP0806#4&1427843b&0&UID198147#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
    private const string DockedMonitor = @"\\?\DISPLAY#DEL4321#5&2f8a91c2&0&UID257#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";

    private static Workspace CreateDockedWorkspace() => new()
    {
        Name = "Docked",
        Monitors =
        [
            new MonitorLayout { DeviceInterfacePath = LaptopScreen, Bounds = new Rect(0, 0, 1920, 1080) },
            new MonitorLayout { DeviceInterfacePath = DockedMonitor, Bounds = new Rect(1920, 0, 2560, 1440) }
        ],
        Containers =
        [
            new DesktopContainer { Title = "OnLaptop", MonitorDeviceInterfacePath = LaptopScreen },
            new DesktopContainer { Title = "OnDock", MonitorDeviceInterfacePath = DockedMonitor }
        ],
        Widgets =
        [
            new WidgetPlacement { WidgetTypeId = "core.clock", MonitorDeviceInterfacePath = LaptopScreen },
            new WidgetPlacement { WidgetTypeId = "core.cpu", MonitorDeviceInterfacePath = DockedMonitor }
        ]
    };

    [Fact]
    public void Undocking_DefersContentInsteadOfDiscardingIt()
    {
        var workspace = CreateDockedWorkspace();

        // Laptop undocked: only the built-in screen remains connected.
        var resolution = WorkspaceResolver.Resolve(workspace, [LaptopScreen]);

        Assert.Equal(LaptopScreen, Assert.Single(resolution.PresentMonitors).DeviceInterfacePath);
        Assert.Equal(DockedMonitor, Assert.Single(resolution.AbsentMonitors).DeviceInterfacePath);

        Assert.Equal("OnLaptop", Assert.Single(resolution.PlaceableContainers).Title);
        Assert.Equal("OnDock", Assert.Single(resolution.DeferredContainers).Title);

        Assert.Equal("core.clock", Assert.Single(resolution.PlaceableWidgets).WidgetTypeId);
        Assert.Equal("core.cpu", Assert.Single(resolution.DeferredWidgets).WidgetTypeId);

        Assert.True(resolution.HasDeferredContent);
    }

    [Fact]
    public void Resolve_DoesNotMutateTheWorkspace_SoRedockingIsLossless()
    {
        var workspace = CreateDockedWorkspace();

        WorkspaceResolver.Resolve(workspace, [LaptopScreen]);

        // The saved workspace must still describe both monitors after resolving against one.
        Assert.Equal(2, workspace.Monitors.Count);
        Assert.Equal(2, workspace.Containers.Count);
        Assert.Equal(2, workspace.Widgets.Count);

        // Redocking restores everything.
        var redocked = WorkspaceResolver.Resolve(workspace, [LaptopScreen, DockedMonitor]);
        Assert.Equal(2, redocked.PlaceableContainers.Count);
        Assert.Empty(redocked.DeferredContainers);
        Assert.False(redocked.HasDeferredContent);
    }

    [Fact]
    public void Resolve_MatchesMonitorPathsCaseInsensitively()
    {
        // Windows device paths are not case-sensitive; case-sensitive matching would
        // spuriously orphan a layout that is in fact on a connected monitor.
        var workspace = CreateDockedWorkspace();

        var resolution = WorkspaceResolver.Resolve(workspace, [LaptopScreen.ToUpperInvariant()]);

        Assert.Single(resolution.PresentMonitors);
        Assert.Equal("OnLaptop", Assert.Single(resolution.PlaceableContainers).Title);
    }

    [Fact]
    public void Resolve_WithNoMonitorsConnected_DefersEverythingAndDiscardsNothing()
    {
        var workspace = CreateDockedWorkspace();

        var resolution = WorkspaceResolver.Resolve(workspace, []);

        Assert.Empty(resolution.PresentMonitors);
        Assert.Empty(resolution.PlaceableContainers);
        Assert.Empty(resolution.PlaceableWidgets);
        Assert.Equal(2, resolution.AbsentMonitors.Count);
        Assert.Equal(2, resolution.DeferredContainers.Count);
        Assert.Equal(2, resolution.DeferredWidgets.Count);
    }

    [Fact]
    public void Resolve_WithAnUnknownConnectedMonitor_ReportsNoDeferredContent()
    {
        // A brand-new monitor the workspace has never seen is not an error; it simply
        // has no saved layout yet.
        var workspace = new Workspace
        {
            Monitors = [new MonitorLayout { DeviceInterfacePath = LaptopScreen }],
            Containers = [new DesktopContainer { Title = "OnLaptop", MonitorDeviceInterfacePath = LaptopScreen }]
        };

        var resolution = WorkspaceResolver.Resolve(workspace, [LaptopScreen, DockedMonitor]);

        Assert.Single(resolution.PresentMonitors);
        Assert.Empty(resolution.AbsentMonitors);
        Assert.False(resolution.HasDeferredContent);
    }
}
