# Supply Chain and Market Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: use `superpowers:subagent-driven-development` to execute this plan task-by-task.

**Goal:** replace the temporary market with a deterministic Station, Merchant, material-inventory, and production simulation faithful to GDD 02 and 12.

**Architecture:** add stable material/product definitions, transaction ledgers, typed HUB inventory, buy requests, a finite Merchant actor, and recipe jobs. Inject `SupplyChain` into `HubWorld`; preserve the public `HubWorld` entry point. Schedule Merchant visits and job completion through the existing event queue. Combat-derived yield stays with sub-project 3.

**Tech Stack:** C# 9 / netstandard2.1, xUnit, existing deterministic `EventQueue` and `SimRandom`.

**Spec:** `docs/superpowers/specs/2026-10-02-domain-supply-chain-design.md`

## Global Constraints

- Domain stays C# 9 / `netstandard2.1` and has no `UnityEngine` dependency.
- Public simulation entry point stays `HubWorld`; use `EventQueue` and `SimRandom` for scheduled/random behavior.
- Runtime and Game.Sim must use the same event-driven rules.
- All tunable numeric values are in `docs/balance/MonsterHUB_Balance.xlsx` with stable ID, unit, status, and GDD/code source.
- APIs and identifiers are English; XML docs and comments are Vietnamese with diacritics.
- Every task writes behavioral tests first, observes the expected failing test, then implements and commits.

## Review Focus

- Station demand, merchant demand, and production reservations overlap: verify the same unit cannot be promised twice.
- Merchant runs out of cash or capacity: preserve unsold inventory, wake waiting trainers after a deterministic bounded wait, never synthesize a sale.
- Jobs are interrupted by missing inputs, cancellation, or producer shutdown: restore reservations exactly once and never create outputs for incomplete jobs.
- Tax, station purchase outflow, and Merchant trade inflow are separate ledger entries: test conservation and prevent double-counting HUB cash.
- Same-time arrivals/completions and equal-priority materials: use stable tie breaks so same-seed runs produce identical event streams.

## File Structure

| Area | Files | Responsibility |
|---|---|---|
| Catalog | `Materials/MaterialDefinition.cs`, `Materials/MaterialCatalog.cs`, `Production/Recipe.cs` | Stable IDs and validated data definitions |
| Ledgers | `Supply/Inventory.cs`, `Supply/TradeLedger.cs` | Quantities, reservations, cash movement and reconciliation |
| Market | `Supply/BuyRequest.cs`, `Supply/Merchant.cs`, `Supply/SupplyChain.cs` | Station requests, Trainer sales, Merchant route/cash/inventory |
| Production | `Production/Producer.cs`, `Production/ProductionJob.cs`, `Production/ProductionController.cs` | Targets, input reservation, timed jobs and output |
| Integration | `Hub/HubWorld.Supply.cs`, `Hub/HubWorld.cs`, `Hub/HubWorldTypes.cs`, `Events/DomainEvents.cs` | Commands, event dispatch, state views, Trainer wake-up |
| Data/config | `Config/SimConfig.cs`, workbook, GDD 02/12/13 | Prototype values, source/status and catalog/recipe mappings |
| Verification | `tests/Game.Domain.Tests/*Supply*Tests.cs`, `*Production*Tests.cs`; `tools/Game.Sim/Program.cs` | Behavior, conservation, repeatable scenarios |

Files are introduced only in the task that first needs them; adjust names when existing neighboring types make a clearer single-responsibility boundary, then record that mapping in the ledger.

## Task 1: parameter/data model and workbook expansion
- Add strongly typed stable IDs for 6 material families x 5 tiers, products, recipes, producers.
- Extend workbook with Material Catalog, Recipe Inputs/Outputs, Merchant, and production controls. Put each prototype/TBD value next to unit/status/source; add all missing numeric drivers to config mappings.
- Add `MaterialCatalog` and validation; reject duplicate IDs, invalid tier/family, missing recipe inputs/outputs.
- Tests: 30 stable material IDs, exact family/tier uniqueness, catalog validation failures.
- Export/inspect workbook and verify row counts; commit.

