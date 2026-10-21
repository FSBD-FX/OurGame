# Attack Readability and Difficulty — Final Fix Report

Date: 2026-08-21

Base: `01472c584fd7f21495e302c658765226c5e2f83f`

Scope: source-aware damage identity, Grave-Lamp Moth dive geometry, Bell-Back Laborer sweep geometry, tests, and fresh presentation evidence

## Result and evidence boundary

All automated acceptance points in `final-fix-brief.md` and the five review rounds pass. Damage deduplication uses `(source runtime id, local attack id)` while the local ID still correlates the four presentation stages. The Moth now warns and executes one World-clamped, fixed-direction, no-slide path to the same first-contact endpoint. The Laborer still derives both collider and presentation from one authoritative convex fan. Fresh full-gate and capture output is clean under merged stdout/stderr leak scanning, and capture keeps its intended visible state through the exact final frame.

The fresh deterministic capture and direct frame review support geometry and cleanup claims. They do **not** prove that a first-time human reads either attack comfortably at normal speed; that qualitative hands-on question remains open.

## Root causes and RED evidence

1. Cross-source damage was deduplicated by bare `AttackId`. Different hitbox instances commonly begin at local ID `1`, so an accepted ordinary hit incorrectly suppressed the next source and Boss damage.
   - RED: `ordinary source B local AttackId 1 remains distinct after invulnerability`.
2. The Moth published only its instantaneous circle and published Active before `MoveAndSlide()`. The warning therefore omitted the locked path, and Active presentation described the prior position rather than the real post-movement collider.
   - RED: locked warning expected a capsule but received a circle.
   - Strengthened RED: expected locked landing `140`, actual `147` before endpoint/path correction.
3. The Laborer scene builder and runtime used a rectangle, so the visible sweep was not an authoritative convex fan; facing could also retarget during one attack.
   - RED: generated attack collider was not convex.
   - Runtime RED: expected `ConvexPolygonShape2D`, actual `RectangleShape2D`.

The RED logs are retained locally under this SDD directory and intentionally excluded from source control.

## Minimal production changes

### Source-aware damage identity

- Added `DamageAttackIdentity(SourceRuntimeId, LocalAttackId)` and exposed it from `DamageRequest`.
- `HealthComponent` queues and deduplicates this identity instead of a bare numeric attack ID.
- `Hitbox` supplies its stable Godot instance ID as the damage source while leaving its local attack counter unchanged for four-stage presentation correlation.
- No per-enemy numeric source allocation or prototype registry was introduced.

### Grave-Lamp Moth

- Locks both the dive start transform and a body-cast reachable landing endpoint when the attack begins.
- Builds Telegraph and locked-danger presentation from the real circular hitbox swept between those endpoints, producing a capsule whose bounds include the real radius.
- Moves only Dive Active with fixed-direction `MoveAndCollide()` and stops on first contact instead of sliding along a surface; all other movement retains `MoveAndSlide()`.
- Publishes Active after movement from the real current circle transform.
- Clears locked geometry outside the relevant phases and publishes a cleared frame on reset/death.

### Bell-Back Laborer

- Added one authoritative 120-degree, radius-80 convex fan with four arc segments.
- The builder writes that point set into the scene's real `ConvexPolygonShape2D`; runtime presentation reads the real collider through `AttackShapeFactory`.
- Locks attack facing once per attack and mirrors the same real collider for left/right attacks, including when the player crosses the Laborer mid-attack.
- Recovery and death clear the active/presentation state.

## Test coverage

### Persistent real damage route

`DamageSourceIdentityRuntimeTests` uses one persistent player `HealthComponent` and `Hurtbox` with actual packed Hitboxes for Rust-Shell, Moth, and Boss. It verifies:

- ordinary source A local ID `1` is accepted;
- a duplicate from source A is rejected after invulnerability expires;
- ordinary source B local ID `1` is independently accepted;
- ordinary-to-Boss local ID `1` is independently accepted;
- `ResetFull()` clears remembered identities and permits both ordinary sources again.

### Moth geometry and lifecycle

Runtime tests cover the real circular hitbox, start/target/radius capsule bounds, player crossover, stable locked endpoints, multiple real physics Active frames, post-movement collider/presentation equality, exact unobstructed landing, World-clamped obstructed landing, Recovery cleanup, and reset behavior.

### Laborer geometry and lifecycle

Builder, scene-structure, and runtime tests cover a convex fan with at least five points, point-for-point and world-bounds equality between the real collider and presentation, both facings, mirrored bounds, player crossover without retargeting, stable AttackId across stages, Recovery cleanup, and death cleanup.

