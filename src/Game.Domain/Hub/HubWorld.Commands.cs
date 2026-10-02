using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Monsters;
using Game.Domain.Materials;
using Game.Domain.Combat;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>
        /// Giám đốc donate Gold cho một Trainer (không hoàn lại). Lỗi của người chơi trả về Rejected.
        /// Nếu Trainer đang kẹt vì hết tiền thì được xử lý lại ngay.
        /// </summary>
        public CommandResult Donate(int trainerId, long gold)
        {
            CommandResult check = CheckTransfer(trainerId, gold);
            if (!check.Ok) return check;

            Trainer t = trainers[trainerId];
            treasury.TrySpend(gold);
            t.Gold += gold;
            Raise(new TreasuryChanged(now, -gold, treasury.Balance, "Donate"));
            Raise(new DonationReceived(now, trainerId, gold, "Director"));
            WakeIfWaitingForMoney(t);
            return CommandResult.Success();
        }

        /// <summary>Ứng lương: Giám đốc đưa Gold trước, khoản này được trừ vào lương Payday kế tiếp.</summary>
        public CommandResult AdvanceWage(int trainerId, long gold)
        {
            CommandResult check = CheckTransfer(trainerId, gold);
            if (!check.Ok) return check;

            Trainer t = trainers[trainerId];
            treasury.TrySpend(gold);
            t.Gold += gold;
            t.WageAdvance += gold;
            Raise(new TreasuryChanged(now, -gold, treasury.Balance, "AdvanceWage"));
            WakeIfWaitingForMoney(t);
            return CommandResult.Success();
        }

        /// <summary>Đặt giá bán của một công trình dịch vụ (Bệnh Viện: giá trên mỗi 10 HP). Giá phải &gt; 0.</summary>
        public CommandResult SetPrice(BuildingKind building, long price)
        {
            if (price <= 0) return CommandResult.Rejected("Giá phải lớn hơn 0.");
            buildings[(int)building].Price = price;
            return CommandResult.Success();
        }

        CommandResult CheckTransfer(int trainerId, long gold)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return CommandResult.Rejected("Trainer không tồn tại.");
            if (gold <= 0) return CommandResult.Rejected("Số Gold phải lớn hơn 0.");
            if (gold > treasury.Balance) return CommandResult.Rejected("Kho bạc không đủ Gold.");
            return CommandResult.Success();
        }

        /// <summary>
        /// Trainer đang kẹt vì hết tiền được xử lý lại ngay sau khi nhận tiền (hủy sự kiện chờ cũ bằng token).
        /// Nếu số tiền nhận được vẫn chưa đủ thì giữ nguyên lượt chờ (không đặt lại đồng hồ 24 giờ).
        /// </summary>
        void WakeIfWaitingForMoney(Trainer t)
        {
            if (t.State != TrainerState.WaitingForMoney) return;
            long needed = PriceForTrainer(t, buildings[(int)t.PendingService]);
            if (t.Gold < needed) return;
            Settle(t);
            LeaveWait(t);
            t.Token++;
            SetState(t, TrainerState.AtHub, "Funded");
            OnDecide(t);
        }

        public CommandResult SwapActiveMonster(int trainerId, string monsterId)
            => ChangeRoster(trainerId, monsterId, "Swap", (t, id) => t.Roster.SetActive(id));
        public CommandResult StoreMonster(int trainerId, string monsterId)
            => ChangeRoster(trainerId, monsterId, "Store", (t, id) => t.Roster.MoveToStorage(id));
        public CommandResult WithdrawMonster(int trainerId, string monsterId)
            => ChangeRoster(trainerId, monsterId, "Withdraw", (t, id) => t.Roster.RestoreFromStorage(id));
        public CommandResult DepositMonster(int trainerId, string monsterId)
        {
            if (!ValidMonsterInput(trainerId, monsterId) || !StoreMonsterInGeneBank(trainerId, new MonsterId(monsterId)))
                return CommandResult.Rejected("Monster cannot be deposited.");
            return CommandResult.Success();
        }
        public CommandResult WithdrawBankMonster(int trainerId, string monsterId)
        {
            if (!ValidMonsterInput(trainerId, monsterId) || !WithdrawMonsterFromGeneBank(trainerId, new MonsterId(monsterId)))
                return CommandResult.Rejected("Monster cannot be withdrawn from Gene Bank.");
            return CommandResult.Success();
        }
        bool ValidMonsterInput(int trainerId, string monsterId)
            => trainerId >= 0 && trainerId < trainers.Count && !string.IsNullOrWhiteSpace(monsterId);
        CommandResult ChangeRoster(int trainerId, string monsterId, string transition, Action<Trainer, MonsterId> change)
        {
            if (!ValidMonsterInput(trainerId, monsterId)) return CommandResult.Rejected("Invalid Trainer or Monster ID.");
            var t = trainers[trainerId];
            if (transition == "Store" && t.Roster.Storage.Count >= cfg.MonsterStorageSettings.Capacity)
                return CommandResult.Rejected("Local Monster storage is full.");
            var monster = t.Roster.Members.Concat(t.Roster.Storage).FirstOrDefault(m => m.Id.Value == monsterId);
            if (monster == null || monster.Custody != MonsterCustody.Trainer) return CommandResult.Rejected("Monster is unavailable.");
            try { change(t, monster.Id); }
            catch (InvalidOperationException ex) { return CommandResult.Rejected(ex.Message); }
            Raise(new MonsterRosterChanged(now, trainerId, monsterId, transition, t.Roster.Active?.Id.Value));
            return CommandResult.Success();
        }
        GeneticLab Lab(Trainer t) => new GeneticLab(t, geneBank, cfg.GeneticLabSettings,
            cfg.RarityUpgradeSettings, cfg.EvolutionCatalogSettings);

        public CommandResult AppraiseMonster(int trainerId, string monsterId)
        {
            if (!ValidMonsterInput(trainerId, monsterId)) return CommandResult.Rejected("Invalid Trainer or Monster ID.");
            var available = trainers[trainerId].Roster.Members.Concat(trainers[trainerId].Roster.Storage)
                .FirstOrDefault(m => m.Id.Value == monsterId);
            if (available != null && available.Custody != MonsterCustody.Trainer)
                return CommandResult.Rejected("Monster is recovering.");
            long price = (cfg.GeneticLabSettings ?? GeneticLabConfig.Prototype).AppraisalPrice;
            if (treasury.Balance > long.MaxValue - price) return CommandResult.Rejected("Treasury limit reached.");
            var result = Lab(trainers[trainerId]).Appraise(new MonsterId(monsterId));
            if (!result.Applied) return CommandResult.Rejected("Monster cannot be appraised or Trainer cannot pay.");
            AddTreasury(result.PricePaid, "MonsterAppraisal");
            Raise(new MonsterAppraised(now, trainerId, monsterId, result.RevealedIv.Value, result.PricePaid));
            return CommandResult.Success();
        }
        public CommandResult DismantleMonster(int trainerId, string monsterId)
        {
            if (!ValidMonsterInput(trainerId, monsterId)) return CommandResult.Rejected("Invalid Trainer or Monster ID.");
            var trainer = trainers[trainerId];
            var result = Lab(trainer).Dismantle(new MonsterId(monsterId));
            if (!result.Applied) return CommandResult.Rejected("Monster cannot be dismantled.");
            if (result.GeneFragments > 0) Raise(new TrainerProductChanged(now, trainerId, "gene_fragment", result.GeneFragments,
                trainer.Inventory.Count(new ProductId("gene_fragment"))));
            Raise(new MonsterDismantled(now, trainerId, monsterId, result.GeneFragments));
            return CommandResult.Success();
        }
        public CommandResult UpgradeMonsterRarity(int trainerId, string monsterId, string protectionCharmId = null)
        {
            if (!ValidMonsterInput(trainerId, monsterId)) return CommandResult.Rejected("Invalid Trainer or Monster ID.");
            var trainer = trainers[trainerId];
            var before = trainer.Inventory.Products;
            var result = Lab(trainer).UpgradeRarity(new MonsterId(monsterId),
                string.IsNullOrWhiteSpace(protectionCharmId) ? default : new ProductId(protectionCharmId), rng);
            if (!result.Eligible) return CommandResult.Rejected("Monster is not eligible or costs are unavailable.");
            EmitProductChanges(trainer, before);
            Raise(new MonsterUpgradeResolved(now, trainerId, monsterId, result.Success, result.SuccessChance,
                result.Roll, result.ProtectionApplied, result.ResultRarity.Value));
            return CommandResult.Success();
        }
        public CommandResult EvolveMonster(int trainerId, string monsterId, string branchId, string protectionCharmId = null)
        {
            if (!ValidMonsterInput(trainerId, monsterId) || string.IsNullOrWhiteSpace(branchId))
                return CommandResult.Rejected("Invalid Trainer, Monster or evolution branch.");
            var trainer = trainers[trainerId];
            var before = trainer.Inventory.Products;
            var result = Lab(trainer).Evolve(new MonsterId(monsterId), branchId,
                string.IsNullOrWhiteSpace(protectionCharmId) ? default : new ProductId(protectionCharmId), rng);
            if (!result.Eligible) return CommandResult.Rejected("Monster is not eligible or costs are unavailable.");
            EmitProductChanges(trainer, before);
            Raise(new MonsterEvolutionResolved(now, trainerId, monsterId, branchId, result.Success, result.SuccessChance,
                result.Roll, result.ProtectionApplied, result.ResultSpeciesId));
            return CommandResult.Success();
        }
        void EmitProductChanges(Trainer trainer, IReadOnlyDictionary<ProductId, int> before)
        {
            foreach (var id in before.Keys.Concat(trainer.Inventory.Products.Keys).Distinct().OrderBy(p => p.Value, StringComparer.Ordinal))
            {
                int prior = before.TryGetValue(id, out int count) ? count : 0;
                int current = trainer.Inventory.Count(id);
                if (current != prior) Raise(new TrainerProductChanged(now, trainer.Id, id.Value, current - prior, current));
            }
        }
        /// <summary>Resolve an explicit weakened wild target through the seeded Hub RNG and inventory.</summary>
        public CommandResult AttemptCapture(int trainerId, Monster target)
        {
            if (trainerId < 0 || trainerId >= trainers.Count || target == null ||
                target.Custody != MonsterCustody.Unassigned || target.Owner != null || geneBank.Contains(target.Id) ||
                veterinaryHospital.Contains(target.Id) || trainers.Any(t => t.Roster.Members.Concat(t.Roster.Storage).Any(m => m.Id == target.Id)))
                return CommandResult.Rejected("Invalid Trainer or already owned capture target.");
            if (!HasMonsterSlot(trainers[trainerId])) return CommandResult.Rejected("Monster ownership capacity is full.");
            if (veterinaryHospital.OccupiedRecoveryBeds >= veterinaryHospital.RecoveryBedCapacity)
                return CommandResult.Rejected("No capture recovery bed is available.");
            var trainer = trainers[trainerId];
            var config = CaptureConfig.Prototype;
            bool eligible = CaptureResolver.ShouldAttempt(target.Definition, target.Rarity, target.Level,
                target.CurrentHp, target.MaxHp, trainer.Roster, trainer.Attributes, trainer.Inventory, config);
            if (!eligible) return CommandResult.Rejected("Capture target is not weak enough or cannot improve the roster.");
            var ball = new ProductId("capture_ball"); var trap = new ProductId("trap");
            var inputs = new CaptureInputs(target.SpeciesId, target.Rarity, target.Level, trainer.Inventory.Count(ball),
                trainer.Inventory.Count(trap), eligible, trainer.Attributes.Dexterity, trainer.Class);
            var result = CaptureResolver.Resolve(MonsterSnapshot.FromMonster(target, target.CombatSkillIds), inputs, config, rng);
            foreach (var cost in new[] { (product: ball, count: result.ConsumedBallCount), (product: trap, count: result.ConsumedTrapCount) })
                if (cost.count > 0)
                {
                    trainer.Inventory.TryConsume(cost.product, cost.count);
                    Raise(new TrainerProductChanged(now, trainerId, cost.product.Value, -cost.count, trainer.Inventory.Count(cost.product)));
                }
            Raise(new MonsterCaptureResolved(now, trainerId, target.Id.Value, result.Success, result.Chance, result.Roll,
                result.ConsumedBallCount, result.ConsumedTrapCount));
            if (result.Success)
            {
                var generated = result.Generation;
                var captured = Monster.Create(target.Id, target.Definition, generated.Rarity, generated.Iv,
                    generated.Level, generated.Seed, statConfig: cfg.MonsterStatSettings);
                var admitted = AdmitCapturedMonster(trainerId, captured);
                if (!admitted.Accepted) throw new InvalidOperationException("Validated capture admission failed.");
            }
            return CommandResult.Success();
        }

        /// <summary>Explicit progression input; rank eligibility is still enforced by ZoneSelector.</summary>
        public CommandResult UnlockZone(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId) || !cfg.ZoneCatalogSettings.Definitions.Any(z => z.Id == zoneId))
                return CommandResult.Rejected("Unknown Zone.");
            if (!unlockedZoneIds.Add(zoneId)) return CommandResult.Rejected("Zone is already unlocked.");
            Raise(new ZoneUnlocked(now, zoneId));
            return CommandResult.Success();
        }

        /// <summary>
        /// Kiểm tra các bất biến của mô phỏng, ném InvalidOperationException nếu vi phạm:
        /// Kho bạc không âm; thanh nhu cầu trong 0-100; mỗi Trainer không ở hai chỗ cùng lúc;
        /// Trainer đang xếp hàng (<see cref="TrainerState.Queued"/>) không có sự kiện cá nhân nào còn hiệu lực,
        /// mọi Trainer khác có đúng một; mỗi Trainer xếp hàng nằm trong đúng một hàng đợi dịch vụ.
        /// </summary>
        public void ValidateInvariants()
        {
            if (merchantFleet != null && merchantFleet.Current.Cash < 0) throw new InvalidOperationException("Merchant cash is negative.");
            if (supplyLedger != null && supplyLedger.TotalBalance != 0) throw new InvalidOperationException("Supply money ledger does not balance.");
            if (treasury.Balance < 0) throw new InvalidOperationException("Kho bạc âm.");

            ValidateMonsterInvariants();

            var seated = new HashSet<int>();
            var waitingIds = new HashSet<int>();
            foreach (ServiceBuilding b in buildings)
            {
                foreach (int id in b.Occupants)
                    if (!seated.Add(id)) throw new InvalidOperationException($"Trainer {id} đang ở hai chỗ cùng lúc.");
                foreach (int id in b.Waiting)
                    if (!waitingIds.Add(id)) throw new InvalidOperationException($"Trainer {id} đang xếp hàng ở nhiều chỗ.");
            }
            foreach (int id in waitingIds)
                if (seated.Contains(id)) throw new InvalidOperationException($"Trainer {id} vừa xếp hàng vừa đang được phục vụ.");

            foreach (Trainer t in trainers)
            {
                Needs n = t.Needs;
                if (n.Stamina < 0 || n.Stamina > 100 || n.Satiety < 0 || n.Satiety > 100
                    || n.Hydration < 0 || n.Hydration > 100 || n.Stress < 0 || n.Stress > 100)
                    throw new InvalidOperationException($"Trainer {t.Id} có thanh nhu cầu ngoài khoảng 0-100.");
            }

            var pending = new int[trainers.Count];
            foreach (SimEvent e in queue.Snapshot)
                if (e.TrainerId >= 0 && trainers[e.TrainerId].Token == e.Token) pending[e.TrainerId]++;
            for (int i = 0; i < pending.Length; i++)
            {
                bool queued = trainers[i].State == TrainerState.Queued;
                int expected = queued ? 0 : 1;   // người xếp hàng chờ SeatWaiting gọi; mọi trạng thái khác luôn có đúng một sự kiện kế tiếp
                if (pending[i] != expected)
                    throw new InvalidOperationException($"Trainer {i} ({trainers[i].State}) có {pending[i]} sự kiện đang chờ, cần đúng {expected}.");
                if (queued != waitingIds.Contains(i))
                    throw new InvalidOperationException($"Trainer {i} ({trainers[i].State}) không khớp với hàng đợi dịch vụ.");
            }
        }
        void ValidateMonsterInvariants()
        {
            var identities = new HashSet<MonsterId>();
            void Check(Monster monster, MonsterCustody custody)
            {
                if (!identities.Add(monster.Id)) throw new InvalidOperationException("Duplicate Monster ownership: " + monster.Id);
                if (string.IsNullOrWhiteSpace(monster.Id.Value) || monster.Level < 1 || monster.Level > 100 ||
                    monster.Custody != custody || monster.CurrentHp < 0 || monster.CurrentHp > monster.MaxHp || monster.MaxHp <= 0)
                    throw new InvalidOperationException("Invalid Monster HP/custody: " + monster.Id);
            }
            var recoveries = veterinaryHospital.Recoveries;
            foreach (var trainer in trainers)
            {
                if (trainer.Gold < 0) throw new InvalidOperationException("Trainer Gold is negative.");
                var roster = trainer.Roster;
                if (roster.Storage.Count > cfg.MonsterStorageSettings.Capacity ||
                    !HasMonsterSlot(trainer, additional: 0) || roster.Members.Count > MonsterRoster.Capacity ||
                    (roster.Members.Count == 0 ? roster.Active != null : !roster.Members.Contains(roster.Active)))
                    throw new InvalidOperationException("Invalid field roster or Active position.");
                foreach (var monster in roster.Members.Concat(roster.Storage))
                {
                    if (monster.Owner != roster || monster.IsStored != roster.Storage.Contains(monster))
                        throw new InvalidOperationException("Invalid roster/storage ownership.");
                    bool recovering = recoveries.Any(r => !r.IsCapturedMonster && r.TrainerId == trainer.Id && r.MonsterId == monster.Id);
                    Check(monster, recovering ? MonsterCustody.Hospital : MonsterCustody.Trainer);
                }
            }
            if (geneBank.Count > (cfg.GeneBankSettings ?? GeneBankConfig.Prototype).Capacity)
                throw new InvalidOperationException("Gene Bank exceeds storage capacity.");
            foreach (var monster in geneBank.StoredMonsters)
            {
                Check(monster, MonsterCustody.GeneBank);
                if (monster.Owner != null || trainers.Count(t => geneBank.GetStoredMonster(t.Id, monster.Id) != null) != 1)
                    throw new InvalidOperationException("Orphaned Gene Bank deposit.");
            }
            foreach (var monster in geneBank.ConfiscatedMonsters)
            {
                Check(monster, MonsterCustody.Hub);
                if (monster.Owner != null) throw new InvalidOperationException("Confiscated Monster still has a roster owner.");
            }
            if (veterinaryHospital.OccupiedRecoveryBeds > veterinaryHospital.RecoveryBedCapacity ||
                veterinaryHospital.OccupiedEmergencyBeds > veterinaryHospital.EmergencyBedCapacity)
                throw new InvalidOperationException("Hospital exceeds bed capacity.");
            var recoveryEvents = queue.Snapshot.Where(e => e.Kind == SimEventKind.MonsterRecoveryDone).ToArray();
            foreach (var recovery in recoveries)
            {
                var monster = veterinaryHospital.RecoveringMonster(recovery.RecoveryId);
                if (recovery.TrainerId < 0 || recovery.TrainerId >= trainers.Count || monster == null)
                    throw new InvalidOperationException("Recovery has no valid Trainer or Monster.");
                if (recovery.IsCapturedMonster)
                {
                    Check(monster, MonsterCustody.Hospital);
                    if (monster.Owner != null) throw new InvalidOperationException("Capture recovery still has a roster owner.");
                }
                else if (!trainers[recovery.TrainerId].Roster.Members.Contains(monster))
                    throw new InvalidOperationException("Orphaned emergency recovery.");
                if (recoveryEvents.Count(e => e.Arg == recovery.RecoveryId && e.Time == recovery.CompleteAtMinute && e.TrainerId == -1) != 1)
                    throw new InvalidOperationException("Recovery must have exactly one completion event.");
            }
            if (recoveryEvents.Any(e => !recoveries.Any(r => r.RecoveryId == e.Arg && r.CompleteAtMinute == e.Time && e.TrainerId == -1)))
                throw new InvalidOperationException("Orphaned recovery event.");
        }
    }
}
