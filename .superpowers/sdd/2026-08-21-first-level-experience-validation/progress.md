# SDD ledger — plan: docs/superpowers/plans/2026-08-21-first-level-experience-validation.md

## Setup

- Worktree: `E:/OurGame/Knight/.worktrees/player-art-pipeline`
- Branch: `codex/player-art-pipeline`
- Starting HEAD: `ea055a711add36fed984aa5c868ceeccf627e48e`
- User authorization: approved continuous completion and validation of the full prologue first level on 2026-08-24.
- Baseline: independent Windows display-backed build 0 warnings / 0 errors, production audit clean, five fresh Pause processes ×20 clean, and two consecutive full suites with exactly one `PROLOGUE_TESTS_OK` each.
- Paid generation: prohibited; no pay-per-use generation will be invoked.

## Preflight dependency scan

| Tasks | Producer / consumer relationship | Finding |
| --- | --- | --- |
| 1 → 2 | Full-route runtime contracts provide the named attack/thread/action/cleanup states later captured as deterministic proof. | Clean: Task 2 may consume only public markers and must not fake runtime state. |
| 1 → 3 | The twice-green full gate establishes the exact candidate later launched for human play. | Clean: no input injection or human report writing is allowed in Task 1. |
| 2 → 3 | Automated proof establishes visibility but cannot establish human understanding or completion. | Clean: deterministic capture and HandsOnLauncher evidence remain separate. |
| 3 → 4 | Fresh human telemetry and qualitative notes feed README truth and final certification. | Clean: a failed or unclear run keeps the goal active. |
| Task 1 internal | Existing smoke already traverses six rooms, tap/hold thread, obstruction, death cleanup, Boss retry, real player damage, and completion. | Extend the real route and public frames; do not create a parallel synthetic level. |
| Task 1 shared files | `RunPrologueTests.cs`, verification scripts, and smoke are shared gates. | Execute sequentially; retain the display-backed auto-start adapter and all protected-file restoration. |

## Rulings

- Ruling: integrated assertions must observe real actor publishers, real hitboxes/shape frames, real thread state progression, and real cleanup nodes in `PrologueLevel.tscn`; a hand-built frame or direct gameplay method call is not valid experience proof.
- Ruling: Task 1 may change integration wiring only when a focused RED proves an owning subsystem defect. Otherwise it owns tests and gate orchestration only.
- Ruling: three mutations must be non-synonymous and restored byte-for-byte before GREEN. They may temporarily alter the owning runtime source under a snapshot/finally discipline, but no test-only branch may remain in production.
- Ruling: exact success markers are counted, protected generated scenes plus `project.godot` are restored in `finally`, and unrelated imports, UID files, screenshots, scenes, art-tool edits, and local logs remain unstaged.
- Ruling: the previously deferred per-frame room descriptor/protected-region allocation remains an explicit final-certification performance review item; it is not silently relabeled complete by integration tests.
- Ruling: Task 1's real continuous route proved the Corridor `EntryFloor → TeachingLedge` gap is about 125 px high versus about 94 px single-jump rise, while the declared assisted contract starts only on the ledge. This is an in-scope authored-route defect, not a hidden Dash test. Lower `TeachingLedge` by about 40 px for margin and add an explicit `entry_floor_to_teaching_ledge` SingleJump contract; keep the following `entry_to_anchor_landing` as Jump + Thread and require zero Dash on the mandatory route.
- Ruling: three-sample runtime RED required extending the lowered Corridor TeachingLedge left edge from x=40 to x=0 while preserving its right edge and following assisted path; all launch samples must pass with same-frame Move+Jump and zero Dash/thread.
- Ruling: the authored Corridor checkpoint was centered in the unsupported gap between AnchorLanding and CompletionFloor. Move both marker and trigger to supported `(1120,198)` and prove pre-contact death does not advance it, post-contact death does, and recovery remains playable.
- Ruling: the Mechanism main completion route was intercepted by the lower Shortcut because CompletionDais required a 104 px undeclared rise. Lower the dais by 20 px to an 84 px rise, add `chain_floor_to_completion_dais` SingleJump, and require a real jump-over path to `P1Complete` with the Shortcut left active.
- Ruling: the deterministic presentation driver must follow the authored checkpoint trigger after the approved move instead of retaining the old coordinate assumption; read the real node position and preserve its existing frame sequence/markers.

