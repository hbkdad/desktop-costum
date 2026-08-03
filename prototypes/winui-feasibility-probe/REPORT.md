# Probe Report: WinUI 3 / Windows App SDK Feasibility

Format per the `desktop-host-prototype` skill. Tests the UI framework assumption in [ADR-0002](../../docs/architecture/adr/0002-provisional-technology-baseline.md), which was explicitly recorded as provisional and unvalidated.

- **Purpose:** ADR-0002 names WinUI 3 and the Windows App SDK as the UI stack but never verified them. Before building an app shell on that assumption, find out whether the toolchain actually works here.
- **Implementation:** `prototypes/winui-feasibility-probe/` — a genuine unpackaged, self-contained WinUI 3 application (`App.xaml`, `MainWindow.xaml`, `WindowsPackageType=None`), not a stub. A first pass built only a *library* with `UseWinUI=true`; that succeeded but proved little, so the probe was upgraded to compile real XAML.
- **Test method:** `dotnet build` against Windows App SDK **1.6** and, after failure, **1.7**. Also inspected the machine for Visual Studio and for the packaging tooling the build asks for.
- **Measurements / findings:**

  | Step | Result |
  |---|---|
  | Restore `Microsoft.WindowsAppSDK` | **Succeeds** — no workload needed |
  | Build a library with `UseWinUI=true` | **Succeeds** |
  | Build a real WinUI app with XAML (1.6) | **FAILS** |
  | Same, with SDK 1.7 | **FAILS identically** |

  The failure is consistent and specific:

  > `MSB4062: The "Microsoft.Build.Packaging.Pri.Tasks.ExpandPriContent" task could not be loaded from … \Microsoft\VisualStudio\v18.0\AppxPackage\Microsoft.Build.Packaging.Pri.Tasks.dll`

  Confirmed by inspection: **no Visual Studio is installed on this machine**, and that `AppxPackage` directory does not exist under the .NET SDK. `MrtCore.PriGen.targets` ships inside the Windows App SDK package but delegates PRI generation to an assembly that arrives with **Visual Studio's packaging workload**, not with the .NET SDK. Setting `EnableMsixTooling=false` and `WindowsPackageType=None` does not avoid it — PRI generation runs regardless.

  Windows build tested: `10.0.26200.0`; .NET SDK `10.0.204`.

- **Limitations:** Not tested with Visual Studio or VS Build Tools installed, which is the configuration Microsoft documents and where this is expected to work. So this establishes *"the .NET SDK alone is insufficient"*, **not** *"WinUI 3 is unusable."* Nor does it say anything about runtime behaviour — nothing was ever launched.

- **Recommendation:** **Keep WinUI 3, but record the toolchain prerequisite explicitly.** Requiring Visual Studio Build Tools with the Windows App SDK / MSIX packaging component is normal, documented WinUI development practice, and GitHub's `windows-latest` image ships VS Build Tools — so CI is likely unaffected. Reversing a framework decision on this evidence would be an overreaction.

  Two things must follow, though, or this becomes a nasty surprise later:
  1. `backlog/dependency-register.md` records VS Build Tools as a **hard prerequisite** for building the UI layer, not an optional convenience.
  2. CI must be *verified* to build a WinUI project before the app shell lands, rather than assumed to.

  Until that tooling is available on this machine, an interim console shell exercises the full stack end to end and needs none of it.

- **Keep/discard decision:** **Keep the finding, discard the probe project** once ADR-0002 is amended — it has answered its question and would otherwise sit in the tree failing to build.

## The architectural silver lining

Every one of the 271 tests, the whole of `DesktopRuntime.Core`, and the `DesktopRuntime.DesktopHost` adapter build and pass **without any of this tooling**. Keeping the domain layer free of UI dependencies means a UI toolchain problem blocks only the UI — which is exactly what that separation was for, now demonstrated rather than asserted.
