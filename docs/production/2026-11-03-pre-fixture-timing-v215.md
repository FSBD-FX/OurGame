# v215 · 前段计时定位与矛盾日志拒绝

仅G:/knight/Knight。原十一项与正式动作/惯性/美术、完整双路、历史资源故障仍ACTIVE/OPEN。本轮没有修改任何生产脚本、场景、资产、游戏数值或键位；只改两个既有测试CS及canonical，SourceRows1874不变。不是试玩内容或正式交付完成。

## 事实、判断与风险

- v214的pre在原180s超时，旧日志显示进入Traversal前已比v213慢25391ms，而之后共同段只多11ms。不能据此把原因归给新敌人动作、渲染器或终结器。
- 现为原44项逐条记录真实enter/return、engine_ms、physics/process及准确差分。保留每条原调用/await、Supply两编译分支、原trace初始化、成功/异常/清理路径，不改成委托包装器，不保留Node/Resource引用。同步同毫秒/同帧返回有效。
- 单次独立审查确认剔除计时语句后，RunPrePhase与精确基线完全相同；44逻辑项对应45文本调用（Supply条件分支只能选一）。它不认证全项目运行质量。
- 记录器允许的安全名称/完成数量是通用接口，宿主验收器独立固定原44名称和88边界；完整计划及唯一pre成功必须在最后，任何失败ENTER/DETAIL记录均拒绝。无ExpectedFailure或漏标记豁免。
- 日志读取并非原子时钟快照，打印/CLR分配可能扰动调度或GC；只把数据当观察，不冒认完全无影响的同一环境控制。没有新强制GC、缓存保活、额外崩溃采集或提高预算。

## 测试先行与唯一审查

原案在`G:/knight/验证证据/october13-stable-claw/`，GUID短号对应唯一目录。

| 案 | 实际结果 | 注意 |
| --- | --- | --- |
| 99df4699 | native RED638ms，缺有序标量记录器 | 外层也因旧marker未完成失败 |
| 65d4e219 | native623ms通过，host缺解析器RED | 严格整体仍FAIL，不能按native0记通过 |
| 862ba8a5 | focused617ms严格PASS | 旧边界/异常合同＋标量3正例16负控＋host2正例12负控 |
| 0167d657 | pre180052ms严格TIMEOUT | 44enter/43return，最后Traversal未返回，无计划/phase成功 |
| 9234ce26 | native738ms通过，host漏拒6矛盾反例RED | 明确保留admitted12..17 |
| 99596d58 | C/I后focused609ms严格PASS | host2正例18负控；原边界/异常合同保留 |

唯一fresh审查C0/I2/M1。I1为计划标记可在开始前/最后返回前出现；I2为明确失败记录与phase成功同时存在仍被接收。先执行六个新增反例实际全RED；修正failure-detail测试中的换行后，仅提取三个纯PowerShell函数复现仍RED，再修成有序90事件流（88边界→计划→唯一pre成功）及stdout/stderr失败记录否决。纯函数随后GREEN，完整focused再GREEN，没有第二审。

纯host回放的原命令/工具输出在`.superpowers/sdd/2026-11-03-pre-fixture-timing/parser-corrected-red.txt`和`parser-corrected-green.txt`，不是额外native案。精确三文件旧基线、实施计划、审查包与全部裁决留在同workspace，不动Git/索引或删除用户改动。

## 第一轮实际阶段分布（0167d657，严格超时）

44个enter、43个return，已返回项elapsed总和163686ms；不能把未返回的第43项或整个pre记为完成。

| 顶层组 | elapsed ms | physics delta | process delta |
| --- | ---: | ---: | ---: |
| BasinAscentCamera | 29268 | 350 | 131 |
| BasinLaborerClearance | 52633 | 1120 | 525 |
| BellBackLaborerSight | 18823 | 1129 | 2728 |
| DescentClawGuidance | 13544 | 438 | 269 |
| FirstCombatGuidance | 12868 | 351 | 424 |
| BasinSupply原四案例 | 10249 | 580 | 1296 |
| MechanicalClaw | 8134 | 489 | 1176 |
| PlayerTraversalInput | 6620 | 397 | 957 |

