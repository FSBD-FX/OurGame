# Task 4 Report — readable mechanical-thread delivery

Date: 2026-08-22
Branch: `codex/player-art-pipeline`

## Result

Task 4 is implemented. The production Player now serializes the real programmatic
`ThreadShuttleVisual` at the stable path `VisualRoot/ThreadShuttle`; the Prologue formal HUD reads
the live `ThreadAbility` and gives compact localized Ready, Preview, Windup, Travel, Latched,
Apply, Recovery, Failed, and Cooldown feedback. The actual critical-path smoke and presentation
drive real InputMap actions, physics, authored targets, and an inserted World collision instead of
writing runtime cast state directly.

The automated release gate, fresh PNG-frame capture, and fresh 60 FPS AVI capture are green. Dense
frame review confirms that preparation, shuttle flight, latch, effect, recovery, both failure
cases, and cleanup are separately visible. A fresh human normal-speed playthrough remains an
explicit subjective-review boundary; this report does not equate automated movie playback with
that review.

## RED contracts

Tests were added before production changes and observed these real failures:

```text
HUD contract:
  CS1061: PrologueHudViewModel did not contain ThreadState / ThreadFailure /
  ThreadStateText / WithThreadCast

Stable production scene:
  Player/VisualRoot/ThreadShuttle was missing

Live HUD integration:
  expected ThreadState=Preview, actual=Idle

Presentation verifier:
  expected exactly one PROLOGUE_THREAD_PRESENTATION_OK state=Preview, actual count=0
```

These failures covered the data model, stable node serialization, runtime binding, and fail-closed
evidence contract rather than only testing helper code.

## GREEN implementation

- `BuildPlayer` creates and validates a real `ThreadShuttleVisual` named `ThreadShuttle` before
  `Effects`. The generated Player scene preserves the stable node path and script resource.
- `PrologueHudViewModel` exposes `ThreadState`, `ThreadFailure`, and `ThreadStateText` with the
  production compact Chinese labels: `机械丝就绪`, `瞄准目标`, `线轴蓄能`, `丝梭飞行`,
  `锁扣咬合`, `牵引生效`, `机械丝回收`, `未发现目标`, `路径受阻`, `目标过远`, `目标失效`,
  `施放取消`, and `线轴回充`.
- `PrologueFormalHud` resolves the live ability through the stable default path
  `../../PlayerRoot/Player/ThreadAbility`, updates only when the cast frame or ready ratio changes,
  and retains the existing HUD and guidance layout.
- `BuildPrologueLevel` validates the stable Player shuttle and formal-HUD paths. It adds to the
  existing Godot arrays instead of replacing wrapper instances and drains managed finalizers
  before editor shutdown.
- Smoke helpers expose `TapThread()` and
  `HoldAimAndReleaseThread(Vector2 aimDirection, int holdPhysicsFrames)`. They press and release
  the real InputMap actions in Godot physics order.
- The critical-path smoke proves neutral mechanism Tap success without horizontal movement,
  hold-aim anchor success, named World-wall `Obstructed` failure with zero effect, real light-enemy
  Apply, and no line/shuttle residue after room transition or death.
- The presentation subscribes to real `ThreadAbility.CastFrameChanged`, records actual frames and
  CastIds, and drives a no-target Tap, an authored-anchor Hold, and a real `StaticBody2D` wall. It
  requires ordered Preview -> Windup -> Travel -> Latched -> Apply -> Recovery and separate
  `NoTarget` / `Obstructed` failures while retaining the existing boss sequence and visible tail.
- `verify-prologue.ps1` requires every new smoke and presentation marker exactly once and retains
  fail-closed scanning for engine, script, parse, ObjectDB, resource, and RID failures across both
  Godot log files and merged console output.

## Mutation proof

After GREEN, the production `Obstructed` label was temporarily changed from `路径受阻` to
`故意破坏的路径反馈`. The real Prologue test runner failed at the intended contract:

```text
expected=路径受阻
actual=故意破坏的路径反馈
```

The production string was restored immediately. The same runner subsequently returned
`PROLOGUE_TESTS_OK`.

## Shutdown-failure debugging evidence

Repeated builder and forced-failure probes exposed a native process exit of `-1073741819`
(`0xC0000005`) during `Godot.Collections.Array` finalization after native engine teardown. This was
not accepted as a flaky pass.

