# Task 1 report — pose math and secondary motion

## Scope

Implemented only the Task 1 allowlist from base `ff14590`:

- immutable/copy-safe `PoseTransform2D` and fixed-slot `PlayerPose` values;
- explicit `PlayerPoseOwnership` flags for Body, both arms, both legs, Weapon,
  Mantle, ShoulderCore, Lantern, and Tether;
- ownership-aware `PlayerPoseMixer` with clamped weight and shortest-angle rotation;
- validated action/locomotion enums, `PlayerActionFrame`, and
  `PlayerPresentationFrame` contracts;
- deterministic, bounded, separate mantle, lantern, shoulder-spool, and tether
  critically damped channels;
- pure-math unit coverage registered in the full Prologue runner.

No gameplay, coordinator, formal visual, builder, scene, import, art, audio,
UID, screenshot, or other agent-owned file was edited or staged.

## TDD evidence

### Initial RED

After writing and registering the named tests, this command failed as required:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
```

Result: exit `1`, `0` warnings, `8` errors. The errors were the expected missing
`PoseTransform2D`, `PlayerActionPhase`, and `SecondaryMotionInput` contracts.

The first behavior run also exposed and fixed a real ownership boundary defect:
Recovery cancellation returned the current transforms but retained the cancelled
action's Body/NearArm ownership mask. The named
`RecoveryCancellationReturnsToCurrentLocomotion` assertion failed until zero
action weight also removed action ownership.

Additional contract RED checks proved:

- exact public constructor names: `CS1739` when the test used required named
  argument `State` before the constructor contract matched it;
- transform interpolation: the action-ownership test failed with expected scale
  `1.12` versus actual `1.02` when scale/modulation interpolation was removed.

### GREEN

Final command results:

```text
dotnet build --no-restore: exit 0, 0 warnings, 0 errors
RunPrologueTests.cs: exit 0, exactly one PROLOGUE_TESTS_OK
```

Final evidence files were written outside the repository:

- `%TEMP%\action-motion-task1-final2-build.log`
- `%TEMP%\action-motion-task1-final2-godot.log`
- `%TEMP%\action-motion-task1-final2-console.log`

Both Godot's own log and the merged console contain exactly one
`PROLOGUE_TESTS_OK` and zero `ERROR:`, `SCRIPT ERROR`, `Parse Error`, unhandled
exception, or C# compiler-error patterns. The pre-existing warning fixtures run
as expected and did not produce engine/script/parse errors.

### Required reset-every-spring mutation

Temporarily resetting mantle, lantern, spool, tether, and the last frame on every
`SecondaryMotionModel.Step` produced exit `1` with the exact named failure:

```text
Assertion failed: mantle X is stable across equivalent delta partitions;
expected=0.024085283, actual=0.0062925816, tolerance=0.001
```

The mutation was removed with an inverse patch. The final build and full runner
were then rerun GREEN. No mutation code remains.

## Self-review

- Inputs are readonly value records; mixing returns new fixed-slot values and
  does not mutate either pose.
- Action ownership gates every overridden slot; unowned body/legs continue
  during thread preview, and cancelling Recovery returns the current locomotion
  pose and ownership.
- Position, both scale axes, RGBA modulation, and the atan2 shortest-angle arc
  interpolate continuously with finite validation.
- Four spring channels keep independent state/frequency/targets. Tether targets
  the declared lantern/spool separation rather than copying either channel.
- Every channel clamps both axes and rotation to public declared limits.
- Equivalent 60 x 1/60 and 120 x 1/120 elapsed-time partitions stay inside the
  declared `0.001` tolerance.
- Zero, negative, or non-finite delta preserves the last finite frame. Non-finite
  input components and invalid phases sanitize closed and cannot propagate NaN.
- The per-frame path creates no Godot nodes/resources or dictionaries and avoids
  enum-array iteration during pose validation.

## Fix Round 1 — independent code-quality review

Base for this fix round: `e492f2f`. The unrelated checkpoint commits
`32bfc10` and `e492f2f` were preserved and were not amended or reverted.

### Contract fixes

- Added validated `PlayerLocomotionFrame(State, NormalizedTime)` and made it an
  explicit field of the authoritative four-argument `PlayerPresentationFrame`.
- Added literal round-trip coverage for all 13 locomotion states plus clamping,
  undefined-enum, and non-finite progress cases.
- Kept a narrow three-argument compatibility constructor. It maps only the old
  coarse entity state and sets progress to zero; Task 2 must use the new
  four-argument constructor whenever StartRun, Brake, JumpStart, Apex, SoftLand,
  HardLand, or Recovery identity matters.
- Added explicit zero-weight locomotion ownership and positive-weight union
  assertions.
- Hardened interpolation with double intermediates for position, scale,
  modulation, and wrapped shortest-angle rotation so extreme finite endpoints
  cannot overflow into NaN/infinity.
- Hardened scalar springs with double state/intermediates. Very large finite
  elapsed time settles at the clamped target with zero velocity instead of
  taking the previous reset-to-origin fallback.

### Fix-round RED evidence

1. Locomotion tests first produced exit `1`, `0` warnings, and `10` expected
   compiler errors: missing `PlayerLocomotionFrame`, missing four-argument
   `PlayerPresentationFrame`, and missing `Locomotion` property.
2. After that contract reached build GREEN, the extreme finite interpolation
   test failed exactly at `extreme finite position endpoints interpolate to
   finite output`.
3. After safe pose interpolation, the large-delta spring test failed exactly:
   `a huge finite delta settles to the finite clamped target; expected=12,
   actual=0, tolerance=0.0001`.

Additional spring tests now require:

- the first update is strictly between origin and target;
- stored velocity carries motion after drive removal, then decays to rest;
- the pre-steady mantle result equals the hand-derived `12 - 24/e = 3.1708934`;
- equivalent partitions remain within `0.001` across a target reversal while
  velocity is non-zero.

Rotation tests directly require the 179° to -179° midpoint to pass through
wrapped ±180°, bounded epsilon endpoints, and no positive-weight destination
hard-cut.

### Fix-round mutation evidence

- **Snap mutation:** setting every scalar spring result directly to its target
  failed `the first spring step moves strictly between origin and its clamped
  target`.
- **Positive-weight rotation hard-cut:** routing every rotation weight greater
  than zero directly to the destination failed `179 to -179 degrees crosses
  through the wrapped 180-degree midpoint`.
- **Huge-delta velocity mutation:** retaining velocity `1` after a huge-delta
  settle failed the literal zero-velocity release value: expected `11.902357`,
  actual `11.916944`, tolerance `0.0001`.

Both mutations were removed with inverse patches before final verification.

### Fix-round final GREEN

```text
dotnet build --no-restore: exit 0, 0 warnings, 0 errors
RunPrologueTests.cs: exit 0, exactly one PROLOGUE_TESTS_OK
```

Final external evidence:

- `%TEMP%\action-motion-task1-fix1-final2-build.log`
- `%TEMP%\action-motion-task1-fix1-final2-godot.log`
- `%TEMP%\action-motion-task1-fix1-final2-console.log`

Godot log and merged console each contain exactly one `PROLOGUE_TESTS_OK` and
zero engine/script/parse/compiler-error patterns.
