using DesktopRuntime.Core.Permissions;

namespace DesktopRuntime.Core.Tests.Permissions;

public class CapabilityTests
{
    [Fact]
    public void Parse_AcceptsTheDocumentedManifestExamples()
    {
        // The exact set used as the worked example in docs/architecture/permission-model.md.
        string[] declarations =
        [
            "system.metrics.read",
            "network.domain:api.example.com",
            "files.user-selected.read",
            "clipboard.read-on-user-action",
            "process.launch:declared-application"
        ];

        var permissions = PermissionSet.FromDeclarations(declarations);

        Assert.Equal(5, permissions.Granted.Count);
    }

    [Theory]
    [InlineData("system.metrics.write")]      // plausible-looking but not in the catalog
    [InlineData("shell.execute")]
    [InlineData("powershell.run")]
    [InlineData("native.loadLibrary")]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_RejectsUnknownOrEmptyCapabilities(string declaration)
    {
        // Unknown names fail loudly rather than being dropped: silently ignoring one
        // would let a package appear to declare less than it actually does.
        Assert.Throws<CapabilityFormatException>(() => Capability.Parse(declaration));
    }

    [Fact]
    public void Catalog_ContainsNoArbitraryExecutionCapability()
    {
        // Guards the non-negotiable: marketplace content must never be able to run
        // arbitrary PowerShell, CMD, JavaScript outside an isolated runtime, native
        // DLLs, or unsigned binaries. The defence is that no such capability exists
        // to declare in the first place.
        string[] forbiddenFragments = ["exec", "shell", "powershell", "cmd", "script", "eval", "loadlibrary", "dll"];

        foreach (string name in CapabilityCatalog.KnownNames)
        {
            foreach (string fragment in forbiddenFragments)
            {
                Assert.False(
                    name.Contains(fragment, StringComparison.OrdinalIgnoreCase),
                    $"Capability '{name}' looks like an arbitrary-execution capability ('{fragment}').");
            }
        }
    }

    [Theory]
    [InlineData("network.domain")]                      // scope required but missing
    [InlineData("network.domain:")]                     // empty scope
    [InlineData("network.domain:*.example.com")]        // wildcards would be an open-ended grant
    [InlineData("network.domain:*")]
    [InlineData("network.domain:https://example.com")]  // scheme invites parser confusion
    [InlineData("network.domain:example.com/path")]
    [InlineData("network.domain:user@example.com")]
    [InlineData("network.domain:not a host")]
    public void Parse_RejectsInvalidNetworkScopes(string declaration)
    {
        Assert.Throws<CapabilityFormatException>(() => Capability.Parse(declaration));
    }

    [Theory]
    [InlineData("process.launch")]                          // scope required but missing
    [InlineData("process.launch:")]
    [InlineData("process.launch:cmd.exe /c del *.*")]       // a command line, not an application id
    [InlineData("process.launch:app & evil")]
    [InlineData("process.launch:app | evil")]
    [InlineData("process.launch:app; evil")]
    [InlineData(@"process.launch:C:\Windows\System32\cmd.exe")]
    public void Parse_RejectsCommandLinesDisguisedAsApplicationIdentifiers(string declaration)
    {
        Assert.Throws<CapabilityFormatException>(() => Capability.Parse(declaration));
    }

    [Fact]
    public void Parse_RejectsAScopeOnAnUnscopedCapability()
    {
        Assert.Throws<CapabilityFormatException>(() => Capability.Parse("system.metrics.read:everything"));
    }

    [Fact]
    public void Parse_NormalizesHostScope_SoComparisonsStayExact()
    {
        var capability = Capability.Parse("network.domain:API.Example.COM.");

        Assert.Equal("network.domain", capability.Name);
        Assert.Equal("api.example.com", capability.Scope);
    }

    [Fact]
    public void EqualCapabilities_AreDeduplicated()
    {
        var permissions = PermissionSet.FromDeclarations(
        [
            "network.domain:example.com",
            "network.domain:EXAMPLE.COM"
        ]);

        Assert.Single(permissions.Granted);
    }
}
