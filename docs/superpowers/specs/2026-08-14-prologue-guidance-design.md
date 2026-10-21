# 序章第一关指导系统设计

日期：2026-08-14
状态：已批准设计，待用户复核书面规格

## 1. 问题与证据

真人试玩 `20260814-120907-b77083f6` 在 03:36 内完成了走廊检查点、上下分支和钟链捷径，
但在 `DefeatBellkeeper` 目标下于走廊、两条分支与钟链房间之间往返 31 次，累计 11 次普通死亡，
Boss 尝试仍为 0。玩家已经理解移动、战斗与机械丝，实际卡点是：HUD 只显示
`DEFEAT THE FALLEN BELLKEEPER`，没有指出钟链房间右侧的 `P1Complete` 才是 Boss 入口。

因此本功能解决的是第一关的空间导航和当前动作表达，不自动移动、不降低战斗数值，也不取消探索分支。

## 2. 范围

### 包含

- 仅在 `scenes/prologue/PrologueLevel.tscn` 挂载指导控制器。
- 默认开启的“下一步”文字、屏幕边缘方向箭头、目标进入画面后的世界标记和距离。
- 根据当前目标、房间、永久进度和 Boss 状态实时更新。
- 同一房间连续发生两次失败恢复后显示一条操作建议。
- 暂停菜单提供本次运行内的“指导：开/关”。
- 展示节点保留稳定路径和可替换视觉接口。

### 不包含

- 第二关及后续关卡的指导。
- 全局 Autoload、跨关卡教程框架或磁盘设置保存。
- 自动寻路、自动跳跃、传送、难度降低或替玩家完成机关。
- 强制指向隐藏凹室；隐藏内容继续保持可选探索。
- 正式美术、配音或本地化服务。

## 3. 玩家体验

HUD 在现有目标下方增加一行简短中文指导，例如：

`下一步：前往钟链房间右侧的金色出口  →  410`

- 目标在相机外：箭头固定在安全画面边缘，指向目标，显示近似距离。
- 目标进入相机内：边缘箭头隐藏，在目标上方显示程序化发光菱形。
- 可选择两个等价分支时：同时显示上下两个标记，文字明确“任选其一”。
- 目标完成或指导关闭：文字、箭头、标记全部隐藏。
- 失败提示不遮挡基础目标，只替换第二行辅助文字，并在下一次房间推进后恢复正常指导。

## 4. 第一关目标映射

目标解析必须使用稳定目标键；场景适配器把键解析为当前房间的 `Node2D`。缺少目标时记录警告、
保留文字并隐藏箭头，不得崩溃或选择一个不相关节点。

| 当前目标 | 当前房间 | 指导目标 | 文字 |
| --- | --- | --- | --- |
| `ReachCorridorLamp` | Awakening Courtyard | `Exits/ToCorridor` | 向右穿过庭院，进入断齿走廊 |
| `ReachCorridorLamp` | Broken-Tooth Corridor | `Checkpoint` | 点亮前方墓灯检查点 |
| `LearnThread` | Broken-Tooth Corridor | `Geometry/ThreadAnchor` | 朝锚点瞄准并按 K 使用机械丝 |
| `LearnThread` | 其他房间 | 返回走廊的出口 | 回到断齿走廊学习机械丝 |
| `ChooseRoute` | Broken-Tooth Corridor | `Exits/ToUpper` 和 `Exits/ToLower` | 上下路线任选其一 |
| `OpenShortcut` | Broken-Tooth Corridor | 上下两个分支出口 | 沿任一分支前往钟链房间 |
| `OpenShortcut` | Upper Pendulum | `Exits/ToMechanism` | 穿过上层摆锤，前往钟链房间 |
| `OpenShortcut` | Lower Buried Machinery | `Exits/ToMechanism` | 穿过下层机械区，前往钟链房间 |
| `OpenShortcut` | Bell-Chain Convergence | `Mechanisms/BellChain` | 对钟链机关瞄准并按 K |
| `DefeatBellkeeper` | Broken-Tooth Corridor | 上下两个分支出口 | 经任一分支返回钟链房间 |
| `DefeatBellkeeper` | Upper/Lower branch | `Exits/ToMechanism` | 前往钟链房间，不要返回走廊 |
| `DefeatBellkeeper` | Bell-Chain Convergence | `Exits/P1Complete` | 前往房间右侧金色出口，进入 Boss 区 |
| `DefeatBellkeeper` | Boss arena，未激活 | `ActivationTrigger` | 向右跨过门槛，唤醒失落守钟人 |
| `DefeatBellkeeper` | Boss arena，战斗中 | 当前 Boss | J 攻击；躲避出招 |
| `DefeatBellkeeper` | Boss 抛锤锁链窗口 | `BossRoot/FallenBellkeeper/ChainTarget` | 锁链已暴露：瞄准并按 K |
| `Complete` | 任意 | 无 | 隐藏指导，保留结算信息 |

