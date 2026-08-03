using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopRuntime.Core.Workspaces;

/// <summary>
/// Reads and writes workspace files. Enforces the versioning contract in
/// docs/architecture/workspace-schema.md: newer-than-supported files are rejected
/// outright (never partially parsed), older files are migrated forward.
/// </summary>
public static class WorkspaceSerializer
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        // Enums are written as names, not numbers, so reordering an enum can never
        // silently reinterpret existing files (e.g. Static becoming Video).
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        workspace.SchemaVersion = WorkspaceSchema.CurrentVersion;
        return JsonSerializer.Serialize(workspace, Options);
    }

    /// <summary>
    /// Parses a workspace document, validating and migrating its schema version first.
    /// </summary>
    /// <exception cref="WorkspaceLoadException">
    /// The document is malformed, is missing a schema version, or was written by a
    /// newer build than this one.
    /// </exception>
    public static Workspace Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        int version = ReadSchemaVersion(json);

        if (version > WorkspaceSchema.CurrentVersion)
        {
            throw new WorkspaceLoadException(
                $"This workspace was created by a newer version of the application " +
                $"(schema version {version}; this build supports up to {WorkspaceSchema.CurrentVersion}). " +
                $"Update the application to open it.");
        }

        if (version < WorkspaceSchema.MinimumSupportedVersion)
        {
            throw new WorkspaceLoadException(
                $"This workspace uses schema version {version}, which is no longer supported " +
                $"(minimum supported version is {WorkspaceSchema.MinimumSupportedVersion}).");
        }

        Workspace? workspace;
        try
        {
            workspace = JsonSerializer.Deserialize<Workspace>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new WorkspaceLoadException("The workspace file is not valid JSON.", ex);
        }

        if (workspace is null)
        {
            throw new WorkspaceLoadException("The workspace file was empty.");
        }

        return WorkspaceMigrations.MigrateToCurrent(workspace, version);
    }

    private static int ReadSchemaVersion(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new WorkspaceLoadException("The workspace file must contain a JSON object.");
            }

            if (!document.RootElement.TryGetProperty("schemaVersion", out var versionElement)
                || !versionElement.TryGetInt32(out int version))
            {
                throw new WorkspaceLoadException(
                    "The workspace file is missing a valid 'schemaVersion' and cannot be safely loaded.");
            }

            return version;
        }
        catch (JsonException ex)
        {
            throw new WorkspaceLoadException("The workspace file is not valid JSON.", ex);
        }
    }
}

/// <summary>
/// Forward-migration chain. Each supported version has exactly one step that moves a
/// document to the next version, so adding schema version N+1 means adding one step
/// here plus a migration test — never rewriting older steps.
/// </summary>
internal static class WorkspaceMigrations
{
    public static Workspace MigrateToCurrent(Workspace workspace, int fromVersion)
    {
        // No migration steps exist yet: version 1 is the initial schema. This method is
        // the designated seam so that introducing version 2 is a localized change rather
        // than a redesign. See docs/architecture/workspace-schema.md "Versioning and migration".
        workspace.SchemaVersion = WorkspaceSchema.CurrentVersion;
        return workspace;
    }
}
