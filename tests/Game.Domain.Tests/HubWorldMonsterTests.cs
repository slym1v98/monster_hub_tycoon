using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;
using System.Linq;
using System.Text.Json;

public sealed class HubWorldMonsterTests
{
    [Fact]
    public void HospitalServicesRejectRequestsWhenUnbuiltOrPoweredOff()
    {
        var capturedDefinition = MonsterCatalog.Default.Definitions[0];
        var captured = Monster.Create(new MonsterId("hospital_gate_capture"), capturedDefinition,
            Rarity.Rare, MonsterIvGrade.B, 1, 3);
        var unbuilt = new HubWorld(new SimConfig { TrainerCount = 1,
            StartingFacilityLevels = new Dictionary<string, int> { ["veterinary_hospital"] = 0 } }, 901);
        Assert.Equal(AdmissionStatus.FacilityUnavailable, unbuilt.AdmitCapturedMonster(0, captured).Status);
        Assert.Equal(MonsterCustody.Unassigned, captured.Custody);

        var world = new HubWorld(new SimConfig { TrainerCount = 1 }.WithServiceFacilities(), 902);
        var monster = world.MonstersForTrainer(0).First(x => x.IsActive);
        Assert.True(world.SetBuildingPower(BuildingKind.Hospital, false).Ok);
        long gold = world.Trainers[0].Gold;
        long treasury = world.Treasury;
        Assert.Equal(AdmissionStatus.FacilityUnavailable, world.RequestMonsterEmergencyCare(new MonsterId(monster.Id)).Status);
        Assert.Equal(gold, world.Trainers[0].Gold);
        Assert.Equal(treasury, world.Treasury);
        Assert.Empty(world.VeterinaryHospital.Recoveries);
    }

