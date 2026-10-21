# Prologue First-Level Guidance System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a dynamic, first-level-only guidance layer that tells a new player what to do and where to go, escalates to one contextual hint after repeated failures, and leads the player from awakening through the Fallen Bellkeeper without changing gameplay difficulty.

**Architecture:** A pure `PrologueGuidanceModel` converts objective, room, Boss, progress, and same-room failure state into an immutable instruction containing stable target keys. A level-local `PrologueGuidanceController` retains the most recently failed challenge-room streak across checkpoint travel, projects `SameRoomFailureCount = 0` while another room is mounted, restores that streak on re-entry, and clears it on real objective progress or completion. The controller resolves target keys against the currently mounted room and projects world targets into viewport data for an interchangeable `IPrologueGuidanceVisual`; the initial visual is a procedural placeholder under `Hud/Guidance/VisualRoot`. The pause menu toggles only the current controller instance, so guidance defaults on for every new prologue run and never becomes a global tutorial or save-data concern.

**Tech Stack:** Godot 4.7.1 Mono, C#/.NET 9.0.317, generated `.tscn` scenes, existing `Knight.Tests` SceneTree test runner, PowerShell verification and capture scripts.

## Global Constraints

- The feature exists only under `scenes/prologue/PrologueLevel.tscn`; do not add an Autoload or modify later-level scenes.
- Guidance defaults on, can be disabled only for the current level instance, and writes nothing to `PrologueProgress`, disk, or playtest completion criteria.
- A room transition clears only the public current-room failure projection. Checkpoint recovery must preserve the most recently failed challenge streak until the player fails in another room or the objective advances/completes.
- Preserve player, enemy, Boss, collision, room topology, exit topology, checkpoint, and tuning values exactly.
- Never point at the optional hidden alcove; both main branches remain equivalent valid choices.
- Keep the replaceable presentation path exactly `Hud/Guidance/VisualRoot` and keep runtime decisions out of the visual implementation.
- Missing target nodes produce one named warning per room/key, retain the instruction text, clear directional markers, and never crash.
- All new Godot C# runtime classes are `partial`; pure model and record types remain non-Node C# types.
- Modify scenes only through `tools/build/prologue/BuildPrologueLevel.cs`; do not hand-edit generated `.tscn` content.
- Preserve unrelated dirty Beehave imports, generated resource-ID noise, and paused player-art files; stage only files named in the active task.
- Do not generate or purchase art. The visual for this milestone is project-owned procedural placeholder UI.
- Completion requires the full `tools/verify-prologue.ps1` gate, deterministic proof capture, visual review of the proof, and a fresh hands-on run; automated capture cannot certify human clear time.
- Approved pre-flight ruling (2026-08-15): the design specification governs abnormal objective/room combinations. Every non-complete context must keep a visible, actionable first step; an unexpectedly sealed Boss room follows the live Boss state instead of hiding guidance.

---

## File Map

### New runtime files

- `scripts/prologue/guidance/PrologueGuidanceTargetKey.cs` — stable semantic target keys, independent of NodePaths.
- `scripts/prologue/guidance/PrologueBossGuidanceState.cs` — the four Boss states relevant to guidance.
- `scripts/prologue/guidance/PrologueGuidanceContext.cs` — immutable input to the pure decision model.
- `scripts/prologue/guidance/PrologueGuidanceInstruction.cs` — immutable text, hint, visibility, and target-key output.
- `scripts/prologue/guidance/PrologueGuidanceModel.cs` — complete table-driven decision and failure-hint policy.
- `scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs` — current-room stable-key-to-`Node2D` adapter.
- `scripts/prologue/guidance/PrologueGuidanceTargetView.cs` — immutable projected target data passed to presentation.
- `scripts/prologue/guidance/IPrologueGuidanceVisual.cs` — replaceable presentation boundary.
- `scripts/prologue/guidance/PrologueGuidancePlaceholderVisual.cs` — procedural text, diamond, and edge-arrow renderer.
- `scripts/prologue/guidance/PrologueGuidanceController.cs` — level-local orchestration, failure counting, Boss summary, resolution, and projection.

### Modified runtime/build files

- `scripts/prologue/ProloguePauseMenu.cs` — `ToggleGuidance` command and current-session button state.
- `tools/build/prologue/BuildPrologueLevel.cs` — mount controller, placeholder visual, larger HUD panel, and fifth pause command.
- `tests/RunPrologueTests.cs` — register new unit and integration suites.
- `README.md` — document first-level-only guidance, toggle, replaceable path, and remaining human acceptance pass.

### New/modified tests

- `tests/unit/PrologueGuidanceModelTests.cs` — all objective/room rows, dual routes, Boss states, completion, and hints.
- `tests/unit/PrologueGuidanceTargetResolverTests.cs` — exact stable path contract for every room and Boss target.
- `tests/integration/PrologueGuidanceRuntimeTests.cs` — live room transitions, cross-checkpoint challenge-streak escalation, shortcut retarget, missing target, and session isolation.
- `tests/integration/PrologueCompletionTests.cs` — pause toggle wiring and default-on behavior on a fresh instance.

### Evidence

- `screenshots/prologue-guidance/` — ignored deterministic proof output only.
- `%APPDATA%/Godot/app_userdata/Knight/playtests/prologue/` — fresh hands-on report from the existing authorized launcher.

---

### Task 1: Pure Guidance Decision Model

**Files:**
- Create: `scripts/prologue/guidance/PrologueGuidanceTargetKey.cs`
- Create: `scripts/prologue/guidance/PrologueBossGuidanceState.cs`
- Create: `scripts/prologue/guidance/PrologueGuidanceContext.cs`
- Create: `scripts/prologue/guidance/PrologueGuidanceInstruction.cs`
- Create: `scripts/prologue/guidance/PrologueGuidanceModel.cs`
- Create: `tests/unit/PrologueGuidanceModelTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Consumes: `PrologueObjective`, `PrologueRoomId`.
- Produces: `PrologueGuidanceInstruction PrologueGuidanceModel.Evaluate(PrologueGuidanceContext context)`.
- Produces: `PrologueGuidanceInstruction.Visible`, `.StepText`, `.HintText`, and `.Targets` with zero to two `PrologueGuidanceTargetKey` values.

- [ ] **Step 1: Add the failing table-driven unit suite and register it**

Create the suite with explicit rows so every approved mapping is independently named:

```csharp
using System.Collections.Generic;
using Knight.Prologue;
using Knight.Prologue.Guidance;

namespace Knight.Tests.Unit;

