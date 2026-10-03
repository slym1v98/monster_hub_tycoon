using System;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Production;
using Game.Domain.Supply;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class ProductionControllerTests
    {
        private static readonly MaterialId Ore = MaterialId.For(MaterialFamily.Ore, 1);
        private static readonly ProductId Blank = new ProductId("blank_ore_tier_1");
        private static readonly ProductId Stone = new ProductId("enhancement_stone");

        [Fact]
        public void DefaultCatalogContainsRefineryAndReactorRecipesAsData()
        {
            var catalog = MaterialCatalog.Default;
            Assert.Equal(18, catalog.Recipes.Count(recipe => recipe.Producer.Value == "refinery"));
            Assert.Equal(45, catalog.Recipes.Count(recipe => recipe.Producer.Value == "reactor"));
            Assert.Equal(44, catalog.Products.Count);
            Assert.Contains(catalog.Products, product => product.Id.Value == "gene_fragment");
            Assert.Contains(catalog.Products, product => product.Id.Value == "protection_charm");
            Assert.DoesNotContain(catalog.Products, product => product.Id.Value == "blank");
            Assert.Contains(catalog.Recipes, recipe => recipe.Inputs.Any(input => input.Material == Ore) && recipe.Outputs.Any(output => output.Product == Blank));
            Assert.Contains(catalog.Recipes, recipe => recipe.Inputs.Any(input => input.Product == Blank) && recipe.Outputs.Any(output => output.Product == Stone));
        }

        [Fact]
        public void MissingRawInputsGenerateDemandWithoutCreatingOutput()
        {
            var inventory = new Inventory();
            var controller = CreateController(inventory, jobDuration: 8);
            controller.SetTarget(Blank, 1, now: 10);
            Assert.Empty(controller.ActiveJobs);
            Assert.Equal(0, inventory.Get(new InventoryItem(Blank)).Available);
            Assert.Contains(controller.RestockDemands, demand => demand.Item == new InventoryItem(Ore) && demand.Quantity > 0);
        }

        [Fact]
        public void JobReservesInputsAndCompletesAtExactMinuteOnlyOnce()
        {
            var inventory = new Inventory();
            inventory.Add(new InventoryItem(Ore), 2);
            var ledger = new MoneyLedger();
            var controller = CreateController(inventory, jobDuration: 8, efficiency: 1m, ledger: ledger, operatingCost: 3);
            controller.SetTarget(Blank, 1, now: 10);
            var job = Assert.Single(controller.ActiveJobs);
            Assert.Equal(10, job.StartMinute);
            Assert.Equal(18, job.FinishMinute);
            Assert.Equal(1, inventory.Get(new InventoryItem(Ore)).InProduction);
            controller.AdvanceTo(17);
            Assert.Equal(0, inventory.Get(new InventoryItem(Blank)).Available);
            controller.AdvanceTo(18);
            Assert.Equal(1, inventory.Get(new InventoryItem(Blank)).Available);
            Assert.Empty(controller.ActiveJobs);
            Assert.Throws<InvalidOperationException>(() => controller.CompleteJob(job.Id));
            Assert.Single(ledger.Transactions);
            Assert.Equal(3, ledger.Transactions[0].Gross);
            Assert.Equal(0, ledger.TotalBalance);
        }

        [Fact]
        public void ProducerPowerOffPausesRunningJobsAndResumesTheirRemainingTime()
        {
            var inventory = new Inventory();
            inventory.Add(new InventoryItem(Ore), 1);
            var controller = CreateController(inventory, jobDuration: 8);
            controller.SetTarget(Blank, 1, now: 10);
            var job = Assert.Single(controller.ActiveJobs);

            controller.SetProducerState(new ProducerId("refinery"), 1, 0);
            Assert.Equal(ProductionJobState.Paused, job.State);
            Assert.Empty(controller.ActiveJobs);
            controller.AdvanceTo(100);
            Assert.Equal(0, inventory.Get(new InventoryItem(Blank)).Available);

            controller.SetProducerState(new ProducerId("refinery"), 1, 1);
            Assert.Equal(108, job.FinishMinute);
            controller.AdvanceTo(107);
            Assert.Equal(0, inventory.Get(new InventoryItem(Blank)).Available);
            controller.AdvanceTo(108);
            Assert.Equal(1, inventory.Get(new InventoryItem(Blank)).Available);
        }

        [Fact]
        public void IntegratedProductionSpendsHubTreasuryWhenJobStarts()
        {
            var inventory = new Inventory();
            inventory.Add(new InventoryItem(Ore), 1);
            var ledger = new MoneyLedger();
            var treasury = new TreasuryAccount(10);
            var controller = new ProductionController(MaterialCatalog.Default, inventory, ledger,
                new ProductionConfig(8, defaultOperatingCost: 3), treasury);

            controller.SetTarget(Blank, 1, now: 0);

            Assert.Single(controller.ActiveJobs);
            Assert.Equal(7, treasury.Balance);
            Assert.Equal(1, inventory.Get(new InventoryItem(Ore)).InProduction);
            Assert.Equal("production operating cost", Assert.Single(ledger.Transactions).Reason);
        }

        [Fact]
        public void IntegratedProductionDoesNotReserveInputsWhenTreasuryCannotPay()
        {
            var inventory = new Inventory();
            inventory.Add(new InventoryItem(Ore), 1);
            var treasury = new TreasuryAccount(2);
            var controller = new ProductionController(MaterialCatalog.Default, inventory, new MoneyLedger(),
                new ProductionConfig(8, defaultOperatingCost: 3), treasury);

            controller.SetTarget(Blank, 1, now: 0);

            Assert.Empty(controller.ActiveJobs);
            Assert.Equal(1, inventory.Get(new InventoryItem(Ore)).Available);
            Assert.Equal(0, inventory.Get(new InventoryItem(Ore)).InProduction);
            Assert.Equal(2, treasury.Balance);
        }

        [Fact]
        public void CancellingJobReturnsEveryReservedInputExactlyOnce()
        {
            var inventory = new Inventory();
            inventory.Add(new InventoryItem(Ore), 1);
            var controller = CreateController(inventory, jobDuration: 8, efficiency: 1m);
            controller.SetTarget(Blank, 1, now: 0);
            var job = Assert.Single(controller.ActiveJobs);
            Assert.True(controller.CancelJob(job.Id));
            Assert.Equal(1, inventory.Get(new InventoryItem(Ore)).Available);
            Assert.Equal(0, inventory.Get(new InventoryItem(Ore)).InProduction);
            Assert.False(controller.CancelJob(job.Id));
            controller.AdvanceTo(100);
            Assert.Equal(0, inventory.Get(new InventoryItem(Blank)).Available);
        }

        [Fact]
        public void TargetsScheduleOnlyMissingOutputAndRespectProducerSlots()
        {
            var inventory = new Inventory();
            inventory.Add(new InventoryItem(new ProductId("blank_ore_tier_1")), 5);
            var controller = CreateController(inventory, jobDuration: 8, efficiency: 1m);
            controller.SetTarget(Stone, 2, now: 0);
            Assert.Single(controller.ActiveJobs);
            Assert.Equal(1, controller.ActiveJobs[0].BaseOutputs[0].Quantity);
            controller.SetTarget(Stone, 5, now: 0);
            Assert.Single(controller.ActiveJobs);
            controller.AdvanceTo(8);
            Assert.Single(controller.ActiveJobs);
            Assert.Equal(8, controller.ActiveJobs[0].StartMinute);
            controller.AdvanceTo(24);
            Assert.Equal(3, inventory.Get(new InventoryItem(Stone)).Available);
            Assert.Equal(new[] { (0, 8), (8, 16), (16, 24), (24, 32) },
                controller.Jobs.Select(job => (job.StartMinute, job.FinishMinute)));
        }

        [Fact]
        public void RefineryLevelControlsYieldUsingFiveLevelPrototypeCurve()
        {
            Assert.Equal(17, RefineAtLevel(1, 17));
            Assert.Equal(19, RefineAtLevel(5, 19));
        }

        [Theory]
        [InlineData(1, 60)]
        [InlineData(2, 54)]
        [InlineData(3, 48)]
        [InlineData(4, 42)]
        [InlineData(5, 36)]
        public void ProducerLevelScalesJobDurationUsingFiveLevelPrototypeCurve(int level, int expectedMinutes)
        {
            var inventory = new Inventory();
            inventory.Add(new InventoryItem(Ore), 1);
            var controller = CreateController(inventory, jobDuration: 60, efficiency: 1m);
            controller.SetProducerState(new ProducerId("refinery"), level, 1);

            controller.SetTarget(Blank, 1, now: 0);

            var job = Assert.Single(controller.ActiveJobs);
            Assert.Equal(expectedMinutes, job.FinishMinute - job.StartMinute);
            Assert.Equal(level, job.ProducerLevel);
        }

        [Fact]
        public void ProductionConfigRejectsInvalidProducerDurationCurve()
        {
            Assert.Throws<ArgumentException>(() => new ProductionConfig(
                new[] { 1.0m, 0.9m, 0.8m, 0.7m, 1.1m }));
        }

        private static int RefineAtLevel(int level, int target)
        {
            var inventory = new Inventory();
            inventory.Add(new InventoryItem(Ore), 20);
            var controller = new ProductionController(MaterialCatalog.Default, inventory, new MoneyLedger(), new ProductionConfig(1));
            controller.SetProducerState(new ProducerId("refinery"), level, 1);
            controller.SetTarget(Blank, target, now: 0);
            while (controller.ActiveJobs.Count > 0)
                controller.AdvanceTo(controller.ActiveJobs[0].FinishMinute);
            return inventory.Get(new InventoryItem(Blank)).Available;
        }

        private static ProductionController CreateController(Inventory inventory, int jobDuration, decimal efficiency = 1m,
            MoneyLedger ledger = null, long operatingCost = 0)
        {
            var efficiencies = Enumerable.Repeat(efficiency, 5).ToArray();
            return new ProductionController(MaterialCatalog.Default, inventory, ledger ?? new MoneyLedger(),
                new ProductionConfig(jobDuration, operatingCost, efficiencies));
        }
    }
}
