using System;
using Game.Domain.Materials;

namespace Game.Domain.Supply
{
    /// <summary>Phân bổ lô hàng bán giữa Trạm và các kênh bán đang chờ tích hợp.</summary>
    public sealed record SaleBreakdown(int StationUnits, int MerchantUnits, int UnsoldUnits,
        long Gross, long Tax, long NetToSeller);

    /// <summary>Trạm mua trực tiếp theo lệnh mua, tồn kho và số dư Kho bạc hữu hạn.</summary>
    public sealed class Station
    {
        private const string TreasuryAccount = "hub:treasury";
        private const string TaxAccount = "hub:treasury";
        public Inventory Stock { get; } = new Inventory();
        public long Treasury => ledgerBackedTreasury ? checked(startingTreasury + ledger.BalanceOf(TreasuryAccount)) : treasury.Balance;
        public double TaxRate { get; private set; }
        private readonly MoneyLedger ledger;
        private readonly global::Game.Domain.TreasuryAccount treasury;
        private readonly long startingTreasury;
        private readonly bool ledgerBackedTreasury;

        public Station(long startingTreasury, double taxRate, MoneyLedger ledger)
        {
            if (startingTreasury < 0) throw new ArgumentOutOfRangeException(nameof(startingTreasury));
            if (double.IsNaN(taxRate) || taxRate < 0 || taxRate > 1) throw new ArgumentOutOfRangeException(nameof(taxRate));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            treasury = new global::Game.Domain.TreasuryAccount(startingTreasury);
            this.startingTreasury = startingTreasury;
            ledgerBackedTreasury = true;
            TaxRate = taxRate;
        }

        public Station(global::Game.Domain.TreasuryAccount treasury, double taxRate, MoneyLedger ledger)
        {
            if (double.IsNaN(taxRate) || taxRate < 0 || taxRate > 1) throw new ArgumentOutOfRangeException(nameof(taxRate));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            this.treasury = treasury ?? throw new ArgumentNullException(nameof(treasury));
            ledgerBackedTreasury = false;
            TaxRate = taxRate;
        }

        public void SetTaxRate(double taxRate)
        {
            if (double.IsNaN(taxRate) || taxRate < 0 || taxRate > 1) throw new ArgumentOutOfRangeException(nameof(taxRate));
            TaxRate = taxRate;
        }

        public SaleBreakdown BuyFromTrainer(string sellerAccount, MaterialId material, int offeredUnits, BuyRequest request)
        {
            if (string.IsNullOrWhiteSpace(sellerAccount)) throw new ArgumentException("Thiếu tài khoản người bán.", nameof(sellerAccount));
            if (offeredUnits < 0) throw new ArgumentOutOfRangeException(nameof(offeredUnits));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Material != material) throw new ArgumentException("Lệnh mua áp dụng cho nguyên liệu khác.", nameof(request));
            if (offeredUnits == 0) return new SaleBreakdown(0, 0, 0, 0, 0, 0);

            var item = new InventoryItem(material);
            var balance = Stock.Get(item);
            var stationUnits = CoveredUnits(balance);
            var deficit = request.Deficit(stationUnits);
            var affordable = request.BidPrice == 0 ? int.MaxValue : Treasury / request.BidPrice;
            var accepted = (int)Math.Min(Math.Min((long)offeredUnits, deficit), Math.Min(affordable, int.MaxValue));
            var unsold = offeredUnits - accepted;
            if (accepted == 0) return new SaleBreakdown(0, 0, unsold, 0, 0, 0);

            var gross = checked((long)accepted * request.BidPrice);
            var tax = decimal.ToInt64(decimal.Round((decimal)gross * (decimal)TaxRate, 0, MidpointRounding.AwayFromZero));
            var net = checked(gross - tax);
            if (balance.Available > int.MaxValue - accepted) throw new OverflowException("Tồn kho Trạm vượt giới hạn.");

            ledger.Record(TreasuryAccount, sellerAccount, TaxAccount, gross, tax, "station material purchase");
            if (!ledgerBackedTreasury)
            {
                if (!treasury.TrySpend(gross)) throw new InvalidOperationException("Kho bạc thay đổi trong lúc thanh toán.");
                treasury.Add(tax);
            }
            Stock.Add(item, accepted);
            return new SaleBreakdown(accepted, 0, unsold, gross, tax, net);
        }

        public SaleBreakdown BuyFromMerchant(string merchantAccount, MaterialId material, int offeredUnits,
            BuyRequest request, double markupRate)
        {
            if (string.IsNullOrWhiteSpace(merchantAccount)) throw new ArgumentException("Thiếu tài khoản Merchant.", nameof(merchantAccount));
            if (offeredUnits < 0) throw new ArgumentOutOfRangeException(nameof(offeredUnits));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Material != material) throw new ArgumentException("Lệnh mua áp dụng cho nguyên liệu khác.", nameof(request));
            if (double.IsNaN(markupRate) || markupRate < 0.05 || markupRate > 0.10) throw new ArgumentOutOfRangeException(nameof(markupRate));
            if (offeredUnits == 0) return new SaleBreakdown(0, 0, 0, 0, 0, 0);
            var stock = Stock.Get(new InventoryItem(material));
            var deficit = request.Deficit(CoveredUnits(stock));
            var quotedPrice = decimal.Floor((decimal)request.BidPrice * (1m + (decimal)markupRate));
            if (quotedPrice > long.MaxValue)
                return new SaleBreakdown(0, 0, offeredUnits, 0, 0, 0);
            var unitPrice = decimal.ToInt64(quotedPrice);
            var affordable = unitPrice == 0 ? int.MaxValue : Treasury / unitPrice;
            var accepted = (int)Math.Min(Math.Min((long)offeredUnits, deficit), Math.Min(affordable, int.MaxValue));
            var unsold = offeredUnits - accepted;
            if (accepted == 0) return new SaleBreakdown(0, 0, unsold, 0, 0, 0);
            var gross = checked((long)accepted * unitPrice);
            var balance = Stock.Get(new InventoryItem(material));
            if (balance.Available > int.MaxValue - accepted) throw new OverflowException("Tồn kho Trạm vượt giới hạn.");
            ledger.Record(TreasuryAccount, merchantAccount, TaxAccount, gross, 0, "merchant station sale");
            if (!ledgerBackedTreasury && !treasury.TrySpend(gross)) throw new InvalidOperationException("Kho bạc thay đổi trong lúc thanh toán.");
            Stock.Add(new InventoryItem(material), accepted);
            return new SaleBreakdown(accepted, 0, unsold, gross, 0, gross);
        }

        private static int CoveredUnits(InventoryBalance balance)
            => (int)Math.Min(int.MaxValue, (long)balance.Available + balance.Reserved + balance.InProduction);
    }
}
