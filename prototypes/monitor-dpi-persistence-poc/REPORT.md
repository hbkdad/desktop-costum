# Prototype 9 Report: Monitor and DPI Configuration Persistence

Format per the `desktop-host-prototype` skill.

- **Purpose:** A workspace must restore container/widget/wallpaper placement onto *the same physical monitor* after a reconnect, dock/undock, or resolution change — Job #1 in `docs/product/jobs-to-be-done.md`, and the acceptance bar for the Multi-Monitor Power User persona (`docs/product/prd.md` §13.5). That requires a monitor identity key that survives those events. This probe establishes which available identifiers exist and which are structurally safe to persist.
- **Implementation:** `prototypes/monitor-dpi-persistence-poc/Program.cs` — standalone console probe, read-only (changes no display settings). Calls `SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)` *before* querying anything, then `EnumDisplayMonitors` → `GetMonitorInfoW` (bounds, work area, primary flag, GDI device name) → `GetDpiForMonitor` (effective DPI/scale) → `EnumDisplayDevicesW` with `EDD_GET_DEVICE_INTERFACE_NAME` (device interface path + friendly name).
- **Test method:** Built and run against the live desktop session.
- **Measurements / findings:**
  - `SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)` returned `True`. **This must happen before any geometry query** — without it Windows reports virtualized (scaled) coordinates, which would silently persist wrong values. Worth treating as a startup invariant of the desktop host, not a per-call concern.
  - One monitor detected: 1920x1080 at (0,0), primary, effective DPI 96x96 (100% scale).
  - Identifier stability assessment (the actual point of this prototype):
    - `HMONITOR` handle (`0x10001`) — runtime handle only, **never persist**.
    - GDI device name (`\\.\DISPLAY1`) — positional/ordinal, reassigned as monitors come and go, **not safe to persist**.
    - Friendly name (`Generic PnP Monitor`) — not unique; identical across same-model monitors, **not sufficient alone**.
    - Device interface path (`\\?\DISPLAY#AOP0806#4&1427843b&0&UID198147#{e6f07b5f-...}`) — embeds hardware/EDID identity (`AOP0806`) plus a UID, **the viable persistence key**.
  - **Incidental finding:** the work area equalled the full monitor bounds (1920x1080 for both), i.e. no taskbar reservation on this machine/configuration. Container-layout logic must therefore **not assume the work area is smaller than the monitor bounds** — a layout algorithm that "leaves room for the taskbar" by assuming a difference would misplace content here.
- **Limitations:** **This machine has only one monitor at 100% scale**, so the most important cases for the target persona — multi-monitor, mixed-DPI, and actual disconnect/reconnect stability — **could not be validated**. This probe establishes which key is *structurally* the right candidate (the only one carrying hardware identity rather than position); it does **not** empirically prove that key survives a reconnect. Also did not evaluate the `QueryDisplayConfig` / `DisplayConfigGetDeviceInfo` path, which yields the same class of stable target identity plus richer topology data and may be the better production API.
- **Recommendation:** Persist monitor identity on the device interface path, never on device name or handle. Treat resolution/position/DPI as *attributes* of an identified monitor rather than as part of its identity, so a resolution change updates a monitor's record instead of creating a new one. Before Phase 4 architecture lock, this needs a real multi-monitor, mixed-DPI test on hardware that has it — that gap is now tracked in `backlog/prototype-backlog.md` and `backlog/risk-register.md` rather than being assumed resolved.
- **Keep/discard decision:** **Keep, with an explicit follow-up.** The identifier analysis and the DPI-awareness-before-query invariant are directly reusable in the multi-monitor manager module. The reconnect-stability claim itself remains **unverified** on this hardware and must not be treated as proven.

## Impact on other project artifacts

- `docs/product/prd.md` §13.5 requires MVP acceptance testing on a 2-monitor mixed-DPI configuration. This prototype cannot satisfy that bar on this machine — it's a hardware limitation, not a code gap, and is now an explicit open dependency.
- Adds a concrete engineering invariant worth carrying into the `winui-component` and multi-monitor work: set per-monitor DPI awareness at process start, before any geometry is read or persisted.
