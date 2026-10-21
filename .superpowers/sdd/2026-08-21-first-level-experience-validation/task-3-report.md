# Task 3 — Phase A Boss guidance correction

## Scope and human-evidence boundary

This is a bounded production-presentation correction before the user-owned
hands-on run. No `tools/play-prologue.ps1` launch, live input injection, or
`HandsOnLauncher` report was created or changed by this work. Automated,
headless, and capture fixtures remain non-human evidence.

## Debugging phases 1–3 (before production code)

### Reproduction

The existing deterministic real route is the reproduction: Task 2's retained
`1280x720` consecutive frames show the world-target guidance card overlapping
the Bellkeeper body and primary danger read in Sweep `2100-2165`, Leap Slam
`2205-2276`, Ground Wave `2319-2399`, and Hammer `2509-2610`. The Task 2 audit
records each of these as `Fail — prompt occlusion` rather than a human
interpretation claim.

### Data flow

`PrologueGuidanceModel.BossInstruction` emits the first-level Boss objective
and `BossBody` target. `PrologueGuidanceController.RefreshNow` resolves that
target to a viewport position and calls
`WorldTargetMarkerVisual.Present`. `WorldTargetMarkerVisual.ApplyPromptLayout`
asks `RoomProtectionRegionAdapter.Collect` for real visible player, Boss,
attack, target, and telegraph rectangles, then calls
`ProtectedPromptPlacer.Place`. The latter tries a fixed local ring around the
world target; if every candidate intersects a protected rectangle, it returns
the closest intersecting candidate (`nearest`). Thus the last fallback breaks
the protection contract precisely in the dense Boss combat state.

### Working comparison

Ordinary world prompts use the same centralized visual and safely accept the
first non-intersecting local candidate. The pre-existing unit fixture proves
that path when at least one candidate is clear. The Boss differs because the
body plus live telegraph/danger geometry blocks the entire local candidate
ring, so the fallback path is used. The bottom Boss HUD is separately laid out
and is not the failed owner.

### Single hypothesis

The occlusion is caused by `ProtectedPromptPlacer.Place` returning an unsafe
nearest local candidate when the whole target-adjacent ring is protected. A
safe-area fallback candidate selected outside that ring, while preserving the
existing local placement whenever it is clear, will keep the Boss body, feet,
danger geometry, and bottom Boss HUD clear without relying on frame, capture,
camera, or source labels.

## RED / GREEN / mutation / capture / gate

### Focused RED

Before the production change, the following focused suite failed exactly as
expected (the build itself succeeded with 0 warnings / 0 errors):

```powershell
dotnet build Knight.csproj --no-restore
G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --script res://tests/RunPrologueTests.cs --log-file task3-phase-a-red.log
```

Exit code: `1`. Named RED assertion: `Boss combat guidance fallback never
covers the Boss, feet, or four attack reads`.

### Minimal correction and GREEN

`ProtectedPromptPlacer.Place` retains the pre-existing target-adjacent ring.
Only after that ring has no clear candidate does it try the two safe-area top
edges, returning one only when it is clear of every protected rectangle. The
existing fallback remains as the theoretical last resort when the whole safe
area is protected. `WorldTargetMarkerVisual` adds the *visible* formal Boss
HUD rectangle to the same centralized protection list, which addresses the
real HammerThrow HUD overlap found while auditing the route; it does not alter
combat, tuning, Boss behavior, or mechanics.

GREEN commands and results:

```powershell
dotnet build Knight.csproj --no-restore
G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --fixed-fps 60 --script res://tests/presentation/ProloguePresentation.cs --log-file task3-phase-a-presentation-green.log
```

Both completed successfully. The real presentation route emitted
`PROLOGUE_PRESENTATION_COMPLETE frame=2989` and the added all-four-attack
clearance checks: Sweep `63`, Leap Slam `69`, Ground Wave `78`, and Hammer
Throw `99` consecutive protected-clear guidance frames.

### Mutation proof

The safe-edge loop was removed temporarily with `apply_patch`, restoring the
previous centered/nearest behavior. Re-running `RunPrologueTests.cs` exited
`1` on the same named Boss overlap assertion. The safe-edge loop was then
restored with `apply_patch`; the focused suite returned `PROLOGUE_TESTS_OK`.

