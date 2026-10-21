# 幽灵飞行／俯冲：显隐权限与真实生命周期 v001

2026-10-17，Asia/Shanghai。开发机制证据，不是正式身份或全50入图。最终同源码12门严格结果 **4 GREEN / 8 FAIL**，完整目标 ACTIVE。

## 事实、推理与风险

- 事实：用户再次确认完整探索测试时在操作其他窗口。该次失焦清键与回答一致，但不能据此排除全部输入问题。本轮只运行无窗口引擎测试，不抢焦点、补系统按键或重跑前台探索。
- 事实：原 Moth 拒绝 Ghost 非空配置；测试先行后 core6全失败、boundary10全失败。多数止于配置，因此不证明原物理或void本身损坏。新显隐接入后 core6符合；boundary仅两条同栈Reset失败，其余8条已符合，包括两角色保存Player的K→J。
- 推理：隐藏不能仅靠透明度或移除碰撞实现。分别控制J最终受击、K发现和最终投递、AI及旧攻击；保真实身体、已接受外力、重力和环境死亡。同步取消回调可能Reset，旧调用尾部必须识别生命已更换。
- 风险：资源退出告警、历史句柄/FATAL、整关旧回归与最终视觉验收均未闭合。头灯／迷雾／正式族系美术、全50放置及manager不由这16项证明。

## 实现与保持项

仅两项生产源改变：`scripts/enemies/moth/GraveLampMothResidence.cs` 与 main的Reset入口。完整1670清单比较确认其他生产源／场景／模型／设置未变；测试、runner、owner在首轮基线前已写。

配置验证后才提交typed Family。Ghost Flyer/Diver必须独占BoneWeakness.None、有限hidden/manifest秒数各[1/60,60]；Bone仍独占Diver/policy2/零Ghost秒数，不因profile非空误入幽灵时钟。

Hidden保真实Hurtbox层／Monitorable，原生J确实重叠但在Health前拒绝；撤K两组与TargetCollision层，捕获bool receiver及直接legacy pull同样拒绝。AI与outgoing hitbox使用同一权限。身体层／mask／circle／World碰撞不改。显形允许同一Health正常J与K。

Hide边沿Cancel旧攻击／锁向／旧ID；保原ThreadStunned、尚未消费的K请求、线程速度与伤害外力。隐藏不提前退出物理／void sweep。sleep暂停剩余phase，不回血；显式Reset从Hidden开始，死亡不被自然显形复活。alpha .3仅开发提示，不是正式美术或截图验收。

两条实际重入反例：

1. Manifest接受K时，Cancel同步回调Reset；旧尾部仍写拉力并报告true。
2. 已真实触碰void且Health仍无敌时，下一次pending Cancel回调Reset；旧尾部以新满HP继续致死。

最小修正：常驻Reset入口增加lifeVersion；捕获旧版本，在同步Cancel后重新核对当前生命。private Pull返回实际bool；pending死亡与phase/physics旧栈检测新生命后撤回。默认未配置波次不增加lifeVersion行为。原K正控和真实环境死亡正控保留。

HP2、原.70s预警/.35s攻击/.70s收招/.75sK、默认540拉力／常驻600上限、重力衰减、Flyer短侧袭96px与Diver下冲不改。无模型状态／HP／位置注入，未改Player输入、锥角或攻击时间；没有新GC/cache/KeepAlive/借用Dispose/额外drain／资源日志豁免。

## 先失败再实现

证据根 `E:/OurGame/Knight/.evidence/october13-stable-claw/`；每案有fresh构建、native contract/wait、三路原始日志和五组保管链。

