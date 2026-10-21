# Task 5 — Version-3 Fall Context and Local Diagnostics

## Delivered contract

`ProloguePlaytestReport` is now schema version 3. It carries a typed `Falls` list of
`PrologueFallSample` values, with typed point and input-edge samples. A fall records its
reason, sector, exact world position, last grounded platform path, checkpoint identity,
claw state/target/material/failure, and the bounded recent stable input edges. Version 1 and
real version 2 payloads migrate to version 3 with an empty fall list; migration never
invents coordinates or a synthetic fall.

`ProloguePlaytestRecorder` records support only while the player is on a floor and a slide
collision has normal `Y < -0.5f`; the saved support is the collider's scene-relative path.
It samples only the approved Q/E, mouse-button, Space/Shift, and WASD transition names from
`PlayerInputReader.CurrentFrame`. The raw edge queue retains the inclusive 1.5-second window
and never receives mouse coordinates or idle frames.

For `FALL RESET`, the recovery callback snapshots the context before any recovery mutation,
then executes the required sequence:

```
ObserveInputEdges (claim current physics sequence) -> CaptureFallContext -> RecordDeath -> RecordFall -> BeginRecovery -> Persist
```

Other recovery reasons cannot impersonate a fall. The recorder persists immediately after
`BeginRecovery` and rejects an attempt to record a fall once recovery has started.

## Repair — physical-frame de-duplication and Markdown table safety

Code review found that `_Process` can observe the same `PlayerInputReader.CurrentFrame` more
than once after a single `_PhysicsProcess` update. `PlayerInputReader.CurrentFrameSequence`
now increments once per physics input update. `ProloguePlaytestRecorder` remembers the last
claimed sequence and samples its stable edge vocabulary only when that sequence changes; it
does not call `ClearSampledFrame`, so no other reader loses the frame.

The Markdown formatter now routes dynamic cells through `MarkdownCell`: null becomes an empty
cell, backslashes are escaped before pipes, CRLF/CR normalize to LF, and LF becomes `<br>`.
This is applied to dynamic report and fall-table values only; `FormatPoint` remains invariant
culture formatting. Thus dynamic reason, target, platform, and input-action text cannot add a
third Markdown column or a table row.

The repair began with two isolated REDs:

- `task5-repair-markdown-red.log`: the report lacked fall-cell escaping.
- `task5-repair-input-sequence-red-build.log`: the test referenced the absent sequence claim
  mechanism.

The canonical focused suite is green in `task5-repair-focused-final.log` after its build log.
It exercises a real `PlayerInputReader` across two physics updates, calls the claim gate twice
with the same sequence and verifies only one Q/E/LMB_DOWN/SPACE/SHIFT batch, and proves pipe,
CR/LF, backslash, and null fall values remain two-column Markdown cells.

## Repair — recovery-before-process input timing

`PrologueLevelController.RecoveryRequested` is emitted before its recovery path clears
`PlayerInputReader.CurrentFrame`. A recovery can therefore arrive before the recorder's normal
`_Process` observation. The recorder now calls `ObserveInputEdges()` immediately after it accepts
the fall-reset reason and before it captures, records, or resets anything. The existing
`CurrentFrameSequence` claim makes this idempotent if ordinary `_Process` already claimed that
physics frame; after the controller clears the sampled input, re-observing the same sequence
cannot manufacture default-frame releases.

The new real timing fixture loads `PrologueLevel`, routes the actual recovery signal into the
recorder, injects one physical input snapshot (sequence 71), and calls `RequestRespawn` before
normal recorder `_Process` gets a chance to see it. Its persisted fall has exactly
`E`, `LMB_DOWN`, `RMB_DOWN`, `SPACE`, and `A_DOWN`. After reset, two more observations of that
same sequence leave the queue unchanged and cannot add `LMB_UP`, `RMB_UP`, or `A_UP`. The fixture
uses an `AutomatedFixture` output location only, writes no human-note field, and deletes its
temporary output; it neither launches nor creates a `HandsOnLauncher` report.

