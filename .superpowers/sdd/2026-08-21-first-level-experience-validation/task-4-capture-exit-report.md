# Task 4A — deterministic movie capture teardown report

## Root cause

The reviewed candidate `7c10c94` produced the complete prologue movie and
`PROLOGUE_PRESENTATION_COMPLETE frame=2989`, but the process then exited with
`-1073741795` (`0xC000001D`) in
`GodotObject.Finalize -> godotsharp_internal_refcounted_disposed`. Both movie
drivers previously drained their `PrologueAudioDirector` and immediately called
`Quit(0)`. The boss-guidance route allocates transient managed Godot wrappers
while collecting protection regions; they could therefore survive until native
process teardown. The working UI, visual-audit, and editor-only teardown paths
already establish the required managed collection/finalizer-drain pattern.

## RED

Before changing production capture code, I added the focused boundary fixture:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/powershell/Task4CaptureExit.ps1 -SkipCapture
```

Expected and observed failure:

```text
Missing centralized managed-wrapper drain before capture Quit.
TASK4_RED_EXIT=1
```

This fixture inspects the actual two presentation success paths and the capture
gate, then runs a real fixed-FPS prologue AVI capture when not skipped. It also
constructs a source variant with the prologue drain removed and requires that
the focused boundary contract reject it.

## Implementation

- Added `TestSceneTeardown.DrainManagedWrappersBeforeQuit(capture)`: two
  `GC.Collect()` / `GC.WaitForPendingFinalizers()` cycles, followed only then
  by `PROLOGUE_CAPTURE_TEARDOWN_OK capture=<name>`.
- Both presentation drivers call that one shared routine immediately before
  their successful `Quit(0)`. The level remains inside the tree through the
  visible final frame; no gameplay, timing, frame count, or scene behavior was
  changed.
- `tools/capture-prologue.ps1` requires the exact teardown marker once for both
  captures in addition to the existing exit, log, AVI, dimension, FPS, and
  frame-count checks.
- During the first full capture run, its long-lived PowerShell session lost
  command resolution for the platform `Get-FileHash` cmdlet in the UI PNG
  validation phase, despite it resolving in a fresh shell and after explicit
  module import. The owned capture script now supplies the same deterministic
  SHA-256 result shape locally, so the existing hash contract stays strict and
  is independent of that session-level module-resolution failure.

## GREEN evidence

Focused real boundary:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/powershell/Task4CaptureExit.ps1
```

```text
TASK4_CAPTURE_EXIT_GREEN_OK capture=prologue frames=2989 dimensions=1280x720 fps=60
```

The focused movie log contains exactly one each of:

```text
PROLOGUE_PRESENTATION_COMPLETE frame=2989
PROLOGUE_CAPTURE_TEARDOWN_OK capture=prologue
```

Full required movie gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/capture-prologue.ps1
```

```text
PROLOGUE_CAPTURE_TEARDOWN_OK capture=prologue
PROLOGUE_CAPTURE_TEARDOWN_OK capture=boss-retry
PROLOGUE_CAPTURE_OK
```

The fresh AVIs pass the existing contracts:

| Capture | Contract |
| --- | --- |
| prologue | 2,989 frames, 1280x720, 60 FPS |
| boss-retry | 600 frames, 1280x720, 60 FPS |

Both capture logs have exactly one completion marker and exactly one teardown
marker. The full capture process exits zero and reports no engine-log errors.

Build:

```powershell
G:/godot/dotnet-sdk/dotnet.exe build Knight.csproj --no-restore
```

```text
0 warnings
0 errors
```

Full regression verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify-prologue.ps1
```

```text
PROLOGUE_VERIFY_OK
```

No human play session was launched.

## Concerns

None remaining. The capture-hash provider is intentionally local to the
existing capture tool and returns the same SHA-256 values the prior evidence
gate consumed; it does not relax output freshness, AVI, exit-code, marker, or
engine-error checks.

## Fix round 1 correction and evidence (2026-08-25)

### Correction to the submitted RED claim

The prior `TASK4_RED_EXIT=1` report line was inaccurate.  It described a
source-only assertion failure, not a marker/output emitted by a runnable Godot
fixture.  It is superseded by the retained runtime evidence below.  This round
does not claim that the original native teardown failure reproduced locally.

`tests/powershell/Task4CaptureExit.ps1` now snapshots the exact bytes of the
real `ProloguePresentation.cs`, removes only its real pre-`Quit(0)` drain,
rebuilds, and starts a fixed-60-FPS movie capture through `Start-Process` so
the observed `Process.ExitCode` is not a pipeline artifact.  `finally` writes
the byte snapshot back, compares every byte, verifies the SHA-256, and rebuilds
the restored source.  If Godot exits zero, the test fails closed with:

```text
No-drain movie capture exited zero; marker absence alone is not accepted as a teardown regression.
```

Two direct real no-drain movies reached the actual completion boundary but did
not reproduce the native crash on this machine.  The second retained run is:

```text
Godot exit: 0
movie: screenshots/task4-capture-exit/no-drain-prologue-20260824165621.avi
log:   screenshots/task4-capture-exit/no-drain-prologue-20260824165621.log
console: screenshots/task4-capture-exit/no-drain-prologue-20260824165621.console.log
PROLOGUE_PRESENTATION_COMPLETE frame=2989 count: 1
PROLOGUE_CAPTURE_TEARDOWN_OK capture=prologue count: 0
```

The resulting AVI is 144342304 bytes (`SHA-256
8d51ed1f82cd6a47b18cda8da690486ac713bbabf7826336bd774ad0e2e84ec8`).
The mutation's `finally` proof emitted
`TASK4_NO_DRAIN_SOURCE_RESTORED` with the exact snapshot SHA-256 and rebuilt
the restored production source.  The direct wrapper output is retained at
`C:\Users\22499\AppData\Local\Temp\task4-round1-red-direct-20260824165359.stdout.log`
and the bounded wrapper-pressure retry at
`C:\Users\22499\AppData\Local\Temp\task4-round1-red-lease-20260824165621.stdout.log`.
Both report actual Godot exit `0`, not a fabricated native failure.

With controller approval, a capture-only test fixture then retained 16,384
real `Godot.RefCounted` wrappers until `Quit(0)`.  Its single bounded no-drain
movie also exited `0`:

```text
TASK4_REFCOUNTED_STRESS_READY wrappers=16384
TASK4_REFCOUNTED_STRESS_NO_DRAIN_QUIT wrappers=16384
exit=0
log: screenshots/task4-capture-exit/refcounted-stress-no-drain-20260824170437.log
console: screenshots/task4-capture-exit/refcounted-stress-no-drain-20260824170437.console.log
```

No further pressure/retry was performed.  The paired drained fixture exited
`0` and emitted exactly one `PROLOGUE_CAPTURE_TEARDOWN_OK
capture=refcounted-stress`; its retained fixed-FPS movie/log/console are
`refcounted-stress-drain-green-20260824170559.*` under the same evidence root.
This fixture is test/capture-only (`tests/RunTask4CaptureTeardownTests.cs`) and
does not enter game runtime.

### Behavioral finalizer-marker regression

`TestSceneTeardown.AssertManagedWrapperDrainMarkerOrdering` creates a
test-only finalizable sentinel, requires its finalizer to be observed after the
real waits, and only then permits the teardown marker.  The normal runtime
output is ordered exactly as follows:

```text
TASK4_TEARDOWN_FINALIZER_OBSERVED count=1
PROLOGUE_CAPTURE_TEARDOWN_OK capture=finalizer-order
TASK4_TEARDOWN_ORDER_OK sequence=finalizer,marker finalizers=1
TASK4_TEARDOWN_ORDER_TESTS_OK
```

Retained GREEN log/console:
`screenshots/task4-capture-exit/marker-order-green-20260824170621.*`
(exit `0`).

The behavioral RED byte-snapshots `TestSceneTeardown.cs`, moves the actual
marker call before `GC.WaitForPendingFinalizers()`, rebuilds, runs the Godot
fixture, and validates runtime output order rather than a global source
`IndexOf` oracle.  It failed nonzero with the marker at runtime log index `2`,
no finalizer-observation line (`-1`), and no ordering-success marker:

```text
PROLOGUE_CAPTURE_TEARDOWN_OK capture=finalizer-order
ERROR: System.InvalidOperationException: capture success marker was emitted before pending finalizers drained
TASK4_MARKER_ORDER_RED_OBSERVED exit=1 marker_index=2 finalizer_index=-1
TASK4_MARKER_ORDER_SOURCE_RESTORED sha256=AB2ED88B7BE3C3E1E50E694F9345498162046A9E3CB10552B02D8DDEBF3F101A
```

Retained RED log/console:
`screenshots/task4-capture-exit/marker-before-finalizer-20260824170627.*`.
The exact source-restoration hash equals the post-run production source hash.

The independent forced presentation failure is also retained and remains
strict: exit `1`, no teardown-success marker, and no retry/ignore behavior.

