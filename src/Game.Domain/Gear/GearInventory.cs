using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Gear
{
    /// <summary>Kho trang bị dự trữ của Trainer; giữ vật phẩm hiến tế cho Nâng Sao.</summary>
    public sealed class GearInventory
    {
        readonly SortedDictionary<string, GearItem> items = new SortedDictionary<string, GearItem>(StringComparer.Ordinal);
        public IReadOnlyList<GearItem> Items => Array.AsReadOnly(items.Values.ToArray());
        public GearItem Get(string itemId) => itemId != null && items.TryGetValue(itemId, out var item) ? item : null;

        internal bool TryAdd(GearItem item)
        {
            if (item == null || item.IsDestroyed || items.ContainsKey(item.Id)) return false;
            items.Add(item.Id, item);
            return true;
        }

        internal bool TryRemove(string itemId, out GearItem item) => items.Remove(itemId, out item);
    }
}
