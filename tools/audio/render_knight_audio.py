#!/usr/bin/env python3
"""Deterministically synthesize the Knight prologue's project-owned audio masters."""

from __future__ import annotations

import json
import hashlib
import math
import random
import struct
import wave
from pathlib import Path

RATE = 48_000
SEED = 0x4B4E4947
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "assets" / "audio" / "prologue"


def env(t: float, duration: float, attack: float = .02, release: float = .18) -> float:
    return min(1.0, t / max(.001, attack), (duration - t) / max(.001, release)) ** 1.4


def write(name: str, duration: float, voice, gain: float = .58) -> str:
    path = OUT / name
    path.parent.mkdir(parents=True, exist_ok=True)
    rng = random.Random(SEED ^ sum((i + 1) * ord(c) for i, c in enumerate(name)))
    frames = []
    for index in range(int(RATE * duration)):
        t = index / RATE
        value = max(-1.0, min(1.0, voice(t, duration, rng) * gain))
        frames.append(struct.pack("<h", int(value * 32767)))
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(b"".join(frames))
    return "res://" + path.relative_to(ROOT).as_posix()


def sound_family(resource_path: str) -> str:
    if "/music/" in resource_path:
        return "music"
    if "/ambience/" in resource_path:
        return "ambience"
    if "/sfx/ui/" in resource_path:
        return "ui"
    if "/sfx/mode/" in resource_path:
        return "mode-feedback"
    if "/sfx/shield/" in resource_path:
        return "shield-feedback"
    if "/sfx/claw/" in resource_path:
        return "claw-feedback"
    if "/sfx/thread/" in resource_path:
        return "mechanical-thread"
    if "telegraph.wav" in resource_path:
        return "danger-warning"
    if "/sfx/boss/" in resource_path:
        return "boss-combat"
    if "/sfx/enemies/" in resource_path:
        return "enemy-combat"
    if "/sfx/player/" in resource_path:
        return "player-combat"
    return "world"


def dominant_frequency(samples: tuple[int, ...], sample_rate: int) -> int:
    window = samples[:int(sample_rate * .6)]
    winner = 0
    winner_energy = -1.0
    for frequency in range(40, 1401, 10):
        coefficient = 2.0 * math.cos(2.0 * math.pi * frequency / sample_rate)
        previous = 0.0
        previous_previous = 0.0
        for raw_sample in window:
            current = raw_sample / 32768.0 + coefficient * previous - previous_previous
            previous_previous = previous
            previous = current
        energy = (previous_previous * previous_previous + previous * previous -
                  coefficient * previous * previous_previous)
        if energy > winner_energy:
            winner = frequency
            winner_energy = energy
    return winner


def rms_dbfs(samples: tuple[int, ...]) -> float:
    energy = sum(value * value for value in samples) / max(1, len(samples))
    return 20.0 * math.log10(max(1e-12, math.sqrt(energy) / 32768.0))


