# First-gate entry terrain v339 implementation plan

Spec: `G:/knight/Knight/docs/superpowers/specs/2026-11-10-first-gate-entry-terrain-design.md`
Execution: inline, root sole writer; readonly fresh review at end. No Git/worktree/cleanup commands; preserve dirty tree and evidence. Existing user opt-out of routine approval questions applies.

## Task1 — saved physical leaf

Interfaces: `FirstGateEntryTerrain.Build()` creates an off-tree PrologueSector with independent bounds, fixed stable reward components, replaceable skins; no old world offset. Tests consume actual bodies/packed reload/minimap, not source text.

1. Add focused unit/authoring test and minimal empty builder; integrate canonical runner/owner focused phase. Run ordinary canonical phase and observe behavioral RED.
2. Implement merged rock masses from grid-aligned corridor union, explicit ledges/catch route and7placements; share existing Platform helper through a local-bounds validated authoring seam.
3. Run canonical phase: quiet build and physical leaf checks GREEN.

## Task2 — formal Player route proof

Interfaces: saved leaf from Task1 + actual Player.tscn + existing keyboard driver. Only initial spawn/camera configured before tree entry. No candidate campaign save/flow workaround.

1. Add native closed-boundary rays and continuous main/A traversal with independent target windows; run RED for unsupported geometry if any.
2. Correct terrain, not player tuning or test acceptance, when collision/traversal fails. Preserve failed cases. Separate explicit observation/timing diagnostics from pass evidence.
3. Run focused phase, models, original affected regressions, fresh readonly whole-increment review, one fix pass for important findings with RED→GREEN, freeze results/limits in README/report/ledger. Do not mark full goal complete.

Review focus: corridor negative-space/rock union; row merging gaps; pack owner loss; unknown reward IDs; mutable geometry; player teleports or injected velocities; recovery hidden by fixture; new leaf confused with old campaign publication; minimap/skin vs native collision mismatch; singleton/cached native lifetime. No visual or final-art acceptance from headless.
