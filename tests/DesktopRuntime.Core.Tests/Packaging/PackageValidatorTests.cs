using DesktopRuntime.Core.Packaging;

namespace DesktopRuntime.Core.Tests.Packaging;

public class PackageValidatorTests
{
    private static PackageSignature SignedByExample => PackageSignature.Valid("CN=Example Publisher");

    private static List<PackageEntry> ValidEntries() =>
    [
        new("manifest.json", 1_024, 400),
        new("assets/background.png", 500_000, 480_000)
    ];

    [Fact]
    public void WellFormedSignedPackage_IsAccepted_AndCarriesThePublisher()
    {
        var result = PackageValidator.Validate(ValidEntries(), SignedByExample);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("CN=Example Publisher", result.PublisherId);
    }

    // --- Signing policy ---

    [Fact]
    public void UnsignedPackage_IsRejectedByDefault()
    {
        var result = PackageValidator.Validate(ValidEntries(), PackageSignature.Absent);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("signature", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnsignedPackage_IsAcceptedOnlyWhenSideloadingIsExplicitlyEnabled()
    {
        var options = new PackageValidationOptions { AllowUnsignedPackages = true };

        var result = PackageValidator.Validate(ValidEntries(), PackageSignature.Absent, options);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Null(result.PublisherId);
    }

    [Fact]
    public void BrokenSignature_IsRejectedEvenWhenSideloadingIsEnabled()
    {
        // A broken signature is not the same as no signature: it suggests tampering with
        // something that was signed, so permitting sideloads must not permit this.
        var options = new PackageValidationOptions { AllowUnsignedPackages = true };
        var tampered = PackageSignature.Invalid("digest mismatch");

        var result = PackageValidator.Validate(ValidEntries(), tampered, options);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    }

    // --- Structure ---

    [Fact]
    public void PackageWithoutAManifest_IsRejected()
    {
        List<PackageEntry> entries = [new("assets/background.png", 500_000, 480_000)];

        var result = PackageValidator.Validate(entries, SignedByExample);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("manifest.json"));
    }

    [Fact]
    public void EmptyPackage_IsRejected()
    {
        var result = PackageValidator.Validate([], SignedByExample);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void EntriesDifferingOnlyInCase_AreRejectedAsDuplicates()
    {
        // They collide on Windows: the second silently overwrites the first.
        List<PackageEntry> entries =
        [
            new("manifest.json", 1_024, 400),
            new("assets/Logo.png", 1_000, 900),
            new("assets/logo.png", 1_000, 900)
        ];

        var result = PackageValidator.Validate(entries, SignedByExample);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PathTraversalEntry_IsRejected()
    {
        List<PackageEntry> entries =
        [
            new("manifest.json", 1_024, 400),
            new("../../../windows/system32/evil.json", 100, 50)
        ];

        var result = PackageValidator.Validate(entries, SignedByExample);

        Assert.False(result.IsValid);
    }

    // --- Content types: allowlist, not blocklist ---

    [Theory]
    [InlineData("payload.exe")]
    [InlineData("payload.dll")]
    [InlineData("script.ps1")]
    [InlineData("script.bat")]
    [InlineData("script.cmd")]
    [InlineData("script.js")]
    [InlineData("page.html")]
    [InlineData("vector.svg")]     // can embed script
    [InlineData("installer.msi")]
    [InlineData("shortcut.lnk")]
    [InlineData("settings.reg")]
    [InlineData("noextension")]
    public void ExecutableAndUnlistedContent_IsRejected(string path)
    {
        List<PackageEntry> entries =
        [
            new("manifest.json", 1_024, 400),
            new(path, 1_000, 900)
        ];

        var result = PackageValidator.Validate(entries, SignedByExample);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("content type", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("assets/image.png")]
    [InlineData("assets/photo.JPG")]     // extension matching is case-insensitive
    [InlineData("assets/loop.mp4")]
    [InlineData("assets/font.woff2")]
    [InlineData("README.md")]
    public void AllowedContentTypes_AreAccepted(string path)
    {
        List<PackageEntry> entries =
        [
            new("manifest.json", 1_024, 400),
            new(path, 1_000, 900)
        ];

        Assert.True(PackageValidator.Validate(entries, SignedByExample).IsValid);
    }

    // --- Decompression bombs ---

    [Fact]
    public void EntryWithAnExtremeCompressionRatio_IsRejected()
    {
        // Caught from declared sizes, before a single byte is written to disk.
        List<PackageEntry> entries =
        [
            new("manifest.json", 1_024, 400),
            new("assets/bomb.png", 10L * 1024 * 1024 * 1024, 1_000)
        ];

        var result = PackageValidator.Validate(entries, SignedByExample);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("bomb", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PackageExceedingTotalExpandedSize_IsRejected()
    {
        // Individually plausible entries can still add up to an unacceptable total,
        // so the aggregate is checked as well as each entry.
        var options = new PackageValidationOptions { MaxTotalUncompressedBytes = 1_000_000 };
        List<PackageEntry> entries = [new("manifest.json", 1_024, 400)];

        for (int i = 0; i < 20; i++)
        {
            entries.Add(new($"assets/image{i}.png", 100_000, 90_000));
        }

        var result = PackageValidator.Validate(entries, SignedByExample, options);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("expands to"));
    }

    [Fact]
    public void PackageWithTooManyEntries_IsRejected()
    {
        var options = new PackageValidationOptions { MaxEntries = 10 };
        List<PackageEntry> entries = [new("manifest.json", 100, 50)];

        for (int i = 0; i < 20; i++)
        {
            entries.Add(new($"assets/image{i}.png", 100, 50));
        }

        Assert.False(PackageValidator.Validate(entries, SignedByExample, options).IsValid);
    }

    [Fact]
    public void EntryClaimingToExpandFromNothing_IsRejected()
    {
        List<PackageEntry> entries =
        [
            new("manifest.json", 1_024, 400),
            new("assets/impossible.png", 1_000_000, 0)
        ];

        Assert.False(PackageValidator.Validate(entries, SignedByExample).IsValid);
    }

    [Fact]
    public void NegativeSizes_AreRejected()
    {
        List<PackageEntry> entries =
        [
            new("manifest.json", 1_024, 400),
            new("assets/odd.png", -1, 100)
        ];

        Assert.False(PackageValidator.Validate(entries, SignedByExample).IsValid);
    }

    [Fact]
    public void EmptyFiles_AreAccepted()
    {
        // A zero-byte file is legitimate and must not be mistaken for a bomb or a
        // negative size.
        List<PackageEntry> entries =
        [
            new("manifest.json", 1_024, 400),
            new("assets/placeholder.txt", 0, 0)
        ];

        Assert.True(PackageValidator.Validate(entries, SignedByExample).IsValid);
    }

    [Fact]
    public void ErrorMessages_DoNotEchoControlCharactersFromHostilePaths()
    {
        // A hostile path may carry terminal escapes intended to corrupt a log.
        string hostile = "assets/" + (char)0x1b + "[2Jgotcha.png";
        List<PackageEntry> entries = [new("manifest.json", 1_024, 400), new(hostile, 100, 50)];

        var result = PackageValidator.Validate(entries, SignedByExample);

        Assert.False(result.IsValid);
        Assert.All(result.Errors, error => Assert.DoesNotContain(error, c => char.IsControl(c)));
    }

    [Fact]
    public void AllStructuralErrors_AreReportedTogether()
    {
        List<PackageEntry> entries =
        [
            new("../escape.json", 100, 50),
            new("payload.exe", 100, 50)
        ];

        var result = PackageValidator.Validate(entries, PackageSignature.Absent);

        Assert.False(result.IsValid);
        // Unsigned, traversal, disallowed content type, and no manifest.
        Assert.True(result.Errors.Count >= 4, $"Got: {string.Join("; ", result.Errors)}");
    }
}
