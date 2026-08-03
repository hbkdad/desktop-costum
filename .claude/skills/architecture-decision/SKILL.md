---
name: architecture-decision
description: Author an Architecture Decision Record (ADR) for a significant technical decision.
---

Use for any decision that affects module boundaries, process/isolation model, a core schema, or a non-negotiable principle — including any proposal to use Electron, a kernel driver, DLL injection, or an unrestricted global hook (all require an ADR to even be considered, per the non-negotiable principles).

1. File: `docs/architecture/adr/NNNN-short-title.md`, numbered sequentially from the last ADR in that directory.
2. Format (Nygard-style): `Status` (Proposed/Accepted/Superseded), `Context`, `Decision`, `Consequences` (including what this rules out).
3. Link the ADR from `.agents/state/decisions.md` with a one-line summary — do not duplicate the full rationale there.
4. If the ADR reverses or supersedes an earlier one, mark the old one `Superseded by NNNN` rather than deleting it.
5. Do not merge Phase 3 prototype code into production architecture based on a prototype result alone — the ADR is the review gate.
