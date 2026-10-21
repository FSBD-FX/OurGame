# Task 11 brief — Replace Verification, Capture and Performance Evidence for the New Prologue

**Plan:** `docs/superpowers/plans/2026-08-26-keyboard-mouse-grapple-canyon-prologue.md`

**Spec:** `docs/superpowers/specs/2026-08-26-keyboard-mouse-grapple-canyon-prologue-design.md`

**Base:** `c4accf1da3804ab4a36f0f9bd419f0d3e119e75a`

## Files

- Modify: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tests/presentation/BossRetryPresentation.cs`
- Modify: `tests/presentation/PrologueVisualAudit.cs`
- Modify: `tools/verify-prologue.ps1`
- Modify: `tools/audit-prologue-production.ps1`
- Modify: `tools/capture-prologue.ps1`
- Create: `tools/extract-prologue-keyframes.ps1`
- Modify: `tests/powershell/PrologueEvidenceChecks.Tests.ps1`
- Modify: `tests/powershell/Task4P0SemanticResources.Tests.ps1`
- Modify: `tests/powershell/Task4P0SmokeCheckpoint.Tests.ps1`
- Modify: `tests/powershell/Task4ProloguePhaseCoverage.Tests.ps1`
- Modify: `tests/powershell/Task4PlaytestRecorderTransition.Tests.ps1`
- Create: `tests/powershell/PrologueKeyframeExtraction.Tests.ps1`
- Modify: `tools/play-prologue.ps1`
- Modify: `README.md`
- Modify: `docs/playtests/2026-08-14-prologue-first-level.md`
- Create: `docs/playtests/2026-08-26-canyon-prologue-validation.md`

## Interfaces

- Consumes the complete runtime from Tasks 1–10.
- Produces exact terminal markers `PROLOGUE_CRITICAL_PATH_OK`, `PROLOGUE_CANYON_TRAVERSAL_OK`, `PROLOGUE_PRESENTATION_COMPLETE`, `PROLOGUE_BOSS_RETRY_COMPLETE`, `PROLOGUE_PRODUCTION_AUDIT_OK`, `PROLOGUE_VERIFY_OK`.
- Produces fresh 1280×720@60 FPS full-flow and Boss-retry videos plus reviewed keyframes.

## Steps

- [ ] **Step 1: Write fail-closed evidence contracts**

Update PowerShell tests to reject missing Q/E/mouse/slot proof, missing energy/overheat proof, missing each surface material, fewer than five sectors, any room teleport marker, mandatory Dash, stale J/K prompt, absent checkpoint restore, absent Boss retry, empty/frozen movie, wrong resolution/FPS, performance peak over 50 ms or forbidden logs.

- [ ] **Step 2: Run evidence RED**

Run the PowerShell evidence suites against the old drivers. Expected: named failures for old topology and missing new markers.

- [ ] **Step 3: Rebuild real critical-path smoke**

Drive InputMap through all five sectors using Q/E, mouse coordinates, slot 1, attack energy, shield normal/heavy/overheat, GripStone, Ice rejection, three ascent grapples, mechanism, both checkpoints, shortcut, death/recovery, all Boss attacks and completion. Keep a bounded frame budget with at least 10% headroom.

- [ ] **Step 4: Rebuild deterministic presentation drivers**

Show title/start, mode HUD, attack/energy, shield/overheat, claw reticle states, descent, basin, three ascents, optional alcove, temple, Boss intro/all attacks/death/retry/completion/title return. Every demonstration uses real runtime state; only the presentation driver may reposition between non-timing proof beats.

- [ ] **Step 5: Rebuild production audit**

Audit all five sectors, every stable visual root, all typed cues/VFX, continuous camera boundaries, exact surface proofs, mounted-sector bounds and callback wall-clock performance. Preserve Windows display and fixed 60 FPS.

- [ ] **Step 6: Run complete verifier twice**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Expected from each fresh process: build 0 warnings/errors, all builders/tests/smoke/audits/capture contracts green, one exact `PROLOGUE_VERIFY_OK`, no fatal/error/leak signature.

- [ ] **Step 7: Capture and inspect final proof**

Run `tools/capture-prologue.ps1`; verify AVI frame counts, 1280×720, 60 FPS and audio headroom. Extract keyframes for every listed beat, inspect them at original resolution and record the reviewed paths in the validation document.

- [ ] **Step 8: Update durable status**

Update README build/remaining-work/asset table, validation document and playtest audit with exact commit, commands, markers, performance peak, movie frames, keyframe paths and the unchanged no-paid-generation status. Mark normal-speed human acceptance open until it actually occurs.

- [ ] **Step 9: Commit automated certification**

```powershell
git commit -m "test: certify seamless canyon prologue"
```

- [ ] **Step 10: Launch normal-speed human playtest**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1
```

