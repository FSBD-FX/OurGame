# Task 8 implementation and evidence

Scope: reachable temporary firefly guidance plus the already-confirmed markerless first-act teaching-card layout defect. Worktree `E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`, branch `codex/task6-certified-final-20260902T0555`, authoritative starting HEAD `eafeca8cb920d7713ee42729dcab786b718da40c`. No helpers, asset generation, downloads, SDK/engine changes, release work, resets, clean, add-all, or edits outside the brief's twelve source/test paths and generated parent scene.

## Implemented contracts

- `CanyonFireflyGuidanceModel` is a pure elapsed-active-play model: 18 seconds stalled, 3 seconds visible, 15 seconds cooldown. Pause freezes all clocks. Guidance-off, missing reachability, combat telegraphs, route progress, important forks and Boss-exit state suppress or clear the hint as specified; progress prevents a repeat until the route context changes.
- A single `WorldRoot/FireflyGuidance` controller/view is generated outside streamed chunks. The controller is inert for the layout-1 Basin sample and uses the existing first-act guidance toggle for layout 2. Its view has no collider, persistence, or gameplay authority.
- Route eligibility uses the actual route graph with exact open-shortcut, activated-mechanism/seal and Boss-result state, mounted authored markers and reachable support. It rejects unloaded markers, closed paths, Ice/Forbidden support and required offscreen targets. It preserves Task 6's living current-sector Rust query and does not treat the static `EntranceCombat` marker as an actor.
- `CanyonCombatTelegraphAdapter` is a narrow local type switch for `RustShellSquire`, `BellBackLaborer`, `GraveLampMoth` and `FallenBellkeeper`, reading each real `CurrentFrame.AttackTimeline.Phase`. The controller scopes actors to the activated local encounter/Boss arena, clears invalid/unmounted/out-of-encounter state and ORs real mechanism telegraphs. The actual PhaseOne sweep and a pre-lock Telegraph with `DangerVisible=false` both defer guidance.
- Title, recovery, new-session and completed lifecycle paths clear the world-level hint. The controller does not create a second subscription registry or change enemy APIs.
- A genuine pending markerless first-act lesson now probes legal screen edges derived from the existing protected rectangles and safe region. It avoids the central action/player/landing/HUD regions, preserves full wrapped text and pending authority, retains same-owner diagnostics and off-owner known-static projection semantics, hides when no legal placement exists, and reappears when one returns. Targeted, dynamic/unknown and layout-1 sample behavior remain intact.
- Chinese and English claw teaching now say E -> click -> automatic pull -> Space release. Neither W/S nor held LMB is taught as required; Q attack/defense/energy/overheat semantics remain unchanged.

## Build and engine convention

Every actual engine/builder invocation followed its own successful fresh pinned build. The build command was:

```text
G:/godot/dotnet-sdk/dotnet.exe build Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo
```

`DOTNET_ROOT` and `DOTNET_ROOT_X64` were `G:/godot/dotnet-sdk`, `DOTNET_MULTILEVEL_LOOKUP=0`, `DOTNET_ROLL_FORWARD=LatestMajor`, `DOTNET_PROCESSOR_COUNT=2`, with the pinned SDK first on `PATH`. The only engine was `G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe`; headless cases used fixed FPS 60. Processes were launched hidden, awaited, and recorded with separate build/engine command, PID, timestamps, exit, stdout and stderr in the case directories below. The two early model cases predate the complete metadata wrapper: their successful build exit/log and actual engine PID/exit are present, but a separate build PID was not recorded. They were not rerun merely to improve metadata.

All evidence directories below are under `E:/OurGame/Knight/.evidence/september09-continuous-canyon/`.

## TDD, mutations, builder and visual cases

