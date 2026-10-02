using System;
using System.Linq;
using Game.Domain.Gear;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class GearScoreTests
    {
        static GearCatalog Cat => GearCatalog.Default;

        static GearItem Item(string slot, int tier, int enh = 0, int stars = 1, GearRefineGrade refine = GearRefineGrade.Normal, int dur = -1, string set = null)
            => new GearItem("i:" + slot + ":" + tier + ":" + enh + ":" + stars + ":" + (int)refine, Cat.GetSlot(slot), tier, Cat.MaxDurabilityFor(Cat.GetSlot(slot).Group), set, enh, stars, refine, dur);

        [Fact]
        public void BrokenItemContributesNoScore()
        {
            var broken = Item("monster.weapon", 3, dur: 0);
            Assert.True(broken.IsBroken);
            Assert.Equal(0, GearScore.Score(broken, Cat));
        }

        [Fact]
        public void EnhanceStarsRefineIncreaseScore()
        {
            double baseScore = GearScore.Score(Item("monster.weapon", 2), Cat);
            double enhanced = GearScore.Score(Item("monster.weapon", 2, enh: 10), Cat);
            double starred = GearScore.Score(Item("monster.weapon", 2, stars: 5), Cat);
            double refined = GearScore.Score(Item("monster.weapon", 2, refine: GearRefineGrade.Mythic), Cat);
            Assert.True(enhanced > baseScore);
            Assert.True(starred > baseScore);
            Assert.True(refined > baseScore);
        }

        [Fact]
        public void EffectiveStatsScaleByEnhanceStarAndRefine()
        {
            var item = Item("monster.weapon", 2, enh: 4, stars: 3, refine: GearRefineGrade.Rare);
            var stats = GearScore.EffectiveStats(item, Cat);
            var baseStats = Cat.GetBaseStats("monster.weapon", 2);
            double starMult = 1 + Cat.GetStarPct(3);
            double refineMult = Cat.GetRefineMultiplier((int)GearRefineGrade.Rare);
            double expectedAttack = (baseStats.Attack * (1 + 4 * Cat.Config.EnhancePerLevelFraction)) * starMult * refineMult;
            Assert.Equal(expectedAttack, stats.Attack, 9);
        }

        [Fact]
        public void SetBonusAppliesAtTwoFourSix()
        {
            var catalog = Cat;
            var set = catalog.GetSet("monster_set_alpha");
            var weapon = Item("monster.weapon", 1, set: "monster_set_alpha");
            var armor = Item("monster.armor", 1, set: "monster_set_alpha");
            var collar = Item("monster.collar", 1, set: "monster_set_alpha");
            var bell = Item("monster.bell", 1, set: "monster_set_alpha");
            var hooves = Item("monster.hooves", 1, set: "monster_set_alpha");
            var core = Item("monster.core", 1, set: "monster_set_alpha");

            var loadout = new GearLoadout();
            loadout.Equip(weapon); loadout.Equip(armor);
            var two = GearLoadout.SetBonus(loadout.Equipped, catalog);
            Assert.Equal(set.Bonuses.First(b => b.RequiredCount == 2).Bonus.Attack, two.Attack, 9);

            loadout.Equip(collar); loadout.Equip(bell);
            var four = GearLoadout.SetBonus(loadout.Equipped, catalog);
            Assert.Equal(set.Bonuses.First(b => b.RequiredCount == 4).Bonus.Defense, four.Defense, 9);

            loadout.Equip(hooves); loadout.Equip(core);
            var six = GearLoadout.SetBonus(loadout.Equipped, catalog);
            Assert.Equal(set.Bonuses.First(b => b.RequiredCount == 2).Bonus.Attack + set.Bonuses.First(b => b.RequiredCount == 6).Bonus.Attack, six.Attack, 9);
        }

        [Fact]
        public void LoadoutEnforcesOneItemPerSlot()
        {
            var loadout = new GearLoadout();
            loadout.Equip(Item("monster.weapon", 1));
            var second = Item("monster.weapon", 2);
            Assert.Throws<InvalidOperationException>(() => loadout.Equip(second));
            Assert.Single(loadout.Equipped);
        }

        [Fact]
        public void LoadoutAggregatesAcrossSlots()
        {
            var loadout = new GearLoadout();
            loadout.Equip(Item("monster.weapon", 2));
            loadout.Equip(Item("monster.armor", 2));
            var total = GearLoadout.TotalStats(loadout.Equipped, Cat);
            var weapon = GearScore.EffectiveStats(loadout.Equipped.First(x => x.Slot.StatKind == GearStatKind.Attack), Cat);
            var armor = GearScore.EffectiveStats(loadout.Equipped.First(x => x.Slot.StatKind == GearStatKind.Defense), Cat);
            Assert.Equal(weapon.Attack + armor.Attack, total.Attack, 9);
            Assert.Equal(weapon.Defense + armor.Defense, total.Defense, 9);
        }
    }
}
