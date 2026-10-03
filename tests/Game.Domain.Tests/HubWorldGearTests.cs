using System;
using System.Linq;
using Game.Domain;
using Game.Domain.Gear;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class HubWorldGearTests
    {
        static GearCatalog Cat => GearCatalog.Default;
        static GearConfig Gcfg => Cat.Config;

        static HubWorld World(out int trainerId)
        {
            var cfg = new SimConfig
            {
                TrainerCount = 2, StartTrainerGold = 100000, StartTreasury = 100000,
                StartTownHallLevel = 11, UnlockedZoneIds = new[] { "zone_1", "zone_2", "zone_3" },
                StartingFacilityLevels = new System.Collections.Generic.Dictionary<string, int>
                {
                    ["monster_forge"] = 1,
                    ["textile_workshop"] = 1,
                    ["jeweler"] = 1
                }
            };
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
        public void MonsterGearCanBeOfferedToEachRosterMemberAndViewsIdentifyOwner()
        {
            var world = World(out int id);
            var monster = world.MonstersForTrainer(id).First();
            var item = Item("monster.weapon", 2);
            Assert.True(world.OfferGearForMonster(id, new MonsterId(monster.Id), item, 1).Ok);
            var views = world.GearForTrainer(id).Where(v => v.SlotId == "monster.weapon").ToArray();
            Assert.Single(views);
            Assert.Equal("monster:" + monster.Id, views[0].OwnerId);
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
            r = world.OfferGearForSacrifice(id, junk, 1);
            Assert.True(r.Ok, r.Reason);
            long goldBefore = world.Trainers[id].Gold;
            long treasuryBefore = world.Treasury;
            r = world.StarUpGear(id, item, junk);
            Assert.True(r.Ok, r.Reason);
            Assert.Equal(goldBefore - Gcfg.StarAttemptCost(2), world.Trainers[id].Gold);
            Assert.Equal(treasuryBefore + Gcfg.StarAttemptCost(2), world.Treasury);
            Assert.True(junk.IsDestroyed);
            Assert.Empty(world.GearStorageForTrainer(id));
        }

        [Fact]
        public void StarUpRejectsUnownedSacrificeItemWithoutCharging()
        {
            var world = World(out int id);
            var target = Item("trainer.gloves", 2);
            var junk = Item("trainer.gloves", 1);
            Assert.True(world.OfferGear(id, target, 1).Ok);
            long goldBefore = world.Trainers[id].Gold;
            Assert.False(world.StarUpGear(id, target, junk).Ok);
            Assert.Equal(1, target.Stars);
            Assert.False(junk.IsDestroyed);
            Assert.Equal(goldBefore, world.Trainers[id].Gold);
        }

        [Fact]
        public void GearUpgradeAndRepairCommandsRequireTheirOperatingWorkshop()
        {
            var world = new HubWorld(new SimConfig
            {
                TrainerCount = 1,
                StartTrainerGold = 100_000,
                StartTreasury = 100_000,
                StartTownHallLevel = 11,
                UnlockedZoneIds = new[] { "zone_1", "zone_2", "zone_3" }
            }, 71);

            var utility = Item("trainer.gloves", 2, dur: 1);
            Assert.True(world.OfferGear(0, utility, 1).Ok);
            world.GrantProductForTest(0, "enhancement_stone", 1);
            Assert.False(world.EnhanceGear(0, utility, false).Ok);
            Assert.False(world.RepairGear(0, utility).Ok);

            var target = Item("trainer.backpack", 2);
            var junk = Item("trainer.backpack", 1);
            Assert.True(world.OfferGear(0, target, 1).Ok);
            Assert.True(world.OfferGearForSacrifice(0, junk, 1).Ok);
            Assert.False(world.StarUpGear(0, target, junk).Ok);

            var aura = Item("aura.whistle", 2);
            Assert.True(world.OfferGear(0, aura, 1).Ok);
            world.GrantProductForTest(0, "world_boss_crystal", Gcfg.RefineCrystalCost);
            world.GrantProductForTest(0, "distilled_water", Gcfg.RefineWaterCost);
            Assert.False(world.RefineGear(0, aura).Ok);

            var textileOnly = new HubWorld(new SimConfig
            {
                TrainerCount = 1,
                StartTrainerGold = 100_000,
                StartTreasury = 100_000,
                StartTownHallLevel = 5,
                UnlockedZoneIds = new[] { "zone_1" },
                StartingFacilityLevels = new System.Collections.Generic.Dictionary<string, int> { ["textile_workshop"] = 1 }
            }, 72);
            var utilityForEnhance = Item("trainer.gloves", 2);
            Assert.True(textileOnly.OfferGear(0, utilityForEnhance, 1).Ok);
            textileOnly.GrantProductForTest(0, "enhancement_stone", 1);
            Assert.Equal("facility.unavailable", textileOnly.EnhanceGear(0, utilityForEnhance, false).Reason);
        }

        [Fact]
        public void SacrificeOfferRequiresACompatibleUnmaxedGearSlot()
        {
            var world = World(out int id);
            var junk = Item("trainer.gloves", 1);
            Assert.False(world.OfferGearForSacrifice(id, junk, 10).Ok);
            var target = Item("trainer.gloves", 2);
            Assert.True(world.OfferGear(id, target, 1).Ok);
            Assert.True(world.OfferGearForSacrifice(id, junk, 10).Ok);
            Assert.Contains(world.GearStorageForTrainer(id), x => x.ItemId == junk.Id);
        }

        [Fact]
        public void RefineAttemptConsumesMaterialsAndGoldEvenWhenRollFails()
        {
            var world = World(out int id);
            var item = Item("trainer.gloves", 2);
            Assert.True(world.OfferGear(id, item, 1).Ok);
            GearRefined refined = null;
            world.EventRaised += e => { if (e is GearRefined value) refined = value; };
            world.GrantProductForTest(id, "world_boss_crystal", 1);
            world.GrantProductForTest(id, "distilled_water", Gcfg.RefineWaterCost);
            long goldBefore = world.Trainers[id].Gold;
            long treasuryBefore = world.Treasury;
            Assert.True(world.RefineGear(id, item).Ok);
            Assert.Equal(goldBefore - Gcfg.RefineAttemptCost(1), world.Trainers[id].Gold);
            Assert.Equal(treasuryBefore + Gcfg.RefineAttemptCost(1), world.Treasury);
            Assert.DoesNotContain(world.Trainers[id].Products, pair => pair.Key.Value == "world_boss_crystal");
            Assert.NotNull(refined);
            Assert.Equal(refined.Success, item.Refine == GearRefineGrade.Refined);
        }

        [Fact]
        public void EnhancementAutomaticallyUsesAvailableTrainerCredit()
        {
            var cfg = new SimConfig { TrainerCount = 1, StartTrainerGold = 10, StartTreasury = 100000,
                StartTownHallLevel = 5, UnlockedZoneIds = new[] { "zone_1" },
                StartingFacilityLevels = new System.Collections.Generic.Dictionary<string, int> { ["monster_forge"] = 1 } };
            var world = new HubWorld(cfg, 5);
            var item = Item("trainer.gloves", 2);
            Assert.True(world.OfferGear(0, item, 1).Ok);
            world.GrantProductForTest(0, "enhancement_stone", 1);
            var result = world.EnhanceGear(0, item, false);
            Assert.True(result.Ok, result.Reason);
            Assert.True(world.Trainers[0].HubLoanBalance > 0);
            Assert.DoesNotContain(world.Trainers[0].Products, p => p.Key == new ProductId("enhancement_stone"));
        }

        [Fact]
        public void ProtectionCharmIsConsumedOnlyWhenItPreventsBreak()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                var cfg = new SimConfig { TrainerCount = 1, StartTrainerGold = 100000, StartTreasury = 100000,
                    StartTownHallLevel = 5, UnlockedZoneIds = new[] { "zone_1" },
                    StartingFacilityLevels = new System.Collections.Generic.Dictionary<string, int> { ["monster_forge"] = 1 } };
                var world = new HubWorld(cfg, seed);
                var item = Item("trainer.gloves", 2, enh: 10);
                Assert.True(world.OfferGear(0, item, 1).Ok);
                world.GrantProductForTest(0, "enhancement_stone", 1);
                world.GrantProductForTest(0, "protection_charm", 1);
                GearEnhanced enhanced = null;
                world.EventRaised += e => { if (e is GearEnhanced value) enhanced = value; };
                Assert.True(world.EnhanceGear(0, item, useProtectionCharm: true).Ok);
                if (enhanced.CharmUsed)
                {
                    Assert.False(world.Trainers[0].Products.TryGetValue(new ProductId("protection_charm"), out int charms));
                    Assert.Contains(world.GearForTrainer(0), view => view.ItemId == item.Id && !view.IsBroken);
                    return;
                }
            }
            Assert.Fail("No deterministic seed produced a protected break in 1000 attempts.");
        }

        [Fact]
        public void BrokenEnhancementRemovesItemFromItsEquipmentSlot()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                var cfg = new SimConfig { TrainerCount = 1, StartTrainerGold = 100000, StartTreasury = 100000,
                    StartTownHallLevel = 5, UnlockedZoneIds = new[] { "zone_1" },
                    StartingFacilityLevels = new System.Collections.Generic.Dictionary<string, int> { ["monster_forge"] = 1 } };
                var world = new HubWorld(cfg, seed);
                var item = Item("trainer.gloves", 2, enh: 10);
                Assert.True(world.OfferGear(0, item, 1).Ok);
                world.GrantProductForTest(0, "enhancement_stone", 1);
                GearEnhanced enhanced = null;
                world.EventRaised += e => { if (e is GearEnhanced value) enhanced = value; };
                Assert.True(world.EnhanceGear(0, item, useProtectionCharm: false).Ok);
                if (enhanced.Broke)
                {
                    Assert.True(item.IsDestroyed);
                    Assert.DoesNotContain(world.GearForTrainer(0), view => view.ItemId == item.Id);
                    return;
                }
            }
            Assert.Fail("No deterministic seed produced a broken enhancement in 1000 attempts.");
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
            var replacement = Item("trainer.gloves", 4);
            Assert.True(world.OfferGear(id, replacement, 2).Ok);
            long before = world.Trainers[id].Gold;
            long tb = world.Treasury;
            r = world.BuybackGear(id, item, 1000);
            Assert.True(r.Ok, r.Reason);
            Assert.Equal(before + (long)(1000 * Gcfg.BuybackFraction), world.Trainers[id].Gold);
            Assert.Equal(tb - (long)(1000 * Gcfg.BuybackFraction), world.Treasury);
            r = world.BuybackGear(id, item, 1000);
            Assert.False(r.Ok);
            Assert.Equal(before + (long)(1000 * Gcfg.BuybackFraction), world.Trainers[id].Gold);
        }

        [Fact]
        public void BuybackRejectsEquippedOrForeignGear()
        {
            var world = World(out int id);
            var item = Item("trainer.gloves", 3);
            Assert.True(world.OfferGear(id, item, 1).Ok);
            Assert.False(world.BuybackGear(id, item, 1000).Ok);
            Assert.False(world.RepairGear(id, Item("trainer.gloves", 4)).Ok);
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
            var wearEvents = new System.Collections.Generic.List<GearDurabilityChanged>();
            world.EventRaised += e => { if (e is GearDurabilityChanged value) wearEvents.Add(value); };
            int before = gloves.Durability;
            world.RunFor(cfg.FarmChunkMinutes + cfg.ZoneTravelMinutes * 2 + 120);
            Assert.True(gloves.Durability <= before);
            Assert.Contains(wearEvents, x => x.ItemId == gloves.Id && x.Cause == "FarmTime");
        }
    }
}