前两组共81901ms，但不是玩家一次启动：Camera是三个保存Level的load/instantiate/mount/playing/释放周期；Laborer是离树工厂＋正常通行＋阻挡负控＋步道支撑，共4次实例化/3次挂载。普通GD.Load而非Ignore，不是每次builder重建。原Frames会依次等待Physics和Process，清理还包含有界音频观察；不能假设每次Frames只过一个physics tick。

## 当前C/I后原始pre（2efd1ee3）

在原180000ms内严格PASS167481ms，44对、完整计划、finalizer/drain/pre成功齐全，guardFailures=[]。C#源码和DLL与上一次超时相同，host修补不改变native执行；**这一次通过不等于间歇超时或资源问题已修复**。

| 同名组 | 第一轮超时案 ms / physics | 本次通过案 ms / physics |
| --- | --- | --- |
| Camera | 29268 / 350 | 22182 / 235 |
| LaborerClearance | 52633 / 1120 | 41722 / 747 |
| BellBackLaborerSight | 18823 / 1129 | 18816 / 1129 |
| DescentClawGuidance | 13544 / 438 | 12631 / 423 |
| FirstCombatGuidance | 12868 / 351 | 11937 / 333 |
| Supply | 10249 / 580 | 9759 / 570 |

本次Traversal本身23940ms/1437physics，完整返回。前两组耗时合计减少17997ms，同时帧计数明显不同，因此后续应细分原子装载、挂载→Playing、原观察循环与cleanup，而不是凭总耗时推断统一CPU/渲染变慢或减少覆盖。

最小下一测量：先分开Laborer实际Run的四个原调用；Camera可在已有load/instantiate/mount/8frames/playing/cleanup边界补同类标量计时。不改顺序、资源、等待、300次观察或预算。当前尚不能定位具体异常子案或与v213差额的因果关系。

## 最终同源回归：4PASS / 1FAIL

串行外层37302已明确结束，exit0仅代表最后models返回，不能掩盖keyboard失败。各案SourceRows1874、guardFailures=[]、无ExpectedFailure，源码/helper冻结，原预算不变。

| 阶段／GUID | native ms | 原预算 ms | 严格结果 |
| --- | ---: | ---: | --- |
| pre／2efd1ee3 | 167481 | 180000 | PASS，44对和完整计划/phase |
| post／8dbf4978 | 205678 | 300000 | PASS，原75有序案例、8次PackedScene owner、nodes1→1；原4条有作用域警告不新增豁免 |
| actions／72b83534 | 9512 | 55000 | PASS，真实键盘动作合同 |
| keyboard／520a1d90 | 20255 | 55000 | **FAIL**，native exit0且有STABLE_CLAW_OK，仍有3条Handle ERROR，不按marker洗白 |
| models／1de96a3c | 511 | 55000 | PASS，仅既有爪模型子集，非全项目或物理美感验收 |

keyboard的stderr第1/38/74行为`Handle is not initialized`，涉及SwapGCHandleForType和SetGodotObjectPtr；完整原日志及external重复日志保留。当前资料不能证明是谁释放资源，也不能推断新增标量计时导致或修好故障。虽然正常双次Rust K及隔离标题合同有完成回执，整案仍严格FAIL；不追加重复运行筛绿、扩cap、GC保活或新dump。

独立增量保管核对：本轮全部11案为6PASS/3意图RED/1原preTIMEOUT/1keyboard严格FAIL；55清单对、5134选定源实体、120helper实体、66bin实体、22owned身份，逐案before/after与实体SHA均无差异。最终当前1874源、13个跨phase所用helper、6bin匹配，各phase清单集合可不同但同路径哈希无冲突。22个旧creation/path身份均结束，当前KNIGHT的Godot/dotnet为0；PID50900被不同creation/path的Python复用，不把PID复用误判为旧dotnet仍活。没有重哈旧v214批次，也没有由此把native0或单次pre通过升级为资源稳定资格。

