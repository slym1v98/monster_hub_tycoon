using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Gear
{
    /// <summary>Điểm trang bị và chỉ số hiệu dụng của một món, theo cường hóa/sao/tinh luyện.</summary>
    public static class GearScore
    {
        /// <summary>
        /// Chỉ số hiệu dụng: (Base[tier] + Enhance × PerLevel) × (1 + StarPct) × RefineMult; vô hiệu khi độ bền 0.
        /// </summary>
        public static GearStats EffectiveStats(GearItem item, GearCatalog catalog)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (item.IsBroken) return GearStats.Zero;
            var baseStats = catalog.GetBaseStats(item.Slot.Id, item.Tier);
            var perLevel = catalog.GetBaseStats(item.Slot.Id, 1);
            baseStats = AddScaled(baseStats, perLevel, item.EnhanceLevel * catalog.Config.EnhancePerLevelFraction);
            double star = 1 + catalog.GetStarPct(item.Stars);
            double refine = catalog.GetRefineMultiplier((int)item.Refine);
            double factor = star * refine;
            return Scale(baseStats, factor);
        }

        public static double Score(GearItem item, GearCatalog catalog)
        {
            var s = EffectiveStats(item, catalog);
            var w = catalog.Config.ScoreWeight;
            return s.Attack * w[GearStatKind.Attack] + s.Defense * w[GearStatKind.Defense] + s.Hp * w[GearStatKind.Hp]
                + s.AttackSpeed * w[GearStatKind.AttackSpeed] + s.CriticalChance * w[GearStatKind.CriticalChance]
                + s.BackpackCapacity * w[GearStatKind.BackpackCapacity] + (s.NightVision ? w[GearStatKind.NightVision] : 0)
                + s.HydrationDecayReduction * w[GearStatKind.HydrationDecayReduction] + s.MiningSpeed * w[GearStatKind.MiningSpeed]
                + (s.AuraAttackMultiplier - 1) * w[GearStatKind.AuraAttackMultiplier]
                + (s.AuraDefenseMultiplier - 1) * w[GearStatKind.AuraDefenseMultiplier]
                + s.AuraCritChance * w[GearStatKind.AuraCritChance] + s.StaminaDecayReduction * w[GearStatKind.StaminaDecayReduction]
                + (s.MoveSpeedMultiplier - 1) * w[GearStatKind.MoveSpeedMultiplier] + s.WeatherResist * w[GearStatKind.WeatherResist];
        }

        static GearStats Scale(GearStats s, double factor) => new GearStats(
            s.Attack * factor, s.Defense * factor, s.Hp * factor, s.AttackSpeed * factor, s.CriticalChance * factor,
            s.BackpackCapacity * factor, s.NightVision, s.HydrationDecayReduction * factor, s.MiningSpeed * factor,
            1 + (s.AuraAttackMultiplier - 1) * factor, 1 + (s.AuraDefenseMultiplier - 1) * factor, s.AuraCritChance * factor,
            s.StaminaDecayReduction * factor, 1 + (s.MoveSpeedMultiplier - 1) * factor, s.WeatherResist * factor);

        static GearStats AddScaled(GearStats target, GearStats source, double factor) => new GearStats(
            target.Attack + source.Attack * factor, target.Defense + source.Defense * factor,
            target.Hp + source.Hp * factor, target.AttackSpeed + source.AttackSpeed * factor,
            target.CriticalChance + source.CriticalChance * factor,
            target.BackpackCapacity + source.BackpackCapacity * factor,
            target.NightVision || source.NightVision,
            target.HydrationDecayReduction + source.HydrationDecayReduction * factor,
            target.MiningSpeed + source.MiningSpeed * factor,
            1 + (target.AuraAttackMultiplier - 1) + (source.AuraAttackMultiplier - 1) * factor,
            1 + (target.AuraDefenseMultiplier - 1) + (source.AuraDefenseMultiplier - 1) * factor,
            target.AuraCritChance + source.AuraCritChance * factor,
            target.StaminaDecayReduction + source.StaminaDecayReduction * factor,
            1 + (target.MoveSpeedMultiplier - 1) + (source.MoveSpeedMultiplier - 1) * factor,
            target.WeatherResist + source.WeatherResist * factor);
    }
}
