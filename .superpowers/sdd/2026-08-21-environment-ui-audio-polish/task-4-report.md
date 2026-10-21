# Task 4 report — staged combat sound language and typed VFX balance

Status: implementation complete, pending independent review

Baseline: `e79973c` (Task 3 approved state visible to the Task 4 implementer)

## Scope and result

- Added the required actor/Boss telegraph and mechanical-thread cues to the typed enum and manifest. `PlayAttackFrame` consumes immutable `AttackPresentationFrame` values and deduplicates by `(AttackId, Phase)`; `PlayThreadFrame` consumes `ThreadCastFrame` and deduplicates by `(CastId, State)`.
- Runtime bindings listen to the real Rust Shell, Grave-Lamp Moth, Bell-Back Laborer, Fallen Bellkeeper, and `ThreadAbility` typed events. Legacy compatibility cues are suppressed only when they repeat the typed attack entry, so they cannot overwrite an actor-specific cue.
- Added 12 locally synthesized fixed-seed WAV masters. All 56 prologue masters are audited as mono 16-bit, 48 kHz, finite/non-empty, with duration, family, peak, exact SHA-256, and two-render reproducibility metadata.
- Added typed procedural VFX driven directly by the authoritative frames and `AttackShapeFrame`: gold countdown outlines for Telegraph, red hatching only for Active danger, white impact crosses for Recovery, and cyan shape/motion channels for Thread states. Reduced-flash lowers effect alpha and leaves full-screen flash at zero.
- Cleanup covers room change, death/recovery, pause, cancellation/invalid target, and source removal. Deferred VFX cleanup retains the parent until `QueueFree` completes and count queries ignore queued effects, preventing the reproduced Mono GCHandle teardown race.
- Corrected the Task 4-owned visual audit's stale Moth Corruption pivot oracle from `(-9,-3)` to the authoritative builder/generated-scene value `(-20,-3)`. No Task 2 builder, scene, atlas, or motion source changed. Longer term, the rig pivot contract should be shared rather than duplicated in the audit.
- No paid generation, external API, third-party download, gameplay authority, tuning, collision, traversal, Task 1–3 presentation, or `default_bus_layout.tres` change was made.

## Strict TDD evidence

- Enum/mapping RED: `task-4-red-enum-manifest.log` names the missing `RustShellTelegraph` enum member.
- Manifest RED: `task-4-red-manifest.log` names the missing `RustShellTelegraph` manifest entry.
- Metadata RED: `task-4-red-file-metadata.log` requires per-file deterministic metadata.
- UI-frequency RED: `task-4-red-ui-frequency.log` requires analyzed dominant-frequency metadata and UI/danger separation.
- Typed API/dedupe RED: `task-4-red-typed-dedupe-clean.log` contains only the genuine missing `PlayAttackFrame` / `PlayThreadFrame` contracts after the unrelated target-typed `new` test syntax was corrected.
- Typed VFX RED: `task-4-red-typed-vfx.log` names the missing typed VFX contract.
- Real binding RED: `task-4-red-runtime-binding.log` proves a real Rust Shell `AttackFrameChanged` did not yet drive the typed layer.
- Binding diagnostic: `task-4-binding-diagnostic.log` showed the typed Rust cue was emitted, then the legacy `AttackTelegraph` fallback replaced it with `PlayerHit`. The narrow compatibility suppression fixes that exact overwrite.
- Focused restored GREEN after all mutations: `task-4-post-mutation-focused-1.log` and `task-4-post-mutation-focused-2.log`; both exit 0 with one `TASK4_AUDIO_VFX_TESTS_OK` and zero error/leak markers.

## Required mutations

- Missing manifest cue: removed `RustShellTelegraph`; `task-4-mutation-manifest-missing.log` exits 1 and names `Audio manifest missing required cue 'RustShellTelegraph'.`
- Duplicate phase playback: disabled attack-phase dedupe; `task-4-mutation-duplicate-phase.log` exits 1 and names `repeated AttackId and Phase play exactly one staged cue; expected=1, actual=2`.
- Red outside Active: changed Telegraph from gold to red; `task-4-mutation-red-outside-active.log` exits 1 and names `Telegraph uses gold readiness rather than danger red`.
- Every mutation was restored with a scoped patch. No mutated state remains.