| Exact case directory | Build PID/exit -> Godot PID/exit | Result |
|---|---:|---|
| `task8-interface-red-20260920T114724889-56fd9d3d2e844334a5de0910ded3dcb4` | build / 1; no engine | Expected interface RED for missing firefly types. It is retained as compiler evidence only, not the behavioral RED. |
| `task8-model-behavior-red-20260920T114812096-27067b45b438441dbe3f455ec1113ee3` | unrecorded / 0 -> 50196 / 1 | Compile-safe behavioral RED: exact threshold behavior failed. |
| `task8-model-green-20260920T114910856-8aee6268c64441fd854e4229bb3b1c6f` | unrecorded / 0 -> 67140 / 1 | Despite the folder label, **not GREEN**: stale hint after progress. This result was not reclassified. |
| `task8-model-green2-20260920T114959317-1cc3fc6108c248d38640ea31cc8c594c` | unrecorded / 0 -> 26644 / 0 | Pure model GREEN for threshold, pause, cooldown, deferral, fork/Boss exit, progress and cleanup cases. |
| `task8-adapter-behavior-red2-20260920T115338231-f7b1d487b7304ff3877fb685303c7765` | 48124 / 0 -> 59556 / 1 | Valid publisher-phase RED using the actual four actor frame publishers. |
| `task8-adapter-green-20260920T115425893-961563e97b7046e8b534f8f91f9ea705` | 34016 / 0 -> 30476 / 0 | Adapter GREEN, including real Boss sweep and danger-invisible pre-lock telegraph. This proves publisher adaptation; world integration is covered below. |
| `task8-runtime-interface-red-20260920T115810724-ac476978af8643ba845f3a91289a841c` | build / 1; no engine | Expected runtime-interface compile RED before controller/view APIs existed. |
| `task8-runtime-behavior-red-20260920T115924997-87c866672085450cbbc9aa77dc8f67fe` | 50260 / 0 -> 52504 / 1 | Compile-safe generated-level RED: parent lacked `FireflyGuidance`. |
| `task8-builder-20260920T120541640-21b85d589d8640efb4fbbf09b70d668c` | 35152 / 0 -> 45764 / 0 | First successful parent regeneration with world-level guidance wiring. |
| `task8-runtime-green-candidate18-keyboard-route-20260921T021633111-bf801a5ef7334e02a6cf883edc74c652` | 3308 / 0 -> 72788 / 0 | Focused runtime GREEN. Normal Start/Playing, enabled normal camera and actual D+Space input reach Descent; no position injection. Covers generated controller, markerless pending lesson, both languages/resolutions/UI scales, no-placement/reappearance, dynamic/unknown targets and the actual layout-1 sample entry. Empty stderr. |
| `task8-runtime-lifecycle-green-candidate3-20260921T022632098-4f9962032f134b548785585b0a07be79` | 61064 / 0 -> 23220 / 0 | Lifecycle cleanup GREEN after correcting the fixture's same-frame reprime/camera-settle assumptions. Empty stderr. |
| `task8-existing-guidance-regressions-green-20260921T023154460-b72645d523a74b3e9088c9902582585b` | 67648 / 0 -> 49828 / 0 | Retained target-resolution guidance regression GREEN. |
| `task8-mutation-shortcut-red-20260921T023251403-fbee66ba74454df0a4b830f07ce2772e` | 22876 / 0 -> 48168 / 1 | Additional temporary source mutation correctly broke the named shortcut-open eligibility assertion. Exact source was restored immediately. |
| `task8-gates-material-localization-green-20260921T023416798-7163908f87644f6299bcde7d55de487a` | 12144 / 0 -> 30520 / 0 | Runtime-state mutation GREEN: shortcut closed -> open -> reset -> restored; completed seal missing -> active -> reset -> restored; support original -> Ice rejected -> Forbidden rejected -> original eligible. Each state changed independently. Both localization contracts pass. Empty stderr. |
| `task8-moving-ui-before-legacy-center-20260921T023527901-251a8bead8ed40759b2a26f1055ab975` | 75464 / 0 -> 22704 / 0 | Controlled legacy-placement toggle, not a historical full build. Vulkan Mobile, 1280x720, 420 frames at 30 FPS; actual D+Space/camera route. AVI SHA-256 `6D907EBF661D87BE27E2392B713588754B04DDA8C1D50142517549581D448808`. |
| `task8-moving-ui-after-safe-edge-20260921T023630175-ad7fa6e02a86404f81e05adab2bf7b32` | 73420 / 0 -> 11676 / 0 | Restored safe-edge code. Vulkan Mobile, 1280x720, 421 frames at 30 FPS; AVI SHA-256 `C5E82A70E802F0F76993DD59ACCF2B913B719CF6C68A726C4C4689603CCB26A4`. |
| `task8-final-builder-parity-20260921T024052975-7c341b06611246949e6ae7257adcf78c` | 30092 / 0 -> 63636 / 0 | Final parent builder success. It reserialized the scene, so this is regeneration proof, not byte parity: scene `4B730A...` -> `5ADECB6C...`. |
| `task8-final-generated-runtime-green-20260921T024150822-edf82e7356b049b0a32d5d38e4b2c06b` | 46864 / 0 -> 54048 / 0 | Focused runtime GREEN against the final generated scene SHA-256 `5ADECB6C519F8D586207E833271BC9B690E71A8E457F51F0B5BC0D849C80CD1C`. Empty stderr. |
| `task8-final-default-aggregate-20260921T024226564-197d87f4fdec48f29adb9da063745f80` | 33976 / 0 -> 42964 / 0 | Final default runner: `CONTINUOUS_CANYON_TESTS_OK`, zero `ERROR:` headings and exactly the existing eight intentional warnings. |

