namespace DesktopRuntime.Core.Workspaces;

/// <summary>
/// A named, saveable arrangement of desktop containers, widgets and per-monitor
/// wallpapers. See docs/architecture/workspace-schema.md for the format contract
/// and the rationale behind the identity/coordinate decisions.
/// </summary>
public sealed class Workspace
{
    /// <summary>Schema version this instance conforms to. Validated on load.</summary>
    public int SchemaVersion { get; set; } = WorkspaceSchema.CurrentVersion;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset ModifiedUtc { get; set; }

    public List<MonitorLayout> Monitors { get; set; } = [];

    public List<DesktopContainer> Containers { get; set; } = [];

    public List<WidgetPlacement> Widgets { get; set; } = [];
}

/// <summary>
/// Per-monitor layout. Identified by <see cref="DeviceInterfacePath"/> only —
/// see workspace-schema.md decision 1 (Prototype 9 found GDI device names and
/// HMONITOR handles are positional/runtime values and unsafe to persist).
/// </summary>
public sealed class MonitorLayout
{
    /// <summary>
    /// Stable identity: the device interface path, which embeds the monitor's
    /// hardware/EDID id. The only field used to match a saved layout to a
    /// physically present monitor.
    /// </summary>
    public string DeviceInterfacePath { get; set; } = string.Empty;

    /// <summary>Diagnostic only — not unique across identical models, never matched on.</summary>
    public string? FriendlyName { get; set; }

    /// <summary>Geometry at save time. An attribute of the monitor, not part of its identity.</summary>
    public Rect Bounds { get; set; }

    public uint Dpi { get; set; } = 96;

    public bool IsPrimary { get; set; }

    public WallpaperAssignment? Wallpaper { get; set; }
}

/// <summary>
/// What the user asked for. Deliberately does NOT record which rendering tier
/// actually served it — see workspace-schema.md decision 4 and ADR-0003: a
/// degraded state must not become sticky across sessions.
/// </summary>
public sealed class WallpaperAssignment
{
    public WallpaperKind Kind { get; set; } = WallpaperKind.Static;

    public string SourcePath { get; set; } = string.Empty;
}

/// <summary>MVP scope only (PRD §2 defers web/shader/particle wallpapers).</summary>
public enum WallpaperKind
{
    Static,
    Video
}

public sealed class DesktopContainer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    /// <summary>Which monitor this container lives on. Matches <see cref="MonitorLayout.DeviceInterfacePath"/>.</summary>
    public string MonitorDeviceInterfacePath { get; set; } = string.Empty;

    /// <summary>Monitor-relative, not virtual-desktop-global — see workspace-schema.md decision 2.</summary>
    public Rect Bounds { get; set; }

    public bool IsCollapsed { get; set; }

    public double Opacity { get; set; } = 1.0;

    public List<string> ItemPaths { get; set; } = [];
}

public sealed class WidgetPlacement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string WidgetTypeId { get; set; } = string.Empty;

    public string MonitorDeviceInterfacePath { get; set; } = string.Empty;

    /// <summary>Monitor-relative, not virtual-desktop-global — see workspace-schema.md decision 2.</summary>
    public Rect Bounds { get; set; }

    /// <summary>
    /// Opaque per-widget settings. The runtime does not interpret these; the owning
    /// widget does. Keeps widget authoring decoupled from the workspace schema version.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = [];
}

public readonly record struct Rect(int X, int Y, int Width, int Height);
