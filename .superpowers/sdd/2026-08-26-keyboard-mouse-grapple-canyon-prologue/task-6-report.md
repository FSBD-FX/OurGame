# Task 6 实现报告：机械爪运行时、可抓表面与旧机关兼容

日期：2026-08-26
工作树：`E:\OurGame\Knight\.worktrees\player-art-pipeline`
分支：`codex/player-art-pipeline`

## 提交与范围

- 基线：`d1226ded38e43f2390d1d35df3e8f91648cea73c`
- 最终实现提交：`0922624a0cb1d0d23cee152101f7cc134a15733e`
- 提交标题：`feat: replace mechanical thread with grapple runtime`
- 实现提交统计：55 files changed，2750 insertions，984 deletions。
- 本报告在实现提交之后单独落盘，以便能够记录不可自引用的实现提交哈希；报告提交只包含本文件。
- 未修改或暂存 README、project.godot、Task 3/Task 4 美术与音频、导入 sidecar、其他计划进度文件或无关场景。
- 未使用付费或正式新增美术；只复用现有线条/梭子表现与已有生产资源。

## 结果摘要

生成 Player 已移除 `ThreadAbility`、`ThreadTuning` 和玩法冷却，稳定挂载 `MechanicalClaw`，表现路径固定为 `VisualRoot/MechanicalClaw` 与 `VisualRoot/MechanicalClaw/Line`。机械爪运行时直接驱动 Task 5 纯模型；每次 `Step` 前刷新复制 frame 的玩家位置与速度。目标只来自真实 2D physics shape/raycast 与显式 `GrappleSurface2D`，支持 36 px assist、520 px 含边界射程、遮挡、材质失败和场景卸载清理。

移动修饰由 `PlayerMotor` 独立组合 ability、shield 与 grapple 三个 scale；护盾期间输入 `ReelAxis=0`，连接不解除。Q/攻击模式请求安全收回，E 选择爪模式，左键发射。未引入二段跳，空中 dash 消耗回归保持通过。

轻型敌人拉取、重型敌人破防、Bell Chain 激活继续经 `IThreadInteractionReceiver`，但入口统一为 `LegacyThreadInteractionAdapter`；三类旧交互在一次应用后立即释放，不形成持久绳索。

## RED → GREEN 证据

### 基线 PRE

在写 Task 6 RED 测试前运行：

```powershell
dotnet build
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrototypeTests.cs
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=pre
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=post
```

原始结果摘要：

- `dotnet build`：exit 0，0 warnings，0 errors。
- Prototype：`PROTOTYPE_TESTS_OK`。
- Prologue pre：`PROLOGUE_TEST_PHASE_OK phase=pre`。
- Prologue post：`PROLOGUE_TEST_PHASE_OK phase=post`，`PROLOGUE_TESTS_OK`。

### RED

先新增 `GrappleSurfaceTests`、`MechanicalClawRuntimeTests` 与 integration `ThreadInteractionTests`，并挂入 runners，尚未写生产类型时运行：

```powershell
dotnet build
```

原始结果：exit 1，11 个 `CS0246`，全部为预期缺失的 Task 6 surface/resolver/controller/adapter 类型。随后尝试 focused runners 时，Godot 使用了上一次成功 build 的旧 DLL 并打印旧绿；该输出明确不计作新测试 RED，RED 证据以编译失败为准。

### GREEN（最终工作树）

builder 重建后运行：

```powershell
dotnet build
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrototypeTests.cs
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=pre
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=post
```

原始结果摘要：

- `dotnet build`：exit 0，0 warnings，0 errors。
- Prototype：`PROTOTYPE_TESTS_OK`。
- Prologue pre：`PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=pre`，`PROLOGUE_TEST_PHASE_OK phase=pre`。
- Prologue post：`PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=post`，`PROLOGUE_TEST_PHASE_OK phase=post`，`PROLOGUE_TESTS_OK`。
- pre 的真实 traversal route 覆盖全部房间；Broken Tooth 1 个辅助段与 Upper Pendulum 3 个辅助段均为 `castCount=1`、`applyCount=1`、`acceptedDash=0`，无二段跳或为旧坐标过拟合的隐藏 dash。
- 警告仅来自已有的故意负路径（错误 PresentationProfile root、缺失 guidance target、拒绝的 flow transition）。

### 精确 index 快照复验

为避免当前共享脏工作树掩盖问题，使用 `git write-tree` + 临时 detached worktree 对“将要提交的 index 内容”隔离复验；导入后删除临时 worktree。原始结果：

