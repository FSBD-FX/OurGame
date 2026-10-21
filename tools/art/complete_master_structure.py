from __future__ import annotations

import argparse
import json
import tempfile
import time
from pathlib import Path
from typing import Callable

from complete_master_contract import PART_IDS, REVIEW_STAGES, MasterRequest, sha256_file
from complete_master_svg import discover_edge, render_svg


BODY_SUBJECT_IDS = PART_IDS[:16]
BACK_INTERFACE_IDS = (
    "front-spool-axle",
    "back-core-mount",
    "shoulder-collar",
    "mantle-root",
    "tether-back-port",
)

JOINTS = (
    ("neck", "head-shell", "torso-shell", (650, 425)),
    ("left-shoulder", "torso-shell", "left-upper-arm", (590, 520)),
    ("left-elbow", "left-upper-arm", "left-forearm", (550, 645)),
    ("left-wrist", "left-forearm", "left-hand", (555, 765)),
    ("right-shoulder", "torso-shell", "right-upper-arm", (750, 530)),
    ("right-elbow", "right-upper-arm", "right-forearm", (800, 655)),
    ("right-wrist", "right-forearm", "right-hand", (830, 780)),
    ("left-hip", "pelvis-shell", "left-thigh", (620, 840)),
    ("right-hip", "pelvis-shell", "right-thigh", (720, 840)),
    ("left-knee", "left-thigh", "left-shin", (620, 935)),
    ("right-knee", "right-thigh", "right-shin", (720, 935)),
    ("left-ankle", "left-shin", "left-boot", (620, 1035)),
    ("right-ankle", "right-shin", "right-boot", (720, 1035)),
)

COLORS = {
    "background": "#0B1018",
    "panel": "#111A26",
    "panel_2": "#162231",
    "line": "#34465A",
    "label": "#DCE7EA",
    "muted": "#8AA0AA",
    "inner": "#303944",
    "inner_2": "#465260",
    "ceramic": "#E8DFC7",
    "ceramic_far": "#BDB69F",
    "brass": "#C79B43",
    "brass_dark": "#775823",
    "mantle": "#267167",
    "mantle_dark": "#164A45",
    "weapon": "#D2773C",
    "weapon_dark": "#763E28",
    "energy": "#57E8D0",
    "joint": "#CE74FF",
    "outline": "#081016",
    "danger": "#FF6978",
}

GLYPHS = {
    "A": ("01110", "10001", "10001", "11111", "10001", "10001", "10001"),
    "B": ("11110", "10001", "10001", "11110", "10001", "10001", "11110"),
    "C": ("01111", "10000", "10000", "10000", "10000", "10000", "01111"),
    "D": ("11110", "10001", "10001", "10001", "10001", "10001", "11110"),
    "E": ("11111", "10000", "10000", "11110", "10000", "10000", "11111"),
    "F": ("11111", "10000", "10000", "11110", "10000", "10000", "10000"),
    "G": ("01111", "10000", "10000", "10111", "10001", "10001", "01111"),
    "H": ("10001", "10001", "10001", "11111", "10001", "10001", "10001"),
    "I": ("11111", "00100", "00100", "00100", "00100", "00100", "11111"),
    "J": ("00111", "00010", "00010", "00010", "10010", "10010", "01100"),
    "K": ("10001", "10010", "10100", "11000", "10100", "10010", "10001"),
    "L": ("10000", "10000", "10000", "10000", "10000", "10000", "11111"),
    "M": ("10001", "11011", "10101", "10101", "10001", "10001", "10001"),
    "N": ("10001", "11001", "10101", "10011", "10001", "10001", "10001"),
    "O": ("01110", "10001", "10001", "10001", "10001", "10001", "01110"),
    "P": ("11110", "10001", "10001", "11110", "10000", "10000", "10000"),
    "Q": ("01110", "10001", "10001", "10001", "10101", "10010", "01101"),
    "R": ("11110", "10001", "10001", "11110", "10100", "10010", "10001"),
    "S": ("01111", "10000", "10000", "01110", "00001", "00001", "11110"),
    "T": ("11111", "00100", "00100", "00100", "00100", "00100", "00100"),
    "U": ("10001", "10001", "10001", "10001", "10001", "10001", "01110"),
    "V": ("10001", "10001", "10001", "10001", "10001", "01010", "00100"),
    "W": ("10001", "10001", "10001", "10101", "10101", "10101", "01010"),
    "X": ("10001", "10001", "01010", "00100", "01010", "10001", "10001"),
    "Y": ("10001", "10001", "01010", "00100", "00100", "00100", "00100"),
    "Z": ("11111", "00001", "00010", "00100", "01000", "10000", "11111"),
    "0": ("01110", "10001", "10011", "10101", "11001", "10001", "01110"),
    "1": ("00100", "01100", "00100", "00100", "00100", "00100", "01110"),
    "2": ("01110", "10001", "00001", "00010", "00100", "01000", "11111"),
    "3": ("11110", "00001", "00001", "01110", "00001", "00001", "11110"),
    "4": ("00010", "00110", "01010", "10010", "11111", "00010", "00010"),
    "5": ("11111", "10000", "10000", "11110", "00001", "00001", "11110"),
    "6": ("01110", "10000", "10000", "11110", "10001", "10001", "01110"),
    "7": ("11111", "00001", "00010", "00100", "01000", "01000", "01000"),
    "8": ("01110", "10001", "10001", "01110", "10001", "10001", "01110"),
    "9": ("01110", "10001", "10001", "01111", "00001", "00001", "01110"),
    "-": ("00000", "00000", "00000", "11111", "00000", "00000", "00000"),
    ".": ("00000", "00000", "00000", "00000", "00000", "01100", "01100"),
    " ": ("00000",) * 7,
}


