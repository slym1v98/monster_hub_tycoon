# Domain Core: Time & Trainer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay `HubEconomy` (mô hình gộp theo ngày) bằng động cơ mô phỏng sự kiện rời rạc theo phút in-game trong `Game.Domain`, nơi Trainer sống đủ một vòng: đi farm, về HUB, xếp hàng, dùng dịch vụ, nhận lương.

**Architecture:** Một lớp `HubWorld` (chia thành các file `partial`) giữ hàng đợi sự kiện ưu tiên theo thời điểm. Mỗi Trainer chạy FSM; kết quả farm và việc bán nguyên liệu đi qua hai interface tạm (`IFarmResolver`, `IMaterialMarket`) để sub-project 2 và 3 thay sau. Mọi ngẫu nhiên qua `SimRandom` có seed, Gold là `long`.

**Tech Stack:** C# 9 (`netstandard2.1`, không phụ thuộc `UnityEngine`), xUnit 2.9.2 trên `net8.0`, console runner `tools/Game.Sim`.

Spec: `docs/superpowers/specs/2026-10-01-domain-core-time-trainer-design.md`.

## Global Constraints

- `src/Game.Domain` là `netstandard2.1`, `LangVersion 9.0`, `Nullable disable`, **không** tham chiếu `UnityEngine`. Dùng `record` được vì có polyfill `IsExternalInit` (Task 2).
- Tên hàm, tham số, biến, giả mã bằng **tiếng Anh**; chú thích và comment bằng **tiếng Việt có dấu**. Không bắt chước code cũ (comment không dấu).
- Toàn bộ file mới dùng `namespace Game.Domain` (phẳng, như code hiện có); thư mục con chỉ để tổ chức.
- Gold là `long`. Không dùng `DateTime`, không dùng `Random` static. Mọi ngẫu nhiên qua `SimRandom`.
- Tất định: cùng seed cho cùng kết quả. Sự kiện cùng thời điểm xếp theo thứ tự vào hàng. Duyệt Trainer theo id tăng dần.
- 1 ngày = 1440 phút in-game (= 15 phút thực), 1 tháng = 30 ngày; ban ngày 06:00-18:00; Payday ở phút 23:59 của ngày 30 (`PaydayMinute(i) = (i + 1) * 43200 - 1`).
- Số chỗ công trình dịch vụ = `5 + floor(0.8 x Level)` (tính bằng số nguyên: `5 + (Level * 4) / 5`); Thiếu bảo trì thì chia đôi.
- Kho bạc không bao giờ âm. Lệnh người chơi sai trả `CommandResult.Rejected`, không ném exception; lỗi lập trình ném exception.
- Commit kết thúc bằng dòng `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Lệnh `dotnet` chạy từ thư mục gốc repo `/Users/nofine/Workspace/gameProjects/MonterHUBTycoon`.

## File Structure

```
src/Game.Domain/
  IsExternalInit.cs                  polyfill cho record trên netstandard2.1
  Simulation/SimClock.cs             hằng số thời gian + struct SimTime
  Simulation/EventQueue.cs           SimEventKind, SimEvent, EventQueue (heap)
  Simulation/SimRandom.cs            RNG tất định, lưu được trạng thái
  Config/SimConfig.cs                SimConfig, BuildingSpec (mọi tham số khởi điểm)
  Hub/TreasuryAccount.cs             Kho bạc không âm
  Hub/Payroll.cs                     lương hợp đồng, Payday, thang vỡ nợ
  Hub/HubWorldTypes.cs               RunResult, CommandResult, PaydayForecast, TrainerView, BuildingView
  Hub/HubWorld.cs                    khởi tạo, vòng chạy, Dawn/Dusk/DayStart
  Hub/HubWorld.Trainers.cs           FSM của Trainer
  Hub/HubWorld.Services.cs           hàng đợi, thanh toán, hết tiền
  Hub/HubWorld.Payday.cs             PaydayDue, ResolvePayday, Forecast
  Hub/HubWorld.Views.cs              truy vấn chỉ đọc
  Hub/HubWorld.Commands.cs           lệnh Giám đốc + ValidateInvariants
  Trainers/Trainer.cs                Trainer, Needs, các enum
  Trainers/PersonalityProfile.cs     hệ số theo tính cách
  Trainers/TrainerBrain.cs           ShouldReturn, PickService (hàm thuần)
  Buildings/ServiceBuilding.cs       BuildingKind, ServiceBuilding
  World/FarmAndMarket.cs             IFarmResolver, SimpleFarmResolver, IMaterialMarket, FixedPriceMarket
  Events/DomainEvents.cs             IDomainEvent + các record sự kiện
tests/Game.Domain.Tests/             mỗi nhóm một file (xem từng Task)
tools/Game.Sim/Program.cs            kịch bản core, ladders, stock
```

---

### Task 1: Cài .NET 8 SDK và gỡ mô hình cũ

**Files:**
- Delete: `src/Game.Domain/HubEconomy.cs`
- Rename + rewrite: `tests/Game.Domain.Tests/DomainTests.cs` -> `EnhancementAndGachaTests.cs`; `LoansAndStockTests.cs` -> `StockMarketTests.cs`; `RemainingSystemsTests.cs` -> `RebellionAndFinanceTests.cs`
- Rewrite: `tools/Game.Sim/Program.cs`

**Interfaces:**
- Produces: repo build được và `dotnet test` xanh khi chỉ còn các mô hình độc lập (`EnhancementModel`, `GachaModel`, `StockMarket`, `FinanceModel`, `RebellionModel`, `UpgradeLadder`, `Rarity`). Enum `Personality` và `ServiceKind` biến mất cùng `HubEconomy.cs` (Task 3 tạo lại `Personality`).

- [ ] **Step 1: Kiểm tra dotnet**

Run: `dotnet --version`
Expected: nếu in số phiên bản `8.x` trở lên thì bỏ qua Step 2. Nếu `command not found` thì làm Step 2.

- [ ] **Step 2: Cài .NET 8 SDK (không cần sudo)**

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
dotnet --version
```
Expected: in `8.0.x`. Các lệnh sau cần `PATH` này; nếu mở shell mới thì chạy lại hai dòng `export`.

- [ ] **Step 3: Chạy test hiện có làm mốc**

Run: `dotnet test tests/Game.Domain.Tests`
Expected: `Passed!` với 0 lỗi (khoảng 37 test).

- [ ] **Step 4: Xóa mô hình cũ và viết lại các file test giữ lại**

```bash
git rm src/Game.Domain/HubEconomy.cs
git mv tests/Game.Domain.Tests/DomainTests.cs tests/Game.Domain.Tests/EnhancementAndGachaTests.cs
git mv tests/Game.Domain.Tests/LoansAndStockTests.cs tests/Game.Domain.Tests/StockMarketTests.cs
git mv tests/Game.Domain.Tests/RemainingSystemsTests.cs tests/Game.Domain.Tests/RebellionAndFinanceTests.cs
```

Ghi đè `tests/Game.Domain.Tests/EnhancementAndGachaTests.cs`:

```csharp
using System;
using Xunit;
using Game.Domain;

public class EnhancementTests
{
    [Fact]
    public void SuccessRateNeverIncreasesWithLevelAndStaysInRange()
    {
        var m = new EnhancementModel();
        double prev = 1.0;
        for (int lv = 1; lv <= 20; lv++)
        {
            double p = m.Success(lv);
            Assert.InRange(p, 0.0, 1.0);
            Assert.True(p <= prev + 1e-12, $"+{lv} cao hơn cấp trước");
            prev = p;
        }
    }

    [Theory] [InlineData(0)] [InlineData(21)]
    public void SuccessRejectsOutOfRangeLevel(int lv)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new EnhancementModel().Success(lv));

    [Fact]
    public void ScrollOnlyMattersFromBreakLevel()
    {
        var m = new EnhancementModel { ScrollPrice = 600 };
        Assert.Equal(m.ExpectedCost(10, false, 2000), m.ExpectedCost(10, true, 2000), 6);
        Assert.True(m.ExpectedCost(20, true, 2000) < m.ExpectedCost(20, false, 2000));
    }

    [Fact]
    public void ExpensiveScrollIsNotWorthIt()
    {
        var m = new EnhancementModel { ScrollPrice = 1_000_000 };
        Assert.True(m.ExpectedCost(15, true, 2000) > m.ExpectedCost(15, false, 2000));
    }
}

public class GachaTests
{
    [Fact] public void OnePullPityIsOnePull() => Assert.Equal(1.0, GachaModel.ExpectedPulls(0.03, 1), 9);

    [Fact]
    public void ExpectedPullsBoundedByPity()
    {
        double e = GachaModel.ExpectedPulls(0.03, 60);
        Assert.InRange(e, 1.0, 60.0);
        Assert.True(GachaModel.ExpectedPulls(0.03, 40) < e);
    }

    [Theory] [InlineData(0.0)] [InlineData(1.5)]
    public void RejectsInvalidProbability(double p)
        => Assert.Throws<ArgumentOutOfRangeException>(() => GachaModel.ExpectedPulls(p, 10));
}
```

Ghi đè `tests/Game.Domain.Tests/StockMarketTests.cs`:

```csharp
using Xunit;
using Game.Domain;

public class StockMarketTests
{
    [Fact]
    public void SameSeedSamePath()
    {
        var a = new StockMarket(1); var b = new StockMarket(1);
        for (int i = 0; i < 100; i++) Assert.Equal(a.StepDay(), b.StepDay());
    }

    [Fact]
    public void PriceNeverBelowOne()
    {
        var m = new StockMarket(2) { DailyVolatility = 0.5 };
        for (int i = 0; i < 500; i++) Assert.True(m.StepDay() >= 1);
    }

    [Fact]
    public void TrafficBoostRaisesPriceWithZeroVolatility()
    {
        var m = new StockMarket(3) { DailyVolatility = 0 };
        double before = m.Price;
        Assert.True(m.StepDay(0.2) > before);
    }
}
```

Ghi đè `tests/Game.Domain.Tests/RebellionAndFinanceTests.cs`:

```csharp
using Xunit;
using Game.Domain;

public class RebellionTests
{
    [Fact] public void SameRaritySameLevelIsControllable()
        => Assert.False(RebellionModel.IsRebellious(30, Rarity.Rare, 30, Rarity.Rare));

    [Fact] public void OneTierHigherRebelsOnlyIfMonsterLevelExceedsTrainer()
    {
        Assert.False(RebellionModel.IsRebellious(30, Rarity.Epic, 30, Rarity.Rare));
        Assert.True(RebellionModel.IsRebellious(31, Rarity.Epic, 30, Rarity.Rare));
    }

    [Fact] public void TierIsWorthTwentyLevels()
    {
        Assert.False(RebellionModel.IsRebellious(50, Rarity.Common, 30, Rarity.Common));
        Assert.True(RebellionModel.IsRebellious(51, Rarity.Common, 30, Rarity.Common));
    }

    [Fact] public void BonusRaisesLeadership()
        => Assert.False(RebellionModel.IsRebellious(60, Rarity.Common, 30, Rarity.Common, bonus: 10));
}

public class FinanceTests
{
    [Fact] public void LossesAreNotTaxed() => Assert.Equal(-100, FinanceModel.AfterTax(-100));
    [Fact] public void GainsAreTaxedAtThirtyPercent() => Assert.Equal(70, FinanceModel.AfterTax(100), 6);

    [Fact] public void IpoSizingHitsTargetYield()
    {
        double shares = FinanceModel.IpoShares(100_000, 0.08, 100);
        double perPeriod = FinanceModel.YieldPer15Days(100_000, shares, 100);
        Assert.Equal(0.08, perPeriod * FinanceModel.DividendPeriodsPerYear, 9);
    }
}
```

- [ ] **Step 2b: Viết lại Game.Sim chỉ còn `ladders` và `stock`**

Ghi đè `tools/Game.Sim/Program.cs`:

```csharp
using System;
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

    static void Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "";
        if (mode == "ladders") { Ladders(); return; }
        if (mode == "stock") { Stock(); return; }
        Console.WriteLine("Dùng: dotnet run --project tools/Game.Sim [ladders|stock]   (kịch bản core được thêm ở Task 9)");
    }
}
```

- [ ] **Step 5: Build và chạy test**

Run: `dotnet build && dotnet test tests/Game.Domain.Tests && dotnet run --project tools/Game.Sim -- ladders | head -3`
Expected: build thành công, test `Passed!`, `ladders` in dòng đầu `# Thang nâng cấp (giá trị khởi điểm)`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: remove HubEconomy and its dependent tests and sim scenarios" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Nền tảng mô phỏng: thời gian, hàng đợi sự kiện, RNG, cấu hình

**Files:**
- Create: `src/Game.Domain/IsExternalInit.cs`, `src/Game.Domain/Simulation/SimClock.cs`, `src/Game.Domain/Simulation/EventQueue.cs`, `src/Game.Domain/Simulation/SimRandom.cs`, `src/Game.Domain/Config/SimConfig.cs`
- Test: `tests/Game.Domain.Tests/SimClockTests.cs`, `EventQueueTests.cs`, `SimRandomTests.cs`
- Modify: `docs/superpowers/specs/2026-10-01-domain-core-time-trainer-design.md` (§9 đồng bộ tham số)

**Interfaces:**
- Produces:
  - `SimClock.MinutesPerDay/DaysPerMonth/MinutesPerMonth/DawnMinute/DuskMinute` (`const int`), `SimClock.IsNight(int)`, `SimClock.NextMinuteOfDay(int fromMinute, int minuteOfDay)` (thời điểm **lớn hơn hẳn** `fromMinute`), `SimClock.PaydayMinute(int paydayIndex)`.
  - `struct SimTime(int totalMinutes)` với `TotalMinutes, Day (0-based), MinuteOfDay, DayOfMonth (1-30), IsNight`.
  - `enum SimEventKind { TrainerDecide, TrainerArriveZone, FarmChunk, TrainerArriveHub, ServiceDone, WaitTick, Dawn, Dusk, DayStart, PaydayDue }`.
  - `readonly struct SimEvent { int Time; long Seq; SimEventKind Kind; int TrainerId; int Token; int Arg; }`.
  - `EventQueue`: `Count`, `PeekTime`, `Schedule(int time, SimEventKind kind, int trainerId = -1, int token = 0, int arg = 0)`, `SimEvent Dequeue()` (ném `InvalidOperationException` khi rỗng), `IReadOnlyList<SimEvent> Snapshot`.
  - `SimRandom(int seed)`: `ulong State {get;set;}`, `double NextDouble()` trong [0,1), `int NextInt(int maxExclusive)`.
  - `BuildingKind { Inn = 0, Restaurant = 1, Bar = 2, Hospital = 3 }` và `BuildingSpec(BuildingKind kind, long fairPrice, int serviceMinutes)` (định nghĩa trong `SimConfig.cs`).
  - `SimConfig` (mọi trường ở Step 8), `SimConfig.Default`, `long SimConfig.ContractWageFor(Rarity, Personality)`.
  - `enum Personality { Warlike = 0, Timid = 1, Glutton = 2, Capitalist = 3 }` được định nghĩa ở Task 3; vì `SimConfig` dùng nó nên **Task 2 tạo luôn file `Trainers/Personality.cs`** (Step 7).

- [ ] **Step 1: Viết test thất bại cho SimClock**

`tests/Game.Domain.Tests/SimClockTests.cs`:

```csharp
using Xunit;
using Game.Domain;

public class SimClockTests
{
    [Theory]
    [InlineData(359, true)]
    [InlineData(360, false)]
    [InlineData(1079, false)]
    [InlineData(1080, true)]
    [InlineData(1440 + 100, true)]
    public void IsNightFollowsDawnAndDuskBoundaries(int minute, bool expected)
        => Assert.Equal(expected, SimClock.IsNight(minute));

    [Fact]
    public void NextMinuteOfDayIsStrictlyLater()
    {
        Assert.Equal(1800, SimClock.NextMinuteOfDay(360, SimClock.DawnMinute));   // đúng mốc thì lấy ngày hôm sau
        Assert.Equal(1080, SimClock.NextMinuteOfDay(360, SimClock.DuskMinute));
        Assert.Equal(1440, SimClock.NextMinuteOfDay(360, 0));
    }

    [Fact]
    public void PaydayIsLastMinuteOfDayThirty()
    {
        Assert.Equal(43199, SimClock.PaydayMinute(0));
        Assert.Equal(86399, SimClock.PaydayMinute(1));
    }

    [Fact]
    public void SimTimeSplitsDayAndMinute()
    {
        var t = new SimTime(1440 + 75);
        Assert.Equal(1, t.Day);
        Assert.Equal(75, t.MinuteOfDay);
        Assert.Equal(2, t.DayOfMonth);
        Assert.True(t.IsNight);
        Assert.Equal(1, new SimTime(30 * 1440).DayOfMonth);   // ngày đầu tháng sau
    }
}
```

