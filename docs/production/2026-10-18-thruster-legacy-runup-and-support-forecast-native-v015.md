# 旧教学助跑、真实支撑与释放路径 · 原生 v015

2026-10-18，Asia/Shanghai。完整用户1–11工具目标ACTIVE，未缩减、未完成。Task1–5不重做；Task6音频退出／视听与Task7消费者迁移仍OPEN。本报告不是V2正常Main／Explore通关、正式美术或真人验收。

## 事实、推理、风险

事实：原legacy下行`.2`独立复现1cast／0latch、Obstructed。测试自有-101／-99观察节点夹住实际Claw(-100)，不写输入、运动、目标或地形；physics20前是Travel、精确locked ID、无Space／primary／shield／attack新边缘，独立同mask射线首先命中配重Area、LOS=false。纯模型独立重放下一状态Latched；实际同tick发表Released／Obstructed。这是边界观测，不冒称读到了controller私有局部变量。

推理：结合现有gate短路顺序，此处先由LOS拒绝，PullMotion检查没有执行。没有改World／Enemy／其他目标的全局遮挡政策，也不将它归因于boost停距或牵引力不足。

风险：原静止立即跳的`.2`失败仍是有效历史反例。新通过的是“同`.2`出生、原LaunchZone内实际助跑后跳”的合法输入轨迹，不能称三个独立20／50／80%起跳点全部通过。新释放预测虽已读取实际快照和支撑，但仍不能处理路径中的所有实体碰撞；最后高层样本正揭示这个缺口。

## 本轮保留的改动

- `RunMandatorySample`仍按原fraction预入树出生；正常`descent_grip_lesson`在原EntryShelf上助跑，catch-return的targetOverride调用明确排除。实际原生up-contact、完整胶囊和原LaunchZone每步验证。
- `.2`从(128,574)到(171.11108,574)，13次循环等待，末vx260；13从原240循环预算中扣除。原cast前等待和第二Space测量等待未增加；240不是整个函数绝对native tick上限。
- 四个assisted按segment解析原保存命名支撑：GripLanding、LandingOne、LandingTwo、LandingThree。读取真实矩形、one-way／16margin、变换及胶囊，求站立Y和完整水平支撑交集；不从当前catch或targetOverride猜落台。Descent站立Y704，不再用zone中心708。
- `PredictLandingX／PredictBoostLandingX`读取实际Motor不可变Motion与Module，使用StepFree、实际Hz、剩余循环预算及真实boost上升保护；保一次boost、原locked normal的去inward→限速顺序。预测首步使用实际释放轴，此后对应原中心松键策略。
- 原最终15%双轴margin和12稳定帧保留；assisted的每个稳定样本另要求原命名支撑、实际up-normal、feet高度和完整胶囊。没有放宽旧oracle。
- 新增严格focused入口与输入副本；观察器、射线query／dictionary／Array backing为自有对象，读取原生slide缓存不Dispose借用wrapper。没有加生产仅测试API。

本轮没有改正式Player、地图、Claw力学／遮挡、HP／伤害、普通跳跃、音频或美术资产。Player原字节SHA256仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。

## 最终同冻结四门

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。batch98735终态exit2，两严格失败；全部consumer已终态才维护文档。

共同source1695／SHA256 `E8420AACE7D13EE3E5EEF83F21080D563A4FF75C5DB6B6AA7120BF8B177AEA38`。四次fresh Rebuild均0warning／0error；四案source、helpers、用户文件inventory和binary before／after哈希分别全等，guards=[]。所有五类原始log全量扫描，失败正文与缺失的截断段已读回。没有新增日志豁免、GC／cache／drain。

| phase／唯一case后缀 | engine／ms | 结果 |
| --- | --- | --- |
| thruster-legacy-descent-entry／20261018-230159-491-3129728ccd3a45b6b505c27f7f45929b | 0／3098 | GREEN；133循环帧、1cast／1latch、真实第二Space借力、无普通二段跳／Shift／恢复，末(646.53143,703.93207)，原12／15%及新命名支撑通过 |
| thruster-legacy-ascent-high／20261018-230238-305-dd8cf691bec845e1977c5c0d5ea566a7 | 1／5001 | 严格FAIL；首`.2`未通过，因此`.5/.8`未执行，不冒认三样本覆盖 |
| thruster-free-forecast／20261018-230320-309-83b89e3f76754b7e963a1e7ff7687e48 | 0／585 | GREEN；原独立几何和／离散地面／反刹过零／旧2800负控保留 |
| prologue-post／20261018-230355-720-a0f6c78bff324670a1706f10b4b1f3d8 | 1／70724 | 严格FAIL；通过9视线、全部4回返、前7段21mandatory样本，首失败推进到ascent_grapple_three `.2` |

最新post四回返末点：(646.5199,703.93286)、(806.00214,1155.9338)、(937.9939,975.93304)、(902.7405,677.9338)，均原12稳定／15%／0KillZone请求并有实际目标支撑。Descent三出生样本循环帧133／124／119。Bell／墙／顶／自救／盾通路的前缀也通过，但不据此前缀为整个post签发GREEN。

## 高层首失败已证实的链条

保存LandingZone从x812开始，实体LandingThree实际从855开始；完整胶囊中心需要至少875.08。原预测819.31只在逻辑zone，不在实体支撑。修真实高度／范围后，新的自由飞行预测878.4692仍失败：释放请求时(819.25946,631.5926)，vx212.52。

