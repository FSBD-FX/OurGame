# Keyboard/Mouse Grapple Canyon Prologue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the room-swapped belfry prologue with a keyboard/mouse-driven, energy-and-shield combat loop plus a continuous mechanical-claw U-shaped canyon route that remains easy to clear and fully replaceable by later art.

**Architecture:** Keep player input, mode/loadout selection, energy, guard resolution, mechanical-claw physics, world streaming, progression and presentation in separate focused units. A persistent `SeamlessPrologueWorld` mounts adjacent sector scenes into one world coordinate system while the player, HUD, audio and progress remain alive; procedural gameplay geometry and stable visual roots preserve later art replacement.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, Godot `CharacterBody2D`/`Area2D`/`Control`, generated `.tscn` scenes, pure C# state models, PowerShell evidence gates, deterministic 1280×720@60 FPS capture.

**Spec:** `docs/superpowers/specs/2026-08-26-keyboard-mouse-grapple-canyon-prologue-design.md`

**Approved revision:** `docs/superpowers/plans/2026-08-29-terrain-grapple-dense-canyon-revision.md` supersedes the Space-detach, 520-pixel point-target, sparse-sector and report-v2 portions of Tasks 5, 6, 8, 9, 10 and 11. Tasks 1–4 and 7 remain regression requirements.

## Global Constraints

- Use `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe` and `G:\godot\dotnet-sdk\dotnet.exe`; preserve Godot 4.7.1 and `.NET 9` project fields.
- The rollback archive is `E:\OurGame\Knight\archives\Knight-Prologue-v0.9.0-belfry-baseline-20260826.zip`, SHA-256 `0EA35D960D3FCAD447F8C46351C6EE4DAA36CF425FFB59DC2263ABC60E0D0720`.
- Preserve unrelated dirty-worktree files. Stage and commit only paths owned by the current task.
- Follow `godot.md`: C# Godot classes are `partial`; generated scenes are built by `SceneTree` scripts; every generated node has the correct owner; packed node counts are validated after instantiation.
- Primary controls are keyboard/mouse: `A/D`, contextual `W/S`, `Space`, `Shift`, `Q`, `E`, mouse left/right and `1–4`.
- Final runtime must not retain `J/K` as hidden player controls.
- Attack mode and grapple mode are sticky selections; mouse left never changes mode based on cursor context.
- Energy starts at 100; melee costs 12; regeneration is 28/s after 0.55 s; shield drains 24/s and heats 42/s; heat cools 35/s after 0.35 s; overheat unlocks below 35.
- Mechanical claw costs no energy, has 520 px range, 36 px main-anchor assist and only one live claw/constraint.
- Mandatory traversal cannot require double jump, air dash, hidden combos or pixel-perfect cursor placement.
- Formal art production and all paid generation remain blocked until gameplay, automated gates and normal-speed human review pass.
- Every task follows RED → observed failure → minimal GREEN → focused regression → task-scoped commit.
- Every playable checkpoint must build with 0 warnings and 0 errors.

## File Structure and Ownership

- `scripts/player/`: input frames, sticky action/loadout selection, energy state, shield state, grapple state and the runtime coordinators that own those states. Pure models do not query Godot input or collision.
- `scripts/combat/`: damage classification and immutable damage requests. It does not decide UI, energy regeneration or grapple behavior.
- `scripts/grapple/`: surface identity/query results, pure rope physics and the one-live-claw runtime. Legacy pull/weakpoint/mechanism effects are reached only through an adapter interface.
- `scripts/prologue/seamless/`: sector identity, validated sector definitions, pure mounted-set calculation and persistent-world mounting. The player, HUD, audio and progress never become children of a sector.
- `scripts/prologue/`, `scripts/save/`: continuous objective/checkpoint state, recovery, first-level guidance, schema migration and playtest telemetry.
- `scripts/ui/`, `scripts/presentation/`, `scripts/audio/`, `scripts/vfx/`: read-only presentation consumers. Gameplay collision and authoritative state remain outside all `VisualRoot` trees.
- `tools/build/`: deterministic scene builders. Leaf player/UI/sector scenes build before the composed prologue level, and every builder validates packed/instantiated node counts.
- `tests/unit/`: pure transition, tuning and structure contracts. `tests/integration/`: generated-scene and real-physics behavior. `tests/smoke/`, `tests/presentation/`, `tests/powershell/`: full route, visual proof and fail-closed release evidence.
- `scenes/player/Player.tscn`, `scenes/ui/PrologueUi.tscn`, `scenes/prologue/sectors/*.tscn`, `scenes/prologue/PrologueLevel.tscn`: generated outputs only; never hand-edit them.
- `README.md`, `docs/playtests/`: durable status, asset replacement table and evidence. Formal/paid asset generation remains outside this plan until human gameplay acceptance.

---

### Task 1: Keyboard/Mouse Command Frame, Sticky Modes and Four Loadout Slots

**Files:**
- Create: `scripts/player/PlayerActionMode.cs`
- Create: `scripts/player/PlayerActionSelectionModel.cs`
- Create: `scripts/player/PlayerActionSelectionState.cs`
- Modify: `scripts/shared/InputActions.cs`
- Modify: `scripts/player/PlayerInputFrame.cs`
- Modify: `scripts/player/PlayerInputReader.cs`
- Modify: `tools/configure/ConfigureProject.cs`
- Modify: `tests/unit/ProjectFoundationTests.cs`
- Create: `tests/unit/PlayerActionSelectionModelTests.cs`
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Produces: `enum PlayerActionMode { Attack, Grapple }`.
- Produces: `PlayerActionSelectionState(PlayerActionMode Mode, int ActiveSlot)` with default `Attack/1`.
- Produces: `PlayerActionSelectionModel.Step(PlayerActionSelectionState state, bool attackModePressed, bool grappleModePressed, int requestedSlot) -> PlayerActionSelectionState`.
- Produces new `PlayerInputFrame` fields: `MouseWorld`, `AttackModePressed`, `GrappleModePressed`, `RequestedSlot`, `PrimaryPressed`, `PrimaryHeld`, `PrimaryReleased`, `ShieldHeld`.
- Temporarily retains existing `AttackPressed`/`ThreadPressed` frame fields only until Task 6 migrates all runtime consumers; final removal is part of Task 10.

- [ ] **Step 1: Write failing mode/loadout tests**

Add `PlayerActionSelectionModelTests.Run()` with exact assertions:

