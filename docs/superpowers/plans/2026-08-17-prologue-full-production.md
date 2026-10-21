# Prologue Full Production Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace every visible prologue gray-box element with a cohesive finished first-level presentation, including the full UI suite, formal character/environment art, animation, VFX, audio, settings, save flow, accessibility, and final playable delivery.

**Architecture:** Existing gameplay roots, collision, damage, checkpoints, progress, and room links remain authoritative. New UI, save/settings, presentation, art, VFX, and audio components consume typed state/events through leaf scenes and profiles; builders generate and validate all `.tscn` files before the parent prologue scene. Runtime assets are project-owned and locally authored, while external production sources stay under `E:\OurGame\ArtProduction`.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, generated Godot scenes, local SVG/PNG and WAV/OGG asset production, Godot `Theme`, `AnimationPlayer`, `AudioStreamPlayer`, `CanvasItem` shaders, NUnit-free in-project test runners, PowerShell verification.

## Global Constraints

- Preserve `Godot.NET.Sdk/4.7.1`, `net9.0`, `EnableDynamicLoading=true`, and the installed Godot 4.7.1 Mono toolchain.
- Do not alter gameplay timing, damage, collision, cooldown, checkpoint, route, or Boss rules to accommodate presentation.
- The rejected A2 dressed character image may not become runtime art, an animation master, or an enemy template.
- Do not call pay-per-use image, video, music, or voice generation APIs without separate user approval.
- Runtime assets live under `assets/`; editable sources and rejected candidates stay outside the Godot project.
- All new Godot C# classes are `partial`; builders contain no runtime behavior and pack/instantiate/count-validate generated scenes.
- The final build must have no visible debug/gray-box fallback, missing asset, fallback font, missing audio, or untranslated UI string.
- Design baseline is 1280×720 and must pass 1920×1080, 2560×1440, and 3840×2160.
- Finish with a visible proof recording and a real final-build playtest; automated drivers are not human completion evidence.

---

### Task 1: Production asset manifest and strict fallback gate

**Files:**
- Create: `scripts/presentation/ProductionAssetManifest.cs`
- Create: `scripts/presentation/ProductionAssetManifestParser.cs`
- Create: `assets/production/prologue-assets.json`
- Create: `tests/unit/ProductionAssetManifestTests.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `README.md`

**Interfaces:**
- Produces: `ProductionAssetManifestParser.Parse(string json) -> ProductionAssetManifest`
- Produces: `ProductionAssetManifest.RequireRuntimeFiles(string projectRoot) -> IReadOnlyList<string>`
- Produces: manifest categories `ui`, `player`, `enemies`, `boss`, `environment`, `vfx`, `audio`, `localization`

- [x] **Step 1: Write the failing manifest tests**

```csharp
public static void Run()
{
    RejectsMissingCategories();
    RejectsDuplicateIds();
    RejectsExternalRuntimePaths();
    RequiresExistingFilesAndDeclaredLicenses();
}
```

Create fixtures that omit `audio`, repeat an id, point at `E:\OurGame\ArtProduction`, omit `license`, and point at a nonexistent `res://assets/...` file. Register `ProductionAssetManifestTests.Run()` before scene runtime tests.

- [x] **Step 2: Run RED**

Run:

```powershell
G:\godot\dotnet-sdk\dotnet.exe build .\Knight.csproj
```

Expected: compile failure because `ProductionAssetManifestParser` does not exist.

- [x] **Step 3: Implement strict parsing and the initial manifest**

```csharp
public sealed record ProductionAssetEntry(string Id, string Category, string RuntimePath,
    string Source, string License, bool Final);

public sealed record ProductionAssetManifest(int Version,
    IReadOnlyList<ProductionAssetEntry> Assets)
{
    public IReadOnlyList<string> RequireRuntimeFiles(string projectRoot) { /* validate res:// files */ }
}
```

Use `System.Text.Json`, ordinal id/path comparisons, all eight required categories, `res://assets/` runtime paths only, and a nonempty source/license. Seed the JSON with final=false entries for every planned asset family so progress is explicit and no nonexistent file is marked final.

- [x] **Step 4: Run GREEN and mutation**

