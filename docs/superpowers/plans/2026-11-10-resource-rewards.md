# Resource Rewards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL superpowers:executing-plans. Root solewriter/native, agents readonly.

**Goal:** Make54 planned rewards actually claimable throughQ and durably recorded, independent of the still-unbuilt24/10geometry.

**Architecture:** Runtime frozen reward catalog and ID ledger are the single material authority. Existing flow does detached atomic-save-first commits; a streamed opt-in cache uses the existing Q candidate controller. Native owner executes an independent cold reader.

**Tech Stack:** Existing Godot4.7.1/.NET9/C#.

**Spec:** `G:/knight/Knight/docs/superpowers/specs/2026-11-10-resource-rewards-design.md`.

## Global Constraints

Only G KNIGHT; schema4/additive optional ledger; Q unchanged; 9fragments/min6 unchanged. No currentmap54deployment/artacceptance. No GUI/OSinput/paid/automation/Git/worktree/delete/realsave/newdump/GCcacheengine experiments. Existingfailures retained.

## Review Focus

1. Generic save erases freshly claimed ledger: test PopulateProgressSnapshot via realflow Task2.
2. Oldv4 omission must not become Recovered; oldschema forged ledger must not grant: Task1 realStore tests.
3. Save failure/heldQ/repeatedphysicalID must not consume or duplicate: Task2 native tests.
4. Success callback New/reentry/unload cannot publish stale state: Task2 native tests.
5. Registry amounts must not depend on mutable planning Candidate; all54 literal totals and cold read: Tasks1/2.

## Task1: Fixed registry and persistent ledger

Create `scripts/prologue/collections/ResourceRewardCatalog.cs`, `tests/unit/ResourceRewardTests.cs`. Modify `PrologueSaveData.cs`, `PrologueSaveStore.cs`, `PrologueProgress.cs`, `tools/build/prologue/FirstGateCampaignPlan.cs`, models branch `tests/RunStableClawPullTests.cs`.

Produces `ResourceRewardCatalog.All/TryGet/Total(ids,kind)`, Progress.ClaimedRewardIds/TryClaimResourceReward/ResourceTotal, SaveData.ClaimedRewardIds.

- [x] Tests first + stub, canonicalmodels expect54vs0RED.
- [x] Implement real registry/validation/copy/migration; oldmetadataunchanged. Builder uses registry, no backwards Build dependency.
- [x] CanonicalmodelsGREEN incl all54 totals9/33/44/12/2, duplicate idempotency, malformed input, invalidRestore preservinglive, oldv4 omission+schema2/3clear; no storage outside admitted fixture.

## Task2: Actual Q cache, durable transaction, cold reader

Create `scripts/prologue/collections/PrologueResourceCache.cs`, `scripts/prologue/PrologueGameFlowController.ResourceRewards.cs`, `tests/integration/ResourceRewardRuntimeTests.cs`, `tests/RunResourceRewardColdRead.cs`. Modify level binding, flow genericcopy, bilingual strings JSON, runnerphase and canonicalwrapperphase/coldguard.

Consumes Task1 registry/ledger. Produces internal flow.TryClaimResourceReward(cache,actor), cache.BindRuntime/CanInteract/TryInteract and successsignal afterdurable/livecommit; source-bound warm witness and read-only cold proof. No snapshotrecord change needed, genericcopy reads liveProgress directly.

- [x] Write native tests + stub transaction, new canonicalphase. ExpectedRED actualfreshQ stillunclaimed, notcompileerror or missingfixture.
- [x] Implement cache/genericcopy/flow; all54 realQ, fullhealth/noequipment/noquota mutation; failure path actualinvalid disk destination, freshretry; death/persist/Continue/New, sameID, reentry.
- [x] GREEN stagepack/reload actualcacheleaf, coldwriter3witnesses; wrapper distinctprocessreader verifies allclaims/literal totals/byteSHAs/read-only andoldv4source.
- [x] One fresh readonly wholeincrementreview; Important/Critical oneTDDfixpass, Minor record. Run originalaffected save/Q/models and ordinarypre/post; nooldfailurewaivers orblindreruns. Currentfullqualification needs actual whole11andnewmap, notcomponentGREEN.
- [x] Record actualsource/binary/scene identity, README, mediaone-shotCheck. No commit/delete; keepgoalACTIVE unless fullobjectiveactuallymet.

本增量执行步骤已完成，不代表整版通过：当前原11为9PASS2FAIL，Explorer/Main既有失败、历史资源FATAL及正式美术/新地图/全目标仍OPEN；不得签成品或merge-ready。核验见本SDD verification-v001.md。
