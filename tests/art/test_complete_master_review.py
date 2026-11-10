from __future__ import annotations

import json
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageChops


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "art"))

from complete_master_body import BODY_PART_IDS, BODY_STACK, render_body_pack
from complete_master_a2_contract import A2_DRESSED_STACK, A2_GATE_ORDER
from complete_master_contract import BODY_SHA256, FULL_SHA256, LANTERN_SHA256, OVERLAY_PART_IDS, PART_IDS, REVIEW_STAGES
from complete_master_lantern_tether import render_lantern_tether_pack
from complete_master_mantle import render_mantle_pack
from complete_master_qc import verify_final_review_state
from complete_master_review import (
    A2_REVIEW_GATES,
    A2_DRESSED_REVIEW_NAMES,
    A2_STRUCTURE_REVIEW_NAMES,
    FINAL_REVIEW_GATES,
    FINAL_REVIEW_NAMES,
    build_contact_sheet,
    build_small_scale_review,
    build_stack,
    compose_master,
    _compose_stack,
    render_final_pack,
    render_a2_review_pack,
    A2_STRUCTURE_STACK,
    _a2_dressed_back_detail,
)
from complete_master_spool_core import render_spool_core_pack
from complete_master_svg import discover_edge
from complete_master_weapon_hands import render_weapon_hands_pack


def request_dict() -> dict:
    return {
        "version": 1,
        "id": "complete-layered-001",
        "canvas": [1402, 1122],
        "baselineY": 1080,
        "facing": "right",
        "parts": list(PART_IDS),
        "sources": {
            "body": {"path": "body.png", "sha256": BODY_SHA256},
            "full": {"path": "full.png", "sha256": FULL_SHA256},
            "lantern": {"path": "lantern.png", "sha256": LANTERN_SHA256},
        },
        "review": {stage: {"status": "not-started", "explicitUserApproval": None} for stage in REVIEW_STAGES},
    }


def build_all(root: Path) -> None:
    render_body_pack(root)
    render_spool_core_pack(root)
    render_mantle_pack(root)
    render_weapon_hands_pack(root)
    render_lantern_tether_pack(root)


class CompleteMasterReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory()
        cls.root = Path(cls.temporary.name)
        (cls.root / "request").mkdir(parents=True)
        (cls.root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
        if discover_edge() is not None:
            build_all(cls.root)

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    def test_stacks_use_body_only_for_uncovered_and_all_parts_once_for_dressed(self):
        uncovered = build_stack("uncovered")
        dressed = build_stack("dressed")
        self.assertEqual(A2_DRESSED_STACK, dressed)
        self.assertEqual(BODY_STACK, uncovered)
        self.assertEqual(35, len(dressed))
        self.assertEqual(set(PART_IDS), set(dressed))
        self.assertEqual(len(dressed), len(set(dressed)))
        for overlay in OVERLAY_PART_IDS:
            self.assertGreater(dressed.index(overlay), dressed.index(overlay.removesuffix("-grip-overlay") + "-hand"))
            self.assertGreater(dressed.index(overlay), dressed.index("half-bell-cleaver"))

    def test_render_a2_structure_pack_uses_revision_state_and_only_structure_reviews(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")

        report = render_a2_review_pack(self.root, "a2Structure")

        self.assertEqual("a2", report["stage"])
        self.assertEqual(35 + len(A2_STRUCTURE_REVIEW_NAMES), report["files"])
        manifest = json.loads(
            (self.root / "evidence" / "a2-manifest.json").read_text(encoding="utf-8")
        )
        self.assertEqual("a2", manifest["stage"])
        self.assertEqual("evidence/a2-review-state.json", manifest["reviewStatePath"])
        self.assertEqual(list(A2_GATE_ORDER), manifest["reviewGateOrder"])
        self.assertEqual(
            set(A2_STRUCTURE_REVIEW_NAMES),
            {
                Path(item["path"]).stem
                for item in manifest["files"]
                if item["role"] == "review"
            },
        )
        self.assertEqual(
            {
                gate: [f"review/{name}.png" for name in names]
                for gate, names in A2_REVIEW_GATES.items()
            },
            manifest["reviewGates"],
        )
        self.assertEqual(
            set(PART_IDS),
            {
                item["partId"]
                for item in manifest["files"]
                if item["role"] in ("part", "overlay")
            },
        )
        self.assertTrue((self.root / "evidence" / "a2-review-state.json").is_file())

    def test_render_a2_dressed_pack_preserves_approved_structure_and_adds_three_reviews(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "fixture"
            shutil.copytree(self.root, root)
            render_a2_review_pack(root, "a2Structure")
            manifest = json.loads(
                (root / "evidence" / "a2-manifest.json").read_text(encoding="utf-8")
            )
            structure_hashes = {
                item["path"]: item["sha256"]
                for item in manifest["files"]
                if Path(item["path"]).stem in A2_STRUCTURE_REVIEW_NAMES
            }
            state_path = root / "evidence" / "a2-review-state.json"
            state = json.loads(state_path.read_text(encoding="utf-8"))
            state["a2Structure"] = {
                "status": "accepted",
                "explicitUserApproval": "approved A2 structure",
                "approvedAt": "2026-08-13T23:55:53+08:00",
                "evidenceSha256": list(structure_hashes.values()),
            }
            state_path.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")

            report = render_a2_review_pack(root, "a2DressedMaster")

            self.assertEqual(35 + 4 + 3, report["files"])
            dressed_manifest = json.loads(
                (root / "evidence" / "a2-manifest.json").read_text(encoding="utf-8")
            )
            self.assertEqual(
                structure_hashes,
                {
                    item["path"]: item["sha256"]
                    for item in dressed_manifest["files"]
                    if Path(item["path"]).stem in A2_STRUCTURE_REVIEW_NAMES
                },
            )
            self.assertEqual(
                set(A2_DRESSED_REVIEW_NAMES),
                {
                    Path(item["path"]).stem
                    for item in dressed_manifest["files"]
                    if Path(item["path"]).stem in A2_DRESSED_REVIEW_NAMES
                },
            )

    def test_a2_mounted_structure_keeps_shoulder_back_core_materially_visible(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        core_ids = ("back-core-housing", "back-energy-cell", "back-core-glow")
        mounted = _compose_stack(self.root, A2_STRUCTURE_STACK, "with-back-core")
        without_core = _compose_stack(
            self.root,
            tuple(part_id for part_id in A2_STRUCTURE_STACK if part_id not in core_ids),
            "without-back-core",
        )
        isolated_core = _compose_stack(self.root, core_ids, "isolated-back-core")
        changed = ImageChops.difference(mounted, without_core).convert("RGB")
        visible_pixels = sum(changed.convert("L").histogram()[1:])
        source_pixels = sum(isolated_core.getchannel("A").histogram()[1:])
        self.assertGreaterEqual(visible_pixels / source_pixels, 0.65)
        self.assertIsNotNone(changed.crop((550, 465, 590, 515)).getbbox())
        changed.close()
        isolated_core.close()
        without_core.close()
        mounted.close()

    def test_a2_dressed_back_detail_has_three_clear_review_annotations(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        dressed = compose_master(self.root, "dressed")
        detail = _a2_dressed_back_detail(dressed)
        self.assertEqual(
            ["shoulder-back-core", "energy-tether", "floating-lantern"],
            detail.meta["technicalAnnotations"],
        )
        self.assertEqual(3, len(detail.meta["labelBoxes"]))
        for index, first in enumerate(detail.meta["labelBoxes"]):
            for second in detail.meta["labelBoxes"][index + 1 :]:
                self.assertTrue(
                    first[2] <= second[0]
                    or second[2] <= first[0]
                    or first[3] <= second[1]
                    or second[3] <= first[1]
                )
        detail.close()
        dressed.close()

    def test_dressed_master_reuses_exact_body_layer_hashes(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        uncovered = compose_master(self.root, "uncovered")
        dressed = compose_master(self.root, "dressed")
        self.assertEqual(uncovered.meta["bodyLayerSha256"], dressed.meta["bodyLayerSha256"])
        self.assertEqual(set(BODY_PART_IDS), set(uncovered.meta["bodyLayerSha256"]))
        uncovered.close()
        dressed.close()

    def test_contact_sheet_has_exactly_thirty_five_canonical_labels(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        sheet = build_contact_sheet(self.root, PART_IDS)
        self.assertEqual(list(PART_IDS), sheet.meta["partLabels"])
        self.assertEqual(35, len(sheet.meta["partLabels"]))
        sheet.close()

    def test_small_scale_review_uses_one_resize_of_complete_dressed_subject(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        dressed = compose_master(self.root, "dressed")
        review = build_small_scale_review(dressed, body_height=96)
        self.assertEqual(96, review.meta["scaledSubjectSize"][1])
        self.assertEqual("single-composite-resize", review.meta["method"])
        self.assertEqual(dressed.getchannel("A").getbbox(), tuple(review.meta["sourceAlphaBox"]))
        review.close()
        dressed.close()

    def test_render_final_pack_has_35_layers_five_reviews_and_gate_map(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        report = render_final_pack(self.root)
        self.assertEqual(40, report["files"])
        manifest = json.loads((self.root / "evidence" / "final-manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(40, len(manifest["files"]))
        self.assertEqual(set(PART_IDS), {item["partId"] for item in manifest["files"] if item["role"] in ("part", "overlay")})
        self.assertEqual(set(FINAL_REVIEW_NAMES), {Path(item["path"]).stem for item in manifest["files"] if item["role"] == "review"})
        self.assertEqual(
            {
                gate: [f"review/{name}.png" for name in names]
                for gate, names in FINAL_REVIEW_GATES.items()
            },
            manifest["reviewGates"],
        )

    def test_final_review_progression_opens_only_one_gate_at_a_time(self):
        review = {stage: {"status": "not-started", "explicitUserApproval": None} for stage in REVIEW_STAGES}
        hashes = {f"review/{name}.png": f"HASH-{index}" for index, name in enumerate(FINAL_REVIEW_NAMES)}
        gates = {gate: [f"review/{name}.png" for name in names] for gate, names in FINAL_REVIEW_GATES.items()}
        gate, status = verify_final_review_state(review, gates, hashes)
        self.assertEqual(("dressedMaster", "ready-for-review"), (gate, status))
        self.assertEqual("not-started", review["smallScale"]["status"])
        review["dressedMaster"] = {
            "status": "accepted",
            "explicitUserApproval": "approved dressed",
            "approvedAt": "2026-08-13T00:00:00+08:00",
            "evidenceSha256": [hashes[path] for path in gates["dressedMaster"]],
        }
        gate, status = verify_final_review_state(review, gates, hashes)
        self.assertEqual(("smallScale", "ready-for-review"), (gate, status))
        self.assertEqual("not-started", review["animationReady"]["status"])


if __name__ == "__main__":
    unittest.main()