## Audio inspection

- `task-4-audio-wav-inspection.log` records cue, exact resource path, 48 kHz rate, duration, semantic family, dominant frequency where applicable, peak dBFS, and complete SHA-256 for every new master.
- Required cues range from 0.16 s to 0.70 s. New cue peaks range from -8.303 dBFS to -3.552 dBFS, safely below the -0.9 dBFS audit ceiling.
- Danger warnings occupy analyzed 40–190 Hz dominant frequencies. UI Move/Confirm/Back occupy 660/880/460 Hz, leaving at least 270 Hz of dominant-frequency separation.
- `task-4-final-production-audit-post-gchandle.log` reports `PROLOGUE_AUDIO_AUDIT files=56 rate=48000 peak_dbfs=-2.3277 deterministic=TRUE`; regeneration twice produces identical manifest and WAV hashes.

## Running visual proof

- Proof root: `screenshots/task4-audio-vfx/` (14 PNGs, each actual 1280×720 viewport output at fixed 60 FPS).
- The proof is driven by real typed runtime `AttackPresentationFrame` / `ThreadCastFrame` inputs through production `PrologueVfxDirector` and true `AttackShapeFrame` geometry. It is not a static helper drawing.
- Reviewed ordinary enemy `ordinary-telegraph.png`, `ordinary-active.png`, and `ordinary-recovery.png`: gold/ticks, red/hatching, and white/cross remain distinct.
- Reviewed `boss-sweep-telegraph.png`, `boss-leap-telegraph.png`, `boss-ground-wave-telegraph.png`, and `boss-hammer-telegraph.png`: convex sweep, circle, ground band, and capsule remain separately legible and isolated.
- Reviewed `thread-preview.png`, `thread-windup.png`, `thread-travel.png`, `thread-latch.png`, and `thread-tension.png`: dashed candidate, double spool rings, moving shuttle, solid latch diamond, and three-strand tension are distinguishable without relying on color.
- Reviewed `reduced-flash-active.png` and `cleanup-after-cancel.png`: reduced-flash preserves the danger shape without a full-screen flash; cancel cleanup leaves no typed attack/thread residual.

## Lifecycle correction

- The first attempted second full pass, `task-4-final-full-2.log`, correctly invalidated the gate after an intermittent `Handle is not initialized` despite exit 0.
- Root cause: Task 4 cleanup called `RemoveChild` and then `QueueFree`, leaving a managed VFX wrapper detached while native deferred free/scene teardown progressed.
- Minimal fix: keep the node parented through deferred free and exclude queued effects from immediate public counts. `PrologueVfxRuntimeTests` now asserts both immediate zero visible count and parent retention until deferred release.
- Focused lifecycle verification `task-4-gchandle-fix-focused-1.log` through `-3.log` passes three consecutive runs with zero ERROR/FATAL/leak markers.

## Final gates

- Production audit: `task-4-final-production-audit-post-gchandle.log` — build 0 warnings/0 errors; audio determinism, five resolution markers, `TASK4_AUDIO_VFX_TESTS_OK`, performance, visual audit, and `PROLOGUE_PRODUCTION_AUDIT_OK`; zero ERROR/FATAL/GCHandle/leak markers.
- Consecutive authoritative full pass 1: `task-4-authoritative-final-full-1.log` with build `task-4-authoritative-final-full-1-build.log` — exit 0, exactly one `PROLOGUE_TESTS_OK`, zero ERROR/FATAL/GCHandle/leak markers, build 0/0.
- Consecutive authoritative full pass 2: `task-4-authoritative-final-full-2.log` with build `task-4-authoritative-final-full-2-build.log` — same clean result.
- Both runs contain the same five intentional warning-path probes already documented by prior tasks; no new or unexpected warning was introduced.

## Owned implementation and tests

