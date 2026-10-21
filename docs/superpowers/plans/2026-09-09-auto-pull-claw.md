# Click-to-Auto-Pull Mechanical Claw Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace hold-LMB/manual-W grappling with a one-click, collision-safe automatic pull that attaches to real stone/metal wall, ceiling, and edge surfaces, then exits through deterministic cancel, dash, shield, or one-shot Space boost-release behavior.

**Architecture:** Keep `MechanicalClawModel` deterministic and scene-free: it owns cast lifecycle, automatic rope take-up, bounded radial/tangential velocity correction, and release metadata. `MechanicalClawController` owns same-frame command arbitration, real `CharacterBody2D` capsule collision/arrival probes, stall detection, the atomic Boost→Released publication, and the target-residency signal. `PlayerMotor` remains the only movement writer and composes the final collision-observed release velocity exactly once. The continuous-world project consumes the residency interface but owns all sector pinning and stream-graph implementation.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, `CharacterBody2D.MoveAndSlide`/`TestMove`, deterministic record structs, repository `SceneTree` test runners.

**Spec:** `docs/superpowers/specs/2026-09-09-continuous-canyon-auto-pull-design.md`

## Global Constraints

- Preserve the approved 620px maximum range and 36px point-target assist radius.
- Initial auto-pull tuning is 540px/s rope take-up, 720px/s maximum targetward speed, 2400px/s² targetward acceleration, 1800px/s shuttle speed, 160px arrival deceleration distance, 0.25s no-progress timeout, and 900px/s final boost-release speed cap.
- LMB release never cancels a cast. Active LMB press cancels it. W/S never controls rope length.
- Same-frame command order is lifecycle/target loss > RMB or Q > Dash > attached Space > LMB > A/D. Losing edges are consumed in that physics frame and are never replayed.
- RMB+Space only cancels and shields; Dash+Space only performs an eligible dash; Q+LMB only selects attack mode and consumes that LMB.
- Space without a successful attachment remains the existing ordinary jump. Attached Space publishes one Boost event and Released in one controller transaction, applies one final release velocity, and never starts an ordinary jump.
- Stone, temple metal, and dedicated anchors remain valid. Ice and forbidden surfaces retain typed rejection. Legacy enemy/mechanism targets retain hit→apply→release and never pull the player.
- Never assign `GlobalPosition` to simulate pulling. `PlayerMotor` remains the only gameplay movement writer.
- Pause freezes pull and stall time. Cancel, death, recovery, target unload, new game, continue, and exit-tree clear the claw correction and residency state exactly once.
- Do not modify `SeamlessPrologueWorld`, `PrologueGameFlow`, `PrologueCompletion`, save files, maps, or sector definitions in this plan. The separate continuous-world plan owns those files. One evidence-backed Task7 exception is authorized: camera-only Basin/Ascent seam visibility correction in `SeamlessPrologueWorld`, without changing streaming, sector authority, recovery, saves, geometry, or claw physics; see Task7's prerequisite below.
- Run one Godot/compiler process at a time. Store new execution evidence under a unique directory outside the repository worktree; never overwrite prior evidence.
- At the start of every execution task, create its evidence directory with `$evidenceRoot = Join-Path 'E:\OurGame\Knight\.evidence' ('september09-auto-pull-claw-' + (Get-Date -Format 'yyyyMMddTHHmmss')); New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null`, then use that variable for every log in that task.
- Immediately before every Godot test command, run `G:\godot\dotnet-sdk\dotnet.exe build --no-restore` with `DOTNET_ROOT`, `DOTNET_ROOT_X64`, `DOTNET_ROLL_FORWARD=LatestMajor`, and the SDK-prefixed `PATH`; if that build exits non-zero, do not start Godot. This prevents a runner from testing a stale assembly.

## File Structure

**Create**

- `scripts/abilities/MechanicalClawCommand.cs` — authoritative model command and release-kind enums.
- `scripts/abilities/MechanicalClawArrivalGeometry.cs` — pure capsule stand-off, dock point, arrival envelope, and normal-match calculations.
- `tests/unit/MechanicalClawArrivalGeometryTests.cs` — geometry and release-vector unit contracts.
- `tests/integration/MechanicalClawAutoPullRuntimeTests.cs` — real input, body collision, arbitration, auto-pull, and atomic release tests.
- `tests/integration/MechanicalClawResidencyContractTests.cs` — target pin-source lifecycle contract without changing the world streamer.
- `tests/RunAutoPullClawTests.cs` — focused runner for this sub-project.

**Modify**

- `scripts/abilities/MechanicalClawSettings.cs` — centralized approved tuning.
- `scripts/abilities/MechanicalClawTargetSnapshot.cs` — immutable locked surface normal.
- `scripts/abilities/MechanicalClawFrame.cs` — locked normal, arrival flag, and release kind.
- `scripts/abilities/MechanicalClawInput.cs` — one authoritative command instead of held/released/reel inputs.
- `scripts/abilities/MechanicalClawTargetResolver.cs` — capture ray normals and preserve the locked normal.
- `scripts/abilities/MechanicalClawModel.cs` — click persistence, auto reel, bounded/decelerated correction, arrival, and releases.
- `scripts/abilities/MechanicalClawController.cs` — arbitration, collision probes, stall handling, Boost→Released transaction, and residency interface.
- `scripts/player/PlayerInputReader.cs` — same-frame primary-edge consumption.
- `scripts/player/PlayerActionModeController.cs` — Q+LMB consumption before combat.
- `scripts/player/PlayerMotor.cs` — exactly-once release velocity composition.
- `scripts/audio/PrologueAudioDirector.cs`, `scripts/vfx/PrologueVfxDirector.cs`, `scripts/presentation/PlayerPresentationCoordinator.cs` — transaction-safe boost/release feedback.
- `assets/localization/prologue-ui.json`, `scripts/ui/PrologueControlsView.cs`, `scripts/prologue/ProloguePauseMenu.cs`, `scripts/prologue/sample/BasinSampleGuideController.cs` — approved click/auto-pull controls and contextual guidance.
- Focused unit/integration/presentation tests named in Tasks 6–7 — replace obsolete held-LMB/W/manual-boost expectations with the approved behavior while retaining unrelated coverage.

---

### Task 1: Lock Target Surface Geometry and Centralize Tuning

**Files:**
- Create: `scripts/abilities/MechanicalClawArrivalGeometry.cs`
- Create: `tests/unit/MechanicalClawArrivalGeometryTests.cs`
- Create: `tests/RunAutoPullClawTests.cs`
- Modify: `scripts/abilities/MechanicalClawSettings.cs`
- Modify: `scripts/abilities/MechanicalClawTargetSnapshot.cs`
- Modify: `scripts/abilities/MechanicalClawFrame.cs`
- Modify: `scripts/abilities/MechanicalClawTargetResolver.cs`
- Modify: `scripts/abilities/MechanicalClawModel.cs`
- Modify: `scripts/abilities/MechanicalClawController.cs`
- Modify: `tests/unit/MechanicalClawModelTests.cs`
- Modify: `tests/integration/MechanicalClawRuntimeTests.cs`

**Interfaces:**
- Produces: `MechanicalClawTargetSnapshot.SurfaceNormal : Vector2`.
- Produces: `MechanicalClawFrame.SurfaceNormal : Vector2`, `MechanicalClawFrame.DockPoint : Vector2`, and `MechanicalClawFrame.Arrived : bool`.
- Produces: `MechanicalClawArrivalGeometry.DockPoint(Vector2 anchor, Vector2 outwardNormal, CapsuleShape2D capsule, float minimumRopeLength, float margin) : Vector2`.
- Produces: `MechanicalClawArrivalGeometry.IsArrived(Vector2 caster, Vector2 dockPoint, float tolerance) : bool`.
- Produces: `MechanicalClawArrivalGeometry.NormalMatches(Vector2 expected, Vector2 actual, float minimumDot) : bool`.
- Produces: the focused test command used by all later tasks.

- [ ] **Step 1: Add geometry RED tests and the focused runner**

Add tests with these exact cases:

```csharp
public static void Run()
{
    WallDockUsesCapsuleRadiusAndMinimumRope();
    CeilingDockUsesCapsuleHalfHeightAndMinimumRope();
    ArrivalRequiresTheSavedSideOfTheSurface();
    CollisionNormalMustMatchTheSavedFace();
}

private static void WallDockUsesCapsuleRadiusAndMinimumRope()
{
    var capsule = new CapsuleShape2D { Radius = 20f, Height = 72f };
    Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(
        new Vector2(100f, 80f), Vector2.Left, capsule, 48f, 4f);
    TestAssert.Equal(new Vector2(52f, 80f), dock,
        "wall docking remains one 48px safe rope length outside the hit pixel");
}

private static void CeilingDockUsesCapsuleHalfHeightAndMinimumRope()
{
    var capsule = new CapsuleShape2D { Radius = 20f, Height = 72f };
    Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(
        new Vector2(100f, 80f), Vector2.Down, capsule, 48f, 4f);
    TestAssert.Equal(new Vector2(100f, 128f), dock,
        "ceiling docking keeps the player centre below the physical hit face");
}
```

`RunAutoPullClawTests` must call `MechanicalClawArrivalGeometryTests.Run()`, `MechanicalClawModelTests.Run()`, `MechanicalClawAutoPullRuntimeTests.Run(this)`, and `MechanicalClawResidencyContractTests.Run(this)` inside the repository's standard exception/`Quit(1)` wrapper. Until later test classes exist, add only the first two calls; later tasks add their call at the same time they create the class.

- [ ] **Step 2: Preserve interface evidence, then obtain a real behavior RED**

```powershell
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -eq 0) { throw 'expected missing geometry interface before Task 1 implementation' }
```

