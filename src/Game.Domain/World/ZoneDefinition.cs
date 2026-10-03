using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Monsters;

namespace Game.Domain
{
    public sealed class BalanceParameter
    {
        public string Id { get; }
        public double Value { get; }
        public string Unit { get; }
        public string Status { get; }
        public string Source { get; }
        public BalanceParameter(string id, double value, string unit, string status, string source)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(unit) || string.IsNullOrWhiteSpace(status) || string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Balance parameter metadata is required.");
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Id = id; Value = value; Unit = unit; Status = status; Source = source;
        }
    }

    public sealed class ZoneMaterialWeight
    {
        public MaterialId MaterialId { get; }
        public double Weight { get; }
        public double GoldEquivalentPerUnit { get; }
        public ZoneMaterialWeight(MaterialId materialId, double weight, double goldEquivalentPerUnit)
        {
            if (materialId.Value == null || !MaterialCatalog.Default.Materials.Any(x => x.Id == materialId)) throw new ArgumentException("MaterialId is not in the material catalog.", nameof(materialId));
            ValidatePositiveFinite(weight, nameof(weight)); ValidateNonNegativeFinite(goldEquivalentPerUnit, nameof(goldEquivalentPerUnit));
            MaterialId = materialId; Weight = weight; GoldEquivalentPerUnit = goldEquivalentPerUnit;
        }
        static void ValidatePositiveFinite(double v, string name) { if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0) throw new ArgumentOutOfRangeException(name); }
        static void ValidateNonNegativeFinite(double v, string name) { if (double.IsNaN(v) || double.IsInfinity(v) || v < 0) throw new ArgumentOutOfRangeException(name); }
    }

    public sealed class ZoneDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int MinimumRank { get; }
        public int WalkMinutes { get; }
        public IReadOnlyList<ZoneMaterialWeight> MaterialWeights { get; }
        public EncounterProfile EncounterProfile { get; }
        public double NightLootMultiplier { get; }
        public double NightExperienceMultiplier { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public ZoneDefinition(string id, string displayName, int minimumRank, int walkMinutes,
            IEnumerable<ZoneMaterialWeight> materialWeights, EncounterProfile encounterProfile,
            double nightLootMultiplier = 2, double nightExperienceMultiplier = 2)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Zone ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Zone name is required.", nameof(displayName));
            if (minimumRank < 1 || minimumRank > 5) throw new ArgumentOutOfRangeException(nameof(minimumRank));
            if (walkMinutes < 0) throw new ArgumentOutOfRangeException(nameof(walkMinutes));
            if (materialWeights == null) throw new ArgumentNullException(nameof(materialWeights));
            var weights = materialWeights.ToArray();
            if (weights.Length == 0 || weights.Any(x => x == null) || weights.Select(x => x.MaterialId).Distinct().Count() != weights.Length) throw new ArgumentException("Material weights must be non-empty, non-null and unique.", nameof(materialWeights));
            EncounterProfile = encounterProfile ?? throw new ArgumentNullException(nameof(encounterProfile));
            ValidatePositiveFinite(nightLootMultiplier, nameof(nightLootMultiplier)); ValidatePositiveFinite(nightExperienceMultiplier, nameof(nightExperienceMultiplier));
            Id = id; DisplayName = displayName; MinimumRank = minimumRank; WalkMinutes = walkMinutes;
            MaterialWeights = Array.AsReadOnly(weights); NightLootMultiplier = nightLootMultiplier; NightExperienceMultiplier = nightExperienceMultiplier;
            var parameters = new List<BalanceParameter> {
                new BalanceParameter(id + ".walk_minutes", walkMinutes, "minutes", "Prototype", "docs/designs/01_World_Map_Environment.md §1: farther zones take longer; duration is a prototype."),
                new BalanceParameter(id + ".night_loot_multiplier", nightLootMultiplier, "multiplier", "Locked", "docs/designs/04_Monster_System.md: night loot ×2."),
                new BalanceParameter(id + ".night_experience_multiplier", nightExperienceMultiplier, "multiplier", "Locked", "docs/designs/04_Monster_System.md: night EXP ×2.")
            };
            foreach (var w in weights)
            {
                parameters.Add(new BalanceParameter(id + ".material." + w.MaterialId.Value + ".weight", w.Weight, "relative_weight", "Prototype", "Prototype encounter material distribution."));
                parameters.Add(new BalanceParameter(id + ".material." + w.MaterialId.Value + ".gold_equivalent", w.GoldEquivalentPerUnit, "gold/unit", "Prototype", "Prototype material fair-value estimate used for Zone scoring."));
            }
            foreach (var (key, value, unit) in EncounterParameters(EncounterProfile)) parameters.Add(new BalanceParameter(id + ".encounter." + key, value, unit, "Prototype", "Prototype encounter profile; not numerically fixed by GDD."));
            foreach (var e in EncounterProfile.ElementWeights) parameters.Add(new BalanceParameter(id + ".element." + e.Key.ToString().ToLowerInvariant() + ".weight", e.Value, "relative_weight", "Prototype", "docs/designs/04_Monster_System.md element theme; relative mix is a prototype."));
            BalanceParameters = Array.AsReadOnly(parameters.ToArray());
        }

        static IEnumerable<(string, double, string)> EncounterParameters(EncounterProfile p)
        {
            yield return ("encounters_per_hour", p.ExpectedEncountersPerHour, "encounters/hour");
            yield return ("gold_per_encounter", p.GoldPerEncounter, "gold/encounter");
            yield return ("material_units_per_encounter", p.ExpectedMaterialUnitsPerEncounter, "units/encounter");
            yield return ("experience_per_encounter", p.ExperiencePerEncounter, "EXP/encounter");
        }
        internal static void ValidatePositiveFinite(double v, string name) { if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0) throw new ArgumentOutOfRangeException(name); }
        internal static void ValidateNonNegativeFinite(double v, string name) { if (double.IsNaN(v) || double.IsInfinity(v) || v < 0) throw new ArgumentOutOfRangeException(name); }
    }
}
