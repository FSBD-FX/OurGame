# Task 7 implementation and evidence

Scope: stable authored checkpoints and explicit nondestructive layout migration only. Base `b6f1c1f5a69906f149ddc2b47ee243dd83848c5d`, worktree `E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`, branch `codex/task6-certified-final-20260902T0555`. Root approved one additional test path, `tests/integration/ContinuousCanyonSaveIsolationTests.cs`; no other ownership expansion. No subagents, asset generation, downloads, publishing, automation, resets, clean, or add-all.

## Implemented contracts

- Schema 3 is independent of layout 2. Raw camelCase `version` and required `layoutVersion` are inspected before deserialization/normalization. Missing layout remains 0 in generic/sample loads. Null, strings, nonintegers, zero, unknown layout, wrong casing, and unsupported full-world schemas cannot qualify as current.
- The sole legacy schema2 sector-to-stable-ID table is in `PrologueSaveStore.LoadFullWorld`. Basin maps to `canyon.v2.basin-camp` / `(1680,3960)`; Temple to `canyon.v2.temple-forecourt` / `(5920,1080)`; no activated checkpoint in a known sector maps to `canyon.v2.start` / `(160,600)`. Old coordinates are never used as placement authority. Unsupported/unknown legacy state gets a Start-only choice.
- Layout2 normalization resolves checkpoint ID, sector and position from authored data. Unknown checkpoint, shortcut, mechanism and discovery IDs fail closed. Existing explicit Boss results, tutorials, supported discoveries, and completion metrics survive valid migration; absent Boss evidence remains false.
- The registry and active leaf/parent builders use the exact IDs above. Entrance still has only its Spawn marker. Basin and Temple leaf triggers were regenerated before the parent. Semantic comparison after removing engine-generated `unique_id` values shows exactly two leaf `EntranceId` changes; parent content is otherwise unchanged.
- `CheckpointTrigger.EntranceId` is the carried stable identity in layout2. World activation publishes its ID/sector/authored position together; old coordinate activation is accepted in layout2 only when it exactly identifies an authored checkpoint. Unknown IDs and backward Basin activation after Temple are rejected. Save/Continue/recovery use stable IDs. Legacy/frozen sample coordinate APIs remain independent.
- The title menu explicitly offers `Continue from mapped checkpoint` and `Start continuous canyon (old save kept)`. Unknown legacy state disables mapped Continue. Headless/automation defaults cannot consume a pending choice. Pending persistence is blocked before `SaveAtomic`; the fixture uses an unwritable target-directory sentinel to prove an attempted call would fail.
- Legacy slot resolution uses the selected new slot's sibling directory, not an unrelated global fallback. Legacy source bytes remain unchanged. Normal current Continue stays read-only. If the NEW slot itself is unreadable/foreign, Continue and pre-choice writes are rejected; an explicit Start retains its exact bytes in `prologue-save-layout-v2.json.preserved-<guid>.json` before atomic replacement. A failed replacement leaves the original file intact; only the call's temporary file is removed.
- The independent Basin sample still reads/writes only its injected sample path with layout0 save data and frozen layout1 authored coordinate validation. It never receives the continuous migration menu.
- `OpenShortcuts` and `ActivatedMechanisms` round-trip actual runtime state. `StableShortcutIds`/`StableMechanismIds` remain graph-edge registries because graph validation rejects non-edge entries. Narrow `PersistableShortcutIds`/`PersistableMechanismIds` additionally admit the actual temple bell gate, lower steam valve, courtyard crank and exact Main7 seal prerequisite. Legacy discovery compatibility is an explicit bounded registry (`hidden_alcove`, `supply_cave`, `high_ruin`), not arbitrary string acceptance. Ordinary encounter kills are not whitelisted or persisted.
- Fresh-world testing found a genuine mount-order issue: early `NodeAdded` applied open state, then staged activation restored authored closed collision bits. `PrologueLevelController` now reapplies durable controls after validated mount, and after save restoration for already mounted controls. No actor reset/defeat replay or claw-residency contract changes were introduced.

## Tests and strength of evidence

`ContinuousCanyonMigrationTests.RunStore` covers all three mappings, no-checkpoint mapping, crafted old Temple `(5040,1294)` rejection, schema/layout independence, unknown IDs, malformed data, absent Boss evidence, SHA-256 preservation through load/choice/new-slot save, and a real Windows locked-file failed atomic replacement. `PrologueSaveTests` adds strict current-wire and whitelist negatives; `PrologueProgressTests` adds stable snapshot and partial-prerequisite restoration.

