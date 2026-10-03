# SP6 review: progression, buildings, reputation, and events

## Scope and result

Reviewed implementation against the approved spec and GDD sources `docs/designs/02_HUB_Economy_Infrastructure.md`, `docs/designs/06_Events_PVE_PVP.md`, and workbook sheet `SP6 Progression & Events`.

**Result: approved; no remaining blockers.** The review found and the implementation fixed three facility lifecycle bypasses: hospital intake/emergency care, event defense membership cleanup, and consumable stall purchases while the shop was unavailable. The Hospital stall now maps to `veterinary_hospital`, matching the facility catalog and GDD.

## Verification evidence

- `/Users/nofine/.dotnet/dotnet test tests/Game.Domain.Tests/Game.Domain.Tests.csproj --no-restore` — 713 passed, 0 failed. Six existing obsolete overload warnings remain in `RebellionAndFinanceTests.cs`.
- `/Users/nofine/.dotnet/dotnet run --project tools/Game.Sim --no-restore -- progression` — passed.
- `/Users/nofine/.dotnet/dotnet run --project tools/Game.Sim --no-restore -- monster` — passed; ledgers reconcile.
- `/Users/nofine/.dotnet/dotnet run --project tools/Game.Sim --no-restore -- expedition` — passed; Trainer Gold, Treasury, supply, ownership, and item differences are zero.
- `git diff --check` — clean.
- Workbook audit: 177 SP6 parameter rows; all have value, unit, status, and source.

## Review boundary

No merge or push was performed. SP6 was committed locally on `codex/domain-progression-events`.