def _fmt(value: float) -> str:
    return str(int(value)) if float(value).is_integer() else f"{value:.2f}".rstrip("0").rstrip(".")


def _label(text: str, x: float, y: float, scale: float = 2, color: str | None = None) -> str:
    fill = color or COLORS["label"]
    cells: list[str] = []
    cursor = x
    for character in text.upper():
        pattern = GLYPHS.get(character, GLYPHS[" "])
        for row, bits in enumerate(pattern):
            for column, bit in enumerate(bits):
                if bit == "1":
                    cells.append(
                        f'<rect x="{_fmt(cursor + column * scale)}" y="{_fmt(y + row * scale)}" '
                        f'width="{_fmt(scale)}" height="{_fmt(scale)}" fill="{fill}"/>'
                    )
        cursor += 6 * scale
    return "".join(cells)


def _panel(title: str, subtitle: str) -> str:
    return "".join(
        (
            f'<rect x="22" y="22" width="1358" height="1078" rx="28" fill="{COLORS["background"]}"/>',
            f'<rect x="46" y="46" width="1310" height="1030" rx="20" fill="{COLORS["panel"]}" stroke="{COLORS["line"]}" stroke-width="3"/>',
            _label(title, 78, 74, 4),
            _label(subtitle, 80, 116, 2, COLORS["muted"]),
            f'<line x1="80" y1="150" x2="1320" y2="150" stroke="{COLORS["line"]}" stroke-width="2"/>',
            f'<line x1="80" y1="1080" x2="1320" y2="1080" stroke="{COLORS["brass"]}" stroke-width="2" stroke-dasharray="12 10"/>',
            _label("BASELINE-1080", 1120, 1052, 1.5, COLORS["brass"]),
        )
    )


def _subject_group(prefix: str, part_id: str, bbox: tuple[int, int, int, int], shape: str) -> str:
    box = " ".join(str(item) for item in bbox)
    return (
        f'<g id="{prefix}-{part_id}" data-subject-id="{part_id}" '
        f'data-geometry-key="body-v1-{part_id}" data-bbox="{box}">{shape}</g>'
    )


