# Sub-project 3: Monster, Combat, Zone and Lifecycle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) to implement this plan task-by-task. Every task has a disjoint primary owner and must be reviewed before the next task starts. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace aggregate Monster HP and placeholder farming with a deterministic, GDD-driven Monster lifecycle from Zone selection and combat through loot, capture, recovery, appraisal, Gene Bank and evolution.

**Architecture:** Keep combat, capture, Zone selection, loot and Monster lifecycle as pure data-driven Domain units; let `HubWorld` own time, Trainer decisions, inventory and ledger mutations. `Game.Sim`, offline progress and tests use the same `HubWorld` path. GDD-locked rules are code invariants; all unspecified numeric rules remain identified Prototype/TBD inputs in the balance workbook.

**Tech Stack:** C# `netstandard2.1`, .NET SDK projects, xUnit, existing `SimRandom`, `HubWorld` event queue, `@oai/artifact-tool` for workbook edits.

**Spec:** `docs/superpowers/specs/2026-10-02-domain-monster-combat-design.md`

## Global Constraints

- Domain is pure C# and has no Unity dependency; `HubWorld` remains the sole entry point.
- All random draws use the seeded `SimRandom`; iteration and tie-breaking use stable IDs.
- Code/type/member names are English; comments are Vietnamese with diacritics.
- Keep locked GDD rules exact: nine-element chart, 1 Active + at most 2 Reserve, auto-Swap below 15% HP, fainted Monsters do not die, Zone N requires Rank ≥ N, night-vision loot/EXP ×2, capture requires a Ball, and Trainer has no HP.
- Every new tunable value has a stable ID, unit, status (`Locked`, `Prototype`, or `TBD`) and GDD/code source in `docs/balance/MonsterHUB_Balance.xlsx`; Prototype is not final balance.
- Material, product, Monster ownership, Gold and item changes are explicit state transitions/events and never arise from a view or hidden fallback.
- Each task follows RED → GREEN → REFACTOR, runs its named focused tests and the full `dotnet test tests/Game.Domain.Tests` suite before commit.

## Review Focus

1. Rank/Trainer-level boundaries and fractional Trainer conversion must preserve the GDD’s `ceil` Monster level and unrounded Rebellion scale; pin in Task 2 tests at Trainer Lv 1, 5, 6, 100 for every Rank.
2. Empty, duplicate, full and fainted rosters must never produce two Active Monsters or silently drop an owned Monster; pin in Task 1 roster tests.
3. A zero-HP action, an HP value exactly at 15%, cooldown expiry and equal-time actions must have stable order and no accidental death/Swap; pin in Task 6 combat tests.
4. Empty stock, partial affordability and insufficient trainer Gold must not partially debit money or consume unreceived goods; pin in Task 10 purchase tests.
5. Full Veterinary recovery beds, full Gene Bank and unpaid Payday fees must leave each Monster owned exactly once and follow the configured wait/store/confiscate policy; pin in Task 13 lifecycle tests.

---

### Task 1: Monster data types and roster invariants

**Files:**
- Create: `src/Game.Domain/Monsters/MonsterTypes.cs`
- Create: `src/Game.Domain/Monsters/MonsterStats.cs`
- Create: `src/Game.Domain/Monsters/MonsterDefinition.cs`
- Create: `src/Game.Domain/Monsters/MonsterCatalog.cs`
- Create: `src/Game.Domain/Monsters/Monster.cs`
- Create: `src/Game.Domain/Monsters/MonsterRoster.cs`
- Modify: `src/Game.Domain/Trainers/Trainer.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.cs`
- Test: `tests/Game.Domain.Tests/MonsterRosterTests.cs`
- Test: `tests/Game.Domain.Tests/MonsterCatalogTests.cs`

**Interfaces:**
- Produces `MonsterElement`, `MonsterRole`, `MonsterIvGrade`, `MonsterLifeState`, immutable `MonsterStats`, data-only `MonsterDefinition`, deterministic `Monster.Create(id, definition, rarity, iv, level, seed)`, and `MonsterRoster.Add`, `SetActive`, `MoveToStorage`, `RestoreFromStorage`.
- `MonsterRoster` has capacity 3 and at most one Active; existing HubWorld-created Trainers start with one Soul-bound Monster of matching Trainer Rarity.
- `Trainer.Roster` becomes the owner of Monster state. Preserve `TeamHp`/`TeamHpMax` only as computed compatibility properties until Task 8 removes all mutable aggregate uses.

