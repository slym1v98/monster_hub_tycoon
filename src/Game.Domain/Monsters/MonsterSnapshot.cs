using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Game.Domain.Monsters
{
    /// <summary>Ảnh chụp bất biến cho trận đấu; HP, bộ kỹ năng và hồi chiêu tách khỏi thực thể Monster.</summary>
    public sealed class MonsterSnapshot
    {
        public MonsterId Id { get; }
        public MonsterElement Element { get; }
        public MonsterStats Stats { get; }
        public long CurrentHp { get; }
        public IReadOnlyList<string> SkillIds { get; }
        public IReadOnlyDictionary<string, int> Cooldowns { get; }
        public int Level { get; }
        public Rarity Rarity { get; }
        public double ManagementScoreReduction { get; }
        public MonsterRole Role { get; }

        public MonsterSnapshot(MonsterId id, MonsterElement element, MonsterStats stats, long currentHp,
            IEnumerable<string> skillIds, IReadOnlyDictionary<string, int> cooldowns = null, int level = 1,
            Rarity rarity = Rarity.Common, double managementScoreReduction = 0, MonsterRole role = MonsterRole.Support)
        {
            if (string.IsNullOrWhiteSpace(id.Value)) throw new ArgumentException("Mã Monster không được rỗng.", nameof(id));
            if (!Enum.IsDefined(typeof(MonsterElement), element)) throw new ArgumentOutOfRangeException(nameof(element));
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            if (stats.Hp <= 0) throw new ArgumentOutOfRangeException(nameof(stats));
            if (level < 1 || level > 100) throw new ArgumentOutOfRangeException(nameof(level));
            if (!Enum.IsDefined(typeof(Rarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (!Enum.IsDefined(typeof(MonsterRole), role)) throw new ArgumentOutOfRangeException(nameof(role));
            if (double.IsNaN(managementScoreReduction) || double.IsInfinity(managementScoreReduction) || managementScoreReduction < 0) throw new ArgumentOutOfRangeException(nameof(managementScoreReduction));
            if (currentHp < 0 || currentHp > stats.Hp) throw new ArgumentOutOfRangeException(nameof(currentHp));
            var ids = (skillIds ?? throw new ArgumentNullException(nameof(skillIds))).ToArray();
            if (ids.Length == 0 || ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
                throw new ArgumentException("Bộ kỹ năng phải có mã hợp lệ và không trùng.", nameof(skillIds));
            var copiedCooldowns = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var skillId in ids) copiedCooldowns.Add(skillId, 0);
            foreach (var pair in cooldowns ?? new Dictionary<string, int>())
            {
                if (!copiedCooldowns.ContainsKey(pair.Key)) throw new ArgumentException("Hồi chiêu không thuộc bộ kỹ năng.", nameof(cooldowns));
                if (pair.Value < 0) throw new ArgumentOutOfRangeException(nameof(cooldowns));
                copiedCooldowns[pair.Key] = pair.Value;
            }
            Id = id;
            Element = element;
            Stats = stats;
            CurrentHp = currentHp;
            SkillIds = Array.AsReadOnly(ids);
            Cooldowns = new ReadOnlyDictionary<string, int>(copiedCooldowns);
            Level = level; Rarity = rarity; ManagementScoreReduction = managementScoreReduction; Role = role;
        }

        /// <summary>Chụp dữ liệu hiện tại; thay đổi thực thể sau đó không tác động ảnh chụp.</summary>
        public static MonsterSnapshot FromMonster(Monster monster, IEnumerable<string> skillIds,
            IReadOnlyDictionary<string, int> cooldowns = null, int totalMinutes = 0)
        {
            if (monster == null) throw new ArgumentNullException(nameof(monster));
            return new MonsterSnapshot(monster.Id, monster.Element, monster.CombatStatsAt(totalMinutes), monster.CurrentHp,
                skillIds, cooldowns, monster.Level, monster.Rarity, monster.RebellionReductionAt(totalMinutes), monster.Role);
        }
    }
}
