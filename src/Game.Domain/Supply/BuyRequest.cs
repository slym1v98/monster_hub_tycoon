using System;
using Game.Domain.Materials;

namespace Game.Domain.Supply
{
    /// <summary>Lệnh mua nguyên liệu của HUB với lượng đang được giữ hoặc đang vận chuyển.</summary>
    public sealed class BuyRequest
    {
        public MaterialId Material { get; }
        public int TargetStock { get; }
        public long BidPrice { get; }
        public bool Enabled { get; }
        public int ReservedForProduction { get; private set; }
        public int IncomingMerchantUnits { get; private set; }

        public BuyRequest(MaterialId material, int targetStock, long bidPrice, bool enabled = true,
            int reservedForProduction = 0, int incomingMerchantUnits = 0)
        {
            if (targetStock < 0) throw new ArgumentOutOfRangeException(nameof(targetStock));
            if (bidPrice < 0) throw new ArgumentOutOfRangeException(nameof(bidPrice));
            if (reservedForProduction < 0) throw new ArgumentOutOfRangeException(nameof(reservedForProduction));
            if (incomingMerchantUnits < 0) throw new ArgumentOutOfRangeException(nameof(incomingMerchantUnits));
            Material = material; TargetStock = targetStock; BidPrice = bidPrice; Enabled = enabled;
            ReservedForProduction = reservedForProduction; IncomingMerchantUnits = incomingMerchantUnits;
        }

        public int Deficit(int stationStock)
        {
            if (stationStock < 0) throw new ArgumentOutOfRangeException(nameof(stationStock));
            if (!Enabled) return 0;
            var covered = (long)stationStock + ReservedForProduction + IncomingMerchantUnits;
            return (int)Math.Max(0L, TargetStock - covered);
        }

        public void ReserveForProduction(int quantity)
        { ReservedForProduction = AddCount(ReservedForProduction, quantity); }
        public void ReleaseProductionReservation(int quantity)
        {
            RequirePositive(quantity);
            if (quantity > ReservedForProduction) throw new InvalidOperationException("Không đủ lượng đã giữ cho sản xuất.");
            ReservedForProduction -= quantity;
        }
        public void AddIncomingMerchantUnits(int quantity)
        { IncomingMerchantUnits = AddCount(IncomingMerchantUnits, quantity); }
        public void ReceiveIncomingMerchantUnits(int quantity)
        {
            RequirePositive(quantity);
            if (quantity > IncomingMerchantUnits) throw new InvalidOperationException("Lượng hàng nhận vượt quá hàng đang về.");
            IncomingMerchantUnits -= quantity;
        }

        private static int AddCount(int current, int quantity)
        { RequirePositive(quantity); return checked(current + quantity); }
        private static void RequirePositive(int quantity)
        { if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity)); }
    }
}
