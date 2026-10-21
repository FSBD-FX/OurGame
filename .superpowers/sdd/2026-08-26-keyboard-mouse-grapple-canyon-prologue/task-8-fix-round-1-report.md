# Task 8 Fix Round 1 Report — Continuous Canyon Gameplay Proof

- Date: 2026-08-26 (Asia/Shanghai)
- Reviewed base: `547963362157027a47b5c4c9f0d6b028d63db95c`
- Implementation commit: `496045a40b8d61cd659f5d07363a85155c35a034`
- Implementation tree: `ab417f83a4b9684bf08053c38c868489cc764dc2`
- Exact generated-main blob: `22227d078debd96b5eaaefb8b596f8b7e83d1b8e`
- Fresh detached verification commit: `521648413054abb0e193164155fdeb3830834ae5`
- Paid/formal art: none; all new presentation remains replaceable graybox

## Outcome

Fix Round 1 closes the Task 8 review's one Critical, four Important findings, and all named
evidence gaps. The production `PrologueLevel` now has a physically continuous Entrance → Descent
→ Basin → Ascent → Lost Temple route. One persistent generated player crosses all four seams,
opens BellChain through a real mechanical-claw cast, traverses the real shield hazard, and reaches
the physical Boss trigger without a post-spawn position/velocity assignment, teleport, early
respawn, or stream/camera discontinuity.

The continuous world remains authoritative. This fix does not restore `RoomRoot`, fabricate
`CurrentRoom`, or add legacy `TransitionTo` teleporting. The already-recorded
`PrologueUiResolutionCapture` incompatibility therefore remains explicit Task 9 Guidance / Task 11
audit migration debt.

## Review findings closed

### Four physical seams and recovery

`AddFallRecovery` now carves a 192-pixel recovery-free corridor around every authored bottom seam
instead of covering the entire sector edge. The generated-main proof loads the exact committed
`PrologueLevel.tscn`, assigns the player's initial spawn before tree entry, then uses only real
InputMap events and physics. Its final display run reports:

```text
TASK8_GENERATED_MAIN_SEAM_OK current=CanyonDescent frames=290 mounted=CanyonEntrance,CanyonDescent,CanyonBasin max_step=10.80 respawns=0
TASK8_GENERATED_MAIN_SEAM_OK current=CanyonBasin frames=630 mounted=CanyonDescent,CanyonBasin,CanyonAscent max_step=13.70 respawns=0
TASK8_GENERATED_MAIN_SEAM_OK current=CanyonAscent frames=1534 mounted=CanyonBasin,CanyonAscent,LostTemple max_step=13.70 respawns=0
TASK8_GENERATED_MAIN_SEAM_OK current=LostTemple frames=2148 mounted=CanyonAscent,LostTemple max_step=17.09 respawns=0
TASK8_GENERATED_MAIN_ROUTE_OK sectors=CanyonEntrance,CanyonDescent,CanyonBasin,CanyonAscent,LostTemple seams=4 max_step=17.09 mounted_max=3 respawns=0 boss_trigger=True capture=True
```

The route monitor also asserts one persistent player instance/parent, exact monotonic sector order,
mounted current sector, player and camera inside authored coverage, maximum per-frame displacement,
and no early spawn/recovery transition. Headless execution covers the first real seam and streaming
until the native DisplayServer pointer boundary; the full pointer-dependent route is authoritative
on the Windows display, while the isolated leaf matrix independently proves all claw physics.

### BellChain mandatory gate

The temple door now reaches the floor (`top=190`, `bottom=610`) and blocks the mandatory mechanism
lane while closed. A real E-mode mechanical-claw input and mouse cast reaches the production
`LegacyThreadInteractionAdapter`, applies the mechanism once, opens the door, permits forward
crossing, and leaves the quick-return route usable:

```text
TASK8_BELL_GATE_OK door_x=750.0 door_top=190.0 door_bottom=610.0 blocked_x=705.0 casts=1 latches=1 applications=1 crossed_x=834.4 return_x=652.1
TASK8_GENERATED_MAIN_BELL_OK target=(4760,1040) player=(4853.1934,1293.9985) open=True
```

