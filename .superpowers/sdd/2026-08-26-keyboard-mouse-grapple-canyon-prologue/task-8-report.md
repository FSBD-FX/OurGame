# Task 8 Implementation Report — Five-Sector U-Shaped Canyon Graybox

- Date: 2026-08-26 (Asia/Shanghai)
- Worktree: `E:\OurGame\Knight\.worktrees\player-art-pipeline`
- Branch: `codex/player-art-pipeline`
- Baseline: `36df716d1f050a032a20a023ed9c7f21851fc31e`
- Implementation commit: `379175021c8c0be05952ef74c6209c42e45a5e97`
- Implementation tree: `b1490bb1714fae2ed65d0a237d068fc46ebdd6f6`
- Evidence-report commit: the commit containing this report, immediately after `3791750`
- Paid/formal art: none; all sector presentation is replaceable graybox geometry

## Outcome

Task 8 replaces the five Task 7 placeholder sectors with five authored, continuous U-shaped
canyon scenes: `CanyonEntrance`, `CanyonDescent`, `CanyonBasin`, `CanyonAscent`, and
`LostTemple`. The generated main scene references those exact leaf scenes through
`WorldRoot/Sectors`; it does not restore `RoomRoot`, legacy room teleporting, or mandatory Dash.

Every sector has authoritative bounds, entrances/exits, collision, fall recovery, seam-safe
critical interactions, and the exact stable visual layers
`VisualRoot/{Background,Midground,GameplayReadable,Foreground,Effects}`. The authored mainline
contains all 11 required named traversal segments. `SupplyCave` and `HighRuin` are both physically
passable, skippable, and returnable. The Fallen Bellkeeper arena remains open/inactive before the
player body crosses its trigger and locks/activates only after the physical crossing.

## Authored layout and content

| Sector | Local bounds | World offset | Required content |
|---|---|---|---|
| CanyonEntrance | 1280×720 | (0, 0) | broad teaching floor, low-pressure Rust Shell, move/attack route |
| CanyonDescent | 1280×1440 | (1280, 0) | GripStone, DedicatedAnchor, visible Ice rejection, catch shelves |
| CanyonBasin | 1600×720 | (1280, 1440) | shield lane, first checkpoint, collapsed machinery, SupplyCave |
| CanyonAscent | 1280×1440 | (2880, 720) | three assisted grapple climbs, catch shelves, HighRuin |
| LostTemple | 1920×720 | (4160, 720) | mechanism, second checkpoint, shortcut, Boss trigger/arena |

The exact mandatory route is:

1. `entrance_move_attack`
2. `descent_grip_lesson`
3. `descent_ice_rejection`
4. `basin_shield_lane`
5. `basin_checkpoint`
6. `ascent_grapple_one`
7. `ascent_grapple_two`
8. `ascent_grapple_three`
9. `temple_mechanism`
10. `temple_checkpoint`
11. `temple_boss_entry`

All mandatory ability masks exclude Dash. The three ascent routes reject an ordinary-jump-only
solution, provide a lower catch shelf, and succeed with one real mechanical-claw latch from each
of three reasonable launch samples.

## RED → GREEN evidence

The new structural test was first run before any Task 8 sector existed. It failed with exit 1 at
`PrologueCanyonStructureTests.cs:48`:

```text
Assertion failed: generated canyon sector exists: CanyonEntrance
```

After implementing the leaf builders/scenes and physical tests, a clean detached tree at
`E:\OurGame\Knight\.worktrees\task8-final-52f2e75` was built and imported from scratch. The final
fresh gates were:

| Gate | Exit | Evidence |
|---|---:|---|
| `.NET 9 dotnet build Knight.csproj --no-incremental` | 0 | `0 warnings`, `0 errors` |
| Godot headless editor import | 0 | import completed without Task 8 parse/resource errors |
| `RunPrototypeTests.cs` | 0 | `PROTOTYPE_TESTS_OK` |
| `RunPrologueTests.cs -- --phase=pre` | 0 | `PROLOGUE_TEST_PHASE_OK phase=pre` |
| headless `RunPrologueTests.cs -- --phase=post` | 0 | all Task 8 markers below; `PROLOGUE_TESTS_OK` |
| Windows display-backed post, 1280×720, fixed 60 FPS | 0 | OpenGL 3.3 / NVIDIA RTX 3060; same full Task 8 matrix; `PROLOGUE_TESTS_OK` |
| six builders rerun from final source | 0 | exact node counts below; main level packed/validated |

