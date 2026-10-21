# Complete Prologue First Level Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build and polish a directly playable 10–15 minute first level that teaches the mechanical thread, supports a compact exploration loop, ends in a two-phase Fallen Bellkeeper encounter, and keeps all placeholder presentation replaceable.

**Architecture:** Keep deterministic gameplay models independent from Godot adapters, keep presentation coordinators downstream of gameplay state, and mount replaceable visuals below stable `VisualRoot` nodes. Six generated room scenes expose typed entrance/exit contracts and are streamed by one persistent `PrologueLevelController`; an in-memory `PrologueProgress` owns checkpoints, shortcut and tutorial state. Existing technical-prototype scenes remain runnable until the final prologue gate replaces the main scene.

**Tech Stack:** Godot 4.7.1 Mono, C# / .NET 9, generated `.tscn` scenes, PowerShell verification, fixed-physics-frame Godot SceneTree tests and movie capture.

## Global Constraints

- Work in `E:\OurGame\Knight\.worktrees\player-art-pipeline`; preserve the unrelated uncommitted changes in `tests/art/test_complete_master_review.py` and `tools/art/complete_master_review.py`.
- Use Godot 4.7.1 Mono from `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe` and .NET 9 from the runtime selected by `tools/verify.ps1`.
- All Godot C# classes are `partial`; pure models remain plain sealed C# classes.
- Use 60 physics ticks per second and InputMap actions only; no gameplay class reads physical keys.
- No paid generation and no formal art. P0–P3 use project-owned procedural placeholders.
- Gameplay never reads texture paths or animation frames; presentation never changes health, cooldowns, collision, encounter completion or level progress.
- Leaf scenes are generated before rooms and rooms before `PrologueLevel.tscn`; every builder must pack, instantiate and validate required paths.
- Each task follows strict RED → observed expected failure → minimal GREEN → full relevant verification → commit.
- Keep the technical prototype gate green until Task 12 deliberately makes the prologue the default main scene.

## Planned File Structure

**Shared configuration and presentation**

- `scripts/config/PlayerTuning.cs` — validated motion/combat values and conversion to existing pure settings records.
- `scripts/config/ThreadTuning.cs` — validated thread selection, pull and presentation values.
- `scripts/config/EnemyArchetypeTuning.cs` — validated common enemy timing and movement values.
- `scripts/config/PresentationProfile.cs` — optional packed visual/effect overrides with a validated procedural fallback.
- `scripts/config/RoomPresentationProfile.cs` — optional static room visual scene with a validated Node2D fallback.
- `scripts/presentation/EntityVisualState.cs`, `PresentationCue.cs`, `EntityPresentationFrame.cs`, `IEntityVisual2D.cs` — art-independent presentation contract.
- `scripts/presentation/PlayerPresentationCoordinator.cs` — maps player runtime state/events to the visual contract.
- `scripts/presentation/PlayerPlaceholderVisual.cs` — procedural P0 visual mounted under `VisualRoot`.
- `scripts/presentation/RustShellPresentationCoordinator.cs`, `RustShellPlaceholderVisual.cs` — maps the existing squire model to the same visual contract.

**Level state and rooms**

- `scripts/prologue/PrologueRoomId.cs`, `PrologueRoomLink.cs`, `PrologueRouteGraph.cs` — typed six-room graph with stable StringName entrance/exit IDs.
- `scripts/prologue/RoomDefinition.cs` — validated packed room, start entrance, connections, camera and encounter metadata.
- `scripts/prologue/PrologueProgress.cs`, `PrologueCheckpointSnapshot.cs` — in-memory durable session state.
- `scripts/prologue/RoomExit.cs`, `CheckpointTrigger.cs`, `PrologueRoom.cs`, `PrologueLevelController.cs` — room/transition/recovery adapters.
- `scripts/prologue/EncounterDefinition.cs`, `EncounterController.cs` — bounded encounter configuration and reset.
- `scripts/prologue/PrologueHud.cs`, `TutorialPromptTrigger.cs` — HUD and one-shot contextual teaching.
- `tools/build/prologue/PrologueBuildParts.cs` — shared primitive geometry/marker helpers only.
- `tools/build/prologue/Build*.cs` — six room builders plus final level builder.

**Enemies, mechanism and Boss**

- `scripts/enemies/moth/*` — Grave-Lamp Moth pure model, adapter and placeholder visual.
- `scripts/enemies/laborer/*` — Bell-Back Laborer pure model, adapter and placeholder visual.
- `scripts/mechanisms/ThreadInteraction.cs`, `IThreadInteractionReceiver.cs`, `BellChainMechanism.cs` — target-agnostic thread interactions and shortcut mechanism.
- `scripts/boss/*` — Fallen Bellkeeper pure model, armor gate, adapter and placeholder visual.

**Verification and evidence**

- `tests/RunPrologueTests.cs` plus unit, scene and integration tests named below.
- `tests/smoke/PrologueCriticalPathSmoke.cs` — deterministic real-input critical path.
- `tests/presentation/ProloguePresentation.cs` and `BossRetryPresentation.cs` — deterministic proof capture drivers.
- `tools/verify-prologue.ps1` and `tools/capture-prologue.ps1` — final gates and proof orchestration.

---

## P0 — Core Slice

### Task 1: Validated tuning injection and replaceable player presentation

**Files:**
- Create: `scripts/config/PlayerTuning.cs`
- Create: `scripts/config/ThreadTuning.cs`
- Create: `scripts/config/PresentationProfile.cs`
- Create: `scripts/presentation/EntityVisualState.cs`
- Create: `scripts/presentation/PresentationCue.cs`
- Create: `scripts/presentation/EntityPresentationFrame.cs`
- Create: `scripts/presentation/IEntityVisual2D.cs`
- Create: `scripts/presentation/PlayerPresentationCoordinator.cs`
- Create: `scripts/presentation/PlayerPlaceholderVisual.cs`
- Create: `scripts/presentation/RustShellPresentationCoordinator.cs`
- Create: `scripts/presentation/RustShellPlaceholderVisual.cs`
- Modify: `scripts/player/PlayerMotor.cs`
- Modify: `scripts/player/PlayerCombatModel.cs`
- Modify: `scripts/player/PlayerCombat.cs`
- Modify: `scripts/abilities/ThreadAbility.cs`
- Modify: `scripts/enemies/RustShellSquire.cs`
- Modify: `tools/build/BuildPlayer.cs`
- Modify: `tools/build/BuildRustShellSquire.cs`
- Create: `tests/unit/TuningContractTests.cs`
- Create: `tests/unit/PlayerPresentationTests.cs`
- Create: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: `PlayerTuning.Validate(): IReadOnlyList<string>`, `PlayerTuning.ToMotionSettings()`, `PlayerTuning.ToCombatSettings()`.
- Produces: `ThreadTuning.Validate(): IReadOnlyList<string>`, `ThreadTuning.ToSettings()`.
- Produces: `PresentationProfile.Resolve(Node visualRoot): IEntityVisual2D`; an invalid override logs once and uses the procedural child fallback.
- Produces: `IEntityVisual2D.ApplyFrame(EntityPresentationFrame frame)` and `PlayCue(PresentationCue cue)`.
- Produces: stable Player and Rust-Shell paths `Presentation` and `VisualRoot/Placeholder`.
- Consumes: existing `PlayerMotionModel`, `PlayerCombatModel`, `ThreadAbilityModel`, `HealthComponent` signals and InputMap frames.

- [ ] **Step 1: Write failing tuning and presentation tests**

