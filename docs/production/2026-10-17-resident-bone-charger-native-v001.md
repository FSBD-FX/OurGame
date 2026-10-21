# 骨骸冲锋者：真实机械爪反击与空中窗口 v001

2026-10-17，Asia/Shanghai。开发承载证据，不是正式族系资产、50只入图或可交付版本。完整目标保持 ACTIVE；本轮最终11门严格结果 **3 GREEN / 8 FAIL**。

## 事实、推理与风险

- 事实：保存 Skitter 原 ConfigureResidence 不处理 CombatProfile，Idle、移动 Retreat 都接受普通 J；捕获的机械爪调用只有 void fallback。新增测试先取得8案7失败，不把编译成功当玩法完成。
- 测试前提纠正：复制 Rust 的 Player x260 不适合 Skitter。保存锚点位于脚部 y128，Player 中心 y92，偏移60,36约31°，超出现有22.5°键盘锥。测试改从 x220 开始，并要求真实 KeyboardPreview 选中保存目标；未改游戏锚点、瞄准锥或距离。
- 推理得到原生正证：最初严格 Stunned 弱点在 K 真实拉起身体后失效。诊断案实际 K1、J1、AreaEntered1、首次接触 Falling、Connected0、HP2；原模型在失去支撑时优先进入 Falling，旧弱点随之关闭。不是漏碰撞。
- 修正：追加独占 Charger 策略 `RecoveryOrThreadExposed`（枚举3），不扩大 Rust 的 `RecoveryOrThreadStunned`（枚举2）。真实 Recover 可受击；成功 K 来源的 Stunned/Falling 在原 `Settings.ThreadStunSeconds` 内可受击，默认0.60秒。移动 Retreat 仍关闭，普通取消晕眩不算 K 来源；睡眠、死亡、void、Reset、退出与窗口到期撤销权限。原模型仍实际 Falling，没有延长眩晕或续接落地计时。
- 风险：本轮行为符合不等于最终难度、视觉可读性、正常整路、60帧或真人手感验收。退出资源告警仍严格失败；无匿名泄漏资源身份和历史 FATAL 根因证据。

## 最小实现与保持项

Profile 完整验证后才提交，Bone Charger 必须独占枚举3且 Ghost 秒数为0；旧枚举2、未知策略、混合/缺失策略和重复/挂载后配置均拒绝。J 在 Health 前判权限，不改保存 HP2、攻击伤害或原 Health 去重。

保存 Skitter 新增最终 bool `IThreadInteractionReceiver`：验证 actor/source 完整存活节点链（包括 queued ancestor）、同 Tree/World、actor HP/模型、休眠/待致死及非空有限正 EnemyPullSpeed；不要求来源有 Health、player 组或位于 AI 领地。未配置实例仍走原 ApplyThreadPull。旧 `ThreadTarget.ApplyPull` 的 fallback 合同未改变，拒绝证明使用实际 transactional/adapter 路径。

原 Skitter model/settings、保存敌人/Player 场景、机械爪与输入生产源均未改；保原600拉力上限、重力、支撑/碰撞、自主冲退/回返和时间。无新资源清理/GC/cache/借用 Dispose/额外 drain/日志豁免。

