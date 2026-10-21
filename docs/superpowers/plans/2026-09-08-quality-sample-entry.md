# Basin quality sample entry implementation plan

> For agentic workers: use superpowers:subagent-driven-development. Track evidence against the task BASE; never absorb unrelated dirty files.

**Goal:** Provide an explicitly unfinished, independent Basin sample entrance with a truthful title, isolated saves/settings, and correct immediate new-game/continue/death recovery. This is the foundation for the approved sample, not completion of its gameplay or art.

**Architecture:** A small production entry node configures an instance of the existing PrologueLevel before adding it to the tree. A C# SceneTree builder saves this wrapper scene. The full default scene and all five sector definitions remain intact. Authored initial Basin starts activate the real initial checkpoint before persistence. An opt-in sample mode prevents full-prologue objective/completion claims and rejects unrelated checkpoint saves; existing full-mode behavior remains the default.

**Tech Stack:** Existing Godot 4.7.1 Mono, C#, .NET SDK 9; packed production scenes and existing real input/lifecycle integration patterns. No new dependency or paid asset.

**Spec:** `docs/production/2026-09-08-quality-sample-brief.md`, execution approved on 2026-09-08 by “设立目标并进行”.

**Global constraints:** Active worktree E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555. Root alone runs build/Godot/ffmpeg strictly serial, including worker test requests. Preserve pre-existing camera RED drafts, importer noise, audio bus and unrelated files. Do not modify project.godot, default map, physics, controls, save format/version or existing save files. Use apply_patch for manual edits; generate .tscn via builder. No remote push/tag/release for this small development increment. Do not claim the sample loop, aesthetics, 5–8 minute length or commercial target is verified.

## Task 1: Authored Basin entry and isolated lifecycle

**Ownership / files:**

- Create `scripts/prologue/sample/BasinQualitySampleEntry.cs` (production wrapper and fixed configuration).
- Create `tools/build/prologue/BuildBasinQualitySample.cs`, generating `scenes/prologue/sample/BasinQualitySample.tscn`; use the existing SceneBuildUtil in its discovered namespace/path and verify packing.
- Modify only the necessary lines in `scripts/prologue/PrologueLevelController.cs`, `scripts/prologue/PrologueGameFlowController.cs`, `scripts/prologue/PrologueCompletionController.cs`, `scripts/ui/PrologueUiRoot.cs`, `scripts/ui/PrologueMainMenu.cs`.
- Create `tests/integration/BasinQualitySampleEntryTests.cs` and `tests/RunBasinQualitySampleTests.cs`; do not edit existing RunPrologueTests registration, which contains an unfinished prior camera RED.
- Root owns README, automation status, this plan, test execution, evidence and final commit. Worker owns code/tests and self-review report only; no other agents or commands that run engine/compiler.
- Root also owns `tests/presentation/BasinQualitySampleEntryProbe.cs`, `tools/play-basin-sample.ps1` and `Play-Basin-Sample.cmd`: narrow automated entrance proof and a local development launcher, not a standalone distributable or human-evidence grant.

### Required behavior

1. Production entry instantiates `res://scenes/prologue/PrologueLevel.tscn`, names the child `Level`, and before AddChild sets WorldRoot/Sectors.InitialSector=CanyonBasin, PlayerRoot/Player.Position=(2380,2014), flow sample mode=true, both auto-start flags=false, save/settings paths under `user://knight/basin-quality-sample/`. Default main scene remains unchanged.
2. Sample title stays visible before actual Start; settings/controls/back and language changes retain an explicit Chinese/English “Basin quality sample · in development” label. No one-time Label hack that Present overwrites.
3. `StartNewGame()` after resetting world and SetStartPosition uses the existing `Progress.ActivateCheckpoint(initialSector, initialPosition)` for an authored initial Basin/LostTemple sector. Entrance stays unactivated exactly as today. Persistence from normal Start must already contain the Basin checkpoint before any physics frame; do not depend on Checkpoint Area collision or construct a fake RestoreSession/RestoreFromSave on new game.
4. Sample mode disables the full objective observation/display chain and final full-prologue completion. Present a short, truthful static developer sample objective (camp → battle → grapple supply platform → return; loop/visual polish in development) instead of advancing to Temple/Boss. Disable old full-route guidance for this increment, including after settings apply. Keep input/HUD/pause usable. This static objective is temporary, not a validated completion tracker.
5. Sample Continue is offered only for a valid activated Basin checkpoint within the authored Basin world bounds, without Boss/Completed/full objective completion claims. A valid but unrelated Temple save must not be loaded or silently overwritten on title opening. New Game may replace only its own isolated slot. Default full-mode Continue is unchanged.
6. Normal sample Start, death recovery, ReturnToTitle, restart Continue and New Game must use existing production lifecycle. Recovered/new runs do not set full-prologue completion, Boss victory or human completion metrics. Stage does not yet restrict geography or implement sample completion; document that limitation.
7. Before entering the tree, expose wrapper `StorageDirectory` for isolated test slots, freeze world simulation while on the title, and isolate or omit the existing full-run PlaytestRecorder so sample sessions cannot overwrite full-run latest reports. Add a sample-only `PauseMenu/Panel/ReturnToTitle` button in the free second-row space, using Resume then ReturnToTitle; hide the old GuidanceToggle in sample mode. Localize that button and restore the temporary sample objective after recovery. These are required player-facing lifecycle details discovered during source inspection, not a wider pause-menu redesign.

