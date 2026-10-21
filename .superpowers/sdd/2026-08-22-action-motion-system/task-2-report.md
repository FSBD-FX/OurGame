# Task 2 report — player action timeline and formal-rig integration

## Scope

Implemented only the Task 2 allowlist from base `1c87b8c`:

- a deterministic `PlayerPresentationTimeline` that produces the authoritative
  four-argument `PlayerPresentationFrame` from real motor, combat, health, and
  `ThreadCastFrame` inputs;
- all 13 typed locomotion states and normalized presentation progress;
- stable ground/air/down attack identity with Anticipation, Burst, Contact, and
  cancellable Recovery driven by the existing `PlayerTuning` durations;
- typed mechanical-thread Preview, Windup, Throw, Latch, Apply, Recovery, and
  Failed presentation without duplicating cast state or cooldown rules;
- immediate direction-aware hurt deformation, death, and checkpoint recovery;
- a primary `PlayerFormalVisual.ApplyPlayerFrame` path that mixes locomotion,
  upper-body action, and Task 1 secondary motion once per slot per frame;
- version 2 typed animation-map entries while preserving `ApplyFrame`,
  `PlayCue`, and procedural placeholder compatibility;
- real generated-Player coverage for thread lifecycle/order and same-edge
  active/Failed-to-Idle locomotion restoration.

No gameplay collision, damage, input, attack timing, cooldown, mechanical-thread
state, builder, generated scene, import, art, audio, UID, screenshot, or other
agent-owned file was edited or staged.

## Contracts and implementation notes

- New production code constructs and consumes the four-field frame: Base,
  Locomotion, Action, and Secondary. The coarse frame remains an adapter only.
- Attack phase durations come from `PlayerTuning`; the presentation timeline
  advances a private deterministic clock because `PlayerCombat` currently
  exposes attack id/direction and `IsAttacking`, but no phase-progress frame.
  Gameplay authority and timing are unchanged.
- Attack type is captured on the first sample of a new attack id, so walking off
  an edge cannot turn one GroundAttack into an AirAttack mid-action.
- Thread action phase/progress is derived directly from the existing
  authoritative `ThreadCastFrame.State`, duration, and remaining time.
- Formal action ownership is limited to Body, NearArm, FarArm, and Weapon.
  Near/Far legs continue locomotion; Mantle, Lantern, ShoulderCore, and Tether
  receive the four independent Task 1 secondary transforms.
- Each stable formal slot receives one final rest-plus-mixed transform per
  frame. The old reset-every-layer and destination-snap loop is no longer the
  authoritative path.
- A narrow non-formal adapter reapplies the current typed thread cue to legacy
  procedural visuals. This keeps the fallback visibly posed through long
  Preview/Failed windows without changing the formal production clock.
- The existing active/Failed-to-Idle callback clears the cue and immediately
  presents a zero-delta current locomotion frame before later observers run.

## TDD evidence

### Initial RED

Tests were written before production changes. The first build failed as
required with exit `1`, `0` warnings, and `5` errors for the missing
`PlayerPresentationTimeline` / `PlayerPresentationInput` contracts:

```text
%TEMP%\action-motion-task2-red-build.log
```

Subsequent behavior RED runs exposed and fixed these integration boundaries:

- legacy real-Player tests still expected coarse `ThreadCast` / `Recovery`
  clips after typed phases became authoritative;
- the procedural placeholder lost a long Preview pose when only the formal
  visual consumed typed frames;
- one attack id changed GroundAttack to AirAttack when grounded state changed;
- health damage impulse was observed but did not yet drive direction-aware
  secondary deformation.

The last two had exact named RED assertions before their fixes:

```text
one attack id cannot switch from GroundAttack to AirAttack after leaving the floor
health impulse direction drives opposite bounded hurt deformation immediately
```

### GREEN coverage

The owned suites now verify:

- every locomotion state, normalized progress, landing classification, dash,
  death, and checkpoint recovery;
- ground/air/down attack preparation, burst, contact, hit/whiff recovery,
  recovery-only move/jump cancellation, hit pause, and stable attack identity;
