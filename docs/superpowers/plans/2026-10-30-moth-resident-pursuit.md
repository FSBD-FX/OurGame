# Moth resident pursuit implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax for tracking.

**Goal:** Stop an out-of-J-range optional resident Moth displacing the unfinished required split wave, without omitting its danger.
**Architecture:** Test-only opt-in geometry filter before nearest-resident competition; original J and selection share one finite geometry helper. Keep production controls/resolver/AI and original full-route gates intact.
**Tech Stack:** Godot.NET4.7.1, C#, existing canonical PowerShell native runner.
**Spec:** docs/superpowers/specs/2026-10-30-moth-resident-pursuit-design.md

## Global constraints

Root owns all writes/build/native processes; read-only agents. Freeze runtime/helpers during orchestration. Original600s full-route/900physics encounter/300physics transfers remain unchanged. No windows/OS input/real saves, paid generation, automation, Git mutation or deletion. Dirty worktree preserved.

## Review focus

- Required actor versus optional resident identity; never filter a wave actor.
- Inclusive J boundaries and NaN/Infinity; no silently enlarged attack window.
- Transient cooldown/energy versus spatial reach; avoid engagement switching on every action frame.
- Filtered actors still enter danger, including sources other than the selected actor.
- No-live/pending/queued actor behavior and caller scope; no fabricated completion.

## Task1: Pursuit geometry and original-map validation

**Files:** tests/integration/PrologueCanyonGeneratedMainRouteTests.cs; native selection fixture in tests/integration/RangedWeaponPlacementRuntimeTests.cs or a narrowly routed existing resident fixture; canonical only if a distinct native phase is necessary.
**Interfaces:** optional bool restrictMothResidentPursuit=false on Task10SelectEncounterActor; internal shared Task10MothStrikeGeometry(Vector2 delta,out bool vertical,out bool horizontal).

- [x] Add native saved-scene selection assertions and literal boundary cases. Add only inert API scaffolding if needed so RED is behavioral, not compilation failure.
- [x] Run focused canonical phase, expect the named out-of-reach resident selection assertion FAIL. Keep raw case.
- [x] Implement the opt-in precompetition filter, live-wave caller scope and shared original J geometry, bounded selection diagnostics. No production change.
- [x] Run same focused phase, expect new controls and existing focused suite PASS with clean strict logs.
- [x] Run original thruster-route-exploration-diagnostic, SourceRows from authoritative inventory, expect either real advancement or preserved exact frontier failure, never certify whole route on partial progress.
- [ ] One fresh review of exact delta; C/I one TDD pass, minors deferred. Audit new immutable evidence, update README/backlog/production report and media-size check. Full-suite item remains open if original qualification is not clean.

No commit or workspace deletion: user-owned dirty tree and retained evidence are not expendable. This overrides generic skill commit/cleanup steps.

当前状态：唯一审查、8案保管和README/backlog已更新；C0/I0/M1延期。最后步骤的完整项目资格仍OPEN（pre14Handle、Main死亡、Explore战后返程FAIL），不能用窄focused或模型/后段通过勾成全suite完成。原十一项与ACTIVE目标不缩减。
