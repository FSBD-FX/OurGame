# Ground Up Cut Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ground Up-J visually raises the real weapon/arms instead of replaying horizontal cut.
**Architecture:** Existing typed phase targets and elapsed-driven wrist trajectory, actual original bitmap/grips. Test first, no gameplay tuning.
**Tech Stack:** Existing Godot4.7.1 .NET/C#, ordinary canonical headless phases.
**Spec:** docs/superpowers/specs/2026-11-08-ground-up-cut-design.md

## Global Constraints

- Only G:/knight/Knight; root exclusively edits/builds/runs native/canonical. Preserve dirty tree/history/evidence.
- No GUI, OSinput, paidart, real-savewrite, engine/GC/cache/budget change, automation, Gitmutation or deletion.
- Original 11 formal and2mantle delegates stay; old arm/grip/gait tests not weakened. Final aesthetics, character and full-prologue goals remainOPEN.

## Review Focus

- Mirrored Up must stay world-up, not reverse to downward: Task1 paired facing±1 and realWJ.
- Secondary hand cannot detach while raising: Task1 actual bitmap grips and fixed links.
- Ground-only aim correction cannot alter horizontal/air/down: Task1 original delegates + scoped branch.
- Phase entry/release/pause must not snap or advance another clock: Task1 existing phase/reset delegates, zero-clock and new real Hurt handoff.
- Pose cannot generate jump/root motion/extra attacks: Task1 native event counts, HP/velocity/jump guards.

## Task 1: Directional motion and current verification

**Files:** Modify scripts/presentation/PlayerFormalVisual.cs (only afterRED), tests/integration/PlayerFormalVisualTests.cs; create tests/integration/PlayerGroundUpAttackTests.cs; wire tests/integration/ThrusterPresentationRuntimeTests.cs; canonical source snapshot + required receipt only.
**Interfaces:** Existing GroundAttack ActionTarget/AttackWristOffset, phase+Aim, actual drawable arm/weapon geometry. New public test RunGroundUpAttack(SceneTree) consumes savedPlayer, no new production API.

- [x] Add horizontal/Up matched actual rig comparison into existing AssertGroundAttackActiveArm: twofacings×30/60/120, wristRise>6, bladeTurn>.4, retaining all old asserts; run ordinary rigcompat SourceRows1912, expect functionalRED.
- [x] If reproduced, define Up-only upper arm/weapon targets and wrist curve within same time/ownership; preserve fixed bones, bitmap and doublegrip. Run old+newrigcompat toGREEN.
- [x] Add actual savedPlayer W/J ground action, both facing, lockedUp after Wrelease, allfour phases, reach/grips/sole/HP/ordinaryjump/zero-clock/Hurt handoff and existing same-attack resume; preserve old reset delegates, hook after old delegates and add canonical source+receipt. SourceRows becomes1913.
- [x] Run final same-source rigcompat, presentationrig, groundgait, recoverycompat, actions, models, ordinarypre/post once; all unexplained failures remainFAIL, no retryfiltering. All8strictPASS, fullpost187836ms; original historicalresourcefailures remainOPEN.
- [x] One fresh whole-increment review; oneTDDfixpass forC/I only; deferMinor; no rereview. Re-read source/bin/userguards; one normal mediaCheck; updateREADME/backlog/report/ledger. Fullgoal staysACTIVE, no unconditionalcomplete if required suiteFAIL. C0/I0/M1, noC/Ifix, Minor recorded.

## Task 2: Current full-prologue headless qualification

**Files:** No C#/assets/canonical changes; same frozen1913source/6binaries. Production report/README/backlog/ledger record actualcases.
**Interfaces:** Original RunTask10HeadlessRoute Main/Exploration and resident-save-compatibility with independentcoldreader. No new routeplanner, inputs, budgets or recovery allowances.

- [x] Run existing canonical `thruster-route-main-diagnostic`, `thruster-route-exploration-diagnostic`, `resident-save-compatibility`, all `-SourceRows1913` and no otherflags; each once. Read actualTitle→samePlayer Boss→supportedexit, recovery0/ordinaryjump grounded/held60/save data, original limits. If failures occur preserve and report, no mix-version qualification or retries selectinggreen. All3andcoldPASS, no retries.
- [x] Verify final source/bin/user/caseguards and independentcoldreceipt; update record, keep fullgoalACTIVE with normal audiovisual/hardware/human/formalassets/resourcecause stillOPEN. Task1code review is not rerun because no sourcechange.
