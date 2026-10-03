using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Game.Domain;
using Game.Domain.Materials;
using Game.Domain.Monsters;

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
        var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), 1);
        var result = w.RunUntilPayday();
        Assert.Equal(StopReason.PaydayDue, result.Stop);
        Assert.Equal(SimClock.PaydayMinute(0), w.Now.TotalMinutes);
        Assert.Equal(0, w.Forecast.DaysLeft);
    }

    [Fact]
    public void RunForReportsRemainingMinutesWhenPaydayInterrupts()
    {
        var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), 1);
        var result = w.RunFor(100_000);
        Assert.Equal(StopReason.PaydayDue, result.Stop);
        Assert.Equal(43199 - SimConfig.Default.StartMinute, result.MinutesRun);
        Assert.Equal(100_000 - result.MinutesRun, result.RemainingMinutes);
    }

    [Fact]
    public void RunForDoesNothingUntilPaydayIsResolved()
    {
        var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), 1);
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
        var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), 1);
        Assert.Throws<InvalidOperationException>(() => w.ResolvePayday());
    }

    [Fact]
    public void NextPaydayComesThirtyDaysLater()
    {
        var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), 1);
        w.RunUntilPayday(); w.ResolvePayday();
        w.RunUntilPayday();
        Assert.Equal(SimClock.PaydayMinute(1), w.Now.TotalMinutes);
    }

    [Fact]
    public void ForecastShowsDaysLeftAndWageBill()
    {
        var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), 1);
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
            var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), seed);
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
        var w = new HubWorld(new SimConfig { TrainerCount = 10 }.WithServiceFacilities(), 3);
        w.RunFor(1200 - 360);   // tới 20:00 ngày 1
        Assert.All(w.Trainers, t => Assert.DoesNotContain(t.State,
            new[] { TrainerState.Farming, TrainerState.Traveling, TrainerState.Returning }));
    }

    [Fact]
    public void DayPhaseEventsFireAtDawnAndDusk()
    {
        var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), 3);
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
        var w = new HubWorld(new SimConfig { TrainerCount = 30, StartBuildingLevel = 1 }.WithServiceFacilities(), 5);
        w.RunFor(3 * 1440);
        Assert.True(w.Buildings[(int)BuildingKind.Inn].MaxQueueLength > 0);
    }

    [Fact]
    public void FairPricesAddNoStress()
    {
        var w = new HubWorld(SimConfig.Default.WithServiceFacilities(), 7);
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
        var w = new HubWorld(new SimConfig { BaseWage = 1_000_000 }.WithServiceFacilities(), 9);
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

    [Fact]
    public void StrikeSendsOutsideTrainersHomeEvenAtNight()
    {
        // Có kính nhìn đêm nên Trainer vẫn ở ngoài lúc 23:59 khi đến kỳ lương.
        var strikeConfig = new SimConfig { BaseWage = 1_000_000, StartWithNightVision = true, TrainerCount = 10 }.WithServiceFacilities();
        var w = new HubWorld(strikeConfig, 9, new FixedFarm(0, 0, 0), new FixedPriceMarket(strikeConfig));
        w.RunUntilPayday();
        var outsideIds = w.Trainers
            .Where(t => t.State == TrainerState.Farming || t.State == TrainerState.Traveling)
            .Select(t => t.Id).ToList();
        Assert.NotEmpty(outsideIds);   // tránh test rỗng

        var outcome = w.ResolvePayday();
        Assert.True(outcome.StrikeStarted);

        Assert.All(w.Trainers, t => Assert.DoesNotContain(t.State,
            new[] { TrainerState.Farming, TrainerState.Traveling }));
        foreach (int id in outsideIds)
        {
            var t = w.Trainers.First(x => x.Id == id);
            Assert.Equal(TrainerState.Returning, t.State);
            Assert.Equal("Strike", t.StateReason);
        }
    }

    [Fact]
    public void StrikeKeepsTrainersOffTheFieldInDaylight()
    {
        var w = new HubWorld(new SimConfig { BaseWage = 1_000_000 }.WithServiceFacilities(), 9);
        w.RunUntilPayday();
        w.ResolvePayday();
        w.RunFor(10 * 60);   // tới 09:59 sáng hôm sau, vẫn trong 5 ngày đình công
        Assert.False(w.Now.IsNight);   // đảm bảo đây là ban ngày, không phải NightRest
        Assert.All(w.Trainers, t => Assert.True(t.StrikeDaysLeft > 0));
        Assert.All(w.Trainers, t => Assert.DoesNotContain(t.State,
            new[] { TrainerState.Farming, TrainerState.Traveling, TrainerState.Returning }));
    }

    [Fact]
    public void StrikeLastsFiveFullDays()
    {
        var w = new HubWorld(new SimConfig { BaseWage = 1_000_000 }.WithServiceFacilities(), 9);
        w.RunUntilPayday();
        Assert.True(w.ResolvePayday().StrikeStarted);
        w.RunFor(1 + 4 * 1440 + 60);   // qua 4 lần nửa đêm đầy đủ, hết ngày đình công thứ 5 chưa tới
        Assert.All(w.Trainers, t => Assert.True(t.StrikeDaysLeft > 0));
        w.RunFor(1440);
        Assert.All(w.Trainers, t => Assert.Equal(0, t.StrikeDaysLeft));
    }

    [Fact]
    public void StrikingTrainerNeverUsesTheHospital()
    {
        // Mỗi khúc farm mất 100 HP nên Trainer ở ngoài lúc 23:59 gần như chắc chắn đang thiếu HP.
        var cfg = new SimConfig { BaseWage = 1_000_000, StartWithNightVision = true }.WithServiceFacilities();
        var w = new HubWorld(cfg, 9, new FixedFarm(0, 0, 100), null);
        var events = Capture(w);
        w.RunUntilPayday();
        Assert.Contains(w.Trainers, t => t.Monsters.Any(m => m.CurrentHp < m.MaxHp));   // tiền đề: có người thiếu HP khi đình công bắt đầu
        Assert.Contains(events, e => e is ServiceUsed u && u.Building == BuildingKind.Hospital);   // và Bệnh Viện vẫn được dùng bình thường trước đó
        int from = events.Count;
        w.ResolvePayday();
        w.RunFor(3 * 1440);
        Assert.DoesNotContain(events.Skip(from).OfType<ServiceUsed>(), u => u.Building == BuildingKind.Hospital);
    }

    // ---- Chế độ cấn nợ (vỡ nợ lần 3) ----
    static HubWorld ReachDebtMode(HubWorld w)
    {
        PaydayOutcome last = null;
        for (int m = 0; m < 3; m++) { w.RunUntilPayday(); last = w.ResolvePayday(); }
        Assert.Equal(3, last.UnpaidStreak);   // tiền đề: đã vào chế độ cấn nợ
        return w;
    }

    static int FirstBackpackAfterFirstChunk(HubWorld w)
    {
        for (int i = 0; i < 4 * 1440 && w.Trainers[0].BackpackUnits == 0; i++) w.RunFor(10);
        return w.Trainers[0].BackpackUnits;
    }

    [Fact]
    public void DebtModeHalvesFarmYield()
    {
        var cfg = new SimConfig { BaseWage = 1_000_000, TrainerCount = 1 }.WithServiceFacilities();
        int normal = FirstBackpackAfterFirstChunk(new HubWorld(cfg, 5, new FixedFarm(10, 100, 0), null));
        Assert.Equal(10, normal);   // tiền đề: bộ giải cố định cho 10 đơn vị mỗi khúc

        var w = ReachDebtMode(new HubWorld(new SimConfig { BaseWage = 1_000_000, TrainerCount = 1 }.WithServiceFacilities(), 5, new FixedFarm(10, 100, 0), null));
        int inDebt = 0;
        for (int i = 0; i < 20 * 1440 && inDebt == 0; i++) { w.RunFor(10); inDebt = w.Trainers[0].BackpackUnits; }
        Assert.Equal(5, inDebt);   // 10 x 0.5
    }

    [Fact]
    public void DebtModeRestaurantIsFreeAndPaysDownTheWageOwed()
    {
        var w = new HubWorld(new SimConfig { BaseWage = 1_000_000, TrainerCount = 1 }.WithServiceFacilities(), 5, new FixedFarm(10, 100, 0), null);
        ReachDebtMode(w);
        long owedAtStart = w.Trainers[0].WageOwed;
        Assert.True(owedAtStart > 0);

        var freeMeals = new List<(long Paid, long Owed)>();
        w.EventRaised += e =>
        {
            if (e is ServiceUsed u && u.Building == BuildingKind.Restaurant) freeMeals.Add((u.Paid, w.Trainers[0].WageOwed));
        };
        w.RunFor(10 * 1440);

        Assert.NotEmpty(freeMeals);                          // tiền đề: có ăn ở Nhà Hàng trong lúc cấn nợ
        Assert.All(freeMeals, m => Assert.Equal(0, m.Paid));
        Assert.True(freeMeals[0].Owed < owedAtStart);        // nợ lương giảm ngay lượt đầu
        for (int i = 1; i < freeMeals.Count; i++) Assert.True(freeMeals[i].Owed < freeMeals[i - 1].Owed);
    }

    // ---- Chi phí vận hành công trình ----
    [Fact]
    public void UnpaidUpkeepHalvesSlotsAndRecoversWhenTreasuryRefills()
    {
        // Không có nguồn thu: Trainer hết tiền, không Tổng tài, Kho bạc 0.
        var cfg = new SimConfig { StartTreasury = 0, StartTrainerGold = 0, MaterialPrice = 0,
                                  StartBuildingLevel = 5, StartTownHallLevel = 5,
                                  PatronChancePerHour = 0, PatronGuaranteedAfterHours = 100_000 }.WithServiceFacilities();
        var w = new HubWorld(cfg, 8, new FixedFarm(0, 0, 0), new FixedPriceMarket(cfg));
        var events = Capture(w);
        Assert.All(w.Buildings, b => Assert.Equal(9, b.Slots));   // tiền đề: Lv5 có 9 chỗ

        w.RunFor(1440 - SimClock.DawnMinute);   // tới đúng 00:00 đầu tiên
        Assert.Equal(0, w.Treasury);
        foreach (BuildingKind kind in Enum.GetValues(typeof(BuildingKind)))
            Assert.Contains(events, e => e is BuildingMaintenanceChanged m && m.Minute == 1440 && m.Building == kind && !m.Maintained);
        Assert.All(w.Buildings, b => { Assert.False(b.Maintained); Assert.Equal(4, b.Slots); });

        // Có nguồn thu trở lại (Tổng tài xuất hiện, Trainer trả tiền dịch vụ) thì 00:00 kế tiếp bảo trì lại được.
        cfg.PatronChancePerHour = 1;
        w.RunFor(1440);
        Assert.True(w.Treasury > 0);
        Assert.Contains(events, e => e is BuildingMaintenanceChanged m && m.Minute == 2880 && m.Maintained);
        Assert.Contains(w.Buildings, b => b.Maintained && b.Slots == 9);
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

/// <summary>Bộ giải farm cố định: mỗi khúc luôn cho cùng một kết quả (tiện kiểm tra chính xác).</summary>
sealed class FixedFarm : IExpeditionResolver
{
    readonly int units; readonly long gold; readonly long hpLost;
    public FixedFarm(int units, long gold, long hpLost) { this.units = units; this.gold = gold; this.hpLost = hpLost; }
    public ExpeditionResult Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random)
    {
        var materials = units == 0 ? Array.Empty<MaterialQuantity>() : new[] { new MaterialQuantity(MaterialId.For(MaterialFamily.Ore, 1), units) };
        var hp = new Dictionary<MonsterId, long>(); long remaining = hpLost;
        foreach (var member in trainer.Team) { long damage = Math.Min(member.CurrentHp, remaining); hp[member.Id] = member.CurrentHp - damage; remaining -= damage; }
        return new ExpeditionResult(Array.Empty<Game.Domain.Combat.BattleResult>(), new ExpeditionLoot(materials, Array.Empty<MaterialQuantity>(), gold, 0), 0, hp);
    }
}
