using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Domain;
using Game.Domain.Combat;
using Game.Domain.Materials;
using Game.Domain.Monsters;

/// <summary>Repeatable integration fixtures; every command and encounter runs through HubWorld.</summary>
public static class MonsterScenarios
{
    const int Seed = 2026;
    const int Days = 30;
    const int FixtureLevel = 40;
    const int ProductionTarget = 20;
    const int CaptureAttempts = 20;
    const string Source = "tools/Game.Sim/MonsterScenarios.cs: deterministic integration fixture, not campaign balance.";

    public static void Run(string mode, TextWriter output)
    {
        if (mode != "monster" && mode != "expedition") throw new ArgumentException("Unknown Monster scenario.", nameof(mode));
        if (output == null) throw new ArgumentNullException(nameof(output));
        var timer = Stopwatch.StartNew();
        var cfg = new SimConfig { TrainerCount = mode == "monster" ? 2 : 6, StartMinute = 100, StartTownHallLevel = 4,
            StartTreasury = 1000000, StartTrainerGold = 10000,
            StartingFacilityLevels = new Dictionary<string, int> { ["inn"] = 1, ["restaurant"] = 1,
                ["bar"] = 1, ["veterinary_hospital"] = 1 },
            HubProgressionSettings = new HubProgressionConfig(facilityUpgradeMinutes: 1),
            StartingConstructionStock = new Dictionary<ProductId, int> { [new ProductId("wood_ingot")] = 20,
                [new ProductId("stone_ingot")] = 20, [new ProductId("iron_ingot")] = 20 },
            ExpeditionSettings = new ExpeditionConfig(opponentAttack: 60),
            RarityUpgradeSettings = new UpgradeConfig(new[] { 1d, 1d, 1d, 1d }, new[] { 0, 0, 0, 0 }, new[] { 1, 1, 1, 1 }) };
        var prices = ConsumablePriceConfig.Prototype.Prices.ToDictionary(x => x.Key, x => x.Value);
        prices[new ProductId("gene_fragment")] = cfg.MaterialPrice;
        cfg.ConsumablePrices = new ConsumablePriceConfig(prices);
        var species = MonsterCatalog.Default.Definitions[0];
        var evolved = new MonsterDefinition("scenario_water_guard", "Water Guard", MonsterElement.Water, MonsterRole.Tank,
            new MonsterStats(400, 12, 12, 1, 0.05), new MonsterStats(0, 0, 0, 0, 0));
        cfg.EvolutionCatalogSettings = new EvolutionCatalog(new[] { new EvolutionDefinition(species.Id, "water_guard", evolved,
            FixtureLevel, 1, 2, new ProductId("gene_fragment"), 1, new[] { "water_strike" }) });
        var world = new HubWorld(cfg, Seed); // Same default resolver and RNG as the game.
        Require(world.ConstructFacility("refinery"));
        Require(world.ConstructFacility("tool_workshop"));
        world.RunFor(1);
        var metrics = new Metrics(world);
        output.WriteLine($"# {mode}: seed {Seed}; {cfg.TrainerCount} Trainers; {Days} days; DefaultExpeditionResolver via HubWorld");
        foreach (var p in Parameters(cfg, mode).OrderBy(p => p.Id, StringComparer.Ordinal))
            output.WriteLine(FormattableString.Invariant($"Balance: {p.Id}={p.Value} {p.Unit} [{p.Status}; source: {p.Source}]") );
        foreach (var material in MaterialCatalog.Default.Materials)
            Require(world.SetBuyRequest(material.Id.Value, cfg.BackpackCapacity * cfg.TrainerCount * Days, cfg.MaterialPrice));
        foreach (var recipe in new[] { "blank_ore_tier_1", "potion", "capture_ball", "tactics_book" })
            Require(world.SetProductionTarget(recipe, ProductionTarget));

        RequireAdmission(world.AdmitCapturedMonster(0, Monster.Create(new MonsterId("scenario_reserve"), species,
            Rarity.Common, MonsterIvGrade.B, FixtureLevel, Seed)));
        RequireAdmission(world.AdmitCapturedMonster(0, Monster.Create(new MonsterId("scenario_salvage"), species,
            Rarity.Common, MonsterIvGrade.D, 1, Seed + 1)));
        Advance(world, cfg.VeterinaryHospitalSettings.FirstCaptureRecoveryMinutes);
        Require(world.SwapActiveMonster(0, "scenario_reserve"));
        Require(world.StoreMonster(0, "scenario_salvage"));
        Require(world.WithdrawMonster(0, "scenario_salvage"));
        Require(world.AppraiseMonster(0, "scenario_reserve"));
        Require(world.DismantleMonster(0, "scenario_salvage"));
        Require(world.UpgradeMonsterRarity(0, "scenario_reserve"));
        Require(world.EvolveMonster(0, "scenario_reserve", "water_guard"));
        Require(world.SetProductBuyRequest("gene_fragment", 1, cfg.MaterialPrice));
        Require(world.SellProductToStation(0, "gene_fragment", 1));
        Require(world.PurchaseProduct(0, "gene_fragment", 1));

        var target = Monster.Create(new MonsterId("scenario_wild_capture"), species, Rarity.Epic,
            MonsterIvGrade.B, FixtureLevel, Seed + 2);
        target.SetCurrentHp(1); // Explicit weakened wild fixture; the Hub owns rolls, costs and recovery.
        bool caught = false, emergency = false;
        for (int day = 1; day < Days; day++)
        {
            if (!caught)
            {
                var trainer = world.Trainers[0];
                int held = trainer.Products.TryGetValue(new ProductId("capture_ball"), out int balls) ? balls : 0;
                int available = world.SupplyStocks.FirstOrDefault(s => s.ItemId == "product:capture_ball")?.Available ?? 0;
                int buy = Math.Min(CaptureAttempts - held, available);
                if (buy > 0) Require(world.PurchaseProduct(0, "capture_ball", buy));
                for (int attempt = 0; attempt < CaptureAttempts && !caught &&
                    world.Trainers[0].Products.ContainsKey(new ProductId("capture_ball")); attempt++)
                {
                    Require(world.AttemptCapture(0, target));
                    caught = metrics.Captures > 0;
                }
            }
            int dayMinutes = SimClock.MinutesPerDay;
            while (dayMinutes > 0)
            {
                int step = emergency ? dayMinutes : Math.Min(dayMinutes, cfg.FarmChunkMinutes);
                Advance(world, step);
                dayMinutes -= step;
                if (!emergency)
                {
                    var wounded = world.Trainers.SelectMany(t => t.Monsters).FirstOrDefault(m =>
                        m.Custody == MonsterCustody.Trainer && m.CurrentHp < m.MaxHp);
                    if (wounded != null) emergency = world.RequestMonsterEmergencyCare(new MonsterId(wounded.Id)).Accepted;
                }
            }
        }
        // Exact total horizon includes the initial recovery day; cross Payday and resume its remaining minutes.
        Advance(world, cfg.StartMinute + Days * SimClock.MinutesPerDay - world.Now.TotalMinutes);
        world.ValidateInvariants();
        if (!caught || !emergency) throw new InvalidOperationException($"Scenario missing coverage: capture={caught}; emergency={emergency}; jobs={world.ProductionJobs.Count}; balls={world.SupplyStocks.FirstOrDefault(s => s.ItemId == "product:capture_ball")?.Available}; ore={world.SupplyStocks.FirstOrDefault(s => s.ItemId == "material:ore_tier_1")?.Available}; wood={world.SupplyStocks.FirstOrDefault(s => s.ItemId == "material:wood_tier_1")?.Available}; blank={world.SupplyStocks.FirstOrDefault(s => s.ItemId == "product:blank_ore_tier_1")?.Available}.");
        metrics.Print(world, output);
        timer.Stop();
        output.WriteLine($"Runtime: {timer.ElapsedMilliseconds} ms");
    }

