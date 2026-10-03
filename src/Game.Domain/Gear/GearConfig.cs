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
        public IReadOnlyDictionary<GearStatKind, double> ScoreWeight { get; }
        public double EnhancePerLevelFraction { get; }
        public double StarFractionPerStar { get; }
        public IReadOnlyList<double> RefineMultipliers { get; }
        public int MonsterMaxDurability { get; }
        public int UtilityMaxDurability { get; }
        public int AuraMaxDurability { get; }
        public int MonsterWearPerAction { get; }
        public int MonsterWearPerHit { get; }
        public double UtilityWearPerFarmHour { get; }
        public double UtilityWeatherFactorDefault { get; }
        public double RepairGoldPerDurability { get; }
        public int NightVisionMinimumTier { get; }
        public double AcceptanceGoldFraction { get; }
        public double BuybackFraction { get; }
        public int RefineCrystalCost { get; }
        public int RefineWaterCost { get; }
        public int EnhanceStoneCost { get; }
        public int EnhanceProtectionCharmCost { get; }
        public double StarGoldBase { get; }
        public double StarGoldGrowth { get; }
        public long StarSacrificeGold { get; }
        public int StarFailureDropFromTarget { get; }
        public IReadOnlyList<double> StarSuccessByTarget { get; }
        public double RefineGoldBase { get; }
        public double RefineGoldGrowth { get; }
        public IReadOnlyList<double> RefineSuccessByTarget { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public GearConfig(double enhancePerLevelFraction = 0.05, double starFractionPerStar = 0.05,
            IEnumerable<double> refineMultipliers = null, int monsterMaxDurability = 100, int utilityMaxDurability = 100,
            int auraMaxDurability = 100, int monsterWearPerAction = 1, int monsterWearPerHit = 1,
            double utilityWearPerFarmHour = 2, double repairGoldPerDurability = 2, int nightVisionMinimumTier = 2,
            double acceptanceGoldFraction = 0.5, double buybackFraction = 0.4, int refineCrystalCost = 1, int refineWaterCost = 10,
            IEnumerable<double> starSuccessByTarget = null, IEnumerable<double> refineSuccessByTarget = null,
            double starGoldBase = 300, double starGoldGrowth = 1.8, long starSacrificeGold = 100,
            int starFailureDropFromTarget = 3, double refineGoldBase = 1000, double refineGoldGrowth = 2.5,
            double utilityWeatherFactorDefault = 1, int enhanceStoneCost = 1, int enhanceProtectionCharmCost = 1)
        {
            Positive(enhancePerLevelFraction, nameof(enhancePerLevelFraction));
            Positive(starFractionPerStar, nameof(starFractionPerStar));
            Positive(utilityWearPerFarmHour, nameof(utilityWearPerFarmHour));
            if (double.IsNaN(utilityWeatherFactorDefault) || double.IsInfinity(utilityWeatherFactorDefault) || utilityWeatherFactorDefault < 0) throw new ArgumentOutOfRangeException(nameof(utilityWeatherFactorDefault));
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
            if (enhanceStoneCost <= 0 || enhanceProtectionCharmCost <= 0) throw new ArgumentOutOfRangeException(nameof(enhanceStoneCost));
            Positive(starGoldBase, nameof(starGoldBase)); Positive(starGoldGrowth, nameof(starGoldGrowth));
            Positive(refineGoldBase, nameof(refineGoldBase)); Positive(refineGoldGrowth, nameof(refineGoldGrowth));
            if (starSacrificeGold < 0 || starFailureDropFromTarget < 2 || starFailureDropFromTarget > 5) throw new ArgumentOutOfRangeException(nameof(starSacrificeGold));
            RefineCrystalCost = refineCrystalCost; RefineWaterCost = refineWaterCost;
            EnhanceStoneCost = enhanceStoneCost; EnhanceProtectionCharmCost = enhanceProtectionCharmCost;
            StarGoldBase = starGoldBase; StarGoldGrowth = starGoldGrowth; StarSacrificeGold = starSacrificeGold;
            StarFailureDropFromTarget = starFailureDropFromTarget; RefineGoldBase = refineGoldBase; RefineGoldGrowth = refineGoldGrowth;
            var starRates = (starSuccessByTarget ?? new[] { 0.90, 0.75, 0.55, 0.35 }).ToArray();
            var refineRates = (refineSuccessByTarget ?? new[] { 0.70, 0.50, 0.30, 0.15 }).ToArray();
            if (starRates.Length != 4 || starRates.Any(x => double.IsNaN(x) || double.IsInfinity(x) || x < 0 || x > 1)) throw new ArgumentException("Need four star success probabilities.", nameof(starSuccessByTarget));
            if (refineRates.Length != 4 || refineRates.Any(x => double.IsNaN(x) || double.IsInfinity(x) || x < 0 || x > 1)) throw new ArgumentException("Need four refine success probabilities.", nameof(refineSuccessByTarget));
            StarSuccessByTarget = Array.AsReadOnly(starRates); RefineSuccessByTarget = Array.AsReadOnly(refineRates);
            EnhancePerLevelFraction = enhancePerLevelFraction; StarFractionPerStar = starFractionPerStar;
            RefineMultipliers = Array.AsReadOnly(refine);
            MonsterMaxDurability = monsterMaxDurability; UtilityMaxDurability = utilityMaxDurability; AuraMaxDurability = auraMaxDurability;
            MonsterWearPerAction = monsterWearPerAction; MonsterWearPerHit = monsterWearPerHit;
            UtilityWearPerFarmHour = utilityWearPerFarmHour; RepairGoldPerDurability = repairGoldPerDurability;
            UtilityWeatherFactorDefault = utilityWeatherFactorDefault;
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
            ScoreWeight = new SortedDictionary<GearStatKind, double>
            {
                [GearStatKind.Attack] = 1, [GearStatKind.Defense] = 1, [GearStatKind.Hp] = 0.1,
                [GearStatKind.AttackSpeed] = 100, [GearStatKind.CriticalChance] = 100,
                [GearStatKind.BackpackCapacity] = 1, [GearStatKind.NightVision] = 25,
                [GearStatKind.HydrationDecayReduction] = 100, [GearStatKind.MiningSpeed] = 100,
                [GearStatKind.AuraAttackMultiplier] = 100, [GearStatKind.AuraDefenseMultiplier] = 100,
                [GearStatKind.AuraCritChance] = 100, [GearStatKind.StaminaDecayReduction] = 100,
                [GearStatKind.MoveSpeedMultiplier] = 100, [GearStatKind.WeatherResist] = 100
            };
            const string TBD = "GDD 05 specifies no value; Prototype placeholder.";
            var list = new List<BalanceParameter>
            {
                P("enhance_per_level_fraction", enhancePerLevelFraction, "fraction/level", TBD),
                P("star_fraction_per_star", starFractionPerStar, "fraction/star", "docs/designs/05: stars raise hidden % stats; magnitude unspecified."),
                P("monster_max_durability", monsterMaxDurability, "points", "docs/designs/05: durability exists; capacity unspecified."),
                P("utility_max_durability", utilityMaxDurability, "points", "docs/designs/05: durability exists; capacity unspecified."),
                P("aura_max_durability", auraMaxDurability, "points", "Prototype storage value; docs/designs/05 locks that Aura gear does not wear."),
                P("monster_wear_per_action", monsterWearPerAction, "points/action", "docs/designs/05: wears per skill cast."),
                P("monster_wear_per_hit", monsterWearPerHit, "points/hit", "docs/designs/05: wears per hit taken."),
                P("utility_wear_per_farm_hour", utilityWearPerFarmHour, "points/hour", "docs/designs/05: wears over farm time and weather."),
                P("repair_gold_per_durability", repairGoldPerDurability, "Gold/point", TBD),
                P("night_vision_minimum_tier", nightVisionMinimumTier, "tier", "docs/designs/05: Goggles from a Tier up grant night vision; Tier unspecified."),
                P("acceptance_gold_fraction", acceptanceGoldFraction, "fraction", "docs/designs/05: Trainer accepts by Gear Score, price, money; threshold unspecified."),
                P("buyback_fraction", buybackFraction, "fraction", TBD),
                P("refine_crystal_cost", refineCrystalCost, "World Boss Crystal/step", "docs/designs/05: Refine needs World Boss Crystal; quantity unspecified."),
                P("refine_water_cost", refineWaterCost, "Distilled Water/step", "docs/designs/05: Refine needs Distilled Water; quantity unspecified."),
                P("enhance_stone_cost", enhanceStoneCost, "Enhancement Stone/attempt", "Prototype item cost; docs/designs/05 §2 requires Enhancement Stones."),
                P("enhance_protection_charm_cost", enhanceProtectionCharmCost, "Protection Charm/attempt", "Prototype item cost; docs/designs/05 §2 charm prevents break."),
                P("star_gold_base", starGoldBase, "Gold/attempt", "docs/designs/13 §4: 300 × 1.8^step + 100 sacrifice."),
                P("star_gold_growth", starGoldGrowth, "multiplier/step", "docs/designs/13 §4: 300 × 1.8^step + 100 sacrifice."),
                P("star_sacrifice_gold", starSacrificeGold, "Gold/attempt", "docs/designs/13 §4: +100 sacrifice cost."),
                P("star_failure_drop_from_target", starFailureDropFromTarget, "target stars", "docs/designs/13 §4: failure from step 3 drops one star."),
                P("star_probability_step_1", starRates[0], "probability", "docs/designs/13 §4: 90/75/55/35/20% star-up probabilities."),
                P("star_probability_step_2", starRates[1], "probability", "docs/designs/13 §4: 90/75/55/35/20% star-up probabilities."),
                P("star_probability_step_3", starRates[2], "probability", "docs/designs/13 §4: 90/75/55/35/20% star-up probabilities."),
                P("star_probability_step_4", starRates[3], "probability", "docs/designs/13 §4: 90/75/55/35/20% star-up probabilities."),
                P("star_probability_step_5_unreachable", 0.20, "probability", "docs/designs/13 §4 says 20%; GDD 05 caps at 5 stars, so this fifth step is unreachable and awaits clarification.", "TBD"),
                P("refine_gold_base", refineGoldBase, "Gold/attempt", "docs/designs/13 §4: 1,000 × 2.5^step."),
                P("refine_gold_growth", refineGoldGrowth, "multiplier/step", "docs/designs/13 §4: 1,000 × 2.5^step."),
                P("refine_probability_step_1", refineRates[0], "probability", "docs/designs/13 §4: 70/50/30/15% refine probabilities."),
                P("refine_probability_step_2", refineRates[1], "probability", "docs/designs/13 §4: 70/50/30/15% refine probabilities."),
                P("refine_probability_step_3", refineRates[2], "probability", "docs/designs/13 §4: 70/50/30/15% refine probabilities."),
                P("refine_probability_step_4", refineRates[3], "probability", "docs/designs/13 §4: 70/50/30/15% refine probabilities."),
                P("utility_weather_factor_default", utilityWeatherFactorDefault, "multiplier", "Prototype neutral factor until weather is integrated by sub-project 6.")
            };
            for (int i = 0; i < refine.Length; i++) list.Add(P("refine_multiplier_" + i, refine[i], "multiplier", "docs/designs/05: Refine Normal→Mythic; magnitude unspecified."));
            foreach (var pair in UnitStat) list.Add(P("unit_stat_" + StatKey(pair.Key), pair.Value, "stat/tier", TBD));
            foreach (var pair in ScoreWeight) list.Add(P("score_weight_" + StatKey(pair.Key), pair.Value, "score/stat unit", "Prototype conversion in GearScore.cs; GDD 05 only specifies comparison by Gear Score."));
            BalanceParameters = Array.AsReadOnly(list.ToArray());
        }

        static void Positive(double value, string name)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(name); }

        static string StatKey(GearStatKind kind) => kind switch
        {
            GearStatKind.Attack => "attack", GearStatKind.Defense => "defense", GearStatKind.Hp => "hp",
            GearStatKind.AttackSpeed => "attack_speed", GearStatKind.CriticalChance => "critical_chance",
            GearStatKind.BackpackCapacity => "backpack_capacity", GearStatKind.NightVision => "night_vision",
            GearStatKind.HydrationDecayReduction => "hydration_decay_reduction", GearStatKind.MiningSpeed => "mining_speed",
            GearStatKind.AuraAttackMultiplier => "aura_attack_multiplier", GearStatKind.AuraDefenseMultiplier => "aura_defense_multiplier",
            GearStatKind.AuraCritChance => "aura_crit_chance", GearStatKind.StaminaDecayReduction => "stamina_decay_reduction",
            GearStatKind.MoveSpeedMultiplier => "move_speed_multiplier", GearStatKind.WeatherResist => "weather_resist",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        public double StarSuccess(int targetStars)
        {
            if (targetStars < 2 || targetStars > 5) throw new ArgumentOutOfRangeException(nameof(targetStars));
            return StarSuccessByTarget[targetStars - 2];
        }
        public long StarAttemptCost(int targetStars)
        {
            if (targetStars < 2 || targetStars > 5) throw new ArgumentOutOfRangeException(nameof(targetStars));
            return checked((long)Math.Ceiling(StarGoldBase * Math.Pow(StarGoldGrowth, targetStars - 2) + StarSacrificeGold));
        }
        public double RefineSuccess(int targetGrade)
        {
            if (targetGrade < 1 || targetGrade > 4) throw new ArgumentOutOfRangeException(nameof(targetGrade));
            return RefineSuccessByTarget[targetGrade - 1];
        }
        public long RefineAttemptCost(int targetGrade)
        {
            if (targetGrade < 1 || targetGrade > 4) throw new ArgumentOutOfRangeException(nameof(targetGrade));
            return checked((long)Math.Ceiling(RefineGoldBase * Math.Pow(RefineGoldGrowth, targetGrade - 1)));
        }

        static BalanceParameter P(string id, double value, string unit, string source, string status = "Prototype")
            => new BalanceParameter("gear." + id, value, unit, status, source);
    }
}
