# v161 — camera contact custody

Continue the existing full eleven-item KNIGHT goal; this is a narrow correctness slice, not a new game design or delivery claim. The latest user answer is NOT SEEN. Do not reopen a window, capture new media, send OS input, change engines/GC/cache, or collect another dump.

## Facts, inference, risk

- Fact: the camera observer and production guidance use `using` on `CharacterBody2D.GetSlideCollision` results. The preceding pre-suite ended in a native finalizer failure after low-Ascent samples/audio drain, with no reliable later load boundary.
- Inference to test: the getter may return a shared wrapper; a read-only consumer must not invalidate another live reader's contact. This is not yet a proven bug or the historical FATAL root cause.
- Risk: extra observations affect timing. Keep contact references synchronous, never across await, and preserve original load/mount/frame/cleanup behavior. Do not mass-edit other collision consumers or equate this test with a root-cause fix.

## Contract

Use the actual saved Level/player/support and existing camera sample. Before the actual observer runs, retain a synchronous peer reference to its real upward support contact. Require that the peer remains valid, retains native identity, collider/normal and actor state when the observer returns. Log repeated-getter reference equality as evidence, not as a guessed API requirement. An invalid peer must fail before dereference.

Only after a genuine native RED may the exact observer's ownership handling change. Production guidance needs its own actual invocation/control before any change there. Keep owned `new KinematicCollision2D` query cleanup intact. No borrowed collision enters an async state machine. Do not add waits or weaken strict diagnostics.

## Acceptance boundaries

Existing `ranged-camera-ground` runs the real three camera fixtures. Targeted GREEN is not full pre/post qualification, a rendering result, human acceptance, leak closure, or historical FATAL closure. Original full routes, resources, formal assets/animation/UI/audio/hardware and the eleven-item backlog remain open. Preserve raw failures and version receipts.
