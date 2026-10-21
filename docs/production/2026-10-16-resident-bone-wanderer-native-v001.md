# 骨骸游荡者姿态弱点与键盘反击 · v001

2026-10-16 / Asia/Shanghai。Task2局部生产实现，不是新增敌群正式入图、最终美术或整关交付。继续无窗口，不抢焦点或操作系统键鼠。

## 事实、推理、风险

- 事实：先运行未实现版本，六项缺口失败；加入Bone Wanderer独立姿态策略后六项行为符合，再用实际保存Player的K→J补验，七项行为符合。最后同源码11门为6 GREEN／5 FAIL，资源与完整回归失败未豁免。
- 推理：Bone不能沿用“非空combat profile就是Ghost”的旧前提。现在按明确家族区分，只有Ghost运行显隐时钟/alpha；Bone的J权限只由真实Recover或真实K造成的Stunned开放，K发现与投递不随J防护消失。
- 风险：当前只是保存Rust的显式开发承载，未成为正式骨骸场景或本关出生名额；0.25秒K窗口沿用原设置，实际按键能接上不等于所有玩家都觉得舒适。七项退出仍有7 Shape RID，资源根因、最终造型/动作可读性和真人体验未关闭。

## 实际生产变更

1. 增加明确`RecoveryOrThreadStunned`策略，不把Heavy的`RearOrExposed`换个含义。Rust配置校验在任何字段提交前完成；Bone拒绝缺失/未知策略、Heavy策略和任何Ghost时长，其他家族不能夹带profile，Ghost原独占时长验证保留。
2. Bone J在真实Health接收前判姿态。自然Recover允许受击；真实K投递保留原拉拽并记来源，只有其实际Stunned开放。尚未进入Stunned的待请求、普通取消/睡眠产生的Stunned都不是弱点。
3. 离开Stunned、睡眠、void、死亡及显式Reset清掉旧K开口。Bone与Ghost共享死亡/void目标撤权及延后Monitorable提交，Bone Reset恢复同一身体的活体权限；Ghost显隐/身体碰撞仍沿原合同。
4. 环境void仍直接请求真实Health，不能被姿态防护免死。没有更改原生命、无敌、攻击/收招/K时长、物理模型、碰撞、外力、原Native/null-profile行为或资源所有权。

仅三份生产源、现有测试、runner/owner变更；无新C#源文件。当前1670为source/resource inventory行数，不是1670个C#文件。没有新素材/音频/保存地图/付费生成，也没有为压掉资源错误新增GC、KeepAlive、借用资源Dispose、缓存/退出drain更改、额外退出等待帧或超时/错误门豁免；新测试按原fixture逐帧观察真实动作。

## 原生行为和真实玩家链

| case | 真实检查 |
|---|---|
| guarded_J_then_real_K_stun_opens_and_expires | 原生Area J确实overlap但拒绝；原K使身体实际移动并进入Stunned，J使HP3→2；窗口到期且无敌期过后再拒J |
| natural_attack_recovery_opens_but_new_telegraph_closes | 真实敌人预警/突刺命中玩家HP9→8，正常Recover接受J，新Telegraph关闭；不注入模型状态 |
| sleep_cancels_K_opening_and_generic_stun_is_not_a_weakness | 真实K开口后睡眠90帧不回血、撤J/K；唤醒的通用Stunned拒J，新K才再开口 |
| native_death_withdraws_targets_and_reset_restores_guarded_life | 原生J致死后K发现/已捕获投递撤权；显式Reset恢复HP及同一身体权限，旧开口不继承 |
| actual_void_bypasses_pose_guard_and_withdraws_targets | 去掉真实地板、实际坠入零layer void，死亡并撤目标；不触发玩家恢复 |
| invalid_profiles_reject_atomically_and_Bone_never_runs_Ghost_clock | 无效profile原子拒绝后仍能配置合法Bone；90帧无Ghost隐藏/alpha变化，缺K Settings拒绝而不抛异常 |
| saved_player_physical_K_then_J_fits_the_real_opening | 实际保存Player、输入采样、MechanicalClaw、保存敌人adapter和PlayerCombat连通；K1/J1/native connected1，HP3→2 |

第七项使用引擎`Input.ParseInputEvent`的物理键码事件，不是OS真人按键；回执明确`OS_origin=false`。K/J时间参数、HP、pose均未注入或放宽，J原0.06秒startup、K原0.25秒Stunned保留。前六项的来袭伤害使用真实Area/Health与原K投递，不扩大成真人输入证明。