The final Temple seam helper stays on the proven continuous entry geometry; the independent
`temple_mechanism` launch/landing contract is placed on the actual mandatory upper floor. This
keeps both the no-teleport generated-main route and all three real-input leaf samples green.

### Basin shield teaching lane

`CanyonShieldLaneHazard` is a narrow production graybox pulse hazard. Unguarded physical entry
takes damage and is knocked back/stopped. Holding the real right mouse button in the correct
direction crosses without damage while energy falls and heat rises. Release and lane exit prove
cooldown, energy regeneration, and heat recovery:

```text
TASK8_SHIELD_LANE_OK unguarded=damaged guarded=crossed energy=drained heat=increased recovery=observed
```

### Traversal distances, input, and double-jump contract

Both launch-to-landing and launch-to-anchor distances are strictly increasing for
`ascent_grapple_one < ascent_grapple_two < ascent_grapple_three`. Each route succeeds from the
20/50/80 launch samples with one real cast and latch. The third route completes in 216/205/185
physics frames in the final fresh run.

Every one of the 33 mandatory samples sends a real InputMap second-air-Space attempt and measures
the following physics frame. Accepted second jumps remain zero. Attached samples also prove claw
release does not synthesize a jump. No mandatory sample accepts Dash. `GripStone` now has a real
cast/latch/landing proof, and representative Q + left click uses real `InputEventAction`,
`InputEventMouseMotion`, and `InputEventMouseButton` events across engine frames without reflected
controller-frame injection:

```text
TASK8_GRIPSTONE_OK casts=1 latches=1 landing=(170,514)
TASK8_CANYON_TRAVERSAL_OK segments=11 samples_each=3 second_air_jump=0 dash=0 optional_pass_skip=2 boss_trigger=physical
```

### Authoritative registry

The exact 18 original Task 8 retirement names are replaced at their authoritative suite sources by
self-contained executable delegates. There is no blanket retirement filter and no prior-run
`CompletedEvidence` dependency. Exact-set equality, duplicate/unclassified rejection, and an
injected unrelated retirement all fail closed. The original aggregate and `RunTask7Fix1Tests`
execute successfully from a fresh process without a Task 8 pre-run:

```text
TASK8_REPLACEMENT_REGISTRY_OK original_names=18 executable=73 retired=2 classified=75 unexpected_retirement=rejected
TASK8_ACTIVE_REPLACEMENT_CASES_OK original_names=18 self_contained=True
TASK7_LEGACY_CASE_PLAN_OK suites=10 executed=73 retired=2 classified=75 fail_closed=True
```

Only the exact two Task 9 save cases remain retired.

## RED → GREEN evidence

The focused RED logs are local under `artifacts/` and remain intentionally unstaged. They reproduce
the reviewed failure classes rather than merely scanning source:

- `task8-fix1-red-structure-all.log`: full-width seam recovery, pass-under Bell gate, metadata-only
  shield lane, and non-increasing ascent geometry.
- `task8-fix1-behavior-red1.log` / `red2.log`: unguarded/guarded shield behavior and closed/open gate
  behavior fail before production geometry and hazard logic.
- `task8-fix1-registry-red.log`: the aggregate cannot run the exact 18 replacements independently
  in a fresh process and the unrelated-retirement mutation is not rejected.
- `task8-fix1-generated-main-red.log`: the exact generated main dies at the Descent → Basin seam.
- Final full-route replay then exposed and retained a genuine Temple seam geometry RED instead of
  accepting the provisional recording. The seam helper and leaf contract were separated, after
  which both leaf and generated-main proofs passed from final source.

Final GREEN logs include `task8-fix1-final-focused-green3.log`,
`task8-fix1-final-task7-aggregate.log`, `task8-fix1-final-generated-main-headless.log`,
`task8-fix1-final-generated-main-movie-green.log`, `task8-fix1-final-prologue-pre.log`, and
`task8-fix1-final-prologue-post.log`. Their fatal/error/leak signature count is zero; pre/post retain
only their intentional negative-path warnings.

