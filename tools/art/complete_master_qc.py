from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any, Mapping

from PIL import Image

from complete_master_contract import OVERLAY_PART_IDS, PART_IDS, SUBJECT_PART_IDS, sha256_file


CANVAS = (1402, 1122)
REVIEW_STATUSES = frozenset(("not-started", "ready-for-review", "rejected", "accepted"))


def _load_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file():
        raise ValueError(f"missing {label}: {path}")
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, UnicodeDecodeError) as error:
        raise ValueError(f"invalid {label}: {error}") from error
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object")
    return value


def _within_root(root: Path, relative: str) -> Path:
    if not isinstance(relative, str) or not relative or Path(relative).is_absolute():
        raise ValueError("evidence path must be a non-empty relative path")
    resolved_root = root.resolve()
    resolved = (resolved_root / relative).resolve()
    if resolved != resolved_root and resolved_root not in resolved.parents:
        raise ValueError(f"evidence path escapes root: {relative}")
    return resolved


def _verify_review_state(review: Mapping[str, Any], stage: str, hashes: list[str]) -> str:
    entry = review.get(stage)
    if not isinstance(entry, Mapping):
        raise ValueError(f"review-state is missing stage {stage}")
    status = entry.get("status")
    if status not in REVIEW_STATUSES:
        raise ValueError(f"invalid review status for {stage}")
    if status == "accepted":
        approval = entry.get("explicitUserApproval")
        if not isinstance(approval, str) or not approval.strip():
            raise ValueError(f"review.{stage}.explicitUserApproval is required for accepted status")
        if not isinstance(entry.get("approvedAt"), str) or not entry["approvedAt"].strip():
            raise ValueError(f"review.{stage}.approvedAt is required for accepted status")
        evidence_hashes = entry.get("evidenceSha256")
        if not hashes:
            raise ValueError(f"review.{stage} cannot be accepted without review evidence")
        if not isinstance(evidence_hashes, list) or not evidence_hashes:
            raise ValueError(f"review.{stage}.evidenceSha256 is required for accepted status")
        normalized = [str(item).upper() for item in evidence_hashes]
        missing = sorted(set(hashes) - set(normalized))
        if missing:
            raise ValueError(f"review.{stage}.evidenceSha256 does not cover current evidence")
    return str(status)


def _promote_ready_for_review(path: Path, review: dict[str, Any], stage: str) -> str:
    entry = review[stage]
    if entry.get("status") != "not-started":
        return str(entry["status"])
    updated = dict(entry)
    updated["status"] = "ready-for-review"
    updated["explicitUserApproval"] = None
    review[stage] = updated
    path.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    return "ready-for-review"


def verify_ordered_review_state(
    review: dict[str, Any],
    ordered_gates: tuple[str, ...],
    gates: Mapping[str, list[str]],
    review_hashes: Mapping[str, str],
) -> tuple[str, str]:
    if not ordered_gates or len(set(ordered_gates)) != len(ordered_gates):
        raise ValueError("review gate order must contain unique gate names")
    last_accepted: str | None = None
    for index, gate in enumerate(ordered_gates):
        entry = review.get(gate)
        if not isinstance(entry, dict):
            raise ValueError(f"review-state is missing stage {gate}")
        required_paths = gates.get(gate)
        if not isinstance(required_paths, list) or not required_paths:
            raise ValueError(f"final review gate {gate} must reference review evidence")
        status = str(entry.get("status"))
        missing_paths = [path for path in required_paths if path not in review_hashes]
        if status == "accepted":
            if missing_paths:
                raise ValueError(f"final review gate {gate} references missing review evidence")
            required_hashes = [review_hashes[path] for path in required_paths]
            _verify_review_state(review, gate, required_hashes)
            last_accepted = gate
            continue
        if any(review[previous].get("status") != "accepted" for previous in ordered_gates[:index]):
            if status != "not-started":
                raise ValueError(f"final review gate {gate} opened before prerequisites")
            continue
        if missing_paths:
            if status != "not-started" or last_accepted is None:
                raise ValueError(f"final review gate {gate} references missing review evidence")
            return last_accepted, "accepted"
        if status == "not-started":
            entry["status"] = "ready-for-review"
            entry["explicitUserApproval"] = None
            status = "ready-for-review"
        if status not in ("ready-for-review", "rejected"):
            raise ValueError(f"invalid review status for {gate}")
        return gate, status
    return last_accepted or ordered_gates[-1], "accepted"


