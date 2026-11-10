# Hurt Attack Clock Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hidden attack age advances while Hurt owns presentation, avoiding expired strike playback without changing gameplay.
**Architecture:** Native matching clock/window probe first; minimal existing Timeline clock extraction/guard only afterRED. Original event IDs, phases, hitpause and all existing delegates retained.
**Tech Stack:** Existing Godot4.7.1 .NET/C#, canonical ordinary headless pipeline.
**Spec:** docs/superpowers/specs/2026-11-08-hurt-attack-clock-design.md

## Global Constraints

- KNIGHTG:/knight/Knight only. Root owns all edits/build/native/canonical; preserve dirtytree/oldfailures.
- NoGUI/OSinput/paid/GC/cache/dump/true-savewrite/Git/worktree mutation/deletion/automation or budgetflags.
- Original rigcompat11formal/2mantle +guard +v325WJ delegates unchanged. Current1913source/6binary is baseline, newtestsource makes1914.

## Review Focus

- Normal Process/Physics skew cannot fake stale strike: Task1 literal2tick clock admission and noHurtmatchedcontrols.
- Hurt ending in the same Step must not double-advance attack: Task1 rates30/60/120 and recovery-normalized-age controls.
- Hitpause must remain active while hidden, not disappear or compound: Task1 pure outgoing-hit controls alongside nativezeroOutgoing.
- Short attack can expire underHurt without re-latching oldID: Task1 tinyattack/freshID/zero-time controls.
- Real gameplay must not pause/restart: Task1 actualMonitoring singlewindow/AttackStarted one/HP and finalrig, originalbroadregressions.

## Task 1: Native proof and bounded clock repair

**Files:** Create tests/integration/PlayerHurtAttackClockTests.cs (partial tests), modify tests/integration/ThrusterPresentationRuntimeTests.cs onlyappend; modify scripts/presentation/PlayerPresentationCoordinator.cs onlyafterRED; canonicalsource snapshot+requiredreceipt only.
**Interfaces:** Existing savedPlayer/FramePublished, BasinBoardingTickObserver.Sample priority100, originalCombat Hitbox.Monitoring and realkeyboard. New public test RunHurtAttackClock(SceneTree); no production test-only API.

- [x] Add matched native floor/WJ controls2facings; collect all clocks/activewindow/Hurt events first, apply clockadmission before staleassert. Add pure30/60/120/default/custom/tiny/hitpause/zero/cancel/freshID controls, aggregate failures without hidingnative. Run ordinary rigcompat -SourceRows1914 with pinnedpwsh, require actual functionalNativeRED, notcompile/admissionfailure.
- [x] Minimal hidden-age advance through existing clock/hitpause path, exactlyonce onHurt-endingstep; no gameplay/otherpriority changes. Run samewhole rigcompat tostrictGREEN and inspectactualwindow/phase facts, notjustsuccessmarker.
- [ ] Finalsame-source rigcompat, presentationrig, recoverycompat, actions, models, pre44/post75, headlessMain/Explore, savecompat+independentcold onceeach. No retries selectinggreen or oldversion borrowing; preserveallfailures.
- [ ] Single fresh wholeincrementreview; regradeC/IoneTDDfixpass, Minor defer, no rereview. Auditcurrentinputs/binaries/usersaves and retainedcasecopies; runmediaCheckonce withnormalreport; updateREADME/backlog/report/ledger. Originalgoal remainsACTIVE, no finalAV/asset/wholegoalclaim.

2026-11-08 execution status: all eleven final frozen stages actually ran once,10PASS/1FAIL; pre crashed in case0 (0returns), so the strict qualification checkbox cannot be completed. Single fresh reviewC0/I0/M0 and final2863pair/66binary/current1914source/user109 audits, mediaone-shot and docs are already performed; do not repeat these from an unchecked checkbox or retry the failed phase to selectgreen. The component repair is verified, Task1 fullcompletion remainsOPEN due resourcequalification; next meaningful work may proceed on separately proven candidates without erasing this failure.
