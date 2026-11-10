# v120 · 蒸汽区出口真实下向格挡

2026-10-26；原 Task15／完整十一项继续 **ACTIVE**。本轮分类 **PROGRESS**：原失败的蒸汽区出口已由合法纯键盘动作实际无伤通过，并确定后续抓取与幽灵伤害的实际来源。不是整条路线、生产战斗平衡、正式视听或交付完成；用户最后窗口反馈仍 **NOTSEEN**。

## 实际改动与原生 RED → 局部 GREEN

仅 `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`，对物理 v119 基线 **106新增／2删除**。没有生产代码、地图、AI、生命、能量、机械爪排名/LOS、公共helper、正式图声改动，Source1844/C#892、0文件新增/删除。

原 RED 是 v119 035740：central-control已实际Idle、HP2，出口 `walk-4170` 第2/112在physics8121被常驻Charger的RushHitbox从左下方击中，生命2→1。实际来袭向量约(-.723298,+.690536)，Down点积.690536满足盾的cos75°门，Right与DownRight分别-.7233/-.0232，不应加D+L就假定能挡。

本轮仅最后键盘 `Walk(4170)` 开启 `guardRearApproach`：

- 在原机关K已完整Latched→Released→Retract→Idle、观察操作结束后，发送真实S+L、A/D/W/K释放。不在K期间开L，不写reader/controller/pose。
- 最多11个原 `WalkTick` 等待真实零伤 Normal `ShieldResolved`，并与真实同世界存活 saved CanyonSkitter 的 `RushHitbox.Connected(playerHurtbox)`关联，消费新transition一次；禁止由任意历史格挡事件授资格。使用真实同步Receive→Shield→Connected链，不调用TryConnect、不增加危险或resolver查询。
- callback异常计fault，成功必须0；无接触在原cap失败，不超时后盲跑。这里是限定正接触场景的测试策略，不是限制生产玩家无敌人时通行。
- finally成对释放S/L并移除来源/盾监听，外层也释放；再1个原WalkTick证实S/L真实释放、盾关闭、reader新序列/axis0，才恢复原D、制动、微调和停稳。
- 整个前缀不超过原12个准备tick，仍在原动态 **112** 总预算中；原HP、完整实体地板支撑、零恢复、普通跳/冲次数和central-control120全部保留。其他Walk与legacy默认不变。

044137实际结果：**physics8121/transition56、actor797655962026/hitbox797756625342、contact1/fault0**；前缀含释放共3/112，玩家保持(4001.676,4199.932)、V0、HP2→2。随后原行走在 **53/112** 到(4170.5117,4199.932)，V0、完整SteamTrenchFloor支撑、jumps12→12/dashes0→0，原机关应用一次/Right真正关闭，Steam出口阶段通过。

这是自动真实键盘输入的通路证明，不是玩家首读可理解性或鼠标替代输入，也不是引擎层新增防御能力。

## 后继失败原样保留

整Explore仍 **FAIL**，engine1/155825ms：原 `Task4ReachSupport` 的同一次 `ResolveKeyboard` 在LiftBackLanding选择了其他接收体，原Traversal1621正确拒绝。expected6154067383051为LiftBackLandingGrip；actual6154218378004的profile是 `CanyonAscent/Gameplay/Collision/AscentGrip2/GrappleSurfaceProfile`，anchor(4671.914,3120)、distance586.26514<620。没有第二次解析、强制目标或改排名放行。

原SteamRejoinTop已到(5054.8467,3563.925)、V0、完整40×72胶囊和原生Top接触、20次jump/0dash。原NW轴在post右缘x4716时y3225.078，高于post顶3290；另一岩壁偏轴约4.21875°，因此不是已证实的K丢失或排名缺陷。

**不能扩大Steam无伤为全程无伤**：出口8172之后恰有一条accepted damage：physics8546/input8517，HP2→1，`canyon.v2.resident.counterweight-lift.03`、`ghost-diver.tscn/AttackHitbox`，actor6170710378975/source6170794265060/attack1，冲量(-.81142247,179.99817)。发生在Step8返回前，Top只证明HP1→1；当时盾未持有/未激活，E1/heat0。不是跌落，不能补血或忽略这个敌人。

Main本轮未改战术，最终044620 engine1/130170ms，仍是同一实际混合火力耗尽后死亡/恢复触发原no-reset。末96条physics7019–7114全outer_rejected，mask3/1/2/16380，maxSafe.2833333、缓存E9.066517→0；不得把未执行的后续能量门写成每次直接拒绝原因。死亡前resident Burst7067/7089伤害，wave Burst7114致死，随后明确DEATH RESET；未改存档恢复语义。

## 同冻结五案

证据根 `G:/knight/验证证据/october13-stable-claw`，日期均20261026。root首Explore handle82430 TERMINAL1；最终四阶段父72070 TERMINAL0，仅表示所有子调用结束。