Run build plus `RunPrologueTests.cs`; expect one `PROLOGUE_TESTS_OK`. Temporarily allow duplicate ids and prove `RejectsDuplicateIds` fails, then restore.

- [x] **Step 5: Update README and commit**

Record the new milestone, manifest path, zero-paid boundary, and asset table. Commit only the six owned files:

```powershell
git commit -m "feat: enforce prologue production assets"
```

---

### Task 2: Settings, localization, input glyphs, and durable save data

**Files:**
- Create: `scripts/settings/GameSettingsData.cs`
- Create: `scripts/settings/GameSettingsStore.cs`
- Create: `scripts/save/PrologueSaveData.cs`
- Create: `scripts/save/PrologueSaveStore.cs`
- Create: `scripts/ui/InputGlyphResolver.cs`
- Create: `scripts/localization/PrologueStrings.cs`
- Create: `assets/localization/prologue-ui.json`
- Create: `tests/unit/GameSettingsTests.cs`
- Create: `tests/unit/PrologueSaveTests.cs`
- Create: `tests/unit/InputGlyphResolverTests.cs`
- Create: `tests/unit/PrologueStringsTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: `GameSettingsStore.LoadOrDefault(string path) -> GameSettingsData`
- Produces: `GameSettingsStore.SaveAtomic(string path, GameSettingsData data)`
- Produces: `PrologueSaveStore.LoadOrNew(string path) -> PrologueSaveData`
- Produces: `InputGlyphResolver.Resolve(InputDeviceKind device, string action) -> InputGlyph`
- Produces: `PrologueStrings.Get(string key, GameLanguage language) -> string`

- [x] **Step 1: Write four failing suites**

```csharp
TestAssert.Equal(0.8f, GameSettingsStore.LoadOrDefault(missing).MasterVolume,
    "missing settings use safe defaults");
TestAssert.True(PrologueSaveStore.LoadOrNew(corrupt).WasRecovered,
    "corrupt save recovers without claiming progress");
TestAssert.Equal("SPACE", InputGlyphResolver.Resolve(InputDeviceKind.KeyboardMouse, "jump").Text,
    "keyboard jump glyph");
TestAssert.Equal("继续", PrologueStrings.Get("menu.continue", GameLanguage.ChineseSimplified),
    "Chinese menu string");
```

Also cover clamping volume/UI scale/shake, atomic replace, all implemented input actions, both languages, missing-key rejection, checkpoint/shortcut/discovery/completion persistence, and automation-source exclusion from human statistics.

- [x] **Step 2: Run RED**

Build must fail on missing settings/save/UI/localization types.

- [x] **Step 3: Implement deterministic stores and string table**

Use JSON version fields, temporary-file + replace writes, `InvariantCulture`, clamped settings, and explicit `GameLanguage.ChineseSimplified`/`English`. Keep paths injectable for tests; runtime controllers later supply `user://` paths.

- [x] **Step 4: Run GREEN and corrupt-file mutation**

Run the full prologue runner. Temporarily deserialize corrupt JSON as a completed save; expect `corrupt save recovers without claiming progress` to fail, then restore.

- [x] **Step 5: Commit**

```powershell
git commit -m "feat: add prologue settings and save data"
```

---

### Task 3: Formal UI theme and product shell

