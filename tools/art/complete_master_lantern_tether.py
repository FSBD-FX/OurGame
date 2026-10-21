from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from complete_master_contract import MasterRequest, sha256_file
from complete_master_svg import discover_edge, render_part_layers
from complete_master_weapon_hands import compose_held


LANTERN_TETHER_PART_IDS = (
    "lantern-housing",
    "lantern-glow",
    "tether-back-connector",
    "tether-energy-middle",
    "tether-lantern-connector",
)
LANTERN_TETHER_REVIEW_NAMES = (
    "lantern-tether-parts",
    "lantern-tether-connected",
    "lantern-tether-energy-off",
)
CANVAS = (1402, 1122)
OUTLINE = "#101820"
BRASS = "#B98A37"
BRASS_LIGHT = "#E4B95B"
BRASS_DARK = "#6D4B1D"
STEEL = "#6F7D84"
STEEL_LIGHT = "#BBC5C5"
ENERGY = "#51E6CD"
ENERGY_LIGHT = "#D7FFF7"

TETHER_CONNECTIONS = {
    "back.in": (541, 490),
    "back.out": (520, 430),
    "energy.in": (520, 430),
    "energy.out": (445, 340),
    "lantern.in": (445, 340),
    "lantern.out": (446, 318),
}

BOUNDS = {
    "lantern-housing": (365, 145, 170, 190),
    "lantern-glow": (400, 180, 100, 112),
    "tether-back-connector": (506, 416, 48, 88),
    "tether-energy-middle": (432, 328, 105, 116),
    "tether-lantern-connector": (426, 310, 40, 40),
}


def _group(part_id: str, body: str, attributes: str) -> str:
    bbox = " ".join(str(value) for value in BOUNDS[part_id])
    return f'<g id="part-{part_id}" data-part-id="{part_id}" data-bbox="{bbox}"{attributes}>{body}</g>'


