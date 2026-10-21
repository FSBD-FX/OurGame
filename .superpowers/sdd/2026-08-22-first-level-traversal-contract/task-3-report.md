# Task 3 report — honest first-level traversal

## Scope and result

Task 3 now drives all ten typed traversal contracts through the production stack (`InputMap -> PlayerInputReader -> PlayerMotor/CharacterBody2D -> ThreadAbility -> packed room collision`). Six `SingleJump` contracts use fresh 20/50/80 samples and four `Assisted` contracts use a fresh 50 sample. Every mandatory sample uses zero Dash. Upper Pendulum also has a stateful four-stage smoke route with one mounted room/player, no mid-route reposition, and no direct exit touch.

The first-use Upper prompt resolves the exact packed `Geometry/AnchorLow`, uses the existing `upper_anchor_low` tutorial ID, and completes only after a real exact-target Apply followed by a real supported landing inside `entry_to_lower_swing`. Guidance-off suppresses presentation without suppressing physical completion.

## RED evidence and classification

- `traversal-task3-compile-red.log`: expected initial compile RED, `PrologueTraversalRouteTests` missing (0 warnings, 1 error).
- `traversal-task3-runtime-red.log`: first packed route RED at Awakening `spawn_to_jump_step`; real single jump missed the authored landing.
- `traversal-task3-guidance-compile-red.log`: expected guidance interface RED (seven missing enum/context members).
- `traversal-task3-guidance-runtime-red.log`: controller completion RED preserved before production observation was implemented.
- Later route RED logs retain the named Broken Tooth death/fall and Upper Apply/landing failures. The apparent Upper `OutOfRange` result was honestly reclassified as a fixture input-release bug; holding vertical aim through the K release produces exact Apply.
- Guidance GREEN exposed two production event/lifecycle facts: Apply publishes an exact target on entering Apply and null target on continuation frames, and the Guidance child `_Ready` precedes its Level parent populating `Player`. The controller now binds idempotently when the production player exists and treats only non-null Apply publications as target decisions.

## Authorized geometry reopening

All room changes followed named real-physics REDs and controller rulings. No movement/thread tuning changed.

| Room | Authorized final semantic change |
|---|---|
| Awakening Courtyard | `AttackLedge=(255,120)`; landing `Rect2(145,57,96,48)` |
| Broken Tooth Corridor | `AnchorLanding=(650,250)`; landing `Rect2(470,182,160,48)` |
| Upper Pendulum | `Lower=(150,210)`, `Mid=(400,170)`, `High=(620,120)`, `Mechanism=(970,120)`, `AnchorHigh=(360,-50)`, `Rebound=(580,-60)`; aligned contract zones; `from_mechanism=(970,68)`, `ToMechanism=(1210,60)`, `Air=(900,-200)` |

Upper preserves EntryFloor, authored entry LaunchZone, AnchorLow `(170,-90)`, HiddenShelf/HiddenAlcove, stable paths/sizes/IDs/kinds, camera/killzone, encounter identity, and all tuning. Stable standing Y ascends `198 -> 160 -> 120 -> 70 -> 68`, finishing 130 px above Entry. Real post-jump Preview distances are `337.94`, `229.26`, and `195.45` px. Assisted landing zone/support X margins are `24.83/54.83`, `53.32/78.32`, and `75.92/100.92` px. Final 20/50/80 jump margins are `13.92`, `42.72`, and `36.25` px. The continuous route walks 9.2 px from the first landing into the next launch; later assisted landings are already inside their next launch zones.

Generated leaves were regenerated one at a time for physics proof. Commit staging uses semantic-only index patches: resource IDs/node UIDs remain unstaged, and unrelated dirty generated scenes remain outside Task 3.

## Final integration markers

`traversal-task3-full-r15-green.log` exits 0 with exactly one `PROLOGUE_TESTS_OK`, zero `ERROR:` lines, and exactly these ten markers:

```text
PROLOGUE_TRAVERSAL_SEGMENT_OK room=AwakeningCourtyard segment=spawn_to_jump_step abilities=Jump sample=20,50,80
PROLOGUE_TRAVERSAL_SEGMENT_OK room=AwakeningCourtyard segment=jump_step_to_attack_ledge abilities=Jump sample=20,50,80
PROLOGUE_TRAVERSAL_SEGMENT_OK room=BrokenToothCorridor segment=entry_to_anchor_landing abilities=Jump, Thread sample=50
PROLOGUE_TRAVERSAL_SEGMENT_OK room=UpperPendulum segment=entry_to_lower_swing abilities=Jump, Thread sample=50
PROLOGUE_TRAVERSAL_SEGMENT_OK room=UpperPendulum segment=lower_to_mid_swing abilities=Jump, Thread sample=50
PROLOGUE_TRAVERSAL_SEGMENT_OK room=UpperPendulum segment=mid_to_high_swing abilities=Jump, Thread sample=50
PROLOGUE_TRAVERSAL_SEGMENT_OK room=UpperPendulum segment=high_to_mechanism_landing abilities=Jump sample=20,50,80
PROLOGUE_TRAVERSAL_SEGMENT_OK room=LowerBuriedMachinery segment=entry_to_arena abilities=Jump sample=20,50,80
PROLOGUE_TRAVERSAL_SEGMENT_OK room=BellChainMechanism segment=arrival_to_chain_floor abilities=Jump sample=20,50,80
PROLOGUE_TRAVERSAL_SEGMENT_OK room=FallenBellkeeperArena segment=retry_to_arena_floor abilities=Jump sample=20,50,80
```

## Continuous smoke

`traversal-task3-smoke-r15-green.log` exits 0 with zero `ERROR:` lines, `PROLOGUE_CRITICAL_PATH_OK`, and exactly four ordered markers:

```text
PROLOGUE_TRAVERSAL_SMOKE_SEGMENT_OK room=UpperPendulum segment=entry_to_lower_swing abilities=Jump, Thread
PROLOGUE_TRAVERSAL_SMOKE_SEGMENT_OK room=UpperPendulum segment=lower_to_mid_swing abilities=Jump, Thread
PROLOGUE_TRAVERSAL_SMOKE_SEGMENT_OK room=UpperPendulum segment=mid_to_high_swing abilities=Jump, Thread
PROLOGUE_TRAVERSAL_SMOKE_SEGMENT_OK room=UpperPendulum segment=high_to_mechanism_landing abilities=Jump
```

The smoke starts from the real Upper entrance, walks into each adjacent source zone, performs the same production-input actions, preserves the light-enemy pull, crosses the preserved HiddenShelf with real jump/run input, and triggers `ToMechanism` through collision. A retained source guard rejects post-entry Fixture/position assignments and direct `TouchExit` use.

## Mutations and cleanup

- K removal: `KNIGHT_TRAVERSAL_SUPPRESS_K_SEGMENT=entry_to_lower_swing` exits 1 at the named segment with `castCount=0`, `applyCount=0`; environment variable removed.
- Zero width: `KNIGHT_TRAVERSAL_ZERO_WIDTH_SEGMENT=jump_step_to_attack_ledge` exits 1 before physics with `LandingZone must have finite positive dimensions and be at least 48 px wide`; environment variable removed.
- No teleport: temporarily reintroducing a post-entry `Fixture(...)` exits 1 at the retained no-reposition/direct-touch assertion; source restored.
- Every driver/failure path releases all gameplay actions, unpauses, disconnects events, and tears down fresh fixtures. The direct final smoke reports `PROLOGUE_AUDIO_DRAIN_OK`.

## Gates

- Build: 0 warnings, 0 errors.
- Full runner: exit 0, 10/10 segment markers, one `PROLOGUE_TESTS_OK`, zero runtime `ERROR:` lines.
- Direct critical path: exit 0, 4/4 ordered Upper markers, `PROLOGUE_CRITICAL_PATH_OK`, zero runtime `ERROR:` lines.
- `tools/verify-prologue.ps1`: exit 0 with `PROLOGUE_EVIDENCE_CHECKS_OK`, `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`.

## Ruling 15 — deterministic evidence normalization