```csharp
public static void Run()
{
    var model = new PlayerActionSelectionModel();
    var initial = new PlayerActionSelectionState(PlayerActionMode.Attack, 1);
    TestAssert.Equal(initial, model.Step(initial, false, false, 0), "selection is sticky");
    TestAssert.Equal(PlayerActionMode.Grapple,
        model.Step(initial, false, true, 0).Mode, "E selects grapple mode");
    TestAssert.Equal(PlayerActionMode.Attack,
        model.Step(initial with { Mode = PlayerActionMode.Grapple }, true, false, 0).Mode,
        "Q selects attack mode");
    TestAssert.Equal(4, model.Step(initial, false, false, 4).ActiveSlot, "slot four selects");
    TestAssert.Equal(1, model.Step(initial, false, false, 5).ActiveSlot,
        "out-of-range slot request is ignored");
}
```

Call it from `RunPrototypeTests` immediately after `ProjectFoundationTests.Run()`.

- [ ] **Step 2: Extend the project-foundation RED contract**

Require the new actions and exact keyboard/mouse bindings while continuing to require movement, jump, dash and pause:

```csharp
RequireKey(InputActions.SelectAttackMode, Key.Q);
RequireKey(InputActions.SelectGrappleMode, Key.E);
RequireMouse(InputActions.PrimaryAction, MouseButton.Left);
RequireMouse(InputActions.Shield, MouseButton.Right);
RequireKey(InputActions.Slot1, Key.Key1);
RequireKey(InputActions.Slot2, Key.Key2);
RequireKey(InputActions.Slot3, Key.Key3);
RequireKey(InputActions.Slot4, Key.Key4);
```

- [ ] **Step 3: Run RED**

Run:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
```

Expected: FAIL because `PlayerActionMode`, `PlayerActionSelectionModel` and the new input actions do not exist.

- [ ] **Step 4: Implement the pure selection model and input names**

Use these exact types:

```csharp
public enum PlayerActionMode { Attack, Grapple }

public readonly record struct PlayerActionSelectionState(PlayerActionMode Mode, int ActiveSlot);

public sealed class PlayerActionSelectionModel
{
    public PlayerActionSelectionState Step(PlayerActionSelectionState state,
        bool attackModePressed, bool grappleModePressed, int requestedSlot)
    {
        PlayerActionMode mode = grappleModePressed ? PlayerActionMode.Grapple :
            attackModePressed ? PlayerActionMode.Attack : state.Mode;
        int slot = requestedSlot is >= 1 and <= 4 ? requestedSlot : state.ActiveSlot;
        return new PlayerActionSelectionState(mode, slot);
    }
}
```

Add exact `StringName` values `select_attack_mode`, `select_grapple_mode`, `primary_action`, `shield`, `slot_1`, `slot_2`, `slot_3`, `slot_4` to `InputActions`.

- [ ] **Step 5: Configure the new InputMap and mouse world frame**

In `ConfigureProject`, map `Q`, `E`, mouse left/right and digits 1–4. In `PlayerInputReader`, compute:

```csharp
Vector2 mouseWorld = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetMousePosition();
int requestedSlot = Input.IsActionJustPressed(InputActions.Slot1) ? 1 :
    Input.IsActionJustPressed(InputActions.Slot2) ? 2 :
    Input.IsActionJustPressed(InputActions.Slot3) ? 3 :
    Input.IsActionJustPressed(InputActions.Slot4) ? 4 : 0;
```

Populate all new frame fields from `Input` action edges. Do not synthesize primary action from cursor target state.

- [ ] **Step 6: Generate InputMap and run GREEN**

Run:

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/configure/ConfigureProject.cs
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrototypeTests.cs --log-file task1-input-green.log
```

Expected: `PROTOTYPE_TESTS_OK`, exact Q/E/mouse/slot bindings and no warning/error line.

- [ ] **Step 7: Commit**

```powershell
git add project.godot scripts/shared/InputActions.cs scripts/player/PlayerInputFrame.cs scripts/player/PlayerInputReader.cs scripts/player/PlayerActionMode.cs scripts/player/PlayerActionSelectionModel.cs scripts/player/PlayerActionSelectionState.cs tools/configure/ConfigureProject.cs tests/unit/ProjectFoundationTests.cs tests/unit/PlayerActionSelectionModelTests.cs tests/RunPrototypeTests.cs
git commit -m "feat: add keyboard mouse action selection"
```

---

### Task 2: Deterministic Energy and Overheat Model

**Files:**
- Create: `scripts/config/PlayerEnergyTuning.cs`
- Create: `scripts/player/PlayerEnergySettings.cs`
- Create: `scripts/player/PlayerEnergyState.cs`
- Create: `scripts/player/PlayerEnergyFrame.cs`
- Create: `scripts/player/PlayerEnergyModel.cs`
- Create: `scripts/player/PlayerEnergyController.cs`
- Create: `tests/unit/PlayerEnergyModelTests.cs`
- Modify: `tests/unit/TuningContractTests.cs`
- Modify: `tests/RunPrototypeTests.cs`
- Modify: `tools/build/BuildPlayer.cs`
- Modify generated output: `scenes/player/Player.tscn`

**Interfaces:**
- Consumes: Task 1 `PlayerInputFrame.ShieldHeld`.
- Produces: `PlayerEnergySettings(float MaxEnergy, float AttackCost, float RegenDelaySeconds, float RegenPerSecond, float ShieldDrainPerSecond, float ShieldHeatPerSecond, float HeatCoolDelaySeconds, float HeatCoolPerSecond, float OverheatUnlockHeat)` and approved value `new(100f, 12f, .55f, 28f, 24f, 42f, .35f, 35f, 35f)`.
- Produces: `PlayerEnergyModel.Step(PlayerEnergyState state, PlayerEnergyInput input, float delta) -> PlayerEnergyFrame`.
- Produces controller methods `bool TrySpendAttack()`, `bool TrySpendHeavyGuard(float amount = 24f)`, `void SetShieldRequested(bool requested)`, `void ResetFull()`.
- Produces read-only controller properties `EnergyRatio`, `HeatRatio`, `IsOverheated`, `IsShieldSustained` and `EnergyFrameChanged` event.

- [ ] **Step 1: Write failing energy tests**

Cover exact boundary behavior:

```csharp
var settings = PlayerEnergySettings.Approved;
var model = new PlayerEnergyModel(settings);
PlayerEnergyState state = PlayerEnergyState.Full(settings);
TestAssert.True(model.TrySpendAttack(state, out state), "full energy accepts attack");
TestAssert.Near(88f, state.Energy, .001f, "attack costs twelve");

PlayerEnergyFrame held = model.Step(state, new PlayerEnergyInput(true, false), 1f);
TestAssert.Near(64f, held.State.Energy, .001f, "shield drains 24 per second");
TestAssert.Near(42f, held.State.Heat, .001f, "shield heats 42 per second");

PlayerEnergyFrame overheated = model.Step(PlayerEnergyState.Full(settings),
    new PlayerEnergyInput(true, false), 100f / 42f);
TestAssert.True(overheated.State.Overheated && !overheated.ShieldSustained,
    "heat 100 forces shield closed");
```

