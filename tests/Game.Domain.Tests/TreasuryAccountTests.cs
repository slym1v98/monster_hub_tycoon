using System;
using Xunit;
using Game.Domain;

public class TreasuryAccountTests
{
    [Fact]
    public void SpendFailsWithoutChangingBalanceWhenInsufficient()
    {
        var t = new TreasuryAccount(100);
        Assert.False(t.TrySpend(101));
        Assert.Equal(100, t.Balance);
        Assert.True(t.TrySpend(100));
        Assert.Equal(0, t.Balance);
    }

    [Fact]
    public void AddIncreasesBalance()
    {
        var t = new TreasuryAccount(0);
        t.Add(50);
        Assert.Equal(50, t.Balance);
    }

    [Fact]
    public void AddRejectsOverflowWithoutChangingBalance()
    {
        var t = new TreasuryAccount(long.MaxValue);
        Assert.Throws<OverflowException>(() => t.Add(1));
        Assert.Equal(long.MaxValue, t.Balance);
    }

    [Fact]
    public void NegativeArgumentsAreProgrammingErrors()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreasuryAccount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreasuryAccount(0).Add(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreasuryAccount(0).TrySpend(-1));
    }
}