- every `ThreadCastState` mapping and real generated-Player Preview through
  Recovery/Failed lifecycle, including movement and jump cancellation;
- hurt contact/recovery and opposite impulse directions;
- action-over-run leg motion, exact ownership exclusions, and exact secondary
  slot consumption;
- bounded displacement/rotation for all ten formal slots across 30 samples at
  60 FPS, plus explicit phase-boundary midpoint/delta continuity;
- typed animation-map completeness and visible legacy adapters.

## Required mutation evidence

Each mutation was applied alone, built successfully, failed the full runner on
the named assertion, and was removed by its inverse patch:

1. **Hard cut:** changed positive partial action interpolation to destination
   weight `1`. Exit `1`:

   ```text
   named per-slot phase-boundary midpoint advances without a destination-position hard cut
   ```

   Evidence: `%TEMP%\action-motion-task2-mutation-hard-cut.log`.

2. **Layer ownership:** added NearLeg to upper-body ownership. Exit `1`:

   ```text
   named locomotion-under-action assertion keeps both run legs advancing under an upper-body attack
   ```

   Evidence: `%TEMP%\action-motion-task2-mutation-ownership.log`.

3. **Cue clear:** skipped active-thread-to-Idle clear/current-locomotion
   presentation. The real generated Player failed with expected Apex versus
   actual ThreadApply:

   ```text
   real movement intent clears formal ThreadCast to the stored locomotion base on the authoritative Idle edge
   ```

   Evidence: `%TEMP%\action-motion-task2-mutation-cue-clear.log`.

No mutation remains.

## Final verification

Fresh post-mutation and post-self-review evidence:

```text
dotnet build --no-restore: exit 0, 0 warnings, 0 errors
RunPrologueTests.cs: exit 0, exactly one PROLOGUE_TESTS_OK
full log scan: 0 ERROR:, SCRIPT ERROR, Parse Error, or unhandled-exception matches
tools/verify-prologue.ps1: exit 0, PROLOGUE_CRITICAL_PATH_OK, PROLOGUE_VERIFY_OK
presentation audit: max_process_ms=16.47 (inside the existing 50 ms budget)
```

Runner evidence: `%TEMP%\action-motion-task2-final3-green.log`.
The verifier also passed import, all builders, semantic scene restoration,
audio/moth teardown, 60 FPS presentation, four UI resolutions, production
audit, its intentional forced-smoke-failure negative gate, and the restored
normal critical path.

## Residual integration note

`PlayerCombat` does not yet publish an authoritative attack phase/progress
frame. Task 2 therefore uses only its real attack id/direction/liveness plus
the centralized `PlayerTuning` durations for a presentation-only clock. If a
future combat contract exposes authoritative phase progress, the timeline
should consume that field and remove the private attack clock; no gameplay
timing change is needed now.

## Fix Round 1 — spec-review corrections

### Review findings and root causes

1. `AttackStarted` and `Downstrike` invoked the formal rig's legacy `PlayCue`
   immediately before the typed frame. The compatibility cue installed a Burst
   pose which then became the typed Anticipation source, reversing the intended
   preparation silhouette. Formal production presentation now treats the typed
   frame as the sole pose authority. Legacy visuals still receive cues, and
   VFX/audio cue side effects remain unchanged.
2. locomotion used normalized loop phase as both state-entry blend weight and
   perpetual blend weight. A Run/Idle wrap from `0.98` to `0.02` therefore
   blended back toward the original entry pose. State-entry blend progress and
   loop phase are now independent; phase wraps advance the monotonic entry blend
   without resetting any of the ten formal slots.
3. checkpoint recovery was represented only by the compatibility `PlayCue`
   adapter. `PlayerPresentationCoordinator` now consumes the existing
   `PrologueLevelController.RecoveryRequested` / `RecoveryCompleted` lifecycle,
   prioritizes the real recovery edge over the still-dead health sample, and
   clears it synchronously on completion.
