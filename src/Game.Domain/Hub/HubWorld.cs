using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Game.Domain.Production;
using Game.Domain.Supply;

namespace Game.Domain
{
    /// <summary>
    /// Điểm vào duy nhất của Domain: Unity, Game.Sim và test đều gọi lớp này.
    /// Mô phỏng sự kiện rời rạc theo phút in-game; chia thành nhiều file partial theo trách nhiệm.
    /// </summary>
    public sealed partial class HubWorld
    {
        readonly SimConfig cfg;
        readonly SimRandom rng;
        readonly EventQueue queue = new EventQueue();
        readonly TreasuryAccount treasury;
        readonly Payroll payroll = new Payroll();
        readonly List<Trainer> trainers = new List<Trainer>();
        readonly ServiceBuilding[] buildings = new ServiceBuilding[4];
        readonly IExpeditionResolver expeditions;
        readonly IMaterialMarket market;
        readonly bool useSupplyChain;
        readonly MoneyLedger supplyLedger;
        readonly Station station;
        readonly MerchantFleet merchantFleet;
        readonly ProductionController production;
        readonly SupplyChain supplyChain;
        readonly Dictionary<ProductId, ConsumableStall> productStalls = new Dictionary<ProductId, ConsumableStall>();
        readonly Dictionary<MaterialId, BuyRequest> buyRequests = new Dictionary<MaterialId, BuyRequest>();
        readonly Dictionary<string, int> reportedRestockDemands = new Dictionary<string, int>(StringComparer.Ordinal);
        long marketReferencePrice;
        double marketTaxRate;
        int productionEventMinute = -1;

        int now;                 // phút in-game hiện tại
        int paydayIndex;         // số Payday đã xử lý
        bool paydayPending;      // Payday đã tới, đang chờ người chơi xử lý

        /// <summary>Sự kiện Domain (C# thuần). Presenter dùng R3 ở lớp Presentation.</summary>
        public event Action<IDomainEvent> EventRaised;

        public HubWorld(SimConfig config, int seed) : this(config, seed, null, null) { }

