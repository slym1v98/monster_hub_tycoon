using System;
using Game.Domain.Production;
using Game.Domain.Supply;

namespace Game.Domain
{
    /// <summary>Bốn công trình dịch vụ của sub-project 1. Giá trị số dùng làm chỉ số mảng.</summary>
    public enum BuildingKind { Inn = 0, Restaurant = 1, Bar = 2, Hospital = 3 }

    /// <summary>Thông số cố định của một loại công trình dịch vụ.</summary>
    public sealed class BuildingSpec
    {
        public readonly BuildingKind Kind;
        /// <summary>Giá hợp lý. Riêng Bệnh Viện: giá trên mỗi 10 HP (14 tương đương 1.4 Gold/HP).</summary>
        public readonly long FairPrice;
        public readonly int ServiceMinutes;

        public BuildingSpec(BuildingKind kind, long fairPrice, int serviceMinutes)
        {
            Kind = kind; FairPrice = fairPrice; ServiceMinutes = serviceMinutes;
        }
    }

    /// <summary>
    /// Mọi tham số khởi điểm của mô phỏng. Quy đổi từ docs/designs/13 §8 (theo ngày) sang phút;
    /// là giá trị tạm, chỉnh khi cân bằng. Sau này nạp từ ScriptableObject.
    /// </summary>
    public sealed class SimConfig
    {
        // --- Khởi tạo ---
        public int TrainerCount = 10;
        /// <summary>Nếu có giá trị thì mọi Trainer dùng tính cách này (tiện cho test).</summary>
        public Personality? ForcedPersonality = null;
        /// <summary>Nếu true thì mọi Trainer bắt đầu với kính nhìn đêm (tiện cho test farm ban đêm).</summary>
        public bool StartWithNightVision = false;
        public long StartTreasury = 20000;
        public long StartTrainerGold = 200;
        public int StartBuildingLevel = 5;
        /// <summary>Phút bắt đầu: 06:00 sáng ngày đầu tiên.</summary>
        public int StartMinute = SimClock.DawnMinute;

        // --- Nhu cầu (mỗi giờ) ---
        public double FieldStaminaPerHour = 6, FieldSatietyPerHour = 5, FieldHydrationPerHour = 6, FieldStressPerHour = 0.5;
        public double HubStaminaPerHour = 2, HubSatietyPerHour = 2, HubHydrationPerHour = 2;
        /// <summary>Thanh dưới mức này thì cần dịch vụ.</summary>
        public double SufficientNeed = 60;
        /// <summary>Ban đêm không có kính: chỉ vào Nhà Trọ khi Thể lực dưới mức này.</summary>
        public double NightSleepBelow = 90;
        public double BarStressThreshold = 70;
        public double BarStressTarget = 20;
        public double QueueStressPerHour = 2;
        public double WaitStressPerHour = 2;
        /// <summary>Stress cộng thêm = hệ số x (giá/giá hợp lý - 1) x độ nhạy giá.</summary>
        public double PriceStressFactor = 10;

        // --- Farm và chợ (tạm, sub-project 2 và 3 thay thế) ---
        public int FarmChunkMinutes = 30;
        public int ZoneTravelMinutes = 30;
        public int FarmMaterialsPerChunk = 3;
        public long FarmGoldPerChunk = 2;
        public long FarmHpLostPerChunk = 3;
        public int BackpackCapacity = 30;
        public long TeamHpMax = 300;
        public long MaterialPrice = 10;
        public double TaxRate = 0.20;
        public MerchantConfig MerchantSettings = MerchantConfig.Prototype;
        public ProductionConfig ProductionSettings = new ProductionConfig();

        // --- Dịch vụ ---
        public double ServiceCogs = 0.25;
        public long UpkeepPerBuildingPerDay = 50;
        public BuildingSpec[] Buildings =
        {
            new BuildingSpec(BuildingKind.Inn, 45, 360),
            new BuildingSpec(BuildingKind.Restaurant, 40, 30),
            new BuildingSpec(BuildingKind.Bar, 800, 60),
            new BuildingSpec(BuildingKind.Hospital, 14, 30),
        };

        // --- Hết tiền ---
        public double PatronChancePerHour = 0.10;
        public int PatronGuaranteedAfterHours = 24;

        // --- Lương và vỡ nợ ---
        public long BaseWage = 2900;
        public double RarityGrowth = 1.7;
        public double CapitalistWageMultiplier = 1.3;
        public int StrikeDays = 5;
        public double StressOnSecondMiss = 30;
        public double DebtModeFarmMultiplier = 0.5;

        public static SimConfig Default => new SimConfig();

        /// <summary>Lương hợp đồng khởi điểm: BaseWage x RarityGrowth^bậc, Tư bản đòi thêm.</summary>
        public long ContractWageFor(Rarity rarity, Personality personality)
        {
            double wage = BaseWage * Math.Pow(RarityGrowth, (int)rarity);
            if (personality == Personality.Capitalist) wage *= CapitalistWageMultiplier;
            return (long)Math.Round(wage);
        }
    }
}
