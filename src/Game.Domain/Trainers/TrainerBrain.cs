using System.Linq;

namespace Game.Domain
{
    /// <summary>Các quyết định thuần (không có trạng thái): có nên về HUB không, và nên dùng dịch vụ nào.</summary>
    public static class TrainerBrain
    {
        /// <summary>
        /// Trả về lý do về HUB (điều kiện đầu tiên gặp), hoặc <see cref="ReturnReason.None"/> nếu tiếp tục farm.
        /// Thứ tự: đình công, ban đêm không kính, Monster cạn HP, Balo đầy, rồi các thanh dưới ngưỡng tính cách.
        /// </summary>
        public static ReturnReason ShouldReturn(Trainer t, bool isNight)
        {
            PersonalityProfile p = PersonalityProfile.Of(t.Personality);
            if (t.IsOnStrike) return ReturnReason.Strike;
            if (isNight && !t.HasNightVision) return ReturnReason.Night;
            if (!t.Roster.Members.Any(x => x.CurrentHp > 0)) return ReturnReason.TeamDown;
            if (t.BackpackUnits >= t.BackpackCapacity) return ReturnReason.BackpackFull;
            if (t.Needs.Stamina < p.StaminaThreshold) return ReturnReason.Tired;
            if (t.Needs.Satiety < p.SatietyThreshold) return ReturnReason.Hungry;
            if (t.Needs.Hydration < p.StaminaThreshold) return ReturnReason.Thirsty;
            return ReturnReason.None;
        }

        /// <summary>
        /// Chọn dịch vụ cần dùng ở HUB, hoặc null nếu không cần gì. Thứ tự ưu tiên:
        /// 1) Stress đầy: Bar. 2) Thiếu HP: Bệnh Viện (bỏ qua khi <paramref name="allowHospital"/> = false, ví dụ đang đình công). 3) Ban đêm không kính và hơi mệt: Nhà Trọ.
        /// 4) Thanh thể chất thấp nhất dưới mức "đủ": công trình của thanh đó. 5) Stress cao: Bar.
        /// </summary>
        public static BuildingKind? PickService(Trainer t, SimConfig cfg, bool isNight, bool allowHospital)
        {
            Needs n = t.Needs;
            if (n.Stress >= 100) return BuildingKind.Bar;
            if (allowHospital && t.Roster.TotalMissingHp > 0) return BuildingKind.Hospital;
            if (isNight && !t.HasNightVision && n.Stamina < cfg.NightSleepBelow) return BuildingKind.Inn;
            if (n.LowestPhysical < cfg.SufficientNeed)
            {
                bool staminaIsLowest = n.Stamina <= n.Satiety && n.Stamina <= n.Hydration;
                return staminaIsLowest ? BuildingKind.Inn : BuildingKind.Restaurant;   // No nê và Nước cùng hồi ở Nhà Hàng
            }
            if (n.Stress >= cfg.BarStressThreshold) return BuildingKind.Bar;
            return null;
        }
    }
}
