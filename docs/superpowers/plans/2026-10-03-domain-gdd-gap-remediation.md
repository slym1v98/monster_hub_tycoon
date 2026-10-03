# Domain GDD Gap Remediation Plan

**Goal:** Close the implementation gaps recorded in the SP1–SP6 and Quest/KPI review against the current GDD.

**Scope:** Patch existing HubWorld flows, add regression tests, and synchronize stale implementation-status notes. Keep prototype values and existing public entry points unless a tested integration requires a small extension.

## Tasks

1. **SP1 service availability:** test that a Trainer with an unmet need stays at HUB and retries while its required facility cannot operate; implement the wait in `OnDecide`.
2. **SP2 stress:** test tax-above-30% stress and stress accumulation while waiting for Merchant; wire existing tax/wait configuration to settlement.
3. **SP3 capture:** test that a weakened wild encounter can enter the existing capture/admission flow during expedition resolution, with seed-stable events and existing ownership/beds constraints.
4. **SP4 facilities:** test and enforce the correct operating facility for enhance, star-up, refine, and repair actions.
5. **SP5/Quest KPI:** test that only Director-owned stock operations advance the manipulation KPI; remove the Director fact from Trainer-to-Trainer transfers.
6. **SP6 Black Friday:** test low-tier gear offers across Trainer/Monster slots alongside existing impulse products; accepted purchases use a prototype unit price and existing affordability/acceptance rules.
7. **Documentation:** update SP2/SP3 implementation status and remove stale O11 claims after code behavior is covered.

## Verification

- Run focused tests for each task while implementing.
- Run `/Users/nofine/.dotnet/dotnet test tests/Game.Domain.Tests/Game.Domain.Tests.csproj --no-restore` after the complete patch.
- Run `git diff --check` and inspect the final diff and working tree.
