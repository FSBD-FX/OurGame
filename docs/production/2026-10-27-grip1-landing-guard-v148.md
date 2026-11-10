# v148 · Grip1合法落地防守与异常撤键

2026-10-27，G:/knight/Knight。原十一项目标ACTIVE/NOTSEEN。本轮是自动试玩驾驶与验证进展，不是新增玩家能力、美术或发行版本；普通键位、物理、敌人、难度、地图和资源释放政策不变。

## 事实、推理、风险

上一轮最终Explore979a抓取已成功，随后无盾下降，被counterweight-lift.01骨骸近战attack1击中，HP1→0、energy1/heat0，原300帧中第94帧失败。旧danger状态891帧前，不证明没有预警。实际冲量支持几乎正下方来源，但不能由它推定所有同时攻击。

本轮只验证一个正常打法：原单次机械爪匹配目标、匹配cast、真实手动Cancel/Released以后，用现有S＋L保护原下降落点；不提前开盾取消Reel/Swing、不新增Space/Shift/K、不延长300、不写位置/速度/HP。第一份20eb实际保住原HP，Grip1=102/300，Grip2=154/300，后续Grip3–8通过；最终a733再次通过Grip1/2和3–7，但更后Grip8死亡。两份尝试均整程FAIL，不拼接成通关。

这说明现有能力可以处理Grip1，不说明地图无需继续完善、其余威胁安全、真人读得懂、引擎稳定或全关通过。用户仍未看见本轮画面。

## 实际代码范围

仅两个运行C#文件变化，仍1857文件/897C#：

- PrologueCanyonGeneratedMainRouteTests.cs：确切phase/keyboard/cast/target/manual-release门；单独的Task10Grip1LandingGuard驾驶scope；Grip1调用/回调/结束门；本地Rust攻击发布及盾响应有界记录；拥有输入故障控制。
- BasinCounterweightRuntimeTests.cs：字面正例/七负例、真实S＋L方向/无A/D/K/Space/Shift及撤键、真实故障撤键控制。

旧纯Task10AscentGrip1Diagnostic保持只读；没有让一个“observer”暗中改输入。新scope通过现有键盘适配器生成引擎内InputEventKey，方法名PointerButton只是旧策略意图翻译，不发系统鼠标/键盘输入。

只有匹配cast17、曾观察到确切post、真实Cancel/Released才能武装。下一原有驾驶回调发S＋L，实际named floor/完整body后释放；结束或首故障时独立撤键/退订。原HP、同player、恢复次数、普通跳跃次数、一次cast及整个300/tail门不减。

Rust-only发布帧明确标注不是post-Motor；其他类型攻击未完整覆盖，私有权限未知，当前游荡坐标不反推stableId。ShieldResolved没有攻击者身份，因此记录source_ownership=unproved。suppressed=0仅指未触发行数上限，不表示未节流或全威胁覆盖。

## 先失败再通过，唯一复核

- 3de6：编译通过后原生明确GRIP1_LANDING_GUARD_POLICY_RED，0.419秒exit1。
- 20f6：猜测的Flight枚举不存在，1编译错误/0警告，无原生；实际读取Aim/Windup/Travel后纠正，不算行为RED。
- 025d：旧完整stationary合同＋新匹配门/真实S＋L控制通过，exit0/1245ms。
- 20eb：首次完整Explore试验有局部进展，整程exit1/167613ms，后述Grip9失败保留。

唯一新复核C0/I1/M1：

I1：首次观察/输入故障只存Fault，后续Refresh直接返回，可能把已取得的S＋L留到外层finally；部分输入成功但尚未设置owns标记也有同问题。增加真实按键故障注入，先运行旧生命周期：e39c同时报after-acquisition和partial-acquisition两个指定RED，原独立退订正例通过。f967 genericArray不支持IDisposable的编译错误单独保留，不算RED。

同一次修正：先标记输入所有权，再调用可能部分失败的发送；首故障立即进入幂等Dispose，先置disposed防递归，然后独立尝试撤键和所有退订。后续回调不能重发。最终8ce1全合同exit0/1530ms，三个控制均通过：发送后故障、部分发送故障、一项退订故障。实际S/L撤销、托管订阅和原生child-entered-tree连接移除、后续不重发及原异常身份保留都有断言。

故障控制只模拟scope武装状态，真实使用保存Player/键盘/护盾，并非实际机械爪或通关证明；真实cast/回收/落地仍由整程另验。无二次复核。

M1延期：现有中性意图fixture不证明任意既存A/D意图在盾释放后都不会恢复；只支持本调用点，未重构适配器、未把它称为已证实玩家bug。所有历史延期项继续保留。

## 两次Explore必须分开

### 20eb：Grip1–8通过，Grip9受阻

