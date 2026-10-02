using System;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    /// <summary>Bảng hệ cố định theo GDD 04 §2; không có miễn nhiễm gây sát thương bằng 0.</summary>
    public static class ElementChart
    {
        // Hàng tấn công, cột phòng thủ: Lửa, Nước, Cỏ, Sét, Băng, Độc, Đất, Sáng, Tối.
        private static readonly double[,] Multipliers =
        {
            { 0.5, 0.5, 2, 1, 2, 1, 1, 1, 1 },
            { 2, 0.5, 0.5, 1, 1, 1, 2, 1, 1 },
            { 0.5, 2, 0.5, 1, 1, 0.5, 2, 1, 1 },
            { 1, 2, 0.5, 0.5, 1, 1, 0.5, 1, 1 },
            { 0.5, 0.5, 2, 1, 0.5, 1, 2, 1, 1 },
            { 1, 1, 2, 1, 1, 0.5, 0.5, 1, 1 },
            { 2, 1, 0.5, 2, 1, 2, 1, 1, 1 },
            { 1, 1, 1, 1, 1, 1, 1, 0.5, 2 },
            { 1, 1, 1, 1, 1, 1, 1, 2, 0.5 }
        };

        public static double Multiplier(MonsterElement attack, MonsterElement defense)
        {
            if (!Enum.IsDefined(typeof(MonsterElement), attack)) throw new ArgumentOutOfRangeException(nameof(attack));
            if (!Enum.IsDefined(typeof(MonsterElement), defense)) throw new ArgumentOutOfRangeException(nameof(defense));
            return Multipliers[(int)attack, (int)defense];
        }
    }
}
