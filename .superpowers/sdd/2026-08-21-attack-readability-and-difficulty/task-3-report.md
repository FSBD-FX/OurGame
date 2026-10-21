# Task 3 report — true-shape telegraph runtime and distinct enemy poses

## Result

- Added collision-authoritative `AttackShapeFactory` conversion and a presentation-only `AttackTelegraphVisual` leaf.
- All four enemy runtimes now publish validated `Action<AttackPresentationFrame> AttackFrameChanged` frames, present them to the optional stable `VisualRoot/AttackTelegraph` leaf, and emit `AttackTelegraph` on warning while retaining `AttackStarted` for Active.
- Gold preparation, locked red danger, Active white confirmation, immediate red clear, and Recovery damage disable are covered by runtime tests.
- Moth and Boss adapters inject live world targets into their existing models and freeze the target halfway through Telegraph; the Moth's dive input remains frozen after lock.
- Boss attack geometry is assigned to the real `AttackHitbox/AttackCollider` before presentation is derived: convex sector (Sweep), circle at the locked landing point (LeapSlam), ground rectangle published as `GroundBand` (GroundWave), and aligned capsule along the hammer/chain path (HammerThrow).
- Rust-Shell, Moth, Laborer, and every Boss attack phase expose distinct named pose markers and visibly different transforms.

## TDD evidence

### RED

Command:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'; $env:DOTNET_ROOT_X64='G:\godot\dotnet-sdk'; $env:DOTNET_ROLL_FORWARD='LatestMajor'; & 'G:\godot\dotnet-sdk\dotnet.exe' build 'Knight.csproj'
```

Output: exit `1`, `CS0246` at `AttackTelegraphRuntimeTests.cs`: missing `AttackTelegraphVisual`. This was the expected missing-feature failure before production implementation.

### GREEN

Command:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'; $env:DOTNET_ROOT_X64='G:\godot\dotnet-sdk'; $env:DOTNET_ROLL_FORWARD='LatestMajor'; & 'G:\godot\dotnet-sdk\dotnet.exe' build 'Knight.csproj'; if ($LASTEXITCODE -eq 0) { & 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script 'res://tests/RunPrologueTests.cs' --log-file 'task3-green-tests-4.log' }
```

Output: build succeeded with `0` warnings / `0` errors; runner finished with `PROLOGUE_TESTS_OK`.

### Geometry mutation

Temporary mutation: multiplied only the published rectangle presentation extent in `AttackShapeFactory` by `.75f`; the collision shape was unchanged.

Command:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'; $env:DOTNET_ROOT_X64='G:\godot\dotnet-sdk'; $env:DOTNET_ROLL_FORWARD='LatestMajor'; & 'G:\godot\dotnet-sdk\dotnet.exe' build 'Knight.csproj'; if ($LASTEXITCODE -eq 0) { & 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script 'res://tests/RunPrologueTests.cs' --log-file 'task3-mutation-tests.log' }
```

Output: build succeeded, runner exited `1` at the named assertion `attack factory preserves the real rectangle world bounds`; expected `(96.036224, -53.1928), (87.92755, 50.385597)`, mutated presentation `(107.027176, -46.8946), (65.94565, 37.7892)`. The mutation was then restored with `apply_patch`.

### Restored final runner

Command:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'; $env:DOTNET_ROOT_X64='G:\godot\dotnet-sdk'; $env:DOTNET_ROLL_FORWARD='LatestMajor'; & 'G:\godot\dotnet-sdk\dotnet.exe' build 'Knight.csproj'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; & 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script 'res://tests/RunPrologueTests.cs' --log-file 'task3-final-full-runner.log'
```

Output: exit `0`; build `0` warnings / `0` errors; `PROLOGUE_TESTS_OK`. Log scan for `ERROR:|SCRIPT ERROR|Parse Error` returned no matches. The runner's existing deliberate warning-path fixtures still print their expected warnings.

## Owned files

- `scripts/presentation/AttackShapeFactory.cs` (new)
- `scripts/presentation/AttackTelegraphVisual.cs` (new)
- `scripts/presentation/PresentationCue.cs`
- `scripts/enemies/RustShellSquire.cs`
- `scripts/enemies/moth/GraveLampMoth.cs`
- `scripts/enemies/laborer/BellBackLaborer.cs`
- `scripts/boss/FallenBellkeeper.cs`
- `scripts/presentation/RustShellFormalVisual.cs`
- `scripts/presentation/GraveLampMothFormalVisual.cs`
- `scripts/presentation/BellBackLaborerFormalVisual.cs`
- `scripts/boss/FallenBellkeeperFormalVisual.cs`
- `tests/integration/AttackTelegraphRuntimeTests.cs` (new)
- `tests/integration/EnemyFormalVisualTests.cs`
- `tests/integration/FallenBellkeeperFormalVisualTests.cs`
- this report

