# First-level combat readability presentation audit

## Evidence boundary

This is deterministic, automated presentation evidence only. It proves that the
real authored route renders the named states; it does not claim a human player
understood the route, enjoyed the difficulty, or completed the level. Those
claims remain exclusively for the source-labelled `HandsOnLauncher` evidence in
Task 3.

## Automated capture contract

- Canonical route: `1280x720`, fixed `60 FPS`, `2989` PNG frames. The extra
  `120` frames are a two-second dwell on the real completion card before the
  existing title return; gameplay, actors, tuning, and generated scenes are
  untouched.
- Retry route: `1280x720`, fixed `60 FPS`, `600` PNG frames.
- UI captures: `1280x720`, `1920x1080`, `2560x1440`, `3840x2160`, and
  `2560x1080`; every output is freshly deleted and recreated, non-empty,
  dimension-checked, SHA-256 manifest-hashed, and checked against exact log
  markers and engine-error signatures.
- The route publishes one exact marker for every ordinary-enemy phase,
  Boss attack, thread phase/failure, player action state, room, compact and
  expanded guidance, Boss HUD, and real completion card. Distinct markers may
  not collapse phases.

## Fresh evidence reviewed

The final repeat began from cleared local proof outputs and completed at
2026-08-24 11:25:28 UTC (route/retry) and 11:26:02 UTC (UI).

| Evidence | Contract | SHA-256 manifest |
| --- | --- | --- |
| `screenshots/prologue/prologue-frames` | 2989 @ 1280x720 / 60 FPS | `16f6da18ec9ae2642acc096f0108ccf959dd5fac91065778ffbb1ed5bbedc7fb` |
| `screenshots/prologue/boss-retry-frames` | 600 @ 1280x720 / 60 FPS | `a22caabb62890665bd906256e79655925cbcdb3636f9551a6ad36338a12243a6` |
| `screenshots/task3-compact-hud/1280x720` | 8 PNGs | `5e1d5ec04d4c2e1173ddb9952f17e59dd2a2ec195a7cfa41d137aa5200feb3a9` |
| `screenshots/task3-compact-hud/1920x1080` | 4 PNGs | `dd220c06cce3085422670c927ac0065fa91f29088038ec75b1220ed11e6be3cc` |
| `screenshots/task3-compact-hud/2560x1440` | 4 PNGs | `a564552c3f0d54df25014854ff0f1c477c52216b9e15d53c116e626cea7f2154` |
| `screenshots/task3-compact-hud/3840x2160` | 4 PNGs | `17b2e99eabc90c0f3cb4f774aea1460aa0d5d6a3a534373e854278fbe35af8af` |
| `screenshots/task3-compact-hud/2560x1080` | 4 PNGs | `2a40ddb92789251fb364a4c6c100b25f366c266690ed5346ce3b5f087f7fa86c` |

The first and second post-fix fresh captures both produced exactly `2989` route
frames and `600` retry frames. `PROLOGUE_CAPTURE_OK` and
`PROLOGUE_UI_RESOLUTION_OK` occur once per run; no `ERROR`, `SCRIPT ERROR`, or
`Parse Error` signature was accepted.

## Consecutive-frame visual review

Review used the numbered PNG sequences around exact `PROLOGUE_SENSORY_CAPTURE`
frames, never a contact sheet alone. Rust, Moth, and Laborer were reviewed at
Telegraph/Active/Recovery; the Boss at Sweep/Leap Slam/Ground Wave/Hammer; the
thread at Preview/Windup/Travel/Latched/Apply/Failed/Recovery; player
run/brake/jump/land/attack/thread/hurt; all six room landmarks; compact and
expanded guidance; Boss HUD, retry, and the completion-card dwell.

| Category | Result | Review observation |
| --- | --- | --- |
| Attack prediction and true-range alignment | Pass | Telegraph shape and Active impact transition remain visible for Rust, Laborer, and each Boss attack; the re-framed real Moth is now visible throughout its three reviewed phases. |
| Skill phase separation | Pass | Thread preview, gold travel strand, latched/apply feedback, failure, and recovery occupy distinct consecutive frames. |
| Pose continuity; cape/lantern inertia | Pass | Player and enemy pose changes are continuous across reviewed frames; cape, tether, and lantern remain attached with no hard one-frame cut. |
| Landmark identity and enemy silhouettes | Pass | Courtyard, corridor, pendulum, lower machinery, Bell-Chain, and arena read as distinct locations; Rust, Moth, Laborer, and Bellkeeper silhouettes remain distinguishable in their proof framing. |
| Foreground occlusion | Pass | The Moth proof was re-framed after the initial audit exposed it behind broad room composition; the final sequence holds it clear of the foreground. |
| HUD overlap and prompt placement | Pass | Expanded/compact guidance, bottom Boss HUD, recovery, and completion remain clear at all five audited resolutions. |
| Danger-color discipline | Pass | Gold/teal guidance stays separate from the red/white active-danger treatment; no full-screen flash appears in the reduced-flash capture. |

