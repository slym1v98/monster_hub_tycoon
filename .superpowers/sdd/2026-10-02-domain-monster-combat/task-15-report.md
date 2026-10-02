# Task 15 report — Hub integration and Monster lifecycle scenarios

Status: **DONE_WITH_CONCERNS**. All Task 15 implementation and verification steps completed. Concerns are the explicit scenario fixture choices, inherited warning/TDD provenance described below; no compile, runtime, reconciliation or test blocker remains.

Commit message: `feat(sim): expose monster lifecycle scenarios`. Task 15 checklist is fully checked; completion is recorded in the progress ledger.
Worktree: `/Users/nofine/.codex/worktrees/domain-monster-combat/MonterHUBTycoon`.
Branch: `codex/domain-monster-combat`.

## Scope and execution

Read Task 15's brief and relevant Domain, simulator and test source only. No prior task ledger history was consumed, no subagents were used, and all changes and generated logs are in the requested worktree. The worktree contained partial Task 15 implementation and tests on entry; those edits were preserved and completed. The checklist is updated in `task-15-brief.md`; the Task 15 completion entry is appended to `progress.md` without reading its existing contents.

## Implemented behavior

- `HubWorld.Views.cs` and `HubWorldTypes.cs`: detached, immutable Monster/Zone/VeterinaryHospital/GeneBank snapshots. Nested lists use read-only collections; product dictionaries are detached read-only copies. `MonstersForTrainer` combines field roster, local storage, that Trainer's bank deposits and capture recoveries in ordinal ID order. Emergency recoveries appear once through their roster membership. Invalid Trainer queries return an immutable empty collection. Monster views include rarity, known/appraised IV, custody and soul-bound identity. Bank custody displays the Stored life state.
- `HubWorld.Commands.cs`: Director swap, local storage/withdraw, bank deposit/withdraw, appraisal, dismantle, rarity upgrade, evolution and Zone unlock commands. Invalid IDs, ownership, state, capacity, costs and evolution branches reject through `CommandResult`; rejection does not consume inventory, move ownership, emit changes or advance the seeded future. Eligible failed upgrade/evolution/capture rolls remain successful commands with explicit failure events, chance and roll. Protection charms and inventory costs are delegated to the existing GeneticLab implementation.
- Explicit roster/appraisal/dismantle/upgrade/evolution/capture/unlock/expedition events expose changes and resolution results. Captures use the world's existing RNG, consume balls/traps explicitly and enter the same Veterinary recovery queue. Stable incoming IDs are retained through capture, recovery, banking, upgrade and evolution. Cross-service duplicate capture/deposit IDs reject before mutation.
- `HubWorld.Supply.cs` and `Station.cs`: configured non-stall products, including salvaged Gene Fragments, can be sold back and repurchased from real Station stock. Stock, Trainer inventory, Gold, treasury and the existing double-entry money ledger change exactly once. Consumable stalls retain their existing route. Unknown products, unavailable prices, insufficient stock/funds and overflow reject.
- `HubWorld.Trainers.cs`: empty rosters do not reach the expedition resolver; field Monsters in emergency recovery are unavailable for farming. Selected final battle Active IDs are applied to the roster. Expedition events include actual rewards, collected/dropped material lots, battle logs, Trainer XP and final level.
- `HubWorld.cs` and `VeterinaryHospital.cs`: read-only recovery inventory for views/invariants, global duplicate checking and eventual ownership-slot reservation while captures recover. Bank withdrawal and resale respect those reservations. Existing recovery scheduling and ownership transfer remain explicit and exactly once.
- `SimConfig.cs`: configurable local-storage safety limit, default 500, through `MonsterStorageConfig`. The limit is independent of Gene Bank capacity and has `Prototype` status, units and an explicit Task 15 source tag. Zero local capacity is supported and tested.
- `Monster.cs`: Gene Bank custody maps to Stored. HP, genes and identity are preserved across transfers.
- `HubWorld.Payday.cs`: the existing wage settlement → treasury event → `PaydayResolved` → Gene Bank fee assessment sequence is retained and covered end to end. Assessment does not collect fees or confiscate Monsters automatically. No unnecessary source change was made to this already-correct implementation.
- `MonsterScenarios.cs` and `Program.cs`: `monster` and `expedition` CLI modes run the same default HubWorld resolver, event queue and RNG. Tests reference Game.Sim to exercise the actual scenario runner.

