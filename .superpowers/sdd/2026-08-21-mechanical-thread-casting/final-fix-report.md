# Mechanical Thread Casting — Plan-wide Final Fix Round 1

Date: 2026-08-22

Base: `e4b7ab1`

Scope: cooldown press gating, observable failed feedback, recovery movement/jump cancellation, focused model/runtime/presentation/HUD coverage, and attempt-bound presentation proof markers.

## Review findings and root causes

1. A fresh press entered `Preview` before cooldown readiness was checked. The later release-time readiness check prevented the pull but still exposed target selection, candidate/strand/shuttle presentation, and 65% movement scale; holding could also bridge into the ready window.
2. `EnterFailed` set duration and remaining to zero even though `ThreadAbilitySettings.VisualSeconds` was already centralized and validated. The next physics step cleared the state. NoTarget/Cooldown also cleared both procedural leaves, and `ThreadFailed` was not replayed as a casting cue, so these failures could have no readable player-space feedback.
3. Recovery only reacted to dash/pause-style cancellation. Normal movement and jump intent left the recovery presentation running until its timer ended.

The two ledger-deferred minors — hold begun during Failed, and pre-threshold dash/pause intent — were intentionally not changed.

## RED evidence

Each RED was captured independently against the pre-fix production behavior after a successful 0-warning/0-error build:

- Cooldown gate: after consuming a real CastId, a fresh held press during cooldown expected named `Failed/Cooldown`; actual state was `Idle`. The real runtime companion also exposed the old press path before the release-time readiness check.
- Failed duration: expected `DurationSeconds == RemainingSeconds == 0.12`; actual values were `0` and the next step returned `Idle`.
- Recovery cancellation: a real cancellation frame during Recovery expected immediate `Idle`; actual frame was `Failed/Cancelled` (and movement/jump were not cancellation sources at all).

The previously incorrect runtime expectation that Failed clear after one physics frame was replaced with a multiple-60-FPS-frame dwell plus a bounded wait for controlled cleanup.

## GREEN implementation

### Cooldown press gate

- `ThreadCastModel.StepIdle` checks readiness on the fresh press before it can enter tap/hold classification or Preview.
- A blocked press enters named `Failed/Cooldown` for the standard visual window. The held continuation is ignored while Failed, and after returning to Idle it cannot auto-fire without a new press edge.
- No candidate, target lock, line strand, Preview shuttle, or 0.65 movement scaling is created during the blocked hold. Cooldown ownership and exactly-once consumption remain unchanged.
- Pure and real InputMap/runtime tests first complete and consume a real cast, then hold through the threshold and cooldown-ready boundary. They assert one Apply/Hit total, no Preview/autofire, zero current target, clean line, full movement scale, named HUD feedback, and an accurate cooldown ring.

### Observable Failed window

- `ThreadCastModel.EnterFailed` now uses `ThreadAbilitySettings.VisualSeconds` for both duration and remaining, decrements it each step, and returns to Idle only when the window expires.
- NoTarget/Cooldown show a compact player-local procedural spool/short needle/X and never fabricate a target line.
- Obstructed/OutOfRange/TargetLost preserve their real failure endpoint and line/X through the same window, then retract and clear.
- The coordinator maps Failed to `ThreadFailed` and keeps formal/placeholder bodies in the failure cue without replaying audio/VFX side effects every frame.
- The placeholder has an independently testable recoiled `ThreadFailedPoseActive` silhouette, broken needle geometry, spool and arc; the formal path maps the cue to its recovery clip. Focused tests assert actual clip/pose/geometry persistence and cleanup rather than glow alone.
- Runtime tests cover NoTarget, Cooldown, Obstructed, and TargetLost dwell, HUD text, line/shuttle semantics, zero gameplay effect, and terminal cleanup.

### Recovery cancellation

- Real non-zero `MoveAxis` or `JumpPressed` while Recovery produces the model's cancellation input; `Cancelled` during Recovery transitions directly to Idle.
- Earlier phases retain named `Failed/Cancelled` behavior.
- Recovery cancellation clears line, shuttle, pose, and movement restriction immediately without emitting ThreadFailed, applying/refunding again, or altering the cooldown already consumed.
- Separate real InputMap physics tests prove horizontal motion and jump velocity still reach `PlayerMotor`, while Apply/Hit remain exactly one and cooldown remains active.

## Mutation evidence

All mutations were temporary, run against named tests, observed failing, and restored before final verification:

