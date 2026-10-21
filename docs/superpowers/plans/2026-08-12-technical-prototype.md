# 《无名钟墓》技术原型实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `E:\OurGame\Knight` 交付一个可运行、可自动验证的灰盒房间，证明移动、可变跳跃、空中冲刺、四向钟刃攻击、机械丝线和一名锈壳侍从能够组成好玩的基础循环。

**Architecture:** 核心规则放在不依赖场景树状态的 C# 模型中，Godot `Node` 只负责读取 InputMap、执行碰撞移动和转发表现事件。所有正式 `.tscn` 由 C# `SceneTree` 构建脚本生成并在保存后验证节点数量；灰盒仅使用 Godot 原生图元，不引入正式付费资产。

**Tech Stack:** Godot 4.7.1 Mono (`a13da4feb`)、C#、.NET SDK 9.0.317、`Godot.NET.Sdk/4.7.1`、`net9.0`、PowerShell、Git。

## Global Constraints

- 工作目录固定为 `E:\OurGame\Knight`，不得继续把 `F:\Documents\游戏` 当作正式工程。
- 正式玩法使用 Godot 4.7.1 Mono、C# 与 .NET 9；所有 Godot C# 类型必须声明为 `partial`。
- 首发平台为 Windows PC，键鼠与 Xbox/PlayStation 风格手柄必须映射到同一组 Godot Action。
- 玩法脚本不得写死物理键位，只能读取 `InputActions` 中定义的 Action 名称。
- 场景必须通过 C# 构建脚本生成；不得手写正式 `.tscn`。
- 玩家移动、战斗、生命和机械丝线保持独立文件与接口，表现层不得决定伤害、冷却或死亡。
- Godot AI 仅用于开发并在导出时排除；Beehave 与 Steering AI 只迁移保留，本计划不在核心玩法中使用它们。
- 本原型只使用程序化灰盒图形和基础音调；任何调用付费生成接口的正式资产工作必须先获得用户确认。
- 每项任务执行红—绿测试循环并单独提交；提交前必须运行该任务列出的完整验证命令。
- 原型目标为 1280×720、60 Hz 物理更新；实际手感参数必须集中在设置类中，不散落魔法数字。

---

## File Map

### Project foundation

- `project.godot`：引擎、显示、物理、主场景、插件和完整输入 Action。
- `Knight.csproj`：Godot C# SDK、`net9.0` 和动态加载设置。
- `AGENTS.md`、`godot.md`：从已验证旧工程迁入的项目协作与 Godot 构建规则。
- `.agents/skills/asset-gen/`：正式资产阶段使用的本地 Skill；本计划不调用付费生成。
- `addons/godot_ai/`、`addons/beehave/`、`addons/com.gdquest.godot-steering-ai-framework/`：已安装插件的项目副本。
- `scripts/shared/InputActions.cs`：唯一的 Action 名称来源。
- `tools/configure/ConfigureProject.cs`：通过 Godot 4.7.1 API 创建并持久化输入映射，避免手写版本敏感的事件序列。
- `tools/SceneBuildUtil.cs`：场景 Owner、节点计数、Pack、实例化验证与保存。
- `tools/verify.ps1`：统一编译、导入、单元测试和冒烟测试入口。

### Player

- `scripts/player/PlayerInputFrame.cs`：一帧归一化输入数据。
- `scripts/player/PlayerInputReader.cs`：将 Godot Action 转成 `PlayerInputFrame`。
- `scripts/player/PlayerMotionSettings.cs`：移动参数。
- `scripts/player/PlayerMotionState.cs`：可测试的移动计时与速度状态。
- `scripts/player/PlayerMotionResult.cs`：一帧移动求值结果。
- `scripts/player/PlayerMotionModel.cs`：纯移动规则。
- `scripts/player/PlayerMotor.cs`：`CharacterBody2D` 碰撞适配器。
- `scripts/player/PlayerCombatSettings.cs`：攻击时间、伤害与击退参数。
- `scripts/player/AttackFrame.cs`：一帧攻击阶段、方向和攻击 ID。
- `scripts/player/PlayerCombatModel.cs`：攻击状态与命中去重规则。
- `scripts/player/PlayerCombat.cs`：输入、攻击 Hitbox 和动画事件适配器。
- `scripts/player/PlayerDebugVisual.cs`：灰盒角色绘制。

### Shared combat

- `scripts/combat/DamageRequest.cs`：伤害值、方向、击退与来源 ID。
- `scripts/combat/HealthComponent.cs`：生命、受击间隔、死亡事件。
- `scripts/combat/Hitbox.cs`：攻击区域与单次攻击命中去重。
- `scripts/combat/Hurtbox.cs`：把命中请求转发给 `HealthComponent`。

### Mechanical thread

- `scripts/abilities/ThreadTargetKind.cs`：`Anchor`、`LightEnemy`、`HeavyEnemy`。
- `scripts/abilities/ThreadCandidate.cs`：纯目标筛选数据。
- `scripts/abilities/IThreadPullReceiver.cs`：可被丝线拉动的场景对象契约。
- `scripts/abilities/ThreadAbilityModel.cs`：射程、锥角、优先级与冷却。
- `scripts/abilities/ThreadTarget2D.cs`：场景目标接口适配器。
- `scripts/abilities/ThreadAbility.cs`：查询目标、可见性验证、牵引和 Line2D 反馈。

### Enemy and prototype room

- `scripts/enemies/RustShellState.cs`：侍从状态枚举。
- `scripts/enemies/RustShellModel.cs`：纯状态机。
- `scripts/enemies/RustShellFrame.cs`：一帧敌人状态、移动方向和攻击窗口。
- `scripts/enemies/RustShellSquire.cs`：移动、攻击、受击与丝线响应。
- `scripts/world/PrototypeCheckpoint.cs`：当前安全点。
- `scripts/world/KillZone.cs`：坠落后短距离恢复。
- `scripts/world/PrototypeRoomController.cs`：重生、调试状态和完成条件。
- `tools/build/BuildPlayer.cs`、`BuildRustShellSquire.cs`、`BuildPrototypeRoom.cs`：叶场景优先的生成脚本。
- `scenes/player/Player.tscn`、`scenes/enemies/RustShellSquire.tscn`、`scenes/prototype/PrototypeRoom.tscn`：生成产物。

### Tests and proof

- `tests/TestAssert.cs`：无外部依赖断言。
- `tests/RunPrototypeTests.cs`：单元测试入口。
- `tests/unit/*Tests.cs`：规则单元测试。
- `tests/smoke/PrototypeSmoke.cs`：运行实际房间的集成测试。
- `tests/presentation/PrototypePresentation.cs`：确定性展示输入。
- `tools/capture-prototype.ps1`：生成原型证明录像。
- `screenshots/prototype/`：被 Git 忽略的捕获帧和视频。

---

### Task 1: Migrate the verified foundation and establish the build gate