### Mutation sensitivity

Each mutation was applied alone, observed failing by its named assertion, and restored before the full gate:

- removing the Hitbox source ID failed `ordinary source B local AttackId 1 remains distinct after invulnerability`;
- publishing Moth geometry before movement failed `every real-physics Moth Active frame is published after movement from the real collider transform`;
- shrinking Laborer presentation points to 75% failed `Bell-Back Laborer warning world bounds equal its real hitbox` (expected `80 × 138.564`, actual `60 × 103.923`).

## Verification

Fresh command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Result: exit `0`, `PROLOGUE_VERIFY_OK`.

The gate completed a zero-warning/zero-error .NET build, all leaf-to-root builders, semantic scene-drift checks, `PROLOGUE_TESTS_OK`, seven complete attack evidence sequences, the real-input critical path, five real player hits reducing Boss health from 12 to 7, and `PROLOGUE_CRITICAL_PATH_OK`.

## Fresh capture and visual review

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1
```

Both runs returned `PROLOGUE_CAPTURE_OK` using Godot 4.7.1 Mono with OpenGL 3.3 on NVIDIA hardware:

- main journey: exactly 2280 frames, 38 seconds, 1280×720 at 60 FPS;
- Boss death/retry: exactly 600 frames, 10 seconds, 1280×720 at 60 FPS.

Direct `view_image` review used the fresh `screenshots/prologue/prologue-frames` sequence:

| Actor | Exact contract frames | Dense review | Observation |
| --- | --- | --- | --- |
| Moth | 836 gold, 857 danger, 878 impact, 898 recovery | 875–880 | The locked red swept capsule stays fixed through frames 875–877 and ends inside the platform's near edge without crossing its far side. Frame 878 replaces it with the white current-circle Active shape, which follows the moving Moth on 879–880. The red path does not linger. Recovery is clear. |
| Laborer | 1174 gold, 1197 danger, 1219 impact, 1231 recovery | 1216–1221 | The locked red convex fan remains stable on 1216–1218. The same fan geometry changes to white Active on 1219–1221 without a second offset shape. Recovery is clear and the planted feet/hammer remain visible. |

These exact and consecutive frames were inspected individually, not inferred from marker logs or a contact sheet.

## Round 2 — production World-collision clamp

The first implementation's no-obstacle exact-landing test set `CollisionMask = 0`, and the synchronous presentation fixture did the same. That hid a production boundary: the packed Moth normally uses the World mask and a 16px body collider, so `MoveAndSlide()` may stop the dive before the nominal `DiveSpeed × AttackSeconds` endpoint.

### RED and root cause

A new real-physics fixture uses the formal packed Moth, its production World mask, and a real 20px `StaticBody2D` platform between the Moth and player. Before the fix it reported:

- locked endpoint `(0, 147)`;
- swept-warning far edge `y=169`, beyond the platform far side `y=110`;
- actual World-contact Recovery position `(0, 73.992195)`.

The named failure was `production-collision Moth warning clamps to its reachable World-contact endpoint`. Active had already proven its white circle equal to the post-`MoveAndSlide()` collider on every physics frame, so the isolated defect was the pre-attack endpoint calculation: it integrated nominal speed but never queried the real body/world collision.

### Engine calibration and minimal fix

A single full-distance cast with the same 147px motion returned `safe=0.5` and `unsafe=0.5039062`. The safe prediction was `y=73.5`, still `0.492195px` from the real stop because the long cast's safe/unsafe search interval spans roughly `0.574px`. The unsafe fraction was not used as a substitute for a safe endpoint.

At the end of Round 2, `ReachableAttackEndpoint` preflighted the actual `CharacterBody2D` through `TestMove` with its body RID, current collision mask, and `SafeMargin`. It advanced the query in `DiveSpeed / Engine.PhysicsTicksPerSecond` increments and accumulated safe travel until the first World collision. Round 3 keeps this engine-calibrated preflight but pairs it with fixed-direction, no-slide Active motion, described below. The locked start and accumulated endpoint are computed once; no visual ray, per-scene numeric source, or arbitrary distance subtraction is involved. The swept warning continues to use that clamped endpoint and the real 22px attack radius.

### Tests and mutation

- The unobstructed real-physics scene now preserves the packed production World mask and still lands exactly at the nominal endpoint.
- The obstructed scene proves warning bounds do not cross the platform's far side, player crossover cannot retarget the clamp, every Active frame equals the post-movement real attack circle, World contact enters Recovery, final collider and warning endpoint remain within `0.001px`, and all warning/damage layers clear. The measured values were `73.99219` and `73.992195`.
- The synchronous presentation fixture also preserves the production World mask; both prior `CollisionMask = 0` overrides are gone.
- Mutating the final endpoint back to nominal motion reproduced the named failure with `(locked=147, recovery=73.992195, warning_end=169, obstacle_far=110)`.

### Round 2 gate and capture

The fresh full `verify-prologue.ps1` gate returned exit `0`, `PROLOGUE_TESTS_OK`, all seven attack sequences, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`. The fresh PNG and AVI captures both returned `PROLOGUE_CAPTURE_OK`: 2280 frames/38 seconds plus 600 frames/10 seconds at 1280×720 and 60 FPS.