Add table-driven literal boundary cases to `TuningContractTests`: every speed/acceleration/gravity/jump/dash value rejects `0`; coyote and jump-buffer time reject values below `0`; attack startup/recovery reject values below `0`; attack active time and damage reject `0`; knockback rejects values below `0`; down-bounce rejects `0`; aim dead zone rejects values below `0` or above `1`; thread range/pull speeds reject `0`; cone rejects `0` and `181`; cooldown/visual time reject values below `0`. Default tunings have no errors and each invalid field produces an error containing that exact property name.

Add a fake `IEntityVisual2D` in `PlayerPresentationTests` and assert that stationary/grounded maps to `Idle`, positive X velocity maps to `Move` facing right, negative Y maps to `Rise`, positive Y maps to `Fall`, active dash maps to `Dash`, and `PresentationCue.AttackHit` reaches the fake exactly once. Add Rust-Shell mapping assertions for `Idle`, `Move`, `Attack`, `Hurt` cue and `Dead`. Test `PresentationProfile` with a valid packed fake visual, an invalid packed Node2D plus procedural fallback, and no override plus procedural fallback.

Register both suites in `RunPrologueTests` and print exactly `PROLOGUE_TESTS_OK` after all registered tests return.

- [ ] **Step 2: Run RED and confirm the missing contracts are the cause**

Run:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
```

Expected: build fails with missing `Knight.Config.PlayerTuning`, `ThreadTuning`, `PresentationProfile`, or `Knight.Presentation.IEntityVisual2D`; no syntax-only failure is accepted as RED.

- [ ] **Step 3: Implement the minimal validated resources and coordinator**

Use exported Godot resources so builders can serialize values while pure models still consume immutable records. `PlayerTuning` contains every field currently defined by `PlayerMotionSettings` and `PlayerCombatSettings`; `ThreadTuning` contains every field currently defined by `ThreadAbilitySettings`. Conversions pass every property positionally so changing an inspector value cannot silently fall back to a record default:

```csharp
[GlobalClass]
public partial class PlayerTuning : Resource
{
    [Export] public float MaxRunSpeed { get; set; } = 260f;
    [Export] public float GroundAcceleration { get; set; } = 2200f;
    [Export] public float GroundDeceleration { get; set; } = 2800f;
    [Export] public float AirAcceleration { get; set; } = 1500f;
    [Export] public float Gravity { get; set; } = 1850f;
    [Export] public float MaxFallSpeed { get; set; } = 780f;
    [Export] public float JumpSpeed { get; set; } = 590f;
    [Export] public float JumpCutSpeed { get; set; } = 230f;
    [Export] public float CoyoteSeconds { get; set; } = 0.10f;
    [Export] public float JumpBufferSeconds { get; set; } = 0.12f;
    [Export] public float DashSpeed { get; set; } = 620f;
    [Export] public float DashSeconds { get; set; } = 0.14f;
    [Export] public float AttackStartupSeconds { get; set; } = 0.06f;
    [Export] public float AttackActiveSeconds { get; set; } = 0.10f;
    [Export] public float AttackRecoverySeconds { get; set; } = 0.14f;
    [Export] public int AttackDamage { get; set; } = 1;
    [Export] public float AttackKnockback { get; set; } = 260f;
    [Export] public float DownBounceSpeed { get; set; } = 360f;
    [Export] public float AimDeadZone { get; set; } = 0.5f;

    public PlayerMotionSettings ToMotionSettings() => new(
        MaxRunSpeed, GroundAcceleration, GroundDeceleration, AirAcceleration,
        Gravity, MaxFallSpeed, JumpSpeed, JumpCutSpeed, CoyoteSeconds,
        JumpBufferSeconds, DashSpeed, DashSeconds);

    public PlayerCombatSettings ToCombatSettings() => new(
        AttackStartupSeconds, AttackActiveSeconds, AttackRecoverySeconds,
        AttackDamage, AttackKnockback, DownBounceSpeed, AimDeadZone);
}
```

`Validate()` returns a new list and adds `"<PropertyName> must ..."` for each boundary named in Step 1; it does not clamp invalid authored data. `ThreadTuning.ToSettings()` passes `Range`, `ConeHalfAngleDegrees`, `CooldownSeconds`, `PlayerPullSpeed`, `EnemyPullSpeed` and `VisualSeconds` to `ThreadAbilitySettings` in that order.

Expose `[Export] PlayerTuning Tuning` on `PlayerMotor` and `PlayerCombat`, instantiate their models in `_Ready()` from the injected resource, and fail with `InvalidOperationException` when validation returns errors. Apply the same pattern to `ThreadAbility` with `ThreadTuning`. Add read-only `PlayerMotor.IsDashing`, `PlayerCombatModel.IsAttacking` and `PlayerCombat.IsAttacking` properties for downstream presentation; they never accept writes from presentation. Add `ThreadFired`, `ThreadHit` and `ThreadFailed` signals to ThreadAbility so the coordinator can emit cues without reconstructing selection results.

`PresentationProfile.Resolve` instantiates `VisualScene` only when its root implements `IEntityVisual2D`; otherwise it frees the invalid root, emits one warning and resolves the existing procedural `IEntityVisual2D` child under VisualRoot. If neither exists it throws an `InvalidOperationException` naming the owner path. The profile also exposes `Godot.Collections.Dictionary<StringName, PackedScene> CueScenes`; `TryInstantiateCue(PresentationCue cue, out Node2D effect)` accepts only Node2D roots and returns false with one warning for a malformed optional effect. Coordinators add valid cue instances under `VisualRoot/Effects`; placeholder visuals still receive `PlayCue` when no effect scene is configured.

`PlayerPresentationCoordinator` reads body velocity, floor state, dash/attack state and health/thread events, sends `EntityPresentationFrame` to the resolved `IEntityVisual2D`, and never writes gameplay state. `PlayerPlaceholderVisual` replaces `PlayerDebugVisual` behavior but lives at `VisualRoot/Placeholder` and changes geometry for locomotion/attack/hurt instead of relying only on color.

Move the Rust-Shell's calls to `SetFrame`/`FlashDamage` out of `RustShellSquire` and into `RustShellPresentationCoordinator`. The gameplay adapter exposes its current frame/facing read-only and emits damage/death cues; `RustShellPlaceholderVisual` consumes the shared interface under `VisualRoot/Placeholder`. This makes the enemy used by P0 obey the same replacement rule as the player.

- [ ] **Step 4: Regenerate Player and run GREEN**

Run the test runner, then builder:

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs --log-file p0-task1-tests.log
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/BuildPlayer.cs --log-file p0-task1-builder.log
```

Expected: one `PROLOGUE_TESTS_OK`; both builders validate `Presentation`, `VisualRoot`, `VisualRoot/Placeholder`, `VisualRoot/Effects`, each placeholder implements `IEntityVisual2D`, injected tuning/profile resources, existing combat paths, and equal pre/post pack node counts.

- [ ] **Step 5: Protect the visual boundary with a mutation check**

Temporarily remove the `IEntityVisual2D` implementation from `PlayerPlaceholderVisual`; rerun `PlayerPresentationTests` and confirm the scene contract fails because no visual consumer is found. Restore it. Then make the Rust-Shell gameplay adapter call its old visual directly and confirm the presentation-boundary test fails because gameplay touched the placeholder fake; restore and rerun GREEN.

- [ ] **Step 6: Commit**

