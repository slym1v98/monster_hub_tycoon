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
        int BackpackUnits, long ContractWage, long WageOwed, int StrikeDaysLeft, long HubLoanBalance, long ReverseLoanBalance,
        int ReverseLoanPaydaysRemaining, bool ReverseLoanOverdue,
        System.Collections.Generic.IReadOnlyList<MonsterView> Monsters, string CurrentZoneId, int Rank, int Level,
        System.Collections.Generic.IReadOnlyDictionary<Game.Domain.Materials.ProductId, int> Products);

    public sealed record MonsterView(string Id, string SpeciesId, Game.Domain.Monsters.MonsterElement Element,
        int Level, long CurrentHp, long MaxHp, Game.Domain.Monsters.MonsterLifeState LifeState, bool IsActive,
        Rarity Rarity = Rarity.Common, Game.Domain.Monsters.MonsterIvGrade? KnownIv = null,
        Game.Domain.Monsters.MonsterCustody Custody = Game.Domain.Monsters.MonsterCustody.Trainer,
        bool IsSoulBound = false);

    public sealed record ZoneView(string Id, string DisplayName, int MinimumRank, int WalkMinutes, bool IsUnlocked);
    public sealed record MonsterRecoveryView(int RecoveryId, int TrainerId, MonsterView Monster, int CompleteAtMinute,
        long Fee, bool IsCapturedMonster);
    public sealed record VeterinaryHospitalView(int RecoveryBedCapacity, int EmergencyBedCapacity,
        int OccupiedRecoveryBeds, int OccupiedEmergencyBeds,
        System.Collections.Generic.IReadOnlyList<MonsterRecoveryView> Recoveries);
    public sealed record BankMonsterView(int TrainerId, MonsterView Monster);
    public sealed record GeneBankView(int Capacity, int Count, int ConfiscatedCount,
        System.Collections.Generic.IReadOnlyList<BankMonsterView> StoredMonsters,
        System.Collections.Generic.IReadOnlyList<MonsterView> ConfiscatedMonsters);

    public sealed record MonsterRosterChanged(int Minute, int TrainerId, string MonsterId, string Transition,
        string ActiveMonsterId) : IDomainEvent;
    public sealed record MonsterAppraised(int Minute, int TrainerId, string MonsterId,
        Game.Domain.Monsters.MonsterIvGrade Iv, long PricePaid) : IDomainEvent;
    public sealed record MonsterDismantled(int Minute, int TrainerId, string MonsterId, int GeneFragments) : IDomainEvent;
    public sealed record MonsterUpgradeResolved(int Minute, int TrainerId, string MonsterId, bool Success,
        double Chance, double? Roll, bool ProtectionApplied, Rarity ResultRarity) : IDomainEvent;
    public sealed record MonsterEvolutionResolved(int Minute, int TrainerId, string MonsterId, string BranchId,
        bool Success, double Chance, double? Roll, bool ProtectionApplied, string ResultSpeciesId) : IDomainEvent;
    public sealed record MonsterCaptureResolved(int Minute, int TrainerId, string MonsterId, bool Success,
        double Chance, double? Roll, int BallsConsumed, int TrapsConsumed) : IDomainEvent;
    public sealed record ZoneUnlocked(int Minute, string ZoneId) : IDomainEvent;
    public sealed record ExpeditionCompleted(int Minute, int TrainerId, string ZoneId,
        System.Collections.Generic.IReadOnlyList<Game.Domain.Combat.BattleResult> Battles,
        System.Collections.Generic.IReadOnlyList<MaterialQuantity> Collected,
        System.Collections.Generic.IReadOnlyList<MaterialQuantity> Dropped,
        long GoldGained, long ExperienceGained, int TrainerLevel) : IDomainEvent;

    /// <summary>Ảnh chụp chỉ đọc của một công trình dịch vụ cho UI.</summary>
    public sealed record BuildingView(
        BuildingKind Kind, int Level, int Slots, int Occupied, int QueueLength, int MaxQueueLength,
        long Price, long FairPrice, bool Maintained);

    public sealed record MaterialStockView(string ItemId, int Available, int Reserved, int InProduction);
    public sealed record BuyRequestView(string MaterialId, int TargetStock, long BidPrice, bool Enabled, int Deficit);
    public sealed record ProductBuyRequestView(string ProductId, int TargetStock, long BidPrice, bool Enabled, int Deficit);
    public sealed record MerchantView(string Id, Game.Domain.Supply.MerchantState State, long Cash, int LoadUnits, int CapacityUnits);
    public sealed record ProductionJobView(long Id, string RecipeId, int StartMinute, int FinishMinute, string State);
    public sealed record TrainerProductChanged(int Minute, int TrainerId, string ProductId, int Quantity, int NewCount) : IDomainEvent;
    public sealed record GearOffered(int Minute, int TrainerId, string SlotId, string CurrentItemId, string OfferedItemId, long Price, double CurrentScore, double OfferedScore, bool Accepted) : IDomainEvent;
    public sealed record GearEnhanced(int Minute, int TrainerId, string ItemId, int OldLevel, int NewLevel, bool Success, bool Broke, bool CharmUsed, long GoldSpent, int StoneSpent) : IDomainEvent;
    public sealed record GearStarUp(int Minute, int TrainerId, string ItemId, int OldStars, int NewStars, string JunkId, bool Success, int StarsLost, long GoldSpent) : IDomainEvent;
    public sealed record GearRefined(int Minute, int TrainerId, string ItemId, Game.Domain.Gear.GearRefineGrade OldGrade, Game.Domain.Gear.GearRefineGrade NewGrade, int CrystalConsumed, int WaterConsumed, bool Success, long GoldSpent) : IDomainEvent;
    public sealed record GearRepaired(int Minute, int TrainerId, string ItemId, int OldDurability, int NewDurability, long GoldSpent, bool Success) : IDomainEvent;
    public sealed record GearDurabilityChanged(int Minute, int TrainerId, string ItemId, int OldDurability, int NewDurability, string Cause) : IDomainEvent;
    public sealed record GearBuyback(int Minute, int TrainerId, string ItemId, long SalePrice, long BuybackPrice, bool Success) : IDomainEvent;
    public sealed record GearFodderPurchased(int Minute, int TrainerId, string ItemId, string SlotId, long Price, bool Success) : IDomainEvent;
    public sealed record ProductPurchased(int Minute, int TrainerId, string ProductId, int Units, long UnitPrice, long TotalPaid) : IDomainEvent;
    public sealed record GeneBankFeeSettled(int Minute, int TrainerId, long Assessed, long Paid, long Unpaid, string ConfiscatedMonsterId) : IDomainEvent;
    public sealed record TrainerLoanBalanceChanged(int Minute, int TrainerId, long OldBalance, long NewBalance, long Amount, string Reason) : IDomainEvent;
    public sealed record TrainerLoanRepaid(int Minute, int TrainerId, long Amount, string IncomeSource) : IDomainEvent;
    public sealed record TrainerLoanOverdueStrike(int Minute, int TrainerId, long Balance, long Limit, int ConsecutivePaydays) : IDomainEvent;
    public sealed record TrainerLoanPolicyChanged(int Minute, double PreviousRate, double NewRate) : IDomainEvent;
    public sealed record StockExchangeLevelChanged(int Minute, int OldLevel, int NewLevel) : IDomainEvent;
    public sealed record StockCompanyIpo(int Minute, string CompanyId, double OpeningPrice, long TotalShares, long HubLockedShares, long FloatShares) : IDomainEvent;
    public sealed record StockPriceChanged(int Minute, string CompanyId, double OldPrice, double NewPrice, double TrafficChange, double NetOrderFraction) : IDomainEvent;
    public sealed record StockTradeSettled(int Minute, string CompanyId, int BuyerId, int SellerId, long Shares,
        long Gross, long Fee, long RealizedProfit, long Tax, long BuyerTotal, long SellerNet, string TradeType) : IDomainEvent;
    public sealed record StockDividendPaid(int Minute, string CompanyId, int TrainerId, long Shares, long Gross, bool Paid) : IDomainEvent;
    public sealed record StockDailyRevenue(int Minute, string CompanyId, long Revenue, int DayNumber) : IDomainEvent;
    public sealed record ReverseLoanBalanceChanged(int Minute, int LenderTrainerId, long OldBalance, long NewBalance, long Amount, string Reason) : IDomainEvent;
    public sealed record ReverseLoanServiceOffset(int Minute, int TrainerId, BuildingKind Building, long ServiceValue, long BalanceRemaining) : IDomainEvent;
}

namespace Game.Domain
{
    /// <summary>Ảnh chụp bất biến của một món trang bị cho UI.</summary>
    public sealed record GearView(string OwnerId, string ItemId, string SlotId, int Tier, int EnhanceLevel, int Stars,
        Game.Domain.Gear.GearRefineGrade Refine, int Durability, int MaxDurability, string SetId, bool IsBroken)
    {
        internal static GearView From(Game.Domain.Gear.GearItem item, string ownerId) => new GearView(ownerId, item.Id, item.Slot.Id, item.Tier,
            item.EnhanceLevel, item.Stars, item.Refine, item.Durability, item.MaxDurability, item.SetId, item.IsBroken);
    }
}
