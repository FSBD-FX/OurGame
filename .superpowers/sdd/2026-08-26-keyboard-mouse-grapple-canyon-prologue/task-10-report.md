# Task 10 implementation report

Date: 2026-08-27

Status: implementation and self-verification are ready for independent review. Task 10 is not marked complete.

## Boundary and history

- Starting base: `e8506ca`.
- Aggregate implementation range before this report: `e8506ca..974e39f99f893f1bb0dda12ab2f7027e87b1c9dd`.
- Controller ruling commits in that range:
  - `16e854468e952a4cceac65e68ab1974a5eadc015` — direct production dependencies.
  - `82779ca31e5e88bc33225520971a57ee7fdc008a` — localization data.
  - `43bac83754e814f400c8921e100378fa6dff07ef` — language dispatch.
  - `569363f5435a4893536d7218ce21d14f650c4ff9` — deterministic placeholder audio.
  - `cee114b0093043780e1bc127aab9d1dc4091a883` — index-isolated InputMap output.
  - `b8b2669ba42f2341350d912ca2ce0ba74792a9d8` — immutable mixed ruling/generated-output commit described below.
- Implementation/tests/audio/generated UI/VFX commit: `974e39f99f893f1bb0dda12ab2f7027e87b1c9dd` (`feat: finish canyon controls feedback and ui`).
- No paid generation or formal-art generation was used. All new audio, UI, and VFX are deterministic, replaceable placeholders.
- `README.md`, unrelated imports, artifacts, playtest documents, room/entity scenes, and user dirty work were not staged.

`b8b2669` unintentionally captured three already-staged, approved generated outputs along with the controller ledger. Per controller ruling it was not rewritten or duplicated:

| Path | Git blob | Generated SHA-256 |
| --- | --- | --- |
| `project.godot` | `19a998bb0b710c790c2c66170a49eb523c62cf13` | `7D64BCCFC70B419DE4AC55471E3A60301D1507F041B8B1A81451D798E0C6DA0E` |
| `scenes/prologue/PrologueLevel.tscn` | `418027b470a55e40baa697e280b3c6eb55bbe259` | `735766ACFDA8E89EADF99FBF9C41625FDF85BC209AD7FB29F6E794B7E717BB18` |
| `scenes/presentation/PrologueEnvironmentKit.tscn` | `6d246e28dda4f9f4def487582cff5d937af32f94` | `59AD67D1CD1E5E2F735C1CBDE39860C3BF0B7EED3DF34F1F91EB114412737F32` |

The generated `project.godot` preserves the committed main scene and contains Q/E/LMB/RMB/1–4 plus gamepad bindings while omitting `attack` and `thread`. The user's local `PrototypeRoom` main-scene override was never staged.

## RED to GREEN

The first strict RED run observed independent named failures in every required category:

- UI: stable path `Hud/FormalHud/AimReticle` was missing.
- Audio: typed cue `ModeAttack` was missing.
- VFX: after enum availability, replaceable root `World/ShieldFan` and its transition behavior were missing.
- Replacement boundary: required manifest slot `slot.hud.energy` was missing.
- Stale controls: the live InputMap still exposed the legacy `attack` action.

Minimal GREEN work then provided:

- immutable mode, energy/heat, loadout, and reticle frames;
- six reticle states with unique color and geometry/line semantics;
- responsive no-overlap/clamped layouts at the four required resolutions;
- current keyboard/gamepad glyphs and coherent English/Chinese labels;
- 16 exact typed audio cues and replaceable VFX roots with entry-only dispatch and cleanup;
- schema-v2 exact replacement slots with fail-closed parser/runtime mutations;
- current Q/E/LMB/RMB/1–4 InputMap and runtime readers, with adapter-only legacy identifiers retained but never polled;
- hidden, empty legacy `ThreadSpool` adapter presentation with no cooldown UI;
- current-input migrations in directly affected runtime fixtures.

Original-resolution capture review found and corrected two additional visible defects before the final capture: duplicate ENERGY/HEAT labels, and a compound `[E + LMB]` glyph overflowing into prompt copy. Focused tests now assert the prompt child regions do not overlap at supported UI scales.

## Exact focused markers

All focused cases exited zero:

