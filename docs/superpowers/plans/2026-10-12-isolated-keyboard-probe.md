# Isolated keyboard probe plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Open the exact formal scene at Title with no input driver, safely observe OS-tool keyboard pulses and normal pause/action responses without touching real saves.

**Architecture:** Reuse `IsolatedPrologueFixture` before AddChild to bind save/settings/recorder and legacy sibling to a unique external case. A test-only SceneTree owns a passive observer,120s monotonic observation window and audio teardown; a PowerShell owner bounds build180s and engine150s, retains native Process handles, inventories and exact fresh binary pairing. This is not the Main/Explore route proof or human input.

**Tech Stack:** Installed Godot4.7.1 Mono/net9.0, PowerShell7, existing isolated fixture and process-identity helper, documented node_repl/sky OS keyboard chord API.

**Spec:** Active full goal and project AGENTS.md; current-route-revalidation/result-v001.md identifies the unproved OS seam. No shared gameplay redesign authorized.

## Global constraints

- Existing linked dirty worktree/HEAD preserved; no commits/deletion/unsafe aggregate/paid assets/automation/publication.
- Protected actual userdata109 rows/104 files unchanged. No hands-on grant/source environment; evidence stays automated-not-human.
- Existing1593 sources plus one new runner =1594; only test source added, product bytes unchanged.
- No fixed FPS/movie/headless for live probe; no forced foreground API, automatic key reinjection, pose/velocity override, input assertions relaxed or route retry. UI input methods themselves target/activate the selected window; record that distinction.
- Build180s, live engine150s; inner120s wall-clock observation plus normal audio teardown. A separate headless contract-only mode must never mount a scene.
- Computer-use screenshots disabled: no new media displayed without prior project retention. Key pulses are not controlled key holds, natural death or full-route evidence.

## Review focus

- Settings writes happen on Title: all channels must be asserted external before the first AddChild.
- Failed isolation or invalid args must never mount the formal scene.
- Paused game still reaches bounded teardown; original process ownership, not PID lookup/kill.
- Observer must never handle, inject or alter gameplay input/pose/focus/state. UI target activation is not a fix for old automated focus failures.
- Failed runtime must still preserve complete logs, source/user guards and freshly built binary identities; no success-branch-only identity audit.

### Task1: Test-only isolated no-driver runner and bounded owner

**Files:** create `tests/RunIsolatedKeyboardProbe.cs`; create `.superpowers/sdd/2026-10-12-isolated-keyboard-probe/verify-probe.ps1`, `progress.md`; update README status only after evidence.

**Interface:** runner accepts exactly `--case-storage-root=<absolute>` and optional `--contract-only`; no other args. `CreateFixture()` loads exact `PrologueLevel.tscn`, binds via existing fixture helper, disables both auto-start flags. Contract-only checks actual off-tree destinations, zero settings/save writes and authored spawn; live observes Title then external keys only.

- [x] Write off-tree storage assertions with unbound CreateFixture, build bounded, run headless contract-only and observe named external-path RED before any mount. Original timeout-admission bug retained/qualified.
- [x] Bind via existing helper; rerun contract-only to GREEN; retain both binary/source revisions independently.
- [x] Independent read-only safety review; fix important defects before live launch. Actual wait3 + exact synthetic fault4 tests; runner failure-cleanup fixes reviewed, not mounted fault-injection proof.
- [x] Run one live120s passive case; use documented OS key pulses with fresh returned window and no screenshot; inspect input/state/player evidence, every failure and all guards. Diagnostic completed, **gameplay acceptance not passed**: logical letters/physical Pause mismatch, Esc after window end.
- [x] Independently audit retained artifacts; update README/ledger with exact limits, not whole-goal acceptance.1594source/109user/10helper/current+retained6/5077rows verified; READY window119595ms, tool timeline secondary, not route/human/FPS proof.

Preflight: runner consumes case storage from owner; owner expects1594 source rows and one terminal sidecar, not a whole-route PASS. No change to the old route owner's1593/build admission or original limits. No plan-approval stall for this in-scope safety implementation; new gameplay designs still need human approval.
