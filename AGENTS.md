# Repository Guidance

This repository implements a Windows 11 desktop runtime for workspaces, containers, widgets, animated wallpapers, automation and creator tools.

Never use Forge-related branding, names or themes.

## Required Workflow

1. Read `.agents/state/current-phase.md`.
2. Read `.agents/state/handoff.md`.
3. Inspect relevant code, tests, ADRs and specifications.
4. Make the smallest correct change.
5. Run appropriate validation.
6. Update project state after meaningful work.

## Non-Negotiable Rules

- Prefer supported Windows APIs.
- Isolate Explorer-specific implementation.
- Maintain safe fallbacks.
- Treat external packages as untrusted.
- Default-deny plugin permissions.
- Do not execute arbitrary imported scripts.
- Avoid UI-thread blocking.
- Clean up native resources.
- Preserve multi-monitor and DPI correctness.
- Add tests for behaviour changes.
- Never claim success without build or runtime evidence.

## Context Efficiency

Keep permanent instructions short. Use skills and topic documentation for detailed workflows. Do not load unrelated files.

## Task Report

Return status, files changed, validation, test results, limitations and next task.
