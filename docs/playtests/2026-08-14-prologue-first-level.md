# Prologue first-level playtest and completion audit — 2026-08-14

## Hands-on evidence capture

Run `tools/play-prologue.ps1`. The scene-owned `ProloguePlaytestRecorder` writes one dedicated JSON
and Markdown pair per launch plus `latest.json` / `latest.md` under
`%APPDATA%\Godot\app_userdata\Knight\playtests\prologue`. The report captures only observable
session facts (unpaused time, room order, branch, checkpoints, deaths, Boss attempts/reentry,
recovery time-to-control, shortcut, alcove and completion), labels its source `HandsOnLauncher`, and
contains explicit blanks for the player's unclear-prompt and feel notes. Direct editor launches,
headless tests and deterministic capture drivers do not receive this source grant and therefore do
not write human evidence. This makes the three outstanding human rows reproducible without
presenting automated drivers as player timing.

## Evidence boundary

This milestone was exercised in three different ways and they are not interchangeable:

- `Knight (DEBUG)` was launched as the actual project main scene and inspected in a visible Godot window at 856×511. The opening room, placeholder visuals, HUD, contextual prompts and fall-recovery feedback rendered correctly. Sustained physical-key input moved the real player and a failed opening jump produced a real `FALL RESET` recovery.
- `tests/smoke/PrologueCriticalPathSmoke.cs` is the authoritative gameplay path. It uses the shipping InputMap actions, Area2D exits, CharacterBody2D physics, hitboxes, checkpoints and encounter rules. It does not report itself as human timing.
- The two presentation drivers create deterministic visual proof at 60 FPS. They use the same room exits and player actions, but reposition the proof actor between demonstrations to keep the recording compact. They are not timing evidence for a first-time player.

The current desktop automation cannot hold several simultaneous keys with human timing reliably enough to make a complete platforming run a defensible human-time measurement. Therefore the approved 10–15 minute first-clear target remains a tuning target, not a measured claim. A controller or keyboard hands-on session is still required before calling pacing final.

## Three acceptance routes

| Route | What was exercised | Result | Timing / observations |
| --- | --- | --- | --- |
| Normal route | Visible main-scene launch; real opening movement and fall; smoke route through courtyard, corridor, upper branch, light-enemy pull, Laborer exposure, mechanism, Boss and completion | Functional automation passes; required full hands-on run incomplete | One operator-caused fall in the opening courtyard. The prompt and `FALL RESET` feedback were readable. Automated completion is bounded and passes, but is deliberately not reported as a 10–15 minute playthrough. |
| Death / retry | Ordinary checkpoint recovery and shortcut persistence in critical-path smoke; phase-two death, fresh Boss reconstruction and real movement into the activation trigger in retry proof | Functional automation passes; required hands-on run incomplete | Retry proof asserts `PhaseTwoArmored` immediately before death at frame 321, confirms recovery at frame 322 and restarts at frame 371: 49 frames / 0.82 s from restored control. No stale Boss, active hitbox, thread target or duplicate encounter remains. |
| Hidden / optional | Upper Pendulum route, hidden-alcove discovery, return to Bell-Chain, permanent shortcut and completion without requiring the unchosen branch | Functional automation passes; required hands-on run incomplete | Discovery and shortcut flags survive death. Optional content does not gate the Boss or completion. The other branch remains reachable through the room graph. |

## Visual proof review

`tools/capture-prologue.ps1` produces fresh ignored evidence under `screenshots/prologue`:

- `prologue.avi`: 1380 frames, 1280×720, 60 FPS, 23.0 s.
- `boss-retry.avi`: 600 frames, 1280×720, 60 FPS, 10.0 s.

Representative frames were inspected from the awakening courtyard, corridor anchor lesson, upper-route Moth, hidden alcove, Laborer/mechanism room, Boss phase one, armor exposure, completion, death recovery and fresh retry. The gray-box silhouettes and navigation are readable; the Boss panel no longer overlaps the persistent HUD and correctly distinguishes `PHASE 1 / OPEN`, armored phase two and `EXPOSED`. No frame is presented as final art.

## Approved-spec completion audit

| Success criterion | Source / interface | Automated evidence | Playtest / proof evidence | Status |
| --- | --- | --- | --- | --- |
| Complete 10–15 minute first-clear loop | `PrologueLevelController`, six `RoomDefinition` scenes, objective sequence | Full real-input critical path reaches terminal completion within its safety budget | Complete 23 s demonstration proves visual continuity, not novice pacing | **Functional; human pacing still open** |
| Safe tutorial, first fight, branch, convergence, shortcut, Boss and ending | Generated room graph plus `PrologueObjectiveModel` | Room-contract, runtime, objective and smoke stages | Full proof visibly visits every required beat | Proven |
| Three thread uses before Boss | `ThreadAbility`, `IThreadTarget`, Moth, Laborer, Bell-Chain | Smoke performs anchor pull, light-enemy pull and heavy/mechanism interaction | Full proof shows all three states | Proven |
| Checkpoint recovery and ~3 s Boss re-entry | `PrologueCheckpointSnapshot`, `BossArenaController`, deferred recovery | Death fixture asserts restored player/room/shortcut and fresh Boss | Retry proof re-enters in 0.82 s after restored control | Proven |
| Art replaceable without gameplay changes | gameplay → presentation coordinator → `VisualRoot`; tuning/room/encounter resources | Structure/config tests reject missing roots and invalid resources | All visible art is procedural placeholder behind the boundary | Proven |
| Automation, real Godot runtime, three human routes and recordings | `verify-prologue.ps1`, this report, capture drivers | Tests + critical path + exact evidence contracts | Visible launch checked and both proofs reviewed; three full hands-on routes remain incomplete | **Incomplete: human route sessions required** |

## Change windows kept open

- Player feel: `PlayerTuning` and `ThreadTuning` resources.
- Enemy and Boss difficulty: `EnemyArchetypeTuning`, `BossTuning` and model settings.
- Composition: `EncounterDefinition` and `RoomDefinition`; connections remain named exits/entrances.
- Visual replacement: each entity or room `Presentation/VisualRoot` and presentation profile. Gameplay never reads sprite frames or texture paths for collision, damage or progression.
- Formal art, audio, Ember, Echo, disk save, localization and full settings remain outside this milestone.

## Next hands-on checkpoint

Have one person unfamiliar with the layout play the checked-in main scene with a normal keyboard or controller. Record clear time, deaths, chosen branch, unclear prompts and Boss attempts. If clear time falls outside 10–15 minutes, tune only the owning resource above and add a regression assertion before moving to formal art production.
