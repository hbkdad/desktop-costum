namespace DesktopRuntime.Core.Packaging;

/// <summary>One entry in a package archive, as declared by the archive itself (untrusted).</summary>
public readonly record struct PackageEntry(string Path, long UncompressedBytes, long CompressedBytes);

/// <summary>
/// The signing state of a package, as reported by a platform signature verifier.
/// <para>
/// This type carries a <em>verdict</em>, never key material or raw signature bytes.
/// Verification itself is deliberately not implemented in this project — see
/// <see cref="IPackageSignatureVerifier"/>.
/// </para>
/// </summary>
public sealed class PackageSignature
{
    private PackageSignature(bool isValid, string? publisherId, string? failureReason)
    {
        IsValid = isValid;
        PublisherId = publisherId;
        FailureReason = failureReason;
    }

    public bool IsValid { get; }

    /// <summary>Stable publisher identity from the verified certificate. Null when unverified.</summary>
    public string? PublisherId { get; }

    public string? FailureReason { get; }

    public static PackageSignature Valid(string publisherId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publisherId);
        return new PackageSignature(true, publisherId, null);
    }

    public static PackageSignature Invalid(string reason) => new(false, null, reason);

    /// <summary>No signature was present at all.</summary>
    public static PackageSignature Absent { get; } = new(false, null, "the package is not signed.");
}

/// <summary>
/// Verifies a package's cryptographic signature.
/// <para>
/// Intentionally an abstraction with no implementation here. Signature verification must
/// be done with platform cryptography and certificate-chain validation, not hand-rolled —
/// so this project defines the <em>policy</em> (what a valid signature entitles a package
/// to) and leaves the cryptography to the platform.
/// </para>
/// </summary>
public interface IPackageSignatureVerifier
{
    PackageSignature Verify(string packagePath);
}

public sealed class PackageValidationOptions
{
    /// <summary>
    /// Whether an unsigned package may be installed. False in normal operation; a host may
    /// enable it for a developer/sideload mode, which must be an explicit, visible choice.
    /// </summary>
    public bool AllowUnsignedPackages { get; init; }

    public long MaxTotalUncompressedBytes { get; init; } = 256L * 1024 * 1024;

    public int MaxEntries { get; init; } = 2_000;

    /// <summary>
    /// Largest allowed uncompressed:compressed ratio for a single entry. A decompression
    /// bomb is characterised by an extreme ratio, so this catches one entry that would
    /// expand to far more than its size suggests, before any bytes are written.
    /// </summary>
    public double MaxCompressionRatio { get; init; } = 100.0;

    /// <summary>
    /// Content types a package may contain. An allowlist, not a blocklist, for the same
    /// reason widget ids use one: a blocklist is a promise to have thought of every bad
    /// extension, and only needs to be wrong once.
    /// </summary>
    public IReadOnlySet<string> AllowedExtensions { get; init; } = DefaultAllowedExtensions;

    public static IReadOnlySet<string> DefaultAllowedExtensions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".json",                                    // manifests and data
            ".png", ".jpg", ".jpeg", ".webp", ".gif",   // images
            ".mp4", ".webm",                            // video wallpapers
            ".woff2",                                   // fonts
            ".txt", ".md"                               // readme / licence
            // Deliberately excluded: .svg (can embed script), and .html/.css/.js — web
            // content is deferred past MVP (PRD §2) and needs the isolated web runtime,
            // not a file in a package directory.
        };
}

/// <summary>
/// Validates the structure of an untrusted package before anything is extracted or run.
/// See docs/architecture/package-format.md.
/// </summary>
public static class PackageValidator
{
    public const string ManifestEntryName = "manifest.json";

    public static PackageValidationResult Validate(
        IReadOnlyList<PackageEntry> entries,
        PackageSignature signature,
        PackageValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(signature);

        options ??= new PackageValidationOptions();
        var errors = new List<string>();

        ValidateSignature(signature, options, errors);
        ValidateEntries(entries, options, errors);

        return errors.Count > 0
            ? PackageValidationResult.Failed(errors)
            : PackageValidationResult.Succeeded(signature.PublisherId);
    }

