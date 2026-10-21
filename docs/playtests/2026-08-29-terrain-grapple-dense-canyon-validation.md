# Terrain Grapple Dense Canyon — Task 6 Validation

> **Certification status — invalidated on 2026-09-01.** The earlier capture and both
> verifier passes below are historical only: subsequent runtime/capture changes invalidated
> their fingerprint. They must not be presented as current Task 6 certification. The human
> acceptance gate remains open.

## Invalidated-capture visual audit — 2026-09-01

Before the next BASE run or recording, all 46 decoded PNGs from the invalidated bundle
`artifacts/task6-final-capture-postdraw-20260901-220833` were opened individually at their
native 1280×720 resolution. The bundle is intentionally retained as RED evidence only:
full-flow AVI `62a1ff31e4aa455e66537bcd8e5b995ad4c02980270faca0b3578fa796e58ac0`
(4,033 frames) and Boss-retry AVI
`6a1c5a86ac7ea82dfa08448cfa388ad1f3d6775825413dabbeee2d6564d68dd0`
(2,719 frames).

The audit found no empty or frozen movie, but did find stale frame bindings. `mode_hud` names
AVI 78, which is a gray transition (AVI 79 is the first readable HUD); `completion` names AVI
2772, which is still the Boss HUD (AVI 2773 is the first completion card). The transient
boost and Boss markers do not yet record a post-draw/Movie-Maker conversion, and adjacent-frame
inspection shows that several visible effects first appear one frame after their named marker.
This is a binding defect, not a license to relabel an old frame. The next candidate must emit
post-draw markers, decode their exact frames, and pixel-check the named visual states.