- tree：`fdd022dfb40489440980f23fe534d12fec725604`
- 临时验证 commit：`f485eb875a64d3d70d9afba74982635b10009738`（仅验证用，非交付提交）
- `dotnet build`：exit 0，0 warnings，0 errors。
- `RunPrototypeTests`：`PROTOTYPE_TESTS_OK`。
- `RunPrologueTests --phase=pre`：`PROLOGUE_TEST_PHASE_OK phase=pre`。
- `godot --headless --import --quit`：exit 0，导入完成。
- `RunPrologueTests --phase=post`：exit 0，`PROLOGUE_TEST_PHASE_OK phase=post`，`PROLOGUE_TESTS_OK`。
- `MechanicalClawProofCapture`：`TASK6_MECHANICAL_CLAW_PROOF_OK casts=3 latches=3 travel_frames=56 failures=0 final=(770.8482, 63.933002)`。
- `git diff --cached --check`：无输出，exit 0。

## Builder 重建

按叶到根顺序运行以下 11 个 builder，均 exit 0 并打印各自 `packed and validated`；总结果为 `TASK6_BUILDERS_OK count=11`：

1. `BuildPlayer.cs`
2. `BuildGraveLampMoth.cs`
3. `BuildBellBackLaborer.cs`
4. `BuildFallenBellkeeper.cs`
5. `BuildBellChainMechanism.cs`
6. `BuildAwakeningCourtyard.cs`
7. `BuildBrokenToothCorridor.cs`
8. `BuildUpperPendulum.cs`
9. `BuildLowerBuriedMachinery.cs`
10. `BuildBellChainRoom.cs`
11. `BuildFallenBellkeeperArena.cs`

调用形式：

```powershell
& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/<builder>.cs
```

## 真实运行与人工观察

- display-backed 录制：`screenshots/task6-mechanical-claw-proof.avi`
- 参数：1280×720，60 fps，557 frames，约 9.17–9.28 秒，约 19.5 MB。
- 人工复核 contact sheet：`screenshots/task6-proof-review-20260826/contact-sheet.png`。
- 观察：Upper Pendulum 中连续完成 3 次互相独立的机械爪发射；每次均可见三股线、真实多帧 shuttle travel、锁定与玩家位移。最后站稳在目标平台，线与梭子均已清空，没有跨房间或结束帧残留。
- 录像与 contact sheet 位于 gitignored `screenshots/`，作为本地证据保留，不进入提交。

## Brief 文件逐项核对

### 运行时与直接迁移

| 文件 | Task 6 语义 |
|---|---|
| `scripts/abilities/GrappleSurface2D.cs` | 新增显式材质、36 px assist、world anchor、可用性与 authored-data 校验。 |
| `scripts/abilities/MechanicalClawTargetResolver.cs` | 新增真实 shape query、raycast LOS、caster collision 排除、最近 cursor-distance 选择与锁定目标快照。 |
| `scripts/abilities/MechanicalClawController.cs` | 新增物理输入、Task 5 model 驱动、每帧 caster frame 刷新、单爪生命周期、shield/Q/unload/reset/motor/legacy adapter 集成。未构造 `ThreadAbilityModel`，未读取 cooldown。 |
| `scripts/abilities/LegacyThreadInteractionAdapter.cs` | 新增旧 receiver 的一次性兼容入口；应用后由 controller 立即释放。 |
| `scripts/abilities/ThreadTarget2D.cs` | 改为继承 `GrappleSurface2D`，保留旧 group/receiver contract。 |
| `scripts/abilities/ThreadTargetKind.cs` | 新增类型化 legacy-interaction 分类。 |
| `scripts/player/PlayerMotor.cs` | 新增独立 grapple correction/scale；与 Task 3 `ShieldMovementScale` 乘法组合，互不覆盖。 |
| `scripts/vfx/ThreadLineVisual.cs` | 订阅机械爪 frame，只负责表现，不做碰撞查询；退出与 Idle 清空。 |
| `scripts/vfx/ThreadShuttleVisual.cs` | 订阅机械爪 frame，映射现有内部表现状态并保证 clear。 |
| `scripts/presentation/PlayerPresentationCoordinator.cs` | 由生成玩家 `ThreadAbility` 订阅迁移到类型化 `MechanicalClawController`；保留旧纯模型表现 helper 供基线测试。 |
| `scripts/prologue/traversal/TraversalSegment2D.cs` | assist target 改为 `GrappleSurface2D`/DedicatedAnchor，范围改为 520 px。 |
| `scripts/player/PlayerActionModeController.cs` | 按裁决将活动契约从临时 `ThreadAbility` 改为 `MechanicalClawController`，保持 `AttackModeRequested` 的 Q 收回语义。 |
| `scripts/prologue/guidance/PrologueGuidanceController.cs` | 按裁决做最小类型化订阅迁移；Upper AnchorLow 监听 Latched，不改 guidance 策略。 |
| `scripts/prologue/PrologueLevelController.cs` | 按裁决迁移 reset/process/unload/physics 路径；房间卸载前通知并清理机械爪；HUD cooldown 固定 ready，不建立新冷却。 |

