from __future__ import annotations

import json
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "art"))

from complete_master_body import (
    BODY_PART_IDS,
    BODY_REVIEW_NAMES,
    BODY_STACK,
    CERAMIC_PART_IDS,
    build_body_svg,
    render_body_pack,
)
from complete_master_contract import (
    BODY_SHA256,
    FULL_SHA256,
    LANTERN_SHA256,
    PART_IDS,
    REVIEW_STAGES,
    MasterRequest,
)
from complete_master_structure import BODY_SUBJECT_IDS, JOINTS
from complete_master_svg import discover_edge, validate_authoring_svg


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
        "review": {
            stage: {"status": "not-started", "explicitUserApproval": None}
            for stage in REVIEW_STAGES
        },
    }


class CompleteMasterBodyTests(unittest.TestCase):
    def request(self) -> MasterRequest:
        return MasterRequest.from_dict(request_dict())

    def test_body_part_contract_is_exact_and_stack_keeps_inner_body_behind_shells(self):
        self.assertEqual(tuple(BODY_SUBJECT_IDS), BODY_PART_IDS)
        self.assertEqual(16, len(BODY_PART_IDS))
        self.assertEqual(set(BODY_PART_IDS), set(BODY_STACK))
        self.assertEqual("inner-body", BODY_STACK[0])
        self.assertLess(BODY_STACK.index("left-hand"), BODY_STACK.index("right-hand"))

    def test_body_svg_passes_safe_authoring_contract(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "body.svg"
            path.write_text(build_body_svg(self.request()), encoding="utf-8")
            report = validate_authoring_svg(path, BODY_PART_IDS)
            self.assertEqual(list(BODY_PART_IDS), report["partIds"])

    def test_every_part_declares_nonempty_bounds_and_all_joint_points_overlap(self):
        root = ET.fromstring(build_body_svg(self.request()))
        parts = {
            element.get("data-part-id"): element
            for element in list(root)
            if element.get("data-part-id")
        }
        self.assertEqual(set(BODY_PART_IDS), set(parts))
        bounds = {}
        for part_id, element in parts.items():
            values = tuple(int(value) for value in element.get("data-bbox").split())
            self.assertEqual(4, len(values))
            self.assertGreater(values[2], 0)
            self.assertGreater(values[3], 0)
            bounds[part_id] = values
        for joint_id, first, second, (x, y) in JOINTS:
            with self.subTest(joint=joint_id):
                for part_id in (first, second):
                    bx, by, width, height = bounds[part_id]
                    self.assertTrue(bx <= x <= bx + width and by <= y <= by + height)

    def test_hands_are_articulated_paths_not_round_placeholders(self):
        root = ET.fromstring(build_body_svg(self.request()))
        parts = {
            element.get("data-part-id"): element
            for element in list(root)
            if element.get("data-part-id")
        }
        for hand_id in ("left-hand", "right-hand"):
            with self.subTest(hand=hand_id):
                hand = parts[hand_id]
                self.assertEqual("articulated-glove-v2", hand.get("data-form"))
                child_tags = {child.tag.rsplit("}", 1)[-1] for child in hand}
                self.assertIn("path", child_tags)
                self.assertNotIn("ellipse", child_tags)

    def test_all_ceramic_parts_use_one_top_left_ceramic_gradient(self):
        root = ET.fromstring(build_body_svg(self.request()))
        gradients = {
            item.get("id"): item
            for item in root.iter()
            if item.tag.rsplit("}", 1)[-1] == "linearGradient"
        }
        self.assertIn("ceramic-main", gradients)
        self.assertNotIn("ceramic-far", gradients)
        gradient = gradients["ceramic-main"]
        self.assertEqual("userSpaceOnUse", gradient.get("gradientUnits"))
        self.assertEqual(
            ("500", "220", "900", "1080"),
            tuple(gradient.get(key) for key in ("x1", "y1", "x2", "y2")),
        )
        parts = {
            item.get("data-part-id"): item
            for item in list(root)
            if item.get("data-part-id")
        }
        self.assertEqual(
            {"canvas-soft-top-left-v1"},
            {parts[part_id].get("data-light-rig") for part_id in CERAMIC_PART_IDS},
        )
        for part_id in CERAMIC_PART_IDS:
            with self.subTest(part=part_id):
                serialized = ET.tostring(parts[part_id], encoding="unicode")
                self.assertIn("url(#ceramic-main)", serialized)
                self.assertNotIn("url(#ceramic-far)", serialized)

    def test_body_has_joint_contact_shadows_and_clean_a2_torso(self):
        root = ET.fromstring(build_body_svg(self.request()))
        roles = [item.get("data-effect") for item in root.iter() if item.get("data-effect")]
        self.assertGreaterEqual(roles.count("joint-contact-shadow"), 8)
        self.assertNotIn("core-bounce-restrained", roles)
        torso = next(
            item for item in list(root) if item.get("data-part-id") == "torso-shell"
        )
        self.assertEqual("clean-ceramic-torso-a2", torso.get("data-form"))

    def test_far_and_near_body_depth_is_metadata_not_a_second_material(self):
        root = ET.fromstring(build_body_svg(self.request()))
        parts = {
            item.get("data-part-id"): item
            for item in list(root)
            if item.get("data-part-id")
        }
        for part_id in (
            "left-upper-arm", "left-forearm", "left-thigh", "left-shin", "left-boot"
        ):
            self.assertEqual("far-occluded-a2", parts[part_id].get("data-depth-role"))
        for part_id in (
            "right-upper-arm", "right-forearm", "right-thigh", "right-shin", "right-boot"
        ):
            self.assertEqual("near-a2", parts[part_id].get("data-depth-role"))

    def test_render_body_pack_outputs_sixteen_layers_and_three_reviews(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(
                json.dumps(request_dict(), indent=2), encoding="utf-8"
            )
            report = render_body_pack(root)
            self.assertEqual(19, report["files"])
            manifest = json.loads(
                (root / "evidence" / "body-manifest.json").read_text(encoding="utf-8")
            )
            self.assertEqual(19, len(manifest["files"]))
            part_records = [item for item in manifest["files"] if item["role"] == "part"]
            review_records = [item for item in manifest["files"] if item["role"] == "review"]
            self.assertEqual(set(BODY_PART_IDS), {item["partId"] for item in part_records})
            self.assertEqual(set(BODY_REVIEW_NAMES), {Path(item["path"]).stem for item in review_records})
            for item in manifest["files"]:
                with Image.open(root / item["path"]) as image:
                    self.assertEqual((1402, 1122), image.size)
                    self.assertEqual("RGBA", image.mode)
                    self.assertIsNotNone(image.getchannel("A").getbbox())
            for boot in ("left-boot", "right-boot"):
                with Image.open(root / "layers" / f"{boot}.png") as image:
                    self.assertEqual(1081, image.getchannel("A").getbbox()[3])
            with Image.open(root / "layers" / "inner-body.png") as inner_body:
                alpha = inner_body.getchannel("A")
                for joint_id, _first, _second, point in JOINTS:
                    with self.subTest(inner_body_joint=joint_id):
                        self.assertGreater(alpha.getpixel(point), 0)

    def test_body_manifest_records_both_sides_of_each_joint(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(
                json.dumps(request_dict(), indent=2), encoding="utf-8"
            )
            render_body_pack(root)
            manifest = json.loads(
                (root / "evidence" / "body-manifest.json").read_text(encoding="utf-8")
            )
            for joint_id, first, second, point in JOINTS:
                self.assertEqual(
                    {first, second},
                    {
                        manifest["connections"][f"{joint_id}.a"]["partId"],
                        manifest["connections"][f"{joint_id}.b"]["partId"],
                    },
                )
                self.assertEqual(point, tuple(manifest["connections"][f"{joint_id}.a"]["point"]))
                self.assertEqual(point, tuple(manifest["connections"][f"{joint_id}.b"]["point"]))


if __name__ == "__main__":
    unittest.main()
