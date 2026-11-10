# Application Quit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Root implements, read-only agents assist, one fresh whole-change review. Checkboxes track actual evidence.

**Goal:** Make the real menu/window quit quiesce producers and observe native audio release before exiting.
**Architecture:** Pure drain gate + terminal audio partial + existing Flow menu/root-window wiring. Keep reusable Shutdown and original bare-Quit diagnostic.
**Tech Stack:** Existing Godot4.7.1/.NET C#, canonical PowerShell.
**Spec:** docs/superpowers/specs/2026-11-03-application-quit-design.md

## Global Constraints

G:/knight/Knight only. Original eleven items ACTIVE. Headless/internal events only; no GUI/OS/paid/runtime/Git/delete/automation/true saves/dump. Root sole writer; freeze all CS/helpers during owned build/native/outer batch. Exact baselines, original build180s/native55s/pre180s/post300s budgets. No old delegate/assertion removal or warning filter. Source1879 plus five new CS yields1884.

## Review Focus

1. Synchronous Shutdown callback reentry and duplicate quit cannot restart sound/session or create a second task.
2. Ordinary Shutdown and non-application/nested title fixtures retain existing reuse/window ownership.
3. Paused/missing owner/invalid timing/timeout cannot fabricate Ready or quit a replacement scene.
4. Same process frame, clock rollback or stale mix observation cannot count cleanup progress.
5. Menu/window actually call production preparation with enabled audio and no new save writes; original immediate-Quit leaf remains unchanged.

### Task1: Terminal audio, drain and actual application quit

**Current status:** in_progress. Execution and evidence steps below are recorded; final same-source15 phases=14PASS/1strictFAIL (unchanged immediate-Free/Quit world-jet leakage). No overall completion, no second review, nested-Task reentry coverage Minor deferred.

**Create:** scripts/audio/AudioQuitDrainGate.cs, scripts/audio/PrologueAudioDirector.Quit.cs; tests/RunApplicationQuitTests.cs, tests/unit/AudioQuitDrainGateTests.cs, tests/integration/ApplicationQuitRuntimeTests.cs.
**Modify:** scripts/audio/PrologueAudioDirector.cs, scripts/prologue/PrologueGameFlowController.cs, scripts/prologue/ProloguePauseMenu.cs, canonical .superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1.
**C/I-pass addition:** scripts/prologue/PrologueLevelController.cs, exact fifth baseline saved before changing queued/resident/next-physics recovery cancellation. This was added after the unique review's real recovery RED, not part of the initial three-file baseline.
**Interfaces:** exact spec signatures; gate observations `(ElapsedMilliseconds, ProcessFrame, MixAgeSeconds, OwnerValid, PlayersSilent, OwnedStreamsReleased)`, result State/ElapsedMilliseconds/ObservedProcessFrames/MixObserved. Flow exposes cached PrepareApplicationQuitAsync and IsApplicationQuitting, no test-only exit callback.

- [x] Save exact three existing-file baselines; create this plan's ledger. Add runner/canonical `application-quit-menu`, saved-level existing Quit event, assert immediate all-player silence, then eventual drain result via reflection until interfaces exist. Run source1880, expected behavior APPLICATION_QUIT_NOT_QUIESCED_RED with quiet build, not a missing CS symbol.
- [x] Add independent literal pure gate and native contracts tests before implementations; canonical `application-quit-contracts`; source1882. Missing gate behavioral RED. Implement gate/audio partial/main gates and Flow preparation/window lease per spec; source1884. Existing menu RED→GREEN; literal duplicate/rollback/deadline/owner controls GREEN.
- [x] Implement/verify real terminal rearm denial and ordinary-Shutdown reverse control; enabled saved Player/native Shift, owned-ID release and cancelled owner. Actual saved-level menu/window/paused quit, repeated requests, foreign-flow lease refusal and owner-exit restoration. Add exact receipt consumer controls; original leaf untouched.
- [x] One fresh review against exact baselines/new files/spec/ledger. C/I one actual RED→GREEN pass if needed; minors explicitly deferred, no re-review.
- [x] Frozen current quit phases + original audio/worldjet/gait/keyboard/actions/models/fullpre/post original caps; report all strict failures, do not retry quiet. Verify saved manifests/current source/helpers/bin, update README/backlog/report/ledger/media-check. No goal completion or visual/physical acceptance claim.