```text
TASK10_STALE_CONTROLS_OK legacy_actions=0
TASK10_UI_STRUCTURE_OK reticle_states=6 resolutions=4
TASK10_CUE_VFX_OK typed_cues=16 cleanup_edges=4
TASK10_REPLACEMENT_OK fail_closed=True
TASK10_CURRENT_GRAPPLE_FIXTURE_OK
TASK10_CURRENT_ATTACK_PRESENTATION_OK
TASK10_CURRENT_INPUT_CLEANUP_OK suites=2
TASK10_FOCUSED_OK
```

The replacement case executes removal, move, duplicate, missing-coverage, collision-under-`VisualRoot`, and state-ownership mutations. The cue/VFX cases execute real entry transitions, no-polling-spam assertions, and death/pause/unload/completion cleanup.

## Fresh verification matrix

Exact executables:

- Godot: `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe`
- .NET: `G:\godot\dotnet-sdk\dotnet.exe`
- `godot-ai.exe` was never invoked or terminated. PID 31500 was treated as external.

| Gate | Result | Log/evidence |
| --- | --- | --- |
| .NET build | exit 0, 0 warnings, 0 errors | console result after final source changes |
| Godot import | exit 0, 0 warnings, 0 errors, `VERIFICATION_EDITOR_GC_DRAIN_OK` | `%TEMP%\knight-task10-import-final.log` |
| Prototype | exit 0, `PROTOTYPE_TESTS_OK` | `%TEMP%\knight-task10-prototype-final.log` |
| Prologue pre | exit 0, `PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=pre`, `PROLOGUE_TEST_PHASE_OK phase=pre` | `%TEMP%\knight-task10-pre-final.log` |
| Prologue post | exit 0, `PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=post`, `PROLOGUE_TEST_PHASE_OK phase=post`, `PROLOGUE_TESTS_OK` | `%TEMP%\knight-task10-post-final2.log` |
| Focused Task 10 matrix | seven exact focused cases exit 0 | `%TEMP%\knight-task10-focused-final\*.log` |
| Windows GPU capture | exit 0, `PROLOGUE_UI_RESOLUTION_OK` | `%TEMP%\knight-task10-gpu-capture-final2.log` |

Pre contains only its two established intentional negative-path `PresentationProfile` warnings. Post contains only its three established intentional warnings: throwing claw observer isolation, invalid room visual root fallback, and rejected invalid completed-to-recovery transition. Both logs contain zero error lines.

## Capture review

- GPU: Windows OpenGL 3.3 compatibility renderer, NVIDIA GeForce RTX 3060 Laptop GPU.
- Root: `C:\Users\22499\AppData\Local\Temp\knight-task10-captures-final2`.
- Exact resolutions: 1280×720, 1920×1080, 2560×1440, and 3840×2160.
- Output: 162 PNGs, including three consecutive frames for every required state.
- Reviewed original-resolution EN/ZH action HUD frames at 1280×720 and the EN frame at 3840×2160.
- Reviewed contact sheet: `C:\Users\22499\AppData\Local\Temp\knight-task10-captures-final2\task10-contact-sheet.png`.
- Contact-sheet SHA-256: `9D5A20EEF5A7F6A535D8AD7F9AFB2FF8FC947798B3E7578EB75E15BB6BD91633`.
- Review result: EN/ZH presentation is coherent; compound prompt glyph is separated; resource labels are not duplicated; the six reticles remain visibly distinct by shape and color; ordinary and Boss states have no evident protected-region overlap.

## Audio and stale-control audits

```text
TASK10_AUDIO_PEAK_OK files=16 max_dbfs=-4.7335 min_dbfs=-6.1579 limit_dbfs=-0.9
TASK10_AUDIO_DETERMINISM_OK seed=1263421767 manifest_hashes=16
```

The runtime audio tests additionally re-prove the current Master limiter/headroom contract and exact SFX bus routing. Exactly 16 Task 10 WAVs are committed. Their generated `.import` sidecars were removed and are not staged.

Player-facing source/generated-scene scans returned zero matches for J/K prompts, old thread cooldown/spool text, and route-choice copy. Active runtime reader/release scans returned zero uses of `InputActions.Attack` or `InputActions.Thread`. The compatibility constants and tutorial IDs remain adapter-only.

## Protected copies and leak result

