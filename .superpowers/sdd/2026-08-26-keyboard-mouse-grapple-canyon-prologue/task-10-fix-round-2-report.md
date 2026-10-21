# Task 10 Fix Round 2 implementation report

Date: 2026-08-27

Reviewed starting point: `918015cb82eb7910d88e7d347a03ea2087fd5b14`

Implementation/tests: `4ddf8c6cff59cc7cfbe287c74a51ab612049673e`

Task 10 remains open for independent re-review. This report is an implementation handoff, not a completion claim.

## Scope and outcome

Fix Round 2 closes both findings from the Fix Round 1 review:

- `PrologueAudioDirector` now silently seeds the current action state when real publishers bind or refresh. A loadout publication or a rejection while the player is already in Attack no longer invents `ModeAttack` audio/VFX.
- `PlayerActionModeController` publishes the exact rejection-state exit when a real attack starts. Presentation silently synchronizes that exit, so a later genuine rejection is a new entry again.
- The production prompt uses smart word wrapping, no trimming, and a taller safe text area. The 720p English action instruction renders as two complete lines, and the same measured contract passes at 1080p, 1440p, and 4K.
- The real generated-main/InputMap regression drives loadout selection, energy rejection, recovery, successful combat attack, and rejection re-entry. It checks exact cue identity, order, count, VFX count, and refresh/rebind silence.

No eligibility, energy-spend, mode/loadout, input-routing, combat, or flow authority changed.

## Strict RED to GREEN evidence

The old production behavior failed with the named assertions below:

```text
TASK10_FIX2_FALSE_FIRST_LOADOUT_RED expected=no-cue/no-vfx actual=ModeAttack/vfx=1
TASK10_FIX2_FALSE_FIRST_REJECTION_RED expected=EnergyEmpty/vfx=1 actual=ModeAttack/vfx=1
TASK10_FIX2_ENERGY_REENTRY_RED expected=EnergyEmpty,EnergyEmpty/count=2/vfx=2 actual=EnergyEmpty/count=1/vfx=1
TASK10_FIX2_PROMPT_CONTAINMENT_RED resolution=(1280, 720) wrap=Off actual_lines=1 measured_lines=1 rendered=(335, 23) available=(335, 40)
```

The prompt RED used an exact-HEAD materialization and a Windows GPU frame; the final `OR` in `ANCHOR` was visibly clipped. The corrected production behavior terminates:

```text
TASK10_FIX2_ACTION_CUES_OK false_first=0 energy_reentry=2 vfx=2
TASK10_FIX2_FOCUSED_OK false_first=0 energy_reentry=2 prompt_resolutions=4
TASK10_CUE_VFX_OK typed_cues=16 cleanup_edges=4
```

The action regression begins in the generated main's real Attack/slot-1 state, uses the live InputMap, proves a first loadout creates no cue/effect, proves a first energy rejection creates only `EnergyEmpty`, shuts down and rebinds the director without a publication, then separates two exact `EnergyEmpty` entries with a recovered, successfully started and completed real combat attack. The matching VFX count is exactly two.

## Exact materialized verification

Final verification ran in the disposable exact-HEAD materialization:

```text
C:\Users\22499\AppData\Local\Temp\knight-task10-fix2-red-084109e96477448bb0d43e7f2f90041a
```

The seven implementation files were copied into that fresh materialization; its imported generated-main scene was restored to the exact reviewed HEAD blob before the final matrix. Results:

```text
dotnet build: 0 warnings, 0 errors
PROTOTYPE_TESTS_OK
PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=pre
PROLOGUE_TEST_PHASE_OK phase=pre
PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=post
PROLOGUE_TEST_PHASE_OK phase=post
PROLOGUE_TESTS_OK
TASK10_FIX2_ACTION_CUES_OK false_first=0 energy_reentry=2 vfx=2
TASK10_CUE_VFX_OK typed_cues=16 cleanup_edges=4
TASK10_FIX2_FOCUSED_OK false_first=0 energy_reentry=2 prompt_resolutions=4
TASK10_FIX1_FOCUSED_OK cues=16 sectors=5 languages=2 resolutions=4
TASK10_FOCUSED_OK
TASK10_STALE_CONTROLS_OK legacy_actions=0
TASK10_REPLACEMENT_OK fail_closed=True
```

Pre produced exactly the two established PresentationProfile negative-fixture warnings. Post produced exactly the three established intentional warnings: throwing claw-observer isolation, invalid room VisualRoot rejection, and rejected `Completed->Recovering`. There were no unexpected warning or error lines.

Fix Round 2 does not touch the manifest parser, manifest mutation test, or any newline-sensitive literal. The fresh `core.autocrlf=true` CRLF archive gate from Fix Round 1 therefore remains directly applicable; this round instead re-materialized exact HEAD and ran the complete runtime matrix after copying only the seven changed C# paths.

## Prompt measurements and GPU evidence

