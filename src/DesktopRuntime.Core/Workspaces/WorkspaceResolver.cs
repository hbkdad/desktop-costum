namespace DesktopRuntime.Core.Workspaces;

/// <summary>
/// Matches a saved workspace against the monitors currently connected.
/// <para>
/// Critically, this never mutates or discards the workspace: content belonging to a
/// monitor that is not currently connected is reported as <em>deferred</em>, not
/// dropped, so undocking and redocking is lossless (workspace-schema.md decision 5).
/// </para>
/// </summary>
public static class WorkspaceResolver
{
    /// <param name="workspace">The saved workspace. Not modified.</param>
    /// <param name="connectedMonitorPaths">
    /// Device interface paths of the physically connected monitors, as reported by the
    /// multi-monitor manager. Matching is case-insensitive: Windows device paths are not
    /// case-sensitive, and treating them as such would spuriously orphan a layout.
    /// </param>
    public static WorkspaceResolution Resolve(Workspace workspace, IEnumerable<string> connectedMonitorPaths)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(connectedMonitorPaths);

        var connected = new HashSet<string>(connectedMonitorPaths, StringComparer.OrdinalIgnoreCase);

        var present = new List<MonitorLayout>();
        var absent = new List<MonitorLayout>();
        foreach (var monitor in workspace.Monitors)
        {
            if (connected.Contains(monitor.DeviceInterfacePath))
            {
                present.Add(monitor);
            }
            else
            {
                absent.Add(monitor);
            }
        }

        var placeableContainers = new List<DesktopContainer>();
        var deferredContainers = new List<DesktopContainer>();
        foreach (var container in workspace.Containers)
        {
            if (connected.Contains(container.MonitorDeviceInterfacePath))
            {
                placeableContainers.Add(container);
            }
            else
            {
                deferredContainers.Add(container);
            }
        }

        var placeableWidgets = new List<WidgetPlacement>();
        var deferredWidgets = new List<WidgetPlacement>();
        foreach (var widget in workspace.Widgets)
        {
            if (connected.Contains(widget.MonitorDeviceInterfacePath))
            {
                placeableWidgets.Add(widget);
            }
            else
            {
                deferredWidgets.Add(widget);
            }
        }

        return new WorkspaceResolution(
            present, absent,
            placeableContainers, deferredContainers,
            placeableWidgets, deferredWidgets);
    }
}

/// <summary>
/// The result of matching a workspace against currently connected monitors.
/// Deferred content is retained so it can be restored when its monitor returns.
/// </summary>
public sealed class WorkspaceResolution(
    IReadOnlyList<MonitorLayout> presentMonitors,
    IReadOnlyList<MonitorLayout> absentMonitors,
    IReadOnlyList<DesktopContainer> placeableContainers,
    IReadOnlyList<DesktopContainer> deferredContainers,
    IReadOnlyList<WidgetPlacement> placeableWidgets,
    IReadOnlyList<WidgetPlacement> deferredWidgets)
{
    public IReadOnlyList<MonitorLayout> PresentMonitors { get; } = presentMonitors;

    /// <summary>Saved monitors that are not currently connected. Their layout is preserved.</summary>
    public IReadOnlyList<MonitorLayout> AbsentMonitors { get; } = absentMonitors;

    public IReadOnlyList<DesktopContainer> PlaceableContainers { get; } = placeableContainers;

    /// <summary>Containers awaiting the return of their monitor. Never discarded.</summary>
    public IReadOnlyList<DesktopContainer> DeferredContainers { get; } = deferredContainers;

    public IReadOnlyList<WidgetPlacement> PlaceableWidgets { get; } = placeableWidgets;

    /// <summary>Widgets awaiting the return of their monitor. Never discarded.</summary>
    public IReadOnlyList<WidgetPlacement> DeferredWidgets { get; } = deferredWidgets;

    /// <summary>True when some saved content cannot be shown right now. Surface this to the user.</summary>
    public bool HasDeferredContent =>
        DeferredContainers.Count > 0 || DeferredWidgets.Count > 0 || AbsentMonitors.Count > 0;
}