| # | Native PNG / AVI frame | Visual conclusion | Audit status |
| ---: | --- | --- | --- |
| 1 | `01-title_start` / 36 | Readable title menu; no blank or teardown frame. | pass |
| 2 | `02-mode_hud` / 78 | Uniform gray transition; no readable HUD, player, or interaction state. | **fail — stale binding** |
| 3 | `03-attack_energy` / 79 | First readable gameplay HUD, player, enemy, and prompt. | pass |
| 4 | `04-recovery` / 93 | Live recovery/gameplay scene. | pass |
| 5 | `05-canyon_descent` / 367 | Player descent, platforms, and HUD are visible. | pass |
| 6 | `06-grapple_reticles` / 381 | Grapple target/reticle and canyon layout are visible. | pass |
| 7 | `07-canyon_basin` / 711 | Basin traversal state is readable. | pass |
| 8 | `08-checkpoint_basin` / 834 | Basin checkpoint and route are readable. | pass |
| 9 | `09-shield_overheat` / 1054 | Shield/overheat UI and actor are visible. | pass |
| 10 | `10-ascent_one` / 1731 | First vertical rope attachment is visible. | pass |
| 11 | `11-ascent_two` / 1868 | Second vertical rope attachment is visible. | pass |
| 12 | `12-ascent_three` / 2040 | Third vertical rope attachment is visible. | pass |
| 13 | `13-optional_alcove` / 2301 | Optional alcove geometry and player route are visible. | pass |
| 14 | `14-lost_temple` / 2319 | Temple approach/reticle scene is readable. | pass |
| 15 | `15-checkpoint_temple` / 2562 | Temple checkpoint state is visible. | pass |
| 16 | `16-boss_intro` / 2629 | Boss, arena, health bar, and intro framing are visible. | pass |
| 17 | `17-death_retry` / 2631 | Live Boss arena/retry state is visible. | pass |
| 18 | `18-completion` / 2772 | Boss HUD remains; completion card is absent. AVI 2773/2774 show the actual card. | **fail — stale binding** |
| 19 | `19-boost_attached` / 2858 | Character, cast line, ceiling target, and attachment are visible. | pass visual; needs post-draw binding |
| 20 | `20-boost_start` / 2859 | Mostly pre-motion attached pose; first clear motion is AVI 2860. | **fail — stale transient binding** |
| 21 | `21-boost_start__previous` / 2858 | Valid pre-motion neighbour for the boost transition. | supporting evidence |
| 22 | `22-boost_start__next` / 2860 | First visibly moved boost frame. | supporting evidence |
| 23 | `23-attached_boost_consumed` / 2861 | Live boosted attachment and rope are visible. | pass visual; needs post-draw binding |
| 24 | `24-boost_arc` / 2862 | Player/rope movement is visibly in progress. | pass visual; needs post-draw binding |
| 25 | `25-boost_arc__previous` / 2861 | Valid arc neighbour with different pose. | supporting evidence |
| 26 | `26-boost_arc__next` / 2863 | Valid arc neighbour with different pose. | supporting evidence |
| 27 | `27-boost_second_rejected` / 2864 | Cyan rope remains; rejection ring is absent. | **fail — stale transient binding** |
| 28 | `28-boost_second_rejected__previous` / 2863 | Valid pre-rejection neighbour. | supporting evidence |
| 29 | `29-boost_second_rejected__next` / 2865 | Purple rejection ring appears and cyan rope has released. | supporting evidence / target raster |
| 30 | `30-boost_land` / 2896 | Player has landed; no rope is present. | pass visual; needs post-draw binding |
| 31 | `31-boost_land__previous` / 2895 | Valid landing neighbour. | supporting evidence |
| 32 | `32-boost_land__next` / 2897 | Valid settled landing neighbour. | supporting evidence |
| 33 | `33-boss_sweep` / 3245 | Boss sweep pose is readable, but marker has no post-draw visual binder. | needs binding |
| 34 | `34-boss_leap_slam` / 3350 | Pre-effect pose; warning ring first becomes visible at AVI 3351. | **fail — stale Boss binder** |
| 35 | `35-boss_ground_wave` / 3464 | Boss is visible but no independently legible ground-wave telegraph is bound. | **fail — ambiguous/unbound Boss state** |
| 36 | `36-boss_hammer_throw` / 3891 | Hammer pose is visible; horizontal throw line first appears at AVI 3892. | **fail — stale Boss binder** |
| 37 | `37-boss_attack_contract` / 3909 | Distinct phase-two contract/long horizontal tether is readable. | pass |
| 38 | `38-completion_title_return` / 3951 | Readable title-menu return. | pass |
| 39 | `01-Attack` / 2923 | Attack-mode reticle/HUD is visible. | pass |
| 40 | `02-Grippable` / 2985 | Cyan diamond target and rope are visible. | pass |
| 41 | `03-Ice` / 3042 | Ice target/reticle is visibly distinct. | pass |
| 42 | `04-Forbidden` / 3092 | Forbidden target reticle is visibly distinct. | pass |
| 43 | `05-Obstructed` / 3116 | Obstructed target and intervening scene are visible. | pass |
| 44 | `06-OutOfRange` / 3137 | Distant out-of-range reticle is visible. | pass |
| 45 | `boss-retry/01-route_started` / 2 | Normal route starts with live player and HUD. | pass |
| 46 | `boss-retry/02-completed_real_retry` / 2696 | Completion card after physical retry is readable. | pass |

Adjacent diagnostic rasters are retained under
`artifacts/task6-final-capture-postdraw-20260901-220833/visual-audit-adjacent-20260901`.
They confirm the gray `77–78 → 79` transition and the one-frame visual onset for the
completion, boost rejection, Leap Slam warning, and Hammer Throw line. They are not
certification evidence and will not be committed.

## Automated evidence status

This document records only automated evidence produced from the isolated `e4e06989af73b4b98a61fbf40d3f103e1888fdcd` plus Task 6 candidate. Capture hashes, frame counts, and per-keyframe visual conclusions are added after the final MJPEG files are extracted and manually inspected.

