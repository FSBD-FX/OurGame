# Task 3 implementation report — input arbitration and atomic boost-release

## Result

Status: `DONE_WITH_CONCERNS`.

The Task 3 focused suite builds with zero warnings/errors and passes under the actual Godot 4.7.1 Mono executable. It exercises the real generated `Player.tscn` processing order and real `InputMap` sampling for the seven required cases, plus raw unavailable-shield arbitration and synchronous lifecycle-reset preemption. This is not a claim that the later presentation/world/residency work is complete.

## Implementation

- `PlayerInputReader.ConsumePrimaryPressedForCurrentPhysicsFrame()` clears only `PrimaryPressed` and the legacy `AttackPressed` alias. It leaves held/released state and `CurrentFrameSequence` unchanged.
- `PlayerActionModeController` consumes a same-frame primary edge immediately after Q selection commits, before Combat's later physics callback.
- `MechanicalClawController` resolves one command in the approved order: raw RMB/Q cancellation, Dash cancellation, attached Space boost-release, active LMB cancellation, idle eligible LMB cast, then no command. Attached Space is suppressed from the motor even when a higher-priority cancel wins; primary is consumed when it loses to shield/Q/dash/attached Space.
- Mouse-up, `PrimaryHeld`, and `Aim.Y` remain outside claw command resolution. A/D remains the model's tangential `SwingAxis`.
- Attached Space queues a synthetic `Boost` copy and the real `Released` frame, drains both publications in order, and only then applies one final release to `PlayerMotor`. `CurrentFrame` ends as Released.
- `PlayerMotor.ApplyGrappleRelease(outwardNormal, boostDelta, speedLimit, preserveLaunchMomentum)` starts from the collision-observed body velocity, adds boost exactly once, removes the component into the saved face, caps total speed, updates both body and motion state, and clears grapple correction/movement scale.
- The controller uses the saved surface normal or the approved point-anchor fallback, `AnchorPoint.DirectionTo(CasterWorldPosition)`. Normal cancellation passes zero boost. Boost direction retains the existing configured upward/tangential rule.
- Reentrant lifecycle reset/failure clears pending publications and the post-publication guard refuses stale motor application if an observer reset or exited the controller during Boost.
- The obsolete Task 2 comment now describes cancellation-snapshot semantics.

## Test coverage

`tests/integration/MechanicalClawAutoPullRuntimeTests.cs` contains scheduled real-input coverage for:

1. one-frame LMB then mouse-up remains click-owned and auto-pulls without W;
2. second fresh click cancels and third fresh click casts again;
3. Q+LMB selects Attack but starts neither attack nor replacement cast, and a later fresh click works;
4. available RMB+Space cancels, shields, never boosts, and never jumps;
5. unavailable zero-energy RMB still owns cancellation and jump suppression;
6. Dash+Space cancels, spends only the existing air-dash budget, and never replays the suppressed jump buffer;
7. attached Space publishes `Boost -> Released`, leaves Released current, preserves publication velocity until both callbacks finish, and applies the literal hand-derived final velocity once;
8. Travel Space remains an ordinary jump and grants no boost metadata/event;
9. a synchronous reset from a Boost observer ends Idle and blocks stale release application.

The focused runner also executes the new `PlayerActionModeRuntimeTests` reader-sequence boundary and `PlayerTraversalInputTests` direct observed-velocity/inward-removal/900px/s-cap boundary.

## TDD and execution evidence

Every engine invocation was gated by this fresh build command, with `DOTNET_ROOT` and `DOTNET_ROOT_X64` set to `G:/godot/dotnet-sdk`, `DOTNET_ROLL_FORWARD=LatestMajor`, `DOTNET_PROCESSOR_COUNT=2`, and the SDK prepended to `PATH`:

```powershell
G:/godot/dotnet-sdk/dotnet.exe build Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --verbosity quiet
```

Every engine run used the actual non-console executable through hidden `Start-Process`:

```powershell
G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe --headless --path E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555 --fixed-fps 60 --script res://tests/RunAutoPullClawTests.cs
```

All structured records and logs are under `E:/OurGame/Knight/.evidence/september09-auto-pull-claw-20260913T011619`:

