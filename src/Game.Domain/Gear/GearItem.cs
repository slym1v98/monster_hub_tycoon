using System;

namespace Game.Domain.Gear
{
    /// <summary>Giá trị chỉ số của một món trang bị; cộng dồn theo từng trường.</summary>
    public sealed record GearStats(
        double Attack = 0, double Defense = 0, double Hp = 0, double AttackSpeed = 0, double CriticalChance = 0,
        double BackpackCapacity = 0, bool NightVision = false, double HydrationDecayReduction = 0, double MiningSpeed = 0,
        double AuraAttackMultiplier = 1, double AuraDefenseMultiplier = 1, double AuraCritChance = 0,
        double StaminaDecayReduction = 0, double MoveSpeedMultiplier = 1, double WeatherResist = 0)
    {
        public static GearStats Zero { get; } = new GearStats();
        public GearStats Add(GearStats other)
        {
            if (other == null) return this;
            return new GearStats(
                Attack + other.Attack, Defense + other.Defense, Hp + other.Hp,
                AttackSpeed + other.AttackSpeed, CriticalChance + other.CriticalChance,
                BackpackCapacity + other.BackpackCapacity,
                NightVision || other.NightVision,
                HydrationDecayReduction + other.HydrationDecayReduction,
                MiningSpeed + other.MiningSpeed,
                AuraAttackMultiplier + (other.AuraAttackMultiplier - 1),
                AuraDefenseMultiplier + (other.AuraDefenseMultiplier - 1),
                AuraCritChance + other.AuraCritChance,
                StaminaDecayReduction + other.StaminaDecayReduction,
                MoveSpeedMultiplier + (other.MoveSpeedMultiplier - 1),
                WeatherResist + other.WeatherResist);
        }
    }

    /// <summary>
    /// Một món trang bị. Tier 1..5, Enhance 0..+20, Stars 1..5, Refine 0..4, Durability trong [0, MaxDurability].
    /// Durability = 0 nghĩa là món vô hiệu, không đóng góp chỉ số.
    /// </summary>
    public sealed class GearItem
    {
        public string Id { get; }
        public GearSlot Slot { get; }
        public int Tier { get; }
        public int EnhanceLevel { get; internal set; }
        public int Stars { get; internal set; }
        public GearRefineGrade Refine { get; internal set; }
        public int Durability { get; internal set; }
        public int MaxDurability { get; }
        public string SetId { get; }
        public bool IsBroken => Durability == 0 || IsDestroyed;
        /// <summary>Món đã vỡ khi Cường hóa hoặc bị hiến tế; không dùng lại được.</summary>
        public bool IsDestroyed { get; internal set; }

        public GearItem(string id, GearSlot slot, int tier, int maxDurability, string setId = null,
            int enhanceLevel = 0, int stars = 1, GearRefineGrade refine = GearRefineGrade.Normal, int durability = -1)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Item ID is required.", nameof(id));
            if (slot == null) throw new ArgumentNullException(nameof(slot));
            if (tier < 1 || tier > 5) throw new ArgumentOutOfRangeException(nameof(tier));
            if (maxDurability <= 0) throw new ArgumentOutOfRangeException(nameof(maxDurability));
            if (enhanceLevel < 0 || enhanceLevel > 20) throw new ArgumentOutOfRangeException(nameof(enhanceLevel));
            if (stars < 1 || stars > 5) throw new ArgumentOutOfRangeException(nameof(stars));
            if (!Enum.IsDefined(typeof(GearRefineGrade), refine)) throw new ArgumentOutOfRangeException(nameof(refine));
            if (durability < 0) durability = maxDurability;
            if (durability > maxDurability) throw new ArgumentOutOfRangeException(nameof(durability));
            Id = id; Slot = slot; Tier = tier; MaxDurability = maxDurability; SetId = setId;
            EnhanceLevel = enhanceLevel; Stars = stars; Refine = refine; Durability = durability;
        }
    }
}