    [Fact]
    public void ViewExposesOwnedMonstersAndFarmAppliesFinalHpAndTrainerExperience()
    {
        var cfg = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DawnMinute,
            BackpackCapacity = 100, FarmChunkMinutes = 30, StartTreasury = 10000 };
        var world = new HubWorld(cfg, 1, new ScriptedExpedition(100, 0), new FixedPriceMarket(cfg));
        world.RunFor(60);
        var trainer = Assert.Single(world.Trainers);
        var monster = Assert.Single(trainer.Monsters);
        Assert.Equal(2, trainer.Level);
        Assert.Equal(MonsterLifeState.Fainted, monster.LifeState);
        Assert.Equal(0, monster.CurrentHp);
        Assert.Equal(ReturnReason.TeamDown.ToString(), trainer.StateReason);
    }

    [Fact]
    public void WalkingUsesZoneDurationAndNeedDecayBeforeFarm()
    {
        var cfg = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DawnMinute, StartWithNightVision = true,
            BackpackCapacity = 100, FarmChunkMinutes = 30, StartTreasury = 10000 };
        var world = new HubWorld(cfg, 1, new ScriptedExpedition(0, 0, faint: false), new FixedPriceMarket(cfg));
        world.RunFor(30);
        Assert.Equal(30, world.Now.TotalMinutes - SimClock.DawnMinute);
        Assert.Equal(TrainerState.Farming, Assert.Single(world.Trainers).State);
        Assert.Equal("zone_1", world.Trainers[0].CurrentZoneId);
        Assert.Equal(97, world.Trainers[0].Stamina, 8);
    }

    [Fact]
    public void HubSelectsHighestIncomeEligibleUnlockedZoneAndKeepsTypedYield()
    {
        ZoneDefinition Make(string id, MaterialId material, double gold) => new ZoneDefinition(id, id, 1, 30,
            new[] { new ZoneMaterialWeight(material, 1, 1) }, new EncounterProfile(new Dictionary<Game.Domain.Monsters.MonsterElement, double> {
                [Game.Domain.Monsters.MonsterElement.Grass] = 1 }, 1, gold, 1, 10));
        var poor = Make("poor", MaterialId.For(MaterialFamily.Ore, 1), 10);
        var rich = Make("rich", MaterialId.For(MaterialFamily.Herb, 1), 100);
        var config = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DawnMinute, StartTreasury = 10000,
            UnlockedZoneIds = new[] { "poor", "rich" }, ZoneCatalogSettings = new ZoneCatalog(new[] { poor, rich }), BackpackCapacity = 10 };
        var resolver = new ScriptedExpedition(0, 0, faint: false, units: 1);
        var world = new HubWorld(config, 4, resolver, new FixedPriceMarket(config));
        world.RunFor(60);
        Assert.Equal("rich", resolver.LastZoneId);
        Assert.Contains(world.Trainers[0].Monsters, m => m.IsActive);
        Assert.Contains(world.Trainers[0].CurrentZoneId, new[] { "rich" });
        Assert.True(world.Trainers[0].BackpackUnits > 0);
    }

    [Fact]
    public void DefaultResolverUsesZoneEncounterRateAndChunkDuration()
    {
        var teamMonster = new MonsterSnapshot(new MonsterId("hero"), Game.Domain.Monsters.MonsterElement.Grass,
            new MonsterStats(1000, 500, 100, 10, 0), 1000, new[] { "grass_strike" });
        var trainer = new TrainerSnapshot(0, 1, 1, Rarity.Common, Personality.Capitalist,
            new TrainerAttributes(1, 0, 1, 1), team: new[] { teamMonster }, activeMonsterId: teamMonster.Id);
        var zone = new ZoneDefinition("rate", "Rate", 1, 0,
            new[] { new ZoneMaterialWeight(MaterialId.For(MaterialFamily.Ore, 1), 1, 1) },
            new EncounterProfile(new Dictionary<Game.Domain.Monsters.MonsterElement, double> {
                [Game.Domain.Monsters.MonsterElement.Grass] = 1 }, 4, 1, 0, 10));
        var result = new DefaultExpeditionResolver(SimConfig.Default).Resolve(trainer, zone, 30, new SimRandom(1));
        Assert.Equal(2, result.Battles.Count);
        Assert.Equal(20, result.TrainerExperience);
    }

    [Fact]
    public void CapturedMonsterRecoveryUsesWorldEventQueueAndTransfersToTrainerOnce()
    {
        var config = new SimConfig { TrainerCount = 1, StartMinute = 100,
            VeterinaryHospitalSettings = new VeterinaryHospitalConfig(recoveryBeds: 1, firstCaptureRecoveryMinutes: 5) };
        var world = new HubWorld(config, 12);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        var definition = MonsterCatalog.Default.Definitions[0];
        var captured = Monster.Create(new MonsterId("world_capture"), definition, Rarity.Rare,
            MonsterIvGrade.B, 1, 3);
        var admission = world.AdmitCapturedMonster(0, captured);
        Assert.True(admission.Accepted);
        Assert.Equal(MonsterCustody.Hospital, captured.Custody);
        world.RunFor(5);
        Assert.Equal(MonsterCustody.Trainer, captured.Custody);
        Assert.Contains(world.Trainers[0].Monsters, x => x.Id == captured.Id.Value);
        Assert.Contains(events, x => x is MonsterRecoveryStarted);
        Assert.Contains(events, x => x is MonsterRecoveryCompleted);
        Assert.Equal(0, world.VeterinaryHospital.OccupiedRecoveryBeds);
    }

    [Fact]
    public void GeneBankFeeAssessmentIsEmittedAfterPaydayWageSettlement()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartTreasury = 10000, StartTrainerGold = 500 }.WithTierThreeFacilities("gene_bank"), 19);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        var starter = Assert.Single(world.Trainers[0].Monsters);
        Assert.True(world.StoreMonsterInGeneBank(0, new MonsterId(starter.Id)));
        world.RunUntilPayday();
        events.Clear();
        world.ResolvePayday();
        int resolved = events.FindIndex(x => x is PaydayResolved);
        int assessed = events.FindIndex(x => x is GeneBankFeeAssessed fee && fee.TrainerId == 0 && fee.Amount == 600);
        Assert.True(resolved >= 0);
        Assert.True(assessed > resolved);
    }

    [Fact]
    public void GeneBankFeeIsUnpaidAndConfiscatesOneMonsterWhenPaydayTreasuryIsEmpty()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartTreasury = 0, StartTrainerGold = 0,
            PatronChancePerHour = 0, UnlockedZoneIds = Array.Empty<string>() }.WithTierThreeFacilities("gene_bank"), 21);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        var starter = Assert.Single(world.Trainers[0].Monsters);
        var id = new MonsterId(starter.Id);
        Assert.True(world.StoreMonsterInGeneBank(0, id));
        world.RunUntilPayday();
        events.Clear();
        long goldBeforePayday = world.Trainers[0].Gold;
        long treasuryBeforePayday = world.Treasury;
        var payday = world.ResolvePayday();

        var settled = Assert.Single(events.OfType<GeneBankFeeSettled>());
        Assert.Equal(600, settled.Assessed);
        Assert.InRange(settled.Paid, 0, 599);
        Assert.Equal(600 - settled.Paid, settled.Unpaid);
        long repaid = events.OfType<TrainerLoanRepaid>().Sum(e => e.Amount);
        Assert.Equal(goldBeforePayday + payday.TotalPaid - repaid - settled.Paid, world.Trainers[0].Gold);
        Assert.Equal(treasuryBeforePayday - payday.TotalPaid + repaid + settled.Paid, world.Treasury);
        Assert.Equal(starter.Id, settled.ConfiscatedMonsterId);
        Assert.Equal(MonsterCustody.Hub, world.GeneBank.ConfiscatedMonsters.Single().Custody);
        Assert.Contains(events, e => e is MonsterConfiscated confiscated && confiscated.MonsterId == starter.Id);
    }

    [Fact]
    public void NoVisionTrainerReturnsAtDuskAndNightVisionTrainerCanRemainOutside()
    {
        HubWorld Create(bool vision)
        {
            var cfg = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DuskMinute - 5,
                StartWithNightVision = vision, StartTreasury = 10000, BackpackCapacity = 100, FarmChunkMinutes = 30 };
            return new HubWorld(cfg, 2, new ScriptedExpedition(0, 0, faint: false), new FixedPriceMarket(cfg));
        }
        var without = Create(false); without.RunFor(10);
        Assert.NotEqual(TrainerState.Farming, without.Trainers[0].State);
        var with = Create(true); with.RunFor(10);
        Assert.Equal(TrainerState.Traveling, with.Trainers[0].State);
    }

    // Missing commands, leaked mutable entities, duplicate IDs and unreported rewards are
    // the integration breaks these tests exercise through the Hub boundary.
    [Fact]
    public void DirectorLifecycleCommandsRejectInvalidInputAndEmitExplicitChanges()
    {
        var cfg = new SimConfig { TrainerCount = 2, StartMinute = 100,
            VeterinaryHospitalSettings = new VeterinaryHospitalConfig(firstCaptureRecoveryMinutes: 1),
            RarityUpgradeSettings = new UpgradeConfig(new[] { 1d, 1d, 1d, 1d }, new[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, 0 }) };
        var definition = MonsterCatalog.Default.Definitions[0];
        var target = new MonsterDefinition("test_evolved", "Test evolved", MonsterElement.Water, MonsterRole.Tank,
            new MonsterStats(400, 12, 12, 1, 0.05), new MonsterStats(0, 0, 0, 0, 0));
        cfg.EvolutionCatalogSettings = new EvolutionCatalog(new[] {
            new EvolutionDefinition(definition.Id, "test_branch", target, 40, 1, 1,
                new ProductId("gene_fragment"), 1, new[] { target.Element.ToString().ToLowerInvariant() + "_strike" }) });
        var world = new HubWorld(cfg.WithTierThreeFacilities("gene_bank"), 31);
        var events = new List<IDomainEvent>(); world.EventRaised += events.Add;
        Assert.True(world.AdmitCapturedMonster(0, Monster.Create(new MonsterId("reserve"), definition,
            Rarity.Common, MonsterIvGrade.B, 40, 2)).Accepted);
        Assert.True(world.AdmitCapturedMonster(0, Monster.Create(new MonsterId("salvage"), definition,
            Rarity.Common, MonsterIvGrade.D, 1, 3)).Accepted);
        world.RunFor(1);
        Assert.False(world.SwapActiveMonster(-1, "reserve").Ok);
        Assert.False(world.SwapActiveMonster(1, "reserve").Ok);
        Assert.False(world.AppraiseMonster(0, "").Ok);
        Assert.False(world.UpgradeMonsterRarity(0, "trainer_0_starter").Ok);
        Assert.True(world.SwapActiveMonster(0, "reserve").Ok);
        Assert.True(world.StoreMonster(0, "reserve").Ok);
        Assert.Contains(world.MonstersForTrainer(0), m => m.Id == "reserve" && m.LifeState == MonsterLifeState.Stored);
        Assert.True(world.WithdrawMonster(0, "reserve").Ok);
        Assert.True(world.DepositMonster(0, "reserve").Ok);
        Assert.False(world.WithdrawBankMonster(1, "reserve").Ok);
        Assert.True(world.WithdrawBankMonster(0, "reserve").Ok);
        long before = world.Trainers[0].Gold;
        Assert.True(world.AppraiseMonster(0, "reserve").Ok);
        Assert.False(world.AppraiseMonster(0, "reserve").Ok);
        Assert.Equal(before - 25, world.Trainers[0].Gold);
        Assert.True(world.DismantleMonster(0, "salvage").Ok);
        Assert.DoesNotContain(world.MonstersForTrainer(0), m => m.Id == "salvage");
        Assert.Equal(5, world.Trainers[0].Products[new ProductId("gene_fragment")]);
        Assert.True(world.UpgradeMonsterRarity(0, "reserve").Ok);
        Assert.False(world.EvolveMonster(0, "reserve", "missing").Ok);
        Assert.True(world.EvolveMonster(0, "reserve", "test_branch").Ok);
        Assert.Equal(3, world.Trainers[0].Products[new ProductId("gene_fragment")]);
        var evolved = world.MonstersForTrainer(0).Single(m => m.Id == "reserve");
        Assert.Equal(target.Id, evolved.SpeciesId);
        Assert.Equal(Rarity.Rare, evolved.Rarity);
        Assert.Equal(MonsterIvGrade.B, evolved.KnownIv);
        Assert.Contains(events, e => e is MonsterAppraised);
        Assert.Contains(events, e => e is MonsterDismantled);
        Assert.Contains(events, e => e is MonsterUpgradeResolved);
        Assert.Contains(events, e => e is MonsterEvolutionResolved);
        Assert.Contains(events, e => e is TrainerProductChanged change && change.Quantity == -2);
        world.ValidateInvariants();
    }

    [Fact]
    public void SnapshotsAreDetachedAndServicesExposeNoMutableEntities()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1 }.WithTierThreeFacilities("gene_bank"), 1);
        var old = world.MonstersForTrainer(0);
        Assert.Throws<NotSupportedException>(() => ((IList<MonsterView>)old).Clear());
        Assert.IsType<VeterinaryHospitalView>(world.VeterinaryHospital);
        Assert.True(world.DepositMonster(0, old[0].Id).Ok);
        Assert.Equal(MonsterCustody.Trainer, old[0].Custody);
        var bank = world.GeneBank;
        Assert.Single(bank.StoredMonsters);
        Assert.True(world.WithdrawBankMonster(0, old[0].Id).Ok);
        Assert.Single(bank.StoredMonsters);
        Assert.Empty(world.GeneBank.StoredMonsters);
        Assert.Empty(world.MonstersForTrainer(-1));
        world.ValidateInvariants();
    }

    [Fact]
    public void DuplicateIdentityAcrossHospitalAndBankIsRejectedBeforeOwnershipTransfer()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 2 }.WithTierThreeFacilities("gene_bank"), 10);
        var def = MonsterCatalog.Default.Definitions[0];
        var first = Monster.Create(new MonsterId("duplicate"), def, Rarity.Common, MonsterIvGrade.B, 1, 1);
        var second = Monster.Create(first.Id, def, Rarity.Common, MonsterIvGrade.B, 1, 2);
        Assert.True(world.AdmitCapturedMonster(0, first).Accepted);
        Assert.False(world.StoreUnassignedMonsterInGeneBank(1, second));
        Assert.Equal(MonsterCustody.Unassigned, second.Custody);
        world.ValidateInvariants();
    }

    [Fact]
    public void SameSeedDefaultResolverProducesIdenticalViewsAndEventStreamsWithUnlocks()
    {
        (string views, string events) Run()
        {
            var world = new HubWorld(new SimConfig { TrainerCount = 2, StartTreasury = 100000 }, 2026);
            var stream = new List<string>();
            world.EventRaised += e => stream.Add(JsonSerializer.Serialize(e, e.GetType()));
            Assert.False(world.UnlockZone("unknown").Ok);
            world.RunFor(1440);
            world.ValidateInvariants();
            Assert.Contains(stream, e => e.Contains("Battles"));
            return (JsonSerializer.Serialize(world.Trainers), string.Join("\n", stream));
        }
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void PaydayCollectsGeneBankFeeAfterWagesAndPreservesStoredMonsterWhenPaid()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartTrainerGold = 1000000,
            StartTreasury = 10000, PatronChancePerHour = 0, UnlockedZoneIds = Array.Empty<string>() }.WithTierThreeFacilities("gene_bank"), 1);
        Assert.True(world.DepositMonster(0, "trainer_0_starter").Ok);
        world.RunUntilPayday();
        long gold = world.Trainers[0].Gold;
        var events = new List<IDomainEvent>(); world.EventRaised += events.Add;
        var outcome = world.ResolvePayday();
        var fee = Assert.Single(events.OfType<GeneBankFeeSettled>());
        long repaid = events.OfType<TrainerLoanRepaid>().Sum(e => e.Amount);
        Assert.Equal(gold + outcome.TotalPaid - repaid - fee.Paid, world.Trainers[0].Gold);
        Assert.Equal(600, fee.Paid);
        Assert.Equal(0, fee.Unpaid);
        Assert.Single(world.GeneBank.StoredMonsters);
        Assert.DoesNotContain(events, e => e is MonsterConfiscated);
        Assert.True(events.FindIndex(e => e is GeneBankFeeSettled) > events.FindIndex(e => e is PaydayResolved));
        world.ValidateInvariants();
    }

    [Fact]
    public void SalvageCanBeSoldAndPurchasedAtStationWithMoneyAndItemLedger()
    {
        var cfg = new SimConfig { TrainerCount = 1, StartMinute = 100,
            ConsumablePrices = new ConsumablePriceConfig(new Dictionary<ProductId, long> { [new ProductId("gene_fragment")] = 10 }),
            VeterinaryHospitalSettings = new VeterinaryHospitalConfig(firstCaptureRecoveryMinutes: 1) };
        var world = new HubWorld(cfg, 1);
        Assert.True(world.AdmitCapturedMonster(0, Monster.Create(new MonsterId("salvage_trade"),
            MonsterCatalog.Default.Definitions[0], Rarity.Common, MonsterIvGrade.D, 1, 1)).Accepted);
        world.RunFor(1);
        Assert.True(world.DismantleMonster(0, "salvage_trade").Ok);
        Assert.True(world.SetProductBuyRequest("gene_fragment", 5, 10).Ok);
        Assert.True(world.SellProductToStation(0, "gene_fragment", 5).Ok);
        long treasury = world.Treasury, gold = world.Trainers[0].Gold;
        Assert.True(world.PurchaseProduct(0, "gene_fragment", 2).Ok);
        Assert.Equal(gold - 20, world.Trainers[0].Gold);
        Assert.Equal(treasury + 20, world.Treasury);
        Assert.Equal(2, world.Trainers[0].Products[new ProductId("gene_fragment")]);
        Assert.Equal(3, world.SupplyStocks.Single(s => s.ItemId == "product:gene_fragment").Available);
        Assert.False(world.PurchaseProduct(0, "gene_fragment", 4).Ok);
        Assert.Equal(treasury + 20, world.Treasury);
        Assert.Equal(2, world.SupplyTransactions.Count);
        world.ValidateInvariants();
    }

    [Fact]
    public void RemovingLastFieldMonsterDuringTravelReturnsHomeWithoutResolvingEmptyTeam()
    {
        var cfg = new SimConfig { TrainerCount = 1, StartTreasury = 10000 };
        var world = new HubWorld(cfg, 1);
        world.RunFor(1);
        Assert.True(world.StoreMonster(0, "trainer_0_starter").Ok);
        world.RunFor(120);
        Assert.Equal(TrainerState.AtHub, world.Trainers[0].State);
        Assert.Equal("NoFieldMonster", world.Trainers[0].StateReason);
        world.ValidateInvariants();
    }

    [Fact]
    public void InvariantsDetectDuplicateOwnersAndNegativeTrainerMoney()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 2 }, 1);
        // Deliberate corruption belongs in tests, never in a production mutation API.
        var field = typeof(HubWorld).GetField("trainers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var trainers = (List<Trainer>)field.GetValue(world);
        trainers[1].Gold = -1;
        Assert.Throws<InvalidOperationException>(() => world.ValidateInvariants());
        trainers[1].Gold = 200;
        trainers[1].Roster.Add(Monster.Create(new MonsterId("trainer_0_starter"), MonsterCatalog.Default.Definitions[0],
            Rarity.Common, MonsterIvGrade.B, 1, 2));
        Assert.Throws<InvalidOperationException>(() => world.ValidateInvariants());
    }

    [Theory]
    [InlineData("monster")]
    [InlineData("expedition")]
    public void SimScenariosUseHubResolverAndReconcileDeterministically(string mode)
    {
        string Run()
        {
            var writer = new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            MonsterScenarios.Run(mode, writer);
            return string.Join("\n", writer.ToString().Split('\n').Where(line => !line.StartsWith("Runtime:")));
        }
        string first = Run();
        Assert.Equal(first, Run());
        Assert.Contains("Gold reconciliation: trainer difference 0; treasury difference 0", first);
        Assert.Contains("Ownership reconciliation: difference 0", first);
        Assert.Contains("Item reconciliation: difference 0", first);
        Assert.Contains("zone_1: encounters ", first);
        Assert.Contains("appraisal 1; evolution attempts 1", first);
        Assert.Matches(@"swaps [1-9][0-9]*", first);
        Assert.Matches(@"faints [1-9][0-9]*", first);
        Assert.Matches(@"losses [1-9][0-9]*", first);
        // The progression gates prevent this rank-one fixture from surveying higher Zones.
    }

    [Fact]
    public void HubCaptureConsumesInventoryAndQueuesRecoveredOwnershipExactlyOnce()
    {
        var cfg = new SimConfig { TrainerCount = 1, StartMinute = 100,
            VeterinaryHospitalSettings = new VeterinaryHospitalConfig(firstCaptureRecoveryMinutes: 1) };
        var world = new HubWorld(cfg, 2026);
        var trainers = (List<Trainer>)typeof(HubWorld).GetField("trainers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(world);
        trainers[0].Inventory.Add(new ProductId("capture_ball"), 20);
        var events = new List<IDomainEvent>(); world.EventRaised += events.Add;
        var target = Monster.Create(new MonsterId("wild_target"), MonsterCatalog.Default.Definitions[0],
            Rarity.Rare, MonsterIvGrade.B, 1, 3);
        target.SetCurrentHp(1);
        bool captured = false;
        for (int i = 0; i < 20 && !captured; i++)
        {
            Assert.True(world.AttemptCapture(0, target).Ok);
            captured = events.OfType<MonsterCaptureResolved>().Any(e => e.Success);
        }
        Assert.True(captured);
        Assert.False(world.AttemptCapture(0, target).Ok);
        Assert.Single(world.VeterinaryHospital.Recoveries);
        world.ValidateInvariants();
        world.RunFor(1);
        Assert.Single(world.MonstersForTrainer(0), m => m.Id == "wild_target");
        Assert.Equal(20 - events.OfType<MonsterCaptureResolved>().Count(),
            world.Trainers[0].Products[new ProductId("capture_ball")]);
        world.ValidateInvariants();
    }

    [Fact]
    public void LocalStorageLimitRejectsTransfersWithoutLosingBankOwnership()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1 }.WithTierThreeFacilities("gene_bank"), 1);
        var trainer = InternalTrainers(world)[0];
        for (int i = 0; i < 500; i++)
            trainer.Roster.AddToStorage(Monster.Create(new MonsterId("stored_" + i), MonsterCatalog.Default.Definitions[0],
                Rarity.Common, MonsterIvGrade.B, 1, i));
        Assert.False(world.StoreMonster(0, "trainer_0_starter").Ok);
        trainer.Roster.Add(Monster.Create(new MonsterId("second"), MonsterCatalog.Default.Definitions[0], Rarity.Common, MonsterIvGrade.B, 1, 2));
        trainer.Roster.Add(Monster.Create(new MonsterId("third"), MonsterCatalog.Default.Definitions[0], Rarity.Common, MonsterIvGrade.B, 1, 3));
        Assert.True(world.StoreUnassignedMonsterInGeneBank(0, Monster.Create(new MonsterId("banked"),
            MonsterCatalog.Default.Definitions[0], Rarity.Common, MonsterIvGrade.B, 1, 4)));
        Assert.False(world.WithdrawBankMonster(0, "banked").Ok);
        Assert.Single(world.GeneBank.StoredMonsters);
        Assert.Equal(503, world.MonstersForTrainer(0).Count(m => m.Custody == MonsterCustody.Trainer));
        world.ValidateInvariants();
        trainer.Roster.AddToStorage(Monster.Create(new MonsterId("overflow"), MonsterCatalog.Default.Definitions[0],
            Rarity.Common, MonsterIvGrade.B, 1, 5));
        Assert.Throws<InvalidOperationException>(world.ValidateInvariants);
    }

    [Fact]
    public void RecoveryAdmissionReservesAnOwnershipSlotAndCannotBeAppraised()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartMinute = 100, StartTrainerGold = 1000 }, 1);
        var trainer = InternalTrainers(world)[0];
        trainer.Roster.Active.SetCurrentHp(1);
        Assert.True(world.RequestMonsterEmergencyCare(trainer.Roster.Active.Id).Accepted);
        long gold = trainer.Gold, treasury = world.Treasury;
        Assert.False(world.AppraiseMonster(0, "trainer_0_starter").Ok);
        Assert.False(world.SwapActiveMonster(0, "trainer_0_starter").Ok);
        Assert.False(world.DepositMonster(0, "trainer_0_starter").Ok);
        Assert.Equal(gold, trainer.Gold);
        Assert.Equal(treasury, world.Treasury);
        world.ValidateInvariants();
    }

    [Fact]
    public void OrphanedRecoveryEventsAndInvalidHpAreDetected()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1 }, 1);
        var monster = InternalTrainers(world)[0].Roster.Active;
        typeof(Monster).GetProperty("CurrentHp").SetValue(monster, monster.MaxHp + 1);
        Assert.Throws<InvalidOperationException>(world.ValidateInvariants);
        monster.SetCurrentHp(monster.MaxHp);
        var queue = (EventQueue)typeof(HubWorld).GetField("queue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(world);
        queue.Schedule(world.Now.TotalMinutes + 1, SimEventKind.MonsterRecoveryDone, arg: 123);
        Assert.Throws<InvalidOperationException>(world.ValidateInvariants);
    }

    [Fact]
    public void RejectedCommandsLeaveSnapshotsEventsAndSeededFutureUnchanged()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1 }, 42);
        var control = new HubWorld(new SimConfig { TrainerCount = 1 }, 42);
        var events = new List<string>(); var controlEvents = new List<string>();
        world.EventRaised += e => events.Add(JsonSerializer.Serialize(e, e.GetType()));
        control.EventRaised += e => controlEvents.Add(JsonSerializer.Serialize(e, e.GetType()));
        Assert.False(world.EvolveMonster(0, "trainer_0_starter", "missing").Ok);
        Assert.False(world.UpgradeMonsterRarity(0, "trainer_0_starter", "unknown").Ok);
        Assert.False(world.DismantleMonster(0, "trainer_0_starter").Ok);
        Assert.False(world.WithdrawMonster(0, "missing").Ok);
        Assert.False(world.UnlockZone(null).Ok);
        Assert.Empty(events);
        world.RunFor(1440); control.RunFor(1440);
        Assert.Equal(controlEvents, events);
        Assert.Equal(JsonSerializer.Serialize(control.Trainers), JsonSerializer.Serialize(world.Trainers));
    }

    [Fact]
    public void PendingCapturesReserveCapacityBeforeInventoryOrRngIsSpent()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartMinute = 100,
            MonsterStorageSettings = new MonsterStorageConfig(0) }, 2026);
        var def = MonsterCatalog.Default.Definitions[0];
        var trainer = InternalTrainers(world)[0];
        trainer.Inventory.Add(new ProductId("capture_ball"), 3);
        Assert.True(world.AdmitCapturedMonster(0, Monster.Create(new MonsterId("pending_a"), def,
            Rarity.Common, MonsterIvGrade.B, 1, 1)).Accepted);
        Assert.True(world.AdmitCapturedMonster(0, Monster.Create(new MonsterId("pending_b"), def,
            Rarity.Common, MonsterIvGrade.B, 1, 2)).Accepted);
        var target = Monster.Create(new MonsterId("over_capacity"), def, Rarity.Epic, MonsterIvGrade.B, 1, 3);
        target.SetCurrentHp(1);
        var events = new List<IDomainEvent>(); world.EventRaised += events.Add;
        Assert.False(world.AttemptCapture(0, target).Ok);
        Assert.Empty(events);
        Assert.Equal(3, trainer.Inventory.Count(new ProductId("capture_ball")));
        Assert.Equal(AdmissionStatus.Full, world.AdmitCapturedMonster(0, target).Status);
        world.RunFor(1440);
        Assert.Equal(3, world.Trainers[0].Monsters.Count);
        world.ValidateInvariants();
    }

    [Fact]
    public void ExtraRecoveryCompletionWithValidIdentityButWrongTimeIsOrphaned()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartMinute = 100 }, 1);
        var admission = world.AdmitCapturedMonster(0, Monster.Create(new MonsterId("queued"),
            MonsterCatalog.Default.Definitions[0], Rarity.Common, MonsterIvGrade.B, 1, 1));
        Assert.True(admission.Accepted);
        var queue = (EventQueue)typeof(HubWorld).GetField("queue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(world);
        queue.Schedule(admission.CompleteAtMinute + 1, SimEventKind.MonsterRecoveryDone, arg: admission.RecoveryId);
        Assert.Throws<InvalidOperationException>(world.ValidateInvariants);
    }

    [Fact]
    public void NestedServiceAndTrainerCollectionsAreImmutableDetachedSnapshots()
    {
        var world = new HubWorld(new SimConfig { TrainerCount = 1, StartMinute = 100,
            VeterinaryHospitalSettings = new VeterinaryHospitalConfig(firstCaptureRecoveryMinutes: 1) }.WithTierThreeFacilities("gene_bank"), 1);
        var monster = Monster.Create(new MonsterId("recovering"), MonsterCatalog.Default.Definitions[0],
            Rarity.Common, MonsterIvGrade.B, 1, 1);
        Assert.True(world.AdmitCapturedMonster(0, monster).Accepted);
        var hospital = world.VeterinaryHospital;
        Assert.Throws<NotSupportedException>(() => ((IList<MonsterRecoveryView>)hospital.Recoveries).Clear());
        var zones = world.Zones;
        Assert.Throws<NotSupportedException>(() => ((IList<ZoneView>)zones).Clear());
        var trainer = world.Trainers[0];
        Assert.Throws<NotSupportedException>(() => ((IDictionary<ProductId, int>)trainer.Products).Add(new ProductId("gene_fragment"), 1));
        world.RunFor(1);
        Assert.Single(hospital.Recoveries);
        Assert.Equal(MonsterLifeState.Recovering, hospital.Recoveries[0].Monster.LifeState);
        Assert.Empty(world.VeterinaryHospital.Recoveries);
        Assert.Single(trainer.Monsters);
        Assert.Equal(2, world.Trainers[0].Monsters.Count);
        Assert.True(zones.Single(z => z.Id == "zone_2").IsUnlocked);
        Assert.True(world.DepositMonster(0, "recovering").Ok);
        var bank = world.GeneBank;
        Assert.Throws<NotSupportedException>(() => ((IList<BankMonsterView>)bank.StoredMonsters).Clear());
        Assert.Equal(MonsterLifeState.Stored, bank.StoredMonsters[0].Monster.LifeState);
        world.ValidateInvariants();
    }

    static List<Trainer> InternalTrainers(HubWorld world) => (List<Trainer>)typeof(HubWorld).GetField("trainers",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(world);

    private sealed class ScriptedExpedition : IExpeditionResolver
    {
        readonly long experience; readonly long gold; readonly bool faint; readonly int units;
        public string LastZoneId { get; private set; }
        public ScriptedExpedition(long experience, long gold, bool faint = true, int units = 0) { this.experience = experience; this.gold = gold; this.faint = faint; this.units = units; }
        public ExpeditionResult Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random)
        {
            LastZoneId = zone.Id;
            var hp = new Dictionary<MonsterId, long>();
            foreach (var monster in trainer.Team) hp[monster.Id] = faint ? 0 : monster.CurrentHp;
            var material = zone.MaterialWeights[0].MaterialId;
            return new ExpeditionResult(Array.Empty<Game.Domain.Combat.BattleResult>(),
                new ExpeditionLoot(units == 0 ? Array.Empty<MaterialQuantity>() : new[] { new MaterialQuantity(material, units) }, Array.Empty<MaterialQuantity>(), gold, experience), experience, hp);
        }
    }
}
