# Task 4 Report — Generated Scenes, Balance Gate, and Visible Proof

## Status

Automated Task 4 acceptance is complete. The generated combat actors expose the stable `VisualRoot/AttackTelegraph` leaf, serialize the approved first-level balance, pass the full production gate, and have fresh 1280×720/60 FPS static-stage and continuous-frame evidence.

Subjective readability at normal playback speed is **not** claimed by this report. The generated AVI files are playable at 60 FPS, but the available `view_image` review surface inspects still images rather than real-time playback. Final normal-speed acceptance therefore remains a human playtest item.

## Delivered contract

- Rust Shell Squire, Grave Lamp Moth, Bell-Back Laborer, and Fallen Bellkeeper mount exactly one recursive `AttackTelegraphVisual` at `VisualRoot/AttackTelegraph`.
- The telegraph is a presentation-only `Node2D`: no collision layer/mask, no gameplay ownership, `z_index = 6`; `VisualRoot/Effects` is `z_index = 10` so hit flashes remain above it.
- Builder validation rejects a missing/wrong telegraph script, gameplay/collision types, wrong z-order, or tuning drift.
- Generated-scene diffs were normalized after regeneration so the Task 4 implementation commit retained existing resource/node IDs and only added semantic content.

Approved generated balance:

| Actor / attack | Health | Telegraph | Active | Recovery | Damage |
| --- | ---: | ---: | ---: | ---: | ---: |
| Rust Shell / thrust | 3 | 0.65 s | 0.14 s | 0.65 s | 1 |
| Grave Lamp Moth / dive | 2 | 0.70 s | 0.35 s | 0.70 s | 1 |
| Bell-Back Laborer / sweep | 6 | 0.75 s | 0.20 s | 0.70 s | 1 |
| Boss / sweep | 12 | 0.80 s | 0.25 s | 0.70 s | 1 |
| Boss / leap slam | 12 | 0.90 s | 0.25 s | 0.75 s | 1 |
| Boss / ground wave | 12 | 1.00 s | 0.30 s | 0.75 s | 1 |
| Boss / hammer throw | 12 | 0.85 s | 0.80 s | 0.70 s | 1 |

## TDD evidence

Initial delivery:

1. Structure RED: `RunPrologueTests.cs` first failed at the absent Rust Shell `VisualRoot/AttackTelegraph`, proving the stable-path assertion was live before scene generation.
2. Leaf regeneration: Rust Shell, Moth, Laborer, then Boss builders each packed and validated their scenes.
3. First GREEN exposed an obsolete integration fixture that created a second telegraph node. The approved cross-task fixture adaptation in `tests/integration/AttackTelegraphRuntimeTests.cs` reuses the production stable path; no production runtime scope was expanded.

Fix Round 1 review corrections:

1. Boss-damage authenticity RED: the static fixture rejected the previous Boss retry driver's direct health call with `tests\presentation\BossRetryPresentation.cs cannot call ApplyDamage on an actor health fixture`. A second field-based bypass mutation then failed with `field-based direct Boss health setup is rejected by the proof-source gate`; the final source gate permits direct `ApplyDamage` only on the player health expression.
2. Boss-damage authenticity GREEN: both Boss drivers now press `InputActions.Attack`, wait for `PlayerCombat`, and count `AttackConnected` events whose target is the production Boss hurtbox. They never call Boss `Health.ApplyDamage`, never call Boss `Hurtbox.Receive`, and never write Boss state or health.
   - Approved cross-task capture fixture adaptation: `BossRetryPresentation.cs` reaches 7 HP from 12 HP through five formal player attacks before killing only the player to prove retry restoration. Marker: `BOSS_RETRY_REAL_PLAYER_DAMAGE_OK hits=5 health=7`.
   - Approved cross-task smoke fixture adaptation: `PrologueCriticalPathSmoke.cs` reaches the transition threshold through five attacks and defeats the Boss through twelve total formal player attacks. Marker: `PROLOGUE_BOSS_REAL_PLAYER_DAMAGE_OK phase_one_hits=5 total_hits=12 health=0`.
