# Resident Far Patrol Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans inline. Root owns all edits/build/native, helpers readonly. User continuous execution selected; no routine approval stall or narrowed full goal.

**Goal:** Restore authored autonomous patrol outside combat detection while preserving near combat and old wave behavior.
**Architecture:** New saved-actor/real-Player native fixture pins the coordination defect, then change only the resident patrol target input. Exact phase admission and original suites qualify the current merge.
**Tech Stack:** Installed Godot4.7.1 mono/C#, existing canonical headless wrapper.
**Spec:** `docs/superpowers/specs/2026-11-09-resident-far-patrol-design.md`.

## Global Constraints

- KNIGHT G root/full11 ACTIVE only; no GUI/OS/paid/art/automation/Git/worktree/delete/real-save experiment/engineGCcache/newdump.
- No input/source edits during owned runs, original strict caps/logs/receipts, no same-version retry to sift green.
- Author slot/home/patrol/territory and saved floor/Player/actor geometry unchanged; controlled fixture explicitly not automatic normal-world population.
- No after-tree pose/velocity/HP/AI writes; real native contacts/LOS/attacks only, no direct damage/TryConnect. Preserve old combat Step/seesPlayer/authorization/timing/numbers.
- All raw failures retained; current source/binary/protected/helper identities and one final mediaCheck required. No broad stable/release/final-art claims.

## Review Focus

- Out-of-range yet LOS-true same-territory residents must patrol, not freeze or chase; no false pass due to occlusion/ungrounded/out-of-domain target.
- Near real Player must still be detected and actually struck with original warning/active/identity; merely changing a residence enum is not a fix.
- Already committed combat, LOS/territory cancellation, dormant/ghost/stunned/returning and old wave paths must not acquire accidental new cancellation or patrol rights.
- Borrowed saved resources and half-initialized fixture ownership/teardown must preserve primary failures and not introduce disposal/cache hacks.
- Full normal routes can change encounter timing from genuine patrol; keep budgets/HP/counts/traps and report actual failures rather than suppressing actors.

## Task1: Real native counterexample and minimal coordination fix

**Create:** `tests/integration/ResidentPatrolRangeRuntimeTests.cs`.
**Modify:** `scripts/enemies/RustShellSquire.cs`, `tests/RunStableClawPullTests.cs`, canonical `.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1`.
**Interfaces:** `ResidentPatrolRangeRuntimeTests.Run(SceneTree tree)` runs five real-disengage/far-authored/near-detection/close-real-hit/default-wave controls; runner admits only exact `resident-patrol-range`; wrapper requires unique completion receipt and scoped inputs.

- [x] Read current state/skills and seal v332 baseline; ledger rulings and single-task preflight.
- [x] Add native fixture/phase before production fix; run canonical `-Phase resident-patrol-range -SourceRows <actual>` and require actual far-patrol functional RED after geometry/LOS/native guards.
- [ ] Apply minimal resident patrol detection coordination after root cause confirmed. Prove the added actual-D withdrawal control fails the old arbitration, restore the exact range condition, then run the phase on new source and expect all5 real cases GREEN; do not alter combat timeline/permission/parameters. Native012227 justified splitting chase from close hit, not deleting the independently recorded outer-range whiff candidate. No full-route J/K/L completion gate is claimed by the single-D fixture.
- [x] Freeze increment. Run old `resident-patrol` aggregate plus original11 phases including ordinary Main/Explore and independent save cold, preserving failures. Fresh single review C/I one TDD fix pass only, Minor deferred.
- [x] Audit same-source snapshots/current set/6 binaries/protected/tool identities; update report/README/backlog and one mediaCheck. Task not complete if any named strict gate fails; full goal remainsACTIVE.

Terminal result: final13=11strictPASS/2strictFAIL (newrange exit6ShapeRID; Explore visual453→455 missing adjacent physics publication). Original11subset10PASS/1FAIL. All5 functional controls passed, but strict GREEN requirement above is intentionally unchecked; Task1 is not complete. Fresh review C0/I0/M0; no second review/fix pass. See terminal report/ledger for exact cases,3346pairs78copies/currentidentity and one mediaCheck.

Self-review: one cohesive production behavior fix; all five focus lines have real case/old-contract/full-route validation. Native controlled scene is not full Level or new formal art. No test-only methods added to production.
