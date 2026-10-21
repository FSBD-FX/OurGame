# Task 1 Report — One Attached-Space Boost and 620-Pixel Claw Model

## RED

Added the six required named model cases and updated the range boundary cases before changing production code. The expected build failure was captured at:

`artifacts/task11-revision/red/task1-claw-model.log`

Command: `dotnet build Knight.csproj --no-restore`

Observed failure: 7 compile errors for the missing `MechanicalClawState.Boost`, `MechanicalClawFrame.BoostConsumed`, and nine-argument `MechanicalClawInput` constructor. This confirms the new tests were red for the missing contract rather than a runtime typo.

## GREEN

- `dotnet build Knight.csproj --no-restore` — passed with 0 warnings and 0 errors.
- Focused Godot proof: `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --script res://tests/RunPrototypeTests.cs --log-file artifacts/task11-revision/green/task1-claw-focused.log --mechanical-claw-only` — exit 0; log contains `MECHANICAL_CLAW_TESTS_OK`.
- The complete `RunPrototypeTests` invocation was also run. It stops before the claw tests at the pre-existing protected `project.godot` legacy J/K InputMap assertion; that unrelated failure is separate from the focused claw GREEN proof.

## Mutation evidence

Each check saved the complete source bytes, applied one temporary mutation, built and ran the focused claw runner, and restored bytes in `finally`. Every mutation failed its intended named test (exit 1), and the post-restore SHA-256 matched the pre-mutation hash:

| Mutation | Expected failing assertion | Pre/post SHA-256 |
| --- | --- | --- |
| Boost consumption (`!boostConsumed` → `boostConsumed`) | `AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity` (`expected=Boost, actual=Swing`) | `153709C8F45EF5D121973FBEEFB6747D57226A410726B6608EE4BDB7237B238C` / same |
| Shield rejection (`!input.ShieldActive` → `input.ShieldActive`) | `AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity` (`expected=Boost, actual=Swing`) | `153709C8F45EF5D121973FBEEFB6747D57226A410726B6608EE4BDB7237B238C` / same |
| 620 inclusivity (`>` → `>=`) | `MaximumRangeIncludesExactlySixHundredTwentyPixels` (`expected=Windup, actual=Failed`) | `153709C8F45EF5D121973FBEEFB6747D57226A410726B6608EE4BDB7237B238C` / same |

Detailed mutation logs are `artifacts/task11-revision/mutation-*-final-run.log` and the corresponding `mutation-*-final.log` files.

## Implementation and files

- Added the `Boost` attached state and deterministic one-shot boost composition (360 px upward, 220 px tangent), preserving cast and target latch identity.
- Added optional `BoostPressed` and `Grounded` input fields, plus `BoostConsumed` frame state with reset on grounded contact, fresh latch, and terminal Idle.
- Changed the fixed max range from 520 px to exactly 620 px and validated finite positive boost settings while retaining the 36 px assist radius.
- Added the explicit `--mechanical-claw-only` focused mode to the owned prototype runner so the new assertions can be proven despite the unrelated dirty InputMap contract.

Modified files:

- `scripts/abilities/MechanicalClawState.cs`
- `scripts/abilities/MechanicalClawSettings.cs`
- `scripts/abilities/MechanicalClawInput.cs`
- `scripts/abilities/MechanicalClawFrame.cs`
- `scripts/abilities/MechanicalClawModel.cs`
- `tests/unit/MechanicalClawModelTests.cs`
- `tests/RunPrototypeTests.cs`
- `.superpowers/sdd/2026-08-29-terrain-grapple-dense-canyon-revision/task-1-report.md`

## Deviations and risks

- `MechanicalClawController` remains unchanged because it is outside Task 1 ownership; Task 2 owns runtime input plumbing and presentation of the new state.
- The complete prototype runner remains blocked by the known user-dirty `project.godot` legacy InputMap assertion. No protected file was edited.

## Commit

Commit SHA: `65bf17371dde01a6d6b1568cb062a32f2f7833a2` (implementation commit before this report's metadata amend; final delivery SHA is reported with the task).
