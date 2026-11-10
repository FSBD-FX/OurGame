# Directional Air Jet Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root sole writer/native owner; read-only assistants. Steps use checkbox syntax.

**Goal:** Make real eight-direction air thrust, actual acceleration and coast/brake drive coherent body/rigid leg poses without changing gameplay.
**Architecture:** Pure observed jet/physics model→optional typed AirJet frame→common attachment transform and free-leg solving. Existing Motor, root lean, v217 landing, actions and ground support remain authoritative.
**Tech Stack:** Current Godot/.NET C#, existing canonical PowerShell, no runtime/dependency change.
**Spec:** docs/superpowers/specs/2026-11-03-directional-air-jet-design.md

## Global Constraints

OnlyG:/knight/Knight; source1875→1876 for one new plain model. Originalbuild180s/focused55s/pre180s/post300s. Preserve eleven-item scope, all old tests/assets and prior failures. No force/HP/input/collision/map/save/engine/resource ownership changes, GUI/OS/payment/capture/automation/Git/deletion. Freeze allCS/helper while any owned process is live; exact manual baselines, one whole-change review.

## Review Focus

1. Owner/lifecycle/epoch/burn stale or default/invalid/contradictory source cannot replay thrust pose or evade suppression/grounding.
2. Duplicate ticks/zero time/gaps/rollback/teleports must not invent acceleration; positive render time may ease cached targets only; extreme finite input stays bounded.
3. Mirrored/vertical/diagonal thrust must use locked direction, not current velocity direction; braking and coast stay distinct, same-world facing turn continuous.
4. Air foot folds must reach Draw with rigid18+18 links, no root/bitmap stretch or hidden clamp; landing/ground/death/recovery handoff must not retain free-flight targets.
5. Common transform preserves all ten attachments, actual hands/weapon/nozzle and partial/full actions; old default frames keep their old behavior.

### Task1: Direction/force pose and real consumer

**Files:** create scripts/presentation/PlayerAirJetPose.cs; modify PlayerActionFrame.cs, PlayerPresentationCoordinator.cs, PlayerFormalVisual.cs; tests/unit/ThrusterPresentationModelTests.cs, PlayerPresentationTests.cs; tests/integration/ThrusterPresentationRuntimeTests.cs, PlayerFormalVisualTests.cs; existing RunThrusterCoreTests.cs/canonical captures/receipt gates as needed. Source-only module, no new scene or texture.
**Interfaces:** exact value types/model signature in spec. Frame.AirJet optional init; Timeline passes current owner from Landing observation, actual Velocity/Ground motion stamp and existing Mobility. No new gameplay owner.

- [x] Add real saved-player air-direction leaf to existing presentation-rig before original delegates. Require actualdirection/nondefaultidentity/sameidentityfreshsample/dash1/jump0/noairrefund first, then missing AirJet channel assertion via reflection (compile succeeds). Canonical -Phase thruster-presentation-rig -SourceRows1875 →genuine AIR_JET_CHANNEL_RED, no cap/waiver change.
- [x] Write pure model controls first in existing unit class/Run: all directions, actual constant-force vs constant velocity, duplicate/zero/rate, stale/header retirement, bad data/extremes, coast/brake/ground/suppression. Existing presentation-model →missing-model assertion RED. Implement value model only, capture newsource and use1876; expectedpureGREEN.
- [x] Wire optional frame/Timeline. Add formal real-node/foot/attachment controls before renderer; witness AIR_JET_DRAW_RED, implement common group/neutral measured Dash/rigid folded feet, expected GREEN. Test all ten slots/facing/partial actions and no stale terminal/ground load.
- [x] Complete same-sample native eight-direction geometry/nozzle/identity proof with real keys, noSpace/force writes. Preserve original five rig delegates, landing/ground and pulse/input/dynamics/reentry contracts. Host exact singleton receipts with functional refusal controls; not text-only tests.
- [x] One fresh whole-change read-only review on exact old baselines/newfile/spec/ledger; one C/I RED→GREEN pass if required; defer minors/no re-review.
- [x] Final current-source focused rig/model/gait/landing and full ordinarypre/post/actions/keyboard/models under originalcaps. Preserve errors/quiet does not cure historicalfault; Main899/fullroutes/normal-speed/hardware/human unchangedOPEN.
- [x] Update README/backlog/U-20261103-01/report/ledger, evidencecustody/currentfingerprints and normal20GiBcheck. Technicalincrement only, no fullgoal completion/Git/cleanup.
