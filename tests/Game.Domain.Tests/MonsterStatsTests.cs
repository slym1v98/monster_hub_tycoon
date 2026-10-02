using System;
using System.Linq;
using Game.Domain;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class MonsterStatsTests
    {
        static MonsterDefinition Definition(string id = "test") => new MonsterDefinition(id, "Test",
            MonsterElement.Fire, MonsterRole.Dps, new MonsterStats(100, 20, 10, 1, 0.1),
            new MonsterStats(10, 2, 1, 0.1, 0.01));
        static MonsterStatConfig UnitRarity => new MonsterStatConfig(new[] { 1.0, 1.0, 1.0, 1.0, 1.0 });

        [Theory]
        [InlineData(MonsterIvGrade.D, 0.8)]
        [InlineData(MonsterIvGrade.C, 0.9)]
        [InlineData(MonsterIvGrade.B, 1.0)]
        [InlineData(MonsterIvGrade.A, 1.1)]
        [InlineData(MonsterIvGrade.S, 1.2)]
        [InlineData(MonsterIvGrade.SS, 1.3)]
        [InlineData(MonsterIvGrade.SSS, 1.5)]
        public void IvUsesExactGddMultiplierForGrowth(MonsterIvGrade iv, double multiplier)
        {
            var stats = MonsterStatsCalculator.Calculate(Definition(), Rarity.Common, iv, 11, UnitRarity);
            Assert.Equal((long)(100 + 100 * multiplier), stats.Hp);
            Assert.Equal(20 + 20 * multiplier, stats.Attack, 10);
            Assert.Equal(10 + 10 * multiplier, stats.Defense, 10);
            Assert.Equal(1 + multiplier, stats.AttackSpeed, 10);
            Assert.Equal(0.1 + 0.1 * multiplier, stats.CriticalChance, 10);
            Assert.Equal(Definition().BaseStats,
                MonsterStatsCalculator.Calculate(Definition(), Rarity.Common, iv, 1, UnitRarity));
        }

        [Theory]
        [InlineData(Rarity.Common, 1)]
        [InlineData(Rarity.Rare, 2)]
        [InlineData(Rarity.Epic, 3)]
        [InlineData(Rarity.Legendary, 4)]
        [InlineData(Rarity.Ultimate, 5)]
        public void ConfigAndCatalogControlStatsWithoutSpeciesArithmetic(Rarity rarity, double factor)
        {
            var config = new MonsterStatConfig(new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }, growthFactor: 2);
            var stats = MonsterStatsCalculator.Calculate(Definition(), rarity, MonsterIvGrade.B, 6, config);
            Assert.Equal((long)(200 * factor), stats.Hp);
            Assert.Equal(40 * factor, stats.Attack, 10);
            Assert.Equal(20 * factor, stats.Defense, 10);
            Assert.Equal(2 * factor, stats.AttackSpeed, 10);
            Assert.Equal(Math.Min(1, 0.2 * factor), stats.CriticalChance, 10);
            Assert.Equal(stats, MonsterStatsCalculator.Calculate(Definition("other_species"), rarity,
                MonsterIvGrade.B, 6, config));
        }

        [Fact]
        public void HpRoundsUpToPositiveIntegerAndCritIsClamped()
        {
            var definition = new MonsterDefinition("tiny", "Tiny", MonsterElement.Ice, MonsterRole.Tank,
                new MonsterStats(1, 1, 1, 1, 0.9), new MonsterStats(0, 0, 0, 0, 0.1));
            var config = new MonsterStatConfig(new[] { 0.1, 1.0, 1.0, 1.0, 1.0 });
            var tiny = MonsterStatsCalculator.Calculate(definition, Rarity.Common, MonsterIvGrade.D, 1, config);
            Assert.Equal(1, tiny.Hp);
            var grown = MonsterStatsCalculator.Calculate(definition, Rarity.Rare, MonsterIvGrade.SSS, 100, config);
            Assert.Equal(1, grown.Hp);
            Assert.Equal(1, grown.CriticalChance);
            var fractional = MonsterStatsCalculator.Calculate(Definition(), Rarity.Common, MonsterIvGrade.B, 2,
                new MonsterStatConfig(new[] { 1.001, 1.0, 1.0, 1.0, 1.0 }));
            Assert.Equal(111, fractional.Hp);
        }

        [Fact]
        public void CreationAndLevelChangesUseSameConfigWithoutHealingOrChangingGenes()
        {
            var config = new MonsterStatConfig(new[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
            var monster = Monster.Create(new MonsterId("a"), Definition(), Rarity.Rare, MonsterIvGrade.SSS,
                1, 42, statConfig: config);
            Assert.Equal(200, monster.MaxHp);
            Assert.Equal(200, monster.CurrentHp);
            monster.SetCurrentHp(17);
            var genes = monster.Genes.ToArray();
            var trainer = new Trainer { Level = 6 };
            trainer.Roster.Add(monster);
            Assert.Equal(2, monster.Level);
            Assert.Equal(230, monster.MaxHp);
            Assert.Equal(17, monster.CurrentHp);
            trainer.Level = 100;
            Assert.Equal(770, monster.MaxHp);
            Assert.Equal(17, monster.CurrentHp);
            var stats = monster.Stats;
            trainer.Level = 1;
            Assert.Same(stats, monster.Stats);
            Assert.Equal(20, monster.Level);
            Assert.Equal(genes, monster.Genes);
            monster.SetCurrentHp(0);
            trainer.Rank = 2;
            Assert.Equal(0, monster.CurrentHp);
            Assert.Equal(MonsterLifeState.Fainted, monster.LifeState);
        }

        [Fact]
        public void HubWorldPassesSimConfigToStarterStatDerivation()
        {
            var config = SimConfig.Default;
            config.TrainerCount = 1;
            config.MonsterStatSettings = new MonsterStatConfig(new[] { 2.0, 2.0, 2.0, 2.0, 2.0 });
            var world = new HubWorld(config, 42);
            var trainers = (System.Collections.Generic.List<Trainer>)typeof(HubWorld)
                .GetField("trainers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(world);
            Assert.Equal(600, trainers[0].Roster.Active.MaxHp);
        }

        [Fact]
        public void InvalidStatsInputsAndOverflowAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => MonsterStatsCalculator.Calculate(null, Rarity.Common,
                MonsterIvGrade.B, 1, UnitRarity));
            Assert.Throws<ArgumentNullException>(() => MonsterStatsCalculator.Calculate(Definition(), Rarity.Common,
                MonsterIvGrade.B, 1, null));
            foreach (var level in new[] { 0, 101 })
                Assert.Throws<ArgumentOutOfRangeException>(() => MonsterStatsCalculator.Calculate(Definition(),
                    Rarity.Common, MonsterIvGrade.B, level, UnitRarity));
            Assert.Throws<ArgumentOutOfRangeException>(() => MonsterStatsCalculator.Calculate(Definition(),
                (Rarity)5, MonsterIvGrade.B, 1, UnitRarity));
            Assert.Throws<ArgumentOutOfRangeException>(() => MonsterStatsCalculator.Calculate(Definition(),
                Rarity.Common, (MonsterIvGrade)7, 1, UnitRarity));
            Assert.Throws<ArgumentException>(() => new MonsterStatConfig(new[] { 1.0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterStatConfig(new[] { 0.0, 1, 1, 1, 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterStatConfig(new[] { double.NaN, 1, 1, 1, 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterStatConfig(new[] { 1.0, 1, 1, 1, 1 }, -1));
            var huge = new MonsterStatConfig(new[] { double.MaxValue, 1, 1, 1, 1 });
            Assert.Throws<OverflowException>(() => MonsterStatsCalculator.Calculate(Definition(), Rarity.Common,
                MonsterIvGrade.B, 1, huge));
        }

        [Fact]
        public void ConfigCopiesInputFactorsAndFailedProgressionIsAtomic()
        {
            var factors = new[] { 1.0, 1, 1, 1, 1 };
            var config = new MonsterStatConfig(factors);
            factors[0] = 5;
            Assert.Equal(100, MonsterStatsCalculator.Calculate(Definition(), Rarity.Common, MonsterIvGrade.B,
                1, config).Hp);
            var overflowing = new MonsterDefinition("huge", "Huge", MonsterElement.Water, MonsterRole.Tank,
                new MonsterStats(1, 1, 1, 1, 0), new MonsterStats(long.MaxValue, 0, 0, 0, 0));
            var trainer = new Trainer();
            trainer.Roster.Add(Monster.Create(new MonsterId("z"), overflowing, Rarity.Common, MonsterIvGrade.SSS, 1, 1));
            trainer.Roster.Add(MonsterRosterTests.Make("a"));
            Assert.Throws<OverflowException>(() => TrainerProgression.AddExperience(trainer, 750,
                TrainerProgressionConfig.Prototype));
            Assert.Equal(1, trainer.Level);
            Assert.Equal(0, trainer.Experience);
            Assert.All(trainer.Roster.Members, m => Assert.Equal(1, m.Level));
        }
    }
}
