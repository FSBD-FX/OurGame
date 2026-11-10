# Title Guidance Preference Implementation Plan

> **For agentic workers:** Use the existing test-driven execution sequence. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let keyboard players set a persistent first-level guidance default from the real title Settings menu.

**Architecture:** Use the already-saved `GameSettingsData.GuidanceEnabled` field and the ordinary Flow commit; no schema change. Generate a single settings row using the existing builder and integrate only the settings hunk. The established pause command remains temporary and the Basin sample continues to prohibit guidance.

**Tech Stack:** KNIGHT Godot/.NET, C# and saved `PrologueUi.tscn`, bilingual JSON, existing headless verifier.

**Spec:** [Full prologue design, §4.4 and persistence](../specs/2026-08-17-prologue-full-production-design.md#44-菜单与设置); [U04 and G04](../../production/2026-10-18-project-remaining-backlog-v001.md).

## Global Constraints

- Pure keyboard; use only isolated headless Godot input; no actual user saves, new media, external writes, or engine changes.
- Keep the old settings, focus order and geometry contracts; use scrolling on the existing high-scale path, not smaller type or hidden content.
- Keep pause override session-local. Never expose or accept the choice in the Basin quality sample.

## Review Focus

- Check the persisted default off and on independently from other settings, and distinguish it from pause-only override.
- Check Chinese and English text, focus and row fit at the actual existing 100% and 150% 720p cases.
- Check the sample cannot focus or submit the setting even if the loaded preference is on.
- Check the original main-menu and Controls leaf bytes do not change.
- Ordinary Front/Post/route and normal-speed visual acceptance remain separate from this local test.

## Task 1 · Add the approved title option

- [x] In `tests/integration/KeyboardTeachingRuntimeTests.cs`, exercise the saved title Settings `Guidance/Value` with keyboard Space and Back. Observe the isolated persisted value, reopened UI and the actual guidance consumer; retain the old tests. Run `verify-claw-knight-v001.ps1 -Phase thruster-teaching-ui -SourceRows 1901` with the original scene, expect a missing real guidance row.
- [x] Add `CheckRow("Guidance")` after `ReducedFog` in `tools/build/ui/BuildPrologueUi.cs`; reopen the staged generated leaf and validate the row. Increase only the normal Settings panel height enough to keep all 14 controls at 100%. Merge only generated Settings nodes and leave the parent scene and other menus unchanged.
- [x] Add the title option to `scripts/ui/PrologueSettingsMenu.cs`, `scripts/ui/PrologueUiRoot.cs` and `assets/localization/prologue-ui.json`, using `settings.guidance` in both locales. Hide and remove the control from focus in the sample. Commit the preference through the ordinary Flow settings path; keep the pause button unchanged.
- [x] Verify the original exact menu gate, the new regression and the sample using `thruster-teaching-ui` and `basin-sample-lifecycle`; run ordinary regression as in-scope evidence. Keep distinct no-window and final experience claims. Preserve raw failures; no local commit, publish or overwrite of historical evidence. [Actual results and limits](../../production/2026-11-06-title-guidance-preference-v290.md).
