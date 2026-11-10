# Incoming guard-priority implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. Root alone edits/builds/native; one final fresh whole-change review.

**Goal:** Stop the postHop test driver yielding its direction to a later Rust warning while an observed Burst circle can reach the actual player capsule in the next predicted step; verify with native Main, not just geometry.

**Architecture:** Optional driver-only incoming context defaults null on all old consumers. A per-encounter scalar identity history supplies adjacent observed displacement. Pure relative swept-circle/capsule geometry returns Unknown/Clear/Incoming; Unknown or Incoming only vetoes NearRust override. It does not change normal Active sorting, damage/clock/J/HP/recovery gates or production gameplay. Actual HP/guard receipts are the acceptance evidence, never an estimated TOI.

**Tech Stack:** Existing Godot4.7.1/.NET9/C#, two existing test CS, SourceRows1872.

**Spec:** docs/production/2026-11-03-mixed-guard-switch-window-v209.md; stable original eleven-item backlog remains ACTIVE.

## Global Constraints

- G:/knight/Knight only; preserve dirty state, baselines and all failures; no Git mutation/deletion/worktree/paid generation/GUI/OS input/real saves/runtime change/new dump/automation/publish.
- Original canonical caps build180s/policy55s/Main600s/pre180s/post300s, SourceRows1872; root freezes source/helpers during every native/build. No ExpectedFailure or budget growth.
- Policy opt-in only the already-owned postHop K stage3/Hop5 Main outer/fresh-J scans; share one value history, no extra query/scan/tick/input. Other consumers retain null/default semantics.

## Review Focus

- Use complete capsule axis/endcaps/world radius; controls include endpoint crossing, interior crossing, tangency, rotated/scaled capsules, degenerate axis, invalid/nonfinite shapes.
- History identity includes actor/projectile/leaf/attack; first/gapped/repeated/reversed physics, sequence rollback, dt/radius changes remain Unknown; independent literal controls pin these cases.
- No passive observer affects policy; original Consider computes incoming separately before diagnostics, default null preserves old policy.
- Prediction never proves safe counter clock or deletes Active/Burst18/Rust threats; only final near override may be vetoed, and actual native HP/zero-guard/direction receipts must verify the chain.
- Current velocity is a constant-step estimate, not native TOI or guaranteed next motion; transformed/finite validation and strict native outcomes prevent prediction-only success claims.

## Task 1: driver-only incoming policy

**Files:** Modify tests/integration/PrologueCanyonGeneratedMainRouteTests.cs and tests/integration/RangedWeaponPlacementRuntimeTests.cs. Baselines under .superpowers/sdd/2026-11-03-incoming-guard-priority; durable report/README/backlog afterward.

**Interfaces:** Task10IncomingStatus enum; Task10IncomingCircleStep(circle,capsule,observedDisplacement,playerVelocity,delta); Task10IncomingGuardHistory.Observe(actor,projectile,leaf,attack,physics,sequence,delta,circle,capsule,playerVelocity); optional Task10IncomingGuardContext(history,physics,sequence,delta,velocity) appended to Task10ObserveEncounterDanger. Unknown/Incoming veto final near override; all old outputs/gates otherwise unchanged.

- [x] Add literal geometry/history controls with disabled stubs; canonical ranged-mixed-policy must produce named behavioral RED.
- [x] Implement pure segment distance + strict identity history, GREEN same controls and old actual policy tests.
- [x] Wire only original outer/fresh-J postHop consumers and original Burst Consider; run actual Main once, retain strict result and native damage/guard/18/Rust evidence. Do not claim route success if original900/Boss/exit fails. (MainstillFAIL; HP2/Rust1 progression measured, preciseBurstguardreceiptOPEN.)
- [x] Single fresh whole-change review, C/I one RED→GREEN fix pass, minors deferred/no re-review; final actions/keyboard/policy/models/pre/post and Main if source changes. No all-green claim if any failure. (I1tangentRED→doublelocal28GREEN; final7phases5PASS/2FAIL.)
- [x] Audit current source/helper/bin, owned processes/HEAD/index, media threshold; update facts/inferences/risks, leave original eleven goal active until all formal gates truly met.

Implementation tested; fullchain acceptance/whole-green-suite and original eleven-item goal remain OPEN/ACTIVE. No release or formal art/physics completion claim.
