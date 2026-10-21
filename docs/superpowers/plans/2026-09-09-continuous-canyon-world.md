# Continuous Canyon World Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the short linear graybox with one continuous first-act canyon containing twelve mechanically distinct challenge spaces, at least six functioning mechanism families, eight main-route plus four branch encounters and one Boss, while preserving a single player/world coordinate space, safe grapple streaming, explicit save migration, and a genuinely readable full route.

**Architecture:** Keep the five existing `PrologueSectorId` values as stable streaming chunks, but replace enum-order `Previous/Next` assumptions with an explicit symmetric sector graph and a separate route-node graph. `CanyonWorldLayout` becomes the authoritative source for the 7680×4320 design envelope, route topology, challenge identity, checkpoints, discoveries, shortcuts, camera focus and playable/recovery regions. Generated collision and mechanism nodes remain gameplay authority; visual layers are replaceable consumers. The world consumes the claw-owned residency interface without adding sector knowledge to `MechanicalClawController`, and layout-v2 progress is stored beside—not over—the legacy save until the player chooses a migration outcome.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, generated `.tscn` scenes, Godot 2D physics, pure C# graph/timing models, existing project-owned environment/enemy/audio assets, focused headless tests followed by normal-window route and human playtesting.

**Spec:** `docs/superpowers/specs/2026-09-09-continuous-canyon-auto-pull-design.md` v2, including its requirement for at least ten play-mechanically distinct spaces (this plan authors twelve), a 6-column × 6-level initial envelope, a controlled 4–6 second abyss descent, at least six functioning mechanism families, and an 8-main/4-branch normal-encounter layout target.

**User-requested content extension, 2026-09-21:** enemy types and numbers, mechanism types and numbers, and scene density must increase; the user explicitly describes the current result as empty/monotonous. `docs/production/2026-09-21-prologue-content-density.md` records the verified3-types/17-spawns/6-core-installations baseline and the first5-types/22-spawns/8-core-families/12-installations increment with intro-ease, source, visual and safety boundaries. Keep the12encounter-group IDs and existing17spawns; new content needs concrete separate owner briefs and tests before integration. This is an additional prerequisite before final Task10 route acceptance/Task11 freeze, not permission to declare older count/route evidence sufficient. Previously accepted Tasks1–9 remain historical checkpoints, not completion of this new extension. First preserve/review the current bounded GearEntry correction; then implement the new batches and rerun full routes. No new paid art, automation or publishing authority.

**Difficulty clarification, 2026-09-23:** the user permits a moderate increase in first-level difficulty. Preserve learnability at the entrance, then develop mid/late challenge through readable positional, shield, claw and mechanism combinations. Do not interpret introductory ease or initially serial validation batches as a requirement that every encounter stay isolated forever. Composition changes require supported retreat space, actual-input combat proof and the existing maximum-two-effective-attackers guard; the authorization does not justify HP inflation, surprise spawn contact or removing full warning windows. Do not blindly alter the separately specified Task12/13 timings before integrated balance evidence.

## Global Constraints

**2026-10-02 measured Split Moth placement revision:** Normal-input case `normal-main-split-supported-20261002T101701205-bc10b76a7e1f405690b1d6c3cfe006a2` proved the original batch1 Moth physically trapped below Grip9 while player stood supported above. Under the user's authorized map repairs, relocate only this Moth from(4740,1700) to(4300,1280), preserving IDs,22actors,other21placements,batches,tuning,platforms and>=140px vertical separation. Update exactly its authored tuple and add clearance regression, not weaken all original17 checks. The historical frozen12 manifest3D68DAB616995553F551FA9157F2CC9419599CC3339C420F2102892EACFAFCCC and old launchers/cases remain immutable. Explicit stage1 revision changes only catalog-test hash; stage2 revision changes exactly catalog-test and production-catalog hashes. New validation launchers must pin each complete revision and whitelist exact changed rows, retain all12 paths and every other10 row, and guard all12 files before/after; never claim a revised run matched the old baseline. No behavioral RED from the initial guard rejection; run actual catalog regression, then full normal routes. Task14/native stability and final acceptance remain open. See task10-split-moth-gallery-placement-brief-20261002.md and ledger for exact snapshots/hashes. Cost if wrong: bounded failed runs and placement rework; original version and saves remain recoverable.

**2026-09-23 execution sequencing ruling, updated after component review:** Task14 formal placement acceptance is still blocked by an unidentified intermittent Godot/.NET disposal FATAL; completed negative diagnostic observations do not resolve it. Task16A has scoped component regression, input-movie and ownership-review evidence. Task16B may now proceed with reversible local Archive installation/reward/save/recorder/route implementation under `.superpowers/sdd/2026-09-09-continuous-canyon-world/task-16b-brief.md`, with exact pre-task source preservation, root-preflighted hermetic fixtures, unchanged frozen12 files and stop-on-unexpected-failure handling. This supersedes the earlier Task14-acceptance-before-16B-implementation ordering, not the release gate. Native stability, full Task16 and complete prologue acceptance requirements remain open and unchanged; possible integration rework is the cost of this sequencing decision.

- Implement only on P0 commit `71c26b2` or a descendant after its `SeamlessPrologueWorld` / `PrologueGameFlowController` / `PrologueCompletionController` focused regressions are green. Resolve this plan against that baseline; do not overwrite or replay pre-P0 versions of those files.
- Implement only after Task 5 of the auto-pull claw plan exposes the exact residency contract. The world may consume that contract but must not edit `MechanicalClawController`, define another residency event, or introduce `PrologueSectorId` into the abilities layer.
- Preserve the existing five serialized `PrologueSectorId` values. Internal route nodes may be added, but they must not renumber save-visible sectors.
- The initial design envelope is exactly `Rect2(0, 0, 7680, 4320)`. This is a layout bound, not a requirement to fill 36 rooms. The controlled abyss may use the increased vertical budget, but it must offer side-wall braking, intermediate exits and a safe bottom; it must not force idle waiting.
- “Twelve challenge spaces” means twelve different interaction contracts, not twelve names, background swaps or cosmetic variants of one jump. The authoritative kinds are `LowCeilingBridge`, `StaggeredRift`, `ControlledAbyss`, `ArchiveSideCave`, `FrozenDetour`, `GearPistons`, `SteamValves`, `CounterweightLift`, `PendulumGallery`, `SplitLevelEncounter`, `SealedCourtyard`, and `BossHall`.
- Keep the normal-encounter layout target at eight main-route groups plus four branch groups, with one separate Boss. A group may contain one or two validated project enemies (including the separately tested new types in the2026-09-21content extension), but at most two normal enemies may be in effective attack range at once. Add spacing/activation boundaries and rest beats; do not inflate health or spawn a crowd to manufacture duration.
- Main-route traversal must not require W/S reel, ordinary double jump, air dash, blind off-screen targeting or a precision-only chain. Auto-pull uses real thick `GripStone`/`TempleMetal` collision surfaces; ice and sealed faces remain explicit rejection materials.
- Reuse existing project-owned atlases, room-layer data, enemy scenes and audio. Do not generate, buy or download art. Any later paid generation remains a separate user-approved action.
- Do not overwrite `E:/OurGame/Knight/.build-deliveries/BasinSample-2026.09.09-dev.b98cc47`, its ZIP, or the legacy save. New evidence and candidates receive new immutable identifiers.
- The user explicitly cancelled recurring automation; do not create or restore a heartbeat/cron task for this plan. Progress is driven by the active goal and explicit implementation turns.
- Automated tests prove contracts and regressions, not fun, visual quality, the 4–6 second felt descent, or 20–30/30–45 minute playtime. Those timing figures are human-test targets and may be exceeded when added content is meaningful.
- Use the bundled SDK at `G:/godot/dotnet-sdk` for builds and the actual GUI binary `G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe` for engine verification. Build leaf sector scenes before the parent level scene.
- Never launch Godot after a failed build. For every engine test or build-script invocation, create a unique external case directory under `E:/OurGame/Knight/.evidence/september09-continuous-canyon`, start the actual `.exe` with `Start-Process -WindowStyle Hidden -PassThru`, call `WaitForExit()`, and inspect that process's `ExitCode`; a console wrapper or parent-shell exit code is not evidence. Use this template, replacing only `$caseName` and `$godotArgs`:

```powershell
$taskDotnetRoot = 'G:/godot/dotnet-sdk'
$env:DOTNET_ROOT = $taskDotnetRoot
$env:DOTNET_ROOT_X64 = $taskDotnetRoot
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:PATH = "$taskDotnetRoot;$env:PATH"
$caseName = 'task1-layout-red'
$runId = '{0}-{1}-{2}' -f $caseName,(Get-Date -Format 'yyyyMMddTHHmmssfff'),([Guid]::NewGuid().ToString('N'))
$caseDirectory = Join-Path 'E:/OurGame/Knight/.evidence/september09-continuous-canyon' $runId
New-Item -ItemType Directory -Path $caseDirectory | Out-Null
& "$taskDotnetRoot/dotnet.exe" build ./Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo 2>&1 |
    Tee-Object -FilePath (Join-Path $caseDirectory 'dotnet-build.log')
if ($LASTEXITCODE -ne 0) { throw 'Build failed; Godot was not started.' }
$godotArgs = @('--headless','--path','.', '--script','res://tests/RunContinuousCanyonTests.cs',
    '--log-file',(Join-Path $caseDirectory 'godot.log'))
$process = Start-Process -FilePath 'G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe' -ArgumentList $godotArgs -WindowStyle Hidden -PassThru
$process.WaitForExit()
$exitCode = $process.ExitCode
[IO.File]::WriteAllText((Join-Path $caseDirectory 'actual-exit-code.txt'),
    $exitCode.ToString([Globalization.CultureInfo]::InvariantCulture))
```

- `tests/RunPrologueTests.cs` already has unrelated in-flight edits. Preserve them, re-read the merged runner immediately before any eventual append, and never replace it from an older revision. The dedicated `RunContinuousCanyonTests.cs` owns focused work until Task 10 integrates the final suite.
- Across every task, a compiler failure caused by a deliberately missing planned type/member is only interface evidence, not the behavioral RED. Capture it, add the smallest compile-safe type/member shells with inert behavior, rebuild successfully, and then run the named test through the actual waited Godot process to obtain a nonzero behavioral RED. Only after that RED may the production behavior be implemented and driven GREEN.

---

### Task 1: Freeze the Layout-v2 Graph and Challenge Contract

**Files:**
- Create: `scripts/prologue/seamless/CanyonWorldLayout.cs`
- Create: `scripts/prologue/seamless/CanyonChallengeKind.cs`
- Create: `scripts/prologue/seamless/CanyonRouteNodeId.cs`
- Create: `scripts/prologue/seamless/CanyonRouteGraph.cs`
- Modify: `scripts/prologue/seamless/PrologueSectorDefinition.cs`
- Modify: `scripts/prologue/seamless/PrologueSectorStreamModel.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `scenes/prologue/PrologueLevel.tscn`
- Create: `tests/unit/CanyonWorldLayoutTests.cs`
- Modify: `tests/unit/PrologueSectorStreamModelTests.cs`
- Create: `tests/RunContinuousCanyonTests.cs`

**Interfaces:**

```csharp
public enum CanyonRouteNodeId
{
    LowCeilingBridge, StaggeredRift, ControlledAbyss, ArchiveSideCave,
    FrozenDetour, BasinCamp, GearPistons, SteamValves, CounterweightLift,
    PendulumGallery, SplitLevelEncounter, SealedCourtyard, BossHall, SafeExit
}

public enum CanyonChallengeKind
{
    LowCeilingBridge, StaggeredRift, ControlledAbyss, ArchiveSideCave,
    FrozenDetour, GearPistons, SteamValves, CounterweightLift,
    PendulumGallery, SplitLevelEncounter, SealedCourtyard, BossHall
}

public enum CanyonRouteGate { None, ShortcutOpen, MechanismActivated, BossDefeated }

public sealed record CanyonRouteNodeDefinition(
    CanyonRouteNodeId Id, PrologueSectorId SectorId, Rect2 WorldBounds,
    CanyonChallengeKind? ChallengeKind, bool Optional, bool RestArea,
    string StableDiscoveryId = "");

public sealed record CanyonRouteEdge(
    CanyonRouteNodeId From, CanyonRouteNodeId To, bool Bidirectional,
    CanyonRouteGate Gate = CanyonRouteGate.None,
    string StableShortcutId = "",
    string TraversalSegmentId = "",
    string RequiredMechanismId = "",
    string ReverseTraversalSegmentId = "");
```

`CanyonWorldLayout` must expose `CurrentLayoutVersion = 2`, `DesignBounds`, `SectorBounds`, `Nodes`, `Edges`, `CheckpointWorldPositions`, `StableCheckpointIds`, `StableShortcutIds`, `StableMechanismIds`, `StableDiscoveryIds`, and `Validate()`. Mechanism IDs include the persistent completed-seal ID `canyon.v2.courtyard-seal-open`, distinct from its individual prerequisite objective. Use these exact chunk rectangles, which form a six-by-six-screen U without interior overlap:

| Sector | World rectangle |
| --- | --- |
| `CanyonEntrance` | `(0, 0, 1280, 720)` |
| `CanyonDescent` | `(1280, 0, 1280, 3600)` |
| `CanyonBasin` | `(1280, 3600, 3840, 720)` |
| `CanyonAscent` | `(3840, 720, 1280, 2880)` |
| `LostTemple` | `(5120, 720, 2560, 720)` |

**Authoritative world-space challenge bounds:**

| Challenge/support node | Sector | World bounds | Contract |
| --- | --- | --- | --- |
| `LowCeilingBridge` | Entrance | `(0,0,1280,720)` | low roof blocks a blind arc; visible wall/underside auto-pull has a safe landing |
| `StaggeredRift` | Descent | `(1280,0,1280,400)` | deliberate left/right transfers across alternating ledges |
| `ControlledAbyss` | Descent | `(1920,400,640,3200)` | commitment at Y=400, safe landing at Y=3560 (3160 px travel), continuous brakeable walls, exits near Y=720/2240/3100 |
| `ArchiveSideCave` | Descent | `(1280,400,640,1400)` | exploration/search and supply, not another mandatory drop |
| `FrozenDetour` | Descent | `(1280,1800,640,1560)` | visible ice rejects grapple; walkable detour and real stone alternatives |
| `BasinCamp` | Basin | `(1280,3600,800,720)` | checkpoint/rest/no active aggro |
| `GearPistons` | Basin | `(2080,3600,960,720)` | telegraphed cyclic obstacle timing |
| `SteamValves` | Basin | `(3040,3960,800,360)` | branch changes hazardous jet state and returns to main route |
| `CounterweightLift` | Ascent | `(3840,2800,1280,800)` | operate and ride lift; alternate grapple ledges prevent hard lock |
| `PendulumGallery` | Ascent | `(3840,2000,1280,800)` | readable sweep timing plus overhead/side grapple choices |
| `SplitLevelEncounter` | Ascent | `(3840,1200,1280,800)` | two-height combat lanes and upper-gallery shortcut control |
| `SealedCourtyard` | Temple | `(5120,720,960,720)` | staged gate encounter with pre-Boss rest/checkpoint beyond it |
| `BossHall` | Temple | `(6080,720,1200,720)` | existing Boss contract in a bounded arena |
| `SafeExit` | Temple | `(7280,720,400,720)` | explicit non-void end region |

Task 1 freezes these node bounds as data; Task 4 authors their collision, physical traversal segments and recovery geometry only after save isolation. Do not invent a second node-coordinate layout during Task 1 or treat data validation as proof of physical passability.

Use the following topology. The main route is `LowCeilingBridge → StaggeredRift → ControlledAbyss → BasinCamp → GearPistons → CounterweightLift → PendulumGallery → SplitLevelEncounter → SealedCourtyard → BossHall → SafeExit`. Loop one is `StaggeredRift → ArchiveSideCave → FrozenDetour → BasinCamp`. Loop two is the high/low choice `GearPistons → CounterweightLift` versus `GearPistons → SteamValves → CounterweightLift`. A persistent bidirectional shortcut connects `SplitLevelEncounter` to `BasinCamp` with ID `canyon.v2.upper-basin-shortcut`.

The two closed loops above mean two distinct forward paths that split and rejoin at the named endpoints; they do not implicitly require reversing the full abyss. Validate both forward alternatives, not merely an undirected cycle count. Only the explicitly bidirectional upper/basin shortcut requires reverse traversal at this slice; any later additional reverse route must supply its own physical contract.

Every route edge must name a nonblank `TraversalSegmentId`. The generated source-sector subtree must contain exactly one `TraversalSegment2D` with that ID; after transforming its launch/landing zones to world space, they must overlap the declared source/destination node bounds in the edge direction. For every `Bidirectional=true` edge, also require a distinct nonblank `ReverseTraversalSegmentId` owned by the destination-sector subtree, with launch in To and landing in From. All forward/reverse segment IDs are globally unique; unidirectional edges leave the reverse ID empty. A shared corridor can underlie two directed contracts, but forward passability does not imply reverse passability. Logical graph adjacency without this physical traversal contract is invalid.

The `SealedCourtyard → BossHall` edge requires `MechanismActivated` with `RequiredMechanismId = "canyon.v2.courtyard-seal-open"`; the `BossHall → SafeExit` edge separately requires `BossDefeated`. A shortcut activation cannot satisfy either gate. The two-condition seal must be represented in the route graph as well as physical collision, so guidance cannot suggest a passage that is still closed.

Keep `PreviousValue`/`NextValue` readable for legacy scenes during the transition, but add exported `Godot.Collections.Array<int> NeighborValues` and decoded `IReadOnlyList<PrologueSectorId> Neighbors`. Once all generated definitions contain `NeighborValues`, graph validation and runtime selection must use only `Neighbors`; no route decision may use enum ordering.

- [ ] **Step 1: Write the graph RED tests**

Add named tests that require: layout version 2; exact envelope and five rectangles; all twelve challenge kinds represented exactly once; `BasinCamp` and `SafeExit` not counted as challenge kinds; two closed loops; one gated persistent shortcut; every edge endpoint inside its declared sector; an ungated path from `LowCeilingBridge` through `SealedCourtyard`; `BossHall` reachable only after the completed-seal activation; `SafeExit` additionally requiring the explicit `BossDefeated` gate; symmetric streaming neighbors; no interior-overlapping sector rectangles; all stable IDs unique/nonblank; nonblank unique traversal-segment IDs; and rejection of undefined/asymmetric neighbors. Independently vary the three gate inputs to prove no shortcut, seal or Boss flag substitutes for another.

Add a test proving `RequiredSet(CanyonBasin, [LostTemple])` returns Basin, its explicit physical neighbors and the otherwise non-neighbor pinned Temple exactly once in deterministic enum order.

- [ ] **Step 2: Run the focused RED**

Use the global real-process template with `$caseName = 'task1-layout-red'`. If the newly written tests intentionally reference not-yet-created types and the build is the RED, capture that build output in the same external evidence directory and stop; do not start Godot. Once minimal type shells compile, run the focused test process and require a nonzero real `ExitCode` for the behavioral RED. Do not weaken old exact-chain assertions before this RED is captured.

- [ ] **Step 3: Implement immutable layout data and graph validation**

Implement `CanyonRouteGraph.Reachable(CanyonRouteNodeId from, IReadOnlySet<string> openShortcuts, IReadOnlySet<string> activatedMechanisms, bool bossDefeated)` with deterministic breadth-first traversal. Gate evaluation is exhaustive: `None` always passes; `ShortcutOpen` passes only for the edge's exact nonblank `StableShortcutId`; `MechanismActivated` passes only for its exact nonblank `RequiredMechanismId` in `activatedMechanisms`; `BossDefeated` passes only when `bossDefeated` is true. Boss defeat must not implicitly open a shortcut or seal, and neither an open shortcut nor completed seal alone exposes SafeExit. `Validate()` must reject missing challenge kinds, duplicate/unknown stable IDs, unknown edge endpoints, one-way declarations for physical streaming neighbors, any disconnection before `SealedCourtyard`, a BossHall entry lacking the completed-seal gate, a SafeExit edge lacking the Boss gate, mismatched gate-ID fields, and coordinates outside `DesignBounds`. After supplying all legitimate gate inputs, the full main route must be reachable.

Change the stream method to this signature:

```csharp
public IReadOnlyList<PrologueSectorId> RequiredSet(
    PrologueSectorId current,
    IEnumerable<PrologueSectorId> pinned)