```powershell
git add scripts/config scripts/presentation scripts/player/PlayerMotor.cs scripts/player/PlayerCombatModel.cs scripts/player/PlayerCombat.cs scripts/abilities/ThreadAbility.cs scripts/enemies/RustShellSquire.cs tools/build/BuildPlayer.cs tools/build/BuildRustShellSquire.cs scenes/player/Player.tscn scenes/enemies/RustShellSquire.tscn tests/unit/TuningContractTests.cs tests/unit/PlayerPresentationTests.cs tests/RunPrologueTests.cs
git commit -m "feat: add replaceable player presentation"
```

### Task 2: Typed prologue route, progress and checkpoint model

**Files:**
- Create: `scripts/prologue/PrologueRoomId.cs`
- Create: `scripts/prologue/PrologueRoomLink.cs`
- Create: `scripts/prologue/PrologueRoomLinkResource.cs`
- Create: `scripts/prologue/PrologueRouteGraph.cs`
- Create: `scripts/prologue/RoomDefinition.cs`
- Create: `scripts/prologue/PrologueCheckpointSnapshot.cs`
- Create: `scripts/prologue/PrologueProgress.cs`
- Create: `tests/unit/PrologueRouteGraphTests.cs`
- Create: `tests/unit/PrologueProgressTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: six `PrologueRoomId` values: `AwakeningCourtyard`, `BrokenToothCorridor`, `UpperPendulum`, `LowerBuriedMachinery`, `BellChainMechanism`, `FallenBellkeeperArena`.
- Produces: `PrologueRouteGraph.TryResolve(PrologueRoomId room, StringName exitId, out PrologueRoomLink link)` and `ValidateRequiredRooms(IReadOnlySet<PrologueRoomId> requiredRooms)`.
- Produces: `RoomDefinition.Validate(): IReadOnlyList<string>` over `RoomId`, `PackedScene Scene`, non-empty `StartEntrance`, positive `CameraBounds`, optional checkpoint ID, encounter resources and outgoing `PrologueRoomLink` entries.
- Produces: `PrologueProgress.ActivateCheckpoint(...)`, `OpenBellChainShortcut()`, `VisitHiddenAlcove()`, `CompleteTutorial(StringName id)`, `MarkBossDefeated()` and `RestoreCheckpoint()`.

- [ ] **Step 1: Write failing route and state tests**

Use hand-written links and literal expectations. Assert both corridor exits reach upper/lower rooms, both branch exits reach the mechanism, the permanent shortcut returns to the corridor checkpoint entrance, reverse links allow backtracking, unknown exits fail and duplicate `(room, exit)` keys are rejected. A graph containing only courtyard/corridor passes when those two are the declared required set; the same graph fails `ValidateRequiredRooms` when the declared set contains the Boss. Add RoomDefinition cases that reject a null PackedScene, empty start entrance, non-positive camera width/height, duplicate outgoing exit IDs, a link whose source room differs from the definition, a null encounter resource and a checkpoint with an empty ID. Accept a literal courtyard definition with one start entrance, one corridor link and no encounter. Duplicate encounter IDs are tested in Task 6 after EncounterDefinition exists.

For progress, assert checkpoint snapshots preserve room/entrance, shortcut and hidden discovery; ordinary enemy deaths are absent from the snapshot; tutorial IDs deduplicate; invalid checkpoint room/entrance is rejected; restoring an uninitialized checkpoint returns the courtyard start.

- [ ] **Step 2: Run RED**

Run the prologue test runner and confirm compile failure names `PrologueRouteGraph` or `PrologueProgress`.

- [ ] **Step 3: Implement immutable links and mutable session progress**

`RoomDefinition` is a `[GlobalClass] Resource` containing exported `RoomId`, `PackedScene Scene`, `StringName StartEntrance`, `Rect2 CameraBounds`, `StringName CheckpointId`, `Godot.Collections.Array<Resource> EncounterResources` and `Godot.Collections.Array<PrologueRoomLinkResource>`. Task 2 accepts an empty encounter array and rejects null entries; Task 6 validates that every entry is an `EncounterDefinition` without changing RoomDefinition consumers. `PrologueRoomLinkResource` converts to the immutable `PrologueRoomLink` record used by the graph.

`PrologueRouteGraph` owns a read-only dictionary keyed by `(PrologueRoomId, StringName)` and validates duplicate IDs plus links whose source or destination is outside its provided RoomDefinitions. `ValidateRequiredRooms` separately verifies the stage's declared set. P0 declares courtyard/corridor, P1 adds both branches and the mechanism, and P2/P3 declare all six rooms; the final gate has a literal all-six assertion. `PrologueProgress` owns only session state; its snapshot is a record copied on checkpoint activation. Neither class stores live scene nodes.

```csharp
public readonly record struct PrologueCheckpointSnapshot(
    PrologueRoomId Room,
    StringName Entrance,
    bool BellChainShortcutOpen,
    bool HiddenAlcoveVisited,
    IReadOnlySet<StringName> CompletedTutorials);
```

- [ ] **Step 4: Run GREEN and mutations**

Confirm `PROLOGUE_TESTS_OK`. Mutate the lower-route destination to `UpperPendulum` and confirm the literal lower-route assertion fails; restore. Mutate snapshot restoration to clear `BellChainShortcutOpen` and confirm the persistence assertion fails; restore.

- [ ] **Step 5: Commit**

```powershell
git add scripts/prologue tests/unit/PrologueRouteGraphTests.cs tests/unit/PrologueProgressTests.cs tests/RunPrologueTests.cs
git commit -m "feat: define prologue route and progress"
```

### Task 3: Persistent level shell, room contracts, HUD and recovery

**Files:**
- Create: `scripts/prologue/ILevelRecovery.cs`
- Create: `scripts/prologue/ICheckpointResettable.cs`
- Create: `scripts/prologue/RoomExit.cs`
- Create: `scripts/prologue/CheckpointTrigger.cs`
- Create: `scripts/prologue/PrologueRoom.cs`
- Create: `scripts/prologue/PrologueLevelController.cs`
- Create: `scripts/prologue/PrologueHud.cs`
- Create: `scripts/prologue/TutorialPromptTrigger.cs`
- Create: `scripts/config/RoomPresentationProfile.cs`
- Modify: `scripts/world/KillZone.cs`
- Modify: `scripts/enemies/RustShellSquire.cs`
- Create: `tests/unit/PrologueRoomContractTests.cs`
- Create: `tests/integration/PrologueLevelRuntimeTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: `ILevelRecovery.RequestRespawn(string reason)` and `ICheckpointResettable.ResetForCheckpoint()`.
- Produces: `RoomExit.ExitRequested(StringName exitId)`.
- Produces: `PrologueRoom.ValidateContract(): IReadOnlyList<string>` and `ResetRoomState()`.
- Produces: `RoomPresentationProfile.Resolve(Node2D visualRoot): Node2D`; a valid override root must be Node2D, otherwise the authored `Presentation/VisualRoot/Placeholder` remains visible and one warning is logged.
- Produces: `PrologueLevelController.TransitionTo(PrologueRoomLink link)` and `RequestRespawn(string reason)`.
- Consumes: validated `RoomDefinition` resources, `PrologueRouteGraph`, `PrologueProgress`, player stable gameplay paths and packed room scenes.

- [ ] **Step 1: Write failing contract and runtime tests**

Create synthetic rooms with real `Marker2D`, `Area2D` and `Rect2` data. Assert validation rejects missing `Entrances`, missing requested entrance, duplicate exit IDs, absent `KillZone` and non-positive camera bounds. In runtime tests, load a synthetic room A, cross a real `RoomExit` with a player body, and assert one transition to room B preserves the same player instance ID and places it at the target entrance. Trigger a real KillZone and assert velocity, combat, thread cooldown and every `ICheckpointResettable` reset after one physics frame.

- [ ] **Step 2: Run RED**

