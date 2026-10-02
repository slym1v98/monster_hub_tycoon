using System;

namespace Game.Domain.Materials
{
    /// <summary>Mã định danh ổn định của sản phẩm.</summary>
    public readonly struct ProductId : IEquatable<ProductId>
    {
        public string Value { get; }
        public ProductId(string value) { Value = value ?? throw new ArgumentNullException(nameof(value)); }
        public bool Equals(ProductId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ProductId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(ProductId left, ProductId right) => left.Equals(right);
        public static bool operator !=(ProductId left, ProductId right) => !left.Equals(right);
    }

    /// <summary>Mã định danh ổn định của công thức.</summary>
    public readonly struct RecipeId : IEquatable<RecipeId>
    {
        public string Value { get; }
        public RecipeId(string value) { Value = value ?? throw new ArgumentNullException(nameof(value)); }
        public bool Equals(RecipeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RecipeId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(RecipeId left, RecipeId right) => left.Equals(right);
        public static bool operator !=(RecipeId left, RecipeId right) => !left.Equals(right);
    }

    /// <summary>Mã định danh ổn định của xưởng sản xuất.</summary>
    public readonly struct ProducerId : IEquatable<ProducerId>
    {
        public string Value { get; }
        public ProducerId(string value) { Value = value ?? throw new ArgumentNullException(nameof(value)); }
        public bool Equals(ProducerId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ProducerId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(ProducerId left, ProducerId right) => left.Equals(right);
        public static bool operator !=(ProducerId left, ProducerId right) => !left.Equals(right);
    }
}