- [ ] **Step 1: Write failing tests** for adding a starter Monster, assigning Active/Reserve, refusing duplicate IDs/fourth member, and preserving Monster identity when moved to storage.
- [ ] **Step 2: Run focused tests and confirm expected failure:** `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~MonsterRosterTests --no-restore`.
- [ ] **Step 3: Implement the Monster types, catalog validation and roster transitions** in the listed Domain files; initialize one deterministic starter Monster from `HubWorld` using the world seed and Trainer ID.
- [ ] **Step 4: Run focused tests, then `dotnet test tests/Game.Domain.Tests`**; both must pass.
- [ ] **Step 5: Commit** as `feat(domain): add monster roster model`.

### Task 2: Level conversion, IV/stat derivation and Trainer attributes

**Files:**
- Create: `src/Game.Domain/Monsters/MonsterProgression.cs`
- Create: `src/Game.Domain/Trainers/TrainerAttributes.cs`
- Create: `src/Game.Domain/Trainers/TrainerProgression.cs`
- Create: `src/Game.Domain/Trainers/TrainerClass.cs`
- Modify: `src/Game.Domain/Monsters/Monster.cs`
- Modify: `src/Game.Domain/Trainers/Trainer.cs`
- Modify: `src/Game.Domain/Config/SimConfig.cs`
- Test: `tests/Game.Domain.Tests/MonsterProgressionTests.cs`
- Test: `tests/Game.Domain.Tests/MonsterStatsTests.cs`

**Interfaces:**
- `MonsterProgression.MonsterLevel(int trainerRank, int trainerLevel) -> int` uses `20 * (rank - 1) + ceil(level / 5)`.
- `MonsterProgression.TrainerManagementLevel(int trainerRank, int trainerLevel) -> double` uses `20 * (rank - 1) + level / 5.0` with no integer truncation.
- `MonsterStatsCalculator.Calculate(MonsterDefinition, Rarity, MonsterIvGrade, int level, MonsterStatConfig) -> MonsterStats` consumes explicit prototype factors; IV multipliers come from GDD §13 (`D=.8, C=.9, B=1, A=1.1, S=1.2, SS=1.3, SSS=1.5`).
- `TrainerAttributes` exposes Dexterity, Luck, Endurance and Leadership with seeded, configurable prototype defaults; `TrainerClass` is `None|Medic|Commander|Engineer|Trapper`, defaults to `None` until Academy progression in Sub-project 6. No Trainer has HP.
- `TrainerProgression.AddExperience(Trainer trainer, long amount, TrainerProgressionConfig config) -> IReadOnlyList<TrainerLevelChanged>` advances Trainer Lv to at most 100 using a named Prototype XP curve; Rebirth stays in Sub-project 6.

- [ ] **Step 1: Write failing boundary tests** for Rank I–V at Trainer Lv 1/5/6/100, Monster level on Rank change without demotion, each IV multiplier, positive integer HP, deterministic Trainer attributes, class default, XP threshold crossing and level cap 100.
- [ ] **Step 2: Run focused tests and confirm expected failure:** `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~MonsterProgressionTests`.
- [ ] **Step 3: Implement formulas, seeded attributes/class and XP progression** with all scaling factors in `SimConfig`/catalog inputs, never species-specific arithmetic branches. Awarded XP does not create Monster XP.
- [ ] **Step 4: Run focused tests and the full Domain test suite.**
- [ ] **Step 5: Commit** as `feat(domain): model monster stats and level conversion`.

### Task 3: Rebellion score uses the GDD level scale

**Files:**
- Modify: `src/Game.Domain/RebellionModel.cs`
- Modify: `src/Game.Domain/Monsters/MonsterProgression.cs`
- Test: `tests/Game.Domain.Tests/RebellionAndFinanceTests.cs`

**Interfaces:**
- Add rank-aware overloads `LeadershipScore(int trainerRank, int trainerLevel, Rarity trainerRarity, double bonus = 0)` and `IsRebellious(int monsterLevel, Rarity monsterRarity, int trainerRank, int trainerLevel, Rarity trainerRarity, double bonus = 0)`.
- Keep an obsolete compatibility overload only until all callers migrate; production callers use `MonsterProgression.TrainerManagementLevel`.

- [ ] **Step 1: Add tests** proving equal-rarity same-rank Monsters do not rebel solely due to level rounding, and one/two Rarity gaps obey K=20 plus leadership bonus.
- [ ] **Step 2: Run `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~RebellionAndFinanceTests` and observe the new assertions fail.**
- [ ] **Step 3: Implement rank-aware score calculation** using the existing `K=20`, leadership base 20 and explicit bonuses for Academy/communication lock.
- [ ] **Step 4: Run focused and full Domain tests.**
- [ ] **Step 5: Commit** as `fix(domain): calculate rebellion on converted trainer level`.

