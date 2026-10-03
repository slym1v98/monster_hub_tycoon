using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;

namespace Game.Domain.Monsters
{
    public sealed class EvolutionDefinition
    {
        public string FromSpeciesId { get; }
        public string BranchId { get; }
        public MonsterDefinition Target { get; }
        public int MinimumLevel { get; }
        public double SuccessChance { get; }
        public int GeneFragments { get; }
        public ProductId ProductCost { get; }
        public int ProductCostUnits { get; }
        public IReadOnlyList<string> SkillIds { get; }

        public EvolutionDefinition(string fromSpeciesId, string branchId, MonsterDefinition target,
            int minimumLevel, double successChance, int geneFragments, ProductId productCost,
            int productCostUnits, IEnumerable<string> skillIds)
        {
            if (string.IsNullOrWhiteSpace(fromSpeciesId)) throw new ArgumentException("Source species is required.", nameof(fromSpeciesId));
            if (string.IsNullOrWhiteSpace(branchId)) throw new ArgumentException("Evolution branch is required.", nameof(branchId));
            Target = target ?? throw new ArgumentNullException(nameof(target)); target.Validate();
            if (Target.Id == fromSpeciesId) throw new ArgumentException("Evolution cannot target the same species.", nameof(target));
            if (minimumLevel < 1 || minimumLevel > 100) throw new ArgumentOutOfRangeException(nameof(minimumLevel));
            if (double.IsNaN(successChance) || double.IsInfinity(successChance) || successChance < 0 || successChance > 1) throw new ArgumentOutOfRangeException(nameof(successChance));
            if (geneFragments < 0) throw new ArgumentOutOfRangeException(nameof(geneFragments));
            if (string.IsNullOrWhiteSpace(productCost.Value) || productCostUnits <= 0) throw new ArgumentOutOfRangeException(nameof(productCostUnits));
            var skills = (skillIds ?? throw new ArgumentNullException(nameof(skillIds))).ToArray();
            if (skills.Length == 0 || skills.Any(string.IsNullOrWhiteSpace) || skills.Distinct(StringComparer.Ordinal).Count() != skills.Length)
                throw new ArgumentException("Evolution must define unique skill IDs.", nameof(skillIds));
            FromSpeciesId = fromSpeciesId; BranchId = branchId; MinimumLevel = minimumLevel;
            SuccessChance = successChance; GeneFragments = geneFragments; ProductCost = productCost;
            ProductCostUnits = productCostUnits; SkillIds = Array.AsReadOnly(skills);
        }
    }

    public sealed class EvolutionCatalog
    {
        readonly IReadOnlyDictionary<string, IReadOnlyList<EvolutionDefinition>> branches;
        public static EvolutionCatalog Empty { get; } = new EvolutionCatalog(Array.Empty<EvolutionDefinition>());
        public IReadOnlyList<EvolutionDefinition> Definitions { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public EvolutionCatalog(IEnumerable<EvolutionDefinition> definitions)
        {
            var entries = (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray();
            if (entries.Any(x => x == null)) throw new ArgumentException("Evolution entries cannot be null.", nameof(definitions));
            if (entries.GroupBy(x => (x.FromSpeciesId, x.BranchId)).Any(x => x.Count() > 1))
                throw new ArgumentException("Evolution branch IDs must be unique per source species.", nameof(definitions));
            var bySource = entries.GroupBy(x => x.FromSpeciesId, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
            foreach (var root in entries.SelectMany(x => new[] { x.FromSpeciesId, x.Target.Id }).Distinct(StringComparer.Ordinal))
                ValidateDepth(root, 0, new HashSet<string>(StringComparer.Ordinal), bySource);
            Definitions = Array.AsReadOnly(entries.OrderBy(x => x.FromSpeciesId, StringComparer.Ordinal)
                .ThenBy(x => x.BranchId, StringComparer.Ordinal).ToArray());
            branches = Definitions.GroupBy(x => x.FromSpeciesId, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => (IReadOnlyList<EvolutionDefinition>)Array.AsReadOnly(x.ToArray()), StringComparer.Ordinal);
            BalanceParameters = Array.AsReadOnly(Definitions.SelectMany(x => new[] {
                new BalanceParameter("evolution." + x.FromSpeciesId + "." + x.BranchId + ".success_chance", x.SuccessChance,
                    "probability", "Prototype", "Evolution odds are not specified in GDD."),
                new BalanceParameter("evolution." + x.FromSpeciesId + "." + x.BranchId + ".min_level", x.MinimumLevel,
                    "Monster level", "Prototype", "Evolution levels are not specified in GDD."),
                new BalanceParameter("evolution." + x.FromSpeciesId + "." + x.BranchId + ".gene_fragments", x.GeneFragments,
                    "units", "Prototype", "Evolution cost is not specified in GDD.")
            }).ToArray());
        }

        public IReadOnlyList<EvolutionDefinition> BranchesFrom(string speciesId)
            => branches.TryGetValue(speciesId ?? string.Empty, out var values) ? values : Array.Empty<EvolutionDefinition>();
        public EvolutionDefinition Find(string speciesId, string branchId)
            => BranchesFrom(speciesId).FirstOrDefault(x => x.BranchId == branchId);

        static void ValidateDepth(string speciesId, int depth, HashSet<string> path,
            IReadOnlyDictionary<string, EvolutionDefinition[]> bySource)
        {
            if (depth > 2) throw new ArgumentException("Evolution chains may contain at most two steps.", nameof(speciesId));
            if (!path.Add(speciesId)) throw new ArgumentException("Evolution catalog cannot contain cycles.", nameof(speciesId));
            if (bySource.TryGetValue(speciesId, out var next))
                foreach (var branch in next) ValidateDepth(branch.Target.Id, depth + 1, new HashSet<string>(path, StringComparer.Ordinal), bySource);
        }
    }
}