`RunMenus` instantiates the generated level, uses the actual menu Button.Pressed bindings for mapped Continue, new Start, and invalid legacy Start, tests headless auto-start suppression, and verifies exact player coordinates immediately at the real Continue boundary. A mapped explicit Boss result then completes once; terminal recovery is rejected and completion/Boss persistence is retained.

`RunPersistence` creates an isolated generated level, contacts the actual serialized Basin/Temple triggers, operates the actual valve/winch/crank/bell receiver contracts, defeats the real ordinary Gear group through actor Health, and (in the completed case) defeats both actual Main7 actor batches through Health. It never calls group `Complete()` or fills persistence lists to fabricate prerequisites. It saves, disposes the level, creates a fresh level/progress instance, then uses real Continue. Checks include shortcut collision/visuals and both graph directions, seal collision/visual/graph agreement, valve/jet restoration, exact Main7 completion, a genuinely previously defeated ordinary group becoming active again, and no additional actor-defeat/group-completion signals during restoration. The test holds the Motor stationary and relocates the player between test sectors; it is persistence/contact evidence, not traversal/playability evidence. There is no separate reward event in this runtime; actor `Defeated` and group `GroupDefeated` are the observed completion signals.

`RunRecovery` proves generated-level completed-save Start preserves paired human metrics but clears session progress, genuine lethal damage returns the first death to `(160,600)`, and physical Basin activation advances recovery to `(1680,3960)`. The focused P0/residency selection also runs accepted escaped-checkpoint rollback and Task2 claw residency tests. The default continuous runner retains and executes all existing sample/Task2–6 cases plus the new Task7 cases; none was skipped or replaced.

## Exact command and evidence convention

Every actual engine case used the worktree above and this fresh successful build first:

```powershell
$env:DOTNET_ROOT = 'G:/godot/dotnet-sdk'
$env:DOTNET_ROOT_X64 = 'G:/godot/dotnet-sdk'
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:DOTNET_PROCESSOR_COUNT = '2'
# G:/godot/dotnet-sdk was prepended to PATH.
G:/godot/dotnet-sdk/dotnet.exe build Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo
```

Actual engine executable: `G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe`.

```text
--headless --path . --fixed-fps 60 --script <script> --log-file <case>/godot.log [-- <selection>]
```

Builders used `res://tools/build/prologue/BuildCanyonBasin.cs`, then `BuildLostTemple.cs`, then `BuildPrologueLevel.cs`, without user args. Tests used `res://tests/RunContinuousCanyonTests.cs`; selections are listed below. Each engine was launched by `Start-Process -WindowStyle Hidden -PassThru`, stdout/stderr redirected, polled with `WaitForExit(55000)`, finalized with `WaitForExit()`, then its actual `ExitCode` recorded. No engine ran after the one failed build.

All case directories below are under `E:/OurGame/Knight/.evidence/september09-continuous-canyon/`. Each actual case contains `process.json` (exact exe, args, cwd, PID, exit, start/end), `actual-exit-code.txt`, `stdout.log`, `stderr.log`, `godot.log`, `dotnet-build.log`, and `source-hashes.json`. From the first menu-green attempt onward, `build-process.json` additionally records actual build PID, exit, separate start/end and all DOTNET settings, with `dotnet-build.stderr.log`.

Important logging limitation: the two initial RED cases and three builder cases ran the pinned compiler synchronously with a checked successful exit and build log, but did not separately record compiler PID/exit/start/end; their `process.json.start` is the wrapper's pre-build start, not precise engine start. Engine PIDs/exits are actual. This limitation is not retroactively repaired or hidden. Subsequent cases include full separate build metadata. Earlier source-hash sets omitted the newly approved isolation-test path; the final default cases include all 20 owned runtime/test/scene inputs.