The automated contract requires the real InputMap traversal, five sectors, 15 micro-scenes, four claw roles, the attached-Space boost contract, two checkpoints, physical Boss retry, and fall-context version 3. It rejects missing markers, an empty or frozen capture, wrong 1280x720/60fps specifications, fatal/error/leak output, or callback work at or above 50ms.

## Human acceptance gate — open

Human acceptance remains open. No automatic HandsOnLauncher report, no user qualitative note, and no substituted human evaluation is created by this Task 6 automation. Step 8 has not started; a user must play the normal-speed build and supply any qualitative acceptance separately.


## Superseded historical automated capture record — not current certification

The final movie bundle was produced in the isolated Task 6 materialization with the preserved
compatible user overlay. Its runtime candidate fingerprint is
`9FC8BDAE4B2947D834AF6ECA4981E538C29F5911BF2A98A27483B3FFF8624D9A`; it binds the
executable, tests, capture/verifier scripts, and six generated traversal scenes before and after
recording. Post-capture documentation is intentionally outside that runtime hash.

- Capture root: `artifacts/task6-final-evidence/capture-final-working-20260901T121429023Z`
- Capture manifest SHA-256: `062E760BF5E61087561B5190249C042F939035A7B2543EC095F80A7C7F8A42F0`
- Driver/specification: Windows, fixed 60 FPS, 1280x720, MJPEG AVI.
- Focused-audit peak callback time: 6.201 ms (< 50 ms); audit-manifest SHA-256:
  `84CB6702976C74FA194AF82AC2AC63CAB10503B9A9A0A598A2DF5AAC107B4357`.
- Audit topology: five sectors, 15 micro-scenes, exactly four claw roles; no fatal/error/leak
  line was accepted by the capture gate.

| Evidence | SHA-256 | Frames | Keyframe-manifest SHA-256 |
| --- | --- | ---: | --- |
| Full flow AVI | `9E3C73AC42DAE516653AE292D011F5AA7A3C6A7CE4434B3A020E8F77C5FB13AD` | 11,443 | `F977AA9AE98088BE4C25696716589AB0306B3C3460EDD516FC5AABA0D51FA760` |
| Full-flow reticle manifest | same full-flow AVI | 11,443 | `3CDA77620453B19327AA16B53932D669CBCD446FFC3885B95EC8F9234EFED684` |
| Boss-retry AVI | `90F38361846E494532679284CCBF5683E40F64CE57092D34585DF4B7E6494C81` | 2,719 | `6509775F406DF15C62AE728720E3E0054517C1975F80D8EA3578C8469FD015A0` |

The full-flow log contains one each of the terrain/620-range, retained-cast attached boost,
15-micro-scene, v3 fall-context, four catch-return, four claw-role, two-checkpoint and
TASK6_INPUT_VIEWPORT_SAMPLE_OK sampled=(1120, 620) markers. It also records all six reticle
dwells, 28 stages, Boss Sweep/Leap Slam/Ground Wave/Hammer Throw, death/retry/completion and
title return. The separate Boss AVI records completed_real_retry at frame 2696.

## Original-resolution keyframe review

All 36 PNGs below were opened individually at original resolution. Each is nonblank and
nonfrozen, and visibly shows the named route/HUD/traversal/presentation/completion state.

### Full-flow keyframes

