# KNIGHT application quit design

## Intent / authority

Close the real menu/window quit path without rearming audio while the engine still holds native playback. This advances the original eleven-item resource/UI lifetime requirements, not a narrower replacement goal. G:/knight/Knight only; user has delegated in-scope implementation choices and opted out of repeated design questions. Poses/art and human acceptance are not part of this fix.

v220 actual owned WAV+Playback leaked after Root/Audio/voice invalid; Stop/Streamnull/ownedDispose already ran. Godot4.7.1 stop requests are asynchronous. Existing Flow.OnQuit directly quits. Existing generic Audio.Shutdown must remain reusable (real Task10 reentry contract).

## Approaches / builder decisions

1. Repeat Stop/Dispose or change cached resource policy: rejected; those calls already execute and shared resources must not be destroyed.
2. Fixed sleep/disable audio/change the old last-leaf test: rejected; masks the boundary, does not protect real late producers, destroys diagnosis.
3. Chosen by builder under autonomous scope: terminal audio quiescing plus bounded observed drain at the real quit entry. Preserve generic Shutdown and the unchanged immediate-Quit diagnostic. No engine/dependency change, new save behavior, new UI or paid tool.

## Interfaces / policy

- `AudioQuitDrainState`: Waiting, Ready, Cancelled, TimedOut, InvalidObservation.
- Pure `AudioQuitDrainGate(double initialMixAgeSeconds, ulong initialProcessFrame)`; `Observe(AudioQuitDrainObservation)` consumes elapsed milliseconds, actual process frame, audio mix age, owner validity, all-players-silent, owned-streams-released. Actual mixer-age wrap followed by two distinct subsequent process observations AND silence/owned release is required. Duplicate frames cannot advance. Invalid/rollback data fails; owner loss cancels; deadline is 2000ms inclusive; terminal states are stable. This uses the existing teardown's 2s upper bound, not a fixed sleep or permission to quit successfully after timeout.
- `PrologueAudioDirector.IsQuiescing`; `BeginApplicationQuit()` sets terminal flag before callbacks, captures scalar IDs of owned WAVs, then existing Shutdown. Idempotent and never reset on reentry. Generic Shutdown alone stays reusable.
- `PrepareApplicationQuitAsync(): Task<AudioQuitDrainResult>` caches one task before synchronous shutdown callbacks. It observes the actual mix/process/owned native IDs on the main runtime, yields with bounded 2ms scheduling plus ProcessFrame, no GC or borrowed playback lookup/disposal. Node/tree loss returns Cancelled. Result includes state, elapsed and observation counts; no fabricated success.
- All audio/music/ambience/guidance/typed events and Task10 publisher binding/emit refuse terminal rearm before counters/stream allocation. Cleanup remains usable.
- `PrologueGameFlowController.PrepareApplicationQuitAsync()` caches the same flow task, marks IsApplicationQuitting before cancelling preparation/freezing gameplay, then uses audio task. Start/continue/title/settings/recovery continuations cannot restart a session during quit. No new persistence writes.
- Existing menu OnQuit and an application-root-only Window.CloseRequested handler await it and call Quit(0) only for Ready, otherwise report and Quit(1), except owner cancellation never quits a replacement scene. No new cancel button.
- Window lease binds deferred only when the level is the SceneTree.CurrentScene directly under Root. It saves/restores AutoAcceptQuit and unsubscribes on owner exit. Nested fixtures/other flows never claim it. Paused runtime still drains; global pause state is not changed merely to make drain succeed.

## Proof / boundaries

First real menu test uses authored saved level, isolated storage and existing Quit button; old synchronous OnQuit must fail immediate quiescence before the loop exits. Pure gate controls must RED before new model. Real enabled audio test covers terminal synchronous callback reentry, fresh native thruster after terminal denial, all players/tweens quiet, owned IDs released; reverse generic-Shutdown reuse remains. Independent menu/window/paused processes exercise the actual Flow quit and inspect awaited result before deferred engine exit; repeated requests share one preparation; owner exit cancels without global quit.

Original last-leaf remains untouched and strict diagnostic FAIL if the engine still reports it; no waiver or claim it passed. New normal exit correctness does not close historical Handle/FATAL, pre timeout, Main899, full routes, formal art/animation/feel or human/hardware acceptance. Root sole writer/native owner; all C#/helpers frozen while owned run/batch lives. No Git mutation/deletion/automation/GUI/true saves/extra dump. Exact pre-edit baselines retained instead of worktree duplication.

Risks: quit may take up to2s before explicit failure; mixer age is an observed boundary, not direct AudioServer playback-list inspection. All borrowed music/SFX release is additionally checked through silence and post-mix process cleanup; only owned WAV IDs can require invalidity because imported streams may remain cached. Application window ownership must be validated against two-flow/removed-owner controls. Existing pause-menu save/retry/cancel/exit-without-save policy stays intact before its committed quit request; its default quit handler must route to the same Flow handshake rather than bypass it.
