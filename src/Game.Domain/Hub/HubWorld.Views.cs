using System.Collections.Generic;
using System.Linq;
using System;
using Game.Domain.Monsters;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        public QuestView Quests => questTracker.View;
        public HubReputationView Reputation => HubReputation.Calculate(buildings, trainers, Bankruptcies, cfg.HubReputationSettings);
        public IReadOnlyList<HubEventView> ActiveEvents
        {
            get
            {
                int payday = SimClock.PaydayMinute(paydayIndex);
                var events = new System.Collections.Generic.List<HubEventView>();
                if (HubEventCalendar.IsBlackFriday(now, payday, cfg.EventSettings))
                    events.Add(new HubEventView(HubEventKind.BlackFriday,
                        payday - cfg.EventSettings.BlackFridayDays * SimClock.MinutesPerDay, payday, true));
                if (inspectionFinishMinute >= 0)
                    events.Add(new HubEventView(HubEventKind.LaborInspection, now, inspectionFinishMinute, true));
                if (monsterFluFinishMinute >= 0)
                    events.Add(new HubEventView(HubEventKind.MonsterFlu, monsterFluStartMinute, monsterFluFinishMinute, true));
                if (breedingSeasonFinishMinute >= 0)
                    events.Add(new HubEventView(HubEventKind.BreedingSeason, breedingSeasonStartMinute, breedingSeasonFinishMinute, true));
                if (monsterSiegeFinishMinute >= 0)
                    events.Add(new HubEventView(HubEventKind.MonsterSiege, monsterSiegeStartMinute, monsterSiegeFinishMinute, true));
                if (worldBossFinishMinute >= 0)
                    events.Add(new HubEventView(HubEventKind.WorldBossRaid, worldBossStartMinute, worldBossFinishMinute, true));
                return Array.AsReadOnly(events.ToArray());
            }
        }
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

        public IReadOnlyList<HubFacilityView> Facilities => Array.AsReadOnly(HubFacilityCatalog.Definitions
            .Select(definition =>
            {
                bool zoneReady = definition.RequiredZone <= 1 || unlockedZoneIds.Contains("zone_" + definition.RequiredZone);
                bool unlocked = townHallLevel >= definition.TownHallUnlockLevel && zoneReady;
                int maxAllowed = !unlocked ? 0 : definition.Id == "dormitory"
                    ? Math.Min(definition.MaxLevel, 1 + townHallLevel / cfg.HubProgressionSettings.TownHallLevelsPerTier)
                    : definition.MaxLevel == 5
                        ? Math.Min(definition.MaxLevel, (townHallLevel - 1) / cfg.HubProgressionSettings.TownHallLevelsPerTier + 1)
                        : Math.Min(definition.MaxLevel, townHallLevel);
                int level = unlocked ? FacilityLevel(definition.Id, definition.StartsRebuilt) : 0;
                facilityStates.TryGetValue(definition.Id, out var runtime);
                BuildingKind? serviceKind = ServiceBuildingForFacility(definition.Id);
                bool powered = serviceKind.HasValue ? buildings[(int)serviceKind.Value].Level > 0 && buildings[(int)serviceKind.Value].PoweredOn : runtime?.PoweredOn ?? false;
                bool maintained = serviceKind.HasValue ? buildings[(int)serviceKind.Value].Maintained : runtime?.Maintained ?? true;
                bool damaged = serviceKind.HasValue ? buildings[(int)serviceKind.Value].Damaged : runtime?.Damaged ?? false;
                string state = !unlocked ? "Locked" : runtime?.PendingLevel >= 0 ? "ConstructionInProgress"
                    : level == 0 ? "Available" : !powered ? "PoweredOff"
                    : !maintained ? "MaintenanceDeficit" : damaged ? "Damaged" : "Operational";
                return new HubFacilityView(definition.Id, level, definition.MaxLevel, maxAllowed,
                    definition.TownHallUnlockLevel, definition.RequiredZone, unlocked, definition.StartsRebuilt, state,
                    runtime?.CompletionMinute >= 0 ? runtime.CompletionMinute : runtime?.RepairFinishMinute >= 0 ? runtime.RepairFinishMinute : null,
                    powered, maintained, damaged);
            }).ToArray());

        int FacilityLevel(string id, bool startsRebuilt)
        {
            if (id == "town_hall") return townHallLevel;
            if (id == "dormitory") return dormitoryLevel;
            BuildingKind? kind = id == "inn" ? BuildingKind.Inn : id == "restaurant" ? BuildingKind.Restaurant :
                id == "bar" ? BuildingKind.Bar : id == "veterinary_hospital" ? BuildingKind.Hospital : (BuildingKind?)null;
            if (kind.HasValue) return buildings[(int)kind.Value].Level;
            return facilityStates.TryGetValue(id, out var state) ? state.Level : startsRebuilt ? 1 : 0;
        }

        public HubProgressionView Progression => new HubProgressionView(townHallLevel,
            (townHallLevel - 1) / cfg.HubProgressionSettings.TownHallLevelsPerTier + 1, dormitoryLevel,
            cfg.HubProgressionSettings.PopulationCaps.Where((_, index) => unlockedZoneIds.Contains("zone_" + (index + 1))).DefaultIfEmpty(0).Max(),
            trainers.Count, townHallUpgradeFinishMinute < 0 ? (int?)null : townHallUpgradeFinishMinute,
            dormitoryUpgradeFinishMinute < 0 ? (int?)null : dormitoryUpgradeFinishMinute);

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
                    list.Add(new BuildingView(b.Kind, b.Level, b.Slots, b.Occupied, b.QueueLength, b.MaxQueueLength,
                        b.Price, b.FairPrice, b.Maintained, b.PoweredOn, b.Damaged, b.FullSlots, b.QualityMultiplier,
                        buildingRepairFinishMinutes[(int)b.Kind] < 0 ? (int?)null : buildingRepairFinishMinutes[(int)b.Kind]));
                return list.AsReadOnly();
            }
        }
    }
}
