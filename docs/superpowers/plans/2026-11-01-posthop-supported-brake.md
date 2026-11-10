# Post-hop supported reverse-brake implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. Root alone builds/runs; one final fresh whole-change review.

**Goal:** Replace the observed post-second-Rust-K unbounded backoff/coast with one finite, physically sampled original-platform retreat and reverse-brake handoff, then retain the original J/danger/energy gates and test full Main.

**Architecture:** Test-driver-only planning uses actual ThrusterDynamics and frozen live motion/module snapshots. Choose the shortest force pulse whose complete reverse-plus-neutral envelope stays inside the original floor's capsule-center corridor and reaches the existing 8..64 strike gap against the current target position. Forecast is not collision or future enemy-position proof: every actual tick rechecks original support, source retention and conservation. No retry token or additional wait budget.

**Tech Stack:** Existing Godot4.7.1/.NET9; two existing integration-test CS files.

**Spec:** docs/production/2026-11-01-finite-posthop-rust-v204.md; original eleven stable items in docs/production/2026-10-18-project-remaining-backlog-v001.md remain ACTIVE, not replaced by this local goal.

## Global Constraints

- G:/knight/Knight only; isolated headless build/test; no GUI/OS input, real saves, paid generation, extra crash collection, automation, Git mutation or deletion.
- Original 900 encounter ticks, Main/Explore600s, pre180s, post300s and canonical SourceRows1872 remain. No numerical gameplay/map/selector changes or error suppression.
- Preserve all dirty work, evidence and failures. No worktree/commit/cleanup helpers that mutate Git or delete the ledger; explicit user scope overrides skill defaults.

## Review Focus

- Actual integration order on the K-return iteration must override already-computed generic A/D before it is sent; test finite stage transitions and verify native trace.
- Reverse can cross zero and accelerate the other way in the same tick; test the entire terminal/neutral envelope, not only a braking flag.
- Forecast must reject nonfinite inputs, unsupported/scaled motion, short clock/budget and corridor escape; literal boundary controls.
- Threats or a changed floor/actor/HP invalidate the lease; no automatic retry after rejection/interruption/completion.
- Sample latency must not become a repeated force pulse or new wait pool; actual new samples/velocity, release and neutral handoff recorded under original ticks.

### Task 1: Finite supported movement and native verification

**Files:** Modify tests/integration/PrologueCanyonGeneratedMainRouteTests.cs and tests/integration/RangedWeaponPlacementRuntimeTests.cs. Update README/backlog, this plan's progress ledger and production report after terminal results.

**Interfaces:** Add Task10PostHopBackoffPlan and TryTask10PostHopBackoffPlan(...) arithmetic, used by original Main immediately before its final A/D send. One local one-shot stage machine owns only A/D while active, releases on interruption and hands existing defense back unchanged. Completed controller suppresses repeated generic too-close retreat while still on the original floor; far-distance approach remains original behavior. Original J/24 reserve/clock/guard ownership and receipt gates unchanged.

- [x] Write literal arithmetic and one-shot transition controls, disabled helper stub; run canonical ranged-mixed-policy -SourceRows1872. Expected named behavioral RED, not a compile failure.
- [x] Implement shortest finite force/reverse/neutral plan with actual parameters; run same policy. Expected all new controls and original fixture PASS.
- [x] Integrate actual sampled finite controller, one original tick per step; capture initial conservation values and original floor/shape. Reject or interrupt safely without restarting; do not assert a completed encounter from a forecast.
- [ ] Run full Main diagnostic once, inspect actual pulse/brake/release/support and original encounter outcome. Expected local actual movement proof and onward progress; if Main fails, retain exact failure and identify next boundary, no gate relaxation.
- [x] Run final-source policy, keyboard, actions, models, full pre and post under original budgets. Explore only if Main reaches the needed encounter; otherwise explicitly defer current dual-route qualification.
- [x] One final independent whole-change review; C/I one TDD fix pass only, minors deferred; full suite failures named. Update durable status and normal report/media-size check; no completion claim for original eleven.

Outcome: Task1 remains OPEN: actual new finite movement was rejected on original-floor geometry, Main still FAIL. See v205 report for measured geometry and next independent bidirectional native plan; no unchecked goal is silently waived.