- [ ] **Step 2: Chạy để thấy lỗi biên dịch**

Run: `dotnet test tests/Game.Domain.Tests --filter SimClockTests`
Expected: FAIL (build error: `SimClock` không tồn tại).

- [ ] **Step 3: Viết polyfill và SimClock**

`src/Game.Domain/IsExternalInit.cs`:

```csharp
// Polyfill để dùng record/init trên netstandard2.1 (C# 9). Không đổi hành vi, chỉ để compiler chấp nhận.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
```

`src/Game.Domain/Simulation/SimClock.cs`:

```csharp
namespace Game.Domain
{
    /// <summary>Hằng số và hàm hỗ trợ về thời gian mô phỏng. Đơn vị là phút in-game (1 ngày = 15 phút thực).</summary>
    public static class SimClock
    {
        public const int MinutesPerDay = 1440;
        public const int DaysPerMonth = 30;
        public const int MinutesPerMonth = MinutesPerDay * DaysPerMonth;
        public const int DawnMinute = 6 * 60;    // 06:00, bắt đầu ban ngày
        public const int DuskMinute = 18 * 60;   // 18:00, bắt đầu ban đêm

        /// <summary>Ban đêm là 18:00 đến 06:00.</summary>
        public static bool IsNight(int totalMinutes)
        {
            int m = totalMinutes % MinutesPerDay;
            return m < DawnMinute || m >= DuskMinute;
        }

        /// <summary>Thời điểm nhỏ nhất lớn hơn hẳn <paramref name="fromMinute"/> mà phút-trong-ngày bằng <paramref name="minuteOfDay"/>.</summary>
        public static int NextMinuteOfDay(int fromMinute, int minuteOfDay)
        {
            int t = (fromMinute / MinutesPerDay) * MinutesPerDay + minuteOfDay;
            if (t <= fromMinute) t += MinutesPerDay;
            return t;
        }

        /// <summary>Phút xảy ra Payday thứ <paramref name="paydayIndex"/> (bắt đầu từ 0): phút cuối cùng của ngày 30.</summary>
        public static int PaydayMinute(int paydayIndex) => (paydayIndex + 1) * MinutesPerMonth - 1;
    }

    /// <summary>Thời điểm mô phỏng ở dạng chỉ đọc, dành cho UI.</summary>
    public readonly struct SimTime
    {
        public readonly int TotalMinutes;
        public SimTime(int totalMinutes) { TotalMinutes = totalMinutes; }

        /// <summary>Số ngày đã qua, bắt đầu từ 0.</summary>
        public int Day => TotalMinutes / SimClock.MinutesPerDay;
        public int MinuteOfDay => TotalMinutes % SimClock.MinutesPerDay;
        /// <summary>Ngày trong tháng, từ 1 đến 30.</summary>
        public int DayOfMonth => Day % SimClock.DaysPerMonth + 1;
        public bool IsNight => SimClock.IsNight(TotalMinutes);
    }
}
```

- [ ] **Step 4: Chạy test SimClock**

Run: `dotnet test tests/Game.Domain.Tests --filter SimClockTests`
Expected: PASS.

- [ ] **Step 5: Viết test thất bại cho EventQueue và SimRandom**

`tests/Game.Domain.Tests/EventQueueTests.cs`:

```csharp
using System;
using Xunit;
using Game.Domain;

public class EventQueueTests
{
    [Fact]
    public void DequeuesInTimeOrder()
    {
        var q = new EventQueue();
        q.Schedule(50, SimEventKind.DayStart);
        q.Schedule(10, SimEventKind.Dawn);
        q.Schedule(30, SimEventKind.Dusk);
        Assert.Equal(10, q.PeekTime);
        Assert.Equal(10, q.Dequeue().Time);
        Assert.Equal(30, q.Dequeue().Time);
        Assert.Equal(50, q.Dequeue().Time);
        Assert.Equal(0, q.Count);
    }

    [Fact]
    public void SameTimeKeepsInsertionOrder()
    {
        var q = new EventQueue();
        q.Schedule(10, SimEventKind.Dawn);
        q.Schedule(10, SimEventKind.Dusk);
        q.Schedule(10, SimEventKind.DayStart);
        Assert.Equal(SimEventKind.Dawn, q.Dequeue().Kind);
        Assert.Equal(SimEventKind.Dusk, q.Dequeue().Kind);
        Assert.Equal(SimEventKind.DayStart, q.Dequeue().Kind);
    }

    [Fact]
    public void ManyRandomEventsComeOutSorted()
    {
        var rng = new SimRandom(5);
        var q = new EventQueue();
        for (int i = 0; i < 500; i++) q.Schedule(rng.NextInt(1000), SimEventKind.FarmChunk, i);
        int prev = -1;
        while (q.Count > 0) { int t = q.Dequeue().Time; Assert.True(t >= prev); prev = t; }
    }

    [Fact]
    public void DequeueOnEmptyThrows()
        => Assert.Throws<InvalidOperationException>(() => new EventQueue().Dequeue());

    [Fact]
    public void EventCarriesTrainerTokenAndArg()
    {
        var q = new EventQueue();
        q.Schedule(7, SimEventKind.ServiceDone, trainerId: 3, token: 9, arg: 2);
        var e = q.Dequeue();
        Assert.Equal(3, e.TrainerId); Assert.Equal(9, e.Token); Assert.Equal(2, e.Arg);
    }
}
```

`tests/Game.Domain.Tests/SimRandomTests.cs`:

```csharp
using Xunit;
using Game.Domain;

public class SimRandomTests
{
    [Fact]
    public void SameSeedSameSequence()
    {
        var a = new SimRandom(42); var b = new SimRandom(42);
        for (int i = 0; i < 100; i++) Assert.Equal(a.NextDouble(), b.NextDouble());
    }

    [Fact]
    public void NextDoubleIsInUnitInterval()
    {
        var r = new SimRandom(1);
        for (int i = 0; i < 10_000; i++) Assert.InRange(r.NextDouble(), 0.0, 0.9999999999999999);
    }

    [Fact]
    public void NextIntStaysBelowBound()
    {
        var r = new SimRandom(2);
        for (int i = 0; i < 10_000; i++) Assert.InRange(r.NextInt(4), 0, 3);
    }

    [Fact]
    public void RestoringStateReplaysSequence()
    {
        var a = new SimRandom(3);
        a.NextDouble();
        ulong saved = a.State;
        double next = a.NextDouble();
        var b = new SimRandom(999) { State = saved };
        Assert.Equal(next, b.NextDouble());
    }
}
```

Run: `dotnet test tests/Game.Domain.Tests --filter "EventQueueTests|SimRandomTests"`
Expected: FAIL (build error: `EventQueue`, `SimRandom` không tồn tại).

- [ ] **Step 6: Viết EventQueue và SimRandom**

`src/Game.Domain/Simulation/EventQueue.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>Các loại sự kiện trong hàng đợi mô phỏng.</summary>
    public enum SimEventKind
    {
        TrainerDecide,      // Trainer chọn việc tiếp theo
        TrainerArriveZone,  // tới Zone, bắt đầu farm
        FarmChunk,          // hết một khúc farm
        TrainerArriveHub,   // về tới HUB
        ServiceDone,        // dùng xong một dịch vụ, nhả chỗ
        WaitTick,           // mỗi giờ chờ tiền: thử gọi Tổng tài
        Dawn,               // 06:00
        Dusk,               // 18:00
        DayStart,           // 00:00: trừ chi phí vận hành, giảm ngày đình công
        PaydayDue           // 23:59 ngày 30: RunFor dừng tại đây
    }

    /// <summary>Một sự kiện đã hẹn giờ. <see cref="Token"/> dùng để bỏ qua sự kiện cũ khi Trainer bị ngắt giữa chừng.</summary>
    public readonly struct SimEvent
    {
        public readonly int Time;
        public readonly long Seq;      // thứ tự vào hàng, để phá hòa khi cùng thời điểm
        public readonly SimEventKind Kind;
        public readonly int TrainerId; // -1 nếu không gắn với Trainer
        public readonly int Token;
        public readonly int Arg;

        public SimEvent(int time, long seq, SimEventKind kind, int trainerId, int token, int arg)
        {
            Time = time; Seq = seq; Kind = kind; TrainerId = trainerId; Token = token; Arg = arg;
        }
    }

    /// <summary>Hàng đợi ưu tiên (min-heap) theo (Time, Seq). Tất định: cùng thời điểm thì ra theo thứ tự vào.</summary>
    public sealed class EventQueue
    {
        readonly List<SimEvent> heap = new List<SimEvent>();
        long nextSeq;

        public int Count => heap.Count;

        /// <summary>Thời điểm của sự kiện sớm nhất. Ném lỗi nếu hàng đợi rỗng.</summary>
        public int PeekTime
        {
            get
            {
                if (heap.Count == 0) throw new InvalidOperationException("Hàng đợi sự kiện đang rỗng.");
                return heap[0].Time;
            }
        }

        /// <summary>Danh sách sự kiện đang chờ (không theo thứ tự), dùng để kiểm tra bất biến.</summary>
        public IReadOnlyList<SimEvent> Snapshot => heap;

        public void Schedule(int time, SimEventKind kind, int trainerId = -1, int token = 0, int arg = 0)
        {
            heap.Add(new SimEvent(time, nextSeq++, kind, trainerId, token, arg));
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (!Less(heap[i], heap[parent])) break;
                Swap(i, parent);
                i = parent;
            }
        }

        public SimEvent Dequeue()
        {
            if (heap.Count == 0) throw new InvalidOperationException("Hàng đợi sự kiện đang rỗng.");
            SimEvent top = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int left = 2 * i + 1, right = left + 1, smallest = i;
                if (left < heap.Count && Less(heap[left], heap[smallest])) smallest = left;
                if (right < heap.Count && Less(heap[right], heap[smallest])) smallest = right;
                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
            return top;
        }

        static bool Less(SimEvent a, SimEvent b) => a.Time < b.Time || (a.Time == b.Time && a.Seq < b.Seq);

        void Swap(int i, int j) { SimEvent tmp = heap[i]; heap[i] = heap[j]; heap[j] = tmp; }
    }
}
```

`src/Game.Domain/Simulation/SimRandom.cs`:

```csharp
namespace Game.Domain
{
    /// <summary>
    /// Bộ sinh số ngẫu nhiên tất định (SplitMix64). Tự cài đặt thay vì dùng System.Random
    /// để kết quả giống hệt giữa các nền tảng và lưu/khôi phục được trạng thái vào save.
    /// </summary>
    public sealed class SimRandom
    {
        ulong state;

        public SimRandom(int seed)
        {
            state = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x1234567UL;
        }

        /// <summary>Trạng thái nội bộ, lưu vào save để chạy tiếp đúng chuỗi.</summary>
        public ulong State { get => state; set => state = value; }

        ulong NextULong()
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Số thực trong [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

        /// <summary>Số nguyên trong [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive) => (int)(NextDouble() * maxExclusive);
    }
}
```

- [ ] **Step 7: Viết Personality và SimConfig**

`src/Game.Domain/Trainers/Personality.cs`:

```csharp
namespace Game.Domain
{
    /// <summary>Bốn tính cách của Trainer (docs/designs/03).</summary>
    public enum Personality { Warlike = 0, Timid = 1, Glutton = 2, Capitalist = 3 }
}
```

`src/Game.Domain/Config/SimConfig.cs`:

```csharp
using System;

namespace Game.Domain
{
    /// <summary>Bốn công trình dịch vụ của sub-project 1. Giá trị số dùng làm chỉ số mảng.</summary>
    public enum BuildingKind { Inn = 0, Restaurant = 1, Bar = 2, Hospital = 3 }

    /// <summary>Thông số cố định của một loại công trình dịch vụ.</summary>
    public sealed class BuildingSpec
    {
        public readonly BuildingKind Kind;
        /// <summary>Giá hợp lý. Riêng Bệnh Viện: giá trên mỗi 10 HP (14 tương đương 1.4 Gold/HP).</summary>
        public readonly long FairPrice;
        public readonly int ServiceMinutes;

        public BuildingSpec(BuildingKind kind, long fairPrice, int serviceMinutes)
        {
            Kind = kind; FairPrice = fairPrice; ServiceMinutes = serviceMinutes;
        }
    }

    /// <summary>
    /// Mọi tham số khởi điểm của mô phỏng. Quy đổi từ docs/designs/13 §8 (theo ngày) sang phút;
    /// là giá trị tạm, chỉnh khi cân bằng. Sau này nạp từ ScriptableObject.
    /// </summary>
    public sealed class SimConfig
    {
        // --- Khởi tạo ---
        public int TrainerCount = 10;
        /// <summary>Nếu có giá trị thì mọi Trainer dùng tính cách này (tiện cho test).</summary>
        public Personality? ForcedPersonality = null;
        public long StartTreasury = 20000;
        public long StartTrainerGold = 200;
        public int StartBuildingLevel = 5;
        /// <summary>Phút bắt đầu: 06:00 sáng ngày đầu tiên.</summary>
        public int StartMinute = SimClock.DawnMinute;

        // --- Nhu cầu (mỗi giờ) ---
        public double FieldStaminaPerHour = 6, FieldSatietyPerHour = 5, FieldHydrationPerHour = 6, FieldStressPerHour = 0.5;
        public double HubStaminaPerHour = 2, HubSatietyPerHour = 2, HubHydrationPerHour = 2;
        /// <summary>Thanh dưới mức này thì cần dịch vụ.</summary>
        public double SufficientNeed = 60;
        /// <summary>Ban đêm không có kính: chỉ vào Nhà Trọ khi Thể lực dưới mức này.</summary>
        public double NightSleepBelow = 90;
        public double BarStressThreshold = 70;
        public double BarStressTarget = 20;
        public double QueueStressPerHour = 2;
        public double WaitStressPerHour = 2;
        /// <summary>Stress cộng thêm = hệ số x (giá/giá hợp lý - 1) x độ nhạy giá.</summary>
        public double PriceStressFactor = 10;

        // --- Farm và chợ (tạm, sub-project 2 và 3 thay thế) ---
        public int FarmChunkMinutes = 30;
        public int ZoneTravelMinutes = 30;
        public int FarmMaterialsPerChunk = 3;
        public long FarmGoldPerChunk = 2;
        public long FarmHpLostPerChunk = 3;
        public int BackpackCapacity = 30;
        public long TeamHpMax = 300;
        public long MaterialPrice = 10;
        public double TaxRate = 0.20;

        // --- Dịch vụ ---
        public double ServiceCogs = 0.25;
        public long UpkeepPerBuildingPerDay = 50;
        public BuildingSpec[] Buildings =
        {
            new BuildingSpec(BuildingKind.Inn, 45, 360),
            new BuildingSpec(BuildingKind.Restaurant, 40, 30),
            new BuildingSpec(BuildingKind.Bar, 800, 60),
            new BuildingSpec(BuildingKind.Hospital, 14, 30),
        };

        // --- Hết tiền ---
        public double PatronChancePerHour = 0.10;
        public int PatronGuaranteedAfterHours = 24;

        // --- Lương và vỡ nợ ---
        public long BaseWage = 2900;
        public double RarityGrowth = 1.7;
        public double CapitalistWageMultiplier = 1.3;
        public int StrikeDays = 5;
        public double StressOnSecondMiss = 30;
        public double DebtModeFarmMultiplier = 0.5;

        public static SimConfig Default => new SimConfig();

        /// <summary>Lương hợp đồng khởi điểm: BaseWage x RarityGrowth^bậc, Tư bản đòi thêm.</summary>
        public long ContractWageFor(Rarity rarity, Personality personality)
        {
            double wage = BaseWage * Math.Pow(RarityGrowth, (int)rarity);
            if (personality == Personality.Capitalist) wage *= CapitalistWageMultiplier;
            return (long)Math.Round(wage);
        }
    }
}
```

- [ ] **Step 8: Đồng bộ spec §9 và kiểm tra build/test**

Các tham số trên khác spec ở 4 chỗ (Stress farm 0.5/giờ, 3 nguyên liệu và 3 HP mỗi khúc, giá nguyên liệu 10) vì giá trị cũ làm Trainer vào Bar mỗi 1.5 ngày và lỗ liên tục so với mô hình cũ (Bar mỗi ~4 ngày). Cập nhật spec cho khớp:

```bash
python3 - <<'EOF'
p = 'docs/superpowers/specs/2026-10-01-domain-core-time-trainer-design.md'
s = open(p, encoding='utf-8').read()
pairs = [
 ("Stamina -6, Satiety -5, Hydration -6, Stress +2 |", "Stamina -6, Satiety -5, Hydration -6, Stress +0.5 |"),
 ("2 đơn vị nguyên liệu x tỉ lệ nhặt; 2 Gold; mất 8 HP;", "3 đơn vị nguyên liệu x tỉ lệ nhặt; 2 Gold; mất 3 HP;"),
 ("trả Trainer 8 Gold/đơn vị", "trả Trainer 10 Gold/đơn vị"),
]
for a, b in pairs:
    print(('ĐÃ SỬA ' if a in s else 'KHÔNG THẤY ') + a[:40])
    s = s.replace(a, b)
open(p, 'w', encoding='utf-8').write(s)
EOF
dotnet test tests/Game.Domain.Tests
```
Expected: ba dòng `ĐÃ SỬA`; test `Passed!`.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat: add simulation clock, event queue, deterministic RNG and SimConfig" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Dữ liệu Trainer, tính cách và bộ não quyết định

**Files:**
- Create: `src/Game.Domain/Trainers/Trainer.cs`, `src/Game.Domain/Trainers/PersonalityProfile.cs`, `src/Game.Domain/Trainers/TrainerBrain.cs`, `src/Game.Domain/Hub/TreasuryAccount.cs`
- Test: `tests/Game.Domain.Tests/TestHelpers.cs`, `TrainerBrainTests.cs`, `TreasuryAccountTests.cs`

**Interfaces:**
- Consumes: `Personality`, `BuildingKind`, `SimConfig` (Task 2); `Rarity` (có sẵn).
- Produces:
  - `enum TrainerState { AtHub, Traveling, Farming, Returning, Queued, InService, WaitingForMoney }`.
  - `enum ReturnReason { None, Strike, Night, TeamDown, BackpackFull, Tired, Hungry, Thirsty }`.
  - `class Needs { double Stamina, Satiety, Hydration, Stress; double LowestPhysical; void Clamp(); }` (mặc định 100/100/100/0).
  - `class Trainer` với các trường: `int Id; Rarity Rarity; int Rank; int Level; Personality Personality; Needs Needs; long Gold; long TeamHp, TeamHpMax; int BackpackUnits, BackpackCapacity; bool HasNightVision; long ContractWage, WageOwed, WageAdvance; int StrikeDaysLeft; TrainerState State; string StateReason; int Token; int LastSettleMinute; int WaitingSinceMinute; BuildingKind PendingService; bool IsOnStrike`.
  - `PersonalityProfile.Of(Personality)` với trường `StaminaThreshold, SatietyThreshold, LootMult, HpLossMult, SatietyDecayMult, PriceDiscount, PriceSensitivity, MaterialPickRate` (đều `double`).
  - `TrainerBrain.ShouldReturn(Trainer t, bool isNight) : ReturnReason` và `TrainerBrain.PickService(Trainer t, SimConfig cfg, bool isNight) : BuildingKind?`.
  - `TreasuryAccount(long initial)`: `long Balance`, `void Add(long)`, `bool TrySpend(long)`.
  - Test helper: `TestTrainers.Make(int id = 0, Personality personality = Personality.Timid, long wage = 1000) : Trainer`.

- [ ] **Step 1: Viết test helper và test thất bại**

`tests/Game.Domain.Tests/TestHelpers.cs`:

```csharp
using Game.Domain;

/// <summary>Tạo Trainer tối giản cho test đơn vị.</summary>
public static class TestTrainers
{
    public static Trainer Make(int id = 0, Personality personality = Personality.Timid, long wage = 1000) => new Trainer
    {
        Id = id, Rarity = Rarity.Common, Personality = personality,
        TeamHp = 300, TeamHpMax = 300, BackpackCapacity = 30, ContractWage = wage,
    };
}
```

`tests/Game.Domain.Tests/TrainerBrainTests.cs`:

```csharp
using Xunit;
using Game.Domain;

public class TrainerBrainTests
{
    static readonly SimConfig Cfg = SimConfig.Default;

    // ---- ShouldReturn ----
    [Fact]
    public void TimidReturnsWhenStaminaBelowFifty()
    {
        var t = TestTrainers.Make(personality: Personality.Timid);
        t.Needs.Stamina = 49;
        Assert.Equal(ReturnReason.Tired, TrainerBrain.ShouldReturn(t, false));
        t.Needs.Stamina = 50;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void WarlikeStaysOutUntilStaminaBelowFifteen()
    {
        var t = TestTrainers.Make(personality: Personality.Warlike);
        t.Needs.Stamina = 49;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, false));
        t.Needs.Stamina = 14;
        Assert.Equal(ReturnReason.Tired, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void GluttonReturnsHungryBelowFiftySatiety()
    {
        var t = TestTrainers.Make(personality: Personality.Glutton);
        t.Needs.Satiety = 49;
        Assert.Equal(ReturnReason.Hungry, TrainerBrain.ShouldReturn(t, false));
        t.Needs.Satiety = 51;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void CapitalistReturnsThirstyBelowTwentyFive()
    {
        var t = TestTrainers.Make(personality: Personality.Capitalist);
        t.Needs.Hydration = 24;
        Assert.Equal(ReturnReason.Thirsty, TrainerBrain.ShouldReturn(t, false));
        t.Needs.Hydration = 26;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void ReturnsWhenBackpackFullOrTeamDown()
    {
        var t = TestTrainers.Make();
        t.BackpackUnits = 30;
        Assert.Equal(ReturnReason.BackpackFull, TrainerBrain.ShouldReturn(t, false));
        t.BackpackUnits = 0; t.TeamHp = 0;
        Assert.Equal(ReturnReason.TeamDown, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void NightForcesReturnOnlyWithoutNightVision()
    {
        var t = TestTrainers.Make();
        Assert.Equal(ReturnReason.Night, TrainerBrain.ShouldReturn(t, true));
        t.HasNightVision = true;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, true));
    }

    [Fact]
    public void StrikeForcesReturn()
    {
        var t = TestTrainers.Make();
        t.StrikeDaysLeft = 2;
        Assert.Equal(ReturnReason.Strike, TrainerBrain.ShouldReturn(t, false));
    }

    // ---- PickService ----
    [Fact]
    public void FullStressGoesToBarBeforeAnythingElse()
    {
        var t = TestTrainers.Make();
        t.Needs.Stress = 100; t.TeamHp = 10; t.Needs.Satiety = 5;
        Assert.Equal(BuildingKind.Bar, TrainerBrain.PickService(t, Cfg, false));
    }

    [Fact]
    public void MissingHpGoesToHospital()
    {
        var t = TestTrainers.Make();
        t.TeamHp = 299; t.Needs.Satiety = 10;
        Assert.Equal(BuildingKind.Hospital, TrainerBrain.PickService(t, Cfg, false));
    }

    [Fact]
    public void NightWithoutVisionSleepsOnlyIfTired()
    {
        var t = TestTrainers.Make();
        t.Needs.Stamina = 80;
        Assert.Equal(BuildingKind.Inn, TrainerBrain.PickService(t, Cfg, true));
        t.Needs.Stamina = 95;
        Assert.Null(TrainerBrain.PickService(t, Cfg, true));
    }

    [Fact]
    public void LowestNeedBelowSufficientPicksItsBuilding()
    {
        var t = TestTrainers.Make();
        t.Needs.Satiety = 50;
        Assert.Equal(BuildingKind.Restaurant, TrainerBrain.PickService(t, Cfg, false));

        t = TestTrainers.Make(); t.Needs.Stamina = 40; t.Needs.Satiety = 55;
        Assert.Equal(BuildingKind.Inn, TrainerBrain.PickService(t, Cfg, false));

        t = TestTrainers.Make(); t.Needs.Hydration = 30; t.Needs.Stamina = 50;
        Assert.Equal(BuildingKind.Restaurant, TrainerBrain.PickService(t, Cfg, false));
    }

    [Fact]
    public void HighStressGoesToBarWhenNothingElseNeeded()
    {
        var t = TestTrainers.Make();
        t.Needs.Stress = 75;
        Assert.Equal(BuildingKind.Bar, TrainerBrain.PickService(t, Cfg, false));
        t.Needs.Stress = 69;
        Assert.Null(TrainerBrain.PickService(t, Cfg, false));
    }
}
```

`tests/Game.Domain.Tests/TreasuryAccountTests.cs`:

```csharp
using System;
using Xunit;
using Game.Domain;

public class TreasuryAccountTests
{
    [Fact]
    public void SpendFailsWithoutChangingBalanceWhenInsufficient()
    {
        var t = new TreasuryAccount(100);
        Assert.False(t.TrySpend(101));
        Assert.Equal(100, t.Balance);
        Assert.True(t.TrySpend(100));
        Assert.Equal(0, t.Balance);
    }

    [Fact]
    public void AddIncreasesBalance()
    {
        var t = new TreasuryAccount(0);
        t.Add(50);
        Assert.Equal(50, t.Balance);
    }

    [Fact]
    public void NegativeArgumentsAreProgrammingErrors()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreasuryAccount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreasuryAccount(0).Add(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreasuryAccount(0).TrySpend(-1));
    }
}
```

- [ ] **Step 2: Chạy để thấy lỗi biên dịch**

Run: `dotnet test tests/Game.Domain.Tests --filter "TrainerBrainTests|TreasuryAccountTests"`
Expected: FAIL (build error: `Trainer`, `TrainerBrain`, `TreasuryAccount` không tồn tại).

- [ ] **Step 3: Viết Trainer, PersonalityProfile, TrainerBrain, TreasuryAccount**

`src/Game.Domain/Trainers/Trainer.cs`:

```csharp
using System;

namespace Game.Domain
{
    /// <summary>Trạng thái hiện tại của Trainer trong FSM.</summary>
    public enum TrainerState { AtHub, Traveling, Farming, Returning, Queued, InService, WaitingForMoney }

    /// <summary>Lý do Trainer quyết định về HUB. <see cref="None"/> nghĩa là tiếp tục farm.</summary>
    public enum ReturnReason { None, Strike, Night, TeamDown, BackpackFull, Tired, Hungry, Thirsty }

    /// <summary>Bốn thanh nhu cầu, thang 0-100. Thể lực/No nê/Nước hồi ở dịch vụ; Stress càng cao càng tệ.</summary>
    public sealed class Needs
    {
        public double Stamina = 100;
        public double Satiety = 100;
        public double Hydration = 100;
        public double Stress = 0;

        /// <summary>Thấp nhất trong ba thanh thể chất (không tính Stress).</summary>
        public double LowestPhysical => Math.Min(Stamina, Math.Min(Satiety, Hydration));

        /// <summary>Ép mọi thanh về khoảng 0-100.</summary>
        public void Clamp()
        {
            Stamina = Math.Max(0, Math.Min(100, Stamina));
            Satiety = Math.Max(0, Math.Min(100, Satiety));
            Hydration = Math.Max(0, Math.Min(100, Hydration));
            Stress = Math.Max(0, Math.Min(100, Stress));
        }
    }

    /// <summary>
    /// Dữ liệu phẳng của một Trainer. Chỉ chứa dữ liệu; logic nằm ở TrainerBrain và HubWorld.
    /// Trainer không có HP riêng: <see cref="TeamHp"/> là HP gộp của 3 Monster (tạm, sub-project 3 thay).
    /// </summary>
    public sealed class Trainer
    {
        public int Id;
        public Rarity Rarity;
        public int Rank = 1;
        public int Level = 1;
        public Personality Personality;
        public readonly Needs Needs = new Needs();
        public long Gold;
        public long TeamHp;
        public long TeamHpMax;
        public int BackpackUnits;
        public int BackpackCapacity;
        /// <summary>Cờ tạm thay cho slot Kính (sub-project 4).</summary>
        public bool HasNightVision;

        // --- Lương ---
        public long ContractWage;
        /// <summary>HUB còn nợ Trainer (nợ lương cộng dồn).</summary>
        public long WageOwed;
        /// <summary>Trainer đã ứng trước, trừ vào Payday kế tiếp.</summary>
        public long WageAdvance;
        public int StrikeDaysLeft;

        public TrainerState State;
        public string StateReason = "";

        // --- Nội bộ của động cơ ---
        /// <summary>Tăng mỗi khi ngắt Trainer giữa chừng; sự kiện mang token cũ sẽ bị bỏ qua.</summary>
        public int Token;
        /// <summary>Lần cuối trừ/cộng nhu cầu theo thời gian trôi.</summary>
        public int LastSettleMinute;
        public int WaitingSinceMinute;
        public BuildingKind PendingService;

        public bool IsOnStrike => StrikeDaysLeft > 0;
    }
}
```

`src/Game.Domain/Trainers/PersonalityProfile.cs`:

```csharp
namespace Game.Domain
{
    /// <summary>Hệ số hành vi theo tính cách (giá trị khởi điểm, docs/designs/03 và spec §9).</summary>
    public sealed class PersonalityProfile
    {
        /// <summary>Ngưỡng về HUB cho Thể lực và Nước (thanh dưới ngưỡng thì về).</summary>
        public double StaminaThreshold;
        /// <summary>Ngưỡng về HUB cho No nê.</summary>
        public double SatietyThreshold;
        /// <summary>Hệ số lượng loot (Gold và nguyên liệu).</summary>
        public double LootMult;
        /// <summary>Hệ số HP Monster mất mỗi khúc farm.</summary>
        public double HpLossMult;
        /// <summary>Hệ số tốc độ tụt No nê.</summary>
        public double SatietyDecayMult;
        /// <summary>Tỉ lệ giảm giá dịch vụ HUB (0.10 = giảm 10%).</summary>
        public double PriceDiscount;
        /// <summary>Độ nhạy với giá cao: nhân vào Stress cộng thêm.</summary>
        public double PriceSensitivity;
        /// <summary>Tỉ lệ nguyên liệu nhặt (phần còn lại bị bỏ lại).</summary>
        public double MaterialPickRate;

        static readonly PersonalityProfile[] Table =
        {
            // Háo chiến
            new PersonalityProfile { StaminaThreshold = 15, SatietyThreshold = 15, LootMult = 1.25, HpLossMult = 1.5, SatietyDecayMult = 1.0, PriceDiscount = 0.0, PriceSensitivity = 1.0, MaterialPickRate = 0.85 },
            // Nhát gan
            new PersonalityProfile { StaminaThreshold = 50, SatietyThreshold = 50, LootMult = 0.85, HpLossMult = 0.5, SatietyDecayMult = 1.0, PriceDiscount = 0.0, PriceSensitivity = 1.0, MaterialPickRate = 0.85 },
            // Tham ăn
            new PersonalityProfile { StaminaThreshold = 30, SatietyThreshold = 50, LootMult = 1.0, HpLossMult = 1.0, SatietyDecayMult = 1.6, PriceDiscount = 0.0, PriceSensitivity = 1.0, MaterialPickRate = 0.85 },
            // Tư bản
            new PersonalityProfile { StaminaThreshold = 25, SatietyThreshold = 25, LootMult = 1.0, HpLossMult = 1.0, SatietyDecayMult = 1.0, PriceDiscount = 0.10, PriceSensitivity = 1.5, MaterialPickRate = 1.0 },
        };

        public static PersonalityProfile Of(Personality personality) => Table[(int)personality];
    }
}
```

`src/Game.Domain/Trainers/TrainerBrain.cs`:

```csharp
namespace Game.Domain
{
    /// <summary>Các quyết định thuần (không có trạng thái): có nên về HUB không, và nên dùng dịch vụ nào.</summary>
    public static class TrainerBrain
    {
        /// <summary>
        /// Trả về lý do về HUB (điều kiện đầu tiên gặp), hoặc <see cref="ReturnReason.None"/> nếu tiếp tục farm.
        /// Thứ tự: đình công, ban đêm không kính, Monster cạn HP, Balo đầy, rồi các thanh dưới ngưỡng tính cách.
        /// </summary>
        public static ReturnReason ShouldReturn(Trainer t, bool isNight)
        {
            PersonalityProfile p = PersonalityProfile.Of(t.Personality);
            if (t.IsOnStrike) return ReturnReason.Strike;
            if (isNight && !t.HasNightVision) return ReturnReason.Night;
            if (t.TeamHp <= 0) return ReturnReason.TeamDown;
            if (t.BackpackUnits >= t.BackpackCapacity) return ReturnReason.BackpackFull;
            if (t.Needs.Stamina < p.StaminaThreshold) return ReturnReason.Tired;
            if (t.Needs.Satiety < p.SatietyThreshold) return ReturnReason.Hungry;
            if (t.Needs.Hydration < p.StaminaThreshold) return ReturnReason.Thirsty;
            return ReturnReason.None;
        }

        /// <summary>
        /// Chọn dịch vụ cần dùng ở HUB, hoặc null nếu không cần gì. Thứ tự ưu tiên:
        /// 1) Stress đầy: Bar. 2) Thiếu HP: Bệnh Viện. 3) Ban đêm không kính và hơi mệt: Nhà Trọ.
        /// 4) Thanh thể chất thấp nhất dưới mức "đủ": công trình của thanh đó. 5) Stress cao: Bar.
        /// </summary>
        public static BuildingKind? PickService(Trainer t, SimConfig cfg, bool isNight)
        {
            Needs n = t.Needs;
            if (n.Stress >= 100) return BuildingKind.Bar;
            if (t.TeamHp < t.TeamHpMax) return BuildingKind.Hospital;
            if (isNight && !t.HasNightVision && n.Stamina < cfg.NightSleepBelow) return BuildingKind.Inn;
            if (n.LowestPhysical < cfg.SufficientNeed)
            {
                bool staminaIsLowest = n.Stamina <= n.Satiety && n.Stamina <= n.Hydration;
                return staminaIsLowest ? BuildingKind.Inn : BuildingKind.Restaurant;   // No nê và Nước cùng hồi ở Nhà Hàng
            }
            if (n.Stress >= cfg.BarStressThreshold) return BuildingKind.Bar;
            return null;
        }
    }
}
```

