using System;
using Game.Domain.Production;

namespace Game.Domain.Materials
{
    public enum ProductEffectKind { None, TemporaryMonsterStatBuff }

    /// <summary>Dữ liệu định nghĩa sản phẩm do xưởng tạo ra.</summary>
    public sealed class ProductDefinition
    {
        public ProductId Id { get; }
        public string Name { get; }
        public ProducerId? Producer { get; }
        public string Unit { get; }
        public decimal? ReferencePrice { get; }
        public ProductEffectKind EffectKind { get; }
        public ProductDefinition(ProductId id, string name, ProducerId? producer = null, string unit = "đơn vị",
            decimal? referencePrice = null, ProductEffectKind effectKind = ProductEffectKind.None)
        {
            if (string.IsNullOrWhiteSpace(id.Value)) throw new ArgumentException("Product ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.", nameof(name));
            if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Product unit is required.", nameof(unit));
            if (producer.HasValue && string.IsNullOrWhiteSpace(producer.Value.Value)) throw new ArgumentException("Producer ID is invalid.", nameof(producer));
            if (referencePrice.HasValue && referencePrice.Value < 0) throw new ArgumentOutOfRangeException(nameof(referencePrice));
            if (!Enum.IsDefined(typeof(ProductEffectKind), effectKind)) throw new ArgumentOutOfRangeException(nameof(effectKind));
            Id = id; Name = name; Producer = producer; Unit = unit; ReferencePrice = referencePrice; EffectKind = effectKind;
        }
    }
}
