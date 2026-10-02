using System;
using Game.Domain.Materials;
using Game.Domain.Supply;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class StationTests
    {
        private static readonly MaterialId Ore = MaterialId.For(MaterialFamily.Ore, 1);

        [Fact]
        public void RequestDeficitSubtractsStationReservedAndIncomingStock()
        {
            var request = new BuyRequest(Ore, targetStock: 100, bidPrice: 4, reservedForProduction: 15, incomingMerchantUnits: 20);
            Assert.Equal(45, request.Deficit(stationStock: 20));
            Assert.Equal(0, request.Deficit(stationStock: 90));
            Assert.Equal(0, new BuyRequest(Ore, 100, 4, enabled: false).Deficit(0));
        }

        [Fact]
        public void StationBuysOnlyUpToDeficitAndPostsTaxSeparately()
        {
            var ledger = new MoneyLedger();
            var station = new Station(1000, 0.20, ledger);
            var request = new BuyRequest(Ore, 10, 10);
            var sale = station.BuyFromTrainer("trainer:1", Ore, 14, request);
            Assert.Equal(10, sale.StationUnits);
            Assert.Equal(0, sale.MerchantUnits);
            Assert.Equal(4, sale.UnsoldUnits);
            Assert.Equal(100, sale.Gross);
            Assert.Equal(20, sale.Tax);
            Assert.Equal(80, sale.NetToSeller);
            Assert.Equal(10, station.Stock.Get(new InventoryItem(Ore)).Available);
            Assert.Equal(920, station.Treasury);
            Assert.Equal(0, ledger.TotalBalance);
            Assert.Equal(-80, ledger.BalanceOf("hub:treasury"));
            Assert.Equal(80, ledger.BalanceOf("trainer:1"));
        }

        [Fact]
        public void StationHonorsTreasuryAffordabilityAndLeavesUnsoldGoods()
        {
            var station = new Station(25, 0.1, new MoneyLedger());
            var sale = station.BuyFromTrainer("trainer:1", Ore, 10, new BuyRequest(Ore, 10, 10));
            Assert.Equal(2, sale.StationUnits);
            Assert.Equal(8, sale.UnsoldUnits);
            Assert.Equal(7, station.Treasury);
            Assert.Equal(2, station.Stock.Get(new InventoryItem(Ore)).Available);
        }

        [Fact]
        public void MerchantMarkupOverflowLeavesGoodsUnsoldInsteadOfThrowing()
        {
            var ledger = new MoneyLedger();
            var station = new Station(long.MaxValue, 0.1, ledger);
            var request = new BuyRequest(Ore, 1, long.MaxValue);

            var result = station.BuyFromMerchant("merchant:1", Ore, 1, request, 0.10);

            Assert.Equal(1, result.UnsoldUnits);
            Assert.Equal(0, station.Stock.Get(new InventoryItem(Ore)).Available);
            Assert.Empty(ledger.Transactions);
        }

        [Theory]
        [InlineData(-1, 0.1)]
        [InlineData(0, -0.1)]
        [InlineData(0, 1.1)]
        public void StationRejectsInvalidConfiguration(long treasury, double taxRate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Station(treasury, taxRate, new MoneyLedger()));
        }
    }
}
