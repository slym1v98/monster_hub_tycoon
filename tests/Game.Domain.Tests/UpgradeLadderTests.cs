using System;
using System.Linq;
using Xunit;
using Game.Domain;

public class UpgradeLadderTests
{
    [Fact]
    public void WithoutDropCostIsSumOfCostOverSuccess()
    {
        var l = new UpgradeLadder(new[] { 0.5, 0.25 }, new[] { 10.0, 20.0 }, new[] { 0, 0 });
        Assert.Equal(10 / 0.5 + 20 / 0.25, l.ExpectedCost(), 6);
    }

    [Fact]
    public void SingleStepIsCostOverProbability()
        => Assert.Equal(40.0, new UpgradeLadder(new[] { 0.25 }, new[] { 10.0 }, new[] { 1 }).ExpectedCost(), 6);

    [Fact]
    public void DroppingLevelOnFailureCostsMore()
    {
        var safe = new UpgradeLadder(new[] { 0.9, 0.5, 0.5 }, new[] { 1.0, 1.0, 1.0 }, new[] { 0, 0, 0 });
        var risky = new UpgradeLadder(safe.Success, safe.Cost, new[] { 0, 1, 1 });
        Assert.True(risky.ExpectedCost() > safe.ExpectedCost());
    }

    [Fact]
    public void ExpectedCostFromLaterStepIsSmaller()
    {
        var l = new UpgradeLadder(new[] { 0.7, 0.5, 0.3 }, new[] { 5.0, 5.0, 5.0 }, new[] { 0, 0, 1 });
        Assert.True(l.ExpectedCost(2) < l.ExpectedCost(1));
        Assert.True(l.ExpectedCost(1) < l.ExpectedCost(0));
    }

    [Fact]
    public void RejectsMismatchedOrInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => new UpgradeLadder(new[] { 0.5 }, new[] { 1.0, 2.0 }, new[] { 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeLadder(new[] { 0.0 }, new[] { 1.0 }, new[] { 0 }));
    }
}
