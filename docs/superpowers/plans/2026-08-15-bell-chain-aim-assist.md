# Bell-Chain Aim Assist Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the first-level Bell-Chain mechanism respond to a close-range neutral `K` press through the real mechanical-thread pipeline without changing ordinary targets or Boss aiming.

**Architecture:** Extend the pure thread candidate with an opt-in fallback range and keep normal cone-based selection as the first pass. `ThreadTarget2D` supplies the target-local value; only the generated first-level Bell-Chain target sets `260px`, while runtime selection, line of sight, cooldown, pull handling and presentation continue through the existing `ThreadAbility` path. End-to-end tests and the deterministic presentation will use neutral input at the mechanism so the shipped gate proves the exact player interaction.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9, Godot SceneTree test runners, PowerShell verification/capture scripts.

**Completion status (2026-08-15):** Implementation and automated verification are complete through `47370df`. The checked items below include TDD RED/GREEN and mutation evidence, target-local runtime integration, neutral-input guidance/smoke coverage, full gates, corrected visible proof, and durable documentation. Final acceptance is still open until a fresh unassisted 10–15 minute completion is recorded from a build containing `47370df` or later. Any pre-`47370df` run, including the 21:08 run, does not satisfy that acceptance.

## Global Constraints

- Work only in `E:\OurGame\Knight\.worktrees\player-art-pipeline` on `codex/player-art-pipeline`.
- Read and follow `F:\Documents\游戏\godot.md` before implementation; judge completion from the running Godot scene, not only a clean build.
- Preserve all pre-existing dirty files. In particular, do not stage Beehave `.import` files, art scripts/imports, screenshots, or generated-scene `unique_id` noise.
- `FallbackAimAssistRange` defaults to `0`; only the first-level Bell-Chain mechanism sets it to exactly `260f`.
- Normal aiming always wins. Fallback runs only when normal selection returns no target, ignores aim angle, still requires line of sight, and uses `min(400f, FallbackAimAssistRange)` as its distance ceiling.
- Keep global thread tuning unchanged: range `400f`, cone half-angle `30f`, cooldown `0.35s`, player pull `720`, enemy pull `540`, visual duration `0.12s`.
- Do not enable fallback for anchors, light/heavy enemies, or the Fallen Bellkeeper chain.
- Do not directly open the mechanism or write `PrologueProgress`; selection must continue through `ThreadTarget2D.ApplyPull`, `IThreadInteractionReceiver`, and the existing mechanism.
- Generated scenes are authored by builders. Because `scenes/mechanisms/BellChainMechanism.tscn` already has unrelated `unique_id` noise, stage only the new serialized gameplay property, not the existing IDs.
- Use TDD for every production behavior: named RED, minimal GREEN, mutation RED, restored GREEN.
- No formal art, audio, paid generation, global auto-aim, teleport, or second-level changes.

## File Map

- `scripts/abilities/ThreadCandidate.cs`: pure candidate data, including target-local fallback range.
- `scripts/abilities/ThreadAbilityModel.cs`: normal-first and opt-in fallback target selection.
- `scripts/abilities/ThreadTarget2D.cs`: exported Godot configuration boundary; default remains disabled.
- `scripts/abilities/ThreadAbility.cs`: copies the target configuration into pure candidates and reuses real LOS/pull/presentation.
- `tools/build/prologue/BuildBellChainMechanism.cs`: authors and validates the one `260px` opt-in target.
- `scenes/mechanisms/BellChainMechanism.tscn`: generated artifact containing the serialized `FallbackAimAssistRange = 260.0` semantic line.
- `tests/unit/ThreadAbilityModelTests.cs`: pure selection ordering, range, LOS, opt-in and determinism.
- `tests/unit/P0RoomStructureTests.cs`: representative ordinary anchor remains opt-out.
- `tests/unit/P1LoopStructureTests.cs`: generated Bell-Chain target is exactly `260px`.
- `tests/unit/BossSceneStructureTests.cs`: Boss chain remains opt-out.
- `tests/integration/BellChainRuntimeTests.cs`: real neutral input, world obstruction, range, thread line and mechanism progress.
- `scripts/prologue/guidance/PrologueGuidanceModel.cs`: approved first-level instruction copy.
- `tests/unit/PrologueGuidanceModelTests.cs`: exact Chinese copy contract.
- `tests/integration/PrologueGuidanceRuntimeTests.cs`: real route opens the shortcut with neutral thread input and retargets immediately.
- `tests/smoke/PrologueCriticalPathSmoke.cs`: shipping critical path proves neutral `K` rather than diagonal aim.
- `tests/presentation/ProloguePresentation.cs`: deterministic visible proof uses neutral `K` at the mechanism.
- `README.md`: durable playable-status and asset/interface record.
- `docs/playtests/2026-08-15-bell-chain-aim-assist.md`: automated/proof boundary and outstanding human acceptance.

