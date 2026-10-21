# Task 2 report — model-owned phases and approved first-level tuning

## Status

Complete. Rust-Shell, Grave-Lamp Moth, Bell-Back Laborer, and Fallen Bellkeeper models now publish model-owned `AttackTimelineFrame` values. Attack identities are allocated only when Telegraph begins, remain stable through Active and Recovery, and actor frames preserve compatibility for existing presentation consumers through a defaulted final timeline parameter.

## Implementation

- Added exact approved ordinary-enemy timings: Rust `0.65 / 0.14 / 0.65`, Moth `0.70 / 0.35 / 0.70`, Laborer `0.75 / 0.20 / 0.70`.
- Added exact Boss timings by kind through `BossAttackTiming` and both `BossTuning.TimingFor(...)` and immutable `BossSettings.TimingFor(...)`: Sweep `0.80 / 0.25 / 0.70`, LeapSlam `0.90 / 0.25 / 0.75`, GroundWave `1.00 / 0.30 / 0.75`, HammerThrow `0.85 / 0.80 / 0.70`.
- Set Boss tuning health to `12`; all first-level enemy/Boss contact damage defaults are `1`.
- Kept existing state machines and exposed exact phase duration and remaining time from their timers.
- Locked Moth and Boss world targets at Telegraph-to-Active and retained them through Recovery. Rust and Laborer retain launch-side/facing locks for the same window.
- Added a real HammerThrow Recovery timeline over the existing armored scheduling state. The initial phase-two armored dwell is `0.35` seconds so the longer approved `0.85`-second hammer Telegraph preserves the previously verified time-to-first-hammer window.
- Added and wired `BellBackLaborerTuning` into the real runtime. Detection, motion, gravity, state timing, contact damage, and model construction now use its immutable settings. The runtime starts its hitbox with the model-owned attack ID.
- Added named tests for inclusive boundaries, stable identities, unique Boss kinds, locked targets, the complete approved timing table, Boss health, one-segment contact damage, and duplicate attack-ID damage rejection.

## Files

- `scripts/enemies/RustShellModel.cs`
- `scripts/enemies/RustShellFrame.cs`
- `scripts/enemies/RustShellSettings.cs`
- `scripts/enemies/moth/GraveLampMothModel.cs`
- `scripts/enemies/moth/GraveLampMothFrame.cs`
- `scripts/config/EnemyArchetypeTuning.cs`
- `scripts/enemies/laborer/BellBackLaborerModel.cs`
- `scripts/enemies/laborer/BellBackLaborerFrame.cs`
- `scripts/config/BellBackLaborerTuning.cs` (new)
- `scripts/enemies/laborer/BellBackLaborer.cs` (controller-approved runtime wiring)
- `scripts/boss/FallenBellkeeperModel.cs`
- `scripts/boss/FallenBellkeeperFrame.cs`
- `scripts/config/BossTuning.cs`
- `tests/unit/RustShellModelTests.cs`
- `tests/unit/GraveLampMothModelTests.cs`
- `tests/unit/BellBackLaborerModelTests.cs`
- `tests/unit/FallenBellkeeperModelTests.cs`
- `.superpowers/sdd/2026-08-21-attack-readability-and-difficulty/task-2-report.md`

## TDD evidence

### RED

Command:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
```

Exact decisive output:

```text
tests\unit\FallenBellkeeperModelTests.cs(38,38): error CS0246: 未能找到类型或命名空间名“BossAttackTiming”(是否缺少 using 指令或程序集引用?)
生成失败。
0 个警告
1 个错误
```

Exit code: `1`. This was the intended missing production contract, not a syntax or fixture error.

### GREEN implementation build

Command:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
```

Output:

```text
Knight -> E:\OurGame\Knight\.worktrees\player-art-pipeline\.godot\mono\temp\bin\Debug\Knight.dll
已成功生成。
0 个警告
0 个错误
```