## 原生 RED → 诊断 → 修正

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw/`。下列每个目录包含原生合同、wait、三路原始日志、构建与保管链；早期阶段不与最终源码混称。

| 目录 | 原生结果 |
| --- | --- |
| resident-family-bone-charger-20261016-234525-774-f319f578cf6849c38bdb38cbbf27e45e | 7187ms，exit1；8案7失败；另有7 Shape RID。K选择案的测试前提错误另行保留。 |
| resident-family-bone-charger-20261016-235538-038-7e77ad4a16204b8b8c06d106c2c1f7ec | 10984ms，exit1；7案符合，保存Player真实J失败/Falling；另有7 Shape RID。 |
| resident-family-bone-charger-20261016-235756-649-a6620d80e1ee4da7a968e4089539dff3 | 11219ms，exit1；原生接触正证：K1/J1/entry1/Falling/Connected0；生产未改，仅添加观测。 |
| resident-family-bone-charger-20261017-000028-002-21dffbe0880f47d9a36d4b6e013fcc71 | 12353ms，engine0；9行为符合，stdout7 Shape RID，严格FAIL。 |

最终九案：关闭J→K开口→到期；真实Rush/Retreat关闭→Recover开放→Telegraph关闭；睡眠清K来源与普通取消晕眩负控；原生致死Area回调内Reset及第二生命；真实K出悬崖/void死亡；无效配置原子拒绝；坏设置/跨World/queued链拒绝及无Health来源正控；保存Player物理键码K→J原生命中；真实向上K在仍处Falling时窗口到期关闭。

保存Player案原 .06s J startup 未改，K1/J1/原生entry1/Connected1/HP2→1，首次接触仍为 Falling。键码通过 `Input.ParseInputEvent` 输入引擎，**不是OS真人按键**。第九案使用真实 healthless caster/legacy K与原生fixture攻击Area证明独立空中到期，并非第二个保存Player键盘演示。

最终九案清理回执实际 native graph gone 9/9、自建Shape分配/释放41/41、world disposed9/9；这些不证明匿名7 RID属于哪些保存资源。清理边界计数不是整个测试的运行帧数。

## 最终同源码回归

| 证据目录 | engine ms / exit | 严格结果 |
| --- | --- | --- |
| resident-family-bone-charger-20261017-000028-002-21dffbe0880f47d9a36d4b6e013fcc71 | 12353 / 0 | 9行为符合；stdout7 Shape RID，FAIL |
| resident-charger-20261017-000252-462-b6a7c93bce8b4070a4c4c9c00557b695 | 42658 / 0 | 原20行为符合；stdout4 Shape RID，FAIL |
| resident-charger-default-20261017-000405-891-36ee69e5adb847398cdfa5dcce882cb2 | 38670 / 0 | 原完整model/runtime综合返回；stderr4 ObjectDB WARNING＋2 resources ERROR，FAIL |
| resident-family-bone-wanderer-20261017-000509-187-c485ea1162404c998ec368e52ae32745 | 7714 / 0 | 原7行为符合；stdout7 Shape RID，FAIL |
| resident-family-ghost-20261017-000544-106-3d080343483b48c39d07b81c7514c03f | 6441 / 0 | 原3行为符合；stdout4 Shape RID，FAIL |
| resident-family-bone-20261017-000616-357-38c8d0920ba84add874ecc5bacbb80dd | 3559 / 0 | 原4行为符合；stdout4 Shape RID，FAIL |
| resident-allocation-20261017-000645-307-8382fa5e2a5341a5ae27608a6298bf77 | 395 / 0 | GREEN，17同检查器负控；仅名册/未就绪状态 |
| keyboard-20261017-000714-193-6f175b8fcb3540bcabdb0cacb2895c15 | 9587 / 0 | GREEN |
| actions-20261017-000753-664-9a0e901321f144908e593aaad2c1cd42 | 6975 / 0 | GREEN |
| prologue-pre-20261017-000829-564-06daa4a4521b44ee8297c5aa7b206d43 | 88305 / 1 | FAIL：UpperPendulum/lower_to_mid_swing sample.5，final264.99634,160.6107，149/150；两条PresentationProfile WARNING |
| prologue-post-20261017-001027-283-8d2c43c0187d46479489f3f7bbc96eb4 | 8036 / 1 | FAIL：BellChain WaitForFloor 540.0704,1196.0139；本次stderr一条六级祖先ILevelRecovery ERROR，不抄历史六条 |

全部普通fresh Rebuild成功，0 warnings/0 errors；构建cap180s，专项native cap55s、pre/post180s，无超时、无guard失败、无named expected failure、无fixed FPS/movie/verbose/identity诊断。外层串行循环结束码0不代表各门通过；严格结果依每门原始日志判定。pre本次Supply返回且未出现Handle/FATAL，不能关闭历史崩溃。

## 保管链及源版本

最终11案 source/resource inventory1670（不是1670个C#文件）、真实保护文件109、五组前后清单共55对一致。只hash真实userdata，无语义读取/写入。pre helper10条，其余各9条；二进制清单6条。所有消费进程终态后才更新本报告/README/台账。

| 项 | SHA256 |
| --- | --- |
| Source inventory | 35D7C6CA55F1AD632FE2C6D930DF28073762AA6D7126827BC8EFCD65A6CF55F3 |
| 通常helper9 | 0BC9EE4CA1DBAA067B59EC0B5083A67C00A6EF0C147FFDD8CB2C23ACF41F641F |
| pre helper10 | A82CB8B3B15B0BBEBFCE8E086D04FDB3CFFB71A7ED8628BA854A0B445023D4A5 |
| Binary manifest6 | F2C412F2370D953F6E02777533B61C0EC00BB8671D66795988D7C1FB6C8A8E8A |
| 真实userdata清单109 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| continuation清单 | 7EB70257593DA06F682A3DDDA54A9D260D4FC514F645237F5CA74B08F8DA61A6 |
| scripts/enemies/skitter/CanyonSkitter.cs | CE777487FD86FF048E284CE1B511FE48F92BF944D789115E93682B6514808975 |
| scripts/enemies/skitter/CanyonSkitterResidence.cs | E55CC002F4E2C3CC218AC7A4C2E836AF4CFDDDAC00F686188AAC1631D9681274 |
| scripts/prologue/residents/ResidentPopulationDefinition.cs | 44395BF8DC6B175D9F268E4BB533AC3A1DDE508E3DB4BEA91993E4D3EEA8ED89 |
| tests/integration/ResidentChargerRuntimeTests.cs | 7248A8A256EFD652CA82D751BCA88AFFF96F0593E4413A9F4AE71ECEAFFFEA21 |
| tests/RunStableClawPullTests.cs | 6188ED9A0A73F43E048C304E9E79B3BC3024E4D652BF92D73740BC3B1AB3738C |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 3E6F96EA9A5459DF80770D7633C35DF6B71E649AD924FDFAF9C65BBD929D997C |

前轮原件未覆盖：BoneWanderer报告SHA9270A2B80961565EF8E3300DE079B7F656675BD3EC83C3E50CCF9BC4DD08169B；Supply mount D5AF2808FDB469366DCA7B04439C40B5C9C33F51824D44580648D2B8BE361E1F；完整Supply 5C4DF2EAEAA0AD12B5B4D901046CEA4355EBACC74C0710B1054A1E682CEE2763；Bone/Ghost 24E7FF5E61AB42984875454F7FE29581467DD0AA769442743C4F565CDB7423DC。

## 未完成范围与下一步

新增正式入图仍0/50；全部50 Placement=null、24子型SceneState.Unimplemented/ScenePath=null。完整规划旧26＋新50＝76普通＋1Boss，Bone10/Ghost10、NPC ECHO1persona/友方身体0，不减名额。Task1最终50、整个Task2及Task3/4均OPEN，不推进整任务完成勾选。

下一实际承载 BoneMoth/Diver，再GhostMoth Flyer/Diver/Sentry（剩余4家族子型）；随后24正式builder场景、ALL50完整活动/玩家接近/攻击/回返点位、world-lifetime manager。只读准备发现Moth没有Falling姿态，原ThreadStunned .75s独立计时；普通Hurt也可请求该姿态，必须区分真实K来源，不能复制Skitter补偿或仅凭时长宣称J可达。

资源根因、旧存档/恢复、正常Main+Explore同角色全路、实体出口、真实呈现60帧、最终美术/音效/真人及审阅归档短录像均未验收。用户在使用其他窗口，仅无窗口测试、不抢键鼠；本轮无新图/视频/美术/付费生成/Git写入/发布/自动化，不把失焦解释扩展成所有输入问题已排除。