**Files:**
- Create: `project.godot`
- Create: `Knight.csproj`
- Create: `AGENTS.md`
- Create: `godot.md`
- Create: `scripts/shared/InputActions.cs`
- Create: `tools/configure/ConfigureProject.cs`
- Create: `tests/TestAssert.cs`
- Create: `tests/unit/ProjectFoundationTests.cs`
- Create: `tests/RunPrototypeTests.cs`
- Create: `tools/SceneBuildUtil.cs`
- Create: `tools/verify.ps1`
- Copy: `.agents/skills/asset-gen/`
- Copy: `addons/godot_ai/`
- Copy: `addons/beehave/`
- Copy: `addons/com.gdquest.godot-steering-ai-framework/`
- Modify: `.gitignore`
- Modify: `README.md`

**Interfaces:**
- Produces: `InputActions.MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`, `Jump`, `Dash`, `Attack`, `Thread`, `Ember`, `EchoRecord`, `EchoReplay`, `Pause` as `StringName` constants.
- Produces: `TestAssert.True(bool, string)`, `Equal<T>(T, T, string)`, `Near(float, float, float, string)`.
- Produces: `SceneBuildUtil.PackAndSave(Node root, string resourcePath)` returning `Error` and rejecting dropped nodes.
- Produces: `tools/verify.ps1 -SkipSmoke` as the common verification command used by later tasks.

- [ ] **Step 1: Copy only the verified development foundation**

Use PowerShell to copy the existing project-local instructions, asset Skill and three installed add-ons. Do not copy `.godot`, logs, runtime downloads, test caches or the old `project.godot`.

```powershell
$sourceRoot = 'F:\Documents\游戏'
$targetRoot = 'E:\OurGame\Knight'
Copy-Item -LiteralPath "$sourceRoot\AGENTS.md" -Destination "$targetRoot\AGENTS.md"
Copy-Item -LiteralPath "$sourceRoot\godot.md" -Destination "$targetRoot\godot.md"
New-Item -ItemType Directory -Path "$targetRoot\.agents\skills" -Force | Out-Null
Copy-Item -LiteralPath "$sourceRoot\.agents\skills\asset-gen" -Destination "$targetRoot\.agents\skills\asset-gen" -Recurse
New-Item -ItemType Directory -Path "$targetRoot\addons" -Force | Out-Null
Copy-Item -LiteralPath "$sourceRoot\addons\godot_ai" -Destination "$targetRoot\addons\godot_ai" -Recurse
Copy-Item -LiteralPath "$sourceRoot\addons\beehave" -Destination "$targetRoot\addons\beehave" -Recurse
Copy-Item -LiteralPath "$sourceRoot\addons\com.gdquest.godot-steering-ai-framework" -Destination "$targetRoot\addons\com.gdquest.godot-steering-ai-framework" -Recurse
```

- [ ] **Step 2: Create the minimal C# project and a deliberately failing foundation test**

Create `Knight.csproj` exactly as follows:

```xml
<Project Sdk="Godot.NET.Sdk/4.7.1">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <RootNamespace>Knight</RootNamespace>
  </PropertyGroup>
</Project>
```

Create a minimal `project.godot` with `config_version=5`, project name `Knight`, feature `4.7`, assembly name `Knight`, 1280×720 viewport, `canvas_items` stretch, `gl_compatibility`, 60 physics ticks, and only `res://addons/godot_ai/plugin.cfg` enabled. Do not add the input section yet.

Create the test runner:

```csharp
// tests/RunPrototypeTests.cs
using Godot;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunPrototypeTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            ProjectFoundationTests.Run();
            GD.Print("PROTOTYPE_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
```

Create the assertion helper exactly as follows:

```csharp
// tests/TestAssert.cs
using System;

namespace Knight.Tests;

public static class TestAssert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
            throw new InvalidOperationException($"Assertion failed: {message}; expected={expected}, actual={actual}");
    }

    public static void Near(float expected, float actual, float tolerance, string message)
    {
        if (MathF.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Assertion failed: {message}; expected={expected}, actual={actual}, tolerance={tolerance}");
    }
}
```

Create `ProjectFoundationTests.Run()` so it asserts all 12 Action names exist and the configured assembly name is `Knight`. Iterate the `StringName` fields from `InputActions` explicitly and call `TestAssert.True(InputMap.HasAction(action), $"missing action {action}")`. This test must fail before the input map is added.

- [ ] **Step 3: Run the test to prove the missing input map is detected**

```powershell
$dotnet = 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317\dotnet.exe'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $dotnet build 'E:\OurGame\Knight\Knight.csproj'
& $godot --headless --path 'E:\OurGame\Knight' --script 'res://tests/RunPrototypeTests.cs'
```

Expected: build succeeds; test exits nonzero with an assertion naming the first missing Action.

- [ ] **Step 4: Add the complete Action map and shared constants**

Define the following bindings; each row is one Action and every Action has both keyboard and controller input:

| Action | Keyboard | Controller |
| --- | --- | --- |
| `move_left` | A / Left Arrow | Left stick left / D-pad left |
| `move_right` | D / Right Arrow | Left stick right / D-pad right |
| `move_up` | W / Up Arrow | Left stick up / D-pad up |
| `move_down` | S / Down Arrow | Left stick down / D-pad down |
| `jump` | Space | A / Cross |
| `dash` | Left Shift | B / Circle |
| `attack` | J | X / Square |
| `thread` | K | Y / Triangle |
| `ember` | L | Left shoulder |
| `echo_record` | U | Right shoulder |
| `echo_replay` | I | Left trigger |
| `pause` | Escape | Start / Options |

`InputActions.cs` must be the only place that spells these strings:

```csharp
using Godot;

namespace Knight.Shared;

public static class InputActions
{
    public static readonly StringName MoveLeft = "move_left";
    public static readonly StringName MoveRight = "move_right";
    public static readonly StringName MoveUp = "move_up";
    public static readonly StringName MoveDown = "move_down";
    public static readonly StringName Jump = "jump";
    public static readonly StringName Dash = "dash";
    public static readonly StringName Attack = "attack";
    public static readonly StringName Thread = "thread";
    public static readonly StringName Ember = "ember";
    public static readonly StringName EchoRecord = "echo_record";
    public static readonly StringName EchoReplay = "echo_replay";
    public static readonly StringName Pause = "pause";
}
```

Implement `tools/configure/ConfigureProject.cs` as a `SceneTree`. Its `_Initialize()` calls a local `AddAction(StringName action, params InputEvent[] events)` helper. The helper erases an existing action, adds it with deadzone `0.2f`, and registers every event using `InputMap.ActionAddEvent`. Construct keyboard events with `PhysicalKeycode`, controller face/shoulder events with `ButtonIndex`, stick events with `Axis` and `AxisValue`, and call `ProjectSettings.Save()` after all actions are configured. Exit nonzero if saving does not return `Error.Ok`.