### Fresh real-frame review

Fresh display-backed 1280x720 / 60 FPS output is retained at
`screenshots/task3-phase-a-boss-frames` (`frame00000000.png` through
`frame00002988.png`, 2,989 consecutive frames). Manual inspection opened the
following real route triples in sequence:

| Attack | Telegraph frames | Active frames | Observation |
| --- | --- | --- | --- |
| Sweep | `2101-2103` | `2125-2127` | Prompt is top-right; Boss body, feet and sweep read remain unobscured. |
| Leap Slam | `2206-2208` | `2233-2235` | Prompt remains clear of ascent/landing and danger geometry. |
| Ground Wave | `2320-2322` | `2350-2352` | Prompt is clear of Boss body and wave read. |
| Hammer Throw | `2510-2512` | `2536-2538` | Prompt is clear of the attack line and bottom Boss HUD. |

These are manual visual observations of deterministic captures, not a human
playtest result.

### Five-resolution proof

Fresh UI capture output is retained under `screenshots/task3-phase-a-ui`.
`PrologueUiResolutionCapture.cs` completed with `PROLOGUE_UI_RESOLUTION_OK` at
1280x720 (24 state frames), 1920x1080 (12), 2560x1440 (12), 3840x2160 (12),
and 2560x1080 (12). The `boss-bottom-safe-frame00.png` result was opened at
each resolution: prompt is top-right, the real Boss arena remains readable,
and neither the Boss body/floor nor the bottom HUD is covered. The capture
fixture also asserts real Boss prompt bounds against every protected region
and the formal Boss HUD.

### Human-boundary fixtures

`tests/powershell/PlayPrologue.Tests.ps1` returned `PLAY_PROLOGUE_TESTS_OK`.
It uses a fake command only, confirms a launcher child receives
`HandsOnLauncher`, and confirms caller environment restoration. The current
`RunPrologueTests.cs` also passed the recorder fixtures: default headless runs
write no report; the explicit automated fixture writes only an
`AutomatedFixture`-labelled report. This work did not launch
`tools/play-prologue.ps1`, inject input, or touch/create a HandsOnLauncher
report.

### Complete verifier and self-review

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