| 目录 | native实际结果 |
| --- | --- |
| resident-family-ghost-air-20261017-010307-836-83e82b8bb4c445748c8ddfb127babb12 | engine1/814ms，6案6失败，4 Shape RID |
| resident-family-ghost-air-boundaries-20261017-010359-120-6249249f1e644b6a949ebf6d16e216a6 | engine1/893ms，10案10失败，7 Shape RID |
| resident-family-ghost-air-20261017-010525-903-7b97161997cf4de491e7493d069d18e6 | 最小显隐后engine0/10617ms，6行为符合，但4 RID严格FAIL |
| resident-family-ghost-air-boundaries-20261017-010641-353-6d341e2f68b240fea2f584cc196bfc2f | engine1/20730ms，10案仅上述2条重入失败，7 RID |
| resident-family-ghost-air-boundaries-20261017-010914-929-ef620bf954fe418b8d9b9e75e0b7b5a9 | lifeVersion修正后engine0/20822ms，10行为符合，但7 RID严格FAIL |

010641→010914全部453条tests清单hash未变，未削断言。保存Player两角色真实物理键码W-Up/K1→Up-J1、native entry1/Connected1/HP2→1、direction1首轮010641即符合，不冒称最后重入修复才开放反击；ParseInputEvent不是OS真人输入。readonly晚期Flyer侧向几何建议未替换这条已观测的早期Manifest正控。

核心6：两角色Hidden原生J/K拒绝且真实World TestMove阻墙；自然Manifest J1/K正控；原.70预警至少42tick、实际侧袭／下冲及目标HP9→8；实际Active跨hide撤旧hitbox/ID，下一显形从完整新预警开始。Manifest4秒仅开发正控，短Flyer1.9/Diver.85秒检查Active取消，不压缩模型。

边界10：两角色已接受K跨hide仍真实受力／重力／返航，sleep长于隐藏时长后剩余phase仍暂停；两角色原生致死回调Reset及第二次死亡；两角色保存Player K→J；无效配置原子性；Manifest坏设置／真实跨World／queued和freed源／K同栈Reset；真实J1+测试1.2秒无敌→下K扫void→删源/sleep一次环境死亡；pending死前Cancel同栈Reset不杀新生命。

未声称long-hitch／跨sector phase持久化、所有家族美术或ALL50实际活动已验证。5个开发探测产物42次teardown均native gone，自建Shapes76/76释放、borrowed0；不由此推断匿名RID身份或资源问题解决。

## 最终同源码12门

| 目录 | engine ms / exit | 严格结果 |
| --- | --- | --- |
| resident-family-ghost-air-boundaries-20261017-010914-929-ef620bf954fe418b8d9b9e75e0b7b5a9 | 20822 / 0 | 10/0行为；stdout7 RID，FAIL |
| resident-family-ghost-air-20261017-011030-243-18d3b45f37fe479fb9f37032f9043437 | 10740 / 0 | 6/0行为；stdout4 RID，FAIL |
| resident-family-bone-air-20261017-011110-172-4c3c3f8547dd4f57ac27f610f59f89b5 | 11180 / 0 | 原8/0；stdout7 RID，FAIL |
| resident-air-20261017-011150-896-43963ba8839b4d38a0f6f4ffb2c5fb13 | 40332 / 0 | 原14/0；stdout4 RID，FAIL |
| resident-air-roles-20261017-011303-095-2f88e2f6285c4fd291ca774d23f62766 | 20990 / 0 | 原7/0；stdout4 RID，FAIL |
| resident-air-boundaries-20261017-011348-725-491255a206c7492d8e942479600bb3c7 | 14208 / 0 | 原6/0；stdout4 RID，FAIL |
| resident-air-default-20261017-011429-107-62442d5575db45d8a1ee5a4837fb8c03 | 4945 / 0 | 原完整model/runtime综合，GREEN |
| resident-allocation-20261017-011502-244-93049f446dca4d6eb7d4f74dd9919d72 | 520 / 0 | 17同检查器负控，GREEN；仅名册／未就绪状态 |
| keyboard-20261017-011534-265-280086f91f93403687c61fbc914da3c2 | 9577 / 0 | GREEN |
| actions-20261017-011612-512-41be672456264f568c517431cc17fe4e | 7076 / 0 | GREEN |
| prologue-pre-20261017-011648-414-e3471958664d4d9dac3c4146c732ec70 | 88377 / 1 | UpperPendulum/lower_to_mid_swing sample.5错台，149/150、final264.99634/160.6107；两PresentationProfile警告，FAIL |
| prologue-post-20261017-011845-042-7173599cf54b469a8f5783a16b84a1ea | 8115 / 1 | BellChain WaitForFloor540.0704/1196.0139，一条六级祖先ILevelRecovery错误，FAIL |

