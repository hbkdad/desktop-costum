using DesktopRuntime.Core.Automation;
using DesktopRuntime.Core.Permissions;

namespace DesktopRuntime.Core.Tests.Automation;

public class AutomationRuleValidatorTests
{
    private static AutomationRule CreateRule(AutomationTrigger trigger, params AutomationAction[] actions) => new()
    {
        Name = "Test rule",
        Trigger = trigger,
        Actions = [.. actions]
    };

    private static AutomationRule CreateValidRule() => CreateRule(
        new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "Battery" },
        new AutomationAction { Kind = AutomationCatalog.ActionSetRenderQuality, Argument = "Reduced" });

    [Fact]
    public void ValidRule_NeedingNoCapabilities_IsAcceptedWithAnEmptyPermissionSet()
    {
        var result = AutomationRuleValidator.Validate(CreateValidRule(), PermissionSet.Empty);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("Test rule", result.Rule!.Name);
    }

    // --- The central security property: automation cannot bypass the permission model ---

    [Fact]
    public void LaunchAction_IsRejected_WhenThePackageHasNoLaunchCapability()
    {
        // Without this, a package could declare no capabilities and then launch
        // applications through an automation rule instead.
        var rule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "AC" },
            new AutomationAction { Kind = AutomationCatalog.ActionLaunchApplication, Argument = "some-app" });

        var result = AutomationRuleValidator.Validate(rule, PermissionSet.Empty);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("process.launch:some-app"));
    }

    [Fact]
    public void LaunchAction_IsAccepted_WhenThatExactApplicationWasDeclared()
    {
        var permissions = PermissionSet.FromDeclarations(["process.launch:some-app"]);
        var rule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "AC" },
            new AutomationAction { Kind = AutomationCatalog.ActionLaunchApplication, Argument = "some-app" });

        Assert.True(AutomationRuleValidator.Validate(rule, permissions).IsValid);
    }

    [Fact]
    public void LaunchAction_IsRejected_WhenADifferentApplicationWasDeclared()
    {
        // The grant is per-application, so holding one launch capability must not
        // authorise launching something else.
        var permissions = PermissionSet.FromDeclarations(["process.launch:approved-app"]);
        var rule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "AC" },
            new AutomationAction { Kind = AutomationCatalog.ActionLaunchApplication, Argument = "other-app" });

        Assert.False(AutomationRuleValidator.Validate(rule, permissions).IsValid);
    }

    [Theory]
    [InlineData("shell.run")]
    [InlineData("process.execute")]
    [InlineData("script.eval")]
    [InlineData("powershell.invoke")]
    [InlineData("")]
    public void ActionsOutsideTheCatalog_AreRejected(string kind)
    {
        var rule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "Battery" },
            new AutomationAction { Kind = kind, Argument = "whatever" });

        var result = AutomationRuleValidator.Validate(rule, PermissionSet.Empty);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown action"));
    }

    [Theory]
    [InlineData("cmd.exe /c del *.*")]
    [InlineData("app & evil")]
    [InlineData("app | evil")]
    [InlineData("app; evil")]
    [InlineData(@"C:\Windows\System32\cmd.exe")]
    [InlineData("app \"arg\"")]
    public void CommandLinesDisguisedAsApplicationIdentifiers_AreRejected(string argument)
    {
        // Even granting the capability cannot help here: the argument fails validation
        // before the capability check, and the same rule is applied when parsing the
        // capability itself, so it could never have been declared either.
        var permissions = PermissionSet.FromDeclarations(["process.launch:approved-app"]);
        var rule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "AC" },
            new AutomationAction { Kind = AutomationCatalog.ActionLaunchApplication, Argument = argument });

        Assert.False(AutomationRuleValidator.Validate(rule, permissions).IsValid);
    }

    [Fact]
    public void ActionCatalog_ContainsNoArbitraryExecutionAction()
    {
        // Structural guard, mirroring the capability-catalog test: automation must not
        // become a second route to the arbitrary execution the permission model refuses.
        string[] forbiddenFragments = ["exec", "shell", "powershell", "cmd", "script", "eval", "command"];

        foreach (string action in AutomationCatalog.KnownActions)
        {
            foreach (string fragment in forbiddenFragments)
            {
                Assert.False(
                    action.Contains(fragment, StringComparison.OrdinalIgnoreCase),
                    $"Action '{action}' looks like an arbitrary-execution action ('{fragment}').");
            }
        }
    }

    // --- Trigger validation ---

    [Theory]
    [InlineData("application.started")]   // plausible typo, must not silently do nothing
    [InlineData("on.boot")]
    [InlineData("")]
    public void TriggersOutsideTheCatalog_AreRejected(string kind)
    {
        var rule = CreateRule(
            new AutomationTrigger { Kind = kind, Argument = "x" },
            new AutomationAction { Kind = AutomationCatalog.ActionPauseRendering });

        var result = AutomationRuleValidator.Validate(rule, PermissionSet.Empty);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown trigger"));
    }

    [Theory]
    [InlineData(AutomationCatalog.TriggerPowerSource, "Plugged in")]
    [InlineData(AutomationCatalog.TriggerPowerSource, "battery")]     // case matters here
    [InlineData(AutomationCatalog.TriggerTimeSchedule, "10pm")]
    [InlineData(AutomationCatalog.TriggerTimeSchedule, "25:00")]
    [InlineData(AutomationCatalog.TriggerWorkspaceActivated, "not-a-guid")]
    [InlineData(AutomationCatalog.TriggerMonitorConnected, "DISPLAY1")]
    public void InvalidTriggerArguments_AreRejected(string kind, string argument)
    {
        var rule = CreateRule(
            new AutomationTrigger { Kind = kind, Argument = argument },
            new AutomationAction { Kind = AutomationCatalog.ActionPauseRendering });

        Assert.False(AutomationRuleValidator.Validate(rule, PermissionSet.Empty).IsValid);
    }

    [Fact]
    public void TriggerRequiringAnArgument_IsRejectedWithoutOne()
    {
        var rule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerTimeSchedule },
            new AutomationAction { Kind = AutomationCatalog.ActionPauseRendering });

        Assert.False(AutomationRuleValidator.Validate(rule, PermissionSet.Empty).IsValid);
    }

    [Fact]
    public void ValidMonitorAndScheduleTriggers_AreAccepted()
    {
        var monitorRule = CreateRule(
            new AutomationTrigger
            {
                Kind = AutomationCatalog.TriggerMonitorConnected,
                Argument = @"\\?\DISPLAY#AOP0806#4&1427843b&0&UID198147#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}"
            },
            new AutomationAction { Kind = AutomationCatalog.ActionResumeRendering });

        var scheduleRule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerTimeSchedule, Argument = "22:30" },
            new AutomationAction { Kind = AutomationCatalog.ActionSetRenderQuality, Argument = "Minimal" });

        Assert.True(AutomationRuleValidator.Validate(monitorRule, PermissionSet.Empty).IsValid);
        Assert.True(AutomationRuleValidator.Validate(scheduleRule, PermissionSet.Empty).IsValid);
    }

    // --- Shape validation ---

    [Fact]
    public void RuleWithNoActions_IsRejected()
    {
        var rule = CreateRule(new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "AC" });

        Assert.False(AutomationRuleValidator.Validate(rule, PermissionSet.Empty).IsValid);
    }

    [Fact]
    public void RuleWithNoTrigger_IsRejected()
    {
        var rule = new AutomationRule
        {
            Name = "No trigger",
            Actions = [new AutomationAction { Kind = AutomationCatalog.ActionPauseRendering }]
        };

        Assert.False(AutomationRuleValidator.Validate(rule, PermissionSet.Empty).IsValid);
    }

    [Fact]
    public void RuleExceedingTheActionLimit_IsRejected()
    {
        var actions = Enumerable
            .Range(0, AutomationSchema.MaxActionsPerRule + 1)
            .Select(_ => new AutomationAction { Kind = AutomationCatalog.ActionPauseRendering })
            .ToArray();

        var rule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "AC" },
            actions);

        Assert.False(AutomationRuleValidator.Validate(rule, PermissionSet.Empty).IsValid);
    }

    [Fact]
    public void ActionTakingNoArgument_IsRejectedWhenGivenOne()
    {
        var rule = CreateRule(
            new AutomationTrigger { Kind = AutomationCatalog.TriggerPowerSource, Argument = "AC" },
            new AutomationAction { Kind = AutomationCatalog.ActionPauseRendering, Argument = "unexpected" });

        Assert.False(AutomationRuleValidator.Validate(rule, PermissionSet.Empty).IsValid);
    }

    [Fact]
    public void NewerRuleSchemaVersion_IsRejected()
    {
        var rule = CreateValidRule();
        rule.SchemaVersion = AutomationSchema.CurrentVersion + 1;

        var result = AutomationRuleValidator.Validate(rule, PermissionSet.Empty);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("newer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RuleNameWithControlCharacters_IsRejected()
    {
        var rule = CreateValidRule();
        rule.Name = "Night" + (char)0x1b + "[2J";

        Assert.False(AutomationRuleValidator.Validate(rule, PermissionSet.Empty).IsValid);
    }
}