## Task 2: inventory and conservation ledgers
- Add `Inventory` with available/reserved/in-production quantities and checked add/remove/reserve/release operations; never permit negative quantities or integer overflow.
- Add `MoneyLedger`/transaction records with payer, receiver, gross, tax and reason; reconcile every transaction.
- Tests: reservations cannot oversell; release restores availability; crafting inputs/output balance; overflow/negative rejection; conservation over generated transaction sequences.
- Commit.

## Task 3: Station buy requests and direct Trainer sales
- Add `BuyRequest` target/bid/enabled, incoming and reserved quantities, deficit calculation; station stock and HUB treasury purchase accounting.
- Replace `FixedPriceMarket` path for station orders, returning structured `SaleBreakdown` (direct station, merchant fallback, unsold) and itemized events.
- Tests: full/partial/no orders, target accounting with incoming/reserved stock, tax withheld, affordability, no negative treasury, exact material and Gold reconciliation.
- Commit.

## Task 4: finite traveling Merchant
- Add Merchant state, route schedule, cash, capacity, inventory, offer pricing, lot-size spread 5%-10%, tax treatment, buy and station-sale settlement.
- Add bankruptcy detection and delayed deterministic replacement; market fallback/wait state and wake event integration in HubWorld without duplicating Trainer events.
- Tests: price bounds/monotonicity, cash/capacity limits, goods preserved, exact ledger, bankruptcy/replacement, Trainer wakes after replacement and no deadlock.
- Commit.

## Task 5: recipes, production queue, Refinery and Reactor
- Add recipes and producer definitions, target-stock controller, input reservation, job time, level production multiplier, completion/cancel semantics and upstream demand generation.
- Implement Refinery raw-to-intermediate and Reactor intermediate-to-upgrade-material recipes as data.
- Tests: target only produces missing quantity; inputs reserved/consumed exactly once; shortage emits restock demand; queue order deterministic; completion at exact simulated minute; no output without inputs.
- Commit.

## Task 6: workshops and consumable stalls
- Model three workshops by slot grouping and consumable producer/shop mappings from docs 02/05/12, including Soda Factory -> General Store.
- Maintain goods inventory and availability; service healing/rest does not consume goods. Stalls sell only produced stock and stop when empty.
- Tests: recipe-to-stall mapping, no stock teleportation, sale decrements stock and credits proper accounts; temporary buff products carry effect data only (effect application deferred to 4/3).
- Commit.

## Task 7: HubWorld integration and views/events
- Inject `SupplyChain` and replace old `IMaterialMarket` behavior. Add commands for setting buy request, production target, quote price, and market configuration with validation.
- Add read-only views and events for stock/request/merchant/job changes. Preserve state-event token invariant and deterministic ordering.
- Tests: Trainer sale integrated through HubWorld; output state/event sequence same seed; no duplicate pending events; serialization-friendly flat view DTOs.
- Commit.

## Task 8: simulation scenarios, workbook/GDD/README sync
- Expand `Game.Sim` core report with gross external Gold, Trainer sales (Station/Merchant), tax, inventory, Merchant cash/bankruptcies, production inputs/outputs, stockouts, job wait times, treasury reconciliation.
- Add dedicated market scenario (10/30 Trainers, 30-day and 90-day); report performance. Preserve `ladders` and `stock`.
- Update GDD 02/12/13, Open Issues and README to reflect what is implemented vs TBD; link workbook. Do not tune for target profit yet.
- Verify: `dotnet build src/Game.Domain`, `dotnet build tools/Game.Sim`, `dotnet test tests/Game.Domain.Tests`, run core/market; grep no FixedPriceMarket production usage. Inspect workbook and git status.
- Commit.
