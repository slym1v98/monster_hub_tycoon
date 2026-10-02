using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    /// <summary>Hai phía tham chiến đã chụp dữ liệu; vị trí Active/Reserve được bổ sung ở tác vụ đội hình.</summary>
    public sealed class BattleInput
    {
        public IReadOnlyList<MonsterSnapshot> Team { get; }
        public IReadOnlyList<MonsterSnapshot> Opponents { get; }

        public BattleInput(IEnumerable<MonsterSnapshot> team, IEnumerable<MonsterSnapshot> opponents)
        {
            Team = Copy(team, nameof(team));
            Opponents = Copy(opponents, nameof(opponents));
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
}
