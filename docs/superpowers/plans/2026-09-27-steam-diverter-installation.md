# Task17B — Steam Diverter Installation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox tracking. This is a subordinate task brief in the existing continuous-canyon plan, not a replacement goal or new independent ledger.

**Goal:** Install the reviewed reversible steam pair in both approved branches, integrate local activation/route telemetry and prove actual forward/reverse use on their existing floors.

**Architecture:** Reuse CanyonSteamDiverter and the existing static claw-control adapter through the C# content builder. Keep activation local to each mounted instance and observe it through the existing recorder. Generate only the affected leaves and their parent metadata; separate structural tests from real-input route movies.

**Tech Stack:** Existing Godot 4.7.1 Mono/C#, pinned .NET SDK, PowerShell guarded launcher, configured hardware OpenGL compatibility. No new dependency.

**Spec:** `.superpowers/sdd/2026-09-09-continuous-canyon-world/task-17-brief.md`, including current-source clarifications; `docs/production/2026-09-21-prologue-content-density.md`. The Task17A component has scoped independent approval including fix round1; its old movie is explicitly pre-fix.

## Global Constraints

- Worktree: `E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`. Preserve all dirty work. HEAD `1cf1041ad80790d25d8ab14c935b46185c4dc825` is identity, not a before-byte baseline.
- Q attack/E claw/LMB mode action/Space release-boost/RMB shield remain unchanged; no new key, cooldown, resource cost or player ability.
- Preserve original17+new5 finite actor tuples, twelve encounter IDs/arenas, safe camp and at most two effective attackers. Each frozen-upper/steam batch1 has ONE Sentry with alternate sites, not two actors.
- Preserve real floors, save format/allowlists, checkpoints, old valve/jet, camera and all route rectangles except the stated SteamValves route extension. No durable diverter state.
- No stage/commit/push/delete/install/download/paid generation/automation/memory capture. Root owns README/ledger/spec; implementer owns only the files below and its report. No worker subagents.
- Preserve frozen12 and original real-userdata baseline from Task17A. Every child requires before/finally guards, source/DLL/raw logs and actual process identity; stop on unexpected failures. Test save/settings/recorder paths must be external and configured OFF TREE.
- Recorder provenance remains enforced: current verifier authorizes only genuine HandsOnLauncher sessions and rejects headless/script/movie launches. Generated fixtures and scripted movies must NOT mint a hands-on grant, impersonate that source, force `_initialized`, or widen the verifier. Use the existing manual recorder/session seams with `AutomatedFixture` for explicitly synthetic consumer tests, and independent passive probe logs for scripted-input movies. Genuine automatic production recorder acceptance stays in the later hands-on gate; manual component/full-level tests cannot claim it.
- Historical native shutdown defects still block release. Ruling: reversible Task17B integration may proceed after Task17A approval, using reviewed local enemy/gate behavior as a provisional dependency, without declaring full Task14/16 accepted. If wrong, local integration may need rework; no public release or new memory capture is implied.

## Review Focus

1. Accepted latch versus busy/failed cast: only accepted latches set activation; entry alone does not record it. Cover in local-state and actual-recorder tests.
2. Streamed remount/reset: local side and activation reset; stale bindings cannot reactivate or double-record a retired instance. Cover generated remount/recorder tests.
3. Narrow frozen landing: actual capsule and incoming fall must remain supported without forced damage; geometry counts alone cannot pass. Cover real-input frozen route proof.
4. Steam route x3840..4200: route/guidance/metadata stay coherent while the enemy arena remains800wide. Cover exact layout/metadata tests and actual route samples.
5. Warning/nozzle readability: warning cannot look like a climbable ladder or be hidden under the floor. Cover committed-shape presentation tests and normal-size movie review; never change collisions to fit paint.

---

## Task17B: Paired installations, local telemetry and route proof

### Files and interfaces

