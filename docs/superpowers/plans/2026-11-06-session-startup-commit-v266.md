# v266 · 成功进入后再提交启动存档

> Inline existing approved P06/P07 failure-closed work; test first, one source review, no Git writes or cleanup.

**Goal:** preserve the prior durable/advertised slot when New Game or mapped Continue is cancelled or cannot start, and prevent a cancelled synchronous mount from publishing Playing. This is separate from the unresolved native Handle error.
**Spec:** original P06/P07/Q05 and failed-session/no-real-saves constraints in `docs/production/2026-10-18-project-remaining-backlog-v001.md`. No new save schema, UI design, rules or user choice on successful starts.
**Architecture:** private pending startup identity/version and draft; level initialization stays hidden, prepare/commit only if still owned, and the existing atomic save is deferred until just before publishing a successful entry. Cancel/reject invalidates the pending attempt and preserves prior committed Save/layout choice. No gameplay/save mutation through Title/Intro callbacks. Use the existing resident, quit and isolated storage seams, no engine/resource policy.

## Review focus

1. Old slot byte identity and actual title Continue preserved across cancelled/rejected startup; successful retry really writes and starts once.
2. Cold Continue synchronous world observer invalidates startup before publication, even if ResidentPreparationPending was cleared. No briefly active gameplay after cancellation.
3. Mapped legacy file untouched, recovered replacement backup policy retained, current Continue no unnecessary write. On startup IO failure return to title with honest failure, no new session.
4. Pending/nonpending legacy starts, retirement/quit and hidden gameplay side effects do not grant stale ownership. Existing legitimate live progress writes and old readiness tests unchanged.
5. Strict headless case-storage proof, no actual user saves, no extra dump/GC/cache changes, no stability or release claim.

### Task 1 — native RED and transaction

- [x] Run real cancel + synchronous cold-Continue observer against old code via canonical `resident-auto-session -SourceRows 1898`; seed actual isolated slot and compare bytes/retained published state. Retain both named failures, not compilation errors.
- [x] Keep `PrologueGameFlowController.Save` committed until the owned startup succeeds. Defer writes, gate lifetime/version and publication; use `PrologueLevelController` hidden initialization option for the flow, preserving direct-test calls. Add verified no-write/failed-write/retry and mapped migration controls within scope.
- [x] Re-run resident-auto-session and relevant original resident-auto-start/storage/retirement gates. Do not change time caps/warnings or re-run quiet filtering. Actual current native gates & acceptance only as far as executed; old resource failure remains OPEN.
- [x] Bind source/binary/host evidence and one independent review. Root sole writer/native executor; no GUI, OS input, generation, real saves, additional capture, automation, deletion or publication.

Scoped outcome: [v266 actual results](../../production/2026-11-06-session-startup-commit-v266.md). Full acceptance remains open.