The default eight warnings are the accepted one terminal `Completed -> Recovering` rejection, six invalid `Continue`/`Title -> Playing` cases, and the deliberate throwing-claw observer warning. No additional warning or error was accepted.

The failed runtime candidates before candidate 18 are diagnostic history, not claimed GREEN. Candidate 5 exposed uninitialized sample resources plus the 1280x720 Chinese 0.8-scale markerless placement failure. Candidate 10 established that the existing finite candidate set missed a legal edge. Candidate 12 showed that a disabled camera/title-gated fixture was not actual movement. Candidate 14 showed `Playing` with `PlayerInputReader` disabled. The final fixture follows the real flow owner, enables/settles the normal camera and uses actual parsed keyboard input. Raw logs retain every intermediate result.

### Moving-card review

Both AVIs decoded completely with FFmpeg exit 0. The independently inspected full-resolution 8.8/9.1/9.5-second frames show the genuine Descent markerless state: before `(376,289,248,142)` in the central play area; after `(48,289,220,142)` on the left edge. The full pending Q/E/1-4/LMB card remains wrapped and unellipsized, while the player, current support, HUD and forward route remain visible. The 10.5/12.5-second frames had already returned to Entrance and show a targeted marker, so they are explicitly excluded from the markerless comparison. See `task-8-moving-layout-root-review.md` and the `root-review-*.png` files in the two evidence directories. This certifies the sampled layout/readability improvement and complete file decode, not whole-film human motion smoothness, audio, real-time performance or full-level art quality.

## Hashes and restoration

Initial owned working-file SHA-256 values recorded before edits were:

| Path | Initial SHA-256 |
|---|---|
| four new firefly source files and two new firefly tests | absent |
| `scripts/prologue/guidance/PrologueGuidanceController.cs` | `3BF36F0A5A9F2BEE7B6FEFB2A4BEB464E84BB4C664B8C89532DA4353BAAD9D9E` |
| `scripts/ui/WorldTargetMarkerVisual.cs` | `259351B9435FC01B82A384107F8597057E472E748B9749569FAB108CCBC2AFA1` |
| `assets/localization/prologue-ui.json` | accepted Task7 blob content SHA-256 `2FB896345C3AC79E6AB6328E9BC0E906FF21D6031702DCF2EE359256EE04A38C` (Git-normalized bytes) |
| `tools/build/prologue/BuildPrologueLevel.cs` | `4D67A1B6939178D135E7C12F3F526D13D6305C2AC715F3738454A2E96C28737D` |
| `tests/integration/PrologueGuidanceRuntimeTests.cs` | `6FD0D8CBAFBF782EA9A281E610544B9B72D6A4A1CAF0FD68C36BB207452B38C5` |
| `tests/RunContinuousCanyonTests.cs` | `7E36A521C340E7D10954069E81F797183F98D13706E17229EC760809453C777D` |
| `scenes/prologue/PrologueLevel.tscn` | `395FDDDAFF204546542592BC40D5957E478048D1CCB44CB35E2EDE6D0528F69F` |

