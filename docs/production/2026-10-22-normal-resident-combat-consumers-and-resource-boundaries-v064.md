# v064：常驻敌人的正常主线消费与资源故障边界

2026-10-22。完整第1–11部分、居民Tasks1–4仍ACTIVE；实际工程G:/knight/Knight，总根G:/knight。不是正式试玩、最终视听或发行完成。上一轮引擎建议只作决策核验，本轮继续Godot，未迁UE。

## 实际变动与边界

- 测试侧Task10NormalRangedEvidence不再将所有RangedWeaponEnemy当旧波次。新常驻必须唯一匹配作者出生点、实际mounted sector与Gameplay/Actors、单一binding、精确保存scene/profile/subtype/family/archetype/carrier；与旧波次身份互斥，未知/重复仍严格失败。NodeAdded早于manager提交lease，故不在这个时点冒认Attached。
- 原波次的真实类型前摇→发射→伤害→玩家命中→死亡→group完成证明保持。常驻使用独立有界值记录与RANGED_RESIDENT事件，不拿挂载、发射或旧wave完成替代全部50实战。
- 旧试玩驱动的危险源和攻击目标同样只按旧名字前缀过滤。新增严格保存常驻的危险感知与同垂直交战带内近处目标选择；没有改生产血量、伤害、数量、AI、物理、地形或900循环预算。**这尚不是成熟的群战驱动**：同版Main仍真实死亡，不能据此宣布地图过难或机制不可过。
- 此阶段没有新美术/音频资产；24开发叶仍明确占位，既有素材与可替换接口保留。

## 五个独立普通主线案例

证据根均在G:/knight/验证证据/october13-stable-claw，每案原始日志/源快照/合同完整保留：

1. 183121-836-6d429ad98f9143068d149493cbb47696：原观察器Single无匹配，已到Playing，第一段WalkTo(300)暴露。预想“12帧开始等待先失败”被实际证据否定，开始等待未改。
2. 190146-935-89a254d1a8284cc7b263b6384e607821：身份分类后通过入口与裂谷两次落台，深井底HP0触发真实死亡重置，随后无传送门拒绝。不是新发现的非法复活传送bug；新增Burst有三发，但不能据此认定它造成致命伤。
3. 191813-862-c185051779ca44daa2b390a45c4450eb：真实Burst警告被旧危险范围漏掉，RANGED_RESIDENT_DANGER_RED，native1／21615ms。这是危险消费修正前的实际RED。
4. 192236-874-566d8f9d05334ec98cc2877f2207f88c：补危险源但仍只追旧敌，裂谷持续举盾/耗能至HP0，native1／24847ms。
5. 192901-832-3882938217314b79b7213528a51fa34f：补近处常驻目标后，真实选中rock-wanderer HP3与rift-flyer HP2→1；裂谷7次J开始、8次连接，仍不能由聚合连接推断每只已击败。Burst两次前摇/三发、Missile一次前摇均在新增断言后记录，原DANGER反例已有正向覆盖。到AbyssSafeBottom完整支撑、HP1，之后真实HP0，native1／38008ms。**完整Main不通过**。

前四个改动中间版本不得与最终冻结矩阵拼成同源通过。上述列含最初183121及后续四版，共5案。

## 当前普通冻结20案：12 PASS／8严格FAIL

19次新回归加保留同源192901 Main失败，不是20次新运行。均1772清单项/888 C#、普通无诊断符号、guards=0、无超时；20案Source清单一致。各行为范围只按对应case解释，存档专项本次通过不能关闭历史资源FATAL。

