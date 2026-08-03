# 0001. Record architecture decisions

## Status

Accepted

## Context

The project will make many significant technical decisions (process model, rendering pipeline, permission model, packaging format, and any exception to a non-negotiable principle such as adopting Electron). These need a durable, reviewable record independent of chat history or a single agent's working memory.

## Decision

We will use lightweight Architecture Decision Records, one per significant decision, numbered sequentially in `docs/architecture/adr/`, following: Status / Context / Decision / Consequences. The `architecture-decision` skill governs the authoring process.

## Consequences

Every significant technical decision must have a corresponding ADR before code depends on it. `.agents/state/decisions.md` holds only a one-line pointer to each ADR, keeping the permanent-context file compact per the project's context-efficiency rules.