### Builder

| 文件 | Task 6 语义 |
|---|---|
| `tools/build/BuildPlayer.cs` | 生成 `MechanicalClaw` 与 `VisualRoot/MechanicalClaw/Line`；移除生成玩家的 `ThreadAbility`、`ThreadTuning`、root `ThreadLine`。 |
| `tools/build/prologue/BuildAwakeningCourtyard.cs` | 新增 GripStone physics surface 与验证。Task 3 的 Q 攻击提示未纳入本提交。 |
| `tools/build/prologue/BuildBrokenToothCorridor.cs` | 现有 anchor 显式 authored 为 DedicatedAnchor/36 px；教程迁移到 E + LMB。 |
| `tools/build/prologue/BuildUpperPendulum.cs` | 三个现有 anchor 显式 authored 为 DedicatedAnchor/36 px，并逐节点验证。 |
| `tools/build/prologue/BuildLowerBuriedMachinery.cs` | 新增 Ice physics surface；旧轻敌教程迁移到 E + LMB。 |
| `tools/build/prologue/BuildBellChainRoom.cs` | 新增 TempleMetal physics surface。 |
| `tools/build/prologue/BuildBellChainMechanism.cs` | Bell Chain target 加 TempleMetal 与 legacy adapter。 |
| `tools/build/prologue/BuildGraveLampMoth.cs` | 轻敌 target 加真实 collision area、TempleMetal 与 legacy adapter。 |
| `tools/build/prologue/BuildBellBackLaborer.cs` | 重敌 target 加 TempleMetal 与 legacy adapter。 |
| `tools/build/prologue/BuildFallenBellkeeperArena.cs` | 新增 Forbidden physics surface。 |
| `tools/build/prologue/BuildFallenBellkeeper.cs` | boss chain target 加 TempleMetal 与 legacy adapter。 |

### 生成场景

| 文件 | 暂存的 Task 6 语义 / 噪音处理 |
|---|---|
| `scenes/player/Player.tscn` | 暂存稳定 MechanicalClaw/runtime/visual 路径及旧节点移除；未暂存 generator ID churn。 |
| `scenes/enemies/GraveLampMoth.tscn` | 暂存 target physics/材质/adapter；排除 Task 4 formal `profile_bounds` 与 telegraph 差异。 |
| `scenes/enemies/BellBackLaborer.tscn` | 暂存材质/adapter；排除 Task 4 formal `profile_bounds` 与 telegraph 差异。 |
| `scenes/mechanisms/BellChainMechanism.tscn` | 暂存材质/adapter；这是 brief builder 的直接生成输出，虽未在 brief 的 generated-output 行单列。 |
| `scenes/prologue/rooms/AwakeningCourtyard.tscn` | 暂存 GripStone surface；排除 Task 3 Q 攻击教程和 ID churn。 |
| `scenes/prologue/rooms/BrokenToothCorridor.tscn` | 暂存 E + LMB claw 教程；anchor 通过 `ThreadTarget2D` 继承获得 surface 语义。 |
| `scenes/prologue/rooms/UpperPendulum.tscn` | builder 已重建并验证；三个现有 anchor 的材质/assist 等于代码默认值，Godot 不序列化默认属性，因此文本语义无变化。只产生 unique/resource ID churn，按裁决未暂存。 |
| `scenes/prologue/rooms/LowerBuriedMachinery.tscn` | 暂存 Ice surface 与 E + LMB 轻敌教程；排除 ID churn。 |
| `scenes/prologue/rooms/BellChainMechanism.tscn` | 暂存 TempleMetal surface；排除 ID churn。 |
| `scenes/prologue/rooms/FallenBellkeeperArena.tscn` | 暂存 Forbidden surface；排除 ID churn。 |
| `scenes/boss/FallenBellkeeper.tscn` | 暂存 chain target 材质/adapter；排除 ID churn。 |

### Brief 测试与 runners

| 文件 | Task 6 语义 |
|---|---|
| `tests/unit/GrappleSurfaceTests.cs` | 覆盖所有材质、有限 anchor、36 px 上限与 legacy kind 类型。 |
| `tests/integration/MechanicalClawRuntimeTests.cs` | 覆盖 519/520/521、36/36.01、真实遮挡、冰/禁区、multi-frame travel、latch/reel/movement/shield/release/unload/残留清理/单爪优先级。 |
| `tests/integration/ThreadInteractionTests.cs` | 覆盖真实轻敌、重敌、Bell Chain adapter 一次应用与无持久绳索。 |
| `tests/integration/PlayerTraversalInputTests.cs` | 将生成玩家 fixture 从 K/ThreadAbility 迁移到 E + primary MechanicalClaw，保持空中 dash/无二段跳回归。 |
| `tests/RunPrototypeTests.cs` | 注册 surface unit tests。 |
| `tests/RunPrologueTests.cs` | 注册 runtime claw 与 integration legacy tests，并消除 unit/integration 同名歧义。 |

