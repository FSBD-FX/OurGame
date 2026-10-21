# 常驻冲锋敌人与真实区域归属验证 v001

2026-10-16。开发记录，不是发行版本、最终美术或完整试玩验收。工作目录：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。当前源码库存1663，受保护用户数据清单109项。

## 事实

用户已确认，刚才自动探索测试期间正在操作其他窗口。这与记录中的前台切换、按键清空一致；本轮只进行无窗口测试，没有抢焦点、补键或重新运行可见路线。更早失败录像的原因不能由此倒推。

本轮有两项实质增量：

1. 真实五区流送世界的边界来源检查。使用保存场景的五份Definitions、真实Ready/registry/公开RestoreCheckpoint与退休释放流程；验证跨分区有效来源、暂存状态、未注册同名实例、卸载与同ID替换、不同逻辑/物理世界隔离。十项严格通过。固定查询探针不算NPC运动、合法出生点或玩家路线。
2. `CanyonSkitter`新增可选常驻冲锋适配器。配置后才能巡游、领地感知、受牵引后返回、休眠与唤醒；未配置的旧波次仍走原策略。新增行为开发的15项原生断言都符合预期，但退出有4个Shape2D RID泄漏，整个专项仍严格FAIL。

实际运动记录：巡游中心范围257.59857–382.40143，单物理步最大0.8000183px；一次公共牵引接口调用仍限速600px/s，实际移动到x471.46738后经过出生位置返回，保留受伤后的HP1。没有在挂载后写怪物位置来伪造返回。这里调用真实牵引接收接口，不冒充物理键盘K的全链试玩，也不是呈现帧率证据。

巡游后的冲锋后退使用本次战斗起点，不再错误依赖最初出生点。完整脚底支撑检查能拒绝中间1px缺口和24px窄支撑；同一地板物理体上的墙仍阻挡自主移动。普通392px下落到合法下层地板不会触发常驻怪物“出生高度+240”假死；真实void走Health与一次Defeated，不调用玩家复活。

补查确实发现并修正了一个行为缺口：无敌期拒绝致死伤害时，旧战斗模型仍会自主Retreat。现在待致死状态撤销自主动作、攻击表现、直接伤害和抓取目标权限；保留已有外力、重力与Health计时。待致死经过休眠、唤醒及危险来源销毁仍会在无敌期结束后接受一次真实Health死亡。此处只证明来源销毁，不是未来人口管理器的跨Actor卸载状态保存。

## 验证记录

均通过同一canonical owner、全新Rebuild、实际1663库存和原始三日志检查。没有命名失败豁免、扩大时间上限、关闭Ready、借用资源Dispose、缓存模式修改或新增GC/退出排空措施。

