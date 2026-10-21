from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from complete_master_contract import MasterRequest, sha256_file
from complete_master_structure import BODY_SUBJECT_IDS, JOINTS
from complete_master_svg import discover_edge, render_part_layers


BODY_PART_IDS = tuple(BODY_SUBJECT_IDS)
BODY_STACK = (
    "inner-body",
    "left-thigh",
    "left-shin",
    "left-boot",
    "left-upper-arm",
    "left-forearm",
    "left-hand",
    "torso-shell",
    "pelvis-shell",
    "head-shell",
    "right-thigh",
    "right-shin",
    "right-boot",
    "right-upper-arm",
    "right-forearm",
    "right-hand",
)
BODY_REVIEW_NAMES = (
    "body-parts",
    "body-uncovered",
    "body-uncovered-96px",
)

CANVAS = (1402, 1122)
OUTLINE = "#101820"
INNER = "#29333D"
INNER_LIGHT = "#424E59"
CERAMIC = "#E7DEC7"
CERAMIC_SHADE = "#B7AF9A"
CERAMIC_HIGHLIGHT = "#FFF8E7"
BRASS = "#B98A37"
BRASS_DARK = "#6D4B1D"
ENERGY = "#51E6CD"


BOUNDS = {
    "inner-body": (500, 380, 390, 680),
    "head-shell": (590, 230, 240, 210),
    "torso-shell": (580, 410, 180, 340),
    "pelvis-shell": (590, 735, 160, 120),
    "left-upper-arm": (520, 480, 90, 180),
    "left-forearm": (500, 630, 90, 150),
    "left-hand": (520, 750, 85, 85),
    "right-upper-arm": (730, 490, 95, 180),
    "right-forearm": (770, 640, 95, 155),
    "right-hand": (805, 760, 85, 80),
    "left-thigh": (585, 825, 70, 120),
    "left-shin": (585, 925, 70, 120),
    "left-boot": (555, 1025, 115, 55),
    "right-thigh": (685, 825, 70, 120),
    "right-shin": (685, 925, 70, 120),
    "right-boot": (675, 1025, 120, 55),
}

CERAMIC_PART_IDS = frozenset(
    {
        "head-shell",
        "torso-shell",
        "pelvis-shell",
        "left-upper-arm",
        "left-forearm",
        "right-upper-arm",
        "right-forearm",
        "left-thigh",
        "left-shin",
        "left-boot",
        "right-thigh",
        "right-shin",
        "right-boot",
    }
)


def _group(
    part_id: str,
    body: str,
    *,
    form: str | None = None,
    depth_role: str | None = None,
) -> str:
    bbox = " ".join(str(value) for value in BOUNDS[part_id])
    form_attribute = f' data-form="{form}"' if form else ""
    depth_attribute = f' data-depth-role="{depth_role}"' if depth_role else ""
    light_attribute = (
        ' data-light-rig="canvas-soft-top-left-v1"' if part_id in CERAMIC_PART_IDS else ""
    )
    return f'<g id="part-{part_id}" data-part-id="{part_id}" data-bbox="{bbox}"{form_attribute}{depth_attribute}{light_attribute}>{body}</g>'


def _ceramic_path(path: str, *, far: bool = False) -> str:
    base = "url(#ceramic-main)"
    highlight = CERAMIC_HIGHLIGHT
    return (
        f'<path d="{path}" fill="{base}" stroke="{OUTLINE}" stroke-width="8" stroke-linejoin="round"/>'
        f'<path d="{path}" fill="none" stroke="{highlight}" stroke-width="3" stroke-linejoin="round" opacity="0.75"/>'
    )