**Files:**
- Create: `scripts/ui/PrologueUiPalette.cs`
- Create: `scripts/ui/PrologueMainMenu.cs`
- Create: `scripts/ui/PrologueSettingsMenu.cs`
- Create: `scripts/ui/PrologueControlsView.cs`
- Create: `scripts/ui/PrologueUiRoot.cs`
- Create: `tools/build/ui/BuildPrologueUi.cs`
- Create: `scenes/ui/PrologueUi.tscn`
- Create: `tests/integration/PrologueUiStructureTests.cs`
- Create: `tests/integration/PrologueUiRuntimeTests.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Produces: `PrologueUiRoot.ShowMainMenu(bool hasSave)`
- Produces: `PrologueUiRoot.ShowSettings(GameSettingsData settings)`
- Produces: `PrologueUiRoot.SetLastInputDevice(InputDeviceKind device)`
- Consumes: Task 2 settings, glyphs, save and localization types

- [x] **Step 1: Write structure and runtime RED tests**

Require exact paths `MainMenu/Buttons/Start`, `Continue`, `Settings`, `Controls`, `Quit`, `SettingsMenu`, and `ControlsView`; assert all visible labels resolve through localization, focus starts on the first enabled button, Continue is disabled without a valid save, Escape returns one menu level, and every button remains inside the 48 px safe margin at four target resolutions.

- [x] **Step 2: Run RED**

Build must fail because `PrologueUiRoot` and the builder are missing.

- [x] **Step 3: Build the dark-glass/brass theme and shell**

```csharp
public static readonly Color Ink = new("0b1118");
public static readonly Color Glass = new(0.055f, 0.086f, 0.11f, 0.94f);
public static readonly Color Bone = new("e8dfc6");
public static readonly Color Brass = new("d6a948");
public static readonly Color Thread = new("72d9d1");
public static readonly Color Rust = new("a54d37");
```

Create reusable `StyleBoxFlat` resources with 2 px brass borders, 10 px corners, clear focus outlines, and no texture noise. Anchor all panels and generate the UI leaf scene with pack/instantiate node-count validation.

- [x] **Step 4: Run GREEN and focus mutation**

Run builder and full runner. Remove initial `GrabFocus()` and prove the named controller-focus assertion fails, then restore.

- [x] **Step 5: Add builder to the verifier and commit**

Add `BuildPrologueUi` before `BuildPrologueLevel`, include `scenes/ui/PrologueUi.tscn` in semantic snapshots, and commit:

```powershell
git commit -m "feat: add formal prologue menu shell"
```

---

### Task 4: HUD, guidance, Boss bar, recovery, and completion UI

**Files:**
- Create: `scripts/ui/PrologueHudViewModel.cs`
- Create: `scripts/ui/PrologueFormalHud.cs`
- Create: `scripts/ui/PrologueRecoveryView.cs`
- Create: `scripts/ui/PrologueCompletionView.cs`
- Create: `scripts/ui/WorldTargetMarkerVisual.cs`
- Modify: `scripts/prologue/PrologueHud.cs`
- Modify: `scripts/prologue/ProloguePauseMenu.cs`
- Modify: `scripts/prologue/guidance/IPrologueGuidanceVisual.cs`
- Modify: `tools/build/ui/BuildPrologueUi.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Create: `tests/unit/PrologueHudViewModelTests.cs`
- Create: `tests/integration/PrologueFormalHudTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: immutable `PrologueHudViewModel` with health segments, thread ratio/state, objective, prompt, Boss state, recovery and completion statistics
- Produces: `PrologueFormalHud.Present(PrologueHudViewModel model)`
- Produces: `WorldTargetMarkerVisual.Present(PrologueGuidanceInstruction instruction, IReadOnlyList<PrologueGuidanceTargetView> targets)`

- [x] **Step 1: Write model and four-resolution RED tests**

```csharp
TestAssert.Equal(5, model.HealthSegments.Count, "five formal health segments");
TestAssert.Equal(1f, model.ThreadReadyRatio, "ready thread spool is full");
TestAssert.False(layout.GuidanceRect.Intersects(layout.BossRect),
    "guidance does not overlap Boss HUD");
