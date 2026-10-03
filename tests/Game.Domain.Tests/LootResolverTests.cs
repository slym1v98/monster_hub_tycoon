using System.Collections.Generic;
using Game.Domain;
using Game.Domain.Combat;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class LootResolverTests
    {
        static BattleResult Victory()
        {
            var team = new MonsterSnapshot(new MonsterId("team"), MonsterElement.Grass,
                new MonsterStats(100, 100, 10, 100, 0), 100, new[] { "grass_strike" });
            var foe = new MonsterSnapshot(new MonsterId("foe"), MonsterElement.Fire,
                new MonsterStats(1, 1, 1, 1, 0), 1, new[] { "fire_strike" });
            return BattleResolver.Resolve(new BattleInput(new[] { team }, new[] { foe }), CombatConfig.Prototype, new SimRandom(1));
        }
        static ZoneDefinition Zone() => new ZoneDefinition("loot", "Loot", 1, 0,
            new[] { new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 1, 10) },
            new EncounterProfile(new Dictionary<MonsterElement, double> { [MonsterElement.Grass] = 1 }, 1, 100, 5, 20));
        static TrainerSnapshot Trainer(bool nightVision = false, double luck = 0) =>
            new TrainerSnapshot(0, 1, 1, Rarity.Common, Personality.Capitalist,
                new TrainerAttributes(1, luck, 1, 1), nightVision);

        [Fact]
        public void NightVisionDoublesLootAndExperienceExactlyOnceAndGoldIsGuaranteed()
        {
            var result = new LootResolver().Resolve(Victory(), Zone(), Trainer(true, 20), new SimTime(SimClock.DuskMinute),
                new LootConfig(backpackCapacity: 100), new SimRandom(9));
            Assert.Equal(120, result.Gold);
            Assert.Equal(40, result.TrainerExperience);
            Assert.Equal(10, Assert.Single(result.Collected).Quantity);
        }

        [Fact]
        public void CapacityReturnsCollectedAndDroppedQuantitiesThatConservePickedLoot()
        {
            var result = new LootResolver().Resolve(Victory(), Zone(), Trainer(true), new SimTime(SimClock.DuskMinute),
                new LootConfig(backpackCapacity: 3), new SimRandom(9));
            Assert.Equal(3, Assert.Single(result.Collected).Quantity);
            Assert.Equal(7, Assert.Single(result.Dropped).Quantity);
            Assert.Equal(10, result.Collected[0].Quantity + result.Dropped[0].Quantity);
        }

        [Fact]
        public void DefeatDoesNotAwardLootGoldOrExperience()
        {
            var defeat = BattleResolver.Resolve(new BattleInput(
                new[] { new MonsterSnapshot(new MonsterId("team"), MonsterElement.Grass, new MonsterStats(1, 1, 1, 1, 0), 0, new[] { "grass_strike" }) },
                new[] { new MonsterSnapshot(new MonsterId("foe"), MonsterElement.Fire, new MonsterStats(1, 1, 1, 1, 0), 1, new[] { "fire_strike" }) }), CombatConfig.Prototype, new SimRandom(2));
            var result = new LootResolver().Resolve(defeat, Zone(), Trainer(), new SimTime(SimClock.DawnMinute), new LootConfig(), new SimRandom(3));
            Assert.Equal(0, result.Gold); Assert.Equal(0, result.TrainerExperience); Assert.Empty(result.Collected);
        }
    }
}