Confirm the first expected failure is missing `PrologueRoom`/`PrologueLevelController` rather than an invalid test fixture.

- [ ] **Step 3: Implement the shell without production rooms**

`PrologueLevelController` contains persistent `PlayerRoot`, `RoomRoot`, `Hud` and `Fade` children plus exported arrays of `RoomDefinition` resources and `RequiredRooms`. It validates all definitions, links and the current stage's required-room set in `_Ready()` before loading the first room. Transition sequence: disable player gameplay → detach player → free old room → instantiate the destination definition's PackedScene → validate room → resolve its RoomPresentationProfile below `Presentation/VisualRoot` → add player → place at target `Marker2D` → apply the definition's camera bounds → enable gameplay next physics frame.

Refactor KillZone to resolve `ILevelRecovery` through its configured controller path. Make `RustShellSquire` implement `ICheckpointResettable`; retain `ResetForCheckpoint()` as its implementation.

`RoomPresentationProfile` behaves like the entity profile without requiring `IEntityVisual2D`: it accepts only a Node2D packed root, mounts it under `Presentation/VisualRoot/Override`, hides the placeholder on success, and keeps the placeholder visible after a missing or malformed override.

`PrologueHud` exposes methods `SetHealth(int current,int max)`, `SetThreadReady(bool ready)`, `SetObjective(string text)`, `ShowPrompt(string text)`, `ShowBoss(...)`, and `HideBoss()`. It only updates Controls.

- [ ] **Step 4: Run GREEN and existing regression gate**

Run `RunPrologueTests.cs`, then `tools/verify.ps1 -SkipSmoke`. Expected: prologue tests green and old `PROTOTYPE_TESTS_OK`; PrototypeRoom still loads with refactored KillZone.

- [ ] **Step 5: Commit**

```powershell
git add scripts/config/RoomPresentationProfile.cs scripts/prologue scripts/world/KillZone.cs scripts/enemies/RustShellSquire.cs tests/unit/PrologueRoomContractTests.cs tests/integration/PrologueLevelRuntimeTests.cs tests/RunPrologueTests.cs
git commit -m "feat: add persistent prologue level shell"
```

### Task 4: P0 Awakening Courtyard and Broken-Tooth Corridor

**Files:**
- Create: `tools/build/prologue/PrologueBuildParts.cs`
- Create: `tools/build/prologue/BuildAwakeningCourtyard.cs`
- Create: `tools/build/prologue/BuildBrokenToothCorridor.cs`
- Create: `tools/build/prologue/BuildPrologueLevel.cs`
- Create: `scenes/prologue/rooms/AwakeningCourtyard.tscn`
- Create: `scenes/prologue/rooms/BrokenToothCorridor.tscn`
- Create: `scenes/prologue/PrologueLevel.tscn`
- Create: `tests/unit/P0RoomStructureTests.cs`
- Create: `tests/smoke/P0CoreSliceSmoke.cs`
- Create: `tools/verify-p0.ps1`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes: stable room paths `Entrances`, `Exits`, `CameraBounds`, `KillZone`, `Actors`, `Geometry`, `Presentation/VisualRoot/Placeholder`.
- Produces: P0 route `AwakeningCourtyard/start → BrokenToothCorridor/from_courtyard` and reverse exit.
- Produces: first checkpoint `corridor_lamp` and tutorial IDs `move`, `jump`, `attack`, `thread_anchor`.

- [ ] **Step 1: Write failing P0 structure and real-input smoke**

Structure assertions use literal platform sizes/positions, camera rectangles, unique entrance/exit IDs, Player under persistent level root, one corridor Rust-Shell, one checkpoint and one anchor. The smoke must press InputMap actions to move, jump, attack the squire, activate the checkpoint, aim/thread to cross a gap, then enter the P0 completion exit. It prints exactly `P0_CORE_SLICE_OK` and has a fixed physics-frame hard timeout.

- [ ] **Step 2: Run RED**

Run the structure runner and confirm missing packed P0 scenes. Do not treat a null reference from a malformed smoke as acceptable RED.

- [ ] **Step 3: Build two authored gray-box rooms and the level shell**

Use `PrologueBuildParts` only for platforms, markers, exits, camera bounds, placeholder backdrop, anchor and checkpoint primitives. Builders assign collision layers explicitly and call `SceneBuildUtil.PackAndSave`. The corridor geometry must force one successful anchor interaction; its safe path may be forgiving but may not be bypassed by a single default dash from the teaching ledge.

`BuildPrologueLevel` creates and serializes `RoomDefinition` resources for the available rooms, including scene, start entrance, camera bounds, checkpoint and links; it serializes the current stage's literal `RequiredRooms`, instances Player as persistent child, and leaves `project.godot` pointing at PrototypeRoom until Task 12.

- [ ] **Step 4: Verify P0 and inspect it running**

`tools/verify-p0.ps1` builds C#, generates Player → RustShell → the two rooms → PrologueLevel, imports, runs `PROLOGUE_TESTS_OK` and `P0_CORE_SLICE_OK`, scans every log, then restores generated scene bytes on exit.

Launch:

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . --editor-pid 0 --path . res://scenes/prologue/PrologueLevel.tscn
```

Record the first manual timing and any blocked jump/thread interaction in `README.md`; do not adjust the spec to hide a poor play result.

- [ ] **Step 5: Mutation checks**

Change corridor target entrance ID to an unknown value and confirm structure/runtime validation fails. Restore. Remove `VisualRoot` from the generated Player and confirm P0 structure validation fails while pure motion tests still pass. Restore.

- [ ] **Step 6: Commit**

```powershell
git add tools/build/prologue scenes/prologue scripts/prologue tests/unit/P0RoomStructureTests.cs tests/smoke/P0CoreSliceSmoke.cs tools/verify-p0.ps1 tests/RunPrologueTests.cs README.md
git commit -m "feat: deliver playable prologue core slice"
```

P0 checkpoint: show the running slice to the user before starting P1 tuning-heavy work. Feedback may change room dimensions, player tuning, prompt cadence or placeholder readability through the interfaces already added.

---

## P1 — Compact Exploration Loop

### Task 5: Grave-Lamp Moth archetype

**Files:**
- Create: `scripts/config/EnemyArchetypeTuning.cs`
- Create: `scripts/enemies/moth/GraveLampMothState.cs`
- Create: `scripts/enemies/moth/GraveLampMothInput.cs`
- Create: `scripts/enemies/moth/GraveLampMothFrame.cs`
- Create: `scripts/enemies/moth/GraveLampMothModel.cs`
- Create: `scripts/enemies/moth/GraveLampMoth.cs`
- Create: `scripts/enemies/moth/GraveLampMothPresentationCoordinator.cs`
- Create: `scripts/enemies/moth/GraveLampMothPlaceholderVisual.cs`
- Create: `tools/build/prologue/BuildGraveLampMoth.cs`
- Create: `scenes/enemies/GraveLampMoth.tscn`
- Create: `tests/unit/GraveLampMothModelTests.cs`
- Create: `tests/integration/GraveLampMothRuntimeTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces states `Hover`, `Align`, `Telegraph`, `Dive`, `Recover`, `ThreadStunned`, `Dead`.
- Consumes `EnemyArchetypeTuning`, `PresentationProfile`, `HealthComponent`, Hitbox/Hurtbox, `IThreadPullReceiver`, `ICheckpointResettable`, and `IEntityVisual2D` through stable `Presentation` / `VisualRoot/Placeholder` paths.

- [ ] **Step 1: Write model RED tests**

