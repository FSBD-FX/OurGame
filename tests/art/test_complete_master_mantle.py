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

from complete_master_body import render_body_pack
from complete_master_contract import BODY_SHA256, FULL_SHA256, LANTERN_SHA256, PART_IDS, REVIEW_STAGES, MasterRequest
from complete_master_mantle import (
    MANTLE_PART_IDS,
    MANTLE_REVIEW_NAMES,
    build_mantle_svg,
    count_alpha_components,
    render_mantle_pack,
)
from complete_master_spool_core import render_spool_core_pack
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
        "review": {stage: {"status": "not-started", "explicitUserApproval": None} for stage in REVIEW_STAGES},
    }


class CompleteMasterMantleTests(unittest.TestCase):
    def request(self) -> MasterRequest:
        return MasterRequest.from_dict(request_dict())

    def test_contract_has_four_independent_parts(self):
        self.assertEqual(
            ("shoulder-collar", "collar-fastener", "front-collar-flap", "mantle"),
            MANTLE_PART_IDS,
        )
        self.assertEqual(
            ("mantle-parts", "mantle-front-back", "mantle-mounted"),
            MANTLE_REVIEW_NAMES,
        )

    def test_svg_is_safe_and_mantle_is_one_continuous_weighted_cloth_subject(self):
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp) / "mantle.svg"
            source.write_text(build_mantle_svg(self.request()), encoding="utf-8")
            report = validate_authoring_svg(source, MANTLE_PART_IDS)
            self.assertEqual(list(MANTLE_PART_IDS), report["partIds"])
            root = ET.fromstring(source.read_text(encoding="utf-8"))
            parts = {item.get("data-part-id"): item for item in list(root) if item.get("data-part-id")}
            mantle = parts["mantle"]
            self.assertEqual("continuous-weighted-cloth-v1", mantle.get("data-form"))
            self.assertEqual("left-root-heavy-lag-v1", mantle.get("data-motion-read"))
            self.assertEqual("canvas-soft-top-left-v1", mantle.get("data-light-rig"))
            self.assertEqual("2", mantle.get("data-hole-count"))
            self.assertEqual("none", mantle.get("data-baked-hardware"))

    def test_rendered_mantle_is_one_alpha_component_with_two_transparent_holes(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
            render_body_pack(root)
            render_spool_core_pack(root)
            render_mantle_pack(root)
            with Image.open(root / "layers" / "mantle.png") as image:
                alpha = image.getchannel("A")
                self.assertEqual(1, count_alpha_components(alpha, threshold=8))
                for point in ((347, 650), (430, 820)):
                    self.assertEqual(0, alpha.getpixel(point))
                for point in ((365, 650), (465, 820)):
                    self.assertGreater(alpha.getpixel(point), 0)

    def test_mantle_alpha_materially_occludes_far_arm_and_leg(self):
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
            render_mantle_pack(root)
            with (
                Image.open(root / "layers" / "mantle.png") as mantle,
                Image.open(root / "layers" / "left-upper-arm.png") as arm,
                Image.open(root / "layers" / "left-thigh.png") as thigh,
            ):
                mantle_alpha = mantle.getchannel("A").point(lambda value: 255 if value > 8 else 0)
                for label, body_layer, minimum in (
                    ("far arm", arm, 0.45),
                    ("far thigh", thigh, 0.35),
                ):
                    body_alpha = body_layer.getchannel("A").point(
                        lambda value: 255 if value > 8 else 0
                    )
                    overlap = Image.new("L", body_alpha.size, 0)
                    overlap.paste(body_alpha, mask=mantle_alpha)
                    visible_pixels = sum(body_alpha.histogram()[1:])
                    overlap_pixels = sum(overlap.histogram()[1:])
                    with self.subTest(subject=label):
                        self.assertGreaterEqual(overlap_pixels / visible_pixels, minimum)

    def test_render_pack_outputs_four_parts_three_reviews_and_root_connection(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
            render_body_pack(root)
            render_spool_core_pack(root)
            report = render_mantle_pack(root)
            self.assertEqual(7, report["files"])
            manifest = json.loads((root / "evidence" / "mantle-manifest.json").read_text(encoding="utf-8"))
            self.assertEqual(7, len(manifest["files"]))
            self.assertEqual(set(MANTLE_PART_IDS), {item["partId"] for item in manifest["files"] if item["role"] == "part"})
            self.assertEqual(set(MANTLE_REVIEW_NAMES), {Path(item["path"]).stem for item in manifest["files"] if item["role"] == "review"})
            root_point = tuple(manifest["connections"]["mantle-root"]["point"])
            self.assertEqual("mantle", manifest["connections"]["mantle-root"]["partId"])
            with Image.open(root / "layers" / "mantle.png") as mantle, Image.open(root / "layers" / "shoulder-collar.png") as collar:
                self.assertGreater(mantle.getpixel(root_point)[3], 0)
                self.assertGreater(collar.getpixel(root_point)[3], 0)
            for item in manifest["files"]:
                with Image.open(root / item["path"]) as image:
                    self.assertEqual((1402, 1122), image.size)
                    self.assertEqual("RGBA", image.mode)
                    self.assertIsNotNone(image.getchannel("A").getbbox())


if __name__ == "__main__":
    unittest.main()