## Progress


- Task 1: fix round 1/5 complete; independent re-review approved with no Critical or Important findings. Base implementation `a4ec86b` passed gameplay integration; the rejected verification false negatives are closed by all-scene committed-canonical semantic comparison, aggregate best-effort restoration, exact warning contracts, and distinct physics-frame Travel sampling. Gameplay/map integration findings remain closed.
- Task 2: pending (deterministic proof route and multi-resolution sensory audit).
- Task 3: pending (fresh hands-on run and evidence-backed correction loop).
- Task 4: pending (final certification, README truth, and playable handoff).

## Task 1 completion evidence

- Real six-room smoke completes in 4156/4700 physics frames, including supported pre/post-checkpoint death behavior, Corridor SingleJump + Assisted traversal, Lower route, Mechanism Dais + enabled-Shortcut jump-over, Boss retry, four Boss attacks, 12 real player hits, and exact completion. The 200-frame increase is the measured cost of same-anchor natural retry plus waiting for a real nonzero Laborer Active danger VFX; the 544-frame headroom is preserved.
- Independent review's two Important oracle gaps are closed: Obstructed naturally retries the same anchor without `ResetTransientState` for exactly one hit/real pull, and accepted fatal damage now begins from a real nonzero attack danger VFX before cleanup proves all transient attack/thread presentation is zero.
- Three independent runtime mutations failed the named Telegraph, multi-frame Travel, and fatal-damage cleanup assertions and restored exact production hashes.
- Post-review `task1-verify-pass1.log` and `task1-verify-pass2.log` each contain exactly one `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`, plus 9 real successful casts and 4156/4700 smoke frames; display-backed audit maxima were 39.39 ms and 39.09 ms.
- `task1-audit-final.log` ends with `PROLOGUE_DISPLAY_BACKED_AUDIT_OK runs=3 driver=windows` and `PROLOGUE_PRODUCTION_AUDIT_OK` at 40.59 ms; 26 protected artifacts retained exact final hashes.

## Task 1 fix round 1 evidence

- All 25 builder outputs now receive a stable semantic comparison. The ten pre-existing dirty outputs compare against committed `HEAD`, while their startup bytes remain protected and are restored exactly. Both complete passes report `semantic_drift=0 shared_dirty_checked=10`.
- Four actor outputs serialize a redundant formal-visual `metadata/profile_bounds` override. A narrow semantic normalizer removes it only from the four actor `Formal` PackedScene instances when the referenced PackedScene root parses successfully and carries the exact same value; changed values, other metadata, other nodes, missing references, and malformed roots remain drift.
- `Restore-ProtectedFileSnapshots` attempts every protected path, verifies exact restored bytes, and throws one aggregate containing every path-specific failure. The focused injected first-write fault proves both later files were still attempted and restored.
- `Assert-ExactEngineWarnings` checks Godot log and merged console independently. Each contains exactly the five intentional negative-test first lines once; focused fixtures reject unexpected, duplicate, and changed warning text.
- Travel lifecycle records a set of distinct `Engine.GetPhysicsFrames()` values. The `TravelSeconds = 0.000001f` mutation fails all nine casts with `collapsed Travel into 1 distinct physics frame(s) before Apply`, then restores `ThreadCastModel.cs` SHA-256 `77678F1789644FC5F6507E51BCF04690C0BF995258512EF001D77E355EEE2A65` exactly.
- `fix1-verify-pass1.log` and `fix1-verify-pass2.log` both pass: exact Verify/Critical/26-hash markers, 9 casts, 4156/4700 frames, 25/25 semantic checks, 10 dirty canonical checks, and display-backed maxima 42.42/49.30 ms. `fix1-final-production-audit.log` passes three Windows display-backed runs at 42.42 ms. Final failure/leak scans are zero.
- Independent Fix Round 1 re-review approved the scoped diff with no Critical or Important findings.
