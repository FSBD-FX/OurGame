# 2026.09.08-r2 — canyon material polish

Status: development playtest candidate, not final art or human feel acceptance.
Base revision: `719af81d9f7880a0035976427e3957b0270e9510` (r1).

## Implemented scope

- Replaced 10 flat point-target discs with material-specific silhouettes: cracked grippable
  stone, riveted temple plate, brass dedicated clasp, icy facets and a slashed forbidden seal.
- Skinned 52 platforms/walls with restrained stone courses, metal panels, ice facets and
  internal shadow. Safe return shelves retain quiet paired brackets. Readable support edges
  remain exactly aligned to physical top/left/right bounds; no drawing bridges actual gaps.
- Kept the five `VisualRoot` layers, target/platform names and separate `ReadableEdge` child.
  `SurfaceMaterial`, `SkinSize`, `CatchShelf` and `Radius` expose the replacement contract.
  Neither renderer owns physics, input, particles, processing callbacks or a redraw loop.
- Changed no movement, grapple, jump, shield, checkpoint, encounter or main-route tuning.
  Repacked five sectors and main through their existing C# builders.
- Used only local project-authored drawing. No paid generation, downloads or new asset licence.

## Verification and proof

Evidence root: `E:/OurGame/Knight/.evidence/september08-r2-material-proof/`.
The before-state and read-only extractor are in the adjacent
`september08-visual-polish-baseline/` directory.

| Check | Result / evidence |
| --- | --- |
| Test-first replacement contract | `september08-material-skin-red.log` failed on the old StartFloor lacking material-specific skin data; builder then passed in `september08-material-skin-builder-green.log` |
| Packed production scene contract | `packed-skins-final.log`: 52 platforms / 10 targets; exact material, center, size, radius, shelf identity, edge bounds, zero visual physics, unrotated/unscaled geometry |
| Serialized gameplay invariant | All five normalized Gameplay SHA256 hashes exactly match the r1 baseline; includes dimensions, positions, materials, layers/masks, one-way settings and scene references. Instanced actor internals are not expanded by this extractor; their sources are unchanged |
| Live renderer pixel check | `material-pixels-final.log`: five distinct occupancy silhouettes, four terrain samples bounded to physical rectangles, multiple rendered material tones. Targets normalized to radius 24 for the comparison board, not a claim that all production targets have that radius |
| UI / replacement / audio / prompt regression | `task10-final.log`: exit 0, `TASK10_FOCUSED_OK`; includes four HUD resolutions, guidance protection, two languages, replacement boundaries and live cue/VFX wiring. One existing deliberately mismatched guidance-target fixture warning |
| Import / build | `import.log`: exit 0, no errors/warnings; latest .NET build completed with zero warnings/errors |
| Full post regression | `post-regression.log`: exit 0, `PROLOGUE_TESTS_OK`, new skin contract, 11 traversal segments sampled three ways, sector streaming/recovery and 75 registered legacy replacement cases. Zero errors; three existing deliberate fault/recovery warnings |
| Full running route proof | `full-flow.avi`, `full-flow.log`, `first-level-material-review.mp4`: exit 0, 4114 frames, 30 named stages, six reticle states, four Boss attacks, two checkpoints and completion/title return. Zero errors; five missing-guidance-target warnings occur only in explicit post-proof reposition scenes |
| Visible inspection | `keyframes/`: inspected descent, ascent grapple, temple approach and Boss sweep at 1280×720; terrain edges, dedicated clasps, tether, player and attack silhouettes remain separate. The normalized material board was also viewed |
| Read-only code review | No critical/important findings. Fixed both minor findings: normalized-size board label and regression assertions for edge width/rotation. Reviewer confirmed no current authored platform overdraw and unchanged five-sector Gameplay hashes |

Commands use the installed Godot 4.7.1 Mono and .NET SDK 9.0.317. Post regression uses
`--headless --fixed-fps 60`; video uses Windows GL Compatibility with `--fixed-fps 60`.
Fixed-step correctness and movie-render statistics do **not** establish normal-speed 60 FPS.
The route is automated input; final Boss completion setup and separately labelled post-proof
close-ups are test fixtures, not a human completion or difficulty assessment.

The new tests run with:

```text
Godot_console --headless --path . --script res://tests/RunCanyonMaterialSkinTests.cs
Godot_console --headless --path . --script res://tests/RunCanyonMaterialSkinTests.cs -- --builder
Godot_console --path . --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/presentation/CanyonMaterialSkinCapture.cs -- --output=<absolute.png>
Godot_console --headless --fixed-fps 60 --path . --script res://tests/RunPrologueTests.cs -- --phase=post
```

## Delivery / remaining work

- Local playable entry remains `Play-Knight.cmd`; workspace entry remains
  `F:/Documents/游戏/Play-Knight-2026-09-08.cmd`.
- Versioned source archive: `E:/OurGame/Knight/releases/2026-09-08/Knight-Prologue-2026.09.08-r2-source.zip`.
  This is a source playtest package requiring Godot Mono/.NET, not a standalone exported EXE.
  The r1 archive remains untouched; archive hash and extraction check live in the r2 receipt.
- Scene art still has an intentionally simplified procedural appearance. Additional composition,
  enemy/character presentation, real-time frame pacing and first-time playability remain open.
  The earlier occasional ascent-one scripted-input failure has not been assigned a proven cause;
  this run succeeded, but no unrelated visual change is claimed to fix that intermittent issue.
- Continue the active refinement goal. Do not substitute these automated checks for user
  acceptance, official final art, or the outstanding full independent certification passes.
