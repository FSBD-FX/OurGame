# Abyss Updraft Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Restore the existing 4–6 second passive descent with a clearly signaled local updraft.

**Architecture:** A generated CanyonUpdraft node owns bounds, velocity limiting and native placeholder cues. PlayerMotor reads only live eligible fields before collision, without persistent registrations or fall-height exemptions.

**Tech Stack:** Existing Godot4.7.1 C# Mono; pinned .NET9.0.317 and owned isolated verification.

**Spec:** docs/superpowers/specs/2026-10-15-abyss-updraft-design.md

## Global Constraints

- Preserve well geometry, checkpoint/fragment IDs, global Gravity1850 / MaxFallSpeed780 and original 4≤seconds<6 oracle.
- World rect(1956,1360,534,2120); target500px/s; braking3000px/s². No paid art, Git writes, installs, user-data mutations or automation.
- Root is sole source/build/engine writer. No edits during an owned consumer. New files legitimately increase source rows; exact inventory and all native diagnostic gates remain mandatory.
- User's explicit reasonable-direct-execution instruction supersedes additional stage approvals, new-worktree and commit defaults. Existing linked worktree and dirty baseline preserved.

## Review Focus

- Dash, upward jump and actual claw ownership: field must not steal them (task2 unit/runtime plus existing actions/claw regressions).
- Environmental braking cannot reset fall height or masquerade as claw braking (task2 ordinary-floor drop).
- Staged, paused, deleted or different-world wind must not remain effective (task2 field eligibility/runtime).
- Reset while inside field must not lose eligibility; exiting must remove effect immediately (task2 reset/exit controls).
- A title-to-Boss failure later in the route stays a failure despite a passing local descent (task3 normal route, retain raw failure).

### Task1: Pin the missing saved mechanic

**Files:** Create tests/integration/CanyonUpdraftRuntimeTests.cs; modify tests/RunStableClawPullTests.cs and verify-claw.ps1.

**Interfaces:** Run(SceneTree tree) uses packed descent and actor; no gameplay position writes after tree entry.

- [ ] Add saved-node presence assertion; run owned `updraft` phase with explicit1646 source rows; require exact missing-field failure with no unrelated native diagnostics.

Execution record: presence assertion ran, but205543 also emitted40ShapeRIDexit diagnostics and therefore did NOT satisfy the clean missing-field baseline. This checkbox is deliberately not marked green. The independent original Main201631 actual3.117s failure supplies the behavioral timing conflict; neither historical artifact is rewritten as a passing case.

### Task2: Implement local force and cue

**Files:** Create scripts/mechanisms/CanyonUpdraft.cs; modify scripts/player/PlayerMotor.cs, CanyonContentBuilder.cs and task1 tests. Update owner exact source-row expectations to1647; builder accepts checked explicit count and freezes new force source.

**Interfaces:** CanyonUpdraft.ApplyAtPlayer(CharacterBody2D,Vector2,double) and LimitDescent(Vector2,float,float,float); motor applies only ordinary free falling velocity. Generated node name Gameplay/Mechanisms/AbyssUpdraft.

- [x] Add limiting/boundary/reset/pause/unload/fall-height tests before implementation, retain main 3.117 behavioral failure as independent RED. Strengthen real jump/down-dash/attached/release controls with exact free-candidate counterfactuals;212054 strict pass.
- [x] Implement bounded velocity limiting, eligibility, fixed warning and native wind-port/upward-streak cue; integrate before collision and separately from grapple correction. A read-only candidate diagnostic does not change forces.
- [x] Generate descent then parent with exact one-output owners; run strict focused phase and keyboard/actions/claw/fall regressions. All four regression raw/manifests and copied binaries independently verified. Final label-only45px refinement regenerated213250/213402 and UI213445 strict pass.

### Task3: Review and handoff

**Files:** README.md, docs/玩家修改意见与世界设定.md, entrance progress ledger; retained media directory per project README.

- [x] Run normal Main preserving original4≤seconds<6 oracle; strengthen clock to actual Engine.GetPhysicsFrames without relaxing threshold. Final213547 strict0/202927ms/guards0, Title→sameactorBoss→supportedexit; actualfall4.633s. Explore steam battle remains a separate known failure, not an updraft outcome.
- [x] Obtain read-only fresh review of new delta/evidence; fix confirmed in-wind ownership test gap. Final reviewer no Critical/Important/Minor within bounded delta, fullgoal remainsOPEN.
- [x] Archive original and presentation proof before viewing; review actual visual cue and update durable status/user choice. v00151/18679026 andv00270/23369081 independently allhash/size verified, original310frame movies+sourceaudio retained (digital silence). Root reviewed6localv2images/samples and3actualfog/HUDimages; overallNEEDS_WORK. No whole-goal completion without full acceptance obligations.
