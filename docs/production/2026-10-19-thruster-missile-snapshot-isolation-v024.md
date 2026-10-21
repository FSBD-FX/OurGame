# 导弹自动操作的实际快照隔离 · v024

2026-10-19，Asia/Shanghai。开发记录版本，不是发布包。完整1–11目标 ACTIVE，Task6/7与最终内容／交付未完成。本轮仅修改测试消费者、被动观测与证据保留；没有生产Player、地图、敌人AI、伤害、正式美术或真实用户存档更改。

## 事实、推理、风险

事实：原HIGH4J／LOW7J通过，但其Drive仍热读已失效的Tuning.GroundDeceleration。新增第三个独立HIGH4J，在Ready之后临时修改本夹具的作者字段、实际Motion／Module未变，旧Drive第二步却松D，真实轨迹改变。只将该Drive迁移到实际ground Neutral后，三组通过；预测、键盘、动作、完整pre回归通过，完整post仍严格失败于旧高层稳定落台。

推理：原基线GREEN不能证明旧消费者正确，标准速度下旧两tick余量可能偶然足够。新增负控区分的是“已生效快照不变，作者热读却污染控制”，不是先假定LOW正常停距必败。获批自由力学不应为旧测试反改。

风险：这是自动操作夹具修正，不是新增玩家技能、正式敌人或画面提升。正常完整双路线、真人、显示60FPS与资源故障仍未验收；本轮新二进制不能借上一冻结v023的后台双路通关取得当前资格。五历史失败、音频2 ObjectDB、匿名FATAL、新50正式0/50、24canonical、最终地图／美术／UI／音频／演出／存档组合／ZIP／GitHub全部保留。

## 七个原生案例

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。所有headless、source1695含296 imported；fresh Rebuild退出0、0警告0错误。无timeout，guards=[]；严格保存失败。整个回归批及阶段间隙冻结source／helper／docs，全部终态后才更新。

| case目录 | build ms | native退出／ms | 实际结果 |
| --- | ---: | --- | --- |
| ranged-steam-missile-policy-20261019-044352-382-da226cda9b55461da4fd31444dd3cf8e | 28844 | 1／8727 | 独立作者热读行为RED |
| ranged-steam-missile-policy-20261019-044529-922-16160e0fce5d4254910b4e4d9564eb1c | 28554 | 0／8931 | actual Neutral后HIGH／LOW／第三HIGH GREEN |
| thruster-free-forecast-20261019-044641-005-a4e6f19b626548b9aa36a56247f1a9a0 | 30524 | 0／683 | 独立预测／旧2800负控GREEN |
| keyboard-20261019-044719-782-245275dcdda74e9baaf9e8a0104bf4db | 26314 | 0／9914 | 键盘原生回归GREEN |
| actions-20261019-044803-037-67119fa4944749539d17a7d8151b5eec | 26724 | 0／7236 | 动作／过热后攻击原生回归GREEN |
| prologue-pre-20261019-044843-488-a015b0a4a0a8451897d6da314668b6f5 | 27444 | 0／100542 | 完整pre GREEN |
| prologue-post-20261019-045059-184-c323441450054966b1123977e534e406 | 26284 | 1／71208 | 完整post旧ascent_grapple_three .2落台FAIL |

整体suite不绿，不以5GREEN／2FAIL比例表示目标完成度。RED与GREEN之间仅Drive差异；后六案同冻结。没有运行`tools/verify.ps1`生成正式场景，也没有新正常窗口或OS输入。

## 独立行为RED→GREEN

原HIGH／LOW基线保留，随后第三HIGH临时将本Ignore-loaded Player实例的作者GroundDeceleration从2800改1。该值不是合法保存迁移配置；明确after_ready=true、valid_saved_configuration=false，不RequestReady、不equip、不写磁盘，finally在原清理前还原。Motor与Combat共用本夹具的作者Resource，但Combat不消费这个字段。

每次原Tick检查actual Motion引用、Module、configuration epoch与motion authority不变；被动priority100只缓存完成Motor后的标量，不发输入或断言。HIGH每次原await后要求完成步physics≥issue、sequence更新且同当前Input、真实OnFloor；不把PhysicsFrame信号直接当完成Motor。无新增await。

RED中原基线HIGH8/8以及LOW先通过。第三HIGH Resource9223372120774411872改值后，index0仍D／v36.666668；index1却raw/current轴0、D=false、v6.666666，而基线为轴1、D=true、v73.333336。独立真实轨迹断言失败，不是source grep、编译或观察器异常；finally同ID恢复2800。仅证明热读隔离缺口，不称LOW正常停距已经失败。

最小改动只在Drive：真实原地板完整支持、actual Motion／Module匹配且无燃烧才预测；ground Neutral horizon取原冻结deadline余额、threshold2，预算内不能停止即失败。保两tick输入余量、gap>64／target60、A禁用、Defend／AllowShieldApproach、盾指向、HP5、原准备攻击与等待；不改反刹、碰撞、AI、时钟或伤害。

GREEN第三HIGH Resource9223372122854787146最终同ID还原。八个fresh完成步的相对位移、速度、raw/current轴、A/D/L和盾状态逐样本同本案基线，也同RED原基线：位移X依次0.6111145／1.8333435／3.666687／6.111145／9.166687／12.833313／17.111084／21.444427，末v260。原8/8及gap减少≥12断言未放宽。