- Behavioral RED: `task3-input-red-fixture5/execution.json`; build PID 127528 exit 0, Godot PID 109868 exit 1. Expected failure: `Q plus primary consumes the losing primary edge before combat; expected=0, actual=1`.
- The earlier `task3-input-red`, `task3-input-red-fixture2`, `task3-input-red-fixture3`, and `task3-input-red-fixture4` records are fixture-coordinate diagnostics and are not counted as behavioral RED. They exposed the required `FinalTransform * GlobalTransformWithCanvas` conversion for real replayed mouse motion.
- Interface compile RED: `task3-motor-api-compile-red/execution.json`; build PID 135352 exit 1 on the missing exact four-argument release API; no Godot process started.
- Compile-caller evidence: `task3-implementation-compile1/execution.json`; build PID 101368 exit 1 identified only `SeamlessPrologueWorldTests.cs:296`; no Godot process started. Root authorized the exact one-line zero-normal/zero-boost/current-cap migration, preserving its camera-consumer assertions.
- Lifecycle reentrancy behavioral RED: `task3-green-attempt1/execution.json`; build PID 86032 exit 0, Godot PID 88292 exit 1 on stale horizontal release rewrite after a Boost-observer reset.
- First reentrancy fix probe: `task3-green-attempt2/execution.json`; build PID 132732 exit 0, Godot PID 98108 exit 1. Momentum/state were fixed; the remaining horizontal assertion still included ordinary scheduled motor deceleration, so the fixture was narrowed by disabling Motor only after establishing attachment.
- First complete runtime GREEN: `task3-green-attempt3/execution.json`; build PID 120568 exit 0, Godot PID 130280 exit 0.
- Expanded focused GREEN including reader and direct motor boundaries: `task3-focused-green1/execution.json`; build PID 132636 exit 0, Godot PID 135484 exit 0.
- Exactly-once mutation RED: `task3-double-apply-mutation-red/execution.json`; build PID 52740 exit 0, Godot PID 118752 exit 1 on `the controller applies the hand-derived ceiling release exactly once after both publications`. The deliberate second release application was then removed with a scoped patch.
- Fresh final GREEN: `task3-final-green/execution.json`; build PID 123432 exit 0 with 0 warnings/errors, Godot PID 91464 exit 0. Output contains `MECHANICAL_CLAW_AUTO_PULL_RUNTIME_OK` and `AUTO_PULL_CLAW_TESTS_OK`; stderr is empty.

## Files changed

- `scripts/player/PlayerInputReader.cs`
- `scripts/player/PlayerActionModeController.cs`
- `scripts/player/PlayerMotor.cs`
- `scripts/abilities/MechanicalClawController.cs`
- `tests/integration/MechanicalClawAutoPullRuntimeTests.cs` (new)
- `tests/integration/PlayerActionModeRuntimeTests.cs`
- `tests/integration/PlayerTraversalInputTests.cs`
- `tests/RunAutoPullClawTests.cs`
- `tests/integration/SeamlessPrologueWorldTests.cs` (authorized one-line compile-only API migration)
- `.superpowers/sdd/2026-09-09-auto-pull-claw/task-3-report.md`

## Self-review and concerns

- Checked the exact public method names/signatures, fixed 900px/s cap source, point-normal fallback, event order, and final Released state against the approved brief.
- The atomic literal test catches a duplicated motor application; the direct motor test independently catches wrong base velocity, missing inward removal, missing cap, and missing constraint clear.
- Raw shield input is tested both when guard can activate and when zero energy keeps it unavailable.
- Q consumption is proven to preserve the input sequence and expire on the next sampled primary edge.
- The air-dash conflict proves the suppressed Space edge is not buffered into the next frame after the air-dash budget is exhausted.
- No gameplay `GlobalPosition` write, map/save/world change, package, push, paid generation, automation, or new asset was introduced.
- `git diff --check` reports no whitespace errors; it reports only the repository's existing LF-to-CRLF notices.
- Pre-existing addon import changes, bus layout, root plan edits, `RunPrologueTests.cs`, artifacts, UIDs, and unrelated untracked files were preserved and excluded from the scoped commit.
- Existing aggregate rendered-Boost/held-release runners remain explicitly pending Task 7 migration. They were not deleted, skipped, weakened, or claimed green. Task 5 still owns target residency/publication target ordering; Task 7 still owns back-to-back presentation/audio/VFX certification.

