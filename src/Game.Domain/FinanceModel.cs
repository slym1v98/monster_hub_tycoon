using System;

namespace Game.Domain
{
    /// <summary>Starting parameters for HUB-to-Trainer loans from GDD 13 §12.</summary>
    public sealed class TrainerLoanConfig
    {
        public static TrainerLoanConfig Prototype { get; } = new TrainerLoanConfig();
        public double InterestPerPayday { get; }
        public double MonthlyWageLimitMultiplier { get; }
        public double IncomeRepaymentFraction { get; }
        public int OverLimitPaydaysToStrike { get; }
        public System.Collections.Generic.IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public TrainerLoanConfig(double interestPerPayday = 0.10, double monthlyWageLimitMultiplier = 2,
            double incomeRepaymentFraction = 0.5, int overLimitPaydaysToStrike = 2)
        {
            if (interestPerPayday < 0.05 || interestPerPayday > 0.40) throw new ArgumentOutOfRangeException(nameof(interestPerPayday));
            if (monthlyWageLimitMultiplier <= 0 || double.IsNaN(monthlyWageLimitMultiplier) || double.IsInfinity(monthlyWageLimitMultiplier)) throw new ArgumentOutOfRangeException(nameof(monthlyWageLimitMultiplier));
            if (incomeRepaymentFraction < 0 || incomeRepaymentFraction > 1) throw new ArgumentOutOfRangeException(nameof(incomeRepaymentFraction));
            if (overLimitPaydaysToStrike <= 0) throw new ArgumentOutOfRangeException(nameof(overLimitPaydaysToStrike));
            InterestPerPayday = interestPerPayday; MonthlyWageLimitMultiplier = monthlyWageLimitMultiplier;
            IncomeRepaymentFraction = incomeRepaymentFraction; OverLimitPaydaysToStrike = overLimitPaydaysToStrike;
            BalanceParameters = System.Array.AsReadOnly(new[] {
                P("loan.interest_per_payday", interestPerPayday, "fraction/Payday", "docs/designs/13_Balance_Parameters.md §12: default 10%; adjustable 5-40%"),
                P("loan.monthly_wage_limit_multiplier", monthlyWageLimitMultiplier, "monthly wages", "docs/designs/13_Balance_Parameters.md §12: default credit limit 2 monthly wages"),
                P("loan.income_repayment_fraction", incomeRepaymentFraction, "fraction", "docs/designs/13_Balance_Parameters.md §12: apply 50% of income and wages to debt"),
                P("loan.over_limit_paydays_to_strike", overLimitPaydaysToStrike, "Paydays", "docs/designs/13_Balance_Parameters.md §12: strike after exceeding limit for two consecutive Paydays")
            });
        }
        static BalanceParameter P(string id, double value, string unit, string source)
            => new BalanceParameter(id, value, unit, "Prototype", source);
    }

    /// <summary>Starting terms for Trainer-to-HUB reverse loans from GDD 02 §2.</summary>
    public sealed class ReverseLoanConfig
    {
        public static ReverseLoanConfig Prototype { get; } = new ReverseLoanConfig();
        public double CashLimitFraction { get; }
        public double InterestPerPayday { get; }
        public int TermPaydays { get; }
        public System.Collections.Generic.IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public ReverseLoanConfig(double cashLimitFraction = 0.5, double interestPerPayday = 0.05, int termPaydays = 2)
        {
            if (cashLimitFraction <= 0 || cashLimitFraction > 1 || double.IsNaN(cashLimitFraction) || double.IsInfinity(cashLimitFraction)) throw new ArgumentOutOfRangeException(nameof(cashLimitFraction));
            if (interestPerPayday < 0 || interestPerPayday > 1 || double.IsNaN(interestPerPayday) || double.IsInfinity(interestPerPayday)) throw new ArgumentOutOfRangeException(nameof(interestPerPayday));
            if (termPaydays < 1 || termPaydays > 2) throw new ArgumentOutOfRangeException(nameof(termPaydays));
            CashLimitFraction = cashLimitFraction; InterestPerPayday = interestPerPayday; TermPaydays = termPaydays;
            BalanceParameters = System.Array.AsReadOnly(new[] {
                new BalanceParameter("reverse_loan.cash_limit_fraction", cashLimitFraction, "fraction of lender cash", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §2; docs/designs/13_Balance_Parameters.md §13: about 50% of each Rank V Trainer's cash"),
                new BalanceParameter("reverse_loan.interest_per_payday", interestPerPayday, "fraction/Payday", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §2; docs/designs/13_Balance_Parameters.md §13: 5% per Payday"),
                new BalanceParameter("reverse_loan.term_paydays", termPaydays, "Paydays", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §2; docs/designs/13_Balance_Parameters.md §13: due in 1-2 Paydays")
            });
        }
    }

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
