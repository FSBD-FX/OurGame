# Task 6 Report — Twelve Challenges and 8+4 Encounter Layout

## Result

Implemented the exact normal-world encounter catalog: eight required main encounters, four optional branch encounters, and the Boss kept separate. Camp/rest regions contain no authored encounter. `canyon.v2.encounter.split-level` is the only normal encounter with `GatesProgression=true`; genuine Main 7 defeat supplies the existing Main7+crank seal prerequisite, while Main 8 remains an ordinary encounter inside the unlocked courtyard.

The generated six-scene chain was rebuilt leaf-to-parent with the existing builders. The final fixed-60 focused aggregate passes from the final source hash set.

## Implementation

- Added immutable encounter and placement definitions plus a validated twelve-entry catalog. Every two-enemy encounter uses distinct activation batches.
- Added `CanyonEncounterGroup`: bounds activation, inactive-batch mounting, named `Defeated` subscriptions, true-defeat batch advancement, one-shot group completion, progression persistence only for Main 7, and subscription-safe retirement/reset/remount. Living retirement never becomes a kill.
- Removed resident normal-world enemy instances and generated encounter-group nodes under each sector's `Gameplay/Encounters` container.
- Added the actual Basin/pre-Boss/safe-exit rest bounds contract. Main 8 retires before the pre-Boss rest and does not create a third courtyard lock.
- Kept Main 3 dormant during the incoming protected fall and required actual `CharacterBody2D.IsOnFloor()` contact at `AbyssSafeBottom` before activation.
- Kept the two Frozen groups on separate supports/arenas and preserved the right-side optional return bypass.
- Kept adjacent-group effective live count at two or fewer; inactive batches remain outside the tree.
- Added an inert `Marker2D` at the legacy `Gameplay/Actors/RustShellSquire` guidance path for the static target contract. It has no health, collision, AI, or encounter interface. The runtime guidance controller now resolves the real living `RustShellSquire` by type within the current Entrance Actors subtree, retaining all existing health/mode/viewport/LOS/range/hysteresis guards and layout-1 behavior.
- Moved the Main 5 first-batch Moth within its authored arena from `(4300,3100)` to `(5000,3000)`. This preserves the initial observation/grapple span and prevents its thread receiver from stealing the strict AscentGrip1 cursor. A runtime regression proves the mounted Moth remains idle at the initial lift approach and genuinely engages after progression to `(4700,3300)`.

No enemy HP, damage, timings, camera logic, claw priority/range, or Task 4 input oracle was changed.

## Test Coverage Added

- Exact `8 main + 4 optional`, route identity, approved scenes, bounds, camp/rest exclusion, Boss separation, Main7-only progression gate, Main8 non-gate, two-level split, separate Frozen arenas, and required per-enemy batch separation.
- Genuine `HealthComponent` defeat advances batches and emits `GroupDefeated` once. Reset, live retirement, and stale callbacks do not fabricate kills.
- Generated Main7 Laborer then Moth defeats provide the exact seal input; the unchanged crank supplies the second condition.
- Generated Main8 live enemy retires before the pre-Boss rest without defeat or persistence.
- Actual safe-bottom landing, adjacent Gear/Steam live cap, separate Frozen live groups/supports, right-side noncombat bypass, and Main5 observation-to-engagement behavior.
- Layout-2 first-combat guidance: marker alone false; nearby living generated Rust in Attack mode true; dead, retired, and other-sector Rust false.
- Generated structure checks reject backdrop/marker-only challenge nodes and require real gameplay witnesses.

## RED / GREEN Evidence

All evidence is under `E:/OurGame/Knight/.evidence/september09-continuous-canyon/`. Each case contains exact command text, actual hidden-process PID/exit/start/end, stdout/stderr, and source hashes. Later records serialize full paths and SHA-256 in JSON. The first interface RED's original table-form hash file is preserved even though PowerShell visually truncated paths.

### Planned RED

