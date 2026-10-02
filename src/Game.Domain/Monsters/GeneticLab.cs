using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;

namespace Game.Domain.Monsters
{
    public sealed class UpgradeConfig
    {
        static readonly double[] DefaultChances = { 0.60, 0.40, 0.25, 0.10 };
        public static UpgradeConfig Prototype { get; } = new UpgradeConfig();
        public IReadOnlyList<double> SuccessChances { get; }
        public IReadOnlyList<int> EnhancementStoneCosts { get; }
        public IReadOnlyList<int> GeneFragmentCosts { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public UpgradeConfig(IEnumerable<double> successChances = null, IEnumerable<int> enhancementStoneCosts = null,
            IEnumerable<int> geneFragmentCosts = null)
        {
            var chances = (successChances ?? DefaultChances).ToArray();
            var stones = (enhancementStoneCosts ?? new[] { 1, 2, 3, 4 }).ToArray();
            var fragments = (geneFragmentCosts ?? new[] { 10, 20, 30, 40 }).ToArray();
            if (chances.Length != 4 || chances.Any(x => double.IsNaN(x) || double.IsInfinity(x) || x < 0 || x > 1))
                throw new ArgumentException("Four valid rarity upgrade chances are required.", nameof(successChances));
            if (stones.Length != 4 || stones.Any(x => x < 0)) throw new ArgumentException("Four nonnegative Stone costs are required.", nameof(enhancementStoneCosts));
            if (fragments.Length != 4 || fragments.Any(x => x < 0)) throw new ArgumentException("Four nonnegative Fragment costs are required.", nameof(geneFragmentCosts));
            SuccessChances = Array.AsReadOnly(chances); EnhancementStoneCosts = Array.AsReadOnly(stones);
            GeneFragmentCosts = Array.AsReadOnly(fragments);
            BalanceParameters = Array.AsReadOnly(Enumerable.Range(0, 4).SelectMany(i => new[] {
                P("step_" + (i + 1) + ".success_chance", chances[i], "probability"),
                P("step_" + (i + 1) + ".enhancement_stones", stones[i], "units"),
                P("step_" + (i + 1) + ".gene_fragments", fragments[i], "units")
            }).ToArray());
        }
        static BalanceParameter P(string id, double value, string unit) => new BalanceParameter("rarity_upgrade." + id,
            value, unit, "Prototype", "docs/designs/04_Monster_System.md and 13_Balance_Parameters.md: prototype upgrade rates/costs.");
    }

    public sealed class GeneticLabConfig
    {
        public static GeneticLabConfig Prototype { get; } = new GeneticLabConfig();
        public long AppraisalPrice { get; }
        public int DismantleGeneFragments { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public GeneticLabConfig(long appraisalPrice = 25, int dismantleGeneFragments = 5)
        {
            if (appraisalPrice < 0 || dismantleGeneFragments < 0) throw new ArgumentOutOfRangeException(nameof(appraisalPrice));
            AppraisalPrice = appraisalPrice; DismantleGeneFragments = dismantleGeneFragments;
            BalanceParameters = Array.AsReadOnly(new[] {
                P("appraisal_price", appraisalPrice, "Gold"), P("dismantle_gene_fragments", dismantleGeneFragments, "units")
            });
        }
        static BalanceParameter P(string id, double value, string unit) => new BalanceParameter("genetic_lab." + id,
            value, unit, "Prototype", "Lab service price and salvage yield are not quantified in GDD.");
    }

    public sealed class AppraisalResult
    {
        public bool Applied { get; }
        public MonsterIvGrade? RevealedIv { get; }
        public long PricePaid { get; }
        internal AppraisalResult(bool applied, MonsterIvGrade? iv, long paid) { Applied = applied; RevealedIv = iv; PricePaid = paid; }
    }
    public sealed class DismantleResult
    {
        public bool Applied { get; }
        public int GeneFragments { get; }
        internal DismantleResult(bool applied, int fragments) { Applied = applied; GeneFragments = fragments; }
    }
    public sealed class UpgradeResult
    {
        public bool Eligible { get; }
        public bool Success { get; }
        public double SuccessChance { get; }
        public double? Roll { get; }
        public bool ProtectionApplied { get; }
        public Rarity? ResultRarity { get; }
        internal UpgradeResult(bool eligible, bool success, double chance = 0, double? roll = null,
            bool protectionApplied = false, Rarity? rarity = null)
        { Eligible = eligible; Success = success; SuccessChance = chance; Roll = roll; ProtectionApplied = protectionApplied; ResultRarity = rarity; }
    }
    public sealed class EvolutionResult
    {
        public bool Eligible { get; }
        public bool Success { get; }
        public string BranchId { get; }
        public double SuccessChance { get; }
        public double? Roll { get; }
        public bool ProtectionApplied { get; }
        public string ResultSpeciesId { get; }
        internal EvolutionResult(bool eligible, bool success, string branchId, double chance = 0,
            double? roll = null, bool protectionApplied = false, string resultSpeciesId = null)
        { Eligible = eligible; Success = success; BranchId = branchId; SuccessChance = chance; Roll = roll; ProtectionApplied = protectionApplied; ResultSpeciesId = resultSpeciesId; }
    }

    public sealed class GeneticLab
    {
        static readonly ProductId Fragments = new ProductId("gene_fragment");
        static readonly ProductId Stones = new ProductId("enhancement_stone");
        readonly Trainer trainer;
        readonly GeneBank geneBank;
        readonly GeneticLabConfig config;
        readonly UpgradeConfig upgradeConfig;
        readonly EvolutionCatalog evolutionCatalog;