Assert: no player keeps Hover; detection enters Align; horizontal/vertical alignment enters Telegraph; telegraph duration is inclusive at the configured boundary; Dive ends on duration or world contact; Recover returns to Hover; thread request interrupts every non-dead state into ThreadStunned; stun expiry returns to Hover; zero health enters Dead immediately; Dead never activates an attack.

- [ ] **Step 2: Run RED and implement the pure model**

Confirm missing `GraveLampMothModel`, then add a deterministic `Step(GraveLampMothInput input, double delta)` with no SceneTree/Input calls. Validate all tuning values before model construction.

- [ ] **Step 3: Write runtime RED tests and add the adapter**

In a real physics scene, assert the moth acquires the player, telegraphs before moving at dive speed, damages through its real Hitbox, is pulled downward/toward the player by real ThreadAbility input, enters `ThreadStunned`, lands, and resets to its authored spawn on checkpoint reset.

The gameplay adapter exposes current model frame/facing and cues but never calls the visual. `GraveLampMothPresentationCoordinator` resolves `PresentationProfile`, maps frames/cues and owns effects. The placeholder visual uses a wing/diamond silhouette, compresses during telegraph, elongates during dive and visibly collapses during thread stun.

- [ ] **Step 4: Generate and verify**

Run prologue tests, `BuildGraveLampMoth`, standalone scene load, log scan, and `tools/verify-p0.ps1`. Mutation: ignore thread input in the model and confirm the runtime test fails on state and velocity; restore.

- [ ] **Step 5: Commit**

```powershell
git add scripts/config/EnemyArchetypeTuning.cs scripts/enemies/moth tools/build/prologue/BuildGraveLampMoth.cs scenes/enemies/GraveLampMoth.tscn tests/unit/GraveLampMothModelTests.cs tests/integration/GraveLampMothRuntimeTests.cs tests/RunPrologueTests.cs
git commit -m "feat: add grave lamp moth enemy"
```

### Task 6: Encounter definitions, reset and completion

**Files:**
- Create: `scripts/prologue/IEncounterActor.cs`
- Create: `scripts/prologue/EncounterSpawn.cs`
- Create: `scripts/prologue/EncounterDefinition.cs`
- Create: `scripts/prologue/EncounterController.cs`
- Create: `tests/unit/EncounterDefinitionTests.cs`
- Create: `tests/integration/EncounterControllerTests.cs`
- Modify: `scripts/enemies/RustShellSquire.cs`
- Modify: `scripts/enemies/moth/GraveLampMoth.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: `EncounterDefinition.Validate()`, spawn entries `(PackedScene Scene, NodePath MarkerPath, StringName SpawnId)` and `EncounterController.Activate()`, `ResetForCheckpoint()`, `Completed` signal.
- Consumes: `RoomDefinition.EncounterResources`, enemies implementing `ICheckpointResettable` and emitting death through their HealthComponent.

- [ ] **Step 1: Write definition RED tests**

Reject empty spawn IDs, duplicate IDs, null packed scenes, missing marker paths and an encounter with zero spawns. Accept one Rust-Shell plus one Moth with distinct markers. Expected values are literal.

- [ ] **Step 2: Write runtime RED tests**

Instantiate a real encounter with both packed enemy scenes. Assert activation spawns each exactly once, repeated activation does not duplicate, completion emits once only after both deaths, reset removes old instances and recreates two fresh instances at authored markers, and resetting an inactive encounter does not spawn.

- [ ] **Step 3: Implement minimal controller and enemy death contract**

Add `IEncounterActor : ICheckpointResettable` with `event Action Defeated`; both enemies implement it. Do not branch on Rust-Shell/Moth concrete types. EncounterController owns only its spawned actors and never scans the global tree for unrelated enemies. Extend RoomDefinition validation so every `EncounterResources` entry must be an `EncounterDefinition` and encounter IDs are unique inside the room.

- [ ] **Step 4: Run GREEN and mutation**

Run prologue tests and P0 gate. Temporarily emit completion after the first death and confirm the two-enemy assertion fails; restore.

- [ ] **Step 5: Commit**

```powershell
git add scripts/prologue/IEncounterActor.cs scripts/prologue/EncounterSpawn.cs scripts/prologue/EncounterDefinition.cs scripts/prologue/EncounterController.cs scripts/prologue/RoomDefinition.cs scripts/enemies/RustShellSquire.cs scripts/enemies/moth/GraveLampMoth.cs tests/unit/EncounterDefinitionTests.cs tests/integration/EncounterControllerTests.cs tests/RunPrologueTests.cs
git commit -m "feat: add resettable prologue encounters"
```

### Task 7: Upper/lower loop, hidden alcove and route convergence

**Files:**
- Create: `tools/build/prologue/BuildUpperPendulum.cs`
- Create: `tools/build/prologue/BuildLowerBuriedMachinery.cs`
- Create: `tools/build/prologue/BuildBellChainRoom.cs`
- Create: `scenes/prologue/rooms/UpperPendulum.tscn`
- Create: `scenes/prologue/rooms/LowerBuriedMachinery.tscn`
- Create: `scenes/prologue/rooms/BellChainMechanism.tscn`
- Modify: `tools/build/prologue/BuildBrokenToothCorridor.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Create: `scripts/prologue/HiddenAlcoveTrigger.cs`
- Create: `tests/unit/P1LoopStructureTests.cs`
- Create: `tests/integration/P1LoopRuntimeTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces corridor exits `to_upper` and `to_lower`; both branch rooms expose `to_mechanism` and `to_corridor`.
- Produces a temporary Bell-Chain convergence gray-box with entrances `from_upper` and `from_lower`, reverse exits to both branches and a `p1_complete` marker. It has no mechanism, heavy enemy or Boss exit until later tasks.
- Produces hidden discovery ID `upper_hidden_alcove` without a gameplay-stat reward.
- Consumes: Moth scene, encounter definitions, PrologueProgress and room transition shell.

- [ ] **Step 1: Write structure and route RED tests**

Assert exact entrance/exit IDs, camera bounds, upper-room anchors and rebound target, lower-room Rust-Shell+Moth spawn markers, hidden trigger location, no required upgrade in the hidden room, and that either branch alone reaches the mechanism. Assert returning through the unchosen branch is valid.

- [ ] **Step 2: Run RED and build both rooms**

Confirm missing scenes, then generate authored geometry and encounter resources. The upper route uses vertical space and one Moth; the lower route uses a confined but dodgeable arena with one Rust-Shell and one Moth. Neither branch may place unavoidable contact damage at an entrance marker. Generate the Bell-Chain room as a safe convergence stub so both routes can complete and backtrack before Task 8 adds its encounter and shortcut.

- [ ] **Step 3: Add runtime loop tests**

Drive transitions through real RoomExit areas: corridor → upper → mechanism placeholder, reset; corridor → lower → mechanism placeholder. Visit the hidden trigger and assert it persists across death. Kill one enemy, die before encounter completion and assert both enemies reset.

- [ ] **Step 4: Run GREEN and inspect branch readability**

Run builders, prologue tests, P0 gate and explicit loads of both rooms. Launch PrologueLevel and manually traverse both branches. Record first-pass branch times in README; target 90–150 seconds per first traversal.

- [ ] **Step 5: Commit**

```powershell
git add tools/build/prologue/BuildUpperPendulum.cs tools/build/prologue/BuildLowerBuriedMachinery.cs tools/build/prologue/BuildBellChainRoom.cs tools/build/prologue/BuildBrokenToothCorridor.cs tools/build/prologue/BuildPrologueLevel.cs scenes/prologue scripts/prologue/HiddenAlcoveTrigger.cs tests/unit/P1LoopStructureTests.cs tests/integration/P1LoopRuntimeTests.cs tests/RunPrologueTests.cs README.md
git commit -m "feat: build prologue exploration loop"
```

### Task 8: Target-agnostic thread interactions, Bell-Back Laborer and permanent shortcut

**Files:**
- Create: `scripts/mechanisms/ThreadInteraction.cs`
- Create: `scripts/mechanisms/IThreadInteractionReceiver.cs`
- Create: `scripts/mechanisms/BellChainMechanism.cs`
- Create: `scripts/combat/IDamageReceiver.cs`
- Create: `scripts/combat/ArmoredHitAccumulator.cs`
- Create: `scripts/combat/ArmoredDamageReceiver.cs`
- Modify: `scripts/combat/HealthComponent.cs`
- Modify: `scripts/combat/Hurtbox.cs`
- Modify: `scripts/abilities/ThreadTargetKind.cs`
- Modify: `scripts/abilities/ThreadTarget2D.cs`
- Create: `scripts/enemies/laborer/BellBackLaborerState.cs`
- Create: `scripts/enemies/laborer/BellBackLaborerFrame.cs`
- Create: `scripts/enemies/laborer/BellBackLaborerModel.cs`
- Create: `scripts/enemies/laborer/BellBackLaborer.cs`
- Create: `scripts/enemies/laborer/BellBackLaborerPresentationCoordinator.cs`
- Create: `scripts/enemies/laborer/BellBackLaborerPlaceholderVisual.cs`
- Create: `tools/build/prologue/BuildBellBackLaborer.cs`
- Create: `tools/build/prologue/BuildBellChainMechanism.cs`
- Create: `scenes/enemies/BellBackLaborer.tscn`
- Create: `scenes/mechanisms/BellChainMechanism.tscn`
- Modify: `tools/build/prologue/BuildBellChainRoom.cs`
- Modify: `scenes/prologue/rooms/BellChainMechanism.tscn`
- Create: `tests/unit/ThreadInteractionTests.cs`
- Create: `tests/unit/ArmoredHitAccumulatorTests.cs`
- Create: `tests/unit/BellBackLaborerModelTests.cs`
- Create: `tests/integration/BellChainRuntimeTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Adds `ThreadTargetKind.Mechanism`.
- Produces `IThreadInteractionReceiver.TryReceiveThread(ThreadInteraction interaction): bool`; ThreadTarget calls this before default anchor/light/heavy behavior.
- Produces `IDamageReceiver.ReceiveDamage(DamageRequest request): bool`; HealthComponent implements it and Hurtbox resolves the receiver from an exported path that defaults to `../Health`.
- Produces `ArmoredDamageReceiver` that accumulates guarded damage with divisor 3 and forwards reduced requests to its configured HealthComponent; exposed damage bypasses accumulation.
- Produces `BellChainMechanism.Opened` and idempotent `ApplyProgress(PrologueProgress progress)`.
- Produces laborer states `Idle`, `Approach`, `Guard`, `Telegraph`, `Attack`, `Exposed`, `Recover`, `Dead`.
- Produces stable laborer paths `Presentation`, `VisualRoot/Placeholder`, `VisualRoot/Effects`; its coordinator maps state/cues and the gameplay adapter never draws.

