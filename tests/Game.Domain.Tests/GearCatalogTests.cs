using System;
using System.Linq;
using Game.Domain.Gear;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class GearCatalogTests
    {
        [Fact]
        public void SlotDefinitionsMatchGddCountAndGroups()
        {
            var catalog = GearCatalog.Default;
            Assert.Equal(18, catalog.Slots.Count); // 6 utility + 6 aura + 6 monster combat
            Assert.Equal(6, catalog.Slots.Count(s => s.Group == GearGroup.TrainerUtility));
            Assert.Equal(6, catalog.Slots.Count(s => s.Group == GearGroup.Aura));
            Assert.Equal(6, catalog.Slots.Count(s => s.Group == GearGroup.MonsterCombat));
        }

        [Fact]
        public void SlotOwnershipAndIndicesAreValid()
        {
            var catalog = GearCatalog.Default;
            foreach (var slot in catalog.Slots)
            {
                Assert.InRange(slot.Index, 0, 5);
                Assert.True(Enum.IsDefined(typeof(GearStatKind), slot.StatKind));
                Assert.Equal(slot.OwnerUnit, slot.Group == GearGroup.MonsterCombat ? "Monster" : "Trainer");
            }
        }

        [Fact]
        public void BaseStatsExistForEverySlotAndTier()
        {
            var catalog = GearCatalog.Default;
            foreach (var slot in catalog.Slots)
            {
                for (int tier = 1; tier <= 5; tier++)
                {
                    var stats = catalog.GetBaseStats(slot.Id, tier);
                    Assert.NotNull(stats);
                    Assert.True(stats.Attack >= 0 && stats.Defense >= 0 && stats.Hp >= 0 && stats.AttackSpeed >= 0 && stats.CriticalChance >= 0);
                }
            }
        }

        [Fact]
        public void RefineMultiplierAndStarPctArePositive()
        {
            var catalog = GearCatalog.Default;
            for (int refine = 0; refine <= 4; refine++)
            {
                double mult = catalog.GetRefineMultiplier(refine);
                Assert.True(mult > 0, $"Refine {refine} multiplier positive");
            }
            for (int stars = 1; stars <= 5; stars++)
            {
                double pct = catalog.GetStarPct(stars);
                Assert.True(pct >= 0, $"Stars {stars} pct non-negative");
            }
        }

        [Fact]
        public void SetBonusesUseOnlyTwoFourSixPieceThresholds()
        {
            var catalog = GearCatalog.Default;
            Assert.NotEmpty(catalog.Sets);
            foreach (var set in catalog.Sets)
            {
                Assert.NotEmpty(set.Bonuses);
                foreach (var bonus in set.Bonuses) Assert.Contains(bonus.RequiredCount, new[] { 2, 4, 6 });
            }
        }
    }
}
