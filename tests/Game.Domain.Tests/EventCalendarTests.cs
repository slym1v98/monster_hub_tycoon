using Game.Domain;
using System.Collections.Generic;
using System;
using System.Linq;
using Game.Domain.Materials;
using Xunit;

public sealed class EventCalendarTests
{
    [Fact]
    public void MonsterFluHalvesCurrentMonsterHpAndCausesConfiguredDailyLoss()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartTreasury = 100000,
            UnlockedZoneIds = Array.Empty<string>(),
            StartMinute = SimClock.DawnMinute }, 121);
        world.SetBuildingPower(BuildingKind.Hospital, false);
        var monster = world.MonstersForTrainer(0).Single(x => x.IsActive);
        long initial = monster.CurrentHp;

        Assert.True(world.ActivateMonsterFlu(2).Ok);
        Assert.Equal(initial / 2, world.MonstersForTrainer(0).Single(x => x.Id == monster.Id).CurrentHp);
        world.RunFor(SimClock.MinutesPerDay);
        Assert.Equal((long)Math.Floor(monster.MaxHp * 0.45), world.MonstersForTrainer(0).Single(x => x.Id == monster.Id).CurrentHp);
        Assert.Contains(world.ActiveEvents, e => e.Kind == HubEventKind.MonsterFlu && e.IsActive);
    }

    [Fact]
    public void HospitalVaccineEndsMonsterFluAndConsumesConfiguredStock()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            StartTreasury = 100000,
            StartingProductStock = new Dictionary<ProductId, int> { [new ProductId("vaccine")] = 1 }
        }, 117);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        Assert.True(world.ActivateMonsterFlu().Ok);

        Assert.True(world.UseVaccineAgainstMonsterFlu().Ok);
        Assert.DoesNotContain(world.ActiveEvents, e => e.Kind == HubEventKind.MonsterFlu);
        Assert.Equal(0, events.OfType<SupplyStockChanged>().Last(x => x.ItemId == "product:vaccine").Balance.Available);
        world.RunFor(SimClock.MinutesPerDay);
        Assert.Contains(events, e => e is HubEventChanged change && change.Kind == HubEventKind.MonsterFlu && change.Phase == "Cured");
    }

    [Fact]
    public void BreedingSeasonIncreasesCaptureGoodsDemandAndAllowsDirectorPriceUpToThreeTimes()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            StartTreasury = 100000,
            StartMinute = SimClock.DawnMinute + 1,
            StartTownHallLevel = 4,
            StartingProductStock = new Dictionary<ProductId, int>
            {
                [new ProductId("capture_ball")] = 10,
                [new ProductId("trap")] = 10
            },
            StartingFacilityLevels = new Dictionary<string, int> { ["tool_workshop"] = 1 }
        }, 118);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        Assert.False(world.SetProductPrice("capture_ball", 31).Ok);
        Assert.True(world.ActivateBreedingSeason().Ok);
        world.RunFor(1);

        Assert.Equal(3, world.Trainers[0].Products.GetValueOrDefault(new ProductId("capture_ball")));
        Assert.Equal(3, world.Trainers[0].Products.GetValueOrDefault(new ProductId("trap")));
        Assert.True(world.SetProductPrice("capture_ball", 30).Ok);
        Assert.False(world.SetProductPrice("capture_ball", 31).Ok);
        Assert.True(world.PurchaseProduct(0, "capture_ball", 1).Ok);
        Assert.Equal(30, events.OfType<ProductPurchased>().Last(e => e.ProductId == "capture_ball").UnitPrice);
    }

    [Fact]
    public void RandomCrisisScheduleStartsDeterministicConfiguredEvent()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            StartMinute = SimClock.DawnMinute + 1,
            EventSettings = new HubEventConfig(randomCrisisCheckIntervalMinutes: 60, randomCrisisProbability: 1)
        }, 119);

        world.RunFor(60);

        Assert.Contains(world.ActiveEvents, e => e.Kind == HubEventKind.MonsterFlu ||
            e.Kind == HubEventKind.BreedingSeason || e.Kind == HubEventKind.MonsterSiege);
    }

    [Fact]
    public void BreedingSeasonHasObservableConfiguredLifetime()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 0,
            EventSettings = new HubEventConfig(breedingSeasonDurationDays: 2) }, 122);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        Assert.True(world.ActivateBreedingSeason().Ok);
        Assert.Contains(world.ActiveEvents, e => e.Kind == HubEventKind.BreedingSeason && e.IsActive);
        world.RunFor(2 * SimClock.MinutesPerDay);

        Assert.DoesNotContain(world.ActiveEvents, e => e.Kind == HubEventKind.BreedingSeason);
        Assert.Contains(events, e => e is HubEventChanged change && change.Kind == HubEventKind.BreedingSeason && change.Phase == "Ended");
    }

    [Fact]
    public void WorldBossRequiresGoldAndRankThenResolvesThroughDomainEvents()
    {
        var events = new List<IDomainEvent>();
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartingTrainerRanks = new[] { 5 },
            StartTreasury = 1000, StartMinute = SimClock.DawnMinute,
            EventSettings = new HubEventConfig(worldBossActivationGold: 50, worldBossDurationMinutes: 10,
                worldBossMinimumRank: 5, worldBossHp: 100, worldBossAttack: 1) }, 123);
        world.EventRaised += events.Add;

        Assert.True(world.ActivateWorldBossRaid().Ok);
        Assert.Equal(950, world.Treasury);
        Assert.Contains(world.ActiveEvents, e => e.Kind == HubEventKind.WorldBossRaid);
        world.RunFor(10);

        Assert.DoesNotContain(world.ActiveEvents, e => e.Kind == HubEventKind.WorldBossRaid);
        Assert.Contains(events, e => e is HubEventChanged change && change.Kind == HubEventKind.WorldBossRaid && change.Phase == "Victory");
        Assert.False(world.ActivateWorldBossRaid().Ok);
    }

    [Fact]
    public void WorldBossCooldownOverflowRejectsBeforeChargingActivationGold()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            StartingTrainerRanks = new[] { 5 },
            StartMinute = int.MaxValue - 100,
            StartTreasury = 1000,
            EventSettings = new HubEventConfig(worldBossActivationGold: 50, worldBossDurationMinutes: 1,
                worldBossCooldownMinutes: 1440, worldBossMinimumRank: 5)
        }, 120);
        long gold = world.Treasury;

        var result = world.ActivateWorldBossRaid();

        Assert.False(result.Ok);
        Assert.Equal("event.time_overflow", result.Reason);
        Assert.Equal(gold, world.Treasury);
        Assert.DoesNotContain(world.ActiveEvents, e => e.Kind == HubEventKind.WorldBossRaid);
    }

    [Fact]
    public void SiegeDefeatCanDamageServiceBuilding()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartTreasury = 10000,
            StartMinute = SimClock.DawnMinute,
            EventSettings = new HubEventConfig(monsterSiegeDurationMinutes: 1,
                monsterSiegeEnemyHp: 100000, monsterSiegeEnemyAttack: 10000,
                monsterSiegeBuildingDamageProbability: 1) }, 124);

        Assert.True(world.ActivateMonsterSiege().Ok);
        world.RunFor(1);

        Assert.Contains(world.Buildings, building => building.Damaged);
    }

    [Fact]
    public void WorldBossOutcomeIsRepeatableForSameSeedAndCommands()
    {
        (long treasury, string phase, int crystalCount) Run()
        {
            var world = new HubWorld(new SimConfig { TrainerCount = 1, StartingTrainerRanks = new[] { 5 },
                StartTreasury = 1000, StartMinute = SimClock.DawnMinute,
                EventSettings = new HubEventConfig(worldBossActivationGold: 50, worldBossDurationMinutes: 10,
                    worldBossMinimumRank: 5, worldBossHp: 100, worldBossAttack: 1) }, 991);
            var events = new List<IDomainEvent>();
            world.EventRaised += events.Add;
            world.ActivateWorldBossRaid();
            world.RunFor(10);
            string phase = events.OfType<HubEventChanged>().Last(e => e.Kind == HubEventKind.WorldBossRaid).Phase;
            return (world.Treasury, phase, world.Trainers[0].Products.GetValueOrDefault(new Game.Domain.Materials.ProductId("world_boss_crystal")));
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void DamagedBuildingConsumesGoldAndThreeIngotsThenRecoversAtRepairCompletion()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartTreasury = 1000,
            StartMinute = SimClock.DawnMinute,
            StartingConstructionStock = new Dictionary<ProductId, int>
            {
                [new ProductId("wood_ingot")] = 2, [new ProductId("stone_ingot")] = 2, [new ProductId("iron_ingot")] = 2
            },
            HubProgressionSettings = new HubProgressionConfig(repairGoldPerBuildingLevel: 20, repairMinutes: 10),
            EventSettings = new HubEventConfig(monsterSiegeDurationMinutes: 1,
                monsterSiegeEnemyHp: 100000, monsterSiegeEnemyAttack: 10000,
                monsterSiegeBuildingDamageProbability: 1) }.WithServiceFacilities(), 125);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        Assert.True(world.ActivateMonsterSiege().Ok);
        world.RunFor(1);
        var damaged = Assert.Single(world.Buildings, building => building.Damaged);
        long gold = world.Treasury;

        Assert.True(world.RepairBuilding(damaged.Kind).Ok);
        Assert.Equal(gold - 20, world.Treasury);
        Assert.True(world.Buildings[(int)damaged.Kind].Damaged);
        Assert.Equal(world.Now.TotalMinutes + 10, world.Buildings[(int)damaged.Kind].RepairFinishMinute);
        Assert.Equal(1, world.SupplyStocks.Single(x => x.ItemId == "product:wood_ingot").Available);
        world.RunFor(10);

        Assert.False(world.Buildings[(int)damaged.Kind].Damaged);
        Assert.Null(world.Buildings[(int)damaged.Kind].RepairFinishMinute);
        Assert.Contains(events, e => e is BuildingRepairCompleted complete && complete.Building == damaged.Kind);
    }

    [Fact]
    public void RepairWithoutAllIngotsRejectsWithoutSpendingGold()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartTreasury = 1000,
            StartMinute = SimClock.DawnMinute,
            EventSettings = new HubEventConfig(monsterSiegeDurationMinutes: 1,
                monsterSiegeEnemyHp: 100000, monsterSiegeEnemyAttack: 10000,
                monsterSiegeBuildingDamageProbability: 1) }, 126);
        Assert.True(world.ActivateMonsterSiege().Ok);
        world.RunFor(1);
        var damaged = Assert.Single(world.Buildings, building => building.Damaged);
        long gold = world.Treasury;

        var result = world.RepairBuilding(damaged.Kind);

        Assert.False(result.Ok);
        Assert.Equal("building.insufficient_materials", result.Reason);
        Assert.Equal(gold, world.Treasury);
        Assert.True(world.Buildings[(int)damaged.Kind].Damaged);
    }

    [Fact]
    public void BlackFridayRunsForThreeInGameDaysBeforePaydayOnly()
    {
        int payday = SimClock.PaydayMinute(0);
        Assert.False(HubEventCalendar.IsBlackFriday(payday - 3 * SimClock.MinutesPerDay - 1, payday));
        Assert.True(HubEventCalendar.IsBlackFriday(payday - 3 * SimClock.MinutesPerDay, payday));
        Assert.True(HubEventCalendar.IsBlackFriday(payday - 1, payday));
        Assert.False(HubEventCalendar.IsBlackFriday(payday, payday));
    }

    [Fact]
    public void BlackFridayStartsAtItsScheduledMinuteAndRaisesAnEvent()
    {
        int start = SimClock.PaydayMinute(0) - 3 * SimClock.MinutesPerDay;
        var world = new HubWorld(new SimConfig { TrainerCount = 0, StartMinute = start - 1 }, 15);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        world.RunFor(1);

        Assert.Contains(world.ActiveEvents, x => x.Kind == HubEventKind.BlackFriday && x.IsActive);
        Assert.Contains(events, x => x is HubEventChanged change && change.Kind == HubEventKind.BlackFriday && change.Phase == "Started");
    }

    [Fact]
    public void BlackFridayRunsImpulsePurchasesEachDayAndEndsAtPayday()
    {
        int payday = SimClock.PaydayMinute(0);
        int start = payday - 3 * SimClock.MinutesPerDay;
        var world = new HubWorld(new SimConfig { TrainerCount = 0, StartMinute = start - 1 }, 151);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        world.RunFor(payday - (start - 1));
        world.ResolvePayday();

        Assert.Equal(3, events.OfType<HubEventChanged>().Count(e => e.Kind == HubEventKind.BlackFriday && e.Phase == "ImpulsePurchases"));
        Assert.Contains(events, e => e is HubEventChanged change && change.Kind == HubEventKind.BlackFriday && change.Phase == "Ended");
    }

    [Fact]
    public void TaxOverThirtyPercentStartsInspectionAndBribeResolvesItWithoutFine()
    {
        var world = new HubWorld(new SimConfig { StartTreasury = 1000 }, 16);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        Assert.True(world.SetMarketTaxRate(0.30).Ok);
        Assert.Empty(world.ActiveEvents);

        Assert.True(world.SetMarketTaxRate(0.31).Ok);
        Assert.Contains(world.ActiveEvents, x => x.Kind == HubEventKind.LaborInspection);
        Assert.True(world.BribeLaborInspector(25).Ok);

        Assert.Equal(975, world.Treasury);
        Assert.DoesNotContain(world.ActiveEvents, x => x.Kind == HubEventKind.LaborInspection);
        Assert.Contains(events, x => x is HubEventChanged change && change.Kind == HubEventKind.LaborInspection && change.Phase == "Bribed");
    }
}
