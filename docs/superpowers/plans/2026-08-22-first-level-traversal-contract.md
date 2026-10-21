# First-Level Traversal Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every mandatory first-level traversal segment honest and repeatable: one jump where declared, one clearly signposted thread pull where assisted, never an undocumented double jump or second air dash.

**Architecture:** `PlayerMotionModel` remains the authority for jump and dash consumption and publishes a narrow diagnostic result for accepted/rejected dash input. Generated rooms carry typed traversal-contract nodes that name stable launch, landing, and optional anchor paths. Integration probes drive the real `InputMap → PlayerInputReader → PlayerMotor → CharacterBody2D/ThreadAbility` stack against those contracts; structure tests reject an unclassified mandatory segment before a room can ship.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, generated `.tscn` rooms, deterministic 60 Hz physics tests, existing mechanical-thread state machine, PowerShell verification/capture tools.

**Spec:** [First-level movement ability and reachability design](../specs/2026-08-22-first-level-traversal-contract-design.md)

## Global Constraints

- Do not add double jump.
- Preserve exactly one directional air dash from takeoff until the next authoritative floor landing. Thread, attack, pause, room transition, wall/ceiling/side contact, knockback, and down-strike bounce do not refund it.
- Keep mechanical thread as the existing discrete pull. Do not add elastic suspension, continuous swinging, trampolines, gummies, or another movement entity.
- Mandatory segments are either stable single-jump segments or explicit one-thread assisted segments. Air dash is correction/optional traversal only and is not required by the first-level main route.
- Use the current production `PlayerTuning` and `ThreadTuning`; do not globally increase jump height, reduce gravity, or increase pull speed to make a test pass.
- Route probes use real InputMap actions and real physics. They may set the initial launch marker once, but may not directly write velocity, teleport after launch, disable collision, or call `PlayerMotionModel.Step` as a substitute for runtime traversal.
- A single-jump segment must pass from three launch samples spanning the middle 60% of its declared launch zone. An assisted segment must complete with Jump + one Thread cast and zero Dash starts.
- Preserve stable player, room, platform, anchor, guidance, and asset-replacement paths. Generated scenes are rebuilt only from their C# builders and semantic diffs are inspected before staging.
- No pay-per-use generation.

---

### Task 0: Close carried action-motion review findings before traversal

**Files:**
- Modify: `scripts/presentation/EnemyFormalVisualPart.cs`
- Modify: `scripts/presentation/GraveLampMothFormalVisual.cs`
- Modify: `tools/build/art/BuildEnemyFormalVisuals.cs`
- Regenerate: `scenes/presentation/GraveLampMothFormalVisual.tscn`
- Modify: `tests/integration/EnemyFormalVisualTests.cs`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tests/presentation/PrologueVisualAudit.cs`

**Interfaces:**
- Preserves: the stable room-independent Moth path `Wings` as a common translation anchor.
- Produces: real typed hinge slots `Wings/FarWing` and `Wings/NearWing`, each owning one rendered wing and inheriting the existing phase-driven Moth pose.
- Produces: `GraveLampMothFormalVisual.CurrentFarWingTransform` and `CurrentNearWingTransform` from those live hinge nodes; preserves `CurrentWingTransform` as the common `Wings` anchor component for compatibility.
- Replaces: the single `EnemyFormalPartKind.MothWings` renderer with `MothFarWing` and `MothNearWing` kinds; no gameplay state, attack timing, collision, damage, contact, or corruption contract changes.
- Strengthens: required target lifecycle visibility from local `CanvasItem.Visible` to effective `CanvasItem.IsVisibleInTree()` plus the existing rendered-vertex bounds checks.

- [ ] **Step 1: Add two named RED regressions**

Add `MothTelegraphFoldsOpposedRealWingHinges()` to `EnemyFormalVisualTests.Run()`. Instantiate the generated Moth leaf, require `Wings/FarWing` and `Wings/NearWing` to be typed renderers, publish a real `MothDive` Telegraph frame, and assert both hinges move, their signed rotations oppose one another, their folded endpoints are closer than idle, and the generated `Wings` path remains present.

Add an ancestor-hide mutation to the presentation audit using:

```text
KNIGHT_PRESENTATION_HIDE_REQUIRED_TARGET_ANCESTOR_LIFECYCLE=thread-lesson
```

During only the named required lifecycle, set a visible `CanvasItem` ancestor of the production marker to `Visible=false` while leaving the marker's own `Visible=true`. The run must fail with lifecycle, frame, marker path, and `effective_visibility=false`.

- [ ] **Step 2: Run RED and preserve both named failures**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --nologo
$env:KNIGHT_PRESENTATION_HIDE_REQUIRED_TARGET_ANCESTOR_LIFECYCLE='thread-lesson'
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --fixed-fps 60 --script res://tests/presentation/ProloguePresentation.cs --log-file traversal-task0-target-red.log
Remove-Item Env:KNIGHT_PRESENTATION_HIDE_REQUIRED_TARGET_ANCESTOR_LIFECYCLE
```