- `scripts/audio/PrologueAudioCue.cs`
- `scripts/audio/PrologueAudioDirector.cs`
- `tools/audio/render_knight_audio.py`
- `assets/audio/prologue/audio-manifest.json`
- 12 new `.wav` files listed in the Task 4 plan
- `scripts/vfx/PrologueVfxDirector.cs`
- `tests/unit/PrologueAudioManifestTests.cs`
- `tests/integration/PrologueAudioDirectorTests.cs`
- `tests/integration/PrologueVfxRuntimeTests.cs`
- `tests/presentation/PrologueVisualAudit.cs`
- `tests/RunTask4AudioVfxTests.cs`
- `tools/audit-prologue-production.ps1`
- this report and the Task 4 progress append

All unrelated `.import` files, screenshots, generated scenes, `default_bus_layout.tres`, Task 1–3 files, art-review files, and other dirty paths remain unstaged.

## Fix Round 1/5 — source-isolated staged presentation

Status: implementation complete, pending independent re-review

Base: `2e9d7882fb2a3cb60d17cb56e740d66a3de71d6b`

### Rejected findings corrected

- Attack audio and VFX identity is now `(live SourceInstanceId, source-local AttackId, Phase)`. Two actors may reuse the same local attack id without suppressing each other; removing or completing one source releases only that source's state.
- Legacy compatibility suppression is a typed one-shot token carrying the live source, source-local attack id, phase, and expected cue. It is consumed by the immediately corresponding legacy cue and cleared by any unrelated legacy request, so a stale typed event cannot suppress later player or enemy audio.
- Rectangle, GroundBand, Capsule, and ConvexPolygon outlines now apply the authoritative `AttackShapeFrame.RotationRadians` through the effect's world transform. The focused contract checks actual `AttackOutline` global points rather than reusing the production transform helper as an oracle.
- Completed and queued-free attack effects are pruned from source-qualified dictionaries during ordinary runtime expiry. Duplicate audit output reports `Duplicate` instead of leaking an unrelated prior resource path.
- Added deterministic local fixed-seed recovery masters and typed mappings for Rust Shell, Grave-Lamp Moth, Bell-Back Laborer, and Fallen Bellkeeper. The mapping matrix covers all seven attack kinds across Telegraph, Active, and Recovery.
- No paid generator, remote API, third-party download, gameplay timing, damage, collision, traversal, UI tuning, production camera, Task 1–3 content, or default bus layout was changed.

### RED / GREEN contract evidence

The first focused run named these independent failures before production changes:

- `CrossSourceAudioIdentity`: same local id from two live sources collapsed to one playback (`expected=2, actual=1`).
- `SourceIsolatedVfxRemovalAndReuse`: removing one actor also removed/repressed the other actor's reused local id.
- `SourceQualifiedOneShotCompatibility`: an unrelated legacy player cue was suppressed by the previous typed event.
- `RotatedAuthoritativeGeometry`: a rotated rectangle expected global point `(207.72984,125.89984)` but rendered `(180,169)`; the same focused case covers rotated rectangle, capsule, and convex polygon.
- `CompleteRecoveryMapping`: `RustShellRecovery` and the remaining actor/Boss recovery contracts were absent.
- `NaturalExpiryPrunesBookkeeping`: 64 naturally expired effects remained in presentation bookkeeping.
- `ValidateWarningMix`: warning-family RMS spread was 10.42 dB (minimum -20.05, maximum -9.63), above the 5 dB contract.
- `ValidateGroundWaveOnset`: Ground Wave recognizable onset was 140 ms, above the 100 ms contract.
- `RealPublisherProofRouteContainsNoSyntheticFrames`: the old capture path contained `ProofAttack` / `ProofThread` and direct director calls.

Restored focused GREEN is recorded by `task-4-fix1-focused-all.log` and `task-4-fix1-task4-audio-vfx.log`; each finishes with its expected success marker and no runtime error/leak marker.

### Required non-synonymous mutations