Require a no-long-idle 10–15 minute complete run, record the least smooth or least clear moment, fix the owning subsystem with a RED test, rerun the proportional and full gates, and append the final report ID plus issue closure to README and the validation document.

- [ ] **Step 11: Commit human closure only after evidence exists**

Stage only the resulting docs and any test-backed fix, then commit:

```powershell
git commit -m "docs: close canyon prologue human playtest"
```

Do not claim the milestone complete before this commit has an actual `HandsOnLauncher` report and qualitative note.

## Current controller boundaries and retained debt

- Reuse and verify, but do not recreate, `E:\OurGame\Knight\archives\Knight-Prologue-v0.9.0-belfry-baseline-20260826.zip`. Verified SHA-256: `0EA35D960D3FCAD447F8C46351C6EE4DAA36CF425FFB59DC2263ABC60E0D0720`. The embedded `./docs/releases/v0.9.0-belfry-baseline.md` identifies `v0.9.0-belfry-baseline`, source `54018ba`, and truthfully leaves the 10–15 minute human gate open.
- Preserve user-dirty `README.md` and `docs/playtests/2026-08-14-prologue-first-level.md`. Startup bytes are snapshotted under ignored `artifacts/task11-preflight/`; initial SHA-256 values are respectively `10B4F19C3022FDD824ED29173AA1722693346EEA4A3C81F5F310B0A3660606FF` and `7D6ECC7A68AEA5EAAE04FB71590E9C176E63D0CC565C413BFAA06882FA87AAD9`. The startup dirty patch SHA-256 is `90942B750C039F2AD0AF7FC431AF1E4E60FE8BAD7466801B00D925ABA3482BF1` (README +19/-10, playtest +19/-0). Apply only minimal Task 11 sections and stage only Task 11 hunks; never overwrite, restore, or stage the pre-existing user diff.
- Preserve user worktree SHA-256 values for `project.godot` (`63E151517E9C332E0588B0942B52D45F2B2D5BE939638D427670819AF36C6B74`), `scenes/prologue/PrologueLevel.tscn` (`AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`), and `scenes/presentation/PrologueEnvironmentKit.tscn` (`C20872EFBB2614E0D9E40216CCD8DD263861009EB2C347CEB03DAD01F4CABC00`). Preserve unrelated dirty/import/artifact files and external PID 31500; never use or terminate `godot-ai.exe`.
- The production topology is the continuous five-sector main only. Do not restore `RoomRoot`, `CurrentRoom`, `TransitionTo`, old room teleports, J/K controls, or mandatory Dash. Legacy verification/guidance/UI truth must migrate to the continuous topology or fail closed.
- Automated certification requires the real five-sector critical path, real InputMap/physics/state, both checkpoints, real death/recovery, shortcut persistence, all Boss attacks and retry, completion, exact stable roots/16 typed cues/VFX/camera/surfaces, callback wall-clock peak below 50 ms, and bounded frame evidence with at least 10% headroom. Presentation may reposition only between non-timing beats; smoke and mandatory traversal may not teleport or reset position/velocity.
- No paid generation or formal art is authorized. README and validation documents must keep normal-speed human acceptance **OPEN** through automated certification and independent review.
- Task 11 automated evidence must receive an independent spec-and-quality APPROVE before automated certification is considered accepted. Only then launch `tools/play-prologue.ps1` visibly for the user. Never fabricate or synthesize HandsOnLauncher telemetry or a qualitative note. Task 11 and the overall goal remain open until a genuine no-long-idle 10–15 minute complete run records the least-smooth or least-clear moment and any owning fix closes under RED→GREEN.
- Use exactly `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe` and `G:\godot\dotnet-sdk\dotnet.exe`. Build, import, test and capture in fresh materializations where required; require 0 build warnings/errors and no task-owned residual Godot process.