```text
TASK4_FAILURE_PATH_RED_OBSERVED exit=1
log: screenshots/task4-capture-exit/forced-presentation-failure-20260824170650.log
console: screenshots/task4-capture-exit/forced-presentation-failure-20260824170650.console.log
PROLOGUE_CAPTURE_TEARDOWN_OK capture=prologue count: 0
```

### Deterministic evidence SHA-256 helper

The local `Get-FileHash` shadow was removed from `tools/capture-prologue.ps1`.
`tools/EvidenceChecks.ps1` now supplies `Get-EvidenceSha256 -LiteralPath`, a
.NET `SHA256` stream helper.  `Assert-PngCaptureContract` uses that helper;
all existing filename sorting and comparison-normalization behavior is
unchanged.

Focused TDD evidence:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/powershell/EvidenceChecksHash.Tests.ps1
```

Before implementation it failed with `Assertion failed: EvidenceChecks exposes
Get-EvidenceSha256`.  GREEN prints `EVIDENCE_CHECKS_HASH_OK` and proves both
literal path modes using hand-derived byte hashes (`abc` =
`ba7816bf...0015ad`; empty = `e3b0c442...b855`).  It also constructs two real
4x3 PNGs and proves the helper's raw and comparison manifest SHA-256 values
equal the pre-change, module-qualified native `Get-FileHash` manifest result.
The existing focused suite also remains green:

```text
PROLOGUE_EVIDENCE_CHECKS_OK
```

### Restored official GREEN

The retained fresh official AVI run is
`screenshots/task4-capture-exit/official-avi-20260824171036.outer.log`
(stderr is an empty, retained companion file).  It exits `0`, ends in
`PROLOGUE_CAPTURE_OK`, and its clean engine/console contracts are checked by
the capture tool.  The fresh AVI artifacts are:

| Capture | AVI | Contract | Log markers |
| --- | --- | --- | --- |
| prologue | `screenshots/prologue/prologue.avi` | 2,989 frames, 1280x720, 60 FPS | completion `1`, post-drain teardown `1` |
| boss-retry | `screenshots/prologue/boss-retry.avi` | 600 frames, 1280x720, 60 FPS | completion `1`, post-drain teardown `1` |

The corresponding retained clean Godot/merged console logs are
`capture-prologue.log`, `capture-prologue.console.log`, `capture-boss-retry.log`,
and `capture-boss-retry.console.log`.  AVI SHA-256 values are respectively
`85702d9efb15d4c50affb0986a67561f617629ec99cd0bf3c0898a1bb9bfc1cd` and
`c7d2f49d0c66eb792fecfa54e6054ab0fe30cc0b27b684ea55a606b57e0701f5`.

The fresh frame-contract/manifest pass retained
`screenshots/task4-capture-exit/official-first.manifest.txt` (seven entries,
SHA-256 `ca032719ac212d4d5522b6a40847b0ba31f700eb416bbd20ffeac8e185ed081f`).

Final build:

```text
G:/godot/dotnet-sdk/dotnet.exe build Knight.csproj --no-restore
0 warnings
0 errors
```

Final full verifier output is retained at
`screenshots/task4-capture-exit/verify-prologue-20260824171307.outer.log` and
ends in `PROLOGUE_VERIFY_OK`.

No human play was launched.

### Remaining concern

The required real no-drain native nonzero RED cannot be truthfully closed in
this environment: the exact production-source mutation and the single
controller-approved 16,384-`RefCounted` fixture both exit zero.  The harness
therefore remains deliberately fail-closed and will reject this condition; it
does not turn a missing marker into a passing substitute for the native crash.

## Fix round 2 / Task 4B correction and evidence (2026-08-25)

### Controller acceptance change

The controller changed Task 4 acceptance from a claimed reproduction/fix of
the original native crash to defensive mitigation.  The no-drain diagnostic
therefore records either outcome without describing an exit-zero result as a
root-cause GREEN.  The prior statement that the no-drain harness was
fail-closed on an exit-zero child is superseded.

`Task4CaptureExit.ps1` now creates a random, verified system-Temp project
copy for every mutation.  Its path must be absolute, beneath the system Temp
root, and start with `knight-task4-capture-mutation-`; cleanup repeats all
three checks before `Remove-Item`.  The native `Copy-Item` walk excludes only
`.git`, `.worktrees`, `screenshots`, `bin`, `obj`, and large local evidence
formats.  It deliberately includes `.godot` and byte-compares a known
`preview.wav-*.sample` imported audio artifact before using `--path` against
the copy.  The temporary first-build allows local restore; subsequent builds
use `--no-restore`.  No shared source is written by mutation code.

The final isolation contract was RED before its evidence-label correction:

```text
Assertion failed: Task4 mutation evidence accurately records that the required .godot cache is copied
TASK4_ISOLATION_REPORTING_RED_EXIT=1
```

Its retained GREEN is
`screenshots/task4-capture-exit/isolation-contract-green-20260824180051.outer.log`:

```text
TASK4_MUTATION_TEMP_COPY_CREATED ... includes=.godot excludes=.git,.worktrees,screenshots,bin,obj,large-local-logs
TASK4_NO_DRAIN_NATIVE_CRASH_UNREPRODUCED exit=0 completion=1 teardown=0 native_process=clean
TASK4_MARKER_ORDER_RED_OBSERVED exit=1 marker_index=2 finalizer_index=-1
TASK4_MARKER_ORDER_GREEN exit=0 indices=2,3,4,5
TASK4_MUTATION_TEMP_COPY_REMOVED ...
TASK4_MUTATION_ISOLATION_OK
TASK4_CAPTURE_ISOLATION_OK
```

The exact byte restoration SHA-256 values are
`2FAD63A05067B8416A2473D9729B10342C92CC92BC9ECE02645046C0C33F7A85`
for `ProloguePresentation.cs` and
`AB2ED88B7BE3C3E1E50E694F9345498162046A9E3CB10552B02D8DDEBF3F101A`
for `TestSceneTeardown.cs`; the shared pre/post hashes match and no Temp
prefix directory remained.  The no-drain line is a diagnostic limitation,
not a native-crash reproduction or a root-cause pass.

The forced visibility-bounds contract was rerun after the isolation work:

```text
TASK4_FAILURE_PATH_RED_OBSERVED exit=1 failure_frame=300
TASK4_FORCED_FAILURE_CONTRACT_OK
```

Retained child evidence is
`forced-presentation-failure-20260824180542.{log,console.log}`.  It contains
exactly one `ERROR: PROLOGUE_PRESENTATION_FAIL frame=300:`, contains `motion
visibility transition=`, `frame=300`, and `path=player/feet-heading-weapon`,
and contains neither completion nor teardown-success marker.

The deterministic SHA helper and existing evidence contracts pass:

```text
EVIDENCE_CHECKS_HASH_OK
PROLOGUE_EVIDENCE_CHECKS_OK
```

Two fresh official `tools/capture-prologue.ps1` runs remain retained at
`official-round2-first-20260824175153.outer.log` and
`official-round2-second-20260824175502.outer.log`.  Each has exactly one of
each of the following lines and ends in `PROLOGUE_CAPTURE_OK`:

```text
PROLOGUE_PRESENTATION_COMPLETE frame=2989
PROLOGUE_CAPTURE_TEARDOWN_OK capture=prologue
BOSS_RETRY_PRESENTATION_COMPLETE frame=600
PROLOGUE_CAPTURE_TEARDOWN_OK capture=boss-retry
```

They validate the fresh `1280x720@60` 2,989-frame `prologue.avi` and
600-frame `boss-retry.avi` through the capture tool; no human play was
launched.

### Full-verifier native-failure investigation and bounded boundary drain

The required Round 2 verifier was executed once before the boundary change:
`verify-round2-20260824175805.outer.log`.  It failed in the prologue test
process after `PROLOGUE_AUDIO_DRAIN_OK frames=5 elapsed_ms=46`, with the
actual native failure:

```text
ERROR: FATAL: Condition "gchandle.is_released()" is true.
Fatal error. 0xC000001D
```

It did not emit `PROLOGUE_VERIFY_OK`.  The stack was after traversal output,
at the RoomContract warning, and before `PROLOGUE_TESTS_OK`; this is retained
as the real RED, not ignored or retried as a pass.

Three one-shot test-only focused probes were then run without changing game
runtime or gameplay.  The RoomPresentation-only, traversal-only, and exact
traversal-to-RoomPresentation probes each reached a double-GC/finalizer
boundary and exited zero:

```text
TASK4_VERIFY_FINALIZER_BOUNDARY_OK phase=room-presentation
TASK4_VERIFY_FINALIZER_BOUNDARY_OK phase=traversal
TASK4_VERIFY_FINALIZER_BOUNDARY_OK phase=traversal-room-presentation
```

Their retained logs are respectively
`full-verify-probe-room-20260824180341.log`,
`full-verify-probe-traversal-20260824180352.log`, and
`full-verify-probe-traversal-room-20260824180452.log`.  Therefore neither
selected component reproduces the native failure in isolation.

With controller authorization, Task 4B added only test infrastructure:
`TestSceneTeardown.DrainManagedWrappersAtTestBoundary` reuses the existing
two `GC.Collect` / `GC.WaitForPendingFinalizers` core, owns a test-only
finalizable sentinel, then emits its diagnostic and exact success marker.
`RunPrologueTests` calls it immediately after awaited
`PrologueTraversalRouteTests.Run(this)` and before RoomContract and later
synchronous test phases:

```text
TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED boundary=post-traversal count=1
PROLOGUE_MANAGED_WRAPPERS_DRAIN_OK boundary=post-traversal
```

The full-verifier native crash is the implementation RED.  The Temp-only
mutation contract then proved both gate regressions behaviorally:

```text
TASK4_POST_TRAVERSAL_DELETE_RED_OBSERVED child_exit=0 marker=0 tests_ok=1 native_fatal=False
TASK4_POST_TRAVERSAL_EARLY_MARKER_RED_OBSERVED child_exit=0 marker_index=70 finalizer_index=71
TASK4_POST_TRAVERSAL_MUTATION_ISOLATION_OK
TASK4_POST_TRAVERSAL_DRAIN_CONTRACT_OK
```

This retained run is
`post-traversal-mutation-contract-rerun-20260824181454.outer.log`; its Temp
directory was removed.  Deleting the drain cannot pass the marker gate even
when its child happens to exit zero; moving the marker produces the observed
runtime marker-before-finalizer order.

Two consecutive standalone full `RunPrologueTests` GREEN runs are retained at
`post-traversal-contract-first-20260824181056.outer.log` and
`post-traversal-contract-second-20260824181925.outer.log`.  Each proves all
of the following exact count-one lines and has no `0xC000001D`, `ERROR:
FATAL`, or `gchandle.is_released()` signature:

```text
POST_TRAVERSAL_NATIVE_EXIT=0
TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED boundary=post-traversal count=1
PROLOGUE_MANAGED_WRAPPERS_DRAIN_OK boundary=post-traversal
PROLOGUE_TESTS_OK
```

The fresh build after this test-only change reported `0 warnings` and
`0 errors`.

### Remaining blocker

The first required full verifier after the new boundary was run exactly once
at `verify-post-traversal-first-20260824182206.outer.log`.  It reached the
new post-traversal markers and did not have the prior native-fatal signature,
but the prologue test process exited `1`, so no second full verifier was
started.  The first error is a `ResourceLoader.Load` bridge failure:

```text
ERROR: System.InvalidOperationException: Handle is not initialized.
... ScriptManagerBridge.SwapGCHandleForType ...
... FallenBellkeeperRuntimeTests.LoadBossLevel ...
```

It subsequently fails `PrologueCompletionTests` with
`real corridor checkpoint advances objective; expected=LearnThread,
actual=ReachCorridorLamp`.  Its stderr reports `Prologue tests failed with
exit code 1`; it does not emit `PROLOGUE_VERIFY_OK`.  Per the controller
gate, no additional GC point, second verifier, retry, or unrelated gameplay
change was attempted.  This is a blocking full-regression failure that must
be diagnosed separately from the verified post-traversal boundary behavior.

### Controller-corrected process-isolation approach

The controller rejected the in-process post-traversal GC boundary.  Although
it prevented the earlier `gchandle.is_released()` signature, the retained
`verify-post-traversal-first-20260824182206.outer.log` showed that a
mid-process collection invalidated later ResourceLoader script handles.  The
post-traversal code and its contract were removed; this is a rejected
approach, not a claimed fix.

`RunPrologueTests` now partitions the original ordered calls into process
phases.  `--phase=pre` owns every original call through the awaited traversal
route test, then performs the managed-wrapper drain only at its process exit.
`--phase=post` starts fresh, re-establishes diagnostic production mode, owns
every remaining original call through accessibility, and drains only at its
process exit before printing the sole aggregate `PROLOGUE_TESTS_OK`.  The
coverage test has a literal ordered ownership list and its Temp mutation that
removes `PrologueRouteGraphTests.Run` fails with count zero.  Its retained
GREEN output is `TASK4_PROLOGUE_PHASE_COVERAGE_OK`.

The phase-exit behavior suite completed with no shared-source mutation and
no Temp-copy residue.  Its retained log is
`phase-exit-contract-rerun-20260824183727.outer.log`:

```text
TASK4_PHASE_EXIT_DELETE_RED_OBSERVED child_exit=0 marker=0 phase_ok=1 native_fatal=False
TASK4_PHASE_EXIT_EARLY_MARKER_RED_OBSERVED child_exit=0 marker_index=76 finalizer_index=77
TASK4_PHASE_EXIT_MUTATION_ISOLATION_OK
TASK4_PHASE_EXIT_GREEN phase=pre pair=one exit=0
TASK4_PHASE_EXIT_GREEN phase=post pair=one exit=0
TASK4_PHASE_EXIT_GREEN phase=pre pair=two exit=0
TASK4_PHASE_EXIT_GREEN phase=post pair=two exit=0
TASK4_PHASE_EXIT_DRAIN_CONTRACT_OK
```

`tools/verify-prologue.ps1` was then run once with the phase-pair gate:
`verify-phase-split-first-20260824184324.outer.log`.  Its pre process exited
cleanly and emitted exactly one
`PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=pre` and
`PROLOGUE_TEST_PHASE_OK phase=pre`.  The fresh post process failed before its
teardown boundary in `PrologueGuidanceRuntimeTests`:

```text
Assertion failed: real checkpoint/tutorial/exit route reaches Upper during OpenShortcut;
expected=OpenShortcut, actual=ChooseRoute
```

The failure is at `RunPrologueTests.cs:117`, nested from
`PrologueGuidanceRuntimeTests.UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff`
line 124.  This run has no `0xC000001D`, `ERROR: FATAL`,
`gchandle.is_released()`, or `Handle is not initialized` signature, but it
does not emit the post phase marker, `PROLOGUE_TESTS_OK`, or
`PROLOGUE_VERIFY_OK`.  Per the phase-pair gate, no second verifier or
`verify-p0` run was started.  This is the current blocker; it has not been
recast as a teardown GREEN.

### Bounded post-only timing diagnostic

With controller authorization, one post-only diagnostic used the identical
headless `RunPrologueTests` invocation and native merged-command wrapper,
without a build or full verifier.  Its retained engine and console logs are
`phase-post-diagnostic-20260825024825.log` and
`phase-post-diagnostic-20260825024825.console.log`.  It reaches the
`Quit(0)` path and has exactly one of each post marker:

```text
TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED phase=post count=1
PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=post
PROLOGUE_TEST_PHASE_OK phase=post
PROLOGUE_TESTS_OK
```

Neither log contains the failed OpenShortcut assertion, `0xC000001D`,
`ERROR: FATAL`, `gchandle.is_released()`, or `Handle is not initialized`.
The desktop runner detached its output pipe while the long native child was
still live, so its numeric wrapper exit record cannot be recovered after the
fact; no numeric exit is claimed here.  The engine output confirms the
`Quit(0)` path only.  This makes the first phase-pair assertion
timing-sensitive rather than a reproduced teardown fault.

Inspection of the failing ordering found that `TouchExit` waits only physics
frames, while `PrologueCompletionController` advances `ChooseRoute` to
`OpenShortcut` from its `_Process` route observation.  The test asserts the
objective immediately after its final physics wait, so it can read
`ChooseRoute` before the following process-frame observation.  No ordering
change or additional verifier was made pending a focused RED/GREEN request.

### Guidance-order correction and subsequent post-suite blocker

The guidance timing RED remains the failed phase-pair verifier’s
`expected=OpenShortcut, actual=ChooseRoute` assertion.  A test-only targeted
helper now waits at most five `ProcessFrame` ticks only after the real Upper
exit, until the existing completion controller observes `OpenShortcut`; its
assertion reports `process_frames=<actual>/5; current=<objective>`.  The
focused PowerShell contract proves removal of that wait fails from a Temp
copy:

```text
TASK4_GUIDANCE_OBJECTIVE_WAIT_DELETE_RED_OBSERVED exit=1
TASK4_GUIDANCE_OBJECTIVE_ORDERING_OK
```

The fresh C# build after that test-only change is `0 warnings` and `0
errors`.  A direct post-only run passed its engine-level markers and entered
the `Quit(0)` path, but was deliberately not accepted because the desktop
runner detached before persisting its numeric native exit.

The retained-exit post runner then ran corrected post 1/2.  Its authoritative
outer record is `post-timing-first-20260825025709.outer.log`:

```text
POST_TIMING_NATIVE_EXIT=1
POST_TIMING_MARKERS drain=0 phase=0 tests_ok=0
POST_TIMING_FATAL=False
POST_TIMING_OBJECTIVE_ASSERTION=False
```

The first error is independent of the guidance ordering correction:
`ProloguePlaytestRecorderTests.ExplicitHeadlessFixtureWritesInspectableEvidence`
line 108 tries to get `BossArenaController` while the current room is still
`AwakeningCourtyard`, then throws `NullReferenceException`.  That test calls
`PrologueLevelController.TransitionTo` at lines 93 and 105 but waits only
three `ProcessFrame` ticks before treating the target room as mounted.
`TransitionTo` first waits a `PhysicsFrame` before mounting.  Consequently
the test has a scheduling race: process ticks may run before the awaited
physics transition.  The prior clean direct post-only diagnostic demonstrates
that the schedule can flip.  No evidence implicates a preceding test; the
failure is fully explained by this immediate transition/current-room
assumption.  Per the gate, post 2/2, both full verifiers, and `verify-p0`
were not started.  No fix was made pending controller authorization.

### P0 smoke stale-coordinate closure and defensive terminal cleanup

The P0 core smoke contained the same stale authored-coordinate family as the
checkpoint failure.  Its Thread route started at `(210,68)`, pressed fixed
right/up input, and released Thread after two frames.  The current authored
`TraversalContracts/entry_to_anchor_landing` instead supplies the launch and
landing zones plus `AssistTargetPath`; the target is outside Tap range but
inside Hold range.  The smoke now resolves that contract and target at
runtime, converts the room-local launch centre to global coordinates, holds
until actual `Preview`, then observes `Apply`, the exact target id, the pull
velocity, and a stable authored landing.  Its P0 exit and respawn assertion
likewise resolve `Exits/P0Complete` and `Entrances/checkpoint` instead of
hard-coded positions.  This is test-only; no scene, builder, or runtime
logic changed.

The retained isolated contract is
`screenshots/task4-capture-exit/p0-teardown-verifier-contract-green-20260825.log`.
It exercises real Temp projects and reports nonzero for the authored
checkpoint/thread mutations, including `THREAD_EARLY_RELEASE` and
`THREAD_TARGET_ID_TAMPER`; static Temp mutations deleting the completion exit
or destination entrance lookup also fail.  All Temp project copies are under
the task prefix and `TEMP_PREFIX_REMAINS=0` after the suite.

One earlier shared verbose P0 smoke, retained at
`p0-smoke-verbose-20260825035048.log.outer.stdout`, reported four terminal
ObjectDB instances (two `AudioStreamWAV` and two `AudioStreamPlaybackWAV`) and
two resources still in use (`rust-recovery.wav` and `checkpoint.wav`).  The
cause in the test fixture was direct `Quit(0)` after `Root.AddChild(_level)`:
the mounted level and its Audio owner were never test-torn-down.  The smoke
now calls `TestSceneTeardown.FreeSceneAfterAudioDrain` on both terminal
success and failure paths before clearing its level/player references and
before the success marker.  This uses the existing test-only process-exit
boundary; it does not introduce an in-process managed-wrapper GC point.

The controller changed acceptance to a defensive cleanup claim.  In the
required Temp deletion mutation, removing the success-boundary teardown still
exits zero and emits `P0_CORE_SLICE_OK` once in each stream, but emits no
audio-drain marker in either stream:

```text
TASK4_P0_SMOKE_TEARDOWN_DELETE_BEHAVIORAL_RED_OBSERVED exit=0 engine_drain=0 stdout_drain=0
TASK4_P0_SMOKE_TEARDOWN_DELETE_NATIVE_LEAK_UNREPRODUCED exit=0
```

The exact named native leak is therefore **not claimed reproducible or
fixed**.  The retained prior shared verbose evidence is diagnostic only.  The
normal Temp verbose GREEN did prove exit zero, one drain marker before one
success marker in each stream, and no ObjectDB/resources/fatal/Handle
signature.  The matching shared proof is
`p0-smoke-teardown-validated-20260825040218.log.outer`:

```text
P0_SMOKE_NATIVE_EXIT=0
P0_CORE_SLICE_OK_ENGINE_COUNT=1
P0_CORE_SLICE_OK_STDOUT_COUNT=1
P0_AUDIO_DRAIN_ENGINE_COUNT=1
P0_AUDIO_DRAIN_STDOUT_COUNT=1
P0_SMOKE_LEAK_FATAL_HANDLE_COUNT=0
```

### Fail-closed P0 native-console verification

`verify-p0.ps1` now runs import, every builder, each pre/post phase, and the
P0 smoke through `Invoke-MergedNativeCommand`, retaining and scanning an
engine log plus console log for each native command.  The deterministic
aggregate pre/post engine and console logs each require exactly the same five
intentional negative-test warnings; every other log remains a zero-warning,
zero-error contract.  Both streams also require the phase markers and the
single aggregate success marker.  Focused mutations deleting the expected
warning contract or the aggregate test-console scan fail, and a synthetic
console-only `resources still in use` fixture fails the shared engine-log
contract.  No console-only fatal/leak can now pass to `P0_VERIFY_OK`.

Final `verify-p0` evidence is
`screenshots/task4-capture-exit/verify-p0-defensive-teardown-20260825040230.outer.log`.
It completed after a fresh `0 warnings / 0 errors` build and emitted one
`P0_VERIFY_OK`; the retained final scans are:

```text
verify-p0-tests.log warnings=5 pre_drain=1 post_drain=1 tests_ok=1
verify-p0-tests.console.log warnings=5 pre_drain=1 post_drain=1 tests_ok=1
verify-p0-smoke.log core=1 drain=1 bad=0
verify-p0-smoke.console.log core=1 drain=1 bad=0
outer_verify_ok=1
outer_fatal=0
```

### Fix Round 3: fail-closed phase and Temp-copy evidence (incomplete final gate)

The focused contract first retained the missing cleanup and unsafe-mode REDs:
`fix3-p0-copy-cleanup-red-20260825042322.log` and
`fix3-phase-console-mode-red-20260825042459.log`.  The P0 Temp-project
helper now catches a partial-copy failure internally, revalidates the
absolute system-Temp/task-prefix path before guarded deletion, and rethrows
the original failure.  Its injected copy-failure check is deliberately not
inside any caller `finally`; it uses a unique child task prefix and proved
that prefix returns to zero:

```text
TASK4_P0_SMOKE_COPY_FAILURE_INTERNAL_CLEANUP_OK before=0 after=0
```

`RunPrologueTests` now accepts only explicit `--phase=pre` or `--phase=post`.
It has no default or compatibility `all` branch.  Retained native probes for
the missing-phase and `--phase=all` invocations both exit 1, emit the exact
pre/post-only error, and emit no drain, phase-success, or overall-success
marker.  `verify-prologue` now requires phase drain/phase-success markers and
the expected per-phase overall-success count in both engine and Console logs,
then requires the aggregate `PROLOGUE_TESTS_OK` in both streams before its
warning/fatal contract.  The focused deletion mutations for test ownership,
the phase Console drain assertion, and aggregate Console overall-success
assertion all fail closed; the complete retained focused output is
`fix3-phase-contract-full-green-20260825042750.log`.

A fresh build after these changes was 0 warnings / 0 errors.  The one
permitted fresh `verify-prologue` then completed both new phase gates and the
2989-frame presentation capture, but failed in the pre-existing display-backed
visual audit.  Its retained outer output is
`fix3-verify-prologue-20260825042814.outer.log`: the concrete first failure
is `runtime frame spike 50.88ms exceeds 50ms` at
`PrologueVisualAudit.FinishSuccess` frame 720.  Consequently it did not emit
`PROLOGUE_VERIFY_OK`.  This is not claimed as a Task 4 teardown result; per
the stop-on-first-failure rule, `verify-p0` was not rerun and no final-green
claim is made for this round.

### Fix Round 3 controller-run sequential gate

The implementer's 50.88 ms failure message arrived only after the controller
had independently started a fresh current-source `verify-prologue`.  That
already-running verification was allowed to finish; no additional retry was
started after the earlier failure became known.  This distinction is retained
because the display-backed 50 ms boundary is demonstrably close and one pass
does not erase the 50.88 ms failure.

The controller run is retained at
`screenshots/task4-capture-exit/fix3-verify-prologue-20260825043237.outer.log`.
It exited zero with exactly one `PROLOGUE_VERIFY_OK`, no native/finalizer/
Handle/leak/fatal signature, and a display-backed maximum of 49.95 ms.  Both
current aggregate streams have the exact phase and warning contract:

```text
verify-prologue-tests.log warnings=5 pre_drain=1 post_drain=1 pre_ok=1 post_ok=1 tests_ok=1 bad=0
verify-prologue-tests.console.log warnings=5 pre_drain=1 post_drain=1 pre_ok=1 post_ok=1 tests_ok=1 bad=0
```

Only after that full verifier succeeded, the controller ran the single P0
gate retained at
`screenshots/task4-capture-exit/fix3-verify-p0-20260825043704.outer.log`.
It exited zero with exactly one `P0_VERIFY_OK` and no bad signature.  Its test
aggregates repeat the exact five-warning/pre/post/overall contract in both
streams, and its smoke engine/Console logs each contain one audio-drain marker
before one `P0_CORE_SLICE_OK`, with no warning or leak/fatal/Handle signature.

This supplies the reviewer's requested current-source
`verify-prologue -> verify-p0` success chain while retaining, rather than
hiding, the immediately preceding 50.88 ms display-audit failure.  Performance
stability remains a final-certification risk and must be exercised again by
the later final repeated gate; it is not relabelled as a Task 4 teardown fix.