证据根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw`。下列目录各自保存源码/helper/二进制/用户数据前后清单、构建记录、三个原始日志和provenance。

| 检查 | 最终目录 | 实际引擎结果 | 严格结论 |
|---|---|---|---|
| 常驻冲锋15项 | resident-charger-20261016-125026-098-d526bc6f8444407c850329211262f25f | exit0，39991ms；15断言符合，stdout退出4Shape RID | FAIL，不能验收此样段 |
| 旧冲锋完整模型/运行综合 | resident-charger-default-20261016-125306-158-00aa31e3c51f4f35a5dd0a8a89ac3822 | exit0，38791ms；三日志无诊断 | GREEN，仅未配置旧行为 |
| 真实流送来源10项 | resident-void-stream-20261016-125807-334-2a63567a0aa545b9ad2dd9ab8b5cc0a0 | exit0，1331ms；三日志无诊断 | GREEN，非出生/运动认证 |
| 当前地图几何普查 | resident-geometry-20261016-125836-583-6aadf88130c148438d7d9f951a45d0fe | exit0，24695ms | GREEN；41候选/9未解，非50认证 |
| 当前人口名册 | resident-allocation-20261016-125929-582-5a16dd1fc0e94ecfa0edcd1ef5de6fe3 | exit0，498ms；17负控 | GREEN，仅配置合同 |
| 完整pre | prologue-pre-20261016-125518-269-108b7bec1c5c4a6cbde470dedf97d5fa | exit1，89614ms | FAIL：旧UpperPendulum/lower_to_mid_swing错台；两条刻意profile警告也保留 |
| 完整post | prologue-post-20261016-125724-572-b9b7531532644171a06a067d344ae569 | exit1，8719ms | FAIL：旧BellChain地板起点540.0704,1196.0139；KillZone无法解析ILevelRecovery |

上述每案guards=0、未超时，五组前后清单相同，真实用户数据109项未变。旧Rust巡游/void、键盘与动作专项最近一次独立检查是1660阶段，本轮不把它们改称1663新跑。

反向证据也保留：

- 流送检查123621-f0040bf1只故意去掉Actor所属分区准入，暂存Actor与未注册同名Actor两项准确失败，其余八项符合。随后精确恢复helper SHA，1661阶段123733和当前1663阶段125807均严格通过。
- 冲锋首次124136-556b86a9证明缺少常驻接口；十项在配置前拒绝，不能算十项已跑运动反例。它同时有4RID，因此不是干净RED。
- 124803-d9c233aa的15项中只有无敌待致死后的自主后退失败；修正后125026十五项行为均符合，但4RID仍在，未降低严格门槛。
- 初始流送122400-f0f62788的10项行为均符合，却有187Shape RID。改从PackedScene.SceneState读取真实Definitions、避免仅为读资料实例化整个关卡后，流送专项干净退出；未逐RID定位原187，也不能称生产游戏资源问题已修复。

## 推理与取舍

应先让实际敌人产生真实身体、冲锋和返回包络，再认证最终点位。当前两种地面适配器是这个顺序的开发进展，不以数据表或静态身体探针替代全部50只的运动检查。退回旧出生点会使“巡游后再攻击”失去真实后退，所以必须区分战斗起点和长期领地归位点。

资源告警与行为断言分别记录。未配置的完整旧冲锋综合无诊断，并不足以解释配置夹具的4RID；没有证据就不能把它归因于某四个借用shape，更不能Dispose借用缓存资源来消红。

## 风险与下一步

- 正式新增敌人仍0，全部50个正式Placement仍null，24个新增族系子型ScenePath仍未实现；正式版仍原26普通名额＋Boss。骨骸10、幽灵10等本关要求不减。
- 41个活动候选不是可直接出生的位置，也不是地图容量上限；联合搜索有上限，9个未解，玩家关键交互保留仍100项。当前地图/小地图及数量未改。
- 当前脚底策略只认证轴对齐静态矩形顶面；不能外推旋转、单向平台、活动平台、飞行/俯冲、弹体或所有边缘。每步原生查询与50只同时活跃的成本未测。
- 15项没有认证普通伤害死亡后的全部抓取组/layer撤销，也没有认证待致死Actor卸载后的管理器状态继承；下轮需要真实死亡/旧实例/新实例负控，不能把void死亡与来源销毁检查外推到这些边界。
- 配置冲锋夹具退出4RID、历史资源/FATAL、旧完整pre/post仍OPEN。下一轮优先做可区分的资源所有权定位，同时继续真实飞行/俯冲等生产适配；不重复空名册、不把ProbeScene当正式骨骸/幽灵。
- 之后必须完成全部50的实际族系包络与点位认证、人口流送/休眠/击杀状态管理，再验证正式主支路、旧档、Boss及有支撑出口、呈现60FPS和真人反馈。
- 完整目标ACTIVE；本轮没有新美术、媒体、付费、发布、Git写入、自动化恢复、安装或用户存档语义改写。所有消费进程已结束后才更新这些文档。

## 核心源码身份

| 文件 | SHA256 |
|---|---|
| scripts/enemies/skitter/CanyonSkitter.cs | 850C41019C6B3236BB6D293CE464C3AB1973C5F26F6837507B92066CDC662616 |
| scripts/enemies/skitter/CanyonSkitterResidence.cs | 604A06D0232AC388CA988046AB5996E89CFA41100AA814FE04F01492E50B9CB8 |
| tests/integration/ResidentChargerRuntimeTests.cs | 4EE98BE3BC1ECC5EA134AF9CFD08D63762373A3FA666BE9B07D17F70B6EA06A4 |
| tests/integration/ResidentVoidStreamRuntimeTests.cs | 27D09EC19352BFB603A954C4D545194DEE4559637EE96B47631C7DFBB1BD0C50 |
| scripts/enemies/residents/ResidentVoidContact.cs | BF7B70B7D52D1C044E9B7551E351836E1E164A4B76BBF015022B2BB12D1CD441 |

实际命令：固定pwsh运行`.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 -Phase <表中阶段> -SourceRows 1663`；默认与新增冲锋分开运行，focused55s、pre/post180s门槛不变。底层固定dotnet执行`-t:Rebuild --no-restore -m:1 -p:UseSharedCompilation=false`，不复用旧二进制冒充验证。