### 必要直接测试/runner 迁移

| 文件 | 原因 |
|---|---|
| `tests/integration/PlayerActionModeRuntimeTests.cs` | 生成玩家不再有 ThreadAbility，活动 handoff 改为机械爪 frame。 |
| `tests/integration/PrototypeRoomRuntimeTests.cs` | 稳定节点与视觉路径迁移。 |
| `tests/integration/PrologueTraversalRouteTests.cs` | 用真实 E + primary claw 驾驶辅助段；保留通用 segment contract，不按单一旧坐标硬编码。 |
| `tests/integration/GraveLampMothRuntimeTests.cs` | 轻敌生成夹具改为 adapter/surface。 |
| `tests/integration/BellChainRuntimeTests.cs` | Bell Chain 生成夹具改为 adapter/surface。 |
| `tests/integration/PrologueGuidanceRuntimeTests.cs` | guidance 观察类型迁移。 |
| `tests/integration/PrologueCompletionTests.cs` | 生成玩家 runtime 类型迁移。 |
| `tests/integration/PrologueLevelRuntimeTests.cs` | reset/room transition 类型迁移。 |
| `tests/integration/ProloguePlaytestRecorderTests.cs` | 稳定玩家节点迁移。 |
| `tests/integration/PrologueFormalHudTests.cs` | 仅做新节点路径/类型必要迁移，不提前做 Task 10 HUD 重构。 |
| `tests/integration/PlayerFormalVisualTests.cs` | 视觉夹具迁移到 MechanicalClaw frames/paths。 |
| `tests/integration/ThreadCastRuntimeTests.cs` | 真实生成 PrologueLevel 的 transition 夹具改为 claw；隔离的旧 ThreadAbility 基线测试保留。 |
| `tests/integration/PrologueVfxRuntimeTests.cs` | 正式线条路径迁移到稳定 visual root。 |
| `tests/presentation/MechanicalClawProofCapture.cs` | 新增 display-backed 三抓 traversal 证明 runner。 |

## 关键契约核查

- `git show :scenes/player/Player.tscn` 仅命中 `MechanicalClawController`、`MechanicalClaw`、`VisualRoot/MechanicalClaw/Line`；未命中 `ThreadAbility`、`ThreadTuning` 或 cooldown。
- 新 controller/resolver 未命中 `ThreadAbilityModel`、`ThreadTuning` 或 `Cooldown`。
- `PlayerMotor.EffectiveMovementScale = ability × shield × grapple`；reset 同时恢复 grapple correction/scale，不覆盖 shield。
- controller 在每一帧 `_model.Step` 前用当前 `GlobalPosition`/`Velocity` 更新 previous frame。
- 右键护盾只把 `ReelAxis` 归零；连接、target 与 rope length 保留。
- Q 的 `AttackModeRequested` 设置安全收回请求；E + primary 才建立新抓取。
- resolver 使用 `IntersectShape` 与 `IntersectRay`，显式 collision mask，排除 caster subtree RID，不按名称/贴图颜色推断。
- 失败优先级测试覆盖：AlreadyDeployed、TargetLost、Ice/Forbidden、OutOfRange、Obstructed、NoTarget。
- room unload、damage、death、checkpoint reset 与 `_ExitTree` 均清理 target、motor constraint 和表现。
- 生成玩家无 K/旧 ThreadAbility 路径；旧源码只供基线对照保留。

## 剩余风险与交接说明

1. 当前共享工作树仍有其他任务的未提交改动和 Godot 导入/生成噪音。实现提交采用“基线场景 + Task 6 语义”的精确 index blob；因此提交后这些生成场景仍会显示 unstaged diff。这是有意保留他人改动，不是 Task 6 遗漏。
2. `UpperPendulum.tscn` 的重建仅改变序列化 ID；Task 6 语义来自已提交的 builder 与 `ThreadTarget2D : GrappleSurface2D`。下次统一重建时可由整合者选择接受新的 ID。
3. 已提交场景与当前完整 builder 输出语义一致，但不追求 byte-for-byte ID 一致；后续统一 generator pass 可能再次产生 ID churn。
4. 旧 `ThreadAbility` 源码和隔离基线测试仍存在，符合 brief；生成主 Player 不挂载。内部 `ThreadLineVisual` / `ThreadShuttleVisual` 命名留待 Task 10。
5. HUD 仍沿用旧 cooldown 展示接口并传入 ready=1；玩法没有冷却。正式 HUD 命名/视觉重构留待 Task 10。
6. 录像使用确定性 scripted driving 而非人工键鼠输入，但运行在真实 display-backed Godot/physics/scene tree，且 exact-index headless proof 与完整 pre/post 套件均通过。

