using Xunit;
using Game.Domain;

public class RebellionTests
{
    [Fact] public void SameRankMonsterDoesNotRebelBecauseMonsterLevelRoundsUp()
        => Assert.False(RebellionModel.IsRebellious(2, Rarity.Rare, 1, 6, Rarity.Rare));

    [Fact] public void LeadershipUsesFractionalRankAwareTrainerManagementLevel()
        => Assert.Equal(61.2, RebellionModel.LeadershipScore(2, 6, Rarity.Rare), 10);

    [Fact] public void OneRarityGapIsWorthTwentyLeadershipPoints()
    {
        Assert.False(RebellionModel.IsRebellious(1, Rarity.Rare, 1, 5, Rarity.Common));
        Assert.True(RebellionModel.IsRebellious(2, Rarity.Rare, 1, 5, Rarity.Common));
    }

    [Fact] public void TwoRarityGapsNeedTwentyBonusPointsToBeManaged()
    {
        Assert.True(RebellionModel.IsRebellious(1, Rarity.Epic, 1, 5, Rarity.Common, bonus: 19));
        Assert.False(RebellionModel.IsRebellious(1, Rarity.Epic, 1, 5, Rarity.Common, bonus: 20));
    }

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
