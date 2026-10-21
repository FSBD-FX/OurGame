from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from complete_master_body import BODY_STACK
from complete_master_contract import MasterRequest, sha256_file
from complete_master_svg import discover_edge, render_part_layers


SPOOL_CORE_PART_IDS = (
    "front-spool-housing",
    "front-spool-disc",
    "front-spool-glow",
    "back-core-housing",
    "back-energy-cell",
    "back-core-glow",
    "hidden-axle",
)
SPOOL_CORE_STACK = (
    "back-core-housing",
    "back-energy-cell",
    "back-core-glow",
    "hidden-axle",
    "body",
    "front-spool-housing",
    "front-spool-disc",
    "front-spool-glow",
)
SPOOL_CORE_REVIEW_NAMES = (
    "spool-core-parts",
    "spool-core-mounted",
    "spool-core-back-detail",
)

CANVAS = (1402, 1122)
OUTLINE = "#101820"
BRASS = "#B98A37"
BRASS_LIGHT = "#E4B95B"
BRASS_DARK = "#6D4B1D"
STEEL = "#75828B"
STEEL_LIGHT = "#B8C2C5"
ENERGY = "#51E6CD"
ENERGY_LIGHT = "#D7FFF7"

BOUNDS = {
    "front-spool-housing": (690, 450, 80, 80),
    "front-spool-disc": (702, 462, 56, 56),
    "front-spool-glow": (715, 475, 30, 30),
    "back-core-housing": (534, 456, 68, 68),
    "back-energy-cell": (544, 466, 48, 48),
    "back-core-glow": (557, 475, 22, 30),
    "hidden-axle": (574, 478, 150, 24),
}


def _group(
    part_id: str,
    body: str,
    *,
    form: str | None = None,
    energy: str | None = None,
) -> str:
    bbox = " ".join(str(value) for value in BOUNDS[part_id])
    attributes = ""
    if form:
        attributes += f' data-form="{form}"'
    if energy:
        attributes += f' data-energy="{energy}"'
    return f'<g id="part-{part_id}" data-part-id="{part_id}" data-bbox="{bbox}"{attributes}>{body}</g>'


