# v175 · 下层导弹爆炸格挡与落台联调

状态：原十一项目标 ACTIVE，完整交付 OPEN。本轮只调整自动试玩的真实键盘策略与观察，不新增生产技能、正式美术或视听资产。用户误发的三段视频请求已取消，不分析、不纳入美术方向。

## 事实：弹体与真实盾响应

证据根均为 `G:/knight/验证证据/october13-stable-claw/`，原失败不覆盖。

| 案例 | build/native ms | 实际结果 |
|---|---:|---|
| Explore165556…04a635ef | 46161 / 236774 | 被动观察，原11539死亡仍在；167行/0drop/0fault，严格FAIL |
| ranged-mixed-policy171634…361980f1 | 39659 / 665 | 新几何意图 inert Zero 实际断言RED，非编译错误 |
| ranged-mixed-policy172449…58805221 | 37645 / 4473 | 16字面方向/范围/无效控制及原Moth/pending/mixed通过；不等实际新盾挡弹 |
| Explore172922…04e3f29f | 53512 / 246233 | 实际挡下blast，HP1保持；后原left-leg-settle站位断言FAIL |
| Explore175016…0fb87efd | 35016 / 222663 | 左右落台校正与原settle局部GREEN；后control2导弹死亡，整路FAIL |
| thruster-free-forecast175653…fcc49eb7 | 29119 / 448 | 实际快照/独立离散预测/反向越零等原控制严格PASS，非真人物理验收 |
| prologue-post175828…9e2dbc71 | 41023 / 208156 | native0/原75及owner8结束，但1条Handle ERROR，严格FAIL |

被动案实际导弹先为飞行Circle8，11538间隙21.085、11539等待前间隙29.564；瞬时撞地爆炸变为Circle56、中心(3902.5906,4107.997)，完整Hurtbox间隙0，随后HP1→0并退休。因此“弹体飞行间隙为正”不是未来爆炸安全证明，actor Recovery 也不等弹体已退休。

172922完整案中，原A+Space已接受地面起跳后才申请Down+L。physics11538爆炸事件到Retired同步区间内，实际rawL=true、shield active、方向(0,1)，`ShieldResolved guarded=True damage=0`，玩家HP1/recovery0/0。回执明确 `request_identity_directly_observed=false`：这是同一实际爆炸同步上下文内的盾结果，不伪称获得DamageRequest逐请求身份。205行/0drop/0fault，3初始Actor roots、2 Missile owners。

## 推理、实现与风险

先更正v174的表述：`KeyboardSource`的`!ShieldHeld`只在Neutral/NeutralRight/control调用；flight Safety并不全程禁止L。本轮没有删除或放宽这些旧检查。地面起跳仍先满足原A/D+Space方向和完整支撑，只有接受后才可临时以W/S+L给纵向盾；横向为中性，释放L恢复原逻辑方向。这是测试适配器发出的真实键盘事件，不是系统鼠标输入。

动作消费者独立重新读取原Basin steam encounter及resident危险，再核同world/actual owner/Missile/发布权限、授权叶、活projectile、匹配attack与当前Circle来源。有限间隙0..两倍实际身体高度、非零有限方向与批准150°盾锥才提出纵向意图；无效、缺失或Infinity不给动作。该近距阈值只是反应启发式，不改变命中半径、伤害或未来安全，不由诊断日志授予权限。

172922落台后x3763.9524/v0、walkway完整支撑、HP1/recovery0/0，但距target3760为3.9524，超过旧±3。原Leg首次Supported(to)即退出，原Settle只松键，因而无法修正这个误差。唯一整合fix pass让同一原Leg120内的地面阶段复用已有中性停点预测，直到原完整支撑/±3/≤2条件，再由原12settle证明fresh neutral。不加预算、不改原两跳/两K/Left→Right→Left、不移动实体、不改速度/HP/AI/能量。真实172922为对应RED，175016整路结果决定修正是否有效；最坏仍应原断言失败，不能筛绿。

175016实际左腿first contact花41帧，继续同一Leg原120内校正；physics11582在原settle第1帧完成x3758.5762/v0，误差1.4238，walkway完整支撑。右腿也first contact41帧，physics11629原settle第1帧完成x3880.5625/v0，误差0.5625，step完整支撑。全过程原HP1/recovery0/0保持，jump18→20正好2次普通跳、dash不增。这是原落台断言真实RED→局部GREEN，不是整路GREEN。

