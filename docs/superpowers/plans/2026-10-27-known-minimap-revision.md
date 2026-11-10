# Known minimap revision compatibility Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax.

**Goal:** Retain verified unchanged explored map areas across the actual v133→current terrain correction without changing core progress or revealing unexplored areas.

**Architecture:** One exact revision/content-attested migration at the existing optional-map codec mismatch boundary. Same-revision path/model Restore remain strict. A compact fixed delta avoids runtime historical dependencies.

**Tech Stack:** Existing Godot4.7.1/.NET9 C#, native headless canonical verification, PowerShell. No new dependencies.

**Spec:** docs/superpowers/specs/2026-10-27-known-minimap-revision-design.md

## Global Constraints

Root sole writer/compiler/native. Freeze all source/helpers/docs/binaries during owned native/audit. Work in existing user-mandated G:/knight/Knight; no Git mutations/new worktree/installs/automation/deletion/real saves/paid/foreground actions. Original eleven-item goal remains ACTIVE; v137 incomplete combat and historical resource failures remain OPEN. New task is independent, not a replacement for those requirements.

## Review Focus

1. Same-sized boundary encodings with shifted indices must not falsely reveal a different edge.
2. Copied target revision with mutated geometry must reject the optional migration.
3. All-zero and sparse inputs must never gain a bit, including changed footprint and new tail padding.
4. Legacy/invalid core and malformed/oversized optional payloads must retain previous strong-core behavior.
5. Two-process cold read must consume the original writer's actual file, validate PID and unchanged bytes, not run a second writer.

## Task1: exact known optional-map migration

**Files:** scripts/save/ExplorationMapSaveCodec.cs; tests/unit/ExplorationMapSaveTests.cs; tests/integration/ExplorationMapColdReadTests.cs; tests/RunMinimapTests.cs only if current argument contract requires; existing canonical .superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1.

**Interfaces:** Preserve public TryRead(JsonElement,MinimapTerrainCatalog,out ExplorationMapSaveData,out string), Write and model Restore. Test helpers `CurrentRevisionCatalog()`/`KnownRevisionMigration(string root)` reconstruct old catalog and compare source SHA independently. Cold reader uses writer storage and existing isolation validation.

- [x] Write tests through real LoadOrNew for sparse old witness; reconstruct old catalog and verify old byte SHA; prove all unaffected owning solids/world segments and literal mapping sentinels. Actual034422 behavioral RED, not setup/build failure.
- [x] Run canonical resident-save-compatibility SourceRows1852; preserve raw/guards. Implement only exact-pair bounded migration after observed RED;034540 GREEN.
- [x] Add malformed/unknown/forged/all-zero/full/changed-footprint/core/hash/padding controls; extend actual cold reader with known old witness, retaining original two witnesses.
- [x] Run current producer and genuinely separate cold process over its storage, guarded and physically retained;034851 writer11366ms/cold470ms PASS. Strict required receipts and selected snapshots added, no error waiver/input authority expansion.
- [x] Execute the six named frozen regressions once and retain all outcomes: parent/models/post PASS, pre/Main/Explore FAIL. One fresh scoped review C0/I0/M0, no C/I pass or re-review. No commit/cleanup. This is verification performed, NOT green whole-suite qualification.
- [x] Durable README/backlog/result/ledger and independent bounded audit after all owners end; auditorENDED UTC20:11:16.4203610. Known migration evidence accepted, original goal not completed.

Task1 full-suite qualification remains INCOMPLETE because pre/Main/Explore remain strictFAIL; no `Task1: complete` ledger claim. Bounded implementation, review and custody are verified. Resume original unresolved work without repeating completed nine attempts or narrowing original eleven-item objective.
