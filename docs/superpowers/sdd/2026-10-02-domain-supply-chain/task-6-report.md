# Task 6 report: equipment groups and consumable stalls

## Delivered

- Added the three GDD 05 workshop groups: six Monster combat slots per Monster at Monster Forge, six Trainer utility slots per Trainer at Textile Workshop, and six Aura slots per Trainer at Jeweler.
- Added 13 consumable recipes for Hospital, Restaurant, Bar, Tool Workshop, Soda Factory, and Inn. Capture balls and traps require both a blank and wood; other listed recipes use the raw material family named in GDD 12.
- Added six stall mappings. General Store sells Soda Factory's buff bottle; stalls can sell only listed products from their current stock and decrement stock on a successful purchase.
- Stall purchases reconcile Trainer spending and HUB income. Healing and rest are not goods in any stall mapping and consume no product inventory.
- Tagged the buff bottle as `TemporaryMonsterStatBuff`; exact stat changes and application remain deferred to combat/itemization work.
- Added Equipment Workshops and Stalls workbook sheets; Recipe Inputs/Outputs now have 75/73 rows. Product Catalog records the effect tag with detailed values TBD. Recipe ratios stay labeled Prototype.

## Verification

- RED: at base `710a015`, adding only `WorkshopAndStallTests.cs` failed because the workshop/stall catalogs and effect metadata did not exist.
- GREEN: `dotnet build src/Game.Domain` passed with 0 warnings/errors; `dotnet test tests/Game.Domain.Tests` passed 146/146.
- Workbook round-trip retained 13 sheets, 75 recipe inputs, 73 outputs, six stall mappings, three workshop groups, and product effect metadata.

## Limits

The three generic equipment products remain mapped by slot group; their 30 individual slot recipes, item stats, pricing, and durability belong to sub-project 4. Actual buff application belongs to combat/itemization. HubWorld service integration is completed with Task 7.
