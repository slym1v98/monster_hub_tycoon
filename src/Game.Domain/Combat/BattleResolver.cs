using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    /// <summary>Bộ giải Domain thuần trên bản sao trạng thái; chỉ tiêu thụ RNG được truyền vào.</summary>
    public static class BattleResolver
    {
        public static BattleResult Resolve(BattleInput input, CombatConfig config, SimRandom random)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (random == null) throw new ArgumentNullException(nameof(random));
            // Kiểm tra toàn bộ dữ liệu trước lần rút RNG đầu tiên; không bỏ qua hiệu ứng chưa định nghĩa.
            var monsters = input.Team.Select(x => new Combatant(x, BattleSide.Team, config.Skills))
                .Concat(input.Opponents.Select(x => new Combatant(x, BattleSide.Opponents, config.Skills)))
                .OrderBy(x => x.Snapshot.Id.Value, StringComparer.Ordinal).ToArray();
            var initialRandomState = random.State;
            var actions = new List<BattleAction>();
            int completedRounds = 0;
            BattleResult Finish(BattleOutcome outcome) => new BattleResult(outcome, completedRounds, actions,
                monsters.Select(x => new BattleMonsterState(x.Snapshot.Id, x.Side, x.Hp, x.Cooldowns)), initialRandomState, random.State);

            for (int turn = 1; ; turn++)
            {
                var outcome = Outcome(monsters);
                if (outcome.HasValue) return Finish(outcome.Value);
                // Prototype: một lượt mỗi Monster còn HP trong vòng, ASPD giảm dần, hòa dùng ID ordinal.
                var initiative = monsters.Where(x => x.Hp > 0).OrderByDescending(x => x.Snapshot.Stats.AttackSpeed)
                    .ThenBy(x => x.Snapshot.Id.Value, StringComparer.Ordinal).ToArray();
                foreach (var actor in initiative)
                {
                    outcome = Outcome(monsters);
                    if (outcome.HasValue) return Finish(outcome.Value);
                    if (actor.Hp == 0) continue;
                    // Prototype: thứ tự loadout là ưu tiên kỹ năng; hết kỹ năng sẵn sàng thì chờ.
                    var skill = actor.Skills.FirstOrDefault(x => actor.Cooldowns[x.Id] == 0);
                    if (skill == null)
                    {
                        actions.Add(new BattleAction(actions.Count + 1, turn, BattleActionKind.Wait, actor.Snapshot.Id,
                            null, null, 0, 0, 1, actor.Hp, false, 0, false, false, RebellionOutcome.None));
                        continue;
                    }
                    var enemies = monsters.Where(x => x.Side != actor.Side && x.Hp > 0);
                    // Prototype: đánh đơn chọn HP thấp nhất rồi ID; diện rộng duyệt mục tiêu theo ID.
                    var targets = skill.TargetRule == SkillTargetRule.AllEnemies
                        ? enemies.OrderBy(x => x.Snapshot.Id.Value, StringComparer.Ordinal).ToArray()
                        : enemies.OrderBy(x => x.Hp).ThenBy(x => x.Snapshot.Id.Value, StringComparer.Ordinal).Take(1).ToArray();
                    // Một lần rút chí mạng mỗi lần dùng skill, kể cả xác suất 0/1; không rút khi chờ.
                    bool critical = random.NextDouble() < actor.Snapshot.Stats.CriticalChance;
                    actor.Cooldowns[skill.Id] = skill.CooldownActions;
                    foreach (var target in targets)
                    {
                        double effectiveness = ElementChart.Multiplier(skill.Element, target.Snapshot.Element);
                        long damage = Math.Min(target.Hp, Damage(skill.Power, actor.Snapshot.Stats, target.Snapshot.Stats,
                            effectiveness, critical ? config.CriticalMultiplier : 1));
                        target.Hp -= damage;
                        actions.Add(new BattleAction(actions.Count + 1, turn, BattleActionKind.Skill, actor.Snapshot.Id,
                            skill.Id, target.Snapshot.Id, damage, 0, effectiveness, target.Hp, critical,
                            actor.Cooldowns[skill.Id], false, false, RebellionOutcome.None));
                    }
                }
                // Mọi hồi chiêu dương giảm một lần mỗi vòng hoàn tất, kể cả kỹ năng vừa dùng.
                // Vòng kết thúc sớm không giảm hồi chiêu và không có bản ghi RoundCompleted.
                foreach (var monster in monsters)
                {
                    foreach (var skillId in monster.Snapshot.SkillIds)
                        if (monster.Cooldowns[skillId] > 0)
                            monster.Cooldowns[skillId]--;
                }
                completedRounds++;
                actions.Add(new BattleAction(actions.Count + 1, turn, BattleActionKind.RoundCompleted, null,
                    null, null, 0, 0, 1, 0, false, 0, false, false, RebellionOutcome.None));
                outcome = Outcome(monsters);
                if (outcome.HasValue) return Finish(outcome.Value);
                if (turn == config.MaxRounds) return Finish(BattleOutcome.RoundLimit);
            }
        }

        /// <summary>Prototype: max(1, floor(power * ATK / max(1, DEF) * hệ * chí mạng)).</summary>
        static long Damage(double power, MonsterStats attacker, MonsterStats defender, double effectiveness, double criticalMultiplier)
        {
            double defense = Math.Max(1, defender.Defense);
            double scaled = power * attacker.Attack;
            // Đổi thứ tự chia khi tích trung gian tràn, dù kết quả sau chia vẫn hữu hạn.
            scaled = double.IsInfinity(scaled) ? power * (attacker.Attack / defense) : scaled / defense;
            double damage = Math.Floor(scaled * effectiveness * criticalMultiplier);
            // Bão hòa trước chuyển double sang long; không để tràn số làm tăng HP hay tạo sát thương âm.
            if (damage >= long.MaxValue) return long.MaxValue;
            return damage < 1 ? 1 : (long)damage;
        }

        static BattleOutcome? Outcome(IEnumerable<Combatant> monsters)
        {
            bool teamAlive = monsters.Any(x => x.Side == BattleSide.Team && x.Hp > 0);
            bool opponentsAlive = monsters.Any(x => x.Side == BattleSide.Opponents && x.Hp > 0);
            if (!teamAlive && !opponentsAlive) return BattleOutcome.Draw;
            if (!teamAlive) return BattleOutcome.OpponentsWon;
            if (!opponentsAlive) return BattleOutcome.TeamWon;
            return null;
        }

        // Trạng thái riêng theo Monster, tách khỏi vòng giải để tác vụ 6 bổ sung vị trí đội hình và chuyển trạng thái.
        sealed class Combatant
        {
            public MonsterSnapshot Snapshot { get; }
            public BattleSide Side { get; }
            public long Hp { get; set; }
            public IReadOnlyList<SkillDefinition> Skills { get; }
            public Dictionary<string, int> Cooldowns { get; }

            public Combatant(MonsterSnapshot snapshot, BattleSide side, SkillCatalog catalog)
            {
                Snapshot = snapshot;
                Side = side;
                Hp = snapshot.CurrentHp;
                Skills = catalog.ResolveLoadout(snapshot.SkillIds);
                foreach (var skill in Skills)
                    if (skill.EffectId != null || (skill.TargetRule != SkillTargetRule.SingleEnemy && skill.TargetRule != SkillTargetRule.AllEnemies))
                        throw new NotSupportedException("Bộ giải cơ sở chỉ hỗ trợ sát thương lên đối thủ không kèm hiệu ứng.");
                Cooldowns = snapshot.Cooldowns.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            }
        }
    }
}
