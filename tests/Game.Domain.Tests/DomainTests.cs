using System;
using System.Linq;
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
            Assert.True(p <= prev + 1e-12, $"+{lv} cao hon cap truoc");
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

public class HubEconomyTests
{
    static readonly Rarity[] Roster = { Rarity.Common, Rarity.Common, Rarity.Rare, Rarity.Epic };

    static HubEconomy Simulate(EconomyParams p, double start, int days, int seed)
    {
        var hub = new HubEconomy(p, start, Roster, seed);
        for (int i = 0; i < days; i++) hub.StepDay();
        return hub;
    }

    [Fact]
    public void SameSeedGivesSameResult()
    {
        var a = Simulate(new EconomyParams(), 5000, 90, 11);
        var b = Simulate(new EconomyParams(), 5000, 90, 11);
        Assert.Equal(a.Treasury, b.Treasury);
    }

    [Fact]
    public void TrainerGoldNeverNegative()
    {
        var hub = Simulate(new EconomyParams { TaxRate = 0.4 }, 0, 120, 3);
        Assert.All(hub.Trainers, t => Assert.True(t.Gold >= 0));
    }

    [Fact]
    public void UnpayableWageCausesStrike()
    {
        var hub = Simulate(new EconomyParams { WageRatio = 50 }, 0, 30, 5);
        Assert.Equal(1, hub.StrikePaydays);
    }

    [Fact]
    public void HigherTaxMakesTrainersPoorer()
    {
        double low = Simulate(new EconomyParams { TaxRate = 0.1 }, 5000, 180, 7).AvgTrainerGold();
        double high = Simulate(new EconomyParams { TaxRate = 0.4 }, 5000, 180, 7).AvgTrainerGold();
        Assert.True(high < low);
    }

    [Fact]
    public void LargerReserveLowersStrikeRisk()
    {
        int Strikes(double reserve) => Enumerable.Range(0, 30).Sum(seed =>
            Simulate(new EconomyParams { ReserveWageMultiple = reserve, ShockChancePerMonth = 0.5 }, 20000, 360, seed).StrikePaydays);
        Assert.True(Strikes(2.0) <= Strikes(0.5));
    }

    [Fact]
    public void GameDomainDoesNotReferenceUnity()
    {
        var refs = typeof(HubEconomy).Assembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain(refs, n => n.StartsWith("UnityEngine"));
    }
}