Treat that compiler failure only as evidence that the new interface is absent. Next add the declared fields/signatures and temporary compile-safe geometry bodies `DockPoint => anchor`, `IsArrived => false`, and `NormalMatches => false`; update every constructor plus the model/controller call sites so the build is green. Then run:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed before geometry behavior RED' }
& $godot --headless --path . --fixed-fps 60 --script res://tests/RunAutoPullClawTests.cs --log-file (Join-Path $evidenceRoot 'task1-geometry-red.log')
```

Expected: build exits 0 and Godot exits 1 with wall dock expected `(52,80)` but actual `(100,80)`. This is the required behavior RED. A compile failure caused by unrelated P0 edits is not acceptable; wait for the P0 owner to restore the shared build first.

- [ ] **Step 3: Add exact geometry fields and tuning**

Add these settings, validate each as finite and positive, and keep `MaxRange == 620f` and `MainAnchorAssistRadius == 36f` guards:

```csharp
AutoReelSpeed: 540f,
MaxPullSpeed: 720f,
MaxPullAcceleration: 2400f,
ArrivalDecelerationDistance: 160f,
ArrivalTolerance: 8f,
ArrivalMargin: 4f,
StallProgressEpsilon: 1f,
StallTimeoutSeconds: .25f,
ReleaseSpeedLimit: 900f,
ShuttleSpeed: 1800f
```

Append `SurfaceNormal` to `MechanicalClawTargetSnapshot`; append `SurfaceNormal`, `DockPoint`, and `Arrived` to `MechanicalClawFrame`, with idle values `Vector2.Zero`, `Vector2.Zero`, and `false`. Do not serialize these transient fields.

Implement geometry without scene queries:

```csharp
public static Vector2 DockPoint(Vector2 anchor, Vector2 outwardNormal,
    CapsuleShape2D capsule, float minimumRopeLength, float margin)
{
    Vector2 normal = outwardNormal.Normalized();
    float segmentHalf = Mathf.Max(0f, (capsule.Height - capsule.Radius * 2f) * .5f);
    float support = capsule.Radius + segmentHalf * Mathf.Abs(normal.Y) + margin;
    return anchor + normal * Mathf.Max(minimumRopeLength, support);
}

public static bool IsArrived(Vector2 caster, Vector2 dockPoint, float tolerance) =>
    caster.DistanceTo(dockPoint) <= tolerance;

public static bool NormalMatches(Vector2 expected, Vector2 actual, float minimumDot) =>
    !expected.IsZeroApprox() && !actual.IsZeroApprox() &&
    expected.Normalized().Dot(actual.Normalized()) >= minimumDot;
```

- [ ] **Step 4: Capture and retain real physics hit normals**

In `ResolveTerrainRay`, require the ray result to contain a finite non-zero `normal`, normalize it, and put it into the snapshot. For point/assist targets without a collision hit, use the normalized direction from anchor to caster at admission. Change the locked signature to:

```csharp
public MechanicalClawTargetSnapshot SnapshotLocked(
    Node2D caster, GrappleSurface2D surface, Vector2 lockedAnchor, Vector2 lockedNormal)
```

`SnapshotLocked` must preserve both `lockedAnchor` and `lockedNormal` for `UseCollisionHitPoint`; it must not chase a new face when the cursor moves. Update all snapshot constructors and test helpers explicitly rather than relying on argument position guesses. Update `MechanicalClawController.ResolveTarget` to call `SnapshotLocked(_player, CurrentTarget, frame.AnchorPoint, frame.SurfaceNormal)`, and update every model frame construction/`with` transition so a valid cast copies the snapshot normal and subsequent Travel/Reel/Swing frames preserve it. The controller computes the capsule-aware dock point once when the target locks and stores it in the frame.

- [ ] **Step 5: Run Task 1 GREEN**

Run the focused command from Step 2 with `task1-geometry-green.log`. Expected: exit 0, `AUTO_PULL_CLAW_TESTS_OK`, wall and ceiling dock assertions pass, existing ice/range/target-identity tests remain green.

- [ ] **Step 6: Commit this independently reviewable unit when the main owner authorizes commits**

```powershell
git add scripts/abilities/MechanicalClawArrivalGeometry.cs scripts/abilities/MechanicalClawSettings.cs scripts/abilities/MechanicalClawTargetSnapshot.cs scripts/abilities/MechanicalClawFrame.cs scripts/abilities/MechanicalClawTargetResolver.cs scripts/abilities/MechanicalClawModel.cs scripts/abilities/MechanicalClawController.cs tests/unit/MechanicalClawArrivalGeometryTests.cs tests/unit/MechanicalClawModelTests.cs tests/integration/MechanicalClawRuntimeTests.cs tests/RunAutoPullClawTests.cs
git commit -m "feat: lock grapple surface geometry"
```

### Task 2: Make Click Persistent and Auto-Pull Deterministic

**Files:**
- Create: `scripts/abilities/MechanicalClawCommand.cs`
- Modify: `scripts/abilities/MechanicalClawInput.cs`
- Modify: `scripts/abilities/MechanicalClawFrame.cs`
- Modify: `scripts/abilities/MechanicalClawController.cs`
- Modify: `tests/integration/MechanicalClawRuntimeTests.cs`
- Modify: `tests/presentation/ClawConnectionReadabilityProbe.cs`
- Modify: `scripts/abilities/MechanicalClawModel.cs`
- Modify: `tests/unit/MechanicalClawModelTests.cs`

**Interfaces:**
- Consumes: locked `SurfaceNormal` and approved settings from Task 1.
- Produces: `MechanicalClawCommand { None, Cast, Cancel, BoostRelease }`.
- Produces: `MechanicalClawReleaseKind { None, Cancel, Boost, Obstructed }`.
- Produces: `MechanicalClawInput(MechanicalClawCommand Command, float SwingAxis, bool Arrived, Vector2 DockPoint)`.
- Produces: a `Released` frame whose `ReleaseKind` tells the controller whether to publish the atomic Boost event.

- [ ] **Step 1: First write compile-valid behavior RED tests against the old input**

Before introducing `MechanicalClawCommand`, use the current `MechanicalClawInput` helper to add and run these two assertions:

```csharp
MechanicalClawFrame travel = EnterTravel(model, target);
MechanicalClawFrame afterMouseUp = model.Step(travel,
    Input(held: false, released: true), target, Frame);
TestAssert.Equal(MechanicalClawState.Travel, afterMouseUp.State,
    "mouse-up does not cancel a click-owned cast");

MechanicalClawFrame attached = EnterLatched(model, target);
float before = attached.RopeLength;
MechanicalClawFrame automatic = model.Step(attached, Input(), target, Frame);
TestAssert.True(automatic.RopeLength < before,
    "an attached claw takes up rope without W or held LMB");
```

Run a real build, then the focused runner. Expected: build exits 0 and Godot exits 1 on the first assertion because current mouse-up enters Released. This is the required behavior RED; a missing-type compiler error is not its substitute.

After preserving that RED, keep range/material/identity tests, replace the helper with the new command input, and add these exact contracts:

```csharp
ClickReleaseDuringTravelDoesNotCancel();
NoInputAfterLatchAutomaticallyShortensTheRope();
MoveUpAndMoveDownDoNotChangeAutomaticTakeUp();
AutoPullDeceleratesInsideOneHundredSixtyPixels();
AutoPullSpeedAndAccelerationRemainBoundedAcrossDeltaSizes();
ArrivedTransitionsFromReelToSwingBeforeAnyStallDecision();
ActiveCastCommandCancelsInsteadOfFailingAlreadyDeployed();
BoostReleaseProducesOneReleasedFrameWithBoostMetadata();
```

The first test must cast, then send `MechanicalClawCommand.None` for all later frames; do not represent mouse-up as a cancel command. Move `MoveUpAndMoveDownDoNotChangeAutomaticTakeUp` into `MechanicalClawRuntimeTests`: use actual input-reader W/S actions in otherwise identical attached fixtures, and compare rope take-up with the no-W/S case. The test must catch an accidental controller mapping from W/S to rope control; passing identical scene-free model inputs twice would not test that boundary. Keep the model input free of W/S fields.

- [ ] **Step 2: Run the expanded Task 2 RED after adding compile-safe enum/input declarations**

Add `MechanicalClawCommand.cs` and the new input record signatures with no model behavior change, update constructor call sites until the build is green, then run `RunAutoPullClawTests.cs` with `task2-model-red.log`. Expected: Godot exits 1 because an idle attached frame still does not shorten its rope and active Cast still follows the obsolete AlreadyDeployed path. Missing enum/type errors are interface migration evidence only and must be resolved before accepting this behavior RED.

- [ ] **Step 3: Replace the model input contract**

Use the exact record:

```csharp
public readonly record struct MechanicalClawInput(
    MechanicalClawCommand Command,
    float SwingAxis,
    bool Arrived,
    Vector2 DockPoint);
```

Add `ReleaseKind : MechanicalClawReleaseKind` to `MechanicalClawFrame` in this task and initialize/reset it to `None` in `Idle`, new casts, and ordinary attached frames. This keeps Task 1 independently compilable before `MechanicalClawCommand.cs` exists.

In the same task, migrate every compile-time caller of `MechanicalClawInput`: `MechanicalClawController`, `MechanicalClawRuntimeTests`, `MechanicalClawModelTests`, and `ClawConnectionReadabilityProbe`. The controller's temporary bridge maps routed idle LMB to `Cast`, `_retractRequested` to `Cancel`, attached Space to `BoostRelease`, passes `sampled.MoveAxis`, the Task 1 arrival result, and `previous.DockPoint`. Remove the old mouse-up/`!PrimaryHeld` retract assignment here. Task 3 replaces this bridge with the complete conflict-consumption priority; Task 2 must already build and run independently.

`Idle` accepts only `Cast`. `Travel` ignores `None`, and `Cancel` enters Released. Attached `Cast` and `Cancel` both mean user cancellation; they must not produce `AlreadyDeployed`. `BoostRelease` is accepted only from Latched/Reel/Swing and returns Released with `ReleaseKind=Boost` and `BoostConsumed=true`. It never grants boost during Aim/Windup/Travel.

- [ ] **Step 4: Implement automatic rope take-up and bounded correction**

For attached, not-arrived frames:

```csharp
Vector2 dockPoint = input.DockPoint;
float stopDistance = previous.AnchorPoint.DistanceTo(dockPoint);
float ropeLength = Mathf.MoveToward(previous.RopeLength, stopDistance,
    _settings.AutoReelSpeed * seconds);
