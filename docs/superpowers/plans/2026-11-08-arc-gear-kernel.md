# Arc Gear Kernel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. Root owns all edits/build/native; helpers readonly.

**Goal:** A real saved/reloaded suspended arc gear hits the visited apex and cannot damage the empty chord, with original damage/lifecycle and all old consumers preserved.
**Architecture:** Independent model, exactarcgeometry, FixedGear-derived native actor; one new canonical phase. No legacyline/default/map changes.
**Tech Stack:** Existing Godot4.7.1 mono/.NET C#/ordinary canonical headless.
**Spec:** docs/superpowers/specs/2026-11-08-arc-gear-kernel-design.md

## Global Constraints

- KNIGHT G:/knight/Knight only, originalfull11goalACTIVE, root solemutator/native; noGUI/OS/paid/newart/Git/worktree/delete/trueusersave/automation/GC/cache/engine/dump.
- Baselinev3291916inputsBB73E836A39E62039907DDC4486BCF88A1307E4192A0D2C9AB466659108C2E52/sixB2F2D3FF9A290262136D9A3B1660DB932289B203A5F208E622717392C0695D45, currentnoownednative.
- Fullperiod endpoints coincidebutvisitedarcdoesnot; queryAABBbroadphaseonly/exactnarrow/noinflateddamage; playercapsule20/72.
- Newphase thruster-arc-gear / --phase=arc-gear, ordinary55s cap. Productionfiles addedonlyafteractualRED; newinputcounts1917→1918→1921 duringTDD, finalexactcountmustreenumerate.

## Review Focus

- Supporting-line crossing/endpoint partitions/anglewrap and rectangle-interior cases must not be missed by exactgeometry.
- Complete native owner/global-index/transform reconciliation; emptyfirstshape cannot dedupawaylatertruehit, unsupported/incoherent geometry mustnot authorizepartialdamage.
- Fullcycle/reversal andhuge/invaliddelta mustnotlosevisitedarc, reset/reentry mustnotteleportdamage orrepeatattackID.
- SharedFixeddamage callbacks can revoke actor/tree/Player midbatch; inheritedguardsmustremain effective; no borrowedresource disposal.
- Inventory/fog/defaultserialization mustnot interpretarc asstatic/line or showfuturepath; independentkernelnotformalmap/normalAV/relativeCCDproof.

## Task1: Exact arc native kernel

**Files create:** tests/integration/CanyonMechanismRuntimeTests.ArcGear.cs, tests/unit/ArcGearMotionModelTests.cs; scripts/mechanisms/ArcGearMotionModel.cs, CircularArcSweep.cs, CanyonArcGear.cs.
**Files modify:** tests/RunThrusterCoreTests.cs; canonical .superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1; scripts/prologue/residents/ResidentEnvironmentInventory.cs. No oldline/base/scene change unless actualcausalproofrequiresledgeredminimalfix.
**Interfaces:** ArcGearMotionModel(PivotOffset,SwingRadians,PeriodSeconds).Step(delta)/Reset()/Current (offset/velocity/visitedfractionminmax/phase). CircularArcSweep consumesworldpivot/initialradiusvector/signedvisitedangles andactualtargetworldgeometry. CanyonArcGear exports PivotOffset/SwingRadians/PeriodSeconds andexposes CurrentMotion/HasValidMotionGeometry/MotionWorldBounds; usesoriginalFixedprotectedProcessGearContacts. Native fixture usesreal savedPlayer/SpringboardFixture andactualreloadedtrap.

- [x] Addtwo savednativegeometrycases+reflectionruntimegate, runnerphase/canonicalrequiredtestsource/uniquemarker. Run -Phase thruster-arc-gear -SourceRows1917: expect missingruntimeRED, no productioneditfirst.
- [x] AddminimalFixed-derivedArcstubwithonlyexports, sameactualtest withSourceRows1918: expect ARC_GEAR_APEX_RED (realHP5not4), retainrawnegativechordcontrol too. No actualarc implementationuntilthisproof.
- [x] Addliteralmodel/arcgeometrytests beforemathcode; makephase model-first, runmissingmodelRED1919. Implementcomplete threeprod files/strictnative narrowphase/Inventorybranch afterRED; finalSourceRows1921. Preserveallliteral/nativepositive-negative/lifecycle/fog/multiShape assertions; runwholearcphase untilrealGREEN, no thresholdgaming.
- [x] Freezefinalinputs/sixbinary; original moving/recycling/regressions and11ordinaryphases+cold eachonce, no sameversionretry/budgetflag. OnefreshwholeincrementreviewC/IoneTDDfixpassonly/Minordefer, independentcurrent/copy/useraudit, mediaCheckonce andREADME/backlog/report/ledgerupdated. OriginalT03formalplacement/full11notcomplete.

Task1 kernel terminal v330:15specifiedphase strictPASS+independentcold, source1921/1B8A, six51F4,3835pairs0diff/90actualcopies/current109userstable; onefreshreviewC0I0M1deferred. SeeproductionreportforallrawIDs/caps/warnings. This completion is the bounded independent kernel only, not original T03/full11 or finalAV acceptance. Next savedplacement plan is separate/unimplemented.

Self-review: one task/no sharedtaskinterfaces. Rootexactnative/modelinterfaceowner isone; mathadvisorproofnotengineevidence. TDDsourceguardsprogressivelyincludeonlyexistingshippedfiles, finalallnewfilesmandatory; no bypassflag/successwithoutfullexpectedreceipts. Native-firstmissingclassthenfunctionalgeometryREDmustbothretained. NoGitmutatingtaskhelpers/cleanup; manual ledger pluscanonical full snapshots.
