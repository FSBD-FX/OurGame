# Pause re-entry layout implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve pause geometry across live scale/language changes and same-node re-entry.
**Architecture:** Extend the real saved Main pause test; repair viewport listener lifetime only if the native test proves loss. Keep authored geometry and all existing delegates.
**Tech Stack:** Existing Godot 4.7.1 .NET/C#, canonical PowerShell headless runner.
**Spec:** docs/superpowers/specs/2026-11-08-pause-reentry-layout-design.md

## Global Constraints

- Current dirty KNIGHT tree only. No Git/worktree writes, cleanup or visible phases.
- Root owns source/build/native/canonical. One fresh read-only review after verification.
- Preserve original four baseline cases, receipts, fonts, budgets and isolated saves.
- Existing resource failures remain failures. No GC/finalizer experiment without separate approval.

## Review Focus

- Scale after SetLanguage must observe the final viewport, not the previous size: Task 1 live 1→1.5→1.
- Node ready occurs once: Task 1 two same-node remove/add cycles without RequestReady/Open.
- Re-entry must not duplicate button callbacks: Task 1 final native Resume plus original full focus cycle.
- Paused changes must not advance gameplay/save: Task 1 same-player, sample and byte guards.
- Failure modal changes language/scale without leaving screen: Task 1 actual locked isolated save, modal geometry and cancel.

## Task 1: Lifecycle behavior and proof

**Files:** tests/integration/KeyboardTeachingRuntimeTests.cs; scripts/prologue/ProloguePauseMenu.cs (only after RED); canonical verify-claw-knight-v001.ps1 (new required completion receipt only).
**Interfaces:** Existing RunHighScalePause(SceneTree), flow.ApplySettings, pause native Esc flow; produce a new PAUSE_LIVE_LAYOUT_OK receipt after all dynamic checks, preserving PAUSE_HIGH_SCALE_OK.

- [x] Append independent assertions for 1280×720/1.5 logical viewport, safe panel, five non-overlapping minimum-size buttons, visible seven-line controls, same player/input/save; run ordinary `-Phase thruster-teaching-ui -SourceRows 1912` with pinned pwsh.
- [x] Read native RED; if no defect, record coverage and make no production change. If lost resize subscription is proven, move subscription to entry lifecycle and keep matching exit unsubscription.
- [x] Add actual locked-save modal rescale check and native cancel/resume; run same full teaching-ui phase strictly GREEN. Require the new receipt in canonical alongside all old ones.
- [x] Run current same-source models, actions, ordinary pre and post once each; record all failures, no retries selecting green. Verify source/binary/user-save guards. Result 4PASS/1FAIL inclUI, not an all-green suite.
- [x] Fresh independent whole-increment review; regrade by user effect, one TDD pass for Critical/Important, defer Minor. No second review. C0/I0/M0, no fixes required.
- [x] Update production record, README/backlog and this ledger. Run existing media -Check once with normal report. Do not mark the original eleven-item goal complete. Whole-suite pre failure remains OPEN, not task-complete.