`src/Game.Domain/Hub/TreasuryAccount.cs`:

```csharp
using System;

namespace Game.Domain
{
    /// <summary>Kho bạc của Giám đốc. Số dư không bao giờ âm.</summary>
    public sealed class TreasuryAccount
    {
        public long Balance { get; private set; }

        public TreasuryAccount(long initial)
        {
            if (initial < 0) throw new ArgumentOutOfRangeException(nameof(initial));
            Balance = initial;
        }

        public void Add(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Balance += amount;
        }

        /// <summary>Trừ tiền nếu đủ; trả về false và giữ nguyên số dư nếu không đủ.</summary>
        public bool TrySpend(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > Balance) return false;
            Balance -= amount;
            return true;
        }
    }
}
```

- [ ] **Step 4: Chạy test**

Run: `dotnet test tests/Game.Domain.Tests`
Expected: PASS toàn bộ.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add Trainer data, personality profiles, brain decisions and treasury" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Công trình dịch vụ

**Files:**
- Create: `src/Game.Domain/Buildings/ServiceBuilding.cs`
- Test: `tests/Game.Domain.Tests/ServiceBuildingTests.cs`

**Interfaces:**
- Consumes: `BuildingKind`, `BuildingSpec`, `SimClock` (Task 2); `PersonalityProfile` (Task 3).
- Produces: `class ServiceBuilding`:
  - ctor `ServiceBuilding(BuildingSpec spec, int level, long upkeepPerDay)`.
  - Trường/thuộc tính: `BuildingKind Kind; int Level; long FairPrice; long Price (khởi tạo = FairPrice); int BaseServiceMinutes; long UpkeepPerDay; bool Maintained = true; int FullSlots; int Slots; int Occupied; int QueueLength; int MaxQueueLength; IReadOnlyList<int> Occupants`.
  - `void Enqueue(int trainerId)`, `List<int> PromoteWaiting()` (chuyển người chờ vào chỗ trống theo FIFO, trả về những id vừa được xếp chỗ), `void Leave(int trainerId)` (nhả chỗ, không tự gọi người kế tiếp), `long PriceFor(PersonalityProfile profile, long missingHp)`, `int ServiceMinutesFor(int nowMinute)`.

- [ ] **Step 1: Viết test thất bại**

`tests/Game.Domain.Tests/ServiceBuildingTests.cs`:

```csharp
using System.Linq;
using Xunit;
using Game.Domain;

public class ServiceBuildingTests
{
    static ServiceBuilding Make(BuildingKind kind, int level)
        => new ServiceBuilding(SimConfig.Default.Buildings.First(b => b.Kind == kind), level, 50);

    [Theory]
    [InlineData(1, 5)]
    [InlineData(5, 9)]
    [InlineData(10, 13)]
    [InlineData(25, 25)]
    public void SlotsAreFivePlusEightyPercentOfLevel(int level, int expected)
        => Assert.Equal(expected, Make(BuildingKind.Inn, level).Slots);

    [Fact]
    public void MaintenanceLossHalvesSlots()
    {
        var b = Make(BuildingKind.Inn, 5);
        b.Maintained = false;
        Assert.Equal(4, b.Slots);   // 9 / 2
    }

    [Fact]
    public void WaitingTrainersAreSeatedInFifoOrder()
    {
        var b = Make(BuildingKind.Inn, 1);   // 5 chỗ
        for (int id = 0; id < 7; id++) b.Enqueue(id);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, b.PromoteWaiting());
        Assert.Equal(2, b.QueueLength);
        Assert.Equal(2, b.MaxQueueLength);

        b.Leave(0);
        Assert.Equal(new[] { 5 }, b.PromoteWaiting());
        b.Leave(1);
        Assert.Equal(new[] { 6 }, b.PromoteWaiting());
        Assert.Equal(0, b.QueueLength);
        Assert.Empty(b.PromoteWaiting());
    }

    [Fact]
    public void CapitalistGetsTenPercentDiscountRoundedUp()
    {
        var b = Make(BuildingKind.Inn, 5);   // giá 45
        Assert.Equal(41, b.PriceFor(PersonalityProfile.Of(Personality.Capitalist), 0));   // ceil(40.5)
        Assert.Equal(45, b.PriceFor(PersonalityProfile.Of(Personality.Timid), 0));
    }

    [Fact]
    public void HospitalPriceIsPerTenHpRoundedUp()
    {
        var b = Make(BuildingKind.Hospital, 5);   // 14 Gold mỗi 10 HP
        var timid = PersonalityProfile.Of(Personality.Timid);
        Assert.Equal(0, b.PriceFor(timid, 0));
        Assert.Equal(14, b.PriceFor(timid, 1));
        Assert.Equal(14, b.PriceFor(timid, 10));
        Assert.Equal(42, b.PriceFor(timid, 25));
    }

    [Fact]
    public void InnSleepsUntilDawnAtNightWithMinimumHalfHour()
    {
        var inn = Make(BuildingKind.Inn, 5);
        Assert.Equal(360, inn.ServiceMinutesFor(600));            // ban ngày: đủ 360
        Assert.Equal(360, inn.ServiceMinutesFor(1080));           // 18:00: còn 720 phút tới sáng, chặn ở 360
        Assert.Equal(180, inn.ServiceMinutesFor(1440 + 180));     // 03:00: còn 180 phút
        Assert.Equal(30, inn.ServiceMinutesFor(1440 + 350));      // 05:50: còn 10 phút, tối thiểu 30
    }

    [Fact]
    public void OtherBuildingsUseBaseServiceMinutes()
    {
        Assert.Equal(30, Make(BuildingKind.Restaurant, 5).ServiceMinutesFor(1100));
        Assert.Equal(60, Make(BuildingKind.Bar, 5).ServiceMinutesFor(1100));
    }

    [Fact]
    public void PriceStartsAtFairPrice()
    {
        var b = Make(BuildingKind.Bar, 5);
        Assert.Equal(800, b.FairPrice);
        Assert.Equal(800, b.Price);
    }
}
```

- [ ] **Step 2: Chạy để thấy lỗi biên dịch**

Run: `dotnet test tests/Game.Domain.Tests --filter ServiceBuildingTests`
Expected: FAIL (build error: `ServiceBuilding` không tồn tại).

- [ ] **Step 3: Viết ServiceBuilding**

`src/Game.Domain/Buildings/ServiceBuilding.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>
    /// Công trình dịch vụ: số chỗ cố định, hàng đợi FIFO, giá do Giám đốc đặt.
    /// Lớp này chỉ giữ chỗ và hàng đợi; việc thanh toán và hồi nhu cầu do HubWorld xử lý.
    /// </summary>
    public sealed class ServiceBuilding
    {
        readonly List<int> occupants = new List<int>();
        readonly Queue<int> waiting = new Queue<int>();

        public readonly BuildingKind Kind;
        public int Level;
        public readonly long FairPrice;
        /// <summary>Giá Giám đốc đặt. Bệnh Viện: giá trên mỗi 10 HP.</summary>
        public long Price;
        public readonly int BaseServiceMinutes;
        public readonly long UpkeepPerDay;
        /// <summary>false khi thiếu tiền vận hành (Thiếu bảo trì): số chỗ chia đôi.</summary>
        public bool Maintained = true;

        public ServiceBuilding(BuildingSpec spec, int level, long upkeepPerDay)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            Kind = spec.Kind; Level = level;
            FairPrice = spec.FairPrice; Price = spec.FairPrice;
            BaseServiceMinutes = spec.ServiceMinutes; UpkeepPerDay = upkeepPerDay;
        }

        /// <summary>Số chỗ khi vận hành bình thường: 5 + floor(0.8 x cấp), tính bằng số nguyên.</summary>
        public int FullSlots => 5 + (Level * 4) / 5;
        public int Slots => Maintained ? FullSlots : FullSlots / 2;
        public int Occupied => occupants.Count;
        public int QueueLength => waiting.Count;
        /// <summary>Độ dài hàng đợi lớn nhất còn lại sau khi xếp chỗ (dùng cho báo cáo Game.Sim).</summary>
        public int MaxQueueLength { get; private set; }
        public IReadOnlyList<int> Occupants => occupants;

        /// <summary>Đưa Trainer vào hàng đợi. Chưa xếp chỗ; gọi <see cref="PromoteWaiting"/> sau đó.</summary>
        public void Enqueue(int trainerId)
        {
            waiting.Enqueue(trainerId);
        }

        /// <summary>Xếp người đang chờ vào các chỗ trống theo thứ tự vào hàng; trả về các id vừa được xếp chỗ.</summary>
        public List<int> PromoteWaiting()
        {
            var seated = new List<int>();
            while (waiting.Count > 0 && occupants.Count < Slots)
            {
                int id = waiting.Dequeue();
                occupants.Add(id);
                seated.Add(id);
            }
            // Ghi nhận hàng đợi dài nhất SAU khi xếp chỗ: chỉ tính những người thật sự phải chờ.
            if (waiting.Count > MaxQueueLength) MaxQueueLength = waiting.Count;
            return seated;
        }

        /// <summary>Nhả chỗ của một Trainer. Không tự gọi người kế tiếp.</summary>
        public void Leave(int trainerId) => occupants.Remove(trainerId);

        /// <summary>
        /// Giá một lượt cho Trainer này, đã trừ giảm giá tính cách (làm tròn lên).
        /// Bệnh Viện tính theo mỗi 10 HP còn thiếu (làm tròn lên số đơn vị).
        /// </summary>
        public long PriceFor(PersonalityProfile profile, long missingHp)
        {
            double raw = Price;
            if (Kind == BuildingKind.Hospital)
            {
                if (missingHp <= 0) return 0;
                raw = Price * ((missingHp + 9) / 10);
            }
            return (long)Math.Ceiling(raw * (1.0 - profile.PriceDiscount));
        }

        /// <summary>
        /// Thời gian phục vụ một lượt. Nhà Trọ ban đêm ngủ tới sáng (tối đa 360 phút, tối thiểu 30 phút).
        /// </summary>
        public int ServiceMinutesFor(int nowMinute)
        {
            if (Kind == BuildingKind.Inn && SimClock.IsNight(nowMinute))
            {
                int untilDawn = SimClock.NextMinuteOfDay(nowMinute, SimClock.DawnMinute) - nowMinute;
                return Math.Max(30, Math.Min(BaseServiceMinutes, untilDawn));
            }
            return BaseServiceMinutes;
        }
    }
}
```

- [ ] **Step 4: Chạy test**

Run: `dotnet test tests/Game.Domain.Tests --filter ServiceBuildingTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add service building with slots, FIFO queue and pricing" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Farm tạm, chợ tạm và lương

**Files:**
- Create: `src/Game.Domain/World/FarmAndMarket.cs`, `src/Game.Domain/Hub/Payroll.cs`
- Test: `tests/Game.Domain.Tests/FarmAndMarketTests.cs`, `PayrollTests.cs`

**Interfaces:**
- Consumes: `Trainer`, `PersonalityProfile` (Task 3); `SimConfig`, `SimRandom`, `Rarity` (Task 2); `TreasuryAccount` (Task 3).
- Produces:
  - `record FarmResult(int MaterialUnits, long Gold, long HpLost)`; `interface IFarmResolver { FarmResult Resolve(Trainer trainer, int minutes); }`; `SimpleFarmResolver(SimConfig cfg, SimRandom rng)`.
  - `record SaleResult(long GrossToTrainer, long Tax)`; `interface IMaterialMarket { SaleResult Quote(Trainer seller, int units); }`; `FixedPriceMarket(SimConfig cfg)`. Trainer nhận `GrossToTrainer - Tax`, Kho bạc nhận `Tax`.
  - `record PaydayOutcome(long TotalDue, long TotalPaid, double PaidRatio, bool StrikeStarted, int UnpaidStreak)`.
  - `class Payroll`: `int UnpaidStreak {get;}`, `bool DebtMode` (`UnpaidStreak >= 3`), `static long DueOf(Trainer t)` = `max(0, ContractWage + WageOwed - WageAdvance)`, `long TotalDue(IReadOnlyList<Trainer> trainers)`, `PaydayOutcome Resolve(IReadOnlyList<Trainer> trainers, TreasuryAccount treasury, SimConfig cfg)`.

- [ ] **Step 1: Viết test thất bại**

`tests/Game.Domain.Tests/FarmAndMarketTests.cs`:

```csharp
using Xunit;
using Game.Domain;

public class FarmAndMarketTests
{
    [Fact]
    public void FarmResultIsDeterministicForSameSeed()
    {
        var cfg = SimConfig.Default;
        var t = TestTrainers.Make(personality: Personality.Capitalist);
        var a = new SimpleFarmResolver(cfg, new SimRandom(7));
        var b = new SimpleFarmResolver(cfg, new SimRandom(7));
        for (int i = 0; i < 20; i++) Assert.Equal(a.Resolve(t, 30), b.Resolve(t, 30));
    }

    [Fact]
    public void FarmResultIsNonNegative()
    {
        var r = new SimpleFarmResolver(SimConfig.Default, new SimRandom(1));
        foreach (Personality p in new[] { Personality.Warlike, Personality.Timid, Personality.Glutton, Personality.Capitalist })
        {
            var res = r.Resolve(TestTrainers.Make(personality: p), 30);
            Assert.True(res.MaterialUnits >= 0 && res.Gold >= 0 && res.HpLost >= 0);
        }
    }

    [Fact]
    public void WarlikeLosesMoreHpAndTimidLess()
    {
        var cfg = SimConfig.Default;
        long Total(Personality p)
        {
            var resolver = new SimpleFarmResolver(cfg, new SimRandom(3));
            long hp = 0;
            for (int i = 0; i < 200; i++) hp += resolver.Resolve(TestTrainers.Make(personality: p), 30).HpLost;
            return hp;
        }
        Assert.True(Total(Personality.Warlike) > Total(Personality.Timid));
    }

    [Fact]
    public void CapitalistPicksUpMoreMaterialsThanTimid()
    {
        var cfg = SimConfig.Default;
        int Total(Personality p)
        {
            var resolver = new SimpleFarmResolver(cfg, new SimRandom(4));
            int units = 0;
            for (int i = 0; i < 200; i++) units += resolver.Resolve(TestTrainers.Make(personality: p), 30).MaterialUnits;
            return units;
        }
        Assert.True(Total(Personality.Capitalist) > Total(Personality.Timid));
    }

    [Fact]
    public void FixedPriceMarketTaxesTwentyPercentOfGross()
    {
        var market = new FixedPriceMarket(SimConfig.Default);   // 10 Gold/đơn vị, thuế 20%
        var sale = market.Quote(TestTrainers.Make(), 30);
        Assert.Equal(300, sale.GrossToTrainer);
        Assert.Equal(60, sale.Tax);
    }

    [Fact]
    public void SellingNothingYieldsNothing()
    {
        var sale = new FixedPriceMarket(SimConfig.Default).Quote(TestTrainers.Make(), 0);
        Assert.Equal(0, sale.GrossToTrainer);
        Assert.Equal(0, sale.Tax);
    }
}
```

`tests/Game.Domain.Tests/PayrollTests.cs`:

```csharp
using System.Linq;
using Xunit;
using Game.Domain;

public class PayrollTests
{
    static readonly SimConfig Cfg = SimConfig.Default;

    static Trainer[] Team(int n, long wage) => Enumerable.Range(0, n).Select(i => TestTrainers.Make(i, wage: wage)).ToArray();

    [Fact]
    public void FullPaymentWhenTreasuryCoversEveryone()
    {
        var team = Team(3, 1000);
        var treasury = new TreasuryAccount(5000);
        var payroll = new Payroll();
        var outcome = payroll.Resolve(team, treasury, Cfg);

        Assert.Equal(3000, outcome.TotalDue);
        Assert.Equal(3000, outcome.TotalPaid);
        Assert.Equal(1.0, outcome.PaidRatio);
        Assert.False(outcome.StrikeStarted);
        Assert.Equal(2000, treasury.Balance);
        Assert.All(team, t => { Assert.Equal(1000, t.Gold); Assert.False(t.IsOnStrike); });
        Assert.Equal(0, payroll.UnpaidStreak);
    }

