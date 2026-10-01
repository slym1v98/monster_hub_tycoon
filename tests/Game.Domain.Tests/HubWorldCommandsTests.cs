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
        var cfg = new SimConfig { StartTrainerGold = 0, TrainerCount = 1, ForcedPersonality = Personality.Timid, PatronChancePerHour = 0, MaterialPrice = 1, FarmGoldPerChunk = 0 };
        var w = new HubWorld(cfg, 2);
        // Trainer tự về HUB khi thấp nhu cầu rồi kẹt vì hết tiền; chạy cho tới khi kẹt.
        TrainerView stuck = null;
        for (int i = 0; i < 3000 && stuck == null; i++)
        {
            w.RunFor(10);
            stuck = w.Trainers.FirstOrDefault(t => t.State == TrainerState.WaitingForMoney);
        }
        Assert.NotNull(stuck);   // bắt buộc có Trainer kẹt, tránh test chạy rỗng (không Tổng tài, không Gold farm, vật liệu rẻ)
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
