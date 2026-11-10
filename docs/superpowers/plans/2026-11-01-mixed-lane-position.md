# Mixed-lane ordinary positioning (v190)

> Root implements and serializes native runs. Required execution skill: superpowers:executing-plans.

Goal: test a legal ordinary A/neutral passage on the existing LiftBackLanding, before the old Rust J stops forward progress. Obtain actual stance evidence; do not substitute it for the unchanged owned Burst Shot/J/K/Defeated or full Main requirements.

Facts: Player and Rust body mask World only, whereas claw LOS includes Enemy. Thus a LOS blocker is not a walking wall. The live v189 Main bound Burst HP2 but never issued K; all 61 captured admission opportunities selected another target. Rust-body LOS obstruction remains an inference, not an unchosen-target ray receipt.

Architecture: a test-only, one-attempt MixedLanePositionControl. Use live axis-aligned body rectangles and the named static shelf to find the non-overlapping center interval between Burst and Rust. Predict held A then neutral with the existing FreeMotionForecast/actual Motor settings/module/delta. Consume only one original outer tick per Step; no inner wait pool. Begin only with actual shelf floor contact, inward velocity, no competing action/guard/claw, and the original all-source safe minimum exceeding predicted steps plus 3 sample ticks. Arrival can still have a partial overhang: permit only inward, monotonically decreasing initial overhang until full support; once full, require it throughout. Stop requires actual later neutral input, physical keys released, actual velocity <= .5, exact same full shelf, retained actors/HP/jump/dash/cast/edges and unchanged geometry. No extra K is issued or accounted as an owned K.

If a new danger or invalid support invalidates the attempt, fail explicitly; no reset/retry, invulnerability, borrowed guard, hidden teleport or relaxed proof. Completed tick releases directions and consumes its one original tick; next outer performs a fresh normal danger scan. Production rules, all original receipts and 900 budget remain unchanged.

Files: new tests/presentation/MixedLanePositionControl.cs; existing KeyboardActionRuntimeTests.cs and MainRouteTests.cs; canonical selected snapshot list only; docs/README/backlog. SourceRows should become1872 (verify). No production/map/number/priority edits.

- [x] TDD: c9b86f26 native916ms/exit1 at intended planner assertion; original raw retained.
- [x] Implemented discrete forecast, retained support/HP/action checks and one-tick ownership. f243a7a9 actions8498ms PASS; six arithmetic controls plus actual no-owner D preservation. These do not certify battle safety or K.
- [x] One changed Main4f0650b8: actual ordinary walk/neutral stop completed in31 original outer ticks, HP2 and support retained. Entire Main151396ms strictFAIL after later death; zero owned K issued. No unchanged-input rerun.
- [x] One fresh final code review C0/I0/M2; Minor rulings recorded, no repair/re-review. Models/actions/post strictPASS; Main/pre strictFAIL retained. All source/helper frozen during native; six-case custody audit closed.
- [x] v190 report, README/backlog, raw outcomes and manual media check recorded. Original eleven remainACTIVE/OPEN, no full completion claim.

Unique final source review: C0/I0/M2, no required repair or second review. Minor1 bounded48-row log saturation and Minor2 active queued-player standalone Dispose cleanup deferred (current34rows; enclosing route ReleaseAll/Dispose remains), with standalone reuse requiring new negative coverage. Code-quality adoption does not qualify old handoff/Burst K/full Main. Current model subsetPASS637ms; pre original180s timed out at180058ms/strictFAIL, no cap increase/retry. Post still running when this note was written; final qualification belongs in v190 report.

Final addendum: post1504a97b strictPASS224194ms within original300000ms;75orderedpairs/owner8/4limitedwarnings. Sixcases3PASS3FAIL (includesintendedRED), final5sameversion3PASS2FAIL. Custody30pairs/2908source/64helper/36bin consistent,12oldownedidentitiesended (onePIDreused;notoperated), current1872source/6bin/post10helper bound. HEAD/index unchanged. Manualmedia7427750/86/Notifyfalse. No secondreview, codefix or newMainrerun. Detailed outcomes/rulings in v190 report.

Authority: only isolated headless builds/tests, not GUI/OS input/real saves/paid assets/crash capture/automation/deletion/Git writes. Unique dirty project: no worktree or commit operation; frozen sources and case copies preserve rollback provenance. Existing goal remains ACTIVE. Prior turn progress = early enrollment and wrong-preview evidence, not successful K/Main.
