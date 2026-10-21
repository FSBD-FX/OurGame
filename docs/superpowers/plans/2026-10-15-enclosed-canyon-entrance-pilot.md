# Enclosed canyon entrance pilot implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Root is sole source/build/engine owner; agents audit read-only.

**Goal:** Turn the approved entrance direction into an actual bounded exterior-to-rock-mouth route, removing the seven-step climb and verifying the connected upper rift.

**Architecture:** Keep existing five sector offsets, stable checkpoint/fragment IDs and the continuous world. The entrance floor remains636/start160,600; a horizontal rock-mouth corridor joins a lowered upper rift, rather than raising the actor into the HUD. Geometry is native collision, separately skinned with existing replaceable prototype visuals; no final art generation.

**Tech Stack:** Existing pinned Godot4.7.1 Mono/C# net9, C# scene builders, isolated owned-process PowerShell7.

**Spec:** `docs/production/2026-10-15-enclosed-canyon-layout-proposal-v001.md`; human explicitly chose “按该方向，先做入口样段” on2026-10-15. Exact trial geometry below is a developer implementation decision, not a claim that the ten-zone layout fits or is complete.

## Global Constraints

- Full original goal stays ACTIVE; no paid generation, Git writes, uploads, automation restoration or real-userdata semantic access.
- Stage1 boundary proof must not be substituted for the full approved entrance/rift pilot. Seven-step removal, horizontal passage, connected rift, old saves, return and new visual proof remain required.
- No player/AI/HP/energy/physics/ability nerf, teleport or hidden recovery in the positive traversal. Same actual40×72 capsule and620 claw.
- Build leaf before parent through exact-output owned builders; retain previous scenes and all failed outputs. Strict native ERROR/WARNING/FATAL veto remains.
- No new checkpoint, saveversion change, loss of any9 fragment IDs, or removal of6-fragment Boss admission.

## Review Focus

1. Real walls versus painted rocks: body contact/normal, not only node metadata.
2. Horizontal opening versus old high seam: lower both entrance and rift markers, retain exact scene offsets.
3. Wider lanes permit bypass: close the alternation wall to the roof, test jump/dash/claw boundaries.
4. New roof can bury actors/fragments/respawn: synchronize their actual supported positions.
5. Narrow/shifted terrain can break old saves and Archive: retain stable checkpoint positions, independently test Continue/respawn and branch support.

### Task1: Permanent outer boundary and thick entrance foundations

**Files:** CanyonContentBuilder.cs; existing PrologueCanyonTraversalTests.cs; Stable runner; verify-claw owner. Builder owner is `.superpowers/sdd/2026-10-15-enclosed-canyon-entrance-pilot/build-pilot.ps1`.

- [x] Write saved-Main title→physicalEnter→supported spawn→physicalA contract; old map must fail named ENTRANCE_PERMANENT_WALL_RED before any production edit. Actual native184754 fails exactly there/2075ms/guards0/no other native diagnostic.
- [x] Add visible/native EntranceOuterWall world(0,0,48,720); extend StartFloor/BridgeFarBank thickness from40 to84 down to720, without moving their feet/fragment/checkpoint.
- [x] Owned entrance builder then parent builder, each allowed to change only its exact scene output. Run `verify-claw.ps1 -SourceRows1645 -Phase entrance-enclosure`; actual wall normal/floor containment/zero recovery/same actor verified185703, extended local gates later.

### Task2: Replace upward stairs with the connected rock-mouth/rift trial

**Files:** same builder/test/owner, CanyonWorldLayout.cs, CanyonEncounterCatalog.cs, PrologueCanyonBuildParts.cs, existing entrance backdrop/platform visuals, generated Entrance/Descent/parent scenes.

- [x] Add real forward/ceiling/claw/return contracts before changing route. Clean191417 horizontal RED follows real prior wall/ceiling/gap criteria, not missing-node/setup failure.
- [x] Remove seven BridgeExitStep bodies; extend far-bank floor400–1280 top636. Thick roof starts600 with underside400 (net236); actual wall/ceiling contact and roof pull proven. Retain80px erosion gap/one grounded jump. Alternation/outer-right绕行negative still separatelyOPEN.
- [x] Lower same-ID entrance seam to600. RiftUpper/Middle/Lower/Commit tops636/856/1076/1296;196 net between24px shelves, alternation wallroof400→1100. Lowerleft later1400 to admit branch; mainright2100 unchanged.
- [x] Preserve stable IDs while moving first Rift actor/activation/retention and rift fragment out of the new rock; start/checkpoint/entrance fragment remain600. RecoveryLanding local880,1260. First brake/first side catch/critical bounds/first two shaft segments synchronized; bottom3560/Basin/bottom recovery windows stay. FrozenCeiling left edge later moved80px to1400 solely for safe branch slot, remaining lower Frozen support unchanged.
- [x] Repartition Rift/Archive/abyss route/focus geometry; ArchiveShelf1420 ends1720, avoidsSupplyRoof1400–1424; ground bridge→rift edge. Two actual branchREDs193545/194019 led to continuous72px leftslot atLower/ArchiveFloor/FrozenCeiling; physical194248/194552 pass to originalFrozenLeftCatch. Not full branch reverse proof.
- [ ] Regenerate Entrance, Descent, parent/minimap revision; no loss of core save progress. Native positive/negative geometry, supported legal opening, same-player return, actual K pull and blocked/ice control, oldContinue/respawn and current early-death/fragment/branch regressions. Full Main/Explore bots must be migrated by physical contracts, not made green with positional writes.
- [x] Archive/review local hardware proof:194552 fixed60/1550frames/6viewport+6video samples. Visible structure can be shown as DRAFT; overall rectangular/empty grayboxNEEDS_WORK, not a final/playable-fullpilot acceptance. Archivev001/v002index/v003check retained. No realtime/human/continuousAV proof.

Remaining before full pilot acceptance: migrate early-death/newlayout prefixtures and normalMain/Explore routes; test currentterrain-revision Continue/core progress preservation and physical restored support; deeper reverse/capsule-containment/anti-bypass contracts; historical resources and fullvisual/performance/human gates. completion-migration195013 passed its7 exactexpectednegativeWarnings, not zero-warning gameplay. Fresh independent review found Ice final-only oracle weakness and missing external-log gate; root repairing test/owner, production unchanged. Keep Task2 incomplete.

Execution ruling: user AGENTS requests reasonable direct continuation without repetitive approval. The human explicitly selected entrance execution; root uses native inline implementation with read-only independent audits, no new Git/worktree/commit/cleanup. Written plan self-review completed: all five focus risks assigned above; exact Archive placement remains a deliberate investigate-before-code step, not a filled-in guess. Task2 is not complete from Task1 GREEN.