public static class PrologueGuidanceModelTests
{
    public static void Run()
    {
        var rows = new[]
        {
            Row(PrologueObjective.ReachCorridorLamp, PrologueRoomId.AwakeningCourtyard,
                "向右穿过庭院，进入断齿走廊", PrologueGuidanceTargetKey.CourtyardToCorridor),
            Row(PrologueObjective.ReachCorridorLamp, PrologueRoomId.BrokenToothCorridor,
                "点亮前方墓灯检查点", PrologueGuidanceTargetKey.CorridorCheckpoint),
            Row(PrologueObjective.LearnThread, PrologueRoomId.BrokenToothCorridor,
                "朝锚点瞄准并按 K 使用机械丝", PrologueGuidanceTargetKey.CorridorThreadAnchor),
            Row(PrologueObjective.LearnThread, PrologueRoomId.UpperPendulum,
                "回到断齿走廊学习机械丝", PrologueGuidanceTargetKey.BranchToCorridor),
            Row(PrologueObjective.LearnThread, PrologueRoomId.BellChainMechanism,
                "经任一分支回到断齿走廊", PrologueGuidanceTargetKey.MechanismToUpper,
                PrologueGuidanceTargetKey.MechanismToLower),
            Row(PrologueObjective.ChooseRoute, PrologueRoomId.BrokenToothCorridor,
                "上下路线任选其一", PrologueGuidanceTargetKey.CorridorToUpper,
                PrologueGuidanceTargetKey.CorridorToLower),
            Row(PrologueObjective.OpenShortcut, PrologueRoomId.BrokenToothCorridor,
                "沿任一分支前往钟链房间", PrologueGuidanceTargetKey.CorridorToUpper,
                PrologueGuidanceTargetKey.CorridorToLower),
            Row(PrologueObjective.OpenShortcut, PrologueRoomId.UpperPendulum,
                "穿过上层摆锤，前往钟链房间", PrologueGuidanceTargetKey.BranchToMechanism),
            Row(PrologueObjective.OpenShortcut, PrologueRoomId.LowerBuriedMachinery,
                "穿过下层机械区，前往钟链房间", PrologueGuidanceTargetKey.BranchToMechanism),
            Row(PrologueObjective.OpenShortcut, PrologueRoomId.BellChainMechanism,
                "对钟链机关瞄准并按 K", PrologueGuidanceTargetKey.MechanismBellChain),
            Row(PrologueObjective.DefeatBellkeeper, PrologueRoomId.BrokenToothCorridor,
                "经任一分支返回钟链房间", PrologueGuidanceTargetKey.CorridorToUpper,
                PrologueGuidanceTargetKey.CorridorToLower),
            Row(PrologueObjective.DefeatBellkeeper, PrologueRoomId.UpperPendulum,
                "前往钟链房间，不要返回走廊", PrologueGuidanceTargetKey.BranchToMechanism),
            Row(PrologueObjective.DefeatBellkeeper, PrologueRoomId.LowerBuriedMachinery,
                "前往钟链房间，不要返回走廊", PrologueGuidanceTargetKey.BranchToMechanism),
            Row(PrologueObjective.DefeatBellkeeper, PrologueRoomId.BellChainMechanism,
                "前往房间右侧金色出口，进入 Boss 区", PrologueGuidanceTargetKey.MechanismBossExit)
        };

        foreach (Case row in rows)
        {
            PrologueGuidanceInstruction actual = PrologueGuidanceModel.Evaluate(
                new PrologueGuidanceContext(row.Objective, row.Room, false,
                    PrologueBossGuidanceState.None, 0));
            TestAssert.True(actual.Visible, $"{row.Objective}/{row.Room} is visible");
            TestAssert.Equal(row.Text, actual.StepText, $"{row.Objective}/{row.Room} text");
            TestAssert.Equal(row.Targets.Count, actual.Targets.Count, $"{row.Objective}/{row.Room} target count");
            for (int i = 0; i < row.Targets.Count; i++)
                TestAssert.Equal(row.Targets[i], actual.Targets[i], $"{row.Objective}/{row.Room} target {i}");
        }

        BossRowsChangeWithEncounterState();
        CompleteHidesEverything();
        RepeatedFailuresAddOnlyTheRoomHint();
        HiddenAlcoveIsNeverTargeted();
        AbnormalCombinationsNeverHideOrThrow();
    }

    private static void BossRowsChangeWithEncounterState()
    {
        var expected = new[]
        {
            (PrologueBossGuidanceState.Inactive, PrologueGuidanceTargetKey.BossActivation, "向右跨过门槛，唤醒失落守钟人"),
            (PrologueBossGuidanceState.Active, PrologueGuidanceTargetKey.BossBody, "按 J 攻击，并躲避出招"),
            (PrologueBossGuidanceState.ChainExposed, PrologueGuidanceTargetKey.BossChain, "锁链已暴露：瞄准并按 K")
        };
        foreach (var row in expected)
        {
            PrologueGuidanceInstruction actual = PrologueGuidanceModel.Evaluate(new(
                PrologueObjective.DefeatBellkeeper, PrologueRoomId.FallenBellkeeperArena,
                true, row.Item1, 0));
            TestAssert.Equal(row.Item2, actual.Targets[0], $"Boss {row.Item1} target");
            TestAssert.Equal(row.Item3, actual.StepText, $"Boss {row.Item1} text");
        }
    }

    private static void CompleteHidesEverything()
    {
        PrologueGuidanceInstruction actual = PrologueGuidanceModel.Evaluate(new(
            PrologueObjective.Complete, PrologueRoomId.FallenBellkeeperArena, true,
            PrologueBossGuidanceState.Active, 9));
        TestAssert.True(!actual.Visible && actual.Targets.Count == 0 && actual.StepText == string.Empty,
            "completion clears guidance");
    }

    private static void RepeatedFailuresAddOnlyTheRoomHint()
    {
        var rows = new[]
        {
            (PrologueRoomId.AwakeningCourtyard, "SHIFT"),
            (PrologueRoomId.BrokenToothCorridor, "K"),
            (PrologueRoomId.UpperPendulum, "摆锤"),
            (PrologueRoomId.LowerBuriedMachinery, "SHIFT"),
            (PrologueRoomId.BellChainMechanism, "重型敌人"),
            (PrologueRoomId.FallenBellkeeperArena, "第二阶段")
        };
        foreach (var row in rows)
        {
            PrologueGuidanceContext first = new(PrologueObjective.DefeatBellkeeper,
                row.Item1, true, PrologueBossGuidanceState.Active, 1);
            PrologueGuidanceInstruction before = PrologueGuidanceModel.Evaluate(first);
            PrologueGuidanceInstruction after = PrologueGuidanceModel.Evaluate(
                first with { SameRoomFailureCount = 2 });
            TestAssert.Equal(string.Empty, before.HintText, $"{row.Item1}: one failure does not escalate");
            TestAssert.True(after.HintText.Contains(row.Item2) && after.StepText == before.StepText &&
                after.Targets.Count == before.Targets.Count,
                $"{row.Item1}: two failures add only the approved action hint");
        }
    }

    private static void HiddenAlcoveIsNeverTargeted()
    {
        foreach (PrologueObjective objective in System.Enum.GetValues<PrologueObjective>())
        foreach (PrologueRoomId room in System.Enum.GetValues<PrologueRoomId>())
        foreach (PrologueBossGuidanceState boss in System.Enum.GetValues<PrologueBossGuidanceState>())
            foreach (PrologueGuidanceTargetKey target in PrologueGuidanceModel.Evaluate(
                new(objective, room, false, boss, 4)).Targets)
                TestAssert.True(!target.ToString().Contains("Hidden", System.StringComparison.OrdinalIgnoreCase),
                    "optional hidden alcove is never a guidance target");
    }