```

Cover broken/restored health pieces, cooldown ratio, objective collapse, keyboard/gamepad prompts, second-failure hints, Boss phase/armor/exposed display, recovery reason, exact completion statistics, and 720p/1080p/1440p/4K bounds.

- [x] **Step 2: Run RED**

Build must fail on missing `PrologueHudViewModel` and formal HUD types.

- [x] **Step 3: Implement formal HUD and replace the generated placeholder tree**

Use separate controls for health shard icons, animated thread spool, objective ribbon, guidance panel, bottom Boss bar, prompt glyph, recovery overlay, and completion card. Keep the existing public `PrologueHud` methods as adapters so gameplay callers do not change.

- [x] **Step 4: Run GREEN and overlap mutation**

Move the Boss bar into the guidance rectangle, prove the exact overlap test fails, restore, then run builder and runner.

- [x] **Step 5: Commit**

```powershell
git commit -m "feat: deliver complete prologue HUD"
```

---

### Task 5: Player formal layered rig and animation set

**Files:**
- Create: `scripts/presentation/PlayerFormalVisual.cs`
- Create: `scripts/presentation/PlayerAnimationMap.cs`
- Create: `tools/build/art/BuildPlayerFormalVisual.cs`
- Create: `scenes/presentation/PlayerFormalVisual.tscn`
- Create: `assets/player/formal/player-layers.png`
- Create: `assets/player/formal/player-layers.json`
- Create: `assets/player/formal/player-animation-map.json`
- Create: `tests/unit/PlayerAnimationMapTests.cs`
- Create: `tests/integration/PlayerFormalVisualTests.cs`
- Modify: `tools/build/BuildPlayer.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Produces: `PlayerAnimationMap.Resolve(EntityPresentationFrame frame) -> PlayerAnimationClip`
- Produces: `PlayerFormalVisual : Node2D, IEntityVisual2D`
- Consumes: existing `PlayerPresentationCoordinator` frames and `PresentationCue`s

- [x] **Step 1: Write RED tests for every required state and layer**

Assert idle, move, rise, fall, land, dash, ground/air/down attack, thread cast, hurt, dead and recovery mappings. Require exact layer nodes `Body`, `NearArm`, `FarArm`, `NearLeg`, `FarLeg`, `Weapon`, `Mantle`, `ShoulderCore`, `Lantern`, `Tether`, and no `PlayerPlaceholderVisual` in the generated Player scene.

- [x] **Step 2: Run RED**

Build must fail on missing formal visual types.

- [x] **Step 3: Author the compact final layer atlas and animation map**

Use project-owned local vector/raster sources, not the rejected A2 dressed image. Keep a 96 px gameplay-height review in the external production workspace. The atlas metadata must provide explicit rectangles and pivot/anchor data; the parser may not infer equal cells.

- [x] **Step 4: Implement motion without whole-image stretching**

`PlayerFormalVisual` maps frames to named clips, uses independent mantle/lantern/tether tracks, and plays cue-specific attack/impact overlays. Scale remains `(±1, 1)` only for facing; animation tracks may not nonuniformly scale the assembled character.

- [x] **Step 5: Run GREEN, animation mutation, and visual capture**

Temporarily map `Fall` to `Rise` and prove the named test fails. Restore, build the leaf and Player scenes, run the test runner, and capture idle/run/jump/fall/attack/thread/hurt sequences at gameplay scale for internal review.

- [x] **Step 6: Update manifest and commit**

Mark only the validated player files final and commit:

```powershell
git commit -m "feat: replace player gray-box art"
```

---

### Task 6: Formal enemy visuals and animation

**Files:**
- Create: `scripts/presentation/RustShellFormalVisual.cs`
- Create: `scripts/presentation/GraveLampMothFormalVisual.cs`
- Create: `scripts/presentation/BellBackLaborerFormalVisual.cs`
- Create: `tools/build/art/BuildEnemyFormalVisuals.cs`
- Create: `scenes/presentation/RustShellFormalVisual.tscn`
- Create: `scenes/presentation/GraveLampMothFormalVisual.tscn`
- Create: `scenes/presentation/BellBackLaborerFormalVisual.tscn`
- Create: `assets/enemies/formal/rust-shell.png`
- Create: `assets/enemies/formal/grave-lamp-moth.png`
- Create: `assets/enemies/formal/bell-back-laborer.png`
- Create: `tests/integration/EnemyFormalVisualTests.cs`
- Modify: three enemy builders
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Produces: three `IEntityVisual2D` implementations consuming existing enemy coordinator frames
- Produces: exact animation names for idle/chase/telegraph/attack/recover/hurt/stunned/dead plus archetype-specific fly/dive/thread-pull

- [x] **Step 1: Write structural/state RED tests**

Require the formal leaf scenes, correct animation for every existing model state, no placeholder/debug visual in generated enemy scenes, stable collision paths, and unchanged gameplay node counts.

- [x] **Step 2: Run RED**

Build must fail on missing formal enemy visual classes.