---

### Task 1: Pure opt-in fallback selection

**Files:**
- Modify: `scripts/abilities/ThreadCandidate.cs`
- Modify: `scripts/abilities/ThreadAbilityModel.cs`
- Test: `tests/unit/ThreadAbilityModelTests.cs`

**Interfaces:**
- Consumes: existing `ThreadAbilitySettings.Range` and current normal cone selection.
- Produces: `ThreadCandidate(..., bool HasLineOfSight, float FallbackAimAssistRange = 0f)` and unchanged `ThreadAbilityModel.SelectTarget(Vector2, Vector2, IReadOnlyList<ThreadCandidate>)` semantics with a normal-first fallback pass.

- [x] **Step 1: Add failing model tests**

Add these calls to `ThreadAbilityModelTests.Run()`:

```csharp
FallbackAssistAcceptsNeutralAndWrongDirection();
FallbackAssistRejectsDisabledOutOfRangeAndOccludedTargets();
NormalAimWinsBeforeFallback();
FallbackTieBreaksByDistanceThenStableId();
```

Add concrete tests and extend the candidate helper:

```csharp
private static void FallbackAssistAcceptsNeutralAndWrongDirection()
{
    var model = new ThreadAbilityModel();
    ThreadCandidate assisted = Candidate(7, new Vector2(220f, 0f), fallbackRange: 260f);

    ThreadCandidate? neutral = model.SelectTarget(Vector2.Zero, Vector2.Zero, [assisted]);
    ThreadCandidate? wrongDirection = model.SelectTarget(Vector2.Zero, Vector2.Left, [assisted]);

    TestAssert.Equal(7UL, neutral!.Value.Id,
        "an opted-in close target accepts a neutral thread input");
    TestAssert.Equal(7UL, wrongDirection!.Value.Id,
        "fallback ignores aim angle after normal selection finds nothing");
}

private static void FallbackAssistRejectsDisabledOutOfRangeAndOccludedTargets()
{
    var model = new ThreadAbilityModel();

    TestAssert.True(!model.SelectTarget(Vector2.Zero, Vector2.Zero,
        [Candidate(1, new Vector2(220f, 0f))]).HasValue,
        "a default-zero target never participates in fallback");
    TestAssert.True(!model.SelectTarget(Vector2.Zero, Vector2.Zero,
        [Candidate(5, new Vector2(220f, 0f), fallbackRange: -1f)]).HasValue,
        "a negative fallback range is treated as disabled");
    TestAssert.True(!model.SelectTarget(Vector2.Zero, Vector2.Zero,
        [Candidate(2, new Vector2(261f, 0f), fallbackRange: 260f)]).HasValue,
        "fallback rejects a target outside its target-local range");
    TestAssert.True(!model.SelectTarget(Vector2.Zero, Vector2.Zero,
        [Candidate(3, new Vector2(401f, 0f), fallbackRange: 999f)]).HasValue,
        "fallback never exceeds the global 400 pixel range");
    TestAssert.True(!model.SelectTarget(Vector2.Zero, Vector2.Zero,
        [Candidate(4, new Vector2(220f, 0f), false, 260f)]).HasValue,
        "fallback rejects an occluded target");
}

private static void NormalAimWinsBeforeFallback()
{
    var model = new ThreadAbilityModel();
    ThreadCandidate normal = Candidate(10, new Vector2(180f, 0f));
    ThreadCandidate assisted = Candidate(1, new Vector2(80f, 80f), fallbackRange: 260f);

    ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Right, [assisted, normal]);

    TestAssert.Equal(10UL, selected!.Value.Id,
        "a legal cone target wins even when an assisted target is nearer");
}

private static void FallbackTieBreaksByDistanceThenStableId()
{
    var model = new ThreadAbilityModel();
    ThreadCandidate farther = Candidate(1, new Vector2(240f, 0f), fallbackRange: 260f);
    ThreadCandidate sameDistanceHighId = Candidate(9, new Vector2(200f, 0f), fallbackRange: 260f);
    ThreadCandidate sameDistanceLowId = Candidate(3, new Vector2(0f, 200f), fallbackRange: 260f);

    ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Zero,
        [farther, sameDistanceHighId, sameDistanceLowId]);

    TestAssert.Equal(3UL, selected!.Value.Id,
        "fallback chooses nearest distance then the lowest stable ID");
}

private static ThreadCandidate Candidate(ulong id, Vector2 position, bool hasLineOfSight = true,
    float fallbackRange = 0f) =>
    new(id, position, ThreadTargetKind.Anchor, hasLineOfSight, fallbackRange);
```

