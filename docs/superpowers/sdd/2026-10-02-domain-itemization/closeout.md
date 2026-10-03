# Sub-project 4: Itemization / Gear closeout

## Scope delivered

- Added the 30-slot catalog (12 Trainer slots and 18 Monster slots), loadouts, owner-aware snapshots, Monster-specific gear offers, effects, and Gear Score.
- Added +1…+20 enhancement with configured materials, Gold, success/break rolls, and protection charm behavior.
- Added probabilistic Star Up with owned fodder consumption, failure downgrade, and Gold costs; added probabilistic Refine with boss crystal, distilled water, and Gold consumption on attempts.
- Added equipment wear, repair, set effects, buyback tracking, domain events, HubWorld commands, and a deterministic `Game.Sim gear` scenario.
- Added 114 gear parameter rows to `docs/balance/MonsterHUB_Balance.xlsx` with IDs, values, units, status, and sources. Star Up's fifth 20% row is TBD because GDD 05 caps equipment at five stars.

## Verification

- `dotnet test tests/Game.Domain.Tests --no-restore --filter FullyQualifiedName~Gear`: 64 passed.
- `dotnet test tests/Game.Domain.Tests --no-restore`: 663 passed.
- `dotnet run --project tools/Game.Sim --no-build -- gear`: completed; gear ledger reconciled to zero.
- `git diff --check`: clean.

## Review notes / balance decisions

- Gear effects now flow into combat snapshots; previously the Aura slot existed without influencing Monster stats.
- Monster combat slots can be addressed by Monster ID rather than implicitly equipping only the active Monster.
- GDD 13 says 2% max durability per trip and 25 Gold per repair trip. GDD 05 describes Monster wear per action/hit and Trainer Utility wear by farm time/weather. Runtime preserves the latter event model and labels its numeric rates Prototype; the trip boundary and reconciliation between these two GDD descriptions remain explicit balance decisions in the workbook/spec rather than inferred silently.
- The fifth Star Up probability (20%) is unreachable under the five-star cap and remains documented as TBD.