| PNG / AVI frame | Visual conclusion |
| --- | --- |
| 01-title_start / 36 | Title menu rendered normally; no teardown/blank frame. |
| 02-boost_start / 7621 | Player visibly attached to ceiling target with cast line. |
| 03-boost_arc / 7623 | Attached boost arc/rope visible in the same traversal setup. |
| 04-boost_second_rejected / 7626 | Attached state remains readable after second-boost rejection. |
| 05-boost_land / 7633 | Player landed beneath the released ceiling target. |
| 06-mode_hud / 7897 | Entrance HUD/mode prompt, player, enemy and target visible. |
| 07-attack_energy / 7898 | Attack-mode encounter framing and energy HUD visible. |
| 08-recovery / 7911 | Recovery/checkpoint state presented, not a frozen transition. |
| 09-canyon_descent / 8185 | Airborne descent traversal, platforms and live HUD visible. |
| 10-grapple_reticles / 8199 | Canyon reticle and grapple-mode HUD visible. |
| 11-canyon_basin / 8529 | Basin traversal/shield lesson layout visible. |
| 12-checkpoint_basin / 8652 | Basin checkpoint lantern and active route visible. |
| 13-shield_overheat / 8872 | Shield-overheat UI state visibly signalled. |
| 14-ascent_one / 9549 | First vertical ascent attachment/rope visible. |
| 15-ascent_two / 9686 | Second vertical ascent attachment/rope visible. |
| 16-ascent_three / 9858 | Third ascent attachment/rope and higher platform visible. |
| 17-optional_alcove / 10119 | Optional alcove geometry and player route visible. |
| 18-lost_temple / 10137 | Temple mechanism approach, objective and reticle visible. |
| 19-checkpoint_temple / 10380 | Temple checkpoint/gate scene visibly active. |
| 20-boss_intro / 10447 | Fallen Bellkeeper, health bar and arena intro visible. |
| 21-death_retry / 10449 | Retry prompt overlays live Boss arena, not gray teardown. |
| 22-completion / 10590 | Chapter-completion card visible and legible. |
| 23-boss_sweep / 10695 | Boss sweep attack pose/telegraph visible. |
| 24-boss_leap_slam / 10800 | Boss leap-slam framing and player warning effect visible. |
| 25-boss_ground_wave / 10914 | Ground-wave combat beat visibly captured in arena. |
| 26-boss_hammer_throw / 11340 | Hammer-throw line/phase-two combat beat visible. |
| 27-boss_attack_contract / 11358 | Boss phase-two attack-contract state visible. |
| 28-completion_title_return / 11400 | Title-menu return visible and distinct from completion. |

### Six reticle dwell keyframes

| PNG / AVI frame | Visual conclusion |
| --- | --- |
| 01-Attack / 7660 | Attack reticle/mode state visible. |
| 02-Grippable / 7722 | Cyan diamond/rope visibly marks a grippable target. |
| 03-Ice / 7779 | Ice-target reticle visibly distinct from grippable state. |
| 04-Forbidden / 7829 | Forbidden reticle visibly distinct in temple scene. |
| 05-Obstructed / 7853 | Obstructed target reticle and intervening layout visible. |
| 06-OutOfRange / 7874 | Out-of-range reticle visibly distinct at distance. |

### Boss-retry keyframes

| PNG / AVI frame | Visual conclusion |
| --- | --- |
| 01-route_started / 2 | Normal canyon route begins with live HUD and player. |
| 02-completed_real_retry / 2696 | Completion card rendered after physical Boss retry; no gray teardown. |

## 2026-09-02 current runtime-bound capture — certification evidence

This section supersedes every historical/invalidated capture reference above. It documents the
current automated candidate only; it is not a human playtest and does not close the human gate.
The candidate is BASE `e4e06989af73b4b98a61fbf40d3f103e1888fdcd` plus the final Task 6 runtime
fingerprint `49A6E23D6782668F04EE8061E3559E2B377765EBFF0ABCAB6E3D46A97AFD612D`.

- Capture root: `artifacts/task6-final-capture-20260902T005609288`
- Driver/specification: Windows display-backed recording, fixed 60 FPS, 1280×720, MJPEG AVI;
  focused callback peak 6.614 ms (<50 ms), five sectors, 15 micro-scenes and four claw roles.
- Capture-manifest SHA-256:
  `9B3FB1B3126D27C8254F91BC4A6A0DE54C13C757075A74B26FD8D8AC9A2AACC4`.
