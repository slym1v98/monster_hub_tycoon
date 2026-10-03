using System;
using System.Collections.Generic;
using Game.Domain.Materials;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    public sealed class MonsterItemConfig
    {
        public static MonsterItemConfig Prototype { get; } = new MonsterItemConfig();
        public long PotionHeal { get; }
        public double BuffAttackMultiplier { get; }
        public double BuffDefenseMultiplier { get; }
        public double BuffCriticalBonus { get; }
        public int BuffDurationMinutes { get; }
        public double CakeRebellionReduction { get; }
        public int CakeDurationMinutes { get; }
        public double CommunicationLeadershipBonus { get; }
        public int SynergyDurationMinutes { get; }
        public double SynergyTankDefenseMultiplier { get; }
        public double SynergyDpsCriticalBonus { get; }
        public long SynergySupportHeal { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public MonsterItemConfig(long potionHeal = 100, double buffAttackMultiplier = 1.25,
            double buffDefenseMultiplier = 1.2, double buffCriticalBonus = 0.05, int buffDurationMinutes = 60,
            double cakeRebellionReduction = 10, int cakeDurationMinutes = 30,
            double communicationLeadershipBonus = 10, int synergyDurationMinutes = 1440,
            double synergyTankDefenseMultiplier = 1.2, double synergyDpsCriticalBonus = 0.1, long synergySupportHeal = 15)
        {
            if (potionHeal <= 0) throw new ArgumentOutOfRangeException(nameof(potionHeal));
            ZoneDefinition.ValidatePositiveFinite(buffAttackMultiplier, nameof(buffAttackMultiplier));
            ZoneDefinition.ValidatePositiveFinite(buffDefenseMultiplier, nameof(buffDefenseMultiplier));
            ZoneDefinition.ValidateNonNegativeFinite(buffCriticalBonus, nameof(buffCriticalBonus));
            if (buffCriticalBonus > 1) throw new ArgumentOutOfRangeException(nameof(buffCriticalBonus));
            if (buffDurationMinutes <= 0 || cakeDurationMinutes <= 0 || synergyDurationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(buffDurationMinutes));
            ZoneDefinition.ValidatePositiveFinite(cakeRebellionReduction, nameof(cakeRebellionReduction));
            ZoneDefinition.ValidatePositiveFinite(communicationLeadershipBonus, nameof(communicationLeadershipBonus));
            ZoneDefinition.ValidatePositiveFinite(synergyTankDefenseMultiplier, nameof(synergyTankDefenseMultiplier));
            ZoneDefinition.ValidateNonNegativeFinite(synergyDpsCriticalBonus, nameof(synergyDpsCriticalBonus));
            if (synergySupportHeal < 0) throw new ArgumentOutOfRangeException(nameof(synergySupportHeal));
            PotionHeal = potionHeal; BuffAttackMultiplier = buffAttackMultiplier; BuffDefenseMultiplier = buffDefenseMultiplier;
            BuffCriticalBonus = buffCriticalBonus; BuffDurationMinutes = buffDurationMinutes;
            CakeRebellionReduction = cakeRebellionReduction; CakeDurationMinutes = cakeDurationMinutes;
            CommunicationLeadershipBonus = communicationLeadershipBonus; SynergyDurationMinutes = synergyDurationMinutes;
            SynergyTankDefenseMultiplier = synergyTankDefenseMultiplier; SynergyDpsCriticalBonus = synergyDpsCriticalBonus; SynergySupportHeal = synergySupportHeal;
            BalanceParameters = Array.AsReadOnly(new[] {
                P("potion_heal", potionHeal, "HP", "docs/designs/12_Item_Catalog.md: Potion heals Monster HP."),
                P("buff_attack_multiplier", buffAttackMultiplier, "multiplier", "docs/designs/04_Monster_System.md: buff bottle temporarily increases stats."),
                P("buff_defense_multiplier", buffDefenseMultiplier, "multiplier", "docs/designs/04_Monster_System.md: buff bottle temporarily increases stats."),
                P("buff_critical_bonus", buffCriticalBonus, "probability", "docs/designs/04_Monster_System.md: buff bottle temporarily increases stats."),
                P("buff_duration_minutes", buffDurationMinutes, "minutes", "Prototype buff duration; GDD does not specify duration."),
                P("cake_rebellion_reduction", cakeRebellionReduction, "management_points", "docs/designs/12_Item_Catalog.md: Reward Cake reduces Rebellion."),
                P("cake_duration_minutes", cakeDurationMinutes, "minutes", "Prototype one-expedition window; GDD does not specify duration."),
                P("communication_leadership_bonus", communicationLeadershipBonus, "leadership_points", "docs/designs/12_Item_Catalog.md: communication lock increases effective Leadership."),
                P("synergy_duration_minutes", synergyDurationMinutes, "minutes", "Prototype tactics-book synergy duration; GDD does not specify duration."),
                P("synergy_tank_defense_multiplier", synergyTankDefenseMultiplier, "multiplier", "docs/designs/04_Monster_System.md: Tank reserve grants HP-oriented protection; implementation uses DEF proxy."),
                P("synergy_dps_critical_bonus", synergyDpsCriticalBonus, "probability", "docs/designs/04_Monster_System.md: DPS reserve buffs Critical chance."),
                P("synergy_support_heal", synergySupportHeal, "HP/battle", "docs/designs/04_Monster_System.md: Support reserve provides Regen.")
            });
        }
        static BalanceParameter P(string id, double value, string unit, string source)
            => new BalanceParameter("item_effect." + id, value, unit, "Prototype", source);
    }

    public sealed class ItemEffectResult
    {
        public ProductId Item { get; }
        public bool Applied { get; }
        public int ConsumedUnits { get; }
        public long HpRestored { get; }
        public double LeadershipBonus { get; }
        public bool BagSynergyEnabled { get; }
        public int ExpiresAtMinute { get; }
        internal ItemEffectResult(ProductId item, bool applied, int consumedUnits = 0, long hpRestored = 0,
            double leadershipBonus = 0, bool bagSynergyEnabled = false, int expiresAtMinute = -1)
        { Item = item; Applied = applied; ConsumedUnits = consumedUnits; HpRestored = hpRestored; LeadershipBonus = leadershipBonus; BagSynergyEnabled = bagSynergyEnabled; ExpiresAtMinute = expiresAtMinute; }
    }

    public static class MonsterItemEffects
    {
        public static ItemEffectResult Apply(ProductId item, Monster target, MonsterItemConfig config)
            => Apply(item, target, config, 0, managementScore: 0, leadershipScore: 0, hasEligibleReserve: false, hasCommunicationLock: false);

        public static ItemEffectResult Apply(ProductId item, Monster target, MonsterItemConfig config, int currentMinute,
            double managementScore, double leadershipScore, bool hasEligibleReserve, bool hasCommunicationLock)
        {
            if (string.IsNullOrWhiteSpace(item.Value)) throw new ArgumentException("Product ID is required.", nameof(item));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (currentMinute < 0) throw new ArgumentOutOfRangeException(nameof(currentMinute));
            if (double.IsNaN(managementScore) || double.IsInfinity(managementScore) || double.IsNaN(leadershipScore) || double.IsInfinity(leadershipScore)) throw new ArgumentOutOfRangeException(nameof(managementScore));
            if (item.Value == "potion")
            {
                long restored = target.RestoreHp(config.PotionHeal);
                return new ItemEffectResult(item, restored > 0, restored > 0 ? 1 : 0, restored);
            }
            if (item.Value == "monster_buff_bottle")
            {
                if (target.HasCombatEffectAt(currentMinute)) return new ItemEffectResult(item, false);
                int expires = checked(currentMinute + config.BuffDurationMinutes);
                target.ApplyTemporaryCombatEffects(config.BuffAttackMultiplier, config.BuffDefenseMultiplier,
                    config.BuffCriticalBonus, 0, expires);
                return new ItemEffectResult(item, true, 1, expiresAtMinute: expires);
            }
            if (item.Value == "reward_cake")
            {
                if (managementScore <= leadershipScore || target.HasRebellionEffectAt(currentMinute)) return new ItemEffectResult(item, false);
                int expires = checked(currentMinute + config.CakeDurationMinutes);
                target.ApplyTemporaryCombatEffects(1, 1, 0, config.CakeRebellionReduction, expires);
                return new ItemEffectResult(item, true, 1, expiresAtMinute: expires);
            }
            if (item.Value == "tactics_book" && hasEligibleReserve)
                return new ItemEffectResult(item, true, 1, bagSynergyEnabled: true,
                    expiresAtMinute: checked(currentMinute + config.SynergyDurationMinutes));
            if (item.Value == "pet_communication_lock" && !hasCommunicationLock)
                return new ItemEffectResult(item, true, 1, leadershipBonus: config.CommunicationLeadershipBonus);
            return new ItemEffectResult(item, false);
        }
    }
}
