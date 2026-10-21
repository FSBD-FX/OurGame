# Terrain Grapple and Dense Canyon Revision Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Amend the implemented 11-task canyon prologue so the mechanical claw raycasts real typed terrain, attached Space performs one controlled boost instead of detaching, the five continuous sectors contain fifteen dense micro-scenes, and every fall can be diagnosed from local hands-on evidence.

**Architecture:** Preserve the existing `GrappleSurface2D` event boundary by attaching a profile node to typed terrain collision bodies and locking the ray hit point in `MechanicalClawTargetSnapshot`; dedicated anchors and legacy enemy/mechanism receivers continue to use point anchors. Extend the pure claw frame with one-boost-per-latch state, rebuild sector content from a catalog of fifteen micro-scenes, and migrate playtest reports to version 3 with exact fall context before refreshing verification and capture evidence.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, Godot 2D physics ray queries, generated `.tscn` scenes, pure C# state models, PowerShell verification, deterministic 1280×720 at 60 FPS capture.

**Spec:** `docs/superpowers/specs/2026-08-26-keyboard-mouse-grapple-canyon-prologue-design.md`

**Amends:** Tasks 5, 6, 8, 9, 10 and 11 of `docs/superpowers/plans/2026-08-26-keyboard-mouse-grapple-canyon-prologue.md`; Tasks 1–4 and 7 remain compatible and are regression gates.

## Global Constraints

- Run the project only with `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe` and build with `G:\godot\dotnet-sdk\dotnet.exe`; keep Godot 4.7.1 and `.NET 9` unchanged.
- Work in `E:\OurGame\Knight\.worktrees\player-art-pipeline`; preserve all unrelated dirty files and stage only the paths named by the active task.
- Do not kill or reuse an unrelated running Godot process. Test and capture processes must have their own PID and bounded exit handling.
- Keep `project.godot`, generated player/level scenes and environment kit under the verifier's snapshot/restore contract; every builder must pack, instantiate and compare its owned output before GREEN.
- Gameplay collision, surface type and traversal contracts stay under `Gameplay`; later art replaces only stable `VisualRoot/{Background,Midground,GameplayReadable,Foreground,Effects}` subtrees.
- Do not generate, purchase or import formal art. Programmatic placeholders and existing project-owned assets are allowed.
- Main-route completion must not require double jump, air dash, a perfect chained swing or pixel-accurate cursor placement.
- Automated acceptance requires two consecutive fresh `tools/verify-prologue.ps1` runs, fresh 1280×720/60 FPS full-flow and Boss-retry proof review, build 0 warnings/errors, no fatal/error/leak signature and callback peak below the existing 50 ms limit.
- Human completion remains open until a genuine no-long-idle `HandsOnLauncher` report completes in 10–15 minutes and records the least smooth or least clear moment.

---

### Task 1: One Attached-Space Boost and 620-Pixel Claw Model

**Files:**
- Modify: `scripts/abilities/MechanicalClawState.cs`
- Modify: `scripts/abilities/MechanicalClawSettings.cs`
- Modify: `scripts/abilities/MechanicalClawInput.cs`
- Modify: `scripts/abilities/MechanicalClawFrame.cs`
- Modify: `scripts/abilities/MechanicalClawModel.cs`
- Modify: `tests/unit/MechanicalClawModelTests.cs`
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Produces state `MechanicalClawState.Boost` between attached and release states.
- Produces `MechanicalClawInput(bool PrimaryPressed, bool PrimaryHeld, bool PrimaryReleased, bool DetachPressed, float SwingAxis, float ReelAxis, bool ShieldActive, bool BoostPressed = false, bool Grounded = false)`; appending optional fields keeps existing runtime constructors source-compatible until Task 2 routes real input.
- Produces `MechanicalClawFrame(..., ulong NextCastId = 1UL, bool BoostConsumed = false)`; a fresh latch and a grounded attached frame set the final field to `false`, one accepted boost sets it to `true`.
- Produces `MechanicalClawSettings(float MaxRange, float MainAnchorAssistRadius, float MinimumRopeLength, float ReelSpeed, float SwingAcceleration, float RadialStiffness, float RadialDamping, float ShuttleSpeed, float RetractSpeed, float BoostUpwardSpeed, float BoostTangentialSpeed)`; changes only `MaxRange` from `520f` to `620f`, adds `360f/220f` boost speeds and keeps assist radius `36f`.

- [ ] **Step 1: Write the attached-boost RED tests**

Add these named cases to `MechanicalClawModelTests.Run()` and implement their bodies with the existing `EnterLatched`, `Target` and `TestAssert` helpers:

```csharp
AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity();
SecondSpaceOnTheSameAirborneLatchDoesNotAddVelocity();
GroundContactOrRelatchRestoresExactlyOneBoost();
ShieldedSpaceDoesNotBoostOrConsumeTheBoost();
MaximumRangeIncludesExactlySixHundredTwentyPixels();
RangeAboveSixHundredTwentyFailsClosed();
```