| 时间/阶段 | strict | engine / ms | 实际结果 |
| --- | --- | --- | --- |
| 044137 Explore | FAIL | 1 / 155825 | Steam出口局部绿；后续错误接收体 |
| 044620 Main | FAIL | 1 / 130170 | 原战斗死亡恢复/no-reset |
| 044859 pre | PASS | 0 / 154361 | 完整原前段、0 ERROR/WARNING |
| 045202 post | FAIL | 0 / 195600 | 原75/owner8完成，但4Handle ERROR |
| 045554 models | PASS | 0 / 496 | 普通模型/恢复构建 |

**2严格PASS／3FAIL**，五次构建0警告/0错误、无超时、ExpectedFailure空、guards0、SourceRows1844，三sink无FATAL。Explore/Main错误计数stdout/stderr/external分别0/1/1；post为0/4/4，WARNING0/4/4。post原75项各一次enter/return、owner8次nodes1→1和原WARNING/scope多重集全部匹配，但4条`Handle is not initialized`仍严格FAIL。既有300000ms工作量预算不变，不追认旧180000ms或当作目标硬件性能。

## 一次独立审查、根裁决与代价

`/root/v120_steam_exit_review` 新鲜上下文审查物理403D…→9F2C…，106+/2−：**Critical0／Important0／新增Minor0**。根依实际用户影响复核后同意，只保留限定测试增量，不批准整个脏分支、Task15或交付；无修复轮/第二次review。

四项继承Deferred Minor继续：energy-unwind早失败flush、边界几何swept措辞、finally `stage==4` handoff摘要、fresh late事实继承outer mask/dxdy而缺origin标签。本轮未顺带修它们，不用于放宽资格。

以下逐项覆盖审查全部17条Declined，根裁决和错误代价：

| 事项 | 根裁决 | 若错的代价 |
| --- | --- | --- |
| 1 完整Main/Explore/Task15/十一项 | 两路均FAIL，原范围ACTIVE | 把真实卡点交给玩家冒作完成 |
| 2 LiftBackLanding接收体 | 保留原拒绝；下一以真实站位验证，不改排名/强放行 | 抓到错误地形或遗漏真实遮挡 |
| 3 Main连贯战术/能量 | 本轮不解释全程，只保留真实短路/死亡 | 对错误相位调节攻防 |
| 4 无Rush的通用通路 | 本场景要求正接触；无接触会假阴性，非生产通行限制 | 将测试策略局限误作游戏障碍 |
| 5 精确.03/全部子型 | 只声明真实saved同世界NormalCharger接触，记录实际ID | 泛化到未测子型/错称稳定身份 |
| 6 恶意重入/伪造事件 | 依已核真实同步链，不称通用事件安全协议 | 未覆盖的重入错误归属 |
| 7 Free/卸载/释放故障注入 | 保留正常finally，不宣称多重故障验证 | 特殊清理失败仍可能遮蔽原错 |
| 8 连续子步/并发全部源 | 一次真实连接，不称连续或全源碰撞证明 | 漏掉其他帧内危险 |
| 9 扫描/监听/硬件成本 | 明确新增一次来源收集，未测零开销/FPS | 目标硬件帧率风险未关闭 |
| 10 W预览/first-hit全面复验 | 原机制未改，不由S/L局部绿续签 | 陈旧预览/遮挡问题仍可存在 |
| 11 Intro/延迟退出/LoadLevel | 原合同保留，本轮不扩修 | 未覆盖准备/重入/双异常 |
| 12 Handle/FATAL/ShapeRID | 生产资源根因仍开，post错误严格保留 | 正常运行仍可能崩溃/泄漏 |
| 13 四项旧Minor | 全部继续Deferred | 诊断/失败摘要精度尚不足 |
| 14 76普通/BossNPC/地图机关/正式AV相机 | 完整原十一项仍须逐项同版验证 | 丢失批准内容或冒称美术已完成 |
| 15 真人首读/反应/审美 | NOTSEEN不变，脚本动作不是玩家上手证据 | 未被玩家理解的路径被误验收 |
| 16 重复性/最终回归/保管 | 已补同冻5案和独立审计；不称重复运行认证 | 一次局部结果被扩大为稳定完整路线 |
| 17 脏分支/Git发行/UE/真存档/付费/自动化 | 不碰/不纳新资格，原授权边界不变 | 跨项目、数据或费用副作用 |

执行Ruling：保留原12准备/112总期内的后手防御，不另加等待池或删敌；代价是实际来袭、输入延迟或支撑改变仍须失败。Ruling：只使用真实同步Guard→Connected的来源，不把Snapshot伪造为ActorInstance；代价是异常重入/别种命中仍未全面覆盖。Ruling：复用同源已结束Explore作为最终Explore，不重复凑绿；代价是不认证独立重复性。Ruling：只按物理基线归因本轮单文件，不把整个脏checkout都列为已审查；代价是历史改动继续保留原门。

