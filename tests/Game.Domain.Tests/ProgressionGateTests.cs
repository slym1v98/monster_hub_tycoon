using System.Linq;
using System.Collections.Generic;
using Game.Domain;
using Game.Domain.Materials;
using Xunit;

public sealed class ProgressionGateTests
{
    [Fact]
    public void FacilityUnlockCatalogFollowsTownHallMilestones()
    {
        var levelOne = new HubWorld(new SimConfig { TrainerCount = 0, StartTownHallLevel = 1 }, 124);
        Assert.True(levelOne.Facilities.Single(x => x.Id == "veterinary_hospital").IsUnlocked);
        Assert.False(levelOne.Facilities.Single(x => x.Id == "inn").IsUnlocked);
        Assert.Equal(1, levelOne.Facilities.Single(x => x.Id == "veterinary_hospital").MaxLevelAllowed);

        var levelSix = new HubWorld(new SimConfig { TrainerCount = 0, StartTownHallLevel = 6,
            UnlockedZoneIds = new[] { "zone_1", "zone_2" } }, 125);
        Assert.True(levelSix.Facilities.Single(x => x.Id == "reactor").IsUnlocked);
        Assert.Equal(2, levelSix.Facilities.Single(x => x.Id == "reactor").MaxLevelAllowed);
        Assert.False(levelSix.Facilities.Single(x => x.Id == "jeweler").IsUnlocked);
    }

    [Fact]
    public void ZoneTwoCannotUnlockUntilDormitoryReachesLevelTwo()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 10,
            StartingTrainerRanks = Enumerable.Repeat(2, 10).ToArray(),
        }, 7);

        var result = world.UnlockZone("zone_2");

        Assert.False(result.Ok);
        Assert.Equal("zone.dormitory_level_required", result.Reason);
        Assert.DoesNotContain(world.Zones, zone => zone.Id == "zone_2" && zone.IsUnlocked);
    }

    [Fact]
    public void ZoneTwoRequiresSevenQualifiedTrainersAtTheCurrentCapacityOfTen()
    {
        var ranks = Enumerable.Repeat(1, 10).ToArray();
        for (int i = 0; i < 6; i++) ranks[i] = 2;
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 10,
            StartingTrainerRanks = ranks,
            StartDormitoryLevel = 2,
        }, 8);

        var result = world.UnlockZone("zone_2");

        Assert.False(result.Ok);
        Assert.Equal("zone.rank_count_required", result.Reason);
        Assert.Equal(10, world.Progression.PopulationCapacity);
    }

    [Fact]
    public void UnlockingZoneTwoRaisesPopulationCapacityToFifteen()
    {
        var ranks = Enumerable.Repeat(1, 10).ToArray();
        for (int i = 0; i < 7; i++) ranks[i] = 2;
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 10,
            StartingTrainerRanks = ranks,
            StartDormitoryLevel = 2,
        }, 9);

        var result = world.UnlockZone("zone_2");

        Assert.True(result.Ok, result.Reason);
        Assert.Equal(15, world.Progression.PopulationCapacity);
    }

    [Fact]
    public void ZoneThreeCannotSkipZoneTwoEvenWhenRankAndDormitoryGatesAreMet()
    {
        var ranks = Enumerable.Repeat(1, 15).ToArray();
        for (int i = 0; i < 12; i++) ranks[i] = 3;
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 15,
            StartingTrainerRanks = ranks,
            StartDormitoryLevel = 3,
        }, 10);

        var result = world.UnlockZone("zone_3");

        Assert.False(result.Ok);
        Assert.Equal("zone.prior_zone_required", result.Reason);
    }

    [Fact]
    public void DormitoryCanFinishItsNextLevelBeforeTheZoneThatRaisesTownHallTier()
    {
        var ranks = Enumerable.Repeat(2, 10).ToArray();
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 10,
            StartingTrainerRanks = ranks,
            StartTownHallLevel = 5,
            StartDormitoryLevel = 1,
            StartingConstructionStock = new Dictionary<ProductId, int>
            {
                [new ProductId("wood_ingot")] = 4,
                [new ProductId("stone_ingot")] = 4,
                [new ProductId("iron_ingot")] = 4
            },
        }, 11);

        var result = world.UpgradeDormitory();

        Assert.True(result.Ok, result.Reason);
        Assert.Equal(2, world.SupplyStocks.Single(x => x.ItemId == "product:wood_ingot").Available);
        Assert.Equal(1, world.Progression.DormitoryLevel);
        Assert.Equal(SimClock.DawnMinute + 2880, world.Progression.DormitoryUpgradeFinishMinute);
        world.RunFor(2880);
        Assert.Equal(2, world.Progression.DormitoryLevel);
        Assert.True(world.UnlockZone("zone_2").Ok);
        Assert.Equal(15, world.Progression.PopulationCapacity);
    }

    [Fact]
    public void DormitoryUpgradeRejectsTimerOverflowWithoutSpendingGoldOrMaterials()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 0,
            StartTreasury = 10000,
            StartTownHallLevel = 5,
            HubProgressionSettings = new HubProgressionConfig(facilityUpgradeMinutes: int.MaxValue),
            StartingConstructionStock = new Dictionary<ProductId, int>
            {
                [new ProductId("wood_ingot")] = 4,
                [new ProductId("stone_ingot")] = 4,
                [new ProductId("iron_ingot")] = 4
            }
        }, 116);
        long gold = world.Treasury;
        int wood = world.SupplyStocks.Single(x => x.ItemId == "product:wood_ingot").Available;

        var result = world.UpgradeDormitory();

        Assert.False(result.Ok);
        Assert.Equal("building.cost_overflow", result.Reason);
        Assert.Equal(gold, world.Treasury);
        Assert.Equal(wood, world.SupplyStocks.Single(x => x.ItemId == "product:wood_ingot").Available);
        Assert.Null(world.Progression.DormitoryUpgradeFinishMinute);
    }

    [Fact]
    public void DormitoryUpgradeWithoutConstructionIngotsHasNoEconomicSideEffects()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 0, StartTreasury = 10000,
            StartTownHallLevel = 5 }, 112);
        long gold = world.Treasury;

        var result = world.UpgradeDormitory();

        Assert.False(result.Ok);
        Assert.Equal("building.insufficient_materials", result.Reason);
        Assert.Equal(gold, world.Treasury);
        Assert.Null(world.Progression.DormitoryUpgradeFinishMinute);
    }

    [Fact]
    public void TownHallLevelSixRequiresZoneTwo()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 10,
            StartingTrainerRanks = Enumerable.Repeat(2, 10).ToArray(),
            StartTownHallLevel = 5,
            StartDormitoryLevel = 2,
        }, 12);

        var result = world.UpgradeTownHall();

        Assert.False(result.Ok);
        Assert.Equal("building.zone_required", result.Reason);
        Assert.Equal(5, world.Progression.TownHallLevel);
    }
}
