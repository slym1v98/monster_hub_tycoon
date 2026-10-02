using Game.Domain;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class HubWorldConsumableTests
    {
        [Fact]
        public void RejectsInvalidTrainerProductQuantityAndUnlistedProductWithoutMutation()
        {
            var world = new HubWorld(new SimConfig { TrainerCount = 1, StartTreasury = 1000, StartTrainerGold = 100 }, 1);
            long gold = world.Trainers[0].Gold;
            long treasury = world.Treasury;
            Assert.False(world.PurchaseProduct(-1, "potion", 1).Ok);
            Assert.False(world.PurchaseProduct(0, "unknown", 1).Ok);
            Assert.False(world.PurchaseProduct(0, "potion", 0).Ok);
            Assert.False(world.PurchaseProduct(0, "monster_gear", 1).Ok);
            Assert.False(world.PurchaseProduct(0, "potion", 1).Ok); // quầy chưa có tồn kho
            Assert.Equal(gold, world.Trainers[0].Gold);
            Assert.Equal(treasury, world.Treasury);
            Assert.False(world.Trainers[0].Products.ContainsKey(new Game.Domain.Materials.ProductId("potion")));
            Assert.Empty(world.SupplyTransactions);
        }

        [Fact]
        public void ProductStockSnapshotContainsAvailableStockAndConfiguredPrices()
        {
            var world = new HubWorld(new SimConfig { TrainerCount = 1 }, 2);
            Assert.Equal(0, world.ConsumableStock.Products[new Game.Domain.Materials.ProductId("potion")].Available);
            Assert.Equal(10, world.ConsumableStock.Products[new Game.Domain.Materials.ProductId("potion")].UnitPrice);
        }

        [Fact]
        public void PurchaseTransfersGoldStockAndLedgerOnlyForWholeOrder()
        {
            var config = new SimConfig { TrainerCount = 1, StartTreasury = 5000, StartTrainerGold = 100,
                MerchantSettings = new Game.Domain.Supply.MerchantConfig(100, 0.05, 0.10, 10,
                    operatingCostPerTrip: 5, startingCash: 1000, routeCycleMinutes: 60,
                    replacementDelayMinutes: 60, maximumWaitMinutes: 120),
                ProductionSettings = new Game.Domain.Production.ProductionConfig(defaultJobDurationMinutes: 5) };
            var world = new HubWorld(config, 3);
            Assert.True(world.SetBuyRequest("herb_tier_1", 1, 10).Ok);
            Assert.True(world.SetProductionTarget("potion", 1).Ok);
            for (int i = 0; i < 300 && world.ConsumableStock.Products[new Game.Domain.Materials.ProductId("potion")].Available == 0; i++) world.RunFor(10);
            Assert.True(world.ConsumableStock.Products[new Game.Domain.Materials.ProductId("potion")].Available > 0);
            long treasuryBefore = world.Treasury;
            long trainerGoldBefore = world.Trainers[0].Gold;
            var events = new System.Collections.Generic.List<IDomainEvent>(); world.EventRaised += events.Add;
            Assert.False(world.PurchaseProduct(0, "potion", 2).Ok); // only one unit is stocked
            Assert.Equal(trainerGoldBefore, world.Trainers[0].Gold);
            Assert.True(world.PurchaseProduct(0, "potion", 1).Ok);
            Assert.Equal(trainerGoldBefore - 10, world.Trainers[0].Gold);
            Assert.Equal(treasuryBefore + 10, world.Treasury);
            Assert.Equal(1, world.Trainers[0].Products[new Game.Domain.Materials.ProductId("potion")]);
            Assert.Equal(0, world.ConsumableStock.Products[new Game.Domain.Materials.ProductId("potion")].Available);
            Assert.Contains(events, e => e is ProductPurchased p && p.ProductId == "potion" && p.Units == 1 && p.TotalPaid == 10);
            Assert.Contains(world.SupplyTransactions, tx => tx.Reason == "consumable stall purchase" && tx.Gross == 10);
        }
    }
}
