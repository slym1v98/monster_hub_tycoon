# Domain sub-project 6: Town Hall, population, Zones, reputation, buildings, and events

## Intent and boundaries

Implement the GDD progression loop in the deterministic Domain simulation. The player-facing layer may issue commands and render events, but eligibility, costs, timers, population capacity, reputation, and event effects belong in `Game.Domain` and must behave identically in `Game.Sim`.

This sub-project follows Monster/combat/loot, gear, and finance. It consumes their existing domain APIs for trainer ranks, materials, gear, market, stock, veterinary care, gene storage, and treasury. It does not reimplement those systems. Quest/KPI is the next separate sub-project and may subscribe to the progression/event domain event stream; this project should expose stable events and cumulative facts needed by it.

## GDD rules treated as requirements

- Town Hall has 25 levels split into five 5-level tiers. Building levels cannot exceed Town Hall level. Tier 2 requires Zone 2; higher tiers follow the same rule. Five-level facilities of tier N require Town Hall level `5N-4`.
- GDD unlocks buildings at Town Hall milestones: Lv1 Dormitory, Trading Station, Veterinary Hospital; Lv2 Inn and Restaurant; Lv3 Refinery and Tool Workshop; Lv4 Bar and Forge; Lv5 Textile Workshop and Bounty Board; tier 2 adds Reactor, Warp Gate, Soda Factory, General Store, and Gene Bank; tier 3 adds Jeweler, Evolution Lab, Academy, and Stock Exchange. The existing project may already model some facilities; integrate through a catalog without breaking their APIs.
- Construction and upgrades consume Gold and Wood/Stone/Iron ingots and take in-game time. A building remains usable at its previous level during an upgrade. Upkeep is charged daily, does not accrue as debt, and a deficit halves efficiency and capacity and reduces quality. A powered-off building has no service or upkeep and affects its company stock price as specified by finance.
- Zone progression gates, in order, are: 5 Rank I Trainers to start Zone 1; then 7/10, 12/15, 17/20, and 22/25 Trainers at Rank II/III/IV/V respectively. The denominator is the current population cap before unlocking the next Zone; after unlock, capacity rises to 15/20/25/30. Dormitory must reach level 1/2/3/4/5 for Zones 1/2/3/4/5. A Trainer may enter a Zone only if Rank >= Zone number.
- **Approved progression exception:** a Dormitory level needed for the next Zone may be completed before that Zone is unlocked. This breaks the GDD prerequisite cycle where Zone unlocks the Town Hall tier needed for the Dormitory upgrade. Other building and Town Hall tier gates remain as written in the GDD.
- Reputation responds to service quality, prices against fair price, average Trainer stress, and bankruptcies. It affects applicant rarity and traffic; low reputation also increases inspection pressure. GDD supplies relationships but no exact function or coefficients.
- Events: Black Friday is the last three days of each in-game month before payday and makes Trainers impulsive buyers of low-priority goods; Breeding Season increases rare genes in Zone 1 and demand for capture balls/traps; Monster Flu reduces Monster HP by 50% and causes ongoing HP loss; Labor Inspection is triggered by transaction tax >30% or red stress (GDD threshold 80) and assesses penalties; Monster Siege calls Trainers back to defend, with victory rewards and loss damage/repair and stock consequences. World Boss Raid is Early Access and may damage buildings on a loss. PvP is out of scope.
- Early Access Quest/KPI is not implemented here. Hidden achievements, Monster Index, relics/Museum, and PvP remain outside scope.

## Design decisions

1. **Progression authority and compatibility.** Add a progression aggregate owned by `HubWorld` (or a cohesive set of internal controllers) as the sole authority for Town Hall level, building catalog/state, population capacity, unlocked Zones, and reputation. Keep existing public `HubWorld` entry points source-compatible where practical; legacy `UnlockZone` becomes a request checked against progression requirements rather than an unconditional mutation.
2. **Dormitory exception.** The next Dormitory level is eligible when all its ordinary cost/material requirements are met and it is within the currently accessible Town Hall tier, even if its corresponding Zone is not yet unlocked. No other building bypasses its Town Hall or Zone requirement.
3. **Capacity and residents.** Population capacity comes from the highest unlocked Zone, bounded by Dormitory level. Existing Trainers above a newly reduced capacity remain present; the system blocks new admission until occupancy is below capacity. This avoids deleting agents and supports deterministic future recruitment. The recruitment/applicant economy itself is not introduced here.
4. **Building lifecycle.** Model locked, available, constructing/upgrading, operational, powered-off, maintenance-deficit, and damaged states as domain data. Only the four GDD defaults (Town Hall, Dormitory, Trading Station, Veterinary Hospital) start rebuilt; other facilities start unbuilt and cannot produce or serve until unlocked and built. Timed jobs complete from simulation events, not frame polling. Upgrade preserves the old active level and operating condition until completion. Power-off pauses producer jobs and resumes their remaining duration when powered on; deficit/damage applies the GDD half-efficiency rule. Cost/time/upkeep/repair values absent from GDD are explicit workbook Prototype/TBD rows with source and units.
5. **Reputation.** Use a bounded deterministic score and an explicit transparent formula from the four GDD inputs. The mapping coefficients are Prototype pending tuning; missing input signals remain neutral and are reported, never silently invented as Locked values. Expose score and contribution breakdown as a view.
6. **Events.** Use the existing event queue and seeded random source for event starts, durations, targeting, and outcomes. Fixed GDD rules (e.g. Black Friday timing and 50% HP reduction) are Locked; unspecified triggers, frequency, rewards, penalties, damage, and durations are workbook Prototype or TBD. Player-triggered World Boss uses cooldown and Gold cost; PvP remains absent. Event effects must be reversible/contained and emit start, phase, resolution, and economic consequence events.
7. **No implicit content.** Do not make up final costs, durations, coefficients, event cadence, or loot rewards. Defaults needed to execute a prototype must be named and traceable in the balance workbook, and marked Prototype/TBD.

