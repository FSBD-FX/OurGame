# Player A2 Shoulder-Rig Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the Thread-Spool Bearer static layered master so the continuous mantle occludes the far-side body, the medium front spool and back core share one shoulder axis, and the whole ceramic body follows one lighting scheme.

**Architecture:** Keep the existing canonical 35 PNG part IDs and local SVG-to-PNG pipeline. Add an A2 revision contract and revision-specific review state so historical approvals remain immutable, then revise the body, shoulder hardware, mantle, tether, and final composition in dependency order. Each formal visual gate generates fresh, hash-bound review evidence and pauses for explicit user approval.

**Tech Stack:** Python 3.13 standard library, Pillow, SVG authored by Python, Microsoft Edge headless SVG rasterization, PowerShell, `unittest`, SHA-256 evidence manifests.

## Global Constraints

- The canonical canvas remains exactly `1402x1122`, baseline `y=1080`, facing right.
- Existing `left-*` limbs are the far side; existing `right-*` limbs are the near side.
- Keep all 35 canonical part IDs; no visible subject may borrow pixels from another layer.
- The mantle occlusion is composition-only: never delete hidden pixels from far-side body layers.
- Both sides use the same bone-white ceramic base and the same soft top-left light rig; any far-side value change is at most about 8%.
- A2 front-spool diameter is 28%–34% of the head diameter; back-core diameter is 24%–30%.
- The chest shell remains complete when both shoulder devices are removed.
- The floating lantern remains above and behind the head; its body-side tether terminal moves to the shoulder-back core.
- Use the free local production path only. Do not invoke paid image APIs or spend generation credit.
- Keep authoring files and review evidence under `E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001`; do not replace Godot runtime assets or scenes.
- Preserve historical `evidence/review-state.json`; use revision-specific `evidence/a2-review-state.json` for the three A2 gates.
- Formal gates, in order, are `a2Structure`, `a2DressedMaster`, and `a2Readiness`. Automated QC cannot approve any gate.
- Start from worktree `E:\OurGame\Knight\.worktrees\player-art-pipeline` on branch `codex/player-art-pipeline`.
- The worktree already contains uncommitted final-composition prototypes in `tools/art/complete_master_review.py`, `tools/art/complete_master_qc.py`, `tools/verify-player-master.ps1`, and their tests. Treat them as in-progress input; do not discard them or commit the rejected chest-mounted output unchanged.

## File Structure

- `tools/art/complete_master_a2_contract.py`: A2 geometry, far/near semantics, stack order, gate order, and revision-state creation.
- `tools/art/complete_master_body.py`: complete body layers, shared ceramic light, and clean chest.
- `tools/art/complete_master_spool_core.py`: medium near-shoulder front spool, same-axis back core, hidden shoulder axle.
- `tools/art/complete_master_mantle.py`: continuous far-side mantle and shoulder-root hardware.
- `tools/art/complete_master_lantern_tether.py`: tether geometry terminating at the new shoulder-back core.
- `tools/art/complete_master_review.py`: A2 final composition, contact sheet, small-scale review, and animation-space review.
- `tools/art/complete_master_qc.py`: generic fresh-evidence, hash, review-state, and ordered-gate verification.
- `tools/verify-player-master.ps1`: test, prepare, and QC entry point for `-Stage a2`.
- `tests/art/test_complete_master_a2_contract.py`: revision isolation, canonical stack, and gate contract.
- Existing `tests/art/test_complete_master_*.py`: component and regression coverage.
- `docs/art/player/player-art-production.md` and `README.md`: durable progress and asset status.

---

### Task 1: A2 Revision Contract and Review-State Isolation

**Files:**
- Create: `tools/art/complete_master_a2_contract.py`
- Create: `tests/art/test_complete_master_a2_contract.py`
- Modify: `tools/art/complete_master_qc.py`
- Modify: `tests/art/test_complete_master_qc.py`
- Modify: `tools/verify-player-master.ps1`

**Interfaces:**
- Produces: `A2_GATE_ORDER: tuple[str, ...]`, `A2_DRESSED_STACK: tuple[str, ...]`, `FAR_BODY_PART_IDS`, `NEAR_BODY_PART_IDS`, `ensure_a2_review_state(root: Path) -> Path`.
- Extends: `verify_stage(root: Path, stage: str, verification_started_ns: int) -> dict[str, Any]` to read a manifest-declared review-state path and ordered gate list.
- Consumes later: component renderers and final review assembly import the exact A2 stack and geometry constants.

