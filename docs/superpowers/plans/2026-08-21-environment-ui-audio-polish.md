# First-Level Environment UI and Audio Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give all six rooms, three ordinary enemies, the Boss, HUD, guidance, VFX, and audio one coherent “Twilight Belfry Industrial Ruin” language while preserving every gameplay and replacement interface.

**Architecture:** RoomVisualDescriptor is the data source for five visible depth roles assembled into the existing seven stable Art leaves. Formal enemy rigs retain their gameplay roots and receive replaceable identity/corruption layers. The compact HUD consumes its view model only. Audio maps the typed attack/thread phase cues from Plans 1–2 to locally synthesized, manifest-verified files.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, Node2D/CanvasItem/Control/Theme, local SVG/PNG and deterministic WAV synthesis, generated scenes, resolution captures, production-asset manifest, PowerShell gates.

**Spec:** [Combat readability and presentation design](../specs/2026-08-21-combat-readability-presentation-design.md)

## Global Constraints

- Preserve all collision, navigation, spawn, exit, checkpoint, target, camera-limit, guidance-target, and room-root paths.
- Preserve the existing Art/Far, Back, Mid, Platforms, Props, Front, and Atmosphere leaves; the five visual roles are composed through those stable slots.
- Readability order is large silhouette, value grouping, landmark, then low-frequency material detail; high-density monochrome noise is prohibited.
- Old gold is interaction/guidance, cyan is player thread, red is danger, white is impact; no room decoration may reuse danger red as ambience.
- AC enemy identity is approximately 65 percent old belfry worker/guard and 35 percent organic corruption; corruption stays off weapon, facing, attack path, and feet.
- Foreground fades when it overlaps combat/player/target regions and never covers a telegraph.
- HUD uses at least two channels per critical state and remains inside safe areas at 720p, 1080p, 1440p, 4K, and supported widescreen.
- All audio is generated locally by the reproducible renderer; no pay-per-use image, video, music, or voice service.

---

### Task 1: Six room descriptors and landmark-specific layered art

**Files:**
- Create: `scripts/presentation/RoomVisualDescriptor.cs`
- Create: `scripts/presentation/RoomVisualCatalog.cs`
- Modify: `scripts/config/RoomPresentationProfile.cs`
- Modify: `scripts/presentation/RoomFormalVisual.cs`
- Modify: `tools/build/art/BuildPrologueEnvironmentKit.cs`
- Modify: `tools/build/prologue/PrologueBuildParts.cs`
- Modify: `tools/build/prologue/BuildAwakeningCourtyard.cs`
- Modify: `tools/build/prologue/BuildBrokenToothCorridor.cs`
- Modify: `tools/build/prologue/BuildUpperPendulum.cs`
- Modify: `tools/build/prologue/BuildLowerBuriedMachinery.cs`
- Modify: `tools/build/prologue/BuildBellChainRoom.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeperArena.cs`
- Modify: `scenes/prologue/rooms/AwakeningCourtyard.tscn`
- Modify: `scenes/prologue/rooms/BrokenToothCorridor.tscn`
- Modify: `scenes/prologue/rooms/UpperPendulum.tscn`
- Modify: `scenes/prologue/rooms/LowerBuriedMachinery.tscn`
- Modify: `scenes/prologue/rooms/BellChainMechanism.tscn`
- Modify: `scenes/prologue/rooms/FallenBellkeeperArena.tscn`
- Modify: `tests/integration/PrologueEnvironmentStructureTests.cs`
- Modify: `tests/integration/PrologueEnvironmentRuntimeTests.cs`

**Interfaces:**
- Produces: `RoomVisualDescriptor(PrologueRoomId Room, string Landmark, Color Shadow, Color Stone, Color Copper, float FarParallax, float MidParallax, float FrontParallax, RoomAtmosphereKind Atmosphere)`.
- Produces: `RoomVisualCatalog.For(PrologueRoomId room) -> RoomVisualDescriptor`.
- Extends: `RoomFormalVisual.Present(RoomVisualDescriptor descriptor, Vector2 cameraVelocity, IReadOnlyList<Rect2> protectedScreenRegions)`.

- [ ] **Step 1: Write catalog and structure RED tests**

  Require one unique descriptor/landmark for Courtyard broken clock, Corridor severed gear train, Upper pendulum chains, Lower copper pipes/fungus, Bell-Chain vertical chain, and Boss backlit broken bell. Assert every room retains seven stable art paths, five depth roles, platform skins aligned to collision, and no duplicate landmark id.

- [ ] **Step 2: Run RED**

  Expected: missing catalog/descriptor and current generic palette/prop construction cannot identify all rooms.

- [ ] **Step 3: Build large-form room compositions**

  Create room-specific silhouettes and landmarks first. Add stone, beams, copper pipes, bells, shafts, and controlled corruption to Mid/Props. Derive platform skins from collision rectangles. Use Far/Mid/Front parallax and Atmosphere particles with deterministic seeds.