新的失败前沿：第二次机关操作control2内physics11697，同一resident precision-missile（stable ID `canyon.v2.resident.steam-valves.05`）attack3杀HP1→0，energy1、盾off；随后Recovering回camp才触发连续性错误。因此不是无原因的瞬移。control2尚未完成，不能将第二次K已申请/已应用当成切换与Idle全流程成功。下一分析原全中性机关操作的安全准入，不在这一fix pass继续堆防御或删Neutral合同，不降低敌人伤害或给玩家补血。

只读时序续核（同175016不可变案）：attack3 Telegraph11573、真实Shot11651；return-control Walk结束11656、D朝向后中性准备11657、KEYBOARD_PREFLIGHT11659、CONTROL_ACCEPTED11665，实际blast11697。ACK后32帧才死亡，且没有CONTROL_IDLE，不是K未响应。`Switch()`的10/24/120为同一phaseFrames累计截止，另有实际operation physics差≤120，不可相加。operation前可合法防御，但现有caller没有现成危险等待循环；不能把未花完的各阶段预算相互转借。下一候选需独立证明同一原有准备阶段内的实际防御→授权危险退役→释放→fresh neutralRight→operation准入，并覆盖全源/完整操作窗口；单次时序不能硬编码“等46帧”或假定下次弹永不来。候选尚未实施或通过。

被动观察最多512行，只追踪入口已挂载的Actor roots里的既有及后续Missile；不是全世界未来新root覆盖。回调故障计数隔离，事件卸载，借用叶不跨await，不给借用节点/资源Dispose；最多24条实际盾意图变更日志。终结/丢弃证据仍有下述延期缺口。

## 一次审查与裁定

fresh reviewer `bottom_flight_guard_review_v175`：C0/I0/M2，源审查不代替物理格挡或完整路线。未请求二审。真实落台失败按交付效果列为Important整合问题，使用已保存行为RED进入唯一fix pass；不是把审查未判断项直接当PASS。

延期Minor（不在本次修正中顺手扩张）：

1. 观察器输出scope仍写`initial-mounted-missiles`，实际可追踪初始Actor roots里之后加入的Missile owner；标签偏窄但不改功能权限。本文以实际范围为准。
2. 未直接控制记录异常、512行饱和与重复Dispose。fault/drop计数诚实，但异常初始化或长路线可能缺少后续关键边界；不能用缺失日志授予安全。

裁定与代价：先取实际轨迹而不猜姿态，成本一次约4分钟完整运行；更正L的调用范围而非删旧合同，成本必须另证短暂横向中性后的真实落台；接触与目标站位分开、共享原120/12，成本是较晚敌人/机关时序仍须重新验证。Main/pre、正式视听UI/硬件真人及范围外旧历史没有获得这次审查结论。

## 资格与保管边界

当前源1867。当前post虽原75测试与owner8/PHASE_OK结束，仍有1条`Handle is not initialized`，native0不覆盖日志错误，canonical严格FAIL。4条旧限定WARNING不是该ERROR的豁免。原Main的混战死亡、v174两pre原180秒超时/普通4Handle、历史原生终结器崩溃继续OPEN；本轮不以诊断0错误对比普通4条来归因，不修改引擎/GC/资源保活或重复筛绿。完整正常主线、探索线、Boss实体出口、资源稳定性与正式美术/连续动作/UI/音频、正常速度画面、目标硬件和用户试玩均未正式验收。

本轮7次构建均成功、7次native严格2 PASS/5 FAIL，失败原样保留。合计35清单对/3622选定源实体/76helper实体/42bin逐件SHA匹配，案内前后零差、未列实体及哈希错误0，14个实际owned build/native PID均结束；这是选定保管范围，不是所有历史文件审计。当前1867源/6bin/post实际10项helper与最后post失败案完全绑定，不能借其native0拼完整资格。HEAD/index仍为 `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6` / `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。测试文件`git diff --check`退出0，仅有原LF→CRLF提示，没有修改Git或据此验游戏。

post本次ERROR首业务路径是`ResourceLoader.Load → ResidentOriginalSceneSources.Get`，不是v174普通pre的AddChild首栈；仅路径关联不等共同原因，未做引擎/GC/缓存猜修。原75 enter/return各75且唯一，owner8/nodes1→1；stdout无严格错误，stderr/external各同一条Handle ERROR，不当两次独立故障；各4旧限定WARNING照旧保留。

未新开窗口、抢前台或发送系统输入；无付费生成、新原生崩溃采集、自动化、Git写入、删除用户资料或真实存档更改。本轮无新过程媒体；最后单次媒体检查2026-10-30T10:05:21.6157376Z为7427750bytes/86files、原baseline3508、Notify=false。20GiB仅在正常汇报随带提示，不停工。
