using System;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        void OnQuestTrackerEvent(IDomainEvent e)
        {
            EventRaised?.Invoke(e);
            if (e is QuestCompleted completed) SettleQuestReward(completed);
        }

        void SettleQuestReward(QuestCompleted completed)
        {
            if (questTracker.IsRewardGranted(completed.ObjectiveId, completed.PeriodIndex)) return;
            long gold;
            int gems, permits, invitations, charms;
            switch (completed.ObjectiveId)
            {
                case "campaign.exploit_foundation":
                    gold = questConfig.CampaignRewardGold;
                    gems = questConfig.CampaignRewardGems;
                    permits = questConfig.CampaignRewardBuildingPermits;
                    invitations = questConfig.CampaignRewardHighTierInvitations;
                    charms = questConfig.CampaignRewardProtectionCharms;
                    break;
                case "kpi.daily.squeezed_medicine":
                case "kpi.daily.stock_pump":
                case "kpi.daily.soften_liquor":
                    gold = questConfig.DailyRewardGold;
                    gems = questConfig.DailyRewardGems;
                    permits = invitations = charms = 0;
                    break;
                case "kpi.weekly.gene_deadbeat":
                    gold = questConfig.WeeklyRewardGold;
                    gems = questConfig.WeeklyRewardGems;
                    permits = invitations = 0;
                    charms = questConfig.WeeklyRewardProtectionCharms;
                    break;
                case "kpi.weekly.risk_insurance":
                    gold = questConfig.WeeklyRewardGold;
                    gems = questConfig.WeeklyRewardGems;
                    permits = invitations = 0;
                    charms = questConfig.WeeklyRewardProtectionCharms;
                    break;
                default:
                    EventRaised?.Invoke(new QuestRewardRejected(now, completed.ObjectiveId,
                        completed.PeriodIndex, "quest.reward.undefined"));
                    return;
            }

            var wallet = questTracker.RewardWallet;
            QuestRewardWalletView nextWallet;
            try
            {
                _ = checked(treasury.Balance + gold);
                nextWallet = new QuestRewardWalletView(checked(wallet.Gems + gems),
                    checked(wallet.BuildingPermits + permits), checked(wallet.HighTierInvitations + invitations),
                    checked(wallet.ProtectionCharms + charms));
            }
            catch (OverflowException)
            {
                EventRaised?.Invoke(new QuestRewardRejected(now, completed.ObjectiveId,
                    completed.PeriodIndex, "quest.reward.balance_overflow"));
                return;
            }

            if (gold > 0) treasury.Add(gold);
            questTracker.SetRewardWallet(nextWallet);
            questTracker.MarkRewardGranted(completed.ObjectiveId, completed.PeriodIndex);
            if (gold > 0)
                EventRaised?.Invoke(new TreasuryChanged(now, gold, treasury.Balance, "QuestSponsorReward"));
            EventRaised?.Invoke(new QuestRewardGranted(now, completed.ObjectiveId, gold, gems,
                permits, invitations, charms, completed.PeriodIndex));
        }
    }
}
