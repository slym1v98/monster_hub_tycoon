using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Game.Domain.Materials;

namespace Game.Domain
{
    public sealed class ConsumablePriceConfig
    {
        readonly IReadOnlyDictionary<ProductId, long> prices;
        public static ConsumablePriceConfig Prototype { get; } = new ConsumablePriceConfig(new Dictionary<ProductId, long> {
            [new ProductId("potion")] = 10, [new ProductId("vaccine")] = 10, [new ProductId("tranquilizer")] = 10,
            [new ProductId("food_drink")] = 10, [new ProductId("reward_cake")] = 10, [new ProductId("liquor")] = 10,
            [new ProductId("raincoat")] = 10, [new ProductId("gas_mask")] = 10, [new ProductId("capture_ball")] = 10,
            [new ProductId("trap")] = 10, [new ProductId("tactics_book")] = 10, [new ProductId("overclock_coffee")] = 10,
            [new ProductId("monster_buff_bottle")] = 10, [new ProductId("enhancement_stone")] = 20, [new ProductId("distilled_water")] = 15, [new ProductId("evolution_stone")] = 50, [new ProductId("protection_charm")] = 500 });
        public IReadOnlyDictionary<ProductId, long> Prices => prices;
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public ConsumablePriceConfig(IDictionary<ProductId, long> prices)
        {
            if (prices == null) throw new ArgumentNullException(nameof(prices));
            foreach (var entry in prices)
                if (string.IsNullOrWhiteSpace(entry.Key.Value) || entry.Value < 0) throw new ArgumentException("Product prices must have valid IDs and nonnegative values.", nameof(prices));
            var sorted = new SortedDictionary<ProductId, long>(Comparer<ProductId>.Create((a, b) => string.CompareOrdinal(a.Value, b.Value)));
            foreach (var entry in prices) sorted.Add(entry.Key, entry.Value);
            this.prices = new ReadOnlyDictionary<ProductId, long>(sorted);
            BalanceParameters = Array.AsReadOnly(sorted.Select(x => new BalanceParameter("product.price." + x.Key.Value, x.Value, "gold/unit", "Prototype", "Prototype consumable purchase prices; GDD ties fair price to building quality." )).ToArray());
        }
        public bool TryGetPrice(ProductId product, out long price) => prices.TryGetValue(product, out price);
    }

    /// <summary>Тренер owns product stacks; equipment items remain assigned to this owner.</summary>
    public sealed class TrainerInventory
    {
        readonly Dictionary<ProductId, int> counts = new Dictionary<ProductId, int>();
        public IReadOnlyDictionary<ProductId, int> Products => new ReadOnlyDictionary<ProductId, int>(new Dictionary<ProductId, int>(counts));
        public int Count(ProductId id) => counts.TryGetValue(id, out var value) ? value : 0;
        public bool CanAdd(ProductId id, int quantity) => !string.IsNullOrWhiteSpace(id.Value) && quantity > 0 && Count(id) <= int.MaxValue - quantity;
        public void Add(ProductId id, int quantity)
        {
            if (string.IsNullOrWhiteSpace(id.Value)) throw new ArgumentException("Product ID is required.", nameof(id));
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            counts[id] = checked(Count(id) + quantity);
        }
        public bool TryConsume(ProductId id, int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            int current = Count(id);
            if (current < quantity) return false;
            int remaining = current - quantity;
            if (remaining == 0) counts.Remove(id); else counts[id] = remaining;
            return true;
        }
    }