def verify_final_review_state(
    review: dict[str, Any],
    gates: Mapping[str, list[str]],
    review_hashes: Mapping[str, str],
) -> tuple[str, str]:
    return verify_ordered_review_state(
        review,
        ("dressedMaster", "smallScale", "animationReady"),
        gates,
        review_hashes,
    )


def verify_stage(root: Path, stage: str, verification_started_ns: int) -> dict[str, Any]:
    external_root = root.resolve()
    if verification_started_ns < 0:
        raise ValueError("verification_started_ns must be non-negative")
    manifest_path = external_root / "evidence" / f"{stage}-manifest.json"
    manifest = _load_json(manifest_path, f"{stage} manifest")
    review_path = _within_root(
        external_root,
        manifest.get("reviewStatePath", "evidence/review-state.json"),
    )
    review = _load_json(review_path, "review-state")
    if manifest.get("version") != 1 or manifest.get("stage") != stage:
        raise ValueError(f"manifest identity does not match stage {stage}")
    files = manifest.get("files")
    if not isinstance(files, list) or not files:
        raise ValueError("manifest files must be a non-empty list")

    records: dict[str, dict[str, Any]] = {}
    part_images: dict[str, Image.Image] = {}
    part_paths: dict[str, Path] = {}
    seen_paths: set[str] = set()
    seen_parts: set[str] = set()
    review_hashes: list[str] = []
    gate_map_for_freshness = manifest.get("reviewGates", {})
    accepted_review_paths = {
        relative
        for gate, gate_entry in review.items()
        if isinstance(gate_entry, Mapping)
        and gate_entry.get("status") == "accepted"
        for relative in (
            gate_map_for_freshness.get(gate, [])
            if isinstance(gate_map_for_freshness, Mapping)
            and isinstance(gate_map_for_freshness.get(gate), list)
            else []
        )
    }
    try:
        for item in files:
            if not isinstance(item, Mapping):
                raise ValueError("manifest file entries must be objects")
            relative = item.get("path")
            if relative in seen_paths:
                raise ValueError(f"duplicate evidence path: {relative}")
            seen_paths.add(relative)
            path = _within_root(external_root, relative)
            if path.suffix.lower() != ".png" or not path.is_file():
                raise ValueError(f"missing PNG: {relative}")
            role = item.get("role")
            assembled_stage = stage == "final" or manifest.get("reviewGateOrder") is not None
            if path.stat().st_mtime_ns < verification_started_ns and (
                not assembled_stage
                or (role == "review" and relative not in accepted_review_paths)
            ):
                raise ValueError(f"stale evidence: {relative}")
            actual_hash = sha256_file(path)
            expected_hash = item.get("sha256")
            if not isinstance(expected_hash, str) or actual_hash != expected_hash.upper():
                raise ValueError(f"sha256 mismatch: {relative}")

            image = Image.open(path)
            image.load()
            if image.size != CANVAS:
                image.close()
                raise ValueError(f"PNG must be 1402x1122: {relative}")
            if image.mode != "RGBA":
                image.close()
                raise ValueError(f"PNG must be RGBA: {relative}")
            alpha = image.getchannel("A")
            if alpha.getbbox() is None:
                image.close()
                raise ValueError(f"empty alpha: {relative}")
            width, height = image.size
            if (
                alpha.crop((0, 0, width, 1)).getbbox()
                or alpha.crop((0, height - 1, width, height)).getbbox()
                or alpha.crop((0, 0, 1, height)).getbbox()
                or alpha.crop((width - 1, 0, width, height)).getbbox()
            ):
                image.close()
                raise ValueError(f"alpha touches canvas edge: {relative}")

            if role not in ("part", "overlay", "review"):
                image.close()
                raise ValueError(f"invalid evidence role: {relative}")
            part_id = item.get("partId")
            if role in ("part", "overlay"):
                expected_set = SUBJECT_PART_IDS if role == "part" else OVERLAY_PART_IDS
                if part_id not in expected_set:
                    image.close()
                    raise ValueError(f"invalid {role} part ID: {part_id}")
                if part_id in seen_parts:
                    image.close()
                    raise ValueError(f"duplicate subject ID: {part_id}")
                seen_parts.add(part_id)
                part_images[str(part_id)] = image
                part_paths[str(part_id)] = path
            else:
                review_hashes.append(actual_hash)
                image.close()
            records[str(relative)] = {
                "sha256": actual_hash,
                "role": role,
                **({"partId": part_id} if part_id else {}),
            }

        connections = manifest.get("connections", {})
        if not isinstance(connections, Mapping):
            raise ValueError("manifest connections must be an object")
        for connection_id, connection in connections.items():
            if not isinstance(connection, Mapping):
                raise ValueError(f"connection {connection_id} must be an object")
            part_id = connection.get("partId")
            point = connection.get("point")
            if part_id not in part_images:
                raise ValueError(f"connection {connection_id} references missing subject {part_id}")
            if (
                not isinstance(point, list)
                or len(point) != 2
                or any(not isinstance(value, int) for value in point)
            ):
                raise ValueError(f"connection {connection_id} point must contain two integers")
            x, y = point
            if x < 0 or x >= CANVAS[0] or y < 0 or y >= CANVAS[1]:
                raise ValueError(f"connection {connection_id} is outside canvas")
            if part_images[str(part_id)].getpixel((x, y))[3] == 0:
                raise ValueError(f"connection {connection_id} is outside {part_id} alpha")

        gate_order_raw = manifest.get("reviewGateOrder")
        if stage == "final" or gate_order_raw is not None:
            gate_map = manifest.get("reviewGates")
            if not isinstance(gate_map, Mapping):
                raise ValueError("final manifest reviewGates must be an object")
            if gate_order_raw is None:
                gate_order = ("dressedMaster", "smallScale", "animationReady")
            elif (
                not isinstance(gate_order_raw, list)
                or not gate_order_raw
                or any(not isinstance(gate, str) or not gate for gate in gate_order_raw)
            ):
                raise ValueError("manifest reviewGateOrder must contain gate names")
            else:
                gate_order = tuple(gate_order_raw)
            review_hash_map = {
                relative: record["sha256"]
                for relative, record in records.items()
                if record["role"] == "review"
            }
            active_gate, status = verify_ordered_review_state(
                review,
                gate_order,
                gate_map,
                review_hash_map,
            )
            review_path.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
        else:
            active_gate = stage
            status = _verify_review_state(review, stage, review_hashes)
            if status == "not-started":
                status = _promote_ready_for_review(review_path, review, stage)
        return {
            "version": 1,
            "stage": stage,
            "status": status,
            "activeGate": active_gate,
            "files": len(files),
            "evidence": records,
            "connections": dict(connections),
        }
    finally:
        for image in part_images.values():
            image.close()


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify complete-master stage evidence")
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--stage", required=True)
    parser.add_argument("--verification-started-ns", type=int, required=True)
    args = parser.parse_args()
    report = verify_stage(args.root, args.stage, args.verification_started_ns)
    report_path = args.root.resolve() / "evidence" / "qc-report.json"
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"COMPLETE_MASTER_QC_OK stage={args.stage} files={report['files']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
