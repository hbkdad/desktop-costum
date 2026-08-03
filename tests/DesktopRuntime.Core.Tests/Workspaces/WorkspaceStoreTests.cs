using DesktopRuntime.Core.Workspaces;

namespace DesktopRuntime.Core.Tests.Workspaces;

public sealed class WorkspaceStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly WorkspaceStore _store;

    public WorkspaceStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "desktop-runtime-tests", Guid.NewGuid().ToString("N"));
        _store = new WorkspaceStore(_directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static Workspace CreateWorkspace(string name = "Focus") => new()
    {
        Name = name,
        Monitors =
        [
            new MonitorLayout
            {
                DeviceInterfacePath = @"\\?\DISPLAY#AOP0806#4&1427843b&0&UID198147#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}",
                Bounds = new Rect(0, 0, 1920, 1080),
                IsPrimary = true
            }
        ],
        Containers = [new DesktopContainer { Title = "Projects", Bounds = new Rect(10, 10, 300, 200) }]
    };

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var workspace = CreateWorkspace();

        _store.Save(workspace);
        var loaded = _store.Load(workspace.Id);

        Assert.Equal(workspace.Id, loaded.Id);
        Assert.Equal("Focus", loaded.Name);
        Assert.Single(loaded.Monitors);
        Assert.Equal("Projects", Assert.Single(loaded.Containers).Title);
    }

    [Fact]
    public void Save_StampsModifiedTime()
    {
        var workspace = CreateWorkspace();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        _store.Save(workspace);

        Assert.True(_store.Load(workspace.Id).ModifiedUtc > before);
    }

    [Fact]
    public void Save_LeavesNoTemporaryFilesBehind()
    {
        // A stray .tmp would be picked up by nothing and silently consume space, and
        // suggests the atomic move did not complete.
        _store.Save(CreateWorkspace());

        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void FileNameComesFromTheId_NotTheName()
    {
        // A workspace name is user- or import-supplied text. If it reached the filesystem
        // this would escape the store directory.
        var workspace = CreateWorkspace(name: @"../../../evil");

        _store.Save(workspace);

        string[] files = Directory.GetFiles(_directory);
        Assert.Single(files);
        Assert.Contains(workspace.Id.ToString("D"), Path.GetFileName(files[0]));
        Assert.Equal(@"../../../evil", _store.Load(workspace.Id).Name);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("name/with/slashes")]
    [InlineData(@"name\with\backslashes")]
    [InlineData("name:with:colons")]
    public void HostileWorkspaceNames_AreStoredSafely(string name)
    {
        var workspace = CreateWorkspace(name);

        _store.Save(workspace);

        Assert.Single(Directory.GetFiles(_directory));
        Assert.Equal(name, _store.Load(workspace.Id).Name);
    }

    [Fact]
    public void Save_OverwritesThePreviousVersionInPlace()
    {
        var workspace = CreateWorkspace();
        _store.Save(workspace);

        workspace.Name = "Renamed";
        _store.Save(workspace);

        Assert.Single(Directory.GetFiles(_directory));
        Assert.Equal("Renamed", _store.Load(workspace.Id).Name);
    }

    [Fact]
    public void Load_OfAnUnknownId_Throws()
    {
        var ex = Assert.Throws<WorkspaceNotFoundException>(() => _store.Load(Guid.NewGuid()));

        Assert.NotEqual(Guid.Empty, ex.Id);
    }

    [Fact]
    public void Save_RejectsAnEmptyId()
    {
        // Every workspace with an empty id would resolve to the same file.
        var workspace = CreateWorkspace();
        workspace.Id = Guid.Empty;

        Assert.Throws<ArgumentException>(() => _store.Save(workspace));
    }

    [Fact]
    public void Load_OfACorruptedFile_ThrowsALoadException_NotSomethingUnexpected()
    {
        var workspace = CreateWorkspace();
        _store.Save(workspace);
        File.WriteAllText(Directory.GetFiles(_directory)[0], "{ this is not json");

        Assert.Throws<WorkspaceLoadException>(() => _store.Load(workspace.Id));
    }

    [Fact]
    public void OneCorruptedFile_DoesNotMakeOtherWorkspacesUnreachable()
    {
        var good = CreateWorkspace("Good");
        var doomed = CreateWorkspace("Doomed");
        _store.Save(good);
        _store.Save(doomed);

        File.WriteAllText(
            Path.Combine(_directory, doomed.Id.ToString("D") + ".workspace.json"),
            "{ corrupted");

        var listed = _store.List();

        Assert.Equal("Good", Assert.Single(listed).Name);

        var unreadable = Assert.Single(_store.ListUnreadable());
        Assert.Contains(doomed.Id.ToString("D"), unreadable.Path);
    }

    [Fact]
    public void List_ReturnsEveryStoredWorkspace()
    {
        var a = CreateWorkspace("A");
        var b = CreateWorkspace("B");
        _store.Save(a);
        _store.Save(b);

        var listed = _store.List();

        Assert.Equal(2, listed.Count);
        Assert.Contains(listed, s => s.Id == a.Id && s.Name == "A");
        Assert.Contains(listed, s => s.Id == b.Id && s.Name == "B");
    }

    [Fact]
    public void List_OnAnEmptyStore_IsEmpty()
    {
        Assert.Empty(_store.List());
        Assert.Empty(_store.ListUnreadable());
    }

    [Fact]
    public void Delete_RemovesTheWorkspace_AndReportsWhetherItExisted()
    {
        var workspace = CreateWorkspace();
        _store.Save(workspace);

        Assert.True(_store.Delete(workspace.Id));
        Assert.False(_store.Exists(workspace.Id));
        Assert.False(_store.Delete(workspace.Id));
    }

    [Fact]
    public void ExportThenImport_ProducesAnIndependentCopy()
    {
        var original = CreateWorkspace("Shared");
        _store.Save(original);
        string exportPath = Path.Combine(_directory, "exported.json");

        _store.Export(original.Id, exportPath);
        Guid importedId = _store.Import(exportPath);

        // A new id, so importing can never silently overwrite the original.
        Assert.NotEqual(original.Id, importedId);
        Assert.Equal("Shared", _store.Load(importedId).Name);
        Assert.True(_store.Exists(original.Id));
        Assert.Equal(2, _store.List().Count);
    }

    [Fact]
    public void Import_OfAMalformedFile_IsRejectedAndStoresNothing()
    {
        string path = Path.Combine(_directory, "bad.json");
        File.WriteAllText(path, "{ not a workspace");

        Assert.Throws<WorkspaceLoadException>(() => _store.Import(path));
        Assert.Empty(_store.List());
    }

    [Fact]
    public void Import_OfANewerSchemaVersion_IsRejected()
    {
        // The same versioning rule as everywhere else: never partially interpret a
        // document written by a newer build.
        var workspace = CreateWorkspace();
        string json = WorkspaceSerializer.Serialize(workspace)
            .Replace($"\"schemaVersion\": {WorkspaceSchema.CurrentVersion}",
                     $"\"schemaVersion\": {WorkspaceSchema.CurrentVersion + 1}");

        string path = Path.Combine(_directory, "future.json");
        File.WriteAllText(path, json);

        Assert.Throws<WorkspaceLoadException>(() => _store.Import(path));
        Assert.Empty(_store.List());
    }

    [Fact]
    public void Import_OfAMissingFile_ThrowsALoadException()
    {
        string missing = Path.Combine(_directory, "does-not-exist.json");

        Assert.Throws<WorkspaceLoadException>(() => _store.Import(missing));
    }

    [Fact]
    public void Constructor_CreatesTheDirectoryIfItIsAbsent()
    {
        string fresh = Path.Combine(_directory, "nested", "deeper");

        _ = new WorkspaceStore(fresh);

        Assert.True(Directory.Exists(fresh));
    }
}
