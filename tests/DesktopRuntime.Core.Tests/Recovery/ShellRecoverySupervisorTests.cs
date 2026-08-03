using DesktopRuntime.Core.Recovery;

namespace DesktopRuntime.Core.Tests.Recovery;

public class ShellRecoverySupervisorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);

    private static ShellRecoveryOptions FastOptions() => new()
    {
        GracePeriod = TimeSpan.FromSeconds(5),
        BaseBackoff = TimeSpan.FromSeconds(2),
        MaxBackoff = TimeSpan.FromSeconds(30),
        MaxAttempts = 3,
        StabilityWindow = TimeSpan.FromMinutes(1)
    };

    [Fact]
    public void HealthyShell_RequiresNoAction()
    {
        var supervisor = new ShellRecoverySupervisor(FastOptions());

        Assert.Equal(RecoveryAction.None, supervisor.Evaluate(shellPresent: true, T0));
        Assert.Equal(0, supervisor.Attempts);
    }

    [Fact]
    public void ShellDisappearing_DoesNotRelaunchImmediately()
    {
        // Prototype 3: Windows restarts explorer.exe itself, and racing it produces a
        // duplicate process and a stray window.
        var supervisor = new ShellRecoverySupervisor(FastOptions());

        Assert.Equal(RecoveryAction.Wait, supervisor.Evaluate(shellPresent: false, T0));
        Assert.Equal(RecoveryAction.Wait, supervisor.Evaluate(shellPresent: false, T0.AddSeconds(1)));
        Assert.Equal(RecoveryAction.Wait, supervisor.Evaluate(shellPresent: false, T0.AddSeconds(4.9)));
        Assert.Equal(0, supervisor.Attempts);
    }

    [Fact]
    public void OsSelfRecoveryWithinGracePeriod_MeansWeNeverRelaunch()
    {
        // The observed real-world case: the shell came back on its own in ~2.7s.
        var supervisor = new ShellRecoverySupervisor(FastOptions());

        supervisor.Evaluate(shellPresent: false, T0);
        supervisor.Evaluate(shellPresent: false, T0.AddSeconds(2));
        var afterRecovery = supervisor.Evaluate(shellPresent: true, T0.AddSeconds(3));

        Assert.Equal(RecoveryAction.None, afterRecovery);
        Assert.Equal(0, supervisor.Attempts);
    }

    [Fact]
    public void ShellStillMissingAfterGracePeriod_TriggersRelaunch()
    {
        var supervisor = new ShellRecoverySupervisor(FastOptions());

        supervisor.Evaluate(shellPresent: false, T0);

        Assert.Equal(RecoveryAction.Relaunch, supervisor.Evaluate(shellPresent: false, T0.AddSeconds(5)));
        Assert.Equal(1, supervisor.Attempts);
    }

    [Fact]
    public void RepeatedFailures_BackOffExponentially()
    {
        var supervisor = new ShellRecoverySupervisor(FastOptions());
        supervisor.Evaluate(shellPresent: false, T0);

        // First attempt at the end of the grace period.
        Assert.Equal(RecoveryAction.Relaunch, supervisor.Evaluate(false, T0.AddSeconds(5)));

        // Second attempt must wait BaseBackoff (2s) after the first.
        Assert.Equal(RecoveryAction.Wait, supervisor.Evaluate(false, T0.AddSeconds(6)));
        Assert.Equal(RecoveryAction.Relaunch, supervisor.Evaluate(false, T0.AddSeconds(7)));

        // Third must wait 4s after the second — the interval doubled.
        Assert.Equal(RecoveryAction.Wait, supervisor.Evaluate(false, T0.AddSeconds(9)));
        Assert.Equal(RecoveryAction.Relaunch, supervisor.Evaluate(false, T0.AddSeconds(11)));

        Assert.Equal(3, supervisor.Attempts);
    }

    [Fact]
    public void ExhaustingAttempts_EntersSafeModeInsteadOfRetryingForever()
    {
        var supervisor = new ShellRecoverySupervisor(FastOptions());
        supervisor.Evaluate(shellPresent: false, T0);

        supervisor.Evaluate(false, T0.AddSeconds(5));    // attempt 1
        supervisor.Evaluate(false, T0.AddSeconds(7));    // attempt 2
        supervisor.Evaluate(false, T0.AddSeconds(11));   // attempt 3 (MaxAttempts)

        Assert.Equal(RecoveryAction.EnterSafeMode, supervisor.Evaluate(false, T0.AddSeconds(60)));
        Assert.True(supervisor.IsInSafeMode);
    }

    [Fact]
    public void SafeMode_IsSticky_AndStopsFurtherRelaunchAttempts()
    {
        var supervisor = new ShellRecoverySupervisor(FastOptions());
        supervisor.Evaluate(shellPresent: false, T0);
        supervisor.Evaluate(false, T0.AddSeconds(5));
        supervisor.Evaluate(false, T0.AddSeconds(7));
        supervisor.Evaluate(false, T0.AddSeconds(11));
        supervisor.Evaluate(false, T0.AddSeconds(60));

        int attemptsAtSafeMode = supervisor.Attempts;

        Assert.Equal(RecoveryAction.EnterSafeMode, supervisor.Evaluate(false, T0.AddSeconds(600)));
        Assert.Equal(attemptsAtSafeMode, supervisor.Attempts);
    }

    [Fact]
    public void AShellThatKeepsDyingShortlyAfterEachRestart_StillTripsTheBreaker()
    {
        // This is the crash/restart LOOP documented across competitors. If the attempt
        // counter reset the moment the shell reappeared, this scenario would relaunch
        // forever and never reach safe mode.
        var supervisor = new ShellRecoverySupervisor(FastOptions());
        var now = T0;
        int relaunches = 0;

        for (int cycle = 0; cycle < 10; cycle++)
        {
            // Shell is gone; run past the grace period and any backoff.
            for (int i = 0; i < 40; i++)
            {
                if (supervisor.Evaluate(shellPresent: false, now) == RecoveryAction.Relaunch)
                {
                    relaunches++;
                }

                now = now.AddSeconds(1);
            }

            // It comes back, but only briefly — well short of the stability window.
            supervisor.Evaluate(shellPresent: true, now);
            now = now.AddSeconds(3);
        }

        Assert.True(supervisor.IsInSafeMode, "A flapping shell must eventually trip the breaker.");
        Assert.Equal(FastOptions().MaxAttempts, relaunches);
    }

    [Fact]
    public void SustainedHealth_ResetsAttemptsAndLeavesSafeMode()
    {
        var supervisor = new ShellRecoverySupervisor(FastOptions());
        supervisor.Evaluate(shellPresent: false, T0);
        supervisor.Evaluate(false, T0.AddSeconds(5));
        supervisor.Evaluate(false, T0.AddSeconds(7));
        supervisor.Evaluate(false, T0.AddSeconds(11));
        supervisor.Evaluate(false, T0.AddSeconds(60));
        Assert.True(supervisor.IsInSafeMode);

        var recovered = T0.AddSeconds(120);
        supervisor.Evaluate(shellPresent: true, recovered);

        // Still inside the stability window: not yet forgiven.
        Assert.Equal(RecoveryAction.EnterSafeMode, supervisor.Evaluate(true, recovered.AddSeconds(30)));
        Assert.True(supervisor.IsInSafeMode);

        // Healthy for the full window: clean slate.
        Assert.Equal(RecoveryAction.None, supervisor.Evaluate(true, recovered.AddMinutes(1)));
        Assert.False(supervisor.IsInSafeMode);
        Assert.Equal(0, supervisor.Attempts);
    }

    [Fact]
    public void BackoffIsCapped()
    {
        var options = new ShellRecoveryOptions
        {
            GracePeriod = TimeSpan.Zero,
            BaseBackoff = TimeSpan.FromSeconds(1),
            MaxBackoff = TimeSpan.FromSeconds(5),
            MaxAttempts = 10,
            StabilityWindow = TimeSpan.FromMinutes(1)
        };
        var supervisor = new ShellRecoverySupervisor(options);
        var now = T0;

        supervisor.Evaluate(shellPresent: false, now);

        // Drive several attempts, always advancing past the capped backoff.
        for (int i = 0; i < 8; i++)
        {
            now = now.AddSeconds(6);
            supervisor.Evaluate(false, now);
        }

        // With an uncapped doubling backoff, 8 attempts would need intervals far beyond
        // 6s and the attempts would not all have been made.
        Assert.True(supervisor.Attempts >= 8, $"Expected the cap to allow steady retries, got {supervisor.Attempts}.");
    }
}