- Cross-source identity mutation removed source qualification from audio dedupe. `task-4-fix1-mutation-cross-source.log` failed `cross-source same local AttackId and phase play once per live source; expected=2, actual=1`; `task-4-fix1-green-cross-source-restored.log` is clean after restoration.
- Rotation mutation replaced the production effect transform rotation with zero. `task-4-fix1-mutation-rotation.log` failed the rectangle global-point assertion (`expected=(207.72984,125.89984), actual=(180,169)`); `task-4-fix1-green-rotation-restored.log` is clean after restoration.
- Compatibility mutation retained a nonmatching token. `task-4-fix1-mutation-stale-token.log` failed `stale non-adjacent compatibility token suppresses nothing; expected=PlayerAttack, actual=PlayerHurt`; `task-4-fix1-green-stale-token-restored.log` is clean after restoration.
- Mutation processes intentionally terminate on assertion and therefore report teardown residue after the named failure. Restored focused, production-audit, and full-suite processes have zero `ERROR` / `FATAL` / GCHandle / leak markers; no mutated state remains.

### Audio calibration and deterministic render

- The manifest now covers 60 mono 16-bit 48 kHz masters with complete SHA-256, family, duration, analyzed RMS/onset, and peak metadata. Four recovery WAVs were produced locally with the fixed-seed renderer.
- Warning RMS spread improved from 10.42 dB to 4.6177 dB: Ground Wave -10.1237, Hammer -13.5818, Leap -9.6312, Sweep -10.1448, Laborer -14.2489, Moth -12.6099, and Rust Shell -11.7660 dBFS.
- Recognizable onset is now 0–50 ms across the warning family; Ground Wave moved from 140 ms to 50 ms. Warning peaks remain below the project ceiling; the whole manifest audit reports `peak_dbfs=-1.1659`.
- The calibrated warnings preserve distinct material signatures while reaching the warning band promptly: short shell-metal thrust, moth buzz/dive, laborer scrape, and four Boss patterns remain separable by envelope and timbre rather than loudness alone.
- Two clean local renders (`task-4-fix1-render-1.log`, `task-4-fix1-render-2.log`) produce `TASK4_RENDER_HASHES_IDENTICAL count=60` in `task-4-fix1-render-hash-compare.log`.

### Honest running proof

- `screenshots/task4-audio-vfx/` contains 23 actual 1280×720, fixed-60-FPS runtime captures: Rust/Moth/Laborer Telegraph+Active+Recovery (9), three ordinary aliases, four Boss shapes, five thread states, reduced-flash Active, and cancel cleanup.
- Each ordinary set is one real actor's same source-local `AttackId` across three real `AttackFrameChanged` phases. The log identifies source type/instance, attack kind/id/phase, authoritative shape, and rotation.
- Boss Sweep, Leap, Ground Wave, and Hammer captures come from the real Fallen Bellkeeper publisher. Sweep convex polygon, Leap circle, Ground Wave band, and rotated Hammer capsule are separately visible with actor and player in frame.
- Thread Preview/Windup/Travel/Latch/Tension come from real player `Input.ActionPress` driving `ThreadAbility.CastFrameChanged` against a live `ThreadTarget2D`; assertions bind the visible line/shuttle endpoint to that exact target instance.
- Capture waits for draw/process frames after each real event and asserts typed VFX screen bounds align to the publisher's global shape. Reduced-flash is a second real Rust Active. Cancellation is real pause/input state transition and ends with zero attack/thread visual residue.
- The proof fixture only widens its own audit camera/positions to keep actors and shapes readable; no production camera or gameplay geometry changed. The screenshots are untracked review artifacts and are deliberately excluded from the commit.

### Pause failure investigation

- The independent-review failure `pause InputMap action pauses the real SceneTree` did not reproduce in three isolated full-suite processes: `task-4-fix1-full-1.log`, `task-4-fix1-full-2.log`, and `task-4-fix1-pause-repro-3.log` each exit 0 with exactly one `PROLOGUE_TESTS_OK`, no pause assertion, and zero runtime error/fatal/GCHandle/leak markers.
- Because every fresh process reconstructs the InputMap and passes while the reported failure occurred only in a prior combined reviewer process, the controlled evidence points to intermittent shared test-harness InputMap state rather than Task 4 presentation lifecycle. No out-of-scope input/pause production code was changed.