Expected RED: the Moth test cannot resolve two typed wing hinges, and the ancestor-hide mutation reaches completion under the old local-visibility check.

- [ ] **Step 3: Split the Moth renderer at real hinge boundaries**

Keep `Wings` as a `Node2D` anchor at the existing pivot. Generate two child `EnemyFormalVisualPart` nodes named `FarWing` and `NearWing` with actor-specific kinds, deterministic z-order around `Body`, positive uniform scale, and zero local hinge pivots. Each renderer draws only its own side. `GraveLampMothFormalVisual` applies shared translation at `Wings` and equal-magnitude opposite-sign rotations to the two hinge children for hover, Telegraph fold, Active dive, Recovery, Hurt, and Dead states. Compatibility wing diagnostics derive from the actual child transforms; capture samples both hinge slots rather than a shadow pose.

- [ ] **Step 4: Make target presence fail closed for the visible tree**

For every already declared required target lifecycle, require:

```csharp
marker.Visible && marker.IsVisibleInTree() && renderedTargets.Count > 0
```

Then retain the existing point-by-point diamond/arrow safe-bounds checks and nonzero sample count per lifecycle. The ancestor-hide mutation must now fail during `thread-lesson`; optional windows remain ungated. Restore the environment and require all seven target lifecycles green.

- [ ] **Step 5: Regenerate, inspect, and run complete GREEN gates**

Run `BuildEnemyFormalVisuals.cs`, then the three ordinary-enemy parent builders without staging unrelated parent-scene churn. Inspect the Moth leaf semantic diff for the stable `Wings` anchor, the two typed hinge children, owner-chain completeness, and z-order `Corruption < FarWing < Body < NearWing < Lamp`.

Run build, the full `RunPrologueTests`, production audit, corrected 60 FPS presentation, and both AVI/PNG capture paths. Require 0 build warnings/errors, exactly one `PROLOGUE_TESTS_OK`, process-frame budget `< 50 ms`, 11/11 motion markers, 7/7 target lifecycles, and no hard wing pop or hidden-target false green. Automated evidence is not human taste acceptance.

- [ ] **Step 6: Commit**

```powershell
git add scripts/presentation/EnemyFormalVisualPart.cs scripts/presentation/GraveLampMothFormalVisual.cs tools/build/art/BuildEnemyFormalVisuals.cs scenes/presentation/GraveLampMothFormalVisual.tscn tests/integration/EnemyFormalVisualTests.cs tests/presentation/ProloguePresentation.cs tests/presentation/PrologueVisualAudit.cs
git commit -m "fix: preserve moth tell and target visibility"
```

---

### Task 1: Authoritative airborne-cycle dash consumption

**Files:**
- Modify: `scripts/player/PlayerMotionState.cs`
- Modify: `scripts/player/PlayerMotionResult.cs`
- Modify: `scripts/player/PlayerMotionModel.cs`
- Modify: `scripts/player/PlayerMotor.cs`
- Modify: `tests/unit/PlayerMotionModelTests.cs`
- Create: `tests/integration/PlayerTraversalInputTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: `PlayerMotionResult(..., bool DashRejected, ...)` for one-frame accepted/rejected input feedback.
- Produces: `PlayerMotor.AirDashAvailable`, `PlayerMotor.AcceptedDashCount`, and `PlayerMotor.RejectedDashCount` as read-only diagnostics for presentation/tests.
- Preserves: `PlayerMotor.ExternalImpulse(Vector2)` and `ResetTransientState()` without granting an airborne dash.

- [ ] **Step 1: Add unit RED cases for order-independent combo input**

Add these named cases to `PlayerMotionModelTests.Run()` and implement them with literal input frames:

```csharp
SpaceUpDashThreadThenDashRejectsSecondDashBeforeLanding();
SimultaneousSpaceUpDashThreadThenDashRejectsSecondDashBeforeLanding();
ThreadImpulseDashThenDashRejectsSecondDashBeforeLanding();
WallCeilingPauseAttackAndBounceNeverRefundAirDash();
AirborneRoomTransitionWithoutFloorContactNeverRefundsAirDash();
AuthoritativeLandingRefundsExactlyOneAirDash();
```

For every no-landing sequence, assert one `StartedDash`, one later `DashRejected`, `AirDashAvailable == false`, and unchanged velocity on the rejected press. The landing case must present `onFloor: true` after at least one airborne step and assert the next airborne dash succeeds exactly once.

- [ ] **Step 2: Run RED and record the missing diagnostic contract**

Run:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --nologo
```

