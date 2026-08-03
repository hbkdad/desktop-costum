using DesktopRuntime.Core.Widgets;

namespace DesktopRuntime.Core.Resources;

/// <summary>One observation of a running widget's resource use.</summary>
public readonly record struct ResourceSample(double CpuPercent, int MemoryMb, DateTimeOffset TakenAt);

/// <summary>How a widget currently stands against the budget it declared.</summary>
public enum BudgetStatus
{
    /// <summary>Not enough observations yet to judge.</summary>
    Unknown,

    WithinBudget,

    /// <summary>Over budget right now, but not for long enough to act on.</summary>
    Spiking,

    /// <summary>Over budget continuously for longer than the tolerance window.</summary>
    SustainedBreach
}

public sealed class ResourceAccountingOptions
{
    /// <summary>
    /// How long a widget must be continuously over its declared budget before the breach
    /// is treated as real. A momentary spike — a widget waking to redraw, a GC pause — is
    /// not misbehaviour, and acting on one would produce exactly the false positives that
    /// make a resource governor annoying rather than useful.
    /// </summary>
    public TimeSpan BreachTolerance { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Headroom over the declared figure before a sample counts as over budget, allowing
    /// for measurement noise.
    /// </summary>
    public double ToleranceFactor { get; init; } = 1.25;

    /// <summary>Samples retained per widget for reporting.</summary>
    public int MaxSamplesPerWidget { get; init; } = 120;
}

/// <summary>
/// Tracks measured resource use against the budget each widget declared in its manifest.
/// <para>
/// The widget manifest's budget is only an author's <em>claim</em>
/// (see docs/architecture/widget-manifest.md). This is what turns that claim into
/// something checkable, which is the difference between the resource-discipline
/// differentiator in docs/research/market-gap-report.md and a marketing line.
/// </para>
/// <para>
/// Pure accounting: no I/O, no clock read, no enforcement. Callers supply samples with
/// timestamps and decide what to do with a verdict. That keeps it deterministically
/// testable and leaves policy (throttle, pause, warn) to the host.
/// </para>
/// </summary>
public sealed class ResourceLedger(ResourceAccountingOptions? options = null)
{
    private readonly ResourceAccountingOptions _options = options ?? new ResourceAccountingOptions();
    private readonly Dictionary<string, WidgetAccount> _accounts = new(StringComparer.Ordinal);

    /// <summary>Registers a widget and the budget it declared. Re-registering resets its history.</summary>
    public void Register(string widgetId, WidgetResourceBudget declaredBudget)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(widgetId);
        ArgumentNullException.ThrowIfNull(declaredBudget);

        _accounts[widgetId] = new WidgetAccount(declaredBudget);
    }

    public void Remove(string widgetId) => _accounts.Remove(widgetId);

    public IReadOnlyCollection<string> TrackedWidgets => _accounts.Keys;

    /// <summary>
    /// Records a sample for a widget.
    /// </summary>
    /// <exception cref="InvalidOperationException">The widget was never registered.</exception>
    public void Record(string widgetId, ResourceSample sample)
    {
        if (!_accounts.TryGetValue(widgetId, out var account))
        {
            // Silently accepting samples for unknown widgets would let a widget escape
            // accounting simply by never registering.
            throw new InvalidOperationException(
                $"Widget '{widgetId}' is not registered with the ledger; its budget is unknown.");
        }

        account.Record(sample, _options);
    }

    public BudgetStatus GetStatus(string widgetId) =>
        _accounts.TryGetValue(widgetId, out var account)
            ? account.Status
            : BudgetStatus.Unknown;

    /// <summary>Widgets currently in sustained breach, worst CPU overshoot first.</summary>
    public IReadOnlyList<BudgetBreach> GetSustainedBreaches() =>
        [.. _accounts
            .Where(pair => pair.Value.Status == BudgetStatus.SustainedBreach)
            .Select(pair => new BudgetBreach(
                pair.Key,
                pair.Value.DeclaredBudget,
                pair.Value.LatestSample!.Value,
                pair.Value.BreachStartedAt!.Value))
            .OrderByDescending(breach => breach.CpuOvershoot)];

    /// <summary>
    /// Total measured use across all tracked widgets, from each one's most recent sample.
    /// Individually-compliant widgets can still add up to an unacceptable total, so the
    /// host needs this as well as per-widget status.
    /// </summary>
    public ResourceTotals GetTotals()
    {
        double cpu = 0;
        int memory = 0;
        int counted = 0;

        foreach (var account in _accounts.Values)
        {
            if (account.LatestSample is { } sample)
            {
                cpu += sample.CpuPercent;
                memory += sample.MemoryMb;
                counted++;
            }
        }

        return new ResourceTotals(cpu, memory, counted);
    }

    private sealed class WidgetAccount(WidgetResourceBudget declaredBudget)
    {
        private readonly Queue<ResourceSample> _samples = new();

        public WidgetResourceBudget DeclaredBudget { get; } = declaredBudget;

        public ResourceSample? LatestSample { get; private set; }

        public DateTimeOffset? BreachStartedAt { get; private set; }

        public BudgetStatus Status { get; private set; } = BudgetStatus.Unknown;

        public void Record(ResourceSample sample, ResourceAccountingOptions options)
        {
            _samples.Enqueue(sample);
            while (_samples.Count > options.MaxSamplesPerWidget)
            {
                _samples.Dequeue();
            }

            LatestSample = sample;

            bool overBudget =
                sample.CpuPercent > DeclaredBudget.IdleCpuPercent * options.ToleranceFactor ||
                sample.MemoryMb > DeclaredBudget.MemoryMb * options.ToleranceFactor;

            if (!overBudget)
            {
                // Any compliant sample ends the breach: the widget is behaving again.
                BreachStartedAt = null;
                Status = BudgetStatus.WithinBudget;
                return;
            }

            BreachStartedAt ??= sample.TakenAt;

            Status = sample.TakenAt - BreachStartedAt.Value >= options.BreachTolerance
                ? BudgetStatus.SustainedBreach
                : BudgetStatus.Spiking;
        }
    }
}

/// <summary>A widget that has been over its declared budget for longer than the tolerance window.</summary>
public sealed class BudgetBreach(
    string widgetId,
    WidgetResourceBudget declaredBudget,
    ResourceSample latestSample,
    DateTimeOffset breachStartedAt)
{
    public string WidgetId { get; } = widgetId;

    public WidgetResourceBudget DeclaredBudget { get; } = declaredBudget;

    public ResourceSample LatestSample { get; } = latestSample;

    public DateTimeOffset BreachStartedAt { get; } = breachStartedAt;

    /// <summary>Measured CPU minus declared CPU. Used to rank the worst offenders first.</summary>
    public double CpuOvershoot => LatestSample.CpuPercent - DeclaredBudget.IdleCpuPercent;

    public int MemoryOvershootMb => LatestSample.MemoryMb - DeclaredBudget.MemoryMb;
}

/// <param name="WidgetsCounted">How many tracked widgets had a sample to contribute.</param>
public readonly record struct ResourceTotals(double CpuPercent, int MemoryMb, int WidgetsCounted);