Also assert no regeneration before 0.55 s, 28 points after one valid second, unlock only below 35 heat, energy-zero shield drop without forced overheat, and `ResetFull` returns 100/0/not-overheated.

- [ ] **Step 2: Run RED**

Run the build. Expected: missing energy types.

- [ ] **Step 3: Implement immutable settings/state/frame and model**

Use one mutation owner: the pure model returns new state; the controller stores it. Clamp energy and heat to `[0,100]`; evaluate forced closure in the same `Step` that crosses 100 heat or zero energy. `TrySpendAttack` updates the regeneration delay only when it succeeds.

- [ ] **Step 4: Add validated tuning resource**

`PlayerEnergyTuning.Validate()` rejects non-finite values, maximum energy `<= 0`, negative rates, overheat unlock outside `[0,100)`, and attack cost greater than maximum energy. `ToSettings()` passes every exported property positionally.

- [ ] **Step 5: Add runtime controller and generated player node**

Mount `Player/Energy` with `PlayerEnergyTuning`. The controller calls the pure model at physics rate, raises `EnergyFrameChanged` only on state change and exposes the exact methods above. Extend `BuildPlayer` post-pack validation to require `PlayerEnergyController` and non-null tuning.

- [ ] **Step 6: Run focused GREEN**

Run build, `BuildPlayer`, then `RunPrototypeTests`. Expected: 0 warnings/errors and `PROTOTYPE_TESTS_OK`.

- [ ] **Step 7: Commit**

```powershell
git add scripts/config/PlayerEnergyTuning.cs scripts/player/PlayerEnergySettings.cs scripts/player/PlayerEnergyState.cs scripts/player/PlayerEnergyFrame.cs scripts/player/PlayerEnergyModel.cs scripts/player/PlayerEnergyController.cs tools/build/BuildPlayer.cs scenes/player/Player.tscn tests/unit/PlayerEnergyModelTests.cs tests/unit/TuningContractTests.cs tests/RunPrototypeTests.cs
git commit -m "feat: add player energy and overheat model"
```

---

### Task 3: Directional Shield and Guard-Class Damage

**Files:**
- Create: `scripts/combat/DamageGuardClass.cs`
- Modify: `scripts/combat/DamageRequest.cs`
- Modify: `scripts/combat/Hitbox.cs`
- Create: `scripts/player/PlayerShieldSettings.cs`
- Create: `scripts/player/PlayerShieldModel.cs`
- Create: `scripts/player/PlayerGuardedDamageReceiver.cs`
- Create: `scripts/player/PlayerShieldController.cs`
- Create: `scripts/presentation/PlayerShieldPlaceholderVisual.cs`
- Modify: `tools/build/BuildPlayer.cs`
- Modify: `scripts/boss/FallenBellkeeper.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeper.cs`
- Modify generated outputs: `scenes/player/Player.tscn`, `scenes/boss/FallenBellkeeper.tscn`
- Create: `tests/unit/PlayerShieldModelTests.cs`
- Create: `tests/integration/PlayerShieldRuntimeTests.cs`
- Modify: `tests/RunPrototypeTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Consumes: Task 2 `PlayerEnergyController`.
- Produces: `DamageGuardClass.Normal`, `Heavy`, `Unblockable` on every `DamageRequest`.
- Produces: `PlayerShieldSettings(float FrontArcDegrees, float MovementScale, float HeavyEnergyCost, float HeavyKnockbackScale)` and approved value `new(150f, .65f, 24f, .45f)`.
- Produces: `PlayerShieldResult(bool Guarded, int DamageAmount, float KnockbackScale, bool SpendHeavyEnergy, bool BrokeGuard)`; a successfully guarded heavy hit returns exactly `new(true, 1, .45f, true, false)`.
- Produces: `PlayerShieldModel.Resolve(PlayerShieldInput input) -> PlayerShieldResult` where input includes shield direction, incoming direction, guard class and heavy-energy availability.
- Produces: `PlayerGuardedDamageReceiver : IDamageReceiver`, delegating accepted damage to `HealthComponent` exactly once.
- Produces stable node paths `Player/Shield`, `Player/VisualRoot/Shield`.

- [ ] **Step 1: Write shield-model RED tests**

Assert a 150-degree frontal cone, rear rejection, normal full guard, heavy one-damage/45%-knockback resolution, insufficient-energy heavy break and unblockable pass-through:

```csharp
var model = new PlayerShieldModel(new PlayerShieldSettings(150f, .65f, 24f, .45f));
PlayerShieldResult normal = model.Resolve(new PlayerShieldInput(
    true, Vector2.Right, Vector2.Right, DamageGuardClass.Normal, true));
TestAssert.True(normal.Guarded && normal.DamageAmount == 0, "front normal is negated");

PlayerShieldResult rear = model.Resolve(new PlayerShieldInput(
    true, Vector2.Right, Vector2.Left, DamageGuardClass.Normal, true));