4. AirAttack and DownAttack each had one phase-independent outline, and the
   typed action frame discarded the authoritative thread aim. Both attacks now
   expose distinguishable Anticipation, Burst, Contact, and Recovery transforms.
   Thread Preview/Windup/Throw/Latch/Apply consume normalized aim to alter the
   actual Body, NearArm/FarArm, and Weapon silhouette.

No gameplay duration, collision, damage, cooldown, input, cast-state, generated
scene, builder, or asset contract changed.

### Approved narrow scope expansion

The review required carrying `ThreadCastFrame.Aim` through the typed action
contract, so `scripts/presentation/PlayerActionFrame.cs` was added to the Task 2
allowlist with approval. Its optional fifth constructor argument preserves all
existing four-argument call sites. Finite non-zero aim is normalized, `None`
always stores zero, and non-finite input is rejected.

### Named RED then GREEN evidence

- Formal legacy cue contamination RED:
  `%TEMP%\action-motion-task2-fix1-red-legacy.log`; the real generated Player
  `AttackStarted` assertion expected unchanged Anticipation source geometry but
  observed legacy Burst (`NearArm.X` actual `1.4364998`). GREEN:
  `%TEMP%\action-motion-task2-fix1-green-legacy2.log`.
- Loop-wrap RED: `%TEMP%\action-motion-task2-fix1-red-loop-wrap.log`; the named
  Run `0.98 -> 0.02` Body continuity assertion failed. GREEN:
  `%TEMP%\action-motion-task2-fix1-green-loop-wrap.log`.
- Real recovery RED: `%TEMP%\action-motion-task2-fix1-red-real-recovery.log`;
  a real generated Player completed the checkpoint death lifecycle without
  publishing typed Recovery. GREEN:
  `%TEMP%\action-motion-task2-fix1-green-real-recovery2.log`.
- Four-stage silhouette RED:
  `%TEMP%\action-motion-task2-fix1-red-air-down-stages.log`; AirAttack stage
  `0->1` was not distinct. GREEN:
  `%TEMP%\action-motion-task2-fix1-green-air-down-stages.log`.
- Aim contract compile RED:
  `%TEMP%\action-motion-task2-fix1-red-typed-aim-build.log`; build failed with
  four named missing-`Aim` errors. After adding the contract/propagation, the
  behavior RED `%TEMP%\action-motion-task2-fix1-red-typed-aim-visual.log`
  showed identical actual Body rotation for Up/Down. GREEN:
  `%TEMP%\action-motion-task2-fix1-green-typed-aim.log`.

The generated-Player integration tests exercise real `PlayerCombat` attack
signals and a real `PrologueLevel.tscn` lethal-health/checkpoint lifecycle;
these are not pure timeline substitutes. The aim test uses a real
`ThreadCastFrame.Aim`, and the formal test asserts applied Body, Arm, and Weapon
rotations.

### Fix Round 1 mutation evidence

The original three Task 2 mutations were rerun against Fix Round 1 and failed
their exact named protections after successful zero-warning builds, then were
restored:

- hard cut: `%TEMP%\action-motion-task2-fix1-mutation-original-hard-cut.log`;
- upper-body ownership overwriting NearLeg:
  `%TEMP%\action-motion-task2-fix1-mutation-original-ownership.log`;
- skipped active/Failed-to-Idle cue clear:
  `%TEMP%\action-motion-task2-fix1-mutation-original-cue-clear.log`.

Five review-specific mutations were also run alone and restored:

- re-enabled formal legacy cue pollution; named real-Player Anticipation-source
  assertion failed (`%TEMP%\action-motion-task2-fix1-mutation-legacy-contamination.log`);
- restored loop phase as entry-blend weight; named Run wrap continuity failed
  (`%TEMP%\action-motion-task2-fix1-mutation-loop-wrap.log`);
- removed real recovery signal subscriptions; named generated-Player recovery
  lifecycle failed (`%TEMP%\action-motion-task2-fix1-mutation-real-recovery.log`);
- collapsed AirAttack Burst into Anticipation; named four-stage silhouette
  failed (`%TEMP%\action-motion-task2-fix1-mutation-stage-silhouette.log`);
