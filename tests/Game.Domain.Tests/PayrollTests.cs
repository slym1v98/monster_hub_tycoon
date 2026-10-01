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