def _body_groups(prefix: str, opacity: float = 1.0) -> str:
    outline = COLORS["outline"]
    far = COLORS["ceramic_far"]
    ceramic = COLORS["ceramic"]
    inner = COLORS["inner"]
    groups = [
        _subject_group(
            prefix,
            "inner-body",
            (560, 380, 210, 680),
            f'<path d="M625 380 L700 380 L735 470 L750 700 L730 805 L760 1040 L705 1050 L668 840 L645 840 L625 1050 L570 1040 L592 805 L565 700 L580 470 Z" fill="{inner}" stroke="{outline}" stroke-width="8"/>',
        ),
        _subject_group(prefix, "left-upper-arm", (520, 480, 90, 180), f'<path d="M574 480 Q535 500 520 560 L535 650 L590 650 L610 520 Z" fill="{far}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "left-forearm", (500, 630, 90, 150), f'<path d="M535 630 L585 640 L575 770 L520 780 L500 690 Z" fill="{far}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "left-hand", (520, 750, 85, 85), f'<ellipse cx="560" cy="790" rx="40" ry="38" fill="{COLORS["inner_2"]}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "left-thigh", (585, 825, 70, 120), f'<ellipse cx="620" cy="880" rx="34" ry="58" fill="{far}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "left-shin", (585, 925, 70, 120), f'<path d="M595 925 L650 925 L645 1045 L585 1045 Z" fill="{far}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "left-boot", (555, 1025, 115, 55), f'<path d="M580 1025 L640 1025 L670 1062 L660 1080 L555 1080 L555 1050 Z" fill="{far}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "right-thigh", (685, 825, 70, 120), f'<ellipse cx="720" cy="880" rx="34" ry="58" fill="{ceramic}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "right-shin", (685, 925, 70, 120), f'<path d="M695 925 L750 925 L755 1045 L685 1045 Z" fill="{ceramic}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "right-boot", (675, 1025, 120, 55), f'<path d="M700 1025 L755 1025 L795 1060 L785 1080 L675 1080 L675 1050 Z" fill="{ceramic}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "torso-shell", (580, 410, 180, 340), f'<path d="M615 410 L700 410 Q752 470 760 575 L735 750 L595 750 L580 565 Q585 470 615 410 Z" fill="{ceramic}" stroke="{outline}" stroke-width="8"/>'),
        _subject_group(prefix, "pelvis-shell", (590, 735, 160, 120), f'<path d="M595 735 L735 735 L750 800 L720 855 L620 855 L590 800 Z" fill="{ceramic}" stroke="{outline}" stroke-width="8"/>'),
        _subject_group(prefix, "head-shell", (590, 230, 240, 210), f'<path d="M615 255 Q700 200 780 255 Q830 292 830 345 Q830 405 775 440 L635 435 Q590 395 590 330 Q590 280 615 255 Z" fill="{ceramic}" stroke="{outline}" stroke-width="9"/><rect x="755" y="326" width="70" height="18" rx="9" fill="{COLORS["energy"]}" stroke="{outline}" stroke-width="6"/>'),
        _subject_group(prefix, "right-upper-arm", (730, 490, 95, 180), f'<path d="M735 490 Q805 505 825 570 L810 660 L755 670 L730 550 Z" fill="{ceramic}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "right-forearm", (770, 640, 95, 155), f'<path d="M790 640 L840 650 L865 740 L845 795 L790 785 L770 690 Z" fill="{ceramic}" stroke="{outline}" stroke-width="7"/>'),
        _subject_group(prefix, "right-hand", (805, 760, 85, 80), f'<ellipse cx="848" cy="800" rx="40" ry="36" fill="{COLORS["inner_2"]}" stroke="{outline}" stroke-width="7"/>'),
    ]
    return f'<g id="{prefix}-body" opacity="{opacity}">' + "".join(groups) + "</g>"


def _joint_markers() -> str:
    items: list[str] = []
    left_y = 210
    right_y = 210
    for index, (joint_id, _first, _second, (x, y)) in enumerate(JOINTS):
        on_left = index in (1, 2, 3, 7, 9, 11)
        label_x = 92 if on_left else 1000
        label_y = left_y if on_left else right_y
        if on_left:
            left_y += 54
            line_x = 320
        else:
            right_y += 54
            line_x = 980
        items.append(f'<circle cx="{x}" cy="{y}" r="10" fill="{COLORS["joint"]}" stroke="{COLORS["outline"]}" stroke-width="4"/>')
        items.append(f'<line x1="{x}" y1="{y}" x2="{line_x}" y2="{label_y + 8}" stroke="{COLORS["joint"]}" stroke-width="2" stroke-dasharray="6 5"/>')
        items.append(_label(joint_id, label_x, label_y, 1.5, COLORS["joint"]))
    return '<g id="joint-labels">' + "".join(items) + "</g>"


def _uncovered_group(visible: bool = True) -> str:
    display = "inline" if visible else "none"
    return (
        f'<g id="structure-uncovered" style="display:{display}">'
        + _panel("STRUCTURE-01 UNCOVERED", "COMPLETE BODY SUBJECTS AND JOINT OVERLAP")
        + _body_groups("uncovered")
        + _joint_markers()
        + _label("RIGHT-FACING", 1080, 970, 2, COLORS["energy"])
        + '<path d="M1190 1020 L1290 1020 L1260 990 M1290 1020 L1260 1050" fill="none" stroke="#57E8D0" stroke-width="6"/>'
        + "</g>"
    )


def _equipment_shapes() -> str:
    outline = COLORS["outline"]
    return "".join(
        (
            f'<path data-part-id="mantle" d="M520 455 Q420 500 300 690 L245 930 L360 855 L410 1010 L545 865 L610 555 Z" fill="{COLORS["mantle"]}" stroke="{outline}" stroke-width="10"/>',
            f'<path data-part-id="shoulder-collar" d="M535 455 Q650 400 790 485 L770 535 Q650 475 555 525 Z" fill="{COLORS["brass_dark"]}" stroke="{outline}" stroke-width="8"/>',
            f'<rect data-part-id="back-core-housing" x="455" y="410" width="135" height="205" rx="55" fill="{COLORS["brass"]}" stroke="{outline}" stroke-width="10"/>',
            f'<rect data-part-id="back-energy-cell" x="485" y="445" width="75" height="130" rx="28" fill="{COLORS["energy"]}" stroke="{outline}" stroke-width="7"/>',
            f'<circle data-part-id="front-spool-housing" cx="688" cy="610" r="116" fill="{COLORS["brass"]}" stroke="{outline}" stroke-width="12"/>',
            f'<circle data-part-id="front-spool-disc" cx="688" cy="610" r="86" fill="{COLORS["inner"]}" stroke="{outline}" stroke-width="8"/>',
            f'<path d="M630 610 A58 58 0 1 1 746 610 A58 58 0 1 1 630 610 M648 610 A40 40 0 1 0 728 610 A40 40 0 1 0 648 610" fill="none" stroke="{COLORS["brass"]}" stroke-width="8"/>',
            f'<path data-part-id="half-bell-cleaver" d="M805 785 L995 785 L1050 730 L1240 730 Q1210 880 1080 930 Q970 945 905 865 L805 845 Z" fill="{COLORS["weapon"]}" stroke="{outline}" stroke-width="10"/>',
            f'<rect x="760" y="795" width="290" height="35" rx="16" fill="{COLORS["weapon_dark"]}" stroke="{outline}" stroke-width="7"/>',
            f'<path data-part-id="tether-energy-middle" d="M510 420 C500 330 620 320 570 220" fill="none" stroke="{COLORS["energy"]}" stroke-width="14" stroke-linecap="round"/>',
            f'<rect data-part-id="lantern-housing" x="500" y="135" width="145" height="115" rx="28" fill="{COLORS["brass"]}" stroke="{outline}" stroke-width="10"/>',
            f'<rect data-part-id="lantern-glow" x="535" y="160" width="75" height="65" rx="18" fill="{COLORS["energy"]}"/>',
        )
    )


def _dressed_group(visible: bool = False) -> str:
    display = "inline" if visible else "none"
    labels = "".join(
        (
            _label("ROUND-HEAD", 1020, 250, 2, COLORS["ceramic"]),
            '<line x1="980" y1="270" x2="820" y2="320" stroke="#E8DFC7" stroke-width="2"/>',
            _label("FRONT-SPOOL", 1040, 485, 2, COLORS["brass"]),
            '<line x1="1010" y1="505" x2="790" y2="570" stroke="#C79B43" stroke-width="2"/>',
            _label("WIDE-MANTLE", 85, 710, 2, COLORS["mantle"]),
            '<line x1="300" y1="730" x2="410" y2="700" stroke="#267167" stroke-width="2"/>',
            _label("HALF-BELL", 1090, 960, 2, COLORS["weapon"]),
            '<line x1="1080" y1="950" x2="1070" y2="900" stroke="#D2773C" stroke-width="2"/>',
            _label("FLOATING-LANTERN", 770, 175, 2, COLORS["energy"]),
            '<line x1="750" y1="195" x2="640" y2="190" stroke="#57E8D0" stroke-width="2"/>',
        )
    )
    return (
        f'<g id="structure-dressed" style="display:{display}">'
        + _panel("STRUCTURE-02 DRESSED", "FIVE SILHOUETTE ANCHORS OVER THE SAME BODY GEOMETRY")
        + _body_groups("dressed", opacity=0.78)
        + _equipment_shapes()
        + labels
        + "</g>"
    )


def _interface_item(
    interface_id: str,
    bbox: tuple[int, int, int, int],
    color: str,
    shape: str,
    label_x: int,
    label_y: int,
) -> str:
    box = " ".join(str(item) for item in bbox)
    return (
        f'<g id="interface-{interface_id}" data-interface-id="{interface_id}" data-bbox="{box}">'
        + shape
        + _label(interface_id, label_x, label_y, 1.5, color)
        + "</g>"
    )


def _back_detail_group(visible: bool = False) -> str:
    display = "inline" if visible else "none"
    outline = COLORS["outline"]
    interfaces = "".join(
        (
            _interface_item("front-spool-axle", (150, 230, 190, 180), COLORS["brass"], f'<circle cx="245" cy="315" r="72" fill="{COLORS["brass"]}" stroke="{outline}" stroke-width="10"/><circle cx="245" cy="315" r="28" fill="{COLORS["inner"]}"/>', 145, 430),
            _interface_item("back-core-mount", (430, 220, 190, 220), COLORS["energy"], f'<rect x="455" y="235" width="135" height="190" rx="50" fill="{COLORS["brass_dark"]}" stroke="{outline}" stroke-width="10"/><rect x="485" y="270" width="75" height="120" rx="25" fill="{COLORS["energy"]}"/>', 425, 455),
            _interface_item("shoulder-collar", (720, 235, 220, 150), COLORS["ceramic"], f'<path d="M735 350 Q820 230 925 340 L900 380 Q820 310 755 385 Z" fill="{COLORS["ceramic"]}" stroke="{outline}" stroke-width="10"/>', 720, 420),
            _interface_item("mantle-root", (185, 620, 260, 230), COLORS["mantle"], f'<path d="M225 650 Q335 585 420 690 L385 825 L250 850 L185 750 Z" fill="{COLORS["mantle"]}" stroke="{outline}" stroke-width="10"/>', 205, 875),
            _interface_item("tether-back-port", (850, 620, 220, 220), COLORS["joint"], f'<circle cx="960" cy="715" r="70" fill="{COLORS["brass"]}" stroke="{outline}" stroke-width="10"/><circle cx="960" cy="715" r="28" fill="{COLORS["energy"]}"/><path d="M960 645 C1040 570 1120 620 1160 550" fill="none" stroke="{COLORS["energy"]}" stroke-width="12"/>', 850, 870),
        )
    )
    connectors = "".join(
        (
            '<line x1="340" y1="315" x2="430" y2="315" stroke="#CE74FF" stroke-width="4" stroke-dasharray="10 8"/>',
            '<line x1="620" y1="320" x2="720" y2="320" stroke="#CE74FF" stroke-width="4" stroke-dasharray="10 8"/>',
            '<line x1="445" y1="735" x2="850" y2="735" stroke="#CE74FF" stroke-width="4" stroke-dasharray="10 8"/>',
            _label("HIDDEN-AXIS", 535, 700, 2, COLORS["joint"]),
        )
    )
    return (
        f'<g id="structure-back-detail" style="display:{display}">'
        + _panel("STRUCTURE-03 BACK-DETAIL", "FIVE SEPARATE INTERFACES WITH A HIDDEN SHARED AXIS")
        + '<path d="M610 535 Q700 475 790 535 L820 900 Q710 975 600 900 Z" fill="#303944" stroke="#081016" stroke-width="10"/>'
        + _label("TORSO REFERENCE", 610, 935, 2, COLORS["muted"])
        + interfaces
        + connectors
        + "</g>"
    )


def _part_color(part_id: str) -> str:
    if part_id in BODY_SUBJECT_IDS:
        return COLORS["inner"] if part_id == "inner-body" or part_id.endswith("hand") else COLORS["ceramic"]
    if "glow" in part_id or "energy" in part_id:
        return COLORS["energy"]
    if part_id.startswith("front-spool") or part_id.startswith("back-") or part_id in ("hidden-axle", "shoulder-collar", "collar-fastener"):
        return COLORS["brass"]
    if part_id in ("front-collar-flap", "mantle"):
        return COLORS["mantle"]
    if part_id == "half-bell-cleaver":
        return COLORS["weapon"]
    if part_id.endswith("grip-overlay"):
        return COLORS["joint"]
    return COLORS["energy"] if part_id.startswith("tether-") else COLORS["brass"]


def _part_map_group(visible: bool = False) -> str:
    display = "inline" if visible else "none"
    cells: list[str] = []
    for index, part_id in enumerate(PART_IDS):
        column = index % 7
        row = index // 7
        x = 70 + column * 183
        y = 180 + row * 168
        color = _part_color(part_id)
        lines = part_id.upper().split("-")
        label_y = y + 105
        labels = "".join(_label(line, x + 14, label_y + offset * 15, 1.25, COLORS["label"]) for offset, line in enumerate(lines[:3]))
        cells.append(
            f'<g id="map-{part_id}" data-part-id="{part_id}">'
            f'<rect x="{x}" y="{y}" width="165" height="150" rx="12" fill="{COLORS["panel_2"]}" stroke="{COLORS["line"]}" stroke-width="2"/>'
            f'<circle cx="{x + 82}" cy="{y + 61}" r="38" fill="{color}" stroke="{COLORS["outline"]}" stroke-width="6"/>'
            f'<circle cx="{x + 82}" cy="{y + 61}" r="12" fill="{COLORS["background"]}"/>'
            + _label(str(index + 1), x + 10, y + 10, 1.5, color)
            + labels
            + "</g>"
        )
    return (
        f'<g id="structure-all-parts" style="display:{display}">'
        + _panel("STRUCTURE-04 PART-MAP", "35 CANONICAL IDS. PURPLE ITEMS ARE OVERLAYS, NOT SUBJECTS")
        + "".join(cells)
        + "</g>"
    )


def _svg_document(content: str) -> str:
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" '
        'viewBox="0 0 1402 1122">'
        + content
        + "</svg>"
    )


