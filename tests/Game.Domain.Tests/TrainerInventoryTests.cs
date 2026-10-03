using System.Collections.Generic;
using Game.Domain;
using Game.Domain.Materials;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class TrainerInventoryTests
    {
        [Fact]
        public void ProductStacksAreOwnedByTrainerAndConsumeAtomically()
        {
            var inventory = new TrainerInventory();
            var potion = new ProductId("potion");
            inventory.Add(potion, 2); inventory.Add(potion, 3);
            Assert.Equal(5, inventory.Count(potion));
            Assert.False(inventory.TryConsume(potion, 6));
            Assert.Equal(5, inventory.Count(potion));
            Assert.True(inventory.TryConsume(potion, 4));
            Assert.Equal(1, inventory.Count(potion));
            Assert.Throws<System.OverflowException>(() => inventory.Add(potion, int.MaxValue));
            Assert.Equal(1, inventory.Count(potion));
            Assert.Throws<System.NotSupportedException>(() => ((IDictionary<ProductId, int>)inventory.Products).Clear());
        }

        [Fact]
        public void PolicyUsesNeedStockCashAndPersonalityPriceSensitivity()
        {
            var trainer = new TrainerSnapshot(0, 1, 1, Rarity.Common, Personality.Capitalist,
                new TrainerAttributes(1, 1, 1, 1), gold: 30);
            var stock = new HubStockSnapshot(new Dictionary<ProductId, ProductStock> {
                [new ProductId("potion")] = new ProductStock(2, 10),
                [new ProductId("tranquilizer")] = new ProductStock(2, 10),
                [new ProductId("capture_ball")] = new ProductStock(2, 10) });
            var plan = ConsumablePolicy.DecidePurchases(trainer, stock,
                new CombatRiskSnapshot(2, nightmareStress: true, captureOpportunity: true), ConsumablePolicyConfig.Prototype);
            Assert.Single(plan);
            Assert.Equal(new ProductId("potion"), plan[0].Product);
            Assert.Equal(2, plan[0].Units);
            Assert.DoesNotContain(plan, x => x.Product == new ProductId("capture_ball"));
        }

        [Fact]
        public void PolicyDoesNotRepurchaseProductsAlreadyOwned()
        {
            var potion = new ProductId("potion");
            var trainer = new TrainerSnapshot(0, 1, 1, Rarity.Common, Personality.Warlike,
                new TrainerAttributes(1, 1, 1, 1), gold: 100,
                products: new[] { new KeyValuePair<ProductId, int>(potion, 2) });
            var stock = new HubStockSnapshot(new Dictionary<ProductId, ProductStock> { [potion] = new ProductStock(5, 1) });
            Assert.Empty(ConsumablePolicy.DecidePurchases(trainer, stock, new CombatRiskSnapshot(2), ConsumablePolicyConfig.Prototype));
        }
    }
}