## Self-review

- Confirmed `AttackTelegraphVisual` never reads or mutates `Area2D.Monitoring`, damage, or collision resources; it draws converted points only in its local canvas space.
- Confirmed Boss collision shape and transform are applied before `Hitbox.Begin` and before `AttackShapeFactory.FromCollision` is called.
- Confirmed every runtime validates the immutable frame before publishing it, clears its visual/event frame on `None`, reset, and death, and uses the model's stable attack ID for `Hitbox.Begin`.
- Preserved existing numeric `PresentationCue` values by appending `AttackTelegraph` rather than inserting it ahead of serialized cues.
- Directly assembled only the future stable `VisualRoot/AttackTelegraph` test leaf; no Task 4 builder or generated scene was changed.
- Reviewed `git diff --check` for owned paths; no owned whitespace errors.

## Concerns / handoff

- Task 4 still needs to serialize the leaf at `VisualRoot/AttackTelegraph`; until then runtimes safely publish the event and skip the missing optional visual.
- Current audio/VFX directors do not own an explicit `AttackTelegraph` mapping in this task's file boundary, so their generic fallback remains until the relevant presentation/audio task adds a dedicated warning treatment.
- `README.md` durable milestone/asset status remains for the parent integration pass because this task's ownership was restricted to the brief files.

## Fix Round 1

### Findings addressed

- Boss attack-facing now locks when a returned timeline exposes a new non-zero `AttackId`. Sweep polygon/offset, GroundWave offset, and Hammer start/fallback direction use that locked facing through Telegraph, Active, and Recovery even if the live player crosses the Boss.
- Returned timeline identity is prepared before the frame target is selected. A new attack clears the previous target lock first, then selects a live target for the new kind, so the first LeapSlam Telegraph frame cannot inherit Sweep's target.
- Hammer tests now verify real capsule/presentation bounds equality, target and path stability across a side crossover, Active monitoring with red clear and white confirmation, `boss_hammer_release`, Recovery monitoring off, and stable Recovery follow-through geometry.

### RED 1 — locked Sweep facing

Command:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'; $env:DOTNET_ROOT_X64='G:\godot\dotnet-sdk'; $env:DOTNET_ROLL_FORWARD='LatestMajor'; & 'G:\godot\dotnet-sdk\dotnet.exe' build 'Knight.csproj'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; & 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script 'res://tests/RunPrologueTests.cs' --log-file 'task3-fix1-red-facing.log'
```

Output: build succeeded with `0` warnings / `0` errors; runner exited `1` at `locked Sweep collision does not flip when the player crosses sides`. Bounds moved from expected `(-168, -72), (148, 108)` to `(20, -72), (148, 108)`, proving the live-facing flip.

### RED 2 — first LeapSlam frame inherited the old target

After implementing only the facing lock, command:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'; $env:DOTNET_ROOT_X64='G:\godot\dotnet-sdk'; $env:DOTNET_ROLL_FORWARD='LatestMajor'; & 'G:\godot\dotnet-sdk\dotnet.exe' build 'Knight.csproj'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; & 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script 'res://tests/RunPrologueTests.cs' --log-file 'task3-fix1-red-leap.log'
```

Output: build succeeded with `0` warnings / `0` errors; runner exited `1` at `first LeapSlam Telegraph uses the new live target, not previous lock (-150, 20)`, proving new-attack identity was reset too late.

### GREEN — amended integration coverage and full runner

Command:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'; $env:DOTNET_ROOT_X64='G:\godot\dotnet-sdk'; $env:DOTNET_ROLL_FORWARD='LatestMajor'; & 'G:\godot\dotnet-sdk\dotnet.exe' build 'Knight.csproj'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; & 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script 'res://tests/RunPrologueTests.cs' --log-file 'task3-fix1-green.log'
```

Output: exit `0`; build succeeded with `0` warnings / `0` errors; full runner emitted `PROLOGUE_TESTS_OK`. The focused Task 3 assertions run synchronously through `EnemyFormalVisualTests.Run()` inside this full runner.

### Fix-round self-review

- Confirmed the live `Facing` remains available for general Boss presentation while all directional attack geometry reads `_lockedAttackFacing` only.
- Confirmed `PrepareAttackIdentity` executes on the model's returned timeline before `frameTarget` and collision geometry are selected.
- Confirmed target lock and facing lock persist for the same `AttackId` through Recovery and reset before the first frame of the next attack.
- Confirmed the expanded Hammer assertions compare presentation against independently sampled real collision geometry at Telegraph, Active, and Recovery.
- Only `FallenBellkeeper.cs`, `AttackTelegraphRuntimeTests.cs`, and this report changed in Fix Round 1.
