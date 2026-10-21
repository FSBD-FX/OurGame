# Task 2 Report — Typed Terrain and Runtime Space Arbitration

## Verdicts

- SPEC: PASS for the Task 2 owned scope.
- QUALITY: PASS for the Task 2 exact diff, complete materialized pre phase, new post contract, and mutation evidence.
- Global post phase: TEMPORARY RED only at the ruled Task 3 obligation (`ascent_grapple_one` 0.8 legacy sparse-route landing). No `PROLOGUE_TESTS_OK` claim is made by Task 2.

## Facts

- Base and starting HEAD: `5b2c97b57b245307c7891b608342bfe05e1af86d` (`feat: add one-shot attached claw boost`).
- Branch: `codex/player-art-pipeline`.
- Implemented explicit `GrappleSurface2D.UseCollisionHitPoint`, dedicated-anchor-first 36-pixel selection, first-collider terrain raycasts, typed `GrappleSurfaceProfile` lookup, real physics hit snapshots, and locked terrain hit identity.
- `MechanicalClawController.CurrentTarget` and `ClawFrameChanged` remain typed as `GrappleSurface2D`; the real hit point exists only in `MechanicalClawTargetSnapshot` / `MechanicalClawFrame.AnchorPoint`.
- Untyped first-hit geometry returns an obstructed snapshot and blocks typed terrain behind it; no node or art name is used to infer material or grippability.
- Attached states are `Latched`, `Reel`, `Swing`, and `Boost`. Attached Space routes one boost and suppresses only the motor's ordinary jump for that motor physics step. LMB release remains the ordinary release path; Q mode switch, damage, death, unload, and scene exit retain their existing safe cleanup paths.
- `PlayerMotor.AcceptedJumpCount` exposes real ordinary-jump acceptance to the traversal proof, so boost velocity is not misclassified as a second jump.
- HUD, line, and shuttle presentation treat `Boost` as attached. Ice remains distinct, Forbidden/untyped obstruction remain blocked, and range/no-target remain the range-ring state.
- Legacy light-enemy pull, heavy exposure, and Bell Chain activation remain adapter-driven and never retain `Latched` / `Reel` / `Swing` / `Boost` rope state.

## RED evidence

- Exact log: `artifacts/task11-revision/red/task2-terrain-runtime.log`.
- Command: `G:\godot\dotnet-sdk\dotnet.exe build .\Knight.csproj --no-restore`.
- Expected missing contracts only: three-argument `SnapshotLocked`, `PlayerMotor.SuppressJumpForCurrentPhysicsFrame`, and `GrappleSurface2D.UseCollisionHitPoint`.
- Test-internal compile mistakes were corrected before the preserved RED; the final RED log contains 0 warnings and 6 errors, all attributable to those missing production contracts.
- Traversal contract RED: `artifacts/task11-revision/red/task2-traversal-contract.log` contains 0 warnings and 4 errors, all for the intentionally missing `PlayerMotor.AcceptedJumpCount` observable written before its minimal implementation.

## GREEN evidence

- Shared checkout build: success, 0 warnings, 0 errors.
- Shared focused pre log: `artifacts/task11-revision/task2-pre-final-shared.log` contains `TASK2_TYPED_TERRAIN_TESTS_OK`; the runner then continues and exposes the known unrelated dirty-scene failure in `CombatReadabilityExperienceTests`.
- Exact-index materialized pre log: `artifacts/task11-revision/task2-pre-materialized.log` contains both `TASK2_TYPED_TERRAIN_TESTS_OK` and `PROLOGUE_TEST_PHASE_OK phase=pre`; process exit was 0.
- The executed Task 2 assertions cover wall face, ceiling underside, platform underside, platform edge, GripStone, TempleMetal, Ice, Forbidden, untyped first obstruction, exact 620 hit, beyond-620 hit, dedicated 36-pixel assist, real hit snapshot identity, scheduled attached Space boost, same cast/target, visible line/shuttle, no ordinary motor jump, no second boost, LMB Released, one-step suppression expiry, and legacy non-persistence.
- Exact-index materialized post executes the revised assisted-route assertion repeatedly with `airborne_edge=True boost=True same_cast_attached=True released_while_left=False lmb_release=True` and `second_air_jump=False`.

## Temporary post-phase red and Task 3 obligation