float remaining = Mathf.Max(0f,
    previous.CasterWorldPosition.DistanceTo(dockPoint));
float distanceCap = _settings.MaxPullSpeed *
    Mathf.Clamp(remaining / _settings.ArrivalDecelerationDistance, 0f, 1f);
float brakingCap = Mathf.Sqrt(2f * _settings.MaxPullAcceleration * remaining);
float targetwardSpeed = Mathf.Min(distanceCap,
    Mathf.Min(brakingCap, _settings.MaxPullSpeed));
```

Use `CasterWorldPosition.DirectionTo(dockPoint)`, not direction to the raw hit pixel, as the auto-pull direction. Move the prior actual dockward speed toward `targetwardSpeed` by at most `MaxPullAcceleration * seconds`. Estimate the unconstrained base as `CasterVelocity - previous.VelocityCorrection`, and return the correction needed to make the composed dockward speed equal the bounded targetward speed. Allow a negative dockward correction near the stop point so braking works. Preserve the existing exponentially damped anchor-tangential component and add `SwingAxis * SwingAcceleration * seconds` after the radial calculation. This makes an angled wall cast converge on `anchor + savedNormal * standOff`; merely reaching another point on the 48px anchor circle is not arrival.

When `input.Arrived` is true, set `Arrived=true`, state `Swing`, stop shortening the rope, and use the existing stretch-only rope constraint to retain controlled tension. Otherwise state remains `Reel`, including the interval after rope length reaches 48px but before the body reaches the dock envelope.

- [ ] **Step 5: Run Task 2 GREEN at 30/60/120Hz model deltas**

Run the focused suite with `task2-model-green.log`. Expected: exit 0; all three delta sequences stay within 720px/s targetward speed and 2400px/s² acceleration tolerance, reach the same 48px rope bound, and never mutate caster position.

- [ ] **Step 6: Commit when authorized**

```powershell
git add scripts/abilities/MechanicalClawCommand.cs scripts/abilities/MechanicalClawInput.cs scripts/abilities/MechanicalClawFrame.cs scripts/abilities/MechanicalClawModel.cs scripts/abilities/MechanicalClawController.cs tests/unit/MechanicalClawModelTests.cs tests/integration/MechanicalClawRuntimeTests.cs tests/presentation/ClawConnectionReadabilityProbe.cs
git commit -m "feat: add deterministic claw auto pull"
```

### Task 3: Consume Conflicting Inputs and Apply Boost-Release Once

**Files:**
- Create: `tests/integration/MechanicalClawAutoPullRuntimeTests.cs`
- Modify: `tests/RunAutoPullClawTests.cs`
- Modify: `scripts/player/PlayerInputReader.cs`
- Modify: `scripts/player/PlayerActionModeController.cs`
- Modify: `scripts/abilities/MechanicalClawController.cs`
- Modify: `scripts/player/PlayerMotor.cs`
- Modify: `tests/integration/PlayerActionModeRuntimeTests.cs`
- Modify: `tests/integration/PlayerTraversalInputTests.cs`
- Modify one compile-time caller only: `tests/integration/SeamlessPrologueWorldTests.cs` — migrate the zero-rise camera-consumer `ApplyGrappleRelease` call to zero normal, zero boost, the configured release-speed limit and `preserveLaunchMomentum: true`; preserve all assertions.

**Interfaces:**
- Consumes: `MechanicalClawCommand` and released-frame metadata from Task 2.
- Produces: `PlayerInputReader.ConsumePrimaryPressedForCurrentPhysicsFrame() : void`.
- Produces: `PlayerMotor.ApplyGrappleRelease(Vector2 outwardNormal, Vector2 boostDelta, float speedLimit, bool preserveLaunchMomentum) : void`.
- Produces: ordered `ClawFrameChanged` publications `Boost` then `Released` in one physics callback, with only Released applied to the motor.

- [ ] **Step 1: Write real scheduled-input RED cases**

Add the new runtime class to the focused runner and implement these tests using the real `Player.tscn` node priorities:

```csharp
ClickForOneFrameThenMouseUpStillLatchesAndPullsWithoutW();
SecondClickCancelsAndAThirdClickCanStartANewCast();
QAndPrimaryConsumesPrimaryWithoutAttackOrCast();
ShieldAndSpaceCancelsWithoutBoostOrJump();
DashAndSpaceCancelsThenUsesOnlyTheExistingDash();
AttachedSpacePublishesBoostThenReleasedAndMovesTheMotorOnce();
TravelSpaceRemainsAnOrdinaryJumpAndGrantsNoBoostRelease();
```

For the atomic test, subscribe to `ClawFrameChanged`, record states and `player.Velocity`, and assert:

```csharp
TestAssert.True(terminalStates.Count == 2 &&
    terminalStates[0] == MechanicalClawState.Boost &&
    terminalStates[1] == MechanicalClawState.Released,
    "one attached Space publishes one ordered transaction");
TestAssert.Equal(jumpsBefore, motor.AcceptedJumpCount,
    "attached Space is never buffered into an ordinary jump");
TestAssert.True(player.Velocity.Length() <= 900.01f,
    "the one applied final release velocity is capped after composition");
```

- [ ] **Step 2: Run Task 3 RED**

Using the required fresh-build preamble, run the focused suite with `task3-input-red.log`. Expected: build exits 0 and Godot exits 1 because Q+LMB still reaches `PlayerCombat`, RMB/Dash conflict edges are not consumed, or the Task 2 bridge ends on one Released frame without the required ordered Boost→Released publications and exactly-once motor application. Mouse-up persistence already passed in Task 2 and is retained as a regression here.

- [ ] **Step 3: Add same-frame primary consumption**

Implement:

```csharp
public void ConsumePrimaryPressedForCurrentPhysicsFrame()
{
    CurrentFrame = CurrentFrame with { PrimaryPressed = false, AttackPressed = false };
}
```

Do not decrement `CurrentFrameSequence` and do not synthesize a release. In `PlayerActionModeController`, after applying a Q selection, consume the same-frame primary edge before `PlayerCombat` runs. Test that the next physical click works; consumption must last only for the current sampled frame.

- [ ] **Step 4: Encode the controller priority exactly once**

Resolve one command per physics tick in this order:

```csharp
MechanicalClawCommand command =
    (sampled.ShieldHeld || sampled.AttackModePressed) && active ? MechanicalClawCommand.Cancel :
    sampled.DashPressed && active ? MechanicalClawCommand.Cancel :
    sampled.JumpPressed && attached ? MechanicalClawCommand.BoostRelease :
    sampled.PrimaryPressed && active ? MechanicalClawCommand.Cancel :
    sampled.PrimaryPressed && _actionModes.CanBeginGrapple ? MechanicalClawCommand.Cast :
    MechanicalClawCommand.None;
