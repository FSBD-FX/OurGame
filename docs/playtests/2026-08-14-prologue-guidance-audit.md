# Prologue first-level guidance audit

Date: 2026-08-15 (Asia/Shanghai)
Implementation baseline: `073768c7529d1fc31a69baa597467e4f4a782e87`

## Evidence boundary

This audit covers automated structure, the real-input critical path, and deterministic visual proof.
The capture driver controls the player and must not be reported as human play evidence or as a human
clear time. A fresh, unassisted 10–15 minute hands-on completion pass remains open.

## Complete verification gate

`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1` was run twice from
the task baseline. Both runs exited `0`, built with 0 warnings and 0 errors, ran all 13 leaf-to-root
builders, and printed `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`
exactly once per run. Semantic scene comparison passed and the protected artifact hash drift count
was 0 after each run.

After the evidence-only HammerThrown dwell edit, the same full gate was run once more on the final
tree. It also exited `0`, built with 0 warnings/errors, emitted each test/smoke marker once, and left
all protected hashes unchanged.

The second run left one fresh import log, 13 fresh builder logs, one test log and one smoke log.
Together with the final capture log, all 17 non-RED engine logs were scanned for `ERROR:`,
`SCRIPT ERROR:`, `Parse Error`, and unhandled exceptions; there were 0 matches. The separate
`verify-prologue-smoke-forced-failure.log` contains the expected `FORCED_SMOKE_FAILURE` fixture and
is intentionally excluded from the non-RED scan.

Protected SHA-256 values after both runs:

| Artifact | SHA-256 |
| --- | --- |
| `project.godot` | `05CF8C16A51596EE89120167AAB48097E9E76DF198244A187B710BB9094E4283` |
| `scenes/player/Player.tscn` | `C32452ADD3DD05B93C424F3C7306D1E6B4D5932FE64D2B50FD5AA3703E176C5C` |
| `scenes/enemies/RustShellSquire.tscn` | `F683BDF351C7628459D0AAE501AF4BAD56339BCA96B8455309B217745144728E` |
| `scenes/enemies/GraveLampMoth.tscn` | `42692DD97BCEFA98638A3E6FB622106A3A55E4B5966ECA09DB72C7B5532B98AD` |
| `scenes/enemies/BellBackLaborer.tscn` | `B829708D9CF52CBD4414D297E331077D42BA05830F508387D969DAFBEE156638` |
| `scenes/boss/FallenBellkeeper.tscn` | `77E9428879FA6BA3E21C1159C3642A9C1D66D27E8E14A6C1AD345D2453F17EDF` |
| `scenes/mechanisms/BellChainMechanism.tscn` | `3A25EC2F95275522FF264A6136961E97A389B7BF2CD0214499384E1D1571A368` |
| `scenes/prologue/rooms/AwakeningCourtyard.tscn` | `0C68BFE7E7F68CC1F945577EE857C11D2AB2682A3ADEA2B825B3AB186492492B` |
| `scenes/prologue/rooms/BrokenToothCorridor.tscn` | `C0B26253D0E7F5202ABFB365F25BBD72FBF7388FE0F8E4A800F6D628E31CE2D1` |
| `scenes/prologue/rooms/UpperPendulum.tscn` | `40003FF5B74D5BF9FCFF8EFA11208C36AEE7C2C26EE14BF1DD7CB6F36B0BD61D` |
| `scenes/prologue/rooms/LowerBuriedMachinery.tscn` | `D1DB2F4D05980CE06074D5620484A98C875D74364D7E96B241E98BE67AD531FB` |
| `scenes/prologue/rooms/BellChainMechanism.tscn` | `0EE82F46BB2122378D12F1CBE27204135E50E12E5B9BA491063C988A1C18F65D` |
| `scenes/prologue/rooms/FallenBellkeeperArena.tscn` | `48807C155AD4DDB9122CF197EF4D1AE8397BA91DD75111CE0F5AF2DE2B77AE75` |
| `scenes/prologue/PrologueLevel.tscn` | `125B327A35827C4ACF0CFDC87EE9D42276A91A6DEB96128804927E28B88D932F` |

