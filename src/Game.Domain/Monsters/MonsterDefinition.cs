using System;

namespace Game.Domain.Monsters
{
    /// <summary>Dữ liệu loài bất biến; các chỉ số chưa chốt GDD mang nhãn Prototype.</summary>
    public sealed class MonsterDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public MonsterElement Element { get; }
        public MonsterRole Role { get; }
        public MonsterStats BaseStats { get; }
        public MonsterStats GrowthStats { get; }
        public string BalanceStatus { get; }

        public MonsterDefinition(string id, string name, MonsterElement element, MonsterRole role,
            MonsterStats baseStats, MonsterStats growthStats, string balanceStatus = "Prototype")
        {
            Id = id;
            Name = name;
            Element = element;
            Role = role;
            BaseStats = baseStats;
            GrowthStats = growthStats;
            BalanceStatus = balanceStatus;
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("Thiếu mã hoặc tên loài Monster.");
            if (!Enum.IsDefined(typeof(MonsterElement), Element) || !Enum.IsDefined(typeof(MonsterRole), Role))
                throw new ArgumentException("Hệ hoặc vai trò Monster không hợp lệ.");
            if (BaseStats == null || GrowthStats == null)
                throw new ArgumentException("Thiếu bộ chỉ số cơ bản hoặc tăng trưởng.");
            if (BaseStats.Hp <= 0 || BaseStats.Attack <= 0 || BaseStats.Defense <= 0 || BaseStats.AttackSpeed <= 0)
                throw new ArgumentException("HP, ATK, DEF và ASPD cơ bản phải lớn hơn 0.");
            if (BalanceStatus != "Prototype" && BalanceStatus != "TBD" && BalanceStatus != "Locked")
                throw new ArgumentException("Trạng thái cân bằng không hợp lệ.");
        }
    }
}
