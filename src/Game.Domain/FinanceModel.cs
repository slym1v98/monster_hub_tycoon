using System;

namespace Game.Domain
{
    /// <summary>Co tuc va Thue Tu Do Tai Chinh cua San Chung Khoan HUB Street (docs/designs/02 §3).</summary>
    public static class FinanceModel
    {
        public const double DividendRate = 0.10;     // ty le doanh thu 15 ngay chia thanh co tuc
        public const double FinancialTaxRate = 0.30; // thue tren loi nhuan chung khoan da chot
        public const int DividendPeriodsPerYear = 24; // 360 ngay / 15 ngay

        public static double DividendPool(double revenue15Days) => revenue15Days * DividendRate;

        /// <summary>So co phieu can phat hanh de co tuc nam dat ty suat muc tieu o gia da cho.</summary>
        public static double IpoShares(double revenue15Days, double targetAnnualYield, double price)
        {
            if (targetAnnualYield <= 0 || price <= 0) throw new ArgumentOutOfRangeException();
            return DividendPool(revenue15Days) * DividendPeriodsPerYear / (targetAnnualYield * price);
        }

        public static double YieldPer15Days(double revenue15Days, double shares, double price)
            => DividendPool(revenue15Days) / (shares * price);

        /// <summary>Loi nhuan sau thue; lo khong bi thue.</summary>
        public static double AfterTax(double realizedProfit)
            => realizedProfit > 0 ? realizedProfit * (1 - FinancialTaxRate) : realizedProfit;
    }
}
