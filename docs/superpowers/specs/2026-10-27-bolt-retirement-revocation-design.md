# Bolt retirement: synchronous contact revocation

Original KNIGHT eleven-item goal: predictable attacks, correct damage/lifecycle and stability. Live root G:/knight/Knight, Godot/.NET. v137 autoplayer fixes and v138 map compatibility remain; current full Main/Explore/legacy resource failures stay OPEN. This task is a production permission fix only if native RED proves it, not a balance/visual change.

## Requirement and chosen approach

Once BellBoltProjectile.Retire marks the projectile logically retired/invisible, delivery through its real Hitbox.TryConnect must refuse an as-yet-unhit real player target immediately, including within Retired callbacks and before deferred Monitoring finishes. Retire remains idempotent and emits once. Live valid projectiles must still damage once; existing target permission and resident availability/full-circle territory checks must not be lost.

Initial projectile wrapper approach passed first-Launch controls but sole review I1 found a genuine post-Launch composition gap. Native053744 reproduced HP5→4 after callback retirement. Root now permits a separate internal optional Hitbox.SourcePermission, installed only by BellBolt in Ready as !IsRetired. TryConnect checks it before TargetFilter and again after configurable callbacks, before per-target identity. This is not another public filter wrapper and cannot be bypassed by ordinary TargetFilter composition. Unconfigured hitboxes retain old rules; no physics timing change. SetResidentAuthority retains its existing available/territory replacement rules; do not claim it previously preserved arbitrary custom permission.

Alternatives rejected: forcing Monitoring=false inside physics callbacks risks engine flush errors; changing activation semantics for all Hitbox expands risk to every weapon; permanent false filter breaks Launch's existing logical reset. A null-default optional internal predicate is the bounded shared-boundary exception justified by actual I1 RED. No existing production caller reuses the same bolt; public Launch does reset IsRetired, but deferred collider restoration/pooling is not supported or newly certified here.

## Proof and boundaries

Native test uses saved player, real Hitbox/Hurtbox/GuardedDamageReceiver/Health; direct TryConnect models queued stale delivery. Motor-disabled/invulnerability0 fixture is explicit, not natural flight/OS input/human proof. First failing test must show a retired invisible bolt actually decrements HP before deferred work; then validate no event/HP/dedup side effects after fix, both synchronous and callback delivery. Controls cover live one-hit, denied custom permission, reentrant retirement, resident availability/territory and invalid launch, plus original projectile/world/native Sentry cases.

Do not read real saves, tune HP/AI/energy/angle/map, add paid assets/foreground/OS input/Git writes/automation/deletion. User delegates reasonable implementation and asks only for consequential missing choices, overriding repeated approval/worktree/commit steps. Root sole writer/native; freeze all source/helpers/docs/bin during owned native/audit. One fresh final review of this distinct production patch, not a v137 re-review; only C/I get one TDD fix pass, minors defer.
