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
            Assert.Equal(setBonus.Attack, 5); // 2-piece bonus
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
    }
}