## Deterministic visual proof

The existing `tests/presentation/ProloguePresentation.cs` driver was captured to
`screenshots/prologue-guidance/final/` (not committed) with Godot 4.7.1 Mono movie writer at
1280×720, 60 FPS, 1380 PNG frames, and 23.0 seconds of deterministic simulation. Capture exited
`0`; `PROLOGUE_PRESENTATION_COMPLETE frame=1380` and the HammerThrown guidance stage marker each
occurred exactly once. The driver now holds HammerThrown for 30 frames (0.5 seconds) before sending
the scripted thread input so the chain instruction is visible; this changes evidence pacing only.

Reviewed frames:

| Frame | Observation |
| --- | --- |
| `00000050` | Courtyard next-step text is present and a right-edge gold arrow points to the off-screen corridor exit. |
| `00000140` | Corridor instruction retargets to the grave-lamp checkpoint; the cyan anchor and gold guidance marker remain visually distinct. |
| `00000280` | “Choose upper or lower route” shows two simultaneous gold route arrows; the bottom `AIM + K THREAD` prompt remains unobscured. |
| `00000620` | Bell-Chain instruction targets the mechanism with one gold diamond. |
| `00000675` | Immediately after opening the shortcut, instruction and marker retarget to the room's right-side gold Boss exit. |
| `00000700` | Inactive Boss guidance points toward the off-screen activation threshold. |
| `00000750` | Active Boss guidance targets the live Boss body; the guidance panel ends above the Boss name/health HUD. |
| `00000990` | HammerThrown displays “锁链已暴露：瞄准并按 K” and one gold chain diamond for the 30-frame dwell. |
| `00001018` | Completion removes instruction text, arrows and diamonds while leaving the completion objective and player HUD intact. |

Across these frames, the guidance panel does not overlap player health/thread status, the normal
objective, the bottom interaction prompt, or the Boss HUD. On-screen diamonds and clamped
off-screen arrows remain readable as separate marker forms.

## Scope and repository hygiene

- `git diff --check` returned no whitespace errors.
- `git diff --name-only 4bcfd00..32e5cf8` lists 35 feature-owned paths: the approved plan and audit,
  README, generated prologue level and builder, pause/guidance runtime, presentation driver, tests,
  and their Godot UIDs. It contains no tuning, collision, topology, art-generation, asset, or
  `project.godot` change. The narrower delivery range `073768c..32e5cf8` contains exactly README,
  this audit, the presentation dwell, and the 13 guidance/test UID sidecars.
- `project.godot` has no guidance Autoload. The controller and `Hud/Guidance/VisualRoot` are mounted
  by `BuildPrologueLevel.cs` only in the complete prologue first level.
- `PrologueGuidanceTargetKey` contains route, mechanism, activation, Boss-body and Boss-chain
  targets only; there is no hidden-alcove target.
- No art generation was run for this task. Captured frames stay uncommitted under `screenshots/`.
- At implementation baseline `073768c`, the repository tracked 176 Godot-generated `.cs.uid`
  sidecars. Godot import generated 13 new guidance/test sidecars, each containing one valid
  `uid://...` value; including them brings the current total to 189, matching the 189 tracked C#
  files. Unrelated Beehave, art/reference imports and generated scene working-tree noise remain
  unstaged.

## Remaining human acceptance

After final code review, launch `tools/play-prologue.ps1` and ask the player to complete the level
without oral navigation. Acceptance remains open until a real hands-on report records a complete
run, at least one Boss attempt, no repeated post-shortcut corridor/Bell-Chain loop, correct
explanation of text/arrow/diamond meaning, unobstructed HUD, and an actual human duration for the
10–15 minute target.