- [ ] **Step 1: Write the failing A2 contract tests**

```python
def test_a2_keeps_historical_review_state_separate(self):
    path = ensure_a2_review_state(self.root)
    self.assertEqual("evidence/a2-review-state.json", path.relative_to(self.root).as_posix())
    self.assertEqual(
        ["a2Structure", "a2DressedMaster", "a2Readiness"],
        list(json.loads(path.read_text(encoding="utf-8"))),
    )
    self.assertEqual(self.legacy_review_bytes, (self.root / "evidence/review-state.json").read_bytes())

def test_a2_stack_occludes_far_body_with_mantle(self):
    self.assertLess(A2_DRESSED_STACK.index("left-upper-arm"), A2_DRESSED_STACK.index("mantle"))
    self.assertLess(A2_DRESSED_STACK.index("left-thigh"), A2_DRESSED_STACK.index("mantle"))
    self.assertLess(A2_DRESSED_STACK.index("mantle"), A2_DRESSED_STACK.index("back-core-housing"))
    self.assertLess(A2_DRESSED_STACK.index("torso-shell"), A2_DRESSED_STACK.index("front-spool-housing"))
    self.assertEqual(set(PART_IDS), set(A2_DRESSED_STACK))
    self.assertEqual(len(PART_IDS), len(A2_DRESSED_STACK))
```

- [ ] **Step 2: Run the A2 tests and verify RED**

Run:

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_a2_contract -v
```

Expected: import failure because `complete_master_a2_contract` does not exist.

- [ ] **Step 3: Implement the minimal A2 contract**

```python
A2_GATE_ORDER = ("a2Structure", "a2DressedMaster", "a2Readiness")
FAR_BODY_PART_IDS = (
    "left-thigh", "left-shin", "left-boot",
    "left-upper-arm", "left-forearm", "left-hand",
)
NEAR_BODY_PART_IDS = (
    "right-thigh", "right-shin", "right-boot",
    "right-upper-arm", "right-forearm", "right-hand",
)
A2_DRESSED_STACK = (
    "inner-body", *FAR_BODY_PART_IDS, "mantle",
    "back-core-housing", "back-energy-cell", "back-core-glow",
    "hidden-axle", "torso-shell", "pelvis-shell", "head-shell",
    *NEAR_BODY_PART_IDS, "shoulder-collar", "collar-fastener",
    "front-collar-flap", "front-spool-housing", "front-spool-disc",
    "front-spool-glow", "half-bell-cleaver", "left-grip-overlay",
    "right-grip-overlay", "tether-back-connector", "tether-energy-middle",
    "tether-lantern-connector", "lantern-housing", "lantern-glow",
)
```

`ensure_a2_review_state()` must create only this object when absent and must never rewrite a pre-existing file:

```json
{
  "a2Structure": {"status": "not-started", "explicitUserApproval": null},
  "a2DressedMaster": {"status": "not-started", "explicitUserApproval": null},
  "a2Readiness": {"status": "not-started", "explicitUserApproval": null}
}
```

- [ ] **Step 4: Add failing QC tests for a revision-specific state path**

```python
def test_a2_manifest_uses_revision_state_and_opens_only_first_gate(self):
    fixture.manifest["reviewStatePath"] = "evidence/a2-review-state.json"
    fixture.manifest["reviewGateOrder"] = ["a2Structure", "a2DressedMaster", "a2Readiness"]
    fixture.manifest["reviewGates"] = {
        "a2Structure": ["review/a2-structure.png"],
        "a2DressedMaster": ["review/a2-dressed.png"],
        "a2Readiness": ["review/a2-readiness.png"],
    }
    report = verify_stage(fixture.root, "a2", fixture.started_ns)
    self.assertEqual("a2Structure", report["activeGate"])
    self.assertEqual("ready-for-review", report["status"])
    self.assertEqual(fixture.legacy_review_bytes, fixture.legacy_review_path.read_bytes())
