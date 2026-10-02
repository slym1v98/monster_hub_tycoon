using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Game.Domain.Materials;
using Xunit;

public class HubWorldSupplyTests
{
    [Fact]
    public void TrainerSaleUsesStationRequestTaxAndSharedTreasury()
    {
        var config = new SimConfig { TrainerCount = 1, StartTreasury = 1000, StartTrainerGold = 1000,
            BackpackCapacity = 1, FarmChunkMinutes = 30, ZoneTravelMinutes = 30 };
        var world = new HubWorld(config, 1, new TypedFarm(1), null);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        Assert.True(world.SetBuyRequest("ore_tier_1", 1, 10).Ok);
        world.RunFor(100);

        Assert.Equal(992, world.Treasury); // Gold 10 trả ra, 2 thuế quay về HUB.
        Assert.Equal(1008, world.Trainers[0].Gold);
        Assert.Contains(events.OfType<MaterialTradeSettled>(), x => x.Channel == "Station" &&
            x.Units == 1 && x.Gross == 10 && x.Tax == 2 && x.NetToSeller == 8);
        Assert.Contains(world.SupplyStocks, x => x.ItemId == "material:ore_tier_1" && x.Available == 1);
        world.ValidateInvariants();
    }

    [Fact]
    public void ProductionStartsWhenStationStockArrivesAndCompletesOnScheduledMinute()
    {
        var config = new SimConfig { TrainerCount = 1, StartTreasury = 1000, StartTrainerGold = 1000,
            BackpackCapacity = 2, FarmChunkMinutes = 30, ZoneTravelMinutes = 30,
            ProductionSettings = new Game.Domain.Production.ProductionConfig(defaultOperatingCost: 3) };
        var world = new HubWorld(config, 2, new TypedFarm(2), null);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        Assert.True(world.SetProductionTarget("blank_ore_tier_1", 1).Ok);
        Assert.True(world.SetBuyRequest("ore_tier_1", 2, 10).Ok);
        world.RunFor(210);

        Assert.Contains(world.SupplyStocks, x => x.ItemId == "product:blank_ore_tier_1" && x.Available == 1);
        Assert.Contains(world.ProductionJobs, x => x.State == "Completed" && x.FinishMinute <= world.Now.TotalMinutes);
        Assert.Contains(events.OfType<ProductionRestockDemandChanged>(), x => x.Quantity > 0);
        Assert.Contains(events.OfType<ProductionJobChanged>(), x => x.State == "Completed" && x.Minute == x.FinishMinute);
        Assert.Contains(events.OfType<TreasuryChanged>(), x => x.Reason == "ProductionCost" && x.Delta == -3);
        world.ValidateInvariants();
    }

    [Fact]
    public void MerchantFallbackPreservesGoodsUntilItsDeterministicReturnVisit()
    {
        var config = new SimConfig { TrainerCount = 1, StartTrainerGold = 1000,
            BackpackCapacity = 1, FarmChunkMinutes = 30, ZoneTravelMinutes = 30 };
        var world = new HubWorld(config, 4, new TypedFarm(1), null);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        world.RunFor(270);

        Assert.Contains(events.OfType<MaterialTradeSettled>(), x => x.Channel == "Merchant" && x.Units == 1);
        Assert.DoesNotContain(world.Trainers, x => x.State == TrainerState.WaitingForMarket);
        Assert.Equal(0, world.Trainers[0].BackpackUnits);
        Assert.True(world.Trainers[0].Gold > 1000);
        world.ValidateInvariants();
    }