### TDD / execution steps

- [ ] Write a focused RED on existing APIs: instantiate the full production level, author InitialSector=Basin and initial player position before AddChild, disable auto-start, use a unique test save path, emit the actual Start button signal, and immediately assert Progress.RestoreCheckpoint() is Basin at (2380,2014) before yielding a physics frame. This diagnostic fixture configures an authored start; it is not proof of the eventual production sample route. Existing code should fail because it records Entrance.
- [ ] Ask root to build and run `--headless --path <worktree> --script tests/RunBasinQualitySampleTests.cs`. Root records genuine behavioral RED; compilation or missing scene failure does not count. Keep the test runner initially limited to the authored-start case until the production entry exists.
- [ ] Implement the initial checkpoint fix, production entry, builder and opt-in sample semantics above. Do not add abstraction layers or redesign the full objective/save system. Expose only narrow exported properties as needed; defaults must preserve full mode.
- [ ] Extend integration tests to load the generated production sample wrapper (not a test teleport): title and actual Start; immediate checkpoint; real Health death/async recovery; return/unload/reload actual Continue; persisted discovery retention and New Game reset; invalid Temple and out-of-bounds sample saves; distinct save/settings paths; normal save sentinel untouched; Chinese/English menu navigation; sample no full-completion; default main still Entrance. Use real state and disk persistence, no manually injected completion for claimed route proof.
- [ ] Root runs build, builder, focused sample suite and existing game-flow/save regressions. Full post suite once before committing when feasible; pre cannot be claimed green while the known prior camera RED remains registered. Record unrelated baseline failures separately without deleting tests.
- [ ] Root runs an ordinary non-fixed-fps sample window using its scene, and an automated short entry/start/return-title proof, then records and reviews a separate short movie. Proof covers only the entrance/lifecycle, not the complete circle or human usability.
- [ ] Worker self-reviews exact owned diff, writes `.superpowers/sdd/2026-09-08-quality-sample-entry/task-1-report.md` with changes, RED/GREEN evidence supplied by root, risks and status. Root obtains fresh review, fixes actionable findings, commits only allowlisted files after validation. No engine/build by worker.

### Commands / evidence

Root sets DOTNET_ROOT/DOTNET_ROOT_X64 to G:/godot/dotnet-sdk, DOTNET_ROLL_FORWARD=LatestMajor and prepends SDK path for each process. Build: `G:/godot/dotnet-sdk/dotnet.exe build --no-restore -t:Rebuild`. Engine: `G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe`. Builder `--headless --script tools/build/prologue/BuildBasinQualitySample.cs`; focused runner `--headless --script tests/RunBasinQualitySampleTests.cs`. Evidence root E:/OurGame/Knight/.evidence/september08-quality-sample-entry. Do not start a second command while any Godot/compiler/movie process from the first remains live.

## Follow-on work, not completed by Task 1

The natural left-side encounter → mechanical-claw supply ascent → return loop still needs an end-to-end baseline and a proper sample objective/reward/ending contract. Enemy isolation, scene composition, original/free art workflow, animation, UI/sound refinement, standalone packaging and user quality review remain active-goal work. Do not publish this foundation as the approved finished sample or call a static objective a completed loop.

Next gameplay increment should address the known two-sided approach: author a separate sample Basin using `PrologueCanyonBuildParts.Build(CanyonBasin)`, remove only `Gameplay/Actors/RustShellSquire` before packing, and replace only a duplicated Basin definition's Scene in the sample entry. Do not mutate the shared full-mode definition resource or hide an enemy every frame. Preserve Laborer/terrain/claw/supply/checkpoint values first. Check one Laborer/no Rust in sample and original actors in default, across Start/death/Continue/unload; then run the natural entire loop without health injection. Removing Rust eliminates a pincer, not all camp danger: the Laborer still detects from about 270px away (range420), so whether a player can observe safely needs separate actual validation. This next increment is not implemented by the current entry task.
