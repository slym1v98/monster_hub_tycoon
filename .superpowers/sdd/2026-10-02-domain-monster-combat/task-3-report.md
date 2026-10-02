# Task 3 report: Rebellion uses converted Trainer level

## Result

Implemented rank-aware Rebellion calculations in `RebellionModel`. The new overloads accept Trainer Rank and Level and source the fractional level from `MonsterProgression.TrainerManagementLevel`; `K = 20`, `LeadershipBase = 20`, strict `ManagementScore > LeadershipScore`, and caller-supplied leadership bonus are preserved. The previous overloads remain available with `Obsolete` attributes for compatibility. No production code currently calls the legacy API.

Added tests for the Monster-level ceiling versus fractional Trainer level, exact converted leadership score, one Rarity gap at K=20, and two Rarity gaps balanced by a 20-point bonus. Documented that Trainer management level retains the GDD's fractional Lv/5 scale in `MonsterProgression.cs`.

## Verification

- Focused command as specified in the brief (`FullyQualifiedName~RebellionAndFinanceTests`) compiled but found no tests because the test classes are named `RebellionTests` and `FinanceTests`.
- Corrected focused filter `FullyQualifiedName~RebellionTests|FullyQualifiedName~FinanceTests`: **11 passed, 0 failed**.
- `/Users/nofine/.dotnet/dotnet test tests/Game.Domain.Tests`: **269 passed, 0 failed**.
- `git diff --check`: passed.

The `dotnet` executable is installed at `/Users/nofine/.dotnet/dotnet` but is not on this shell's PATH. The obsolete compatibility API produces expected CS0618 warnings in pre-existing legacy API tests.

## Scope and review

Changed only `src/Game.Domain/RebellionModel.cs`, `src/Game.Domain/Monsters/MonsterProgression.cs`, and `tests/Game.Domain.Tests/RebellionAndFinanceTests.cs`. No workbook changes. There are no production Rebellion call sites to migrate in this checkout; the rank-aware overload is the new API for future production callers. `K`, `LeadershipBase`, fractional scale, and bonus behavior match the approved brief/spec.