def build_spool_core_svg(request: MasterRequest) -> str:
    if request.canvas != CANVAS or request.baseline_y != 1080 or request.facing != "right":
        raise ValueError("spool/core request must use the canonical canvas, baseline, and facing")
    groups = [
        _group(
            "front-spool-housing",
            f'<path fill-rule="evenodd" d="M710 452 L750 452 L768 470 L768 510 L750 528 L710 528 L692 510 L692 470 Z M712 468 L748 468 L752 472 L752 508 L748 512 L712 512 L708 508 L708 472 Z" fill="url(#steel)" stroke="{OUTLINE}" stroke-width="6" stroke-linejoin="round"/>'
            f'<path d="M705 463 L715 453 L745 453 L756 464 M705 517 L716 527 L744 527 L756 516" fill="none" stroke="{BRASS_LIGHT}" stroke-width="6" stroke-linecap="round"/>'
            f'<circle cx="698" cy="490" r="5" fill="{BRASS_DARK}"/><circle cx="762" cy="490" r="5" fill="{BRASS_DARK}"/>',
            form="shoulder-front-spool-a2",
        ),
        _group(
            "front-spool-disc",
            f'<circle cx="730" cy="490" r="27" fill="url(#steel)" stroke="{OUTLINE}" stroke-width="6"/>'
            f'<circle cx="730" cy="490" r="19" fill="{BRASS_DARK}" stroke="{BRASS_LIGHT}" stroke-width="4"/>'
            f'<path d="M730 471 L730 509 M711 490 L749 490 M717 477 L743 503 M743 477 L717 503" stroke="{STEEL_LIGHT}" stroke-width="4" opacity="0.78"/>'
            f'<circle cx="730" cy="490" r="8" fill="{OUTLINE}" stroke="{BRASS}" stroke-width="4"/>',
            form="rotating-disc-v1",
        ),
        _group(
            "front-spool-glow",
            f'<circle cx="730" cy="490" r="14" fill="{ENERGY}" opacity="0.2" filter="url(#glow)"/>'
            f'<path d="M730 476 C738 482 742 489 740 496 C738 502 734 505 730 506 C724 502 720 496 720 490 C720 483 724 479 730 476 Z" fill="url(#energy)" stroke="{ENERGY_LIGHT}" stroke-width="2"/>',
            energy="glow-only",
        ),
        _group(
            "back-core-housing",
            f'<path d="M551 458 L585 458 L600 473 L600 507 L585 522 L551 522 L536 507 L536 473 Z" fill="url(#steel)" stroke="{OUTLINE}" stroke-width="6" stroke-linejoin="round"/>'
            f'<path d="M546 474 L554 466 L582 466 L591 475 M546 506 L555 514 L581 514 L591 505" fill="none" stroke="{BRASS}" stroke-width="5" stroke-linecap="round"/>'
            f'<circle cx="541" cy="490" r="5" fill="{BRASS_DARK}"/>',
            form="shoulder-back-core-a2",
        ),
        _group(
            "back-energy-cell",
            f'<rect x="546" y="468" width="44" height="44" rx="15" fill="{OUTLINE}" stroke="{BRASS}" stroke-width="5"/>'
            f'<path d="M553 477 L583 477 M553 503 L583 503" stroke="{STEEL_LIGHT}" stroke-width="4" opacity="0.75"/>'
            f'<rect x="553" y="480" width="30" height="20" rx="9" fill="#173B3A" stroke="{STEEL}" stroke-width="4"/>',
            form="removable-cell-v1",
        ),
        _group(
            "back-core-glow",
            f'<ellipse cx="568" cy="490" rx="10" ry="14" fill="{ENERGY}" opacity="0.22" filter="url(#glow)"/>'
            f'<path d="M568 477 C575 484 577 491 574 498 C572 502 570 504 568 505 C563 500 560 495 560 490 C560 484 563 480 568 477 Z" fill="url(#energy)" stroke="{ENERGY_LIGHT}" stroke-width="2"/>',
            energy="glow-only",
        ),
        _group(
            "hidden-axle",
            f'<rect x="578" y="480" width="142" height="20" rx="10" fill="url(#steel)" stroke="{OUTLINE}" stroke-width="6"/>'
            f'<path d="M596 482 L596 498 M620 482 L620 498 M644 482 L644 498 M668 482 L668 498 M692 482 L692 498" stroke="{BRASS}" stroke-width="4"/>'
            f'<circle cx="584" cy="490" r="9" fill="{BRASS_DARK}" stroke="{BRASS_LIGHT}" stroke-width="4"/><circle cx="714" cy="490" r="9" fill="{BRASS_DARK}" stroke="{BRASS_LIGHT}" stroke-width="4"/>',
            form="hidden-shoulder-axle-a2",
        ),
    ]
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" viewBox="0 0 1402 1122">'
        '<defs>'
        '<linearGradient id="brass" x1="0" y1="0" x2="1" y2="1">'
        f'<stop offset="0" stop-color="{BRASS_LIGHT}"/><stop offset="0.5" stop-color="{BRASS}"/><stop offset="1" stop-color="{BRASS_DARK}"/>'
        '</linearGradient>'
        '<linearGradient id="steel" x1="0" y1="0" x2="1" y2="1">'
        f'<stop offset="0" stop-color="{STEEL_LIGHT}"/><stop offset="0.5" stop-color="{STEEL}"/><stop offset="1" stop-color="#3D4951"/>'
        '</linearGradient>'
        '<linearGradient id="energy" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{ENERGY_LIGHT}"/><stop offset="0.45" stop-color="{ENERGY}"/><stop offset="1" stop-color="#168E88"/>'
        '</linearGradient>'
        '<filter id="glow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="9"/></filter>'
        '</defs>'
        + "".join(groups)
        + "</svg>"
    )


def _blank_review() -> Image.Image:
    image = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((22, 22, 1379, 1099), radius=28, fill=(11, 16, 24, 255))
    draw.rounded_rectangle((46, 46, 1355, 1075), radius=20, fill=(17, 26, 38, 255), outline=(52, 70, 90, 255), width=3)
    return image


def _read_rgba(path: Path) -> Image.Image:
    with Image.open(path) as source:
        image = source.convert("RGBA")
        image.load()
    return image


def _compose_body(layers: Path) -> Image.Image:
    result = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    for part_id in BODY_STACK:
        result.alpha_composite(_read_rgba(layers / f"{part_id}.png"))
    return result


def compose_mounted(layers: Path, *, include_hidden_axle: bool = True) -> Image.Image:
    result = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    for part_id in ("back-core-housing", "back-energy-cell", "back-core-glow"):
        result.alpha_composite(_read_rgba(layers / f"{part_id}.png"))
    if include_hidden_axle:
        result.alpha_composite(_read_rgba(layers / "hidden-axle.png"))
    result.alpha_composite(_compose_body(layers))
    for part_id in ("front-spool-housing", "front-spool-disc", "front-spool-glow"):
        result.alpha_composite(_read_rgba(layers / f"{part_id}.png"))
    return result