```

It returns `current + current.Neighbors + valid pinned`, de-duplicates and sorts; it does not automatically include every route node’s chunk. Keep this source-compatible overload until Task 2 has updated every production caller, then remove it in Task 2:

```csharp
public IReadOnlyList<PrologueSectorId> RequiredSet(PrologueSectorId current) =>
    RequiredSet(current, Array.Empty<PrologueSectorId>());
```

Update `BuildPrologueLevel.cs` in this task to emit explicit symmetric `NeighborValues` for the existing generated scene, and regenerate `scenes/prologue/PrologueLevel.tscn`; this keeps the intermediate commit runnable before Task 4 changes its geometry.

**Task1 review ruling (2026-09-13):** The required builder regeneration may change engine-generated node `unique_id` metadata in addition to the five new `NeighborValues` arrays. This is an explicit exception to the execution note's earlier over-broad “all serialized fields” preservation wording. Node names/paths, resource identities, all runtime exported/geometry values and save-visible/stable gameplay IDs remain unchanged. Current saved checkpoints use sectors/coordinates, and fresh packed-scene/Continue/death/remount/door tests cover this slice; external editor-only identity consumers remain an unverified risk. Do not hand-edit the generated scene to mask this metadata churn.

- [ ] **Step 4: Run GREEN and mutation checks**

Run the focused runner with `$caseName = 'task1-layout-green'` and require real `ExitCode == 0`. Then temporarily, with byte snapshot/finally restoration, remove the Steam-Valves-to-Counterweight edge, duplicate one stable ID, and restore `RequiredSet` to enum predecessor/successor behavior. Each mutation must produce a nonzero real process exit and name the expected failed test. Defer `RunPrologueTests` integration to Task 10 so this task does not touch its in-flight edits.

- [ ] **Step 5: Commit the data-contract slice**

```powershell
git add scripts/prologue/seamless/CanyonWorldLayout.cs scripts/prologue/seamless/CanyonChallengeKind.cs scripts/prologue/seamless/CanyonRouteNodeId.cs scripts/prologue/seamless/CanyonRouteGraph.cs scripts/prologue/seamless/PrologueSectorDefinition.cs scripts/prologue/seamless/PrologueSectorStreamModel.cs tools/build/prologue/BuildPrologueLevel.cs scenes/prologue/PrologueLevel.tscn tests/unit/CanyonWorldLayoutTests.cs tests/unit/PrologueSectorStreamModelTests.cs tests/RunContinuousCanyonTests.cs
git commit -m "feat: define continuous canyon layout graph"
```

---

### Task 2: Make Streaming Graph-Aware and Pin the Real Claw Target

**Prerequisite:** The P0 world fix and auto-pull residency owner are merged. Before editing, verify the signatures below exist exactly. If they differ, stop and reconcile with the auto-pull plan owner; do not create an adapter that changes their meaning.

**Owned here:** world/stream behavior and its tests only.

**Files:**
- Modify: `scripts/prologue/seamless/SeamlessPrologueWorld.cs`
- Modify: `scripts/prologue/seamless/PrologueSectorStreamModel.cs`
- Modify: `tests/unit/PrologueSectorStreamModelTests.cs`
- Modify: `tests/integration/SeamlessPrologueWorldTests.cs`
- Modify: `tests/RunContinuousCanyonTests.cs`

**Consumed claw contract (do not implement or modify here):**

```csharp
public GrappleSurface2D ResidencyTarget { get; private set; }
public event Action<GrappleSurface2D, bool> TargetResidencyChanged;
public bool NotifyTargetWillUnload(Node subtree);
```

`ResidencyTarget` is non-null only for a non-legacy terrain target with a nonzero `LockedTargetRuntimeId`. Residency begins on first Travel and remains through Travel, Latched, Reel, Swing and the synthetic Boost publication. On Space release, the claw publishes Boost while the target is resident, then `(sameTarget, false)`, then Released and drops its reference. The claw owner emits exactly one `(target, true)` and one `(sameTarget, false)` per residency; other cancel, Q/RMB/Dash, failure, death/recovery, forced unload and exit paths clear it once. Do not shorten this contract to the states visible after a physics tick, because Latched/Boost may only be observed by synchronous subscribers.

- [ ] **Step 1: Write streaming-residency RED tests**

Add tests that mount a target in an adjacent chunk, cross the seam during Travel/Reel/Swing, and assert that target chunk, current chunk and intended landing-neighbor chunk stay mounted. Synchronous Latched/Boost event observers must still find the same mounted resident target. Assert that the matching `false` unpins only after current and landing chunks are mounted, followed by Released/Idle with no resident target. Add the reentrant cases: event occurs before world subscription (initial `ResidencyTarget` read catches it), duplicate `true` is idempotent, `false` for a stale target does not unpin the current target, and world exit unsubscribes.

Include an outgoing target owner that would actually leave the desired set after unpinning (not already retained by current/neighbor membership). A later subscriber to the same false event must still find the argument valid, inside its mounted sector, and equal to the claw's still-readable old residency reference. After all false callbacks return, require the intended Released transaction exactly once, no spurious TargetLost/reset from normal retirement, and safe removal at the next queued streaming refresh. Capture Boost→false→Released for Space and preserve the current/landing coverage across that interval.

Add route adjacency tests for both directions of each physical seam and prove `AreAdjacent` no longer reads `Previous`/`Next`. Normal desired-set shrink must not call `NotifyTargetWillUnload` on an active resident target; externally removing/destroying the target must call it and synchronously clear motor/line state before the owning chunk retires.

- [ ] **Step 2: Run RED**

Expected: focused runner fails because the world has no pin set/subscription and still uses `RequiredSet(request.Sector)` plus `Previous/Next` adjacency.

- [ ] **Step 3: Implement the world-side consumer**

Subscribe first, then immediately pass `_claw.ResidencyTarget` through the same handler. Resolve target ownership by walking ancestors until a mounted `PrologueSector` is found and matching that exact instance in `_mounted`; never infer ownership from position or node name.

```csharp
private readonly HashSet<PrologueSectorId> _pinnedSectors = [];
private GrappleSurface2D _residentTarget;

private void OnTargetResidencyChanged(GrappleSurface2D target, bool resident)
{
    if (resident) PinMountedOwner(target);
    else if (ReferenceEquals(target, _residentTarget)) QueueResidentPinRelease(target);
}
```

Compute the desired set with `_streamModel.RequiredSet(request.Sector, _pinnedSectors)`. Stage all additions, commit them, update current-sector ownership, then retire removals. Inside the target-false callback, guarantee current/all-neighbor landing coverage and schedule removal of the matching pin, but do not detach, retire, free or force-notify the outgoing target owner. Retain the world-side target and pin through the whole false transaction so another subscriber's synchronous transition still includes the outgoing owner in its desired set. Clear that pin and perform the shrinking refresh only after the callback stack has unwound and the claw has completed Released/reference cleanup (the next world physics pass is suitable). A target-owning chunk may enter normal removal only after that completed false transaction, not merely because the false callback has started: during the callback the claw deliberately still exposes its old reference. Use `NotifyTargetWillUnload` solely when external destruction or an invalid mounted owner makes the normal guarantee impossible; preserve synchronous cleanup for those genuinely forced cases.

After changing every production and test caller in this task, remove the one-argument compatibility overload introduced by Task 1; the compiler then guards against future unpinned desired-set calculations.

- [ ] **Step 4: Run GREEN and forced-loss mutation**

Run the focused suite and existing seamless-world integration suite. Mutate the desired-set call to omit `_pinnedSectors`; the active-cross-seam test must fail before restoration.

- [ ] **Step 5: Commit the streaming slice**

```powershell
git add scripts/prologue/seamless/SeamlessPrologueWorld.cs scripts/prologue/seamless/PrologueSectorStreamModel.cs tests/unit/PrologueSectorStreamModelTests.cs tests/integration/SeamlessPrologueWorldTests.cs tests/RunContinuousCanyonTests.cs
git commit -m "feat: pin active grapple sectors in continuous world"
```

---

### Task 3: Replace Sector Camera Snaps with Continuous Route Focus

**Files:**
- Create: `scripts/prologue/seamless/CanyonCameraFocusModel.cs`
- Modify: `scripts/prologue/seamless/CanyonWorldLayout.cs`
- Modify: `scripts/prologue/seamless/SeamlessPrologueWorld.cs`
- Create: `tests/unit/CanyonCameraFocusModelTests.cs`
- Modify: `tests/integration/SeamlessPrologueWorldTests.cs`
- Modify: `tests/RunContinuousCanyonTests.cs`

**Interface:**

```csharp
public sealed record CanyonCameraFocusRegion(
    string Id, Rect2 WorldBounds, Vector2 FocusOffset,
    Vector2 VelocityLookAhead, float BlendSeconds);

public readonly record struct CanyonCameraFrame(
    Vector2 LocalPosition, Vector2 FocusOffset, string RegionId);
```

Add `[Export] public int LayoutVersion { get; set; } = 1;` to `SeamlessPrologueWorld`. Task 3 exercises layout-v2 focus through fixtures with `LayoutVersion=2`, but the checked-in pre-expansion `PrologueLevel.tscn` stays at 1 and retains its existing camera path. Task 4 may set the generated world to 2 only after its Step 0 save-isolation guard is committed and GREEN; this prevents camera/layout code from becoming the first live layout mutation while the old save slot is still active.

- [ ] **Step 1: Write camera RED tests**

Require a single global camera limit derived from `CanyonWorldLayout.DesignBounds`; continuity in both directions at all four chunk seams; no `SnapCameraToTarget()` on ordinary region/chunk transitions; same `PlayerRoot` instance; and a bounded focus derivative under the existing blend rate. Assert the live `Camera2D` remains the Player child and receives a player-local position. Feeding a world-space focus point directly into `Camera2D.Position` must fail because it double-counts the parent translation.

For `ControlledAbyss`, assert downward velocity progressively increases downward look-ahead, caps at 260 px and returns smoothly after landing. Before the Y=400 no-damage commitment ledge, the first reachable brake face and safe Y=720 side exit must both fit in the 1280×720 viewport. The Y=2240 and Y=3100 exits need not be visible from the top; each must enter the viewport with at least 320 px of vertical decision lead as the player approaches, while a reachable brake-wall segment remains continuously visible. The safe bottom must enter view before its final landing decision, not remain visible throughout the 3160 px shaft. No guidance or test may direct a grapple toward an off-screen surface.

- [ ] **Step 2: Run RED**

Expected first: compiler/interface evidence for the missing camera model, followed by inert shells and a successful rebuild. The actual focused process must then exit nonzero because current per-sector `_cameraTargetBounds` behavior and `CanyonAscent` hard-coded headroom violate the assertions.

- [ ] **Step 3: Implement continuous focus**

Use the global design bounds as camera limits. Select authored focus regions from player position, blend `FocusOffset`, and add velocity look-ahead without changing player position. The camera is a direct child of Player: convert any selected world focus through `_player.ToLocal(worldFocus)` before assigning the resulting local value to `Camera2D.Position`. Never assign a world coordinate to `Position`, and never let another component race the world’s existing per-physics-frame writer. Keep presentation shake on `Camera2D.Offset`; this model owns only the local `Camera2D.Position`. Remove the `CanyonAscent` special case. Retain camera snaps only for new-run spawn, explicit continue/checkpoint relocation and recovery after the player position has been applied.

The abyss function must be monotonic and clamped:

```csharp
float fallRatio = Mathf.Clamp(Mathf.Max(0f, velocity.Y) / maxFallSpeed, 0f, 1f);
float downwardLookAhead = Mathf.Lerp(80f, 260f, fallRatio);
```

Pass `new PlayerMotionSettings().MaxFallSpeed` as `maxFallSpeed`; reject nonpositive/nonfinite values. Camera tests must validate the staged visibility windows above using actual authored brake-face, side-exit and safe-bottom bounds, not this numeric expression alone. The rationale is geometric: at Y=400, a 720 px viewport plus 260 px maximum look-ahead cannot expose a Y=1280 exit, while the Y=720 exit remains a readable pre-commit option without zoom, teleport or physics padding.

- [ ] **Step 4: Run GREEN and two regressions**

Run focused tests, then existing camera/recovery tests. Mutate the abyss look-ahead sign and reintroduce a seam snap; their named tests must fail.

- [ ] **Step 5: Commit**

```powershell
git add scripts/prologue/seamless/CanyonCameraFocusModel.cs scripts/prologue/seamless/CanyonWorldLayout.cs scripts/prologue/seamless/SeamlessPrologueWorld.cs tests/unit/CanyonCameraFocusModelTests.cs tests/integration/SeamlessPrologueWorldTests.cs tests/RunContinuousCanyonTests.cs
git commit -m "feat: add continuous canyon camera focus"
```

---

### Task 4: Build the Six-Level U and the Three Core Traversal Lessons

**Status — accepted 2026-09-20:** four scoped commits through `f0262ee`; independent spec/quality review Approved, controller fresh complete default dependency run build33556/0 → engine69208/0. Seven intentional negative-test warnings disclosed, no others. This accepts the geometry/compatibility shell only; full-game and visual/human acceptance remain open.

**Files:**
- Modify first: `scripts/save/PrologueSaveData.cs`
- Modify first: `scripts/save/PrologueSaveStore.cs`
- Modify first: `scripts/prologue/PrologueGameFlowController.cs`
- Modify first: `tests/unit/PrologueSaveTests.cs`
- Create first: `tests/integration/ContinuousCanyonSaveIsolationTests.cs`
- Modify: `tools/build/prologue/PrologueCanyonBuildParts.cs`
- Modify: `tools/build/prologue/CanyonContentCatalog.cs`
- Modify: `tools/build/prologue/CanyonMicroSceneDefinition.cs`
- Modify: `tools/build/prologue/CanyonContentBuilder.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tests/unit/PrologueCanyonStructureTests.cs`
- Modify: `tests/integration/PrologueCanyonTraversalTests.cs`
- Modify: `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`
- Modify: `tests/RunContinuousCanyonTests.cs`
- Modify the concrete terrain-assist contract consumer: `scripts/prologue/traversal/TraversalSegment2D.cs`; cover through the already-owned `tests/unit/PrologueCanyonStructureTests.cs`.
- Modify the concrete old-path guidance consumer: `scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs`, `scripts/prologue/guidance/PrologueGuidanceController.cs` and `tests/unit/PrologueGuidanceTargetResolverTests.cs`, limited to explicit layout-aware target mappings, static-target owning-sector projection and their compatibility tests.
- Modify `tests/unit/PrologueSectorStreamModelTests.cs` only for the generated-layout compatibility check: retain literal layout1 bounds/offsets/neighbors against actual sample definitions and frozen/sample leaves, then independently verify main explicit layout2 and exact authoritative bounds/offsets/neighbors. Preserve all malformed-definition negatives. Modify `tests/unit/SeamlessPrologueMainSceneContractTests.cs` only to parameterize its lifetime-proof owner label with the existing label as default; the owned focused runner may retain its own process-lifetime main PackedScene and reuse the unchanged repeated-instance/node-baseline assertions. Actual same-order full-suite GCHandle errors must disappear before acceptance; do not change the dirty full runner or runtime to silence diagnostics.
- Regenerate through builders: `scenes/prologue/sectors/CanyonEntrance.tscn`, `CanyonDescent.tscn`, `CanyonBasin.tscn`, `CanyonAscent.tscn`, `LostTemple.tscn` and `scenes/prologue/PrologueLevel.tscn` (the same six outputs listed in Step 6).
- Sample compatibility substep: create `scripts/prologue/sample/BasinSampleLegacyLayout.cs`, `tools/build/prologue/BuildBasinSampleLegacyLayout.cs` and `tests/integration/BasinSampleLayoutIsolationTests.cs`; modify `scripts/prologue/sample/BasinQualitySampleEntry.cs` and `tools/build/prologue/BuildBasinQualitySample.cs`.
- Capture pre-v2 geometry through the new one-shot builder: `scenes/prologue/sample/legacy/CanyonEntrance.tscn`, `CanyonDescent.tscn`, `CanyonBasin.tscn`, `CanyonAscent.tscn`, `LostTemple.tscn`; regenerate the existing sample outputs `scenes/prologue/sample/BasinQualitySample.tscn`, `BasinQualitySampleSector.tscn`, `BasinLaborer.tscn` only through their updated builder.
- Create the frozen-directory-only `scenes/prologue/sample/legacy/.gitattributes` with `*.tscn -text` so snapshot bytes survive future `core.autocrlf` checkouts; do not change global Git configuration or renormalize unrelated files.

- [x] **Step 0: Isolate the new layout before changing any scene or bound**

Write a RED test against the unchanged P0 code proving that starting the future layout would still discover/write `user://knight/prologue-save.json`. Add compile-safe save shells, obtain the behavioral RED, then implement and commit this guard before touching builders or generated scenes:

```csharp
// Missing JSON fields deserialize to zero and remain distinguishable from layout v2.
public int LayoutVersion { get; set; }

public static PrologueSaveData NewForLayout(int layoutVersion) => new()
{
    Version = CurrentVersion,
    LayoutVersion = layoutVersion
};
```

Set the full-world default `SavePath` to `user://knight/prologue-save-layout-v2.json` and add read-only migration source `LegacySavePath = "user://knight/prologue-save.json"`. During this intermediate task, Start creates `NewForLayout(CanyonWorldLayout.CurrentLayoutVersion)` in memory and writes only the new path after the player starts; Continue reads only a new-path file whose explicit `LayoutVersion == 2`. A legacy-only file remains byte-identical and Continue stays unavailable until Task 7 supplies the choice UI. A new-path JSON with a missing/zero/unknown `LayoutVersion` is refused and not overwritten.

`PrologueSaveStore` must inspect the raw JSON `layoutVersion` token before normalization. The C# property is `LayoutVersion`, but existing JSON naming is camelCase: use wire keys `version`, `layoutVersion`, `checkpointId`, `openShortcuts`, `activatedMechanisms` and `discoveries` when inspecting `JsonDocument`. Do not use a property initializer, constructor default, `?? 2`, or normalized fallback that can relabel a missing field as layout 2. Preserve current schema version 2 temporarily; Task 7 owns the schema-3 migration.

The new layout policy applies only to the full-world mode. BasinQualitySampleEntry injects its own SavePath before tree entry and uses the old authored checkpoint rules; keep its Start/Continue and layout-0 legacy data valid. Generic normalization must preserve a missing layout as zero rather than globally requiring/upgrading to layout 2. Put full-world eligibility at the mode-aware load/flow boundary, and never replace the injected sample path with either full-world path. In the isolation fixture, verify accepted sample Start and subsequent valid Continue still work, rejected sample Continue writes nothing, and both full-world sentinel files remain byte-identical. For read-only initialization fixtures set HeadlessAutoStart=false before tree entry: the existing full-world headless default otherwise calls Start and legitimately writes.

Run `PrologueSaveTests` and `ContinuousCanyonSaveIsolationTests` through the actual process template. Require byte hashes of the legacy fixture to match before/after Start, rejected Continue and new-slot save. Commit the isolation guard separately:

```powershell
git add scripts/save/PrologueSaveData.cs scripts/save/PrologueSaveStore.cs scripts/prologue/PrologueGameFlowController.cs tests/unit/PrologueSaveTests.cs tests/integration/ContinuousCanyonSaveIsolationTests.cs tests/RunContinuousCanyonTests.cs
git commit -m "fix: isolate continuous canyon save slot"
```

No following step in this task may run before this commit and its focused tests are green.

- [x] **Step 0.5: Preserve the independently playable sample before main geometry changes**

The current sample loads the shared main parent and replaces only its Basin scene; therefore new main bounds/offsets/LayoutVersion would reject or misplace the old sample even though its save path is isolated. Before changing live main geometry, capture the five current pre-v2 sector scenes into the new sample/legacy paths using `BuildBasinSampleLegacyLayout`. Capture must reject an existing destination (never overwrite a frozen snapshot), record source identities and verify packed geometry/collision/profile equivalence. The frozen Basin is a seed for future sample rebuilds; runtime continues using the existing sample-specific Basin output. Snapshot generation is an explicit exception to the ordinary main-builder-only scene rule, not permission to rewrite old gameplay or duplicate runtime code.

Preserve strict snapshot SHA256 across platforms with the local frozen-directory `.gitattributes` rule `*.tscn -text`. Verify working-tree/index byte correspondence, scope the attribute to these snapshots, and commit it before main geometry activation. Do not use broad re-normalization, checkout/reset or global Git settings to achieve this.

Use `BasinSampleLegacyLayout` to return fresh sample-owned definitions with the exact pre-v2 five sector IDs, offsets, bounds, explicit neighbors and key-interaction bounds from the pre-expansion parent. In `BasinQualitySampleEntry`, replace the entire definitions array and set `LayoutVersion=1` before tree entry. Continue sharing current player, UI, controllers and parent structure. Modify `BuildBasinQualitySample` to instantiate the frozen Basin seed instead of calling the mutable main `PrologueCanyonBuildParts.Build`; validate using its sample-owned legacy definition, not future main bounds/offsets. Preserve the existing sample actor, visual and checkpoint modifications.

Add `BasinSampleLayoutIsolationTests` to the focused runner. Capture a genuine failing independence assertion before the fix. Test actual sample initialization/Start/death/Continue, old AuthoredStart `(2380,2014)` on real support, legacy camera/layout selection, distinct definition resources and all five frozen scene references. Preserve the existing sample temple/recovery contracts as well as save sentinels. Repeat after all six main scene builders and the sample builder have run, so independence is proved against actual expanded main scenes, not only a fabricated metadata array. Verify frozen snapshot hashes remain unchanged after main/sample rebuilds. Commit this compatibility guard separately before any main-v2 builder/scene edit:

```powershell
git add scripts/prologue/sample/BasinSampleLegacyLayout.cs scripts/prologue/sample/BasinQualitySampleEntry.cs tools/build/prologue/BuildBasinSampleLegacyLayout.cs tools/build/prologue/BuildBasinQualitySample.cs tests/integration/BasinSampleLayoutIsolationTests.cs tests/RunContinuousCanyonTests.cs scenes/prologue/sample/legacy/CanyonEntrance.tscn scenes/prologue/sample/legacy/CanyonDescent.tscn scenes/prologue/sample/legacy/CanyonBasin.tscn scenes/prologue/sample/legacy/CanyonAscent.tscn scenes/prologue/sample/legacy/LostTemple.tscn scenes/prologue/sample/BasinQualitySample.tscn scenes/prologue/sample/BasinQualitySampleSector.tscn scenes/prologue/sample/BasinLaborer.tscn
git commit -m "fix: isolate legacy sample geometry from canyon expansion"
```

**Authoritative world-space challenge bounds:** Use the single table frozen in Task 1 verbatim. This task activates that data after save isolation; it does not define a second coordinate layout.

- [x] **Step 1: Write structure/traversal RED tests**

Replace assertions for the old 6080×2160 chain with tests for the exact five rectangles and fourteen route/support nodes above. For every challenge, assert a distinct `ChallengeKind`; do not infer the kind from its name. Assert true collision-body geometry, not backdrop pixels, for all floors/walls/ceilings.

For the first three lessons, add geometry-level traversal contracts: bridge can be crossed without double jump; rift requires alternating horizontal direction at least twice but offers a catch shelf after each transfer; abyss has a commitment ledge at Y=400, continuous brake faces on both walls, intermediate exits at Y=720/2240/3100, and a safe landing at Y=3560. The Y=720 exit and first brake face must be visible before commitment; later exits must have the camera decision lead specified in Task 3. Assert the uninterrupted center travel is 3160 px. At current gravity 1850 px/s² and terminal fall speed 780 px/s, the continuous analytical reference is about 2955.57 px at four seconds and 4515.57 px at six seconds; therefore this layout can support a four-second passive descent but must not claim an unconditional six-second free fall inside 4320 px. Use the actual `PlayerMotionModel` test rather than the estimate as the gate. All main-route grapple surfaces must be child profiles of thick `StaticBody2D` collision with `UseCollisionHitPoint=true`; floating dedicated anchors cannot be the sole solution.

Player collision is a centered capsule with 20 px radius and 72 px total height. Every authored gap, stop surface and landing probe must include at least 20 px horizontal wall stand-off and 36 px ceiling/floor stand-off; test the real capsule sweep, not the player origin or sprite rectangle.

The existing `TraversalSegment2D.ValidateAssistTarget` only accepts a DedicatedAnchor with child TargetCollision Area2D, which conflicts with the required physical-terrain Assisted routes. Extend the owned validator with a narrow terrain-profile branch: GripStone/TempleMetal, UseCollisionHitPoint, proper StaticBody2D owner, both World and ThreadTarget collision bits, and a real enabled nondegenerate collision shape. Retain existing DedicatedAnchor compatibility, finite/range/direction/ability guards and negative cases. Reject Ice, Forbidden, missing/disabled collision and label-only targets; do not relabel Assisted routes as SingleJump. Capture the actual terrain-contract behavioral RED before editing the validator. Any further point-origin/direction semantic conflict needs concrete evidence and an explicit ruling; static validation never substitutes for real capsule/input passage proof.

For every `CanyonRouteEdge`, instantiate the generated source sector and resolve its `TraversalSegmentId` by exact exported ID among the traversal-contract children, requiring exactly one match; IDs are not NodePaths. Preserve all immutable dotted IDs verbatim. Godot sanitizes illegal node-name characters; the installed C# `ValidateNodeName()` helper removes dots (unlike direct Node.Name assignment). The owned CanyonContentBuilder Segment wrapper and TraversalSegment2D validator explicitly use that helper's form for Node.Name only. Retain required/duplicate exact-ID rejection, arbitrary wrong-name rejection and unchanged legacy safe-name compatibility; add a dotted-ID mapping positive case and these negatives. Do not change the generic PrologueBuildParts helper or relax range/direction/ability/zone contracts. Transform `TraversalSegment2D.LaunchZone` and `LandingZone` to world space and require them to intersect the `From` and `To` route-node bounds respectively. For bidirectional edges, independently resolve `ReverseTraversalSegmentId` from the destination sector and validate To-launch/From-landing zones. Explicitly test both the upper `GearPistons → CounterweightLift` segment and lower `GearPistons → SteamValves → CounterweightLift` segments; a graph-only Steam/Counterweight connection fails. Name separate actual-input assertions for `UpperGalleryShortcutReachesBasin` and `BasinShortcutReturnsToUpperGallery`; neither may teleport or borrow an unearned double jump.

- [x] **Step 2: Run RED**

Step 0 already captured and fixed the old-save-slot behavioral RED in its own external evidence case. For this Step 2, do not expect or recreate that failure: after any geometry/layout compile-safe shells and a successful rebuild, the actual focused process must exit nonzero only on current bounds/catalog counts, missing physical edge segments and old anchor-chain behavior. The save-isolation suite must remain GREEN in the same run.

- [x] **Step 3: Move geometry authority into the layout/catalog**

Make `PrologueCanyonBuildParts.Bounds`, `WorldOffset`, scene definitions and level definitions read the exact `CanyonWorldLayout` data. Set the generated `SeamlessPrologueWorld.LayoutVersion` to 2 here, after Step 0 has isolated the save slot; this is the first live activation of layout-v2 camera/world behavior. Extend `CanyonMicroSceneDefinition` with `CanyonChallengeKind? ChallengeKind` and `CanyonRouteNodeId RouteNodeId`; remove catalog entries that only rename a backdrop, or mark them as visual landmarks subordinate to one challenge instead of counting them.

Use the builder’s existing terrain pattern: collision under `Gameplay/Collision`, profile child named `GrappleSurfaceProfile`, route markers under `Gameplay/RouteMarkers`, recovery under `Gameplay/FallRecovery`, and skins only under `VisualRoot/GameplayReadable`. Author the three lessons first and pack/instantiate each leaf sector before saving.

Normal traversal can leave the Entrance while the real `LearnAttackMode` objective is still pending. Static guidance keys have explicit authored owning sectors: a known off-owner target has no local marker, while a missing or invalid target inside its owning sector retains the existing diagnostic. Unknown keys must not silently become off-owner exclusions. Preserve pending lesson text/objective/progress and live Boss/claw resolution; do not force tutorial completion or fake enemy defeats. Cover owner mappings/negatives and the actual generated-main bypass. This narrow projection repair does not implement Task 8 timers or fireflies.

The existing guidance resolver hardcodes old sector paths and the generated v2 main now genuinely warns about missing EntranceCombat. Capture a focused resolver RED, then select explicit layout-aware mappings (authored sector metadata in the owned builder or actual world version), preserving standalone legacy/default-1 fixtures. Resolve semantic keys to genuine v2 actors, terrain and routes, with required material/collider validation; do not infer mode from dimensions, absent nodes or names, add misleading legacy aliases, or suppress the warning. Preserve dynamic BossBody/Chain resolution. Move the existing exact legacy-path assertions to frozen/sample layout-1 resources and add v2 mappings plus invalid-target negatives. Re-run generated-main/resolver and old sample checks. This is a narrow consumer migration, not Task 8 firefly/timing work; controller ownership is limited to the explicitly authorized Present projection repair above.

- [x] **Step 4: Prove the abyss is controllable, not padded**

Add a deterministic traversal probe that starts from rest on the real Y=400 commitment ledge, never sets `GlobalPosition`, uses the actual `PlayerMotionModel` gravity 1850 px/s² and 780 px/s terminal cap, keeps grapple correction zero, and lands the real capsule on the Y=3560 bottom. The no-input/no-brake reference must record at least four seconds between leaving the ledge and first safe-bottom contact. Separate runs must use actual side-wall grapple braking and exit at each intermediate shelf; because grapple correction is added after the base model and is not clamped by the 780 px/s base fall cap, these runs must measure the real body trajectory and may not reuse the passive estimate. Do not pass by lowering gravity/terminal speed, inserting a timer/elevator lock, shrinking physics time steps selectively or waiting in midair. The upper end of 4–6 seconds comes from meaningful braking/side-route decisions; it is not guaranteed by pure free fall. Automated measurement catches geometric/physics regressions; normal-window and human review still owns comfort, pacing and perceived duration.

For passive onset, first prove supported rest on the solid commitment ledge. Use normal A/D only to walk off, then begin the measured no-input interval at the actual grounded-to-air transition and release input. No jump, dash, claw or correction is permitted during that interval; walking time is excluded. Do not disable ledge collision or teleport the body to initiate the fall. This reconciles a real supported starting condition with a passive airborne reference.

- [x] **Step 5: Build leaf scenes in dependency order and run GREEN**

```powershell
$taskDotnetRoot = 'G:/godot/dotnet-sdk'
$env:DOTNET_ROOT = $taskDotnetRoot
$env:DOTNET_ROOT_X64 = $taskDotnetRoot
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:PATH = "$taskDotnetRoot;$env:PATH"
$runId = 'task4-{0}-{1}' -f (Get-Date -Format 'yyyyMMddTHHmmssfff'),([Guid]::NewGuid().ToString('N'))
$evidenceRoot = Join-Path 'E:/OurGame/Knight/.evidence/september09-continuous-canyon' $runId
New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
$scripts = @(
  'res://tools/build/prologue/BuildCanyonEntrance.cs',
  'res://tools/build/prologue/BuildCanyonDescent.cs',
  'res://tools/build/prologue/BuildCanyonBasin.cs',
  'res://tools/build/prologue/BuildCanyonAscent.cs',
  'res://tools/build/prologue/BuildLostTemple.cs',
  'res://tools/build/prologue/BuildPrologueLevel.cs',
  'res://tests/RunContinuousCanyonTests.cs'
)
foreach ($script in $scripts) {
  $leaf = [IO.Path]::GetFileNameWithoutExtension($script)
  & "$taskDotnetRoot/dotnet.exe" build ./Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo 2>&1 |
    Tee-Object -FilePath (Join-Path $evidenceRoot "$leaf.dotnet-build.log")
  if ($LASTEXITCODE -ne 0) { throw "Build failed before $script; Godot was not started." }
  $args = @('--headless','--path','.', '--script',$script,
    '--log-file',("$evidenceRoot/$leaf.log"))
  $process = Start-Process -FilePath 'G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe' -ArgumentList $args -WindowStyle Hidden -PassThru
  $process.WaitForExit()
  [IO.File]::WriteAllText((Join-Path $evidenceRoot "$leaf.actual-exit-code.txt"),
    $process.ExitCode.ToString([Globalization.CultureInfo]::InvariantCulture))
  if ($process.ExitCode -ne 0) { throw "$script exited $($process.ExitCode)" }
}
```

Expected: all builders exit 0, all packed scenes instantiate/validate, focused tests print their success sentinel, and the log has no `ERROR`, `WARNING`, orphan or leak signature.

- [x] **Step 6: Commit**

```powershell
git add tools/build/prologue/PrologueCanyonBuildParts.cs tools/build/prologue/CanyonContentCatalog.cs tools/build/prologue/CanyonMicroSceneDefinition.cs tools/build/prologue/CanyonContentBuilder.cs tools/build/prologue/BuildPrologueLevel.cs scripts/prologue/traversal/TraversalSegment2D.cs scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs scripts/prologue/guidance/PrologueGuidanceController.cs tests/unit/PrologueGuidanceTargetResolverTests.cs tests/unit/PrologueSectorStreamModelTests.cs tests/unit/SeamlessPrologueMainSceneContractTests.cs scenes/prologue/sectors/CanyonEntrance.tscn scenes/prologue/sectors/CanyonDescent.tscn scenes/prologue/sectors/CanyonBasin.tscn scenes/prologue/sectors/CanyonAscent.tscn scenes/prologue/sectors/LostTemple.tscn scenes/prologue/PrologueLevel.tscn tests/unit/PrologueCanyonStructureTests.cs tests/integration/PrologueCanyonTraversalTests.cs tests/integration/PrologueCanyonGeneratedMainRouteTests.cs tests/RunContinuousCanyonTests.cs
git commit -m "feat: build six-level continuous canyon shell"
```

---

### Task 5: Implement Six Distinct Mechanism Families

**Files:**
- Create: `scripts/mechanisms/CanyonMechanismFrame.cs`
- Create: `scripts/mechanisms/ICanyonTelegraphSource.cs`
- Create: `scripts/mechanisms/CanyonCounterweightLift.cs`
- Create: `scripts/mechanisms/CanyonGearPiston.cs`
- Create: `scripts/mechanisms/CanyonSteamValve.cs`
- Create: `scripts/mechanisms/CanyonSteamJet.cs`
- Create: `scripts/mechanisms/CanyonPendulumHazard.cs`
- Create: `scripts/mechanisms/CanyonSealGate.cs`
- Create: `scripts/mechanisms/CanyonShortcutWinch.cs`
- Modify: `scripts/mechanisms/BellChainMechanism.cs`
- Create: `scripts/prologue/CanyonEncounterGroup.cs`
- Modify: `scripts/prologue/PrologueProgress.cs`
- Modify: `scripts/prologue/PrologueLevelController.cs` (consume the changed shortcut signal; retain legacy call compatibility)
- Modify: `tools/build/prologue/CanyonContentBuilder.cs`
- Create: `tests/unit/CanyonMechanismModelTests.cs`
- Create: `tests/integration/CanyonMechanismRuntimeTests.cs`
- Modify: `tests/integration/BellChainRuntimeTests.cs`
- Modify: `tests/RunContinuousCanyonTests.cs`