Final owned source/test/generated SHA-256 values are:

```text
C65D90AB486005E97EED7A5F6D2AC12E9C65B1BD6147FF6DC83E046FF7513D15  scripts/prologue/guidance/CanyonFireflyGuidanceModel.cs
389EB7D98B197C774017B6E4490A9B80015F43D2D2BC0C4DEBCED8C35E24F850  scripts/prologue/guidance/CanyonFireflyGuidanceController.cs
1A5A29F76334F503A57C9D72FE82E06A7639DEA20D1C94C29819BD34842F2DE2  scripts/prologue/guidance/CanyonFireflyGuidanceView.cs
435B498247990EA5D6EAB4D48A36FB79A130B3522C3BB7E03E90FB5F0B9647B4  scripts/prologue/guidance/CanyonCombatTelegraphAdapter.cs
61DD508A3C638566A225D286C805FE0D7844C3D7B3BB19FC98D03CD89D2CF20C  scripts/prologue/guidance/PrologueGuidanceController.cs
0F27A2218971167DBDA0A7C026D382332A8F7C2769DFF3E48494E4E857579819  scripts/ui/WorldTargetMarkerVisual.cs
09684EC2818B05C7C1E065DC02A3FCBEFC8E47036242A4F4C7D181048A381514  assets/localization/prologue-ui.json
BBA22066F643AA2410AED31C579D5C3F10ED8C6F57FF26B8E33E3065D56FB4B9  tools/build/prologue/BuildPrologueLevel.cs
59EC7A41095BC322F8AFAB967151E2196F0D6A14FC8B913697C7E04B89AF010C  tests/unit/CanyonFireflyGuidanceModelTests.cs
A8C71891F5AE49413C214BCE602F3E9DBF1B221F951E52C789C930C435BD02F1  tests/integration/CanyonFireflyGuidanceRuntimeTests.cs
AE827F92450B63A9C2A8F4E56C6CB0714342E4F902859DC9D00233B9619DF1E4  tests/integration/PrologueGuidanceRuntimeTests.cs
A3190F9BBFF34635B2C1813ACA9D7E61FE0FD05E9A91BF95EAC7B5C79444E6E1  tests/RunContinuousCanyonTests.cs
5ADECB6C519F8D586207E833271BC9B690E71A8E457F51F0B5BC0D849C80CD1C  scenes/prologue/PrologueLevel.tscn
```

The temporary source shortcut mutation changed `CanyonFireflyGuidanceController.cs`, produced its named failure, then was inverted exactly. Its SHA-256 before and after is `389EB7D98B197C774017B6E4490A9B80015F43D2D2BC0C4DEBCED8C35E24F850`; all later focused/default runs used the restored file. The visual's controlled legacy-layout toggle was also restored; `PrologueGuidanceController.cs` SHA-256 is `61DD508A3C638566A225D286C805FE0D7844C3D7B3BB19FC98D03CD89D2CF20C`, matching the after-movie metadata and final file. Runtime gate/material mutations restore their original values inside `finally` blocks and the final aggregate ran afterward. No cache was patched.

## Changed paths

- `scripts/prologue/guidance/CanyonFireflyGuidanceModel.cs`
- `scripts/prologue/guidance/CanyonFireflyGuidanceController.cs`
- `scripts/prologue/guidance/CanyonFireflyGuidanceView.cs`
- `scripts/prologue/guidance/CanyonCombatTelegraphAdapter.cs`
- `scripts/prologue/guidance/PrologueGuidanceController.cs`
- `scripts/ui/WorldTargetMarkerVisual.cs`
- `assets/localization/prologue-ui.json`
- `tools/build/prologue/BuildPrologueLevel.cs`
- `tests/unit/CanyonFireflyGuidanceModelTests.cs`
- `tests/integration/CanyonFireflyGuidanceRuntimeTests.cs`
- `tests/integration/PrologueGuidanceRuntimeTests.cs`
- `tests/RunContinuousCanyonTests.cs`
- `scenes/prologue/PrologueLevel.tscn`
- `.superpowers/sdd/2026-09-09-continuous-canyon-world/task-8-report.md`

