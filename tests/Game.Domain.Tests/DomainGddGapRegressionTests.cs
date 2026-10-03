using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

public sealed class DomainGddGapRegressionTests
{
    [Fact]
    public void TrainerWithStressAtOneHundredWaitsAtHubWhenBarCannotOperate()
    {
        var config = new SimConfig
        {
            TrainerCount = 1,
            StartMinute = SimClock.DawnMinute,
            StartTownHallLevel = 1,
            StartWithNightVision = true,
            ForcedPersonality = Personality.Timid,
            FieldSatietyPerHour = 100,
            FieldStressPerHour = 1000,
            FarmChunkMinutes = 30
        };
        var world = new HubWorld(config, 301, new EmptyExpedition(), null);

        world.RunFor(360);

        Assert.Equal(TrainerState.AtHub, world.Trainers[0].State);
        Assert.Equal("ServiceUnavailable", world.Trainers[0].StateReason);
        Assert.Equal(0, world.Trainers[0].Satiety);
        Assert.Equal(100, world.Trainers[0].Stress);
    }

    [Fact]
    public void TradeTaxAboveThirtyPercentAddsStressProportionalToExcess()
    {
        HubWorld Create(double tax) => new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            TaxRate = tax,
            StartTrainerGold = 1_000,
            BackpackCapacity = 1,
            FarmChunkMinutes = 30,
            ZoneTravelMinutes = 1,
            StartWithNightVision = true,
            FieldStressPerHour = 0,
            HubStaminaPerHour = 0,
            HubSatietyPerHour = 0,
            HubHydrationPerHour = 0,
            ForcedPersonality = Personality.Warlike
        }, 302, new OneOreExpedition(), null);

        var baseline = Create(0.30);
        var taxed = Create(0.40);
        var events = new List<IDomainEvent>();
        taxed.EventRaised += events.Add;
        Assert.True(baseline.SetBuyRequest("ore_tier_1", 10, 100).Ok);
        Assert.True(taxed.SetBuyRequest("ore_tier_1", 10, 100).Ok);
        baseline.RunFor(180);
        taxed.RunFor(180);

        var taxedTrades = events.OfType<MaterialTradeSettled>().Where(trade => trade.Tax > 0).ToArray();
        Assert.NotEmpty(taxedTrades);
        Assert.Equal(taxedTrades.Length, taxed.Trainers[0].Stress - baseline.Trainers[0].Stress, 3);
    }

    [Fact]
    public void WaitingForMerchantAccumulatesConfiguredWaitStress()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            StartTrainerGold = 1_000,
            BackpackCapacity = 1,
            FarmChunkMinutes = 30,
            StartWithNightVision = true,
            FieldStressPerHour = 0,
            WaitStressPerHour = 60,
            HubStaminaPerHour = 0,
            HubSatietyPerHour = 0,
            HubHydrationPerHour = 0,
            ForcedPersonality = Personality.Warlike,
            MerchantSettings = new Game.Domain.Supply.MerchantConfig(100, 0.05, 0.10, 10,
                operatingCostPerTrip: 25, startingCash: 0, routeCycleMinutes: 6000,
                replacementDelayMinutes: 6000, maximumWaitMinutes: 500)
        }, 303, new OneOreExpedition(), null);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        world.RunFor(400);

        Assert.Equal(TrainerState.WaitingForMarket, world.Trainers[0].State);
        Assert.True(world.Trainers[0].Stress >= 50,
            $"Expected market-wait stress, got {world.Trainers[0].Stress}; state={world.Trainers[0].State}/{world.Trainers[0].StateReason}; " +
            string.Join(" | ", events.OfType<TrainerStateChanged>().Select(x => $"{x.Minute}:{x.From}->{x.To}/{x.Reason}")));
    }

    [Fact]
    public void ExpeditionCaptureOpportunityUsesTheHubCaptureAndAdmissionFlow()
    {
        var world = new HubWorld(new SimConfig
        {
            TrainerCount = 1,
            StartTrainerGold = 1_000,
            StartWithNightVision = true,
            FarmChunkMinutes = 30
        }, 304, new CaptureExpedition(), null);
        world.GrantProductForTest(0, "capture_ball", 1);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        world.RunFor(60);

        Assert.Contains(events, x => x is MonsterCaptureResolved capture && capture.TrainerId == 0);
    }

    [Fact]
    public void DefaultExpeditionResolverReturnsTheWildTargetsFromItsBattles()
    {
        var trainer = TestTrainers.Make();
        var zone = ZoneCatalog.Default.Definitions.Single(x => x.Id == "zone_1");
        var resolver = new DefaultExpeditionResolver(new SimConfig());

        var result = resolver.Resolve(TrainerSnapshot.FromTrainer(trainer, SimClock.DawnMinute), zone, 30, new SimRandom(305));
        var otherTrainer = TestTrainers.Make(id: 1);
        var otherResult = resolver.Resolve(TrainerSnapshot.FromTrainer(otherTrainer, SimClock.DawnMinute), zone, 30, new SimRandom(305));

        Assert.Equal(result.Battles.Count, result.CaptureOpportunities.Count);
        Assert.All(result.CaptureOpportunities, target => Assert.StartsWith("zone_1.wild.", target.Id.Value));
        Assert.NotEqual(result.CaptureOpportunities[0].Id, otherResult.CaptureOpportunities[0].Id);
    }

    private sealed class EmptyExpedition : IExpeditionResolver
    {
        public ExpeditionResult Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random)
            => new ExpeditionResult(System.Array.Empty<Game.Domain.Combat.BattleResult>(),
                new ExpeditionLoot(System.Array.Empty<MaterialQuantity>(), System.Array.Empty<MaterialQuantity>(), 0, 0));
    }

    private sealed class OneOreExpedition : IExpeditionResolver
    {
        public ExpeditionResult Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random)
            => new ExpeditionResult(System.Array.Empty<Game.Domain.Combat.BattleResult>(),
                new ExpeditionLoot(new[] { new MaterialQuantity(MaterialId.For(MaterialFamily.Ore, 1), 1) },
                    System.Array.Empty<MaterialQuantity>(), 0, 0));
    }

    private sealed class CaptureExpedition : IExpeditionResolver
    {
        public ExpeditionResult Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random)
        {
            var definition = MonsterCatalog.Default.Definitions[0];
            var target = Monster.Create(new MonsterId("wild.capture.test"), definition, Rarity.Legendary,
                MonsterIvGrade.B, 1, 10);
            target.SetCurrentHp(0);
            return new ExpeditionResult(System.Array.Empty<Game.Domain.Combat.BattleResult>(),
                new ExpeditionLoot(System.Array.Empty<MaterialQuantity>(), System.Array.Empty<MaterialQuantity>(), 0, 0),
                captureOpportunities: new[] { target });
        }
    }
}
