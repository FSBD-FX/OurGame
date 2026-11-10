# KNIGHT large-map minimap grid design — v343

## Intent and scope

The full24-segment/10-branch canyon→temple map needs more world area than the current6144×8192/grid8 catalog, already786432/1048576 cells. Build a versioned grid16 option for the expanded campaign without increasing memory/work caps, shrinking the actual level, revealing unseen terrain or altering old saves. This is infrastructure for the remaining13–24/G–J, not their deployment or full-goal completion.

Current project `G:/knight/Knight`, Godot4.7.1.NET/net9,1280×720. Headless only. User opted out of routine approval prompts; the design choices below are developer calls, not new player approvals. Root solewriter/native, read-only agents. Previousgoalturn v342 was PROGRESS (actual30 continuous pickups/rejoins/cold plus strict17/18 and custody), not a wait/no-progress. Full goal remainsACTIVE.

## Approaches and developer choice

1. Increase1048576 cap with unchanged grid8: rejected, increases bitsets/directory and violates current budgets.
2. Chunked/sparse grid8: retains free-space resolution but requires a different persistent addressing/streaming/render contract; possible future alternative, not a shortcut this turn.
3. Versioned grid16 free-space cells with exact physical solids and ≤8-world-unit boundary segments: chosen. Coarsens only optional minimap free-space memory, never physics, level dimensions, art, outline segmentation or reveal radius. Narrow passages may conservatively have fewer safe whole cells; exact fine boundaries remain. Normal rendered narrow-passage readability remains an explicit later gate, not headless-certified polish.

## Binding contracts

- Old default constructor/builder remain grid8. Old schema1 bytes, revision digests, current saved scenes, legacy migration attestations, grid8 model behavior/save/slots are exact. No mass rewrite or converting legacy bits as if indices matched.
- New catalog schema2 stores explicit cellSize16. Only8 and16 supported; schema1 implies8, schema2 requires16. New builder fingerprint includes new schema and grid. CellSize is immutable, all dimensions/counts checked before publication, malformed/over-capacity input rejected without freezing a partially valid resource.
- Both independent bitsets remain≤1048576 bits each. Geometry/segments remain exact and existing≤8 segment check unchanged. Grid16 permits16384×12288 at786432 cells as a test envelope, NOT a committed final-map layout/difficulty/timing choice.
- All grid-coordinate consumers use catalog.CellSize: catalogCell/model/scheduler/view cache/checkpoints/shader. Boundary directory remains64-world-unit buckets; boundary queryGrow8/free-side stroke8 remain physical geometry, not multiplied by grid ratio.
- Reveal remains living actual position only, complete current occlusion/loaded swept coverage, all corners/hull within160world radius;64 candidates/256 scheduler expansions/max2 rebuilds/stamp unchanged. Newhalo ceil160/cell+1 preserves grid8 halo21/side43, grid16 halo11/side23. No center-only visibility fallback or budget increase.
- HUD260×170/1280world width/cache256×192 and ellipse remain; grid16 changes world extent of cache, not UI size. Shader uniform default8 preserves direct existing users; set on catalog switch without per-Present Variant churn. Cache rebuild needed on catalog switch even if model revision/actor match.
- Old8 optional-map format1/defaultDTO/wire and core schema4 remain exact. ReviewI1 exposed equal-revision/equal-byte-size cross-grid ambiguity outside the builder. New16 optional memory therefore usesformat2 (implicitly16); Model and Codec both enforceformat1↔8/format2↔16 in addition toTerrainRevision/counts. Grid mismatch discards onlyoptionalmap, preservescore/sourcebytes; olderreadersrejectnewoptionalformat2ratherthanmisreadit. Existing four legacy geometry migrations remain exact. This is a developer correction of the originalformat1-only assumption, not a new player-approved core-save change.
- Verify real saved/reloaded catalog, model, view cache/material/checkpoints, optional save read/write and independent cold reader. No claim of normal rendered pixels/FPS/hardware/human acceptance from headless.

## Non-goals and remaining full goal

No gameplay/physics/HP/claw/thruster changes; no new art/enemies/mechanisms/terrain in this increment. Do not silently switch existing profile to grid16 before new geometry. Current48ShapeRID and historicalHandle/Profile-null/FATAL remain strict failures; parallel resource recon is read-only, not a speculative release fix. Full24/10, old76/new natural population/density/Boss23→24/original11/formalart/actions/AV/hardware/human/package remainOPEN.

No GUI/OS input/paid generation/automation/Git/worktree/commit/push/deletion/real-save mutation/newdump/GC/cache/engine experiment. Key media only;20GiB informational normal report.