## Invariants

`ValidateInvariants` now verifies:

1. Unique global Monster IDs and custody across all rosters, local storage, bank deposits, confiscated bank stock and capture recoveries.
2. At most three field members, a single valid Active reference in every nonempty roster, bounded local storage and Gene Bank capacity.
3. Trainer roster back-references, stored-position flags, valid identity/level, HP in `[0, MaxHp]`, positive MaxHp and correct custody for emergency recovery.
4. Capture recoveries reserve capacity until completion; hospital bed capacities hold; captured Monsters have no roster owner until transfer.
5. Bank deposits have exactly one known depositor and no roster owner; confiscated Monsters have no roster owner.
6. Every recovery has exactly one completion event at its designated time, and every completion event matches an existing recovery, including its timestamp and global event identity. Extra completions with a valid ID but wrong time are rejected as orphaned.
7. Nonnegative treasury, Trainer Gold and Merchant cash; balanced supply double-entry ledger. Existing needs/service/Trainer-event invariants remain in force.

## TDD and verification evidence

SDK: `/Users/nofine/.dotnet/dotnet` (the installed SDK is not on the shell's default PATH). Every command below used this executable in the specified worktree.

| Check | Result |
| --- | --- |
| `dotnet test tests/Game.Domain.Tests --filter FullyQualifiedName~HubWorldMonsterTests --nologo` | **25 passed, 0 failed**, 737 ms reported test duration |
| `dotnet test tests/Game.Domain.Tests --nologo` | **602 passed, 0 failed, 0 skipped**, approximately 3 seconds reported test duration |
| `dotnet run --project tools/Game.Sim -- monster` | Exit 0, runtime **450 ms** |
| `dotnet run --project tools/Game.Sim -- expedition` | Exit 0, runtime **484 ms** |
| `git diff --check` | Exit 0; no whitespace errors |

Observed red → green work in this session:

- Missing scenario runner first failed compilation. A throwing API stub then allowed the real tests to fail with `NotImplementedException`; both scenarios subsequently passed.
- Local storage overflow transfer initially returned success; the capacity and bank-ownership test now passes.
- Emergency-recovery appraisal initially succeeded; it now rejects without changing money. The initial test fixture needed sufficient Gold for admission; that setup was corrected before confirming the intended red failure.
- Pending capture capacity initially allowed an attempted capture and inventory spend; the new preflight now rejects before inventory/RNG/event changes.
- An extra recovery completion with a valid ID but wrong time initially escaped invariant validation; it is now detected.
- A bank snapshot initially reported Ready rather than Stored; it now reports Stored.
- Scenario assertions initially found no combat swaps/faints. A tagged scenario opponent-attack override now exercises swaps, faints and losses in both modes; all five survey Zones have nonzero encounters in expedition.

Controller independently confirmed focused HubWorldMonsterTests **25/25**, full Domain **602/602** with **0 warnings in that output**, and both required scenarios with reconciliation differences **0** and approximate runtimes **415 ms / 440 ms**. Test execution is complete; no additional rerun was performed after that confirmation.

Additional tests cover same-seed world snapshots and serialized event streams, deterministic scenario summaries excluding runtime, invalid command atomicity and identical seeded future, immutable nested collections and old snapshots, full lifecycle commands/costs/events, explicit capture inventory/recovery ownership, duplicate IDs across hospital/bank, corrupt HP/negative money/duplicate ownership, last-Monster removal while traveling, salvage trade and Payday fee ordering/assessment-only behavior.

The command/view portions already implemented on entry passed their regression tests before further changes here. Their original red runs were not witnessed in this session, so this report does not claim a fresh red-first implementation for inherited edits.

## Scenario results

Both fixtures use seed 2026 and simulate 30 days, with 2 Trainers for monster and 6 for expedition. The scenario prints status/source-tagged numeric fixture parameters and existing configuration metadata. Opponent Attack is overridden to 60 through `ExpeditionConfig`, preserving the existing battle algorithm and seed flow. Upgrade/evolution fixture probabilities are 1; upgrade fragment cost is 1 with zero Stone cost, and evolution consumes 3 Gene Fragments total. These deliberate fixture costs make every lifecycle branch reviewable without a long progression grind.

| Scenario / Zone | Encounters | Wins | Losses | Swaps | Team faints |
| --- | ---: | ---: | ---: | ---: | ---: |
| monster / zone_1 | 785 | 769 | 16 | 1 | 16 |
| expedition / zone_1 | 476 | 459 | 17 | 1 | 17 |
| expedition / zone_2 | 117 | 67 | 50 | 1 | 51 |
| expedition / zone_3 | 70 | 45 | 25 | 0 | 25 |
| expedition / zone_4 | 45 | 19 | 26 | 2 | 26 |
| expedition / zone_5 | 21 | 1 | 20 | 0 | 20 |

Monster keeps the default Zone catalog and starts only in Zone 1. Expedition uses explicitly tagged survey data with rank-one eligibility and increasing Gold incentives, unlocking one Zone every six days so all five themes are exercised without implementing Rebirth. HubWorld still checks each definition's rank and uses its normal Zone selector.

Both modes record 3 roster transitions, 4 recoveries started/completed, 1 successful capture, 1 appraisal, 1 upgrade attempt, 1 evolution attempt and 1 dismantle. Monster makes 2 capture attempts; expedition makes 3. Two initial capture admissions are imported fixtures with stable IDs (a level-40 reserve and a salvage candidate); the separately weakened wild target is an explicit scenario input whose probability roll, item consumption, generated IV/genes and hospital ownership are resolved through HubWorld. Automatic selection of capture targets inside the expedition resolver is not introduced by Task 15.

Monster: 15,380 Trainer XP, final Trainer levels 23/22, 1,410 collected material units, 111 purchased product units, 279 completed jobs. Expedition: 16,180 Trainer XP, final levels 14/13/12/11/11/11, 1,089 collected material units, 148 purchased product units, 322 completed jobs. Both have zero dropped material units and empty backpacks at the measured horizon. Runtime includes configuration, command execution, simulation, invariant checks and formatting; it excludes CLI startup/build. These are measurements on this host, not a general performance guarantee.

## Reconciliation

Both scenarios exit successfully only when every computed difference is zero:

- Trainer Gold: initial wallets + explicit expedition Gold + donations + paid wages + material/product net sales − services − appraisal − product purchases, compared with final wallets.
- Treasury: initial treasury + all `TreasuryChanged` deltas, compared with final treasury.
- Supply Gold: independently derive treasury movement from double-entry supply transactions and compare with the supply-related treasury event reasons.
- Ownership: replay recovery, bank-transfer and dismantle events into an ID/depositor/custody map, then compare against final `MonstersForTrainer` snapshots. Final owned counts are 4/8, each including one bank deposit, with no pending recoveries.
- Trainer products: sum every product delta per Trainer/product, check each event's resulting count, and compare every final inventory stack. Material collection/drop/production/purchase counts are printed separately; the item reconciliation line specifically audits Trainer products rather than claiming a full per-material production conservation audit.

Monster Payday pays 6,670 Gold; expedition pays 20,010. Each assesses 600 Gold for one banked Monster, collects no fee automatically and records zero confiscations.

## Decisions and concerns

- Local storage's numeric maximum is unspecified in the task brief. Chose a configurable, source-tagged Prototype safety limit of 500; changing the balance decision requires configuration only. Capture reservations avoid loss during asynchronous recovery when capacity fills.
- Scenario fixtures intentionally import a level-40 reserve and a low-IV salvage candidate, guarantee upgrade/evolution, and use survey rank eligibility for expedition. Their printed Prototype metadata identifies their demonstration purpose; results do not establish campaign balance.
- Earlier compilation exposed six pre-existing CS0618 warnings in `RebellionAndFinanceTests.cs` call an obsolete rank-unaware overload. No failing tests remain and the newly introduced xUnit warning was removed.
- Final review was performed by the author under the explicit no-subagents instruction. It checked task coverage, immutable boundaries, seeded command rejection, capacity reservation, recovery event pairing and supply settlement; it does not substitute for an independent reviewer.

## Evidence files

Local task artifacts (not prior ledger history): `task-15-focused-tests.txt`, `task-15-full-tests.txt`, `task-15-monster-output.txt`, `task-15-expedition-output.txt`, plus the scenario red-run logs. The source and this report are committed; verification logs remain local task artifacts.
