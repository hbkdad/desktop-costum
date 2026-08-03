using System.Diagnostics.CodeAnalysis;
using DesktopRuntime.Core.Permissions;

namespace DesktopRuntime.Core.Automation;

/// <summary>
/// The closed sets of triggers and actions an automation rule may use.
/// <para>
/// As with <see cref="CapabilityCatalog"/>, the security property comes from the
/// catalog being closed: there is deliberately no action that runs a command line,
/// a shell, a script, or an arbitrary binary. Automation cannot become a way to
/// smuggle in the arbitrary execution the permission model refuses to grant.
/// </para>
/// See docs/architecture/automation-schema.md.
/// </summary>
public static class AutomationCatalog
{
    // --- Triggers ---
    public const string TriggerApplicationStart = "application.start";
    public const string TriggerApplicationExit = "application.exit";
    public const string TriggerMonitorConnected = "monitor.connected";
    public const string TriggerMonitorDisconnected = "monitor.disconnected";
    public const string TriggerPowerSource = "power.source";
    public const string TriggerTimeSchedule = "time.schedule";
    public const string TriggerWorkspaceActivated = "workspace.activated";

    // --- Actions ---
    public const string ActionActivateWorkspace = "workspace.activate";
    public const string ActionLaunchApplication = "application.launch";
    public const string ActionShowWidget = "widget.show";
    public const string ActionHideWidget = "widget.hide";
    public const string ActionPauseRendering = "render.pause";
    public const string ActionResumeRendering = "render.resume";
    public const string ActionSetRenderQuality = "render.quality";

    private static readonly Dictionary<string, TriggerDefinition> Triggers = new(StringComparer.Ordinal)
    {
        [TriggerApplicationStart] = new(TriggerApplicationStart, ValidateApplicationId, "declared-application"),
        [TriggerApplicationExit] = new(TriggerApplicationExit, ValidateApplicationId, "declared-application"),
        [TriggerMonitorConnected] = new(TriggerMonitorConnected, ValidateMonitorPath, @"\\?\DISPLAY#..."),
        [TriggerMonitorDisconnected] = new(TriggerMonitorDisconnected, ValidateMonitorPath, @"\\?\DISPLAY#..."),
        [TriggerPowerSource] = new(TriggerPowerSource, ValidatePowerSource, "Battery"),
        [TriggerTimeSchedule] = new(TriggerTimeSchedule, ValidateTimeOfDay, "22:30"),
        [TriggerWorkspaceActivated] = new(TriggerWorkspaceActivated, ValidateGuid, "<workspace id>")
    };

    private static readonly Dictionary<string, ActionDefinition> Actions = new(StringComparer.Ordinal)
    {
        [ActionActivateWorkspace] = new(ActionActivateWorkspace, ValidateGuid, "<workspace id>"),

        // The only action that reaches outside the application, and therefore the only
        // one gated on a capability. The launched application must ALSO be declared as
        // process.launch:<id> in the package's permissions.
        [ActionLaunchApplication] = new(
            ActionLaunchApplication, ValidateApplicationId, "declared-application",
            requiredCapability: CapabilityCatalog.ProcessLaunch,
            capabilityScopeIsArgument: true),

        [ActionShowWidget] = new(ActionShowWidget, ValidateGuid, "<widget id>"),
        [ActionHideWidget] = new(ActionHideWidget, ValidateGuid, "<widget id>"),
        [ActionPauseRendering] = new(ActionPauseRendering),
        [ActionResumeRendering] = new(ActionResumeRendering),
        [ActionSetRenderQuality] = new(ActionSetRenderQuality, ValidateRenderQuality, "Reduced")
    };

    public static IReadOnlyCollection<string> KnownTriggers => Triggers.Keys;

    public static IReadOnlyCollection<string> KnownActions => Actions.Keys;

    public static bool TryGetTrigger(string kind, [NotNullWhen(true)] out TriggerDefinition? definition) =>
        Triggers.TryGetValue(kind, out definition);

    public static bool TryGetAction(string kind, [NotNullWhen(true)] out ActionDefinition? definition) =>
        Actions.TryGetValue(kind, out definition);

