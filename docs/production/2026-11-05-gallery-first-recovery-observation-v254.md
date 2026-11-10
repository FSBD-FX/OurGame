# v254 · 主线首次反击窗口的同次危险证据

范围：原十一项Q05诊断推进，非正式美术/玩法完成。根继续G:/knight/Knight，headless隔离存储；记录器实际RED→GREEN，当前9项相关检查 **7PASS / 2FAIL**。Main仍原899失败、pre-prefix实际原生FATAL，Explore315918ms通过Boss/支撑出口/恢复0；不继承旧整套的当前资格。

## 已改变下一步的事实

对v253 Main143241/Explore143555：下一批均在上批死亡后37个physics进入。不是换批速度不同。Skitter准入至击败分别665/207physics，Main首次局部eligible（8030/loop317/input8000，持续至8045）却没发J，直到8435/loop717。首次窗口有37.3能量、目标HP2和Recover；Explore较低27.1能量时立即发J。

这只证明局部门成立，不能证明周围所有危险都允许完整反击。当前外层 `enemyThreat` 否决与probe-only的早反击路径是可检验假设；HP1logger未覆盖这个HP2首窗，所以先只补前24条同次完整危险标量，不改J、数值、899、输入或生产敌人。

## 资源桥接调查：事实、推理、风险