def build_body_svg(request: MasterRequest) -> str:
    if request.canvas != CANVAS or request.baseline_y != 1080 or request.facing != "right":
        raise ValueError("body request must use the canonical canvas, baseline, and facing")
    groups = [
        _group(
            "inner-body",
            f'<path d="M650 425 L665 520 L665 800 M590 520 L550 645 L555 765 M750 530 L800 655 L830 780 M620 840 L620 935 L620 1035 M720 840 L720 935 L720 1035" fill="none" stroke="{OUTLINE}" stroke-width="54" stroke-linecap="round" stroke-linejoin="round"/>'
            f'<path d="M650 425 L665 520 L665 800 M590 520 L550 645 L555 765 M750 530 L800 655 L830 780 M620 840 L620 935 L620 1035 M720 840 L720 935 L720 1035" fill="none" stroke="url(#inner-soft)" stroke-width="38" stroke-linecap="round" stroke-linejoin="round"/>'
            f'<path d="M625 380 L700 380 L735 470 L750 700 L730 805 L760 1040 L705 1050 L668 840 L645 840 L625 1050 L570 1040 L592 805 L565 700 L580 470 Z" fill="url(#inner-soft)" stroke="{OUTLINE}" stroke-width="8"/>'
            f'<path d="M622 450 Q665 420 710 455 L720 720 Q670 760 605 720 L600 475 Z" fill="{INNER_LIGHT}" opacity="0.45"/>'
            f'<path d="M535 630 Q552 641 574 642 M520 664 Q548 676 579 671 M785 644 Q807 659 835 657 M778 680 Q810 694 847 686" fill="none" stroke="{BRASS_DARK}" stroke-width="5" opacity="0.75"/>'
            f'<path d="M590 610 Q665 655 742 610 M605 800 Q665 825 735 800" fill="none" stroke="{BRASS_DARK}" stroke-width="7" opacity="0.72"/>',
        ),
        _group(
            "head-shell",
            _ceramic_path("M615 255 Q700 200 780 255 Q830 292 830 345 Q830 405 775 440 L635 435 Q590 395 590 330 Q590 280 615 255 Z")
            +
            f'<path d="M650 250 Q690 225 720 230" fill="none" stroke="{CERAMIC_HIGHLIGHT}" stroke-width="8" stroke-linecap="round" opacity="0.7"/>'
            f'<path d="M705 235 Q735 285 730 335" fill="none" stroke="{BRASS_DARK}" stroke-width="5" opacity="0.55"/>'
            f'<path d="M604 350 L650 425" fill="none" stroke="{BRASS_DARK}" stroke-width="12"/>'
            f'<circle cx="650" cy="360" r="30" fill="{BRASS}" stroke="{OUTLINE}" stroke-width="7"/>'
            f'<circle cx="650" cy="360" r="10" fill="{BRASS_DARK}"/>'
            f'<rect x="752" y="325" width="75" height="20" rx="10" fill="{OUTLINE}"/>'
            f'<rect x="762" y="330" width="54" height="10" rx="5" fill="{ENERGY}"/>',
        ),
        _group(
            "torso-shell",
            _ceramic_path("M615 410 L700 410 Q752 470 760 575 L735 750 L595 750 L580 565 Q585 470 615 410 Z")
            +
            f'<path d="M602 520 Q670 485 738 525" fill="none" stroke="{BRASS}" stroke-width="7" opacity="0.8"/>'
            f'<path d="M610 690 Q665 720 725 690" fill="none" stroke="{BRASS_DARK}" stroke-width="5" opacity="0.55"/>',
            form="clean-ceramic-torso-a2",
        ),
        _group(
            "pelvis-shell",
            _ceramic_path("M595 735 L735 735 L750 800 L720 855 L620 855 L590 800 Z")
            +
            f'<path d="M610 765 L730 765" stroke="{BRASS}" stroke-width="7"/>',
        ),
        _group(
            "left-upper-arm",
            _ceramic_path("M574 480 Q535 500 520 560 L535 650 L590 650 L610 520 Z", far=True)
            + f'<path data-effect="joint-contact-shadow" d="M548 497 Q575 485 596 512" fill="none" stroke="{OUTLINE}" stroke-width="12" stroke-linecap="round" opacity="0.3"/>',
            depth_role="far-occluded-a2",
        ),
        _group(
            "left-forearm",
            _ceramic_path("M535 630 L585 640 L575 770 L520 780 L500 690 Z", far=True)
            +
            f'<path d="M514 692 L580 690" stroke="{BRASS_DARK}" stroke-width="7"/>'
            f'<path data-effect="joint-contact-shadow" d="M530 642 Q556 634 582 648" fill="none" stroke="{OUTLINE}" stroke-width="11" stroke-linecap="round" opacity="0.32"/>',
            depth_role="far-occluded-a2",
        ),
        _group(
            "left-hand",
            f'<path d="M548 754 C529 756 517 770 518 792 C519 816 536 833 559 835 C577 837 593 829 603 814 L600 786 C597 769 584 759 568 758 Z" fill="url(#inner-soft)" stroke="{OUTLINE}" stroke-width="8" stroke-linejoin="round"/>'
            f'<path d="M531 779 Q555 766 584 775 M529 793 Q557 782 593 792 M534 807 Q559 798 590 807" fill="none" stroke="{BRASS_DARK}" stroke-width="4" stroke-linecap="round"/>'
            f'<path d="M548 756 L579 759" stroke="{BRASS}" stroke-width="6" stroke-linecap="round"/>',
            form="articulated-glove-v2",
        ),
        _group(
            "right-upper-arm",
            _ceramic_path("M735 490 Q805 505 825 570 L810 660 L755 670 L730 550 Z")
            + f'<path data-effect="joint-contact-shadow" d="M748 507 Q780 494 805 520" fill="none" stroke="{OUTLINE}" stroke-width="12" stroke-linecap="round" opacity="0.3"/>',
            depth_role="near-a2",
        ),
        _group(
            "right-forearm",
            _ceramic_path("M790 640 L840 650 L865 740 L845 795 L790 785 L770 690 Z")
            +
            f'<path d="M780 692 L850 690" stroke="{BRASS}" stroke-width="7"/>'
            f'<path data-effect="joint-contact-shadow" d="M790 650 Q817 642 842 657" fill="none" stroke="{OUTLINE}" stroke-width="11" stroke-linecap="round" opacity="0.32"/>',
            depth_role="near-a2",
        ),
        _group(
            "right-hand",
            f'<path d="M827 763 C811 770 805 786 808 804 C812 826 831 838 852 837 C872 836 887 824 890 807 L883 782 C876 768 863 762 848 763 Z" fill="url(#inner-soft)" stroke="{OUTLINE}" stroke-width="8" stroke-linejoin="round"/>'
            f'<path d="M820 788 Q846 774 874 786 M818 802 Q849 790 881 801 M824 816 Q851 807 877 814" fill="none" stroke="{BRASS_DARK}" stroke-width="4" stroke-linecap="round"/>'
            f'<path d="M828 766 L859 763" stroke="{BRASS}" stroke-width="6" stroke-linecap="round"/>',
            form="articulated-glove-v2",
        ),
        _group(
            "left-thigh",
            _ceramic_path("M620 825 C650 825 655 860 654 885 C653 925 640 945 620 945 C600 945 586 925 586 885 C586 850 600 825 620 825 Z", far=True)
            + f'<path data-effect="joint-contact-shadow" d="M600 836 Q620 824 642 838" fill="none" stroke="{OUTLINE}" stroke-width="11" stroke-linecap="round" opacity="0.32"/>',
            depth_role="far-occluded-a2",
        ),
        _group(
            "left-shin",
            _ceramic_path("M595 925 L650 925 L645 1045 L585 1045 Z", far=True)
            + f'<path data-effect="joint-contact-shadow" d="M596 936 Q622 925 648 936" fill="none" stroke="{OUTLINE}" stroke-width="10" stroke-linecap="round" opacity="0.3"/>',
            depth_role="far-occluded-a2",
        ),
        _group(
            "left-boot",
            _ceramic_path("M580 1025 L640 1025 L670 1059 L660 1077 L555 1077 L555 1050 Z", far=True)
            +
            f'<path d="M557 1065 L663 1065" stroke="{BRASS_DARK}" stroke-width="8"/>',
            depth_role="far-occluded-a2",
        ),
        _group(
            "right-thigh",
            _ceramic_path("M720 825 C750 825 755 860 754 885 C753 925 740 945 720 945 C700 945 686 925 686 885 C686 850 700 825 720 825 Z")
            + f'<path data-effect="joint-contact-shadow" d="M700 836 Q720 824 742 838" fill="none" stroke="{OUTLINE}" stroke-width="11" stroke-linecap="round" opacity="0.32"/>',
            depth_role="near-a2",
        ),
        _group(
            "right-shin",
            _ceramic_path("M695 925 L750 925 L755 1045 L685 1045 Z")
            + f'<path data-effect="joint-contact-shadow" d="M696 936 Q722 925 748 936" fill="none" stroke="{OUTLINE}" stroke-width="10" stroke-linecap="round" opacity="0.3"/>',
            depth_role="near-a2",
        ),
        _group(
            "right-boot",
            _ceramic_path("M700 1025 L755 1025 L795 1057 L785 1077 L675 1077 L675 1050 Z")
            +
            f'<path d="M678 1065 L788 1065" stroke="{BRASS}" stroke-width="8"/>',
            depth_role="near-a2",
        ),
    ]
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" viewBox="0 0 1402 1122">'
        '<defs>'
        '<linearGradient id="ceramic-main" gradientUnits="userSpaceOnUse" x1="500" y1="220" x2="900" y2="1080">'
        f'<stop offset="0" stop-color="{CERAMIC_HIGHLIGHT}"/><stop offset="0.42" stop-color="{CERAMIC}"/><stop offset="1" stop-color="#A69E89"/>'
        '</linearGradient>'
        '<linearGradient id="inner-soft" x1="0" y1="0" x2="1" y2="1">'
        f'<stop offset="0" stop-color="{INNER_LIGHT}"/><stop offset="0.55" stop-color="{INNER}"/><stop offset="1" stop-color="#171E25"/>'
        '</linearGradient>'
        '</defs>'
        + "".join(groups)
        + "</svg>"
    )