- [ ] **Step 4: Add readability-aware foreground fading**

  Fade only the overlapping Front leaf or prop group, never the whole room. Protected regions include player, active enemies, thread target/line, current attack shape, and Boss feet/ground danger.

- [ ] **Step 5: Run GREEN and occlusion mutation**

  Temporarily keep a foreground column opaque over a named combat protected region. Require the runtime occlusion assertion to fail, restore, regenerate, and rerun.

- [ ] **Step 6: Commit**

    git commit -m "feat: distinguish layered prologue rooms"

---

### Task 2: AC medium-corruption enemy and Boss art pass

**Files:**
- Modify: `tools/build/art/BuildEnemyFormalVisuals.cs`
- Modify: `tools/build/art/BuildFallenBellkeeperFormalVisual.cs`
- Modify: `assets/enemies/formal/enemy-atlas.png`
- Modify: `assets/enemies/formal/rust-shell.png`
- Modify: `assets/enemies/formal/grave-lamp-moth.png`
- Modify: `assets/enemies/formal/bell-back-laborer.png`
- Modify: `assets/boss/fallen-bellkeeper/fallen-bellkeeper.png`
- Modify: `scenes/presentation/RustShellFormalVisual.tscn`
- Modify: `scenes/presentation/GraveLampMothFormalVisual.tscn`
- Modify: `scenes/presentation/BellBackLaborerFormalVisual.tscn`
- Modify: `scenes/presentation/FallenBellkeeperFormalVisual.tscn`
- Modify: `tests/integration/EnemyFormalVisualTests.cs`
- Modify: `tests/integration/FallenBellkeeperFormalVisualTests.cs`
- Modify: `tests/unit/ProductionAssetManifestTests.cs`

**Interfaces:**
- Stable visual children per ordinary enemy: `Body`, `Weapon`, `Corruption`, `Accessory`, `Effects`.
- Boss retains `Body`, `ArmorShell`, `Hammer`, `Chain`, and `Core`.
- Builders publish deterministic atlas metadata with source region, pivot, and semantic layer.

- [ ] **Step 1: Add layer and silhouette RED assertions**

  Require explicit weapon/body/corruption separation, weapon-side bounds unobscured by corruption, visible facing indicator, visible feet/contact, enemy-specific profile dimensions, and medium corruption limited to back/non-weapon outline.

- [ ] **Step 2: Run RED**

  Expected: current atlas/scenes do not expose the complete ordinary-enemy layer contract.

- [ ] **Step 3: Rebuild project-owned formal silhouettes**

  Rust is a low shield-shell guard with a long thrust silhouette; Moth has broad wing membrane and grave-lamp core; Laborer has a heavy back bell and readable hammer arc; Boss remains asymmetric and five-part. Use broad value shapes and sparse corrosion, not micro-noise.

- [ ] **Step 4: Regenerate and review at gameplay scale**

  Review 1280×720 captures with all warning colors disabled first to verify identity from silhouette alone, then with warning colors enabled to verify no corruption competes with attack information.

- [ ] **Step 5: Run GREEN and weapon-occlusion mutation**

  Move a corruption layer across the weapon-side bounding box; require the named structure/audit assertion to fail, restore, and rerun.

- [ ] **Step 6: Commit**

    git commit -m "feat: refine belfry enemy silhouettes"

---

### Task 3: Compact layered HUD and contextual guidance

