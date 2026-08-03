---
name: product-requirements
description: Draft or update a section of the versioned PRD at docs/product/prd.md.
---

Use when adding or revising product requirements.

1. Edit `docs/product/prd.md` directly — it is the single versioned PRD, not a new file per feature.
2. Bump the version header and add a one-line changelog entry at the top of the file for any material change (new/removed requirement, changed acceptance criteria).
3. Every functional requirement needs: a rationale tied to a real user/job (link to `docs/research/` personas/JTBD once they exist), and testable acceptance criteria.
4. Respect non-negotiables from `AGENTS.md`/`CLAUDE.md` — a requirement that conflicts with a non-negotiable principle needs an ADR justifying the exception before it's added, not after.
5. Keep MVP-scope requirements clearly separated from post-MVP/backlog ideas — do not let scope creep into the MVP acceptance criteria silently.
