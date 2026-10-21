# 短喷推进器、力学与资源接入口调查 · v001

2026-10-18，Asia/Shanghai。权威项目：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。本报告是本轮只读源码调查和需求记录，不是已批准设计规格、实现计划或新运行验收。用户原意见[U-20261018-001](../玩家修改意见与世界设定.md)。未修改游戏代码、正式地图、资产状态或用户存档，未构建／运行引擎、付费生成、发布、恢复自动化或操作用户键鼠。

## 事实：当前运动不是短喷推力

| 项目 | 当前源码事实 | 依据 |
| --- | --- | --- |
| Shift | 按下边缘触发，冲刺活动帧直接指定方向×620px/s，名义0.14s；绕过普通重力和制动 | `scripts/player/PlayerMotionModel.cs:40`、55–63；`PlayerInputReader.cs:112` |
| 跑动与惯性 | 跑速260；地面加速2200、空中1500；地面／空中松键均用2800减速，没有独立空阻或主动反向制动参数 | `scripts/config/PlayerTuning.cs:10`；`PlayerMotionModel.cs:86` |
| 向上余速 | 普通松Space会将上升速度硬截到-230；冲刺结束后也可能触发，不可让它误截推进器的上升余速 | `PlayerMotionModel.cs:72` |
| 重力／跳跃 | 重力1850px/s²、终端下降780px/s、跳跃590px/s；土狼时间0.10s、跳跃缓存0.12s，实际Player Tuning子资源未覆盖默认值 | `PlayerTuning.cs:10`；`scenes/player/Player.tscn:25` |
| 落地 | Motor使用MoveAndSlide，实际碰撞速度差写回运动模型；硬落地430px/s／0.16s目前属于表现，不是受力缓冲 | `scripts/player/PlayerMotor.cs:108`；`scripts/presentation/PlayerPresentationCoordinator.cs:39`、183 |
| 摔伤 | 依据连续落差，安全600px，此后每400px向上取整伤害；不是按撞地速度计算 | `scripts/player/PlayerFallDamageModel.cs:29`、70 |
| 冲刺预算 | 一个AirDashAvailable，仅真实空中→落地边缘恢复；既有测试防止抓爪、墙顶、暂停、反弹和换区暗中补回 | `PlayerMotionModel.cs:30`；`tests/unit/PlayerMotionModelTests.cs:245`、391；`tests/integration/PlayerTraversalInputTests.cs:22` |
| 能量／无敌 | Dash不耗能、不积热，也没有专属无敌。0.35s是受伤后的无敌，不是冲刺无敌 | `scripts/player/PlayerEnergyController.cs:34`；`scripts/combat/HealthComponent.cs:20` |

目前空中松键的被动减速大于按反向键的加速度，因此现状与用户希望“反向键更有效制动”不一致。上述参数是源码值，不是本轮实测距离／时间；不能以620×0.14直接声明实际位移，因为离散步进、碰撞与后续运动仍参与。

项目固定物理频率60Hz。本机固定版本GodotSharp.xml的CharacterBody2D说明确认该类是用户控制的运动／碰撞体，速度单位为px/s，力与重力需要控制器管理；MoveAndSlide会处理墙、坡和碰撞。位置在`G:/godot/Godot_v4.7.1-stable_mono_win64/GodotSharp/Api/Debug/GodotSharp.xml:77114`。因此建议在现有单一Motor中增加受力规则，不因“更真实”就直接更换引擎或改成RigidBody2D。现实重力数值不能不经长度尺度换算直接写入像素单位。

## 推理：必须保留运动所有权与状态边界

- Motor已分离自由速度和抓爪绝对修正；自动牵引时不能重复叠加第二份跑动／重力，摆荡另有径向约束。依据`PlayerMotor.cs:87`、144。
- 抓爪释放从实际身体速度起算，一次添加boost，剔除朝墙内分量并限合速度900。保留动量标志还影响JumpCut、上升气流和部分相机，不可直接当推进器惯性标志。依据`PlayerMotor.cs:161`；`scripts/prologue/seamless/SeamlessPrologueWorld.cs:865`。
- `ExternalImpulse`实际是替换整个速度、取消Dash并清除抓爪释放状态，不是增量加力。推进器不能逐帧借此灌入小加速度。依据`PlayerMotor.cs:191`。
- 输入优先级为盾、Dash、附着Space、攻击、爪、Q；即使盾过热，持续L仍会吞Dash。盾倍率0.65只缩MoveAxis，不缩当前Dash。推进器与盾的关系要写明，不能仅更换视觉。依据`scripts/player/PlayerActionCommandModel.cs:16`；`PlayerShieldController.cs:127`。
- 现有回归要求新Dash覆盖抓爪释放速度等。推力模型若有意改变旧契约，需要明确升级并真实重验，不把旧断言删掉当通过。依据`PlayerMotionModelTests.cs:317`；`tests/integration/MechanicalClawRuntimeTests.cs:495`。
- 当前受伤impulse主要被Health信号／表现消费，本次未找到PlayerMotor订阅它产生真实击退的证据；将全部外力统一属于额外明确的行为变更，不能顺带冒称已有。

