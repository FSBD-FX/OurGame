# Task 2 report — AC medium-corruption enemy and Boss art pass

## Scope and result

- Rebuilt the three project-owned ordinary-enemy atlases and the Fallen Bellkeeper atlas with broad silhouettes, sparse corrosion, and no generated/pay-per-use art.
- Added the stable ordinary-enemy top-level roles `Body`, `Weapon`, `Corruption`, `Accessory`, and `Effects` without changing gameplay, collision, attack timing, warning colors, or actor production paths.
- Retained the Boss five-part contract (`Body`, `ArmorShell`, `Hammer`, `Chain`, `Core`) and added deterministic source-region, pivot, semantic-layer, draw-bounds, profile, facing, and foot-contact metadata.
- Preserved the legacy runtime-controlled parts (`Shell`, `Wings`, `Lamp`, `Bell`, `Feet`) so existing typed motion continues to drive the same attacks.

## TDD evidence

- RED: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-red.log`
  - Exit `1`.
  - Named missing-contract failure: `RustShellFormalVisual/Body publishes its deterministic semantic layer`.
  - This was captured before production-builder changes.
- GREEN: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-green.log`
  - Exit `0`, exactly one `PROLOGUE_TESTS_OK` after the implementation correction.
- Named weapon-occlusion mutation: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-weapon-occlusion-mutation.log`
  - Moved Rust corruption and its declared pivot from `(-9,-3)` to `(35,-3)`, overlapping the weapon-side box.
  - Exit `1`; exactly one named failure: `RustShellFormalVisual medium corruption stays on the back/non-weapon outline`; no `PROLOGUE_TESTS_OK`.
  - Restored by rerunning `BuildEnemyFormalVisuals.cs`, then reran the final gate.

## Fresh final gates

- `dotnet build --no-restore`: exit `0`, `0` warnings, `0` errors.
- `RunPrologueTests.cs`: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-final-full.log`
  - Exit `0`.
  - `PROLOGUE_TESTS_OK` count: `1`.
  - Real `ERROR:` count: `0`.
  - Managed exception count: `0`.
