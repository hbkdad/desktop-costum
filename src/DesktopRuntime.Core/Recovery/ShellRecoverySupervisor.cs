namespace DesktopRuntime.Core.Recovery;

/// <summary>What the desktop host should do about the shell right now.</summary>
public enum RecoveryAction
{
    /// <summary>The shell is present and healthy. Nothing to do.</summary>
    None,

    /// <summary>
    /// The shell is missing, but do not act yet — either the grace period for the OS's
    /// own recovery has not elapsed, or a backoff interval is still running.
    /// </summary>
    Wait,

    /// <summary>Relaunch the shell now.</summary>
    Relaunch,

    /// <summary>
    /// Stop trying. Repeated failures indicate relaunching is not working, and continuing
    /// would make things worse rather than better.
    /// </summary>
    EnterSafeMode
}

public sealed class ShellRecoveryOptions
{
    /// <summary>
    /// How long to wait after noticing the shell is gone before relaunching it ourselves.
    /// <para>
    /// Phase 3 Prototype 3 found that Windows 11 restarts explorer.exe on its own, and
    /// that an immediate manual relaunch races it — producing a duplicate process and a
    /// stray window. Measured OS recovery there was ~2.7s, so the default leaves room.
    /// </para>
    /// </summary>
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Backoff before the second and subsequent relaunch attempts. Doubles each time.</summary>
    public TimeSpan BaseBackoff { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Relaunch attempts allowed before giving up into safe mode.</summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>
    /// How long the shell must stay continuously healthy before the attempt counter
    /// resets. Deliberately much longer than the backoff: see the class remarks.
    /// </summary>
    public TimeSpan StabilityWindow { get; init; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Decides how to respond to the shell (explorer.exe) disappearing.
/// <para>
/// The policy is <em>detect, then wait, then relaunch</em> — never relaunch immediately.
/// Prototype 3 showed Windows recovers on its own and that racing it causes duplicate
/// processes and stray windows.
/// </para>
/// <para>
/// The attempt counter resets only after the shell has been healthy for
/// <see cref="ShellRecoveryOptions.StabilityWindow"/>, not the moment it reappears.
/// That distinction is the whole point: a shell that dies again seconds after each
/// restart is exactly the crash/restart loop documented across competitors in
/// docs/research/competitor-matrix.md, and resetting on mere reappearance would let
/// such a loop run forever without ever tripping the breaker.
/// </para>
/// <para>
/// This type is pure policy — it performs no I/O and reads no clock. The caller supplies
/// the observation and the timestamp, which keeps it deterministically testable.
/// </para>
/// </summary>
public sealed class ShellRecoverySupervisor(ShellRecoveryOptions? options = null)
{
    private readonly ShellRecoveryOptions _options = options ?? new ShellRecoveryOptions();

    private DateTimeOffset? _shellLostAt;
    private DateTimeOffset? _lastAttemptAt;
    private DateTimeOffset? _healthySince;

    /// <summary>Relaunch attempts since the last reset.</summary>
    public int Attempts { get; private set; }

    /// <summary>True once the supervisor has given up relaunching.</summary>
    public bool IsInSafeMode { get; private set; }

    /// <summary>
    /// Decides what to do given the latest observation of the shell.
    /// </summary>
    /// <param name="shellPresent">Whether the shell was observed to be present.</param>
    /// <param name="now">The time of the observation.</param>
    public RecoveryAction Evaluate(bool shellPresent, DateTimeOffset now)
    {
        return shellPresent ? EvaluateHealthy(now) : EvaluateMissing(now);
    }

    private RecoveryAction EvaluateHealthy(DateTimeOffset now)
    {
        _shellLostAt = null;
        _healthySince ??= now;

        if (now - _healthySince.Value >= _options.StabilityWindow)
        {
            // Sustained health, not mere reappearance, is what earns a clean slate.
            Attempts = 0;
            _lastAttemptAt = null;
            IsInSafeMode = false;
        }

        return IsInSafeMode ? RecoveryAction.EnterSafeMode : RecoveryAction.None;
    }

    private RecoveryAction EvaluateMissing(DateTimeOffset now)
    {
        _healthySince = null;

        if (IsInSafeMode)
        {
            return RecoveryAction.EnterSafeMode;
        }

        if (_shellLostAt is null)
        {
            _shellLostAt = now;
            return RecoveryAction.Wait;
        }

        // Give the OS its own chance to recover first.
        if (now - _shellLostAt.Value < _options.GracePeriod)
        {
            return RecoveryAction.Wait;
        }

        if (Attempts >= _options.MaxAttempts)
        {
            IsInSafeMode = true;
            return RecoveryAction.EnterSafeMode;
        }

        if (_lastAttemptAt is not null && now - _lastAttemptAt.Value < CurrentBackoff())
        {
            return RecoveryAction.Wait;
        }

        Attempts++;
        _lastAttemptAt = now;
        return RecoveryAction.Relaunch;
    }

    private TimeSpan CurrentBackoff()
    {
        if (Attempts <= 0)
        {
            return TimeSpan.Zero;
        }

        // Doubling, but computed in ticks with a cap so a large attempt count cannot
        // overflow the shift.
        double multiplier = Math.Pow(2, Math.Min(Attempts - 1, 16));
        double ticks = _options.BaseBackoff.Ticks * multiplier;

        return ticks >= _options.MaxBackoff.Ticks
            ? _options.MaxBackoff
            : TimeSpan.FromTicks((long)ticks);
    }
}
