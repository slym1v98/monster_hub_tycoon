namespace Game.Domain
{
    /// <summary>Hệ số hành vi theo tính cách (giá trị khởi điểm, docs/designs/03 và spec §9).</summary>
    public sealed class PersonalityProfile
    {
        /// <summary>Ngưỡng về HUB cho Thể lực và Nước (thanh dưới ngưỡng thì về).</summary>
        public double StaminaThreshold;
        /// <summary>Ngưỡng về HUB cho No nê.</summary>
        public double SatietyThreshold;
        /// <summary>Hệ số lượng loot (Gold và nguyên liệu).</summary>
        public double LootMult;
        /// <summary>Hệ số HP Monster mất mỗi khúc farm.</summary>
        public double HpLossMult;
        /// <summary>Hệ số tốc độ tụt No nê.</summary>
        public double SatietyDecayMult;
        /// <summary>Tỉ lệ giảm giá dịch vụ HUB (0.10 = giảm 10%).</summary>
        public double PriceDiscount;
        /// <summary>Độ nhạy với giá cao: nhân vào Stress cộng thêm.</summary>
        public double PriceSensitivity;
        /// <summary>Tỉ lệ nguyên liệu nhặt (phần còn lại bị bỏ lại).</summary>
        public double MaterialPickRate;

        static readonly PersonalityProfile[] Table =
        {
            // Háo chiến
            new PersonalityProfile { StaminaThreshold = 15, SatietyThreshold = 15, LootMult = 1.25, HpLossMult = 1.5, SatietyDecayMult = 1.0, PriceDiscount = 0.0, PriceSensitivity = 1.0, MaterialPickRate = 0.85 },
            // Nhát gan
            new PersonalityProfile { StaminaThreshold = 50, SatietyThreshold = 50, LootMult = 0.85, HpLossMult = 0.5, SatietyDecayMult = 1.0, PriceDiscount = 0.0, PriceSensitivity = 1.0, MaterialPickRate = 0.85 },
            // Tham ăn
            new PersonalityProfile { StaminaThreshold = 30, SatietyThreshold = 50, LootMult = 1.0, HpLossMult = 1.0, SatietyDecayMult = 1.6, PriceDiscount = 0.0, PriceSensitivity = 1.0, MaterialPickRate = 0.85 },
            // Tư bản
            new PersonalityProfile { StaminaThreshold = 25, SatietyThreshold = 25, LootMult = 1.0, HpLossMult = 1.0, SatietyDecayMult = 1.0, PriceDiscount = 0.10, PriceSensitivity = 1.5, MaterialPickRate = 1.0 },
        };

        public static PersonalityProfile Of(Personality personality) => Table[(int)personality];
    }
}
