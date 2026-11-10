# Controls high scale Implementation Plan

> Inline execution under the user's established direct workflow; no commits or cleanup. Use test-driven-development and verification-before-completion.

**Goal:** a keyboard player can read the entire saved title reference and leave at 1280×720 / 150% without reducing scale.
**Architecture:** only the controls panel responds to actual viewport height; fixed heading/back outside a clipped vertical body; standard keyboard keys in the visible page only. C# builder owns authored scene.
**Tech Stack:** Godot 4.7.1 .NET / C#; existing isolated normal runner.
**Spec:** [v270 design](../specs/2026-11-06-controls-high-scale-v270-design.md).

## Global Constraints

- `G:/knight/Knight` only. No window, real user saves, OS keys, publish, new media, cleanup or unrelated changes.
- Preserve current binding resolver, language/device switch, panel style and exact visible default text. Do not change Root scaling or settings.
- Normal renderer/visual/hardware acceptance remains separate.

## Review Focus

- Actual 150% logical rect and high-scale Back before keyboard action; first and last actual label geometry, stable fixed chrome.
- Live wrapping and default compact layout in both languages, with font sizes unchanged and separate existing remap checks.
- Actual scoped keyboard scroll and Enter path, no test-only position write; reopen from top.
- Scene-builder output and untouched Main/Settings structural content; don't publish unverified staging.
- Exact 1898 inventory/version and strict original diagnostics, no marker waiver.

### Task 1: responsive title reference

- [x] In `tests/integration/KeyboardTeachingRuntimeTests.cs` mount the isolated saved level. Apply settings at 1280×720/1.5 and require the actual ≈853×480 logical size. Open real Controls and first assert Back and title are within the actual screen, with Back focused. Observe named RED with ordinary `thruster-teaching-ui -SourceRows 1898`.
- [x] Implement title panel clamp and native scroll in `scripts/ui/PrologueControlsView.cs`, `tools/build/ui/BuildPrologueUi.cs`, generated `scenes/ui/PrologueUi.tscn`, and bilingual hint. Preserve typed scene path consumers and stable Back. Stage only the UI output and verify exact intended diff before publishing in the worktree.
- [x] Assert actual PageDown/PageUp/arrow offsets, complete last-row visibility, fixed Back/title, original fonts and physical Enter returning to title; both languages, no hard-coded test position. Rerun original controls/remap, runtime, and model gates. Evidence only as strong as actual results.
- [x] One independent scope review; run final verification, update README/backlog/evidence. Do not close G05 or full goal from this subset.

Scoped [evidence](../../production/2026-11-06-controls-high-scale-v270.md): high-scale and related gates pass; full post gate remains failed, not a release approval.
