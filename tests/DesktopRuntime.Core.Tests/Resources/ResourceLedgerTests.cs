using DesktopRuntime.Core.Resources;
using DesktopRuntime.Core.Widgets;

namespace DesktopRuntime.Core.Tests.Resources;

public class ResourceLedgerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);

    private static WidgetResourceBudget Budget(double cpu = 1.0, int memoryMb = 32) =>
        new() { IdleCpuPercent = cpu, MemoryMb = memoryMb, FramesPerSecond = 1 };

    private static ResourceAccountingOptions Options() => new()
    {
        BreachTolerance = TimeSpan.FromSeconds(30),
        ToleranceFactor = 1.25
    };

    [Fact]
    public void UnregisteredWidget_HasUnknownStatus()
    {
        var ledger = new ResourceLedger(Options());

        Assert.Equal(BudgetStatus.Unknown, ledger.GetStatus("com.example.clock"));
    }

    [Fact]
    public void RecordingForAnUnregisteredWidget_Throws()
    {
        // Otherwise a widget could escape accounting simply by never registering.
        var ledger = new ResourceLedger(Options());

        Assert.Throws<InvalidOperationException>(
            () => ledger.Record("com.example.ghost", new ResourceSample(50, 500, T0)));
    }

    [Fact]
    public void CompliantWidget_IsWithinBudget()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.clock", Budget());

        ledger.Record("com.example.clock", new ResourceSample(0.5, 20, T0));

        Assert.Equal(BudgetStatus.WithinBudget, ledger.GetStatus("com.example.clock"));
        Assert.Empty(ledger.GetSustainedBreaches());
    }

    [Fact]
    public void MeasurementNoiseWithinTheToleranceFactor_IsNotABreach()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.clock", Budget(cpu: 1.0));

        // 1.2% against a declared 1.0% is inside the 1.25x allowance.
        ledger.Record("com.example.clock", new ResourceSample(1.2, 20, T0));

        Assert.Equal(BudgetStatus.WithinBudget, ledger.GetStatus("com.example.clock"));
    }

    [Fact]
    public void BriefSpike_IsReportedAsSpiking_NotAsABreach()
    {
        // A widget waking to redraw, or a GC pause, is not misbehaviour. Acting on a
        // single sample would produce exactly the false positives that make a resource
        // governor annoying rather than useful.
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.clock", Budget());

        ledger.Record("com.example.clock", new ResourceSample(40, 20, T0));
        Assert.Equal(BudgetStatus.Spiking, ledger.GetStatus("com.example.clock"));

        ledger.Record("com.example.clock", new ResourceSample(40, 20, T0.AddSeconds(10)));
        Assert.Equal(BudgetStatus.Spiking, ledger.GetStatus("com.example.clock"));

        Assert.Empty(ledger.GetSustainedBreaches());
    }

    [Fact]
    public void SustainedOveruse_BecomesABreachOnceToleranceElapses()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.hog", Budget());

        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0));
        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0.AddSeconds(29)));
        Assert.Equal(BudgetStatus.Spiking, ledger.GetStatus("com.example.hog"));

        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0.AddSeconds(30)));
        Assert.Equal(BudgetStatus.SustainedBreach, ledger.GetStatus("com.example.hog"));

        var breach = Assert.Single(ledger.GetSustainedBreaches());
        Assert.Equal("com.example.hog", breach.WidgetId);
        Assert.Equal(T0, breach.BreachStartedAt);
        Assert.Equal(39.0, breach.CpuOvershoot);
    }

    [Fact]
    public void ReturningToBudget_ClearsTheBreach_AndRestartsTheClock()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.hog", Budget());

        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0));
        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0.AddSeconds(30)));
        Assert.Equal(BudgetStatus.SustainedBreach, ledger.GetStatus("com.example.hog"));

        ledger.Record("com.example.hog", new ResourceSample(0.5, 20, T0.AddSeconds(31)));
        Assert.Equal(BudgetStatus.WithinBudget, ledger.GetStatus("com.example.hog"));
        Assert.Empty(ledger.GetSustainedBreaches());

        // The tolerance window restarts rather than resuming where it left off.
        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0.AddSeconds(32)));
        Assert.Equal(BudgetStatus.Spiking, ledger.GetStatus("com.example.hog"));
    }

    [Fact]
    public void MemoryOveruseAlone_IsEnoughToBreach()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.leaky", Budget(cpu: 1.0, memoryMb: 32));

        ledger.Record("com.example.leaky", new ResourceSample(0.1, 400, T0));
        ledger.Record("com.example.leaky", new ResourceSample(0.1, 400, T0.AddSeconds(30)));

        var breach = Assert.Single(ledger.GetSustainedBreaches());
        Assert.Equal(368, breach.MemoryOvershootMb);
    }

    [Fact]
    public void Breaches_AreRankedWorstFirst()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.mild", Budget());
        ledger.Register("com.example.severe", Budget());

        foreach (int offset in new[] { 0, 30 })
        {
            ledger.Record("com.example.mild", new ResourceSample(10, 20, T0.AddSeconds(offset)));
            ledger.Record("com.example.severe", new ResourceSample(80, 20, T0.AddSeconds(offset)));
        }

        var breaches = ledger.GetSustainedBreaches();

        Assert.Equal(2, breaches.Count);
        Assert.Equal("com.example.severe", breaches[0].WidgetId);
    }

    [Fact]
    public void Totals_AggregateIndividuallyCompliantWidgets()
    {
        // Ten widgets each well inside their own budget can still add up to an
        // unacceptable total, so the host needs this as well as per-widget status.
        var ledger = new ResourceLedger(Options());

        for (int i = 0; i < 10; i++)
        {
            string id = $"com.example.widget{i}";
            ledger.Register(id, Budget());
            ledger.Record(id, new ResourceSample(0.9, 30, T0));
        }

        var totals = ledger.GetTotals();

        Assert.Equal(10, totals.WidgetsCounted);
        Assert.Equal(9.0, totals.CpuPercent, precision: 5);
        Assert.Equal(300, totals.MemoryMb);

        // ...while every individual widget is compliant.
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(BudgetStatus.WithinBudget, ledger.GetStatus($"com.example.widget{i}"));
        }
    }

    [Fact]
    public void Totals_IgnoreRegisteredWidgetsThatHaveNotReportedYet()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.reporting", Budget());
        ledger.Register("com.example.silent", Budget());
        ledger.Record("com.example.reporting", new ResourceSample(1.0, 10, T0));

        var totals = ledger.GetTotals();

        Assert.Equal(1, totals.WidgetsCounted);
        Assert.Equal(10, totals.MemoryMb);
    }

    [Fact]
    public void RemovingAWidget_StopsTrackingIt()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.hog", Budget());
        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0));
        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0.AddSeconds(30)));
        Assert.Single(ledger.GetSustainedBreaches());

        ledger.Remove("com.example.hog");

        Assert.Empty(ledger.GetSustainedBreaches());
        Assert.Empty(ledger.TrackedWidgets);
        Assert.Equal(BudgetStatus.Unknown, ledger.GetStatus("com.example.hog"));
    }

    [Fact]
    public void ReRegistering_ResetsHistory()
    {
        var ledger = new ResourceLedger(Options());
        ledger.Register("com.example.hog", Budget());
        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0));
        ledger.Record("com.example.hog", new ResourceSample(40, 20, T0.AddSeconds(30)));
        Assert.Equal(BudgetStatus.SustainedBreach, ledger.GetStatus("com.example.hog"));

        ledger.Register("com.example.hog", Budget());

        Assert.Equal(BudgetStatus.Unknown, ledger.GetStatus("com.example.hog"));
    }
}