若运行状态出现不应到达的房间组合，使用房间图中通往该目标的第一步出口；不得通过直接修改进度来纠正。

## 5. 失败后的辅助提示

`PrologueGuidanceController` 监听第一关的 `RecoveryRequested`。离开失败房间时，公开的 `SameRoomFailureCount` 仅投影为 `0`；最近失败挑战的连续失败 streak 会跨检查点保留，并在重进该房间时恢复。若玩家在另一房间失败，或当前目标推进、完成，则重置该 streak。
第二次及以后同房间失败时，显示对应提示：

- 庭院：`跳跃后可按 SHIFT 冲刺，先落在中间平台。`
- 走廊：`朝悬挂锚点按 K，可越过长距离。`
- 上层路线：`先观察摆锤节奏；机械丝可拉向轻型目标。`
- 下层路线：`跳跃末段使用 SHIFT，避免落入机械坑。`
- 钟链房间：`重型敌人不能被拉走；用 K 暴露弱点，再攻击。`
- Boss 房：`第二阶段等待抛锤，再对锁链按 K。`

本计数仅用于 UI，不写入 `PrologueProgress`，不改变血量、速度、碰撞或敌人状态。

## 6. 架构与替换边界

### `PrologueGuidanceModel`

纯 C# 决策模型。输入为目标、房间、进度摘要、Boss 摘要和当前房间失败次数；输出为不可变的
`PrologueGuidanceInstruction`：正文、辅助提示、一个或两个目标键、是否完成。模型不访问 SceneTree、
输入设备、相机或资源路径。

### `PrologueGuidanceController`

仅作为 `PrologueLevel` 子节点存在。读取 `PrologueCompletionController.CurrentObjective`、
`PrologueLevelController`、`PrologueProgress` 和当前 Boss 状态；把模型目标键解析为当前房间稳定节点，
再将世界坐标交给展示器。房间切换、目标变化、Boss 状态变化或指导开关变化时刷新。

### `IPrologueGuidanceVisual`

展示接口接收正文、辅助文字、最多两个目标的屏幕方向/可见状态/距离，以及总开关。
默认实现 `PrologueGuidancePlaceholderVisual` 使用 Control 绘制文字、边缘箭头和目标菱形。
稳定路径为 `Hud/Guidance/VisualRoot`；后续可用 PackedScene 替换视觉，不改变目标模型、节点路径、碰撞或进度。

### 暂停菜单

新增 `ToggleGuidance` 命令和按钮。状态只保存在当前 `PrologueGuidanceController` 实例，默认开启；
重新进入第一关恢复开启。其他关卡不会创建这个控制器，因此不会显示按钮或提示。

## 7. 数据流与容错

1. 第一关控制器或 Boss 状态发生变化。
2. GuidanceController 生成状态摘要并调用纯模型。
3. 当前房间适配器解析一个或两个目标节点。
4. GuidanceController 计算目标相对玩家和相机的位置。
5. Visual 接口更新文字、箭头和世界标记。

目标节点缺失、已释放或不属于当前房间时，展示器隐藏该箭头；控制器只输出一次具名警告。
切房、恢复、暂停和关闭指导时不得保留旧房间标记。指导关闭不暂停模型进度，也不影响真人遥测。

## 8. 测试与验收

### 自动化

- 模型表驱动测试覆盖表中所有目标/房间组合、双分支、完成态和连续失败提示。
- 结构测试要求第一关存在 GuidanceController、`Hud/Guidance/VisualRoot`、开关按钮和稳定节点路径。
- 集成测试真实切换房间、打开钟链后验证目标立即从机关切为 `Exits/P1Complete`。
- Boss 集成测试覆盖未激活、普通战斗和锁链暴露三种提示。
- 缺失目标节点测试验证警告、隐藏箭头且运行不中断。
- 暂停测试验证开关只影响指导视觉，不影响游戏暂停、进度或其他 HUD。
- 完整 `verify-prologue.ps1` 和现有 smoke 继续通过。

### 真人验收

重新开启一份正常路线记录。成功标准：

- 玩家不需要口头提示即可从钟链房间进入 Boss 房。
- `BossAttempts >= 1`，不再在 `DefeatBellkeeper` 下连续循环多个旧房间。
- 玩家能说明箭头与文字所指对象，且提示没有遮住角色或 Boss HUD。
- 完整通关后再根据真实时间判断是否达到 10–15 分钟；指导不能用自动烟测计时代替真人数据。

## 9. 非目标回归约束

- 不修改 PlayerTuning、敌人数值、Boss 数值、房间碰撞或出口拓扑。
- 不把隐藏凹室变为必经目标。
- 不把指导数据写入永久进度或真人报告中的完成判定。
- 不将任何序章专用类型注册为全局 Autoload。