TestAssert.True(!rear.Guarded, "rear attack bypasses shield");
```

- [ ] **Step 2: Run RED**

Run build. Expected: missing guard and shield types.

- [ ] **Step 3: Extend damage identity without breaking duplicate suppression**

Add `DamageGuardClass GuardClass = DamageGuardClass.Normal` to `DamageRequest` and `Hitbox`. Preserve `DamageAttackIdentity(SourceRuntimeId, SourceAttackId)` unchanged. Update `Hitbox.TryConnect` to pass the guard class.

- [ ] **Step 4: Implement pure shield resolution and guarded receiver**

The receiver queries `PlayerShieldController.TryResolve(request, out DamageRequest resolved)`. A fully guarded normal attack returns `true` after recording the attack identity in the guard receiver; a heavy forwards exactly one resolved request to health; rear/unblockable forwards the original request. Use a bounded attack-identity set matching `HealthComponent`'s duplicate policy.

- [ ] **Step 5: Implement runtime shield controller and placeholder**

Read `ShieldHeld` and mouse aim. Request Task 2 shield sustain, set motor ability scale to `.65f`, expose `IsActive` and `ShieldFrameChanged`, and update the transparent procedural fan under `VisualRoot/Shield`. The visual uses no collision or damage logic.

- [ ] **Step 6: Classify Boss attacks**

Set Sweep, Leap Slam and Hammer Throw hitboxes to `Heavy`; set Ground Wave to `Unblockable`. Keep existing telegraph kind and danger geometry, and add a distinct unblockable cue consumed later by Task 10.

- [ ] **Step 7: Add generated nodes and runtime tests**

Route player Hurtbox to `PlayerGuardedDamageReceiver`. Runtime test a front normal, rear normal, front heavy with 24 energy, heavy without energy, overheat same-frame deactivation, death reset and duplicate attack identity.

- [ ] **Step 8: Run GREEN and regression**

Run `BuildPlayer`, `BuildFallenBellkeeper`, `RunPrototypeTests`, `RunFallenBellkeeperRuntimeTests` and `RunPrologueTests`. Expected: all success markers once and zero unexpected errors.

- [ ] **Step 9: Commit**

Commit only the files named in this task with message:

```powershell
git commit -m "feat: add directional energy shield"
```

---

### Task 4: Runtime Mode Router, Energy-Aware Combat and Four-Slot HUD Contract

**Files:**
- Create: `scripts/player/PlayerActionModeController.cs`
- Modify: `scripts/player/PlayerCombat.cs`
- Modify: `scripts/presentation/PlayerPresentationCoordinator.cs`
- Modify: `scripts/ui/PrologueHudViewModel.cs`
- Modify: `scripts/ui/PrologueFormalHud.cs`
- Modify: `tools/build/BuildPlayer.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify generated outputs: `scenes/player/Player.tscn`, `scenes/prologue/PrologueLevel.tscn`
- Create: `tests/integration/PlayerActionModeRuntimeTests.cs`
- Modify: `tests/unit/PrologueHudViewModelTests.cs`
- Modify: `tests/integration/PrologueFormalHudTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Consumes: Task 1 selection model/frame; Task 2 energy; Task 3 shield.
- Produces: `PlayerActionModeController.CurrentMode`, `ActiveSlot`, `ModeChanged`, `LoadoutChanged`, `bool CanBeginAttack`, `bool CanBeginGrapple`.
- Produces stable HUD paths `ModeSelector/Attack`, `ModeSelector/Grapple`, `Loadout/Slot1..Slot4`, `ResourceCluster/Energy`, `ResourceCluster/Heat`.

- [ ] **Step 1: Write runtime-router RED tests**

Instantiate the generated player and inject frames proving: default Attack/1, E sticks to Grapple, Q sticks back to Attack, slot 4 stays selected, empty slot rejects attack, shield rejects new attack, accepted slot-1 attack spends exactly 12, and rejected attacks spend zero.

- [ ] **Step 2: Write HUD RED tests**

Require all stable paths, selected/unselected modes, selected empty slot, exact energy/heat ratios, overheat state and non-color shape marker. Require 1280×720 bounds not to overlap the health or objective ribbons.

- [ ] **Step 3: Run RED**

Run build and `RunPrologueTests`. Expected: missing controller/HUD nodes.

- [ ] **Step 4: Implement mode controller**

Read the selection inputs once per physics frame. On Q while a grapple-compatible consumer is active, raise `AttackModeRequested`; Task 6 will subscribe and retract. Keep `CanBeginAttack` true only for Attack mode, active slot 1, shield inactive and sufficient attack energy. Keep `CanBeginGrapple` true only for Grapple mode and shield not starting a new guard.

- [ ] **Step 5: Route combat through mode and energy**

Replace `input.AttackPressed` with `input.PrimaryPressed && mode.CanBeginAttack`. Call `TrySpendAttack()` before `PlayerCombatModel.TryStart`; if model start fails because an attack is already active, refund is not needed because `CanBeginAttack` also requires `!IsAttacking`. Emit distinct rejection reason through the mode controller for empty slot, energy and shield.

- [ ] **Step 6: Build and present stable HUD nodes**

Extend `PrologueHudViewModel` with immutable mode, active slot, energy, heat, overheat and rejection fields. Draw simple vector placeholders in new focused controls; do not place gameplay state in the controls.

- [ ] **Step 7: Run GREEN**

Build generated player/level and run `RunPrototypeTests` plus `RunPrologueTests`. Expected: mode, energy and HUD assertions pass; old mechanical-thread tests still pass until Task 6 replacement.

- [ ] **Step 8: Commit**

```powershell
git commit -m "feat: route mouse combat through modes and energy"
```

---

### Task 5: Pure Mechanical-Claw State and Rope Dynamics

**Files:**
- Create: `scripts/abilities/GrappleSurfaceMaterial.cs`
- Create: `scripts/abilities/MechanicalClawState.cs`
- Create: `scripts/abilities/MechanicalClawFailure.cs`
- Create: `scripts/abilities/MechanicalClawSettings.cs`
- Create: `scripts/abilities/MechanicalClawInput.cs`
- Create: `scripts/abilities/MechanicalClawTargetSnapshot.cs`
- Create: `scripts/abilities/MechanicalClawFrame.cs`
- Create: `scripts/abilities/MechanicalClawModel.cs`
- Create: `tests/unit/MechanicalClawModelTests.cs`
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Produces exact states `Idle`, `Aim`, `Windup`, `Travel`, `Latched`, `Reel`, `Swing`, `Released`, `Retract`, `Failed`.
- Produces failure values `None`, `NoTarget`, `OutOfRange`, `Obstructed`, `IceSlip`, `Forbidden`, `TargetLost`, `AlreadyDeployed`.
- Produces `MechanicalClawSettings(float MaxRange, float MainAnchorAssistRadius, float MinimumRopeLength, float ReelSpeed, float SwingAcceleration, float RadialStiffness, float RadialDamping, float ShuttleSpeed, float RetractSpeed)` with approved range/assist values fixed at `520f/36f`; the remaining motion values are centralized here and covered by the traversal tests rather than duplicated in runtime nodes.
- Produces `MechanicalClawInput(bool PrimaryPressed, bool PrimaryHeld, bool PrimaryReleased, bool DetachPressed, float SwingAxis, float ReelAxis, bool ShieldActive)`.
- Produces `MechanicalClawTargetSnapshot(ulong RuntimeId, bool Exists, GrappleSurfaceMaterial Material, Vector2 WorldAnchor, bool HasLineOfSight, bool IsLegacyInteraction)`.
- Produces `MechanicalClawModel.Step(MechanicalClawFrame previous, MechanicalClawInput input, MechanicalClawTargetSnapshot target, float delta) -> MechanicalClawFrame`.
- Produces `MechanicalClawFrame` fields for cast ID, state, failure, anchor point, rope length, velocity correction and effective visibility.

- [ ] **Step 1: Write lifecycle RED tests**

Prove exact chronological states from press through multi-frame travel, latch, swing, reel, release, retract and idle. Require at least two distinct physics steps in `Travel` at 520 px. Require only one cast ID and no second deployment before idle.

- [ ] **Step 2: Write material/failure RED tests**

Assert `GripStone`, `TempleMetal` and `DedicatedAnchor` accept; `Ice` produces `IceSlip`; `Forbidden` produces `Forbidden`; 520 is inclusive and 520.01 is out of range; obstructed valid material returns `Obstructed`; target loss while latched returns `TargetLost` and retracts.

- [ ] **Step 3: Write rope RED tests**

Use deterministic inputs to assert A/D tangential acceleration, W rope shortening, S lengthening, bounds on minimum/maximum rope length, shield pause preserving rope length, release preserving tangential velocity and no energy field anywhere in the claw input/frame.

- [ ] **Step 4: Run RED**

Run build. Expected: all claw types missing.

- [ ] **Step 5: Implement the pure model**

Use frame-rate-independent damping `velocity *= Mathf.Exp(-rate * delta)`. Calculate radial correction separately from tangential input; never teleport position in the model. Lock target identity and anchor at Travel entry, and fail closed if the snapshot identity changes or disappears.

- [ ] **Step 6: Run GREEN and mutation checks**

Run `RunPrototypeTests`. Then temporarily change Ice acceptance, range inclusivity and single-claw guard one at a time; confirm the named tests fail and restore exact source bytes.

- [ ] **Step 7: Commit**

```powershell
git commit -m "feat: model continuous mechanical claw"
```

---

### Task 6: Runtime Mechanical Claw, Surface Query and Legacy Interaction Adapter

**Files:**
- Create: `scripts/abilities/GrappleSurface2D.cs`
- Create: `scripts/abilities/MechanicalClawTargetResolver.cs`
- Create: `scripts/abilities/MechanicalClawController.cs`
- Create: `scripts/abilities/LegacyThreadInteractionAdapter.cs`
- Modify: `scripts/abilities/ThreadTarget2D.cs`
- Modify: `scripts/abilities/ThreadTargetKind.cs`
- Modify: `scripts/player/PlayerMotor.cs`
- Modify: `scripts/vfx/ThreadLineVisual.cs`
- Modify: `scripts/vfx/ThreadShuttleVisual.cs`
- Modify: `scripts/presentation/PlayerPresentationCoordinator.cs`
- Modify: `scripts/prologue/traversal/TraversalSegment2D.cs`
- Modify: `tools/build/BuildPlayer.cs`
- Modify: `tools/build/prologue/BuildAwakeningCourtyard.cs`
- Modify: `tools/build/prologue/BuildBrokenToothCorridor.cs`
- Modify: `tools/build/prologue/BuildUpperPendulum.cs`
- Modify: `tools/build/prologue/BuildLowerBuriedMachinery.cs`
- Modify: `tools/build/prologue/BuildBellChainRoom.cs`
- Modify: `tools/build/prologue/BuildBellChainMechanism.cs`
- Modify: `tools/build/prologue/BuildGraveLampMoth.cs`
- Modify: `tools/build/prologue/BuildBellBackLaborer.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeperArena.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeper.cs`
- Modify generated output: `scenes/player/Player.tscn`
- Modify generated outputs: `scenes/enemies/GraveLampMoth.tscn`, `scenes/enemies/BellBackLaborer.tscn`
- Modify generated outputs: `scenes/prologue/rooms/AwakeningCourtyard.tscn`, `BrokenToothCorridor.tscn`, `UpperPendulum.tscn`, `LowerBuriedMachinery.tscn`, `BellChainMechanism.tscn`, `FallenBellkeeperArena.tscn`
- Modify generated output: `scenes/boss/FallenBellkeeper.tscn`
- Create: `tests/unit/GrappleSurfaceTests.cs`
- Create: `tests/integration/MechanicalClawRuntimeTests.cs`
- Modify: `tests/integration/ThreadInteractionTests.cs`
- Modify: `tests/integration/PlayerTraversalInputTests.cs`
- Modify: `tests/RunPrototypeTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Consumes: Task 4 `CanBeginGrapple` and Attack-mode retract event; Task 5 pure model.
- Produces: `GrappleSurface2D.Material`, `AssistRadius`, `WorldAnchor`, `IsAvailable`.
- Produces: `MechanicalClawController.CurrentFrame`, `CurrentTarget`, `ClawFrameChanged`, `ResetTransientState()`.
- Preserves existing `IThreadInteractionReceiver.TryReceiveThread(ThreadInteraction)` behind `LegacyThreadInteractionAdapter` until names are migrated in a later release.