Final builder pack/reload counts were exact:

```text
BuildCanyonEntrance nodes_before=72 nodes_after=72
BuildCanyonDescent nodes_before=74 nodes_after=74
BuildCanyonBasin nodes_before=74 nodes_after=74
BuildCanyonAscent nodes_before=87 nodes_after=87
BuildLostTemple nodes_before=91 nodes_after=91
BuildPrologueLevel: five-sector authored canyon route packed and validated.
```

The final headless and display-backed post runs both emitted:

```text
TASK8_CANYON_STRUCTURE_OK sectors=5 segments=11 visual_layers=5 seam_margin=96
TASK8_CANYON_TRAVERSAL_OK segments=11 samples_each=3 second_air_jump=0 dash=0 optional_pass_skip=2 boss_trigger=physical
TASK8_ACTIVE_REPLACEMENT_CASES_OK structure=12 traversal=6 total=18
TASK7_LEGACY_CASE_PLAN_OK suites=10 executed=73 retired=2 classified=75 fail_closed=True
PROLOGUE_TEST_PHASE_OK phase=post
PROLOGUE_TESTS_OK
```

The third, longest ascent route completed from launch samples 20/50/80 with 206/209/213 physics
frames respectively; every sample recorded exactly one cast and one latch. The other two ascent
routes likewise completed all three samples with one cast/one latch, zero Dash, and no accepted
second air jump. Entrance uses a real Q+mouse attack edge, Descent uses real E+mouse grapple and
Ice rejection input, and Basin crosses the shield lane while the real right-mouse shield remains
active.

Task 8 replaces all 18 Task 8-owned Task 7 retirement entries with active, named structure and
traversal cases. Only the two exact Task 9 save cases remain retired:

- `PrologueGameFlowTests.CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart`
- `PrologueGameFlowTests.ContinueAndCorruptRecovery`

No Task 8-required case is skipped.

## Known cross-task production-audit migration debt

`tools/audit-prologue-production.ps1 -SkipBuild` reaches the real Windows/NVIDIA display driver,
then exits 1 in its first legacy UI capture with:

```text
PROLOGUE_UI_RESOLUTION_FAIL: initial first-level teaching is not visibly expanded at (1280, 720)
```

Root cause was traced rather than masked: `PrologueUiResolutionCapture` still requires the removed
six-room main `CurrentRoom` and later calls legacy `TransitionTo`. The Task 7/8 main intentionally
has `RoomDefinitions=0`, `CurrentRoom=null`, and a continuous sector world, so
`PrologueGuidanceController._Process` cannot drive that old capture. Reintroducing `RoomRoot`,
fabricating `CurrentRoom`, or accepting legacy teleporting would violate the approved architecture.
Task 9 owns sector-aware guidance/progress migration and Task 11 explicitly owns rebuilding the
production audit for all five sectors. The dedicated Task 8 Windows 1280×720 fixed-60 full post is
green and exercises the actual Task 8 route; this legacy incompatibility is therefore recorded as
named future ownership, not hidden by a skip or false success marker.

## Exact generated-scene and dirty-worktree preservation

The implementation commit contains exactly 16 authorized files. Before commit:

- exact index tree: `b1490bb1714fae2ed65d0a237d068fc46ebdd6f6`
- generated main-scene index blob: `3644c510a6b58a66303c3a1102126fe3ea4c51d3`
- `git diff --cached --check`: exit 0
- temporary direct runners: absent
- all non-Task8 dirty/untracked files: preserved and unstaged

The user-owned working copy of `scenes/prologue/PrologueLevel.tscn` remained byte-identical before
and after the implementation commit, SHA-256:

```text
AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115
```

The authored generated main blob was committed from the exact index while that worktree copy
remained untouched.

## Deferred by the approved plan

- Task 9: continuous sector progress, checkpoint restore/save semantics, sector-aware first-level
  guidance, and playtest telemetry.
- Task 10: final keyboard/mouse HUD and reticle migration.
- Task 11: final production audit/capture, presentation replacement, sound, and human playtest.
- Formal/paid art remains out of scope; all graybox visual nodes keep stable replacement paths.
