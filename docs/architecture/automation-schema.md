# Automation Rule Schema

**Schema version: 1**
Source of truth for automation rules. Implementation: `src/DesktopRuntime.Core/Automation/`. Governed by the `automation-rule` and `security-review` skills.

A rule is: *when this trigger fires, run these actions.*

```jsonc
{
  "schemaVersion": 1,
  "id": "<guid>",
  "name": "Save power on battery",
  "enabled": true,
  "trigger": { "kind": "power.source", "argument": "Battery" },
  "actions": [
    { "kind": "render.quality", "argument": "Minimal" }
  ]
}
```

## The central security property

**Automation cannot bypass the permission model.** Validation *requires* the package's `PermissionSet`, because a rule is only meaningful relative to what its package may do:

```csharp
AutomationRuleValidator.Validate(rule, grantedPermissions)
```

Without that coupling, a package could declare no capabilities and then reach outside the application through an automation rule instead. An action needing a capability is refused unless that exact capability — including its scope — was granted. `application.launch:some-app` requires `process.launch:some-app`; holding `process.launch:approved-app` does not authorise launching anything else.

Two further structural defences:

1. **The action catalog is closed.** There is deliberately no action that runs a command line, shell, script, or arbitrary binary. A test (`ActionCatalog_ContainsNoArbitraryExecutionAction`) fails the build if one is ever added.
2. **Application identifiers are validated by the same code path as `process.launch` capability scopes.** Reusing that check rather than writing a second one means a command line can never be accepted in one place and refused in the other — so `cmd.exe /c ...` fails both as a rule argument and as a declared capability.

## Triggers

| Kind | Argument |
|---|---|
| `application.start` / `application.exit` | declared application identifier |
| `monitor.connected` / `monitor.disconnected` | device interface path (`\\?\DISPLAY#…`) |
| `power.source` | `AC` or `Battery` |
| `time.schedule` | 24-hour time, `HH:mm` |
| `workspace.activated` | workspace id (GUID) |

## Actions

| Kind | Argument | Capability required |
|---|---|---|
| `workspace.activate` | workspace id | — |
| `application.launch` | declared application id | `process.launch:<argument>` |
| `widget.show` / `widget.hide` | widget id | — |
| `render.pause` / `render.resume` | — | — |
| `render.quality` | `Full`, `Reduced` or `Minimal` | — |

`application.launch` is the only action that reaches outside the application, and is therefore the only capability-gated one.

## Other rules

- A rule needs exactly one trigger and at least one action; at most `MaxActionsPerRule` (16) actions.
- Unknown trigger or action kinds are rejected loudly — a plausible typo like `application.started` must not silently do nothing.
- Arguments are validated per kind; an argument supplied to a kind that takes none is an error, as is a missing required argument.
- Rule names may not contain control characters, since rules are shown to the user when reviewing automation.
- A newer `schemaVersion` is rejected outright, consistent with the workspace and widget schemas.

## Not yet specified

Rule evaluation and scheduling at runtime, conflict resolution when several rules fire together, rate limiting, per-rule audit logging, and the user-facing automation editor. This document defines what a rule may *express* and how it is authorised — not how it executes.
