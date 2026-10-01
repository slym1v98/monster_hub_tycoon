using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Game.Domain;

// Console runner mô phỏng cân bằng. Chạy: dotnet run --project tools/Game.Sim [ladders|stock]
static class Program
{
    static double[] Geo(double a, double r, int n) => Enumerable.Range(0, n).Select(i => a * Math.Pow(r, i)).ToArray();

    // Chi phí kỳ vọng của các thang nâng cấp (Nâng Sao, Tinh Luyện, tăng tư chất).
    static void Ladders()
    {
        const double costBase = 100, dailyGross = 400;   // Trainer Common: 400 Gold/ngày
        void Row(string name, UpgradeLadder l)
        {
            double e = l.ExpectedCost();
            Console.WriteLine($"{name,-34} chi phí kỳ vọng {e,10:F0} Gold = {e / costBase,7:F0} x CostBase = {e / dailyGross,6:F1} ngày thu nhập Common");
        }
        Console.WriteLine("# Thang nâng cấp (giá trị khởi điểm)");
        var star = new UpgradeLadder(new[] { 0.90, 0.75, 0.55, 0.35, 0.20 }, Geo(300, 1.8, 5).Select(c => c + 100).ToArray(), new[] { 0, 0, 1, 1, 1 });
        Row("Nâng Sao 0->5 (rớt 1 sao từ bước 3)", star);
        var starSafe = new UpgradeLadder(star.Success, star.Cost, new[] { 0, 0, 0, 0, 0 });
        Row("Nâng Sao 0->5 (không rớt sao)", starSafe);
        var refine = new UpgradeLadder(new[] { 0.70, 0.50, 0.30, 0.15 }, Geo(1000, 2.5, 4), new[] { 0, 0, 0, 0 });
        Row("Tinh Luyện Normal->Mythic", refine);
        var rarityUp = new UpgradeLadder(new[] { 0.60, 0.40, 0.25, 0.10 }, Geo(2000, 3.0, 4), new[] { 0, 0, 0, 0 });
        Row("Tăng tư chất Common->Ultimate", rarityUp);
        Console.WriteLine("\n# Từng bước: chi phí kỳ vọng để qua bước i (Gold)");
        foreach (var (name, l) in new[] { ("Nâng Sao", star), ("Tinh Luyện", refine), ("Tư chất", rarityUp) })
        {
            var parts = Enumerable.Range(0, l.Steps).Select(i => (l.Cost[i] / l.Success[i]).ToString("F0"));
            Console.WriteLine($"{name,-10}: " + string.Join(" | ", parts));
        }
    }

    // Tần suất sự kiện cổ phiếu trong 360 ngày.
    static void Stock()

    {
        Console.WriteLine("# Cổ phiếu: tần suất sự kiện trong 360 ngày (200 lần chạy)");
        Console.WriteLine("Biến động/ngày  hoảng loạn (giảm >=10% trong 3 ngày)/năm  sập (giảm >=20% trong 15 ngày)/năm  pump (tăng >=20% trong 15 ngày)/năm");
        foreach (var vol in new[] { 0.01, 0.02, 0.03, 0.05 })
        {
            double panic = 0, crash = 0, pump = 0; int runs = 200;
            for (int r = 0; r < runs; r++)
            {
                var m = new StockMarket(r) { DailyVolatility = vol };
                var path = new double[361]; path[0] = m.Price;
                for (int d = 1; d <= 360; d++) path[d] = m.StepDay();
                bool inPanic = false, inCrash = false, inPump = false;
                for (int d = 3; d <= 360; d++)
                {
                    bool pn = path[d] <= 0.90 * path[d - 3];
                    if (pn && !inPanic) panic++; inPanic = pn;
                }
                for (int d = 15; d <= 360; d++)
                {
                    bool cr = path[d] <= 0.80 * path[d - 15], pu = path[d] >= 1.20 * path[d - 15];
                    if (cr && !inCrash) crash++; inCrash = cr;
                    if (pu && !inPump) pump++; inPump = pu;
                }
            }
            Console.WriteLine($"{vol,13:P0}  {panic / runs,40:F1}  {crash / runs,36:F1}  {pump / runs,34:F1}");
        }
    }

