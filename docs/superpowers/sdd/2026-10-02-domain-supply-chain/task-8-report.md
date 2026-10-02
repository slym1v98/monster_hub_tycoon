# Task 8 report: scenarios and GDD/workbook sync

## Delivered

- `Game.Sim core` retains the 10-Trainer, three-month Payday and time-use report, now including market waits and supply-chain metrics. These measurements inspect the core scenario; they are not balance tuning.
- Added `Game.Sim market` runs for 10/30 Trainers over 30/90 days. It reports estimated external Gold, Station/Merchant Trainer sales, tax, stock, Merchant cash/load/bankruptcies, production input/output, restock shortages, wait/job times, treasury reconciliation, and runtime.
- Updated README, GDD 02/12/13, and Open Issues with implemented prototype scope, known limitations, and the balance workbook reference. Values remain Prototype/TBD; the scenarios do not target profit. Added a configurable five-level producer-duration curve, explicitly marked Prototype for later balance.
- Removed a duplicate `MerchantSpreadLotDirection` row from the workbook. Reopened the exported workbook: 13 sheets remain, including 75 recipe-input rows, 73 recipe-output rows, 13 producer-capacity controls, six stall mappings, and the Merchant parameters.

## Verification

- `/Users/nofine/.dotnet/dotnet build tools/Game.Sim --no-restore` passed with 0 warnings/errors.
- `/Users/nofine/.dotnet/dotnet test tests/Game.Domain.Tests --no-restore` passed 165/165.
- `core` completed in 247 ms. Supply-ledger and treasury-event reconciliation difference was 0.
- `market` completed all cases in 164, 148, 234, and 678 ms; reconciliation difference was 0 for each. All four were below the 1000 ms prototype target.

| Run | External Gold* | Direct Trainer sales (Station gross/net) | Trainer sales (Merchant gross/net) | Merchant bankruptcies | Jobs / inputs / outputs | Material-days below target† | Runtime |
|---|---:|---:|---:|---:|---:|---:|---:|
| 10 Trainers / 30 days | 9,618 | 4,770 / 3,816 | 69,390 / 55,513 | 7 | 315 / 315 / 300 | 14 | 161 ms |
| 10 Trainers / 90 days | 21,427 | 4,770 / 3,816 | 207,018 / 165,617 | 22 | 315 / 315 / 300 | 14 | 118 ms |
| 30 Trainers / 30 days | 173,381 | 13,650 / 10,920 | 74,097 / 59,276 | 7 | 636 / 636 / 606 | 27 | 234 ms |
| 30 Trainers / 90 days | 367,045 | 15,990 / 12,792 | 210,168 / 168,134 | 22 | 944 / 944 / 900 | 39 | 699 ms |

Across these prototype runs, the longest market wait was 210 minutes (the configured cap); mean production job duration was 60 minutes, and observed demand-to-job-start averaged 330 minutes for one input-demand episode. Output is lower than input for Refinery due to the prototype level-yield/remainder model. The 30-Trainer cases also produce substantially more Patron donations, which should be examined when the behavior and loot tables are complete. Supply treasury reconciliation includes production operating costs charged at job start and remains exact in all five scenarios.

\* External Gold is farm Gold inferred by reconciling Trainer balances plus Patron donations; no zone loot table exists yet. The scenarios use ore Tier 1 as a provisional SimpleFarmResolver output and set the buy and production targets from aggregate backpack capacity.

† This is a sampled count of material-days with inventory below the buy target. It does not mean a consumable stall was empty. Trainer purchase behavior at stalls and item-effect application remain unimplemented; unused unsold materials stay in Trainer backpacks. Stress from trade tax above 30% and market waiting is also deferred per the supply-chain spec and is tracked in Open Issues.