Run the configurator with the installed Mono editor so Godot serializes its own version-specific syntax:

```powershell
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --headless --path 'E:\OurGame\Knight' --script 'res://tools/configure/ConfigureProject.cs'
```

After it runs, inspect `project.godot` and assert all 12 `[input]` entries are present. Do not replace the generated event objects with hand-authored serialization.

- [ ] **Step 5: Implement validated scene packing and the common verifier**

`SceneBuildUtil.PackAndSave` must set Owner recursively for locally created descendants, skip recursion below any child whose `SceneFilePath` is non-empty, pack the scene, instantiate it, compare node counts, and only then call `ResourceSaver.Save`. Return `Error.InvalidData` when nodes were dropped.

`tools/verify.ps1` must:

1. Set `DOTNET_ROOT` to `F:\Documents\游戏\tools\runtime\dotnet-9.0.317` for the process.
2. Run `dotnet build Knight.csproj`.
3. Run Godot `--headless --path . --import`.
4. Run `res://tests/RunPrototypeTests.cs`.
5. Unless `-SkipSmoke` is passed, run `res://tests/smoke/PrototypeSmoke.cs` when that file exists.
6. Search each generated log for `ERROR:`, `SCRIPT ERROR:` and `Parse Error`; fail if any are present.

Use this implementation skeleton for the two critical gates:

```csharp
// tools/SceneBuildUtil.cs
using Godot;

namespace Knight.Tools;

public static class SceneBuildUtil
{
    public static Error PackAndSave(Node root, string resourcePath)
    {
        SetOwners(root, root);
        int expected = Count(root);
        var packed = new PackedScene();
        Error packError = packed.Pack(root);
        if (packError != Error.Ok) return packError;
        Node copy = packed.Instantiate();
        int actual = Count(copy);
        copy.Free();
        if (actual != expected)
        {
            GD.PushError($"Scene node count mismatch: expected={expected}, actual={actual}");
            return Error.InvalidData;
        }
        return ResourceSaver.Save(packed, resourcePath);
    }

    private static void SetOwners(Node node, Node owner)
    {
        foreach (Node child in node.GetChildren())
        {
            child.Owner = owner;
            if (string.IsNullOrEmpty(child.SceneFilePath)) SetOwners(child, owner);
        }
    }

    private static int Count(Node node)
    {
        int count = 1;
        foreach (Node child in node.GetChildren()) count += Count(child);
        return count;
    }
}
```

```powershell
# tools/verify.ps1
param([switch]$SkipSmoke)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dotnetRoot = 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$env:DOTNET_ROOT = $dotnetRoot
$env:Path = "$dotnetRoot;$env:Path"
& "$dotnetRoot\dotnet.exe" build (Join-Path $project 'Knight.csproj')
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$logs = [System.Collections.Generic.List[string]]::new()
$importLog = Join-Path $project 'verify-import.log'; $logs.Add($importLog)
& $godot --headless --path $project --import --log-file $importLog
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$unitLog = Join-Path $project 'verify-unit.log'; $logs.Add($unitLog)
& $godot --headless --path $project --script 'res://tests/RunPrototypeTests.cs' --log-file $unitLog
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$smoke = Join-Path $project 'tests\smoke\PrototypeSmoke.cs'
if (-not $SkipSmoke -and (Test-Path -LiteralPath $smoke)) {
    $smokeLog = Join-Path $project 'verify-smoke.log'; $logs.Add($smokeLog)
    & $godot --headless --path $project --script 'res://tests/smoke/PrototypeSmoke.cs' --log-file $smokeLog
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
$errors = foreach ($log in $logs) {
    Select-String -LiteralPath $log -Pattern 'ERROR:','SCRIPT ERROR:','Parse Error' -SimpleMatch
}
if (@($errors).Count) { $errors | ForEach-Object { $_.Line }; exit 1 }
```

- [ ] **Step 6: Run the complete foundation gate**

Run:

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
```

Expected: `PROTOTYPE_TESTS_OK`, zero build errors, zero import/script/parse errors.

- [ ] **Step 7: Update durable status and commit**

Update `README.md` with verified engine/SDK paths, current milestone `技术原型`, the input table, and an asset table marking every visible asset as `程序化灰盒 / 无付费生成`.

```powershell
git add .gitignore README.md AGENTS.md godot.md project.godot Knight.csproj .agents addons scripts/shared tests tools
git commit -m "chore: bootstrap Godot C# project"
```

---

### Task 2: Build responsive movement as a tested model plus Godot adapter

**Files:**
- Create: `scripts/player/PlayerInputFrame.cs`
- Create: `scripts/player/PlayerInputReader.cs`
- Create: `scripts/player/PlayerMotionSettings.cs`
- Create: `scripts/player/PlayerMotionState.cs`
- Create: `scripts/player/PlayerMotionResult.cs`
- Create: `scripts/player/PlayerMotionModel.cs`
- Create: `scripts/player/PlayerMotor.cs`
- Create: `scripts/player/PlayerDebugVisual.cs`
- Create: `tests/unit/PlayerMotionModelTests.cs`
- Create: `tools/build/BuildPlayer.cs`
- Create: `scenes/player/Player.tscn` (generated)
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Produces: `PlayerInputFrame(float MoveAxis, Vector2 Aim, bool JumpPressed, bool JumpHeld, bool DashPressed, bool AttackPressed, bool ThreadPressed)`.
- Produces: `PlayerMotionModel.Step(PlayerMotionState state, PlayerInputFrame input, bool onFloor, double delta) -> PlayerMotionResult`.
- Produces: `PlayerMotor.ExternalImpulse(Vector2 velocity)` for mechanical thread and knockback.
- Produces node paths: `Player/Input`, `Player/Motor`, `Player/Visual`, `Player/BodyCollider`.

- [ ] **Step 1: Write movement tests before implementation**

Add `PlayerMotionModelTests.Run()` to the runner. Tests must prove:

```csharp
public static void Run()
{
    GroundAccelerationReachesConfiguredSpeedWithoutOvershoot();
    BufferedJumpFiresOnTheFirstGroundedFrame();
    CoyoteJumpFiresInsideTheConfiguredWindow();
    ReleasedJumpCutsUpwardVelocity();
    AirDashCanOnlyBeConsumedOnceBeforeLanding();
    LandingRestoresTheAirDash();
}
```

For the buffered-jump test, press jump while airborne with `JumpBufferRemaining=0`, advance one frame, then call `Step` with `onFloor=true`; assert `StartedJump` and a negative Y velocity. For the dash test, assert the first airborne dash returns `StartedDash=true` and the second returns false until an `onFloor=true` step occurs.

- [ ] **Step 2: Run the red test**

Run:

```powershell
& 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317\dotnet.exe' build 'E:\OurGame\Knight\Knight.csproj'
```

Expected: compile failure because `PlayerMotionModel` and its types do not exist.

- [ ] **Step 3: Implement the minimal deterministic motion model**

Use one settings object with these initial tuning values:

```csharp
namespace Knight.Player;

