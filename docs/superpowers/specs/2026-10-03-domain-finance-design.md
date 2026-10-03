# Domain sub-project 5: Finance

## Scope and source of truth
Implement the remaining Early Access finance behavior from `docs/designs/02_HUB_Economy_Infrastructure.md` §§2–3 and `docs/designs/13_Balance_Parameters.md` §§3, 12–13: Trainer debt/loans, HUB shares and market, and Gene Bank payday fees/confiscation. Keep debt balances and stock holdings in the simulation domain; every balance change emits an event and every runtime parameter is exported to `docs/balance/MonsterHUB_Balance.xlsx` with status and source.

## Current implementation audit
- Implemented in this sub-project: ordinary Trainer credit, interest, income/wage offsets and overdue strikes; Rank V Trainer reverse loans; actual Gene Bank Payday transfers/confiscation; HubWorld IPO/trades/dividends/tax; and deterministic AI buying/selling behavior by rarity/personality.
- `StockMarket` remains as a legacy standalone price utility. Runtime shares use `StockExchange` with share conservation checked by `HubWorld.ValidateInvariants`.
- Source-defined outcomes are enforced; unspecified transaction fee, IPO eligibility threshold, exchange listing rate, AI purchase fractions and panic threshold remain Prototype values exported to the workbook.
- Future event-driven traffic shocks can feed stock-price changes through the existing traffic formula; event probability/reward generation stays with sub-project 6.

## Required behavior
1. **Trainer borrowing:** HUB lends to a Trainer on demand, up to the configured limit (default 2 monthly wages); principal can drive cash below zero only through an authorized loan. Interest accrues at default 10% per Payday, configurable from 5–40%. Wage and sale income repay debt before becoming spendable; 50% of eligible income is applied to the balance. If debt exceeds the limit for two consecutive Paydays, the Trainer strikes. Balances, income offsets, interest and strike counters are explicit and evented.
2. **Reverse borrowing:** HUB can borrow from Rank V Trainers up to 50% of each lender's available cash, at 5% per Payday, due after 1–2 Paydays. Repayment follows the chosen contract schedule; overdue lender debt is serviced with free HUB services and a corresponding reduction in amount owed. Never mint Gold: each transfer balances a named counterparty and Treasury.
3. **Stock market:** IPO eligible facilities with ownership split (51% locked HUB, 49% offered to Trainers), shares outstanding, seeded daily price updates (±3% noise + 0.5× Traffic change + configured net-order impact), immediate trades during daytime market hours, exchange fees to HUB, dividends every 15 days based on revenue and share fraction, and 30% tax on realized profits only. Support director trades and seeded AI decisions by rarity/personality where defined; leave unspecified thresholds as Prototype/TBD. No synthetic stock issuance or cash creation.
4. **Gene Bank:** after Payday wages, assess 20 Gold/Monster/day over the 30-day cycle. Charge available Trainer Gold and credit Treasury. If the Trainer cannot pay the full assessed fee, confiscate one weakest stored Monster (stable ID tie-break); emit fee and custody events. Ensure a fee cannot be collected twice for one payday.

## Parameter contract
Use stable IDs (`finance.*`, `stock.*`, `loan.*`, `reverse_loan.*`, `gene_bank.*`) with `Locked`, `Prototype`, or `TBD`, units, source path/section, and a short note for unresolved GDD details. Values directly stated by GDD13 retain `Prototype` status as marked there. Existing config exports should be aggregated into Game.Sim and the workbook; do not duplicate same IDs.

## Acceptance checks
- Unit tests cover debt limits, rate bounds, interest, repayments, two-Payday overdue transition, reverse-loan eligibility/cap/term/service offset, stable seeded price path, IPO ownership, trade ledger, day closure, dividend and tax, Gene Bank actual fee transfer/confiscation ordering, and event ordering.
- Integration tests reconcile every Gold movement between Trainer and Treasury/counterparty with no unexplained creation/loss.
- Deterministic finance scenario runs for 360 days and reports debt, stock, dividend/tax, fee and custody ledgers plus all parameters.
- Workbook runtime IDs, values, units, statuses, and sources exactly match the finance scenario export (27 parameters, checked by a script).