    [Fact]
    public void ShortfallPaysProportionallyAndEveryoneStrikes()
    {
        var team = Team(3, 1000);
        var treasury = new TreasuryAccount(1500);
        var payroll = new Payroll();
        var outcome = payroll.Resolve(team, treasury, Cfg);

        Assert.True(outcome.StrikeStarted);
        Assert.Equal(0.5, outcome.PaidRatio, 6);
        Assert.Equal(0, treasury.Balance);
        Assert.All(team, t =>
        {
            Assert.Equal(500, t.Gold);
            Assert.Equal(500, t.WageOwed);
            Assert.Equal(Cfg.StrikeDays, t.StrikeDaysLeft);
        });
        Assert.Equal(1, payroll.UnpaidStreak);
    }

    [Fact]
    public void RoundingRemainderIsGivenToFirstTrainersSoTreasuryDrainsToZero()
    {
        var team = Team(3, 10);
        var treasury = new TreasuryAccount(20);
        new Payroll().Resolve(team, treasury, Cfg);
        Assert.Equal(new long[] { 7, 7, 6 }, team.Select(t => t.Gold).ToArray());
        Assert.Equal(0, treasury.Balance);
    }

    [Fact]
    public void SecondMissAddsStressThirdMissEntersDebtMode()
    {
        var team = Team(2, 1000);
        var payroll = new Payroll();
        var treasury = new TreasuryAccount(0);

        payroll.Resolve(team, treasury, Cfg);
        Assert.All(team, t => Assert.Equal(0, t.Needs.Stress));
        Assert.False(payroll.DebtMode);

        payroll.Resolve(team, treasury, Cfg);
        Assert.All(team, t => Assert.Equal(Cfg.StressOnSecondMiss, t.Needs.Stress));
        Assert.False(payroll.DebtMode);

        payroll.Resolve(team, treasury, Cfg);
        Assert.True(payroll.DebtMode);
        Assert.Equal(3, payroll.UnpaidStreak);
    }

    [Fact]
    public void AdvanceIsDeductedFromNextPayday()
    {
        var team = Team(1, 1000);
        team[0].WageAdvance = 400;
        var treasury = new TreasuryAccount(5000);
        var outcome = new Payroll().Resolve(team, treasury, Cfg);
        Assert.Equal(600, outcome.TotalDue);
        Assert.Equal(600, team[0].Gold);
        Assert.Equal(0, team[0].WageAdvance);
    }

    [Fact]
    public void BackPayIsSettledOnceTreasuryRecovers()
    {
        var team = Team(2, 1000);
        var payroll = new Payroll();
        var treasury = new TreasuryAccount(0);
        payroll.Resolve(team, treasury, Cfg);                  // thiếu hoàn toàn: mỗi người bị nợ 1000
        Assert.All(team, t => Assert.Equal(1000, t.WageOwed));

        treasury.Add(10_000);
        var outcome = payroll.Resolve(team, treasury, Cfg);    // trả cả lương mới lẫn nợ cũ
        Assert.Equal(4000, outcome.TotalDue);
        Assert.All(team, t => { Assert.Equal(2000, t.Gold); Assert.Equal(0, t.WageOwed); });
        Assert.Equal(0, payroll.UnpaidStreak);
        Assert.Equal(6000, treasury.Balance);
    }
}
```

- [ ] **Step 2: Chạy để thấy lỗi biên dịch**

Run: `dotnet test tests/Game.Domain.Tests --filter "FarmAndMarketTests|PayrollTests"`
Expected: FAIL (build error: `SimpleFarmResolver`, `FixedPriceMarket`, `Payroll` không tồn tại).

- [ ] **Step 3: Viết FarmAndMarket**

`src/Game.Domain/World/FarmAndMarket.cs`:

```csharp
using System;

namespace Game.Domain
{
    /// <summary>Kết quả một khúc farm.</summary>
    public sealed record FarmResult(int MaterialUnits, long Gold, long HpLost);

    /// <summary>Điểm cắm cho chiến đấu thật (sub-project 3): tính kết quả farm của một Trainer trong <paramref name="minutes"/> phút.</summary>
    public interface IFarmResolver
    {
        FarmResult Resolve(Trainer trainer, int minutes);
    }

    /// <summary>Bộ giải farm tạm: công thức đơn giản theo tính cách và Rarity, có nhiễu ±20%.</summary>
    public sealed class SimpleFarmResolver : IFarmResolver
    {
        readonly SimConfig cfg;
        readonly SimRandom rng;

        public SimpleFarmResolver(SimConfig config, SimRandom random)
        {
            cfg = config; rng = random;
        }

        public FarmResult Resolve(Trainer trainer, int minutes)
        {
            PersonalityProfile p = PersonalityProfile.Of(trainer.Personality);
            double rarityScale = Math.Pow(cfg.RarityGrowth, (int)trainer.Rarity);
            double chunks = minutes / (double)cfg.FarmChunkMinutes;
            double noise = 0.8 + 0.4 * rng.NextDouble();   // một lần bốc ngẫu nhiên cho cả khúc

            int units = (int)Math.Round(cfg.FarmMaterialsPerChunk * chunks * p.MaterialPickRate * p.LootMult * rarityScale * noise);
            long gold = (long)Math.Round(cfg.FarmGoldPerChunk * chunks * p.LootMult * rarityScale * noise);
            long hpLost = (long)Math.Round(cfg.FarmHpLostPerChunk * chunks * p.HpLossMult * rarityScale * noise);
            return new FarmResult(units, gold, hpLost);
        }
    }

    /// <summary>Kết quả bán nguyên liệu: Trainer nhận <c>GrossToTrainer - Tax</c>, Kho bạc nhận <c>Tax</c>.</summary>
    public sealed record SaleResult(long GrossToTrainer, long Tax);

    /// <summary>Điểm cắm cho Trạm Giao Thương + Thương nhân (sub-project 2). Chỉ tính toán, không đổi trạng thái.</summary>
    public interface IMaterialMarket
    {
        SaleResult Quote(Trainer seller, int units);
    }

    /// <summary>
    /// Chợ tạm: một người mua bên ngoài (đóng vai Thương nhân) trả giá cố định cho mỗi đơn vị
    /// (Gold từ ngoài vào), HUB chỉ thu thuế giao dịch. HUB không mua, không bán nguyên liệu.
    /// </summary>
    public sealed class FixedPriceMarket : IMaterialMarket
    {
        readonly SimConfig cfg;
        public FixedPriceMarket(SimConfig config) { cfg = config; }

        public SaleResult Quote(Trainer seller, int units)
        {
            long gross = units * cfg.MaterialPrice;
            long tax = (long)Math.Round(gross * cfg.TaxRate);
            return new SaleResult(gross, tax);
        }
    }
}
```

- [ ] **Step 4: Viết Payroll**

`src/Game.Domain/Hub/Payroll.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>Kết quả một kỳ Payday.</summary>
    public sealed record PaydayOutcome(long TotalDue, long TotalPaid, double PaidRatio, bool StrikeStarted, int UnpaidStreak);

    /// <summary>
    /// Lương hợp đồng và thang vỡ nợ.
    /// Đủ tiền: trả đủ. Thiếu tiền: chia theo tỉ lệ, TOÀN BỘ Trainer đình công, phần thiếu thành nợ lương.
    /// Vỡ nợ lần 2: mọi Trainer +Stress. Từ lần 3: chế độ cấn nợ (<see cref="DebtMode"/>).
    /// </summary>
    public sealed class Payroll
    {
        /// <summary>Số Payday liên tiếp không trả đủ lương.</summary>
        public int UnpaidStreak { get; private set; }

        /// <summary>Từ lần vỡ nợ thứ 3: farm giảm, Nhà Hàng miễn phí để cấn nợ lương.</summary>
        public bool DebtMode => UnpaidStreak >= 3;

        /// <summary>Số tiền HUB phải trả một Trainer ở kỳ này: lương hợp đồng + nợ cũ - phần đã ứng.</summary>
        public static long DueOf(Trainer t) => Math.Max(0, t.ContractWage + t.WageOwed - t.WageAdvance);

        public long TotalDue(IReadOnlyList<Trainer> trainers)
        {
            long total = 0;
            for (int i = 0; i < trainers.Count; i++) total += DueOf(trainers[i]);
            return total;
        }

        public PaydayOutcome Resolve(IReadOnlyList<Trainer> trainers, TreasuryAccount treasury, SimConfig cfg)
        {
            long total = TotalDue(trainers);
            if (total <= treasury.Balance)
            {
                foreach (Trainer t in trainers)
                {
                    t.Gold += DueOf(t);
                    t.WageOwed = 0;
                    t.WageAdvance = 0;
                }
                treasury.TrySpend(total);
                UnpaidStreak = 0;
                return new PaydayOutcome(total, total, 1.0, false, 0);
            }

            // Thiếu tiền: trả mỗi người floor(due x balance / total), rồi chia phần dư cho các Trainer đầu danh sách.
            long balance = treasury.Balance;
            int n = trainers.Count;
            var paid = new long[n];
            long sum = 0;
            for (int i = 0; i < n; i++)
            {
                paid[i] = DueOf(trainers[i]) * balance / total;
                sum += paid[i];
            }
            long remainder = balance - sum;
            bool progressed = true;
            while (remainder > 0 && progressed)
            {
                progressed = false;
                for (int i = 0; i < n && remainder > 0; i++)
                {
                    if (paid[i] < DueOf(trainers[i])) { paid[i]++; remainder--; progressed = true; }
                }
            }

            long totalPaid = 0;
            for (int i = 0; i < n; i++)
            {
                Trainer t = trainers[i];
                long due = DueOf(t);
                t.Gold += paid[i];
                t.WageOwed = due - paid[i];
                t.WageAdvance = 0;
                t.StrikeDaysLeft = cfg.StrikeDays;
                totalPaid += paid[i];
            }
            treasury.TrySpend(totalPaid);

            UnpaidStreak++;
            if (UnpaidStreak == 2)
            {
                foreach (Trainer t in trainers)
                {
                    t.Needs.Stress += cfg.StressOnSecondMiss;
                    t.Needs.Clamp();
                }
            }
            return new PaydayOutcome(total, totalPaid, (double)totalPaid / total, true, UnpaidStreak);
        }
    }
}
```

- [ ] **Step 5: Chạy test**

Run: `dotnet test tests/Game.Domain.Tests`
Expected: PASS toàn bộ.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add temporary farm resolver, fixed-price market and payroll" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Sự kiện Domain và kiểu dữ liệu công khai

**Files:**
- Create: `src/Game.Domain/Events/DomainEvents.cs`, `src/Game.Domain/Hub/HubWorldTypes.cs`
- Test: không có test riêng (record thuần dữ liệu); được phủ bởi test của Task 7 và 8. Bước kiểm tra là build.

**Interfaces:**
- Consumes: `TrainerState`, `Rarity`, `Personality`, `BuildingKind`, `PaydayOutcome`.
- Produces:
  - `interface IDomainEvent { int Minute { get; } }` và các record: `TrainerStateChanged(int Minute, int TrainerId, TrainerState From, TrainerState To, string Reason)`, `ServiceUsed(int Minute, int TrainerId, BuildingKind Building, long Paid, long ListPrice, long FairPrice, double StressAdded)`, `QueueChanged(int Minute, BuildingKind Building, int QueueLength, int Occupied)`, `TreasuryChanged(int Minute, long Delta, long Balance, string Reason)`, `PaydayDue(int Minute, PaydayForecast Forecast)`, `PaydayResolved(int Minute, PaydayOutcome Outcome)`, `BuildingMaintenanceChanged(int Minute, BuildingKind Building, bool Maintained)`, `DonationReceived(int Minute, int TrainerId, long Gold, string Source)` (Source là `"Patron"` hoặc `"Director"`), `DayPhaseChanged(int Minute, bool IsNight)`.
  - `enum StopReason { Completed, PaydayDue }`; `record RunResult(int MinutesRun, int RemainingMinutes, StopReason Stop)`; `record CommandResult(bool Ok, string Reason)` với `Success()` và `Rejected(string)`; `record PaydayForecast(int DaysLeft, long WagesDue, long TreasuryBalance)`; `record TrainerView(int Id, Rarity Rarity, Personality Personality, TrainerState State, string StateReason, long Gold, double Stamina, double Satiety, double Hydration, double Stress, long TeamHp, long TeamHpMax, int BackpackUnits, long ContractWage, long WageOwed, int StrikeDaysLeft)`; `record BuildingView(BuildingKind Kind, int Level, int Slots, int Occupied, int QueueLength, int MaxQueueLength, long Price, long FairPrice, bool Maintained)`.

- [ ] **Step 1: Viết hai file**

`src/Game.Domain/Events/DomainEvents.cs`:

```csharp
namespace Game.Domain
{
    /// <summary>Sự kiện Domain (C# thuần). Presenter dùng R3 ở lớp Presentation để chuyển thành luồng UI.</summary>
    public interface IDomainEvent
    {
        /// <summary>Phút in-game lúc sự kiện xảy ra.</summary>
        int Minute { get; }
    }

    /// <summary>Trainer đổi trạng thái; <c>Reason</c> là lý do để UI giải thích (ví dụ "Hungry").</summary>
    public sealed record TrainerStateChanged(int Minute, int TrainerId, TrainerState From, TrainerState To, string Reason) : IDomainEvent;

    /// <summary>Một lượt dùng dịch vụ. <c>StressAdded</c> &gt; 0 khi giá Giám đốc đặt cao hơn giá hợp lý.</summary>
    public sealed record ServiceUsed(int Minute, int TrainerId, BuildingKind Building, long Paid, long ListPrice, long FairPrice, double StressAdded) : IDomainEvent;

    public sealed record QueueChanged(int Minute, BuildingKind Building, int QueueLength, int Occupied) : IDomainEvent;

    public sealed record TreasuryChanged(int Minute, long Delta, long Balance, string Reason) : IDomainEvent;

    public sealed record PaydayDue(int Minute, PaydayForecast Forecast) : IDomainEvent;

    public sealed record PaydayResolved(int Minute, PaydayOutcome Outcome) : IDomainEvent;

    public sealed record BuildingMaintenanceChanged(int Minute, BuildingKind Building, bool Maintained) : IDomainEvent;

    /// <summary>Trainer nhận tiền không hoàn lại. <c>Source</c> là "Patron" (Tổng tài) hoặc "Director".</summary>
    public sealed record DonationReceived(int Minute, int TrainerId, long Gold, string Source) : IDomainEvent;

    /// <summary>Sang ngày (IsNight = false) hoặc sang đêm (IsNight = true).</summary>
    public sealed record DayPhaseChanged(int Minute, bool IsNight) : IDomainEvent;
}
```

`src/Game.Domain/Hub/HubWorldTypes.cs`:

```csharp
namespace Game.Domain
{
    public enum StopReason { Completed, PaydayDue }

    /// <summary>Kết quả một lần chạy mô phỏng: số phút đã chạy, số phút chưa chạy (khi dừng sớm) và lý do dừng.</summary>
    public sealed record RunResult(int MinutesRun, int RemainingMinutes, StopReason Stop);

    /// <summary>Kết quả lệnh của Giám đốc. Lỗi của người chơi trả về Rejected, không ném exception.</summary>
    public sealed record CommandResult(bool Ok, string Reason)
    {
        public static CommandResult Success() => new CommandResult(true, "");
        public static CommandResult Rejected(string reason) => new CommandResult(false, reason);
    }

    /// <summary>Dự báo Payday: "Payday sau X ngày, cần Y Gold, hiện có Z Gold".</summary>
    public sealed record PaydayForecast(int DaysLeft, long WagesDue, long TreasuryBalance);

    /// <summary>Ảnh chụp chỉ đọc của một Trainer cho UI.</summary>
    public sealed record TrainerView(
        int Id, Rarity Rarity, Personality Personality, TrainerState State, string StateReason, long Gold,
        double Stamina, double Satiety, double Hydration, double Stress,
        long TeamHp, long TeamHpMax, int BackpackUnits, long ContractWage, long WageOwed, int StrikeDaysLeft);

