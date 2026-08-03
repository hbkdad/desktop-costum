namespace DesktopRuntime.Core.Workspaces;

/// <summary>Identifying information about a stored workspace, without loading all of it.</summary>
public readonly record struct WorkspaceSummary(Guid Id, string Name, DateTimeOffset ModifiedUtc);

/// <summary>
/// Persists workspaces to a directory, one file each.
/// <para>
/// Two properties matter more than anything else here, because a workspace is the
/// user's accumulated arrangement and losing one is the worst thing this component
/// can do:
/// </para>
/// <list type="number">
/// <item>
/// <b>Saves are atomic.</b> Content is written to a temporary file, flushed to disk,
/// then moved into place. A crash or power loss mid-save leaves the previous version
/// intact rather than a half-written file.
/// </item>
/// <item>
/// <b>Filenames come from the workspace id, never its name.</b> A name is user- or
/// import-supplied text that may contain path separators, traversal sequences, or
/// reserved device names — the same class of input <see cref="Packaging.PackagePath"/>
/// exists to defend against.
/// </item>
/// </list>
/// </summary>
public sealed class WorkspaceStore
{
    private const string FileExtension = ".workspace.json";
    private const string TempExtension = ".tmp";

    private readonly string _directory;

    public WorkspaceStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    /// <summary>
    /// Writes a workspace, replacing any previous version atomically.
    /// </summary>
    public void Save(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        if (workspace.Id == Guid.Empty)
        {
            // An empty id would collide with every other workspace that has one.
            throw new ArgumentException("A workspace must have a non-empty id.", nameof(workspace));
        }

        workspace.ModifiedUtc = DateTimeOffset.UtcNow;
        string json = WorkspaceSerializer.Serialize(workspace);

        string destination = PathFor(workspace.Id);
        string temporary = destination + TempExtension;

        // Flush to disk before the move: without it, the move can complete while the
        // content is still only in the OS cache, and a power loss then leaves an empty
        // file where a valid workspace used to be.
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(json);
            writer.Flush();
            stream.Flush(flushToDisk: true);
        }

        File.Move(temporary, destination, overwrite: true);
    }

    /// <summary>
    /// Loads a workspace by id.
    /// </summary>
    /// <exception cref="WorkspaceNotFoundException">No workspace with that id is stored.</exception>
    /// <exception cref="WorkspaceLoadException">The stored file is unreadable or malformed.</exception>
    public Workspace Load(Guid id)
    {
        string path = PathFor(id);

        if (!File.Exists(path))
        {
            throw new WorkspaceNotFoundException(id);
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new WorkspaceLoadException($"The workspace file for {id} could not be read.", ex);
        }

        return WorkspaceSerializer.Deserialize(json);
    }

    public bool Exists(Guid id) => File.Exists(PathFor(id));

    public bool Delete(Guid id)
    {
        string path = PathFor(id);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    /// <summary>
    /// Lists stored workspaces.
    /// <para>
    /// A workspace that fails to load is <b>skipped rather than thrown over</b>: one
    /// corrupted file must not make every other workspace unreachable. Use
    /// <see cref="ListUnreadable"/> to surface those separately.
    /// </para>
    /// </summary>
    public IReadOnlyList<WorkspaceSummary> List()
    {
        var summaries = new List<WorkspaceSummary>();

        foreach (string path in EnumerateWorkspaceFiles())
        {
            try
            {
                var workspace = WorkspaceSerializer.Deserialize(File.ReadAllText(path));
                summaries.Add(new WorkspaceSummary(workspace.Id, workspace.Name, workspace.ModifiedUtc));
            }
            catch (Exception ex) when (ex is WorkspaceLoadException or IOException)
            {
                // Deliberately swallowed here; reported by ListUnreadable.
            }
        }

        return summaries;
    }

    /// <summary>Files present in the store that could not be loaded, with the reason.</summary>
    public IReadOnlyList<(string Path, string Reason)> ListUnreadable()
    {
        var unreadable = new List<(string, string)>();

        foreach (string path in EnumerateWorkspaceFiles())
        {
            try
            {
                WorkspaceSerializer.Deserialize(File.ReadAllText(path));
            }
            catch (Exception ex) when (ex is WorkspaceLoadException or IOException)
            {
                unreadable.Add((path, ex.Message));
            }
        }

        return unreadable;
    }

    /// <summary>Writes a workspace to an arbitrary path for sharing.</summary>
    public void Export(Guid id, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var workspace = Load(id);
        File.WriteAllText(destinationPath, WorkspaceSerializer.Serialize(workspace));
    }

    /// <summary>
    /// Reads a workspace from an arbitrary path and stores it.
    /// <para>
    /// An imported file is untrusted — it came from somewhere else — so it is fully
    /// parsed and validated before anything is written. It is also given a <b>new id</b>
    /// so importing can never silently overwrite an existing workspace that happens to
    /// share an id.
    /// </para>
    /// </summary>
    /// <returns>The id assigned to the imported workspace.</returns>
    public Guid Import(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        string json;
        try
        {
            json = File.ReadAllText(sourcePath);
        }
        catch (IOException ex)
        {
            throw new WorkspaceLoadException($"The file '{sourcePath}' could not be read.", ex);
        }

        var workspace = WorkspaceSerializer.Deserialize(json);

        workspace.Id = Guid.NewGuid();
        workspace.CreatedUtc = DateTimeOffset.UtcNow;
        Save(workspace);

        return workspace.Id;
    }

    private IEnumerable<string> EnumerateWorkspaceFiles() =>
        Directory.EnumerateFiles(_directory, "*" + FileExtension);

    /// <summary>
    /// The id is a GUID, whose string form contains only hex digits and hyphens, so the
    /// resulting filename cannot escape the store directory regardless of what the
    /// workspace is called.
    /// </summary>
    private string PathFor(Guid id) => Path.Combine(_directory, id.ToString("D") + FileExtension);
}

public sealed class WorkspaceNotFoundException(Guid id)
    : Exception($"No workspace with id {id} is stored.")
{
    public Guid Id { get; } = id;
}