## Final exact verification matrix

The index tree was verified in a clean detached worktree at temporary commit `5216484` before the
implementation commit was created from the identical tree:

| Gate | Result |
|---|---|
| `.NET 9 dotnet build Knight.csproj --no-incremental` | exit 0; 0 warnings, 0 errors |
| fresh Godot headless editor import | exit 0; `VERIFICATION_EDITOR_GC_DRAIN_OK` |
| `RunPrototypeTests.cs` | `PROTOTYPE_TESTS_OK` |
| `RunPrologueTests.cs -- --phase=pre` | `PROLOGUE_TEST_PHASE_OK phase=pre` |
| `RunPrologueTests.cs -- --phase=post` | `PROLOGUE_TESTS_OK` |
| `RunTask8Fix1Tests.cs` | `TASK8_FIX1_FOCUSED_OK` |
| fresh `RunTask7Fix1Tests.cs` | 73 executable / 2 retired / 75 classified; exit 0 |
| generated-main headless boundary | first seam/streaming green; pointer boundary named |
| generated-main Windows/NVIDIA 1280×720 fixed 60 | all four seams, Bell, Boss, exit 0 |

All six builders also pack and reload without node loss:

```text
BuildCanyonEntrance nodes_before=72 nodes_after=72
BuildCanyonDescent nodes_before=76 nodes_after=76
BuildCanyonBasin nodes_before=74 nodes_after=74
BuildCanyonAscent nodes_before=87 nodes_after=87
BuildLostTemple nodes_before=99 nodes_after=99
BuildPrologueLevel: five-sector authored canyon route packed and validated.
```

## Final continuous visual proof

The final proof was re-recorded after the last Temple geometry correction; earlier recordings are
provisional only.

- Source AVI: `artifacts/task8-fix1-proof/final-generated-main-route.avi`
  - 1280×720 MJPEG + stereo PCM, 60 FPS, 2,495 frames, 41.583 seconds
  - 119,187,202 bytes
  - SHA-256 `183D31A6C1A8E4238CA2D5AB29DEFABCD410C54DCB3D2626C10B12ED3A81D9DA`
- Practical MP4: `artifacts/task8-fix1-proof/final-generated-main-route.mp4`
  - H.264/AAC, 2,425,182 bytes; full decode exits with zero error
  - SHA-256 `09F202042B63850F2B7D28781DBF0CEFB9E18E19DA0C373A8EE73A7CE42E7FC7`
- Six final 1280×720 stills: `01-entrance.png` through `06-boss-trigger.png`
- Review sheets:
  - `artifacts/task8-fix1-proof/review/final-contact-sheet.png`
  - `artifacts/task8-fix1-proof/review/final-static-run.png`

All 2,495 MJPEG frames were decoded at reduced resolution for continuity checks. Blank frames: 0.
The longest near-identical sequence is 165 frames / 2.750 seconds at the Basin shield recovery
wait; manual review confirms the energy/heat UI changes through the interval, so it is not a frozen
frame. The contact sheet and six PNGs show the route, player, UI, grapples, Temple/Bell gate, and
Boss trigger. No blank camera, visible respawn/teleport, or stuck route was found.

## Commit boundary and dirty-worktree preservation

The fix expands the initial Task 8 16-file boundary to exactly 26 implementation/test files because
the independent review required new behavior/proof surfaces: one narrow hazard, the Bell gate
configuration, generated-main proof runners, physical fix tests, and seven authoritative registry
owners. No art, unrelated runtime, report, artifact, `.uid`, or user-dirty file entered the
implementation commit. `git diff --cached --check` passed and staged leak count was zero.

The exact generated main was staged from the builder output, then the user-owned worktree copy was
restored before commit. Its SHA-256 remains byte-identical:

```text
AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115
```

The implementation commit contains generated blob `22227d0` while the worktree intentionally still
shows the preserved user version as an unstaged modification. All other pre-existing dirty and
untracked files remain unstaged and untouched.
