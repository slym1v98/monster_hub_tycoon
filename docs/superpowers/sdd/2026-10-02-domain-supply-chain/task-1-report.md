# Task 1 report: catalog and balance workbook

## Delivered

- Added stable IDs and definitions for 6 material families × 5 tiers, 25 GDD products, recipes, and producers.
- Added `MaterialCatalog` validation for duplicate/empty IDs, invalid family/tier pairs, missing producer references, empty recipe sides, invalid quantities, and unknown item references.
- Expanded `docs/balance/MonsterHUB_Balance.xlsx` to 11 sheets, including material catalog, recipe inputs/outputs, Merchant, and production controls. Unknown numeric values remain marked TBD with units/status/source.
- Added a GDD behavior coverage map to track which sub-project simulates each described behavior and what evidence is required.

## Verification

- RED: at baseline commit `8c92cd8`, copied only `MaterialCatalogTests.cs` into a detached worktree and ran `dotnet test`; compilation failed because `Game.Domain.Materials` and `Game.Domain.Production` were not implemented (`/tmp/monterhub-task1-red.log`). Removed the temporary worktree afterward.
- GREEN: `dotnet build src/Game.Domain` succeeded with 0 warnings/errors.
- GREEN: `dotnet build tools/Game.Sim` succeeded with 0 warnings/errors.
- GREEN: `dotnet test tests/Game.Domain.Tests` passed 120/120.
- Workbook round-trip inspection showed 11 sheets; catalog rows include 30 materials and recipes/merchant/production controls expose units, status, and source columns.

## Scope note

This task creates the data model and catalog only. It does not claim that the supply chain behavior is simulated; inventory, transactions, buying, finite Merchant behavior, jobs, and HubWorld integration remain in Tasks 2–7. No balance targets were selected.
