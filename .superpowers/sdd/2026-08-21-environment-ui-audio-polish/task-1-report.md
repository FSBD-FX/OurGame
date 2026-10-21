# Task 1 report — six room descriptors and landmark-specific layered art

Status: PASS

Baseline: `743800b835dbbd9e9b3ec955a364d70043571464`

## Implemented

- Added `RoomVisualDescriptor`, `RoomAtmosphereKind`, and the exhaustive `RoomVisualCatalog.For(PrologueRoomId)` mapping.
- Bound the six rooms to unique approved landmarks: broken clock, severed gear train, pendulum chains, copper pipes/fungus, vertical bell chain, and backlit broken bell.
- Replaced the shared generic room prop construction with six large-form silhouettes and landmark-specific layered compositions.
- Preserved `Art/Far`, `Back`, `Mid`, `Platforms`, `Props`, `Front`, and `Atmosphere`; tagged the binding five-role mapping (`far`, `mid`, `play`, `front`, `atmosphere`).
- Continued deriving every platform skin from the real rectangle collision center/size, without adding presentation collision.
- Added descriptor-driven Far/Mid/Front parallax, deterministic per-room atmosphere seeds/kinds, and room-specific stone/copper/shadow palettes.
- Split the foreground into independently fading `LeftForeground` and `RightForeground` leaves. `Present(descriptor, cameraVelocity, protectedScreenRegions)` accepts zero or multiple caller-supplied screen-space regions; the running visual also discovers the live player region without owning gameplay state.
- Extended `RoomPresentationProfile` so authored overrides receive the same descriptor interface.
- Updated durable README status and the environment asset row. No paid generation was invoked.

## TDD evidence

### RED 1 — missing API

Command:

`G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --nologo`

Exit: `1`. Expected failures: 9 compiler errors naming missing `RoomVisualDescriptor`, `RoomVisualCatalog`, `RoomFormalVisual.Present(...)`, `RoomFormalVisual.Room`, and `RoomFormalVisual.LandmarkId`. This proved the tests required the new public behavior before production code existed.

### RED 2 — generic serialized room art

After the minimal catalog/API compiled, command:

`Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --script res://tests/RunEnvironmentTests.cs`

Exit: `1`. Expected first structural failure:

`AwakeningCourtyard formal art serializes its unique landmark ID; expected=broken-clock, actual=environment-kit`

This proved the tests rejected the previous shared environment-kit construction instead of merely checking the catalog.

### GREEN — focused

After leaf-to-parent regeneration (environment kit, then six sibling room builders):

- `dotnet build Knight.csproj --nologo` — exit `0`, 0 warnings, 0 errors.
- `Godot ... --headless --path . --script res://tests/RunEnvironmentTests.cs` — exit `0`, `ENVIRONMENT_TESTS_OK`.

The focused tests validate all six literal room/landmark mappings, landmark uniqueness, seven stable leaves, five role mappings, replaceable large-form landmark nodes, collision-aligned platform skins, no art collision, parallax role behavior, zero-region safety, and per-leaf overlap fading.

### Required occlusion mutation

Mutation: temporarily changed the overlap branch in `ForegroundOccluder.SetProtectedRegions` from `PlayerBehind = true` to `false`, keeping the overlapping foreground column opaque.

Build remained green; focused runtime command exited `1` at the required named assertion:

`Assertion failed: only the foreground leaf overlapping the named combat region fades`

The mutation was restored with a patch, the environment kit and six rooms were regenerated, and the focused runner returned `ENVIRONMENT_TESTS_OK`.

### Self-review regression cycle — caller parallax persistence

Staged-diff self-review found that live protected-region discovery called `Present` with zero camera velocity on the next `_Process`, erasing a caller-supplied parallax offset. A new runtime assertion was added first. Focused runner exited `1` with the expected failure:

`live protected-region discovery does not erase caller-supplied camera parallax; expected=-1.6, actual=-0`

`RoomFormalVisual` now retains the last caller velocity while refreshing live protection regions. Build returned 0 warnings/0 errors, focused runner returned `ENVIRONMENT_TESTS_OK`, and the complete prologue runner was rerun after the fix.

### Full gate

Final auditable command output is stored locally in `task-1-full-suite.log` (not staged):

`Godot ... --headless --path . --script res://tests/RunPrologueTests.cs`

Exit: `0`; final marker: `PROLOGUE_TESTS_OK`. Output contains only existing named negative-test warnings (invalid presentation override/cue, missing guidance target, rejected invalid flow transition) and no Task 1 error.

## Running proof

