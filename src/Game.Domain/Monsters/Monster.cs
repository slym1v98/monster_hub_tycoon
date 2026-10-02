using System;
using System.Collections.Generic;

namespace Game.Domain.Monsters
{
    /// <summary>Monster sở hữu HP và gen riêng; việc đổi đội hoặc gửi kho không sinh lại dữ liệu.</summary>
    public sealed class Monster
    {
        public MonsterId Id { get; }
        public MonsterDefinition Definition { get; }
        public string SpeciesId => Definition.Id;
        public MonsterElement Element => Definition.Element;
        public MonsterRole Role => Definition.Role;
        public Rarity Rarity { get; }
        public MonsterIvGrade Iv { get; }
        public int Level { get; }
        public bool IsSoulBound { get; }
        /// <summary>Năm giá trị gen cố định theo thứ tự HP, ATK, DEF, ASPD, CRIT trong [0, 1).</summary>
        public IReadOnlyList<double> Genes { get; }
        public MonsterStats Stats { get; }
        public long CurrentHp { get; private set; }
        public long MaxHp => Stats.Hp;
        public MonsterLifeState LifeState => IsStored ? MonsterLifeState.Stored :
            CurrentHp == 0 ? MonsterLifeState.Fainted : MonsterLifeState.Ready;

        internal MonsterRoster Owner { get; set; }
        internal bool IsStored { get; set; }

        Monster(MonsterId id, MonsterDefinition definition, Rarity rarity, MonsterIvGrade iv, int level,
            int seed, bool isSoulBound)
        {
            Id = id;
            Definition = definition;
            Rarity = rarity;
            Iv = iv;
            Level = level;
            IsSoulBound = isSoulBound;
            var random = new SimRandom(seed);
            Genes = Array.AsReadOnly(new[] { random.NextDouble(), random.NextDouble(), random.NextDouble(),
                random.NextDouble(), random.NextDouble() });
            // Tác vụ 2 sẽ tính tăng trưởng, Rarity và IV từ các đầu vào tường minh.
            Stats = definition.BaseStats;
            CurrentHp = MaxHp;
        }

        public static Monster Create(MonsterId id, MonsterDefinition definition, Rarity rarity,
            MonsterIvGrade iv, int level, int seed, bool isSoulBound = false)
        {
            if (string.IsNullOrWhiteSpace(id.Value)) throw new ArgumentException("Mã Monster không được rỗng.", nameof(id));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            definition.Validate();
            if (!Enum.IsDefined(typeof(Rarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (!Enum.IsDefined(typeof(MonsterIvGrade), iv)) throw new ArgumentOutOfRangeException(nameof(iv));
            if (level < 1 || level > 100) throw new ArgumentOutOfRangeException(nameof(level));
            return new Monster(id, definition, rarity, iv, level, seed, isSoulBound);
        }

        /// <summary>Cập nhật HP của chính Monster; HP bằng 0 là ngất, không mất danh tính.</summary>
        public void SetCurrentHp(long hp)
        {
            if (hp < 0 || hp > MaxHp) throw new ArgumentOutOfRangeException(nameof(hp));
            CurrentHp = hp;
        }
    }
}
