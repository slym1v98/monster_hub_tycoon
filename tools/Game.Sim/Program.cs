using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain;

// Console runner mo phong can bang kinh te. Chay: dotnet run --project tools/Game.Sim
// Ket qua dung de dien docs/designs/13_Balance_Parameters.md (gia tri "Khoi diem").
static class Program
{
    static IEnumerable<Rarity> Roster(int n, double[] dist)
    {
        var rng = new Random(7);
        for (int i = 0; i < n; i++)
        {
            double x = rng.NextDouble(), c = 0;
            for (int r = 0; r < dist.Length; r++) { c += dist[r]; if (x <= c) { yield return (Rarity)r; goto next; } }
            yield return Rarity.Common;
            next:;
        }
    }

    sealed class Result { public double StrikeRate, EndTreasury, MonthProfit, TrainerGold; public double Margin, Drift; }

    static Result Run(EconomyParams p, int trainers, double[] dist, double start, int months, int runs)
    {
        double strike = 0, end = 0, profit = 0, gold = 0, margin = 0, drift = 0;
        for (int i = 0; i < runs; i++)
        {
            var hub = new HubEconomy(p, start, Roster(trainers, dist).ToList(), 1000 + i);
            double mid = 0;
            for (int d = 0; d < months * p.PaydayEvery; d++) { hub.StepDay(); if (d == months * p.PaydayEvery / 2 - 1) mid = hub.AvgTrainerGold(); }
            drift += hub.AvgTrainerGold() - mid;
            strike += (double)hub.StrikePaydays / hub.Paydays;
            end += hub.Treasury; profit += hub.LastMonthProfit; gold += hub.AvgTrainerGold();
            double flow = hub.Trainers.Sum(t => p.LootPerTrip * p.TripsPerDay * t.Scale * p.PaydayEvery);
            margin += hub.LastMonthProfit / flow;
        }
        return new Result { StrikeRate = strike / runs, EndTreasury = end / runs, MonthProfit = profit / runs,
            TrainerGold = gold / runs, Margin = margin / runs, Drift = drift / runs };
    }

    // Muc tieu thiet ke: bien loi nhuan HUB 20-35%; Trainer "luon ngheo di" (vang tich luy <= ~5 ngay thu nhap);
    // FTUE 5 Common von 3000 song sot 3 thang nhung khong thua thai.
    static double Score(EconomyParams p, out Result full, out Result ftue)
    {
        var mixed = new double[] { 0.45, 0.30, 0.15, 0.08, 0.02 };
        full = Run(p, 30, mixed, 20000, 12, 20);
        ftue = Run(p, 5, new double[] { 1.0 }, 3000, 3, 40);
        double sc = 0;
        double cap = 5 * p.LootPerTrip * p.TripsPerDay;           // 5 ngay thu nhap Common
        if (full.TrainerGold > cap) sc += (full.TrainerGold - cap) / cap;     // Trainer khong duoc giau
        if (full.Drift > 0) sc += full.Drift / cap;                            // vang khong duoc tang dan: "luon ngheo di"
        if (full.StrikeRate > 0.05) sc += full.StrikeRate * 5;
        if (ftue.StrikeRate > 0.0) sc += ftue.StrikeRate * 5;                  // FTUE khong duoc dinh cong o Payday dau
        return sc;
    }

    static void Calibrate()
    {
        var rng = new Random(42);
        var best = new List<(double sc, EconomyParams p, Result f, Result t)>();
        for (int i = 0; i < 600; i++)
        {
            var p = new EconomyParams
            {
                WageRatio = 0.15 + 0.35 * rng.NextDouble(),
                HospitalPricePerHp = 1.0 + 3.0 * rng.NextDouble(),
                FoodPerDay = 40 + 80 * rng.NextDouble(),
                InnPerDay = 30 + 70 * rng.NextDouble(),
                RepairPerTrip = 15 + 40 * rng.NextDouble(),
                GearShare = 0.10 + 0.50 * rng.NextDouble(),
                ProcessingYield = 0.85 + 0.15 * rng.NextDouble(),
                Buildings = 12
            };
            double sc = Score(p, out var f, out var t);
            best.Add((sc, p, f, t));
        }
        Console.WriteLine("## Do tham so (600 mau, top 5)");
        foreach (var b in best.Where(x => x.sc < 0.01).OrderByDescending(x => x.p.WageRatio).Take(5))
            Console.WriteLine($"score {b.sc:F2} | luong {b.p.WageRatio:P0} vienphi/HP {b.p.HospitalPricePerHp:F2} an {b.p.FoodPerDay:F0} tro {b.p.InnPerDay:F0} sua/chuyen {b.p.RepairPerTrip:F0} trangbi {b.p.GearShare:P0} yield {b.p.ProcessingYield:F2} | dong tien ve HUB {b.f.Margin:P0} vang/Trainer {b.f.TrainerGold:F0} xu huong {b.f.Drift:F0} dinhcong {b.f.StrikeRate:P0} | FTUE dinhcong {b.t.StrikeRate:P0} LN thang {b.t.MonthProfit:F0}");
    }

    static EconomyParams Seed() => new EconomyParams();   // mac dinh trong Game.Domain = bo khoi diem

