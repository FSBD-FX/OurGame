# 推进器模块与自由运动核心 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Root主写／原生串行，并行代理只读；不另起实现者写同一工作树。

**Goal:** 先交付经过真实RED→GREEN验证的推进器不可变参数模块和自由运动积分核心，为获批短喷的实际Player接入提供唯一参数／数学入口；本计划结束不宣称游戏已能短喷或完整序章完成。

**Architecture:** 保留CharacterBody2D和现有唯一Motor。新增Resource只生成值快照，新增纯动力学核心只接收／返回速度及有限燃烧量，不写节点位置、不另有控制器。实际输入原子接受、爪／Motor／落地／装配／视听接入属于紧随本计划的同一已批准规格后续阶段，不在未验证前改现有Player路径。

**Tech Stack:** 当前Godot.NET.Sdk/4.7.1、net9.0、C#；固定本机dotnet9.0.317与Godot a13da4feb，不安装／升级依赖。

**Spec:** [已批准短喷规格](../specs/2026-10-18-shift-thruster-inertia-design.md)。完整目标仍是[总待办](../../production/2026-10-18-project-remaining-backlog-v001.md)及2026-10-18用户“设立目标并完成1–11”，不缩成这两个任务。

## Global Constraints

- 权威隔离checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`，branch `codex/task6-certified-final-20260902T0555`；保留所有既有dirty改动，不创建新checkout。
- 用户已批准设计、先说明全部实现点并继续；执行方式沿既定root主写／并行只读，已完成说明，不再次制造相同审批停点。
- 任一自有build／native消费者存活时冻结全部source／helper／docs，不并发构建，不抢焦点／OS按键／自动补键。
- 真实userdata仅哈希库存保护，不读取语义；新测试使用外部隔离case，原始stdout／stderr／external、源码／helper／binary保管链保留。
- 默认ModuleId `mobility.short-jet.standard.v1`；BurnDuration0.18s、ThrustAcceleration5600px/s²、PoweredProjectionLimit850px/s、GroundFriction1800px/s²、AirDragRate1.6/s、GroundReverseBrake4000px/s²、AirReverseBrake3000px/s²。
- 普通260／2200／1500跑动参数、1850重力／780下降限速及590跳速保留。短喷新增推力不是固定速度覆盖。
- 无新付费／素材生成、前台游戏、用户存档、存档schema／地图／敌人数值修改、自动化恢复、Git提交／推送／标签或发布。
- 五项既有严格失败保持OPEN；新阶段不使用named waiver、GC／cache／借用Dispose／退出drain或日志过滤改变结果。

## Review Focus

1. Resource运行中修改不能热改已取快照；任务1测试旧快照与新快照不同，拒绝非法数。
2. 未开始或最后不足一tick的喷射不能多发推力；任务2独立公式测试0／0.005／0.18s及不同delta。
3. 起始反向喷不能误取消，纯竖直喷不能被水平刹车取消；任务2三种方向／实际速度组合测试。
4. 外来高余速／垂直速度不能被850总速度归一化截断；任务2高入射／切向分量负控。
5. 无效delta／NaN输入不能产生新非有限速度或消费燃烧；任务2校验拒绝且输入值不被修改，不用TestAssert.Near掩盖NaN。

## 验证入口与记录约定

- 原现成单元依赖Godot Vector2／Mathf／Resource，没有独立dotnet test项目，不伪造该命令。
- 新增`tests/RunThrusterCoreTests.cs`，仅`--phase=module`或`--phase=dynamics`；独立SceneTree直接调用相应unit库，不挂Player／场景、不drain。成功唯一`THRUSTER_CORE_OK phase=<phase> human=false whole_goal=false`，异常保留并Quit(1)。
- 复用已有自有process／清单保护owner `.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1`，新增strict `thruster-module`／`thruster-dynamics`分支选择新runner，不改变旧phase规则；`ExpectedFailure`对此两phase拒绝。现存helper先保留版本，新增分支不借旧runner的drain。
- 命令前按原Get-SourceInventory规则实际点数，传精确`-SourceRows`，不沿用1675。Run：固定pwsh `-NoProfile -File <owner> -Phase thruster-module|thruster-dynamics -SourceRows <实际数>`。Build fresh Rebuild<=180s，focused engine<=55s；预期GREEN＝build无错警告、engine0、唯一正确marker、完整raw无ERROR/WARNING/FATAL、所有保护／before-after／binary一致。RED仍严格失败案例，不通过参数豁免。
- 当前广域pre／post已知失败不是绿色基线。每任务后跑新专项及现有gravity-delta／keyboard／actions；阶段末再跑完整pre／post并按名报告，不能把专项绿称全suite绿。
- 执行台账：`.superpowers/sdd/2026-10-18-thruster-module-and-dynamics/progress.md`。不以Git提交数记完成；本轮无Git写权限，保留源快照／哈希和原始案例，不删除台账。

## Task 1: 参数Resource与不可变快照

**Files:**
- Create: `scripts/player/MobilityModuleSettings.cs`、`scripts/config/MobilityModuleDefinition.cs`。
- Test: `tests/unit/MobilityModuleTests.cs`、`tests/RunThrusterCoreTests.cs`。
- Modify: 自有owner新增两个phase的严格入口、snapshot scope与成功检查；不改原PlayerTuning／Motor。

**Interfaces:**
- Produces: `readonly record struct MobilityModuleSettings(string ModuleId, float BurnDuration, float ThrustAcceleration, float PoweredProjectionLimit, float GroundFriction, float AirDragRate, float GroundReverseBrake, float AirReverseBrake)`；静态`Standard`返回唯一默认快照。
- Produces: `[GlobalClass] partial MobilityModuleDefinition : Resource`导出上述八字段；`MobilityModuleSettings ToSettings()`值拷贝；`IReadOnlyList<string> Validate()`及`static IReadOnlyList<string> Validate(MobilityModuleSettings settings)`。
- 时长／推力／投影上限／主动制动力须有限且>0，摩擦／阻力有限且>=0；ModuleId非空白且无首尾空白。非法不自动修正。

- [x] **Step 1: 写测试与最小runner**。测试通过运行程序集发现正式Resource类型并真实实例化／调用，不grep源文件；若类型不存在，明确行为断言失败。`SnapshotDoesNotHotReload`使用非默认八字段，取快照后再改Resource，原快照保持；`InvalidFieldsAreRejected`逐字段0／负／NaN／Infinity及空白ID，检查有命名错误。`StandardProfileIsValid`检查可被验证接受，默认行为数值由任务2消费验证，不仅断言常量。
- [x] **Step 2: 运行module，读取真实RED**。预期构建成功、引擎断言“Mobility module definition is unavailable”，不是编译错误或native异常；保留严格FAIL。
- [x] **Step 3: 实现上述两个类型**。Resource默认字段由Standard快照取得，禁止两套默认常量；验证只返回命名错误，不改变Resource。测试自己创建的Resource在finally确定Dispose，不处理任何借用对象。
- [x] **Step 4: 运行module及gravity-delta／keyboard／actions**。预期新module严格GREEN；既有门按实际结果记录，不隐藏失败。
- [x] **Step 5: 台账记录**。保留精确文件／case／完整日志／哈希、任务状态与尚未接入Player的事实，不Git提交／发布。

## Task 2: 有限推力、阻力与水平制动数学核心

**Files:**
- Create: `scripts/player/ThrusterDynamics.cs`、`tests/unit/ThrusterDynamicsTests.cs`。
- Modify: `tests/RunThrusterCoreTests.cs`、owner输入scope；原PlayerMotionModel／State／Motor暂不变。

**Interfaces:**
- Consumes: 任务1的`MobilityModuleSettings`与验证、现有`PlayerMotionSettings`。
- Produces: `readonly record struct ThrusterDynamicsInput(Vector2 Velocity, float MoveAxis, bool OnFloor, Vector2 BurnDirection, float BurnRemaining, bool ApplyGravity = true)`。
- Produces: `readonly record struct ThrusterDynamicsResult(Vector2 Velocity, float BurnRemaining, float BurnUsedSeconds, Vector2 GravityVelocityDelta, Vector2 ThrustVelocityDelta, bool Braking, bool CancelledForReverse)`。
- Produces: `static ThrusterDynamicsResult ThrusterDynamics.Step(ThrusterDynamicsInput input, MobilityModuleSettings module, PlayerMotionSettings motion, double delta)`；输入为值，不写body／预算／跳跃／爪／风／上升权限。非法有限性／范围抛命名Argument异常，零delta合法无施力。

- [x] **Step 1: 写独立数值测试**。先用反射边界取得缺失核心并断言缺失，再由literal输入／独立公式判断结果，不计算期望自生产helper。采用无重力／阻力对照证明v0=100、a5600、有效0.005s→128且燃烧归0；完整0.18s、v0=0→850封顶；高初速1000保持而非归一化；沿水平喷的垂直分量不被截。
- [x] **Step 2: 运行dynamics，读取真实RED**。预期引擎断言核心尚不存在，构建和原生基础可用。
- [x] **Step 3: 实现Step**。按规格4.1唯一顺序，将tick拆为有限燃烧／余段积分：水平控制→重力→沿d有限新增推力→原下降终速，风由以后Motor单独合成。非零水平喷嘴只被动阻力，不叠普通驱动；纯竖直喷和非燃烧段采用互斥水平控制。
- [x] **Step 4: 补测并先看对应RED再实现分支**。无输入空速600在0.1s后为`600*exp(-0.16)`；反向空速600、0.1s后300；0.25s先0.2s刹停、余0.05s普通反驱动为-75。反喷嘴后续输入取消／初始逆速度喷保留／纯竖直水平刹车三例分开。测剩余燃烧不足tick、零delta、非法值、同向超跑速、斜向归一化、30／60／120Hz收敛与重力独立贡献。
- [x] **Step 5: 运行dynamics＋module及既有三门，再完整pre／post**。预期新专项严格GREEN；完整结果逐名保留，若有FAIL本计划不标全面通过。独立复核新增组件与台账，后续再接真实Player，不能把纯积分测试算真实碰撞／正常全关／真人手感。
- [x] **Step 6: 保存阶段证据和后续接入说明**。将后续原子输入／Claw接管、Motor唯一MoveAndSlide／来源拆分、可靠落地、装配、typed视听／文案、全关／真人门保留OPEN；不删除旧证据／台账，不发布。

## 自查与后续衔接

已按规格检查本计划覆盖模块及自由积分两个可单测边界：任务1→任务2只有不可变Settings，字段同名；Task2没有第二个Velocity状态／节点权威，不在输入预算接口未完成时偷偷替换Motor。其余获批要求明确留给紧接的Player整合计划，不是删减。

执行Ruling：用户“先讲再开始”及“设立目标并完成1–11”授权沿既定native方式持续推进，计划已按说明顺序展示，不重复问执行方式；代价是实现细节由root负责并经测试／最终独立审查。Bash脚本写文件／提交／删除scratch与当前Windows／apply_patch／无Git写约束不兼容时，按同一plan路径用apply_patch维护台账和任务brief；证据不以未发生的commit伪造。
