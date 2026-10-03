# Domain sub-project: Campaign quests and Daily/Weekly KPIs

## Intent and scope

Implement the Early Access Quest/KPI loop in the deterministic Domain simulation, using the approved direction: a quest tracker owned by `HubWorld` observes completed domain events. It covers the Main Quest campaign and Daily/Weekly KPIs in `docs/designs/08_Quests_Achievements_Collections.md`. Achievements, titles, Monster Index, and Director's Vault remain post-Early-Access and are explicitly out of scope.

The objective thresholds in GDD 08 and balance document §7 are placeholders. Keep them configurable and record every runtime threshold, reward, and period rule in `docs/balance/MonsterHUB_Balance.xlsx`, with status and a GDD/workbook source. Do not present prototype values as final balance.

## GDD requirements

- Main Quests are a campaign/tutorial path through Town Hall/building, Zone, and population progression. The named example “Nền Móng Bóc Lột” completes when Zone 2 is unlocked and the Veterinary Hospital reaches level 2. “Giai Cấp Mới” is an example requiring recruitment of an Epic Trainer; the current Domain has no recruitment operation, so this objective is represented as a declared, not-yet-activatable content dependency until a recruitment API/event exists.
- Daily KPIs: medicine sales totaling the configured Gold target; one completed stock-market manipulation operation; and the configured number of Liquor units sold to Trainers.
- Weekly KPIs: the configured number of Gene Bank monsters confiscated for unpaid fees; and the configured number of Protection Charms sold to Trainers for Gold.
- Progress comes from successful, settled domain facts. Rejected commands, unfilled purchases, failed trades, and attempted but unsettled operations do not count.
- Completing a Quest/KPI grants its configured sponsor reward. Main Quest rewards include building-permit and high-tier-invitation entitlements. KPI rewards may include Gold/Gem funding. Rewards are granted once per campaign objective or KPI period.

## Proposed design

### Ownership and observation

Add an internal `HubQuestTracker` owned by each `HubWorld`. It consumes the `IDomainEvent` stream synchronously at the `HubWorld` event boundary. It maintains immutable read-only views for campaign progress, current Daily KPI progress, current Weekly KPI progress, and unspent Quest rewards. It does not issue normal economy/progression commands on behalf of a KPI and does not infer progress by polling mutable balances.

Events created by a KPI reward must not recursively count as objective progress. Publish the original gameplay event first, evaluate matching facts once, then emit the resulting progress/completion/reward events in deterministic catalog order.

### Periods and reset

Daily periods use the simulation's absolute in-game day boundaries (`SimClock.MinutesPerDay`). Weekly KPIs use consecutive seven-day in-game periods anchored at simulation day zero. Events at an exact boundary belong to the new period. Progress resets at period change; a completed KPI cannot pay twice within one period. The campaign is persistent for the lifetime of the `HubWorld` and each objective can complete once.

### KPI fact mapping

- Medicine revenue sums `ProductPurchased.TotalPaid` for Hospital products (`potion`, `vaccine`, `tranquilizer`). This measures paid sales revenue, not production, stock deposits, or purchases rejected by the Hospital facility gate.
- A stock-market operation counts a successfully settled, director-issued share trade. Add explicit command-origin metadata (or an equivalent director-only fact) because the existing `StockTradeSettled` event is also emitted by Trainer stock AI. IPO purchase and settled share transfer may qualify; AI trades, failed/closed-market operations, and stock-price updates caused by traffic do not. Exact GDD wording is ambiguous; this is the explicit measurable interpretation.
- Liquor sales sum settled `ProductPurchased.Units` for `liquor`.
- Gene Bank confiscation sums `MonsterConfiscated` facts caused by unpaid Gene Bank fees. It does not count voluntary withdrawals, event damage, or generic ownership changes.
- Protection Charm sales sum settled `ProductPurchased.Units` for `protection_charm`.

