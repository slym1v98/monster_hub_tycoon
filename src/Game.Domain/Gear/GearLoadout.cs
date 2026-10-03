using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Gear
{
    /// <summary>Bộ trang bị của một chủ sở hữu (Trainer hoặc từng Monster). Mỗi slot chứa tối đa một món.</summary>
    public sealed class GearLoadout
    {
        readonly SortedDictionary<string, GearItem> items = new SortedDictionary<string, GearItem>(StringComparer.Ordinal);
        public IReadOnlyList<GearItem> Equipped => items.Values.ToList().AsReadOnly();

        public GearItem Get(string slotId) => items.TryGetValue(slotId, out var item) ? item : null;

        public bool TryGet(string slotId, out GearItem item) => items.TryGetValue(slotId, out item);

        public void Equip(GearItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (items.ContainsKey(item.Slot.Id)) throw new InvalidOperationException("Slot đã có món: " + item.Slot.Id);
            items[item.Slot.Id] = item;
        }

        /// <summary>Tháo món khỏi slot và trả về; slot trống trả null.</summary>
        public GearItem Unequip(string slotId) => items.Remove(slotId, out var item) ? item : null;

        /// <summary>Tổng chỉ số hiệu dụng của mọi món không vỡ, chưa gồm set bonus.</summary>
        public static GearStats TotalStats(IEnumerable<GearItem> equipped, GearCatalog catalog)
        {
            var total = GearStats.Zero;
            foreach (var item in equipped ?? Array.Empty<GearItem>()) total = total.Add(GearScore.EffectiveStats(item, catalog));
            return total;
        }

        /// <summary>Set bonus cộng dồn theo số món Active (chưa vỡ) cùng SetId; trả chỉ số bonus tổng.</summary>
        public static GearStats SetBonus(IEnumerable<GearItem> equipped, GearCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var active = (equipped ?? Array.Empty<GearItem>()).Where(x => !x.IsBroken).ToList();
            var result = GearStats.Zero;
            foreach (var group in active.Where(x => !string.IsNullOrEmpty(x.SetId)).GroupBy(x => x.SetId, StringComparer.Ordinal))
            {
                var set = catalog.GetSet(group.Key);
                if (set == null) continue;
                int count = group.Count();
                foreach (var bonus in set.Bonuses)
                    if (count >= bonus.RequiredCount) result = result.Add(bonus.Bonus);
            }
            return result;
        }
    }
}
