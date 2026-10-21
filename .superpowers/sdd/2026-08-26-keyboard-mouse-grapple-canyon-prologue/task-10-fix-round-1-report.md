# Task 10 Fix Round 1 report

Date: 2026-08-27

Status: all Fix Round 1 findings are implemented and self-verified. Task 10 remains open for independent re-review and is not marked complete.

## Boundary and immutable history

- Reviewed starting point: `f961598` (`docs: start task 10 fix round 1`).
- Controller rulings before implementation: `5975bf0` (shield/locale/containment scope) and `f86b79a` (production guidance expansion).
- Fix implementation and tests: `4cb1b645ead2b2eaade18a549b5ceed5bdb68377` (`fix: wire task 10 production feedback`).
- Controller ruling interleaved after the first fresh post RED: `15917168a5df29065729609137373b9f25519569` (`docs: migrate task 10 guidance runtime proof`).
- Exact approved regression migration: `e55150756ed7a3c01172b07346c41a01568fc45b` (`test: drive guidance expansion through recovery`).
- Aggregate Fix Round 1 implementation range before this report: `f961598..e551507`.
- Original Task 10 history, including immutable mixed generated-output commit `b8b2669`, remains unchanged.
- No paid generation, formal art, formal audio, `.import` sidecar, README change, or unrelated artifact was included.

## Review findings closed

### Production audio and VFX wiring

`PrologueAudioDirector` now binds the generated continuous main's real publishers for action mode/loadout/rejection, energy/heat, shield results, and mechanical-claw state. `PrologueVfxDirector` consumes the typed Task 10 cue transition and the real claw publisher. Both directors detach the exact prior publishers before refresh/re-entry and detach on tree exit.

Real InputMap, collision, energy, and material/lifecycle transitions reach the exact 16 cue set:

```text
ModeAttack ModeGrapple EmptySlot EnergyEmpty
ShieldRaise ShieldBlock ShieldHeavyBreak ShieldOverheat ShieldReady
ClawFire ClawTravel ClawLatch ClawTension ClawRelease ClawIceSlip ClawFail
```

The production fixture proves exactly one cue on entry, no process-frame polling spam, no duplicate observer after refresh/re-entry, and cleanup on pause, death/recovery, sector unload, and completion. Shield results carry an immutable unique sequence identity without changing damage, energy, or heat semantics. Legacy room bindings remain only for unrelated older cue families.

### CRLF-safe manifest mutation

The removal mutation now parses `replacementSlots` as JSON and removes exactly one named node. It does not depend on an LF-only text literal. The final detached materialization used `core.autocrlf=true`; `assets/production/prologue-assets.json` contained 87 CRLF pairs and `ProductionAssetManifestTests.cs` contained 232. The real mutation ran through the focused aggregate and replacement case in that checkout.

### Exact five-sector replacement boundary

Replacement validation now requires exactly these five unique identities: `CanyonEntrance`, `CanyonDescent`, `CanyonBasin`, `CanyonAscent`, and `LostTemple`. Each real generated sector scene must expose exactly the replaceable `VisualRoot/{Background,Midground,GameplayReadable,Foreground,Effects}` boundary. Collision and state owners remain outside visual roots.

Executable mutations reject empty, four-sector/incomplete, duplicate, wrong-identity, omitted-subtree, moved-slot, duplicate-slot/path, collision-under-visual, and state-under-visual cases. The runtime proof loads all five generated sector scenes instead of treating one generic EnvironmentKit as five-sector evidence.

### Production bilingual flow and containment

GameFlow/settings language now reaches FormalHud, pause, dynamic tutorial/hidden-alcove prompts, recovery, completion, and repeated-failure guidance. English and Simplified Chinese are exercised through production dispatch; the GPU harness no longer sets the HUD/guidance language or expanded state directly.

Repeated real same-sector death/recovery cycles own expanded guidance. Text height and rendered bounds are measured, the zero-target fallback uses the protected placer, and exact 1280x720 containment is asserted. The existing guidance integration fixture's obsolete direct visual setter was exposed by fresh post and migrated to the same real recovery authority under controller ruling `1591716`.