- [ ] **Step 1: Write resolver and runtime RED tests**

Build a world with each surface material, a blocking collider and targets at 519/520/521 px. Inject E plus primary mouse input. Assert visible preview, real multi-frame shuttle travel, latch, rope length changes, player movement, clean release, target unload failure and no leftover line/shuttle.

- [ ] **Step 2: Write legacy-interaction RED tests**

Require light enemy pull, heavy enemy weak-point exposure and Bell Chain mechanism activation through the new controller. Require no persistent rope for those three receiver types.

- [ ] **Step 3: Run RED**

Run build and focused runners. Expected: missing runtime claw/surface types.

- [ ] **Step 4: Implement authored surface query**

Resolve surface candidates by physics shape/raycast and explicit `GrappleSurface2D`; never infer grippability from texture color or node name. Return nearest cursor-distance candidate inside the 36 px assist radius after range and line-of-sight checks. Preserve exact failure priority: active claw, target loss, ice/forbidden, out of range, obstruction, no target.

- [ ] **Step 5: Implement controller and motor constraint**

Drive the Task 5 model from physics input. Apply model velocity correction through a dedicated `PlayerMotor.ApplyGrappleConstraint(Vector2 velocity, float movementScale)` method. While shield is active, feed `ReelAxis=0` but preserve connection. Subscribe to Q selection and call safe retract.

- [ ] **Step 6: Reuse stable line/shuttle visual roots**

Rename player runtime node to `MechanicalClaw` and visual node to `VisualRoot/MechanicalClaw`; retain the existing visual classes internally until Task 10 renames presentation types. Visuals subscribe to frames and never query collision themselves.

- [ ] **Step 7: Remove gameplay cooldown and primary K dependency**

The new controller never constructs `ThreadAbilityModel` or consumes `ThreadTuning.CooldownSeconds`. Migrate all required room targets to surface or adapter nodes. Keep old `ThreadAbility` source only while old scenes remain available for baseline comparison; it is absent from the generated main player.

- [ ] **Step 8: Run GREEN and traversal regression**

Build every affected scene, run prototype/prologue tests and the real traversal route runner. Require one-claw lifetime, ice failure, all three legacy interactions, no double jump and no old `K` input in the generated player path.

- [ ] **Step 9: Commit**

```powershell
git commit -m "feat: replace mechanical thread with grapple runtime"
```

---

### Task 7: Persistent Seamless World and Sector Lifecycle

