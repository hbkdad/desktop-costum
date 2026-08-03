using DesktopRuntime.Core.Permissions;
using DesktopRuntime.Core.Widgets;

namespace DesktopRuntime.Core.Tests.Widgets;

public class WidgetManifestValidatorTests
{
    private static WidgetManifest CreateValidManifest() => new()
    {
        Id = "com.example.clock",
        Name = "Clock",
        Version = "1.0.0",
        Author = "Example",
        Permissions = ["system.metrics.read"],
        Sizes = [new WidgetSize { Name = "small", Width = 200, Height = 100 }],
        ResourceBudget = new WidgetResourceBudget { IdleCpuPercent = 0.1, MemoryMb = 32, FramesPerSecond = 1 }
    };

    [Fact]
    public void ValidManifest_IsAccepted_AndCarriesItsParsedPermissions()
    {
        var result = WidgetManifestValidator.Validate(CreateValidManifest());

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Manifest);
        Assert.Equal("com.example.clock", result.Manifest!.Id);
        Assert.True(result.Manifest.Permissions.IsGranted(CapabilityCatalog.SystemMetricsRead));
        Assert.False(result.Manifest.Permissions.IsGranted(CapabilityCatalog.ClipboardReadOnUserAction));
    }

    [Fact]
    public void ManifestDeclaringNoPermissions_IsValid_AndGrantsNothing()
    {
        var manifest = CreateValidManifest();
        manifest.Permissions = [];

        var result = WidgetManifestValidator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.False(result.Manifest!.Permissions.IsGranted(CapabilityCatalog.SystemMetricsRead));
    }

    [Fact]
    public void ManifestDeclaringAnUnknownPermission_IsRejected()
    {
        // The whole manifest fails rather than loading with the bad entry dropped.
        var manifest = CreateValidManifest();
        manifest.Permissions = ["system.metrics.read", "shell.execute"];

        var result = WidgetManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("shell.execute"));
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData(@"..\..\windows\system32")]
    [InlineData("com.example/clock")]
    [InlineData(@"com.example\clock")]
    [InlineData("C:.example.clock")]
    [InlineData("com..example")]
    [InlineData(".com.example")]
    [InlineData("com.example.")]
    [InlineData("com.example.Clock")]      // uppercase: would collide case-insensitively on disk
    [InlineData("com example clock")]
    [InlineData("com.example.clock ")]
    [InlineData("com.example.clock\0")]    // null byte: can truncate a path in native code
    [InlineData("clock")]                   // single segment, not reverse-DNS
    public void MaliciousOrMalformedIds_AreRejected(string id)
    {
        // The id names on-disk storage for the package, so path traversal and
        // case-collision tricks must be impossible by construction.
        var manifest = CreateValidManifest();
        manifest.Id = id;

        var result = WidgetManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void MissingResourceBudget_IsRejected()
    {
        // A widget that cannot state its resource cost cannot be accepted — the
        // resource-discipline gap is the #2 differentiator in the market-gap report.
        var manifest = CreateValidManifest();
        manifest.ResourceBudget = null;

        var result = WidgetManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("resource budget", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(99.0, 32, 1)]      // absurd CPU claim
    [InlineData(-1.0, 32, 1)]
    [InlineData(0.1, 100000, 1)]   // absurd memory claim
    [InlineData(0.1, 0, 1)]
    [InlineData(0.1, 32, 10000)]   // absurd frame rate
    [InlineData(0.1, 32, -1)]
    public void OutOfRangeResourceBudgets_AreRejected(double cpu, int memoryMb, int fps)
    {
        var manifest = CreateValidManifest();
        manifest.ResourceBudget = new WidgetResourceBudget
        {
            IdleCpuPercent = cpu,
            MemoryMb = memoryMb,
            FramesPerSecond = fps
        };

        var result = WidgetManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void EventDrivenWidget_MayDeclareZeroFramesPerSecond()
    {
        var manifest = CreateValidManifest();
        manifest.ResourceBudget = new WidgetResourceBudget
        {
            IdleCpuPercent = 0,
            MemoryMb = 16,
            FramesPerSecond = 0
        };

        Assert.True(WidgetManifestValidator.Validate(manifest).IsValid);
    }

    [Fact]
    public void MissingSizes_AreRejected()
    {
        var manifest = CreateValidManifest();
        manifest.Sizes = [];

        var result = WidgetManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("size", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-5, 100)]
    [InlineData(100000, 100)]   // unbounded surface is a cheap way to force huge allocations
    [InlineData(100, 100000)]
    public void InvalidSizeDimensions_AreRejected(int width, int height)
    {
        var manifest = CreateValidManifest();
        manifest.Sizes = [new WidgetSize { Name = "bad", Width = width, Height = height }];

        Assert.False(WidgetManifestValidator.Validate(manifest).IsValid);
    }

    [Fact]
    public void DuplicateSizeNames_AreRejected()
    {
        var manifest = CreateValidManifest();
        manifest.Sizes =
        [
            new WidgetSize { Name = "small", Width = 200, Height = 100 },
            new WidgetSize { Name = "SMALL", Width = 300, Height = 150 }
        ];

        Assert.False(WidgetManifestValidator.Validate(manifest).IsValid);
    }

    [Fact]
    public void NewerManifestVersion_IsRejected()
    {
        var manifest = CreateValidManifest();
        manifest.ManifestVersion = WidgetManifestSchema.CurrentVersion + 1;

        var result = WidgetManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("newer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NameWithControlCharacters_IsRejected()
    {
        // Control characters in a display name can spoof what the user sees in a
        // consent prompt (here: a bell plus an ANSI erase-display sequence).
        var manifest = CreateValidManifest();
        manifest.Name = "Clock" + (char)0x07 + (char)0x1b + "[2J";

        Assert.False(WidgetManifestValidator.Validate(manifest).IsValid);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    [InlineData("1.0.0-beta")]
    [InlineData("")]
    public void MalformedVersions_AreRejected(string version)
    {
        var manifest = CreateValidManifest();
        manifest.Version = version;

        Assert.False(WidgetManifestValidator.Validate(manifest).IsValid);
    }

    [Fact]
    public void AllValidationErrors_AreReportedTogether()
    {
        // An author should see everything wrong at once, not fix-and-retry one at a time.
        var manifest = new WidgetManifest
        {
            Id = "BAD ID",
            Name = "",
            Version = "nope",
            Permissions = ["shell.execute"],
            Sizes = [],
            ResourceBudget = null
        };

        var result = WidgetManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 5, $"Expected several errors, got: {string.Join("; ", result.Errors)}");
    }
}
