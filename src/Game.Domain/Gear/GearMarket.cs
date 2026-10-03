using System;

namespace Game.Domain.Gear
{
    /// <summary>
    /// Quyết định chào hàng của Giám đốc theo GDD 05: Trainer đồng ý khi Gear Score cao hơn,
    /// đủ tiền và không chi quá tỉ lệ Gold đang có. Đồ cũ biến mất trừ khi HUB mua lại (Buyback).
    /// </summary>
    public static class GearMarket
    {
        public static bool WouldAccept(GearItem current, GearItem offered, long money, long price, GearConfig config)
        {
            if (offered == null || config == null) return false;
            if (offered.IsBroken) return false;
            if (price <= 0 || money < price) return false;
            if ((double)price > money * config.AcceptanceGoldFraction) return false;
            double currentScore = current == null ? 0 : GearScore.Score(current, GearCatalog.Default);
            double offeredScore = GearScore.Score(offered, GearCatalog.Default);
            return offeredScore > currentScore;
        }

        /// <summary>Giá HUB mua lại đồ cũ; bằng BuybackFraction của giá chào.</summary>
        public static long BuybackPrice(GearItem item, long salePrice, GearConfig config)
        {
            if (item == null || salePrice < 0 || config == null) return 0;
            return (long)(salePrice * config.BuybackFraction);
        }
    }
}
