using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    public enum BattleActionKind { Skill, Wait, RoundCompleted, Swap, Rebellion }
    public enum RebellionOutcome { None, Obey, Skip, Sleep, AreaAggroAttack }

    /// <summary>
    /// Bản ghi replay bất biến, Sequence và Turn bắt đầu từ 1. Damage là HP thực mất sau giới hạn HP.
    /// Mỗi mục tiêu của skill diện rộng có một bản ghi; cùng lần dùng có chung chí mạng và hồi chiêu.
    /// RoundCompleted không có actor/target, đánh dấu đúng thời điểm giảm hồi chiêu.
    /// Swap: actor cũ, target Active mới, ResultingHp là HP của Active mới; không trừ HP hay đặt hồi chiêu.
    /// Fainted đánh dấu mục tiêu của Skill vừa về 0 HP; danh tính được giữ trong kết quả, không chết.
    /// Rebellion ghi quyết định một lần mỗi Monster; Wait ghi Skip/Sleep, Skill ghi tấn công diện rộng bất tuân.
    /// </summary>
    public sealed record BattleAction(int Sequence, int Turn, BattleActionKind Kind, MonsterId? ActorId,
        string SkillId, MonsterId? TargetId, long Damage, long Heal, double Effectiveness, long ResultingHp,
        bool IsCritical, int SkillCooldown, bool Swap, bool Fainted, RebellionOutcome Rebellion);
}
