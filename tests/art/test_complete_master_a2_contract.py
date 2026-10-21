from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "art"))

from complete_master_contract import PART_IDS
from complete_master_a2_contract import (
    A2_DRESSED_STACK,
    A2_GATE_ORDER,
    FAR_BODY_PART_IDS,
    NEAR_BODY_PART_IDS,
    ensure_a2_review_state,
)


class CompleteMasterA2ContractTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        evidence = self.root / "evidence"
        evidence.mkdir(parents=True)
        self.legacy_review_path = evidence / "review-state.json"
        self.legacy_review_path.write_text(
            json.dumps(
                {
                    "version": 1,
                    "dressedMaster": {
                        "status": "ready-for-review",
                        "explicitUserApproval": None,
                    },
                },
                indent=2,
            )
            + "\n",
            encoding="utf-8",
        )
        self.legacy_review_bytes = self.legacy_review_path.read_bytes()

    def test_a2_keeps_historical_review_state_separate(self):
        path = ensure_a2_review_state(self.root)

        self.assertEqual(
            "evidence/a2-review-state.json",
            path.relative_to(self.root).as_posix(),
        )
        self.assertEqual(
            ["a2Structure", "a2DressedMaster", "a2Readiness"],
            list(json.loads(path.read_text(encoding="utf-8"))),
        )
        self.assertEqual(
            self.legacy_review_bytes,
            self.legacy_review_path.read_bytes(),
        )

    def test_existing_a2_review_state_is_not_rewritten(self):
        path = self.root / "evidence" / "a2-review-state.json"
        path.write_text('{"sentinel": true}\n', encoding="utf-8")
        original = path.read_bytes()

        self.assertEqual(path, ensure_a2_review_state(self.root))
        self.assertEqual(original, path.read_bytes())

    def test_a2_stack_occludes_far_body_with_mantle(self):
        self.assertEqual(
            ("a2Structure", "a2DressedMaster", "a2Readiness"),
            A2_GATE_ORDER,
        )
        self.assertEqual(
            (
                "left-thigh",
                "left-shin",
                "left-boot",
                "left-upper-arm",
                "left-forearm",
                "left-hand",
            ),
            FAR_BODY_PART_IDS,
        )
        self.assertEqual(
            (
                "right-thigh",
                "right-shin",
                "right-boot",
                "right-upper-arm",
                "right-forearm",
                "right-hand",
            ),
            NEAR_BODY_PART_IDS,
        )
        self.assertLess(
            A2_DRESSED_STACK.index("left-upper-arm"),
            A2_DRESSED_STACK.index("mantle"),
        )
        self.assertLess(
            A2_DRESSED_STACK.index("left-thigh"),
            A2_DRESSED_STACK.index("mantle"),
        )
        self.assertLess(
            A2_DRESSED_STACK.index("mantle"),
            A2_DRESSED_STACK.index("hidden-axle"),
        )
        self.assertLess(
            A2_DRESSED_STACK.index("hidden-axle"),
            A2_DRESSED_STACK.index("torso-shell"),
        )
        self.assertLess(
            A2_DRESSED_STACK.index("collar-fastener"),
            A2_DRESSED_STACK.index("back-core-housing"),
        )
        self.assertLess(
            A2_DRESSED_STACK.index("back-core-glow"),
            A2_DRESSED_STACK.index("front-spool-housing"),
        )
        self.assertLess(
            A2_DRESSED_STACK.index("torso-shell"),
            A2_DRESSED_STACK.index("front-spool-housing"),
        )
        self.assertEqual(set(PART_IDS), set(A2_DRESSED_STACK))
        self.assertEqual(len(PART_IDS), len(A2_DRESSED_STACK))


if __name__ == "__main__":
    unittest.main()
