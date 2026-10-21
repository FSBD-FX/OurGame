# Task 9 implementation report — continuous progress, guidance, save, and telemetry

Date: 2026-08-27

Implementation commits: `802b3ea` (`feat: migrate prologue progress to seamless canyon`) and Fix Round 1 `865f43a` (`fix: harden prologue recovery and evidence`)

Status: Fix Round 1 implementation and verification are finished; Task 9 remains open for independent controller re-review.

## Result

Task 9 now uses the five-sector seamless canyon as its only production authority. It implements the exact ten-state objective sequence, explicit Basin/Temple checkpoint activation, deterministic same-frame death recovery with a clean next-physics resume boundary, current-mounted-sector guidance, schema-v2 save/report data, selective schema-v1 migration, bounded launcher-grant authorization for real HandsOn telemetry, exact retirement replacement, continuous bilingual UI capture, and an AutomatedFixture exact-generated-main physical route through Boss completion.

No production `RoomRoot`, `CurrentRoom`, `CurrentRoomId`, `TransitionTo`, room teleport, old entrance, or old-room capture authority was added. No formal/paid art or `README.md` was changed by the Task 9 commit.

## Scope

The implementation commit contains 41 approved implementation/test files: the 23 primary plan files plus the controller-approved direct dependencies for continuous guidance targets/localization, same-frame recovery, game flow, registry execution, exact-generated-main proof, continuous capture, FormalHud compatibility, and protected prompt placement.

Narrow scope rulings used during execution:

- `PrologueHud.cs` and `PrologueFormalHudTests.cs`: remove the old `ReachCorridorLamp`/K fallback only.
- `PrologueGuidanceTargetResolverTests.cs`: replace old room rows with five-sector/current-mounted truth; `ProloguePresentation.cs`: compile-surface migration only.
- `PrologueCanyonGeneratedMainRouteTests.cs`: optional internal Task 9 milestone observer. Default-null Task 8 execution remains exact zero-recovery; observer execution requires exact four Requested/Completed pairs and grants one displacement exemption only inside each measured recovery window.
- `WorldTargetMarkerVisual.cs` and `ProtectedPromptPlacerTests.cs`: protect all visible FormalHud rectangles (Health, ThreadSpool, ObjectiveRibbon, Prompt, ModeSelector, Loadout, ResourceCluster, Boss), excluding hidden controls. No Task 10 HUD redesign.
- `P1LoopRuntimeTests.cs`: subscribe before `RequestRespawn` and assert synchronous exact `1/1`; no production or old-room rewrite.

## RED to GREEN evidence

| Contract | Real RED | GREEN closure |
|---|---|---|
| Objective | Compile failed on 17 references after deleting legacy states. | Exact ten enum states; physics-edge advancement; no same-frame skip. `TASK9_OBJECTIVE_GREEN_OK states=10 same_frame_skip=False`. |
| Guidance glyphs | Exact slot glyphs `2`/`3` were missing. | Q/E, LMB/RMB, 1–4, A/D/W/S/release come from InputMap and bilingual first-level-only strings. |
| Continue | Runtime expected Basin `(2380,2014)` but initially remained at the un-authored continuous origin `(0,0)` after two physics frames; the loaded snapshot was activated. | Schema-v2 Continue restores the explicit Basin sector/world position and durable shortcut/discovery/tutorial/objective data. |
| Objective timing | `_Process` allowed an observation edge at the wrong time. | Objective completion is sampled on `_PhysicsProcess`; each event advances at most one edge. |
| Recovery | Resetting sector actors during a physics flush raised collision-query errors. | Player health/energy/heat/shield/claw/combat/motor/velocity reset synchronously; sector actor reset is the only deferred part. |
| Boss route | Real InputMap final attack left Boss at 1 HP. | Route approaches physically and attacks through public input until the real Boss defeat/completion signal. |
| Route accounting | Task 8 default route expected recovery `0`, observer route produced `4`. | Default-null remains strict `0`; Task 9 observer asserts exact four paired windows. |
| Telemetry | Boss attempts was `1` while other exact fields were present. | Two measured frames after initial Boss trigger make attempts `2`; close flush retains the final report. |
| Continuous capture | Capture expected legacy auto-expanded room guidance. | Capture reads current sector/root and live InputMap glyphs; old room/controller fallbacks are absent. |
| Prompt placement | First contact sheet placed guidance over Health/Thread/Objectives/Mode/Loadout/Resource. | Visible FormalHud regions are protected with 8 px growth; center fallback finds a safe on-screen placement. |
| Post recovery oracle | Plan-listed runtime test expected old room entrance `(40,40)`. | It now instantiates the exact continuous main, uses the sole pre-tree `(120,574)` spawn, kills the public HealthComponent, and observes Requested/Completed exact `1/1` in the same frame. |
| Registry hang | `P1LoopRuntimeTests.HiddenDiscoveryPersistsAcrossDeathRecovery` subscribed after synchronous completion and waited forever. | Subscribe before request; exact Requested/Completed `1/1`. Named diagnosis then completed all 75 cases. |
| Snapshot equality | Two schema-v2 snapshots printed identically but record equality compared distinct HashSet references. | Exact scalar fields plus `SetEquals` for tutorials/discoveries. |