def build_lantern_tether_svg(request: MasterRequest) -> str:
    if request.canvas != CANVAS or request.baseline_y != 1080 or request.facing != "right":
        raise ValueError("lantern request must use the canonical canvas, baseline, and facing")
    groups = [
        _group(
            "lantern-housing",
            f'<path fill-rule="evenodd" d="M404 153 L496 153 Q523 169 531 195 L531 278 Q523 309 497 326 L400 326 Q372 311 366 282 L366 196 Q374 168 404 153 Z M414 185 L486 185 Q500 196 501 213 L501 269 Q498 288 483 296 L415 296 Q399 286 397 268 L397 214 Q399 196 414 185 Z" fill="url(#lantern-metal)" stroke="{OUTLINE}" stroke-width="9" stroke-linejoin="round"/>'
            f'<path d="M403 162 L422 144 L480 144 L500 162 M385 183 L399 198 M513 183 L500 198" fill="none" stroke="{BRASS_LIGHT}" stroke-width="8" stroke-linecap="round"/>'
            f'<path d="M387 280 Q407 312 449 315 Q489 313 510 280" fill="none" stroke="{BRASS}" stroke-width="9"/>'
            f'<path d="M418 176 L480 176 M411 305 L487 305" stroke="{STEEL_LIGHT}" stroke-width="5" opacity="0.72"/>'
            f'<circle cx="389" cy="229" r="8" fill="{BRASS_DARK}"/><circle cx="510" cy="229" r="8" fill="{BRASS_DARK}"/>',
            ' data-form="complete-floating-lantern-v1" data-light-rig="canvas-soft-top-left-v1"',
        ),
        _group(
            "lantern-glow",
            f'<rect x="403" y="183" width="94" height="112" rx="32" fill="{ENERGY}" opacity="0.24"/>'
            f'<path d="M450 190 C478 210 489 235 481 260 C474 281 463 291 450 296 C431 282 421 260 421 238 C421 216 432 201 450 190 Z" fill="url(#energy)" stroke="{ENERGY_LIGHT}" stroke-width="4"/>',
            ' data-energy="glow-only"',
        ),
        _group(
            "tether-back-connector",
            f'<path d="M541 490 C532 472 527 450 520 430" fill="none" stroke="{OUTLINE}" stroke-width="22" stroke-linecap="round"/>'
            f'<path d="M541 490 C532 472 527 450 520 430" fill="none" stroke="url(#brass)" stroke-width="12" stroke-linecap="round"/>'
            f'<circle cx="541" cy="490" r="13" fill="{BRASS_DARK}" stroke="{BRASS_LIGHT}" stroke-width="5"/>',
            ' data-form="shoulder-back-core-terminal-a2"',
        ),
        _group(
            "tether-energy-middle",
            f'<path d="M520 430 C505 406 478 397 468 374 C460 356 455 349 445 340" fill="none" stroke="{ENERGY}" stroke-width="19" stroke-linecap="round" opacity="0.18"/>'
            f'<path d="M520 430 C505 406 478 397 468 374 C460 356 455 349 445 340" fill="none" stroke="url(#energy)" stroke-width="9" stroke-linecap="round"/>'
            f'<path d="M520 430 C505 406 478 397 468 374 C460 356 455 349 445 340" fill="none" stroke="{ENERGY_LIGHT}" stroke-width="3" stroke-linecap="round" opacity="0.75"/>',
            ' data-energy="energy-only" data-form="shoulder-to-lantern-sway-a2"',
        ),
        _group(
            "tether-lantern-connector",
            f'<path d="M445 340 L446 318" fill="none" stroke="{OUTLINE}" stroke-width="20" stroke-linecap="round"/>'
            f'<path d="M445 340 L446 318" fill="none" stroke="url(#brass)" stroke-width="11" stroke-linecap="round"/>'
            f'<circle cx="446" cy="318" r="12" fill="{BRASS_DARK}" stroke="{BRASS_LIGHT}" stroke-width="5"/>',
            ' data-form="solid-terminal-v1"',
        ),
    ]
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" viewBox="0 0 1402 1122">'
        '<defs>'
        '<linearGradient id="lantern-metal" gradientUnits="userSpaceOnUse" x1="365" y1="140" x2="535" y2="335">'
        f'<stop offset="0" stop-color="{STEEL_LIGHT}"/><stop offset="0.5" stop-color="{STEEL}"/><stop offset="1" stop-color="#39464D"/>'
        '</linearGradient>'
        '<linearGradient id="brass" x1="0" y1="0" x2="1" y2="1">'
        f'<stop offset="0" stop-color="{BRASS_LIGHT}"/><stop offset="0.5" stop-color="{BRASS}"/><stop offset="1" stop-color="{BRASS_DARK}"/>'
        '</linearGradient>'
        '<linearGradient id="energy" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{ENERGY_LIGHT}"/><stop offset="0.45" stop-color="{ENERGY}"/><stop offset="1" stop-color="#168E88"/>'
        '</linearGradient>'
        '</defs>' + "".join(groups) + '</svg>'
    )


def _read(path: Path) -> Image.Image:
    with Image.open(path) as source:
        result = source.convert("RGBA")
        result.load()
    return result


def compose_lantern_system(layers: Path, *, energy_on: bool) -> Image.Image:
    result = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    result.alpha_composite(_read(layers / "tether-back-connector.png"))
    if energy_on:
        result.alpha_composite(_read(layers / "tether-energy-middle.png"))
    result.alpha_composite(_read(layers / "tether-lantern-connector.png"))
    result.alpha_composite(_read(layers / "lantern-housing.png"))
    if energy_on:
        result.alpha_composite(_read(layers / "lantern-glow.png"))
    return result


def _blank_review() -> Image.Image:
    image = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((22, 22, 1379, 1099), radius=28, fill=(11, 16, 24, 255))
    draw.rounded_rectangle((46, 46, 1355, 1075), radius=20, fill=(17, 26, 38, 255), outline=(52, 70, 90, 255), width=3)
    return image


