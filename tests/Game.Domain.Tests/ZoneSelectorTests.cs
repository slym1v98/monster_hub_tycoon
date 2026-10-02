using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Domain;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class ZoneSelectorTests
    {
        static readonly IReadOnlyList<ZoneIncomeModifier> NoModifiers = Array.Empty<ZoneIncomeModifier>();
        static TrainerSnapshot Trainer(int rank = 5, Personality personality = Personality.Glutton, double luck = 0) =>
            new TrainerSnapshot(0, rank, 1, Rarity.Common, personality, new TrainerAttributes(10, luck, 100, 20));

        internal static ZoneDefinition Zone(string id, int rank = 1, double gold = 100, int walk = 0,
            double materialUnits = 0, double materialValue = 0, double encounters = 1) => new ZoneDefinition(
                id, id, rank, walk,
                new[] { new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 1, materialValue) },
                new EncounterProfile(new Dictionary<MonsterElement, double> { [MonsterElement.Grass] = 1 },
                    encounters, gold, materialUnits, 10));

        [Fact]
        public void RankFilteringPrecedesIncomeAndModifiers()
        {
            var eligible = Zone("zone_1", gold: 1);
            var tooHigh = Zone("zone_2", rank: 2, gold: double.MaxValue);
            var selected = new ZoneSelector().Select(Trainer(rank: 1), new[] { tooHigh, eligible },
                new[] { new ZoneIncomeModifier("zone_2", double.MaxValue, true) });
            Assert.Same(eligible, selected);
        }

        [Theory]
        [InlineData(1, "zone_1")]
        [InlineData(2, "zone_2")]
        [InlineData(3, "zone_3")]
        [InlineData(4, "zone_4")]
        [InlineData(5, "zone_5")]
        public void EachRankCanReachItsZoneButNoHigherRank(int rank, string expectedId)
        {
            var zones = new List<ZoneDefinition>();
            for (int n = 1; n <= 5; n++) zones.Add(Zone($"zone_{n}", n, gold: n * 100));
            Assert.Equal(expectedId, new ZoneSelector().Select(Trainer(rank), zones, NoModifiers).Id);
        }

        [Fact]
        public void UnlockedInputIsTheOnlyCandidateSetEvenWithALockedZoneModifier()
        {
            var unlocked = Zone("zone_1", gold: 1);
            Assert.Same(unlocked, new ZoneSelector().Select(Trainer(), new[] { unlocked },
                new[] { new ZoneIncomeModifier("zone_5", 10000, true) }));
        }

        [Fact]
        public void NoEligibleZoneReturnsNullWithoutScoring()
        {
            var selector = new ZoneSelector();
            Assert.Null(selector.Select(Trainer(), Array.Empty<ZoneDefinition>(), NoModifiers));
            Assert.Null(selector.Select(Trainer(1), new[] { Zone("zone_2", 2, double.MaxValue) }, NoModifiers));
        }

        [Fact]
        public void SelectionMaximizesGoldEquivalentInsteadOfRankOrRawGold()
        {
            var gold = Zone("zone_5", 5, gold: 100);
            var materials = Zone("zone_1", gold: 1, materialUnits: 10, materialValue: 20);
            Assert.Same(materials, new ZoneSelector().Select(Trainer(), new[] { gold, materials }, NoModifiers));
        }

        [Fact]
        public void RoundTripWalkingChangesExpectedIncomePerHour()
        {
            var far = Zone("zone_2", gold: 200, walk: 60);
            var near = Zone("zone_1", gold: 100);
            var selector = new ZoneSelector();
            Assert.Equal(200.0 / 3, selector.ExpectedGoldEquivalentPerHour(Trainer(), far, NoModifiers), 8);
            Assert.Same(near, selector.Select(Trainer(), new[] { far, near }, NoModifiers));
        }

        [Fact]
        public void MaterialWeightsAreNormalizedAndEncounterRateScalesIncome()
        {
            var zone = new ZoneDefinition("weighted", "Weighted", 1, 0,
                new[] { new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 3, 10),
                    new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Herb, 1), 1, 30) },
                new EncounterProfile(new Dictionary<MonsterElement, double> { [MonsterElement.Grass] = 1 },
                    2, 5, 4, 10));
            // 2 × (5 + 4 × ((3 × 10 + 1 × 30) / 4) × 0.85) = 112.
            Assert.Equal(112, new ZoneSelector().ExpectedGoldEquivalentPerHour(Trainer(), zone, NoModifiers), 8);
        }

        [Fact]
        public void LuckChangesGoldAndPersonalityChangesMaterialPickup()
        {
            var gold = Zone("gold", gold: 100);
            var material = Zone("material", gold: 0, materialUnits: 10, materialValue: 11);
            var selector = new ZoneSelector();
            Assert.Same(gold, selector.Select(Trainer(), new[] { gold, material }, NoModifiers));
            Assert.Same(material, selector.Select(Trainer(personality: Personality.Capitalist), new[] { gold, material }, NoModifiers));
            Assert.Equal(120, selector.ExpectedGoldEquivalentPerHour(Trainer(luck: 20), gold, NoModifiers), 8);
        }

        [Fact]
        public void CapitalistBiasAppliesOnlyToExplicitBountyIncome()
        {
            var ordinary = Zone("zone_1", gold: 100);
            var target = Zone("zone_2", gold: 80);
            var selector = new ZoneSelector();
            var bounty = new[] { new ZoneIncomeModifier("zone_2", 15, true) };
            Assert.Same(ordinary, selector.Select(Trainer(), new[] { ordinary, target }, bounty));
            Assert.Same(target, selector.Select(Trainer(personality: Personality.Capitalist), new[] { ordinary, target }, bounty));
            Assert.Same(ordinary, selector.Select(Trainer(personality: Personality.Capitalist), new[] { ordinary, target },
                new[] { new ZoneIncomeModifier("zone_2", 15, false) }));
            Assert.Same(ordinary, selector.Select(Trainer(personality: Personality.Capitalist), new[] { ordinary, target }, NoModifiers));
        }

        [Fact]
        public void ModifiersUseTheirZoneIdAndApplyBeforeTravelAmortization()
        {
            var target = Zone("zone_2", gold: 40, walk: 30);
            var modifiers = new[] { new ZoneIncomeModifier("other", 1000, true),
                new ZoneIncomeModifier("zone_2", 10, false), new ZoneIncomeModifier("zone_2", 10, false) };
            Assert.Equal(30, new ZoneSelector().ExpectedGoldEquivalentPerHour(Trainer(), target, modifiers), 8);
        }

        [Fact]
        public void TieBreakIsOrdinalZoneIdIndependentOfInputOrderAndCulture()
        {
            var a = Zone("I");
            var b = Zone("ı");
            var selector = new ZoneSelector();
            var before = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                Assert.Same(a, selector.Select(Trainer(), new[] { b, a }, NoModifiers));
                Assert.Same(a, selector.Select(Trainer(), new[] { a, b }, NoModifiers));
            }
            finally { CultureInfo.CurrentCulture = before; }
        }

        [Fact]
        public void ModifierOrderCannotChangeFloatingPointIncomeOrChoice()
        {
            var target = Zone("zone_2", gold: 0);
            var other = Zone("zone_1", gold: 10000000000000002d);
            var large = new ZoneIncomeModifier("zone_2", 10000000000000000d, false);
            var small = new ZoneIncomeModifier("zone_2", 1, false);
            var selector = new ZoneSelector();
            var first = new[] { large, small, small };
            var second = new[] { small, small, large };
            Assert.Equal(selector.ExpectedGoldEquivalentPerHour(Trainer(), target, first),
                selector.ExpectedGoldEquivalentPerHour(Trainer(), target, second));
            Assert.Equal(selector.Select(Trainer(), new[] { other, target }, first).Id,
                selector.Select(Trainer(), new[] { target, other }, second).Id);
        }

        [Fact]
        public void RejectsNullInputsNullEntriesAndDuplicateZoneIds()
        {
            var selector = new ZoneSelector();
            var zone = Zone("zone_1");
            Assert.Throws<ArgumentNullException>(() => selector.Select(null, new[] { zone }, NoModifiers));
            Assert.Throws<ArgumentNullException>(() => selector.Select(Trainer(), null, NoModifiers));
            Assert.Throws<ArgumentNullException>(() => selector.Select(Trainer(), new[] { zone }, null));
            Assert.Throws<ArgumentException>(() => selector.Select(Trainer(), new ZoneDefinition[] { null }, NoModifiers));
            Assert.Throws<ArgumentException>(() => selector.Select(Trainer(), new[] { zone, zone }, NoModifiers));
            Assert.Throws<ArgumentException>(() => selector.Select(Trainer(), new[] { zone }, new ZoneIncomeModifier[] { null }));
            Assert.Throws<ArgumentNullException>(() => selector.ExpectedGoldEquivalentPerHour(Trainer(), null, NoModifiers));
            Assert.Throws<ArgumentException>(() => selector.ExpectedGoldEquivalentPerHour(Trainer(1), Zone("zone_2", 2), NoModifiers));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void RejectsUnusableModifierIncome(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneIncomeModifier("zone_1", value, true));
        }

        [Fact]
        public void RejectsEmptyModifierIdAndNonfiniteComputedIncome()
        {
            Assert.Throws<ArgumentException>(() => new ZoneIncomeModifier(" ", 1, false));
            Assert.Throws<OverflowException>(() => new ZoneSelector().Select(Trainer(),
                new[] { Zone("zone_1", gold: double.MaxValue, encounters: 2) }, NoModifiers));
        }
    }
}