### Final gates

- Production audit `task-4-fix1-production-audit.log`: build 0 warnings/0 errors; `PROLOGUE_AUDIO_AUDIT files=60 rate=48000 peak_dbfs=-1.1659 deterministic=TRUE`; five supported resolutions; `TASK4_AUDIO_VFX_TESTS_OK`; runtime visual audit; `PROLOGUE_PRODUCTION_AUDIT_OK`; zero error/fatal/GCHandle/leak markers.
- Consecutive authoritative pass 1: `task-4-fix1-full-1-build.log` and `task-4-fix1-full-1.log` — build 0/0, exactly one `PROLOGUE_TESTS_OK`, zero error/fatal/GCHandle/leak markers.
- Consecutive authoritative pass 2: `task-4-fix1-full-2-build.log` and `task-4-fix1-full-2.log` — identical clean result.
- The third isolated pause investigation full run is also clean. Existing intentional warning-path probes remain warnings only.

### Fix Round owned additions

- `assets/audio/prologue/sfx/enemies/{rust,moth,laborer}-recovery.wav`
- `assets/audio/prologue/sfx/boss/recovery.wav`
- `tests/RunTask4FixRound1Tests.cs`
- `tests/integration/Task4FixRound1ContractTests.cs`
- Fix Round edits to the existing Task 4 audio/VFX renderer, manifest, focused tests, visual audit, this report, and progress ledger.

All `.import` / `.uid`, screenshots, scenes, `default_bus_layout.tres`, art-review files, and unrelated dirty paths remain unstaged.

## Fix Round 2/5 — live geometry refresh and deterministic pause edge

Status: implementation complete, pending independent re-review

Base: `eb31489`

### Same-phase authoritative geometry

- Root cause: `(source instance, local AttackId, Phase)` duplicate handling returned before touching the existing effect. This correctly suppressed one-shot feedback but froze the first geometry published in a phase.
- Named RED `task-4-fix2-red-moth-refresh.log` uses a real Grave-Lamp Moth publisher. One Telegraph retains the same source/local id while advancing from its initial Circle to a target-locked Capsule; the existing effect incorrectly remained Circle.
- Repeated frames now call `RefreshAttack` on the existing effect. It stores the complete validated `AttackPresentationFrame`, updates `AttackShapeFrame` and world rotation/origin, queues redraw, and preserves the same node instance and elapsed lifetime. Lifetime is only extended to the latest authoritative remaining time; it is never reset.
- Creation count, audio one-shot dedupe, camera feedback, color/channel selection, and active/recovery phase transitions are unchanged. The real-publisher test proves count remains one, node reference remains identical, and every rendered global Capsule point equals the latest authoritative swept path.
- Mutation `task-4-fix2-mutation-frozen-moth.log` restores the old early return and fails the named Circle-versus-Capsule assertion. Restored GREEN is in `task-4-fix2-post-mutation-focused.log`.

### Deterministic pause InputMap fixture

- Root cause trace: synthetic `ActionRelease(Pause)` can leave the prior `just-pressed` edge observable by the next real production `_Process`. The first stress attempt recorded `pressed=False tree=True menu=True` after only release plus two process frames, proving the old edge opened the real menu during settling.
- The fixture now releases Pause, advances two real process frames, closes any menu opened by that stale edge, unpauses, advances two more process frames, then asserts `!Input.IsActionPressed(Pause)`, `!tree.Paused`, and closed menu before issuing the new real `ActionPress`.
- The production `ProloguePauseMenu._Process` still receives the real InputMap just-pressed edge; no direct `Open()` replacement or assertion weakening was introduced.
- The entire fixture is enclosed by `try/finally`. Cleanup always releases Pause, closes an open menu through its production Resume command, restores `tree.Paused=false`, disposes every loaded level, and settles the process loop even after assertion failure.
- Named RED `task-4-fix2-red-pause-contract.log` identifies the missing pre-release. Mutation `task-4-fix2-mutation-pause-no-settle.log` omits settling and deterministically fails `pause baseline action is released before stress iteration 0`; restored 20-iteration GREEN is `task-4-fix2-post-mutation-pause-20.log`.
- Five additional fresh processes (`task-4-fix2-pause-fresh-1.log` through `-5.log`) each deliver the real edge and finish with one `TASK4_PAUSE_RUNNER_OK`, zero error/fatal/GCHandle/leak markers.

