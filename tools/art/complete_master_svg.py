from __future__ import annotations

import argparse
import copy
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any

from PIL import Image


CANVAS = (1402, 1122)
ALLOWED_ELEMENTS = frozenset(
    {
        "svg",
        "g",
        "defs",
        "path",
        "rect",
        "circle",
        "ellipse",
        "polygon",
        "polyline",
        "line",
        "linearGradient",
        "radialGradient",
        "stop",
        "filter",
        "feGaussianBlur",
        "feColorMatrix",
        "clipPath",
        "mask",
    }
)
REMOTE_REFERENCE = re.compile(r"(?:https?|file|ftp|data|javascript):", re.IGNORECASE)
URL_REFERENCE = re.compile(r"url\(\s*([^)]*)\s*\)", re.IGNORECASE)


def _local_name(name: str) -> str:
    return name.rsplit("}", 1)[-1]


def _number(value: str | None, field: str) -> float:
    if value is None:
        raise ValueError(f"SVG {field} is required")
    normalized = value.strip()
    if normalized.endswith("px"):
        normalized = normalized[:-2]
    try:
        return float(normalized)
    except ValueError as error:
        raise ValueError(f"SVG {field} must be numeric") from error


def validate_authoring_svg(path: Path, expected_ids: tuple[str, ...]) -> dict[str, Any]:
    if not path.is_file():
        raise ValueError(f"SVG does not exist: {path}")
    source = path.read_text(encoding="utf-8")
    lowered = source.lower()
    if "<!doctype" in lowered or "<!entity" in lowered:
        raise ValueError("forbidden XML declaration in authoring SVG")
    try:
        root = ET.fromstring(source)
    except ET.ParseError as error:
        raise ValueError(f"invalid SVG XML: {error}") from error

    if _local_name(root.tag) != "svg":
        raise ValueError("authoring document root must be svg")
    width = _number(root.get("width"), "width")
    height = _number(root.get("height"), "height")
    try:
        view_box = tuple(float(item) for item in (root.get("viewBox") or "").split())
    except ValueError as error:
        raise ValueError("SVG canvas must be exactly 1402x1122") from error
    if (width, height) != CANVAS or view_box != (0.0, 0.0, 1402.0, 1122.0):
        raise ValueError("SVG canvas must be exactly 1402x1122 with viewBox 0 0 1402 1122")

    all_ids: set[str] = set()
    part_occurrences: dict[str, int] = {}
    top_level_elements = {id(element) for element in list(root)}
    for element in root.iter():
        name = _local_name(element.tag)
        element_id = element.get("id")
        if element_id:
            if element_id in all_ids:
                raise ValueError(f"duplicate SVG id: {element_id}")
            all_ids.add(element_id)
        for attribute, value in element.attrib.items():
            attribute_name = _local_name(attribute).lower()
            if attribute_name.startswith("on"):
                raise ValueError(f"event handler attribute is forbidden: {attribute_name}")
            if attribute_name == "href":
                raise ValueError("href attributes are forbidden in authoring SVG")
            if REMOTE_REFERENCE.search(value):
                raise ValueError(f"remote URL is forbidden in SVG attribute {attribute_name}")
            for match in URL_REFERENCE.finditer(value):
                target = match.group(1).strip().strip("\"'")
                if not target.startswith("#"):
                    raise ValueError(f"remote URL is forbidden in SVG attribute {attribute_name}")
        if name not in ALLOWED_ELEMENTS:
            raise ValueError(f"forbidden element: {name}")
        if name == "g" and element_id and element_id.startswith("part-"):
            part_id = element_id.removeprefix("part-")
            if id(element) not in top_level_elements:
                raise ValueError(f"part group must be top-level: {part_id}")
            part_occurrences[part_id] = part_occurrences.get(part_id, 0) + 1

    duplicate_parts = sorted(part for part, count in part_occurrences.items() if count > 1)
    if duplicate_parts:
        raise ValueError(f"duplicate part ID: {duplicate_parts[0]}")
    expected = set(expected_ids)
    actual = set(part_occurrences)
    missing = sorted(expected - actual)
    extra = sorted(actual - expected)
    if missing:
        raise ValueError(f"missing part ID: {missing[0]}")
    if extra:
        raise ValueError(f"unexpected part ID: {extra[0]}")

    return {
        "canvas": [CANVAS[0], CANVAS[1]],
        "partIds": list(expected_ids),
        "elementCount": sum(1 for _ in root.iter()),
    }