focused实际记录：physics83中心(829.9252,634.5415)，碰到`CatchReturnCeilingThree@(849.92,630)`且vx变0；该非one-way矩形为x850..946、y606..630。physics92中心x836.17523／y676.34186，下一tick已低于站立Y678，尚远不足完整支撑左界875.08；随后碰LandingThree边缘并掉落。末(1140.484,1299.9333)，239／240循环帧，1cast／1latch／1boost、无观测Failure，故不能再称未cast或目标找不到。日志未记录该接触normal，不能把推断的精确(-1,0)当已测值。

左侧摆荡替代策略两次严格失败：225819-505f2910过早左移被AscentAnchorTwo遮挡；225945-00395003限制Swing后仍未完成、没有Space借力。失败原件完整保留，替代策略已从当前源码撤回；没有提升拉力、移走天花板、扩budget或删case凑绿。后续先补碰撞／真实输入采样边界的可达性计划，不反复调一个落点阈值。

## 本轮原始过程证据

全部在同一证据根，未覆盖旧案：

- 原下行独立RED：`thruster-legacy-descent-entry-20261018-222841-961-9e9f1bd5e1b5466a8bfee2a10f19eaf0`。
- 边界诊断严格FAIL、behaviorAccepted=false：`thruster-legacy-descent-entry-trace-20261018-223630-438-d27d0e9e58ee41e6b02ca3a5204e3135`。
- 首助跑GREEN：`thruster-legacy-descent-entry-20261018-223855-361-ba22b53128f047a180783c0ec2773bc9`。
- 首post误用于catch的测试错误：`prologue-post-20261018-224030-001-5735cc8ea9de41ec9a3384a548fdb477`；随后限定normal调用，未删catch。
- 后续post高层RED：`prologue-post-20261018-224236-902-5d6c5781fdc746daa0fbf714aaf420a7`。
- 支撑预测仍FAIL：`thruster-legacy-ascent-high-20261018-225013-324-19b23756881b49eb9af45ed4a0fac529`；碰撞观察FAIL：`thruster-legacy-ascent-high-20261018-225351-781-723f0a5214144f14bcad2aa51ec0aa15`。
- 两个撤回策略案：`thruster-legacy-ascent-high-20261018-225819-513-505f2910b09c4eb887e7f1a6d5e17463`、`thruster-legacy-ascent-high-20261018-225945-403-0039500350af42f1883870a075cd6876`。

## Task7数值消费者账（不等于用户1–11清单）

当前源码重建11文件，不冒称原历史清单逐字还原：

| 文件／接缝 | 当前剩余 |
| --- | --- |
| PrologueTraversalRouteTests／continuous PredictDetachedLandingX | fresh四条已有证据；continuous仍旧积分／固定Hz／热读Tuning |
| SentryCounterEnergyBudget／SteamSentryRecoveryDriver | 旧地面neutral停距，保60／64／8及≤2实际速度 |
| BellBoltSentryRuntimeTests／Recovery | 旧global／firstK时限和fallback停距，尚未迁移 |
| CanyonSteamDiverterRouteProbe | staging及通行时长旧公式 |
| RangedWeaponRuntimeTests／SteamMissileMovingTrial | strike band停距及旧资格字段 |
| PrologueCanyonTraversalTests | 本轮assisted预测部分迁移；高层碰撞未闭合，镜像站位／shield-jump旧停距仍在 |
| PrologueCanyonGeneratedMainRouteTests | walkway／Boss／LowerBell／配重与旧GrappleToLanding仍待逐处迁移 |
| 同类BasinReturn | 原budget／±20／12完整支撑下旧停距 |
| 同类FragmentReturn | walk/drop不能全程假设地面 |
| 同类NaturalFragments | ±1.5／≤2／120tick旧站位 |
| 同类TempleSupply | ±1.5／≤2／90tick旧站位 |

可独立下一步是Sentry地面neutral闭环。旧60Hz原始验收cap为global1215／首次真实bolt退休后的firstK73；新实际快照推导与历史cap须分别记录，可取更严格的min，不能把裁剪值伪称新derived，也不能擅自扩cap取绿。行为RED应捕获实际neutral要求松A而旧2800仍持A的区分样本，并观察下一完成Motor步；当前尚未实现或运行。正常Main launcher的1675旧库存绑定也必须按实际1695迁移后再启动，不伪报库存。

## 未完成与保管边界

最新两FAIL只有原落地断言，没有新的退出资源诊断；这不能关闭历史Handle／RID／FATAL，也没有修复Task6短喷音频2 ObjectDB。pre本轮未重跑，v014两次GREEN仅保留为历史证据。完整正常双路线、最终资源／动作／UI／音频、CG范围、存档／真人／显示60FPS、全七任务末review和ZIP／授权GitHub仍OPEN；新50正式仍0／50、24canonical未实现、正式127／候选132未发布。

无新媒体生成／展示，因此没有新增过程图稿或视频文件；以后展示仍先按`E:/OurGame/Knight/过程图稿与视频`规范归档。没有付费、下载、OS按键／前台抢占、Git写入、发布或恢复自动化；真实用户存档只做原保护inventory，无语义读取。
