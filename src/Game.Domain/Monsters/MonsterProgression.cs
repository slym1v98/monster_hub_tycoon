using System;
using System.Linq;

namespace Game.Domain.Monsters
{
    /// <summary>Quy đổi cấp theo GDD; Monster hiện có chỉ tăng cấp, không bị hạ khi Trainer đổi Rank/Lv.</summary>
    public static class MonsterProgression
    {
        public static int MonsterLevel(int trainerRank, int trainerLevel)
        {
            Validate(trainerRank, trainerLevel);
            return 20 * (trainerRank - 1) + (trainerLevel + 4) / 5;
        }

        /// <summary>Cấp Quản Lý giữ phần lẻ Lv/5 theo GDD, không làm tròn như cấp Monster.</summary>
        public static double TrainerManagementLevel(int trainerRank, int trainerLevel)
        {
            Validate(trainerRank, trainerLevel);
            return 20 * (trainerRank - 1) + trainerLevel / 5.0;
        }

        internal static void UpdateLevels(Trainer trainer, int rank, int level)
        {
            int target = MonsterLevel(rank, level);
            // Tính hết trước khi áp dụng: lỗi chỉ số không làm đội cập nhật nửa chừng.
            var updates = trainer.Roster.Members.Concat(trainer.Roster.Storage)
                .OrderBy(monster => monster.Id.Value, StringComparer.Ordinal)
                .Select(monster => (Monster: monster, Stats: monster.StatsAtLevel(target))).ToArray();
            foreach (var update in updates)
                update.Monster.ApplyLevel(target, update.Stats);
        }

        static void Validate(int rank, int level)
        {
            if (rank < 1 || rank > 5) throw new ArgumentOutOfRangeException(nameof(rank));
            if (level < 1 || level > 100) throw new ArgumentOutOfRangeException(nameof(level));
        }
    }
}
