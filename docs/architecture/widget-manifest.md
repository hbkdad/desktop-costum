# Widget Manifest

**Manifest version: 1**
Source of truth for the widget package manifest. Implementation: `src/DesktopRuntime.Core/Widgets/`. Governed by the `widget-builder` and `security-review` skills.

## Format

```jsonc
{
  "manifestVersion": 1,
  "id": "com.example.clock",
  "name": "Clock",
  "version": "1.0.0",
  "author": "Example",

  // Default-deny: anything not listed here is refused. See permission-model.md.
  "permissions": ["system.metrics.read"],

  "sizes": [
    { "name": "small",  "width": 200, "height": 100 },
    { "name": "medium", "width": 400, "height": 200 }
  ],

  // Required. A widget that cannot state its resource cost cannot be accepted.
  "resourceBudget": {
    "idleCpuPercent": 0.1,
    "memoryMb": 32,
    "framesPerSecond": 1      // 0 = event-driven: redraws only when its data changes
  }
}
```

## Validated vs as-authored

`WidgetManifest` is the **untrusted, as-authored** form. Nothing in it is believed until it passes `WidgetManifestValidator`, which returns a `ValidatedWidgetManifest` carrying the parsed `PermissionSet`. Runtime code should accept only the validated type, so an unchecked manifest cannot reach the runtime by accident — the type system carries the guarantee rather than relying on every caller remembering to validate.

Validation collects **all** errors rather than throwing on the first, so an author sees everything wrong at once.

## Rules and why

Each is covered by a test in `tests/DesktopRuntime.Core.Tests/Widgets/`:

- **Id is strictly constrained** to dot-separated lowercase alphanumeric segments (`com.example.clock`). The id names on-disk storage for the package, so path traversal (`../`), path separators, drive letters, null bytes, whitespace and uppercase (which would collide case-insensitively on Windows) are excluded *by construction* via an allowlist pattern, not by a blocklist of known-bad inputs.
- **Unknown permissions fail the whole manifest**, rather than being dropped. A package must never appear to declare less than it does.
- **Declaring no permissions is valid** — and is the safest possible manifest.
- **A resource budget is required**, and is range-checked. Resource discipline is the #2 market gap in `docs/research/market-gap-report.md`; a widget with an unstated cost cannot be reasoned about. The ceilings (`MaxIdleCpuPercent`, `MaxMemoryMb`, `MaxFramesPerSecond`) bound what is *expressible*; the real per-scenario budgets live with the Performance Agent's benchmark profiles.
- **Sizes are bounded and positive.** An unbounded declared surface is a cheap way to force enormous allocations.
- **Display names may not contain control characters**, which can otherwise spoof what the user sees in a consent prompt.
- **A newer `manifestVersion` is rejected outright**, never partially interpreted — the same rule as the workspace schema.

## Declared vs actual cost

The budget in a manifest is the author's **claim**; nothing in *this* document verifies it. Checking claims against measured use is `docs/architecture/resource-accounting.md`, which compares samples to the declared budget and reports sustained breaches. What to *do* about an offender — throttle, pause, warn, or reflect it in a marketplace listing — is still open (Phase 6–8).

## Not yet specified

Widget content/entry point (the actual renderable), data bindings and the expression system, per-widget settings schema, localization, and package signing. The workspace schema's per-widget `settings` bag is deliberately opaque so widget authoring can evolve without forcing a workspace schema version bump.