    /// <summary>
    /// An application identifier is valid here exactly when it would be valid as a
    /// <c>process.launch</c> capability scope. Reusing that check rather than writing a
    /// second one means a command line can never be accepted in one place and refused in
    /// the other.
    /// </summary>
    private static bool ValidateApplicationId(string argument, [NotNullWhen(false)] out string? error)
    {
        if (Capability.TryParse($"{CapabilityCatalog.ProcessLaunch}:{argument}", out _, out string? capabilityError))
        {
            error = null;
            return true;
        }

        error = capabilityError;
        return false;
    }

    private static bool ValidateMonitorPath(string argument, [NotNullWhen(false)] out string? error)
    {
        if (!argument.StartsWith(@"\\?\DISPLAY#", StringComparison.OrdinalIgnoreCase))
        {
            error = @"a monitor must be identified by its device interface path (starting \\?\DISPLAY#).";
            return false;
        }

        foreach (char c in argument)
        {
            if (char.IsControl(c))
            {
                error = "the monitor path contains control characters.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool ValidatePowerSource(string argument, [NotNullWhen(false)] out string? error)
    {
        if (argument is "AC" or "Battery")
        {
            error = null;
            return true;
        }

        error = "must be 'AC' or 'Battery'.";
        return false;
    }

    private static bool ValidateTimeOfDay(string argument, [NotNullWhen(false)] out string? error)
    {
        if (TimeOnly.TryParseExact(argument, "HH:mm", out _))
        {
            error = null;
            return true;
        }

        error = "must be a 24-hour time of day, e.g. '22:30'.";
        return false;
    }

    private static bool ValidateGuid(string argument, [NotNullWhen(false)] out string? error)
    {
        if (Guid.TryParse(argument, out _))
        {
            error = null;
            return true;
        }

        error = "must be a valid identifier (GUID).";
        return false;
    }

    private static bool ValidateRenderQuality(string argument, [NotNullWhen(false)] out string? error)
    {
        if (argument is "Full" or "Reduced" or "Minimal")
        {
            error = null;
            return true;
        }

        error = "must be 'Full', 'Reduced' or 'Minimal'.";
        return false;
    }
}

internal delegate bool ArgumentValidator(string argument, [NotNullWhen(false)] out string? error);

public sealed class TriggerDefinition
{
    private readonly ArgumentValidator? _validator;

    internal TriggerDefinition(string kind, ArgumentValidator? validator = null, string? argumentExample = null)
    {
        Kind = kind;
        _validator = validator;
        ArgumentExample = argumentExample;
    }

    public string Kind { get; }

    public string? ArgumentExample { get; }

    public bool RequiresArgument => _validator is not null;

    internal bool TryValidateArgument(string argument, [NotNullWhen(false)] out string? error)
    {
        if (_validator is null)
        {
            error = "this trigger does not take an argument.";
            return false;
        }

        return _validator(argument, out error);
    }
}

public sealed class ActionDefinition
{
    private readonly ArgumentValidator? _validator;

    internal ActionDefinition(
        string kind,
        ArgumentValidator? validator = null,
        string? argumentExample = null,
        string? requiredCapability = null,
        bool capabilityScopeIsArgument = false)
    {
        Kind = kind;
        _validator = validator;
        ArgumentExample = argumentExample;
        RequiredCapability = requiredCapability;
        CapabilityScopeIsArgument = capabilityScopeIsArgument;
    }

    public string Kind { get; }

    public string? ArgumentExample { get; }

    public bool RequiresArgument => _validator is not null;

    /// <summary>The capability this action needs, or null if it needs none.</summary>
    public string? RequiredCapability { get; }

    /// <summary>Whether the required capability must be scoped to this action's argument.</summary>
    public bool CapabilityScopeIsArgument { get; }

    internal bool TryValidateArgument(string argument, [NotNullWhen(false)] out string? error)
    {
        if (_validator is null)
        {
            error = "this action does not take an argument.";
            return false;
        }

        return _validator(argument, out error);
    }
}