The initial final verify failed before tests because rebuilding unrelated `LowerBuriedMachinery.tscn` reordered equivalent `ext_resource` declarations and regenerated their local IDs. The old verifier assigned canonical resource tokens by declaration ordinal, so references that were semantically unchanged appeared rewired. Lower's only pre-existing worktree differences were regenerated `unique_id` values; its builder was clean. Task 3 triggered the false positive only because the final verifier intentionally regenerates every prologue scene, including Lower.

TDD evidence:

- `traversal-task3-evidence-normalizer-red.log`: the new reordered-declarations/regenerated-IDs/equivalent-references case failed under the ordinal normalizer.
- `traversal-task3-evidence-normalizer-green.log`: `EVIDENCE_CHECKS_OK` after stable `ext_resource` identity normalization by `(type,path)` and order-insensitive canonical declaration comparison.
- `traversal-task3-prologue-evidence-green.log`: `PROLOGUE_EVIDENCE_CHECKS_OK`.
- Negative cases still reject changed external-resource path, type, or reference wiring; changed `sub_resource` content; and ordinary scene semantic changes. `sub_resource` declarations remain content-sensitive.

Neither `LowerBuriedMachinery.tscn` nor its builder was edited or staged. The final verifier now accepts its serialization-only ext-resource reorder while retaining semantic sensitivity, and it restores all regenerated worktree bytes after the audit.

## Commit and audit

Commit message: `test: prove honest first-level traversal` (the containing commit hash is reported in the handoff). Task 3 authorized paths and the two Ruling-15 gate files are staged separately. Unrelated imports, assets, presentation, parent scene, Lower/BellChain scenes, capture, and pre-existing generated UID churn are excluded. The three authorized room leaves are staged as semantic-only index patches with no resource-ID or node-UID churn.

## Fix Round 1

Independent review findings were closed without gameplay, guidance-production, builder, P0/P1, Lower, movement, or thread changes.

### Evidence normalizer

- `traversal-task3-fix1-evidence-red.log`: the old global quoted-ID replacement falsely accepted an ordinary metadata change from `"1_old"` to `"88_new"` because both values happened to be ext-resource IDs.
- The normalizer now parses and validates declarations first, rejects duplicate ext IDs, duplicate `(type,path)` identities, duplicate sub-resource IDs, and undeclared references, then uses one targeted regex callback. It rewrites only ext/sub declaration `id` fields and `ExtResource(...)`/`SubResource(...)` references. Ordinary string values are never rewritten, so neither an old/new ID literal nor a generated-token literal can collide with normalization.
- Positive reordered ext declarations/regenerated IDs remain accepted. Negative path, type, wiring, sub-resource content, ordinary scene semantic, old/new-ID string, generated-token string, duplicate ID, and duplicate identity fixtures remain rejected.
- `traversal-task3-fix1-evidence-green.log`: `EVIDENCE_CHECKS_OK`.
- `traversal-task3-fix1-prologue-evidence-green.log`: `PROLOGUE_EVIDENCE_CHECKS_OK`.

### Continuous-helper anti-cheat proof

The guard still validates the bounded smoke call block and now also reads the actual route-test source, locates each helper by a line-anchored declaration, and extracts its balanced body while skipping strings, characters, and comments. It separately audits `TraverseUpperContractsContinuously` and `ReachUpperMechanismExitContinuously` for player position writes, fixture use, and direct exit touching.

`traversal-task3-fix1-helper-mutation-red.log` records the opposing mutation: inserting `player.Position = segment.LandingZone.GetCenter()` inside the real traversal helper exits 1 at `continuous helper guard rejects reposition/direct-touch in TraverseUpperContractsContinuously`, before any route marker. The mutation was removed with `apply_patch`.

### Failed-cast controller regression

The new real runtime regression performs exact current-room AnchorLow Apply, keeps the real player airborne, lets the same production `ThreadAbility` return naturally to Idle/cooldown-ready, drives the next real InputMap cast to `Failed`, then restores physics and lands supported inside the packed first landing zone. `upper_anchor_low` must remain incomplete.

For mutation proof, temporarily removing the existing production `Failed -> ResetUpperAnchorLowObservation()` call produced the named RED `a real failed cast clears the pending exact Apply before supported landing` after all ten route markers. The production branch was restored byte-for-byte; no guidance production diff remains.

