# Permission Model

Source of truth for the capability system. Implementation: `src/DesktopRuntime.Core/Permissions/`. Governed by the `security-review` skill.

Every widget, wallpaper, script and plugin — **including first-party sample content** — is treated as untrusted. A package receives exactly the capabilities it declares and nothing else.

## Manifest format

```jsonc
{
  "permissions": [
    "system.metrics.read",
    "network.domain:api.example.com",
    "files.user-selected.read",
    "clipboard.read-on-user-action",
    "process.launch:declared-application"
  ]
}
```

A declaration is either a bare capability name or `name:scope` for scoped capabilities.

## The catalog is closed

| Capability | Scoped | Grants |
|---|---|---|
| `system.metrics.read` | no | Read aggregate system metrics (CPU, memory, storage) for display. |
| `files.user-selected.read` | no | Read only files the user explicitly picked. No ambient filesystem access. |
| `clipboard.read-on-user-action` | no | Read the clipboard, only in direct response to a user action. |
| `network.domain` | host | Contact exactly one declared host. One entry per host. |
| `process.launch` | application id | Launch one specific declared application. Never a command line. |

**There is deliberately no capability for executing arbitrary PowerShell, CMD, JavaScript outside an isolated runtime, native DLLs, or unsigned binaries.** That is the primary defence, and it is enforced structurally rather than by filtering: no such capability exists to declare. A test (`Catalog_ContainsNoArbitraryExecutionCapability`) fails the build if a capability is ever added whose name suggests one.

## Enforcement rules

These are each covered by tests in `tests/DesktopRuntime.Core.Tests/Permissions/`:

1. **Default deny.** An empty permission set grants nothing. A request is refused unless an exactly-matching grant is present.
2. **No implicit widening.** There is no wildcard and no hierarchy. Holding one capability never implies another.
3. **Unknown capabilities are rejected at parse time, loudly.** A typo or a capability from a newer runtime fails validation rather than loading with a quietly reduced grant — silently dropping an entry would let a package appear to declare less than it does.
4. **A scoped capability is never satisfied without a scope.** Treating a missing scope as "any" would be precisely the implicit widening this model exists to prevent.
5. **Network grants are exact-host only.** A grant for `example.com` does **not** cover `sub.example.com` (no subdomain implication), `example.com.evil.com` (suffix attack), or `notexample.com` (prefix attack). Wildcards, schemes, paths and user-info are rejected in a declaration, because each invites parser-confusion tricks where the declared host is not the host actually contacted. Hosts are normalized to lowercase once at parse time and compared ordinally thereafter, since DNS is case-insensitive but string comparison should not be culture-sensitive.
6. **`process.launch` names an application, not a command.** Scopes containing whitespace, quotes, path separators, or shell metacharacters (`& | ; < > % $ \``) are rejected.

## Where enforcement physically happens

This document defines *what may be requested and how a request is evaluated*; it does not by itself sandbox a running package. That is [ADR-0004](adr/0004-process-and-isolation-model.md), which places package code in a sandboxed per-package process and — the essential rule — performs every permission check in the **core service**, on the trusted side of the IPC boundary. A sandboxed process never decides what it is allowed to do.

ADR-0004 is designed but **not built or validated**: Phase 3 Prototype 13 (plugin process isolation) has not been run.

## What this model still does not cover

Creator identity issuance and revocation, the marketplace review/scanning pipeline, and the user-facing consent prompt. Package signing policy is now covered by [`package-format.md`](package-format.md).