Expected RED: compilation fails because `DashRejected` and read-only dash diagnostics do not yet exist. Preserve the relevant compiler output in the task report.

- [ ] **Step 3: Implement one airborne-cycle latch**

Keep `AirDashAvailable` false after the first airborne dash and only restore it from a real `onFloor && !state.WasOnFloor` landing edge (initial grounded initialization may seed availability once). A rejected dash returns the pre-input velocity and does not start presentation state:

```csharp
bool dashRequested = input.DashPressed;
bool canStartDash = dashRequested && (onFloor || state.AirDashAvailable);
bool dashRejected = dashRequested && !canStartDash;

if (canStartDash)
{
    state.DashDirection = ResolveDashDirection(input, state);
    state.DashRemaining = _settings.DashSeconds;
    if (!onFloor) state.AirDashAvailable = false;
    state.Velocity = state.DashDirection * _settings.DashSpeed;
    return new PlayerMotionResult(state.Velocity, false, true, false, landed);
}

return new PlayerMotionResult(state.Velocity, startedJump, false, dashRejected, landed);
```

`ExternalImpulse`, thread callbacks, attack callbacks, pause, and non-floor collision paths must not write `AirDashAvailable`. `ResetTransientState` may reinitialize only after a confirmed spawn/recovery placement; the room-transition runtime test below guards against using it during ordinary airborne travel.

- [ ] **Step 4: Add real InputMap RED/GREEN coverage**

Create `PlayerTraversalInputTests.Run(SceneTree tree)` and instantiate the real player scene in a real collision fixture. Drive actions with `Input.ActionPress/Release`; subscribe only to the `PlayerMotor` diagnostics. Reproduce these sequences at 60 Hz:

```text
Space → W+Shift → K → Shift
Space+W+Shift+K in one physics frame → release → Shift
Space → K → Shift → Shift
air dash → thread Apply → Shift
air dash → pause/resume → Shift
air dash → wall/ceiling/side collision → Shift
air dash → down-strike bounce/knockback → Shift
air dash → room transition to a no-floor entrance → Shift
```

Each sequence asserts `AcceptedDashCount == 1`, `RejectedDashCount >= 1`, and no second transition into a successful dash before real landing. Then land on a real `StaticBody2D`, jump again, and assert one new dash succeeds.

- [ ] **Step 5: Run GREEN and refund mutation**

