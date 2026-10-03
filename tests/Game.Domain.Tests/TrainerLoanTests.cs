using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Xunit;

public sealed class TrainerLoanTests
{
    static HubWorld Create(TrainerLoanConfig settings = null) => new HubWorld(new SimConfig
    {
        TrainerCount = 1, StartTreasury = 100000, StartTrainerGold = 0,
        UnlockedZoneIds = System.Array.Empty<string>(), UpkeepPerBuildingPerDay = 0,
        TrainerLoanSettings = settings ?? TrainerLoanConfig.Prototype
    }.WithServiceFacilities(), 411);

    static HubWorld CreateRankFiveLender() => new HubWorld(new SimConfig
    {
        TrainerCount = 1, StartTreasury = 100000, StartTrainerGold = 1000,
        UnlockedZoneIds = System.Array.Empty<string>(), StartingTrainerRanks = new[] { 5 }
    }.WithServiceFacilities(), 412);

    [Fact]
    public void DirectorLoanTransfersTreasuryGoldAndEnforcesLimit()
    {
        var world = Create();
        long limit = world.TrainerLoanLimit(0);
        long treasury = world.Treasury;
        Assert.True(world.LendToTrainer(0, limit).Ok);
        Assert.Equal(limit, world.Trainers[0].HubLoanBalance);
        Assert.Equal(limit, world.Trainers[0].Gold);
        Assert.Equal(treasury - limit, world.Treasury);
        Assert.False(world.LendToTrainer(0, 1).Ok);
        Assert.Equal(limit, world.Trainers[0].HubLoanBalance);
    }

    [Fact]
    public void DirectorCanAdjustLoanRateOnlyWithinGddRange()
    {
        var world = Create();
        Assert.False(world.SetTrainerLoanInterestRate(0.049).Ok);
        Assert.False(world.SetTrainerLoanInterestRate(0.401).Ok);
        Assert.True(world.SetTrainerLoanInterestRate(0.25).Ok);
    }

    [Fact]
    public void LoanInterestCapitalizesAndOverLimitForTwoPaydaysStartsStrike()
    {
        var world = Create(new TrainerLoanConfig(interestPerPayday: 0.10,
            monthlyWageLimitMultiplier: 2, incomeRepaymentFraction: 0, overLimitPaydaysToStrike: 2));
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        long limit = world.TrainerLoanLimit(0);
        Assert.True(world.LendToTrainer(0, limit).Ok);
        for (int payday = 0; payday < 2; payday++)
        {
            world.RunUntilPayday();
            world.ResolvePayday();
        }
        Assert.True(world.Trainers[0].HubLoanBalance > limit);
        Assert.Equal(2, events.OfType<TrainerLoanOverdueStrike>().Single().ConsecutivePaydays);
        Assert.True(world.Trainers[0].StrikeDaysLeft > 0);
        Assert.Equal(2, events.OfType<TrainerLoanBalanceChanged>().Count(e => e.Reason == "PaydayInterest"));
    }

    [Fact]
    public void IncomeRepaymentIsTransferredBackToTreasury()
    {
        var world = Create();
        long limit = world.TrainerLoanLimit(0);
        Assert.True(world.LendToTrainer(0, limit).Ok);
        long balanceBefore = world.Trainers[0].HubLoanBalance;
        long treasuryBefore = world.Treasury;
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        // Payday wage is income and half of it is automatically applied to the outstanding balance.
        world.RunUntilPayday();
        world.ResolvePayday();
        Assert.True(world.Trainers[0].HubLoanBalance < balanceBefore + (long)(balanceBefore * 0.10));
        Assert.True(world.Treasury > treasuryBefore - limit);
        Assert.Contains(events.OfType<TrainerLoanRepaid>(), e => e.IncomeSource == "PaydayWages" && e.Amount > 0);
    }

    [Fact]
    public void ReverseLoanRequiresRankFiveAndCapsAtHalfAvailableCash()
    {
        var ordinary = Create();
        Assert.False(ordinary.BorrowFromTrainer(0, 1).Ok);

        var world = CreateRankFiveLender();
        long treasury = world.Treasury;
        Assert.False(world.BorrowFromTrainer(0, 501).Ok);
        Assert.True(world.BorrowFromTrainer(0, 500).Ok);
        Assert.Equal(500, world.Trainers[0].ReverseLoanBalance);
        Assert.Equal(500, world.Trainers[0].Gold);
        Assert.Equal(treasury + 500, world.Treasury);
        Assert.False(world.BorrowFromTrainer(0, 1).Ok);
    }

    [Fact]
    public void ReverseLoanAccruesFivePercentAndOverdueServicesOffsetTheDebt()
    {
        var world = CreateRankFiveLender();
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        Assert.True(world.BorrowFromTrainer(0, 500).Ok);
        for (int i = 0; i < 2; i++)
        {
            world.RunUntilPayday();
            world.ResolvePayday();
        }
        Assert.True(world.Trainers[0].ReverseLoanOverdue);
        long maturedBalance = world.Trainers[0].ReverseLoanBalance;
        Assert.True(maturedBalance >= 551);
        world.RunFor(2 * SimClock.MinutesPerDay);
        Assert.Contains(events.OfType<ReverseLoanServiceOffset>(), e => e.ServiceValue > 0);
        Assert.True(world.Trainers[0].ReverseLoanBalance < maturedBalance);
    }
}