- cast17真实Released physics7718；向下盾在7732真实Guarded=True/Broke=False；7737原完整Grip1支撑。
- 43记录行/0上限抑制/0记录故障；原Grip1 HP1保留102/300、单K无额外跳；Grip2原武装地面Space＋单K154/300；Grip3–8原门通过。
- Grip9唯一K在9438接受cast28；9455发布Released/Failure=Obstructed/Release=Obstructed，target空而locked target仍旧ID。
- 当时HP1、仍在原Grip8上，玩家(4725.3145,1603.9601)，recoveries0/0，没有cast29。clearance复合断言至少Failure/CurrentTarget两项失败，不冒称HP/地面/cast ID失败。
- 原清障边界4724，发布点还差1.3145px；这是与假设相容的几何线索，不证明具体碰撞物。该发布回调里的floor contact不是该次预测碰撞返回；LOS/stall/pull-obstruction实际分支未捕获。

### 最终a733：Grip1–7通过，Grip8被失控残响击中

- Grip1/2仍分别102/300、154/300，盾响应1、43行/0抑制/0清理故障；Grip3–7通过。
- stdout1909，physics9416/input9387，第5次接受伤害HP1→0，来自lost-echo近战，不是旧Grip1骨骸，也不是Grip9受阻。
- 精确身份：stableId canyon.v2.resident.split-level.02，actor6332493075566，Hitbox6332560184434，attack1，scene res://scenes/enemies/residents/v001/lost-echo.tscn，cacheHit/attributed均true；路径CanyonAscent/Gameplay/Actors/@CharacterBody2D@62/AttackHitbox。
- 玩家(4696.565,1567.4756)，冲量(110.073326,-154.86725)，盾未按/未激活、朝右，energy1/heat0、未过热/未攻击。
- 之前9398 Swing/Arrived；9400原Cancel回收cast27；9401 Retract；9405 Idle/next28、向下vy182.68268、未落地。它们是发布回调，不冒充9416完整post-Motor快照。
- 原Grip8第134/300帧HP/恢复门失败，recoveries1/0，接受伤害5/gaps0，无SUPPORT_OK。所附danger为392帧前旧决策，不能当无预警证据。

## 最终普通九项：6PASS /3FAIL

| 阶段／短ID | 原生结果 | 严格结果 |
|---|---|---|
| stationary＋故障控制／8ce1 | 0/1530ms | PASS |
| models／2321 | 0/391ms | PASS |
| keyboard／e820 | 0/11902ms | PASS |
| recycling-traps／1c949 | -1073741795/24107ms | FATAL，FAIL |
| 完整Main／dd70 | 1/118425ms | 死亡恢复，FAIL |
| 完整Explore／a733 | 1/168522ms | Grip8死亡，FAIL |
| pre／c05e | 0/155151ms | PASS |
| post／3663 | 0/197316ms | PASS |
| 普通诊断排除／cdd6 | 0/421ms | PASS |

机关FATAL发生于保存关卡combination variant0＋audio drain之后，原文gchandle.is_released()、GodotObject.Dispose(false)/Finalize、csharp_script.cpp1788。普通模式没有资源身份记录，具体类型未知；不认作v146同一个PlayerTuning，也不无证据归因或排除新改动。没有重跑筛绿。

Main仍physics6617死亡、恢复1/0，位置回检查点的3120.29px重置触发严格门。最终Main/Explore的stderr/external测试ERROR均保留。

pre三个输出无诊断；post实际75执行/0退役/75归类＋owner重复8、退出drain/phase通过，只有4条限定WARNING（seamless1/room-profile1/flow-registry2）。本轮通过不能关闭其他案FATAL。普通实际程序集排除lifetime/native-identity类型，未把诊断构建交给普通运行。

证据根 G:/knight/验证证据/october13-stable-claw/，最终九案：

    thruster-stationary-guard-20261027-225040-507-8ce13789534a4864a970409084d9a7ff
    models-20261027-225117-139-2321ab75a31b4f078d0a3f9bbc84b3d8
    keyboard-20261027-225145-243-e8207cb2b6aa40b699e939b39431f99d
    recycling-traps-20261027-225226-440-1c949337c33a4c85885de12e84fc513e
    thruster-route-main-diagnostic-20261027-225317-816-dd70d99de79f45bfa97a26eaba52b479
    thruster-route-exploration-diagnostic-20261027-225545-887-a7333e05abc04784af944aa260c4b517
    prologue-pre-20261027-225903-636-c05ef29a7ebc4406b2287c9a749de315
    prologue-post-20261027-230213-941-36639f326da14586b50eb1907f27ac0f
    thruster-profile-trace-normal-20261027-230603-977-cdd6e03725004d5594f00f4dd114e58a