The transient-reset assertions cover full health, full energy, zero heat, inactive shield, idle claw, stopped combat, reset motor, and zero velocity. Shortcut/discovery progress remains durable.

## Final verification

All Godot runs used `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe`; builds used `G:\godot\dotnet-sdk\dotnet.exe`. No `godot-ai.exe` was used.

| Gate | Result / terminal evidence | Local log |
|---|---|---|
| Fresh build | Exit 0; 0 warnings, 0 errors. | `artifacts/task9/final-build.log` |
| Fresh exact import | Exit 0; verification GC drain terminal. | `artifacts/task9/final2-import.log` |
| Prototype | Exit 0; `PROTOTYPE_TESTS_OK`. | `artifacts/task9/final2-prototype.log` |
| Prologue pre | Exit 0; phase and drain terminals; only enumerated negative-fixture warnings. | `artifacts/task9/final2-pre.log` |
| Prologue post | Exit 0 in 125.537 s; `PROLOGUE_TEST_PHASE_OK phase=post`, `PROLOGUE_TESTS_OK`. | `artifacts/task9/final-post-terminal.log` |
| Task 9 core | Exit 0; objective/progress/guidance/save/telemetry/game-flow/registry terminals. | `artifacts/task9/final-core-focused.log` |
| Level recovery focused | Exit 0; checkpoint `(120,574)`, Requested/Completed `1/1`, audio drain. | `artifacts/task9/level-runtime-focused3.log` |
| Named registry | Exit 0; all 75 START/COMPLETE pairs. | `artifacts/task9/registry-diagnose3.log` |
| Registry aggregate | Exit 0; 75 executable, 0 retired, exact two Task 9 replacements. | `artifacts/task9/registry-aggregate-green.log` |
| Generated-main headless | Exit 0; Descent pointer boundary, mounted Entrance/Descent/Basin, recovery `0/0`. | `artifacts/task9/final-generated-main-headless.log` |
| Windows GPU | Exit 0; OpenGL/NVIDIA RTX 3060, 1280×720 fixed 60, exact generated main. | `artifacts/task9/final-generated-main-gpu.log` |

The final post intentionally exercises the throwing claw observer, invalid room-presentation root, and rejected `Completed -> Recovering` negative paths. These warnings are asserted fixtures, not product failures. The headless smoke also emits one fail-safe current-sector missing-target warning after crossing into Descent before display pointer input becomes available; it neither resolves a stale room target nor affects the zero-recovery boundary result.

## Exact generated-main physical route

The GPU route uses one persistent production player, public InputMap events, public damage, real checkpoint Areas, the Bell mechanism, real Boss trigger/attacks, and physics-frame sampling. After the sole pre-tree spawn assignment, neither the driver nor observer writes position/velocity or directly toggles controller/checkpoint/Boss state.

