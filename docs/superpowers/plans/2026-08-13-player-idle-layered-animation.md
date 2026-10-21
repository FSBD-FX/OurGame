# Thread-Spool Bearer Layered Idle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce one free, deterministic 12-frame layered idle review sample that preserves rigid character volume, gives the mantle readable cloth inertia, and keeps the crown lantern separate.

**Architecture:** Repository-owned Python tools validate an external rig contract, prepare overlapping rigid plates and one continuous mantle mesh from the accepted RGBA reference, render a deterministic timeline, and reject bad evidence. All editable images and rendered review media remain in the external art workspace; only tools, tests, specifications, and attempt metadata are tracked. The result stops at a visual review gate and does not modify the runtime atlas, Godot scene, or gameplay.

**Tech Stack:** Python 3.14.6, Pillow 12.3.0, NumPy 2.5.2, `unittest`, PowerShell, existing accepted PNG references

## Global Constraints

- Work in `E:\OurGame\Knight\.worktrees\player-art-pipeline` on branch `codex/player-art-pipeline`.
- Canonical body input is `docs/art/player/reference/thread-spool-bearer-body.png`, SHA-256 `0358BFEA48D6FF79271A5B14AE03B0396361748E60EB2D9D36EF6A696D7B1B44`.
- Canonical lantern input is `assets/player/thread-spool-bearer/lantern.png`, SHA-256 `C313BFAAF1A1B8C9994AA6FEBC96D5A9E5C82F740B58305FF3C96696DCC73AB0`.
- Use `E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe`; add no package beyond Pillow and NumPy.
- External output root is `E:\OurGame\ArtProduction\thread-spool-bearer-v1\actions\idle-layered`.
- First attempt is exactly 12 frames at 8 FPS on a 256x256 transparent canvas with output origin `(128,231)`.
- Fixed boots remain on the baseline. Chassis plates may translate vertically at most 2 output pixels; every rigid plate has scale exactly `(1,1)`.
- The mantle is one connected texture with smoothly blended root, middle, and tip influences. Do not implement it as three visible fragments.
- Lantern and tether appear only in composite review frames; neither is baked into body frames.
- No host-native image generation, external image API, paid service, or included image quota is authorized.
- Stop before animation if the separation preview exposes unreconstructable holes, damaged identity markers, visible plate seams, or incorrect occlusion.
- Stop after review rendering until the user explicitly accepts the motion. Do not publish an atlas, manifest, `.tres`, scene, or runtime script in this plan.
- Preserve `action-idle-001` as rejected evidence; the new local attempt id is `action-idle-layered-001`.

## File map

- `tools/art/idle_layered_contract.py`: immutable rig types, hash/config validation, and the canonical 12-frame timeline.
- `tools/art/idle_layered_prepare.py`: verified source copying, polygon-mask rasterization, overlapping plate extraction, rest-pose reconstruction, and separation previews.
- `tools/art/idle_layered_render.py`: rigid transforms, triangle-mesh mantle warp, layer compositing, lantern/tether review composite, PNG/GIF export, and render metadata.
- `tools/art/idle_layered_qc.py`: evidence checks for frame count, canvas, anchors, rigid scales, bounds, alpha, loop seam, hashes, and metadata.
- `tools/verify-player-idle.ps1`: focused test/preflight/render/QC wrapper; never added to the main game gate in this review stage.
- `tests/art/test_idle_layered_contract.py`: config and timeline tests.
- `tests/art/test_idle_layered_prepare.py`: synthetic mask, overlap, and exact rest-pose reconstruction tests.
- `tests/art/test_idle_layered_render.py`: rigid-transform and connected-mesh rendering tests.
- `tests/art/test_idle_layered_qc.py`: mutation tests proving each rejection rule fails closed.
- `docs/art/player/player-art-generation-log.json`: append the free local attempt only after fresh rendering and QC.
- `docs/art/player/player-art-production.md`: record preflight and review state without publishing runtime assets.

External files created by the tools:

