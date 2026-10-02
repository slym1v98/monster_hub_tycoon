using System;
using System.Collections.Generic;
using Game.Domain.Materials;

namespace Game.Domain.Supply
{
    /// <summary>Mã vật phẩm dùng chung cho nguyên liệu thô và sản phẩm.</summary>
    public readonly struct InventoryItem : IEquatable<InventoryItem>
    {
        public string Value { get; }
        public InventoryItem(MaterialId id) { Value = "material:" + id.Value; }
        public InventoryItem(ProductId id) { Value = "product:" + id.Value; }
        public bool Equals(InventoryItem other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is InventoryItem other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(InventoryItem left, InventoryItem right) => left.Equals(right);
        public static bool operator !=(InventoryItem left, InventoryItem right) => !left.Equals(right);
    }

    /// <summary>Số lượng có thể dùng, đã giữ chỗ và đang được sản xuất.</summary>
    public sealed record InventoryBalance(int Available, int Reserved, int InProduction);

    /// <summary>Tồn kho có kiểm tra số lượng và giữ chỗ nguyên tử.</summary>
    public sealed class Inventory
    {
        private sealed class MutableBalance
        {
            public int Available;
            public int Reserved;
            public int InProduction;
        }
        private readonly Dictionary<InventoryItem, MutableBalance> balances = new Dictionary<InventoryItem, MutableBalance>();

        public int TotalAvailableUnits
        {
            get
            {
                var total = 0;
                checked { foreach (var balance in balances.Values) total += balance.Available; }
                return total;
            }
        }

        public InventoryBalance Get(InventoryItem item)
        {
            if (!balances.TryGetValue(item, out var b)) return new InventoryBalance(0, 0, 0);
            return new InventoryBalance(b.Available, b.Reserved, b.InProduction);
        }

        public void Add(InventoryItem item, int quantity)
        {
            RequireItem(item); RequirePositive(quantity);
            var b = GetMutable(item);
            var value = checked(b.Available + quantity);
            b.Available = value;
        }

        public void Remove(InventoryItem item, int quantity)
        {
            RequireItem(item); RequirePositive(quantity);
            var b = GetMutable(item);
            if (b.Available < quantity) throw new InvalidOperationException("Không đủ tồn kho khả dụng.");
            b.Available -= quantity;
        }

        public void Reserve(InventoryItem item, int quantity)
        {
            RequireItem(item); RequirePositive(quantity);
            var b = GetMutable(item);
            if (b.Available < quantity) throw new InvalidOperationException("Không đủ tồn kho để giữ chỗ.");
            b.Available -= quantity;
            b.Reserved = checked(b.Reserved + quantity);
        }

        public void Release(InventoryItem item, int quantity)
        {
            RequireItem(item); RequirePositive(quantity);
            var b = GetMutable(item);
            if (b.Reserved < quantity) throw new InvalidOperationException("Không đủ số lượng đã giữ chỗ để hoàn trả.");
            var available = checked(b.Available + quantity);
            b.Reserved -= quantity;
            b.Available = available;
        }

        public void BeginProduction(InventoryItem item, int quantity)
        {
            RequireItem(item); RequirePositive(quantity);
            var b = GetMutable(item);
            if (b.Reserved < quantity) throw new InvalidOperationException("Không đủ nguyên liệu đã giữ chỗ để bắt đầu sản xuất.");
            var inProduction = checked(b.InProduction + quantity);
            b.Reserved -= quantity;
            b.InProduction = inProduction;
        }

        public void CompleteProduction(InventoryItem input, int inputQuantity, InventoryItem output, int outputQuantity)
        {
            RequireItem(input); RequireItem(output); RequirePositive(inputQuantity); RequirePositive(outputQuantity);
            var source = GetMutable(input);
            if (source.InProduction < inputQuantity) throw new InvalidOperationException("Không đủ đầu vào đang sản xuất để hoàn tất.");
            var destination = GetMutable(output);
            var available = checked(destination.Available + outputQuantity);
            source.InProduction -= inputQuantity;
            destination.Available = available;
        }

        public void CancelProduction(InventoryItem item, int quantity, bool returnInputs)
        {
            RequireItem(item); RequirePositive(quantity);
            var b = GetMutable(item);
            if (b.InProduction < quantity) throw new InvalidOperationException("Không đủ đầu vào đang sản xuất để hủy.");
            var available = returnInputs ? checked(b.Available + quantity) : b.Available;
            b.InProduction -= quantity;
            b.Available = available;
        }

        private MutableBalance GetMutable(InventoryItem item)
        {
            if (!balances.TryGetValue(item, out var b)) balances.Add(item, b = new MutableBalance());
            return b;
        }
        private static void RequirePositive(int quantity)
        { if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng phải lớn hơn 0."); }
        private static void RequireItem(InventoryItem item)
        { if (string.IsNullOrWhiteSpace(item.Value)) throw new ArgumentException("Mã vật phẩm không hợp lệ.", nameof(item)); }
    }
}