- [x] **Step 2: Run the RED build**

Run from the worktree:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'
$env:DOTNET_ROOT_X64=$env:DOTNET_ROOT
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
```

Expected: non-zero build with the named fifth `ThreadCandidate` argument/constructor unavailable. Record the exact compiler failure before production edits.

- [x] **Step 3: Extend the candidate and implement normal-first fallback**

Change the record to:

```csharp
public readonly record struct ThreadCandidate(
    ulong Id,
    Vector2 Position,
    ThreadTargetKind Kind,
    bool HasLineOfSight,
    float FallbackAimAssistRange = 0f);
```

Replace `SelectTarget` with the normal-first flow below; retain the current `IsBetter` helper for normal aim:

```csharp
public ThreadCandidate? SelectTarget(Vector2 origin, Vector2 aim,
    IReadOnlyList<ThreadCandidate> candidates)
{
    float globalRangeSquared = _settings.Range * _settings.Range;
    ThreadCandidate? aimed = null;
    float bestDot = float.NegativeInfinity;
    float bestDistanceSquared = float.PositiveInfinity;

    if (!aim.IsZeroApprox())
    {
        Vector2 normalizedAim = aim.Normalized();
        float minimumDot = Mathf.Cos(Mathf.DegToRad(_settings.ConeHalfAngleDegrees));
        foreach (ThreadCandidate candidate in candidates)
        {
            if (!candidate.HasLineOfSight) continue;
            Vector2 offset = candidate.Position - origin;
            float distanceSquared = offset.LengthSquared();
            if (distanceSquared > globalRangeSquared || Mathf.IsZeroApprox(distanceSquared)) continue;
            float aimDot = normalizedAim.Dot(offset / Mathf.Sqrt(distanceSquared));
            if (aimDot < minimumDot) continue;
            if (!aimed.HasValue || IsBetter(candidate, aimDot, distanceSquared,
                    aimed.Value, bestDot, bestDistanceSquared))
            {
                aimed = candidate;
                bestDot = aimDot;
                bestDistanceSquared = distanceSquared;
            }
        }
    }

    if (aimed.HasValue) return aimed;

    ThreadCandidate? fallback = null;
    float fallbackDistanceSquared = float.PositiveInfinity;
    foreach (ThreadCandidate candidate in candidates)
    {
        if (!candidate.HasLineOfSight || candidate.FallbackAimAssistRange <= 0f) continue;
        Vector2 offset = candidate.Position - origin;
        float distanceSquared = offset.LengthSquared();
        float allowed = Mathf.Min(_settings.Range, candidate.FallbackAimAssistRange);
        if (Mathf.IsZeroApprox(distanceSquared) || distanceSquared > allowed * allowed) continue;
        if (!fallback.HasValue || IsBetterFallback(candidate, distanceSquared,
                fallback.Value, fallbackDistanceSquared))
        {
            fallback = candidate;
            fallbackDistanceSquared = distanceSquared;
        }
    }
    return fallback;
}

