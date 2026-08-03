using DesktopRuntime.Core.Permissions;

namespace DesktopRuntime.Core.Tests.Permissions;

public class PermissionSetTests
{
    [Fact]
    public void EmptySet_GrantsNothing()
    {
        var permissions = PermissionSet.Empty;

        Assert.False(permissions.IsGranted(CapabilityCatalog.SystemMetricsRead));
        Assert.False(permissions.IsGranted(CapabilityCatalog.ClipboardReadOnUserAction));
        Assert.False(permissions.IsGranted(CapabilityCatalog.FilesUserSelectedRead));
        Assert.False(permissions.IsGranted(CapabilityCatalog.NetworkDomain, "api.example.com"));
        Assert.False(permissions.IsGranted(CapabilityCatalog.ProcessLaunch, "declared-application"));
    }

    [Fact]
    public void DeclaringOneCapability_DoesNotGrantAnother()
    {
        var permissions = PermissionSet.FromDeclarations([CapabilityCatalog.SystemMetricsRead]);

        Assert.True(permissions.IsGranted(CapabilityCatalog.SystemMetricsRead));
        Assert.False(permissions.IsGranted(CapabilityCatalog.ClipboardReadOnUserAction));
        Assert.False(permissions.IsGranted(CapabilityCatalog.FilesUserSelectedRead));
    }

    [Fact]
    public void ScopedCapability_IsNeverGrantedWithoutAScope()
    {
        // Treating a missing scope as "any host" would be exactly the implicit widening
        // this model exists to prevent.
        var permissions = PermissionSet.FromDeclarations(["network.domain:api.example.com"]);

        Assert.False(permissions.IsGranted(CapabilityCatalog.NetworkDomain));
        Assert.False(permissions.IsGranted(CapabilityCatalog.NetworkDomain, scope: null));
        Assert.True(permissions.IsGranted(CapabilityCatalog.NetworkDomain, "api.example.com"));
    }

    [Fact]
    public void UnscopedCapability_CannotBeInvokedWithAScope()
    {
        var permissions = PermissionSet.FromDeclarations([CapabilityCatalog.SystemMetricsRead]);

        Assert.False(permissions.IsGranted(CapabilityCatalog.SystemMetricsRead, "anything"));
    }

    [Theory]
    // The classic host-confusion attacks: a grant for example.com must not leak to any of these.
    [InlineData("evil.com")]
    [InlineData("sub.example.com")]          // subdomains are NOT implied
    [InlineData("example.com.evil.com")]     // suffix attack
    [InlineData("notexample.com")]           // prefix attack
    [InlineData("example.co")]
    [InlineData("xample.com")]
    public void NetworkGrant_DoesNotLeakToOtherHosts(string otherHost)
    {
        var permissions = PermissionSet.FromDeclarations(["network.domain:example.com"]);

        Assert.True(permissions.IsGranted(CapabilityCatalog.NetworkDomain, "example.com"));
        Assert.False(permissions.IsGranted(CapabilityCatalog.NetworkDomain, otherHost));
    }

    [Fact]
    public void NetworkGrant_MatchesHostCaseInsensitively()
    {
        // DNS is case-insensitive; a case-sensitive comparison would break legitimate
        // requests without adding any security.
        var permissions = PermissionSet.FromDeclarations(["network.domain:API.Example.COM"]);

        Assert.True(permissions.IsGranted(CapabilityCatalog.NetworkDomain, "api.example.com"));
        Assert.True(permissions.IsGranted(CapabilityCatalog.NetworkDomain, "API.EXAMPLE.COM"));
    }

    [Fact]
    public void ProcessLaunchGrant_IsLimitedToTheDeclaredApplication()
    {
        var permissions = PermissionSet.FromDeclarations(["process.launch:declared-application"]);

        Assert.True(permissions.IsGranted(CapabilityCatalog.ProcessLaunch, "declared-application"));
        Assert.False(permissions.IsGranted(CapabilityCatalog.ProcessLaunch, "some-other-application"));
    }

    [Fact]
    public void MultipleHosts_RequireOneDeclarationEach()
    {
        var permissions = PermissionSet.FromDeclarations(
        [
            "network.domain:a.example.com",
            "network.domain:b.example.com"
        ]);

        Assert.True(permissions.IsGranted(CapabilityCatalog.NetworkDomain, "a.example.com"));
        Assert.True(permissions.IsGranted(CapabilityCatalog.NetworkDomain, "b.example.com"));
        Assert.False(permissions.IsGranted(CapabilityCatalog.NetworkDomain, "c.example.com"));
    }

    [Fact]
    public void RequestingAnUnknownCapability_IsDenied()
    {
        var permissions = PermissionSet.FromDeclarations([CapabilityCatalog.SystemMetricsRead]);

        Assert.False(permissions.IsGranted("system.metrics.write"));
        Assert.False(permissions.IsGranted("process.execute", "cmd.exe"));
    }
}
