using System.Text.RegularExpressions;
using DesktopRuntime.Core.Permissions;

namespace DesktopRuntime.Core.Widgets;

/// <summary>
/// Validates an as-authored <see cref="WidgetManifest"/> and, on success, produces a
/// <see cref="ValidatedWidgetManifest"/>. Every failure is collected and reported rather
/// than throwing on the first one, so a package author sees everything wrong at once.
/// See docs/architecture/widget-manifest.md.
/// </summary>
public static partial class WidgetManifestValidator
{
    // Deliberately narrow: lowercase alphanumerics and hyphens, in dot-separated
    // segments. This excludes path separators, '..', drive letters, whitespace, unicode
    // look-alikes and control characters by construction rather than by blocklist,
    // because the id is used to name on-disk storage for the package.
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*(\.[a-z0-9]+(-[a-z0-9]+)*)+$")]
    private static partial Regex IdPattern { get; }

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex VersionPattern { get; }

    public const int MinIdLength = 3;
    public const int MaxIdLength = 128;
    public const int MaxNameLength = 64;
    public const int MaxSizes = 16;

    public static WidgetManifestValidationResult Validate(WidgetManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var errors = new List<string>();

        if (manifest.ManifestVersion > WidgetManifestSchema.CurrentVersion)
        {
            // Same rule as the workspace schema: never partially interpret a document
            // written by a newer build.
            errors.Add($"Manifest version {manifest.ManifestVersion} is newer than this build supports " +
                       $"({WidgetManifestSchema.CurrentVersion}).");
        }
        else if (manifest.ManifestVersion < 1)
        {
            errors.Add("Manifest version must be 1 or greater.");
        }

        ValidateId(manifest.Id, errors);
        ValidateName(manifest.Name, errors);

        if (!VersionPattern.IsMatch(manifest.Version ?? string.Empty))
        {
            errors.Add("Version must be three numeric parts, e.g. '1.0.0'.");
        }

        var permissions = ValidatePermissions(manifest.Permissions, errors);
        ValidateSizes(manifest.Sizes, errors);
        ValidateResourceBudget(manifest.ResourceBudget, errors);

        if (errors.Count > 0)
        {
            return WidgetManifestValidationResult.Failed(errors);
        }

        return WidgetManifestValidationResult.Succeeded(
            new ValidatedWidgetManifest(manifest, permissions!));
    }

    private static void ValidateId(string? id, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add("Id is required.");
            return;
        }

        if (id.Length is < MinIdLength or > MaxIdLength)
        {
            errors.Add($"Id must be between {MinIdLength} and {MaxIdLength} characters.");
            return;
        }

        if (!IdPattern.IsMatch(id))
        {
            errors.Add("Id must be dot-separated lowercase alphanumeric segments, e.g. 'com.example.clock'. " +
                       "Path separators, '..', whitespace and uppercase are not permitted.");
        }
    }

    private static void ValidateName(string? name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
            return;
        }

        if (name.Length > MaxNameLength)
        {
            errors.Add($"Name must be {MaxNameLength} characters or fewer.");
        }

        foreach (char c in name)
        {
            if (char.IsControl(c))
            {
                // Control characters in a display name can be used to spoof what the
                // user sees in a consent prompt.
                errors.Add("Name must not contain control characters.");
                return;
            }
        }
    }

    private static PermissionSet? ValidatePermissions(List<string>? declarations, List<string> errors)
    {
        if (declarations is null || declarations.Count == 0)
        {
            // Declaring nothing is entirely valid and is the safest possible manifest.
            return PermissionSet.Empty;
        }

        try
        {
            return PermissionSet.FromDeclarations(declarations);
        }
        catch (CapabilityFormatException ex)
        {
            errors.Add($"Invalid permission declaration: {ex.Message}");
            return null;
        }
    }

    private static void ValidateSizes(List<WidgetSize>? sizes, List<string> errors)
    {
        if (sizes is null || sizes.Count == 0)
        {
            errors.Add("At least one size must be declared.");
            return;
        }

        if (sizes.Count > MaxSizes)
        {
            errors.Add($"No more than {MaxSizes} sizes may be declared.");
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var size in sizes)
        {
            if (string.IsNullOrWhiteSpace(size.Name))
            {
                errors.Add("Every size must have a name.");
            }
            else if (!seenNames.Add(size.Name))
            {
                errors.Add($"Duplicate size name '{size.Name}'.");
            }

            if (size.Width <= 0 || size.Height <= 0)
            {
                errors.Add($"Size '{size.Name}' must have positive width and height.");
            }
            else if (size.Width > WidgetManifestSchema.MaxDimension || size.Height > WidgetManifestSchema.MaxDimension)
            {
                errors.Add($"Size '{size.Name}' exceeds the maximum dimension of " +
                           $"{WidgetManifestSchema.MaxDimension}px.");
            }
        }
    }

    private static void ValidateResourceBudget(WidgetResourceBudget? budget, List<string> errors)
    {
        if (budget is null)
        {
            errors.Add("A resource budget is required: a widget must declare its expected resource cost.");
            return;
        }

        if (budget.IdleCpuPercent < 0 || budget.IdleCpuPercent > WidgetManifestSchema.MaxIdleCpuPercent)
        {
            errors.Add($"Declared idle CPU must be between 0 and {WidgetManifestSchema.MaxIdleCpuPercent}%.");
        }

        if (budget.MemoryMb <= 0 || budget.MemoryMb > WidgetManifestSchema.MaxMemoryMb)
        {
            errors.Add($"Declared memory must be between 1 and {WidgetManifestSchema.MaxMemoryMb} MB.");
        }

        if (budget.FramesPerSecond < 0 || budget.FramesPerSecond > WidgetManifestSchema.MaxFramesPerSecond)
        {
            errors.Add($"Declared frame rate must be between 0 (event-driven) and " +
                       $"{WidgetManifestSchema.MaxFramesPerSecond}.");
        }
    }
}

/// <summary>The outcome of validating a manifest: either a validated manifest or the reasons it failed.</summary>
public sealed class WidgetManifestValidationResult
{
    private WidgetManifestValidationResult(ValidatedWidgetManifest? manifest, IReadOnlyList<string> errors)
    {
        Manifest = manifest;
        Errors = errors;
    }

    public ValidatedWidgetManifest? Manifest { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Manifest is not null;

    internal static WidgetManifestValidationResult Succeeded(ValidatedWidgetManifest manifest) =>
        new(manifest, []);

    internal static WidgetManifestValidationResult Failed(IReadOnlyList<string> errors) =>
        new(null, errors);
}

/// <summary>
/// A manifest that has passed validation, paired with its parsed permission set.
/// Only obtainable from <see cref="WidgetManifestValidator"/>, so possessing one is
/// itself evidence the manifest was checked.
/// </summary>
public sealed class ValidatedWidgetManifest
{
    internal ValidatedWidgetManifest(WidgetManifest manifest, PermissionSet permissions)
    {
        Id = manifest.Id;
        Name = manifest.Name;
        Version = manifest.Version;
        Author = manifest.Author;
        Sizes = manifest.Sizes.AsReadOnly();
        ResourceBudget = manifest.ResourceBudget!;
        Permissions = permissions;
    }

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public string? Author { get; }

    public IReadOnlyList<WidgetSize> Sizes { get; }

    public WidgetResourceBudget ResourceBudget { get; }

    public PermissionSet Permissions { get; }
}
