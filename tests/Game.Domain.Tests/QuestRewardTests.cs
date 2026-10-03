using System.Collections.Generic;
using System.Linq;
using Game.Domain;
using Game.Domain.Materials;
using Xunit;

public sealed class QuestRewardTests
{
    [Fact]
    public void DailyKpiSettlesGoldAndWalletRewardsOnceInDeterministicEventOrder()
    {
        var world = World(startTreasury: 100, rewardGold: 25);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        Assert.True(world.PurchaseProduct(0, "liquor", 2).Ok);
        Assert.True(world.PurchaseProduct(0, "liquor", 1).Ok);

        Assert.Equal(155, world.Treasury);
        Assert.Equal(new QuestRewardWalletView(2, 0, 0, 0), world.Quests.RewardWallet);
        var kpi = world.Quests.DailyKpis.Single(x => x.Id == "kpi.daily.soften_liquor");
        Assert.True(kpi.IsComplete);
        Assert.True(kpi.IsRewardGranted);
        Assert.Single(events.OfType<QuestRewardGranted>(), x => x.ObjectiveId == kpi.Id);
        Assert.True(events.FindIndex(x => x is ProductPurchased) < events.FindIndex(x => x is QuestProgressChanged));
        Assert.True(events.FindIndex(x => x is QuestCompleted) < events.FindIndex(x => x is QuestRewardGranted));
    }

    [Fact]
    public void TreasuryOverflowRejectsWholeRewardWithoutPartialWalletMutation()
    {
        var world = World(startTreasury: long.MaxValue - 20, rewardGold: 1);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        Assert.True(world.PurchaseProduct(0, "liquor", 2).Ok);

        Assert.Equal(long.MaxValue, world.Treasury);
        Assert.Equal(new QuestRewardWalletView(0, 0, 0, 0), world.Quests.RewardWallet);
        Assert.Contains(events, x => x is QuestRewardRejected rejected && rejected.Reason == "quest.reward.balance_overflow");
        Assert.DoesNotContain(events, x => x is QuestRewardGranted);
        Assert.False(world.Quests.DailyKpis.Single(x => x.Id == "kpi.daily.soften_liquor").IsRewardGranted);
    }

    [Fact]
    public void RejectedPurchaseDoesNotCompleteOrGrantKpi()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1 }.WithServiceFacilities(), 612);
        var result = world.PurchaseProduct(0, "liquor", 1);

        Assert.False(result.Ok);
        Assert.Equal(0, world.Quests.DailyKpis.Single(x => x.Id == "kpi.daily.soften_liquor").Progress);
        Assert.Equal(new QuestRewardWalletView(0, 0, 0, 0), world.Quests.RewardWallet);
    }

    [Fact]
    public void AlreadySatisfiedCampaignStateAtWorldStartIsCompletedAndRewarded()
    {
        var config = new SimConfig
        {
            TrainerCount = 0,
            StartBuildingLevel = 2,
            UnlockedZoneIds = new[] { "zone_1", "zone_2" },
            QuestSettings = new HubQuestConfig(campaignRewardGold: 50, campaignRewardBuildingPermits: 2)
        }.WithServiceFacilities();
        var world = new HubWorld(config, 700);

        var campaign = world.Quests.CampaignQuests.Single(x => x.Id == "campaign.exploit_foundation");

        Assert.True(campaign.IsComplete);
        Assert.True(campaign.IsRewardGranted);
        Assert.Equal(new QuestRewardWalletView(0, 2, 1, 1), world.Quests.RewardWallet);
    }

    [Fact]
    public void CampaignRewardRequiresBothZoneAndHospitalRequirements()
    {
        var config = new SimConfig
        {
            TrainerCount = 7,
            StartingTrainerRanks = Enumerable.Repeat(2, 7).ToArray(),
            StartTownHallLevel = 11,
            StartDormitoryLevel = 2,
            StartTreasury = 100_000,
            UnlockedZoneIds = new[] { "zone_1" },
            StartingConstructionStock = new Dictionary<ProductId, int>
            {
                [new ProductId("wood_ingot")] = 2,
                [new ProductId("stone_ingot")] = 2,
                [new ProductId("iron_ingot")] = 2
            },
            QuestSettings = new HubQuestConfig(campaignRewardGold: 50,
                campaignRewardGems: 3, campaignRewardBuildingPermits: 2,
                campaignRewardHighTierInvitations: 4, campaignRewardProtectionCharms: 5)
        }.WithServiceFacilities();
        var world = new HubWorld(config, 818);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        Assert.True(world.UnlockZone("zone_2").Ok);
        Assert.False(world.Quests.CampaignQuests.Single(x => x.Id == "campaign.exploit_foundation").IsComplete);
        Assert.True(world.UpgradeFacility("veterinary_hospital").Ok);
        world.RunFor(config.HubProgressionSettings.FacilityUpgradeMinutes);

        var campaign = world.Quests.CampaignQuests.Single(x => x.Id == "campaign.exploit_foundation");
        Assert.True(campaign.IsComplete);
        Assert.True(campaign.IsRewardGranted);
        Assert.Equal(new QuestRewardWalletView(3, 2, 4, 5), world.Quests.RewardWallet);
        Assert.Contains(events, x => x is TreasuryChanged changed && changed.Reason == "QuestSponsorReward" && changed.Delta == 50);
        Assert.Single(events.OfType<QuestRewardGranted>(), x => x.ObjectiveId == campaign.Id);
    }

    static HubWorld World(long startTreasury, long rewardGold)
    {
        var config = new SimConfig
        {
            TrainerCount = 1,
            StartTreasury = startTreasury,
            StartTrainerGold = 100,
            StartingProductStock = new Dictionary<ProductId, int> { [new ProductId("liquor")] = 10 },
            QuestSettings = new HubQuestConfig(dailyLiquorUnitTarget: 2,
                dailyRewardGold: rewardGold, dailyRewardGems: 2)
        }.WithServiceFacilities();
        return new HubWorld(config, 611);
    }
}