Modify:
- `scripts/mechanisms/CanyonSteamDiverter.cs`: `[Export] public string MechanismId { get; set; } = string.Empty; public bool HasActivated { get; private set; }`. Set true only after successful TryReceiveThread; clear on ResetForCheckpoint, including fail-closed cases. Do not change selection/clock contracts.
- `scripts/prologue/ProloguePlaytestRecorder.cs`: one CanyonSteamDiverter case in TryReadMechanism using MechanismId/HasActivated. Preserve all old cases and persistence behavior.
- `tools/build/prologue/CanyonContentBuilder.cs`: `private static void AddSteamDiverter(PrologueSector root, string name, string mechanismId, Vector2 controlWorld, Vector2 leftWorld, Vector2 rightWorld, Vector2 jetSize, string supportBodyName)` plus the two calls. Convert world to sector-local once; do not restructure the builder.
- `scripts/prologue/seamless/CanyonWorldLayout.cs`: only SteamValves rectangle width800→1160.
- `tests/unit/CanyonWorldLayoutTests.cs`: only the corresponding exact expectation plus focused boundary assertions if required.
- `scripts/mechanisms/CanyonSteamDiverterVisual.cs` / `tests/integration/CanyonSteamDiverterVisualTests.cs`: only the reviewed warning/nozzle issues after a focused presentation test. Visuals remain removable/nonauthoritative.
- `tests/integration/ContinuousCanyonReadabilityTests.cs`: only exact affected metadata/readability expectations if needed; do not relax unrelated checks.

Generate through existing builders, never hand-edit:
- `scenes/prologue/sectors/CanyonDescent.tscn`
- `scenes/prologue/sectors/CanyonBasin.tscn`
- `scenes/prologue/PrologueLevel.tscn`

Create:
- `tests/integration/CanyonSteamDiverterIntegrationTests.cs`: focused `public static void RunPackedContracts()` and `public static Task RunRuntimeContracts(SceneTree tree)`; additional named methods for recorder/route/remount can live here rather than broad aggregates.
- `tests/RunTask17SteamDiverterIntegrationTests.cs`: explicit bounded modes, isolated storage validation and PID-release handshake before any fixture.
- `tests/presentation/CanyonSteamDiverterRouteProbe.cs`: `public static Task Run(SceneTree tree, string route)` with route exactly `frozen` or `trench`; literal keyboard/mouse events, passive observations.
- `.superpowers/sdd/2026-09-09-continuous-canyon-world/verify-task17b-steam-installation.ps1`: adapt the proven Task16B scene-output guards plus corrected Task17A process identity handshake/cleanup. No universal launcher framework.
- `.superpowers/sdd/2026-09-09-continuous-canyon-world/task17b-installation-report.md`: exact preparation/RED/GREEN/visual evidence, source changes and remaining gaps.

Reuse unchanged: `tests/IsolatedPrologueFixture.cs`, old builder entry scripts, reviewed Task17A tests, actual Player/ThreadTarget2D/LegacyThreadInteractionAdapter, existing recorder and route APIs. Do not modify frozen12. Unexpected shared-file requirements must be presented with a concrete failing test first.

### Exact installation contract

| Item | Frozen | Trench |
| --- | --- | --- |
| Sector | CanyonDescent | CanyonBasin |
| Controller under Gameplay/Mechanisms | SteamDiverterFrozen | SteamDiverterTrench |
| Stable local telemetry ID | canyon.v2.steam-diverter.frozen | canyon.v2.steam-diverter.trench |
| Control (world) | (1740,2500) | (3990,4070) |
| Left/right centers (world) | (1660,2540)/(1820,2540) | (3890,4110)/(4110,4110) |
| Each jet rectangle | (56,150) | (64,150) |
| Existing support | x1580..1920 top2640 | x3040..4200 top4236 |

New jets retain component default tuning and authored disabled CollisionShape2D. Left/Right children use names LeftJet/RightJet and default controller paths. Use `AddStaticControl` with name `<controller>Control`, existing22radius target and receiver relative to Gameplay/GrappleSurfaces. Visual node under GameplayReadable resolves actual control/controller with explicit NodePaths; connect pipes to credible mouths without extra terrain/targets. Default right cycles; left dormant; preserve .60s handoff/full ordinary warning.

