using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Combat;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class BattleResolverTests
    {
        internal static MonsterSnapshot Monster(string id, double attack = 10, double defense = 10,
            double speed = 10, double crit = 0, MonsterElement element = MonsterElement.Light,
            long hp = 1000, long? currentHp = null, string[] skills = null,
            IReadOnlyDictionary<string, int> cooldowns = null) => new MonsterSnapshot(
                new MonsterId(id), element, new MonsterStats(hp, attack, defense, speed, crit),
                currentHp ?? hp, skills ?? new[] { "strike" }, cooldowns);

        internal static CombatConfig Config(double power = 10, int cooldown = 0, int rounds = 1,
            double criticalMultiplier = 2, MonsterElement element = MonsterElement.Fire,
            SkillTargetRule targets = SkillTargetRule.SingleEnemy) => new CombatConfig(
                new SkillCatalog(new[] { new SkillDefinition("strike", element, power, cooldown, targets) }),
                criticalMultiplier, rounds);

        internal static BattleResult Resolve(MonsterSnapshot actor, MonsterSnapshot target, CombatConfig config,
            int seed = 42) => BattleResolver.Resolve(new BattleInput(new[] { actor }, new[] { target }), config, new SimRandom(seed));

        [Theory]
        [InlineData(2.9, 3, 2, 4)]
        [InlineData(0.1, 1, 100, 1)]
        [InlineData(0, 10, 10, 1)]
        [InlineData(3, 2, 0, 6)]
        public void DamageFloorsOnceAndHasMinimumOne(double power, double attack, double defense, long expected)
        {
            var result = Resolve(Monster("a", attack: attack), Monster("b", defense: defense), Config(power));
            var action = result.Actions.First(x => x.Kind == BattleActionKind.Skill);
            Assert.Equal(expected, action.Damage);
            Assert.Equal(1000 - expected, action.ResultingHp);
        }

        [Theory]
        [InlineData(0, 10, false)]
        [InlineData(1, 25, true)]
        [InlineData(0.59, 10, false)]
        [InlineData(0.5940674489353119, 10, false)]
        [InlineData(0.6, 25, true)]
        public void CriticalChanceBoundariesUseConfiguredMultiplier(double chance, long expected, bool critical)
        {
            var result = Resolve(Monster("a", crit: chance), Monster("b"), Config(criticalMultiplier: 2.5));
            var action = result.Actions.First(x => x.Kind == BattleActionKind.Skill);
            Assert.Equal(expected, action.Damage);
            Assert.Equal(critical, action.IsCritical);
        }

        [Theory]
        [InlineData(MonsterElement.Grass, 2, 21)]
        [InlineData(MonsterElement.Water, 0.5, 5)]
        [InlineData(MonsterElement.Light, 1, 10)]
        public void SkillElementMultiplierAppliesBeforeFloor(MonsterElement defense, double multiplier, long damage)
        {
            var result = Resolve(Monster("a", element: MonsterElement.Dark), Monster("b", element: defense), Config(power: 10.7));
            var action = result.Actions.First(x => x.Kind == BattleActionKind.Skill);
            Assert.Equal(multiplier, action.Effectiveness);
            Assert.Equal(damage, action.Damage);
        }

        [Fact]
        public void InitiativeUsesDescendingSpeedThenOrdinalId()
        {
            var input = new BattleInput(new[] { Monster("z", speed: 20), Monster("a"), Monster("A") }, new[] { Monster("b") });
            var result = BattleResolver.Resolve(input, Config(), new SimRandom(1));
            Assert.Equal(new[] { "z", "A", "a", "b" }, result.Actions.Where(x => x.Kind == BattleActionKind.Skill).Select(x => x.ActorId.Value.Value));
        }

        [Fact]
        public void SingleTargetChoosesLowestHpThenOrdinalId()
        {
            var input = new BattleInput(new[] { Monster("actor", speed: 100) },
                new[] { Monster("z", currentHp: 800), Monster("a", currentHp: 900), Monster("B", currentHp: 800) });
            var result = BattleResolver.Resolve(input, Config(), new SimRandom(1));
            Assert.Equal(new MonsterId("B"), result.Actions[0].TargetId);
        }

        [Fact]
        public void CooldownTwoHasReadyBlockedReadyCycleAndWaitsDoNotDrawRandom()
        {
            var random = new SimRandom(42);
            var result = BattleResolver.Resolve(new BattleInput(new[] { Monster("a") }, new[] { Monster("b") }), Config(cooldown: 2, rounds: 3), random);
            Assert.Equal(new[] { BattleActionKind.Skill, BattleActionKind.Wait, BattleActionKind.Skill },
                result.Actions.Where(x => x.ActorId == new MonsterId("a")).Select(x => x.Kind));
            Assert.Equal(new[] { 1, 2, 3 }, result.Actions.Where(x => x.ActorId == new MonsterId("a")).Select(x => x.Turn));
            Assert.Equal(1, result.FinalMonsters.Single(x => x.Id == new MonsterId("a")).Cooldowns["strike"]);
            var expectedRandom = new SimRandom(42);
            for (int i = 0; i < 4; i++) expectedRandom.NextDouble();
            Assert.Equal(expectedRandom.State, random.State);
        }

        [Fact]
        public void InitialCooldownTicksOncePerRoundRegardlessOfActorCount()
        {
            var actor = Monster("a", cooldowns: new Dictionary<string, int> { ["strike"] = 2 });
            var input = new BattleInput(new[] { actor }, new[] { Monster("b"), Monster("c") });
            var result = BattleResolver.Resolve(input, Config(rounds: 3), new SimRandom(1));
            Assert.Equal(new[] { BattleActionKind.Wait, BattleActionKind.Wait, BattleActionKind.Skill },
                result.Actions.Where(x => x.ActorId == actor.Id).Select(x => x.Kind));
            Assert.Equal(3, result.CompletedRounds);
        }

        [Fact]
        public void FirstReadySkillUsesLoadoutPriorityAndFallsBackWhenBlocked()
        {
            var skills = new SkillCatalog(new[] {
                new SkillDefinition("z", MonsterElement.Fire, 20, 2, SkillTargetRule.SingleEnemy),
                new SkillDefinition("a", MonsterElement.Fire, 1, 0, SkillTargetRule.SingleEnemy) });
            var result = Resolve(Monster("actor", speed: 20, skills: new[] { "z", "a" }),
                Monster("enemy", skills: new[] { "a" }), new CombatConfig(skills, maxRounds: 3));
            Assert.Equal(new[] { "z", "a", "z" }, result.Actions.Where(x => x.ActorId == new MonsterId("actor")).Select(x => x.SkillId));
        }

        [Fact]
        public void AllEnemiesRecordsEachTargetOrdinallyAndDrawsCritOncePerSkill()
        {
            var random = new SimRandom(7);
            var result = BattleResolver.Resolve(new BattleInput(new[] { Monster("actor", speed: 20, crit: 1) },
                new[] { Monster("z", element: MonsterElement.Grass), Monster("b", element: MonsterElement.Water) }),
                Config(targets: SkillTargetRule.AllEnemies), random);
            var attacks = result.Actions.Where(x => x.ActorId == new MonsterId("actor")).ToArray();
            Assert.Equal(new[] { "b", "z" }, attacks.Select(x => x.TargetId.Value.Value));
            Assert.Equal(new long[] { 10, 40 }, attacks.Select(x => x.Damage));
            var expected = new SimRandom(7);
            for (int i = 0; i < 3; i++) expected.NextDouble();
            Assert.Equal(expected.State, random.State);
        }

        [Fact]
        public void DefeatedOpponentCannotActAndBattleStopsBeforeRoundCompletes()
        {
            var result = Resolve(Monster("a"), Monster("b", currentHp: 3), Config(rounds: 10));
            var action = Assert.Single(result.Actions);
            Assert.Equal(3, action.Damage);
            Assert.Equal(0, action.ResultingHp);
            Assert.Equal(BattleOutcome.TeamWon, result.Outcome);
            Assert.Equal(0, result.CompletedRounds);
        }

        [Fact]
        public void RoundLimitReturnsRemainingHpInsteadOfInventingWinner()
        {
            var result = Resolve(Monster("a"), Monster("b"), Config(rounds: 2));
            Assert.Equal(BattleOutcome.RoundLimit, result.Outcome);
            Assert.Equal(2, result.CompletedRounds);
            Assert.All(result.FinalMonsters, monster => Assert.Equal(980, monster.CurrentHp));
        }

        [Fact]
        public void EmptyLivingSideEndsWithoutActionsOrRandomDraws()
        {
            var random = new SimRandom(2);
            var before = random.State;
            var result = BattleResolver.Resolve(new BattleInput(new[] { Monster("a", currentHp: 0) }, new[] { Monster("b") }), Config(), random);
            Assert.Equal(BattleOutcome.OpponentsWon, result.Outcome);
            Assert.Empty(result.Actions);
            Assert.Equal(before, random.State);
        }

        [Fact]
        public void UnknownSkillsRejectBeforeConsumingRandom()
        {
            var random = new SimRandom(2);
            var before = random.State;
            Assert.Throws<KeyNotFoundException>(() => BattleResolver.Resolve(
                new BattleInput(new[] { Monster("a", skills: new[] { "missing" }) }, new[] { Monster("b") }), Config(), random));
            Assert.Equal(before, random.State);
        }

        [Theory]
        [InlineData(SkillTargetRule.Self)]
        [InlineData(SkillTargetRule.SingleAlly)]
        [InlineData(SkillTargetRule.AllAllies)]
        public void UndefinedSupportBehaviorRejectsInsteadOfDamagingAllies(SkillTargetRule targetRule)
        {
            Assert.Throws<NotSupportedException>(() => Resolve(Monster("a"), Monster("b"), Config(targets: targetRule)));
        }

        [Fact]
        public void UndefinedEffectBehaviorRejectsBeforeRandomDraw()
        {
            var skills = new SkillCatalog(new[] { new SkillDefinition("strike", MonsterElement.Fire, 0, 0,
                SkillTargetRule.SingleEnemy, "regen") }, new[] { "regen" });
            var random = new SimRandom(2);
            var before = random.State;
            Assert.Throws<NotSupportedException>(() => BattleResolver.Resolve(new BattleInput(new[] { Monster("a") }, new[] { Monster("b") }), new CombatConfig(skills), random));
            Assert.Equal(before, random.State);
        }

        [Fact]
        public void DuplicateCombatantIdsRejectAcrossSides()
        {
            Assert.Throws<ArgumentException>(() => new BattleInput(new[] { Monster("a") }, new[] { Monster("a") }));
        }

        [Fact]
        public void SnapshotRejectsInvalidHpLoadoutsAndCooldowns()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Monster("a", currentHp: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monster("a", currentHp: 1001));
            Assert.Throws<ArgumentException>(() => Monster("a", skills: Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => Monster("a", skills: new[] { "strike", "strike" }));
            Assert.Throws<ArgumentException>(() => Monster("a", cooldowns: new Dictionary<string, int> { ["unknown"] = 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monster("a", cooldowns: new Dictionary<string, int> { ["strike"] = -1 }));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(0.9)]
        public void InvalidCriticalMultiplierRejects(double multiplier)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Config(criticalMultiplier: multiplier));
        }

        [Fact]
        public void CooldownOneDecrementsOnUseRoundAndIsReadyNextRound()
        {
            var result = Resolve(Monster("a"), Monster("b"), Config(cooldown: 1, rounds: 2));
            Assert.Equal(new[] { BattleActionKind.Skill, BattleActionKind.Skill },
                result.Actions.Where(x => x.ActorId == new MonsterId("a")).Select(x => x.Kind));
            Assert.All(result.FinalMonsters, monster => Assert.Equal(0, monster.Cooldowns["strike"]));
        }

        [Fact]
        public void FiniteDamageDoesNotOverflowIntermediateProductBeforeDefenseDivision()
        {
            var result = Resolve(Monster("a", attack: double.MaxValue), Monster("b", defense: double.MaxValue), Config(power: 2));
            Assert.Equal(2, result.Actions[0].Damage);
        }

        [Fact]
        public void DamageSaturatesSafelyAndLogsOnlyActualHpLost()
        {
            var result = Resolve(Monster("a", attack: double.MaxValue), Monster("b", hp: long.MaxValue), Config(power: double.MaxValue));
            Assert.Equal(long.MaxValue, result.Actions[0].Damage);
            Assert.Equal(0, result.Actions[0].ResultingHp);
            Assert.Equal(BattleOutcome.TeamWon, result.Outcome);
        }

        [Fact]
        public void EmptySidesDrawWithoutActions()
        {
            var result = BattleResolver.Resolve(new BattleInput(Array.Empty<MonsterSnapshot>(), Array.Empty<MonsterSnapshot>()), Config(), new SimRandom(42));
            Assert.Equal(BattleOutcome.Draw, result.Outcome);
            Assert.Empty(result.Actions);
            Assert.Empty(result.FinalMonsters);
            Assert.Equal(result.InitialRandomState, result.FinalRandomState);
        }

        [Fact]
        public void DefeatedTargetsAreExcludedFromSelection()
        {
            var result = BattleResolver.Resolve(new BattleInput(new[] { Monster("a", speed: 20) },
                new[] { Monster("dead", currentHp: 0), Monster("living") }), Config(), new SimRandom(42));
            Assert.Equal(new MonsterId("living"), result.Actions[0].TargetId);
            Assert.DoesNotContain(result.Actions, action => action.ActorId == new MonsterId("dead"));
        }

        [Fact]
        public void BattleEndingOnLastScheduledActionCompletesItsRound()
        {
            var result = Resolve(Monster("a", currentHp: 1), Monster("b"), Config(cooldown: 2, rounds: 3));
            Assert.Equal(BattleOutcome.OpponentsWon, result.Outcome);
            Assert.Equal(BattleActionKind.RoundCompleted, result.Actions.Last().Kind);
            Assert.Equal(1, result.CompletedRounds);
            Assert.All(result.FinalMonsters, monster => Assert.Equal(1, monster.Cooldowns["strike"]));
        }

        [Fact]
        public void NullResolveArgumentsReject()
        {
            var input = new BattleInput(new[] { Monster("a") }, new[] { Monster("b") });
            Assert.Throws<ArgumentNullException>(() => BattleResolver.Resolve(null, Config(), new SimRandom(1)));
            Assert.Throws<ArgumentNullException>(() => BattleResolver.Resolve(input, null, new SimRandom(1)));
            Assert.Throws<ArgumentNullException>(() => BattleResolver.Resolve(input, Config(), null));
        }

        [Fact]
        public void SnapshotAndInputRejectMissingOrInvalidCombatData()
        {
            Assert.Throws<ArgumentException>(() => new MonsterSnapshot(default, MonsterElement.Fire, new MonsterStats(1, 1, 1, 1, 0), 1, new[] { "strike" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monster("a", element: (MonsterElement)9));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monster("a", hp: 0));
            Assert.Throws<ArgumentException>(() => Monster("a", skills: new[] { " " }));
            Assert.Throws<ArgumentNullException>(() => new MonsterSnapshot(new MonsterId("a"), MonsterElement.Fire, null, 1, new[] { "strike" }));
            Assert.Throws<ArgumentNullException>(() => new MonsterSnapshot(new MonsterId("a"), MonsterElement.Fire, new MonsterStats(1, 1, 1, 1, 0), 1, null));
            Assert.Throws<ArgumentNullException>(() => MonsterSnapshot.FromMonster(null, new[] { "strike" }));
            Assert.Throws<ArgumentNullException>(() => new BattleInput(null, Array.Empty<MonsterSnapshot>()));
            Assert.Throws<ArgumentNullException>(() => new BattleInput(Array.Empty<MonsterSnapshot>(), null));
            Assert.Throws<ArgumentException>(() => new BattleInput(new MonsterSnapshot[] { null }, Array.Empty<MonsterSnapshot>()));
        }

        [Fact]
        public void InvalidRoundLimitRejects()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Config(rounds: 0));
        }
    }
}
