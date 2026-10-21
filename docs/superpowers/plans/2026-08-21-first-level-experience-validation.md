# Complete First-Level Experience Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate the attack, thread, action, environment, HUD, VFX, and audio milestones into one stable first-level candidate; prove it automatically and visually; then use a fresh human playthrough to correct difficulty and sensory problems before declaring completion.

**Architecture:** This plan changes no core subsystem until Plans 1–4 are integrated. It strengthens the existing full-route runner, deterministic presentation/capture, production audit, and hands-on recorder around their public contracts. Automated proof and human evidence remain separate; tuning changes return to the owning centralized configuration and its tests.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, existing 20-builder prologue gate, deterministic SceneTree drivers, PNG/AVI proof capture, project-owned playtest telemetry, PowerShell orchestration, README and playtest audit documents.

**Spec:** [Combat readability and presentation design](../specs/2026-08-21-combat-readability-presentation-design.md)

## Global Constraints

- Execute after the four subsystem plans in this order: attack readability, mechanical thread, action motion, environment/UI/audio polish.
- Preserve the distinction between deterministic automation and `HandsOnLauncher`; capture drivers may never write human evidence.
- A clean build or automated completion is not a finished milestone. Completion requires a fresh fixed-build human first-level completion and qualitative review.
- First-clear target is 10–15 minutes of unpaused play, 0–5 ordinary deaths, and 2–6 Boss attempts. These are diagnostic thresholds, never statistics to fake or silently trim.
- Every tuning change is centralized, test-backed, recorded with before/after evidence, and followed by the complete gate.
- Preserve zero-pay-per-use production, existing saves/settings, the first-level-only guidance boundary, and all replacement interfaces.
- Leave unrelated import, unique-id, screenshots, rejected art, and local art-tool modifications untouched and unstaged.

---

### Task 1: Full-route integration coverage and mutation trust

**Files:**
- Modify: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Create: `tests/integration/CombatReadabilityExperienceTests.cs`
- Create: `tests/integration/ThreadExperienceRuntimeTests.cs`
- Modify: `tests/RunPrologueTests.cs`
- Modify: `tools/verify-prologue.ps1`
- Modify: `tools/audit-prologue-production.ps1`

**Interfaces:**
- Smoke uses public tap/hold helpers from Plan 2 and public attack frames from Plan 1.
- Full gate produces exact markers `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK` once each.
- Gate protects generated scenes and `project.godot` from semantic/hash drift and restores bytes in `finally`.

- [ ] **Step 1: Add integrated RED assertions**

  Traverse the real six-room level and assert: every ordinary enemy exposes Telegraph before damage; each true danger shape intersects its active hitbox; tap opens Bell-Chain without movement input; hold-aim reaches an anchor outside tap range; wall obstruction produces no pull and a named failure; every thread cast spans multiple physics frames; all three Boss attacks and HammerThrow are observed; death/recovery clears transient visuals; completion still persists.

- [ ] **Step 2: Run RED against missing integration behavior**

    & 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
    & 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs --log-file experience-integration-red.log

  Expected: named failure at the first unintegrated subsystem contract, not a compile/import crash.

- [ ] **Step 3: Fix only integration wiring**

  Connect existing public frames, stable node paths, and reset hooks. If a model or renderer is wrong, stop and fix it in its owning plan/file with focused RED/GREEN evidence rather than adding smoke-only exceptions.

- [ ] **Step 4: Prove three mutations**

  Independently remove one attack Telegraph emission, collapse thread Travel into Apply, and skip death cleanup. Each mutation must fail its named runtime assertion; restore after each.

- [ ] **Step 5: Run GREEN twice**

    powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
    powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1

  Require identical success markers, 0 warnings/errors, all builders, clean error scans, and no protected-file drift in both runs.

- [ ] **Step 6: Commit**

    git commit -m "test: verify readable first-level experience"

---

### Task 2: Deterministic proof route and multi-resolution sensory audit

