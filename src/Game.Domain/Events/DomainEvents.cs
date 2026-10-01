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
}