| Evidence directory | Actual process result | Expected observation |
|---|---:|---|
| `task6-catalog-interface-red-20260920T084047196-00dbff1b9303402491af0a4d8e971814` | dotnet `76068/1` | planned catalog/rest members missing |
| `task6-catalog-behavioral-red-20260920T084144029-865f61577b6041d58e1115ca8a997b56` | build `38232/0` → Godot `37496/1` | expected 8 main, actual 0 |
| `task6-group-interface-red-20260920T084532070-6989ef436f824e2d98ca558ba89b9fdd` | dotnet `73800/1` | planned group API missing |
| `task6-group-behavioral-red-20260920T084614320-384ef73f43634d17959585a7fa279a26` | build `42420/0` → Godot `70344/1` | activation did not mount batch |
| `task6-generated-structure-red-20260920T085147573-32e1e742d75f4c92a50a06c8b2072b1f` | build `34508/0` → Godot `38336/1` | generated Entrance expected one group, actual zero |
| `task6-guidance-live-actor-behavioral-red-20260920T091615378-0786d732f0e8408c930ba6182ba41c04` | build `57380/0` → Godot `72960/1` | genuine nearby generated Rust did not trigger combat cue |

The catalog behavioral RED put `--log-file` after `--`; Godot treated it as a user argument, so that case relies on redirected stdout/stderr plus actual PID/exit. It was not repeated merely to improve logging. The valid live-guidance RED and its first GREEN used the honest command `dotnet build Knight.csproj --nologo` before the build-flag drift was noticed; subsequent/final verification returned to `--no-restore -m:1 -p:UseSharedCompilation=false`.

### Focused GREEN and Diagnostics

| Evidence directory | Actual process result | Result |
|---|---:|---|
| `task6-catalog-green-20260920T084324479-3ee0c33c98174d5a9dc1ab674c7635ec` | `7232/0` → `7796/0` | catalog |
| `task6-generated-structure-green-20260920T085325483-f160ceef11544c9f8213e5959131bc30` | `51820/0` → `46324/0` | generated structure |
| `task6-encounter-runtime-green-20260920T085347565-e5668b2ee7e24801a20d282deb64c032` | `76240/0` → `37516/0` | genuine defeat/retirement runtime |
| `task6-frozen-bypass-red-diagnostic-20260920T085559753-f2a14afc1a50413ebd3b8a6ca714671a` | `77480/0` → `48952/1` | real right-side bypass was inside lower activation; also exposed render-frame vs physics-frame waiting |
| `task6-frozen-bypass-green2-20260920T085734701-85ef7584f4ac4d09b2b5bf8b0c3506d3` | `57436/0` → `38692/0` | narrowed activation and physics-frame test are green |
| `task6-runtime-seal-green-20260920T085903238-c6ecb7546f52441b972378fff77683c4` | `73740/0` → `76892/0` | genuine Main7 + crank seal seam |
| `task6-guidance-contract-green-20260920T090905748-91a2afe83b164fc9960d495903596ef9` | `43296/0` → `65188/0` | static guidance target contracts |
| `task6-guidance-live-actor-green-20260920T091703747-5cf5708e67704f30b384524a10471a0d` | `57172/0` → `38132/0` | live/dead/retired/other-sector guidance; empty stderr |
| `task6-main5-participation-green-20260920T092508421-7ae9c75f522644f4ac75854f75b442ca` | `4472/0` → `30176/0` | fixed-60 Main5 observation then genuine engagement |
| `task6-main5-placement-ascent-fixed60-green-20260920T092544975-2b25f1efeb6b4e23a0f01dc9c852e786` | `39292/0` → `2948/0` | unchanged strict Task4 ascent oracle |

Intermediate compile-safe shell iteration `task6-group-green-iteration1-*` failed build `64976/1` on a missing namespace; no engine ran. After correction, `task6-group-green-iteration2-*` built `69688/0` and Godot `53316/1` stopped only at the then-unbuilt generated Main8 group. This is retained as iteration evidence, not claimed as GREEN.

### Required Mutation Proofs

