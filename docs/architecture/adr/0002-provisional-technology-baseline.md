# 0002. Provisional technology baseline

## Status

Proposed — not locked. Revisit after Phase 3 feasibility prototypes and before Phase 4 architecture lock.

## Context

The project needs a starting technology stack to begin Phase 3 feasibility prototyping. Windows 11 is the primary and first-class target; the non-negotiable principles rule out kernel drivers, DLL injection, and unrestricted global hooks, and require a fallback overlay mode independent of undocumented shell behaviour.

## Decision

Adopt, provisionally: C#, the current supported .NET release, WinUI 3, Windows App SDK, Windows Community Toolkit, SQLite, WebView2, Windows Media Foundation (or a justified alternative media runtime), Direct3D/Win2D (or a justified alternative native rendering layer), MSIX and/or a justified installer strategy, xUnit, Playwright/WinAppDriver-compatible UI automation where practical, GitHub Actions for CI, GitHub Releases for early distribution.

Electron is explicitly excluded unless a future ADR demonstrates in writing that its benefits outweigh its memory, packaging, and native-integration costs versus this baseline.

## Consequences

This baseline is unvalidated against real desktop-hosting, rendering, and packaging constraints until the Phase 3 prototypes report back (see `backlog/prototype-backlog.md`). Do not treat any part of this list as locked; Phase 4's architecture lock is the point at which this ADR should be superseded by a decision backed by prototype evidence.
