# 幽灵炮台：显隐、飞弹撤销与停炮反击 v001

2026-10-17，Asia/Shanghai。开发承载证据，不是正式族系美术、50只入图或完成版本。最终同源码15门 **4 GREEN / 11 FAIL**，完整目标 ACTIVE。

## 事实、推理、风险

- 事实：旧Sentry静默忽略Ghost profile；只有void机械爪fallback，缺最终bool拒绝。先写真实保存场景12案；首轮10失败，加强“坠落前已隐藏”观察后11失败。保存Player的近身K→J首轮已能原生命中。
- 推理：Sentry的K是原0.80秒停炮眩晕，不拉身体，也不清已经发出的弹；不能套用Moth下拉逻辑。Hidden必须分别撤J／K发现与最终接收／AI／旧飞弹，同时保身体、原重力和环境死亡。不能仅靠alpha或让目标不进屏幕伪造成功。
- 风险：所有新增机制专项仍有资源退出告警。旧Upper／BellChain完整回归、历史句柄/FATAL、最终视听／正常双路／60帧与全50实际放置均未闭合，不能据12行为符合宣布正式交付。

本轮继续无窗口；用户此前确认使用其他窗口，解释仅适用于该次失焦，不追认全部输入问题。无OS按键、抢焦点或前台试玩。

## 最小实现与保持项

仅两项生产源改变：`BellBoltSentry.cs`、`ResidentTurretResidence.cs`。新Ghost策略只由Sentry显式接入，其他四枪类不获得虚假的Ghost支持。

Sentry在所有配置验证后提交typed Family/profile和residence。Ghost独占None弱点＋有限两时长[1/60,60]；缺失／混合／其他family非空profile拒绝。共享residence增加可选combat许可回调，默认true；生命Available保持原语义，CombatAvailable才叠加显隐。其他四枪原构造调用不变。

Hidden保保存Hurtbox真实层／监测／Monitorable，原生J确实重叠，再在Health前拒绝。K两个group和TargetCollision层撤销，捕获bool receiver及直接ApplyThreadPull同样拒绝。AI／弹体过滤使用CombatAvailable；原LOS、完整领地、视口、Health及World条件仍在。身体capsule50×68／local(0,-5)、layer/mask和World碰撞未改。

Hide取消原攻击、锁向及真实bolt；旧Hitbox在Retired同步回调内就拒绝再次交付，实际live bolt先跨Recover，再在Hide前未命中时退休。原producer自建两枚CircleShape的退出释放不变，不Dispose借用资源。

sleep暂停剩余显隐时间、不回血；环境pending仍直接请求Health，不走Ghost J门。Reset先回Hidden再应用共享权限，修正原尾部无条件加thread_targets导致隐藏组被重新加回的问题。原死亡通知的重入保护与pending Reset后的VoidPending复查原本正确，不冒称新修了旧生命误杀。

新phase接入后真实复现：最后Manifest tick已接受K，下个Hide取消把尚未消费的`_threadRequested`清掉。最小修正仅给Cancel增加默认false的preserve参数，只在Ghost Hide／活着的Hidden路径保留已接受请求；**赋值在退休／None回调之前提交**，不在回调Reset后恢复旧bool。原模型Stunned计时不扩展，sleep／void等旧调用仍按原规则清请求。

保持HP2、原1.10秒预警／.55锁向／.12发射／1.0收招／.80K、440弹速、半径7、伤害1、最大行程640、原重力及固定身体。原保存Sentry无敌期0；void pending案明确使用测试1.2秒保护期，不改保存资源。无状态／HP／位置注入、无新增GC/cache/KeepAlive/借用Dispose/额外drain、无日志豁免。

## Native先失败再实现

证据根 `E:/OurGame/Knight/.evidence/october13-stable-claw/`，全部有fresh构建、contract/wait、三路原始日志和五组保管链。

| 目录 | native实际结果 |
| --- | --- |
| resident-family-ghost-sentry-20261017-013456-863-f40ee8f39c5f4c6da02b34bac5615763 | 12案10失败；engine1/10431ms；6 Shape RID |
| resident-family-ghost-sentry-20261017-013743-139-9f5caf286ef14e568472d9f0e10149ec | 仅测试加强Hidden-before-void，12案11失败；engine1/9832ms；6 RID |
| resident-family-ghost-sentry-20261017-013936-043-400742c4fd16435c8bbb678f92434974 | 最小phase接入后12案2失败；engine1/15513ms；6 RID |
| resident-family-ghost-sentry-20261017-014132-400-e8bcedf879bf4e70848640ca73836b82 | 只修观察时点，12案1失败；engine1/15649ms；6 RID |
| resident-family-ghost-sentry-20261017-014315-508-9122e7db7f8146cf8f9b09537e1dcd10 | 只修pending-K保留，12行为符合；engine0/16925ms；6 RID严格FAIL |

