# Native saved-player hop shape synchronization plan (v196)

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Root implements and serializes every build/native; assistants are read-only.

**Goal:** Determine whether current native ranged queries see the actual saved Player Hurtbox immediately after its real Motor moves, before changing game mechanics or the Main driver.
**Architecture:** Extend the existing independent native-geometry suite with two clean saved-Player fixtures. Both use real keyboard Space, real Motor and native ranged Sweep; the initial root ForceUpdateTransform control failed, while forcing the Hurtbox itself corrected all8 samples. Pair pre/post observers by actual physics frame and read CPU/native transforms plus newly occupied/vacated capsule probes. Never equate this fixture with a full Main replay.
**Tech Stack:** Existing Godot/.NET/C#, isolated canonical ranged-native-geometry phase.
**Spec:** docs/production/2026-11-01-hop-ballistic-feasibility-v195.md; original eleven-item scope in docs/production/2026-10-18-project-remaining-backlog-v001.md. Installed GodotSharp.xml documents accumulated CanvasItem transforms and ForceUpdateTransform child propagation; experiment, not that documentation alone, decides current behavior.

## Global Constraints

ONLY G:/knight/Knight; original complete goal ACTIVE. Existing CS only, SourceRows1872; build180s/native geometry55s unchanged. Root native serial/source+helpers frozen through terminal. No GUI/OS input, real saves, paid generation, extra crash capture, automation, Git writes/publish or deletion. No difficulty, map, damage, jump tuning, K timing or original Main-contract edits. Fixture initial placement before tree entry only; after takeoff no pose/velocity/HP/resource writes. ForceUpdateTransform is an explicit diagnostic treatment, not read-only and not a preapproved production fix.

## Review Focus

- Exact saved Hurtbox/GuardedDamage identity, native area+shape transforms and current capsule, not a circle-only mock or BodyCollider substitute.
- Same-frame pre/post samples; observers bracket Motor but do not prove default-priority projectile tree ordering or broadphase synchronization by transform equality alone.
- Independent stable positive/negative native controls, then newly occupied/vacated points with clear margins; no tangent-only expectation or prediction used as truth.
- Global input single-owner/release and unchanged Health/one accepted jump/no extra action; callback observes and queries only, except the explicitly isolated force variant.
- Owned viewport/world/floor disposed after node drain; shared loaded Player shapes never disposed; callbacks cleared before freeing and no swallowed assertion.

## Task1 — actual native characterization and conditional repair

Files: tests/integration/RangedAttackGeometryRuntimeTests.cs; canonical .superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1 only for explicit added dependency capture. If experiment proves an actual production synchronization defect, update this plan with the exact minimal production repair and its RED evidence before editing production.

- [x] Add ActualPlayerHopNativeCalibration after the original11 geometry cases, with two isolated variants and8 real post-Motor upward samples each. Read actual Motor priority/settings and capsule. Record pre/post native area transform and native Sweep hit for a newly occupied top probe and vacated bottom probe. At rest, a center probe must find the exact Hurtbox and a far probe must find none. Use actual delta, physics/input sequence, not nominal timing only.
- [x] Preserve driver ownership and full cleanup; add driver/observer/probe dependencies to this phase's archived input scope without altering admission, rows or deadlines. Resource exit errors still OPEN; ownership code is not clean-exit certification.
- [x] Run canonical -Phase ranged-native-geometry -SourceRows1872. Actual root control failed, Area control worked and baseline mismatch was genuine RED; all failed cases retained and each changed variable ledgered.
- [ ] Strict acceptance remains OPEN: minimal production synchronization implemented from actual baselineRED and all16 native behavior samples now pass, but geometry exits with3shape ERROR and Main still takes shot13 damage. Current actions/models/pre/post/completeExplore PASS does not close these failures. Do not reapply the fix or rerun unchanged; next resource RID/root proof and any explicitly approved Main timing change.
- [x] One fresh final review C0/I0/M1, no C/I repair required; Minor deferred/no rereview. All11 new-case custody/current-source/process identities, durable report/README/backlog and manual media check completed. This is local implementation/evidence progress, not full Task4, original eleven, normal-display/hardware/human or release acceptance.

Ruling: approved continuous inline headless work overrides repetitive preference prompts; this is a narrow diagnostic, not a new player mechanic. Sole dirty project uses frozen case snapshots instead of worktree/commit/delete helpers. An honest existing-behavior characterization may pass without RED; no production code is written until an actual failing behavior supports it. Cost: no broad stability or finished-game claim from this local experiment.

## Evidence-backed production repair (added before implementation)

Caseaefd8e99: real controls valid, baseline8/8 native stale, HurtboxForce8/8 correct; native2899ms/exit1 strictbaselineRED. RootForce control afb78f0d did not work. Existing3shape exit error remains separate and retained; moving assertion outside completed async calibration did not remove it, so no resource-cause claim.

Modify scripts/player/PlayerMotor.cs only: cache optional existing sibling Hurtbox during Ready, and after actual MoveAndSlide call its ForceUpdateTransform when still live/in-tree/not queued. Preserve optional component behavior, original tree-residency lifetime, all motion/damage settings and body pose; gate motion ownership/revision/health again after the notification boundary. No test-only production API or test flag. The same no-force native fixture must then observe current targets without diagnostic intervention. Run current geometry, actions, claw models and original Main/pre/post; report every strict failure including resource errors. No complete-goal qualification from local behavior success.

Ruling: the production Motor change affects both routes, so also run one current complete Explore diagnostic under its original600s after post. Main failure does not exempt the independent route; do not renew v186's old certificate or change source between these runs. This adds evidence/cost only, not gameplay/input budget or new authority.
