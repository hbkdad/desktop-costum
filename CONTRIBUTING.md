# Contributing

## Branch policy

- `main` is the trunk; keep it always buildable and passing CI.
- Work in short-lived feature branches (`feature/<short-slug>`, `fix/<short-slug>`, `docs/<short-slug>`, `prototype/<short-slug>` for Phase 3 throwaway work).
- Open a PR into `main`; do not push directly to `main` once branch protection is enabled (owner decision, see `backlog/task-backlog.md`).
- Squash or keep history readable at merge time — prefer commits that each build and pass tests over a sprawling WIP history.
- Prototype branches (`prototype/*`) are explicitly allowed to be messy; they are discarded or formalized via an ADR, never merged as-is into `main`.

## Coding standards

- C#, current supported .NET release (see `backlog/dependency-register.md` for the SDK version in use).
- Nullable reference types and implicit usings enabled by default for new projects.
- Keep UI code (WinUI 3 views/view-models) separate from domain logic; keep Explorer-specific/native-interop code isolated behind an adapter (see `AGENTS.md`).
- Add or update a test for every behaviour change — no exceptions for "small" changes.
- Prefer small, vertical changes over broad refactors; do not modify unrelated files in the same change.
- Run `dotnet build` and the relevant `dotnet test` project(s) before claiming a task complete; broader test runs are required when shared/core code changes.
- Use cancellation tokens for long-running operations; dispose native resources deterministically; never swallow exceptions silently.
- Follow the Definition of Done in `backlog/definition-of-done.md` before marking any task finished.

## Where things live

See [`README.md`](README.md) for the repository layout, and `CLAUDE.md`/`AGENTS.md` for the compact operating rules AI coding agents follow in this repo.