**Files:**
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tests/presentation/PrologueVisualAudit.cs`
- Modify: `tests/presentation/PrologueUiResolutionCapture.cs`
- Modify: `tools/capture-prologue.ps1`
- Modify: `tools/EvidenceChecks.ps1`
- Create: `docs/playtests/2026-08-21-combat-readability-presentation-audit.md`

**Interfaces:**
- Presentation route exposes named frame markers for all attack phases, thread phases/failures, action transitions, room landmarks, compact HUD states, Boss retry, and completion.
- Evidence checks require fresh nonempty output, exact completion marker, clean log, expected dimensions/frame count/rate, and no stale output reuse.

- [ ] **Step 1: Add audit RED requirements**

  Require named captures for Rust/Moth/Laborer Telegraph-Active-Recovery; Boss Sweep/LeapSlam/GroundWave/Hammer; thread Preview/Windup/Travel/Latched/Apply/Failed/Recovery; player run-brake-jump-land/attack/thread/hurt; each room landmark; compact and expanded guidance; Boss HUD and completion clear.

- [ ] **Step 2: Run RED**

  Existing proof lacks the full dwell/marker set and must fail the new evidence assertions rather than reuse old screenshots.

- [ ] **Step 3: Extend only the deterministic presentation driver**

  Add physics-frame dwell at observable states without changing gameplay source. Capture at 1280×720/60 FPS; run UI audit at 1920×1080, 2560×1440, 3840×2160, and supported widescreen.

- [ ] **Step 4: Capture fresh evidence**

    powershell -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly

  Verify fresh timestamps/hashes, exact completion markers, no ERROR/SCRIPT ERROR/Parse Error, and deterministic frame counts.

- [ ] **Step 5: Perform manual frame review**

  Inspect consecutive frames, not isolated best frames. Record pass/fail for attack prediction, true-range alignment, skill phase separation, pose continuity, cape/lantern inertia, landmark identity, enemy silhouette, foreground occlusion, HUD overlap, prompt placement, and danger-color discipline.

- [ ] **Step 6: Commit**

    git commit -m "docs: audit first-level sensory improvements"

---

### Task 3: Fresh hands-on run and evidence-backed correction loop

**Files:**
- Modify only if needed: `scripts/prologue/ProloguePlaytestSession.cs`
- Modify only if needed: `scripts/prologue/ProloguePlaytestReport.cs`
- Modify only if needed: `scripts/prologue/ProloguePlaytestRecorder.cs`
- Modify: `docs/playtests/2026-08-14-prologue-first-level.md`
- Modify: `docs/playtests/2026-08-21-combat-readability-presentation-audit.md`
- Modify: `README.md`

**Interfaces:**
- Hands-on report retains source label, unpaused time, room order, ordinary/Boss deaths, Boss attempts, retry delay, checkpoint recovery, shortcut/discovery, and completion.
- Qualitative review records: attack prediction cause, thread success/failure clarity, action continuity, room/enemy/UI readability, and remaining discomfort.

- [ ] **Step 1: Verify the human-evidence boundary**

  Run recorder fixtures proving headless/capture/editor sources do not write a HandsOnLauncher report and launcher environment grant is restored after child process creation.

- [ ] **Step 2: Launch the exact candidate**

    powershell -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1

  Do not inject inputs. The human player completes or closes the run. Record the fixed commit hash before launch.

- [ ] **Step 3: Read the fresh report without rewriting it**

    powershell -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1 -OpenReportOnly

  Compare completion, unpaused time, ordinary deaths, Boss attempts, room where deaths cluster, retry time-to-control, shortcut, and discovery against the target thresholds.

- [ ] **Step 4: Record qualitative answers**

  For each ordinary enemy and Boss attack, note whether direction was predicted from pose/range/sound before Active. For mechanical thread, note whether tap/hold intent, target, flight, latch, effect, and failure were understood. Note any hard action cuts, hidden feet/weapons, visual noise, HUD obstruction, or unclear room landmark.

- [ ] **Step 5: Correct one evidence-backed cluster at a time**

  Make the smallest centralized tuning or presentation change in the owning subsystem. Add/adjust its focused test, run RED/GREEN plus mutation, then run the complete gate and a focused visible capture. Repeat until the player completes and no unresolved Critical/Important sensory issue remains.

- [ ] **Step 6: Keep the goal active if the run fails**

  A non-completion, out-of-target time, >5 ordinary deaths, >6 Boss attempts, or inability to explain attacks/thread leaves the milestone `正在优化` or `待真人复核`; do not relabel it complete.

---

### Task 4: Final certification, README truth, and playable handoff

**Files:**
- Modify: `README.md`
- Modify: `docs/playtests/2026-08-21-combat-readability-presentation-audit.md`
- Create: `docs/playtests/2026-08-21-first-level-sensory-certification.md`
- Modify: `assets/production/prologue-assets.json` only when each listed family is genuinely accepted

**Interfaces:**
- README status vocabulary: `已完成`, `正在优化`, `待真人复核`, `待正式素材替换`.
- Certification links exact commit, gate logs, capture metadata/key frames, and HandsOnLauncher report.

- [ ] **Step 1: Run final gates on exact HEAD twice**

    powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
    powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
    powershell -ExecutionPolicy Bypass -File .\tools\audit-prologue-production.ps1

  Require 0 warnings/errors, all builders, exact markers, no semantic/hash drift, clean `git diff --check`, and no unresolved fallback.

- [ ] **Step 2: Produce and review final proof**

  Capture fresh deterministic evidence, scan logs, and inspect all named key-frame sequences. The proof may establish visual/runtime behavior but not human completion.

- [ ] **Step 3: Require fixed-build human completion**

  Launch the exact verified HEAD and retain the source-labeled report. Certification remains open until the human completes and explicitly accepts difficulty, mechanical thread, action continuity, and overall sensory direction.

- [ ] **Step 4: Update durable project status**

  README states what is built, what remains, the latest real report, exact run/verify/capture commands, zero-paid boundary, and asset-table status per family. Do not leave “Final” on a family still under active visual or feel revision.

- [ ] **Step 5: Independent review**

  Request a read-only review of code, tests, generated-scene semantics, proof freshness, documentation truth, and human-evidence boundary. Fix all Critical/Important findings and rerun the affected gates.

- [ ] **Step 6: Commit and launch the playable build**

    git commit -m "feat: certify improved first-level experience"
    powershell -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1

  Leave the visible game running for the user with no injected input and report the process/window evidence. Only then may the long-running goal be marked complete.