## RED与实现阶段，分别保留

以下均在`E:/OurGame/Knight/.evidence/october13-stable-claw/`，前两案不能混入最终同源码回归。

| case目录 | 引擎ms/exit | build ms | 观察与严格结果 |
|---|---:|---:|---|
| resident-family-bone-wanderer-20261016-230415-806-ab88662bca7a40f1bac15c6cbfd28506 | 703/1 | 21313 | 六项失败：前五合法Bone配置未支持、第六配置拒绝断言失败；stdout4 Shape RID，严格FAIL |
| resident-family-bone-wanderer-20261016-230728-768-7fbb02ba002c45dea10c78481d621436 | 7367/0 | 21736 | 最小生产实现后六项行为符合，但stdout4 Shape RID，严格FAIL |
| resident-family-bone-wanderer-20261016-231501-744-6422f4290a0244959ede7844ca0d63a4 | 7948/0 | 24018 | 七项行为含保存Player K→J符合，但stdout7 Shape RID，严格FAIL |

baseline第六项的日志没有逐项打印被接受的无效profile；根据当时测试列表顺序及校验源码，定位为Bone/null组合被接受，这是源码推理，不冒充独立命名回执。第一轮owner补丁因hunk顺序错误原子失败，排序后应用、AST解析通过；无引擎消费者期间改动，没有生产回退或漏跑RED。

三阶段source SHA分别`8FEC4EEE3BF12E204F0749D641D643AA9A7A21EE2E08070314F3C813C72288C7`／`2446C68EB74BA9153963072A64E75ABE43781860B5AFECB1A6B473AD2C91FDD0`／`30B3F5B1CACFD426E0AA58CBBBB89906E5C5961DD99B87FBFA7B52631918DF6C`。各案五组custody相同。前两helper9 SHA`9664AEA0816F78646E1F1F42E9B75EE4B4927D9E9678AD9E59B0ACAA6CC93723`，final helper见下表。

清理只按实际输出记录：三阶段6／30／31条owned Shape释放行，最后31个ID不同，七次borrowed inventory；没有aggregate native_gone/allocated/disposed/borrowed证书，不补造。fixture原有actor无效断言保留。4→7的数量变化不能指认匿名资源或证明借用形状泄漏，既有错误仍严格否决。

## 最终同源码11门

| case目录（同证据根） | engine实际/上限ms | fresh Rebuild实际ms | 严格结果 |
|---|---:|---:|---|
| resident-family-bone-wanderer-20261016-231501-744-6422f4290a0244959ede7844ca0d63a4 | 7948/55000 | 24018 | FAIL，七项行为符合，stdout7 Shape RID |
| resident-family-ghost-20261016-231735-265-99984a13131241c4a68214d339b9d822 | 6467/55000 | 22035 | FAIL，旧三项行为符合，stdout4 Shape RID |
| resident-family-bone-20261016-231810-502-3b4157eebe114481bcc00afbd24c13c5 | 3665/55000 | 23679 | FAIL，旧Heavy四项符合，stdout4 Shape RID |
| resident-patrol-20261016-231845-105-98d0efaccb394109bc951e7e8dc49f4a | 44875/55000 | 23885 | GREEN，7模型＋15原生及原Rust模型/Sight综合 |
| resident-void-20261016-232000-652-f329cc6c62e4460381b00ce4eb623d8f | 13595/55000 | 23750 | GREEN，原14实际环境控制 |
| resident-void-stream-20261016-232044-364-5db1c0661dca4948a47911a9df380635 | 2335/55000 | 31211 | GREEN，原10流送身份控制，npc_motion=false |
| resident-allocation-20261016-232125-607-a15a877c543542f498d33c859d42f93e | 419/55000 | 24222 | GREEN，名册/17负控；仍spawned0/readiness未解 |
| keyboard-20261016-232157-339-5bc96bc446fb4219a26d5f88ba3891c0 | 9580/55000 | 23432 | GREEN |
| actions-20261016-232236-225-5cb659aa42a3431588d856c6b31a5022 | 7089/55000 | 26168 | GREEN |
| prologue-pre-20261016-232315-023-b344420156a64d33821ec5b500e70471 | 89118/180000 | 23541 | FAIL，旧Upper lower_to_mid sample0.5及2个PresentationProfile警告 |
| prologue-post-20261016-232514-177-701c42fe96c44641b2f1337e1b4e4edb | 8202/180000 | 21953 | FAIL，旧BellChain落地及Recovery路径 |

