# Task 9 Report: Replaceable Canyon Art Layers and Traversal Readability

Date: 2026-09-21

Baseline: `02d9552590c4e38760dcd4db221607c0b47ef9ea`

Branch: `codex/task6-certified-final-20260902T0555`

## Outcome

Task 9 establishes replaceable environment-presentation layers for all five canyon sectors and improves the project-native mechanical-canyon / ruined-temple staging without changing traversal authority. Background, midground, gameplay-readable, foreground and effects presentation are independent of collision, grapple materials, mechanisms, route gates, encounters and saves. The generated leaf scenes carry the same separation.

The visible pass adds continuous low-contrast canyon depth, region-specific platform treatment, readable support/end-cap structure and twelve rendered landmark compositions spanning entrance, descent, basin, ascent and temple height bands. The closed upper-basin shortcut remains the canonical `UpperBasinShortcutClosed` `Polygon2D`, with the same world bounds `(3840, 1520, 410, 1930)`, state binding and collision; only visual children changed. Steam roots likewise retain their authored world bounds and phase authority.

This is a materially improved, replaceable native staging pass, not commissioned/final environment illustration and not full-game visual acceptance. The corrected normal-window movie resolves the reviewed hard sector scenery cutoffs, dominant flat brown shutter and unarticulated platform ends while preserving player, support and grapple-line clarity. Repeated/flat giant-bell treatment, close regional light colors, actor/enemy animation, UI-card occlusion and target/icon semantics remain Task 11 whole-frame polish work.

## Implemented Boundaries and Runtime Wiring

- `CanyonContentBuilder` creates and populates five independent presentation subtrees after gameplay construction. It assigns sector identity to presentation renderers while leaving gameplay nodes authoritative.
- `CanyonBackdropVisual` draws a common opaque skirt at absolute `Z = -100`, with a conservative design envelope derived from the supported 1920x1080 physical viewport, logical 1280x720 canvas, camera focus/look-ahead and retirement/seam probes. All sectors share the same base, preventing opaque palette rectangles at residency boundaries.
- Distant canyon forms are world-aligned and overlap continuously across sector scene bounds. Regional identity sits in low-contrast middle-distance structure and light, rather than in five discontinuous base fills.
- `RoomVisualCatalog` binds twelve distinct composition recipes to rendered landmark geometry across three height bands: broken entrance bell, belfry ribs, abyss crane, archive vault, ice choir, gear crown, steam pump, moving lift, pendulums, upper bell frame, sealed forecourt and fallen-bell temple.
- Landmark bells use back/cast-shadow planes, facing planes, highlights, rims and clappers. Mechanical landmarks use recessed/facing panels, braces, rails, bolts, contact shadows and spoked wheels rather than single flat blocks.
- `CanyonPlatformVisual` keeps material-family readability while adding restrained sector fills, top-edge contrast, end caps, supports and structural details. Grip, temple, ice, forbidden and dedicated families remain distinguishable and gameplay material remains authoritative.
- `RoomFormalVisual` exposes the shared foreground safety contract. Foreground rendering is checked against player/support/next-target visibility rather than assumed safe from node order alone.
- Five `.tscn` leaf sectors were regenerated only through the existing builder. No parent scene was refreshed and no extra production-file ownership was needed.

## Existing Art and Explicit Limits

The bounded prior inventory inspected the actual pixels in all ten existing PNGs. The environment files are swatches/blocks, the player and enemy/VFX sheets are simple component silhouettes, and the richest reusable image is a 96x96 lantern with four-ring VFX limits. A manifest `final: true` flag is metadata, not visual quality.

This pass therefore reused project-native palette/material semantics, existing runtime bindings and presentation renderers. It did not stretch swatches into fake stone, duplicate atlas metadata, import new pixels, download assets, call paid generation or fabricate procedural textures as final art. The 96px lantern was not forced into unrelated large-scale scenery. Original illustrated canyon surfaces, richer bell/mechanism models, regional lighting differentiation and full actor/UI cohesion remain explicitly unavailable/pending.

## TDD and Failure Evidence

The implementation followed compile-safe assertion work, fresh build, behavioral RED, implementation GREEN, generated-leaf checks, mounted runtime checks and art-removal verification.

### Behavioral REDs

