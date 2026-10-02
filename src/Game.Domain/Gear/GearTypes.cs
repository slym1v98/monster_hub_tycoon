using System;

namespace Game.Domain.Gear
{
    /// <summary>Nhóm slot theo GDD 05: Trainer Tiện ích (Xưởng Dệt), Hào quang (Tiệm Kim Hoàn) và Monster Chiến đấu (Lò Rèn).</summary>
    public enum GearGroup { TrainerUtility = 0, Aura = 1, MonsterCombat = 2 }

    /// <summary>Thang Tinh Luyện: Normal(0) → Mythic(4).</summary>
    public enum GearRefineGrade { Normal = 0, Refined = 1, Rare = 2, Epic = 3, Mythic = 4 }

    /// <summary>Chỉ số của một slot; mỗi slot có một chỉ số chính.</summary>
    public enum GearStatKind
    {
        Attack = 0, Defense = 1, Hp = 2, AttackSpeed = 3, CriticalChance = 4,
        BackpackCapacity = 5, NightVision = 6, HydrationDecayReduction = 7, MiningSpeed = 8,
        AuraAttackMultiplier = 9, AuraDefenseMultiplier = 10, AuraCritChance = 11,
        StaminaDecayReduction = 12, MoveSpeedMultiplier = 13, WeatherResist = 14
    }

    /// <summary>Định nghĩa một slot trang bị (một trong 18 kind).</summary>
    public sealed class GearSlot
    {
        public string Id { get; }
        public GearGroup Group { get; }
        public int Index { get; }
        public string OwnerUnit { get; }
        public GearStatKind StatKind { get; }
        public GearSlot(string id, GearGroup group, int index, string ownerUnit, GearStatKind statKind)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Slot ID is required.", nameof(id));
            if (index < 0 || index > 5) throw new ArgumentOutOfRangeException(nameof(index));
            if (string.IsNullOrWhiteSpace(ownerUnit)) throw new ArgumentException("Owner unit is required.", nameof(ownerUnit));
            if (!Enum.IsDefined(typeof(GearStatKind), statKind)) throw new ArgumentOutOfRangeException(nameof(statKind));
            Id = id; Group = group; Index = index; OwnerUnit = ownerUnit; StatKind = statKind;
        }
    }
}
