### Task 10: Final HUD, Reticle, Audio/VFX and Replacement-Slot Migration

**Files:**
- Create: `scripts/ui/PrologueActionModeVisual.cs`
- Create: `scripts/ui/PrologueEnergyHeatVisual.cs`
- Create: `scripts/ui/PrologueLoadoutVisual.cs`
- Create: `scripts/ui/PrologueAimReticle.cs`
- Modify: `scripts/ui/PrologueFormalHud.cs`
- Modify: `scripts/ui/PrologueControlsView.cs`
- Modify: `scripts/ui/InputGlyphResolver.cs`
- Modify: `tools/build/ui/BuildPrologueUi.cs`
- Modify generated output: `scenes/ui/PrologueUi.tscn`
- Modify: `scripts/audio/PrologueAudioCue.cs`
- Modify: `scripts/audio/PrologueAudioDirector.cs`
- Modify: `scripts/vfx/PrologueVfxDirector.cs`
- Modify: `scripts/presentation/ProductionAssetManifest.cs`
- Modify: `scripts/presentation/RoomVisualCatalog.cs`
- Modify: `tools/build/art/BuildPrologueVfx.cs`
- Modify: `tools/build/art/BuildPrologueEnvironmentKit.cs`
- Modify: `scripts/shared/InputActions.cs`
- Modify: `tools/configure/ConfigureProject.cs`
- Modify: `tests/unit/InputGlyphResolverTests.cs`
- Modify: `tests/unit/PrologueHudViewModelTests.cs`
- Modify: `tests/unit/PrologueAudioManifestTests.cs`
- Modify: `tests/unit/ProductionAssetManifestTests.cs`
- Modify: `tests/unit/PresentationCueCoverageTests.cs`
- Modify: `tests/integration/PrologueFormalHudTests.cs`
- Modify: `tests/integration/PrologueUiStructureTests.cs`
- Modify: `tests/integration/PrologueUiRuntimeTests.cs`
- Modify: `tests/integration/PrologueAccessibilityTests.cs`
- Modify: `tests/integration/PrologueAudioDirectorTests.cs`
- Modify: `tests/integration/PrologueVfxRuntimeTests.cs`
- Modify: `tests/integration/PrologueProductionAssetRuntimeTests.cs`

**Interfaces:**
- Consumes all runtime frames from Tasks 4, 6 and 9.
- Produces shape-plus-color reticle states `Attack`, `Grippable`, `Ice`, `Obstructed`, `OutOfRange`, `Retracting`.
- Produces typed cues `ModeAttack`, `ModeGrapple`, `EmptySlot`, `EnergyEmpty`, `ShieldRaise`, `ShieldBlock`, `ShieldHeavyBreak`, `ShieldOverheat`, `ShieldReady`, `ClawFire`, `ClawTravel`, `ClawLatch`, `ClawTension`, `ClawRelease`, `ClawIceSlip`, `ClawFail`.
- Produces stable visual roots required by the spec for HUD/player/sectors.

- [ ] **Step 1: Write UI structure and resolution RED tests**

Require every stable path and non-overlap at 1280×720, 1920×1080, 2560×1440 and 3840×2160. Each reticle state must differ by geometry/line style as well as color. Heat over 100 and ratios outside `[0,1]` are clamped.

- [ ] **Step 2: Write typed audio/VFX RED tests**

Trigger every cue from a real runtime transition. Require exactly one cue at transition entry, no cue polling spam, correct audio bus and complete VFX cleanup on death, pause, sector unload and completion.

- [ ] **Step 3: Write replacement-boundary RED tests**

Require stable HUD/player/sector paths, gameplay collision outside all `VisualRoot` subtrees, no placeholder script owning combat state, and a production manifest entry for each replaceable slot. Removing or moving one named slot must fail the structural gate.

- [ ] **Step 4: Implement focused UI controls and reticle**

Each control consumes an immutable view-model frame and draws only its own layer. The reticle follows screen-projected world cursor and uses open circle/diamond/crossed ice bars/dashed obstruction arc/range ring/retract spiral for the six states.

- [ ] **Step 5: Add deterministic placeholder cues and VFX**

