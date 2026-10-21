# New-game checkpoint reset regression report

## Root cause

`PrologueGameFlowController` accepted every valid disk save as an active run, including a
completed save whose checkpoint was `BrokenToothCorridor/checkpoint`. Starting a new game
reset the in-memory `PrologueProgress`, but did not immediately replace that completed late
checkpoint on disk.

The fix keeps completion records from advertising Continue and atomically persists the
fresh `AwakeningCourtyard/start` snapshot after `StartNewGame()` has reset and mounted the
real generated level.

## TDD evidence

- RED — completed save availability: a temporary completed late-checkpoint save failed
  `a completed late-checkpoint save does not advertise Continue as an active run`.
- GREEN — completed saves no longer set `HasSave`; the existing normal incomplete
  `BrokenToothCorridor/checkpoint` Continue fixture still restores its checkpoint and
  durable route progress.
- RED — new-game persistence: after Start New, the temporary disk save failed
  `Start New immediately replaces a late disk checkpoint with the courtyard round start`;
  expected `AwakeningCourtyard`, actual `BrokenToothCorridor`.
- GREEN — Start New immediately persists `AwakeningCourtyard/start`, `Completed=false`,
  and clears shortcut, discovery, and tutorial progress. In the same real generated level,
  a recovery requested before any checkpoint returns the player to the authored courtyard
  `start` marker.
- Mutation — removing `PersistProgress("new-game")` reproduced the named disk-checkpoint
  failure above. The production line was restored before verification and commit.

All fixtures use a unique directory below `Path.GetTempPath()` and delete it after the run.
The user's real `user://knight/prologue-save.json` was never read, changed, or deleted by
these regression tests.

## Final verification

- `dotnet build Knight.csproj`: exit 0, 0 warnings, 0 errors.
- Full `res://tests/RunPrologueTests.cs`: exit 0; `PROLOGUE_TESTS_OK` appears
  exactly once. Shared evidence checks found no engine/script/parse/fatal/native/leak
  errors in either the Godot log or merged console.
- `tools/verify-prologue.ps1`: exit 0 with `PROLOGUE_VERIFY_OK`, including the real
  critical-path smoke and its fail-closed forced-failure check.

## Fix Round 1 — durable completion evidence

Independent review found that replacing `Save` with a default object also erased
`BestHumanCompletionSeconds` and `BestHumanDeaths`. Those fields describe durable human
completion evidence, not current-run checkpoint progress.

- RED — a completed late save seeded with `123.5` seconds and `7` deaths became `-1/-1`
  after Start New. The named in-memory assertion failed with `expected=123.5, actual=-1`.
- GREEN — Start New carries both best-human fields into the fresh save while still clearing
  completed/checkpoint/shortcut/discovery/tutorial run state. Assertions cover both the
  controller's in-memory `Save` and the atomically replaced disk file.
- RED/GREEN — immediately after `Complete()`, `HasSave` remained true until a later title
  transition. `PersistProgress` now derives it from `!Save.Completed`, so the public state
  stops advertising the finished run immediately.
- Mutation — replacing the preservation initializer with `new PrologueSaveData()` failed
  `Start New preserves best human completion time in the fresh in-memory run` with
  `expected=123.5, actual=-1`. The preservation code was restored before final verification.

Round 1 final gate: build exit 0 with 0 warnings and 0 errors; the full runner exited 0
with exactly one `PROLOGUE_TESTS_OK` and no engine/script/parse/fatal/native/leak errors in
the Godot log or merged console; the full verifier exited 0 with `PROLOGUE_VERIFY_OK`.
