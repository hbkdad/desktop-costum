using DesktopRuntime.Core.Permissions;

namespace DesktopRuntime.Core.Automation;

/// <summary>
/// Validates an as-authored <see cref="AutomationRule"/> against the permissions its
/// package was actually granted.
/// <para>
/// Validation deliberately requires a <see cref="PermissionSet"/>: a rule is only
/// meaningful relative to what its package may do. Without this, automation would be a
/// way around the permission model — a package could declare no capabilities and then
/// launch applications through a rule.
/// </para>
/// See docs/architecture/automation-schema.md.
/// </summary>
public static class AutomationRuleValidator
{
    public static AutomationValidationResult Validate(AutomationRule rule, PermissionSet grantedPermissions)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(grantedPermissions);

        var errors = new List<string>();

        if (rule.SchemaVersion > AutomationSchema.CurrentVersion)
        {
            errors.Add($"Rule schema version {rule.SchemaVersion} is newer than this build supports " +
                       $"({AutomationSchema.CurrentVersion}).");
        }
        else if (rule.SchemaVersion < 1)
        {
            errors.Add("Rule schema version must be 1 or greater.");
        }

        ValidateName(rule.Name, errors);
        ValidateTrigger(rule.Trigger, errors);
        ValidateActions(rule.Actions, grantedPermissions, errors);

        return errors.Count > 0
            ? AutomationValidationResult.Failed(errors)
            : AutomationValidationResult.Succeeded(new ValidatedAutomationRule(rule));
    }

    private static void ValidateName(string? name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Rule name is required.");
            return;
        }

        if (name.Length > AutomationSchema.MaxNameLength)
        {
            errors.Add($"Rule name must be {AutomationSchema.MaxNameLength} characters or fewer.");
        }

        foreach (char c in name)
        {
            if (char.IsControl(c))
            {
                // A rule is shown to the user when they review automation; control
                // characters could misrepresent what the rule is.
                errors.Add("Rule name must not contain control characters.");
                return;
            }
        }
    }

    private static void ValidateTrigger(AutomationTrigger? trigger, List<string> errors)
    {
        if (trigger is null)
        {
            errors.Add("A trigger is required.");
            return;
        }

        if (!AutomationCatalog.TryGetTrigger(trigger.Kind ?? string.Empty, out var definition))
        {
            errors.Add($"Unknown trigger '{trigger.Kind}'.");
            return;
        }

        if (definition.RequiresArgument)
        {
            if (string.IsNullOrWhiteSpace(trigger.Argument))
            {
                errors.Add($"Trigger '{definition.Kind}' requires an argument, e.g. '{definition.ArgumentExample}'.");
            }
            else if (!definition.TryValidateArgument(trigger.Argument, out string? error))
            {
                errors.Add($"Trigger '{definition.Kind}' has an invalid argument: {error}");
            }
        }
        else if (trigger.Argument is not null)
        {
            errors.Add($"Trigger '{definition.Kind}' does not take an argument.");
        }
    }

    private static void ValidateActions(
        List<AutomationAction>? actions,
        PermissionSet grantedPermissions,
        List<string> errors)
    {
        if (actions is null || actions.Count == 0)
        {
            errors.Add("At least one action is required.");
            return;
        }

        if (actions.Count > AutomationSchema.MaxActionsPerRule)
        {
            errors.Add($"A rule may declare at most {AutomationSchema.MaxActionsPerRule} actions.");
        }

        foreach (var action in actions)
        {
            if (!AutomationCatalog.TryGetAction(action.Kind ?? string.Empty, out var definition))
            {
                // Covers the interesting case: an action like "shell.run" simply does not
                // exist in the catalog, so it cannot be requested at all.
                errors.Add($"Unknown action '{action.Kind}'.");
                continue;
            }

            if (definition.RequiresArgument)
            {
                if (string.IsNullOrWhiteSpace(action.Argument))
                {
                    errors.Add($"Action '{definition.Kind}' requires an argument, e.g. '{definition.ArgumentExample}'.");
                    continue;
                }

                if (!definition.TryValidateArgument(action.Argument, out string? error))
                {
                    errors.Add($"Action '{definition.Kind}' has an invalid argument: {error}");
                    continue;
                }
            }
            else if (action.Argument is not null)
            {
                errors.Add($"Action '{definition.Kind}' does not take an argument.");
                continue;
            }

            ValidateActionCapability(definition, action, grantedPermissions, errors);
        }
    }

    private static void ValidateActionCapability(
        ActionDefinition definition,
        AutomationAction action,
        PermissionSet grantedPermissions,
        List<string> errors)
    {
        if (definition.RequiredCapability is null)
        {
            return;
        }

        string? scope = definition.CapabilityScopeIsArgument ? action.Argument : null;

        if (!grantedPermissions.IsGranted(definition.RequiredCapability, scope))
        {
            string declaration = scope is null
                ? definition.RequiredCapability
                : $"{definition.RequiredCapability}:{scope}";

            errors.Add($"Action '{definition.Kind}' requires the capability '{declaration}', " +
                       $"which this package has not been granted.");
        }
    }
}

public sealed class AutomationValidationResult
{
    private AutomationValidationResult(ValidatedAutomationRule? rule, IReadOnlyList<string> errors)
    {
        Rule = rule;
        Errors = errors;
    }

    public ValidatedAutomationRule? Rule { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Rule is not null;

    internal static AutomationValidationResult Succeeded(ValidatedAutomationRule rule) => new(rule, []);

    internal static AutomationValidationResult Failed(IReadOnlyList<string> errors) => new(null, errors);
}

/// <summary>
/// A rule that has passed validation against a specific permission set. Only obtainable
/// from <see cref="AutomationRuleValidator"/>, so holding one is evidence the rule was
/// checked and that its actions are within the package's granted capabilities.
/// </summary>
public sealed class ValidatedAutomationRule
{
    internal ValidatedAutomationRule(AutomationRule rule)
    {
        Id = rule.Id;
        Name = rule.Name;
        Enabled = rule.Enabled;
        Trigger = rule.Trigger!;
        Actions = rule.Actions.AsReadOnly();
    }

    public Guid Id { get; }

    public string Name { get; }

    public bool Enabled { get; }

    public AutomationTrigger Trigger { get; }

    public IReadOnlyList<AutomationAction> Actions { get; }
}
