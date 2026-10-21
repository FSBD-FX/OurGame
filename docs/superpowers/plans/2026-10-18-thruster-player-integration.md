# Shift短喷实际Player整合 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans；root唯一写者／串行原生消费者，代理只读。继续既定已批准流程，不另问相同规格或执行方式。所有任务均须读真实RED再实现对应新行为；无前台抢焦点／OS按键。

**Goal:** 将已验证的模块与自由动力学真正接入保存Player，实现获批短喷、有限预算、爪／风／上升来源、可靠落地减伤、装配入口及typed视听，不再仅交付未接组件。此计划是完整目标1–11中的短喷部分，不能替代地图／50常驻／最终视听与整关交付。

**Spec:** `docs/superpowers/specs/2026-10-18-shift-thruster-inertia-design.md` 已批准。前置组件证据 `docs/production/2026-10-18-thruster-core-native-v001.md`；两任务ledger complete，勿重做。完整总账 `docs/production/2026-10-18-project-remaining-backlog-v001.md`。

**Architecture:** 保存根CharacterBody2D不换类型；Motor唯一写最终速度、唯一MoveAndSlide。保留原输入优先级，但Input同时保留经生命周期抑制、尚未经动作仲裁的原始帧；Motor持有原子接受权威，Claw只消费已接受交接。单一pulse状态持有燃烧／预算／身份，不另存一份身体速度。所有配置以不可变快照生效。

**Stack / constraints:** Godot4.7.1 a13da4feb、net9.0、固定dotnet9.0.317；同一codex linked dirty checkout，不安装／升级／新建worktree。不手改tscn；保存Player资源改动必须builder生成、先外部候选、搬移读回后采用。真实userdata仅库存哈希保护，隔离storage；所有source/helper/docs在任何owned消费者期间冻结。保留五FAIL和原始raw，不改GC/cache/借用Dispose/drain/日志豁免/时限。没有新付费或Git发布。没有自动化恢复。

## Interfaces / shared invariants

1. Task1产生`PlayerInputReader.RawFrame`，与`CurrentFrameSequence`同一次采样。Raw指动作仲裁前、边界抑制后的事实，不是另一个授权通道；清帧／一次性消费／暂停恢复同样清原始边缘。
2. Task2产生单一pulse状态和只读接受决策／提交身份：生命周期、配置epoch、单调BurnId、输入采样序号；不存第二份速度。Task3消费同一实例，不能同时保留旧Dash预算和新pulse预算。
3. Task3消费已验证`ThrusterDynamics`／`MobilityModuleSettings`，将跳跃来源与抓爪环境权限分开；Task4只消费唯一移动前后冻结的接触测量，不根据技能状态免伤。
4. Task5只在真实站稳／无待提交动作等合同满足时换快照，递增同一个配置epoch；Task6通过typed只读观察／唯一事件消费，不向运动或预算反写。
5. Task7迁移旧消费者，正常双路仍用真实输入、AI／危险／碰撞；不将未燃烧误当停稳，不将Headless或固定FPS录制当真人／显示60FPS。

## Verification owner

延用自有`.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1`，逐新增严格phase、真实源库存计数和明确行为receipt；不改旧phase清理策略。`thruster-*`不得用ExpectedFailure。新的原生输入专项可以由现有无drain `RunThrusterCoreTests`显式dispatch，但必须诚实标明手动采样适配器与自动Physics/完整Player链之区别。每阶段保留fresh build、stdout/stderr/external、清单及before/after/binary；原生消费者终态后再编辑。

用户仍可能使用其他窗口；原生headless物理键事件走Godot输入映射，不发送桌面按键。正常显示／真人阶段需另择可用时段。完整pre/post仍严格FAIL不暂停实现，但按名记录，禁止冒称全suite通过；目标最终必须关闭。

## Task 1: 原始输入事实与边界清理

**Files:** `scripts/player/PlayerInputReader.cs`；新增`tests/integration/ThrusterKeyboardFactsRuntimeTests.cs`；`tests/RunThrusterCoreTests.cs`／自有owner明确`thruster-input` phase。

**Produces:** 只读`RawFrame`与既有同序号。不改变动作仲裁结果，不开始短喷，不修改Motor／爪。