The retained `task3-phase-a-full-gate.log` exited `0` and contains
`PROLOGUE_TESTS_OK`, the four `PROLOGUE_BOSS_GUIDANCE_CLEAR_OK` markers,
`PROLOGUE_UI_RESOLUTION_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and
`PROLOGUE_VERIFY_OK`. A first attempt with an environment-overridden isolated
UI capture root produced all captures but a display-backed process exit `1` in
the production-audit harness; clearing that override and using the verifier's
native capture path produced the clean gate above. No production assertion was
involved.

Self-review checked the scoped diff and `git diff --check`: the placement
change is centralized, preserves normal local placement, only adds a visible
HUD protection rectangle, includes a regression/mutation test and real-route
audit, and leaves gameplay/tuning/Boss/mechanical-thread code untouched.

## Files and remaining concern

Production: `scripts/ui/ProtectedPromptPlacer.cs`,
`scripts/ui/WorldTargetMarkerVisual.cs`.
Tests/audit: `tests/unit/ProtectedPromptPlacerTests.cs`,
`tests/presentation/ProloguePresentation.cs`, and
`tests/presentation/PrologueUiResolutionCapture.cs`.

Commit: `fix: keep boss guidance clear of attacks` (final commit identifier is
reported with the Phase A handoff).

The user-owned hands-on run remains required. Automated clearance and capture
evidence does not establish first-time-player comprehension, timing, or feel.

## Fix Round 1 — review reproduction and data flow (before code change)

Independent review found two ways the prior evidence could overstate safety.
First, `ProtectedPromptPlacer.Place` correctly rejected local and top-edge
candidates but then returned an already-rejected `nearest` rectangle when all
of them were blocked. `WorldTargetMarkerVisual.ApplyPromptLayout` treated that
rectangle as a real card, so the fallback was not safe by construction.
Second, the UI capture cleared `Guidance/VisualRoot`, advanced only physics
frames, and then inspected `PromptRect`. That allowed the capture to accept a
default hidden rectangle while a stale formal `[K] 牵动机关` prompt remained.

The production data flow remains `BossInstruction/BossBody` →
`PrologueGuidanceController.RefreshNow` → `WorldTargetMarkerVisual.Present` →
`ProtectedPromptPlacer.Place`, with real actor/telegraph bounds supplied by
`RoomProtectionRegionAdapter`. The round-one hypothesis is therefore: an
explicit no-placement result, consumed by clearing/hiding card controls, and
a process-frame wait that proves the live Boss instruction/target before UI
capture will make the presentation and its evidence contract safe. The RED
tests below are added before the corresponding production/capture changes.

### Fix Round 1 RED / GREEN / mutation evidence

#### Explicit no-placement contract

`ProtectedPromptPlacer.Place` now returns the default, zero-area `Rect2` when
all candidate positions intersect the protected geometry. This is an explicit
no-placement result, not a rejected nearest candidate. `WorldTargetMarkerVisual`
interprets that result by clearing its text and hiding the card chrome/content;
the target diamonds remain available. Thus an all-blocked layout cannot reuse a
stale card or cover a Boss attack, danger marker, or Boss HUD.

The focused all-blocked RED was executed before that production change:

```powershell
dotnet build Knight.csproj --no-restore
& $godot --headless --path . --script res://tests/RunPrologueTests.cs --log-file task3-round1-unit-red.log
```

The build completed with `0 Warning(s), 0 Error(s)`. The test process exited
`1` with the expected named failure:

```text
Assertion failed: all-blocked protected prompt placement returns explicit no-placement instead of an unsafe card
```

The corresponding GREEN run was:

```powershell
dotnet build Knight.csproj --no-restore
& $godot --headless --path . --script res://tests/RunPrologueTests.cs --log-file task3-round1-unit-green.log
```

It completed successfully with `PROLOGUE_TESTS_OK`. The focused tests now use
the production `8px` clearance (`region.Grow(8f)`) and include the all-blocked
regression.

#### Real arena route RED and byte-restored mutation

Literal geometry is not the only RED evidence. The retained real arena route
activates `BossSweep`, `BossLeapSlam`, `BossGroundWave`, and `BossHammerThrow`,
collects the actual `RoomProtectionRegionAdapter` marker bounds, and asserts
the named `REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL` only after all four attacks
have been evaluated. This means the route reports all reads rather than hiding
later failures behind the first one.

The auditable mutation command was:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceMutation.ps1 -GodotPath $godot -RouteLog task3-round1-real-route-mutation.log *> task3-round1-mutation-harness.log
```

The harness byte-snapshotted `scripts/ui/ProtectedPromptPlacer.cs`, replaced
the named local candidate acceptance with an unconditional acceptance, and
logged the exact source identities:

```text
TASK3_MUTATION_SOURCE original_sha256=84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789 mutated_sha256=CC7582A039183C3C7B78889410ED4406457E6311F3FEECEF6B5B49DBEABD5887 diff_id=FEF8EB8F99BF78EB2CE029E075488BF04F92543E913CB12DF6939CDA91C9BF71
TASK3_REAL_ROUTE_MUTATION_RED_OK exit=1 all_attacks=4 assertion=REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL
TASK3_MUTATION_RESTORED_SHA256=84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789
```

The mutated real route reported adapter/marker violations at frame `2102`
(`BossSweep`), `2207` (`BossLeapSlam`), `2321` (`BossGroundWave`), and `2511`
(`BossHammerThrow`) before failing. The `finally` restoration hash exactly
matches the original byte hash above; no mutation branch remains in production
code.

The restored GREEN route was captured with:

```powershell
dotnet build Knight.csproj --no-restore
& $godot --path . --fixed-fps 60 --write-movie screenshots\task3-round1-boss-frames\frame.png --script res://tests/presentation/ProloguePresentation.cs --log-file task3-round1-presentation-green.log
```

It completed at frame `2989` and logged consecutive verified-clear frames of
`63` (`BossSweep`), `69` (`BossLeapSlam`), `78` (`BossGroundWave`), and `99`
(`BossHammerThrow`). I manually inspected consecutive start triples
`2101–2103`, `2206–2208`, `2320–2322`, and `2510–2512` in
`screenshots/task3-round1-boss-frames`: the Boss body/attack reads and bottom
Boss HUD are clear while the teaching card is at the protected top-right edge.

