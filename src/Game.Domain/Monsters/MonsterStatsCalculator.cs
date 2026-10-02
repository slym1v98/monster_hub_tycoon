using System;

namespace Game.Domain.Monsters
{
    /// <summary>Chỉ số Prototype = (cơ bản + tăng trưởng × (Lv − 1) × IV × hệ số) × Rarity.</summary>
    public static class MonsterStatsCalculator
    {
        // GDD 04 §1 và 13 §13: IV chỉ nhân phần tăng trưởng, không sinh lại gen.
        static readonly double[] IvMultipliers = { 0.8, 0.9, 1.0, 1.1, 1.2, 1.3, 1.5 };

        public static MonsterStats Calculate(MonsterDefinition definition, Rarity rarity, MonsterIvGrade iv,
            int level, MonsterStatConfig config)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (config == null) throw new ArgumentNullException(nameof(config));
            definition.Validate();
            if (!Enum.IsDefined(typeof(Rarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (!Enum.IsDefined(typeof(MonsterIvGrade), iv)) throw new ArgumentOutOfRangeException(nameof(iv));
            if (level < 1 || level > 100) throw new ArgumentOutOfRangeException(nameof(level));
            var basis = definition.BaseStats;
            var growth = definition.GrowthStats;
            double scale = (level - 1) * IvMultipliers[(int)iv] * config.GrowthFactor;
            double rarityScale = config.RarityMultipliers[(int)rarity];
            double hp = Derive(basis.Hp, growth.Hp, scale, rarityScale);
            // HP làm tròn lên; 2^63 là biên loại trừ vì double không biểu diễn chính xác long.MaxValue.
            hp = Math.Ceiling(hp);
            if (hp >= 9223372036854775808.0) throw new OverflowException("HP vượt giới hạn số nguyên.");
            return new MonsterStats(Math.Max(1, (long)hp),
                Derive(basis.Attack, growth.Attack, scale, rarityScale),
                Derive(basis.Defense, growth.Defense, scale, rarityScale),
                Derive(basis.AttackSpeed, growth.AttackSpeed, scale, rarityScale),
                Math.Min(1, Derive(basis.CriticalChance, growth.CriticalChance, scale, rarityScale)));
        }

        static double Derive(double basis, double growth, double scale, double rarity)
        {
            double value = (basis + growth * scale) * rarity;
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new OverflowException("Chỉ số Monster vượt giới hạn số thực.");
            return value;
        }
    }
}