    static IEnumerable<BalanceParameter> Parameters(SimConfig cfg, string mode)
    {
        foreach (var p in new[] {
            P("scenario.seed", Seed, "seed"), P("scenario.days", Days, "days"), P("scenario.trainers", cfg.TrainerCount, "Trainers"),
            P("scenario.start_treasury", cfg.StartTreasury, "Gold"), P("scenario.start_trainer_gold", cfg.StartTrainerGold, "Gold"),
            P("scenario.start_minute", cfg.StartMinute, "minutes"), P("scenario.fixture_level", FixtureLevel, "level"),
            P("scenario.production_target", ProductionTarget, "units"), P("scenario.capture_attempt_limit", CaptureAttempts, "attempts"),
            P("scenario.wild_hp", 1, "HP"), P("scenario.salvage_level", 1, "level"),
            P("scenario.evolved_hp", 400, "HP"), P("scenario.evolved_attack", 12, "ATK"), P("scenario.evolved_defense", 12, "DEF"),
            P("scenario.evolved_speed", 1, "ASPD"), P("scenario.evolved_critical", 0.05, "probability"),
            P("scenario.gene_fragment_price", cfg.MaterialPrice, "Gold/unit") }) yield return p;
        if (mode == "expedition")
        {
            yield return P("scenario.survey_minimum_rank", 1, "rank");
            yield return P("scenario.unlock_interval", 6, "days");
            yield return P("scenario.survey_gold_quadratic_factor", 100, "Gold/tier squared");
        }
        foreach (var p in cfg.ZoneCatalogSettings.Definitions.SelectMany(z => z.BalanceParameters)
            .Concat(cfg.ExpeditionSettings.BalanceParameters).Concat(cfg.LootSettings.BalanceParameters)
            .Concat(cfg.GeneticLabSettings.BalanceParameters).Concat(cfg.RarityUpgradeSettings.BalanceParameters)
            .Concat(cfg.EvolutionCatalogSettings.BalanceParameters).Concat(cfg.VeterinaryHospitalSettings.BalanceParameters)
            .Concat(cfg.GeneBankSettings.BalanceParameters).Concat(cfg.MonsterStorageSettings.BalanceParameters)
            .Concat(cfg.TrainerLoanSettings.BalanceParameters)
            .Concat(cfg.ReverseLoanSettings.BalanceParameters)
            .Concat(cfg.StockExchangeSettings.BalanceParameters)
            .Concat(cfg.GeneBankSettings.BalanceParameters)
            .Concat(cfg.MonsterItemSettings.BalanceParameters).Concat(CaptureConfig.Prototype.BalanceParameters)
            .Concat(cfg.ConsumablePrices.BalanceParameters)) yield return p;
    }
    static BalanceParameter P(string id, double value, string unit) => new BalanceParameter(id, value, unit, "Prototype", Source);
    static void Require(CommandResult result) { if (!result.Ok) throw new InvalidOperationException(result.Reason); }
    static void RequireAdmission(AdmissionResult result) { if (!result.Accepted) throw new InvalidOperationException(result.Status.ToString()); }
    static void Advance(HubWorld world, int minutes)
    {
        int left = minutes;
        while (left > 0)
        {
            var result = world.RunFor(left);
            left = result.RemainingMinutes;
            if (result.Stop == StopReason.PaydayDue) world.ResolvePayday();
            world.ValidateInvariants();
        }
    }

