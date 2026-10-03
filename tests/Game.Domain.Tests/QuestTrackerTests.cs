using System;
using System.Linq;
using Game.Domain;
using Xunit;

public sealed class QuestTrackerTests
{
    [Fact]
    public void InitialSnapshotUsesCatalogOrderAndSortedStableIds()
    {
        var tracker = new HubQuestTracker(HubQuestConfig.Prototype);

        var view = tracker.View;

        Assert.Equal(new[] { "campaign.exploit_foundation", "campaign.new_class" }, view.CampaignQuests.Select(x => x.Id));
        Assert.Equal(new[] { "kpi.daily.soften_liquor", "kpi.daily.squeezed_medicine", "kpi.daily.stock_pump" }, view.DailyKpis.Select(x => x.Id));
        Assert.Equal(new[] { "kpi.weekly.gene_deadbeat", "kpi.weekly.risk_insurance" }, view.WeeklyKpis.Select(x => x.Id));
    }

    [Fact]
    public void SnapshotIsReadOnlyAndCampaignProgressNeverDecreases()
    {
        var tracker = new HubQuestTracker(HubQuestConfig.Prototype);
        var initial = tracker.View;

        tracker.Observe(new ZoneUnlocked(5, "zone_2"));
        var afterZone = tracker.View.CampaignQuests.Single(x => x.Id == "campaign.exploit_foundation");
        tracker.Observe(new FacilityUpgradeCompleted(8, "veterinary_hospital", 1, 2));
        var completed = tracker.View.CampaignQuests.Single(x => x.Id == "campaign.exploit_foundation");

        Assert.Equal(0, initial.CampaignQuests.Single(x => x.Id == "campaign.exploit_foundation").Progress);
        Assert.Equal(1, afterZone.Progress);
        Assert.True(completed.Progress >= afterZone.Progress);
        Assert.True(completed.IsComplete);
        Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<QuestObjectiveView>)initial.CampaignQuests).Clear());
    }

    [Fact]
    public void IgnoresUnrelatedFacts()
    {
        var tracker = new HubQuestTracker(HubQuestConfig.Prototype);
        var before = tracker.View;

        tracker.Observe(new TreasuryChanged(1, 100, 100, "test"));

        Assert.Equal(before.CampaignQuests, tracker.View.CampaignQuests);
        Assert.Equal(before.DailyKpis, tracker.View.DailyKpis);
        Assert.Equal(before.WeeklyKpis, tracker.View.WeeklyKpis);
    }
}
