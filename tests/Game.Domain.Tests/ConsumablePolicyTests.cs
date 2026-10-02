using System.Collections.Generic;
using Game.Domain;
using Game.Domain.Materials;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class ConsumablePolicyTests
    {
        [Fact]
        public void BuysPotionCakeAndTacticsBookOnlyForCorrespondingRisksAndAvailableStock()
        {
            var trainer = new TrainerSnapshot(0, 1, 1, Rarity.Common, Personality.Warlike,
                new TrainerAttributes(1, 1, 1, 1), gold: 100);
            var ids = new[] { "potion", "reward_cake", "tactics_book" };
            var stock = new Dictionary<ProductId, ProductStock>();
            foreach (string id in ids) stock.Add(new ProductId(id), new ProductStock(2, 10));
            var plan = ConsumablePolicy.DecidePurchases(trainer, new HubStockSnapshot(stock),
                new CombatRiskSnapshot(0, rebelliousMonsterCount: 2, hasEligibleReserve: true, expectedCombatRisk: .5), ConsumablePolicyConfig.Prototype);
            Assert.Collection(plan,
                x => { Assert.Equal(new ProductId("potion"), x.Product); Assert.Equal(1, x.Units); },
                x => { Assert.Equal(new ProductId("reward_cake"), x.Product); Assert.Equal(2, x.Units); },
                x => { Assert.Equal(new ProductId("tactics_book"), x.Product); Assert.Equal(1, x.Units); });
        }

        [Fact]
        public void DoesNotBuyRiskItemsBelowThresholdOrWhenAlreadyOwned()
        {
            var potion = new ProductId("potion");
            var trainer = new TrainerSnapshot(0, 1, 1, Rarity.Common, Personality.Capitalist,
                new TrainerAttributes(1, 1, 1, 1), gold: 100,
                products: new[] { new KeyValuePair<ProductId, int>(potion, 1) });
            var stock = new HubStockSnapshot(new Dictionary<ProductId, ProductStock> { [potion] = new ProductStock(5, 1) });
            Assert.Empty(ConsumablePolicy.DecidePurchases(trainer, stock,
                new CombatRiskSnapshot(0, expectedCombatRisk: .1), ConsumablePolicyConfig.Prototype));
        }
    }
}