- Before Basin: Entrance, checkpoint inactive, death restores `(120,574)` in the same frame.
- After physical Basin activation: checkpoint active, death restores `(2380,2014)`.
- After physical Temple activation: checkpoint active, death restores `(5040,1294)`.
- Boss retry: fourth physical death restores the Temple checkpoint; physical movement retriggers Boss.
- Bell/shortcut opens through E + LMB; Boss completes through real InputMap attack.
- Route terminal: five sectors, four seams, `mounted_max=3`, recovery Requested/Completed exact `4/4`.
- Automated route telemetry terminal: `source=AutomatedFixture production_hands_on_silent=True sectors=5 checkpoints=2 ice=1 overheats=1 attempts=2 completed=True close_flush=True stale_shield_ignored=True shortcut_requests=1 boss_requests=1 boss_saved_pre_completion=True`.

The prior automated `HandsOnLauncher` claim is retracted. Automated generated-main, script, headless, editor, movie-writer, capture, and automation runs cannot produce production HandsOn evidence. Real authorization now requires a fresh bounded single-use launcher grant, matching nonce and canonical project path, an unexpired issue window, the exact launcher source, and a process/argument surface that is not any excluded mode. The verifier consumes the grant even on rejection. The automated route records only test-owned in-memory `AutomatedFixture` telemetry and asserts that the production recorder writes nothing.

## UI and recording review

Continuous resolution capture root: `artifacts/task9/ui-continuous-fixed2/`

Accepted UI contact sheet: `artifacts/task9/ui-contact-sheet-fixed.png`

SHA-256: `38D481CA4C11ED5CD09A53263C3CD2661F7DA87DB5FB08E255D6B6F9C4A0B8F4`

The controller and parent reviewed 720p entrance/retry/Boss/completion plus 4K and ultrawide frames: no blank, clip/overlap, offscreen prompt, J/K, old room, or old route residue; Q/E/LMB/1–4 and current-sector targets are visible. The existing compact/mixed-language FormalHud density is explicitly Task 10 work and does not block Task 9.

Superseded automated route MP4: `artifacts/task9/task9-generated-main-proof.mp4`

Size: 5,027,334 bytes

Duration/frames: 43.35 s / 2,601 frames / 60 FPS / 1280×720

SHA-256: `550A8CF1396B7FF6E98853C0080A2D7D4FC0DF5BCBA96275482E6410C9945D3D`

Lossless movie-writer source: `artifacts/task9/task9-generated-main-proof.avi`

Size/SHA-256: 132,218,344 bytes / `0EFC29FFDF04BE2921B9DCE4D8B76EA30EF888E59AB63283F1D2EFE353DC2547`

Final six-frame sheet: `artifacts/task9/task9-generated-main-contact-sheet.png`

SHA-256: `5478F5654F0F65C2A449A1ABAA710EE201BDA0188766F8C8FB4214BBB2E1A8D1`

This artifact remains valid as historical automated route coverage but is rejected as HandsOn evidence. Its old human-telemetry interpretation is withdrawn.

## Fix Round 1 RED to GREEN closure