Direct `view_image` review covered Moth exact frames 836/857/878/898 and dense frames 875–880. The locked red capsule remains stable and stops at the reachable platform side; its 22px attack radius enters the platform's near edge by about 7px but never appears on the far side. Red clears at frame 878, the white current circle follows real movement on frames 879–880, and Recovery is clear at 898. The existing presentation fixture already exercises the real platform placement, so no synthetic collision bypass or visual-only reposition was added.

Normal-speed first-read quality remains a human playtest question.

## Round 3 — fixed-direction first contact and clean process teardown

### Phase 1 evidence and leak root cause

Two fresh full gates were saved with combined stdout/stderr before changing production movement. Gate 1 failed with `8 ObjectDB instances / 4 resources` after ordinary presentation and `4 / 2` after forced smoke; Gate 2 was clean, establishing an intermittent teardown failure rather than a deterministic Moth collision leak. Four verbose presentation repetitions produced clean, clean, `8 / 4`, and `13 / 6`. Every leaked object named by Godot was an `AudioStreamPlaybackWAV` or `AudioStreamWAV`; none was a `KinematicCollision2D`.

The engine reports ObjectDB/resource counts before its final tracked-instance disposal. Stopping or disposing audio wrappers, and an `AudioServer` lock barrier, each still leaked intermittently and were reverted. Round 3 introduced a full-scene `TestSceneTeardown.FreeAfterAudioDrain` contract: stop the real audio director, queue-free the owned scene, observe an actual subsequent `AudioServer` mix, then allow one final process frame. It contains no fixed sleep and no GC masking. A focused runtime audio probe that plays layered music, ambience, and five SFX passed 20/20 repetitions after this change. Round 4 separates this smoke-oriented full-scene helper from the presentation lifecycle because freeing the visible scene inside the recording window was not a valid capture contract.

The Moth preflight probe also validated the managed-wrapper lifecycle assumption around `KinematicCollision2D`. The production allocation site already used `using` from Round 2; Round 3 did not add that production disposal. Instead, a 12-packed-Moth probe measured synchronous `Performance.ObjectCount`: the existing `using` path measured `1727 → 1728` (`+1`), while the deliberate no-dispose mutation measured `+13` and failed `production Moth preflight disposes collision wrappers immediately`. This proves the existing allocation-site ownership is necessary and avoids attributing the intermittent audio leak to the Moth wrapper.

### Diagonal RED and no-slide architecture

A new real-physics test uses the packed Moth, production World mask, real body/attack shapes, and a 220×20 `StaticBody2D` platform rotated 65 degrees. The warning is locked before the player crosses the route, and every Active frame is compared with the real post-movement attack circle. Mutating Dive Active back to `MoveAndSlide()` reproduced the named RED:

- locked first-contact endpoint: `(0, 38.472656)`;
- Recovery position after surface slide: `(1.4092909, 41.342613)`;
- horizontal error: `1.409291px`;
- total endpoint error: `3.197304px`;
- allowed error: `0.001px`.

The root cause was architectural: `TestMove` warns the first collision along the locked line, while `MoveAndSlide` intentionally consumes tangential motion after that collision. Reimplementing multi-plane slide prediction would make the warning simulate a different movement algorithm. Dive Active now alone uses `MoveAndCollide(lockedVelocity × delta, SafeMargin)` and latches World contact; hover, chase, recovery, and other states retain `MoveAndSlide`. The same tick-sized `TestMove` preflight and real body shape establish the endpoint, so straight collision, diagonal collision, and unobstructed travel share one fixed-direction contract. On contact, no tangential remainder is consumed, the next model step enters Recovery, and the final body/hitbox position stays within `0.001px` of the locked endpoint.

Real-physics tests cover unobstructed, straight-obstructed, and 65-degree diagonal fixtures; production mask preservation; locked capsule bounds; player crossover; every post-move Active circle; first-contact Recovery; final endpoint equality; and phase cleanup. The MoveAndSlide mutation fails the diagonal assertion above.

