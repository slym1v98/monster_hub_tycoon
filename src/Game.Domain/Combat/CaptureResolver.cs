using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    public sealed class CaptureConfig
    {
        public static CaptureConfig Prototype { get; } = new CaptureConfig();
        public double BaseChance { get; }
        public double HpDepletionBonus { get; }
        public double BallTierBonus { get; }
        public double TrapTierBonus { get; }
        public double DexterityScale { get; }
        public double TrapperBonus { get; }
        public double RarityPenalty { get; }
        public double MinimumChance { get; }
        public double MaximumChance { get; }
        public double WeakHpFraction { get; }
        public bool ConsumeTrapOnFailure { get; }
        public double PowerHpWeight { get; }
        public double PowerAttackWeight { get; }
        public double PowerDefenseWeight { get; }
        public double PowerAttackSpeedWeight { get; }
        public double PowerCriticalWeight { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public CaptureConfig(double baseChance = 0.15, double hpDepletionBonus = 0.5,
            double ballTierBonus = 0.05, double trapTierBonus = 0.08, double dexterityScale = 0.005,
            double trapperBonus = 0.15, double rarityPenalty = 0.025, double minimumChance = 0.05,
            double maximumChance = 0.95, double weakHpFraction = 0.3, bool consumeTrapOnFailure = false,
            double powerHpWeight = 1, double powerAttackWeight = 10, double powerDefenseWeight = 8,
            double powerAttackSpeedWeight = 20, double powerCriticalWeight = 100)
        {
            foreach (var value in new[] { baseChance, hpDepletionBonus, ballTierBonus, trapTierBonus,
                dexterityScale, trapperBonus, rarityPenalty, minimumChance, maximumChance, weakHpFraction,
                powerHpWeight, powerAttackWeight, powerDefenseWeight, powerAttackSpeedWeight, powerCriticalWeight })
                ZoneDefinition.ValidateNonNegativeFinite(value, nameof(baseChance));
            if (minimumChance > maximumChance || maximumChance > 1) throw new ArgumentOutOfRangeException(nameof(maximumChance));
            if (weakHpFraction > 1) throw new ArgumentOutOfRangeException(nameof(weakHpFraction));
            if (powerHpWeight + powerAttackWeight + powerDefenseWeight + powerAttackSpeedWeight + powerCriticalWeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(powerHpWeight));
            BaseChance = baseChance; HpDepletionBonus = hpDepletionBonus; BallTierBonus = ballTierBonus;
            TrapTierBonus = trapTierBonus; DexterityScale = dexterityScale; TrapperBonus = trapperBonus;
            RarityPenalty = rarityPenalty; MinimumChance = minimumChance; MaximumChance = maximumChance;
            WeakHpFraction = weakHpFraction; ConsumeTrapOnFailure = consumeTrapOnFailure;
            PowerHpWeight = powerHpWeight; PowerAttackWeight = powerAttackWeight; PowerDefenseWeight = powerDefenseWeight;
            PowerAttackSpeedWeight = powerAttackSpeedWeight; PowerCriticalWeight = powerCriticalWeight;
            BalanceParameters = Array.AsReadOnly(new[] {
                P("base_chance", baseChance, "probability"), P("hp_depletion_bonus", hpDepletionBonus, "probability"),
                P("ball_tier_bonus", ballTierBonus, "probability/tier"), P("trap_tier_bonus", trapTierBonus, "probability/tier"),
                P("dexterity_scale", dexterityScale, "probability/point"), P("trapper_bonus", trapperBonus, "probability"),
                P("rarity_penalty", rarityPenalty, "probability/rarity"), P("minimum_chance", minimumChance, "probability"),
                P("maximum_chance", maximumChance, "probability"), P("weak_hp_fraction", weakHpFraction, "HP fraction"),
                P("power_hp_weight", powerHpWeight, "power/HP"), P("power_attack_weight", powerAttackWeight, "power/ATK"),
                P("power_defense_weight", powerDefenseWeight, "power/DEF"), P("power_attack_speed_weight", powerAttackSpeedWeight, "power/ASPD"),
                P("power_critical_weight", powerCriticalWeight, "power/critical")
            });
        }
        static BalanceParameter P(string name, double value, string unit) => new BalanceParameter(
            "capture." + name, value, unit, "Prototype", "docs/designs/04_Monster_System.md: Capture chance/replacement formula is not specified.");
    }

    public sealed class ReplacementScore
    {
        public MonsterId MonsterId { get; }
        public Rarity Rarity { get; }
        public double Power { get; }
        internal ReplacementScore(MonsterId id, Rarity rarity, double power) { MonsterId = id; Rarity = rarity; Power = power; }
    }

    public static class CaptureResolver
    {
        static readonly ProductId Ball = new ProductId("capture_ball");

        public static ReplacementScore WeakestMember(MonsterRoster roster, CaptureConfig config)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (config == null) throw new ArgumentNullException(nameof(config));
            var weakest = roster.Members.Select(x => new ReplacementScore(x.Id, x.Rarity, Power(x.Stats, config)))
                .OrderBy(x => x.Power).ThenBy(x => x.MonsterId.Value, StringComparer.Ordinal).FirstOrDefault();
            return weakest;
        }

        public static bool ShouldAttempt(MonsterDefinition target, Rarity targetRarity, int targetLevel, long targetCurrentHp,
            long targetMaxHp, MonsterRoster roster, TrainerAttributes trainer, TrainerInventory inventory,
            CaptureConfig config)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.Validate();
            if (!Enum.IsDefined(typeof(Rarity), targetRarity)) throw new ArgumentOutOfRangeException(nameof(targetRarity));
            if (targetLevel < 1 || targetLevel > 100) throw new ArgumentOutOfRangeException(nameof(targetLevel));
            if (targetMaxHp <= 0 || targetCurrentHp < 0 || targetCurrentHp > targetMaxHp) throw new ArgumentOutOfRangeException(nameof(targetCurrentHp));
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (inventory.Count(Ball) <= 0 || targetCurrentHp / (double)targetMaxHp > config.WeakHpFraction) return false;
            var weakest = WeakestMember(roster, config);
            if (weakest == null) return true;
            var candidate = MonsterStatsCalculator.Calculate(target, targetRarity, MonsterIvGrade.B, targetLevel, MonsterStatConfig.Prototype);
            double candidatePower = Power(candidate, config);
            return targetRarity > weakest.Rarity || candidatePower > weakest.Power;
        }

        public static double CalculateChance(MonsterSnapshot target, CaptureInputs inputs, CaptureConfig config)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (config == null) throw new ArgumentNullException(nameof(config));
            double hpFraction = target.CurrentHp / (double)target.Stats.Hp;
            double trapBonus = inputs.AvailableTraps > 0 ? inputs.TrapTier * config.TrapTierBonus : 0;
            double raw = config.BaseChance + (1 - hpFraction) * config.HpDepletionBonus
                + inputs.BallTier * config.BallTierBonus + trapBonus + inputs.Dexterity * config.DexterityScale
                + (inputs.TrainerClass == TrainerClass.Trapper ? config.TrapperBonus : 0)
                - (int)inputs.TargetRarity * config.RarityPenalty;
            return Math.Max(config.MinimumChance, Math.Min(config.MaximumChance, raw));
        }

        public static CaptureResult Resolve(MonsterSnapshot target, CaptureInputs inputs, CaptureConfig config, SimRandom random)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (random == null) throw new ArgumentNullException(nameof(random));
            double hpFraction = target.CurrentHp / (double)target.Stats.Hp;
            bool attempted = inputs.AvailableBalls > 0 && hpFraction <= config.WeakHpFraction && inputs.ReplacementEligible;
            if (!attempted) return new CaptureResult(false, false, 0, null, hpFraction, inputs, 0, 0, null);
            double chance = CalculateChance(target, inputs, config);
            double roll = random.NextDouble();
            bool success = roll < chance;
            int trapsUsed = inputs.AvailableTraps > 0 && (success || config.ConsumeTrapOnFailure) ? 1 : 0;
            CapturedMonsterGeneration generation = null;
            if (success)
            {
                var iv = (MonsterIvGrade)random.NextInt(7);
                int seed = random.NextInt(int.MaxValue);
                generation = new CapturedMonsterGeneration(inputs.SpeciesId, target.Element, inputs.TargetRarity,
                    inputs.TargetLevel, iv, seed);
            }
            return new CaptureResult(true, success, chance, roll, hpFraction, inputs, 1, trapsUsed, generation);
        }

        static double Power(MonsterStats stats, CaptureConfig config)
            => stats.Hp * config.PowerHpWeight + stats.Attack * config.PowerAttackWeight
                + stats.Defense * config.PowerDefenseWeight + stats.AttackSpeed * config.PowerAttackSpeedWeight
                + stats.CriticalChance * config.PowerCriticalWeight;
    }
}
