using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    /// <summary>Hai phía tham chiến đã chụp dữ liệu; đội có một Active và tối đa hai Reserve.</summary>
    public sealed class BattleInput
    {
        public IReadOnlyList<MonsterSnapshot> Team { get; }
        public IReadOnlyList<MonsterSnapshot> Opponents { get; }

        public MonsterId? ActiveId { get; }
        public IReadOnlyList<MonsterSnapshot> Reserves { get; }
        public TrainerCombatContext Trainer { get; }
        public IReadOnlyDictionary<string, double> ManagementScores { get; }

        public BattleInput(IEnumerable<MonsterSnapshot> team, IEnumerable<MonsterSnapshot> opponents,
            MonsterId? activeId = null, TrainerCombatContext trainer = null,
            IReadOnlyDictionary<MonsterId, double> managementScores = null)
        {
            Team = Copy(team, nameof(team));
            Opponents = Copy(opponents, nameof(opponents));
            if (Team.Count > MonsterRoster.Capacity) throw new ArgumentException("Đội có tối đa ba Monster.", nameof(team));
            ActiveId = activeId ?? Team.FirstOrDefault()?.Id;
            if (ActiveId.HasValue && !Team.Any(x => x.Id == ActiveId.Value))
                throw new ArgumentException("Active phải thuộc đội.", nameof(activeId));
            Reserves = Array.AsReadOnly(Team.Where(x => x.Id != ActiveId).ToArray());
            Trainer = trainer;
            var scores = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (var pair in managementScores ?? new Dictionary<MonsterId, double>())
            {
                if (!Team.Any(x => x.Id == pair.Key)) throw new ArgumentException("Điểm Quản Lý phải thuộc đội.", nameof(managementScores));
                if (double.IsNaN(pair.Value) || double.IsInfinity(pair.Value) || pair.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(managementScores));
                scores.Add(pair.Key.Value, pair.Value);
            }
            if ((trainer != null && scores.Count != Team.Count) || (trainer == null && scores.Count != 0))
                throw new ArgumentException("Điểm Quản Lý đầy đủ cần đi cùng dữ liệu Trainer.", nameof(managementScores));
            ManagementScores = new ReadOnlyDictionary<string, double>(scores);
            var ids = Team.Concat(Opponents).Select(x => x.Id);
            if (ids.Distinct().Count() != Team.Count + Opponents.Count)
                throw new ArgumentException("Mã Monster phải duy nhất trong trận.");
        }

        static IReadOnlyList<MonsterSnapshot> Copy(IEnumerable<MonsterSnapshot> snapshots, string name)
        {
            var entries = (snapshots ?? throw new ArgumentNullException(name)).ToArray();
            if (entries.Any(x => x == null)) throw new ArgumentException("Ảnh chụp Monster không được null.", name);
            return Array.AsReadOnly(entries.OrderBy(x => x.Id.Value, StringComparer.Ordinal).ToArray());
        }
    }
    /// <summary>Đầu vào Lãnh đạo bất biến; bonus synergy/vật phẩm đã được bên gọi xác nhận, không tạo vật phẩm.</summary>
    public sealed class TrainerCombatContext
    {
        public int Rank { get; }
        public int Level { get; }
        public Rarity Rarity { get; }
        public double LeadershipBonus { get; }
        public double SynergyLeadershipBonus { get; }
        public double ItemLeadershipBonus { get; }
        public double LeadershipScore { get; }

        public TrainerCombatContext(int rank, int level, Rarity rarity, double leadershipBonus = 0,
            double synergyLeadershipBonus = 0, double itemLeadershipBonus = 0)
        {
            MonsterProgression.TrainerManagementLevel(rank, level);
            if (!Enum.IsDefined(typeof(Rarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            foreach (double bonus in new[] { leadershipBonus, synergyLeadershipBonus, itemLeadershipBonus })
                if (double.IsNaN(bonus) || double.IsInfinity(bonus) || bonus < 0)
                    throw new ArgumentOutOfRangeException(nameof(leadershipBonus));
            double score = RebellionModel.LeadershipScore(rank, level, rarity,
                leadershipBonus + synergyLeadershipBonus + itemLeadershipBonus);
            if (double.IsInfinity(score)) throw new ArgumentOutOfRangeException(nameof(leadershipBonus));
            Rank = rank;
            Level = level;
            Rarity = rarity;
            LeadershipBonus = leadershipBonus;
            SynergyLeadershipBonus = synergyLeadershipBonus;
            ItemLeadershipBonus = itemLeadershipBonus;
            LeadershipScore = score;
        }
    }
}
