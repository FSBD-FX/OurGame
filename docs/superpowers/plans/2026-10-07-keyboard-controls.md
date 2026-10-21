# Keyboard Controls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让现有序章以真实键盘完成移动、攻击、抓爪、防御及Q环境交互，无须鼠标或先选模式。

**Architecture:** 在现有输入采样上增加独立动作语义与单帧仲裁；保留运动/能量/机械爪物理模型。方向候选解析和环境交互各有独立职责，UI只读权威状态。供后续收集和雾系统共享方向、交互和可观察候选。

**Tech Stack:** 现有Godot 4 .NET/C#，`Knight.csproj`的Godot.NET.Sdk/4.7.1与net9.0保持不变；不新增依赖。

**Spec:** `docs/superpowers/specs/2026-10-07-keyboard-collection-headlamp-fog-design.md`（用户已确认）。

## Global Constraints

- A/D移动，W/S配合A/D指定方向，Space跳跃/附着跃出，Shift冲刺，J攻击，K机械爪，L按住防御，1–4装备插槽，Esc暂停。
- Q不承担背包道具消费；初始交互半径80世界坐标单位，只接受站稳时的单次按下。
- 普通跳跃可以与J或K同帧使用；无普通二段跳；既有620抓爪射程、36鼠标辅助半径及牵引/能量/过热数值不改。
- 工作树为`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`，已是隔离工作树，不再复制；保留现有脏改动和未验收披风候选。
- 不恢复自动化、不新增付费生成、不发布或覆盖旧版本；场景只能由C# builder生成。
- 所有运行保存/设置/日志使用显式唯一外部case路径，在挂载场景前调用`IsolatedPrologueFixture.Configure`；原user://数据前后必须相同。

## Review Focus

- 鼠标停在屏幕外/任意位置：不能影响键盘抓爪或护盾方向（Task 2）。
- 多键同帧与暂停松键：不得产生延后攻击、重复跃出或额外跳跃/冲刺（Task 1/4）。
- 同方向多个目标与隔墙冰面：预览稳定、有效且发射锁定，不选被挡目标（Task 2）。
- Q旁边多个机关或角色在空中：只执行可见预览的一个，空中不执行（Task 3）。
- 老教程进度、空装备槽、护盾过热：不改存档来重学，不扣失败攻击能量，提示与实际规则一致（Task 4）。

---

## Verification boundary shared by the three feature plans

Task 1同时创建`.superpowers/sdd/2026-10-07-keyboard-collection-fog/verify-feature.ps1`。参数为`-Feature keyboard|fragments|fog -ExpectedOutcome red|green`，分别只允许`RunKeyboardControlsTests.cs`、`RunQuestFragmentTests.cs`、`RunHeadlampFogTests.cs`。每次在`E:/OurGame/Knight/.evidence/october07-keyboard-collection-fog`创建唯一case，传递`--case-storage-root=<该case绝对路径>`；记录编译/等待到退出的引擎进程、日志、源码/场景哈希与原用户数据前后清单。按已核实的`verify-task18-normal-route.ps1`隔离边界派生，不改其原脚本或放宽冻结记录。

编译失败只算接口/基线失败，不算行为RED。缺失类型先加最小无行为壳，使编译成功，再取得命名行为断言失败；GREEN要求退出0及对应唯一OK标记。只终止本case拥有的超时进程。执行前核对工具实际路径/版本，不安装新工具。

### Task 1: Independent keyboard commands and arbitration

**Files:** Modify `project.godot`, `scripts/shared/InputActions.cs`, `scripts/player/PlayerInputFrame.cs`, `PlayerInputReader.cs`, `PlayerActionModeController.cs`, `PlayerCombat.cs`, `scripts/abilities/MechanicalClawController.cs`; Create `scripts/player/PlayerActionCommandModel.cs`, 上述verify脚本；Test `tests/unit/PlayerActionCommandModelTests.cs`, `tests/integration/KeyboardActionRuntimeTests.cs`, `tests/RunKeyboardControlsTests.cs`。

**Interfaces:** 在`PlayerInputFrame`末尾追加可选`bool ClawPressed=false, bool InteractPressed=false`；实际J采样到`AttackPressed`，K采样到`ClawPressed`。新增`[Flags] AcceptedPlayerActions`（Jump/Attack/Claw/Interact/Dash/Shield）及`PlayerActionCommandModel.Resolve(PlayerInputFrame frame, bool attached, bool controlsBlocked)`。仲裁后采样帧只保留本帧有效动作，运动仍独立读取`MoveAxis`；旧模式状态仅作兼容表现，不作J/K必要前置。

- [ ] 写`BlockedConsumesAllEdges`、`AttackWinsOverClawAndQ`、`OrdinaryJumpCombinesWithAttackAndClaw`、`AttachedSpaceConsumesLowerPriority`：断言`Resolve(jump+attack,false,false)`含Jump/Attack，`Resolve(jump+claw,true,false)`只有附着跃出命令而无Claw；普通移动轴保留。
- [ ] 写真实J/K/L事件断言：不按Q/E能出刀/发爪；J+K+Q只出刀；暂停松键后恢复不出刀；L+附着Space不跃出。创建runner与安全wrapper后执行`pwsh -File .superpowers/sdd/2026-10-07-keyboard-collection-fog/verify-feature.ps1 -Feature keyboard -ExpectedOutcome red`，保存行为RED。
- [ ] 实现输入和仲裁；生命周期清理新增边沿。J结束抓取后正常判攻击，不能通过改速度、恢复次数或赠送能量实现。保留空槽/实际护盾状态的既有失败语义，过热回归与按键优先级冲突必须显式复核，不能删旧断言掩盖。
- [ ] 同一命令改为`-ExpectedOutcome green`；要求`KEYBOARD_CONTROLS_OK`和真实运行断言通过；记录既有测试因输入契约变化需要的逐项迁移，不批量放宽。
- [ ] 独立审查后仅提交本任务自有文件，检查暂存清单；保留所有无关用户改动。