### Task 4: Element chart and data-driven skill catalog

**Files:**
- Create: `src/Game.Domain/Combat/ElementChart.cs`
- Create: `src/Game.Domain/Combat/SkillDefinition.cs`
- Create: `src/Game.Domain/Combat/SkillCatalog.cs`
- Create: `src/Game.Domain/Combat/CombatConfig.cs`
- Test: `tests/Game.Domain.Tests/ElementChartTests.cs`
- Test: `tests/Game.Domain.Tests/SkillCatalogTests.cs`

**Interfaces:**
- `ElementChart.Multiplier(MonsterElement attack, MonsterElement defense) -> double` returns only 0.5, 1, or 2 from the exact GDD 04 matrix.
- `SkillDefinition` contains stable ID, element, power, cooldown-in-actions, target rule and optional effect ID. `SkillCatalog` rejects duplicate IDs and unknown references.
- Prototype skills and all power/cooldown/effect values are configuration/catalog data; the element chart is GDD-defined and must match all 81 cells.

- [ ] **Step 1: Write failing tests** for all 81 matchup cells, reciprocal light/dark, no zero multiplier, invalid enum inputs and duplicate skill IDs.
- [ ] **Step 2: Run `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~ElementChartTests` and observe failure.**
- [ ] **Step 3: Implement the literal 9×9 chart and validated skill catalog.**
- [ ] **Step 4: Run focused and full Domain tests.**
- [ ] **Step 5: Commit** as `feat(domain): add monster elements and skill catalog`.

### Task 5: Deterministic battle resolver and replay record

**Files:**
- Create: `src/Game.Domain/Combat/BattleInput.cs`
- Create: `src/Game.Domain/Combat/BattleAction.cs`
- Create: `src/Game.Domain/Combat/BattleResult.cs`
- Create: `src/Game.Domain/Combat/BattleResolver.cs`
- Modify: `src/Game.Domain/Combat/CombatConfig.cs`
- Test: `tests/Game.Domain.Tests/BattleResolverTests.cs`
- Test: `tests/Game.Domain.Tests/BattleReplayTests.cs`

**Interfaces:**
- `BattleResolver.Resolve(BattleInput input, CombatConfig config, SimRandom random) -> BattleResult` consumes immutable snapshots and returns ordered actions plus final per-Monster state; it does not mutate `HubWorld` or call Unity.
- `BattleAction` records sequence/turn, actor/skill/target IDs, damage/heal, effectiveness, resulting HP, swap, faint and rebellion outcome.
- Prototype integer damage formula is `max(1, floor((power * attacker.Attack / max(1, defender.Defense)) * effectiveness * criticalMultiplier))`; constants `power` and `criticalMultiplier` are workbook inputs. Add only if no more specific GDD rule exists.
- Resolve tied action initiative by stable Monster ID. Cooldowns decrement once per completed action round; use occurs only at zero.

- [ ] **Step 1: Write failing tests** for damage floor, crit multiplier, elemental multiplier application, target tie-break, cooldown ready/blocked/ready cycle, identical seed replay and log-to-final-state consistency.
- [ ] **Step 2: Run `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~BattleResolverTests` and confirm feature failures.**
- [ ] **Step 3: Implement the resolver and replay log** over copied battle state; do not mutate input snapshots.
- [ ] **Step 4: Run focused and full Domain tests.**
- [ ] **Step 5: Commit** as `feat(domain): resolve deterministic monster battles`.

### Task 6: Team combat, Swap, faint and Rebellion outcomes

**Files:**
- Modify: `src/Game.Domain/Combat/BattleResolver.cs`
- Modify: `src/Game.Domain/Combat/BattleAction.cs`
- Modify: `src/Game.Domain/Combat/BattleResult.cs`
- Modify: `src/Game.Domain/Combat/CombatConfig.cs`
- Test: `tests/Game.Domain.Tests/BattleResolverTests.cs`

**Interfaces:**
- Extend battle input with Active/Reserve roster, Trainer Leadership, Monster management scores and synergy/item modifiers.
- After an action that leaves Active HP strictly below 15% of max and above zero, `BattleResolver` swaps to the lowest stable-ID eligible living Reserve before the next action. At 0 HP it faints; a living Reserve may take over. All three faint means team loss and `BattleResult.TeamDown=true`.
- Rebellion outcome is a seeded action per Monster per encounter; allowed outcomes are obey, skip/sleep, or area aggro attack. Chance/formulas are explicit Prototype inputs.

