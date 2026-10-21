# Attack Readability and First-Level Difficulty Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every first-level enemy and Boss attack readable before damage through one authoritative attack-frame contract, true-shape three-stage telegraphs, distinct poses, and the approved beginner-friendly timing/damage baseline.

**Architecture:** Enemy and Boss models remain the authority for attack identity and timing. Each runtime combines that immutable timeline with its actual CollisionShape2D transform to publish an AttackPresentationFrame; AttackTelegraphVisual, formal enemy visuals, VFX, and later audio consume that frame without owning damage or collision.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, CharacterBody2D/Area2D/CollisionShape2D, generated scenes, project-owned procedural visuals, in-project unit/integration runners, PowerShell verification.

**Spec:** [Combat readability and presentation design](../specs/2026-08-21-combat-readability-presentation-design.md)

## Global Constraints

- Preserve existing route, checkpoint, save, guidance, encounter, thread-target, and Boss phase contracts.
- Telegraph geometry must be derived from the same hitbox shape and transform used for damage; do not maintain decorative duplicate dimensions.
- Gold means Telegraph, red means the locked Active danger shape, and white means impact confirmation; color is reinforced by shape and motion.
- First-level tuning is centralized and asserted: Rust 0.65/0.14/0.65, Moth 0.70/0.35/0.70, Laborer 0.75/0.20/0.70, Boss Sweep 0.80/0.25/0.70, LeapSlam 0.90/0.25/0.75, GroundWave 1.00/0.30/0.75, HammerThrow 0.85/real flight/0.70.
- Boss maximum health is 12 and every first-level enemy attack/contact event applies at most one health segment.
- Visual-node loss may log a named warning but may not suppress or create gameplay damage.
- No pay-per-use generation; all temporary and final telegraph art is project-owned procedural Godot output.

---

### Task 1: Shared attack timeline and shape contracts

**Files:**
- Create: `scripts/combat/AttackPresentationFrame.cs`
- Create: `scripts/combat/AttackShapeFrame.cs`
- Create: `tests/unit/AttackPresentationFrameTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: `AttackPhase { None, Telegraph, Active, Recovery }`
- Produces: `EnemyAttackKind { None, RustShellThrust, MothDive, LaborerSweep, BossSweep, BossLeapSlam, BossGroundWave, BossHammerThrow }`
- Produces: `AttackShapeKind { None, Rectangle, Circle, Capsule, ConvexPolygon, GroundBand }`
- Produces: `AttackTimelineFrame(ulong AttackId, EnemyAttackKind Kind, AttackPhase Phase, float PhaseDurationSeconds, float PhaseRemainingSeconds, Vector2 LockedWorldTarget, bool HasLockedTarget)`
- Produces: `AttackShapeFrame(AttackShapeKind Kind, Vector2 Origin, Vector2 Extent, float RotationRadians, IReadOnlyList<Vector2> Points)`
- Produces: `AttackPresentationFrame(AttackTimelineFrame Timeline, AttackShapeFrame Shape)`

- [ ] **Step 1: Register failing contract tests**

    public static void Run()
    {
        RejectsNegativeDurationOrRemainingTime();
        RejectsRemainingTimeAboveTheDeclaredPhase();
        PreservesAttackIdentityAcrossAllThreePhases();
        RejectsNegativeShapeExtent();
        PreservesLockedWorldTargetAcrossTelegraphAndActive();
    }

- [ ] **Step 2: Run RED**

    & 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj

  Expected: CS0246/CS0103 failures for the missing attack-frame types.

- [ ] **Step 3: Implement immutable validated frames**

    public readonly record struct AttackPresentationFrame(
        AttackTimelineFrame Timeline,
        AttackShapeFrame Shape)
    {
        public AttackPresentationFrame Validate();
    }

  Validation rejects invalid enum values, negative time/extent, remaining time above duration, malformed polygon points, and Active frames with no usable shape. None is the only legal phase without geometry.

- [ ] **Step 4: Run GREEN and mutation**

  Build and run `res://tests/RunPrologueTests.cs`; expect exactly one `PROLOGUE_TESTS_OK`. Temporarily allow a negative rectangle extent and prove `RejectsNegativeShapeExtent` fails, then restore.

