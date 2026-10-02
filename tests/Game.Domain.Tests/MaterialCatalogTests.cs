using System;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Production;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class MaterialCatalogTests
    {
        [Fact]
        public void DefaultCatalogContainsStableIdForEveryFamilyAndTier()
        {
            var materials = MaterialCatalog.Default.Materials;

            Assert.Equal(30, materials.Count);
            Assert.Equal(30, materials.Select(material => material.Id.Value).Distinct().Count());
            Assert.Equal(6, materials.Select(material => material.Family).Distinct().Count());
            Assert.Equal(5, materials.Select(material => material.Tier).Distinct().Count());
            Assert.All(Enum.GetValues(typeof(MaterialFamily)).Cast<MaterialFamily>(), family =>
                Assert.Equal(5, materials.Count(material => material.Family == family)));
            Assert.All(Enumerable.Range(1, 5), tier =>
                Assert.Equal(6, materials.Count(material => material.Tier == tier)));
            foreach (MaterialFamily family in Enum.GetValues(typeof(MaterialFamily)))
                for (var tier = 1; tier <= 5; tier++)
                    Assert.Single(materials, material => material.Family == family && material.Tier == tier);
            Assert.Equal("ore_tier_1", MaterialId.For(MaterialFamily.Ore, 1).Value);
        }

        [Fact]
        public void CatalogRejectsDuplicateMaterialIds()
        {
            var definition = new MaterialDefinition(MaterialId.For(MaterialFamily.Ore, 1), MaterialFamily.Ore, 1, "Quặng Tier 1");

            Assert.Throws<ArgumentException>(() => new MaterialCatalog(new[] { definition, definition }));
        }

        [Theory]
        [InlineData((MaterialFamily)99, 1)]
        [InlineData(MaterialFamily.Ore, 0)]
        [InlineData(MaterialFamily.Ore, 6)]
        public void CatalogRejectsInvalidMaterialFamilyOrTier(MaterialFamily family, int tier)
        {
            var definition = new MaterialDefinition(new MaterialId("invalid"), family, tier, "Nguyên liệu");

            Assert.Throws<ArgumentException>(() => new MaterialCatalog(new[] { definition }));
        }

        [Fact]
        public void CatalogRejectsRecipesWithoutInputsOrOutputs()
        {
            var material = MaterialCatalog.Default.Materials[0];
            var producer = new ProducerDefinition(new ProducerId("refinery"), "Nhà máy Tinh chế");
            var noInputs = new Recipe(new RecipeId("missing-input"), "Công thức thiếu đầu vào", producer.Id, Array.Empty<RecipeInput>(), new[] { new RecipeOutput(material.Id, 1) });
            var noOutputs = new Recipe(new RecipeId("missing-output"), "Công thức thiếu đầu ra", producer.Id, new[] { new RecipeInput(material.Id, 1) }, Array.Empty<RecipeOutput>());

            Assert.Throws<ArgumentException>(() => new MaterialCatalog(new[] { material }, recipes: new[] { noInputs }, producers: new[] { producer }));
            Assert.Throws<ArgumentException>(() => new MaterialCatalog(new[] { material }, recipes: new[] { noOutputs }, producers: new[] { producer }));
        }
    }
}
