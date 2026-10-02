using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Gear
{
    /// <summary>Điểm trang bị và chỉ số hiệu dụng của một món, theo cường hóa/sao/tinh luyện.</summary>
    public static class GearScore
    {
        /// <summary>
        /// Chỉ số hiệu dụng: Base[tier] × (1 + Enhance × pct) × (1 + StarPct) × RefineMult; vô hiệu khi độ bền 0.
        /// </summary>
        public static GearStats EffectiveStats(GearItem item, GearCatalog catalog)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (item.IsBroken) return GearStats.Zero;
            var baseStats = catalog.GetBaseStats(item.Slot.Id, item.Tier);
            double enhance = 1 + item.EnhanceLevel * catalog.Config.EnhancePerLevelFraction;
            double star = 1 + catalog.GetStarPct(item.Stars);
            double refine = catalog.GetRefineMultiplier((int)item.Refine);
            double factor = enhance * star * refine;
            return Scale(baseStats, factor);
        }

        public static double Score(GearItem item, GearCatalog catalog)
        {
            var s = EffectiveStats(item, catalog);
            // Tổng quy ước: ATK/DEF/HP/ASPD/CRIT + các hiệu ứng utility (đơn vị quy đổi prototype).
            return s.Attack + s.Defense + s.Hp / 10.0 + s.AttackSpeed * 100 + s.CriticalChance * 100
                + s.BackpackCapacity + s.HydrationDecayReduction * 100 + s.MiningSpeed * 100
                + (s.AuraAttackMultiplier - 1) * 100 + (s.AuraDefenseMultiplier - 1) * 100 + s.AuraCritChance * 100
                + s.StaminaDecayReduction * 100 + (s.MoveSpeedMultiplier - 1) * 100 + s.WeatherResist * 100
                + (s.NightVision ? 25 : 0);
        }

        static GearStats Scale(GearStats s, double factor) => new GearStats(
            s.Attack * factor, s.Defense * factor, s.Hp * factor, s.AttackSpeed * factor, s.CriticalChance * factor,
            s.BackpackCapacity * factor, s.NightVision, s.HydrationDecayReduction * factor, s.MiningSpeed * factor,
            1 + (s.AuraAttackMultiplier - 1) * factor, 1 + (s.AuraDefenseMultiplier - 1) * factor, s.AuraCritChance * factor,
            s.StaminaDecayReduction * factor, 1 + (s.MoveSpeedMultiplier - 1) * factor, s.WeatherResist * factor);
    }
}
