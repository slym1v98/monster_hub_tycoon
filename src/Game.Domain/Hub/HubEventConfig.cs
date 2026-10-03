using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class HubEventConfig
    {
        public static HubEventConfig Prototype { get; } = new HubEventConfig();
        public int RandomCrisisCheckIntervalMinutes { get; }
        public double RandomCrisisProbability { get; }
        public int BlackFridayDays { get; }
        public int BlackFridayImpulseUnits { get; }
        public long BlackFridayJunkGearPrice { get; }
        public double InspectionTaxThreshold { get; }
        public double InspectionStressThreshold { get; }
        public double InspectionFineFraction { get; }
        public int InspectionCooldownMinutes { get; }
        public int InspectionResolutionMinutes { get; }
        public int BreedingSeasonDurationDays { get; }
        public double BreedingSeasonRareIvWeightMultiplier { get; }
        public double BreedingSeasonDemandMultiplier { get; }
        public double BreedingSeasonPriceCapMultiplier { get; } = 3;
        public int MonsterFluDurationDays { get; }
        public int MonsterFluVaccineUnitsToCure { get; }
        public long MonsterFluHospitalRevenueBonus { get; }
        public double MonsterFluDailyHpLossFraction { get; }
        public double MonsterFluInitialHpFraction { get; }
        public double BreedingSeasonOrdinaryIvWeight { get; }
        public int BreedingSeasonRareIvGradeCount { get; }
        public int MonsterSiegeDurationMinutes { get; }
        public int MonsterSiegeEnemyHp { get; }
        public int MonsterSiegeMinimumRank { get; }
        public double MonsterSiegeEnemyAttack { get; }
        public double MonsterSiegeEnemyDefense { get; }
        public double MonsterSiegeEnemyAttackSpeed { get; }
        public double MonsterSiegeBuildingDamageProbability { get; }
        public int MonsterSiegeVictoryGold { get; }
        public int MonsterSiegeBossCoreCount { get; }
        public double MonsterSiegeMinimumWinFraction { get; }
        public int WorldBossDurationMinutes { get; }
        public int WorldBossMinimumRank { get; }
        public int WorldBossHp { get; }
        public double WorldBossAttack { get; }
        public double WorldBossDefense { get; }
        public double WorldBossAttackSpeed { get; }
        public long WorldBossActivationGold { get; }
        public int WorldBossCooldownMinutes { get; }
        public double WorldBossBuildingDamageProbability { get; }
        public int WorldBossCrystalCount { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public HubEventConfig(int randomCrisisCheckIntervalMinutes = 1440, double randomCrisisProbability = 0.01,
            int blackFridayDays = 3, int blackFridayImpulseUnits = 1,
            double inspectionTaxThreshold = 0.30, double inspectionStressThreshold = 80,
            double inspectionFineFraction = 0.10, int inspectionCooldownMinutes = 43200,
            int inspectionResolutionMinutes = 60, int breedingSeasonDurationDays = 7,
            int monsterFluDurationDays = 7, double monsterFluDailyHpLossFraction = 0.05,
            double breedingSeasonRareIvWeightMultiplier = 2, double breedingSeasonDemandMultiplier = 3,
            int monsterSiegeDurationMinutes = 360, int monsterSiegeMinimumRank = 1, int monsterSiegeEnemyHp = 1800,
            double monsterSiegeEnemyAttack = 80, double monsterSiegeEnemyDefense = 35,
            double monsterSiegeEnemyAttackSpeed = 1,
            double monsterSiegeBuildingDamageProbability = 0.25, int monsterSiegeVictoryGold = 500,
            double monsterSiegeMinimumWinFraction = 0.5,
            int worldBossDurationMinutes = 360, int worldBossMinimumRank = 3, int worldBossHp = 5000,
            double worldBossAttack = 250, double worldBossDefense = 80, double worldBossAttackSpeed = 1,
            long worldBossActivationGold = 5000,
            int worldBossCooldownMinutes = 43200, double worldBossBuildingDamageProbability = 0.25,
            int worldBossCrystalCount = 1, int monsterFluVaccineUnitsToCure = 1, int monsterSiegeBossCoreCount = 1, long monsterFluHospitalRevenueBonus = 500,
            long blackFridayJunkGearPrice = 100)
        {
            if (randomCrisisCheckIntervalMinutes <= 0 || !UnitInterval(randomCrisisProbability)) throw new ArgumentOutOfRangeException(nameof(randomCrisisCheckIntervalMinutes));
            if (blackFridayDays <= 0 || blackFridayImpulseUnits < 0 || inspectionCooldownMinutes <= 0 || inspectionResolutionMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(blackFridayDays));
            if (blackFridayJunkGearPrice <= 0) throw new ArgumentOutOfRangeException(nameof(blackFridayJunkGearPrice));
            if (double.IsNaN(inspectionTaxThreshold) || inspectionTaxThreshold < 0 || inspectionTaxThreshold > 1) throw new ArgumentOutOfRangeException(nameof(inspectionTaxThreshold));
            if (double.IsNaN(inspectionStressThreshold) || inspectionStressThreshold < 0 || inspectionStressThreshold > 100) throw new ArgumentOutOfRangeException(nameof(inspectionStressThreshold));
            if (double.IsNaN(inspectionFineFraction) || inspectionFineFraction < 0 || inspectionFineFraction > 1) throw new ArgumentOutOfRangeException(nameof(inspectionFineFraction));
            if (breedingSeasonDurationDays <= 0 || monsterFluDurationDays <= 0 || monsterFluVaccineUnitsToCure <= 0 || monsterSiegeBossCoreCount <= 0 || monsterFluHospitalRevenueBonus < 0) throw new ArgumentOutOfRangeException(nameof(breedingSeasonDurationDays));
            if (double.IsNaN(monsterFluDailyHpLossFraction) || monsterFluDailyHpLossFraction < 0 || monsterFluDailyHpLossFraction > 1) throw new ArgumentOutOfRangeException(nameof(monsterFluDailyHpLossFraction));
            if (double.IsNaN(breedingSeasonRareIvWeightMultiplier) || breedingSeasonRareIvWeightMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(breedingSeasonRareIvWeightMultiplier));
            if (double.IsNaN(breedingSeasonDemandMultiplier) || breedingSeasonDemandMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(breedingSeasonDemandMultiplier));
            RandomCrisisCheckIntervalMinutes = randomCrisisCheckIntervalMinutes;
            RandomCrisisProbability = randomCrisisProbability;
            BlackFridayDays = blackFridayDays; BlackFridayImpulseUnits = blackFridayImpulseUnits;
            BlackFridayJunkGearPrice = blackFridayJunkGearPrice;
            InspectionTaxThreshold = inspectionTaxThreshold; InspectionStressThreshold = inspectionStressThreshold;
            InspectionFineFraction = inspectionFineFraction; InspectionCooldownMinutes = inspectionCooldownMinutes;
            InspectionResolutionMinutes = inspectionResolutionMinutes;
            BreedingSeasonDurationDays = breedingSeasonDurationDays;
            BreedingSeasonRareIvWeightMultiplier = breedingSeasonRareIvWeightMultiplier;
            BreedingSeasonDemandMultiplier = breedingSeasonDemandMultiplier;
            MonsterFluDurationDays = monsterFluDurationDays;
            MonsterFluVaccineUnitsToCure = monsterFluVaccineUnitsToCure;
            MonsterFluHospitalRevenueBonus = monsterFluHospitalRevenueBonus;
            MonsterSiegeBossCoreCount = monsterSiegeBossCoreCount;
            MonsterFluDailyHpLossFraction = monsterFluDailyHpLossFraction;
            MonsterFluInitialHpFraction = 0.5;
            BreedingSeasonOrdinaryIvWeight = 1;
            BreedingSeasonRareIvGradeCount = 2;
            if (monsterSiegeDurationMinutes <= 0 || monsterSiegeMinimumRank < 1 || monsterSiegeMinimumRank > 5 ||
                monsterSiegeEnemyHp <= 0 || monsterSiegeVictoryGold < 0 ||
                worldBossDurationMinutes <= 0 || worldBossMinimumRank < 1 || worldBossMinimumRank > 5 ||
                worldBossHp <= 0 || worldBossActivationGold < 0 || worldBossCooldownMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(monsterSiegeDurationMinutes));
            if (double.IsNaN(monsterSiegeEnemyAttack) || monsterSiegeEnemyAttack < 0 ||
                double.IsNaN(monsterSiegeEnemyDefense) || monsterSiegeEnemyDefense < 0 ||
                double.IsNaN(monsterSiegeEnemyAttackSpeed) || monsterSiegeEnemyAttackSpeed <= 0 ||
                double.IsNaN(worldBossAttack) || worldBossAttack < 0 ||
                double.IsNaN(worldBossDefense) || worldBossDefense < 0 ||
                double.IsNaN(worldBossAttackSpeed) || worldBossAttackSpeed <= 0 ||
                !UnitInterval(monsterSiegeBuildingDamageProbability) || !UnitInterval(monsterSiegeMinimumWinFraction) ||
                !UnitInterval(worldBossBuildingDamageProbability) || worldBossCrystalCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(monsterSiegeEnemyAttack));
            MonsterSiegeDurationMinutes=monsterSiegeDurationMinutes; MonsterSiegeMinimumRank=monsterSiegeMinimumRank; MonsterSiegeEnemyHp=monsterSiegeEnemyHp;
            MonsterSiegeEnemyAttack=monsterSiegeEnemyAttack; MonsterSiegeEnemyDefense=monsterSiegeEnemyDefense; MonsterSiegeEnemyAttackSpeed=monsterSiegeEnemyAttackSpeed;
            MonsterSiegeBuildingDamageProbability=monsterSiegeBuildingDamageProbability; MonsterSiegeVictoryGold=monsterSiegeVictoryGold;
            MonsterSiegeMinimumWinFraction=monsterSiegeMinimumWinFraction;
            WorldBossDurationMinutes=worldBossDurationMinutes; WorldBossMinimumRank=worldBossMinimumRank;
            WorldBossHp=worldBossHp; WorldBossAttack=worldBossAttack; WorldBossDefense=worldBossDefense;
            WorldBossAttackSpeed=worldBossAttackSpeed;
            WorldBossActivationGold=worldBossActivationGold; WorldBossCooldownMinutes=worldBossCooldownMinutes;
            WorldBossBuildingDamageProbability=worldBossBuildingDamageProbability; WorldBossCrystalCount=worldBossCrystalCount;
            BalanceParameters = Array.AsReadOnly(new[] {
                P("random_crisis.check_interval_minutes", RandomCrisisCheckIntervalMinutes, "in-game minutes", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1B/1D: random event cadence unspecified."),
                P("random_crisis.probability_per_check", RandomCrisisProbability, "probability/check", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1B/1D: random event chance unspecified."),
                P("black_friday.duration_days", blackFridayDays, "in-game days", "Locked", "docs/designs/06_Events_PVE_PVP.md §1A: last 3 in-game days before Payday."),
                P("black_friday.impulse_units_per_product", blackFridayImpulseUnits, "units/Trainer/product", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1A: impulsive buying; quantity is unspecified."),
                P("black_friday.junk_gear_price", blackFridayJunkGearPrice, "Gold/gear item", "Prototype", "docs/designs/05_Itemization_Gear_System.md §1 and docs/designs/06_Events_PVE_PVP.md §1A: impulse gear/utility buying; price unspecified."),
                P("labor_inspection.tax_threshold", inspectionTaxThreshold, "fraction", "Locked", "docs/designs/06_Events_PVE_PVP.md §1B: transaction tax >30%."),
                P("labor_inspection.stress_threshold", inspectionStressThreshold, "stress points", "Locked", "docs/designs/06_Events_PVE_PVP.md §1B and docs/designs/02_HUB_Economy_Infrastructure.md §1.5: Stress >=80."),
                P("labor_inspection.fine_fraction", inspectionFineFraction, "fraction of Treasury", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1B: penalty is unspecified."),
                P("labor_inspection.cooldown_minutes", inspectionCooldownMinutes, "minutes", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1B: inspection cadence is unspecified.")
                ,P("labor_inspection.resolution_minutes", inspectionResolutionMinutes, "minutes", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1B: response window is unspecified."),
                P("breeding_season.duration_days", breedingSeasonDurationDays, "in-game days", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1C: season duration is unspecified."),
                P("breeding_season.rare_iv_weight_multiplier", breedingSeasonRareIvWeightMultiplier, "multiplier", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1C: rare gene increase is unquantified."),
                P("breeding_season.capture_goods_demand_multiplier", breedingSeasonDemandMultiplier, "quantity multiplier", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1C: demand rises; amount unspecified."),
                P("breeding_season.capture_goods_price_cap_multiplier", BreedingSeasonPriceCapMultiplier, "maximum price multiplier", "Locked", "docs/designs/06_Events_PVE_PVP.md §1C: Manager may set capture ball/trap prices to 3x."),
                P("monster_flu.duration_days", monsterFluDurationDays, "in-game days", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1D: disease duration is unspecified."),
                P("monster_flu.daily_hp_loss_fraction", monsterFluDailyHpLossFraction, "fraction of max HP/day", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1D: ongoing HP loss rate is unspecified."),
                P("monster_flu.initial_hp_fraction", MonsterFluInitialHpFraction, "fraction of current HP", "Locked", "docs/designs/06_Events_PVE_PVP.md §1D: Monsters lose 50% HP at flu onset."),
                P("monster_flu.vaccine_units_to_cure", MonsterFluVaccineUnitsToCure, "vaccine units/outbreak", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1D: vaccine can cut the epidemic; usage quantity unspecified."),
                P("monster_flu.hospital_revenue_bonus_per_day", MonsterFluHospitalRevenueBonus, "Gold/day", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1D: Hospital shares rise; revenue signal amount unspecified."),
                P("breeding_season.ordinary_iv_weight", BreedingSeasonOrdinaryIvWeight, "weight/grade", "Prototype", "src/Game.Domain/Hub/HubWorld.Events.cs: equal ordinary-grade baseline; weighting model provisional."),
                P("breeding_season.rare_iv_grade_count", BreedingSeasonRareIvGradeCount, "grades", "Locked", "docs/designs/06_Events_PVE_PVP.md §1C: rare grades named are S and SS."),
                P("monster_siege.duration_minutes", monsterSiegeDurationMinutes, "in-game minutes", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1E: duration unspecified."),
                P("monster_siege.minimum_rank", monsterSiegeMinimumRank, "Trainer rank", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1E: defender eligibility unspecified."),
                P("monster_siege.enemy_hp", monsterSiegeEnemyHp, "HP", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1E: enemy scaling unspecified."),
                P("monster_siege.enemy_attack", monsterSiegeEnemyAttack, "ATK", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1E: enemy scaling unspecified."),
                P("monster_siege.enemy_defense", monsterSiegeEnemyDefense, "DEF", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1E: enemy scaling unspecified."),
                P("monster_siege.enemy_attack_speed", monsterSiegeEnemyAttackSpeed, "attacks/round", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1E: enemy scaling unspecified."),
                P("monster_siege.building_damage_probability", monsterSiegeBuildingDamageProbability, "probability on loss", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.6: damage may occur; probability unspecified."),
                P("monster_siege.victory_gold", monsterSiegeVictoryGold, "Gold/winning Trainer", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1E: victory Gold unspecified."),
                P("monster_siege.boss_core_count", MonsterSiegeBossCoreCount, "Boss Cores/winning Trainer", "Prototype", "docs/designs/06_Events_PVE_PVP.md §2: Siege victory grants a valuable Boss Core; quantity unspecified."),
                P("monster_siege.minimum_win_fraction", monsterSiegeMinimumWinFraction, "fraction", "Prototype", "docs/designs/06_Events_PVE_PVP.md §1E: group victory rule unspecified."),
                P("world_boss.duration_minutes", worldBossDurationMinutes, "in-game minutes", "Prototype", "docs/designs/01_World_Map_Environment.md §25: duration unspecified."),
                P("world_boss.minimum_rank", worldBossMinimumRank, "Trainer rank", "Prototype", "docs/designs/01_World_Map_Environment.md §25: sufficient Rank requirement lacks threshold."),
                P("world_boss.hp", worldBossHp, "HP", "Prototype", "docs/designs/01_World_Map_Environment.md §25: Boss combat stats unspecified."),
                P("world_boss.attack", worldBossAttack, "ATK", "Prototype", "docs/designs/01_World_Map_Environment.md §25: Boss combat stats unspecified."),
                P("world_boss.defense", worldBossDefense, "DEF", "Prototype", "docs/designs/01_World_Map_Environment.md §25: Boss combat stats unspecified."),
                P("world_boss.attack_speed", worldBossAttackSpeed, "attacks/round", "Prototype", "docs/designs/01_World_Map_Environment.md §25: Boss combat stats unspecified."),
                P("world_boss.activation_gold", worldBossActivationGold, "Gold", "Prototype", "docs/designs/01_World_Map_Environment.md §25: activation fee amount unspecified."),
                P("world_boss.cooldown_minutes", worldBossCooldownMinutes, "in-game minutes", "Prototype", "docs/designs/01_World_Map_Environment.md §25: cooldown duration unspecified."),
                P("world_boss.building_damage_probability", worldBossBuildingDamageProbability, "probability on loss", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.6: Boss may damage a building; chance unspecified."),
                P("world_boss.crystal_count", worldBossCrystalCount, "crystals/winning Trainer", "Prototype", "docs/designs/01_World_Map_Environment.md §25: Boss is the sole source; quantity unspecified.")
            });
        }

        static BalanceParameter P(string id, double value, string unit, string status, string source)
            => new BalanceParameter("events." + id, value, unit, status, source);

        static bool UnitInterval(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 1;
    }

    public static class HubEventCalendar
    {
        public static bool IsBlackFriday(int minute, int paydayMinute, HubEventConfig config = null)
        {
            int days = (config ?? HubEventConfig.Prototype).BlackFridayDays;
            long start = (long)paydayMinute - (long)days * SimClock.MinutesPerDay;
            return minute >= start && minute < paydayMinute;
        }
    }
}
