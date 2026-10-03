using System;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Production;

namespace Game.Domain.Supply
{
    public sealed record ShopSaleResult(int UnitsSold, int UnitsUnfilled, long Gross);

    /// <summary>Quầy bán hàng hóa đã sản xuất; tồn kho giảm đúng theo số bán.</summary>
    public sealed class ConsumableStall
    {
        private readonly ConsumableStallDefinition definition;
        private readonly Inventory inventory;
        private readonly MoneyLedger ledger;

        public string ShopId => definition.ShopId;

        public ConsumableStall(ConsumableStallDefinition definition, Inventory inventory, MoneyLedger ledger)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        }

        public int Available(ProductId product)
        {
            EnsureListed(product);
            return inventory.Get(new InventoryItem(product)).Available;
        }

        public bool CanSell(ProductId product)
            => definition.Products.Contains(product) && inventory.Get(new InventoryItem(product)).Available > 0;

        public ShopSaleResult SellToTrainer(string trainerAccount, ProductId product, int requestedUnits,
            long unitPrice, long trainerSpendingLimit)
        {
            if (string.IsNullOrWhiteSpace(trainerAccount)) throw new ArgumentException("Thiếu tài khoản Trainer.", nameof(trainerAccount));
            if (requestedUnits < 0) throw new ArgumentOutOfRangeException(nameof(requestedUnits));
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
            if (trainerSpendingLimit < 0) throw new ArgumentOutOfRangeException(nameof(trainerSpendingLimit));
            EnsureListed(product);
            if (requestedUnits == 0) return new ShopSaleResult(0, 0, 0);

            var available = inventory.Get(new InventoryItem(product)).Available;
            var affordable = unitPrice == 0 ? int.MaxValue : trainerSpendingLimit / unitPrice;
            var sold = (int)Math.Min(Math.Min((long)requestedUnits, available), affordable);
            if (sold == 0) return new ShopSaleResult(0, requestedUnits, 0);
            var gross = checked((long)sold * unitPrice);
            ledger.Record(trainerAccount, "hub:treasury", "world:tax-sink", gross, 0, "consumable stall sale");
            inventory.Remove(new InventoryItem(product), sold);
            return new ShopSaleResult(sold, requestedUnits - sold, gross);
        }

        /// <summary>Buys the entire requested quantity or changes neither cash ledger nor stock.</summary>
        public bool TryPurchaseToTrainer(string trainerAccount, ProductId product, int units, long unitPrice,
            long trainerSpendingLimit, out ShopSaleResult result)
        {
            if (string.IsNullOrWhiteSpace(trainerAccount)) throw new ArgumentException("Thiếu tài khoản Trainer.", nameof(trainerAccount));
            if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units));
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
            if (trainerSpendingLimit < 0) throw new ArgumentOutOfRangeException(nameof(trainerSpendingLimit));
            EnsureListed(product);
            long gross;
            try { gross = checked((long)units * unitPrice); }
            catch (OverflowException) { result = new ShopSaleResult(0, units, 0); return false; }
            var item = new InventoryItem(product);
            if (inventory.Get(item).Available < units || trainerSpendingLimit < gross)
            { result = new ShopSaleResult(0, units, 0); return false; }
            // Record can fail before state mutation (overflow/invalid ledger state); the following operations were prevalidated.
            ledger.Record(trainerAccount, "hub:treasury", "world:tax-sink", gross, 0, "consumable stall purchase");
            inventory.Remove(item, units);
            result = new ShopSaleResult(units, 0, gross);
            return true;
        }

        private void EnsureListed(ProductId product)
        {
            if (!definition.Products.Contains(product)) throw new ArgumentException("Sản phẩm không được bán tại quầy này.", nameof(product));
        }
    }
}
