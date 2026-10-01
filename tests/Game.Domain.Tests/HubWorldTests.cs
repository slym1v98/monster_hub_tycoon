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

    [Fact]
    public void StrikeSendsOutsideTrainersHomeEvenAtNight()
    {
        // Có kính nhìn đêm nên Trainer vẫn ở ngoài lúc 23:59 khi đến kỳ lương.
        var w = new HubWorld(new SimConfig { BaseWage = 1_000_000, StartWithNightVision = true, TrainerCount = 10 }, 9);
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
        var w = new HubWorld(new SimConfig { BaseWage = 1_000_000 }, 9);
        w.RunUntilPayday();
        w.ResolvePayday();
        w.RunFor(10 * 60);   // tới 09:59 sáng hôm sau, vẫn trong 5 ngày đình công
        Assert.False(w.Now.IsNight);   // đảm bảo đây là ban ngày, không phải NightRest
        Assert.All(w.Trainers, t => Assert.True(t.StrikeDaysLeft > 0));
        Assert.All(w.Trainers, t => Assert.DoesNotContain(t.State,
            new[] { TrainerState.Farming, TrainerState.Traveling, TrainerState.Returning }));
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
