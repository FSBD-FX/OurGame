# Grounded gait implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. Root owns code and serial native runs, readonly exploration/custody separate, one fresh whole-change review at end.

**Goal:** Replace the production rigid-leg/time-only gait with observed-support, actual-distance stepping and fixed-length knee/ankle/sole drawing without changing game physics.

**Architecture:** New pure PlayerGroundGait model accepts immutable support samples and outputs immutable world sole targets/stance/weight/distance phase. Coordinator samples existing body collisions, Timeline owns the model and publishes it, FormalVisual consumes after all pose transforms, and FormalLayer renders fixed two-link legs inside the ten existing slots.

**Tech Stack:** Existing Godot4.7.1/.NET9/C#, no new dependency or generated image.

**Spec:** docs/superpowers/specs/2026-11-03-grounded-gait-design.md; U-20261103-01 requirements; original eleven-item backlog unchanged.

## Global Constraints

- G:/knight/Knight only; preserve dirty state, baselines/failures, real saves. No GUI/OS input, paid generation, extra dump, runtime changes, automation, Git mutation/publication or deletion.
- One new production CS means explicit SourceRows1873. Preserve original build180s, focused55s, routes600s, pre180s/post300s. Freeze ALL CS/helpers during owned build/native.
- Keep ten sibling slots, pose ownership, constructors, single mirror, old15-point geometry export. No new physics/input/Node/Resource ownership, no body/bitmap scaling or approved asset replacement.
- Full goal and Main/Handle/art/feel/user/hardware acceptance remain OPEN; program anatomy dimensions and step parameters are developer sample choices only.

## Review Focus

- Production observation really reaches the final Draw consumer, rather than only passing a synthetic model test; unknown support cannot masquerade as planted.
- Inertial reversal, wall stop, zero delta and jump/landing do not teleport foot/knee positions or retain dead-world contacts.
- Moving/rotating support carries planted feet without advancing walking phase; new collider/teleport/singular transform resets safely without holding native wrappers.
- Thirty/sixty/120Hz samples crossing step boundaries preserve stance, fixed segment lengths and finite outputs; no final target is silently clamped yet reported as exact support.
- Facing/lean/attack/shield/claw overlap preserves leg ownership, single mirror, weapon grip/cloth contracts and legacy observation layout.

## Task1 — complete production sample and evidence

**Files:** Create scripts/presentation/PlayerGroundGait.cs. Modify scripts/presentation/PlayerActionFrame.cs, PlayerPresentationCoordinator.cs, PlayerFormalVisual.cs, PlayerFormalLayer.cs; tests/unit/PlayerPresentationTests.cs, tests/integration/PlayerFormalVisualTests.cs, tests/RunThrusterCoreTests.cs; canonical verifier only to admit/capture the new phase/file, never relax existing gates.

**Interfaces:** PlayerGroundSample(Observed,RootWorld,SupportId,SupportTransform,GroundPoint,GroundNormal); PlayerGroundGaitFrame(Observed,Weight,Phase,NearSoleWorld,FarSoleWorld,NearStance,FarStance); PlayerGroundGait.Step(sample,eligible,delta), default frame remains compatibility unavailable. Input.Ground and Frame.Gait init properties; Layer.LegHip/Knee/Ankle/SoleLocal, LegReachClamped, SetLegSoleLocal(sole,up). Formal consumes Gait; new tests RunGroundGait/RunGroundGaitOnly and phase --ground-gait must exercise real output/binding.

- [x] Save exact modified-source/helper baselines; create stub interfaces and literal/model/native tests first. New phase cap55s, no extra flags; run named behavioral RED, not compile failure.
- [x] Implement pure support-relative model, actual Coordinator/Timeline publication and final leg rendering; run focused GREEN. If stages expose a second missing render/binding, retain that RED then implement it, not weaken tests.
- [ ] One fresh review on exact saved-baseline range including new file; C/I one TDD pass, minors deferred/no rereview. Run focused plus original action/keyboard/model/pre/post and route gates on frozen final source; report any old/new failure.
- [x] Custody/source/helper/bin/process/HEAD/index/media checks; update README/backlog/requirements with real progress and unfinished visual review. No Task/goal-complete from partial native passes.

Execution note: one fresh review C0/I4/M1 and one C/I RED->GREEN pass done, no rereview; final current9 gates executed7PASS2FAIL (Main original899 and pre native finalizer crash). The regression-completion checkbox and Task1 remain OPEN, not a partially green completion. Final custody verifies20cases/100manifestpairs/9692selectedsource/210helpers/120bins/41ownedended; current1873/10helpers/6binmatch, HEAD/index unchanged, media no newthreshold. No Git/workspace deletion or branch finish.

Manual ledger because no Git mutation/deletion. All implementation inline because model, renderer and producer share one new value contract; final review is the fresh independent check. Self-review: all spec sections map to Task1; exact old APIs kept; no visual/taste acceptance inferred from program geometry.