## Self-review and limits

Self-review checked the pure timing transitions, route-gate predicates, actual publisher type switch, encounter scoping, lifecycle clearing, layout-version gate, no gameplay authority in the view, pending lesson authority, no-target reappearance, text containment and exact owned generated wiring. `git diff --check` is clean apart from Git's line-ending notices. No reviewer subagent was spawned; root supplies independent review.

The old broad guidance fixture still contains pre-Task7 authored paths (`GripStoneCatchZone`, static `FirstCombat` marker assumptions and old A/D-only routing). Five attempted broad retained runs document those stale contracts. They were not weakened or rewritten because final reconciliation belongs to Task 10 and the corresponding unowned runner was preserved. The in-scope retained target resolver, generated Task8 runtime, localization/gate/material cases and complete default continuous runner all pass.

This is automated and sampled rendered evidence, not full human combat/fun/audio/performance/whole-prologue visual acceptance. The firefly direction check found no player-visible reverse prompt during high-speed ascent and required no selector change. Existing repetitive slabs/sparse scenery and wider presentation quality remain later-task concerns. Root-owned README/plan/ledger, `tests/RunPrologueTests.cs`, `default_bus_layout.tres`, import/artifact dirt, Basin camera files and accidental `--task5-lifecycle-fix-only/` remain untouched and unstaged.

## Formal review fix round 1 — 2026-09-21

Starting point for this round was `ee8829a26ea5a069f9d3c8489b5af606a4461c4e`. This round addresses all three Important findings and the live-instance Minor finding from `task-8-review-report.md`; it does not change the previously reviewed movie appearance.

### Findings to fixes

- **Ordinary pause:** `CanyonFireflyGuidanceController` previously ran in Always mode but treated every non-Playing flow state as cleanup. Actual `ProloguePauseMenu.Open` therefore reset the stalled/visible/cooldown model. Paused is now a distinct branch: it advances the model with `Paused=true`, preserving the model frame, marker, telegraph snapshot and view. Guidance-off during pause still performs full cleanup; Title, recovery, completed and other non-Playing states retain cleanup behavior.
- **Mechanism locality:** mechanism danger is no longer accepted merely because its root is tree-visible in the large streamed sector. The controller computes the full transformed AABB of each real rectangular `CollisionShape2D`, including child offset, scale and pendulum rotation. A mechanism can defer guidance only when its physical footprint intersects both the current authored route-node `WorldBounds` and the current canvas viewport. This uses actual collision authority rather than telegraph visuals, a fabricated relevance flag or a root-position radius. Existing activated encounter/Boss actor scoping is unchanged.
- **Retained coverage and runner interface:** all 15 `PrologueGuidanceRuntimeTests` registry keys now execute against current continuous-canyon authorities. The no-argument `RunContinuousCanyonTests` path runs Task8 model, generated runtime, actual markerless and full retained guidance coverage exactly once before the existing aggregate. Focused Task8 flags still return after their requested slice.
- **Live no-placement lifecycle:** the test now blocks the actual mounted pending card with the real `ObjectiveRibbon`, observes instruction authority and the complete live label retained while content is hidden, restores the ribbon, and observes the same card reappear contained with the complete label. A temporary source mutation that cleared `Label.Text` produced the expected RED before exact Git-blob restoration.

The retained registry preserves every case ID. Its old room-specific meanings map to layout 2 as follows: old wrong/valid anchor distinction -> real Descent GripStone versus Ice collision; pending reset -> recovery volumes cannot consume the Basin seam; failed Ice cast -> visible, collidable typed `FrozenCeiling`; apply/landing -> validated `rift-to-abyss` landing contract; three Upper assists -> three distinct Ascent markers plus three distinct validated landing contracts; first encounter -> one activation-authored Rust-Shell in `CanyonEncounterCatalog` while the static marker is not treated as an actor; recovery/checkpoint cases -> five recovery authorities and the two stable checkpoint IDs; critical shortcut/Boss cases -> current BellChain/door/seal/Boss ordering and fresh state; missing assist target -> every current traversal contract validates, every current Assisted segment resolves its `AssistTargetPath` to `GrappleSurface2D`, every route-node marker exists, and every forward/reverse edge contract resolves. The shared legacy helper was not edited or pinned to layout 1.

