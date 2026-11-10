# Vertical mantle landing implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root inline implementation, ONE fresh final review.

**Goal:** Drive actual fixed-root cloth through valid pure-vertical landing, not only whole-layer land pose.

**Architecture:** Reuse Timeline→SecondaryMotion→Formal→MapMantlePoint. Preserve horizontal spring and limits; add bounded Y target from vertical speed and existing landing signal, no new spring/state/resource.

**Tech Stack:** pinned Godot4.7.1.NET/C#, canonical verifier.

**Spec:** docs/superpowers/specs/2026-10-28-vertical-mantle-landing-design.md

## Global Constraints

- Existing1857/897 KNIGHT dirty checkout; root sole writer/native owner, no worktree/Git writes/deletion.
- No paid assets/UI/OSinput/real-saves/GC/cache changes/another ProcDump capture.
- Same X/rotation, 12×7 bound/.42angle/spring8, ten slots/assets and physics unchanged.
- Freeze source/helpers/docs/bin during owned runs/review/custody; retain all failures.

## Review Focus

- Landing is a real Timeline ground edge, not manual secondary injection.
- Landing contribution is distinguished from preceding vertical velocity and whole-node land rotation.
- Both facings preserve pinned band, single mirror and finite bounded cloth points.
- Zero/invalid/delta partition and existing horizontal lag remain intact.
- Release/idle converges without retained cloth offset or gameplay/Resource ownership change.

### Task1: real producer RED→GREEN

**Files:** tests/integration/PlayerMantleMotionTests.cs; scripts/presentation/SecondaryMotionModel.cs; canonical thruster-mantle selected-copy list only.

**Interfaces:** Existing SecondaryMotionModel.Step and PlayerPresentationTimeline.Step/PlayerFormalVisual.ApplyPlayerFrame; no public API change. Original two test delegates retained, new helper inside existing second delegate.

- [x] Add mounted realTimeline vertical sequence with savedTuning, facings±1 and landing speeds180/650; compare pinned upper band and actual hem, groundedge vs identical airborne zero-velocity control, bounded finite geometry and300frame rest.
- [x] Add SecondaryMotionModel.cs to existing phase's entity-copy list, not phase semantics; run canonical -Phase thruster-mantle -SourceRows1857. Expected native named missing landing hem RED, not build/fixture failure.
- [x] Only after actual RED add -.006×Velocity.Y +7×LandingImpulse to mantleTarget.Y before original clamp; preserve X/rotation/spring/other channels.
- [x] Run same focused phase plus models. Expected allold/new checks GREEN and clean sinks; otherwise diagnose, never relax original limits.

### Task2: regression, review, custody and status

**Files:** v152 production record/README/original backlogV03/V04, this plan ledger.

**Interfaces:** consumes Task1's unchanged APIs and actual RED/GREEN receipts.

- [x] Run ordinary rig-compatibility/formal-claw/presentation-rig/keyboard/pre/post once, no repeat-to-green. Existing fullMain/Explore failures remain OPEN if not newly verified.
- [x] ONE fresh gpt-6-astra/high final review with Review Focus; one C/I actualRED→GREEN pass if needed, Minor defer, no rereview.
- [x] Verify own manifests/copies/bin/processes, normal one-shot media check, durable status; whole11goal staysactive/NOTSEEN. No merge/commit/delete or final-visual claims.
