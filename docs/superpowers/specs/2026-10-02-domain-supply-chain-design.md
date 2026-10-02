# Domain sub-project 2: Supply chain, station market, merchants, production

## Intent and constraints

The user asked to complete the remaining Domain sub-projects autonomously, capture numeric parameters in a workbook for later balancing, and simulate the actors and behaviors already described in the GDD before balancing. Therefore this sub-project replaces the temporary `FixedPriceMarket`; it must make each Trainer sale, Director buy order, Merchant trip, HUB stock change, and production job observable and deterministic. Numbers added here are prototype defaults and belong in `docs/balance/MonsterHUB_Balance.xlsx` with status and source.

## Alternatives considered

1. **Direct station-only trade.** Trainer sells only into Director buy orders. This is easy but contradicts the mobile Merchant and fails the GDD promise that Trainers can always liquidate gathered materials.
2. **Infinite Merchant as a price function.** Trainer always sells and HUB automatically buys missing goods at a markup, but the Merchant has no inventory, capital, travel, or bankruptcy behavior. It would retain the temporary model's core abstraction.
3. **Finite Merchant with inventory and credit (chosen).** Trainer can fill active Station buy orders, otherwise sell to a traveling Merchant. The Merchant accumulates goods and sells them to HUB when recipes/order deficits create demand. Finite liquid capital and carrying capacity make insolvency and replacement meaningful. Every trade has explicit payer, receiver, inventory movement, tax, and event.

The chosen model is event-driven and uses the existing `EventQueue`, `SimRandom`, `HubWorld.RunFor`, and Domain event channel. Unity only presents locations/animations. Offline and `Game.Sim` use the same market and production rules.

## Domain actors and rules

### Raw material catalog

There are six material families, each with five Zone tiers: ore/mineral, wood, cloth/leather, gems, herbs, and food (30 stable IDs). Zone N yields mostly tier N with occasional lower-tier goods. Each material definition carries family, tier, reference price, base weight, and primary demand recipes. Names/prices/yields are data, not switch statements. Keep the existing item catalog names; missing per-tier names use stable IDs until art naming is finalized.

### HUB station and Director buy requests

For each raw material, the Director sets an active request: target stock, bid price per unit, and enabled flag. Effective requested quantity is `max(0, TargetStock - StationStock - ReservedForProduction - IncomingMerchantUnits)`. The station buys direct from returning Trainers up to this deficit. The transaction debits HUB treasury by gross bid, credits seller by gross less transaction tax, and credits tax to treasury (purchase outflow must be visible separately from tax inflow). Unaccepted units are sold to the Merchant. No negative balances or hidden creation of materials.

### Traveling Merchant

The Merchant travels on a scheduled route between Zones and the Station. A meeting permits purchase from a Trainer at a discount below the configured Station reference price; the discount varies monotonically from 5% to 10% with lot size. Merchant funds and capacity limit the purchase. Any unbought remainder can be sold to another Merchant visit; the actor never deletes goods silently. At the Station, Merchant inventory can fill active stock deficits, at a markup of 5%-10%, paid by HUB treasury. Both buys and sales debit/credit the Merchant ledger. If operating cash stays below zero/credit limit after settlement, Merchant becomes bankrupt; a replacement arrives after a configurable delay with fresh starting capital. A market fallback path always exists so Trainer stock does not get stuck forever: direct Station demand, current Merchant, then wait for the next replacement visit. The Merchant's limited capital can cause a wait; it is observable in Trainer state and has a bounded guarantee.

### Tax and money flow

The GDD says Trainer bears trade tax. Tax is calculated on gross seller proceeds and withheld; tax enters HUB treasury. For direct Station buys, the treasury pays the gross order value then receives the withheld tax, so net cash outflow is explicit. For Merchant buys, the Merchant pays the gross and HUB receives the withheld tax. Inventory and money ledgers reconcile at each transaction. The rules do not create Gold from HUB exports: HUB never sells outside the economy.

### HUB inventory and production

HUB owns a typed inventory by material/product ID with available, reserved, and in-production quantities. Director sets target stock for each product. Producers enqueue jobs while projected stock is below target and all input materials exist. Each job reserves inputs when started, consumes them on completion, and adds outputs; cancel/failure releases or consumes according to a specified result. A job has recipe, output quantity, duration modified by producer level, and per-job operating cost. Production is event-driven, not polled every tick. Missing materials generate a station buy need using the upstream demand graph.

Sub-project 2 models the supply-chain buildings and job lifecycle needed to create/sell stock: Refinery (raw -> intermediate), three equipment workshops by slot group (Monster forge, Trainer utility textile shop, aura jeweler), and Reactor (intermediate -> enhancement materials). Consumable stalls use the same production abstraction. Each stall owns product recipes: Hospital medicines/vaccines/anxiolytics; Restaurant food/water/reward cakes; Bar liquor; Tool shop weather gear/traps/balls/books; Soda factory temporary Monster stat bottles sold by General Store. Rest/medical service recovery itself is not produced as inventory.

### Trainer behavior integration

A Trainer returns with a material stack, attempts to sell every unit, and receives a deterministic breakdown in event payloads: accepted by Station, sold to Merchant, remaining due to capacity/cash. Do not change the existing needs/FSM decisions in this sub-project except to represent `WaitingForMarket` and retry on a Merchant-arrival or replacement event. The Trainer's choice of Zone and material pick rate remains farm-resolver responsibility until sub-project 3; the world/zone material catalog is introduced now.

### Persistence, determinism, and observability

All random variation (merchant price spread, route/encounter time, bankruptcy/replacement) comes from `SimRandom`. Tie-breaking uses stable material and trainer IDs. Market and production emit C# domain events with timestamps and quantities. Public view models expose stock, active requests, Merchant cash/inventory/state, producer queues/utilization, and transaction aggregates. `HubWorld` remains the external entry point.

## Workbook contract

The workbook `docs/balance/MonsterHUB_Balance.xlsx` is the editable balance source for parameter values. Add/update sheets or ranges for Material Catalog, Buy Requests, Merchant, Recipes, Production, and Economy Flow. Each driver has a stable ID, value, unit, status (`Locked`, `Prototype`, `TBD`), and GDD/code source. C# defaults remain usable in tests, but every tunable value must map to a workbook ID and a corresponding `SimConfig`/definition field. Never describe placeholder values as balanced.

## Acceptance criteria

- Station direct purchase and Merchant fallback follow requests, targets, tax and prices exactly; all balance-sheet ledgers reconcile.
- Merchant buys below Station reference price and sells above it, with quantity-dependent spread bounded by 5%-10%; finite capital/capacity and bankruptcy/replacement affect availability.
- Producer jobs reserve inputs, respect targets, duration, capacity and level, and trigger upstream restock demand; no product appears without a recipe/input.
- Material families/tiers are data-driven and Zone N loot is mostly tier N with lower-tier spillover.
- The existing Trainer FSM can wait for unavailable market service and wakes on a relevant market event without duplicate pending events.
- Same seed and commands produce identical market/production state and event stream.
- Game.Sim reports trade, stock, production, Merchant insolvency/replacement and HUB cash flows; unit tests assert conservation and state transitions.
- Every exposed balance constant is present in the workbook with source/status; README links to it.

## Explicit deferred work

Combat-derived yields/HP, capture and Monster recovery are sub-project 3. Item stats/durability and gear score are sub-project 4. Loans/stock integration is sub-project 5. Town Hall/Zone gate and event schedules are sub-project 6. Sub-project 2 may define data contracts and recipe IDs those projects will consume, but must not replace their behavior with stubs beyond the existing farm interface.
