using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Gear
{
    /// <summary>
    /// Tham số trang bị. GDD 05 không cho số nên mọi giá trị là Prototype/TBD; Id ổn định theo tiền tố gear.
    /// </summary>
    public sealed class GearConfig
    {
        public static GearConfig Prototype { get; } = new GearConfig();
        /// <summary>Chỉ số chính ở Tier 1 cho mỗi GearStatKind; Tier t nhân t.</summary>
        public IReadOnlyDictionary<GearStatKind, double> UnitStat { get; }
        public double EnhancePerLevelFraction { get; }
        public double StarFractionPerStar { get; }
        public IReadOnlyList<double> RefineMultipliers { get; }
        public int MonsterMaxDurability { get; }
        public int UtilityMaxDurability { get; }
        public int AuraMaxDurability { get; }
        public int MonsterWearPerAction { get; }
        public int MonsterWearPerHit { get; }
        public double UtilityWearPerFarmHour { get; }
        public double RepairGoldPerDurability { get; }
        public int NightVisionMinimumTier { get; }
        public double AcceptanceGoldFraction { get; }
        public double BuybackFraction { get; }
        public int RefineCrystalCost { get; }
        public int RefineWaterCost { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public GearConfig(double enhancePerLevelFraction = 0.05, double starFractionPerStar = 0.05,
            IEnumerable<double> refineMultipliers = null, int monsterMaxDurability = 100, int utilityMaxDurability = 100,
            int auraMaxDurability = 100, int monsterWearPerAction = 1, int monsterWearPerHit = 1,
            double utilityWearPerFarmHour = 2, double repairGoldPerDurability = 2, int nightVisionMinimumTier = 2,
            double acceptanceGoldFraction = 0.5, double buybackFraction = 0.4, int refineCrystalCost = 1, int refineWaterCost = 10)
        {
            Positive(enhancePerLevelFraction, nameof(enhancePerLevelFraction));
            Positive(starFractionPerStar, nameof(starFractionPerStar));
            Positive(utilityWearPerFarmHour, nameof(utilityWearPerFarmHour));
            Positive(repairGoldPerDurability, nameof(repairGoldPerDurability));
            if (acceptanceGoldFraction <= 0 || acceptanceGoldFraction > 1) throw new ArgumentOutOfRangeException(nameof(acceptanceGoldFraction));
            if (buybackFraction < 0 || buybackFraction > 1) throw new ArgumentOutOfRangeException(nameof(buybackFraction));
            if (monsterMaxDurability <= 0 || utilityMaxDurability <= 0 || auraMaxDurability <= 0) throw new ArgumentOutOfRangeException(nameof(monsterMaxDurability));
            if (monsterWearPerAction < 0 || monsterWearPerHit < 0) throw new ArgumentOutOfRangeException(nameof(monsterWearPerAction));
            if (nightVisionMinimumTier < 1 || nightVisionMinimumTier > 5) throw new ArgumentOutOfRangeException(nameof(nightVisionMinimumTier));
            var refine = (refineMultipliers ?? new[] { 1.0, 1.2, 1.5, 2.0, 3.0 }).ToArray();
            if (refine.Length != 5 || refine.Any(x => double.IsNaN(x) || double.IsInfinity(x) || x <= 0)) throw new ArgumentException("Need five positive refine multipliers.", nameof(refineMultipliers));
            for (int i = 1; i < refine.Length; i++) if (refine[i] < refine[i - 1]) throw new ArgumentException("Refine multipliers must not decrease.", nameof(refineMultipliers));
            if (refineCrystalCost <= 0 || refineWaterCost <= 0) throw new ArgumentOutOfRangeException(nameof(refineCrystalCost));
            RefineCrystalCost = refineCrystalCost; RefineWaterCost = refineWaterCost;
            EnhancePerLevelFraction = enhancePerLevelFraction; StarFractionPerStar = starFractionPerStar;
            RefineMultipliers = Array.AsReadOnly(refine);
            MonsterMaxDurability = monsterMaxDurability; UtilityMaxDurability = utilityMaxDurability; AuraMaxDurability = auraMaxDurability;
            MonsterWearPerAction = monsterWearPerAction; MonsterWearPerHit = monsterWearPerHit;
            UtilityWearPerFarmHour = utilityWearPerFarmHour; RepairGoldPerDurability = repairGoldPerDurability;
            NightVisionMinimumTier = nightVisionMinimumTier;
            AcceptanceGoldFraction = acceptanceGoldFraction; BuybackFraction = buybackFraction;
            UnitStat = new SortedDictionary<GearStatKind, double>
            {
                [GearStatKind.Attack] = 5, [GearStatKind.Defense] = 4, [GearStatKind.Hp] = 40,
                [GearStatKind.AttackSpeed] = 0.05, [GearStatKind.CriticalChance] = 0.01,
                [GearStatKind.BackpackCapacity] = 5, [GearStatKind.NightVision] = 1,
                [GearStatKind.HydrationDecayReduction] = 0.1, [GearStatKind.MiningSpeed] = 0.1,
                [GearStatKind.AuraAttackMultiplier] = 0.02, [GearStatKind.AuraDefenseMultiplier] = 0.02,
                [GearStatKind.AuraCritChance] = 0.01, [GearStatKind.StaminaDecayReduction] = 0.1,
                [GearStatKind.MoveSpeedMultiplier] = 0.05, [GearStatKind.WeatherResist] = 0.1
            };
            const string TBD = "GDD 05 specifies no value; Prototype placeholder.";
            var list = new List<BalanceParameter>
            {
                P("enhance_per_level_fraction", enhancePerLevelFraction, "fraction/level", TBD),
                P("star_fraction_per_star", starFractionPerStar, "fraction/star", "docs/designs/05: stars raise hidden % stats; magnitude unspecified."),
                P("monster_max_durability", monsterMaxDurability, "points", "docs/designs/05: durability exists; capacity unspecified."),
                P("utility_max_durability", utilityMaxDurability, "points", "docs/designs/05: durability exists; capacity unspecified."),
                P("aura_max_durability", auraMaxDurability, "points", "docs/designs/05: Aura gear does not wear; kept for repair-free invariant.", "Locked"),
                P("monster_wear_per_action", monsterWearPerAction, "points/action", "docs/designs/05: wears per skill cast."),
                P("monster_wear_per_hit", monsterWearPerHit, "points/hit", "docs/designs/05: wears per hit taken."),
                P("utility_wear_per_farm_hour", utilityWearPerFarmHour, "points/hour", "docs/designs/05: wears over farm time and weather."),
                P("repair_gold_per_durability", repairGoldPerDurability, "Gold/point", TBD),
                P("night_vision_minimum_tier", nightVisionMinimumTier, "tier", "docs/designs/05: Goggles from a Tier up grant night vision; Tier unspecified."),
                P("acceptance_gold_fraction", acceptanceGoldFraction, "fraction", "docs/designs/05: Trainer accepts by Gear Score, price, money; threshold unspecified."),
                P("buyback_fraction", buybackFraction, "fraction", TBD),
                P("refine_crystal_cost", refineCrystalCost, "World Boss Crystal/step", "docs/designs/05: Refine needs World Boss Crystal; quantity unspecified."),
                P("refine_water_cost", refineWaterCost, "Distilled Water/step", "docs/designs/05: Refine needs Distilled Water; quantity unspecified.")
            };
            for (int i = 0; i < refine.Length; i++) list.Add(P("refine_multiplier_" + i, refine[i], "multiplier", "docs/designs/05: Refine Normal→Mythic; magnitude unspecified."));
            foreach (var pair in UnitStat) list.Add(P("unit_stat_" + pair.Key, pair.Value, "stat/tier", TBD));
            BalanceParameters = Array.AsReadOnly(list.ToArray());
        }

        static void Positive(double value, string name)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(name); }

        static BalanceParameter P(string id, double value, string unit, string source, string status = "Prototype")
            => new BalanceParameter("gear." + id, value, unit, status, source);
    }
}
