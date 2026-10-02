using System;
using System.Collections.Generic;
using System.Linq;

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
        public MonsterCustody Custody { get; internal set; } = MonsterCustody.Unassigned;
        /// <summary>Năm giá trị gen cố định theo thứ tự HP, ATK, DEF, ASPD, CRIT trong [0, 1).</summary>
        public IReadOnlyList<double> Genes { get; }
        public MonsterStats Stats { get; private set; }
        public long CurrentHp { get; private set; }
        public long MaxHp => Stats.Hp;
        public MonsterLifeState LifeState => Custody == MonsterCustody.Hospital ? MonsterLifeState.Recovering : IsStored ? MonsterLifeState.Stored :
            CurrentHp == 0 ? MonsterLifeState.Fainted : MonsterLifeState.Ready;

        internal MonsterRoster Owner { get; set; }
        internal bool IsStored { get; set; }

        readonly MonsterStatConfig statConfig;
        readonly List<(double attack, double defense, double critical, double rebellionReduction, int expiresAt)> temporaryEffects
            = new List<(double, double, double, double, int)>();

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

        public long RestoreHp(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            long restored = Math.Min(amount, MaxHp - CurrentHp);
            CurrentHp += restored;
            return restored;
        }

        public void ApplyTemporaryCombatEffects(double attackMultiplier, double defenseMultiplier,
            double criticalBonus, double managementReduction, int expiresAtMinute)
        {
            if (double.IsNaN(attackMultiplier) || double.IsInfinity(attackMultiplier) || attackMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(attackMultiplier));
            if (double.IsNaN(defenseMultiplier) || double.IsInfinity(defenseMultiplier) || defenseMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(defenseMultiplier));
            if (double.IsNaN(criticalBonus) || double.IsInfinity(criticalBonus) || criticalBonus < 0) throw new ArgumentOutOfRangeException(nameof(criticalBonus));
            if (double.IsNaN(managementReduction) || double.IsInfinity(managementReduction) || managementReduction < 0) throw new ArgumentOutOfRangeException(nameof(managementReduction));
            temporaryEffects.Add((attackMultiplier, defenseMultiplier, criticalBonus, managementReduction, expiresAtMinute));
        }

        internal MonsterStats CombatStatsAt(int minute)
        {
            var active = temporaryEffects.Where(x => x.expiresAt > minute).ToArray();
            if (active.Length == 0) return Stats;
            return new MonsterStats(Stats.Hp, Stats.Attack * active.Aggregate(1d, (n, x) => n * x.attack),
                Stats.Defense * active.Aggregate(1d, (n, x) => n * x.defense), Stats.AttackSpeed,
                Math.Min(1, Stats.CriticalChance + active.Sum(x => x.critical)));
        }
        internal double RebellionReductionAt(int minute) => temporaryEffects.Where(x => x.expiresAt > minute).Sum(x => x.rebellionReduction);
        internal bool HasCombatEffectAt(int minute) => temporaryEffects.Any(x => x.expiresAt > minute);
        internal bool HasRebellionEffectAt(int minute) => temporaryEffects.Any(x => x.expiresAt > minute && x.rebellionReduction > 0);

        /// <summary>Cập nhật HP của chính Monster; HP bằng 0 là ngất, không mất danh tính.</summary>
        public void SetCurrentHp(long hp)
        {
            if (hp < 0 || hp > MaxHp) throw new ArgumentOutOfRangeException(nameof(hp));
            CurrentHp = hp;
        }
    }
}
