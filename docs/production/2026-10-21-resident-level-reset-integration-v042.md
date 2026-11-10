# KNIGHT 正式 Level 常驻重置接线 v042

2026-10-21。内部实施记录，不是发行版。资源总目录 `G:/knight`，实际 Godot 工程 `G:/knight/Knight`。完整 1–11 目标仍 ACTIVE；正式新居民 **0/50**，24 canonical 场景仍 Unimplemented，50 个 Placement 仍 null。

## 事实：这次实际改了什么

在 [v041](2026-10-21-resident-lifecycle-and-health-custody-v041.md) 的状态/真实 HP 保管上继续实际接线，而非新增一个与正式游戏隔离的模型：

- `PrologueLevelController` 在真实 seamless World Ready 后建立唯一 `CanyonResidentManager`；退出时幂等释放。实际 Flow.StartNew 与成功的 Progress.Restore 后调用新轮次入口。无效 schema 必须先被拒绝，不能提前重置敌人或推进 epoch。
- manager 用 World 上的自有字符串标记拒绝第二个 owner；Dispose 仅移除自己的标记。新轮次只对已接管、仍有效的 opt-in 居民调用真实 ResetForCheckpoint 一次，再接管新 lease；不扩大原26波次敌人的重置范围。
- `SeamlessPrologueWorld` 在原来的实际 ResetDescendants 边界发出内部 begin/finished 通知；保留原目的区优先、同实例不重复、departing 必须仍挂载且实例相符的选择规则。manager 在 begin 解绑、清相应区域标量状态，在成功 finished 重新接管，**不会再次调用 actor reset**。
- `ResidentPopulationState.ResetSector` 只清指定区域 alive/HP/lease、递增 ResetRevision，保留当前 run epoch 与单调 lease generation。未参与恢复的邻区不被重生；HP准备来源加入 revision，拒绝过期准备的设计边界不再只依赖 epoch。
- 新增实际 `ResidentLevelLifecycleRuntimeTests`；其余改动为原状态/生命周期测试、runner 和规范 owner 的严格阶段准入。新增一个实际 C# 文件，受保护源/资源清单为 **1700 项，其中 870 C#**；不是1700个 C# 文件。

没有正式出生事务、正式资源发布、关卡人口增加或新视觉素材。本轮 Level 的 manager 已真实存在，但 canonical 尚不能出生，不能把接线说成50只完成。

## 真实边界与反例

新原生夹具使用保存的正式 Level、真实 Flow、World 和 Player Health；隔离存储、复制后自有碰撞形状、冻结 fixture 敌人 AI，未改变正式地图/身体/输入/原等待与清理策略。fixture 的直接定位用于生命周期测试，**不是自然通关证据**。

1. 实际 Flow.StartNew：受伤 Rust HP2 经真实 reset 回 HP3，epoch1→2，reset发布1次；无效 schema999 保持 HP2/epoch2/次数1；有效隔离存档恢复到 epoch3、HP3/次数2。六个真实已知残片 ID 保留，输入 seed 序列化字节不变，无伪造豁免。
2. 真实玩家致死恢复，分别测试同区、departing仍保留、departing已卸载三种。RecoveryCompleted 回调只记录，证明确实早于延迟敌人重置，不能在该事件上抢跑名册。
3. 原生 reset probe、真实 actor reset发布/HP、所有50 catalog槽位 revision交叉核对：目的实例均1次；仍有效 source1次，已卸载 source0次；其余/退休/新邻实例均0次，epoch不变。这里“50槽位”是选择器覆盖，不是50个出生对象。
4. 新 `resident-save-compatibility` 调用**未改动**的 QuestFragmentProgress、BossAdmissionModel 与 QuestFragmentPersistenceRuntimeTests。源内断言覆盖独立9个残片ID/5拒6准入、隔离磁盘 Continue、不改输入旧档、schema3合法旧档迁移/豁免、死亡保留、新游戏清除。日志的完成标记单独不包含全部字段，应与实际执行的原测试源一起解读；非真实用户存档、接触拾取或正常路线证明。