### Task 2: Keyboard aim and visible claw selection

**Files:** Create `scripts/player/PlayerKeyboardAimModel.cs`; Modify `scripts/abilities/MechanicalClawTargetResolver.cs`, `MechanicalClawController.cs`, `scripts/player/PlayerShieldController.cs`, `PlayerInputReader.cs`; Test `tests/unit/PlayerKeyboardAimModelTests.cs`, `tests/integration/KeyboardClawTargetTests.cs`。

**Interfaces:** `PlayerKeyboardAimModel.ResolveDirection(Vector2 rawAim, int lastFacing) -> Vector2`返回归一八向/无输入面向；`PlayerInputReader.KeyboardAimDirection`为只读实际方向；`MechanicalClawTargetResolver.ResolveKeyboard(Node2D caster, Vector2 direction) -> MechanicalClawTargetResolution`供预览和施放共用，原`Resolve`保留给旧测试/适配但不作正式键盘路径。

- [ ] 写`EightDirectionsAndFacingFallback`：W+D得到归一(1,-1)，零输入面向-1得到Left；屏幕鼠标位置不改变结果。
- [ ] 写真实地形/锚点case：620内有效，超距/冰面/墙后拒绝；同方向唯一稳定预览，按K后转向不改已锁目标；无有效目标返回None。运行keyboard RED。
- [ ] 实现范围内八向候选搜索：初始半角22.5度，先过滤材料/射程/LOS，再按方向偏差、距离、稳定对象ID排序；地形使用有限射线扇采样，不用620终点附近36圆形假装方向辅助。护盾改读共享方向，保留实际GuardClass与能量结算。
- [ ] 运行keyboard GREEN并补真实跳跃接K、附着Space、K取消后落台、L取消及护盾方向回归；只把数值/局部物理通过写成对应证据。
- [ ] 独立审查后只提交本任务文件。

### Task 3: Q environment interaction

**Files:** Create `scripts/interactions/IEnvironmentInteractable.cs`, `scripts/interactions/EnvironmentInteractionModel.cs`, `scripts/interactions/EnvironmentInteractionController.cs`; Modify `tools/build/BuildPlayer.cs`及其生成的`scenes/player/Player.tscn`；Test `tests/unit/EnvironmentInteractionModelTests.cs`, `tests/integration/EnvironmentInteractionRuntimeTests.cs`。

**Interfaces:** `IEnvironmentInteractable`提供`string StableId`, `Node2D InteractionNode`, `bool CanInteract(CharacterBody2D actor)`, `bool TryInteract(CharacterBody2D actor)`；注册组`environment_interactable`。controller提供只读`IEnvironmentInteractable CurrentCandidate`，只消费已仲裁`InteractPressed`，不发送伪造ThreadInteraction。

- [ ] 写`NearestVisibleIdTieBreak`、`AirborneRejects`、`Beyond80Rejects`、`PressExecutesExactlyOnce`：两对象同距按Ordinal稳定ID选择；80以内且LOS/站稳才可执行；按住Q不重复，Q不消费装备。
- [ ] 在无保存副作用的两个测试机关fixture取得keyboard行为RED。
- [ ] 实现单候选交互，提供`Q 交互`提示；旧必须靠爪的曲柄/锁链不因本任务自动改为Q，真实新Q机关后续复用接口。
- [ ] 运行keyboard GREEN，验证同时J/K/L的消费、对象卸载、暂停与复活；生成/重新实例化玩家scene节点数一致。
- [ ] 独立审查后只提交本任务文件。

### Task 4: Teaching, UI and complete real-input route

**Files:** Modify `scripts/ui/InputGlyphResolver.cs`, `PrologueControlsView.cs`, `PrologueFormalHud.cs`, `scripts/prologue/PrologueLevelController.cs`, `guidance/PrologueGuidanceController.cs`, `sample/BasinSampleGuideController.cs`, `assets/localization/prologue-ui.json`, `tests/presentation/ContinuousCanyonRouteProbe.cs`, `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`；extend keyboard runner/专项测试与README。

**Interfaces:** 教学以实际`AttackStarted`/被接受抓取/护盾事件推进；保留既有Objective枚举/已完成教程ID的存档兼容，替换展示文字而非批量重置进度。路线驱动只发正常键盘事件，不写玩家位置/模型状态。

- [ ] 写`OldTutorialRemainsCompleted`、`EmptySlotDoesNotSpend`、`KeyboardHelpMatchesRuntime`与按键切换后无旧Q/E/LMB/RMB必要提示断言；运行RED。
- [ ] 更新中英文字/图标、暂停键盘导航和真实路线驱动；不把手工fixture击杀计作正常通关。
- [ ] 运行keyboard GREEN，再用新的唯一来源快照扩展正常路线wrapper验证Main/Explore及Boss/实体出口。旧wrapper冻结约束不删除，输入版本变更另存比较记录。
- [ ] 对正常窗口运行、实时帧间隔和短录制作限定审阅；展示前保存到`E:/OurGame/Knight/过程图稿与视频`新日期/主题/版本目录并维护索引。
- [ ] 更新README事实/未完成项，独立审查后只提交本任务文件；未经实际通过不标记整个序章完成。

计划状态：待用户审阅并选择执行方式。尚未实施或运行上述任务。