- Branch-required mutation: `task6-mutation-branch-required-20260920T090000050-36460fb5f68c436ab3dc32fe815e8b0f`, build `37332/0` → Godot `60560/1`, named `TASK6_BRANCH_REQUIRED_MUTATION` assertion (`expected main=8`, `actual=9`).
- Batch-collapse mutation: `task6-mutation-batch-collapse-20260920T090100387-7b7a03a4a28a4a108a7969af4a700629`, build `54252/0` → Godot `54312/1`, named `TASK6_BATCH_COLLAPSE_MUTATION` assertion (`expected 2`, `actual 1`).
- Both directories contain restoration hashes. Catalog hash after each restore was `966EEA5CA631E12C5FA7448C254BDFEE458A52DB6C0840F190DBC7F64B3513AB` at that source checkpoint.

### Builders and Final Aggregate

Final six-step builder evidence before the guidance-marker compatibility rebuild is `task6-final-leaf-parent-20260920T090334406-5450b3c6aeb64f039afe22cd8e381700`:

- Entrance build `37052/0` → builder `53088/0`
- Descent build `2176/0` → builder `48716/0`
- Basin build `50236/0` → builder `42968/0`
- Ascent build `24332/0` → builder `62404/0`
- Temple build `41792/0` → builder `64256/0`
- Parent build `42728/0` → builder `45452/0`

The marker change then rebuilt Entrance and parent in `task6-guidance-fix-builders-green-20260920T090827877-ed54513942934e68b50413cec336aa04`: Entrance `53616/0` → `60800/0`, parent `33940/0` → `38720/0`.

The first aggregate after actor migration (`task6-final-full-focused-20260920T090421634-*`, `20256/0` → `29144/1`) correctly exposed the static EntranceCombat target regression. After fixing that, the next aggregate (`task6-final-full-focused-20260920T091800026-*`, `75116/0` → `58564/1`) exposed a real Main5 Moth ThreadTarget intercepting Task4 AscentGrip1. Neither assertion was muted or waived.

Final evidence: `task6-final-full-focused-fixed60-20260920T092625601-2ec71bc9cfd1427aafa172ab9414721e`.

- Build exact command: `G:/godot/dotnet-sdk/dotnet.exe build Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo`
- Build actual: PID `40628`, exit `0`, `2026-09-20T09:26:25.6621587+08:00` to `09:26:26.7536484+08:00`.
- Engine exact command: `G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe --headless --fixed-fps 60 --path . --script res://tests/RunContinuousCanyonTests.cs --log-file <evidence>/godot.log`
- Engine actual: PID `72780`, exit `0`, `2026-09-20T09:26:26.7628255+08:00` to `09:26:50.1915557+08:00`.
- Final heading: `CONTINUOUS_CANYON_TESTS_OK`.
- Stderr: no errors; six inherited `Rejected prologue flow transition Title->Playing` warnings plus the intentional Task7 throwing-observer isolation warning. No new guidance target, typed-cast, or encounter warnings.

## Changed Files

Source/test ownership:

- `tools/build/prologue/CanyonEncounterDefinition.cs`
- `tools/build/prologue/CanyonEncounterCatalog.cs`
- `tools/build/prologue/CanyonContentCatalog.cs`
- `tools/build/prologue/CanyonContentBuilder.cs`
- `scripts/prologue/seamless/CanyonWorldLayout.cs`
- `scripts/prologue/CanyonEncounterGroup.cs`
- `scripts/prologue/guidance/PrologueGuidanceController.cs` (narrow root-approved compatibility extension)
- `tests/unit/CanyonEncounterCatalogTests.cs`
- `tests/integration/ContinuousCanyonEncounterTests.cs`
- `tests/unit/PrologueCanyonStructureTests.cs`
- `tests/RunContinuousCanyonTests.cs`

Generated scenes:

- `scenes/prologue/sectors/CanyonEntrance.tscn`
- `scenes/prologue/sectors/CanyonDescent.tscn`
- `scenes/prologue/sectors/CanyonBasin.tscn`
- `scenes/prologue/sectors/CanyonAscent.tscn`
- `scenes/prologue/sectors/LostTemple.tscn`
- `scenes/prologue/PrologueLevel.tscn`

No unrelated dirty import, audio layout, root plan/README/ledger, `RunPrologueTests.cs`, Basin camera test/probe, artifact, or accidental `--task5-lifecycle-fix-only` path is part of the scoped change.

## Concerns / Bounds