```

- [ ] **Step 5: Run the QC test and verify RED**

Run:

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_qc.CompleteMasterQcTests.test_a2_manifest_uses_revision_state_and_opens_only_first_gate -v
```

Expected: failure because QC still hardcodes `evidence/review-state.json` and the legacy final gate order.

- [ ] **Step 6: Generalize QC without weakening legacy freshness rules**

Read `reviewStatePath` with a default of `evidence/review-state.json`, validate it through `_within_root`, and use `reviewGateOrder` only when present. Keep these invariants:

```python
if stage == "final" or manifest.get("reviewGateOrder"):
    active_gate, status = verify_ordered_review_state(
        review,
        tuple(manifest["reviewGateOrder"]),
        manifest["reviewGates"],
        review_hash_map,
    )
```

Only `role == "review"` files must be newer than the verification start for assembled revision stages; reused part layers still require exact manifest hashes. Legacy component stages continue to reject any stale manifest file.

- [ ] **Step 7: Add `a2` to the verification entry point**

Map `a2` to `tools\art\complete_master_review.py`. Do not add any Godot import or runtime-copy step.

- [ ] **Step 8: Run contract and QC tests GREEN**

Run:

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_a2_contract tests.art.test_complete_master_qc -v
```

Expected: all selected tests pass, including legacy stale-evidence rejection.

- [ ] **Step 9: Commit**

```powershell
git add tools/art/complete_master_a2_contract.py tools/art/complete_master_qc.py tools/verify-player-master.ps1 tests/art/test_complete_master_a2_contract.py tests/art/test_complete_master_qc.py
git commit -m "test: define A2 shoulder rig evidence"
```

---

### Task 2: Unified Ceramic Body and Clean Chest

**Files:**
- Modify: `tools/art/complete_master_body.py`
- Modify: `tests/art/test_complete_master_body.py`

**Interfaces:**
- Consumes: `FAR_BODY_PART_IDS` and `NEAR_BODY_PART_IDS` from `complete_master_a2_contract.py`.
- Produces: the same 16 complete body PNGs and three body reviews; `torso-shell` has no device-shaped glow or hole.
- Preserves: joint points, complete hidden anatomy, baseline, head, hands, and exact body part IDs.

- [ ] **Step 1: Replace the old far-gradient assertion with failing A2 light tests**

```python
def test_all_ceramic_parts_use_one_top_left_ceramic_gradient(self):
    root = ET.fromstring(build_body_svg(self.request()))
    self.assertIsNone(next((x for x in root.iter() if x.get("id") == "ceramic-far"), None))
    for part_id in CERAMIC_PART_IDS:
        serialized = ET.tostring(part(part_id), encoding="unicode")
        self.assertIn("url(#ceramic-main)", serialized)
        self.assertEqual("canvas-soft-top-left-v1", part(part_id).get("data-light-rig"))

def test_torso_is_complete_clean_ceramic_without_old_core_bounce(self):
    torso = part("torso-shell")
    self.assertEqual("clean-ceramic-torso-a2", torso.get("data-form"))
    self.assertNotIn("core-bounce-restrained", ET.tostring(torso, encoding="unicode"))
```

- [ ] **Step 2: Run the body tests and verify RED**

Run:

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_body -v
```

Expected: failures because `ceramic-far` exists and the torso still contains two core-bounce strokes.

- [ ] **Step 3: Apply one ceramic material and clean the torso**

- Make `_ceramic_path()` always use `url(#ceramic-main)` and the same highlight color.
- Remove `ceramic-far` from `<defs>`.
- Mark far parts with `data-depth-role="far-occluded-a2"`; do not change their base material.
- Mark near parts with `data-depth-role="near-a2"`.
- Remove both `core-bounce-restrained` paths from `torso-shell` and add `data-form="clean-ceramic-torso-a2"`.
- Retain small joint-contact shadows and brass seam lines; these provide local depth without recoloring a whole side.

- [ ] **Step 4: Prove generated layers remain complete**

Add an image-level test that renders the pack and checks every far-side alpha bounding box and joint point remains non-empty. Compare all boot baselines to `1081` as before.

