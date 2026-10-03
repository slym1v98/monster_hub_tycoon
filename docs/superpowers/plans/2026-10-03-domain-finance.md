# Finance sub-project plan

> Follow RED → GREEN → REFACTOR for each behavior. Keep finance behavior deterministic under seeded simulation. Add every runtime balance to `docs/balance/MonsterHUB_Balance.xlsx` with stable ID, status, unit, source.

## Task 1: Parameter inventory and SDD artifacts
- Inventory finance, loan, stock and Gene Bank values against GDD 02/13.
- Add spec and this plan; add baseline tests exposing missing integrations.

## Task 2: Trainer loan ledger
- Implement loan balance, rate/limit configuration, explicit borrow/repay events, income-offset ordering, Payday interest and overdue strike transition.
- Test debt never changes except through recorded flows and no implicit Gold creation.

## Task 3: Reverse loan contracts
- Add eligible Rank V lender offers, per-lender available limit and maturity, interest, repayments, overdue service credit.
- Test lender isolation, limits, term, custody of all Gold flows, deterministic settlement.

## Task 4: Stock exchange
- Add company share and ownership records, IPO, director and Trainer trade commands, market hours/fees, daily price update, 15-day dividend and realized-profit tax.
- Integrate seeded AI stock behavior only where GDD defines an action; expose unspecified heuristics as Prototype/TBD.
- Test seeded paths, ownership invariants, no sale of locked 51%, and transaction conservation.

## Task 5: Gene Bank payday settlement
- Debit assessed fee from Trainer, credit Treasury, seize one weakest stored Monster on shortfall, and emit ordered events after wages.
- Test exactly-once collection and stable confiscation tie-break.

## Task 6: HubWorld integration and scenario
- Add command/view/event APIs and a deterministic 360-day `finance` Game.Sim mode.
- Report ledgers and runtime parameters.

## Task 7: Workbook and GDD/docs
- Add finance parameters to the workbook and reconcile exports.
- Update design status/open issues, document unresolved values, and update scenario README.

## Task 8: Review, verification, closeout, commit
- Run focused and full Domain tests, build Game.Sim, run finance scenario, verify workbook rows and `git diff --check`.
- Record review and evidence in `docs/superpowers/sdd/2026-10-03-domain-finance/closeout.md` and commit locally. Do not merge or push.
