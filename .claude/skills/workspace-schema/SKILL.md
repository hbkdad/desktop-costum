---
name: workspace-schema
description: Design or modify the workspace/layout schema (the format that captures containers, widgets, wallpapers and layout state for save/load/export).
---

Use when the workspace file format needs to change.

1. Schema lives under `docs/architecture/` (workspace schema section) as the source of truth, versioned explicitly (e.g. `schemaVersion` field in the format itself).
2. Every change must define a migration path from the previous version — old workspace files must keep loading, or fail with a clear, recoverable error, never silently corrupt.
3. Keep the schema serialization-format-agnostic in the spec even if the current implementation uses one format (e.g. JSON) — note the format explicitly where it matters (import/export, portability).
4. Add a round-trip test (save → load → compare) for any schema change.
5. Coordinate with `widget-builder` and `wallpaper-runtime` skills when the workspace schema embeds their manifests — don't let the two drift out of sync.
