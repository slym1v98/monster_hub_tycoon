using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            var input = new BattleInput(new[] { Monster("z", speed: 20) }, new[] { Monster("a"), Monster("A"), Monster("b") });
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
            Assert.False(result.TeamDown);
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

        static BattleInput TeamInput(MonsterSnapshot active, MonsterSnapshot[] reserves, MonsterSnapshot[] enemies,
            double? management = null, TrainerCombatContext trainer = null)
        {
            var team = new[] { active }.Concat(reserves).ToArray();
            return new BattleInput(team, enemies, active.Id, trainer,
                management.HasValue ? team.ToDictionary(x => x.Id, x => management.Value) : null);
        }

        static CombatConfig RebellionConfig(double probability = 1, double skip = 1, double sleep = 0,
            double area = 0, int rounds = 2, int skipActions = 1, int sleepActions = 1) => new CombatConfig(
                Config().Skills, maxRounds: rounds,
                rebellion: new RebellionCombatConfig(probability, 1, skip, sleep, area, skipActions, sleepActions));

        [Theory]
        [InlineData(1499, true)]
        [InlineData(1500, false)]
        [InlineData(1501, false)]
        public void AutoSwapUsesStrictFifteenPercentBoundaryAfterDamage(long remaining, bool swaps)
        {
            var active = Monster("z-active", hp: 10000, currentHp: remaining + 10, speed: 1);
            var reserve = Monster("a-reserve", speed: 100);
            var result = BattleResolver.Resolve(TeamInput(active, new[] { reserve }, new[] { Monster("enemy", speed: 20) }), Config(), new SimRandom(42));
            Assert.Equal(swaps, result.Actions.Any(x => x.Swap));
            Assert.Equal(remaining, result.FinalMonsters.Single(x => x.Id == active.Id).CurrentHp);
            Assert.Equal(swaps ? reserve.Id : active.Id, result.ActiveId);
            Assert.Equal(swaps ? reserve.Id : active.Id, result.Actions.Single(x => x.Kind == BattleActionKind.Skill && x.TargetId == new MonsterId("enemy")).ActorId);
        }

        [Fact]
        public void SwapChoosesLowestOrdinalLivingReserveEvenWhenItIsBelowThreshold()
        {
            var active = Monster("active", hp: 10000, currentHp: 1509, speed: 1);
            var reserves = new[] { Monster("z"), Monster("A", currentHp: 1) };
            var result = BattleResolver.Resolve(TeamInput(active, reserves, new[] { Monster("enemy", speed: 20) }), Config(), new SimRandom(42));
            var swap = result.Actions.First(x => x.Swap);
            Assert.Equal(active.Id, swap.ActorId);
            Assert.Equal(new MonsterId("A"), swap.TargetId);
            Assert.Contains(result.Actions, x => x.Kind == BattleActionKind.Skill && x.ActorId == new MonsterId("A"));
            Assert.Equal(1, swap.ResultingHp);
        }

        [Fact]
        public void FaintedActiveIsRetainedAndLivingReserveActsInPendingTeamSlot()
        {
            var active = Monster("active", currentHp: 10, speed: 1);
            var reserve = Monster("reserve", speed: 100);
            var result = BattleResolver.Resolve(TeamInput(active, new[] { reserve }, new[] { Monster("enemy", speed: 20) }), Config(), new SimRandom(42));
            var faint = Assert.Single(result.Actions, x => x.Fainted);
            Assert.Equal(active.Id, faint.TargetId);
            Assert.Equal(0, faint.ResultingHp);
            Assert.Equal(BattleActionKind.Skill, faint.Kind);
            Assert.Equal(new[] { "enemy", "reserve" }, result.Actions.Where(x => x.Kind == BattleActionKind.Skill).Select(x => x.ActorId.Value.Value));
            Assert.True(result.FinalMonsters.Single(x => x.Id == active.Id).Fainted);
            Assert.Equal(3, result.FinalMonsters.Count);
            Assert.False(result.TeamDown);
        }

        [Fact]
        public void InitialFaintedActiveHandsOverWithoutActingAndSkipsFaintedReserve()
        {
            var active = Monster("active", currentHp: 0);
            var result = BattleResolver.Resolve(TeamInput(active,
                new[] { Monster("A", currentHp: 0), Monster("z", speed: 20) }, new[] { Monster("enemy") }), Config(), new SimRandom(42));
            Assert.Equal(new MonsterId("z"), result.ActiveId);
            Assert.Equal(new MonsterId("z"), result.Actions.First(x => x.Kind == BattleActionKind.Skill).ActorId);
            Assert.DoesNotContain(result.Actions, x => x.Kind == BattleActionKind.Skill && (x.ActorId == active.Id || x.ActorId == new MonsterId("A")));
            Assert.False(result.TeamDown);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        public void InitiallyAllFaintedRosterLosesButTeamDownRequiresThreeMembers(int teamSize, bool expectedTeamDown)
        {
            var team = Enumerable.Range(0, teamSize).Select(i => Monster("team-" + i, currentHp: 0)).ToArray();
            var result = BattleResolver.Resolve(new BattleInput(team, new[] { Monster("enemy") }), Config(), new SimRandom(42));

            Assert.Equal(BattleOutcome.OpponentsWon, result.Outcome);
            Assert.Equal(expectedTeamDown, result.TeamDown);
            var finalTeam = result.FinalMonsters.Where(x => x.Side == BattleSide.Team).ToArray();
            Assert.Equal(teamSize, finalTeam.Length);
            Assert.All(finalTeam, x => Assert.True(x.Fainted));
            Assert.Empty(result.Actions);
            Assert.Equal(result.InitialRandomState, result.FinalRandomState);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        public void RosterFaintingDuringCombatLosesButTeamDownRequiresThreeMembers(int teamSize, bool expectedTeamDown)
        {
            var team = Enumerable.Range(0, teamSize).Select(i => Monster("team-" + i, hp: 10, speed: 1)).ToArray();
            var result = BattleResolver.Resolve(new BattleInput(team, new[] { Monster("enemy", speed: 100) }), Config(rounds: 10), new SimRandom(42));

            Assert.Equal(BattleOutcome.OpponentsWon, result.Outcome);
            Assert.Equal(expectedTeamDown, result.TeamDown);
            var finalTeam = result.FinalMonsters.Where(x => x.Side == BattleSide.Team).ToArray();
            Assert.Equal(teamSize, finalTeam.Length);
            Assert.All(finalTeam, x => Assert.True(x.Fainted));
            Assert.Equal(teamSize, result.Actions.Count(x => x.Fainted));
        }

        [Fact]
        public void AllThreeFaintBeforeTeamDownAndNoMonsterDisappears()
        {
            var active = Monster("active", hp: 10, speed: 1);
            var result = BattleResolver.Resolve(TeamInput(active,
                new[] { Monster("A", hp: 10), Monster("z", hp: 10) }, new[] { Monster("enemy", speed: 100) }), Config(rounds: 10), new SimRandom(42));
            Assert.Equal(BattleOutcome.OpponentsWon, result.Outcome);
            Assert.True(result.TeamDown);
            Assert.Equal(3, result.Actions.Count(x => x.Fainted));
            Assert.Equal(new[] { "A", "z" }, result.Actions.Where(x => x.Swap).Select(x => x.TargetId.Value.Value));
            Assert.All(result.FinalMonsters.Where(x => x.Side == BattleSide.Team), x => { Assert.Equal(0, x.CurrentHp); Assert.True(x.Fainted); });
            Assert.Equal(active.Id, result.Actions.First(x => x.Fainted).TargetId);
            Assert.Equal(new MonsterId("z"), result.ActiveId);
        }

        [Fact]
        public void HealthyReservesNeitherActNorReceiveAreaDamage()
        {
            var active = Monster("active", speed: 20);
            var result = BattleResolver.Resolve(TeamInput(active, new[] { Monster("A"), Monster("z") },
                new[] { Monster("enemy") }), Config(targets: SkillTargetRule.AllEnemies), new SimRandom(42));
            Assert.Equal(new[] { "active", "enemy" }, result.Actions.Where(x => x.Kind == BattleActionKind.Skill).Select(x => x.ActorId.Value.Value));
            Assert.All(result.FinalMonsters.Where(x => x.Id == new MonsterId("A") || x.Id == new MonsterId("z")), x => Assert.Equal(1000, x.CurrentHp));
        }

        [Fact]
        public void SwapAfterTeamSlotDoesNotGrantExtraActionThisRound()
        {
            var active = Monster("active", currentHp: 159, speed: 100);
            var result = BattleResolver.Resolve(TeamInput(active, new[] { Monster("reserve", speed: 200) },
                new[] { Monster("enemy", speed: 20) }), Config(rounds: 2), new SimRandom(42));
            var reserveActions = result.Actions.Where(x => x.Kind == BattleActionKind.Skill && x.ActorId == new MonsterId("reserve")).ToArray();
            Assert.Equal(2, Assert.Single(reserveActions).Turn);
        }

        [Fact]
        public void SwapsRetainIndividualCooldownsAndTickReservesOncePerRound()
        {
            var active = Monster("active", currentHp: 159, speed: 1);
            var reserve = Monster("reserve", cooldowns: new Dictionary<string, int> { ["strike"] = 2 });
            var result = BattleResolver.Resolve(TeamInput(active, new[] { reserve }, new[] { Monster("enemy", speed: 20) }), Config(rounds: 3), new SimRandom(42));
            Assert.Equal(new[] { BattleActionKind.Wait, BattleActionKind.Wait, BattleActionKind.Skill },
                result.Actions.Where(x => x.ActorId == reserve.Id).Select(x => x.Kind));
            Assert.Equal(0, result.FinalMonsters.Single(x => x.Id == active.Id).Cooldowns["strike"]);
        }

        [Fact]
        public void LoneLivingActiveBelowThresholdContinuesUntilFainting()
        {
            var active = Monster("active", currentHp: 20, speed: 20);
            var result = BattleResolver.Resolve(TeamInput(active, new[] { Monster("reserve", currentHp: 0) }, new[] { Monster("enemy") }), Config(rounds: 3), new SimRandom(42));
            Assert.DoesNotContain(result.Actions, x => x.Swap);
            Assert.Equal(2, result.Actions.Count(x => x.Kind == BattleActionKind.Skill && x.ActorId == active.Id));
            Assert.Equal(BattleOutcome.OpponentsWon, result.Outcome);
            Assert.False(result.TeamDown);
        }

        [Fact]
        public void FifteenPercentCheckDoesNotOverflowForLongHp()
        {
            var active = Monster("active", hp: long.MaxValue, currentHp: (long)(long.MaxValue * 15m / 100m) + 10, speed: 1);
            var result = BattleResolver.Resolve(TeamInput(active, new[] { Monster("reserve") }, new[] { Monster("enemy", speed: 20) }), Config(), new SimRandom(42));
            Assert.Equal(new MonsterId("reserve"), result.ActiveId);
        }

        [Theory]
        [InlineData(0, 1, 0, 0, RebellionOutcome.Obey)]
        [InlineData(1, 1, 0, 0, RebellionOutcome.Skip)]
        [InlineData(1, 0, 1, 0, RebellionOutcome.Sleep)]
        [InlineData(1, 0, 0, 1, RebellionOutcome.AreaAggroAttack)]
        public void ExplicitRebellionOddsProduceEachOutcomeOncePerEncounter(double chance, double skip, double sleep, double area, RebellionOutcome expected)
        {
            var input = TeamInput(Monster("active", speed: 100), Array.Empty<MonsterSnapshot>(), new[] { Monster("enemy") }, 100, new TrainerCombatContext(1, 1, Rarity.Common));
            var result = BattleResolver.Resolve(input, RebellionConfig(chance, skip, sleep, area, rounds: 3), new SimRandom(42));
            Assert.Equal(expected, Assert.Single(result.Actions, x => x.Kind == BattleActionKind.Rebellion).Rebellion);
            Assert.Equal(expected == RebellionOutcome.Skip || expected == RebellionOutcome.Sleep ? 2 : 3,
                result.Actions.Count(x => x.Kind == BattleActionKind.Skill && x.ActorId == new MonsterId("active")));
            Assert.Equal(expected, result.FinalMonsters.Single(x => x.Side == BattleSide.Team).Rebellion);
        }

        [Theory]
        [InlineData(RebellionOutcome.Skip)]
        [InlineData(RebellionOutcome.Sleep)]
        public void SkipAndSleepUseExplicitActionDurations(RebellionOutcome outcome)
        {
            var input = TeamInput(Monster("active", speed: 100), Array.Empty<MonsterSnapshot>(), new[] { Monster("enemy") }, 100, new TrainerCombatContext(1, 1, Rarity.Common));
            var result = BattleResolver.Resolve(input, RebellionConfig(skip: outcome == RebellionOutcome.Skip ? 1 : 0,
                sleep: outcome == RebellionOutcome.Sleep ? 1 : 0, rounds: 4, skipActions: 2, sleepActions: 3), new SimRandom(42));
            Assert.Equal(outcome == RebellionOutcome.Skip ? new[] { 3, 4 } : new[] { 4 },
                result.Actions.Where(x => x.Kind == BattleActionKind.Skill && x.ActorId == new MonsterId("active")).Select(x => x.Turn));
        }

        [Theory]
        [InlineData(40.2, 0, 0, RebellionOutcome.Obey)]
        [InlineData(40.3, 0, 0, RebellionOutcome.Skip)]
        [InlineData(42.2, 1, 1, RebellionOutcome.Obey)]
        [InlineData(42.3, 1, 1, RebellionOutcome.Skip)]
        public void RebellionUsesFractionalTrainerManagementLevelAndSynergyItemLeadership(double management, double synergy, double item, RebellionOutcome expected)
        {
            var input = TeamInput(Monster("active", speed: 100), Array.Empty<MonsterSnapshot>(), new[] { Monster("enemy") }, management,
                new TrainerCombatContext(2, 1, Rarity.Common, synergyLeadershipBonus: synergy, itemLeadershipBonus: item));
            var result = BattleResolver.Resolve(input, RebellionConfig(probability: 100), new SimRandom(42));
            Assert.Equal(expected, result.Actions.Single(x => x.Kind == BattleActionKind.Rebellion).Rebellion);
        }

        [Theory]
        [InlineData(0.59, RebellionOutcome.Obey)]
        [InlineData(0.5940674489353119, RebellionOutcome.Obey)]
        [InlineData(0.60, RebellionOutcome.Skip)]
        public void SeededRebellionProbabilityUsesStrictRollBoundary(double probability, RebellionOutcome expected)
        {
            var input = TeamInput(Monster("active", speed: 100), Array.Empty<MonsterSnapshot>(), new[] { Monster("enemy") }, 21.2, new TrainerCombatContext(1, 1, Rarity.Common));
            var result = BattleResolver.Resolve(input, RebellionConfig(probability), new SimRandom(42));
            Assert.Equal(expected, result.Actions[0].Rebellion);
        }

        [Fact]
        public void AreaRebellionAttacksEveryLivingOpponentOrdinallyWithOneCriticalDraw()
        {
            var input = TeamInput(Monster("active", speed: 100), Array.Empty<MonsterSnapshot>(),
                new[] { Monster("z", currentHp: 5), Monster("B", currentHp: 5), Monster("dead", currentHp: 0) }, 100, new TrainerCombatContext(1, 1, Rarity.Common));
            var random = new SimRandom(42);
            var result = BattleResolver.Resolve(input, RebellionConfig(skip: 0, area: 1), random);
            var attacks = result.Actions.Where(x => x.Kind == BattleActionKind.Skill).ToArray();
            Assert.Equal(new[] { "B", "z" }, attacks.Select(x => x.TargetId.Value.Value));
            Assert.All(attacks, x => { Assert.True(x.Fainted); Assert.Equal(RebellionOutcome.AreaAggroAttack, x.Rebellion); Assert.Equal(5, x.Damage); });
            Assert.Equal(BattleOutcome.TeamWon, result.Outcome);
            var expected = new SimRandom(42); expected.NextDouble(); expected.NextDouble();
            Assert.Equal(expected.State, random.State);
        }

        [Fact]
        public void UnusedReservesDoNotRollRebellionOrConsumeRandom()
        {
            var input = TeamInput(Monster("active", speed: 100), new[] { Monster("reserve") },
                new[] { Monster("enemy", currentHp: 1) }, 100, new TrainerCombatContext(1, 1, Rarity.Common));
            var result = BattleResolver.Resolve(input, RebellionConfig(probability: 0), new SimRandom(42));
            Assert.Single(result.Actions, x => x.Kind == BattleActionKind.Rebellion);
            Assert.Equal(RebellionOutcome.None, result.FinalMonsters.Single(x => x.Id == new MonsterId("reserve")).Rebellion);
        }

        [Fact]
        public void TeamAndRebellionReplayIsIndependentOfEnumerationAndDoesNotMutateInputs()
        {
            var active = Monster("active", currentHp: 10, speed: 1, crit: .5);
            var reserveA = Monster("A", crit: .5);
            var reserveZ = Monster("z", crit: .5);
            var enemies = new[] { Monster("enemy", speed: 100), Monster("other", speed: 90) };
            var scores = new Dictionary<MonsterId, double> { [active.Id] = 100, [reserveA.Id] = 100, [reserveZ.Id] = 100 };
            var trainer = new TrainerCombatContext(1, 1, Rarity.Common);
            var first = new BattleInput(new[] { active, reserveZ, reserveA }, enemies, active.Id, trainer, scores);
            var second = new BattleInput(new[] { reserveA, active, reserveZ }, enemies.Reverse(), active.Id, trainer, scores);
            var before = JsonSerializer.Serialize(first);
            scores[reserveA.Id] = 0;
            var config = RebellionConfig(skip: 1, sleep: 1, area: 1, rounds: 5);
            var result = BattleResolver.Resolve(first, config, new SimRandom(42));
            Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(BattleResolver.Resolve(second, config, new SimRandom(42))));
            Assert.Equal(before, JsonSerializer.Serialize(first));
            Assert.Equal(100, first.ManagementScores[reserveA.Id.Value]);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, double>)first.ManagementScores).Clear());
        }

        [Fact]
        public void BattleInputRejectsInvalidActiveCapacityAndPartialManagementData()
        {
            var team = new[] { Monster("a"), Monster("b") };
            var trainer = new TrainerCombatContext(1, 1, Rarity.Common);
            Assert.Throws<ArgumentException>(() => new BattleInput(team, Array.Empty<MonsterSnapshot>(), new MonsterId("missing")));
            Assert.Throws<ArgumentException>(() => new BattleInput(new[] { Monster("a"), Monster("b"), Monster("c"), Monster("d") }, Array.Empty<MonsterSnapshot>()));
            Assert.Throws<ArgumentException>(() => new BattleInput(team, Array.Empty<MonsterSnapshot>(), trainer: trainer));
            Assert.Throws<ArgumentException>(() => new BattleInput(team, Array.Empty<MonsterSnapshot>(), trainer: trainer, managementScores: new Dictionary<MonsterId, double> { [team[0].Id] = 100 }));
            Assert.Throws<ArgumentException>(() => new BattleInput(team, Array.Empty<MonsterSnapshot>(), managementScores: team.ToDictionary(x => x.Id, x => 100d)));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(-1)]
        public void InvalidManagementScoresReject(double score)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TeamInput(Monster("active"), Array.Empty<MonsterSnapshot>(), Array.Empty<MonsterSnapshot>(), score, new TrainerCombatContext(1, 1, Rarity.Common)));
        }

        [Fact]
        public void InvalidRebellionConfigurationAndLeadershipReject()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RebellionCombatConfig(probabilityPerMissingPoint: double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RebellionCombatConfig(maxProbability: 1.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RebellionCombatConfig(skipWeight: -1));
            Assert.Throws<ArgumentException>(() => new RebellionCombatConfig(skipWeight: 0, sleepWeight: 0, areaAggroWeight: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RebellionCombatConfig(skipActions: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerCombatContext(0, 1, Rarity.Common));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerCombatContext(1, 101, Rarity.Common));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerCombatContext(1, 1, (Rarity)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrainerCombatContext(1, 1, Rarity.Common, itemLeadershipBonus: double.PositiveInfinity));
        }

        [Theory]
        [InlineData(0, BattleActionKind.Skill)]
        [InlineData(1, BattleActionKind.Wait)]
        public void AlreadyLowActiveSwapsAfterItsAttackOrWait(int cooldown, BattleActionKind kind)
        {
            var active = Monster("active", currentHp: 149, speed: 100,
                cooldowns: new Dictionary<string, int> { ["strike"] = cooldown });
            var result = BattleResolver.Resolve(TeamInput(active, new[] { Monster("reserve") }, new[] { Monster("enemy") }), Config(), new SimRandom(42));
            Assert.Equal(kind, result.Actions[0].Kind);
            Assert.True(result.Actions[1].Swap);
            Assert.Equal(new MonsterId("reserve"), result.Actions[2].TargetId);
            Assert.Equal(149, result.FinalMonsters.Single(x => x.Id == active.Id).CurrentHp);
        }

        [Fact]
        public void TerminalEncounterDoesNotSwapOrConsumeRandom()
        {
            var input = TeamInput(Monster("active", currentHp: 0), new[] { Monster("reserve") }, Array.Empty<MonsterSnapshot>());
            var result = BattleResolver.Resolve(input, Config(), new SimRandom(42));
            Assert.Equal(BattleOutcome.TeamWon, result.Outcome);
            Assert.Empty(result.Actions);
            Assert.Equal(input.ActiveId, result.ActiveId);
            Assert.Equal(result.InitialRandomState, result.FinalRandomState);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        public void TeamReplayReconstructsHpCooldownsActiveFaintAndRebellionIncludingEarlyAreaWin(int scenario, bool expectedTeamDown)
        {
            var active = Monster("active", hp: scenario == 2 ? 10 : 1000, currentHp: 10, speed: scenario == 1 ? 100 : 1);
            var reserves = scenario == 1 ? Array.Empty<MonsterSnapshot>()
                : new[] { Monster("A", hp: scenario == 2 ? 10 : 1000), Monster("z", hp: scenario == 2 ? 10 : 1000) };
            var enemies = scenario == 1 ? new[] { Monster("B", currentHp: 5), Monster("enemy", currentHp: 5) }
                : new[] { Monster("enemy", speed: 100) };
            var input = TeamInput(active, reserves, enemies, 100, new TrainerCombatContext(1, 1, Rarity.Common));
            var config = RebellionConfig(skip: scenario == 1 ? 0 : 1, sleep: scenario == 1 ? 0 : 1, area: 1, rounds: 5);
            var result = BattleResolver.Resolve(input, config, new SimRandom(42));
            var hp = input.Team.Concat(input.Opponents).ToDictionary(x => x.Id, x => x.CurrentHp);
            var cooldowns = input.Team.Concat(input.Opponents).ToDictionary(x => x.Id, x => x.Cooldowns.ToDictionary(y => y.Key, y => y.Value));
            var rebellion = hp.Keys.ToDictionary(x => x, x => RebellionOutcome.None);
            var activeId = input.ActiveId;
            int sequence = 0;
            foreach (var action in result.Actions)
            {
                Assert.Equal(++sequence, action.Sequence);
                if (action.Kind == BattleActionKind.Skill)
                {
                    if (input.Team.Any(x => x.Id == action.ActorId)) Assert.Equal(activeId, action.ActorId);
                    if (input.Team.Any(x => x.Id == action.TargetId)) Assert.Equal(activeId, action.TargetId);
                    hp[action.TargetId.Value] -= action.Damage;
                    hp[action.TargetId.Value] += action.Heal;
                    Assert.Equal(hp[action.TargetId.Value], action.ResultingHp);
                    Assert.Equal(action.ResultingHp == 0, action.Fainted);
                    cooldowns[action.ActorId.Value][action.SkillId] = action.SkillCooldown;
                }
                else if (action.Kind == BattleActionKind.Swap)
                {
                    Assert.True(action.Swap);
                    Assert.Equal(activeId, action.ActorId);
                    Assert.True(hp[action.TargetId.Value] > 0);
                    activeId = action.TargetId;
                    Assert.Equal(hp[activeId.Value], action.ResultingHp);
                }
                else if (action.Kind == BattleActionKind.Rebellion)
                {
                    Assert.Equal(RebellionOutcome.None, rebellion[action.ActorId.Value]);
                    rebellion[action.ActorId.Value] = action.Rebellion;
                }
                else if (action.Kind == BattleActionKind.RoundCompleted)
                {
                    foreach (var monster in cooldowns.Values)
                        foreach (var skill in monster.Keys.ToArray()) monster[skill] = Math.Max(0, monster[skill] - 1);
                }
            }
            Assert.Equal(activeId, result.ActiveId);
            Assert.Equal(expectedTeamDown, result.TeamDown);
            Assert.Equal(result.CompletedRounds, result.Actions.Count(x => x.Kind == BattleActionKind.RoundCompleted));
            foreach (var monster in result.FinalMonsters)
            {
                Assert.Equal(hp[monster.Id], monster.CurrentHp);
                Assert.Equal(hp[monster.Id] == 0, monster.Fainted);
                Assert.Equal(rebellion[monster.Id], monster.Rebellion);
                Assert.Equal(cooldowns[monster.Id].OrderBy(x => x.Key), monster.Cooldowns.OrderBy(x => x.Key));
            }
            if (scenario == 1)
            {
                Assert.Equal(BattleOutcome.TeamWon, result.Outcome);
                Assert.Equal(0, result.CompletedRounds);
                Assert.Equal(2, result.Actions.Count(x => x.Rebellion == RebellionOutcome.AreaAggroAttack && x.Kind == BattleActionKind.Skill));
            }
            if (scenario == 2) Assert.True(result.TeamDown);
        }

        [Fact]
        public void RebellionDoesNotRerollWhenLowHpMonsterReturnsFromReserve()
        {
            var input = TeamInput(Monster("active", currentHp: 149, speed: 100),
                new[] { Monster("reserve", currentHp: 160, speed: 100) }, new[] { Monster("enemy") },
                100, new TrainerCombatContext(1, 1, Rarity.Common));
            var result = BattleResolver.Resolve(input, RebellionConfig(rounds: 4), new SimRandom(42));
            var decisions = result.Actions.Where(x => x.Kind == BattleActionKind.Rebellion).ToArray();
            Assert.Equal(new[] { "active", "reserve" }, decisions.Select(x => x.ActorId.Value.Value));
            Assert.True(result.Actions.Count(x => x.Swap) > 2);
            Assert.Contains(result.Actions, x => x.Kind == BattleActionKind.Skill && x.ActorId == new MonsterId("active") && x.Turn > 1);
        }
    }
}
