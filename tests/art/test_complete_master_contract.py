from __future__ import annotations

import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "art"))

from complete_master_contract import (
    BODY_SHA256,
    FULL_SHA256,
    LANTERN_SHA256,
    OVERLAY_PART_IDS,
    PART_IDS,
    SUBJECT_PART_IDS,
    MasterRequest,
)
from complete_master_workspace import bootstrap_workspace


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def valid_request() -> dict:
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
            for stage in (
                "structure",
                "body",
                "spoolCore",
                "mantle",
                "weaponHands",
                "lanternTether",
                "dressedMaster",
                "smallScale",
                "animationReady",
            )
        },
    }


class CompleteMasterContractTests(unittest.TestCase):
    def test_every_subject_part_is_unique_and_overlays_are_not_subjects(self) -> None:
        self.assertEqual(35, len(PART_IDS))
        self.assertEqual(len(PART_IDS), len(set(PART_IDS)))
        self.assertNotIn("left-grip-overlay", SUBJECT_PART_IDS)
        self.assertNotIn("right-grip-overlay", SUBJECT_PART_IDS)
        self.assertIn("left-grip-overlay", OVERLAY_PART_IDS)
        self.assertIn("right-grip-overlay", OVERLAY_PART_IDS)
        self.assertEqual(set(PART_IDS), set(SUBJECT_PART_IDS) | set(OVERLAY_PART_IDS))

    def test_request_enforces_canvas_baseline_facing_and_ordered_parts(self) -> None:
        request = valid_request()

        parsed = MasterRequest.from_dict(request)

        self.assertEqual((1402, 1122), parsed.canvas)
        self.assertEqual(1080, parsed.baseline_y)
        self.assertEqual("right", parsed.facing)
        self.assertEqual(PART_IDS, parsed.parts)

    def test_request_rejects_wrong_source_hash(self) -> None:
        request = valid_request()
        request["sources"]["body"]["sha256"] = "0" * 64

        with self.assertRaisesRegex(ValueError, "sources.body.sha256"):
            MasterRequest.from_dict(request)

    def test_acceptance_cannot_be_created_without_explicit_review(self) -> None:
        request = valid_request()
        request["review"]["structure"] = {
            "status": "accepted",
            "explicitUserApproval": None,
        }

        with self.assertRaisesRegex(ValueError, "explicitUserApproval"):
            MasterRequest.from_dict(request)

    def test_request_rejects_unknown_review_status(self) -> None:
        request = valid_request()
        request["review"]["body"]["status"] = "passed"

        with self.assertRaisesRegex(ValueError, "review.body.status"):
            MasterRequest.from_dict(request)

    def test_workspace_copies_sources_byte_for_byte_and_preserves_request(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            project = root / "project"
            external = root / "external"
            body = project / "docs" / "art" / "player" / "reference" / "thread-spool-bearer-body.png"
            full = project / "docs" / "art" / "player" / "reference" / "thread-spool-bearer-full.png"
            lantern = project / "assets" / "player" / "thread-spool-bearer" / "lantern.png"
            for path, payload in (
                (body, b"body-source"),
                (full, b"full-source"),
                (lantern, b"lantern-source"),
            ):
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(payload)

            hashes = {
                "body": sha256(body),
                "full": sha256(full),
                "lantern": sha256(lantern),
            }
            first = bootstrap_workspace(
                project,
                external,
                expected_hashes=hashes,
            )
            request_path = external / "request" / "master-request.json"
            original_text = request_path.read_text(encoding="utf-8")
            request = json.loads(original_text)
            request["note"] = "manual review note"
            edited_text = json.dumps(request, indent=2) + "\n"
            request_path.write_text(edited_text, encoding="utf-8")

            second = bootstrap_workspace(
                project,
                external,
                expected_hashes=hashes,
            )

            self.assertEqual(body.read_bytes(), (external / "request" / "references" / "body.png").read_bytes())
            self.assertEqual(full.read_bytes(), (external / "request" / "references" / "full.png").read_bytes())
            self.assertEqual(lantern.read_bytes(), (external / "request" / "references" / "lantern.png").read_bytes())
            self.assertEqual(edited_text, request_path.read_text(encoding="utf-8"))
            self.assertEqual(35, first["parts"])
            self.assertEqual(first["sourceSha256"], second["sourceSha256"])
            for directory in (
                "authoring",
                "layers",
                "review",
                "evidence",
                "rejected-b-route",
            ):
                self.assertTrue((external / directory).is_dir(), directory)


if __name__ == "__main__":
    unittest.main()
