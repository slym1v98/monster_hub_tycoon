using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Monsters
{
    public sealed class VeterinaryHospitalConfig
    {
        public static VeterinaryHospitalConfig Prototype { get; } = new VeterinaryHospitalConfig();
        public int RecoveryBeds { get; }
        public int EmergencyBeds { get; }
        public int FirstCaptureRecoveryMinutes { get; }
        public int EmergencyRecoveryMinutes { get; }
        public int FaintedRecoveryMultiplier { get; }
        public long EmergencyPricePerTenHp { get; }
        public long FaintedSurcharge { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public VeterinaryHospitalConfig(int recoveryBeds = 100, int emergencyBeds = 25,
            int firstCaptureRecoveryMinutes = 1440, int emergencyRecoveryMinutes = 60,
            int faintedRecoveryMultiplier = 3, long emergencyPricePerTenHp = 14, long faintedSurcharge = 50)
        {
            if (recoveryBeds <= 0 || emergencyBeds <= 0) throw new ArgumentOutOfRangeException(nameof(recoveryBeds));
            if (firstCaptureRecoveryMinutes <= 0 || emergencyRecoveryMinutes <= 0 || faintedRecoveryMultiplier < 1)
                throw new ArgumentOutOfRangeException(nameof(firstCaptureRecoveryMinutes));
            if (emergencyPricePerTenHp < 0 || faintedSurcharge < 0) throw new ArgumentOutOfRangeException(nameof(emergencyPricePerTenHp));
            RecoveryBeds = recoveryBeds; EmergencyBeds = emergencyBeds;
            FirstCaptureRecoveryMinutes = firstCaptureRecoveryMinutes; EmergencyRecoveryMinutes = emergencyRecoveryMinutes;
            FaintedRecoveryMultiplier = faintedRecoveryMultiplier; EmergencyPricePerTenHp = emergencyPricePerTenHp;
            FaintedSurcharge = faintedSurcharge;
            BalanceParameters = Array.AsReadOnly(new[] {
                P("recovery_beds", recoveryBeds, "Monster"), P("emergency_beds", emergencyBeds, "Monster"),
                P("first_capture_recovery_minutes", firstCaptureRecoveryMinutes, "minutes"),
                P("emergency_recovery_minutes", emergencyRecoveryMinutes, "minutes"),
                P("fainted_recovery_multiplier", faintedRecoveryMultiplier, "multiplier"),
                P("emergency_price_per_ten_hp", emergencyPricePerTenHp, "Gold/10 HP"),
                P("fainted_surcharge", faintedSurcharge, "Gold")
            });
        }
        static BalanceParameter P(string name, double value, string unit) => new BalanceParameter(
            "veterinary." + name, value, unit, "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.4; exact recovery duration/surcharge are unspecified.");
    }

    public enum AdmissionStatus { Accepted, NoMissingHp, Full, UnknownTrainer, AlreadyOwned, AlreadyAdmitted, InsufficientFunds, UnknownRecovery }
    public sealed class AdmissionResult
    {
        public AdmissionStatus Status { get; }
        public int RecoveryId { get; }
        public int TrainerId { get; }
        public MonsterId MonsterId { get; }
        public int CompleteAtMinute { get; }
        public long Fee { get; }
        public bool IsCapturedMonster { get; }
        public bool Accepted => Status == AdmissionStatus.Accepted;
        internal AdmissionResult(AdmissionStatus status, int recoveryId = -1, int trainerId = -1,
            MonsterId monsterId = default, int completeAtMinute = -1, long fee = 0, bool isCaptured = false)
        { Status = status; RecoveryId = recoveryId; TrainerId = trainerId; MonsterId = monsterId; CompleteAtMinute = completeAtMinute; Fee = fee; IsCapturedMonster = isCaptured; }
    }

    public sealed class VeterinaryHospital
    {
        sealed class Recovery
        {
            public int Id;
            public Trainer Trainer;
            public Monster Monster;
            public int FinishMinute;
            public long Fee;
            public bool Captured;
        }
        readonly Dictionary<int, Trainer> trainers;
        readonly Dictionary<int, Recovery> recoveries = new Dictionary<int, Recovery>();
        readonly VeterinaryHospitalConfig config;
        int nextId;
        public int OccupiedRecoveryBeds => recoveries.Values.Count(x => x.Captured);
        public int OccupiedEmergencyBeds => recoveries.Values.Count(x => !x.Captured);
        public int RecoveryBedCapacity => config.RecoveryBeds;
        public int EmergencyBedCapacity => config.EmergencyBeds;

        internal IReadOnlyList<AdmissionResult> Recoveries => Array.AsReadOnly(recoveries.Values.OrderBy(x => x.Id)
            .Select(x => new AdmissionResult(AdmissionStatus.Accepted, x.Id, x.Trainer.Id, x.Monster.Id,
                x.FinishMinute, x.Fee, x.Captured)).ToArray());
        internal Monster RecoveringMonster(int recoveryId) => recoveries.TryGetValue(recoveryId, out var recovery) ? recovery.Monster : null;
        internal bool Contains(MonsterId id) => recoveries.Values.Any(x => x.Monster.Id == id);

        public VeterinaryHospital(IEnumerable<Trainer> trainers, VeterinaryHospitalConfig config = null)
        {
            if (trainers == null) throw new ArgumentNullException(nameof(trainers));
            var entries = trainers.ToArray();
            if (entries.Any(x => x == null) || entries.Select(x => x.Id).Distinct().Count() != entries.Length)
                throw new ArgumentException("Veterinary Hospital requires unique valid Trainers.", nameof(trainers));
            this.trainers = entries.ToDictionary(x => x.Id);
            this.config = config ?? VeterinaryHospitalConfig.Prototype;
        }

        public AdmissionResult RequestEmergencyCare(MonsterId monsterId, int now)
        {
            if (now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            if (OccupiedEmergencyBeds >= config.EmergencyBeds) return new AdmissionResult(AdmissionStatus.Full);
            var trainer = trainers.Values.OrderBy(x => x.Id).FirstOrDefault(x => x.Roster.Members.Any(m => m.Id == monsterId));
            var monster = trainer?.Roster.Members.FirstOrDefault(x => x.Id == monsterId);
            if (monster == null) return new AdmissionResult(AdmissionStatus.UnknownTrainer);
            if (monster.Custody != MonsterCustody.Trainer) return new AdmissionResult(AdmissionStatus.AlreadyAdmitted);
            if (recoveries.Values.Any(x => x.Monster.Id == monsterId)) return new AdmissionResult(AdmissionStatus.AlreadyAdmitted);
            if (monster.CurrentHp == monster.MaxHp) return new AdmissionResult(AdmissionStatus.NoMissingHp);
            bool fainted = monster.CurrentHp == 0;
            long missing = monster.MaxHp - monster.CurrentHp;
            long fee = checked(((missing + 9) / 10) * config.EmergencyPricePerTenHp + (fainted ? config.FaintedSurcharge : 0));
            int duration = checked(config.EmergencyRecoveryMinutes * (fainted ? config.FaintedRecoveryMultiplier : 1));
            int completeAt = checked(now + duration);
            monster.Custody = MonsterCustody.Hospital;
            return Admit(new Recovery { Trainer = trainer, Monster = monster, FinishMinute = completeAt, Fee = fee });
        }

        public AdmissionResult AdmitCaptured(Monster monster, int trainerId, int now)
        {
            if (monster == null) throw new ArgumentNullException(nameof(monster));
            if (now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            if (!trainers.TryGetValue(trainerId, out var trainer)) return new AdmissionResult(AdmissionStatus.UnknownTrainer);
            if (monster.Custody != MonsterCustody.Unassigned || monster.Owner != null) return new AdmissionResult(AdmissionStatus.AlreadyOwned);
            if (recoveries.Values.Any(x => x.Monster.Id == monster.Id) || trainers.Values.Any(x =>
                x.Roster.Members.Any(m => m.Id == monster.Id) || x.Roster.Storage.Any(m => m.Id == monster.Id)))
                return new AdmissionResult(AdmissionStatus.AlreadyOwned);
            if (OccupiedRecoveryBeds >= config.RecoveryBeds) return new AdmissionResult(AdmissionStatus.Full);
            int completeAt = checked(now + config.FirstCaptureRecoveryMinutes);
            monster.Custody = MonsterCustody.Hospital;
            return Admit(new Recovery { Trainer = trainer, Monster = monster,
                FinishMinute = completeAt, Captured = true });
        }

        public AdmissionResult CompleteRecovery(int recoveryId)
        {
            if (!recoveries.TryGetValue(recoveryId, out var recovery))
                return new AdmissionResult(AdmissionStatus.UnknownRecovery, recoveryId: recoveryId);
            recoveries.Remove(recoveryId);
            recovery.Monster.RestoreHp(recovery.Monster.MaxHp);
            if (!recovery.Captured) recovery.Monster.Custody = MonsterCustody.Trainer;
            if (recovery.Captured)
            {
                recovery.Monster.Custody = MonsterCustody.Unassigned;
                if (recovery.Trainer.Roster.Members.Count < MonsterRoster.Capacity) recovery.Trainer.Roster.Add(recovery.Monster);
                else recovery.Trainer.Roster.AddToStorage(recovery.Monster);
            }
            return new AdmissionResult(AdmissionStatus.Accepted, recovery.Id, recovery.Trainer.Id,
                recovery.Monster.Id, recovery.FinishMinute, recovery.Fee, recovery.Captured);
        }

        internal bool CancelRecovery(int recoveryId)
        {
            if (!recoveries.TryGetValue(recoveryId, out var recovery)) return false;
            recoveries.Remove(recoveryId);
            if (!recovery.Captured) recovery.Monster.Custody = MonsterCustody.Trainer;
            else recovery.Monster.Custody = MonsterCustody.Unassigned;
            return true;
        }

        AdmissionResult Admit(Recovery recovery)
        {
            recovery.Id = nextId;
            nextId = checked(nextId + 1);
            recoveries.Add(recovery.Id, recovery);
            return new AdmissionResult(AdmissionStatus.Accepted, recovery.Id, recovery.Trainer.Id,
                recovery.Monster.Id, recovery.FinishMinute, recovery.Fee, recovery.Captured);
        }
    }
}