- `task9-readability-red-20260921T034712676-097104...`: build exited 0 with 0 warnings/0 errors; Godot PID 52572 exited 1 on the intended missing coverage-skirt assertion. Historical `BuildPid=36604` is the PowerShell invocation host, not a verified `dotnet` child PID; the original record is preserved and is not presented as exact compiler PID evidence.
- `task9-formal-boundary-red2-...`: build PID 37068 exited 0; Godot PID 34436 exited 1 on the intended missing shared foreground-safety contract. A preceding compile attempt (PID 27484, exit 1) exposed a C# keyword mistake and correctly did not launch the engine.
- `task9-local-art-transform-red-...e7aa...`: build PID 21056 exited 0; Godot PID 35176 exited 1 because composition IDs were not yet bound to correctly transformed, rendered sector-local geometry. This prevents metadata-only “variety” from satisfying the visual boundary.
- `task9-visual-corrections-builder-...`: build PID 55816 exited 1 on an operator/switch syntax error; no engine launch followed. The corrected focused run is listed below.

### Focused and Generated GREEN

- Structural readability: `task9-readability-green-clean-20260921T035443481-be44a56a15de4ee693cbbf3ebf519205`, build PID 14716/0 -> Godot PID 76900/0, clean logs; 12 compositions, 121 platform visuals and 4 target witnesses.
- Local rendered-transform contract: `task9-local-art-transform-green-...705d...`, build PID 73976/0 -> Godot PID 47652/0, clean. Its environment record used `PROCESSOR_COUNT=2` rather than the required `DOTNET_PROCESSOR_COUNT=2`; later authoritative cases use the correct variable and this provenance defect is not hidden.
- Corrected visual structure: `task9-visual-corrections-green2-...`, build PID 47144/0 -> Godot PID 41800/0, clean.
- Final generated leaves, each with a fresh successful build and clean builder engine run:
  - Entrance `task9-final-regenerate-entrance-...348a...`: 72020/0 -> 34648/0, 103 nodes.
  - Descent `task9-final-regenerate-descent-...189ce...`: 72584/0 -> 5004/0, 210 nodes.
  - Basin `task9-final-regenerate-basin-...3feb...`: 75464/0 -> 68052/0, 245 nodes.
  - Ascent `task9-final-regenerate-ascent-...a471...`: 47668/0 -> 26812/0, 407 nodes.
  - Lost Temple `task9-final-regenerate-temple-...10f6...`: 54664/0 -> 41508/0, 136 nodes.

### Runtime, Cleanup and Resolution Evidence

- An early mounted headless case, `task9-mounted-build-...837a...`, build PID 51604/0 -> Godot PID 28740, was manually terminated with exit -1 after about 121 seconds while waiting on `RenderingServer.FramePostDraw` in headless mode. The render-only wait was replaced with a process-frame path for structural headless checks; the failed evidence remains preserved.
- `task9-mounted-runtime-red-...793f...`, build PID 75464/0 -> Godot PID 14764/0, passed runtime assertions but failed shutdown hygiene: stderr records two `ObjectDB` instances and one resource still in use. It is not counted as a clean GREEN. Fixture cleanup was corrected rather than suppressed.
- A headless `--resolution 1920` case remained the project-authored logical 1280x720 canvas and was not counted as physical-1920 proof. A later headless physical-window assertion correctly failed at `(0,0)`. The project canvas/stretch settings were preserved.
- Final mounted 1280: `task9-final-mounted-1280-20260921T043156789-4d26...`, build PID 45524/0 -> Godot PID 73836/0, clean. Five normal PrologueLevel probes use the actual inverse canvas/world transform and assert actor, support and next-target visibility, seam/residency coverage and retired-count zero.
- Final rendered 1920: `task9-final-mounted-1920-20260921T043220390-c7c7...`, build PID 40684/0 -> Godot PID 71556/0, clean OpenGL run at physical 1920x1080 with the authored logical 1280x720 canvas. Assertions apply the final canvas transform instead of redefining project resolution.
- Final packed/art-removal: `task9-final-packed-green-20260921T043246267-00e873...`, build PID 62196/0 -> Godot PID 14816/0, clean; 12 compositions, 12 rendered silhouette witnesses, 3 height bands, 121 platform visuals and 4 target witnesses. Hiding/removing presentation layers preserves the collision/material/route/telegraph/activation fingerprint. An earlier packed run's 15 owner-inconsistent reattachment warnings were fixed by freeing detached fixture layers; they are preserved as historical evidence, not accepted output.
- Final default suite: `task9-final-default-green-20260921T043309408-ed30974199c64fcb99d7c41730c22ff5`, build PID 16308/0 with 0 warnings/0 errors -> Godot PID 76772/0 with `CONTINUOUS_CANYON_TESTS_OK`, exactly 8 established negative-case warnings and 0 errors. Task 9 added no warning suppression or new warning debt; the accepted eight-case warning hygiene remains Task 10.