| Review finding | Confirmed RED | GREEN closure |
|---|---|---|
| Critical telemetry authorization | A forged environment string authorized the scripted/movie route as `HandsOnLauncher`. | Fresh random grant-file + nonce handshake; canonical project and expiry checks; single-use deletion on every validation; explicit headless/editor/project-manager/`--script`/movie/capture/automation rejection. The PowerShell helper writes UTF-8 without BOM, while the verifier also tolerates and trims a BOM before parsing. |
| Recovery input closure | Held movement, primary attack, and shield requests remained sampled across same-frame reset. | Input/Motor/Combat/Claw/ActionModes/Shield/Energy are synchronously disabled, sampled input and the shield-request latch are cleared, transients reset before `RecoveryCompleted`, and gameplay resumes only on the next clean physics boundary. |
| Shield objective edge | Stale shield heat/use before Basin could satisfy `LearnShield`. | Completion observes a real rising `ShieldFrameChanged(true)` only while `LearnShield`; old use is rejected and one fresh RMB edge advances exactly once. Player replacement and exit safely unsubscribe/resubscribe without duplicate handlers. |
| Legacy authority | Continuous checkpoint/save/progress/guidance exposed room, entrance, hidden, completion, and same-room aliases. | Continuous contracts contain only sector/world/checkpoint/progress truth. Old-room fixture compatibility is isolated in internal `LegacyPrologueCheckpointState`; it cannot restore or become continuous authority. |
| Bell/Boss ownership | Mechanism/Boss mutation before LevelController's handler produced zero or duplicate persistence. | Mechanism/Boss emit events without mutating progress; LevelController is the sole subscription/mutation owner and persists only a true change. Route evidence records exact one shortcut and one Boss request. |
| Schema-v2 Boss durability/non-finite data | `BossDefeated` depended on `Completed`; inactive NaN/infinities were canonicalized instead of rejected. | Independent `BossDefeated` round-trip is verified after defeat and before completion. NaN, +Inf, and -Inf are rejected before active/inactive canonicalization. |
| Registry authority | Count/subset checks could miss a renamed executable and still advertised 73/2. | Independent literal sorted equality covers all 75 names, with missing/renamed/new/duplicate/unclassified/unexpected-retirement mutations rejected; terminal is 75 executable / 0 retired. |
| Deferred actor reset | No test established sector-deferred `ICheckpointResettable` timing. | Real continuous recovery proves player reset and Completed synchronously with actor count 0, then exact one actor reset after the correct Entrance sector is mounted. |
| Localized guidance chrome | Expanded English guidance reused Chinese heading/hint chrome. | Heading and hint prefix are localized data in both languages; five-resolution capture asserts English/Chinese mutual exclusion and protected HUD placement. |

## Fix Round 1 final verification

All gates used the exact indexed main inside a bounded materialization window, followed by byte-identical restoration of the user scene.

| Gate | Terminal evidence |
|---|---|
| Fresh build/import | Build exit 0 with 0 warnings/errors; import exit 0 with `VERIFICATION_EDITOR_GC_DRAIN_OK`. Import log: `artifacts/task9-fix1-final/import.log`. |
| Prototype / pre / post | `PROTOTYPE_TESTS_OK`; `PROLOGUE_TEST_PHASE_OK phase=pre`; full post `PROLOGUE_TEST_PHASE_OK phase=post` and `PROLOGUE_TESTS_OK` (about 110 s, no hang). |
| Task 9 core | `TASK9_CORE_FOCUSED_OK`; exact ten states, checkpoints, guidance, saves, telemetry schema, game flow, and registry all green. |
| Launcher grant | PowerShell `PLAY_PROLOGUE_TESTS_OK`; C# `TASK9_TELEMETRY_GRANT_CONTRACT_FOCUSED_OK single_use=True`. |
| Focused contracts | `legacy_aliases=0`; `authoritative=75`; runtime Requested/Completed `1/1`, `input_closure=True`, `actor_deferred=1`; `bilingual_chrome=True`. |
| Generated-main headless | `TASK9_GENERATED_MAIN_HEADLESS_OK stream=True recovery=0 pointer_boundary=True`. |
| Windows GPU | OpenGL/NVIDIA RTX 3060, 1280x720 fixed 60; four physical Requested/Completed pairs, Bell/shortcut, Boss retry/completion, stale shield rejected, exact one shortcut/Boss request, Boss saved pre-completion; `TASK9_GENERATED_MAIN_OK`. |
| Continuous capture | 84 PNG frames across 720p, 1080p, 1440p, 4K, and ultrawide; terminal `PROLOGUE_UI_RESOLUTION_OK`. |

Final Fix Round 1 automated route MP4: `artifacts/task9-fix1-final/task9-fix1-generated-main.mp4`