    [Fact]
    public void WaitingTrainerGetsReplacementMerchantAtTheWaitDeadline()
    {
        var config = new SimConfig { TrainerCount = 1, StartTrainerGold = 1000,
            BackpackCapacity = 1, FarmChunkMinutes = 5, ZoneTravelMinutes = 5,
            MerchantSettings = new Game.Domain.Supply.MerchantConfig(100, 0.05, 0.10, 10,
                operatingCostPerTrip: 25, startingCash: 30, routeCycleMinutes: 60,
                replacementDelayMinutes: 60, maximumWaitMinutes: 90) };
        var world = new HubWorld(config, 5, new TypedFarm(1), null);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        world.RunFor(500);

        Assert.Contains(events.OfType<MaterialTradeSettled>(), x => x.Channel == "Merchant" && x.Units > 0);
        Assert.DoesNotContain(events.OfType<TrainerStateChanged>(), x => x.Reason == "MarketWaitLimit");
        Assert.Contains(events.OfType<MerchantStateChanged>().Where(x => x.MerchantId != "route_merchant_1" &&
            x.State == Game.Domain.Supply.MerchantState.AtTrainerRoute), replacement =>
            events.OfType<TrainerStateChanged>().Any(x => x.Minute == replacement.Minute &&
                x.From == TrainerState.WaitingForMarket && x.Reason == "MarketSettled"));
        Assert.DoesNotContain(world.Trainers, x => x.State == TrainerState.WaitingForMarket);
        world.ValidateInvariants();
    }

    [Fact]
    public void RetryRemainsScheduledWhenReplacementCannotAffordWaitingBackpack()
    {
        var config = new SimConfig { TrainerCount = 1, StartTrainerGold = 1000,
            BackpackCapacity = 1, FarmChunkMinutes = 5, ZoneTravelMinutes = 5,
            MerchantSettings = new Game.Domain.Supply.MerchantConfig(100, 0.05, 0.10, 10,
                operatingCostPerTrip: 25, startingCash: 8, routeCycleMinutes: 60,
                replacementDelayMinutes: 60, maximumWaitMinutes: 1) };
        var world = new HubWorld(config, 5, new TypedFarm(1), null);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        world.RunFor(1000);

        Assert.Contains(events.OfType<TrainerStateChanged>(), x => x.Reason == "MarketWaitLimit");
        Assert.DoesNotContain(world.Trainers, x => x.State == TrainerState.WaitingForMarket);
        var waitEntries = events.OfType<TrainerStateChanged>()
            .Where(x => x.To == TrainerState.WaitingForMarket).ToArray();
        foreach (var exit in events.OfType<TrainerStateChanged>().Where(x => x.Reason == "MarketWaitLimit"))
        {
            var entry = waitEntries.Last(x => x.Minute <= exit.Minute);
            Assert.Equal(1, exit.Minute - entry.Minute);
        }
        world.ValidateInvariants();
    }

    [Fact]
    public void SupplyChainEventSequenceIsDeterministicAndTrainerEventsStayUnique()
    {
        List<string> Run(int seed)
        {
            var config = new SimConfig { TrainerCount = 1, BackpackCapacity = 2,
                FarmChunkMinutes = 30, ZoneTravelMinutes = 30 };
            var world = new HubWorld(config, seed, new TypedFarm(2), null);
            var events = new List<string>();
            world.EventRaised += x => events.Add(x.ToString());
            world.SetProductionTarget("blank_ore_tier_1", 1);
            world.SetBuyRequest("ore_tier_1", 2, 10);
            world.RunFor(210);
            world.ValidateInvariants();
            return events;
        }

        Assert.Equal(Run(12), Run(12));
    }

    [Fact]
    public void MarketCommandsRejectUnknownIdsAndInvalidConfiguration()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1 }, 3);

        Assert.False(world.SetBuyRequest("ore_tier_6", 10, 1).Ok);
        Assert.False(world.SetBuyRequest("ore_tier_1", -1, 1).Ok);
        Assert.False(world.SetBuyRequest("ore_tier_1", 10, -1).Ok);
        Assert.False(world.SetProductionTarget("unknown_product", 1).Ok);
        Assert.False(world.SetMarketTaxRate(double.NaN).Ok);
        Assert.False(world.SetMaterialReferencePrice(-1).Ok);
        Assert.True(world.SetMarketTaxRate(0.30).Ok);
        Assert.True(world.SetMaterialReferencePrice(12).Ok);
    }

    private sealed class TypedFarm : IFarmResolver
    {
        private readonly int units;
        public TypedFarm(int units) => this.units = units;
        public FarmResult Resolve(Trainer trainer, int minutes)
            => new FarmResult(units, 0, 0, MaterialId.For(MaterialFamily.Ore, 1));
    }
}