        public GeneticLab(Trainer trainer, GeneBank geneBank = null, GeneticLabConfig config = null,
            UpgradeConfig upgradeConfig = null, EvolutionCatalog evolutionCatalog = null)
        {
            this.trainer = trainer ?? throw new ArgumentNullException(nameof(trainer));
            this.geneBank = geneBank;
            this.config = config ?? GeneticLabConfig.Prototype;
            this.upgradeConfig = upgradeConfig ?? UpgradeConfig.Prototype;
            this.evolutionCatalog = evolutionCatalog ?? EvolutionCatalog.Empty;
        }

        public AppraisalResult Appraise(MonsterId id)
        {
            var monster = FindOwned(id);
            if (monster == null || monster.IsIvAppraised || trainer.Gold < config.AppraisalPrice) return new AppraisalResult(false, null, 0);
            trainer.Gold -= config.AppraisalPrice;
            monster.RevealIv();
            return new AppraisalResult(true, monster.Iv, config.AppraisalPrice);
        }

        public DismantleResult Dismantle(MonsterId id)
        {
            var monster = FindOwned(id);
            if (monster == null || (monster.Iv != MonsterIvGrade.D && monster.Iv != MonsterIvGrade.C)
                || (config.DismantleGeneFragments > 0 && !trainer.Inventory.CanAdd(Fragments, config.DismantleGeneFragments)))
                return new DismantleResult(false, 0);
            Monster consumed;
            if (monster.Custody == MonsterCustody.GeneBank)
            {
                consumed = geneBank?.DismantleForTrainer(trainer.Id, id);
                if (consumed == null) return new DismantleResult(false, 0);
            }
            else
            {
                try { consumed = trainer.Roster.RemoveForTransfer(id); }
                catch (InvalidOperationException) { return new DismantleResult(false, 0); }
            }
            consumed.Custody = MonsterCustody.Consumed;
            if (config.DismantleGeneFragments > 0) trainer.Inventory.Add(Fragments, config.DismantleGeneFragments);
            return new DismantleResult(true, config.DismantleGeneFragments);
        }

        public UpgradeResult UpgradeRarity(MonsterId id, ProductId protectionCharm, SimRandom random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var monster = FindOwned(id);
            if (monster == null || monster.Level < 40 || monster.Custody != MonsterCustody.Trainer
                || (int)monster.Rarity >= upgradeConfig.SuccessChances.Count) return new UpgradeResult(false, false);
            int step = (int)monster.Rarity;
            int stoneCost = upgradeConfig.EnhancementStoneCosts[step];
            int fragmentCost = upgradeConfig.GeneFragmentCosts[step];
            bool charmRequested = !string.IsNullOrWhiteSpace(protectionCharm.Value);
            if (!CanPay(new[] { (Stones, stoneCost), (Fragments, fragmentCost) }, protectionCharm, charmRequested))
                return new UpgradeResult(false, false);
            double chance = upgradeConfig.SuccessChances[step];
            double roll = random.NextDouble();
            bool success = roll < chance;
            bool protectedFailure = !success && charmRequested;
            if (success || !protectedFailure)
            {
                Consume(new[] { (Stones, stoneCost), (Fragments, fragmentCost) });
                if (success) monster.ApplyRarity((Rarity)((int)monster.Rarity + 1));
            }
            if (protectedFailure) trainer.Inventory.TryConsume(protectionCharm, 1);
            return new UpgradeResult(true, success, chance, roll, protectedFailure, monster.Rarity);
        }

        public EvolutionResult Evolve(MonsterId id, string branchId, ProductId protectionCharm, SimRandom random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var monster = FindOwned(id);
            var evolution = monster == null ? null : evolutionCatalog.Find(monster.SpeciesId, branchId);
            if (evolution == null || monster.Custody != MonsterCustody.Trainer || monster.Level < evolution.MinimumLevel)
                return new EvolutionResult(false, false, branchId);
            bool charmRequested = !string.IsNullOrWhiteSpace(protectionCharm.Value);
            var costs = new[] { (new ProductId("gene_fragment"), evolution.GeneFragments), (evolution.ProductCost, evolution.ProductCostUnits) };
            if (!CanPay(costs, protectionCharm, charmRequested)) return new EvolutionResult(false, false, branchId);
            double roll = random.NextDouble();
            bool success = roll < evolution.SuccessChance;
            bool protectedFailure = !success && charmRequested;
            if (success || !protectedFailure)
            {
                Consume(costs);
                if (success) monster.ApplyEvolution(evolution.Target, evolution.SkillIds);
            }
            if (protectedFailure) trainer.Inventory.TryConsume(protectionCharm, 1);
            return new EvolutionResult(true, success, branchId, evolution.SuccessChance, roll,
                protectedFailure, monster.SpeciesId);
        }

        Monster FindOwned(MonsterId id)
            => trainer.Roster.Members.Concat(trainer.Roster.Storage).FirstOrDefault(x => x.Id == id)
                ?? geneBank?.GetStoredMonster(trainer.Id, id);

        bool CanPay(IEnumerable<(ProductId product, int units)> costs, ProductId charm, bool charmRequested)
        {
            if (charmRequested && charm != new ProductId("protection_charm")) return false;
            if (charmRequested && trainer.Inventory.Count(charm) < 1) return false;
            var totals = costs.GroupBy(x => x.product).Select(x => (product: x.Key, units: x.Sum(y => y.units)));
            return totals.All(x => trainer.Inventory.Count(x.product) >= x.units);
        }
        void Consume(IEnumerable<(ProductId product, int units)> costs)
        {
            foreach (var cost in costs.GroupBy(x => x.product).Select(x => (product: x.Key, units: x.Sum(y => y.units))))
                if (cost.units > 0 && !trainer.Inventory.TryConsume(cost.product, cost.units)) throw new InvalidOperationException("Validated Genetic Lab costs changed during transaction.");
        }
    }
}