The first case must retain `CastId`, `LockedTargetRuntimeId`, `PrimaryHeld=true` and an attached state while asserting `VelocityCorrection.Y < before.Y`. The second must compare the complete correction before/after the second Space edge. The reset case must prove both a grounded attached frame and a new cast restore one acceptance. Replace all 520/520.01 boundary assertions with 620/620.01.

- [ ] **Step 2: Run RED and preserve its exact reason**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
```

Expected: compile failure because `Boost`, `BoostPressed`, `Grounded`, `BoostConsumed`, `BoostUpwardSpeed` and `BoostTangentialSpeed` do not yet exist. Save the console output under `artifacts/task11-revision/red/task1-claw-model.log` without staging it.

- [ ] **Step 3: Add the immutable boost fields and validated tuning**

Use this exact record shape and defaults, retaining the existing fields in their current order before the new values:

```csharp
public readonly record struct MechanicalClawInput(
    bool PrimaryPressed,
    bool PrimaryHeld,
    bool PrimaryReleased,
    bool DetachPressed,
    float SwingAxis,
    float ReelAxis,
    bool ShieldActive,
    bool BoostPressed = false,
    bool Grounded = false);

// MechanicalClawSettings.Default additions
MaxRange: 620f,
BoostUpwardSpeed: 360f,
BoostTangentialSpeed: 220f
```

Validate `MaxRange == 620f`, finite positive boost speeds and the unchanged `MainAnchorAssistRadius == 36f`. Add `BoostConsumed=false` to `MechanicalClawFrame.Idle()` and clear it in terminal Idle.

- [ ] **Step 4: Implement deterministic boost composition**

Treat `Boost` as attached in `Step`. In `StepAttached`, reset the consumed flag when `input.Grounded`, then apply an accepted unshielded edge after the normal radial/tangential correction:

```csharp
Vector2 toAnchor = previous.CasterWorldPosition.DirectionTo(previous.AnchorPoint);
Vector2 tangent = new(-toAnchor.Y, toAnchor.X);
float projected = previous.CasterVelocity.Dot(tangent);
float requested = Mathf.Abs(input.SwingAxis) > Epsilon ? input.SwingAxis : Mathf.Sign(projected);
float direction = Mathf.IsZeroApprox(requested) ? 1f : Mathf.Sign(requested);
bool acceptsBoost = input.BoostPressed && !input.ShieldActive && !boostConsumed;
if (acceptsBoost)
{
    velocityCorrection += Vector2.Up * _settings.BoostUpwardSpeed +
        tangent * (direction * _settings.BoostTangentialSpeed);
    boostConsumed = true;
    state = MechanicalClawState.Boost;
}
```

Do not call `ToReleased` for `BoostPressed`. Release remains owned exclusively by `PrimaryReleased` or `DetachPressed`.

- [ ] **Step 5: Run GREEN and three mutation checks**

Run build and the prototype runner:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrototypeTests.cs --log-file artifacts/task11-revision/task1-prototype.log
```

Expected: build 0 warnings/errors and `PROTOTYPE_TESTS_OK`. Under byte snapshot/finally restoration, separately mutate boost consumption, shield rejection and 620 inclusivity; each named test must fail, and the final SHA-256 of every mutated source must match its pre-mutation hash.

- [ ] **Step 6: Commit the pure-model slice**

```powershell
git add scripts/abilities/MechanicalClawState.cs scripts/abilities/MechanicalClawSettings.cs scripts/abilities/MechanicalClawInput.cs scripts/abilities/MechanicalClawFrame.cs scripts/abilities/MechanicalClawModel.cs tests/unit/MechanicalClawModelTests.cs tests/RunPrototypeTests.cs
git commit -m "feat: add one-shot attached claw boost"
```

---

### Task 2: Typed Terrain Raycast, Runtime Jump Arbitration and Real Hit Anchors