    /// <summary>Ảnh chụp chỉ đọc của một công trình dịch vụ cho UI.</summary>
    public sealed record BuildingView(
        BuildingKind Kind, int Level, int Slots, int Occupied, int QueueLength, int MaxQueueLength,
        long Price, long FairPrice, bool Maintained);
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/Game.Domain`
Expected: `Build succeeded`, 0 lỗi.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: add domain events and public result types" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 7: HubWorld: vòng chạy, FSM Trainer, dịch vụ và Payday

**Files:**
- Create: `src/Game.Domain/Hub/HubWorld.cs`, `HubWorld.Trainers.cs`, `HubWorld.Services.cs`, `HubWorld.Payday.cs`, `HubWorld.Views.cs`
- Test: `tests/Game.Domain.Tests/HubWorldTests.cs`

**Interfaces:**
- Consumes: mọi thứ từ Task 2-6.
- Produces (công khai): `HubWorld(SimConfig config, int seed)`, `HubWorld(SimConfig config, int seed, IFarmResolver farm, IMaterialMarket market)` (truyền `null` để dùng bản tạm); `SimTime Now`; `long Treasury`; `int MaxMoneyWaitMinutes`; `RunResult RunFor(int minutes)`; `RunResult RunUntilPayday()`; `PaydayOutcome ResolvePayday()` (ném `InvalidOperationException` nếu Payday chưa đến); `PaydayForecast Forecast`; `IReadOnlyList<TrainerView> Trainers`; `IReadOnlyList<BuildingView> Buildings`; `event Action<IDomainEvent> EventRaised`.
- Nội bộ dùng cho Task 8 (cùng lớp `partial`): các trường `cfg, rng, queue, treasury, payroll, trainers, buildings, now, paydayIndex, paydayPending`; các hàm `Settle(Trainer)`, `SetState(Trainer, TrainerState, string)`, `OnDecide(Trainer)`, `LeaveWait(Trainer)`, `Raise(IDomainEvent)`, `AddTreasury(long, string)`.

- [ ] **Step 1: Viết test thất bại**

`tests/Game.Domain.Tests/HubWorldTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Game.Domain;

public class HubWorldTests
{
    static List<IDomainEvent> Capture(HubWorld w)
    {
        var list = new List<IDomainEvent>();
        w.EventRaised += e => list.Add(e);
        return list;
    }

    // ---- Thời gian và Payday ----
    [Fact]
    public void RunUntilPaydayStopsAtLastMinuteOfDayThirty()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        var result = w.RunUntilPayday();
        Assert.Equal(StopReason.PaydayDue, result.Stop);
        Assert.Equal(SimClock.PaydayMinute(0), w.Now.TotalMinutes);
        Assert.Equal(0, w.Forecast.DaysLeft);
    }

