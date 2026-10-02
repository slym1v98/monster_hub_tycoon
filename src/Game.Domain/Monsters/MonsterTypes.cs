using System;

namespace Game.Domain.Monsters
{
    /// <summary>Chín hệ nguyên tố theo GDD; vai trò chiến đấu là dữ liệu riêng.</summary>
    public enum MonsterElement { Fire, Water, Grass, Electric, Ice, Poison, Ground, Light, Dark }
    public enum MonsterRole { Tank, Dps, Support }
    public enum MonsterIvGrade { D, C, B, A, S, SS, SSS }

    /// <summary>Trạng thái sinh tồn độc lập với vị trí Active/Reserve trong đội.</summary>
    public enum MonsterLifeState { Ready, Fainted, Recovering, Stored }

    /// <summary>Danh tính ổn định, so sánh theo chuỗi ordinal, không dùng hash để sinh seed.</summary>
    public readonly struct MonsterId : IEquatable<MonsterId>
    {
        public string Value { get; }
        public MonsterId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Mã Monster không được rỗng.", nameof(value));
            Value = value;
        }
        public bool Equals(MonsterId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MonsterId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(MonsterId left, MonsterId right) => left.Equals(right);
        public static bool operator !=(MonsterId left, MonsterId right) => !left.Equals(right);
    }
}
