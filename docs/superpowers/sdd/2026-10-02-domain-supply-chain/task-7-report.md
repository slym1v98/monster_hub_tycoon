# Task 7 report: HubWorld supply-chain integration

## Delivered

- Composed the shared ledger, Station, Merchant fleet, and production controller in an internal `SupplyChain`; `Station` in HubWorld shares the same treasury as services, payroll, and production costs.
- The default HubWorld path now uses Station requests and a finite traveling Merchant. The old injected `IMaterialMarket` remains as an explicit compatibility path for existing callers.
- Trainer backpacks retain material identity. New farm resolvers can provide a catalog ID; legacy resolvers without one are kept as `legacy_untyped` rather than silently mapped to a real material.
- Merchant departures/arrivals, production completion, and bounded market retries use the existing deterministic event queue. Unsold lots stay in the Trainer backpack; timeout resumes the Trainer with those goods retained.
- Added validated commands for buy requests, production targets, reference price, and tax rate; added flat stock/request/Merchant/job views and stock, trade, restock-demand, Merchant, and job events.
- Production reconciles when new stock arrives, accounts for available + reserved + in-production units against buy targets, debits an affordable job's operating cost when it starts, and completes jobs at the scheduled minute. An unaffordable job leaves its inputs available.
- Producer level scales recipe/default job duration with a prototype curve of 1.0/0.9/0.8/0.7/0.6 for levels 1–5; the resulting minutes round up, with a one-minute minimum. The curve is editable in the balance workbook.
- A due Merchant replacement wakes waiting Trainers immediately, including a same-minute retry/replacement tie. The 210-minute maximum wait is a prototype safety cap; replacement arrival normally wakes a seller sooner.
- Retry scheduling honors short and long wait caps from the first retry; unrepresentable Merchant quotes remain unsold, and Trainer tax is limited by actual Treasury headroom before settlement.

## Verification

- Domain and Game.Sim builds passed with 0 warnings/errors.
- `/Users/nofine/.dotnet/dotnet test tests/Game.Domain.Tests --no-restore` passed 165/165, including shared production treasury affordability, all five Producer duration levels and curve validation, strict market-wait deadlines, Merchant replacement at the wait deadline, retry retention when replacement funds are insufficient, overflow-safe quotes/tax headroom, checked Treasury addition, Station settlement/tax, retained backpack, production wake-up/completion, deterministic events/invariants, and invalid command checks.
- A compatibility regression found during verification—standalone Station no longer reflecting Merchant tax from the shared ledger—was fixed by retaining ledger-backed accounting for standalone Stations and using the shared wallet only in HubWorld.
- The updated `core` simulation completed in 247 ms for three months with prototype parameters; it reports `WaitingForMarket` as 10.8% of Trainer time and a 210-minute maximum observed market wait. Supply-ledger/treasury reconciliation is 0.

## Limits

The fallback farm resolver currently emits ore tier 1 for the prototype. Older custom resolvers that return only a unit count use the untyped compatibility key. Actual per-Zone material stacks and yields remain part of the farm/Monster sub-project. Per the supply-chain spec, Stress from tax above 30% and from market waiting remains deferred to the Trainer AI/Stress work. Default request, price, Merchant finance/capacity, route timing, and production settings—including the prototype duration curve—remain balance inputs.
