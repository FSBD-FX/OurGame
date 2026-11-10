# First Gate Allocation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root implements; agents readonly. Steps use checkbox syntax.

**Goal:** Supply validated24/10 allocation input for the future continuous map builder without publishing untested terrain.

**Architecture:** Immutable typed catalog references old encounter/resident identities and separate new spawn/reward/feature records. Pure validation rejects malformed allocations; density comparison requires finite measured lengths. No actor instantiation or scene/save changes.

**Tech Stack:** Existing Godot4.7.1/.NET9/C#, canonical isolated headless harness.

**Spec:** `G:/knight/Knight/docs/superpowers/specs/2026-11-10-first-gate-campaign-design.md`.

## Global Constraints

- Only KNIGHT G root; Boss23→24; J23→23 beforeBoss; level2 excluded.
- Preserve76 old slots/24subtypes, nine fragments/min6, all historical failures.
- Initial100slots/10chests44piles are budgets, not runtime completion/density evidence.
- Headless only; no paid/GUI/input/Git/worktree/delete/real-save/GC/cache/engine experiments.

## Review Focus

1. Counts correct but branch bypassBoss: reject mutated J(Task1).
2. Duplicate legacy hides missing identity: exact-set validation(Task1).
3. Aggregate rewards hide empty branch/cross-route chest: validate actual matching ID(Task1).
4. NaN/infinite/nonpositive length or lower density must fail(Task1).
5. Misclassified damaging feature/new natural replacing old type must fail(Task1).

## Task1: Validated allocation catalog

**Files:** Create `tools/build/prologue/FirstGateCampaignPlan.cs`, `tests/unit/FirstGateCampaignPlanTests.cs`; modify models branch of `tests/RunStableClawPullTests.cs`. Archiveindex/README/feedback/ledger document same increment.

**Interfaces:** `FirstGateCampaignCatalog.Candidate: FirstGateCampaignPlan`, `LegacyEnemyIds: IReadOnlyList<string>`, `Validate(FirstGateCampaignPlan): IReadOnlyList<string>`, `ValidateDensity(int,double,int,double): IReadOnlyList<string>`.

- [x] Tests first: literal24/10/76/24/54 counts, exact old identities, ordered main, Boss23→24/J23→23, every branchchest, newnatural6/8/6/4 frontonly, feature semantics.
- [x] Stub bodies then canonical `verify-claw-knight-v001.ps1 -Phase models -SourceRows <actual>`; expected functionalRED `first-gate candidate has24 main segments`, not compilation/fixture error. Preservecase.
- [x] Implement immutable allocation and validator; controls: missing/duplicate oldIDs, J→24, reward mismatch/emptybranch, naturalatM19, invalid featureclass, badquantity, null/incompleteplan and invalid density inputs.
- [ ] Same canonical models after sourcechange: require catalogmarker/all old controls and pristine diagnostics. Run available whole unit command; original11 broader regression qualification remains pending, never inferred from models.
- [ ] One fresh readonly wholeincrement review, fix Important/Critical via newRED→GREEN once, no rereview. Record source/binary/scene preservation and media one-shotCheck. No Git commit. OnlyTask1 may complete; full goalACTIVE.

## Follow-on boundary

Reward persistence, runtime geometry and natural actors/hazards need independent follow-on designs/native tests. Continue without routine approval prompts, only missing material choice/authority can stop. Originaleleven/art/actions/normalAV/resource root issues stayOPEN.