Presentation placement clarification (source audit, not movie acceptance): the actual hazard ends25px above Frozen floor and51px above Trench floor. Keep that hazard/mouth unchanged. Add an optional visual-only `[Export] public NodePath SupportShapePath { get; set; } = new();`, borrowing the existing `Gameplay/Collision/<supportBodyName>/Collision` rectangle (`FrozenRightCatch`/`SteamTrenchFloor`). Draw narrow metal risers/socket fittings from each actual mouth down to that floor's actual top, with no broad fake landing edge. Empty/missing/unsupported support suppresses only support paint, never changes hazard paint or gameplay; do not dispose the borrowed shape. Installed visual goes under `VisualRoot/GameplayReadable/MechanismTelegraphs` at relativeZ+1, above floor skins and below actors. Replace full-width horizontal warning rungs with sparse nonconnected warning ticks or diagonal marks inside the unchanged footprint. Focused tests must verify exact floor contact, optional support removal independence and unchanged hazard corners; normal-size movie decides visual quality. No arbitrary floor-Y export or expanded steam footprint is needed.

SteamValves only changes Rect2(3040,3960,800,360)→Rect2(3040,3960,1160,360). Enemy arena remains800. Old valve body(3160,4120), actual control(3160,4040), old jet(3460,4110) remain unchanged. Candidate adjustments only after failed actual proof and within their existing floor; never thin hazard/player collision or add a platform.

**Open route premise discovered during preparation:** the1160width covers both devices but NOT the existing eight-step rejoin. Recorder and firefly currently use rectangle containment with no fallback; beyondx4200 they return null until player center reaches CounterweightLift atY<3600. Existing `canyon.v2.route.steam-valves-to-lift` supplies only launch(3560,4164,160,72) and landing(4940,3528,140,72), not a corridor. Therefore the later continuous route/guidance acceptance below cannot be claimed from this width change alone. Root must add a focused behavioral RED and bounded shared routing amendment after the initial installation slice; do not widen unrelated rectangles, introduce nearest-region guesses, or make initial worker silently change the recorder/guidance beyond current ownership. Initial structural/activation work may proceed, full Task17B route acceptance remains open. A source audit is not actual traversal proof.

### Bounded routing amendment — after presentation slice, not yet dispatched

Ruling: preserve all node rectangles/graph/encounter/camera/terrain geometry, but classify the existing steam rejoin's narrow player-center corridor as SteamValves when ordinary node containment finds nothing. Both recorder and firefly must use the same production resolver. This repairs their real membership gap without enlarging combat activation or claiming empty space as a route. Cost if wrong: revise corridor clearance after actual travel; this is not a terrain redesign or completed traversal proof.

