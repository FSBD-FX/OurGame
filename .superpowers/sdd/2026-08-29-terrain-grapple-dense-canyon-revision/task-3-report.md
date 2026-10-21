# Task 3 report — dense canyon micro-scenes

## Outcome

Task 3 builds the five-sector canyon as fifteen named micro-scenes, with typed gameplay terrain,
recoverable catch layers, four one-shot claw mechanisms, and stable visual/evidence roots.  The
final materialized verifier is GREEN in both phases; POST ends with `PROLOGUE_TESTS_OK`.

The final commit SHA is reported by the delivery record. A commit cannot embed its own SHA in its
contents without changing that SHA, so this report intentionally records the immutable test and
file-hash evidence instead.

## Materialized GREEN proof

The materialization is canonical `e992423333997b9cdafaa6679674366b8b38ee4c` plus Task 3 only,
with the frozen user delta reverse-applied. Its builders ran in leaf-to-main order and packed:

| Builder | Validated nodes | Micro-scenes |
|---|---:|---:|
| CanyonEntrance | 84 | 3 |
| CanyonDescent | 154 | 4 |
| CanyonBasin | 158 | 3 |
| CanyonAscent | 201 | 3 |
| LostTemple | 125 | 2 |
| PrologueLevel | five-sector route | — |

- Build: `artifacts/task11-revision/task3-materialized/build-final-runner.log` — 0 warnings,
  0 errors.
- Builders: `artifacts/task11-revision/task3-materialized/task-only-builders.console.log`.
- PRE: `artifacts/task11-revision/task3-materialized/pre-task-only.console.log` —
  `PROLOGUE_TEST_PHASE_OK phase=pre`.
- POST: `artifacts/task11-revision/task3-materialized/post-task-only.console.log` —
  `PROLOGUE_TEST_PHASE_OK phase=post`, `PROLOGUE_TESTS_OK`.

Expected PresentationProfile/audio-resource diagnostics remain in the materialized logs, but the
test suite handles the fallback path and exits successfully; no assertion or compiler error is
present.

## Independent review repair — exact claw inventory

Independent review found that `CanyonContentBuilder` also created
`CanyonAscent/Gameplay/Mechanisms/AscentCounterweight`, silently making five generated
`CanyonClawMechanism2D` instances. The approved set is exactly four: Descent `Counterweight`,
Descent `HiddenPanel` in ArchiveAlcove, Basin `CargoPlatform`, and Temple `BellBody`.

This was repaired test-first in a fresh detached materialization of the committed Task 3 tree,
with no frozen user hunks:

- RED: `artifacts/task11-revision/task3-review-repair/red-exact-claw-inventory.console.log`
  exits 1 with `generated canyon contains exactly four claw mechanisms; expected=4, actual=5`.
  The new structure test enumerates all packed `CanyonClawMechanism2D` descendants and asserts
  both count and the exact sector-relative path plus kind of every approved instance.
- Minimal GREEN change: remove only the Ascent `AddMechanism("AscentCounterweight", ...)` call.
  Dependency search found no other `AscentCounterweight` references. `CounterweightWell` and
  `CounterweightWall` remain independent authored micro-scene/terrain content, not mechanism
  dependencies.
- Rebuilt leaf-to-main materialization: Entrance 84/3, Descent 154/4, Basin 158/3, Ascent 194/3
  (seven self-contained mechanism nodes removed), LostTemple 125/2, then the main route.
- Focused: `artifacts/task11-revision/task3-review-repair/green-focused-catch-one.console.log`
  reruns the 9/9 sightline and all three Catch probes; it records the same real CatchThree
  `(882.1267,630)` 514.29 px ray-notch proof and the boost/release/stable-landing contracts.
- Final task-only PRE after the mechanism-mutation restoration:
  `artifacts/task11-revision/task3-review-repair/final-pre-task-only.console.log` ends
  `PROLOGUE_TEST_PHASE_OK phase=pre` and includes `TASK3_CLAW_MECHANISMS_OK kinds=4`.
