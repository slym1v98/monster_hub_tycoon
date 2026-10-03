using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Monsters
{
    /// <summary>Sở hữu đội tối đa ba Monster và kho; đội không rỗng luôn có đúng một vị trí Active.</summary>
    public sealed class MonsterRoster
    {
        public const int Capacity = 3;
        readonly Trainer trainer;
        readonly List<Monster> members = new List<Monster>();
        readonly List<Monster> storage = new List<Monster>();
        public IReadOnlyList<Monster> Members { get; }
        public IReadOnlyList<Monster> Storage { get; }
        public Monster Active { get; private set; }
        public IReadOnlyList<Monster> Reserves => Array.AsReadOnly(members.Where(x => x != Active).ToArray());
        public long TotalMissingHp => members.Sum(x => x.MaxHp - x.CurrentHp);

        public MonsterRoster() : this(null) { }

        internal MonsterRoster(Trainer trainer)
        {
            this.trainer = trainer;
            Members = members.AsReadOnly();
            Storage = storage.AsReadOnly();
        }

        public void Add(Monster monster)
        {
            if (monster == null) throw new ArgumentNullException(nameof(monster));
            if (members.Any(x => x.Id == monster.Id) || storage.Any(x => x.Id == monster.Id))
                throw new InvalidOperationException("Monster đã thuộc đội hoặc kho.");
            if (monster.Owner != null || monster.Custody != MonsterCustody.Unassigned) throw new InvalidOperationException("Monster đã có chủ sở hữu hoặc đang được giữ bởi một dịch vụ khác.");
            EnsureCapacity();
            if (trainer != null)
            {
                int level = MonsterProgression.MonsterLevel(trainer.Rank, trainer.Level);
                monster.ApplyLevel(level, monster.StatsAtLevel(level));
            }
            members.Add(monster);
            Sort(members);
            monster.Owner = this;
            monster.Custody = MonsterCustody.Trainer;
            if (Active == null) Active = monster;
        }

        public void AddToStorage(Monster monster)
        {
            if (monster == null) throw new ArgumentNullException(nameof(monster));
            if (members.Any(x => x.Id == monster.Id) || storage.Any(x => x.Id == monster.Id))
                throw new InvalidOperationException("Monster đã thuộc đội hoặc kho.");
            if (monster.Owner != null || monster.Custody != MonsterCustody.Unassigned) throw new InvalidOperationException("Monster đã có chủ sở hữu hoặc đang được giữ bởi một dịch vụ khác.");
            storage.Add(monster);
            Sort(storage);
            monster.Owner = this;
            monster.Custody = MonsterCustody.Trainer;
            monster.IsStored = true;
        }

        public Monster RemoveForTransfer(MonsterId id)
        {
            var monster = members.FirstOrDefault(x => x.Id == id);
            if (monster != null)
            {
                if (monster.Custody != MonsterCustody.Trainer) throw new InvalidOperationException("Monster đang được giữ bởi một dịch vụ khác.");
                members.Remove(monster);
                if (Active == monster) Active = members.FirstOrDefault(x => x.CurrentHp > 0) ?? members.FirstOrDefault();
            }
            else
            {
                monster = storage.FirstOrDefault(x => x.Id == id);
                if (monster == null) throw new InvalidOperationException("Monster không thuộc đội hoặc kho Trainer.");
                if (monster.Custody != MonsterCustody.Trainer) throw new InvalidOperationException("Monster đang được giữ bởi một dịch vụ khác.");
                storage.Remove(monster);
                monster.IsStored = false;
            }
            monster.Owner = null;
            monster.Custody = MonsterCustody.Unassigned;
            return monster;
        }

        public void SetActive(MonsterId id)
        {
            var monster = Find(members, id);
            if (monster.CurrentHp == 0) throw new InvalidOperationException("Không thể chọn Monster ngất để chiến đấu.");
            Active = monster;
        }

        public void MoveToStorage(MonsterId id)
        {
            var monster = Find(members, id);
            if (monster.Custody != MonsterCustody.Trainer) throw new InvalidOperationException("Monster đang được giữ bởi một dịch vụ khác.");
            members.Remove(monster);
            storage.Add(monster);
            Sort(storage);
            monster.IsStored = true;
            if (Active == monster)
                // Ưu tiên con còn HP theo ID; nếu cả đội ngất vẫn giữ một vị trí Active.
                Active = members.FirstOrDefault(x => x.CurrentHp > 0) ?? members.FirstOrDefault();
        }

        public void RestoreFromStorage(MonsterId id)
        {
            var monster = Find(storage, id);
            EnsureCapacity();
            storage.Remove(monster);
            members.Add(monster);
            Sort(members);
            monster.IsStored = false;
            if (Active == null) Active = monster;
        }

        /// <summary>Cầu nối farm cũ đến tác vụ 9: trừ HP Active rồi Reserve theo ID, không giữ HP gộp.</summary>
        public void ApplyDamage(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Active == null) return;
            amount = Damage(Active, amount);
            foreach (var monster in members)
                if (monster != Active) amount = Damage(monster, amount);
        }

        /// <summary>Cầu nối Bệnh Viện cũ: chỉ hồi HP thành viên trong đội, không tác động kho.</summary>
        public void RestoreAllHp()
        {
            foreach (var monster in members) monster.SetCurrentHp(monster.MaxHp);
        }

        static long Damage(Monster monster, long amount)
        {
            long lost = Math.Min(monster.CurrentHp, amount);
            monster.SetCurrentHp(monster.CurrentHp - lost);
            return amount - lost;
        }

        void EnsureCapacity()
        {
            if (members.Count >= Capacity) throw new InvalidOperationException("Đội Monster đã đủ ba thành viên.");
        }

        static Monster Find(List<Monster> monsters, MonsterId id) => monsters.FirstOrDefault(x => x.Id == id)
            ?? throw new InvalidOperationException("Monster không thuộc vị trí yêu cầu.");
        static void Sort(List<Monster> monsters) => monsters.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value));
    }
}
