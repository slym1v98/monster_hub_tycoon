using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Gear
{
    public sealed record GearSetBonus(int RequiredCount, GearStats Bonus);

    public sealed class GearSetDefinition
    {
        public string Id { get; }
        public IReadOnlyList<GearSetBonus> Bonuses { get; }
        public GearSetDefinition(string id, IEnumerable<GearSetBonus> bonuses)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Set ID is required.", nameof(id));
            var list = (bonuses ?? throw new ArgumentNullException(nameof(bonuses))).OrderBy(x => x.RequiredCount).ToArray();
            if (list.Length == 0 || list.Any(x => x.RequiredCount != 2 && x.RequiredCount != 4 && x.RequiredCount != 6) ||
                list.Select(x => x.RequiredCount).Distinct().Count() != list.Length) throw new ArgumentException("Set bonuses must use unique 2/4/6 thresholds.", nameof(bonuses));
            Id = id; Bonuses = Array.AsReadOnly(list);
        }
    }

    /// <summary>Danh mục slot, chỉ số gốc theo Tier, Tinh Luyện, Sao và Set.</summary>
    public sealed class GearCatalog
    {
        public IReadOnlyList<GearSlot> Slots { get; }
        public IReadOnlyList<GearSetDefinition> Sets { get; }
        public GearConfig Config { get; }

        public static GearCatalog Default { get; } = new GearCatalog(GearConfig.Prototype);

        public GearCatalog(GearConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Slots = Array.AsReadOnly(new[]
            {
                S("trainer.hat", GearGroup.TrainerUtility, 0, "Trainer", GearStatKind.StaminaDecayReduction),
                S("trainer.coat", GearGroup.TrainerUtility, 1, "Trainer", GearStatKind.WeatherResist),
                S("trainer.backpack", GearGroup.TrainerUtility, 2, "Trainer", GearStatKind.BackpackCapacity),
                S("trainer.boots", GearGroup.TrainerUtility, 3, "Trainer", GearStatKind.MoveSpeedMultiplier),
                S("trainer.bottle", GearGroup.TrainerUtility, 4, "Trainer", GearStatKind.HydrationDecayReduction),
                S("trainer.gloves", GearGroup.TrainerUtility, 5, "Trainer", GearStatKind.MiningSpeed),
                S("aura.whistle", GearGroup.Aura, 0, "Trainer", GearStatKind.AuraAttackMultiplier),
                S("aura.badge", GearGroup.Aura, 1, "Trainer", GearStatKind.AuraDefenseMultiplier),
                S("aura.cape", GearGroup.Aura, 2, "Trainer", GearStatKind.AuraDefenseMultiplier),
                S("aura.goggles", GearGroup.Aura, 3, "Trainer", GearStatKind.NightVision),
                S("aura.jewelry", GearGroup.Aura, 4, "Trainer", GearStatKind.AuraCritChance),
                S("aura.satellite", GearGroup.Aura, 5, "Trainer", GearStatKind.AuraAttackMultiplier),
                S("monster.weapon", GearGroup.MonsterCombat, 0, "Monster", GearStatKind.Attack),
                S("monster.armor", GearGroup.MonsterCombat, 1, "Monster", GearStatKind.Defense),
                S("monster.collar", GearGroup.MonsterCombat, 2, "Monster", GearStatKind.Hp),
                S("monster.bell", GearGroup.MonsterCombat, 3, "Monster", GearStatKind.CriticalChance),
                S("monster.hooves", GearGroup.MonsterCombat, 4, "Monster", GearStatKind.AttackSpeed),
                S("monster.core", GearGroup.MonsterCombat, 5, "Monster", GearStatKind.Attack)
            });
            // Prototype: hai bộ mẫu cho Monster để kiểm thử 2/4/6; GDD chỉ khóa ngưỡng, không khóa nội dung.
            Sets = Array.AsReadOnly(new[]
            {
                new GearSetDefinition("monster_set_alpha", new[]
                {
                    new GearSetBonus(2, new GearStats(Attack: 5)),
                    new GearSetBonus(4, new GearStats(Defense: 8, Hp: 40)),
                    new GearSetBonus(6, new GearStats(Attack: 15, CriticalChance: 0.05))
                }),
                new GearSetDefinition("aura_set_alpha", new[]
                {
                    new GearSetBonus(2, new GearStats(AuraAttackMultiplier: 1.05)),
                    new GearSetBonus(4, new GearStats(AuraDefenseMultiplier: 1.05)),
                    new GearSetBonus(6, new GearStats(AuraCritChance: 0.05))
                })
            });
        }

        static GearSlot S(string id, GearGroup g, int i, string owner, GearStatKind k) => new GearSlot(id, g, i, owner, k);

        public GearSlot GetSlot(string id)
            => Slots.FirstOrDefault(x => x.Id == id) ?? throw new ArgumentException("Unknown slot: " + id, nameof(id));

        public GearSetDefinition GetSet(string id) => Sets.FirstOrDefault(x => x.Id == id);

        public int MaxDurabilityFor(GearGroup group) => group switch
        {
            GearGroup.MonsterCombat => Config.MonsterMaxDurability,
            GearGroup.TrainerUtility => Config.UtilityMaxDurability,
            _ => Config.AuraMaxDurability
        };

        /// <summary>Chỉ số chính của slot ở Tier: UnitStat × Tier.</summary>
        public GearStats GetBaseStats(string slotId, int tier)
        {
            if (tier < 1 || tier > 5) throw new ArgumentOutOfRangeException(nameof(tier));
            return Build(GetSlot(slotId).StatKind, Config.UnitStat[GetSlot(slotId).StatKind] * tier);
        }

        public double GetRefineMultiplier(int refine)
        {
            if (refine < 0 || refine > 4) throw new ArgumentOutOfRangeException(nameof(refine));
            return Config.RefineMultipliers[refine];
        }

        public double GetStarPct(int stars)
        {
            if (stars < 1 || stars > 5) throw new ArgumentOutOfRangeException(nameof(stars));
            return (stars - 1) * Config.StarFractionPerStar;
        }

        internal static GearStats Build(GearStatKind kind, double value) => kind switch
        {
            GearStatKind.Attack => new GearStats(Attack: value),
            GearStatKind.Defense => new GearStats(Defense: value),
            GearStatKind.Hp => new GearStats(Hp: value),
            GearStatKind.AttackSpeed => new GearStats(AttackSpeed: value),
            GearStatKind.CriticalChance => new GearStats(CriticalChance: value),
            GearStatKind.BackpackCapacity => new GearStats(BackpackCapacity: value),
            GearStatKind.NightVision => new GearStats(NightVision: value > 0),
            GearStatKind.HydrationDecayReduction => new GearStats(HydrationDecayReduction: value),
            GearStatKind.MiningSpeed => new GearStats(MiningSpeed: value),
            GearStatKind.AuraAttackMultiplier => new GearStats(AuraAttackMultiplier: 1 + value),
            GearStatKind.AuraDefenseMultiplier => new GearStats(AuraDefenseMultiplier: 1 + value),
            GearStatKind.AuraCritChance => new GearStats(AuraCritChance: value),
            GearStatKind.StaminaDecayReduction => new GearStats(StaminaDecayReduction: value),
            GearStatKind.MoveSpeedMultiplier => new GearStats(MoveSpeedMultiplier: 1 + value),
            GearStatKind.WeatherResist => new GearStats(WeatherResist: value),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