```

Lifecycle and target-loss callbacks do not enter this sampled-input expression: they synchronously call the existing reset/`NotifyTargetWillUnload` path before the next model step, clear the motor correction, and therefore preempt every sampled command. When shield/Q/dash wins, call `ConsumePrimaryPressedForCurrentPhysicsFrame` if the same frame also contains LMB. For any attached frame containing Space when Space is not the winning command, call `PlayerMotor.SuppressJumpForCurrentPhysicsFrame()` so the motion model cannot retain a jump buffer. Ignore `PrimaryHeld`, `PrimaryReleased`, and `Aim.Y`; continue using `MoveAxis` for A/D tangential control.

Resolver-detected loss of an allocated locked target has the same precedence: never replace an invalid/unavailable locked snapshot with a synthetic `Exists: true` snapshot merely to record cancellation. Preserve the typed TargetLost outcome, consume the losing sampled edge, and clear the constraint. A lifecycle reset or `NotifyTargetWillUnload` invoked after this frame was sampled must not allow that same sampled LMB to begin a replacement cast from Idle. Add real scheduled-input cases for unavailable target + LMB and post-sampling forced unload/reset + LMB, asserting no replacement cast/attack, no replay, and that a later fresh valid click still works. Keep these protections in the input/controller boundary, without weakening the model's already-correct locked-target validation.

- [ ] **Step 5: Compose final release velocity in PlayerMotor**

Replace the currently unused tangential parameter with the exact release API above. Start from `_body.Velocity`, add `boostDelta` once, remove motion into the saved face, then cap:

```csharp
Vector2 normal = outwardNormal.Normalized();
Vector2 released = _body.Velocity + boostDelta;
float inward = released.Dot(normal);
if (inward < 0f) released -= normal * inward;
if (released.Length() > speedLimit) released = released.Normalized() * speedLimit;
_state.Velocity = released;
_body.Velocity = released;
_state.PreservingGrappleReleaseMomentum = preserveLaunchMomentum;
_grappleVelocityCorrection = Vector2.Zero;
_grappleMovementScale = 1f;
```

For a point anchor with no physical normal, the controller supplies `AnchorPoint.DirectionTo(CasterWorldPosition)` as the outward fallback. Normal cancellation supplies `Vector2.Zero` boost. Boost release uses the existing configured upward/tangential boost components, then the motor removes the component pointing into the saved face and caps the final vector at 900px/s.

- [ ] **Step 6: Publish Boost→Released without applying Boost to the motor**

When the model returns Released with `ReleaseKind=Boost`, enqueue a synthetic copy with `State=Boost`, followed by the real Released frame, drain both publications, then call `ApplyGrappleRelease` once for Released. `CurrentFrame` must be Released when `_PhysicsProcess` returns. Do not call `ApplyGrappleConstraint` for the synthetic Boost publication.

- [ ] **Step 7: Run Task 3 GREEN**

Run the focused suite with `task3-input-green.log`. Expected: exit 0; event order is Boost→Released, accepted jump count is unchanged, dash count changes only according to the existing air-dash budget, Q+LMB starts neither attack nor cast, and the following fresh LMB works.

- [ ] **Step 8: Commit when authorized**

```powershell
git add scripts/player/PlayerInputReader.cs scripts/player/PlayerActionModeController.cs scripts/player/PlayerMotor.cs scripts/abilities/MechanicalClawController.cs tests/integration/MechanicalClawAutoPullRuntimeTests.cs tests/integration/PlayerActionModeRuntimeTests.cs tests/integration/PlayerTraversalInputTests.cs tests/integration/SeamlessPrologueWorldTests.cs tests/RunAutoPullClawTests.cs
git commit -m "feat: arbitrate claw input and boost release"
```

### Task 4: Stop Safely on Real Walls, Ceilings, Edges, and Obstructions

**Status (2026-09-13):** Complete at `979727f` after independent review and two test-hardening rounds. Root fresh build/actual-engine gate passed; see the plan ledger and README. This is isolated physics acceptance, not normal-entry, authored-world or player-feel certification.

**Files:**
- Modify: `scripts/abilities/MechanicalClawTargetResolver.cs`
- Modify: `scripts/abilities/MechanicalClawController.cs`
- Modify: `tests/integration/MechanicalClawAutoPullRuntimeTests.cs`
- Modify: `tests/integration/MechanicalClawRuntimeTests.cs`
- Modify: `tests/RunAutoPullClawTests.cs`
- Modify one renamed-method caller only: `tests/RunGrappleReleaseTests.cs`

**Interfaces:**
- Consumes: locked face normal, dock-point helper, bounded model correction, and release API.
- Produces: collision-safe `Arrived` input to the model and `Released/Obstructed` on early collision or a 0.25s stall.
- Produces: `MechanicalClawTargetResolver.ColliderBelongsToSurface(Node collider, GrappleSurface2D surface) : bool` for collision classification.

- [ ] **Step 1: Add RED fixtures for actual body geometry**

Add these fixtures to the already-registered `MechanicalClawAutoPullRuntimeTests.Run`, using the real radius-20/height-72 player capsule and real `StaticBody2D` collision shapes. Current-map `PrologueCanyonTraversalTests` route migration remains Task 7; this task's long fall is an isolated actual-physics fixture, not a claim about an authored route:

```csharp
AutoPullStopsAtWallDockWithoutOverlapOrJitter();
AutoPullStopsBelowCeilingWithoutOverlapOrJitter();
AutoPullStopsOnTheSavedFaceOfAConvexEdge();
FourSecondFallCanClickAVisibleWallAndBrakeContinuously();
ThinWorldBlockerBetweenCasterAndTargetCannotBeCrossed();
CapsuleFlankBlockerWithClearLineOfSightReleasesBeforeContact();
InsertedBlockerDuringPullReleasesAsObstructed();
EarlyCollisionWithAnotherFaceOfTheTargetIsObstructed();
ReachedDockWinsBeforeTheNoProgressTimeout();
NoProgressForQuarterSecondReleasesOnceAndClearsCorrection();
SubpixelDockProgressAccumulatesBeforeStallTimeout();
ExplicitReleaseIsNotReclassifiedByPullSweep();
PauseDoesNotAdvanceTheNoProgressClock();
```

For wall/ceiling/edge and long-fall terrain fixtures, use a real World-layer StaticBody2D with a GripStone/GripMetal GrappleSurface2D profile whose UseCollisionHitPoint is true. Do not attach a DedicatedAnchor circle or tune embedded point-anchor offsets to make contact coincide with the dock. Aim inside/beyond the real collider face; assert the locked anchor is the first ray-hit face, the saved normal is the actual face normal, and both anchor and dock remain locked. Collect every physics-frame position. Assert no point crosses the collider face minus the capsule support distance, at least six final samples stay within the 8px arrival envelope, and final speed does not alternate sign across the saved normal. Arrival at the safe stand-off is normally non-contact, so an actual slide collision is not a prerequisite. The thin blocker test must use a collider thinner than one 60Hz maximum-speed displacement and prove the body remains on the origin side.

The long-fall fixture must let the real `PlayerMotor` fall for 240 physics frames without teleporting, confirm downward speed has reached the existing 780px/s cap, then issue one airborne E-mode LMB click to a visible side-wall face within 620px and release LMB on the following input frame. Before first wall contact, assert every frame-to-frame targetward velocity change stays within `MaxPullAcceleration * delta` plus one gravity step, downward speed decreases over a multi-frame interval rather than becoming zero in one frame, and no ordinary jump or dash count changes. After safe wall arrival, one Space must publish Boost→Released, remain at or below 900px/s, and still leave accepted ordinary-jump count unchanged. The future map plan separately proves the authored abyss lasts 4–6 seconds and offers a middle exit and bottom landing; this fixture proves the claw remains usable after the long fall.

The terrain correction must not trivialize travel: start wall/ceiling/edge pulls meaningfully outside their arrival envelopes and assert a substantial physical approach. Preserve the original above-player wall/aim-height challenge in the long fall; do not move the target to predicted latch height merely to shorten braking. Record actual click, latch, braking and arrival observations in fixture/evidence output, not name-gated production logging. The prior embedded point-anchor trace is retained as diagnostic evidence, not mislabeled as real terrain-hitpoint proof.

Discriminate the actual capsule sweep from the other release branches with CapsuleFlankBlockerWithClearLineOfSightReleasesBeforeContact: insert a thin independent World collider in the capsule's flank path but outside the center-to-anchor ray. Assert continuing LOS remains clear, the body is initially separated with no prior contact, and an actual TestMove of the next displacement reaches the blocker. With only the motor held for one controller tick, require one typed Obstructed release before MoveAndSlide or stall timeout can classify it, then reenable physical motion and retain the no-crossing check. The same-owner wrong-face fixture must also assert owner-accepted LOS so its result is not misattributed to line occlusion.

- [ ] **Step 2: Run Task 4 RED**

Run the focused suite with `task4-collision-red.log`. Expected: exit 1 because post-latch occlusion currently retains pull physics and the controller lacks the body-sweep/contact-face classification and stall clock. Task 2 already supplies the pure dock-distance arrival envelope; retain that working baseline rather than claiming it is absent.

- [ ] **Step 3: Expose target ownership classification**

Rename the resolver's private `BelongsToSurface` helper to public static `ColliderBelongsToSurface` without changing its ancestry/legacy-receiver semantics. All LOS and collision code must call the same helper so a collider cannot be accepted by the ray but rejected by the body probe under a different ownership rule.

- [ ] **Step 4: Evaluate arrival before obstruction and stall**

At the start of each attached controller tick:

1. Resolve the real `CapsuleShape2D` from `BodyCollider`.
2. Read the DockPoint locked in the current cast frame; do not recompute or retarget it each tick.
3. Read the previous `MoveAndSlide` collisions from `CharacterBody2D.GetSlideCollisionCount/GetSlideCollision`.
4. Set `arrived=true` when the body is inside the 8px dock envelope and no actually present contact with the locked target has a normal incompatible with the saved face (dot must be ≥ 0.8 for target contacts). Target contact is not mandatory: the accepted 48px stand-off leaves a 12–28px gap from physical capsule contact, larger than the 8px envelope. Ignore unrelated supporting floor contacts in this saved-face arrival check. A wrong-face target contact is an obstruction under the surviving-real-pull guard, not a successful dock.
5. If arrived, reset `_stallSeconds` before any stall calculation.
6. Otherwise compare current body-to-saved-DockPoint distance with `_lastPullDistance`; a cumulative reduction of at least 1px since the last meaningful-progress sample resets the clock and updates that baseline, while less progress adds the unpaused physics delta. Qualify actual bounded braking of outward momentum as progress too: successive observed body velocities, projected along the same current dockward direction, must demonstrate a finite, nontrivial reduction of negative dockward speed toward zero. Proposed correction alone is not evidence of braking; a frozen body must still time out. During qualified braking, reset the clock and rebase dock distance to the current distance so later approach is not required to repay all pre-braking overshoot before being recognized. Keep collision/LOS obstruction authoritative and do not increase the 0.25s timeout. Initialize observations on attachment, freeze them during pause, and clear on terminal/reset. Outside qualified braking, do not overwrite the distance baseline every subpixel tick: legitimate slow final approach must accumulate progress. Raw anchor distance is not the progress authority because automatic motion converges on the saved dock point, not an arbitrary anchor-radius circle.
7. At `.25f`, create one Released frame with `Failure=Obstructed` and `ReleaseKind=Obstructed`, clear motor correction, and reset the clock.

Do not count time in Aim/Windup/Travel, Swing/arrived, pause, or after release.

- [ ] **Step 5: Check continuing line of sight and sweep the next displacement before applying it**

Only when the selected command leaves a real attached pull/constraint frame (Latched/Reel/Swing), before `ApplyMotor(next)`, form the intended displacement from the current observed body velocity plus the change in grapple correction. Never run this pull-classification sweep over an already selected Cancel/Boost Released transaction or a legacy target hit→apply→release; ordinary released-body collision stays with MoveAndSlide. This preserves Task 3's ordered input outcome rather than relabeling an intentional release as a pull obstruction.

First check the continuing real line of sight from the locked target resolution. A still-valid target with blocked LOS produces one Released frame with `Failure=Obstructed` and `ReleaseKind=Obstructed` before applying further pull correction; do not wait until the body physically reaches that remote blocker. This remains true after arrival in Swing: arrival suppresses stall, not newly blocked LOS. Add an arrived-then-inserted-blocker regression. An invalid/lost target remains Task 3's higher-priority TargetLost outcome. This LOS rule applies to surviving real attached constraints under the same command guard, not to an intentional Cancel/Boost release.

```csharp
Vector2 intendedVelocity = _player.Velocity +
    (next.VelocityCorrection - previous.VelocityCorrection);
Vector2 intendedMotion = intendedVelocity * seconds;
using var collision = new KinematicCollision2D();
bool wouldCollide = _player.TestMove(
    _player.GlobalTransform, intendedMotion, collision, _player.SafeMargin, false);
