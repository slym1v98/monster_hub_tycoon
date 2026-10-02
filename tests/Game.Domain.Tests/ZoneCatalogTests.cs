using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class ZoneCatalogTests
    {
        [Fact]
        public void DefaultCatalogHasTheFiveGddThemesRanksAndElementGroups()
        {
            var zones = ZoneCatalog.Default.Definitions;
            Assert.Equal(new[] { "zone_1", "zone_2", "zone_3", "zone_4", "zone_5" }, zones.Select(x => x.Id));
            Assert.Equal(new[] { "Đồng Cỏ", "Núi Lửa", "Hầm Băng", "Đầm Lầy", "Vực Thẳm" }, zones.Select(x => x.DisplayName));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, zones.Select(x => x.MinimumRank));
            MonsterElement[][] elements = {
                new[] { MonsterElement.Grass, MonsterElement.Water, MonsterElement.Ground },
                new[] { MonsterElement.Fire, MonsterElement.Ground },
                new[] { MonsterElement.Ice, MonsterElement.Water },
                new[] { MonsterElement.Poison, MonsterElement.Water, MonsterElement.Grass },
                new[] { MonsterElement.Dark }
            };
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(elements[i].OrderBy(x => x), zones[i].EncounterProfile.ElementWeights.Keys.OrderBy(x => x));
                Assert.Equal(2, zones[i].NightLootMultiplier);
                Assert.Equal(2, zones[i].NightExperienceMultiplier);
                if (i > 0) Assert.True(zones[i].WalkMinutes > zones[i - 1].WalkMinutes);
            }
        }

        [Fact]
        public void EachZoneFavorsItsTierAndAllowsOnlyLowerTierSpilloverForEveryFamily()
        {
            foreach (var zone in ZoneCatalog.Default.Definitions)
            foreach (MaterialFamily family in Enum.GetValues(typeof(MaterialFamily)))
            {
                // Tra danh tính qua catalog độc lập, không suy ra tier bằng trọng số.
                var materials = MaterialCatalog.Default.Materials.Where(x => x.Family == family).ToDictionary(x => x.Id);
                var weights = zone.MaterialWeights.Where(x => materials.ContainsKey(x.MaterialId)).ToArray();
                Assert.Equal(zone.MinimumRank, weights.Length);
                var primary = Assert.Single(weights, x => materials[x.MaterialId].Tier == zone.MinimumRank);
                Assert.All(weights, x => Assert.InRange(materials[x.MaterialId].Tier, 1, zone.MinimumRank));
                if (zone.MinimumRank > 1)
                {
                    var lower = weights.Where(x => materials[x.MaterialId].Tier < zone.MinimumRank).ToArray();
                    Assert.All(lower, x => Assert.True(x.Weight > 0));
                    Assert.True(primary.Weight > lower.Sum(x => x.Weight));
                }
            }
        }

        [Fact]
        public void CatalogAndNestedProfilesCopyInputsAndExposeReadOnlyCollections()
        {
            var elements = new Dictionary<MonsterElement, double> { [MonsterElement.Grass] = 1 };
            var profile = new EncounterProfile(elements, 1, 10, 1, 10);
            var materials = new List<ZoneMaterialWeight> { new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 1, 10) };
            var zone = new ZoneDefinition("zone_1", "Test", 1, 0, materials, profile);
            var input = new List<ZoneDefinition> { ZoneSelectorTests.Zone("zone_2"), zone };
            var catalog = new ZoneCatalog(input);
            input.Clear(); materials.Clear(); elements.Clear();
            Assert.Equal(new[] { "zone_1", "zone_2" }, catalog.Definitions.Select(x => x.Id));
            Assert.Single(zone.MaterialWeights);
            Assert.Single(profile.ElementWeights);
            Assert.Throws<NotSupportedException>(() => ((IList<ZoneDefinition>)catalog.Definitions).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<ZoneMaterialWeight>)zone.MaterialWeights).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<MonsterElement, double>)profile.ElementWeights).Clear());
        }

        [Fact]
        public void CatalogRejectsNullEmptyAndDuplicateDefinitions()
        {
            var zone = ZoneSelectorTests.Zone("zone_1");
            Assert.Throws<ArgumentNullException>(() => new ZoneCatalog(null));
            Assert.Throws<ArgumentException>(() => new ZoneCatalog(Array.Empty<ZoneDefinition>()));
            Assert.Throws<ArgumentException>(() => new ZoneCatalog(new ZoneDefinition[] { null }));
            Assert.Throws<ArgumentException>(() => new ZoneCatalog(new[] { zone, zone }));
        }

        [Fact]
        public void ZoneRejectsInvalidIdentityRanksWalkingAndMaterials()
        {
            var profile = ZoneCatalog.Default.Definitions[0].EncounterProfile;
            var ore = new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 1, 10);
            Assert.Throws<ArgumentException>(() => new ZoneDefinition(" ", "Name", 1, 0, new[] { ore }, profile));
            Assert.Throws<ArgumentException>(() => new ZoneDefinition("id", " ", 1, 0, new[] { ore }, profile));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneDefinition("id", "Name", 0, 0, new[] { ore }, profile));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneDefinition("id", "Name", 6, 0, new[] { ore }, profile));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneDefinition("id", "Name", 1, -1, new[] { ore }, profile));
            Assert.Throws<ArgumentException>(() => new ZoneDefinition("id", "Name", 1, 0, Array.Empty<ZoneMaterialWeight>(), profile));
            Assert.Throws<ArgumentException>(() => new ZoneDefinition("id", "Name", 1, 0, new[] { ore, ore }, profile));
            Assert.Throws<ArgumentException>(() => new ZoneDefinition("id", "Name", 1, 0, new ZoneMaterialWeight[] { null }, profile));
            Assert.Throws<ArgumentNullException>(() => new ZoneDefinition("id", "Name", 1, 0, null, profile));
            Assert.Throws<ArgumentNullException>(() => new ZoneDefinition("id", "Name", 1, 0, new[] { ore }, null));
            Assert.Throws<ArgumentException>(() => new ZoneMaterialWeight(new MaterialId("unknown"), 1, 10));
            Assert.Throws<ArgumentException>(() => new ZoneMaterialWeight(default, 1, 10));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void NumericProfileAndSelectionInputsRejectNegativeOrNonfiniteValues(double value)
        {
            var elements = new Dictionary<MonsterElement, double> { [MonsterElement.Grass] = 1 };
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), value, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 1, value));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterProfile(elements, value, 10, 1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterProfile(elements, 1, value, 1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterProfile(elements, 1, 10, value, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterProfile(elements, 1, 10, 1, value));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneSelectionConfig(luckGoldPerPoint: value));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneSelectionConfig(capitalistBountyMultiplier: value));
        }

        [Fact]
        public void ProfilesRejectZeroWeightsInvalidElementsAndZeroFarmDuration()
        {
            Assert.Throws<ArgumentNullException>(() => new EncounterProfile(null, 1, 10, 1, 10));
            Assert.Throws<ArgumentException>(() => new EncounterProfile(new Dictionary<MonsterElement, double>(), 1, 10, 1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterProfile(new Dictionary<MonsterElement, double> { [(MonsterElement)99] = 1 }, 1, 10, 1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterProfile(new Dictionary<MonsterElement, double> { [MonsterElement.Grass] = 0 }, 1, 10, 1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneSelectionConfig(expectedFarmMinutes: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneSelectionConfig(capitalistBountyMultiplier: 0.5));
        }

        [Fact]
        public void AllNewNumericDefaultsHaveStableUniqueIdsUnitsStatusesAndSources()
        {
            var parameters = ZoneCatalog.Default.Definitions.SelectMany(x => x.BalanceParameters)
                .Concat(ZoneSelectionConfig.Prototype.BalanceParameters)
                .Concat(ExpeditionConfig.Prototype.BalanceParameters).Concat(LootConfig.Prototype.BalanceParameters)
                .Concat(ConsumablePriceConfig.Prototype.BalanceParameters).Concat(ConsumablePolicyConfig.Prototype.BalanceParameters)
                .Concat(Game.Domain.Combat.MonsterItemConfig.Prototype.BalanceParameters).ToArray();
            Assert.Equal(parameters.Length, parameters.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count());
            Assert.All(parameters, x => {
                Assert.False(string.IsNullOrWhiteSpace(x.Id));
                Assert.False(string.IsNullOrWhiteSpace(x.Unit));
                Assert.False(string.IsNullOrWhiteSpace(x.Source));
                Assert.Contains(x.Status, new[] { "Prototype", "Locked" });
            });
            foreach (var zone in ZoneCatalog.Default.Definitions)
            {
                Assert.Contains(zone.BalanceParameters, x => x.Id == zone.Id + ".walk_minutes" && x.Status == "Prototype" && x.Value == zone.WalkMinutes);
                Assert.Contains(zone.BalanceParameters, x => x.Id == zone.Id + ".night_loot_multiplier" && x.Status == "Locked" && x.Value == 2);
                foreach (var material in zone.MaterialWeights)
                {
                    Assert.Contains(zone.BalanceParameters, x => x.Id == zone.Id + ".material." + material.MaterialId.Value + ".weight" && x.Status == "Prototype" && x.Value == material.Weight);
                    Assert.Contains(zone.BalanceParameters, x => x.Id == zone.Id + ".material." + material.MaterialId.Value + ".gold_equivalent" && x.Status == "Prototype" && x.Value == material.GoldEquivalentPerUnit);
                }
            }
            Assert.Same(ZoneCatalog.Default, SimConfig.Default.ZoneCatalogSettings);
            Assert.Same(ZoneSelectionConfig.Prototype, SimConfig.Default.ZoneSelectionSettings);
        }

        [Fact]
        public void TrainerSnapshotCopiesValidatedTrainerState()
        {
            var trainer = new Trainer { Id = 7, Rank = 3, Level = 42, Rarity = Rarity.Epic, Personality = Personality.Capitalist };
            var snapshot = TrainerSnapshot.FromTrainer(trainer);
            trainer.Id = 9; trainer.Rank = 4; trainer.Level = 1; trainer.Rarity = Rarity.Common; trainer.Personality = Personality.Timid;
            Assert.Equal(7, snapshot.Id); Assert.Equal(3, snapshot.Rank); Assert.Equal(42, snapshot.Level);
            Assert.Equal(Rarity.Epic, snapshot.Rarity); Assert.Equal(Personality.Capitalist, snapshot.Personality);
            Assert.Equal(trainer.Attributes, snapshot.Attributes);
            Assert.NotSame(trainer.Attributes, snapshot.Attributes);
        }

        [Theory]
        [InlineData(-1, 1, 1, Rarity.Common, Personality.Glutton)]
        [InlineData(0, 0, 1, Rarity.Common, Personality.Glutton)]
        [InlineData(0, 6, 1, Rarity.Common, Personality.Glutton)]
        [InlineData(0, 1, 0, Rarity.Common, Personality.Glutton)]
        [InlineData(0, 1, 101, Rarity.Common, Personality.Glutton)]
        [InlineData(0, 1, 1, (Rarity)99, Personality.Glutton)]
        [InlineData(0, 1, 1, Rarity.Common, (Personality)99)]
        public void TrainerSnapshotRejectsInvalidIdentityProgressionAndEnums(int id, int rank, int level, Rarity rarity, Personality personality)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerSnapshot(id, rank, level, rarity, personality, new TrainerAttributes(1, 1, 1, 1)));
        }

        [Fact]
        public void TrainerSnapshotRejectsMissingTrainerOrAttributes()
        {
            Assert.Throws<ArgumentNullException>(() => TrainerSnapshot.FromTrainer(null));
            Assert.Throws<ArgumentNullException>(() => new TrainerSnapshot(0, 1, 1, Rarity.Common, Personality.Glutton, null));
        }
    }
}
