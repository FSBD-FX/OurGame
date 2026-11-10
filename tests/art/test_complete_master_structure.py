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

from complete_master_contract import (
    BODY_SHA256,
    FULL_SHA256,
    LANTERN_SHA256,
    PART_IDS,
    REVIEW_STAGES,
    MasterRequest,
)
from complete_master_structure import (
    BACK_INTERFACE_IDS,
    BODY_SUBJECT_IDS,
    JOINTS,
    build_structure_svg,
    render_structure_pack,
)
from complete_master_svg import discover_edge


def valid_request_dict() -> dict:
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


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def find_by_id(root: ET.Element, element_id: str) -> ET.Element:
    for element in root.iter():
        if element.get("id") == element_id:
            return element
    raise AssertionError(f"missing SVG id: {element_id}")


def subject_map(view: ET.Element) -> dict[str, ET.Element]:
    return {
        element.get("data-subject-id"): element
        for element in view.iter()
        if element.get("data-subject-id")
    }


def parse_box(value: str) -> tuple[int, int, int, int]:
    x, y, width, height = (int(item) for item in value.split())
    return x, y, width, height


def contains(box: tuple[int, int, int, int], point: tuple[int, int]) -> bool:
    x, y, width, height = box
    return x <= point[0] <= x + width and y <= point[1] <= y + height


class CompleteMasterStructureTests(unittest.TestCase):
    def build_document(self) -> ET.Element:
        request = MasterRequest.from_dict(valid_request_dict())
        return ET.fromstring(build_structure_svg(request))

    def test_uncovered_contains_all_sixteen_body_subjects(self):
        root = self.build_document()
        uncovered = subject_map(find_by_id(root, "structure-uncovered"))
        self.assertEqual(16, len(BODY_SUBJECT_IDS))
        self.assertEqual(set(BODY_SUBJECT_IDS), set(uncovered))

    def test_left_and_right_limb_subject_ids_are_distinct(self):
        self.assertEqual(6, len([item for item in BODY_SUBJECT_IDS if item.startswith("left-")]))
        self.assertEqual(6, len([item for item in BODY_SUBJECT_IDS if item.startswith("right-")]))
        self.assertFalse(
            set(item.removeprefix("left-") for item in BODY_SUBJECT_IDS if item.startswith("left-"))
            - set(item.removeprefix("right-") for item in BODY_SUBJECT_IDS if item.startswith("right-"))
        )

    def test_dressed_structure_reuses_uncovered_geometry_keys(self):
        root = self.build_document()
        uncovered = subject_map(find_by_id(root, "structure-uncovered"))
        dressed = subject_map(find_by_id(root, "structure-dressed"))
        self.assertEqual(set(BODY_SUBJECT_IDS), set(uncovered) & set(dressed))
        for part_id in BODY_SUBJECT_IDS:
            self.assertEqual(
                uncovered[part_id].get("data-geometry-key"),
                dressed[part_id].get("data-geometry-key"),
                part_id,
            )

    def test_back_detail_has_five_independent_interfaces(self):
        root = self.build_document()
        back = find_by_id(root, "structure-back-detail")
        actual = {
            element.get("data-interface-id")
            for element in back.iter()
            if element.get("data-interface-id")
        }
        self.assertEqual(set(BACK_INTERFACE_IDS), actual)
        boxes = [parse_box(element.get("data-bbox")) for element in back if element.get("data-bbox")]
        for index, left in enumerate(boxes):
            for right in boxes[index + 1 :]:
                lx, ly, lw, lh = left
                rx, ry, rw, rh = right
                overlap = lx < rx + rw and rx < lx + lw and ly < ry + rh and ry < ly + lh
                self.assertFalse(overlap, (left, right))

    def test_every_joint_point_lies_in_both_adjacent_subject_bounds(self):
        root = self.build_document()
        uncovered = subject_map(find_by_id(root, "structure-uncovered"))
        self.assertGreaterEqual(len(JOINTS), 13)
        for joint_id, first, second, point in JOINTS:
            with self.subTest(joint=joint_id):
                self.assertIn(first, uncovered)
                self.assertIn(second, uncovered)
                self.assertTrue(contains(parse_box(uncovered[first].get("data-bbox")), point))
                self.assertTrue(contains(parse_box(uncovered[second].get("data-bbox")), point))

    def test_source_uses_no_embedded_images_scripts_or_font_dependent_text(self):
        root = self.build_document()
        names = {local_name(element.tag) for element in root.iter()}
        self.assertNotIn("image", names)
        self.assertNotIn("script", names)
        self.assertNotIn("foreignObject", names)
        self.assertNotIn("text", names)
        self.assertFalse(any(local_name(attribute).lower() == "href" for element in root.iter() for attribute in element.attrib))

    def test_render_structure_pack_creates_four_fresh_rgba_review_images(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            external = Path(temp)
            (external / "request").mkdir(parents=True)
            (external / "request" / "master-request.json").write_text(
                json.dumps(valid_request_dict(), indent=2), encoding="utf-8"
            )
            report = render_structure_pack(external)
            self.assertEqual(4, report["files"])
            manifest = json.loads(
                (external / "evidence" / "structure-manifest.json").read_text(encoding="utf-8")
            )
            self.assertEqual(4, len(manifest["files"]))
            for item in manifest["files"]:
                self.assertEqual("review", item["role"])
                path = external / item["path"]
                with Image.open(path) as image:
                    self.assertEqual((1402, 1122), image.size)
                    self.assertEqual("RGBA", image.mode)
                    self.assertIsNotNone(image.getchannel("A").getbbox())
                    self.assertEqual(0, image.getpixel((0, 0))[3])


if __name__ == "__main__":
    unittest.main()