public sealed record PlayerMotionSettings(
    float MaxRunSpeed = 260f,
    float GroundAcceleration = 2200f,
    float GroundDeceleration = 2800f,
    float AirAcceleration = 1500f,
    float Gravity = 1850f,
    float MaxFallSpeed = 780f,
    float JumpSpeed = 590f,
    float JumpCutSpeed = 230f,
    float CoyoteSeconds = 0.10f,
    float JumpBufferSeconds = 0.12f,
    float DashSpeed = 620f,
    float DashSeconds = 0.14f);
```

`Step` must update jump buffer and coyote timers, restore air dash on ground, prioritize an accepted dash over gravity, consume buffered jump when coyote time is available, apply jump cut when jump is released, and use `Mathf.MoveToward` for horizontal acceleration/deceleration. Clamp fall speed to `MaxFallSpeed`. Do not read `Input` or query a Node inside the model.

Define the input and mutable model state explicitly:

```csharp
// scripts/player/PlayerInputFrame.cs
using Godot;

namespace Knight.Player;

public readonly record struct PlayerInputFrame(
    float MoveAxis,
    Vector2 Aim,
    bool JumpPressed,
    bool JumpHeld,
    bool DashPressed,
    bool AttackPressed,
    bool ThreadPressed);
```

```csharp
// scripts/player/PlayerMotionState.cs
using Godot;

namespace Knight.Player;

public sealed class PlayerMotionState
{
    public Vector2 Velocity { get; set; }
    public float CoyoteRemaining { get; set; }
    public float JumpBufferRemaining { get; set; }
    public float DashRemaining { get; set; }
    public Vector2 DashDirection { get; set; } = Vector2.Right;
    public bool AirDashAvailable { get; set; } = true;
    public bool WasOnFloor { get; set; }
}
```

Define the result as:

```csharp
using Godot;

namespace Knight.Player;

public readonly record struct PlayerMotionResult(
    Vector2 Velocity,
    bool StartedJump,
    bool StartedDash,
    bool Landed);
```

- [ ] **Step 4: Make the movement tests pass**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
```

Expected: six movement tests and the foundation test pass.

- [ ] **Step 5: Add the Godot adapter and generated player scene**

`PlayerInputReader` reads `Input.GetAxis(InputActions.MoveLeft, InputActions.MoveRight)` and `Input.GetVector(...)`; it stores the current physics-frame snapshot. `PlayerMotor` obtains its parent `CharacterBody2D`, calls the model before `MoveAndSlide`, writes the resulting velocity, and refreshes floor state on the next physics frame. `ExternalImpulse` replaces current velocity and cancels an active dash.

`PlayerDebugVisual._Draw()` must draw a unique gray-box silhouette: bone-white rounded body, copper joint circle and a short rectangular bell-blade. It must not draw a horned mask.

`BuildPlayer.cs` generates a `CharacterBody2D` with a capsule collider, `PlayerInputReader`, `PlayerMotor`, `PlayerDebugVisual` and `Camera2D`. Use `SceneBuildUtil`; set the root script last if one is attached, re-fetch it through a temporary parent, then pack and validate.

Run:

```powershell
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --headless --path 'E:\OurGame\Knight' --script 'res://tools/build/BuildPlayer.cs'
```

Expected: `scenes/player/Player.tscn` exists and the builder prints matching pre-pack/post-instantiate node counts.

- [ ] **Step 6: Verify and commit movement**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
git add scripts/player tests tools/build scenes/player
git commit -m "feat: add responsive player movement"
```

---

### Task 3: Add health, four-direction attacks and hit feedback contracts

**Files:**
- Create: `scripts/combat/DamageRequest.cs`
- Create: `scripts/combat/HealthComponent.cs`
- Create: `scripts/combat/Hitbox.cs`
- Create: `scripts/combat/Hurtbox.cs`
- Create: `scripts/player/AttackDirection.cs`
- Create: `scripts/player/PlayerCombatSettings.cs`
- Create: `scripts/player/AttackFrame.cs`
- Create: `scripts/player/PlayerCombatModel.cs`
- Create: `scripts/player/PlayerCombat.cs`
- Create: `tests/unit/HealthComponentTests.cs`
- Create: `tests/unit/PlayerCombatModelTests.cs`
- Modify: `tools/build/BuildPlayer.cs`
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Produces: `DamageRequest(int Amount, Vector2 Direction, float Knockback, ulong SourceAttackId)`.
- Produces: `HealthComponent.ApplyDamage(DamageRequest request) -> bool`, signal `Damaged(int remaining, Vector2 impulse)`, signal `Died()`.
- Produces: `PlayerCombatModel.TryStart(AttackDirection direction) -> ulong?` and `Tick(double delta) -> AttackFrame`.
- Consumes: `PlayerInputReader.Current`, `PlayerMotor.ExternalImpulse(Vector2)`.

- [ ] **Step 1: Write failing combat tests**

Add tests for the following exact behavior:

```csharp
public static void Run()
{
    DamageReducesHealthOncePerSourceAttack();
    InvulnerabilityRejectsImmediateSecondAttack();
    HealthNeverDropsBelowZeroAndDeathFiresOnce();
    AttackUsesUpWhenAimYIsNegativePastDeadZone();
    AttackUsesDownOnlyWhileAirborne();
    AttackCannotRestartDuringActiveOrRecoveryFrames();
    ANewAttackReceivesANewSourceAttackId();
}
```

Use a default attack timeline of 0.06 seconds startup, 0.10 active, 0.14 recovery. Assert that `AttackFrame.IsActive` is false before startup, true during the active interval and false in recovery.

- [ ] **Step 2: Run the red build**

```powershell
& 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317\dotnet.exe' build 'E:\OurGame\Knight\Knight.csproj'
```

Expected: missing combat types produce a compile failure.

- [ ] **Step 3: Implement health and attack models**

Use these immutable request types:

```csharp
using Godot;

namespace Knight.Combat;

public readonly record struct DamageRequest(
    int Amount,
    Vector2 Direction,
    float Knockback,
    ulong SourceAttackId);
```

`HealthComponent` starts with exported `MaxHealth=5` and `InvulnerabilitySeconds=0.35`. It rejects non-positive damage, repeated `SourceAttackId`, damage during invulnerability and all damage after death. `PlayerCombatModel` owns a monotonically increasing attack ID and chooses `Up` before horizontal when aim Y ≤ -0.5, `Down` only when airborne and aim Y ≥ 0.5, otherwise horizontal using facing.

Define direction and settings as:

```csharp
// scripts/player/AttackDirection.cs
namespace Knight.Player;

public enum AttackDirection { Horizontal, Up, Down }
```

```csharp
// scripts/player/PlayerCombatSettings.cs
namespace Knight.Player;