#### Live Boss guidance at five viewports

The capture flow now clears the old formal prompt, waits on process frames for
the live guidance refresh, and requires a visible nonempty card targeted only
at `BossBody` with both model and rendered instruction text exactly
`按 J 攻击，并躲避出招` before it performs geometry/HUD assertions or writes a
frame. This corrects the stale `[K] 牵动机关` capture condition. The legacy
schedule on this machine already refreshed to live guidance after its physics
wait, so it did not provide a standalone false-green RED; the old retained
stale capture and the new strict contract were the reproduction evidence.

The display-backed GREEN capture was:

```powershell
$captureRoot = 'screenshots\task3-round1-ui-green'
$env:KNIGHT_UI_CAPTURE_ROOT = 'res://screenshots/task3-round1-ui-green'
& $godot --path . --fixed-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/presentation/PrologueUiResolutionCapture.cs --log-file task3-round1-ui-green.log
```

It finished with `PROLOGUE_UI_RESOLUTION_OK`. Manual inspection of
`boss-bottom-safe-frame00.png` in `1280x720`, `1920x1080`, `2560x1440`,
`3840x2160`, and `2560x1080` confirms the live `◆ 按 J 攻击，并躲避出招`
card, clear Boss body, and clear bottom Boss HUD at every viewport.

#### Boundary fixtures, full gate, and self-review

The human-boundary fixture command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\PlayPrologue.Tests.ps1
```

returned `PLAY_PROLOGUE_TESTS_OK`. No `tools/play-prologue.ps1` invocation,
input injection, `HandsOnLauncher` report, or human-run claim was made.

The complete verifier was run after the GREEN changes:

```powershell
cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File tools\verify-prologue.ps1 > task3-round1-full-gate.log 2>&1"
```

It exited `0` and the retained log contains both `PLAY_PROLOGUE_TESTS_OK`, all
four `PROLOGUE_BOSS_GUIDANCE_CLEAR_OK` markers, `PROLOGUE_UI_RESOLUTION_OK`,
`PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`.

Self-review checked that (1) the only fallback is a no-area result, (2) the
consumer clears and hides stale content, (3) all adapter regions and the Boss
HUD retain the same 8px clearance, (4) the mutation restores original bytes in
`finally`, (5) five captures prove live rather than retained guidance, and
(6) scoped commits leave pre-existing unrelated dirty/import/UID/scene/art
artifacts unstaged.

Production code and tests are committed as `fe904e7 fix: make boss guidance
placement fail safe`. This report and the append-only audit/README handoff are
recorded separately so the report can identify the completed production commit.

**Handoff status: NEEDS_CONTEXT.** Automated Phase A evidence is complete,
but the user-owned hands-on launch and its human evidence remain pending.

## Fix Round 2 — suppression oracle and truthful aggregate markers (2026-08-24)

### Investigation, comparison, and hypothesis

The review finding was reproduced on the retained real Boss route, not with a
literal rectangle. `ObserveBossGuidanceClearance` inspected the rendered
`WorldTargetMarkerVisual` during every Boss Telegraph/Active frame. Its prior
single branch treated an invisible/zero-area card as a failure unconditionally.
That collapsed three different states: a hidden guidance model, an unexpected
missing card, and the intentional fail-closed result from
`ProtectedPromptPlacer.Place`.

The working comparison is the ordinary route: its live instruction has one
resolved target, the visual root remains visible, the placer returns a safe
area card, and the card controls render it. The deliberate all-blocked route
instead has a live instruction and target but the same real placer returns its
zero-area no-placement contract. `WorldTargetMarkerVisual` correctly clears
the `Text` label and hides `Backdrop`, `TopRule`, and `Text` in that case.

The traced data flow is `PrologueGuidanceController.CurrentInstruction` and
its resolved target view -> `WorldTargetMarkerVisual.Present` ->
`ProtectedPromptPlacer.Place` -> rendered card controls ->
`ProloguePresentation.ObserveBossGuidanceClearance`. The hypothesis was: only
the combination of a visible one-target live instruction, a recomputed
zero-area placement against the current `RoomProtectionRegionAdapter` regions
plus Boss HUD, and hidden/empty card controls represents intentional
suppression; every other missing card remains a failure. Separately,
`ValidateBossGuidanceClearance` must aggregate first and print an OK marker
only after the aggregate contains no failures.

During the first GREEN attempt, the new classifier exposed one data-flow detail
instead of a new production defect: at frame `2563` the real HammerThrown
state switches from `BossBody` to the valid one-target `BossChain` instruction
while the Hammer attack timeline is still active. The classifier therefore
requires a live, visible, single-target Boss-arena instruction, rather than
hard-coding `BossBody`; it continues to verify its current target view and
recomputed protected placement. This preserves the actual route's guidance
semantics without treating an empty or hidden model as suppression.

### RED: both scoped regressions

Before changing `ProloguePresentation`, the unsafe-placement mutation test was
extended to reject an OK marker in a failed aggregate and run with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceMutation.ps1 -GodotPath $godot -RouteLog task3-round2-unsafe-red-route.log
```