Captured `tests/presentation/PrologueEnvironmentPresentation.cs` with hardware OpenGL/NVIDIA at 1280×720, fixed 30 FPS, 120 frames / 4 seconds. Capture exited `0` with `PROLOGUE_ENVIRONMENT_PRESENTATION_COMPLETE frame=120`. Reviewed frame 60 at `screenshots/task1-environment/frame00000060.png`: all six silhouettes and landmarks are visibly distinct, platform rhythm remains readable, and ambience avoids danger red. Capture-only owner reparent warnings pre-exist in the presentation harness; the runtime scenes and test output are clean.

## Task-owned files

- `README.md`
- `scripts/config/RoomPresentationProfile.cs`
- `scripts/presentation/ForegroundOccluder.cs`
- `scripts/presentation/RoomFormalVisual.cs`
- `scripts/presentation/RoomVisualDescriptor.cs` (+ generated `.uid`)
- `scripts/presentation/RoomVisualCatalog.cs` (+ generated `.uid`)
- `tools/build/art/BuildPrologueEnvironmentKit.cs`
- `tools/build/prologue/PrologueBuildParts.cs`
- six room builders listed in the brief
- six generated room scenes listed in the brief
- `tests/integration/PrologueEnvironmentStructureTests.cs`
- `tests/integration/PrologueEnvironmentRuntimeTests.cs`
- `tests/RunEnvironmentTests.cs` (+ generated `.uid`)
- this report

`scenes/presentation/PrologueEnvironmentKit.tscn` was regenerated only to validate the builder and remains unstaged because it is outside the brief's owned scene list; its residual diff is generated-ID/serialization noise. All unrelated dirty/import/generated files remain untouched and unstaged.

## Self-review

- Binding roots for collisions, navigation, spawns, exits, checkpoints, targets, camera limits, guidance, and room roots were not changed.
- Presentation adds no `CollisionObject2D`; platform art is a read-only projection of rectangle geometry.
- No ambience uses danger red; interaction gold, thread cyan, danger red, and impact white remain reserved.
- Foreground opacity is applied to the overlapping leaf node only, never to `Art` or the whole `Front` group.
- Catalog switch is exhaustive for the current six-value enum and fails closed for an unknown value.
- Remaining taste concern: the six-up proof intentionally scales rooms down; normal-speed route review may still tune detail density and landmark framing, but no automated or structural requirement is open.

---

## Fix Round 1/5 — reviewer Critical/Important remediation

Status: PASS

### Fixed

- Caller-supplied screen regions are copied and retained by `RoomFormalVisual` until the next explicit `Present(...)` call. Each `_Process` merges them with live read-only discovery instead of replacing them.
- Added presentation-local `RoomProtectionRegionAdapter`. It reads the existing scene tree only and covers the player group, active Enemy collision layer, live `ThreadTarget2D`, active `ThreadLineVisual`, current `AttackTelegraphVisual` shape, Fallen Bellkeeper body/feet, and enabled EnemyAttack/PlayerAttack collision geometry including ground danger. It does not write gameplay state.
- `Platforms` now remains at `Vector2.Zero` for every camera velocity. `Props` retains its independent low-amplitude play-depth parallax.
- Broken-Tooth Corridor now uses two broad, low-frequency toothed polygons (10 and 9 teeth; 30/38 px root-to-tip spans) and two separated axle stubs with a large visible break.
- Environment tests now run asynchronously in a real `SceneTree`; the focused runner and full runner both await the lifecycle coverage.

### New RED evidence

All new behavior assertions were written before their production fixes.

1. Gear silhouette RED after a clean build:

   `Assertion failed: Broken-Tooth drive gear has a low-frequency toothed silhouette at proof scale`

2. After the gear-only fix and Corridor regeneration, non-zero parallax RED:

   `non-zero camera velocity never shifts collision-derived platform skins; expected=(0, 0), actual=(-1.0500001, 0.52500004)`

3. After fixing Platforms, real lifecycle persistence RED:

   `Assertion failed: caller-supplied named combat region survives multiple live _Process frames`

4. A final negative-case review added a disabled EnemyAttack shape to the same live harness. Before the adapter distinguished authored-but-disabled geometry, focused runtime exited `1` at:

   `Assertion failed: disabled attack geometry is not treated as a current protected attack shape`

5. The final current-shape audit then kept the attack shape enabled while disabling the real `Area2D` monitoring state. Focused runtime exited `1` at:

   `Assertion failed: non-monitoring attack geometry is not treated as a current protected attack shape`

Each RED command was:

`dotnet build Knight.csproj --nologo` followed by
`Godot ... --headless --path . --script res://tests/RunEnvironmentTests.cs`