    public sealed class HubStockSnapshot
    {
        public IReadOnlyDictionary<ProductId, ProductStock> Products { get; }
        public HubStockSnapshot(IDictionary<ProductId, ProductStock> products)
        {
            if (products == null) throw new ArgumentNullException(nameof(products));
            foreach (var pair in products)
                if (string.IsNullOrWhiteSpace(pair.Key.Value) || pair.Value == null) throw new ArgumentException("Product stock entries must be valid.", nameof(products));
            Products = new ReadOnlyDictionary<ProductId, ProductStock>(new Dictionary<ProductId, ProductStock>(products));
        }
    }
    public sealed class ProductStock
    {
        public int Available { get; }
        public long UnitPrice { get; }
        public ProductStock(int available, long unitPrice)
        { if (available < 0) throw new ArgumentOutOfRangeException(nameof(available)); if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice)); Available = available; UnitPrice = unitPrice; }
    }
    public sealed class CombatRiskSnapshot
    {
        public int InjuredMonsterCount { get; }
        public bool NightmareStress { get; }
        public bool CaptureOpportunity { get; }
        public int RebelliousMonsterCount { get; }
        public bool HasEligibleReserve { get; }
        public double ExpectedCombatRisk { get; }
        public CombatRiskSnapshot(int injuredMonsterCount, bool nightmareStress = false, bool captureOpportunity = false,
            int rebelliousMonsterCount = 0, bool hasEligibleReserve = false, double expectedCombatRisk = 0)
        {
            if (injuredMonsterCount < 0 || rebelliousMonsterCount < 0) throw new ArgumentOutOfRangeException(nameof(injuredMonsterCount));
            ZoneDefinition.ValidateNonNegativeFinite(expectedCombatRisk, nameof(expectedCombatRisk));
            InjuredMonsterCount = injuredMonsterCount; NightmareStress = nightmareStress; CaptureOpportunity = captureOpportunity;
            RebelliousMonsterCount = rebelliousMonsterCount; HasEligibleReserve = hasEligibleReserve; ExpectedCombatRisk = expectedCombatRisk;
        }
    }
    public sealed class ProductPurchase
    {
        public ProductId Product { get; }
        public int Units { get; }
        public ProductPurchase(ProductId product, int units)
        { if (string.IsNullOrWhiteSpace(product.Value)) throw new ArgumentException("Product ID is required.", nameof(product)); if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units)); Product = product; Units = units; }
    }
    public sealed class ConsumablePolicyConfig
    {
        public static ConsumablePolicyConfig Prototype { get; } = new ConsumablePolicyConfig();
        public int MaximumUnitsPerNeed { get; }
        public double PriceSensitivityBudgetDivisor { get; }
        public double MinimumExpectedCombatRisk { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public ConsumablePolicyConfig(int maximumUnitsPerNeed = 2, double priceSensitivityBudgetDivisor = 1, double minimumExpectedCombatRisk = 0.25)
        {
            if (maximumUnitsPerNeed < 1) throw new ArgumentOutOfRangeException(nameof(maximumUnitsPerNeed));
            ZoneDefinition.ValidatePositiveFinite(priceSensitivityBudgetDivisor, nameof(priceSensitivityBudgetDivisor));
            ZoneDefinition.ValidateNonNegativeFinite(minimumExpectedCombatRisk, nameof(minimumExpectedCombatRisk));
            MaximumUnitsPerNeed = maximumUnitsPerNeed; PriceSensitivityBudgetDivisor = priceSensitivityBudgetDivisor; MinimumExpectedCombatRisk = minimumExpectedCombatRisk;
            BalanceParameters = Array.AsReadOnly(new[] {
                new BalanceParameter("consumable_policy.max_units_per_need", maximumUnitsPerNeed, "units", "Prototype", "Prototype per-visit quantity cap."),
                new BalanceParameter("consumable_policy.price_sensitivity_budget_divisor", priceSensitivityBudgetDivisor, "multiplier", "Prototype", "Prototype personality price-sensitivity influence."),
                new BalanceParameter("consumable_policy.minimum_expected_combat_risk", minimumExpectedCombatRisk, "risk_score", "Prototype", "Prototype decision threshold to carry one Potion into a planned expedition.")
            });
        }
    }
    public static class ConsumablePolicy
    {
        public static IReadOnlyList<ProductPurchase> DecidePurchases(TrainerSnapshot trainer, HubStockSnapshot stock,
            CombatRiskSnapshot risk, ConsumablePolicyConfig config)
        {
            if (trainer == null) throw new ArgumentNullException(nameof(trainer)); if (stock == null) throw new ArgumentNullException(nameof(stock));
            if (risk == null) throw new ArgumentNullException(nameof(risk)); if (config == null) throw new ArgumentNullException(nameof(config));
            var wanted = new List<(ProductId product, int units)>();
            int Owned(ProductId id) => trainer.Products.TryGetValue(id, out var count) ? count : 0;
            var potion = new ProductId("potion");
            int potionDemand = risk.InjuredMonsterCount > 0 ? Math.Min(risk.InjuredMonsterCount, config.MaximumUnitsPerNeed)
                : risk.ExpectedCombatRisk >= config.MinimumExpectedCombatRisk ? 1 : 0;
            int potionNeed = Math.Max(0, potionDemand - Owned(potion));
            if (potionNeed > 0) wanted.Add((potion, potionNeed));
            var tranquilizer = new ProductId("tranquilizer");
            if (risk.NightmareStress && Owned(tranquilizer) == 0) wanted.Add((tranquilizer, 1));
            var captureBall = new ProductId("capture_ball");
            if (risk.CaptureOpportunity && Owned(captureBall) == 0) wanted.Add((captureBall, 1));
            var cake = new ProductId("reward_cake");
            if (risk.RebelliousMonsterCount > 0 && Owned(cake) == 0) wanted.Add((cake, Math.Min(risk.RebelliousMonsterCount, config.MaximumUnitsPerNeed)));
            var tactics = new ProductId("tactics_book");
            if (risk.HasEligibleReserve && Owned(tactics) == 0) wanted.Add((tactics, 1));
            double budgetValue = Math.Floor(trainer.Gold / (PersonalityProfile.Of(trainer.Personality).PriceSensitivity * config.PriceSensitivityBudgetDivisor));
            long budget = budgetValue >= trainer.Gold ? trainer.Gold : (long)budgetValue;
            var result = new List<ProductPurchase>();
            foreach (var entry in wanted)
            {
                if (!stock.Products.TryGetValue(entry.product, out var available) || available.Available == 0) continue;
                int affordable = available.UnitPrice == 0 ? int.MaxValue : (int)Math.Min(int.MaxValue, budget / available.UnitPrice);
                int quantity = Math.Min(entry.units, Math.Min(available.Available, affordable));
                if (quantity <= 0) continue;
                result.Add(new ProductPurchase(entry.product, quantity));
                budget -= checked((long)quantity * available.UnitPrice);
            }
            return Array.AsReadOnly(result.ToArray());
        }
    }
}