- disabled aim transforms; named actual Body Up/Down rotation failed
  (`%TEMP%\action-motion-task2-fix1-mutation-thread-aim.log`).

No mutation remains.

### Fix Round 1 final verification

Fresh restored-tree evidence:

```text
dotnet build --no-restore: exit 0, 0 warnings, 0 errors
RunPrologueTests.cs: exit 0, exactly one PROLOGUE_TESTS_OK
runner log scan: 0 ERROR:, SCRIPT ERROR, Parse Error, or Unhandled matches
tools/verify-prologue.ps1: exit 0, PROLOGUE_CRITICAL_PATH_OK, PROLOGUE_VERIFY_OK
verifier non-forced logs: 54 scanned, 0 engine-error matches
presentation audit: max_process_ms=16.88 (inside the existing 50 ms budget)
```

Runner evidence:
`%TEMP%\action-motion-task2-fix1-final-runner.log`. The verifier's deliberate
`FORCED_SMOKE_FAILURE` negative-path log remained isolated and was accepted by
its exact-error fail-closed gate before the restored normal smoke passed.

## Fix Round 2 — ordered recovery and world-facing aim

### Review RED and root cause

Four review failures were reproduced independently from the formal generated
Player path before implementation:

- real Prologue lethal damage published `SoftLand` instead of a first visible
  typed `Dead` frame because `Health.Died -> RequestRespawn ->
  RecoveryRequested` completed in one synchronous call chain
  (`%TEMP%\action-motion-task2-fix2-red-dead-order.log`);
- left-facing world aim was rotated in world space and then mirrored again by
  the formal root, so Right/Left did not produce the same facing-local Body
  transform (`%TEMP%\action-motion-task2-fix2-red-facing-local-aim.log`);
- a real stationary left attack moved the gameplay hitbox left but kept the
  typed root facing right because presentation only read velocity
  (`%TEMP%\action-motion-task2-fix2-red-real-input-attack-facing.log`);
- a real up-left thread cast retained the cast aim but kept the typed root
  facing right because coordinator facing ignored `ThreadCastFrame.Aim`
  (`%TEMP%\action-motion-task2-fix2-red-real-input-thread-facing.log`).

The requested Idle wrap and Infinity contracts were missing coverage rather
than failing behavior on the reviewed baseline, so they were added as coverage
and proved by focused mutations instead of manufacturing a false pre-fix RED.

### Minimal implementation

- presentation now latches `Dead` and `Recovery` in a presentation-only stage
  machine. `Dead` is submitted synchronously from the real death signal, and
  both stages are held for at least one natural process pass before advancing;
  recovery completion cannot erase either visible state in the same call chain;
- formal thread transforms convert normalized world aim into facing-local aim
  before rotating Body, NearArm/FarArm, and Weapon, while root mirroring remains
  the sole horizontal mirror;
- the real attack-start sample uses the same `PlayerCombatModel` horizontal
  facing resolver and tuning dead zone as gameplay, then locks that facing for
  the active attack; active thread presentation gives authoritative cast aim
  priority over stale velocity;
- no gameplay duration, collision, hitbox placement, damage, attack identity,
  cast state, cooldown, scene, or generated node was changed.

The previously approved contract expansion in
`scripts/presentation/PlayerActionFrame.cs` remains source-compatible and
continues to normalize finite non-zero aim, store `None` as zero, and reject
NaN/Infinity. Fix Round 2 only adds the explicit Infinity regression assertion;
the contract file has no new diff in this round.

### Generated-Player and formal-rig coverage

- real `PrologueLevel.tscn` lethal damage records the consecutive formal order
  `Dead -> Recovery -> ordinary locomotion`; sampling is synchronized after a
  requested idle-process pass at the following physics boundary so headless
  multi-physics scheduling cannot skip a staged presentation frame;
- real `Input.ActionPress -> PlayerInputReader -> PlayerCombat` proves a
  stationary backward attack gives both the live hitbox and typed/formal root
  the same left facing;
- real diagonal thread input proves `ThreadAbility.CurrentCastFrame.Aim` enters
  the typed frame and drives root facing plus normalized X/Y;
