from __future__ import annotations

import hashlib
import json
import os
import sys
import tempfile
import time
import unittest
from pathlib import Path

from PIL import Image, ImageDraw


TOOLS = Path(__file__).resolve().parents[2] / "tools" / "art"
sys.path.insert(0, str(TOOLS))

from complete_master_qc import verify_stage


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


class QcFixture:
    def __init__(self, root: Path):
        self.root = root
        (root / "layers").mkdir(parents=True)
        (root / "evidence").mkdir(parents=True)
        self.image_path = root / "layers" / "inner-body.png"
        self.write_image()
        self.manifest = {
            "version": 1,
            "stage": "body",
            "files": [
                {
                    "path": "layers/inner-body.png",
                    "sha256": digest(self.image_path),
                    "role": "part",
                    "partId": "inner-body",
                }
            ],
            "connections": {
                "neck": {"partId": "inner-body", "point": [120, 120]},
            },
        }
        self.review = {
            "version": 1,
            "body": {
                "status": "ready-for-review",
                "explicitUserApproval": None,
            },
        }
        self.started_ns = self.image_path.stat().st_mtime_ns - 1
        self.save()

    def write_image(
        self,
        *,
        path: Path | None = None,
        size: tuple[int, int] = (1402, 1122),
        mode: str = "RGBA",
        empty: bool = False,
        edge: bool = False,
    ) -> Path:
        target = path or self.image_path
        image = Image.new(mode, size, (0, 0, 0, 0) if mode == "RGBA" else (0, 0, 0))
        if not empty:
            draw = ImageDraw.Draw(image)
            box = (0, 100, 30, 130) if edge else (100, 100, 200, 200)
            fill = (255, 255, 255, 255) if mode == "RGBA" else (255, 255, 255)
            draw.rectangle(box, fill=fill)
        image.save(target)
        return target

    def save(self) -> None:
        (self.root / "evidence" / "body-manifest.json").write_text(
            json.dumps(self.manifest, indent=2), encoding="utf-8"
        )
        (self.root / "evidence" / "review-state.json").write_text(
            json.dumps(self.review, indent=2), encoding="utf-8"
        )

    def refresh_hash(self, index: int = 0, path: Path | None = None) -> None:
        self.manifest["files"][index]["sha256"] = digest(path or self.image_path)
        self.save()