- [x] **Step 3: Author and wire three distinct silhouettes**

Create low armored Rust-Shell, winged lantern Moth, and bell-backed Laborer assets in the shared palette. Keep telegraph shapes readable without color. Build leaf scenes, then regenerate all three enemy scenes.

- [x] **Step 4: Run GREEN and state mutation**

Temporarily map Laborer stunned to idle and prove the visual-state assertion fails, restore, then run import/builders/tests.

- [x] **Step 5: Update manifest and commit**

```powershell
git commit -m "feat: replace prologue enemy art"
```

---

### Task 7: Fallen Bellkeeper formal Boss presentation

**Files:**
- Create: `scripts/boss/FallenBellkeeperFormalVisual.cs`
- Create: `tools/build/art/BuildFallenBellkeeperFormalVisual.cs`
- Create: `scenes/presentation/FallenBellkeeperFormalVisual.tscn`
- Create: `assets/boss/fallen-bellkeeper/fallen-bellkeeper.png`
- Create: `assets/boss/fallen-bellkeeper/animation-map.json`
- Create: `tests/integration/FallenBellkeeperFormalVisualTests.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeper.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Produces: `FallenBellkeeperFormalVisual.ApplyBossFrame(FallenBellkeeperFrame frame, int facing)`
- Consumes: existing `FallenBellkeeperPresentationCoordinator` and model state

- [x] **Step 1: Write RED tests for Boss readability**

Require unique formal clips for open phase, armor close, transition, hammer telegraph, hammer thrown, chain pull, exposed, recovery and defeated. Assert `ChainTarget` and hammer visuals remain separate nodes and final visuals do not alter hitbox/health paths.

- [x] **Step 2: Run RED**

Build must fail on the missing formal Boss visual.

- [x] **Step 3: Author and build the Boss leaf scene**

Use a large asymmetric fallen bell shell with a readable hammer arm, independent chains and cyan exposed core. Preserve arena visibility and use shape/action plus audio hooks for attacks.

- [x] **Step 4: Run GREEN and exposed-state mutation**

Map Exposed to ArmorClosed, prove the named state assertion fails, restore, regenerate Boss and arena, then run full tests.

- [x] **Step 5: Commit**

```powershell
git commit -m "feat: deliver Fallen Bellkeeper art"
```

---

### Task 8: Shared environment kit and six-room art layers

**Files:**
- Create: `scripts/presentation/RoomFormalVisual.cs`
- Create: `scripts/presentation/ForegroundOccluder.cs`
- Create: `tools/build/art/BuildPrologueEnvironmentKit.cs`
- Create: `scenes/presentation/PrologueEnvironmentKit.tscn`
- Create: `assets/environment/nameless-belfry/environment-atlas.png`
- Create: `assets/environment/nameless-belfry/environment-atlas.json`
- Create: six `assets/environment/nameless-belfry/rooms/<room>-layers.json` files
- Modify: six room builders
- Create: `tests/integration/PrologueEnvironmentStructureTests.cs`
- Create: `tests/integration/PrologueEnvironmentRuntimeTests.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Produces: room paths `Art/Far`, `Art/Back`, `Art/Mid`, `Art/Platforms`, `Art/Props`, `Art/Front`, `Art/Atmosphere`
- Produces: `ForegroundOccluder.SetPlayerBehind(bool behind)`
- Consumes: existing collision/platform/checkpoint/exit/mechanism nodes unchanged

- [x] **Step 1: Write RED tests for all six rooms**

Assert all seven art groups exist, platform skins align to every canonical platform rectangle, interactive targets remain separate, parallax layers share the 1280×720 camera basis, foreground fades rather than hiding combat, and no `GrayboxGeometry` or debug label is visible.

- [x] **Step 2: Run RED**

Tests must fail on missing `Art/*` groups.

- [x] **Step 3: Author one reusable environment atlas and room descriptors**

Include stone edges, broken platform pieces, beams, gears, chains, pipes, railings, cloth, bells and six room-specific backdrop/atmosphere descriptors. Keep collision source-of-truth in the existing builders.

- [x] **Step 4: Build each room from shared kit plus room-specific layers**

