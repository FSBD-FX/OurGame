# Bell Query Ownership Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans inline. Root sole mutator/build/native, agents readonly.

**Goal:** Release Bell's owned terrain-ray temporaries before returning without changing native movement, terrain visibility or committed contact.
**Architecture:** Extend existing real BellSight fixtures with synchronous ObjectCount controls, watch native RED, scope only three owned temporaries. Freeze source then run unchanged original end-to-end/resource gates once.
**Tech Stack:** Existing Godot4.7.1/.NET/C# canonical headless runner.
**Spec:** docs/superpowers/specs/2026-11-09-bell-query-ownership-design.md.

## Global Constraints

- Full eleven-item goal ACTIVE, KNIGHT G root; headless only/no paid/GUI/OS/automation/Git/worktree/delete/real saves/GCcache/engine/newdump.
- Do not alter HP/energy/timings/physics/route controls/acceptance/terrain/angles or joint ground rule. Missing/failed gates stay failed.
- Bell terrain semantics World-only, Bodies=true, Areas=false, HitFromInside=true, self RID exclude unchanged; no new caches or borrowed resource disposal.
- Original caps/strict diagnostics/protected inventory stay; no source/input edits during owned native execution. No identical-source retry to select green.

## Review Focus

- Own only query/exclude backing/result; never dispose world/directspace/scene shape/tuning or duplicate owner handles.
- Blocked and open returns both dispose; self/body/foreign Area and solid world rules remain Bell's original ones.
- Reflection calls actual private method; zero added physics/process/GC and unchanged identities/pose/HP/state prove the controlled call boundary, not gameplay completion.
- Existing real Active overlap/HP and same AttackId commitment remain verified, not replaced by ObjectCount-only tests.
- Aggregate or cleanup errors remain strict failures; no claim that this closes historical GCHandle/RID, full routes, art or human AV.

## Task1: Production terrain-query ownership

**Modify:** tests/integration/BellBackLaborerSightRuntimeTests.cs; after RED only scripts/enemies/laborer/BellBackLaborer.cs.
**Interfaces:** Existing private HasClearTerrainPathTo(Vector2) bool unchanged. New test-only AssertSightQueryOwnership(Fixture,int,bool,string) uses local reflection and Performance.ObjectCount; no public production hook.

- [x] Add four64call controls in existing openOwnedCollider/realWorldWall fixtures, literal maxRetained≤0/expected visibility, snapshots unchanged.
- [x] Run canonical `-Phase resident-heavy-default -SourceRows 1927`; actual native BELL_SIGHT_QUERY_OWNERSHIP_RED with positive retained delta1, not compile/fixture failure. Raw041157-5ccb31fa retained.
- [x] After terminal RED, apply three lexical ownership scopes preserving all original query options; run same phase once. Actual allfour max0 plus complete original sight/armor/claw receipts and strict logs PASS19769ms, 041322-7c73cf28. This local gate does not qualify full routes/resources.
- [ ] One fresh whole-increment read-only review; freeze then original11+save cold once. No unrelated melee/Bone/Ghost retry. Final same-source/production/binary/protected/helper/pin audit, one mediaCheck, README/backlog/report; leave component/goal incomplete when named gates fail.

Self-review: one cohesive owned native lifetime repair, no shared task interfaces. Existing runner already freezes both files in required inputs; no canonical contract edits needed. Continuous execution and current dirtytree preservation take precedence over generic worktree/commit/delete workflow.
