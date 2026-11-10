# v119 · 蒸汽区三批战斗与主线实际决策

2026-10-26；沿用原 Task15 和完整十一项，目标 ACTIVE。前一目标轮分类 **PROGRESS**：实际推进了合法战斗输入并改变下一障碍的因果证据，不是整关、正式视听、性能或交付验收。唯一工程 `G:/knight/Knight`；用户最后窗口状态仍为 **NOTSEEN**。

## 实际改动与范围

仅 `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`，对物理封存 v118 基线为 **225新增/22删除**，Source1844/C#892、0新增/删除。生产AI、物理、地图、生命、能量、原敌人和时限、公共helper均未改。

1. Steam 所选 Bell 使用已有实际 ready 窗口，而非仅 `frame % 75 == 0`。保留同一次原 PlanSelects、实际键盘K/CastAt、目标/application/生命/危险/支撑门；其他调用保留旧节拍。没有传入会开启 PistonTwo 诱导的 `gearLureBell` 来冒充这个策略。
2. Steam 原批次实际 `ArrivalWarning + Live0` 时，在原900次外层循环内保持中性行走、释放J/K、用一次替代原危险扫描并防御真实威胁，让原生预告与生成正常发生。该分支置于原 Sentry 退休尾声逻辑之后，没有新等待池、强制生成或重置时钟。
3. 仅 Steam 键盘调用传入原 authored Arena。当原最近目标为场外居民且实际仍有有效正生命 owned wave 时，保留该必做波次为追逐主目标。原单次子节点扫描、其他默认调用、居民本体/AI/碰撞和全部危险源扫描保留，不把场外居民从危险列表过滤。
4. 新增被动原生边界观察：原 observer priority100，记录物理帧/序列、标量位置/速度、波次/激活/完成、最近外层目标上下文与真实原始键状态；原 try 内订阅/finally清理，不新增物理查询、输入或额外await。`LastSelected` 明确只是 last_outer_decision。
5. Main HP1 原混合战斗增加同次扫描事实与实际短路门掩码。原14组表达式求值顺序、调用次数、180tick原证据和 fresh guardCapsule=null 保留；缓存能量不冒充被执行的 CanAfford 结果。保留最近96行及drop/fault计数，日志异常不得覆盖原路线异常。

一个实验性 Steam resident Charger K 分支在真实应用后仍失败，已经从最终候选移除；不是移除生产机械爪能力。其原始日志/源副本继续保留。

## RED、保留的反例与局部 GREEN

- 旧节拍首个K在loop150，真实ready已在39–50。031640的首K变为 **loop39/physics7325**，下一次105；整战斗仍失败，不能凭这一步称通关。
- 033019原生边界证实 physics7697/loop376/x3840.2896/vx169，实际原始键26（D+S+L）越出Arena，batch1→-1/live0/inactive；7791回入Activation后新batch0。970样本/11边界/0遗漏/0fault。此前“可能重开”现在有真实离界与重开链，不是存档瞬移故障。
- 034215实验K在7499/loop177用原同次plan、application0→1成功；敌人3901.998→3873.032、玩家3525.355→3592.654，不能证明有效近战反击。后来7844真实死亡恢复失败，实验分支退役。
- 035005的实际arrival hold36tick保持HP2，Sentry在7741/loop419用真实K2/J2退休；批次2又hold16tick，原生Missile出现。但再次追场外Charger，7823越界重开，原剩余时钟失败。这是保留的失败，不加预算补绿。
- **035740最终同源Explore**：原Steam三批全部完成，**10J started/10Connected/4K、HP2→2、damage0**；初始Bell6J、Sentry真实K2/J2，Missile真实发射后2J/Defeated。601实际样本/7边界/0fault，无离界、无重开。没有清空居民或关闭攻击。
- 整Explore仍 **FAIL**：出口 physics8121，`walk-4170` 的2/112，玩家(4003.5093,4199.932)生命2→1。实际accepted-damage归因至居民 `canyon.v2.resident.steam-valves.03`、`scrap-charger.tscn` 的 RushHitbox；Actor798259941817/SourceRuntimeId798360605118/LocalAttackId3。旧阀门和paired jets当时未激活，无fall；不把它误诊为蒸汽机关。

## Main仍失败：真实限制而非猜输入丢失

最终040547 Main131125ms，仍因真实伤害致死后恢复而触发原no-reset门。该案6815首J将owned Burst HP2→1，6834–6882实际Stunned，原jet在6853完成，owned firstShot6949才到来。原完整J恢复阈值约.43333334s；后期全源安全窗口与能量仍不足。032740是另一诊断，不能把其6814/6833/6948帧号跨案当成最终040547。不得删除真实Shot/K义务或容许任意第二次K绕过原单次收据。

最终040547最近96条为physics7020–7115，全部 `outer_rejected`，eval3/pass1/fail2/notEvaluated16380，maxSafe.2833333；same-pass事实fault0/drop0，较早环形行丢弃181。原表达式在危险许可处拒绝，**未执行后续能量门**；该案缓存E9.066517→0不能倒写成这些分支都因能量拒绝。032740的7019–7114/E13.733192属于另一运行。实际限制源包括Moth Recover、resident Burst Telegraph/仍活动的projectile、owned Burst Active/tail。

