using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Game.Domain.Gear;
using Game.Domain.Materials;
using Xunit;

public sealed class ProtectionCharmMarketTests
{
    [Fact]
    public void ProvisioningAndPurchaseConserveStockCashAndAdvanceKpiOnlyAfterSettlement()
    {
        var world = CreateWorld();

        Assert.True(world.ProvisionProtectionCharms(8).Ok);
        Assert.Equal(8, Stock(world));
        long gold = world.Trainers[0].Gold;
        Assert.True(world.PurchaseProduct(0, "protection_charm", 3).Ok);

        Assert.Equal(5, Stock(world));
        Assert.Equal(gold - 1500, world.Trainers[0].Gold);
        Assert.Equal(3, world.Quests.WeeklyKpis.Single(x => x.Id == "kpi.weekly.risk_insurance").Progress);
        Assert.Equal(3, world.Trainers[0].Products[new ProductId("protection_charm")]);
    }

    [Fact]
    public void ProvisioningHonorsPerCallLimitAndFailedPurchasePreservesState()
    {
        var world = CreateWorld(trainerGold: 1);

        Assert.False(world.ProvisionProtectionCharms(1001).Ok);
        Assert.False(world.PurchaseProduct(0, "protection_charm", 1).Ok);
        var purchase = world.PurchaseProduct(0, "protection_charm", 1);
        Assert.False(purchase.Ok);

        Assert.Equal(0, Stock(world));
        Assert.Equal(0, world.Quests.WeeklyKpis.Single(x => x.Id == "kpi.weekly.risk_insurance").Progress);
    }

    [Fact]
    public void TrainerBuysCharmOnlyForBreakRiskAndBelowExpectedAvoidedLoss()
    {
        var world = CreateWorld();
        Assert.True(world.ProvisionProtectionCharms(2).Ok);
        var slot = GearCatalog.Default.GetSlot("monster.weapon");
        var breakRiskGear = new GearItem("test+10", slot, 1, 100, enhanceLevel: 10);
        var safeGear = new GearItem("test+9", slot, 1, 100, enhanceLevel: 9);

        Assert.True(world.TrainerPrepareEnhancementProtection(0, safeGear).Ok);
        Assert.Equal(0, world.Trainers[0].Products.GetValueOrDefault(new ProductId("protection_charm")));
        Assert.True(world.SetProductPrice("protection_charm", 1000).Ok);
        Assert.True(world.TrainerPrepareEnhancementProtection(0, breakRiskGear).Ok);
        Assert.Equal(0, world.Trainers[0].Products.GetValueOrDefault(new ProductId("protection_charm")));
        Assert.True(world.SetProductPrice("protection_charm", 999).Ok);
        Assert.True(world.TrainerPrepareEnhancementProtection(0, breakRiskGear).Ok);
        Assert.Equal(1, world.Trainers[0].Products[new ProductId("protection_charm")]);
        Assert.Equal(1, world.Quests.WeeklyKpis.Single(x => x.Id == "kpi.weekly.risk_insurance").Progress);
    }

    [Fact]
    public void TrainerEnhancementActionRunsCharmDecisionBeforeEnhancing()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            StartTreasury = 100_000,
            StartTrainerGold = 100_000,
            QuestSettings = new HubQuestConfig(maximumProtectionCharmFulfillment: 10)
        }.WithServiceFacilities(), 9001);
        var item = new GearItem("ai+10", GearCatalog.Default.GetSlot("trainer.gloves"), 2, 100, enhanceLevel: 10);
        Assert.True(world.OfferGear(0, item, 1).Ok);
        Assert.True(world.ProvisionProtectionCharms(1).Ok);
        world.GrantProductForTest(0, "enhancement_stone", 1);

        Assert.True(world.TrainerEnhanceGear(0, item).Ok);

        Assert.Equal(0, world.SupplyStocks.Where(x => x.ItemId == "product:protection_charm").Select(x => x.Available).DefaultIfEmpty(0).Single());
        Assert.Equal(1, world.Quests.WeeklyKpis.Single(x => x.Id == "kpi.weekly.risk_insurance").Progress);
        Assert.InRange(item.EnhanceLevel, 10, 11); // attempt may fail; the action must have purchased before resolving it
    }

    [Fact]
    public void PriceChangesApplyToProtectionCharmPurchase()
    {
        var world = CreateWorld();
        Assert.True(world.ProvisionProtectionCharms(2).Ok);
        Assert.True(world.SetProductPrice("protection_charm", 750).Ok);
        long before = world.Trainers[0].Gold;

        Assert.True(world.PurchaseProduct(0, "protection_charm", 2).Ok);

        Assert.Equal(before - 1500, world.Trainers[0].Gold);
    }

    static HubWorld CreateWorld(long trainerGold = 10_000, long startTreasury = 1000)
        => new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            StartTreasury = startTreasury,
            StartTrainerGold = trainerGold,
            QuestSettings = new HubQuestConfig(maximumProtectionCharmFulfillment: 10)
        }.WithServiceFacilities(), 9001);

    static int Stock(HubWorld world)
        => world.SupplyStocks.Where(x => x.ItemId == "product:protection_charm")
            .Select(x => x.Available).DefaultIfEmpty(0).Single();
}
