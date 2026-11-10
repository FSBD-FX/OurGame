# Stability failure observation implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root owns runner/canonical/parser/native; one test-only worker owns pure parser tests. Steps use checkbox syntax.

**Goal:** Preserve first failure before hazardous teardown and measure the final pre traversal without changing behavior or accepting failures as gameplay success.

**Architecture:** Real stable-runner sentinel uses actual catch/drain/Quit1; strict three-sink parser checks order/error identity. Distinct pre OBS logs surround existing awaits and final cleanup.

**Tech Stack:** pinnedGodot4.7.1.NET/C#, pwsh, existingcanonical.

**Spec:** docs/superpowers/specs/2026-10-29-stability-failure-observation-design.md

## Global Constraints

- G:/knight/Knight designateddirtycheckout. Preserve1857/897, current143solids/9fragments/76ordinary target andall11scope.
- No production/gameplay/assets/scene/physics/GC/cache/strongroots/Dispose/engine/save/foreground/OS/collector/automation/Git/delete changes.
- Rootsolecompiler/nativeowner; allwritersfreeze duringnative. Original55/180/300seconds andstrict3sinks.
- No fake ordinarysuccess: controlnativeexit1, exactsentinel, noSTABLE_CLAW_OK. Prior failures retained, no rerun-until-green.

## Review Focus

- Real primary exception logging precedes every await/cleanup; no duplicated original exception or swallowed cleanup failure.
- Negative control never grants acceptance to unrelated ERROR/WARNING/FATAL, timeouts, missing/duplicate/misordered markers or ordinary-success text.
- Pure fixtures exercise actual parser with literal expected effects, not source grep or mocked native success.
- OBS events preserve all original receipt text, delegate order, sample cleanup and wait/physics/input budgets.
- Current headless evidence does not claim native rootcause, fullroute success, pixels, performance or user acceptance.

### Task1: first-error boundary RED→GREEN

**Files:** tests/RunStableClawPullTests.cs; canonical; workspace failure-log-contract.ps1(root); test-failure-log-contract.ps1(testworker).

**Interfaces:** Assert-StableFailureLogBoundary([string]Stdout,[string]Stderr,[string]External,[int]ExitCode,[bool]TimedOut) throwsinvalid/returnsnooutputvalid. Phasefailure-log-boundary requiresExpectedFailure=STABLE_FAILURE_LOG_SENTINEL andordinaryheadless; nativeTestAssert.True(false,exactsentinel) usesrealcatch. ENTER literalSTABLE_FAILURE_DRAIN_ENTER original_gc=true additional_gc=0. ExistingTASK4...phase=stable-claw-failure count=1 andPROLOGUE_TEST_PHASE_EXIT_DRAIN_OK samephase retained.

- [x] Puretests first, expectedmissingcontractfile RED; implementstrictparser andallliteralnegativecontrols GREEN.
- [x] Add testphase andENTER scaffolding, keepoldprimaryloggingorder. Canonicalloads+freezesparser/testscripts andchecksall3sinks includingExternal primary<ENTER<finalizer<exit. ActualnativeexpectedorderingRED.
- [x] Move originalsingleGD.PushError tocatchfirststatement. Sameactualnativecontrol GREEN; nootherbehaviorchange.

Control qualifies negative-error retention only. RealbannerparserfalseFAIL retained andfixedviaadditionalpureRED→98GREEN. Fullprojectpre stillFAIL, noall-suite/taskcompleteclaim.

### Task2: pre observation and real failing route

**Files:** tests/RunPrologueTests.cs, tests/integration/PrologueTraversalRouteTests.cs; canonicalselectedcopies; status/docs/ledger.

**Interfaces:** distinctPROLOGUE_PRE_TRAVERSAL_OBS andPROLOGUE_TRAVERSAL_SAMPLE_OBS entry/return withengine_ms/physics/process andactualexistingsegment/sampleidentity; no ordinarysuccessclaim.

- [x] Add observation only, preserve all existing success markers and call/cleanup/waits. No timestamp appends to existing exact receipts.
- [x] Run ordinaryrecycling once toobserveanyinitialexceptionbeforecleanup; models/pre/post once. Recordalloutcomes, useOBS toboundtimeonlyifactuallyexecuted.
- [x] One fresh finalreview, C/I singleTDDfixpass/Minor defer; proportionatechangedfiles/caseguard/processchecks andnormalmediaCheck; updateREADME/backlog/report. No fulltask/goalcompleteabsentgreenqualificationandfull11proof.

OBSactualcoverage0: earlierpresecondJassertionfailed, postnotthispath. TimeattributionandnormalMainExplore/nativehistory/visual/hardware/usergatesremainOPEN. Finalordinary3PASS1FAIL,separatenegativecontractPASS,C0I0M0,nocleanuporbranchfinish.
