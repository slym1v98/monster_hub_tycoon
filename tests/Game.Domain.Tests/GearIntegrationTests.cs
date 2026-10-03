using System;
using System.Linq;
using Game.Domain.Gear;
using Game.Domain.Monsters;
using Game.Domain.Combat;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class GearIntegrationTests
    {
        static GearCatalog Cat => GearCatalog.Default;

        [Fact]
        public void MonsterGearAddsStatsToSnapshotButNotBaseStats()
        {
            var def = MonsterCatalog.Default.Definitions.First();
            var monster = Monster.Create(new MonsterId("m1"), def, Rarity.Common, MonsterIvGrade.C, 10, seed: 1, statConfig: null);
            var loadout = new GearLoadout();
            loadout.Equip(new GearItem("w", Cat.GetSlot("monster.weapon"), 3, 100));
            loadout.Equip(new GearItem("a", Cat.GetSlot("monster.armor"), 3, 100));
            var baseStats = monster.Stats;
            var boosted = MonsterSnapshot.FromMonster(monster, monster.CombatSkillIds, totalMinutes: 0, loadout: loadout, catalog: Cat);
            Assert.True(boosted.Stats.Attack > baseStats.Attack);
            Assert.True(boosted.Stats.Defense > baseStats.Defense);
            // base stats unchanged
            Assert.Equal(baseStats.Attack, monster.Stats.Attack);
        }

        [Fact]
        public void BrokenMonsterGearContributesNothing()
        {
            var def = MonsterCatalog.Default.Definitions.First();
            var monster = Monster.Create(new MonsterId("m2"), def, Rarity.Common, MonsterIvGrade.C, 10, seed: 2, statConfig: null);
            var loadout = new GearLoadout();
            var broken = new GearItem("w", Cat.GetSlot("monster.weapon"), 5, 100, null, 0, 1, GearRefineGrade.Normal, 0);
            loadout.Equip(broken);
            var snap = MonsterSnapshot.FromMonster(monster, monster.CombatSkillIds, totalMinutes: 0, loadout: loadout, catalog: Cat);
            Assert.Equal(monster.Stats.Attack, snap.Stats.Attack, 9);
        }

        [Fact]
        public void SetBonusIncludedInSnapshot()
        {
            var def = MonsterCatalog.Default.Definitions.First();
            var monster = Monster.Create(new MonsterId("m3"), def, Rarity.Common, MonsterIvGrade.C, 10, seed: 3, statConfig: null);
            var loadout = new GearLoadout();
            loadout.Equip(new GearItem("w", Cat.GetSlot("monster.weapon"), 1, 100, "monster_set_alpha"));
            loadout.Equip(new GearItem("a", Cat.GetSlot("monster.armor"), 1, 100, "monster_set_alpha"));
            var snap = MonsterSnapshot.FromMonster(monster, monster.CombatSkillIds, null, 0, loadout, Cat);
            var setBonus = GearLoadout.SetBonus(loadout.Equipped, Cat);
            Assert.True(snap.Stats.Attack > monster.Stats.Attack + GearScore.EffectiveStats(loadout.Equipped[0], Cat).Attack - 0.001);
            Assert.Equal(5, setBonus.Attack); // 2-piece bonus
        }

        [Fact]
        public void TrainerUtilityGearAffectsBackpackAndNeeds()
        {
            var trainer = new Trainer(new SimRandom(1));
            var loadout = new GearLoadout();
            loadout.Equip(new GearItem("bp", Cat.GetSlot("trainer.backpack"), 3, 100));
            loadout.Equip(new GearItem("bt", Cat.GetSlot("trainer.bottle"), 3, 100));
            var stats = GearLoadout.TotalStats(loadout.Equipped, Cat);
            Assert.True(stats.BackpackCapacity > 0);
            Assert.True(stats.HydrationDecayReduction > 0);
        }

        [Fact]
        public void NightVisionFromGogglesAura()
        {
            var loadout = new GearLoadout();
            loadout.Equip(new GearItem("g", Cat.GetSlot("aura.goggles"), 2, 100));
            var stats = GearLoadout.TotalStats(loadout.Equipped, Cat);
            Assert.True(stats.NightVision);
        }

        [Fact]
        public void TrainerAuraStatsBoostMonsterSnapshot()
        {
            var def = MonsterCatalog.Default.Definitions.First();
            var monster = Monster.Create(new MonsterId("m4"), def, Rarity.Common, MonsterIvGrade.C, 10, seed: 4);
            var auraLoadout = new GearLoadout();
            auraLoadout.Equip(new GearItem("whistle", Cat.GetSlot("aura.whistle"), 3, 100));
            var aura = GearLoadout.TotalStats(auraLoadout.Equipped, Cat);
            var snap = MonsterSnapshot.FromMonster(monster, monster.CombatSkillIds, trainerAura: aura);
            Assert.True(snap.Stats.Attack > monster.Stats.Attack);
        }

        [Fact]
        public void EnhancementAddsFlatTierOneIncrementBeforeStarAndRefineMultipliers()
        {
            var baseItem = new GearItem("base", Cat.GetSlot("monster.weapon"), 1, 100);
            var enhanced = new GearItem("enh", Cat.GetSlot("monster.weapon"), 1, 100, null, enhanceLevel: 2);
            double unit = Cat.GetBaseStats("monster.weapon", 1).Attack;
            Assert.Equal(unit * Cat.Config.EnhancePerLevelFraction * 2,
                GearScore.EffectiveStats(enhanced, Cat).Attack - GearScore.EffectiveStats(baseItem, Cat).Attack, 9);
        }

        [Fact]
        public void WorkbookParametersHaveUniqueIdsAndSupportedStatuses()
        {
            var values = Cat.BalanceParameters.Concat(new EnhancementModel().BalanceParameters).ToArray();
            Assert.Equal(values.Length, values.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count());
            Assert.All(values, x => Assert.Contains(x.Status, new[] { "Locked", "Prototype", "TBD" }));
            Assert.Contains(values, x => x.Id == "gear.star_probability_step_5_unreachable" && x.Status == "TBD");
        }

        [Fact]
        public void AreaSkillWearsMonsterGearOncePerCastAndOncePerHit()
        {
            var trainer = new Trainer(new SimRandom(41));
            var definition = MonsterCatalog.Default.Definitions.First();
            var actor = Monster.Create(new MonsterId("gear_actor"), definition, Rarity.Common, MonsterIvGrade.C, 10, seed: 41);
            trainer.Roster.Add(actor);
            var weapon = new GearItem("aoe_weapon", Cat.GetSlot("monster.weapon"), 1, 100);
            actor.Gear.Equip(weapon);

            MonsterSnapshot Enemy(string id) => new MonsterSnapshot(new MonsterId(id), MonsterElement.Dark,
                new MonsterStats(1000, 1, 100, 0.1, 0), 1000, new[] { "strike" });
            var actorSnapshot = MonsterSnapshot.FromMonster(actor, new[] { "strike" }, loadout: actor.Gear, catalog: Cat);
            var config = new CombatConfig(new SkillCatalog(new[]
            {
                new SkillDefinition("strike", actor.Element, 0.01, 0, SkillTargetRule.AllEnemies)
            }), criticalMultiplier: 2, maxRounds: 1);
            var battle = BattleResolver.Resolve(new BattleInput(new[] { actorSnapshot }, new[] { Enemy("enemy_a"), Enemy("enemy_b") }), config, new SimRandom(51));
            int hitsToActor = battle.Actions.Count(x => x.TargetId == actor.Id && x.Damage > 0);
            var world = new HubWorld(new SimConfig { TrainerCount = 1 }, 51);
            var result = new ExpeditionResult(new[] { battle }, new ExpeditionLoot(Array.Empty<MaterialQuantity>(), Array.Empty<MaterialQuantity>(), 0, 0));
            var wearEvents = new System.Collections.Generic.List<GearDurabilityChanged>();
            world.EventRaised += e => { if (e is GearDurabilityChanged value) wearEvents.Add(value); };

            world.ApplyGearWear(trainer, result, minutes: 0);

            Assert.Equal(100 - Cat.Config.MonsterWearPerAction - hitsToActor * Cat.Config.MonsterWearPerHit, weapon.Durability);
            Assert.Equal(1 + hitsToActor, wearEvents.Count);
            Assert.Contains(wearEvents, x => x.Cause == "MonsterAction");
            Assert.Equal(hitsToActor, wearEvents.Count(x => x.Cause == "MonsterHit"));
        }
    }
}
