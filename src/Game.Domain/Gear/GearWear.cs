using System;

namespace Game.Domain.Gear
{
    /// <summary>
    /// Áp dụng hao mòn theo GDD 05:
    /// - Monster Chiến đấu: hao mỗi lần tung chiêu (action) hoặc bị đánh (hit).
    /// - Trainer Tiện ích: hao theo thời gian farm × hệ số thời tiết.
    /// - Hào quang: không hao mòn.
    /// Độ bền không bao giờ xuống dưới 0; về 0 thì món vô hiệu (GearScore = 0).
    /// </summary>
    public static class GearWear
    {
        public static void OnMonsterAction(GearItem item, GearConfig config)
        {
            if (item == null || config == null || item.Slot.Group != GearGroup.MonsterCombat) return;
            Wear(item, config.MonsterWearPerAction);
        }

        public static void OnMonsterHit(GearItem item, GearConfig config)
        {
            if (item == null || config == null || item.Slot.Group != GearGroup.MonsterCombat) return;
            Wear(item, config.MonsterWearPerHit);
        }

        public static void OnFarmMinutes(GearItem item, int minutes, double weatherFactor, GearConfig config)
        {
            if (item == null || config == null || item.Slot.Group != GearGroup.TrainerUtility || minutes <= 0) return;
            if (double.IsNaN(weatherFactor) || double.IsInfinity(weatherFactor) || weatherFactor < 0) throw new ArgumentOutOfRangeException(nameof(weatherFactor));
            double wear = config.UtilityWearPerFarmHour * minutes / 60.0 * weatherFactor;
            Wear(item, (int)Math.Ceiling(wear));
        }

        static void Wear(GearItem item, int amount)
        {
            if (amount <= 0 || item.IsDestroyed) return;
            item.Durability = Math.Max(0, item.Durability - amount);
        }
    }
}
