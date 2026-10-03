using System;

namespace Game.Domain.Monsters
{
    /// <summary>Bộ chỉ số bất biến; cho phép 0 để biểu diễn tăng trưởng chưa cấu hình.</summary>
    public sealed record MonsterStats
    {
        public long Hp { get; }
        public double Attack { get; }
        public double Defense { get; }
        public double AttackSpeed { get; }
        public double CriticalChance { get; }

        public MonsterStats(long hp, double attack, double defense, double attackSpeed, double criticalChance)
        {
            if (hp < 0) throw new ArgumentOutOfRangeException(nameof(hp));
            ValidateNonnegative(attack, nameof(attack));
            ValidateNonnegative(defense, nameof(defense));
            ValidateNonnegative(attackSpeed, nameof(attackSpeed));
            ValidateNonnegative(criticalChance, nameof(criticalChance));
            if (criticalChance > 1) throw new ArgumentOutOfRangeException(nameof(criticalChance));
            Hp = hp;
            Attack = attack;
            Defense = defense;
            AttackSpeed = attackSpeed;
            CriticalChance = criticalChance;
        }

        static void ValidateNonnegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