- `source/body.png`, `source/lantern.png`
- `rig/regions.json`, `rig/timeline.json`, `rig/rig-meta.json`
- `layers/boots.png`, `layers/underlay.png`, `layers/chassis.png`, `layers/weapon.png`, `layers/mantle.png`, `layers/core-glow.png`
- `review/separation-overlay.png`, `review/rest-reconstruction.png`, `review/rest-difference.png`, `review/occlusion-sweep.gif`
- `render/body/idle-01.png` through `idle-12.png`
- `render/composite/idle-01.png` through `idle-12.png`
- `render/idle-body.gif`, `render/idle-composite.gif`, `render/render-meta.json`, `render/qc-report.json`

---

### Task 1: Define and test the immutable rig contract

**Files:**
- Create: `tools/art/idle_layered_contract.py`
- Create: `tests/art/test_idle_layered_contract.py`

**Interfaces:**
- Produces: `sha256_file(path: Path) -> str`
- Produces: `FramePose(index: int, chassis_y: float, chassis_deg: float, weapon_deg: float, mantle_root: tuple[float,float], mantle_mid: tuple[float,float], mantle_tip: tuple[float,float], core_energy: float, lantern_offset: tuple[float,float], lantern_deg: float)`; `mantle_root` controls the first free cloth column after the pinned shoulder attachment, not the attachment itself.
- Produces: `RigConfig.load(path: Path) -> RigConfig`
- Produces: `build_default_timeline() -> tuple[FramePose, ...]`
- Produces: `write_default_timeline(path: Path) -> None`

- [ ] **Step 1: Write failing contract tests**

Create tests that assert the exact inputs, timing, rigid-scale rule, phase order,
delayed mantle motion, and cyclic seam:

```python
from pathlib import Path
import json, sys, unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "art"))
from idle_layered_contract import RigConfig, build_default_timeline

class IdleLayeredContractTests(unittest.TestCase):
    def test_default_timeline_is_12_frames_at_8_fps(self):
        poses = build_default_timeline()
        self.assertEqual(12, len(poses))
        self.assertEqual(list(range(12)), [p.index for p in poses])
        self.assertTrue(all(abs(p.chassis_y) <= 2.0 for p in poses))
        self.assertTrue(all(0.0 <= p.core_energy <= 1.0 for p in poses))

    def test_pulse_travels_root_to_mid_to_tip(self):
        poses = build_default_timeline()
        root_peak = max(range(4, 9), key=lambda i: abs(poses[i].mantle_root[1]))
        mid_peak = max(range(4, 10), key=lambda i: abs(poses[i].mantle_mid[1]))
        tip_peak = max(range(4, 11), key=lambda i: abs(poses[i].mantle_tip[1]))
        self.assertLess(root_peak, mid_peak)
        self.assertLess(mid_peak, tip_peak)

    def test_config_rejects_wrong_source_hash(self):
        raw = {
            "version": 1, "frames": 12, "fps": 8,
            "canvas": [256, 256], "origin": [128, 231],
            "bodySource": {"path": "body.png", "sha256": "bad"},
            "lanternSource": {"path": "lantern.png", "sha256": "bad"},
            "rigidScale": [1.0, 1.0]
        }
        with self.assertRaisesRegex(ValueError, "bodySource.sha256"):
            RigConfig.from_dict(raw)
```

- [ ] **Step 2: Run the tests to establish RED**

Run:

```powershell
$python='E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe'
& $python -m unittest discover -s tests\art -p 'test_idle_layered_contract.py' -v
```

Expected: non-zero with `ModuleNotFoundError: No module named 'idle_layered_contract'`.

- [ ] **Step 3: Implement the contract and canonical timeline**

Use frozen dataclasses. Validate exact frame count, FPS, canvas, origin, both
64-character uppercase hashes, and `rigidScale == (1.0,1.0)`. Use these
periodic pose arrays; they encode a quiet breeze plus one medium pulse without
requiring random or wall-clock time:

```python
CHASSIS_Y = (0.0, -0.5, -1.0, -1.5, -2.0, -1.5, -1.0, -0.5, 0.0, 0.5, 1.0, 0.5)
CHASSIS_DEG = (0.0, 0.05, 0.10, 0.12, 0.10, 0.05, 0.0, -0.05, -0.10, -0.12, -0.10, -0.05)
WEAPON_DEG = (0.0, -0.08, -0.16, -0.20, -0.16, -0.08, 0.0, 0.08, 0.16, 0.20, 0.16, 0.08)
BREEZE = (0.0,0.5,0.866,1.0,0.866,0.5,0.0,-0.5,-0.866,-1.0,-0.866,-0.5)
PULSE = (0.0,0.0,0.0,0.0,1.0,0.55,0.15,0.0,0.0,0.0,0.0,0.0)
CORE = (0.18,0.16,0.15,0.18,0.35,0.75,1.0,0.62,0.32,0.24,0.22,0.20)

def pulse_at(frame, delay):
    source = frame - delay
    return PULSE[source] if 0 <= source < 12 else 0.0

def cloth_offset(frame, delay, breeze_x, breeze_y, pulse_y):
    breeze = BREEZE[(frame - delay) % 12]
    return (breeze_x * breeze, breeze_y * breeze - pulse_y * pulse_at(frame, delay))

ROOT = tuple(cloth_offset(i, 0, 1.0, -1.0, 3.0) for i in range(12))
MID = tuple(cloth_offset(i, 1, 1.6, -1.3, 4.0) for i in range(12))
TIP = tuple(cloth_offset(i, 2, 2.4, -1.8, 5.0) for i in range(12))
LANTERN = tuple((
    -2.0 * BREEZE[(i - 2) % 12] + 2.0 * pulse_at(i, 3),
    BREEZE[(i - 2) % 12] - 6.0 * pulse_at(i, 3)
) for i in range(12))
LANTERN_DEG = tuple(0.6 * BREEZE[(i - 3) % 12] + 0.8 * pulse_at(i, 3) for i in range(12))
```

Restrict peak search to the pulse interval shown above so the contract test
measures the intended root-to-tip event rather than an ambient-breeze extremum.

- [ ] **Step 4: Run Task 1 tests and the complete art-test discovery**

```powershell
& $python -m unittest discover -s tests\art -p 'test_idle_layered_contract.py' -v
& $python -m unittest discover -s tests\art -p 'test_idle_layered_*.py' -v
```

Expected: all current tests pass.

- [ ] **Step 5: Commit Task 1**

```powershell
git add tools/art/idle_layered_contract.py tests/art/test_idle_layered_contract.py
git commit -m "test: define layered idle rig contract"
```

---

### Task 2: Build a fail-closed, non-destructive layer preflight

**Files:**
- Create: `tools/art/idle_layered_prepare.py`
- Create: `tests/art/test_idle_layered_prepare.py`

**Interfaces:**
- Consumes: `RigConfig`, `sha256_file`, and `write_default_timeline` from Task 1.
- Produces: `polygon_mask(size, points, feather_px) -> Image.Image`
- Produces: `extract_plate(source, mask, overlap_px) -> Image.Image`
- Produces: `reconstruct_rest(plates: Sequence[Image.Image]) -> Image.Image`
- Produces: `prepare_workspace(body_path: Path, lantern_path: Path, output_root: Path) -> dict`
- Produces: CLI `python tools/art/idle_layered_prepare.py --body <png> --lantern <png> --output-root <dir>`.

- [ ] **Step 1: Write synthetic separation tests**

Use a 64x64 RGBA fixture with fixed boots, a cream chassis, a green mantle,
and a brass weapon. Tests must prove source files are copied byte-for-byte,
mask pixels stay within source alpha, overlap preserves the rest pose, and a
region disjoint from source alpha is rejected:

```python
def test_rest_reconstruction_matches_source_inside_one_rgba_level(self):
    source, regions = make_synthetic_character()
    result = prepare_synthetic(source, regions)
    diff = ImageChops.difference(source, result.rest_reconstruction)
    self.assertLessEqual(max(channel[1] for channel in diff.getextrema()), 1)

def test_region_disjoint_from_source_alpha_fails_closed(self):
    source, regions = make_synthetic_character()
    regions["mantle"]["polygons"] = [[[0,0],[4,0],[4,4],[0,4]]]
    with self.assertRaisesRegex(ValueError, "does not intersect source alpha"):
        prepare_synthetic(source, regions)
```