---

## Fix Round 1（独立审查 Changes Required）

日期：2026-08-26

- 审查起点：`ae1fb28dc5e2dad0fcf7aab60ff0a89172d61919`
- Fix Round 1 实现提交：`ea325e7df0cca2eb10d1719563a7eba3f41ec62c`
- 提交标题：`fix: stabilize mechanical claw runtime`
- 精确实现范围：20 files changed，726 insertions，93 deletions。
- 报告在实现提交后单独提交，因而可以记录实现提交的不可自引用哈希。

### 审查项逐条修复映射

1. **累计 correction / 松脱二次叠加**：`MechanicalClawModel` 现在把径向 correction 作为模型拥有的绝对分量递进；`PlayerMotor` 把它与基础运动、碰撞调整分离，每帧不再把上帧 correction 重复写回基础速度。松脱先从真实 `CharacterBody2D.Velocity` 继承 Task 5 切向速度，再清 constraint，避免隐藏 correction 二次加速。真实 Motor 自动 physics 120 帧测试覆盖长期速度上界与 release 前后速度上界；测试曾真实复现 `maximum=1710.72`。
2. **卸载同步、一次性 TargetLost**：`NotifyTargetWillUnload` 同步发布且只发布一次 `Failed/TargetLost`，事件序列明确为 `Failed(TargetLost) -> Idle`，在返回前清 target、motor constraint、line/shuttle；Level reset 只有在未发布卸载失败时才执行普通 reset。测试记录完整 frame/event 序列与计数，不只断言最终 Idle。
3. **Prototype Fall Reset**：`PrototypeRoomController` 已把无伤害坠落恢复迁移到 `MechanicalClaw`；恢复窗口暂停 Input/Action/Claw/Motor/Combat，传送后旧 target、correction、line、shuttle 全为空。RED 为 `expected=Idle, actual=Latched`，真实 KillZone 回归现已通过。
4. **RustShellSquire**：builder 与提交场景均加入真实 layer-32 `ThreadTarget/TargetCollision`、18 px shape、`TempleMetal` 与 `LegacyThreadInteractionAdapter`。真实场景测试覆盖轻敌拉取成功、遮挡拒绝、一次 adapter apply、无持久绳。builder 重建 node count 24/24 并验证；提交场景只暂存 Task 6 语义，排除 formal metadata 与 ID churn。
5. **dead/unavailable/rejected legacy**：`ThreadTarget2D.TryApplyLegacyInteraction` 变为事务式入口；receiver 不可用或 adapter 拒绝时不先发布 `Latched`，直接发布类型化 `Failed/TargetLost -> Idle`，success count 不增加、receiver 无副作用、无持久绳。旧灯、重型破防与 Bell Chain 成功路径仍为一次性交互后 `Released`。
6. **自动物理调度**：`BuildPlayer` 与提交 `Player.tscn` 明确固定 `Input=-300 -> ActionModes=-200 -> MechanicalClaw=-100 -> Motor=0`；节点顺序也把 MechanicalClaw 放在 Motor 前。自动调度测试使用真实 InputMap Q 和 physics frames，不手调 `_PhysicsProcess`；同时覆盖 ability `.5` × shield `.65` × grapple `.65` = `.21125` 的三重独立倍率，以及 caster 自身 World/ThreadTarget colliders 都被 query 排除。
7. **Minor NoTarget 表现**：NoTarget 清空世界绳，只允许本地失败标记；不再从玩家画到世界原点。对应 RED 为 `expected=0, actual=1`，现在 line point count 保持 0。

### 测试驾驶迁移说明

旧 traversal driver 的固定 `PureReelFrames` 与预测器把累计 correction 当成可持续推进力，实际固化了审查指出的错误。Fix Round 1 将其迁移为输入语义驱动：水平输入朝 authored landing、竖直输入负责 reel、Jump 提供既有单跳辅助，并从真实 `player.Velocity`、空中控制目标和 gravity 预测松脱落点；只有预测落点进入 authored landing zone 才释放 primary。它不使用单一旧地图的固定抓取帧数或隐藏 dash。最终 Broken Tooth 及 Upper Pendulum 三段均为 `castCount=1`、`applyCount=1`、`acceptedDash=0`。

### RED 证据（新测试先行）

所有 RED 前均先执行新鲜 `dotnet build`，build 为 exit 0、0 warnings、0 errors；随后运行：

```powershell
& $godot --headless --path . --script res://tests/RunPrototypeTests.cs
& $godot --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=pre
```

测试 runner 每次在首个断言处停止；修复一个缺口后重跑，逐级暴露下一项。以下均为 exit 1 的原始首失败，且来自当次新 DLL，不依赖旧日志/旧二进制：