- Before the builder fix, three repeated runs produced two native crashes.
- `BuildPrologueLevel` now mutates the existing array wrappers and explicitly forces managed
  collection/finalizers before `Quit`; five consecutive builder runs then exited 0.
- Smoke teardown now frees the complete mounted level and drains array wrappers before quitting.
  A forced-failure probe that could previously crash then exited exactly 1 in three consecutive
  runs, printed only the forced-failure marker, and emitted no fatal shutdown signature.
- The import phase now uses bounded headless editor shutdown (`--editor --quit-after 120`) so scan
  and managed cleanup complete before native teardown. The full verifier passed this path.

## Fresh verification

All commands below were run against the final production source with Godot 4.7.1 Mono at
`G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe`.

```text
Godot ... --headless --path . --script res://tools/build/BuildPlayer.cs
  exit 0

Godot ... --headless --path . --script res://tools/build/prologue/BuildPrologueLevel.cs
  exit 0

G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --no-restore
  0 warnings
  0 errors

Godot ... --headless --path . --script res://tests/RunGraveLampMothRuntimeTests.cs
  GRAVE_LAMP_MOTH_RUNTIME_OK

Godot ... --headless --path . --script res://tests/RunPrototypeTests.cs
  PROTOTYPE_TESTS_OK

Godot ... --headless --path . --script res://tests/RunPrologueTests.cs
  PROLOGUE_TESTS_OK

Godot ... --headless --path . --script res://tests/smoke/PrologueCriticalPathSmoke.cs
  PROLOGUE_CRITICAL_PATH_OK

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
  PROLOGUE_VERIFY_OK

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly
  PROLOGUE_CAPTURE_OK

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1
  PROLOGUE_CAPTURE_OK
```

The full verifier rebuilt twenty generated resources/scenes, imported with a bounded editor run,
built C# with 0 warnings / 0 errors, ran focused/unit/integration suites, the actual critical-path
smoke, presentation, production/UI/visual audits, teardown probes, and the intentional forced
failure. The production audit reported `max_process=26.47ms` and `max_nodes=362`.

Required smoke markers, each present exactly once:

```text
PROLOGUE_THREAD_SMOKE_HOLD_OK
PROLOGUE_THREAD_SMOKE_OBSTRUCTED_OK
PROLOGUE_THREAD_SMOKE_ROOM_CLEANUP_OK
PROLOGUE_THREAD_SMOKE_TAP_OK
PROLOGUE_THREAD_SMOKE_DEATH_CLEANUP_OK
PROLOGUE_THREAD_SMOKE_OK
```

Required presentation markers, each present exactly once:

```text
PROLOGUE_THREAD_PRESENTATION_OK state=Preview
PROLOGUE_THREAD_PRESENTATION_OK state=Windup
PROLOGUE_THREAD_PRESENTATION_OK state=Travel
PROLOGUE_THREAD_PRESENTATION_OK state=Latched
PROLOGUE_THREAD_PRESENTATION_OK state=Apply
PROLOGUE_THREAD_PRESENTATION_OK state=Recovery
PROLOGUE_THREAD_PRESENTATION_OK failure=NoTarget
PROLOGUE_THREAD_PRESENTATION_OK failure=Obstructed
PROLOGUE_THREAD_PRESENTATION_SEQUENCE_OK
```

## Capture evidence and visual review

The fresh PNG-frame and AVI captures use the same deterministic presentation at
1280x720, 60 FPS:

| Proof | Frames | Duration | AVI size |
| --- | ---: | ---: | ---: |
| `screenshots/prologue/prologue.avi` | 2280 | 38.00 s | 122,301,610 bytes |
| `screenshots/prologue/boss-retry.avi` | 600 | 10.00 s | 34,621,900 bytes |

Exact and dense sequential PNG frames were opened with the local image viewer:

- frame 618: red/orange `未发现目标`, with no line or shuttle residue;
- frames 638–649: cyan Preview line and `瞄准目标` become visible before allocation;
- frame 650: gold Windup line with `线轴蓄能`;
- frames 654–660: `丝梭飞行`, with the cyan shuttle visibly advancing along the line;
- frame 667: `锁扣咬合`, with the line fully attached to the anchor;
- frame 672: `牵引生效`, with visible player displacement;
- frame 675: `机械丝回收`, with line fading/retracting;
- frames 682–684: Ready and fully clean, with no line or shuttle residue;
- frame 700: red `路径受阻` and a small red obstruction mark, with zero target effect;
- frame 701: Ready and clean again;
- frames 2277–2279: identical stable title tail, with no teardown/gray frame.