3. Attack identity mutation RED: deliberately recording `impact` as `AttackId + 1` failed with `attack proof identity drift for RustShell/RustShellThrust: gold=1, danger=1, impact=2, recovery=1` and emitted no completion marker.
4. Attack identity GREEN: each of the seven actor/kind sequences now requires four nonzero IDs and equality across gold/danger/impact/recovery before its exact-once success marker can be emitted.
5. Recursive uniqueness mutation RED: adding a second nested Rust telegraph failed with `Rust-Shell owns exactly one recursive AttackTelegraphVisual; expected=1, actual=2`. The mutation was removed; all four structure contracts are GREEN.
6. Forced-failure filter RED/GREEN: the PowerShell fixture first failed because no exact engine-error filter existed. `Assert-OnlyExpectedEngineError` now accepts exactly one expected forced error and rejects any additional `ERROR:`, `SCRIPT ERROR:`, or `Parse Error` line.

Fix Round 2 review corrections:

1. Static anti-bypass RED: a local Boss hurtbox alias (`bossTarget.Receive(...)`) initially passed the source gate and failed the mutation assertion `local alias direct Boss Hurtbox mutation is rejected by the proof-source gate`. A separate Boss-state mutation (`boss.Set(...)`) likewise initially passed and failed `Boss Set mutation is rejected by the proof-source gate`.
2. Static anti-bypass GREEN: the source gate now follows local/field aliases resolved from the Boss hurtbox, Boss, and non-player health. It rejects direct receiver/dynamic-call mutators, `Set`/`SetIndexed`, and property writes. It separately requires the real `_player` combat node, an `AttackConnected` subscription on that resolved alias, and `Press(InputActions.Attack)`. Direct player-health damage used to exercise death/retry remains legal. Both production proof drivers still pass at runtime with 5/12 formal player hits.
3. Late-cleanup RED: a fresh merged forced-failure run exited 1 after the expected error but also reported `4 ObjectDB instances were leaked at exit` and `2 resources still in use at exit`. A second mutation fixture proved both cleanup messages are rejected. The first merged verifier run also exposed a Windows PowerShell boundary: native stderr became a terminating `NativeCommandError` before the scan could execute.
4. Root-cause evidence: before forced failure the tree held 2395 objects / 236 resources / 314 nodes / 0 orphans; after shutdown, free, two frames, and one GC it held 1793 / 184 / 1 / 0. Disabling only the two startup audio players removed exactly four objects and two resources at exit. Retaining normal audio but pumping four more frames and a second GC removed the leak. This isolated asynchronous audio playback/resource disposal after `PrologueAudioDirector.Shutdown`, not a Boss subscription, orphan node, or suppressed engine error.
5. Cleanup GREEN: smoke teardown now gives queued audio/native wrappers four frames after the first finalizer pass, then performs a second collection/finalizer pass. The verifier temporarily treats native stderr as non-terminating, converts the merged stream to strings, restores the prior preference, and scans both the Godot log and preserved merged console. The focused forced run exits 1 with exactly one `FORCED_SMOKE_FAILURE` and no ObjectDB/resource leak.

## Deterministic presentation evidence

The 2280-frame presentation observes runtime `AttackFrameChanged` events and validates the live telegraph state before recording each evidence item: gold preparation only, locked red true danger, white Active impact with red cleared, then all telegraph flags clear in Recovery. Each row's four stages also carry the same nonzero `AttackId`.

| Sequence | Gold | Danger | Impact | Recovery | AttackId | Static/continuous visual review |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Rust Shell thrust | 430 | 450 | 469 | 478 | 1 | Gold thrust lane, locked red lane, white impact box, clear recovery |
| Moth dive | 836 | 857 | 878 | 898 | 1 | Gold dive area, locked red dive area, white impact ring, clear recovery |
| Laborer sweep | 1174 | 1197 | 1219 | 1231 | 1 | Gold sweep box, locked red sweep, white impact box, clear recovery |
| Boss sweep | 1512 | 1536 | 1560 | 1575 | 1 | Weapon-side gold wedge, red locked wedge, white impact wedge, grounded recovery |
| Boss leap slam | 1617 | 1644 | 1671 | 1686 | 2 | Gold landing ring, locked red ring, white slam ring, clear recovery |
| Boss ground wave | 1731 | 1761 | 1791 | 1809 | 3 | Gold floor lane, locked red lane, white impact lane, clear grounded recovery |
| Boss hammer throw | 1921 | 1947 | 1972 | 2020 | 4 | Long gold lane, locked red lane, white projectile/impact lane, clear recovery |