| 阶段 | 20261022后case标识 | native exit／ms | 严格结果 |
| --- | --- | --- | --- |
| thruster-route-main-diagnostic | 192901-832-3882938217314b79b7213528a51fa34f | 1／38008 | FAIL：真实HP0／零恢复守卫；不是通关 |
| actions | 193831-544-e7aa353a7b0449d5b6cf817f93463503 | 0／7114 | PASS |
| keyboard | 193754-230-26d153cbd9fc46be9e695bfc8cc1b23a | 0／9679 | PASS |
| prologue-post | 194149-279-73ef3de6ddbc4022b7e4261cbe8c7afd | 1／5551 | FAIL：旧相机Motor自动状态断言，未到75案例 |
| prologue-pre | 194111-724-3c366c3af3894db185810527f09d5d32 | -1073741795／10320 | FAIL：gchandle FATAL，未完成 |
| ranged-mixed-policy | 193121-177-9371df926fcb45cd9cfe8e5b0f084642 | 0／3503 | PASS；保留旧组外身份／暂停／退休等负控 |
| ranged-mixed-resource | 193159-854-de05e09fc88c4e75bf0b52baeba51327 | -1073741795／3778 | FAIL：gchandle FATAL，非显式退出GC |
| resident-archive-metadata | 194007-447-3d5f3f322cbf4c7c953cf547a029e108 | 0／4750 | FAIL：287 Shape RID |
| resident-auto-session | 193307-848-7154d1f1cdbc4a77af45899c7a1d8371 | 0／13162 | PASS |
| resident-auto-start | 193236-455-835b741dd1cc4b8086bf321373e09971 | 0／4448 | PASS；保留1条精确预期Intro→Intro拒绝WARNING |
| resident-auto-teardown | 193349-017-d03e32fdada5468caeb41961c93546a5 | 0／5276 | PASS |
| resident-development-published | 193423-089-0295215f39f74a60889ec0232514d2f5 | 0／2443 | PASS |
| resident-level-lifecycle | 193456-761-462c62a7322b407391040fa6f90f1d35 | 0／4717 | PASS |
| resident-lifecycle | 193611-982-f3c1226843114574bcf3123b5c3bef75 | 0／1568 | PASS |
| resident-parent | 193714-975-22646e7a5bb7492aa62b95feac0ca717 | 0／11938 | PASS |
| resident-save-compatibility | 193530-205-6f86c04709494a458dfbd4239fc1df40 | 0／14550 | PASS |
| resident-scene-binding | 193905-736-80ba8e06e7c045eaa52fabc1ab700563 | 0／7834 | FAIL：138 Shape RID |
| resident-state | 193643-027-369c31ad25a347d39ebc610afadec4a1 | 0／567 | PASS |
| resident-steam-metadata | 194038-730-2810ba3c385c458aa7229631eb1a7d82 | 0／5643 | FAIL：206 Shape RID |
| resident-subtype-profile | 193939-943-843aba7045b14bfb9ef20c6464007d36 | 0／791 | FAIL：31 Shape RID |

auto-start的一条WARNING是实际重入拒绝负测，由原精确消息/来源/函数/数量分类器核验，不是零WARNING运行。post在旧相机夹具PlayerMotor断言停止，scope收据0，不能继承历史75案例。四项RID都在原始输出中，即使native0仍FAIL。

冻结锚点：

- Source manifest SHA256：BD114C77DA2A6FA6EFE1D57606AFC15D563834FC5BA9E7DFFAE40B48D0DC7372
- 六binary manifest：D30D787F2E2658F50AA531178805F9A6392345E132288CD060EB158E66AF0B3B
- Knight.dll：3B381BA9ABF47DC38B87D0AD1CEAB3C5849AE5B21E236D94B1EBDFB11BE986CF
- HEAD：e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
- Git index：21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C（无Git写入）

独立只读审计核对100对清单、9090份选定源码副本、206份helper副本及120份binary，全部匹配；不是1772项均有文件副本的证明。40个build/native合同、实际PID/argv/wait一致，20次构建均无warning/error/诊断编译符号；封存共同DLL中所查诊断类型为0。原真实userdata109项只核哈希，不读存档语义；共同manifest为BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB。基础9项helper manifest为8EE153FDA22C850536E3D26F2C955730C8FD0078252580777C1BED5C67FE321A；ranged/pre额外专用helper单独核验，不冒称所有helper清单字节相同。