def build_structure_svg(request: MasterRequest) -> str:
    if request.canvas != (1402, 1122) or request.baseline_y != 1080 or request.facing != "right":
        raise ValueError("structure request must use the canonical canvas, baseline, and facing")
    return _svg_document(
        _uncovered_group(True)
        + _dressed_group(False)
        + _back_detail_group(False)
        + _part_map_group(False)
    )


def _review_svg(view: str) -> str:
    builders: dict[str, Callable[[bool], str]] = {
        "structure-uncovered": _uncovered_group,
        "structure-dressed": _dressed_group,
        "structure-back-detail": _back_detail_group,
        "structure-all-parts": _part_map_group,
    }
    if view not in builders:
        raise ValueError(f"unknown structure review view: {view}")
    return _svg_document(builders[view](True))


def _ensure_review_state(root: Path, request: MasterRequest) -> None:
    path = root / "evidence" / "review-state.json"
    if path.exists():
        return
    review = {
        "version": 1,
        **{
            stage: dict(request.review[stage])
            for stage in REVIEW_STAGES
        },
    }
    path.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")


def render_structure_pack(root: Path) -> dict[str, object]:
    external_root = root.resolve()
    request_path = external_root / "request" / "master-request.json"
    request = MasterRequest.load(request_path)
    authoring = external_root / "authoring"
    review = external_root / "review"
    evidence = external_root / "evidence"
    for directory in (authoring, review, evidence):
        directory.mkdir(parents=True, exist_ok=True)
    edge = discover_edge()
    if edge is None:
        raise RuntimeError("Microsoft Edge was not found")

    source_path = authoring / "structure.svg"
    source_path.write_text(build_structure_svg(request), encoding="utf-8")
    names = (
        "structure-uncovered",
        "structure-dressed",
        "structure-back-detail",
        "structure-all-parts",
    )
    files: list[dict[str, str]] = []
    with tempfile.TemporaryDirectory(prefix="complete-master-structure-") as temporary:
        temporary_root = Path(temporary)
        for name in names:
            render_source = temporary_root / f"{name}.svg"
            render_source.write_text(_review_svg(name), encoding="utf-8")
            output = review / f"{name}.png"
            render_svg(edge, render_source, output)
            files.append(
                {
                    "path": output.relative_to(external_root).as_posix(),
                    "sha256": sha256_file(output),
                    "role": "review",
                }
            )

    manifest = {
        "version": 1,
        "stage": "structure",
        "generatedAtNs": time.time_ns(),
        "files": files,
        "connections": {},
    }
    (evidence / "structure-manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )
    _ensure_review_state(external_root, request)
    return {
        "stage": "structure",
        "files": len(files),
        "source": str(source_path),
        "review": [str(review / f"{name}.png") for name in names],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Build complete-player structure review evidence")
    parser.add_argument("--external-root", type=Path, required=True)
    args = parser.parse_args()
    report = render_structure_pack(args.external_root)
    print(f"COMPLETE_MASTER_STRUCTURE_OK files={report['files']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