Run the full prologue runner. Then temporarily set `AirDashAvailable = true` in `ExternalImpulse`; the named thread-after-dash test must fail. Restore the production code and rerun GREEN.

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs --log-file traversal-task1.log
```

Require exit 0 and exactly one `PROLOGUE_TESTS_OK`.

- [ ] **Step 6: Commit**

```powershell
git add scripts/player/PlayerMotionState.cs scripts/player/PlayerMotionResult.cs scripts/player/PlayerMotionModel.cs scripts/player/PlayerMotor.cs tests/unit/PlayerMotionModelTests.cs tests/integration/PlayerTraversalInputTests.cs tests/RunPrologueTests.cs
git commit -m "fix: enforce one air dash per airborne cycle"
```

---

### Task 2: Typed room traversal contracts and honest upper route

**Files:**
- Create: `scripts/prologue/traversal/TraversalSegmentKind.cs`
- Create: `scripts/prologue/traversal/TraversalAbility.cs`
- Create: `scripts/prologue/traversal/TraversalSegment2D.cs`
- Modify: `tools/build/prologue/PrologueBuildParts.cs`
- Modify: `tools/build/prologue/BuildAwakeningCourtyard.cs`
- Modify: `tools/build/prologue/BuildBrokenToothCorridor.cs`
- Modify: `tools/build/prologue/BuildUpperPendulum.cs`
- Modify: `tools/build/prologue/BuildLowerBuriedMachinery.cs`
- Modify: `tools/build/prologue/BuildBellChainRoom.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeperArena.cs`
- Modify: `tests/integration/PrologueEnvironmentStructureTests.cs`
- Modify: `tests/RunPrologueTests.cs`
- Regenerate: `scenes/prologue/rooms/AwakeningCourtyard.tscn`
- Regenerate: `scenes/prologue/rooms/BrokenToothCorridor.tscn`
- Regenerate: `scenes/prologue/rooms/UpperPendulum.tscn`
- Regenerate: `scenes/prologue/rooms/LowerBuriedMachinery.tscn`
- Regenerate: `scenes/prologue/rooms/BellChainMechanism.tscn`
- Regenerate: `scenes/prologue/rooms/FallenBellkeeperArena.tscn`

**Interfaces:**
- Produces: `TraversalSegmentKind { SingleJump, Assisted, OptionalAdvanced }`.
- Produces: `[Flags] TraversalAbility { None = 0, Jump = 1, Thread = 2, Dash = 4 }`.
- Produces: `TraversalSegment2D` with exported `SegmentId`, `Kind`, `RequiredAbilities`, `LaunchZone`, `LandingZone`, and `AssistTargetPath`.
- Produces: a stable room-local root `TraversalContracts/<segment-id>`.

- [ ] **Step 1: Add structure RED tests**

Load all six generated rooms and assert unique non-empty segment IDs, positive launch/landing rectangles, and legal ability masks:

```csharp
SingleJump        => RequiredAbilities == TraversalAbility.Jump &&
                     (AssistTargetPath == null || AssistTargetPath.IsEmpty),
Assisted          => RequiredAbilities == (TraversalAbility.Jump | TraversalAbility.Thread) &&
                     AssistTargetPath != null && !AssistTargetPath.IsEmpty,
OptionalAdvanced  => (RequiredAbilities & TraversalAbility.Dash) != 0,
```

For every assisted contract, resolve `AssistTargetPath` to a real `ThreadTarget2D` of kind `Anchor`. Assert the authored start-to-anchor distance is `<= 400f`, the target collision is on `CombatCollisionLayers.ThreadTarget`, and the anchor is inside the launch zone's declared facing half-plane.

- [ ] **Step 2: Run RED**

Run the full prologue runner. Expected RED: the typed traversal classes and `TraversalContracts` roots are absent.

- [ ] **Step 3: Add the typed builder helper**

Add a builder helper with no runtime gameplay behavior:

```csharp
public static TraversalSegment2D TraversalSegment(
    Node parent, string id, TraversalSegmentKind kind,
    TraversalAbility required, Rect2 launchZone, Rect2 landingZone,
    NodePath assistTargetPath = default)
{
    var segment = new TraversalSegment2D
    {
        Name = id,
        SegmentId = id,
        Kind = kind,
        RequiredAbilities = required,
        LaunchZone = launchZone,
        LandingZone = landingZone,
        AssistTargetPath = assistTargetPath
    };
    parent.AddChild(segment);
    return segment;
}
```

`TraversalSegment2D.Validate(Node room)` returns named errors for empty IDs, non-positive rectangles, illegal masks, missing/non-anchor assist targets, and Dash on a mandatory segment.

- [ ] **Step 4: Author the mandatory route manifest**

Create `TraversalContracts` in every room. The required first pass is:

| Room | Segment ID | Kind | Required abilities |
| --- | --- | --- | --- |
| AwakeningCourtyard | `spawn_to_jump_step` | SingleJump | Jump |
| AwakeningCourtyard | `jump_step_to_attack_ledge` | SingleJump | Jump |
| BrokenToothCorridor | `entry_to_anchor_landing` | Assisted | Jump + Thread (`Geometry/ThreadAnchor`) |
| UpperPendulum | `entry_to_lower_swing` | Assisted | Jump + Thread (`Geometry/AnchorLow`) |
| UpperPendulum | `lower_to_mid_swing` | Assisted | Jump + Thread (`Geometry/AnchorHigh`) |
| UpperPendulum | `mid_to_high_swing` | Assisted | Jump + Thread (`Geometry/ReboundTarget`) |
| UpperPendulum | `high_to_mechanism_landing` | SingleJump | Jump |
| LowerBuriedMachinery | `entry_to_arena` | SingleJump | Jump |
| BellChainMechanism | `arrival_to_chain_floor` | SingleJump | Jump |
| FallenBellkeeperArena | `retry_to_arena_floor` | SingleJump | Jump |

Use platform-top-aligned rectangles with at least 48 px horizontal width. For the upper route, use these first-pass centers while keeping existing stable node names:

```csharp
LowerSwing       = new Vector2(210, 120);
MidSwing         = new Vector2(610, -80);
HighSwing        = new Vector2(1010, -300);
MechanismLanding = new Vector2(1430, -300);
AnchorLow        = new Vector2(170, -90);
AnchorHigh       = new Vector2(500, -310);
ReboundTarget    = new Vector2(900, -530);
```

These values are not a license to weaken global movement tuning; Task 3's real route probe is the acceptance authority. If a declared assisted segment fails with one thread cast, move only that segment's platform/anchor/zone while retaining its stable paths.

- [ ] **Step 5: Rebuild rooms leaf-first and inspect semantic diffs**

Run each room builder with the installed Godot console, then rebuild `BuildPrologueLevel.cs`. For example:

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/prologue/BuildUpperPendulum.cs
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/prologue/BuildPrologueLevel.cs
```