private static bool IsBetterFallback(ThreadCandidate candidate, float candidateDistanceSquared,
    ThreadCandidate best, float bestDistanceSquared)
{
    const float comparisonTolerance = 0.00001f;
    if (candidateDistanceSquared < bestDistanceSquared - comparisonTolerance) return true;
    if (candidateDistanceSquared > bestDistanceSquared + comparisonTolerance) return false;
    return candidate.Id < best.Id;
}
```

- [x] **Step 4: Run GREEN model verification**

Run the .NET build, then the real Godot test runner:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' `
  --headless --path $PWD --script 'res://tests/RunPrologueTests.cs' --log-file '.\bell-chain-task1-green.log'
```

Expected: build `0 warnings / 0 errors`, runner exit `0`, exactly one `PROLOGUE_TESTS_OK`, and no engine/script/parse errors.

- [x] **Step 5: Prove the fallback test is capable of failing**

Temporarily replace `return fallback;` with `return null;`, rerun the Godot runner, and require failure at `an opted-in close target accepts a neutral thread input`. Restore `return fallback;` and rerun GREEN.

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' `
  --headless --path $PWD --script 'res://tests/RunPrologueTests.cs' --log-file '.\bell-chain-task1-mutation.log'
```

- [x] **Step 6: Commit Task 1**

```powershell
git add -- scripts/abilities/ThreadCandidate.cs scripts/abilities/ThreadAbilityModel.cs tests/unit/ThreadAbilityModelTests.cs
git diff --cached --check
git commit -m "feat: add opt-in thread fallback selection"
```

---

### Task 2: Target configuration and real Bell-Chain runtime

**Files:**
- Modify: `scripts/abilities/ThreadTarget2D.cs`
- Modify: `scripts/abilities/ThreadAbility.cs`
- Modify: `tools/build/prologue/BuildBellChainMechanism.cs`
- Modify semantically: `scenes/mechanisms/BellChainMechanism.tscn`
- Test: `tests/unit/P0RoomStructureTests.cs`
- Test: `tests/unit/P1LoopStructureTests.cs`
- Test: `tests/unit/BossSceneStructureTests.cs`
- Test: `tests/integration/BellChainRuntimeTests.cs`

**Interfaces:**
- Consumes: Task 1 `ThreadCandidate.FallbackAimAssistRange` and `ThreadAbilityModel.SelectTarget`.
- Produces: exported `ThreadTarget2D.FallbackAimAssistRange`, runtime candidate propagation, one generated `260f` target, and real neutral-input behavior with LOS/range protection.

- [x] **Step 1: Write failing structure and runtime tests**

Add the following structure assertions:

```csharp
// P0RoomStructureTests, after loading Geometry/ThreadAnchor
TestAssert.Equal(0f, anchor.FallbackAimAssistRange,
    "ordinary corridor anchor keeps fallback aim assist disabled");

// P1LoopStructureTests, inside MechanismStubConvergesBothBranchesSafely
ThreadTarget2D mechanismTarget = room.GetNode<ThreadTarget2D>(
    "Mechanisms/BellChain/ThreadTarget");
TestAssert.Equal(260f, mechanismTarget.FallbackAimAssistRange,
    "first-level Bell Chain alone opts into 260 pixel fallback assist");

// BossSceneStructureTests, after loading ChainTarget
TestAssert.Equal(0f, chain.FallbackAimAssistRange,
    "Boss chain keeps precise directional aiming");