    private static void ValidateSignature(
        PackageSignature signature,
        PackageValidationOptions options,
        List<string> errors)
    {
        if (signature.IsValid)
        {
            return;
        }

        if (!options.AllowUnsignedPackages)
        {
            errors.Add($"The package signature could not be verified: {signature.FailureReason}");
            return;
        }

        // Even when sideloading is permitted, a *broken* signature is different from no
        // signature: it suggests tampering with something that was signed, so it is
        // refused regardless.
        if (!ReferenceEquals(signature, PackageSignature.Absent))
        {
            errors.Add($"The package signature is present but invalid: {signature.FailureReason}");
        }
    }

    private static void ValidateEntries(
        IReadOnlyList<PackageEntry> entries,
        PackageValidationOptions options,
        List<string> errors)
    {
        if (entries.Count == 0)
        {
            errors.Add("The package is empty.");
            return;
        }

        if (entries.Count > options.MaxEntries)
        {
            errors.Add($"The package contains {entries.Count} entries, exceeding the limit of {options.MaxEntries}.");
        }

        long totalUncompressed = 0;
        bool manifestFound = false;
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (!PackagePath.TryNormalize(entry.Path, out string? normalized, out string? pathError))
            {
                errors.Add($"Rejected entry '{Describe(entry.Path)}': {pathError}");
                continue;
            }

            // Case-insensitive, because two entries differing only in case collide on
            // Windows — the second silently overwrites the first.
            if (!seenPaths.Add(normalized))
            {
                errors.Add($"Duplicate entry '{normalized}' (paths differing only in case collide on Windows).");
                continue;
            }

            if (string.Equals(normalized, ManifestEntryName, StringComparison.OrdinalIgnoreCase))
            {
                manifestFound = true;
            }

            string extension = System.IO.Path.GetExtension(normalized);
            if (!options.AllowedExtensions.Contains(extension))
            {
                errors.Add($"Entry '{normalized}' has content type '{extension}', which packages may not contain.");
            }

            if (entry.UncompressedBytes < 0 || entry.CompressedBytes < 0)
            {
                errors.Add($"Entry '{normalized}' declares a negative size.");
                continue;
            }

            if (entry.CompressedBytes > 0)
            {
                double ratio = (double)entry.UncompressedBytes / entry.CompressedBytes;
                if (ratio > options.MaxCompressionRatio)
                {
                    errors.Add($"Entry '{normalized}' expands {ratio:0}x, above the limit of " +
                               $"{options.MaxCompressionRatio:0}x (possible decompression bomb).");
                }
            }
            else if (entry.UncompressedBytes > 0)
            {
                errors.Add($"Entry '{normalized}' claims to expand from nothing.");
            }

            totalUncompressed += entry.UncompressedBytes;
        }

        if (totalUncompressed > options.MaxTotalUncompressedBytes)
        {
            errors.Add($"The package expands to {totalUncompressed} bytes, above the limit of " +
                       $"{options.MaxTotalUncompressedBytes}.");
        }

        if (!manifestFound)
        {
            errors.Add($"The package does not contain a '{ManifestEntryName}' at its root.");
        }
    }

    /// <summary>
    /// Renders a rejected path safely for an error message. A hostile path may contain
    /// control characters intended to corrupt a log or terminal.
    /// </summary>
    private static string Describe(string? path)
    {
        if (string.IsNullOrEmpty(path)) return "(empty)";

        var builder = new System.Text.StringBuilder(path.Length);
        foreach (char c in path)
        {
            builder.Append(char.IsControl(c) ? '�' : c);
        }

        return builder.Length > 120 ? builder.ToString(0, 120) + "…" : builder.ToString();
    }
}

public sealed class PackageValidationResult
{
    private PackageValidationResult(bool isValid, string? publisherId, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        PublisherId = publisherId;
        Errors = errors;
    }

    public bool IsValid { get; }

    /// <summary>Verified publisher, when the package was signed. Null for accepted sideloads.</summary>
    public string? PublisherId { get; }

    public IReadOnlyList<string> Errors { get; }

    internal static PackageValidationResult Succeeded(string? publisherId) => new(true, publisherId, []);

    internal static PackageValidationResult Failed(IReadOnlyList<string> errors) => new(false, null, errors);
}