**Files:**
- Create: `scripts/prologue/seamless/PrologueSectorId.cs`
- Create: `scripts/prologue/seamless/PrologueSectorDefinition.cs`
- Create: `scripts/prologue/seamless/PrologueSector.cs`
- Create: `scripts/prologue/seamless/PrologueSectorStreamModel.cs`
- Create: `scripts/prologue/seamless/SeamlessPrologueWorld.cs`
- Create: `tests/unit/PrologueSectorStreamModelTests.cs`
- Create: `tests/integration/SeamlessPrologueWorldTests.cs`
- Modify: `scripts/prologue/PrologueLevelController.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces sector IDs `CanyonEntrance`, `CanyonDescent`, `CanyonBasin`, `CanyonAscent`, `LostTemple`.
- Produces `PrologueSectorDefinition(Id, Scene, WorldOffset, Bounds, Previous, Next)`.
- Produces `PrologueSectorStreamModel.RequiredSet(current)` returning current plus existing previous/next.
- Produces `SeamlessPrologueWorld.CurrentSector`, `MountedSectorIds`, `TryActivateCheckpoint`, `RestoreCheckpoint`, `ResetForNewRun`.

- [ ] **Step 1: Write stream-model RED tests**

Assert entrance requires entrance/descent; middle requires previous/current/next; temple requires ascent/temple; no set exceeds three; every required sector is unique and ordered.

- [ ] **Step 2: Write runtime RED tests**

Use five tiny fixture sectors at continuous offsets. Move a real player through boundaries and assert no player reparent, no position rewrite, preserved velocity/energy/heat/mode/slot, smooth camera-limit interpolation and bounded mounted set. Latch a claw inside a sector, cross away and assert target-loss cleanup before unmount.

- [ ] **Step 3: Run RED**

Run build and prologue runner. Expected: seamless world types missing.

- [ ] **Step 4: Implement validated sector definitions and stream model**

Reject null scenes, non-finite offsets/bounds, duplicate IDs, missing contiguous neighbors and key interaction bounds that overlap the unload seam. Keep pure required-set calculation independent of Godot nodes.

- [ ] **Step 5: Implement runtime mounting**

Mount scenes below `WorldRoot/Sectors/<SectorId>` at authored world offsets. Prepare the next set before freeing obsolete sectors. Keep the player under `PlayerRoot`, never below a sector. Notify the claw controller before queue-free, then release retired sectors through the existing bounded room-retirement discipline.

- [ ] **Step 6: Adapt the level controller without deleting the old graph**

Add a `SeamlessWorldPath` and delegate current location/checkpoint queries to it when configured. Keep old `RoomDefinition` flow selectable only for existing isolated tests until Task 9 migrates completion/progress; do not expose old room switching in the main generated level.

- [ ] **Step 7: Run GREEN and lifecycle stress**

Traverse entrance→temple→entrance ten times in a fixture. Require mounted count `<=3`, retired count bounded, no stale targets and no errors/leaks. Run `RunPrologueTests` twice in fresh Godot processes.

- [ ] **Step 8: Commit**

```powershell
git commit -m "feat: add seamless prologue sector streaming"
```

---

### Task 8: Generate the Five-Sector U-Shaped Canyon Graybox

**Files:**
- Create: `tools/build/prologue/PrologueCanyonBuildParts.cs`
- Create: `tools/build/prologue/BuildCanyonEntrance.cs`
- Create: `tools/build/prologue/BuildCanyonDescent.cs`
- Create: `tools/build/prologue/BuildCanyonBasin.cs`
- Create: `tools/build/prologue/BuildCanyonAscent.cs`
- Create: `tools/build/prologue/BuildLostTemple.cs`
- Create generated scenes: `scenes/prologue/sectors/CanyonEntrance.tscn`, `CanyonDescent.tscn`, `CanyonBasin.tscn`, `CanyonAscent.tscn`, `LostTemple.tscn`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify generated output: `scenes/prologue/PrologueLevel.tscn`
- Create: `tests/unit/PrologueCanyonStructureTests.cs`
- Create: `tests/integration/PrologueCanyonTraversalTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Consumes: Task 6 surfaces/claw; Task 7 sector definitions.
- Produces the five exact sector scenes and stable sector visual roots.
- Produces named mandatory traversal segments and safe landing zones consumed by automated physical probes.

- [ ] **Step 1: Write structural RED tests**

Require each sector root, world bounds, entrances, exits, collision, fall recovery, `VisualRoot/{Background,Midground,GameplayReadable,Foreground,Effects}`, sector seam margins and no critical grapple surface inside 96 px of a seam.

- [ ] **Step 2: Write route-contract RED tests**

Define exact mandatory segments:

```text
entrance_move_attack
descent_grip_lesson
descent_ice_rejection
basin_shield_lane
basin_checkpoint
ascent_grapple_one
ascent_grapple_two
ascent_grapple_three
temple_mechanism
temple_checkpoint
temple_boss_entry
```

For every segment, require a visible launch zone, landing zone, allowed ability mask and explicit assist surface where applicable. No mandatory mask contains Dash.

- [ ] **Step 3: Run RED**

Run prologue tests. Expected: missing canyon builders/scenes/contracts.

- [ ] **Step 4: Build entrance and descent**

Place entrance at the high-left world origin and descent progressively down/right. Include one low-pressure Rust-Shell encounter, broad jump shelves, `GripStone` lesson, `DedicatedAnchor` lesson, one visible `Ice` rejection and catch platforms below every required attempt.

- [ ] **Step 5: Build basin and first optional alcove**

Lay the basin horizontally right with collapsed machinery, shield lane, first checkpoint and a short hidden supply cave. The cave can be skipped and returns to the same mainline coordinate without a one-way drop.

- [ ] **Step 6: Build ascent and second optional ruin**

Author three increasingly long but assisted grapple segments. Their ordinary-jump fallback is intentionally insufficient, while one grapple without Dash reaches a wide safe landing. Add lower catch shelves and a non-blocking high ruin detour.

- [ ] **Step 7: Build Lost Temple and Boss integration**

Continue right at high elevation through outer wall and mechanism hall to second checkpoint, shortcut and Fallen Bellkeeper arena. Reuse the Boss scene and encounter contract without room teleport. Boss arena locks only after the player crosses the authored activation trigger.

- [ ] **Step 8: Run builders and pack validation**

Build leaf sectors, then `BuildPrologueLevel`. Each builder compares pre-pack and instantiated node counts and fails on dropped nodes.

- [ ] **Step 9: Run real traversal GREEN**

Drive actual InputMap and physics through every mandatory segment from at least three reasonable launch positions. Require no accepted second air jump, no Dash on mandatory segments, real surface latch, real landing and completion headroom. Run both optional paths as pass/skip cases.

- [ ] **Step 10: Commit**

```powershell
git commit -m "feat: build seamless u shaped canyon prologue"
```

---

### Task 9: Continuous Progress, Checkpoints, Guidance, Save and Playtest Telemetry