### Gate hardening and mutations

`Invoke-MergedNativeCommand` captures ordinary Godot stdout and stderr into a persisted console log, converts PowerShell 5 native stderr records without aborting the run, returns the real exit code, and restores the caller's `ErrorActionPreference` in `finally`. `verify-prologue.ps1` and `capture-prologue.ps1` scan both Godot `--log-file` output and merged console output. Clean scanning rejects unexpected `ERROR`, `SCRIPT ERROR`, `Parse Error`, ObjectDB leaks, resources still in use, and RID allocations.

The PowerShell pressure test proves stdout/stderr persistence, exit-code preservation, and preference restoration. Appending `WARNING: 8 ObjectDB instances were leaked at exit (mutation)` to an ordinary presentation merged log produces a named rejection at that exact file and line. The forced-smoke expected error remains isolated behind its exact contract; it does not weaken ordinary-run scans.

After teardown added two recorded lifecycle frames, the first fresh PNG capture correctly failed `prologue capture expected 2280 PNG frames; found 2282`. Round 3 moved those cleanup frames inside the 2280/600 windows to restore the count, but that historical fix freed the visible scene before the actual final frame and produced gray tail frames. Round 4 replaces that coupling; exact frame-count assertions remain unchanged and no output is trimmed.

### Round 3 GREEN, capture, and evidence boundary