The first full behavioral run exposed one existing hard-coded runtime cadence assumption: with the approved Hammer Telegraph increased from `0.50` to `0.85`, the Boss was still in `HammerTelegraph` after the integration fixture's fixed 75 frames. The minimal model/tuning correction changed only the pre-hammer armored dwell from `0.65` to `0.35`, preserving the prior total time to HammerThrown while retaining the approved `0.85` readable Telegraph. The next full runner passed.

### Mutation 1 — Rust timing

Temporary mutation: `RustShellSettings.TelegraphSeconds = 0.35f`.

Command:

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrototypeTests.cs
```

Exact decisive output:

```text
Assertion failed: approved Rust-Shell thrust telegraph is exactly 0.65 seconds; expected=0.65, actual=0.35, tolerance=1E-06
at Knight.Tests.Unit.RustShellModelTests.ApprovedRustShellTimingTableIsExact()
```

Exit code: `1`. Restored to `0.65` before the next mutation.

### Mutation 2 — Boss identity regeneration on Active

Temporary mutation: call `BeginAttack()` on the phase-one Telegraph-to-Active transition.

Command:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs
```

Exact decisive output:

```text
Assertion failed: Sweep Active preserves its Telegraph identity; expected=1, actual=2
at Knight.Tests.Unit.FallenBellkeeperModelTests.PhaseOneCyclesThreeAttacksThroughTelegraphActiveAndRecovery()
```

Exit code: `1`. Removed the Active regeneration before final verification.

## Final verification

Command:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrototypeTests.cs
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs
```

Exact success output:

```text
已成功生成。
0 个警告
0 个错误
PROTOTYPE_TESTS_OK
PROLOGUE_TESTS_OK
```

Combined exit code: `0`. The prologue runner also emitted its existing intentional negative-fixture warnings for invalid presentation roots, a missing guidance target, and a rejected invalid flow transition; none was an engine/script/parse failure.

Self-review command:

```powershell
git diff --check -- <all Task 2 owned source and test files>
```

Exit code: `0`; only Git's existing LF-to-CRLF checkout notices were printed. A targeted residue scan confirmed Rust is restored to `0.65`, Boss Active contains no `BeginAttack()`, Boss health is `12`, and all centralized contact defaults are `1`.

## Self-review

- Requirements: all four frames expose `AttackTimeline`; every attack family has a distinct kind; IDs increment once at Telegraph; phase timings/remaining are model-owned; Moth/Boss targets are locked and stable; exact approved timings and Boss health are asserted.
- Damage: the owned test suite demonstrates one first-level contact hit removes one segment and a repeated source attack ID is rejected even with invulnerability disabled. Player invulnerability was not changed.
- Architecture: Task 1 immutable types were consumed unchanged. No presentation code, telegraph builder, scene, import, art tool, screenshot, or unrelated unique-ID file was modified or staged.
- Compatibility: existing frame construction sites remain valid because `AttackTimeline` is the final defaulted positional parameter.
- Runtime: Bell-Back Laborer tuning is independently live and validated, including hitbox damage and model-owned combat identity.

## Concerns / next-task boundaries

- Task 3 must pass live player world positions into the optional Moth/Boss model step target argument, derive true shapes from actual hitboxes, publish read-only presentation events, and switch the remaining Rust/Moth/Boss hitbox IDs to their model timelines.
- Task 4 owns regenerating the Boss scene from `BossTuning` so the currently authored `MaxHealth = 18` scene becomes the approved `12`, and owns generated-scene validation of the exact contact damage values. This task deliberately did not modify those builders/scenes.

## Fix Round 1

This section supersedes the original report's temporary `0.35`-second phase-two armored-dwell decision. Review correctly identified that compensating for the approved `0.85`-second HammerThrow telegraph by shortening an unrelated cadence was out of scope. The initial/default armored dwell is restored to `0.65` seconds.

### Controller ruling and scope

The controller explicitly granted this fix round ownership of `tests/integration/FallenBellkeeperRuntimeTests.cs`. Its hard-coded 75/147/148-frame HammerThrown waits encoded the old shorter cadence. They now observe the public `CurrentFrame.State` transition with named budgets:

- `PhaseTwoToHammerThrownBudgetFrames = 92` covers the preserved `0.65` armored dwell plus approved `0.85` telegraph with a two-frame scheduling tolerance.
- `TransitionToHammerThrownBudgetFrames = 168` additionally covers the existing `1.20` transition.

No Boss gameplay cadence was shortened to satisfy the integration test. The deferred Minor request for a real Laborer runtime tuning regression test was intentionally not addressed.

### Fixes and exact covering tests

1. `BossTuning.PhaseTwoArmoredSeconds` default is restored from `0.35` to `0.65`. `tests/unit/FallenBellkeeperModelTests.cs` asserts the exact default plus the inclusive `0.649 + 0.001` model boundary. `tests/integration/FallenBellkeeperRuntimeTests.cs` observes HammerThrown within named state-transition budgets.
2. `EnemyArchetypeTuning` and `BellBackLaborerTuning` now reject every `ContactDamage` value except `1`. Focused positive-above-one regressions are in `tests/unit/GraveLampMothModelTests.cs` and `tests/unit/BellBackLaborerModelTests.cs`; existing default and zero-boundary coverage remains intact.
3. `RustShellSettings` now owns a centralized validation contract for detection/attack ranges, movement, every duration, and first-level contact damage. `RustShellModel` rejects invalid settings in its constructor. `tests/unit/RustShellModelTests.cs` covers negative and non-finite ranges/durations, invalid movement, `ContactDamage = 2`, valid defaults, named errors, and constructor rejection.

### Independent RED evidence

Commands for the Moth, Laborer, and Boss REDs:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs
```