- [ ] **Step 5: Run body tests GREEN and render the external body pack**

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_body -v
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' tools\art\complete_master_body.py --external-root 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001'
```

Expected: tests pass and output is `COMPLETE_MASTER_BODY_OK files=19`.

- [ ] **Step 6: Visually inspect, but do not open a formal A2 gate yet**

Inspect `review/body-uncovered.png` and `review/body-uncovered-96px.png`. Confirm one light direction, no chest device silhouette, complete far-side limbs, and no new stretched or noisy forms. Fix only body defects before continuing.

- [ ] **Step 7: Commit**

```powershell
git add tools/art/complete_master_body.py tests/art/test_complete_master_body.py
git commit -m "art: unify A2 ceramic body lighting"
```

---

### Task 3: A2 Shoulder Hardware, Mantle Occlusion, and Structure Gate

**Files:**
- Modify: `tools/art/complete_master_spool_core.py`
- Modify: `tests/art/test_complete_master_spool_core.py`
- Modify: `tools/art/complete_master_mantle.py`
- Modify: `tests/art/test_complete_master_mantle.py`
- Modify: `tools/art/complete_master_review.py`
- Modify: `tests/art/test_complete_master_review.py`
- Modify: external `evidence/a2-review-state.json` only after explicit approval

**Interfaces:**
- Consumes: complete body layers, A2 geometry constants, and A2 stack.
- Produces: seven shoulder-device layers, four mantle/collar layers, and structure reviews `a2-structure-uncovered.png`, `a2-structure-mounted.png`, `a2-shoulder-detail.png`, and `a2-all-parts.png`.
- Formal gate: `a2Structure` only.

- [ ] **Step 1: Write failing shoulder-axis and size tests**

```python
def test_front_spool_and_back_core_share_a2_shoulder_axis_and_size(self):
    boxes = declared_boxes(build_spool_core_svg(self.request()))
    head_diameter = 240
    self.assertTrue(0.28 <= boxes["front-spool-housing"].width / head_diameter <= 0.34)
    self.assertTrue(0.24 <= boxes["back-core-housing"].width / head_diameter <= 0.30)
    self.assertLess(abs(boxes["front-spool-housing"].center_y - A2_SHOULDER_AXIS[1]), 18)
    self.assertLess(abs(boxes["back-core-housing"].center_y - A2_SHOULDER_AXIS[1]), 18)
    self.assertLess(boxes["back-core-housing"].center_x, boxes["front-spool-housing"].center_x)

def test_a2_devices_clear_head_and_near_arm(self):
    self.assertFalse(overlaps(boxes["front-spool-housing"], HEAD_CLEARANCE_BOX))
    self.assertFalse(overlaps(boxes["front-spool-housing"], NEAR_ARM_MOTION_CLEARANCE_BOX))
```

- [ ] **Step 2: Write failing mantle-overlap tests**

```python
def test_mantle_overlaps_far_body_enough_to_occlude_it(self):
    render_body_pack(root)
    render_spool_core_pack(root)
    render_mantle_pack(root)
    self.assertGreater(alpha_overlap_ratio(layer("mantle"), layer("left-upper-arm")), 0.45)
    self.assertGreater(alpha_overlap_ratio(layer("mantle"), layer("left-thigh")), 0.35)
    self.assertEqual(1, count_alpha_components(layer("mantle").getchannel("A"), threshold=24))
```

- [ ] **Step 3: Run spool and mantle tests and verify RED**

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_spool_core tests.art.test_complete_master_mantle -v
```

Expected: current chest spool is too large and too low; current mantle overlap/stack does not prove far-body occlusion.

- [ ] **Step 4: Redraw the A2 shoulder hardware**

- Move the front spool to the near-shoulder front, centered near the shared A2 shoulder axis.
- Move the back core behind that axis, slightly up/left in canvas depth, with a smaller diameter.
- Redraw `hidden-axle` as a short shoulder-through axle instead of a chest-through bar.
- Keep housing, disc/cell, and glow layers separately nested inside their declared bounds.
- Update the back-detail review labels to `SHOULDER-BACK CORE`, `SHOULDER AXLE`, and `SHOULDER-FRONT SPOOL`.

- [ ] **Step 5: Redraw mantle root and mounted composition**