```

The body sweep uses the real Player shapes and World collision mask; fixture blockers must therefore be physical World-layer colliders, not ThreadTarget-only areas. Unlike direct-space queries, TestMove has no custom Exclude parameter. Dispose the temporary native collision wrapper with `using`. Classify `collision.GetCollider()` and `GetNormal()`; the predicted contact position is `_player.GlobalPosition + collision.GetTravel()`.

An impact is expected only when its collider belongs to `CurrentTarget`, its normal matches the saved normal, and that predicted contact position is in the arrival envelope. Every other hit emits one Released frame with both `Failure=Obstructed` and `ReleaseKind=Obstructed` before applying the proposed correction. Let `MoveAndSlide` remain authoritative; never move the body from the probe.

- [ ] **Step 6: Change the old occlusion contract**

Rename `AttachedOcclusionKeepsReelPhysicsAndClipsTheVisibleLine` to `AttachedOcclusionReleasesBeforePullingThroughTheBlocker`. Capture the one real Released publication with `Failure=Obstructed` and `ReleaseKind=Obstructed`, then assert Released→Retract/Idle and zero residual motor scale/correction. The real `ThreadLineVisual` clears on Released and `ThreadShuttleVisual` leaves attached cues; do not fabricate an extra active publication to preserve an obsolete visible-line assertion.

Retain the actual blocker-geometry invariant by calling the renderer's existing public `MechanicalClawTargetResolver.TryGetLineOcclusion(caster, surface, origin, anchor, out hit, out normal)` against the stable fixture surface and released frame endpoints. For the existing blocker centered at x=210 with width 40, assert the actual ray hits the first face at x=190 and its renderer-compatible `hit + normal.Normalized() * 4f` endpoint is near x=186 and never beyond the first face. Assert actual subscribed cleanup: line Idle, zero active strands, null gradient, no obscured connection flag, and no shuttle obscured cue. Do not claim an empty released line visually demonstrates an active clipped strand. Remove old continuing-reel, same-cast LOS-restoration and follow-on attached-unload assertions; target-unload cleanup remains covered by its dedicated lifecycle tests.

Update its existing aggregate registration, its one caller in `RunGrappleReleaseTests`, and directly await this renamed public method from `RunAutoPullClawTests._Initialize()`, keeping all current focused registrations. The focused command must execute the revised old occlusion contract, not defer it invisibly to Task 7's aggregate run.

- [ ] **Step 7: Run Task 4 GREEN**

Run the focused suite at fixed 60fps with `task4-collision-green.log`. Its retained `AutoPullSpeedAndAccelerationRemainBoundedAcrossDeltaSizes` scene-free test already steps the model at 30/60/120Hz inside that run; preserve and report those checks rather than re-running all real-physics fixtures under different engine frame rates or adding an otherwise unnecessary runner mode. Expected: all shapes stop safely, the thin blocker is never crossed, arrival wins over stall, and each obstruction produces one terminal cleanup.

- [ ] **Step 8: Commit when authorized**

```powershell
git add scripts/abilities/MechanicalClawTargetResolver.cs scripts/abilities/MechanicalClawController.cs tests/integration/MechanicalClawAutoPullRuntimeTests.cs tests/integration/MechanicalClawRuntimeTests.cs tests/RunAutoPullClawTests.cs tests/RunGrappleReleaseTests.cs
git commit -m "fix: make claw pull collision safe"
```

### Task 5: Publish the Target-Sector Residency Contract

**Status (2026-09-13): accepted at d5ad9e6 after independent scoped fix1 review and fresh exact-HEAD root verification.** Source API only; world pins/retirement remain in the continuous-world plan. Full evidence and cannot-verify dispositions are in this plan's SDD ledger.

**Files:**
- Create: `tests/integration/MechanicalClawResidencyContractTests.cs`
- Modify: `tests/RunAutoPullClawTests.cs`
- Modify: `scripts/abilities/MechanicalClawController.cs`

**Interfaces:**
- Produces: `public GrappleSurface2D ResidencyTarget { get; private set; }`.
- Produces: `public event Action<GrappleSurface2D, bool> TargetResidencyChanged`.
- Preserves: `public bool NotifyTargetWillUnload(Node subtree)` as the world→claw forced-invalidity API.
- World ownership boundary: the continuous-world project maps `ResidencyTarget` ancestry to its mounted sector and owns pin counts; abilities never reference `PrologueSectorId`.

- [ ] **Step 1: Add a compile-valid interface RED, then event-order behavior RED tests**

First assert the interface through reflection so the existing code still builds:

```csharp
Type controller = typeof(MechanicalClawController);
TestAssert.True(controller.GetProperty("ResidencyTarget") != null,
    "the claw exposes its current terrain residency target");
TestAssert.True(controller.GetEvent("TargetResidencyChanged") != null,
    "the claw exposes residency transitions to the world streamer");
```

Using the required fresh-build preamble, run the focused runner and preserve the runtime assertion failure; a missing-member compiler failure is not the accepted RED. Then add the property and event declarations without transition calls, replace the reflection-only check with the following event-order cases, rebuild successfully, and preserve their behavior RED:

```csharp
TerrainTravelPinsOnceAndReleaseUnpinsOnce();
AimWindupAndLegacyTargetsNeverPin();
CancelBoostDashShieldAndQUnpinBeforeTargetReferenceClears();
DamageDeathResetAndExitTreeUnpinExactlyOnce();
NotifyTargetWillUnloadUnpinsThenClearsSynchronously();
SubscriberCanReadResidencyTargetAfterLateBinding();
ThrowingSubscriberCannotPreventCleanupOrOtherSubscribers();
ResetInsidePinOrUnpinCallbackDoesNotRecurseOrRepin();
```

The callback must record that `(target,false)` fires while the argument remains valid and before the controller drops its private residency reference. Do not instantiate or modify `SeamlessPrologueWorld` in this suite.

- [ ] **Step 2: Run Task 5 behavior RED**

Using the required fresh-build preamble, run the focused suite with `task5-residency-red.log`. Expected: build exits 0 and Godot exits 1 because terrain Travel produces no `(target,true)` event and reset/unload produces no matching false event.

- [ ] **Step 3: Implement one transition helper**

All assignments go through one reentrancy-safe transition helper. A false callback must still be able to read the previous ResidencyTarget; consequently assigning the new field before notifying is not valid. Queue/coalesce requests made by synchronous callbacks instead of recursively re-entering the old-target notification:

```csharp
private bool _changingResidency;
private bool _hasPendingResidency;
private GrappleSurface2D _pendingResidency;

