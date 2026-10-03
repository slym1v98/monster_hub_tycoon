using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Supply;
using Game.Domain.Monsters;
using Game.Domain.Combat;
using Game.Domain.Gear;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        void OnRandomCrisisCheck()
        {
            if (monsterFluFinishMinute < 0 && breedingSeasonFinishMinute < 0 && monsterSiegeFinishMinute < 0 && worldBossFinishMinute < 0
                && rng.NextDouble() < cfg.EventSettings.RandomCrisisProbability)
            {
                switch (rng.NextInt(3))
                {
                    case 0: ActivateMonsterFlu(); break;
                    case 1: ActivateBreedingSeason(); break;
                    default: ActivateMonsterSiege(); break;
                }
            }
            if (now <= int.MaxValue - cfg.EventSettings.RandomCrisisCheckIntervalMinutes)
                queue.Schedule(now + cfg.EventSettings.RandomCrisisCheckIntervalMinutes, SimEventKind.RandomCrisisCheck);
        }

        public CommandResult UseVaccineAgainstMonsterFlu()
        {
            if (monsterFluFinishMinute < 0 || now >= monsterFluFinishMinute) return CommandResult.Rejected("event.not_active");
            if (station == null) return CommandResult.Rejected("event.vaccine_stock_unavailable");
            var vaccine = new ProductId("vaccine");
            int units = cfg.EventSettings.MonsterFluVaccineUnitsToCure;
            if (station.Stock.Get(new InventoryItem(vaccine)).Available < units) return CommandResult.Rejected("event.vaccine_unavailable");
            station.Stock.Remove(new InventoryItem(vaccine), units);
            monsterFluFinishMinute = -1;
            monsterFluStartMinute = -1;
            Raise(new SupplyStockChanged(now, "product:vaccine", station.Stock.Get(new InventoryItem(vaccine))));
            Raise(new HubEventChanged(now, HubEventKind.MonsterFlu, "Cured", 0, units));
            return CommandResult.Success();
        }

        public CommandResult ActivateMonsterFlu(int durationDays = 0)
        {
            if (monsterFluFinishMinute >= 0) return CommandResult.Rejected("event.already_active");
            int duration = durationDays == 0 ? cfg.EventSettings.MonsterFluDurationDays : durationDays;
            if (duration <= 0) return CommandResult.Rejected("event.invalid_duration");
            int finish = checked(now + checked(duration * SimClock.MinutesPerDay));
            monsterFluStartMinute = now;
            monsterFluFinishMinute = finish;
            foreach (var monster in AllMonsters())
                monster.SetCurrentHp((long)Math.Floor(monster.CurrentHp * cfg.EventSettings.MonsterFluInitialHpFraction));
            queue.Schedule(monsterFluFinishMinute, SimEventKind.MonsterFluEnd);
            Raise(new HubEventChanged(now, HubEventKind.MonsterFlu, "Started", 0, AllMonsters().Count()));
            return CommandResult.Success();
        }

        public CommandResult ActivateBreedingSeason(int durationDays = 0)
        {
            if (breedingSeasonFinishMinute >= 0) return CommandResult.Rejected("event.already_active");
            int duration = durationDays == 0 ? cfg.EventSettings.BreedingSeasonDurationDays : durationDays;
            if (duration <= 0) return CommandResult.Rejected("event.invalid_duration");
            int finish = checked(now + checked(duration * SimClock.MinutesPerDay));
            breedingSeasonStartMinute = now;
            breedingSeasonFinishMinute = finish;
            queue.Schedule(breedingSeasonFinishMinute, SimEventKind.BreedingSeasonEnd);
            queue.Schedule(now, SimEventKind.BreedingSeasonDemand);
            Raise(new HubEventChanged(now, HubEventKind.BreedingSeason, "Started", 0, 0));
            return CommandResult.Success();
        }

        void OnBreedingSeasonEnd()
        {
            if (breedingSeasonFinishMinute < 0 || now < breedingSeasonFinishMinute) return;
            breedingSeasonFinishMinute = -1;
            breedingSeasonStartMinute = -1;
            Raise(new HubEventChanged(now, HubEventKind.BreedingSeason, "Ended", 0, 0));
        }

        bool IsBreedingSeasonActive => breedingSeasonFinishMinute >= 0 && now < breedingSeasonFinishMinute;

        void OnBreedingSeasonDemand()
        {
            if (!IsBreedingSeasonActive) return;
            if (useSupplyChain)
            {
                int units = Math.Max(1, (int)Math.Ceiling(cfg.EventSettings.BreedingSeasonDemandMultiplier));
                int purchases = 0;
                foreach (var trainer in trainers.OrderBy(t => t.Id))
                    foreach (string id in new[] { "capture_ball", "trap" })
                        if (PurchaseProduct(trainer.Id, id, units).Ok) purchases++;
                Raise(new HubEventChanged(now, HubEventKind.BreedingSeason, "CaptureGoodsDemand", 0, purchases));
            }
            int next = checked(now + SimClock.MinutesPerDay);
            if (next < breedingSeasonFinishMinute) queue.Schedule(next, SimEventKind.BreedingSeasonDemand);
        }

        public CommandResult ActivateMonsterSiege()
        {
            if (monsterSiegeFinishMinute >= 0 || worldBossFinishMinute >= 0) return CommandResult.Rejected("event.defense_already_active");
            int finish = checked(now + cfg.EventSettings.MonsterSiegeDurationMinutes);
            if (!trainers.Any(t => t.Rank >= cfg.EventSettings.MonsterSiegeMinimumRank && t.Roster.Members.Count > 0)) return CommandResult.Rejected("event.no_defenders");
            monsterSiegeStartMinute = now;
            monsterSiegeFinishMinute = finish;
            queue.Schedule(finish, SimEventKind.MonsterSiegeResolve);
            CallTrainersHomeForDefense(cfg.EventSettings.MonsterSiegeMinimumRank);
            Raise(new HubEventChanged(now, HubEventKind.MonsterSiege, "Started", 0, trainers.Count));
            return CommandResult.Success();
        }

        public CommandResult ActivateWorldBossRaid()
        {
            if (monsterSiegeFinishMinute >= 0 || worldBossFinishMinute >= 0) return CommandResult.Rejected("event.defense_already_active");
            if (now < nextWorldBossMinute) return CommandResult.Rejected("event.cooldown");
            if (!trainers.Any(t => t.Rank >= cfg.EventSettings.WorldBossMinimumRank && t.Roster.Members.Count > 0))
                return CommandResult.Rejected("event.no_eligible_trainer");
            if (cfg.EventSettings.WorldBossActivationGold > treasury.Balance) return CommandResult.Rejected("event.insufficient_gold");
            int finish;
            int cooldownFinish;
            try
            {
                finish = checked(now + cfg.EventSettings.WorldBossDurationMinutes);
                cooldownFinish = checked(now + cfg.EventSettings.WorldBossCooldownMinutes);
            }
            catch (OverflowException) { return CommandResult.Rejected("event.time_overflow"); }
            if (!treasury.TrySpend(cfg.EventSettings.WorldBossActivationGold)) return CommandResult.Rejected("event.insufficient_gold");
            if (cfg.EventSettings.WorldBossActivationGold > 0)
                Raise(new TreasuryChanged(now, -cfg.EventSettings.WorldBossActivationGold, treasury.Balance, "WorldBossActivation"));
            worldBossStartMinute = now;
            worldBossFinishMinute = finish;
            nextWorldBossMinute = cooldownFinish;
            queue.Schedule(finish, SimEventKind.WorldBossResolve);
            CallTrainersHomeForDefense(cfg.EventSettings.WorldBossMinimumRank);
            Raise(new HubEventChanged(now, HubEventKind.WorldBossRaid, "Started", -cfg.EventSettings.WorldBossActivationGold,
                trainers.Count(t => t.Rank >= cfg.EventSettings.WorldBossMinimumRank)));
            return CommandResult.Success();
        }

        void CallTrainersHomeForDefense(int minimumRank)
        {
            foreach (var trainer in trainers.Where(t => t.Rank >= minimumRank && t.Roster.Members.Count > 0).OrderBy(t => t.Id))
            {
                defenseCalledTrainerIds.Add(trainer.Id);
                if (trainer.State == TrainerState.Traveling || trainer.State == TrainerState.Farming || trainer.State == TrainerState.Returning)
                    SendHome(trainer, ReturnReason.EventCall);
                else if (trainer.State == TrainerState.AtHub)
                {
                    trainer.Token++;
                    SetState(trainer, TrainerState.AtHub, ReturnReason.EventCall.ToString());
                    queue.Schedule(Math.Max(monsterSiegeFinishMinute, worldBossFinishMinute), SimEventKind.TrainerDecide,
                        trainer.Id, trainer.Token);
                }
            }
        }

        void OnMonsterSiegeResolve()
        {
            if (monsterSiegeFinishMinute < 0 || now < monsterSiegeFinishMinute) return;
            int eligible = trainers.Count(t => defenseCalledTrainerIds.Contains(t.Id) && t.State == TrainerState.AtHub && t.Roster.Members.Count > 0);
            var winners = ResolveDefenseBattles(HubEventKind.MonsterSiege, cfg.EventSettings.MonsterSiegeMinimumRank, cfg.EventSettings.MonsterSiegeEnemyHp,
                cfg.EventSettings.MonsterSiegeEnemyAttack, cfg.EventSettings.MonsterSiegeEnemyDefense,
                cfg.EventSettings.MonsterSiegeDurationMinutes);
            bool victory = eligible > 0 && winners.Count / (double)eligible >= cfg.EventSettings.MonsterSiegeMinimumWinFraction;
            if (victory)
            {
                long reward = cfg.EventSettings.MonsterSiegeVictoryGold;
                var bossCore = new ProductId("boss_core");
                foreach (var trainer in trainers.Where(t => winners.Contains(t.Id)).OrderBy(t => t.Id))
                {
                    ReceiveTrainerIncome(trainer, reward, "MonsterSiegeVictory");
                    if (trainer.Inventory.CanAdd(bossCore, cfg.EventSettings.MonsterSiegeBossCoreCount))
                    {
                        trainer.Inventory.Add(bossCore, cfg.EventSettings.MonsterSiegeBossCoreCount);
                        Raise(new TrainerProductChanged(now, trainer.Id, bossCore.Value, cfg.EventSettings.MonsterSiegeBossCoreCount, trainer.Inventory.Count(bossCore)));
                    }
                }
            }
            else if (rng.NextDouble() < cfg.EventSettings.MonsterSiegeBuildingDamageProbability) DamageRandomServiceBuilding(HubEventKind.MonsterSiege);
            monsterSiegeFinishMinute = -1;
            monsterSiegeStartMinute = -1;
            Raise(new HubEventChanged(now, HubEventKind.MonsterSiege, victory ? "Victory" : "Defeat", 0, eligible));
            ResumeEventCalledTrainers();
        }

        void OnWorldBossResolve()
        {
            if (worldBossFinishMinute < 0 || now < worldBossFinishMinute) return;
            int eligible = trainers.Count(t => defenseCalledTrainerIds.Contains(t.Id) && t.State == TrainerState.AtHub && t.Roster.Members.Count > 0);
            var winners = ResolveDefenseBattles(HubEventKind.WorldBossRaid, cfg.EventSettings.WorldBossMinimumRank,
                cfg.EventSettings.WorldBossHp, cfg.EventSettings.WorldBossAttack, cfg.EventSettings.WorldBossDefense,
                cfg.EventSettings.WorldBossDurationMinutes);
            bool victory = eligible > 0 && winners.Count == eligible;
            if (victory)
            {
                var crystal = new ProductId("world_boss_crystal");
                foreach (var trainer in trainers.Where(t => winners.Contains(t.Id)).OrderBy(t => t.Id))
                    if (trainer.Inventory.CanAdd(crystal, cfg.EventSettings.WorldBossCrystalCount))
                    {
                        trainer.Inventory.Add(crystal, cfg.EventSettings.WorldBossCrystalCount);
                        Raise(new TrainerProductChanged(now, trainer.Id, crystal.Value, cfg.EventSettings.WorldBossCrystalCount,
                            trainer.Inventory.Count(crystal)));
                    }
            }
            else if (rng.NextDouble() < cfg.EventSettings.WorldBossBuildingDamageProbability) DamageRandomServiceBuilding(HubEventKind.WorldBossRaid);
            worldBossFinishMinute = -1;
            worldBossStartMinute = -1;
            Raise(new HubEventChanged(now, HubEventKind.WorldBossRaid, victory ? "Victory" : "Defeat", 0, eligible));
            ResumeEventCalledTrainers();
        }

        IReadOnlyCollection<int> ResolveDefenseBattles(HubEventKind kind, int minimumRank, int hp, double attack, double defense, int durationMinutes)
        {
            var winners = new List<int>();
            foreach (var trainer in trainers.Where(t => defenseCalledTrainerIds.Contains(t.Id) && t.Rank >= minimumRank
                && t.State == TrainerState.AtHub && t.Roster.Members.Count > 0).OrderBy(t => t.Id))
            {
                var snapshot = TrainerSnapshot.FromTrainer(trainer, now);
                var enemy = new MonsterSnapshot(new MonsterId($"{kind}.foe.{trainer.Id}"), MonsterElement.Dark,
                new MonsterStats(hp, attack, defense,
                    kind == HubEventKind.MonsterSiege ? cfg.EventSettings.MonsterSiegeEnemyAttackSpeed : cfg.EventSettings.WorldBossAttackSpeed,
                    0), hp, new[] { "dark_strike" });
                var management = snapshot.Team.ToDictionary(m => m.Id,
                    m => Math.Max(0, RebellionModel.ManagementScore(m.Level, m.Rarity)));
                var leadership = new TrainerCombatContext(trainer.Rank, trainer.Level, trainer.Rarity,
                    itemLeadershipBonus: snapshot.LeadershipBonus);
                var battle = BattleResolver.Resolve(new BattleInput(snapshot.Team, new[] { enemy }, snapshot.ActiveMonsterId,
                    leadership, management), CombatConfig.Prototype, rng);
                foreach (var state in battle.FinalMonsters.Where(s => s.Side == BattleSide.Team))
                {
                    var monster = trainer.Roster.Members.FirstOrDefault(m => m.Id == state.Id);
                    if (monster != null) monster.SetCurrentHp(state.CurrentHp);
                }
                if (battle.ActiveId.HasValue && trainer.Roster.Members.Any(m => m.Id == battle.ActiveId.Value && m.CurrentHp > 0))
                    trainer.Roster.SetActive(battle.ActiveId.Value);
                ApplyGearWear(trainer, new ExpeditionResult(new[] { battle },
                    new ExpeditionLoot(Array.Empty<MaterialQuantity>(), Array.Empty<MaterialQuantity>(), 0, 0)),
                    durationMinutes);
                bool victory = battle.Outcome == BattleOutcome.TeamWon;
                if (victory) winners.Add(trainer.Id);
                Raise(new HubEventChanged(now, kind, victory ? "TrainerVictory" : "TrainerDefeat", 0, trainer.Id));
            }
            return winners.AsReadOnly();
        }

        void ResumeEventCalledTrainers()
        {
            foreach (var trainer in trainers.Where(t => defenseCalledTrainerIds.Contains(t.Id) && t.State == TrainerState.AtHub).OrderBy(t => t.Id).ToArray())
            {
                trainer.Token++;
                if (trainer.BackpackUnits > 0 && useSupplyChain && !SellBackpack(trainer))
                {
                    trainer.MarketWaitSinceMinute = now;
                    SetState(trainer, TrainerState.WaitingForMarket, "MerchantRoute");
                    ScheduleOrEndMarketWait(trainer);
                }
                else OnDecide(trainer);
            }
            // Calls are scoped to one event. Trainers still returning or in another
            // service state resume through their existing event/state transitions.
            defenseCalledTrainerIds.Clear();
        }

        void DamageRandomServiceBuilding(HubEventKind cause)
        {
            var serviceCandidates = buildings.Where(b => !b.Damaged).OrderBy(b => b.Kind).ToArray();
            if (serviceCandidates.Length > 0)
            {
                var building = serviceCandidates[rng.NextInt(serviceCandidates.Length)];
                building.Damaged = true;
                string id = ServiceFacilityId(building.Kind);
                if (facilityStates.TryGetValue(id, out var state)) { state.Damaged = true; ApplyFacilityProductionState(id, state); }
                UpdateReputation();
                Raise(new HubEventChanged(now, cause, "BuildingDamaged:" + building.Kind, 0, 1));
                return;
            }
            var candidates = HubFacilityCatalog.Definitions.Where(d => d.Id != "town_hall" && d.Id != "dormitory" &&
                !ServiceBuildingForFacility(d.Id).HasValue && townHallLevel >= d.TownHallUnlockLevel &&
                (d.RequiredZone <= 1 || unlockedZoneIds.Contains("zone_" + d.RequiredZone)) &&
                facilityStates.TryGetValue(d.Id, out var state) && state.Level > 0 && !state.Damaged).ToArray();
            if (candidates.Length == 0) return;
            var definition = candidates[rng.NextInt(candidates.Length)];
            facilityStates[definition.Id].Damaged = true;
            ApplyFacilityProductionState(definition.Id, facilityStates[definition.Id]);
            UpdateReputation();
            Raise(new HubEventChanged(now, cause, "BuildingDamaged:" + definition.Id, 0, 1));
        }

        public CommandResult RepairBuilding(BuildingKind kind)
        {
            if ((int)kind < 0 || (int)kind >= buildings.Length) return CommandResult.Rejected("building.unknown");
            var building = buildings[(int)kind];
            if (!building.Damaged) return CommandResult.Rejected("building.not_damaged");
            if (buildingRepairFinishMinutes[(int)kind] >= 0) return CommandResult.Rejected("building.repair_in_progress");
            if (station == null) return CommandResult.Rejected("building.material_stock_unavailable");
            var settings = cfg.HubProgressionSettings;
            int wood, stone, iron;
            long gold;
            int finish;
            try
            {
                wood = checked(settings.WoodIngotsPerLevel * building.Level);
                stone = checked(settings.StoneIngotsPerLevel * building.Level);
                iron = checked(settings.IronIngotsPerLevel * building.Level);
                gold = checked(settings.RepairGoldPerBuildingLevel * building.Level);
                finish = checked(now + settings.RepairMinutes);
            }
            catch (OverflowException) { return CommandResult.Rejected("building.cost_overflow"); }
            var needs = new[] { (Id: "wood_ingot", Units: wood), (Id: "stone_ingot", Units: stone), (Id: "iron_ingot", Units: iron) };
            if (needs.Any(x => station.Stock.Get(new InventoryItem(new ProductId(x.Id))).Available < x.Units))
                return CommandResult.Rejected("building.insufficient_materials");
            if (gold > treasury.Balance || !treasury.TrySpend(gold)) return CommandResult.Rejected("building.insufficient_gold");
            if (gold > 0) Raise(new TreasuryChanged(now, -gold, treasury.Balance, "BuildingRepair"));
            foreach (var item in needs.Where(x => x.Units > 0))
            {
                var product = new ProductId(item.Id);
                station.Stock.Remove(new InventoryItem(product), item.Units);
                Raise(new SupplyStockChanged(now, "product:" + item.Id, station.Stock.Get(new InventoryItem(product))));
            }
            buildingRepairFinishMinutes[(int)kind] = finish;
            queue.Schedule(finish, SimEventKind.BuildingRepairComplete, arg: (int)kind);
            Raise(new BuildingRepairScheduled(now, kind, gold, wood, stone, iron, finish));
            return CommandResult.Success();
        }

        void OnBuildingRepairComplete(BuildingKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= buildings.Length || buildingRepairFinishMinutes[index] < 0 || now < buildingRepairFinishMinutes[index]) return;
            buildings[index].Damaged = false;
            string facilityId = ServiceFacilityId(kind);
            if (facilityStates.TryGetValue(facilityId, out var state)) { state.Damaged = false; ApplyFacilityProductionState(facilityId, state); }
            buildingRepairFinishMinutes[index] = -1;
            UpdateReputation();
            SeatWaiting(buildings[index]);
            Raise(new BuildingRepairCompleted(now, kind));
        }

        static string ServiceFacilityId(BuildingKind kind) => kind == BuildingKind.Inn ? "inn"
            : kind == BuildingKind.Restaurant ? "restaurant" : kind == BuildingKind.Bar ? "bar" : "veterinary_hospital";

        MonsterIvGrade RollBreedingSeasonIv()
        {
            double ordinary = cfg.EventSettings.BreedingSeasonOrdinaryIvWeight;
            double rare = ordinary * cfg.EventSettings.BreedingSeasonRareIvWeightMultiplier;
            double total = 5 * ordinary + 2 * rare;
            double roll = rng.NextDouble() * total;
            for (int grade = 0; grade < 7; grade++)
            {
                bool isRare = grade == (int)MonsterIvGrade.S || grade == (int)MonsterIvGrade.SS;
                double weight = isRare ? rare : ordinary;
                if (roll < weight) return (MonsterIvGrade)grade;
                roll -= weight;
            }
            return MonsterIvGrade.SS;
        }

        IEnumerable<Monster> AllMonsters() => trainers.SelectMany(t => t.Roster.Members.Concat(t.Roster.Storage))
            .Concat(geneBank.StoredMonsters).Concat(geneBank.ConfiscatedMonsters)
            .Concat(veterinaryHospital.Recoveries.Select(r => veterinaryHospital.RecoveringMonster(r.RecoveryId)))
            .Distinct();

        void ApplyMonsterFluDailyLoss()
        {
            if (monsterFluFinishMinute < 0 || now >= monsterFluFinishMinute) return;
            stockExchange.RecordRevenue(BuildingKind.Hospital.ToString(), cfg.EventSettings.MonsterFluHospitalRevenueBonus);
            Raise(new HubEventChanged(now, HubEventKind.MonsterFlu, "HospitalDemand", cfg.EventSettings.MonsterFluHospitalRevenueBonus, 1));
            int changed = 0;
            foreach (var monster in AllMonsters())
            {
                long loss = (long)Math.Ceiling(monster.MaxHp * cfg.EventSettings.MonsterFluDailyHpLossFraction);
                long next = Math.Max(0, monster.CurrentHp - loss);
                if (next != monster.CurrentHp) { monster.SetCurrentHp(next); changed++; }
            }
            if (changed > 0) Raise(new HubEventChanged(now, HubEventKind.MonsterFlu, "DailyDamage", 0, changed));
        }

        void OnMonsterFluEnd()
        {
            if (monsterFluFinishMinute < 0 || now < monsterFluFinishMinute) return;
            monsterFluFinishMinute = -1;
            monsterFluStartMinute = -1;
            Raise(new HubEventChanged(now, HubEventKind.MonsterFlu, "Ended", 0, 0));
        }

        void ScheduleBlackFridayStart(int paydayMinute)
        {
            int start = checked(paydayMinute - cfg.EventSettings.BlackFridayDays * SimClock.MinutesPerDay);
            if (start >= now) queue.Schedule(start, SimEventKind.BlackFridayStart);
            else if (HubEventCalendar.IsBlackFriday(now, paydayMinute, cfg.EventSettings))
                queue.Schedule(now, SimEventKind.BlackFridayStart);
        }

        void OnBlackFridayStart()
        {
            int payday = SimClock.PaydayMinute(paydayIndex);
            if (!HubEventCalendar.IsBlackFriday(now, payday, cfg.EventSettings)) return;
            int eventStart = payday - cfg.EventSettings.BlackFridayDays * SimClock.MinutesPerDay;
            if (now == eventStart) Raise(new HubEventChanged(now, HubEventKind.BlackFriday, "Started", 0, trainers.Count));
            if (useSupplyChain && cfg.EventSettings.BlackFridayImpulseUnits > 0)
            {
                string[] impulseGoods = { "protection_charm", "overclock_coffee", "capture_ball", "trap", "liquor" };
            int purchases = 0;
            foreach (var trainer in trainers.OrderBy(x => x.Id))
            {
                if (trainer.State != TrainerState.AtHub) continue;
                foreach (string product in impulseGoods)
                {
                    if (station.Stock.Get(new InventoryItem(new ProductId(product))).Available <= 0) continue;
                    var purchased = PurchaseProduct(trainer.Id, product, cfg.EventSettings.BlackFridayImpulseUnits);
                    if (purchased.Ok) purchases++;
                }
                foreach (var slot in GearCat.Slots)
                {
                    for (int unit = 0; unit < cfg.EventSettings.BlackFridayImpulseUnits; unit++)
                    {
                        var gear = new GearItem(
                            $"black_friday:{paydayIndex}:{now}:{trainer.Id}:{slot.Id}:{unit}",
                            slot, 1, GearCat.MaxDurabilityFor(slot.Group));
                        var current = slot.Group == GearGroup.MonsterCombat
                            ? trainer.Roster.Members.Select(m => m.Gear.Get(slot.Id)).FirstOrDefault(x => x != null)
                            : trainer.Gear.Get(slot.Id);
                        var purchased = current != null && current.Stars < 5
                            ? OfferGearForSacrifice(trainer.Id, gear, cfg.EventSettings.BlackFridayJunkGearPrice)
                            : OfferGear(trainer.Id, gear, cfg.EventSettings.BlackFridayJunkGearPrice);
                        if (purchased.Ok) purchases++;
                    }
                }
            }
                Raise(new HubEventChanged(now, HubEventKind.BlackFriday, "ImpulsePurchases", 0, purchases));
            }
            int nextDay = checked(now + SimClock.MinutesPerDay);
            if (nextDay < payday) queue.Schedule(nextDay, SimEventKind.BlackFridayStart);
        }

        void CheckLaborInspectionTrigger()
        {
            if (inspectionFinishMinute >= 0 || now < nextInspectionMinute) return;
            foreach (var trainer in trainers) Settle(trainer);
            bool overTax = marketTaxRate > cfg.EventSettings.InspectionTaxThreshold;
            bool redStress = trainers.Any(t => t.Needs.Stress >= cfg.EventSettings.InspectionStressThreshold);
            if (!overTax && !redStress) return;
            inspectionBribed = false;
            inspectionFinishMinute = checked(now + cfg.EventSettings.InspectionResolutionMinutes);
            queue.Schedule(inspectionFinishMinute, SimEventKind.LaborInspectionResolve);
            Raise(new HubEventChanged(now, HubEventKind.LaborInspection, "Started", 0, trainers.Count(t => t.Needs.Stress >= cfg.EventSettings.InspectionStressThreshold)));
        }

        void OnLaborInspectionResolve()
        {
            if (inspectionFinishMinute < 0 || now < inspectionFinishMinute) return;
            long penalty = 0;
            bool triggerRemains = marketTaxRate > cfg.EventSettings.InspectionTaxThreshold
                || trainers.Any(t => t.Needs.Stress >= cfg.EventSettings.InspectionStressThreshold);
            if (!inspectionBribed && triggerRemains && treasury.Balance > 0)
            {
                penalty = Math.Min(treasury.Balance,
                    (long)Math.Ceiling(treasury.Balance * cfg.EventSettings.InspectionFineFraction));
                if (treasury.TrySpend(penalty)) Raise(new TreasuryChanged(now, -penalty, treasury.Balance, "LaborInspectionFine"));
                else penalty = 0;
            }
            inspectionFinishMinute = -1;
            nextInspectionMinute = checked(now + cfg.EventSettings.InspectionCooldownMinutes);
            Raise(new HubEventChanged(now, HubEventKind.LaborInspection,
                inspectionBribed ? "Bribed" : triggerRemains ? "FineAssessed" : "TaxTheatre", -penalty, trainers.Count));
            inspectionBribed = false;
        }

        public CommandResult BribeLaborInspector(long gold)
        {
            if (inspectionFinishMinute < 0) return CommandResult.Rejected("inspection.not_active");
            if (gold <= 0) return CommandResult.Rejected("inspection.invalid_bribe");
            if (gold > treasury.Balance || !treasury.TrySpend(gold)) return CommandResult.Rejected("building.insufficient_gold");
            Raise(new TreasuryChanged(now, -gold, treasury.Balance, "LaborInspectionBribe"));
            inspectionBribed = true;
            inspectionFinishMinute = now;
            OnLaborInspectionResolve();
            return CommandResult.Success();
        }
    }
}