def recognizable_onset_ms(samples: tuple[int, ...], sample_rate: int) -> float:
    window = max(1, sample_rate // 100)
    for offset in range(0, len(samples) - window + 1, window):
        if rms_dbfs(samples[offset:offset + window]) >= -12.0:
            return offset * 1000.0 / sample_rate
    return len(samples) * 1000.0 / sample_rate


def file_metadata(resource_path: str) -> dict[str, object]:
    path = ROOT / resource_path.removeprefix("res://")
    with wave.open(str(path), "rb") as reader:
        sample_rate = reader.getframerate()
        frame_count = reader.getnframes()
        sample_width = reader.getsampwidth()
        channels = reader.getnchannels()
        payload = reader.readframes(frame_count)
    if sample_width != 2 or channels != 1:
        raise ValueError(f"Expected mono 16-bit WAV: {resource_path}")
    samples = struct.unpack(f"<{frame_count}h", payload)
    peak = max(abs(value) for value in samples)
    peak_dbfs = 20.0 * math.log10(max(1, peak) / 32767.0)
    family = sound_family(resource_path)
    metadata = {
        "path": resource_path,
        "sampleRate": sample_rate,
        "durationSeconds": round(frame_count / sample_rate, 6),
        "family": family,
        "peakDbfs": round(peak_dbfs, 4),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }
    if family in ("ui", "danger-warning"):
        metadata["dominantFrequencyHz"] = dominant_frequency(samples, sample_rate)
    if family == "danger-warning":
        metadata["rmsDbfs"] = round(rms_dbfs(samples), 4)
        metadata["first100MsRmsDbfs"] = round(rms_dbfs(samples[:sample_rate // 10]), 4)
        metadata["recognizableOnsetMs"] = round(recognizable_onset_ms(samples, sample_rate), 3)
    return metadata


def bell(freq: float, decay: float = 2.8):
    partials = ((1, 1), (2.01, .42), (2.73, .27), (4.12, .16), (5.4, .08))
    return lambda t, d, r: env(t, d, .004, .08) * math.exp(-decay * t) * sum(
        amp * math.sin(math.tau * freq * mul * t) for mul, amp in partials) / 1.75


def hit(freq: float, grit: float = .35):
    return lambda t, d, r: env(t, d, .002, d * .8) * (
        math.sin(math.tau * (freq * (1.0 - .4 * t / d)) * t) * (1 - grit) +
        (r.random() * 2 - 1) * grit * math.exp(-18 * t))


def wind(t, d, r):
    slow = math.sin(math.tau * .13 * t) * .2 + math.sin(math.tau * .071 * t + 1.7) * .15
    return (r.random() * 2 - 1) * (.22 + slow) + math.sin(math.tau * 63 * t) * .035


def drone(root: float, bright: float = .2):
    def voice(t, d, r):
        pulse = .72 + .28 * math.sin(math.tau * .25 * t)
        tone = math.sin(math.tau * root * t) + .45 * math.sin(math.tau * root * 1.5 * t)
        tone += bright * math.sin(math.tau * root * 3.01 * t)
        return tone * .34 * pulse + wind(t, d, r) * .08
    return voice


def sweep(start: float, end: float, noise: float = .08):
    return lambda t, d, r: env(t, d, .006, d * .35) * (
        math.sin(math.tau * (start + (end - start) * t / d) * t) * (1 - noise) +
        (r.random() * 2 - 1) * noise)


def main() -> None:
    files: dict[str, str] = {}
    files["music_exploration"] = write("music/exploration.wav", 4.0, drone(55, .12), .34)
    files["music_boss_pulse"] = write("music/boss-pulse.wav", 4.0,
        lambda t, d, r: drone(41.2, .28)(t, d, r) + hit(82, .08)(t % 1.0, 1.0, r) * (.55 if t % 1 < .18 else 0), .34)
    files["music_boss_bells"] = write("music/boss-bells.wav", 4.0,
        lambda t, d, r: bell(174.6, 1.5)(t % 2.0, 2.0, r) * (.7 if t % 2 < 1.2 else 0), .32)

    ambience_specs = {
        "AwakeningCourtyard": ("ambience/courtyard.wav", 46, .05),
        "BrokenToothCorridor": ("ambience/corridor.wav", 52, .08),
        "UpperPendulum": ("ambience/upper-wind.wav", 68, .13),
        "LowerBuriedMachinery": ("ambience/lower-machinery.wav", 37, .18),
        "BellChainMechanism": ("ambience/bell-chain.wav", 58, .11),
        "FallenBellkeeperArena": ("ambience/boss-room.wav", 31, .06),
    }
    ambience: dict[str, str] = {}
    for room, (name, root, metallic) in ambience_specs.items():
        ambience[room] = write(name, 3.0,
            lambda t, d, r, root=root, metallic=metallic: wind(t, d, r) * .48 +
            math.sin(math.tau * root * t) * .12 + metallic * math.sin(math.tau * root * 4.03 * t), .3)

    specs = {
        "PlayerAttack": [("sfx/player/attack-1.wav", .16, sweep(420, 145, .12)), ("sfx/player/attack-2.wav", .17, sweep(510, 172, .1))],
        "PlayerHit": [("sfx/player/hit-1.wav", .22, hit(118, .48)), ("sfx/player/hit-2.wav", .24, hit(92, .42))],
        "PlayerHurt": [("sfx/player/hurt.wav", .28, sweep(190, 74, .28))],
        "PlayerDash": [("sfx/player/dash.wav", .2, sweep(110, 580, .36))],
        "PlayerDownstrike": [("sfx/player/downstrike.wav", .3, sweep(310, 68, .24))],
        "ThreadFire": [("sfx/thread/fire-1.wav", .22, sweep(240, 880, .06)), ("sfx/thread/fire-2.wav", .24, sweep(280, 1020, .08))],
        "ThreadHit": [("sfx/thread/hit.wav", .34, bell(392, 4.2))],
        "ThreadFail": [("sfx/thread/fail.wav", .2, sweep(220, 90, .24))],
        "ThreadPreview": [("sfx/thread/preview.wav", .18, bell(740, 8.0))],
        "ThreadWindup": [("sfx/thread/windup.wav", .16, sweep(260, 760, .04))],
        "ThreadTravel": [("sfx/thread/travel.wav", .28, sweep(420, 1180, .12))],
        "ThreadLatch": [("sfx/thread/latch.wav", .16, hit(310, .16))],
        "ThreadTension": [("sfx/thread/tension.wav", .34,
            lambda t, d, r: bell(246.9, 5.2)(t, d, r) * .7 + sweep(180, 420, .03)(t, d, r) * .35)],
        "RustShellAttack": [("sfx/enemies/rust-attack.wav", .3, hit(76, .44))],
        "RustShellTelegraph": [("sfx/enemies/rust-telegraph.wav", .42,
            lambda t, d, r: (sweep(138, 286, .20)(t, d, r) * .7 +
            bell(196, 7.0)(t, d, r) * .25) * 1.28)],
        "RustShellRecovery": [("sfx/enemies/rust-recovery.wav", .24,
            lambda t, d, r: sweep(210, 96, .08)(t, d, r) * .52 + bell(174.6, 8.0)(t, d, r) * .28)],
        "RustShellHurt": [("sfx/enemies/rust-hurt.wav", .22, hit(143, .3))],
        "RustShellDeath": [("sfx/enemies/rust-death.wav", .48, sweep(122, 38, .45))],
        "MothDive": [("sfx/enemies/moth-dive.wav", .32, sweep(680, 130, .38))],
        "MothTelegraph": [("sfx/enemies/moth-telegraph.wav", .46,
            lambda t, d, r: (math.sin(math.tau * 174 * t) * math.sin(math.tau * 19 * t) *
            env(t, d, .03, .12) * .55 + bell(523.3, 7.5)(t, d, r) * .3) * 1.65)],
        "MothRecovery": [("sfx/enemies/moth-recovery.wav", .28,
            lambda t, d, r: sweep(420, 210, .05)(t, d, r) * .48 + bell(349.2, 9.0)(t, d, r) * .22)],
        "MothHurt": [("sfx/enemies/moth-hurt.wav", .2, sweep(510, 260, .2))],
        "MothDeath": [("sfx/enemies/moth-death.wav", .46, sweep(390, 84, .32))],
        "LaborerAttack": [("sfx/enemies/laborer-attack.wav", .36, hit(64, .35))],
        "LaborerTelegraph": [("sfx/enemies/laborer-telegraph.wav", .52,
            lambda t, d, r: (bell(116.5, 6.0)(t, d, r) * .55 + hit(73, .20)(t % .13, .13, r) *
            (.42 if (t % .13) < .06 else 0)) * 1.95)],
        "LaborerRecovery": [("sfx/enemies/laborer-recovery.wav", .30,
            lambda t, d, r: bell(130.8, 7.0)(t, d, r) * .42 + sweep(142, 68, .10)(t, d, r) * .34)],
        "LaborerGuard": [("sfx/enemies/laborer-guard.wav", .42, bell(196, 5.0))],
        "LaborerHurt": [("sfx/enemies/laborer-hurt.wav", .25, hit(105, .3))],
        "LaborerDeath": [("sfx/enemies/laborer-death.wav", .58, sweep(98, 31, .4))],
        "BossIntro": [("sfx/boss/intro.wav", .9, bell(82.4, 1.8))],
        "BossAttack": [("sfx/boss/attack.wav", .42, hit(48, .44))],
        "BossSweepTelegraph": [("sfx/boss/sweep-telegraph.wav", .58, sweep(92, 238, .12))],
        "BossLeapTelegraph": [("sfx/boss/leap-telegraph.wav", .62,
            lambda t, d, r: sweep(61, 188, .06)(t, d, r) + bell(123.5, 4.8)(t, d, r) * .22)],
        "BossGroundWaveTelegraph": [("sfx/boss/ground-wave-telegraph.wav", .7,
            lambda t, d, r: (bell(55, 5.5)(t, d, r) * .5 +
            math.sin(math.tau * 41.2 * t) * env(t, d, .12, .16) * .5) * 1.75)],
        "BossHammerTelegraph": [("sfx/boss/hammer-telegraph.wav", .64,
            lambda t, d, r: (bell(146.8, 6.5)(t, d, r) * .45 + hit(82, .22)(t % .18, .18, r) *
            (.5 if (t % .18) < .075 else 0)) * 1.95)],
        "BossRecovery": [("sfx/boss/recovery.wav", .34,
            lambda t, d, r: sweep(118, 52, .08)(t, d, r) * .42 + bell(98, 7.5)(t, d, r) * .34)],
        "BossTransition": [("sfx/boss/transition.wav", .85, sweep(74, 410, .16))],
        "BossHammer": [("sfx/boss/hammer.wav", .55, hit(39, .52))],
        "BossExposed": [("sfx/boss/exposed.wav", .62, bell(261.6, 2.2))],
        "BossDefeat": [("sfx/boss/defeat.wav", 1.1, lambda t, d, r: bell(73.4, 1.3)(t, d, r) + sweep(180, 28, .2)(t, d, r) * .45)],
        "Checkpoint": [("sfx/world/checkpoint.wav", .72, bell(329.6, 2.8))],
        "Shortcut": [("sfx/world/shortcut.wav", .64, bell(220, 3.5))],
        "UiMove": [("sfx/ui/move.wav", .08, bell(660, 12))],
        "UiConfirm": [("sfx/ui/confirm.wav", .14, bell(880, 10))],
        "UiBack": [("sfx/ui/back.wav", .12, sweep(520, 330, .02))],
        "RecoveryStart": [("sfx/world/recovery-start.wav", .52, sweep(230, 62, .18))],
        "RecoveryEnd": [("sfx/world/recovery-end.wav", .58, bell(293.7, 3.0))],
        "Completion": [("sfx/world/completion.wav", 1.2, lambda t, d, r: bell(220, 1.8)(t, d, r) + bell(329.6, 2.4)(max(0, t-.22), d, r) * (.7 if t > .22 else 0))],
        "ModeAttack": [("sfx/mode/attack.wav", .18, bell(620, 9.3))],
        "ModeGrapple": [("sfx/mode/grapple.wav", .20, bell(780, 8.2))],
        "EmptySlot": [("sfx/mode/empty-slot.wav", .16, hit(170, .22))],
        "EnergyEmpty": [("sfx/mode/energy-empty.wav", .22, sweep(290, 105, .08))],
        "ShieldRaise": [("sfx/shield/raise.wav", .20, sweep(240, 520, .12))],
        "ShieldBlock": [("sfx/shield/block.wav", .16, hit(420, .20))],
        "ShieldHeavyBreak": [("sfx/shield/heavy-break.wav", .32, hit(125, .44))],
        "ShieldOverheat": [("sfx/shield/overheat.wav", .42, sweep(480, 150, .18))],
        "ShieldReady": [("sfx/shield/ready.wav", .24, bell(540, 7.4))],
        "ClawFire": [("sfx/claw/fire.wav", .18, sweep(310, 720, .09))],
        "ClawTravel": [("sfx/claw/travel.wav", .22, sweep(690, 410, .11))],
        "ClawLatch": [("sfx/claw/latch.wav", .18, hit(510, .18))],
        "ClawTension": [("sfx/claw/tension.wav", .26, bell(250, 6.8))],
        "ClawRelease": [("sfx/claw/release.wav", .20, sweep(440, 180, .09))],
        "ClawIceSlip": [("sfx/claw/ice-slip.wav", .28, sweep(940, 330, .15))],
        "ClawFail": [("sfx/claw/fail.wav", .24, hit(150, .34))],
    }
    cues: dict[str, list[str]] = {}
    for cue, variants in specs.items():
        cues[cue] = [write(name, duration, voice) for name, duration, voice in variants]

    manifest = {
        "version": 1,
        "sampleRate": RATE,
        "source": "Knight local procedural synthesis",
        "license": "Project-owned",
        "seed": SEED,
        "generator": "tools/audio/render_knight_audio.py",
        "buses": ["Master", "Music", "SFX", "Ambience", "UI"],
        "music": {
            "Exploration": [files["music_exploration"]],
            "BossPhaseOne": [files["music_boss_pulse"]],
            "BossPhaseTwo": [files["music_boss_pulse"], files["music_boss_bells"]],
        },
        "ambience": ambience,
        "cues": cues,
    }
    all_paths = sorted(set(files.values()) | set(ambience.values()) |
        {path for variants in cues.values() for path in variants})
    manifest["fileMetadata"] = {path: file_metadata(path) for path in all_paths}
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "audio-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Rendered {sum(len(v) for v in cues.values()) + 9} original 48 kHz WAV masters to {OUT}")


if __name__ == "__main__":
    main()
