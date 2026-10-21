# Task 2 — Tap/hold target policy, line of sight, and cooldown semantics

## RED

- Added the mode-aware target-policy and configured-cast-timing tests before implementation.
- `G:\godot\dotnet-sdk\dotnet.exe build .\Knight.csproj` failed as intended: `ThreadAbilitySettings` did not have `TapThresholdSeconds`, `TapAssistRange`, or `HoldRange`; `ThreadCastModel` had no settings constructor; `SelectTarget` had no four-argument mode overload; and `ThreadAbilityModel` had neither `CanConsume` nor `Consume`.

## GREEN

- Added centralized settings for tap/hold policy and cast durations, then made `ThreadCastModel` read every threshold/duration/range used by its pure state machine from those settings.
- Added `ThreadTargetPriority` and mode-aware target selection: line of sight and range are filtered first, followed by mechanism / pullable-enemy / movement-anchor priority, aim dot, distance, then id.
- Tap permits only policy-scoped neutral assist (260 px); hold with a zero aim selects nothing. The legacy target-local fallback value is intentionally not considered by the mode-aware policy.
- Heavy enemies are not a pullable target priority; their existing direct `ThreadTarget2D.ApplyPull` interaction behavior remains covered by the unchanged `ThreadInteractionTests` suite.
- `CanConsume` and idempotent `Consume(castId, now)` centralize cooldown authority. Only a caller that invokes `Consume` starts cooldown; duplicate observations of an already-consumed cast succeed without moving the boundary.

## Priority mutation

- Temporarily reversed priority ordering. `RunPrototypeTests` failed at `MechanismWinsOverNearerEnemy`: expected mechanism id `2`, received nearer enemy id `1`.
- With the same reversed policy and only the priority-unit calls temporarily omitted so the integration runner could reach it, `RunPrologueTests` failed at the real Bell-Chain interaction assertion: the mechanism did not open while a nearer valid anchor was present.
- Restored both priority comparisons and all test calls before final verification.

## Restored verification

- `G:\godot\dotnet-sdk\dotnet.exe build .\Knight.csproj` — 0 warnings, 0 errors.
- Rebuilt DLL, then `RunPrototypeTests.cs` — `PROTOTYPE_TESTS_OK`.
- Rebuilt DLL, then `RunPrologueTests.cs` — `PROLOGUE_TESTS_OK` (the runner emits its pre-existing diagnostic presentation warnings on stderr).

## Centralized configuration

- Target policy: `TapThresholdSeconds`, `TapAssistRange`, `TapConeHalfAngleDegrees`, `HoldRange`, `HoldConeHalfAngleDegrees`.
- Shared behavior: `CooldownSeconds`, pull speeds, and visual duration.
- Cast phases: `WindupSeconds`, `MinimumTravelSeconds`, `MaximumTravelSeconds`, `TravelDistanceForMaximumDuration`, `LatchSeconds`, `ApplySeconds`, and `RecoverySeconds`.
- `ThreadTuning` exports/validates/forwards the same values. `Range` and `ConeHalfAngleDegrees` remain source/serialized-resource compatibility aliases for the old immediate route.

## Compatibility shim

- The existing three-argument `SelectTarget` remains temporarily for `ThreadAbility`: zero aim routes through Tap, directed immediate input routes through Hold. `TryConsume` allocates a compatibility cast id and delegates to `Consume`.
- Task 3 must migrate `ThreadAbility` to select using the actual tap/hold mode and call `Consume` on the Travel state edge.

## Scope audit

- Modified only Task 2 implementation/tests plus this required report. `ThreadInteractionTests.cs` was deliberately not changed; its heavy-interaction regression still runs in the Prologue runner.
- Did not stage, revert, or alter the unrelated imported-art, room, UID, screenshot, or audio working-tree changes.

## Commit / concerns

- Commit: `feat: add tap and hold thread targeting` (final hash is recorded by the handoff `git log`).
- Concern: the running `ThreadAbility` remains an intentional compatibility route until Task 3 owns its state-edge migration; it cannot yet provide true held-preview gameplay semantics.

## Fix round 1 — Heavy tutorial target, strict mode-aware Tap, and tuning contract

### RED

- Added named regressions for heavy tutorial selection, zero-aim Tap rejection, 55-degree boundary rejection, real player-to-laborer interaction, complete tuning forwarding/boundaries, and configured latch/apply/recovery durations.
- After rebuilding, `RunPrototypeTests.cs` failed as intended at `HeavyEnemyIsSelectableAsTutorialInteractionTarget`: expected id `1`, actual id `2`. This demonstrated that the policy discarded `HeavyEnemy` before the receiver could run.

### GREEN

- `HeavyEnemy` now resolves to the mechanism/tutorial tier for selection only. It is not reclassified as `LightEnemy`; `ThreadTarget2D.ApplyPull` still dispatches its receiver and retains the existing non-pull heavy behavior.
- Four-argument mode-aware selection rejects zero aim for both Tap and Hold and always applies the configured 55-degree Tap cone. The old three-argument shim supplies `Vector2.Right` only for its legacy neutral Bell-Chain route.
- Expanded `ThreadTuning` contract coverage across every added setting and validation boundary. The configured cast test now advances through Latch, Apply, and Recovery and asserts all custom durations.

### Verification / scope audit

- `G:\godot\dotnet-sdk\dotnet.exe build .\Knight.csproj` — 0 warnings, 0 errors.
- Rebuilt DLL, then `RunPrototypeTests.cs` — `PROTOTYPE_TESTS_OK`.
- Rebuilt DLL, then `RunPrologueTests.cs` — `PROLOGUE_TESTS_OK` (same pre-existing diagnostic presentation warnings on stderr).
- This round touched only the expanded Task 2 allowlist: target model, unit tests, Bell-Chain integration test, tuning contract test, and this report. Unrelated dirty art/import/room/UID files were neither staged nor modified.

## Fix round 2 — Dead Bell-Back laborer target eligibility

### RED

- Added a formal Bell-Back laborer runtime regression with its real `ThreadTarget`, a valid light target, actual player thread input, death, and checkpoint reset.
- Rebuilt DLL and ran `RunPrologueTests.cs`; it failed at `a dead heavy tutorial target is removed from the real thread target group`, proving the dead interaction target still preempted the candidate group.

### GREEN / verification

- `BellBackLaborer.OnDied` now removes its own `ThreadTarget` from `thread_targets`; `ResetForCheckpoint` restores it after health reset. Fresh room instances retain the existing `ThreadTarget2D._Ready` registration path.
- The regression proves the dead heavy target leaves the group, a valid light target receives the next real player thread without player fallback pull, and reset restores the tutorial target so a real thread reaches Exposed again.
- `G:\godot\dotnet-sdk\dotnet.exe build .\Knight.csproj` — 0 warnings, 0 errors.
- Rebuilt DLL, then `RunPrototypeTests.cs` — `PROTOTYPE_TESTS_OK`.
- Rebuilt DLL, then `RunPrologueTests.cs` — `PROLOGUE_TESTS_OK` (same pre-existing diagnostic presentation warnings on stderr).

### Scope audit

- This round changed only `BellBackLaborer.cs`, `BellChainRuntimeTests.cs`, and this required report. No generic eligibility interface or unrelated dirty file was changed or staged.
