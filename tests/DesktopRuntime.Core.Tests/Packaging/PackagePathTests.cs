using DesktopRuntime.Core.Packaging;

namespace DesktopRuntime.Core.Tests.Packaging;

public class PackagePathTests
{
    [Theory]
    [InlineData("manifest.json")]
    [InlineData("assets/background.png")]
    [InlineData("assets/fonts/inter.woff2")]
    [InlineData("a/b/c/d/e/f/g.json")]
    public void LegitimatePaths_AreAccepted(string path)
    {
        Assert.True(PackagePath.TryNormalize(path, out _, out string? error), error);
    }

    [Fact]
    public void BackslashSeparators_AreNormalizedToForwardSlashes()
    {
        Assert.True(PackagePath.TryNormalize(@"assets\images\bg.png", out string? normalized, out _));

        Assert.Equal("assets/images/bg.png", normalized);
    }

    // --- Zip slip: the reason this class exists ---

    [Theory]
    [InlineData("../evil.json")]
    [InlineData("../../../windows/system32/evil.json")]
    [InlineData(@"..\..\evil.json")]
    [InlineData("assets/../../evil.json")]
    [InlineData("assets/./../../evil.json")]
    [InlineData("./evil.json")]
    public void PathTraversal_IsRejected(string path)
    {
        // An archive is attacker-authored data; a path that escapes the extraction
        // directory writes attacker-chosen bytes to an attacker-chosen location.
        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData(@"\windows\system32\evil.json")]
    [InlineData("C:/windows/evil.json")]
    [InlineData(@"C:\windows\evil.json")]
    public void RootedAndDriveQualifiedPaths_AreRejected(string path)
    {
        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    // --- Windows-specific traps ---

    [Theory]
    [InlineData("CON")]
    [InlineData("con.json")]
    [InlineData("assets/NUL.png")]
    [InlineData("PRN.txt")]
    [InlineData("aux/thing.json")]
    [InlineData("COM1.json")]
    [InlineData("LPT9.txt")]
    public void ReservedWindowsDeviceNames_AreRejected(string path)
    {
        // Windows treats these as devices in any directory, with or without an extension.
        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Theory]
    [InlineData("evil.json.")]
    [InlineData("assets/thing. ")]
    [InlineData(" leading.json")]
    [InlineData("trailing .json ")]
    public void TrailingDotsAndSpaces_AreRejected(string path)
    {
        // Windows silently strips these, so "evil." and "evil" resolve to the same file —
        // a way to smuggle a second entry past a naive duplicate check.
        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Theory]
    [InlineData("notes.txt:hidden")]
    [InlineData("assets/image.png:$DATA")]
    public void AlternateDataStreams_AreRejected(string path)
    {
        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Theory]
    [InlineData("assets//bg.png")]
    [InlineData("assets/")]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptySegments_AreRejected(string path)
    {
        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Theory]
    [InlineData("bad<name>.json")]
    [InlineData("wild*.json")]
    [InlineData("what?.json")]
    [InlineData("pipe|.json")]
    public void CharactersInvalidOnWindows_AreRejected(string path)
    {
        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Fact]
    public void ControlCharactersInPaths_AreRejected()
    {
        string path = "assets/" + (char)0x1b + "[2Kbg.png";

        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Fact]
    public void ExcessivelyDeepPaths_AreRejected()
    {
        string path = string.Join('/', Enumerable.Repeat("a", PackagePath.MaxDepth)) + "/file.json";

        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Fact]
    public void ExcessivelyLongPaths_AreRejected()
    {
        string path = "assets/" + new string('a', PackagePath.MaxPathLength) + ".json";

        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }

    [Fact]
    public void ExcessivelyLongSegments_AreRejected()
    {
        string path = new string('a', PackagePath.MaxSegmentLength + 1) + ".json";

        Assert.False(PackagePath.TryNormalize(path, out _, out _));
    }
}
