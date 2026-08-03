using DesktopRuntime.Core.Workspaces;

namespace DesktopRuntime.Core.Tests.Workspaces;

public class WorkspaceSerializerTests
{
    private const string MonitorA = @"\\?\DISPLAY#AOP0806#4&1427843b&0&UID198147#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
    private const string MonitorB = @"\\?\DISPLAY#DEL4321#5&2f8a91c2&0&UID257#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";

    private static Workspace CreateSampleWorkspace() => new()
    {
        Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        Name = "Focus",
        CreatedUtc = new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero),
        ModifiedUtc = new DateTimeOffset(2026, 8, 2, 13, 30, 0, TimeSpan.Zero),
        Monitors =
        [
            new MonitorLayout
            {
                DeviceInterfacePath = MonitorA,
                FriendlyName = "Generic PnP Monitor",
                Bounds = new Rect(0, 0, 1920, 1080),
                Dpi = 96,
                IsPrimary = true,
                Wallpaper = new WallpaperAssignment
                {
                    Kind = WallpaperKind.Video,
                    SourcePath = @"C:\wallpapers\loop.mp4"
                }
            }
        ],
        Containers =
        [
            new DesktopContainer
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                Title = "Projects",
                MonitorDeviceInterfacePath = MonitorA,
                Bounds = new Rect(40, 40, 420, 300),
                IsCollapsed = false,
                Opacity = 0.85,
                ItemPaths = [@"C:\code\project.sln"]
            }
        ],
        Widgets =
        [
            new WidgetPlacement
            {
                Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
                WidgetTypeId = "core.clock",
                MonitorDeviceInterfacePath = MonitorA,
                Bounds = new Rect(1500, 40, 300, 140),
                Settings = new Dictionary<string, string> { ["format"] = "24h" }
            }
        ]
    };

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = CreateSampleWorkspace();

        var restored = WorkspaceSerializer.Deserialize(WorkspaceSerializer.Serialize(original));

        Assert.Equal(WorkspaceSchema.CurrentVersion, restored.SchemaVersion);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.CreatedUtc, restored.CreatedUtc);
        Assert.Equal(original.ModifiedUtc, restored.ModifiedUtc);

        var monitor = Assert.Single(restored.Monitors);
        Assert.Equal(MonitorA, monitor.DeviceInterfacePath);
        Assert.Equal("Generic PnP Monitor", monitor.FriendlyName);
        Assert.Equal(new Rect(0, 0, 1920, 1080), monitor.Bounds);
        Assert.Equal(96u, monitor.Dpi);
        Assert.True(monitor.IsPrimary);
        Assert.NotNull(monitor.Wallpaper);
        Assert.Equal(WallpaperKind.Video, monitor.Wallpaper!.Kind);
        Assert.Equal(@"C:\wallpapers\loop.mp4", monitor.Wallpaper.SourcePath);

        var container = Assert.Single(restored.Containers);
        Assert.Equal("Projects", container.Title);
        Assert.Equal(MonitorA, container.MonitorDeviceInterfacePath);
        Assert.Equal(new Rect(40, 40, 420, 300), container.Bounds);
        Assert.Equal(0.85, container.Opacity);
        Assert.Equal([@"C:\code\project.sln"], container.ItemPaths);

        var widget = Assert.Single(restored.Widgets);
        Assert.Equal("core.clock", widget.WidgetTypeId);
        Assert.Equal(new Rect(1500, 40, 300, 140), widget.Bounds);
        Assert.Equal("24h", widget.Settings["format"]);
    }

    [Fact]
    public void Serialize_WritesEnumsAsNames_SoReorderingAnEnumCannotReinterpretOldFiles()
    {
        var json = WorkspaceSerializer.Serialize(CreateSampleWorkspace());

        Assert.Contains("\"Video\"", json);
        // A numeric enum would make Static/Video positional and therefore fragile.
        Assert.DoesNotContain("\"kind\": 1", json);
    }

    [Fact]
    public void Serialize_DoesNotPersistWhichRenderingTierServedTheWallpaper()
    {
        // ADR-0003 / workspace-schema.md decision 4: persisting a degraded tier would make
        // the degradation sticky across sessions, which PRD §13.7 forbids.
        var json = WorkspaceSerializer.Serialize(CreateSampleWorkspace());

        Assert.DoesNotContain("tier", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_RejectsNewerSchemaVersion_WithActionableMessage()
    {
        var json = WorkspaceSerializer.Serialize(CreateSampleWorkspace())
            .Replace($"\"schemaVersion\": {WorkspaceSchema.CurrentVersion}",
                     $"\"schemaVersion\": {WorkspaceSchema.CurrentVersion + 1}");

        var ex = Assert.Throws<WorkspaceLoadException>(() => WorkspaceSerializer.Deserialize(json));

        Assert.Contains("newer version", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_RejectsDocumentWithoutSchemaVersion()
    {
        const string json = """{ "name": "No version here" }""";

        var ex = Assert.Throws<WorkspaceLoadException>(() => WorkspaceSerializer.Deserialize(json));

        Assert.Contains("schemaVersion", ex.Message);
    }

    [Fact]
    public void Deserialize_RejectsMalformedJson()
    {
        Assert.Throws<WorkspaceLoadException>(() => WorkspaceSerializer.Deserialize("{ not json"));
    }

    [Fact]
    public void Deserialize_RejectsNonObjectRoot()
    {
        Assert.Throws<WorkspaceLoadException>(() => WorkspaceSerializer.Deserialize("[]"));
    }

    [Fact]
    public void RoundTrip_PreservesLayoutForMonitorsNotCurrentlyConnected()
    {
        // Undock/redock must be lossless — workspace-schema.md decision 5.
        var workspace = CreateSampleWorkspace();
        workspace.Monitors.Add(new MonitorLayout
        {
            DeviceInterfacePath = MonitorB,
            Bounds = new Rect(1920, 0, 2560, 1440),
            Dpi = 144
        });
        workspace.Containers.Add(new DesktopContainer
        {
            Title = "Reference",
            MonitorDeviceInterfacePath = MonitorB,
            Bounds = new Rect(10, 10, 300, 200)
        });

        var restored = WorkspaceSerializer.Deserialize(WorkspaceSerializer.Serialize(workspace));

        Assert.Equal(2, restored.Monitors.Count);
        Assert.Contains(restored.Monitors, m => m.DeviceInterfacePath == MonitorB && m.Dpi == 144);
        Assert.Contains(restored.Containers, c => c.MonitorDeviceInterfacePath == MonitorB);
    }
}
