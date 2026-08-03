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

## Repository layout

```
src/            .NET solution (grows through Phase 5+)
tests/          automated tests
docs/           product, research and architecture documentation
backlog/        task backlog, risk/dependency registers, DoD, prototype backlog
.agents/        state files and agent role prompts
.claude/skills/ reusable task procedures for coding agents
.github/        CI workflows
```
