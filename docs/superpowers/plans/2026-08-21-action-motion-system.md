# Player and Enemy Action Motion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace pose snapping with a continuous three-layer action system that makes player locomotion, attacks, thread casting, enemy tells, Boss attacks, cape, lantern, spool, and corruption motion readable at normal speed.

**Architecture:** PlayerPresentationCoordinator produces a typed PlayerPresentationFrame containing locomotion, action, phase, facing, and impulses. PlayerPoseMixer blends authored key poses without resetting the rig every frame; SecondaryMotionModel adds bounded spring motion after the primary pose. Enemy/Boss formal visuals consume the attack frames from Plan 1 and use the same phase-based interpolation discipline.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, layered Node2D rigs, deterministic curve/spring math, generated formal visual scenes, unit/integration/presentation tests.

**Spec:** [Combat readability and presentation design](../specs/2026-08-21-combat-readability-presentation-design.md)

## Global Constraints

- Depends on the attack frames from Plan 1 and thread cast frames from Plan 2; do not duplicate either state machine.
- Keep stable player slots: Body, NearArm, FarArm, NearLeg, FarLeg, Weapon, Mantle, ShoulderCore, Lantern, Tether, plus the Plan 2 ThreadShuttle leaf.
- Base locomotion, upper-body action, and secondary motion are independent layers with explicit ownership; no layer may restore another layer to rest each frame.
- Action timing is expressed in seconds/normalized phase and remains deterministic at 60 FPS; no wall-clock timers.
- Preparation and contact confirmation remain visible; only declared Recovery phases are movement/jump cancellable.
- Hit pause is local presentation time only, short and bounded, and never delays input/physics.
- Secondary offsets are clamped so cape, lantern, spool, and corruption never cover weapon, feet, target marker, or danger geometry.
- Use current project-owned rig parts and replaceable slots; no pay-per-use generation.

---

### Task 1: Pose data, interpolation, and secondary-motion math