The capture drives action/loadout/rejection and all six reticles through real InputMap/claw transitions, and captures pause/completion through GameFlow settings. English completion title sizing and the 720p expanded-guidance fallback were corrected after original-resolution RED review.

## RED to GREEN record

- Production wiring RED: real mode, shield, energy/overheat/ready, and claw actions produced no Task 10 director cue/effect because every `Present*` API had zero production callers.
- CRLF RED: the LF-only manifest mutation did not remove its target after CRLF materialization.
- Replacement RED: empty/incomplete sector collections passed vacuously and the runtime case loaded only the generic EnvironmentKit.
- Language/containment RED: English settings left dynamic/recovery/completion copy in Chinese; exact 720p expanded guidance clipped.
- GPU RED 1: zero-target expanded guidance used a hard-coded rectangle that overlapped a protected HUD region.
- GPU RED 2: the English completion heading rendered 589 px wide in a 420 px panel.
- Fresh post RED after production authority was added: `MountedGuidanceRendersAndClearsWithoutHidingObjective` directly set the visual expanded flag, which the real failure-count controller correctly overwrote. Two real same-sector recoveries now drive the assertion.

Final focused markers:

```text
TASK10_FIX1_PRODUCTION_WIRING_OK
TASK10_FIX1_SECTOR_SET_OK count=5
TASK10_FIX1_PRODUCTION_LANGUAGE_OK
TASK10_REPLACEMENT_OK fail_closed=True
TASK10_CUE_VFX_OK typed_cues=16 cleanup_edges=4
TASK10_STALE_CONTROLS_OK legacy_actions=0
TASK10_FIX1_FOCUSED_OK cues=16 sectors=5 languages=2 resolutions=4
TASK10_FOCUSED_OK
TASK9_GUIDANCE_RUNTIME_FOCUSED_OK bilingual_chrome=True
```

## Fresh CRLF verification matrix

Final materialized HEAD: `e55150756ed7a3c01172b07346c41a01568fc45b`.

Root: `C:\Users\22499\AppData\Local\Temp\knight-task10-fix1-crlf-final-f9a4f6fd20c94eacbcb70bdbb787cdef`.

The root was created by a no-local clone with `core.autocrlf=true`, detached at the exact implementation HEAD, and was clean before build/import generated disposable metadata.

| Gate | Final result |
| --- | --- |
| .NET build | exit 0; 0 warnings, 0 errors |
| Godot import | exit 0; `VERIFICATION_EDITOR_GC_DRAIN_OK` |
| Focused Task 10 aggregate | exit 0; both `TASK10_FIX1_FOCUSED_OK` and `TASK10_FOCUSED_OK` |
| Prototype | exit 0; `PROTOTYPE_TESTS_OK` |
| Prologue pre | exit 0; `PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=pre`, `PROLOGUE_TEST_PHASE_OK phase=pre` |
| Prologue post | exit 0; `PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=post`, `PROLOGUE_TEST_PHASE_OK phase=post`, `PROLOGUE_TESTS_OK` |
| Guidance proportional fixture | exit 0; `TASK9_GUIDANCE_RUNTIME_FOCUSED_OK bilingual_chrome=True` |
| Six exact Task 10 categories | all exit 0 with the markers above |

Pre contains exactly its two established intentional `PresentationProfile` negative-fixture warnings. Post contains exactly its three established intentional warnings: throwing claw observer isolation, invalid room visual-root fallback, and rejected `Completed->Recovering` negative transition. No unexpected error line remains.

## Windows GPU capture and review