- [ ] **Step 5: Commit**

    git commit -m "feat: define enemy attack presentation frames"

---

### Task 2: Model-owned phases and approved first-level tuning

**Files:**
- Modify: `scripts/enemies/RustShellModel.cs`
- Modify: `scripts/enemies/RustShellFrame.cs`
- Modify: `scripts/enemies/RustShellSettings.cs`
- Modify: `scripts/enemies/moth/GraveLampMothModel.cs`
- Modify: `scripts/enemies/moth/GraveLampMothFrame.cs`
- Modify: `scripts/config/EnemyArchetypeTuning.cs`
- Modify: `scripts/enemies/laborer/BellBackLaborerModel.cs`
- Modify: `scripts/enemies/laborer/BellBackLaborerFrame.cs`
- Create: `scripts/config/BellBackLaborerTuning.cs`
- Modify: `scripts/boss/FallenBellkeeperModel.cs`
- Modify: `scripts/boss/FallenBellkeeperFrame.cs`
- Modify: `scripts/config/BossTuning.cs`
- Modify: `tests/unit/RustShellModelTests.cs`
- Modify: `tests/unit/GraveLampMothModelTests.cs`
- Modify: `tests/unit/BellBackLaborerModelTests.cs`
- Modify: `tests/unit/FallenBellkeeperModelTests.cs`

**Interfaces:**
- Every enemy/Boss frame gains `AttackTimelineFrame AttackTimeline`.
- Model increments `AttackId` exactly once when a new Telegraph begins.
- Boss tuning exposes per-kind timings through `BossAttackTiming TimingFor(FallenBellkeeperAttack attack)`.

- [ ] **Step 1: Add failing phase-boundary tests**

  Assert exact inclusive boundaries, stable AttackId, locked Moth/Boss target positions, and unique Boss kinds. Add named assertions for the complete approved timing table and Boss health 12.

- [ ] **Step 2: Run RED**

  Expected: compile failures because current frames do not expose `AttackTimeline` and current tuning values differ.

- [ ] **Step 3: Implement the smallest model changes**

  Keep the existing state machines, but expose phase duration/remaining from their current timers. Freeze target/facing at the Telegraph-to-Active boundary where the attack requires locking. Replace the single Boss phase-one timing with the per-kind resolver.

- [ ] **Step 4: Prove damage limits**

  Extend health/runtime tests so one attack id cannot remove more than one segment from one hurtbox and first-level contact damage is one. Do not change player invulnerability or unrelated prototype tuning.

- [ ] **Step 5: Run GREEN and two mutations**

  First restore Rust telegraph to 0.35 seconds and require the named timing assertion to fail. Then regenerate Boss attacks with a new AttackId on Active and require the identity assertion to fail. Restore both and rerun the full runner.

- [ ] **Step 6: Commit**

    git commit -m "feat: expose readable enemy attack phases"

---

### Task 3: True-shape telegraph runtime and distinct enemy poses

**Files:**
- Create: `scripts/presentation/AttackShapeFactory.cs`
- Create: `scripts/presentation/AttackTelegraphVisual.cs`
- Modify: `scripts/presentation/PresentationCue.cs`
- Modify: `scripts/enemies/RustShellSquire.cs`
- Modify: `scripts/enemies/moth/GraveLampMoth.cs`
- Modify: `scripts/enemies/laborer/BellBackLaborer.cs`
- Modify: `scripts/boss/FallenBellkeeper.cs`
- Modify: `scripts/presentation/RustShellFormalVisual.cs`
- Modify: `scripts/presentation/GraveLampMothFormalVisual.cs`
- Modify: `scripts/presentation/BellBackLaborerFormalVisual.cs`
- Modify: `scripts/boss/FallenBellkeeperFormalVisual.cs`
- Create: `tests/integration/AttackTelegraphRuntimeTests.cs`
- Modify: `tests/integration/EnemyFormalVisualTests.cs`
- Modify: `tests/integration/FallenBellkeeperFormalVisualTests.cs`

