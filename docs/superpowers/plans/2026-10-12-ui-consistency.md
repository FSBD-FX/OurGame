# UI consistency repair plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox syntax.

**Goal:** Remove two existing contradictions without changing mechanics: localized legacy-save choices and admission-aware Boss guidance.

**Architecture:** Keep the existing menu and use its chosen language for all migration states. Feed the same BossAdmissionModel result used by the physical gate into guidance; do not duplicate quota or legacy rules.

**Tech Stack:** Godot 4.7.1 C#, .NET 9; existing saved scenes and isolated fixture helper.

**Spec:** Existing approved pure-keyboard/localization and 6-of-9 Boss gate requirements in README.md; audit finding recorded here. This is a bug repair, not approval of the pending global claw redesign.

## Global Constraints

- Preserve dirty worktree, real userdata, game physics, geometry, save/migration rules and stable fragment IDs.
- No paid generation, external publication, automation, dependency changes, staging or commits.
- Mount whole-game fixtures only after IsolatedPrologueFixture.Configure; retain proof outside the worktree and before showing it under the required media archive.
- No whole-route, human-input or frame-rate acceptance inferred from fixture tests or images.

## Review Focus

- Chinese and English, including recovered and unmappable saves, retain the correct choice/disabled state.
- Longer copy fits existing controls; sample menu stays unchanged; ordinary menu resets prior migration copy.
- Five fragments must not point through a closed gate; the sixth and legal legacy exemption obey the same original mechanism prerequisites.
- Active/chain-exposed Boss and disabled guidance keep their existing ownership.
- Tests use controlled snapshots only in isolated storage, not real saves or a claimed natural route.

### Task 1: Migration menu copy

Files: scripts/ui/PrologueMainMenu.cs; tests/integration/PrologueMigrationMenuTests.cs; tests/RunUiConsistencyTests.cs; update the obsolete English-only assertion in tests/integration/ContinuousCanyonMigrationTests.cs.

Interface: PrologueMainMenu.Present(bool hasSave, GameLanguage language); existing MigrationStatus and BasinQualitySampleMode.

- [ ] Add tests using the actual saved menu for both languages, migration/recovery statuses, disabled state, ordinary reset and font containment.
- [ ] Run isolated menu phase and observe the Chinese migration-copy failure before production edits.
- [ ] Localize concise preservation/recovery copy; keep semantics and state unchanged.
- [ ] Run menu phase green and capture/review the real rendered menu in both languages.

### Task 2: Boss admission guidance

Files: scripts/prologue/guidance/PrologueGuidanceContext.cs, PrologueGuidanceModel.cs, PrologueGuidanceController.cs; assets/localization/prologue-ui.json; tests/integration/BossAdmissionGuidanceTests.cs.

Interface: BossAdmissionModel.Evaluate(PrologueProgress) produces BossAdmissionResult; guidance consumes the authoritative result without mutating progress or gate.

- [ ] Add real mounted controller tests for five/six fragments, missing mechanisms, legal legacy quota exemption and active Boss ownership; both languages, no unknown-target warning.
- [ ] Run isolated guidance phase and observe the five-fragment closed-gate instruction failure before production edits.
- [ ] Use admission-aware localized text and no inaccessible BossActivation marker while blocked; retain allowed/active behavior and attached-claw override.
- [ ] Run both focused phases and relevant existing model tests green; review actual rendered five/six comparison.
- [ ] Correct the existing TemplePressureGateAccessPhysicsTests.Guidance.cs eligible-entry fixture to preplace six stable fragments and its completed courtyard seal, retaining its original attachment/release assertions; run this adjacent test, not its unrelated aggregate.
- [ ] If that old fixture uses retired E/LMB, replace only the stimulus with current W+K and a finally release; keep the same real target, 300-frame bound and Space-release assertions, and preserve the original failed run.

### Verification and retention

- [ ] Use a fresh bounded build and original owned process handle, hash guards for source/binary/userdata and explicit external logs/storage. Never invoke the unreviewed aggregate full suite.
- [ ] Preserve originals/presentation copies and source/version/status index; obtain independent code review and record any limits in README and feedback ledger.
