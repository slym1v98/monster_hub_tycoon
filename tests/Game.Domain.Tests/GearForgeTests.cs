using System;
using Game.Domain.Gear;
using Game.Domain;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class GearForgeTests
    {
        static GearCatalog Cat => GearCatalog.Default;
        static EnhancementModel Enh => new EnhancementModel { CostBase = 100, CostGrowth = 1.3, BreakFrom = 11, BreakChance = 0.3 };
        static GearConfig Cfg => Cat.Config;
        static GearItem Weapon(int tier, int enh = 0, int stars = 1, GearRefineGrade refine = GearRefineGrade.Normal, int dur = -1)
            => new GearItem("w:" + tier + ":" + enh + ":" + stars + ":" + (int)refine, Cat.GetSlot("monster.weapon"), tier, Cat.MaxDurabilityFor(GearGroup.MonsterCombat), null, enh, stars, refine, dur);

        [Fact]
        public void EnhanceDeterministicFromSeed()
        {
            var item = Weapon(2);
            var rng1 = new SimRandom(123);
            var r1 = GearForge.TryEnhance(item, Enh, Cfg, rng1, protectionCharm: false);
            var item2 = Weapon(2);
            var rng2 = new SimRandom(123);
            var r2 = GearForge.TryEnhance(item2, Enh, Cfg, rng2, protectionCharm: false);
            Assert.Equal(r1.Success, r2.Success);
            Assert.Equal(r1.Broke, r2.Broke);
            Assert.Equal(r1.GoldSpent, r2.GoldSpent);
        }

        [Fact]
        public void EnhanceBelowBreakLevelNeverBreaks()
        {
            for (int seed = 0; seed < 300; seed++)
            {
                var item = Weapon(3, enh: 5);
                var r = GearForge.TryEnhance(item, Enh, Cfg, new SimRandom(seed), false);
                Assert.False(r.Broke);
                Assert.False(item.IsDestroyed);
                Assert.Equal(r.Success ? 6 : 5, item.EnhanceLevel);
            }
        }

        [Fact]
        public void EnhanceToElevenCanBreakAndCharmPrevents()
        {
            int breakSeed = -1;
            for (int seed = 0; seed < 10000 && breakSeed < 0; seed++)
                if (GearForge.TryEnhance(Weapon(3, enh: 10), Enh, Cfg, new SimRandom(seed), false).Broke) breakSeed = seed;
            Assert.True(breakSeed >= 0);

            var noCharm = Weapon(3, enh: 10);
            var broken = GearForge.TryEnhance(noCharm, Enh, Cfg, new SimRandom(breakSeed), false);
            Assert.True(broken.Broke);
            Assert.True(noCharm.IsDestroyed);
            Assert.False(broken.CharmConsumed);

            var withCharm = Weapon(3, enh: 10);
            var saved = GearForge.TryEnhance(withCharm, Enh, Cfg, new SimRandom(breakSeed), true);
            Assert.False(saved.Broke);
            Assert.False(withCharm.IsDestroyed);
            Assert.True(saved.CharmConsumed);
            Assert.Equal(10, withCharm.EnhanceLevel);
        }

        [Fact]
        public void EnhanceSpendsRoundedUpAttemptCostAndOneStone()
        {
            var item = Weapon(2, enh: 5);
            var r = GearForge.TryEnhance(item, Enh, Cfg, new SimRandom(1), false);
            Assert.Equal((long)Math.Ceiling(Enh.AttemptCost(6)), r.GoldSpent);
            Assert.Equal(1, r.StonesSpent);
        }

        [Fact]
        public void EnhanceRejectsMaxedOrDestroyedItem()
        {
            Assert.Throws<InvalidOperationException>(() => GearForge.TryEnhance(Weapon(2, enh: 20), Enh, Cfg, new SimRandom(1), false));
            var gone = Weapon(3, enh: 10);
            // tạo một món đã phá hủy bằng cách thủ công (không qua Cường hóa)
            var dead = new GearItem("dead", Cat.GetSlot("monster.weapon"), 1, 100);
            typeof(GearItem).GetProperty("IsDestroyed", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public)?.SetValue(dead, true);
            Assert.Throws<InvalidOperationException>(() => GearForge.TryEnhance(dead, Enh, Cfg, new SimRandom(1), false));
        }

        [Fact]
        public void StarUpConsumesSameSlotJunkAndRaisesStars()
        {
            var item = Weapon(2, stars: 2);
            var junk = new GearItem("junk", Cat.GetSlot("monster.weapon"), 1, 100);
            var r = GearForge.StarUp(item, junk);
            Assert.True(r.Success);
            Assert.Equal(3, item.Stars);
            Assert.True(junk.IsDestroyed);
        }

        [Fact]
        public void StarUpRejectsMaxStarsWrongSlotOrSameItem()
        {
            var maxed = Weapon(2, stars: 5);
            var junk = new GearItem("junk", Cat.GetSlot("monster.weapon"), 1, 100);
            Assert.False(GearForge.StarUp(maxed, junk).Success);
            Assert.False(junk.IsDestroyed);
            var armorJunk = new GearItem("a", Cat.GetSlot("monster.armor"), 1, 100);
            Assert.False(GearForge.StarUp(Weapon(2), armorJunk).Success);
            var self = Weapon(2);
            Assert.False(GearForge.StarUp(self, self).Success);
        }

        [Fact]
        public void RefineRequiresMaterialsAndIncreasesGrade()
        {
            var item = Weapon(3, refine: GearRefineGrade.Normal);
            var r = GearForge.Refine(item, worldBossCrystal: Cfg.RefineCrystalCost, distilledWater: Cfg.RefineWaterCost, Cfg);
            Assert.True(r.Success);
            Assert.Equal(GearRefineGrade.Refined, item.Refine);
            Assert.Equal(Cfg.RefineCrystalCost, r.CrystalConsumed);
            Assert.Equal(Cfg.RefineWaterCost, r.WaterConsumed);
        }

        [Fact]
        public void RefineFailsWithoutMaterials()
        {
            var item = Weapon(3, refine: GearRefineGrade.Refined);
            var r = GearForge.Refine(item, worldBossCrystal: 0, distilledWater: Cfg.RefineWaterCost, Cfg);
            Assert.False(r.Success);
            Assert.Equal(GearRefineGrade.Refined, item.Refine);
        }

        [Fact]
        public void RepairRestoresDurabilityAndChargesGold()
        {
            var item = Weapon(2, dur: 40);
            long goldBefore = 10000;
            var r = GearForge.Repair(item, goldBefore, Cfg);
            Assert.True(r.Success);
            Assert.Equal(Cfg.MonsterMaxDurability, item.Durability);
            long expectedCost = (Cfg.MonsterMaxDurability - 40) * (long)Cfg.RepairGoldPerDurability;
            Assert.Equal(goldBefore - expectedCost, r.GoldRemaining);
        }

        [Fact]
        public void RepairFailsWhenNoGold()
        {
            var item = Weapon(2, dur: 10);
            var r = GearForge.Repair(item, 0, Cfg);
            Assert.False(r.Success);
            Assert.Equal(10, item.Durability);
        }

        [Fact]
        public void AuraNeverWearsAndRepairCostZero()
        {
            var item = new GearItem("aura.goggles:3", Cat.GetSlot("aura.goggles"), 3, Cat.MaxDurabilityFor(GearGroup.Aura), null);
            var r = GearForge.Repair(item, 1000, Cfg);
            Assert.True(r.Success); // Aura không hao mòn, sửa 0 Gold
            Assert.Equal(Cfg.AuraMaxDurability, item.Durability);
            Assert.Equal(1000, r.GoldRemaining);
        }
    }
}