1. Bypassed the fresh cooldown gate: cooldown test failed `expected Failed, actual Idle`.
2. Forced failure duration back to zero: duration test failed `expected 0.12, actual 0`.
3. Removed the Recovery-specific direct-Idle branch: recovery test failed `expected Idle, actual Failed`.
4. Final integration audit exposed that the initial failure-endpoint branch could pass caster origin to the normal Travel shuttle. The formerly monotonic-only assertion allowed a constant position, so it was strengthened to require a real departure from the caster. With the bad branch retained, the focused runner failed `the real runtime shuttle leaves the caster and advances toward the locked target during Travel`; restoring the normal target endpoint returned `THREAD_CAST_RUNTIME_TESTS_OK`.

Post-restore inspection confirmed the fresh cooldown gate, `VisualSeconds` duration/remaining assignment, Recovery direct-Idle branch, and normal Travel target endpoint are all present. No mutation remains.

## Real runtime, HUD, and presentation evidence

- Focused real fixture mounts `ThreadAbility`, `ThreadLineVisual`, `ThreadShuttleVisual`, real `PlayerInputReader`, motor/health, physics targets/occluders, and a minimal real `PrologueFormalHud` state/cooldown surface.
- Named runtime assertions cover cooldown hold across threshold/ready, NoTarget/Cooldown local cue with no fake line, Obstructed/TargetLost real endpoint dwell, movement/jump Recovery cancellation, motor response, exactly-one Apply/Hit, and preserved cooldown.
- The attempt-bound presentation run emitted:
  - `PROLOGUE_THREAD_PRESENTATION_FRAME failure=NoTarget frame=618` and `DWELL ... frames=7`
  - all six successful cast phase markers through `Recovery frame=675`
  - `PROLOGUE_THREAD_PRESENTATION_RECOVERY_CANCEL frame=677 hits=1 failures=1` and `_OK`
  - `failure=Cooldown frame=680` and `DWELL ... frames=8`
  - the held input continued beyond cooldown ready with no new attempt/Preview/fire
  - `failure=Obstructed frame=737` and `DWELL ... frames=8`
  - `PROLOGUE_THREAD_PRESENTATION_SEQUENCE_OK`
- The observer was corrected to record the successful Anchor attempt's Recovery terminal before returning from success handling; this makes the new recovery-cancel marker attempt-bound instead of frame-only.

## Verification commands and outputs

