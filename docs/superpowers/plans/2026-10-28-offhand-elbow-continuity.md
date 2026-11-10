# Offhand elbow continuity implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root implements inline; one fresh final review.

**Goal:** Remove a native-verified elbow flip during valid down-attack/hurt recovery without changing wrist/shoulder/weapon/gameplay contracts.

**Architecture:** Keep existing saved rig and real Timeline. Store only the last solved local elbow in PlayerFormalLayer and choose the nearest exact two-link solution at wrist updates; reads remain pure. Reuse the original headless formal-claw phase and original32 motion observer.

**Tech Stack:** pinned Godot4.7.1 .NET, C#, canonical verifier.

**Spec:** docs/superpowers/specs/2026-10-28-offhand-elbow-continuity-design.md

## Global Constraints

- G:/knight/Knight, dirty user branch unchanged; source1857/897C#, no new runtime files.
- Root sole writer/native owner; freeze sources/helpers/docs/bin during owned run/review/custody.
- No visible window/OS input/paid generation/install/automation/Git writes/deletion/new GC or drain/pins/cache changes.
- Same24+24 lengths, exact wrist/primary+secondary grips, same ten pose slots, no Timeline/combat/physics/difficulty/asset changes.
- Existing32 per-adjacent-60Hz-publication bound unchanged; not GPU/display/human qualification or resource fatal fix.

## Review Focus

- Hurt overlaps an active DownAttack, not a synthetic wrist jump.
- Both bend solutions remain exact24+24 with no shoulder/node-transform mutation.
- Zero/near-zero and repeated reads cannot flip or advance solver history.
- Facing/root travel/parent transform does not become false jitter or duplicate mirror.
- Old manual Draw-only negative controls and all378 grip samples still reject/validate as before.

### Task 1: real-producer RED and minimal IK fix

**Files:** tests/integration/PlayerFormalVisualTests.cs; scripts/presentation/PlayerFormalLayer.cs. Existing tests/presentation/ProloguePresentation.cs observer is read-only/unchanged.

**Interfaces:** add private AssertDrawArmInterruptedDownAttack(PlayerFormalVisual); preserve public SetArmWristLocal(Vector2), ArmWristLocal, ArmElbowLocal and ArmDrawRect.

- [x] Write native test after original normalTimeline test. Reuse savedPlayer Tuning, realTimeline1/60, stableApex16 then DownAttackId fresh, HurtTriggered tick3, observer32 across at least16 attack ticks. Require realHurt and resumedDown and fixedbones/hand; no wrist writes in main counterexample.
- [x] Run canonical -Phase thruster-formal-claw-phases -SourceRows1857. Expected named arm Draw motion RED; a build/fixture failure is not evidence. If not reproduced, do not edit production from the arithmetic hypothesis.
- [x] After actual RED, modify only Draw solver state/solution choice; no alternate wrist target or timing.
- [x] Run same focused phase and original models; expected focusedGREEN with all old checks and newproducer case, strict logs.
- [x] Add left/right/ordinaryattack/zero-cross/read-stability controls, run focused again; expect finite fixedlength and oldgrip/probe/camera contracts retained. Near-zero nonzero length approximation/extra controls explicitly deferred M1, see production record.

### Task 2: regression, one review and custody

**Files:** new production v151 record, README, original backlog V03/V04 local status, this plan's progress.md.

**Interfaces:** Task1's unchanged rig API and canonical ordinary phases. No new launcher or source count waiver.

- [x] Run ordinary formal-recovery, presentation-rig, keyboard, prologue-pre/post and normal-absence once. Retain failures, don't repeat-to-green or claim fullMain/Explore passed. Seven phases including models: 6PASS/1 actual post FATAL, NOT whole regression GREEN.
- [x] Freeze and obtain ONE fresh gpt-6-astra/high final review; root one C/I RED→GREEN fix pass if needed, Minor deferred, no rereview. All whole-game failures remain disclosed. C0/I0/M1, no C/I pass needed.
- [x] Check owned manifests/copies/bins/processes, normal one-shot media check, durable status. Keep original11goalACTIVE; no commit/delete/qualification from a narrowgreen.