Add moonlit courtyard, broken corridor, vertical pendulum, compressed machinery, chain chamber and Boss arena compositions. Use consistent horizon, light direction and safe traversal contrast.

- [x] **Step 5: Run GREEN and platform-drift mutation**

Offset one formal platform skin by 20 px, prove the alignment assertion fails, restore, then run all builders and tests.

- [x] **Step 6: Update manifest and commit**

```powershell
git commit -m "feat: skin all prologue rooms"
```

---

### Task 9: Formal mechanisms, checkpoint, doors, and environmental animation

**Files:**
- Create: `scripts/presentation/PrologueMechanismVisual.cs`
- Create: `tools/build/art/BuildPrologueMechanismVisuals.cs`
- Create: `scenes/presentation/GraveLampVisual.tscn`
- Create: `scenes/presentation/ThreadAnchorVisual.tscn`
- Create: `scenes/presentation/BellChainVisual.tscn`
- Create: `scenes/presentation/ShortcutDoorVisual.tscn`
- Create: `assets/environment/nameless-belfry/mechanisms.png`
- Create: `tests/integration/PrologueMechanismVisualTests.cs`
- Modify: mechanism/checkpoint helpers and builders
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Produces: `PrologueMechanismVisual.SetState(MechanismVisualState state)`
- Produces: visual states idle, highlighted, activating, active, disabled and restored

- [x] **Step 1: Write RED tests**

Require separate visual leaves for grave lamp, anchor, Bell Chain, shortcut door and Boss chain; assert Bell Chain target marker and thread endpoint resolve to the same world point; assert checkpoint/shortcut persistence drives active visuals after respawn.

- [x] **Step 2: Run RED**

Tests must fail because formal mechanism scenes are absent.

- [x] **Step 3: Author and wire mechanisms**

Use gold for available interaction, cyan for active energy, and desaturated metal for unavailable state. Animate chains/gears/doors through explicit state changes, never by reading label text.

- [x] **Step 4: Run GREEN and endpoint mutation**

Offset the Bell Chain visual target by 80 px, prove the world-point assertion fails, restore, then run tests and a deterministic neutral-K capture.

- [x] **Step 5: Commit**

```powershell
git commit -m "feat: finish prologue mechanism art"
```

---

### Task 10: VFX, camera feedback, and presentation cue coverage

**Files:**
- Create: `scripts/vfx/PrologueVfxDirector.cs`
- Create: `scripts/vfx/CameraFeedbackController.cs`
- Create: `scripts/vfx/ThreadLineVisual.cs`
- Create: `tools/build/art/BuildPrologueVfx.cs`
- Create: `scenes/vfx/PrologueVfx.tscn`
- Create: `assets/vfx/prologue-vfx.png`
- Create: `tests/unit/PresentationCueCoverageTests.cs`
- Create: `tests/integration/PrologueVfxRuntimeTests.cs`
- Modify: presentation coordinators
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Produces: `PrologueVfxDirector.Play(PresentationCue cue, Vector2 worldPosition, Vector2 direction)`
- Produces: `CameraFeedbackController.Request(CameraFeedback request)` with intensity clamping and accessibility scaling
- Produces: `ThreadLineVisual.Present(Vector2 start, Vector2 end, float tension, ThreadLineState state)`

- [x] **Step 1: Write RED cue and runtime tests**

Require explicit VFX mapping for attack, hit, hurt, guard, dash, downstrike, thread fire/hit/fail, checkpoint, shortcut, Boss transition/exposed/defeated and recovery. Assert thread endpoints equal runtime start/target, shake clamps to settings, reduced flash disables full-screen white, and no cue mutates gameplay.

- [x] **Step 2: Run RED**

Build must fail on missing VFX types.

- [x] **Step 3: Implement leaf VFX and camera controllers**

Use controlled particles, trails, line curves and shader flashes. Keep full-screen effects below the configured opacity/duration and route every effect through typed cues.

- [x] **Step 4: Run GREEN and clamp mutation**

Remove shake clamping, prove the accessibility assertion fails, restore, run full tests and capture attack/thread/Boss/recovery sequences.

- [x] **Step 5: Commit**

```powershell
git commit -m "feat: add final prologue effects"
```

