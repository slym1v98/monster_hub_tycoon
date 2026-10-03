# Domain GDD Gap Remediation Review

## Scope

Reviewed the working-tree patch against the remediation specification and relevant GDD sections for SP1–SP6 and Quest/KPI.

## Findings and disposition

- **SP4 enhancement facility:** initial group-based routing sent Utility enhancement to Textile Workshop and Aura enhancement to Jeweler. GDD 12 specifies Cường hóa tại Lò Rèn; routing now requires the Monster Forge for every gear group. Regression coverage confirms Textile Workshop alone is insufficient; existing enhancement success tests operate with the Forge.
- **SP3 capture identity:** default wild target IDs now include the Trainer ID as well as Zone/time/encounter, preventing simultaneous expeditions from producing colliding capture IDs.
- **Catalog implementation notes:** GDD 12 incorrectly said that Trainer purchases were not simulated at all and that farm loot always used the tier-one ore fixture. It now describes partial policy-based purchases and distinguishes `SimpleFarmResolver` from the default Zone encounter/loot resolver.
- **Black Friday venue:** impulse offers are processed for Trainers in `AtHub`, matching the HUB gear stall context in GDD 02/05. This leaves off-HUB Trainers for the following event-day check.
- **Finance reconciliation:** station material sale settlement previously derived its Treasury event delta from a balance interval that also contained income-loan repayment. It now records the station's net seller payment separately; this avoids double-counting the repayment in the event stream.

## Verification

- Focused equipment tests: 17 passed, 0 failed.
- Full `Game.Domain.Tests`: 741 passed, 0 failed.
- `git diff --check`: clean.
- Workbook verified: `SP6 Progression & Events!A179:E179` contains the Prototype gear-offer price, unit, and GDD source.

## Remaining limits

Loot probabilities, prices and yields remain Prototype/TBD where the GDD does not define them. AI shopping and item-use policies remain partial. Black Friday currently simulates a prototype Tier 1 offer by catalog slot rather than consuming a finite gear inventory, because the Domain has no Director gear-stock ledger.