- [ ] **Step 1: Write failing tests** for HP 14.99% Swap, exactly 15% no Swap, 0 HP faint without death, subsequent Reserve participation, all-faint team loss, each Rebellion outcome and deterministic tie-break.
- [ ] **Step 2: Run the focused test and confirm failures.**
- [ ] **Step 3: Implement team-state transitions and action-log entries** without changing the GDD thresholds.
- [ ] **Step 4: Run `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~BattleResolverTests`, then full suite.**
- [ ] **Step 5: Commit** as `feat(domain): add monster swaps fainting and rebellion actions`.

### Task 7: Zone catalog, unlock input and Zone selection

**Files:**
- Create: `src/Game.Domain/World/ZoneDefinition.cs`
- Create: `src/Game.Domain/World/ZoneCatalog.cs`
- Create: `src/Game.Domain/World/ZoneSelector.cs`
- Create: `src/Game.Domain/World/EncounterProfile.cs`
- Modify: `src/Game.Domain/Config/SimConfig.cs`
- Test: `tests/Game.Domain.Tests/ZoneCatalogTests.cs`
- Test: `tests/Game.Domain.Tests/ZoneSelectorTests.cs`

**Interfaces:**
- `ZoneDefinition` contains stable ID, display name, minimum Rank, walk minutes, family/tier material weights, encounter profile and day/night modifier.
- `ZoneSelector.Select(TrainerSnapshot trainer, IReadOnlyList<ZoneDefinition> unlockedZones, IReadOnlyList<ZoneIncomeModifier> activeModifiers) -> ZoneDefinition` chooses the greatest expected Gold-equivalent per hour; Rank and unlock checks run before scoring; ties resolve by Zone ID. Sub-project 6 maps live Bounty Board orders into modifiers.
- Default HubWorld unlock input is Zone 1 until Sub-project 6 owns progression. Game.Sim may pass all zones explicitly to exercise the selector. Tiers match GDD map: Zone1 Grass, Zone2 Volcano, Zone3 Ice, Zone4 Swamp, Zone5 Abyss; Zone N requires Rank ≥ N.

- [ ] **Step 1: Write failing tests** for rank filtering, locked-zone filtering, all-ineligible result, income maximization, Capitalist bounty preference and stable tie-break.
- [ ] **Step 2: Run `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~ZoneSelectorTests` and observe expected failures.**
- [ ] **Step 3: Implement data-driven Zone definitions and selector**; weights and scores not fixed by GDD are Prototype inputs.
- [ ] **Step 4: Run focused and full Domain tests.**
- [ ] **Step 5: Commit** as `feat(domain): add zone catalog and trainer zone choice`.

### Task 8: Encounter generation, typed loot and EXP resolution

**Files:**
- Create: `src/Game.Domain/Combat/EncounterGenerator.cs`
- Create: `src/Game.Domain/World/ExpeditionResult.cs`
- Create: `src/Game.Domain/World/LootResolver.cs`
- Modify: `src/Game.Domain/Materials/MaterialCatalog.cs`
- Modify: `src/Game.Domain/Config/SimConfig.cs`
- Test: `tests/Game.Domain.Tests/EncounterGeneratorTests.cs`
- Test: `tests/Game.Domain.Tests/LootResolverTests.cs`

**Interfaces:**
- `EncounterGenerator.Generate(ZoneDefinition zone, SimTime time, SimRandom rng) -> EncounterDefinition` uses the Zone’s encounter weights and stable catalog IDs.
- `LootResolver.Resolve(BattleResult battle, ZoneDefinition zone, TrainerSnapshot trainer, SimTime time, LootConfig config, SimRandom rng) -> ExpeditionLoot` returns typed raw materials, external Gold and Trainer EXP separately.
- Zone N material weighting must favor tier N while allowing configured lower-tier spillover; Night Vision at night applies ×2 to loot/EXP exactly once. Pickup roll uses personality `MaterialPickRate`; Gold is always picked and scales with Luck.
- Capacity truncation returns both collected and dropped units by material ID; each quantity is non-negative and their sum equals generated loot.

- [ ] **Step 1: Write failing tests** for weighted zone generation determinism, tier priority/spillover, personality pickup, guaranteed Gold, Luck scaling, nighttime ×2 and exact backpack-capacity conservation.
- [ ] **Step 2: Run focused tests and confirm failures:** `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~LootResolverTests`.
- [ ] **Step 3: Implement generator/resolver** with all missing rates in named config properties and no fallback to `ore_tier_1`.
- [ ] **Step 4: Run focused and full Domain tests.**
- [ ] **Step 5: Commit** as `feat(domain): resolve zone encounters and typed loot`.

