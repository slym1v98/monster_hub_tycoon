using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class MonsterCatalogTests
    {
        static MonsterDefinition Definition(string id = "test", MonsterElement element = MonsterElement.Grass,
            MonsterRole role = MonsterRole.Support, MonsterStats stats = null) => new MonsterDefinition(
                id, "Test Monster", element, role, stats ?? new MonsterStats(300, 10, 10, 1, 0.05),
                new MonsterStats(0, 0, 0, 0, 0));

        [Fact]
        public void CatalogCopiesDefinitionsAndOrdersByOrdinalSpeciesId()
        {
            var input = new List<MonsterDefinition> { Definition("z"), Definition("a") };
            var catalog = new MonsterCatalog(input);
            input.Clear();
            Assert.Equal(new[] { "a", "z" }, catalog.Definitions.Select(x => x.Id));
            Assert.Throws<NotSupportedException>(() => ((IList<MonsterDefinition>)catalog.Definitions).Clear());
        }

        [Fact]
        public void CatalogRejectsDuplicateEmptyAndNullDefinitions()
        {
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { Definition(), Definition() }));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { Definition("") }));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { Definition(" ") }));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new MonsterDefinition[] { null }));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(Array.Empty<MonsterDefinition>()));
            Assert.Throws<ArgumentNullException>(() => new MonsterCatalog(null));
        }

        [Theory]
        [InlineData((MonsterElement)99, MonsterRole.Support)]
        [InlineData(MonsterElement.Grass, (MonsterRole)99)]
        public void CatalogRejectsInvalidElementsAndRoles(MonsterElement element, MonsterRole role)
        {
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { Definition(element: element, role: role) }));
        }

        [Fact]
        public void CatalogRejectsMissingOrUnusableBaseStats()
        {
            var missing = new MonsterDefinition("test", "Test", MonsterElement.Grass, MonsterRole.Tank,
                null, new MonsterStats(0, 0, 0, 0, 0));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { missing }));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { Definition(stats: new MonsterStats(0, 10, 10, 1, 0)) }));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { Definition(stats: new MonsterStats(1, 0, 10, 1, 0)) }));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { Definition(stats: new MonsterStats(1, 10, 0, 1, 0)) }));
            Assert.Throws<ArgumentException>(() => new MonsterCatalog(new[] { Definition(stats: new MonsterStats(1, 10, 10, 0, 0)) }));
        }

        [Theory]
        [InlineData(-1, 10, 10, 1, 0)]
        [InlineData(1, -1, 10, 1, 0)]
        [InlineData(1, double.NaN, 10, 1, 0)]
        [InlineData(1, 10, double.PositiveInfinity, 1, 0)]
        [InlineData(1, 10, 10, -1, 0)]
        [InlineData(1, 10, 10, 1, 1.1)]
        [InlineData(1, 10, 10, 1, -0.1)]
        public void StatsRejectNegativeNonfiniteAndInvalidCriticalChance(long hp, double attack,
            double defense, double speed, double criticalChance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterStats(hp, attack, defense, speed, criticalChance));
        }

        [Fact]
        public void MonsterCreateValidatesIdentityDefinitionAndGenerationInputs()
        {
            Assert.Throws<ArgumentException>(() => new MonsterId(" "));
            Assert.Throws<ArgumentException>(() => Monster.Create(default, Definition(), Rarity.Common, MonsterIvGrade.B, 1, 42));
            Assert.Throws<ArgumentNullException>(() => Monster.Create(new MonsterId("a"), null, Rarity.Common, MonsterIvGrade.B, 1, 42));
            Assert.Throws<ArgumentException>(() => Monster.Create(new MonsterId("a"), Definition(""), Rarity.Common, MonsterIvGrade.B, 1, 42));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monster.Create(new MonsterId("a"), Definition(), (Rarity)99, MonsterIvGrade.B, 1, 42));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monster.Create(new MonsterId("a"), Definition(), Rarity.Common, (MonsterIvGrade)99, 1, 42));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monster.Create(new MonsterId("a"), Definition(), Rarity.Common, MonsterIvGrade.B, 0, 42));
        }
    }
}