- Size / duration / frames: 4,771,314 bytes / 44.82 s / 2,689 frames at 60 FPS, 1280x720.
- SHA-256: `9D8D2049433060190403636F6BCDC1A2D981F2B3C5AE625827B37459EC4BC867`.
- Full decode exit 0; black-detect emitted no interval; maximum exact identical run is 4 frames / 0.067 s.
- Successful lossless source: `task9-fix1-generated-main-retry.avi`, 136,610,574 bytes, SHA-256 `3CEB973AFE3EE7DFB70ED0834DE88AF063EEACDFA9CDE59777191FAF39367E96`.
- A first provisional movie attempt stopped at the Temple physical drive timeout after 2,260 frames and is not accepted or listed as evidence; the bounded retry completed the full route without a code change.

Accepted six-stage route sheet: `artifacts/task9-fix1-final/task9-fix1-generated-main-contact-sheet.png`, SHA-256 `5A1F0E26D4B11DBA7A79B9982A9DEB9DC1E667691ACAE0F45D5ACE62360F8893`.

Accepted bilingual sheet: `artifacts/task9-fix1-final/task9-fix1-bilingual-ui-contact-sheet.png`, SHA-256 `E265D9B717E18D346C2E5F559210FC370D81544C0441800BEB98049C01E9CD2A`.

The implementer and controller reviewed the original frames. The route sheet shows live guidance, pre-Basin recovery, Basin checkpoint, Temple Bell/shortcut, Boss retry, and completion with no blank frame or obvious non-recovery teleport. The bilingual 720p/4K/ultrawide samples show mutually correct English/Chinese chrome, current-sector/InputMap text, safe bounds, and HUD avoidance. Existing compact mixed-language FormalHud density remains Task 10 debt and did not expand this fix.

## Protected main, leak, and dirty-worktree audit

- Worktree `scenes/prologue/PrologueLevel.tscn` before and after every exact gate: SHA-256 `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`.
- Verified user backup: `artifacts/task9/protected-main-user-copy.tscn`, same SHA-256.
- Exact materialized main SHA-256: `3E55D713486553899C674E66F529D1726A743B75E319AF8BF27483115AFB447C`.
- Committed/index main blob: `2b5532a3047ca0b07adbc5bfcc938ff900fd41c4`; it already matched HEAD, so the Task 9 implementation commit did not stage the user worktree scene.
- Original implementation staged scope: exactly 41 approved implementation/test files. Fix Round 1 staged exactly 38 approved implementation/test files; `git diff --cached --check` passed. No artifact, `.uid`, import, README, formal/paid art, protected scene, or unrelated dirty path was staged.
- GPU PID before/after sets are identical. PID 31500 is a controller-approved pre-existing external baseline created 2026-08-22 00:07:15 with inaccessible protected command fields; it was not killed. Four pre-existing `godot-ai.exe` attach processes were not touched. No Task 9 Godot process remained.

Local `artifacts/`, capture output, import sidecars, and all unrelated user dirty/untracked files remain unstaged. Task 9 generated no paid assets.

## Handoff

Original implementation/tests are committed at `802b3ea`; Fix Round 1 implementation/tests are committed at `865f43a`. This report and the tracked ledger are committed separately. Task 9 remains open for independent spec and quality re-review; it is not marked independently approved or complete here.

## Fix Round 2 — atomic rejection consumption

Independent Fix Round 1 re-review closed every other finding and retained one Important defect: forbidden launch surfaces and several malformed shapes returned before consuming an otherwise valid grant, so the capability could be replayed during its expiry window. Fix Round 2 implementation/tests are committed at `e49040d` (`fix: atomically consume playtest evidence grants`).

The tests-only RED reported 13 concrete old-behavior failures in one fresh focused process:

- process `--headless`, `--editor`, `--project-manager`, `--script`, `--write-movie`, `--capture`, and `--automation`: rejected but `consumed=0`;
- user `--headless` and `--capture`: rejected but `consumed=0`;
- blank content and duplicate nonce with one identifiable valid path: rejected but `consumed=0`;
- prefix-collision outside path and `..` traversal path: incorrectly `authorized=True`, `consumed=1`, and arbitrary file not preserved.

