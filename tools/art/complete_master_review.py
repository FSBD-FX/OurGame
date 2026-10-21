from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from complete_master_a2_contract import (
    A2_DRESSED_STACK,
    A2_GATE_ORDER,
    ensure_a2_review_state,
)
from complete_master_body import BODY_PART_IDS, BODY_STACK
from complete_master_contract import OVERLAY_PART_IDS, PART_IDS, sha256_file
from complete_master_lantern_tether import TETHER_CONNECTIONS


CANVAS = (1402, 1122)
FINAL_REVIEW_NAMES = (
    "master-uncovered",
    "master-dressed",
    "master-back-detail",
    "master-all-parts",
    "master-dressed-96px",
)
FINAL_REVIEW_GATES = {
    "dressedMaster": [
        "master-uncovered",
        "master-dressed",
        "master-back-detail",
        "master-all-parts",
    ],
    "smallScale": ["master-dressed-96px"],
    "animationReady": ["master-back-detail"],
}

A2_STRUCTURE_REVIEW_NAMES = (
    "a2-structure-uncovered",
    "a2-structure-mounted",
    "a2-shoulder-detail",
    "a2-all-parts",
)
A2_DRESSED_REVIEW_NAMES = (
    "a2-dressed",
    "a2-dressed-back-detail",
    "a2-dressed-layers",
)
A2_REVIEW_GATES = {
    "a2Structure": list(A2_STRUCTURE_REVIEW_NAMES),
    "a2DressedMaster": list(A2_DRESSED_REVIEW_NAMES),
    "a2Readiness": ["a2-dressed-96px", "a2-animation-space"],
}

DRESSED_STACK = A2_DRESSED_STACK

A2_STRUCTURE_STACK = tuple(
    part_id
    for part_id in A2_DRESSED_STACK
    if part_id
    not in {
        "half-bell-cleaver",
        "left-grip-overlay",
        "right-grip-overlay",
        "tether-back-connector",
        "tether-energy-middle",
        "tether-lantern-connector",
        "lantern-housing",
        "lantern-glow",
    }
)


def build_stack(mode: str) -> tuple[str, ...]:
    if mode == "uncovered":
        return BODY_STACK
    if mode == "dressed":
        return DRESSED_STACK
    raise ValueError(f"unknown master composition mode: {mode}")


def _read(path: Path) -> Image.Image:
    with Image.open(path) as source:
        result = source.convert("RGBA")
        result.load()
    return result


def compose_master(root: Path, mode: str) -> Image.Image:
    layers = root.resolve() / "layers"
    stack = build_stack(mode)
    result = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    for part_id in stack:
        path = layers / f"{part_id}.png"
        if not path.is_file():
            raise ValueError(f"complete-master layer is missing: {part_id}")
        result.alpha_composite(_read(path))
    result.meta = {
        "mode": mode,
        "stack": list(stack),
        "bodyLayerSha256": {
            part_id: sha256_file(layers / f"{part_id}.png") for part_id in BODY_PART_IDS
        },
    }
    return result


def _compose_stack(root: Path, stack: tuple[str, ...], mode: str) -> Image.Image:
    layers = root.resolve() / "layers"
    result = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    for part_id in stack:
        result.alpha_composite(_read(layers / f"{part_id}.png"))
    result.meta = {"mode": mode, "stack": list(stack)}
    return result


def _blank_review() -> Image.Image:
    image = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((22, 22, 1379, 1099), radius=28, fill=(11, 16, 24, 255))
    draw.rounded_rectangle((46, 46, 1355, 1075), radius=20, fill=(17, 26, 38, 255), outline=(52, 70, 90, 255), width=3)
    return image


def _present_master(master: Image.Image) -> Image.Image:
    review = _blank_review()
    review.alpha_composite(master)
    ImageDraw.Draw(review).line((80, 1080, 1320, 1080), fill=(185, 138, 55, 255), width=2)
    return review