private void SetResidencyTarget(GrappleSurface2D next)
{
    _pendingResidency = next;
    _hasPendingResidency = true;
    if (_changingResidency) return;
    _changingResidency = true;
    try
    {
        while (_hasPendingResidency)
        {
            next = _pendingResidency;
            _pendingResidency = null;
            _hasPendingResidency = false;
            if (ReferenceEquals(ResidencyTarget, next)) continue;
            GrappleSurface2D previous = ResidencyTarget;
            if (previous != null && GodotObject.IsInstanceValid(previous))
                DispatchResidencyChanged(previous, false);
            ResidencyTarget = null;

            // A reset requested inside the false callback supersedes the stale request.
            if (_hasPendingResidency)
            {
                next = _pendingResidency;
                _pendingResidency = null;
                _hasPendingResidency = false;
            }
            if (next != null && GodotObject.IsInstanceValid(next))
            {
                ResidencyTarget = next;
                DispatchResidencyChanged(next, true);
            }
        }
    }
    finally { _changingResidency = false; }
}
```

Set a target only when a non-legacy surface owns the allocated `LockedTargetRuntimeId` and the state first enters Travel; it remains resident through Travel, Latched, Reel, Swing, and the synthetic Boost publication. On Space, publish Boost first, then emit `(target,false)`, then publish Released and clear the ordinary current-target reference. Clear it with the same once-only ordering for other Released, Retract, Idle, Failed, every reset path, target unload, and `_ExitTree`. Dispatch subscribers with the same exception isolation used by `ClawFrameChanged`.

In the added real-controller callback test, call ResetTransientState once from the first true callback and once from a false callback in separate runs. Both runs must end Idle with no target/correction, one true/false pair, no recursion and no stale post-callback repin or motor application. Reuse Task 3's lifecycle-preemption protection rather than creating another gameplay movement writer. This test is about the consumer-observable lifecycle, not private guard-field values.

- [ ] **Step 4: Document the consumer order in code comments and tests**

The world consumer must subscribe, immediately read `ResidencyTarget` to acquire any already-active target, then handle future events. On `(target,true)` it pins the owning sector. On `(target,false)` it first guarantees current-player and landing-sector residency, then unpins the target sector. This task documents and tests that contract; it does not implement the world side.

- [ ] **Step 5: Run Task 5 GREEN**

Run the focused suite with `task5-residency-green.log`. Expected: exit 0, no duplicate pin/unpin event, legacy interaction has zero residency events, and forced unload finishes at Idle with normal motor scale.

- [ ] **Step 6: Commit when authorized**

```powershell
git add scripts/abilities/MechanicalClawController.cs tests/integration/MechanicalClawResidencyContractTests.cs tests/RunAutoPullClawTests.cs
git commit -m "feat: expose grapple target residency"
```

### Task 6: Adapt Controls, Guidance, Pose, Audio, and VFX

**Status (2026-09-13): accepted at01c0821 after independent fix1 review.** Initial528c234 passed root fresh controls/feedback/auto-pull gates; audio-only fix01c0821 passed a fresh covering feedback run. This certifies code-level contracts, not normal-window visual or listening quality. The two-positive-sample envelope's human legibility remains a named Task8 check.

**Files:**
- Create: `tests/integration/PlayerClawPresentationRuntimeTests.cs`
- Modify: `tests/RunControlsReferenceTests.cs`
- Modify: `tests/RunTask4AudioVfxTests.cs`
- Modify: `assets/localization/prologue-ui.json`
- Modify: `scripts/ui/PrologueControlsView.cs`
- Modify: `scripts/prologue/ProloguePauseMenu.cs`
- Modify: `scripts/prologue/sample/BasinSampleGuideController.cs`
- Modify: `scripts/presentation/PlayerPresentationCoordinator.cs`
- Modify: `scripts/audio/PrologueAudioDirector.cs`
- Modify: `scripts/vfx/PrologueVfxDirector.cs`
- Modify: `tests/integration/PrologueUiRuntimeTests.cs`
- Modify: `tests/unit/PrologueGuidanceModelTests.cs`
- Modify: `tests/integration/PrologueGuidanceRuntimeTests.cs`
- Modify: `tests/integration/DescentClawGuidanceRuntimeTests.cs`
- Modify: `tests/integration/PrologueAudioDirectorTests.cs`
- Modify: `tests/integration/PrologueVfxRuntimeTests.cs`
- Modify: `tests/unit/PlayerPresentationTests.cs`

**Interfaces:**
- Consumes: ordered Boost→Released publications and real Reel/Swing states.
- Produces: bilingual click/auto-pull instructions with no hold-LMB, release-LMB, or W/S-reel claim.
- Produces: one boost onset and one release onset for the atomic Space transaction.

- [ ] **Step 1: Write UI/guidance RED assertions against real controls nodes**

For both languages, require the keyboard GrappleMode label and pause controls to contain E, LMB/左键, automatic pull wording, A/D, Space, and RMB/Q cancel wording. Reject `HOLD LMB`, `按住左键`, `RELEASE LMB`, `松开左键`, `W/S REEL`, and `W/S 收放绳`. Retain the existing four-resolution minimum-size, container-width, Back-button non-overlap, and keyboard↔gamepad↔keyboard duplication checks.

Update attached guidance assertions to require “A/D optional swing” and “Space release/跃出”, while pre-attachment guidance remains E→click and does not prematurely teach attached controls.

Register the guidance assertions in the actual focused command: `RunControlsReferenceTests` must retain its existing UI call and also call `PrologueGuidanceModelTests.Run()`, `await PrologueGuidanceRuntimeTests.Run(this)`, and `await DescentClawGuidanceRuntimeTests.Run(this)`, adding the unit-test namespace import. The current runner does not reach those classes; modifying a test class without registering it is not a RED/GREEN result. Keep each existing test registered.

- [ ] **Step 2: Add atomic feedback RED assertions**

Feed Reel, Swing, Boost, Released, duplicate Boost, and Retract frames for one cast. Assert one tension-loop onset before Space, one distinct accepted Boost onset, one release onset, and no duplicate onset from the repeated synthetic Boost or Retract. Assert the presentation coordinator observes Boost before Released even though controller `CurrentFrame` ends Released.

For VFX, assert Boost can produce one onset even if the same cast already emitted Reel tension; a duplicate Boost for that cast produces no second burst.

Register `PlayerPresentationTests.Run()` in `RunTask4AudioVfxTests` with its unit-test namespace import, retaining the existing awaited audio and VFX runs. This puts the changed presentation assertions on the feedback command's execution path. Do not depend on the later full `RunPrologueTests --phase=pre` migration to execute Task 6's new tests.

Also register `await PlayerClawPresentationRuntimeTests.Run(this)` in that feedback runner. The scene-free timeline unit suite cannot prove the real coordinator consumes the atomic publication. In a real player/controller/coordinator fixture, capture ordered Boost→Released, then sample the coordinator's published `LastPlayerFrame` and the formal visual's applied typed frame after a positive-delta presentation frame. Require a visible launch/tension action before a subsequent recovery action despite gameplay already being Released/Retract. Counting synchronous cue calls alone is insufficient. Verify duplicate events cannot replay the onset, and death/recovery/new cast/higher-priority shield or dash does not retain stale claw presentation. The fixture may arrange its isolated starting state but must trigger the actual controller transaction and must not hold or alter gameplay Boost, physical velocity, input, or target lifetime to manufacture the visible phase. The later Task8 recording remains the rendered legibility/taste check.

- [ ] **Step 3: Run the focused UI/audio REDs**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed before controls RED' }
& $godot --headless --path . --fixed-fps 60 --script res://tests/RunControlsReferenceTests.cs --log-file (Join-Path $evidenceRoot 'task6-controls-red.log')
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed before feedback RED' }
& $godot --headless --path . --fixed-fps 60 --script res://tests/RunTask4AudioVfxTests.cs --log-file (Join-Path $evidenceRoot 'task6-feedback-red.log')
```

Expected: controls fail on W/S/hold/release wording; presentation fixtures fail because they expect Boost to remain the current attached state.

- [ ] **Step 4: Replace the obsolete bilingual copy**

Use meaning-equivalent compact copy:

```text
中文：鼠标瞄准，点按左键发射；命中后自动牵引
      挂接后 A/D 可选摆荡；SPACE 跃出并松爪；Q/右键取消
English: Mouse aim; click LMB to fire; a hit pulls automatically.
         Attached: A/D optional swing; SPACE leap-releases; Q/RMB cancels.
```

Apply the same rules to `guidance.grapple_attached`, `guidance.climb`, both pause strings, and the Basin ascent card. Preserve the compact card size and local enemy/readiness branches; change only obsolete claw-control wording.

- [ ] **Step 5: Make feedback transaction-aware**

Keep `CueForClawFrame(Boost) == ClawTension` and the existing audio exception that permits Boost onset after Reel/Swing. Audio dedup remains `(CastId, State, Failure)` for allocated casts so repeated Boost is rejected. Pre-validation rejections have `CastId == 0`, which is not an attempt identity: use consecutive-frame dedup for those frames so a later identical rejected attempt after Idle/new Aim still produces feedback. Test both same-attempt duplicate rejection and the next identical attempt; do not introduce a new gameplay cast/attempt API for this presentation-only fix.

Change VFX's Task10 dedup key to include a boost-event discriminator:

```csharp
var eventKind = frame.State == MechanicalClawState.Boost ? 1 : 0;
var key = (cue.Value, frame.CastId, eventKind);
```

Reel and Swing still share event kind 0 and therefore do not spam tension bursts; Boost gets exactly one event kind 1 burst. The following Released uses its existing release cue. Do not create a new audio asset or cue enum.

Update `PlayerPresentationCoordinator` so the back-to-back Boost and Released callbacks each advance the local presentation state once; it must not require gameplay Boost to survive until the next rendered frame. The Boost callback selects the existing `ThreadApplied`/boost action, and Released selects `ThreadRecovery`. Preserve a bounded presentation-only launch-to-recovery transition across positive-delta display frames rather than letting immediate recovery overwrite the launch before it can be drawn. Use value snapshots only, never retain a target node after release. Keep actual rope/target release immediate and all gameplay timing unchanged. Explicitly test lifecycle preemption and clear the local transition on recovery reset; report the chosen local envelope and any ambiguity before broadening ownership. Do not treat two back-to-back `PlayCue` calls as proof: typed `ApplyPlayerFrame` currently overrides compatibility cues each process frame.

- [ ] **Step 6: Run Task 6 GREEN**

Run `RunControlsReferenceTests.cs`, `RunTask4AudioVfxTests.cs`, and `RunAutoPullClawTests.cs` serially with unique green logs. Expected: all exit 0, no obsolete keyboard instruction remains in tested user-visible surfaces, and Boost/Released feedback is each observed exactly once.

- [ ] **Step 7: Commit when authorized**

```powershell
git add tests/RunControlsReferenceTests.cs tests/RunTask4AudioVfxTests.cs assets/localization/prologue-ui.json scripts/ui/PrologueControlsView.cs scripts/prologue/ProloguePauseMenu.cs scripts/prologue/sample/BasinSampleGuideController.cs scripts/presentation/PlayerPresentationCoordinator.cs scripts/audio/PrologueAudioDirector.cs scripts/vfx/PrologueVfxDirector.cs tests/integration/PrologueUiRuntimeTests.cs tests/unit/PrologueGuidanceModelTests.cs tests/integration/PrologueGuidanceRuntimeTests.cs tests/integration/DescentClawGuidanceRuntimeTests.cs tests/integration/PrologueAudioDirectorTests.cs tests/integration/PrologueVfxRuntimeTests.cs tests/unit/PlayerPresentationTests.cs tests/integration/PlayerClawPresentationRuntimeTests.cs
git commit -m "feat: present click auto pull controls"
```

### Task 7: Migrate Existing Claw Routes Without Weakening Compatibility

**Development-checkpoint ordering (root, 2026-09-13; not task completion):** This task has exposed both unused legacy-room drivers and a real layout1 catch-return conflict with the new collision-safe pull. After the bounded input migrations, freeze an explicitly incomplete development checkpoint: run all five required commands on the same source and report their actual outcomes without requiring a false GREEN; preserve every failing case/threshold and all user tests. Record the first physical obstruction and unexecuted downstream cases. Independent review must inspect the implemented changes and identify unresolved production defects; root must obtain fresh focused claw/controls/feedback and P0 boundary/door coverage before building on the checkpoint. Task7 and Task8 remain incomplete, and no playable release, ZIP, or complete-claw claim is permitted. Independent continuous-world graph/content work may then proceed under that plan's already accepted P0/residency prerequisites, carrying the unresolved layout1/catch-return and normal-window obligations into explicit final verification. This reorders work toward the user's requested new world; it does not turn an old route failure into success or authorize weaker landing/physics checks.

**Evidence-backed prerequisite (root ruling, 2026-09-13):** The existing dirty aggregate runner starts with the user's untracked `BasinAscentCameraRuntimeTests`. Its fresh baseline fails before the owned claw cases: at Basin position `(2867.57,2013.98)`, a complete capsule's horizontal viewport margin is `-7.5700684px` instead of at least `16px`, with camera limits `1280..2880` and zero recoveries. Sector-only camera limits clip the body's radius before its center crosses the seam. This is an actual pre-existing visibility defect, not grounds to skip the test or weaken the requirement.