## Normal-Window Visual Review

The existing Task 6 baseline and matched before frames remain the “before” reference; they are a prerequisite-injected traversal, not full combat or final art approval.

The first Task 9 movie (`task9-rendered-main-route-after-...57b...`, 1718 frames/57.27 seconds, SHA-256 `70E988F6AFB9302C84C508DEE50C3847EF53E41D2D009FA43946D52547D88931`) established improved coverage and new landmarks, but root review of twelve after frames and four same-position before frames rejected it for hard distant-scenery cutoffs, flat giant bells/ochre mechanisms, a dominant brown shutter and repeated platform vocabulary. Those defects drove a second visual iteration.

The first corrected recording attempt (`task9-rendered-main-route-final-...3650...`) built successfully (PID 58396/0) but Godot PID 29072 exited 1 after an actual-input traversal failure at `AscentGrip8` on frame 1537. Presentation changes did not alter physics; a fresh headless route recheck (`task9-main-route-recheck-...c7e9...`, 72716/0 -> 3436/0) passed. The recording failure remains an unresolved reliability gap: the passing recheck neither identifies its cause nor erases it.

The authoritative corrected movie is:

`E:/OurGame/Knight/.evidence/september09-continuous-canyon/task9-rendered-main-route-final2-20260921T043001551-c9be1c2fc25442f9a83e0e9b349840c8`

- Fresh build PID 60700/0 -> Godot PID 28544/0.
- 1718 frames, 1280x720 at 30 fps, 57.27 seconds, actual-input route through the Lost Temple pre-boss checkpoint.
- Movie SHA-256: `AC69670A246499CF419B9AA2D70B9F58EBEC85C279D8A9F89A7455F5E2DEA8BD`.
- Full FFmpeg decode succeeded. Twelve native representative frames plus the contact frame were opened and reviewed independently by root. Root did not perform a human-speed full-motion or audio-quality review.
- Representative-frame finding: the principal rectangular distant-scenery discontinuities, giant brown shutter block and missing platform end treatment are materially improved. The sampled frames preserve player silhouette, landing-edge and cyan grapple-line readability; the successful actual-input route proves traversal of this capture, not subjective motion polish.
- Remaining limit: large bells are still comparatively flat/repeated and region lighting remains close in hue. The tutorial card's lower occlusion and gold wheel/target semantics require UI and live-mechanism context, so they were not “fixed” by dimming/removing target signals in this environment-only task.

This recording is representative normal-window evidence for the scoped environment pass. It does not certify full combat, boss-to-safe-exit, actor animation, UI polish or final user art approval.

## Test Coverage Added

`ContinuousCanyonReadabilityTests` is registered in the default runner and also exposes focused `--task9-readability-only`, `--task9-packed-only`, `--task9-mounted-only` and `--task9-mounted-1920` paths. Coverage includes:

- common skirt and viewport envelope across five sectors;
- actual rendered landmark/geometry binding, twelve composition witnesses and three height bands;
- sector-local and world-transform correctness;
- mounted inverse-canvas/final-canvas checks at logical 1280 and physical 1920;
- actor, safe support and next-target foreground-occlusion checks;
- material-family and sector platform readability;
- exact protected shutter and steam root boundaries;
- presentation hiding/removal while gameplay fingerprints remain unchanged;
- clean generated-leaf packing and normal residency/retirement behavior.

## Hash Inventory

SHA-256 values are raw file bytes. `NEW` means the file did not exist at the baseline.