- Put the unchanged eight `SteamRejoinStep` rectangles and `SteamRejoinTop` rectangle in immutable layout data; the builder consumes those exact rectangles instead of duplicating its current formula. Step starts are `(4200,4156)`, `(4310,4076)`, `(4420,3996)`, `(4530,3916)`, `(4640,3836)`, `(4750,3756)`, `(4860,3676)`, `(4970,3600)`, each150×40; top remains `(4940,3600,180,40)`. Names/materials/ownership stay unchanged. Full generated semantic comparison must show NO changed floor/collision/grapple surface.
- The fallback is the UNION of nine independently expanded support rectangles, not the bounding box of their endpoints. For a support `(x,y,w,40)`, player-center corridor is `(x-20,y-160,w+40,164)`. Horizontal20 matches capsule radius; top160 allows standing offset36 plus source-derived99.03 held single-jump rise at60Hz plus24.97 clearance. Bottomy+4 is a small edge tolerance, not a new floor. Normal containment takes precedence, so the actual upper rejoin belongs to CounterweightLift as soon as its existing rectangle contains the player.
- Values are an authored conservative envelope based on source, not measured movement or coverage of arbitrary claw/dash trajectories. Actual key/mouse travel may identify a needed bounded adjustment; preserve failed evidence first. Ordinary no-route space and nonfinite coordinates remain unresolved.
- Add one shared `CanyonWorldLayout.TryResolveRouteNode(Vector2, out CanyonRouteNodeId)` production method; retain the two existing private consumer wrappers with minimal delegation. No save-format change and no production test-only API. Expanded ownership, when dispatched, is layout/builder, the two resolver wrappers in recorder and `scripts/prologue/guidance/CanyonFireflyGuidanceController.cs`, focused tests and the existing runner/launcher/report. It is NOT permission to alter guidance marker eligibility, combat deferral or timing.
- Test-first: use existing test-local reflection patterns to invoke both current private resolvers with independently literal expectations. Required SteamValves positions include `(3890,4110)`, `(4110,4110)`, `(4190,4110)`, `(4210,4110)`, `(4275,4130)`, `(4385,4050)`, `(5000,3601)`; `(5000,3599)` must remain CounterweightLift. Exclude `(4210,3900)`, `(4600,3660)`, `(5500,3800)`, plus nonfinite coordinates; test exact half-open corridor boundaries. Initial expected RED must be actual existing null results at the gap, not absent new APIs.
- Then use a small isolated mounted Basin/Ascent consumer fixture: disclosed fixed-position setup, actual guidance RefreshNow and recorder ObserveOrderedRouteTelemetry with the real session; assert ordered SteamValves entry, no fabricated leave/reentry through corridor, then SteamValves exit/CounterweightLift entry. Observe actual guidance progress retention and clearing outside the corridor. A visible marker assertion requires an eligible on-screen target and elapsed stall time, not just nonnull route. This is synthetic consumer evidence, separately followed by real travel. ObserveOrderedTelemetryForTest alone is NOT a route test.

### Parent seam-safety metadata — narrow installation follow-up

Read-only source audit found that current parent `KeyInteractionBounds` omits both new controls and their jets. These rectangles only feed the stream model's96px unload-seam validation; runtime required sectors use current/neighbors/pins, and claw pins use ancestry. This is missing validation coverage, NOT proof of invisible devices or early unloading.

Ruling: after a focused missing-coverage RED, expand builder ownership ONLY to `PrologueCanyonBuildParts.CriticalInteractionBounds` and append six exact local rectangles; preserve every existing entry, then regenerate the same parent through its existing builder. Descent: control `(438,2478,44,44)`, left `(352,2465,56,150)`, right `(512,2465,56,150)`. Basin: control `(2688,448,44,44)`, left `(2578,435,64,150)`, right `(2798,435,64,150)`. They represent radius22 controls and full actual hazards, not decoration. Tests must check live metadata and packed parent coverage independently and the real stream-model seam validation, without changing unload margins or runtime residency. Cost if wrong: corrected metadata/rebuild, no expanded combat area. No source edit/run for this follow-up is granted to the visual-preparation worker yet.

### Implementation / verification sequence

Neighbor compatibility amendment: the pre-existing archive parent-metadata test expects exactly five Descent entries. After the six-entry append has its own RED/GREEN, retain a focused failure of `ArchivePressureGateIntegrationTests.ParentStreamMetadataMatchesArchiveInstallation`, then preserve its original five literals and append the three Frozen rectangles. Require exact `SequenceEqual` over all eight entries rather than weakening count/coverage. This grants only that test's expectation update after evidence, not archive runtime changes. Reuse a bounded identity-guarded mode; never launch the old full aggregate. Other inspected metadata tests have no stale literal counts.

Route-movie preparation note (source geometry only, not traversal proof): a frozen initial setup already standing at `(1605,2604)` bypasses the required incoming landing and is insufficient. Start on the prior actual `FrozenLeftCatch` (X1280..1510/top2180, standing centerY2144), then use real input to leave it and land on `FrozenRightCatch` (X1580..1920/top2640); preserve any genuinely activated upper encounter. Candidate setup X1480..1500 lies inside its activation bounds and is not an encounter-free fixture. Far-side centerX1875..1890 has little braking room. Trench candidate setup `(3300,4200)` activates the actual Laborer batch, then the real Sentry can arrive at3750 or3250 after defeat; do not assume a fixed side. Suggested pair traversal endpoints3830/4170 and control3990,4070 are source-derived candidates only. Recompute mouse aim from the actual canvas transform, observe real target/latch/support/health, and preserve failure evidence if timing or approach is unsafe.