```

Register a new `BellChainRuntimeTests` method before the persistence test:

```csharp
await NeutralThreadAssistHonorsRangeAndWorldOcclusion(tree);
```

Add the runtime test and helper:

```csharp
private static async Task NeutralThreadAssistHonorsRangeAndWorldOcclusion(SceneTree tree)
{
    PrologueProgress progress = new();
    MechanismFixture fixture = await MechanismRoom(tree, "NeutralAssistFixture", progress);
    var ability = fixture.Player.GetNode<ThreadAbility>("ThreadAbility");
    Line2D line = fixture.Player.GetNode<Line2D>("ThreadLine");
    ThreadTarget2D target = fixture.Mechanism.GetNode<ThreadTarget2D>("ThreadTarget");
    int hits = 0;
    ability.ThreadHit += () => hits++;

    fixture.Player.GlobalPosition = target.GlobalPosition + new Vector2(-300f, 0f);
    await NeutralThread(tree, ability);
    TestAssert.True(!fixture.Mechanism.IsOpen && hits == 0,
        "neutral K outside 260 pixels does not open the mechanism");

    fixture.Player.GlobalPosition = target.GlobalPosition + new Vector2(-220f, 0f);
    var wall = new StaticBody2D {
        Name = "AssistOccluder", CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
    };
    wall.AddChild(new CollisionShape2D {
        Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(32f, 160f) }
    });
    fixture.Root.AddChild(wall);
    wall.GlobalPosition = target.GlobalPosition + new Vector2(-110f, 0f);
    await Frames(tree, 2);
    await NeutralThread(tree, ability);
    TestAssert.True(!fixture.Mechanism.IsOpen && hits == 0,
        "neutral K cannot assist through a real world-layer wall");

    wall.Free();
    await Frames(tree, 1);
    fixture.Player.GlobalPosition = target.GlobalPosition + new Vector2(-220f, 0f);
    await NeutralThread(tree, ability);
    TestAssert.True(fixture.Mechanism.IsOpen && progress.BellChainShortcutOpen && hits == 1,
        "neutral K inside 260 pixels uses the real mechanism interaction once");
    TestAssert.Equal(2, line.Points.Length,
        "successful fallback displays the existing mechanical-thread line");

    await Dispose(tree, fixture.Root);
}

private static async Task NeutralThread(SceneTree tree, ThreadAbility ability)
{
    Input.ActionRelease(InputActions.MoveLeft);
    Input.ActionRelease(InputActions.MoveRight);
    Input.ActionRelease(InputActions.MoveUp);
    Input.ActionRelease(InputActions.MoveDown);
    ability.ResetTransientState();
    Input.ActionPress(InputActions.Thread);
    await Frames(tree, 2);
    Input.ActionRelease(InputActions.Thread);
    await Frames(tree, 1);
}
```

Add `using Knight.Abilities;` to the integration test.

- [x] **Step 2: Run RED before adding the Godot property**

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
```

Expected: non-zero compile with `ThreadTarget2D.FallbackAimAssistRange` missing. This proves tests/configuration precede production support.

- [x] **Step 3: Add the exported target property and runtime propagation**

Add to `ThreadTarget2D`:

```csharp
[Export]
public float FallbackAimAssistRange { get; set; }
```

Change the candidate construction in `ThreadAbility.SelectTarget` to:

```csharp
candidates.Add(new ThreadCandidate(id, target.GlobalPosition, target.Kind,
    HasLineOfSight(origin, target), target.FallbackAimAssistRange));
```

Do not change `HasLineOfSight`, cooldown consumption, `ApplyPull`, line rendering or signals.

- [x] **Step 4: Author and validate the single assisted target**

Set the builder target property:

```csharp
var target = new ThreadTarget2D
{
    Name = "ThreadTarget", Position = new Vector2(0, -80),
    Kind = ThreadTargetKind.Mechanism, PullReceiverPath = new NodePath(".."),
    FallbackAimAssistRange = 260f
};
```

Strengthen builder validation:

```csharp
ThreadTarget2D generatedTarget = copy.GetNodeOrNull<ThreadTarget2D>("ThreadTarget");
bool valid = generatedTarget?.Kind == ThreadTargetKind.Mechanism &&
    Mathf.IsEqualApprox(generatedTarget.FallbackAimAssistRange, 260f) &&
    copy.GetNodeOrNull<CollisionShape2D>("Door/Collision") != null &&
    copy.GetNodeOrNull<Node2D>("VisualRoot/Placeholder") != null &&
    copy.GetNodeOrNull<Node2D>("VisualRoot/Effects") != null;
```

Use `apply_patch` to add exactly this semantic line after `Kind = 3` in the currently dirty generated scene, preserving its existing IDs:

```text
FallbackAimAssistRange = 260.0
```

- [x] **Step 5: Run GREEN through the real Godot gate**

Run the build and test runner, then the full prologue gate:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' `
  --headless --path $PWD --script 'res://tests/RunPrologueTests.cs' --log-file '.\bell-chain-task2-green.log'
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Expected: build `0/0`; runtime test proves out-of-range rejection, wall rejection, neutral success and line rendering; full gate prints `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, `PROLOGUE_VERIFY_OK`; the gate restores exact pre-run scene bytes.

- [x] **Step 6: Mutation-check the builder contract**

Temporarily set the builder assignment to `FallbackAimAssistRange = 0f`, run `verify-prologue.ps1`, and require a non-zero failure at `BuildBellChainMechanism contract validation failed` (or the named semantic drift if validation runs later). Restore `260f`, rerun the full gate, and confirm GREEN.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

- [x] **Step 7: Stage only owned semantics and commit Task 2**

Stage source/tests normally. Do **not** `git add` the whole dirty generated scene. Apply an index-only patch against HEAD for the one serialized property:

```powershell
git add -- scripts/abilities/ThreadTarget2D.cs scripts/abilities/ThreadAbility.cs `
  tools/build/prologue/BuildBellChainMechanism.cs tests/unit/P0RoomStructureTests.cs `
  tests/unit/P1LoopStructureTests.cs tests/unit/BossSceneStructureTests.cs `
  tests/integration/BellChainRuntimeTests.cs

$indexPatch = @'
diff --git a/scenes/mechanisms/BellChainMechanism.tscn b/scenes/mechanisms/BellChainMechanism.tscn
--- a/scenes/mechanisms/BellChainMechanism.tscn
+++ b/scenes/mechanisms/BellChainMechanism.tscn
@@ -17,2 +17,3 @@
 script = ExtResource("2_f8idv")
 Kind = 3
+FallbackAimAssistRange = 260.0
'@
$indexPatch | git apply --cached --unidiff-zero --whitespace=nowarn -
git diff --cached --check
git diff --cached --name-only
```

Before committing, confirm the cached scene diff contains only `FallbackAimAssistRange` and the working-tree scene still retains the pre-existing `unique_id` differences.

```powershell
git commit -m "feat: configure bell-chain aim assist"
```

---

### Task 3: First-level guidance and neutral critical path

**Files:**
- Modify: `scripts/prologue/guidance/PrologueGuidanceModel.cs`
- Test: `tests/unit/PrologueGuidanceModelTests.cs`
- Test: `tests/integration/PrologueGuidanceRuntimeTests.cs`
- Test: `tests/smoke/PrologueCriticalPathSmoke.cs`
- Modify proof driver: `tests/presentation/ProloguePresentation.cs`

**Interfaces:**
- Consumes: Task 2 real neutral-input Bell-Chain behavior.
- Produces: approved first-level copy, neutral-input route coverage, and a deterministic proof stage that visibly exercises the fallback.

- [x] **Step 1: Make the approved guidance copy fail first**

Change only the expected OpenShortcut/BellChain row in `PrologueGuidanceModelTests` to:

```csharp
Row(PrologueObjective.OpenShortcut, PrologueRoomId.BellChainMechanism,
    "靠近金色菱形，按 K 牵动机关；方向键可精确瞄准。",
    PrologueGuidanceTargetKey.MechanismBellChain),
```

Run `RunPrologueTests.cs` and require the exact text assertion to fail with the old `对钟链机关瞄准并按 K` value.

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' `
  --headless --path $PWD --script 'res://tests/RunPrologueTests.cs' --log-file '.\bell-chain-task3-red.log'