Use existing fixed-seed synthesis and audio manifest conventions. Keep peak below -0.9 dBFS and captured mix below the current limiter/headroom gates. Add shield fan, block flash, heavy-break ripple, overheat vent, claw material hit and rope tension effects under replaceable roots.

- [ ] **Step 6: Remove old controls and stale text**

Erase formal `attack`/`thread` J/K actions from InputMap, remove their runtime readers, update pause controls and localization, and scan source/generated scenes for player-facing `J`, `K`, old thread cooldown and route-choice text. Retain legacy class names only where adapters need them, not as active input.

- [ ] **Step 7: Run GREEN**

Build UI/VFX/environment/player/level, run prologue tests and four-resolution capture. Require exact cue coverage, stable paths, accessibility and zero stale-control matches in player-facing assets.

- [ ] **Step 8: Commit**

```powershell
git commit -m "feat: finish canyon controls feedback and ui"
```

## Controller context and rulings

- Task 9 is COMPLETE and independently review-clean: Critical 0 / Important 0 / Minor 0 / Evidence gaps 0. Task 10 is now in progress from `f9246a7fd6e010f39be0ba9b780b955f83057149`.
- Resolve the current compact, mixed-language `FormalHud` debt. English and Chinese presentation must each remain coherent, accessible, and mutually consistent without mixing player-facing languages inside a selected locale.
- Preserve the continuous-guidance protected regions established in Task 9. New HUD frames and reticle behavior must not cause guidance cards or world-target labels to overlap the health/resource/objective/prompt/mode/loadout/Boss regions at any required resolution.
- No paid generation or formal art is authorized. All Task 10 audio/VFX/visual work must be deterministic, replaceable placeholder production under the existing no-paid-generation boundary.
- Preserve the Task 6 stable player replacement roots `Player/VisualRoot/Shield` and `Player/VisualRoot/MechanicalClaw`, and the Task 8 sector roots `VisualRoot/{Background,Midground,GameplayReadable,Foreground,Effects}`. Gameplay collision, surface semantics, damage, mechanisms, checkpoints, and authoritative state stay outside every `VisualRoot` subtree.
- Treat all six reticle states as exact shape-plus-color contracts; the geometric/line-style semantics named by Step 4 are not color-only suggestions.
- Typed audio/VFX are edge-triggered presentation consumers: exactly once on state entry, never per-frame polling spam, and fully cleaned on death, pause, sector unload, completion, and re-entry.
- Replacement slots require a fail-closed manifest and stable-path contract. Mutation tests must prove removal/move, missing manifest coverage, gameplay collision under a visual root, or a placeholder owning combat state fails the gate.
- Remove active `J`/`K` InputMap actions and runtime readers plus player-facing stale cooldown/route-choice text, while preserving adapter-only legacy class/type names where the current compatibility layer still requires them.
- The protected user worktree copy of `scenes/prologue/PrologueLevel.tscn` must remain byte-identical at SHA-256 `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`. Required generated UI/VFX/environment/player/level outputs may be staged through exact index-isolated generation, followed by immediate restoration of the user copy and a final empty cached diff for that protected path unless the exact generated main blob is intentionally owned and documented.
- Preserve `README.md`, unrelated dirty/import/artifact files, and all user work. Any production path beyond the 30 planned files or a direct test runner/generated-output dependency requires a controller scope ruling before edit.
- Execute strict RED → observed named failure → minimal GREEN → focused regression. Use real immutable runtime frames, InputMap transitions, generated scenes, cleanup edges, and fail-closed mutations rather than source-text-only or assertion-only substitutes.
- Final evidence must include fresh exact build/import/Prototype/pre/post/focused gates, four-resolution Windows GPU captures, audio peak/headroom checks, typed cue/VFX runtime and cleanup proof, replacement-boundary scans/mutations, player-facing stale-control scan, task-owned process leak audit, protected SHA restoration, and scoped index/commit audit.
- Commit implementation/tests/generated outputs first, then append a tracked task report and progress handoff in a separate documentation commit. Task 10 remains open until an independent spec-and-quality reviewer returns APPROVE; maximum five bounded fix rounds.