- Keep `mantle` one connected soft-cloth layer with two holes and its gold edge.
- Move its root to the far-shoulder collar/axle.
- Extend its inner silhouette across the far upper arm, far hip, and most of the far thigh/shin.
- Compose mounted review in A2 order: complete inner/far body, mantle, back core, torso/head/near body, collar, front spool.
- Do not include any hardware or limb pixels in the mantle layer.

- [ ] **Step 6: Run component tests GREEN**

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_body tests.art.test_complete_master_spool_core tests.art.test_complete_master_mantle -v
```

Expected: all tests pass.

- [ ] **Step 7: Generate the A2 structure evidence**

Implement `render_a2_review_pack(root: Path, gate: str)` so `gate="a2Structure"` writes exactly the four structure review images and an `a2-manifest.json` whose first gate references them. Generate in dependency order:

```powershell
$py='E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe'
$root='E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001'
& $py tools\art\complete_master_body.py --external-root $root
& $py tools\art\complete_master_spool_core.py --external-root $root
& $py tools\art\complete_master_mantle.py --external-root $root
& $py tools\art\complete_master_review.py --external-root $root --gate a2Structure
```

- [ ] **Step 8: Run QC and pause for explicit user approval**

Run A2 QC, verify `activeGate=a2Structure`, then show all four review images. Do not alter `a2-review-state.json` before the user approves.

Acceptance checklist:

- far body is visibly hidden by cloth in the dressed structure but complete in the uncovered view;
- ceramic lighting no longer looks like two body colors;
- front spool and back core clearly share one shoulder axis and read at A2 medium scale;
- clean chest, head, weapon clearance, and standing balance remain intact.

- [ ] **Step 9: Record the exact approval and commit**

After explicit approval, store the exact user text, current timestamp, and SHA-256 hashes of the four gate images under `a2Structure`, rerun QC, then commit:

```powershell
git add tools/art/complete_master_spool_core.py tools/art/complete_master_mantle.py tools/art/complete_master_review.py tests/art/test_complete_master_spool_core.py tests/art/test_complete_master_mantle.py tests/art/test_complete_master_review.py
git commit -m "art: approve A2 shoulder structure"
```

---

### Task 4: Shoulder-Back Tether and Dressed-Master Gate

**Files:**
- Modify: `tools/art/complete_master_lantern_tether.py`
- Modify: `tests/art/test_complete_master_lantern_tether.py`
- Modify: `tools/art/complete_master_review.py`
- Modify: `tests/art/test_complete_master_review.py`
- Modify: external `evidence/a2-review-state.json` only after explicit approval

**Interfaces:**
- Consumes: approved A2 structure layers, unchanged weapon/hand layers, `A2_DRESSED_STACK`.
- Produces: revised tether terminal and curve plus `a2-dressed.png`, `a2-dressed-back-detail.png`, and `a2-dressed-layers.png`.
- Formal gate: `a2DressedMaster`; prerequisite is accepted `a2Structure`.

- [ ] **Step 1: Write failing tether and stack tests**

```python
def test_tether_starts_on_shoulder_back_core(self):
    self.assertEqual(A2_BACK_CORE_TETHER_POINT, TETHER_CONNECTIONS["back.in"])
    self.assertEqual(TETHER_CONNECTIONS["back.out"], TETHER_CONNECTIONS["energy.in"])
    self.assertTrue(point_inside_alpha(layer("back-core-housing"), A2_BACK_CORE_TETHER_POINT))

def test_a2_dressed_stack_uses_every_part_once_and_preserves_weapon_overlays(self):
    self.assertEqual(set(PART_IDS), set(A2_DRESSED_STACK))
    self.assertEqual(35, len(A2_DRESSED_STACK))
    self.assertLess(A2_DRESSED_STACK.index("half-bell-cleaver"), A2_DRESSED_STACK.index("left-grip-overlay"))
    self.assertLess(A2_DRESSED_STACK.index("half-bell-cleaver"), A2_DRESSED_STACK.index("right-grip-overlay"))
```

- [ ] **Step 2: Add a no-intersection image test**

Rasterize the new tether and assert the energy-middle alpha has no overlap with the head-shell alpha and no overlap with the front-spool housing alpha. It may overlap the back connector and lantern connector only at the declared endpoints.

- [ ] **Step 3: Run lantern/review tests and verify RED**

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_lantern_tether tests.art.test_complete_master_review -v
```

