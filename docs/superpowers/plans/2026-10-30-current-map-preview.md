# Current map preview implementation plan

> **For agentic workers:** Use superpowers:executing-plans; root implements inline with one fresh final reviewer, read-only source agents only.

**Goal:** Deliver R09's current annotated side-view demo from real saved/configured state, not another invented layout.
**Architecture:** One synchronous Godot snapshot exporter; one isolated native artifact test/phase; one offline PowerShell SVG/PNG/HTML renderer. No gameplay/scene edits or resource holds across await.
**Tech Stack:** Existing Godot.NET/C#, System.Text.Json, Windows PowerShell/System.Drawing.
**Spec:** docs/superpowers/specs/2026-10-30-current-map-preview-design.md

## Global constraints

G:/knight/Knight remains the designated dirty checkout; no commits/worktree/cleanup. Full eleven-item goal ACTIVE/NOTSEEN. No game window/OS input/paid generation/new collector/engine-cache-GC changes/real-save mutation/automation/publication/deletion. Generated map is debug documentation, never runtime art/collision.

## Review focus

- Resident allocation mistaken for authored layout: test literal lost-echo home/patrol and50 identity count.
- Alternate encounter spawn double-counted: exactly26 slots plus50 residents; alternate stays on its slot.
- World offset/affine errors: actual143 catalog comparison and independent Entrance wall/Basin shelf/Boss literals.
- Static anchors mistaken for dynamic danger or runtime births: schema flags, visible legend, no fixed ECHO point.
- Stale/unsafe outputs: fresh isolated storage, source digests, renderer validation, refuse existing output directory and preserve archival copies.

### Task 1: Isolated native export and contracts

Files: new tools/export/CurrentPrologueMapSnapshot.cs; new tests/integration/CurrentPrologueMapExportTests.cs; tests/RunStableClawPullTests.cs; canonical .superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1.
Consumes saved PrologueLevel/Definitions, MinimapTerrainCatalogBuilder.Build/Checkpoints, CanyonEncounterCatalog.Normal, ResidentAuthoredLayout.Load, QuestFragmentCatalog and saved nodes. Produces schema knight.current-layout.v1 JSON at isolated storage/current-prologue-map.json.

- [ ] Add resident-map-preview admission and native test: missing file must fail with current map snapshot absent (not compiler/setup failure).
- [ ] Execute canonical resident-map-preview with current actual source-row count; retain RED.
- [ ] Implement CurrentPrologueMapSnapshot.BuildJson(PrologueLevelController level): string, synchronous scalar projection and source SHA; call it before the same file/content assertions. Own/free only instantiated roots and newly built catalog.
- [ ] Execute same phase GREEN; retain all warnings/errors as strict failures. No exemption for resource leaks/FATAL.

### Task 2: Source-bound review map

File: new tools/render-current-prologue-map.ps1. Consumes Task1 JSON; produces one3200×1800 PNG plusSVG and standaloneHTML/full record tables from same data, with archived original/presentation copies and source/status/SHA index.

- [ ] Write validator/self-controls first and exercise invalid schema/count/coordinate/duplicate-ID inputs against the real renderer validation; add escaping/output-overwrite checks.
- [ ] Render actual native output only; verify SVG143 solids/76 targets/9 fragments, PNG3200×1800, source binding and output identities. Inspect the archived PNG before showing it.

### Task 3: Whole-slice verification and record

- [ ] One fresh read-only final review; Critical/Important get one RED→GREEN fix pass. Defer Minor explicitly.
- [ ] Restore ordinary builds and run proportionate resident-parent/models plus original pre/post project regressions; no replay of already-ended handles or expanded caps. All failures are reported, never greenwashed.
- [ ] Verify current source/binaries/inputs and media one-shot20GiB check; update README/backlog/report and ledger. Deliver current map as configuration evidence, not whole-game completion.

## Executed record

Task1 missing-file RED→actual export GREEN→payload-binding real consumer RED→nativeSHA-bound GREEN. One intermediate CS1674 build failure retained, noengine. Task2 validator7 RED→GREEN, actual SVG/PNG/archive/copy checks passed; one fresh review C0/I1/M3, three actual review misses RED→all7controls GREEN in one PS-only fix pass. Final r003 preserves originals/copies/index; old versions and labelled control fixtures retained.

Ordinary final native export/parent/models/pre/post all passed, post original75＋owner8/4scopedWARNING. All9attempts6PASS3FAIL include initial originalExploreGhostdeath, missing-artifact RED and buildFAIL. Source1859/899, no gameplay/scene changes. Original full routes/resources/formalAV/hardware/human and full11goal remainOPEN/ACTIVE/NOTSEEN. Source-readrace and overview-overlap/browserpixel limitations explicitly documented; no claim of final art or entire game delivery. Full evidence/custody in docs/production/2026-10-30-current-map-preview-v162.md; no commits/workspace cleanup.
