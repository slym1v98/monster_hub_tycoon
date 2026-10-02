# Task 5 report: recipe jobs, Refinery, and Reactor

## Delivered

- Added 15 tier-specific Refinery recipes (ore, cloth/leather, and gem → matching blank) and 45 Reactor recipes (each blank → enhancement stone, distilled water, or evolution stone).
- Replaced the generic `blank` item with stable tier/family-specific blank IDs.
- Added multi-item atomic inventory reservation/start/completion/cancellation so a multi-input recipe cannot partially reserve or consume ingredients.
- Added deterministic target-stock scheduling, producer level/capacity controls, simulated-minute completion, cancellation pause, itemized cost postings, and recursive missing-input demand down to raw materials.
- Refinery efficiency applies per producer level and carries fractional yields across jobs, so unit rounding does not destroy the stated long-run ratio.
- Workbook now lists 39 products, 60 recipe input rows, 60 output rows, default job duration/cost/capacity, and a five-level efficiency curve. The 1:1 recipe basis, 60-minute job, one concurrent job, and interpolated five-level efficiencies are marked Prototype because exact recipes/timing are not specified; unknown prices remain TBD.

## Verification

- RED: at base `5568d7a`, adding only `ProductionControllerTests.cs` failed compilation because `ProductionController` did not exist.
- GREEN: `dotnet build src/Game.Domain` passed with 0 warnings/errors; `dotnet test tests/Game.Domain.Tests` passed 142/142.
- Workbook was exported then re-imported; all 11 tabs remained. Recipe input/output ranges contain 60 rows each, product catalog contains 39 products, and producer capacity data is present.

## Limits

Consumable recipes and stall mappings are added in Task 6. EventQueue integration and Station restock command wiring remain in Task 7. Prototype values are exposed for later balancing, not treated as approved balance targets.