- Exact-index materialized post log: `artifacts/task11-revision/task2-post-materialized.log`.
- The obsolete second-Space-release assertion was replaced under the expanded Task 2 ownership. It now proves one attached boost, same cast, no release while LMB is held, no ordinary motor jump, and a later explicit LMB `Released` event.
- The only remaining materialized post failure is the unchanged safe-zone assertion for the legacy sparse `ascent_grapple_one` 0.8 sample: final position `(378.4288, 1156.0376)` versus zone x-min `380`. The approved ruling explicitly assigns restoration of full post GREEN to Task 3; Task 2 did not weaken the safe-zone assertion or change boost tuning `360/220`.

## Mutation evidence

Each mutation used an in-memory byte snapshot, ran a fresh build and Task 2 pre entrypoint, restored in `finally`, then compared SHA-256.

1. First blocker ignored: ray mask mutated from `World | ThreadTarget` to `ThreadTarget`; `the first untyped collider blocks...` failed. Resolver SHA before/after: `6D638A7614F97AAD744C8BA377196003EB4F7D8D0AE41C53C6AE34579EBF8702` / identical.
2. Anchor recomputed: terrain locked anchor mutated from `lockedAnchor` to `surface.WorldAnchor`; `terrain lock keeps the original physics hit...` failed. Resolver SHA before/after: `6D638A7614F97AAD744C8BA377196003EB4F7D8D0AE41C53C6AE34579EBF8702` / identical.
3. Space retract restored: `sampled.JumpPressed` was added back to `_retractRequested`; scheduled boost failed with expected `Boost`, actual `Released`. Controller SHA before/after: `688D80D2ABD572E9F7FB47EE612E1B0065487FD6CFCE16DBBC81CE371F0077CF` / identical.

Mutation logs: `artifacts/task11-revision/task2-mutation-blocker.log`, `task2-mutation-anchor.log`, and `task2-mutation-space.log`.

## HUD preservation proof

- Frozen evidence SHA-256 remains `D0000B94E3E1847F8397A71367AB8CAF1F3E578FA3322458DF52B086B8F885AB`.
- Cached HUD diff is exactly one Task 2 replacement: append `MechanicalClawState.Boost` to the Grippable attached-state pattern (`1` insertion, `1` deletion).
- Unstaged HUD diff is exactly the pre-existing user delta (`13` insertions, `0` deletions): the seven mode-label lines and six objective-localization lines remain outside the index.
- The index HUD blob was constructed from HEAD plus only the Task 2 replacement; the working file continues to contain both the staged Task 2 line and the untouched user-owned 13 lines.

## Traversal preservation proof

- Frozen evidence SHA-256 remains `6038DFF508368948E15C6B01D4D38AA0986DE8A2248EA0AA8C82EF005C364D21`.
- Cached traversal diff contains only the new attached-Space boost / ordinary-jump / explicit-LMB-release contract and directly supporting trace observations.
- Unstaged traversal diff is exactly the pre-existing two replacements (`2` insertions, `2` deletions): both `player.Position = new Vector2(170, 514)` probes remain user-owned `574` values outside the index.
- The traversal index blob was constructed from the working Task 2 implementation with only those two `574` values restored to HEAD's `514`; the working file retains both user edits.

## Changed files

- `scripts/abilities/GrappleSurface2D.cs`
- `scripts/abilities/MechanicalClawTargetResolver.cs`
- `scripts/abilities/MechanicalClawController.cs`
- `scripts/player/PlayerMotor.cs`
- `scripts/ui/PrologueFormalHud.cs` (Task 2 hunk only in index)
- `scripts/vfx/ThreadLineVisual.cs`
- `scripts/vfx/ThreadShuttleVisual.cs`
- `tests/unit/GrappleSurfaceTests.cs`
- `tests/integration/MechanicalClawRuntimeTests.cs`
- `tests/integration/PlayerTraversalInputTests.cs`
- `tests/integration/ThreadInteractionTests.cs`
- `tests/integration/PrologueCanyonTraversalTests.cs` (Task 2 contract hunks only in index)
- `tests/RunPrologueTests.cs`
- This report.

## Deviations and risks

- Per the updated ruling, Task 3 must update dense route geometry/fixtures so the legacy `ascent_grapple_one` 0.8 sample satisfies the unchanged safe-zone contract and restores materialized `PROLOGUE_TESTS_OK`.
- A temporary non-worktree materialization exists at `E:\OurGame\Knight\.materialized-task2-index`; recursive cleanup was rejected by the execution safety layer after its exact resolved path was verified. It contains only an exported HEAD plus the staged Task 2 index and generated build/import cache.
- Formal/paid art was not generated or imported. No `project.godot`, `.tscn`, README, spec, or plan file was modified.

## Commit

- Commit message: `feat: grapple typed terrain at real hit points`.
- Final commit SHA: the commit containing this report (recorded externally after commit because a commit cannot contain its own cryptographic hash).
