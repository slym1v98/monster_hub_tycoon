using Xunit;
using Game.Domain;

public class FarmAndMarketTests
{
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