| Case directory | Build PID/exit | Godot PID/exit | Selection and reviewed result |
|---|---:|---:|---|
| `task7-migration-behavioral-red-20260920T104326674-05a305005f704e788e40f70d9a381cdf` | not separately recorded / successful log | 57696 / 1 | `--task7-migration-only`: inert load-result shell fails `LegacyRequiresExplicitChoice`, expected MigratableLegacy, actual Current. Genuine behavioral RED, not compiler evidence. |
| `task7-menu-behavioral-red-20260920T104939318-07735be284f44a4da3e31da17e08d40e` | not separately recorded / successful log | 48124 / 1 | Same selection: actual menu headless default consumed choice, expected Title, actual Playing. |
| `task7-build-basin-20260920T105425069-031cd2b7c8054ecf915a4d6e308f3512` | not separately recorded / successful log | 3656 / 0 | Basin builder: packed/validated 218 nodes before/after, 3 micro-scenes; no engine errors. |
| `task7-build-temple-20260920T105512113-b66bcee74bc3420b9e9419048513e9b7` | not separately recorded / successful log | 50756 / 0 | Temple builder: 117 nodes before/after, 3 micro-scenes; no engine errors. |
| `task7-build-parent-20260920T105545137-8bedc9187bf34a569348b3253340bd69` | not separately recorded / successful log | 52104 / 0 | Parent packed and validated after both leaves; no engine errors. |
| `task7-migration-menu-green-attempt-20260920T105610432-8164dc5a5be544c58a53845947a8dd07` | 34324 / 0 | 75260 / 0 | NOT clean GREEN: assertions passed, but disposed AudioStreamPlayer error during fixture teardown. Fixed test ordering to let restored Boss completion settle before audio drain; audio production files untouched. |
| `task7-real-persistence-first-20260920T105951015-95af02e8b40f4386aecd97994105d45f` | 13308 / 1 | not started | Test used nonexistent graph method `ReachableFrom`; corrected to existing `Reachable`. Compiler-only failure, not behavioral RED. |
| `task7-real-persistence-attempt2-20260920T110033374-de78c2bc3e6846b494012280d9dc5f0c` | 38764 / 0 | 37392 / 1 | `--task7-persistence-only`: fixture disabled the whole CharacterBody and thereby its collision; actual contact did not activate Basin. Fixed fixture to hold Motor only, retain physical body. |
| `task7-real-persistence-attempt3-20260920T110138888-2a1021fb34454636aa98636604f1f07d` | 43280 / 0 | 53316 / 1 | Genuine fresh-world shortcut collision failure. |
| `task7-real-persistence-diagnostic-20260920T110242854-bc4c3785532e49df8669d33cf245d7aa` | 41268 / 0 | 59488 / 1 | Confirmed disk/progress IDs and open visuals, but collision disabled=false after fresh Continue. |
| `task7-real-persistence-green-attempt-20260920T110400450-331e1f2dd7e549759069653494ee3456` | 72800 / 0 | 72448 / 0 | Clean persistence GREEN after post-mount control reapply: partial seal false, full seal true, no replayed events. |
| `task7-migration-menu-isolation-green-attempt-20260920T110551965-93447e6de0e94576b38e177a467190e1` | 24700 / 0 | 49932 / 1 | Locked-file fixture got Windows UnauthorizedAccessException rather than IOException; test catch broadened to the two real filesystem failure types. |
| `task7-migration-menu-isolation-attempt2-20260920T110717819-cce01d73351043629101cc0222f0d825` | 65352 / 0 | 14008 / 0 | Store/menu/sample-isolation GREEN, no errors; expected rejected-transition warnings only. |
| `task7-p0-residency-first-20260920T110834369-23e83e0f2a584436bf587c0d19d2102d` | 50940 / 0 | 67992 / 0 | `--task7-p0-residency-only`: generated P0 recovery, escaped checkpoint transaction, Task2 residency GREEN; no errors. |
| `task7-coordinate-application-mutation-20260920T111049407-175dce147df44374a7993ef5fcafcd20` | 74140 / 0 | 55204 / 1 | `--task7-migration-only`: named `CoordinateRejection` fails, expected `(1680,3960)`, actual copied old `(5040,1294)`. |
| `task7-coordinate-restored-green-20260920T111121317-653b11dad28d46a5a376301fc55c32f3` | 65488 / 0 | 41932 / 0 | Exact restored source; expanded store/menu/invalid-choice/isolation GREEN, intentional rejection warnings only. |
| `task7-default-continuous-regression-20260920T111157886-3cf0a14864ea4acfa8b28a97b4dbee38` | 46636 / 0 | 31728 / 0 | Default full continuous runner: `CONTINUOUS_CANYON_TESTS_OK`; 0 errors, 8 intentional warnings. |
| `task7-final-default-with-ordinary-freshness-20260920T111545555-3b2f31eece1140e0a1b7b9a31fd55202` | 50112 / 0 | 71292 / 0 | Final default suite after strengthening ordinary freshness: actual Gear group defeated before save, active again in fresh world. Partial seal case retains defeats=2/groups=1; full case defeats=4/groups=2, unchanged on Continue. `CONTINUOUS_CANYON_TESTS_OK`; 0 errors, the same 8 intentional warnings. |

The default run's build timestamps are `2026-09-20T11:11:57.9196511+08:00` through `11:11:58.8362173+08:00`; actual engine `11:11:58.8586050+08:00` through `11:12:19.7841562+08:00`. All other precise recorded intervals are in each case's JSON files. All successful builds report 0 warnings / 0 errors.

