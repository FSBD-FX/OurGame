from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Mapping


BODY_SHA256 = "0358BFEA48D6FF79271A5B14AE03B0396361748E60EB2D9D36EF6A696D7B1B44"
FULL_SHA256 = "726E13EA5AD2BF4BAF6B161CE51CE29BA2CD8ABFC259616471961CBC91F25909"
LANTERN_SHA256 = "C313BFAAF1A1B8C9994AA6FEBC96D5A9E5C82F740B58305FF3C96696DCC73AB0"

PART_IDS = (
    "inner-body",
    "head-shell",
    "torso-shell",
    "pelvis-shell",
    "left-upper-arm",
    "left-forearm",
    "left-hand",
    "right-upper-arm",
    "right-forearm",
    "right-hand",
    "left-thigh",
    "left-shin",
    "left-boot",
    "right-thigh",
    "right-shin",
    "right-boot",
    "front-spool-housing",
    "front-spool-disc",
    "front-spool-glow",
    "back-core-housing",
    "back-energy-cell",
    "back-core-glow",
    "hidden-axle",
    "shoulder-collar",
    "collar-fastener",
    "front-collar-flap",
    "mantle",
    "half-bell-cleaver",
    "left-grip-overlay",
    "right-grip-overlay",
    "lantern-housing",
    "lantern-glow",
    "tether-back-connector",
    "tether-energy-middle",
    "tether-lantern-connector",
)

OVERLAY_PART_IDS = (
    "left-grip-overlay",
    "right-grip-overlay",
)
SUBJECT_PART_IDS = tuple(part for part in PART_IDS if part not in OVERLAY_PART_IDS)

REVIEW_STAGES = (
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
REVIEW_STATUSES = frozenset(("not-started", "ready-for-review", "rejected", "accepted"))


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


@dataclass(frozen=True)
class SourceReference:
    path: str
    sha256: str


@dataclass(frozen=True)
class MasterRequest:
    version: int
    id: str
    canvas: tuple[int, int]
    baseline_y: int
    facing: str
    parts: tuple[str, ...]
    sources: dict[str, SourceReference]
    review: dict[str, dict[str, Any]]

    @classmethod
    def load(cls, path: Path) -> "MasterRequest":
        return cls.from_dict(json.loads(path.read_text(encoding="utf-8")))

    @classmethod
    def from_dict(
        cls,
        raw: Mapping[str, Any],
        *,
        expected_hashes: Mapping[str, str] | None = None,
    ) -> "MasterRequest":
        if raw.get("version") != 1:
            raise ValueError("version must be 1")
        if raw.get("id") != "complete-layered-001":
            raise ValueError("id must be complete-layered-001")
        if raw.get("canvas") != [1402, 1122]:
            raise ValueError("canvas must be [1402, 1122]")
        if raw.get("baselineY") != 1080:
            raise ValueError("baselineY must be 1080")
        if raw.get("facing") != "right":
            raise ValueError("facing must be right")
        if raw.get("parts") != list(PART_IDS):
            raise ValueError("parts must match the canonical ordered part IDs")

        expected = dict(
            expected_hashes
            or {
                "body": BODY_SHA256,
                "full": FULL_SHA256,
                "lantern": LANTERN_SHA256,
            }
        )
        source_raw = raw.get("sources")
        if not isinstance(source_raw, Mapping):
            raise ValueError("sources must be an object")
        sources: dict[str, SourceReference] = {}
        for name in ("body", "full", "lantern"):
            item = source_raw.get(name)
            if not isinstance(item, Mapping):
                raise ValueError(f"sources.{name} must be an object")
            digest = item.get("sha256")
            if digest != expected[name]:
                raise ValueError(f"sources.{name}.sha256 must be {expected[name]}")
            source_path = item.get("path")
            if not isinstance(source_path, str) or not source_path:
                raise ValueError(f"sources.{name}.path must be a non-empty string")
            sources[name] = SourceReference(source_path, digest)

        review_raw = raw.get("review")
        if not isinstance(review_raw, Mapping):
            raise ValueError("review must be an object")
        if tuple(review_raw.keys()) != REVIEW_STAGES:
            raise ValueError("review must contain the canonical ordered stages")
        review: dict[str, dict[str, Any]] = {}
        for stage in REVIEW_STAGES:
            item = review_raw[stage]
            if not isinstance(item, Mapping):
                raise ValueError(f"review.{stage} must be an object")
            status = item.get("status")
            if status not in REVIEW_STATUSES:
                raise ValueError(f"review.{stage}.status is invalid")
            approval = item.get("explicitUserApproval")
            if status == "accepted" and not isinstance(approval, str):
                raise ValueError(f"review.{stage}.explicitUserApproval is required for accepted status")
            if status == "accepted" and not approval.strip():
                raise ValueError(f"review.{stage}.explicitUserApproval cannot be empty")
            review[stage] = dict(item)

        return cls(
            version=1,
            id="complete-layered-001",
            canvas=(1402, 1122),
            baseline_y=1080,
            facing="right",
            parts=PART_IDS,
            sources=sources,
            review=review,
        )

    def validate_sources(self, project_root: Path) -> None:
        for name, source in self.sources.items():
            path = (project_root / source.path).resolve()
            if not path.is_file():
                raise ValueError(f"sources.{name}.path does not exist: {path}")
            actual = sha256_file(path)
            if actual != source.sha256:
                raise ValueError(
                    f"sources.{name}.sha256 mismatch: expected {source.sha256}, got {actual}"
                )
