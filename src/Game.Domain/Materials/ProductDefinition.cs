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
        { Id = id; Name = name; Producer = producer; Unit = unit; ReferencePrice = referencePrice; EffectKind = effectKind; }
    }
}
