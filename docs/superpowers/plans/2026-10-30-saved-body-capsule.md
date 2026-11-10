# Saved body capsule implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans task-by-task. Root is sole writer/process owner; agents read-only. Existing unique G:/knight/Knight codex branch is retained; no new worktree/Git mutations.

**Goal:** Read the actual saved player capsule for layout checks without instantiating a player or changing gameplay.
**Architecture:** Private SceneState reader and native test controls in the existing Basin test; no production helper API. Read borrowed saved graph, return one owned duplicate.
**Tech Stack:** Godot4.7.1 Mono/C#, existing native canonical headless runner.
**Spec:** docs/superpowers/specs/2026-10-30-saved-body-capsule-design.md

## Global Constraints

- Root sole writer; source/helper freeze during every root build/native process. No real saves/windows/input/paid assets/automation/Git/deletion/crash capture.
- Preserve all eleven items and original55s focused/180s pre/300s post/600s route caps; no AI/HP/geometry/input changes.

## Review Focus

- Borrowed resource disposal must not invalidate another scene consumer; duplicate mutation test pins original20/72.
- Missing/unsupported shape or transform rejects without runtime creation; native Pack negative controls exercise this.
- Initialization exceptions cannot leave an entered root or owned shape alive; root finally covers setup.
- Similar Hurtbox capsule must not be accidentally chosen; exact path/type/source dimension checks pin direct BodyCollider.
- Inheritance/duplicate corrupted property fixtures need independent construction before claiming coverage; no guessed raw encoding or false test pass.

### Task1: saved-body reader

Files: tests/integration/BasinClawTargetTests.cs and existing canonical helper only.
Interface: private `CapsuleShape2D ReadSavedBodyCapsule(PackedScene scene)` returns owned duplicate or InvalidOperationException.

- [ ] Extract the current actor-instantiating read unchanged; add native disabled-body control that the old implementation wrongly accepts. Run `-Phase basin-claw-target -SourceRows 1869` and verify actual behavior RED, not compile/setup failure.
- [ ] Implement direct SceneState reader with validation from spec; change layout setup to owned cleanup. Run same focused phase GREEN, exact unique new marker plus old layout/wall/Ice still required.
- [ ] One fresh review of complete slice, one C/I TDD fix round if needed; defer Minor. Run original models/pre/post; do not treat primary0+ERROR asPASS. Audit current immutable source/bin/helpers/ownedPIDs and update durable status/manualmedia check.

Commit/cleanup steps from skills are intentionally not executed: current user safety boundary forbids Git writes and deletion. Completion here is one verified slice, not the full goal.
