using System;
using System.Linq;
using Game.Domain.Gear;
using Game.Domain;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class GearWearTests
    {
        static GearCatalog Cat => GearCatalog.Default;
        static GearConfig Cfg => Cat.Config;
        static GearItem MonsterWeapon(int dur = -1) => new GearItem("mw", Cat.GetSlot("monster.weapon"), 2, Cat.MaxDurabilityFor(GearGroup.MonsterCombat), null, 0, 1, GearRefineGrade.Normal, dur);
        static GearItem UtilityGloves(int dur = -1) => new GearItem("ug", Cat.GetSlot("trainer.gloves"), 2, Cat.MaxDurabilityFor(GearGroup.TrainerUtility), null, 0, 1, GearRefineGrade.Normal, dur);
        static GearItem AuraGoggles() => new GearItem("ag", Cat.GetSlot("aura.goggles"), 2, Cat.MaxDurabilityFor(GearGroup.Aura), null);

        [Fact]
        public void MonsterGearWearsOnActionAndHit()
        {
            var weapon = MonsterWeapon();
            var before = weapon.Durability;
            GearWear.OnMonsterAction(weapon, Cfg);
            Assert.Equal(before - Cfg.MonsterWearPerAction, weapon.Durability);

            var armor = MonsterWeapon();
            var before2 = armor.Durability;
            GearWear.OnMonsterHit(armor, Cfg);
            Assert.Equal(before2 - Cfg.MonsterWearPerHit, armor.Durability);
        }

        [Fact]
        public void UtilityGearWearsOverFarmTime()
        {
            var gloves = UtilityGloves();
            var before = gloves.Durability;
            GearWear.OnFarmMinutes(gloves, minutes: 60, weatherFactor: 1, Cfg);
            int expectedWear = (int)Math.Ceiling(Cfg.UtilityWearPerFarmHour * 1.0); // 60 phút = 1 giờ
            Assert.Equal(before - expectedWear, gloves.Durability);
        }

        [Fact]
        public void AuraGearNeverWears()
        {
            var goggles = AuraGoggles();
            var before = goggles.Durability;
            GearWear.OnMonsterAction(goggles, Cfg);
            GearWear.OnFarmMinutes(goggles, 1440, 1.0, Cfg);
            Assert.Equal(before, goggles.Durability);
        }

        [Fact]
        public void WearNeverGoesNegativeAndBrokenItemDisables()
        {
            var weapon = MonsterWeapon(2);
            GearWear.OnMonsterAction(weapon, Cfg); // 2 - 1 = 1
            GearWear.OnMonsterAction(weapon, Cfg); // 1 - 1 = 0
            GearWear.OnMonsterAction(weapon, Cfg); // 0, không âm
            Assert.Equal(0, weapon.Durability);
            Assert.True(weapon.IsBroken);
            // broken item không còn đóng góp chỉ số
            var stats = GearScore.EffectiveStats(weapon, Cat);
            Assert.Equal(0, stats.Attack);
        }

        [Fact]
        public void WearIsDeterministic()
        {
            var item1 = MonsterWeapon(10);
            var item2 = MonsterWeapon(10);
            for (int i = 0; i < 20; i++) GearWear.OnMonsterAction(item1, Cfg);
            for (int i = 0; i < 20; i++) GearWear.OnMonsterAction(item2, Cfg);
            Assert.Equal(item1.Durability, item2.Durability);
        }
    }
}