- [ ] **Step 1: Write thread-dispatch RED tests**

Assert a Mechanism target calls its receiver once and does not impulse the player; a LightEnemy with only `IThreadPullReceiver` keeps existing pull behavior; an Anchor still pulls the player; HeavyEnemy with an interaction receiver opens a weak point without moving either body; missing receiver falls back to pulling the player for HeavyEnemy.

- [ ] **Step 2: Implement generic dispatch and run GREEN**

`ThreadTarget2D.ApplyPull` constructs a `ThreadInteraction` containing player, origin and converted settings. It first invokes `IThreadInteractionReceiver.TryReceiveThread`; only a `false` result uses existing kind fallback. No branch may mention `BellBackLaborer` or `BellChainMechanism`.

Add `IDamageReceiver` without changing the external Hitbox/Hurtbox flow: Hitbox still calls Hurtbox, Hurtbox resolves `DamageReceiverPath`, and the receiver decides how to forward the DamageRequest. HealthComponent implements the new interface by delegating `ReceiveDamage` to existing `ApplyDamage`, preserving old callers and tests.

- [ ] **Step 3: Write laborer/model RED tests and implement**

First test the accumulator with literal guarded sequence `1,1,1,1,1,1 → 0,0,1,0,0,1`, divisor 1 pass-through, invalid divisor rejection and reset clearing the remainder. Then assert guarded front hits route through `ArmoredDamageReceiver`, thread opens Exposed for the configured duration, exposed hits bypass the accumulator, the body does not receive pull velocity, expiry returns to Guard, and death overrides every state. The Hurtbox receiver path points to armor while guarded and Health while exposed; changing this path is an adapter action, not a visual action.

- [ ] **Step 4: Write mechanism/shortcut runtime RED tests and build the room**

Replace the convergence stub content with the authored laborer encounter, chain mechanism and a shortcut exit that returns to the corridor checkpoint entrance; retain both branch entrances/backtracking exits. In the real room, aim/thread at the chain, assert one Opened signal, shortcut collision disables, progress flag becomes true, and repeated thread use does not emit again. Die after opening, restore checkpoint and assert the shortcut remains open while the laborer resets guarded at spawn. Load a fresh PrologueProgress and assert the shortcut starts closed. Keep a temporary `p1_complete` marker; the Boss exit is added in Task 10 when the destination room exists.

- [ ] **Step 5: Run P1 GREEN and mutation checks**

Run all leaf builders, BellChain room builder, Prologue builder, prologue tests and P0 gate. Mutate ThreadTarget to treat Mechanism as Anchor and confirm the player-impulse assertion fails. Restore. Mutate shortcut reset to close the door and confirm persistence test fails. Restore.

- [ ] **Step 6: Commit**

```powershell
git add scripts/mechanisms scripts/combat/IDamageReceiver.cs scripts/combat/ArmoredHitAccumulator.cs scripts/combat/ArmoredDamageReceiver.cs scripts/combat/HealthComponent.cs scripts/combat/Hurtbox.cs scripts/abilities/ThreadTargetKind.cs scripts/abilities/ThreadTarget2D.cs scripts/enemies/laborer tools/build/prologue scenes/enemies/BellBackLaborer.tscn scenes/mechanisms scenes/prologue/rooms/BellChainMechanism.tscn tests/unit/ThreadInteractionTests.cs tests/unit/ArmoredHitAccumulatorTests.cs tests/unit/BellBackLaborerModelTests.cs tests/integration/BellChainRuntimeTests.cs tests/RunPrologueTests.cs
git commit -m "feat: add bell chain shortcut and heavy enemy"
```

P1 checkpoint: the running build must now support both branches, hidden discovery, all three thread uses and a persistent shortcut. Capture a short deterministic branch-loop clip before P2 so layout regressions are visually comparable.

---

## P2 — Boss Closure

### Task 9: Deterministic Fallen Bellkeeper model

