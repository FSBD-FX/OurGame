# Representable projectile range termination plan (v200)

> **For agentic workers:** REQUIRED SUB-SKILL superpowers:executing-plans. Root implements and serializes all build/native; agents read-only. Continue already approved headless execution, no new GUI/OS/paid/save/crash/automation/Git/delete authority.

**Goal:** A real fired projectile must finish after spending its configured travel budget instead of remaining an active hazard at a float32 world endpoint; preserve actual last-segment collision/damage/area and missile explosion semantics.
**Architecture:** First reproduce the observed559.99994 Burst endpoint via real native physics at large and small coordinates. If confirmed, derive a per-tick logical range exhaustion flag from double speed×age-clamped delta before float endpoint quantization; consume it only after original native sweep/contact/ledger handling. Keep actual TravelledDistance, not a fabricated maximum.
**Tech Stack:** Godot/.NET/C#, canonical ranged-projectile55s, build180s, SourceRows1872, Main/Explore600s, pre180s/post300s.
**Spec:** docs/production/2026-11-01-warning-timed-hop-v199.md (observed production candidate), original eleven-item backlog and existing projectile maximum-travel/contact/owner/age contracts in production/tests. Candidate mechanism provisional until native RED.

## Global Constraints

Only G:/knight/Knight. Preserve dirty baseline, Main2E407/PlacementD952 unchanged unless separately ruled actual passive diagnostic is necessary. Production baselineProjectile5C67C282, testB0582007; MotorCB65/GeometryA359/canonical7A79 unchanged. No changes to speed/travel/damage/radius, enemy counts or difficulty, resolver priorities, extra jump/HP/pose writes, disabled actors in real routes, test clock/budgets or warning suppression. All source/helpers frozen until every native terminal. All failures retained. No window/input to OS, true player saves, paid generation, extra crash collection, automation, Git mutation or deletion.

## Review Focus

- Real physics termination must not happen before travel budget, or require giant delta/manual private state to pass.
- Final-tick Hurtbox delivery and preceding/coincident World collision must occur before range termination, exactly once.
- Missile must retain age clamp, guidance budget, safe endpoint explosion exactly once, area/LOS and callback revocation semantics.
- Tiny legitimate steps/paused/invalid delta are not globally interpreted as stuck and retired; no arbitrary epsilon increase or fake TravelledDistance.
- Actual native controls vs IEEE arithmetic inference, production scope and Main/Explore/visual acceptance must remain distinct; no aggregate failure hidden.

## Task1 — reproduce and repair logical range completion

Files: scripts/enemies/ranged/RangedWeaponProjectile.cs; tests/integration/RangedWeaponProjectileRuntimeTests.cs; docs. Existing Fixture owns nodes/shapes/settings, new observer borrowed from test infrastructure, no new runner/phase.
Interfaces: existing public Launch, _PhysicsProcess, IsRetired/TravelledDistance/Telegraph, Retired/ExplosionResolved. No new production API or resource lifetime.

- [x] Add real-engine finite range tests at origins(0,0)/(4533,3459), fixed direction(.9276568,-.37343386), own440/560/r5 settings. Cover Burst, Scatter and Missile if repair generalizes. Wait bounded100 await pairs with post-node scalar observation; retain before-cap alive and terminal exact-once results. Expect namedRANGE_NATIVE_TERMINAL_RED, not compilation error. All cases run before assertion so negative outcome is not censored.
- [x] If reproduced, add post-sweep range-exhausted decision from double requestedDistance>=remaining, preserving order. Do not terminate just because motion0 or write Travelled560. Add last-spend-tick target/world/coincident/missile blast controls, independent literal expected HP/endpoint. Expect original+new projectile suite GREEN.
- [x] One changed current Main to inspect real knock-on results with original driver/gates. Main02f9 still original900FAIL; all8specifiedaggregatesstrictPASS, ExploreBoss/supportexit/D60/recovery0. Existingraw+ownershipcontractsolveslastliveRustidentity; no new passive CSchange required thisround, HP/geometrynext. No acceptance relaxation or unchangedfailure rerun.
- [x] One fresh whole-change review C0/I0/M2; no C/I fix or re-review. Final12case/currentcustody, README/backlog/production report andmanualmediacheck complete. Full goalACTIVE, not entireMain/eleven complete.

Ruling: prioritize a real production hazard-lifetime defect over another automatic combat tactic; v199 originalMainFAIL remains. Cost production fix needs stronger contact/role regression, and does not automatically resolve route targeting or certify formal visuals.
Ruling: existing headless authority and codex branch plus frozen archives replace new worktree/commits and repeated approval; cost no Git release or normal-display acceptance. User authority forbids creating/deleting artifacts via Git helpers.