| Owned file | Baseline | Final |
|---|---|---|
| `tools/build/prologue/CanyonContentBuilder.cs` | `4169CAF9B3845E5E768D547ABDC4D7825E8E6D60DBD57BE30FF08BB68E947C14` | `CACEA69ACC3DC004C3534638EC1D255C9E8BDA0DB3B6F609A170D2DB02BF31B8` |
| `scripts/presentation/CanyonBackdropVisual.cs` | `432605A9932A87AA748CD64404D1A9FFA9DE667BC6D1F8A1A2FBD0D3CE0D901F` | `78F935BFB0CF3C49A4B3CD89ED0C0F4CD71537FE66386F22D6FFA8FA183F7EAA` |
| `scripts/presentation/CanyonPlatformVisual.cs` | `7ED0634F716BBE0E2B93F6CBA47010A511CB2851CA35349DB2C833619D9191DD` | `62A25F98243B3D24B056750636F5622073A0CE27BDA87D3CD12DA9FDAF698BDF` |
| `scripts/presentation/RoomFormalVisual.cs` | `6A9D8FBDCD99EB5955C7BCAE298DD5FE1A0B4DEF55727429C9A23CCE164ACE7B` | `6118249429EE2733AE8FD1FD72A1192AFCE11E663B863074231D9AC5A88BF793` |
| `scripts/presentation/RoomVisualCatalog.cs` | `978066F3E78A2CBC7E060305893467D056C5844A77FC47F79A32FC716D75912A` | `3A0F96142685D1D96240D100E304DB7750F24D86F294B90E6F02B1580D010024` |
| `tests/unit/CanyonMaterialSkinTests.cs` | `FE237B199C986A779B9A239A752A1473BF8C7A47699A62291FAD665247C74C7B` | `24F576F94C26824E4B9A3113FF6A3EE0F86484709ECB792070099A592F4FD848` |
| `tests/integration/ContinuousCanyonReadabilityTests.cs` | `NEW` | `30201FB4FFB29BA5781520B76513DC5151FCA40433F9FFFF818ED6788B7C0247` |
| `tests/RunContinuousCanyonTests.cs` | `EDAEF070B8C54910C799CE920653DCC40E75E76051BC15C34CAA091839A13981` | `1F85C69EFA9BE9E97C632E287E9FE500100BBEA6E671D360CDCF83C70474EB80` |
| `scenes/prologue/sectors/CanyonEntrance.tscn` | `D005B3A07E6B09190C054447FE730DA8F9D01316AB3EE41AD26028F97BFFD915` | `68F6DF175EEEEB40F3B797B8E877D21C773F60F0B7747CE6E16F1D9AF4FD4CDA` |
| `scenes/prologue/sectors/CanyonDescent.tscn` | `3695D59E86EC70F36EAE8B28ACAB210672BCD10106F7474A9EC40C0EB9E8ECA0` | `754C5D6510643E5C3CC0CA718CCADED8C613B5F0D25B4BF551580222EEEF7546` |
| `scenes/prologue/sectors/CanyonBasin.tscn` | `D400758CE5B0E4E8F5358FEB15D0734EA1CB2C1BCFCDCE011093FC54BD629576` | `C69EE7A2E73E07ED3285176C19D8B95BD18F576F121A2C7453A073A5AE6CE649` |
| `scenes/prologue/sectors/CanyonAscent.tscn` | `837E61406DD418AEF06118BC229B9B0F73DB88F9A264DF0F3373D70BC2284981` | `D7704FB552208C4F33BAA7918919E90AFAAE42622FF55E07F5D10E4707EE2133` |
| `scenes/prologue/sectors/LostTemple.tscn` | `A0E922760749F3D60718001BB9B425651F3484BBE465FD31D99B95261D0E98B5` | `33C5FC1491C404C7ED5B5DD70D69DF68D0FDA726FC7740AFCBA40B6F7FEBD95D` |

The report itself is the fourteenth owned commit path and is excluded from its own pre-write hash inventory.

## Self-Review and Scope Audit

- No camera, accepted capsule/geometry, collision, grapple-material authority, save schema, encounter, mechanism authority or gate was changed to conceal an appearance defect.
- Protected shortcut and steam roots retain exact names/types/bounds; visual articulation is child-only.
- Generated scenes came from the existing builder after fresh builds; no hand-edited generated scene content is relied upon.
- Tests distinguish metadata from rendered geometry and static algebra from mounted normal transforms.
- Headless logical-canvas and rendered physical-window assertions are reported separately.
- Failed builds, timeout, shutdown-hygiene failure, one-off route failure and incomplete early PID metadata are retained rather than rewritten as successes.
- No warnings were suppressed. The final focused, packed, mounted and builder outputs are clean; the default suite's exact eight warnings are accepted pre-existing negative cases.
- Only the thirteen assigned source/test/generated files plus this report are intended for the Task 9 commit. README/plan/ledger, imports, audio layout, Task 8 files, accidental directories and all unrelated untracked files remain untouched and unstaged.

## Remaining Work / Handoff

