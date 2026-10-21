# Task 4 report — traversal delivery proof and durable status

## Outcome and boundary

Task 4 automated delivery evidence is review-ready. The presentation proves the shipped contract through real input, movement, collision and ThreadAbility state: no double jump; exactly one accepted directional air Dash per airborne cycle; a second Shift in the same airtime is rejected without a second burst; and mechanical thread is one discrete pull per assisted segment rather than an elastic swing. The complete one-fixture Upper route reaches the mechanism without Dash, mid-route reposition, direct velocity writes or direct exit touching.

This does not close the project goal. A normal-speed human traversal run still has to confirm that the mandatory route suggests no unknown combo or suspected double jump, pull strength feels sufficient, and prompts/anchors are readable and comfortable.

## Schedule and frame-budget RED

The retained presentation evidence through frame 970 and the old Lower entry left only 165 frames for the real Upper route. Task 3's complete continuous evidence measured 589 physics frames, producing the named RED:

```text
TRAVERSAL_FRAME_BUDGET_RED available=165 required=589 deficit=424
```

Controller ruling 2 approved the smallest honest schedule: `FinalFrame=2869`, setup at frame 995, continuous Upper proof on frames 996–1584, and a +589 shift for every old event/window at or after 1015. The retained downstream schedule is ToLower 1604, Lower 1629–1709, Laborer/Mechanism 1739–1994, Boss 2039–2741, return 2749, audio drain 2779–2869, and final visible/complete at 2869.

## TDD and opposing evidence

- Initial RED: `traversal-task4-stage-order-red.log` built with 0 warnings/0 errors, ran the full former 2280-frame presentation, then failed because all seven traversal stages were absent: `actual=0 []`.
- First real route RED: AnchorLow genuinely Applied and landed at frame 1144, but the formal discrete tether had naturally cleaned up before landing. Controller ruling 3 correctly retained immutable consecutive Preview/Travel/Latched/Apply evidence through the later supported landing instead of inventing a persistent swing.
- Final GREEN after exact-world-marker audit hardening: `traversal-task4-final-marker-green3.log` ends with `PROLOGUE_PRESENTATION_FINAL_VISIBLE frame=2869 state=Title level_inside_tree=True` and `PROLOGUE_PRESENTATION_COMPLETE frame=2869`.
- Missing-stage mutation: `KNIGHT_PRESENTATION_DROP_TRAVERSAL_STAGE=single_jump` exits 1 at frame 368 with `expected=single_jump actual=air_dash_consumed`.
- Invisible-thread mutation: `KNIGHT_PRESENTATION_HIDE_TRAVERSAL_THREAD=1` exits 1 at frame 1144 with `upper_anchor_low ... frames=0`.
- Both mutation environment variables were removed after their runs. Production state was restored before capture and both delivery gates.
- Task 4 reused Task 3's independently approved continuous helpers and their actual-body Position/GlobalPosition/Velocity/Fixture/direct-exit guards. No source guard or gameplay helper was weakened or reimplemented.

## Live traversal evidence

| Ordered stage | Published frame | Live evidence |
| --- | ---: | --- |
| `single_jump` | 327 | Real InputMap jump left and returned to the same supported floor; Dash/thread deltas stayed zero |
| `air_dash_consumed` | 368 | Up+Shift accepted once while airborne and consumed `AirDashAvailable` |
| `second_dash_rejected` | 377 | Later same-airtime Shift incremented rejected count, accepted count stayed one, two post-reject frames had no discontinuity |
| `upper_anchor_low` | 1144 | Exact target, casts=1, applies=1, exact-anchor visible run=9 (`1101..1131` evidence interval), later supported LowerSwing landing |
| `upper_anchor_high` | 1204 | Exact target, casts=1, applies=1, exact-anchor visible run=9 (`1163..1190` evidence interval), later supported MidSwing landing |
| `upper_rebound` | 1258 | Exact target, casts=1, applies=1, exact-anchor visible run=9 (`1218..1244` evidence interval), later supported HighSwing landing |
| `upper_landed` | 1410 | Real final single jump, supported mechanism arrival, real exit request/current-room transition, zero Dash and zero thread residue |