### Refreshed proof

- `ordinary-moth-telegraph.png` was regenerated from the real Moth `AttackFrameChanged` target-locked Telegraph rather than its initial frame. Runtime marker: `kind=MothDive attack_id=1 phase=Telegraph shape=Capsule rotation=0.983`.
- The capture waits for `HasLockedTarget` and Capsule, then compares all rendered global outline points with the publisher's authoritative swept-path frame before saving. `Moth locked swept proof rendered outline equals authoritative frame` and `PROLOGUE_VISUAL_AUDIT_OK` are recorded in `task-4-fix2-visual-proof.log`.
- Visual inspection confirms the diagonal gold swept-path outline is visible with the real Moth and player. Proof-only camera/position handling remains unchanged; no production camera or visual tuning changed.

### Final gates

- `task-4-fix2-production-audit.log`: audio audit, five resolutions, Task 4 focused suite, refreshed runtime visual audit, performance, and `PROLOGUE_PRODUCTION_AUDIT_OK`; zero error/fatal/GCHandle/leak markers.
- Consecutive full pass 1: `task-4-fix2-full-1-build.log` is 0 warnings/0 errors; `task-4-fix2-full-1.log` has exactly one `PROLOGUE_TESTS_OK` and zero error/fatal/GCHandle/leak markers.
- Consecutive full pass 2: `task-4-fix2-full-2-build.log` and `task-4-fix2-full-2.log` have the same clean result.
- No gameplay, timing, damage, collision, traversal, UI visual, audio asset, paid generation, scene, import, default bus, Task 1–3, or unrelated dirty file was changed.

### Fix Round 2 owned files

- `scripts/vfx/PrologueVfxDirector.cs`
- `tests/integration/PrologueCompletionTests.cs`
- `tests/presentation/PrologueVisualAudit.cs`
- `tests/integration/Task4FixRound2ContractTests.cs`
- `tests/RunTask4FixRound2Tests.cs`
- `tests/RunTask4PauseStressTests.cs`
- this report and the Task 4 progress append

All screenshots, `.import` / `.uid`, scenes, `default_bus_layout.tres`, art-review files, and unrelated dirty paths remain unstaged.

## Fix Round 3/5 — deterministic engine-delivered pause edge

Status: implementation complete, pending independent re-review

Base: `cdc80cb`

### Root cause and named RED

- The review-only failure was isolated to the pause test harness. A 200-iteration local run of the prior `Input.ActionPress` fixture happened to pass (`task-4-fix3-red-current-actionpress-200.log`), confirming the edge was schedule-dependent rather than a deterministic production-menu failure. `Input.ActionPress` mutates action state but does not enqueue and explicitly flush an engine input event for the real `ProloguePauseMenu._Process` consumer.
- Named RED `task-4-fix3-red-pause-event-delivery.log` requires the fixture to publish an `InputEventAction` through `Input.ParseInputEvent`, flush buffered events, and assert the registered InputMap edge before production `_Process` consumes it. The pre-fix fixture failed that contract by only changing action state.

### Minimal harness repair

- `DeliverPauseInput` now constructs the real Pause `InputEventAction`, sends it through `Input.ParseInputEvent`, and calls `Input.FlushBufferedEvents`. Immediately after the press, the fixture asserts both `Input.IsActionPressed(Pause)` and `Input.IsActionJustPressed(Pause)` before advancing real process frames.
- The production `ProloguePauseMenu._Process` remains the only consumer that opens the menu. No direct production-method shortcut, production pause change, assertion weakening, gameplay/tuning change, scene edit, or VFX change was introduced.
- The Fix Round 2 two-stage clean baseline and `try/finally` cleanup remain intact. Release events use the same engine-delivery path, every fixture resumes/unpauses/disposes, and each stress baseline records `pressed=False tree=False menu=False`.

