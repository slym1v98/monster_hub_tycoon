using Xunit;
using Game.Domain;

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