    // Kịch bản core: 10 Trainer Common, 3 tháng, báo cáo dòng tiền và cách Trainer dùng thời gian.
    static void Core()
    {
        const int months = 3, trainerCount = 10;
        var sw = Stopwatch.StartNew();
        var world = new HubWorld(new SimConfig { TrainerCount = trainerCount }, 2026);

        // Thời gian Trainer ở từng trạng thái: cộng dồn khi trạng thái đổi.
        var minutesInState = new Dictionary<TrainerState, long>();
        foreach (TrainerState s in Enum.GetValues(typeof(TrainerState))) minutesInState[s] = 0;
        var lastChange = new int[trainerCount];
        for (int i = 0; i < trainerCount; i++) lastChange[i] = world.Now.TotalMinutes;
        int donations = 0;
        world.EventRaised += e =>
        {
            if (e is TrainerStateChanged s) { minutesInState[s.From] += s.Minute - lastChange[s.TrainerId]; lastChange[s.TrainerId] = s.Minute; }
            else if (e is DonationReceived d && d.Source == "Patron") donations++;
        };

        Console.WriteLine("# Core: 10 Trainer Common, 3 tháng in-game");
        Console.WriteLine("Tháng  Kho bạc trước Payday  Quỹ lương   Trả được  Đình công  Gold TB Trainer");
        for (int m = 1; m <= months; m++)
        {
            world.RunUntilPayday();
            long before = world.Treasury;
            PaydayOutcome o = world.ResolvePayday();
            double avgGold = 0;
            foreach (TrainerView t in world.Trainers) avgGold += t.Gold;
            avgGold /= trainerCount;
            Console.WriteLine($"{m,5}  {before,20}  {o.TotalDue,9}  {o.PaidRatio,8:P0}  {(o.StrikeStarted ? "có" : "không"),9}  {avgGold,14:F0}");
        }

        int end = world.Now.TotalMinutes;
        for (int i = 0; i < trainerCount; i++) minutesInState[world.Trainers[i].State] += end - lastChange[i];
        long total = 0; foreach (long v in minutesInState.Values) total += v;
        double Share(params TrainerState[] states) { long s = 0; foreach (var st in states) s += minutesInState[st]; return (double)s / total; }

        Console.WriteLine("\n# Thời gian của Trainer");
        Console.WriteLine($"Farm {Share(TrainerState.Farming):P1} | Đi lại {Share(TrainerState.Traveling, TrainerState.Returning):P1} | " +
                          $"Xếp hàng {Share(TrainerState.Queued):P1} | Dịch vụ {Share(TrainerState.InService):P1} | " +
                          $"Chờ tiền {Share(TrainerState.WaitingForMoney):P1} | Rảnh ở HUB {Share(TrainerState.AtHub):P1}");

        Console.WriteLine("\n# Công trình");
        foreach (BuildingView b in world.Buildings)
            Console.WriteLine($"{b.Kind,-11} cấp {b.Level} {b.Slots,2} chỗ, hàng đợi dài nhất {b.MaxQueueLength}");
        Console.WriteLine($"\nTổng tài donate {donations} lần; chờ tiền dài nhất {world.MaxMoneyWaitMinutes / 60.0:F1} giờ in-game.");
        world.ValidateInvariants();
        sw.Stop();
        Console.WriteLine($"Thời gian chạy: {sw.ElapsedMilliseconds} ms (mục tiêu dưới 1000 ms)");
    }

    static void Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "core";
        if (mode == "core") { Core(); return; }
        if (mode == "ladders") { Ladders(); return; }
        if (mode == "stock") { Stock(); return; }
        Console.WriteLine("Dùng: dotnet run --project tools/Game.Sim [core|ladders|stock]");
    }
}
