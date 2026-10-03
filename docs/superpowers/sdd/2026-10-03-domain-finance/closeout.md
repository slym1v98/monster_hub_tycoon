# Domain sub-project 5: Finance — closeout

## Delivered

- Added HUB-to-Trainer credit with Payday interest, wage and income repayment, editable policy rate, credit line, and overdue strikes. Added Rank V Trainer-to-HUB reverse loans with per-lender limits, interest, maturity, and free-service offsets after maturity.
- Gene Bank fees now transfer Trainer Gold to Treasury after payroll, settle once per Payday, and confiscate the weakest stored Monster on a shortfall with custody events.
- Added HubWorld stock IPOs with 51% locked HUB ownership, public float, seeded prices, traffic and net-order effects, Trainer and Director trades, fees, realized-profit tax, dividends, and share conservation checks. Common/Ultimate rarity and Capitalist/Timid personality trading tendencies run at dawn.
- Added deterministic 360-day `Game.Sim finance`; it reports debt, fees, confiscation, trades, dividends, Treasury and Trainer balances, and exports all finance runtime parameters.
- Added a `Finance` worksheet with 27 IDs and the same values, units, statuses, and source strings as the scenario export. `node verify-finance.mjs` confirmed exact equality.
- Updated the README, this spec and plan, and O7 in `docs/99_Open_Issues.md`.

## Review notes

- Review caught a reversed sign on market sell flow. Net selling now lowers price; the regression test verifies the price response and share conservation.
- Share conservation is checked by `HubWorld.ValidateInvariants`: issued shares equal locked shares, available float, Director float, and Trainer positions.
- Parameters absent from GDD remain `Prototype`, with rationale and source in the workbook. Traffic events from sub-project 6 will feed the existing stock-price traffic formula.

## Verification

- `/Users/nofine/.dotnet/dotnet test tests/Game.Domain.Tests/Game.Domain.Tests.csproj --no-restore`: 679 passed, 0 failed. Existing obsolete-overload warnings remain in Rebellion tests.
- `/Users/nofine/.dotnet/dotnet build tools/Game.Sim/Game.Sim.csproj --no-restore`: succeeded, 0 warnings, 0 errors.
- `/Users/nofine/.dotnet/dotnet run --project tools/Game.Sim -- finance`: 360-day scenario completed; Treasury balance delta and Treasury event ledger both `-43,766` Gold (difference 0). Reported 7,200 Gold Gene Bank fees, 3 stock trades, 1,254 Gold dividends, and 172 Gold in stock fees/tax.
- Workbook check: 27 runtime parameter rows matched exactly.
- `git diff --check`: clean.

## Integration boundary

Stock-price reactions to event-driven traffic are covered by the same traffic equation. Event generation, Town Hall unlocks, and event rewards are implemented with sub-project 6. This commit is local to the worktree; it is not merged or pushed.