---

### Task 11: Original audio library and adaptive audio director

**Files:**
- Create: `tools/audio/render_knight_audio.py`
- Create: `scripts/audio/PrologueAudioCue.cs`
- Create: `scripts/audio/PrologueAudioDirector.cs`
- Create: `assets/audio/prologue/audio-manifest.json`
- Create: generated original WAV/OGG files under `assets/audio/prologue/`
- Create: `tests/unit/PrologueAudioManifestTests.cs`
- Create: `tests/integration/PrologueAudioDirectorTests.cs`
- Modify: `project.godot`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: buses `Master`, `Music`, `SFX`, `Ambience`, `UI`
- Produces: `PrologueAudioDirector.Play(PrologueAudioCue cue, Vector2 worldPosition)`
- Produces: `PrologueAudioDirector.SetMusicState(PrologueMusicState state)`

- [x] **Step 1: Write RED manifest/director tests**

Require all five buses, exploration and two-layer Boss music, room ambience, player actions, three enemy families, Boss states, mechanisms, UI, recovery and completion cues. Assert every declared file exists, is nonempty, has a project-owned source/license, and variant selection avoids immediate repetition.

- [x] **Step 2: Run RED**

Build must fail on missing audio types and manifest.

- [x] **Step 3: Render the original audio set locally**

The renderer uses deterministic seeded synthesis and writes 48 kHz WAV masters: tuned bell partials, bowed-metal layers, wind/stone ambience, impacts, cloth, thread energy and UI ticks. Convert long loops to OGG only after seamless-loop sample checks pass; retain source parameters in the manifest.

- [x] **Step 4: Implement the audio director and settings binding**

Map typed cues to variant pools, crossfade exploration/Boss layers, apply Task 2 bus volumes, and stop/restore cleanly across pause, recovery and completion.

- [x] **Step 5: Run GREEN and missing-file mutation**

Rename one cue file, prove manifest validation fails, restore, import assets, then run tests and a runtime log/audio-duration audit.

- [x] **Step 6: Commit**

```powershell
git commit -m "feat: add original prologue audio"
```

---

### Task 12: Integrate product shell, formal presentation, save, and completion flow