Inspect every generated scene diff for stable platform/anchor paths, `TraversalContracts`, owner-chain completeness, and no unrelated visual/resource churn.

- [ ] **Step 6: Run GREEN and contract-removal mutation**

Run build and the full prologue runner. Temporarily omit `mid_to_high_swing` from the upper builder; the named room-manifest assertion must fail. Restore and rerun GREEN.

- [ ] **Step 7: Commit**

Stage only the three traversal types, builder changes, structure test, runner registration, and six semantically regenerated room scenes. Commit:

```powershell
git commit -m "feat: author first-level traversal contracts"
```

---

### Task 3: Real-physics route probes and first-use guidance

**Files:**
- Create: `tests/integration/PrologueTraversalRouteTests.cs`
- Modify: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceTargetKey.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceModel.cs`
- Modify: `tests/unit/PrologueGuidanceModelTests.cs`
- Modify: `tests/unit/PrologueGuidanceTargetResolverTests.cs`

**Interfaces:**
- Consumes: every `TraversalSegment2D` under the room-local `TraversalContracts` root.
- Produces: named output `PROLOGUE_TRAVERSAL_SEGMENT_OK room=<room> segment=<id> abilities=<mask>` from real-physics probes.
- Produces: one-time upper-room anchor guidance targeting `Geometry/AnchorLow` before the first assisted segment.

- [ ] **Step 1: Add route-probe RED tests**

For each `SingleJump` contract, sample launch X at 20%, 50%, and 80% of `LaunchZone`. Set the real player once at that launch point, then press only the declared horizontal direction and `Space`; do not press `Shift` or `K`. Within 90 physics frames, the player body center must enter `LandingZone` without kill-zone recovery.

For each `Assisted` contract, start at the 50% launch sample, press `Space`, aim toward the contract's resolved anchor, hold/release `K` once through the real `ThreadAbility`, and press no `Shift`. Within 150 physics frames, assert:

```csharp
threadApplyCount == 1;
motor.AcceptedDashCount == 0;
landingZone.HasPoint(player.GlobalPosition);
```

Use a fresh room/player fixture per sample so a preceding success cannot seed the next one.

- [ ] **Step 2: Run RED and classify failures**

Run `RunPrologueTests`. A failure must name room, segment, sample fraction, final position, thread state/failure reason, accepted dash count, and frame budget. Fix only the corresponding builder geometry/zone or the genuine dash-consumption bug; do not write velocity or expand global tuning.

- [ ] **Step 3: Add upper-room first-use guidance**

Add `UpperAnchorLow` to the target key/resolver and resolve it to `Geometry/AnchorLow`. Before the first upper assisted segment, show this one-time message:

```text
跳起后朝金色锚点按 K；Shift 只用于空中修正，每次滞空一次
```

After the first successful `entry_to_lower_swing` contract or once the existing tutorial completion flag is recorded, collapse back to the normal one-line objective. Guidance-off continues to suppress it; later rooms do not reuse the expanded first-level help.

- [ ] **Step 4: Make critical-path smoke traverse instead of teleporting the upper route**

Replace direct exit touching for the upper-room main path with the same real InputMap helper used by the integration probe. Emit exactly one segment marker for each mandatory segment and assert the upper route reaches `Exits/ToMechanism` without Dash input. Fixture positioning remains permitted only at room entry, never between upper segments.

- [ ] **Step 5: Run GREEN and input-removal mutations**

Run the full runner and `verify-prologue.ps1`. Then perform both mutations separately:

1. suppress the single `K` press on `entry_to_lower_swing` — its assisted-segment assertion must fail;
2. shrink a single-jump landing zone to zero width — validation must fail before the route probe runs.

Restore after each mutation and require:

```text
PROLOGUE_TESTS_OK exactly once
PROLOGUE_CRITICAL_PATH_OK present
PROLOGUE_VERIFY_OK present
```

- [ ] **Step 6: Commit**

```powershell
git add tests/integration/PrologueTraversalRouteTests.cs tests/smoke/PrologueCriticalPathSmoke.cs tests/RunPrologueTests.cs scripts/prologue/guidance/PrologueGuidanceTargetKey.cs scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs scripts/prologue/guidance/PrologueGuidanceModel.cs tests/unit/PrologueGuidanceModelTests.cs tests/unit/PrologueGuidanceTargetResolverTests.cs
git commit -m "test: prove honest first-level traversal"
```

---

### Task 4: Delivery gates, proof capture, and durable status

**Files:**
- Modify: `README.md`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tests/presentation/PrologueVisualAudit.cs`
- Modify: `tools/capture-prologue.ps1`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Consumes: Task 1 dash diagnostics and Task 2 traversal contracts.
- Produces: 1280×720@60 FPS proof frames/video covering rejected second dash, one-thread upper traversal, and failure cleanup.
- Produces: README capability contract and a six-room route table using `SingleJump`, `Assisted`, and `OptionalAdvanced` labels.