**Six required functional families:** counterweight lift, timed gear piston, valve-controlled steam jet, swinging pendulum, two-condition seal linkage, and persistent upper-gallery-to-basin shortcut winch. The shortcut winch drops/removes a static barrier and exposes a permanent bidirectional grapple/drop corridor; it has no moving passenger platform and therefore is not a second elevator. A painted gear, idle chain or renamed trigger does not count.

**Shared contracts:**

```csharp
public readonly record struct CanyonMechanismFrame(
    bool Telegraphing, bool HazardActive, bool Traversable, float Phase01);

public interface ICanyonTelegraphSource
{
    bool IsDangerTelegraphActive { get; }
}
```

Use pure timing/state helpers inside the mechanism files rather than a general framework. Every moving collider must be `AnimatableBody2D` or an engine-supported moving-body type and move in `_PhysicsProcess`; never translate a `StaticBody2D`.

The bounded existing-claw audit confirms its world anchor/normal and DockPoint deliberately remain fixed after lock (with an explicit regression test); it does not follow moving targets. Choose the already-approved `Forbidden` branch for all translating, rotating, cycling or collision-disabling bodies from creation, including lift cars, pistons, pendulums and opening seal/barrier bodies. Do not allow grappling only while stopped: changing material later does not invalidate an active lock. Separate permanent static wall grips and one-shot controls from the moving payload, with narrow real-body profile ownership. Steam hazards offer no grab point. This is an authoring boundary, not permission for a new claw/relative-velocity API. Real lift carriage in both directions/endpoints, jump/dash departure, crushing boundaries and moving-obstacle occlusion of a static grip still require actual runtime tests; safe fallback footholds do not waive the functional lift requirement.

- [ ] **Step 1: Write six-family RED tests**

Require the following behavioral differences: the lift waits for activation, carries the player and stops at authored endpoints; the piston has safe/telegraph/active/recovery phases and collision only during active extension; the valve permanently disables or reroutes its paired jet for the run; the pendulum collision follows its visible angle with a telegraph boundary; the seal gate opens only after both its named mechanism objective and named encounter group are complete; the shortcut winch changes a static blocked corridor into persistent walk/grapple geometry, contains no passenger-moving body, survives sector retirement/remount through the same run's progress state, and is traversable both directions. Task 7 introduces stable-set disk serialization and must additionally prove fresh-process save/reload of the shortcut and completed seal; do not claim disk persistence from this intermediate Task 5 test.

Also assert pause freezes each timer, recovery/new run resets nonpersistent phase, no mechanism moves or damages outside its declared route-node bounds, and a grapple target mounted on a moving mechanism is either explicitly supported by the claw contract or marked `Forbidden`—never accidentally treated as stable terrain.

- [ ] **Step 2: Run RED**

Expected first: compiler/interface evidence for the six missing mechanism types and shortcut-ID progress API. Add inert compile-safe shells, rebuild to exit 0, then run the named cases and require a nonzero actual Godot exit because none of the six behaviors is implemented. Do not jump from compile failure directly to GREEN implementation.

- [ ] **Step 3: Implement exact progress IDs and signals**

Replace the single-purpose boolean API with stable sets while keeping a compatibility read during schema migration:

```csharp
public IReadOnlySet<string> OpenShortcuts => _openShortcuts;
public IReadOnlySet<string> ActivatedMechanisms => _activatedMechanisms;
public bool OpenShortcut(string stableId);
public bool ActivateMechanism(string stableId);
public bool IsShortcutOpen(string stableId);
public bool BellChainShortcutOpen => IsShortcutOpen("canyon.v2.temple-bell-gate");
```

Generalize `BellChainMechanism` to export `StringName ShortcutId`, emit `Opened(StringName shortcutId)`, and query `progress.IsShortcutOpen(ShortcutId)`. Assign `canyon.v2.upper-basin-shortcut` to `CanyonShortcutWinch` and `canyon.v2.temple-bell-gate` to the existing temple mechanism. Do not conflate the two. `CanyonSealGate` must export both `RequiredMechanismId` and `RequiredEncounterId`; either prerequisite alone leaves it visibly sealed, while completion of both changes collision, route availability and its persistent visible state. Until Task 7 migrates the serialized shape, loading legacy `ShortcutOpen=true` populates only `canyon.v2.temple-bell-gate`; keep the derived compatibility property above so intermediate commits still compile and behave like the current temple flow.

Update the actual signal consumer in `PrologueLevelController`: both continuous and legacy binding sites use `RecordShortcutOpened(StringName shortcutId)`, which records the supplied stable ID and requests persistence only when that ID is newly opened. Preserve no-argument `RecordShortcutOpened()` and `PrologueProgress.OpenBellChainShortcut()` as temple-ID-only compatibility delegates for existing callers; they must never open the upper/basin shortcut. The existing no-argument test subscriber in `BellChainRuntimeTests` must accept and assert the signal ID. Add focused coverage that opening either gate does not open the other and that duplicate signal delivery does not duplicate the progress-persist request. This small consumer migration belongs to Task 5, not a delayed compile break for Task 7.

Give the seal its own exported `ActivationId = "canyon.v2.courtyard-seal-open"`. Once both prerequisites are genuinely satisfied, latch `progress.ActivateMechanism(ActivationId)` exactly once; graph reachability, collision and visuals consume this same completed-seal state. On sector remount or later persisted restore, an already active seal opens without synthesizing enemy-defeat events or rewards. This does not introduce general persistent ordinary-enemy kills; only the completed gate is latched. The individual prerequisite mechanism cannot use the completed-seal ID.

- [ ] **Step 4: Build and wire the mechanisms**

Place each runtime node in its matching challenge subtree. Give every hazard an independent visible telegraph child under `VisualRoot/GameplayReadable` and a collision node under `Gameplay`. Alternative safe footholds must remain present for lift/pendulum failure. The lower Steam-Valves branch must connect physically into the bottom of `CounterweightLift`; the upper Gear-Pistons route reaches the same lift independently. The generated traversal segments must prove both launches and landings rather than accepting graph edges alone.

- [ ] **Step 5: Run GREEN and six mutations**

For each family, mutate the one behavior that makes it distinct (endpoint stop, telegraph boundary, valve run-state preservation, visible/collision angle agreement, either seal prerequisite, static shortcut deployment/remount). Each named test must fail independently and all files must be restored byte-for-byte. Also remove the completed-seal activation to prove collision/route state cannot diverge. Fresh disk-reload mutations belong to Task 7 after its schema exists.

- [ ] **Step 6: Commit**

```powershell
git add scripts/mechanisms/CanyonMechanismFrame.cs scripts/mechanisms/ICanyonTelegraphSource.cs scripts/mechanisms/CanyonCounterweightLift.cs scripts/mechanisms/CanyonGearPiston.cs scripts/mechanisms/CanyonSteamValve.cs scripts/mechanisms/CanyonSteamJet.cs scripts/mechanisms/CanyonPendulumHazard.cs scripts/mechanisms/CanyonSealGate.cs scripts/mechanisms/CanyonShortcutWinch.cs scripts/mechanisms/BellChainMechanism.cs scripts/prologue/CanyonEncounterGroup.cs scripts/prologue/PrologueProgress.cs scripts/prologue/PrologueLevelController.cs tools/build/prologue/CanyonContentBuilder.cs scenes/prologue/sectors/CanyonBasin.tscn scenes/prologue/sectors/CanyonAscent.tscn scenes/prologue/sectors/LostTemple.tscn tests/unit/CanyonMechanismModelTests.cs tests/integration/CanyonMechanismRuntimeTests.cs tests/integration/BellChainRuntimeTests.cs tests/RunContinuousCanyonTests.cs
git commit -m "feat: add canyon traversal mechanisms"
```

---

### Task 6: Populate Twelve Challenges and the 8+4 Encounter Layout

**Files:**
- Create: `tools/build/prologue/CanyonEncounterCatalog.cs`
- Create: `tools/build/prologue/CanyonEncounterDefinition.cs`
- Modify: `scripts/prologue/CanyonEncounterGroup.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceController.cs` (first-combat lookup compatibility only)
- Modify: `tools/build/prologue/CanyonContentBuilder.cs`
- Modify: `tools/build/prologue/CanyonContentCatalog.cs`
- Modify: `scripts/prologue/seamless/CanyonWorldLayout.cs`
- Modify: `tests/unit/PrologueCanyonStructureTests.cs`
- Create: `tests/unit/CanyonEncounterCatalogTests.cs`
- Create: `tests/integration/ContinuousCanyonEncounterTests.cs`
- Modify: `tests/RunContinuousCanyonTests.cs`

**Encounter contract:**

```csharp
public sealed record CanyonEncounterDefinition(
    string StableId, CanyonRouteNodeId RouteNode, bool Optional,
    Rect2 ActivationBounds, Rect2 ArenaBounds,
    IReadOnlyList<CanyonEnemyPlacement> Enemies,
    bool GatesProgression = false);

public sealed record CanyonEnemyPlacement(
    string ScenePath, Vector2 WorldPosition, int ActivationBatch);
```

Author exactly these normal groups with existing RustShell, BellBackLaborer and GraveLampMoth scenes:

| Group | Exact RouteNode | Enemies | Purpose |
| --- | --- | --- | --- |
| Main 1 | `LowCeilingBridge` | RustShell | first single-enemy read |
| Main 2 | `StaggeredRift` | RustShell | movement under light pressure |
| Main 3 | `ControlledAbyss` | Moth | aerial read near the landing, outside the no-damage commitment lesson |
| Main 4 | `GearPistons` | RustShell + Laborer | timing plus armor/guard break, separated batches |
| Main 5 | `CounterweightLift` | Moth + RustShell | two-height spatial split |
| Main 6 | `PendulumGallery` | Laborer | readable sweep response after a safe observation span |
| Main 7 | `SplitLevelEncounter` | Laborer + Moth | mixed pattern and two lanes |
| Main 8 | `SealedCourtyard` | RustShell + Laborer | gated pre-Boss encounter |
| Branch 1 | `ArchiveSideCave` | RustShell | optional supply guard |
| Branch 2 | `FrozenDetour` | Moth | upper safe-stone island beside ice |
| Branch 3 | `SteamValves` | Laborer | optional mechanism/combat interaction |
| Branch 4 | `FrozenDetour` | RustShell + Moth | lower return alcove, separate arena and activation from Branch 2 |
| Boss | `BossHall` | existing Fallen Bellkeeper | separate Boss contract, not counted among 12 normal groups |

`BasinCamp` contains no encounter group or activation bounds. Branch 2 and Branch 4 occupy distinct non-overlapping upper/lower subregions of the 1560 px tall FrozenDetour and cannot activate together; the lower arena has actual safe stone footing. Both belong only to the optional archive/frozen loop. PendulumGallery is mandatory traversal and its Laborer is Main 6, never counted as an optional branch encounter.

Main 8's "gated pre-Boss encounter" is inside the courtyard unlocked by Main 7 plus the crank; it is not an additional defeat lock on the Boss. Preserve the exact two-condition seal from Task 5: Main 7 keeps stable ID `canyon.v2.encounter.split-level` and is the only normal catalog group with `GatesProgression=true`; all other normal groups, including Main 8, use false. Main 8 remains a main-route group, followed by a real no-aggro checkpoint/rest span. Verify that span with actual live-enemy behavior, including a player leaving the encounter without killing every actor. Do not add a third prerequisite, rename the completed-seal ID, or persist every ordinary enemy group's defeat.

Preserve the existing EntranceCombat semantic target when replacing the old resident Rust with an activated group. An inert authored encounter marker may retain the static resolver path, but it is not a living enemy: the nearby-first-combat teaching must resolve the actual current Entrance Rust from its Actors subtree and retain mode, health, viewport, line-of-sight, range and hysteresis guards. Add a real layout2 regression in ContinuousCanyonEncounterTests: marker/inactive group alone gives no combat cue; live group activation in nearby Attack mode does; death/retirement/other-sector actors do not. Preserve layout1 guidance and do not fake a Health/AI component, force lesson completion or suppress warnings. This is Task6 compatibility at the changed spawn seam, not early Task8 firefly implementation.

- [ ] **Step 1: Write catalog RED tests**

Assert exactly eight nonoptional and four optional normal encounter IDs, plus one Boss; every group sits inside its exact route-node bounds in the table; at least one normal group exercises each of single read, Laborer armor/guard break, aerial enemy and two-level separation; no camp/rest rectangle intersects an activation bound; every branch encounter belongs to an optional route node; the two FrozenDetour arenas and activation bounds do not overlap or activate concurrently; optional combat cannot gate the main route; and activation batches never expose more than two living normal enemies inside the effective-range fixture.

- [ ] **Step 2: Run RED**

Expected first: compiler/interface evidence for the new catalog/group members, followed by inert shells and a successful rebuild. The actual focused process must then exit nonzero because current four normal placements and uncatalogued scene-local spawns fail count, identity and activation-bound assertions.

- [ ] **Step 3: Implement encounter groups without changing enemy health**

`CanyonEncounterGroup` owns activation by authored player bounds, subscribes to the existing enemy `Defeated` events, and publishes `GroupDefeated(stableId)` once. It activates the next batch only after the current batch is defeated or the player crosses an authored separation threshold. It must not alter enemy health/damage/timing values and must detach subscriptions on retirement.

Use geometry, elevation and activation batches to prevent a four-enemy pile. Put an observation/rest span before every two-enemy group and a no-aggro rest/checkpoint between `SealedCourtyard` and `BossHall`.

- [ ] **Step 4: Fill the twelve gameplay spaces**

Connect each named challenge to its actual geometry/mechanism/encounter contract. Tests must reject a challenge node that contains only a backdrop, landmark or renamed legacy micro-scene. Preserve multiple grapple choices in the ascent and a noncombat bypass through at least one optional loop.

- [ ] **Step 5: Rebuild scenes and run GREEN**

Run the Task 4 leaf-to-parent builder order, then focused structure/encounter/runtime tests. Mutate one branch group to required and collapse one activation batch; the corresponding tests must fail.

- [ ] **Step 6: Commit**

```powershell
git add tools/build/prologue/CanyonEncounterCatalog.cs tools/build/prologue/CanyonEncounterDefinition.cs tools/build/prologue/CanyonContentBuilder.cs tools/build/prologue/CanyonContentCatalog.cs scripts/prologue/CanyonEncounterGroup.cs scripts/prologue/guidance/PrologueGuidanceController.cs scripts/prologue/seamless/CanyonWorldLayout.cs scenes/prologue/sectors/CanyonEntrance.tscn scenes/prologue/sectors/CanyonDescent.tscn scenes/prologue/sectors/CanyonBasin.tscn scenes/prologue/sectors/CanyonAscent.tscn scenes/prologue/sectors/LostTemple.tscn scenes/prologue/PrologueLevel.tscn tests/unit/PrologueCanyonStructureTests.cs tests/unit/CanyonEncounterCatalogTests.cs tests/integration/ContinuousCanyonEncounterTests.cs tests/RunContinuousCanyonTests.cs
git commit -m "feat: populate continuous canyon encounters"
```

---

### Task 7: Add Stable Checkpoint IDs and Side-by-Side Layout Migration

**Prerequisite:** Reconcile against the merged P0 flow/continue fix. Preserve its corrected recovery, completion and checkpoint transaction ordering.

**Files:**
- Modify: `scripts/save/PrologueSaveData.cs`
- Modify: `scripts/save/PrologueSaveStore.cs`
- Modify: `scripts/prologue/PrologueProgress.cs`
- Modify: `scripts/prologue/PrologueCheckpointSnapshot.cs`
- Modify: `scripts/prologue/CheckpointTrigger.cs`
- Modify: `scripts/prologue/seamless/CanyonWorldLayout.cs` (checkpoint ID registry and known persistable IDs only)
- Modify: `tools/build/prologue/CanyonContentBuilder.cs` (checkpoint authoring only)
- Modify: `scripts/prologue/seamless/SeamlessPrologueWorld.cs`
- Modify: `scripts/prologue/PrologueLevelController.cs`
- Modify: `scripts/prologue/PrologueGameFlowController.cs`
- Modify: `scripts/ui/PrologueMainMenu.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tests/unit/PrologueSaveTests.cs`
- Modify: `tests/unit/PrologueProgressTests.cs`
- Create: `tests/integration/ContinuousCanyonMigrationTests.cs`
- Modify: `tests/integration/ContinuousCanyonSaveIsolationTests.cs` (reconcile retained Task4 assertions with explicit migration choices; preserve slot/sample isolation)
- Modify: `tests/RunContinuousCanyonTests.cs`
- Regenerate from the owned builder: `scenes/prologue/PrologueLevel.tscn`
- Regenerate affected leaves with existing builders before parent: `scenes/prologue/sectors/CanyonBasin.tscn`, `scenes/prologue/sectors/LostTemple.tscn`

**New save shape:**

```csharp
public const int CurrentVersion = 3;
public int LayoutVersion { get; set; } // zero means absent/legacy, never inferred as current
public string CheckpointId { get; set; } = "canyon.v2.start";
public List<string> OpenShortcuts { get; set; } = [];
public List<string> ActivatedMechanisms { get; set; } = [];
public List<string> Discoveries { get; set; } = [];

public static PrologueSaveData NewLayoutV2() => new()
{
    Version = CurrentVersion,
    LayoutVersion = CanyonWorldLayout.CurrentLayoutVersion,
    CheckpointId = "canyon.v2.start"
};
```

Use layout-v2 path `user://knight/prologue-save-layout-v2.json`; retain legacy path `user://knight/prologue-save.json`. Valid checkpoint IDs and exact authored positions are `canyon.v2.start`, `canyon.v2.basin-camp`, and `canyon.v2.temple-forecourt` from `CanyonWorldLayout`.

Reconcile the current registry's older `canyon.v2.checkpoint.*` authoring names with these three exact IDs in both the registry and active builders, preserving authored positions `(160,600)`, `(1680,3960)`, `(5920,1080)` and their sectors. Basin/Temple serialized leaf triggers must emit the new IDs; rebuilding only the parent is insufficient. The Entrance has a Spawn marker, not an activated checkpoint trigger. Preserve the independent legacy/sample paths and the single explicit legacy mapping boundary; unknown IDs are not silently repaired.

- [x] **Step 1: Write migration RED tests**

Require: schema and layout versions are independent; JSON with no camelCase `layoutVersion` token remains layout 0 even when all other fields look current; arbitrary coordinates are rejected/ignored; a V2 CanyonBasin checkpoint maps to `canyon.v2.basin-camp`; V2 LostTemple maps to `canyon.v2.temple-forecourt`; entrance/no-checkpoint maps to `canyon.v2.start`; existing discoveries/tutorials and an explicit true Boss result survive; no absent Boss result is synthesized; unknown/invalid legacy checkpoint yields a choice, not an overwrite; old bytes remain unchanged after load, choice, new save and failed write.

