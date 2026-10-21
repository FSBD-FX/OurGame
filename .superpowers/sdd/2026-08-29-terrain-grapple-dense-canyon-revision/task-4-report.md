# Task 4 — First-Level Guidance, Reticle Semantics and Replaceable Content Feedback

## Scope and compatibility decision

This slice updates the first-level mechanical-claw instruction, live terrain targeting,
reticle feedback, and the existing deterministic audio/VFX presentation seams. It does not
claim the user-owned `tests/RunTask11UiTests.cs` file: its byte SHA-256 at preflight was
`BA8E9F379A74C8F3D35621E67F5A3C7796A1FF99F58FE25BB02AA7409C87C1E8` and it has no Git
origin. The task-owned runner is `tests/RunTask4UiTests.cs`.

The compatibility alias `Retracting = OutOfRange` is retained only for old callers. It is
not a seventh visual state: it resolves to the dual-ring OutOfRange treatment.

## Delivered behavior

- Chinese and English guidance use live input glyph placeholders for `E`, held/released
  LMB, `A`/`D`, `W`/`S`, and one attached Space boost. The copy explicitly rejects ice and
  sealed surfaces, never calls the boost a double jump, and removes old J/K, Space-detach,
  and room-transition wording.
- `DescentGripStone` resolves through the generated CraneShaftWall terrain profile/owner;
  `DescentIce` resolves an explicit visible-ice profile.
- Attack, Grippable, Ice, Forbidden, Obstructed, and OutOfRange each have a distinct
  color, geometry, and existing-cue presentation: open circle, diamond, crossed ice bars,
  blocked hex/crossbar, dashed obstruction arc, and dual range ring respectively.
- No new audio or VFX assets were generated or downloaded. Existing deterministic cues are
  reused by the audio and VFX directors.
- The aim reticle now avoids visible Formal HUD protected regions at runtime. It picks the
  nearest legal padded position in the safe canvas bounds instead of drawing over status UI.

## Test-first evidence

The initial isolated RED is saved at
`artifacts/task11-revision/red/task4-ui.log` and reports these named checks:

```
TASK4_COPY_ZH_LIVE_GLYPH_A
TASK4_TARGET_DESCENT_GRIP_TERRAIN_PROFILE
TASK4_RETICLE_REQUIRED_STATE_Forbidden
TASK4_AUDIO_FAILURE_CUE_MAPPING_IS_EXPLICIT
```

A later visual inspection caught a genuine protected-region violation. The dedicated
runtime RED, at
`artifacts/task11-revision/task4-final-materialized/task4-reticle-protection-red.log`,
failed with:

```
real runtime reticle Attack overlaps visible FormalHud region (48, 40), (220, 72) at (1280, 720)
```

The green rerun is
`artifacts/task11-revision/task4-final-materialized/task4-reticle-protection-green.log`
and ends `PROLOGUE_UI_RESOLUTION_OK`.

## Task-only materialization and verification

All product verification ran from
`artifacts/task11-revision/task4-final-materialized`, constructed from committed/task-owned
blobs while excluding frozen shared-checkout deltas. Generated scenes and capture artifacts
remain only there and were not staged.

- `dotnet build --no-restore`: 0 warnings, 0 errors.
- `task4-canonical-runner-after-mutations.log`: `TASK11_UI_READABILITY_OK`.
- `task4-final-pre-after-protection.log`: `PROLOGUE_TEST_PHASE_OK phase=pre`.
- `task4-final-post-after-protection.log`: `PROLOGUE_TEST_PHASE_OK phase=post` and
  `PROLOGUE_TESTS_OK`.
- The post run contains known diagnostic output when the archive materialization tries to
  load absent audio resources, plus intentional invalid-presentation test warnings. They are
  observable in the log and did not suppress, weaken, or replace a test assertion.

The real OpenGL capture used the RTX 3060 Laptop GPU backend at 1280x720, 1920x1080,
2560x1440, and 3840x2160. It checks wrapped guidance, visible reticle, stable
`Hud/FormalHud/AimReticle` paths, and protected-region separation. Manual review confirmed
the 1280 Chinese long copy and 4K English long copy wrap completely; the post-fix 1280
Forbidden frame places the blocked hex/crossbar below the objective ribbon rather than over
the health region.

## Negative mutations and restoration

Each mutation was applied only in the task-only materialization, produced its named RED,
and restored the canonical source blob afterward.

| Mutation log | Required RED |
|---|---|
| `mutations-task4-final/reticle-attack.log` | `TASK4_RETICLE_SIGNATURE_Attack` |
| `mutations-task4-final/reticle-grippable.log` | `TASK4_RETICLE_SIGNATURE_Grippable` |
| `mutations-task4-final/reticle-ice.log` | `TASK4_RETICLE_SIGNATURE_Ice` |
| `mutations-task4-final/reticle-forbidden.log` | `TASK4_RETICLE_SIGNATURE_Forbidden` |
| `mutations-task4-final/reticle-obstructed.log` | `TASK4_RETICLE_SIGNATURE_Obstructed` |
| `mutations-task4-final/reticle-outofrange.log` | `TASK4_RETICLE_SIGNATURE_OutOfRange` |
| `mutations-task4-final/copy-old-wording.log` | `TASK4_COPY_EXCLUDES_STALE_double jump` |
| `mutations-task4-final/target-old-path.log` | `TASK4_TARGET_DESCENT_GRIP_TERRAIN_PROFILE` |

Restored canonical blob IDs are `1cdd2ac5ea8b7bea1be397a57248e3ca81701b57`
(`PrologueAimReticle.cs`), `0b4a3d7c063cad2b88c255255215e20fc8deccc7`
(`prologue-ui.json`), and `91cde54bc2d0f8523134c841c3796a7b8b6fe8ee`
(`PrologueGuidanceTargetResolver.cs`).

## Shared-worktree preservation

- Initial frozen shared delta:
  `task-4-preexisting.diff`, SHA-256
  `44F65BC3CE8A53A5F131ECBA7159972F4E4C381F199938EC0A4357EDCFFDE001`.
- Additional WorldTargetMarker user delta:
  `task-4-preexisting-world-target-marker.diff`, SHA-256
  `13494CE32AFB6323BB3408E57D138DE417F384B72FA4B6ED8558B3DF7B2D2138`.
- All six frozen generated-scene hashes match after verification:
  `PrologueLevel AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`,
  `CanyonEntrance 3F7844D5AFF22308D471EC5A99BCA9FB56F18BFA0C5F0618259FC87264A2C14D`,
  `CanyonDescent D5B981D9E350CD116D232F5762CC4E20B867795662314D9881455A1BA37236FC`,
  `CanyonBasin 6594F7A707E42B74BABC27EA275FC8CBE81981E8CB9A1C0A3567FCD255104B38`,
  `CanyonAscent 028B88B64EF11118CD36D2BB075260E72AA86E04A2DF61B6216C58B5BF14CC9C`, and
  `LostTemple C50CF4CE9180834F09C42D61DB77F49AD97AA10DC0DCD77F55FCE4029B71DEBE`.

Only task-owned source/test blobs and this report/ledger update are included in the final
isolated-index amendment. Screenshots, artifact logs, generated `.tscn` files, `project.godot`,
and all unrelated dirty files remain unstaged.