### Fix-round evidence

All directories are under `E:/OurGame/Knight/.evidence/september09-continuous-canyon/`. Every engine invocation below immediately followed its own successful pinned build with the environment and engine convention documented above.

| Exact case directory | Build PID/exit -> Godot PID/exit | Result |
|---|---:|---|
| `task8-fix1-pause-locality-visual-red-20260921T030744270-f9da0cb098de496fad3eef3fe4d6d125` | 41200 / 0 -> 45524 / 1 | Fixture-setup failure: the chosen player position had no on-screen marker. Not accepted as behavioral RED. |
| `task8-fix1-pause-behavior-red-20260921T030918880-6694228267344693b128ad5ddb180d8a` | 60120 / 0 -> 53544 / 1 | Valid actual-owner pause RED: resumed stalled clock failed at the exact threshold because pause had reset it. |
| `task8-fix1-pause-green-locality-red-20260921T031011369-1bb9fc345c76400b89653f625269898a` | 55444 / 0 -> 66356 / 1 | Pause assertions progressed through stalled, visible and cooldown freeze; valid locality RED then failed because remote same-sector SteamJet deferred local guidance. |
| `task8-fix1-pause-locality-visual-green-20260921T031237616-8295b0cfce214b17bef093d2a4c69407` | 73716 / 0 -> 24400 / 0 | Focused GREEN for actual pause/resume clocks, remote SteamJet rejection, nearby GearPiston acceptance, and mounted markerless live-instance hide/restore. Build had zero warnings/errors. |
| `task8-fix1-live-card-authority-mutation-red-20260921T031345784-b1f0e96aeb144608b504c682a2a0cdf4` | 73532 / 0 -> 40528 / 1 | Controlled mutation cleared the live label in the no-placement branch; the new actual-card authority assertion failed at `PrologueGuidanceRuntimeTests:231`. |
| `task8-fix1-live-card-authority-restored-green-20260921T031524086-32f10d16117c431f9c239245cb876a00` | 58040 / 0 -> 27284 / 0 | Restored Git blob GREEN; `TASK8_FIREFLY_RUNTIME_TESTS_OK`, zero build diagnostics and empty engine stderr. |
| `task8-fix1-full-retained-guidance-20260921T031602423-b3cc39cd185c4460929a8e459e899bc7` | 38916 / 0 -> 56572 / 1 | Valid retained-suite RED: obsolete current-resource lookup for removed `GripStoneLesson`. |
| `task8-fix1-full-retained-guidance-candidate2-20260921T032023588-35e8dff5d2d147f99ed0a89a4deea5ea` | 42128 / 0 -> 33372 / 1 | Fixture correction progressed to first-encounter pressure; offline current sector correctly contained zero resident enemies, exposing the need to read the activation catalog instead of the static marker. |
| `task8-fix1-full-retained-guidance-candidate3-20260921T032145231-bc68bcd2cef644199f5b1679c3d2ec1a` | 62832 / 0 -> 68572 / 0 | Full retained guidance GREEN: all 15 registry cases completed; `TASK8_EXISTING_GUIDANCE_REGRESSIONS_OK`; zero build diagnostics and empty engine stderr. |
| `task8-fix1-final-default-noarg-20260921T032222925-243c27c6a64a4981a36e733e2ed35fca` | 48672 / 0 -> 39188 / 0 | Final no-argument GREEN: `TASK8_DEFAULT_GUIDANCE_COVERAGE_OK` and `CONTINUOUS_CANYON_TESTS_OK`. Build zero warnings/errors, Godot zero `ERROR:` headings and exactly the existing eight intentional warnings. |
| `task8-fix1-full-retained-guidance-final-20260921T032633785-e9bb1e6e18124e0d98d698fdc8e4878e` | 12840 / 0 -> 70080 / 0 | Final retained GREEN after restoring every current Assisted-target resolution assertion; all 15 registry cases completed, build zero warnings/errors and engine stderr empty. |
| `task8-fix1-final-default-noarg2-20260921T032710192-2b00d275cc7e486b84974869776e8cbf` | 24424 / 0 -> 33340 / 0 | Final authoritative no-argument GREEN after the retained-target addition: both Task8/default sentinels, zero `ERROR:` headings and exactly the existing eight intentional warnings. |

