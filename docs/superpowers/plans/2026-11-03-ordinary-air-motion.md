# Ordinary Air Motion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Root sole writer/native owner, assistants read-only. Steps use checkbox syntax.

**Goal:** Distinguish real jump from walk-off and make ordinary airborne rigid legs/body follow actual motion continuously.
**Architecture:** Readonly Motor sequence→optional Jump observation→pure stamped OrdinaryAir model→typed frame→common attachment load and released-ankle interpolation. Ground/AirJet/action ownership remains.
**Tech Stack:** Existing Godot/.NET C#, canonical PowerShell. No dependency/scene/image change.
**Spec:** docs/superpowers/specs/2026-11-03-ordinary-air-motion-design.md

## Global Constraints

G:/knight/Knight only; autonomous approved motion scope, provisional pose mapping not user-approved style. No GUI/OS/paid/save/Git/delete/runtime/force/input/budget changes. Root sole writer, freeze ALLCS/helpers while any owned native/build/outer batch lives; terminal observation required before edits. Manual baselines retained. Existing1876source→1877newnative test→1879with new pure model/unit test. Build180s/focused55s/pre180s/post300s originalcaps. Full11/Main899/resources/R-I/R-C/R-P/humanOPEN.

## Review Focus

1. Missing/new owner, first late sample, stale sequence/tick, zero elapsed, reset and coyote/buffered jump cannot manufacture/replay launch.
2. Real walk-off must not inherit launch crouch; observed production must not use legacy no-observation heuristic.
3. Duplicate physical tick/new raw velocity, gaps, rollback, teleport and finite extremes must not alter cached targets or break bounds.
4. Both facings, partial/full attack/guard, active jet/claw/landing/death/recovery preserve priority and all10 attachments.
5. First airborne leg geometry/entry interpolation and complete up/apex/fall/landing use rigid18+18 links, no root/stretch/reach-clamp concealment.

### Task1: Accepted cause and ordinary-air production chain

**Files:** new scripts/presentation/PlayerOrdinaryAirMotion.cs, tests/unit/OrdinaryAirMotionTests.cs, tests/integration/OrdinaryAirMotionRuntimeTests.cs; modify scripts/player/PlayerMotor.cs, scripts/presentation/PlayerActionFrame.cs, PlayerPresentationCoordinator.cs, PlayerFormalVisual.cs; tests/unit/PlayerPresentationTests.cs, tests/RunThrusterCoreTests.cs; existing canonical capture/strict receipts. No old tests removed.
**Interfaces:** exact value/model signatures and mappings in spec; input.Jump/frame.OrdinaryAir optional init. New native class RunCause/RunDraw/RunJump use saved Player, actual internal keys, owned finite ledge shape, same FramePublished snapshots; no velocity writes.

- [x] Save exact existing-file baselines. Add native finite-ledge walk-off cause test and core ground-gait hook before old delegates; add new test capture, SourceRows1877. Run canonical ground-gait: expected real ORDINARY_AIR_CAUSE_RED, compile success, old physical counts0. No production changes yet.
- [x] Add pure observation/model controls via reflection (no compile-failure substitute), including real-vs-legacy Timeline cause, model first-owner/reset/suppression/duplicate/rate/bounds, actual jump vs unsupported downward air. New unit source gives1878 before production1879. Expected missing model/channel RED. Implement Motor sequence/value model/frame/Timeline per spec; pure GREEN then native cause GREEN.
- [x] Add actual formal-node/draw controls before rendering; expected ORDINARY_AIR_DRAW_RED. Implement neutral measured air targets, common10slot load and same-solver local ankle/up entry interpolation. Test mirror/partial/full actions, continuity/zero time/clear priorities/no clamp. Expected DrawGREEN.
- [x] Add ordinary Space/coyote/up-apex-fall-landing actual snapshot proof, jump1/dash0/noairrefund and true contacts. Host exact singleton receipts + executable missing/duplicate/false controls; no substring-only certification. Preserve v2188direction and original rig5.
- [x] One fresh whole-change review against this task's exact baselines/newfiles/spec/ledger. One C/I RED→GREEN pass if needed; minors defer, no re-review. Review Important fixed; subsequent actual post regression fixed within same pass with original claw assertion preserved; current48/targetedclawGREEN, fullsuite stillnotGREEN.
- [ ] Current frozen ground-gait/presentation-model/presentation-rig/impact-player/negative/support/worldjet plus fullpre/post/actions/keyboard/clawmodels originalcaps. Read all failures and raw logs; no rerun filtering. Update README/backlog/U/report/ledger/custody and normal20GiBcheck. Only local program increment done, not fullgoal/aesthetic or historicalresource repair.
