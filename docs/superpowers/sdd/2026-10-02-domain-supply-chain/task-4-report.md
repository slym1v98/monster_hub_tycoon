# Task 4 report: finite traveling Merchant

## Delivered

- Added a Merchant state machine for trainer-route, travel-to-Station, Station, travel-to-trainers, and bankruptcy.
- Merchant purchases honor remaining carrying capacity and available capital. A monotone lot curve uses 5%–10% spread; the chosen direction is larger lot → larger discount/markup, recorded as a prototype assumption because GDD 02 does not specify direction.
- Merchant resale honors Station demand, HUB treasury, and Merchant cash headroom. Goods not sold remain in Merchant inventory.
- Route costs are paid to an explicit ledger sink; insufficient cash causes bankruptcy without deleting goods.
- Added `MerchantFleet` with delayed deterministic replacement and retained former Merchant inventory for audit.
- Added workbook values and labels for prototype cash, capacity, route cycle, trip cost, replacement delay, maximum wait, and spread lot threshold. The replacement/wait values are simulation defaults, not balance conclusions; unspecified Station visit duration remains TBD.

## Verification

- RED: on base `56bfa4a`, adding only `MerchantTests.cs` failed compilation because Merchant types did not exist.
- GREEN: `dotnet build src/Game.Domain`, `dotnet build tools/Game.Sim`, and `dotnet test tests/Game.Domain.Tests` succeeded; 135/135 tests passed.
- Workbook export/import round-trip retained all 11 sheets; Merchant parameters were inspected after reopening.

## Scope note

Event scheduling and waking Trainers after replacement remain in Task 7. The route state machine and replacement clock are ready for the shared Domain event queue; no standalone randomness or timer loop was introduced here.
