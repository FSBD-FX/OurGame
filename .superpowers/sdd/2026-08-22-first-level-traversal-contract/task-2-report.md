# Task 2 report — typed room traversal contracts and honest upper route

## Scope and commit

- Base: `46e5b714495655d28fbb443402c2e39e66942776`
- Commit: `82a8a29bf0ac427e32c4c8a6aa506dc672de03ad` (`feat: author first-level traversal contracts`).
- Production scope stayed inside the three traversal metadata types, the shared builder helper, the six named room builders, and the six named leaf scenes.
- Test ownership was expanded by the controller only for the three stale UpperPendulum anchor literals in `tests/unit/P1LoopStructureTests.cs` after the binding geometry made the old assertions fail.
- `tests/RunPrologueTests.cs` required no registration change. No player, tuning, thread runtime, guidance, smoke, capture, README, asset/import, or parent `PrologueLevel.tscn` file was changed or staged.

## Pre-task generated-scene snapshot

The six room files were hashed before any regeneration. Only three were already dirty, all with UID-only regeneration churn:

| Room | Pre-task status | SHA-256 | Git numstat |
| --- | --- | --- | --- |
| AwakeningCourtyard | modified | `1D9CDD098FEF652A0787FD8D404D69A478AEC7592E716D40AF55274982CFD047` | 94/94 |
| BrokenToothCorridor | clean | `88ECCF0A626114C2684C7FE2EE8849398368FEB135956E00C814F99A90518343` | 0/0 |
| UpperPendulum | clean | `68F5D13DD4BB966421094323E655066B3EB33FD6DD75F5437377430C2C8A6C01` | 0/0 |
| LowerBuriedMachinery | modified | `61993975870B5BE079CC02CD8344EF0898B879EEDF14F67D68731349C8FABF56` | 111/111 |
| BellChainMechanism | modified | `8BE2D8924052BB3712571B8F7A4327AFBBDC8A780A563A77B78DCE6F0D1CF45B` | 101/101 |
| FallenBellkeeperArena | clean | `10B8FE7B7AFEF071987D0FBFF469D39594D7DE9C8EB65153DEF3B91EAA9714F5` | 0/0 |

An immutable safety copy was kept outside the worktree at `E:/OurGame/Knight/task2-room-snapshot-20260823`; the raw generated GREEN output was retained at `E:/OurGame/Knight/task2-room-generated-20260823` for comparison.

Godot regeneration changed every scene's resource IDs and node UIDs. To avoid staging that unrelated churn, the six scenes were restored from their exact pre-task snapshots and only the generated semantic delta was reapplied: one traversal script resource, the typed `TraversalContracts` nodes, and the six approved UpperPendulum gameplay/art positions. The three pre-dirty rooms were staged from snapshot-to-current zero-context patches directly into the index. Cached audit found `0` staged UID changes while `612` UID +/- lines remained unstaged, preserving the exact 306 pre-existing UID substitutions.

## RED

After adding only the packed-scene structure tests:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
```

Result: expected compile RED, `0` warnings / `4` errors:

- `CS0234`: namespace `Knight.Prologue.Traversal` missing.
- `CS0246`: `TraversalSegment2D` missing.
- `CS0246`: `TraversalSegmentKind` missing.
- `CS0246`: `TraversalAbility` missing.

A direct Godot runner invocation at this point used the previously compiled DLL, so its stale green marker was explicitly rejected as evidence.

## Implementation and generated-room evidence

- Added metadata-only `TraversalSegment2D`, `TraversalSegmentKind`, and flagged `TraversalAbility`.
- Validation fails closed on empty/duplicate IDs, non-finite/non-positive or sub-48 px zones, illegal masks, Dash on mandatory routes, missing/non-anchor assist targets, wrong collision layer, range over 400 px, and the wrong facing half-plane.
- Authored exactly ten entries under stable room-local `TraversalContracts/<segment-id>` paths.
- All six leaf builders ran successfully and printed `packed and validated`; the dirty parent `BuildPrologueLevel.cs` was intentionally not run per the task brief.
- UpperPendulum uses the binding values: `LowerSwing=(210,120)`, `MidSwing=(610,-80)`, `HighSwing=(1010,-300)`, `MechanismLanding=(1430,-300)`, `AnchorLow=(170,-90)`, `AnchorHigh=(500,-310)`, `ReboundTarget=(900,-530)`.
- The two tall launch rectangles end at their source platform tops and put their authored centers within the formal 400 px hold range (approximately 385 px and 395 px). Task 3's real physics probe remains the reachability authority.
- Packed reload tests prove the root/segment owner chains, exact manifest, stable paths, real Anchor target resolution/kind/layer/range/facing, and no Dash on mandatory segments.

## GREEN

Commands:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' `
  --headless --path . --script res://tests/RunPrologueTests.cs