- [ ] **Step 1: Add capture/audit RED assertions**

Require the presentation capture to publish these ordered stages:

```text
TRAVERSAL_PROOF_STAGE=single_jump
TRAVERSAL_PROOF_STAGE=air_dash_consumed
TRAVERSAL_PROOF_STAGE=second_dash_rejected
TRAVERSAL_PROOF_STAGE=upper_anchor_low
TRAVERSAL_PROOF_STAGE=upper_anchor_high
TRAVERSAL_PROOF_STAGE=upper_rebound
TRAVERSAL_PROOF_STAGE=upper_landed
```

The audit fails if a stage is missing, out of order, uses more than one accepted dash per airtime, or an assisted stage has no visible anchor/thread line in consecutive frames.

- [ ] **Step 2: Capture normal-speed evidence**

Run at 1280×720 and fixed 60 FPS. The clip must show: one ordinary single jump; upward `W+Shift`; the second Shift rejection without a second burst; the full upper route using exactly one thread cast per assisted segment and no dash; one deliberate failed cast with named cleanup; and arrival at the mechanism exit.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1
```

Review consecutive frames for a false second-jump silhouette, hidden anchors, misleading target markers, thread clearing before landing, and guidance covering the launch/landing zones.

- [ ] **Step 3: Run the complete delivery gate twice**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --nologo
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrototypeTests.cs --log-file traversal-prototype.log
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs --log-file traversal-prologue.log
powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Require 0 build warnings/errors, success markers exactly once, no runtime `ERROR`/`SCRIPT ERROR`/`Parse Error`, and the same result on the second clean run.

- [ ] **Step 4: Update durable status**

Document in `README.md`:

- no double jump;
- one directional air dash per airborne cycle;
- discrete mechanical-thread pull, not continuous swinging;
- six-room traversal contract table and latest gate/capture paths;
- automatic evidence complete but normal-speed human traversal review still pending.

Keep the overall prologue goal active until the user completes a normal-speed playthrough and confirms no mandatory unknown combo, suspected double jump, or insufficient anchor pull remains.

- [ ] **Step 5: Commit**

```powershell
git add README.md tests/presentation/ProloguePresentation.cs tests/presentation/PrologueVisualAudit.cs tools/capture-prologue.ps1 tools/verify-prologue.ps1
git commit -m "docs: deliver first-level traversal proof"
```
