using System;
using System.Collections.Generic;
using Game.Domain.Materials;
using Game.Domain.Gear;
using Game.Domain.Monsters;

namespace Game.Domain
{
    /// <summary>Trạng thái hiện tại của Trainer trong FSM.</summary>
    public enum TrainerState { AtHub, Traveling, Farming, Returning, Queued, InService, WaitingForMoney, WaitingForMarket }

    /// <summary>Lý do Trainer quyết định về HUB. <see cref="None"/> nghĩa là tiếp tục farm.</summary>
    public enum ReturnReason { None, Strike, Night, TeamDown, BackpackFull, Tired, Hungry, Thirsty }

    /// <summary>Bốn thanh nhu cầu, thang 0-100. Thể lực/No nê/Nước hồi ở dịch vụ; Stress càng cao càng tệ.</summary>
    public sealed class Needs
    {
        public double Stamina = 100;
        public double Satiety = 100;
        public double Hydration = 100;
        public double Stress = 0;

        /// <summary>Thấp nhất trong ba thanh thể chất (không tính Stress).</summary>
        public double LowestPhysical => Math.Min(Stamina, Math.Min(Satiety, Hydration));

        /// <summary>Ép mọi thanh về khoảng 0-100.</summary>
        public void Clamp()
        {
            Stamina = Math.Max(0, Math.Min(100, Stamina));
            Satiety = Math.Max(0, Math.Min(100, Satiety));
            Hydration = Math.Max(0, Math.Min(100, Hydration));
            Stress = Math.Max(0, Math.Min(100, Stress));
        }
    }

    /// <summary>
    /// Dữ liệu phẳng của một Trainer. Chỉ chứa dữ liệu; logic nằm ở TrainerBrain và HubWorld.
    /// Trainer không có HP riêng; HP thuộc từng Monster trong Roster.
    /// </summary>
    public sealed class Trainer
    {
        public int Id;
        public Rarity Rarity;
        int rank = 1;
        int level = 1;
        public int Rank
        {
            get => rank;
            set { MonsterProgression.UpdateLevels(this, value, level); rank = value; }
        }
        public int Level
        {
            get => level;
            set { MonsterProgression.UpdateLevels(this, rank, value); level = value; }
        }
        /// <summary>EXP còn lại tới cấp kế tiếp; Lv100 bỏ phần dư, chưa thực hiện Rebirth.</summary>
        public long Experience { get; internal set; }
        public TrainerAttributes Attributes { get; }
        public TrainerClass Class { get; internal set; } = TrainerClass.None;

        public Personality Personality;
        public readonly Needs Needs = new Needs();
        public long Gold;
        public MonsterRoster Roster { get; }
        public TrainerInventory Inventory { get; } = new TrainerInventory();
        /// <summary>Trang bị Tiện ích + Hào quang của Trainer (12 slot); chỉ số cộng dồn.</summary>
        public GearLoadout Gear { get; } = new GearLoadout();
        public int BackpackUnits;
        public readonly Dictionary<MaterialId, int> BackpackMaterials = new Dictionary<MaterialId, int>();
        public int BackpackCapacity;
        /// <summary>Cờ tạm thay cho slot Kính (sub-project 4).</summary>
        public bool HasNightVision;
        public string CurrentZoneId;
        public double LeadershipItemBonus;
        public int BagSynergyExpiresAtMinute = -1;

        // --- Lương ---
        public long ContractWage;
        /// <summary>HUB còn nợ Trainer (nợ lương cộng dồn).</summary>
        public long WageOwed;
        /// <summary>Trainer đã ứng trước, trừ vào Payday kế tiếp.</summary>
        public long WageAdvance;
        public int StrikeDaysLeft;

        public TrainerState State;
        public string StateReason = "";

        // --- Nội bộ của động cơ ---
        /// <summary>Tăng mỗi khi ngắt Trainer giữa chừng; sự kiện mang token cũ sẽ bị bỏ qua.</summary>
        public int Token;
        /// <summary>Lần cuối trừ/cộng nhu cầu theo thời gian trôi.</summary>
        public int LastSettleMinute;
        public int WaitingSinceMinute;
        public int MarketWaitSinceMinute;
        public BuildingKind PendingService;

        public Trainer(SimRandom random = null, TrainerAttributeConfig attributeConfig = null)
        {
            Roster = new MonsterRoster(this);
            Attributes = TrainerAttributes.Generate(random ?? new SimRandom(0),
                attributeConfig ?? TrainerAttributeConfig.Prototype);
        }

        public bool IsOnStrike => StrikeDaysLeft > 0;
    }
}
