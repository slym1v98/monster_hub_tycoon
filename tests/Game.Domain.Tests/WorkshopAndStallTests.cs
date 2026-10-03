using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Production;
using Game.Domain.Supply;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class WorkshopAndStallTests
    {
        [Fact]
        public void EquipmentWorkshopsMapToTheThreeGddSlotGroups()
        {
            var workshops = EquipmentWorkshopCatalog.Default.Workshops;
            Assert.Equal(3, workshops.Count);
            Assert.Contains(workshops, x => x.Producer.Value == "monster_forge" && x.Group == EquipmentWorkshopGroup.MonsterCombat && x.SlotsPerOwner == 6);
            Assert.Contains(workshops, x => x.Producer.Value == "trainer_textile_workshop" && x.Group == EquipmentWorkshopGroup.TrainerUtility && x.SlotsPerOwner == 6);
            Assert.Contains(workshops, x => x.Producer.Value == "aura_jeweler" && x.Group == EquipmentWorkshopGroup.Aura && x.SlotsPerOwner == 6);
        }

        [Fact]
        public void ConsumableRecipesUseGddMaterialsAndSodaGoodsSellAtGeneralStore()
        {
            var catalog = MaterialCatalog.Default;
            Assert.Contains(catalog.Recipes, r => r.Producer.Value == "hospital" && r.Outputs.Any(o => o.Product == new ProductId("potion")) && r.Inputs.Any(i => i.Material == MaterialId.For(MaterialFamily.Herb, 1)));
            Assert.Contains(catalog.Recipes, r => r.Producer.Value == "tool_workshop" && r.Outputs.Any(o => o.Product == new ProductId("trap")) && r.Inputs.Count == 2);
            var sodaStall = Assert.Single(ConsumableStallCatalog.Default.Stalls, x => x.ShopId == "general_store");
            Assert.Equal("soda_factory", sodaStall.Producer.Value);
            Assert.Contains(new ProductId("monster_buff_bottle"), sodaStall.Products);
        }

        [Fact]
        public void StallSellsOnlyProducedStockAndPostsMoneyExactlyOnce()
        {
            var catalog = MaterialCatalog.Default;
            var stallDefinition = Assert.Single(ConsumableStallCatalog.Default.Stalls, x => x.ShopId == "veterinary_hospital");
            var stock = new Inventory();
            var potion = new InventoryItem(new ProductId("potion"));
            var food = new InventoryItem(new ProductId("food_drink"));
            stock.Add(new InventoryItem(MaterialId.For(MaterialFamily.Herb, 1)), 3);
            stock.Add(new InventoryItem(MaterialId.For(MaterialFamily.Food, 1)), 2);
            var ledger = new MoneyLedger();
            var production = new ProductionController(catalog, stock, ledger, new ProductionConfig(defaultJobDurationMinutes: 60));
            production.SetTarget(new ProductId("potion"), 3, now: 0);
            production.SetTarget(new ProductId("food_drink"), 2, now: 0);
            production.AdvanceTo(180);
            Assert.Equal(3, stock.Get(potion).Available);
            Assert.Equal(2, stock.Get(food).Available);
            var stall = new ConsumableStall(stallDefinition, stock, ledger);

            Assert.False(stall.CanSell(new ProductId("sleep_service")));
            Assert.Throws<System.ArgumentException>(() => stall.SellToTrainer("trainer:1", new ProductId("food_drink"), 1, 5, 100));
            var sale = stall.SellToTrainer("trainer:1", new ProductId("potion"), 2, 10, 15);
            Assert.Equal(1, sale.UnitsSold);
            Assert.Equal(1, sale.UnitsUnfilled);
            Assert.Equal(2, stock.Get(potion).Available);
            Assert.Equal(2, stock.Get(food).Available);
            Assert.Equal(-10, ledger.BalanceOf("trainer:1"));
            Assert.Equal(10, ledger.BalanceOf("hub:treasury"));
            Assert.Equal(0, ledger.TotalBalance);
            Assert.DoesNotContain(stallDefinition.Products, id => id.Value.Contains("sleep") || id.Value.Contains("heal"));
        }

        [Fact]
        public void BuffBottleCarriesEffectTagWithoutApplyingAnEffect()
        {
            var bottle = Assert.Single(MaterialCatalog.Default.Products, x => x.Id.Value == "monster_buff_bottle");
            Assert.Equal(ProductEffectKind.TemporaryMonsterStatBuff, bottle.EffectKind);
        }

        [Fact]
        public void ExactStallPurchaseRejectsPartialStockOrCashWithoutMutation()
        {
            var definition = Assert.Single(ConsumableStallCatalog.Default.Stalls, x => x.ShopId == "veterinary_hospital");
            var stock = new Inventory(); var potion = new ProductId("potion"); var item = new InventoryItem(potion);
            stock.Add(item, 1); var ledger = new MoneyLedger(); var stall = new ConsumableStall(definition, stock, ledger);
            Assert.False(stall.TryPurchaseToTrainer("trainer:1", potion, 2, 10, 100, out var unavailable));
            Assert.Equal(0, unavailable.UnitsSold); Assert.Equal(1, stock.Get(item).Available); Assert.Empty(ledger.Transactions);
            Assert.False(stall.TryPurchaseToTrainer("trainer:1", potion, 1, 10, 9, out var unaffordable));
            Assert.Equal(0, unaffordable.UnitsSold); Assert.Equal(1, stock.Get(item).Available); Assert.Empty(ledger.Transactions);
            Assert.True(stall.TryPurchaseToTrainer("trainer:1", potion, 1, 10, 10, out var bought));
            Assert.Equal(1, bought.UnitsSold); Assert.Equal(0, stock.Get(item).Available);
            Assert.Equal(-10, ledger.BalanceOf("trainer:1")); Assert.Equal(10, ledger.BalanceOf("hub:treasury"));
        }
    }
}
