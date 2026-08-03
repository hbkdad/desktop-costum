# Desktop Runtime (internal placeholder name)

A Windows 11 desktop creation and runtime platform: workspaces, desktop containers, widgets, animated wallpapers, automation and a creator studio, built as independently recoverable modules on supported Windows APIs.

Product name is unresolved — `Desktop Runtime` is an internal placeholder only. Never use Forge-related names, branding or themes.

## Start here

- [`docs/architecture/system-context.md`](docs/architecture/system-context.md) — **start here for orientation**: what the pieces are, how they relate, and which of them exist yet.
- [`CLAUDE.md`](CLAUDE.md) / [`AGENTS.md`](AGENTS.md) — compact operating rules for AI coding agents working in this repo.
- [`.agents/state/current-phase.md`](.agents/state/current-phase.md) — what phase the project is in right now.
- [`.agents/state/handoff.md`](.agents/state/handoff.md) — the next task and open questions.
- [`docs/product/current-state.md`](docs/product/current-state.md) — product snapshot.
- [`docs/product/prd.md`](docs/product/prd.md) — versioned product requirements.
- [`.claude/skills/`](.claude/skills/) — task-specific procedures (research, ADRs, prototypes, security review, etc.).
- [`.agents/prompts/`](.agents/prompts/) — role prompts for the specialized planning agents (research, architecture, security, UX, performance, commercial, documentation).

## Try it

There is no UI yet (see the tooling note in [ADR-0002](docs/architecture/adr/0002-provisional-technology-baseline.md)), but the runtime is runnable today:

```bash
dotnet run --project src/DesktopRuntime.Cli -- monitors
```

That reports the monitors the runtime can see, whether the animated-wallpaper surface is available on your system, and what the current wallpaper is. `new`, `set-wallpaper`, `activate`, `list` and `delete` complete the loop.

## Repository layout

```
src/
  DesktopRuntime.Core/        domain logic — no I/O, no OS calls, fully unit-tested
  DesktopRuntime.DesktopHost/ the Windows adapter (all P/Invoke lives here)
  DesktopRuntime.Cli/         `desktopruntime` — the interim runnable shell
tests/          unit tests plus integration tests against the real Windows APIs
prototypes/     throwaway Phase 3 feasibility probes, each with a REPORT.md
docs/           product, research and architecture documentation
backlog/        task backlog, risk/dependency registers, DoD, prototype backlog
.agents/        state files and agent role prompts
.claude/skills/ reusable task procedures for coding agents
.github/        CI workflows
```