    [Fact]
    public void RunForReportsRemainingMinutesWhenPaydayInterrupts()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        var result = w.RunFor(100_000);
        Assert.Equal(StopReason.PaydayDue, result.Stop);
        Assert.Equal(43199 - SimConfig.Default.StartMinute, result.MinutesRun);
        Assert.Equal(100_000 - result.MinutesRun, result.RemainingMinutes);
    }

    [Fact]
    public void RunForDoesNothingUntilPaydayIsResolved()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        w.RunUntilPayday();
        var blocked = w.RunFor(10);
        Assert.Equal(0, blocked.MinutesRun);
        Assert.Equal(10, blocked.RemainingMinutes);
        Assert.Equal(StopReason.PaydayDue, blocked.Stop);

        w.ResolvePayday();
        var resumed = w.RunFor(60);
        Assert.Equal(60, resumed.MinutesRun);
        Assert.Equal(StopReason.Completed, resumed.Stop);
    }

    [Fact]
    public void ResolvePaydayBeforeItIsDueThrows()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        Assert.Throws<InvalidOperationException>(() => w.ResolvePayday());
    }

    [Fact]
    public void NextPaydayComesThirtyDaysLater()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        w.RunUntilPayday(); w.ResolvePayday();
        w.RunUntilPayday();
        Assert.Equal(SimClock.PaydayMinute(1), w.Now.TotalMinutes);
    }

    [Fact]
    public void ForecastShowsDaysLeftAndWageBill()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        var f = w.Forecast;
        Assert.Equal(30, f.DaysLeft);                  // bắt đầu 06:00 ngày 1: còn tới hết ngày 30
        Assert.Equal(w.Trainers.Sum(t => t.ContractWage), f.WagesDue);
        Assert.Equal(w.Treasury, f.TreasuryBalance);
    }

    // ---- Tất định ----
    [Fact]
    public void SameSeedGivesIdenticalWorld()
    {
        (long, long[]) Run(int seed)
        {
            var w = new HubWorld(SimConfig.Default, seed);
            for (int m = 0; m < 3; m++) { w.RunUntilPayday(); w.ResolvePayday(); }
            return (w.Treasury, w.Trainers.Select(t => t.Gold).ToArray());
        }
        var a = Run(11); var b = Run(11);
        Assert.Equal(a.Item1, b.Item1);
        Assert.Equal(a.Item2, b.Item2);
    }

    // ---- Ngày và đêm ----
    [Fact]
    public void NobodyIsOutsideAtEightPmWhenNoOneHasNightVision()
    {
        var w = new HubWorld(new SimConfig { TrainerCount = 10 }, 3);
        w.RunFor(1200 - 360);   // tới 20:00 ngày 1
        Assert.All(w.Trainers, t => Assert.DoesNotContain(t.State,
            new[] { TrainerState.Farming, TrainerState.Traveling, TrainerState.Returning }));
    }

    [Fact]
    public void DayPhaseEventsFireAtDawnAndDusk()
    {
        var w = new HubWorld(SimConfig.Default, 3);
        var events = Capture(w);
        w.RunFor(2 * 1440);
        var phases = events.OfType<DayPhaseChanged>().ToList();
        Assert.Contains(phases, p => p.IsNight && p.Minute == 1080);
        Assert.Contains(phases, p => !p.IsNight && p.Minute == 1440 + 360);
    }

    // ---- Dịch vụ, hàng đợi, giá ----
    [Fact]
    public void SmallBuildingsFormQueuesWhenManyTrainersSleepAtDusk()
    {
        var w = new HubWorld(new SimConfig { TrainerCount = 30, StartBuildingLevel = 1 }, 5);
        w.RunFor(3 * 1440);
        Assert.True(w.Buildings[(int)BuildingKind.Inn].MaxQueueLength > 0);
    }

    [Fact]
    public void FairPricesAddNoStress()
    {
        var w = new HubWorld(SimConfig.Default, 7);
        var events = Capture(w);
        w.RunFor(3 * 1440);
        var uses = events.OfType<ServiceUsed>().ToList();
        Assert.NotEmpty(uses);
        Assert.All(uses, u => Assert.Equal(0, u.StressAdded));   // giá khởi điểm = giá hợp lý; giá cao được test ở Task 8
    }

    // ---- Payday thiếu tiền ----
    [Fact]
    public void UnpayableWagesMakeEveryoneStrikeAndStayOffTheField()
    {
        var w = new HubWorld(new SimConfig { BaseWage = 1_000_000 }, 9);
        w.RunUntilPayday();
        var outcome = w.ResolvePayday();
        Assert.True(outcome.StrikeStarted);
        Assert.True(outcome.PaidRatio < 1.0);
        Assert.All(w.Trainers, t => Assert.Equal(5, t.StrikeDaysLeft));

        w.RunFor(120);
        Assert.All(w.Trainers, t => Assert.DoesNotContain(t.State,
            new[] { TrainerState.Farming, TrainerState.Traveling, TrainerState.Returning }));
        Assert.All(w.Trainers, t => Assert.True(t.StrikeDaysLeft > 0));
    }

    // ---- Kho bạc ----
    [Fact]
    public void TreasuryNeverGoesNegative()
    {
        var w = new HubWorld(new SimConfig { StartTreasury = 0 }, 4);
        for (int m = 0; m < 3; m++)
        {
            w.RunUntilPayday();
            Assert.True(w.Treasury >= 0);
            w.ResolvePayday();
            Assert.True(w.Treasury >= 0);
        }
    }
}
```

- [ ] **Step 2: Chạy để thấy lỗi biên dịch**

Run: `dotnet test tests/Game.Domain.Tests --filter HubWorldTests`
Expected: FAIL (build error: `HubWorld` không tồn tại).

- [ ] **Step 3: Viết `HubWorld.cs` (khởi tạo, vòng chạy, Dawn/Dusk/DayStart)**

`src/Game.Domain/Hub/HubWorld.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>
    /// Điểm vào duy nhất của Domain: Unity, Game.Sim và test đều gọi lớp này.
    /// Mô phỏng sự kiện rời rạc theo phút in-game; chia thành nhiều file partial theo trách nhiệm.
    /// </summary>
    public sealed partial class HubWorld
    {
        readonly SimConfig cfg;
        readonly SimRandom rng;
        readonly EventQueue queue = new EventQueue();
        readonly TreasuryAccount treasury;
        readonly Payroll payroll = new Payroll();
        readonly List<Trainer> trainers = new List<Trainer>();
        readonly ServiceBuilding[] buildings = new ServiceBuilding[4];
        readonly IFarmResolver farm;
        readonly IMaterialMarket market;

        int now;                 // phút in-game hiện tại
        int paydayIndex;         // số Payday đã xử lý
        bool paydayPending;      // Payday đã tới, đang chờ người chơi xử lý

        /// <summary>Sự kiện Domain (C# thuần). Presenter dùng R3 ở lớp Presentation.</summary>
        public event Action<IDomainEvent> EventRaised;

        public HubWorld(SimConfig config, int seed) : this(config, seed, null, null) { }

        /// <param name="farmResolver">Bộ giải farm; null thì dùng bản tạm <see cref="SimpleFarmResolver"/>.</param>
        /// <param name="materialMarket">Chợ nguyên liệu; null thì dùng bản tạm <see cref="FixedPriceMarket"/>.</param>
        public HubWorld(SimConfig config, int seed, IFarmResolver farmResolver, IMaterialMarket materialMarket)
        {
            cfg = config ?? throw new ArgumentNullException(nameof(config));
            rng = new SimRandom(seed);
            farm = farmResolver ?? new SimpleFarmResolver(cfg, rng);
            market = materialMarket ?? new FixedPriceMarket(cfg);
            treasury = new TreasuryAccount(cfg.StartTreasury);
            now = cfg.StartMinute;

            foreach (BuildingSpec spec in cfg.Buildings)
                buildings[(int)spec.Kind] = new ServiceBuilding(spec, cfg.StartBuildingLevel, cfg.UpkeepPerBuildingPerDay);
            for (int i = 0; i < buildings.Length; i++)
                if (buildings[i] == null) throw new ArgumentException($"SimConfig.Buildings thiếu công trình {(BuildingKind)i}.");

            for (int i = 0; i < cfg.TrainerCount; i++)
            {
                Personality personality = cfg.ForcedPersonality ?? (Personality)rng.NextInt(4);
                trainers.Add(new Trainer
                {
                    Id = i, Rarity = Rarity.Common, Personality = personality,
                    Gold = cfg.StartTrainerGold,
                    TeamHp = cfg.TeamHpMax, TeamHpMax = cfg.TeamHpMax,
                    BackpackCapacity = cfg.BackpackCapacity,
                    ContractWage = cfg.ContractWageFor(Rarity.Common, personality),
                    LastSettleMinute = now,
                });
            }

            queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DawnMinute), SimEventKind.Dawn);
            queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DuskMinute), SimEventKind.Dusk);
            queue.Schedule(SimClock.NextMinuteOfDay(now, 0), SimEventKind.DayStart);
            queue.Schedule(SimClock.PaydayMinute(0), SimEventKind.PaydayDue);
            foreach (Trainer t in trainers) queue.Schedule(now, SimEventKind.TrainerDecide, t.Id, t.Token);
        }

        /// <summary>Phút in-game hiện tại.</summary>
        public SimTime Now => new SimTime(now);

        /// <summary>Số dư Kho bạc.</summary>
        public long Treasury => treasury.Balance;

        /// <summary>Thời gian chờ tiền dài nhất (phút) của một Trainer từ đầu đến giờ. Dùng để kiểm tra "không ai kẹt mãi".</summary>
        public int MaxMoneyWaitMinutes { get; private set; }

        /// <summary>
        /// Chạy tối đa <paramref name="minutes"/> phút in-game. Dừng sớm nếu tới Payday; khi đó
        /// <see cref="RunResult.RemainingMinutes"/> là phần chưa chạy (Đồng Hồ Cát giữ lại phần này).
        /// Khi Payday đang chờ xử lý, hàm chạy 0 phút.
        /// </summary>
        public RunResult RunFor(int minutes)
        {
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            if (paydayPending) return new RunResult(0, minutes, StopReason.PaydayDue);

            int startNow = now;
            long end = (long)now + minutes;
            while (queue.Count > 0 && queue.PeekTime <= end)
            {
                SimEvent e = queue.Dequeue();
                now = e.Time;
                Dispatch(e);
                if (paydayPending)
                {
                    int ran = now - startNow;
                    return new RunResult(ran, minutes - ran, StopReason.PaydayDue);
                }
            }
            now = (int)end;
            return new RunResult(minutes, 0, StopReason.Completed);
        }

        /// <summary>Chạy tới Payday gần nhất rồi dừng. Dùng cho tiến trình offline.</summary>
        public RunResult RunUntilPayday()
        {
            if (paydayPending) return new RunResult(0, 0, StopReason.PaydayDue);
            return RunFor(SimClock.PaydayMinute(paydayIndex) - now);
        }

        void Dispatch(SimEvent e)
        {
            Trainer t = null;
            if (e.TrainerId >= 0)
            {
                t = trainers[e.TrainerId];
                if (t.Token != e.Token) return;   // sự kiện cũ, Trainer đã bị ngắt giữa chừng
            }
            switch (e.Kind)
            {
                case SimEventKind.TrainerDecide: OnDecide(t); break;
                case SimEventKind.TrainerArriveZone: OnArriveZone(t); break;
                case SimEventKind.FarmChunk: OnFarmChunk(t); break;
                case SimEventKind.TrainerArriveHub: OnArriveHub(t); break;
                case SimEventKind.ServiceDone: OnServiceDone(t, buildings[e.Arg]); break;
                case SimEventKind.WaitTick: OnWaitTick(t); break;
                case SimEventKind.Dawn: OnDawn(); break;
                case SimEventKind.Dusk: OnDusk(); break;
                case SimEventKind.DayStart: OnDayStart(); break;
                case SimEventKind.PaydayDue: OnPaydayDue(); break;
            }
        }

        void Raise(IDomainEvent e) => EventRaised?.Invoke(e);

        /// <summary>Cộng tiền vào Kho bạc và phát sự kiện (bỏ qua khi số tiền bằng 0).</summary>
        void AddTreasury(long amount, string reason)
        {
            if (amount == 0) return;
            treasury.Add(amount);
            Raise(new TreasuryChanged(now, amount, treasury.Balance, reason));
        }

        void OnDawn()
        {
            Raise(new DayPhaseChanged(now, false));
            queue.Schedule(now + SimClock.MinutesPerDay, SimEventKind.Dawn);
        }

        /// <summary>18:00: Trainer đang ở ngoài mà không có kính nhìn đêm phải về HUB.</summary>
        void OnDusk()
        {
            Raise(new DayPhaseChanged(now, true));
            foreach (Trainer t in trainers)
            {
                bool outside = t.State == TrainerState.Traveling || t.State == TrainerState.Farming;
                if (outside && !t.HasNightVision) SendHome(t, ReturnReason.Night);
            }
            queue.Schedule(now + SimClock.MinutesPerDay, SimEventKind.Dusk);
        }

        /// <summary>00:00: trừ chi phí vận hành từng công trình, giảm số ngày đình công.</summary>
        void OnDayStart()
        {
            foreach (Trainer t in trainers)
                if (t.StrikeDaysLeft > 0) t.StrikeDaysLeft--;

            foreach (ServiceBuilding b in buildings)
            {
                bool wasMaintained = b.Maintained;
                bool paid = treasury.TrySpend(b.UpkeepPerDay);
                b.Maintained = paid;
                if (paid) Raise(new TreasuryChanged(now, -b.UpkeepPerDay, treasury.Balance, "Upkeep"));
                if (paid != wasMaintained) Raise(new BuildingMaintenanceChanged(now, b.Kind, paid));
                if (paid && !wasMaintained) SeatWaiting(b);   // số chỗ tăng lại: gọi thêm người đang chờ
            }
            queue.Schedule(now + SimClock.MinutesPerDay, SimEventKind.DayStart);
        }
    }
}
```

- [ ] **Step 4: Viết `HubWorld.Trainers.cs` (FSM)**

`src/Game.Domain/Hub/HubWorld.Trainers.cs`:

```csharp
using System;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>
        /// Cộng trừ nhu cầu cho khoảng thời gian trôi kể từ lần cập nhật trước, theo trạng thái ĐANG có.
        /// Luôn gọi trước khi đổi trạng thái của Trainer.
        /// </summary>
        void Settle(Trainer t)
        {
            int minutes = now - t.LastSettleMinute;
            t.LastSettleMinute = now;
            if (minutes <= 0) return;

            double hours = minutes / 60.0;
            PersonalityProfile p = PersonalityProfile.Of(t.Personality);
            Needs n = t.Needs;
            bool outside = t.State == TrainerState.Traveling || t.State == TrainerState.Farming || t.State == TrainerState.Returning;
            if (outside)
            {
                n.Stamina -= cfg.FieldStaminaPerHour * hours;
                n.Satiety -= cfg.FieldSatietyPerHour * p.SatietyDecayMult * hours;
                n.Hydration -= cfg.FieldHydrationPerHour * hours;
                n.Stress += cfg.FieldStressPerHour * hours;
            }
            else if (t.State != TrainerState.InService)   // đang được phục vụ thì không tụt
            {
                n.Stamina -= cfg.HubStaminaPerHour * hours;
                n.Satiety -= cfg.HubSatietyPerHour * p.SatietyDecayMult * hours;
                n.Hydration -= cfg.HubHydrationPerHour * hours;
                if (t.State == TrainerState.Queued) n.Stress += cfg.QueueStressPerHour * hours;
                if (t.State == TrainerState.WaitingForMoney) n.Stress += cfg.WaitStressPerHour * hours;
            }
            n.Clamp();
        }

        void SetState(Trainer t, TrainerState state, string reason)
        {
            if (t.State == state && t.StateReason == reason) return;
            TrainerState from = t.State;
            t.State = state;
            t.StateReason = reason;
            Raise(new TrainerStateChanged(now, t.Id, from, state, reason));
        }

        /// <summary>
        /// Trainer chọn việc tiếp theo ở HUB: dịch vụ cần dùng > nghỉ (đình công, ban đêm không kính) > đi farm.
        /// </summary>
        void OnDecide(Trainer t)
        {
            Settle(t);
            bool isNight = SimClock.IsNight(now);

            BuildingKind? need = TrainerBrain.PickService(t, cfg, isNight);
            if (need.HasValue) { RequestService(t, need.Value); return; }

            if (t.IsOnStrike)
            {
                SetState(t, TrainerState.AtHub, "Strike");
                queue.Schedule(now + 60, SimEventKind.TrainerDecide, t.Id, t.Token);   // đình công: nghỉ 1 giờ rồi xét lại
                return;
            }
            if (isNight && !t.HasNightVision)
            {
                SetState(t, TrainerState.AtHub, "NightRest");
                queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DawnMinute), SimEventKind.TrainerDecide, t.Id, t.Token);
                return;
            }
            SetState(t, TrainerState.Traveling, "Farm");
            queue.Schedule(now + cfg.ZoneTravelMinutes, SimEventKind.TrainerArriveZone, t.Id, t.Token);
        }

        void OnArriveZone(Trainer t)
        {
            Settle(t);
            SetState(t, TrainerState.Farming, "Farm");
            queue.Schedule(now + cfg.FarmChunkMinutes, SimEventKind.FarmChunk, t.Id, t.Token);
        }

        /// <summary>Hết một khúc farm: nhận kết quả, rồi quyết định ở lại hay về HUB.</summary>
        void OnFarmChunk(Trainer t)
        {
            Settle(t);
            FarmResult r = farm.Resolve(t, cfg.FarmChunkMinutes);
            if (payroll.DebtMode)   // chế độ cấn nợ: farm ít hơn
                r = new FarmResult((int)(r.MaterialUnits * cfg.DebtModeFarmMultiplier), (long)(r.Gold * cfg.DebtModeFarmMultiplier), r.HpLost);

            t.BackpackUnits = Math.Min(t.BackpackCapacity, t.BackpackUnits + r.MaterialUnits);
            t.Gold += r.Gold;                                  // Gold quái rơi là nguồn tiền từ ngoài vào
            t.TeamHp = Math.Max(0, t.TeamHp - r.HpLost);

            ReturnReason reason = TrainerBrain.ShouldReturn(t, SimClock.IsNight(now));
            if (reason != ReturnReason.None) StartReturn(t, reason);
            else queue.Schedule(now + cfg.FarmChunkMinutes, SimEventKind.FarmChunk, t.Id, t.Token);
        }

        /// <summary>Bắt đầu đường về HUB. Gọi sau <see cref="Settle"/>.</summary>
        void StartReturn(Trainer t, ReturnReason reason)
        {
            SetState(t, TrainerState.Returning, reason.ToString());
            queue.Schedule(now + cfg.ZoneTravelMinutes, SimEventKind.TrainerArriveHub, t.Id, t.Token);
        }

        /// <summary>Ngắt việc đang làm của Trainer (hủy sự kiện cũ bằng token) và cho về HUB.</summary>
        void SendHome(Trainer t, ReturnReason reason)
        {
            t.Token++;
            Settle(t);
            StartReturn(t, reason);
        }

        /// <summary>Về tới HUB: bán nguyên liệu (HUB thu thuế), rồi chọn việc tiếp theo.</summary>
        void OnArriveHub(Trainer t)
        {
            Settle(t);
            SetState(t, TrainerState.AtHub, "Arrived");
            if (t.BackpackUnits > 0)
            {
                SaleResult sale = market.Quote(t, t.BackpackUnits);
                t.Gold += sale.GrossToTrainer - sale.Tax;
                AddTreasury(sale.Tax, "TradeTax");
                t.BackpackUnits = 0;
            }
            OnDecide(t);
        }
    }
}
```

- [ ] **Step 5: Viết `HubWorld.Services.cs` (hàng đợi, thanh toán, hết tiền)**

`src/Game.Domain/Hub/HubWorld.Services.cs`:

```csharp
using System;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>Trainer xin một dịch vụ: hết tiền thì chờ tiền, ngược lại vào hàng đợi FIFO.</summary>
        void RequestService(Trainer t, BuildingKind kind)
        {
            ServiceBuilding b = buildings[(int)kind];
            PersonalityProfile profile = PersonalityProfile.Of(t.Personality);
            long price = b.PriceFor(profile, t.TeamHpMax - t.TeamHp);
            bool freeInDebtMode = payroll.DebtMode && kind == BuildingKind.Restaurant && t.WageOwed > 0;
            if (!freeInDebtMode && t.Gold < price) { BeginWaitForMoney(t, kind); return; }

            SetState(t, TrainerState.Queued, kind.ToString());
            b.Enqueue(t.Id);
            SeatWaiting(b);
        }

        /// <summary>Xếp người đang chờ vào các chỗ trống và bắt đầu phục vụ họ.</summary>
        void SeatWaiting(ServiceBuilding b)
        {
            System.Collections.Generic.List<int> seated;
            while ((seated = b.PromoteWaiting()).Count > 0)
                foreach (int id in seated) StartService(trainers[id], b);
            Raise(new QueueChanged(now, b.Kind, b.QueueLength, b.Occupied));
        }

        /// <summary>
        /// Bắt đầu một lượt phục vụ: tính giá tại thời điểm này, thu tiền (hoặc cấn nợ ở chế độ vỡ nợ),
        /// cộng Stress nếu giá cao hơn giá hợp lý, rồi hẹn giờ kết thúc.
        /// </summary>
        void StartService(Trainer t, ServiceBuilding b)
        {
            Settle(t);   // cộng nốt Stress xếp hàng
            PersonalityProfile profile = PersonalityProfile.Of(t.Personality);
            long normalPrice = b.PriceFor(profile, t.TeamHpMax - t.TeamHp);
            bool free = payroll.DebtMode && b.Kind == BuildingKind.Restaurant && t.WageOwed > 0;
            long paid = free ? 0 : normalPrice;

            if (t.Gold < paid)   // giá bị Giám đốc đẩy lên trong lúc chờ
            {
                b.Leave(t.Id);
                BeginWaitForMoney(t, b.Kind);
                return;
            }

            if (free)
            {
                t.WageOwed = Math.Max(0, t.WageOwed - normalPrice);   // giá trị dịch vụ trừ vào nợ lương
            }
            else
            {
                t.Gold -= paid;
                long cogs = (long)Math.Round(paid * cfg.ServiceCogs);
                AddTreasury(paid - cogs, "Service");
            }

            double priceRatio = (double)b.Price / b.FairPrice;
            double stressAdded = cfg.PriceStressFactor * Math.Max(0.0, priceRatio - 1.0) * profile.PriceSensitivity;
            t.Needs.Stress += stressAdded;
            t.Needs.Clamp();

            SetState(t, TrainerState.InService, b.Kind.ToString());
            queue.Schedule(now + b.ServiceMinutesFor(now), SimEventKind.ServiceDone, t.Id, t.Token, (int)b.Kind);
            Raise(new ServiceUsed(now, t.Id, b.Kind, paid, b.Price, b.FairPrice, stressAdded));
        }

        /// <summary>Dùng xong dịch vụ: hồi nhu cầu tương ứng, nhả chỗ, gọi người kế tiếp.</summary>
        void OnServiceDone(Trainer t, ServiceBuilding b)
        {
            Settle(t);
            switch (b.Kind)
            {
                case BuildingKind.Inn: t.Needs.Stamina = 100; break;
                case BuildingKind.Restaurant: t.Needs.Satiety = 100; t.Needs.Hydration = 100; break;
                case BuildingKind.Bar: t.Needs.Stress = cfg.BarStressTarget; break;
                case BuildingKind.Hospital: t.TeamHp = t.TeamHpMax; break;
            }
            b.Leave(t.Id);
            SeatWaiting(b);
            SetState(t, TrainerState.AtHub, "Served");
            queue.Schedule(now, SimEventKind.TrainerDecide, t.Id, t.Token);
        }

        // ---- Hết tiền ----
        void BeginWaitForMoney(Trainer t, BuildingKind kind)
        {
            SetState(t, TrainerState.WaitingForMoney, kind.ToString());
            t.WaitingSinceMinute = now;
            t.PendingService = kind;
            queue.Schedule(now + 60, SimEventKind.WaitTick, t.Id, t.Token);
        }

        /// <summary>Kết thúc trạng thái chờ tiền và ghi nhận thời gian chờ.</summary>
        void LeaveWait(Trainer t)
        {
            int waited = now - t.WaitingSinceMinute;
            if (waited > MaxMoneyWaitMinutes) MaxMoneyWaitMinutes = waited;
        }

        /// <summary>
        /// Mỗi giờ chờ tiền: nếu đã đủ tiền (giá giảm) thì xử lý lại ngay; nếu không thì thử gọi Tổng tài
        /// (xác suất mỗi giờ, chắc chắn sau PatronGuaranteedAfterHours giờ). Tổng tài donate gấp đôi giá cần.
        /// </summary>
        void OnWaitTick(Trainer t)
        {
            Settle(t);
            ServiceBuilding b = buildings[(int)t.PendingService];
            long needed = b.PriceFor(PersonalityProfile.Of(t.Personality), t.TeamHpMax - t.TeamHp);

            if (t.Gold >= needed)
            {
                LeaveWait(t);
                SetState(t, TrainerState.AtHub, "Funded");
                OnDecide(t);
                return;
            }

            int hoursWaited = (now - t.WaitingSinceMinute) / 60;
            bool patronAppears = hoursWaited >= cfg.PatronGuaranteedAfterHours || rng.NextDouble() < cfg.PatronChancePerHour;
            if (patronAppears)
            {
                long donation = Math.Max(needed * 2, 1);
                t.Gold += donation;   // tiền từ ngoài vào
                Raise(new DonationReceived(now, t.Id, donation, "Patron"));
                LeaveWait(t);
                SetState(t, TrainerState.AtHub, "Funded");
                OnDecide(t);
                return;
            }
            queue.Schedule(now + 60, SimEventKind.WaitTick, t.Id, t.Token);
        }
    }
}
```

- [ ] **Step 6: Viết `HubWorld.Payday.cs` và `HubWorld.Views.cs`**

`src/Game.Domain/Hub/HubWorld.Payday.cs`:

```csharp
using System;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>Dự báo Payday: số ngày còn lại, quỹ lương cần trả, Kho bạc hiện có.</summary>
        public PaydayForecast Forecast
        {
            get
            {
                int daysLeft = paydayPending
                    ? 0
                    : (SimClock.PaydayMinute(paydayIndex) - now + SimClock.MinutesPerDay) / SimClock.MinutesPerDay;
                return new PaydayForecast(daysLeft, payroll.TotalDue(trainers), treasury.Balance);
            }
        }

        void OnPaydayDue()
        {
            paydayPending = true;
            Raise(new PaydayDue(now, Forecast));
        }

        /// <summary>
        /// Người chơi xử lý Payday: trả lương hợp đồng, nếu thiếu thì chia theo tỉ lệ và toàn bộ Trainer đình công.
        /// Chỉ gọi được khi <see cref="RunFor"/> đã dừng vì PaydayDue; nếu không, ném InvalidOperationException (lỗi lập trình).
        /// </summary>
        public PaydayOutcome ResolvePayday()
        {
            if (!paydayPending) throw new InvalidOperationException("Payday chưa tới, chưa thể xử lý.");

            PaydayOutcome outcome = payroll.Resolve(trainers, treasury, cfg);
            paydayPending = false;
            paydayIndex++;
            queue.Schedule(SimClock.PaydayMinute(paydayIndex), SimEventKind.PaydayDue);
            if (outcome.TotalPaid > 0) Raise(new TreasuryChanged(now, -outcome.TotalPaid, treasury.Balance, "Payday"));
            Raise(new PaydayResolved(now, outcome));

            if (outcome.StrikeStarted)   // đình công: Trainer đang ở ngoài phải về HUB
            {
                foreach (Trainer t in trainers)
                {
                    bool outside = t.State == TrainerState.Traveling || t.State == TrainerState.Farming;
                    if (outside) SendHome(t, ReturnReason.Strike);
                }
            }
            return outcome;
        }
    }
}
```

`src/Game.Domain/Hub/HubWorld.Views.cs`:

```csharp
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>Ảnh chụp chỉ đọc của mọi Trainer (tạo mới mỗi lần gọi).</summary>
        public IReadOnlyList<TrainerView> Trainers
        {
            get
            {
                var list = new List<TrainerView>(trainers.Count);
                foreach (Trainer t in trainers)
                    list.Add(new TrainerView(
                        t.Id, t.Rarity, t.Personality, t.State, t.StateReason, t.Gold,
                        t.Needs.Stamina, t.Needs.Satiety, t.Needs.Hydration, t.Needs.Stress,
                        t.TeamHp, t.TeamHpMax, t.BackpackUnits, t.ContractWage, t.WageOwed, t.StrikeDaysLeft));
                return list;
            }
        }

        /// <summary>Ảnh chụp chỉ đọc của các công trình dịch vụ, theo thứ tự <see cref="BuildingKind"/>.</summary>
        public IReadOnlyList<BuildingView> Buildings
        {
            get
            {
                var list = new List<BuildingView>(buildings.Length);
                foreach (ServiceBuilding b in buildings)
                    list.Add(new BuildingView(b.Kind, b.Level, b.Slots, b.Occupied, b.QueueLength, b.MaxQueueLength, b.Price, b.FairPrice, b.Maintained));
                return list;
            }
        }
    }
}
```

- [ ] **Step 7: Chạy test**

Run: `dotnet test tests/Game.Domain.Tests`
Expected: PASS toàn bộ. Nếu một test sai, đọc thông báo: các lỗi thường gặp là thiếu `using System.Linq` trong test, hoặc `SimClock.IsNight` dùng sai biên (xem Task 2).

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add HubWorld event loop, trainer FSM, service handling and payday" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Lệnh của Giám đốc và kiểm tra bất biến

**Files:**
- Create: `src/Game.Domain/Hub/HubWorld.Commands.cs`
- Test: `tests/Game.Domain.Tests/HubWorldCommandsTests.cs`

**Interfaces:**
- Consumes: `HubWorld` nội bộ (Task 7): `trainers, buildings, treasury, now, queue`, `Settle`, `LeaveWait`, `SetState`, `OnDecide`, `Raise`.
- Produces: `CommandResult Donate(int trainerId, long gold)`, `CommandResult AdvanceWage(int trainerId, long gold)`, `CommandResult SetPrice(BuildingKind building, long price)`, `void ValidateInvariants()` (ném `InvalidOperationException` nếu vi phạm).

- [ ] **Step 1: Viết test thất bại**

`tests/Game.Domain.Tests/HubWorldCommandsTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Game.Domain;

public class HubWorldCommandsTests
{
    static List<IDomainEvent> Capture(HubWorld w)
    {
        var list = new List<IDomainEvent>();
        w.EventRaised += e => list.Add(e);
        return list;
    }