| 时间（Asia/Shanghai，日志落盘） | 日志 | 终端首失败 marker |
|---|---|---|
| 07:45:17 | `task6-fix1-red-prototype.log` | `prototype fall recovery ... expected=Idle, actual=Latched` |
| 07:46:12 | `task6-fix1-red-prologue-pre.log` | `120 automatic swing frames keep velocity bounded; maximum=1710.72` |
| 07:47:21 | `task6-fix1-green1-red2-pre.log` | `generated Input ... expected=-300, actual=0` |
| 07:48:03 | `task6-fix1-green2-red3-pre.log` | `real InputMap Q ... expected=Attack, actual=Grapple` |
| 07:49:16 | `task6-fix1-green2-red3b-pre.log` | `ability, shield and grapple ... expected=0.21124998, actual=0.325` |
| 07:49:50 | `task6-fix1-green3-red4-pre.log` | `NotifyTargetWillUnload ... expected=1, actual=0` |
| 07:50:24 | `task6-fix1-green4-red5-pre.log` | `NoTarget ... expected=0, actual=1` |
| 07:50:51 | `task6-fix1-green5-red6-pre.log` | RustShell `LegacyThreadInteractionAdapter` / `TargetCollision` node missing，随后 NullReference |

dead/adapter-rejected、caster 自身 collider 排除和事件序列断言与上述 RED 测试批次同时加入；runner 的 fail-fast 特性使它们没有各自成为新的首失败，但它们在最终 fresh 与 exact-index 两阶段均实际执行并通过。

### GREEN：最终共享工作树

先运行 12 个 builders：`BuildPlayer`、`BuildRustShellSquire`、GraveLampMoth、BellBackLaborer、FallenBellkeeper、BellChainMechanism、AwakeningCourtyard、BrokenToothCorridor、UpperPendulum、LowerBuriedMachinery、BellChainRoom、FallenBellkeeperArena。调用形式：

```powershell
& $godot --headless --path . --script res://tools/build/<builder>.cs
```

原始结果：exit 0；`TASK6_FIX1_BUILDERS_OK count=12`。其中 Player `pre-pack node count=34; post-instantiate node count=34`，RustShell `24/24`，其余均打印 `packed and validated`。日志：`task6-fix1-builders.log`。

最终 fresh 命令：

```powershell
dotnet build
& $godot --headless --path . --script res://tests/RunPrototypeTests.cs
& $godot --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=pre
& $godot --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=post
& $godot --headless --path . --script res://tests/presentation/MechanicalClawProofCapture.cs
```

| 命令 | 开始 → 结束 | exit / terminal marker |
|---|---|---|
| `dotnet build` | 08:08:43.518 → 08:08:44.968 | exit 0；0 warnings，0 errors |
| Prototype | 08:08:44.972 → 08:08:45.683 | exit 0；`PROTOTYPE_TESTS_OK` |
| Prologue pre | 08:08:53.760 → 08:09:30.806 | exit 0；`PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=pre`，`PROLOGUE_TEST_PHASE_OK phase=pre` |
| Prologue post | 08:09:40.698 → 08:10:28.628 | exit 0；`PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=post`，`PROLOGUE_TEST_PHASE_OK phase=post`，`PROLOGUE_TESTS_OK` |
| focused proof | 08:10:41.992 → 08:10:49.789 | exit 0；`TASK6_MECHANICAL_CLAW_PROOF_OK casts=3 latches=3 travel_frames=56 failures=0 final=(770.35925, 63.933002)` |

对应原始日志为 `task6-fix1-final-build.log`、`task6-fix1-final-prototype.log`、`task6-fix1-final-pre.log`、`task6-fix1-final-post.log`、`task6-fix1-final-proof.log`。警告仍仅是既有故意负路径：错误 presentation root、缺失 guidance target、拒绝的 flow transition。

### Display-backed 真实运行与人工复核

- 录像：`screenshots/task6-fix1-mechanical-claw-proof.avi`
- 运行：08:11:03.603 → 08:11:12.814，exit 0；OpenGL 3.3，NVIDIA GeForce RTX 3060 Laptop GPU。
- 参数：1280×720，60 FPS，439 frames，约 7.32 秒，15,213,834 bytes。
- 抽帧目录：`screenshots/task6-fix1-review/`，共 439 PNG；人工查看 frame 50/100/150/200/250/300/350/438。
- 人工观察：frame 150 可见玩家在空中与 anchor 相连的三股青色爪线；后续抽帧显示玩家真实跨越平台；最终 frame 438 已稳定落在末端平台，世界绳、shuttle 和失败表现均无残留。运行内 marker 同样为 3 casts / 3 latches / 56 travel frames / 0 failures，且无 dash。
- 原始日志：`task6-fix1-display-proof.log`。

### 精确 index 快照复验