### Mutation and stress evidence

- Non-synonymous mutation `task-4-fix3-mutation-no-flush.log` removes only `Input.FlushBufferedEvents` while leaving the real `InputEventAction` and `Input.ParseInputEvent` intact. It deterministically records `seeded_pressed=False` and fails the named `pause InputMap edge is registered before production _Process iteration 0` assertion. The flush was restored before all final gates.
- Restored focused contract and full Task 4 Fix Round 2 runner are clean in `task-4-fix3-green-pause-delivery-contract.log` and `task-4-fix3-focused-all.log`; the same-process 20-iteration stress is clean in `task-4-fix3-restored-pause-20.log`.
- Five final fresh processes (`task-4-fix3-pause-final-fresh-1.log` through `-5.log`) each execute 20 iterations, contain exactly one `TASK4_PAUSE_STRESS_OK` and one `TASK4_PAUSE_RUNNER_OK`, contain 20 clean baseline markers, and contain zero error/fatal/GCHandle/leak markers. An earlier cold-start evidence batch had one exit-time resource warning in its first process, so it was excluded and the complete five-process gate was rerun from scratch rather than counted selectively.

### Final gates

- `task-4-fix3-production-audit.log`: build 0 warnings/0 errors; 60-file deterministic 48 kHz audio audit; five supported resolutions; `TASK4_AUDIO_VFX_TESTS_OK`; runtime visual/performance audit; exactly one `PROLOGUE_PRODUCTION_AUDIT_OK`; zero error/fatal/GCHandle/leak markers.
- Consecutive full pass 1: `task-4-fix3-full-1-build.log` is 0 warnings/0 errors; `task-4-fix3-full-1.log` has exactly one `PROLOGUE_TESTS_OK` and zero error/fatal/GCHandle/leak markers.
- Consecutive full pass 2: `task-4-fix3-full-2-build.log` and `task-4-fix3-full-2.log` have the same clean result.
- Fix Round 3 owns only `tests/integration/PrologueCompletionTests.cs`, `tests/integration/Task4FixRound2ContractTests.cs`, this report, and the Task 4 progress append. All production pause/VFX/audio code, scenes, screenshots, `.import` / `.uid`, `default_bus_layout.tres`, Task 1–3 files, and unrelated dirty work remain untouched and unstaged.

## Fix Round 4/5 — explicit playable flow on the display-backed path

Status: implementation complete, pending independent re-review

Base: `a3bf9fd`

### Display-backed root cause and RED

- The exact review path was reproduced before changing code with the Windows display driver in `task-4-fix4-red-display-backed.log`: the real queued/flushed Pause edge was registered, but production `_Process` left the menu closed and the process exited 1.
- Production behavior is correct: `ProloguePauseMenu._Process` accepts Pause only while `GameFlow` is `Playing` or `Paused`. The fixture loaded a real level without explicitly starting it. Headless `HeadlessAutoStart` masked that missing precondition; display-backed initialization remained `Title`.
- Named runtime RED `task-4-fix4-red-playing-precondition.log` adds the pre-input contract and fails before Pause delivery with `expected=Playing, actual=Title`.

### Real public start path

- `LoadLevel` now disables environment-dependent `HeadlessAutoStart` before entering the tree, waits for normal initialization, invokes the existing public `PrologueGameFlowController.StartNew()`, and asserts `Playing`. Both Pause entry points independently assert `Playing` before publishing input.
- `InputEventAction -> Input.ParseInputEvent -> Input.FlushBufferedEvents`, the pre-process just-pressed assertion, real `ProloguePauseMenu._Process`, two-stage baseline, and `try/finally` cleanup are unchanged.
- Non-synonymous display mutation `task-4-fix4-mutation-omit-start-display.log` removes only `StartNew()` and fails `PauseStress00 enters Playing through the public game-flow start path; expected=Playing, actual=Title`.

