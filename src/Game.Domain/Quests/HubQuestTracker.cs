using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain
{
    public sealed record QuestObjectiveView(string Id, long Progress, long Target, int PeriodIndex,
        bool IsComplete, bool IsAvailable, string AvailabilityReason, bool IsRewardGranted);

    public sealed record QuestView(IReadOnlyList<QuestObjectiveView> CampaignQuests,
        IReadOnlyList<QuestObjectiveView> DailyKpis, IReadOnlyList<QuestObjectiveView> WeeklyKpis,
        QuestRewardWalletView RewardWallet);
    public sealed record QuestRewardWalletView(long Gems, long BuildingPermits, long HighTierInvitations,
        long ProtectionCharms);

    public sealed record QuestProgressChanged(int Minute, string ObjectiveId, long PreviousProgress,
        long CurrentProgress, long Target, int PeriodIndex) : IDomainEvent;
    public sealed record QuestCompleted(int Minute, string ObjectiveId, int PeriodIndex) : IDomainEvent;
    public sealed record QuestRewardGranted(int Minute, string ObjectiveId, long Gold, int Gems,
        int BuildingPermits, int HighTierInvitations, int ProtectionCharms, int PeriodIndex) : IDomainEvent;
    public sealed record QuestRewardRejected(int Minute, string ObjectiveId, int PeriodIndex, string Reason) : IDomainEvent;

    /// <summary>Deterministic quest state. Only settled Domain facts can advance progress.</summary>
    public sealed class HubQuestTracker
    {
        readonly HubQuestConfig config;
        readonly Dictionary<string, ObjectiveState> campaign = new Dictionary<string, ObjectiveState>(StringComparer.Ordinal);
        readonly Dictionary<string, ObjectiveState> daily = new Dictionary<string, ObjectiveState>(StringComparer.Ordinal);
        readonly Dictionary<string, ObjectiveState> weekly = new Dictionary<string, ObjectiveState>(StringComparer.Ordinal);
        int dailyPeriod = -1;
        int weeklyPeriod = -1;
        QuestRewardWalletView rewardWallet = new QuestRewardWalletView(0, 0, 0, 0);

        public event Action<IDomainEvent> EventProduced;
        public QuestView View => new QuestView(Snapshot(campaign), Snapshot(daily), Snapshot(weekly), rewardWallet);
        public QuestRewardWalletView RewardWallet => rewardWallet;

        public HubQuestTracker(HubQuestConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            foreach (var q in HubQuestCatalog.Default.CampaignQuests)
                campaign.Add(q.Id, new ObjectiveState(q.Id, q.IsAvailable, q.AvailabilityReason,
                    q.Id == "campaign.exploit_foundation" ? config.CampaignFoundationRequirementCount : 1, 0));
            foreach (var kpi in HubQuestCatalog.Default.DailyKpis)
                daily.Add(kpi.Id, new ObjectiveState(kpi.Id, true, "", DailyTarget(kpi.Id), 0));
            foreach (var kpi in HubQuestCatalog.Default.WeeklyKpis)
                weekly.Add(kpi.Id, new ObjectiveState(kpi.Id, true, "", WeeklyTarget(kpi.Id), 0));
            AdvanceTo(0);
        }

        public void AdvanceTo(int minute)
        {
            if (minute < 0) throw new ArgumentOutOfRangeException(nameof(minute));
            int day = minute / SimClock.MinutesPerDay;
            int nextDaily = day / config.DailyPeriodDays;
            int nextWeekly = day / config.WeeklyPeriodDays;
            if (nextDaily > dailyPeriod)
            {
                dailyPeriod = nextDaily;
                foreach (var item in daily.Values) { item.Progress = 0; item.PeriodIndex = dailyPeriod; item.Completed = false; item.RewardGranted = false; }
            }
            if (nextWeekly > weeklyPeriod)
            {
                weeklyPeriod = nextWeekly;
                foreach (var item in weekly.Values) { item.Progress = 0; item.PeriodIndex = weeklyPeriod; item.Completed = false; item.RewardGranted = false; }
            }
        }

        internal void SeedCampaignState(bool zoneTwoUnlocked, bool hospitalAtLeastLevelTwo, int minute)
        {
            if (zoneTwoUnlocked) SetCampaignBit("campaign.exploit_foundation", 1, minute);
            if (hospitalAtLeastLevelTwo) SetCampaignBit("campaign.exploit_foundation", 2, minute);
        }

        public void Observe(IDomainEvent fact)
        {
            if (fact == null) throw new ArgumentNullException(nameof(fact));
            if (fact is ZoneUnlocked zone && zone.ZoneId == "zone_" + config.CampaignFoundationZoneNumber)
            {
                AdvanceTo(zone.Minute);
                SetCampaignBit("campaign.exploit_foundation", 1, zone.Minute);
            }
            else if (fact is FacilityUpgradeCompleted upgrade && upgrade.FacilityId == "veterinary_hospital" && upgrade.ToLevel >= config.CampaignFoundationHospitalLevel)
            {
                AdvanceTo(upgrade.Minute);
                SetCampaignBit("campaign.exploit_foundation", 2, upgrade.Minute);
            }
            else if (fact is ProductPurchased purchase)
            {
                AdvanceTo(purchase.Minute);
                if (purchase.ProductId == "potion" || purchase.ProductId == "vaccine" || purchase.ProductId == "tranquilizer")
                    AddProgress(daily, "kpi.daily.squeezed_medicine", purchase.TotalPaid, purchase.Minute);
                else if (purchase.ProductId == "liquor")
                    AddProgress(daily, "kpi.daily.soften_liquor", purchase.Units, purchase.Minute);
                else if (purchase.ProductId == "protection_charm")
                    AddProgress(weekly, "kpi.weekly.risk_insurance", purchase.Units, purchase.Minute);
            }
            else if (fact is DirectorStockOperationSettled stockOperation)
            {
                AdvanceTo(stockOperation.Minute);
                AddProgress(daily, "kpi.daily.stock_pump", 1, stockOperation.Minute);
            }
            else if (fact is MonsterConfiscated confiscation)
            {
                AdvanceTo(confiscation.Minute);
                AddProgress(weekly, "kpi.weekly.gene_deadbeat", 1, confiscation.Minute);
            }
        }

        internal bool IsRewardGranted(string id, int periodIndex)
            => FindObjective(id)?.RewardGranted == true;

        internal void MarkRewardGranted(string id, int periodIndex)
        {
            var state = FindObjective(id) ?? throw new ArgumentException("Unknown quest objective.", nameof(id));
            if (state.PeriodIndex != periodIndex || !state.Completed || state.RewardGranted)
                throw new InvalidOperationException("Quest reward is not grantable in the current state.");
            state.RewardGranted = true;
        }

        internal void SetRewardWallet(QuestRewardWalletView wallet)
            => rewardWallet = wallet ?? throw new ArgumentNullException(nameof(wallet));

        internal void ConsumeProtectionCharmEntitlements(int units)
        {
            long consumed = Math.Min(rewardWallet.ProtectionCharms, units);
            rewardWallet = rewardWallet with { ProtectionCharms = rewardWallet.ProtectionCharms - consumed };
        }

        ObjectiveState FindObjective(string id)
            => campaign.TryGetValue(id, out var state) ? state
                : daily.TryGetValue(id, out state) ? state
                : weekly.TryGetValue(id, out state) ? state : null;

        void SetCampaignBit(string id, int bit, int minute)
        {
            var state = campaign[id];
            if (!state.Available || (state.CampaignBits & bit) != 0 || state.Completed) return;
            long previous = state.Progress;
            state.CampaignBits |= bit;
            state.Progress = PopCount(state.CampaignBits);
            Emit(new QuestProgressChanged(minute, id, previous, state.Progress, state.Target, 0));
            if (state.CampaignBits == 3)
            {
                state.Completed = true;
                Emit(new QuestCompleted(minute, id, 0));
            }
        }

        long DailyTarget(string id) => id switch
        {
            "kpi.daily.squeezed_medicine" => config.DailyMedicineGoldTarget,
            "kpi.daily.stock_pump" => config.DailyStockOperationTarget,
            "kpi.daily.soften_liquor" => config.DailyLiquorUnitTarget,
            _ => 1
        };

        long WeeklyTarget(string id) => id switch
        {
            "kpi.weekly.gene_deadbeat" => config.WeeklyGeneConfiscationTarget,
            "kpi.weekly.risk_insurance" => config.WeeklyProtectionCharmSaleTarget,
            _ => 1
        };

        void AddProgress(Dictionary<string, ObjectiveState> states, string id, long amount, int minute)
        {
            if (amount <= 0) return;
            var state = states[id];
            if (state.Completed || state.Progress >= state.Target) return;
            long previous = state.Progress;
            long remaining = state.Target - state.Progress;
            state.Progress = amount >= remaining ? state.Target : state.Progress + amount;
            Emit(new QuestProgressChanged(minute, id, previous, state.Progress, state.Target, state.PeriodIndex));
            if (state.Progress == state.Target)
            {
                state.Completed = true;
                Emit(new QuestCompleted(minute, id, state.PeriodIndex));
            }
        }

        void Emit(IDomainEvent e) => EventProduced?.Invoke(e);
        static int PopCount(int bits) => ((bits & 1) != 0 ? 1 : 0) + ((bits & 2) != 0 ? 1 : 0);

        static IReadOnlyList<QuestObjectiveView> Snapshot(Dictionary<string, ObjectiveState> states)
            => Array.AsReadOnly(states.Values.OrderBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new QuestObjectiveView(x.Id, x.Progress, x.Target, x.PeriodIndex,
                    x.Completed, x.Available, x.AvailabilityReason, x.RewardGranted)).ToArray());

        sealed class ObjectiveState
        {
            public string Id { get; }
            public bool Available { get; }
            public string AvailabilityReason { get; }
            public long Target { get; }
            public long Progress;
            public int CampaignBits;
            public int PeriodIndex;
            public bool Completed;
            public bool RewardGranted;
            public ObjectiveState(string id, bool available, string reason, long target, int period)
            { Id = id; Available = available; AvailabilityReason = reason; Target = target; PeriodIndex = period; }
        }
    }
}