### Final gates and preservation

- Build: 0 warnings, 0 errors.
- `traversal-task3-fix1-smoke-final-green.log`: exit 0, exactly four ordered smoke markers, one critical-path marker, zero runtime `ERROR:` lines.
- `traversal-task3-fix1-full-final-green.log`: exit 0, exactly ten route markers, exactly one tests marker, zero runtime `ERROR:` lines.
- Final `verify-prologue.ps1`: exit 0 with evidence, tests, critical-path, and verify markers.
- The prior semantic scene patch's 3 Awakening, 3 Broken, and 20 Upper CRLF additions are normalized to LF in the index only. Both plain `git diff --check` and the `cr-at-eol` variant pass. No scene semantic/resource/UID change is introduced.
- Lower scene/builder and all unrelated dirty files remain unstaged. Existing generated UID/resource worktree changes in the three authorized rooms remain preserved outside the index.

## Fix Round 2

This round is test-only. No gameplay, guidance production, geometry, scene, builder, movement/thread tuning, P0/P1, or Lower file changed.

### Velocity anti-cheat coverage

The balanced-body source audit for both `TraverseUpperContractsContinuously` and `ReachUpperMechanismExitContinuously` now rejects direct or compound assignment to `player.Velocity` as well as `Position` and `GlobalPosition`. Equality comparison remains allowed.

Opposing mutation proof inserted `player.Velocity = new Vector2(999f, -999f);` inside the executing `TraverseUpperContractsContinuously` helper. `traversal-task3-fix2-velocity-mutation-red.log` exited 1 at the named assertion:

```text
continuous helper guard rejects position/velocity writes or direct-touch in TraverseUpperContractsContinuously; stateWrite=True, fixture=False, directExit=False
```

The mutation failed before any `PROLOGUE_TRAVERSAL_SEGMENT_OK` marker (`0` markers), then was removed with `apply_patch`. Both real helpers are clean in the final source.

### Evidence negative regressions

`EvidenceChecks.Tests.ps1` now permanently asserts the innermost, intended validation reason for each malformed fixture:

- undeclared `ExtResource("missing")`: `ExtResource reference uses undeclared id 'missing'.`
- undeclared `SubResource("missing")`: `SubResource reference uses undeclared id 'missing'.`
- duplicate sub-resource ID: `Duplicate sub_resource id 'Resource_old' is ambiguous.`

The generated-token collision fixture now uses the implementation's actual literal `__GODOT_CANONICAL_EXT_RESOURCE_0__` and proves that ordinary metadata text is preserved rather than rewritten. Existing negative path/type/wiring, sub-resource-content, ordinary-semantic, duplicate ext-ID, and duplicate identity cases remain green.

### Final verification

- `traversal-task3-fix2-evidence-green.log`: exit 0, `EVIDENCE_CHECKS_OK`.
- `traversal-task3-fix2-prologue-evidence-green.log`: exit 0, `PROLOGUE_EVIDENCE_CHECKS_OK`.
- `traversal-task3-fix2-build-final-green.log`: exit 0, 0 warnings, 0 errors.
- `traversal-task3-fix2-smoke-final-green.log`: exit 0, exactly 4 ordered `PROLOGUE_TRAVERSAL_SMOKE_SEGMENT_OK`, exactly 1 `PROLOGUE_CRITICAL_PATH_OK`, 0 runtime `ERROR:` lines.
- `traversal-task3-fix2-full-final-green.log`: exit 0, exactly 10 `PROLOGUE_TRAVERSAL_SEGMENT_OK`, exactly 1 `PROLOGUE_TESTS_OK`, 0 runtime `ERROR:` lines.
- `traversal-task3-fix2-verify-final-green.log`: verifier exits 0 and ends with `PROLOGUE_VERIFY_OK`; its deliberate forced-smoke negative self-test remains expected verifier output.

Only the four Fix Round 2 authorized files are included in the narrow commit. All pre-existing unrelated imports, assets, scenes (including Lower and the three earlier Task 3 room worktree UID/resource hunks), capture files, and other dirty work remain outside the index.