- The final aggregate is deterministic headless engine/runtime evidence, not rendered visual proof or a human-played combat-clear recording.
- The genuine defeat tests damage actual generated actors through `HealthComponent` and verify real signals/progression. The inherited full traversal remains explicitly a prerequisite fixture and is not represented as genuine combat proof.
- No Task6 process remains. A post-run process inventory found no live `dotnet`, `VBCSCompiler`, `MSBuild`, or pinned Godot process. Existing unrelated `godot-ai` helpers were observed and intentionally untouched.

## Independent Review Fix Round 1 (2026-09-20)

Review base was `907ec5cfef776aa839ef0141d77af52566a4e293`. The bounded independent review identified two Important findings and one root-resolved integration gap. Source fix commit is `d53b7a1` (`fix: close task6 encounter review gaps`).

### Corrections

- Moved only the Main6/Main7 activation strips onto ordinary ascent ingress: Main6 is now `(4380,2580) size (400,210)` and Main7 `(4380,1760) size (400,230)`. Arena bounds, enemy placements, observation spans, encounter count, enemy pressure, Main7 gate identity, and all combat/claw values are unchanged.
- Added an engine-driven regression that begins on the accepted preceding physical supports, selects Grapple through input, resolves the unchanged physical grip, performs the ordinary grapple, crosses the activation rectangle, and observes a live first batch. It covers both `AscentGrip3 -> AscentGrip4` and `AscentGrip6 -> AscentGrip7`; it neither places the player in the trigger nor pre-completes Main7.
- Replaced witness existence checks with typed gameplay contracts: physical routes require real enabled collision shapes; mechanism routes require their concrete components and relevant collision/thread pairings; Boss Hall requires a configured `BossArenaController`, boss root, trigger, and both door collisions. A same-name inert `Marker2D` replacement now fails by construction.
- Deferred next-batch mounting past physics query flush. A lifecycle generation and pending flag cancel stale deferred work during checkpoint reset, arena retirement, completion, and exit. The real `Player/Combat/AttackHitbox` `AreaEntered` path now defeats the final Rust actor and mounts the Laborer batch without changing collision monitoring during the query flush.
- Added reset and retirement races between final-actor defeat and the deferred callback; neither can resurrect the next batch.
- Updated the pre-existing generated Main7 progression fixture from the old activation sample `(4100,1800)` to `(4500,1800)`, inside the corrected strip. This fixture remains separate from the unforced ingress proof.

No enemy HP/damage/timing, camera, claw priority/range, Task4 traversal oracle, scene geometry, or generated scene changed. The review's minor reset-origin observation remains deliberately deferred; this round did not change spawn transform ordering.

### Fix-Round RED Evidence

Every recorded case used the fresh build command immediately before Godot:

`G:/godot/dotnet-sdk/dotnet.exe build Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo`

Godot commands used the pinned executable with `--headless --fixed-fps 60 --path . --script res://tests/RunContinuousCanyonTests.cs --log-file <evidence>/godot.log`, followed by `--` and the focused user flag where applicable.

| Evidence directory / exact focused flag | Actual build -> engine | Actual output |
|---|---:|---|
| `task6-fix1-contact-behavioral-red-20260920T095450314-9210e633e76c44a8aef2f10cf525ca55` / `--task6-contact-only` | `31320/0` -> `42888/1` (`09:54:50.377`-`09:55:00.310`) | real `Hitbox.AreaEntered` emitted `Can't change this state while flushing queries` and failed the deferred-mount assertion |
| `task6-fix1-main-route-activation-red-20260920T095525986-6c91f16218894b52a57c4933aedfce6c` / `--task6-activation-only` | `58008/0` -> `75688/1` (`09:55:26.045`-`09:55:28.072`) | `ordinary unforced AscentGrip3->AscentGrip4 ... activation` failed against the old strip |
| `task6-fix1-witness-negative-red-20260920T095746523-775c1470b49b4739a48e8a19d620e478` / `--task6-witness-negative-only` | `49976/0` -> `67268/1` (`09:57:46.559`-`09:57:48.221`) | `an inert same-name marker must fail` failed while the shell still used node existence |