- Right/Left and UpRight/UpLeft world pairs assert mirror-equivalent local Body,
  Arm, and Weapon transforms, with separate Up/Down silhouette assertions;
- Idle and Run both assert `0.98 -> 0.02` continuity for all ten formal slots;
- NaN and Infinity each have an explicit typed-action rejection assertion.

### Fix Round 2 mutation evidence

Every mutation built with zero warnings/errors, failed its exact named
protection, and was restored:

- removed the synchronous typed Dead submission:
  `%TEMP%\action-motion-task2-fix2-mutation-dead-latch.log`;
- skipped the Dead-to-Recovery stage:
  `%TEMP%\action-motion-task2-fix2-mutation-dead-recovery-order.log`;
- reapplied world aim directly to the local mirrored rig:
  `%TEMP%\action-motion-task2-fix2-mutation-facing-local-aim.log`;
- ignored real attack input facing:
  `%TEMP%\action-motion-task2-fix2-mutation-real-attack-facing4.log`;
- ignored real thread horizontal intent:
  `%TEMP%\action-motion-task2-fix2-mutation-real-thread-facing.log`;
- reset Idle entry blend from loop phase on wrap:
  `%TEMP%\action-motion-task2-fix2-mutation-idle-loop-wrap.log`;
- accepted Infinity by checking only NaN:
  `%TEMP%\action-motion-task2-fix2-mutation-infinity-aim.log`.

The original Task 2 hard-cut, ownership, and cue-clear mutations were already
rerun and restored in Fix Round 1 above. No mutation remains.

### Fix Round 2 final verification

Fresh restored-tree evidence:

```text
dotnet build --no-restore: exit 0, 0 warnings, 0 errors
RunPrologueTests.cs: exit 0, exactly one PROLOGUE_TESTS_OK
runner merged/log scan: 0 ERROR:, SCRIPT ERROR, Parse Error, or Unhandled matches
tools/verify-prologue.ps1: exit 0, PROLOGUE_CRITICAL_PATH_OK, PROLOGUE_VERIFY_OK
presentation audit: max_process_ms=18.26 (inside the existing 50 ms budget)
```

Runner evidence:
`%TEMP%\action-motion-task2-fix2-final-runner.log`; verifier evidence:
`%TEMP%\action-motion-task2-fix2-final-verifier.log`. The verifier's deliberate
`FORCED_SMOKE_FAILURE` negative path was accepted only by its isolated exact
fail-closed check before the restored smoke emitted `PROLOGUE_CRITICAL_PATH_OK`.

## Fix Round 3 — lossless frame observation and death action reset

### Review RED and root cause

The recovery assertion still observed only `LastPlayerFrame`, so scheduler
timing could replace a short-lived stage before the test sampled it. The first
RED changed the real Prologue test to request a read-only publication boundary;
the build failed exactly because `PlayerPresentationCoordinator.FramePublished`
did not exist (`%TEMP%\action-motion-task2-fix3-red-frame-published.log`). The
root cause was a lossy latest-value snapshot, not incorrect recovery ordering.

The second RED used real input to start `PlayerCombat`, applied real lethal
damage, and observed natural publications from the real generated Player. It
failed on Recovery with `expected=None, actual=GroundAttack`
(`%TEMP%\action-motion-task2-fix3-red-attack-death-state2.log`). On Dead, the
timeline returned `None` but retained attack ID/state/aim/elapsed/hit-pause;
the coordinator also retained the old attack ID, so Recovery resumed the old
presentation attack.

### Minimal implementation and coverage

- `FramePublished` is a public C# event: downstream observers may subscribe,
  but only the coordinator can invoke it. Every natural `PresentCurrent` first
  applies the authoritative four-part typed frame and then publishes that exact
  value. The event never drives gameplay and direct compatibility `Present`
  calls do not pretend to be natural samples;
- the real Prologue lifecycle test records event publications, collapses only
  adjacent duplicate locomotion states, and requires natural
  `Dead -> Recovery -> ordinary` order. Its frame budget is a fail-closed
  timeout only; no assertion polls `LastPlayerFrame`, manually calls `_Process`,
  or guesses which frame contains Recovery;