LOW仍真实≤.35能量Active前向D+L挡爆、玩家HP5；两个fresh J精确同Hurtbox，敌HP2→1→0／Defeated1，末gap60.261536/v0。本案实际shot1／guard1；原断言是shots>0和同AttackId关联guards.Any，不冒称已增加全程exact1／1合同。HIGH只验证早预警八步，不证明其击败或全停距；夹具不证明正常group／batch。

## 完整回归仍未关闭的失败

本轮post真实首错为`ascent_grapple_three .2`：physics3967在(819.25946,631.5926)、v(212.52051,−37.626865)请求第二Space，预测X878.4692、真实LandingThree站立Y678、完整支撑X875.08..1259.92。最终239/240、1cast／1latch、boost=true、无claw failure，却在(1140.484,1299.9333)，未满足十二稳定帧及15%安全内带。未放宽断言或将其删出suite。

该失败是OnFloor／stable≥12／margin的复合谓词，当前日志没有逐项拆分，不能据此单独归因某次碰撞或某个margin。前七个segment的20/50/80样本已通过至ascent_grapple_two；第三段后续与其余post阶段尚未通过。

本案未记录该次释放的逐优先级边界及首Contact，不能把旧v015的具体碰撞tick当本案测量。下一诊断入口是`PrologueCanyonTraversalTests.DriveAssisted`自动Input−300／Claw−100／Motor0，不是另一文件里手调SampleClawInput的旧room路径。

下一只做第二Space起12physics／128行上限的test-owned被动记录：Input后−299、Claw前−101／同步Boost-Release payload／后−99、Motor后100。区分payload与尚未应用释放冲量的实体速度；首Contact仅复制已有slide cache标量、不Dispose借用碰撞对象、不增加world查询／await。原240、十二稳定帧、15%内带、几何和输入时机全部保留，先取证再决定修法。

## 保管链及范围

RED／GREEN五日志行数分别7/0/22/46/68与7/0/31/0/31。RED仅一个行为ERROR；external顺序为stdout前20＋stderr全部46＋stdout末2，不丢栈。GREEN三trial无诊断、external逐行等于stdout。既有finalizer-count1／EXIT_DRAIN不关闭历史资源故障，未新增GC／drain／清理等待。

两案四对库存及continuation前后相等；实际selected180=174source+6helper，入／退出实际副本与六binary逐项匹配。相对旧导弹基线，owner只增加五个显式保留依赖：MotionSettings、ModuleSettings、ThrusterDynamics、ThrusterPulseModel、FreeMotionForecast。不是sourceRows增加或隐匿漂移。

| 冻结／清单 | SHA256 |
| --- | --- |
| RED source | 860DC137E04507CBF7B808CA8EBAEE4136A08330C1B46E409FFADDD2BC023FFF |
| GREEN及五回归source | F93C41C2FFFDE4C4AFA793AB7621F52DD9649277EAF164C295F97BECD0FCCF10 |
| GREEN及五回归binary6 | E8C9E807797CC083BB8C8EAE323DD7D01271ED56A6C332A551EF9014D86A3135 |
| Missile helper10（RED／GREEN相同） | 0C3E79138E497D752BD1609D96061957D141251CBC106F11E439493DD3137FEB |
| forecast／keyboard／actions helper9 | DEF02DE786FD174EF97B336ABDEDCBB097764DF004E2F7FCF6E7C32B99165F7D |
| 当前owner | D097F80B4D739536ECCF3760173F8E5273F77AD54500A7923DE409471C8B0DB0 |

当前DLL `C09516F0BB7F28D6614EC73BAAD36680C2E64FA2EB521AFB3BA8B72992089237`，PDB `282A5F2F2813E09DE256D04B4ED17084A6B9114B169E7D0FF403D72C9B9136D8`。forecast／keyboard／actions各实际selected166／154／154，三案四pair／continuation／所有副本及六binary匹配。同freeze不同selected scope不是源码差异；helper9仅比Missile helper10少既有ranged admission。

最后pre／post的四pair／continuation／全部入退出实际副本及六binary也匹配当前GREEN。实际selected分别181=175source+6helper与171=166+5；pre helper10为`7121B9B75CF766F61C02EAFE77A25ABDB2FF16E0E5B6B58F897AEACEE324B728`，只多既有clearance helper，post helper9同上。五日志行数分别7/0/334/0/334与7/0/177/17/194，external逐行stdout+stderr；pre完整PHASE_OK，post仅一个行为ERROR，没有新Handle／FATAL／ObjectDB／资源API ERROR。原失败drain不抵消严格FAIL。

Task7仅减少一个明确的Drive旧消费者；原pre-mount deadline基础、其他文件／分支、legacy碰撞与资源身份仍按总账处理，不称整文件或M09完成。[v023完整后台双路](2026-10-19-thruster-temple-supply-and-headless-dual-route-v023.md)是上一source0F97／binaryFD43冻结的历史资格，不搬到本轮F93／E8C9。正常窗口、真人、FPS、完整1–11均未授予。

所有owned消费者终态、CIM无owned后记录。E空闲5229080576字节约4.87GiB，F约26.82GiB，G约356.09GiB；未来新增证据存G询问未答，不移删旧件。无新展示图稿／录像，过程归档位置仍为`E:/OurGame/Knight/过程图稿与视频`。目标ACTIVE，无付费／Git写入／ZIP／发布／自动化恢复。