### Task 9: Connect expeditions to HubWorld and remove aggregate HP farm

**Files:**
- Create: `src/Game.Domain/World/IExpeditionResolver.cs`
- Modify: `src/Game.Domain/World/FarmAndMarket.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Trainers.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Views.cs`
- Modify: `src/Game.Domain/Hub/HubWorldTypes.cs`
- Modify: `src/Game.Domain/Trainers/TrainerBrain.cs`
- Modify: `src/Game.Domain/Trainers/Trainer.cs`
- Test: `tests/Game.Domain.Tests/HubWorldMonsterTests.cs`
- Test: `tests/Game.Domain.Tests/TrainerBrainTests.cs`

**Interfaces:**
- `IExpeditionResolver.Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random) -> ExpeditionResult` is the new field-work port. `ExpeditionResult` contains ordered battles, typed loot, XP, and final Monster state. Replace `IFarmResolver` and `SimpleFarmResolver`; migrate all in-repo adapters/tests and remove the aggregate-HP path.
- Keep `HubWorld(SimConfig config, int seed)` and `HubWorld(SimConfig config, int seed, IExpeditionResolver expeditionResolver, IMaterialMarket materialMarket)` as the only constructors; do not add same-arity overloads that make `(null, null)` ambiguous.
- `HubWorld` selects an unlocked Zone on each farm decision, stores `Trainer.CurrentZoneId`, applies walk duration/need decay, applies battle state/loot/XP in stable order, and returns to HUB when all physical needs hit personality threshold, backpack is full, team is down, or the GDD night rule applies.
- `TrainerView` exposes per-Monster views; `TeamHp` aggregate is removed after all consumers migrate. `ShouldReturn` reads roster states, not an aggregate scalar.

- [ ] **Step 1: Write failing HubWorld tests** for default one-Monster team return, typed material yields from multiple Zones, HP per Monster, EXP level-up, all-team-down return, walk need decay, Night Vision ×2, no-glasses Dusk return and per-Monster view.
- [ ] **Step 2: Run focused tests and confirm the expected failures.**
- [ ] **Step 3: Replace the default `SimpleFarmResolver` path with `IExpeditionResolver`**, migrate `FixedFarm`/`TypedFarm` tests to expedition fixtures, and update HubWorld dispatch/state/views; preserve event queue one-pending-event invariants.
- [ ] **Step 4: Run focused HubWorld/Trainer tests and full Domain suite.**
- [ ] **Step 5: Commit** as `feat(domain): run monster expeditions through hub world`.

### Task 10: Trainer product inventory and atomic purchases

**Files:**
- Create: `src/Game.Domain/Trainers/TrainerInventory.cs`
- Create: `src/Game.Domain/Trainers/ConsumablePolicy.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Supply.cs`
- Modify: `src/Game.Domain/Supply/ConsumableStall.cs`
- Modify: `src/Game.Domain/Materials/ProductDefinition.cs`
- Modify: `src/Game.Domain/Config/SimConfig.cs`
- Test: `tests/Game.Domain.Tests/TrainerInventoryTests.cs`
- Test: `tests/Game.Domain.Tests/HubWorldConsumableTests.cs`
- Test: `tests/Game.Domain.Tests/WorkshopAndStallTests.cs`

**Interfaces:**
- `TrainerInventory` stores positive counts keyed by `ProductId` and supports atomic `TryConsume`/`Add`; each Monster item remains owned by its Trainer.
- Add `HubWorld.PurchaseProduct(int trainerId, string productId, int units) -> CommandResult`; it validates Trainer, stall/catalog mapping, current available stock and cash, then atomically debits Trainer, credits HUB, decrements product stock and records money/stock events.
- `ConsumablePolicy.DecidePurchases(TrainerSnapshot, HubStockSnapshot, CombatRiskSnapshot, ConsumablePolicyConfig) -> IReadOnlyList<ProductPurchase>` chooses finite stock quantities according to need/affordability/personality. No scripted free inventory.

- [ ] **Step 1: Write failing tests** for exact cash/stock/ledger changes, invalid product, no stock, unaffordable/partial quantity, duplicate item stack and overflow-safe counts.
- [ ] **Step 2: Run focused tests and observe the failures.**
- [ ] **Step 3: Implement inventory and atomic purchase** so a rejected purchase changes none of money, stock, inventory or ledger.
- [ ] **Step 4: Run focused tests and full Domain suite.**
- [ ] **Step 5: Commit** as `feat(domain): add trainer consumable inventory and purchase`.