- the death entry clears both coordinator attack latches and the timeline's
  presentation-only attack ID/state/aim/elapsed/cancel/hit-pause/contact
  transient before synchronously publishing Dead. Clearing both sides prevents
  the coordinator's old ID from immediately re-latching a reset timeline;
- a second real lifecycle starts an attack through
  `Input.ActionPress -> PlayerInputReader -> PlayerCombat`, applies lethal
  damage, and asserts Action `None` for every published frame from Dead through
  Recovery and the first ordinary locomotion frame;
- no gameplay combat model, hitbox, damage, collision, cast state, recovery
  timing, scene, generated node, or traversal document was changed.

### Fix Round 3 mutation evidence

Both mutations built with zero warnings/errors, failed their exact named
protection, and were restored:

- skipped only Recovery frame publication; the event-driven lifecycle timed out
  without fabricating a sampled state
  (`%TEMP%\action-motion-task2-fix3-mutation-skip-recovery-publish.log`);
- retained the pre-death coordinator/timeline attack state; the natural
  Recovery publication restored `GroundAttack` instead of `None`
  (`%TEMP%\action-motion-task2-fix3-mutation-retain-attack-state.log`).

No mutation remains.

### Fix Round 3 repeated final verification

Fresh restored-tree stability evidence:

```text
full runner 1: build exit 0, 0 warnings, 0 errors; runner exit 0;
               exactly one PROLOGUE_TESTS_OK; merged/log error matches 0
full runner 2: build exit 0, 0 warnings, 0 errors; runner exit 0;
               exactly one PROLOGUE_TESTS_OK; merged/log error matches 0
verifier 1: exit 0; PROLOGUE_CRITICAL_PATH_OK; PROLOGUE_VERIFY_OK;
            max_process_ms=18.06
verifier 2: exit 0; PROLOGUE_CRITICAL_PATH_OK; PROLOGUE_VERIFY_OK;
            max_process_ms=19.28
```

Evidence logs:

- `%TEMP%\action-motion-task2-fix3-final-runner-1.log`;
- `%TEMP%\action-motion-task2-fix3-final-runner-2.log`;
- `%TEMP%\action-motion-task2-fix3-final-verifier-1.log`;
- `%TEMP%\action-motion-task2-fix3-final-verifier-2.log`.

Each verifier isolated and accepted its deliberate `FORCED_SMOKE_FAILURE`
negative-path probe before the restored critical-path smoke passed.

## Fix Round 4 — edge identity, observer isolation, local secondary motion, and terminal exit

The code-quality follow-up preserves gameplay timing, hitboxes, damage,
collision, cast state, and generated scenes while tightening the typed
presentation boundary:

- presentation attack identity now separates the last observed external edge
  from the currently active timeline action. Natural completion can clear the
  active action without treating the coordinator's unchanged ID as a fresh
  attack. Death and non-death checkpoint recovery share one presentation-only
  attack clear path;
- `FramePublished` is explicitly diagnostic/downstream observation. Each
  subscriber is isolated, later subscribers still receive the same natural
  frame, recovery sequencing cannot be frozen, and a read-only failure count
  summarizes swallowed observer failures without polluting engine error gates;
- world velocity, acceleration (including hurt impulse), and turn direction
  are converted exactly once into facing-local secondary inputs. The formal
  root remains the sole X mirror, so Body, Mantle, Lantern, shoulder spool, and
  tether/charm produce equivalent right/left world silhouettes;
- authoritative Thread Recovery progress now fades the action overlay itself,
  revealing live locomotion instead of blending an owned action slot merely to
  identity. Apply and Failed retain a visible middle window and approach Idle
  continuously at the real NearArm/Weapon slots. ThreadFailed is not marked
  recovery-cancellable;
- extreme finite typed Aim uses scaled double-precision normalization, keeping
  direction finite without changing the existing zero and non-finite input
  contracts;
- animation map schema version, per-clip fps, and loop flags are parsed and
  validated against runtime clip metadata. The approved scope extension remains
  limited to `scripts/presentation/PlayerActionFrame.cs`; no gameplay or scene
  file changed.

