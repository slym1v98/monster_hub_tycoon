using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain
{
    /// <summary>Runnable Early Access Quest/KPI settings; placeholder values remain Prototype or TBD.</summary>
    public sealed class HubQuestConfig
    {
        public static HubQuestConfig Prototype { get; } = new HubQuestConfig();

        public long DailyMedicineGoldTarget { get; }
        public int DailyStockOperationTarget { get; }
        public int DailyLiquorUnitTarget { get; }
        public int WeeklyGeneConfiscationTarget { get; }
        public int WeeklyProtectionCharmSaleTarget { get; }
        public int DailyPeriodDays { get; }
        public int WeeklyPeriodDays { get; }
        public long DailyRewardGold { get; }
        public int DailyRewardGems { get; }
        public long WeeklyRewardGold { get; }
        public int WeeklyRewardGems { get; }
        public long CampaignRewardGold { get; }
        public int CampaignRewardGems { get; }
        public int CampaignRewardBuildingPermits { get; }
        public int CampaignRewardHighTierInvitations { get; }
        public int CampaignRewardProtectionCharms { get; }
        public int WeeklyRewardProtectionCharms { get; }
        public long ProtectionCharmOfferPrice { get; }
        public long ProtectionCharmExpectedAvoidedLoss { get; }
        public int MaximumProtectionCharmFulfillment { get; }
        public int CampaignFoundationRequirementCount { get; } = 2;
        public int CampaignFoundationZoneNumber { get; } = 2;
        public int CampaignFoundationHospitalLevel { get; } = 2;
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public HubQuestConfig(long dailyMedicineGoldTarget = 50_000,
            int dailyStockOperationTarget = 1, int dailyLiquorUnitTarget = 20,
            int weeklyGeneConfiscationTarget = 5, int weeklyProtectionCharmSaleTarget = 10,
            int dailyPeriodDays = 1, int weeklyPeriodDays = 7,
            long dailyRewardGold = 500, int dailyRewardGems = 0,
            long weeklyRewardGold = 2_500, int weeklyRewardGems = 0,
            long campaignRewardGold = 5_000, int campaignRewardGems = 0,
            int campaignRewardBuildingPermits = 1, int campaignRewardHighTierInvitations = 1,
            int campaignRewardProtectionCharms = 1, int weeklyRewardProtectionCharms = 1,
            long protectionCharmOfferPrice = 500, long protectionCharmExpectedAvoidedLoss = 1_000,
            int maximumProtectionCharmFulfillment = 1_000)
        {
            Positive(dailyMedicineGoldTarget, nameof(dailyMedicineGoldTarget));
            Positive(dailyStockOperationTarget, nameof(dailyStockOperationTarget));
            Positive(dailyLiquorUnitTarget, nameof(dailyLiquorUnitTarget));
            Positive(weeklyGeneConfiscationTarget, nameof(weeklyGeneConfiscationTarget));
            Positive(weeklyProtectionCharmSaleTarget, nameof(weeklyProtectionCharmSaleTarget));
            Positive(dailyPeriodDays, nameof(dailyPeriodDays));
            Positive(weeklyPeriodDays, nameof(weeklyPeriodDays));
            NonNegative(dailyRewardGold, nameof(dailyRewardGold));
            NonNegative(dailyRewardGems, nameof(dailyRewardGems));
            NonNegative(weeklyRewardGold, nameof(weeklyRewardGold));
            NonNegative(weeklyRewardGems, nameof(weeklyRewardGems));
            NonNegative(campaignRewardGold, nameof(campaignRewardGold));
            NonNegative(campaignRewardGems, nameof(campaignRewardGems));
            NonNegative(campaignRewardBuildingPermits, nameof(campaignRewardBuildingPermits));
            NonNegative(campaignRewardHighTierInvitations, nameof(campaignRewardHighTierInvitations));
            NonNegative(campaignRewardProtectionCharms, nameof(campaignRewardProtectionCharms));
            NonNegative(weeklyRewardProtectionCharms, nameof(weeklyRewardProtectionCharms));
            NonNegative(protectionCharmOfferPrice, nameof(protectionCharmOfferPrice));
            NonNegative(protectionCharmExpectedAvoidedLoss, nameof(protectionCharmExpectedAvoidedLoss));
            Positive(maximumProtectionCharmFulfillment, nameof(maximumProtectionCharmFulfillment));

            DailyMedicineGoldTarget = dailyMedicineGoldTarget;
            DailyStockOperationTarget = dailyStockOperationTarget;
            DailyLiquorUnitTarget = dailyLiquorUnitTarget;
            WeeklyGeneConfiscationTarget = weeklyGeneConfiscationTarget;
            WeeklyProtectionCharmSaleTarget = weeklyProtectionCharmSaleTarget;
            DailyPeriodDays = dailyPeriodDays;
            WeeklyPeriodDays = weeklyPeriodDays;
            DailyRewardGold = dailyRewardGold;
            DailyRewardGems = dailyRewardGems;
            WeeklyRewardGold = weeklyRewardGold;
            WeeklyRewardGems = weeklyRewardGems;
            CampaignRewardGold = campaignRewardGold;
            CampaignRewardGems = campaignRewardGems;
            CampaignRewardBuildingPermits = campaignRewardBuildingPermits;
            CampaignRewardHighTierInvitations = campaignRewardHighTierInvitations;
            CampaignRewardProtectionCharms = campaignRewardProtectionCharms;
            WeeklyRewardProtectionCharms = weeklyRewardProtectionCharms;
            ProtectionCharmOfferPrice = protectionCharmOfferPrice;
            ProtectionCharmExpectedAvoidedLoss = protectionCharmExpectedAvoidedLoss;
            MaximumProtectionCharmFulfillment = maximumProtectionCharmFulfillment;
            BalanceParameters = Array.AsReadOnly(BuildBalanceParameters().ToArray());
        }

        IEnumerable<BalanceParameter> BuildBalanceParameters()
        {
            const string gddTargets = "docs/designs/08_Quests_Achievements_Collections.md §1 and docs/designs/13_Balance_Parameters.md §7: objective value is a placeholder.";
            const string gddRewards = "docs/designs/08_Quests_Achievements_Collections.md §1: sponsor rewards are required; quantity is unspecified.";
            yield return P("quest.campaign.exploit_foundation.requirements", CampaignFoundationRequirementCount, "requirements/quest", "Locked", "docs/designs/08_Quests_Achievements_Collections.md §1A: Zone 2 and Veterinary Hospital level 2 are conjunctive requirements.");
            yield return P("quest.campaign.exploit_foundation.zone_number", CampaignFoundationZoneNumber, "zone number", "Locked", "docs/designs/08_Quests_Achievements_Collections.md §1A: Nền Móng Bóc Lột requires Zone 2.");
            yield return P("quest.campaign.exploit_foundation.hospital_level", CampaignFoundationHospitalLevel, "facility level", "Locked", "docs/designs/08_Quests_Achievements_Collections.md §1A: Nền Móng Bóc Lột requires Veterinary Hospital level 2.");
            yield return P("quest.campaign.new_class.requirements", 1, "requirements/quest", "Locked", "docs/designs/08_Quests_Achievements_Collections.md §1A: Giai Cấp Mới requires Epic Trainer recruitment; content remains unavailable until recruitment support exists.");
            yield return P("quest.daily.medicine_gold_target", DailyMedicineGoldTarget, "Gold/day", "Prototype", gddTargets);
            yield return P("quest.daily.stock_operations", DailyStockOperationTarget, "settled director stock operations/day", "Prototype", gddTargets);
            yield return P("quest.daily.liquor_units", DailyLiquorUnitTarget, "units/day", "Prototype", gddTargets);
            yield return P("quest.weekly.gene_confiscations", WeeklyGeneConfiscationTarget, "monsters/week", "Prototype", gddTargets);
            yield return P("quest.weekly.protection_charm_units", WeeklyProtectionCharmSaleTarget, "units/week", "Prototype", gddTargets);
            yield return P("quest.daily.period_days", DailyPeriodDays, "in-game days", "Prototype", "docs/designs/08_Quests_Achievements_Collections.md §1: daily reset cadence; absolute time anchor is a simulator convention.");
            yield return P("quest.weekly.period_days", WeeklyPeriodDays, "in-game days", "Prototype", "docs/designs/08_Quests_Achievements_Collections.md §1: weekly reset cadence; seven-day length and epoch anchor are simulator conventions.");
            yield return P("quest.reward.daily.gold", DailyRewardGold, "Gold", "TBD", gddRewards);
            yield return P("quest.reward.daily.gems", DailyRewardGems, "Gems", "TBD", gddRewards);
            yield return P("quest.reward.weekly.gold", WeeklyRewardGold, "Gold", "TBD", gddRewards);
            yield return P("quest.reward.weekly.gems", WeeklyRewardGems, "Gems", "TBD", gddRewards);
            yield return P("quest.reward.campaign.gold", CampaignRewardGold, "Gold", "TBD", gddRewards);
            yield return P("quest.reward.campaign.gems", CampaignRewardGems, "Gems", "TBD", gddRewards);
            yield return P("quest.reward.campaign.building_permits", CampaignRewardBuildingPermits, "permits", "TBD", "docs/designs/08_Quests_Achievements_Collections.md §1A: building permits are a reward; quantity is unspecified.");
            yield return P("quest.reward.campaign.high_tier_invitations", CampaignRewardHighTierInvitations, "invitations", "TBD", "docs/designs/08_Quests_Achievements_Collections.md §1A: high-tier invitations are a reward; quantity is unspecified.");
            yield return P("quest.reward.campaign.protection_charms", CampaignRewardProtectionCharms, "Protection Charms", "Prototype", "docs/designs/07_Monetization_Model.md: Quest is a free Protection Charm source.");
            yield return P("quest.reward.weekly.protection_charms", WeeklyRewardProtectionCharms, "Protection Charms", "Prototype", "docs/designs/07_Monetization_Model.md: weekly KPI is a free Protection Charm source.");
            yield return P("quest.protection_charm.offer_price", ProtectionCharmOfferPrice, "Gold/Charm", "Prototype", "docs/designs/07_Monetization_Model.md: Director sets Trainer sale price; starting estimate matches product.price.protection_charm in ConsumablePriceConfig.");
            yield return P("quest.protection_charm.expected_avoided_loss", ProtectionCharmExpectedAvoidedLoss, "Gold", "TBD", "docs/designs/07_Monetization_Model.md: AI buys below expected enhancement loss; expected value is unspecified.");
            yield return P("quest.protection_charm.maximum_fulfillment", MaximumProtectionCharmFulfillment, "Charms/call", "Prototype", "Provider/reward fulfillment bound for deterministic simulation; GDD gives no stock batch limit.");
        }

        static BalanceParameter P(string id, double value, string unit, string status, string source)
            => new BalanceParameter(id, value, unit, status, source);

        static void Positive(long value, string name)
        { if (value <= 0) throw new ArgumentOutOfRangeException(name); }
        static void NonNegative(long value, string name)
        { if (value < 0) throw new ArgumentOutOfRangeException(name); }
    }
}