- [ ] 先用真实Godot物理键事件＋实际InputMap写测试，反射检查缺失RawFrame而明确失败，不以编译错误作RED。最小入树InputReader／Health上下文，无禁用Player碰撞凑通过；本小测试可手动调用采样适配器，须显式标注非自动Player链。
- [ ] 同采样Shift+Space+K：Raw保留原始三个边缘，Current只Dash；Q/J/L优先级不改。无边缘重放、ClearSampledFrame、ConsumeOneShotEdges、ResetForGameplayBoundary及暂停/恢复的抑制同样作用Raw。
- [ ] 观察RED、最小实现、fresh GREEN；确认此步骤不把Raw当执行入口。后续Task3必须再以真实自动Physics键盘链证明组合预算。

## Task 2: pulse预算、身份与预检／提交状态

**Files:** 新增`scripts/player/ThrusterPulseModel.cs`及对应unit；后接`PlayerMotionState`时才移走旧预算唯一权威。

**Produces:** 接受请求的只读预检／有版本身份提交；活跃燃烧、锁定方向、空中预算、burn/lifecycle/config/sample身份；typed开始／唯一结束原因。无节点、速度、爪或输入注入。

- [ ] 先测拒绝零副作用（死亡、暂停、配置非法、正在燃烧、空中预算无、同采样重放）；地面Shift与地面Shift+原始Space、空中Shift区分；长按不连发。预检后reset／更换epoch／同sample再提交拒绝，预算不被旧令牌更改。
- [ ] 真实落地恢复只有一处通知；接爪／顶墙／换区／暂停／燃烧结束不退款。生命周期重置清状态但不让旧身份复用越权；成功起喷身份只生成一次。
- [ ] 对应RED→GREEN。补上前review的多Hz未饱和制动0.1→300／0.25→−75，30Hz用末步fractional delta，对独立分段公式，不以饱和−260代替；测试覆盖补强不伪造原产品有bug。

## Task 3: Motor原子接受、Claw交接与真实自由积分

**Files:** `PlayerMotor`／`PlayerMotionModel`／`PlayerMotionState`／`PlayerMotionResult`，`MechanicalClawController`及必要model帧，既有KeyboardClaw／PlayerTraversalInput与新短喷runtime tests。

**Consumes:** 同采样Current＋Raw Space、单一pulse、模块快照。**Produces:** 已接受事务、实际body速度唯一继承、真实喷力／重力／风／碰撞回写。

- [ ] 先运行真实键盘自动physics的Shift+Space+K RED：仅一次喷射、无普通跳／K cast、空中资格消费、松键／喷完无延迟重放；地面单Shift保留空中资格作为对照。前置场景合法出生／真实支撑，不能靠反射写仲裁后帧证明原始组合。
- [ ] 由Motor只读检查全部资格后事务接管，调用专用“已接受短喷撤爪”；保存真实body.Velocity一次，不走普通Released的boost／去inward／release限速／ResetFallTracking。不在Shift拒绝时先Cancel爪；拒绝不提前返回跳过物理。
- [ ] 事务回调发生死亡/reset／旧cast/旧sector/lifecycle/config迟到时禁止回来继续起喷或恢复correction；每次预检／提交与事件身份必须有效，重入不能双发。
- [ ] 用pulse取代旧固定速度Dash分支及唯一预算；保留兼容IsDashing/计数别名但语义为实际燃烧。每个真实tick一次MoveAndSlide；高入射／角落／薄墙／低顶受阻分量回写，不积蓄隐藏速度。
- [ ] 普通跳缓存／土狼／JumpCut保持，但Shift优先消费的Space不缓存。分离爪上升保护、喷力实际向上贡献保护、风/相机环境授权；水平Shift不切断合法爪上升保护，普通跳／风不冒认喷气来源；顶撞／顶点／落地／普通新跳／完整替换／死亡清对应来源。
- [ ] 真实附爪接管才结束余喷，不在Aim/Travel早结束；Reel仍单一爪速度所有权，不叠自由重力风；Swing沿原重力支撑合同。未附爪喷射／余速仍受原气流一次整tick合成。
- [ ] `ExternalImpulse`保持完整替换、不退款；暂停冻结、恢复不重放；换区不中断同一Player运动。原旧fixture的“拒绝不积分重力”断言改成独立合法积分对照，不扩大容差。
- [ ] RED→GREEN、真实风／爪／碰撞／生命周期focused回归、重力／键盘／动作及广域逐名记录。公开动力学结果旁补终速归因说明，禁止把两贡献向量当长期来源账。

## Task 4: 可靠着陆样本与高度兼容减伤

**Files:** `PlayerFallDamageModel`／`PlayerMotor`／`PlayerTuning`，新增接触采样value/adapter及unit/native tests；只用已安装公开Godot API，必要时读本地bindings。