The original timing RED in `task5-recovery-timing-red.log` reports
`expected=E|LMB_DOWN|RMB_DOWN|SPACE|A_DOWN, actual=`. After the minimal recorder change,
`task5-recovery-timing-green.log` and `task5-recovery-timing-final-focused.log` end with
`TASK5_RECOVERY_TIMING_FOCUSED_OK`. Removing just the recovery-time observation produces the
same expected RED in `task5-recovery-timing-mutation.log`; the canonical blob was restored before
the final full verification.

## Test-first evidence and green verification

The isolated RED build is `artifacts/task11-revision/task5-fall-context-materialized/task5-red-build.log`.
It fails with the expected missing `PrologueInputEdgeSample` and `PrologueFallSample` types
before the schema implementation exists.

The final materialized build is green with zero warnings and zero errors in
`task5-commit-final-build.log`. The focused in-memory fixture in
`task5-focused-after-death-filter.log` proves exact JSON and Markdown for this fall:

| Field | Exact value |
| --- | --- |
| position | `(1304.5, 702.25)` |
| sector | `CanyonDescent` |
| last grounded platform | `Gameplay/Platforms/GripCatchShelf` |
| checkpoint | `NotActivated` |
| claw | `Latched` / `GripStone` / `GripStone` / `None` |
| recent edges | `E (1.00s)`, `LMB_DOWN (0.50s)`, `SPACE (0.25s)` |

The same focused suite loads a literal version-2 fixture and proves its resulting version is
3 with zero falls. It also proves an edge exactly 1.5 seconds before recovery remains while
an older edge and an unsupported `MOUSE_WORLD` sample do not.

Full task-only verification first rebuilt the Task 3 generated canyon in the isolated
materialization, in leaf-to-main order only: `CanyonEntrance`, `CanyonDescent`,
`CanyonBasin`, `CanyonAscent`, `LostTemple`, then `PrologueLevel`.
`task5-task3-materialization.log` records the pack results. The final full passes are:

- `task5-commit-final-pre.log`: `PROLOGUE_TEST_PHASE_OK phase=pre`.
- `task5-commit-final-post.log`: `PROLOGUE_TEST_PHASE_OK phase=post` and `PROLOGUE_TESTS_OK`.

The PowerShell transition contract is a direct-execution repository convention rather than a
`Describe`-block suite. Its canonical direct run and `Invoke-Pester` host both return exit 0
after expected child-process REDs are asserted; logs are `task5-pester-final.log` and
`task5-invoke-pester-final.log`. Pester 3 therefore reports zero framework cases while it
executes the repository's direct contract script. The script deliberately resets the expected
child mutation exit state only after all canonical assertions have passed, so `Invoke-Pester`
does not report the expected negative subprocess as a false parent failure.
The pre-fix host result is retained in `task5-invoke-pester-current.log` (`INVOKE_PESTER_EXIT=1`)
and the final host result is `INVOKE_PESTER_EXIT=0`; the only change is this expected-child
exit-state normalization.

## Negative mutations

Every mutation ran only in the isolated materialization and the canonical blob was restored
afterwards.

| Mutation | Required RED evidence | Log |
| --- | --- | --- |
| reject v2 migration | `Unsupported report version 2` | `task5-mutation-v2-migration.log` |
| shorten input window to 1.49s | exact 1.5-second edge unexpectedly absent | `task5-mutation-input-window.log` |
| break `--capture` evidence boundary | forbidden capture grant is authorized | `task5-mutation-capture-boundary.log` |
| omit `RecordFall` before recovery | ordered snapshot/death/fall/recovery/persist contract fails | `task5-pester-final.log` |
| rename `LMB_UP` to an unsupported edge | left-button-release contract fails | `task5-pester-final.log` |
| accept a duplicate input-frame sequence | second Q/E/LMB_DOWN/SPACE/SHIFT batch appears | `task5-repair-mutation-duplicate.log` |
| stop incrementing the physical input sequence | physics fixture observes `first=0, second=0` | `task5-repair-mutation-physics-sequence.log` |
| stop escaping Markdown pipes | escaped fall-reason contract fails | `task5-repair-mutation-markdown.log` |
| omit recovery-time input observation | recovery-before-process fall has no `E/LMB_DOWN/RMB_DOWN/SPACE/A_DOWN` | `task5-recovery-timing-mutation.log` |

