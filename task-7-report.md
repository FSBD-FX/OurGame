# Task 7 report — deterministic smoke, proof capture, and durable status

## Status

Complete.

## Delivered

- `tests/smoke/PrototypeSmoke.cs` is a fixed-physics-frame, hard-budgeted end-to-end room check. It loads the generated room, waits two physics ticks, then proves the player group, exact `Geometry/ThreadAnchor` and `Actors/RustShellSquire` contracts, input mapping, movement, jump, combat damage, anchor pull, world occlusion, and local fall reset. A success emits `PROTOTYPE_SMOKE_OK`; any contract exception emits a failure and exits `1`.
- The room builder now intentionally exposes the brief’s stable grouping paths, and legacy structural/runtime tests follow those paths.
- Real attack contact was repaired: `PlayerCombat` is a `Node2D`, so its hitbox follows the player’s world transform. The source hitbox itself remains non-monitorable; hurtboxes remain monitorable, which is the minimal collision configuration required for attack overlap callbacks.
- Thread line of sight now excludes all player collision objects and accepts a ray hit on a target’s collision child while continuing to reject a world body ahead of the target. Thread cooldown uses deterministic physics-time accumulation, not wall-clock time.
- `tests/presentation/PrototypePresentation.cs` drives a 720-frame / 12-second route through variable jump, dash, thread, horizontal/up/down attack, fall reset, enemy pass, and exit. `tools/capture-prototype.ps1` records AVI and supports numbered PNG fallback.
- `tools/verify.ps1` now requires and runs smoke by default; `-SkipSmoke` still permits focused unit/build debugging. README has the playable-state, controls, commands, exclusions, asset table, and plan links.

## TDD and mutation evidence

1. Initial RED: `smoke-red.log` exited `1` at `Assertion failed: thread anchor exists`; the previous room used `GrayboxGeometry` plus root-level anchor/enemy nodes. The generator was changed to the specified `Geometry/ThreadAnchor` and `Actors/RustShellSquire` contracts.
2. Real-contact RED: `smoke-green-attempt4.log` reached `attack_damage` with `started=1`, `connections=0`, and enemy health unchanged. This isolated an input-consumed-but-noncontact defect. Changing `PlayerCombat` back to `Node` then regenerating the scene reproduces it in `mutation-playercombat-node-red.log`; it fails at `horizontal attack damages the enemy fixture`. The retained `Node2D` contract fixes transform propagation and yields health `3 -> 2`, one connection.
3. Thread-target RED: `mutation-thread-target-red.log` removes the target ancestor/descendant acceptance and fails at `thread pull velocity points toward the anchor`, while the physical target collision remains present. Restoring it passes both target selection and the smoke’s inserted-world-wall rejection.
4. A monitorable mutation was explored but showed that the player attack area does not need to be monitorable for its own `AreaEntered` callback. The unrelated experiment was reverted; no unnecessary monitorable/deferred-overlap production change remains.

## Proof capture and visual review

- Final AVI: `screenshots/prototype/prototype.avi`, 22,150,324 bytes at final capture.
- Godot movie output: 1280×720, 60 FPS, 720 frames, 12.00 seconds; `capture-prototype.log` contains every presentation stage and `PRESENTATION_COMPLETE frame=720`.
- PNG fallback was run once for audit: 720 numbered frames; sampled frames 0, 180, 360, 540, and 719 have distinct SHA-256 hashes. Visual inspection of frames 0, 360 and 620 confirms start state, the reached `PROTOTYPE COMPLETE` state, and a distinct exit-position frame. The frames and AVI are ignored local proof output.

## Final verification

- Leaf builders ran in order: Player (14 nodes), Rust-Shell Squire (9 nodes), PrototypeRoom.
- `powershell -ExecutionPolicy Bypass -File .\tools\verify.ps1` completed with zero build warnings/errors, `PROTOTYPE_TESTS_OK`, `PROTOTYPE_SMOKE_OK`, and no scanned `ERROR:`, `SCRIPT ERROR:`, or `Parse Error` log entries.

## Residual risk

The presentation is a deterministic proof route rather than a human-input usability study. It establishes end-to-end wiring and visibly progressing frames; final hand-feel tuning remains a later milestone.

## Review remediation

- Added shared PowerShell evidence checks and an isolated fixture suite. Capture now deletes only the previous output for the selected mode inside `screenshots/prototype`, records the run start time, and rejects missing, empty, or stale outputs.
- Capture requires exactly one line equal to `PRESENTATION_COMPLETE frame=720` in a fresh capture log and rejects `ERROR:`, `SCRIPT ERROR:`, or `Parse Error` entries.
- Verification requires exactly one `PROTOTYPE_TESTS_OK` line and, unless `-SkipSmoke` is selected, exactly one `PROTOTYPE_SMOKE_OK` line, in addition to exit-code and engine-error checks.
- README references now point to the tracked design specification and implementation plan under `docs/superpowers`.
- Review RED: `review-evidence-red.log` records the isolated fixture failing because `tools/EvidenceChecks.ps1` did not exist. GREEN: the same fixture prints `EVIDENCE_CHECKS_OK` and is now part of every verifier run.