生成场景重建产生大量既有 resource/unique ID churn。暂存时以审查起点场景为底，仅加入 Task 6 语义：Player 的 priority/node scheduling；RustShell 的 TempleMetal、layer-32 shape 与 adapter。未暂存 formal `profile_bounds`、其他生成 metadata 或 ID churn；这些共享脏改动继续留在工作树。

通过 `git write-tree` + `git commit-tree` 创建只包含精确 index 的临时 detached worktree；在无旧 `.godot`/DLL 的临时目录中重新编译、导入、运行全套，随后已移除临时 worktree：

- tree：`adff925fc78409fc997a9dc55751b6ae4492c6c2`
- 临时验证 commit：`7ffe11751e38e685347f4749718d4b9a3fb42ff9`（仅验证，不是交付）
- `dotnet build`：08:17:33.522 → 08:17:43.731，exit 0，0 warnings / 0 errors。
- `godot --headless --import --quit`：08:17:43.736 → 08:17:56.101，exit 0，`VERIFICATION_EDITOR_GC_DRAIN_OK`。
- Prototype：08:17:56.101 → 08:17:56.957，exit 0，`PROTOTYPE_TESTS_OK`。
- pre：08:17:56.958 → 08:18:34.158，exit 0，`PROLOGUE_TEST_PHASE_OK phase=pre`。
- post：08:18:34.158 → 08:19:22.500，exit 0，`PROLOGUE_TEST_PHASE_OK phase=post`、`PROLOGUE_TESTS_OK`。
- proof：08:19:22.500 → 08:19:30.406，exit 0，`TASK6_MECHANICAL_CLAW_PROOF_OK casts=3 latches=3 travel_frames=56 failures=0 final=(770.35925, 63.933002)`。
- 原始日志：`task6-fix1-index-build/import/prototype/pre/post/proof.log`；`git diff --cached --check` 无输出、exit 0。

### Fix Round 1 变更文件与范围说明

- 生产：`LegacyThreadInteractionAdapter.cs`、`MechanicalClawController.cs`、`MechanicalClawModel.cs`、`ThreadTarget2D.cs`、`PlayerMotor.cs`、`PrologueLevelController.cs`、`ThreadLineVisual.cs`、`PrototypeRoomController.cs`。
- builders/生成场景：`BuildPlayer.cs`、`BuildRustShellSquire.cs`、`Player.tscn`、`RustShellSquire.tscn`。
- 直接测试：`BellChainRuntimeTests.cs`、`MechanicalClawRuntimeTests.cs`、`PrologueCompletionTests.cs`、`PrologueGuidanceRuntimeTests.cs`、`PrologueTraversalRouteTests.cs`、`PrototypeRoomRuntimeTests.cs`、`ThreadCastRuntimeTests.cs`、`ThreadInteractionTests.cs`。
- 原 brief 中其余 builder/脚本/场景未再修改：Fix Round 1 没有改变其 Task 6 authored surface/adapter 语义，12-builder 全重建与 pre/post 已重新验证；为避免吸收其他任务生成噪音，不重复暂存无语义文本变化。

### 剩余风险

1. `MechanicalClawModel` 的径向 correction 是离散 physics-step contract；当前覆盖 60 Hz 自动调度和 120 帧长期摆荡。若未来改 physics tick，应保留同类上界回归。
2. traversal driver 现在依赖 authored landing zone，而不是旧固定 reel 帧数；后续若平台布局改变，应同步 authored segment 数据，不应重新引入坐标特例。
3. 共享工作树继续包含其他任务的场景/导入噪音；Fix Round 1 的交付安全性以 exact-index tree 与实现提交为准。

---

## Fix Round 2（成功 legacy interaction 的真实 Latched 边）

日期：2026-08-26

- reviewed HEAD：`0abbe639b8b462b89e28397f4c552a36ca6e18c5`
- Fix Round 2 实现提交：`722c42015ceeeae177064b89c5248176d8e7c99d`
- 标题：`fix: publish accepted legacy claw latch`
- 实现范围：`MechanicalClawController.cs` 与直接测试 `ThreadInteractionTests.cs`；2 files changed，68 insertions，21 deletions。
- 本节在实现提交后单独提交，以记录实现提交的不可自引用哈希。

### 根因与最小修复

Task 5 模型已真实计算出 `next.State == Latched`；旧交互 adapter 也已先完成轻敌拉取、重敌破防或 Bell Chain 激活。但成功分支直接对 `next` 再 Step 得到 `Released` 并只发布后者，因此 `ClawFrameChanged` 观察者从 `Travel` 直接看到 `Released`。`PlayerPresentationCoordinator` 只消费该类型化事件，所以只保留先前 `ThreadFired`，永远收不到 Latched 对应的 `ThreadHit`。