最终040547的resident precision-burst在7068/7090造成3→2→1；7115 owned wave第4发令1→0，E0、未持L/未过热。随后明确DEATH RESET，生产恢复先返还HP/E，再到原检查点(1680,3960)。no-reset断言先失败不是存档恢复错误证明。后续需要连贯的混合火力/能量输入策略，不能以门掩码或日志代替真正通过。

## 全部十案及同冻结结果

证据根 `G:/knight/验证证据/october13-stable-claw`；以下时间均20261026，各时间前缀唯一定位完整case目录。所有构建exit0/0警告/0错误；无超时，ExpectedFailure空、guards空、SourceRows1844。

| 时间/阶段 | strict | engine / ms | 保留的结果 |
| --- | --- | --- | --- |
| 031640 Explore | FAIL | 1 / 147990 | ready K局部通过，Steam批次1未完 |
| 032740 Main | FAIL | 1 / 127217 | 原死亡恢复；late96实际拒绝 |
| 033019 Explore | FAIL | 1 / 148993 | 精确离界/波次重开链 |
| 034215 实验K Explore | FAIL | 1 / 138664 | K成功但后来zero-recovery；分支已移除 |
| 035005 arrival hold Explore | FAIL | 1 / 148353 | Sentry过，批次2再次追居民越界 |
| 035740 最终Explore | FAIL | 1 / 146601 | 三批局部无伤通过，出口Rush伤害 |
| 040547 最终Main | FAIL | 1 / 131125 | 同原死亡/no-reset |
| 040828 最终pre | PASS | 0 / 155097 | 完整原前段，ERROR/WARNING0 |
| 041134 最终post | FAIL | 0 / 196459 | 原75及owner8完成，但4Handle ERROR |
| 041524 最终models | PASS | 0 / 407 | 普通模型/恢复构建 |

总计 **2严格PASS / 8FAIL**。根最终批次36408 TERMINAL0只表示四个子进程都结束，不表示通过；035740与最终源清单完全一致，复用为最终Explore而不为记账重复跑同版。

post原75次enter/75次return、索引0–74，owner8的nodes1→1均真实完成。三sink原限定4WARNING与scope精确匹配，但stderr/external各4条`Handle is not initialized`仍严格FAIL，不被stdout完成marker/engine0覆盖。300000ms为原修正工作量预算，不追认180000ms，不是性能认证。

四实际调用站点：`PrologueSectorStreamModelTests.GeneratedLayoutOneSceneKeepsGeometryAndAddsExplicitNeighbors:67`；同法:71经`PrologueSectorDefinition.ValidateSceneRootTransform:121`；`GraveLampMothRuntimeTests.FormalScenePreservesGameplayAndReplaceableVisualContracts:257`；`RealPhysicsLocksSweptPathThroughLandingAndRecovery:34`。只读追踪未找到消费者显式Dispose借用PackedScene/Profile/Shape或字段提前清空的证据，不能据栈猜owner。下一可做单变量诊断控制：只省略前置legacy traversal而完整保留四消费者/后续次序，且严格标为控制，不能充当完整post通过。

## 一次独立审查与根裁决

新鲜上下文审查 `/root/v119_steam_priority_review` 对物理基线1BD3…和最终403D…核对225+/22−：**Critical0/Important0/新增Minor1**。根按实际影响重分级后同意；没有Important修复轮，也没有第二次review。本次不构成整个脏分支、Task15或全目标批准。

新增Deferred Minor：fresh scan替换same-pass facts并增加pass，但输出的门mask和dx/dy仍继承外层计算，缺少明确origin outerpass/physics链接。现有最终96行全outer_rejected，不影响本次证据；未来不能据fresh标签推导这些继承值的因果，修复应标provenance或省略，不能为了日志重求值。

继承三项Deferred Minor仍开：energy-unwind早失败的flush；边界几何“swept”措辞偏强；await失败时finally的`native_handoff=(stage==4)`摘要偏强。合计4项延期，不据它们放宽资格。

以下覆盖审查全部16项Declined；根逐项裁决及错误代价：

