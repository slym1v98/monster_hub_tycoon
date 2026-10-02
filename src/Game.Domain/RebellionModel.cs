using System;
using Game.Domain.Monsters;

namespace Game.Domain
{
    /// <summary>Tính Rebellion theo cấp Monster và cấp Quản Lý Trainer quy đổi từ Rank/Lv.</summary>
    public static class RebellionModel
    {
        public const double K = 20;
        public const double LeadershipBase = 20;

        public static double ManagementScore(int monsterLevel, Rarity monsterRarity, double k = K)
            => monsterLevel + k * (int)monsterRarity;

        /// <summary>Điểm Lãnh đạo dùng cấp Quản Lý Trainer phân số theo công thức GDD.</summary>
        public static double LeadershipScore(int trainerRank, int trainerLevel, Rarity trainerRarity, double bonus = 0)
            => LeadershipBase + K * (int)trainerRarity
                + MonsterProgression.TrainerManagementLevel(trainerRank, trainerLevel) + bonus;

        /// <summary>Điểm Lãnh đạo tương thích với API cũ; cấp truyền vào không được quy đổi theo Rank.</summary>
        [Obsolete("Dùng overload rank-aware với trainerRank và trainerLevel.")]
        public static double LeadershipScore(int trainerLevel, Rarity trainerRarity, double bonus = 0, double k = K)
            => LeadershipBase + k * (int)trainerRarity + trainerLevel + bonus;

        public static bool IsRebellious(int monsterLevel, Rarity monsterRarity, int trainerRank, int trainerLevel,
            Rarity trainerRarity, double bonus = 0)
            => ManagementScore(monsterLevel, monsterRarity)
                > LeadershipScore(trainerRank, trainerLevel, trainerRarity, bonus);

        /// <summary>Kết quả tương thích với API cũ; dùng overload rank-aware cho tính toán mới.</summary>
        [Obsolete("Dùng overload rank-aware với trainerRank và trainerLevel.")]
        public static bool IsRebellious(int monsterLevel, Rarity monsterRarity, int trainerLevel, Rarity trainerRarity, double bonus = 0)
            => ManagementScore(monsterLevel, monsterRarity) > LeadershipScore(trainerLevel, trainerRarity, bonus);
    }
}