- `dotnet build Knight.csproj --no-restore` → build succeeded, `0 Warning(s)`, `0 Error(s)`.
- focused `PrologueVfxRuntimeTests.cs` runner → `THREAD_CAST_RUNTIME_TESTS_OK`.
- `RunPrototypeTests.cs` → `PROTOTYPE_TESTS_OK`.
- actual `PrototypeSmoke.cs` → `PROTOTYPE_SMOKE_OK`, including its original final reset-to-initial-checkpoint assertion.
- actual `PrologueCriticalPathSmoke.cs` → all six thread state markers and `PROLOGUE_CRITICAL_PATH_OK`.
- full `RunPrologueTests.cs` → `PROLOGUE_TESTS_OK`.
- direct `ProloguePresentation.cs` proof → `PROLOGUE_PRESENTATION_COMPLETE frame=2280` with every new attempt-bound marker above.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1` → post-fix exit 0, `PROLOGUE_VERIFY_OK`; production audit reported `max_process_ms=13.68`, `max_nodes=362`; the verifier's deliberate forced-splice negative check failed as expected before the normal proof passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly` → exit 0, `PROLOGUE_CAPTURE_OK`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1` → post-fix exit 0, `PROLOGUE_CAPTURE_OK`; `prologue.avi` is 2,280 frames / 38 seconds / 122,286,404 bytes and `boss-retry.avi` is 600 frames / 10 seconds / 34,621,900 bytes.

## Exact and dense frame review

Fresh 1280x720 frame capture was inspected at the exact event frames and across each dwell/cleanup boundary:

- Frames 618, 622, 625: NoTarget shows red named HUD plus a recoiled player-local short-needle failure cue across multiple frames; there is no line to the distant enemy; both pose and feedback clear by the boundary.
- Frames 654, 658, 662, 666: the needle/shuttle visibly leaves the caster and advances monotonically along three distinct strands toward the upper anchor, closing the final distance before Latched.
- Frames 675, 677, 678: visible Recovery is followed by movement cancellation at frame 677; the strand/shuttle and cast pose are cleared without a second hit, and subsequent frames remain clean while the cooldown ring continues.
- Frames 680, 684, 688, 714, 718, 719: Cooldown shows a brief local failure cue and named ring text, never a candidate diamond or target strand. After the ring becomes ready, the still-held input remains visually/gameplay idle and does not auto-fire.
- Frames 737, 741, 745, 746: Obstructed shows the red line/retraction and X at the real nearby wall endpoint for the dwell, then clears back to the ready HUD and neutral pose.
- Frames 2277–2279: the visible tail is stable on the title screen with no leaked thread presentation.

Objective result: the runtime timing changes are visible and readable at exact frames and normal density. Human normal-speed taste/play review remains open as requested; no automated claim replaces it.

## Scope and preservation

Intended allowlist:

- `scripts/abilities/ThreadAbility.cs`
- `scripts/abilities/ThreadCastModel.cs`
- `scripts/presentation/PlayerPlaceholderVisual.cs`
- `scripts/presentation/PlayerPresentationCoordinator.cs`
- `scripts/vfx/ThreadShuttleVisual.cs`
- `tests/integration/PlayerFormalVisualTests.cs`
- `tests/integration/ThreadCastRuntimeTests.cs`
- `tests/presentation/ProloguePresentation.cs`
- `tests/unit/PlayerAnimationMapTests.cs`
- `tests/unit/ThreadCastModelTests.cs`
- `tools/capture-prologue.ps1`
- `tools/verify-prologue.ps1`
- this report

No builder or scene serialization was changed for this fix. Existing unrelated imported assets, room/player scenes, audio, UID, screenshot, and art-review changes were left unstaged and untouched. `README.md` was not changed because delivery truth did not change.

## Concerns

- No known automated blocker remains.
- The long-form proof intentionally uses a proof-only one-second ThreadTuning cooldown so cooldown failure, dwell, ready transition, and held-no-autofire can all be visibly captured without changing serialized production tuning.
- Normal-speed human feel review remains open; all structural/runtime/verifier gates are green.

# Mechanical Thread Casting — Plan-wide Final Fix Round 2

Date: 2026-08-22

Base: `33b755e`

Scope: clear the player presentation cue override exactly when the authoritative thread cast leaves an active/Failed state for Idle. The deferred report-claim minor, hold-begun-during-Failed minor, and pre-threshold dash/pause minor were not changed.

## Finding and root cause

Recovery movement/jump cancellation already made the model Idle and cleared line/shuttle/gameplay state on the authoritative edge, but `PlayerFormalVisual` retained its timed `ThreadCast` override. `PlayerPresentationCoordinator` could apply cues but had no narrowly scoped way to clear a cue override. The placeholder happened to recover through its locomotion frame path, so this was formal-player-specific observable residue rather than a gameplay-state defect.

## RED evidence

- The natural Recovery completion fixture first exposed the old production behavior: expected the stored locomotion clip after authoritative Idle, actual `ThreadCast`.
- The requested real generated-Player movement-cancellation fixture then failed on the exact `CastFrameChanged(Idle)` boundary: `real movement intent clears formal ThreadCast to the stored locomotion base ... expected Fall, actual ThreadCast`.
- The test records the pre-cast base clip instead of assuming grounded Idle; the generated fixture is airborne and legitimately uses `Fall`. A temporary Idle literal also exposed a real `Land` cue in the fixture, so the assertion was corrected to test the contract—restoration of the stored base—without weakening the synchronous boundary requirement.

## GREEN implementation

- `IEntityVisual2D.ClearCueOverride()` is a default no-op capability. Existing enemy visuals require no broad edits.
- `PlayerFormalVisual` cancels its override timer, restores `CurrentClip` from `_baseClip`, and animates immediately. `PlayerPlaceholderVisual` clears its cue timer and redraws consistently.
- `PlayerPresentationCoordinator` remembers the previous authoritative thread state and invokes the clear capability only for `Preview/Windup/Travel/Latched/Apply/Recovery/Failed -> Idle`. Ordinary locomotion frames do not clear unrelated attack/hurt/death cues.
- Real generated-Player tests separately cancel Recovery with `MoveRight` and `JumpPressed`. At the same authoritative Idle event they capture and assert the restored formal clip plus clear line/shuttle. Apply, compatibility Hit, and cooldown remain exactly once/preserved, with no Failed event.
- Natural Recovery and Failed completion restore the stored base clip. Existing active-phase replay assertions continue to prove the formal clip stays `ThreadCast` during all active casting phases.
- A focused direct-visual assertion proves ordinary Fall locomotion does not clear a `Hurt` override; only the explicit capability restores the Fall base.

## Mutation evidence

The coordinator's `_visual?.ClearCueOverride()` call was temporarily bypassed while all other production and test code remained intact. The named real movement fixture failed exactly at the synchronous formal-clip assertion: expected `Fall`, actual `ThreadCast`. Restoring the call returned the full Prologue runner to `PROLOGUE_TESTS_OK`. No mutation or diagnostic print remains.

## Runtime and presentation proof

- The generated Player fixture uses its real `PlayerInputReader`, `PlayerMotor`, `ThreadAbility`, `PlayerPresentationCoordinator`, `PlayerFormalVisual`, `ThreadLineVisual`, and `ThreadShuttleVisual` with a real physics target/floor.
- The long-form observer checks gameplay/line/shuttle/cooldown on the authoritative Recovery-to-Idle signal, then records the formal visual on the next presentation process tick because its SceneTree subscription is installed before the player's coordinator. It emitted `PROLOGUE_THREAD_PRESENTATION_RECOVERY_CANCEL frame=678 hits=1 failures=1 formal_clip=Rise` and `_OK`. The integration fixture, where the generated Player is fully ready before the observer subscribes, proves the production clear itself is synchronous on that authoritative event.
- The jump path is covered by the separate real InputMap integration fixture; it is not duplicated in the scripted visual capture.

## Verification commands and outputs

- `dotnet build Knight.csproj --no-restore` -> build succeeded, `0 Warning(s)`, `0 Error(s)`.
- focused `PrologueVfxRuntimeTests.cs` runner -> `THREAD_CAST_RUNTIME_TESTS_OK`.
- `RunPrototypeTests.cs` -> `PROTOTYPE_TESTS_OK`.
- actual `PrototypeSmoke.cs` -> `PROTOTYPE_SMOKE_OK`, including its original final reset-to-initial-checkpoint assertion.
- actual `PrologueCriticalPathSmoke.cs` -> `PROLOGUE_CRITICAL_PATH_OK`.
- full `RunPrologueTests.cs` -> `PROLOGUE_TESTS_OK`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1` -> exit 0, `PROLOGUE_VERIFY_OK`; production audit reported `max_process_ms=23.93`, `max_nodes=362`, and the verifier's deliberate forced-splice negative check failed as expected before the normal proof passed.
- fresh `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly` -> exit 0, `PROLOGUE_CAPTURE_OK`, 2,280 main frames plus 600 boss-retry frames.
- fresh `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1` -> exit 0, `PROLOGUE_CAPTURE_OK`; `prologue.avi` is 2,280 frames / 38 seconds / 122,288,484 bytes and `boss-retry.avi` is 600 frames / 10 seconds / 34,621,900 bytes.