**Produces:** 唯一MoveAndSlide前冻结的指令速度与保守可靠性／回退原因；碰撞后同一步相对冲击；高度基数先不截HP→factor平方→ceil→最后伤害上限。

- [ ] 先独立公式RED：800/780→1、800/≤300→0、1400/540→1，同按键但高速仍伤；双方同速法向冲击为0。配置有限、参考速度>安全速度，安全300集中角色配置。
- [ ] 首版仅空中开始、无平台携带、唯一有效floor slide、无恢复/嵌入/snap、travel/remainder一致且接触点速度可靠时启用；墙→地、多/混合支撑、carry、非法数/法线明确回退，不猜GetRealVelocity或post-contactY。真实移动floor另作对照，不重复计平台。
- [ ] 全软metadata必须全部显式bool true，任一普通支撑不免；每次落地先消费后Health，i-frame不站稳后补扣。真实爪刹车/上升/反弹原合同保留；熔炉/KillZone/战斗/射线不走减免。
- [ ] RED→GREEN及原摔伤、复杂接触、移动支撑和喷后再次落下负控，回退也须消费一次落地，不把样本无效当免伤。

## Task 5: 保存模块消费与未来装配入口

**Files:** `BuildPlayer`／Motor/模块装配入口、候选保存资源及runtime tests；禁止手写tscn。

- [ ] 默认模块由唯一Standard／Resource快照消费，保存候选Player搬移重载后仍实际运行同module ID／参数。旧显式非默认Dash/GroundDeceleration作者资源必须诊断映射，不能静默忽略；不升级用户schema4。
- [ ] 先测装配事务：存活非暂停、连续两个完成物理tick真实floor且相对切向速度<1、无跳/Shift待提交、无燃烧/Claw Aim/Travel/附着。移动平台亦可站稳；单IsOnFloor不足。
- [ ] 两测试模块实际运动不同；非法/空中/燃烧/未站稳更换拒绝无状态修改。成功只换快照增config epoch，不改速度/位置/HP/预算/残片/检查点；Resource随后编辑不热改正在运行模型。

## Task 6: typed动作、喷焰、声音与键盘UI

**Files:** PlayerPresentationCoordinator／PlayerActionFrame／正式visual/world effects／audio bank接缝、localization和UI测试。

- [ ] typed Burning/Coasting/Braking区分，实际开始/终止/取消唯一Burn身份；余速观察最长0.45且按规格提前退出，不建立第二速度、不清仍有效上升保护。普通刹车不伪造BurnId。
- [ ] 喷焰反向锁定喷嘴、接受才播放、停喷即停止；倾身/回稳/制动不拉伸图片伪装，J/L更高层覆盖仍一致。旧音bank可标占位，不双播，不称最终音色；无付费生成。
- [ ] 同步首关／暂停／UI“Shift短喷，A/D反向制动”；不造燃料条/新槽/永久冷却。低闪有效，所有展示先归档original/presentation/index并审阅。

## Task 7: 消费者迁移与正常路线交付

**Files:** 全部旧Dash/GroundDeceleration/HasGrappleReleaseMomentum消费者，路线预测、相机、Thread取消与测试；由rg列完整清单，不能只改Motor。

- [ ] 旧停距v²/(2a)不得用于指数空阻；读取运行同快照，分支前向预测、独立公式与负控；所有旧!IsDashing停稳假设迁移。
- [ ] 同一冻结版本正常Main与Explore从标题到同Player Boss与实体出口，实际喷射/余速/制动/爪/风，不禁AI/危险/碰撞、不改HP/运行中坐标凑通关；失败raw保留。存档/死亡/暂停/换区/旧档仍隔离实测。
- [ ] 关闭本阶段发现的新回归，五旧FAIL按名继续根因修复；正常速度AV先归档并审阅、真人键盘时段和实际显示性能单独验收。专项、可玩机制、整关和最终视听四种结论分开。

## Pre-flight self-review / ledger

Task1→2/3共享原始边缘与sample序号；2→3/5/6共享唯一身份/预算与epoch；3→4共享唯一移动测量；3/5→7共享模块快照及来源权限。均沿spec定义，无新增存档、背包、付费或第二运动owner。方法具体落点按源码接缝落实，如需偏离须先ledgered Ruling；尤其不得用普通Released接管作为简化。

状态保存于`.superpowers/sdd/2026-10-18-thruster-player-integration/progress.md`。user已批准规格并要求完整1–11持续完成，按已说明root native流程执行，不重复审批。无Git写／不删ledger同前组件；整体未绿不合并或发布。