## Remaining boundary

Automated evidence is complete enough to hand to the human playtest. It does
not supersede Task 3's first-time completion, difficulty, prediction, or
comfort review.

## Fix Round 1 — superseding executable and visual record

This section supersedes the earlier final-capture inventory and the earlier
all-pass visual summary. It is the durable Task 2 conclusion.

### Executable evidence

The canonical route is `1280x720` at fixed `60 FPS`, with `2989` frames. The
Boss retry is `1280x720` at fixed `60 FPS`, with `600` frames. UI state evidence
contains three consecutive PNGs per state: `24` at `1280x720` and `12` at each
of `1920x1080`, `2560x1440`, `3840x2160`, and `2560x1080` (`72` UI PNGs total).

Fresh baseline capture: `2026-08-24 12:23:46 UTC`, retained in
`task2-fix1-matrix-first.log` and `task2-fix1-matrix-first.manifest.txt`.
Fresh repeat: `2026-08-24 12:26:41 UTC`, retained in
`task2-fix1-matrix-repeat.log`; it emitted
`PROLOGUE_CAPTURE_MANIFEST_MATCH ... entries=7` and `PROLOGUE_CAPTURE_OK`.
`Assert-PngCaptureContract` opens, dimension-validates, and raw-hashes **every**
ordered PNG in the same loop. The repeat comparison rejects a changed valid PNG
or any altered route/retry/UI manifest entry. The sole comparison normalization
is the controller-owned elapsed-time glyph rectangle (`x=504,y=249,w=74,h=20`)
on real completion-card frames `2741-2868`; exact original PNG hashes remain
recorded, and every pixel outside that known volatile region is compared.

| Evidence | Contract | SHA-256 manifest |
| --- | --- | --- |
| `screenshots/prologue/prologue-frames` | 2989 PNGs, 1280x720, 60 FPS | `7a973f42b047cc129de82617fefaa946392fc1a2335b6426798d03ece45a2f0d` |
| `screenshots/prologue/boss-retry-frames` | 600 PNGs, 1280x720, 60 FPS | `a22caabb62890665bd906256e79655925cbcdb3636f9551a6ad36338a12243a6` |
| `screenshots/task3-compact-hud/1280x720` | 24 PNGs, 1280x720 | `20bf72aafcc9dd431ace8db171d2f4174665d37b3af15cc8ef3eaffab4293185` |
| `screenshots/task3-compact-hud/1920x1080` | 12 PNGs, 1920x1080 | `42a50a4d5ac088ef6fbb4307ffd5a759f5e8e04f0ff0dd773d18f55ee1525c34` |
| `screenshots/task3-compact-hud/2560x1440` | 12 PNGs, 2560x1440 | `688cf5026af2f88c535f5a3709526bae32e294466061d2db91d842a5f72c5d66` |
| `screenshots/task3-compact-hud/3840x2160` | 12 PNGs, 3840x2160 | `eaa18d942182a486b3c89ee97dd585120542df2179136a59bada0f83006656fb` |
| `screenshots/task3-compact-hud/2560x1080` | 12 PNGs, 2560x1080 | `ac9634a6efd8c1cac8c3ec0e1badcab8cab6fecb605f3f403599d4ca8e146c8a` |

### Durable consecutive-frame manual review matrix

All observations below come from opening the listed numbered PNGs in order,
not from marker names or a contact sheet. `route` means
`screenshots/prologue/prologue-frames/frameNNNNNNNN.png`; `retry` means
`screenshots/prologue/boss-retry-frames/frameNNNNNNNN.png`; UI paths use the
literal `-frame00.png` through `-frame02.png` sequence shown. `Pass` only means
the stated visual criterion passed; it is not a human-playtest conclusion.

