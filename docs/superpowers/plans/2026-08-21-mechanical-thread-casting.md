# Mechanical Thread Casting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the one-frame mechanical-thread action with the approved tap-to-lock/hold-to-aim cast, an explicit cancel-safe state machine, visible needle-shuttle travel/latch/tension, and unambiguous success, failure, and cooldown feedback.

**Architecture:** PlayerInputReader classifies physical button edges only; ThreadCastModel owns intent and phase timing; ThreadAbilityModel remains the deterministic target selector/cooldown authority; ThreadAbility adapts Godot targets and physics to those models. ThreadShuttleVisual, ThreadLineVisual, the player action layer, HUD, VFX, and audio consume ThreadCastFrame without deciding pulls.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, InputMap, CharacterBody2D, RayCast/DirectSpaceState, Line2D/Node2D procedural visuals, generated Player scene, unit/integration/smoke tests.

**Spec:** [Combat readability and presentation design](../specs/2026-08-21-combat-readability-presentation-design.md)

## Global Constraints

- Preserve K/Y input mapping, target group, collision masks, pull receivers, checkpoint reset, room cleanup, and the Bell-Chain stable target path.
- Tap threshold is 0.14 seconds; tap selection is 260 px and 55-degree half-angle; hold selection is 400 px and 30-degree half-angle.
- Priority is mechanism/tutorial target, pullable light enemy, then movement anchor, subject to true line of sight and configured range.
- Preview limits horizontal target speed to 65 percent, keeps jump available, and cancels without cooldown on dash, hurt, death, pause, or room transition.
- Windup, Travel, Latched, Apply, Recovery, and Failed are observable phases; a valid cast may not begin, apply, and clear in one physics frame.
- Gameplay pull is applied once by AttackId-equivalent CastId; visual loss cannot duplicate or cancel it.
- Preserve the existing 260 px Bell-Chain fallback while migrating it into the general tap-assist policy.
- No paid generation; needle, latch, three-strand line, and spool effects are project-owned procedural leaves.

---

### Task 1: Input edges and deterministic cast-state model