```

- [x] **Step 2: Implement the approved first-level copy**

Change the Bell-Chain row in `PrologueGuidanceModel.RouteToMechanism` to:

```csharp
PrologueRoomId.BellChainMechanism => Show(
    "靠近金色菱形，按 K 牵动机关；方向键可精确瞄准。",
    PrologueGuidanceTargetKey.MechanismBellChain),
```

Do not change Boss-chain text or later-level guidance.

- [x] **Step 3: Force the real guidance route and smoke to use neutral K**

Replace the direction presses in `OpenBellChainWithRealInput` with only the thread action:

```csharp
level.Player.GlobalPosition = mechanism.GetNode<ThreadTarget2D>("ThreadTarget").GlobalPosition +
    new Vector2(-220f, 0f);
level.Player.GetNode<ThreadAbility>("ThreadAbility").ResetTransientState();
Input.ActionRelease(InputActions.MoveRight);
Input.ActionRelease(InputActions.MoveUp);
Input.ActionPress(InputActions.Thread);
for (int frame = 0; frame < 4 && !level.Progress.BellChainShortcutOpen; frame++)
    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
ReleaseGameplayInput();
TestAssert.True(mechanism.IsOpen && level.Progress.BellChainShortcutOpen,
    "neutral K opens the close Bell Chain and retargets guidance within four physics frames");
```

In `PrologueCriticalPathSmoke.LaborerAndShortcut`, change only the mechanism call:

```csharp
await ThreadInput(right: false, up: false);
```

Keep the light-enemy, heavy-enemy and Boss calls directional, proving those targets were not broadened.

- [x] **Step 4: Make the deterministic proof visibly neutral**

At frame 610, rename the stage to:

```csharp
GD.Print("PROLOGUE_PRESENTATION_STAGE=bell_chain_neutral_aim_assist");
```

Replace only frames 635/639:

```csharp
if (_frame == 635)
{
    Release(InputActions.MoveRight);
    Release(InputActions.MoveUp);
    Press(InputActions.Thread);
}
if (_frame == 639) Release(InputActions.Thread);
```

Do not change `ThreadPulse` for anchors, enemies or Boss.

- [x] **Step 5: Run GREEN and a focused mutation**

Run the build, test runner and full gate:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' `
  --headless --path $PWD --script 'res://tests/RunPrologueTests.cs' --log-file '.\bell-chain-task3-tests.log'
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Expected: exact Chinese copy passes; guidance retargets to `MechanismBossExit` on the first post-open frame; smoke opens the mechanism with neutral `K`; ordinary and Boss direction-dependent stages remain green.

Temporarily change the mechanism's serialized assist to `0.0` in the working scene (leave source at `260f`) and run only the smoke. Require failure at `real thread input opens and persists the Bell-Chain shortcut`. Restore `260.0` and rerun the full gate. This mutation proves the critical path is exercising fallback rather than an accidental direction input.

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' `
  --headless --path $PWD --script 'res://tests/smoke/PrologueCriticalPathSmoke.cs' `
  --log-file '.\bell-chain-task3-mutation.log'
```

- [x] **Step 6: Commit Task 3**

```powershell
git add -- scripts/prologue/guidance/PrologueGuidanceModel.cs `
  tests/unit/PrologueGuidanceModelTests.cs tests/integration/PrologueGuidanceRuntimeTests.cs `
  tests/smoke/PrologueCriticalPathSmoke.cs tests/presentation/ProloguePresentation.cs
git diff --cached --check
git commit -m "test: prove neutral bell-chain interaction"
```

---

### Task 4: Final gate, visible proof, durable status and hands-on launch

**Files:**
- Modify: `README.md`
- Create: `docs/playtests/2026-08-15-bell-chain-aim-assist.md`
- Generate ignored evidence: `screenshots/prologue/prologue-frames/frame*.png`
- Read external human evidence: `%APPDATA%\Godot\app_userdata\Knight\playtests\prologue\latest.md`