The route itself failed for all four real attacks at `2102`, `2207`, `2321`,
and `2511` as intended, but also emitted all four
`PROLOGUE_BOSS_GUIDANCE_CLEAR_OK` markers before
`REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL`. The focused harness then failed
with the expected message `Mutated real route emitted an OK marker alongside
its aggregate Boss guidance failure.` and returned
`TASK3_ROUND2_UNSAFE_RED_EXIT=1`. Its `finally` restored
`ProtectedPromptPlacer.cs` exactly to SHA-256
`84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789`.

The second RED introduces an auditable byte-restored all-blocked mutation that
replaces both real candidate-acceptance branches with `if (false)`, leaving the
normal fail-closed return in place:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceSuppression.ps1 -GodotPath $godot -RouteLog task3-round2-suppression-red-route.log
```

It logged source SHA-256
`84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789` ->
`160980CC1B022AD6A5BD387C0979F85A50B419C591D2CAE5CADC3311C2E37D0C`
with diff identity
`2153D98A90B625ACAF54F077E8E8879FDE439F9F40DA06302B4D9D47663DE80E`.
Before the audit correction it recorded `missing visible prompt` as a violation
for all four attacks and exited `1` with the expected
`Intentional all-blocked Boss guidance suppression was counted as a route
failure (exit=1).` The source restoration marker again exactly matched
`84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789`.
The intentional mutation's compile warnings (`CS0162` for forced-false test
branches) are expected and confined to the temporary byte mutation; all
unmutated builds are warning-free.

### GREEN, mutation sensitivity, and complete gate

The corrected suppression route was run with the same
`Task3BossGuidanceSuppression.ps1` command and passed:

```text
PROLOGUE_BOSS_GUIDANCE_SUPPRESSED kind=BossSweep frame=2102 zero_area=true card_hidden=true stale_text=false
PROLOGUE_BOSS_GUIDANCE_SUPPRESSED kind=BossLeapSlam frame=2207 zero_area=true card_hidden=true stale_text=false
PROLOGUE_BOSS_GUIDANCE_SUPPRESSED kind=BossGroundWave frame=2321 zero_area=true card_hidden=true stale_text=false
PROLOGUE_BOSS_GUIDANCE_SUPPRESSED kind=BossHammerThrow frame=2511 zero_area=true card_hidden=true stale_text=false
TASK3_REAL_ROUTE_SUPPRESSION_GREEN_OK exit=0 all_attacks=4
TASK3_SUPPRESSION_MUTATION_RESTORED_SHA256=84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789
```

It retained the real route's clearance evidence of `63`, `69`, `78`, and `99`
consecutive frames for sweep, leap slam, ground wave, and hammer throw. The
unsafe-placement mutation was then rerun GREEN as a test of the oracle:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceMutation.ps1 -GodotPath $godot -RouteLog task3-round2-unsafe-green-route.log
```

It exited through `TASK3_REAL_ROUTE_MUTATION_RED_OK exit=1 all_attacks=4
assertion=REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL`, emitted four real geometry
failure markers, emitted no `PROLOGUE_BOSS_GUIDANCE_CLEAR_OK` marker, and
restored the exact original SHA-256 in `finally`.