| 事项 | 根裁决 | 若错的代价 |
| --- | --- | --- |
| 1 完整Main/Explore | 两条仍FAIL，只保留Steam局部GREEN | 过早把死亡/出口失败交付玩家 |
| 2 Steam后续居民出口战斗 | 下一实际障碍，保留原敌人/通路/生命门 | 把有效敌人误当机关或删敌凑绿 |
| 3 Main较早机会/交还后许可 | 最近96不覆盖早期门，不补造旧掩码 | 对错误窗口实施战术 |
| 4 全战斗付费能量因果 | 缓存E不是已求值门，先尊重实际短路 | 错把耗能当所有拒绝的直接原因 |
| 5 queued/non-owner选择负例 | 静态规则保留，未新增原生反例 | 特殊生命边界仍可能误选 |
| 6 原900物理时长 | 900是原外层预算，保留既有内部await | 外层数被误称固定900物理tick |
| 7 连续碰撞/子步 | priority100仅原生边界采样 | 漏掉帧内交叉而夸大安全 |
| 8 observer成本/硬件 | 不宣称零开销或性能认证 | 真实帧率风险被诊断掩盖 |
| 9 异常Free注入 | 本轮只保留成对清理，未增故障注入 | 特殊清理错误尚未覆盖 |
| 10 W/first-hit | 未改读键/首击授权，不用二次resolver放行 | 遮挡或陈旧预览风险仍待原证据 |
| 11 Intro/延迟身份/LoadLevel双异常 | 原合同保持，未扩大本轮修复 | 未覆盖重入/失败清理仍可出错 |
| 12 已移除Charger K实验 | 仅原始反例保留，不列当前能力修复 | 将一次应用误当有效完整战术 |
| 13 Handle/FATAL/ShapeRID/query | 资源根因仍开，不据quiet或旧query绿关闭 | 正常运行仍有崩溃/泄漏风险 |
| 14 继承三Minor | 继续延期；新增fresh来源Minor另列 | 日志/失败摘要可能过强 |
| 15 人口/BossNPC/地图/正式AV相机/硬件真人/发行 | 原十一项全部保持ACTIVE/NOTSEEN | 丢失批准范围或伪造用户验收 |
| 16 脏更改/UE/真存档/付费/自动化/Git | 本轮不碰，不纳入新资格 | 跨项目损坏、费用或数据副作用 |

附加执行裁决：Steam只改变必做波次的追逐优先级，不要求单场杀完全部76只；居民仍能在出口真实攻击。代价是居民战斗仍须补完整而非被偷偷排除。arrival hold消耗原循环，不增等待池；原生预告/AI仍可使它失败。日志被动、异常保留主异常；不把观察当防御授权。

## 独立保管与冻结身份

`/root/v118_custody_audit` 明确 **v119 audit ENDED**，末只读exec08f013同步exit0，20个记录native均结束，CIM KNIGHT Godot/dotnet/owner均0。所有source/helper/docs冻结覆盖父批次间隙，直到审计ENDED后才写本记录。审计不冒称检查根跨会话handle36408。

- 五对manifest：**50/50逐字节一致**；实际source/helper副本5394份（5290+104）逐份SHA一致，missing/extra/fault0；封存binary60/60一致。
- 当前Source1844/C#892与最终models逐项一致，Debug6/6一致；对v118仅Main测试变化，0新增/删除。11条helper联集各单版本且当前匹配，owner未变。
- 20份已存在user manifest同SHA `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`；只比清单字节，不读真实存档语义。
- source-before/after `851D3EC7014D3763372AF5A7FA15FD92B44B0795242302A2B49941D8AFCEF028`；binary `2C7C7D8544EEAE4F4C6D86FF3E7B15D26497F8970776D80D3E962BE9202541C5`；helper `A1AC76A1CBEEAF2061E4787C7AA4191B2FE6831CCBD6640BC2EE66C2E403B8F1`。
- 当前Main `403D872D6D662322756FD0D6AE43EE0A2284B94B10A82BA6164781FF05403A74`；物理v118基线 `1BD3B68379F69FE4DBC4F437DAAF4EA5D306BEFDEC8151AFAD95569E8EAC4A9E`；owner `886AEA3D4D90054D4C567F8527F15C61DCE12D23573E08286FFFBF3BDE284C70`。
- branch `codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index21610FDC…945C、audioCAD39DF1…EC7A和生产Claw6DAF77B2…F4AE保持原值。没有Git写入/提交/推送。

## 留档与下一实际工作

本轮无新正式图声/游戏图稿/实录、付费生成、前台/OS输入、自动化、删除或真实存档语义读取，资产表无新增正式项。一次手动媒体Check UTC2026-10-25T20:31:52.1789405：Current860284/基线3508/增长856776字节，Notify=false，无新20GiB档位，不停工、不创建独立通知。

下一继续真实Steam出口居民攻防与Main连贯战术，并保留资源控制和相机/正式视听/目标硬件/真人试玩/授权打包GitHub门。所有原稳定条目ID、76普通敌人+Boss/NPC/机关、死亡恢复继续与出口义务完整保留，不加入FTR/取消方向或另一个UE工程；不标Task15或目标完成。

最终原始证据：[Explore日志](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-035740-875-d3ce542184d74a89b7c31fb0ba592804/engine.stdout.log)、[Main日志](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-040547-075-9d16d20d040745caad2a1790e3d111de/engine.stdout.log)、[post来源](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-041134-696-b350ecf3dd8c4915ae959ac9bb7cab80/provenance.json)、[post原始错误](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-041134-696-b350ecf3dd8c4915ae959ac9bb7cab80/engine.stderr.log)、[models来源](G:/knight/验证证据/october13-stable-claw/models-20261026-041524-141-ebd323312b8f4bc1a931abc84d6746a6/provenance.json)。
