# Ground Brake Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL superpowers:executing-plans. Root sole writer/native owner. Readonly agents assist; one fresh whole-change review, no second review.

**Goal:** Make real control deceleration visibly brace before the player has already stopped, retaining physical motion and grounded support.
**Architecture:** Motor publishes a versioned completed-step value; pure brake model drives Timeline/Frame, one common body load and reduced swing-foot arc.
**Tech Stack:** Current Godot4.7.1 Mono/.NET C#, canonical PowerShell.
**Spec:** docs/superpowers/specs/2026-11-03-ground-brake-presentation-design.md

## Global Constraints

G:/knight/Knight only; original eleven ACTIVE. Headless/internal events only. No GUI/OS/paid/true saves/dump/runtime swap/automation/Git mutation/delete. Preserve dirty work/all failures, exact baselines. AllC#/helpers frozen during any owned build/native/outer run. Original build180s/native55s/pre180s/post300s/route600s caps. Baseline Source1885; new test only1886 for first actualRED, plus observation/model two production CS yields1888.

## Review Focus

1. Synchronous landing/death/reset/claw callbacks must never publish a stale completed observation.
2. Collision-adjusted speed or input reversal alone is not proof of ordinary control braking; same-step zero crossing must retain incoming direction.
3. Duplicate/zero-time/render-rate/owner-revision changes cannot replay load or advance feet.
4. Reducing foot lift can worsen reach; actual Draw18/18, no clamp, planted foot and coherent attack/shield attachments are mandatory.
5. Legacy no-observation frames retain old behavior; new body load cannot double count the old BodyLoad/Brake template or steal air/terminal ownership.

### Task1: Physical observation to grounded bracing

**Create:** scripts/player/PlayerGroundMotionObservation.cs; scripts/presentation/PlayerGroundBrakeModel.cs; tests/integration/PlayerFormalVisualTests.GroundBrake.cs.
**Modify:** PlayerMotor.cs; PlayerPresentationCoordinator.cs; PlayerActionFrame.cs; PlayerGroundGait.cs; PlayerFormalVisual.cs; tests/RunThrusterCoreTests.cs; canonical verify-claw-knight-v001.ps1.
**Interfaces:** exact records/Step/property in spec. Input init `GroundMotion`, Frame init `GroundBrake`; test delegates `RunGroundBrakeNative(SceneTree)`, `RunGroundBrakeModel()`, `RunGroundBrakeDrawOnly(SceneTree)` and strict receipt helper.

- [x] Save seven exact old-file baselines and ledger. Add actual saved Player→Action right→release test, assert nonzero high-speed real Brake before stop. Add runner call before original ground-gait. Run canonical `-Phase thruster-ground-gait -SourceRows 1886`; expected GROUND_BRAKE_NATIVE_RED, not missing-symbol/build fail.
- [x] Add hand-derived pure/model/support/Draw tests before production implementation; use reflection only for absent new interfaces so clean compile reaches intended missing-feature assertions. Implement the two pure production files and versioned completed Motor publication/clear gates, Timeline/Frame and coherent Draw/gait consumer. Same phaseSource1888: new native/model/Draw and all old suites must execute; host exact receipt positive/negative controls retained.
- [x] One fresh whole-change review versus baselines/new files/spec/plan/ledger. C/I one realRED→GREEN pass; minors defer, no second review. Scoped current green3d0cf318; full-suite gate remains below and Task1 remains in_progress until its evidence qualifies.
- [ ] Frozen current final original gait/rig/worldjet/actions/keyboard/models/pre/post/Main/Explore plus quit compatibility. Read failures, save/current binding and metadata, update durable report/README/backlog/feedback/media one-shot check. No overall goal completion or normal-speed quality claim without full evidence.

Final gate status: all listed verification executed, current18 phases17PASS/1MainFAIL; two incorrect live-route caller invocations were rejected before native, retained separately and corrected caller-only to established headless phases. Current1888/19helper/6bin plus90 saved textpairs/37 nativeidentities bind; docs and one-shot media check updated. This checkbox remains open because the strict whole-suite gate is not green; Task1 in_progress, original eleven ACTIVE. See v223 report for all rulings/deferredMinor/limits; no second review, goal completion, Git mutation or cleanup.