Test both menu choices: “Continue from mapped checkpoint” and “Start continuous canyon (old save kept)”. Before a choice, assert neither `SaveAtomic` nor the new slot write occurs. Independently verify the Basin sample keeps its explicitly injected save path and cannot read/write the full-prologue slots.

This task closes Task 5's disk-persistence dependency: activate the actual shortcut and satisfy both real seal prerequisites, save, dispose the run, then Continue with a fresh world/progress instance. Assert both shortcut directions stay open, the seal remains open in collision/visual/graph state, and no additional enemy-defeat/reward event is emitted. A run saved with only one seal prerequisite must not acquire the completed-seal activation. Reject unknown `ActivatedMechanisms` IDs as well as unknown shortcut/discovery/checkpoint IDs.

- [x] **Step 2: Run RED**

Expected after Task 4: any new load-result/menu members first provide interface evidence only; after inert shells and a successful rebuild, the actual focused process exits nonzero because V2 data still has enum sector/coordinates and one shortcut boolean and no migration-choice flow exists. Missing-layout JSON must already fail as legacy/unknown, not pass by receiving a default 2.

- [x] **Step 3: Implement a typed load result and explicit mapping**

```csharp
public enum PrologueSaveLoadStatus
{
    Current, MigratableLegacy, NeedsNewLayoutChoice, Recovered
}

public sealed record PrologueSaveLoadResult(
    PrologueSaveLoadStatus Status,
    PrologueSaveData Data,
    string PreservedLegacyPath);
```

Put the only V2→layout-v2 mapping table in `PrologueSaveStore`; never translate legacy world coordinates. Parse the camelCase wire key `version` and presence/value of `layoutVersion` from `JsonDocument.RootElement` before deserializing into `PrologueSaveData`; only the explicit integer value 2 is the current full-world layout. Missing, null, noninteger, zero and unknown values route to the full-world legacy/choice/rejection flow and can never be repaired into 2 by `Normalize()`. The independent Basin sample stays on its injected path and legacy authored-coordinate rules; it must not receive the continuous-world migration menu or blanket layout-2 rejection. Normalize layout-v2 IDs against the exact known persistable-ID registry in `CanyonWorldLayout`, rejecting unknown shortcut/mechanism/discovery/checkpoint IDs. Preserve the existing graph-only `StableMechanismIds`/`StableShortcutIds` topology invariants; narrowly separate persistable sets may include the actual authored controls and named Main7 seal prerequisite, never arbitrary ordinary kills. `CheckpointTrigger.EntranceId` becomes the stable checkpoint ID carried by its signal; the world resolves the authored position and publishes checkpoint ID, sector and position atomically.

- [x] **Step 4: Add the non-destructive menu choice**

Present the mapping choice only when the load result requires it. “Start” initializes a new in-memory layout-v2 save; “Continue mapped” uses the mapped authored checkpoint. Both preserve the old slot and write only the new slot after the player explicitly starts/continues. Do not add a silent timeout/default.

Retain the existing save-isolation suite in the default runner. Its pre-migration “legacy Continue unavailable” assertion must now distinguish an offered mapping choice from a committed Continue; no read or unselected choice may write. An explicit new start may create the new slot, but malformed/unknown bytes already occupying that slot must be preserved recoverably before replacement, never silently discarded. Keep legacy-byte, read-only current Continue, failed-write and independent injected-path sample assertions.

- [x] **Step 5: Run GREEN plus byte-level preservation checks**

Regenerate `PrologueLevel.tscn` from the owned `BuildPrologueLevel` after its code changes, with a fresh successful build before the actual builder process. Run focused migration and P0 continue/recovery/completion tests against the generated level. Record SHA-256 of the legacy fixture before/after every case. Mutate the mapping to apply legacy coordinates; the named coordinate-rejection test must fail.

First regenerate the affected Basin and LostTemple leaves from their existing wrappers, each with a fresh successful build; verify the real serialized triggers publish the exact stable IDs before the parent/runtime migration tests.

- [x] **Step 6: Commit**

```powershell
git add scripts/save/PrologueSaveData.cs scripts/save/PrologueSaveStore.cs scripts/prologue/PrologueProgress.cs scripts/prologue/PrologueCheckpointSnapshot.cs scripts/prologue/CheckpointTrigger.cs scripts/prologue/seamless/CanyonWorldLayout.cs tools/build/prologue/CanyonContentBuilder.cs scripts/prologue/seamless/SeamlessPrologueWorld.cs scripts/prologue/PrologueLevelController.cs scripts/prologue/PrologueGameFlowController.cs scripts/ui/PrologueMainMenu.cs tools/build/prologue/BuildPrologueLevel.cs tests/unit/PrologueSaveTests.cs tests/unit/PrologueProgressTests.cs tests/integration/ContinuousCanyonMigrationTests.cs tests/integration/ContinuousCanyonSaveIsolationTests.cs tests/RunContinuousCanyonTests.cs scenes/prologue/sectors/CanyonBasin.tscn scenes/prologue/sectors/LostTemple.tscn scenes/prologue/PrologueLevel.tscn
git commit -m "feat: migrate prologue saves to canyon layout v2"
```

---

### Task 8: Add Reachable, Temporary Firefly Guidance

**Files:**
- Create: `scripts/prologue/guidance/CanyonFireflyGuidanceModel.cs`
- Create: `scripts/prologue/guidance/CanyonFireflyGuidanceController.cs`
- Create: `scripts/prologue/guidance/CanyonFireflyGuidanceView.cs`
- Create: `scripts/prologue/guidance/CanyonCombatTelegraphAdapter.cs`
- Modify: `scripts/prologue/guidance/PrologueGuidanceController.cs`
- Modify: `scripts/ui/WorldTargetMarkerVisual.cs` (markerless first-act card layout only)
- Modify: `assets/localization/prologue-ui.json`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Create: `tests/unit/CanyonFireflyGuidanceModelTests.cs`
- Create: `tests/integration/CanyonFireflyGuidanceRuntimeTests.cs`
- Modify: `tests/integration/PrologueGuidanceRuntimeTests.cs`
- Modify: `tests/RunContinuousCanyonTests.cs`
- Regenerate from the owned builder: `scenes/prologue/PrologueLevel.tscn`

**Model contract:**

```csharp
public readonly record struct CanyonFireflyInput(
    bool GuidanceEnabled, bool Paused, bool CombatTelegraphActive,
    bool AtImportantFork, bool BossExitAvailable, bool RouteProgressed,
    bool HasReachableMarker);

public readonly record struct CanyonFireflyFrame(
    bool Visible, float VisibleSecondsRemaining, float CooldownSecondsRemaining);
```

Defaults: stalled trigger 18 seconds, visible duration 3 seconds, cooldown 15 seconds. All are elapsed active-play seconds.

- [x] **Step 1: Write timing/reachability RED tests**

Test exact below/at threshold behavior, one visible hint at a time, cooldown, pause freeze, combat-telegraph deferral, guidance-off suppression, no repeat after route progress, Boss-exit hint and cleanup on completion/title/recovery/new. Route resolution must reject closed gates, `Ice`, `Forbidden`, unloaded chunks and markers whose support region is not reachable from the current route node.

Exercise the actual attack publishers of RustShellSquire, BellBackLaborer, GraveLampMoth and FallenBellkeeper. Include a Boss sweep whose coarse state is still PhaseOne and a pre-lock Telegraph where DangerVisible is false; both must defer guidance. Recovery/idle and unmounted or out-of-encounter actors must not leave a stale deferral. A fabricated frame alone cannot certify the runtime adapter.

- [x] **Step 2: Run RED**

Expected first: compiler/interface evidence for missing firefly types. Add inert compile-safe shells, rebuild to exit 0, then require the actual focused process to exit nonzero on timing/reachability behavior before implementation. Do not treat the missing-type compile error as the behavioral RED.

- [x] **Step 3: Implement the pure model and runtime adapter**

The controller reads the existing first-act guidance toggle and mounted, locally relevant `ICanyonTelegraphSource` mechanism nodes. `CanyonCombatTelegraphAdapter` uses a small local type switch over the existing four actors and reads each actual `CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph`; it does not change enemy APIs or infer windup from coarse state or the optional visual's DangerVisible. Scope actor polling to the current activated encounter/Boss arena, resolve membership after sector changes, and discard invalid/unmounted references. OR these actual enemy and mechanism signals into CombatTelegraphActive. This reuses the existing guidance polling model and avoids a second event-subscription registry.

The controller queries `CanyonRouteGraph.Reachable` with the actual `OpenShortcuts`, `ActivatedMechanisms` and Boss result, and chooses only an authored, currently mounted route marker. A closed seal blocks BossHall guidance even if another shortcut is open. The visual receives a world-space polyline/target and has no collider, save writes or gameplay authority. Mount it once under `WorldRoot/FireflyGuidance`, outside streamed sectors, so sector retirement cannot duplicate it.

The shared parent is also used by the preserved layout-1 Basin sample. Enable this new route-graph controller only for `SeamlessPrologueWorld.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion`; layout1 must remain inert and retain its existing sample guidance. Verify the actual sample entry after regenerating the parent does not start a layout2 hint or resolve its old coordinates through the new graph. This mode gate belongs to the new controller and its owned runtime tests, not a duplicate sample-entry patch.

- [x] **Step 4: Replace stale claw teaching copy**

Update Chinese and English first-act teaching to `E → click → automatic pull → Space release`. Remove any statement that W/S or held LMB is required. Do not expand the temporary firefly into a permanent instruction card.

The normal game's existing teaching card has a separate confirmed layout defect: with a genuine pending lesson but no local target (for example LearnAttackMode after leaving Entrance), WorldTargetMarkerVisual anchors its long wrapped card near screen center. Fix this in the now-owned view with safe-edge markerless first-act placement; builder-only coordinates are overridden each Present. Preserve actual pending objective/progress, full text/glyphs, targeted marker placement and layout1 sample guidance. Do not fabricate a marker, force lesson completion or merely hide required controls. Reuse existing protection/placement helpers without expanding their ownership. If no safe position exists, preserve explicit no-placement safety and restore the pending instruction when a legal edge returns.

Add a behavioral RED and normal-entry regression for that real pending/off-owner state, available safe-edge placement outside central action/player/landing/HUD protected regions, both languages and existing resolution/UI-scale support. Preserve wrapped-label/no-ellipsis containment, same-owner missing-target warnings and unknown/dynamic target semantics. Verify no-safe-placement then reappearance and the actual sample entry. Review moving before/after UI footage; geometric assertions alone do not certify beauty. The bounded cause/ownership rationale is in the Task8 execution notes.

- [x] **Step 5: Run GREEN and route-gate mutations**

Regenerate `PrologueLevel.tscn` from the owned `BuildPrologueLevel` so its world-level guidance wiring is present in the actual game, with a fresh successful build before the actual builder process. Run model, generated-level runtime and existing localization/guidance suites. Close the shortcut, remove completed-seal activation, and mark the nearest surface Ice in separate mutations; each must make the corresponding marker ineligible without borrowing another gate's flag.

Register the Task8 model, generated runtime, markerless-card and retained guidance coverage in the no-argument continuous runner as well. Focused flags are supplementary entry points, not the only way those tests execute. Reconcile obsolete owned guidance-fixture coordinates/paths without weakening their behavioral assertions or assigning unresolved failures to an unowned later task.

- [x] **Step 6: Commit**

```powershell
git add scripts/prologue/guidance/CanyonFireflyGuidanceModel.cs scripts/prologue/guidance/CanyonFireflyGuidanceController.cs scripts/prologue/guidance/CanyonFireflyGuidanceView.cs scripts/prologue/guidance/CanyonCombatTelegraphAdapter.cs scripts/prologue/guidance/PrologueGuidanceController.cs scripts/ui/WorldTargetMarkerVisual.cs assets/localization/prologue-ui.json tools/build/prologue/BuildPrologueLevel.cs tests/unit/CanyonFireflyGuidanceModelTests.cs tests/integration/CanyonFireflyGuidanceRuntimeTests.cs tests/integration/PrologueGuidanceRuntimeTests.cs tests/RunContinuousCanyonTests.cs scenes/prologue/PrologueLevel.tscn
git commit -m "feat: guide continuous canyon routes with fireflies"
```

---

### Task 9: Establish Replaceable Art Layers and Traversal Readability

**Stage accepted 2026-09-21:** initial89706a4 + fix1d19b142; scoped independent review confirms the first-seam foreground defect and live encounter-after-art-removal proof gap addressed with no new Critical/Important breakage. Root fresh default53288/0→26084/0 includes both new checks,0builddiagnostics/0engineerrors or leaks,with8known negative warnings retained. This accepts replaceable staging/readability, not final beauty, whole-motion/audio, full normal-Start combat/exit or human/performance gates. Keep the unresolved historical AscentGrip8 failure assigned toTask10 diagnostic and the remaining illustration/actor/UI polish toTask11.

**User reinforcement, 2026-09-20:** visual beauty is an independent acceptance requirement, not merely the presence of replaceable layers. Preserve the mechanical canyon/ruined-temple direction while improving coherent materials, restrained lighting, foreground/midground/background depth and landmark composition. Avoid noisy microdetail, stretched textures and scenery/effects that compete with actors, telegraphs, safe supports or grapple surfaces. Compare representative normal-window scenes before/after and inspect in motion; automated structural checks alone cannot certify polish. Overall character/enemy animation and UI typography/icon/motion consistency must also be reviewed in Task 11, with concrete defects routed to their owners before delivery rather than silently considered outside scope. No paid generation is authorized by this reminder.

**Files:**
- Modify: `tools/build/prologue/CanyonContentBuilder.cs`
- Modify: `scripts/presentation/CanyonBackdropVisual.cs`
- Modify: `scripts/presentation/CanyonPlatformVisual.cs`
- Modify: `scripts/presentation/RoomFormalVisual.cs`
- Modify: `scripts/presentation/RoomVisualCatalog.cs`
- Modify: `tests/unit/CanyonMaterialSkinTests.cs`
- Create: `tests/integration/ContinuousCanyonReadabilityTests.cs`
- Modify: `tests/RunContinuousCanyonTests.cs`
- Regenerate the five sector outputs already listed in Step 5: `scenes/prologue/sectors/CanyonEntrance.tscn`, `CanyonDescent.tscn`, `CanyonBasin.tscn`, `CanyonAscent.tscn`, `LostTemple.tscn`

- [x] **Step 1: Write visual-boundary RED tests**

Require every sector to retain `VisualRoot/Background`, `Midground`, `GameplayReadable`, `Foreground`, `Effects` and separate `Gameplay` collision/mechanism/route-marker subtrees. Verify that hiding or replacing Background/Midground/Foreground does not remove collision, grapple profiles, telegraphs, route markers or encounter activation. Foreground occlusion rectangles must not cover the player body, supporting floor or next required grapple surface in the authored camera probes.

Add composition assertions that the twelve challenges use at least three height layers and distinct silhouettes/landmarks, while explicitly refusing to count backdrop palettes as challenge types.

- [x] **Step 2: Run RED**

Expected: expanded spaces lack authored formal layers/occlusion contracts.

- [x] **Step 3: Reuse existing licensed assets through stable adapters**

Visually verify candidate pixels before reuse: root inspection on2026-09-20 found that `assets/environment/nameless-belfry/environment-atlas.png` is striped palette swatches, `mechanisms.png` is simple prototype symbols, and the player atlas is placeholder tiles; current enemy/Boss atlases contain simple flat component silhouettes. Their manifest `final: true` does not establish finished artwork. Reuse these only where their actual content is suitable (for example palette or prototype symbols), not as mandatory illustrated scenery or an automatic actor upgrade. Use genuinely suitable existing local licensed art, the room-layer JSON and `CanyonBackdropVisual`, `CanyonPlatformVisual`, `RoomFormalVisual`, `RoomVisualCatalog` for coherent replaceable staging without copying source textures into new folders. Preserve all original files and keep collisions and surface materials authored independently. Record unavailable illustration needs explicitly instead of stretching swatches, fabricating completed-art claims or adding unapproved paid generation. This corrects the prior blind-atlas-reuse assumption without expanding environment ownership into actor/UI production.

Use environmental landmarks to hand off direction between firefly moments: broken bell at entrance, crane silhouette above the abyss, lit basin camp, moving counterweight, upper-gallery bell frame and sealed temple façade. Ensure the abyss brake walls and all hazard telegraphs remain higher-contrast than scenery.

- [x] **Step 4: Run GREEN and art-removal proof**

Run focused structure/readability/material tests with normal assets, then with the replaceable visual subtrees hidden. Gameplay contracts must still pass; screenshot/presentation checks may differ but must keep player/support/target visibility.

- [x] **Step 5: Commit**

```powershell
git add tools/build/prologue/CanyonContentBuilder.cs scripts/presentation/CanyonBackdropVisual.cs scripts/presentation/CanyonPlatformVisual.cs scripts/presentation/RoomFormalVisual.cs scripts/presentation/RoomVisualCatalog.cs scenes/prologue/sectors/CanyonEntrance.tscn scenes/prologue/sectors/CanyonDescent.tscn scenes/prologue/sectors/CanyonBasin.tscn scenes/prologue/sectors/CanyonAscent.tscn scenes/prologue/sectors/LostTemple.tscn tests/unit/CanyonMaterialSkinTests.cs tests/integration/ContinuousCanyonReadabilityTests.cs tests/RunContinuousCanyonTests.cs
git commit -m "feat: stage readable canyon art layers"
```

---

### Task 10: Prove Route Connectivity, Recovery and Persistent Progress