**Files:**
- Modify: `scripts/abilities/GrappleSurface2D.cs`
- Modify: `scripts/abilities/MechanicalClawTargetResolver.cs`
- Modify: `scripts/abilities/MechanicalClawController.cs`
- Modify: `scripts/player/PlayerMotor.cs`
- Modify: `scripts/ui/PrologueFormalHud.cs`
- Modify: `scripts/vfx/ThreadLineVisual.cs`
- Modify: `scripts/vfx/ThreadShuttleVisual.cs`
- Modify: `tests/unit/GrappleSurfaceTests.cs`
- Modify: `tests/integration/MechanicalClawRuntimeTests.cs`
- Modify: `tests/integration/PlayerTraversalInputTests.cs`
- Modify: `tests/integration/ThreadInteractionTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Adds `GrappleSurface2D.UseCollisionHitPoint`; `false` keeps a dedicated point anchor, `true` makes the parent collision body's ray hit the locked anchor.
- Keeps `MechanicalClawController.CurrentTarget` and `ClawFrameChanged` typed as `GrappleSurface2D`, avoiding churn in audio, VFX, presentation and telemetry consumers.
- Changes `MechanicalClawTargetResolver.SnapshotLocked(Node2D caster, GrappleSurface2D surface, Vector2 lockedAnchor)` so terrain locks preserve the original hit point.
- Adds `PlayerMotor.SuppressJumpForCurrentPhysicsFrame()`; it suppresses only the ordinary motor jump consumed by an attached claw boost and clears itself after one motor step.

- [ ] **Step 1: Write terrain-ray and input-arbitration RED fixtures**

Build runtime fixtures with a `StaticBody2D` containing `CollisionShape2D` plus this child profile:

```csharp
var profile = new GrappleSurface2D
{
    Name = "GrappleSurfaceProfile",
    Material = GrappleSurfaceMaterial.GripStone,
    UseCollisionHitPoint = true
};
terrain.CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget;
terrain.AddChild(profile);
```

Add tests for wall face, ceiling underside, platform underside, platform edge, TempleMetal, Ice, Forbidden, an untyped obstruction, exact 620 range, cursor beyond range and a dedicated anchor inside 36-pixel assist. Assert terrain `Snapshot.WorldAnchor` equals the physics hit position rather than `profile.WorldAnchor`.

Add a scheduled real-input test that holds LMB on a live latch and presses Space. It must assert: the same cast remains attached, line and shuttle remain visible, vertical velocity increases, `PlayerMotor` accepts no ordinary second jump, a second Space edge adds no boost, releasing LMB still publishes `Released`.

- [ ] **Step 2: Run focused RED**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
```

Expected: compile failures for `UseCollisionHitPoint`, the new locked-snapshot signature and jump suppression. Store output at `artifacts/task11-revision/red/task2-terrain-runtime.log`.

- [ ] **Step 3: Resolve dedicated anchors first, then the first typed terrain hit**

Keep the existing 36-pixel shape query for point anchors where `UseCollisionHitPoint == false`. If no valid assisted anchor wins, raycast from caster to cursor with `World | ThreadTarget`; accept only the first visible collider and resolve its profile by walking ancestors and checking `GrappleSurfaceProfile` children:

```csharp
private static GrappleSurface2D FindSurface(Node collider)
{
    for (Node current = collider; current != null; current = current.GetParent())
    {
        if (current is GrappleSurface2D point) return point;
        GrappleSurface2D profile = current.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
        if (GodotObject.IsInstanceValid(profile)) return profile;
    }
    return null;
}
```

For terrain profiles, use `result["position"].AsVector2()` as `WorldAnchor`; for point anchors, use `surface.WorldAnchor`. All gameplay collision bodies encountered by the revised canyon must carry an explicit profile, including Ice and Forbidden bodies, so the reticle never guesses from node name or art.

- [ ] **Step 4: Route Space to boost without ordinary jump or retract**

Replace the controller's current jump retract condition with release-only logic and construct the new model input:

```csharp
if (active && (sampled.PrimaryReleased || !sampled.PrimaryHeld))
    _retractRequested = true;
bool attached = previous.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
    MechanicalClawState.Swing or MechanicalClawState.Boost;
if (attached && sampled.JumpPressed) _motor.SuppressJumpForCurrentPhysicsFrame();
var modelInput = new MechanicalClawInput(
    PrimaryPressed: routedPress,
    PrimaryHeld: sampled.PrimaryHeld,
    PrimaryReleased: active && sampled.PrimaryReleased,
    DetachPressed: active && _retractRequested,
    SwingAxis: sampled.MoveAxis,
    ReelAxis: shieldActive ? 0f : sampled.Aim.Y,
    ShieldActive: shieldActive,
    BoostPressed: attached && sampled.JumpPressed,
    Grounded: _player.IsOnFloor());
```

In `PlayerMotor._PhysicsProcess`, rewrite only `JumpPressed=false` and `JumpHeld=false` when the suppression flag is set, then clear the flag in `finally`. Q mode switching, damage, death, target unload and scene exit still request safe retract.

- [ ] **Step 5: Preserve reticle, line, shuttle and legacy receivers**

Treat `Boost` as attached anywhere the current code recognizes `Latched/Reel/Swing`. `PrologueFormalHud` must keep Ice distinct, show Forbidden/obstruction with the blocked geometry, show range failures with the range ring and show a valid terrain profile as Grippable. Light enemy pull, heavy enemy exposure and Bell Chain activation continue through `LegacyThreadInteractionAdapter` and never create a persistent terrain rope.