Task7 additionally owns a minimal camera-only correction in `scripts/prologue/seamless/SeamlessPrologueWorld.cs`. Permit horizontal viewing into a genuinely mounted adjacent sector only while that sector covers the complete visible vertical band. Keep the entire real capsule at least 16 logical viewport pixels from either horizontal edge during the existing seam-side holds. Above the Basin/Ascent shared vertical band, preserve the Ascent left boundary so the view does not reveal absent Basin coverage. Do not merge the enclosing rectangles across uncovered space, change `CurrentSector`, mount extra sectors, move/recover the player, touch camera shake `Offset`, edit maps, or alter physics to obtain visibility. Preserve all existing camera tests, including the supplied untracked test and probe, without staging or changing them. Add a durable equivalent regression to the already-owned `PrologueCanyonTraversalTests` using actual capsule projection and the same unchanged visibility/authority/no-recovery guards. Record the original baseline failure and final covering pre/post results; do not label the route suite green until the real pre phase passes.

**Files:**
- Modify (camera-only prerequisite): `scripts/prologue/seamless/SeamlessPrologueWorld.cs`
- Modify (input-fixture prerequisite only): `tests/integration/PlayerShieldRuntimeTests.cs`
- Modify (nonfatal damage input invalidation only): `scripts/abilities/MechanicalClawController.cs`
- Modify (scoped input invalidation helper only): `scripts/player/PlayerInputReader.cs`
- Modify diagnostic output only: `tests/integration/MechanicalClawAutoPullRuntimeTests.cs`
- Modify: `tests/integration/MechanicalClawRuntimeTests.cs`
- Modify: `tests/integration/PlayerTraversalInputTests.cs`
- Modify: `tests/integration/PrologueCanyonTraversalTests.cs`
- Modify (missed legacy route consumer): `tests/integration/PrologueTraversalRouteTests.cs`
- Modify: `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`
- Modify: `tests/presentation/ClawConnectionReadabilityProbe.cs`
- Modify: `tests/presentation/ProloguePresentationTask6.cs`
- Modify: `tests/presentation/PrologueUiResolutionCapture.cs`
- Modify: `tests/presentation/FirstEnemyEncounterProbe.cs`
- Modify: `tests/presentation/BasinQualitySampleLoopProbe.cs`
- Modify: `tests/presentation/BasinExplorationProbe.cs`
- Modify: `tests/integration/CanyonClawMechanismTests.cs`
- Modify: `tests/integration/BellChainRuntimeTests.cs`

**Second evidence-backed prerequisite (root ruling, 2026-09-13):** Full pre now passes the three unchanged camera cases and migrated claw cases but fails `PlayerShieldRuntimeTests.ActivateWithoutDrain`: after the prior rear hit, the production damage→claw-reset path clears the sampled input. The fixture presses Shield and manually ticks only Shield/Energy, omitting the real Input sampler. Authorize only this test file to sample the actual `PlayerInputReader` before manual zero-delta input-dependent cycles, preserving zero time/energy drain, exact 24-energy spending, same-frame heavy-hit sustain, directional guarding, overheat, and movement assertions. Keep the original failing run. This is not permission to alter shield/energy/input production or weaken the two same-frame heavy-hit contract; report a subsequent divergence with evidence for separate root diagnosis.

**Third evidence-backed prerequisite (root ruling, 2026-09-13):** After that fixture migration, actual pre-shield-sampler21 (build131668/0, Godot116584/1) fails the preserved same-frame second-heavy shield-sustain assertion. Independent source audit and runtime RED agree: first resolved chip damage resets the claw and clears held shield; the second energy publication refreshes against this erased hold and closes the still-affordable shield. Additionally authorize MechanicalClawController and PlayerInputReader only to distinguish nonfatal damage reset from full lifecycle input clearing. Nonfatal damage must still reset the claw/correction/residency once and consume the sampled one-shot action/mode/slot edges, but preserve held defense and continuous aim/input. Public reset, death, recovery and target-loss retain full ClearSampledFrame behavior; no global weakening or shield/energy/physics tuning. Keep the no-resampling same-frame heavy test as the RED→GREEN regression. In an already-owned runtime test, also prove nonfatal damage consumes old primary/jump/dash edges without recasting, preserves held shield/aim, and death/public reset still clear them. This supersedes only the earlier production exclusion for this diagnosed chain. Run fresh covering five gates after the fix; prior focused GREEN does not certify amended production.

**Missed legacy-route consumer (root ruling, 2026-09-13):** pre-damage-fix26 passes the shield chain, then fails PrologueTraversalRouteTests in BrokenToothCorridor.entry_to_anchor_landing (sample0.5,149/150frames, still Reel). The independent bounded source audit finds DriveAssisted still holds W/S/Primary and tries to cancel with only Primary release. Add this test file to input-only migration ownership, including its shared continuous-route driver. Use first click/mouse-up, no W/S reel, and an actual second Primary press when its physical/predicted safe-release window requires cancellation; passive mouse-up cannot release. Do not require arrival as a substitute for every intentional early release. Preserve all six rooms/twelve contracts/28 fresh samples, 90/150-frame budgets, departure, target-zone two-frame stable landing, zero Dash, no death/exit, exact one cast/apply/authored target identity and range/failure assertions. Do not alter room geometry, mapper, registry or production for this missing consumer; report any genuinely infeasible original landing contract separately.

**Interfaces:**
- Consumes: final click/auto-pull runtime and presentation semantics.
- Preserves: typed ice/forbidden/range/obstruction failures, exact cast identity, A/D swing, target unload cleanup, Q handoff, air-dash budget, and legacy adapter activation.
- Produces: current-map regression routes that no longer depend on held LMB or fixed-duration W reeling.

- [ ] **Step 1: Inventory each obsolete assertion before editing it**

Use this bounded scan and record every hit in the task log:

```powershell
rg -n 'HOLD LMB|hold LMB|RELEASE LMB|release LMB|W/S|reel|收绳|松开左键|PrimaryHeld|PrimaryReleased|ReelAxis' tests scripts/prologue assets/localization
```

Classify each hit as user-facing instruction, model/runtime contract, route driver, or harmless historical evidence text. Do not rewrite historical docs. Every executable route driver in the file list above must be migrated or explicitly deleted with a replacement assertion in the same commit.

Close the deferred diagnostic-output minors from Tasks1/4: keep default focused output concise; emit full BOOST_RENDER_TRACE/TASK4_TERRAIN_TRAJECTORY per-frame data only through explicit diagnostic mode or a named evidence artifact. Preserve every behavioral/trajectory assertion. MechanicalClawAutoPullRuntimeTests ownership here is diagnostic gating only, not an opportunity to weaken its accepted true-terrain/sweep/fall contracts. Also rename the old ScheduledAttachedSpaceBoostKeepsLatchAndSuppressesMotorJump test to match the accepted Released transaction and retained ordinary-jump suppression, migrating its named callers.

- [ ] **Step 2: Convert route input, not success thresholds**

For every real route:

1. Press LMB for one input frame and release it immediately.
2. Never press MoveUp/MoveDown as a reel input.
3. Wait on observable `Reel`, `Arrived/Swing`, body position, or named landing support with the existing bounded-frame timeout.
4. Use A/D only when the route is intentionally testing optional tangential steering.
5. Use Space once only where the route intentionally tests boost-release.

Do not replace movement with `GlobalPosition` assignment, enlarge map collision, relax safe-landing margins, or extend timeouts merely to get green.

**Ceiling-release semantic ruling (root, 2026-09-13):** Actual post-ceiling-geometry22 (build50332/0, engine111720/1) reaches the real underside `(750.81964,516)` with saved outward normal `(0,1)` and dock `(750.81964,564)`. Requested release `(129.53165,-387.68054)` points inward; the accepted motor correctly removes its inward normal component. The obsolete assertion requiring still-more-upward velocity contradicts safe atomic release at a solid ceiling. Replace only that assertion with an actual release-transaction velocity check against independently computed outward-normal projection and the existing speed cap, retaining meaningful tangential motion, exact cast Boost→Released/Boost identity, zero ordinary jump/Dash, real collision profile, and no W auto-take-up assertions. Sample after motor release application, not a pre-physics stale frame; later gravity samples are not the release velocity. Keep the independent side-grip upward-inertia/three gravity-only ticks/short-hop restoration thresholds unchanged. No ceiling/map/motor change is authorized by this ruling.

- [ ] **Step 3: Preserve legacy and failure tests verbatim in meaning**

`CanyonClawMechanismTests` and `BellChainRuntimeTests` must still prove E+one LMB click performs exactly one legacy effect and reaches Released/Idle without player displacement toward the receiver. Ice, forbidden, no-target, range, and initial LOS tests retain their exact typed reasons. Target identity replacement and target unload still fail closed and clear the motor/visual state.

- [ ] **Step 4: Replace rendered Boost dwell assumptions**

`ProloguePresentationTask6` and capture probes must observe the ordered callback transaction rather than polling until `CurrentFrame.State == Boost`. Capture the last rendered attached Reel/Swing frame, the Boost callback's selected event, and Task6's positive-delta displayed launch-to-recovery frames while gameplay is already Released/Retract. Assert the boost event existed once and the local visible transition is applied; do not require a one-frame gameplay Boost dwell that the approved transaction forbids.

`ClawConnectionReadabilityProbe` must show automatic spool rotation during Reel with zero W input, stable spool at arrived Swing, and cleanup after Space/cancel. Retain its pixel-change lower bounds unless an actual reviewed frame proves the bound is invalid.

The generated-main route's `Capture` helper currently writes fixed names into `res://artifacts/task8-fix1-proof`. When `captureFrames` is true, route those stills into a caller-supplied absolute external `--output=` directory instead, using the same outside-project validation already present in `ClawConnectionReadabilityProbe`. Fail clearly before writing when no valid capture destination was supplied; non-capturing/headless paths need no output argument. Root will supply a new unique directory for each Task8 display run. Do not overwrite the old artifact directory.