- Final task-only POST after the mechanism-mutation restoration:
  `artifacts/task11-revision/task3-review-repair/final-post-task-only.console.log` ends
  `PROLOGUE_TEST_PHASE_OK phase=post` and `PROLOGUE_TESTS_OK`.

The related repeat-activation mutation was rerun after the repair:
`artifacts/task11-revision/task3-review-repair/mechanism-repeat-red-rerun.console.log` exits 1
at `Counterweight rejects a repeated adapter interaction after activation`, then restores the
detached materialization source byte SHA-256
`56273CEDF9FC00D7C596C6462395F8D921C8123E64ADFF21984C4DBB985AFCE4`.
That materialization uses CRLF working-tree bytes; its restored Git content is clean against the
same `d712fa9332889620e8e5fed386b1378c0e6c34bc` mechanism blob. The earlier LF-materialization
mutation log remains separately reproducible with its recorded `374837...` byte hash.

## Dense-route focused evidence

`artifacts/task11-revision/task3-ray-notch-rerun6/probe.console.log` proves the constrained
ascent geometry:

- `TASK3_ASCENT_SIGHTLINE_MATRIX_OK`: all 9/9 main-anchor LOS samples reach their exact typed
  target.
- CatchOne real hit `(834, 1079.2617)`, CatchTwo `(1034.7826, 850)`, and CatchThree
  `(882.1267, 630)` are within the required 360–520 px recovery range.
- `TASK3_CATCH_THREE_RAY_NOTCH_OK`: CatchThree hits the real ceiling underside at 514.29 px,
  with AnchorTwo clearance 14.30 px, LandingThree ray notch 27.99 px, AnchorThree gap 37.18 px,
  and 12.00 px vertical headroom. This replaces the rejected 2.5 px-clearance approach.
- Each catch proof records `TASK8_ATTACHED_SECOND_SPACE_OK` with one attached airborne Space
  boost and explicit LMB release, plus `TASK3_CATCH_RETURN_OK` with zero Dash, a 15% landing
  inset, 12 stable frames, and zero KillZone entries.

The final POST independently repeats the same 9/9 and all three real Catch proofs.

## Repaired cross-test assumptions

### ArchiveAlcove

Root cause: the Archive floor ended at y=1240 and the skip floor began at y=1310, a 70 px gap for
a 72 px-tall player body. The optional route could therefore pin the player even though it looked
like a pass-through.

Minimal content change: `ArchiveFloor` is one-way. It is still a physical top support, while the
lower skip route can pass beneath it; no clearance gamble or hidden teleport was added. Final POST
proves `TASK8_OPTIONAL_ROUTE_OK sector=CanyonDescent path=ArchiveAlcove pass=True skip=True
returnable=True`.

### BellChain range assertion

Task 1 (`5b2c97b`) increased the default claw maximum range from 520 to 620. The old
`BellChainRuntimeTests` out-of-range setup used 521 px and had become a valid cast. The minimal
test-only adaptation positions the player at `MechanicalClawSettings.Default.MaxRange + 1` (621 px
at the current default), preserving the real `OutOfRange` behavior rather than weakening it. No
production range or mechanism behavior changed.

### Persistent Guidance and Completion settings

Full post exposed test order dependence through the shared `user://knight/settings.json`: a prior
fixture could persist Guidance disabled, so later visual and pause checks began from the wrong
state. `PrologueGuidanceRuntimeTests` and `PrologueCompletionTests` now route each fixture to a
unique temporary save/settings directory and dispose it after audio drains. Direct focused cases
and the final full POST pass with no global settings mutation.

### Inspectable evidence labels and frozen source boundary

The legacy route-evidence contract requires `VisualRoot/Effects/SectorLabel` for every sector.
That label is a pre-existing common-root responsibility in `PrologueCanyonBuildParts`, not Task 3
content geometry. A frozen user hunk removes it in the shared dirty tree. During audit, an early
materialization was found to contain that frozen hunk, so its evidence result was discarded. The
final task-only materialization restores the base common label, does not add a duplicate in
`CanyonContentBuilder`, and passes both full phases. The task-only builder log reports the expected
84, 154, 158, 201, and 125-node leaf scenes.

