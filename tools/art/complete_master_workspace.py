from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path
from typing import Any, Mapping

from complete_master_contract import (
    BODY_SHA256,
    FULL_SHA256,
    LANTERN_SHA256,
    PART_IDS,
    REVIEW_STAGES,
    MasterRequest,
    sha256_file,
)


SOURCE_PATHS = {
    "body": Path("docs/art/player/reference/thread-spool-bearer-body.png"),
    "full": Path("docs/art/player/reference/thread-spool-bearer-full.png"),
    "lantern": Path("assets/player/thread-spool-bearer/lantern.png"),
}


def _default_request(hashes: Mapping[str, str]) -> dict[str, Any]:
    return {
        "version": 1,
        "id": "complete-layered-001",
        "canvas": [1402, 1122],
        "baselineY": 1080,
        "facing": "right",
        "parts": list(PART_IDS),
        "sources": {
            name: {
                "path": SOURCE_PATHS[name].as_posix(),
                "sha256": hashes[name],
            }
            for name in ("body", "full", "lantern")
        },
        "review": {
            stage: {"status": "not-started", "explicitUserApproval": None}
            for stage in REVIEW_STAGES
        },
    }


def bootstrap_workspace(
    project_root: Path,
    external_root: Path,
    *,
    expected_hashes: Mapping[str, str] | None = None,
) -> dict[str, Any]:
    project_root = project_root.resolve()
    external_root = external_root.resolve()
    expected = dict(
        expected_hashes
        or {
            "body": BODY_SHA256,
            "full": FULL_SHA256,
            "lantern": LANTERN_SHA256,
        }
    )

    sources: dict[str, Path] = {}
    actual_hashes: dict[str, str] = {}
    for name, relative in SOURCE_PATHS.items():
        source = (project_root / relative).resolve()
        if not source.is_file():
            raise ValueError(f"source does not exist: {source}")
        actual = sha256_file(source)
        if actual != expected[name]:
            raise ValueError(
                f"{name} sha256 mismatch: expected {expected[name]}, got {actual}"
            )
        sources[name] = source
        actual_hashes[name] = actual

    for directory in (
        "request/references",
        "authoring",
        "layers",
        "review",
        "evidence",
        "rejected-b-route",
    ):
        (external_root / directory).mkdir(parents=True, exist_ok=True)

    references = external_root / "request" / "references"
    for name, source in sources.items():
        shutil.copyfile(source, references / f"{name}{source.suffix.lower()}")

    request_path = external_root / "request" / "master-request.json"
    if not request_path.exists():
        request_path.write_text(
            json.dumps(_default_request(actual_hashes), indent=2) + "\n",
            encoding="utf-8",
        )
    request = MasterRequest.from_dict(
        json.loads(request_path.read_text(encoding="utf-8")),
        expected_hashes=expected,
    )

    manifest = {
        "version": 1,
        "sourceSha256": actual_hashes,
        "copiedSha256": {
            name: sha256_file(references / f"{name}{source.suffix.lower()}")
            for name, source in sources.items()
        },
    }
    (external_root / "request" / "source-manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )
    if manifest["sourceSha256"] != manifest["copiedSha256"]:
        raise ValueError("copied references do not match their sources")

    return {
        "parts": len(request.parts),
        "sourceSha256": actual_hashes,
        "externalRoot": str(external_root),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Bootstrap the complete player master workspace.")
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--external-root", type=Path, required=True)
    arguments = parser.parse_args()
    result = bootstrap_workspace(arguments.project_root, arguments.external_root)
    print(f"COMPLETE_MASTER_WORKSPACE_OK parts={result['parts']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
