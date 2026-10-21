# Task 3 report — compact prologue HUD and contextual guidance

## Scope and result

- Replaced the separate top-left health/thread blocks with one compact 368×72 instrument row at the 1280×720 logical canvas. Health retains five independent shards; the mechanical-thread spool retains typed state ownership and now uses cooldown ring, center shape, color, and a non-overlapping text label as independent channels.
- Reduced the ordinary objective to one localized Chinese line. A changed objective expands for 2.25 seconds, restarts on rapid replacement, then remains collapsed through routine health/thread refreshes.
- Replaced the fixed guidance rectangle with target-adjacent compact prompts placed outside the Task 1 player/enemy/thread-target/attack protected regions. Expanded first-level teaching appears only for the initial trigger, repeated failure, or manual re-enable and automatically collapses after four seconds.
- Removed raw numeric target distance from the teaching card and made the formal first-level objective language consistent with the Chinese contextual guidance.
- Kept the Boss bar in a 62-pixel bottom safe band and added a reusable protected-region audit. Controls, collisions, traversal, damage, checkpoints, room routes, enemy/Boss timing, and Task 1/2 art were not changed.
- No paid generation was used.

## Strict TDD evidence

- Compact health/thread cluster RED: `task-3-red-compact-cluster.log`.
- Collapsed objective RED: `task-3-red-collapsed-objective.log`.
- Objective transition API RED: `task-3-red-objective-transition-time.log`.
- Mounted UI-scale RED: `task-3-red-mounted-ui-scale.log`.
- Protected target prompt RED: `task-3-red-protected-target-prompt.log`; first implementation correctly exposed a real overlap in `task-3-green-ui-scale-protected-prompt.log`, then passed in `task-3-green-ui-scale-protected-prompt-2.log`.
- First-level expanded teaching and old fixed-block REDs: `task-3-red-first-level-expanded-teaching.log` and `task-3-gchandle-full-repro-2.log`.
- Automatic teaching collapse RED: `task-3-red-guidance-auto-collapse.log`.
- Shortcut first-observable-frame RED: `task-3-red-shortcut-first-frame-repeatability.log` observed stale `MechanismBellChain` at the actual `BellChainMechanism.Opened` signal.
- Shortcut GREEN: `task-3-green-shortcut-first-frame-repeatability.log` passes three fresh level instances, each observing `MechanismBossExit` on the opening signal.
- Typed spool/glyph RED/GREEN: `task-3-red-runtime-hud-states-glyphs.log` and `task-3-green-runtime-hud-states-glyphs.log`.
- Running-capture RED: `task-3-red-running-ui-captures.log`; the first windowed run then exposed the real routine-refresh objective reopening defect before final GREEN.
- Visual-review RED: `task-3-red-visual-review-polish.log` rejects the raw distance suffix before the language/label correction.

## Systematic debugging evidence

- The Mono `gchandle.is_released()` fatal recurred in `task-3-teardown-full-pass1.log`, so the earlier one-off diagnosis was rejected. `task-3-guidance-focused-probe.log` then reproduced the fatal inside the Task 3 Guidance suite after six drained fixtures, independently of the earlier runtime suites.
- Phase-1 boundaries showed that every fixture drained and freed its visible scene, but `LoadFixture` created a new temporary C# `PackedScene` wrapper around the same cached native resource on every call. GC/finalizer timing could therefore release a stale wrapper handle while the repeated scene suite was still running. The minimal test-harness fix lazily loads and strongly retains one `PackedScene` wrapper while continuing to audio-drain and free every instantiated level.
- `task-3-guidance-cached-probe-1.log` and `task-3-guidance-cached-probe-2.log` each pass two Guidance rounds in one process with forced GC between rounds: four clean focused rounds, zero fatal/error markers. The temporary runner and diagnostic boundary prints were removed before the final build.
- The separate deterministic `ArgumentException: '48' cannot be greater than -48` was root-caused to a zero-sized `Guidance` parent feeding an inverted clamp range. The initial 3.67 MB run was terminated; `Guidance` and `VisualRoot` now fill the viewport and drawing guards sub-96-pixel sizes. `task-3-green-expanded-teaching-large-block-2.log` and later full suites are clean.

## Named Boss overlap mutation

- `task-3-mutation-boss-upward-overlap.log` temporarily raises the production Boss bar 72 logical pixels into the protected combat-floor region.
- Exit `1`, zero success markers; named failure: `Boss bottom-safe-area audit accepts the production bar above protected combat floor`.
- The production expression was restored with a scoped patch before the final build, full suite, and running captures.

## Running proof and resolution/accessibility gates

- Final capture log: `task-3-green-final-running-ui-captures.log` — exit `0`, `PROLOGUE_UI_RESOLUTION_OK`, OpenGL/NVIDIA renderer.
- Proof root (not committed): `screenshots/task3-compact-hud/`.
- Actual running states at 1280×720: `ordinary-keyboard.png`, `ordinary-controller.png`, `expanded-teaching.png`, `repeated-failure-retry.png`, `boss-bottom-safe.png`, `ui-scale-150.png`, and `reduced-flash-two-channel.png`.
- Ordinary, expanded-teaching, and Boss states were additionally captured at 1920×1080, 2560×1440, 3840×2160, and supported 2560×1080 widescreen.
- The proof fixture uses the project's real 1280×720 `canvas_items` logical stretch through `SubViewport.Size2DOverride`; this corrected the earlier fixture-only 4K grey border/tiny-HUD artifact. The final 4K and widescreen frames fill the render target and retain readable scaled HUD.
- Runtime assertions cover safe margins, menu containment, compact ordinary footprint, protected prompt placement, automatic collapse, keyboard `[K]`/controller `[Y]`, UI scale 1.5, reduced full-screen flash, typed text+shape failure feedback, and Boss/combat-floor separation.

