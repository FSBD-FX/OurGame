# Task 1 report — full-route integration coverage and mutation trust

## Result

GREEN. The real `PrologueLevel.tscn` route now verifies six authored rooms, readable ordinary/Boss attacks, typed mechanical-thread progression, cleanup and persistence, supported checkpoint recovery, real Boss retry/damage/completion, and bounded execution.

## TDD evidence

- Initial integration RED: `experience-integration-red.log` failed because the real Rust-Shell Telegraph was not yet observed.
- Corridor RED/GREEN: `corridor-single-jump-red.log`, `corridor-edge-launch-suite.log`, and `corridor-checkpoint-support-red.log` proved the missing SingleJump segment, impossible sample-20 edge, and unsupported checkpoint. The final 20/50/80 samples use same-frame Move+Jump with zero Dash/thread; the following authored segment remains Jump+Thread.
- Mechanism RED/GREEN: `mechanism-dais-single-jump-red.log` and `mechanism-dais-traversal-suite.log` proved the undeclared 104 px rise and the final 20/50/80 zero-Dash/thread landing on the lowered Dais. The continuous smoke jumps above the enabled Shortcut and reaches the real `P1Complete` exit.
- Presentation downstream RED/GREEN: `task1-verify-pass1.log` recorded the old checkpoint fixture restoring to Awakening Courtyard at frame 805. `presentation-checkpoint-green.log` reads the real `Checkpoint` trigger position and retains the exact final frame/markers at frame 2869.
- Review-oracle RED/GREEN: `task1-review-oracles-red.log` names the missing nonzero attack-danger precondition. `task1-review-oracles-green.log` proves a natural same-anchor retry after Obstructed without `ResetTransientState`, exactly one new hit and real pull, then observes a real Laborer Active danger VFX immediately before accepted fatal damage. The added real waits bring the bounded route to 4156/4700 physics frames with 544 frames of headroom.

## Runtime observations

- Ordinary attacks: RustShellThrust, MothDive, and LaborerSweep each publish Telegraph before Active; locked danger and live world-space hitbox geometry overlap.
- Boss attacks: BossSweep, BossLeapSlam, BossGroundWave, and BossHammerThrow satisfy the same real-publisher/real-hitbox contract.
- Thread: nine real successful casts cover Tap and Hold; every success visits Windup, Travel on at least two distinct `Engine.GetPhysicsFrames()` values, Latched, and Apply. The named Obstructed cast produces zero pull/hit, recovers naturally to Idle, and then retries the same authored anchor without `ResetTransientState` for exactly one new hit and real pull.
- Cleanup/persistence: the death boundary begins with a real Laborer Active publication and nonzero typed danger VFX, while its held-thread setup independently establishes live line/shuttle/thread presentation. Accepted fatal damage synchronously publishes Failed/Cancelled and clears line/shuttle; recovery and room transitions reach Idle with zero typed thread/danger VFX. Pre-contact death keeps courtyard/start; real checkpoint contact persists Corridor/checkpoint and restores on CompletionFloor. The shortcut persists.
- Completion: Boss retry is reachable, 12 real player hitbox contacts defeat the Boss (5 in phase one), and completion persists exactly once.

## Mutation trust

| Mutation | Named failure | Exact restore |
| --- | --- | --- |
| Suppress Rust-Shell Telegraph publication | `reached Active before Telegraph` in `mutation-attack-telegraph.log` | `RustShellSquire.cs` SHA-256 `14D5B3FB7D4EFFFCADE4C06CB2976AB3D248EBF590D30E2BFC3CFED5770A4BB5` |
| Collapse Travel to one physics frame | `collapsed Travel into 1 distinct physics frame(s)` in `fix1-travel-distinct-frame-red.log` | `ThreadCastModel.cs` SHA-256 `77678F1789644FC5F6507E51BCF04690C0BF995258512EF001D77E355EEE2A65` |
| Skip fatal-damage thread cleanup callbacks | `actual=Preview/None` in `mutation-death-cleanup.log` | `ThreadAbility.cs` SHA-256 `2DCCF893EEED1DA479947FD328DF0099EE014FF5CFFE5633DEED65D8E24A89DD` |

## Final gates

- `task1-verify-pass1.log`: `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK` exactly once each; 9 real successful casts, 4156/4700 smoke frames, and display-backed maximum process frame 39.39 ms.
- `task1-verify-pass2.log`: the same three markers exactly once each with the same cast/frame totals; display-backed maximum process frame 39.09 ms.
- `task1-audit-final.log`: UI resolutions, audio/VFX, visual audit, and three Windows display-backed runs pass; maximum process frame 40.59 ms.
- Both full gates run all 20 builders, scan fatal/native/RID/leak output, restore generated-scene/project bytes, and verify 26 protected artifact hashes before success.
- Ten shared pre-existing dirty generated scenes were identified explicitly, compared against committed `HEAD` canonical semantics, restored byte-for-byte after builder validation, and left unstaged. All 25 generated scenes now receive the semantic oracle.
- The four actor builders serialize a redundant `metadata/profile_bounds` override already inherited from their referenced formal-visual PackedScene root. The semantic gate folds only that exact same-value override on the four named `Formal` instances; changed values, other metadata/nodes, missing references, and malformed referenced roots remain drift.
- Restoration is best-effort across all 26 protected paths and aggregates path-specific failures after attempting every file. The injected writer fault proves later paths still restore exactly.
- Godot log and merged console are checked independently: the five intentional negative-test warning first lines each occur exactly once, while unexpected, duplicate, or changed warnings fail closed.
- `git diff --check` passes. No paid generation was used.

One earlier audit sample reached 52.06 ms; the unchanged 50 ms threshold was retained. The immediate identical audit (41.01 ms), post-review complete gates (39.39/39.09 ms), and final audit (40.59 ms) passed. The existing per-frame allocation review remains a later certification item as ruled in the ledger.

## Fix round 1 verification

- Focused RED/GREEN covered dirty-scene builder mutation, inherited PackedScene metadata normalization, aggregate restoration fault injection, unexpected/duplicate/changed warnings, and collapsed same-physics-frame Travel. Every temporary runtime mutation restored its original SHA-256 exactly.
- `fix1-verify-pass1.log` and `fix1-verify-pass2.log` each end in one exact `PROLOGUE_VERIFY_OK`, one exact `PROLOGUE_CRITICAL_PATH_OK`, 9 successful casts, 4156/4700 smoke frames, and `PROLOGUE_PROTECTED_ARTIFACT_HASHES_OK count=26`. Display-backed maxima were 42.42 ms and 49.30 ms.
- Both passes report `byte_drift=25 semantic_drift=0 shared_dirty_checked=10 project_drift=0`; startup bytes are restored in `finally` even when the body fails.
- The final standalone audit passed build 0 warnings/errors, UI/audio/VFX/visual checks, three Windows display-backed runs, and `PROLOGUE_PRODUCTION_AUDIT_OK` at 42.42 ms.
- Final fatal/parse/unhandled/GCHandle/ObjectDB/RID/leak scans are clean. No paid generation was used.
- Independent Fix Round 1 re-review approved the scoped verification diff with no Critical or Important findings.
