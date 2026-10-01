using System.Linq;
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

public class PersonalityAndGeneBankTests
{
    static HubEconomy Hub(EconomyParams p, Personality? ps, int days = 180)
    {
        var hub = new HubEconomy(p, 1e9, Enumerable.Repeat(Rarity.Common, 20).ToList(), 5, ps);
        for (int i = 0; i < days; i++) hub.StepDay();
        return hub;
    }

    [Fact] public void GluttonsSpendMoreOnFoodAndTimidSpendLessOnHospital()
    {
        var p = new EconomyParams { PersonalityEnabled = true, Buildings = 0, ReinvestRate = 0 };
        var glutton = Hub(p, Personality.Glutton); var timid = Hub(p, Personality.Timid); var warlike = Hub(p, Personality.Warlike);
        Assert.True(glutton.LastMonthServiceRevenue[(int)ServiceKind.Food] > timid.LastMonthServiceRevenue[(int)ServiceKind.Food]);
        Assert.True(warlike.LastMonthServiceRevenue[(int)ServiceKind.Hospital] > timid.LastMonthServiceRevenue[(int)ServiceKind.Hospital]);
    }

    [Fact] public void CapitalistsDemandHigherWages()
    {
        var p = new EconomyParams { PersonalityEnabled = true, Buildings = 0, ReinvestRate = 0 };
        Assert.True(Hub(p, Personality.Capitalist).LastMonthWages > Hub(p, Personality.Timid).LastMonthWages);
    }

    [Fact] public void PersonalityOffMeansNoDifference()
    {
        var p = new EconomyParams { PersonalityEnabled = false };
        Assert.Equal(Hub(p, Personality.Warlike).Treasury, Hub(p, Personality.Timid).Treasury);
    }

    [Fact] public void MonthlyBillingKeepsMonstersAtLowFee()
    {
        var p = new EconomyParams { GeneBankEnabled = true, GeneBankMonthlyBilling = true, GeneBankFeePerMonsterDay = 10 };
        Assert.Equal(0, Hub(p, null, 360).MonstersSeized);
    }

    [Fact] public void DailyBillingSeizesMonstersFromBrokeTrainers()
    {
        var p = new EconomyParams { GeneBankEnabled = true, GeneBankMonthlyBilling = false, GeneBankFeePerMonsterDay = 20 };
        Assert.True(Hub(p, null, 360).MonstersSeized > 0);
    }
}
