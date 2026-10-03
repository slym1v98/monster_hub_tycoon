using Game.Domain;
using Xunit;
using Game.Domain.Materials;
using System.Collections.Generic;

public sealed class BuildingOperationsTests
{
    [Fact]
    public void PoweredOffBuildingSkipsDailyUpkeepAndHasNoCapacity()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 0, StartTreasury = 1000 }.WithServiceFacilities(), 13);
        Assert.True(world.SetBuildingPower(BuildingKind.Inn, false).Ok);

        world.RunFor(SimClock.MinutesPerDay - SimClock.DawnMinute);

        Assert.Equal(800, world.Treasury);
        Assert.Equal(0, world.Buildings[(int)BuildingKind.Inn].Slots);
        Assert.False(world.Buildings[(int)BuildingKind.Inn].PoweredOn);
    }

    [Fact]
    public void GenericFacilityCanBeBuiltCompletedPoweredOffAndUpgraded()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 0, StartTreasury = 10000, StartTownHallLevel = 4,
            UpkeepPerBuildingPerDay = 0,
            StartingConstructionStock = new Dictionary<ProductId, int>
            {
                [new ProductId("wood_ingot")] = 20, [new ProductId("stone_ingot")] = 20,
                [new ProductId("iron_ingot")] = 20
            }
        }, 15);
        Assert.Equal("Available", System.Linq.Enumerable.Single(world.Facilities, x => x.Id == "tool_workshop").State);
        Assert.True(world.ConstructFacility("tool_workshop").Ok);
        Assert.Equal("ConstructionInProgress", System.Linq.Enumerable.Single(world.Facilities, x => x.Id == "tool_workshop").State);
        world.RunFor(2880);
        var built = System.Linq.Enumerable.Single(world.Facilities, x => x.Id == "tool_workshop");
        Assert.Equal(1, built.Level);
        Assert.Equal("Operational", built.State);
        Assert.True(world.SetFacilityPower("tool_workshop", false).Ok);
        Assert.Equal("PoweredOff", System.Linq.Enumerable.Single(world.Facilities, x => x.Id == "tool_workshop").State);
        Assert.True(world.UpgradeFacility("tool_workshop").Ok);
        world.RunFor(2880);
        Assert.Equal(2, System.Linq.Enumerable.Single(world.Facilities, x => x.Id == "tool_workshop").Level);
    }

    [Fact]
    public void LockedAndUnbuiltFacilitiesCannotBeOperated()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 0, StartTownHallLevel = 3 }, 16);
        Assert.Equal("Locked", System.Linq.Enumerable.Single(world.Facilities, x => x.Id == "bar").State);
        Assert.Equal("facility.locked", world.ConstructFacility("bar").Reason);
        Assert.Equal("facility.unavailable", world.SetProductionTarget("blank_ore_tier_1", 1).Reason);
        Assert.Equal("facility.not_built", world.SetFacilityPower("tool_workshop", false).Reason);
        Assert.Equal("facility.locked", world.SetFacilityPower("stock_exchange", false).Reason);
        Assert.False(world.StoreMonsterInGeneBank(0, new Game.Domain.Monsters.MonsterId("trainer_0_starter")));
        Assert.Equal("facility.unavailable", world.SetStockExchangeLevel(2).Reason);
    }

    [Fact]
    public void ConsumableSalesRequireAnOperatingShop()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1,
            StartingFacilityLevels = new Dictionary<string, int> { ["veterinary_hospital"] = 0 } }, 17);
        Assert.Equal("facility.unavailable", world.PurchaseProduct(0, "potion", 1).Reason);

        var operating = new HubWorld(new SimConfig { TrainerCount = 1 }.WithServiceFacilities(), 18);
        Assert.True(operating.SetBuildingPower(BuildingKind.Hospital, false).Ok);
        Assert.Equal("facility.unavailable", operating.PurchaseProduct(0, "vaccine", 1).Reason);
    }

    [Fact]
    public void MaintenanceDeficitReducesServiceQualityAlongWithCapacity()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 0, StartTreasury = 0,
            StartBuildingLevel = 5 }.WithServiceFacilities(), 14);

        world.RunFor(SimClock.MinutesPerDay - SimClock.DawnMinute);

        var inn = world.Buildings[(int)BuildingKind.Inn];
        Assert.False(inn.Maintained);
        Assert.Equal(0.5, inn.QualityMultiplier);
        Assert.Equal(4, inn.Slots);
        Assert.Equal(9, inn.FullSlots);
    }
}
