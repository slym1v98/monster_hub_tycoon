using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Domain.Combat;
using Game.Domain.Monsters;
using Xunit;
using static Game.Domain.Tests.BattleResolverTests;

namespace Game.Domain.Tests
{
    public sealed class BattleReplayTests
    {
        [Fact]
        public void IdenticalSeedReplaysIdenticalLogFinalStateAndRandomState()
        {
            var input = new BattleInput(new[] { Monster("a", crit: 0.5) }, new[] { Monster("b", crit: 0.5) });
            var config = Config(cooldown: 2, rounds: 5);
            var first = BattleResolver.Resolve(input, config, new SimRandom(42));
            var replay = BattleResolver.Resolve(input, config, new SimRandom(42));
            Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(replay));
            Assert.NotEqual(JsonSerializer.Serialize(first), JsonSerializer.Serialize(BattleResolver.Resolve(input, config, new SimRandom(43))));
        }

        [Fact]
        public void InputEnumerationOrderDoesNotChangeBattleOrDrawAssignment()
        {
            var a = Monster("a", crit: 0.5);
            var b = Monster("b", crit: 0.5);
            var c = Monster("c", crit: 0.5);
            var d = Monster("d", crit: 0.5);
            var config = Config(rounds: 3, targets: SkillTargetRule.AllEnemies);
            var first = BattleResolver.Resolve(new BattleInput(new[] { b, a }, new[] { d, c }), config, new SimRandom(3));
            var reordered = BattleResolver.Resolve(new BattleInput(new[] { a, b }, new[] { c, d }), config, new SimRandom(3));
            Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(reordered));
        }

        [Fact]
        public void OrderedReplayReconstructsFinalHpAndCooldownsIncludingWaitRounds()
        {
            var input = new BattleInput(new[] { Monster("a", crit: 0.5) }, new[] { Monster("b", cooldowns: new Dictionary<string, int> { ["strike"] = 1 }) });
            var result = BattleResolver.Resolve(input, Config(cooldown: 2, rounds: 5), new SimRandom(4));
            var hp = input.Team.Concat(input.Opponents).ToDictionary(x => x.Id, x => x.CurrentHp);
            var cooldowns = input.Team.Concat(input.Opponents).ToDictionary(x => x.Id, x => x.Cooldowns.ToDictionary(pair => pair.Key, pair => pair.Value));
            int sequence = 1;
            foreach (var action in result.Actions)
            {
                Assert.Equal(sequence++, action.Sequence);
                if (action.Kind == BattleActionKind.Skill)
                {
                    var target = action.TargetId.Value;
                    hp[target] = hp[target] - action.Damage + action.Heal;
                    Assert.Equal(hp[target], action.ResultingHp);
                    cooldowns[action.ActorId.Value][action.SkillId] = action.SkillCooldown;
                }
                else if (action.Kind == BattleActionKind.RoundCompleted)
                {
                    foreach (var monster in cooldowns)
                        foreach (var skill in monster.Value.Keys.ToArray())
                            monster.Value[skill] = Math.Max(0, monster.Value[skill] - 1);
                }
                Assert.False(action.Swap);
                Assert.Equal(RebellionOutcome.None, action.Rebellion);
            }
            Assert.Equal(result.CompletedRounds, result.Actions.Count(x => x.Kind == BattleActionKind.RoundCompleted));
            foreach (var monster in result.FinalMonsters)
            {
                Assert.Equal(hp[monster.Id], monster.CurrentHp);
                Assert.Equal(cooldowns[monster.Id].OrderBy(x => x.Key), monster.Cooldowns.OrderBy(x => x.Key));
            }
        }

        [Fact]
        public void SnapshotsAndResultsAreDefensiveCopiesAndResolveDoesNotMutateInputs()
        {
            var ids = new List<string> { "strike" };
            var cooldowns = new Dictionary<string, int> { ["strike"] = 1 };
            var snapshot = new MonsterSnapshot(new MonsterId("a"), MonsterElement.Light, new MonsterStats(1000, 10, 10, 10, 0), 1000, ids, cooldowns);
            var team = new List<MonsterSnapshot> { snapshot };
            var input = new BattleInput(team, new[] { Monster("b") });
            ids.Clear(); cooldowns["strike"] = 99; team.Clear();
            var before = JsonSerializer.Serialize(input);
            var result = BattleResolver.Resolve(input, Config(rounds: 3), new SimRandom(42));
            Assert.Equal(before, JsonSerializer.Serialize(input));
            Assert.Equal(1, snapshot.Cooldowns["strike"]);
            Assert.Equal(1000, snapshot.CurrentHp);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)snapshot.SkillIds).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)snapshot.Cooldowns).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<MonsterSnapshot>)input.Team).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<BattleAction>)result.Actions).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<BattleMonsterState>)result.FinalMonsters).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)result.FinalMonsters[0].Cooldowns).Clear());
        }

        [Fact]
        public void SnapshotFromMonsterIsDetachedFromLaterHpChanges()
        {
            var monster = MonsterRosterTests.Make("a");
            var snapshot = MonsterSnapshot.FromMonster(monster, new[] { "strike" });
            monster.SetCurrentHp(1);
            var result = Resolve(snapshot, Monster("b"), Config());
            Assert.Equal(300, snapshot.CurrentHp);
            Assert.Equal(1, monster.CurrentHp);
            Assert.True(result.FinalMonsters.Single(x => x.Id == monster.Id).CurrentHp < 300);
        }
    }
}
