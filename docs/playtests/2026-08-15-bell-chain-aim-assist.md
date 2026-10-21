# Bell-Chain aim-assist verification — 2026-08-15

## Cause and scope

The first-level Bell-Chain target was visible and close, but a zero or off-angle aim caused a silent miss. The fix is target-local: only this mechanism opts into a `260px` fallback; anchors, enemies and the Boss chain remain directional.

## Automated evidence

- The pure model proves normal-first ordering, opt-in default zero, `260px` and global-range boundaries, line-of-sight rejection and deterministic tie-breaking.
- The real room proves neutral `K` fails outside `260px`, fails through a world wall, then emits the normal `ThreadHit`, draws the existing thread line and opens persistent progress in range.
- The guidance route and critical-path smoke open the mechanism with no movement direction held.
- Two consecutive full prologue gates passed with 13 builders and the exact `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK` and `PROLOGUE_VERIFY_OK` markers.

## Visual proof

The fresh post-review 1380-frame, 1280×720, 60 FPS prologue capture shows neutral thread input, the opening door and immediate Boss-exit retarget. The guidance resolver uses the stable path `Mechanisms/BellChain/ThreadTarget`, not the mechanism root. In frame `00000634`, the guidance diamond is centered on that actual hit target; in frame `00000635`, the thread terminates at the same visible position while the door opens, Boss-exit guidance appears and the player remains in place. Frames `00000636`–`00000641` preserve the opened state and readable HUD, and frame `00000642` shows the thread line released. This is deterministic functional proof using placeholder visuals, not human pacing evidence or a formal-art claim.

The paired Boss-retry capture contains 600 frames at the same 1280×720 and 60 FPS. Each capture produced exactly one completion marker and the capture gate ended with `PROLOGUE_CAPTURE_OK`.

## Human acceptance still open

Launch through `tools/play-prologue.ps1`. A fresh unassisted completion is still required before claiming the 10–15 minute first-clear target; the prior 560-minute idle-contaminated report is not pacing evidence.