The build portion was 0 warnings / 0 errors for every behavior RED; runner exit was `1` at the named expected assertion.

### GREEN coverage

`tests/integration/PrologueEnvironmentRuntimeTests.cs` now proves:

- caller-supplied named combat regions survive three real `_Process` frames;
- player, active enemy, thread target, active thread line, current attack telegraph, Boss feet, and current ground danger each fade their overlapping leaf in a live `SceneTree`;
- the telegraph leaf reaches readable faded opacity and is never left covered;
- authored but disabled collision and enabled shapes on a non-monitoring attack `Area2D` are not reported as current attack shapes;
- zero regions remain safe;
- non-zero camera velocity leaves `Platforms` at zero and every skin aligned to its real `Geometry/<platform>` collision while Props may move independently.

`tests/integration/PrologueEnvironmentStructureTests.cs` now requires proof-scale radial tooth spans and separated axle stubs for Broken-Tooth Corridor.

After restoring the mutation and regenerating all six room scenes:

- `dotnet build Knight.csproj --nologo` — exit `0`, 0 warnings, 0 errors.
- focused `RunEnvironmentTests.cs` — exit `0`, `ENVIRONMENT_TESTS_OK`.
- full `RunPrologueTests.cs` — exit `0`, `PROLOGUE_TESTS_OK`; an explicit `^ERROR:` scan returned no matches, and only the existing named negative-test warnings appear. Full output is local at `task-1-fix-round-1-full-suite.log` and is not staged.

The lifecycle fixtures use the shared two-frame queued teardown helper. This removed a transient managed-handle release error seen during an intermediate full-suite cleanup run; the final focused and full logs are clean.

### Opaque mutation regression

Temporarily changed the overlap branch to keep the combat/telegraph leaf opaque (`PlayerBehind = false`). Build stayed green and the real lifecycle focused runner exited `1` at:

`Assertion failed: caller-supplied named combat region survives multiple live _Process frames`

The mutation was restored before regeneration and final gates.

### Refreshed proof review

Re-recorded `screenshots/task1-environment/frame.png`: 1280×720, 30 FPS, 120/120 frames, exit `0`, `PROLOGUE_ENVIRONMENT_PRESENTATION_COMPLETE frame=120`. Reviewed frame 60. Broken-Tooth now reads as a gear train at the six-up scale: both large wheels have unmistakable broad teeth, while the separated axle stubs leave a strong central fracture. No small monochrome noise was added; the other five room silhouettes remain unchanged and readable. The capture harness retains its known owner-reparent warnings only.

### Fix-owned files

- `scripts/presentation/RoomFormalVisual.cs`
- `scripts/presentation/RoomProtectionRegionAdapter.cs`
- `tools/build/prologue/PrologueBuildParts.cs`
- `scenes/prologue/rooms/BrokenToothCorridor.tscn`
- `tests/integration/PrologueEnvironmentRuntimeTests.cs`
- `tests/integration/PrologueEnvironmentStructureTests.cs`
- `tests/RunEnvironmentTests.cs`
- `tests/RunPrologueTests.cs`
- this report

The other five rooms were regenerated and verified as requested but remain unstaged because their residual diffs are generated serialization/ID noise with no Fix Round 1 semantic change. All unrelated dirty files remain untouched and unstaged.

---

## Fix Round 2/5 — production sibling search boundary

Status: PASS with unrelated full-runner cleanup concern recorded below.

### RED

The lifecycle harness was first changed to the production hierarchy: `LevelRoot` owns sibling `PlayerRoot/Player` and `RoomRoot/AwakeningCourtyard/Art`; the active `ThreadLineVisual` is a child of `Player`. Enemy, thread target, telegraph, Boss, and ground danger remain under the current room. An unrelated enemy was also placed in a third LevelRoot sibling.

Command:

`dotnet build Knight.csproj --nologo` followed by
`Godot ... --headless --path . --script res://tests/RunEnvironmentTests.cs`

Build exit: `0`, 0 warnings, 0 errors. Focused runner exit: `1`, at the expected named assertion:

`Assertion failed: live read-only adapter protects Player across _Process frames`

This was the old `art.GetParent()` search boundary failing against the real sibling layout; no production code had been changed for Round 2 before this RED.

### GREEN implementation and coverage

`RoomProtectionRegionAdapter` now resolves a bounded presentation scope from the Art branch:

- the direct current-room branch beneath the nearest `RoomRoot` ancestor; and
- only the direct `PlayerRoot` sibling belonging to that same LevelRoot.

It does not traverse the LevelRoot or any other sibling scene instance, and it remains read-only. Standalone room/art fixtures retain the prior local-parent fallback.

