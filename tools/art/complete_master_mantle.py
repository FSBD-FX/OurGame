from __future__ import annotations

import argparse
import json
import time
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from complete_master_body import BODY_STACK
from complete_master_contract import MasterRequest, sha256_file
from complete_master_spool_core import compose_mounted
from complete_master_svg import discover_edge, render_part_layers


MANTLE_PART_IDS = ("shoulder-collar", "collar-fastener", "front-collar-flap", "mantle")
MANTLE_REVIEW_NAMES = ("mantle-parts", "mantle-front-back", "mantle-mounted")
CANVAS = (1402, 1122)
OUTLINE = "#101820"
CLOTH = "#172E36"
CLOTH_MID = "#244A50"
CLOTH_LIGHT = "#326B68"
CLOTH_INNER = "#0D2028"
BRASS = "#B98A37"
BRASS_LIGHT = "#E4B95B"
BRASS_DARK = "#6D4B1D"

BOUNDS = {
    "shoulder-collar": (510, 407, 285, 125),
    "collar-fastener": (520, 455, 54, 54),
    "front-collar-flap": (685, 440, 135, 165),
    "mantle": (215, 430, 440, 620),
}


def _group(part_id: str, body: str, attributes: str = "") -> str:
    bbox = " ".join(str(value) for value in BOUNDS[part_id])
    return f'<g id="part-{part_id}" data-part-id="{part_id}" data-bbox="{bbox}"{attributes}>{body}</g>'


def build_mantle_svg(request: MasterRequest) -> str:
    if request.canvas != CANVAS or request.baseline_y != 1080 or request.facing != "right":
        raise ValueError("mantle request must use the canonical canvas, baseline, and facing")
    groups = [
        _group(
            "shoulder-collar",
            f'<path d="M525 475 Q555 420 625 415 Q715 410 790 475 L760 530 Q690 490 620 492 Q565 493 530 520 Z" fill="url(#collar)" stroke="{OUTLINE}" stroke-width="10" stroke-linejoin="round"/>'
            f'<path d="M548 476 Q610 440 684 450 Q735 456 770 481" fill="none" stroke="{BRASS_LIGHT}" stroke-width="7" stroke-linecap="round" opacity="0.75"/>'
            f'<path d="M555 505 Q625 475 740 508" fill="none" stroke="{BRASS_DARK}" stroke-width="5" opacity="0.65"/>',
            ' data-form="rigid-shoulder-collar-v1" data-light-rig="canvas-soft-top-left-v1"',
        ),
        _group(
            "collar-fastener",
            f'<circle cx="548" cy="482" r="22" fill="url(#brass)" stroke="{OUTLINE}" stroke-width="7"/>'
            f'<circle cx="548" cy="482" r="8" fill="{BRASS_DARK}"/>'
            f'<path d="M539 469 L557 495" stroke="{BRASS_LIGHT}" stroke-width="4"/>',
            ' data-form="separate-fastener-v1"',
        ),
        _group(
            "front-collar-flap",
            f'<path d="M701 449 Q765 460 807 507 L782 592 Q746 560 704 553 L690 485 Z" fill="url(#cloth-front)" stroke="{OUTLINE}" stroke-width="9" stroke-linejoin="round"/>'
            f'<path d="M708 469 Q755 478 791 513" fill="none" stroke="{BRASS}" stroke-width="7" stroke-linecap="round"/>'
            f'<path d="M720 500 Q755 515 783 542" fill="none" stroke="{CLOTH_LIGHT}" stroke-width="5" opacity="0.6"/>',
            ' data-form="separate-front-flap-v1" data-light-rig="canvas-soft-top-left-v1"',
        ),
        _group(
            "mantle",
            f'<path fill-rule="evenodd" d="M560 447 Q500 450 455 492 Q380 553 320 638 Q264 718 239 802 L220 935 L330 850 L372 1040 L455 930 L520 1020 L600 930 L645 1035 L650 820 Q654 716 644 610 Q635 515 585 462 Q573 447 560 447 Z M333 628 Q352 614 370 628 Q381 650 365 670 Q344 681 327 663 Q319 645 333 628 Z M417 795 Q438 780 458 796 Q471 819 452 841 Q430 851 411 833 Q401 814 417 795 Z" fill="url(#mantle-cloth)" stroke="{OUTLINE}" stroke-width="11" stroke-linejoin="round"/>'
            f'<path d="M552 466 Q485 505 424 575 Q352 658 293 770 Q268 821 251 895" fill="none" stroke="{CLOTH_LIGHT}" stroke-width="12" stroke-linecap="round" opacity="0.45"/>'
            f'<path d="M566 470 Q612 548 610 654 Q608 770 582 900" fill="none" stroke="{CLOTH_INNER}" stroke-width="20" stroke-linecap="round" opacity="0.62"/>'
            f'<path d="M233 919 L330 837 L374 1018 L454 908 L520 998 L600 908 L636 1012" fill="none" stroke="{BRASS}" stroke-width="9" stroke-linejoin="round"/>'
            f'<path d="M277 724 Q352 700 407 723 M260 783 Q330 760 384 782 M469 660 Q528 680 577 656" fill="none" stroke="{CLOTH_LIGHT}" stroke-width="5" opacity="0.28"/>'
            f'<path d="M315 625 Q349 600 380 626 M402 793 Q438 766 468 796" fill="none" stroke="{BRASS_DARK}" stroke-width="6" opacity="0.75"/>',
            ' data-form="continuous-weighted-cloth-v1" data-motion-read="left-root-heavy-lag-v1" data-light-rig="canvas-soft-top-left-v1" data-hole-count="2" data-baked-hardware="none"',
        ),
    ]
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" viewBox="0 0 1402 1122">'
        '<defs>'
        '<linearGradient id="mantle-cloth" gradientUnits="userSpaceOnUse" x1="250" y1="430" x2="620" y2="1010">'
        f'<stop offset="0" stop-color="{CLOTH_LIGHT}"/><stop offset="0.42" stop-color="{CLOTH_MID}"/><stop offset="1" stop-color="{CLOTH_INNER}"/>'
        '</linearGradient>'
        '<linearGradient id="cloth-front" gradientUnits="userSpaceOnUse" x1="680" y1="430" x2="820" y2="610">'
        f'<stop offset="0" stop-color="{CLOTH_LIGHT}"/><stop offset="0.55" stop-color="{CLOTH}"/><stop offset="1" stop-color="{CLOTH_INNER}"/>'
        '</linearGradient>'
        '<linearGradient id="collar" gradientUnits="userSpaceOnUse" x1="500" y1="400" x2="800" y2="540">'
        '<stop offset="0" stop-color="#AAB5B9"/><stop offset="0.5" stop-color="#66747D"/><stop offset="1" stop-color="#34414A"/>'
        '</linearGradient>'
        '<linearGradient id="brass" x1="0" y1="0" x2="1" y2="1">'
        f'<stop offset="0" stop-color="{BRASS_LIGHT}"/><stop offset="0.5" stop-color="{BRASS}"/><stop offset="1" stop-color="{BRASS_DARK}"/>'
        '</linearGradient>'
        '</defs>' + "".join(groups) + '</svg>'
    )


