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
from complete_master_contract import BODY_SHA256, FULL_SHA256, LANTERN_SHA256, PART_IDS, REVIEW_STAGES, MasterRequest
from complete_master_mantle import render_mantle_pack
from complete_master_spool_core import render_spool_core_pack
from complete_master_svg import discover_edge, validate_authoring_svg
from complete_master_weapon_hands import (
    WEAPON_HAND_PART_IDS,
    WEAPON_HAND_REVIEW_NAMES,
    build_weapon_hands_svg,
    compose_held,
    render_weapon_hands_pack,
)


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


class CompleteMasterWeaponHandsTests(unittest.TestCase):
    def request(self) -> MasterRequest:
        return MasterRequest.from_dict(request_dict())

    def test_contract_is_complete_weapon_plus_two_minimal_overlays(self):
        self.assertEqual(("half-bell-cleaver", "left-grip-overlay", "right-grip-overlay"), WEAPON_HAND_PART_IDS)
        self.assertEqual(("weapon-grip-parts", "weapon-unheld", "weapon-held"), WEAPON_HAND_REVIEW_NAMES)

    def test_svg_is_safe_and_weapon_contains_complete_handle_without_baked_hands(self):
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp) / "weapon-hands.svg"
            source.write_text(build_weapon_hands_svg(self.request()), encoding="utf-8")
            report = validate_authoring_svg(source, WEAPON_HAND_PART_IDS)
            self.assertEqual(list(WEAPON_HAND_PART_IDS), report["partIds"])
            root = ET.fromstring(source.read_text(encoding="utf-8"))
            parts = {item.get("data-part-id"): item for item in list(root) if item.get("data-part-id")}
            weapon = parts["half-bell-cleaver"]
            self.assertEqual("complete-half-bell-cleaver-v1", weapon.get("data-form"))
            self.assertEqual("complete-uncropped", weapon.get("data-handle"))
            self.assertEqual("none", weapon.get("data-baked-hands"))
            for overlay in ("left-grip-overlay", "right-grip-overlay"):
                self.assertEqual("minimal-finger-occlusion-v1", parts[overlay].get("data-form"))

    def test_grip_overlays_are_smaller_than_35_percent_of_complete_hands(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
            render_body_pack(root)
            render_spool_core_pack(root)
            render_mantle_pack(root)
            render_weapon_hands_pack(root)
            for hand, overlay in (("left-hand", "left-grip-overlay"), ("right-hand", "right-grip-overlay")):
                with Image.open(root / "layers" / f"{hand}.png") as hand_image, Image.open(root / "layers" / f"{overlay}.png") as overlay_image:
                    hand_area = sum(
                        1 for value in hand_image.getchannel("A").get_flattened_data() if value > 8
                    )
                    overlay_area = sum(
                        1 for value in overlay_image.getchannel("A").get_flattened_data() if value > 8
                    )
                    self.assertGreater(overlay_area, 0)
                    self.assertLess(overlay_area / hand_area, 0.35)

    def test_held_composite_changes_only_when_minimal_overlays_are_added(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
            render_body_pack(root)
            render_spool_core_pack(root)
            render_mantle_pack(root)
            render_weapon_hands_pack(root)
            held = compose_held(root / "layers", include_overlays=True)
            unheld = compose_held(root / "layers", include_overlays=False)
            difference = ImageChops.difference(held, unheld).convert("RGB")
            self.assertIsNotNone(difference.getbbox())
            held.close()
            unheld.close()

    def test_pack_outputs_three_layers_three_reviews_and_grip_connections(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
            render_body_pack(root)
            render_spool_core_pack(root)
            render_mantle_pack(root)
            report = render_weapon_hands_pack(root)
            self.assertEqual(6, report["files"])
            manifest = json.loads((root / "evidence" / "weaponHands-manifest.json").read_text(encoding="utf-8"))
            self.assertEqual(6, len(manifest["files"]))
            self.assertEqual(set(WEAPON_HAND_PART_IDS), {item["partId"] for item in manifest["files"] if item["role"] == "part" or item["role"] == "overlay"})
            self.assertEqual({"left-grip", "right-grip"}, set(manifest["connections"]))
            for item in manifest["files"]:
                with Image.open(root / item["path"]) as image:
                    self.assertEqual((1402, 1122), image.size)
                    self.assertEqual("RGBA", image.mode)
                    self.assertIsNotNone(image.getchannel("A").getbbox())


if __name__ == "__main__":
    unittest.main()