This establishes preparation -> flight -> latch -> effect -> recovery separation and cleanup at
deterministic 60 FPS. It is objective capture evidence only; human normal-speed comfort,
readability, and taste remain to be judged in a fresh play session.

## Scope and concerns

The commit allowlist is limited to the Task 4 builder, HUD, smoke, presentation, verifier, durable
README status, the semantic Player scene addition, and this report. The regenerated
`PrologueLevel.tscn` changed only editor-generated resource identifiers, so it is intentionally not
staged. Unrelated room, art, audio, import, UID, screenshot, and other dirty worktree files remain
untouched and unstaged.

No automated release blocker remains. The only open boundary is a fresh human normal-speed
playthrough of the 60 FPS proof or running build; any resulting timing/taste feedback belongs to
the next iteration rather than being inferred from frame inspection.

## Fix Round 1 — fail-closed evidence, failure cleanup, and attempt identity

Date: 2026-08-22

### Review findings addressed

- Ordinary and forced-failure evidence now use the same explicit fail-closed signature list. It
  rejects engine/script/parse errors, ObjectDB/resource/RID leaks, fatal output, unhandled or
  native exceptions, crash-handler/crash signatures, access violations, `0xC0000005`, Windows
  native exit `-1073741819`, unexpected process termination, and segmentation faults.
- The full verifier checks both the Godot log and merged console for the intentional forced
  failure. Exit code 1 and exactly one `FORCED_SMOKE_FAILURE` are required; no second fatal,
  native, access-violation, or RID signature is permitted in either stream.
- `BuildPrologueLevel` drains managed wrappers from a common `finally` path, including pack,
  save, and validation failures. It preserves the original named failure and nonzero exit.
- Presentation evidence is bound to explicit attempts started immediately before each real
  InputMap press. Preview through Recovery must belong to the same Anchor attempt; NoTarget and
  Obstructed must belong to their own distinct attempts. The attempt token only annotates
  observed runtime events and never writes a gameplay state.

### RED evidence

The focused PowerShell contract was first extended with fatal/native/RID fixtures. Before the
shared filter existed it failed as intended:

```text
Assertion failed: ordinary Godot log fatal signature is rejected by the fail-closed evidence gate
exit 1
```

A real temporary builder mutation changed the expected `RequiredRooms` count from six to seven.
Before cleanup was moved to a common lifecycle path, five consecutive runs all ended in the
native teardown crash instead of the requested managed failure:

```text
BuildPrologueLevel contract validation failed.
Fatal error. 0xC0000005
godotsharp_array_destroy
Godot.Collections.Array.Finalize
exit -1073741819 (5/5)
```

The presentation mutation `KNIGHT_PRESENTATION_SPLICE_THREAD_ATTEMPT=1` uses real input: attempt A
reaches Preview and is aborted by the real Dash action, then attempt B performs the complete cast.
Before attempt identity was validated, this spliced evidence incorrectly passed:

```text
PROLOGUE_THREAD_PRESENTATION_SEQUENCE_OK
exit 0
```

### GREEN and mutation restoration

After the changes:

- `PrologueEvidenceChecks.Tests.ps1` returns `PROLOGUE_EVIDENCE_CHECKS_OK`. Its ordinary Godot
  log, ordinary merged console, forced Godot log, and forced merged console fixtures each reject
  representative fatal/native/RID text.
- With the same six-versus-seven builder validation mutation, five consecutive runs exited
  exactly 1, retained `BuildPrologueLevel contract validation failed.`, and had zero
  C0000005/fatal/native/RID matches in both streams. After restoring the six-room contract, five
  consecutive builder runs exited 0 with the named success marker and clean streams.
- The real-input attempt-splice mutation now exits 1 with the intended diagnostic:

```text
mechanical-thread presentation attempt splice at Preview:
evidence=Anchor/2, expected=Anchor/3
```

  Its log and merged console contain only the intended presentation failure and no fatal/native/
  RID teardown signature. The mutation was removed from the environment; the mutation hook stays
  as a deterministic regression probe, and the normal presentation is green.

### Final verification

The following were run fresh against the restored production source with Godot 4.7.1 Mono:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\PrologueEvidenceChecks.Tests.ps1
  PROLOGUE_EVIDENCE_CHECKS_OK

G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --no-restore
  0 warnings
  0 errors

