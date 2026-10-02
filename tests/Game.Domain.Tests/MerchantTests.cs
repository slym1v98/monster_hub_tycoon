using System;
using Game.Domain.Materials;
using Game.Domain.Supply;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class MerchantTests
    {
        private static readonly MaterialId Ore = MaterialId.For(MaterialFamily.Ore, 1);

        [Fact]
        public void SpreadIsMonotonicAndBoundedByFiveAndTenPercent()
        {
            var merchant = new Merchant("m1", 10000, new MerchantConfig(1000, 0.05, 0.10, 20), new MoneyLedger());
            Assert.Equal(0.05, merchant.BuyDiscountForLot(1), 8);
            Assert.InRange(merchant.BuyDiscountForLot(10), 0.05, 0.10);
            Assert.Equal(0.10, merchant.BuyDiscountForLot(20), 8);
            Assert.True(merchant.BuyDiscountForLot(10) > merchant.BuyDiscountForLot(2));
            Assert.Equal(merchant.BuyDiscountForLot(100), merchant.SellMarkupForLot(100));
        }

        [Fact]
        public void MerchantPurchasesOnlyWithinCashAndCapacityAndKeepsRemainderWithTrainer()
        {
            var ledger = new MoneyLedger();
            var merchant = new Merchant("m1", 250, new MerchantConfig(3, 0.05, 0.10, 10), ledger);
            var sale = merchant.BuyFromTrainer("trainer:1", Ore, 10, 100, 0.20);
            Assert.Equal(2, sale.MerchantUnits);
            Assert.Equal(8, sale.UnsoldUnits);
            Assert.InRange(sale.NetToSeller, 1, sale.Gross);
            Assert.Equal(2, merchant.LoadUnits);
            Assert.True(merchant.Cash >= 0);
            Assert.Equal(0, ledger.TotalBalance);
        }

        [Fact]
        public void MerchantSellsOnlyRequestedStockAtMarkupAndKeepsUnsoldInventory()
        {
            var ledger = new MoneyLedger();
            var config = new MerchantConfig(20, 0.05, 0.10, 20);
            var merchant = new Merchant("m1", 1000, config, ledger);
            var station = new Station(10000, 0.20, ledger);
            var trainerSale = merchant.BuyFromTrainer("trainer:1", Ore, 4, 100, 0.20);
            Assert.Equal(10000 + trainerSale.Tax, station.Treasury);
            merchant.DepartForStation();
            Assert.True(merchant.ArriveAtStation());
            var request = new BuyRequest(Ore, 2, 100);
            var sale = merchant.SellToStation(station, Ore, request);
            Assert.Equal(2, sale.StationUnits);
            Assert.Equal(2, sale.UnsoldUnits);
            Assert.True(sale.Gross > 200);
            Assert.Equal(2, merchant.LoadUnits);
            Assert.Equal(2, station.Stock.Get(new InventoryItem(Ore)).Available);
            Assert.Equal(0, ledger.TotalBalance);
        }

        [Fact]
        public void MerchantLeavesGoodsUnsoldWhenStationMarkupCannotFitInLong()
        {
            var ledger = new MoneyLedger();
            var merchant = new Merchant("m1", 1000,
                new MerchantConfig(20, 0.05, 0.10, 20, operatingCostPerTrip: 0), ledger);
            merchant.BuyFromTrainer("trainer:1", Ore, 1, 10, 0.20);
            merchant.DepartForStation();
            Assert.True(merchant.ArriveAtStation());
            var station = new Station(long.MaxValue, 0.20, new MoneyLedger());
            var before = ledger.Transactions.Count;

            var result = merchant.SellToStation(station, Ore, new BuyRequest(Ore, 1, long.MaxValue));

            Assert.Equal(1, result.UnsoldUnits);
            Assert.Equal(1, merchant.LoadUnits);
            Assert.Equal(0, station.Stock.Get(new InventoryItem(Ore)).Available);
            Assert.Equal(before, ledger.Transactions.Count);
        }

        [Fact]
        public void MerchantPurchaseRespectsTreasuryTaxHeadroomBeforeRecordingSale()
        {
            var ledger = new MoneyLedger();
            var merchant = new Merchant("m1", long.MaxValue,
                new MerchantConfig(10, 0.05, 0.10, 10, operatingCostPerTrip: 0), ledger);

            var result = merchant.BuyFromTrainer("trainer:1", Ore, 1, 100, 1.0, maximumTax: 0);

            Assert.Equal(1, result.UnsoldUnits);
            Assert.Equal(0, merchant.LoadUnits);
            Assert.Empty(ledger.Transactions);
        }

        [Fact]
        public void BankruptMerchantKeepsGoodsAndFleetReplacesItAfterDelay()
        {
            var ledger = new MoneyLedger();
            var config = new MerchantConfig(20, 0.05, 0.10, 10, operatingCostPerTrip: 10);
            var fleet = new MerchantFleet(5, 30, config, ledger);
            var oldMerchant = fleet.Current;
            oldMerchant.Goods.Add(new InventoryItem(Ore), 3);
            oldMerchant.DepartForStation();
            Assert.False(oldMerchant.ArriveAtStation());
            Assert.True(oldMerchant.IsBankrupt);
            Assert.False(fleet.AdvanceTo(100));
            Assert.Equal(130, fleet.ReplacementMinute);
            Assert.False(fleet.AdvanceTo(129));
            Assert.True(fleet.AdvanceTo(130));
            Assert.NotSame(oldMerchant, fleet.Current);
            Assert.Equal(3, Assert.Single(fleet.FormerMerchants).Goods.Get(new InventoryItem(Ore)).Available);
        }

        [Fact]
        public void MerchantCannotTradeAwayFromRouteLocation()
        {
            var merchant = new Merchant("m1", 100, MerchantConfig.Prototype, new MoneyLedger());
            Assert.Throws<InvalidOperationException>(() => merchant.SellToStation(new Station(100, .2, new MoneyLedger()), Ore, new BuyRequest(Ore, 1, 1)));
        }
    }
}
