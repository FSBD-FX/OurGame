from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from complete_master_contract import MasterRequest, sha256_file
from complete_master_mantle import _mounted_review as compose_mantle_mounted
from complete_master_svg import discover_edge, render_part_layers


WEAPON_HAND_PART_IDS = ("half-bell-cleaver", "left-grip-overlay", "right-grip-overlay")
WEAPON_HAND_REVIEW_NAMES = ("weapon-grip-parts", "weapon-unheld", "weapon-held")
CANVAS = (1402, 1122)
OUTLINE = "#101820"
BRASS = "#B98A37"
BRASS_LIGHT = "#E4B95B"
BRASS_DARK = "#6D4B1D"
STEEL = "#7B858A"
STEEL_LIGHT = "#CDD3D2"
STEEL_DARK = "#414C52"
LEATHER = "#6E3D26"
LEATHER_LIGHT = "#A86C3D"
GLOVE = "#29333D"

BOUNDS = {
    "half-bell-cleaver": (505, 755, 705, 215),
    "left-grip-overlay": (526, 775, 42, 45),
    "right-grip-overlay": (812, 775, 43, 45),
}


def _group(part_id: str, body: str, attributes: str) -> str:
    bbox = " ".join(str(value) for value in BOUNDS[part_id])
    return f'<g id="part-{part_id}" data-part-id="{part_id}" data-bbox="{bbox}"{attributes}>{body}</g>'


def build_weapon_hands_svg(request: MasterRequest) -> str:
    if request.canvas != CANVAS or request.baseline_y != 1080 or request.facing != "right":
        raise ValueError("weapon request must use the canonical canvas, baseline, and facing")
    groups = [
        _group(
            "half-bell-cleaver",
            f'<path d="M530 784 L895 784 L928 762 L951 788 L1186 788 Q1198 788 1203 800 L1168 878 Q1135 936 1064 959 Q997 977 932 946 Q884 922 852 886 L835 856 L530 856 Q510 856 510 836 L510 805 Q510 784 530 784 Z" fill="url(#weapon-steel)" stroke="{OUTLINE}" stroke-width="11" stroke-linejoin="round"/>'
            f'<path d="M530 797 L918 797" fill="none" stroke="{LEATHER}" stroke-width="46" stroke-linecap="round"/>'
            f'<path d="M530 797 L918 797" fill="none" stroke="{LEATHER_LIGHT}" stroke-width="9" stroke-linecap="round" opacity="0.75"/>'
            f'<path d="M552 777 L552 817 M582 777 L582 817 M612 777 L612 817 M642 777 L642 817 M672 777 L672 817 M702 777 L702 817 M732 777 L732 817 M762 777 L762 817 M792 777 L792 817 M822 777 L822 817 M852 777 L852 817 M882 777 L882 817" stroke="{BRASS_DARK}" stroke-width="7" opacity="0.82"/>'
            f'<path d="M913 790 L947 804 L1182 804 Q1151 869 1087 912 Q1024 950 955 919 Q913 899 881 855 L850 830 Z" fill="url(#bell-face)" stroke="{BRASS}" stroke-width="7" stroke-linejoin="round"/>'
            f'<path d="M955 815 Q1018 842 1082 822 Q1133 807 1171 806" fill="none" stroke="{STEEL_LIGHT}" stroke-width="8" opacity="0.7"/>'
            f'<path d="M952 914 Q1025 951 1090 910" fill="none" stroke="{BRASS_LIGHT}" stroke-width="8" stroke-linecap="round"/>'
            f'<path d="M935 818 L914 851 L881 855" fill="none" stroke="{BRASS_DARK}" stroke-width="10" stroke-linejoin="round"/>',
            ' data-form="complete-half-bell-cleaver-v1" data-handle="complete-uncropped" data-baked-hands="none" data-light-rig="canvas-soft-top-left-v1"',
        ),
        _group(
            "left-grip-overlay",
            f'<path d="M530 787 Q548 774 564 789 L568 813 Q552 823 534 815 Q526 803 530 787 Z" fill="url(#glove)" stroke="{OUTLINE}" stroke-width="6"/>'
            f'<path d="M535 792 Q549 785 562 793 M534 803 Q551 795 565 803" fill="none" stroke="{BRASS_DARK}" stroke-width="4" stroke-linecap="round"/>',
            ' data-form="minimal-finger-occlusion-v1"',
        ),
        _group(
            "right-grip-overlay",
            f'<path d="M815 787 Q833 774 850 789 L855 812 Q838 823 820 815 Q811 803 815 787 Z" fill="url(#glove)" stroke="{OUTLINE}" stroke-width="6"/>'
            f'<path d="M820 792 Q835 785 848 793 M819 803 Q837 795 852 803" fill="none" stroke="{BRASS_DARK}" stroke-width="4" stroke-linecap="round"/>',
            ' data-form="minimal-finger-occlusion-v1"',
        ),
    ]
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" viewBox="0 0 1402 1122">'
        '<defs>'
        '<linearGradient id="weapon-steel" gradientUnits="userSpaceOnUse" x1="500" y1="750" x2="1210" y2="970">'
        f'<stop offset="0" stop-color="{STEEL_LIGHT}"/><stop offset="0.5" stop-color="{STEEL}"/><stop offset="1" stop-color="{STEEL_DARK}"/>'
        '</linearGradient>'
        '<linearGradient id="bell-face" gradientUnits="userSpaceOnUse" x1="850" y1="780" x2="1185" y2="955">'
        '<stop offset="0" stop-color="#D6934B"/><stop offset="0.55" stop-color="#A75931"/><stop offset="1" stop-color="#6D3026"/>'
        '</linearGradient>'
        '<linearGradient id="glove" x1="0" y1="0" x2="1" y2="1">'
        '<stop offset="0" stop-color="#45515B"/><stop offset="0.6" stop-color="#29333D"/><stop offset="1" stop-color="#171E25"/>'
        '</linearGradient>'
        '</defs>' + "".join(groups) + '</svg>'
    )


