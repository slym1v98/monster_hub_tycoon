# Domain sub-project 6 implementation plan

**Spec:** [2026-10-03-domain-progression-events-design.md](../specs/2026-10-03-domain-progression-events-design.md)  
**Execution:** Native, sequential implementation on `codex/domain-progression-events`, based on the completed Monster, gear, and finance commits.  
**Scope:** Town Hall, buildings, population, Zones, reputation, Early Access events, and simulation scenarios. Quest/KPI remains the following sub-project.

## Review gates

1. User-approved design spec (complete).
2. User review/approval of this plan before implementation (approved by user on 2026-10-03).
3. Implementation verification, independent self-review, review fixes, then one local commit. No merge or push.

## Plan

### 1. Establish exact GDD rules and workbook IDs

- Read current code contracts from the Monster/gear/finance branch: ranks, Zone access, building/stock IDs, `HubWorld` command/view/event patterns, balances, and `Game.Sim` scenarios.
- Apply the GDD table notation as rank-count / current-population-cap: Zone 2 needs 7 Rank II Trainers with current cap 10, Zone 3 needs 12 Rank III with cap 15, Zone 4 needs 17 Rank IV with cap 20, and Zone 5 needs 22 Rank V with cap 25. The denominator is context, not a ratio or an additional minimum. The new Zone raises capacity by 5.
- Inspect workbook sheets and existing IDs; add a progression/event parameter sheet or a compatible extension. Record every gate and fixed ratio/timing as Locked, and every code-required but unspecified value as Prototype/TBD with a source citation.
- Add config types whose field names or IDs map directly to workbook entries. Keep balancing decisions out of untraceable literals.

### 2. Add progression model and Zone access

- Add a deterministic progression/catalog model for five Zones, Town Hall levels/tiers, Dormitory level, building unlock milestones, and population capacity. Keep the four existing service buildings compatible while introducing catalog entries for GDD facilities.
- Add read-only progression views and stable rejection reason codes for Town Hall upgrade, building unlock, and Zone unlock commands.
- Enforce rank access at Zone entry and enforce Zone unlock gates from the verified GDD rule, required Dormitory level, and rank counts.
- Implement the approved Dormitory exception: allow the next gated Dormitory upgrade inside the currently available Town Hall tier before opening that next Zone. Do not permit unrelated facilities to bypass their GDD gates.
- Keep current residents when capacity is reduced by configuration; block new admissions beyond capacity. Do not add recruitment.
- Write focused tests first for each Zone boundary, rank access, capacity, locked facility, and the Dormitory exception; then implement until they pass.

### 3. Add building lifecycle and daily economy

- Add data-driven building definitions and lifecycle state for locked/available, build or upgrade in progress, operational, powered off, maintenance deficit, and damaged.
- Add construction, upgrade, power, upkeep, and repair commands. Validate affordability/material requirements before mutation, emit explicit Gold/material deltas, and schedule completion with simulation events.
- Keep previous building level functional throughout upgrades and apply the new level only on completion. Charge daily upkeep without debt; on a shortfall, preserve nonnegative treasury, halve efficiency/capacity, reduce quality, and restore operation on a later paid day. Power-off skips upkeep and service. Damage and repair must use the same building state and finance/stock interfaces.
- Add tests for no-op rejection, conservation, completion timing, old-level availability, daily upkeep/no debt, restoration, power-off, damage/repair, and stock-price notification integration.

### 4. Add reputation and derived outputs

- Compute a bounded deterministic reputation from service quality, prices vs fair price, average Trainer stress, and bankruptcies; expose a contribution breakdown and update event.
- Expose applicant rarity/traffic and inspection-pressure modifiers as explicit deterministic outputs for downstream consumers. Do not create applicants or silently alter recruitment because recruitment is out of scope.
- Keep unspecified coefficients as workbook Prototype/TBD. Add tests for bounds, same-state repeatability, neutral missing inputs, and monotonic direction of each GDD input.