**Files:**
- Create: `tests/presentation/ContinuousCanyonRouteProbe.cs`
- Create: `tests/RunContinuousCanyonRouteProbe.cs`
- Modify: `scripts/prologue/ProloguePlaytestRecorder.cs`
- Modify: `scripts/prologue/ProloguePlaytestSession.cs`
- Modify: `scripts/prologue/ProloguePlaytestReport.cs`
- Modify: `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`
- Modify: `tests/integration/SeamlessPrologueWorldTests.cs`
- Modify: `tests/integration/PrologueGameFlowTests.cs` (current normal-game fixture reconciliation; retain all five cases and frozen-sample independence)
- Modify conditionally: `tests/integration/PrologueCanyonTraversalTests.cs` (the observed `AscentGrip8` recording failure's narrow per-tick diagnostics/verbose-trace gating, plus exposing existing low-level traversal/input/frame helpers for Task10; preserve helper bodies, assertions and legacy Task4 driver/callers)
- Create conditionally: `tests/ExpectedWarningLogger.cs` (test-only thread-safe observation and exact expected-warning assertions; no production logging suppression)
- Modify conditionally: `tests/RunContinuousCanyonTests.cs` (the warning observer's registration, narrowly scoped expected-warning sets, focused guard coverage and cleanup; retain all existing suites and default coverage)
- Modify conditionally: `tests/unit/ProloguePlaytestSessionTests.cs` (current-schema assertions, explicit v3-to-v4 migration with empty new telemetry, malformed-v3 Falls rejection and lifecycle/retry event dedup regressions; preserve v1/v2 migration, fail-closed and exact-field round trips)
- Modify conditionally, persistence setup/teardown only: `tests/integration/ProloguePlaytestRecorderTests.cs`, `PrologueLevelRuntimeTests.cs`, `PrologueFormalHudTests.cs`, `PrologueProductionAssetRuntimeTests.cs`, `PrologueAccessibilityTests.cs`, `ThreadCastRuntimeTests.cs`, `PrologueVfxRuntimeTests.cs`, `PlayerFormalVisualTests.cs` (thirteen full-post mount sites/fourteen instances, plus the same-file Task10 accessibility entry; retain all cases/assertions)
- Create conditionally: `tests/IsolatedPrologueFixture.cs` (test-only off-tree external persistence-path helper; no global NodeAdded interception or production changes)
- Additional conditional scope within `tests/integration/ProloguePlaytestRecorderTests.cs`: actual recorder binding/Continue/restored-baseline/retry/dynamic-sector/recovery-shortcut regressions from the independent telemetry pre-review. Preserve all registered cases, headless/human-evidence authorization boundaries and isolated persistence; do not replace real binding coverage with only manual session additions.
- Modify conditionally: `tools/build/prologue/CanyonContentBuilder.cs` and regenerated `scenes/prologue/sectors/CanyonDescent.tscn`, only the archive-to-frozen-detour LaunchZone metadata proven inconsistent with the natural actual one-jump path. Preserve its ID, SingleJump kind, landing, all collision/art/encounter nodes and other contracts; first assert real takeoff containment against old metadata, then fix and re-prove it.
- Modify: `tests/RunPrologueTests.cs`

Additional narrowly ruled geometry ownership: `CanyonContentBuilder.cs` and regenerated `scenes/prologue/sectors/CanyonBasin.tscn` for GearPistonOne's left extension only, plus its single matching `ProtectedWorldProbes` rectangle in `tests/integration/ContinuousCanyonReadabilityTests.cs`. Read `task-10-gear-entry-geometry-ruling.md` for the exact2420,3916,300,80 rectangle, retained boundaries, three actual-input launch positions, RED/guard/regression/render/review requirements and prohibited changes. This responds to the measured held-shield single-jump failure, not a grant to redesign all geometry or weaken old helper assertions.

**Probe rule:** Use two separately identified normal-Start full playthroughs, each with fresh isolated persistence and one persistent `PlayerRoot`: main via the full ControlledAbyss descent and direct lift path; exploration via Archive/Frozen to Basin and the Steam branch, including the persistent shortcut both ways. Both independently finish Boss and the physical SafeExit. Their explicit coverage union must prove all twelve challenge contracts and eight main/four branch encounters; do not claim one uninterrupted all-content run or combine their timing. This follows Task1's authored forward split/rejoin topology, which has no reverse-abyss contract. Read `task-10-forward-fork-coverage-ruling.md` in the SDD directory for exact paths and retained obligations. Each case uses real input/action APIs. It may instrument state and abort safely, but it may not set `GlobalPosition`, invoke checkpoint activation directly, kill enemies by API or mark gates/discoveries complete. Focused recovery/save cases may start from an authored checkpoint only after separately proving the checkpoint was activated and persisted.

Encounter attribution follows the approved optional-combat bypass: the main case proves all eight main encounters, the exploration case proves all four optional encounters, and common encounters actually entered must be handled truthfully. The Steam route's east lift landing need not invent a westward reverse path solely to repeat the noncritical Counterweight encounter already proved on the main route. All onward gate/Boss/exit prerequisites still apply in each case. Prove all six functioning mechanism families in applicable explicit traces; node visitation does not replace real operation/ride evidence.

**Persistence isolation:** The pinned engine ignored both attempted OS-directory overrides. Use the existing off-tree absolute SavePath, SettingsPath and PlaytestRecorder.OutputDirectory exports before AddChild, with a unique external case root, containment/reparse/grant guards and unchanged byte inventories of default user://knight, user://playtests and user://logs after teardown and process exit. Legacy loading must stay beside the injected save. Route engine logs externally. Log actual OS.GetUserDataDir truthfully; do not claim it was redirected. Do not alter project.godot/shared override.cfg or production defaults. See `task-10-persistence-isolation-ruling.md` in the SDD handoff directory for the audited writers and exact guard contract. Every fresh Continue scene must be configured before its settings initializer can write.

The existing prerequisite-injected recording had an unexplained `AscentGrip8` failure: failed and successful rendered logs agree through local frame 0 (`Aim`, valid target), then diverge by frame 5 (`Idle`/cleared versus `Travel`). Preserve that failure; passing reruns do not explain it. The conditional helper ownership permits only opt-in per-tick diagnostics around press/release and the first few physics steps, using actual input/claw/contact and available flow/health/pause state, plus gating pre-existing verbose traces. Capture exact source/DLL/argv/process provenance for a comparable rendered run. Determine the cause before any behavior change; a production dependency needs a separate minimal ownership assignment. Do not use that prerequisite-injected diagnostic as the full normal-Start proof or infer physics duration from movie FPS.

The first normal-Start route baseline stopped at the intended courtyard seal after all ascent catches: the reused Task4 traversal-only driver never defeated the split-level group or operated the real linkage. The linkage/control are correctly authored in CanyonAscent, not LostTemple; do not regenerate/remove the gate based on their absence from the Temple file. Replace only Task10's legacy-driver call with full input-driven route orchestration in its owned route fixture/probe. Existing low-level helpers may become internal or gain thin wrappers, but retain their behavior/assertions and all old callers. The new orchestration must perform combat, mechanisms, branches and shortcut travel rather than injecting prerequisites. This is a test-driver coverage gap, not evidence of a missing production control.

The064626 actual-input candidate reached FrozenLeft directly after one jump from ArchiveFloor atx1496.051, clearing its left edge and steering back, with no position/velocity injection. Its right-side authored LaunchZone1660..1840 does not contain that natural takeoff; an expected intermediate FrozenCeiling landing was also disproven. Require direct real target support, one accepted jump/no dash/no claw/no recovery, and a focused failing real-takeoff containment assertion before the conditional metadata correction. Derive the left takeoff window from that path and safe floor bounds; do not touch a remote launch zone merely to mark it visited. Review the regenerated semantic diff to ensure only this contract changed. No collision geometry or combat rebalance is authorized by this evidence.

Reconcile the retained `PrologueGameFlowTests` normal-level fixtures with the accepted schema3/layout2 stable-ID contract and authored positions. They load the current main scene, not the frozen Basin sample. Retain all five cases and the best-metric, settings, real-death, terminal-state, presentation and corruption assertions; do not skip them or globally pin normal tests to old geometry. Genuine legacy coverage must explicitly exercise legacy wire data and the migration choice. Task7's generated P0 tests supplement, not replace, this final-runner obligation.

Before full post, correct the thirteen production-default persistence mount sites/fourteen instances in the eight conditionally owned integration fixtures, plus PrologueAccessibilityTests.Task10ProductionEnglishFlowsToPause reachable from the separate Task10 runner. Inject case-unique absolute SavePath, SettingsPath and Recorder.OutputDirectory before AddChild; settings initialization writes immediately. Preserve all fifteen instances and existing assertions. Reject a raw relative storage argument before normalization, and check existing reparse ancestors of the full case path before its first directory creation as well as afterwards. The user-owned first BasinAscentCameraRuntimeTests call is already isolated and must not be changed. Read `task-10-full-regression-storage-audit.md` in the SDD directory for exact cases and limits; the currently guarded normal-route probe may continue, but full post must wait for these fixes and retain whole-process default-data byte guards.

- [ ] **Step 1: Write full-route RED assertions**

Record ordered visits per named main/exploration playthrough and assert their explicit combined coverage of all main nodes, both branch alternatives, all twelve challenge contracts, eight main and four branch encounter activations/real defeats, and the persistent shortcut in both directions. Each playthrough must independently prove Boss defeat and the physical safe exit with one PlayerRoot. Visiting the abyss bottom from a branch does not prove its full Commitment-to-bottom long descent. Record chunk crossings (including required bidirectional coverage), `PlayerRoot` instance ID, mounted sectors, resident grapple target/chunk, camera delta, mechanism state and recovery transaction ID. Do not add a graph-only reverse edge or unplanned wall-climb to force both descent alternatives into one run.

Fail on fade/level card/teleport during ordinary travel; target chunk retirement before claw `false`; duplicate death/fall recovery; checkpoint advance without activation; optional branch gating the main route; completion while player can still fall; or a route-marker target that is unreachable.

- [ ] **Step 2: Run RED**

Tasks 1–9 are already implemented at this point; do not claim that this run tests an old layout or roll back those tasks. First obtain a focused behavioral RED for the new recorder's missing ordered route/encounter/mechanism records (add inert, compile-safe telemetry members if needed). Separately run the actual-input full route to reveal remaining integration failures. If that route already succeeds, preserve its GREEN baseline instead of manufacturing a failure; missing telemetry is this task's intentional RED. Every genuine integration failure must retain its reproduction and be assigned to the owning implementation slice, never hidden by relaxing route assertions.

- [ ] **Step 3: Extend telemetry without manufacturing timing claims**

Bump `ProloguePlaytestReport` only if its serialized shape changes. Add route-node enter/exit, encounter activation/defeat, mechanism activation, shortcut direction and firefly target records with monotonic timestamps. Do not compute or label 20–30/30–45 minutes as passed from the automated probe.

The new five serialized telemetry collections require report schema4. The two existing unit expectations hardcoded to schema3 must advance with the report; test an actual schema3 wire fixture migrating to schema4 with its existing fields preserved and all new telemetry empty. This is independent of save-data schema3/layout2; do not alter save schemas or drop older report migrations.

Independent telemetry pre-review found four correctness gaps to close before acceptance: restored completed encounters/mechanisms must not become fresh current-run events; real events must cover existing/delayed mounted sectors and lifecycle cleanup; physical shortcut traversal must require open uninterrupted corridor entry/exit and cancel on recovery; stable-ID dedup must preserve true reset/reactivation/retry events. V3 missing/null Falls remains invalid even after migration (only V2 may synthesize that absent field). Read `task-10-telemetry-prereview.md` in the SDD directory and obtain focused behavioral RED→GREEN using the actual recorder observation/binding path. Keep automated fixtures labeled as such and the production headless/HandsOn authorization gate unchanged. Manual session samples alone are not this proof.

- [ ] **Step 4: Run focused GREEN, then full regressions**

```powershell
$taskDotnetRoot = 'G:/godot/dotnet-sdk'
$env:DOTNET_ROOT = $taskDotnetRoot
$env:DOTNET_ROOT_X64 = $taskDotnetRoot
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:PATH = "$taskDotnetRoot;$env:PATH"
$runId = 'task10-{0}-{1}' -f (Get-Date -Format 'yyyyMMddTHHmmssfff'),([Guid]::NewGuid().ToString('N'))
$evidenceRoot = Join-Path 'E:/OurGame/Knight/.evidence/september09-continuous-canyon' $runId
New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
$runs = @(
  @{ Name='contracts'; Script='res://tests/RunContinuousCanyonTests.cs'; ScriptArgs=@() },
  @{ Name='route'; Script='res://tests/RunContinuousCanyonRouteProbe.cs'; ScriptArgs=@("--case-storage-root=$evidenceRoot/route-storage") },
  @{ Name='prologue-pre'; Script='res://tests/RunPrologueTests.cs'; ScriptArgs=@('--phase=pre', "--case-storage-root=$evidenceRoot/pre-storage") }
)
foreach ($run in $runs) {
  & "$taskDotnetRoot/dotnet.exe" build ./Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo 2>&1 |
      Tee-Object -FilePath (Join-Path $evidenceRoot "$($run.Name).dotnet-build.log")
  if ($LASTEXITCODE -ne 0) { throw "$($run.Name) build failed; Godot was not started." }
  $args = @('--headless','--path','.', '--script',$run.Script,
    '--log-file',("$evidenceRoot/$($run.Name).log"))
  if ($run.ScriptArgs.Count -gt 0) { $args += @('--') + $run.ScriptArgs }
  $process = Start-Process -FilePath 'G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe' -ArgumentList $args -WindowStyle Hidden -PassThru
  $process.WaitForExit()
  [IO.File]::WriteAllText((Join-Path $evidenceRoot "$($run.Name).actual-exit-code.txt"),
    $process.ExitCode.ToString([Globalization.CultureInfo]::InvariantCulture))
  if ($process.ExitCode -ne 0) { throw "$($run.Name) exited $($process.ExitCode)" }
}
```

Create each case-unique external storage directory before its process starts. Rebuild generated leaf scenes and the parent level, then run `RunPrologueTests --phase=post --case-storage-root=<new absolute external post directory>` through the guarded wrapper. Require build 0 warnings/errors, success sentinels, no engine error, leak/orphan signature, unhandled exception or unexpected warning. Normal-input route/capture runs must have no engine warnings. Deliberate negative-test warnings are allowed only inside explicitly named expected-warning scopes with exact message/origin/count assertions; keep raw stdout/stderr unchanged and report expected diagnostics separately rather than calling them absent.

**Warning-hygiene ruling, 2026-09-21:** local Godot4.7.1 `Logger._LogError` plus `OS.AddLogger/RemoveLogger` can observe internal warnings; it provides no handled/consume return contract. Keep production warnings and all negative assertions intact. A test-only observer may record both code/rationale and source/type under synchronization, then assert on the main test flow (never log or throw from the callback). Confirm the actual payload with a focused run, detach/dispose in reliable cleanup, and prove missing/extra warnings fail the guard. The currently accepted continuous default deliberately emits exactly six `Rejected prologue flow transition Title->Playing.`, one `Rejected prologue flow transition Completed->Recovering.`, and one `MechanicalClawController isolated a throwing ClawFrameChanged observer: intentional Task7 observer fault`; focused and other runners must scope expectations to the cases they actually execute, not permit that global multiset everywhere. See the bounded API/origin note for exact emitters. No global output mute, raw-log filtering, production diagnostic flag, or broader production-file ownership is authorized. This resolves the previous contradictory demand for zero raw warnings while retaining intentionally warning-emitting negative cases; any genuinely unexpected diagnostic remains a failure.

The command sketch above supplies runner selection and build ordering, not complete evidence capture. Use a recorded wrapper that also captures actual build/engine PIDs, argv, environment, source/DLL hashes, unedited stdout/stderr and protected default application-data inventories before/after every process, including failure. Do not run full post before the audited off-tree fixture fixes. Never substitute the ignored OS-directory flags for these guards.

Run the focused suites twice, and repeat each main/exploration full-route variant twice with fresh case-unique external application-persistence storage to expose persistence/order coupling. Identify the variant and iteration in every artifact; never use branch-only bottom visitation as main-long-fall proof. The actual OS user directory need not move; the verified save/settings/recorder paths and unchanged default-data byte inventories are the isolation proof.

- [ ] **Step 5: Commit**

```powershell
git add tests/presentation/ContinuousCanyonRouteProbe.cs tests/RunContinuousCanyonRouteProbe.cs scripts/prologue/ProloguePlaytestRecorder.cs scripts/prologue/ProloguePlaytestSession.cs scripts/prologue/ProloguePlaytestReport.cs tests/integration/PrologueCanyonGeneratedMainRouteTests.cs tests/integration/SeamlessPrologueWorldTests.cs tests/integration/PrologueGameFlowTests.cs tests/RunPrologueTests.cs
# If changed for the scoped diagnostic above, stage only that helper's owned changes too:
# git add tests/integration/PrologueCanyonTraversalTests.cs
# If used for exact expected-warning capture, stage only these test-harness paths:
# git add tests/ExpectedWarningLogger.cs tests/RunContinuousCanyonTests.cs
git commit -m "test: prove continuous canyon route contracts"
```

---

### Task 11: Normal-Window Review, Human Timing and Candidate Freeze

**Files:**
- Modify: `export_presets.cfg`
- Create: `packaging/CONTINUOUS-CANYON-README.txt`
- Create: `packaging/CONTINUOUS-CANYON-KNOWN-ISSUES.txt`
- Modify: `README.md`
- Create: `docs/playtests/2026-09-09-continuous-canyon-route-review.md`
- Create: `docs/playtests/2026-09-09-continuous-canyon-human-feedback.md`
- Conditional diagnostic harness ownership: `tests/RunRealtimePrologueProbe.cs`, only to replace its obsolete generated-main workload with Task10's accepted reusable actual-input `ContinuousCanyonRouteProbe` and identify that completed contract truthfully. Preserve `RealtimeFrameSampler`, visible-window/realtime guards and conditions/percentile/long-frame/focus reporting. No new production instrumentation or sampler rewrite is authorized by this workload reconciliation; human active-play timing remains separate.
- Modify only if an observed defect requires it: the owning production/test files from Tasks 1–10
- Conditional presentation ownership for the already-required actor/UI polish: `scripts/presentation/PlayerFormalLayer.cs`, `PlayerFormalVisual.cs`, `PlayerPose.cs`, `SecondaryMotionModel.cs`; `scripts/presentation/EnemyFormalVisualPart.cs`, `RustShellFormalVisual.cs`, `BellBackLaborerFormalVisual.cs`, `GraveLampMothFormalVisual.cs`; `scripts/boss/FallenBellkeeperFormalPart.cs`, `FallenBellkeeperFormalVisual.cs`; `scripts/ui/PrologueFormalHud.cs`, `PrologueUiRoot.cs`; their existing formal-visual/HUD/UI tests and presentation builders/scenes. Root must select the smallest concrete owner/test list for an observed normal-window defect before dispatch, not grant all of these files to the packaging worker. Preserve independent sample visuals and gameplay-authoritative timing/state.
- Conditional action-effect ownership: `scripts/vfx/PrologueVfxDirector.cs`, `ThreadLineVisual.cs`, `ThreadShuttleVisual.cs`; `scripts/presentation/AttackTelegraphVisual.cs`, `PlayerShieldPlaceholderVisual.cs`; `scripts/ui/PrologueEnergyHeatVisual.cs`, with focused VFX/claw/shield/HUD tests and an explicitly selected normal-route capture owner. Preserve actual hit/defense shapes and active windows, controller/audio event semantics, occlusion clipping and cleanup. The Task11 action-FX ownership notes map current bindings and sample-only evidence limits. New trail/visual types require their own concrete scope; current generic burst roots are not evidence of bespoke effects.

- [ ] **Step 1: Perform a normal-window developer route review**

Start from the real title/menu with fresh isolated application persistence. Carry Task10's verified off-tree external save/settings/recorder configuration and pre/post default-data byte guards into the actual window harness; the pinned engine ignored OS-directory override attempts, so those flags alone do not isolate anything. Every remounted Continue scene must be configured before its initializer. Carry Task10's separately named main/exploration full playthrough variants: their explicit union covers both forward branch alternatives and all content, with one persistent PlayerRoot and physical Boss exit per playthrough, and the shortcut physically traversed both ways. Do not concatenate their recordings or timing into a single uninterrupted all-content claim. Specifically review: first no-damage abyss lesson, full Commitment-to-bottom 4–6 second controlled descent target (not the branch's short bottom drop), downward camera anticipation, left/right rift rhythm, visibility of next landing/grapple surface, six mechanism telegraphs, rest spacing, no camp aggro, audio overlap, Chinese/English UI, pause/resume, death/recovery, continue from basin and temple checkpoints, and no W/held-LMB teaching residue.

Record a short proof video and inspect the recording itself. Log observed start/end times and pauses, but label this as developer evidence, not user timing.

Measure same-environment realtime frame pacing separately from movie capture, using the existing sampler with the accepted Task10 full-route workload. Use a visible normal window, time scale 1, no `--fixed-fps` and no `--write-movie`; preserve actual launch/PID/exit and renderer/window/VSync/cap metadata. Inspect complete-sample/focus conditions and p50/p95/p99/max plus >33.3 ms / >50 ms counts. The old hard-wired route's injected deaths, Boss health=1 and missing physical exit cannot stand in for current workload coverage. Callback wall-clock intervals are not isolated GPU or display-present timing; do not infer stable 60 FPS, human feel or target playtime from this diagnostic alone.

Visual polish is a separate gate reinforced by the user on 2026-09-20. Review representative normal-window before/after views and moving footage for coherent ruined-mechanical-temple art direction, material/light/depth hierarchy, restrained noise, stable textures, natural actor/enemy action transitions and distinct claw/shield/attack feedback. Review UI typography, icons, spacing, layering and transitions alongside real play, not only a menu screenshot. Check that richer decoration/effects never hide attack windups, actors, supports or valid/invalid grapple surfaces, and compare observed frame pacing in the same environment. Record unresolved visual/animation/UI defects and route concrete owner-file fixes through Step 2 before claiming polished delivery; passing collision tests or adding more assets is not aesthetic acceptance. Preserve replaceable asset interfaces and the prior-approval requirement for paid generation.

- [ ] **Step 2: Fix only evidence-backed failures and rerun their owner suites**

For every defect, first add a focused failing regression to its existing owner suite (Tasks 1–10 or the conditional presentation scope above), make the smallest owner-file fix, rerun that suite, then repeat the affected normal-window segment. Machine-checkable pose/layout/state constraints supplement rather than certify aesthetic improvement; review the actual moving before/after. The known weak normal actor/environment contrast must receive an explicit disposition after Task9's new environment is visible. Do not silently exclude it because earlier map tasks did not own actor renderers. Do not redesign encounter health, add content for time alone or mask a traversal defect with a hidden boost/teleport.

A bounded source audit found a required presentation compatibility fix before final action-effect review: PrologueVfxDirector discovers typed enemy attack publishers only under legacy CurrentRoom, not the normal seamless mounted sectors. Reproduce with real normal-layout2 mounted and delayed-spawn actors, fix the owned VFX discovery seam, and cover existing/delayed actors, retained neighboring sectors, retirement/remount/reset, deduplication/cleanup and layout1 compatibility in PrologueVfxRuntimeTests. Do not replace actual actor-event binding with direct PlayAttackFrame calls in the proof. Local actor telegraphs remain a separate path; verify combined real output in the normal window after the binding fix. The Task11 seamless-VFX note supplies source references and scope boundaries.

Carry the measured very-close melee case from `task-11-close-combat-observation.md`: Task10 demonstrated actual damage, then near-coincident player/enemy centers and repeated accepted swings without contact. A bot keeping an ideal attack band does not certify human close-combat comfort. Inspect normal-window facing, weapon/body pose, contact readability and enemy response at that spacing before deciding whether any small presentation or combat correction is warranted; do not assume physical body separation or change damage reach from the bot failure alone.

The same carry-forward note also records narrow-platform enemy retirement and Basin ShieldLane direction readability. Inspect both in the normal window: partial foot support must not make a disappearing/resetting enemy confusing; a player must be able to read the lane's right-side danger source and mouse-aimed shield direction while tracking enemies. Active RMB alone is not a successful block. Do not infer missing whole-scene guidance from the Area2D node alone, or certify human clarity from exact-coordinate bot input. Route any observed production fix through Step2's explicit scope; no damage, guard-cone, energy or arena rebalance is pre-authorized by these observations.

- [ ] **Step 3: Freeze an immutable private candidate**

Only after the complete normal-window route succeeds, add a distinct `Continuous Canyon Private Review Windows x64` export preset. It must use the default `run/main_scene="res://scenes/prologue/PrologueLevel.tscn"`, `custom_features=""`, Windows x86_64, external PCK, `dotnet/include_scripts_content=false`, `dotnet/include_debug_symbols=false`, and `dotnet/embed_build_outputs=false`. Exclude the independent Basin sample scene/scripts; never reuse the `basin_sample` feature preset for the full-world candidate.

Commit the preset and package copy first, then stage the exact commit to a new directory with the existing allowlist helper and export from that staged project:

```powershell
git add export_presets.cfg packaging/CONTINUOUS-CANYON-README.txt packaging/CONTINUOUS-CANYON-KNOWN-ISSUES.txt
git commit -m "build: add continuous canyon private export"
```

Use an actual waited engine process:

```powershell
$git = 'C:/Users/22499/AppData/Local/Microsoft/WinGet/Packages/Git.MinGit_Microsoft.Winget.Source_8wekyb3d8bbwe/cmd/git.exe'
$commit = (& $git rev-parse HEAD).Trim()
$short = $commit.Substring(0, 7)
$stageRoot = "E:/OurGame/Knight/.build-staging/continuous-canyon-$short"
& ./tools/stage-basin-package.ps1 -Destination $stageRoot -SourceCommit $commit -GitPath $git
if ($LASTEXITCODE -ne 0) { throw 'Committed-source staging failed.' }
$project = Join-Path $stageRoot 'project'
$runtime = Join-Path $stageRoot 'runtime'
New-Item -ItemType Directory -Path $runtime | Out-Null
$exportRunId = 'export-{0}-{1}-{2}' -f $short,(Get-Date -Format 'yyyyMMddTHHmmssfff'),([Guid]::NewGuid().ToString('N'))
$exportEvidence = Join-Path 'E:/OurGame/Knight/.evidence/september09-continuous-canyon' $exportRunId
New-Item -ItemType Directory -Path $exportEvidence | Out-Null
$taskDotnetRoot = 'G:/godot/dotnet-sdk'
$env:DOTNET_ROOT = $taskDotnetRoot
$env:DOTNET_ROOT_X64 = $taskDotnetRoot
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:PATH = "$taskDotnetRoot;$env:PATH"
Push-Location $project
try {
  # The allowlist archive contains source only: no obj/project.assets.json or .godot cache.
  & "$taskDotnetRoot/dotnet.exe" restore ./Knight.csproj --disable-parallel --nologo 2>&1 |
      Tee-Object -FilePath (Join-Path $exportEvidence 'dotnet-restore.log')
  if ($LASTEXITCODE -ne 0) { throw 'Staged dependency restore failed; no engine launched.' }
  & "$taskDotnetRoot/dotnet.exe" build ./Knight.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo 2>&1 |
      Tee-Object -FilePath (Join-Path $exportEvidence 'dotnet-build.log')
  $stagedBuildExit = $LASTEXITCODE
}
finally { Pop-Location }
if ($stagedBuildExit -ne 0) { throw 'Staged SDK build failed; Godot was not started.' }
$importArgs = @('--headless','--path',$project,'--editor','--import','--quit',
  '--log-file',(Join-Path $exportEvidence 'godot-import.log'))
$importProcess = Start-Process -FilePath 'G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe' -ArgumentList $importArgs -WindowStyle Hidden -PassThru
$importProcess.WaitForExit()
[IO.File]::WriteAllText((Join-Path $exportEvidence 'import.actual-exit-code.txt'),
  $importProcess.ExitCode.ToString([Globalization.CultureInfo]::InvariantCulture))
if ($importProcess.ExitCode -ne 0) { throw "Staged Godot import exited $($importProcess.ExitCode)" }
# Keep the fresh-build gate immediately before export as well as before import.
& "$taskDotnetRoot/dotnet.exe" build (Join-Path $project 'Knight.csproj') --no-restore -m:1 -p:UseSharedCompilation=false --nologo 2>&1 |
    Tee-Object -FilePath (Join-Path $exportEvidence 'dotnet-pre-export.log')
if ($LASTEXITCODE -ne 0) { throw 'Pre-export build failed; export was not started.' }
$exportLog = Join-Path $exportEvidence 'godot-export.log'
$args = @('--headless','--path',$project,'--export-release',
  '"Continuous Canyon Private Review Windows x64"',
  (Join-Path $runtime 'CanyonPrologue.exe'),'--log-file',$exportLog)
$process = Start-Process -FilePath 'G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe' -ArgumentList $args -WindowStyle Hidden -PassThru
$process.WaitForExit()
[IO.File]::WriteAllText((Join-Path $exportEvidence 'actual-exit-code.txt'),
  $process.ExitCode.ToString([Globalization.CultureInfo]::InvariantCulture))
if ($process.ExitCode -ne 0) { throw "Godot export exited $($process.ExitCode)" }
```

The quoted preset name is intentional: PowerShell `Start-Process -ArgumentList` joins the array into a command line, so the name with spaces must reach Godot as one argument. If using paths with spaces, quote those argument values too. Do not copy ignored build/import caches from the dirty development checkout to make a pristine staging build pass.

Statically audit the new PCK and runtime, rather than inheriting b98 counts: all runtime JSON/remap/imported sample/ctex dependencies close; no `tests`, `tools`, editor add-ons, docs, source bodies, Basin sample entry, player saves or absolute developer paths are present; `runtimeconfig.json`/`deps.json` versions match the bundled DLLs. Reuse the established Godot/.NET notice texts only after runtime dependency hashes and versions match their recorded provenance; otherwise source notices from the exact new runtime packs.

Copy the audited runtime, the two continuous-canyon packaging texts and verified notices into a new commit-stamped delivery directory. Generate a manifest after all files except the manifest are final, ZIP that directory, extract to another new directory, and compare every listed size/SHA-256 plus the ZIP SHA-256. Launch both candidate and extracted `CanyonPrologue.exe` with verified isolated application persistence and an SDK/editor-free `PATH`; wait for the actual game PID and record its exit code. Establish a supported packaged-entry isolation mechanism before mounting the normal scene; do not assume the ignored OS-directory flags or excluded test harness will work in the package. Preserve normal title/Start/Continue semantics and protect the original data byte-for-byte. A missing packaged-entry mechanism needs its own minimal owner assignment, not a silent production-default overwrite. Do not modify or replace the b98 candidate.

- [ ] **Step 4: Invite the user’s actual playtest**

Collect the user’s first active-play duration for main route and for main+branches, excluding pause/idle. Record confusion, stuck points, mechanic clarity, combat fatigue, abyss comfort, camera comfort, sound and visual feedback. Compare to the 20–30 and 30–45 minute starting targets without treating shorter skilled play as automatic failure or longer meaningful exploration as a defect.

The candidate remains “private review / human acceptance pending” until this occurs. Automated route completion, content counts and developer timing cannot close this gate.

- [ ] **Step 5: Update durable status and commit evidence**

Update `README.md` with what actually exists, what is still open, the reused asset table and exact candidate/evidence identifiers. The two playtest documents must distinguish fact, inference and unresolved risk. Never claim commercial scale, public-release readiness or human approval without the corresponding evidence.

```powershell
git add README.md docs/playtests/2026-09-09-continuous-canyon-route-review.md docs/playtests/2026-09-09-continuous-canyon-human-feedback.md
git commit -m "docs: record continuous canyon acceptance"
```

---

### Task 12: Content Extension A — Playable Bell-Bolt Sentry

This user-requested extension executes before final Task10 route validation and Task11 freeze. Its spec is `docs/production/2026-09-21-prologue-content-density.md`. This task delivers one separately playable new enemy, not all five new placements, all density targets, or final artwork. Placement integration follows the new actors' independent verification; do not silently change the 17 existing spawns here.

**Goal:** introduce a readable ranged encounter with real counterplay, not a recolored melee enemy.

**Files owned by this task:**
- Create `scripts/enemies/sentry/BellBoltSentrySettings.cs`, `BellBoltSentryModel.cs`, `BellBoltSentry.cs`, `BellBoltProjectile.cs`, `BellBoltSentryVisual.cs`, `BellBoltSentryPresentation.cs` (model file may contain its tightly coupled input/frame/state value types).
- Create `tools/build/prologue/BuildBellBoltSentry.cs` and its generated `scenes/enemies/BellBoltSentry.tscn`.
- Modify only append-only enemy kind in `scripts/combat/AttackPresentationFrame.cs` and sentry-specific cue mappings/source recognition in `scripts/audio/PrologueAudioDirector.cs`.
- Create `tests/unit/BellBoltSentryModelTests.cs`, `tests/integration/BellBoltSentryRuntimeTests.cs`, `tests/presentation/BellBoltSentryProbe.cs`, and `tests/RunBellBoltSentryTests.cs`.
- Own generated UIDs for these new scripts and one task-local verification wrapper/report. No edits to world builder/catalog/packed sectors/player physics/default buses/imports/save code/old tests/global VFX director.

**Interfaces:**
`Knight.Enemies.Sentry.BellBoltSentry : CharacterBody2D, IThreadPullReceiver, IEncounterActor` exposes `Defeated`, `AttackFrameChanged`, `PresentationCueRequested`, `CurrentFrame`, and `ResetForCheckpoint()`. `BellBoltSentryModel.Step(BellBoltSentryInput input, double delta)` returns a frame with `State`, `AimDirection`, `FireRequested` (one edge only) and `AttackTimeline`. Input contains world muzzle/target positions, visibility, stun and death. State enum: Idle, Telegraph, Fire, Recover, Stunned, Dead. A new `EnemyAttackKind.BellBoltShot` is appended, never renumbering existing kinds. Visual and presentation nodes have no damage/state authority. The actor still works with all visual descendants removed.

**Behavior and safety constraints:**
- First-pass tuning: detection560px, telegraph1.10s, direction lock at0.55s, Fire0.12s, recovery1.00s, thread stun0.80s, bolt speed440px/s, maximum travel640px, bolt radius7px, health2, damage1/Normal guard class, gravity980. Positive finite settings; invalid/infinite times or delta rejected or safely clamped consistently, never firing on invalid input. No homing, spread, burst or contact damage.
- Idle starts a telegraph only with a visible living player in range. Before lock, retarget and cancel safely if sight/range is lost. At lock, freeze muzzle-relative firing direction; after lock, no follow-through tracking. The shot travels straight and stops at first solid-world contact, player contact (including a successful shield block), distance limit, owner death, owner removal or checkpoint reset. Do not tunnel through thin walls. At most one projectile is alive per actor; recovery never starts a new shot early.
- Thread contact interrupts an unlaunched attack and gives a visible stun window. Death wins over stun and firing; Defeated once only. Reset clears projectile, attack/audio presentation identity, queued fire and stun, restores health and the actual spawn position. EncounterGroup assigns position after AddChild, so do not blindly snapshot origin in Ready.
- Use actual HealthComponent/Hurtbox/Hitbox/DamageRequest and PlayerShieldController contracts. Projectile owner exclusions must not make the player's Hurtbox appear to be self; world collision must prevent behind-wall damage. Keep projectile parented to the actor for retirement lifetime even when rendered/moved in world coordinates.
- Telegraph is a thin, non-damaging gold line with visible charge/progress and locked red direction, clipped by world geometry. Active presentation describes the actual projectile circle, not a full damaging laser. Distinct provisional silhouette: squat bell shell on three supports, visible aiming aperture, charge/recoil/recovery/stun poses; no cyan decorative target and no new texture/paid generation. Native draw is explicitly a replaceable functional visual, not final original art.
- The actor's0.12s Fire pose is not the projectile's lifetime: a bolt may travel for about1.45s. Its live dangerous circle/visible shot must remain truthful until collision/range/cleanup, including while the actor is recovering. Keep projectile lifetime presentation distinct where necessary; no duplicate launch audio and no early disappearance of a still-damaging projectile. Runtime tests cover late-flight visibility/damage and immediate cleanup.
- Sentry presentation directly forwards its own typed frames to the existing runtime audio/VFX APIs; do not rely on the known legacy CurrentRoom-only auto-binding. It must clear typed state on reset/removal and avoid double dispatch. Reuse existing licensed project cues provisionally: LaborerTelegraph, RustShellAttack, LaborerRecovery, LaborerHurt/LaborerDeath for the new kind/source; no asset generation or new audio manifest entries. Local AttackTelegraphVisual remains available without directors.

**Review focus and required tests:**
1. Player changes side after aim lock: shot must keep its locked direction, with a full visible warning before every shot.
2. Thin wall between bolt and player, wrong-side shield, correct-side shield: world stops bolt; normal player loses exactly one health segment; correct facing blocks and consumes the projectile.
3. Owner removed/reset during telegraph or while projectile flies: no orphan projectile/delayed damage/stale cue; death+fire same step does not fire.
4. Late encounter spawn at nonzero world position, retire/reenter and checkpoint reset: correct position/health and no inherited attack identity.
5. Empty/invalid aim, missing player, rapid input, pause/resume and visual-node removal: no NaN movement, hidden damage, duplicate attacks, or gameplay depending on art.

- [ ] Step1 — write behavioral RED tests and a focused runner before implementation. Use literal outcomes, not source-string scans. Minimal scene-missing assertion is an allowed first RED, then state/real collision tests must each prove behavior. Example desired model contract:
```csharp
var model = new BellBoltSentryModel();
var input = new BellBoltSentryInput(Vector2.Zero, new Vector2(300, 0), true, false, false);
var start = model.Step(input, 0);
TestAssert.Equal(AttackPhase.Telegraph, start.AttackTimeline.Phase, "warning precedes shot");
TestAssert.True(!start.FireRequested, "warning cannot fire");
model.Step(input, .55);
var shot = model.Step(input with { TargetWorldPosition = new Vector2(-300, 0) }, .55);
TestAssert.True(shot.FireRequested && shot.AimDirection.X > .99f, "locked shot does not track across player");
TestAssert.True(!model.Step(input, .01).FireRequested, "shot event emits only once");
```
- [ ] Step2 — run fresh pinned build plus focused runner and record expected RED with actual exit and raw logs. Implement settings/model/actor/projectile and builder using existing `SceneBuildUtil.PackAndSave`; verify round-trip node counts and contracts. Build leaf scene through C#, never hand-author its tscn. Wire body layer4/mask1, player attack receiver, light-enemy ThreadTarget/LegacyThreadInteractionAdapter and shot damage1.
```csharp
// The builder owns generated hierarchy, never runtime input/state.
Error result = SceneBuildUtil.PackAndSave(actor, "res://scenes/enemies/BellBoltSentry.tscn");
if (result != Error.Ok) throw new InvalidOperationException($"Sentry pack failed: {result}");
```
- [ ] Step3 — run deterministic model tests for timings, one-shot edges, lock/loss-of-sight, interruption/death/reset and invalid finite inputs. Run real physics fixtures for actual damage/block, thin-world occlusion, maximum travel, removal/reset cleanup and visual independence. Test doubles may drive a visibility input in model tests only; runtime tests use real collision and receiver objects.
- [ ] Step4 — build a separately playable test scene from the existing real Player, new enemy, a supported broad floor, one cover column and camera. Mount initial positions before adding to tree; thereafter proof player movement/attack/guard/thread actions use real input only (no HP setters, API kills, velocity/position overrides). A local freeplay flag may leave the sandbox interactive. Mark it an enemy demonstration, not the normal prologue. The short recorded case must show one ordinary shot, shield interception, E+LMB thread interruption, Q+LMB actual defeat, then clean end. If a single attempt cannot show all cases safely, independent fresh fixtures may be labeled explicitly rather than hidden resets. Inputs use the existing Q/E/LMB/RMB contracts.
- [ ] Step5 — run focused tests plus existing attack timeline/telegraph/audio/VFX and encounter structure regressions. Run full project pre/post commands only with verified isolation for every mounted level and before/after storage guards; report named pre-existing route failures without deleting tests or fabricating a green suite. Protected default roots are actual Knight userdata `knight`, `playtests`, `logs`; engine --user-data-dir does not isolate them. External case paths injected before AddChild via IsolatedPrologueFixture when mounting PrologueLevel. Standalone actor fixture has no production persistence writer. All engine runs use an external --log-file and finally verify protected path/size/SHA inventories unchanged.
- [ ] Step6 — record and review normal non-headless hardware-rendered proof; retain raw movie and extract a short MP4/montage using installed FFmpeg. Report actual renderer, dimensions/frames, visual observations and boundaries. A fixedFPS movie does not prove realtime performance or human enjoyment. Freeze task-owned diff/new source files, hashes, commands/PIDs/exits/DLL hash and raw logs for independent review. No stage/commit/push in this dirty checkpoint; root handles scoped checkpoints after review. Report to `.superpowers/sdd/2026-09-09-continuous-canyon-world/task-12-report.md` with RED/GREEN, exact files, unresolved findings and runnable preview command.

Task12 does not authorize new catalogue placements or completion claims for the broader density/beauty goal. The short demonstration is an early visual checkpoint; subsequent task briefs cover the second enemy, real placements, mechanisms and region dressing.

### Task 13: Content Extension B — Grounded Canyon Skitter

Execute after Task12's independent acceptance and before catalogue placement integration. Binding spec: `docs/production/2026-09-21-prologue-content-density.md`. Read-only source map: `.superpowers/sdd/2026-09-09-continuous-canyon-world/content-skitter-source-map.md`. Its candidate parameters and suggestion to replace an existing spawn are not authority: this task adds no world placements and preserves all17 existing individuals. The actor must create a different player decision from RustShell's stationary thrust, Laborer's armored sweep and Moth's aerial dive.

**Goal:** a perch-holding light enemy that makes one readable short ground rush, yields ground while vulnerable, then waits. No sustained chase, armor, homing or ranged attack. This is a separately playable functional visual, not final art or a completed density upgrade.

**Owned files:**
- Create `scripts/enemies/skitter/CanyonSkitterSettings.cs`, `CanyonSkitterModel.cs`, `CanyonSkitter.cs`, `CanyonSkitterVisual.cs`, `CanyonSkitterPresentation.cs`.
- Create `tools/build/prologue/BuildCanyonSkitter.cs` and generated `scenes/enemies/CanyonSkitter.tscn`, with their UIDs.
- Append only `EnemyAttackKind.SkitterRush` in `scripts/combat/AttackPresentationFrame.cs`; add this actor's typed and legacy source/cue mappings in `scripts/audio/PrologueAudioDirector.cs`. Preserve accepted Task12 additions exactly.
- Create `tests/unit/CanyonSkitterModelTests.cs`, `tests/integration/CanyonSkitterRuntimeTests.cs`, `tests/presentation/CanyonSkitterProbe.cs`, `tests/RunCanyonSkitterTests.cs`, their UIDs and task-local wrapper/report.
- No player/shared Hitbox/Health/ThreadTarget changes, no world catalogue/geometry/placement changes, no old tests/default buses/imports/save writes, no Task12 source changes. Root owns durable docs and final checkpoints.

**Interfaces and tuning starting point:**
`Knight.Enemies.Skitter.CanyonSkitter : CharacterBody2D, IThreadPullReceiver, IEncounterActor`, with `Defeated`, `AttackFrameChanged`, `PresentationCueRequested`, `CurrentFrame`, `ResetForCheckpoint()`. Node-free model owns phase/locked side/planned dash distance/one attack edge; actor owns actual supported movement/collision and actual distance accounting. States: Idle, Telegraph, Rush, Retreat, Recover, Stunned, Falling, Dead. Model/frame/input value types may share the model file. Settings validate finite values and meaningful nonnegative ranges; unknown/invalid aim never enables damage.

First-pass values: HP2, normal guard damage1, engage range120px, same-shelf vertical tolerance40px, telegraph0.65s with direction lock halfway, rush320px/s for at most0.20s/64px, retreat160px/s for at most0.30s/48px, stationary recovery0.60s, claw stun0.60s, gravity980. These are testable starting settings, not a claim of balanced play. Idle does not chase. Start only with a living visible player on a physically compatible shelf, within range, and supported ground under the actor. World-only line of sight and vertical qualification prevent aggro from another stacked platform.

Carry Task12 review lessons into the actor boundary: live targeting must check the real player's HealthComponent, not merely group membership; healthless runtime doubles are not an implicit living target. Normalize the finite/nonnegative physics step once before both model and actor motion (including gravity), so NaN/infinity/overflow cannot enter MoveAndSlide. Hurt/Death cue tests must drive actual damage/death through the presentation node to the audio director, not call the mapping API manually as a substitute for event wiring.

**Behavior contract:**
1. Telegraph visibly crouches/compresses forelegs, shows the fixed upcoming horizontal threat and a charge cue. Face the target until the midpoint; freeze side and maximum travel afterward. All attacks have the complete warning. A player crossing behind after lock cancels the rush safely rather than spinning the active hitbox through them.
2. Rush uses one real Hitbox attack identity; its danger is the moving front hitbox, not the entire forecast line. It must never damage during telegraph, retreat, recovery, stun or fall. Cap forward distance to the authored64px, world/wall contact, available floor and safe center gap; prevent crossing through the target and reversing the shield-source side. Evaluate prospective body motion and support before each step, including the complete body footprint on a240px test perch. Thin walls cannot be tunneled. A miss still goes through retreat/recovery; no immediate retry.
3. Retreat is non-damaging, opposite the locked rush side, no more than48px and no farther than the home position/support permits. Wall/edge stops it without skipping the stationary punish window. The actor remains normally vulnerable throughout; no invulnerability/exposure subsystem. Show it returning space with leg/body action, not by stretching a sprite.
4. Mechanical-claw contact from telegraph/rush/retreat/recover immediately disables damage logically in the same call, safely defers collision-monitor changes if currently inside a physics signal, cancels authored rush and enters stun. Actual light-enemy pull is retained within finite bounded velocity; do not add a player action or require W. An externally pulled-off enemy visibly falls, cannot attack while unsupported and is not invisibly teleported home or healed. Actor-local fall-out defeat is allowed only after it is at least240px below the authored perch and remains unsupported/falling for0.25s, using an explicit environmental damage/defeat path, not a test API kill. Preserve current health on ordinary lower-platform landing; never call checkpoint reset as fall recovery. Test the fall-out threshold and one Defeated event so a required group cannot retain an unreachable live enemy indefinitely.
5. Death wins over an attack edge; no post-death hit or late cue. Reset records the actual nonzero encounter spawn after its post-AddChild placement, restores HP/home pose, clears pull/rush/fall timers/hitbox/presentation identities and cannot inherit a prior target. Removal/retirement must clear damage and director state. Fresh re-entry is clean. No contact damage.
6. Use actual player Health/Hurtbox/GuardedDamage/shield. Front-facing shield blocks the single hit; wrong-facing shield receives one damage; locked-side crossing never makes an old front attack become a surprise rear hit. Do not change shared shield rules. A successful block still ends the attack's one-contact opportunity.
7. Removable native visual: low six-legged silhouette with a narrow pale dorsal shell and dark articulated legs; warm small eye, no cyan grapple-decoration look. Telegraph compresses legs, rush extends them, retreat steps backward, stun sags, unsupported fall visibly tumbles. Fixed collision/attack authority is separate from all visual transforms. Local typed telegraph plus direct audio/VFX forwarding follows the accepted Task12 seam, not legacy CurrentRoom-only discovery, with no duplicate launch cue. Reuse existing licensed project cues; no generation or new sound assets.

**Implementation and validation:**
- [ ] Step1 — behavioral RED model tests first, including full warning, half-time lock, behind crossing cancellation,64px cap, non-damaging48px retreat, guaranteed recovery, one attack identity, invalid inputs, and death/thread priority. Capture real failing engine evidence after a compiling shell; a missing type error is not the sole RED.
- [ ] Step2 — implement minimal model/actor and C# scene builder; pack through SceneBuildUtil rather than hand-writing tscn. Round-trip layer/mask/body/hurtbox/hitbox/LightEnemy target and IEncounterActor contracts. Native presentation remains removable. No placement edits.
- [ ] Step3 — real runtime fixtures on600px and240px floors in both facing directions, with a3px wall and real player. Cover actual dash distance, wall/ledge stops, correct/wrong shield, cross-behind, same-shelf qualification, thread cancellation during every attack phase, lower-floor landing without healing, actual fall-out defeat, nonzero late spawn/reset, tree removal, pause/resume, and detached visual roots. No production persistence mounted for actor-only tests.
- [ ] Step4 — a separately labeled normal-window playable sandbox proves warning→jump/block or evade→pursue vulnerable retreat, E+LMB interruption, Q+LMB defeat through actual existing input. Initial setup positions are allowed before mount, but no player teleports/velocity/HP writes/API kills afterward. Prefer a single continuous successful sample; label any independent fresh fixtures. Check whether the player decision really differs from old melee. If it still looks like another thrust, report that finding; do not silently add the source map's feint/backstep alternative or certify interest from timings alone.
- [ ] Step5 — use the guarded pinned build/engine wrapper, fresh build immediately before every engine, actual PID/argv/exit/source/DLL hashes, raw logs and protected Knight userdata before/finally inventory. Reject unexpected ERROR/WARNING even with exit0/sentinel. Run safe adjacent attack/audio/VFX/encounter tests and Task12 regression after acceptance. Full RunPrologueTests pre/post remains forbidden until the separately documented remaining storage-isolation defects are fixed. Existing PrologueCanyonStructureTests15-vs14 legacy contract is a separately recorded pre-existing failure, not permission to drop assertions or mutate world content.
- [ ] Step6 — retain hardware renderer/window metadata, a short MP4 and sampled native frames from the actual sandbox, review visible motion/hitbox/edge/retreat clarity, freeze scoped source and full-context diff against the post-Task12 baseline for independent spec+quality review. No stage/commit/push in this dirty checkpoint. Report exact RED/GREEN/known failures/preview command to `.superpowers/sdd/2026-09-09-continuous-canyon-world/task-13-report.md`. FixedFPS movie proves neither realtime frame pacing nor human enjoyment.

Task13 does not complete the5types/22placements goal by itself. A later integration task preserves old17 and adds the3sentries/2skitters to five approved encounter groups, then normal-route difficulty/camera/retirement proof must be repeated. Region dressing, two new mechanism families/six installations, final art/actions/UI/audio and full-route/human acceptance remain required.

## Dependency and Ownership Summary

### Active formal-map integration task (Task14)

`.superpowers/sdd/2026-09-09-continuous-canyon-world/task-14-brief.md` integrates the accepted three Sentries/two Skitters without changing old17 tuples or twelve stable encounter IDs. Each new second wave has two finite authored candidate sites, one visible0.60s arrival period and exactly one safe actor; source investigation found that current immediate spawning can overlap the player at a first enemy's death location. Candidate clearance is checked at actual birth and normal-map acceptance permits no suppressed arrivals. A bounded invalid-placement outcome must not forge defeat/progression or endlessly retry. The same task locally fixes grounded left-overhang retention for frozen-upper, with unchanged activation/route bounds. Dispatched2026-09-23 after Task13 and Task15 acceptance, with exclusive implementation/build/Godot ownership; integration counts are not accepted until generated-region input proof and lifecycle regression.

### Locally accepted regional dressing task (Task15)

The approved `docs/production/2026-09-21-canyon-scenery-density.md` has a bounded brief at `.superpowers/sdd/2026-09-09-continuous-canyon-world/task-15-brief.md`. Regional backdrop/internal platform drawing, isolated comparison capture and narrow mounted-readability storage correction are locally accepted2026-09-23 after fix1 independent review. Nine matched hardware Vulkan Mobile BEFORE/AFTER views, builder/packed/mounted checks and a reviewed actual-input first-seam segment support this limited gate. Capture self-test storage writes were confined and rejecting-path evidence added in the reviewed fix. Gameplay/common world-aligned coverage is unchanged. Simplified native geometry is not final original art; full routes, light/material/action/UI/audio, realtime and human acceptance remain open. Task14 now owns implementation/build/engine.

### Split pressure-gate component / prepared first installation (Task16)

The bounded `.superpowers/sdd/2026-09-09-continuous-canyon-world/task-16-brief.md` specifies one real optional Archive supply alcove. It preserves the old leftward launch, uses player-only plate activation but player/enemy-safe closing and interior release, and reuses finite supply behavior with a separate reward allowlist. Local gate phase is not persistent progression. This is ONE of the two planned pressure gates, not completion of the8-family/12-installation increment; second gate/diverters/extra piston/pendulum remain pending. Task16A now has scoped component evidence. Under the updated sequencing ruling and `task-16b-brief.md`,16B is dispatched first for test/launcher preparation and then root-preflighted local integration; native stability still blocks release, not this reversible implementation. Neither Task14 nor full Task16 is accepted from the sequencing change.

### Prepared paired steam-diverter task (Task17)

Current status (2026-09-27): the independent Task17A component and its scoped fix review passed; Task17B has installed both paired diverters and recorded local actual-input forward/return routes (the Trench route also includes combat and rejoin). Local activation/recorder, shared route membership, support and parent metadata changes have focused evidence. The v004 optional-paint revision still failed visual review; the warning/active silhouette fix, final named neighbors and cohesive Task17B review remain open. No full-prologue/native-shutdown/release acceptance is implied. Read the existing ledger for exact source and evidence versions.

`.superpowers/sdd/2026-09-09-continuous-canyon-world/task-17-brief.md` and subordinate installation plan retain the binding contracts: harmless bounded switching, true two-jet exclusivity, real outgoing-overlap/reset-order/forward-return proof, unchanged supporting floors, and narrowly authorized route membership. The conflicting frozen pendulum proposal remains rejected. No new key or persistent toggle schema. The recorded reversible-integration ruling permits this work while historical Task14/16 release gates remain open; those gates are not waived.

### Prepared second pressure-gate installation (Task18)

`.superpowers/sdd/2026-09-09-continuous-canyon-world/task-18-brief.md` prepares a real-terrain-grab upper Temple supply alcove, reusing Task16's accepted gate/runtime/finite-reward pattern. Actual moving player/Laborer clearance under the shelf, upper landing, anti-crush in the tighter pocket, conditional injured-arrival benefit, camera and unchanged seal/Boss/ground completion are acceptance gates. No floating target or new route node. Candidate only; the second piston/pendulum and full visual/input acceptance remain separate.

1. P0 reliability fix merges first.
2. Auto-pull claw implementation and its residency contract merge second; it exclusively owns `MechanicalClawController`.
3. Tasks 1–3 establish graph, streaming and camera contracts before expanded scene generation.
4. Tasks 4–6 build geometry, mechanisms and encounters before persistence/guidance/art polish.
5. Task 7 is the only save-layout migration owner; it must preserve the legacy slot byte-for-byte.
6. Tasks 8–10 integrate guidance, replaceable visuals and complete route evidence.
7. Task 11 is the normal-window and human acceptance gate; it cannot be parallelized away or replaced by counts.

## Plan Completion Criteria

- No gameplay stubs, TODO behaviors or cosmetic-only challenge counting remain. Replaceable programmatic art placeholders may remain where the spec allows them; they are not formal or paid art and cannot be counted as a finished presentation gate.
- All twelve named challenge kinds have different tested interaction contracts.
- The approved density extension is integrated and readable: eight core mechanism families across twelve installations, and five normal enemy behaviors across twenty-two finite authored placements. Preserve the original seventeen placements, eight main plus four branch groups, both single-enemy entrance groups and the safe camp, without more than two simultaneously effective normal attackers. Counts do not substitute for distinct interactions, visible regional richness or playtesting.
- The U route, both loops, shortcut and safe Boss exit are physically continuous with one player/world coordinate space.
- Active target/current/landing chunks remain resident, and ordinary crossings have no fade, card, teleport or camera snap.
- Layout-v2 save/continue uses stable IDs and a separate slot; old coordinates are never applied and the legacy file is preserved.
- Automated suites and generated-scene validation are green, then a normal-window route recording is reviewed.
- Human timing and quality targets remain explicitly open until the user plays the frozen private candidate.