The fresh full gate returned exit `0` with a zero-warning/zero-error build, `PROLOGUE_TESTS_OK`, `PROLOGUE_AUDIO_TEARDOWN_OK`, Moth wrapper delta `+1`, all seven attack sequences, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`. Normal full-gate Godot logs and merged consoles contain no unexpected Error, Script/Parse, ObjectDB, resource, or RID cleanup line.

Fresh PNG and AVI capture commands both returned `PROLOGUE_CAPTURE_OK`:

- main journey: exactly 2280 frames / 38 seconds / 1280×720 / 60 FPS;
- Boss retry: exactly 600 frames / 10 seconds / 1280×720 / 60 FPS;
- all four capture Godot log/merged-console pairs passed the same clean scan.

Direct `view_image` review covered Moth exact frames 836/857/878/898, transition-dense frames 875–880, and contact-dense frames 892–898. In the production vertical route the locked red capsule remains fixed and stops at the platform's reachable near-contact region without extending to the far side; Active white circles descend at fixed X to the same contact, then clear in Recovery. The 65-degree collision proof is intentionally supplied by the real-physics test and its MoveAndSlide mutation, not represented as a screenshot from a different presentation fixture.

These deterministic results support geometry, collision, cleanup, and capture claims. They do not close the normal-speed first-time readability question.

## Round 4 — independent audio drain and visible final frame

### Architecture RED

The frozen Round 3 PNG evidence exposed the lifecycle defect directly. Main frames 2277–2279 and Boss frames 597–599 were uniform RGB `(76, 76, 76)`, while the preceding frames still contained the intended title and Boss views. Both drivers entered teardown two frames early but printed a hard-coded target marker. Replacing that marker with the actual `_frame` produced the named failures `actual=2278 expected=2280` and `actual=598 expected=600`.

A PowerShell capture contract now samples a 32×18 grid across each of the final three PNGs and rejects a uniform or near-uniform tail. The old fresh proof fails with `sample_colors=1, luminance_range=0.000`; synthetic visible fixtures pass. This check complements the exact directory count, freshness, dimensions, FPS, AVI-header, required-marker, and merged-log contracts.

### Minimal lifecycle correction

`TestSceneTeardown.DrainAudioOwner` now owns only audio shutdown and drain. It stops and queue-frees the formal `AudioDirector`, observes a subsequent real `AudioServer` mix with both a 60-frame and 2000ms bound, and verifies that the visible root remains in the tree. A 2ms scheduler yield lets the audio thread advance even when headless game frames execute faster than its mix interval; this is still condition-based, not a fixed completion sleep. Timeout or Task fault is surfaced as a clear error.

Both presentation drivers start this Task 90 logical frames before their target, keep the level/title/Boss nodes visible, and explicitly poll task completion without `async void`. If the audio task is still running at the penultimate logical frame, presentation holds there rather than publishing a false final marker; a capture hold would add real recorded frames and therefore fail the exact count. On successful drain, the driver advances to its real final frame, validates the visible state and removed audio owner, prints the marker from the actual `_frame`, and calls controlled `Quit(0)` without freeing the visible level. The pre-GREEN timeout experiment produced a clear nonzero presentation exit, proving the fault path is observed. Critical-path smoke retains the separately named full-scene cleanup helper.

### GREEN, repetition, and fresh capture

The active-audio teardown probe passed 8/8 repetitions with observed drain times of 11–80ms and no ObjectDB/resource/RID output. Four main and four Boss presentation repetitions then passed with exact dynamic markers; their drain observations ranged from 1–33 frames and 5–92ms. Every Godot log and merged console passed the ordinary clean scan.

Fresh FramesOnly capture returned exactly 2280 and 600 PNGs. The final three main frames each sampled 10 colors with a luminance range of `205.666` and retained the title menu; the final three Boss frames each sampled 23 colors with a range of `211.390` and retained the active encounter. Exact final markers were `PROLOGUE_PRESENTATION_COMPLETE frame=2280` and `BOSS_RETRY_PRESENTATION_COMPLETE frame=600`, paired with explicit `level_inside_tree=True` visible-state markers. Fresh AVI capture also passed at 1280×720/60 FPS with exactly 2280 and 600 frames. All four fresh capture log/merged-console pairs were clean.

Direct `view_image` review again covered Moth exact frames 836/857/878/898 and dense frames 876–879. The locked red capsule stops at the platform contact, Active follows the same vertical path, and Recovery clears it; the Round 4 lifecycle correction does not obscure or alter that evidence. Normal-speed first-read quality remains open for a fresh human playtest.

## Round 5 — bounded same-scene tail reference

### Independent RED fixtures and root cause

The Round 4 tail gate rejected uniform gray but only inspected each image in isolation. A deterministic high-entropy RGB tail and an unrelated high-contrast two-panel scene both passed that old gate because both contain many colors and broad luminance range. The focused PowerShell suite now names those two independent mutations `ROUND5_HIGH_ENTROPY_REFERENCE_MUTATION` and `ROUND5_WRONG_SCENE_REFERENCE_MUTATION`; before the reference contract, both were reported as not rejected.

The root cause was evidence scope rather than capture lifecycle: “nonuniform” says nothing about whether a tail frame belongs to the stable scene immediately preceding it. Presentation and audio teardown code therefore remain unchanged in Round 5.

### Transparent metric and fail-closed boundary

For each capture, `capture-prologue.ps1` explicitly selects final-4 as the stable same-scene reference and passes it alongside final-3 through final-1. `Assert-VisiblePngTail` first retains the nonuniform check, requires a present and decodable reference, and requires exact image dimensions. It then compares an evenly spaced 32×18 grid using two documented values:

- `mean_rgb_mae`: mean absolute per-channel RGB difference, on a 0–255 scale, limited to `12`;
- `changed_fraction`: fraction of grid samples whose mean RGB difference exceeds `24`, limited to `0.20`.

Both bounds must pass. They allow small localized animation or antialiasing changes but reject broad substitution and high-entropy output. This is bounded same-scene reference similarity, not visual recognition or semantic scene classification. The noise mutation measured `105.016 / 0.981`; the unrelated high-contrast scene measured `126.406 / 0.873`. A structured reference with a slightly moving indicator passes, while uniform gray, missing reference, corrupt reference, mismatched dimensions, noise, and the unrelated scene each fail closed through their intended branch.

### GREEN and fresh evidence

The focused PowerShell suite returns `PROLOGUE_EVIDENCE_CHECKS_OK`. Fresh `verify-prologue.ps1` returns `PROLOGUE_VERIFY_OK`, including the complete runtime tests, audio/Moth teardown probes, seven attack sequences, presentation, audits, and real critical path; ordinary Godot logs and merged consoles remain free of unexpected Error, ObjectDB, resource, and RID cleanup output.

Fresh FramesOnly capture returns exactly 2280 main and 600 Boss PNGs. The selected references are frames 2276 and 596; each of their final three comparisons measured `mean_rgb_mae=0` and `changed_fraction=0`. Direct `view_image` review of all six final frames confirms three complete title frames and three complete Boss-arena frames. Fresh AVI capture again returns exactly 2280/600 frames at 1280×720 and 60 FPS, and both capture modes return `PROLOGUE_CAPTURE_OK` with clean merged output.

## Remaining work

- Run a fresh first-time human playtest at normal speed and record whether the Moth path/landing and Laborer fan/recovery read soon enough without prior knowledge.
- Continue the already planned motion, AC-corruption, UI/audio, and overall first-level sensory passes. None is claimed complete by this fix.

## Commit boundary

Only the source, tests, generated Laborer scene, README truth update, and this report belonging to the fix are included. Existing import metadata, room/art work, UID files, screenshots, and unrelated dirty changes are deliberately left unstaged.