The repair reran the direct and `Invoke-Pester` transition contract successfully in
`task5-repair-pester-final-direct.log` and `task5-repair-pester-final-invoke.log`. It also
rebuilt the Task 3 canyon only in `task5-repair-task3-materialization.log` and then completed
`task5-repair-full-pre.log` (`PROLOGUE_TEST_PHASE_OK phase=pre`) and
`task5-repair-full-post.log` (`PROLOGUE_TEST_PHASE_OK phase=post`, `PROLOGUE_TESTS_OK`).

The recovery-timing repair repeated the same isolated Task 3 leaf-to-main materialization in
`task5-recovery-timing-task3-materialization.log`, then completed
`task5-recovery-timing-pre.log` (`PROLOGUE_TEST_PHASE_OK phase=pre`) and
`task5-recovery-timing-post.log` (`PROLOGUE_TEST_PHASE_OK phase=post`, `PROLOGUE_TESTS_OK`).
Its direct transition contract and Pester host logs are
`task5-recovery-timing-pester-direct.log` and `task5-recovery-timing-pester-invoke.log`.

The final restored Git blobs are:

- `ProloguePlaytestSession.cs`: `e1df5900495d918f977e65e40557f266cd1dea90`
- `ProloguePlaytestReport.cs`: `59fc2d443541aa8a4f2559314242c4cee3c60ea0`
- `ProloguePlaytestRecorder.cs`: `204ad5ad746434442e40685d3ed9aba24bd27a60`
- `ProloguePlaytestEvidenceGrantVerifier.cs` (capture mutation target, restored to HEAD):
  `aee606a9823550e306ba95c46452f8429b86e31c`

## Safety and human-evidence boundary

No `HandsOnLauncher`, capture, editor, or headless automation generated a hands-on report in
this task. Automated tests do not write human-note fields. The temporary focused runner
printed an in-memory JSON/Markdown fixture and was deleted before final verification. A real
10–15 minute human report remains a Task 6 user-owned gate and is not claimed here.

Task 5 was materialized from HEAD plus these task blobs under
`artifacts/task11-revision/task5-fall-context-materialized`; generated scenes, build outputs,
temporary runners, and logs stay there and are not staged.

## Frozen shared-worktree proof

Preflight captured the only overlapping tracked file,
`tests/powershell/Task4PlaytestRecorderTransition.Tests.ps1`, without using its user-dirty
content as Task 5 input:

- `task-5-preexisting.diff` SHA-256:
  `F0CF6D842663E1D6695C0DF015901CA46CB3D568D62206A7D4A5B29CD67650FB`
- `task-5-preexisting.manifest.txt` SHA-256:
  `91D2A75FE1865255E6F2C208862C424DE661DB8DC06CBA29268D41D7145692A5`
- shared dirty Pester file SHA-256 after Task 5:
  `0102CB552105625B0CA6759F8D9B5EC38DDA72DAF0925A969B24CF9E3DC55C8D`

All six protected shared scene hashes remain unchanged:

- `PrologueLevel`: `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`
- `CanyonEntrance`: `3F7844D5AFF22308D471EC5A99BCA9FB56F18BFA0C5F0618259FC87264A2C14D`
- `CanyonDescent`: `D5B981D9E350CD116D232F5762CC4E20B867795662314D9881455A1BA37236FC`
- `CanyonBasin`: `6594F7A707E42B74BABC27EA275FC8CBE81981E8CB9A1C0A3567FCD255104B38`
- `CanyonAscent`: `028B88B64EF11118CD36D2BB075260E72AA86E04A2DF61B6216C58B5BF14CC9C`
- `LostTemple`: `C50CF4CE9180834F09C42D61DB77F49AD97AA10DC0DCD77F55FCE4029B71DEBE`

Only the Task 5 source/tests, this report, and the ledger update are eligible for the final
temporary-index commit. No artifacts, `.tscn`, `project.godot`, user hunks, or human evidence
are eligible.