### Task 11: Monster item behavior and AI use policy

**Files:**
- Create: `src/Game.Domain/Combat/MonsterItemEffects.cs`
- Modify: `src/Game.Domain/Combat/BattleResolver.cs`
- Modify: `src/Game.Domain/Trainers/ConsumablePolicy.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Trainers.cs`
- Modify: `src/Game.Domain/Materials/MaterialCatalog.cs`
- Test: `tests/Game.Domain.Tests/MonsterItemEffectsTests.cs`
- Test: `tests/Game.Domain.Tests/ConsumablePolicyTests.cs`

**Interfaces:**
- `MonsterItemEffects.Apply(ProductId item, Monster target, MonsterItemConfig config) -> ItemEffectResult` models Potion, temporary stat bottle, reward cake/Rebellion reduction, tactics-book synergy and communication lock/leadership bonus.
- AI buys and uses items only when a configurable expected-value rule and sufficient cash/stock permit. Potion is consumed only when injured; reward cake/communication lock only when Rebellion threshold is exceeded; tactics book enables declared team synergy. Item effects expire on the configured time/action boundary.
- Exact amounts, duration, prices and purchase propensity absent from GDD are Prototype/TBD workbook inputs.

- [ ] **Step 1: Write failing tests** for each listed item effect, use threshold, quantity decrement, effect expiration and no-use at full HP/no Rebellion.
- [ ] **Step 2: Run focused tests and confirm missing effects.**
- [ ] **Step 3: Implement item effects and AI policy** using inventory and purchase API from Task 10.
- [ ] **Step 4: Run focused and full Domain tests.**
- [ ] **Step 5: Commit** as `feat(domain): simulate monster consumable use`.

### Task 12: Capture decisions and attempt resolution

**Files:**
- Create: `src/Game.Domain/Combat/CaptureResolver.cs`
- Create: `src/Game.Domain/Combat/CaptureResult.cs`
- Modify: `src/Game.Domain/Combat/EncounterDefinition.cs`
- Modify: `src/Game.Domain/Trainers/ConsumablePolicy.cs`
- Test: `tests/Game.Domain.Tests/CaptureResolverTests.cs`

**Interfaces:**
- `CaptureResolver.ShouldAttempt(MonsterDefinition target, MonsterRoster roster, TrainerAttributes trainer, TrainerInventory inventory, CaptureConfig config) -> bool` checks a Ball, weakened-target threshold and replacement value.
- `CaptureResolver.Resolve(MonsterSnapshot target, CaptureInputs inputs, CaptureConfig config, SimRandom random) -> CaptureResult` includes chance inputs, roll outcome, consumed Ball/Trap counts and captured Monster generation inputs.
- `ReplacementScore` identifies the weakest roster member by derived power, then stable ID. `CaptureResolver.ShouldAttempt` accepts a wild candidate only when its Rarity is greater than that member's Rarity **or** its derived power is greater; keep the GDD OR rule rather than a rarity-first comparison.
- Balls are mandatory; traps, Dexterity, `TrainerClass.Trapper` and target rarity feed the capture formula. Prototype formula/rates are named config values.

- [ ] **Step 1: Write failing tests** for no-Ball rejection, not-weakened skip, rarity/strength replacement criteria, each probability input and deterministic success/failure/item consumption.
- [ ] **Step 2: Run `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~CaptureResolverTests` and observe expected failure.**
- [ ] **Step 3: Implement resolver and result record** without directly mutating HUB inventory or roster.
- [ ] **Step 4: Run focused and full Domain tests.**
- [ ] **Step 5: Commit** as `feat(domain): resolve monster capture attempts`.

### Task 13: Veterinary Hospital and Gene Bank ownership lifecycle

**Files:**
- Create: `src/Game.Domain/Monsters/VeterinaryHospital.cs`
- Create: `src/Game.Domain/Monsters/GeneBank.cs`
- Create: `src/Game.Domain/Events/MonsterEvents.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Trainers.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Payday.cs`
- Modify: `src/Game.Domain/Events/DomainEvents.cs`
- Modify: `src/Game.Domain/Simulation/EventQueue.cs`
- Test: `tests/Game.Domain.Tests/VeterinaryHospitalTests.cs`
- Test: `tests/Game.Domain.Tests/GeneBankTests.cs`
- Test: `tests/Game.Domain.Tests/HubWorldMonsterTests.cs`