The earlier `task6-fix1-contact-behavior-*` setup case (`76488/0 -> 45072/1`) was invalid because its too-small arena retired the player/actor fixture. The corrected diagnostic (`69176/0 -> 58156/0`) still contained query-flush errors despite exit zero and was not accepted as GREEN; the valid RED above added the explicit assertion.

Several directories containing `activation-green` in their provisional names are failures, not GREEN. `activation-green` (`3656/0 -> 52312/1`), `green2` (`46732/0 -> 66112/1`), `green3` (`44460/0 -> 24644/1`), `green4` (`75584/0 -> 43128/1`), `green5` (`9612/0 -> 73836/1`), and `green6` (`44812/0 -> 52492/1`) all exited one. The process diagnostic (`72928/0 -> 54928/1`) recorded `inside=True`, `physics=True`, `can=True`, all ancestor modes `Inherit`, `paused=False`, and `active=True`; it proved activation was working and isolated the failure to an unrelated provisional `>80px` displacement conjunct. The final oracle instead requires the actual attached grapple to cross the rectangle and the engine-driven group to activate. One transient diagnostic directly invoked the lifecycle tick and exited zero, but did not retain a child PID; it is not cited as proof and the direct call was removed.

### Fix-Round GREEN / Final Evidence

| Evidence directory / exact focused flag | Actual build -> engine | Actual output |
|---|---:|---|
| `task6-fix1-contact-green-20260920T100006733-f8ddfb17f41845b68141f1998f5da9fa` / `--task6-contact-only` | `69540/0` -> `62016/0` (`10:00:06.770`-`10:00:08.620`) | `TASK6_PLAYER_HITBOX_BATCH_TRANSITION_OK`; empty diagnostic headings |
| `task6-fix1-witness-green-20260920T101346080-afbf9e8f84c64b048dfc2b08cd25b85a` / `--task6-witness-negative-only` | `57656/0` -> `51516/0` (`10:13:46.106`-`10:13:47.548`) | `TASK6_INERT_WITNESS_NEGATIVE_OK` |
| `task6-fix1-activation-green-final-20260920T101646447-cd0c5797dad34760851cab46b11e64a0` / `--task6-activation-only` | `68420/0` -> `72072/0` (`10:16:46.474`-`10:16:55.833`) | `TASK6_MAIN_ROUTE_ACTIVATION_OK main6=True main7=True input_grapple=True` |
| `task6-fix1-encounters-aggregate-green-20260920T101852473-3601be9c3c0c4d379e234ebda20d46c1` / `--task6-encounters-only` | `42284/0` -> `53380/0` (`10:18:52.499`-`10:19:01.828`) | catalog, contact, both route activations, and continuous encounter aggregate all OK |
| `task6-fix1-strict-ascent-20260920T101954248-33b61b9f5a58457da08f3a8dd3d1f2c8` / `--ascent-only` | `45512/0` -> `70608/0` (`10:19:54.276`-`10:20:01.417`) | unchanged strict input route reached `TASK4_LIFT_PENDULUM_SPLIT_COURTYARD_INPUT_OK` |
| `task6-fix1-structure-contracts-final-20260920T102115177-70f64a0acaeb4f3da3dc4224aac9c9d2` / `--task6-structure-only` | `39112/0` -> `10140/0` (`10:21:15.204`-`10:21:23.793`) | typed witnesses, inert replacement rejection, and exact generated 12-group structure all OK |
| `task6-fix1-final-full-focused-20260920T102144892-a6060204f6e5487592e7cc68ada762b9` / no user flag | `66676/0` -> `48380/0` (`10:21:44.918`-`10:22:05.398`) | `CONTINUOUS_CANYON_TESTS_OK`; exactly six inherited rejected-transition warnings plus one intentional Task7 observer warning, no errors |

All these evidence directories contain full-path SHA-256 JSON plus exact command, stdout/stderr, and actual process records. Covering test files are `tests/integration/ContinuousCanyonEncounterTests.cs`, `tests/unit/PrologueCanyonStructureTests.cs`, and `tests/RunContinuousCanyonTests.cs`; production coverage is against `scripts/prologue/CanyonEncounterGroup.cs` and `tools/build/prologue/CanyonEncounterCatalog.cs`.