Final strengthened run: compiler `2026-09-20T11:15:45.5856790+08:00` through `11:15:55.9040718+08:00`; engine `11:15:55.9261473+08:00` through `11:16:16.9582969+08:00`. Its `source-hashes.json` is the final tested runtime/test/scene snapshot. No runtime/source changes followed this run; only this report was completed.

The eight default-run warnings are one intentional `Completed->Recovering` rejection, six intentional invalid `Title->Playing` rejections (legacy/current/sample), and the accepted deliberate throwing claw observer isolation warning. There are no `ERROR:` records in either stdout or stderr. The deliberately faulting observer's stack points into the existing transaction regression, not a new gameplay failure.

## Mutation restoration and self-review

The coordinate mutation replaced the mapper's normalized return with a normalized object whose X/Y were then overwritten from raw legacy JSON. The named coordinate test failed in the actual rebuilt engine. The exact manual inverse patch restored `PrologueSaveStore.cs`; SHA-256 before and after is `CF0573E0847AF02044A3118B4A33311654277ECEFE69B74B5C585006051BA56A`. Subsequent focused and default engine runs used restored source. No cache patch or stale binary was used.

Self-review verified raw-token gating, supported legacy mapping vs. unknown rejection, no synthetic Boss/seal completion, retained sample path/coordinate semantics, control state reapplied after collision staging, atomic stable checkpoint publication, terminal completion/recovery order, no ordinary kill save IDs, and generated scene semantic scope. `git diff --check` succeeded. Root performs independent review of the exact resulting commit range; no extra reviewer was spawned.

## Changed files

Owned sources/tests: `scripts/save/PrologueSaveData.cs`, `scripts/save/PrologueSaveStore.cs`, `scripts/prologue/PrologueProgress.cs`, `scripts/prologue/PrologueCheckpointSnapshot.cs`, `scripts/prologue/CheckpointTrigger.cs`, `scripts/prologue/seamless/CanyonWorldLayout.cs`, `tools/build/prologue/CanyonContentBuilder.cs`, `scripts/prologue/seamless/SeamlessPrologueWorld.cs`, `scripts/prologue/PrologueLevelController.cs`, `scripts/prologue/PrologueGameFlowController.cs`, `scripts/ui/PrologueMainMenu.cs`, `tools/build/prologue/BuildPrologueLevel.cs`, `tests/unit/PrologueSaveTests.cs`, `tests/unit/PrologueProgressTests.cs`, `tests/integration/ContinuousCanyonMigrationTests.cs`, `tests/RunContinuousCanyonTests.cs`, `tests/integration/ContinuousCanyonSaveIsolationTests.cs`.

Generated scenes: `scenes/prologue/sectors/CanyonBasin.tscn`, `scenes/prologue/sectors/LostTemple.tscn`, `scenes/prologue/PrologueLevel.tscn`. This report is the only owned documentation edit. Root README/plan/brief/ledger edits, unrelated imports, `default_bus_layout.tres`, `tests/RunPrologueTests.cs`, user BasinAscentCamera files, existing artifacts, and accidental `--task5-lifecycle-fix-only/` remain unstaged by this worker.

## Limits

- This is automated contract/runtime evidence, not human visual/fun/playtime acceptance or proof of the later physical Boss-to-SafeExit requirement. Full completion remains the existing P0 ordered-objective flow.
- Tests exercise actual button bindings, receivers, physical checkpoint contacts and actual actor Health defeats, not hardware mouse/gamepad traversal through the whole world. The accepted continuous default suite separately retains traversal tests.
- The old unowned `PrologueGameFlowTests` fixture still authors pre-layout2 coordinates/layout0 saves; it was not rewritten or represented as current-layout evidence. Generated Task7 recovery/menu/current-Continue cases provide the in-scope P0 proof. `RunPrologueTests.cs` was not edited or invoked.
- Preserved malformed new-slot backups intentionally accumulate; no retention/deletion policy was added. Read/parse failures are kept nondestructive; filesystem write errors still surface to the caller rather than being silently hidden.
- Early compiler-process metadata limitation is explicitly documented above. No active compiler/Godot process is retained after handoff.

## Commit and process release

Implementation commit: `bf9ce4b3756ba58d13360dcc4ef3e2eb670edc14` (`feat: migrate prologue saves to canyon layout v2`), containing exactly the 20 owned source/test/generated paths above. This report is committed separately. The final source hashes match the final successful default-run evidence; report-only changes followed that run.

Final process inventory found no running pinned Godot engine or dotnet compiler process. Existing unrelated `godot-ai.exe` attach services were left untouched. All Task7 compiler/engine processes are exited and exclusive ownership is released to root. Unrelated tracked and untracked dirt remains preserved; no root-owned documentation was staged.