### Display-backed full-suite harness

- The first display-backed full run then exposed a separate test-runner assumption: fixtures that intentionally kept default `HeadlessAutoStart=true` remained at Title and `PrologueFormalHudTests` failed `Preview; actual=Idle`. No production defect was inferred and no production file was changed.
- The authorized test-only `DisplayBackedFlowAutoStartHarness` connects to `SceneTree.NodeAdded` before the full suite. On a non-headless display, a newly entering real `PrologueGameFlowController` with `HeadlessAutoStart==true` receives its existing public `AutomationAutoStart=true` before `_Ready`. Explicit `HeadlessAutoStart=false` fixtures are untouched. The adapter never calls `StartNew`, and disconnects in the runner's `finally` block.
- Behavioral RED `task-4-fix4-red-display-adapter.log` records `automatic=Title explicit=Title` and fails only the dependent fixture's expected `Playing` state. GREEN records `TASK4_DISPLAY_FLOW_FIXTURE automatic=Playing explicit=Title`.
- Adapter mutation `task-4-fix4-mutation-disable-display-adapter.log` changes only the automation value to false; it again records `automatic=Title explicit=Title` and fails the named dependent-fixture assertion, proving explicit-false Title semantics remain preserved.

### Lifecycle follow-up

- Exact final-log scanning caught an intermittent runtime RED in `task-4-fix4-final-pause-display-fresh-3.log`: functional markers passed but exit reported four leaked ObjectDB instances and two live resources. The repeated fixture was creating temporary wrappers for the same cached `PackedScene` and freeing an active real Audio owner with only one process frame.
- The fixture now retains one `PackedScene` wrapper and uses the repository's existing `TestSceneTeardown.FreeSceneAfterAudioDrain` two-frame teardown. A runtime assertion verifies the level instance is invalid after drain; no forced GC or production lifecycle branch was added.
- Post-fix `task-4-fix4-lifecycle-pause-display-20.log` has 20 clean baselines, 20 audio-drain markers, and one runner marker. Ten independent verbose processes (`task-4-fix4-lifecycle-verbose-1.log` through `-10.log`) and five final ordinary processes (`task-4-fix4-lifecycle-final-fresh-1.log` through `-5.log`) each execute 20 iterations with zero error/fatal/GCHandle/leak markers; every ordinary process records 20 audio drains.

### Final display-backed gates

- All display-backed commands use the Windows driver, compatibility renderer, Dummy audio, and fixed 60 FPS.
- `task-4-fix4-lifecycle-build.log`: 0 warnings / 0 errors. `task-4-fix4-lifecycle-focused-display.log`: exactly one focused success marker, `automatic=Playing explicit=Title`, and zero error/fatal/GCHandle/leak markers.
- `task-4-fix4-lifecycle-final-production-audit.log`: deterministic 60-file 48 kHz audio audit, five resolutions, Task 4 audio/VFX, runtime visual/performance audit, exactly one `PROLOGUE_PRODUCTION_AUDIT_OK`, and zero bad markers.
- Consecutive display-backed full pass 1: `task-4-fix4-lifecycle-final-full-display-1-build.log` is 0/0; `task-4-fix4-lifecycle-final-full-display-1.log` has exactly one `PROLOGUE_TESTS_OK` and zero bad markers.
- Consecutive display-backed full pass 2: `task-4-fix4-lifecycle-final-full-display-2-build.log` and `task-4-fix4-lifecycle-final-full-display-2.log` have the same clean result.
- Fix Round 4 owns only `tests/integration/PrologueCompletionTests.cs`, `tests/integration/Task4FixRound2ContractTests.cs`, `tests/RunPrologueTests.cs`, this report, and the progress append. No production, gameplay, tuning, scene, VFX, asset, import, UID, screenshot, default bus, Task 1–3, or unrelated dirty file changed.
