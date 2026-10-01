using System;

namespace Game.Domain
{
    /// <summary>
    /// Rebellion: Monster bat tuan khi Diem Quan Ly > Diem Lanh dao cua Trainer (docs/designs/04).
    /// Moi bac Rarity tuong duong K cap do (K=20): Monster hon Trainer 1 bac thi chi quan ly duoc neu cap do khong cao hon Trainer.
    /// </summary>
    public static class RebellionModel
    {
        public const double K = 20;
        public const double LeadershipBase = 20;

        public static double ManagementScore(int monsterLevel, Rarity monsterRarity, double k = K)
            => monsterLevel + k * (int)monsterRarity;

        /// <summary>Diem Lanh dao = nen + K x bac Rarity cua Trainer + Cap do Trainer + thuong (Khoa Giao Tiep, Hoc Vien...).</summary>
        public static double LeadershipScore(int trainerLevel, Rarity trainerRarity, double bonus = 0, double k = K)
            => LeadershipBase + k * (int)trainerRarity + trainerLevel + bonus;

        public static bool IsRebellious(int monsterLevel, Rarity monsterRarity, int trainerLevel, Rarity trainerRarity, double bonus = 0)
            => ManagementScore(monsterLevel, monsterRarity) > LeadershipScore(trainerLevel, trainerRarity, bonus);
    }
}
