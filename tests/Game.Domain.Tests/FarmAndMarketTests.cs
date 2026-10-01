using Xunit;
using Game.Domain;

public class FarmAndMarketTests
{
    [Fact]
    public void FarmResultIsDeterministicForSameSeed()
    {
        var cfg = SimConfig.Default;
        var t = TestTrainers.Make(personality: Personality.Capitalist);
        var a = new SimpleFarmResolver(cfg, new SimRandom(7));
        var b = new SimpleFarmResolver(cfg, new SimRandom(7));
        for (int i = 0; i < 20; i++) Assert.Equal(a.Resolve(t, 30), b.Resolve(t, 30));
    }

    [Fact]
    public void FarmResultIsNonNegative()
    {
        var r = new SimpleFarmResolver(SimConfig.Default, new SimRandom(1));
        foreach (Personality p in new[] { Personality.Warlike, Personality.Timid, Personality.Glutton, Personality.Capitalist })
        {
            var res = r.Resolve(TestTrainers.Make(personality: p), 30);
            Assert.True(res.MaterialUnits >= 0 && res.Gold >= 0 && res.HpLost >= 0);
        }
    }

    [Fact]
    public void WarlikeLosesMoreHpAndTimidLess()
    {
        var cfg = SimConfig.Default;
        long Total(Personality p)
        {
            var resolver = new SimpleFarmResolver(cfg, new SimRandom(3));
            long hp = 0;
            for (int i = 0; i < 200; i++) hp += resolver.Resolve(TestTrainers.Make(personality: p), 30).HpLost;
            return hp;
        }
        Assert.True(Total(Personality.Warlike) > Total(Personality.Timid));
    }

    [Fact]
    public void CapitalistPicksUpMoreMaterialsThanTimid()
    {
        var cfg = SimConfig.Default;
        int Total(Personality p)
        {
            var resolver = new SimpleFarmResolver(cfg, new SimRandom(4));
            int units = 0;
            for (int i = 0; i < 200; i++) units += resolver.Resolve(TestTrainers.Make(personality: p), 30).MaterialUnits;
            return units;
        }
        Assert.True(Total(Personality.Capitalist) > Total(Personality.Timid));
    }

    [Fact]
    public void FixedPriceMarketTaxesTwentyPercentOfGross()
    {
        var market = new FixedPriceMarket(SimConfig.Default);   // 10 Gold/đơn vị, thuế 20%
        var sale = market.Quote(TestTrainers.Make(), 30);
        Assert.Equal(300, sale.GrossToTrainer);
        Assert.Equal(60, sale.Tax);
    }

    [Fact]
    public void SellingNothingYieldsNothing()
    {
        var sale = new FixedPriceMarket(SimConfig.Default).Quote(TestTrainers.Make(), 0);
        Assert.Equal(0, sale.GrossToTrainer);
        Assert.Equal(0, sale.Tax);
    }
}