所有消费者终止后，root对当前1772源码项、9个基础helper、6个普通Debug产物逐项回读与当前Main冻结对照，差异0；文档更新后diff --check通过，HEAD/index未变，无自有Godot进程。媒体工作流一次检查Current4838B／Baseline3508B／新增1330B，Notify=false，没有达到20GiB新档位，也未建立任何调度器。

## 资源调查：有线索，没有修复或共同根因证明

180200-091-53520c79e3ed4e168b1936e46781b64c是独立REFCOUNTED诊断：native−1073741795／2255ms；48 ENTER、47 RETURN，唯一未返回为PresentationProfile serial16、native9223372143205551908。它在op2/11/20/29被观察为同一assigned Moth Profile，probe.Free后仍活着。不是旧serial476 Tuning；不可跨进程合并成一个对象。

op38确为Moth而Tuning=null，op44确为Prism而Settings=null；两条都发生在FATAL之后，只排除根包装类型不匹配，不证明null是先因。精确引擎csharp_script.cpp来源commit a13da4feb8d8aefc283c3763d33a2f170a18d541、86659B、SHA256 6E1A6ED8C6F67A7FE8BB6DD2598DD763C2FD10F0158D777F7F043A86A19C994D；1788检查released handle，后续可能重建managed包装的分支存在，不证明本案发生了那个竞态。只读拥有者审计未找到明确误Dispose该Moth Tuning/Profile的应用调用，不据此归罪引擎。

诊断专用RunDetachedEnemyResourceRepro：9保存原始叶×48轮=432离树根，只Load/Instantiate/Free，没有World/Flow/Ready/物理查询；不加GC，仍保留原退出drain。181619-f6faa8b7（1643ms）安静；182315-5af853e5只额外持有未实例化的父PackedScene（2116ms）也安静。两案测试源码冻结不同，不是同源配对，也不是生产cache修补，更不能关闭普通故障。诊断phase只接受相应symbol，不输出普通成功凭据；最终普通构建由后续正常owner重建。

当前ranged-mixed-resource 193159-de05e09f又在普通旧敌混编出现FATAL：直接Ascent＋Player＋wave，没有Level/ResidentManager/原出生探针，且runner显式跳过退出GC排空；第一轮行为标记已出，整个runner未完成。这说明“此类FATAL必须先运行original-spawn探针”不成立，**不证明它与原案同根因**。目前缺首轮finally与下一轮构造间的精确边界，不能猜是哪一行释放。

当前pre与mixed-resource的stderr字节相同，仅提供同一FATAL签名／堆栈的证据；两案Handle-is-not-initialized均0，不能借旧案数量或相同堆栈推成共同对象／共同根因。

## 下一步与尚未完成

1. 在真实Health.Damaged同步回调沿用现有EarlyDeath只读_processedAttackOrder.Last()模式，取得真正扣血的source/localAttackId；与Hitbox.Connected或Ranged ShotEmitted ordinal精确关联，保留未知归因。完全格挡的Connected不当生命损失，Scatter最多归因到齐射，不猜单颗弹。
2. 依据真实扣血、盾方向、能量和J/K/L消费完善多来源群战；保持旧wave全部证明/零恢复/完整身体与原预算。当前nearest与危险感知只是接入，不是群战或全部50合格。
3. 最小资源实验只改变旧混编执行一轮或进入下一fixture，用相同纯标量边界标记区分清理/再构造；不改GC/cache/KeepAlive/借用Dispose，不无修改反复碰运气。
4. 复用合法Continue与真实键盘路线验证旧相机采样，不能用两处会碰居民保护域的旧预置坐标、禁敌或FinalOnly冒认正常相机。
5. 最终族系美术、场景、UI、动作/VFX/音频、全部机关、完整Main/Explore/Boss/实体出口、真人键盘、正常显示60FPS、720p–4K、同版ZIP及授权GitHub归档继续按总待办完成，不缩范围。

无新付费、自动化、OS按键/抢前台、旧资料删除、引擎升级、Git写入或试玩发行。普通调试不新增图稿/录像；过程媒体仍G:/knight/过程图稿与视频，20GiB只随正常汇报提醒且不停工。