    // ---- Donate ----
    [Fact]
    public void DonateMovesGoldFromTreasuryToTrainer()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        long before = w.Treasury, goldBefore = w.Trainers[0].Gold;
        var r = w.Donate(0, 500);
        Assert.True(r.Ok);
        Assert.Equal(before - 500, w.Treasury);
        Assert.Equal(goldBefore + 500, w.Trainers[0].Gold);
    }

    [Fact]
    public void DonateIsRejectedForBadArguments()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        Assert.False(w.Donate(99, 10).Ok);
        Assert.False(w.Donate(-1, 10).Ok);
        Assert.False(w.Donate(0, 0).Ok);
        Assert.False(w.Donate(0, -5).Ok);
        Assert.False(w.Donate(0, w.Treasury + 1).Ok);
        Assert.Equal(20000, w.Treasury);
    }

    // ---- AdvanceWage ----
    [Fact]
    public void AdvanceWageLendsGoldAndLowersNextPayroll()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        long dueBefore = w.Forecast.WagesDue;
        Assert.True(w.AdvanceWage(0, 400).Ok);
        Assert.Equal(dueBefore - 400, w.Forecast.WagesDue);
        Assert.Equal(20000 - 400, w.Treasury);
    }

    [Fact]
    public void AdvanceWageIsRejectedForBadArguments()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        Assert.False(w.AdvanceWage(50, 10).Ok);
        Assert.False(w.AdvanceWage(0, 0).Ok);
        Assert.False(w.AdvanceWage(0, w.Treasury + 1).Ok);
    }

    // ---- SetPrice ----
    [Fact]
    public void SetPriceChangesListPriceAndRejectsNonPositive()
    {
        var w = new HubWorld(SimConfig.Default, 1);
        Assert.True(w.SetPrice(BuildingKind.Inn, 90).Ok);
        Assert.Equal(90, w.Buildings[(int)BuildingKind.Inn].Price);
        Assert.Equal(45, w.Buildings[(int)BuildingKind.Inn].FairPrice);
        Assert.False(w.SetPrice(BuildingKind.Inn, 0).Ok);
        Assert.False(w.SetPrice(BuildingKind.Inn, -1).Ok);
        Assert.Equal(90, w.Buildings[(int)BuildingKind.Inn].Price);
    }

    [Fact]
    public void DoubleFairPriceAddsStressToServiceUses()
    {
        var w = new HubWorld(SimConfig.Default, 7);
        w.SetPrice(BuildingKind.Inn, 90);   // gấp đôi giá hợp lý
        var events = Capture(w);
        w.RunFor(3 * 1440);
        var innUses = events.OfType<ServiceUsed>().Where(u => u.Building == BuildingKind.Inn).ToList();
        Assert.NotEmpty(innUses);
        Assert.All(innUses, u => Assert.True(u.StressAdded > 0));
    }

    // ---- Hết tiền ----
    [Fact]
    public void NobodyWaitsForMoneyLongerThanTwentyFourHours()
    {
        var cfg = new SimConfig { MaterialPrice = 1, StartTrainerGold = 0, TrainerCount = 10 };   // Trainer rất nghèo
        var w = new HubWorld(cfg, 21);
        var events = Capture(w);
        for (int m = 0; m < 3; m++) { w.RunUntilPayday(); w.ResolvePayday(); }
        Assert.Contains(events, e => e is DonationReceived d && d.Source == "Patron");   // đã có người phải chờ Tổng tài
        Assert.True(w.MaxMoneyWaitMinutes <= 24 * 60, $"Chờ tiền tối đa {w.MaxMoneyWaitMinutes} phút");
    }

    [Fact]
    public void DirectorDonationWakesATrainerWaitingForMoney()
    {
        var cfg = new SimConfig { StartTrainerGold = 0, TrainerCount = 1, ForcedPersonality = Personality.Timid };
        var w = new HubWorld(cfg, 2);
        // Trainer tự về HUB khi thấp nhu cầu rồi kẹt vì hết tiền; chạy cho tới khi kẹt.
        TrainerView stuck = null;
        for (int i = 0; i < 400 && stuck == null; i++)
        {
            w.RunFor(30);
            stuck = w.Trainers.FirstOrDefault(t => t.State == TrainerState.WaitingForMoney);
        }
        if (stuck == null) return;   // nếu kịch bản không kẹt thì không có gì để kiểm tra
        Assert.True(w.Donate(stuck.Id, 5000).Ok);
        Assert.NotEqual(TrainerState.WaitingForMoney, w.Trainers[stuck.Id].State);
    }

    // ---- Bất biến ----
    [Fact]
    public void InvariantsHoldThroughThreeMonthsWithManyTrainers()
    {
        var w = new HubWorld(new SimConfig { TrainerCount = 30, StartBuildingLevel = 3 }, 13);
        for (int m = 0; m < 3; m++)
        {
            for (int step = 0; step < 30; step++)
            {
                w.RunFor(1440);
                w.ValidateInvariants();
                if (w.Forecast.DaysLeft == 0) break;
            }
            w.RunUntilPayday();
            w.ValidateInvariants();
            w.ResolvePayday();
            w.ValidateInvariants();
        }
        Assert.True(w.Treasury >= 0);
        Assert.All(w.Trainers, t =>
        {
            Assert.InRange(t.Stamina, 0, 100);
            Assert.InRange(t.Satiety, 0, 100);
            Assert.InRange(t.Hydration, 0, 100);
            Assert.InRange(t.Stress, 0, 100);
        });
    }

    [Fact]
    public void DomainAssemblyDoesNotReferenceUnity()
    {
        var refs = typeof(HubWorld).Assembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain(refs, n => n.StartsWith("UnityEngine"));
    }
}
```

- [ ] **Step 2: Chạy để thấy lỗi biên dịch**

Run: `dotnet test tests/Game.Domain.Tests --filter HubWorldCommandsTests`
Expected: FAIL (build error: `Donate`, `AdvanceWage`, `SetPrice`, `ValidateInvariants` không tồn tại).

- [ ] **Step 3: Viết `HubWorld.Commands.cs`**

`src/Game.Domain/Hub/HubWorld.Commands.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>
        /// Giám đốc donate Gold cho một Trainer (không hoàn lại). Lỗi của người chơi trả về Rejected.
        /// Nếu Trainer đang kẹt vì hết tiền thì được xử lý lại ngay.
        /// </summary>
        public CommandResult Donate(int trainerId, long gold)
        {
            CommandResult check = CheckTransfer(trainerId, gold);
            if (!check.Ok) return check;

            Trainer t = trainers[trainerId];
            treasury.TrySpend(gold);
            t.Gold += gold;
            Raise(new TreasuryChanged(now, -gold, treasury.Balance, "Donate"));
            Raise(new DonationReceived(now, trainerId, gold, "Director"));
            WakeIfWaitingForMoney(t);
            return CommandResult.Success();
        }

        /// <summary>Ứng lương: Giám đốc đưa Gold trước, khoản này được trừ vào lương Payday kế tiếp.</summary>
        public CommandResult AdvanceWage(int trainerId, long gold)
        {
            CommandResult check = CheckTransfer(trainerId, gold);
            if (!check.Ok) return check;

            Trainer t = trainers[trainerId];
            treasury.TrySpend(gold);
            t.Gold += gold;
            t.WageAdvance += gold;
            Raise(new TreasuryChanged(now, -gold, treasury.Balance, "AdvanceWage"));
            WakeIfWaitingForMoney(t);
            return CommandResult.Success();
        }

        /// <summary>Đặt giá bán của một công trình dịch vụ (Bệnh Viện: giá trên mỗi 10 HP). Giá phải &gt; 0.</summary>
        public CommandResult SetPrice(BuildingKind building, long price)
        {
            if (price <= 0) return CommandResult.Rejected("Giá phải lớn hơn 0.");
            buildings[(int)building].Price = price;
            return CommandResult.Success();
        }

        CommandResult CheckTransfer(int trainerId, long gold)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return CommandResult.Rejected("Trainer không tồn tại.");
            if (gold <= 0) return CommandResult.Rejected("Số Gold phải lớn hơn 0.");
            if (gold > treasury.Balance) return CommandResult.Rejected("Kho bạc không đủ Gold.");
            return CommandResult.Success();
        }

        /// <summary>Trainer đang kẹt vì hết tiền được xử lý lại ngay sau khi nhận tiền (hủy sự kiện chờ cũ bằng token).</summary>
        void WakeIfWaitingForMoney(Trainer t)
        {
            if (t.State != TrainerState.WaitingForMoney) return;
            Settle(t);
            LeaveWait(t);
            t.Token++;
            SetState(t, TrainerState.AtHub, "Funded");
            OnDecide(t);
        }

        /// <summary>
        /// Kiểm tra các bất biến của mô phỏng, ném InvalidOperationException nếu vi phạm:
        /// Kho bạc không âm; thanh nhu cầu trong 0-100; mỗi Trainer không ở hai chỗ cùng lúc;
        /// mỗi Trainer có tối đa một sự kiện cá nhân còn hiệu lực trong hàng đợi.
        /// </summary>
        public void ValidateInvariants()
        {
            if (treasury.Balance < 0) throw new InvalidOperationException("Kho bạc âm.");

            var seated = new HashSet<int>();
            foreach (ServiceBuilding b in buildings)
                foreach (int id in b.Occupants)
                    if (!seated.Add(id)) throw new InvalidOperationException($"Trainer {id} đang ở hai chỗ cùng lúc.");

            foreach (Trainer t in trainers)
            {
                Needs n = t.Needs;
                if (n.Stamina < 0 || n.Stamina > 100 || n.Satiety < 0 || n.Satiety > 100
                    || n.Hydration < 0 || n.Hydration > 100 || n.Stress < 0 || n.Stress > 100)
                    throw new InvalidOperationException($"Trainer {t.Id} có thanh nhu cầu ngoài khoảng 0-100.");
            }

            var pending = new int[trainers.Count];
            foreach (SimEvent e in queue.Snapshot)
                if (e.TrainerId >= 0 && trainers[e.TrainerId].Token == e.Token) pending[e.TrainerId]++;
            for (int i = 0; i < pending.Length; i++)
                if (pending[i] > 1) throw new InvalidOperationException($"Trainer {i} có {pending[i]} sự kiện đang chờ.");
        }
    }
}
```

- [ ] **Step 4: Chạy toàn bộ test**

Run: `dotnet test tests/Game.Domain.Tests`
Expected: PASS toàn bộ.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add director commands and invariant checks to HubWorld" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 9: Kịch bản Game.Sim `core`, cập nhật tài liệu, kiểm tra cuối

**Files:**
- Modify: `tools/Game.Sim/Program.cs`
- Modify: `README.md`, `docs/designs/13_Balance_Parameters.md`, `docs/99_Open_Issues.md`

**Interfaces:**
- Consumes: `HubWorld`, `SimConfig`, `TrainerStateChanged`, `PaydayOutcome` và các enum.
- Produces: kịch bản `core` (mặc định khi chạy không tham số) in báo cáo; Game.Sim vẫn có `ladders`, `stock`.

- [ ] **Step 1: Thêm kịch bản `core` vào Program.cs**

Trong `tools/Game.Sim/Program.cs`, thêm `using System.Collections.Generic;` và `using System.Diagnostics;` vào đầu file, thêm hàm `Core()` ngay trên `static void Main`, và thay phần thân `Main`:

```csharp
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
```

(Xóa phiên bản `Main` cũ in dòng "Dùng: ..." ở Task 1.)

- [ ] **Step 2: Chạy kịch bản core**

Run: `dotnet run --project tools/Game.Sim -c Release`
Expected: in bảng 3 tháng, tỉ lệ thời gian (các phần trăm cộng lại 100%), bảng công trình, dòng "Thời gian chạy: N ms" với N < 1000, không có exception. Số liệu cụ thể chưa có tiêu chí đúng/sai (đây là giá trị khởi điểm); ghi lại kết quả để dán vào commit message.

- [ ] **Step 3: Cập nhật README, doc 13, Open Issues**

```bash
python3 - <<'EOF'
import re
# README: thay phần lệnh Game.Sim
p = 'README.md'
s = open(p, encoding='utf-8').read()
start = s.index('## Mô phỏng kinh tế')
end = s.index('Logic nằm ở `src/Game.Domain`')
new = '''## Mô phỏng kinh tế

```
dotnet run --project tools/Game.Sim            # kịch bản core: 10 Trainer, 3 tháng, dòng tiền và cách Trainer dùng thời gian
dotnet run --project tools/Game.Sim -- ladders # chi phí kỳ vọng Nâng Sao, Tinh Luyện, tăng tư chất
dotnet run --project tools/Game.Sim -- stock   # tần suất sự kiện cổ phiếu
dotnet test tests/Game.Domain.Tests            # unit test Domain
```

'''
s = s[:start] + new + s[end:]
open(p, 'w', encoding='utf-8').write(s)

# Doc 13: ghi chú nguồn số liệu cũ
p = 'docs/designs/13_Balance_Parameters.md'
s = open(p, encoding='utf-8').read()
marker = '## 9. Kết quả và giới hạn của mô phỏng sơ bộ'
note = '''> **Ghi chú (sub-project 1):** các số liệu ở §8-§13 do mô hình gộp theo ngày `HubEconomy` tạo ra. Mô hình đó đã bị xóa khi Domain chuyển sang mô phỏng sự kiện rời rạc (xem `docs/superpowers/specs/2026-10-01-domain-core-time-trainer-design.md`). Các con số vẫn dùng làm điểm xuất phát, nhưng cần mô phỏng lại bằng `tools/Game.Sim` khi các hệ thống tương ứng được dựng lại.

'''
if note not in s:
    s = s.replace(marker, note + marker, 1)
open(p, 'w', encoding='utf-8').write(s)

# Open Issues: cập nhật mục O7
p = 'docs/99_Open_Issues.md'
lines = open(p, encoding='utf-8').read().split('\n')
for i, l in enumerate(lines):
    if l.startswith('| O7 |'):
        lines[i] = '| O7 | P1 | Lõi mới (sub-project 1) đã giải quyết: lương hợp đồng, chi phí vận hành, Stress theo giá, số công trình. Còn lại: hạn mức vay 2 lương và Gene Bank thu theo Payday (sub-project 5), `RebellionModel` dùng cấp quy đổi Trainer (sub-project 3). | Sửa khi làm sub-project tương ứng. |'
open(p, 'w', encoding='utf-8').write('\n'.join(lines))
EOF
git diff --stat
```
Expected: 3 file thay đổi.

- [ ] **Step 4: Kiểm tra cuối**

```bash
dotnet build && dotnet test tests/Game.Domain.Tests
grep -rn "HubEconomy\|EconomyParams" --include=*.cs --include=*.csproj . ; echo "grep exit: $?"
```
Expected: build thành công; test `Passed!`; `grep` không in dòng nào và `grep exit: 1` (không còn tham chiếu). Các tham chiếu `HubEconomy` trong file `.md` (docs lịch sử) là chấp nhận được.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add Game.Sim core scenario and update docs for the new Domain core" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review (đã chạy)

**Spec coverage:** §1 phạm vi -> Task 1-9. §2 kiến trúc/`long`/tất định -> Task 2-3, Global Constraints. §3 thời gian và sự kiện -> Task 2, Task 7 (`Dispatch`, Dawn/Dusk/DayStart/PaydayDue). §4 FSM, `PickService`, `TrainerDecide`, ban đêm -> Task 3 (`TrainerBrain`), Task 7. §5 công trình, số chỗ, giá, Stress theo giá, vận hành -> Task 4, Task 7 (`StartService`, `OnDayStart`). §6 xếp hàng, hết tiền, Tổng tài, lệnh Giám đốc -> Task 7 (`OnWaitTick`), Task 8. §7 Payday -> Task 5 (`Payroll`), Task 7 (`ResolvePayday`). §8 giao diện -> Task 6-8. §9 tham số -> Task 2 (kèm đồng bộ spec). §10 kiểm thử -> Task 2-8. §11 Game.Sim và dọn dẹp -> Task 1, 9. §12 tiêu chí -> Task 9 Step 2 và 4.

**Chỗ lệch spec đã xử lý:** `TrainerBrainTests`/`HubWorldTests` chia nhiều file hơn spec; 3 tham số farm/Stress/giá nguyên liệu khác bảng §9 gốc và spec được đồng bộ ở Task 2 Step 8. Bệnh Viện tính "14 Gold mỗi 10 HP" (tương đương 1.4 Gold/HP) vì Gold là `long`.

**Rủi ro biết trước:** (1) Task 7 là task lớn nhất; test của nó chỉ chắc chắn đúng nếu các tham số khởi điểm không làm Trainer kẹt vĩnh viễn (đã có Tổng tài chặn ở 24 giờ). (2) Test `DirectorDonationWakesATrainerWaitingForMoney` có nhánh `return` sớm nếu kịch bản không sinh ra Trainer kẹt tiền; test `NobodyWaitsForMoneyLongerThanTwentyFourHours` mới là test chính cho bất biến này. (3) Số liệu kịch bản `core` là giá trị khởi điểm chưa cân bằng; mục tiêu của kế hoạch này là động cơ đúng, không phải cân bằng.
