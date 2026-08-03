---
updated: 2026-08-02
---

# Handoff

## Where things stand

15 commits pushed to `main` on `github.com/hbkdad/desktop-costum`. **CI green on every push** (verified via `gh run list`). Build clean, **156/156 tests passing**.

Phases 0 and 1 complete. Phase 2 partial (PRD §1–4, §13). Phase 3: 6 of 13 prototypes, including all three "recommended first". Phase 4: 7 deliverables done.

## Real, tested code (`src/DesktopRuntime.Core/`)

| Area | What it guarantees |
|---|---|
| `Workspaces/` | Versioned serializer + resolver that **defers rather than discards** layouts for absent monitors (lossless undock/redock) |
| `Permissions/` | Closed capability catalog with no arbitrary-execution capability *by construction*; exact-host network scoping |
| `Widgets/` | Manifest validation producing a distinct validated type; allowlist ids; required resource budget |
| `Automation/` | Rules validated **against the package's permission set**, so automation cannot bypass permissions |
| `Wallpapers/` | ADR-0003 made executable: video degrades to static **visibly**, never silently |
| `Recovery/` | Detect→wait→relaunch→backoff→safe-mode; attempt counter resets only on *sustained* health, so a flapping shell still trips the breaker |
| `Resources/` | Declared widget budgets made checkable; sustained-breach detection plus aggregate totals |

Each has a source-of-truth doc in `docs/architecture/`. All are pure policy — no I/O, no clock reads — so scenarios spanning hours are deterministic and run in milliseconds.

## Design themes worth preserving

Two patterns recur and are worth keeping as new areas are added:

1. **Structural over filtered.** Security properties come from things not existing (no execution capability, no shell action) rather than from blocklists. Tests fail the build if one is ever introduced.
2. **Sustained over instantaneous.** Both the recovery supervisor and the resource ledger deliberately refuse to act on a single observation — and in both cases the *reset* rule is the subtle part, because resetting eagerly defeats the breaker.

## Open — needs the owner

1. **Two hardware validation gaps** (`backlog/risk-register.md`): no second monitor, no battery. Both block MVP acceptance criteria already in the PRD, and each maps to an MVP-primary persona. The only items genuinely blocked on something I cannot do.
2. Repo visibility / branch protection on `main`.
3. Codex skill-mirror path — unconfirmed, nothing fabricated.
4. Product name — unresolved.
5. Pricing hypotheses unvalidated with users; Reddit-corroboration gap in the competitor matrix.

## Recommended next task

Remaining Phase 4: package format + signing, IPC contracts, database design, rendering pipeline, system context/process diagrams. **Package format is the highest-value next step** — it is the trust boundary for everything the marketplace will distribute, and it composes the widget manifest, permission model and automation schema already built.

Alternatively, populate PRD §5–12, which now has substantial technical grounding.

## Exact prompt to continue

> Read `.agents/state/current-phase.md` and this handoff. Continue Phase 4 with the package format and signing model: a source-of-truth doc in `docs/architecture/`, plus a validated type in `src/DesktopRuntime.Core/` following the pattern in `Widgets/` and `Automation/` (untrusted input → validator → validated type, abuse-case tests). Then update state files, run `dotnet test`, and commit/push per standing authorization.
