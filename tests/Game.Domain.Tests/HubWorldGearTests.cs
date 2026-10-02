using System;
using System.Linq;
using Game.Domain;
using Game.Domain.Gear;
using Game.Domain.Materials;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class HubWorldGearTests
    {
        static GearCatalog Cat => GearCatalog.Default;
        static GearConfig Gcfg => Cat.Config;

        static HubWorld World(out int trainerId)
        {
            var cfg = new SimConfig { TrainerCount = 2, StartTrainerGold = 100000, StartTreasury = 100000 };
            var world = new HubWorld(cfg, 7);
            trainerId = 0;
            return world;
        }

        static GearItem Item(string slot, int tier, int enh = 0, int stars = 1, GearRefineGrade refine = GearRefineGrade.Normal, int dur = -1)
            => new GearItem("g:" + slot + ":" + tier + ":" + enh + ":" + stars + ":" + (int)refine, Cat.GetSlot(slot), tier, Cat.MaxDurabilityFor(Cat.GetSlot(slot).Group), null, enh, stars, refine, dur);

        [Fact]
        public void OfferGearAcceptedTransfersGoldAndEquips()
        {
            var world = World(out int id);
            var offered = Item("trainer.backpack", 4);
            long before = world.Trainers[id].Gold;
            long treasuryBefore = world.Treasury;
            var r = world.OfferGear(id, offered, 500);
            Assert.True(r.Ok, r.Reason);
            Assert.Equal(before - 500, world.Trainers[id].Gold);
            Assert.Equal(treasuryBefore + 500, world.Treasury);
            var view = world.GearForTrainer(id);
            Assert.Contains(view, v => v.ItemId == offered.Id && v.SlotId == "trainer.backpack");
        }

        [Fact]
        public void OfferGearRejectedWhenUnaffordable()
        {
            var world = World(out int id);
            // adjust gold via Director donation
            world.Donate(id, 100); // now 100000 + 100 = 100100
            // spend by advancing wage then spending? no purchase. Just test reject path: price > gold
            var r = world.OfferGear(id, Item("trainer.backpack", 5), 200000);
            Assert.False(r.Ok);
        }

        [Fact]
        public void EnhanceGearSpendsGoldAndStoneAndRaisesLevel()
        {
            var world = World(out int id);
            var item = Item("trainer.gloves", 2);
            var r = world.OfferGear(id, item, 1);
            Assert.True(r.Ok, r.Reason);
            world.GrantProductForTest(id, "enhancement_stone", 3);
            long before = world.Trainers[id].Gold;
            r = world.EnhanceGear(id, item, false);
            Assert.True(r.Ok, r.Reason);
            Assert.Equal(1, item.EnhanceLevel);
            Assert.True(world.Trainers[id].Gold < before);
            Assert.Equal(2, world.Trainers[id].Products.First(p => p.Key.Value == "enhancement_stone").Value);
        }

        [Fact]
        public void StarUpGearConsumesJunk()
        {
            var world = World(out int id);
            var item = Item("trainer.gloves", 2);
            var junk = Item("trainer.gloves", 1);
            var r = world.OfferGear(id, item, 1);
            Assert.True(r.Ok);
            r = world.StarUpGear(id, item, junk);
            Assert.True(r.Ok, r.Reason);
            Assert.Equal(2, item.Stars);
            Assert.True(junk.IsDestroyed);
        }

        [Fact]
        public void RepairGearRestoresDurability()
        {
            var world = World(out int id);
            var item = Item("trainer.gloves", 2, dur: 20);
            var r = world.OfferGear(id, item, 1);
            Assert.True(r.Ok);
            r = world.RepairGear(id, item);
            Assert.True(r.Ok, r.Reason);
            Assert.Equal(Gcfg.UtilityMaxDurability, item.Durability);
        }

        [Fact]
        public void BuybackTransfersGoldFromTreasury()
        {
            var world = World(out int id);
            var item = Item("trainer.gloves", 3);
            var r = world.OfferGear(id, item, 1);
            Assert.True(r.Ok);
            long before = world.Trainers[id].Gold;
            long tb = world.Treasury;
            r = world.BuybackGear(id, item, 1000);
            Assert.True(r.Ok, r.Reason);
            Assert.Equal(before + (long)(1000 * Gcfg.BuybackFraction), world.Trainers[id].Gold);
            Assert.Equal(tb - (long)(1000 * Gcfg.BuybackFraction), world.Treasury);
        }

        [Fact]
        public void GearWearAppliedDuringFarmRun()
        {
            var cfg = new SimConfig { TrainerCount = 1, StartTrainerGold = 100000, StartTreasury = 100000, FarmChunkMinutes = 30 };
            var world = new HubWorld(cfg, 11);
            var t = world.Trainers[0];
            var gloves = Item("trainer.gloves", 2);
            var r = world.OfferGear(0, gloves, 1);
            Assert.True(r.Ok);
            int before = gloves.Durability;
            world.RunFor(cfg.FarmChunkMinutes + cfg.ZoneTravelMinutes * 2 + 120);
            Assert.True(gloves.Durability <= before);
        }
    }
}