def count_alpha_components(alpha: Image.Image, *, threshold: int) -> int:
    pixels = alpha.load()
    width, height = alpha.size
    visited: set[tuple[int, int]] = set()
    components = 0
    for y in range(height):
        for x in range(width):
            if (x, y) in visited or pixels[x, y] <= threshold:
                continue
            components += 1
            queue = deque([(x, y)])
            visited.add((x, y))
            while queue:
                current_x, current_y = queue.popleft()
                for next_x, next_y in ((current_x - 1, current_y), (current_x + 1, current_y), (current_x, current_y - 1), (current_x, current_y + 1)):
                    if 0 <= next_x < width and 0 <= next_y < height and (next_x, next_y) not in visited and pixels[next_x, next_y] > threshold:
                        visited.add((next_x, next_y))
                        queue.append((next_x, next_y))
    return components


def _blank_review() -> Image.Image:
    image = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((22, 22, 1379, 1099), radius=28, fill=(11, 16, 24, 255))
    draw.rounded_rectangle((46, 46, 1355, 1075), radius=20, fill=(17, 26, 38, 255), outline=(52, 70, 90, 255), width=3)
    return image


def _read(path: Path) -> Image.Image:
    with Image.open(path) as source:
        result = source.convert("RGBA")
        result.load()
    return result