The unmutated focused route GREEN was:

```powershell
dotnet build Knight.csproj --no-restore
& $godot --headless --path . --fixed-fps 60 --script res://tests/presentation/ProloguePresentation.cs --log-file task3-round2-presentation-green.log
```

It completed successfully at frame `2989` with the unmutated 63/69/78/99
clearance markers and no suppression or violation marker.

The full gate was run after those checks:

```powershell
cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File tools\verify-prologue.ps1 > task3-round2-full-gate.log 2>&1"
```

The retained output contains `PLAY_PROLOGUE_TESTS_OK`, `PROLOGUE_TESTS_OK`,
all four `PROLOGUE_BOSS_GUIDANCE_CLEAR_OK` markers, `PROLOGUE_UI_RESOLUTION_OK`,
`PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`. Its visible
`FORCED_SMOKE_FAILURE` is the verifier's expected negative fixture, followed
by the successful critical-path and overall-verifier markers.

### Scope and handoff

Round 2 changes only the real-route audit and its two PowerShell mutation
regressions. The audit still uses actual adapter geometry and the Boss HUD;
intentional suppression is accepted only after proving the model/target,
zero-area placer result, and hidden/empty card state. Unexpected missing
guidance remains a named violation. No gameplay, Boss, timing, tuning, scene,
asset, human launcher, input, or `HandsOnLauncher` report was touched. The
pre-existing unrelated dirty/import/UID/scene/art artifacts remain unstaged.

**Handoff status: NEEDS_CONTEXT.** The user-owned hands-on run remains the
only pending Task 3 evidence boundary.

## Fix Round 3 — independent suppression oracle and live target contract (2026-08-24)

### Reproduction, data flow, comparison, and hypothesis

This round started from `9791e62`. The retained real four-attack route exposed
two audit-oracle defects rather than a gameplay or Boss-tuning defect.
`ObserveBossGuidanceClearance` previously delegated its intentional-suppression
decision to `IsIntentionalBossGuidanceSuppression`, which called
`ProtectedPromptPlacer.Place` again. Therefore the all-candidates-rejected
mutation changed both the production decision and the audit's acceptance
criterion. The same classifier accepted every visible one-target arena
instruction, including the inactive `BossActivation` target.

The traced path is: `PrologueGuidanceModel` ->
`PrologueGuidanceController.CurrentInstruction` plus resolved target view ->
`WorldTargetMarkerVisual.Present` -> `ProtectedPromptPlacer.Place` -> the
rendered card. The working comparison is the unmutated route: a current
`BossBody` instruction during phase-one attacks (and `BossChain` only while
the Hammer is thrown) receives a real current target view and a visible safe
card. The fail-closed comparison is a genuine all-blocked layout: the same
live instruction and target remain present, but all placement candidates meet
current protected geometry and the visual clears its card controls.

Hypothesis: intentional suppression is valid only if an oracle independent of
the placer enumerates every exact production candidate against authoritative
current protected regions at the production `8px` clearance, verifies all are
blocked, and the live target is `BossBody` or a state-valid `BossChain`.
Anything else, including `BossActivation`, an inactive arena, stale target
view, a clear candidate, or retained card contents is an unexpected missing
prompt and must fail the real route.

### Focused RED and minimal correction

Before correcting the classifier, a real route fixture used
`KNIGHT_PRESENTATION_FORCE_BOSS_PROMPT_CANDIDATES_BLOCKED=1` to create ten
visible `Area2D`/`CollisionShape2D` objects on the actual room `Art` root.
They use `CombatCollisionLayers.EnemyAttack`, so
`RoomProtectionRegionAdapter.Collect` receives genuine runtime collision
bounds rather than literals. The fixture positions each rectangle over one
candidate and refreshes the live guidance controller.