**Files:**
- Create: `scripts/presentation/PoseTransform2D.cs`
- Create: `scripts/presentation/PlayerActionFrame.cs`
- Create: `scripts/presentation/PlayerPose.cs`
- Create: `scripts/presentation/PlayerPoseMixer.cs`
- Create: `scripts/presentation/SecondaryMotionModel.cs`
- Create: `tests/unit/PlayerPoseMixerTests.cs`
- Create: `tests/unit/SecondaryMotionModelTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: `PlayerLocomotionState { Idle, StartRun, Run, Brake, JumpStart, Rise, Apex, Fall, SoftLand, HardLand, Dash, Dead, Recovery }`
- Produces: `PlayerActionState { None, GroundAttack, AirAttack, DownAttack, ThreadPreview, ThreadWindup, ThreadThrow, ThreadLatch, ThreadApply, ThreadFailed, Hurt }`
- Produces: `PlayerActionPhase { None, Anticipation, Burst, Contact, Recovery }`
- Produces: `PlayerActionFrame(PlayerActionState State, PlayerActionPhase Phase, float NormalizedTime, bool RecoveryCancellable)`
- Produces: `PlayerPresentationFrame(EntityPresentationFrame Base, PlayerActionFrame Action, SecondaryMotionFrame Secondary)`
- Produces: `PlayerPoseMixer.Mix(PlayerPose locomotion, PlayerPose action, float actionWeight) -> PlayerPose`
- Produces: `SecondaryMotionModel.Step(SecondaryMotionInput input, float deltaSeconds) -> SecondaryMotionFrame`

- [ ] **Step 1: Add failing pure-math tests**

    public static void Run()
    {
        ActionLayerOverridesOnlyOwnedUpperBodySlots();
        LocomotionContinuesUnderThreadPreview();
        PoseInterpolationIsContinuousAtPhaseBoundaries();
        RecoveryCancellationReturnsToCurrentLocomotion();
        CapeLanternSpoolAndTetherLagDifferentInputs();
        SecondaryOffsetsRemainInsideDeclaredBounds();
        ResultsAreStableAcrossEquivalentDeltaPartitions();
    }

- [ ] **Step 2: Run RED**

    & 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj

  Expected: missing pose/action/spring types.

- [ ] **Step 3: Implement immutable poses and ownership masks**

  Each pose stores named slot transforms and an ownership mask. Mix rest-to-locomotion first, then only action-owned slots, then additive secondary offsets. Interpolate position/rotation/scale/modulate with shortest-angle rotation.

- [ ] **Step 4: Implement bounded critically damped secondary motion**

  Drive cape by horizontal acceleration/turn, lantern by acceleration/landing, spool by thread phase, and tether by lantern-spool separation. Reset only on death/scene teardown; checkpoint recovery settles rather than teleports.

- [ ] **Step 5: Run GREEN and reset mutation**

  Temporarily reset all springs on every Step and prove the continuity assertion fails, then restore.

- [ ] **Step 6: Commit**

    git commit -m "feat: add layered pose and secondary motion"

---

### Task 2: Player action timeline and formal-rig integration

**Files:**
- Modify: `scripts/presentation/PlayerPresentationCoordinator.cs`
- Modify: `scripts/presentation/PlayerFormalVisual.cs`
- Modify: `scripts/presentation/PlayerAnimationClip.cs`
- Modify: `scripts/presentation/PlayerAnimationMap.cs`
- Modify: `assets/player/formal/player-animation-map.json`
- Modify: `tests/unit/PlayerPresentationTests.cs`
- Modify: `tests/unit/PlayerAnimationMapTests.cs`
- Modify: `tests/integration/PlayerFormalVisualTests.cs`

**Interfaces:**
- Consumes: `PlayerPresentationFrame(EntityPresentationFrame Base, PlayerActionFrame Action, SecondaryMotionFrame Secondary)` from `PlayerActionFrame.cs`.
- Replaces cue-duration pose snapping with `PlayerFormalVisual.ApplyPlayerFrame(PlayerPresentationFrame frame)`.
- Maintains `IEntityVisual2D.ApplyFrame` and `PlayCue` only as compatibility adapters during migration.

- [ ] **Step 1: Write action-timeline RED tests**

  Cover idle/start/run/brake/jump/rise/apex/fall/soft-land/hard-land; ground/air/down attack anticipation-burst-contact-recovery; hit and whiff recovery; every thread state; hurt/knockback/dead/recovery; action-over-run; and declared recovery cancellation.

- [ ] **Step 2: Run RED**

  Expected: current coordinator has only coarse EntityVisualState plus short cue overrides.

- [ ] **Step 3: Produce continuous action frames**

  Coordinator reads motor velocity/grounded/dash, combat attack id/direction/window, health impulses, and Plan 2 thread cast frames. It advances normalized phase from authoritative durations and never invents gameplay timing.

- [ ] **Step 4: Replace per-frame rest reset**

  Build named key poses once, mix them each frame, and apply final transforms once per slot. Preserve root facing mirror only; do not mirror individual inertia or recreate dictionaries every frame.

- [ ] **Step 5: Add readable thread/body actions**

  Preview rotates shoulder/arm toward aim while legs continue. Windup charges the spool, Throw leads with arm and needle, Latch holds for confirmation, Apply braces the torso, Failed recoils, and Recovery retracts. Every phase must be distinguishable in a still frame.

- [ ] **Step 6: Run GREEN and hard-cut mutation**

  Temporarily replace interpolation with the destination pose. The phase-boundary position/rotation delta assertion must fail. Restore and run the full runner.

- [ ] **Step 7: Commit**

    git commit -m "feat: blend complete player action poses"

---

### Task 3: Enemy and Boss phase-driven formal motion

**Files:**
- Modify: `scripts/presentation/RustShellFormalVisual.cs`
- Modify: `scripts/presentation/GraveLampMothFormalVisual.cs`
- Modify: `scripts/presentation/BellBackLaborerFormalVisual.cs`
- Modify: `scripts/boss/FallenBellkeeperFormalVisual.cs`
- Modify: `scripts/boss/FallenBellkeeperFormalPart.cs`
- Modify: `tests/integration/EnemyFormalVisualTests.cs`
- Modify: `tests/integration/FallenBellkeeperFormalVisualTests.cs`

**Interfaces:**
- Adds `PresentAttack(AttackPresentationFrame frame)` to each formal enemy/Boss visual.
- Adds actor-specific secondary inputs but shares the pure interpolation helpers from Task 1.

- [ ] **Step 1: Add silhouette and continuity RED tests**

  Assert Rust lowers body and retracts weapon before thrust; Moth folds wings, locks its eye lamp, then dives; Laborer plants feet and counter-rotates before sweep; Boss Sweep, LeapSlam, GroundWave, and HammerThrow each produce distinct Body/Armor/Hammer/Chain transforms. Assert corruption-side movement remains behind the weapon/face/feet bounds.

- [ ] **Step 2: Run RED**

  Expected: current formal visuals expose only modest state deltas and Boss open-phase attacks share a pose.

- [ ] **Step 3: Implement phase curves and delayed appendages**

  Use each attack frame's normalized remaining time. Root/weapon establish the readable primary silhouette; shell, wings, back bell, chain, fungus, and armor follow with small delayed curves. Active uses a short white contact impulse; Recovery opens the counterattack silhouette.

- [ ] **Step 4: Run GREEN and unique-Boss-pose mutation**

  Temporarily route GroundWave to Sweep pose and prove the exact transform-difference assertion fails, then restore.

- [ ] **Step 5: Commit**

    git commit -m "feat: animate readable enemy action phases"

---

### Task 4: Rig builders, performance budget, and normal-speed proof

**Files:**
- Modify: `tools/build/art/BuildPlayerFormalVisual.cs`
- Modify: `tools/build/art/BuildEnemyFormalVisuals.cs`
- Modify: `tools/build/art/BuildFallenBellkeeperFormalVisual.cs`
- Modify: `scenes/presentation/PlayerFormalVisual.tscn`
- Modify: `scenes/presentation/RustShellFormalVisual.tscn`
- Modify: `scenes/presentation/GraveLampMothFormalVisual.tscn`
- Modify: `scenes/presentation/BellBackLaborerFormalVisual.tscn`
- Modify: `scenes/presentation/FallenBellkeeperFormalVisual.tscn`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tests/presentation/PrologueVisualAudit.cs`
- Modify: `tools/audit-prologue-production.ps1`

