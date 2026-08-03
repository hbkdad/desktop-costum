namespace DesktopRuntime.Core.Workspaces;

/// <summary>
/// Schema version constants and validation. See docs/architecture/workspace-schema.md.
/// </summary>
public static class WorkspaceSchema
{
    /// <summary>The schema version this build writes and can read up to.</summary>
    public const int CurrentVersion = 1;

    /// <summary>The oldest schema version this build can migrate forward from.</summary>
    public const int MinimumSupportedVersion = 1;
}

/// <summary>
/// Raised when a workspace file cannot be loaded. Carries enough detail for the
/// application to show a recoverable error rather than failing silently or, worse,
/// partially applying a layout.
/// </summary>
public sealed class WorkspaceLoadException : Exception
{
    public WorkspaceLoadException(string message) : base(message) { }

    public WorkspaceLoadException(string message, Exception inner) : base(message, inner) { }
}