def build_contact_sheet(root: Path, part_ids: tuple[str, ...]) -> Image.Image:
    if tuple(part_ids) != PART_IDS:
        raise ValueError("contact sheet must use the canonical 35 ordered parts")
    review = _blank_review()
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=13)
    layers = root.resolve() / "layers"
    for index, part_id in enumerate(part_ids):
        column = index % 7
        row = index // 7
        cell = (62 + column * 188, 70 + row * 198, 230 + column * 188, 250 + row * 198)
        draw.rounded_rectangle(cell, radius=10, fill=(22, 34, 49, 255), outline=(52, 70, 90, 255), width=2)
        layer = _read(layers / f"{part_id}.png")
        crop = layer.crop(layer.getchannel("A").getbbox())
        crop.thumbnail((125, 115), Image.Resampling.LANCZOS)
        review.alpha_composite(crop, (cell[0] + (168 - crop.width) // 2, cell[1] + 12))
        draw.text((cell[0] + 8, cell[3] - 35), f"{index + 1:02} {part_id}", font=font, fill=(224, 235, 237, 255))
    review.meta = {"partLabels": list(part_ids)}
    return review


def build_small_scale_review(image: Image.Image, body_height: int = 96) -> Image.Image:
    if body_height <= 0:
        raise ValueError("body_height must be positive")
    source_box = image.getchannel("A").getbbox()
    if source_box is None:
        raise ValueError("dressed master is empty")
    subject = image.crop(source_box)
    width = max(1, round(subject.width * body_height / subject.height))
    small = subject.resize((width, body_height), Image.Resampling.LANCZOS)
    zoom = small.resize((small.width * 5, small.height * 5), Image.Resampling.NEAREST)
    review = _blank_review()
    review.alpha_composite(small, (270 - small.width // 2, 660 - small.height))
    review.alpha_composite(zoom, (855 - zoom.width // 2, 720 - zoom.height // 2))
    draw = ImageDraw.Draw(review)
    draw.rectangle((150, 680, 390, 686), fill=(81, 230, 205, 255))
    draw.rectangle((520, 990, 1190, 996), fill=(185, 138, 55, 255))
    review.meta = {
        "method": "single-composite-resize",
        "scaledSubjectSize": [small.width, small.height],
        "sourceAlphaBox": list(source_box),
    }
    return review


def _back_detail(root: Path, dressed: Image.Image) -> Image.Image:
    review = _present_master(dressed)
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=15)
    colors = ((202, 116, 255, 255), (81, 230, 205, 255), (228, 185, 91, 255))
    points = ((590, 520), (750, 530), (550, 645), (800, 655), (620, 840), (720, 840))
    for index, (x, y) in enumerate(points):
        color = colors[index % len(colors)]
        draw.ellipse((x - 9, y - 9, x + 9, y + 9), fill=color, outline=(16, 24, 32, 255), width=3)
    for connection_id, point in TETHER_CONNECTIONS.items():
        x, y = point
        draw.rectangle((x - 5, y - 5, x + 5, y + 5), fill=(81, 230, 205, 255))
        draw.text((x + 9, y - 7), connection_id, font=font, fill=(220, 235, 237, 255))
    draw.text((915, 145), "STACK: SUBJECTS -> WEAPON -> GRIP OVERLAYS -> LANTERN", font=font, fill=(228, 185, 91, 255))
    return review


def _a2_shoulder_detail(mounted: Image.Image) -> Image.Image:
    review = _present_master(mounted)
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=16)
    axis_y = 490
    draw.line((500, axis_y, 790, axis_y), fill=(228, 185, 91, 255), width=3)
    draw.ellipse((558, 480, 578, 500), fill=(81, 230, 205, 255), outline=(16, 24, 32, 255), width=3)
    draw.ellipse((720, 480, 740, 500), fill=(228, 185, 91, 255), outline=(16, 24, 32, 255), width=3)
    draw.text((430, 455), "SHOULDER-BACK CORE", font=font, fill=(81, 230, 205, 255))
    draw.text((650, 420), "SHOULDER AXIS", font=font, fill=(228, 185, 91, 255))
    draw.text((735, 535), "SHOULDER-FRONT SPOOL", font=font, fill=(228, 185, 91, 255))
    draw.text((260, 940), "MANTLE OCCLUDES FAR ARM / HIP / LEG", font=font, fill=(224, 235, 237, 255))
    review.meta = {
        "technicalAnnotations": [
            "shoulder-back-core",
            "shoulder-axis",
            "shoulder-front-spool",
            "mantle-far-body-occlusion",
        ]
    }
    return review


def _a2_dressed_layers_review(dressed: Image.Image) -> Image.Image:
    review = _present_master(dressed)
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=15)
    labels = (
        ((450, 240), "FLOATING LANTERN", (81, 230, 205, 255)),
        ((465, 390), "SHOULDER TETHER", (81, 230, 205, 255)),
        ((650, 455), "A2 SHOULDER RIG", (228, 185, 91, 255)),
        ((340, 730), "FAR-BODY MANTLE", (80, 168, 162, 255)),
        ((870, 900), "TWO-HAND WEAPON", (228, 185, 91, 255)),
    )
    for (x, y), label, color in labels:
        draw.ellipse((x - 7, y - 7, x + 7, y + 7), fill=color, outline=(16, 24, 32, 255), width=2)
        draw.text((x + 12, y - 8), label, font=font, fill=color)
    review.meta = {
        "layerRead": [
            "floating-lantern",
            "shoulder-tether",
            "a2-shoulder-rig",
            "far-body-mantle",
            "two-hand-weapon",
        ]
    }
    return review


def _a2_dressed_back_detail(dressed: Image.Image) -> Image.Image:
    review = _present_master(dressed)
    draw = ImageDraw.Draw(review)
    font = ImageFont.load_default(size=16)
    annotations = (
        (
            "shoulder-back-core",
            (568, 490),
            (900, 445, 1175, 487),
            "SHOULDER-BACK CORE",
            (228, 185, 91, 255),
        ),
        (
            "energy-tether",
            (478, 380),
            (145, 330, 365, 372),
            "ENERGY TETHER",
            (81, 230, 205, 255),
        ),
        (
            "floating-lantern",
            (450, 235),
            (145, 170, 365, 212),
            "FLOATING LANTERN",
            (81, 230, 205, 255),
        ),
    )
    for _name, point, box, label, color in annotations:
        anchor = (box[2], (box[1] + box[3]) // 2) if box[2] < point[0] else (box[0], (box[1] + box[3]) // 2)
        draw.line((point[0], point[1], anchor[0], anchor[1]), fill=color, width=3)
        draw.ellipse((point[0] - 7, point[1] - 7, point[0] + 7, point[1] + 7), fill=color, outline=(16, 24, 32, 255), width=2)
        draw.rounded_rectangle(box, radius=10, fill=(16, 24, 34, 230), outline=color, width=2)
        draw.text((box[0] + 12, box[1] + 11), label, font=font, fill=color)
    review.meta = {
        "technicalAnnotations": [name for name, *_rest in annotations],
        "labelBoxes": [list(box) for _name, _point, box, _label, _color in annotations],
    }
    return review


def render_a2_review_pack(root: Path, gate: str) -> dict[str, object]:
    if gate not in ("a2Structure", "a2DressedMaster"):
        raise ValueError(f"A2 review gate is not implemented yet: {gate}")
    external_root = root.resolve()
    layers, review, evidence = (
        external_root / name for name in ("layers", "review", "evidence")
    )
    for directory in (layers, review, evidence):
        directory.mkdir(parents=True, exist_ok=True)
    for part_id in PART_IDS:
        if not (layers / f"{part_id}.png").is_file():
            raise ValueError(f"complete-master layer is missing: {part_id}")
    ensure_a2_review_state(external_root)
    if gate == "a2Structure":
        uncovered = compose_master(external_root, "uncovered")
        mounted = _compose_stack(external_root, A2_STRUCTURE_STACK, "a2-structure")
        outputs = {
            "a2-structure-uncovered": _present_master(uncovered),
            "a2-structure-mounted": _present_master(mounted),
            "a2-shoulder-detail": _a2_shoulder_detail(mounted),
            "a2-all-parts": build_contact_sheet(external_root, PART_IDS),
        }
        included_review_names = A2_STRUCTURE_REVIEW_NAMES
        uncovered.close()
        mounted.close()
    else:
        state = json.loads(
            (evidence / "a2-review-state.json").read_text(encoding="utf-8")
        )
        if state.get("a2Structure", {}).get("status") != "accepted":
            raise ValueError("a2DressedMaster requires accepted a2Structure")
        for name in A2_STRUCTURE_REVIEW_NAMES:
            if not (review / f"{name}.png").is_file():
                raise ValueError(f"approved A2 structure review is missing: {name}")
        dressed = compose_master(external_root, "dressed")
        outputs = {
            "a2-dressed": _present_master(dressed),
            "a2-dressed-back-detail": _a2_dressed_back_detail(dressed),
            "a2-dressed-layers": _a2_dressed_layers_review(dressed),
        }
        included_review_names = A2_STRUCTURE_REVIEW_NAMES + A2_DRESSED_REVIEW_NAMES
        dressed.close()
    for name, image in outputs.items():
        image.save(review / f"{name}.png")
        image.close()
    files = []
    for part_id in PART_IDS:
        path = layers / f"{part_id}.png"
        files.append(
            {
                "path": path.relative_to(external_root).as_posix(),
                "sha256": sha256_file(path),
                "role": "overlay" if part_id in OVERLAY_PART_IDS else "part",
                "partId": part_id,
            }
        )
    files.extend(
        {
            "path": (review / f"{name}.png").relative_to(external_root).as_posix(),
            "sha256": sha256_file(review / f"{name}.png"),
            "role": "review",
        }
        for name in included_review_names
    )
    manifest = {
        "version": 1,
        "stage": "a2",
        "generatedAtNs": time.time_ns(),
        "files": files,
        "connections": {},
        "reviewStatePath": "evidence/a2-review-state.json",
        "reviewGateOrder": list(A2_GATE_ORDER),
        "reviewGates": {
            review_gate: [f"review/{name}.png" for name in names]
            for review_gate, names in A2_REVIEW_GATES.items()
        },
        "dressedStack": list(A2_DRESSED_STACK),
    }
    (evidence / "a2-manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )
    return {"stage": "a2", "files": len(files), "gate": gate}


def render_final_pack(root: Path) -> dict[str, object]:
    external_root = root.resolve()
    layers, review, evidence = (external_root / name for name in ("layers", "review", "evidence"))
    for directory in (layers, review, evidence):
        directory.mkdir(parents=True, exist_ok=True)
    for part_id in PART_IDS:
        if not (layers / f"{part_id}.png").is_file():
            raise ValueError(f"complete-master layer is missing: {part_id}")
    uncovered = compose_master(external_root, "uncovered")
    dressed = compose_master(external_root, "dressed")
    outputs = {
        "master-uncovered": _present_master(uncovered),
        "master-dressed": _present_master(dressed),
        "master-back-detail": _back_detail(external_root, dressed),
        "master-all-parts": build_contact_sheet(external_root, PART_IDS),
        "master-dressed-96px": build_small_scale_review(dressed),
    }
    for name, image in outputs.items():
        image.save(review / f"{name}.png")
        image.close()
    uncovered.close()
    dressed.close()
    files = []
    for part_id in PART_IDS:
        path = layers / f"{part_id}.png"
        files.append({
            "path": path.relative_to(external_root).as_posix(),
            "sha256": sha256_file(path),
            "role": "overlay" if part_id in OVERLAY_PART_IDS else "part",
            "partId": part_id,
        })
    files.extend({"path": (review / f"{name}.png").relative_to(external_root).as_posix(), "sha256": sha256_file(review / f"{name}.png"), "role": "review"} for name in FINAL_REVIEW_NAMES)
    manifest = {
        "version": 1,
        "stage": "final",
        "generatedAtNs": time.time_ns(),
        "files": files,
        "connections": {},
        "reviewGates": {
            gate: [f"review/{name}.png" for name in names]
            for gate, names in FINAL_REVIEW_GATES.items()
        },
        "bodyLayerSha256": {
            part_id: sha256_file(layers / f"{part_id}.png") for part_id in BODY_PART_IDS
        },
        "dressedStack": list(DRESSED_STACK),
    }
    (evidence / "final-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return {"stage": "final", "files": len(files)}


def main() -> int:
    parser = argparse.ArgumentParser(description="Assemble final complete layered-player static masters")
    parser.add_argument("--external-root", type=Path, required=True)
    parser.add_argument("--gate", choices=A2_GATE_ORDER)
    args = parser.parse_args()
    if args.gate:
        report = render_a2_review_pack(args.external_root, args.gate)
        print(f"COMPLETE_MASTER_A2_OK gate={args.gate} files={report['files']}")
    else:
        report = render_final_pack(args.external_root)
        print(f"COMPLETE_MASTER_FINAL_OK files={report['files']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