    static void Stress()
    {
        var mixed = new double[] { 0.45, 0.30, 0.15, 0.08, 0.02 };
        var b = Run(Seed(), 30, mixed, 20000, 12, 100);
        Console.WriteLine($"Bo khoi diem: luong 30%, vien phi 1.4/HP, an 80, tro 45, sua 25/chuyen, trang bi 40%, yield 0.92");
        Console.WriteLine($" 30 Trainer/12 thang: dinh cong {b.StrikeRate:P0}, vang/Trainer {b.TrainerGold:F0}, xu huong {b.Drift:F0}, dong tien ve HUB {b.Margin:P0}");

        Console.WriteLine("\n# Von toi thieu de FTUE (5 Common) khong dinh cong o Payday dau");
        foreach (var cap in new[] { 0.0, 500, 1000, 2000, 3000, 5000 })
        {
            var r = Run(Seed(), 5, new double[] { 1.0 }, cap, 1, 100);
            Console.WriteLine($" von {cap:F0}: ty le dinh cong {r.StrikeRate:P0}");
        }

        Console.WriteLine("\n# Thue giao dich (30 Trainer, 12 thang): vang Trainer va xu huong");
        foreach (var tax in new[] { 0.10, 0.20, 0.30, 0.40 })
        {
            var p = Seed(); p.TaxRate = tax;
            var r = Run(p, 30, mixed, 20000, 12, 60);
            Console.WriteLine($" thue {tax:P0}: vang/Trainer {r.TrainerGold:F0}, xu huong {r.Drift:F0}, dinh cong {r.StrikeRate:P0}, loi nhuan thang cuoi {r.MonthProfit:F0}");
        }

        Console.WriteLine("\n# Soc thu nhap: LootPerTrip giam (vi du khu farm xau / Trainer chet nhieu), 30 Trainer, von 20000");
        foreach (var k in new[] { 1.0, 0.7, 0.5, 0.3 })
        {
            var p = Seed(); p.LootPerTrip *= k;
            var r = Run(p, 30, mixed, 20000, 6, 60);
            Console.WriteLine($" thu nhap x{k}: dinh cong {r.StrikeRate:P0}, kho bac cuoi {r.EndTreasury:F0}");
        }

        Console.WriteLine("\n# Luong: ty le Payday dinh cong theo WageRatio, HUB it von (5000), 30 Trainer");
        foreach (var w in new[] { 0.30, 0.60, 1.00, 1.50 })
        {
            var p = Seed(); p.WageRatio = w;
            var r = Run(p, 30, mixed, 5000, 6, 60);
            Console.WriteLine($" luong {w:P0}: dinh cong {r.StrikeRate:P0}, kho bac cuoi {r.EndTreasury:F0}");
        }
    }

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "calibrate") { Calibrate(); return; }
        if (args.Length > 0 && args[0] == "stress") { Stress(); return; }
        var ftue = new double[] { 1.0 };
        var mixed = new double[] { 0.45, 0.30, 0.15, 0.08, 0.02 };

        Console.WriteLine("## A. FTUE: 5 Trainer Common, von 3000, 3 thang");
        foreach (var tax in new[] { 0.10, 0.20, 0.30, 0.40 })
        {
            var p = new EconomyParams { TaxRate = tax };
            var r = Run(p, 5, ftue, 3000, 3, 200);
            Console.WriteLine($"thue {tax:P0}: ty le Payday dinh cong {r.StrikeRate:P0}, kho bac cuoi {r.EndTreasury:F0}, loi nhuan thang cuoi {r.MonthProfit:F0}, vang Trainer/he so {r.TrainerGold:F0}");
        }

        Console.WriteLine("\n## B. HUB day du: 30 Trainer hon hop, von 20000, 12 thang, quet WageRatio");
        foreach (var w in new[] { 0.20, 0.30, 0.35, 0.40, 0.50 })
        {
            var p = new EconomyParams { WageRatio = w, Buildings = 12 };
            var r = Run(p, 30, mixed, 20000, 12, 100);
            Console.WriteLine($"luong {w:P0} thu nhap: dinh cong {r.StrikeRate:P0}, kho bac cuoi {r.EndTreasury:F0}, bien loi nhuan {r.Margin:P0}, vang Trainer/he so {r.TrainerGold:F0}");
        }

        Console.WriteLine("\n## C. Cuong hoa (chi phi ky vong, don vi Gold; CostBase 100, tang 1.30/cap)");
        var enh = new EnhancementModel { ScrollPrice = 600 };
        double replacement = 2000;
        foreach (var target in new[] { 10, 15, 20 })
            Console.WriteLine($"+{target}: khong Bua {enh.ExpectedCost(target, false, replacement):F0}, co Bua(600/lan) {enh.ExpectedCost(target, true, replacement):F0}");
        foreach (var sp in new[] { 300, 600, 1200, 2500 })
        {
            enh.ScrollPrice = sp;
            Console.WriteLine($"Bua {sp}: +15 co Bua {enh.ExpectedCost(15, true, replacement):F0} vs khong Bua {enh.ExpectedCost(15, false, replacement):F0}");
        }

        Console.WriteLine("\n   Gia Bua hoa von (so voi CostBase=100) - Bua dang mua khi gia < gia nay:");
        foreach (var target in new[] { 15, 20 })
        {
            double lo = 0, hi = 1e7;
            for (int it = 0; it < 60; it++)
            {
                double mid = (lo + hi) / 2; enh.ScrollPrice = mid;
                if (enh.ExpectedCost(target, true, replacement) < enh.ExpectedCost(target, false, replacement)) lo = mid; else hi = mid;
            }
            Console.WriteLine($"   +{target}: hoa von o gia Bua ~ {lo:F0} Gold (= {lo / 100:F0} x CostBase)");
        }

        Console.WriteLine("\n## D. Gacha Thu Moi Hoang Gia (Epic 80%, Legendary 17%, Ultimate 3%)");
        foreach (var pity in new[] { 40, 60, 80 })
            Console.WriteLine($"hard pity Ultimate {pity}: ky vong {GachaModel.ExpectedPulls(0.03, pity):F1} luot; Legendary+ (20%) pity 10: {GachaModel.ExpectedPulls(0.20, 10):F1} luot");
    }
}