def _parts_review(paths: dict[str, Path]) -> Image.Image:
    review = _blank_review()
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=17)
    for index, part_id in enumerate(SPOOL_CORE_PART_IDS):
        column = index % 4
        row = index // 4
        cell = (75 + column * 326, 120 + row * 430, 365 + column * 326, 500 + row * 430)
        draw.rounded_rectangle(cell, radius=18, fill=(22, 34, 49, 255), outline=(52, 70, 90, 255), width=2)
        source = _read_rgba(paths[part_id])
        bbox = source.getchannel("A").getbbox()
        crop = source.crop(bbox)
        crop.thumbnail((215, 250), Image.Resampling.LANCZOS)
        review.alpha_composite(crop, (cell[0] + (290 - crop.width) // 2, cell[1] + 45))
        draw.text((cell[0] + 18, cell[3] - 62), part_id.upper(), font=font, fill=(224, 235, 237, 255))
        accent = (81, 230, 205, 255) if "glow" in part_id else (185, 138, 55, 255)
        draw.rectangle((cell[0] + 18, cell[3] - 24, cell[2] - 18, cell[3] - 16), fill=accent)
    return review


def _mounted_review(layers: Path) -> Image.Image:
    review = _blank_review()
    mounted = compose_mounted(layers)
    review.alpha_composite(mounted)
    draw = ImageDraw.Draw(review)
    draw.line((80, 1080, 1320, 1080), fill=(185, 138, 55, 255), width=2)
    return review


def _paste_crop(review: Image.Image, source: Image.Image, center: tuple[int, int], size: tuple[int, int]) -> None:
    bbox = source.getchannel("A").getbbox()
    crop = source.crop(bbox)
    crop.thumbnail(size, Image.Resampling.LANCZOS)
    review.alpha_composite(crop, (center[0] - crop.width // 2, center[1] - crop.height // 2))


def build_back_detail(layers: Path, *, include_hidden_axle: bool = True) -> Image.Image:
    review = _blank_review()
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=20)
    body = _compose_body(layers)
    body.putalpha(body.getchannel("A").point(lambda value: round(value * 0.23)))
    review.alpha_composite(body)
    components = (
        ("back-core-housing", (360, 520)),
        ("back-energy-cell", (560, 520)),
        ("back-core-glow", (725, 520)),
        ("hidden-axle", (900, 620)),
        ("front-spool-housing", (1050, 520)),
        ("front-spool-disc", (1210, 520)),
    )
    for part_id, center in components:
        if part_id == "hidden-axle" and not include_hidden_axle:
            continue
        _paste_crop(review, _read_rgba(layers / f"{part_id}.png"), center, (180, 260))
    draw.line((430, 630, 1160, 630), fill=(81, 230, 205, 180), width=3)
    draw.ellipse((650, 604, 676, 630), fill=(17, 26, 38, 255), outline=(228, 185, 91, 255), width=4)
    draw.text((285, 820), "BACK CORE", font=font, fill=(224, 235, 237, 255))
    draw.text((620, 820), "HIDDEN AXLE", font=font, fill=(81, 230, 205, 255))
    draw.text((1040, 820), "FRONT SPOOL", font=font, fill=(224, 235, 237, 255))
    return review


def render_spool_core_pack(root: Path) -> dict[str, object]:
    external_root = root.resolve()
    request = MasterRequest.load(external_root / "request" / "master-request.json")
    authoring = external_root / "authoring"
    layers = external_root / "layers"
    review = external_root / "review"
    evidence = external_root / "evidence"
    for directory in (authoring, layers, review, evidence):
        directory.mkdir(parents=True, exist_ok=True)
    for part_id in BODY_STACK:
        if not (layers / f"{part_id}.png").is_file():
            raise ValueError(f"approved body layer is missing: {part_id}")
    edge = discover_edge()
    if edge is None:
        raise RuntimeError("Microsoft Edge was not found")
    source = authoring / "spool-core.svg"
    source.write_text(build_spool_core_svg(request), encoding="utf-8")
    outputs = render_part_layers(edge, source, layers, SPOOL_CORE_PART_IDS)
    review_outputs = {
        "spool-core-parts": _parts_review(outputs),
        "spool-core-mounted": _mounted_review(layers),
        "spool-core-back-detail": build_back_detail(layers),
    }
    for name, image in review_outputs.items():
        image.save(review / f"{name}.png")
        image.close()
    files = [
        {
            "path": path.relative_to(external_root).as_posix(),
            "sha256": sha256_file(path),
            "role": "part",
            "partId": part_id,
        }
        for part_id, path in outputs.items()
    ]
    files.extend(
        {
            "path": (review / f"{name}.png").relative_to(external_root).as_posix(),
            "sha256": sha256_file(review / f"{name}.png"),
            "role": "review",
        }
        for name in SPOOL_CORE_REVIEW_NAMES
    )
    manifest = {
        "version": 1,
        "stage": "spoolCore",
        "generatedAtNs": time.time_ns(),
        "files": files,
        "connections": {},
    }
    (evidence / "spoolCore-manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )
    return {"stage": "spoolCore", "files": len(files), "source": str(source)}


def main() -> int:
    parser = argparse.ArgumentParser(description="Build complete-player spool/core layers and review evidence")
    parser.add_argument("--external-root", type=Path, required=True)
    args = parser.parse_args()
    report = render_spool_core_pack(args.external_root)
    print(f"COMPLETE_MASTER_SPOOL_CORE_OK files={report['files']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
