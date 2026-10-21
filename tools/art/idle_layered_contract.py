from __future__ import annotations

import hashlib
import json
import math
import re
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any


BODY_SHA256 = "0358BFEA48D6FF79271A5B14AE03B0396361748E60EB2D9D36EF6A696D7B1B44"
LANTERN_SHA256 = "C313BFAAF1A1B8C9994AA6FEBC96D5A9E5C82F740B58305FF3C96696DCC73AB0"
HASH_PATTERN = re.compile(r"^[0-9A-F]{64}$")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


@dataclass(frozen=True)
class SourceSpec:
    path: Path
    sha256: str

    @classmethod
    def from_dict(cls, raw: dict[str, Any], field: str) -> "SourceSpec":
        if not isinstance(raw, dict):
            raise ValueError(f"{field} must be an object")
        hash_value = raw.get("sha256")
        if not isinstance(hash_value, str) or HASH_PATTERN.fullmatch(hash_value) is None:
            raise ValueError(f"{field}.sha256 must be 64 uppercase hexadecimal characters")
        source_path = raw.get("path")
        if not isinstance(source_path, str) or not source_path.strip():
            raise ValueError(f"{field}.path must be a non-empty string")
        return cls(Path(source_path), hash_value)


@dataclass(frozen=True)
class RigConfig:
    version: int
    frames: int
    fps: int
    canvas: tuple[int, int]
    origin: tuple[int, int]
    body_source: SourceSpec
    lantern_source: SourceSpec
    rigid_scale: tuple[float, float]

    @classmethod
    def from_dict(cls, raw: dict[str, Any]) -> "RigConfig":
        if raw.get("version") != 1:
            raise ValueError("version must be 1")
        if raw.get("frames") != 12:
            raise ValueError("frames must be 12")
        if raw.get("fps") != 8:
            raise ValueError("fps must be 8")
        if raw.get("canvas") != [256, 256]:
            raise ValueError("canvas must be [256, 256]")
        if raw.get("origin") != [128, 231]:
            raise ValueError("origin must be [128, 231]")
        rigid_scale = raw.get("rigidScale")
        if rigid_scale != [1.0, 1.0]:
            raise ValueError("rigidScale must be [1.0, 1.0]")

        return cls(
            version=1,
            frames=12,
            fps=8,
            canvas=(256, 256),
            origin=(128, 231),
            body_source=SourceSpec.from_dict(raw.get("bodySource"), "bodySource"),
            lantern_source=SourceSpec.from_dict(raw.get("lanternSource"), "lanternSource"),
            rigid_scale=(1.0, 1.0),
        )

    @classmethod
    def load(cls, path: Path) -> "RigConfig":
        config = cls.from_dict(json.loads(path.read_text(encoding="utf-8")))
        for field, source in (
            ("bodySource", config.body_source),
            ("lanternSource", config.lantern_source),
        ):
            source_path = source.path if source.path.is_absolute() else path.parent / source.path
            if not source_path.is_file():
                raise ValueError(f"{field}.path does not exist: {source_path}")
            actual = sha256_file(source_path)
            if actual != source.sha256:
                raise ValueError(f"{field}.sha256 mismatch: expected {source.sha256}, got {actual}")
        return config


@dataclass(frozen=True)
class FramePose:
    index: int
    chassis_y: float
    chassis_deg: float
    weapon_deg: float
    mantle_root: tuple[float, float]
    mantle_mid: tuple[float, float]
    mantle_tip: tuple[float, float]
    core_energy: float
    lantern_offset: tuple[float, float]
    lantern_deg: float


CHASSIS_Y = (0.0, -0.5, -1.0, -1.5, -2.0, -1.5, -1.0, -0.5, 0.0, 0.5, 1.0, 0.5)
CHASSIS_DEG = (0.0, 0.05, 0.10, 0.12, 0.10, 0.05, 0.0, -0.05, -0.10, -0.12, -0.10, -0.05)
WEAPON_DEG = (0.0, -0.08, -0.16, -0.20, -0.16, -0.08, 0.0, 0.08, 0.16, 0.20, 0.16, 0.08)
BREEZE = tuple(math.sin(math.tau * index / 12.0) for index in range(12))
PULSE = (0.0, 0.0, 0.0, 0.0, 1.0, 0.55, 0.15, 0.0, 0.0, 0.0, 0.0, 0.0)
CORE = (0.18, 0.16, 0.15, 0.18, 0.35, 0.75, 1.0, 0.62, 0.32, 0.24, 0.22, 0.20)


def pulse_at(frame: int, delay: int) -> float:
    source = frame - delay
    return PULSE[source] if 0 <= source < len(PULSE) else 0.0


def cloth_offset(
    frame: int,
    delay: int,
    breeze_x: float,
    breeze_y: float,
    pulse_y: float,
) -> tuple[float, float]:
    breeze = BREEZE[(frame - delay) % len(BREEZE)]
    return (
        breeze_x * breeze,
        breeze_y * breeze - pulse_y * pulse_at(frame, delay),
    )


def build_default_timeline() -> tuple[FramePose, ...]:
    poses: list[FramePose] = []
    for index in range(12):
        lantern_breeze = BREEZE[(index - 2) % 12]
        lantern_pulse = pulse_at(index, 3)
        poses.append(
            FramePose(
                index=index,
                chassis_y=CHASSIS_Y[index],
                chassis_deg=CHASSIS_DEG[index],
                weapon_deg=WEAPON_DEG[index],
                mantle_root=cloth_offset(index, 0, 1.0, -1.0, 3.0),
                mantle_mid=cloth_offset(index, 1, 1.6, -1.3, 4.0),
                mantle_tip=cloth_offset(index, 2, 2.4, -1.8, 5.0),
                core_energy=CORE[index],
                lantern_offset=(
                    -2.0 * lantern_breeze + 2.0 * lantern_pulse,
                    lantern_breeze - 6.0 * lantern_pulse,
                ),
                lantern_deg=0.6 * BREEZE[(index - 3) % 12] + 0.8 * lantern_pulse,
            )
        )
    return tuple(poses)


def write_default_timeline(path: Path) -> None:
    poses = []
    for pose in build_default_timeline():
        raw = asdict(pose)
        raw["rigidScale"] = [1.0, 1.0]
        poses.append(raw)
    payload = {
        "version": 1,
        "frames": 12,
        "fps": 8,
        "gifDurationsMs": [120, 130] * 6,
        "poses": poses,
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
