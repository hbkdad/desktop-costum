namespace DesktopRuntime.Core.Widgets;

/// <summary>
/// The declared contents of a widget package, as written by its author.
/// <para>
/// This is the untrusted, as-authored form. Nothing here is believed until it has
/// been through <see cref="WidgetManifestValidator"/>, which produces a
/// <see cref="ValidatedWidgetManifest"/>. Code that consumes widgets should take the
/// validated type, so an unvalidated manifest cannot reach the runtime by accident.
/// </para>
/// See docs/architecture/widget-manifest.md.
/// </summary>
public sealed class WidgetManifest
{
    public int ManifestVersion { get; set; } = WidgetManifestSchema.CurrentVersion;

    /// <summary>Reverse-DNS style identifier, e.g. <c>com.example.clock</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Three-part numeric version, e.g. <c>1.0.0</c>.</summary>
    public string Version { get; set; } = string.Empty;

    public string? Author { get; set; }

    /// <summary>Declared capabilities. Anything not declared is denied.</summary>
    public List<string> Permissions { get; set; } = [];

    /// <summary>Supported sizes. At least one is required.</summary>
    public List<WidgetSize> Sizes { get; set; } = [];

    /// <summary>
    /// The author's declared resource cost. Required — a widget that cannot state its
    /// resource impact cannot be accepted (see the `widget-builder` and `performance-test`
    /// skills, and the Definition of Done).
    /// </summary>
    public WidgetResourceBudget? ResourceBudget { get; set; }
}

public sealed class WidgetSize
{
    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }
}

/// <summary>The declared, to-be-verified resource cost of a widget.</summary>
public sealed class WidgetResourceBudget
{
    /// <summary>Expected CPU use while idle, as a percentage of one core.</summary>
    public double IdleCpuPercent { get; set; }

    public int MemoryMb { get; set; }

    /// <summary>Redraw rate. 0 means event-driven (redraws only when its data changes).</summary>
    public int FramesPerSecond { get; set; }
}

public static class WidgetManifestSchema
{
    public const int CurrentVersion = 1;

    /// <summary>
    /// Upper bounds on a declared budget. A widget may declare less, never more.
    /// These are ceilings for what is even expressible, not per-widget targets —
    /// the real budgets live with the Performance Agent's benchmark profiles.
    /// </summary>
    public const double MaxIdleCpuPercent = 5.0;
    public const int MaxMemoryMb = 512;
    public const int MaxFramesPerSecond = 120;

    /// <summary>
    /// Bounds on a declared widget surface. A surface larger than any plausible display
    /// is refused: an unbounded size is a cheap way to force enormous allocations.
    /// </summary>
    public const int MaxDimension = 16384;
}
