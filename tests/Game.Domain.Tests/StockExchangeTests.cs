using System.Linq;
using Game.Domain;
using Xunit;

public sealed class StockExchangeTests
{
    static StockExchange Create() => new StockExchange(2026, new StockExchangeConfig(
        dailyVolatility: 0.03, trafficSensitivity: 0.5, orderImpactCoefficient: 0.05));

    static StockCompanyView WarmCompany(StockExchange exchange, int days = 15)
    {
        for (int day = 1; day <= days; day++)
        {
            exchange.RecordRevenue("Restaurant", 1000);
            exchange.CloseDay(day);
        }
        Assert.False(exchange.TryIpo("Restaurant", 4, out _));
        Assert.True(exchange.TryIpo("Restaurant", 5, out var view));
        return view;
    }

    [Fact]
    public void IpoUsesFifteenDayRevenueAndLocksFiftyOnePercentWithRemainingFloat()
    {
        var exchange = Create();
        var listing = WarmCompany(exchange);
        Assert.Equal(4500, listing.TotalShares);
        Assert.Equal(2295, listing.HubLockedShares);
        Assert.Equal(2205, listing.AvailableFloatShares);
        Assert.Equal(0, listing.DirectorFloatShares);
        Assert.Equal(100, listing.Price);
        Assert.Equal(15000, listing.Revenue15Days);
    }

    [Fact]
    public void PrimaryAndSecondaryTradesTrackFloatHoldingsFeesAndRealizedProfitTax()
    {
        var exchange = Create();
        WarmCompany(exchange);
        var ipo = exchange.QuoteIpoPurchase("Restaurant", 100);
        Assert.Equal(10000, ipo.Gross);
        Assert.Equal(100, ipo.Fee);
        Assert.Equal(10100, ipo.BuyerTotal);
        Assert.True(exchange.BuyIpoShares(0, "Restaurant", 100, out _));
        Assert.Equal(2105, exchange.GetCompany("Restaurant").AvailableFloatShares);
        Assert.Equal(101, exchange.HoldingsFor(0).Single().AverageCost);

        exchange.RecordRevenue("Restaurant", 10000);
        var close = exchange.CloseDay(16).Single();
        Assert.True(close.NewPrice > close.OldPrice);
        var quote = exchange.QuoteTransfer("Restaurant", 0, 1, 50);
        Assert.True(quote.RealizedProfit > 0);
        Assert.Equal((long)System.Math.Ceiling(quote.RealizedProfit * 0.30), quote.Tax);
        Assert.True(exchange.TransferShares("Restaurant", 0, 1, 50, out _));
        Assert.Equal(50, exchange.HoldingsFor(0).Single().Shares);
        Assert.Equal(50, exchange.HoldingsFor(1).Single().Shares);
        Assert.Equal(4500, exchange.GetCompany("Restaurant").TotalShares);
    }

    [Fact]
    public void FifteenDayDividendsUseRevenueWindowAndPublicOwnershipOnly()
    {
        var exchange = Create();
        WarmCompany(exchange);
        Assert.True(exchange.BuyIpoShares(0, "Restaurant", 100, out _));
        StockDailyClose close = null;
        for (int day = 16; day <= 30; day++)
        {
            exchange.RecordRevenue("Restaurant", 1000);
            close = exchange.CloseDay(day).Single();
        }
        Assert.Equal(15000, close.Revenue15Days);
        Assert.Equal(1500, close.DividendPool);
        var dividends = exchange.Dividends(close);
        Assert.Single(dividends);
        Assert.Equal(100, dividends[0].Shares);
        Assert.Equal(33, dividends[0].Gross);
    }

    [Fact]
    public void ExchangeLevelLimitsActiveListingsAndSeedDeterminesPricePath()
    {
        var a = Create(); var b = Create();
        WarmCompany(a); WarmCompany(b);
        for (int day = 16; day <= 20; day++)
        {
            a.RecordRevenue("Restaurant", day * 10); b.RecordRevenue("Restaurant", day * 10);
            Assert.Equal(a.CloseDay(day).Single().NewPrice, b.CloseDay(day).Single().NewPrice);
        }
        for (int day = 1; day <= 15; day++)
        {
            a.RecordRevenue("Inn", 500); b.RecordRevenue("Inn", 500);
            a.CloseDay(day); b.CloseDay(day);
        }
        Assert.False(a.TryIpo("Inn", 5, out _));
        Assert.True(a.SetExchangeLevel(2));
        Assert.True(a.TryIpo("Inn", 5, out _));
    }

    [Fact]
    public void NetSellOrdersMovePriceDownAndShareOwnershipAlwaysReconciles()
    {
        var exchange = new StockExchange(1, new StockExchangeConfig(dailyVolatility: 0,
            trafficSensitivity: 0, orderImpactCoefficient: 0.5));
        WarmCompany(exchange);
        Assert.True(exchange.BuyIpoShares(0, "Restaurant", 100, out _));
        exchange.RecordRevenue("Restaurant", 1000);
        exchange.CloseDay(16); // Clear the initial IPO buy order from the next day's net flow.
        Assert.True(exchange.TransferShares("Restaurant", 0, 1, 100, out _));
        exchange.RecordRevenue("Restaurant", 1000);
        var close = exchange.CloseDay(17).Single();
        Assert.True(close.NetOrderFraction < 0);
        Assert.True(close.NewPrice < close.OldPrice);
        exchange.ValidateInvariants();
    }

    [Fact]
    public void DirectorTradesOnlyPublicFloatAndPreservesIssuedShareCount()
    {
        var exchange = Create();
        WarmCompany(exchange);
        Assert.True(exchange.BuyIpoShares(0, "Restaurant", 100, out _));
        Assert.True(exchange.DirectorBuysFloat("Restaurant", 0, 40, out var buy));
        Assert.True(buy.Gross > 0);
        Assert.Equal(40, exchange.GetCompany("Restaurant").DirectorFloatShares);
        Assert.True(exchange.DirectorSellsFloat("Restaurant", 1, 10, out var sell));
        Assert.True(sell.BuyerTotal > sell.Gross);
        var company = exchange.GetCompany("Restaurant");
        Assert.Equal(30, company.DirectorFloatShares);
        Assert.Equal(4500, company.TotalShares);
        exchange.ValidateInvariants();
    }
}