def _parts_review(outputs: dict[str, Path]) -> Image.Image:
    review = _blank_review()
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=16)
    for index, part_id in enumerate(LANTERN_TETHER_PART_IDS):
        cell = (60 + index * 264, 155, 300 + index * 264, 900)
        draw.rounded_rectangle(cell, radius=18, fill=(22, 34, 49, 255), outline=(52, 70, 90, 255), width=2)
        source = _read(outputs[part_id])
        crop = source.crop(source.getchannel("A").getbbox())
        crop.thumbnail((190, 500), Image.Resampling.LANCZOS)
        review.alpha_composite(crop, (cell[0] + (240 - crop.width) // 2, cell[1] + 90))
        draw.text((cell[0] + 14, cell[3] - 70), part_id.upper(), font=font, fill=(224, 235, 237, 255))
        accent = (81, 230, 205, 255) if "glow" in part_id or "energy" in part_id else (185, 138, 55, 255)
        draw.rectangle((cell[0] + 14, cell[3] - 28, cell[2] - 14, cell[3] - 18), fill=accent)
    return review


def _connected_review(layers: Path, *, energy_on: bool) -> Image.Image:
    review = compose_held(layers, include_overlays=True)
    review.alpha_composite(compose_lantern_system(layers, energy_on=energy_on))
    return review


def render_lantern_tether_pack(root: Path) -> dict[str, object]:
    external_root = root.resolve()
    request = MasterRequest.load(external_root / "request" / "master-request.json")
    authoring, layers, review, evidence = (external_root / name for name in ("authoring", "layers", "review", "evidence"))
    for directory in (authoring, layers, review, evidence):
        directory.mkdir(parents=True, exist_ok=True)
    for required in ("head-shell", "back-core-housing", "weapon-held"):
        if required == "weapon-held":
            if not (review / "weapon-held.png").is_file():
                raise ValueError("approved prerequisite review is missing: weapon-held")
        elif not (layers / f"{required}.png").is_file():
            raise ValueError(f"approved prerequisite layer is missing: {required}")
    edge = discover_edge()
    if edge is None:
        raise RuntimeError("Microsoft Edge was not found")
    source = authoring / "lantern-tether.svg"
    source.write_text(build_lantern_tether_svg(request), encoding="utf-8")
    outputs = render_part_layers(edge, source, layers, LANTERN_TETHER_PART_IDS)
    review_outputs = {
        "lantern-tether-parts": _parts_review(outputs),
        "lantern-tether-connected": _connected_review(layers, energy_on=True),
        "lantern-tether-energy-off": _connected_review(layers, energy_on=False),
    }
    for name, image in review_outputs.items():
        image.save(review / f"{name}.png")
        image.close()
    files = [{"path": path.relative_to(external_root).as_posix(), "sha256": sha256_file(path), "role": "part", "partId": part_id} for part_id, path in outputs.items()]
    files.extend({"path": (review / f"{name}.png").relative_to(external_root).as_posix(), "sha256": sha256_file(review / f"{name}.png"), "role": "review"} for name in LANTERN_TETHER_REVIEW_NAMES)
    manifest = {
        "version": 1,
        "stage": "lanternTether",
        "generatedAtNs": time.time_ns(),
        "files": files,
        "connections": {
            connection_id: {
                "partId": (
                    "tether-back-connector" if connection_id.startswith("back.") else
                    "tether-energy-middle" if connection_id.startswith("energy.") else
                    "tether-lantern-connector"
                ),
                "point": list(point),
            }
            for connection_id, point in TETHER_CONNECTIONS.items()
        },
    }
    (evidence / "lanternTether-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return {"stage": "lanternTether", "files": len(files), "source": str(source)}


def main() -> int:
    parser = argparse.ArgumentParser(description="Build complete-player lantern and tether layers")
    parser.add_argument("--external-root", type=Path, required=True)
    args = parser.parse_args()
    report = render_lantern_tether_pack(args.external_root)
    print(f"COMPLETE_MASTER_LANTERN_TETHER_OK files={report['files']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