    sealed class Metrics
    {
        readonly List<IDomainEvent> events = new List<IDomainEvent>();
        readonly Dictionary<string, (int trainer, MonsterCustody custody)> owners = new Dictionary<string, (int, MonsterCustody)>(StringComparer.Ordinal);
        readonly Dictionary<(int trainer, string product), long> products = new Dictionary<(int, string), long>();
        readonly long initialTrainerGold, initialTreasury;
        int productEventErrors;
        public int Captures => events.OfType<MonsterCaptureResolved>().Count(e => e.Success);
        public Metrics(HubWorld world)
        {
            initialTrainerGold = world.Trainers.Sum(t => t.Gold); initialTreasury = world.Treasury;
            foreach (var trainer in world.Trainers)
            {
                foreach (var m in world.MonstersForTrainer(trainer.Id)) owners.Add(m.Id, (trainer.Id, m.Custody));
                foreach (var p in trainer.Products) products.Add((trainer.Id, p.Key.Value), p.Value);
            }
            world.EventRaised += e =>
            {
                events.Add(e);
                if (e is MonsterRecoveryStarted r) owners[r.MonsterId] = (r.TrainerId, MonsterCustody.Hospital);
                if (e is MonsterRecoveryCompleted done) owners[done.MonsterId] = (done.TrainerId, MonsterCustody.Trainer);
                if (e is GeneBankOwnershipChanged bank) owners[bank.MonsterId] = (bank.TrainerId, bank.To);
                if (e is MonsterDismantled dismantle) owners.Remove(dismantle.MonsterId);
                if (e is TrainerProductChanged change)
                {
                    var key = (change.TrainerId, change.ProductId);
                    products.TryGetValue(key, out long prior);
                    products[key] = prior + change.Quantity;
                    if (products[key] != change.NewCount || products[key] < 0) productEventErrors++;
                }
            };
        }
        public void Print(HubWorld world, TextWriter output)
        {
            var expeditions = events.OfType<ExpeditionCompleted>().ToArray();
            foreach (var zone in world.Zones)
            {
                var battles = expeditions.Where(e => e.ZoneId == zone.Id).SelectMany(e => e.Battles).ToArray();
                output.WriteLine($"{zone.Id}: encounters {battles.Length}; wins {battles.Count(b => b.Outcome == BattleOutcome.TeamWon)}; " +
                    $"losses {battles.Count(b => b.Outcome == BattleOutcome.OpponentsWon)}; unresolved {battles.Count(b => b.Outcome != BattleOutcome.TeamWon && b.Outcome != BattleOutcome.OpponentsWon)}; " +
                    $"swaps {battles.SelectMany(b => b.Actions).Count(a => a.Swap)}; faints {battles.SelectMany(b => b.Actions).Count(a => a.Fainted && a.TargetId.HasValue && bTeamId(a.TargetId.Value))}; unlocked {zone.IsUnlocked}");
            }
            bool bTeamId(MonsterId id) => !id.Value.Contains(".wild.", StringComparison.Ordinal);
            output.WriteLine($"Lifecycle: roster changes {events.OfType<MonsterRosterChanged>().Count()}; recovery started {events.OfType<MonsterRecoveryStarted>().Count()}; recovery completed {events.OfType<MonsterRecoveryCompleted>().Count()}; capture attempts {events.OfType<MonsterCaptureResolved>().Count()}; captures {Captures}; appraisal {events.OfType<MonsterAppraised>().Count()}; evolution attempts {events.OfType<MonsterEvolutionResolved>().Count()}; upgrades {events.OfType<MonsterUpgradeResolved>().Count()}; dismantled {events.OfType<MonsterDismantled>().Count()}");
            output.WriteLine($"Progression: XP {expeditions.Sum(e => e.ExperienceGained)}; " + string.Join("; ", world.Trainers.Select(t => $"Trainer {t.Id} rank {t.Rank} level {t.Level}")));
            output.WriteLine($"Item flow: collected {expeditions.Sum(e => e.Collected.Sum(m => (long)m.Quantity))}; dropped {expeditions.Sum(e => e.Dropped.Sum(m => (long)m.Quantity))}; " +
                $"product changes {events.OfType<TrainerProductChanged>().Count()}; purchases {events.OfType<ProductPurchased>().Sum(e => e.Units)}; jobs completed {events.OfType<ProductionJobChanged>().Count(e => e.State == "Completed")}; backpack {world.Trainers.Sum(t => t.BackpackUnits)}");
            long expectedGold = initialTrainerGold + expeditions.Sum(e => e.GoldGained) + events.OfType<DonationReceived>().Sum(e => e.Gold)
                + events.OfType<PaydayResolved>().Sum(e => e.Outcome.TotalPaid) + events.OfType<MaterialTradeSettled>().Where(e => e.TrainerId >= 0).Sum(e => e.NetToSeller)
                + events.OfType<ProductTradeSettled>().Sum(e => e.NetToSeller) - events.OfType<ServiceUsed>().Sum(e => e.Paid)
                - events.OfType<MonsterAppraised>().Sum(e => e.PricePaid) - events.OfType<ProductPurchased>().Sum(e => e.TotalPaid)
                - events.OfType<GeneBankFeeSettled>().Sum(e => e.Paid)
                + events.OfType<TrainerLoanBalanceChanged>().Where(e => e.Reason == "Disbursement").Sum(e => e.Amount)
                - events.OfType<TrainerLoanRepaid>().Sum(e => e.Amount);
            long trainerDifference = world.Trainers.Sum(t => t.Gold) - expectedGold;
            long treasuryDifference = world.Treasury - initialTreasury - events.OfType<TreasuryChanged>().Sum(e => e.Delta);
            long supplyMovement = world.SupplyTransactions.Sum(tx => (tx.Payer == "hub:treasury" ? -tx.Gross : 0)
                + (tx.Payee == "hub:treasury" ? tx.Gross - tx.Tax : 0) + (tx.TaxAccount == "hub:treasury" ? tx.Tax : 0));
            string[] supplyReasons = { "TradeTax", "MerchantPurchase", "ProductionCost", "ConsumableSale", "StationProductSale", "ProductBuyback" };
            long supplyDifference = supplyMovement - events.OfType<TreasuryChanged>().Where(e => supplyReasons.Contains(e.Reason)).Sum(e => e.Delta);
            var actualOwners = world.Trainers.SelectMany(t => world.MonstersForTrainer(t.Id).Select(m => (m.Id, trainer: t.Id, m.Custody))).ToArray();
            int ownershipDifference = owners.Count(o => !actualOwners.Any(m => m.Id == o.Key && (m.trainer, m.Custody) == o.Value))
                + actualOwners.Count(m => !owners.TryGetValue(m.Id, out var owner) || owner != (m.trainer, m.Custody));
            var actualProducts = world.Trainers.SelectMany(t => t.Products.Select(p => (key: (t.Id, p.Key.Value), count: p.Value))).ToDictionary(p => p.key, p => (long)p.count);
            long itemDifference = productEventErrors + products.Keys.Concat(actualProducts.Keys).Distinct().Sum(key =>
                Math.Abs((products.TryGetValue(key, out long expected) ? expected : 0) - (actualProducts.TryGetValue(key, out long actual) ? actual : 0)));
            output.WriteLine($"Gold reconciliation: trainer difference {trainerDifference}; treasury difference {treasuryDifference}; supply difference {supplyDifference}");
            output.WriteLine($"Ownership reconciliation: difference {ownershipDifference}; owned {actualOwners.Length}; recovery pending {world.VeterinaryHospital.Recoveries.Count}; bank {world.GeneBank.Count}");
            output.WriteLine($"Item reconciliation: difference {itemDifference}; trainer product stacks {actualProducts.Count}");
            output.WriteLine($"Payday: wages {events.OfType<PaydayResolved>().Sum(e => e.Outcome.TotalPaid)}; bank fees assessed {events.OfType<GeneBankFeeAssessed>().Sum(e => e.Amount)}; confiscations {events.OfType<MonsterConfiscated>().Count()}");
            if (trainerDifference != 0 || treasuryDifference != 0 || supplyDifference != 0 || ownershipDifference != 0 || itemDifference != 0)
                throw new InvalidOperationException("Monster scenario ledgers do not reconcile.");
        }
    }
}
