# Versioned minimap grid implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL superpowers:executing-plans inline. Root sole writer/native, read-only recon and one fresh whole-increment review; no per-task reviewer/Git helpers under current authority.

**Goal:** Enable saved grid16 large-map catalogs end-to-end with oldgrid8 bytes/behavior exact and bounded visibility/work.
**Architecture:** Version catalog payload/fingerprint; immutable CellSize feeds existing model/scheduler/view/shader. ReviewI1 adds optionalmapformat2fornew16 whileold8format1/defaultwire/core4remainexact; nooldcoremigration.
**Tech Stack:** Godot.NET4.7.1/net9/C#,1280×720.
**Spec:** `G:/knight/Knight/docs/superpowers/specs/2026-11-10-minimap-versioned-grid-design.md`.

## Global Constraints

Spec budgets:1048576bits each/64candidates/256expansions/2rebuilds/160worldradius/64loadedbounds/256occluders; boundary segments≤8/stroke8/buckets64. Onlygrid8/16, default8/schema1 exact, schema2explicit16. No gameplay or current saved-scene rewrite; no source/helper edits while owned native live. Current v3421953source/15EE, sixbinary3157,user9FB16 baseline; oldstrictfailures retained. User opted out of routine design/execution prompts; no Git/worktree/cleanup and root inline workflow by existing authority.

## Review Focus

- Alternate raw schema/grid/counts or late invalid geometry must not publish partially configured catalog.
- Same-size/revision-looking catalog switches cannot reuse old cache pixels/uniform or clear core save data.
- grid16 must not reveal behind32thick walls, unloaded gaps,160radius corners, or starve a near cell behind dense boundary entries.
- Narrow32/48/64 corridors and offsets must remain conservative with fine known outlines; do not certify pixels without rendering.
- grid8 stored bytes/fingerprint/legacy migrations remain exact; old bits must not be misread as grid16.

## Task1 — serialized grid and bounded model

Files: new`tests/integration/MinimapVersionedGridTests.cs`; modify Catalog/Builder/Model/Scheduler, `tests/RunStableClawPullTests.cs`, canonical `.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1` phase/freezes only.
Interfaces: existing `SerializedValues` first consumes independently encoded schema2 and must initially fail behaviorally (not compile error); produces immutable `CellSize`, optional final`int cellSize=8` constructor/builder argument, grid-awareCell/Advance.

- [x] Write rawschema2 acceptance/large-world lastID mapping test with literal bounds16384×12288/cells786432/lastRect16368,12272,16,16; execute ordinary`minimap-grid` current source1954. Expected behavioral RED unsupported schema, production unchanged.
- [x] Add schema2/CellSize, constructor/builder optional argument, grid-aware scheduler/model; preserve old serialization/fingerprint paths. Extend tests invalid sizes/schema/counts/truncation/atomic recovery, largeID/no-wrap/160radius/loadedgap/thinwall/near-cell fairness/budgets/oldmodel17 and codec negatives.
- [x] Execute current phase. Expected strict functional success or named raw resource failure; no cap/visibility relaxation or blind rerun. Record actualRED→GREEN and everyruling.

## Task2 — saved builder/HUD and independent cold

Files: same testclass; new`tests/RunMinimapGridColdRead.cs`; modify View/minimap-mask.gdshader, canonicalowner cold admission and source freezes.
Interfaces: `Build(definitions,int cellSize=8)` uses exact old structural extraction, newgrid16 schema2; actualsave witnesses `grid16.tres`, optionalprogressJSON, manifest. Coldtest consumes three exact source hashes/writerPID in sameisolatedcase, independentreader.

- [x] Saved smallsector fixture builds8/16: literal exact solids/boundaries, grid-specificrevision; ResourceSaver/reload16samebytes/cells/geometry; oldcurrent/legacycatalogSHA exact. No inline borrowed-resource Dispose.
- [x] Write realview grid16 cache/checkpoint/material switch test. Observewrongcache/uniform/checkpoint/directorybehavior before fixes; then parameterize grid math, preserve64worldbuckets/8outline/defaultshader8 and HUD dimensions/allocation contracts. No pixel claim from headlesscommands.
- [x] Warm actualoptional save/restore/revision mismatch/core/sourcebytes; independentcold reload catalog/JSON/hashread-only. Old migration tests run unchanged; no currentprofile auto-switch. Expected strictwarm+cold markers/PID/byteproof.

## Task3 — same-version regression, review and custody

- [x] One fresh review against actualv342 frozen snapshots, mandatory focus above, regrade findings; one TDD C/I fix pass, minor ledger, no second reviewer.
- [x] Final ordinary same-source grid/HUD/oldmodel/visibility/save/lifecycle plus current abyss main/explore/profile/terrain/world/reward/resident/pre/post/normal fullroute gates appropriate to global minimap change. Do not reuse prior binaries; list every failure. Actual snapshot/helper/binary/user/process audit.
- [x] Update README/assettable/backlog/playerfeedback/report with exactinfra scope/no finalpixel/fullmap claim, media one-shot informational. FullgoalACTIVE and strictresourcegateOPEN unless actuallyfixed; no delete/commit/publish.

Pre-flight: Task1 CellSize feeds Task2 cache/shader/cold identity; schema1 must not silently become2, and64bucket span is independent of CellSize. Rule explicitversions/default8/physical8outline, no generic `/8` replacement. This plan is a slice of the full goal, not a replacement completion definition.

## Execution status / unpassed gates

Checkedboxesabove recordimplementation/review/execution/documentation steps, NOTcleanwhole-plan acceptance. Actual22same-sourcecases17strictPASS5FAIL: grid285RID/terrain48RID/preFirstCombatHP5→4/oldMainBell899/oldExploreHP2vsHP1entry. Oldminimap14lifecyclewasaddedasplannedcoverageafter21checkpoint, notrerunoffailure. TargetedI1/I2actualRED→GREEN, nosecondreview; M1lategeometryatomicreuse/equal-lookingcatalogcounteradversarialcontrolsdeferred.

- [ ] Strictresource/gameplayreleasegate: resolveall5currentFAILs andhistoricalHandle/Profile-null/FATAL/fullroutegates; nouseracceptancefromsubset.
- [ ] DeferredM1exactadversarialcontrols andnormalrenderedpixels/corridorreadability/movingviewperformance/hardware/human gates, notcertifiedbyheadless.
- [ ] Fullnew16commonparentintegration(BuildGraph+SaveCandidate),13–24/G–J/population/density/Boss23→24/original11/art/actions/AV/package: fullactivegoal, notthisslicecompletion.

Finalrootaudit42190/v002 actualexit0:source1955/8145FC1625C02B0429752C1DDAC3F4FC1C9ABADDEA3B438F528F44F9BF1C603D,binary56D7EBFA4C6B127185136CE263D9AABCB865E506EBEA40DF62427D9B8E27A9E4,11074actualsnapshotfiles/132binarycopies/13helperpaths/50terminalcontracts/109protectedrows/nativenone. Verification-v001.md/review-result.md/progress.md preserveallrulingsandboundaries. No fullplan/goalcomplete, noGit/deletion/automation/publication.