    private static void AbnormalCombinationsNeverHideOrThrow()
    {
        foreach (PrologueObjective objective in System.Enum.GetValues<PrologueObjective>())
        foreach (PrologueRoomId room in System.Enum.GetValues<PrologueRoomId>())
        {
            if (objective == PrologueObjective.Complete) continue;
            PrologueBossGuidanceState boss = room == PrologueRoomId.FallenBellkeeperArena
                ? PrologueBossGuidanceState.Inactive
                : PrologueBossGuidanceState.None;
            PrologueGuidanceInstruction actual = PrologueGuidanceModel.Evaluate(
                new(objective, room, false, boss, 0));
            TestAssert.True(actual.Visible && actual.Targets.Count > 0,
                $"abnormal {objective}/{room} retains an actionable first step");
        }
    }

    private static Case Row(PrologueObjective objective, PrologueRoomId room, string text,
        params PrologueGuidanceTargetKey[] targets) => new(objective, room, text, targets);

    private sealed record Case(PrologueObjective Objective, PrologueRoomId Room, string Text,
        IReadOnlyList<PrologueGuidanceTargetKey> Targets);
}
```

Insert `PrologueGuidanceModelTests.Run();` immediately after `PrologueObjectiveTests.Run();` in `tests/RunPrologueTests.cs`.

- [ ] **Step 2: Run the build and confirm the RED state**

Run:

```powershell
$env:DOTNET_ROOT='G:\godot\dotnet-sdk'
$env:Path="$env:DOTNET_ROOT;$env:Path"
& "$env:DOTNET_ROOT\dotnet.exe" build .\Knight.csproj
```

Expected: non-zero build with `Knight.Prologue.Guidance` or `PrologueGuidanceModel` missing. If it passes, the new test was not registered or compiled.

- [ ] **Step 3: Add the immutable model types and minimal complete mapping**

Use these exact contracts:

```csharp
namespace Knight.Prologue.Guidance;

public enum PrologueGuidanceTargetKey
{
    CourtyardToCorridor,
    CorridorCheckpoint,
    CorridorThreadAnchor,
    CorridorToUpper,
    CorridorToLower,
    BranchToCorridor,
    BranchToMechanism,
    MechanismToUpper,
    MechanismToLower,
    MechanismBellChain,
    MechanismBossExit,
    BossActivation,
    BossBody,
    BossChain
}
```

```csharp
namespace Knight.Prologue.Guidance;

public enum PrologueBossGuidanceState { None, Inactive, Active, ChainExposed }
```

```csharp
namespace Knight.Prologue.Guidance;

public readonly record struct PrologueGuidanceContext(
    PrologueObjective Objective,
    PrologueRoomId Room,
    bool ShortcutOpen,
    PrologueBossGuidanceState BossState,
    int SameRoomFailureCount);
```

```csharp
using System;
using System.Collections.Generic;

namespace Knight.Prologue.Guidance;

public sealed record PrologueGuidanceInstruction(
    bool Visible,
    string StepText,
    string HintText,
    IReadOnlyList<PrologueGuidanceTargetKey> Targets)
{
    public static readonly PrologueGuidanceInstruction Hidden =
        new(false, string.Empty, string.Empty, Array.Empty<PrologueGuidanceTargetKey>());
}
```

Implement `Evaluate` as a pure switch and append the room hint without mutating the selected target list:

```csharp
using System;

namespace Knight.Prologue.Guidance;

public static class PrologueGuidanceModel
{
    public static PrologueGuidanceInstruction Evaluate(PrologueGuidanceContext context)
    {
        if (context.Objective == PrologueObjective.Complete)
            return PrologueGuidanceInstruction.Hidden;

        PrologueGuidanceInstruction instruction = context.Objective switch
        {
            PrologueObjective.Wake or PrologueObjective.ReachCorridorLamp => context.Room switch
            {
                PrologueRoomId.AwakeningCourtyard => Show("向右穿过庭院，进入断齿走廊", PrologueGuidanceTargetKey.CourtyardToCorridor),
                PrologueRoomId.BrokenToothCorridor => Show("点亮前方墓灯检查点", PrologueGuidanceTargetKey.CorridorCheckpoint),
                _ => ReturnToCorridor(context)
            },
            PrologueObjective.LearnThread => context.Room == PrologueRoomId.BrokenToothCorridor
                ? Show("朝锚点瞄准并按 K 使用机械丝", PrologueGuidanceTargetKey.CorridorThreadAnchor)
                : ReturnToCorridor(context),
            PrologueObjective.ChooseRoute => context.Room == PrologueRoomId.BrokenToothCorridor
                ? Show("上下路线任选其一", PrologueGuidanceTargetKey.CorridorToUpper, PrologueGuidanceTargetKey.CorridorToLower)
                : ReturnToCorridor(context),
            PrologueObjective.OpenShortcut => RouteToMechanism(context, false),
            PrologueObjective.DefeatBellkeeper => RouteToBoss(context),
            _ => PrologueGuidanceInstruction.Hidden
        };

        return instruction with
        {
            HintText = context.SameRoomFailureCount >= 2 ? HintFor(context.Room) : string.Empty
        };
    }

    private static PrologueGuidanceInstruction RouteToMechanism(PrologueGuidanceContext context, bool bossRoute) => context.Room switch
    {
        PrologueRoomId.AwakeningCourtyard => Show("返回断齿走廊", PrologueGuidanceTargetKey.CourtyardToCorridor),
        PrologueRoomId.BrokenToothCorridor => Show(bossRoute ? "经任一分支返回钟链房间" : "沿任一分支前往钟链房间", PrologueGuidanceTargetKey.CorridorToUpper, PrologueGuidanceTargetKey.CorridorToLower),
        PrologueRoomId.UpperPendulum => Show(bossRoute ? "前往钟链房间，不要返回走廊" : "穿过上层摆锤，前往钟链房间", PrologueGuidanceTargetKey.BranchToMechanism),
        PrologueRoomId.LowerBuriedMachinery => Show(bossRoute ? "前往钟链房间，不要返回走廊" : "穿过下层机械区，前往钟链房间", PrologueGuidanceTargetKey.BranchToMechanism),
        PrologueRoomId.BellChainMechanism => Show("对钟链机关瞄准并按 K", PrologueGuidanceTargetKey.MechanismBellChain),
        PrologueRoomId.FallenBellkeeperArena => BossInstruction(context.BossState),
        _ => BossInstruction(context.BossState)
    };

    private static PrologueGuidanceInstruction RouteToBoss(PrologueGuidanceContext context)
    {
        if (context.Room != PrologueRoomId.FallenBellkeeperArena)
        {
            if (context.Room == PrologueRoomId.BellChainMechanism)
                return Show("前往房间右侧金色出口，进入 Boss 区", PrologueGuidanceTargetKey.MechanismBossExit);
            return RouteToMechanism(context, true);
        }
        return BossInstruction(context.BossState);
    }

