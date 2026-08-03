# System Context

Orientation document: what the pieces are, how they relate, and which of them exist yet. Detail lives in the linked documents.

## Context

```mermaid
flowchart TB
    user([User])
    creator([Package creator])

    subgraph product["Desktop Runtime"]
        shell["App shell<br/>(WinUI 3)"]
        service["Core service<br/>state · automation · permissions"]
        host["Desktop host<br/>shell integration"]
        wall["Wallpaper renderer"]
        widget["Widget host<br/>(sandboxed, per package)"]
    end

    windows[["Windows shell<br/>explorer.exe"]]
    store[("Marketplace<br/>(future)")]

    user --> shell
    creator -.publishes package.-> store
    store -.signed package.-> service
    shell <--> service
    service --> host
    service --> wall
    service <-->|"IPC — permission<br/>checked here"| widget
    host <-->|"attach / fallback"| windows
```

## Trust boundaries

Everything inside the product is our code **except the widget host**, which runs package-authored code and is sandboxed accordingly. The critical rule from [ADR-0004](adr/0004-process-and-isolation-model.md): permission checks happen in the core service, never in the sandboxed process. A compromised widget host may issue any request and still gain nothing it was not granted.

## Wallpaper rendering path

```mermaid
flowchart LR
    req["Requested<br/>wallpaper"] --> kind{Kind?}
    kind -->|Static| tierS["Static tier<br/>(OS wallpaper API)"]
    kind -->|Video| avail{"Attachment<br/>available?"}
    avail -->|Yes| tierA["Attached surface<br/>(animated)"]
    avail -->|No| degraded["Static tier<br/>+ visible degradation notice"]
```

Per [ADR-0003](adr/0003-desktop-hosting-strategy.md), animated wallpaper is **opportunistic, not guaranteed** — Phase 3 found behind-icon rendering is not reliably achievable on current Windows 11 builds. Static content uses the static tier even when attachment is available, since a still image should not consume a scarce, fragile surface.

## Shell recovery

```mermaid
stateDiagram-v2
    [*] --> Healthy
    Healthy --> Waiting: shell lost
    Waiting --> Healthy: OS recovered itself
    Waiting --> Relaunching: grace period elapsed
    Relaunching --> Waiting: backoff
    Relaunching --> SafeMode: attempts exhausted
    Waiting --> SafeMode: attempts exhausted
    SafeMode --> Healthy: sustained health
```

The transition out of `SafeMode` requires *sustained* health, not mere reappearance — see [recovery-model.md](recovery-model.md) for why that distinction is the whole design.

## What exists

| Area | Status |
|---|---|
| Workspace schema, permission model, widget manifest, automation schema, wallpaper tiers, recovery policy, resource accounting, package format | **Implemented and tested** (`src/DesktopRuntime.Core/`) |
| Process/isolation model | Designed ([ADR-0004](adr/0004-process-and-isolation-model.md)) and **validated by prototype** — caps enforced, processes launch into AppContainers, default-deny denies. Not yet **built** into the product. |
| Desktop host | **Partly built** — `src/DesktopRuntime.DesktopHost` provides monitor enumeration, the attachment probe and the static wallpaper surface, covered by integration tests against the real APIs. No rendering surface yet. |
| App shell, core service, wallpaper renderer, widget host | **Not built** — no UI or running processes yet |
| Marketplace | Not started (Phase 8) |

`DesktopRuntime.Core` remains pure policy — no I/O, no clock reads, no OS calls — which is why it is thoroughly testable. All OS access is confined to `DesktopRuntime.DesktopHost` behind the three interfaces in `Core/Hosting/`, satisfying the "isolate Explorer-specific and undocumented behaviour" rule. Undocumented behaviour is confined further still: to a single class, `WindowsAttachmentProbe`, which only ever *asks a question* and never throws.

## Reading order

1. [`../product/prd.md`](../product/prd.md) — what is being built and for whom
2. [`adr/0003-desktop-hosting-strategy.md`](adr/0003-desktop-hosting-strategy.md) — the finding that shaped everything else
3. [`adr/0004-process-and-isolation-model.md`](adr/0004-process-and-isolation-model.md) — how isolation is meant to work
4. [`permission-model.md`](permission-model.md) → [`widget-manifest.md`](widget-manifest.md) → [`package-format.md`](package-format.md) — the trust chain, outermost last
5. [`workspace-schema.md`](workspace-schema.md), [`automation-schema.md`](automation-schema.md), [`recovery-model.md`](recovery-model.md), [`resource-accounting.md`](resource-accounting.md)
