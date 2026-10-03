using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Domain;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class MonsterRosterTests
    {
        internal static Monster Make(string id, int seed = 42) => Monster.Create(
            new MonsterId(id), MonsterCatalog.Default.Definitions[0], Rarity.Common, MonsterIvGrade.B, 1, seed);

        [Fact]
        public void EmptyRosterHasNoActiveOrOwnedMonsters()
        {
            var roster = new MonsterRoster();
            Assert.Null(roster.Active);
            Assert.Empty(roster.Members);
            Assert.Empty(roster.Reserves);
            Assert.Empty(roster.Storage);
        }

        [Fact]
        public void FirstMonsterBecomesActiveAndOthersAreReserves()
        {
            var roster = new MonsterRoster();
            var first = Make("c");
            roster.Add(first);
            roster.Add(Make("b"));
            roster.Add(Make("a"));
            Assert.Same(first, roster.Active);
            Assert.Equal(new[] { "a", "b", "c" }, roster.Members.Select(x => x.Id.Value));
            Assert.Equal(new[] { "a", "b" }, roster.Reserves.Select(x => x.Id.Value));
        }

        [Fact]
        public void SetActiveSwapsSlotsWithoutChangingOwnership()
        {
            var roster = new MonsterRoster();
            var first = Make("a");
            var second = Make("b");
            roster.Add(first);
            roster.Add(second);
            roster.SetActive(second.Id);
            roster.SetActive(second.Id);
            Assert.Same(second, roster.Active);
            Assert.Same(first, Assert.Single(roster.Reserves));
            Assert.Equal(2, roster.Members.Count);
            Assert.Throws<InvalidOperationException>(() => roster.SetActive(new MonsterId("missing")));
            Assert.Same(second, roster.Active);
        }

        [Fact]
        public void DuplicateIdAndFourthMemberAreRejectedAtomically()
        {
            var roster = new MonsterRoster();
            var first = Make("a");
            roster.Add(first);
            Assert.Throws<InvalidOperationException>(() => roster.Add(first));
            Assert.Throws<InvalidOperationException>(() => roster.Add(Make("a")));
            roster.Add(Make("b"));
            roster.Add(Make("c"));
            var fourth = Make("d");
            Assert.Throws<InvalidOperationException>(() => roster.Add(fourth));
            Assert.Equal(3, roster.Members.Count);
            Assert.Same(first, roster.Active);
            var other = new MonsterRoster();
            other.Add(fourth);
            Assert.Same(fourth, other.Active);
        }

        [Fact]
        public void SameInstanceCannotBeOwnedByTwoRosters()
        {
            var monster = Make("a");
            var first = new MonsterRoster();
            var second = new MonsterRoster();
            first.Add(monster);
            Assert.Throws<InvalidOperationException>(() => second.Add(monster));
            first.MoveToStorage(monster.Id);
            Assert.Throws<InvalidOperationException>(() => second.Add(monster));
            Assert.Empty(second.Members);
        }

        [Fact]
        public void StorageRoundTripPreservesIdentityGenesAndHp()
        {
            var roster = new MonsterRoster();
            var monster = Make("a");
            monster.SetCurrentHp(123);
            var genes = monster.Genes.ToArray();
            roster.Add(monster);
            roster.MoveToStorage(monster.Id);
            Assert.Empty(roster.Members);
            Assert.Null(roster.Active);
            Assert.Same(monster, Assert.Single(roster.Storage));
            Assert.Equal(MonsterLifeState.Stored, monster.LifeState);
            Assert.Throws<InvalidOperationException>(() => roster.Add(Make("a")));
            roster.RestoreFromStorage(monster.Id);
            Assert.Empty(roster.Storage);
            Assert.Same(monster, roster.Active);
            Assert.Equal(123, monster.CurrentHp);
            Assert.Equal(genes, monster.Genes);
            Assert.Equal(MonsterLifeState.Ready, monster.LifeState);
        }

        [Fact]
        public void StoringActivePromotesHealthyReserveByStableId()
        {
            var roster = new MonsterRoster();
            var active = Make("z");
            var fainted = Make("a");
            fainted.SetCurrentHp(0);
            roster.Add(active);
            roster.Add(Make("c"));
            roster.Add(fainted);
            roster.MoveToStorage(active.Id);
            Assert.Equal("c", roster.Active.Id.Value);
            Assert.Equal("a", Assert.Single(roster.Reserves).Id.Value);
        }

        [Fact]
        public void AllFaintedRosterKeepsExactlyOneActiveSlotAndOwnedMonsters()
        {
            var roster = new MonsterRoster();
            var active = Make("z");
            var reserve = Make("a");
            active.SetCurrentHp(0);
            reserve.SetCurrentHp(0);
            roster.Add(active);
            roster.Add(reserve);
            Assert.Same(active, roster.Active);
            Assert.Throws<InvalidOperationException>(() => roster.SetActive(reserve.Id));
            roster.MoveToStorage(active.Id);
            Assert.Same(reserve, roster.Active);
            roster.RestoreFromStorage(active.Id);
            Assert.Equal(2, roster.Members.Count);
            Assert.All(roster.Members, x => Assert.Equal(MonsterLifeState.Fainted, x.LifeState));
        }

        [Fact]
        public void FullRosterCannotRestoreOrLoseStoredMonster()
        {
            var roster = new MonsterRoster();
            var stored = Make("stored");
            roster.Add(stored);
            roster.MoveToStorage(stored.Id);
            foreach (var id in new[] { "a", "b", "c" }) roster.Add(Make(id));
            Assert.Throws<InvalidOperationException>(() => roster.RestoreFromStorage(stored.Id));
            Assert.Same(stored, Assert.Single(roster.Storage));
            Assert.Equal(MonsterLifeState.Stored, stored.LifeState);
            Assert.Equal(3, roster.Members.Count);
            Assert.Throws<InvalidOperationException>(() => roster.MoveToStorage(new MonsterId("missing")));
            Assert.Throws<InvalidOperationException>(() => roster.RestoreFromStorage(new MonsterId("missing")));
        }

        [Fact]
        public void CollectionsCannotBypassRosterInvariants()
        {
            var roster = new MonsterRoster();
            roster.Add(Make("a"));
            Assert.Throws<NotSupportedException>(() => ((IList<Monster>)roster.Members).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<Monster>)roster.Storage).Add(Make("b")));
            Assert.Throws<ArgumentNullException>(() => roster.Add(null));
        }

        [Fact]
        public void TrainerDoesNotExposeAggregateHpAndRosterOwnsEveryMonster()
        {
            var trainer = new Trainer();
            var first = Make("a");
            var second = Make("b");
            trainer.Roster.Add(first);
            trainer.Roster.Add(second);
            first.SetCurrentHp(100);
            Assert.Equal(100, first.CurrentHp);
            trainer.Roster.MoveToStorage(second.Id);
            Assert.Single(trainer.Roster.Members);
            Assert.Single(trainer.Roster.Storage);
            Assert.Null(typeof(Trainer).GetProperty("TeamHp"));
            Assert.Null(typeof(Trainer).GetProperty("TeamHpMax"));
        }

        [Fact]
        public void LegacyDamageAndHealingOperateOnMonstersAndExcludeStorage()
        {
            var roster = new MonsterRoster();
            var active = Make("z");
            var reserve = Make("a");
            var stored = Make("stored");
            roster.Add(active);
            roster.Add(reserve);
            roster.Add(stored);
            roster.MoveToStorage(stored.Id);
            roster.ApplyDamage(350);
            Assert.Equal(0, active.CurrentHp);
            Assert.Equal(MonsterLifeState.Fainted, active.LifeState);
            Assert.Equal(250, reserve.CurrentHp);
            Assert.Equal(300, stored.CurrentHp);
            roster.ApplyDamage(long.MaxValue);
            Assert.All(roster.Members, x => Assert.Equal(0, x.CurrentHp));
            roster.RestoreAllHp();
            Assert.All(roster.Members, x => Assert.Equal(300, x.CurrentHp));
            Assert.Throws<ArgumentOutOfRangeException>(() => roster.ApplyDamage(-1));
        }

        [Fact]
        public void MonsterGenerationIsSeededAndHpRemainsValid()
        {
            var first = Make("a", 42);
            var replay = Make("a", 42);
            var different = Make("a", 43);
            Assert.Equal(first.Genes, replay.Genes);
            Assert.False(first.Genes.SequenceEqual(different.Genes));
            Assert.All(first.Genes, gene => Assert.InRange(gene, 0, Math.BitDecrement(1.0)));
            Assert.Throws<NotSupportedException>(() => ((IList<double>)first.Genes).Clear());
            Assert.Throws<ArgumentOutOfRangeException>(() => first.SetCurrentHp(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => first.SetCurrentHp(301));
            Assert.Equal(300, first.CurrentHp);
        }

        [Fact]
        public void HubWorldCreatesOneSoulBoundMatchingRarityStarterPerTrainer()
        {
            var config = SimConfig.Default;
            config.TrainerCount = 3;
            config.StarterMonsterHp = 450;
            var world = new HubWorld(config, 123);
            var trainers = GetTrainers(world);
            Assert.Equal(3, trainers.Count);
            Assert.All(trainers, trainer =>
            {
                var starter = Assert.Single(trainer.Roster.Members);
                Assert.Same(starter, trainer.Roster.Active);
                Assert.True(starter.IsSoulBound);
                Assert.Equal(trainer.Rarity, starter.Rarity);
                Assert.Equal(1, starter.Level);
                Assert.Equal(450, starter.CurrentHp);
            });
            Assert.Equal(3, trainers.Select(x => x.Roster.Active.Id).Distinct().Count());
        }

        [Fact]
        public void StarterGenesDependOnWorldSeedAndTrainerIdNotTrainerCountOrPersonalityDraws()
        {
            var config = SimConfig.Default;
            config.TrainerCount = 3;
            var original = GetTrainers(new HubWorld(config, 123));
            config.TrainerCount = 5;
            config.ForcedPersonality = Personality.Timid;
            var replay = GetTrainers(new HubWorld(config, 123));
            var differentSeed = GetTrainers(new HubWorld(config, 124));
            for (var i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i].Roster.Active.Id, replay[i].Roster.Active.Id);
                Assert.Equal(original[i].Roster.Active.Genes, replay[i].Roster.Active.Genes);
                Assert.False(original[i].Roster.Active.Genes.SequenceEqual(differentSeed[i].Roster.Active.Genes));
            }
            Assert.False(original[0].Roster.Active.Genes.SequenceEqual(original[1].Roster.Active.Genes));
        }

        // Truy cập dữ liệu nội bộ để kiểm tra khởi tạo; API ảnh chụp Monster thuộc tác vụ sau.
        static List<Trainer> GetTrainers(HubWorld world) => (List<Trainer>)typeof(HubWorld)
            .GetField("trainers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(world);
    }
}