The deliberate NoTarget attempt enters real `ThreadCastState.Failed`. Failure feedback is visible on frames 619–624 (6 consecutive frames), line/shuttle/target state is clear at frame 625, and the next real Anchor Preview proves retry readiness. Each successful sample also requires the exact in-world `ThreadAnchor` formal marker to be effectively visible and colocated with its typed target within 1 px while the shuttle endpoint is on that target. Successful assisted markers are published only after their genuine Apply record and later LandingZone landing. Normal Recovery/Idle cleanup before landing is allowed; cleanup before Apply, target mismatch, absent marker/visual evidence or residue fails.

## Capture and visual review

Fresh outputs from both `tools/capture-prologue.ps1` modes:

- `screenshots/prologue/prologue.avi`: 149,426,866 bytes, 1280x720, fixed 60 FPS, 2869 frames, 47.8167 seconds, SHA-256 `42822C6D6182A7A7A4A7C87AB7B5162DDE85C9912ECB71C643FC45CD75135ABD`.
- `screenshots/prologue/boss-retry.avi`: 34,842,714 bytes, 1280x720, fixed 60 FPS, 600 frames, 10 seconds, SHA-256 `6835A4F167DB9243D007354F1311149FFC7B4320ABD7A69DD6A3E66395228097`.
- `screenshots/prologue/prologue-frames`: exactly 2869 numbered PNGs.
- `screenshots/prologue/boss-retry-frames`: exactly 600 numbered PNGs.
- Both modes print `PROLOGUE_CAPTURE_OK`, require the seven ordered stages and all earlier presentation/motion/thread/attack markers, retain visible tails and report no engine errors.

The compact untracked review set is `screenshots/prologue/traversal-review` (23 PNGs): jump 305/312/327; accepted Dash 367/368; rejected Dash 374/375/377; failure feedback 619/620/624 and cleanup 625; low line 1101/1102 and landing 1144; high line 1163/1164 and landing 1204; rebound line 1218/1219 and landing 1258; mechanism arrival 1409/1410. Direct inspection found a continuous ordinary-jump silhouette, no false second-jump/burst discontinuity, visible unambiguous exact anchors and line/shuttle pairs, clean supported landings, complete Failed cleanup, real mechanism arrival, no launch/landing overlay occlusion, and no stretched visual.

## Two complete delivery gates

Both restored-state sequences ran the four binding commands: build, `RunPrototypeTests`, `RunPrologueTests`, then `tools/verify-prologue.ps1`.

| Gate | Build | Prototype | Prologue | Route | Upper smoke | Traversal | Motion | Targets | Unexpected errors | Verify |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 0 warnings / 0 errors | 1 | 1 | 10 | 4 | 7 | 11 | 7 | 0 | `PROLOGUE_VERIFY_OK` |
| 2 | 0 warnings / 0 errors | 1 | 1 | 10 | 4 | 7 | 11 | 7 | 0 | `PROLOGUE_VERIFY_OK` |

Relevant final-source logs are `traversal-task4-fix1-gate{1,2}-{build,prototype,prologue}.log`, `traversal-task4-fix1-gate{1,2}-verify.console.log`, `verify-prologue-tests.log`, and `verify-prologue-attack-presentation.log`. The 10 route contract lines, 4 Upper smoke lines and 7 ordered stage lines occur exactly once in their owning run.

## Durable README and asset boundary

README now states the no-double-jump/one-air-Dash/discrete-pull contract; contains a six-room table with all ten stable IDs classified as `SingleJump` or `Assisted`; identifies Dash as `OptionalAdvanced` and absent from every mandatory contract; records the 2869/600 proof paths and reviewed frames; and keeps automated acceptance distinct from the pending human run. No paid assets were generated. The existing asset table and replaceable production roots remain intact.

## Scope and dirty-state preservation

Intended staged scope is exactly:

1. `README.md`
2. `tests/presentation/ProloguePresentation.cs`
3. `tests/presentation/PrologueVisualAudit.cs`
4. `tools/capture-prologue.ps1`
5. `tools/verify-prologue.ps1`
6. `.superpowers/sdd/2026-08-22-first-level-traversal-contract/progress.md`
7. `.superpowers/sdd/2026-08-22-first-level-traversal-contract/task-4-report.md`