Godot ... --headless --path . --script res://tests/RunPrologueTests.cs
  PROLOGUE_TESTS_OK

Godot ... --headless --path . --script res://tests/smoke/PrologueCriticalPathSmoke.cs
  PROLOGUE_THREAD_SMOKE_HOLD_OK
  PROLOGUE_THREAD_SMOKE_OBSTRUCTED_OK
  PROLOGUE_THREAD_SMOKE_ROOM_CLEANUP_OK
  PROLOGUE_THREAD_SMOKE_TAP_OK
  PROLOGUE_THREAD_SMOKE_DEATH_CLEANUP_OK
  PROLOGUE_THREAD_SMOKE_OK
  PROLOGUE_CRITICAL_PATH_OK

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
  PROLOGUE_VERIFY_OK
  production audit max_process_ms=16.23, max_nodes=362

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly
  PROLOGUE_CAPTURE_OK

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1
  PROLOGUE_CAPTURE_OK
```

The fresh full verifier rebuilt/imported the production resources, built C# 0/0, ran the full
Prologue runner, teardown probes, actual smoke, presentation, four UI resolutions, visual and
production audits, and intentional forced failure. Every one of the nine required mechanical-
thread presentation markers and six required thread-smoke markers appears exactly once. A manual
shared-filter check also passed for presentation and smoke log/console pairs and for both forced-
failure streams (`FIX1_MARKER_AND_STREAM_SCAN_OK`).

### Fresh capture and visual inspection

| Proof | Dimensions / FPS | Frames | Duration | AVI size |
| --- | --- | ---: | ---: | ---: |
| `screenshots/prologue/prologue.avi` | 1280x720 / 60 | 2280 | 38.00 s | 122,301,534 bytes |
| `screenshots/prologue/boss-retry.avi` | 1280x720 / 60 | 600 | 10.00 s | 34,621,900 bytes |

Exact frames opened with the local image viewer were 618, 638, 650, 660, 667, 672, 675, 684,
700, 701, and 2277–2279. They show, respectively: named no-target failure; Preview; Windup;
shuttle visibly in flight; Latched; visible Apply displacement; retracting Recovery; Ready with no
residue; named wall obstruction; Ready with no line/shuttle/X residue; and three consecutive,
fully visible title-menu tail frames. This objectively verifies preparation -> flight -> latch ->
effect -> recovery separation and cleanup at deterministic 60 FPS.

### Scope and remaining boundary

Fix Round 1 changes only the shared evidence checks and focused fixtures, full verifier, Prologue
builder lifecycle, presentation attempt evidence, and this report. Generated scenes have no
semantic Fix Round 1 change and are not staged. README delivery truth did not change, so README is
not updated. Unrelated room/art/audio/import/UID/screenshot and existing dirty files remain
untouched and unstaged.

The automated gate has no remaining blocker. A fresh human normal-speed playthrough remains the
honest subjective boundary; frame inspection and automated 60 FPS capture are not claimed as a
human comfort/readability judgment.

### Fix Round 1 follow-up — deterministic Mono editor import teardown

The first full verifier run after commit `cc25f77` correctly failed closed during its editor-import
phase. The subprocess returned `-1073741819` and the merged console contained:

```text
Fatal error. 0xC0000005
Godot.NativeInterop.NativeFuncs.godotsharp_array_destroy
Godot.Collections.Array.Finalize
```

This was investigated rather than hidden by a retry. Reproduction probes showed:

- existing `--editor --quit-after 120`: one of one crashed;
- `--import`: the first run passed and the second crashed;
- `--editor --quit`: one of one crashed;
- explicitly opening a lightweight visual scene: one of one crashed;
- temporarily clearing editor `open_scenes` and `current_scene`: one of one crashed;
- recovery mode avoided that native stack but emitted a different Godot C# HotReload timer
  `ERROR`, so it also failed the evidence gate.

Those results ruled out the Prologue scene, saved editor layout, and import CLI spelling. The
common failure was Godot Mono editor native teardown occurring before outstanding managed Array
wrappers finalized. A temporary C# `EditorPlugin` probe that drains finalizers from `_ExitTree`
then produced five consecutive clean exits. Because the original Task 4 file list did not include
an editor plugin or `project.godot`, the task owner explicitly authorized this minimal scope
extension before it was retained.

The permanent `VerificationGcDrain` is compiled only for `TOOLS`, derives only from
`EditorPlugin`, and has one override: `_ExitTree`. It immediately returns unless
`DisplayServer.GetName()` equals `headless` with ordinal comparison. It has no process/physics
callback, child node, runtime autoload, scene reference, or interactive-editor action. On the
headless editor exit path it performs Collect -> WaitForPendingFinalizers -> Collect and emits the
single evidence marker `VERIFICATION_EDITOR_GC_DRAIN_OK`. The verifier requires that marker
exactly once and still rejects every fatal/native/RID signature in both streams.

The structural contract was observed RED before implementation:

```text
Assertion failed: headless editor GC-drain plugin descriptor exists
exit 1
```

Final GREEN evidence:

```text
PrologueEvidenceChecks.Tests.ps1
  PROLOGUE_EVIDENCE_CHECKS_OK