**Interfaces:**
- Consumes: Tasks 1–3 exact HEAD and the existing evidence/launcher scripts.
- Produces: fresh gate logs, reviewed visible proof, durable project status, and a normal player-owned validation launch.

- [x] **Step 1: Run the exact-HEAD full gate twice**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
git diff --check
```

Each run must exit `0`, build with `0 warnings / 0 errors`, execute all 13 builders, print exactly one each of `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`, report no engine/script/parse errors, and restore all protected scene/project bytes.

- [x] **Step 2: Capture fresh frame proof and inspect the actual interaction**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly
```

Require `PROLOGUE_CAPTURE_OK`, exactly 1380 prologue frames plus 600 Boss-retry frames, 1280×720 at 60 FPS, one completion marker per capture, and clean logs. Inspect `screenshots/prologue/prologue-frames/frame00000634.png` through `frame00000642.png` with `view_image` and confirm:

- no directional movement is held at the mechanism proof stage;
- a visible mechanical-thread line reaches the gold diamond;
- the door opens and the guidance switches toward the Boss exit;
- the HUD remains readable and no formal-art claim is made.

- [x] **Step 3: Update durable status and evidence boundaries**

Update `README.md` playable status to state that the first-level Bell-Chain target alone has a `260px` opt-in near-field fallback, neutral `K` is verified, and ordinary/Boss targets remain directional. Keep the outstanding fresh human 10–15 minute completion explicitly open.

Create `docs/playtests/2026-08-15-bell-chain-aim-assist.md` with these exact sections and facts:

```markdown
# Bell-Chain aim-assist verification — 2026-08-15

## Cause and scope
The first-level Bell-Chain target was visible and close, but a zero or off-angle aim caused a silent miss. The fix is target-local: only this mechanism opts into a 260px fallback; anchors, enemies and the Boss chain remain directional.

## Automated evidence
- The pure model proves normal-first ordering, opt-in default zero, 260px and global-range boundaries, LOS rejection and deterministic tie-breaking.
- The real room proves neutral K fails outside 260px, fails through a world wall, then emits the normal ThreadHit, draws the existing thread line and opens persistent progress in range.
- The guidance route and critical-path smoke open the mechanism with no movement direction held.
- Two consecutive full prologue gates passed with 13 builders and the exact PROLOGUE_TESTS_OK, PROLOGUE_CRITICAL_PATH_OK and PROLOGUE_VERIFY_OK markers.

## Visual proof
The fresh 1380-frame, 1280×720, 60 FPS prologue capture shows neutral thread input, the gold-diamond hit, the opening door and immediate Boss-exit retarget. This is deterministic functional proof, not human pacing evidence.

## Human acceptance still open
Launch through tools/play-prologue.ps1. A fresh unassisted completion is still required before claiming the 10–15 minute first-clear target; the prior 560-minute idle-contaminated report is not pacing evidence.
```

- [x] **Step 4: Commit only the delivery documents**

```powershell
git add -- README.md docs/playtests/2026-08-15-bell-chain-aim-assist.md
git diff --cached --check
git commit -m "docs: record bell-chain assist verification"
```

Confirm `git status --short` shows only the pre-existing import/art/scene-ID/screenshot noise and no owned uncommitted source or test files.

- [x] **Step 5: Re-run the post-commit automated gate**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

The command must repeat the complete GREEN contract on committed HEAD.

- [ ] **Step 6: Complete fixed-build hands-on acceptance**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1
```

This is a visible, player-owned launch from `47370df` or later: do not inject input, teleport, capture automated time, or present its report as complete before the user finishes. The pre-`47370df` 21:08 run is implementation history only and cannot close this item.

After the user exits, read `latest.md` and report only observed facts. Completion requires a new report with `Completed: true`, the Bell-Chain shortcut opened, at least one Boss attempt, terminal completion reached, and a defensible active clear time. If the player still cannot operate the mechanism, preserve the report/log and return to systematic debugging rather than widening global aim.