**Files:**
- Modify: `scripts/prologue/PrologueObjective.cs`
- Modify: `scripts/prologue/PrologueObjectiveModel.cs`
- Modify: `scripts/prologue/PrologueProgress.cs`
- Modify: `scripts/prologue/PrologueCheckpointSnapshot.cs`
- Modify: `scripts/prologue/PrologueCompletionController.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceContext.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceModel.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceController.cs`
- Modify: `scripts/save/PrologueSaveData.cs`
- Modify: `scripts/save/PrologueSaveStore.cs`
- Modify: `scripts/prologue/ProloguePlaytestSession.cs`
- Modify: `scripts/prologue/ProloguePlaytestReport.cs`
- Modify: `scripts/prologue/ProloguePlaytestRecorder.cs`
- Modify: `tests/unit/PrologueObjectiveTests.cs`
- Modify: `tests/unit/PrologueProgressTests.cs`
- Modify: `tests/unit/PrologueGuidanceModelTests.cs`
- Modify: `tests/unit/PrologueSaveTests.cs`
- Modify: `tests/unit/ProloguePlaytestSessionTests.cs`
- Modify: `tests/integration/PrologueGuidanceRuntimeTests.cs`
- Modify: `tests/integration/PrologueCompletionTests.cs`
- Modify: `tests/integration/PrologueLevelRuntimeTests.cs`
- Modify: `tests/integration/ProloguePlaytestRecorderTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Consumes: Task 7 sector IDs/checkpoints and Task 8 route markers.
- Produces continuous objective sequence `Wake → LearnAttackMode → LearnGrappleMode → ReachBasin → LearnShield → ClimbCanyon → EnterTemple → PullTempleMechanism → DefeatBellkeeper → Complete`.
- Produces checkpoint snapshot fields `SectorId`, `WorldPosition`, `ShortcutOpen`, progress snapshot; transient energy/heat/claw/shield are deliberately excluded.
- Produces playtest report fields `sectorsVisited`, `firstCheckpoint`, `grappleFailuresByReason`, `shieldOverheats`, `modeSwitches`, `selectedSlots` while retaining report version migration.

- [ ] **Step 1: Write objective and checkpoint RED tests**

Assert the exact sequence, no skipped objective on same-frame transitions, basin checkpoint cannot be restored before activation, temple checkpoint supersedes basin only after activation, death restores full energy/zero heat/no shield/no claw and preserves shortcut/discoveries.

- [ ] **Step 2: Write guidance RED tests**

Require first-level-only prompts for Q/E, 1–4, attack energy, shield/overheat, GripStone/Ice and A/D/W/S/release. Every prompt resolves glyphs from InputMap and a live world target from the current mounted sector. No text contains `J`, `K`, “上下路线任选其一” or old room names.

- [ ] **Step 3: Write save migration RED tests**

Load schema-v1 room-coordinate data and assert only settings/completion flags migrate; room ID, entrance ID and transient combat values are discarded. Save schema-v2 round-trips sector-independent progress and rejects corrupt/non-finite world positions.

- [ ] **Step 4: Write telemetry RED tests**

Start through `HandsOnLauncher`, visit all five sectors, activate both checkpoints, produce one ice failure and one overheat, complete Boss, close and assert exact JSON/Markdown fields. Direct editor/capture runs must not write `HandsOnLauncher` evidence.

- [ ] **Step 5: Implement progress and restore pipeline**

Move objective decisions from mounted room identity to sector/progress events. On recovery call player reset methods in deterministic order, then position at the activated checkpoint. Never derive checkpoint selection from furthest sector loaded.

- [ ] **Step 6: Implement new guidance and save/report schemas**

Use stable sector target keys. Update localization strings in both languages. Increment save/report schema versions and keep explicit migration functions with tests.

- [ ] **Step 7: Run GREEN and real death routes**

Run unit/integration suites plus a real smoke route containing death before basin, death after basin, death after temple, shortcut persistence and Boss retry. Require each restore position and transient cleanup in the same physics frame.

- [ ] **Step 8: Commit**

```powershell
git commit -m "feat: migrate prologue progress to seamless canyon"
```

---

### Task 10: Final HUD, Reticle, Audio/VFX and Replacement-Slot Migration

**Files:**
- Create: `scripts/ui/PrologueActionModeVisual.cs`
- Create: `scripts/ui/PrologueEnergyHeatVisual.cs`
- Create: `scripts/ui/PrologueLoadoutVisual.cs`
- Create: `scripts/ui/PrologueAimReticle.cs`
- Modify: `scripts/ui/PrologueFormalHud.cs`
- Modify: `scripts/ui/PrologueControlsView.cs`
- Modify: `scripts/ui/InputGlyphResolver.cs`
- Modify: `tools/build/ui/BuildPrologueUi.cs`
- Modify generated output: `scenes/ui/PrologueUi.tscn`
- Modify: `scripts/audio/PrologueAudioCue.cs`
- Modify: `scripts/audio/PrologueAudioDirector.cs`
- Modify: `scripts/vfx/PrologueVfxDirector.cs`
- Modify: `scripts/presentation/ProductionAssetManifest.cs`
- Modify: `scripts/presentation/RoomVisualCatalog.cs`
- Modify: `tools/build/art/BuildPrologueVfx.cs`
- Modify: `tools/build/art/BuildPrologueEnvironmentKit.cs`
- Modify: `scripts/shared/InputActions.cs`
- Modify: `tools/configure/ConfigureProject.cs`
- Modify: `tests/unit/InputGlyphResolverTests.cs`
- Modify: `tests/unit/PrologueHudViewModelTests.cs`
- Modify: `tests/unit/PrologueAudioManifestTests.cs`
- Modify: `tests/unit/ProductionAssetManifestTests.cs`
- Modify: `tests/unit/PresentationCueCoverageTests.cs`
- Modify: `tests/integration/PrologueFormalHudTests.cs`
- Modify: `tests/integration/PrologueUiStructureTests.cs`
- Modify: `tests/integration/PrologueUiRuntimeTests.cs`
- Modify: `tests/integration/PrologueAccessibilityTests.cs`
- Modify: `tests/integration/PrologueAudioDirectorTests.cs`
- Modify: `tests/integration/PrologueVfxRuntimeTests.cs`
- Modify: `tests/integration/PrologueProductionAssetRuntimeTests.cs`

**Interfaces:**
- Consumes all runtime frames from Tasks 4, 6 and 9.
- Produces shape-plus-color reticle states `Attack`, `Grippable`, `Ice`, `Obstructed`, `OutOfRange`, `Retracting`.
- Produces typed cues `ModeAttack`, `ModeGrapple`, `EmptySlot`, `EnergyEmpty`, `ShieldRaise`, `ShieldBlock`, `ShieldHeavyBreak`, `ShieldOverheat`, `ShieldReady`, `ClawFire`, `ClawTravel`, `ClawLatch`, `ClawTension`, `ClawRelease`, `ClawIceSlip`, `ClawFail`.
- Produces stable visual roots required by the spec for HUD/player/sectors.

- [ ] **Step 1: Write UI structure and resolution RED tests**

Require every stable path and non-overlap at 1280×720, 1920×1080, 2560×1440 and 3840×2160. Each reticle state must differ by geometry/line style as well as color. Heat over 100 and ratios outside `[0,1]` are clamped.

- [ ] **Step 2: Write typed audio/VFX RED tests**

Trigger every cue from a real runtime transition. Require exactly one cue at transition entry, no cue polling spam, correct audio bus and complete VFX cleanup on death, pause, sector unload and completion.

- [ ] **Step 3: Write replacement-boundary RED tests**

Require stable HUD/player/sector paths, gameplay collision outside all `VisualRoot` subtrees, no placeholder script owning combat state, and a production manifest entry for each replaceable slot. Removing or moving one named slot must fail the structural gate.

- [ ] **Step 4: Implement focused UI controls and reticle**

Each control consumes an immutable view-model frame and draws only its own layer. The reticle follows screen-projected world cursor and uses open circle/diamond/crossed ice bars/dashed obstruction arc/range ring/retract spiral for the six states.

- [ ] **Step 5: Add deterministic placeholder cues and VFX**

Use existing fixed-seed synthesis and audio manifest conventions. Keep peak below -0.9 dBFS and captured mix below the current limiter/headroom gates. Add shield fan, block flash, heavy-break ripple, overheat vent, claw material hit and rope tension effects under replaceable roots.

- [ ] **Step 6: Remove old controls and stale text**

Erase formal `attack`/`thread` J/K actions from InputMap, remove their runtime readers, update pause controls and localization, and scan source/generated scenes for player-facing `J`, `K`, old thread cooldown and route-choice text. Retain legacy class names only where adapters need them, not as active input.

- [ ] **Step 7: Run GREEN**

Build UI/VFX/environment/player/level, run prologue tests and four-resolution capture. Require exact cue coverage, stable paths, accessibility and zero stale-control matches in player-facing assets.

- [ ] **Step 8: Commit**

```powershell
git commit -m "feat: finish canyon controls feedback and ui"
```

---

### Task 11: Replace Verification, Capture and Performance Evidence for the New Prologue

**Files:**
- Modify: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tests/presentation/BossRetryPresentation.cs`
- Modify: `tests/presentation/PrologueVisualAudit.cs`
- Modify: `tools/verify-prologue.ps1`
- Modify: `tools/audit-prologue-production.ps1`
- Modify: `tools/capture-prologue.ps1`
- Create: `tools/extract-prologue-keyframes.ps1`
- Modify: `tests/powershell/PrologueEvidenceChecks.Tests.ps1`
- Modify: `tests/powershell/Task4P0SemanticResources.Tests.ps1`
- Modify: `tests/powershell/Task4P0SmokeCheckpoint.Tests.ps1`
- Modify: `tests/powershell/Task4ProloguePhaseCoverage.Tests.ps1`
- Modify: `tests/powershell/Task4PlaytestRecorderTransition.Tests.ps1`
- Create: `tests/powershell/PrologueKeyframeExtraction.Tests.ps1`
- Modify: `tools/play-prologue.ps1`
- Modify: `README.md`
- Modify: `docs/playtests/2026-08-14-prologue-first-level.md`
- Create: `docs/playtests/2026-08-26-canyon-prologue-validation.md`

