using System;
using System.Linq;

namespace Game.Domain.Gear
{
    /// <summary>Hiệu ứng trang bị Trainer lên hành vi mô phỏng: sức chứa balo, nhìn đêm và giảm tụt nhu cầu.</summary>
    public static class GearEffects
    {
        public static GearStats AuraStats(Game.Domain.Trainer t, GearCatalog catalog)
        {
            if (t == null || catalog == null || t.Gear.Equipped.Count == 0) return GearStats.Zero;
            var aura = t.Gear.Equipped.Where(x => x.Slot.Group == GearGroup.Aura).ToArray();
            return GearLoadout.TotalStats(aura, catalog).Add(GearLoadout.SetBonus(aura, catalog));
        }

        public static GearStats TrainerStats(Game.Domain.Trainer t, GearCatalog catalog)
        {
            if (t == null || catalog == null || t.Gear.Equipped.Count == 0) return GearStats.Zero;
            return GearLoadout.TotalStats(t.Gear.Equipped, catalog).Add(GearLoadout.SetBonus(t.Gear.Equipped, catalog));
        }

        public static int BackpackCapacity(Game.Domain.Trainer t, GearCatalog catalog, int baseCapacity)
            => baseCapacity + (int)Math.Floor(TrainerStats(t, catalog).BackpackCapacity);

        public static bool HasNightVision(Game.Domain.Trainer t, GearCatalog catalog, bool flagFromConfig)
            => flagFromConfig || TrainerStats(t, catalog).NightVision;

        /// <summary>Hệ số tụt Thể lực/giờ khi farm sau khi trừ trang bị Nón; không âm.</summary>
        public static double StaminaDecayPerHour(Game.Domain.Trainer t, GearCatalog catalog, double fieldDecayPerHour)
            => Math.Max(0, fieldDecayPerHour - TrainerStats(t, catalog).StaminaDecayReduction);

        public static double HydrationDecayPerHour(Game.Domain.Trainer t, GearCatalog catalog, double fieldDecayPerHour)
            => Math.Max(0, fieldDecayPerHour - TrainerStats(t, catalog).HydrationDecayReduction);
    }
}
