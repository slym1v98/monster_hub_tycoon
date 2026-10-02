using System;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    /// <summary>Quy tắc chọn nhóm mục tiêu; thứ tự chọn mục tiêu cụ thể thuộc bộ giải trận.</summary>
    public enum SkillTargetRule { Self, SingleEnemy, AllEnemies, SingleAlly, AllAllies }

    /// <summary>Dữ liệu kỹ năng bất biến; sức mạnh và hồi chiêu chưa được GDD khóa.</summary>
    public sealed class SkillDefinition
    {
        public string Id { get; }
        public MonsterElement Element { get; }
        /// <summary>Hệ số sức mạnh Prototype/TBD; cho phép 0 với kỹ năng chỉ có hiệu ứng.</summary>
        public double Power { get; }
        /// <summary>Số hành động hồi chiêu Prototype/TBD; 0 nghĩa là không cần chờ.</summary>
        public int CooldownActions { get; }
        public SkillTargetRule TargetRule { get; }
        /// <summary>Mã hiệu ứng được khai báo trong danh mục; null nghĩa là không có hiệu ứng.</summary>
        public string EffectId { get; }
        public string BalanceStatus { get; }

        public SkillDefinition(string id, MonsterElement element, double power, int cooldownActions,
            SkillTargetRule targetRule, string effectId = null, string balanceStatus = "Prototype")
        {
            Id = id;
            Element = element;
            Power = power;
            CooldownActions = cooldownActions;
            TargetRule = targetRule;
            EffectId = effectId;
            BalanceStatus = balanceStatus;
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id)) throw new ArgumentException("Mã kỹ năng không được rỗng.");
            if (!Enum.IsDefined(typeof(MonsterElement), Element)) throw new ArgumentException("Hệ kỹ năng không hợp lệ.");
            if (!Enum.IsDefined(typeof(SkillTargetRule), TargetRule)) throw new ArgumentException("Quy tắc mục tiêu không hợp lệ.");
            if (double.IsNaN(Power) || double.IsInfinity(Power) || Power < 0)
                throw new ArgumentException("Sức mạnh kỹ năng phải hữu hạn và không âm.");
            if (CooldownActions < 0) throw new ArgumentException("Số hành động hồi chiêu không được âm.");
            if (EffectId != null && string.IsNullOrWhiteSpace(EffectId))
                throw new ArgumentException("Mã hiệu ứng phải có giá trị hoặc là null.");
            if (BalanceStatus != "Prototype" && BalanceStatus != "TBD")
                throw new ArgumentException("Số liệu kỹ năng chưa khóa phải mang trạng thái Prototype hoặc TBD.");
        }
    }
}