        /// <param name="expeditionResolver">Bộ giải expedition; null thì dùng bộ giải Domain mặc định.</param>
        /// <param name="materialMarket">Chợ tương thích cho caller cũ; để null dùng supply chain mặc định.</param>
        public HubWorld(SimConfig config, int seed, IExpeditionResolver expeditionResolver, IMaterialMarket materialMarket)
        {
            cfg = config ?? throw new ArgumentNullException(nameof(config));
            marketReferencePrice = cfg.MaterialPrice;
            marketTaxRate = cfg.TaxRate;
            rng = new SimRandom(seed);
            expeditions = expeditionResolver ?? new DefaultExpeditionResolver(cfg);
            useSupplyChain = materialMarket == null;
            market = materialMarket;
            treasury = new TreasuryAccount(cfg.StartTreasury);
            if (useSupplyChain)
            {
                supplyLedger = new MoneyLedger();
                station = new Station(treasury, cfg.TaxRate, supplyLedger);
                merchantFleet = new MerchantFleet(cfg.MerchantSettings ?? throw new ArgumentNullException(nameof(config), "Thiếu MerchantSettings."), supplyLedger);
                production = new ProductionController(MaterialCatalog.Default, station.Stock, supplyLedger,
                    cfg.ProductionSettings ?? throw new ArgumentNullException(nameof(config), "Thiếu ProductionSettings."), treasury);
                supplyChain = new SupplyChain(supplyLedger, station, merchantFleet, production);
                foreach (var definition in ConsumableStallCatalog.Default.Stalls)
                {
                    var stall = new ConsumableStall(definition, station.Stock, supplyLedger);
                    foreach (var product in definition.Products) productStalls.Add(product, stall);
                }
            }
            now = cfg.StartMinute;

            foreach (BuildingSpec spec in cfg.Buildings)
                buildings[(int)spec.Kind] = new ServiceBuilding(spec, cfg.StartBuildingLevel, cfg.UpkeepPerBuildingPerDay);
            for (int i = 0; i < buildings.Length; i++)
                if (buildings[i] == null) throw new ArgumentException($"SimConfig.Buildings thiếu công trình {(BuildingKind)i}.");

            var starterDefinition = MonsterCatalog.CreateDefault(cfg.StarterMonsterHp).Definitions[0];
            for (int i = 0; i < cfg.TrainerCount; i++)
            {
                Personality personality = cfg.ForcedPersonality ?? (Personality)rng.NextInt(4);
                // Luồng chỉ số riêng theo seed và ID, không làm lệch chuỗi ngẫu nhiên mô phỏng.
                int attributeSeed = unchecked(seed ^ (i * (int)0x9E3779B9u) ^ (int)0xA341316Cu);
                var trainer = new Trainer(new SimRandom(attributeSeed),
                    cfg.TrainerAttributeSettings ?? throw new ArgumentException("Thiếu TrainerAttributeSettings.", nameof(config)))
                {
                    Id = i, Rarity = Rarity.Common, Personality = personality,
                    Gold = cfg.StartTrainerGold,
                    BackpackCapacity = cfg.BackpackCapacity,
                    ContractWage = cfg.ContractWageFor(Rarity.Common, personality),
                    LastSettleMinute = now,
                    HasNightVision = cfg.StartWithNightVision,
                };
                // Trộn seed bằng số nguyên ổn định; không tiêu thụ chuỗi ngẫu nhiên của mô phỏng.
                int monsterSeed = unchecked(seed ^ (i * (int)0x9E3779B9u));
                trainer.Roster.Add(Monster.Create(new MonsterId("trainer_" + i.ToString(CultureInfo.InvariantCulture) + "_starter"),
                    starterDefinition, trainer.Rarity, MonsterIvGrade.B,
                    MonsterProgression.MonsterLevel(trainer.Rank, trainer.Level), monsterSeed, isSoulBound: true,
                    statConfig: cfg.MonsterStatSettings ?? throw new ArgumentException("Thiếu MonsterStatSettings.", nameof(config))));
                trainers.Add(trainer);
            }

            queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DawnMinute), SimEventKind.Dawn);
            queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DuskMinute), SimEventKind.Dusk);
            queue.Schedule(SimClock.NextMinuteOfDay(now, 0), SimEventKind.DayStart);
            queue.Schedule(SimClock.PaydayMinute(0), SimEventKind.PaydayDue);
            if (useSupplyChain) queue.Schedule(now + Math.Max(1, cfg.MerchantSettings.RouteCycleMinutes / 2), SimEventKind.MerchantRouteStep);
            foreach (Trainer t in trainers) queue.Schedule(now, SimEventKind.TrainerDecide, t.Id, t.Token);
        }

        /// <summary>Phút in-game hiện tại.</summary>
        public SimTime Now => new SimTime(now);

        /// <summary>Số dư Kho bạc.</summary>
        public long Treasury => treasury.Balance;

        /// <summary>Thời gian chờ tiền dài nhất (phút) của một Trainer từ đầu đến giờ. Dùng để kiểm tra "không ai kẹt mãi".</summary>
        public int MaxMoneyWaitMinutes { get; private set; }

        /// <summary>
        /// Chạy tối đa <paramref name="minutes"/> phút in-game. Dừng sớm nếu tới Payday; khi đó
        /// <see cref="RunResult.RemainingMinutes"/> là phần chưa chạy (Đồng Hồ Cát giữ lại phần này).
        /// Khi Payday đang chờ xử lý, hàm chạy 0 phút.
        /// </summary>
        public RunResult RunFor(int minutes)
        {
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            if (paydayPending) return new RunResult(0, minutes, StopReason.PaydayDue);

            int startNow = now;
            long end = (long)now + minutes;
            while (queue.Count > 0 && queue.PeekTime <= end)
            {
                SimEvent e = queue.Dequeue();
                now = e.Time;
                Dispatch(e);
                if (paydayPending)
                {
                    int ran = now - startNow;
                    return new RunResult(ran, minutes - ran, StopReason.PaydayDue);
                }
            }
            now = (int)end;
            return new RunResult(minutes, 0, StopReason.Completed);
        }

        /// <summary>Chạy tới Payday gần nhất rồi dừng. Dùng cho tiến trình offline.</summary>
        public RunResult RunUntilPayday()
        {
            if (paydayPending) return new RunResult(0, 0, StopReason.PaydayDue);
            return RunFor(SimClock.PaydayMinute(paydayIndex) - now);
        }

        void Dispatch(SimEvent e)
        {
            Trainer t = null;
            if (e.TrainerId >= 0)
            {
                t = trainers[e.TrainerId];
                if (t.Token != e.Token) return;   // sự kiện cũ, Trainer đã bị ngắt giữa chừng
            }
            switch (e.Kind)
            {
                case SimEventKind.TrainerDecide: OnDecide(t); break;
                case SimEventKind.TrainerArriveZone: OnArriveZone(t); break;
                case SimEventKind.FarmChunk: OnFarmChunk(t); break;
                case SimEventKind.TrainerArriveHub: OnArriveHub(t); break;
                case SimEventKind.ServiceDone: OnServiceDone(t, buildings[e.Arg]); break;
                case SimEventKind.WaitTick: OnWaitTick(t); break;
                case SimEventKind.Dawn: OnDawn(); break;
                case SimEventKind.Dusk: OnDusk(); break;
                case SimEventKind.DayStart: OnDayStart(); break;
                case SimEventKind.PaydayDue: OnPaydayDue(); break;
                case SimEventKind.MerchantRouteStep: OnMerchantRouteStep(); break;
                case SimEventKind.ProductionComplete: OnProductionComplete(); break;
                case SimEventKind.MarketRetry: if (t != null) OnMarketRetry(t); break;
            }
        }

        void Raise(IDomainEvent e) => EventRaised?.Invoke(e);

        /// <summary>Cộng tiền vào Kho bạc và phát sự kiện (bỏ qua khi số tiền bằng 0).</summary>
        void AddTreasury(long amount, string reason)
        {
            if (amount == 0) return;
            treasury.Add(amount);
            Raise(new TreasuryChanged(now, amount, treasury.Balance, reason));
        }

        void OnDawn()
        {
            Raise(new DayPhaseChanged(now, false));
            queue.Schedule(now + SimClock.MinutesPerDay, SimEventKind.Dawn);
        }

        /// <summary>18:00: Trainer đang ở ngoài mà không có kính nhìn đêm phải về HUB.</summary>
        void OnDusk()
        {
            Raise(new DayPhaseChanged(now, true));
            foreach (Trainer t in trainers)
            {
                bool outside = t.State == TrainerState.Traveling || t.State == TrainerState.Farming;
                if (outside && !t.HasNightVision) SendHome(t, ReturnReason.Night);
            }
            queue.Schedule(now + SimClock.MinutesPerDay, SimEventKind.Dusk);
        }

        /// <summary>00:00: trừ chi phí vận hành từng công trình, giảm số ngày đình công.</summary>
        void OnDayStart()
        {
            // Đình công bắt đầu đúng 23:59 ngày Payday nên lần nửa đêm ngay sau đó chưa tính là một ngày đã qua.
            bool firstStrikeMidnight = paydayIndex > 0 && now == SimClock.PaydayMinute(paydayIndex - 1) + 1;
            if (!firstStrikeMidnight)
                foreach (Trainer t in trainers)
                    if (t.StrikeDaysLeft > 0) t.StrikeDaysLeft--;

            foreach (ServiceBuilding b in buildings)
            {
                bool wasMaintained = b.Maintained;
                bool paid = treasury.TrySpend(b.UpkeepPerDay);
                b.Maintained = paid;
                if (paid) Raise(new TreasuryChanged(now, -b.UpkeepPerDay, treasury.Balance, "Upkeep"));
                if (paid != wasMaintained) Raise(new BuildingMaintenanceChanged(now, b.Kind, paid));
                if (paid && !wasMaintained) SeatWaiting(b);   // số chỗ tăng lại: gọi thêm người đang chờ
            }
            queue.Schedule(now + SimClock.MinutesPerDay, SimEventKind.DayStart);
        }
    }
}
