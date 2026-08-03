namespace DesktopRuntime.Core.Automation;

/// <summary>
/// An as-authored automation rule: when <see cref="Trigger"/> fires, run <see cref="Actions"/>.
/// <para>
/// Untrusted until validated. See docs/architecture/automation-schema.md.
/// </para>
/// </summary>
public sealed class AutomationRule
{
    public int SchemaVersion { get; set; } = AutomationSchema.CurrentVersion;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public AutomationTrigger? Trigger { get; set; }

    public List<AutomationAction> Actions { get; set; } = [];
}

/// <summary>
/// What causes a rule to fire. <see cref="Kind"/> must name a trigger in
/// <see cref="AutomationCatalog"/>; <see cref="Argument"/> is interpreted per kind.
/// </summary>
public sealed class AutomationTrigger
{
    public string Kind { get; set; } = string.Empty;

    public string? Argument { get; set; }
}

/// <summary>
/// What a rule does when it fires. Actions are drawn from a closed catalog — there is
/// deliberately no action that runs a command line, shell, or script.
/// </summary>
public sealed class AutomationAction
{
    public string Kind { get; set; } = string.Empty;

    public string? Argument { get; set; }
}

public static class AutomationSchema
{
    public const int CurrentVersion = 1;

    public const int MaxActionsPerRule = 16;

    public const int MaxNameLength = 64;
}
