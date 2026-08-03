// Interim shell for the desktop runtime.
//
// The WinUI 3 app shell is blocked on tooling that is not installed here
// (prototypes/winui-feasibility-probe/REPORT.md), so this console front end exists to
// exercise the whole stack end to end in the meantime: WorkspaceStore ->
// WorkspaceResolver -> WallpaperTierResolver -> the real Windows adapter.
//
// It is not throwaway. The master plan calls for diagnostics export and troubleshooting
// tooling, and "show me what the runtime actually sees" is exactly that.

using System.Runtime.Versioning;
using DesktopRuntime.Cli;

[assembly: SupportedOSPlatform("windows")]

return CommandLine.Run(args);