**Interfaces:**
- `VeterinaryHospital.RequestEmergencyCare(MonsterId, int now) -> AdmissionResult` and `AdmitCaptured(Monster, int now) -> AdmissionResult` model emergency team recovery and exactly 100 recovery beds for captured Monsters. Recovery completion uses event-queue ordering and charges configured first-recovery/faint surcharges.
- `GeneBank.Store(trainerId, MonsterId)`, `Withdraw`, `CalculatePaydayFee(trainerId, days)`, `ConfiscateForUnpaidFee(trainerId) -> Monster?`, `ResellToTrainer(buyerId, MonsterId)`, and `Dismantle` transfer one owner at a time. Fee is 20 Gold/Monster/day × 30 days at Payday as the GDD prototype; shortage confiscates exactly one selected Monster. Bank capacity/selection rule/resale price are explicit Prototype settings.
- Sub-project 3 adds fee quote/confiscation events and a `HubWorld` fee-assessment hook after wages; it does not settle the fee or create a second payroll. Sub-project 5 owns cash settlement/debt priority and calls this hook.

- [ ] **Step 1: Write failing tests** for 100th/101st recovery admission, full-bed wait/store decision, emergency healing/faint surcharge, first-capture recovery duration, bank storage/withdraw/resale ownership, fee assessment after wages, fee calculation and one-only confiscation.
- [ ] **Step 2: Run focused tests and verify the specified missing behavior.**
- [ ] **Step 3: Implement hospital, bank ownership transitions and timestamped events**; schedule recovery completions through `EventQueue` with Trainer token rules.
- [ ] **Step 4: Run focused tests and the full Domain suite.**
- [ ] **Step 5: Commit** as `feat(domain): model veterinary and gene bank lifecycle`.

### Task 14: Appraisal, dismantling, rarity upgrade and evolution

**Files:**
- Create: `src/Game.Domain/Monsters/GeneticLab.cs`
- Create: `src/Game.Domain/Monsters/EvolutionCatalog.cs`
- Modify: `src/Game.Domain/Monsters/Monster.cs`
- Modify: `src/Game.Domain/Monsters/GeneBank.cs`
- Modify: `src/Game.Domain/Materials/MaterialCatalog.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Commands.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Supply.cs`
- Modify: `src/Game.Domain/Supply/Station.cs`
- Test: `tests/Game.Domain.Tests/GeneticLabTests.cs`
- Test: `tests/Game.Domain.Tests/EvolutionTests.cs`

**Interfaces:**
- `GeneticLab.Appraise(MonsterId) -> AppraisalResult` reveals fixed IV once and charges configured service price. `Dismantle(MonsterId) -> DismantleResult` consumes an eligible Monster and credits configured Gene Fragments exactly once to Trainer inventory.
- Add product buyback support to `HubWorld.Supply`/`Station` for `gene_fragment`, because GDD says Fragments can be sold to the Station; use an explicit ProductId request, tax and the existing money/inventory ledger.
- `UpgradeRarity(MonsterId, ProductId protectionCharm, SimRandom rng) -> UpgradeResult` requires level ≥40, uses success chances 60/40/25/10% by step and the `UpgradeLadder` data; failure consumes cost unless protection preserves inputs.
- `EvolutionCatalog` declares species-specific 0–2 steps and branches. `Evolve(MonsterId, branchId, protectionCharm, rng)` consumes Stone/Core/partial Gene Fragments; success preserves rarity/level/IV/gear references and changes form/role/skills; failure follows protection rules.
- No Monster, material or item changes on rejected/insufficient-input commands. Evolution probabilities/costs/branches not specified in GDD are Prototype/TBD.

- [ ] **Step 1: Write failing tests** for hidden/revealed IV, duplicate appraisal, D/C dismantle yield, level 39/40 eligibility, all four rarity rates, charm-preserved costs, 0/1/2 evolution limits, branch selection and preserved identity fields.
- [ ] **Step 2: Run focused tests and confirm failures.**
- [ ] **Step 3: Implement genetic lab transactions** with validated inventory/stock/cost operations and deterministic randomness.
- [ ] **Step 4: Run focused and full Domain tests.**
- [ ] **Step 5: Commit** as `feat(domain): simulate monster appraisal and evolution`.

### Task 15: Hub commands, views, invariants and end-to-end scenarios

**Files:**
- Modify: `src/Game.Domain/Hub/HubWorld.Commands.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Supply.cs`
- Modify: `src/Game.Domain/Supply/Station.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Views.cs`
- Modify: `src/Game.Domain/Hub/HubWorldTypes.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Trainers.cs`
- Modify: `src/Game.Domain/Hub/HubWorld.Payday.cs`
- Create: `tools/Game.Sim/MonsterScenarios.cs`
- Modify: `tools/Game.Sim/Program.cs`
- Test: `tests/Game.Domain.Tests/HubWorldMonsterTests.cs`