## 独立保管审计与身份

`/root/v118_custody_audit` 明确 **v120 audit ENDED**，最后只读命令31417c同步exit0，无运行handle/子会话；10个记录native进程结束，CIM KNIGHT Godot/dotnet/owner各0。roothandle82430/72070由root实际终态证明，审计未冒称跨会话检查。所有source/helper/docs直到全部rootnative与审计结束才解冻。

- 五对manifest **25/25完整逐字节一致**；实际副本2574份（source2520/helper54）逐份SHA一致，missing/extra/fault0；sealed binary30/30一致。
- 五案source清单相同；当前Source1844/C#892及Debug6/6逐项匹配最终models。对物理v119仅Main测试106+/2−，0文件新增/删除。
- helper联集11路径各单版本且当前匹配，canonical owner `886AEA3D4D90054D4C567F8527F15C61DCE12D23573E08286FFFBF3BDE284C70`不变。branch/HEAD/index/audio/生产Claw均同原锁定值。
- 10份既有user manifest SHA `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`，未读取真实存档内容。

最终source `7F392B3C0EFE0DA77E6DCAC235E10F39DF392A12A098A86C67F6831EF75D6EAA`；binary `692CDDCB1DAD50BB52435BB8DA62BD5F5AD78DCFE75303274C0C11D7536D71BC`；helper `A1AC76A1CBEEAF2061E4787C7AA4191B2FE6831CCBD6640BC2EE66C2E403B8F1`；Main `9F2CADAB6F7BB13CE1A2F9934DB6BDE71237FF834C168CDD69B26AC1BA37740D`。

## 下一实际候选（尚未实现或验收）

1. Top站位：依据原post右面4716、原aimY3300、实测Y3563.925，解析x=4979.925，左移约74.922px；完整胶囊落在Top内，保守几何候选区间4971.925…4985.925。把真实A接近/释放/停稳计入原Top300共享期，保留完整支撑/HP1/动作数与原Landing同次解析/K/落地。**几何不证明LOS、实际Reel或避开ghost-diver**；不能补血、增跳或另加等待池。
2. Main调度：生产和原同owned-Burst真实Shot后致死J义务不要求K早于Shot；当前v106早窗子目标把眩晕浪费在Shot门开启前。可明确修订调度为实际首Shot后唯一K，再同cast/application+1/真实Stunned/handoff和完整J窗口；早期机会仍建立一次K欠账、终态不能静默跳过。须TDD证明Shot到来不再误到期、漏/重复K/handoff/abort仍拒绝，保持原900/jet/全源危险/能量/HP/零恢复；不能直接删门或加第二K。当前不足以保证成功，延后jet也改变相位/位置。
3. 资源：现成canonical无省略prefix开关；最小一变量legacy traversal控制须仅测试隔离环境arm、实际runner明确CONTROL收据、禁止普通post完成marker。即使没有Handle也不是完整post通过，更不是资源修复；还未实现。

v119补充校正：不同native案例物理帧不混用。最终040547末96是7020–7115、缓存E9.066517→0；032740是另一诊断。v119报告已按最终040547明确更新该段，原raw不改。

## 媒体与完整交付边界

没有新增正式图声或游戏图稿/实录，没有前台/OS键鼠、付费、自动化、Git写入/提交/推送、删除或真实存档语义读取。资产表无新增正式项。本目标轮仅一次手动媒体Check：UTC2026-10-25T20:31:52.1789405，Current860284/基线3508/增长856776字节，Notify=false；此后无新过程媒体，不达新20GiB档位，不停工或创建独立通知。

全部原稳定ID和十一项保持：移动短喷、连续多层地图、76普通+Boss/NPC、机关、纯键盘/收集/存档流程、引导AI、头灯迷雾小地图、正式分层美术动作、UI/设置/音频/批准演出、稳定性能、资源文档版本交付。正常同版双路、死亡继续/出口、正式AV正常速度审阅、相机、目标硬件、真人键盘及授权打包GitHub仍待真实证明；FTR/取消/另一个UE不扩入。不标Task15或目标完成。

原始证据：[Explore日志](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-044137-999-ef22cf8611f544bf9d19b98c39476d9f/engine.stdout.log)、[Explore错误](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-044137-999-ef22cf8611f544bf9d19b98c39476d9f/engine.stderr.log)、[Main日志](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-044620-398-fd4b668bcd444844aba3eac76de8dfd3/engine.stdout.log)、[post来源](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-045202-190-67de8f77b7d54db091ad68325160e478/provenance.json)、[post错误](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-045202-190-67de8f77b7d54db091ad68325160e478/engine.stderr.log)、[models来源](G:/knight/验证证据/october13-stable-claw/models-20261026-045554-419-f00c13dca4f241a19e544cb903153a66/provenance.json)。
