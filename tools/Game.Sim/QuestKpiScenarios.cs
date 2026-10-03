using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Domain;
using Game.Domain.Materials;
using Game.Domain.Monsters;

public static class QuestKpiScenarios
{
    public static void Run(TextWriter output)
    {
        if (output == null) throw new ArgumentNullException(nameof(output));
        Campaign(output);
        Kpis(output);
    }

    static void Campaign(TextWriter output)
    {
        int trainers = 7;
        var config = new SimConfig
        {
            TrainerCount = trainers,
            StartingTrainerRanks = Enumerable.Repeat(2, trainers).ToArray(),
            StartTownHallLevel = 11,
            StartDormitoryLevel = 2,
            StartTreasury = 100_000,
            UnlockedZoneIds = new[] { "zone_1" },
            StartingConstructionStock = ConstructionStock(2),
            QuestSettings = new HubQuestConfig(campaignRewardGold: 50,
                campaignRewardGems: 3, campaignRewardBuildingPermits: 2,
                campaignRewardHighTierInvitations: 4, campaignRewardProtectionCharms: 5)
        };
        SetServiceFacilities(config, 1);
        var world = new HubWorld(config, 20261003);
        Require(world.UnlockZone("zone_2").Ok, "Campaign Zone 2 unlock");
        Require(!Find(world.Quests.CampaignQuests, "campaign.exploit_foundation").IsComplete,
            "Campaign remains incomplete before Hospital level 2");
        Require(world.UpgradeFacility("veterinary_hospital").Ok, "Campaign Hospital upgrade");
        world.RunFor(config.HubProgressionSettings.FacilityUpgradeMinutes);
        var quest = Find(world.Quests.CampaignQuests, "campaign.exploit_foundation");
        Require(quest.IsComplete && quest.IsRewardGranted, "Campaign completion and reward");
        world.ValidateInvariants();
        output.WriteLine($"campaign id={quest.Id} progress={quest.Progress}/{quest.Target} complete={quest.IsComplete} wallet={world.Quests.RewardWallet}");
    }

    static void Kpis(TextWriter output)
    {
        var config = new SimConfig
        {
            TrainerCount = 1,
            StartTownHallLevel = 11,
            StartDormitoryLevel = 3,
            StartTreasury = 100_000,
            StartTrainerGold = 100_000,
            UnlockedZoneIds = new[] { "zone_1", "zone_2", "zone_3" },
            StartingProductStock = new Dictionary<ProductId, int>
            {
                [new ProductId("potion")] = 2,
                [new ProductId("liquor")] = 2
            },
            QuestSettings = new HubQuestConfig(dailyMedicineGoldTarget: 10,
                dailyStockOperationTarget: 1, dailyLiquorUnitTarget: 1,
                weeklyGeneConfiscationTarget: 1, weeklyProtectionCharmSaleTarget: 1)
        };
        SetServiceFacilities(config, 1);
        config.StartingFacilityLevels["gene_bank"] = 1;
        var salesWorld = new HubWorld(config, 20261004);
        Require(salesWorld.ProvisionProtectionCharms(2).Ok, "Protection Charm fulfillment");
        Require(salesWorld.PurchaseProduct(0, "potion", 1).Ok, "Medicine revenue KPI fact");
        Require(salesWorld.PurchaseProduct(0, "liquor", 1).Ok, "Liquor units KPI fact");
        Require(salesWorld.PurchaseProduct(0, "protection_charm", 1).Ok, "Protection Charm KPI fact");
        var starterId = new MonsterId(salesWorld.Trainers[0].Monsters[0].Id);
        Require(salesWorld.StoreMonsterInGeneBank(0, starterId), "Gene Bank voluntary storage setup");
        Require(salesWorld.ConfiscateForUnpaidGeneBankFee(0) != null, "Unpaid-fee confiscation KPI fact");
        var medicine = Find(salesWorld.Quests.DailyKpis, "kpi.daily.squeezed_medicine");
        var liquor = Find(salesWorld.Quests.DailyKpis, "kpi.daily.soften_liquor");
        var confiscation = Find(salesWorld.Quests.WeeklyKpis, "kpi.weekly.gene_deadbeat");
        var charm = Find(salesWorld.Quests.WeeklyKpis, "kpi.weekly.risk_insurance");
        Require(medicine.IsComplete && liquor.IsComplete && confiscation.IsComplete && charm.IsComplete,
            "Medicine, Liquor, confiscation and Protection Charm KPIs complete");
        salesWorld.ValidateInvariants();

        var stockConfig = new SimConfig
        {
            TrainerCount = 1, StartBuildingLevel = 5, StartTownHallLevel = 11, StartDormitoryLevel = 3,
            StartTreasury = 1_000_000, StartTrainerGold = 100_000,
            UnlockedZoneIds = new[] { "zone_1", "zone_2", "zone_3" },
            QuestSettings = new HubQuestConfig(dailyStockOperationTarget: 1)
        };
        SetServiceFacilities(stockConfig, 5);
        stockConfig.StartingFacilityLevels["stock_exchange"] = 1;
        var stockWorld = new HubWorld(stockConfig, 20261005);
        stockWorld.RunFor(15 * SimClock.MinutesPerDay + 1);
        Require(stockWorld.IpoBuildingStock(BuildingKind.Restaurant).Ok, "KPI stock operation IPO setup");
        var company = stockWorld.StockCompanies.Single(x => x.CompanyId == BuildingKind.Restaurant.ToString());
        Require(stockWorld.BuyIpoShares(0, company.CompanyId, 1).Ok, "Director stock operation KPI fact");
        var stock = Find(stockWorld.Quests.DailyKpis, "kpi.daily.stock_pump");
        Require(stock.IsComplete, "Stock operation KPI complete");
        stockWorld.ValidateInvariants();
        output.WriteLine($"kpi medicine={medicine.Progress}/{medicine.Target}@day{medicine.PeriodIndex} stock={stock.Progress}/{stock.Target}@day{stock.PeriodIndex} liquor={liquor.Progress}/{liquor.Target}@day{liquor.PeriodIndex} gene={confiscation.Progress}/{confiscation.Target}@week{confiscation.PeriodIndex} charm={charm.Progress}/{charm.Target}@week{charm.PeriodIndex}");
    }

    static Dictionary<ProductId, int> ConstructionStock(int units)
        => new Dictionary<ProductId, int>
        {
            [new ProductId("wood_ingot")] = units,
            [new ProductId("stone_ingot")] = units,
            [new ProductId("iron_ingot")] = units
        };

    static void SetServiceFacilities(SimConfig config, int level)
    {
        config.StartingFacilityLevels = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["inn"] = level,
            ["restaurant"] = level,
            ["bar"] = level,
            ["veterinary_hospital"] = level
        };
    }

    static QuestObjectiveView Find(IReadOnlyList<QuestObjectiveView> list, string id)
        => list.Single(x => x.Id == id);

    static void Require(bool condition, string step)
    {
        if (!condition) throw new InvalidOperationException("Quest/KPI scenario failed: " + step);
    }
}
