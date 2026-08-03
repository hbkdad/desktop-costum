---
name: desktop-host-prototype
description: Build and evaluate a Phase 3 feasibility prototype for desktop/shell integration (attachment, overlay fallback, Explorer restart recovery, per-monitor hosting, DPI persistence, fullscreen detection, etc.).
---

Use for any item in `backlog/prototype-backlog.md`.

1. Build the prototype isolated from production code (a throwaway or clearly-labeled `prototypes/` project) — never merge it into the main solution directly.
2. Report, per prototype, all of: purpose, implementation summary, test method, measurements, limitations, Windows build tested on, recommendation, and a keep/discard decision.
3. Store the report next to the backlog entry it answers (append to `backlog/prototype-backlog.md` or link a file under `docs/architecture/prototypes/`).
4. A prototype result that changes architectural direction needs an ADR (`architecture-decision` skill) before anything is built on top of it — a prototype report alone is not a merge gate.
5. If the technique is undocumented/compatibility-sensitive, the report must state what the fallback mode is and whether it was also tested.
