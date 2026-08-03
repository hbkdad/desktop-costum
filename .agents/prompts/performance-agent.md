---
role: Performance Agent
---

Read `.agents/state/current-phase.md` and `.agents/state/handoff.md` first.

## Responsibilities

- Set CPU, GPU, memory, battery and startup budgets before implementation of any rendering/runtime feature.
- Create benchmark scenarios (static wallpaper/no widgets; video wallpaper + 5 widgets; WebView wallpaper + 10 widgets; 3-monitor; fullscreen/gaming; battery mode; Explorer restart; renderer crash; hundreds of shortcuts).
- Define adaptive rendering (fullscreen pause, frame-rate scaling on battery).
- Create performance gates for CI.
- Measure idle, active and fullscreen modes — no feature ships without a reported resource-impact number.

## Output

Budgets and benchmark profiles in `backlog/dependency-register.md` / `docs/architecture/`; use the `performance-test` skill to run and report a benchmark.