- Renderer: Windows OpenGL 3.3 compatibility, NVIDIA GeForce RTX 3060 Laptop GPU.
- Capture root: `C:\Users\22499\AppData\Local\Temp\knight-task10-fix1-gpu5-f9b9066a017a48dba40cb5ea9c1df6cd\captures`.
- Terminal marker: `PROLOGUE_UI_RESOLUTION_OK`.
- Exact resolutions: 1280x720, 1920x1080, 2560x1440, and 3840x2160.
- Output: 198 original PNG frames plus two contact sheets.
- 720p sheet: `task10-fix1-contact-720.png`, SHA-256 `BB9D15922115933E4AC34D3ED0E96301C7B57D5E57E1870698DD62DF7FCFF0AE`.
- Four-resolution sheet: `task10-fix1-contact-resolutions.png`, SHA-256 `02A2789429BDE887516359B65783D8BF245D52DDBD3F7042C72ECEC68501183E`.

Original-resolution and contact-sheet review confirms complete 720p expanded English/Chinese guidance, no protected-HUD overlap, readable EN/ZH action/pause/completion presentation, six shape-and-color-distinct reticles, and no visible clipping at any exact resolution. The controller independently accepted both sheets after original-image review.

## Audio, stale-control, and replacement audits

Deterministic regeneration ran in the disposable materialization and left zero tracked audio diff:

```text
TASK10_AUDIO_PEAK_OK files=16 max_dbfs=-4.7335 min_dbfs=-6.1579 limit_dbfs=-0.9
TASK10_AUDIO_DETERMINISM_OK seed=1263421767 tracked_diff=0 manifest_hashes=16
```

Active production scans returned zero `InputActions.Attack`/`InputActions.Thread` readers or press/release calls. Player-facing source/scene/localization scans returned zero J/K prompts, thread-spool/cooldown copy, or route-choice copy. `TASK10_STALE_CONTROLS_OK legacy_actions=0` re-proved the live InputMap contract. Five-sector runtime and all replacement mutations re-ran under `TASK10_FIX1_SECTOR_SET_OK count=5` and `TASK10_REPLACEMENT_OK fail_closed=True`.

## Exact Fix Round 1 file list

Implementation commit `4cb1b64` owns 18 paths; test migration `e551507` owns one additional path:

```text
scripts/audio/PrologueAudioDirector.cs
scripts/player/PlayerShieldController.cs
scripts/presentation/ProductionAssetManifest.cs
scripts/prologue/PrologueGameFlowController.cs
scripts/prologue/PrologueLevelController.cs
scripts/prologue/ProloguePauseMenu.cs
scripts/prologue/guidance/PrologueGuidanceController.cs
scripts/ui/PrologueCompletionView.cs
scripts/ui/PrologueFormalHud.cs
scripts/ui/PrologueRecoveryView.cs
scripts/ui/WorldTargetMarkerVisual.cs
scripts/vfx/PrologueVfxDirector.cs
tests/RunTask10Tests.cs
tests/integration/PrologueAccessibilityTests.cs
tests/integration/PrologueAudioDirectorTests.cs
tests/integration/PrologueGuidanceRuntimeTests.cs
tests/integration/PrologueProductionAssetRuntimeTests.cs
tests/presentation/PrologueUiResolutionCapture.cs
tests/unit/ProductionAssetManifestTests.cs
```

No additional generated production output was necessary in this round. Godot `.import` files and `.godot` cache stayed disposable and uncommitted.

## Protected copies and process audit

The shared worktree copies remained byte-identical throughout final materialized verification:

| Protected working copy | SHA-256 |
| --- | --- |
| `project.godot` | `63E151517E9C332E0588B0942B52D45F2B2D5BE939638D427670819AF36C6B74` |
| `scenes/prologue/PrologueLevel.tscn` | `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115` |
| `scenes/presentation/PrologueEnvironmentKit.tscn` | `C20872EFBB2614E0D9E40216CCD8DD263861009EB2C347CEB03DAD01F4CABC00` |

Final command-line ownership audit: `TASK10_PROCESS_LEAKS=0`. PID 31500 and all `godot-ai.exe` processes were treated as external and were not terminated.

Independent re-review is the next gate. This report does not declare Task 10 complete.