全部ordinary fresh Rebuild0W0E。build cap180s／专项55s／pre-post180s，无超时或guard失败、expectedFailure、movie、fixedFPS、verbose/typeidentity。外层串行exit0不是全绿。六专项退出RID仅stdout有，不能由external通过标记覆盖；pre/post原始日志保留，不豁免。当前Supply正常返回／无Handle或FATAL不关闭历史崩溃，旧Skitter default资源故障本轮未复测。

## 保管链与版本

最终12案五组前后清单60对一致，source/resource1670（不是C#文件数）、protected userdata109；只hash真实数据，不读写语义。pre helpers10，其余9，binary6。全部消费者终态并确认owned native0后才写本报告和README。

| 项 | SHA256 |
| --- | --- |
| source inventory | 1301C1B4547580DCE736B86D4352596124E045BA677CDFB650750ACD5B069A32 |
| binary manifest6 | 2B3C0FB7473FC042CEB21DB155E8362011920D59C1336E98A22C872C9AA0F214 |
| 通常helper9 | 69F81E7F65E0EE69EFAC6855EB4D5A03C4D6887118A7964C8FA5C1FA4B2EF079 |
| pre helper10 | 7093067D425CE3F9694910FC47686E4BDC0515CE0A1843DD2C5ADDAE9F2C5508 |
| userdata109 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| Moth main | 6C6E4AD22EFAD462B685B2897CE35B81BE00A15228431B211754E2CF1070544B |
| Moth Residence | C4B7529B6BAD0F2AC237319E8F1959659BDE4B02A4A9F5D4D32264AC512CBE8B |
| Air tests | 1BAAC89246FED5695CCADC7C84210B2A49F268753A68184C4C94B6EDF732933C |
| runner | E4AFFC0C05B195423E64F9B83CC17BB56F7F162A86B42D0796392778ECB6F3E9 |
| canonical owner | 1375D4EFE264B4C63FC12A7C7DA6C5BA40EBC08991289B2C8B1D373072BAAFB4 |

前轮BoneMoth报告原件SHA CA0AC502B6E08E1FB14FE16209E6D5E6EDF64A3C592562A3C2EA3419C417280C保留。已有source/header历史不重写成完成证据。

## 未完成与下一步

正式新增仍0/50；全部Placement=null、24子型SceneState.Unimplemented/ScenePath=null。旧26+新50=76普通+1Boss、Bone10/Ghost10、ECHO1persona/独立友方身体0不减。Task1最终50／整个Task2／Task3-4仍OPEN，资源／旧档／正常Main+Explore同角色标题到Boss和有支撑出口／呈现60帧／最终视听／真人／审阅归档录像均待验收。

下一实际做唯一剩余Ghost炮台承载，再24正式builder场景与ALL50活动／攻击／返航／玩家接近及world-lifetime manager。只读准备：真实类是BellBoltSentry与ResidentTurretResidence，唯一目标ghost-tower／灵能拘束塔，不是ClockworkSentry或额外Ghost远程类型。原单发bolt／1.10s预警／K仅眩晕不拉近；复用1024×640炮台fixture，别拿32×32空中fixture证明射击；Reset尾部无条件加thread_targets要实测Hidden双组；void用RemoveFloor实际重力下落，不套Moth Kdown。该炮台尚未实现／运行Ghost策略。

本轮无新地图、美术／音效生成、图稿／视频、付费、Git写入、上传、安装、删除或自动化。无窗口证据不能替代最终视觉和真人体验；目标继续ACTIVE，不触发task-done或全计划完成审查。