- [ ] **Step 6: Run GREEN and obstruction/identity mutations**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs --log-file artifacts/task11-revision/task2-prologue.log
```

Expected: `PROLOGUE_TESTS_OK`, no accepted second ordinary air jump and no residual rope. Mutation checks must fail if the ray ignores the first blocker, if a terrain lock recomputes its anchor after latch, or if Space again sets `_retractRequested`; restore exact bytes after each mutation.

- [ ] **Step 7: Commit the terrain-runtime slice**

```powershell
git add scripts/abilities/GrappleSurface2D.cs scripts/abilities/MechanicalClawTargetResolver.cs scripts/abilities/MechanicalClawController.cs scripts/player/PlayerMotor.cs scripts/ui/PrologueFormalHud.cs scripts/vfx/ThreadLineVisual.cs scripts/vfx/ThreadShuttleVisual.cs tests/unit/GrappleSurfaceTests.cs tests/integration/MechanicalClawRuntimeTests.cs tests/integration/PlayerTraversalInputTests.cs tests/integration/ThreadInteractionTests.cs tests/RunPrologueTests.cs
git commit -m "feat: grapple typed terrain at real hit points"
```

---

### Task 3: Fifteen Dense Micro-Scenes, Recovery Layers and Claw Mechanisms

**Files:**
- Create: `tools/build/prologue/CanyonMicroSceneDefinition.cs`
- Create: `tools/build/prologue/CanyonContentCatalog.cs`
- Create: `tools/build/prologue/CanyonContentBuilder.cs`
- Create: `scripts/prologue/mechanisms/CanyonClawMechanismKind.cs`
- Create: `scripts/prologue/mechanisms/CanyonClawMechanism2D.cs`
- Modify: `tools/build/prologue/PrologueCanyonBuildParts.cs`
- Modify: `tools/build/prologue/BuildCanyonEntrance.cs`
- Modify: `tools/build/prologue/BuildCanyonDescent.cs`
- Modify: `tools/build/prologue/BuildCanyonBasin.cs`
- Modify: `tools/build/prologue/BuildCanyonAscent.cs`
- Modify: `tools/build/prologue/BuildLostTemple.cs`
- Modify generated: `scenes/prologue/sectors/CanyonEntrance.tscn`
- Modify generated: `scenes/prologue/sectors/CanyonDescent.tscn`
- Modify generated: `scenes/prologue/sectors/CanyonBasin.tscn`
- Modify generated: `scenes/prologue/sectors/CanyonAscent.tscn`
- Modify generated: `scenes/prologue/sectors/LostTemple.tscn`
- Modify generated: `scenes/prologue/PrologueLevel.tscn`
- Modify: `tests/unit/PrologueCanyonStructureTests.cs`
- Modify: `tests/integration/PrologueCanyonTraversalTests.cs`
- Modify: `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`
- Create: `tests/integration/CanyonClawMechanismTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces exactly 15 `CanyonMicroSceneDefinition(Id, SectorId, LocalBounds, OptionalSubarea)` entries and matching `Gameplay/MicroScenes/<Id>` markers.
- Produces `CanyonClawMechanism2D.Activated`, `bool TryReceiveThread(ThreadInteraction interaction)` and one-shot kinds `Counterweight`, `CargoPlatform`, `HiddenPanel`, `BellBody`; the first valid interaction returns `true`, a repeated interaction returns `false` without moving the linked node again.
- Every generated gameplay body owns `GrappleSurfaceProfile` with explicit material; formal art never owns collision or claw rules.
- Keeps the same five sector IDs, continuous world offsets, two checkpoints, Boss scene and stable visual-root paths.

- [ ] **Step 1: Write the content-density and recovery RED contracts**

Define the exact catalog:

```csharp
new("BrokenBridgeLookout", CanyonEntrance, new Rect2(0, 0, 360, 720), false),
new("SuspendedYard", CanyonEntrance, new Rect2(360, 0, 430, 720), false),
new("CollapsedGate", CanyonEntrance, new Rect2(790, 0, 490, 720), false),
new("CraneShaft", CanyonDescent, new Rect2(0, 0, 640, 500), false),
new("FrostVent", CanyonDescent, new Rect2(520, 360, 760, 440), false),
new("BrokenLift", CanyonDescent, new Rect2(300, 760, 980, 680), false),
new("ArchiveAlcove", CanyonDescent, new Rect2(0, 980, 360, 300), true),
new("GearGraveyard", CanyonBasin, new Rect2(0, 0, 520, 720), false),
new("SteamPumpTrench", CanyonBasin, new Rect2(520, 0, 580, 720), false),
new("CheckpointCampSalvage", CanyonBasin, new Rect2(1100, 0, 500, 720), false),
new("CounterweightWell", CanyonAscent, new Rect2(0, 900, 480, 540), false),
new("ScaffoldPendulumGallery", CanyonAscent, new Rect2(320, 360, 700, 900), false),
new("CeilingRailHighRuin", CanyonAscent, new Rect2(760, 0, 520, 560), false),
new("OuterSealGuardWorkshop", LostTemple, new Rect2(0, 0, 760, 720), false),
new("BellAntechamberBossHall", LostTemple, new Rect2(760, 0, 1160, 720), false)
```

