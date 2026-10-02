using System;

namespace Game.Domain.Combat
{
    /// <summary>Công thức và nhịp chiến đấu Prototype; ngưỡng Swap và quy tắc ngất giữ đúng GDD; GDD chưa khóa công thức và xác suất.</summary>
    public sealed class CombatConfig
    {
        public const string BalanceStatus = "Prototype";
        public SkillCatalog Skills { get; }
        /// <summary>Hệ số nhân sát thương chí mạng; mặc định Prototype 2, đơn vị lần.</summary>
        public double CriticalMultiplier { get; }
        /// <summary>Giới hạn vòng hành động Prototype 1000; hết giới hạn trả kết quả chưa phân thắng bại.</summary>
        public int MaxRounds { get; }
        public RebellionCombatConfig Rebellion { get; }
        public static CombatConfig Prototype { get; } = new CombatConfig(SkillCatalog.Prototype);

        public CombatConfig(SkillCatalog skills, double criticalMultiplier = 2, int maxRounds = 1000, RebellionCombatConfig rebellion = null)
        {
            Skills = skills ?? throw new ArgumentNullException(nameof(skills));
            if (double.IsNaN(criticalMultiplier) || double.IsInfinity(criticalMultiplier) || criticalMultiplier < 1)
                throw new ArgumentOutOfRangeException(nameof(criticalMultiplier));
            if (maxRounds < 1) throw new ArgumentOutOfRangeException(nameof(maxRounds));
            CriticalMultiplier = criticalMultiplier;
            MaxRounds = maxRounds;
            Rebellion = rebellion ?? RebellionCombatConfig.Prototype;
        }
    }
    /// <summary>Prototype: xác suất min(maxProbability, thiếu điểm * hệ số); một lần rút cho mỗi Monster tham gia.</summary>
    public sealed class RebellionCombatConfig
    {
        public const string BalanceStatus = "Prototype";
        public double ProbabilityPerMissingPoint { get; }
        public double MaxProbability { get; }
        public double SkipWeight { get; }
        public double SleepWeight { get; }
        public double AreaAggroWeight { get; }
        public int SkipActions { get; }
        public int SleepActions { get; }
        public static RebellionCombatConfig Prototype { get; } = new RebellionCombatConfig();

        public RebellionCombatConfig(double probabilityPerMissingPoint = .01, double maxProbability = 1,
            double skipWeight = 1, double sleepWeight = 1, double areaAggroWeight = 1,
            int skipActions = 1, int sleepActions = 1)
        {
            foreach (double value in new[] { probabilityPerMissingPoint, maxProbability, skipWeight, sleepWeight, areaAggroWeight })
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(probabilityPerMissingPoint));
            if (maxProbability > 1) throw new ArgumentOutOfRangeException(nameof(maxProbability));
            double total = skipWeight + sleepWeight + areaAggroWeight;
            if (total <= 0 || double.IsInfinity(total)) throw new ArgumentException("Tổng trọng số phải dương và hữu hạn.");
            if (skipActions < 1) throw new ArgumentOutOfRangeException(nameof(skipActions));
            if (sleepActions < 1) throw new ArgumentOutOfRangeException(nameof(sleepActions));
            ProbabilityPerMissingPoint = probabilityPerMissingPoint;
            MaxProbability = maxProbability;
            SkipWeight = skipWeight;
            SleepWeight = sleepWeight;
            AreaAggroWeight = areaAggroWeight;
            SkipActions = skipActions;
            SleepActions = sleepActions;
        }
    }
}
