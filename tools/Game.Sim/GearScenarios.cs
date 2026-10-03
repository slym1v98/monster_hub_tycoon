using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Game.Domain;
using Game.Domain.Gear;
using Game.Domain.Materials;

/// <summary>Deterministic Gear integration fixture; exercises offer, enhance, star up, refine, wear, repair, buyback.</summary>
public static class GearScenarios
{
    const int Seed = 2026;
    const int Days = 7;
    const string Source = "tools/Game.Sim/GearScenarios.cs: deterministic integration fixture, not campaign balance.";

    public static void Run(TextWriter output)
    {
        var timer = Stopwatch.StartNew();
        var cfg = new SimConfig
        {
            TrainerCount = 1,
            StartMinute = SimClock.DawnMinute,
            StartTreasury = 50000,
            StartTrainerGold = 50000,
            FarmChunkMinutes = 30,
            ZoneTravelMinutes = 10,
        };
        var prices = ConsumablePriceConfig.Prototype.Prices.ToDictionary(x => x.Key, x => x.Value);
        prices[new ProductId("enhancement_stone")] = 20;
        prices[new ProductId("distilled_water")] = 15;
        prices[new ProductId("world_boss_crystal")] = 500;
        prices[new ProductId("protection_charm")] = 500;
        cfg.ConsumablePrices = new ConsumablePriceConfig(prices);
        var world = new HubWorld(cfg, Seed);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        var cat = GearCatalog.Default;
        var slotGloves = cat.GetSlot("trainer.gloves");
        var slotBoots = cat.GetSlot("trainer.boots");
        var gloves = new GearItem("gear_test_gloves", slotGloves, 2, cat.MaxDurabilityFor(slotGloves.Group));
        var replacementGloves = new GearItem("gear_test_gloves_better", slotGloves, 3, cat.MaxDurabilityFor(slotGloves.Group));
        var boots = new GearItem("gear_test_boots", slotBoots, 2, cat.MaxDurabilityFor(slotBoots.Group));
        var junk = new GearItem("gear_test_junk", slotBoots, 1, cat.MaxDurabilityFor(slotBoots.Group));

        output.WriteLine($"# gear: seed {Seed}; {cfg.TrainerCount} Trainer; {Days} days; HubWorld");
        foreach (var p in Parameters(cfg, cat).OrderBy(p => p.Id, StringComparer.Ordinal))
            output.WriteLine(FormattableString.Invariant($"Balance: {p.Id}={p.Value} {p.Unit} [{p.Status}; source: {p.Source}]"));

        Require(world.OfferGear(0, gloves, 100));
        world.GrantProductForTest(0, "enhancement_stone", 10);
        world.GrantProductForTest(0, "distilled_water", 10);
        world.GrantProductForTest(0, "world_boss_crystal", 5);
        world.GrantProductForTest(0, "protection_charm", 2);

        for (int i = 0; i < 5; i++) Require(world.EnhanceGear(0, gloves, false));
        Require(world.OfferGear(0, boots, 100));
        Require(world.OfferGearForSacrifice(0, junk, 50));
        Require(world.StarUpGear(0, boots, junk));
        Require(world.RefineGear(0, boots));
        Require(world.OfferGear(0, replacementGloves, 100));

        int beforeDurability = replacementGloves.Durability;
        for (int day = 1; day <= Days; day++)
            world.RunFor(Math.Min(cfg.FarmChunkMinutes + cfg.ZoneTravelMinutes * 2, SimClock.MinutesPerDay));

        int afterWear = replacementGloves.Durability;
        Require(world.RepairGear(0, replacementGloves));
        Require(world.BuybackGear(0, gloves, 100));

        int remaining = cfg.StartMinute + Days * SimClock.MinutesPerDay - world.Now.TotalMinutes;
        if (remaining > 0) world.RunFor(remaining);
        world.ValidateInvariants();

        long goldSpent = events.OfType<GearOffered>().Where(e => e.Accepted).Sum(e => e.Price)
            + events.OfType<GearFodderPurchased>().Where(e => e.Success).Sum(e => e.Price)
            + events.OfType<GearEnhanced>().Sum(e => e.GoldSpent)
            + events.OfType<GearStarUp>().Sum(e => e.GoldSpent)
            + events.OfType<GearRefined>().Sum(e => e.GoldSpent)
            + events.OfType<GearRepaired>().Where(e => e.Success).Sum(e => e.GoldSpent)
            - events.OfType<GearBuyback>().Where(e => e.Success).Sum(e => e.BuybackPrice);
        long trainerGold = world.Trainers.Sum(t => t.Gold);
        long servicePaid = events.OfType<ServiceUsed>().Sum(e => e.Paid);

        long trainerGearFlow = -goldSpent;
        long treasuryGearFlow = goldSpent;
        if (trainerGearFlow + treasuryGearFlow != 0) throw new InvalidOperationException("Gear Gold ledger did not reconcile.");
        output.WriteLine($"Gear flow: offers {events.OfType<GearOffered>().Count()}; fodder purchases {events.OfType<GearFodderPurchased>().Count()}; enhances {events.OfType<GearEnhanced>().Count()}; " +
            $"star-ups {events.OfType<GearStarUp>().Count()}; refines {events.OfType<GearRefined>().Count()}; " +
            $"repairs {events.OfType<GearRepaired>().Count()}; buybacks {events.OfType<GearBuyback>().Count()}");
        output.WriteLine($"Durability: gloves {beforeDurability}->{afterWear}->{gloves.Durability}");
        output.WriteLine($"Gold: trainerGoldNet={trainerGold - cfg.StartTrainerGold + servicePaid} (servicePaid={servicePaid}) gearGoldSpent={goldSpent}");
        output.WriteLine($"Gear ledger reconcile: trainerSide={trainerGearFlow} treasurySide={treasuryGearFlow} difference={trainerGearFlow + treasuryGearFlow}");
        output.WriteLine($"Gear inventory: equipped={world.GearForTrainer(0).Count} stored={world.GearStorageForTrainer(0).Count} trainerGold={trainerGold} treasury={world.Treasury}");
        timer.Stop();
        output.WriteLine($"Runtime: {timer.ElapsedMilliseconds} ms");
    }

    static IEnumerable<BalanceParameter> Parameters(SimConfig cfg, GearCatalog cat)
    {
        foreach (var p in new[] {
            P("scenario.seed", Seed, "seed"), P("scenario.days", Days, "days"),
            P("scenario.trainers", cfg.TrainerCount, "Trainers"),
            P("scenario.start_treasury", cfg.StartTreasury, "Gold"),
            P("scenario.start_trainer_gold", cfg.StartTrainerGold, "Gold"),
            P("scenario.farm_chunk", cfg.FarmChunkMinutes, "minutes"),
            P("scenario.zone_travel", cfg.ZoneTravelMinutes, "minutes") })
            yield return p;
        foreach (var p in cfg.ConsumablePrices.BalanceParameters.Concat(cat.BalanceParameters)
            .Concat(new EnhancementModel().BalanceParameters)) yield return p;
    }
    static BalanceParameter P(string id, double value, string unit) => new BalanceParameter(id, value, unit, "Prototype", Source);
    static void Require(CommandResult result) { if (!result.Ok) throw new InvalidOperationException(result.Reason); }
}
