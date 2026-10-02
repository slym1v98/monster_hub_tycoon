using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

public sealed class HubWorldMonsterTests
{
    [Fact]
    public void ViewExposesOwnedMonstersAndFarmAppliesFinalHpAndTrainerExperience()
    {
        var cfg = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DawnMinute,
            BackpackCapacity = 100, FarmChunkMinutes = 30, StartTreasury = 10000 };
        var world = new HubWorld(cfg, 1, new ScriptedExpedition(100, 0), new FixedPriceMarket(cfg));
        world.RunFor(60);
        var trainer = Assert.Single(world.Trainers);
        var monster = Assert.Single(trainer.Monsters);
        Assert.Equal(2, trainer.Level);
        Assert.Equal(MonsterLifeState.Fainted, monster.LifeState);
        Assert.Equal(0, monster.CurrentHp);
        Assert.Equal(ReturnReason.TeamDown.ToString(), trainer.StateReason);
    }

    [Fact]
    public void WalkingUsesZoneDurationAndNeedDecayBeforeFarm()
    {
        var cfg = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DawnMinute, StartWithNightVision = true,
            BackpackCapacity = 100, FarmChunkMinutes = 30, StartTreasury = 10000 };
        var world = new HubWorld(cfg, 1, new ScriptedExpedition(0, 0, faint: false), new FixedPriceMarket(cfg));
        world.RunFor(30);
        Assert.Equal(30, world.Now.TotalMinutes - SimClock.DawnMinute);
        Assert.Equal(TrainerState.Farming, Assert.Single(world.Trainers).State);
        Assert.Equal("zone_1", world.Trainers[0].CurrentZoneId);
        Assert.Equal(97, world.Trainers[0].Stamina, 8);
    }

    [Fact]
    public void HubSelectsHighestIncomeEligibleUnlockedZoneAndKeepsTypedYield()
    {
        ZoneDefinition Make(string id, MaterialId material, double gold) => new ZoneDefinition(id, id, 1, 30,
            new[] { new ZoneMaterialWeight(material, 1, 1) }, new EncounterProfile(new Dictionary<Game.Domain.Monsters.MonsterElement, double> {
                [Game.Domain.Monsters.MonsterElement.Grass] = 1 }, 1, gold, 1, 10));
        var poor = Make("poor", MaterialId.For(MaterialFamily.Ore, 1), 10);
        var rich = Make("rich", MaterialId.For(MaterialFamily.Herb, 1), 100);
        var config = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DawnMinute, StartTreasury = 10000,
            UnlockedZoneIds = new[] { "poor", "rich" }, ZoneCatalogSettings = new ZoneCatalog(new[] { poor, rich }), BackpackCapacity = 10 };
        var resolver = new ScriptedExpedition(0, 0, faint: false, units: 1);
        var world = new HubWorld(config, 4, resolver, new FixedPriceMarket(config));
        world.RunFor(60);
        Assert.Equal("rich", resolver.LastZoneId);
        Assert.Contains(world.Trainers[0].Monsters, m => m.IsActive);
        Assert.Contains(world.Trainers[0].CurrentZoneId, new[] { "rich" });
        Assert.True(world.Trainers[0].BackpackUnits > 0);
    }

    [Fact]
    public void DefaultResolverUsesZoneEncounterRateAndChunkDuration()
    {
        var teamMonster = new MonsterSnapshot(new MonsterId("hero"), Game.Domain.Monsters.MonsterElement.Grass,
            new MonsterStats(1000, 500, 100, 10, 0), 1000, new[] { "grass_strike" });
        var trainer = new TrainerSnapshot(0, 1, 1, Rarity.Common, Personality.Capitalist,
            new TrainerAttributes(1, 0, 1, 1), team: new[] { teamMonster }, activeMonsterId: teamMonster.Id);
        var zone = new ZoneDefinition("rate", "Rate", 1, 0,
            new[] { new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 1, 1) },
            new EncounterProfile(new Dictionary<Game.Domain.Monsters.MonsterElement, double> {
                [Game.Domain.Monsters.MonsterElement.Grass] = 1 }, 4, 1, 0, 10));
        var result = new DefaultExpeditionResolver(SimConfig.Default).Resolve(trainer, zone, 30, new SimRandom(1));
        Assert.Equal(2, result.Battles.Count);
        Assert.Equal(20, result.TrainerExperience);
    }

    [Fact]
    public void NoVisionTrainerReturnsAtDuskAndNightVisionTrainerCanRemainOutside()
    {
        HubWorld Create(bool vision)
        {
            var cfg = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DuskMinute - 5,
                StartWithNightVision = vision, StartTreasury = 10000, BackpackCapacity = 100, FarmChunkMinutes = 30 };
            return new HubWorld(cfg, 2, new ScriptedExpedition(0, 0, faint: false), new FixedPriceMarket(cfg));
        }
        var without = Create(false); without.RunFor(10);
        Assert.NotEqual(TrainerState.Farming, without.Trainers[0].State);
        var with = Create(true); with.RunFor(10);
        Assert.Equal(TrainerState.Traveling, with.Trainers[0].State);
    }

    private sealed class ScriptedExpedition : IExpeditionResolver
    {
        readonly long experience; readonly long gold; readonly bool faint; readonly int units;
        public string LastZoneId { get; private set; }
        public ScriptedExpedition(long experience, long gold, bool faint = true, int units = 0) { this.experience = experience; this.gold = gold; this.faint = faint; this.units = units; }
        public ExpeditionResult Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random)
        {
            LastZoneId = zone.Id;
            var hp = new Dictionary<MonsterId, long>();
            foreach (var monster in trainer.Team) hp[monster.Id] = faint ? 0 : monster.CurrentHp;
            var material = zone.MaterialWeights[0].MaterialId;
            return new ExpeditionResult(Array.Empty<Game.Domain.Combat.BattleResult>(),
                new ExpeditionLoot(units == 0 ? Array.Empty<MaterialQuantity>() : new[] { new MaterialQuantity(material, units) }, Array.Empty<MaterialQuantity>(), gold, experience), experience, hp);
        }
    }
}