- [x] Preserve exact pre-task bytes/hashes for every owned existing file and scenes in a unique external Task17B directory; mark new files absent. Prepare test/runner/guarded launcher ONLY, production shells limited to compile-safe missing APIs. Report a single exact RED command and expected behavioral assertions; release source/build/engine for root preflight. No first launch before root audit.
- [x] Write `PackedPairsMatchApprovedWorldGeometry`: assert both controllers exist, exact positions/size/default children/path bindings, real floors/oldvalve/actors unchanged, exactly two unique IDs, route end4200 and unchanged arena; compare generated leaves and live builder separately. Write `ActivationOnlyFollowsAcceptedLatch`: false initially/after reset, true on accepted cast, busy rejection does not create another activation, and absence from durable progress. Initial RED/GREEN retained; unchanged preexisting actors checked through full generated semantic delta, not actual encounter behavior.
- [ ] Write `RecorderObservesLocalActivationAndRemount`: isolate full level OFF TREE, verify entering routes creates no mechanism activation, accepted activation uses each exact ID, reset/remount remains default and old instance does not publish; preserve old pressure-gate/valve recorder cases. Distinguish direct receiver contract fixture from mapped-input proof. Run compile-safe RED before behavior implementation; retain failed logs.
- [ ] Implement the narrow activation/recorder and builder/layout changes. Generate Descent then Basin then parent with existing C# entry scripts through the guarded wrapper. For build-scenes only those three scene outputs may change; original real-userdata/frozen/source checks remain strict before each child and finally. Inspect pack counts and full generated delta; no unrelated regenerated sectors.
- [ ] Run focused packed/local/runtime/recorder tests to GREEN. Test actual pause/reset/remount and standalone visual removal with generated children; guard real persistence on success and failure. Run exact layout and readability tests, reviewed component regression and audited old-valve/Task14/Task16 neighbor entrypoints on final source; never invoke unsafe legacy full aggregates. Report each group separately and stop unexpected diagnostics rather than blind retry.
- [ ] Fix the recorded presentation problems test-first: warning plume must have distinct non-climbable silhouette, actual hazard footprint unchanged, credible nozzle support/pipe depth. Review full-size states with the actual floors, actors and warnings visible. No new raster/audio assets.
- [ ] Prepare then run two guarded hardware-GL route movies using literal E/LMB/A/D/Space/Q/RMB as needed: disclosed initial setup → approach/landing → actual latch → handoff → safe crossing → fresh reverse latch → safe return. At least one includes real branch combat and fair Sentry arrival before the control. After setup no HP/position/velocity overrides, API kills, fake unlocks, hidden resets or disabled actors. Sample real route/guidance through x3840..4200 and at actual rejoin. Stop and diagnose failed routes; keep evidence. FixedFPS/silent movie is not realtime/audio/human proof.
- [ ] Freeze scoped full-context diff against true before bytes, source/scene/binary/evidence manifests and report; release all owned processes. Root reviews motion/full-size frames and dispatches one independent spec+quality reviewer. No completion claim until both placements and their actual routes are proved. Keep full-game/native/Windows release gates open.

### Plan self-review — root

Spec coverage: component contracts1–7 carried through unchanged neighbors; contract8, both placements, route extension, actors, forward/reverse and two movies are explicit. Known visuals are included without new art scope. Exact APIs match current source or are declared above. Each Review Focus has a named test/proof. Source sharing: controller→recorder uses MechanismId/HasActivated; builder→packed tests uses the named nodes/table; layout→metadata/tests uses the single1160width; visuals→movies use committed shapes, never collision authority. One cohesive integration task, no competing writers. Root preflight gates are process safety checks, not repeated user design approvals.
