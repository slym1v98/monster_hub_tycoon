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
