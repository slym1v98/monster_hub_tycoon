namespace Game.Domain
{
    /// <summary>Sự kiện Domain (C# thuần). Presenter dùng R3 ở lớp Presentation để chuyển thành luồng UI.</summary>
    public interface IDomainEvent
    {
        /// <summary>Phút in-game lúc sự kiện xảy ra.</summary>
        int Minute { get; }
    }

    /// <summary>Trainer đổi trạng thái; <c>Reason</c> là lý do để UI giải thích (ví dụ "Hungry").</summary>
    public sealed record TrainerStateChanged(int Minute, int TrainerId, TrainerState From, TrainerState To, string Reason) : IDomainEvent;

    /// <summary>Một lượt dùng dịch vụ. <c>StressAdded</c> &gt; 0 khi giá Giám đốc đặt cao hơn giá hợp lý.</summary>
    public sealed record ServiceUsed(int Minute, int TrainerId, BuildingKind Building, long Paid, long ListPrice, long FairPrice, double StressAdded) : IDomainEvent;

    public sealed record QueueChanged(int Minute, BuildingKind Building, int QueueLength, int Occupied) : IDomainEvent;

    public sealed record TreasuryChanged(int Minute, long Delta, long Balance, string Reason) : IDomainEvent;

    public sealed record PaydayDue(int Minute, PaydayForecast Forecast) : IDomainEvent;

    public sealed record PaydayResolved(int Minute, PaydayOutcome Outcome) : IDomainEvent;

    public sealed record BuildingMaintenanceChanged(int Minute, BuildingKind Building, bool Maintained) : IDomainEvent;

    /// <summary>Trainer nhận tiền không hoàn lại. <c>Source</c> là "Patron" (Tổng tài) hoặc "Director".</summary>
    public sealed record DonationReceived(int Minute, int TrainerId, long Gold, string Source) : IDomainEvent;

    /// <summary>Sang ngày (IsNight = false) hoặc sang đêm (IsNight = true).</summary>
    public sealed record DayPhaseChanged(int Minute, bool IsNight) : IDomainEvent;

    public sealed record SupplyStockChanged(int Minute, string ItemId, Game.Domain.Supply.InventoryBalance Balance) : IDomainEvent;
    public sealed record BuyRequestChanged(int Minute, string MaterialId, int TargetStock, long BidPrice, bool Enabled) : IDomainEvent;
    public sealed record MaterialTradeSettled(int Minute, int TrainerId, string MaterialId, string Channel,
        int Units, long Gross, long Tax, long NetToSeller) : IDomainEvent;
    public sealed record MerchantStateChanged(int Minute, string MerchantId, Game.Domain.Supply.MerchantState State,
        long Cash, int LoadUnits) : IDomainEvent;
    public sealed record ProductionJobChanged(int Minute, long JobId, string RecipeId, string State, int FinishMinute) : IDomainEvent;
    public sealed record ProductionRestockDemandChanged(int Minute, string ItemId, int Quantity) : IDomainEvent;
}
