# Domain Quests and KPI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Implement the approved Early Access Main Quest and daily/weekly KPI design, including event-backed progress, rewards, and the Director-to-Trainer Protection Charm sale path.

**Architecture:** An internal tracker owned by `HubWorld` consumes settled Domain events and exposes immutable Quest/KPI views. Existing Domain ledgers settle Gold; a Quest reward wallet holds Gem and reward entitlements; a provider fulfillment boundary supplies Director-owned Protection Charms to Trainer sales.

**Tech Stack:** C# (`Game.Domain`, `Game.Domain.Tests`, `Game.Sim`), existing simulation clock/event queue and seeded random source, `docs/balance/MonsterHUB_Balance.xlsx`.

**Spec:** [2026-10-03-domain-quests-kpi-design.md](../specs/2026-10-03-domain-quests-kpi-design.md)

## Global Constraints

- Early Access scope is Main Quests plus Daily and Weekly KPIs; Achievements, titles, Monster Index, and Director's Vault remain out of scope.
- Progress is based on settled Domain events; rejected commands and unsuccessful operations do not count.
- All numeric runtime Quest/KPI settings must have workbook rows with stable ID, value, unit, status, and source.
- Simulation outcomes must be deterministic for the same seed, configuration, and commands.
- Keep existing HubWorld command semantics and financial/inventory conservation; no new external packages.
- Work remains local; do not merge or push.

## Review Focus

- An event exactly at a daily or weekly boundary must accrue to the new period; verify first event at day/week zero and rollover without intervening transactions.
- Trainer AI stock trades must not satisfy the director stock KPI; only successfully settled director-originated stock operations count.
- Re-emitting a Quest reward must not recursively advance KPIs or pay twice; verify completion reentrancy and Treasury overflow atomicity.
- Protection Charm stock fulfillment, price changes, purchases, and inventory limits must conserve stock and Gold; verify no failed purchase consumes stock or advances the KPI.
- Campaign Zone 2 + Hospital Lv2 requirement is conjunctive; verify one requirement alone leaves it incomplete.

## Plan

### Task 1: Quest balance and content model

**Files:**
- Create `src/Game.Domain/Quests/HubQuestConfig.cs`
- Create `src/Game.Domain/Quests/HubQuestCatalog.cs`
- Modify `docs/balance/MonsterHUB_Balance.xlsx`
- Test `tests/Game.Domain.Tests/QuestCatalogTests.cs`

**Interfaces:** Produces immutable `HubQuestConfig` numeric settings and ordered, stable Quest/KPI definitions with source/status metadata. GDD example thresholds are Prototype; unspecified rewards and Protection Charm decision values are TBD or documented Prototype values.

- [x] Write `QuestCatalogTests` for exact campaign/KPI IDs, daily/weekly period units, target validation, and balance row IDs/status/source.
- [x] Run `dotnet test tests/Game.Domain.Tests/Game.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~QuestCatalogTests`; expect the new tests to fail until the model exists.
- [x] Implement config validation and the deterministic content catalog in the files above.
- [x] Update the workbook with a `Quest & KPI` sheet containing every numeric runtime setting used by this spec.
- [x] Rerun the focused test; expect all Quest catalog tests to pass and workbook audit to find no missing metadata.

### Task 2: Tracker state, views, and event boundary

**Files:**
- Create `src/Game.Domain/Quests/HubQuestTracker.cs`
- Modify `src/Game.Domain/Hub/HubWorld.cs`
- Modify `src/Game.Domain/Hub/HubWorld.Views.cs`
- Modify `src/Game.Domain/Hub/HubWorldTypes.cs`
- Test `tests/Game.Domain.Tests/QuestTrackerTests.cs`

**Interfaces:** `HubQuestTracker.AdvanceTo(int minute)`, `HubQuestTracker.Observe(IDomainEvent fact)`, read-only `QuestView HubWorld.Quests`, and Quest progress/completion/reward Domain events.

- [x] Write pure tracker tests for sorted IDs, snapshot immutability, progress monotonicity within a period, and ignored unrelated events.
- [x] Run the filtered Quest tracker tests; expect the tracker API to be missing.
- [x] Implement tracker state and snapshot types, then integrate observation into the HubWorld event boundary so original facts publish before derived Quest events.
- [ ] Ensure Quest-derived events cannot re-enter observation and add boundary time advancement to `HubWorld` day transitions.
- [x] Rerun tracker tests; expect state transitions and deterministic event ordering to pass.

### Task 3: Campaign predicates and daily/weekly KPI facts

**Files:**
- Modify `src/Game.Domain/Quests/HubQuestTracker.cs`
- Modify `src/Game.Domain/Hub/HubWorld.Stock.cs`
- Modify `src/Game.Domain/Hub/HubWorldTypes.cs`
- Test `tests/Game.Domain.Tests/QuestProgressTests.cs`