    private static PrologueGuidanceInstruction BossInstruction(PrologueBossGuidanceState bossState) => bossState switch
    {
        PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive => Show("向右跨过门槛，唤醒失落守钟人", PrologueGuidanceTargetKey.BossActivation),
        PrologueBossGuidanceState.ChainExposed => Show("锁链已暴露：瞄准并按 K", PrologueGuidanceTargetKey.BossChain),
        _ => Show("按 J 攻击，并躲避出招", PrologueGuidanceTargetKey.BossBody)
    };

    private static PrologueGuidanceInstruction ReturnToCorridor(PrologueGuidanceContext context) => context.Room switch
    {
        PrologueRoomId.AwakeningCourtyard => Show("返回断齿走廊", PrologueGuidanceTargetKey.CourtyardToCorridor),
        PrologueRoomId.BrokenToothCorridor => Show("点亮前方墓灯检查点", PrologueGuidanceTargetKey.CorridorCheckpoint),
        PrologueRoomId.UpperPendulum or PrologueRoomId.LowerBuriedMachinery => Show("回到断齿走廊学习机械丝", PrologueGuidanceTargetKey.BranchToCorridor),
        PrologueRoomId.BellChainMechanism => Show("经任一分支回到断齿走廊", PrologueGuidanceTargetKey.MechanismToUpper, PrologueGuidanceTargetKey.MechanismToLower),
        PrologueRoomId.FallenBellkeeperArena => BossInstruction(context.BossState),
        _ => BossInstruction(context.BossState)
    };

    private static string HintFor(PrologueRoomId room) => room switch
    {
        PrologueRoomId.AwakeningCourtyard => "跳跃后可按 SHIFT 冲刺，先落在中间平台。",
        PrologueRoomId.BrokenToothCorridor => "朝悬挂锚点按 K，可越过长距离。",
        PrologueRoomId.UpperPendulum => "先观察摆锤节奏；机械丝可拉向轻型目标。",
        PrologueRoomId.LowerBuriedMachinery => "跳跃末段使用 SHIFT，避开机械坑。",
        PrologueRoomId.BellChainMechanism => "重型敌人不能被拉起；用 K 暴露弱点，再攻击。",
        PrologueRoomId.FallenBellkeeperArena => "第二阶段等待投锤，再对锁链按 K。",
        _ => string.Empty
    };

    private static PrologueGuidanceInstruction Show(string text, params PrologueGuidanceTargetKey[] targets) =>
        new(true, text, string.Empty, Array.AsReadOnly(targets));
}
```

- [ ] **Step 4: Run the focused model suite through the real test runner**

Run:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs
```

Expected: build exit `0`, test exit `0`, and exactly one `PROLOGUE_TESTS_OK`. Mutate the `BellChainMechanism`/`DefeatBellkeeper` key to `MechanismBellChain`, rerun, observe the named RED assertion, then restore it and rerun GREEN.

- [ ] **Step 5: Commit the pure model slice**

```powershell
git add scripts/prologue/guidance/PrologueGuidanceTargetKey.cs scripts/prologue/guidance/PrologueBossGuidanceState.cs scripts/prologue/guidance/PrologueGuidanceContext.cs scripts/prologue/guidance/PrologueGuidanceInstruction.cs scripts/prologue/guidance/PrologueGuidanceModel.cs tests/unit/PrologueGuidanceModelTests.cs tests/RunPrologueTests.cs
git commit -m "feat: model first-level guidance decisions"
```

---

### Task 2: Stable Target Resolution and Level-Local Controller

**Files:**
- Create: `scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs`
- Create: `scripts/prologue/guidance/PrologueGuidanceTargetView.cs`
- Create: `scripts/prologue/guidance/IPrologueGuidanceVisual.cs`
- Create: `scripts/prologue/guidance/PrologueGuidanceController.cs`
- Create: `tests/unit/PrologueGuidanceTargetResolverTests.cs`
- Create: `tests/integration/PrologueGuidanceRuntimeTests.cs`
- Modify: `tests/RunPrologueTests.cs`

**Interfaces:**
- Consumes: Task 1 model types, `PrologueLevelController.CurrentRoom`, `PrologueCompletionController.CurrentObjective`, `BossArenaController.CurrentBoss`, `FallenBellkeeperState`.
- Produces: `IPrologueGuidanceVisual.Present(PrologueGuidanceInstruction, IReadOnlyList<PrologueGuidanceTargetView>)` and `.Clear()`.
- Produces controller properties `GuidanceEnabled`, `SameRoomFailureCount`, `CurrentInstruction`, `ResolvedTargetCount`, plus `SetGuidanceEnabled(bool)`, `ToggleGuidance()`, and `RefreshNow()`.

- [ ] **Step 1: Write resolver and live-controller tests before runtime types exist**

The resolver suite must instantiate each real room scene, add it to the tree, and assert these exact pairs:

```csharp
private static readonly (string Scene, PrologueRoomId Room, PrologueGuidanceTargetKey Key, string Path)[] Rows =
{
    ("res://scenes/prologue/rooms/AwakeningCourtyard.tscn", PrologueRoomId.AwakeningCourtyard, PrologueGuidanceTargetKey.CourtyardToCorridor, "Exits/ToCorridor"),
    ("res://scenes/prologue/rooms/BrokenToothCorridor.tscn", PrologueRoomId.BrokenToothCorridor, PrologueGuidanceTargetKey.CorridorCheckpoint, "Checkpoint"),
    ("res://scenes/prologue/rooms/BrokenToothCorridor.tscn", PrologueRoomId.BrokenToothCorridor, PrologueGuidanceTargetKey.CorridorThreadAnchor, "Geometry/ThreadAnchor"),
    ("res://scenes/prologue/rooms/BrokenToothCorridor.tscn", PrologueRoomId.BrokenToothCorridor, PrologueGuidanceTargetKey.CorridorToUpper, "Exits/ToUpper"),
    ("res://scenes/prologue/rooms/BrokenToothCorridor.tscn", PrologueRoomId.BrokenToothCorridor, PrologueGuidanceTargetKey.CorridorToLower, "Exits/ToLower"),
    ("res://scenes/prologue/rooms/UpperPendulum.tscn", PrologueRoomId.UpperPendulum, PrologueGuidanceTargetKey.BranchToCorridor, "Exits/ToCorridor"),
    ("res://scenes/prologue/rooms/UpperPendulum.tscn", PrologueRoomId.UpperPendulum, PrologueGuidanceTargetKey.BranchToMechanism, "Exits/ToMechanism"),
    ("res://scenes/prologue/rooms/LowerBuriedMachinery.tscn", PrologueRoomId.LowerBuriedMachinery, PrologueGuidanceTargetKey.BranchToCorridor, "Exits/ToCorridor"),
    ("res://scenes/prologue/rooms/LowerBuriedMachinery.tscn", PrologueRoomId.LowerBuriedMachinery, PrologueGuidanceTargetKey.BranchToMechanism, "Exits/ToMechanism"),
    ("res://scenes/prologue/rooms/BellChainMechanism.tscn", PrologueRoomId.BellChainMechanism, PrologueGuidanceTargetKey.MechanismToUpper, "Exits/ToUpper"),
    ("res://scenes/prologue/rooms/BellChainMechanism.tscn", PrologueRoomId.BellChainMechanism, PrologueGuidanceTargetKey.MechanismToLower, "Exits/ToLower"),
    ("res://scenes/prologue/rooms/BellChainMechanism.tscn", PrologueRoomId.BellChainMechanism, PrologueGuidanceTargetKey.MechanismBellChain, "Mechanisms/BellChain"),
    ("res://scenes/prologue/rooms/BellChainMechanism.tscn", PrologueRoomId.BellChainMechanism, PrologueGuidanceTargetKey.MechanismBossExit, "Exits/P1Complete"),
    ("res://scenes/prologue/rooms/FallenBellkeeperArena.tscn", PrologueRoomId.FallenBellkeeperArena, PrologueGuidanceTargetKey.BossActivation, "ActivationTrigger")
};
```