本轮只读核对官方同commit的 [ScriptManagerBridge](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs) 和 [CSharp引用回调](https://github.com/godotengine/godot/blob/4.7.1-stable/modules/mono/csharp_script.cpp)：引用计数变化会切换托管handle的强弱状态，桥接异常并不提供本地资源身份。网页行号与本地PDB不同，不把网页位置当精确本地帧。此机制与已见堆栈一致，但不证明哪个对象或哪段项目代码负责。

本地v253七块堆栈逐字相同，发生在同一次新根AddChild中；现有标量确认根挂载成功。不能由此强持全部Resource、换cache、加GC/等待或替换引擎。接下来仅现有无窗口control/pre-prefix各一次对照上下文；安静也不关闭旧pre7Handle/FATAL/RID。

## 当前边界

本轮当前Main152911-748-df066c3a仍严格FAIL，但新增首窗证据有效：17条/0fault/单次flush，所有readStart/readEnd的physics/process/input一致。首次loop317/physics8030/input8000：目标HP2、Recover剩.5833334、能量37.266647、可起招/可负担、盾未举。原同扫统计13counterSources，主威胁attack2 Telegraph剩.59999996，最小全源反击余量.5833334，无observerfault/缺项/未知/重叠/来弹否决。

loop317–326共10条满足原fullAttack.3＋8/60=.43333334反击窗但未发J；327–332余量已不足，不能一起放行。直到loop717/physics8435/input8405首次J，和v253原输入序号/局部窗口相同。本轮未改策略。代码与实际布尔明确定位第一个否决为outer敌威胁条件，旧foreign-budget因短路未执行，记录null/unknown而非伪造false；没有因日志而重复预算函数。

这是支持“有限早预警counter可回收前期浪费”的证据，不是“立刻可以无条件反击”的证明；真实来源身份、落脚边缘和其他原策略约束还需核对。完整同源其余7阶段仍在单批11938运行，尚不能出最终总数。

后续只读核实来源4909130845404是实际骨骸俯冲者 `canyon.v2.resident.pendulum-gallery.03 / bone-diver.tscn / GraveLampMoth`，不是仅依名字猜测。原扫描会排除无效、离树、queued、错误父级或HP<=0；该同次来源身份有效。选中Skitter的实际绑定在窗口前后仍为owned，当前17条未逐帧重复测OwnsLiveActor。Grip5落脚在physics8012和8058有实测，原运行平台AABB(4260,2360),(240,40)，与现tscn一致；8030–8039的slide0/collider/body-floor边界没有直接记录，因此不把推算的宽裕边缘写成已实测。

本轮Explore153223-899-b1cc9d26有1条HP2记录，loop341立即J、无威胁、SafeCounter.6、预算已执行且true、能量27.066677、缺项/未知0；start/end provenance相同。Main比它能量多却较早被全预警否决，差异不是能量不足。资源独立control153816已通过，pre-prefix仍待单次结果；任何quiet不冲销完整pre旧错误。

pre-prefix153856-900-65f0e096现已终态严格FAIL：native52130ms、exit-1073741795、非超时，出现 `gchandle.is_released` 的Dispose/Finalize原生FATAL及3条Handle错误。stdout到四个前置完成后的BASIN_RESOURCE_BOUNDARY_ENTER，未到completion。两类错误共现，但多线程stderr交织不证明先后或同一对象。计划增加一次既有claw-prefix（仅后两个前置）对照，不重跑失败case、加dump、改GC/cache/引擎或扩cap。

下一合理方案（未实施）：仅让正常键盘gallery的owned Skitter Recovery候选进入现有严格早预警检查，保持完整攻击＋8ticks、全源异常拒绝、真实来源/攻击身份、floor/slide0/边界、已有能量预算和盾释放后的fresh-danger复查；不放开所有Telegraph，不改变游戏数值或899。要以真实J/命中/下一防守和完整Main再验，不把本轮日志布尔当作反击已安全。资源问题继续独立归因。

## 本轮真实验证与资源范围

新收集器先false-stub实际RED，case ranged-models151803-530-882b1db9，编译成功且首次保存字面量失败；实现后152603完整远程模型实际GREEN543ms。原HP1前32/尾128/mask与J/await/899不改，新增记录只在已有outer-J算完后取同次标量。仅两个测试CS改变，生产游戏源码和美术没有改变；这不是新的玩法或感官提升。

| 当前同源阶段 | case时间/短ID | native毫秒 | 严格结果 |
|---|---|---:|---|
| 远程模型/新真实列表控制 | 152603-601-1b8fd2c7 | 543 | PASS |
| 普通Main | 152911-748-df066c3a | 153440 | FAIL：原899 |
| 普通Explore | 153223-899-b1cc9d26 | 315918 | PASS：Boss/支撑出口/恢复0 |
| 资源无前置control | 153816-848-11fb38b8 | 2777 | PASS，仅本次 |
| 资源四前置pre-prefix | 153856-900-65f0e096 | 52130 | FAIL：FATAL＋3Handle，非超时 |
| 爪模型子集 | 154032-572-deeb0869 | 534 | PASS，非全模型 |
| 键盘 | 154110-297-cb35cdb5 | 20305 | PASS |
| 动作/技能技术 | 154212-258-8ca5a9ed | 9541 | PASS，非美感认可 |
| 资源后两前置claw-prefix | 154503-929-27c5c44a | 3997 | PASS，仅本次 |

批11938终态exit1、7阶段2失败，另ranged-modelsGREEN及新增唯一claw-prefix，共9阶段7PASS/2FAIL。所有owned运行已终态，不重启旧handle。不把更短链quiet当作排除时序/偶发因素：它使调查优先集中到前两夹具及与后续的交互，而不是证明某个资源已坏。

45对source/helper/user/continuation/binary前后文本无差异；9案source/binary manifests各唯一1；当前1897输入/完整文件集/6binary/12helper独立重哈希0差异。实际完整case在G:/knight/验证证据/october13-stable-claw/，本轮audit与exact before/current两文件diff在 `.superpowers/sdd/2026-11-05-gallery-first-recovery-observation-v254/`。完整pre/post、全居民/所有机关本轮未重跑，旧v253或更早结果不是本轮同版完成证明。

只读核查前两夹具未找到显式释放借用Profile/Tuning/PackedScene/保存Shape或复用已Free根；Camera的旧碰撞快照疑点已有36次相异wrapper/peer有效证据，不能重复当成支持假说。配置异常保护缺口不在本次成功返回阶段触发，仍不能解释FATAL。资源下一步应记录每次实际赋值资源与live fixture/cycle/root的关系，并与已有释放事件配对；现有identity日志首次owner不等于唯一owner/最终故障owner。需先核已有诊断入口符合引擎不变/无额外dump边界，不借此增加强根或盲改释放。

仅正常汇报单次媒体Check成功：2026-11-05T07:50:43.1878226Z，7,427,750B/86文件/Notify=false；无新20GiB档位、无新媒体或自动化。

## 审查与保留

唯一fresh增量审查C0/I0/M0，可保留诊断，不批准早反击或整章就绪；无修补轮/二审。审查独立核对RED/GREEN、Main17/Explore1条同次记录及原HP1两线前32条/尾部fault0保留。全部13项考虑后不判缺陷行为、root裁决和代价见[完整终审记录](../../.superpowers/sdd/2026-11-05-gallery-first-recovery-observation-v254/final-review-v254.md)。原有回执唯一性、空borrowed对照等历史Minor没有关闭。

仅本诊断增量完成执行与审查；当前Main/资源失败、原十一项、正式美术/连续动作/R-I/R-C/R-P/正常速度视听/性能硬件和真人仍OPEN。没有新付费、美术、过程媒体、GUI/OS输入、额外原生dump、自动化、Git/外部发布、真实存档实验或删除。所有原失败与before均保留。

上一轮15阶段13PASS/2FAIL属v253输入；一旦本轮测试源码变化，不代称本轮完整同源通过。当前Main失败、资源稳定性、全76敌群、正式美术连续动作、惯性/物理手感R-I/R-C/R-P、正常速度/硬件真人均OPEN。V1旧图暂缺，后续审现有稿而非转授批准。无新dump/GUI/OS/付费/媒体/自动化/Git/真实存档实验或删除。