- [ ] **Step 2: Run Task 2 tests to establish RED**

```powershell
& $python -m unittest discover -s tests\art -p 'test_idle_layered_prepare.py' -v
```

Expected: non-zero because `idle_layered_prepare` does not exist.

- [ ] **Step 3: Implement verified copying and overlapping masks**

The tool must:

1. Hash the repository inputs before copying them.
2. Copy them to `source/` without recompressing.
3. Rasterize source-pixel polygons from `regions.json`, require every named
   region to intersect foreground, and intersect all output masks with the body
   alpha channel. Polygon bounds may include transparent padding.
4. Use 32 source pixels of overlap at ankles, grip, and mantle root, covering
   at least 6 output pixels after normalization so maximum excursions do not
   reveal a one-pixel crack.
5. Build `underlay.png` only inside the explicit torso-under-mantle polygon in
   `regions.json`. Fill it from the nearest visible near-black lining and torso
   samples using deterministic nearest-neighbor propagation; never extend it
   beyond that polygon or the character's rest silhouette.
6. Keep boots on the bottom layer, followed by underlay, mantle, chassis,
   weapon, and core glow. Use a non-overlapping ownership mask for the exact
   rest comparison; animation overlap is checked by the occlusion sweep.
7. Recompose the exact rest pose and write a difference image amplified 8x.
8. Write `separation-overlay.png` with boots blue, chassis cream, mantle green,
   weapon orange, overlap magenta, and unassigned foreground red.
9. Reject any unassigned foreground island larger than 16 source pixels, any
   named polygon that does not intersect source alpha, or any underlay pixel
   outside its explicit polygon.

When `rig/regions.json` does not exist, initialize it in the external workspace
with these source-space regions and pivots, never normalized or inferred at
runtime. When it already exists, validate and preserve it byte-for-byte; do not
overwrite later manual mask corrections:

```json
{
  "version": 1,
  "sourceSize": [1402, 1122],
  "bodySha256": "0358BFEA48D6FF79271A5B14AE03B0396361748E60EB2D9D36EF6A696D7B1B44",
  "regions": {
    "boots": {"polygons": [[[474,997],[626,997],[625,1081],[458,1081]],[[665,1001],[849,1001],[849,1081],[660,1081]]]},
    "mantle": {"polygons": [[[575,479],[666,518],[704,598],[665,704],[607,827],[552,1001],[493,993],[452,936],[401,920],[363,883],[295,852],[342,777],[409,687],[480,588]]], "rootPivot": [586,505]},
    "underlay": {"polygons": [[[450,608],[620,545],[694,635],[650,815],[560,961],[450,914]]], "samplePolygons": [[[570,688],[694,635],[650,815],[560,961]]]},
    "weapon": {"polygons": [[[579,838],[668,831],[780,852],[844,825],[1004,826],[1077,1047],[1016,1051],[925,1012],[840,970],[769,927],[648,923],[592,901]]], "pivot": [641,874]},
    "coreGlow": {"polygons": [[[457,489],[520,465],[576,493],[577,572],[526,609],[466,585]]]},
    "chassis": {"mode": "remaining-alpha-plus-overlap"}
  },
  "overlapPx": 32
}
```

These coordinates are an initial deterministic mask definition, not an art
approval. The generated overlay and rest reconstruction are the authority; if
they fail visually, revise `regions.json` locally and rerun without generating
pixels.

- [ ] **Step 4: Run tests, then create the real external preflight**

```powershell
& $python -m unittest discover -s tests\art -p 'test_idle_layered_prepare.py' -v
& $python tools/art/idle_layered_prepare.py `
  --body docs/art/player/reference/thread-spool-bearer-body.png `
  --lantern assets/player/thread-spool-bearer/lantern.png `
  --output-root 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\actions\idle-layered'