The current Domain lacks a Director-to-Trainer Protection Charm supply path, although GDD 07 and 12 specify that the Director obtains Charms from a provider/reward and offers them to AI Trainers for Gold. Add a deterministic provisioning boundary for fulfilled Director stock, expose the configured offer price through the existing product-price path, and let Trainer purchase settle through the existing stock, Gold, and `ProductPurchased` event path. The simulator must be able to provision earned/fulfilled stock without modeling a real-money provider. Trainer demand follows GDD 07: when considering a gear enhancement with break risk, an AI buys the Charm if its configured price is below the expected avoided loss; otherwise it may enhance without a Charm. Add the smallest deterministic enhancement-decision hook needed to exercise that rule. Expected value, fulfillment amount/source, and any stock constraints remain configurable and explicitly Prototype/TBD in the workbook. Facility access, pricing limits, treasury settlement, and affordability must use the existing Domain rules.

### Campaign objective dependency

The first campaign milestone uses existing `ZoneUnlocked` and Hospital facility-level events. The Epic-recruitment example is present in the Quest catalog schema but inactive until the Domain has a recruitment operation and a successful-recruitment event. Do not fake it using starting roster rarity, and do not add the Gacha/recruitment feature to this sub-project.

### Rewards and observability

Grant rewards automatically on completion, in deterministic order, with a `QuestProgressChanged`, `QuestCompleted`, and `QuestRewardGranted` event. Gold rewards credit the HUB Treasury through its existing checked ledger/event path; Gem, Building Permit, and Invitation rewards accumulate in a Quest reward wallet exposed to the read-only view because no player Gem/permit/invitation wallet exists in Domain yet. This does not grant construction access or invoke Gacha implicitly. Reward types/amounts and the prototype use of the Quest reward wallet must be configurable and documented in the balance workbook.

Use stable quest/KPI IDs and stable rejection/availability codes. Views must expose target, current progress, period/index, completion, claimed/granted state, and reward balances so a UI can render the system without owning game rules.

## Balance and data requirements

Add a workbook sheet for Quest/KPI parameters. At minimum record daily medicine-Gold target, stock-operation count, Liquor-unit target; weekly confiscation count and Protection-Charm-unit target; daily/weekly period lengths; prototype reward amounts by type; and Protection Charm price/stock fulfillment controls. Every row must have a stable ID, value, unit, status (`Locked`, `Prototype`, or `TBD`), and source. Placeholder GDD example targets are `Prototype`, not `Locked`; rules fixed by the GDD are `Locked`; unspecified reward/economy choices are `TBD` until an explicit runnable prototype value is selected.

## Validation requirements

- A fact advances only its matching objective, and failed/unsettled actions never advance progress.
- Period rollover handles midnight, seven-day boundaries, exact-boundary events, skipped simulation intervals, and one-time reward settlement without duplicate payouts.
- Identical commands/events/configuration produce identical quest state and rewards.
- Each campaign predicate is conjunctive where GDD specifies multiple requirements; the first campaign milestone cannot complete after only Zone 2 or only Hospital level 2.
- KPI Gold, Gem, permit, and invitation balances and Treasury deltas reconcile with completion events; checked arithmetic rejects overflow without partial state changes.
- Protection Charm provisioning, offer pricing, exact-quantity purchase, stock conservation, and reward counting use the same Domain purchase path as other products.
- Workbook values and statuses match every numeric Quest/KPI runtime setting.

## Source documents

- `docs/designs/08_Quests_Achievements_Collections.md` — Early Access scope and example Campaign/KPI objectives.
- `docs/designs/13_Balance_Parameters.md` §7 — objective values are placeholders.
- `docs/designs/07_Monetization_Model.md` — Director-provided Protection Charms sold to AI Trainers for Gold and Quest/free reward sources.
- `docs/designs/12_Item_Catalog.md` — Protection Charm source/use.
- `docs/designs/02_HUB_Economy_Infrastructure.md` §1.2 — Gene Bank unpaid-fee confiscation rule.