public sealed record PlayerCombatSettings(
    float StartupSeconds = 0.06f,
    float ActiveSeconds = 0.10f,
    float RecoverySeconds = 0.14f,
    int Damage = 1,
    float Knockback = 260f,
    float DownBounceSpeed = 360f,
    float AimDeadZone = 0.5f);
```

`HealthComponent` stores processed attack IDs in a 64-entry FIFO plus `HashSet<ulong>`; when the 65th ID arrives, evict the oldest. This preserves duplicate protection without unbounded growth.

Define the frame returned by `Tick` as:

```csharp
namespace Knight.Player;

public readonly record struct AttackFrame(
    ulong AttackId,
    AttackDirection Direction,
    bool IsActive,
    bool Finished);
```

- [ ] **Step 4: Run tests and confirm the rules pass**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
```

Expected: all health and combat tests pass with `PROTOTYPE_TESTS_OK`.

- [ ] **Step 5: Wire attack and hurt areas into the player scene**

`Hitbox` activates only during `AttackFrame.IsActive` and keeps a `HashSet<ulong>` of hurtbox instance IDs for the current attack. `Hurtbox` references its sibling/parent `HealthComponent`. `PlayerCombat` positions a rectangular attack Area at `(facing*42, 0)`, `(0,-42)` or `(0,42)`, emits `AttackStarted` and `AttackConnected`, and applies downward-attack bounce velocity `(currentX,-360)` through `PlayerMotor.ExternalImpulse` when a hit confirms.

The adapter's hit path must follow this shape:

```csharp
// scripts/combat/Hitbox.cs (core methods)
private ulong _attackId;
private readonly HashSet<ulong> _hitInstances = new();

public void Begin(ulong attackId)
{
    _attackId = attackId;
    _hitInstances.Clear();
    Monitoring = true;
}

public void End() => Monitoring = false;

private void OnAreaEntered(Area2D area)
{
    if (area is not Hurtbox hurtbox || !_hitInstances.Add(hurtbox.GetInstanceId())) return;
    Vector2 direction = GlobalPosition.DirectionTo(hurtbox.GlobalPosition);
    if (hurtbox.Receive(new DamageRequest(1, direction, 260f, _attackId)))
        EmitSignal(SignalName.Connected, hurtbox);
}
```

```csharp
// scripts/player/PlayerCombat.cs (direction placement)
private static Vector2 HitboxOffset(AttackDirection direction, int facing) => direction switch
{
    AttackDirection.Up => new Vector2(0, -42),
    AttackDirection.Down => new Vector2(0, 42),
    _ => new Vector2(facing * 42, 0)
};
```

Regenerate `Player.tscn`. Verify it contains `Health`, `Hurtbox`, `Combat`, `AttackHitbox` and collision shapes, and that pack/instantiate counts match.

- [ ] **Step 6: Verify and commit combat**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
git add scripts/combat scripts/player tests tools/build scenes/player
git commit -m "feat: add player combat and health"
```

---

### Task 4: Implement mechanical-thread targeting and pull behavior

**Files:**
- Create: `scripts/abilities/ThreadTargetKind.cs`
- Create: `scripts/abilities/ThreadCandidate.cs`
- Create: `scripts/abilities/IThreadPullReceiver.cs`
- Create: `scripts/abilities/ThreadAbilitySettings.cs`
- Create: `scripts/abilities/ThreadAbilityModel.cs`
- Create: `scripts/abilities/ThreadTarget2D.cs`
- Create: `scripts/abilities/ThreadAbility.cs`
- Create: `tests/unit/ThreadAbilityModelTests.cs`
- Modify: `tools/build/BuildPlayer.cs`
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Produces: `ThreadCandidate(ulong Id, Vector2 Position, ThreadTargetKind Kind, bool HasLineOfSight)`.
- Produces: `ThreadAbilityModel.SelectTarget(Vector2 origin, Vector2 aim, IReadOnlyList<ThreadCandidate> candidates) -> ThreadCandidate?`.
- Produces: `ThreadAbilityModel.TryConsume(double nowSeconds) -> bool` with 0.35 second cooldown.
- Produces: `ThreadTarget2D.ApplyPull(Node2D player, Vector2 origin)`.
- Consumes: `PlayerMotor.ExternalImpulse(Vector2)` and `InputActions.Thread`.

- [ ] **Step 1: Write failing target-selection and cooldown tests**

```csharp
public static void Run()
{
    RejectsTargetsOutsideFourHundredPixels();
    RejectsTargetsOutsideThirtyDegreeAimCone();
    RejectsOccludedTargets();
    ChoosesSmallestAngularErrorThenNearestDistance();
    CooldownRejectsUseBeforePointThreeFiveSeconds();
    CooldownAllowsUseAtBoundary();
}
```

Use origin `(0,0)`, aim `(1,0)`, range `400`, cone half-angle `30°`. For tie-breaking, two candidates with equal angular error must choose the nearer candidate; an exact remaining tie chooses the lower instance ID to remain deterministic.

- [ ] **Step 2: Run the red build**

```powershell
& 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317\dotnet.exe' build 'E:\OurGame\Knight\Knight.csproj'
```

Expected: compile failure for the missing thread model.

- [ ] **Step 3: Implement pure selection and cooldown rules**

The settings are fixed for the first playable pass:

```csharp
namespace Knight.Abilities;

public sealed record ThreadAbilitySettings(
    float Range = 400f,
    float ConeHalfAngleDegrees = 30f,
    float CooldownSeconds = 0.35f,
    float PlayerPullSpeed = 720f,
    float EnemyPullSpeed = 540f,
    float VisualSeconds = 0.12f);
```

Selection must normalize the aim vector, discard zero-length aim, range failures, cone failures and occluded candidates, then order by angular error, squared distance and ID. The model does not query physics or nodes.

Define the selection types:

```csharp
// scripts/abilities/ThreadTargetKind.cs
namespace Knight.Abilities;

public enum ThreadTargetKind { Anchor, LightEnemy, HeavyEnemy }
```

```csharp
// scripts/abilities/ThreadCandidate.cs
using Godot;

namespace Knight.Abilities;

public readonly record struct ThreadCandidate(
    ulong Id,
    Vector2 Position,
    ThreadTargetKind Kind,
    bool HasLineOfSight);
```

```csharp
// scripts/abilities/IThreadPullReceiver.cs
using Godot;

namespace Knight.Abilities;

public interface IThreadPullReceiver
{
    void ApplyThreadPull(Vector2 velocity);
}
```

- [ ] **Step 4: Run the model tests**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
```

Expected: all thread tests pass.

- [ ] **Step 5: Add the Godot target adapter and visible thread**

`ThreadAbility` collects nodes in group `thread_targets`, converts them into candidates and uses `PhysicsDirectSpaceState2D.IntersectRay` to set `HasLineOfSight`. On success:

- `Anchor`: pull the player toward the target at `PlayerPullSpeed`.
- `LightEnemy`: call the target body with velocity toward the player at `EnemyPullSpeed`.
- `HeavyEnemy`: pull the player toward the target at `PlayerPullSpeed`.