- Full-flow AVI: `full-flow.avi`, SHA-256
  `6C6E5046F0653BD02C366D8CF0C8119FB5CF2971DC83B51068BA2DA5A62B48E5`, 4,053 frames;
  keyframe-manifest SHA-256
  `351921FACDC603B277C872A653EEF34B988FCCD31711B8E75CA7007AA5EB805D`; reticle-manifest
  SHA-256 `52AC04E88447A337DF674023AC9DEFDAB63774EB5F375D1121B5BDD85CAE79AA`.
- Boss-retry AVI: `boss-retry.avi`, SHA-256
  `FDF14343CF0A5C2755B076597DF2BDB5C526B77B64382A56959173409F0F261C`, 2,719 frames;
  keyframe-manifest SHA-256
  `C93E1805E38B546B12E42F91392155B41465CB904AB03721668475CE796EBF18`.

The timeline gate found 30 ordered stages, 4,053 total frames, an 84-frame title tail, and six
separate reticle dwells. Boost start/arc/rejection/land use independent post-draw frames with
different adjacent-frame sample hashes and pixels; no pause or repeated-static-frame scheme is
used to manufacture motion. The capture contract also binds its markers to the logs, movie hash,
and manifest, rather than accepting source-text marker strings.

### Original-resolution keyframe review — all 46 exports

I opened each PNG listed below at its native 1280×720 resolution. The result is visual evidence
only: each named picture is readable, nonblank, and not a frozen/gray teardown frame. The full
movie's 30 sampled stages are globally distinct (`changed_transitions=29`); the separate
Boss-retry movie also has a changing transition. This does not substitute for a user's
normal-speed qualitative playthrough.