**Files:**
- Create: `scripts/config/BossTuning.cs`
- Create: `scripts/boss/FallenBellkeeperState.cs`
- Create: `scripts/boss/FallenBellkeeperAttack.cs`
- Create: `scripts/boss/FallenBellkeeperInput.cs`
- Create: `scripts/boss/FallenBellkeeperFrame.cs`
- Create: `scripts/boss/FallenBellkeeperModel.cs`
- Create: `tests/unit/FallenBellkeeperModelTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces states `Dormant`, `Intro`, `PhaseOne`, `Transition`, `PhaseTwoArmored`, `HammerTelegraph`, `HammerThrown`, `Exposed`, `Defeated`.
- Produces attacks `None`, `Sweep`, `LeapSlam`, `GroundWave`, `HammerThrow`.
- Produces frame flags `AttackActive`, `ArmorClosed`, `ChainTargetActive`, `EnteredState`.
- Consumes literal validated BossTuning values including transition ratio `0.60` and retry-ready delay `3.0` seconds or less.

- [ ] **Step 1: Write Boss model RED tests**

Assert Dormant waits for arena activation; Intro cannot damage; PhaseOne cycles only Sweep/LeapSlam/GroundWave with telegraph before active frames; health fraction exactly `0.60` triggers Transition once; Transition never attacks; PhaseTwo armored schedules HammerThrow; only HammerThrown accepts a chain thread; successful thread enters Exposed; exposure expiry recloses armor; zero health enters Defeated from every non-defeated state; Defeated never attacks.

- [ ] **Step 2: Run RED and implement pure classes**

The model receives all observations in `FallenBellkeeperInput`; no physics query, SceneTree, random generator or wall clock call occurs inside it. Use a deterministic attack index so tests and proof capture are repeatable.

- [ ] **Step 3: Run GREEN and mutation suite**

Confirm exact boundary tests at 0.60, telegraph/active/recovery endpoints and exposure endpoint. Mutate transition comparison from `<=` to `<` and confirm the boundary test fails; restore. Mutate Defeated to retain active attack and confirm all-state death test fails; restore.

- [ ] **Step 4: Commit**

```powershell
git add scripts/config/BossTuning.cs scripts/boss tests/unit/FallenBellkeeperModelTests.cs tests/RunPrologueTests.cs
git commit -m "feat: model fallen bellkeeper encounter"
```

### Task 10: Fallen Bellkeeper scene, arena and complete reset

**Files:**
- Create: `scripts/boss/FallenBellkeeper.cs`
- Create: `scripts/boss/FallenBellkeeperPresentationCoordinator.cs`
- Create: `scripts/boss/FallenBellkeeperPlaceholderVisual.cs`
- Create: `scripts/boss/BossArenaController.cs`
- Create: `tools/build/prologue/BuildFallenBellkeeper.cs`
- Create: `tools/build/prologue/BuildFallenBellkeeperArena.cs`
- Create: `scenes/boss/FallenBellkeeper.tscn`
- Create: `scenes/prologue/rooms/FallenBellkeeperArena.tscn`
- Create: `tests/unit/BossSceneStructureTests.cs`
- Create: `tests/integration/FallenBellkeeperRuntimeTests.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces Boss stable paths `Health`, `Hurtbox`, `AttackHitbox`, `ChainTarget`, `Presentation`, `VisualRoot/Placeholder`, `VisualRoot/Effects`.
- Produces `BossArenaController.BeginEncounter()`, `ResetForCheckpoint()`, `EncounterDefeated`.
- Consumes PrologueHud Boss methods, PrologueProgress and `IThreadInteractionReceiver` for the thrown chain.

- [ ] **Step 1: Write scene structure RED tests**

Assert one body collider, one Hurtbox, one attack hitbox, inactive chain target at spawn, visual root, closed/open armor geometry nodes, arena doors, boss checkpoint, non-damaging transition configuration and room entrances `from_mechanism` / `boss_retry`.

- [ ] **Step 2: Write runtime RED tests**

Use real physics and InputMap attack/thread. Assert: entry closes doors and starts Intro; PhaseOne attacks damage only during active windows; forced health to 60% enters non-damaging Transition; closed armor applies the 3:1 accumulator; during HammerThrown a thread interaction enters Exposed and hides/disables closed armor target; exposed player hits deal normal damage; player-delivered final damage emits EncounterDefeated.

For reset, kill the player during each of `PhaseOne`, `Transition`, `HammerThrown` and `Exposed`. After recovery assert one Boss instance, full health, Dormant/Intro-ready state, closed entry door restored to pre-fight state, no active hitbox, inactive chain target, full player health, zero player velocity, reset combat/thread state and retry trigger reachable within 180 physics frames.

- [ ] **Step 3: Implement adapter, visuals and arena**

Boss adapter translates model frames to movement, hitbox and chain target; it exposes the current frame and cues but never chooses attacks independently or calls the visual. `FallenBellkeeperPresentationCoordinator` resolves the shared PresentationProfile, maps frames/cues and owns effects. Placeholder visual changes bell geometry between closed and exposed, uses a visible hammer arc during telegraph, and uses a non-color shape change for transition.

BossArenaController owns encounter instance and door state. Its reset is idempotent: disconnect old events, free old Boss, instantiate exactly one fresh packed scene, rebind events, then signal ready.

- [ ] **Step 4: Generate, run GREEN and mutate**

Run all tests, Boss builder, arena builder, Prologue builder and explicit arena load. Mutation: omit old Boss free during reset and confirm the exactly-one-instance assertion fails. Restore. Mutation: leave chain target active outside HammerThrown and confirm state/structure assertion fails. Restore.

- [ ] **Step 5: Commit**

```powershell
git add scripts/boss tools/build/prologue/BuildFallenBellkeeper.cs tools/build/prologue/BuildFallenBellkeeperArena.cs tools/build/prologue/BuildPrologueLevel.cs scenes/boss scenes/prologue/rooms/FallenBellkeeperArena.tscn tests/unit/BossSceneStructureTests.cs tests/integration/FallenBellkeeperRuntimeTests.cs tests/RunPrologueTests.cs
git commit -m "feat: add fallen bellkeeper boss arena"
```

### Task 11: Closing Echo, objective HUD and pause flow

**Files:**
- Create: `scripts/prologue/PrologueObjective.cs`
- Create: `scripts/prologue/PrologueObjectiveModel.cs`
- Create: `scripts/prologue/PrologueCompletionController.cs`
- Create: `scripts/prologue/ProloguePauseMenu.cs`
- Modify: `scripts/prologue/PrologueHud.cs`
- Modify: `scripts/prologue/PrologueLevelController.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeperArena.cs`
- Create: `tests/unit/PrologueObjectiveTests.cs`
- Create: `tests/integration/PrologueCompletionTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces objective sequence `Wake`, `ReachCorridorLamp`, `LearnThread`, `ChooseRoute`, `OpenShortcut`, `DefeatBellkeeper`, `Complete`.
- Produces pause commands `Resume`, `ShowControls`, `ReturnToCheckpoint`, `QuitToDesktop` through existing InputMap `pause`.
- Consumes Boss `EncounterDefeated`, PrologueProgress and HUD methods.

- [ ] **Step 1: Write objective and completion RED tests**

Assert objective transitions only on the matching progress event, branch choice does not require both routes, shortcut objective completes only on real mechanism open, Boss death before shortcut cannot skip required progression, and `Complete` is terminal. Completion test asserts final player damage causes Boss death, doors reopen, boss HUD hides, completion text appears, input remains available, progress marks Boss defeated and repeated death signals do not repeat completion.

- [ ] **Step 2: Write pause RED tests**

In a real SceneTree, press pause and assert tree paused while menu processes; resume unpauses; ReturnToCheckpoint unpauses then requests exactly one recovery; controls view contains the current keyboard/controller actions; quit is injectable in tests so the runner does not exit.

- [ ] **Step 3: Implement minimal objective, completion and pause controllers**

Visible text lives in one dictionary keyed by `PrologueObjective`; gameplay code stores enum values only. Completion text is limited to the “last gravekeeper” clue and route summary. Do not implement settings, localization service or disk save.

- [ ] **Step 4: Run GREEN and full P2 manual path**

Run all prologue tests and scene loads, then manually play from courtyard to Boss completion once. Confirm no interaction requires hidden developer keys and HUD never covers the player in 1280×720.

- [ ] **Step 5: Commit**

```powershell
git add scripts/prologue tools/build/prologue/BuildPrologueLevel.cs tools/build/prologue/BuildFallenBellkeeperArena.cs tests/unit/PrologueObjectiveTests.cs tests/integration/PrologueCompletionTests.cs tests/RunPrologueTests.cs
git commit -m "feat: complete prologue objective and pause flow"
```

P2 checkpoint: expose the full start-to-finish build to the user. Gameplay/layout/Boss feedback is handled through tuning, encounter, room or presentation contracts before final evidence work.

---

## P3 — Polish, Verification and Evidence

### Task 12: Full prologue generation gate and deterministic critical-path smoke

**Files:**
- Create: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Create: `tests/powershell/PrologueEvidenceChecks.Tests.ps1`
- Create: `tools/verify-prologue.ps1`
- Modify: `tools/EvidenceChecks.ps1`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `project.godot`
- Modify: `README.md`

**Interfaces:**
- Produces log markers `PROLOGUE_TESTS_OK` and `PROLOGUE_CRITICAL_PATH_OK` exactly once.
- Produces deterministic builder order: Player, RustShell, Moth, Laborer, Boss, BellChain mechanism, six rooms, PrologueLevel.
- Requires `PrologueLevelController.RequiredRooms` to equal the literal set of all six PrologueRoomId values before the main scene can change.
- Makes `res://scenes/prologue/PrologueLevel.tscn` the main scene only after the final gate passes.