Tests must require all IDs exactly once, every marker inside its sector, all five visual roots, required landing widths `>=180f`, necessary grapple distances in `[360f,520f]`, no mandatory Dash, and a catch platform within 320 vertical pixels below every taught or required grapple launch zone. `ArchiveAlcove`, the salvage subsection and high ruin must be skippable without blocking completion.

- [ ] **Step 2: Write real traversal and mechanism RED tests**

Replace the old attached-Space assertion that expected release. Add physical probes for wall latch, ceiling latch plus Space boost, reel, ice rejection, falling self-rescue, three ascent combinations and return from each catch layer. Each mainline probe starts from three positions, performs real `E`, LMB, A/D, W/S and Space input, uses zero Dash, lands for 12 stable frames and retains at least 15% horizontal/vertical margin inside the landing zone.

For mechanisms, instantiate each kind, send one real adapter interaction and assert exactly one activation, deterministic linked-node displacement and no second displacement on a repeated cast.

- [ ] **Step 3: Run RED against the sparse canyon**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs --log-file artifacts/task11-revision/red/task3-dense-canyon.log
```

Expected: missing catalog/mechanism types and named density, terrain-profile, boost-route and recovery-layer failures.

- [ ] **Step 4: Implement the catalog and focused content builder**

Move sector-specific geometry out of the unwieldy `PrologueCanyonBuildParts` methods into `CanyonContentBuilder`; leave scene packing, common roots, seams, fall volumes and save validation in `PrologueCanyonBuildParts`. The platform helper must attach a typed profile:

```csharp
private static StaticBody2D Platform(Node parent, string name, Vector2 center,
    Vector2 size, GrappleSurfaceMaterial material, bool oneWay = false)
{
    var body = new StaticBody2D
    {
        Name = name,
        Position = center,
        CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget
    };
    body.AddChild(new GrappleSurface2D
    {
        Name = "GrappleSurfaceProfile",
        Material = material,
        UseCollisionHitPoint = true
    });
    var shape = new CollisionShape2D
    {
        Name = "CollisionShape2D",
        Shape = new RectangleShape2D { Size = size },
        OneWayCollision = oneWay
    };
    body.AddChild(shape);
    parent.AddChild(body);
    return body;
}
```

Every visible gameplay body passes `GripStone`, `TempleMetal`, `Ice` or `Forbidden` explicitly. Dedicated anchors remain point targets and are limited to tutorial, mechanism and Boss-critical support.

- [ ] **Step 5: Populate encounters, loops and four claw uses**

Use existing enemy scenes rather than new enemy families: one low-pressure Rust-Shell in Entrance, mixed Rust-Shell/Laborer pressure in Basin, one Moth sightline in Ascent and existing Temple/Boss encounters. Place Counterweight in Descent, CargoPlatform in Basin, HiddenPanel in ArchiveAlcove and BellBody in Temple. Mainline uses the claw for movement, recovery and a mechanism; light-enemy displacement is demonstrated once without making it a completion gate.

Required path remains readable through `GameplayReadable` placeholders and moving machinery. Optional areas add recovery/world information only. Do not extend the mandatory route by duplicating combat waves.

- [ ] **Step 6: Build leaf scenes and run structural GREEN**

Run the five leaf builders, then the main level builder and the prologue suite:

```powershell
$godot='G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --headless --path . --script res://tools/build/prologue/BuildCanyonEntrance.cs
& $godot --headless --path . --script res://tools/build/prologue/BuildCanyonDescent.cs
& $godot --headless --path . --script res://tools/build/prologue/BuildCanyonBasin.cs
& $godot --headless --path . --script res://tools/build/prologue/BuildCanyonAscent.cs
& $godot --headless --path . --script res://tools/build/prologue/BuildLostTemple.cs
& $godot --headless --path . --script res://tools/build/prologue/BuildPrologueLevel.cs
& $godot --headless --path . --script res://tests/RunPrologueTests.cs --log-file artifacts/task11-revision/task3-prologue.log
```

Expected: every builder reports packed/validated node counts and the suite ends `PROLOGUE_TESTS_OK` plus real canyon traversal success. Compare and restore protected generated outputs exactly according to the verifier contract before staging.

- [ ] **Step 7: Commit the dense-map slice**

```powershell
git add tools/build/prologue/CanyonMicroSceneDefinition.cs tools/build/prologue/CanyonContentCatalog.cs tools/build/prologue/CanyonContentBuilder.cs scripts/prologue/mechanisms/CanyonClawMechanismKind.cs scripts/prologue/mechanisms/CanyonClawMechanism2D.cs tools/build/prologue/PrologueCanyonBuildParts.cs tools/build/prologue/BuildCanyonEntrance.cs tools/build/prologue/BuildCanyonDescent.cs tools/build/prologue/BuildCanyonBasin.cs tools/build/prologue/BuildCanyonAscent.cs tools/build/prologue/BuildLostTemple.cs tests/unit/PrologueCanyonStructureTests.cs tests/integration/PrologueCanyonTraversalTests.cs tests/integration/PrologueCanyonGeneratedMainRouteTests.cs tests/integration/CanyonClawMechanismTests.cs tests/RunPrologueTests.cs
git commit -m "feat: build dense layered canyon micro-scenes"
```

---

### Task 4: First-Level Guidance, Reticle Semantics and Replaceable Content Feedback

**Files:**
- Modify: `assets/localization/prologue-ui.json`
- Modify: `scripts/prologue/guidance/PrologueGuidanceModel.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceController.cs`
- Modify: `scripts/ui/PrologueAimReticle.cs`
- Modify: `scripts/ui/PrologueFormalHud.cs`
- Modify: `scripts/audio/PrologueAudioDirector.cs`
- Modify: `scripts/vfx/PrologueVfxDirector.cs`
- Modify: `tests/unit/PrologueGuidanceModelTests.cs`
- Modify: `tests/integration/PrologueGuidanceRuntimeTests.cs`
- Modify: `tests/integration/PrologueFormalHudTests.cs`
- Modify: `tests/RunTask11UiTests.cs`

**Interfaces:**
- First-level claw instruction names `E`, LMB hold/release, `A/D`, `W/S` and attached Space boost; it never calls the boost double jump.
- `DescentGripStone` resolves to the generated terrain profile or its owning body, while `DescentIce` resolves to an explicit Ice profile.
- Adds `PrologueAimReticleState.Forbidden` with a geometry signature distinct from Ice, obstruction and range; feedback remains shape plus color plus sound.

- [ ] **Step 1: Write localization, target and reticle RED tests**

Require Simplified Chinese and English strings to contain every real binding token and no stale `J`, `K`, “Space disconnect”, `GripStone` implementation name or room-swap wording. Instantiate the generated Descent sector and require both guidance target keys to resolve live terrain nodes. Require six distinct reticle signatures: Attack, Grippable, Ice, Forbidden, Obstructed and OutOfRange.

- [ ] **Step 2: Run RED**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunTask11UiTests.cs --log-file artifacts/task11-revision/red/task4-ui.log
```