- Task 10/11 must cover full normal-input combat and boss-to-safe-exit behavior, warning-hygiene cleanup, actor/enemy animation, UI typography/icon/motion, card occlusion and target/mechanism semantic review.
- Final-art work still needs deliberate illustration/asset production for richer bells, surfaces, regional light identity and whole-screen cohesion. No such paid or new-asset work was authorized here.
- The final2 movie and matched before evidence should remain attached to future visual review; structural counts and test success alone are not visual acceptance.

---

# Task 9 Fix Round 1 — First-Seam Foreground and Live Art-Replacement Witness

Fix base: `89706a453b0779f8f5445a0e65a502dfdadd8d81`.

## Review Finding and Minimal Correction

Independent review found that the generic upper-corner foreground pair crossed the unchanged first-seam traversal envelope: Entrance `NearRight` covered the launch region around `(1220,64)`, while Descent `NearLeft` covered the arrival region around `(1360,64)`. The production correction omits only those two decorative polygons and retains the opposite near-depth shoulder in each sector. Gameplay geometry, camera behavior, residency, collision and material authority are unchanged. Only Entrance and Descent were regenerated, through their existing builders.

The mounted fixture now places the real player at both first-seam positions, proves both sectors are resident, converts every resident foreground polygon through the actual screen transform and rejects any intersection with the live actor screen bounds. This is a two-sided seam witness, not a metadata or node-count proxy.

The art-replacement assertion was also strengthened from fingerprints alone to a bounded live production witness. With `Background`, `Midground` and `Foreground` physically detached and replaced, the fixture preserves the original `Gameplay`, `GameplayReadable` and actual piston telegraph nodes, enters the authored `GearPistonsEncounter` activation bounds, observes the production encounter activate and spawn exactly one `IEncounterActor`, and verifies the piston telegraph remains live and bound. It then exits the arena and waits for actor retirement before teardown. This does not broaden the test into full combat or Task 10 route coverage.

## TDD and Generated Evidence

- Behavioral seam RED: `task9-fix1-seam-red-20260921T050158462-b6bf1119793748ba8abdf3e553a58a36`, build PID `34456`/0 with 0 warnings/0 errors -> Godot PID `43420`/1. The intended failure was `entrance-launch/CanyonEntrance/NearRight foreground misses the live actor envelope`. Its shutdown also reported three `ObjectDB` instances and one resource still in use, so this RED is not presented as clean.
- Regenerated Entrance: `task9-fix1-regenerate-entrance-20260921T050333981-0a790c1f9f024cd88b55b46961587395`, build `73592`/0 -> builder `74160`/0, clean, 102 nodes.
- Regenerated Descent: `task9-fix1-regenerate-descent-20260921T050346570-8c92216c50bb4bc2b1b48ff025595add`, build `51916`/0 -> builder `47244`/0, clean, 209 nodes.
- Focused readability/material structure: `task9-fix1-builder-green-20260921T050502261-98f22c4ffd314152a524ebc88256b563`, build `62300`/0 -> Godot `74404`/0, clean; all 12 compositions and 12 rendered silhouettes, three height layers, 121 platform visuals and four grapple-material targets remained covered.
- Packed leaves/material structure: `task9-fix1-packed-green-20260921T050515120-5938e904651a48ce9fa5ec79d14aa384`, build `39032`/0 -> Godot `71252`/0, clean, with the same readability and material sentinels.

All authoritative cases used `G:/godot/dotnet-sdk/dotnet.exe build Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo` with `DOTNET_ROOT`, `DOTNET_ROOT_X64`, `DOTNET_MULTILEVEL_LOOKUP=0`, `DOTNET_ROLL_FORWARD=LatestMajor` and `DOTNET_PROCESSOR_COUNT=2`, immediately before the recorded `G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe` invocation. Per-case `source.json`, exact argv/environment, child PIDs, timestamps, exits and full stdout/stderr are preserved in the external evidence directories.

## Mounted Runtime and Cleanup