class CompleteMasterQcTests(unittest.TestCase):
    def with_fixture(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        return QcFixture(Path(temp.name))

    def test_accepts_fresh_hashed_rgba_subject_and_connection(self):
        fixture = self.with_fixture()
        report = verify_stage(fixture.root, "body", fixture.started_ns)
        self.assertEqual("body", report["stage"])
        self.assertEqual(1, report["files"])

    def test_rejects_missing_png(self):
        fixture = self.with_fixture()
        fixture.image_path.unlink()
        with self.assertRaisesRegex(ValueError, "missing PNG"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_rejects_wrong_size(self):
        fixture = self.with_fixture()
        fixture.write_image(size=(1401, 1122))
        fixture.refresh_hash()
        with self.assertRaisesRegex(ValueError, "1402x1122"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_rejects_non_rgba(self):
        fixture = self.with_fixture()
        fixture.write_image(mode="RGB")
        fixture.refresh_hash()
        with self.assertRaisesRegex(ValueError, "RGBA"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_rejects_empty_alpha(self):
        fixture = self.with_fixture()
        fixture.write_image(empty=True)
        fixture.refresh_hash()
        with self.assertRaisesRegex(ValueError, "empty alpha"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_rejects_alpha_touching_canvas_edge(self):
        fixture = self.with_fixture()
        fixture.write_image(edge=True)
        fixture.refresh_hash()
        with self.assertRaisesRegex(ValueError, "canvas edge"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_rejects_hash_mismatch(self):
        fixture = self.with_fixture()
        fixture.manifest["files"][0]["sha256"] = "0" * 64
        fixture.save()
        with self.assertRaisesRegex(ValueError, "sha256 mismatch"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_rejects_duplicate_subject_id(self):
        fixture = self.with_fixture()
        second = fixture.write_image(path=fixture.root / "layers" / "copy.png")
        fixture.manifest["files"].append(
            {
                "path": "layers/copy.png",
                "sha256": digest(second),
                "role": "part",
                "partId": "inner-body",
            }
        )
        fixture.save()
        with self.assertRaisesRegex(ValueError, "duplicate subject ID.*inner-body"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_rejects_subject_connection_outside_alpha(self):
        fixture = self.with_fixture()
        fixture.manifest["connections"]["left-elbow"] = {
            "partId": "inner-body",
            "point": [4, 4],
        }
        fixture.save()
        with self.assertRaisesRegex(ValueError, "left-elbow.*outside"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_rejects_stale_evidence(self):
        fixture = self.with_fixture()
        stale_start = fixture.image_path.stat().st_mtime_ns + 1
        with self.assertRaisesRegex(ValueError, "stale evidence"):
            verify_stage(fixture.root, "body", stale_start)

    def test_final_reuses_hashed_parts_but_requires_fresh_review_images(self):
        fixture = self.with_fixture()
        review_path = fixture.root / "review" / "master-dressed.png"
        review_path.parent.mkdir()
        fixture.write_image(path=review_path)
        review_hash = digest(review_path)
        fixture.manifest = {
            "version": 1,
            "stage": "final",
            "files": [
                {
                    "path": "layers/inner-body.png",
                    "sha256": digest(fixture.image_path),
                    "role": "part",
                    "partId": "inner-body",
                },
                {
                    "path": "review/master-dressed.png",
                    "sha256": review_hash,
                    "role": "review",
                },
            ],
            "connections": {},
            "reviewGates": {
                "dressedMaster": ["review/master-dressed.png"],
                "smallScale": ["review/master-dressed.png"],
                "animationReady": ["review/master-dressed.png"],
            },
        }
        fixture.review = {
            "version": 1,
            "dressedMaster": {"status": "not-started", "explicitUserApproval": None},
            "smallScale": {"status": "not-started", "explicitUserApproval": None},
            "animationReady": {"status": "not-started", "explicitUserApproval": None},
        }
        (fixture.root / "evidence" / "final-manifest.json").write_text(
            json.dumps(fixture.manifest, indent=2), encoding="utf-8"
        )
        (fixture.root / "evidence" / "review-state.json").write_text(
            json.dumps(fixture.review, indent=2), encoding="utf-8"
        )
        started_ns = review_path.stat().st_mtime_ns - 1
        old_ns = started_ns - 1_000_000_000
        os.utime(fixture.image_path, ns=(old_ns, old_ns))

        report = verify_stage(fixture.root, "final", started_ns)

        self.assertEqual("dressedMaster", report["activeGate"])
        self.assertEqual("ready-for-review", report["status"])

    def test_a2_manifest_uses_revision_state_and_opens_only_first_gate(self):
        fixture = self.with_fixture()
        legacy_review_path = fixture.root / "evidence" / "review-state.json"
        legacy_review_bytes = legacy_review_path.read_bytes()
        review_paths = []
        for name in ("a2-structure", "a2-dressed", "a2-readiness"):
            path = fixture.root / "review" / f"{name}.png"
            path.parent.mkdir(exist_ok=True)
            fixture.write_image(path=path)
            review_paths.append(path)
        fixture.manifest = {
            "version": 1,
            "stage": "a2",
            "files": [
                {
                    "path": "layers/inner-body.png",
                    "sha256": digest(fixture.image_path),
                    "role": "part",
                    "partId": "inner-body",
                },
                *[
                    {
                        "path": path.relative_to(fixture.root).as_posix(),
                        "sha256": digest(path),
                        "role": "review",
                    }
                    for path in review_paths
                ],
            ],
            "connections": {},
            "reviewStatePath": "evidence/a2-review-state.json",
            "reviewGateOrder": ["a2Structure", "a2DressedMaster", "a2Readiness"],
            "reviewGates": {
                "a2Structure": ["review/a2-structure.png"],
                "a2DressedMaster": ["review/a2-dressed.png"],
                "a2Readiness": ["review/a2-readiness.png"],
            },
        }
        (fixture.root / "evidence" / "a2-manifest.json").write_text(
            json.dumps(fixture.manifest, indent=2), encoding="utf-8"
        )
        (fixture.root / "evidence" / "a2-review-state.json").write_text(
            json.dumps(
                {
                    gate: {"status": "not-started", "explicitUserApproval": None}
                    for gate in ("a2Structure", "a2DressedMaster", "a2Readiness")
                },
                indent=2,
            ),
            encoding="utf-8",
        )

        report = verify_stage(fixture.root, "a2", fixture.started_ns)

        self.assertEqual("a2Structure", report["activeGate"])
        self.assertEqual("ready-for-review", report["status"])
        self.assertEqual(legacy_review_bytes, legacy_review_path.read_bytes())

    def test_a2_first_gate_does_not_require_unopened_future_evidence(self):
        fixture = self.with_fixture()
        review_path = fixture.root / "review" / "a2-structure.png"
        review_path.parent.mkdir(exist_ok=True)
        fixture.write_image(path=review_path)
        fixture.manifest = {
            "version": 1,
            "stage": "a2",
            "files": [
                {
                    "path": "layers/inner-body.png",
                    "sha256": digest(fixture.image_path),
                    "role": "part",
                    "partId": "inner-body",
                },
                {
                    "path": "review/a2-structure.png",
                    "sha256": digest(review_path),
                    "role": "review",
                },
            ],
            "connections": {},
            "reviewStatePath": "evidence/a2-review-state.json",
            "reviewGateOrder": ["a2Structure", "a2DressedMaster", "a2Readiness"],
            "reviewGates": {
                "a2Structure": ["review/a2-structure.png"],
                "a2DressedMaster": ["review/a2-dressed.png"],
                "a2Readiness": ["review/a2-readiness.png"],
            },
        }
        (fixture.root / "evidence" / "a2-manifest.json").write_text(
            json.dumps(fixture.manifest, indent=2), encoding="utf-8"
        )
        (fixture.root / "evidence" / "a2-review-state.json").write_text(
            json.dumps(
                {
                    gate: {"status": "not-started", "explicitUserApproval": None}
                    for gate in ("a2Structure", "a2DressedMaster", "a2Readiness")
                },
                indent=2,
            ),
            encoding="utf-8",
        )

        report = verify_stage(fixture.root, "a2", fixture.started_ns)

        self.assertEqual("a2Structure", report["activeGate"])
        self.assertEqual("ready-for-review", report["status"])

    def test_a2_accepted_structure_stays_valid_until_dressed_evidence_exists(self):
        fixture = self.with_fixture()
        review_path = fixture.root / "review" / "a2-structure.png"
        review_path.parent.mkdir(exist_ok=True)
        fixture.write_image(path=review_path)
        review_hash = digest(review_path)
        fixture.manifest = {
            "version": 1,
            "stage": "a2",
            "files": [
                {
                    "path": "layers/inner-body.png",
                    "sha256": digest(fixture.image_path),
                    "role": "part",
                    "partId": "inner-body",
                },
                {
                    "path": "review/a2-structure.png",
                    "sha256": review_hash,
                    "role": "review",
                },
            ],
            "connections": {},
            "reviewStatePath": "evidence/a2-review-state.json",
            "reviewGateOrder": ["a2Structure", "a2DressedMaster", "a2Readiness"],
            "reviewGates": {
                "a2Structure": ["review/a2-structure.png"],
                "a2DressedMaster": ["review/a2-dressed.png"],
                "a2Readiness": ["review/a2-readiness.png"],
            },
        }
        (fixture.root / "evidence" / "a2-manifest.json").write_text(
            json.dumps(fixture.manifest, indent=2), encoding="utf-8"
        )
        (fixture.root / "evidence" / "a2-review-state.json").write_text(
            json.dumps(
                {
                    "a2Structure": {
                        "status": "accepted",
                        "explicitUserApproval": "approved A2 structure",
                        "approvedAt": "2026-08-13T23:55:53+08:00",
                        "evidenceSha256": [review_hash],
                    },
                    "a2DressedMaster": {
                        "status": "not-started",
                        "explicitUserApproval": None,
                    },
                    "a2Readiness": {
                        "status": "not-started",
                        "explicitUserApproval": None,
                    },
                },
                indent=2,
            ),
            encoding="utf-8",
        )

        report = verify_stage(fixture.root, "a2", fixture.started_ns)

        self.assertEqual("a2Structure", report["activeGate"])
        self.assertEqual("accepted", report["status"])

    def test_a2_reuses_accepted_structure_evidence_and_opens_fresh_dressed_gate(self):
        fixture = self.with_fixture()
        structure_path = fixture.root / "review" / "a2-structure.png"
        structure_path.parent.mkdir(exist_ok=True)
        fixture.write_image(path=structure_path)
        structure_hash = digest(structure_path)
        started_ns = time.time_ns()
        old_ns = started_ns - 1_000_000_000
        os.utime(structure_path, ns=(old_ns, old_ns))
        dressed_path = fixture.write_image(
            path=fixture.root / "review" / "a2-dressed.png"
        )
        fixture.manifest = {
            "version": 1,
            "stage": "a2",
            "files": [
                {
                    "path": "layers/inner-body.png",
                    "sha256": digest(fixture.image_path),
                    "role": "part",
                    "partId": "inner-body",
                },
                {
                    "path": "review/a2-structure.png",
                    "sha256": structure_hash,
                    "role": "review",
                },
                {
                    "path": "review/a2-dressed.png",
                    "sha256": digest(dressed_path),
                    "role": "review",
                },
            ],
            "connections": {},
            "reviewStatePath": "evidence/a2-review-state.json",
            "reviewGateOrder": ["a2Structure", "a2DressedMaster", "a2Readiness"],
            "reviewGates": {
                "a2Structure": ["review/a2-structure.png"],
                "a2DressedMaster": ["review/a2-dressed.png"],
                "a2Readiness": ["review/a2-readiness.png"],
            },
        }
        (fixture.root / "evidence" / "a2-manifest.json").write_text(
            json.dumps(fixture.manifest, indent=2), encoding="utf-8"
        )
        (fixture.root / "evidence" / "a2-review-state.json").write_text(
            json.dumps(
                {
                    "a2Structure": {
                        "status": "accepted",
                        "explicitUserApproval": "approved A2 structure",
                        "approvedAt": "2026-08-13T23:55:53+08:00",
                        "evidenceSha256": [structure_hash],
                    },
                    "a2DressedMaster": {
                        "status": "not-started",
                        "explicitUserApproval": None,
                    },
                    "a2Readiness": {
                        "status": "not-started",
                        "explicitUserApproval": None,
                    },
                },
                indent=2,
            ),
            encoding="utf-8",
        )

        report = verify_stage(fixture.root, "a2", started_ns)

        self.assertEqual("a2DressedMaster", report["activeGate"])
        self.assertEqual("ready-for-review", report["status"])

    def test_rejects_tool_authored_acceptance(self):
        fixture = self.with_fixture()
        fixture.review["body"] = {
            "status": "accepted",
            "explicitUserApproval": None,
        }
        fixture.save()
        with self.assertRaisesRegex(ValueError, "explicitUserApproval"):
            verify_stage(fixture.root, "body", fixture.started_ns)

    def test_acceptance_is_bound_to_review_evidence_not_every_part_hash(self):
        fixture = self.with_fixture()
        review_path = fixture.write_image(path=fixture.root / "review.png")
        with Image.open(review_path) as source:
            review_image = source.copy()
        review_image.putpixel((210, 210), (0, 255, 255, 255))
        review_image.save(review_path)
        review_hash = digest(review_path)
        fixture.manifest["files"].append(
            {
                "path": "review.png",
                "sha256": review_hash,
                "role": "review",
            }
        )
        fixture.review["body"] = {
            "status": "accepted",
            "explicitUserApproval": "批准身体结构审查图",
            "approvedAt": "2026-08-13T12:00:00+08:00",
            "evidenceSha256": [review_hash],
        }
        fixture.save()
        report = verify_stage(fixture.root, "body", fixture.started_ns)
        self.assertEqual("accepted", report["status"])

    def test_success_promotes_only_to_ready_for_review(self):
        fixture = self.with_fixture()
        fixture.review["body"] = {
            "status": "not-started",
            "explicitUserApproval": None,
        }
        fixture.save()
        report = verify_stage(fixture.root, "body", fixture.started_ns)
        stored = json.loads(
            (fixture.root / "evidence" / "review-state.json").read_text(encoding="utf-8")
        )
        self.assertEqual("ready-for-review", report["status"])
        self.assertEqual("ready-for-review", stored["body"]["status"])
        self.assertIsNone(stored["body"]["explicitUserApproval"])

    def test_rejects_accepted_state_without_review_images(self):
        fixture = self.with_fixture()
        fixture.review["body"] = {
            "status": "accepted",
            "explicitUserApproval": "批准",
            "approvedAt": "2026-08-13T12:00:00+08:00",
            "evidenceSha256": ["A" * 64],
        }
        fixture.save()
        with self.assertRaisesRegex(ValueError, "review evidence"):
            verify_stage(fixture.root, "body", fixture.started_ns)


if __name__ == "__main__":
    unittest.main()