Draw a two-point `Line2D` for exactly `VisualSeconds`; clear it on cooldown completion, target deletion, death or scene exit. No rope simulation is included in this prototype.

Use this target contract so the ability never switches on concrete enemy classes:

```csharp
// scripts/abilities/ThreadTarget2D.cs
using Godot;

namespace Knight.Abilities;

public partial class ThreadTarget2D : Node2D
{
    [Export] public ThreadTargetKind Kind { get; set; }
    [Export] public NodePath PullReceiverPath { get; set; } = "..";

    public override void _Ready() => AddToGroup("thread_targets");

    public void ApplyPull(Node2D player, Vector2 origin, ThreadAbilitySettings settings)
    {
        Node receiver = GetNode(PullReceiverPath);
        Vector2 towardTarget = origin.DirectionTo(GlobalPosition);
        Vector2 towardPlayer = GlobalPosition.DirectionTo(player.GlobalPosition);
        if (Kind == ThreadTargetKind.LightEnemy && receiver is IThreadPullReceiver pullReceiver)
            pullReceiver.ApplyThreadPull(towardPlayer * settings.EnemyPullSpeed);
        else if (player.GetNodeOrNull<PlayerMotor>("Motor") is { } motor)
            motor.ExternalImpulse(towardTarget * settings.PlayerPullSpeed);
    }
}
```

`ThreadAbility` converts each valid `ThreadTarget2D` to `ThreadCandidate`, calls `SelectTarget`, resolves the selected ID back to its node, then calls `ApplyPull`. `Line2D.Points` must be set to local `(Vector2.Zero, ToLocal(target.GlobalPosition))` and cleared by a `SceneTreeTimer` timeout guarded with `IsInstanceValid`.

Regenerate `Player.tscn` with `ThreadAbility` and `ThreadLine` children.

- [ ] **Step 6: Verify and commit the thread ability**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
git add scripts/abilities tests tools/build scenes/player
git commit -m "feat: add mechanical thread ability"
```

---

### Task 5: Add the Rust-Shell Squire as a readable test enemy

**Files:**
- Create: `scripts/enemies/RustShellState.cs`
- Create: `scripts/enemies/RustShellSettings.cs`
- Create: `scripts/enemies/RustShellModel.cs`
- Create: `scripts/enemies/RustShellFrame.cs`
- Create: `scripts/enemies/RustShellSquire.cs`
- Create: `scripts/enemies/RustShellDebugVisual.cs`
- Create: `tests/unit/RustShellModelTests.cs`
- Create: `tools/build/BuildRustShellSquire.cs`
- Create: `scenes/enemies/RustShellSquire.tscn` (generated)
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Produces states: `Idle`, `Chase`, `Telegraph`, `Attack`, `Recover`, `Stunned`, `Dead`.
- Produces: `RustShellModel.Step(float signedDistanceToPlayer, bool seesPlayer, bool stunned, bool dead, double delta) -> RustShellFrame`.
- Produces node paths: `RustShellSquire/Health`, `Hurtbox`, `AttackHitbox`, `ThreadTarget`, `Visual`.
- Consumes: `DamageRequest`, `ThreadTargetKind.LightEnemy`.

- [ ] **Step 1: Write failing enemy state tests**

```csharp
public static void Run()
{
    IdleRemainsStillOutsideDetectionRange();
    ChaseMovesTowardVisiblePlayer();
    AttackAlwaysPassesThroughTelegraphFirst();
    TelegraphLastsPointThreeFiveSeconds();
    AttackWindowLastsPointOneTwoSeconds();
    RecoverPreventsImmediateSecondAttack();
    ThreadPullCausesPointTwoFiveSecondStun();
    DeadOverridesEveryOtherState();
}
```

Initial settings: detection `360`, attack range `70`, speed `95`, telegraph `0.35 s`, attack `0.12 s`, recovery `0.55 s`, thread stun `0.25 s`.

Define them explicitly:

```csharp
namespace Knight.Enemies;

public sealed record RustShellSettings(
    float DetectionRange = 360f,
    float AttackRange = 70f,
    float MoveSpeed = 95f,
    float TelegraphSeconds = 0.35f,
    float AttackSeconds = 0.12f,
    float RecoverySeconds = 0.55f,
    float ThreadStunSeconds = 0.25f);
```

Define the state names exactly once:

```csharp
// scripts/enemies/RustShellState.cs
namespace Knight.Enemies;

public enum RustShellState
{
    Idle,
    Chase,
    Telegraph,
    Attack,
    Recover,
    Stunned,
    Dead
}
```

- [ ] **Step 2: Run the red build**

```powershell
& 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317\dotnet.exe' build 'E:\OurGame\Knight\Knight.csproj'
```

Expected: missing enemy model causes compile failure.

- [ ] **Step 3: Implement the finite-state model**

The model accepts only scalar signed horizontal distance and booleans; it does not fetch the player or move a Node. Every transition resets an explicit state timer. `Dead` is absorbing. `Stunned` returns to `Chase` when its timer expires and the player remains visible, otherwise `Idle`.

Define its result as:

```csharp
namespace Knight.Enemies;

public readonly record struct RustShellFrame(
    RustShellState State,
    float MoveDirection,
    bool AttackActive,
    bool EnteredState);
```

- [ ] **Step 4: Run tests and verify all state timings**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
```

Expected: all eight enemy tests pass.

- [ ] **Step 5: Build the enemy adapter and generated scene**

`RustShellSquire` finds the player through group `player`, feeds distance and visibility into the model, applies horizontal velocity, flips its visual facing, activates its attack Hitbox only in `Attack`, and stops moving in `Telegraph`, `Recover`, `Stunned` and `Dead`. A successful thread pull sets velocity toward the player and requests `Stunned`.

The gray-box visual uses a rust-brown rectangular shell and one circular back bell. Telegraph changes the bell to amber and performs a short backward lean; Attack flashes the blade shape red. This color change is accompanied by a visible pose change so it is not color-only.

Keep Node work in this adapter boundary:

```csharp
// scripts/enemies/RustShellSquire.cs (physics core)
public override void _PhysicsProcess(double delta)
{
    Node2D? player = GetTree().GetFirstNodeInGroup("player") as Node2D;
    bool seesPlayer = player is not null && HasLineOfSight(player.GlobalPosition);
    float signedDistance = player is null ? float.PositiveInfinity : player.GlobalPosition.X - GlobalPosition.X;
    RustShellFrame frame = _model.Step(signedDistance, seesPlayer, _stunRequested, _health.IsDead, delta);
    _stunRequested = false;
    Velocity = new Vector2(frame.MoveDirection * _settings.MoveSpeed, Velocity.Y + Gravity * (float)delta);
    _attackHitbox.Monitoring = frame.AttackActive;
    MoveAndSlide();
    _visual.SetFrame(frame);
}

public void ApplyThreadPull(Vector2 velocity)
{
    Velocity = velocity;
    _stunRequested = true;
}
```

