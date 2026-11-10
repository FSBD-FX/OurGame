# Ranged character rigs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax for tracking.

**Goal:** Give four existing ranged actors distinct complete articulated bodies, real attack-driven motion and replacement art slots without changing gameplay.
**Architecture:** Pure value rig drives existing default renderer; an interface separates presentation from concrete visuals. Native attack identities deduplicate recoil and an explicit checkpoint event resets the visual generation.
**Tech Stack:** Existing Godot4.7.1/.NET9 C#, no runtime/dependency change.
**Spec:** `docs/superpowers/specs/2026-11-03-ranged-character-rigs-design.md`

## Global Constraints

- Sole root writer and serial native owner; freeze C#/helpers while native/build live. Headless only; no paid assets, GUI/OS input, real saves, Git mutation, deletion, automation.
- Preserve original colliders/HP/Muzzle/attack geometry and timelines/1-3-1-1 emissions; no Main tactic change.
- One new pure CS, SourceRows1874; exact pre-edit baselines, full raw cases, explicit helper input capture. Existing caps unchanged.
- Developer program sample, not approved/final art or motion. Original eleven items and R-I/R-C/R-P stay OPEN.

## Review Focus

1. Replay/cancellation/death cannot revive old attack transients; checkpoint reset is distinct from Idle.
2. Rotated/scaled ancestors must not reinterpret a world aim as local or alter gameplay Muzzle.
3. Limbs keep length, feet keep ground baseline, body/action continuity follows elapsed time rather than duplicate event publication.
4. Borrowed texture lifetime and non-square uniform-fit draw geometry survive actual slot replacement.
5. Replacement visual interface receives real saved actor events, cleanup and checkpoint reset without concrete-type dependence.

### Task 1: Complete four-role rig and native presentation contract

**Files:** Create `scripts/enemies/ranged/RangedCharacterRig.cs`; modify existing `RangedWeaponContract.cs`, `RangedWeaponVisual.cs`, `RangedWeaponPresentation.cs`, `RangedWeaponEnemy.cs`, `tests/integration/RangedWeaponPresentationRuntimeTests.cs`, canonical `.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1`; update README/backlog/asset record with results.

**Interfaces:** `IRangedWeaponVisual2D.PresentWeaponFrame(role,frame)`, `ClearAttackPresentation()`, `ResetWeaponPresentation(role)`; existing actor emits `PresentationResetRequested` at checkpoint only. `RangedCharacterRig.Reset(role)`, `Apply(frame)`, `Advance(double)`, `ClearAttack()`, `Pose`; renderer `GetPartGeometry(string part,bool far=false)` is also the actual Draw source. All eight slots borrowed.

- [x] Write existing-runtime-entry tests first: missing complete rig/slots/interface must fail with an explicit behavior assertion; preserve old runtime checks.
- [x] Run canonical ranged-presentation SourceRows1873. Expected intentional RED for missing rig, not compile error.
- [x] Implement production rig/Draw/interface/reset; add new CS to selected helper inputs and required success markers. Test profile/phase/cancel/recoil/IK/finite/time/actual fit/rotated aim/real actor reset/replacement. Expected narrow GREEN1874, no unrelated behavior changes.
- [ ] Run relevant ranged models/geometry/actor-saved/protection plus original actions/keyboard/models/player rig/gait/compatibility and full pre/post/Main/Explore, unchanged caps. Expected all gates green for completion; any actual failure keeps Task1 INCOMPLETE.
- [x] One fresh whole-change review, regrade findings, one C/I RED→GREEN fix pass. Defer minors; no re-review. Localized fixes verified; whole suite still2FAIL, Task1 remains INCOMPLETE.
- [x] Preserve evidence/ledger, report rulings/costs and deferred minors. No commit/workspace deletion because Git/deletion not authorized. Never mark task or full goal complete on partial tests.
