using System.Linq;
using Game.Domain;
using Xunit;

public sealed class QuestProgressTests
{
    [Fact]
    public void SettledProductFactsAdvanceOnlyMatchingDailyAndWeeklyObjectives()
    {
        var tracker = new HubQuestTracker(HubQuestConfig.Prototype);

        tracker.Observe(new ProductPurchased(10, 0, "potion", 1, 30_000, 30_000));
        tracker.Observe(new ProductPurchased(11, 0, "vaccine", 1, 20_000, 20_000));
        tracker.Observe(new ProductPurchased(12, 0, "liquor", 7, 15, 105));
        tracker.Observe(new ProductPurchased(13, 0, "protection_charm", 4, 500, 2_000));
        tracker.Observe(new ProductPurchased(14, 0, "wood_ingot", 99, 5, 495));

        Assert.Equal(50_000, Find(tracker.View.DailyKpis, "kpi.daily.squeezed_medicine").Progress);
        Assert.Equal(7, Find(tracker.View.DailyKpis, "kpi.daily.soften_liquor").Progress);
        Assert.Equal(4, Find(tracker.View.WeeklyKpis, "kpi.weekly.risk_insurance").Progress);
        Assert.Equal(0, Find(tracker.View.DailyKpis, "kpi.daily.stock_pump").Progress);
    }

    [Fact]
    public void OnlyDirectorStockOperationFactCountsAndConfiscationRequiresConfiscationFact()
    {
        var tracker = new HubQuestTracker(HubQuestConfig.Prototype);

        tracker.Observe(new StockTradeSettled(5, "Inn", 0, 1, 1, 100, 1, 0, 0, 101, 99, "TrainerTrade"));
        tracker.Observe(new DirectorStockOperationSettled(6, "IpoPurchase"));
        tracker.Observe(new GeneBankFeeSettled(7, 0, 100, 100, 0, ""));
        tracker.Observe(new MonsterConfiscated(8, 0, "confiscated_1"));

        Assert.Equal(1, Find(tracker.View.DailyKpis, "kpi.daily.stock_pump").Progress);
        Assert.True(Find(tracker.View.DailyKpis, "kpi.daily.stock_pump").IsComplete);
        Assert.Equal(1, Find(tracker.View.WeeklyKpis, "kpi.weekly.gene_deadbeat").Progress);
    }

    [Fact]
    public void ExactBoundaryFactBelongsToNewPeriodAndCompletedKpiCannotPayTwice()
    {
        var tracker = new HubQuestTracker(new HubQuestConfig(dailyLiquorUnitTarget: 2));
        int completions = 0;
        tracker.EventProduced += e => { if (e is QuestCompleted c && c.ObjectiveId == "kpi.daily.soften_liquor") completions++; };

        tracker.Observe(new ProductPurchased(10, 0, "liquor", 2, 1, 2));
        tracker.Observe(new ProductPurchased(SimClock.MinutesPerDay, 0, "liquor", 2, 1, 2));
        tracker.Observe(new ProductPurchased(SimClock.MinutesPerDay + 1, 0, "liquor", 2, 1, 2));

        var current = Find(tracker.View.DailyKpis, "kpi.daily.soften_liquor");
        Assert.Equal(1, current.PeriodIndex);
        Assert.Equal(2, current.Progress);
        Assert.Equal(2, completions);
    }

    [Fact]
    public void WeeklyPeriodRollsAtSevenDayBoundaryWithoutInterveningTransactions()
    {
        var tracker = new HubQuestTracker(HubQuestConfig.Prototype);
        tracker.AdvanceTo(7 * SimClock.MinutesPerDay);

        var weekly = Find(tracker.View.WeeklyKpis, "kpi.weekly.gene_deadbeat");

        Assert.Equal(1, weekly.PeriodIndex);
        Assert.Equal(0, weekly.Progress);
    }

    static QuestObjectiveView Find(System.Collections.Generic.IReadOnlyList<QuestObjectiveView> list, string id)
        => list.Single(x => x.Id == id);
}