**Interfaces:**
- Add read-only `MonsterView`, `ZoneView`, Veterinary/GeneBank views and `HubWorld.MonstersForTrainer(int trainerId)`; snapshots never expose mutable entities.
- Add Director commands for roster swap, storage/withdraw, appraisal/dismantle, upgrade/evolve, and active Zone unlock input where allowed. Invalid player commands return `CommandResult.Rejected`.
- `tools/Game.Sim` gains `monster` and `expedition` scenarios using exactly the same `HubWorld` resolver; print per-Zone encounters, wins/losses, swaps, faint/recovery, captures, appraisal/evolution, XP/levels, item flow and ownership/Gold reconciliation.
- `HubWorld.ValidateInvariants` checks unique Monster ownership, max roster/storage, one Active per nonempty field roster, valid HP/state, no orphaned recovery/bank events and money non-negativity.

- [ ] **Step 1: Write failing end-to-end tests** for same-seed world/event-stream equality, Director commands, view immutability, ownership invariant and Payday fee contract event ordering.
- [ ] **Step 2: Run focused tests and confirm missing integration.**
- [ ] **Step 3: Integrate commands/views/events and add deterministic `monster`/`expedition` Game.Sim scenarios.**
- [ ] **Step 4: Run focused and full Domain tests; run `dotnet run --project tools/Game.Sim -- monster` and `-- expedition`; verify ledgers reconcile and runtime is reported.**
- [ ] **Step 5: Commit** as `feat(sim): expose monster lifecycle scenarios`.

### Task 16: Balance workbook, GDD traceability and final verification

**Files:**
- Modify: `docs/balance/MonsterHUB_Balance.xlsx`
- Modify: `docs/designs/04_Monster_System.md`
- Modify: `docs/designs/12_Item_Catalog.md`
- Modify: `docs/designs/13_Balance_Parameters.md`
- Modify: `docs/99_Open_Issues.md`
- Modify: `README.md`
- Create: `docs/superpowers/sdd/2026-10-02-domain-monster-combat/task-report.md`
- Test: full `tests/Game.Domain.Tests` suite and Game.Sim scenarios from Task 15

**Interfaces:**
- Workbook additions reuse existing `Parameters`, `Domain Config`, `Product Catalog`, `Material Catalog` and `Backlog` where rows suffice. Add structured `Monster Catalog`, `Combat & Capture`, and `Zones & Loot` sheets where distinct tabular data needs its own build; every row carries stable ID, value, unit, status and source.
- Runtime defaults, catalog tables, spec decisions and report must agree. Keep locked GDD inputs as `Locked`/existing GDD status; mark inferred numeric inputs `Prototype`; leave unresolved values `TBD`.
- Update docs only for implemented behavior and retain cross-project dependencies (Gene Bank Payday settlement → Sub-project 5; Zone unlock/events → Sub-project 6; gear wear → Sub-project 4).

- [ ] **Step 1: Add workbook rows and structured catalogs** with `@oai/artifact-tool`; before the first authoring command run `node container_tools/mark_artifact_operation_started.mjs --operation-kind edit --expected-output-count 1 --output-format xlsx` exactly once. Keep a single reproducible `.mjs` builder in task scratch space and save only the requested workbook.
- [ ] **Step 2: Reopen the saved workbook read-only** and verify sheet names, parameter IDs, values, units, statuses, sources, formulas/references and no duplicate/conflicting runtime inputs.
- [ ] **Step 3: Update GDD parameter notes, O7/O10/O11 statuses, README Domain progress and task report** with behavior implemented, explicit prototype gaps and numeric IDs.
- [ ] **Step 4: Run `dotnet test tests/Game.Domain.Tests`; run `dotnet run --project tools/Game.Sim -- monster`, `-- expedition`, `-- core`, and `-- market`; confirm same-seed event hashes, inventory ownership and Gold/material ledgers reconcile.**
- [ ] **Step 5: Run `git diff --check`, inspect the complete requirement-to-test map against the approved spec and coverage map, then commit** as `docs(balance): document monster simulation parameters`.

## Handoff and execution

The user selected subagent-driven development earlier. Use one fresh implementation subagent per task, then an independent fresh reviewer for each task before proceeding; tasks are sequential because later interfaces depend on earlier ones. Work only in the current `codex/domain-monster-combat` worktree. Do not merge/push as part of these tasks; integration/push is a separate user instruction after the complete sub-project is reviewed.