## Fix round 1/5: target loss outranks sampled primary

Review found that an active LMB cancellation path fabricated a valid locked-target snapshot when the resolver reported the target missing. That allowed `Cancel` to beat resolver-detected target loss. A lifecycle `NotifyTargetWillUnload` arriving after Reader sampling could similarly reset to Idle and let the same sampled LMB start a replacement cast.

### Behavioral RED

Both races were captured independently through real scheduled nodes. The test node runs at physics priority `-150`, after InputReader (`-300`) and ActionModes (`-200`) but before Claw (`-100`), and records the exact sampled input sequence it acts on.

- Resolver unavailable plus LMB: `E:/OurGame/Knight/.evidence/september09-auto-pull-claw-task3-fix1-20260913T015715/target-loss-input-red3/execution.json`; build PID 88588 exit 0, actual hidden Godot PID 124548 exit 1. The failure showed cancellation instead of typed `Failed/TargetLost`.
- Post-sampling `NotifyTargetWillUnload` plus LMB: `E:/OurGame/Knight/.evidence/september09-auto-pull-claw-task3-fix1-20260913T015715/post-sample-unload-input-red/execution.json`; build PID 73644 exit 0, actual hidden Godot PID 108508 exit 1. The failure showed final Aim instead of Idle because the losing sampled edge recast.
- `target-loss-input-red` was an interface diagnostic (outer Godot class required `partial`; build PID 128624 exit 1, no engine). `target-loss-input-red2` was a fixture-order diagnostic (build PID 117064 exit 0, Godot PID 122516 exit 1) and is not counted as behavioral RED.

### Fix and focused coverage

- Removed the synthetic-valid cancellation snapshot. The model's existing locked-target validation now receives the resolver's missing snapshot and publishes typed `TargetLost` before sampled commands.
- When Travel or an attached state loses its live target, the controller clears the sampled frame before later Motor/Combat callbacks. Lifecycle reset/failure does the same, so a post-sampling unload wins and the losing command cannot replay.
- Released/Retract cleanup is deliberately excluded from live-target consumption, preserving ordinary controls after legitimate release.
- Added focused cases for resolver-unavailable plus LMB and post-sampling lifecycle unload plus LMB. Each proves the scheduled sequence, typed target-loss publication, cleared primary edge, no attack/recast replay while held, Idle cleanup, and later fresh-click Grapple recovery.
- The accepted atomic Boost/Released and all earlier focused Task 3 cases remain in the same runner.

### GREEN and exact execution

The environment and exact build/Godot commands are unchanged from the Task 3 evidence section above. A fresh build preceded every engine invocation; no engine was launched after a failed build.

- `target-loss-green1`: build PID 83748 exit 0, Godot PID 133320 exit 1; test-only cast-id assumption was corrected because lifecycle reset intentionally restarts its model cast counter.
- `target-loss-green2`: build PID 103216 exit 0, Godot PID 112488 exit 0.
- `target-loss-final-green`: build PID 107760 exit 0, Godot PID 115380 exit 1; the test's exact `+1` sequence assumption was narrowed because `Dispatch` can cross an additional scheduled sample.
- `target-loss-final-green2`: build PID 116392 exit 0, Godot PID 66812 exit 0.
- Fresh final GREEN after strengthening the lifecycle case's typed `TargetLost` assertion: `E:/OurGame/Knight/.evidence/september09-auto-pull-claw-task3-fix1-20260913T015715/target-loss-final-green3/execution.json`; build PID 125796 exit 0 with 0 warnings/errors, actual hidden Godot PID 85796 exit 0. Output contains `MECHANICAL_CLAW_AUTO_PULL_RUNTIME_OK` and `AUTO_PULL_CLAW_TESTS_OK`; stderr is empty.

### Fix self-review

- Target loss and lifecycle reset now consume the whole sampled frame, matching their priority over all commands; `CurrentFrameSequence` remains unchanged.
- The model's locked-target validation and typed failure were not weakened. A later fresh click still recovers.
- No motor/release behavior, gameplay `GlobalPosition`, world/map/save, runner registration, or compatibility API changed in this fix round.
- Root-owned README/plan/ledger edits and all pre-existing unrelated imports, bus layout, `RunPrologueTests.cs`, artifacts, UIDs, and untracked files remain outside this commit.