Generate `RustShellSquire.tscn` and validate pack/instantiate node counts.

- [ ] **Step 6: Verify and commit the enemy**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
git add scripts/enemies tests tools/build scenes/enemies
git commit -m "feat: add rust shell test enemy"
```

---

### Task 6: Generate the playable gray-box room and short-distance reset

**Files:**
- Create: `scripts/world/PrototypeCheckpoint.cs`
- Create: `scripts/world/KillZone.cs`
- Create: `scripts/world/PrototypeRoomController.cs`
- Create: `scripts/world/PrototypeHud.cs`
- Create: `tests/unit/PrototypeCheckpointTests.cs`
- Create: `tools/build/BuildPrototypeRoom.cs`
- Create: `scenes/prototype/PrototypeRoom.tscn` (generated)
- Modify: `project.godot`
- Modify: `tests/RunPrototypeTests.cs`

**Interfaces:**
- Produces: `PrototypeCheckpoint.Activate(Vector2 safePosition)` and `CurrentPosition`.
- Produces: `PrototypeRoomController.RespawnPlayer()`.
- Consumes generated `Player.tscn` and `RustShellSquire.tscn`.

- [ ] **Step 1: Write the failing checkpoint tests**

Tests must assert that activation stores a finite safe position, rejects NaN/infinite coordinates, preserves the last valid position after a rejected activation and returns the player to that position with zero velocity.

```csharp
public static void Run()
{
    StoresTheMostRecentFiniteSafePosition();
    RejectsNonFiniteSafePositions();
    RespawnClearsVelocityAndUsesTheStoredPosition();
}
```

- [ ] **Step 2: Run the red build**

```powershell
& 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317\dotnet.exe' build 'E:\OurGame\Knight\Knight.csproj'
```

Expected: missing checkpoint types cause compile failure.

- [ ] **Step 3: Implement checkpoint and kill-zone behavior**

`PrototypeCheckpoint` starts at the player spawn. `KillZone` calls `RespawnPlayer` instead of reducing permanent resources. Respawn must disable player processing for one physics frame, set position, clear velocity, clear attack/thread transient state and then restore processing.

Use a model that can be tested without a scene:

```csharp
// scripts/world/PrototypeCheckpoint.cs
using Godot;

namespace Knight.World;

public sealed class PrototypeCheckpoint
{
    public Vector2 CurrentPosition { get; private set; }
    public PrototypeCheckpoint(Vector2 initialPosition) => Activate(initialPosition);

    public bool Activate(Vector2 safePosition)
    {
        if (!float.IsFinite(safePosition.X) || !float.IsFinite(safePosition.Y)) return false;
        CurrentPosition = safePosition;
        return true;
    }

    public void ApplyRespawn(CharacterBody2D body)
    {
        body.GlobalPosition = CurrentPosition;
        body.Velocity = Vector2.Zero;
    }
}
```

`PrototypeRoomController.RespawnPlayer()` calls `SetPhysicsProcess(false)`, `PrototypeCheckpoint.ApplyRespawn(player)`, `player.GetNode<PlayerCombat>("Combat").ResetTransientState()`, `player.GetNode<ThreadAbility>("ThreadAbility").ResetTransientState()`, awaits `ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame)`, then re-enables physics processing.

- [ ] **Step 4: Run the checkpoint tests**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
```

Expected: all checkpoint tests pass.

- [ ] **Step 5: Generate the room from leaf scenes**

`BuildPrototypeRoom.cs` must instantiate the already-generated player and enemy scenes instead of recreating their internal nodes. Build this fixed 1280×720 test route:

- Spawn platform from X `-560` to `-250`, Y `250`.
- Two 120-pixel stepping platforms at X `-120` and `80`, Y `170` and `80`.
- Thread anchor at `(260,-80)` over a 260-pixel gap.
- Landing platform from X `360` to `680`, Y `80`.
- One Rust-Shell Squire at `(520,20)`.
- One low ceiling segment that requires an up attack check.
- One suspended bell target that supports down-attack bounce.
- Kill zone below Y `520`.
- Checkpoint trigger immediately before the gap.
- Exit marker beyond the enemy; reaching it displays `PROTOTYPE COMPLETE` without changing scenes.

Use `StaticBody2D` plus rectangle collision shapes for gray-box platforms. Use `Polygon2D` or custom draw nodes for visible geometry. Assign collision layers explicitly: world `1`, player `2`, enemy `3`, player attack `4`, enemy attack `5`, thread targets `6`.

The builder must use one reusable platform helper and instantiate leaf scenes by resource path:

```csharp
private static StaticBody2D Platform(Node parent, string name, Vector2 center, Vector2 size)
{
    var body = new StaticBody2D { Name = name, Position = center, CollisionLayer = 1, CollisionMask = 0 };
    var collision = new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = size } };
    var visual = new Polygon2D
    {
        Name = "Visual",
        Color = new Color("29463c"),
        Polygon = new[] { -size / 2, new Vector2(size.X/2,-size.Y/2), size/2, new Vector2(-size.X/2,size.Y/2) }
    };
    body.AddChild(collision); body.AddChild(visual); parent.AddChild(body);
    return body;
}

var playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
var enemyScene = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn");
CharacterBody2D player = playerScene.Instantiate<CharacterBody2D>();
CharacterBody2D enemy = enemyScene.Instantiate<CharacterBody2D>();
```

After creating all nodes, call `SceneBuildUtil.PackAndSave(root, "res://scenes/prototype/PrototypeRoom.tscn")` and only then set `ProjectSettings.SetSetting("application/run/main_scene", "res://scenes/prototype/PrototypeRoom.tscn")` followed by `ProjectSettings.Save()`.

Set `run/main_scene="res://scenes/prototype/PrototypeRoom.tscn"` only after the generated scene passes pack validation.

- [ ] **Step 6: Run the room and inspect the actual game**

```powershell
$godotEditor = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe'
& $godotEditor --path 'E:\OurGame\Knight'
```

Manual acceptance:

1. Keyboard and controller can move, jump, dash, attack and use thread.
2. The gap is crossable with the anchor but not with a normal jump alone.
3. Down attack bounces from the bell target.
4. The enemy telegraph is readable without relying only on color.
5. Falling restarts at the nearby checkpoint in under one second.
6. The route can be completed without debug controls.

