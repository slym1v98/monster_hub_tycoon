using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class HubCampaignQuestDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public bool IsAvailable { get; }
        public string AvailabilityReason { get; }

        internal HubCampaignQuestDefinition(string id, string title, bool isAvailable, string availabilityReason)
        { Id = id; Title = title; IsAvailable = isAvailable; AvailabilityReason = availabilityReason; }
    }

    public sealed class HubKpiDefinition
    {
        public string Id { get; }
        public string Title { get; }

        internal HubKpiDefinition(string id, string title)
        { Id = id; Title = title; }
    }

    /// <summary>Early Access Quest IDs and display labels in stable simulation order.</summary>
    public sealed class HubQuestCatalog
    {
        static readonly IReadOnlyList<HubCampaignQuestDefinition> campaign = Array.AsReadOnly(new[]
        {
            new HubCampaignQuestDefinition("campaign.exploit_foundation", "Nền Móng Bóc Lột", true, ""),
            new HubCampaignQuestDefinition("campaign.new_class", "Giai Cấp Mới", false, "quest.dependency.trainer_recruitment_unavailable")
        });

        static readonly IReadOnlyList<HubKpiDefinition> daily = Array.AsReadOnly(new[]
        {
            new HubKpiDefinition("kpi.daily.squeezed_medicine", "Vắt Kiệt"),
            new HubKpiDefinition("kpi.daily.stock_pump", "Bơm Thổi"),
            new HubKpiDefinition("kpi.daily.soften_liquor", "Xoa Dịu")
        });

        static readonly IReadOnlyList<HubKpiDefinition> weekly = Array.AsReadOnly(new[]
        {
            new HubKpiDefinition("kpi.weekly.gene_deadbeat", "Tài Liệt"),
            new HubKpiDefinition("kpi.weekly.risk_insurance", "Bảo Hiểm Rủi Ro")
        });

        public static HubQuestCatalog Default { get; } = new HubQuestCatalog();
        public IReadOnlyList<HubCampaignQuestDefinition> CampaignQuests => campaign;
        public IReadOnlyList<HubKpiDefinition> DailyKpis => daily;
        public IReadOnlyList<HubKpiDefinition> WeeklyKpis => weekly;

        HubQuestCatalog() { }
    }
}
