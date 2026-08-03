# Project Instructions

Build a secure, performant Windows 11 desktop runtime combining workspaces, desktop containers, widgets, animated wallpapers, automation and creator tools.

Never use Forge-related names, branding, terminology or themes.

## Before Work

1. Read `.agents/state/current-phase.md`.
2. Read `.agents/state/handoff.md`.
3. Inspect relevant specifications, ADRs and tests.
4. Search before creating duplicate code or documentation.

## Engineering Rules

- Prefer supported Windows APIs.
- Isolate Explorer-specific and undocumented behaviour.
- Maintain overlay and safe-mode fallbacks.
- Treat imported packages as hostile.
- Default-deny permissions.
- Keep core features offline-capable.
- Prioritize low idle resource use.
- Add tests for behaviour changes.
- Build and test before claiming completion.
- Do not modify unrelated files.

## Context Rules

Keep this file compact. Store detailed guidance in `docs/`, `.claude/skills/` and `.agents/prompts/`. Update state and handoff files after major work.

## Completion

Report objective, files changed, tests, results, risks and next action.
