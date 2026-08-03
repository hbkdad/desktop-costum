using System.Runtime.Versioning;
using DesktopRuntime.Core.Hosting;
using DesktopRuntime.Core.Wallpapers;
using DesktopRuntime.Core.Workspaces;
using DesktopRuntime.DesktopHost;

namespace DesktopRuntime.Cli;

[SupportedOSPlatform("windows")]
internal static class CommandLine
{
    private const int Ok = 0;
    private const int UsageError = 2;
    private const int Failed = 1;

    internal static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return args.Length == 0 ? UsageError : Ok;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "monitors" => ShowMonitors(),
                "list" => ListWorkspaces(),
                "new" => CreateWorkspace(args),
                "set-wallpaper" => SetWallpaper(args),
                "activate" => ActivateWorkspace(args),
                "delete" => DeleteWorkspace(args),
                "where" => ShowStoreLocation(),
                _ => UnknownCommand(args[0])
            };
        }
        catch (WorkspaceNotFoundException ex)
        {
            Error(ex.Message);
            return Failed;
        }
        catch (WorkspaceLoadException ex)
        {
            Error(ex.Message);
            return Failed;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            desktopruntime — interim shell for the Windows 11 desktop runtime

            USAGE
              desktopruntime <command> [arguments]

            COMMANDS
              monitors            Show the monitors the runtime can see, and whether the
                                  animated-wallpaper surface is available on this system
              list                List stored workspaces
              new <name>          Create a workspace capturing the current monitor setup
              set-wallpaper <id> <image> [--video]
                                  Assign a wallpaper to every monitor in a workspace
              activate <id>       Apply a workspace to the desktop
              delete <id>         Delete a workspace
              where               Show where workspaces are stored

            Ids may be abbreviated to any unambiguous prefix.
            """);
    }

    private static int UnknownCommand(string command)
    {
        Error($"Unknown command '{command}'. Run 'desktopruntime help' for usage.");
        return UsageError;
    }

    // --- Commands ---

    private static int ShowMonitors()
    {
        var monitors = new WindowsMonitorProvider().GetMonitors();
        bool attachment = new WindowsAttachmentProbe().IsAttachmentSurfaceAvailable();

        if (monitors.Count == 0)
        {
            Console.WriteLine("No monitors detected.");
        }

        foreach (var monitor in monitors)
        {
            Console.WriteLine($"{(monitor.IsPrimary ? "*" : " ")} {monitor.FriendlyName ?? "(unnamed)"}");
            Console.WriteLine($"    {monitor.Bounds.Width}x{monitor.Bounds.Height} at " +
                              $"({monitor.Bounds.X},{monitor.Bounds.Y})   {monitor.Dpi} DPI " +
                              $"({monitor.Dpi / 96.0 * 100:0}% scale)");
            Console.WriteLine($"    id: {monitor.DeviceInterfacePath}");
            Console.WriteLine();
        }

        Console.WriteLine($"Animated wallpaper surface: {(attachment ? "available" : "NOT available")}");
        if (!attachment)
        {
            // Stated plainly rather than left as a silent capability gap — it is the
            // expected outcome on current Windows 11 builds, not a fault.
            Console.WriteLine("  Video wallpapers will fall back to a still image on this system.");
        }

        var surface = new WindowsWallpaperSurface();
        Console.WriteLine($"Per-monitor wallpaper:      {(surface.SupportsPerMonitor ? "supported" : "NOT supported")}");
        Console.WriteLine($"Current wallpaper:          {surface.GetCurrentWallpaper() ?? "(none)"}");

        return Ok;
    }

    private static int ListWorkspaces()
    {
        var store = OpenStore();
        var workspaces = store.List();

        if (workspaces.Count == 0)
        {
            Console.WriteLine("No workspaces yet. Create one with: desktopruntime new \"My workspace\"");
        }

        foreach (var workspace in workspaces.OrderByDescending(w => w.ModifiedUtc))
        {
            Console.WriteLine($"{Short(workspace.Id)}  {workspace.Name,-30}  modified {workspace.ModifiedUtc.LocalDateTime:g}");
        }

        // Surfaced rather than hidden: a corrupted file is skipped by List() by design,
        // and the user should still be told it exists.
        var unreadable = store.ListUnreadable();
        if (unreadable.Count > 0)
        {
            Console.WriteLine();
            Warn($"{unreadable.Count} workspace file(s) could not be read:");
            foreach (var (path, reason) in unreadable)
            {
                Console.WriteLine($"  {Path.GetFileName(path)}: {reason}");
            }
        }

        return Ok;
    }

    private static int CreateWorkspace(string[] args)
    {
        if (args.Length < 2)
        {
            Error("Usage: desktopruntime new <name>");
            return UsageError;
        }

        string name = string.Join(' ', args[1..]);
        var monitors = new WindowsMonitorProvider().GetMonitors();

        var workspace = new Workspace
        {
            Name = name,
            CreatedUtc = DateTimeOffset.UtcNow,
            Monitors = [.. monitors.Select(m => new MonitorLayout
            {
                DeviceInterfacePath = m.DeviceInterfacePath,
                FriendlyName = m.FriendlyName,
                Bounds = m.Bounds,
                Dpi = m.Dpi,
                IsPrimary = m.IsPrimary
            })]
        };

        OpenStore().Save(workspace);

        Console.WriteLine($"Created '{name}' ({Short(workspace.Id)}) capturing {monitors.Count} monitor(s).");
        return Ok;
    }

    private static int SetWallpaper(string[] args)
    {
        if (args.Length < 3)
        {
            Error("Usage: desktopruntime set-wallpaper <id> <image> [--video]");
            return UsageError;
        }

        var store = OpenStore();
        if (!TryResolveId(store, args[1], out Guid id))
        {
            return Failed;
        }

        string imagePath = Path.GetFullPath(args[2]);
        if (!File.Exists(imagePath))
        {
            // Refused at configuration time rather than at activation, so the problem
            // surfaces where the user can still fix it easily.
            Error($"'{imagePath}' does not exist.");
            return Failed;
        }

        var kind = args.Contains("--video", StringComparer.OrdinalIgnoreCase)
            ? WallpaperKind.Video
            : WallpaperKind.Static;

        var workspace = store.Load(id);
        foreach (var monitor in workspace.Monitors)
        {
            monitor.Wallpaper = new WallpaperAssignment { Kind = kind, SourcePath = imagePath };
        }

        store.Save(workspace);

        Console.WriteLine($"Set {kind} wallpaper on {workspace.Monitors.Count} monitor(s) of '{workspace.Name}'.");
        if (kind == WallpaperKind.Video && !new WindowsAttachmentProbe().IsAttachmentSurfaceAvailable())
        {
            Warn("This system cannot show animated wallpaper; it will fall back to a still image.");
        }

        return Ok;
    }

    private static int ActivateWorkspace(string[] args)
    {
        if (args.Length < 2)
        {
            Error("Usage: desktopruntime activate <id>");
            return UsageError;
        }

        var store = OpenStore();
        if (!TryResolveId(store, args[1], out Guid id))
        {
            return Failed;
        }

        var activator = new WorkspaceActivator(
            new WindowsMonitorProvider(),
            new WindowsAttachmentProbe(),
            new WindowsWallpaperSurface());

        var workspace = store.Load(id);
        var result = activator.Activate(workspace);

        Console.WriteLine($"Activated '{workspace.Name}'.");
        Console.WriteLine();

        foreach (var monitor in result.Monitors)
        {
            string tier = monitor.TierDecision is null ? "-" : monitor.TierDecision.SelectedTier.ToString();
            Console.WriteLine($"  {monitor.Outcome,-20} tier={tier,-16} {Truncate(monitor.MonitorDeviceInterfacePath, 48)}");
            if (monitor.Detail is not null)
            {
                Console.WriteLine($"      {monitor.Detail}");
            }
        }

        if (result.AppliedExactlyAsConfigured)
        {
            Console.WriteLine();
            Console.WriteLine("Applied exactly as configured.");
            return Ok;
        }

        Console.WriteLine();
        foreach (string warning in result.Warnings)
        {
            Warn(warning);
        }

        // Warnings are not failure: activation is best-effort by design, and the caller
        // has been told exactly what differs from the saved configuration.
        return Ok;
    }

    private static int DeleteWorkspace(string[] args)
    {
        if (args.Length < 2)
        {
            Error("Usage: desktopruntime delete <id>");
            return UsageError;
        }

        var store = OpenStore();
        if (!TryResolveId(store, args[1], out Guid id))
        {
            return Failed;
        }

        string name = store.Load(id).Name;
        return store.Delete(id)
            ? Report($"Deleted '{name}'.", Ok)
            : Report($"'{name}' could not be deleted.", Failed, isError: true);
    }

    private static int ShowStoreLocation()
    {
        Console.WriteLine(StoreDirectory);
        return Ok;
    }

    // --- Helpers ---

    private static string StoreDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopRuntime", "workspaces");

    private static WorkspaceStore OpenStore() => new(StoreDirectory);

    /// <summary>
    /// Accepts a full id or any unambiguous prefix. An ambiguous prefix is an error rather
    /// than a guess — picking one arbitrarily could activate or delete the wrong workspace.
    /// </summary>
    private static bool TryResolveId(WorkspaceStore store, string input, out Guid id)
    {
        if (Guid.TryParse(input, out id))
        {
            return true;
        }

        var matches = store.List()
            .Where(w => w.Id.ToString("D").StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .ToList();

        switch (matches.Count)
        {
            case 1:
                id = matches[0].Id;
                return true;

            case 0:
                Error($"No workspace matches '{input}'.");
                return false;

            default:
                Error($"'{input}' matches {matches.Count} workspaces:");
                foreach (var match in matches)
                {
                    Console.Error.WriteLine($"  {Short(match.Id)}  {match.Name}");
                }
                return false;
        }
    }

    private static string Short(Guid id) => id.ToString("D")[..8];

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : "…" + value[^(max - 1)..];

    private static int Report(string message, int code, bool isError = false)
    {
        if (isError) Error(message); else Console.WriteLine(message);
        return code;
    }

    private static void Warn(string message) => Console.WriteLine($"warning: {message}");

    private static void Error(string message) => Console.Error.WriteLine($"error: {message}");
}