建议的力学方向尚未批准：有限时推力改变速度、喷后保留余速、地面摩擦与空中阻力分开、反向键以有限制动力先减速再反向；重力、碰撞落地、短跳、抓爪和外力采用一致的状态交接。质量／长度尺度、推力曲线、停止距离、喷射方向、次数、能量／热量、伤害兼容都还不是已定数值。

## 事实：未来可换模块目前没有完整接入口

装备1–4目前只选槽号；攻击硬编码Slot1，HUD占用硬编码为true／false／false／false。没有发现推进器定义、通用背包或模块换装系统。依据`PlayerActionModeController.cs:31`、77；`scripts/ui/PrologueFormalHud.cs:327`。

专用任务残片的拾取有“稳定ID→先持久化成功→再改live进度”事务可借鉴，但它绑定QuestFragmentCatalog，不是装备拾取。依据`scripts/prologue/collections/QuestFragment.cs:76`；`PrologueGameFlowController.cs:327`。

保存schema4没有已拥有／已装备模块；Normalize重建DTO，未来不能只加属性而漏复制／校验／迁移。Tuning在Motor Ready时转成不可变model settings，运行中仅替换Tuning属性不会使模块生效。依据`scripts/save/PrologueSaveData.cs:15`、101；`PrologueSaveStore.cs:83`；Motor初始化。

用户所说“未来拾取更换”先作为扩展意向保留，建议首先设计移动模块ID／参数与显式装配边界，不自动扩大为本轮完整背包、掉落、换装UI或存档迁移；Q仍是环境交互，原暂缓道具使用决策不静默取消。

## 事实：资源清单不等于实际运行消费

正式入口仍`project.godot:6`所指的PrologueLevel，五区引用仍是正式原版；上轮候选父快照和132小地图并未覆盖正式127。24子型默认Unimplemented／ScenePath=null，50身份仍未传Placement。依据`ResidentPopulationDefinition.cs:22`；`tools/build/prologue/CanyonResidentCatalog.cs:203`。

候选builder和pre-Ready factory存在，但叶是ExplicitPlaceholder，默认装载拒绝占位并要求点位；本轮在scripts／tools生产源码范围未找到正式factory调用。manager和population state仍是计划待创建，不因文件名出现在计划就算实现。依据`scripts/prologue/residents/ResidentSceneBinding.cs:12`及居民计划Task3。

Production manifest八类均final=true、文件存在，现有文件门实际为File.Exists，不能代替像素、事件、许可或真人验收。依据`scripts/presentation/ProductionAssetManifest.cs:27`。可确认运行加载有：

- 玩家动画JSON：`PlayerFormalVisual.cs:675`调用PlayerAnimationMap.Load。
- 音频manifest／WAV：`scripts/audio/PrologueAudioDirector.cs:104`、228实际加载AudioStream。
- 本地化：`scripts/ui/PrologueUiRoot.cs:30`。
- Fog shader：`scripts/presentation/fog/PrologueFogCanvas.cs:27`。

PlayerFormalLayer与EnemyFormalVisualPart仍有程序几何绘制，分别见`:56`及`:55`。敌atlas在场景有metadata引用，本轮未找到实际像素消费证据；不声称绝不使用，也不凭metadata声称最终美术已接入。UI／VFX／Audio节点存在同样不足以证明清单里每个PNG或theme JSON已被实际消费。

“全部资源”应沿同版五叶→父快照→catalog、24子型profile／承载、合法50点位／活动／manager及真实视听消费者逐批接入。不能默认包括第三方参考图、过程稿／失败录像、负例夹具、不同版本专用产物、未许可来源或未批准付费素材。manifest的Project-owned是现有声明，本次未独立验证权利来源。

## 风险与当前检查点

1. 新加减速会改变每条路线的可达与停止距离；移动平台、抓爪、井中气流、墙顶及落地必须实际重验，不能只改单元测试数值。
2. 若真实推进制动后缓慢落地仍按原高度扣血，会出现不符合新直觉的结果。需明确摔伤规则的兼容或升级，不能按下技能即免伤。
3. 不从“推进器”自动推导额外空中次数、无敌、能量／热量成本或无限飞行。这些是玩法决策，不是物理定律。
4. 上轮五项严格FAIL仍开放：冰区三Handle、叶154Shape RID、profile31Shape RID、旧Upper＋两profile警告、旧Bell／Recovery。正常全关、旧档、显示帧率、最终视听、真人和录像门仍需验收。
5. 保留旧26＋新50＝76普通＋1Boss、骨骸10／幽灵10及ECHO一个引导身份，43候选／7未解不是正式出生数量。

首先需要用户明确的操作选择：把Shift冲刺升级为短喷推进器，还是保留Shift并另外增加一种推进装备。建议前者以避免重复位移／按键／空中预算冲突；当前尚未批准。架构级设计梳理先停在这个功能选择，不进入产品实现或付费生成。调查后两个只读agent均已完成；根只维护意见与README／本报告，未以源代码调查冒充新物理运行结果。