Expected: stale claw copy/target paths and missing Forbidden reticle signature fail by name.

- [ ] **Step 3: Implement approved copy and live targets**

Use this Chinese claw instruction, with glyph tokens substituted by the existing guidance controller:

```text
按 {grapple_mode} 切换机械爪，瞄准发亮的粗糙墙面、平台边缘或天花板并按住 {primary}。用 {move_left}/{move_right} 摆荡、{move_up}/{move_down} 收放绳；连接时按 {jump} 跃升一次，松开 {primary} 脱离。冰面和封闭表面不可抓。
```

The English string conveys the same operations without exposing internal material enum names. Guidance remains first-level-only and follows the existing user setting.

- [ ] **Step 4: Implement distinct no-grab feedback**

Add a blocked hex/crossbar geometry for Forbidden, retain crossed ice bars for Ice, dashed arc for Obstructed and dual ring for OutOfRange. Audio and VFX map Forbidden to a dry impact, Ice to slip/shards, Obstructed to blocked spark and range to no-impact pulse. Do not add new external audio files; use existing project-owned deterministic cues.

- [ ] **Step 5: Run GREEN at four resolutions**

Run Task 11 UI, complete prologue tests and the existing four-resolution UI capture:

```powershell
$godot='G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --headless --path . --script res://tests/RunTask11UiTests.cs --log-file artifacts/task11-revision/task4-ui.log
& $godot --headless --path . --script res://tests/RunPrologueTests.cs --log-file artifacts/task11-revision/task4-prologue.log
$env:KNIGHT_UI_CAPTURE_ROOT='res://artifacts/task11-revision/task4-ui-capture'
& $godot --headless --path . --script res://tests/presentation/PrologueUiResolutionCapture.cs --log-file artifacts/task11-revision/task4-ui-capture.log
Remove-Item Env:\KNIGHT_UI_CAPTURE_ROOT -ErrorAction SilentlyContinue
```

Expected: `TASK11_UI_READABILITY_OK`, `PROLOGUE_TESTS_OK` and `PROLOGUE_UI_RESOLUTION_OK`. At 1280×720, 1920×1080, 2560×1440 and 3840×2160 require complete wrapped guidance, visible reticle, no protected-region overlap and stable `Hud/FormalHud/AimReticle` path.

- [ ] **Step 6: Commit the feedback slice**