6 GREEN／5 FAIL。最终11次fresh Rebuild均180000ms上限、exit0/0警告/0错误、build stderr空；无超时、guard failure、expected failure、诊断symbol、verbose、fixed-fps或movie。全部headless、human/naturalRoute/wholeGoalAccepted=false。

pre已返回BASIN_SUPPLY_RUNTIME_OK，随后Upper末点(264.99634,160.6107)、149/150帧、目标区crossings0；post末点(540.0704,1196.0139)，KillZone未解析`../../../../../..`的ILevelRecovery。本次没有Handle/FATAL，不是历史原生崩溃修复。pre/post只有failure drain，不是phase成功。

## 保管链和当前源

最终11案55/55组source/user/helper/binary/continuation前后相同。source1670 SHA`30B3F5B1CACFD426E0AA58CBBBB89906E5C5961DD99B87FBFA7B52631918DF6C`；binary6 SHA`AEC4D4520C414039B5372BB9C248CF5C2F3D99E129B05D4C454EAC5ECB88849B`。protected109与continuation1保持原SHA，真实userdata只hash保护，不读写语义内容。

pre helper10 SHA`50F86517802290FD978A2E4D6AF56A6B2502B5C229C5B9CC9ACEE5E4448EB75A`；其余10案helper9 SHA`D001E92648BF41A9E32FB54DA87A7949324217D75EB441441F8187D81FD7390A`。所有消费者终态、owned consumer0后才更新文档。root扫描三个raw、构建/等待/调用/保管链，长轨迹有限展开；另有只读独立证据复核，不是代码或整计划批准。

| 当前源文件 | SHA256 |
|---|---|
| scripts/enemies/RustShellSquire.cs | 9C3D9AC6E253CF4AA7DEF0DE6E6B75E684D931F2880FBA2047C7ED7A1358CD78 |
| scripts/enemies/RustShellGhostResidence.cs | 9A0C4BDB0602E91747184417A332723E15C9A61D54FB4E597273105DABF24D77 |
| scripts/prologue/residents/ResidentPopulationDefinition.cs | 0250961DF232555BE57E2E2E31837C618E01F77853568678DBEFF369D74D25D8 |
| tests/integration/ResidentPatrolRuntimeTests.cs | 3931E635D277E1E8185DB19518ADBAF3F641D980508DD6ED17A71DD99381E369 |
| tests/RunStableClawPullTests.cs | F1AAC92E684E465CDD7452F00561D22A69BB0ACC58C782060F8810CDBA067E88 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 90DF9B36D44262A6697A3D7E6D36ADF371D54F68C0A3433FB320E90AF5A54583 |

上轮原件未覆盖：mount-only v001 SHA`D5AF2808FDB469366DCA7B04439C40B5C9C33F51824D44580648D2B8BE361E1F`，Supply isolation v001 SHA`5C4DF2EAEAA0AD12B5B4D901046CEA4355EBACC74C0710B1054A1E682CEE2763`，Bone/Ghost v001 SHA`24E7FF5E61AB42984875454F7FE29581467DD0AA769442743C4F565CDB7423DC`。没有Git写入/版本发布/自动化/安装/删除或新媒体。

## 下一制作入口与未完成范围

下一实际Bone Skitter：精确判真实Recover，不把仍在后退的Retreat（虽同为Recovery timeline）当静止弱点；真实K来源与sleep取消产生的Stunned分开，保留原0.60秒K、HP2、600速度限幅、外力/冲退/返航。先原生Area与真实K负控，再最小生产实现。saved void及sleep/死亡/Reset权限不减，不能用void fallback“返回成功”冒充末端bool撤权。只读源码准备已完成，尚未实现或运行该策略。

剩余五个骨骸/幽灵承载为Bone Skitter/Moth、Ghost Moth Flyer/Diver/Sentry，不是仍六个。之后24正式profile/场景、全部50真实身体/活动/攻击/返航/玩家通路与跨区manager。当前正式新增仍0、50 Placement全null、24 SceneState Unimplemented/ScenePath null；Bone10/Ghost10和旧26＋新50＝76普通＋1Boss不变，ECHO1人格/独立友方身体0/商人0。

Task1最终50点、整个Task2和Task3–4保持OPEN；资源、旧档、正常Main/Explore同角色Boss与有支撑出口、最终围合/家族美术/视听、可见稳定60帧、真人反馈和审阅归档录像全部仍是完整目标的门。目标ACTIVE，无完成、整计划批准或十万付费成果声明。