**Interfaces:**
- Produces: `AttackShapeFactory.FromCollision(CollisionShape2D source, Transform2D worldTransform, AttackShapeKind requestedKind) -> AttackShapeFrame`
- Produces: `AttackTelegraphVisual.Present(AttackPresentationFrame frame)`
- Produces: runtime event `event Action<AttackPresentationFrame> AttackFrameChanged`
- Adds: `PresentationCue.AttackTelegraph` while `AttackStarted` remains the Active cue.

- [ ] **Step 1: Write real-physics RED tests**

  Instantiate each formal runtime and assert: Telegraph is visible before monitoring; its world bounds equal the real hitbox bounds; Active turns the hitbox on and red fill off; Recovery disables damage; locked Moth dive does not chase the player after lock. Assert unique formal pose markers for Rust crouch/thrust, Moth folded wings/dive, Laborer counter-twist/sweep, and each Boss attack.

- [ ] **Step 2: Run RED**

  Expected: missing factory/visual/event types and current runtimes only cue on Active.

- [ ] **Step 3: Implement one presentation leaf per actor**

  Runtimes enrich the model timeline from their actual hitbox, publish it, and call the leaf visual. `AttackTelegraphVisual` draws only in local presentation space and never toggles monitoring. Use a gold contour/progress sweep during Telegraph, a red shape only after lock and before Active, and a white impact pulse at Active.

- [ ] **Step 4: Make Boss geometry attack-specific**

  Use the existing hitbox node as the single authority. Sweep uses a real ConvexPolygonShape2D sector, LeapSlam a locked CircleShape2D, GroundWave a real ground-band RectangleShape2D, and HammerThrow the real projectile/chain path. Set the hitbox shape first, then derive the presentation frame from it.

- [ ] **Step 5: Run GREEN and geometry mutation**

  Temporarily scale only the telegraph extent by 0.75. The real-physics equality assertion must fail. Restore and run the full prologue runner with no engine/script/parse errors.

- [ ] **Step 6: Commit**

    git commit -m "feat: render true enemy attack telegraphs"

---

### Task 4: Generated scenes, first-level balance gate, and visible proof

**Files:**
- Modify: `tools/build/BuildRustShellSquire.cs`
- Modify: `tools/build/prologue/BuildGraveLampMoth.cs`
- Modify: `tools/build/prologue/BuildBellBackLaborer.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeper.cs`
- Modify: `scenes/enemies/RustShellSquire.tscn`
- Modify: `scenes/enemies/GraveLampMoth.tscn`
- Modify: `scenes/enemies/BellBackLaborer.tscn`
- Modify: `scenes/boss/FallenBellkeeper.tscn`
- Modify: `tests/unit/P0RoomStructureTests.cs`
- Modify: `tests/unit/P1LoopStructureTests.cs`
- Modify: `tests/unit/BossSceneStructureTests.cs`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Stable child path on every actor: `VisualRoot/AttackTelegraph`.
- Builders validate the telegraph script, z-order, mouse/collision neutrality, and exact first-level tuning.

- [ ] **Step 1: Add structure RED assertions**

  Require all four stable paths, no collision layer/mask on the visual, visibility behind player hit flashes but above room gameplay skins, and exact generated tuning.

- [ ] **Step 2: Regenerate leaf scenes and run GREEN**

  Run the four builders in leaf-to-parent order, then build and run `RunPrologueTests.cs`. Review cached scene diffs and stage only semantic lines; preserve unrelated unique-id/import noise.

- [ ] **Step 3: Add deterministic evidence dwell**

  Update the presentation driver to hold before and during each ordinary attack plus all three Boss attacks. Record enough frames to show gold preparation, locked red danger, white impact, and recovery for each.

- [ ] **Step 4: Run the production gate**

    powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1

  Expected: 0 build warnings/errors, all builders validated, exactly one each of `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`.

- [ ] **Step 5: Capture and review**

    powershell -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly

  Review the exact warning/active/impact frames at 1280×720. Reject any pose, danger area, weapon, feet, or Boss ground contact that cannot be read at normal playback speed.

- [ ] **Step 6: Commit**

    git commit -m "feat: deliver first-level attack readability"