**Files:**
- Modify: `scripts/ui/PrologueHudViewModel.cs`
- Modify: `scripts/ui/PrologueFormalHud.cs`
- Modify: `scripts/ui/PrologueThreadSpoolVisual.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceController.cs`
- Modify: `scripts/ui/WorldTargetMarkerVisual.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `scenes/prologue/PrologueLevel.tscn`
- Modify: `tests/unit/PrologueHudViewModelTests.cs`
- Modify: `tests/integration/PrologueFormalHudTests.cs`
- Modify: `tests/integration/PrologueGuidanceRuntimeTests.cs`
- Modify: `tests/presentation/PrologueUiResolutionCapture.cs`

**Interfaces:**
- `PrologueHudViewModel` consumes Plan 2 thread state/failure plus current objective transition time.
- `PrologueHudLayout.Calculate(Vector2 viewport, float uiScale) -> PrologueHudLayout`.
- Guidance visual supports compact world prompt and expanded teaching card while controller progression remains first-level-only.

- [ ] **Step 1: Write compact-layout RED tests**

  Require health and thread to form one top-left instrument cluster; objective to occupy one line by default; contextual action prompt to stay near its target but outside player/enemy/danger protected regions; Boss bar to stay in the bottom safe area; and Guidance expanded only on first trigger, repeated failure, or manual re-enable.

- [ ] **Step 2: Run RED**

  Expected: current thread panel is separate, objective/guidance occupy large fixed blocks, and target-aware prompt avoidance is absent.

- [ ] **Step 3: Implement the compact hierarchy**

  Merge shard/spool framing without merging data ownership. The spool outer ring is cooldown; center/icon represents Ready, Preview, Travel, Latched, Failed. Objective briefly expands on change then collapses to a single line. World prompts use stable target views and a protected-region placer.

- [ ] **Step 4: Validate accessibility and resolutions**

  Capture 1280×720, 1920×1080, 2560×1440, 3840×2160, and supported widescreen. Assert safe margins, no overlap, readable type size, keyboard/controller glyph changes, reduced flash, UI scale, and two-channel critical states.

- [ ] **Step 5: Run GREEN and overlap mutation**

  Force the Boss bar upward over the ground danger region; require the layout audit to fail, restore, and rerun.

- [ ] **Step 6: Commit**

    git commit -m "feat: compact first-level combat HUD"

---

### Task 4: Phase-specific local audio, VFX balance, and production audit

**Files:**
- Modify: `scripts/audio/PrologueAudioCue.cs`
- Modify: `scripts/audio/PrologueAudioDirector.cs`
- Modify: `tools/audio/render_knight_audio.py`
- Modify: `assets/audio/prologue/audio-manifest.json`
- Create: `assets/audio/prologue/sfx/enemies/rust-telegraph.wav`
- Create: `assets/audio/prologue/sfx/enemies/moth-telegraph.wav`
- Create: `assets/audio/prologue/sfx/enemies/laborer-telegraph.wav`
- Create: `assets/audio/prologue/sfx/boss/sweep-telegraph.wav`
- Create: `assets/audio/prologue/sfx/boss/leap-telegraph.wav`
- Create: `assets/audio/prologue/sfx/boss/ground-wave-telegraph.wav`
- Create: `assets/audio/prologue/sfx/boss/hammer-telegraph.wav`
- Create: `assets/audio/prologue/sfx/thread/preview.wav`
- Create: `assets/audio/prologue/sfx/thread/windup.wav`
- Create: `assets/audio/prologue/sfx/thread/travel.wav`
- Create: `assets/audio/prologue/sfx/thread/latch.wav`
- Create: `assets/audio/prologue/sfx/thread/tension.wav`
- Modify: `scripts/vfx/PrologueVfxDirector.cs`
- Modify: `tests/unit/PrologueAudioManifestTests.cs`
- Modify: `tests/integration/PrologueAudioDirectorTests.cs`
- Modify: `tests/integration/PrologueVfxRuntimeTests.cs`
- Modify: `tests/presentation/PrologueVisualAudit.cs`
- Modify: `tools/audit-prologue-production.ps1`

**Interfaces:**
- Adds audio cues: `RustShellTelegraph`, `MothTelegraph`, `LaborerTelegraph`, `BossSweepTelegraph`, `BossLeapTelegraph`, `BossGroundWaveTelegraph`, `BossHammerTelegraph`, `ThreadPreview`, `ThreadWindup`, `ThreadTravel`, `ThreadLatch`, and `ThreadTension`.
- `PrologueAudioDirector.PlayAttackFrame(Node source, AttackPresentationFrame frame, Vector2 position)` deduplicates by AttackId/phase.
- `PrologueAudioDirector.PlayThreadFrame(ThreadCastFrame frame, Vector2 position)` deduplicates by CastId/state.

- [ ] **Step 1: Write mapping/deduplication RED tests**

  Require a distinct onset path for each attack kind, separate windup/active/recovery families, four mechanical-thread layers, no duplicate playback for repeated same-phase frames, manifest files at 48 kHz, and no UI cue using the danger-warning frequency family.

- [ ] **Step 2: Run RED**

  Expected: current audio enum maps only Active AttackStarted and thread Fired/Hit/Fail.

- [ ] **Step 3: Extend the deterministic local renderer**

  Generate short, actor-specific warning/onset/recovery sounds and spool/shuttle/latch/tension layers with fixed seeds. Preserve existing masters, bus layout, limiter, manifest validation, and reproducibility.

- [ ] **Step 4: Wire typed frames and rebalance VFX**

  Emit warning sound on Telegraph entry, attack onset on Active entry, recovery release once. VFX follows the same frames, keeps red exclusively on true danger, white on impact, and obeys reduced-flash/shake settings.

- [ ] **Step 5: Run audio/visual production audit**

    powershell -ExecutionPolicy Bypass -File .\tools\audit-prologue-production.ps1

  Require manifest completeness, deterministic hashes, no clipping above the existing limiter/headroom contract, no missing resources, no unresolved fallback, and all resolution/performance markers.

- [ ] **Step 6: Commit**

    git commit -m "feat: add staged combat sound language"