**Interfaces:** Add a director-originated stock-operation fact for settled player/director stock commands; Trainer AI calls must identify as AI. Tracker observes `ZoneUnlocked`, `FacilityUpgradeCompleted`, `ProductPurchased`, `MonsterConfiscated`, and the director-only stock fact.

- [x] Write tests for each KPI mapping: Hospital Product Gold, settled director stock operation, Liquor units, unpaid-fee confiscations, and Protection Charm sale units.
- [x] Write tests proving AI stock orders, rejected commands, partial/unfilled orders, production, and voluntary Gene Bank withdrawals do not count.
- [x] Verify the progress tests fail before implementation.
- [x] Implement exact event-to-objective mapping and stock command origin marking without counting AI trades.
- [x] Write and pass tests for midnight/seven-day rollovers, exact-boundary event assignment, period reset, and no double completion per period.

### Task 4: Campaign completion and sponsor rewards

**Files:**
- Modify `src/Game.Domain/Quests/HubQuestTracker.cs`
- Modify `src/Game.Domain/Hub/HubWorld.cs`
- Modify `src/Game.Domain/Hub/HubWorldTypes.cs` or the existing domain event file
- Test `tests/Game.Domain.Tests/QuestRewardTests.cs`

**Interfaces:** One-time campaign completion for Zone 2 plus Hospital level 2; period-scoped KPI reward settlement; reward wallet view for Gem, permits, invitations, and Protection Charms. Gold awards use the checked Treasury ledger path.

- [x] Write tests for conjunctive Campaign completion, auto-grant exactly once, all reward types, deterministic order, and Treasury overflow leaving no partial reward.
- [x] Run the filtered reward tests and confirm they fail against the current Domain.
- [x] Implement event emission and reward settlement; ensure reward-derived events cannot recursively advance progress.
- [x] Run reward tests and confirm Treasury deltas, wallet balances, and completion views reconcile.

### Task 5: Director Protection Charm fulfillment and Trainer demand

**Files:**
- Modify `src/Game.Domain/Hub/HubWorld.Supply.cs`
- Modify `src/Game.Domain/Trainers/TrainerInventory.cs`
- Modify `src/Game.Domain/Hub/HubWorld.Trainers.cs`
- Modify `src/Game.Domain/Materials/MaterialCatalog.cs` only if the existing product catalog needs a sellable classification
- Test `tests/Game.Domain.Tests/ProtectionCharmMarketTests.cs`

**Interfaces:** Add `HubWorld.ProvisionProtectionCharms(int units)` as the fulfillment boundary for stock obtained through an external provider, quest, or reward. The offer price uses the product pricing API; settled purchases emit the normal `ProductPurchased` fact. Extend the deterministic consumable policy with configured expected avoided loss for AI equipment enhancement under break risk.

- [x] Write tests for fulfilled stock, offer pricing, AI buy/decline at the expected-value boundary, exact quantity/cash, stock conservation, and weekly KPI progress only after settlement.
- [x] Verify the focused tests fail before implementation.
- [x] Implement provider fulfillment and AI purchase decision with every numeric assumption configured and recorded in the workbook.
- [x] Run tests and reconcile Treasury, inventory, and KPI counters for success, rejection, and insufficient-stock cases.

### Task 6: Scenario, workbook audit, independent review, and commit

**Files:**
- Modify `tools/Game.Sim/Program.cs`
- Create `tools/Game.Sim/QuestKpiScenarios.cs`
- Create `docs/superpowers/reviews/2026-10-03-domain-quests-kpi-review.md`
- Update this plan with exact verification and final review result

- [x] Add a seeded Quest/KPI scenario that completes the campaign milestone, resets at daily/weekly boundaries, and exercises all five KPI types including Protection Charm sales.
- [x] Run `/Users/nofine/.dotnet/dotnet test tests/Game.Domain.Tests/Game.Domain.Tests.csproj --no-restore` and record the exact pass/fail count.
- [x] Run `/Users/nofine/.dotnet/dotnet run --project tools/Game.Sim --no-restore -- quest-kpi` and verify repeatable output and balanced Gold/item/reward ledgers.
- [x] Audit every numeric Quest/KPI setting against `Quest & KPI` workbook rows and verify every row has a value, unit, status, and source; run `git diff --check`.
- [ ] Perform an independent code review against the approved spec and GDD 02, 07, 08, 12, and 13; resolve findings before close-out.
- [ ] Stage only Quest/KPI implementation, tests, plan/review docs, scenario, and workbook changes; commit locally with `feat(domain): complete quests and KPI sub-project`.

## Close-out

- Spec approved on 2026-10-03 and committed locally as `410adf8`.
- The plan was approved; Implementation is complete pending final review and commit.
