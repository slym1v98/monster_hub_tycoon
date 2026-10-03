using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Domain;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class MonsterProgressionTests
    {
        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(1, 5, 1)]
        [InlineData(1, 6, 2)]
        [InlineData(1, 100, 20)]
        [InlineData(2, 1, 21)]
        [InlineData(2, 5, 21)]
        [InlineData(2, 6, 22)]
        [InlineData(2, 100, 40)]
        [InlineData(3, 1, 41)]
        [InlineData(3, 5, 41)]
        [InlineData(3, 6, 42)]
        [InlineData(3, 100, 60)]
        [InlineData(4, 1, 61)]
        [InlineData(4, 5, 61)]
        [InlineData(4, 6, 62)]
        [InlineData(4, 100, 80)]
        [InlineData(5, 1, 81)]
        [InlineData(5, 5, 81)]
        [InlineData(5, 6, 82)]
        [InlineData(5, 100, 100)]
        public void OwnedMonsterTracksTrainerRankAndLevel(int rank, int level, int expected)
        {
            var trainer = new Trainer();
            var monster = MonsterRosterTests.Make("starter");
            trainer.Roster.Add(monster);
            trainer.Rank = rank;
            trainer.Level = level;
            Assert.Equal(expected, monster.Level);
        }

        [Fact]
        public void RankChangeAndLevelResetNeverDemoteExistingOrStoredMonsters()
        {
            var trainer = new Trainer();
            var active = MonsterRosterTests.Make("active");
            var stored = MonsterRosterTests.Make("stored");
            trainer.Roster.Add(active);
            trainer.Roster.Add(stored);
            trainer.Roster.MoveToStorage(stored.Id);
            trainer.Level = 100;
            Assert.Equal(20, active.Level);
            Assert.Equal(20, stored.Level);
            trainer.Level = 1;
            Assert.Equal(20, active.Level);
            trainer.Rank = 2;
            Assert.Equal(21, active.Level);
            Assert.Equal(21, stored.Level);
            trainer.Rank = 1;
            Assert.Equal(21, active.Level);
            Assert.Equal(21, stored.Level);
        }

        [Fact]
        public void NewMonsterUsesCurrentTrainerLevelEvenWhenExistingLevelIsHigher()
        {
            var trainer = new Trainer { Rank = 3, Level = 6 };
            trainer.Roster.Add(MonsterRosterTests.Make("old"));
            Assert.Equal(42, trainer.Roster.Active.Level);
            trainer.Level = 1;
            var acquired = MonsterRosterTests.Make("new");
            trainer.Roster.Add(acquired);
            Assert.Equal(41, acquired.Level);
            Assert.Equal(42, trainer.Roster.Active.Level);
        }
        [Theory]
        [InlineData(1, 1, 1, 0.2)]
        [InlineData(1, 5, 1, 1.0)]
        [InlineData(1, 6, 2, 1.2)]
        [InlineData(1, 100, 20, 20.0)]
        [InlineData(2, 1, 21, 20.2)]
        [InlineData(2, 5, 21, 21.0)]
        [InlineData(2, 6, 22, 21.2)]
        [InlineData(2, 100, 40, 40.0)]
        [InlineData(3, 1, 41, 40.2)]
        [InlineData(3, 5, 41, 41.0)]
        [InlineData(3, 6, 42, 41.2)]
        [InlineData(3, 100, 60, 60.0)]
        [InlineData(4, 1, 61, 60.2)]
        [InlineData(4, 5, 61, 61.0)]
        [InlineData(4, 6, 62, 61.2)]
        [InlineData(4, 100, 80, 80.0)]
        [InlineData(5, 1, 81, 80.2)]
        [InlineData(5, 5, 81, 81.0)]
        [InlineData(5, 6, 82, 81.2)]
        [InlineData(5, 100, 100, 100.0)]
        public void ConversionsRespectCeilingAndFractionalManagementScale(int rank, int level,
            int monsterLevel, double managementLevel)
        {
            Assert.Equal(monsterLevel, MonsterProgression.MonsterLevel(rank, level));
            Assert.Equal(managementLevel, MonsterProgression.TrainerManagementLevel(rank, level), 10);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(6, 1)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public void InvalidProgressionInputsAreRejectedWithoutChangingTrainer(int rank, int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MonsterProgression.MonsterLevel(rank, level));
            Assert.Throws<ArgumentOutOfRangeException>(() => MonsterProgression.TrainerManagementLevel(rank, level));
            var trainer = new Trainer();
            if (rank < 1 || rank > 5)
                Assert.Throws<ArgumentOutOfRangeException>(() => trainer.Rank = rank);
            else
                Assert.Throws<ArgumentOutOfRangeException>(() => trainer.Level = level);
            Assert.Equal(1, trainer.Rank);
            Assert.Equal(1, trainer.Level);
        }

        [Fact]
        public void AttributesAreSeededOnceAndClassDefaultsToNone()
        {
            var first = new Trainer(new SimRandom(42));
            var replay = new Trainer(new SimRandom(42));
            var different = new Trainer(new SimRandom(43));
            Assert.Equal(first.Attributes, replay.Attributes);
            Assert.NotEqual(first.Attributes, different.Attributes);
            Assert.Same(first.Attributes, first.Attributes);
            Assert.InRange(first.Attributes.Dexterity, 8, 12);
            Assert.InRange(first.Attributes.Luck, 8, 12);
            Assert.InRange(first.Attributes.Endurance, 90, 110);
            Assert.InRange(first.Attributes.Leadership, 18, 22);
            Assert.Equal(TrainerClass.None, first.Class);
            Assert.Equal(new[] { TrainerClass.None, TrainerClass.Medic, TrainerClass.Commander,
                TrainerClass.Engineer, TrainerClass.Trapper }, Enum.GetValues<TrainerClass>());
            Assert.Null(typeof(Trainer).GetProperty("Hp"));
            Assert.Null(typeof(Trainer).GetField("Hp"));
        }

        [Fact]
        public void AttributeGenerationUsesConfiguredRangesInFixedOrder()
        {
            var config = new TrainerAttributeConfig(new TrainerAttributeRange(1, 2),
                new TrainerAttributeRange(3, 4), new TrainerAttributeRange(5, 6), new TrainerAttributeRange(7, 8));
            var random = new SimRandom(42);
            var expected = new SimRandom(42);
            var attributes = TrainerAttributes.Generate(random, config);
            Assert.Equal(1 + expected.NextDouble(), attributes.Dexterity);
            Assert.Equal(3 + expected.NextDouble(), attributes.Luck);
            Assert.Equal(5 + expected.NextDouble(), attributes.Endurance);
            Assert.Equal(7 + expected.NextDouble(), attributes.Leadership);
            Assert.Equal(expected.State, random.State);
        }

        [Fact]
        public void HubWorldInitializesAttributesFromSeedIdAndSimConfig()
        {
            var config = SimConfig.Default;
            config.TrainerCount = 2;
            var first = GetTrainers(new HubWorld(config, 123));
            config.TrainerCount = 3;
            config.ForcedPersonality = Personality.Timid;
            var replay = GetTrainers(new HubWorld(config, 123));
            var different = GetTrainers(new HubWorld(config, 124));
            Assert.Equal(first[0].Attributes, replay[0].Attributes);
            Assert.Equal(first[1].Attributes, replay[1].Attributes);
            Assert.NotEqual(first[0].Attributes, first[1].Attributes);
            Assert.NotEqual(first[0].Attributes, different[0].Attributes);
            config.TrainerAttributeSettings = new TrainerAttributeConfig(new TrainerAttributeRange(2, 2),
                new TrainerAttributeRange(3, 3), new TrainerAttributeRange(4, 4), new TrainerAttributeRange(5, 5));
            Assert.Equal(new TrainerAttributes(2, 3, 4, 5), GetTrainers(new HubWorld(config, 123))[0].Attributes);
        }

        [Fact]
        public void ExperienceCrossesExactThresholdAndRetainsRemainder()
        {
            var trainer = new Trainer { Id = 7 };
            var config = TrainerProgressionConfig.Prototype;
            Assert.Empty(TrainerProgression.AddExperience(trainer, 99, config));
            Assert.Equal(1, trainer.Level);
            Assert.Equal(99, trainer.Experience);
            var change = Assert.Single(TrainerProgression.AddExperience(trainer, 1, config));
            Assert.Equal(new TrainerLevelChanged(7, 1, 1, 2), change);
            Assert.Equal(2, trainer.Level);
            Assert.Equal(0, trainer.Experience);
            Assert.Empty(TrainerProgression.AddExperience(trainer, 124, config));
            change = Assert.Single(TrainerProgression.AddExperience(trainer, 11, config));
            Assert.Equal(3, change.ToLevel);
            Assert.Equal(10, trainer.Experience);
        }

        [Fact]
        public void ExperienceCrossesMultipleLevelsAndSynchronizesMonstersWithoutRerolling()
        {
            var trainer = new Trainer { Rank = 2, Id = 9 };
            var monster = MonsterRosterTests.Make("starter");
            trainer.Roster.Add(monster);
            var genes = monster.Genes.ToArray();
            monster.SetCurrentHp(12);
            var changes = TrainerProgression.AddExperience(trainer, 751, TrainerProgressionConfig.Prototype);
            Assert.Equal(new[] { 2, 3, 4, 5, 6 }, changes.Select(x => x.ToLevel));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, changes.Select(x => x.FromLevel));
            Assert.All(changes, x => { Assert.Equal(9, x.TrainerId); Assert.Equal(2, x.Rank); });
            Assert.Equal(6, trainer.Level);
            Assert.Equal(1, trainer.Experience);
            Assert.Equal(22, monster.Level);
            Assert.Equal(genes, monster.Genes);
            Assert.Equal(12, monster.CurrentHp);
            Assert.Null(typeof(Monster).GetProperty("Experience"));
            Assert.Throws<NotSupportedException>(() => ((IList<TrainerLevelChanged>)changes).Clear());
        }

        [Fact]
        public void ExperienceCurveIsConfigurableAndCapDiscardsExcessWithoutRebirth()
        {
            var trainer = new Trainer { Rank = 5, Level = 99 };
            trainer.Roster.Add(MonsterRosterTests.Make("starter"));
            var config = new TrainerProgressionConfig(baseExperience: 10, experiencePerLevel: 2);
            Assert.Empty(TrainerProgression.AddExperience(trainer, 205, config));
            Assert.Equal(205, trainer.Experience);
            var change = Assert.Single(TrainerProgression.AddExperience(trainer, long.MaxValue, config));
            Assert.Equal(100, change.ToLevel);
            Assert.Equal(100, trainer.Level);
            Assert.Equal(100, trainer.Roster.Active.Level);
            Assert.Equal(5, trainer.Rank);
            Assert.Equal(0, trainer.Experience);
            Assert.Empty(TrainerProgression.AddExperience(trainer, long.MaxValue, config));
            Assert.Equal(100, trainer.Level);
            Assert.Equal(0, trainer.Experience);
        }

        [Fact]
        public void ExperienceAwardsRejectNegativeAmountsAndZeroDoesNothing()
        {
            var trainer = new Trainer();
            Assert.Throws<ArgumentOutOfRangeException>(() => TrainerProgression.AddExperience(trainer, -1,
                TrainerProgressionConfig.Prototype));
            Assert.Throws<ArgumentNullException>(() => TrainerProgression.AddExperience(null, 1,
                TrainerProgressionConfig.Prototype));
            Assert.Throws<ArgumentNullException>(() => TrainerProgression.AddExperience(trainer, 1, null));
            Assert.Empty(TrainerProgression.AddExperience(trainer, 0, TrainerProgressionConfig.Prototype));
            Assert.Equal(1, trainer.Level);
            Assert.Equal(0, trainer.Experience);
        }

        [Fact]
        public void InvalidPrototypeConfigurationIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerProgressionConfig(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerProgressionConfig(1, -1));
            Assert.Throws<OverflowException>(() => new TrainerProgressionConfig(long.MaxValue, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerAttributeRange(-1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerAttributeRange(2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerAttributeRange(0, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerAttributeRange(0, double.PositiveInfinity));
            Assert.Throws<ArgumentNullException>(() => TrainerAttributes.Generate(null, TrainerAttributeConfig.Prototype));
            Assert.Throws<ArgumentNullException>(() => TrainerAttributes.Generate(new SimRandom(1), null));
        }

        static List<Trainer> GetTrainers(HubWorld world) => (List<Trainer>)typeof(HubWorld)
            .GetField("trainers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(world);
    }
}