`tools/capture-prologue.ps1` began Task 4 with exactly eleven previously approved `PROLOGUE_MOTION_OK` required-marker lines. They were reconciled against the presentation's eleven motion markers, retained unchanged, and are intentionally included with the new frame/stage requirements. Screenshots, captures, logs, imports, scenes, assets, production/gameplay/tuning code, builders, Task 3 tests/smoke and every unrelated dirty file remain untracked or unstaged. Commit hash is reported in the parent handoff after independent staged-scope review.

## Fix Round 1 — exact line geometry and effective marker visibility

The independent review's two Important false-positive paths are closed in the real presentation observation path without changing production, gameplay, scenes, tuning, capture scripts, or README:

- A successful assisted visual now requires finite `LastWorldStart` within 12 px of the real player origin, finite `LastWorldEnd` within 1 px of the exact typed target, and at least one actual strand terminal within 1 px after converting that strand's own local terminal through its own `Line2D.ToGlobal`. The 12 px start tolerance is one 60 Hz physics step at the production 720 px/s pull speed; it accommodates callback ordering without accepting a detached origin. The exact shuttle endpoint, active line, exact formal target and two-consecutive-frame requirements remain binding.
- The formal anchor marker now requires finite effective global basis lengths above `.001`, a non-degenerate effective transform, and effective alpha above `.001`. Effective alpha follows CanvasItem semantics: the marker's `SelfModulate.A` is multiplied by its own and every CanvasItem ancestor's `Modulate.A`; ancestor `SelfModulate` is intentionally excluded because it does not propagate to children.
- The line API was inspected rather than guessed: `GetStrandPoints(index)` returns points in that strand's local basis, index 0 is the main `ThreadLineVisual`, and indices 1–2 are `Strand2`/`Strand3`. Each terminal is therefore converted by the actual strand node.

Strict TDD first established both prior false greens. Before the new assertions, `KNIGHT_PRESENTATION_WRONG_TRAVERSAL_LINE_END=1` and `KNIGHT_PRESENTATION_ZERO_TRAVERSAL_MARKER_SCALE=1` each ran the full presentation with exit 0 and published the success stage. After the assertions:

- Wrong line end exits 1 at frame 1144 before `upper_anchor_low`: `visible=0 origin=0.000/12.000 line_end=80.000/1.000 strand_end=80.000/1.000 marker_scale=(1.000,1.000) marker_alpha=1.000`. The shuttle remains on the correct target while the real line is deliberately presented to `target + (80, 0)`.
- Zero-scale formal marker exits 1 at frame 1144 before `upper_anchor_low`: `visible=0 origin=0.000/12.000 line_end=0.000/1.000 strand_end=0.000/1.000 marker_scale=(0.000,0.000) marker_alpha=1.000`.
- Both mutation environments are removed after execution. The marker's original scale is recorded for restoration in the observation fixture; the expected failing process owns no persistent scene state.

The restored formal run reaches frame 2869 with all seven ordered traversal stages exactly once. Each assisted stage has exactly one real cast and Apply, nine consecutive exact-anchor frames, and `max_origin_delta=0.000`, `max_line_end_delta=0.000`, `max_strand_end_delta=0.000`, `marker_scale=(1.000,1.000)`, `marker_alpha=1.000`. Fresh AVI and FramesOnly capture produced the hashes/counts above and exactly 2869 + 600 PNGs. Frames 1101/1102, 1163/1164 and 1218/1219 were re-reviewed as consecutive player-to-exact-anchor line/shuttle pairs; 1143/1203/1257 show supported cleanup landings and 1409 shows real mechanism arrival with no residue.

Two final complete delivery gates each passed build, Prototype, Prologue and `verify-prologue.ps1`: build 0 warnings/0 errors; markers `1/1/10/4/7/11/7` for Prototype/Prologue/route/Upper/traversal/motion/target lifecycles; exactly one `PROLOGUE_VERIFY_OK`; zero unexpected runtime errors. The verifier's one logged `FORCED_SMOKE_FAILURE` is its expected opposing mutation. README remains hash-consistent because it records the unchanged proof paths, dimensions, frame counts and duration rather than a content hash; no README edit is needed for this audit-only fix.