## Final gates

- Fresh build: `task-3-final-build-after-handle-fix.log` — exit `0`, 0 warnings, 0 errors.
- Consecutive full suite pass 1: `task-3-handle-fixed-full-pass1.log` — exit `0`, exactly one `PROLOGUE_TESTS_OK`, 23 audio drains, zero `FATAL:` and zero `ERROR:` lines.
- Consecutive full suite pass 2: `task-3-handle-fixed-full-pass2.log` — exit `0`, exactly one `PROLOGUE_TESTS_OK`, 23 audio drains, zero `FATAL:` and zero `ERROR:` lines.
- Both full runs contain the same five intentional warning-path probes (invalid presentation fallback, missing guidance target, and rejected completed-to-recovering flow); none is a build warning or unhandled error.

## Owned files

- `scripts/ui/PrologueHudViewModel.cs`
- `scripts/ui/PrologueFormalHud.cs`
- `scripts/ui/PrologueThreadSpoolVisual.cs`
- `scripts/ui/ProtectedPromptPlacer.cs` (new reusable deterministic placer)
- `scripts/prologue/guidance/PrologueGuidanceController.cs`
- `scripts/ui/WorldTargetMarkerVisual.cs`
- `tools/build/prologue/BuildPrologueLevel.cs`
- `scenes/prologue/PrologueLevel.tscn`
- `tests/unit/PrologueHudViewModelTests.cs`
- `tests/unit/ProtectedPromptPlacerTests.cs` (focused placer test)
- `tests/integration/PrologueFormalHudTests.cs`
- `tests/integration/PrologueGuidanceRuntimeTests.cs`
- `tests/presentation/PrologueUiResolutionCapture.cs`

## Concerns

- No open Task 3 blocker. Both deterministic teardown faults were reproduced, root-caused, and covered by focused plus consecutive full-suite evidence. The retained `PackedScene` is test-harness-only and does not alter production scene/resource ownership.

## Fix round 1/5 — proportional UI scale and real Boss-floor clearance

### Reviewer findings resolved

- `SetUiScale(1.5)` now applies one production content scale to the mounted health shards and spacing, thread-spool internals, objective/rule line, prompt glyph/text/rule line, and Boss name/state/health/rule line. Runtime containment assertions cover the real child controls, not only their parent rectangles.
- The production Boss bar now clears the measured visible `FallenBellkeeperArena/Art/Platforms/ArenaFloor` by eight logical pixels. Its rule line is sized from the real 640-pixel parent instead of the former 760-pixel fixed width.
- The capture fixture transitions through the production `FallenBellkeeperArena` entrance, starts the real `BossArenaController`, reads the live `ArenaFloor` polygon and Task 1 protection regions, and rejects either child overflow or combat-floor overlap. The previous synthetic Courtyard rectangle was removed.

### RED / GREEN evidence

- Runtime content-scale RED: `task-3-fix1-red-runtime-content-scale.log` — mounted 1.5× health shards remained `190×42` instead of the expected scaled geometry.
- Runtime content-scale GREEN: `task-3-fix1-green-runtime-content-scale-3.log` — real shard geometry/spacing, objective/prompt/Boss fonts and rules, and child containment pass at 1.5×.
- Real Boss child RED: `task-3-fix1-red-real-boss-floor.log` — in the production Boss room, `TopRule` left its parent as `(320, 618), (760, 2)`.
- Real Boss-floor RED: `task-3-fix1-red-real-boss-floor-overlap.log` — after isolating rule containment, the old bar position intersected the measured visible floor `(0, 630), (1280, 40)`.
- Final running GREEN: `task-3-fix1-green-final-running-ui-captures.log` — `PROLOGUE_UI_RESOLUTION_OK`, zero `ERROR:`/`FATAL:` lines across 1280×720, 1920×1080, 2560×1440, 3840×2160 and 2560×1080.

### Named real-floor mutation

- `task-3-fix1-mutation-real-boss-floor-overlap.log` temporarily expands the production Boss panel from 62 to 86 logical pixels without moving its top edge.
- Exit `1`, zero success markers; named failure: `real Boss HUD overlaps visible FallenBellkeeperArena combat floor: hud=(312, 552), (656, 102) floor=(0, 630), (1280, 40)`.
- The production height was restored with a scoped patch, rebuilt, and the complete running capture passed again.

### Visual proof and final gates

- Proof root, not committed: `screenshots/task3-compact-hud/`.
- Manually reviewed: `1280x720/ui-scale-150.png` (all content scales together; mechanical-thread label, shards, objective and prompt remain separated), `1280x720/boss-bottom-safe.png` (real Boss encounter and visible floor), and `3840x2160/boss-bottom-safe.png` (project stretch fills the target and HUD remains readable).
- Focused Guidance: `task-3-fix1-guidance-focused.log` — one complete Guidance suite, 19 audio drains, three fresh shortcut-opening attempts, `TASK3_FIX1_GUIDANCE_PROBE_OK`, zero `ERROR:`/`FATAL:`. The temporary runner was removed before the final build.
- Fresh build: `task-3-fix1-final-build.log` — exit `0`, 0 warnings, 0 errors.
- Consecutive full passes: `task-3-fix1-full-pass1.log` and `task-3-fix1-full-pass2.log` — each has exactly one `PROLOGUE_TESTS_OK`, 23 audio drains, zero `ERROR:` and zero `FATAL:`.
- No gameplay, collision, traversal, tuning, Task 1/2 art, or Task 4 audio ownership changed; no paid generation was used. No open Fix Round 1 concern.