## Workbook contract

Update `docs/balance/MonsterHUB_Balance.xlsx`. Every runtime parameter introduced or selected here must have a stable ID, value, unit, status (`Locked`, `Prototype`, or `TBD`), and source. Record GDD-fixed gates/timing/ratios as Locked with exact section citations. Record assumptions as Prototype; use TBD where the GDD does not support a useful provisional default. Include Town Hall/building progression, population/Zone gates, construction/upkeep/repair, reputation formula, and event triggers/effects/rewards. Do not silently promote an existing legacy value to a GDD rule.

## Observable behavior and interfaces

- Commands for Town Hall upgrade, facility construction/upgrade, power toggles, maintenance payment, repair, Zone unlock, and World Boss activation return accepted/rejected results with stable reason codes and quoted costs/times.
- Read models expose Town Hall level/tier, each facility's unlock/level/progress/operating state, population/capacity, Zone gates, reputation score and inputs, active event state, and event history/aggregate consequences.
- Domain events include progression changes, construction/upgrade completion, Zone unlock, maintenance deficit/recovery, power changes, reputation updates, event lifecycle, affected Trainers/Monsters/buildings, and explicit money/material deltas.
- Preserve deterministic replay for same seed, commands, and configuration. Do not use wall clock, Unity callbacks, or unordered dictionary iteration for decisions.

## Acceptance criteria

- Zone and Town Hall gates match all five GDD milestones, including rank counts, Dormitory levels, capacity and rank-based access; the approved Dormitory exception is covered by tests.
- Invalid/unaffordable commands leave state, Gold, and materials unchanged and return stable reason codes. Successful costs are conserved and reported.
- Timed construction/upgrades finish at the configured simulation time while the previous level remains available; daily upkeep, deficit, power-off, repair, and damage follow their specified state transitions without negative treasury/material balances or hidden upkeep debt.
- Reputation is deterministic, bounded, explainable by its inputs, and changes traffic/rarity/inspection pressure monotonically in the GDD direction.
- Scheduled, triggered, and random event lifecycle and effects are deterministic and observable; fixed GDD rules are exact and all unprovided tuning values are workbook-tracked as Prototype/TBD.
- Every runtime parameter has a workbook row with status and source; every workbook value used by code is read through configuration/data and can be traced to a stable ID.
- `Game.Sim` can run reproducible progression and event scenarios and summarize gate status, building operations, reputation, event outcomes, and ledger changes.
- Unit and integration tests cover gates, costs, timers, building states, reputation, each in-scope event, determinism, and event/economy reconciliation.
- Add a sub-project plan, complete a review, document test evidence, and make one local commit. Do not merge or push.

## Open points for plan/implementation review

- Zone unlock uses the exact GDD thresholds: 7 Rank II Trainers while current capacity is 10, 12 Rank III while capacity is 15, 17 Rank IV while capacity is 20, and 22 Rank V while capacity is 25. The denominator records the pre-unlock capacity and is not a fraction or a second gate.
- Applicant “traffic” and “rarity” are progression outputs only until the future recruitment sub-project exists. If no applicant model exists after inspecting the current branch, expose deterministic reputation modifiers rather than creating applicants.
- GDD does not specify exact random-event cadence, inspection fine, repair bill, Siege enemy scaling, World Boss cooldown/activation price, or Breeding Season duration. Keep these as tunable Prototype/TBD values and avoid claiming balance.
- Event scope targets GDD Early Access scheduled/crisis events, Monster Siege, and World Boss Raid. Any event that depends on a missing connected system should still emit its trigger and represent effects through the owning domain service; do not implement duplicate stock, combat, or healing rules.