The Windows GPU capture terminates `PROLOGUE_UI_RESOLUTION_OK` and produced 198 original PNG frames under:

```text
C:\Users\22499\AppData\Local\Temp\knight-task10-fix2-green1
```

Every exact resolution reports the same logical measured containment:

```text
TASK10_FIX2_PROMPT_BOUNDS_OK resolution=1280x720 lines=2 rendered=(228, 46) available=(246, 50)
TASK10_FIX2_PROMPT_BOUNDS_OK resolution=1920x1080 lines=2 rendered=(228, 46) available=(246, 50)
TASK10_FIX2_PROMPT_BOUNDS_OK resolution=2560x1440 lines=2 rendered=(228, 46) available=(246, 50)
TASK10_FIX2_PROMPT_BOUNDS_OK resolution=3840x2160 lines=2 rendered=(228, 46) available=(246, 50)
```

The assertion checks Godot's rendered line count, font-glyph line widths/heights, the actual label rectangle, and the containing prompt panel; backing-string equality is not accepted as proof. Original-image review confirms the complete two-line English prompt, contained glyph and text, no HUD overlap, bilingual coherence, and the retained six shape-and-color-distinct reticles. The controller independently accepted the corrected 720p action HUD.

| Evidence | SHA-256 |
| --- | --- |
| `task10-fix2-prompt-contact-sheet.png` | `B7378D65A467E5C8EB7B8B273CB9EA90347566089CF54557BF7FFCB4868A2E1E` |
| `task10-fix2-720p-contact-sheet.png` | `0F95CFCA6DD855FB1935E23E54CB4AC50027BD884F394E6CF036AFE51C4CD832` |
| `1280x720/action-hud-en-frame00.png` | `A6629E02CAEDC61C2AF571BD13C28B38BA97AF0DB6A82B21ED6C363A369FA8FC` |
| `1920x1080/action-hud-en-frame00.png` | `D3D13F4F9D8F43DE13A075F78E63A4B9E3D9357124611DA9E4E263B315AAC6EA` |
| `2560x1440/action-hud-en-frame00.png` | `357C9EF5DC0B7207AC2E2C3A71DF7E490BFD3F290C7E073452A7A805633A0379` |
| `3840x2160/action-hud-en-frame00.png` | `A7735D392BD5BCDC1A68D8937482A23E10950939D0DBA1FBD0AAE9A505E10DB3` |

## Generated-scene ruling

The source builder was executed successfully in the disposable materialization:

```text
BuildPrologueLevel: five-sector authored canyon route packed and validated.
```

Diffing its regenerated `PrologueLevel.tscn` against the saved exact-HEAD blob showed the intended prompt serialization only (`offset_top` 8 to 4, `offset_bottom` 48 to 54, and `autowrap_mode = 3`); every other observed difference was nondeterministic resource UID/serialization-default churn. `NoTrimming` and `ClipText = false` are engine defaults and do not serialize. The exact reviewed HEAD scene already passes the corrected runtime prompt behavior through `PrologueFormalHud`, so the controller ruling required source-builder plus runtime code only. No noisy protected scene blob was staged.

## Audio, replacement, stale-control, and leak audits

The exact 16 Task 10 WAVs were regenerated twice in the disposable materialization with equal hashes:

```text
TASK10_FIX2_AUDIO_OK files=16 peak_dbfs=-4.7335 headroom_db=4.7335
TASK10_FIX2_AUDIO_DETERMINISM_OK files=16 exact_hashes=16
```

The unchanged exact 16-cue production wiring and cleanup suite passed, as did the executable replacement and live stale-control scans. No audio, asset, manifest, scene, `.import`, `.uid`, README, or artifact path entered the implementation commit.

The final process audit found no task-owned Godot console process: `TASK10_PROCESS_LEAKS=0`. External PID 31500 and all pre-existing `godot-ai.exe` processes were audited only and never terminated.

## Exact implementation file list

Commit `4ddf8c6cff59cc7cfbe287c74a51ab612049673e` contains exactly seven approved paths:

```text
scripts/audio/PrologueAudioDirector.cs
scripts/player/PlayerActionModeController.cs
scripts/ui/PrologueFormalHud.cs
tests/RunTask10Tests.cs
tests/integration/PrologueAudioDirectorTests.cs
tests/presentation/PrologueUiResolutionCapture.cs
tools/build/prologue/BuildPrologueLevel.cs
```

## Protected copies

The shared user worktree copies remained byte-identical after verification, staging, and the implementation commit:

| Protected working copy | SHA-256 |
| --- | --- |
| `project.godot` | `63E151517E9C332E0588B0942B52D45F2B2D5BE939638D427670819AF36C6B74` |
| `scenes/prologue/PrologueLevel.tscn` | `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115` |
| `scenes/presentation/PrologueEnvironmentKit.tscn` | `C20872EFBB2614E0D9E40216CCD8DD263861009EB2C347CEB03DAD01F4CABC00` |

Independent re-review is the next gate. Task 10 is not marked complete.