- [ ] **Step 5: Run focused then full legacy suites serially**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed before auto-pull tests' }
& $godot --headless --path . --fixed-fps 60 --script res://tests/RunAutoPullClawTests.cs --log-file (Join-Path $evidenceRoot 'task7-auto-pull.log')
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed before claw contracts' }
& $godot --headless --path . --fixed-fps 60 --script res://tests/RunMechanicalClawContracts.cs --log-file (Join-Path $evidenceRoot 'task7-claw-contracts.log')
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed before release tests' }
& $godot --headless --path . --fixed-fps 60 --script res://tests/RunGrappleReleaseTests.cs --log-file (Join-Path $evidenceRoot 'task7-release.log')
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed before pre phase' }
& $godot --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs --log-file (Join-Path $evidenceRoot 'task7-pre.log') -- --phase=pre
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'build failed before post phase' }
& $godot --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs --log-file (Join-Path $evidenceRoot 'task7-post.log') -- --phase=post
```

Expected: every process exits 0; logs contain their exact success marker; no `ERROR`, managed exception, leaked RID, or timeout. A route failure is investigated from its first divergent state/position and is not hidden by rerunning or raising the bound.

These commands cover the physical sector routes, not the display-backed generated-main pointer route. `RunTask8GeneratedMainProof` is the existing caller for `PrologueCanyonGeneratedMainRouteTests`; its headless path intentionally returns before the first grapple. Migrate that route's source here, but record its full display-backed execution as the required Task8 gate below, not as part of these five GREENs. The old private `ScheduledAttachedSpaceBoostKeepsLatchAndSuppressesMotorJump` currently has only its same-file `Run` caller; do not edit the dirty aggregate runner unless a concrete new call-site issue requires a separately scoped change.

- [ ] **Step 6: Commit when authorized**

```powershell
git add tests/integration/MechanicalClawAutoPullRuntimeTests.cs tests/integration/MechanicalClawRuntimeTests.cs tests/integration/PlayerTraversalInputTests.cs tests/integration/PrologueCanyonTraversalTests.cs tests/integration/PrologueCanyonGeneratedMainRouteTests.cs tests/presentation/ClawConnectionReadabilityProbe.cs tests/presentation/ProloguePresentationTask6.cs tests/presentation/PrologueUiResolutionCapture.cs tests/presentation/FirstEnemyEncounterProbe.cs tests/presentation/BasinQualitySampleLoopProbe.cs tests/presentation/BasinExplorationProbe.cs tests/integration/CanyonClawMechanismTests.cs tests/integration/BellChainRuntimeTests.cs
git commit -m "test: migrate routes to click auto pull"
```

### Task 8: Validate the Sub-Project as a Running Game

**Files:**
- Modify only if evidence exposes a scoped defect: files already owned by Tasks 1–7.
- This includes Task6's new `tests/integration/PlayerClawPresentationRuntimeTests.cs` and its focused runner registration; stage these only if actually amended during the final proof.
- Evidence output: unique external evidence directory selected by the main owner.

**Interfaces:**
- Consumes: complete auto-pull implementation and all focused/full runners.
- Produces: reviewable proof for the mechanical-claw sub-project; it does not certify the future continuous map or 20–45 minute duration targets.

- [ ] **Step 1: Run a clean rebuild only after all source edits stop**

```powershell
$env:DOTNET_ROOT = 'G:\godot\dotnet-sdk'
$env:DOTNET_ROOT_X64 = 'G:\godot\dotnet-sdk'
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:PATH = 'G:\godot\dotnet-sdk;' + $env:PATH
& 'G:\godot\dotnet-sdk\dotnet.exe' build --no-restore -t:Rebuild
```

Expected: exit 0 with no compile warning newly introduced by this change. Do not start Godot while the compiler remains active.

- [ ] **Step 2: Re-run all focused and full gates once**

Run the five commands from Task 7 plus `RunControlsReferenceTests.cs` and `RunTask4AudioVfxTests.cs`, serially into new log names. Even after the clean rebuild, invoke the Global Constraints fresh `dotnet build --no-restore` preamble immediately before each individual Godot command; never infer freshness from the preceding runner. Check process exit, exact success marker, and absence of `ERROR`, exception, timeout, and leaked-resource signatures in both console and Godot logs.

- [ ] **Step 3: Run one visible keyboard/mouse proof from the normal entry**

Use the repository's normal prologue launcher, not a test-only scene. Demonstrate without holding LMB or W:

Normal-entry interpretation: use the configured production `PrologueLevel.tscn` and demonstrate its real Title→Start-button-input→Playing path with the same level/UI/player instances; a dedicated capture driver may load that exact production scene, as allowed by godot.md. Do not repeat the old launcher's console-wrapper import sequence or the old recorder's Dummy/gl_compatibility arguments, which conflict with the fresh-build/actual-process/hardware-Vulkan requirements. In the captured generated-main path, disable both auto-start flags, await the actual visible/focused Start control, inject a press/release of `ui_accept` or a mouse click at its live button center, and require exactly one `SessionStarted`, Playing state, hidden ProductUi and unchanged instance IDs. Do not call flow methods or emit button signals to simulate that press. Preserve non-capture route contracts unless a concrete integration issue requires migration. The source-only title/terrain preflights in the task workspace identify integration points and missing casts, not runtime evidence.

First run the existing `res://tests/RunTask8GeneratedMainProof.cs` with a real display/hardware Vulkan, a fresh successful build and unique evidence output. Supply `-- --output=<new absolute external stills directory>` to the migrated capture helper. It loads the exact generated `PrologueLevel.tscn` and exercises the changed pointer route. Require `TASK8_GENERATED_MAIN_ROUTE_OK` and `TASK8_GENERATED_MAIN_PROOF_OK`; `TASK8_GENERATED_MAIN_HEADLESS_OK` is only a partial stream-boundary result and cannot satisfy this gate. This route-driver proof complements, not replaces, the normal-entry visible demonstration below.

1. ground click→release to a wall;
2. airborne click→release to a ceiling;
3. convex edge auto-pull and stable stop;
4. optional A/D swing;
5. Space boost-release;
6. second-click cancel;
7. RMB cancel to shield;
8. Dash cancel without refunded air dash;
9. ice rejection;
10. legacy mechanism activation without pulling the player.

The current map is not required to contain the newly approved 4–6 second abyss, and this sub-project must not add one. After another fresh build, separately run `FourSecondFallCanClickAVisibleWallAndBrakeContinuously` in the real-physics fixture and preserve its trajectory log. The continuous-world plan owns the later normal-entry proof that the authored abyss supports a four-second fall, wall catch, middle exit, and safe bottom landing; this plan must not claim that map-level proof.

Record the actual input edges, positions, states, cast IDs, release kinds, accepted jump/dash counts, and maximum speed. Do not claim sound quality from a dummy-audio run.

- [ ] **Step 4: Review the recording before handoff**

Verify visually that the body follows a continuous arc, slows near the surface, does not penetrate or vibrate at the wall/ceiling/edge, the rope and spool agree with Reel/Swing, and Space shows one readable release action. If any item fails, add the smallest reproducing assertion to the owning Task 1–7 suite before changing production.

- [ ] **Step 5: Request a human feel check without claiming the map is complete**

Ask the user to try wall, ceiling, edge, cancel, and Space release. Record whether target selection, acceleration, arrival softness, and release direction feel understandable. Treat 540/720/2400/160/900 as tuning starts: any change requires before/after trajectory evidence and rerunning Tasks 2, 4, 6, and 7.

- [ ] **Step 6: Commit the final scoped corrections when authorized**

```powershell
git status --short
git diff --check
git add -- scripts/abilities/MechanicalClawArrivalGeometry.cs scripts/abilities/MechanicalClawCommand.cs scripts/abilities/MechanicalClawSettings.cs scripts/abilities/MechanicalClawTargetSnapshot.cs scripts/abilities/MechanicalClawFrame.cs scripts/abilities/MechanicalClawInput.cs scripts/abilities/MechanicalClawTargetResolver.cs scripts/abilities/MechanicalClawModel.cs scripts/abilities/MechanicalClawController.cs scripts/player/PlayerInputReader.cs scripts/player/PlayerActionModeController.cs scripts/player/PlayerMotor.cs scripts/audio/PrologueAudioDirector.cs scripts/vfx/PrologueVfxDirector.cs scripts/presentation/PlayerPresentationCoordinator.cs scripts/ui/PrologueControlsView.cs scripts/prologue/ProloguePauseMenu.cs scripts/prologue/sample/BasinSampleGuideController.cs assets/localization/prologue-ui.json tests/RunAutoPullClawTests.cs tests/unit/MechanicalClawArrivalGeometryTests.cs tests/unit/MechanicalClawModelTests.cs tests/integration/MechanicalClawAutoPullRuntimeTests.cs tests/integration/MechanicalClawResidencyContractTests.cs tests/integration/MechanicalClawRuntimeTests.cs tests/integration/PlayerActionModeRuntimeTests.cs tests/integration/PlayerTraversalInputTests.cs tests/integration/PrologueUiRuntimeTests.cs tests/unit/PrologueGuidanceModelTests.cs tests/integration/PrologueGuidanceRuntimeTests.cs tests/integration/DescentClawGuidanceRuntimeTests.cs tests/integration/PrologueAudioDirectorTests.cs tests/integration/PrologueVfxRuntimeTests.cs tests/unit/PlayerPresentationTests.cs tests/integration/PrologueCanyonTraversalTests.cs tests/integration/PrologueCanyonGeneratedMainRouteTests.cs tests/presentation/ClawConnectionReadabilityProbe.cs tests/presentation/ProloguePresentationTask6.cs tests/presentation/PrologueUiResolutionCapture.cs tests/presentation/FirstEnemyEncounterProbe.cs tests/presentation/BasinQualitySampleLoopProbe.cs tests/presentation/BasinExplorationProbe.cs tests/integration/CanyonClawMechanismTests.cs tests/integration/BellChainRuntimeTests.cs
git commit -m "feat: deliver click auto pull claw"
```

Do not include P0 world/game-flow edits, continuous-map work, generated evidence, user saves, exports, or packaged builds in this commit.