The valid pause RED and locality RED are distinct actual engine results even though the second folder contains the pause fix. Candidate 3 is the retained-suite GREEN; earlier retained candidates are not reclassified.

### Corrections to the original report

- Candidate 12 did **not** prove a global title-gating change. `PrologueGameFlowController.HeadlessAutoStart` defaults to true; the fixture disabled the normal camera and did not establish the required normal Start/Playing path. The later D-only `PlayerInputReader=false` observation was recovery timing. Candidate 18 remains the valid actual D+Space, enabled-camera proof.
- Shortcut, completed-seal and Ice/Forbidden gate mutations used isolated `PrologueProgress` objects plus a restored support material. They prove independent state changes and eligibility restoration inside those isolated fixtures; they were not mutations of one shared final global progress object.
- The earlier “broad guidance fixture deferred to Task 10” limitation is superseded. Task8 now executes and reconciles the complete retained `PrologueGuidanceRuntimeTests` suite and the no-argument consumer. There is no Task8 coverage omission hidden behind a later task.
- Existing moving evidence remains scoped exactly as previously stated: two fully decoded controlled-layout movies and independently inspected markerless frames, not whole-film motion quality, real-time performance or whole-level visual acceptance.

### Hashes, restoration and self-review

Final fix-round SHA-256 values:

```text
D378C79927D998B1DDFBB0C016E635DC433D55103DE122ACE70E2E501FDF7B20  scripts/prologue/guidance/CanyonFireflyGuidanceController.cs
43C3D137BC608C4055AECACDAAEEF1649B9EFD0405946F2E43528EA8EB1CD77B  tests/integration/CanyonFireflyGuidanceRuntimeTests.cs
A8D415F1CF90B2C73E96184050BAFDEAF7549AF203E594939F875B2CB9507109  tests/integration/PrologueGuidanceRuntimeTests.cs
EDAEF070B8C54910C799CE920653DCC40E75E76051BC15C34CAA091839A13981  tests/RunContinuousCanyonTests.cs
```

The live-card mutation changed the worktree SHA-256 to `32EE77AEBFC6410A6DF2694B7348FB976B3BEEA23279DED2DE7AB01DD9DBC168`. After restoration the worktree SHA-256 is `3F4F5EC9A58C41CB61DA626366BD927ACDE843FA8F7ED6CAB8EC086474BED613`; its Git-normalized blob is `c25b9157e5d3f1b289155ef8ab9fb13f8b5e400e`, exactly equal to `HEAD:scripts/ui/WorldTargetMarkerVisual.cs`, and the file has no final diff. No cache was patched.

Fix-round changed paths are only:

- `scripts/prologue/guidance/CanyonFireflyGuidanceController.cs`
- `tests/integration/CanyonFireflyGuidanceRuntimeTests.cs`
- `tests/integration/PrologueGuidanceRuntimeTests.cs`
- `tests/RunContinuousCanyonTests.cs`
- `.superpowers/sdd/2026-09-09-continuous-canyon-world/task-8-report.md`

Self-review checked paused-state precedence over retained mechanism frames, guidance-off cleanup during pause, non-paused lifecycle cleanup, physical-footprint locality for all three mechanism implementations including rotated pendulum geometry, unchanged encounter/Boss adapter scoping, the 15 retained case identities, focused flags, no-argument single execution, source-mutation restoration and diagnostic counts. `git diff --check` is clean apart from line-ending notices. No reviewer/helper subagent was used. There are no known remaining in-scope fix-round items; independent review remains with root.