两项必须区分：

1. 退休测试起初在Retired回调内读CurrentFrame.Recover，但生产Cancel先把模型切Idle再退休，故观察位置错误。改为**Hide前实际观察“Recover且bolt仍活”**，保留退休、captured拒绝、旧弹销毁／空列表／None／玩家HP9全部断言。013936→014132生产清单未变；真实输出retired=True/captured_denied=True/bolt_gone=True/live0/None/HP9，不声称修了原本正确的退休代码。
2. 014132→014315全部测试清单不变，仅Sentry pending-K参数／调用／赋值顺序改变，真实接受K跨Hide后进入原Stunned。不是延长眩晕或增加拉力。

首轮warning正控的计时失败：profile被忽略时，原攻击已经在Mount6帧中开始，观察过晚；不能据此称原1.10秒预警损坏。phase接入后从自然新Manifest首帧观察，完整预警／真实bolt伤害成立。

保存Player案：原fixture为1024×640、floor329、Sentry home320/300；近身Player380/293，真实A短脉冲确立左向、K→独立水平J。K1/J1/native entry1/Connected1/HP2→1/direction0在baseline已通过，未增加J距离或把K描述成拉近。ParseInputEvent物理键码不是OS真人来源。

12案覆盖真实隐藏J重叠／最终K拒绝／身体World碰撞、自然显形同HP J/K、原完整预警和实际弹体伤害、锁定预警跨Hide新ID、飞行弹跨Recover后Hide同步退休、已接受pending K、sleep剩余phase暂停、原生致死None回调Reset与第二死亡／双组撤销、配置原子拒绝、坏设置／跨World／queued及freed来源／无Health合法源、独立观察Hidden后自然重力扫void／删源sleep一次死亡、保存Player K/J、pending None重入Reset和Hide None重入Reset。

开发probe时间（如.6Hidden／1.25Manifest）只为保原1.10预警后留实际飞行到Hide的窗口，不是最终平衡批准。未证明long-hitch、跨sector phase持久化、最终美术或ALL50活动。

5探测产物60次teardown均native gone，自建Shapes147/147释放、borrowed0；25保管链对一致。数量不能识别匿名6 RID，也不证明资源问题已解决。

## 最终同源码15门

| 目录 | engine ms / exit | 严格结果 |
| --- | --- | --- |
| resident-family-ghost-sentry-20261017-014315-508-9122e7db7f8146cf8f9b09537e1dcd10 | 16925 / 0 | 12/0行为；stdout6 RID，FAIL |
| resident-turret-20261017-014434-161-6e9de83c582048aebd96068a0de37513 | 25116 / 0 | 原15/0；stdout15 RID，FAIL |
| resident-turret-sleep-20261017-014528-028-9565de6a2e9340a9ac24e1c016f1cfb7 | 20840 / 0 | 原5/0；stdout15 RID，FAIL |
| resident-turret-boundaries-20261017-014627-830-d39acee7e5a34a05b64ba55e597a53c5 | 11893 / 0 | 原7/0；stdout15 RID，FAIL |
| resident-turret-configuration-20261017-014709-302-864397d636224919b20fbe12f6dee7c4 | 9484 / 0 | 原5/0；stdout15 RID，FAIL |
| resident-turret-perception-20261017-014747-699-b99bc0c609fa4c2d9e903dc322a339f2 | 33461 / 0 | 原10/0；stdout7 RID，FAIL |
| resident-turret-void-20261017-014848-479-d004841f864f4d459c2bf23bb70ab9cf | 11003 / 0 | 原10/0；stdout15 RID，FAIL |
| resident-turret-void-pending-20261017-014929-906-b1685b195c1f4411a17137327f5deaf9 | 14402 / 0 | 原5/0；stdout15 RID，FAIL |
| resident-sentry-death-20261017-015011-555-91a987377928426a8ce01c792cacab1c | 2921 / 0 | 原1/0；stdout3 RID，FAIL |
| resident-sentry-default-20261017-015043-360-667f7a07c3a54974a6ddd52408956cc8 | 10856 / 0 | 原完整model/runtime综合，GREEN |
| resident-allocation-20261017-015124-177-e4317b6e41d24d51ae1b61b11426f3f6 | 496 / 0 | 17同检查器负控，GREEN；仅名册／未就绪状态 |
| keyboard-20261017-015152-874-e5a96e45c3d9435bb59f4c182adcfcd0 | 9675 / 0 | GREEN |
| actions-20261017-015234-997-c8c8f1fcbbb74ec3ba231e1865c71053 | 7064 / 0 | GREEN |
| prologue-pre-20261017-015308-886-f3ac8389548d47a9af409217607f9c7c | 88285 / 1 | UpperPendulum/lower_to_mid_swing sample.5错台、final264.99634/160.6107、149/150；两PresentationProfile警告，FAIL |
| prologue-post-20261017-015505-045-70cf6caa95cd4bec808180a836be9db8 | 8479 / 1 | BellChain WaitForFloor540.0704/1196.0139；一条六级祖先ILevelRecovery错误，FAIL |

