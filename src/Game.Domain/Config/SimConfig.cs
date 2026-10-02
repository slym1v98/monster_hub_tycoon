using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Combat;
using Game.Domain.Production;
using Game.Domain.Supply;
using Game.Domain.Monsters;

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

        // --- Chỉ số và tiến trình Prototype của Monster/Trainer ---
        public MonsterStatConfig MonsterStatSettings = MonsterStatConfig.Prototype;
        public TrainerAttributeConfig TrainerAttributeSettings = TrainerAttributeConfig.Prototype;
        public TrainerProgressionConfig TrainerProgressionSettings = TrainerProgressionConfig.Prototype;
        public ZoneCatalog ZoneCatalogSettings = ZoneCatalog.Default;
        public ZoneSelectionConfig ZoneSelectionSettings = ZoneSelectionConfig.Prototype;
        public LootConfig LootSettings = LootConfig.Prototype;
        public ExpeditionConfig ExpeditionSettings = ExpeditionConfig.Prototype;
        public ConsumablePriceConfig ConsumablePrices = ConsumablePriceConfig.Prototype;
        public ConsumablePolicyConfig ConsumablePolicySettings = ConsumablePolicyConfig.Prototype;
        public MonsterItemConfig MonsterItemSettings = MonsterItemConfig.Prototype;
        public VeterinaryHospitalConfig VeterinaryHospitalSettings = VeterinaryHospitalConfig.Prototype;
        public GeneBankConfig GeneBankSettings = GeneBankConfig.Prototype;
        /// <summary>Progression unlock is owned by Sub-project 6; default campaign begins in Zone 1.</summary>
        public string[] UnlockedZoneIds = { "zone_1" };

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

        // --- Expedition timing, backpack and market ---
        public int FarmChunkMinutes = 30;
        public int ZoneTravelMinutes = 30;
        public int BackpackCapacity = 30;
        public long StarterMonsterHp = 300;
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

    /// <summary>Hệ số chỉ số Prototype; IV theo GDD cố định và không thuộc cấu hình này.</summary>
    public sealed class MonsterStatConfig
    {
        public const string BalanceStatus = "Prototype";
        public IReadOnlyList<double> RarityMultipliers { get; }
        public double GrowthFactor { get; }
        public static MonsterStatConfig Prototype { get; } = new MonsterStatConfig(new[] { 1.0, 1.2, 1.4, 1.6, 1.8 });

        public MonsterStatConfig(IEnumerable<double> rarityMultipliers, double growthFactor = 1)
        {
            var factors = (rarityMultipliers ?? throw new ArgumentNullException(nameof(rarityMultipliers))).ToArray();
            if (factors.Length != 5) throw new ArgumentException("Cần hệ số cho đủ năm bậc Rarity.", nameof(rarityMultipliers));
            foreach (var factor in factors)
                if (double.IsNaN(factor) || double.IsInfinity(factor) || factor <= 0)
                    throw new ArgumentOutOfRangeException(nameof(rarityMultipliers));
            if (double.IsNaN(growthFactor) || double.IsInfinity(growthFactor) || growthFactor < 0)
                throw new ArgumentOutOfRangeException(nameof(growthFactor));
            RarityMultipliers = Array.AsReadOnly(factors);
            GrowthFactor = growthFactor;
        }
    }

    /// <summary>Khoảng sinh chỉ số Prototype liên tục; hai đầu bằng nhau cho giá trị cố định.</summary>
    public sealed class TrainerAttributeRange
    {
        public double Minimum { get; }
        public double Maximum { get; }
        public TrainerAttributeRange(double minimum, double maximum)
        {
            if (double.IsNaN(minimum) || double.IsInfinity(minimum) || minimum < 0)
                throw new ArgumentOutOfRangeException(nameof(minimum));
            if (double.IsNaN(maximum) || double.IsInfinity(maximum) || maximum < minimum)
                throw new ArgumentOutOfRangeException(nameof(maximum));
            Minimum = minimum;
            Maximum = maximum;
        }
    }

    /// <summary>Bốn khoảng chỉ số Prototype; chưa nối hiệu ứng nhu cầu hoặc chiến đấu ở tác vụ này.</summary>
    public sealed class TrainerAttributeConfig
    {
        public const string BalanceStatus = "Prototype";
        public TrainerAttributeRange Dexterity { get; }
        public TrainerAttributeRange Luck { get; }
        public TrainerAttributeRange Endurance { get; }
        public TrainerAttributeRange Leadership { get; }
        public static TrainerAttributeConfig Prototype { get; } = new TrainerAttributeConfig(
            new TrainerAttributeRange(8, 12), new TrainerAttributeRange(8, 12),
            new TrainerAttributeRange(90, 110), new TrainerAttributeRange(18, 22));

        public TrainerAttributeConfig(TrainerAttributeRange dexterity, TrainerAttributeRange luck,
            TrainerAttributeRange endurance, TrainerAttributeRange leadership)
        {
            Dexterity = dexterity ?? throw new ArgumentNullException(nameof(dexterity));
            Luck = luck ?? throw new ArgumentNullException(nameof(luck));
            Endurance = endurance ?? throw new ArgumentNullException(nameof(endurance));
            Leadership = leadership ?? throw new ArgumentNullException(nameof(leadership));
        }
    }

    /// <summary>Đường cong PrototypeLinear: EXP lên cấp = cơ sở + bước × (cấp hiện tại − 1).</summary>
    public sealed class TrainerProgressionConfig
    {
        public const string BalanceStatus = "Prototype";
        public string CurveId => "PrototypeLinear";
        public long BaseExperience { get; }
        public long ExperiencePerLevel { get; }
        public static TrainerProgressionConfig Prototype { get; } = new TrainerProgressionConfig();

        public TrainerProgressionConfig(long baseExperience = 100, long experiencePerLevel = 25)
        {
            if (baseExperience <= 0) throw new ArgumentOutOfRangeException(nameof(baseExperience));
            if (experiencePerLevel < 0) throw new ArgumentOutOfRangeException(nameof(experiencePerLevel));
            // Chỉ cần ngưỡng tới Lv100; phép kiểm tra chặn cấu hình gây tràn số trước khi đổi trạng thái.
            _ = checked(baseExperience + 98 * experiencePerLevel);
            BaseExperience = baseExperience;
            ExperiencePerLevel = experiencePerLevel;
        }

        public long ExperienceToNextLevel(int level)
        {
            if (level < 1 || level >= 100) throw new ArgumentOutOfRangeException(nameof(level));
            return checked(BaseExperience + (level - 1) * ExperiencePerLevel);
        }
    }
}