### Fix Round 4 RED and mutation evidence

Genuine REDs named each observed defect before the minimal fix:

- non-death checkpoint recovery retained `GroundAttack`
  (`%TEMP%\action-motion-task2-fix4-red-checkpoint-attack-lock.log`);
- a throwing observer blocked downstream recovery frames and produced repeated
  diagnostic exceptions
  (`%TEMP%\action-motion-task2-fix4-red-throwing-frame-observer.log`);
- mirrored motion produced opposite facing-local Mantle X values
  (`%TEMP%\action-motion-task2-fix4-red-facing-local-secondary.log`);
- ThreadApply `.98 Recovery -> Idle` hard-cut the actual NearArm slot
  (`%TEMP%\action-motion-task2-fix4-red-thread-terminal-exit.log`);
- extreme finite Aim normalized to zero, ThreadFailed was cancellable, and JSON
  metadata had no parsed contract
  (`%TEMP%\action-motion-task2-fix4-red-extreme-finite-aim.log`,
  `%TEMP%\action-motion-task2-fix4-red-threadfailed-cancellable.log`,
  `%TEMP%\action-motion-task2-fix4-red-animation-map-metadata.log`).

Every restored-tree mutation built and failed its named protection:

- active-ID comparison re-latched the unchanged external edge and timed out
  waiting for five post-completion None frames
  (`%TEMP%\action-motion-task2-fix4-mutation-attack-edge-memory.log`);
- skipping the checkpoint clear restored `GroundAttack` during Recovery
  (`%TEMP%\action-motion-task2-fix4-mutation-checkpoint-attack-clear.log`);
- direct multicast observer invocation lost the natural recovery sequence
  (`%TEMP%\action-motion-task2-fix4-mutation-observer-isolation.log`);
- world-space secondary inputs violated the named facing-local hurt/mirror
  contract (`%TEMP%\action-motion-task2-fix4-mutation-facing-local-secondary.log`);
- fixed terminal action weight caused a `0.51639795` rad NearArm exit cut
  (`%TEMP%\action-motion-task2-fix4-mutation-thread-terminal-exit.log`);
- float-vector normalization collapsed the extreme Aim direction
  (`%TEMP%\action-motion-task2-fix4-mutation-extreme-finite-aim.log`);
- marking ThreadFailed cancellable failed its authoritative state contract
  (`%TEMP%\action-motion-task2-fix4-mutation-threadfailed-cancellable.log`);
- ignoring JSON version or fps/loop fields failed the schema metadata contracts
  (`%TEMP%\action-motion-task2-fix4-mutation-animation-map-metadata.log`,
  `%TEMP%\action-motion-task2-fix4-mutation-animation-map-fps-loop.log`).

No mutation remains.

### Fix Round 4 repeated final verification

```text
full runner 1: build exit 0, 0 warnings, 0 errors; runner exit 0;
               log/console each exactly one PROLOGUE_TESTS_OK; error matches 0
full runner 2: build exit 0, 0 warnings, 0 errors; runner exit 0;
               log/console each exactly one PROLOGUE_TESTS_OK; error matches 0
verifier 1: exit 0; PROLOGUE_CRITICAL_PATH_OK; PROLOGUE_VERIFY_OK;
            max_process_ms=19.20
verifier 2: exit 0; PROLOGUE_CRITICAL_PATH_OK; PROLOGUE_VERIFY_OK;
            max_process_ms=16.48
```

Evidence logs:

- `%TEMP%\action-motion-task2-fix4-final-runner-1.log` and `.console.log`;
- `%TEMP%\action-motion-task2-fix4-final-runner-2.log` and `.console.log`;
- `%TEMP%\action-motion-task2-fix4-final-verifier-1.log`;
- `%TEMP%\action-motion-task2-fix4-final-verifier-2.log`.

Both verifiers accepted only their deliberate `FORCED_SMOKE_FAILURE` negative
probe before the restored critical path passed, and their snapshot `finally`
restored every generated scene byte-for-byte.