**Files:**
- Create: `scripts/prologue/PrologueGameFlowController.cs`
- Modify: `scripts/prologue/PrologueLevelController.cs`
- Modify: `scripts/prologue/PrologueCompletionController.cs`
- Modify: `scripts/prologue/ProloguePauseMenu.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `project.godot`
- Create: `tests/integration/PrologueGameFlowTests.cs`
- Modify: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Produces: `PrologueGameFlowController.StartNew()`, `Continue()`, `ReturnToTitle()`, `ApplySettings()`, `Complete()`
- Consumes: Tasks 2–11 UI, save, presentation, VFX and audio interfaces

- [x] **Step 1: Write RED end-to-end flow tests**

Cover title→new game→intro→control, checkpoint save, shortcut/discovery save, corrupt save recovery, pause/settings return, death fast recovery, Boss completion, completion card, return to title, Continue, language/input changes, and no automation session recorded as human.

- [x] **Step 2: Run RED**

Build must fail on missing game-flow controller.

- [x] **Step 3: Implement explicit flow states**

```csharp
public enum PrologueGameFlowState
{
    Title, Intro, Playing, Paused, Recovering, Completed
}
```

Keep one state transition owner, persist only at checkpoints/shortcut/discovery/completion/settings changes, and reject invalid transitions with clear diagnostics.

- [x] **Step 4: Regenerate the parent scene and run GREEN**

Build UI and all formal leaf scenes first, then entities, rooms and `PrologueLevel`. Run the complete runner and critical-path smoke.

- [x] **Step 5: Invalid-transition mutation and commit**

Allow `Completed -> Recovering`, prove the named test fails, restore, then commit:

```powershell
git commit -m "feat: integrate finished prologue flow"
```

---

### Task 13: Resolution, accessibility, performance, and visible-fallback audit

**Files:**
- Create: `tests/presentation/PrologueUiResolutionCapture.cs`
- Create: `tests/presentation/PrologueVisualAudit.cs`
- Create: `tests/integration/PrologueAccessibilityTests.cs`
- Create: `tests/integration/PrologueProductionAssetRuntimeTests.cs`
- Create: `tools/audit-prologue-production.ps1`
- Modify: `tools/verify-prologue.ps1`
- Modify: `README.md`

**Interfaces:**
- Produces: exact markers `PROLOGUE_UI_RESOLUTION_OK`, `PROLOGUE_VISUAL_AUDIT_OK`, `PROLOGUE_PRODUCTION_AUDIT_OK`
- Consumes: the final manifest and all generated scenes

- [x] **Step 1: Write failing production audits**

Scan instantiated scenes for placeholder/debug visual classes, visible `GrayboxGeometry`, fallback labels, missing textures/audio, untranslated keys and invalid fonts. Capture HUD, Boss UI, pause, settings, recovery and completion at all four resolutions and assert safe bounds.

- [x] **Step 2: Run RED before final cleanup**

Expected: audit fails and names every remaining visible fallback or nonfinal manifest entry.

- [x] **Step 3: Remove every named fallback and tune accessibility**

Keep diagnostic fallbacks available only behind a development flag; final mode must fail fast. Verify UI scale, screen shake, reduced flash, guidance, language, keyboard and gamepad paths.

- [x] **Step 4: Profile the complete level**

Run a 60 FPS deterministic traversal and record frame-time spikes, draw calls, particle counts, texture memory and import pauses. Reduce offscreen particles/animation and atlas fragmentation without removing visible required content.

- [x] **Step 5: Run GREEN and audit mutation**

Temporarily re-enable `PlayerPlaceholderVisual`, prove the production audit fails by exact class name, restore, then require all three audit markers in `verify-prologue.ps1`.

- [x] **Step 6: Update README and commit**

Mark completed systems and exact remaining human playtest only. Commit:

```powershell
git commit -m "test: certify prologue production quality"
```

---

### Task 14: Final regression, proof recording, and playable handoff

**Files:**
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tools/capture-prologue.ps1`
- Create: `docs/playtests/2026-08-17-prologue-full-production.md`
- Modify: `README.md`
- Create: `.superpowers/sdd/2026-08-17-prologue-full-production/final-report.md`

**Interfaces:**
- Produces: exact markers `PROLOGUE_CAPTURE_OK`, `PROLOGUE_PRESENTATION_COMPLETE`, `PROLOGUE_VERIFY_OK`
- Produces: final proof video plus log/evidence timestamps tied to exact HEAD

- [x] **Step 1: Run the full gate twice on the exact final tree**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Require .NET 9, zero build warnings/errors, all builders, unit/integration/smoke/audit markers exactly once, semantic generated-scene stability and zero engine/script/parse/missing-resource errors.

- [x] **Step 2: Record the finished proof**

Capture title, settings, intro, HUD, checkpoint, both branches, all enemies, Bell Chain neutral K, Boss phases, one recovery, completion card and return-to-title. Require 1280×720 or higher, 60 FPS, exact completion markers, fresh nonempty output and clean capture logs.

- [x] **Step 3: Review representative frames and audio**

Inspect at least one frame from every room and UI state, the player’s main motion/attack/thread states, each enemy, Boss open/armored/thrown/exposed/defeated, and verify thread/marker endpoints. Listen for loop seams, missing cues, clipping and incorrect bus balance.

- [x] **Step 4: Commit final evidence docs only**

Record exact commit, tool versions, evidence paths, asset/license totals, automated results and the still-open human result. Do not commit proof frames or import noise.

```powershell
git commit -m "docs: certify full prologue production"
```

- [ ] **Step 5: Launch the exact verified build for the user**

Use `tools/play-prologue.ps1` only after the final commit has passed the gate. Confirm the running process/window belongs to the exact final tree, inject no input, and leave it open for the user’s 10–15 minute completion attempt.

- [ ] **Step 6: Record the real playtest and close the goal**

Read the HandsOnLauncher report only after the user finishes. If they cannot complete or report a concrete issue, reproduce, fix, rerun the full gate and relaunch. Mark the goal complete only after the complete build and human playtest meet the design’s completion definition.
