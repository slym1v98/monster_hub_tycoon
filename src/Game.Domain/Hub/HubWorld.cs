using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        readonly HubQuestTracker questTracker;
        readonly HubQuestConfig questConfig;
        readonly Payroll payroll = new Payroll();
        readonly StockExchange stockExchange;
        readonly List<Trainer> trainers = new List<Trainer>();
        readonly VeterinaryHospital veterinaryHospital;
        readonly GeneBank geneBank;
        readonly ServiceBuilding[] buildings = new ServiceBuilding[4];
        readonly int[] buildingRepairFinishMinutes = { -1, -1, -1, -1 };
        readonly HashSet<int> defenseCalledTrainerIds = new HashSet<int>();
        readonly Dictionary<string, HubFacilityRuntimeState> facilityStates = new Dictionary<string, HubFacilityRuntimeState>(StringComparer.Ordinal);
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
        readonly Dictionary<ProductId, ProductBuyRequest> productBuyRequests = new Dictionary<ProductId, ProductBuyRequest>();
        readonly Dictionary<ProductId, long> productPriceOverrides = new Dictionary<ProductId, long>();
        readonly Dictionary<string, int> reportedRestockDemands = new Dictionary<string, int>(StringComparer.Ordinal);
        long marketReferencePrice;
        double marketTaxRate;
        int productionEventMinute = -1;
        int stockAiOperationDepth;
        int townHallLevel;
        int dormitoryLevel;
        int dormitoryUpgradeFinishMinute = -1;
        int townHallUpgradeFinishMinute = -1;
        int inspectionFinishMinute = -1;
        int nextInspectionMinute;
        bool inspectionBribed;
        int monsterFluFinishMinute = -1;
        int monsterFluStartMinute = -1;
        int breedingSeasonStartMinute = -1;
        int breedingSeasonFinishMinute = -1;
        int monsterSiegeFinishMinute = -1;
        int monsterSiegeStartMinute = -1;
        int worldBossFinishMinute = -1;
        int worldBossStartMinute = -1;
        int nextWorldBossMinute;
        double lastReputationScore = double.NaN;

        int now;                 // phút in-game hiện tại
        readonly HashSet<string> unlockedZoneIds = new HashSet<string>(StringComparer.Ordinal);
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
            currentTrainerLoanRate = (cfg.TrainerLoanSettings ?? TrainerLoanConfig.Prototype).InterestPerPayday;
            stockExchange = new StockExchange(unchecked(seed ^ (int)0x51A7C0DE), cfg.StockExchangeSettings ?? StockExchangeConfig.Prototype);
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
                foreach (var item in cfg.StartingConstructionStock ?? new Dictionary<ProductId, int>())
                {
                    if (!new[] { "wood_ingot", "stone_ingot", "iron_ingot" }.Contains(item.Key.Value) || item.Value < 0)
                        throw new ArgumentException("Invalid starting construction stock.", nameof(config));
                    if (item.Value > 0) station.Stock.Add(new InventoryItem(item.Key), item.Value);
                }
                foreach (var item in cfg.StartingProductStock ?? new Dictionary<ProductId, int>())
                {
                    if (!MaterialCatalog.Default.Products.Any(x => x.Id == item.Key) || item.Value < 0)
                        throw new ArgumentException("Invalid starting product stock.", nameof(config));
                    if (item.Value > 0) station.Stock.Add(new InventoryItem(item.Key), item.Value);
                }
                foreach (var definition in ConsumableStallCatalog.Default.Stalls)
                {
                    var stall = new ConsumableStall(definition, station.Stock, supplyLedger);
                    foreach (var product in definition.Products) productStalls.Add(product, stall);
                }
            }
            if (cfg.MonsterStorageSettings == null) throw new ArgumentException("Missing MonsterStorageSettings.", nameof(config));
            now = cfg.StartMinute;
            questConfig = cfg.QuestSettings ?? HubQuestConfig.Prototype;
            questTracker = new HubQuestTracker(questConfig);
            questTracker.AdvanceTo(now);
            questTracker.EventProduced += OnQuestTrackerEvent;
            productPriceOverrides[new ProductId("protection_charm")] = questConfig.ProtectionCharmOfferPrice;
            nextInspectionMinute = now;
            nextWorldBossMinute = now;
            var progression = cfg.HubProgressionSettings ?? throw new ArgumentException("Missing HubProgressionSettings.", nameof(config));
            if (cfg.StartTownHallLevel < 1 || cfg.StartTownHallLevel > progression.TownHallMaxLevel) throw new ArgumentOutOfRangeException(nameof(config), "Town Hall start level is outside progression limits.");
            if (cfg.StartDormitoryLevel < 1 || cfg.StartDormitoryLevel > progression.DormitoryMaxLevel) throw new ArgumentOutOfRangeException(nameof(config), "Dormitory start level is outside progression limits.");
            townHallLevel = cfg.StartTownHallLevel;
            dormitoryLevel = cfg.StartDormitoryLevel;
            if (cfg.StartBuildingLevel > townHallLevel)
                throw new ArgumentException("Service building level cannot exceed Town Hall level.", nameof(config));
            foreach (var id in cfg.UnlockedZoneIds ?? Array.Empty<string>()) unlockedZoneIds.Add(id);

            foreach (BuildingSpec spec in cfg.Buildings)
            {
                string facilityId = spec.Kind == BuildingKind.Inn ? "inn" : spec.Kind == BuildingKind.Restaurant ? "restaurant"
                    : spec.Kind == BuildingKind.Bar ? "bar" : "veterinary_hospital";
                var definition = HubFacilityCatalog.Definitions.First(x => x.Id == facilityId);
                int level = definition.StartsRebuilt ? cfg.StartBuildingLevel : 0;
                if (cfg.StartingFacilityLevels != null && cfg.StartingFacilityLevels.TryGetValue(facilityId, out int overrideLevel))
                    level = overrideLevel;
                if (level < 0 || level > definition.MaxLevel) throw new ArgumentOutOfRangeException(nameof(config), $"Invalid start level for {facilityId}.");
                buildings[(int)spec.Kind] = new ServiceBuilding(spec, level, cfg.UpkeepPerBuildingPerDay);
            }
            for (int i = 0; i < buildings.Length; i++)
                if (buildings[i] == null) throw new ArgumentException($"SimConfig.Buildings thiếu công trình {(BuildingKind)i}.");
            foreach (var definition in HubFacilityCatalog.Definitions.Where(x => x.Id != "town_hall" && x.Id != "dormitory"))
            {
                int level = ServiceBuildingForFacility(definition.Id) is BuildingKind serviceKind
                    ? buildings[(int)serviceKind].Level : definition.StartsRebuilt ? 1 : 0;
                if (cfg.StartingFacilityLevels != null && cfg.StartingFacilityLevels.TryGetValue(definition.Id, out int startingLevel))
                    level = startingLevel;
                if (level < 0 || level > definition.MaxLevel)
                    throw new ArgumentOutOfRangeException(nameof(config), $"Invalid start level for {definition.Id}.");
                var state = new HubFacilityRuntimeState(level);
                facilityStates.Add(definition.Id, state);
                string producerId = ProducerForFacility(definition.Id);
                if (producerId != null && production != null)
                {
                    if (level > 0) ApplyFacilityProductionState(definition.Id, state);
                    else production.SetProducerState(new ProducerId(producerId), 1, 0);
                }
            }
            foreach (ServiceBuilding building in buildings) stockExchange.RecordRevenue(building.Kind.ToString(), 0);

            var starterDefinition = MonsterCatalog.CreateDefault(cfg.StarterMonsterHp).Definitions[0];
            for (int i = 0; i < cfg.TrainerCount; i++)
            {
                int initialRank = cfg.StartingTrainerRanks != null && i < cfg.StartingTrainerRanks.Length ? cfg.StartingTrainerRanks[i] : 1;
                if (initialRank < 1 || initialRank > 5) throw new ArgumentOutOfRangeException(nameof(config), "Starting Trainer rank must be within 1..5.");
                Personality personality = cfg.ForcedPersonality ?? (Personality)rng.NextInt(4);
                // Luồng chỉ số riêng theo seed và ID, không làm lệch chuỗi ngẫu nhiên mô phỏng.
                int attributeSeed = unchecked(seed ^ (i * (int)0x9E3779B9u) ^ (int)0xA341316Cu);
                var trainer = new Trainer(new SimRandom(attributeSeed),
                    cfg.TrainerAttributeSettings ?? throw new ArgumentException("Thiếu TrainerAttributeSettings.", nameof(config)))
                {
                    Id = i, Rarity = cfg.StartingTrainerRarities != null && i < cfg.StartingTrainerRarities.Length
                        ? cfg.StartingTrainerRarities[i] : Rarity.Common, Personality = personality, Rank = initialRank,
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

            veterinaryHospital = new VeterinaryHospital(trainers, cfg.VeterinaryHospitalSettings ?? VeterinaryHospitalConfig.Prototype);
            geneBank = new GeneBank(trainers, cfg.GeneBankSettings ?? GeneBankConfig.Prototype);

            queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DawnMinute), SimEventKind.Dawn);
            queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DuskMinute), SimEventKind.Dusk);
            queue.Schedule(SimClock.NextMinuteOfDay(now, 0), SimEventKind.DayStart);
            queue.Schedule(SimClock.PaydayMinute(0), SimEventKind.PaydayDue);
            ScheduleBlackFridayStart(SimClock.PaydayMinute(0));
            if (now <= int.MaxValue - cfg.EventSettings.RandomCrisisCheckIntervalMinutes)
                queue.Schedule(now + cfg.EventSettings.RandomCrisisCheckIntervalMinutes, SimEventKind.RandomCrisisCheck);
            if (useSupplyChain) queue.Schedule(now + Math.Max(1, cfg.MerchantSettings.RouteCycleMinutes / 2), SimEventKind.MerchantRouteStep);
            foreach (Trainer t in trainers) queue.Schedule(now, SimEventKind.TrainerDecide, t.Id, t.Token);
            lastReputationScore = HubReputation.Calculate(buildings, trainers, Bankruptcies, cfg.HubReputationSettings).Score;
            questTracker.SeedCampaignState(unlockedZoneIds.Contains("zone_" + questConfig.CampaignFoundationZoneNumber),
                FacilityLevel("veterinary_hospital", true) >= questConfig.CampaignFoundationHospitalLevel, now);
        }

        /// <summary>Phút in-game hiện tại.</summary>
        public SimTime Now => new SimTime(now);


        // Captures reserve their eventual field/storage slot while recovering.
        bool HasMonsterSlot(Trainer trainer, int additional = 1)
        {
            long owned = trainer.Roster.Members.Count + (long)trainer.Roster.Storage.Count;
            long pending = veterinaryHospital.Recoveries.Count(r => r.TrainerId == trainer.Id && r.IsCapturedMonster);
            return owned + pending + additional <= MonsterRoster.Capacity + (long)cfg.MonsterStorageSettings.Capacity;
        }

        public AdmissionResult AdmitCapturedMonster(int trainerId, Monster monster)
        {
            if (!CanFacilityOperate("veterinary_hospital")) return new AdmissionResult(AdmissionStatus.FacilityUnavailable);
            if (trainerId < 0 || trainerId >= trainers.Count) return new AdmissionResult(AdmissionStatus.UnknownTrainer);
            if (monster == null) return new AdmissionResult(AdmissionStatus.AlreadyOwned);
            if (!HasMonsterSlot(trainers[trainerId])) return new AdmissionResult(AdmissionStatus.Full);
            if (geneBank.Contains(monster.Id)) return new AdmissionResult(AdmissionStatus.AlreadyOwned);
            var result = veterinaryHospital.AdmitCaptured(monster, trainerId, now);
            if (result.Accepted)
            {
                queue.Schedule(result.CompleteAtMinute, SimEventKind.MonsterRecoveryDone, arg: result.RecoveryId);
                Raise(new MonsterRecoveryStarted(now, trainerId, result.MonsterId.Value, result.RecoveryId,
                    result.CompleteAtMinute, result.Fee, true));
            }
            return result;
        }

        public AdmissionResult RequestMonsterEmergencyCare(MonsterId monsterId)
        {
            if (!CanFacilityOperate("veterinary_hospital")) return new AdmissionResult(AdmissionStatus.FacilityUnavailable);
            var result = veterinaryHospital.RequestEmergencyCare(monsterId, now);
            if (result.Accepted)
            {
                var trainer = trainers[result.TrainerId];
                if (trainer.Gold < result.Fee)
                {
                    veterinaryHospital.CancelRecovery(result.RecoveryId);
                    return new AdmissionResult(AdmissionStatus.InsufficientFunds, fee: result.Fee);
                }
                long cogs = (long)Math.Round(result.Fee * cfg.ServiceCogs);
                long hubShare = result.Fee - cogs;
                if (treasury.Balance > long.MaxValue - hubShare)
                {
                    veterinaryHospital.CancelRecovery(result.RecoveryId);
                    return new AdmissionResult(AdmissionStatus.InsufficientFunds, fee: result.Fee);
                }
                trainer.Gold -= result.Fee;
                AddTreasury(hubShare, "EmergencyHospital");
                queue.Schedule(result.CompleteAtMinute, SimEventKind.MonsterRecoveryDone, arg: result.RecoveryId);
                Raise(new MonsterRecoveryStarted(now, result.TrainerId, result.MonsterId.Value, result.RecoveryId,
                    result.CompleteAtMinute, result.Fee, false));
                Raise(new ServiceUsed(now, result.TrainerId, BuildingKind.Hospital, result.Fee, result.Fee,
                    result.Fee, 0));
            }
            return result;
        }

        public bool StoreMonsterInGeneBank(int trainerId, MonsterId monsterId)
        {
            if (!CanFacilityOperate("gene_bank") || trainerId < 0 || trainerId >= trainers.Count || !geneBank.Store(trainerId, monsterId)) return false;
            Raise(new GeneBankOwnershipChanged(now, trainerId, monsterId.Value, MonsterCustody.Trainer, MonsterCustody.GeneBank));
            return true;
        }

        public bool StoreUnassignedMonsterInGeneBank(int trainerId, Monster monster)
        {
            if (!CanFacilityOperate("gene_bank") || trainerId < 0 || trainerId >= trainers.Count || monster == null ||
                veterinaryHospital.Contains(monster.Id) ||
                trainers.Any(x => x.Roster.Members.Any(m => m.Id == monster.Id) || x.Roster.Storage.Any(m => m.Id == monster.Id)) ||
                !geneBank.StoreUnassignedMonster(trainerId, monster)) return false;
            Raise(new GeneBankOwnershipChanged(now, trainerId, monster.Id.Value, MonsterCustody.Unassigned, MonsterCustody.GeneBank));
            return true;
        }

        public bool WithdrawMonsterFromGeneBank(int trainerId, MonsterId monsterId)
        {
            if (!CanFacilityOperate("gene_bank") || trainerId < 0 || trainerId >= trainers.Count || !HasMonsterSlot(trainers[trainerId]) || !geneBank.Withdraw(trainerId, monsterId)) return false;
            Raise(new GeneBankOwnershipChanged(now, trainerId, monsterId.Value, MonsterCustody.GeneBank, MonsterCustody.Trainer));
            return true;
        }

        public Monster ConfiscateForUnpaidGeneBankFee(int trainerId)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return null;
            var monster = geneBank.ConfiscateForUnpaidFee(trainerId);
            if (monster != null) Raise(new MonsterConfiscated(now, trainerId, monster.Id.Value));
            return monster;
        }

        public bool ResellConfiscatedMonster(int buyerId, MonsterId monsterId)
        {
            long price = (cfg.GeneBankSettings ?? GeneBankConfig.Prototype).ResalePrice;
            if (!CanFacilityOperate("gene_bank") || buyerId < 0 || buyerId >= trainers.Count || !HasMonsterSlot(trainers[buyerId]) || trainers[buyerId].Gold < price || treasury.Balance > long.MaxValue - price) return false;
            if (!geneBank.ResellToTrainer(buyerId, monsterId)) return false;
            treasury.Add(price);
            if (price > 0) Raise(new TreasuryChanged(now, price, treasury.Balance, "GeneBankMonsterSale"));
            Raise(new GeneBankOwnershipChanged(now, buyerId, monsterId.Value, MonsterCustody.Hub, MonsterCustody.Trainer));
            Raise(new GeneBankMonsterResold(now, buyerId, monsterId.Value, price));
            return true;
        }

        /// <summary>Số dư Kho bạc.</summary>
        public long Treasury => treasury.Balance;
        int? Bankruptcies => merchantFleet == null ? (int?)null :
            merchantFleet.FormerMerchants.Count + (merchantFleet.Current.State == MerchantState.Bankrupt ? 1 : 0);

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
                case SimEventKind.DormitoryUpgradeComplete: OnDormitoryUpgradeComplete(); break;
                case SimEventKind.TownHallUpgradeComplete: OnTownHallUpgradeComplete(); break;
                case SimEventKind.FacilityUpgradeComplete: OnFacilityUpgradeComplete(e.Arg); break;
                case SimEventKind.FacilityRepairComplete: OnFacilityRepairComplete(e.Arg); break;
                case SimEventKind.BlackFridayStart: OnBlackFridayStart(); break;
                case SimEventKind.RandomCrisisCheck: OnRandomCrisisCheck(); break;
                case SimEventKind.LaborInspectionResolve: OnLaborInspectionResolve(); break;
                case SimEventKind.MonsterFluEnd: OnMonsterFluEnd(); break;
                case SimEventKind.BreedingSeasonEnd: OnBreedingSeasonEnd(); break;
                case SimEventKind.BreedingSeasonDemand: OnBreedingSeasonDemand(); break;
                case SimEventKind.MonsterSiegeResolve: OnMonsterSiegeResolve(); break;
                case SimEventKind.WorldBossResolve: OnWorldBossResolve(); break;
                case SimEventKind.BuildingRepairComplete: OnBuildingRepairComplete((BuildingKind)e.Arg); break;
                case SimEventKind.MarketRetry: if (t != null) OnMarketRetry(t); break;
                case SimEventKind.MonsterRecoveryDone: OnMonsterRecoveryDone(e.Arg); break;
            }
        }

        void OnMonsterRecoveryDone(int recoveryId)
        {
            var result = veterinaryHospital.CompleteRecovery(recoveryId);
            if (result.Accepted)
                Raise(new MonsterRecoveryCompleted(now, result.TrainerId, result.MonsterId.Value,
                    result.IsCapturedMonster, result.IsCapturedMonster && trainers[result.TrainerId].Roster.Storage.Any(x => x.Id == result.MonsterId)));
        }

        void Raise(IDomainEvent e)
        {
            EventRaised?.Invoke(e);
            questTracker.Observe(e);
        }

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
            ExecuteStockAiOrders();
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
            questTracker.AdvanceTo(now);
            ApplyMonsterFluDailyLoss();
            CloseStockMarketDay();
            // Đình công bắt đầu đúng 23:59 ngày Payday nên lần nửa đêm ngay sau đó chưa tính là một ngày đã qua.
            bool firstStrikeMidnight = paydayIndex > 0 && now == SimClock.PaydayMinute(paydayIndex - 1) + 1;
            if (!firstStrikeMidnight)
                foreach (Trainer t in trainers)
                    if (t.StrikeDaysLeft > 0) t.StrikeDaysLeft--;

            foreach (ServiceBuilding b in buildings)
            {
                if (!b.PoweredOn) continue;
                bool wasMaintained = b.Maintained;
                bool paid = treasury.TrySpend(b.UpkeepPerDay);
                b.Maintained = paid;
                if (paid) Raise(new TreasuryChanged(now, -b.UpkeepPerDay, treasury.Balance, "Upkeep"));
                if (paid != wasMaintained) Raise(new BuildingMaintenanceChanged(now, b.Kind, paid));
                string facilityId = b.Kind == BuildingKind.Inn ? "inn" : b.Kind == BuildingKind.Restaurant ? "restaurant"
                    : b.Kind == BuildingKind.Bar ? "bar" : "veterinary_hospital";
                if (facilityStates.TryGetValue(facilityId, out var runtime))
                {
                    runtime.Maintained = b.Maintained;
                    runtime.Damaged = b.Damaged;
                    runtime.PoweredOn = b.PoweredOn;
                    ApplyFacilityProductionState(facilityId, runtime);
                }
                if (paid && !wasMaintained) SeatWaiting(b);   // số chỗ tăng lại: gọi thêm người đang chờ
            }
            foreach (var definition in HubFacilityCatalog.Definitions)
            {
                if (!facilityStates.TryGetValue(definition.Id, out var state) || state.Level <= 0 || !state.PoweredOn ||
                    ServiceBuildingForFacility(definition.Id).HasValue) continue;
                long upkeep;
                try { upkeep = checked(cfg.UpkeepPerBuildingPerDay * state.Level); }
                catch (OverflowException) { upkeep = long.MaxValue; }
                bool paid = treasury.TrySpend(upkeep);
                if (paid) Raise(new TreasuryChanged(now, -upkeep, treasury.Balance, "FacilityUpkeep:" + definition.Id));
                if (paid != state.Maintained)
                {
                    state.Maintained = paid;
                    Raise(new FacilityMaintenanceChanged(now, definition.Id, paid));
                    ApplyFacilityProductionState(definition.Id, state);
                }
            }
            UpdateReputation();
            CheckLaborInspectionTrigger();
            queue.Schedule(now + SimClock.MinutesPerDay, SimEventKind.DayStart);
        }
    }
}
