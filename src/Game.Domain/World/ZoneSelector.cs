using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain
{
    public sealed class ZoneIncomeModifier
    {
        public string ZoneId { get; }
        public double AdditionalIncome { get; }
        public bool IsBountyTarget { get; }
        public ZoneIncomeModifier(string zoneId, double additionalIncome, bool isBountyTarget)
        {
            if (string.IsNullOrWhiteSpace(zoneId)) throw new ArgumentException("Zone ID is required.", nameof(zoneId));
            if (double.IsNaN(additionalIncome) || double.IsInfinity(additionalIncome) || additionalIncome < 0) throw new ArgumentOutOfRangeException(nameof(additionalIncome));
            ZoneId = zoneId; AdditionalIncome = additionalIncome; IsBountyTarget = isBountyTarget;
        }
    }

    public sealed class ZoneSelectionConfig
    {
        public static ZoneSelectionConfig Prototype { get; } = new ZoneSelectionConfig();
        public double LuckGoldPerPoint { get; }
        public double ExpectedFarmMinutes { get; }
        public double CapitalistBountyMultiplier { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public ZoneSelectionConfig(double luckGoldPerPoint = 0.01, double expectedFarmMinutes = 60, double capitalistBountyMultiplier = 2)
        {
            ZoneDefinition.ValidateNonNegativeFinite(luckGoldPerPoint, nameof(luckGoldPerPoint));
            ZoneDefinition.ValidatePositiveFinite(expectedFarmMinutes, nameof(expectedFarmMinutes));
            if (double.IsNaN(capitalistBountyMultiplier) || double.IsInfinity(capitalistBountyMultiplier) || capitalistBountyMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(capitalistBountyMultiplier));
            LuckGoldPerPoint = luckGoldPerPoint; ExpectedFarmMinutes = expectedFarmMinutes; CapitalistBountyMultiplier = capitalistBountyMultiplier;
            BalanceParameters = Array.AsReadOnly(new[] {
                new BalanceParameter("zone_selection.luck_gold_per_point", luckGoldPerPoint, "gold/point/encounter", "Prototype", "Prototype conversion; GDD states Luck affects gold quantity."),
                new BalanceParameter("zone_selection.expected_farm_minutes", expectedFarmMinutes, "minutes", "Prototype", "Prototype planning horizon for travel amortization."),
                new BalanceParameter("zone_selection.capitalist_bounty_multiplier", capitalistBountyMultiplier, "multiplier", "Prototype", "docs/designs/01_World_Map_Environment.md: Capitalists favor bounty zones; magnitude is prototype.")
            });
        }
    }

    public sealed class ZoneSelector
    {
        readonly ZoneSelectionConfig config;
        public ZoneSelector(ZoneSelectionConfig config = null) { this.config = config ?? ZoneSelectionConfig.Prototype; }

        public ZoneDefinition Select(TrainerSnapshot trainer, IReadOnlyList<ZoneDefinition> unlockedZones, IReadOnlyList<ZoneIncomeModifier> activeModifiers)
        {
            if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            if (unlockedZones == null) throw new ArgumentNullException(nameof(unlockedZones));
            if (activeModifiers == null) throw new ArgumentNullException(nameof(activeModifiers));
            ValidateZones(unlockedZones); ValidateModifiers(activeModifiers);
            ZoneDefinition best = null; double bestScore = double.NegativeInfinity;
            foreach (var zone in unlockedZones.Where(z => z.MinimumRank <= trainer.Rank))
            {
                var score = ExpectedGoldEquivalentPerHour(trainer, zone, activeModifiers);
                if (score > bestScore || (score == bestScore && string.CompareOrdinal(zone.Id, best.Id) < 0)) { best = zone; bestScore = score; }
            }
            return best;
        }

        public double ExpectedGoldEquivalentPerHour(TrainerSnapshot trainer, ZoneDefinition zone, IReadOnlyList<ZoneIncomeModifier> activeModifiers)
        {
            if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (activeModifiers == null) throw new ArgumentNullException(nameof(activeModifiers));
            if (zone.MinimumRank > trainer.Rank) throw new ArgumentException("Trainer does not meet the Zone rank requirement.", nameof(zone));
            ValidateModifiers(activeModifiers);
            var p = zone.EncounterProfile;
            double totalWeight = zone.MaterialWeights.Sum(x => x.Weight);
            double weightedUnitValue = zone.MaterialWeights.Sum(x => x.Weight * x.GoldEquivalentPerUnit) / totalWeight;
            double pickup = PersonalityProfile.Of(trainer.Personality).MaterialPickRate;
            double gold = p.GoldPerEncounter * (1 + trainer.Attributes.Luck * config.LuckGoldPerPoint);
            double perEncounter = gold + p.ExpectedMaterialUnitsPerEncounter * weightedUnitValue * pickup;
            double hourly = p.ExpectedEncountersPerHour * perEncounter;
            foreach (var modifier in activeModifiers.Where(x => string.Equals(x.ZoneId, zone.Id, StringComparison.Ordinal)).OrderBy(x => x.AdditionalIncome).ThenBy(x => x.IsBountyTarget))
            {
                double amount = modifier.AdditionalIncome;
                if (modifier.IsBountyTarget && trainer.Personality == Personality.Capitalist) amount *= config.CapitalistBountyMultiplier;
                hourly += amount;
            }
            double score = hourly * config.ExpectedFarmMinutes / (config.ExpectedFarmMinutes + 2.0 * zone.WalkMinutes);
            if (double.IsNaN(score) || double.IsInfinity(score)) throw new OverflowException("Computed Zone income is not finite.");
            return score;
        }

        static void ValidateZones(IEnumerable<ZoneDefinition> zones)
        {
            var a = zones.ToArray();
            if (a.Any(x => x == null)) throw new ArgumentException("Zone collection contains null entries.", nameof(zones));
            if (a.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != a.Length) throw new ArgumentException("Zone IDs must be unique.", nameof(zones));
        }
        static void ValidateModifiers(IEnumerable<ZoneIncomeModifier> modifiers)
        { if (modifiers.Any(x => x == null)) throw new ArgumentException("Modifier collection contains null entries.", nameof(modifiers)); }
    }
}