较早独立epoch：

    thruster-stationary-guard-20261027-221229-007-3de693d39474487bb203aa6dea170435
    thruster-stationary-guard-20261027-221842-316-20f6ed2c1a014231b53a084b2032dd40
    thruster-stationary-guard-20261027-222002-694-025d0f1f240547da8a6cb6589912573c
    thruster-route-exploration-diagnostic-20261027-222220-221-20ebde33f94d48c2a93fa263e67d68a9
    thruster-stationary-guard-20261027-224525-172-f967c33bf7984a8d9b147932c009d2be
    thruster-stationary-guard-20261027-224747-053-e39c05a7bdef400b8abc45daa94ac680

## 独立有界保管

审计截止2026-10-27T15:09:56.2575134Z，本轮15尝试另列于旧v147：

- 60前后源码/helper/user/continuation清单对相等，7202源码副本＋154helper副本匹配各自清单和选定路径集合。
- 13 fresh/after二进制清单对相等，78封存二进制匹配；两编译失败不伪补fresh/native证据。
- 28记录的自有PID＋CreationTime身份结束，0timeout/0hash mismatch；不是全系统空闲声明。
- 独立枚举1857路径/897C#，最终九案各全清单与当前一致；54普通二进制和83相应helper条目与当前相符。
- 13clean build/原生、2build失败；原生7 exit0/6非零，严格7PASS/8FAIL，包含有意RED，不算玩家成功率。

    source manifest C03BA023D9FF208D3C8925E007746B10CADA8895E4E2293715E565C96BEBA704
    binary manifest 74DBD09847251728E3CE775FA2AD5AA3B60A46FE3A76FC73B9BD3A4142F7E8FB
    MainRoute F46251B7C5AE6917B6D57E1CC70F1A1C07C6B54CEEEBD7BB2501722236590962
    Basin 2145023FB8C7007C0F92FABBE038A98FA9D068B33524F0528349FC4169B95F39
    canonical 155EB543ED41A38AF63FFDFBAE6D3D30602628AAB71990AF329ABE574E78DA4F
    Knight.dll 06115ACDC95EA839642B6F0B171A31B56187C36C2463272C7C5E880C3AF18D92
    Knight.pdb BD112953F6755813BAE3D393DC35B32B89F795E78835443087205538F0BAE2D8
    Knight.deps.json 214598669CA8B8883DAFE876E7EA3DE11E15D9828EB3FA4294E828CF24F23964
    Knight.runtimeconfig.json C2BD1DEFE29583AB9DB5A1110B36B3537214BCB56321F076ABFFBB9956852722
    GodotSharp.dll 7B1D06575DB78E4EA4353186BE5ADD207E43617B836DB24D65EC4088E6FAD18C
    GodotSharpEditor.dll EADA28133A15ED75B9E892EBF1C73C026AF6034262BA71838A204C656FD3AD08
    HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
    index 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C

## 取舍、延期与后续

按ledger顺序：

1. 基于实际伤害/方向，只做正常S＋L驾驶试验，不改生产物理/敌人/难度或额外预算。代价：不能据此声称全部威胁安全，失败必须保留。
2. 输入scope与旧纯观察器分离；增加匹配cast拒绝旧释放。只记录实际runtime/scene，不增加来源lookup、不用巡游位置猜stableId。代价：其他威胁/被挡攻击归属仍未知。
3. 保留两次已验证的Grip1局部打法，后续Grip9受阻和最终Grip8死亡各自独立，不加跳/改地形取绿。代价：完整Explore仍失败。
4. 复核未判定事项逐一保持边界：完整Main/Explore/fullmatrix另测（已执行但失败）；Grip9/历史故障不算已修；生产AI/数值/地形/tuning未改；引擎/资源/硬件原因未证；真人和美术不可由headless接受；全威胁/被挡owner/私有权限/post-Motor未证；两次局部成功不是概率可靠性研究；故障分支通过本次真实控制而非冒称reviewer已测试；保管/存档hash/媒体另审；没有付费/自动化/系统输入/外部发布；没有二审。代价：原目标全部未证门继续OPEN。
5. 新Minor M1延期：当前中性调用点证据不等任意既存方向意图的无恢复保证，适配器未重构。历史延期项不减。

手工媒体Check2026-10-27T15:12:56.7669803Z：1431609bytes/16files，baseline3508/growth1428101，Notify=false，不重置/不自动化。无新图稿实录、付费生成、前台窗口、系统键鼠、Git写入、真实存档语义读取或删除；原143实体/9残片/76普通敌人、全部美术/音频源不变。

下一按真实1c949短机关FATAL定位资源身份；以a733实际lost-echo下降伤害为独立后段问题，Grip9实际预测阻挡分支仍需查明，不能用floor contact猜碰撞物。Main另有已观测真实右侧低位遮挡，后续可在原900内验证驻留/再入场，不重试v142储能阈值或承诺固定60等待。以上是未执行候选，不冒称已修。

原十一项不缩减。正式美术分层、连续动作、UI/音频/相机观感、完整两路、长期资源、目标硬件/真人及版本交付仍待独立验收。
