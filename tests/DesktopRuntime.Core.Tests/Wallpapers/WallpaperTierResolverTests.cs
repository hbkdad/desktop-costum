using DesktopRuntime.Core.Wallpapers;
using DesktopRuntime.Core.Workspaces;

namespace DesktopRuntime.Core.Tests.Wallpapers;

public class WallpaperTierResolverTests
{
    private static readonly WallpaperHostCapabilities AttachmentWorks = new(AttachedSurfaceAvailable: true);
    private static readonly WallpaperHostCapabilities AttachmentUnavailable = new(AttachedSurfaceAvailable: false);

    private static WallpaperAssignment Video() =>
        new() { Kind = WallpaperKind.Video, SourcePath = @"C:\wallpapers\loop.mp4" };

    private static WallpaperAssignment Static() =>
        new() { Kind = WallpaperKind.Static, SourcePath = @"C:\wallpapers\still.jpg" };

    [Fact]
    public void Video_UsesTheAttachedSurface_WhenAttachmentIsAvailable()
    {
        var decision = WallpaperTierResolver.Resolve(Video(), AttachmentWorks);

        Assert.Equal(WallpaperTier.AttachedSurface, decision.SelectedTier);
        Assert.False(decision.IsDegraded);
        Assert.Null(decision.DegradationReason);
    }

    [Fact]
    public void Video_FallsBackToStatic_AndReportsTheDegradation_WhenAttachmentIsUnavailable()
    {
        // The case Phase 3 Prototype 1 found on the current Windows build. PRD §13.7
        // requires this to be visible to the user rather than silently different.
        var decision = WallpaperTierResolver.Resolve(Video(), AttachmentUnavailable);

        Assert.Equal(WallpaperTier.StaticImage, decision.SelectedTier);
        Assert.True(decision.IsDegraded);
        Assert.False(string.IsNullOrWhiteSpace(decision.DegradationReason));
    }

    [Fact]
    public void DegradationReason_IsPresentExactlyWhenDegraded()
    {
        // Guards the invariant the UI relies on: never a degraded state with nothing to
        // show the user, and never a spurious message on a normal outcome.
        WallpaperAssignment[] assignments = [Static(), Video()];
        WallpaperHostCapabilities[] capabilities = [AttachmentWorks, AttachmentUnavailable];

        foreach (var assignment in assignments)
        {
            foreach (var capability in capabilities)
            {
                var decision = WallpaperTierResolver.Resolve(assignment, capability);

                Assert.Equal(decision.IsDegraded, decision.DegradationReason is not null);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Static_AlwaysUsesTheStaticTier_AndIsNeverDegraded(bool attachmentAvailable)
    {
        // A still image is served by the robust path even when attachment is available:
        // spending a scarce, fragile surface on a still image buys nothing.
        var decision = WallpaperTierResolver.Resolve(Static(), new WallpaperHostCapabilities(attachmentAvailable));

        Assert.Equal(WallpaperTier.StaticImage, decision.SelectedTier);
        Assert.False(decision.IsDegraded);
    }

    [Fact]
    public void Resolve_PreservesTheRequestedKind_SoTheRequestIsNeverRewritten()
    {
        // The decision reports what the user asked for alongside what was delivered.
        // Persisting the delivered tier as the request would make degradation sticky —
        // see workspace-schema.md decision 4.
        var decision = WallpaperTierResolver.Resolve(Video(), AttachmentUnavailable);

        Assert.Equal(WallpaperKind.Video, decision.RequestedKind);
        Assert.Equal(WallpaperTier.StaticImage, decision.SelectedTier);
    }

    [Fact]
    public void EveryWallpaperKind_HasAnExplicitTierDecision()
    {
        // If a kind is added without deciding its tier, this fails rather than letting it
        // fall through to a silent default.
        foreach (WallpaperKind kind in Enum.GetValues<WallpaperKind>())
        {
            var assignment = new WallpaperAssignment { Kind = kind, SourcePath = "x" };

            foreach (var capability in new[] { AttachmentWorks, AttachmentUnavailable })
            {
                var exception = Record.Exception(() => WallpaperTierResolver.Resolve(assignment, capability));

                Assert.Null(exception);
            }
        }
    }
}