```

Expected: exit 0; verified source hashes in `rig/rig-meta.json`; all five layer
PNGs and three preflight images exist; rest maximum RGBA difference is at most
1 before amplified visualization.

- [ ] **Step 5: Perform the mandatory visual separation gate**

Inspect at original resolution:

- `review/separation-overlay.png`
- `review/rest-reconstruction.png`
- `review/rest-difference.png`

Reject if the face slit, chest spool, half-bell blade, hands, boots, mantle
holes, mantle trim, or core is assigned to the wrong plate; if red unassigned
foreground remains; or if the rest pose differs visibly. Show these images to
the user and stop for explicit preflight approval. Do not proceed on the basis
of a zero exit code alone.

- [ ] **Step 6: Commit Task 2 tools and tests after the preflight is acceptable**

```powershell
git add tools/art/idle_layered_prepare.py tests/art/test_idle_layered_prepare.py
git commit -m "feat: add layered idle separation preflight"
```

---

### Task 3: Render rigid plates and one continuous weighted mantle

**Files:**
- Create: `tools/art/idle_layered_render.py`
- Create: `tests/art/test_idle_layered_render.py`

**Interfaces:**
- Consumes: approved external layers and timeline from Tasks 1-2.
- Produces: `affine_plate(image, pivot, translate, degrees) -> Image.Image`
- Produces: `warp_triangle_mesh(image, source_points, target_points, triangles) -> Image.Image`
- Produces: `render_body_frame(rig, pose) -> Image.Image`
- Produces: `render_composite_frame(body, lantern, pose) -> Image.Image`
- Produces: CLI `python tools/art/idle_layered_render.py --root <external-root>`.

- [ ] **Step 1: Write deterministic renderer tests**

Tests use synthetic colored plates and a checker mantle. They assert:

```python
def test_rigid_plate_translation_never_changes_declared_scale(self):
    pose = build_default_timeline()[4]
    frame, meta = render_synthetic_frame(pose)
    self.assertEqual([1.0, 1.0], meta["plates"]["chassis"]["scale"])
    self.assertEqual([1.0, 1.0], meta["plates"]["weapon"]["scale"])

def test_mantle_root_is_pinned_and_mesh_is_connected(self):
    frame, meta = render_synthetic_frame(build_default_timeline()[7])
    source = meta["mantle"]["sourceAttachment"]
    expected = [source[0], source[1] + meta["plates"]["chassis"]["translate"][1]]
    self.assertEqual(expected, meta["mantle"]["targetAttachment"])
    self.assertEqual(1, alpha_component_count(frame.crop((0,0,36,64))))

def test_render_is_byte_deterministic(self):
    first = render_synthetic_sequence()
    second = render_synthetic_sequence()
    self.assertEqual(frame_hashes(first), frame_hashes(second))
```

- [ ] **Step 2: Run Task 3 tests to establish RED**

```powershell
& $python -m unittest discover -s tests\art -p 'test_idle_layered_render.py' -v
```

Expected: non-zero because `idle_layered_render` does not exist.

- [ ] **Step 3: Implement the renderer without new dependencies**

- Convert each destination triangle back to its source triangle with a 3x3
  affine solve using NumPy.
- Sample the source with Pillow bicubic resampling and clip each result with a
  destination polygon mask before alpha compositing.
- Define one connected mantle mesh with a shoulder attachment column pinned to
  the moving chassis, followed by one root-region column, two middle columns,
  and one tip column. Blend `ROOT`, `MID`, and `TIP` displacements across the
  free columns with per-vertex weights that sum to 1.0; attachment vertices
  receive only the chassis transform.
- Transform boots by `(0,0,0deg)`, chassis by `(0,chassis_y,chassis_deg)`, and
  weapon about its grip by `(0,chassis_y,weapon_deg)`. Never pass a resize or
  scale argument to rigid transforms.
- Render body plates to the 256x256 canvas at the scale derived from the
  accepted body alpha height to the existing 144-pixel idle scale reference.
- Render a separate review composite. Normalize the accepted lantern once so
  its non-transparent bounding box is exactly 24 output pixels tall, reuse that
  identical scale in all frames, place it at its crown rest point plus
  `lantern_offset`, rotate by `lantern_deg`, and draw a two-segment cyan tether
  from the transformed back-core marker to the lantern base.
- Modulate only the separate core/glow overlay using `core_energy`; never
  modify body RGB values to fake the pulse.
- Write exactly 12 body PNGs, 12 composite PNGs, and two looping GIFs whose
  frame delays alternate `120,130` milliseconds. GIF stores time in 10ms units,
  so this sequence preserves the exact 1500ms loop and 8 FPS average without
  claiming an impossible 125ms-per-frame encoding. Write `render-meta.json`
  containing every transform, delay, and SHA-256.

- [ ] **Step 4: Run renderer tests and render a real occlusion sweep**

```powershell
& $python -m unittest discover -s tests\art -p 'test_idle_layered_render.py' -v
& $python tools/art/idle_layered_render.py `
  --root 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\actions\idle-layered' `
  --occlusion-sweep-only
