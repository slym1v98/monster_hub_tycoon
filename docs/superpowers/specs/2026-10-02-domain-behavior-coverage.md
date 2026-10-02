# Domain GDD behavior coverage map

Purpose: completion audit for the user goal “simulate the GDD-described actors before balancing”. This map separates already simulated behavior from standalone formulas and assigns every remaining GDD behavior to one owning sub-project. A class existing by itself is not counted as behavior coverage.

| GDD behavior | Owning sub-project | Required simulation evidence | Current state |
|---|---|---|---|
| Trainer needs, FSM, queues, payday, strike, service price/stress | 1 Core time/Trainer | `HubWorld` events and tests | Implemented; some defaults are intentionally unbalanced |
| Station bids, Merchant routes/capital/inventory/bankruptcy, materials, stock targets, recipes/jobs | 2 Supply chain | reconciled transactions, stockout/replacement scenarios | In progress |
| Zone encounter tables, species/stats/IV/rarity, skills/cooldowns/targeting, elements, class/gear/synergy, swap, auto-battle replay | 3 Monster/combat | combat outcomes produce HP/durability/loot/EXP/capture and repeat by seed | Not implemented; `SimpleFarmResolver` remains temporary |
| Catching and choice, Hospital recovery beds, active/reserve swap, storage fees/seizure/resale/dismantle, IV appraisal, gene fragments, stat-rarity improvement, evolution branches (0–2) and rebellion | 3 Monster/combat | Monster lifecycle through battle → capture → recovery → roster/storage/use/sale | Standalone `RebellionModel` only; no integrated Monster entities or lifecycle |
| 30 equipment slots, item stats by tier, gear score, price response and Director offer, durability/repair, set bonuses, enhancement/star/refine/protection | 4 Itemization | equipment purchase offer changes combat/needs; decay causes repair purchases; upgrade consumes actual stock/resources | Enhancement/UpgradeLadder standalone; no item ownership/gear simulation |
| Trainer inventory/debt and Director loan terms, interest, repayment priority, default and free-service offset | 5 Finance | loan balance and cash flows reconciled on each payday; AI borrow/repay behavior | No integrated Trainer loans |
| HUB stock issuance/IPO, 51% lock, float, shares, market fees, dividends, financial tax, price movement from traffic/order flow, personality trading, panic sell/herding, director pump/short | 5 Finance | holdings/orders/trades/dividends/tax reconcile; same-seed stock history; HubWorld integration | `StockMarket` and `FinanceModel` standalone; StockMarket uses separate `System.Random`; no AI trading/IPO ledger |
| Town Hall levels/build permissions, Dorm population, Zone rank gates, recruitment candidate pool/reputation, contract negotiation/raises/termination/compensation, Rebirth certificate/reset/stats/Rank, trainer class missions/pass/fail/raise | 6 Progression/events | commands change world unlocks, roster, wage contracts and missions; growth gates depend on roster rank not elapsed time | Mostly unimplemented; contract wages are fixed startup defaults |
| Zone access, travel times, day/night, weather/hazards, gates/price choice, Bounty purchase orders and zone choice | 6 Progression/events | AI selects eligible Zone by expected return; travel consumes needs; schedule/weather changes decisions | Zone 1 only, fixed travel; no weather/gates/bounty |
| Black Friday, Breeding Season, Monster Flu, Labor Inspection | 6 Progression/events | scheduled/random state modifiers and explicit outcomes/costs; active Trader/stock/service consequences | Not implemented; Inspector and Flu are post-EA per current roadmap |
| World Boss call/cooldown/cost, participation, drop, damage to Monster/buildings, ad buff hook | 6 Progression/events | event affects combat/service/treasury and returns Boss crystal; call is a Director command | Not implemented |
| Siege and Corporate Ladder (post-EA) | 6 Progression/events or deferred post-EA | only count as covered if enabled for release scope; event state and resource/risk outcomes explicit | Explicitly post-EA in roadmap |
| Quests/KPI/achievements/collections | 6 Progression/events or dedicated content task if needed | objective counters consume Domain events; rewards modify owned values exactly once | Not integrated |
| Offline progress to pending Payday and hourglass stop/continue semantics | 1 Core time/Trainer, re-audit after integrations | every new event system uses same `RunUntilPayday`; excess real-time is held/discarded per current GDD | Core API implemented; must re-verify with new event types |
| PVP arenas, espionage, global tournament | deferred post-EA | separate backend and explicitly out of offline EA scope | Deferred by GDD |

## Cross-system requirements

- Domain simulation, offline progression, `Game.Sim`, and tests share one deterministic event/rule path; no second approximate economy path.
- All Gold/material/item/stock/debt/Monster state changes have an owning ledger or explicit state transition; no hidden faucet/sink.
- AI decisions consume modeled state (needs, prices, money, stock, personality, eligibility) rather than scripted purchases used only to inflate revenue.
- Randomness uses the Domain seeded generator; standalone `System.Random` instances must be integrated/replaced when their behavior becomes live.
- Balance workbook stores each tunable parameter with stable ID, unit, status and GDD/code source. Unknown values remain `TBD`; behavior is still implemented with a clearly labeled prototype value where necessary.
- No global economy tuning until sub-projects 2–6 are implemented and the complete 10/30 Trainer scenarios reconcile and pass behavior tests.
