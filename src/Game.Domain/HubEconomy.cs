using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>Tham so kinh te (don vi: Gold; moi gia tri la khoi diem, xem docs/designs/13).</summary>
    public sealed class EconomyParams
    {
        public double TaxRate = 0.20;           // thue giao dich o Tram Giao Thuong
        public double LootPerTrip = 100;        // gia tri nguyen lieu moi chuyen cua Trainer Common
        public int TripsPerDay = 4;
        public double RarityGrowth = 1.7;       // he so nhan thu nhap/chi tieu theo bac
        public double ProcessingYield = 0.92;   // gia tri HUB thu ve tren moi 1 Gold nguyen lieu (sau gia cong)
        public double WageRatio = 0.30;         // luong thang = WageRatio * thu nhap rong thang cua bac do
        public double DamagePerTripHp = 20;     // HP mat moi chuyen (Common)
        public double HospitalPricePerHp = 1.4; // Gold/HP
        public double FoodPerDay = 80;          // Gold/ngay (Common)
        public double InnPerDay = 45;
        public double RepairPerTrip = 25;
        public double BarSpend = 800;           // moi lan vao Bar
        public double StressPerDay = 25;        // Stress tang/ngay; vao Bar khi >= 100
        public double GearShare = 0.40;         // ty le tien du Trainer chi vao trang bi moi ngay
        public double ServiceCogs = 0.25;       // gia von dich vu/trang bi (ty le doanh thu)
        public double UpkeepPerBuildingDay = 50;
        public int Buildings = 12;              // so cong trinh dang van hanh
        public int StrikeDays = 5;
        public int PaydayEvery = 30;

        // --- Cho Trainer Vay (Vay Nang Lai) ---
        public bool LoansEnabled = false;
        public double LoanInterest = 0.10;      // lai moi Payday tren du no
        public double LoanLimitWages = 1.0;     // han muc = boi so x luong thang du kien
        public double RepayShare = 0.5;         // ty le thu nhap/luong dung de tra no
        public int OverduePaydays = 2;          // so Payday lien tiep con no thi Trainer dinh cong

        // --- Chi phi va rui ro phia Giam doc (O7) ---
        /// <summary>Giam doc luon giu lai it nhat boi so nay x luong du kien; phan du duoc tai dau tu (nang cap, mua so, ...).</summary>
        public double ReserveWageMultiple = 1.0;
        /// <summary>Ty le phan vuot qua muc du tru duoc tai dau tu moi ngay.</summary>
        public double ReinvestRate = 0.5;
        /// <summary>Xac suat moi thang xay ra cu soc (Siege, Thanh Tra phat, Boss lam hong cong trinh).</summary>
        public double ShockChancePerMonth = 0.0;
        /// <summary>Chi phi cu soc, tinh bang so ngay loi nhuan trung binh.</summary>
        public double ShockCostDays = 10;
    }

    public sealed class Trainer
    {
        public Rarity Rarity;
        public double Gold;
        public double Stress;
        public int StrikeLeft;
        public double Scale;
        public double MonthIncome;      // thu nhap rong thang truoc (de tinh luong)
        public double MonthIncomeAcc;
        public double Debt;
        public int DebtPaydays;
    }

    public enum ServiceKind { Hospital = 0, Food = 1, Inn = 2, Repair = 3, Gear = 4, Bar = 5 }

    public sealed class DayStats
    {
        public double HubRevenue, HubCost;
    }

    public sealed class HubEconomy
    {
        readonly EconomyParams P;
        readonly Random rng;
        public double Treasury;
        public readonly List<Trainer> Trainers = new List<Trainer>();
        public int Day;
        public int StrikePaydays;
        public int Paydays;
        public double LastMonthProfit;
        public double ExpansionSpent;
        public double LastMonthWages;
        /// <summary>Doanh thu rong (sau gia von) theo loai dich vu, thang gan nhat.</summary>
        public readonly double[] LastMonthServiceRevenue = new double[6];
        readonly double[] serviceAcc = new double[6];
        public int Shocks;
        public int DebtStrikes;
        public double InterestAccrued;
        double monthProfitAcc;
        double dailyProfitEma;

        public HubEconomy(EconomyParams p, double startTreasury, IEnumerable<Rarity> roster, int seed)
        {
            P = p; Treasury = startTreasury; rng = new Random(seed);
            foreach (var r in roster)
            {
                double s = RarityScale.Of(r, p.RarityGrowth);
                Trainers.Add(new Trainer { Rarity = r, Scale = s, Gold = 200 * s,
                    MonthIncome = p.LootPerTrip * p.TripsPerDay * s * (1 - p.TaxRate) * p.PaydayEvery });
            }
        }

        double Noise() => 0.8 + 0.4 * rng.NextDouble();

        public void StepDay()
        {
            Day++;
            double profit = 0;
            foreach (var t in Trainers)
            {
                bool striking = t.StrikeLeft > 0;
                if (striking) t.StrikeLeft--;
                double s = t.Scale;

                if (!striking)
                {
                    double gross = P.LootPerTrip * P.TripsPerDay * s * Noise();
                    double pay = gross * (1 - P.TaxRate);          // HUB tra cho Trainer
                    double resale = gross * P.ProcessingYield;     // HUB thu ve sau gia cong
                    Treasury -= pay; Treasury += resale; profit += resale - pay;
                    double repay = P.LoansEnabled ? Math.Min(t.Debt, P.RepayShare * pay) : 0;
                    t.Debt -= repay; Treasury += repay;
                    t.Gold += pay - repay; t.MonthIncomeAcc += pay;

                    double hpLoss = P.DamagePerTripHp * P.TripsPerDay * s * Noise();
                    Spend(t, hpLoss * P.HospitalPricePerHp, ref profit, ServiceKind.Hospital);
                    Spend(t, P.FoodPerDay * s, ref profit, ServiceKind.Food);
                    Spend(t, P.InnPerDay * s, ref profit, ServiceKind.Inn);
                    Spend(t, P.RepairPerTrip * P.TripsPerDay * s * Noise(), ref profit, ServiceKind.Repair);
                    t.Stress += P.StressPerDay + (P.TaxRate > 0.30 ? 8 : 0);
                    VisitBarIfStressed(t, s, ref profit);   // Bar duoc uu tien truoc trang bi
                    double gear = Math.Max(0, t.Gold - 2 * (P.FoodPerDay + P.InnPerDay) * s) * P.GearShare;
                    Spend(t, gear, ref profit, ServiceKind.Gear);
                }
                else
                {
                    t.Stress += 3; // dinh cong: chi di Bar
                    VisitBarIfStressed(t, s, ref profit);
                }
            }
            double upkeep = P.UpkeepPerBuildingDay * P.Buildings;
            Treasury -= upkeep; profit -= upkeep;

            dailyProfitEma = Day == 1 ? profit : 0.9 * dailyProfitEma + 0.1 * profit;
            if (P.ShockChancePerMonth > 0 && rng.NextDouble() < P.ShockChancePerMonth / P.PaydayEvery)
            {
                double cost = P.ShockCostDays * Math.Max(0, dailyProfitEma);
                Treasury -= cost; profit -= cost; Shocks++;
            }
            monthProfitAcc += profit;

            // Tai dau tu: giu lai du tru bang ReserveWageMultiple x luong du kien (theo thu nhap thang truoc/dang tich luy).
            double expectedWage = 0;
            foreach (var t in Trainers) expectedWage += P.WageRatio * Math.Max(t.MonthIncome, t.MonthIncomeAcc / Math.Max(1, Day % P.PaydayEvery == 0 ? P.PaydayEvery : Day % P.PaydayEvery) * P.PaydayEvery);
            double reserve = P.ReserveWageMultiple * expectedWage;
            if (Treasury > reserve)
            {
                double spend = (Treasury - reserve) * P.ReinvestRate;
                Treasury -= spend; ExpansionSpent += spend;
            }

            if (Day % P.PaydayEvery == 0) Payday();
        }

        void VisitBarIfStressed(Trainer t, double s, ref double profit)
        {
            if (t.Stress < 100) return;
            Spend(t, P.BarSpend * s, ref profit, ServiceKind.Bar);
            t.Stress = 20;
        }

        void Spend(Trainer t, double amount, ref double profit, ServiceKind kind)
        {
            double paid = Math.Min(Math.Max(0, t.Gold), amount);
            t.Gold -= paid;
            double borrow = 0;
            if (P.LoansEnabled && paid < amount)
            {
                double limit = P.LoanLimitWages * P.WageRatio * Math.Max(t.MonthIncome, t.MonthIncomeAcc);
                borrow = Math.Min(amount - paid, Math.Max(0, limit - t.Debt));
                t.Debt += borrow;               // HUB cho vay: Gold roi khoi kho bac roi quay lai thanh doanh thu dich vu
                Treasury -= borrow;
            }
            double net = (paid + borrow) * (1 - P.ServiceCogs);
            Treasury += net; profit += net;
            serviceAcc[(int)kind] += net;
        }

        void Payday()
        {
            Paydays++;
            double total = 0;
            foreach (var t in Trainers) total += P.WageRatio * t.MonthIncomeAcc;
            bool wagesPaid = Treasury >= total;
            if (wagesPaid)
            {
                foreach (var t in Trainers) t.Gold += P.WageRatio * t.MonthIncomeAcc;
                Treasury -= total; monthProfitAcc -= total;
            }
            else
            {
                StrikePaydays++;
                foreach (var t in Trainers) t.StrikeLeft = P.StrikeDays;
            }
            if (P.LoansEnabled)
                foreach (var t in Trainers)
                {
                    double r = Math.Min(Math.Min(t.Debt, P.RepayShare * P.WageRatio * t.MonthIncomeAcc), Math.Max(0, t.Gold));
                    if (wagesPaid) { t.Debt -= r; t.Gold -= r; Treasury += r; }
                    if (t.Debt > 0)
                    {
                        double interest = t.Debt * P.LoanInterest;
                        t.Debt += interest; InterestAccrued += interest;
                        // Qua han: du no vuot han muc (1 luong thang) o nhieu Payday lien tiep
                        double limit = P.LoanLimitWages * P.WageRatio * Math.Max(t.MonthIncome, t.MonthIncomeAcc);
                        if (t.Debt > limit) t.DebtPaydays++; else t.DebtPaydays = 0;
                        if (t.DebtPaydays >= P.OverduePaydays) { t.StrikeLeft = P.StrikeDays; DebtStrikes++; t.DebtPaydays = 0; }
                    }
                    else t.DebtPaydays = 0;
                }
            LastMonthWages = total;
            Array.Copy(serviceAcc, LastMonthServiceRevenue, serviceAcc.Length); Array.Clear(serviceAcc, 0, serviceAcc.Length);
            foreach (var t in Trainers) { t.MonthIncome = t.MonthIncomeAcc; t.MonthIncomeAcc = 0; }
            LastMonthProfit = monthProfitAcc; monthProfitAcc = 0;
        }

        public double AvgTrainerGold()
        {
            double s = 0; foreach (var t in Trainers) s += t.Gold / t.Scale; return s / Trainers.Count;
        }
    }
}
