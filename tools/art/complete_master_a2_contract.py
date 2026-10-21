from __future__ import annotations

import json
from pathlib import Path


A2_GATE_ORDER = ("a2Structure", "a2DressedMaster", "a2Readiness")

FAR_BODY_PART_IDS = (
    "left-thigh",
    "left-shin",
    "left-boot",
    "left-upper-arm",
    "left-forearm",
    "left-hand",
)

NEAR_BODY_PART_IDS = (
    "right-thigh",
    "right-shin",
    "right-boot",
    "right-upper-arm",
    "right-forearm",
    "right-hand",
)

A2_DRESSED_STACK = (
    "inner-body",
    *FAR_BODY_PART_IDS,
    "mantle",
    "hidden-axle",
    "torso-shell",
    "pelvis-shell",
    "head-shell",
    *NEAR_BODY_PART_IDS,
    "shoulder-collar",
    "collar-fastener",
    "front-collar-flap",
    "back-core-housing",
    "back-energy-cell",
    "back-core-glow",
    "front-spool-housing",
    "front-spool-disc",
    "front-spool-glow",
    "half-bell-cleaver",
    "left-grip-overlay",
    "right-grip-overlay",
    "tether-back-connector",
    "tether-energy-middle",
    "tether-lantern-connector",
    "lantern-housing",
    "lantern-glow",
)


def ensure_a2_review_state(root: Path) -> Path:
    path = root.resolve() / "evidence" / "a2-review-state.json"
    if path.is_file():
        return path
    path.parent.mkdir(parents=True, exist_ok=True)
    state = {
        gate: {"status": "not-started", "explicitUserApproval": None}
        for gate in A2_GATE_ORDER
    }
    path.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")
    return path
