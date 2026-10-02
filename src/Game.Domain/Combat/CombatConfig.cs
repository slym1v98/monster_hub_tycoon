using System;

namespace Game.Domain.Combat
{
    /// <summary>Nền cấu hình chiến đấu Prototype; tác vụ giải trận bổ sung công thức và nhịp hành động.</summary>
    public sealed class CombatConfig
    {
        public const string BalanceStatus = "Prototype";
        public SkillCatalog Skills { get; }
        public static CombatConfig Prototype { get; } = new CombatConfig(SkillCatalog.Prototype);

        public CombatConfig(SkillCatalog skills)
        {
            Skills = skills ?? throw new ArgumentNullException(nameof(skills));
        }
    }
}
