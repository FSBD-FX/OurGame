from __future__ import annotations

import json
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageChops


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "art"))

from complete_master_body import render_body_pack
from complete_master_contract import (
    BODY_SHA256,
    FULL_SHA256,
    LANTERN_SHA256,
    PART_IDS,
    REVIEW_STAGES,
    MasterRequest,
    sha256_file,
)
from complete_master_spool_core import (
    BOUNDS,
    SPOOL_CORE_PART_IDS,
    SPOOL_CORE_REVIEW_NAMES,
    SPOOL_CORE_STACK,
    build_back_detail,
    build_spool_core_svg,
    compose_mounted,
    render_spool_core_pack,
)
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


def _inside(inner: tuple[int, int, int, int], outer: tuple[int, int, int, int]) -> bool:
    return (
        outer[0] <= inner[0]
        and outer[1] <= inner[1]
        and inner[2] <= outer[2]
        and inner[3] <= outer[3]
    )


def _xyxy(box: tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    x, y, width, height = box
    return (x, y, x + width, y + height)


def _overlaps(first: tuple[int, int, int, int], second: tuple[int, int, int, int]) -> bool:
    return not (
        first[2] <= second[0]
        or second[2] <= first[0]
        or first[3] <= second[1]
        or second[3] <= first[1]
    )


class CompleteMasterSpoolCoreTests(unittest.TestCase):
    def request(self) -> MasterRequest:
        return MasterRequest.from_dict(request_dict())

    def test_contract_is_exact_and_stack_places_axle_and_core_behind_body(self):
        self.assertEqual(
            (
                "front-spool-housing",
                "front-spool-disc",
                "front-spool-glow",
                "back-core-housing",
                "back-energy-cell",
                "back-core-glow",
                "hidden-axle",
            ),
            SPOOL_CORE_PART_IDS,
        )
        self.assertEqual(7, len(SPOOL_CORE_PART_IDS))
        self.assertLess(SPOOL_CORE_STACK.index("hidden-axle"), SPOOL_CORE_STACK.index("body"))
        self.assertLess(SPOOL_CORE_STACK.index("back-core-housing"), SPOOL_CORE_STACK.index("body"))
        self.assertGreater(SPOOL_CORE_STACK.index("front-spool-disc"), SPOOL_CORE_STACK.index("body"))

    def test_svg_is_safe_and_declares_open_cradle_and_separate_energy_parts(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "spool-core.svg"
            path.write_text(build_spool_core_svg(self.request()), encoding="utf-8")
            report = validate_authoring_svg(path, SPOOL_CORE_PART_IDS)
            self.assertEqual(list(SPOOL_CORE_PART_IDS), report["partIds"])
            root = ET.fromstring(path.read_text(encoding="utf-8"))
            parts = {
                item.get("data-part-id"): item
                for item in list(root)
                if item.get("data-part-id")
            }
            self.assertEqual("shoulder-front-spool-a2", parts["front-spool-housing"].get("data-form"))
            self.assertEqual("rotating-disc-v1", parts["front-spool-disc"].get("data-form"))
            self.assertEqual("shoulder-back-core-a2", parts["back-core-housing"].get("data-form"))
            self.assertEqual("hidden-shoulder-axle-a2", parts["hidden-axle"].get("data-form"))
            self.assertEqual(
                {"front-spool-glow", "back-core-glow"},
                {part_id for part_id, item in parts.items() if item.get("data-energy") == "glow-only"},
            )

    def test_front_spool_and_back_core_share_a2_shoulder_axis_and_size(self):
        head_diameter = 240
        front = BOUNDS["front-spool-housing"]
        back = BOUNDS["back-core-housing"]
        front_center = (front[0] + front[2] / 2, front[1] + front[3] / 2)
        back_center = (back[0] + back[2] / 2, back[1] + back[3] / 2)

        self.assertTrue(0.28 <= front[2] / head_diameter <= 0.34)
        self.assertTrue(0.24 <= back[2] / head_diameter <= 0.30)
        self.assertLessEqual(abs(front_center[1] - 490), 6)
        self.assertLessEqual(abs(back_center[1] - 490), 6)
        self.assertLess(back_center[0], front_center[0])

    def test_a2_devices_clear_head_and_near_arm_motion_space(self):
        front = _xyxy(BOUNDS["front-spool-housing"])
        back = _xyxy(BOUNDS["back-core-housing"])
        head_clearance = (590, 230, 830, 440)
        near_arm_motion = (790, 450, 900, 700)

        self.assertFalse(_overlaps(front, head_clearance))
        self.assertFalse(_overlaps(back, head_clearance))
        self.assertFalse(_overlaps(front, near_arm_motion))

    def test_declared_glow_and_disc_bounds_fit_their_housings(self):
        root = ET.fromstring(build_spool_core_svg(self.request()))
        boxes = {}
        for item in list(root):
            part_id = item.get("data-part-id")
            if part_id:
                x, y, width, height = (int(value) for value in item.get("data-bbox").split())
                boxes[part_id] = (x, y, x + width, y + height)
        self.assertTrue(_inside(boxes["front-spool-disc"], boxes["front-spool-housing"]))
        self.assertTrue(_inside(boxes["front-spool-glow"], boxes["front-spool-disc"]))
        self.assertTrue(_inside(boxes["back-energy-cell"], boxes["back-core-housing"]))
        self.assertTrue(_inside(boxes["back-core-glow"], boxes["back-energy-cell"]))
        front_clearance = boxes["front-spool-disc"][0] - boxes["front-spool-housing"][0]
        self.assertGreaterEqual(front_clearance, 12)

    def test_render_pack_preserves_body_and_outputs_seven_parts_three_reviews(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(
                json.dumps(request_dict(), indent=2), encoding="utf-8"
            )
            render_body_pack(root)
            body_hashes = {
                path.name: sha256_file(path)
                for path in (root / "layers").glob("*.png")
            }
            report = render_spool_core_pack(root)
            self.assertEqual(10, report["files"])
            self.assertEqual(
                body_hashes,
                {
                    path.name: sha256_file(path)
                    for path in (root / "layers").glob("*.png")
                    if path.name in body_hashes
                },
            )
            manifest = json.loads(
                (root / "evidence" / "spoolCore-manifest.json").read_text(encoding="utf-8")
            )
            self.assertEqual(10, len(manifest["files"]))
            self.assertEqual(
                set(SPOOL_CORE_PART_IDS),
                {item["partId"] for item in manifest["files"] if item["role"] == "part"},
            )
            self.assertEqual(
                set(SPOOL_CORE_REVIEW_NAMES),
                {Path(item["path"]).stem for item in manifest["files"] if item["role"] == "review"},
            )
            for item in manifest["files"]:
                with Image.open(root / item["path"]) as image:
                    self.assertEqual((1402, 1122), image.size)
                    self.assertEqual("RGBA", image.mode)
                    self.assertIsNotNone(image.getchannel("A").getbbox())

    def test_hidden_axle_is_occluded_when_mounted_but_visible_in_back_detail(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(
                json.dumps(request_dict(), indent=2), encoding="utf-8"
            )
            render_body_pack(root)
            render_spool_core_pack(root)
            mounted_with = compose_mounted(root / "layers", include_hidden_axle=True)
            mounted_without = compose_mounted(root / "layers", include_hidden_axle=False)
            self.assertIsNone(
                ImageChops.difference(mounted_with, mounted_without).convert("RGB").getbbox()
            )
            mounted_with.close()
            mounted_without.close()
            detail_with = build_back_detail(root / "layers", include_hidden_axle=True)
            detail_without = build_back_detail(root / "layers", include_hidden_axle=False)
            self.assertIsNotNone(
                ImageChops.difference(detail_with, detail_without).convert("RGB").getbbox()
            )
            detail_with.close()
            detail_without.close()


if __name__ == "__main__":
    unittest.main()
