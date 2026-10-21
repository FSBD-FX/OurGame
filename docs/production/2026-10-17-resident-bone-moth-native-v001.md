# 骨骸俯冲型：真实姿态弱点与机械爪事务 v001

2026-10-17，Asia/Shanghai。开发承载证据，不是正式身份、50只入图或最终版本。完整目标 ACTIVE；最终同源码10门严格结果 **4 GREEN / 6 FAIL**。

## 事实、推理与风险

- 事实：现有 Moth ConfigureResidence 忽略 CombatProfile，Hover 和普通受伤产生的 ThreadStunned 都接受 J；机械爪只有 void fallback，不能证明最终拒绝。先写8个实际运行案，首轮7失败。
- 推理：Moth 原模型没有 Falling/Returning 姿态，ThreadStunned 原0.75秒计时不因离地提前关闭；但普通非零 Hurt 和真实 K 都请求同一姿态。因此新增独占 Bone+Diver+RecoveryOrThreadStunned 策略：真实 Recover 或“真实K来源且当前ThreadStunned”才允许 J，不能复制 Skitter 空中补偿计时器。
- 风险：仅开发机制，不是最终难度、美术或手感。Shape RID 告警、历史资源异常与整关回归仍未闭合，不能宣称正式交付。

## 最小实现与保持项

本轮从 native RED 到 producer 只修改一项生产源：`GraveLampMothResidence.cs`。独占 profile 在所有验证后提交；Bone 必须 Diver、policy2、Ghost秒数0，缺失/混合/错误策略拒绝。J 在 Health 前判实际姿态与 K 来源，保原 Health 去重/无敌期。

来源标志只由接受的常驻 Pull 设置；原 K 窗口内接受 Hurt 不扩展模型时长。离开 ThreadStunned、sleep、Reset、death、void pending及退出清标志；取消晕眩/普通击退不产生 K 来源。void 致死仍直接请求真实 Health，不受 Bone J gate 阻断。

新增最终 bool receiver 检查 actor/source live queued链、同Tree/World、actor HP/模型、sleep/void及非空有限正 EnemyPullSpeed；健康值或 AI 领地不作为来源权限。未配置实例和 Native Flyer/Diver 仍调用原 ApplyThreadPull。旧 ThreadTarget.ApplyPull 的拒绝后 fallback 未改，最终拒绝证明走直接捕获接口/transactional路径。

Moth main/model、Tuning、Health、场景、输入、Player/机械爪源与资源均未改变。保原HP2、Recover .70s、K .75s、原常驻600拉力上限、重力/衰减/碰撞/原Flyer与Diver轨迹。无新GC、cache、借用Dispose、额外drain、资源告警豁免或测试强制姿态/HP注入。

## Native RED → producer

证据根 `E:/OurGame/Knight/.evidence/october13-stable-claw/`，每案保留合同/wait/构建、三路原始日志和保管链。

| 目录 | 实际结果 |
| --- | --- |
| resident-family-bone-air-20261017-002938-409-bf6f3b7d59564535895a152109ffa52b | build0/0W0E；engine1/3963ms；8案7失败；stdout7 Shape RID。 |
| resident-family-bone-air-20261017-003255-620-aa08b411d0624482a1f7b4f8e121cff4 | build0/0W0E；engine0/11270ms；8行为符合；stdout7 Shape RID，严格FAIL。 |

八案覆盖：Hover关闭→真实K开放→到期关闭/实际返航；实际Dive→Recover受击→非零Hurt的泛用ThreadStunned关闭→真实K再开放；sleep清旧来源而保已接受外力；原生致死Area回调Reset及第二生命；原生J1建立测试1.2s Health无敌后K下拉穿实际void、pending跨删源/睡眠最终死亡；无效配置原子拒绝；坏设置/跨World/queued及freed来源/queued actor拒绝、无Health来源正控；保存Player的物理键码W-Up/K→Up-J原生命中。

普通Hurt负控：攻击Area中心侧偏12px、Knockback260，证明真实位移和ThreadStunned；接受伤害后等待22原生帧，排除默认.35s无敌期遮蔽。睡眠案等待5+18帧后仍实际ThreadStunned，但旧K来源不能恢复。每次拒绝均新原生攻击且证明真实重叠，不只看HP未变。

保存Player案 K1/J1/native entry1/首次ThreadStunned/Connected1/HP2→1，原.06s startup/.10s active不改。**该正控首轮已符合，不冒称新修了时长或可达性**；引擎ParseInputEvent物理键码不是OS真人输入。首轮void案复合断言首先缺 bool receiver，不证明原void本身损坏；producer保留实际pending/死亡正证。

真实外力越出领地后位置最大(498.084,347.29007)，连续物理返航returned=True、HP1，无硬夹紧/回血。8个fixture实际native gone8/8、自建Shape22/22释放、borrowed disposed0；这些不识别匿名7 RID的资源身份。

## 最终同源码10门