```powershell
git add assets/localization/prologue-ui.json scripts/prologue/guidance/PrologueGuidanceModel.cs scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs scripts/prologue/guidance/PrologueGuidanceController.cs scripts/ui/PrologueAimReticle.cs scripts/ui/PrologueFormalHud.cs scripts/audio/PrologueAudioDirector.cs scripts/vfx/PrologueVfxDirector.cs tests/unit/PrologueGuidanceModelTests.cs tests/integration/PrologueGuidanceRuntimeTests.cs tests/integration/PrologueFormalHudTests.cs tests/RunTask11UiTests.cs
git commit -m "feat: teach and present terrain grapple controls"
```

---

### Task 5: Version-3 Fall Context and Local Hands-On Diagnostics

**Files:**
- Modify: `scripts/prologue/ProloguePlaytestSession.cs`
- Modify: `scripts/prologue/ProloguePlaytestReport.cs`
- Modify: `scripts/prologue/ProloguePlaytestRecorder.cs`
- Modify: `tests/unit/ProloguePlaytestSessionTests.cs`
- Modify: `tests/unit/ProloguePlaytestEvidenceGrantTests.cs`
- Modify: `tests/integration/ProloguePlaytestRecorderTests.cs`
- Modify: `tests/powershell/Task4PlaytestRecorderTransition.Tests.ps1`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces `ProloguePointSample(float X, float Y)` and `PrologueInputEdgeSample(double SecondsBeforeRecovery, string Action)`.
- Produces `PrologueFallSample(string Reason, string Sector, ProloguePointSample WorldPosition, string LastGroundedPlatformId, string ActiveCheckpoint, string ClawState, string ClawTarget, string ClawMaterial, string ClawFailure, IReadOnlyList<PrologueInputEdgeSample> RecentInputs)`.
- Adds `IReadOnlyList<PrologueFallSample> Falls` to report version 3; v1/v2 migration yields an empty list without inventing coordinates.

- [ ] **Step 1: Write report-v3 and evidence-boundary RED tests**

Create a session, record one fall at `(1304.5f, 702.25f)` in `CanyonDescent`, last support `Gameplay/Platforms/GripCatchShelf`, no activated checkpoint, attached GripStone target and recent `E`, `LMB_DOWN`, `SPACE` edges. Round-trip JSON and Markdown and assert every exact field. Load a real v2 JSON fixture and require `Version==3` with `Falls.Count==0`.

The headless/editor/capture boundary must still produce no `HandsOnLauncher` report. No automated test writes human-note fields.

- [ ] **Step 2: Run RED**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
```

Expected: missing fall sample records, report field and v3 migration.

- [ ] **Step 3: Sample last support and a bounded input-edge window**

In `ProloguePlaytestRecorder.ObserveRuntime`, when `Player.IsOnFloor()`, inspect slide collisions whose normal has `Y < -0.5f` and store the collider's scene path. Maintain a queue of real input edges from `PlayerInputReader.CurrentFrame`; keep only entries whose timestamp is within 1.5 seconds of current session time and encode only these stable names:

```text
Q, E, LMB_DOWN, LMB_UP, RMB_DOWN, RMB_UP, SPACE, SHIFT, A_DOWN, A_UP, D_DOWN, D_UP, W_DOWN, W_UP, S_DOWN, S_UP
```

Do not record continuous mouse coordinates or background idle frames.

- [ ] **Step 4: Capture fall context before recovery mutates state**

At `RecoveryRequested`, build `PrologueFallSample` before the level resets the player, claw or checkpoint. Read current sector, player global position, stored support ID, activated checkpoint, current claw frame, current target name/material and recent edges; then call `RecordDeath`, `RecordFall` and `BeginRecovery` in that order. Persist immediately.

- [ ] **Step 5: Run GREEN and inspect a local fixture report**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj --no-restore
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs --log-file artifacts/task11-revision/task5-prologue.log
```

Expected: `PROLOGUE_TESTS_OK`, v3 JSON/Markdown contain the exact synthetic fall, and no unauthorized hands-on evidence is created.

- [ ] **Step 6: Commit the telemetry slice**

```powershell
git add scripts/prologue/ProloguePlaytestSession.cs scripts/prologue/ProloguePlaytestReport.cs scripts/prologue/ProloguePlaytestRecorder.cs tests/unit/ProloguePlaytestSessionTests.cs tests/unit/ProloguePlaytestEvidenceGrantTests.cs tests/integration/ProloguePlaytestRecorderTests.cs tests/powershell/Task4PlaytestRecorderTransition.Tests.ps1 tests/RunPrologueTests.cs
git commit -m "feat: record exact canyon fall context"
```

---

### Task 6: Revised Critical Path, Two Full Verifiers, Proof Review and Human Gate