- [ ] **Step 1: Write failing smoke and evidence fixtures**

Smoke uses real InputMap and physics areas to complete: courtyard movement/jump/attack, corridor squire/checkpoint/anchor, one chosen branch, a light-enemy pull, Bell-Back exposure, mechanism shortcut, Boss phase transition, chain exposure, final player hit and completion. It also runs a death fixture that verifies checkpoint recovery and a Boss retry fixture. Every stage prints a unique `PROLOGUE_STAGE=<name>` and the runner enforces a fixed physics-frame budget.

PowerShell fixtures reject missing/duplicate markers, ERROR lines, missing builder logs, empty/stale movie outputs and a verification plan with builders out of order.

- [ ] **Step 2: Run RED**

Run the smoke directly and confirm it fails because final orchestration/gate is missing, not because an input remains pressed from a prior stage. Run PowerShell fixtures and confirm missing prologue evidence helpers.

- [ ] **Step 3: Implement the complete non-destructive gate**

`verify-prologue.ps1` snapshots every generated `.tscn` plus `project.godot`, builds C#, runs leaf-to-parent builders, rejects semantic scene drift, restores exact bytes in `finally`, imports, runs prologue tests, critical-path smoke and log scans. It must preserve pre-existing unrelated working-tree changes.

After this gate succeeds, set main scene to PrologueLevel in the checked-in `project.godot`; retain `tools/verify.ps1` for technical-prototype regression debugging.

- [ ] **Step 4: Run full gate twice and verify repository state**

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
git diff --check
git status --short
```

Expected twice: C# 0 warnings/errors, `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, no engine errors, generated scene/project hashes unchanged after each run, and only the two known pre-existing art-script modifications plus current task files before commit.

- [ ] **Step 5: Mutation checks**

Swap Moth/Laborer builder order and confirm PowerShell evidence fixture rejects it. Remove shortcut persistence from progress restore and confirm smoke fails after death. Disable Boss chain exposure and confirm smoke fails before completion. Restore each mutation and rerun full GREEN.

- [ ] **Step 6: Commit**

```powershell
git add tests/smoke/PrologueCriticalPathSmoke.cs tests/powershell/PrologueEvidenceChecks.Tests.ps1 tools/verify-prologue.ps1 tools/EvidenceChecks.ps1 tools/build/prologue/BuildPrologueLevel.cs project.godot README.md
git commit -m "test: verify prologue first level end to end"
```

### Task 13: Timed playtests, proof recordings and milestone audit

**Files:**
- Create: `tests/presentation/ProloguePresentation.cs`
- Create: `tests/presentation/BossRetryPresentation.cs`
- Create: `tools/capture-prologue.ps1`
- Create: `docs/playtests/2026-08-14-prologue-first-level.md`
- Modify: `README.md`

**Interfaces:**
- Produces exact log markers `PROLOGUE_PRESENTATION_COMPLETE` and `BOSS_RETRY_PRESENTATION_COMPLETE`.
- Produces `screenshots/prologue/prologue.avi` and `screenshots/prologue/boss-retry.avi`, or numbered PNG sequences under the same ignored proof directory.
- Consumes only InputMap, room exits and encounter rules used by the playable game; no direct completion calls.

- [ ] **Step 1: Write capture/evidence RED fixtures**

Extend PowerShell evidence tests to reject a stale prologue AVI, an empty retry AVI, duplicate completion markers, a proof shorter than its configured frame count and any capture log containing engine errors. Confirm RED before wiring `capture-prologue.ps1`.

- [ ] **Step 2: Implement deterministic capture drivers**

The full presentation drives one selected branch, shortcut and Boss completion at fixed 60 FPS. The retry presentation deliberately dies during PhaseTwo, shows checkpoint recovery and starts the next Boss attempt. Both pre-position the camera before the first movie frame and release every pressed action in `finally`.

- [ ] **Step 3: Execute three actual playtests**

Record in `docs/playtests/2026-08-14-prologue-first-level.md`:

1. Normal route: total time, deaths, unclear prompts, route choice and Boss attempts.
2. Death/retry route: ordinary checkpoint time-to-control, Boss time-to-reentry and any stale state.
3. Hidden route: discovery path, return path, shortcut persistence and whether optional content blocks completion.

The target is 10–15 minutes for a first normal clear and Boss reentry within about 3 seconds. If evidence falls outside these bounds, change the owning `PlayerTuning`, `ThreadTuning`, `EnemyArchetypeTuning`, `BossTuning`, `EncounterDefinition` or `RoomDefinition`, add a regression assertion for the discovered bug, and rerun the gate that owns that configuration.

- [ ] **Step 4: Capture and inspect both proofs**

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1
```

Verify fresh non-empty output, exact marker count, frame count/duration, 1280×720 output, clean logs and actual visual progression. Inspect representative frames from courtyard, branch, mechanism, Boss transition, completion, death and retry; a valid file alone is not visual proof.

- [ ] **Step 5: Run the completion audit**

Map every success criterion in the approved spec to current evidence: source/interface, automated test/log, playtest row and proof timestamp. Mark missing or indirect evidence as incomplete and continue work. Only when every row is proven run `tools/verify-prologue.ps1` once more and confirm the working tree contains no unexpected files.

- [ ] **Step 6: Update durable status and commit**

README must state what is built, what remains, current run/verify/capture commands and a truthful asset table. Keep formal art, audio, Ember, Echo, disk save and full settings listed as excluded.

```powershell
git add tests/presentation tools/capture-prologue.ps1 docs/playtests/2026-08-14-prologue-first-level.md README.md
git commit -m "docs: deliver playable prologue first level"
```

After Task 13, use `superpowers:requesting-code-review`, address validated findings with `superpowers:receiving-code-review`, rerun the completion audit, then use `superpowers:finishing-a-development-branch` to integrate without discarding the two pre-existing art-script edits.