dotnet build Knight.csproj --no-restore
  0 warnings, 0 errors

headless editor import probe, five independent runs
  IMPORT_FINAL_PROBE_OK run=1..5 exit=0 marker=1 streams=clean

tools/verify-prologue.ps1
  VERIFICATION_EDITOR_GC_DRAIN_OK
  PROLOGUE_TESTS_OK
  PROLOGUE_THREAD_PRESENTATION_SEQUENCE_OK
  PROLOGUE_THREAD_SMOKE_OK
  PROLOGUE_CRITICAL_PATH_OK
  PROLOGUE_VERIFY_OK
```

A post-gate scan found the drain marker only in `verify-prologue-import.log` and its merged console
copy (`import_streams_only=2`), with zero runtime-scene references. Thus the lifecycle fix does not
weaken fail-closed evidence and does not change the playable game, capture, HUD, or presentation
truth recorded above. README status remains unchanged.

## Fix Round 2 — anchored forced-error line

Date: 2026-08-22

### Critical finding and RED

`Assert-OnlyExpectedEngineError` previously treated an entire physical line as allowed whenever
the line contained `FORCED_SMOKE_FAILURE`. That let a native failure hide after the token on the
same line, for example:

```text
ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE Fatal error. 0xC0000005
```

Four focused fixtures were added: forced Godot log plus forced merged console, each with a
same-line Fatal/0xC0000005 case and a same-line RID-allocation case. Before the filter change, the
first mutation false-passed the filter and the focused suite produced the intended RED:

```text
Assertion failed: forced Godot log same-line fatal access violation cannot hide behind the
allowed forced-failure token
exit 1
```

This directly demonstrates the previous same-line masking rather than relying on an inferred
regex weakness.

### GREEN implementation

The only permitted forced-error physical line is now anchored to Godot's real emitted form:

```text
ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE
```

Leading/trailing whitespace and normal spacing between the three fields are allowed. Appending a
fatal, native exception, RID leak, another engine/script error, or any other content prevents the
line from matching the allowed form; because the line still matches the shared fail-closed
patterns, it is independently classified as unexpected. A second exact expected line also fails
the exactly-once count. Existing separate-line and ordinary-output rejection remain unchanged.

### Fresh verification

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\PrologueEvidenceChecks.Tests.ps1
  PROLOGUE_EVIDENCE_CHECKS_OK

G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --no-restore
  0 warnings, 0 errors

actual forced PrologueCriticalPathSmoke
  exit 1
  exact expected forced line=1
  Godot log + merged console clean apart from that line

actual successful PrologueCriticalPathSmoke
  exit 0
  PROLOGUE_CRITICAL_PATH_OK exactly once
  Godot log + merged console clean

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
  PROLOGUE_EVIDENCE_CHECKS_OK
  PROLOGUE_TESTS_OK
  PROLOGUE_THREAD_PRESENTATION_SEQUENCE_OK
  PROLOGUE_THREAD_SMOKE_OK
  PROLOGUE_CRITICAL_PATH_OK
  PROLOGUE_VERIFY_OK
```

### Scope and capture decision

Fix Round 2 changes only the shared forced-error assertion, its focused PowerShell fixtures, and
this report. The deferred GC-drain marker-scope minor is intentionally not changed. No gameplay,
presentation, scene, HUD, input, physics, or capture code changed, so a new movie/PNG recapture
would be identical and was not run. The last fresh reviewed evidence remains valid: Prologue AVI
1280x720 at 60 FPS, 2280 frames/38 seconds; Boss retry AVI 600 frames/10 seconds; and exact frame
review at 618, 638, 650, 660, 667, 672, 675, 684, 700, 701, and 2277–2279. README delivery truth
also remains unchanged.