def _read(path: Path) -> Image.Image:
    with Image.open(path) as source:
        result = source.convert("RGBA")
        result.load()
    return result


def _base_dressed(layers: Path) -> Image.Image:
    return compose_mantle_mounted(layers)


def compose_held(layers: Path, *, include_overlays: bool) -> Image.Image:
    result = _base_dressed(layers)
    result.alpha_composite(_read(layers / "half-bell-cleaver.png"))
    if include_overlays:
        result.alpha_composite(_read(layers / "left-grip-overlay.png"))
        result.alpha_composite(_read(layers / "right-grip-overlay.png"))
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
    font = ImageFont.load_default(size=18)
    cells = ((70, 150, 870, 900), (900, 150, 1110, 900), (1130, 150, 1340, 900))
    for part_id, cell in zip(WEAPON_HAND_PART_IDS, cells):
        draw.rounded_rectangle(cell, radius=18, fill=(22, 34, 49, 255), outline=(52, 70, 90, 255), width=2)
        source = _read(outputs[part_id])
        crop = source.crop(source.getchannel("A").getbbox())
        crop.thumbnail((cell[2] - cell[0] - 50, 520), Image.Resampling.LANCZOS)
        review.alpha_composite(crop, (cell[0] + (cell[2] - cell[0] - crop.width) // 2, cell[1] + 100))
        draw.text((cell[0] + 18, cell[3] - 70), part_id.upper(), font=font, fill=(224, 235, 237, 255))
        draw.rectangle((cell[0] + 18, cell[3] - 28, cell[2] - 18, cell[3] - 18), fill=(185, 138, 55, 255))
    return review


def _character_review(layers: Path, *, held: bool) -> Image.Image:
    review = _base_dressed(layers)
    review.alpha_composite(_read(layers / "half-bell-cleaver.png"))
    if held:
        review.alpha_composite(_read(layers / "left-grip-overlay.png"))
        review.alpha_composite(_read(layers / "right-grip-overlay.png"))
    return review


def render_weapon_hands_pack(root: Path) -> dict[str, object]:
    external_root = root.resolve()
    request = MasterRequest.load(external_root / "request" / "master-request.json")
    authoring, layers, review, evidence = (external_root / name for name in ("authoring", "layers", "review", "evidence"))
    for directory in (authoring, layers, review, evidence):
        directory.mkdir(parents=True, exist_ok=True)
    for required in ("left-hand", "right-hand", "mantle", "shoulder-collar", "front-spool-disc"):
        if not (layers / f"{required}.png").is_file():
            raise ValueError(f"approved prerequisite layer is missing: {required}")
    edge = discover_edge()
    if edge is None:
        raise RuntimeError("Microsoft Edge was not found")
    source = authoring / "weapon-hands.svg"
    source.write_text(build_weapon_hands_svg(request), encoding="utf-8")
    outputs = render_part_layers(edge, source, layers, WEAPON_HAND_PART_IDS)
    review_outputs = {
        "weapon-grip-parts": _parts_review(outputs),
        "weapon-unheld": _character_review(layers, held=False),
        "weapon-held": _character_review(layers, held=True),
    }
    for name, image in review_outputs.items():
        image.save(review / f"{name}.png")
        image.close()
    files = []
    for part_id, path in outputs.items():
        files.append({
            "path": path.relative_to(external_root).as_posix(),
            "sha256": sha256_file(path),
            "role": "overlay" if part_id.endswith("grip-overlay") else "part",
            "partId": part_id,
        })
    files.extend({"path": (review / f"{name}.png").relative_to(external_root).as_posix(), "sha256": sha256_file(review / f"{name}.png"), "role": "review"} for name in WEAPON_HAND_REVIEW_NAMES)
    manifest = {
        "version": 1,
        "stage": "weaponHands",
        "generatedAtNs": time.time_ns(),
        "files": files,
        "connections": {
            "left-grip": {"partId": "left-grip-overlay", "point": [548, 798]},
            "right-grip": {"partId": "right-grip-overlay", "point": [835, 798]},
        },
    }
    (evidence / "weaponHands-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return {"stage": "weaponHands", "files": len(files), "source": str(source)}


def main() -> int:
    parser = argparse.ArgumentParser(description="Build complete-player weapon and grip-overlay layers")
    parser.add_argument("--external-root", type=Path, required=True)
    args = parser.parse_args()
    report = render_weapon_hands_pack(args.external_root)
    print(f"COMPLETE_MASTER_WEAPON_HANDS_OK files={report['files']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