- Deterministic running-game proof: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-proof.log`
  - 2869 frames, 1280×720, 60 FPS, complete prologue route.
  - `PROLOGUE_PRESENTATION_COMPLETE frame=2869` count: `1`; `ERROR:` count: `0`.
  - Attack evidence sequences are complete for Rust Shell, Grave-Lamp Moth, Bell-Back Laborer, and all four Boss attacks.

## Gameplay-scale visual review

- No-active-warning frames: `screenshots/task2-enemy-silhouettes/warning-off/`
  - `rust-shell-idle.png`
  - `grave-lamp-moth-idle.png`
  - `bell-back-laborer-idle.png`
  - `fallen-bellkeeper-idle.png`
- Active-danger-warning frames: `screenshots/task2-enemy-silhouettes/warning-on/`
  - `rust-shell-danger.png`
  - `grave-lamp-moth-danger.png`
  - `bell-back-laborer-danger.png`
  - `fallen-bellkeeper-danger.png`
- All eight review frames are 1280×720. The idle frames remain distinguishable by low shell/needle, broad moth wings/lamp, heavy back bell/hammer, and asymmetric five-part Boss. In the active frames, danger shapes remain the strongest colored information and the blue-green corruption stays behind the weapon-side profile.

## Owned implementation and test files

- `tools/build/art/BuildEnemyFormalVisuals.cs`
- `tools/build/art/BuildFallenBellkeeperFormalVisual.cs`
- `assets/enemies/formal/enemy-atlas.png`
- `assets/enemies/formal/rust-shell.png`
- `assets/enemies/formal/grave-lamp-moth.png`
- `assets/enemies/formal/bell-back-laborer.png`
- `assets/boss/fallen-bellkeeper/fallen-bellkeeper.png`
- `scenes/presentation/RustShellFormalVisual.tscn`
- `scenes/presentation/GraveLampMothFormalVisual.tscn`
- `scenes/presentation/BellBackLaborerFormalVisual.tscn`
- `scenes/presentation/FallenBellkeeperFormalVisual.tscn`
- `tests/integration/EnemyFormalVisualTests.cs`
- `tests/integration/FallenBellkeeperFormalVisualTests.cs`
- `tests/unit/ProductionAssetManifestTests.cs`

## Concerns

- None within Task 2 scope. The capture still shows the intentionally existing large first-level guidance panels; their compaction belongs to Task 3 and was not changed here.

## Independent-review fix round 1

### Corrected semantic atlas contract

- Replaced the holistic ordinary-enemy and Boss sheets with non-overlapping semantic crops. Each declared `atlas_source` now contains only the named role's authored pixels and has a transparent isolation gutter.
- Added `atlas_origin` for deterministic crop alignment and measured `atlas_content` directly from alpha pixels during each builder run. The integration tests independently reload the saved PNGs, recompute alpha bounds, require the expected role-specific value-group color, reconstruct local bounds around the atlas origin, and reject any opaque pixel without exactly one semantic owner.
- Expanded the ordinary-enemy atlases to `256x160` and the combined sheet to `768x160` so Laborer effects and the other broad semantic shapes fit without overlap or clipping. The Boss remains `512x256` with five separated semantic regions.
- Corrected formerly estimated draw/profile bounds exposed by the pixel tests, including Rust feet, the Moth corruption-side placement, and the Boss body/hammer extents.

### Corrected action-pose ownership

- Removed the three static `Accessory/IdentityPlate` duplicate silhouettes and the Moth's static `Weapon/GraveLampLens`, which previously rendered above the typed moving parts.
- Stable `Accessory` and Moth `Weapon` roles now publish deterministic `motion_sources` paths to the real pose-controlled `Shell`, `Wings/FarWing`, `Wings/NearWing`, `Bell`, and `Lamp` nodes. The real typed renderers remain the only dominant visible silhouettes, so folded wings, bell counter-twist, and lamp motion cannot be masked by a static copy.
- Regression tests require every declared motion source to move in the typed telegraph and explicitly reject the old static duplicate nodes.

### Fix-round TDD and verification evidence

- Pixel RED: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix1-red.log`
  - Exit `1`; named failure: `RustShellFormalVisual/Body pixels have a transparent isolation gutter for safe replacement cropping`.
- Action-mask RED: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix1-action-red.log`
  - Exit `1`; named failure: `RustShellFormalVisual/Accessory has no static duplicate silhouette that can mask typed action motion`.
- Named occlusion mutation: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix1-weapon-occlusion-mutation.log`
  - Moved Rust corruption from `(-9,-3)` to `(35,-3)` while keeping pivot metadata synchronized.
  - Exit `1`, zero success markers; named failure: `RustShellFormalVisual medium corruption stays on the back/non-weapon outline`.
  - Restored deterministically with `BuildEnemyFormalVisuals.cs` before the final gate.
- Final `dotnet build --no-restore`: exit `0`, `0` warnings, `0` errors.
- Final full suite: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix1-final-full.log`
  - Exit `0`; `PROLOGUE_TESTS_OK=1`, real `ERROR:=0`, managed `Exception=0`.
- Fresh running-game proof: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix1-proof.log`
  - NVIDIA/OpenGL movie capture, 2869 frames at `1280x720`, one completion marker, zero engine errors.
  - Warning-off/on review: `screenshots/task2-enemy-silhouettes-fix1/warning-off/` and `warning-on/`.
  - Explicit action review: `screenshots/task2-enemy-silhouettes-fix1/action-poses/` shows Moth folded/dive poses, Laborer windup/sweep impact, and Boss sweep/ground-wave/hammer-throw attacks.

### Fix-round visual review

