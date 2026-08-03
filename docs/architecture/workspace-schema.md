# Workspace Schema

**Schema version: 1**
Source of truth for the workspace/layout format. Implementation: `src/DesktopRuntime.Core/Workspaces/`. Governed by the `workspace-schema` skill.

A *workspace* is a named, saveable, restorable arrangement of desktop containers, widgets and per-monitor wallpapers. It is the unit users create, switch between, import and export (Phase 5, Slice 1).

## Design decisions and why

These are not arbitrary — each follows from a Phase 3 prototype result:

1. **Monitors are identified by device interface path, never by `\\.\DISPLAYn` or `HMONITOR`.** Prototype 9 (`prototypes/monitor-dpi-persistence-poc/REPORT.md`) established that only the device interface path carries hardware/EDID identity; the others are positional or runtime-only. Persisting either would silently move a user's layout to the wrong screen after a reconnect.
2. **Container and widget bounds are stored relative to their monitor, not in global virtual-desktop coordinates.** If a monitor's position in the virtual desktop changes (a very common result of unplugging/replugging), globally-positioned content would drift; monitor-relative content does not. This directly serves Job #1 in `docs/product/jobs-to-be-done.md`.
3. **Monitor geometry (bounds, DPI, primary flag) is an *attribute* of an identified monitor, not part of its identity.** A resolution change updates a monitor's record rather than creating a second one.
4. **A wallpaper assignment records what the user *requested*, not what the runtime managed to deliver.** Per [ADR-0003](adr/0003-desktop-hosting-strategy.md), a video wallpaper may be served by Tier 1 (WorkerW attach) or fall back to a static tier. Which tier actually served it is *runtime state*, not saved configuration — persisting a degraded state would make the degradation sticky across sessions, which is exactly the silent-failure behaviour `docs/product/prd.md` §13.7 forbids.
5. **Layouts for monitors that are not currently connected are preserved, not discarded.** Loading a 3-monitor workspace on a laptop with 1 screen must not destroy the other two monitors' layouts — undocking and redocking has to be lossless.

## Structure

```jsonc
{
  "schemaVersion": 1,
  "id": "<guid>",
  "name": "Focus",
  "createdUtc": "2026-08-02T00:00:00Z",
  "modifiedUtc": "2026-08-02T00:00:00Z",

  "monitors": [
    {
      // Identity — the only stable key (Prototype 9).
      "deviceInterfacePath": "\\\\?\\DISPLAY#AOP0806#4&1427843b&0&UID198147#{e6f07b5f-...}",
      // Diagnostic only. Never used for matching: not unique across identical models.
      "friendlyName": "Generic PnP Monitor",
      // Attributes captured at save time, refreshed on load. Not identity.
      "bounds": { "x": 0, "y": 0, "width": 1920, "height": 1080 },
      "dpi": 96,
      "isPrimary": true,
      "wallpaper": {
        "kind": "Static",              // Static | Video  (MVP scope, PRD §2)
        "sourcePath": "C:\\...\\bg.jpg"
      }
    }
  ],

  "containers": [
    {
      "id": "<guid>",
      "title": "Projects",
      "monitorDeviceInterfacePath": "\\\\?\\DISPLAY#AOP0806#...",
      // Monitor-relative, NOT virtual-desktop-global (decision 2).
      "bounds": { "x": 40, "y": 40, "width": 420, "height": 300 },
      "isCollapsed": false,
      "opacity": 0.85,
      "itemPaths": ["C:\\Users\\...\\project.sln"]
    }
  ],

  "widgets": [
    {
      "id": "<guid>",
      "widgetTypeId": "core.clock",
      "monitorDeviceInterfacePath": "\\\\?\\DISPLAY#AOP0806#...",
      "bounds": { "x": 1500, "y": 40, "width": 300, "height": 140 },
      // Opaque per-widget settings. The runtime does not interpret these;
      // the owning widget does. Keeps widget authoring independent of this schema.
      "settings": { "format": "24h" }
    }
  ]
}
```

## Versioning and migration

- `schemaVersion` is written by the serializer and validated on load.
- **A file with a newer `schemaVersion` than the running build is rejected with a clear, recoverable error** — never partially parsed. Silently ignoring unknown fields from a future version risks destroying data the user's newer build wrote when the older build saves over it.
- A file with an *older* version is upgraded through a migration chain (`WorkspaceMigrations`), one step per version, before being handed to the application.
- Adding a version means: bump `CurrentSchemaVersion`, add a migration step, add a round-trip test for the new version and a migration test from the previous one.

## Not yet specified

Widget manifest/permission schema, wallpaper package format, and automation rule schema are separate Phase 4 deliverables (see `backlog/prototype-backlog.md` and the `widget-builder` / `wallpaper-runtime` / `automation-rule` skills). The `settings` bag above is deliberately opaque so widget authoring can evolve without forcing a workspace schema version bump.
