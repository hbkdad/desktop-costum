# Shell Recovery Model

Source of truth for how the desktop host responds to the Windows shell (explorer.exe) disappearing. Implementation: `src/DesktopRuntime.Core/Recovery/`.

This addresses the **#1 market gap** in `docs/research/market-gap-report.md`: every competitor researched has documented Windows-update or Explorer-restart breakage, and several have documented crash/restart *loops*.

## Policy: detect, wait, then relaunch

**Never relaunch immediately.** Phase 3 Prototype 3 (`prototypes/explorer-restart-recovery-poc/REPORT.md`) found that Windows 11 restarts explorer.exe on its own, and that a manual relaunch races it — producing a duplicate process and a stray Explorer window. Measured OS self-recovery was ~2.7 s, so the default grace period is 5 s.

| Observation | Action |
|---|---|
| Shell present, healthy | `None` |
| Shell missing, within grace period | `Wait` — the OS may fix this itself |
| Shell missing, grace elapsed | `Relaunch` |
| Shell missing, within backoff interval | `Wait` |
| Attempts exhausted | `EnterSafeMode` |

Backoff doubles from `BaseBackoff` (2 s) up to `MaxBackoff` (60 s), so a persistent failure does not become a tight retry loop of our own making.

## The reset rule is the important part

The attempt counter resets only after the shell has been **continuously healthy for `StabilityWindow`** (1 minute) — *not* the moment it reappears.

That distinction is the whole design. A shell that dies again a few seconds after every restart is precisely the crash/restart loop documented across competitors. If the counter reset on mere reappearance, that scenario would relaunch forever and never trip the breaker. The test `AShellThatKeepsDyingShortlyAfterEachRestart_StillTripsTheBreaker` pins this: across ten flap cycles, exactly `MaxAttempts` relaunches occur and the supervisor then stays in safe mode.

Safe mode is sticky, and is left only by the same sustained-health rule — so recovery is automatic, but never eager.

## Handles must be re-acquired, never reused

Prototype 3 also confirmed that Progman's window handle changes across a restart and the old handle fails `IsWindow`. Any Tier-1 attachment state (per [ADR-0003](adr/0003-desktop-hosting-strategy.md)) must be treated as invalid the moment a restart is detected, and re-acquired from scratch. The supervisor signals *when* to do that; acquiring handles is the desktop host's job.

## Testability

`ShellRecoverySupervisor` is pure policy: it performs no I/O and reads no clock. The caller supplies both the observation and the timestamp, so every scenario above — including hour-long flap sequences — is deterministic and runs in milliseconds.

## Not yet specified

Detecting shell loss (the observation source), the safe-mode user experience, what state is snapshotted for restoration, and recovery for the runtime's own renderer/widget-host processes as distinct from the shell. This document covers the shell-restart policy only.
