---
name: performance-test
description: Define or run a performance benchmark against the project's CPU/GPU/memory/battery/startup budgets.
---

Use before a rendering/runtime feature ships, or when validating a performance regression fix.

1. Run against the standard benchmark profiles (see `.agents/prompts/performance-agent.md`): static wallpaper/no widgets; video wallpaper + 5 widgets; WebView wallpaper + 10 widgets; 3-monitor; fullscreen/gaming; battery mode; Explorer restart; renderer crash; hundreds of shortcuts.
2. Report idle, active, and fullscreen-mode numbers separately — a feature that's cheap idle but expensive active still needs the active number reported.
3. Compare against the budget defined for that module; a feature with no defined budget yet must get one before this test is considered complete, not after.
4. No feature is "done" without a reported resource-impact number — this is a Definition of Done requirement, not optional polish.
5. Wire repeatable benchmarks into CI as a gate once the module has a stable harness; note in `backlog/dependency-register.md` if CI can't yet run this class of test (e.g. needs a GPU-capable runner).