生产修复只有一行：`adapter.TryApply(...)` 成功后先 `Publish(next)`，随后仍在同一 physics call 内立即计算并发布 `Released`。没有 Apply 第二次、没有建立持久 motor constraint，也没有改变拒绝/死亡分支；失败路径仍是一次 `Failed(TargetLost) -> Idle` 且零 Latched。

### RED

先补测试，再运行新鲜编译与 pre：

```powershell
dotnet build
& $godot --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=pre
```

- build：08:40:46.450 → 08:40:53.990，exit 0，0 warnings / 0 errors；日志 `task6-fix2-red-build.log`。
- pre：08:40:54.000 → 08:40:59.609，exit 1；日志 `task6-fix2-red-pre.log`。
- 原始失败：`the generated PlayerPresentationCoordinator consumes legacy Latched as ThreadHit; expected=ThreadHit, actual=ThreadFired`。
- 该失败来自当次刚编译的 `Knight.dll`，不是旧 DLL 或旧日志。

新增测试对 GraveLampMoth、RustShellSquire、BellBackLaborer、BellChainMechanism 四个真实成功 fixture 分别记录完整 event list，断言恰好一次 `Latched`、随后恰好一次 `Released`、ApplicationCount/业务副作用恰好一次，最终 target=null、frame correction=0、motor grapple scale=1、line idle。真实生成 Player 的 `PlayerPresentationCoordinator -> PrologueVfxDirector` 边界断言 Latched 被消费为 `ThreadHit`。dead receiver 与 synthetic adapter rejection 继续断言零 Latched、一次 typed failure、零成功副作用。

### GREEN：共享工作树 final

命令与原始结果：

| 命令 | 时间 | exit / terminal marker |
|---|---|---|
| `dotnet build` | 08:43:05.105 → 08:43:09.531 | exit 0；0 warnings，0 errors |
| Prototype | 08:43:09.536 → 08:43:10.387 | exit 0；`PROTOTYPE_TESTS_OK` |
| Prologue pre | 08:43:10.389 → 08:43:47.515 | exit 0；`TASK6_FIX2_LEGACY_SEQUENCE_OK success=4 latched_each=1 released_each=1 failure_latched=0 presentation=ThreadHit`；`PROLOGUE_TEST_PHASE_OK phase=pre` |
| Prologue post | 08:43:47.516 → 08:44:35.814 | exit 0；`PROLOGUE_TEST_PHASE_OK phase=post`；`PROLOGUE_TESTS_OK` |
| focused proof | 08:44:35.815 → 08:44:43.665 | exit 0；`TASK6_MECHANICAL_CLAW_PROOF_OK casts=3 latches=3 travel_frames=56 failures=0 final=(770.35925, 63.933002)` |

日志：`task6-fix2-final-build/prototype/pre/post/proof.log`。警告仅为既有故意负路径（invalid presentation root、缺失 guidance target、rejected flow transition）。Fix Round 2 不修改 builder 或生成场景，因此没有重建/暂存新场景；Fix Round 1 的 12-builder 输出保持不变，并由 final pre/post 的真实场景再次加载验证。

### 精确 index 快照

只暂存上述 controller 与直接测试后，使用 `git write-tree` / `git commit-tree` 创建临时 detached worktree；其初始目录没有旧 `.godot`/DLL，完整 build/import 后再跑相同套件，结束后已移除：

- tree：`9a78e9d0150d1ecba5f17aba0cb80586407f0722`
- 临时验证 commit：`4774959f3a60d3d129f5143903a43f126ba8ddc6`（只用于验证）
- build：08:45:41.405 → 08:45:45.834，exit 0，0 warnings / 0 errors。
- import：08:45:45.837 → 08:45:57.822，exit 0，`VERIFICATION_EDITOR_GC_DRAIN_OK`。
- Prototype：08:45:57.824 → 08:45:58.667，exit 0，`PROTOTYPE_TESTS_OK`。
- pre：08:45:58.669 → 08:46:35.905，exit 0，包含 `TASK6_FIX2_LEGACY_SEQUENCE_OK ... presentation=ThreadHit` 与 `PROLOGUE_TEST_PHASE_OK phase=pre`。
- post：08:46:35.907 → 08:47:24.112，exit 0，`PROLOGUE_TESTS_OK`。
- proof：08:47:24.113 → 08:47:31.969，exit 0，3 casts / 3 latches / 56 travel frames / 0 failures。
- 日志：`task6-fix2-index-build/import/prototype/pre/post/proof.log`；暂存前后 `git diff --cached --check` 均无输出、exit 0。

### Fix Round 2 剩余风险

成功 legacy interaction 的 Latched 与 Released 是同一 physics call 内的两个同步事件，这是为表现/音频提供真实 contact edge 且不建立持久绳索的明确契约。后续观察者必须按事件消费，不应假设每个 physics call 最多一个 claw frame；本轮 regression 已固定该顺序与次数。
