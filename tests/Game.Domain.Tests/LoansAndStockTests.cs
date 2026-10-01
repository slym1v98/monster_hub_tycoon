using System.Linq;
using Xunit;
using Game.Domain;

public class LoanTests
{
    static HubEconomy Run(EconomyParams p, int days = 360, int seed = 3)
    {
        var hub = new HubEconomy(p, 20000, Enumerable.Repeat(Rarity.Common, 10).ToList(), seed);
        for (int i = 0; i < days; i++) hub.StepDay();
        return hub;
    }

    [Fact]
    public void NoDebtWhenLoansDisabled()
    {
        var hub = Run(new EconomyParams { LoansEnabled = false });
        Assert.All(hub.Trainers, t => Assert.Equal(0, t.Debt));
        Assert.Equal(0, hub.InterestAccrued);
    }

    [Fact]
    public void ZeroInterestAccruesNothing()
    {
        var hub = Run(new EconomyParams { LoansEnabled = true, LoanInterest = 0 });
        Assert.Equal(0, hub.InterestAccrued);
        Assert.Contains(hub.Trainers, t => t.Debt > 0);   // Trainer Common van vay vi thu nhap < nhu cau
    }

    [Fact]
    public void HigherInterestAccruesMoreAndDebtIsNeverNegative()
    {
        var low = Run(new EconomyParams { LoansEnabled = true, LoanInterest = 0.05 });
        var high = Run(new EconomyParams { LoansEnabled = true, LoanInterest = 0.40 });
        Assert.True(high.InterestAccrued > low.InterestAccrued);
        Assert.All(high.Trainers, t => Assert.True(t.Debt >= 0));
    }

    [Fact]
    public void LargeLimitAndHighInterestCauseOverdueStrikes()
    {
        var hub = Run(new EconomyParams { LoansEnabled = true, LoanInterest = 0.40, LoanLimitWages = 3 });
        Assert.True(hub.DebtStrikes > 0);
    }
}

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