def _compose_layers(paths: dict[str, Path]) -> Image.Image:
    result = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    for part_id in BODY_STACK:
        with Image.open(paths[part_id]) as layer:
            result.alpha_composite(layer.convert("RGBA"))
    return result


def _review_canvas() -> Image.Image:
    image = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((22, 22, 1379, 1099), radius=28, fill=(11, 16, 24, 255))
    draw.rounded_rectangle((46, 46, 1355, 1075), radius=20, fill=(17, 26, 38, 255), outline=(52, 70, 90, 255), width=3)
    return image


def _body_review(composite: Image.Image) -> Image.Image:
    review = _review_canvas()
    review.alpha_composite(composite)
    draw = ImageDraw.Draw(review)
    draw.line((80, 1080, 1320, 1080), fill=(185, 138, 55, 255), width=2)
    for _joint_id, _first, _second, point in JOINTS:
        x, y = point
        draw.ellipse((x - 8, y - 8, x + 8, y + 8), fill=(206, 116, 255, 255), outline=(16, 24, 32, 255), width=3)
    return review


def _parts_review(paths: dict[str, Path]) -> Image.Image:
    review = _review_canvas()
    draw = ImageDraw.Draw(review)
    label_font = ImageFont.load_default(size=17)
    index_font = ImageFont.load_default(size=15)
    for index, part_id in enumerate(BODY_PART_IDS):
        column = index % 4
        row = index // 4
        cell = (85 + column * 322, 95 + row * 240, 375 + column * 322, 305 + row * 240)
        draw.rounded_rectangle(cell, radius=16, fill=(22, 34, 49, 255), outline=(52, 70, 90, 255), width=2)
        with Image.open(paths[part_id]) as source:
            layer = source.convert("RGBA")
            bbox = layer.getchannel("A").getbbox()
            crop = layer.crop(bbox)
            crop.thumbnail((180, 145), Image.Resampling.LANCZOS)
            x = cell[0] + (cell[2] - cell[0] - crop.width) // 2
            y = cell[1] + 12
            review.alpha_composite(crop, (x, y))
        bar_color = (81, 230, 205, 255) if part_id == "inner-body" or part_id.endswith("hand") else (231, 222, 199, 255)
        draw.text((cell[0] + 18, cell[1] + 14), f"{index + 1:02}", font=index_font, fill=bar_color)
        draw.text((cell[0] + 18, cell[3] - 48), part_id.upper(), font=label_font, fill=(220, 231, 234, 255))
        draw.rectangle((cell[0] + 18, cell[3] - 17, cell[2] - 18, cell[3] - 11), fill=bar_color)
    return review


