using System;

namespace Game.Domain.Materials
{
    /// <summary>Nhóm nguyên liệu thô theo GDD 12.</summary>
    public enum MaterialFamily { Ore = 0, Wood = 1, ClothLeather = 2, Gem = 3, Herb = 4, Food = 5 }

    /// <summary>Mã ổn định của một nguyên liệu; định dạng family_tier_N.</summary>
    public readonly struct MaterialId : IEquatable<MaterialId>
    {
        public string Value { get; }
        public MaterialId(string value) { Value = value ?? throw new ArgumentNullException(nameof(value)); }
        public static MaterialId For(MaterialFamily family, int tier)
        {
            ValidateFamily(family);
            if (tier < 1 || tier > 5) throw new ArgumentOutOfRangeException(nameof(tier));
            return new MaterialId($"{FamilyKey(family)}_tier_{tier}");
        }
        internal static bool IsValidFamily(MaterialFamily family) => Enum.IsDefined(typeof(MaterialFamily), family);
        internal static string FamilyKey(MaterialFamily family)
        {
            switch (family)
            {
                case MaterialFamily.Ore: return "ore";
                case MaterialFamily.Wood: return "wood";
                case MaterialFamily.ClothLeather: return "cloth_leather";
                case MaterialFamily.Gem: return "gem";
                case MaterialFamily.Herb: return "herb";
                case MaterialFamily.Food: return "food";
                default: throw new ArgumentOutOfRangeException(nameof(family));
            }
        }
        private static void ValidateFamily(MaterialFamily family)
        { if (!IsValidFamily(family)) throw new ArgumentOutOfRangeException(nameof(family)); }
        public bool Equals(MaterialId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MaterialId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(MaterialId left, MaterialId right) => left.Equals(right);
        public static bool operator !=(MaterialId left, MaterialId right) => !left.Equals(right);
    }

    /// <summary>Dữ liệu định nghĩa một nguyên liệu thô. Giá và khối lượng chờ cân bằng.</summary>
    public sealed class MaterialDefinition
    {
        public MaterialId Id { get; }
        public MaterialFamily Family { get; }
        public int Tier { get; }
        public string Name { get; }
        public decimal? ReferencePrice { get; }
        public decimal? BaseWeight { get; }

        public MaterialDefinition(MaterialId id, MaterialFamily family, int tier, string name,
            decimal? referencePrice = null, decimal? baseWeight = null)
        { Id = id; Family = family; Tier = tier; Name = name; ReferencePrice = referencePrice; BaseWeight = baseWeight; }
    }
}
