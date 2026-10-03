using System;
using System.Linq;
using Game.Domain;
using Xunit;

public sealed class QuestCatalogTests
{
    [Fact]
    public void CatalogContainsEarlyAccessCampaignAndAllDailyWeeklyKpisInStableOrder()
    {
        var catalog = HubQuestCatalog.Default;

        Assert.Equal(new[] { "campaign.exploit_foundation", "campaign.new_class" },
            catalog.CampaignQuests.Select(x => x.Id));
        Assert.Equal(new[] { "kpi.daily.squeezed_medicine", "kpi.daily.stock_pump", "kpi.daily.soften_liquor" },
            catalog.DailyKpis.Select(x => x.Id));
        Assert.Equal(new[] { "kpi.weekly.gene_deadbeat", "kpi.weekly.risk_insurance" },
            catalog.WeeklyKpis.Select(x => x.Id));
        Assert.False(catalog.CampaignQuests.Single(x => x.Id == "campaign.new_class").IsAvailable);
    }

    [Fact]
    public void PrototypeTargetsMatchGddExamplesAndExposeCompleteBalanceMetadata()
    {
        var config = HubQuestConfig.Prototype;

        Assert.Equal(50_000, config.DailyMedicineGoldTarget);
        Assert.Equal(1, config.DailyStockOperationTarget);
        Assert.Equal(20, config.DailyLiquorUnitTarget);
        Assert.Equal(5, config.WeeklyGeneConfiscationTarget);
        Assert.Equal(10, config.WeeklyProtectionCharmSaleTarget);
        Assert.Equal(1, config.DailyPeriodDays);
        Assert.Equal(7, config.WeeklyPeriodDays);

        var parameters = config.BalanceParameters;
        Assert.Equal(parameters.Count, parameters.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(parameters, x =>
        {
            Assert.False(string.IsNullOrWhiteSpace(x.Id));
            Assert.False(string.IsNullOrWhiteSpace(x.Unit));
            Assert.False(string.IsNullOrWhiteSpace(x.Status));
            Assert.Contains(x.Status, new[] { "Locked", "Prototype", "TBD" });
            Assert.False(string.IsNullOrWhiteSpace(x.Source));
        });
        Assert.Contains(parameters, x => x.Id == "quest.daily.medicine_gold_target" && x.Status == "Prototype");
        Assert.Contains(parameters, x => x.Id == "quest.campaign.exploit_foundation.zone_number" && x.Value == 2 && x.Status == "Locked");
        Assert.Contains(parameters, x => x.Id == "quest.campaign.exploit_foundation.hospital_level" && x.Value == 2 && x.Status == "Locked");
        Assert.Contains(parameters, x => x.Id == "quest.weekly.protection_charm_units" && x.Status == "Prototype");
        Assert.Contains(parameters, x => x.Id == "quest.weekly.period_days" && x.Value == 7);
        Assert.Contains(parameters, x => x.Id == "quest.protection_charm.expected_avoided_loss" && x.Status == "TBD");
    }

    [Fact]
    public void QuestConfigRejectsInvalidTargetsAndPeriodLengths()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HubQuestConfig(dailyMedicineGoldTarget: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HubQuestConfig(weeklyProtectionCharmSaleTarget: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HubQuestConfig(dailyPeriodDays: 0));
    }
}