def _parts_review(outputs: dict[str, Path]) -> Image.Image:
    review = _blank_review()
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=18)
    for index, part_id in enumerate(MANTLE_PART_IDS):
        cell = (80 + index * 326, 175, 370 + index * 326, 890)
        draw.rounded_rectangle(cell, radius=18, fill=(22, 34, 49, 255), outline=(52, 70, 90, 255), width=2)
        source = _read(outputs[part_id])
        crop = source.crop(source.getchannel("A").getbbox())
        crop.thumbnail((245, 530), Image.Resampling.LANCZOS)
        review.alpha_composite(crop, (cell[0] + (290 - crop.width) // 2, cell[1] + 60))
        draw.text((cell[0] + 18, cell[3] - 70), part_id.upper(), font=font, fill=(224, 235, 237, 255))
        draw.rectangle((cell[0] + 18, cell[3] - 27, cell[2] - 18, cell[3] - 18), fill=(185, 138, 55, 255))
    return review


def _front_back_review(layers: Path) -> Image.Image:
    review = _blank_review()
    mantle = _read(layers / "mantle.png")
    bbox = mantle.getchannel("A").getbbox()
    crop = mantle.crop(bbox)
    crop.thumbnail((480, 700), Image.Resampling.LANCZOS)
    review.alpha_composite(crop, (360 - crop.width // 2, 560 - crop.height // 2))
    inner = crop.copy()
    tint = Image.new("RGBA", inner.size, (7, 25, 34, 165))
    tint.putalpha(inner.getchannel("A").point(lambda value: round(value * 0.72)))
    inner.alpha_composite(tint)
    inner = inner.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    review.alpha_composite(inner, (1030 - inner.width // 2, 560 - inner.height // 2))
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=22)
    draw.text((270, 940), "OUTER FACE", font=font, fill=(224, 235, 237, 255))
    draw.text((930, 940), "INNER FACE", font=font, fill=(81, 230, 205, 255))
    return review


def _mounted_review(layers: Path) -> Image.Image:
    review = _blank_review()
    mantle = _read(layers / "mantle.png")
    review.alpha_composite(mantle)
    review.alpha_composite(compose_mounted(layers))
    for part_id in ("shoulder-collar", "collar-fastener", "front-collar-flap"):
        review.alpha_composite(_read(layers / f"{part_id}.png"))
    ImageDraw.Draw(review).line((80, 1080, 1320, 1080), fill=(185, 138, 55, 255), width=2)
    return review


def render_mantle_pack(root: Path) -> dict[str, object]:
    external_root = root.resolve()
    request = MasterRequest.load(external_root / "request" / "master-request.json")
    authoring, layers, review, evidence = (external_root / name for name in ("authoring", "layers", "review", "evidence"))
    for directory in (authoring, layers, review, evidence):
        directory.mkdir(parents=True, exist_ok=True)
    for part_id in BODY_STACK:
        if not (layers / f"{part_id}.png").is_file():
            raise ValueError(f"approved body layer is missing: {part_id}")
    edge = discover_edge()
    if edge is None:
        raise RuntimeError("Microsoft Edge was not found")
    source = authoring / "mantle.svg"
    source.write_text(build_mantle_svg(request), encoding="utf-8")
    outputs = render_part_layers(edge, source, layers, MANTLE_PART_IDS)
    review_outputs = {
        "mantle-parts": _parts_review(outputs),
        "mantle-front-back": _front_back_review(layers),
        "mantle-mounted": _mounted_review(layers),
    }
    for name, image in review_outputs.items():
        image.save(review / f"{name}.png")
        image.close()
    files = [{"path": path.relative_to(external_root).as_posix(), "sha256": sha256_file(path), "role": "part", "partId": part_id} for part_id, path in outputs.items()]
    files.extend({"path": (review / f"{name}.png").relative_to(external_root).as_posix(), "sha256": sha256_file(review / f"{name}.png"), "role": "review"} for name in MANTLE_REVIEW_NAMES)
    manifest = {
        "version": 1,
        "stage": "mantle",
        "generatedAtNs": time.time_ns(),
        "files": files,
        "connections": {"mantle-root": {"partId": "mantle", "point": [545, 475]}},
    }
    (evidence / "mantle-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return {"stage": "mantle", "files": len(files), "source": str(source)}


def main() -> int:
    parser = argparse.ArgumentParser(description="Build complete-player mantle layers and review evidence")
    parser.add_argument("--external-root", type=Path, required=True)
    args = parser.parse_args()
    report = render_mantle_pack(args.external_root)
    print(f"COMPLETE_MASTER_MANTLE_OK files={report['files']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