## Negative mutation proof

Each mutation was made only in the materialized project, rebuilt, and had to return a nonzero
test exit. The exact mutated source hash and restored baseline source hash are recorded in each
log; after every restoration all five leaf scenes and the main scene were rebuilt.

| Case | Direct failure observed | Restored source SHA-256 | Log |
|---|---|---|---|
| Missing catalog marker | exact catalog ID expected `BrokenBridgeLookout`, got mutation | `2A539689D542669B8B451271BF282DFA756575F20800C318819E9B7BB095E3E3` | `artifacts/task11-revision/task3-mutations-task-only/01-missing-catalog-marker.console.log` |
| Gameplay body without typed profile | `CanyonEntrance/StartFloor owns a typed collision-hit profile` | `35B68B349997624837FAF37B4D0CA19A035D6A39820E65005CB9A8D4E79BF94E` | `artifacts/task11-revision/task3-mutations-task-only/02-missing-typed-profile.console.log` |
| Catch layer below 320 px | `ascent_grapple_one ... actual=352.00` | `35B68B349997624837FAF37B4D0CA19A035D6A39820E65005CB9A8D4E79BF94E` | `artifacts/task11-revision/task3-mutations-task-only/03-catch-beyond-320.console.log` |
| Repeated mechanism movement | `Counterweight rejects a repeated adapter interaction after activation` | `3748375F7CEAB8C95D4E452DE8AAFD0687EDA114A0F0FA6E70ACA5370248B84C` | `artifacts/task11-revision/task3-mutations-task-only/04-repeated-mechanism-movement.console.log` |
| Reintroduced mandatory Dash | `Dash is not allowed on a mandatory traversal segment` | `35B68B349997624837FAF37B4D0CA19A035D6A39820E65005CB9A8D4E79BF94E` | `artifacts/task11-revision/task3-mutations-task-only/05-mandatory-dash.console.log` |

## Protected-file preservation

No generated scene or `project.godot` is staged. The original worktree retains its required
protected scene content byte-for-byte:

| File | SHA-256 |
|---|---|
| `scenes/prologue/PrologueLevel.tscn` | `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115` |
| `scenes/prologue/sectors/CanyonEntrance.tscn` | `3F7844D5AFF22308D471EC5A99BCA9FB56F18BFA0C5F0618259FC87264A2C14D` |
| `scenes/prologue/sectors/CanyonDescent.tscn` | `D5B981D9E350CD116D232F5762CC4E20B867795662314D9881455A1BA37236FC` |
| `scenes/prologue/sectors/CanyonBasin.tscn` | `6594F7A707E42B74BABC27EA275FC8CBE81981E8CB9A1C0A3567FCD255104B38` |
| `scenes/prologue/sectors/CanyonAscent.tscn` | `028B88B64EF11118CD36D2BB075260E72AA86E04A2DF61B6216C58B5BF14CC9C` |
| `scenes/prologue/sectors/LostTemple.tscn` | `C50CF4CE9180834F09C42D61DB77F49AD97AA10DC0DCD77F55FCE4029B71DEBE` |

The pre-existing source/test dirty-delta snapshot is
`2A2FFEB29122E8D407FA8E031AF0EA7ED632B963400B848060B05BA4BF4A5663` and is deliberately
kept unstaged. An isolated task-only index leaves exactly its 20 insertions/11 deletions in the
working tree; its hunk coordinates differ after Task 3 source insertions, but its changed lines
match the snapshot. The protected generated-scene snapshot remains
`DBEB903021A9346B0145EE1D694CF7C531ABBC599EAB260FD591FB63AEBF35D1`.

## Scope and residual risk

The staged slice is limited to Task 3 builders/content/mechanism/tests plus the three narrowly
necessary test-isolation/compatibility repairs above and this report/ledger. It excludes all
generated `.tscn` files, `project.godot`, artifact logs, and user/other-agent dirty hunks.

The 12 px CatchThree headroom is intentional and asserted, not an implicit visual tolerance.
Future changes to player collider height, landing height, or ceiling geometry must rerun the
ray-notch focused proof and materialized post suite.