新12＋旧58共70行为符合，不等于70项严格GREEN。九专项仅stdout存在RID退出错误，external通过标记不覆盖它；完整pre/post原始错误不豁免。本轮Supply返回且无Handle/FATAL，不关闭历史崩溃；旧Skitter默认资源故障未复测。

全部ordinary fresh Rebuild0W0E，无超时／guard失败；build cap180s、专项55s、pre/post180s。无expectedFailure、movie、fixedFPS、verbose或type-identity诊断。串行外层exit0不等于全绿。

## 保管链与源版本

最终15案 source/resource1670（不是C#文件数）、userdata109只hash不读写语义、五组前后清单75对一致；binary6、helpers通常9/pre10。全部消费者终态并确认owned native0后才更新文档。

| 项 | SHA256 |
| --- | --- |
| source inventory | 7DD62294F09E530E481ACCFD90E7C9A8659B1976B6FCE9479F83B91C3B3EF649 |
| binary manifest6 | A738FBBEE996D7D4D409994C483503BD2DD5B29DFDC3B8149E67EFB20AB3E0FD |
| 通常helper9 | 7A75D4E1312CCF3E072ED1DEF40151C294AE079CB335036775EB18D8C2C2E28C |
| pre helper10 | C2F7FB7C3094A130E6C0BEFBBC2A98DC9E0E455F7305D3C6876BA38658049D24 |
| userdata109 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| BellBoltSentry.cs | 75BB49F366DDBE56C3773AB1F0F25BFC0A368495CE40FC34D391B002E8C0B44B |
| ResidentTurretResidence.cs | 345ACC5C69AE27CCE57B72DEFE219894FAE084C52A32232628A0E0B21BA48627 |
| ResidentTurretRuntimeTests.cs | F665ECB61B4A2500BD6F43175305A37B1D89401188DA54F48FAC80DCD955D0B6 |
| runner | CEBD63DB125B2DEF5ABC44D1EF6B8EAAB94B0223A4953E88F64F07A48D599B40 |
| canonical owner | 3FFF41CCCB050FEDB6E355B4FA137A39A50E97FEB50217547A502AF141EC881D |

前轮GhostAir报告SHA0794065E7229F783FF9592506FCFEFFD2A1B4C5AA1E71620AB7C622437A5E58A原件保留。

## 未完成与下一实际入口

正式新增仍0/50、全50 Placement=null、24子型SceneState.Unimplemented/ScenePath=null；旧26+新50=76普通+1Boss、Bone10/Ghost10、ECHO1persona／独立友方身体0完整目标不减。八个Bone/Ghost开发机制承载已具规则，不等于24正式资源或全50活动验证。Task1最终50／整个Task2／Task3-4均OPEN；资源／旧档／正常Main+Explore同角色标题至Boss与有支撑出口／呈现60帧／最终视听／真人／审阅归档录像均待验收。

下一推进24子型的真实builder场景身份／profile绑定与验证，再ALL50完整身体、活动／攻击／返航／玩家接近点位及world-lifetime manager。不能仅改名称／SceneState、给ProbeScenePath换标签就宣称正式身份。

只读准备入口：`CanyonResidentCatalog.Subtypes`规范24绑定；`ResidentSubtypeDefinition`区分Probe和ScenePath；当前ValidateSceneReadiness即使Implemented也只返回native_behavior_proof_required，尚无正式ScenePath加载消费者。使用`SceneBuildUtil.PackAndSave`叶构建（Validated版本只准PrologueLevel）；已有Rust/Moth/Laborer用真实PresentationProfile与IEntityVisual2D，四枪`BuildRangedWeaponEnemies.CreateActor(role)`。需独立保存／重载检查／实际profile引用及身体身份验证；允许复用actor类／renderer不等于假装24份新美术。视觉未正式完成时须诚实保留placeholder／未验收，不借绑定测试关闭美术门。

本轮无新地图、美术／音效生成、图稿／视频、付费、Git写入、上传、自动化、安装、删除或前台试玩。保留完整目标ACTIVE，不触发task-done／全计划完成审查，不无变化重跑身份／名册或资源计数代替制作。
