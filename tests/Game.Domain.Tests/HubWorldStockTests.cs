using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Xunit;

public sealed class HubWorldStockTests
{
    [Fact]
    public void HubRecordsFacilityRevenueIposDuringDayTradesAndPaysFifteenDayDividend()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 1, StartMinute = SimClock.DawnMinute, StartBuildingLevel = 5, StartTownHallLevel = 5,
            StartTrainerGold = 100000, StartTreasury = 1000000,
            UnlockedZoneIds = Array.Empty<string>(), ForcedPersonality = Personality.Timid
        }.WithServiceFacilities().WithTierThreeFacilities("stock_exchange"), 20261003);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        world.RunFor(15 * SimClock.MinutesPerDay + 1); // Include the midnight close at the 15-day boundary.
        var ipo = world.IpoBuildingStock(BuildingKind.Restaurant);
        Assert.True(ipo.Ok, $"IPO failed at {world.Now}; restaurant services={events.OfType<ServiceUsed>().Count(e => e.Building == BuildingKind.Restaurant)}");
        var company = Assert.Single(world.StockCompanies);
        Assert.Equal(BuildingKind.Restaurant.ToString(), company.CompanyId);
        long gold = world.Trainers[0].Gold;
        Assert.True(world.BuyIpoShares(0, company.CompanyId, company.AvailableFloatShares).Ok);
        Assert.Single(world.StockHoldingsForTrainer(0));
        Assert.True(world.Trainers[0].Gold < gold);
        Assert.Contains(events, e => e is StockCompanyIpo);
        Assert.Contains(events, e => e is StockTradeSettled trade && trade.TradeType == "IpoPurchase");

        world.RunFor(15 * SimClock.MinutesPerDay);
        // This idle Trainer earns no post-IPO facility revenue, so the 15-day pool is correctly zero.
        Assert.Contains(events, e => e is StockDailyRevenue revenue && revenue.CompanyId == company.CompanyId);
        Assert.Contains(events, e => e is StockPriceChanged price && price.CompanyId == company.CompanyId);
        world.ValidateInvariants();
    }

    [Fact]
    public void StockTradesAreRejectedOutsideDaylightTradingHours()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartMinute = SimClock.DuskMinute }, 91);
        Assert.False(world.BuyIpoShares(0, "missing", 1).Ok);
        Assert.False(world.IpoBuildingStock(BuildingKind.Restaurant).Ok);
        Assert.Empty(world.StockCompanies);
    }

    [Fact]
    public void CommonTrainerBuysAvailablePublicSharesAtDawnAfterListing()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 2, StartMinute = SimClock.DawnMinute, StartBuildingLevel = 5, StartTownHallLevel = 5,
            StartTrainerGold = 100000, StartTreasury = 1000000,
            UnlockedZoneIds = Array.Empty<string>(), ForcedPersonality = Personality.Warlike
        }.WithServiceFacilities().WithTierThreeFacilities("stock_exchange"), 20261004);
        world.RunFor(15 * SimClock.MinutesPerDay + 1);
        Assert.True(world.IpoBuildingStock(BuildingKind.Restaurant).Ok);
        var company = Assert.Single(world.StockCompanies);
        world.RunFor(SimClock.MinutesPerDay);
        Assert.Contains(world.StockHoldingsForTrainer(0), holding => holding.CompanyId == company.CompanyId && holding.Shares > 0);
        world.ValidateInvariants();
    }
}