- Moth wings now visibly fold and rotate into the dive; no static open-wing polygon remains above them.
- Laborer's back bell counter-rotates with the windup while the hammer releases through a distinct sweep arc; the former static bell-shaped overlay is absent.
- Boss body, armor, chain, and hammer remain separated and visibly change across sweep, ground-wave, and hammer-throw poses.
- Danger-red remains stronger than the subordinate blue-green corruption. The existing large guidance panels remain a Task 3 concern and were not changed.

## Independent-review fix round 2

### One production-atlas authority

- The production manifest remains authoritative at `res://assets/enemies/formal/enemy-atlas.png`. Every ordinary-enemy stable role now publishes that exact `atlas_path` plus global composite coordinates: Rust begins at X `0`, Moth at X `256`, and Laborer at X `512`.
- `atlas_source`, `atlas_origin`, and measured `atlas_content` now share the same global coordinate system. The semantic integration test parses the enemies runtime path from `prologue-assets.json`, loads that production PNG, and verifies real alpha pixels, role-specific RGB signatures, isolation gutters, non-overlap, unique whole-atlas ownership, and reconstructed draw bounds. It no longer substitutes the per-enemy convenience PNGs for the production contract.

### One wing crop per motion source

- Moth `Accessory` is an aggregate stable role only. Its `Accessory/FarWing` and `Accessory/NearWing` semantic children own separate non-overlapping production-atlas crops, origins, alpha-content bounds, and exactly one `motion_source` each (`Wings/FarWing` and `Wings/NearWing`).
- Regression coverage requires both dedicated crops to contain authored production pixels and requires both mapped typed wing hinges to move independently with opposed rotations during the dive telegraph. No aggregate two-wing crop remains ambiguous.

### Fix-round TDD and verification evidence

- Production-atlas RED: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix2-production-atlas-red.log`
  - Exit `1`, zero success markers; named failure: `GraveLampMothFormalVisual/Body production-atlas crop owns its expected semantic value-group pixels`.
- Wing-map RED: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix2-moth-wing-map-red.log`
  - Exit `1`, zero success markers; named failure: `Moth Accessory/FarWing publishes its own semantic pixel-to-motion mapping`.
- Named occlusion mutation: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix2-weapon-occlusion-mutation.log`
  - Temporarily moved Rust corruption and synchronized pivot from `(-9,-3)` to `(35,-3)`.
  - Exit `1`, zero success markers; named failure: `RustShellFormalVisual medium corruption stays on the back/non-weapon outline`.
  - Restored deterministically with `BuildEnemyFormalVisuals.cs` before final verification.
- Final build: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix2-final-build.log` — exit `0`, `0` warnings, `0` errors.
- Final full suite: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix2-final-full.log` — exit `0`, exactly one `PROLOGUE_TESTS_OK`, zero real `ERROR:` lines, and zero managed exception failures.
  - A preceding attempt hit the existing guidance timing race (`MechanismBellChain` for one frame after shortcut opening); it is preserved as `task-2-fix2-final-full-attempt1-unrelated-guidance-flake.log`. No out-of-scope guidance code was changed, and the immediate fresh rerun above is clean.
- Fresh running-game proof: `.superpowers/sdd/2026-08-21-environment-ui-audio-polish/task-2-fix2-proof.log`
  - NVIDIA/OpenGL capture, 2869 PNG frames at `1280x720` and 60 FPS, exactly one completion marker, zero engine errors.
  - Moth action proof: `screenshots/task2-enemy-silhouettes-fix2/action-poses/`.
  - Rust/Laborer/Boss sanity proof: `screenshots/task2-enemy-silhouettes-fix2/sanity/`.

### Fix-round visual review

- Moth FarWing and NearWing remain visually distinct at gameplay scale, visibly close during the gold/danger windup, and rotate into the narrow dive silhouette without a static open-wing mask.
- Rust still exposes its thrust side; Laborer retains the bell counter-pose and readable hammer release; Boss sweep remains asymmetric and legible. No gameplay, collision, tuning, timing, warning-color, or encounter behavior changed.