def _small_scale_review(composite: Image.Image) -> Image.Image:
    review = _review_canvas()
    bbox = composite.getchannel("A").getbbox()
    crop = composite.crop(bbox)
    original_height = crop.height
    small_width = max(1, round(crop.width * 96 / original_height))
    small = crop.resize((small_width, 96), Image.Resampling.LANCZOS)
    zoom = small.resize((small.width * 5, small.height * 5), Image.Resampling.NEAREST)
    review.alpha_composite(small, (310 - small.width // 2, 600 - small.height))
    review.alpha_composite(zoom, (700 - zoom.width // 2, 300))
    draw = ImageDraw.Draw(review)
    draw.rectangle((220, 620, 400, 626), fill=(81, 230, 205, 255))
    draw.rectangle((450, 820, 950, 826), fill=(185, 138, 55, 255))
    return review


def render_body_pack(root: Path) -> dict[str, object]:
    external_root = root.resolve()
    request = MasterRequest.load(external_root / "request" / "master-request.json")
    authoring = external_root / "authoring"
    layers = external_root / "layers"
    review = external_root / "review"
    evidence = external_root / "evidence"
    for directory in (authoring, layers, review, evidence):
        directory.mkdir(parents=True, exist_ok=True)
    edge = discover_edge()
    if edge is None:
        raise RuntimeError("Microsoft Edge was not found")
    source = authoring / "body.svg"
    source.write_text(build_body_svg(request), encoding="utf-8")
    outputs = render_part_layers(edge, source, layers, BODY_PART_IDS)
    composite = _compose_layers(outputs)
    review_outputs = {
        "body-parts": _parts_review(outputs),
        "body-uncovered": _body_review(composite),
        "body-uncovered-96px": _small_scale_review(composite),
    }
    for name, image in review_outputs.items():
        image.save(review / f"{name}.png")
        image.close()
    composite.close()

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
        for name in BODY_REVIEW_NAMES
    )
    connections = {}
    for joint_id, first, second, point in JOINTS:
        connections[f"{joint_id}.a"] = {"partId": first, "point": list(point)}
        connections[f"{joint_id}.b"] = {"partId": second, "point": list(point)}
    manifest = {
        "version": 1,
        "stage": "body",
        "generatedAtNs": time.time_ns(),
        "files": files,
        "connections": connections,
    }
    (evidence / "body-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return {"stage": "body", "files": len(files), "source": str(source)}


def main() -> int:
    parser = argparse.ArgumentParser(description="Build complete-player body layers and review evidence")
    parser.add_argument("--external-root", type=Path, required=True)
    args = parser.parse_args()
    report = render_body_pack(args.external_root)
    print(f"COMPLETE_MASTER_BODY_OK files={report['files']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