原始 RED/GREEN 均在 `G:/knight/验证证据/october13-stable-claw`，不得删除：state `215010` sector reset no-op → `215304`10案通过；lifecycle `215048`重叠 owner获准 → `215333`拒绝；Level `215806`未接 manager → `215930`仅清名册未重置HP → `220115`坏档提前重置 → `220245`新游戏/读档通过；`220502`真实死亡后revision仍0 → `220715`三实际恢复情形通过。原存档兼容 `221203`通过是补覆盖，不冒称修复原存档代码。

## 同冻最终11门：10 PASS / 1 strictFAIL

规范 owner 串行 fresh rebuild，所有构建 exit0/0警告/0错误；没有超时或 guard 漂移。源、helper、用户文件、二进制和 continuation 的 **55/55 对**前后清单一致，原生 PID/contract/wait/provenance 相符。所有消费者终态后才更新本报告。

| 阶段 | 严格结果 | engine ms | 完整 case ID |
| --- | --- | ---: | --- |
| state10＋原Health3 | PASS | 516 | `resident-state-20261021-221824-475-61de8a3c9e4f4f8dbdd73ec968b647d1` |
| 真实生命周期/HP | PASS | 1543 | `resident-lifecycle-20261021-221854-135-364e21f6bd60453fadc739d75bc36e46` |
| 实际Level/新轮/读档/三死亡 | PASS | 2736 | `resident-level-lifecycle-20261021-221921-749-a199cd3f376341608298af80a108573c` |
| 原碎片/Boss模型/存档兼容 | PASS | 2639 | `resident-save-compatibility-20261021-221951-966-95ab7ec8502c4d81a7aa1dbf5ea6880f` |
| 原流式来源 | PASS | 1359 | `resident-void-stream-20261021-222019-928-63ebd14989054028a7dd1e462abef53e` |
| 完整名册/未就绪拒绝 | PASS | 505 | `resident-allocation-20261021-222046-390-df56bf22502743faa71983cb710d4d51` |
| 原检查点生命周期 | PASS | 1261 | `checkpoint-lifecycle-20261021-222112-787-b4ef544b78624e8e96ca6c56fe97e872` |
| 键盘 | PASS | 9594 | `keyboard-20261021-222138-645-c165f8461ab640da9338c55539c9ae2b` |
| 动作 | PASS | 6989 | `actions-20261021-222212-859-b9777772e8f541d09d4bf09eca8d49f0` |
| 完整pre | **FAIL** | 30786 | `prologue-pre-20261021-222244-040-1048079704f84a7aac44025fc3e8ac6d` |
| 完整post | PASS | 147278 | `prologue-post-20261021-222341-960-893dd74084424237a24d7d1fbf6ef40d` |

共同 source manifest SHA256：`7E2C48C69175C4D9B6A816E6977E36F70F009869C9ECBC1CD3390115A2A14603`。共同9 helper manifest：`6DAB42C7CFE45E4F0F05C14713317EACE37B6C3624DF5C1DD3E487CF62E8B390`；pre仅多出原、未变的 clearance admission helper。user109/binary6/continuation1跨案一致；真实用户文件只校验哈希，不读语义。

### pre 是真实原生崩溃，未修复

engine exit **-1073741795 / 0xC000001D**，不是超时。stderr有两条 ERROR，external镜像同两条，不是四个独立事件：

- `FATAL gchandle.is_released()`，`mono_object_disposed_baseref:1788 → GodotObject.Dispose/Finalize → GC.RunFinalizers`。
- World.Ready校验发现 Definitions[0..3] 为 null，栈到 `DescentClawGuidanceRuntimeTests.Run → AddChild:120`，其上一个完整套件为 BasinSupply。

最后 stdout 为 `BASIN_SUPPLY_RUNTIME_OK`；pre没有阶段完成或退出drain完成标记。两故障的先后因果、具体 native资源/包装器拥有者未证实，不能称是 manager.Dispose、allocation无关、引擎外因或某个借用资源导致。已有只读 trace 的 opt-in 在本案未开启；当前 Descent/BasinSupply 文件仍逐字节匹配本冻清单，没有测试期间源漂移。V041的五条Handle失败原件继续有效，不能用本案症状不同或post通过撤销。

### post 的通过边界

