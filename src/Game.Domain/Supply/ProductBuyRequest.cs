using System;
using Game.Domain.Materials;

namespace Game.Domain.Supply
{
    public sealed class ProductBuyRequest
    {
        public ProductId Product { get; }
        public int TargetStock { get; }
        public long BidPrice { get; }
        public bool Enabled { get; }
        public ProductBuyRequest(ProductId product, int targetStock, long bidPrice, bool enabled = true)
        {
            if (string.IsNullOrWhiteSpace(product.Value)) throw new ArgumentException("Product ID is required.", nameof(product));
            if (targetStock < 0) throw new ArgumentOutOfRangeException(nameof(targetStock));
            if (bidPrice < 0) throw new ArgumentOutOfRangeException(nameof(bidPrice));
            Product = product; TargetStock = targetStock; BidPrice = bidPrice; Enabled = enabled;
        }
        public int Deficit(int currentStock)
        {
            if (currentStock < 0) throw new ArgumentOutOfRangeException(nameof(currentStock));
            return Enabled ? Math.Max(0, TargetStock - currentStock) : 0;
        }
    }
}
