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
        public int Level { get; private set; }
        public bool IsSoulBound { get; }
        /// <summary>Năm giá trị gen cố định theo thứ tự HP, ATK, DEF, ASPD, CRIT trong [0, 1).</summary>
        public IReadOnlyList<double> Genes { get; }
        public MonsterStats Stats { get; private set; }
        public long CurrentHp { get; private set; }
        public long MaxHp => Stats.Hp;
        public MonsterLifeState LifeState => IsStored ? MonsterLifeState.Stored :
            CurrentHp == 0 ? MonsterLifeState.Fainted : MonsterLifeState.Ready;

        internal MonsterRoster Owner { get; set; }
        internal bool IsStored { get; set; }

        readonly MonsterStatConfig statConfig;

        Monster(MonsterId id, MonsterDefinition definition, Rarity rarity, MonsterIvGrade iv, int level,
            int seed, bool isSoulBound, MonsterStatConfig statConfig)
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
            this.statConfig = statConfig;
            Stats = MonsterStatsCalculator.Calculate(definition, rarity, iv, level, statConfig);
            CurrentHp = MaxHp;
        }

        public static Monster Create(MonsterId id, MonsterDefinition definition, Rarity rarity,
            MonsterIvGrade iv, int level, int seed, bool isSoulBound = false, MonsterStatConfig statConfig = null)
        {
            if (string.IsNullOrWhiteSpace(id.Value)) throw new ArgumentException("Mã Monster không được rỗng.", nameof(id));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            definition.Validate();
            if (!Enum.IsDefined(typeof(Rarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (!Enum.IsDefined(typeof(MonsterIvGrade), iv)) throw new ArgumentOutOfRangeException(nameof(iv));
            if (level < 1 || level > 100) throw new ArgumentOutOfRangeException(nameof(level));
            return new Monster(id, definition, rarity, iv, level, seed, isSoulBound, statConfig ?? MonsterStatConfig.Prototype);
        }

        internal MonsterStats StatsAtLevel(int targetLevel) => targetLevel <= Level ? null :
            MonsterStatsCalculator.Calculate(Definition, Rarity, Iv, targetLevel, statConfig);

        /// <summary>Tăng cấp giữ nguyên HP hiện tại, kể cả trạng thái ngất; gen không đổi.</summary>
        internal void ApplyLevel(int targetLevel, MonsterStats stats)
        {
            if (stats == null) return;
            Level = targetLevel;
            Stats = stats;
            CurrentHp = Math.Min(CurrentHp, MaxHp);
        }

        /// <summary>Cập nhật HP của chính Monster; HP bằng 0 là ngất, không mất danh tính.</summary>
        public void SetCurrentHp(long hp)
        {
            if (hp < 0 || hp > MaxHp) throw new ArgumentOutOfRangeException(nameof(hp));
            CurrentHp = hp;
        }
    }
}