Moth RED, exit `1`:

```text
Assertion failed: first-level Moth tuning rejects positive contact damage above one; expected=1, actual=0
at Knight.Tests.Unit.GraveLampMothModelTests.FirstLevelMothTuningRejectsContactDamageOtherThanOne()
```

After the minimal Moth validator correction, Laborer RED, exit `1`:

```text
Assertion failed: first-level Laborer tuning rejects positive contact damage above one; expected=1, actual=0
at Knight.Tests.Unit.BellBackLaborerModelTests.FirstLevelLaborerTuningRejectsContactDamageOtherThanOne()
```

After the minimal Laborer correction, Boss cadence RED, exit `1`:

```text
Assertion failed: Boss preserves the existing 0.65-second initial phase-two armored dwell; expected=0.65, actual=0.35, tolerance=1E-06
at Knight.Tests.Unit.FallenBellkeeperModelTests.ApprovedBossTimingHealthAndDamageTableIsExact()
```

Rust validation RED command:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
```

Exact decisive output, exit `1`:

```text
tests\unit\RustShellModelTests.cs(98,35): error CS1061: “RustShellSettings”未包含“Validate”的定义
tests\unit\RustShellModelTests.cs(121,35): error CS1061: “RustShellSettings”未包含“Validate”的定义
生成失败。
0 个警告
2 个错误
```

Each RED named the missing or incorrect production behavior before its implementation changed.

### Focused GREEN and full runner

Command:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrototypeTests.cs
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs
```

Exact success output:

```text
已成功生成。
0 个警告
0 个错误
PROTOTYPE_TESTS_OK
PROLOGUE_TESTS_OK
```

Combined exit code: `0`. The prologue runner emitted only its established intentional negative-fixture warnings described earlier in this report.

### Fix-round self-review

- Boss tuning now preserves both independent approved facts: `0.65` initial armored dwell and `0.85` HammerThrow telegraph. Runtime tests wait for the observable state instead of encoding a stale exact frame count.
- Ordinary first-level tuning validation cannot accept contact damage `0`, negative, or greater than `1`; the only valid value is `1`.
- Rust validation rejects all non-finite inputs before timers/range comparisons enter the model. The model constructor is the enforcement boundary.
- Task 1 immutable contracts, player invulnerability, Boss/model phase logic, presentation code, builders, scenes, art/import noise, and the deferred Laborer runtime test remain untouched.