Every generated-scene/import/test window restored the user's bytes immediately after the task-owned process exited:

| Protected working copy | Restored SHA-256 |
| --- | --- |
| `project.godot` | `63E151517E9C332E0588B0942B52D45F2B2D5BE939638D427670819AF36C6B74` |
| `scenes/prologue/PrologueLevel.tscn` | `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115` |
| `scenes/presentation/PrologueEnvironmentKit.tscn` | `C20872EFBB2614E0D9E40216CCD8DD263861009EB2C347CEB03DAD01F4CABC00` |

Final process audit: `TASK10_PROCESS_LEAKS=0`. Only the pre-existing Godot/godot-ai PIDs remained; none were killed.

## Aggregate implementation file list

The range `e8506ca..974e39f` contains 72 paths: the 68-file implementation commit, the three exact generated outputs in `b8b2669`, and the controller progress ledger.

### Runtime, data, generated outputs, and tools

```text
assets/audio/prologue/audio-manifest.json
assets/audio/prologue/sfx/claw/{fail,fire,ice-slip,latch,release,tension,travel}.wav
assets/audio/prologue/sfx/mode/{attack,empty-slot,energy-empty,grapple}.wav
assets/audio/prologue/sfx/shield/{block,heavy-break,overheat,raise,ready}.wav
assets/localization/prologue-ui.json
assets/production/prologue-assets.json
project.godot
scenes/presentation/PrologueEnvironmentKit.tscn
scenes/prologue/PrologueLevel.tscn
scenes/ui/PrologueUi.tscn
scenes/vfx/PrologueVfx.tscn
scripts/audio/PrologueAudioCue.cs
scripts/audio/PrologueAudioDirector.cs
scripts/player/PlayerInputReader.cs
scripts/presentation/ProductionAssetManifest.cs
scripts/presentation/ProductionAssetManifestParser.cs
scripts/presentation/RoomVisualCatalog.cs
scripts/prologue/PrologueGameFlowController.cs
scripts/prologue/PrologueLevelController.cs
scripts/prologue/ProloguePauseMenu.cs
scripts/shared/InputActions.cs
scripts/ui/InputGlyphResolver.cs
scripts/ui/PrologueActionModeVisual.cs
scripts/ui/PrologueAimReticle.cs
scripts/ui/PrologueControlsView.cs
scripts/ui/PrologueEnergyHeatVisual.cs
scripts/ui/PrologueFormalHud.cs
scripts/ui/PrologueHudViewModel.cs
scripts/ui/PrologueLoadoutVisual.cs
scripts/vfx/PrologueVfxDirector.cs
scripts/world/PrototypeRoomController.cs
tools/audio/render_knight_audio.py
tools/build/art/BuildPrologueEnvironmentKit.cs
tools/build/art/BuildPrologueVfx.cs
tools/build/prologue/BuildPrologueLevel.cs
tools/build/ui/BuildPrologueUi.cs
tools/configure/ConfigureProject.cs
```

### Tests and capture harness

```text
tests/RunTask10Tests.cs
tests/integration/BellChainRuntimeTests.cs
tests/integration/GraveLampMothRuntimeTests.cs
tests/integration/PlayerFormalVisualTests.cs
tests/integration/PlayerTraversalInputTests.cs
tests/integration/PrologueAccessibilityTests.cs
tests/integration/PrologueAudioDirectorTests.cs
tests/integration/PrologueFormalHudTests.cs
tests/integration/PrologueProductionAssetRuntimeTests.cs
tests/integration/PrologueTraversalRouteTests.cs
tests/integration/PrologueUiRuntimeTests.cs
tests/integration/PrologueUiStructureTests.cs
tests/integration/PrologueVfxRuntimeTests.cs
tests/integration/ThreadCastRuntimeTests.cs
tests/presentation/PrologueUiResolutionCapture.cs
tests/unit/InputGlyphResolverTests.cs
tests/unit/PresentationCueCoverageTests.cs
tests/unit/ProductionAssetManifestTests.cs
tests/unit/ProjectFoundationTests.cs
tests/unit/PrologueAudioManifestTests.cs
tests/unit/PrologueHudViewModelTests.cs
```

Independent spec and quality review is the next gate. This report does not declare Task 10 complete.