The real lifecycle test proves across multiple `_Process` frames that sibling `PlayerRoot/Player` and its child `ThreadLine` are both protected. The existing active enemy, thread target, telegraph, Boss feet, and ground-danger assertions remain green. A named negative assertion proves an unrelated LevelRoot sibling enemy is outside the search boundary.

Focused final command:

`dotnet build Knight.csproj --nologo` followed by
`Godot ... --headless --path . --script res://tests/RunEnvironmentTests.cs`

Exit: `0`; build 0 warnings / 0 errors; final marker: `ENVIRONMENT_TESTS_OK`.

Full command:

`Godot ... --headless --path . --script res://tests/RunPrologueTests.cs`

Exit: `0`; final marker: `PROLOGUE_TESTS_OK`. Output is stored locally in `task-1-fix-round-2-full-suite.log` and is not staged. The log contains intermittent Godot managed-handle cleanup errors originating in earlier P0/FallenBellkeeper/P1 tests before `PrologueEnvironmentRuntimeTests` runs; the runner continues through the new environment lifecycle coverage and succeeds. This is outside the bounded Round 2 change and is reported rather than hidden.

### Fix-owned files

- `scripts/presentation/RoomProtectionRegionAdapter.cs`
- `tests/integration/PrologueEnvironmentRuntimeTests.cs`
- this report

---

## Task 1 gate follow-up — Fallen Bellkeeper managed-handle teardown

Status: PASS.

### Root cause and RED evidence

`FallenBellkeeperRuntimeTests.Dispose(...)` bypassed the repository's formal teardown protocol: it queued the node and awaited only one process frame. The death-recovery fixtures dispose a full `PrologueLevel` containing `PrologueAudioDirector`, then immediately load and instantiate the next full level. Under full-suite resource/finalizer pressure, the stale teardown crossed that boundary and Godot reported `System.InvalidOperationException: Handle is not initialized` from `PackedScene.Instantiate`.

A new focused runner, `tests/RunFallenBellkeeperRuntimeTests.cs`, executes two consecutive natural Fallen Bellkeeper lifecycle passes. A single focused pass and the two-pass runner did not reproduce the timing-sensitive error in isolation; no artificial GC workaround is present. The auditable pre-fix external ERROR-scanning RED is the full-suite log already captured before the fix:

`task-1-fix-round-2-full-suite.log`

Scan result: exactly one `PROLOGUE_TESTS_OK`, but `2` exact `ERROR: System.InvalidOperationException: Handle is not initialized.` matches. The supplied stack crosses `ArenaResetAlwaysReplacesExactlyOneBoss` teardown into `DeathRecoveryIsCompleteFromCriticalBossStates`/`LoadBossLevel` at `PackedScene.Instantiate`.

### Minimal fix

The local `Dispose(...)` still releases all input actions, then delegates node cleanup to `TestSceneTeardown.FreeSceneAfterAudioDrain(tree, node)`. Full levels now shut down and release `PrologueAudioDirector`, wait for a real AudioServer mix, then queue-free and await two process frames. Boss-only and room-only fixtures use the same helper's no-Audio two-frame path. No shared helper or production gameplay file changed.

An investigated explicit `GC.Collect`/finalizer-drain hypothesis was rejected and fully removed after it caused Godot's `gchandle.is_released()` fatal path. Neither the focused runner nor the committed test contains active GC calls.

### Focused GREEN

Command:

`dotnet build Knight.csproj --nologo` followed by
`Godot ... --headless --path . --script res://tests/RunFallenBellkeeperRuntimeTests.cs`, with an external scan for `^ERROR:`, `Handle is not initialized`, and `System.*Exception`.

Results: build exit `0`, 0 warnings / 0 errors; focused runner exit `0`; exactly one `FALLEN_BELLKEEPER_RUNTIME_TESTS_OK`; `ERROR_OR_EXCEPTION_MATCHES=0`. Output: `task-1-fallen-bellkeeper-teardown-green.log` (local, unstaged).

### Full GREEN gate

Command:

`Godot ... --headless --path . --script res://tests/RunPrologueTests.cs`, with the same external log scan plus an exact `^PROLOGUE_TESTS_OK$` count.

Results: runner exit `0`; exactly one `PROLOGUE_TESTS_OK`; `ERROR_OR_EXCEPTION_MATCHES=0`. Existing named warning-test output remains expected. Output: `task-1-handle-gate-green.log` (local, unstaged).

### Gate-follow-up owned files

- `tests/integration/FallenBellkeeperRuntimeTests.cs`
- `tests/RunFallenBellkeeperRuntimeTests.cs`
- this report
- `progress.md`