- The first assertions-passing live-witness run, `task9-fix1-mounted-1280-final-20260921T050542073-f808ef6f4b2c4023a38ad96bf433952d`, built `46260`/0 and ran Godot `26672`/0, but shutdown reported seven `ObjectDB` objects and three resources still in use. It is an assertions-pass/shutdown-fail record, not a clean GREEN.
- Verbose case `task9-fix1-mounted-cleanup-diagnostic-20260921T050613624-9949cd5528314297ac7159726df40299`, build `71288`/0 -> Godot `40872`/0, identified retained `AudioStreamWAV`/`PlaybackWAV` instances after the live actor was left active through parent teardown. The fixture was corrected to activate away from the authored spawn, leave the real arena and verify actor retirement before freeing detached layers. No warning suppression or production cleanup change was added.
- Clean mounted 1280: `task9-fix1-mounted-1280-clean-20260921T050711594-29635ae5bad64905b7d1f416abe44764`, build `55260`/0 -> Godot `75008`/0, empty stderr. It emitted `TASK9_FIX1_FIRST_SEAM_FOREGROUND_OK probes=2 residents=Entrance,Descent`, `TASK9_FIX1_ART_REPLACEMENT_ENCOUNTER_OK ... actors=1`, and the existing five-probe mounted sentinel.
- Clean rendered physical 1920x1080/logical 1280x720: `task9-fix1-mounted-1920-clean-20260921T050748975-65fb0595468340fea42d657e55ab6170`, build `76208`/0 -> Godot `74448`/0, empty stderr, with both new sentinels and the actual final-canvas transform path.

## Actual-Input Seam Evidence

Evidence directory:

`E:/OurGame/Knight/.evidence/september09-continuous-canyon/task9-fix1-first-seam-movie-20260921T050824705-7a56a7a59991497490bf7c82c3923c95`

- Fresh build PID `50952`/0 with 0 warnings/0 errors -> Vulkan/mobile Godot PID `62084`/0, clean engine stderr.
- `task9-fix1-route.avi`: 1718 frames at 1280x720/30 fps, 57.27 seconds, SHA-256 `C4BCB86DF78DB462CC3CB150C3EE04109C4B610E89B1A134F0CD9D87FD05381B`. This actual-input run reached the Lost Temple pre-boss checkpoint; it is a passing rerun and does not resolve or explain the separate `AscentGrip8` reliability gap.
- `task9-fix1-seam-4-6.5.avi`: 75 frames/2.50 seconds, SHA-256 `117E3A736DC9E3D29DFF61B0143D3CF749B50E35AAE78E75F9E1506881289FC2`; FFmpeg PID `72860` exited 0 and a full decode by PID `73380` exited 0 with empty error output.
- Native 4.5, 5.0, 5.5 and 6.0 second frames are retained in `seam-frames/`. Agent inspection found the player head, lantern, torso and blade clear of the former dark upper-corner shapes while the opposite foreground shoulders remain. This is scoped first-seam evidence pending independent review, not full-motion/audio, UI, whole-route art or final-polish certification.

## Fix-Round Hashes and Scope

SHA-256 values are raw file bytes; the baseline column hashes the Git blob at the fix base, while the final column hashes the working-tree file before this appendix/commit.

| Owned file | Fix base | Fix round final |
|---|---|---|
| `tools/build/prologue/CanyonContentBuilder.cs` | `1F6146C4930BCF439590752585D3B3E89004CABAE5AAD7A8E8A1A74695154880` | `0247B570110E72356F4604CC9D7B828C8E48A6791BAEADD8570EA7209C47199E` |
| `tests/integration/ContinuousCanyonReadabilityTests.cs` | `30201FB4FFB29BA5781520B76513DC5151FCA40433F9FFFF818ED6788B7C0247` | `CE7150B67E0F9011705B2085DC8727DCC2F1000B16A3ABE5B40021E0ADD13776` |
| `scenes/prologue/sectors/CanyonEntrance.tscn` | `68F6DF175EEEEB40F3B797B8E877D21C773F60F0B7747CE6E16F1D9AF4FD4CDA` | `BE3448AD8FD3669D3E18D46F0973C39FFE60CD9C007623AA88EDE8F2E055F549` |
| `scenes/prologue/sectors/CanyonDescent.tscn` | `754C5D6510643E5C3CC0CA718CCADED8C613B5F0D25B4BF551580222EEEF7546` | `EA81E315D290A7DBCD0E521F4D432046EDF4D0E80A26DEF05BF4247E5E54C54B` |

No source outside the original Task 9 ownership changed. The fix commit is intended to contain only these four paths plus this appended report. Unrelated README/plan/ledger, import, audio-layout, camera-test, `RunPrologueTests`, accidental directory and untracked changes remain untouched and unstaged. No default six-minute suite was rerun for this bounded fix; root owns dependent validation after re-review as requested.