### 5. Add event director and GDD effects

- Add deterministic event lifecycle and views using the simulation clock, event queue, and seeded RNG. Make Black Friday calendar placement exact and ensure Payday ordering follows the GDD “three days before Payday” wording.
- Add Breeding Season rare-gene/consumable-demand modifiers, Monster Flu HP/degen effects through the existing Monster/veterinary APIs, Labor Inspection triggers (`tax > 30%` or stress >= 80) and configurable consequences, Siege call-to-defense/resolution and connected combat/repair/stock effects, and paid/cooldown World Boss activation and loss damage.
- Do not duplicate combat, inventory, market, healing, or finance logic. Integrate through owning systems and emit explicit start/phase/resolution and consequence events. Use a stable event seed/order and ensure temporary effects expire or resolve.
- Any cadence, durations, penalty, enemy scale, reward, damage chance/amount, cooldown, or fee not specified in GDD must be configurable and explicitly Prototype/TBD in the workbook.
- Add event tests for fixed thresholds/effects, deterministic scheduling, transitions, end/cleanup, treasury/material changes, and interactions with combat, stock, and buildings.

### 6. Expose simulation scenarios and close the review loop

- Add `Game.Sim` commands/scenarios and concise reports for all five Zone gates, a building upgrade and upkeep shortfall, reputation shifts, and each in-scope event outcome.
- Add integration tests that exercise the same `HubWorld` API and assert ledger conservation, event observability, and repeatable results for identical seed/commands/configuration.
- Run the complete solution test suite and the new progression/event simulation scenarios. Record exact commands and result counts in a sub-project report or the plan's close-out section.
- Perform a code review against the approved spec/GDD, verify every numeric runtime parameter against the workbook source/status rows, fix findings, inspect `git diff --check`, and confirm only SP6 files changed.
- Commit locally with an SP6-scoped message after review and tests. Do not merge or push.

## Close-out

- Implementation complete; final independent review is pending after the last facility-gating fix.
- Verification: `/Users/nofine/.dotnet/dotnet test tests/Game.Domain.Tests/Game.Domain.Tests.csproj --no-restore` — 713 passed, 0 failed; six existing obsolete-overload warnings.
- Simulation scenarios passed: `/Users/nofine/.dotnet/dotnet run --project tools/Game.Sim --no-restore -- progression`, `... -- monster`, and `... -- expedition`; the Monster and Expedition reports reconcile Trainer Gold, Treasury, supply, ownership, and items to zero difference.
- `git diff --check` — clean. Workbook audit: `SP6 Progression & Events` contains 177 parameter rows; each row has a value, unit, Locked/Prototype/TBD status, and source.
- Independent review findings were fixed: block veterinary intake and emergency care when unavailable; clear event defense membership at resolution; gate product stall purchases by the operating facility. Hospital stall uses the GDD facility ID `veterinary_hospital`.
- Local commit SHA and final review outcome are recorded in [the SP6 completion report](../reviews/2026-10-03-domain-progression-events-review.md).

## Files expected to change

- `src/Game.Domain/Hub/` and related progression/building/event definitions, config, views, commands, and domain events.
- `src/Game.Domain/Simulation/EventQueue.cs` and simulation dispatch only where required for new scheduled domain events.
- `src/Game.Sim/` scenario commands/output.
- `tests/Game.Domain.Tests/` progression/building/reputation/event tests and `tests/Game.Sim.Tests/` if present.
- `docs/balance/MonsterHUB_Balance.xlsx`, this plan, a completion/review report, and any narrowly necessary GDD open-issue note.

## Required final checks

- All domain and simulation tests pass; no existing behavior regresses.
- All event effects and lifecycle transitions are deterministic and use the same Domain path for live/offline simulation.
- Every numeric configuration consumed at runtime appears in the workbook with stable ID, value, unit, status, and source.
- `git status` contains only intended SP6 files before commit, the SP6 commit is present locally, and no merge/push has occurred.
