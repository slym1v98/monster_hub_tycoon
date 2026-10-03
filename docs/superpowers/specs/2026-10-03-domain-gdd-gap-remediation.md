# Domain GDD Gap Remediation Specification

## Purpose

Close the behavior gaps found while reviewing Domain sub-projects SP1–SP6 and Quest/KPI against the current GDD. Preserve the GDD rules, retain Prototype status for unspecified balance values, and keep every newly introduced parameter sourced in the balance workbook.

## Requirements

1. **SP1 – Trainer AI:** when a selected service need cannot be served because its facility is unavailable, keep the Trainer at the HUB and retry later; do not redirect the Trainer to farming.
2. **SP2 – Economy and stress:** accrue stress from transaction tax above the 30% GDD threshold and configured stress while a Trainer waits for the Merchant. Report station settlement deltas without including a second copy of loan repayments.
3. **SP3 – Monster expedition:** expose weakened wild encounter targets to the existing capture resolver and ownership/admission flow. Keep seeded encounter identity stable and clamp combat HP to the owned Monster's maximum when applying results.
4. **SP4 – Equipment:** require the operating facility for equipment operations. GDD 12 maps Enhancement Stone use to the Monster Forge for every gear group; repair remains at Monster Forge for combat gear and Textile Workshop for utility gear; star-up and refine remain at the Jeweler.
5. **SP5 and Quest/KPI:** trainer-to-trainer share transfers must not emit a Director stock manipulation fact or advance the Director KPI.
6. **SP6 – Black Friday:** while the event is active, Trainers at the HUB can receive Tier 1 gear offers across the catalog slots. Gear follows existing purchase acceptance and payment rules. The runtime default for the otherwise unspecified offer price is a Prototype parameter in the workbook.
7. **Documentation:** correct stale implementation claims in GDD 02, 04, 12 and O11. Document remaining prototype and partial-simulation limits rather than presenting them as GDD-locked behavior.

## Balance data

`events.black_friday.junk_gear_price = 100 Gold/gear item` is **Prototype**. GDD 05 defines equipment slots and GDD 06 describes Black Friday impulse purchases, but neither specifies the price. The parameter and source note are in `docs/balance/MonsterHUB_Balance.xlsx`, sheet `SP6 Progression & Events`.

## Acceptance

- Regression tests cover unavailable services, tax stress, Merchant wait stress, expedition capture flow, workshop requirements, stock KPI facts, and Black Friday gear purchases.
- The Domain test project passes in full and `git diff --check` is clean.
- Review is recorded before commit; no merge or push occurs without explicit authorization.
