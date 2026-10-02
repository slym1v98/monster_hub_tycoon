using System;

namespace Game.Domain.Combat
{
    /// <summary>Công thức và nhịp chiến đấu Prototype theo brief tác vụ 5; GDD chưa khóa các giá trị này.</summary>
    public sealed class CombatConfig
    {
        public const string BalanceStatus = "Prototype";
        public SkillCatalog Skills { get; }
        /// <summary>Hệ số nhân sát thương chí mạng; mặc định Prototype 2, đơn vị lần.</summary>
        public double CriticalMultiplier { get; }
        /// <summary>Giới hạn vòng hành động Prototype 1000; hết giới hạn trả kết quả chưa phân thắng bại.</summary>
        public int MaxRounds { get; }
        public static CombatConfig Prototype { get; } = new CombatConfig(SkillCatalog.Prototype);

        public CombatConfig(SkillCatalog skills, double criticalMultiplier = 2, int maxRounds = 1000)
        {
            Skills = skills ?? throw new ArgumentNullException(nameof(skills));
            if (double.IsNaN(criticalMultiplier) || double.IsInfinity(criticalMultiplier) || criticalMultiplier < 1)
                throw new ArgumentOutOfRangeException(nameof(criticalMultiplier));
            if (maxRounds < 1) throw new ArgumentOutOfRangeException(nameof(maxRounds));
            CriticalMultiplier = criticalMultiplier;
            MaxRounds = maxRounds;
        }
    }
}
