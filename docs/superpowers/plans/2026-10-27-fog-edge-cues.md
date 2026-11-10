# Fog edge cue implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prevent valid screen-edge attack geometry from losing its above-fog command solely because the source origin is outside the viewport.

**Architecture:** Keep the existing live-source pipeline. Replace only the origin viewport predicate with allocation-free conservative frame-shape bounds in logical canvas coordinates. Add an explicitly headless fixture phase, without changing existing trap delegates or drains.

**Tech Stack:** pinned Godot 4.7.1 .NET, C#, canonical PowerShell verifier.

**Spec:** docs/superpowers/specs/2026-10-27-fog-edge-cues-design.md

## Global Constraints

- Live project G:/knight/Knight; preserve dirty worktree, source inventory 1857/897 C#.
- No visible window, OS input, paid generation, automation, Git writes, deletion, real-save reads/writes, new GC/drain or widened watchdogs.
- Root sole writer/native owner; freeze source/helpers/binaries during native runs and fresh final review.
- Keep all gameplay, LOS, 620 range, visibility and live-world gates unchanged. No Grip8 cause claim or pixel/human acceptance.

## Review Focus

- Convex points already world-space even when source local transform differs.
- Logical canvas transform under scale/translation; never physical FinalTransform.
- Capsule shorter than diameter still draws a circle of radius Extent.X.
- Bounds overflow/nonfinite or entirely offscreen must not repaint stale cues.
- New fog-prefixed phase must be headless and reject mixed visible/diagnostic flags.

### Task 1: native failing command fixture and minimal production fix

**Files:** tests/integration/AttackTelegraphRuntimeTests.cs, tests/RunStableClawPullTests.cs, scripts/presentation/fog/FogReadabilityOverlay.cs, canonical .superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1.

**Interfaces:** RunFogEdgeCommands(SceneTree) -> Task; production private ShapeOverlapsViewport(AttackShapeFrame, Transform2D, Rect2) -> bool. PresentAttack and PresentAttacks public signatures unchanged.

- [x] Add exact fog-edge-commands admission/routing and native fixture: source(144,64), rectangle half-extents(20,9), viewport128x128, actor(64,64), complete native snapshot. Expected PresentedCueCount=1 although source is outside. Run canonical phase with SourceRows1857; expected named behavioral RED, not build error.
- [x] Add allocation-free transformed shape bounds, replace only viewport gate. Run same canonical phase; expected zero exit, exact FOG_EDGE_COMMANDS_OK with pixels=false/human=false.
- [x] Extend native fixture with each Review Focus input and fail-closed controls; expected hand-derived counts, not helper-mirrored answers. Run focused phase again.

### Task 2: regression, fresh review and durable status

**Files:** README.md, original eleven-item backlog, player feedback journal, new v149 production record and this plan's progress ledger.

**Interfaces:** Unchanged canonical ordinary phases and retained raw case roots; task1's headless command receipt is narrow, not whole-route certification.

- [x] Run unchanged ordinary models, keyboard, recycling-traps, prologue-pre and prologue-post plus normal assembly-absence phase; retain and report every failure without waiver or repeat-to-green. Routes' prior v148 failures remain unqualified unless freshly run.
- [x] Freeze current source/helper/binary identities and obtain ONE fresh-context final review; root only one RED→GREEN pass for Critical/Important, defer Minor. No rereview.
- [x] Audit all owned case manifests/process terminal records, update durable status and normal one-shot media-size check. Do not mark original eleven-item goal complete or claim visible quality acceptance.

Execution note: checked steps record work performed, not green-suite qualification. Ordinary pre strictFAIL/13 Handle ERRORs remains OPEN. See production/2026-10-28-fog-edge-cues-v149.md and this plan's ledger; Task 2 cannot certify whole-suite stability.