| # | AVI frame / PNG | Original-resolution visual conclusion |
| ---: | --- | --- |
| 1 | 36 / `01-title_start.png` | Readable title screen with normal background and menu; no blank/teardown state. |
| 2 | 81 / `02-mode_hud.png` | Live entry scene: player, formal HUD, prompt, and target are all visible. |
| 3 | 82 / `03-attack_energy.png` | Attack-mode combat framing and energy HUD are visible. |
| 4 | 94 / `04-recovery.png` | Recovery state is readable in a live world view, not a transition frame. |
| 5 | 368 / `05-canyon_descent.png` | Airborne descent, platforms, player and HUD are visible. |
| 6 | 382 / `06-grapple_reticles.png` | Grapple target/reticle and canyon geometry are visible together. |
| 7 | 712 / `07-canyon_basin.png` | Basin traversal layout and active player/HUD view are visible. |
| 8 | 835 / `08-checkpoint_basin.png` | Basin checkpoint lantern/route state is readable. |
| 9 | 1055 / `09-shield_overheat.png` | Shield-overheat feedback is visibly signalled with the active scene. |
| 10 | 1732 / `10-ascent_one.png` | First vertical ascent has an attached rope/player silhouette. |
| 11 | 1869 / `11-ascent_two.png` | Second ascent anchor and elevated route are visible. |
| 12 | 2041 / `12-ascent_three.png` | Third ascent anchor/player/upper platform are visible. |
| 13 | 2302 / `13-optional_alcove.png` | Optional alcove geometry and the traversable side route are visible. |
| 14 | 2320 / `14-lost_temple.png` | Lost Temple approach shows its mechanism setting and live reticle. |
| 15 | 2563 / `15-checkpoint_temple.png` | Temple checkpoint is visible and the route remains live. |
| 16 | 2630 / `16-boss_intro.png` | Fallen Bellkeeper, arena, health bar and Boss introduction are readable. |
| 17 | 2632 / `17-death_retry.png` | Live Boss death/retry prompt overlays the arena; it is not a gray exit frame. |
| 18 | 2774 / `18-completion.png` | Completion card is fully visible and readable. |
| 19 | 2857 / `19-boost_attached.png` | Ceiling attachment, player, cast/rope and target are visible before boost. |
| 20 | 2863 / `20-boost_start.png` | First boost-motion pose is visible; neighbor frames differ. |
| 21 | 2862 / `21-boost_start__previous.png` | Pre-start attached pose is visible and supports the start transition. |
| 22 | 2864 / `22-boost_start__next.png` | Moved boost pose is visibly distinct from the start frame. |
| 23 | 2865 / `23-attached_boost_consumed.png` | Consumed attached boost shows a live elevated player and cast. |
| 24 | 2866 / `24-boost_arc.png` | Player/rope are visibly in the boost arc. |
| 25 | 2865 / `25-boost_arc__previous.png` | Arc predecessor differs in player/rope pose. |
| 26 | 2867 / `26-boost_arc__next.png` | Arc successor differs in player/rope pose. |
| 27 | 2869 / `27-boost_second_rejected.png` | Purple rejection ring is visible; cyan cast has released, proving the second boost was rejected. |
| 28 | 2868 / `28-boost_second_rejected__previous.png` | Pre-rejection arc/pose is visible and differs from the rejection frame. |
| 29 | 2870 / `29-boost_second_rejected__next.png` | Post-rejection frame remains visibly different and readable. |
| 30 | 2902 / `30-boost_land.png` | Player has landed below the released ceiling target. |
| 31 | 2901 / `31-boost_land__previous.png` | Landing predecessor shows the approach to support. |
| 32 | 2903 / `32-boost_land__next.png` | Settled supported player pose follows landing. |
| 33 | 3271 / `33-boss_sweep.png` | Boss sweep attack pose/telegraph is independently readable. |
| 34 | 3376 / `34-boss_leap_slam.png` | Leap-slam warning/attack beat is independently visible. |
| 35 | 3490 / `35-boss_ground_wave.png` | Ground-wave attack effect and Boss arena are independently readable. |
| 36 | 3908 / `36-boss_hammer_throw.png` | Hammer-throw line/attack beat is independently visible. |
| 37 | 3927 / `37-boss_attack_contract.png` | Late Boss attack contract/tether framing is visible and distinct. |
| 38 | 3969 / `38-completion_title_return.png` | Return to the title screen is visible and distinct from the completion card. |
| 39 | 2928 / `01-Attack.png` | Attack reticle/mode is visibly distinct. |
| 40 | 2990 / `02-Grippable.png` | Cyan grippable target/reticle is visibly distinct. |
| 41 | 3047 / `03-Ice.png` | Ice rejection reticle is visibly distinct from grippable. |
| 42 | 3097 / `04-Forbidden.png` | Forbidden red reticle is visibly distinct. |
| 43 | 3121 / `05-Obstructed.png` | Obstructed reticle and intervening geometry are visibly distinct. |
| 44 | 3142 / `06-OutOfRange.png` | Out-of-range reticle is visibly distinct at distance. |
| 45 | 2 / `boss-retry/01-route_started.png` | Boss-retry movie begins on a live canyon route with player and HUD. |
| 46 | 2696 / `boss-retry/02-completed_real_retry.png` | Completion card appears after the physical Boss retry; no gray teardown. |

Two fresh formal verifiers subsequently accepted this exact runtime candidate:
`artifacts/task6-final-verify-1-20260902T010601067` and
`artifacts/task6-final-verify-2-20260902T012103825`. Each contains exactly one
`PROLOGUE_VERIFY_OK`, zero build warnings/errors, no fatal/error/leak line, the same restored
scene SHA `9C81ABD51F8C834295AF03FD7F2C8C4088AD2640B7C809ED392235C0E0FD7270`, and the same verifier
summary SHA `BA94961F9965D0A2FE7191B08EA7B3F7947D244C91F3254A147DE5C5C5B0C9AA`.

## Human acceptance remains open

Step 8 is deliberately not started. No `HandsOnLauncher` report, user qualitative note, or
automatic substitute was created. The remaining gate is a user-run normal-speed playthrough
with real physical mouse/keyboard input and the user's own observations.
