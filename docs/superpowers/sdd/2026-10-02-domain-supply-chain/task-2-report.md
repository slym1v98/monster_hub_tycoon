# Task 2 report: inventory and money ledgers

## Delivered

- Added typed inventory keys for raw materials and products, with available, reserved, and in-production quantities.
- Added checked add/remove/reserve/release, production start/completion, and cancellation transitions. Invalid or insufficient operations leave quantities unchanged.
- Added a money ledger that records gross payer outflow, seller net proceeds, and tax as separate balanced postings; it validates tax bounds and account identities and checks overflow before applying postings.

## Verification

- RED: `SupplyLedgerTests` initially failed to compile because the supply ledger types did not exist.
- GREEN: `dotnet test tests/Game.Domain.Tests` passed 124/124.

## Limits

The ledger records cash movement and does not impose account solvency; Station/Merchant callers enforce affordability when those actors are implemented. Production transition operations are item-level primitives; multi-input recipe transactions are added with the production controller.
