using System;
using Game.Domain.Materials;
using Game.Domain.Supply;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class SupplyLedgerTests
    {
        private static readonly InventoryItem Ore = new InventoryItem(MaterialId.For(MaterialFamily.Ore, 1));
        private static readonly InventoryItem Water = new InventoryItem(new ProductId("distilled_water"));

        [Fact]
        public void ReservationReducesAvailableAndReleaseRestoresIt()
        {
            var inventory = new Inventory();
            inventory.Add(Ore, 12);

            inventory.Reserve(Ore, 7);

            Assert.Equal(5, inventory.Get(Ore).Available);
            Assert.Equal(7, inventory.Get(Ore).Reserved);
            inventory.Release(Ore, 7);
            Assert.Equal(12, inventory.Get(Ore).Available);
            Assert.Equal(0, inventory.Get(Ore).Reserved);
        }

        [Fact]
        public void InventoryRejectsOversellNegativeAndOverflowWithoutPartialMutation()
        {
            var inventory = new Inventory();
            inventory.Add(Ore, int.MaxValue);
            Assert.Throws<OverflowException>(() => inventory.Add(Ore, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Add(Ore, -1));
            inventory.Remove(Ore, 1);
            Assert.Throws<InvalidOperationException>(() => inventory.Remove(Ore, int.MaxValue));
            Assert.Equal(int.MaxValue - 1, inventory.Get(Ore).Available);
        }

        [Fact]
        public void ProductionReservationConsumesInputsAndAddsOutputsExactlyOnce()
        {
            var inventory = new Inventory();
            inventory.Add(Ore, 10);
            inventory.Reserve(Ore, 4);
            inventory.BeginProduction(Ore, 4);
            inventory.CompleteProduction(Ore, 4, Water, 2);
            Assert.Equal(6, inventory.Get(Ore).Available);
            Assert.Equal(0, inventory.Get(Ore).Reserved);
            Assert.Equal(2, inventory.Get(Water).Available);
            Assert.Throws<InvalidOperationException>(() => inventory.CompleteProduction(Ore, 4, Water, 2));
        }

        [Fact]
        public void MultiInputReservationIsAtomicWhenOneIngredientIsMissing()
        {
            var wood = new InventoryItem(MaterialId.For(MaterialFamily.Wood, 1));
            var inventory = new Inventory();
            inventory.Add(Ore, 3);
            Assert.Throws<InvalidOperationException>(() => inventory.ReserveMany(new[]
            {
                new InventoryItemQuantity(Ore, 2), new InventoryItemQuantity(wood, 1)
            }));
            Assert.Equal(3, inventory.Get(Ore).Available);
            Assert.Equal(0, inventory.Get(Ore).Reserved);
        }

        [Fact]
        public void MoneyLedgerReconcilesGrossTaxAndNetAndRejectsInvalidEntries()
        {
            var ledger = new MoneyLedger();
            ledger.Record("merchant", "trainer:1", "hub:tax", 100, 7, "trainer sale");
            Assert.Equal(93, ledger.BalanceOf("trainer:1"));
            Assert.Equal(-100, ledger.BalanceOf("merchant"));
            Assert.Equal(7, ledger.BalanceOf("hub:tax"));
            Assert.Equal(0, ledger.TotalBalance);
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Record("a", "b", "tax", -1, 0, "bad"));
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Record("a", "b", "tax", 1, 2, "bad"));
            Assert.Equal(0, ledger.BalanceOf("a"));
        }
    }
}
