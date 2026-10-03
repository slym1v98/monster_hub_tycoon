using System.Collections.Generic;
using System.Linq;
using System;
using Game.Domain.Monsters;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        public IReadOnlyList<StockCompanyView> StockCompanies => stockExchange.Companies;
        public IReadOnlyList<StockHoldingView> StockHoldingsForTrainer(int trainerId)
            => trainerId < 0 || trainerId >= trainers.Count ? Array.Empty<StockHoldingView>() : stockExchange.HoldingsFor(trainerId);

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
                        t.BackpackUnits, t.ContractWage, t.WageOwed, t.StrikeDaysLeft, t.HubLoanBalance, t.ReverseLoanBalance,
                        t.ReverseLoanPaydaysRemaining, t.ReverseLoanOverdue,
                        System.Array.AsReadOnly(t.Roster.Members.Select(m => ViewMonster(m, m == t.Roster.Active)).ToArray()), t.CurrentZoneId, t.Rank, t.Level,
                        t.Inventory.Products));
                return list.AsReadOnly();
            }
        }

        static MonsterView ViewMonster(Monster m, bool active = false) => new MonsterView(m.Id.Value, m.SpeciesId,
            m.Element, m.Level, m.CurrentHp, m.MaxHp, m.LifeState, active, m.Rarity, m.KnownIv, m.Custody, m.IsSoulBound);

        /// <summary>Field, local storage, bank deposits and captures recovering for this Trainer, in stable ID order.</summary>
        public IReadOnlyList<MonsterView> MonstersForTrainer(int trainerId)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return Array.AsReadOnly(Array.Empty<MonsterView>());
            var t = trainers[trainerId];
            return Array.AsReadOnly(t.Roster.Members.Concat(t.Roster.Storage)
                .Concat(geneBank.StoredMonsters.Where(m => geneBank.GetStoredMonster(trainerId, m.Id) != null))
                .Concat(veterinaryHospital.Recoveries.Where(r => r.TrainerId == trainerId && r.IsCapturedMonster)
                    .Select(r => veterinaryHospital.RecoveringMonster(r.RecoveryId)))
                .OrderBy(m => m.Id.Value, StringComparer.Ordinal).Select(m => ViewMonster(m, m == t.Roster.Active)).ToArray());
        }

        public IReadOnlyList<ZoneView> Zones => Array.AsReadOnly(cfg.ZoneCatalogSettings.Definitions
            .OrderBy(z => z.Id, StringComparer.Ordinal).Select(z => new ZoneView(z.Id, z.DisplayName,
                z.MinimumRank, z.WalkMinutes, unlockedZoneIds.Contains(z.Id))).ToArray());

        public VeterinaryHospitalView VeterinaryHospital => new VeterinaryHospitalView(veterinaryHospital.RecoveryBedCapacity,
            veterinaryHospital.EmergencyBedCapacity, veterinaryHospital.OccupiedRecoveryBeds, veterinaryHospital.OccupiedEmergencyBeds,
            Array.AsReadOnly(veterinaryHospital.Recoveries.Select(r => new MonsterRecoveryView(r.RecoveryId, r.TrainerId,
                ViewMonster(veterinaryHospital.RecoveringMonster(r.RecoveryId)), r.CompleteAtMinute, r.Fee, r.IsCapturedMonster)).ToArray()));

        public GeneBankView GeneBank => new GeneBankView((cfg.GeneBankSettings ?? GeneBankConfig.Prototype).Capacity,
            geneBank.Count, geneBank.ConfiscatedCount,
            Array.AsReadOnly(geneBank.StoredMonsters.Select(m => new BankMonsterView(
                trainers.Single(t => geneBank.GetStoredMonster(t.Id, m.Id) != null).Id, ViewMonster(m))).ToArray()),
            Array.AsReadOnly(geneBank.ConfiscatedMonsters.Select(m => ViewMonster(m)).ToArray()));

        /// <summary>Ảnh chụp chỉ đọc của các công trình dịch vụ, theo thứ tự <see cref="BuildingKind"/>.</summary>
        public IReadOnlyList<BuildingView> Buildings
        {
            get
            {
                var list = new List<BuildingView>(buildings.Length);
                foreach (ServiceBuilding b in buildings)
                    list.Add(new BuildingView(b.Kind, b.Level, b.Slots, b.Occupied, b.QueueLength, b.MaxQueueLength, b.Price, b.FairPrice, b.Maintained));
                return list.AsReadOnly();
            }
        }
    }
}
