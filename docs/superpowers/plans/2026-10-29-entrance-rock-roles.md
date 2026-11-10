# Entrance rock roles implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root owns production/runner/canonical/native; one test-only worker owns CanyonMaterialSkinTests.cs. One fresh final review, no per-worker review duplicate.

**Goal:** Integrate distinct ground/roof/wall paint for the five actual entrance rock masses without changing gameplay.

**Architecture:** Builder explicit enum→saved CanyonPlatformVisual→same read-only command plan→_Draw. Legacy branch unchanged elsewhere.

**Tech Stack:** pinned Godot4.7.1.NET/C#, existing C# scene generator/canonical strict headless.

**Spec:** docs/superpowers/specs/2026-10-29-entrance-rock-roles-design.md; original fullproduction2.1/2.3/6/9.

## Global Constraints

- G:/knight/Knight designated dirtycheckout; preserve useredits, no newworktree/Git/delete/paid/OS/real-save semantic/engine/GC/cache/ProcDump/automation changes.
- Existing1857/897 inventory,143solid/9fragment/76ordinary target retained; no new runtimefile.
- Original skinbounds/ReadableEdge/material/catch/route/physics unchanged; no unknown scene overwrite. Generate candidate then check onlyvisual delta before actual leaf integration.
- No source/helper/docs writes during ownednative/review; root alone sequentialbuilds. Worker stops edits before RED.

## Review Focus

- Explicit roles bind only five correct entrance consumers; oldsample/nonentry/nonstone/stagingoff retainslegacy.
- Returned plans/pointsets immutable; actual_Draw consumes them, not unused metadata.
- Every polygon/finite stroke envelope fits originalrect incl narrowwall and thinroof, no fake support/gap.
- Generatedleaf retains complete old gameplay/material/ReadableEdge/metadata apart from visualrole, parent externalrefs valid.
- No pixel/normal-speed/final-art claim from headless source/data checks; failures and legacyresourceissues remainvisible.

### Task1: RED→paint/authoring→candidate and saved GREEN

**Files:** tests/unit/CanyonMaterialSkinTests.cs(worker); scripts/presentation/CanyonPlatformVisual.cs(root); tools/build/prologue/CanyonContentBuilder.cs(root); tests/RunStableClawPullTests.cs/canonical(root); generatedscenes/prologue/sectors/CanyonEntrance.tscn(root generator only).

**Interfaces:** enumCanyonRockSkinRole Legacy0/Ground1/Roof2/Wall3; exportedRockSkinRole; BuildEntranceRockPaintPlan():IReadOnlyList<CanyonRockPaintCommand>; commandPoints:IReadOnlyList<Vector2>,Tint:Color,StrokeWidth:float. Reflection test supports initialmissingrole asreallegacy0, no synthetic successfulplan.

- [x] Worker adds RunEntranceRockRoles() toexistingtest, preserving Run(bool); independent five expectedroles/sizes inbuilder+saved, literalbounds/edge/collider checks, finite/strokebounds/immutableplan andoldbranch controls. No productionbeforeRED.
- [x] Root adds ordinary exact entrance-rock-roles headlessphase with strict3sink/copies and55000ms cap. Run1857; expected named missing explicitrockrole RED.
- [x] Onlyafter actualRED implement explicitbuilderroles and read-onlypaintplan usedby_Draw. DefaultLegacy/nonentry/nonstone/stagingoff untouched.
- [x] Generate/validate entrance candidate underisolatedcase storage, compare completegameplay/schema withactualsavedleaf ignoring onlyfive newvisualroles and nondeterministiceditorIDs; preserve oldleafinbefore. Integrate viaexistingbuilder, neverhandedit TSCN.
- [ ] Focusedbuilder+packed/oldmaterialchecks thenmodels; expectedclean. If not, diagnose; no bounds/role/physics relaxation.

All paint assertions passed, but focused native remains strictFAIL (183ShapeRID/3HandleERROR); modelsPASS does not close it. Task1 notcomplete/clean.

### Task2: regression, review and durable status

**Files:** v155/README/backlog/ledger, no newscope.

**Interfaces:** actualgeneratedleaf andimmutablepaintcommands fromTask1.

- [x] Run ordinary canyon-structure-v2/keyboard/recycling-traps/pre/post once; retainallerrors, Main/Explore/hardware/usergates remainOPEN absentactualnewproof.
- [x] ONE fresh final review; C/I oneTDDfixpass, Minor defer, no rereview.
- [x] Proportionate changed-source/leaf/runtime/canonical/contract evidence custody plusnative terminal checks, normalmediaCheck and status. No repeatedall-historyaudit orimagesclaim; full11goalACTIVE.

Execution/review/status recorded; overallqualification remains4PASS/3FAIL, no task-done green-suite or branch finish. See v155 report and ledger.