Seven fresh dense contact sheets were generated from the 60 FPS PNG sequence. Each sheet samples at least twenty chronologically ordered frames across the complete attack interval and includes the exact four key frames. All seven were opened with `view_image` and reviewed: preparation persists before danger; the danger geometry locks and turns red; impact turns white with the red fill cleared; recovery clears the telegraph. No sampled pose, weapon, feet, or Boss ground contact was occluded by the telegraph. This is static stage and continuous-frame timing evidence, not subjective real-time playback acceptance.

Fresh ignored evidence (not force-added):

- Main PNG sequence: `E:\OurGame\Knight\.worktrees\player-art-pipeline\screenshots\prologue\prologue-frames`, exactly 2280 PNGs, frame timestamps 2026-08-21 15:39:30–15:40:40 CST.
- Boss retry PNG sequence: `E:\OurGame\Knight\.worktrees\player-art-pipeline\screenshots\prologue\boss-retry-frames`, exactly 600 PNGs, frame timestamps 2026-08-21 15:40:43–15:41:01 CST.
- Main AVI: `E:\OurGame\Knight\.worktrees\player-art-pipeline\screenshots\prologue\prologue.avi`, 120,588,200 bytes, 2280 frames, 38 seconds, 1280×720 at 60 FPS.
- Boss retry AVI: `E:\OurGame\Knight\.worktrees\player-art-pipeline\screenshots\prologue\boss-retry.avi`, 34,588,896 bytes, 600 frames, 10 seconds, 1280×720 at 60 FPS.
- Contact sheets: `E:\OurGame\Knight\.worktrees\player-art-pipeline\screenshots\prologue\task4-fix1-contact-sheets`.
- Capture logs: `E:\OurGame\Knight\.worktrees\player-art-pipeline\capture-prologue.log`, `capture-boss-retry.log`, `task4-fix1-capture-frames.console.log`, and `task4-fix1-capture-video.console.log`.
- Capture result: both runs emitted `PROLOGUE_CAPTURE_OK`; main emitted `PROLOGUE_PRESENTATION_COMPLETE frame=2280`; retry emitted `BOSS_RETRY_PRESENTATION_COMPLETE frame=600`.

Capture artifacts cannot be reliably bound to the candidate commit hash because they must be produced before the commit exists. Artifact-to-commit fingerprint binding is explicitly deferred; the exact paths, sizes, frame counts, timestamps, markers, and key frames above provide the current handoff metadata.

The capture scripts reject missing nodes/resources, C# script errors, parse errors, and engine errors. None appeared. The generic telegraph audio/VFX fallback remains intentionally in place for the later environment/UI/audio plan.

## Production gate

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Fix Round 2 result on the commit candidate:

- Evidence checks: `PROLOGUE_EVIDENCE_CHECKS_OK`.
- .NET build: 0 warnings, 0 errors.
- All leaf-to-parent builders validated and all generated scenes were semantically equivalent to their pre-gate snapshots.
- Exactly one each: `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, `PROLOGUE_VERIFY_OK`.
- All seven `PROLOGUE_ATTACK_SEQUENCE_OK` markers emitted exactly once after identity validation.
- Smoke emitted the exact real-player-damage marker for 5 phase-one / 12 total hits.
- UI resolution, visual audit, production audit, and forced-failure teardown gates passed.
- The forced-failure Godot log and merged stdout/stderr console each contained their one expected `FORCED_SMOKE_FAILURE` error and no additional engine/script/parse error, ObjectDB leak, or resource-still-in-use line.
- Fresh full-gate log: `E:\OurGame\Knight\.worktrees\player-art-pipeline\task4-round2-final-verify.console.log`; result `PROLOGUE_VERIFY_OK`.

## Residual validation boundary

Automated structure, serialized balance, deterministic attack identity/timing, formal player-to-Boss hit routing, fresh capture contracts, and static/continuous-frame visual review are complete. A human still needs to play or watch the 60 FPS output at normal speed to accept subjective first-read clarity and first-session retry distribution.
