# Resource Accounting

Source of truth for how declared resource budgets are checked against reality. Implementation: `src/DesktopRuntime.Core/Resources/`. Governed by the `performance-test` skill.

Addresses the **#2 market gap** in `docs/research/market-gap-report.md`: every animated-wallpaper competitor researched draws documented CPU/GPU/battery complaints.

## Why this exists

A widget manifest declares a resource budget, but that is only the author's **claim** — `docs/architecture/widget-manifest.md` says so explicitly. The ledger is what turns the claim into something checkable. Without it, "we respect your battery" is a marketing line rather than a property of the system.

## Model

- `Register(widgetId, declaredBudget)` — a widget must be registered before it can report. Recording a sample for an unregistered widget **throws**, so a widget cannot escape accounting by never registering.
- `Record(widgetId, sample)` — one observation: CPU %, memory MB, timestamp.
- `GetStatus(widgetId)` → `Unknown` | `WithinBudget` | `Spiking` | `SustainedBreach`.
- `GetSustainedBreaches()` — offenders, worst CPU overshoot first.
- `GetTotals()` — aggregate across all tracked widgets.

## Two decisions worth calling out

**1. A breach must be sustained, not instantaneous.** A widget waking to redraw, or a GC pause, is not misbehaviour. A sample over budget marks the widget `Spiking`; only continuous overuse beyond `BreachTolerance` (default 30 s) becomes a `SustainedBreach`. Acting on single samples would produce the false positives that make a resource governor annoying rather than useful — the same failure mode as the maximized-window bug found in Prototype 10.

Any compliant sample clears the breach and restarts the clock: the widget is behaving again.

A `ToleranceFactor` (default 1.25×) additionally absorbs measurement noise, so a widget declaring 1.0% CPU is not flagged at 1.2%.

**2. Totals matter independently of per-widget status.** Ten widgets each comfortably inside their own budget can still add up to an unacceptable total. `GetTotals()` exists so the host can act on the aggregate even when no individual widget is at fault — pinned by a test that has ten compliant widgets summing to 9% CPU and 300 MB.

## Deliberately not enforcement

The ledger performs no I/O, reads no clock, and takes no action. Callers supply samples with timestamps and decide what to do with a verdict — throttle, pause, warn the user, or surface it in a marketplace listing. Keeping measurement separate from policy makes hour-long scenarios deterministic to test, and leaves the host free to choose different responses in different modes (on battery, during fullscreen, in safe mode).

## Not yet specified

Where samples come from (per-process counters, job objects), GPU accounting, enforcement policy, how breaches are surfaced to the user or to a marketplace listing, and whether persistent offenders are blocked from installation. Sampling cost itself must stay negligible — Prototype 10 measured the comparable fullscreen check at ~0.7 ms, which is the right order of magnitude to aim for.