For each row, call `PrologueGuidanceTargetResolver.TryResolve(room, row.Room, row.Key, out Node2D target)` and assert `target == room.GetNode<Node2D>(row.Path)`. In the Boss scene also assert `BossBody` resolves to `arena.CurrentBoss` and `BossChain` resolves to `arena.CurrentBoss.GetNode<Node2D>("ChainTarget")` after one process frame.

The runtime suite must load `PrologueLevel.tscn`, assert `GuidanceEnabled == true`, call `RequestRespawn` twice in one room and observe `SameRoomFailureCount == 2`, transition to another room and observe the current projection is `0`, and verify that disabling guidance calls `Clear()` without altering `CurrentObjective` or `Progress`. It must also use a real branch challenge: enter `UpperPendulum`, request recovery to the corridor checkpoint, re-enter through the real exit, and repeat. The first and second re-entry must restore counts `1` and `2`; the second must visibly render the upper-room rescue hint. Opening the real Bell Chain advances the objective and clears the saved streak before a later upper-room re-entry.

Register `await PrologueGuidanceTargetResolverTests.Run(this);` before integration suites and `await PrologueGuidanceRuntimeTests.Run(this);` immediately before `PrologueCompletionTests.Run(this)`.

- [ ] **Step 2: Run the test runner and confirm the RED state**

Run the Task 1 build/test commands. Expected: build failure naming `PrologueGuidanceTargetResolver`, `PrologueGuidanceController`, or `IPrologueGuidanceVisual`.

- [ ] **Step 3: Implement the stable resolver without fallback guesses**

Use a single exact path switch and dynamic Boss resolution:

```csharp
using Godot;
using Knight.Boss;

namespace Knight.Prologue.Guidance;

public static class PrologueGuidanceTargetResolver
{
    public static bool TryResolve(PrologueRoom room, PrologueRoomId roomId,
        PrologueGuidanceTargetKey key, out Node2D target)
    {
        target = null;
        if (room == null) return false;
        if (key is PrologueGuidanceTargetKey.BossBody or PrologueGuidanceTargetKey.BossChain)
        {
            BossArenaController arena = room.GetNodeOrNull<BossArenaController>("BossArenaController");
            if (arena?.CurrentBoss == null) return false;
            target = key == PrologueGuidanceTargetKey.BossBody
                ? arena.CurrentBoss
                : arena.CurrentBoss.GetNodeOrNull<Node2D>("ChainTarget");
            return GodotObject.IsInstanceValid(target);
        }

        string path = (roomId, key) switch
        {
            (PrologueRoomId.AwakeningCourtyard, PrologueGuidanceTargetKey.CourtyardToCorridor) => "Exits/ToCorridor",
            (PrologueRoomId.BrokenToothCorridor, PrologueGuidanceTargetKey.CorridorCheckpoint) => "Checkpoint",
            (PrologueRoomId.BrokenToothCorridor, PrologueGuidanceTargetKey.CorridorThreadAnchor) => "Geometry/ThreadAnchor",
            (PrologueRoomId.BrokenToothCorridor, PrologueGuidanceTargetKey.CorridorToUpper) => "Exits/ToUpper",
            (PrologueRoomId.BrokenToothCorridor, PrologueGuidanceTargetKey.CorridorToLower) => "Exits/ToLower",
            (PrologueRoomId.UpperPendulum or PrologueRoomId.LowerBuriedMachinery, PrologueGuidanceTargetKey.BranchToCorridor) => "Exits/ToCorridor",
            (PrologueRoomId.UpperPendulum or PrologueRoomId.LowerBuriedMachinery, PrologueGuidanceTargetKey.BranchToMechanism) => "Exits/ToMechanism",
            (PrologueRoomId.BellChainMechanism, PrologueGuidanceTargetKey.MechanismToUpper) => "Exits/ToUpper",
            (PrologueRoomId.BellChainMechanism, PrologueGuidanceTargetKey.MechanismToLower) => "Exits/ToLower",
            (PrologueRoomId.BellChainMechanism, PrologueGuidanceTargetKey.MechanismBellChain) => "Mechanisms/BellChain",
            (PrologueRoomId.BellChainMechanism, PrologueGuidanceTargetKey.MechanismBossExit) => "Exits/P1Complete",
            (PrologueRoomId.FallenBellkeeperArena, PrologueGuidanceTargetKey.BossActivation) => "ActivationTrigger",
            _ => null
        };
        target = path == null ? null : room.GetNodeOrNull<Node2D>(path);
        return GodotObject.IsInstanceValid(target);
    }
}
```

- [ ] **Step 4: Implement the presentation contract and level controller**

Use these exact presentation types:

```csharp
using Godot;

namespace Knight.Prologue.Guidance;

public readonly record struct PrologueGuidanceTargetView(
    Vector2 ViewportPosition,
    Vector2 Direction,
    float Distance,
    bool IsOnScreen);
```

```csharp
using System.Collections.Generic;

namespace Knight.Prologue.Guidance;

public interface IPrologueGuidanceVisual
{
    void Present(PrologueGuidanceInstruction instruction,
        IReadOnlyList<PrologueGuidanceTargetView> targets);
    void Clear();
}
```

`PrologueGuidanceController` must:

1. Export `LevelPath = ".."`, `CompletionPath = "../Completion"`, and `VisualPath = "../Hud/Guidance/VisualRoot"`.
2. Bind `RecoveryRequested` in `_Ready`, unbind in `_ExitTree`, and retain a private most-recent failed room plus its consecutive failure count. A recovery in the same failed room increments the streak; a recovery requested from a different room starts that room's streak at one.
3. On `CurrentRoomId` change, update only the public `SameRoomFailureCount` projection: expose the saved count in its failed room and `0` everywhere else. Do not erase the saved streak during checkpoint travel. Clear the saved streak when `CurrentObjective` advances or completes.
4. Build `PrologueBossGuidanceState` from the real `BossArenaController`: inactive, active, or chain-exposed only when `CurrentBoss.CurrentFrame.State == FallenBellkeeperState.HammerThrown`.
5. Call the pure model from `RefreshNow()` and preserve `CurrentInstruction` even when target resolution fails.
6. For each valid target use `Camera2D.GetCanvasTransform() * target.GlobalPosition`, the viewport safe rectangle inset by `48`, and player-to-target world distance to create `PrologueGuidanceTargetView`.
7. Log only once per `(CurrentRoomId, targetKey)` using a `HashSet<string>` and `GD.PushWarning($"Guidance target '{key}' is missing in room '{roomId}'.")`.
8. If disabled or complete, call `Clear()` and set `ResolvedTargetCount = 0`; do not stop `PrologueCompletionController` or mutate progress.

