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
        public double BarSpend = 70;            // moi lan vao Bar
        public double StressPerDay = 12;        // Stress tang/ngay; vao Bar khi >= 100
        public double GearShare = 0.40;         // ty le tien du Trainer chi vao trang bi moi ngay
        public double ServiceCogs = 0.25;       // gia von dich vu/trang bi (ty le doanh thu)
        public double UpkeepPerBuildingDay = 40;
        public int Buildings = 12;              // so cong trinh dang van hanh
        public int StrikeDays = 5;
        public int PaydayEvery = 30;
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
    }

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
        double monthProfitAcc;

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
                    t.Gold += pay; t.MonthIncomeAcc += pay;

                    double hpLoss = P.DamagePerTripHp * P.TripsPerDay * s * Noise();
                    Spend(t, hpLoss * P.HospitalPricePerHp, ref profit);
                    Spend(t, P.FoodPerDay * s, ref profit);
                    Spend(t, P.InnPerDay * s, ref profit);
                    Spend(t, P.RepairPerTrip * P.TripsPerDay * s * Noise(), ref profit);
                    double gear = Math.Max(0, t.Gold - 2 * (P.FoodPerDay + P.InnPerDay) * s) * P.GearShare;
                    Spend(t, gear, ref profit);
                    t.Stress += P.StressPerDay + (P.TaxRate > 0.30 ? 8 : 0);
                }
                else
                {
                    t.Stress += 3; // dinh cong: chi di Bar
                }
                if (t.Stress >= 100)
                {
                    Spend(t, P.BarSpend * s, ref profit);
                    t.Stress = 20;
                }
            }
            double upkeep = P.UpkeepPerBuildingDay * P.Buildings;
            Treasury -= upkeep; profit -= upkeep;
            monthProfitAcc += profit;

            if (Day % P.PaydayEvery == 0) Payday();
        }

        void Spend(Trainer t, double amount, ref double profit)
        {
            double paid = Math.Min(Math.Max(0, t.Gold), amount);
            t.Gold -= paid;
            double net = paid * (1 - P.ServiceCogs);
            Treasury += net; profit += net;
        }

        void Payday()
        {
            Paydays++;
            double total = 0;
            foreach (var t in Trainers) total += P.WageRatio * t.MonthIncomeAcc;
            if (Treasury >= total)
            {
                foreach (var t in Trainers) t.Gold += P.WageRatio * t.MonthIncomeAcc;
                Treasury -= total; monthProfitAcc -= total;
            }
            else
            {
                StrikePaydays++;
                foreach (var t in Trainers) t.StrikeLeft = P.StrikeDays;
            }
            foreach (var t in Trainers) { t.MonthIncome = t.MonthIncomeAcc; t.MonthIncomeAcc = 0; }
            LastMonthProfit = monthProfitAcc; monthProfitAcc = 0;
        }

        public double AvgTrainerGold()
        {
            double s = 0; foreach (var t in Trainers) s += t.Gold / t.Scale; return s / Trainers.Count;
        }
    }
}