当前主要指纹：RunPrologueTests `9929C0DA4E17946AFFA02FA6276B25560AECD09BF318C3A0E043A651C2441645`；Task7CaseRegistryTests `64B21A9D8BF89F75CAB163DFF616C71D662626C0C61E1A66EE8F9486A0A00DCC`；canonical `04B7A9B685478A3AE9BE5C480DA17D7CB8B3D9508E02FB1294B22075F8001AB8`。分支/HEAD/索引保持原值，未提交或发布。媒体7427750bytes/86文件，Notify=false、新20GiB档位0；没有新过程媒体。

## 尚未关闭

Main原899没有新运行或战术批准，不改第二次地面跳；历史fatal和当前Handle错误、正常速度动作/惯性/视觉/硬件/真人验收及完整目标保持未完成。本计时实施计划的完整回归尚未全绿，不写Task1 complete或删除工作区。唯一Minor：同index的嵌套Enter分支已有正确拒绝，但原负控先被index顺序挡住，缺单独回归，按审查延期；不顺手改测试掩盖范围。

本轮不调用美术生成/付费/GUI/OS输入/自动化/外发/删除或真实存档；没有新过程媒体。正常汇报前执行本项目媒体20GiB检查，达阈值仅顺带提醒不停止。

## 本轮裁决及代价（按ledger顺序，非新批准）

1. 直接在44项原语句前后记标量，不改委托/注册表：降低生命周期干扰；代价是重复代码，观察本身仍可能扰动调度。
2. 延续原单写者、精确三文件基线、原位工作与一次fresh审查，不建/改Git或删工作区：保护用户已有工作；代价是版本隔离较弱，必须保留手工基线及冻结核对。
3. 用既有post-boundary-contract兼测pre纯值记录器，不新增phase：减少入口面；代价是名称不能代表完整pre覆盖，报告需分清专项与全段。
4. 观察完整原pre而非跳前缀或拿专项代替：保留原验收；代价是仍可能超时，不是资源/玩法修复。
5. 时钟读取、日志和CLR分配可能影响调度/GC：只作观察；代价是不能把本轮时间当零扰动控制或直接因果证明。
6. 缺失/重复/错配/回退、UInt64大数和零差分由现有合同保持拒绝/准确接受，不臆造通用缺陷：代价若判断错会假验收；C#接近UInt64Max边界未额外独立运行。
7. 固定44项使十亿级index溢出不属当前实用路径；通用journal接受安全token/count，宿主掌握44名称：代价是未来扩计划须同时更新权威，不能假定无限复用。
8. 三时钟非原子与打印开销另列观察限制，不改时钟/加GC求一致：代价是归因仍需窄范围新证据，不能用单次变快证明修复。
9. 真实异常走不到后续Return/Complete，原timeout否决保留；OS崩溃时日志刷盘耐久未证：代价是最后未刷出的边界可能缺失，不能补造完成。
10. 外部日志/继承的大门规则不由本三文件审查重新认证，生产库存另核：代价是不从observer审查推出资源或完整发行合格。
11. focused和审查不关闭原pre、Main899、历史fatal、美术动作/真人/全目标：代价是这些独立门继续OPEN，不能用代码审查顶替运行质量。
12. 本任务全套为原pre/post加actions/keyboard/models；未改Main/Explore且新战术未批准，不重复双路求绿：代价是没有本轮新完整路线资格，旧失败/旧通过不互相续签。
13. keyboard即使exit0/成功marker与外层0，仍因3Handle ERROR严格FAIL，不盲目重跑或混改物理：代价是本计划回归不全绿、Task1不能完成，仍需有界资源定位及后续动作手感验收。

延期Minor：M1同index重复Enter分支已有拒绝实现，但当前嵌套负控先撞index顺序门，缺独立反例；本次不顺手扩大修补。无Critical延期，两个Important已在唯一TDD修补中验证；整套回归仍有独立资源失败，不能借此结束任务。
