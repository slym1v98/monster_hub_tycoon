using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    public enum BattleActionKind { Skill, Wait, RoundCompleted }
    public enum RebellionOutcome { None, Obey, Skip, Sleep, AreaAggroAttack }

    /// <summary>
    /// Bản ghi replay bất biến, Sequence và Turn bắt đầu từ 1. Damage là HP thực mất sau giới hạn HP.
    /// Mỗi mục tiêu của skill diện rộng có một bản ghi; cùng lần dùng có chung chí mạng và hồi chiêu.
    /// RoundCompleted không có actor/target, đánh dấu đúng thời điểm giảm hồi chiêu.
    /// Swap/Fainted/Rebellion dành cho tác vụ đội hình; bộ giải cơ sở chưa phát các chuyển trạng thái đó.
    /// </summary>
    public sealed record BattleAction(int Sequence, int Turn, BattleActionKind Kind, MonsterId? ActorId,
        string SkillId, MonsterId? TargetId, long Damage, long Heal, double Effectiveness, long ResultingHp,
        bool IsCritical, int SkillCooldown, bool Swap, bool Fainted, RebellionOutcome Rebellion);
}