**Interfaces:**
- Builders retain all stable slot paths and validate pivots, z-order, and required action support.
- Audit exposes maximum one-frame slot displacement/rotation per named transition and presentation-frame cost.

- [ ] **Step 1: Add builder/audit RED assertions**

  Require every player slot, enemy weapon/corruption split, Boss five-part rig, valid pivots, no negative/zero scale, no whole-character stretch, and bounded transition deltas at 60 FPS.

- [ ] **Step 2: Regenerate leaf scenes**

  Run player, enemy, and Boss formal visual builders, then their parent gameplay builders. Inspect semantic generated-scene diffs before staging.

- [ ] **Step 3: Run focused and full verification**

    & 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
    & 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs --log-file action-motion-tests.log
    powershell -ExecutionPolicy Bypass -File .\tools\audit-prologue-production.ps1

  Require 0 warnings/errors, one `PROLOGUE_TESTS_OK`, and the existing 50 ms deterministic budget.

- [ ] **Step 4: Capture normal-speed transitions**

  Record run-start-stop-jump-land, attack hit/whiff, tap/hold thread, hurt/recovery, each ordinary enemy attack, and each Boss attack. Review consecutive frames for popping, stretching, frozen feet, hidden weapons, and cape/lantern teleports.

- [ ] **Step 5: Commit**

    git commit -m "feat: deliver continuous action motion"