**Interfaces:**
- Consumes the complete runtime from Tasks 1–10.
- Produces exact terminal markers `PROLOGUE_CRITICAL_PATH_OK`, `PROLOGUE_CANYON_TRAVERSAL_OK`, `PROLOGUE_PRESENTATION_COMPLETE`, `PROLOGUE_BOSS_RETRY_COMPLETE`, `PROLOGUE_PRODUCTION_AUDIT_OK`, `PROLOGUE_VERIFY_OK`.
- Produces fresh 1280×720@60 FPS full-flow and Boss-retry videos plus reviewed keyframes.

- [ ] **Step 1: Write fail-closed evidence contracts**

Update PowerShell tests to reject missing Q/E/mouse/slot proof, missing energy/overheat proof, missing each surface material, fewer than five sectors, any room teleport marker, mandatory Dash, stale J/K prompt, absent checkpoint restore, absent Boss retry, empty/frozen movie, wrong resolution/FPS, performance peak over 50 ms or forbidden logs.

- [ ] **Step 2: Run evidence RED**

Run the PowerShell evidence suites against the old drivers. Expected: named failures for old topology and missing new markers.

- [ ] **Step 3: Rebuild real critical-path smoke**

Drive InputMap through all five sectors using Q/E, mouse coordinates, slot 1, attack energy, shield normal/heavy/overheat, GripStone, Ice rejection, three ascent grapples, mechanism, both checkpoints, shortcut, death/recovery, all Boss attacks and completion. Keep a bounded frame budget with at least 10% headroom.

- [ ] **Step 4: Rebuild deterministic presentation drivers**

Show title/start, mode HUD, attack/energy, shield/overheat, claw reticle states, descent, basin, three ascents, optional alcove, temple, Boss intro/all attacks/death/retry/completion/title return. Every demonstration uses real runtime state; only the presentation driver may reposition between non-timing proof beats.

- [ ] **Step 5: Rebuild production audit**

Audit all five sectors, every stable visual root, all typed cues/VFX, continuous camera boundaries, exact surface proofs, mounted-sector bounds and callback wall-clock performance. Preserve Windows display and fixed 60 FPS.

- [ ] **Step 6: Run complete verifier twice**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Expected from each fresh process: build 0 warnings/errors, all builders/tests/smoke/audits/capture contracts green, one exact `PROLOGUE_VERIFY_OK`, no fatal/error/leak signature.

- [ ] **Step 7: Capture and inspect final proof**

Run `tools/capture-prologue.ps1`; verify AVI frame counts, 1280×720, 60 FPS and audio headroom. Extract keyframes for every listed beat, inspect them at original resolution and record the reviewed paths in the validation document.

- [ ] **Step 8: Update durable status**

Update README build/remaining-work/asset table, validation document and playtest audit with exact commit, commands, markers, performance peak, movie frames, keyframe paths and the unchanged no-paid-generation status. Mark normal-speed human acceptance open until it actually occurs.

- [ ] **Step 9: Commit automated certification**

```powershell
git commit -m "test: certify seamless canyon prologue"
```

- [ ] **Step 10: Launch normal-speed human playtest**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1
```

Require a no-long-idle 10–15 minute complete run, record the least smooth or least clear moment, fix the owning subsystem with a RED test, rerun the proportional and full gates, and append the final report ID plus issue closure to README and the validation document.

- [ ] **Step 11: Commit human closure only after evidence exists**

Stage only the resulting docs and any test-backed fix, then commit:

```powershell
git commit -m "docs: close canyon prologue human playtest"
```

Do not claim the milestone complete before this commit has an actual `HandsOnLauncher` report and qualitative note.
