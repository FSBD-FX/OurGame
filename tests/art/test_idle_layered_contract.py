from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "art"))

from idle_layered_contract import RigConfig, build_default_timeline, write_default_timeline


BODY_HASH = "0358BFEA48D6FF79271A5B14AE03B0396361748E60EB2D9D36EF6A696D7B1B44"
LANTERN_HASH = "C313BFAAF1A1B8C9994AA6FEBC96D5A9E5C82F740B58305FF3C96696DCC73AB0"


def valid_config() -> dict:
    return {
        "version": 1,
        "frames": 12,
        "fps": 8,
        "canvas": [256, 256],
        "origin": [128, 231],
        "bodySource": {"path": "body.png", "sha256": BODY_HASH},
        "lanternSource": {"path": "lantern.png", "sha256": LANTERN_HASH},
        "rigidScale": [1.0, 1.0],
    }


class IdleLayeredContractTests(unittest.TestCase):
    def test_default_timeline_is_twelve_frames_with_bounded_rigid_motion(self) -> None:
        poses = build_default_timeline()

        self.assertEqual(12, len(poses))
        self.assertEqual(list(range(12)), [pose.index for pose in poses])
        self.assertTrue(all(abs(pose.chassis_y) <= 2.0 for pose in poses))
        self.assertTrue(all(0.0 <= pose.core_energy <= 1.0 for pose in poses))

    def test_medium_pulse_reaches_root_then_middle_then_tip(self) -> None:
        poses = build_default_timeline()

        root_peak = max(range(4, 9), key=lambda i: abs(poses[i].mantle_root[1]))
        middle_peak = max(range(4, 10), key=lambda i: abs(poses[i].mantle_mid[1]))
        tip_peak = max(range(4, 11), key=lambda i: abs(poses[i].mantle_tip[1]))

        self.assertEqual((4, 5, 6), (root_peak, middle_peak, tip_peak))

    def test_lantern_pulse_lags_mantle_tip(self) -> None:
        poses = build_default_timeline()
        tip_peak = max(range(4, 11), key=lambda i: abs(poses[i].mantle_tip[1]))
        lantern_peak = max(range(4, 12), key=lambda i: abs(poses[i].lantern_offset[1]))

        self.assertEqual(6, tip_peak)
        self.assertEqual(7, lantern_peak)

    def test_loop_boundary_is_no_larger_than_an_internal_step(self) -> None:
        poses = build_default_timeline()
        series = [
            [pose.chassis_y for pose in poses],
            [pose.chassis_deg for pose in poses],
            [pose.weapon_deg for pose in poses],
            [pose.mantle_tip[0] for pose in poses],
            [pose.mantle_tip[1] for pose in poses],
            [pose.lantern_offset[0] for pose in poses],
            [pose.lantern_offset[1] for pose in poses],
            [pose.lantern_deg for pose in poses],
        ]

        for values in series:
            internal_max = max(abs(values[i + 1] - values[i]) for i in range(11))
            self.assertLessEqual(abs(values[0] - values[-1]), internal_max + 1e-9)

    def test_config_rejects_malformed_source_hash(self) -> None:
        raw = valid_config()
        raw["bodySource"]["sha256"] = "bad"

        with self.assertRaisesRegex(ValueError, "bodySource.sha256"):
            RigConfig.from_dict(raw)

    def test_config_rejects_non_unit_rigid_scale(self) -> None:
        raw = valid_config()
        raw["rigidScale"] = [1.01, 1.0]

        with self.assertRaisesRegex(ValueError, "rigidScale"):
            RigConfig.from_dict(raw)

    def test_config_rejects_wrong_timing_contract(self) -> None:
        raw = valid_config()
        raw["frames"] = 16

        with self.assertRaisesRegex(ValueError, "frames"):
            RigConfig.from_dict(raw)

    def test_timeline_writer_emits_exact_review_contract(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "timeline.json"
            write_default_timeline(path)
            raw = json.loads(path.read_text(encoding="utf-8"))

        self.assertEqual(1, raw["version"])
        self.assertEqual(12, raw["frames"])
        self.assertEqual(8, raw["fps"])
        self.assertEqual([120, 130] * 6, raw["gifDurationsMs"])
        self.assertEqual(12, len(raw["poses"]))
        self.assertTrue(all(pose["rigidScale"] == [1.0, 1.0] for pose in raw["poses"]))


if __name__ == "__main__":
    unittest.main()