## Exact and dense frame review

- Frames 618, 622, 625: NoTarget failure pose/ring remains readable across the configured dwell and then clears without a fake target strand.
- Frames 654, 658, 662, 666: ThreadCast body pose remains continuous while the shuttle advances monotonically along three visibly separate strands toward the upper anchor.
- Frames 675-679: 675-676 retain Recovery pose and strands; frame 677 has already cleared strand/shuttle and restored the airborne base silhouette; frame 678 records `formal_clip=Rise`; 679 remains clean with the consumed cooldown ring intact and no second hit.
- Frames 680, 684, 688, 718: Cooldown local cue/ring remains readable, then returns ready without target strand or automatic recast.
- Frames 737, 741, 746: Obstructed failure retains the real wall endpoint/X through its dwell, then restores the base pose and ready HUD.
- Frames 2277-2279: the visible tail is stable on the title screen with no leaked thread presentation.

Human normal-speed play/taste review remains open as requested; no automated observation is presented as a replacement.

## Scope and preservation

Exact intended allowlist:

- `scripts/presentation/IEntityVisual2D.cs`
- `scripts/presentation/PlayerFormalVisual.cs`
- `scripts/presentation/PlayerPlaceholderVisual.cs`
- `scripts/presentation/PlayerPresentationCoordinator.cs`
- `tests/integration/PlayerFormalVisualTests.cs`
- `tests/presentation/ProloguePresentation.cs`
- this report

No builder or scene serialization, README, deferred behavior, imported asset, room/player scene, audio, UID, screenshot, or art-review file was staged or changed for this round.

## Concerns

- No known automated blocker remains.
- `ClearCueOverride` is intentionally generic but is only invoked by the player coordinator on an authoritative thread-active/Failed-to-Idle edge; this keeps unrelated cues untouched and avoids forcing changes across enemy visuals.