原 registry 75 executed/classified、0 retired，替换2/退休0；原同owner主场景8次实例化，节点1→1。四条受控负面 WARNING 按 seamless1、room-profile1、flow-registry2核验，external只镜像，不计八条；无 ERROR/Handle/gchandle/ObjectDB/RID/leak/resource-in-use诊断。不是“全日志零警告”，也不替代自然 Main/Explore、最终视听、完整可访问性、真人或60FPS。

### 矩阵后的单次身份诊断：仍 strictFAIL，不是补跑通过

所有11门终态后，仅开启既有 `-TraceSectorDefinitions`，没有源/helper修订或新诊断代码。唯一诊断案 `prologue-pre-sector-identities-20261021-223341-981-b64bdd47fb0e4f01aadc1193e7fad66b`：native exit0/100247ms、无超时/guard，**stderr与external镜像同6条Handle错误，严格失败**。六栈均在 `SwapGCHandleForType → Descent.AddChild:120`；不是6个已识别资源。没有本案FATAL/Definitions null，不表示原崩溃修复；即使阶段完成标记存在也不覆盖错误，provenance明确 behaviorAccepted=false。

五次Basin实例替换前、Descent source-pre/instance-immediate/source-post/入树前，五项定义均 count5、非null、IsInstanceValid=true，并保留同五个native ID：`9223372083377997702 / 9223372083394774919 / 9223372083411552136 / 9223372083428329353 / 9223372083445106570`。本案没有复现“源或实例定义已缺失”的分支；**IsInstanceValid不能证明托管GCHandle仍完好**，而getter本身可能扰动包装器寿命。因此仍未识别被finalizer处理的具体对象，不据此添加引用保活、额外等待或借用Dispose。

诊断与最终矩阵使用同1700源清单；终态后再次逐文件核对当前1700项及共同9 helper，漂移均0。规范owner AST0，四阶段named-waiver拒绝测试通过且不启动引擎。HEAD/index未改变。媒体手动检查4838bytes、新增1330bytes/未到20GiB，无新媒体/自动化。

## 推理与风险

把名册同步绑到真实 reset，而非较早的恢复事件，避免恢复后保存旧伤势或重复重置。代价：异常/重入/正式出生事务仍需进一步覆盖；同轮旧准备 revision虽已纳入身份，完整 native负控未补齐。新游戏/有效读档只重置当前已接管对象，未来正式装载器必须自行完成新出生/卸载后重挂，不能误认为当前接口已完成全50管理。

本轮是实际进展，但完整回归未通过。没有调整GC、缓存、drain、等待、借用 Dispose 或伤害/地图/输入来凑通过，也没有原样重复失败直到偶然绿。根因不明时继续收集区分性标量证据，不先修改资源所有权。

## 剩余与下一步

1. 定位pre原生 RefCounted/GCHandle 生命周期失败：先核对 BasinSupply→Descent 实际资源交接及现有诊断边界，区分源PackedScene、实例Definitions和退出包装器，不能仅凭堆栈/ID配对认定拥有者。
2. 正式原子出生、7处空间缺口、全50实际身体/活动/接近/保护区/身份历史、全9类型HP与幽灵姿态连续性、24正式族系资产仍OPEN；不减原26+新50=76普通+1Boss/骨骸10/幽灵10。
3. 历史154/31等资源失败、[v040恢复音频快速退出3WAV＋3PlaybackWAV](2026-10-21-current-claw-and-post-regression-v040.md)、另一Task10演出/双语入口仍OPEN。本轮未改/重跑音频，不宣布关闭。
4. 最终原创美术/UI/动作/VFX/音频/演出、丰富机关、同冻正常Main/Explore、旧档整体隔离、720p–4K/真实60FPS/连续视听与真人、唯一Windows ZIP与经授权GitHub归档仍待完成。全计划Tasks1–4未完成，无task-done或整计划通过标记。

没有新展示图/录像或付费素材，无自动化、前台模拟按键、Git写入、删除、安装升级、ZIP/发行标签。关键媒体仍只在 `G:/knight/过程图稿与视频` 按节点留档，不恢复已删除的 `G:/OurGameArchive`。