| Required state | Exact consecutive frames | Observation | Result |
| --- | --- | --- | --- |
| Rust Shell Squire — Telegraph / danger / Active / Recovery | route `429-431` / `449-451` / `468-470` / `477-479` | Body, cue, impact and settle are separately visible; no false one-frame cut. | Pass |
| Grave-lamp Moth — Telegraph / danger / Active / Recovery | route `899-901` / `920-922` / `941-943` / `962-964` | The real Moth remains visibly framed in all named phases; the dive silhouette and tether/impact are readable. | Pass |
| Bell-back Laborer — Telegraph / danger / Active / Recovery | route `1762-1764` / `1785-1787` / `1807-1809` / `1819-1821` | Guard, danger, strike and recovery stay distinct from one another. | Pass |
| Bellkeeper Sweep — Telegraph / Active / danger / Recovery | route `2100-2102` / `2124-2126` / `2148-2150` / `2163-2165` | The state transition can be seen, but centered guidance covers the Boss body/telegraph and its active read. | **Fail — prompt occlusion** |
| Bellkeeper Leap Slam — Telegraph / Active / danger / Recovery | route `2205-2207` / `2232-2234` / `2259-2261` / `2274-2276` | Jump/landing progression is present, but the same centered guidance overlaps the central Boss action. | **Fail — prompt occlusion** |
| Bellkeeper Ground Wave — Telegraph / Active / danger / Recovery | route `2319-2321` / `2349-2351` / `2379-2381` / `2397-2399` | Wave progression is visible at the edges, but the central prompt obstructs the primary body/telegraph read. | **Fail — prompt occlusion** |
| Bellkeeper Hammer — Telegraph / Active / danger / Recovery | route `2509-2511` / `2535-2537` / `2560-2562` / `2608-2610` | Hammer sequence changes are visible, but the prompt overlaps the central threat and reduces prediction clarity. | **Fail — prompt occlusion** |
| Thread — Failed / Preview / Windup / Travel / Latched / Apply / Recovery | route `617-619` / `637-639` / `649-651` / `653-655` / `667-669` / `672-674` / `675-677` (plus cooldown failure `679-681`, obstruction `736-738`) | Empty-target failure, preview, cast, travel, latch, apply and recovery each have a visibly different tether/cursor presentation. | Pass |
| Player action continuity — Run / Jump / Land / Attack / Brake / Thread / Hurt | route `296-298` / `301-303` / `327-329` / `356-358` / `457-459` / `620-622` / `751-753` | Poses change continuously; visible cape, lantern and tether remain attached without a hard frame pop. | Pass |
| Awakening Courtyard | route `249-251` | Opening platform, player, objective and courtyard composition remain identifiable through the transition. | Pass |
| Broken Corridor | route `397-399` | Corridor checkpoint composition is visually distinct from the courtyard. | Pass |
| Upper Pendulum | route `807-809` | Hanging/vertical machinery landmark distinguishes the upper room. | Pass |
| Bell-Chain | route `1408-1410` | Chain/mechanism landmark distinguishes this room. | Pass |
| Lower Buried Machinery | route `1606-1608` | Lower machinery and the different palette/layout identify the room. | Pass |
| Fallen Bellkeeper Arena | route `1996-1998` | Arena scale, Bellkeeper silhouette and boss framing are distinct from the six-room route. | Pass |
| Title UI | route `0-2` | Title, focused first action and menu hierarchy remain stable over consecutive frames. | Pass |
| In-route Boss HUD | route `2041-2043` | Bottom health/phase HUD is legible and remains inside safe area. | Pass |
| In-route completion card | route `2741-2743` | Real controller-owned completion card, elapsed result, outcomes and actions are readable over the fixed completion dwell; original PNGs are retained exactly. | Pass |
| Death, checkpoint and retry continuity | retry `396-398` / `397-399` / `446-448` / `498-500` / `598-599` | Death reset resolves to lit checkpoint, then player re-enters the arena with a coherent HUD/actor state. | Pass |
| UI — 1280x720 all states | `screenshots/task3-compact-hud/1280x720/{expanded-teaching,ordinary-keyboard,boss-bottom-safe,completion-clear,ordinary-controller,ui-scale-150,repeated-failure-retry,reduced-flash-two-channel}-frame00..02.png` | Every listed state was opened as three consecutive captures: no crop, overlap, blank output or unsafe-edge placement; reduced-flash state has no full-screen flash. | Pass |
| UI — 1920x1080 core states | `screenshots/task3-compact-hud/1920x1080/{expanded-teaching,ordinary-keyboard,boss-bottom-safe,completion-clear}-frame00..02.png` | Guidance, glyph, Boss-safe placement and completion dialog are legible and uncropped. | Pass |
| UI — 2560x1440 core states | `screenshots/task3-compact-hud/2560x1440/{expanded-teaching,ordinary-keyboard,boss-bottom-safe,completion-clear}-frame00..02.png` | Same four states remain readable at 1440p with no edge collision. | Pass |
| UI — 3840x2160 core states | `screenshots/task3-compact-hud/3840x2160/{expanded-teaching,ordinary-keyboard,boss-bottom-safe,completion-clear}-frame00..02.png` | Same four states remain readable at 4K with no crop or scaling loss. | Pass |
| UI — 2560x1080 core states | `screenshots/task3-compact-hud/2560x1080/{expanded-teaching,ordinary-keyboard,boss-bottom-safe,completion-clear}-frame00..02.png` | Same four states remain readable in supported ultrawide without side crop. | Pass |

### Sensory-category judgment