The public state and toggle implementation must be exact:

```csharp
public bool GuidanceEnabled { get; private set; } = true;
public int SameRoomFailureCount { get; private set; }
public int ResolvedTargetCount { get; private set; }
public PrologueGuidanceInstruction CurrentInstruction { get; private set; } =
    PrologueGuidanceInstruction.Hidden;

public void SetGuidanceEnabled(bool enabled)
{
    GuidanceEnabled = enabled;
    RefreshNow();
}

public void ToggleGuidance() => SetGuidanceEnabled(!GuidanceEnabled);
```

Do not store a target `Node2D` across refreshes; resolve against the current mounted room every time so room replacement cannot leave stale references.

- [ ] **Step 5: Run resolver/runtime tests and mutation checks**

Run the Task 1 build/test commands. Expected: `PROLOGUE_TESTS_OK`.

Then perform two temporary mutations separately:

- Change `MechanismBossExit` path to `Mechanisms/BellChain`; expect the resolver suite to fail with the exact path assertion.
- Reintroduce clearing the private saved streak on every room change; expect the real upper-room checkpoint-retry test to fail on the first re-entry (`expected=1, actual=0`).

Restore each mutation and rerun GREEN.

- [ ] **Step 6: Commit resolution and orchestration**

```powershell
git add scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs scripts/prologue/guidance/PrologueGuidanceTargetView.cs scripts/prologue/guidance/IPrologueGuidanceVisual.cs scripts/prologue/guidance/PrologueGuidanceController.cs tests/unit/PrologueGuidanceTargetResolverTests.cs tests/integration/PrologueGuidanceRuntimeTests.cs tests/RunPrologueTests.cs
git commit -m "feat: resolve prologue guidance targets"
```

---

### Task 3: Replaceable Procedural Guidance Presentation

**Files:**
- Create: `scripts/prologue/guidance/PrologueGuidancePlaceholderVisual.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tests/integration/PrologueGuidanceRuntimeTests.cs`

**Interfaces:**
- Consumes: `IPrologueGuidanceVisual` and `PrologueGuidanceTargetView` from Task 2.
- Produces: stable runtime node `Hud/Guidance/VisualRoot`, public read-only `RenderedStepText`, `RenderedTargetCount`, and normal Godot `Visible` for integration evidence.

- [ ] **Step 1: Add failing structure and rendering assertions**

Extend the live runtime suite to require:

```csharp
PrologueGuidanceController guidance = level.GetNode<PrologueGuidanceController>("Guidance");
var visual = level.GetNode<PrologueGuidancePlaceholderVisual>("Hud/Guidance/VisualRoot");
TestAssert.True(visual is IPrologueGuidanceVisual, "stable guidance root implements replaceable visual contract");
TestAssert.True(visual.Visible && visual.RenderedStepText.Length > 0,
    "default-on guidance renders current step text");
TestAssert.True(visual.RenderedTargetCount is 1 or 2,
    "placeholder renders the model's one or two targets");
guidance.SetGuidanceEnabled(false);
TestAssert.True(!visual.Visible && visual.RenderedTargetCount == 0,
    "disabling guidance clears all visual state");
TestAssert.True(level.GetNode<Label>("Hud/Objective").Visible,
    "guidance toggle does not hide the normal objective HUD");
```

Also assert the HUD panel height is at least `160`, `VisualRoot.MouseFilter == Ignore`, and the first-level builder post-pack validation contains both `Guidance` and `Hud/Guidance/VisualRoot`.

- [ ] **Step 2: Run the runner and confirm the RED state**

Expected: compile failure for `PrologueGuidancePlaceholderVisual`, or runtime missing-node failure before builder regeneration.

- [ ] **Step 3: Implement the procedural placeholder renderer**

Use a full-viewport `Control` that delegates all decisions back to the controller and draws only supplied data:

```csharp
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue.Guidance;

public partial class PrologueGuidancePlaceholderVisual : Control, IPrologueGuidanceVisual
{
    public string RenderedStepText { get; private set; } = string.Empty;
    public int RenderedTargetCount => _targets.Count;

    private readonly List<PrologueGuidanceTargetView> _targets = new();
    private Label _text;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _text = GetNodeOrNull<Label>("Text");
    }

    public void Present(PrologueGuidanceInstruction instruction,
        IReadOnlyList<PrologueGuidanceTargetView> targets)
    {
        RenderedStepText = instruction.StepText;
        _targets.Clear();
        foreach (PrologueGuidanceTargetView target in targets) _targets.Add(target);
        if (_text != null)
        {
            string distance = _targets.Count == 0 ? string.Empty :
                "  /  " + string.Join(" | ", _targets.ConvertAll(t => $"{Mathf.RoundToInt(t.Distance)}"));
            _text.Text = "下一步：" + instruction.StepText + distance +
                (string.IsNullOrEmpty(instruction.HintText) ? string.Empty : "\n提示：" + instruction.HintText);
        }
        Visible = instruction.Visible;
        QueueRedraw();
    }

    public void Clear()
    {
        RenderedStepText = string.Empty;
        _targets.Clear();
        if (_text != null) _text.Text = string.Empty;
        Visible = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Rect2 safe = new(new Vector2(48, 48), Size - new Vector2(96, 96));
        foreach (PrologueGuidanceTargetView target in _targets)
        {
            Vector2 point = target.IsOnScreen ? target.ViewportPosition : new Vector2(
                Mathf.Clamp(target.ViewportPosition.X, safe.Position.X, safe.End.X),
                Mathf.Clamp(target.ViewportPosition.Y, safe.Position.Y, safe.End.Y));
            Color color = new("e6b85c");
            if (target.IsOnScreen)
            {
                Vector2[] diamond =
                [point + Vector2.Up * 12, point + Vector2.Right * 12,
                 point + Vector2.Down * 12, point + Vector2.Left * 12];
                DrawColoredPolygon(diamond, new Color(color, .72f));
                DrawPolyline([diamond[0], diamond[1], diamond[2], diamond[3], diamond[0]], color, 2f);
            }
            else
            {
                Vector2 forward = target.Direction == Vector2.Zero ? Vector2.Right : target.Direction.Normalized();
                Vector2 side = forward.Orthogonal();
                DrawColoredPolygon([point + forward * 18, point - forward * 12 + side * 10,
                    point - forward * 12 - side * 10], color);
            }
        }
    }
}
```

- [ ] **Step 4: Generate the stable HUD/controller hierarchy**

In `BuildPrologueLevel._Initialize`, add `new PrologueGuidanceController { Name = "Guidance" }` after `Completion` and before `PauseMenu`.

In `BuildHud`, change the panel size from `new Vector2(680, 112)` to `new Vector2(760, 164)`, then add:

