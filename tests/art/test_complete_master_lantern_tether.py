from __future__ import annotations

import json
import math
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageChops


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "art"))

from complete_master_body import render_body_pack
from complete_master_contract import BODY_SHA256, FULL_SHA256, LANTERN_SHA256, PART_IDS, REVIEW_STAGES, MasterRequest, sha256_file
from complete_master_lantern_tether import (
    LANTERN_TETHER_PART_IDS,
    LANTERN_TETHER_REVIEW_NAMES,
    TETHER_CONNECTIONS,
    build_lantern_tether_svg,
    compose_lantern_system,
    render_lantern_tether_pack,
)
from complete_master_mantle import render_mantle_pack
from complete_master_spool_core import render_spool_core_pack
from complete_master_svg import discover_edge, validate_authoring_svg
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


def prepare_prerequisites(root: Path) -> None:
    render_body_pack(root)
    render_spool_core_pack(root)
    render_mantle_pack(root)
    render_weapon_hands_pack(root)


class CompleteMasterLanternTetherTests(unittest.TestCase):
    def request(self) -> MasterRequest:
        return MasterRequest.from_dict(request_dict())

    def test_contract_has_complete_lantern_and_three_tether_segments(self):
        self.assertEqual(
            ("lantern-housing", "lantern-glow", "tether-back-connector", "tether-energy-middle", "tether-lantern-connector"),
            LANTERN_TETHER_PART_IDS,
        )
        self.assertEqual(("lantern-tether-parts", "lantern-tether-connected", "lantern-tether-energy-off"), LANTERN_TETHER_REVIEW_NAMES)

    def test_svg_is_safe_and_declares_independent_housing_glow_and_connectors(self):
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp) / "lantern-tether.svg"
            source.write_text(build_lantern_tether_svg(self.request()), encoding="utf-8")
            report = validate_authoring_svg(source, LANTERN_TETHER_PART_IDS)
            self.assertEqual(list(LANTERN_TETHER_PART_IDS), report["partIds"])
            root = ET.fromstring(source.read_text(encoding="utf-8"))
            parts = {item.get("data-part-id"): item for item in list(root) if item.get("data-part-id")}
            self.assertEqual("complete-floating-lantern-v1", parts["lantern-housing"].get("data-form"))
            self.assertEqual("glow-only", parts["lantern-glow"].get("data-energy"))
            self.assertEqual("energy-only", parts["tether-energy-middle"].get("data-energy"))
            self.assertEqual(
                "shoulder-back-core-terminal-a2",
                parts["tether-back-connector"].get("data-form"),
            )
            self.assertEqual(
                "solid-terminal-v1",
                parts["tether-lantern-connector"].get("data-form"),
            )

    def test_declared_tether_endpoints_join_within_two_pixels(self):
        self.assertEqual((541, 490), TETHER_CONNECTIONS["back.in"])
        self.assertEqual((520, 430), TETHER_CONNECTIONS["back.out"])
        pairs = (("back.out", "energy.in"), ("energy.out", "lantern.in"))
        for first, second in pairs:
            distance = math.dist(TETHER_CONNECTIONS[first], TETHER_CONNECTIONS[second])
            self.assertLessEqual(distance, 2.0)

    def test_a2_tether_mounts_on_back_core_and_energy_clears_head_and_front_spool(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        locked_hashes = {
            "half-bell-cleaver": "06E657BB4E3831473688E5CFE7B6BB4FF2FD2A80D77C4EE17514D4B2325C325A",
            "left-hand": "4CBBA9E8E8BD31136E6B1D105C40CFFC03E568EE967C87C3BC66058FD36289C7",
            "right-hand": "CF46FACA5382CCA7FCE705304D2DE9800A4DD674C044CBC7A2122AA401E2D428",
            "left-grip-overlay": "058DB41E707619C25D1416DD5EDA4BD6BC00F8407DE2EBDF18B373D8FA7A6EBE",
            "right-grip-overlay": "AA83FADEF3D3DE26B3D004D7353D959FB4ED26CAE10C76CCC68A807ADAC37BD4",
            "lantern-housing": "E44108207A69C2DAF5BCDC9BFCB54284A7CAE9FA2839667AEB49E2E40AD62F5A",
            "lantern-glow": "6A4CF5EDB99D6931DFD42325595F6E8F0D6A1A86E738581ADBDF2AA9C379FDDA",
        }
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(
                json.dumps(request_dict(), indent=2), encoding="utf-8"
            )
            prepare_prerequisites(root)
            render_lantern_tether_pack(root)
            layers = root / "layers"
            with (
                Image.open(layers / "back-core-housing.png") as core,
                Image.open(layers / "tether-energy-middle.png") as energy,
                Image.open(layers / "head-shell.png") as head,
                Image.open(layers / "front-spool-housing.png") as spool,
            ):
                self.assertGreater(core.getpixel(TETHER_CONNECTIONS["back.in"])[3], 0)
                energy_alpha = energy.getchannel("A")
                self.assertIsNone(ImageChops.multiply(energy_alpha, head.getchannel("A")).getbbox())
                self.assertIsNone(ImageChops.multiply(energy_alpha, spool.getchannel("A")).getbbox())
            self.assertEqual(
                locked_hashes,
                {
                    part_id: sha256_file(layers / f"{part_id}.png")
                    for part_id in locked_hashes
                },
            )

    def test_rendered_glow_stays_inside_housing_and_housing_does_not_touch_head(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
            prepare_prerequisites(root)
            render_lantern_tether_pack(root)
            with Image.open(root / "layers" / "lantern-housing.png") as housing, Image.open(root / "layers" / "lantern-glow.png") as glow, Image.open(root / "layers" / "head-shell.png") as head:
                housing_alpha = housing.getchannel("A")
                glow_alpha = glow.getchannel("A")
                head_alpha = head.getchannel("A")
                housing_box = housing_alpha.getbbox()
                glow_box = glow_alpha.getbbox()
                self.assertIsNotNone(housing_box)
                self.assertIsNotNone(glow_box)
                self.assertTrue(
                    housing_box[0] <= glow_box[0]
                    and housing_box[1] <= glow_box[1]
                    and glow_box[2] <= housing_box[2]
                    and glow_box[3] <= housing_box[3]
                )
                overlap = ImageChops.multiply(housing_alpha, head_alpha)
                self.assertIsNone(overlap.getbbox())

    def test_energy_off_keeps_both_solid_connectors_visible(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
            prepare_prerequisites(root)
            render_lantern_tether_pack(root)
            energy_off = compose_lantern_system(root / "layers", energy_on=False)
            for point in (TETHER_CONNECTIONS["back.in"], TETHER_CONNECTIONS["lantern.out"]):
                self.assertGreater(energy_off.getpixel(point)[3], 0)
            energy_off.close()

    def test_pack_outputs_five_layers_three_reviews_and_connection_points(self):
        if discover_edge() is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "request").mkdir(parents=True)
            (root / "request" / "master-request.json").write_text(json.dumps(request_dict(), indent=2), encoding="utf-8")
            prepare_prerequisites(root)
            report = render_lantern_tether_pack(root)
            self.assertEqual(8, report["files"])
            manifest = json.loads((root / "evidence" / "lanternTether-manifest.json").read_text(encoding="utf-8"))
            self.assertEqual(8, len(manifest["files"]))
            self.assertEqual(set(LANTERN_TETHER_PART_IDS), {item["partId"] for item in manifest["files"] if item["role"] == "part"})
            self.assertEqual(set(TETHER_CONNECTIONS), set(manifest["connections"]))
            for item in manifest["files"]:
                with Image.open(root / item["path"]) as image:
                    self.assertEqual((1402, 1122), image.size)
                    self.assertEqual("RGBA", image.mode)
                    self.assertIsNotNone(image.getchannel("A").getbbox())


if __name__ == "__main__":
    unittest.main()