**Files:**
- Modify: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tests/presentation/MechanicalClawProofCapture.cs`
- Modify: `tests/presentation/BossRetryPresentation.cs`
- Modify: `tests/presentation/PrologueVisualAudit.cs`
- Modify: `tools/verify-prologue.ps1`
- Modify: `tools/audit-prologue-production.ps1`
- Modify: `tools/capture-prologue.ps1`
- Modify: `tests/powershell/PrologueEvidenceChecks.Tests.ps1`
- Modify: `tests/powershell/Task4ProloguePhaseCoverage.Tests.ps1`
- Modify: `README.md`
- Create: `docs/playtests/2026-08-29-terrain-grapple-dense-canyon-validation.md`

**Interfaces:**
- Adds exact proof markers `PROLOGUE_TERRAIN_GRAPPLE_OK`, `PROLOGUE_ATTACHED_BOOST_OK`, `PROLOGUE_MICROSCENES_OK count=15` and `PROLOGUE_FALL_CONTEXT_OK version=3` while retaining all existing Task 11 success markers.
- Produces fresh full-flow and Boss-retry AVI files at 1280×720/60 FPS and a keyframe manifest tied to their hashes.
- Keeps final goal status open until a genuine version-3 `HandsOnLauncher` report completes in 10–15 minutes and the user's least-clear issue is closed with a proportional RED/GREEN fix.

- [ ] **Step 1: Write fail-closed evidence RED contracts**

Require the critical path and capture to prove: wall hit, ceiling hit, platform underside/edge, Ice and Forbidden rejection, 620 range, one attached Space boost with retained cast, second boost rejection, no ordinary second jump, all 15 micro-scene IDs, all catch-return routes, four claw mechanism roles, both checkpoints, pre-checkpoint death returning to start, Boss death/retry and v3 fall fields. Reject a frozen/empty movie, wrong resolution/FPS, missing marker, automated human note, stale Space-detach copy, fewer than five sectors, fewer than fifteen micro-scenes, mandatory Dash, performance over 50 ms or any fatal/error/leak signature.

- [ ] **Step 2: Run evidence RED before driver changes**

```powershell
Invoke-Pester .\tests\powershell\PrologueEvidenceChecks.Tests.ps1
Invoke-Pester .\tests\powershell\Task4ProloguePhaseCoverage.Tests.ps1
```

Expected: named failures for missing terrain/boost/micro-scene/fall-v3 proof; no unrelated existing contract may turn green by being removed.

- [ ] **Step 3: Update the real-input critical path and deterministic presentation**

Drive all revised mechanics through InputMap. The critical path may use stable aiming coordinates and waits but may not directly call grapple/model/mechanism methods or teleport across a traversal proof. Presentation repositioning remains allowed only between completed non-timing proof beats. Include distinct visible dwell for each reticle state, boost start/arc/landing, recovery layer, optional side-room entrance, both checkpoints, Boss death/retry and completion.

- [ ] **Step 4: Run focused audit and capture once**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\audit-prologue-production.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1
```

Expected: Windows display/fixed-60 audit includes five sectors, fifteen micro-scenes, real claw proof states and callback peak below 50 ms. Capture emits nonempty MJPEG AVIs with exact 1280×720 dimensions and 60 FPS metadata.

- [ ] **Step 5: Run two consecutive fresh full verifiers**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Each independent process must end with exactly one `PROLOGUE_VERIFY_OK`, build 0 warnings/errors, all revised markers, no leak/fatal/error signature and protected generated hashes restored. A failure restarts the two-pass count only after its owning RED/GREEN fix.

- [ ] **Step 6: Extract and inspect proof at original resolution**

Use `tools/extract-prologue-keyframes.ps1` to extract frames for title/HUD, every reticle material, wall latch, ceiling latch, attached boost, second-boost rejection feedback, each sector/micro-scene group, both checkpoints, ordinary fall recovery, all Boss attacks, Boss death/retry, completion and title return. Inspect every extracted PNG at original resolution and record AVI path, SHA-256, frame count, keyframe manifest hash, display driver and performance peak in `docs/playtests/2026-08-29-terrain-grapple-dense-canyon-validation.md`.

- [ ] **Step 7: Update durable automated status and commit**

Update README's built/remaining/asset-table sections. State that no formal/paid art was generated and that human acceptance is still open. Stage only owned source, tests and documents, then commit:

```powershell
git commit -m "test: certify terrain grapple canyon revision"
```

- [ ] **Step 8: Launch the user-owned normal-speed playtest**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1
```

Require a genuine no-long-idle complete report between 600 and 900 seconds. Read its v3 falls, grapple failures and qualitative note; fix the least smooth or least clear issue with a focused RED/GREEN, rerun the proportional gate and both full verifiers if production source changed. Do not write the user's qualitative note on their behalf.

- [ ] **Step 9: Close the goal only with real report evidence**

Append the exact run ID, duration, completion state, fall contexts, issue and closure evidence to README and the validation document. Commit the resulting owned changes with:

```powershell
git commit -m "docs: close terrain grapple human playtest"
```

Only after that commit and a requirement-by-requirement audit may the active goal be marked complete.
