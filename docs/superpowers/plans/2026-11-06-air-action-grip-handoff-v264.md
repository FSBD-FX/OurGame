# v264 Air Action Grip Handoff Verification Plan

> **For agentic workers:** Use superpowers:executing-plans inline. Steps use checkbox syntax.

**Goal:** Check actual saved visual grip geometry across a nonzero jet-to-ordinary bridge while action ownership changes, closing v261 M1's named test gap.
**Architecture:** Extend the existing saved Formal runtime fixture, not the source models or gameplay. Exercise the attack→shield→none presentation seam at 30/60/120Hz and both facings; observe actual bitmap contact and free/regrip endpoints. Investigate any real RED before proposing a change.
**Tech Stack:** Existing Godot/.NET runtime tests and canonical headless host.
**Spec:** `docs/superpowers/specs/2026-11-06-air-load-handoff-v261.md`, especially requirements 1–3 and the v261 final-review M1.

## Global Constraints

- Live G:/knight/Knight, preserve dirty in-place branch and original 1898-row scope. No git writes or cleanup.
- Only headless saved-scene geometry and existing internal inputs. No new art, GUI/OS input, actual saves, paid generation, native dump, GC/cache/lifetime or difficulty changes.
- This consumer fixture is not proof that a specific input sequence occurs at the exact modeled clock edge. Keep natural saved-player verification in the existing branch.
- No speculative production patch or fresh broad qualification if this is only an added regression test. Report previous native stability blocker separately.

## Review Focus

1. A matching zero-load rig isolates the retained carrier angle when authored Shield entry intentionally changes pose; same-time repeats retain all displayed geometry including wrists/elbows.
2. Primary grip must be checked against the actual authored bitmap, including both release and reacquisition.
3. Offhand must demonstrably move away, settle to free wrist and reach contact again; not merely skip an assertion under shield.
4. Repeat/time-freeze and three render rates cannot silently skip middle frames or advance another clock.
5. Honest limits: consumer schedule versus natural keyboard fixture, no stability, visual taste or full-prologue acceptance transfer.

### Task 1: Compound visual handoff proof

**Files:** `tests/integration/OrdinaryAirMotionRuntimeTests.cs`; existing `tests/RunThrusterCoreTests.cs` route unchanged.
**Interfaces:** private `CheckActionGripHandoff(PlayerFormalVisual)` inside `RunNonzeroAirHandoff`; consumes actual `PlayerFormalLayer` wrists, elbows and Draw geometry. No new production API.

- [x] Write six saved-rig cases with settled nonzero AirAttack; enter Shield at the same presentation clock as carrier replacement, then advance and enter None during bridge. Verify actual wrist endpoints, primary contact, monotone intermediate release/regrip and repeated-time freeze. Catch instant release/reattach, wrong contact flag, stale hand attachment and double advancement.
- [x] Run existing canonical `-Phase thruster-ground-gait -SourceRows 1898`. Actual `AIR_ACTION_GRIP_HANDOFF_OK cases=6` plus unchanged native tests and natural player branch; 16061ms PASS.
- [x] Pass path: retain test-only change, no production counterexample or fabricated TDD fix.
- [x] Bind source/output/host guards and do one fresh independent read-only review. No commit/cleanup; original stability and full-goal gates stay open.

### Task 2: Native input companion

**Files:** `tests/integration/ThrusterPresentationRuntimeTests.cs`, `tests/RunThrusterCoreTests.cs`.
**Interfaces:** `RunNaturalAirActionHandoff(SceneTree)` uses the existing no-floor `Fixture`; no production APIs, changed authoring, synthetic action injection or test-owned alternate controller.

- [x] Preserve both original natural no-action cases. Use actual internal A/D+Shift, then late-Coast J; prove one accepted horizontal attack, same airborne Fall and AirAttack on both sides of natural lease expiry, and retained actual hip geometry and weapon contact.
- [x] After the observed boundary, hold L until physical shield and rendered free offhand, then release without a new primary; observe actual Shield.Recovery, None and renewed secondary contact. Check primary bitmap from the boundary through regrip, separate authored movement from common load, and no extra jumps/dashes/refund. Do not assert Shield itself overlaps the exact expiry.
- [x] Full canonical ground-gait with new native consumer: quiet18263ms PASS, both new suites and existing phase markers. Both tasks reviewed together C0/I0/M0.