```csharp
var guidance = new Control { Name = "Guidance" };
var visual = new PrologueGuidancePlaceholderVisual
{
    Name = "VisualRoot",
    Position = Vector2.Zero,
    Size = new Vector2(1280, 720),
    MouseFilter = Control.MouseFilterEnum.Ignore
};
var guidanceText = Label("Text", new Vector2(32, 88), new Vector2(720, 64), string.Empty);
guidanceText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
visual.AddChild(guidanceText);
guidance.AddChild(visual);
hud.AddChild(guidance);
```

Extend post-pack validation with:

```csharp
copy.GetNodeOrNull<PrologueGuidanceController>("Guidance") != null &&
copy.GetNodeOrNull<PrologueGuidancePlaceholderVisual>("Hud/Guidance/VisualRoot") != null
```

- [ ] **Step 5: Regenerate only the parent scene, then run focused tests**

Run:

```powershell
& 'G:\godot\dotnet-sdk\dotnet.exe' build .\Knight.csproj
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/prologue/BuildPrologueLevel.cs
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs
```

Expected: builder prints its validation success; tests print `PROLOGUE_TESTS_OK`. Inspect `git diff -- scenes/prologue/PrologueLevel.tscn` and confirm changes are the intended new nodes/properties plus serializer IDs only.

- [ ] **Step 6: Commit presentation and generated scene together**

```powershell
git add scripts/prologue/guidance/PrologueGuidancePlaceholderVisual.cs tools/build/prologue/BuildPrologueLevel.cs tests/integration/PrologueGuidanceRuntimeTests.cs scenes/prologue/PrologueLevel.tscn
git commit -m "feat: render replaceable prologue guidance"
```

---

### Task 4: Pause Toggle and Session Isolation

**Files:**
- Modify: `scripts/prologue/ProloguePauseMenu.cs`
- Modify: `tools/build/prologue/BuildPrologueLevel.cs`
- Modify: `tests/integration/PrologueCompletionTests.cs`

**Interfaces:**
- Consumes: `PrologueGuidanceController.ToggleGuidance()` and `.GuidanceEnabled`.
- Produces: `ProloguePauseCommand.ToggleGuidance` and button path `PauseMenu/Panel/GuidanceToggle`.

- [ ] **Step 1: Extend the real pause-menu test and make it fail**

Change the button contract assertion from four to five commands and add:

```csharp
Button toggle = level.GetNode<Button>("PauseMenu/Panel/GuidanceToggle");
PrologueGuidanceController guidance = level.GetNode<PrologueGuidanceController>("Guidance");
TestAssert.True(guidance.GuidanceEnabled, "guidance defaults on");
toggle.EmitSignal(Button.SignalName.Pressed);
TestAssert.True(!guidance.GuidanceEnabled && toggle.Text.Contains("OFF"),
    "pause command disables only guidance and reflects OFF");
TestAssert.True(tree.Paused && pause.IsOpen,
    "guidance toggle keeps the pause menu open and the game paused");
toggle.EmitSignal(Button.SignalName.Pressed);
TestAssert.True(guidance.GuidanceEnabled && toggle.Text.Contains("ON"),
    "second press restores guidance");
```

Before disposing, disable guidance. Load a second `PrologueLevel` and assert its controller is enabled, its normal objective is unchanged, and `ProjectSettings.HasSetting("autoload/PrologueGuidance") == false`.

Run the focused runner. Expected: missing `GuidanceToggle` node or enum member.

- [ ] **Step 2: Add the command and current-instance wiring**

Add `ToggleGuidance` to `ProloguePauseCommand`, export `GuidancePath = new("../Guidance")`, bind `Panel/GuidanceToggle`, and implement:

```csharp
case ProloguePauseCommand.ToggleGuidance:
    PrologueGuidanceController guidance = GetNodeOrNull<PrologueGuidanceController>(GuidancePath);
    guidance?.ToggleGuidance();
    UpdateGuidanceButton();
    break;
```

Call `UpdateGuidanceButton()` from `_Ready()` and `Open()`:

```csharp
private void UpdateGuidanceButton()
{
    Button button = GetNodeOrNull<Button>("Panel/GuidanceToggle");
    PrologueGuidanceController guidance = GetNodeOrNull<PrologueGuidanceController>(GuidancePath);
    if (button != null) button.Text = guidance?.GuidanceEnabled == false ? "GUIDANCE: OFF" : "GUIDANCE: ON";
}
```

Append `\nGUIDANCE: PAUSE MENU` to `ControlsText`. Do not add a global input action or save flag.

- [ ] **Step 3: Add the fifth generated button without covering controls**

In `BuildPauseMenu`, add:

```csharp
panel.AddChild(Button("GuidanceToggle", new Vector2(36, 148), "GUIDANCE: ON", 190));
```

Move `Panel/Controls` from `y = 160` to `y = 210` and reduce its height to `220`; preserve all existing buttons and their callbacks.

- [ ] **Step 4: Regenerate, run tests, and prove state isolation**

Run the Task 3 builder and runner commands. Expected: all pause assertions pass and `PROLOGUE_TESTS_OK` appears once.

Temporarily change `GuidanceEnabled` default to `false`; rerun and observe the fresh-instance assertion fail. Restore and rerun GREEN.

- [ ] **Step 5: Commit pause toggle**

```powershell
git add scripts/prologue/ProloguePauseMenu.cs tools/build/prologue/BuildPrologueLevel.cs tests/integration/PrologueCompletionTests.cs scenes/prologue/PrologueLevel.tscn
git commit -m "feat: toggle first-level guidance from pause"
```

---

### Task 5: Critical-Path and Boss Guidance Integration

**Files:**
- Modify: `tests/integration/PrologueGuidanceRuntimeTests.cs`
- Modify only if a test exposes a defect: `scripts/prologue/guidance/PrologueGuidanceController.cs`
- Modify only if a test exposes a defect: `scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs`

**Interfaces:**
- Consumes: complete Tasks 1–4 runtime contracts.
- Produces: runtime proof that the real shortcut and Boss states retarget immediately.

- [ ] **Step 1: Add the real shortcut-retarget integration sequence**

Reuse the existing `PrologueCompletionTests` helpers locally rather than exposing production shortcuts. Drive this sequence with real room exits and input:

1. Courtyard `Exits/ToCorridor`.
2. Real checkpoint overlap.
3. Real `Tutorials/ThreadAnchor` overlap.
4. Corridor `Exits/ToUpper`.
5. Upper `Exits/ToMechanism`.
6. Aim right/up and press `InputActions.Thread` at `Mechanisms/BellChain`.

Before the mechanism opens, assert `CurrentInstruction.Targets[0] == MechanismBellChain`. On the first frame after `Progress.BellChainShortcutOpen` becomes true, assert it changes to `MechanismBossExit`, resolves exactly one target, and its resolved target is `CurrentRoom.GetNode<Node2D>("Exits/P1Complete")`. This assertion directly guards the failure seen in hands-on report `20260814-120907-b77083f6`.

- [ ] **Step 2: Add all three real Boss-state assertions**