```

Expected: tests pass; `review/occlusion-sweep.gif` shows maximum chassis,
weapon, and mantle excursions without transparent cracks or duplicated edges.
Inspect the sweep before rendering the finished timeline.

- [ ] **Step 5: Commit Task 3**

```powershell
git add tools/art/idle_layered_render.py tests/art/test_idle_layered_render.py
git commit -m "feat: render deterministic layered idle"
```

---

### Task 4: Add evidence checks that catch the previous false-green class

**Files:**
- Create: `tools/art/idle_layered_qc.py`
- Create: `tests/art/test_idle_layered_qc.py`
- Create: `tools/verify-player-idle.ps1`

**Interfaces:**
- Consumes: `render-meta.json`, body frames, composite frames, and both GIFs.
- Produces: `verify_render(root: Path) -> dict`
- Produces: CLI exact marker `PLAYER_IDLE_LAYERED_QC_OK frames=12 fps=8`.

- [ ] **Step 1: Write mutation tests for every fail-closed rule**

Fixtures must independently mutate frame count, canvas, scale metadata, foot
anchor, edge alpha, component count, stale file time, missing hash, and loop
velocity. Each mutation must raise a message naming its exact field:

```python
def test_rejects_any_non_unit_rigid_scale(self):
    evidence = valid_evidence()
    evidence.meta["frames"][5]["plates"]["chassis"]["scale"] = [1.01, 1.0]
    with self.assertRaisesRegex(ValueError, "chassis.scale"):
        verify_fixture(evidence)

def test_rejects_loop_transform_velocity_snap(self):
    evidence = valid_evidence()
    evidence.meta["frames"][11]["mantle"]["tip"] = [9.0, -8.0]
    with self.assertRaisesRegex(ValueError, "loop velocity"):
        verify_fixture(evidence)