**Files:**
- Modify: `scripts/player/PlayerInputFrame.cs`
- Modify: `scripts/player/PlayerInputReader.cs`
- Modify: `scripts/player/PlayerMotor.cs`
- Create: `scripts/abilities/ThreadCastFrame.cs`
- Create: `scripts/abilities/ThreadCastModel.cs`
- Create: `tests/unit/ThreadCastModelTests.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Extends: `PlayerInputFrame` with `ThreadHeld` and `ThreadReleased` after `ThreadPressed`.
- Produces: `ThreadCastState { Idle, Preview, Windup, Travel, Latched, Apply, Recovery, Failed }`
- Produces: `ThreadAimMode { Tap, Hold }`
- Produces: `ThreadFailureReason { None, NoTarget, Obstructed, OutOfRange, Cooldown, TargetLost, Cancelled }`
- Produces: `ThreadCastInput(bool Pressed, bool Held, bool Released, Vector2 Aim, bool Cancelled)`
- Produces: `ThreadTargetSnapshot(ulong Id, Vector2 Position, bool IsValid, ThreadFailureReason InvalidReason)`
- Produces: `ThreadCastFrame(ulong CastId, ThreadCastState State, ThreadAimMode AimMode, Vector2 Aim, ulong TargetId, bool HasTarget, ThreadFailureReason Failure, float StateDurationSeconds, float StateRemainingSeconds)`
- Produces: `ThreadCastModel.Step(ThreadCastInput input, float deltaSeconds, ThreadTargetSnapshot target, bool cooldownReady) -> ThreadCastFrame`
- Adds: `PlayerMotor.AbilityMovementScale`, clamped to 0–1 and reset to 1 outside Preview.

- [ ] **Step 1: Add failing classification and transition tests**

    public static void Run()
    {
        ReleaseBeforeThresholdCreatesTapWindup();
        HoldingAtThresholdEntersPreview();
        PreviewReleaseLocksTheCurrentTarget();
        ValidCastSpansWindupTravelLatchApplyAndRecovery();
        DashHurtDeathAndRoomChangeCancelPreviewWithoutCooldown();
        MissingTargetProducesNamedFailure();
        TargetLostDuringTravelNeverApplies();
    }

- [ ] **Step 2: Run RED**

    & 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj

  Expected: compile failure for the missing held/released fields and cast types.

- [ ] **Step 3: Implement input edges and the pure state model**

  Reader uses `Input.IsActionPressed` and `Input.IsActionJustReleased`. The model advances only from supplied delta, allocates CastId on Windup, and exposes phase duration/remaining. PlayerMotor multiplies only horizontal move intent by 0.65 in Preview; jump input remains unchanged. Use initial 60 FPS targets: 4-frame windup, 3-frame release impulse, distance-based 0.08–0.28 travel, 5-frame latch, 7-frame recovery.

- [ ] **Step 4: Run GREEN and same-frame mutation**

  Temporarily transition Windup directly through Apply in one `Step`. Require `ValidCastSpansWindupTravelLatchApplyAndRecovery` to fail, restore, and rerun both test runners.

- [ ] **Step 5: Commit**

    git commit -m "feat: model multi-stage thread casting"

---

### Task 2: Tap/hold target policy, line of sight, and cooldown semantics

**Files:**
- Modify: `scripts/abilities/ThreadAbilitySettings.cs`
- Modify: `scripts/config/ThreadTuning.cs`
- Modify: `scripts/abilities/ThreadCandidate.cs`
- Modify: `scripts/abilities/ThreadAbilityModel.cs`
- Modify: `tests/unit/ThreadAbilityModelTests.cs`
- Modify: `tests/unit/ThreadInteractionTests.cs`
- Modify: `tests/integration/BellChainRuntimeTests.cs`

**Interfaces:**
- Adds: `TapThresholdSeconds`, `TapAssistRange`, `TapConeHalfAngleDegrees`, `HoldRange`, `HoldConeHalfAngleDegrees`, and per-phase durations to settings/tuning.
- Replaces target selection with `SelectTarget(Vector2 origin, Vector2 aim, ThreadAimMode mode, IReadOnlyList<ThreadCandidate> candidates)`.
- Adds target priority `ThreadTargetPriority { Mechanism, PullableEnemy, MovementAnchor }`.
- Adds cooldown operations `CanConsume(double nowSeconds)` and `Consume(ulong castId, double nowSeconds)`; a CastId can consume once.

- [ ] **Step 1: Write target-policy RED tests**

  Cover tap 260/55, hold 400/30, line-of-sight rejection, mechanism-over-nearer-enemy priority, enemy-over-anchor priority, deterministic distance/id tie breaks, existing Bell-Chain neutral tap, and no neutral global fallback outside the policy.

- [ ] **Step 2: Run RED**

  Expected: existing three-parameter `SelectTarget` cannot express mode or priority.

- [ ] **Step 3: Implement centralized selection**

  Convert candidate kinds into explicit priority, filter LOS and range before scoring, then compare priority, aim dot, distance, and id in that order. Keep all numeric policy in ThreadTuning.

- [ ] **Step 4: Implement cooldown correctness**

  Cooldown is consumed only on entering Travel with a valid target. Preview cancellation, no target, obstruction, and out-of-range release do not consume the full cooldown. Target loss after Travel uses a short failed recovery but cannot refund/duplicate the applied cast.

- [ ] **Step 5: Run GREEN and priority mutation**

  Temporarily prefer the nearer anchor over the mechanism and prove the named mechanism-priority test plus real Bell-Chain test fail. Restore and run both runners.

- [ ] **Step 6: Commit**

    git commit -m "feat: add tap and hold thread targeting"

---

### Task 3: Runtime state adapter and needle-shuttle presentation

**Files:**
- Modify: `scripts/abilities/ThreadAbility.cs`
- Modify: `scripts/abilities/ThreadTarget2D.cs`
- Modify: `scripts/vfx/ThreadLineVisual.cs`
- Create: `scripts/vfx/ThreadShuttleVisual.cs`
- Modify: `scripts/presentation/PresentationCue.cs`
- Modify: `scripts/presentation/PlayerPresentationCoordinator.cs`
- Create: `tests/integration/ThreadCastRuntimeTests.cs`
- Modify: `tests/integration/PrologueVfxRuntimeTests.cs`

**Interfaces:**
- Produces: `ThreadAbility.CurrentCastFrame`.
- Produces: `event Action<ThreadCastFrame, ThreadTarget2D> CastFrameChanged`.
- Adds cues: `ThreadPreview`, `ThreadWindup`, `ThreadTravel`, `ThreadLatched`, `ThreadApplied`, `ThreadRecovery`; existing Fired/Hit/Failed remain compatibility notifications until all consumers migrate.
- Produces: `ThreadShuttleVisual.Present(ThreadCastFrame frame, Vector2 origin, Vector2 targetOrFailurePoint)`.
- Extends `ThreadLineState` with `Preview`, `Travel`, `Latched`, and `Recovery`.

- [ ] **Step 1: Write real-runtime RED tests**

  Instantiate Player with real Input, ThreadAbility, target, wall, line, and shuttle. Assert preview does not pull, Travel lasts at least one physics frame, shuttle position advances monotonically, latch appears before pull, Apply happens once, three-strand connected line persists through latch/recovery, and death/room exit clears every transient.

- [ ] **Step 2: Run RED**

  Expected: the current runtime selects, pulls, and schedules clear in the pressed frame and has no shuttle/state event.

- [ ] **Step 3: Adapt the pure models to Godot**

  Resolve candidates each physics frame in Preview and once at lock. Compute travel duration from real distance, retain a weak reference to the target, revalidate it at Apply, and call `ApplyPull` once for the CastId. Use the real ray obstruction point for Failed visuals.

- [ ] **Step 4: Render the approved four-stage skill**

  Preview is low-brightness cyan with a gold candidate diamond. Windup rotates/brightens the shoulder spool. Travel moves a needle-shaped shuttle with up to three trailing strands. Latched expands a diamond lock; Apply tightens the strands; Recovery retracts without snapping.

- [ ] **Step 5: Run GREEN and target-loss mutation**

  Free the target during Travel and assert Failed/TargetLost, zero pull, zero stale references, and clean line/shuttle. Temporarily skip that revalidation to prove the test fails, then restore.

- [ ] **Step 6: Commit**

    git commit -m "feat: present mechanical thread casting"

---

### Task 4: Player scene, HUD feedback, smoke route, and visible proof

**Files:**
- Modify: `tools/build/BuildPlayer.cs`
- Modify: `scenes/player/Player.tscn`
- Modify: `scripts/ui/PrologueHudViewModel.cs`
- Modify: `scripts/ui/PrologueFormalHud.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `scenes/prologue/PrologueLevel.tscn`
- Modify: `tests/unit/PrologueHudViewModelTests.cs`
- Modify: `tests/integration/PrologueFormalHudTests.cs`
- Modify: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Stable Player child path: `VisualRoot/ThreadShuttle`.
- HUD adds `ThreadCastState ThreadState`, `ThreadFailureReason ThreadFailure`, and `string ThreadStateText`.
- Smoke helpers expose explicit `TapThread()` and `HoldAimAndReleaseThread(Vector2 aim, int holdFrames)`.

- [ ] **Step 1: Add builder/HUD/smoke RED tests**

  Require the shuttle path, compact spool states for Ready/Preview/Travel/Latched/Failed/Cooldown, one tap mechanism success, one hold-aim anchor success, one obstructed failure, and no input movement during deterministic tap.

- [ ] **Step 2: Regenerate and run GREEN**

  Run BuildPlayer, BuildPrologueLevel, build, and both test runners. Cache only semantic generated-scene changes and preserve unrelated import/unique-id noise.

- [ ] **Step 3: Add deterministic presentation dwell**

  Capture Preview, Windup, Travel, Latched, Apply, Recovery, no-target, and wall-obstructed states at 60 FPS. The normal-speed recording must visibly separate at least preparation, flight, latch, and effect.

- [ ] **Step 4: Run full gate and capture**

    powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
    powershell -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly

  Require all exact success markers once, no engine/script/parse errors, and no preview/shuttle/line residue after death or room transition.

- [ ] **Step 5: Commit**

    git commit -m "feat: deliver readable mechanical thread"