- [ ] **Step 7: Verify and commit the room**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1' -SkipSmoke
git add project.godot scripts/world tests tools/build scenes/prototype
git commit -m "feat: add playable prototype room"
```

---

### Task 7: Add deterministic smoke coverage, proof capture and status documentation

**Files:**
- Create: `tests/smoke/PrototypeSmoke.cs`
- Create: `tests/presentation/PrototypePresentation.cs`
- Create: `tools/capture-prototype.ps1`
- Modify: `tools/verify.ps1`
- Modify: `.gitignore`
- Modify: `README.md`

**Interfaces:**
- Produces smoke output: `PROTOTYPE_SMOKE_OK` and exit code `0`.
- Produces proof video: `screenshots/prototype/prototype.avi` or numbered PNG frames when AVI capture is unavailable.
- Consumes: stable node paths from Tasks 2, 5 and 6.

- [ ] **Step 1: Write an integration smoke script that fails on incomplete wiring**

`PrototypeSmoke : SceneTree` must load `PrototypeRoom.tscn`, add it to the root, wait two physics frames, and assert:

```csharp
TestAssert.True(player.IsInGroup("player"), "player group");
TestAssert.True(room.HasNode("Geometry/ThreadAnchor"), "thread anchor exists");
TestAssert.True(room.HasNode("Actors/RustShellSquire"), "enemy exists");
TestAssert.True(InputMap.HasAction(InputActions.Thread), "thread input exists");
```

Then it must use `Input.ActionPress`/`ActionRelease` to:

1. Hold right for 30 physics frames and assert player X increases by at least 40 pixels.
2. Press jump and assert player Y decreases within 10 frames.
3. Reset to a fixture position next to the enemy, press attack and assert enemy health decreases.
4. Reset before the gap, face the anchor, press thread and assert player speed points toward the anchor.
5. Place the player in the kill zone and assert position returns to the checkpoint.

On success print `PROTOTYPE_SMOKE_OK`; on any exception print it and `Quit(1)`.

- [ ] **Step 2: Run smoke before adapting any incorrect wiring**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1'
```

Expected: if any generated node path, collision mask or action is wrong, the smoke test exits nonzero and names that contract. Do not weaken an assertion to make it pass; fix the production wiring.

- [ ] **Step 3: Fix only the production contracts exposed by smoke**

Allowed fixes are limited to node paths, group assignment, collision layers/masks, deterministic fixture positions and input consumption order. Preserve the gameplay rules already covered by unit tests.

Use these diagnostics in order and fix the first failing contract only:

```powershell
rg -n 'Name = "(Input|Motor|Visual|Health|Hurtbox|Combat|AttackHitbox|ThreadAbility|ThreadLine)"' tools/build/BuildPlayer.cs
rg -n 'Name = "(Health|Hurtbox|AttackHitbox|ThreadTarget|Visual)"' tools/build/BuildRustShellSquire.cs
rg -n 'AddToGroup\("(player|thread_targets)"\)' scripts
rg -n 'CollisionLayer|CollisionMask' tools/build scripts
& 'E:\OurGame\Knight\tools\verify.ps1'
```

If the named node is absent, correct its builder `Name`; if a group is absent, add it in `_Ready`; if contact is absent, correct only the documented collision bit/mask pair; if the input is ignored, ensure `PlayerInputReader` snapshots Action state before `PlayerMotor`, `PlayerCombat` and `ThreadAbility` consume it. Re-run the full verifier after each single correction.

- [ ] **Step 4: Run the complete green gate**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1'
```

Expected: build succeeds; import has zero errors; unit runner prints `PROTOTYPE_TESTS_OK`; room runner prints `PROTOTYPE_SMOKE_OK`.

- [ ] **Step 5: Add deterministic presentation capture**

`PrototypePresentation.cs` drives a 12-second sequence at 60 FPS: run and variable jump, air dash, thread across the gap, horizontal attack, up attack, down-bounce, one intentional fall/reset, defeat or pass the enemy, reach exit. It must position the camera before the first rendered frame and quit after frame 720.

`capture-prototype.ps1` runs:

```powershell
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
New-Item -ItemType Directory -Path 'E:\OurGame\Knight\screenshots\prototype' -Force | Out-Null
& $godot --path 'E:\OurGame\Knight' --fixed-fps 60 --write-movie 'E:\OurGame\Knight\screenshots\prototype\prototype.avi' --script 'res://tests/presentation/PrototypePresentation.cs'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

If this Godot build cannot write AVI, change only the output to `screenshots/prototype/frame.png` and retain the numbered frames. Do not declare capture complete without opening the output and checking that movement progresses across the full frame.

- [ ] **Step 6: Update the durable project status**

`README.md` must include:

- What is playable now.
- Exact keyboard and controller controls.
- Exact build, test, run and capture commands.
- Known exclusions: ember, echo, full map, Boss, formal art, audio, localization, settings, save, Steamworks.
- Asset table with columns `Asset`, `Current source`, `License/status`, `Replacement milestone`; all prototype visuals are marked procedural and owned by the project.
- Link to the design spec and this plan.

Use this concrete section structure:

```markdown
## Current milestone

Technical prototype: playable gray-box route with responsive movement, four-direction attacks, mechanical thread, one Rust-Shell Squire and fast checkpoint reset.

## Run and verify

`powershell -ExecutionPolicy Bypass -File tools/verify.ps1`

## Known exclusions

Ember, echo, circular map, remaining enemies, Bellkeeper Boss, formal art/audio, localization, settings, save data and Steamworks are not part of this milestone.

## Asset status

| Asset | Current source | License/status | Replacement milestone |
| --- | --- | --- | --- |
| Player gray-box | Project-owned procedural drawing | Owned; no paid generation | Formal art and audio |
| Rust-Shell gray-box | Project-owned procedural drawing | Owned; no paid generation | Formal art and audio |
| Prototype room | Godot primitives generated in C# | Owned; no paid generation | Formal art and audio |
```

- [ ] **Step 7: Final verification, commit and push**

```powershell
& 'E:\OurGame\Knight\tools\verify.ps1'
git diff --check
git status --short
git add tests tools .gitignore README.md
git commit -m "test: verify technical prototype end to end"
git push origin main
```

Expected: all gates pass, proof capture exists and is visually reviewed, `main` is pushed, and the only ignored outputs are `.godot`, logs, local capture files and development runtime data.

---

## Plan Self-Review

- **Scope coverage:** This plan covers only milestone 1 from the approved design: foundation migration, responsive movement, precise basic combat, permanent mechanical thread, one Rust-Shell Squire, a gray-box test route, quick reset and proof capture.
- **Intentionally deferred:** Ember, echo, circular map, remaining two enemies, Bellkeeper Boss, final art/audio, bilingual narrative/UI, save/settings/accessibility and Steamworks each belong to later plans after the prototype hand-feel review.
- **Boundary check:** No task introduces identity switching, skill trees, additional weapons, economy, online features or paid asset generation.
- **Type consistency:** Later tasks consume the exact `PlayerInputFrame`, `PlayerMotor.ExternalImpulse`, `DamageRequest`, `HealthComponent.ApplyDamage`, `ThreadTarget2D.ApplyPull` and generated node paths defined earlier.
- **Completion evidence:** A clean build is insufficient; completion requires unit tests, actual Godot import, runtime smoke, manual keyboard/controller traversal and an opened proof capture.
