using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Monsters
{
    public sealed class GeneBankConfig
    {
        public static GeneBankConfig Prototype { get; } = new GeneBankConfig();
        public int Capacity { get; }
        public long GoldPerMonsterPerDay { get; }
        public int PaydayDays { get; }
        public long ResalePrice { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public GeneBankConfig(int capacity = 500, long goldPerMonsterPerDay = 20, int paydayDays = 30,
            long resalePrice = 500)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (goldPerMonsterPerDay < 0 || paydayDays <= 0 || resalePrice < 0) throw new ArgumentOutOfRangeException(nameof(goldPerMonsterPerDay));
            Capacity = capacity; GoldPerMonsterPerDay = goldPerMonsterPerDay; PaydayDays = paydayDays; ResalePrice = resalePrice;
            BalanceParameters = Array.AsReadOnly(new[] {
                P("capacity", capacity, "Monsters"), P("fee_per_monster_day", goldPerMonsterPerDay, "Gold/Monster/day"),
                P("payday_days", paydayDays, "days"), P("resale_price", resalePrice, "Gold")
            });
        }
        static BalanceParameter P(string name, double value, string unit) => new BalanceParameter(
            "gene_bank." + name, value, unit, "Prototype", "docs/designs/13_Balance_Parameters.md §13 and docs/designs/02_HUB_Economy_Infrastructure.md §2; capacity/resale price are prototype settings.");
    }

    public sealed class BankFeeAssessment
    {
        public int TrainerId { get; }
        public int MonsterCount { get; }
        public int Days { get; }
        public long Amount { get; }
        public BankFeeAssessment(int trainerId, int monsterCount, int days, long amount)
        { TrainerId = trainerId; MonsterCount = monsterCount; Days = days; Amount = amount; }
    }

    public sealed class GeneBank
    {
        sealed class Entry
        {
            public int DepositorId;
            public Monster Monster;
            public long Sequence;
        }
        readonly List<Trainer> trainers;
        readonly List<Entry> stored = new List<Entry>();
        readonly List<Monster> confiscated = new List<Monster>();
        readonly GeneBankConfig config;
        long nextSequence;
        public int Count => stored.Count;
        public int ConfiscatedCount => confiscated.Count;
        public bool Contains(MonsterId id) => stored.Any(x => x.Monster.Id == id) || confiscated.Any(x => x.Id == id);
        public IReadOnlyList<Monster> StoredMonsters => Array.AsReadOnly(stored.OrderBy(x => x.Monster.Id.Value, StringComparer.Ordinal).Select(x => x.Monster).ToArray());
        public IReadOnlyList<Monster> ConfiscatedMonsters => Array.AsReadOnly(confiscated.OrderBy(x => x.Id.Value, StringComparer.Ordinal).ToArray());

        public GeneBank(IEnumerable<Trainer> trainers, GeneBankConfig config = null)
        {
            this.trainers = (trainers ?? throw new ArgumentNullException(nameof(trainers))).ToList();
            if (this.trainers.Any(x => x == null) || this.trainers.Select(x => x.Id).Distinct().Count() != this.trainers.Count)
                throw new ArgumentException("Gene Bank requires unique valid Trainers.", nameof(trainers));
            this.config = config ?? GeneBankConfig.Prototype;
        }

        public bool Store(int trainerId, MonsterId monsterId)
        {
            var trainer = FindTrainer(trainerId);
            if (stored.Count >= config.Capacity || Contains(monsterId)) return false;
            if (trainer.Roster.Members.All(x => x.Id != monsterId) && trainer.Roster.Storage.All(x => x.Id != monsterId)) return false;
            var owned = trainer.Roster.Members.FirstOrDefault(x => x.Id == monsterId)
                ?? trainer.Roster.Storage.FirstOrDefault(x => x.Id == monsterId);
            if (owned.Custody != MonsterCustody.Trainer) return false;
            var monster = trainer.Roster.RemoveForTransfer(monsterId);
            monster.Custody = MonsterCustody.GeneBank;
            stored.Add(new Entry { DepositorId = trainerId, Monster = monster, Sequence = nextSequence++ });
            return true;
        }

        public bool StoreUnassignedMonster(int trainerId, Monster monster)
        {
            FindTrainer(trainerId);
            if (monster == null) throw new ArgumentNullException(nameof(monster));
            if (stored.Count >= config.Capacity || Contains(monster.Id) || monster.Custody != MonsterCustody.Unassigned || monster.Owner != null) return false;
            monster.Custody = MonsterCustody.GeneBank;
            stored.Add(new Entry { DepositorId = trainerId, Monster = monster, Sequence = nextSequence++ });
            return true;
        }

        public bool Withdraw(int trainerId, MonsterId monsterId)
        {
            var trainer = FindTrainer(trainerId);
            var entry = stored.FirstOrDefault(x => x.DepositorId == trainerId && x.Monster.Id == monsterId);
            if (entry == null) return false;
            stored.Remove(entry);
            entry.Monster.Custody = MonsterCustody.Unassigned;
            if (trainer.Roster.Members.Count < MonsterRoster.Capacity) trainer.Roster.Add(entry.Monster);
            else trainer.Roster.AddToStorage(entry.Monster);
            return true;
        }

        public Monster GetStoredMonster(int trainerId, MonsterId monsterId)
        {
            FindTrainer(trainerId);
            return stored.FirstOrDefault(x => x.DepositorId == trainerId && x.Monster.Id == monsterId)?.Monster;
        }

        public Monster DismantleForTrainer(int trainerId, MonsterId monsterId)
        {
            FindTrainer(trainerId);
            var entry = stored.FirstOrDefault(x => x.DepositorId == trainerId && x.Monster.Id == monsterId);
            if (entry == null) return null;
            stored.Remove(entry);
            entry.Monster.Custody = MonsterCustody.Unassigned;
            return entry.Monster;
        }

        public BankFeeAssessment CalculatePaydayFee(int trainerId, int days = -1)
        {
            FindTrainer(trainerId);
            if (days < 0) days = config.PaydayDays;
            if (days <= 0) throw new ArgumentOutOfRangeException(nameof(days));
            int count = stored.Count(x => x.DepositorId == trainerId);
            long amount = checked(checked((long)count * config.GoldPerMonsterPerDay) * days);
            return new BankFeeAssessment(trainerId, count, days, amount);
        }

        /// <summary>Transfers one unpaid Trainer-owned deposit into HUB custody, weakest first then stable ID.</summary>
        public Monster ConfiscateForUnpaidFee(int trainerId)
        {
            FindTrainer(trainerId);
            var entry = stored.Where(x => x.DepositorId == trainerId)
                .OrderBy(x => Power(x.Monster.Stats)).ThenBy(x => x.Monster.Id.Value, StringComparer.Ordinal)
                .ThenBy(x => x.Sequence).FirstOrDefault();
            if (entry == null) return null;
            stored.Remove(entry);
            entry.Monster.Custody = MonsterCustody.Hub;
            confiscated.Add(entry.Monster);
            return entry.Monster;
        }

        public bool ResellToTrainer(int buyerId, MonsterId monsterId)
        {
            var buyer = FindTrainer(buyerId);
            var monster = confiscated.FirstOrDefault(x => x.Id == monsterId);
            if (monster == null || buyer.Gold < config.ResalePrice) return false;
            if (buyer.Roster.Members.Any(x => x.Id == monsterId) || buyer.Roster.Storage.Any(x => x.Id == monsterId)) return false;
            buyer.Gold -= config.ResalePrice;
            confiscated.Remove(monster);
            monster.Custody = MonsterCustody.Unassigned;
            if (buyer.Roster.Members.Count < MonsterRoster.Capacity) buyer.Roster.Add(monster);
            else buyer.Roster.AddToStorage(monster);
            return true;
        }

        public Monster Dismantle(MonsterId monsterId)
        {
            var entry = stored.FirstOrDefault(x => x.Monster.Id == monsterId);
            if (entry != null) stored.Remove(entry);
            else
            {
                var seized = confiscated.FirstOrDefault(x => x.Id == monsterId);
                if (seized == null) return null;
                confiscated.Remove(seized);
                seized.Custody = MonsterCustody.Consumed;
                return seized;
            }
            entry.Monster.Custody = MonsterCustody.Consumed;
            return entry.Monster;
        }

        Trainer FindTrainer(int trainerId) => trainers.FirstOrDefault(x => x.Id == trainerId)
            ?? throw new ArgumentOutOfRangeException(nameof(trainerId), "Trainer does not exist.");
        static double Power(MonsterStats s) => s.Hp + s.Attack * 10 + s.Defense * 8 + s.AttackSpeed * 20 + s.CriticalChance * 100;
    }
}