Enter through `Exits/P1Complete` and assert:

```csharp
TestAssert.Equal(PrologueGuidanceTargetKey.BossActivation,
    guidance.CurrentInstruction.Targets[0], "inactive arena points at activation threshold");
arena.BeginEncounter();
await Frames(tree, 2);
TestAssert.Equal(PrologueGuidanceTargetKey.BossBody,
    guidance.CurrentInstruction.Targets[0], "active arena points at live Boss");
```

Advance the real Boss into phase two using the existing `DamageRequest` and `ThreadInteraction` patterns from `FallenBellkeeperRuntimeTests`, wait until `CurrentFrame.State == FallenBellkeeperState.HammerThrown`, then assert `BossChain` and exact resolution to `CurrentBoss/ChainTarget`. Kill the Boss and assert the guidance visual clears when the completion objective becomes `Complete`.

- [ ] **Step 3: Add missing-target graceful-degradation evidence**

In an isolated room fixture, remove `Exits/P1Complete`, call `RefreshNow()`, and assert:

- `CurrentInstruction.StepText == "前往房间右侧金色出口，进入 Boss 区"`.
- `ResolvedTargetCount == 0`.
- the placeholder remains visible with text and has zero drawn targets.
- a captured Godot warning contains `Guidance target 'MechanismBossExit' is missing in room 'BellChainMechanism'.` exactly once after two refreshes.

Capture warnings through the existing Godot test logging pattern; do not weaken this into a no-throw-only assertion.

- [ ] **Step 4: Run RED, make only evidence-driven runtime corrections, then GREEN**

First run the full test runner before touching production and record the named failing assertion. Correct only the controller/resolver behavior implicated by that failure. Rerun until `PROLOGUE_TESTS_OK` appears once and no unexpected `ERROR`, `SCRIPT ERROR`, or `Parse Error` appears.

- [ ] **Step 5: Run three mutation checks**

Perform and restore each mutation separately:

- Delay shortcut retarget by one room transition; the immediate `MechanismBossExit` assertion must fail.
- Treat all active Boss states as `BossBody`; the `HammerThrown` assertion must fail.
- Clear the instruction when resolution fails; the missing-target text assertion must fail.

After restoring, rerun the runner once more.

- [ ] **Step 6: Commit critical-path coverage and any minimal fix**

```powershell
git add tests/integration/PrologueGuidanceRuntimeTests.cs scripts/prologue/guidance/PrologueGuidanceController.cs scripts/prologue/guidance/PrologueGuidanceTargetResolver.cs
git commit -m "test: verify prologue guidance critical path"
```

If neither production file changed, stage and commit only the test file.

---

### Task 6: Verification, Proof, Documentation, and Hands-On Handoff

**Files:**
- Modify: `README.md`
- Create: `docs/playtests/2026-08-14-prologue-guidance-audit.md`
- Do not commit: `screenshots/prologue-guidance/*`

**Interfaces:**
- Consumes: existing `tools/verify-prologue.ps1`, `tools/capture-prologue.ps1`, and `tools/play-prologue.ps1`.
- Produces: clean automated gate, reviewed visible proof, and a precise human acceptance checklist.

- [ ] **Step 1: Run the complete verification gate twice**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

Both runs must exit `0`, build with zero warnings/errors, execute all 13 builders, print `PROLOGUE_TESTS_OK` and `PROLOGUE_CRITICAL_PATH_OK` exactly once, pass semantic scene drift checks, and leave checked-in scene hashes unchanged. Scan fresh non-RED logs for `ERROR`, `SCRIPT ERROR`, `Parse Error`, and unhandled exceptions.

- [ ] **Step 2: Capture and visually inspect guidance proof**

Run the existing deterministic capture into a fresh output directory. If the current presentation driver does not pause long enough to show the new guidance states, add only deterministic camera/input dwell points to `tests/presentation/ProloguePresentation.cs`; do not let the recorder classify the capture as human evidence.

Review frames that show:

- courtyard/corridor next step and off-screen arrow,
- two simultaneous route markers,
- BellChain instruction changing to the right-side gold exit,
- inactive Boss threshold marker,
- active Boss marker,
- thrown-hammer chain marker,
- completion clearing all guidance,
- no overlap with player health, normal objective, prompt, or Boss HUD.

Record frame numbers, dimensions, capture duration, marker observations, and log scan result in `docs/playtests/2026-08-14-prologue-guidance-audit.md`. Do not report deterministic capture time as human clear time.

- [ ] **Step 3: Update durable project status**

In `README.md`:

- change the pause-menu description from four commands to five,
- state that dynamic guidance is mounted only in the complete prologue first level,
- document `Pause > GUIDANCE: ON/OFF` as session-only and default-on,
- add `Hud/Guidance/VisualRoot` to the asset/replacement table as project-owned procedural UI,
- mark automated guidance verification complete,
- keep the fresh human 10–15 minute completion pass open until the user actually completes it.

- [ ] **Step 4: Audit scope and repository hygiene**

Run:

```powershell
git diff --check
git status --short
git diff --name-only HEAD~5..HEAD
rg -n "autoload.*Guidance|PrologueGuidance" project.godot scripts scenes tools tests README.md
```

Confirm no Autoload, no tuning/collision/topology file changes, no hidden-alcove target, no art generation, and no unrelated Beehave/art/import file staged. Confirm every new `.cs` has its Godot-generated `.uid` only if import created one; do not manufacture UID contents by hand.

- [ ] **Step 5: Commit documentation and audit**

```powershell
git add README.md docs/playtests/2026-08-14-prologue-guidance-audit.md
git commit -m "docs: audit first-level guidance"
```

- [ ] **Step 6: Launch a fresh authorized hands-on run**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1
```

Ask the player to complete the level without oral navigation. Acceptance requires:

- the player reaches `Exits/P1Complete` from the BellChain room without external instruction,
- `BossAttempts >= 1`,
- the report no longer shows repeated corridor/branch/BellChain loops after `DefeatBellkeeper`,
- the player can explain what the text, arrow, and target diamond indicate,
- the UI does not hide the player or Boss HUD,
- a full clear produces a real human duration before evaluating the 10–15 minute target.

If the run still fails, diagnose the new report and create a new approved design delta; do not silently add stronger automation, teleportation, or difficulty changes.

---

## Plan Self-Review

- **Spec coverage:** Tasks 1–5 cover every objective/room row, equivalent branches, all six failure hints, all three Boss states, missing targets, completion clearing, pause toggle, current-session isolation, and first-level-only mounting. Task 6 covers structural verification, visible proof, durable README status, and a new human acceptance run.
- **Boundary coverage:** The plan preserves optional exploration, gameplay values, progress/telemetry semantics, generated-scene workflow, and the replaceable visual path.
- **Type consistency:** `PrologueGuidanceContext`, `PrologueGuidanceInstruction`, `PrologueGuidanceTargetKey`, `PrologueGuidanceTargetView`, `IPrologueGuidanceVisual`, and controller public members use the same names and signatures in every task.
- **Placeholder scan:** Every implementation step names concrete files, contracts, assertions, commands, expected failures, and completion evidence.