Expected: current tether starts at the old torso-side point and the rejected draft stack places mantle/back hardware incorrectly.

- [ ] **Step 4: Move the tether and finalize the A2 composition**

- Move `tether-back-connector` to the upper/rear edge of `back-core-housing`.
- Recurve `tether-energy-middle` upward behind the head without crossing head, front spool, or mantle body.
- Leave `lantern-housing`, `lantern-glow`, and the lantern-side endpoint unchanged.
- Make `compose_master(root, "dressed")` import and use `A2_DRESSED_STACK`; do not retain a second competing stack in the review file.
- Keep weapon and grip-overlay source hashes unchanged from the previously approved layers.

- [ ] **Step 5: Run tests GREEN and prove unchanged locked parts**

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_lantern_tether tests.art.test_complete_master_weapon_hands tests.art.test_complete_master_review -v
```

Add assertions that `half-bell-cleaver`, both hands, both grip overlays, lantern housing, and lantern glow match the pre-A2 approved hashes recorded before regeneration.

- [ ] **Step 6: Generate and QC the dressed evidence**

```powershell
$py='E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe'
$root='E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001'
& $py tools\art\complete_master_lantern_tether.py --external-root $root
& $py tools\art\complete_master_review.py --external-root $root --gate a2DressedMaster
```

QC must report `activeGate=a2DressedMaster` only after `a2Structure` is accepted.

- [ ] **Step 7: Pause for the dressed-master visual gate**

Show the three dressed images. Require explicit approval of silhouette, shoulder device identity, clean chest, mantle occlusion, natural weapon clearance, and tether connection. Do not treat the existing phrase “批准 A2 重构设计” as approval of the rendered art.

- [ ] **Step 8: Record approval and commit**

After approval, bind the exact user text to the three current review hashes, rerun QC, and commit:

```powershell
git add tools/art/complete_master_lantern_tether.py tools/art/complete_master_review.py tests/art/test_complete_master_lantern_tether.py tests/art/test_complete_master_review.py
git commit -m "art: approve A2 dressed master"
```

---

### Task 5: Small-Scale and Animation-Readiness Gate

**Files:**
- Modify: `tools/art/complete_master_review.py`
- Modify: `tests/art/test_complete_master_review.py`
- Modify: external `evidence/a2-review-state.json` only after explicit approval

**Interfaces:**
- Consumes: approved A2 dressed composite.
- Produces: `a2-dressed-96px.png` and `a2-animation-space.png`.
- Formal gate: `a2Readiness`; prerequisite is accepted `a2DressedMaster`.

- [ ] **Step 1: Write failing readiness tests**

```python
def test_a2_small_scale_uses_one_resize_of_the_complete_composite(self):
    review = build_small_scale_review(compose_master(root, "dressed"), body_height=96)
    self.assertEqual("single-composite-resize", review.meta["method"])
    self.assertEqual(96, review.meta["bodyHeight"])

def test_animation_space_review_marks_required_clearances(self):
    review = build_animation_space_review(root)
    self.assertEqual(
        {"near-arm-swing", "head-device-gap", "mantle-root", "shoulder-axis", "weapon-grip"},
        set(review.meta["clearanceLabels"]),
    )
```

- [ ] **Step 2: Run readiness tests and verify RED**

Expected: `build_animation_space_review` and A2 readiness metadata do not exist.

- [ ] **Step 3: Implement the two review images**

- Downscale the already composed dressed subject once with LANCZOS to exactly 96 pixels body height; never resize individual parts separately.
- Show the native 96-pixel result and a nearest-neighbor 5× inspection on one review canvas.
- Build the animation-space image from the full composite with colored, labeled arcs/points for the shared shoulder axis, head/device gap, near-arm swing corridor, mantle root, and two-hand weapon grip.
- Technical annotations must be review-only and never enter part layers.

- [ ] **Step 4: Run tests GREEN and generate readiness evidence**

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest tests.art.test_complete_master_review -v
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' tools\art\complete_master_review.py --external-root 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' --gate a2Readiness
```

- [ ] **Step 5: Pause for the readiness visual gate**

Show both images separately. Approval requires:

- head slit, medium shoulder spool, mantle silhouette, weapon, and lantern remain distinguishable at 96 pixels;
- no high-frequency noise returns;
- the near arm can lift and swing without immediately colliding with head or shoulder hardware;
- the shoulder axle, mantle root, tether, and weapon grip remain separable for later animation.

- [ ] **Step 6: Record approval and commit**

Bind exact approval text and both hashes under `a2Readiness`, rerun QC, then commit:

```powershell
git add tools/art/complete_master_review.py tests/art/test_complete_master_review.py
git commit -m "art: approve A2 static master readiness"
```

---

### Task 6: Full Verification, Documentation, and Static-Master Handoff

**Files:**
- Modify: `README.md`
- Modify: `docs/art/player/player-art-production.md`
- Modify: `tools/verify-player-master.ps1`
- Test: all `tests/art/test_complete_master_*.py`

**Interfaces:**
- Consumes: all three accepted A2 gates and their hash-bound evidence.
- Produces: clean verification output and a documented static-master handoff; no runtime asset publication.

- [ ] **Step 1: Make the full verification command regenerate in dependency order**

For `-Stage a2`, the script must run body, spool/core, mantle, lantern/tether, and final review preparation before QC. It must emit progress markers and fail on any nonzero subprocess. It must not invoke paid services, copy into `assets/`, or launch Godot.

- [ ] **Step 2: Add/retain regression coverage for all 35 parts**

The final manifest must contain exactly 35 unique part/overlay records plus the A2 review records. Verify every PNG is RGBA `1402x1122`, nonempty, within canvas, hash-matched, and uniquely owned. Verify only the first unmet gate can be opened.

- [ ] **Step 3: Run the complete test discovery**

```powershell
& 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe' -m unittest discover -s tests\art -p 'test_complete_master_*.py' -v
```

Expected: all tests pass without errors or warnings.

- [ ] **Step 4: Run the complete external A2 gate**

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify-player-master.ps1 -ExternalRoot 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' -Stage a2
```

Expected markers:

```text
COMPLETE_MASTER_TESTS_OK
COMPLETE_MASTER_A2_OK
COMPLETE_MASTER_QC_OK stage=a2
```

The QC report must show `status=accepted`, `activeGate=a2Readiness`, and all three A2 entries accepted with current evidence hashes.

- [ ] **Step 5: Review the actual output, not only the test log**

Open the final dressed image, 96-pixel image, animation-space image, shoulder detail, and all-parts sheet. Confirm they match the approved evidence hashes and contain no debug marks outside technical review images.

- [ ] **Step 6: Update durable project status**

In `README.md` and `docs/art/player/player-art-production.md`, record:

- A2 static layered master complete and its three gate approvals;
- 35 independently owned part layers;
- free local production, cost `$0`;
- intended in-game body height `96 px`;
- external authoring/review root;
- runtime atlas, animation clips, and Godot integration still left to do.

Do not claim the character is running in the game. The next project must be a separately approved animation and runtime-integration plan.

- [ ] **Step 7: Check the exact diff and commit**

```powershell
git diff --check
git status --short
git add README.md docs/art/player/player-art-production.md tools/verify-player-master.ps1
git commit -m "docs: hand off A2 static player master"
```

- [ ] **Step 8: Final handoff**

Report the three approval records, test count, QC result, exact final review paths, and remaining runtime work. Include the final dressed and 96-pixel images in the response. State explicitly that no paid generation occurred and no Godot runtime asset was replaced.

---

## Plan Self-Review

- **Spec coverage:** Tasks 2–5 cover unified light, clean chest, far-body occlusion, same-axis A2 devices, tether migration, 35-layer ownership, 96-pixel readability, and animation clearances. Tasks 1 and 6 cover historical approval preservation, ordered manual gates, free-only production, and no runtime replacement.
- **Scope:** One subsystem only: revision of the static layered player master. Animation generation and Godot integration remain separate future work.
- **Type consistency:** `A2_GATE_ORDER`, `A2_DRESSED_STACK`, `FAR_BODY_PART_IDS`, `NEAR_BODY_PART_IDS`, `ensure_a2_review_state()`, and `render_a2_review_pack()` retain the same names throughout all tasks.
- **Placeholder scan:** No `TBD`, `TODO`, implicit “similar to” steps, or unspecified error-handling tasks remain.