```

Fresh result after the minimal semantic scene reconciliation:

- build: success, `0` warnings / `0` errors
- runner exit: `0`
- `PROLOGUE_TESTS_OK`: exactly `1`
- runtime `ERROR:` lines: `0`

## Mutation proof

`mid_to_high_swing` was temporarily removed from the UpperPendulum builder, followed by a required rebuild, UpperPendulum leaf regeneration, and full suite run. Result:

```text
MUTATION_EXIT=1
Assertion failed: UpperPendulum has the exact mandatory traversal manifest count; expected=4, actual=3
```

The call was restored, the project rebuilt, the leaf regenerated, and the full suite returned to exit `0`, one marker, and zero runtime errors.

Invalid metadata is proved without mutating production: the committed structure test creates an in-memory zero-width `LaunchZone` plus duplicate `SegmentId`, requires named `LaunchZone` and `duplicate` errors, and passes in the fresh GREEN suite.

## Cached diff audit

Staged files:

- six named leaf scenes
- `scripts/prologue/traversal/TraversalAbility.cs`
- `scripts/prologue/traversal/TraversalSegment2D.cs`
- `scripts/prologue/traversal/TraversalSegmentKind.cs`
- `tests/integration/PrologueEnvironmentStructureTests.cs`
- controller-authorized `tests/unit/P1LoopStructureTests.cs`
- six named room builders
- `tools/build/prologue/PrologueBuildParts.cs`

`git diff --cached --check` is clean. The cached six-scene diff is focused at 105 insertions / 9 deletions; it contains only traversal metadata plus approved UpperPendulum positions/art alignment. No staged imports, parent scene, README, capture, assets, or unrelated dirty work.

## Concerns / handoff

- Task 2 validates declared metadata, not real reachability. Task 3 must still drive actual InputMap/physics probes and may adjust only a named failing segment per the plan ruling.
- The tall UpperPendulum launch zones describe the airborne casting corridor while their lower edges align with platform tops. Task 3 should confirm the player naturally crosses those zones during a normal jump and one thread cast.
- Godot's generator continues to emit whole-file resource/UID churn. Future builder runs should repeat the same semantic reconciliation or first fix deterministic scene ID output; broad staging would reintroduce unrelated noise.

# Fix Round 1

## Review findings addressed

- The packed-scene test now binds every one of the exact ten segment IDs to a real source and target `StaticBody2D` rectangle collision surface and derives the formal player capsule from `Player.tscn` (`radius=20`, `height=72`).
- For every segment, the 20/50/80 launch samples must have capsule-margin support, the standing body center must be inside the launch band, no sample may already satisfy the landing, launch and landing may not overlap, and the landing must contain a capsule-supported standing center on the intended target.
- All ten launch zones were corrected. This includes the seven review-listed defects plus three additional edge-margin defects that the new RED exposed sequentially: BrokenToothCorridor `entry_to_anchor_landing`, UpperPendulum `lower_to_mid_swing`, and UpperPendulum `mid_to_high_swing`.
- AwakeningCourtyard `jump_step_to_attack_ledge` now lands on the actual `AttackLedge` top and contains the standing player center.
- `TraversalSegment2D.Validate` now rejects a resolved assisted-anchor position with non-finite X or Y before distance/facing arithmetic and names the invalid `AssistTargetPath`.
- The exact ten-entry manifest, masks, stable paths, binding UpperPendulum platform/anchor positions, and no-Dash mandatory contract remain unchanged.

## TDD evidence

Initial REDs against `82a8a29`:

```text
Assertion failed: AwakeningCourtyard/spawn_to_jump_step launch sample 50% is capsule-supported by Geometry/SpawnFloor
Assertion failed: non-finite resolved anchor position fails closed with a named finite-position error
```

After the finite-position production guard, build was `0` warnings / `0` errors and the anchor regression passed while the packed-room support RED remained. Corrections were then applied one at a time; each fresh full runner exposed the next unsupported real surface:

```text
BrokenToothCorridor/entry_to_anchor_landing launch sample 80% ... Geometry/TeachingLedge
UpperPendulum/entry_to_lower_swing launch sample 50% ... Geometry/EntryFloor
UpperPendulum/lower_to_mid_swing launch sample 80% ... Geometry/LowerSwing
UpperPendulum/mid_to_high_swing launch sample 80% ... Geometry/MidSwing
UpperPendulum/high_to_mechanism_landing launch sample 80% ... Geometry/HighSwing
LowerBuriedMachinery/entry_to_arena launch sample 50% ... Geometry/EntryFloor
BellChainMechanism/arrival_to_chain_floor launch sample 50% ... Geometry/UpperArrival
FallenBellkeeperArena/retry_to_arena_floor launch sample 50% ... Geometry/LeftDais
```

The first generated GREEN and the later snapshot-reconciled GREEN both produced build `0` warnings / `0` errors, runner exit `0`, exactly one `PROLOGUE_TESTS_OK`, and zero runtime `ERROR:` lines.

## Fix-round mutations

Unsupported-zone mutation: FallenBellkeeperArena's launch zone was temporarily returned to `Rect2(-278, 130, 96, 48)`. The full runner exited `1` with the named failure:

```text
FallenBellkeeperArena/retry_to_arena_floor launch sample 50% is capsule-supported by Geometry/LeftDais
```

Non-finite-anchor mutation: UpperPendulum's `Geometry` ancestor was temporarily assigned `Position.X=NaN`, preserving the binding local `AnchorLow=(170,-90)` literal. The full runner exited `1` with the named validation failure:

```text
UpperPendulum/entry_to_lower_swing ... AssistTargetPath 'Geometry/AnchorLow' must resolve to a finite room-local position
```

Both mutations were restored. The final fresh verification again produced build `0` warnings / `0` errors, runner exit `0`, exactly one `PROLOGUE_TESTS_OK`, and zero runtime `ERROR:` lines.

## Generated-scene preservation

Before fix-round regeneration, exact six-room snapshots and hashes were stored at `E:/OurGame/Knight/task2-fix1-room-snapshot-20260823`; raw generated output was retained at `E:/OurGame/Knight/task2-fix1-room-generated-20260823`. After all six leaf builders validated, each leaf was restored from its exact pre-fix snapshot and only the generated semantic metadata delta was reapplied: ten `LaunchZone` values and one AwakeningCourtyard `LandingZone` value. No platform geometry, stable node path, owner chain, parent `PrologueLevel.tscn`, or unrelated room content was changed. The three pre-existing UID-only room hunks remain in the working tree and are excluded from the fix commit by index-only semantic staging.

## Fix-round cached audit

- The focused index contains 15 authorized paths: this report, six leaf scenes, six matching room builders, `TraversalSegment2D.cs`, and `PrologueEnvironmentStructureTests.cs`.
- `git diff --cached --check` is clean.
- Cached scene audit: `0` UID/resource-ID/unique-ID lines and exactly `22` launch/landing +/- lines (the 11 intended old/new metadata substitutions).
- Unstaged pre-existing room audit: `0` launch/landing lines and `612` UID/resource-ID/unique-ID +/- lines remain outside the index (the exact 306 pre-existing substitutions).
- Parent `PrologueLevel.tscn`, Player/runtime tuning, thread runtime, guidance/smoke/capture/README/assets/imports, and every unrelated dirty path remain unstaged.

# Fix Round 2

## Transform-aware structural authority

- Scope is test-only: `PrologueEnvironmentStructureTests.cs` plus this append-only report. No production metadata, builder, scene, Player, thread, tuning, or other file changed.
- The ten-entry packed-scene surface mapping is unchanged.
- Rectangle platform bounds now transform all four shape corners from `CollisionShape2D` through the body and every `Node2D` ancestor into room-local space. Collider offset/scale plus body and `Geometry` transforms therefore affect source support, standing height, and target landing support.
- The helper accepts finite non-zero axis-aligned scale/translation, including reflected scale, and fails by name on unsupported rotation/skew, non-finite transforms, missing/non-rectangle platform shapes, or invalid shape dimensions.
- Player body-origin extents now come from the real packed `Player/BodyCollider` capsule transformed relative to the Player root. Support uses the transformed min/max X offsets and standing-center Y uses the transformed bottom extent; the formal radius/height are read from the packed shape rather than duplicated as literals.
- The secondary assisted-target check now resolves the target through its full ancestor chain into room-local coordinates, matching production validation instead of reading `target.Position`.
- Focused cases exercise a translated/scaled platform ancestor, an offset/scaled rectangle collider, an offset/scaled player capsule under an ancestor, named rotation/skew rejection, and named non-finite rejection. Packed runtime regressions exercise source-collider offset, Player body-collider offset, and assisted-target ancestor translation.

## RED

With only the packed source-collider regression added against the prior helper, build remained `0` warnings / `0` errors and the full runner exited `1` because the old body-position/raw-size calculation incorrectly stayed green internally:

```text
Assertion failed: AwakeningCourtyard/spawn_to_jump_step packed source CollisionShape2D offset fails the named surface-support gate
```

The regression moves the instantiated packed `Geometry/SpawnFloor/Collision` by `+600` X without moving its `StaticBody2D`, proving the exact false-positive gap.

## GREEN and mutation

After the transform-aware helper and regressions, the first full run returned exit `0`, exactly one `PROLOGUE_TESTS_OK`, and zero runtime `ERROR:` lines.

For the mandatory opposing mutation, the same packed source collision remained offset and its corrected gate was temporarily exposed directly. Build stayed `0` warnings / `0` errors and the full runner exited `1` with the named segment and surface failure:

```text
AwakeningCourtyard/spawn_to_jump_step launch sample 20% is capsule-supported by Geometry/SpawnFloor
```

The direct exposure was removed; the runtime mutation continues to be restored by freeing its isolated packed instance. The final fresh build/full-run result was build `0` warnings / `0` errors, runner exit `0`, exactly one `PROLOGUE_TESTS_OK`, and zero runtime `ERROR:` lines.