| 证据目录 | engine ms / exit | 严格结果 |
| --- | --- | --- |
| resident-family-bone-air-20261017-003255-620-aa08b411d0624482a1f7b4f8e121cff4 | 11270 / 0 | 8/0行为，stdout7 Shape RID，FAIL |
| resident-air-20261017-003522-880-65800cfce1d84c2fa32cbed068e1aa16 | 40365 / 0 | 原14/0行为，stdout4 Shape RID，FAIL |
| resident-air-roles-20261017-003638-746-4ba1e3fdb6374c34b8dfee6044321318 | 20969 / 0 | 原7/0角色，stdout4 Shape RID，FAIL |
| resident-air-boundaries-20261017-003727-338-0274b97283984ab3a369a10cf92768c5 | 14202 / 0 | 原6/0边界，stdout4 Shape RID，FAIL |
| resident-air-default-20261017-003808-764-477554067fc745018fdc09e6063a6c87 | 5159 / 0 | 原完整model/runtime综合，GREEN |
| resident-allocation-20261017-003841-633-d96bf5623e994a569b7ef3d37a1f58fe | 515 / 0 | 17同检查器负控，GREEN；仅名册/未就绪状态 |
| keyboard-20261017-003911-133-0c9d1a9c80054a848ddd12b0820305ac | 9573 / 0 | GREEN |
| actions-20261017-003945-013-fe14d22b21e24958b3e9ee7facfcd703 | 6977 / 0 | GREEN |
| prologue-pre-20261017-004023-655-de98421ad5c34b72b332e3039ffaccb8 | 88083 / 1 | UpperPendulum/lower_to_mid_swing sample.5，final264.99634,160.6107，149/150；两条PresentationProfile WARNING，FAIL |
| prologue-post-20261017-004217-249-2fd6c607ced24f029498032bf1ac4fb8 | 8015 / 1 | BellChain WaitForFloor 540.0704,1196.0139；一条六级祖先ILevelRecovery ERROR，FAIL |

全部普通fresh Rebuild0W0E，build cap180s、专项native55s、pre/post180s，无超时/guard失败/expectedFailure豁免/固定FPS/movie/verbose/identity诊断。串行外层exit0不代表各门通过；依据每门三路原始诊断判定。当前Supply返回、没有当前Handle/FATAL不能关闭历史崩溃；上轮Skitter默认ObjectDB/2资源问题本轮未复测，未关闭。

## 保管链与源版本

全部消费者终态后才更新文档。最终10案 source/resource1670（非C#文件数）、保护109、五组前后清单50对一致；真实userdata仅hash，无语义读取/写入。pre helper10，其余9；binary清单6。

| 项 | SHA256 |
| --- | --- |
| Source inventory | 241DA3F21EABFEF54448E0703CEA9938050F2831A58D0852586B27FAC6684DDB |
| 通常helper9 | 089D08EEAD08430F16AC8D658FF9845977C2C47CBD6DECC551EFBEF266022791 |
| pre helper10 | 00A9F95998BB91A9963A4D4D8CA90BD203FB97F817E02DE0599D1544AD58B073 |
| Binary manifest6 | 7239865727A6E6727AD1B3FFF62B2D70DF16EED705B3C0C7FFCC691220FE0780 |
| 真实userdata109清单 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| scripts/enemies/moth/GraveLampMothResidence.cs | 3CEB4C5DAF50DAE8086CDC9E1F3379B2F9F5EA16A86C009E1B4CCF3D01EC367C |
| tests/integration/ResidentAirRuntimeTests.cs | 020E3BB2904AAFE6C59F2C02D6CDED5E1B294A44A32882CF4E10D9213E475DB3 |
| tests/RunStableClawPullTests.cs | 0205EE67BA97D38236F1DCBA51B7F46B5F77AA1138F346F134AAD08461126EE3 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | E76847700DF6D47EB439A1AD5A4F603162DD9A31A548528382086CCA13C56FBF |

前轮骨骸冲锋报告原件保留，SHA B732B0849B0924F293FF5E4F2DAC7F9D4929BEB3827E2F62D56D384FAEB41097，其他历史报告未编辑。

## 未完成范围与下一实际动作

正式新增仍0/50，50 Placement全null、24子型SceneState.Unimplemented/ScenePath=null。旧26+新50=76普通+1Boss、Bone10/Ghost10、ECHO1persona/友方身体0完整规划不减。Task1最终50、整个Task2及Task3/4均OPEN，资源/旧档/正常Main+Explore同角色全路/有支撑出口/呈现60帧/最终美术音效/真人/审阅归档录像均未验收。

下一实装 GhostMoth Flyer/Diver，再GhostSentry（剩余3家族承载），再24正式builder场景、ALL50完整活动/攻击/回返/玩家接近点位与world-lifetime manager。只读准备：Ghost需typed family，不可把Bone profile非空当Ghost；Hidden分别撤J权限、K发现与最终接收/旧攻击，保身体和已接受外力/void。短.8s显形不够原Moth .70s预警+原攻击，未来原生角色正控须给足有限显形时间，不压缩模型。该部分尚未实现/运行。

本轮无新地图、贴图、音频、图稿/视频、付费生成、Git写入、上传、安装或自动化。继续无窗口，不抢用户键鼠；失焦解释不等于全部输入问题已排除。