The stale-target RED command was:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceStaleTarget.ps1 -RouteLog task3-round3-stale-target-red.log
```

It byte-mutated the single ASCII target token in
`PrologueGuidanceModel.cs`, with source identities:

```text
TASK3_STALE_TARGET_MUTATION_SOURCE original_sha256=C696E4FEC407F3EA38ED6430B60CC359DE539C09A547B4D1E87AEEF444C4F638 mutated_sha256=4347DAA47DD317B20C9247F5C1FF5E23977B610AC0DBBA2559FE189ADD23B4B5 diff_id=29EF19790E231C6A906FCBABF1798795DA348576B12DC0CC5B24BB355C498E90
```

Before the correction the underlying real route incorrectly exited `0` and
emitted `PROLOGUE_BOSS_GUIDANCE_SUPPRESSED` for Sweep `2102`, Leap Slam
`2207`, Ground Wave `2321`, and Hammer Throw `2511`; the focused harness then
failed as required with `Stale BossActivation guidance unexpectedly passed
intentional-suppression classification.` It restored the exact original model
bytes in `finally`.

The narrow production-audit correction in `ProloguePresentation.cs` is an
independent enumerator of the exact ten placement rectangles: the eight
target-relative candidates (`TargetGap=16`, including the four 64px diagonal
vertical offsets) followed by the two clamped top safe-edge candidates. It
uses the same size clamp and `protectedRegion.Grow(8f)` clearance as the
centralized owner, but does not call `ProtectedPromptPlacer`. The oracle adds
the currently visible Boss HUD to `RoomProtectionRegionAdapter` bounds and
only accepts suppression when all ten intersect a protected region. It also
requires an active Boss arena; `BossBody` or `BossChain` only; `BossChain`
only during `HammerThrown`; a resolver-valid current target; and a current
camera-derived target view. The existing zero-area/hidden/empty-content checks
remain mandatory.

### GREEN and mutation sensitivity

The corrected stale-target harness completed successfully (its named result
means the intentionally bad route was detected):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceStaleTarget.ps1 -RouteLog task3-round3-stale-target-green.log
```

```text
PROLOGUE_BOSS_GUIDANCE_CLEAR_FAIL kind=BossSweep ... key=BossActivation
PROLOGUE_BOSS_GUIDANCE_CLEAR_FAIL kind=BossLeapSlam ... key=BossActivation
PROLOGUE_BOSS_GUIDANCE_CLEAR_FAIL kind=BossGroundWave ... key=BossActivation
PROLOGUE_BOSS_GUIDANCE_CLEAR_FAIL kind=BossHammerThrow ... key=BossActivation
TASK3_STALE_TARGET_RED_OK exit=1 all_attacks=4 assertion=REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL
TASK3_STALE_TARGET_MUTATION_RESTORED_SHA256=C696E4FEC407F3EA38ED6430B60CC359DE539C09A547B4D1E87AEEF444C4F638
```

The false all-blocked mutation now proves that the oracle does not reuse the
placer it judges:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceSuppression.ps1 -RouteLog task3-round3-false-suppression-green.log
```

It changed both candidate acceptance branches of
`ProtectedPromptPlacer.cs` from SHA-256
`84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789` to
`160980CC1B022AD6A5BD387C0979F85A50B419C591D2CAE5CADC3311C2E37D0C`
with diff identity
`2153D98A90B625ACAF54F077E8E8879FDE439F9F40DA06302B4D9D47663DE80E`.
The mutated live route failed all four attacks with independent clear candidate
`index=8 rect=(984, 40), (248, 48)`, emitted no suppression marker, and the
harness completed with:

```text
TASK3_REAL_ROUTE_FALSE_SUPPRESSION_RED_OK exit=1 all_attacks=4 assertion=REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL
TASK3_SUPPRESSION_MUTATION_RESTORED_SHA256=84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789
```

The genuine controlled all-blocked GREEN uses no production-source mutation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceLiveSuppression.ps1 -RouteLog task3-round3-live-suppression-green.log
```

It logged ten authoritative collision blockers and, for all four attacks,
`independent_candidates=10 blocked=10 zero_area=true card_hidden=true
stale_text=false`. Sweep, Leap Slam, Ground Wave, and Hammer Throw were first
suppressed at frames `2102`, `2207`, `2321`, and `2511`, respectively, while
retaining consecutive clearance runs of `63`, `69`, `78`, and `99` frames.
The source hashes before and after matched exactly and the run ended:

```text
TASK3_LIVE_ALL_BLOCKED_GREEN_OK attacks=4 candidates=10 padding=8 card_hidden=true
TASK3_LIVE_SUPPRESSION_SOURCE_UNCHANGED placement_sha256=84270938F70602F3EDCDF5382C7681E2F4E8E54C42A84DC6B5EC4485AF3F1789 presentation_sha256=EF53E4F171A2E40B89DD996D67B15E6BD43B3DE0CC6E64B85F9939A167E9B7DF
```

The retained unsafe-placement mutation was also rerun:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\Task3BossGuidanceMutation.ps1 -RouteLog task3-round3-unsafe-placement-mutation.log
```

It changed the placer to SHA-256
`CC7582A039183C3C7B78889410ED4406457E6311F3FEECEF6B5B49DBEABD5887`
(diff `FEF8EB8F99BF78EB2CE029E075488BF04F92543E913CB12DF6939CDA91C9BF71`),
failed the named real-route assertion for all four actual attacks, and restored
the original placer SHA exactly. Its temporary mutation compiled with zero
errors; all unmutated builds were zero-warning/zero-error.

### Fresh capture, five viewports, boundary, and gate

The normal unmutated route was rebuilt and completed with the four
`PROLOGUE_BOSS_GUIDANCE_CLEAR_OK` markers at 63/69/78/99 consecutive frames:

```powershell
dotnet build Knight.csproj --no-restore
& $godot --headless --path . --fixed-fps 60 --script res://tests/presentation/ProloguePresentation.cs --log-file task3-round3-normal-route.log
```

The display-backed five-resolution capture was then run with a user-data-only
capture root, not a human launcher:

```powershell
$env:KNIGHT_UI_CAPTURE_ROOT = 'user://task3-round3-ui-debug'
& $godot --path . --fixed-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/presentation/PrologueUiResolutionCapture.cs --log-file task3-round3-ui-debug.log
```

It produced three consecutive live `boss-bottom-safe` frames at `1280x720`,
`1920x1080`, `2560x1440`, `3840x2160`, and `2560x1080`, and ended with
`PROLOGUE_UI_RESOLUTION_OK`. Manual inspection of each `frame00.png` confirms
the live `◆ 按 J 攻击，并躲避出招` card at the top-right safe edge, no overlap
with the Boss body/danger read or bottom Boss HUD, and the same clear layout
across all five aspect/resolution contracts.

The human-evidence boundary fixture was run without invoking a launcher:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\powershell\PlayPrologue.Tests.ps1
```

It returned `PLAY_PROLOGUE_TESTS_OK`. No `tools/play-prologue.ps1`, input
injection, `HandsOnLauncher` report, or human evidence was created.

The full gate was run sequentially after the focused checks:

```powershell
cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File tools\verify-prologue.ps1 > task3-round3-full-gate.log 2>&1"
```

It ended with `PROLOGUE_VERIFY_OK`, including `PLAY_PROLOGUE_TESTS_OK`,
`PROLOGUE_TESTS_OK`, all four Boss clearance markers,
`PROLOGUE_UI_RESOLUTION_OK`, `PROLOGUE_PRODUCTION_AUDIT_OK`, and
`PROLOGUE_CRITICAL_PATH_OK`. The `FORCED_SMOKE_FAILURE` line is the
verifier's expected negative fixture; the overall gate passed afterward.

### Self-review and handoff

Self-review confirmed that (1) the intentional-suppression classifier never
calls `ProtectedPromptPlacer`, (2) it duplicates the exact ten-candidate,
8px production contract against current adapter/HUD geometry, (3) a genuine
all-blocked case uses live collision shapes, (4) stale/inactive/
`BossActivation` guidance is rejected, (5) every byte mutation restores in a
`finally` block, (6) `CLEAR_OK` remains after aggregate failure evaluation,
and (7) only scoped tracked files are staged; all pre-existing imported, UID,
scene, art, screenshot, and controller-ledger changes remain unstaged.

Production/audit commit: `a7763ea fix: make boss guidance suppression oracle independent`.

**Handoff status: NEEDS_CONTEXT.** Phase A automated evidence is corrected;
the user-owned hands-on launch remains pending and was not invoked.