def discover_edge() -> Path | None:
    candidates = [
        Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
        Path(r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
    ]
    executable = shutil.which("msedge")
    if executable:
        candidates.insert(0, Path(executable))
    return next((candidate.resolve() for candidate in candidates if candidate.is_file()), None)


def render_svg(edge_path: Path, source_svg: Path, output_png: Path) -> None:
    edge = edge_path.resolve()
    source = source_svg.resolve()
    output = output_png.resolve()
    if not edge.is_file():
        raise ValueError(f"Microsoft Edge executable does not exist: {edge}")
    if not source.is_file():
        raise ValueError(f"source SVG does not exist: {source}")
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        output.unlink()

    with tempfile.TemporaryDirectory(prefix="complete-master-edge-") as profile:
        command = [
            str(edge),
            "--headless",
            "--disable-gpu",
            "--hide-scrollbars",
            "--default-background-color=00000000",
            f"--window-size={CANVAS[0]},{CANVAS[1]}",
            f"--user-data-dir={profile}",
            f"--screenshot={output}",
            source.as_uri(),
        ]
        result = subprocess.run(command, capture_output=True, text=True, timeout=45)
    if result.returncode != 0:
        detail = (result.stderr or result.stdout).strip()
        raise RuntimeError(f"Edge SVG render failed ({result.returncode}): {detail}")
    if not output.is_file() or output.stat().st_size == 0:
        raise RuntimeError("Edge SVG render did not create a non-empty PNG")
    with Image.open(output) as rendered:
        if rendered.size != CANVAS:
            raise RuntimeError(f"Edge rendered {rendered.size}, expected {CANVAS}")
        rgba = rendered.convert("RGBA")
        rgba.load()
    rgba.save(output)


def render_part_layers(
    edge_path: Path,
    source_svg: Path,
    output_directory: Path,
    part_ids: tuple[str, ...],
) -> dict[str, Path]:
    validate_authoring_svg(source_svg, part_ids)
    try:
        document = ET.parse(source_svg)
    except ET.ParseError as error:
        raise ValueError(f"invalid SVG XML: {error}") from error
    root = document.getroot()
    output_directory.mkdir(parents=True, exist_ok=True)
    outputs: dict[str, Path] = {}
    with tempfile.TemporaryDirectory(prefix="complete-master-parts-") as temporary:
        temporary_root = Path(temporary)
        for part_id in part_ids:
            isolated_root = copy.deepcopy(root)
            expected_group = f"part-{part_id}"
            found = False
            for child in list(isolated_root):
                child_id = child.get("id")
                if child_id and child_id.startswith("part-"):
                    if child_id == expected_group:
                        found = True
                    else:
                        isolated_root.remove(child)
            if not found:
                raise ValueError(f"missing part ID during render: {part_id}")
            isolated_source = temporary_root / f"{part_id}.svg"
            ET.ElementTree(isolated_root).write(
                isolated_source,
                encoding="utf-8",
                xml_declaration=True,
            )
            output = output_directory / f"{part_id}.png"
            render_svg(edge_path, isolated_source, output)
            outputs[part_id] = output
    return outputs


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate or render complete-master SVG files")
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--part-id", action="append", default=[])
    parser.add_argument("--edge", type=Path)
    args = parser.parse_args()
    report = validate_authoring_svg(args.source, tuple(args.part_id))
    if args.output:
        edge = args.edge or discover_edge()
        if edge is None:
            raise SystemExit("Microsoft Edge was not found")
        render_svg(edge, args.source, args.output)
    print(f"COMPLETE_MASTER_SVG_OK parts={len(report['partIds'])}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
