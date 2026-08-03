# 0004. Process and isolation model

## Status

**Accepted.** Validated on real hardware, unelevated, across two prototypes:

| Claim | Evidence |
|---|---|
| Job memory caps are genuinely **enforced** | A 512 MB allocation was refused under a 146 MB cap (`prototypes/process-isolation-poc/`) |
| Per-job accounting is readable from the trusted side | ~**0.17 ms** per query — a viable `ResourceLedger` sample source, observed *about* the sandbox rather than self-reported *by* it |
| A process can be **launched into** an AppContainer | Control process ran inside a container and returned its exit code (`prototypes/appcontainer-launch-poc/`) |
| Default-deny actually **denies** | A file readable outside the container was refused inside it, with zero capabilities granted — a differential test |

**Still to be exercised (engineering, not open research):** restricted tokens; granting the container SID read+execute on our own install directory (required for our binary, unlike the `cmd.exe` used in the probe); positive tests that *granted* capabilities work, not only that ungranted ones are denied; and CPU capping — only memory was tested, and CPU rate control has different semantics that can terminate rather than throttle, which would be wrong for a widget.

## Context

`docs/architecture/permission-model.md` defines what a package *may request*, and says plainly that it "does not by itself sandbox a running package." Something has to make a denied capability actually impossible rather than merely undeclared. That is a process-boundary question, not a code-structure one.

Three further constraints bear on it:

- **Crash isolation.** Phase 3 found the shell-integration layer is the fragile part (ADR-0003), and competitor research found Explorer crash loops are the most visible failure mode in this category. A failure there must not take down the application.
- **Resource discipline.** `docs/architecture/resource-accounting.md` *measures* budget breaches but deliberately does not enforce them. Hard limits need an OS mechanism.
- **Low idle cost.** A process per widget would be prohibitive — the product must be cheap at idle, which is the #2 market gap.

## Decision

### Processes

| Process | Trust | Rationale |
|---|---|---|
| **App shell** (WinUI 3) | Full user | Foreground UI. Its crash is recoverable and visible. |
| **Core service** | Full user | Workspace state, SQLite, automation engine, permission decisions. The authority. |
| **Desktop host** | Full user | Owns shell integration (ADR-0003 tiers) — the fragile part, isolated so its crash cannot take down the shell process or the service. |
| **Wallpaper renderer** | Full user, resource-capped | Media/GPU work. A codec or driver fault must not kill desktop hosting. |
| **Widget host** | **Sandboxed**, one per package | Runs untrusted package code. |

### One widget-host process per *package*, not per widget

A process per widget instance would mean a separate process for a clock and a CPU meter — unacceptable against the idle-cost budget. A single shared host for all packages would mean one author's code sharing an address space with another's, which defeats the point.

Per-package is the defensible middle: the isolation boundary matches the trust boundary, since a package is the unit that is authored, signed, and granted capabilities.

### Sandboxed processes get

- An **AppContainer** with a package-specific SID, so filesystem and network access are denied by default at the OS level rather than by our own checks.
- A **restricted token** dropping unneeded privileges.
- A **job object** with hard memory and CPU caps derived from the manifest's declared budget. This is what turns `ResourceLedger`'s measurement into enforcement: the ledger observes and reports; the job object makes the ceiling real.

### Permission checks happen on the trusted side of the IPC boundary

A sandboxed process never enforces its own limits. It *asks* the core service, and the service checks the package's `PermissionSet` before acting. A compromised widget host can therefore issue any request it likes and still gain nothing it was not granted.

This is the single most important rule here: any design where the sandboxed side decides what it is allowed to do is wrong, regardless of how carefully that code is written.

### IPC

Named pipes with an explicit message contract per direction, and per-connection identification of which package is on the other end. Message shapes are versioned like every other schema in this project: unknown or newer versions are rejected, never partially interpreted.

## Consequences

- **Crash recovery becomes per-process.** `ShellRecoverySupervisor` currently models shell restarts only; the same detect→wait→relaunch→backoff→safe-mode policy generalises to renderer and widget-host processes, and should be reused rather than reinvented per process type.
- **A widget cannot exceed its declared budget**, rather than merely being reported for it — but only once job-object limits are wired up, which is not done.
- **Process count grows with installed packages.** Idle cost must be measured against the benchmark profiles as package count rises; this is a real risk to the idle-cost budget and belongs in the performance work.
- **IPC latency enters the widget update path.** Widgets are event-driven and low-frequency by design, so this should be acceptable — but it is an assumption, not a measurement.
- **The Phase 5 blocker is cleared.** Prototypes 13 and 13b together validate the load-bearing basis of this model: caps are enforced, accounting is cheap and trustworthy, processes launch into containers, and default-deny denies. What remains is engineering with known shapes rather than open research, so Phase 5 can proceed on validated ground.