```

- [ ] **Step 2: Run Task 4 tests to establish RED**

```powershell
& $python -m unittest discover -s tests\art -p 'test_idle_layered_qc.py' -v
```

Expected: non-zero because `idle_layered_qc` does not exist.

- [ ] **Step 3: Implement strict QC and the focused wrapper**

`verify_render` must require:

- 12 body and 12 composite files named `idle-01.png` through `idle-12.png`;
- 256x256 RGBA frames and exactly two 12-frame GIFs;
- alternating 120/130ms GIF frame durations, exactly 1500ms total and 8 FPS
  average, matching the authored 8 FPS timeline;
- non-empty alpha with no edge contact;
- foot baseline range at most 1 output pixel;
- all rigid scale metadata exactly `[1.0,1.0]`;
- mantle attachment target equal to its source attachment plus the current
  chassis transform in every frame;
- transform continuity at the 12-to-1 seam and cyclic first-difference bounds
  no larger than the maximum adjacent-frame delta already present in the loop;
- current file hashes matching `render-meta.json`;
- output modification times newer than the current verification start time;
- no magenta-dominant opaque pixel;
- exact success marker once.

`tools/verify-player-idle.ps1` first runs all `tests/art` tests, then optionally
runs prepare/render/QC when `-ExternalRoot` is supplied. It deletes only prior
`render/` and `review/occlusion-sweep.gif` inside the exact validated external
root. It never deletes `source/`, `rig/`, `layers/`, rejected `idle-001`, or any
repository asset.

- [ ] **Step 4: Prove RED mutations and GREEN verification**

```powershell
& $python -m unittest discover -s tests\art -p 'test_idle_layered_qc.py' -v
powershell -ExecutionPolicy Bypass -File tools/verify-player-idle.ps1
```

Expected: mutation suite and all art tests pass. The focused wrapper prints
`PLAYER_IDLE_LAYERED_TESTS_OK` and does not require external art files when
`-ExternalRoot` is omitted.

- [ ] **Step 5: Commit Task 4**

```powershell
git add tools/art/idle_layered_qc.py tests/art/test_idle_layered_qc.py tools/verify-player-idle.ps1
git commit -m "test: verify layered idle evidence"
```

---

### Task 5: Render exactly one review candidate and stop at visual approval

**Files:**
- Modify: `docs/art/player/player-art-generation-log.json`
- Modify: `docs/art/player/player-art-production.md`
- External only: `E:\OurGame\ArtProduction\thread-spool-bearer-v1\actions\idle-layered\render\*`

**Interfaces:**
- Consumes: visually approved separation preflight and Tasks 1-4 tools.
- Produces: local attempt record `action-idle-layered-001` with status `review-ready` or `rejected`.
- Does not produce any runtime asset.

- [ ] **Step 1: Run a fresh full local render and strict QC**

```powershell
$root='E:\OurGame\ArtProduction\thread-spool-bearer-v1\actions\idle-layered'
powershell -ExecutionPolicy Bypass -File tools/verify-player-idle.ps1 -ExternalRoot $root
```

Expected: exit 0, test marker once, `PLAYER_IDLE_LAYERED_QC_OK frames=12 fps=8`
once, 12 fresh body frames, 12 fresh composite frames, and two fresh GIFs.

- [ ] **Step 2: Inspect the motion at three scales**

View `idle-composite.gif` at original 256x256, at approximate 96-pixel body
height, and enlarged 4x with nearest-neighbor presentation. Also inspect frames
1, 4, 6, 8, 10, and 12 plus the 12-to-1 boundary.

Reject immediately if the body volume changes, feet slide, joints crack,
weapon detaches, mantle hinges, the pulse travels in the wrong order, the tip
snaps, the lantern moves in sync instead of lagging, or the loop seam is felt.

- [ ] **Step 3: Record the free local attempt before asking for approval**

Append a JSON record with this exact accounting contract:

```json
{
  "id": "action-idle-layered-001",
  "operation": "local-layered-rig",
  "hostMode": "none",
  "reportedModel": null,
  "quotaBearing": false,
  "externalApiCostCents": 0,
  "status": "review-ready",
  "action": "idle",
  "frameCount": 12,
  "fps": 8,
  "loop": true,
  "parent": "body-001"
}
```

Add absolute external paths, SHA-256 hashes for both GIFs, `render-meta.json`
and `qc-report.json`, and a review note stating that no runtime asset has been
published. If local or visual inspection already failed, set `status` to
`rejected` and fill `rejectionReason` instead.

- [ ] **Step 4: Validate docs and commit the review record**

```powershell
Get-Content -Raw docs/art/player/player-art-generation-log.json | ConvertFrom-Json | Out-Null
git diff --check
git add docs/art/player/player-art-generation-log.json docs/art/player/player-art-production.md
git commit -m "art: prepare layered idle review candidate"
```

- [ ] **Step 5: Present the composite GIF and representative frames**

State the exact automated evidence and explicitly separate it from subjective
approval. Ask the user to approve or reject `action-idle-layered-001`. Do not
start `run`, expand to 16 frames, publish the atlas, or change Godot while the
answer is pending.

If the user says the motion path is correct but cramped, change only the frame
contract to 16 frames at 8 FPS, add in-betweens from the same rig, assign a new
attempt id, rerun Task 4 evidence, and present it independently.

## Completion boundary

This plan is complete when one freshly rendered 12-frame composite has passed
strict local evidence checks and has been shown to the user with a complete
free-processing record. It is successful for production only if the user also
approves the motion. Runtime integration remains a later plan.