Corrupt/expired/mismatched grant content was already consumed by the prior implementation. Duplicate path arguments already rejected without deleting either ambiguous file, and an injected consume exception already denied authorization. Fix Round 2 retains those behaviors and closes the ordering/path failures.

The runtime boundary now performs this sequence:

1. Identify exactly one separate `--knight-playtest-grant-file` value.
2. Canonicalize it and require its exact parent to be the platform temp directory's direct child `knight-playtest-grants`; prefix collisions, traversal, missing/blank/equals-form, duplicate paths, and reparse grant directories are rejected without a consume attempt.
3. Atomically move the grant entry within that same directory to a fresh unique claimed name. The original capability path is unavailable before bytes are read or any remaining decision is made.
4. Reject a claimed reparse/symlink entry before reading it; read the claimed file and delete it in `finally`. Move/read/reparse/cleanup exceptions all fail closed. A cleanup failure cannot authorize the claimed capability.
5. Only after the atomic claim, evaluate malformed/forbidden process and user arguments, missing/forged environment source, duplicate/blank nonce, blank/corrupt/expired content, and nonce/project/grant-source mismatch. Every such identifiable dedicated grant is terminally consumed; a clean replay fails. Success remains single-use.

Direct coverage asserts exact successful consume count and canonical path, failed claim attempts, replay denial, valid grant plus every forbidden surface, missing/forged environment source, corrupt/blank/expired/mismatched content, duplicate nonce, malformed null user argument, duplicate/missing/blank/equals path shapes, prefix collision, traversal, arbitrary-file preservation, injected consume failure, and a real filesystem atomic claim whose original path is absent before the forbidden decision.

Proportional final verification:

- fresh `dotnet build Knight.csproj --nologo`: exit 0, 0 warnings, 0 errors;
- two fresh focused Godot processes: `TASK9_TELEMETRY_GRANT_CONTRACT_FOCUSED_OK single_use=True`;
- unchanged launcher helper regression: `PLAY_PROLOGUE_TESTS_OK`;
- optional full Task 9 core against the protected user worktree scene reached all objective/progress/guidance/save/telemetry unit markers, then failed the existing exact-generated-main round-start position oracle because the protected AE09 user scene is intentionally not the indexed generated main. The Fix Round 2 ruling explicitly required no exact-main materialization; this non-authoritative route attempt caused no scope expansion.

Fix Round 2 changes exactly two approved files: `scripts/prologue/ProloguePlaytestEvidenceGrantVerifier.cs` and `tests/unit/ProloguePlaytestEvidenceGrantTests.cs`. `tools/PlaytestEvidenceGrant.ps1` required no change. The protected worktree main remains SHA-256 `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`, the index main blob remains `2b5532a3047ca0b07adbc5bfcc938ff900fd41c4`, and the scene is not cached. No new Godot process remains; PID 31500 and the surviving `godot-ai.exe` processes are external baseline entries. README, scenes, `.uid`, imports, artifacts, formal/paid art, and unrelated dirty files remain unstaged.

Task 9 remains open for the same independent reviewer; Fix Round 2 does not claim approval or completion.

## Independent approval

The same independent reviewer completed the scoped Fix Round 2 re-review of `2f7a300..c8836d7` and returned **APPROVE** with Critical 0 / Important 0 / Minor 0 / Evidence gaps 0. Fresh exact-commit review repeated the 0-warning/0-error build, launcher PowerShell regression, editor import/GC drain, and two standalone telemetry-grant focused processes. It independently confirmed the atomic claim ordering, rejection consumption and clean replay denial, ambiguous/outside path preservation, exact two-file implementation plus two-document boundary, protected main/index integrity, and zero review-run process leaks.

Task 9 is therefore **COMPLETE and independently review-clean**. Task 10 remains the next planned scope; no Task 10 HUD redesign was folded into this task.