| Review category | Exact reviewed evidence | Result and concise observation |
| --- | --- | --- |
| Attack prediction and true-range alignment | Rust/Laborer rows; all four Boss rows above | Pass for the ordinary enemies; **Fail for every Boss attack** because centered guidance obscures the primary prediction area, most clearly Sweep `2100-2165`. |
| Skill phase separation | Thread `617-681`, `736-738` | Pass — all seven required thread phases/failure states are visually separable. |
| Pose continuity; cape/lantern inertia | Player `296-303`, `327-329`, `356-358`, `457-459`, `620-622`, `751-753` | Pass for visible continuity/no detachment or one-frame pop; these images do not establish subjective animation feel. |
| Landmark identity and enemy silhouette | Six room rows; ordinary enemy rows; arena `1996-1998` | Pass — landmarks and the four enemy silhouettes can be distinguished. |
| Foreground occlusion | Moth `899-964`; Boss rows `2100-2610` | Pass for the Moth; **Fail for Boss** because a foreground guidance card obscures the central visual information. |
| HUD overlap and prompt placement | UI rows; in-route Boss HUD `2041-2043`; Boss rows | UI capture placement passes at five resolutions, but **Fail in the real Boss route**: centered guidance overlaps the threat. |
| Danger-color discipline | Ordinary/Boss attack rows; `reduced-flash-two-channel-frame00..02.png` | Pass — gold/teal guidance remains distinct from danger treatment; no full-screen flash appears in the reduced-flash sequence. |

### Result and handoff

**DONE_WITH_CONCERNS.** The executable evidence contract and its deterministic
repeat pass, and the complete project gate passed at `2026-08-24 12:40:29 UTC`
with `PROLOGUE_VERIFY_OK`. The manual review uncovered a real production sensory
defect: centered Boss guidance obscures the Boss sweep/attack read. This task
does not change production presentation to hide it. Task 3 owns the human
review and the production follow-up; automated evidence remains evidence only.

### Task 3 Phase A — Boss-guidance production correction (2026-08-24)

Append-only follow-up: the centered Boss prompt was traced to the centralized
`ProtectedPromptPlacer.Place` fallback, which returned its nearest unsafe local
candidate when every target-adjacent candidate intersected real Boss/danger
geometry. The correction preserves normal local placement and selects a clear
safe-area top edge only when the local ring is exhausted. The visible formal
Boss HUD is now part of that same protection set after the real Hammer route
showed a bottom-HUD collision candidate.

The focused RED suite failed on `Boss combat guidance fallback never covers the
Boss, feet, or four attack reads`; removing the safe-edge loop as a mutation
reproduced that same failure. The restored GREEN route reports protected-clear
consecutive guidance frames: Sweep `63`, Leap Slam `69`, Ground Wave `78`,
Hammer Throw `99`.

Fresh display-backed evidence is retained at
`screenshots/task3-phase-a-boss-frames` (2,989 consecutive 1280x720/60 FPS
frames). Manual inspection of Sweep `2101-2103`/`2125-2127`, Leap
`2206-2208`/`2233-2235`, Ground `2320-2322`/`2350-2352`, and Hammer
`2510-2512`/`2536-2538` found the card at the top right with Boss body, feet,
danger read, and Boss HUD clear. Fresh `screenshots/task3-phase-a-ui` captures
also passed and were inspected at 1280x720, 1920x1080, 2560x1440, 3840x2160,
and 2560x1080.

The complete verifier exited `0` with `PROLOGUE_VERIFY_OK`; its retained log is
`task3-phase-a-full-gate.log`. The launcher/recorder fixtures passed but no
hands-on launcher was run and no human report was written. This resolves the
automated production-presentation defect only; first-time human comprehension
and feel remain a user-owned check.

### Task 3 Phase A Fix Round 1 — fail-safe correction (2026-08-24)

Independent review found that the original safe-edge fallback could still
return a rejected placement when every candidate was blocked. The production
contract now fails closed: no safe rectangle is a zero-area result, and the
guidance consumer clears and hides its card content instead of retaining stale
text. The focused all-blocked RED failed as expected before the change and the
post-change unit run passed with production's `8px` clearance.

The retained real route was also mutation-tested with a byte snapshot of the
placer source. The forced centered candidate route failed the named
`REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL` across all four real Boss attacks,
using actual `RoomProtectionRegionAdapter` marker bounds, then restored the
original source bytes exactly in `finally`. The restored route passed with
63/69/78/99 consecutive clear frames for sweep/leap/ground-wave/hammer throw.

Resolution capture now waits for process-driven live Boss guidance and requires
a visible nonempty `BossBody` card with the rendered instruction
`按 J 攻击，并躲避出招`; it passed in all five viewports. The full verifier
subsequently exited `0` with `PROLOGUE_VERIFY_OK`. The scoped implementation is
`fe904e7 fix: make boss guidance placement fail safe`. No human launcher was
run and no `HandsOnLauncher` evidence was created; the user-owned hands-on
check remains pending.
