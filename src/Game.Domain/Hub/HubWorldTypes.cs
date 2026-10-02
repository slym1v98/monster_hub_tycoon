namespace Game.Domain
{
    public enum StopReason { Completed, PaydayDue }

    /// <summary>Kết quả một lần chạy mô phỏng: số phút đã chạy, số phút chưa chạy (khi dừng sớm) và lý do dừng.</summary>
    public sealed record RunResult(int MinutesRun, int RemainingMinutes, StopReason Stop);

    /// <summary>Kết quả lệnh của Giám đốc. Lỗi của người chơi trả về Rejected, không ném exception.</summary>
    public sealed record CommandResult(bool Ok, string Reason)
    {
        public static CommandResult Success() => new CommandResult(true, "");
        public static CommandResult Rejected(string reason) => new CommandResult(false, reason);
    }

    /// <summary>Dự báo Payday: "Payday sau X ngày, cần Y Gold, hiện có Z Gold".</summary>
    public sealed record PaydayForecast(int DaysLeft, long WagesDue, long TreasuryBalance);

    /// <summary>Ảnh chụp chỉ đọc của một Trainer cho UI.</summary>
    public sealed record TrainerView(
        int Id, Rarity Rarity, Personality Personality, TrainerState State, string StateReason, long Gold,
        double Stamina, double Satiety, double Hydration, double Stress,
        int BackpackUnits, long ContractWage, long WageOwed, int StrikeDaysLeft,
        System.Collections.Generic.IReadOnlyList<MonsterView> Monsters, string CurrentZoneId, int Rank, int Level,
        System.Collections.Generic.IReadOnlyDictionary<Game.Domain.Materials.ProductId, int> Products);

    public sealed record MonsterView(string Id, string SpeciesId, Game.Domain.Monsters.MonsterElement Element,
        int Level, long CurrentHp, long MaxHp, Game.Domain.Monsters.MonsterLifeState LifeState, bool IsActive);

    /// <summary>Ảnh chụp chỉ đọc của một công trình dịch vụ cho UI.</summary>
    public sealed record BuildingView(
        BuildingKind Kind, int Level, int Slots, int Occupied, int QueueLength, int MaxQueueLength,
        long Price, long FairPrice, bool Maintained);

    public sealed record MaterialStockView(string ItemId, int Available, int Reserved, int InProduction);
    public sealed record BuyRequestView(string MaterialId, int TargetStock, long BidPrice, bool Enabled, int Deficit);
    public sealed record MerchantView(string Id, Game.Domain.Supply.MerchantState State, long Cash, int LoadUnits, int CapacityUnits);
    public sealed record ProductionJobView(long Id, string RecipeId, int StartMinute, int FinishMinute, string State);
    public sealed record TrainerProductChanged(int Minute, int TrainerId, string ProductId, int Quantity, int NewCount) : IDomainEvent;
    public sealed record ProductPurchased(int Minute, int TrainerId, string ProductId, int Units, long UnitPrice, long TotalPaid) : IDomainEvent;
}
