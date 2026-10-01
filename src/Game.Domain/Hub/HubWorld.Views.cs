using System.Collections.Generic;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>Ảnh chụp chỉ đọc của mọi Trainer (tạo mới mỗi lần gọi).</summary>
        public IReadOnlyList<TrainerView> Trainers
        {
            get
            {
                var list = new List<TrainerView>(trainers.Count);
                foreach (Trainer t in trainers)
                    list.Add(new TrainerView(
                        t.Id, t.Rarity, t.Personality, t.State, t.StateReason, t.Gold,
                        t.Needs.Stamina, t.Needs.Satiety, t.Needs.Hydration, t.Needs.Stress,
                        t.TeamHp, t.TeamHpMax, t.BackpackUnits, t.ContractWage, t.WageOwed, t.StrikeDaysLeft));
                return list;
            }
        }

        /// <summary>Ảnh chụp chỉ đọc của các công trình dịch vụ, theo thứ tự <see cref="BuildingKind"/>.</summary>
        public IReadOnlyList<BuildingView> Buildings
        {
            get
            {
                var list = new List<BuildingView>(buildings.Length);
                foreach (ServiceBuilding b in buildings)
                    list.Add(new BuildingView(b.Kind, b.Level, b.Slots, b.Occupied, b.QueueLength, b.MaxQueueLength, b.Price, b.FairPrice, b.Maintained));
                return list;
            }
        }
    }
}
