# v105 · 区域资源创建来源与结束上下文

2026-10-25，KNIGHT，唯一工程 `G:/knight/Knight`。开发记录，不是发布版或整关验收。

## 事实、推理、风险

事实：上一轮有局部行为与归属进展，但主线未通、资源FATAL未关闭。本轮核对v104全部1844源哈希零漂移后继续；用户最终“没看到”有效，没有重开前台窗口、发送系统输入、恢复自动化、付费生成、Git提交/推送或删除资料。

本轮只推进资源定位和测试可靠性，没有新增正式美术、音频、地形、敌人或玩家能力。最终普通pre通过、post仍原生FATAL、models恢复普通构建。正常Main/Explore本轮未改未重跑，保留v104死亡失败，不能借专项通过给它们续签。

推理：v104坏wrapper是PrologueSectorDefinition，但无法从serial1133辨别它来自保存场景还是样例新建。先补纯CLR创建来源，比按类型猜缓存/Dispose修复合理。新记录现在能区分两条创建路径；然而本次REF pre只超时、没有FATAL，因此没有新的坏serial可关联，根因仍未知。

风险：构造入口记录不等于构造完成、native所有权或资源健康。StackTrace/JSON/flush也会影响GC与调度，不能因为没有新增native查询就声称零扰动。本轮没有生产资源策略修复，不能宣称崩溃概率降低。

## 实施与真实反例

来源检查的代码边界：保存Level有五个子资源；样例在AddChild前用LegacyLayout新建的五项替换Definitions。代码没有显式Dispose这些定义。这个替换仅是已确认的所有权转换点，尚非根因。

- 在现有REF-only typed serial initializer中，新增 `NextSectorAllocationSerial()`，仍只发一次共享serial。记录完整托管类型、线程、现有OriginalProbe上下文和完整托管栈，不读取ResourcePath/native ID/GetState、不持有Resource、不增加GC或加载实例。
- 有限4096行/32768字符；截断、cap与sink失败均计loss，构造仍获得唯一serial。原pre和完整Supply的REF终态检查loss，普通构建编译排除。OriginalProbe只覆盖既有同步操作，instantiate-return后不借用上一操作的Slot/Path。
- 现有真实native identity contract保留原三Resource/所有PP等检查，另验七个真实区域定义构造控制（原三Resource中的一个定义另计）：默认/参数构造、早期/未启用/active/instantiate-return/ended上下文、同serial的显式释放配对，以及纯CLRworker隔离、sink失败与cap拒绝完整性。没有增加新的验证phase。

第一次尝试是两处 `System.Environment`/`Godot.Environment` 歧义的编译失败，没有native，不计行为RED。明确限定System后，034931案真实native575ms exit1：`SECTOR_ALLOCATION_RED`，首个真实构造没有创建行；该时尚未实现logger。实现后035130案native1120ms通过。canonical另要求新收据恰一次，实际producer原文与7个独立变异执行真实owner gate，共8纯控制通过，不只搜索源文本。

### 独立审查I1：不是当前End实现错误，而是覆盖退化

一次fresh review C0/I1/M0：新增instantiate-return已先清operation，原PP及新Sector的ended检查便不能独立发现 `EndOriginalProbe` 不清理。

一次修正过程：

1. 只在REF诊断方法中临时注入End no-op。040228案native0、旧contract误接受；root外部“必须拒绝故障”的断言实际报 `END_CONTEXT_CONSUMER_RED`。这不是好版本，也不是生产End已存在的bug。
2. 在原ended PP/Sector检查前增加独立活动End-only scope，不增加真实Resource数量或减少原断言。保持同一no-op，040410案native747ms exit1；真实PP行仍是active operation2、Slot=`allocation-end-contract`、Path=`contract-only/end-no-load`，明确因上下文断言拒绝，而非native错误/设置失败。
3. 移除唯一故障注入行，040559最终contract native1324ms通过。root再次比对，End方法正文与v104相同，临时注入不存在。没有第二次审查或追加修复轮。

全项目回归仍红，因此只记I1局部修正，不运行task-done、不结清计划或目标。

## 原流程观察与全部尝试

统一证据根：`G:/knight/验证证据/october13-stable-claw/`。目录保留原wait/stdout/stderr/清单及封存bin；耗时ms。

| 目录 | build exit/耗时 | native exit/耗时 | 结论 |
|---|---:|---:|---|
| resident-census-identity-contract-refcounted-identity-20261025-034820-455-9a753e07e1064bd59fce65f91278c2f4 | 1/24715 | 未启动 | 编译失败，不是行为RED |
| resident-census-identity-contract-refcounted-identity-20261025-034931-984-43311268148a43ae8d1d22fd3b9645ae | 0/32465 | 1/575 | 实际missing-row RED，严格FAIL |
| resident-census-identity-contract-refcounted-identity-20261025-035130-447-774b84e79bc141039ca94ab986b9885e | 0/28981 | 0/1120 | 初始诊断合同PASS |
| basin-supply-lifetime-refcounted-identity-20261025-035301-289-4b17caa47f044dd1a78a6641694666c9 | 0/29694 | 0/11690 | 诊断PASS，不是根因修复 |
| prologue-pre-refcounted-identity-20261025-035351-910-581a107d76864c58a5dc5907c7e8b796 | 0/27087 | -1/180121 | 原180000预算超时，严格FAIL |
| resident-census-identity-contract-refcounted-identity-20261025-040228-498-29739d4a51ad48bc96da94c80817b682 | 0/28446 | 0/1011 | 故意End no-op被误接受，外部反例RED，不是合格好版 |
| resident-census-identity-contract-refcounted-identity-20261025-040410-711-1395e88cc2cd43259c85c56d4a54c4b0 | 0/30818 | 1/747 | 同故障被新检查正确拒绝；native仍严格FAIL |
| resident-census-identity-contract-refcounted-identity-20261025-040559-750-ce30e47427ee442a987fd4eb85b51946 | 0/24850 | 0/1324 | 移除故障后的最终诊断PASS |
| basin-supply-lifetime-refcounted-identity-20261025-040634-971-5defa592b005496081c6a473cbd279f6 | 0/28438 | 0/12131 | 最终完整Supply诊断PASS |
| prologue-pre-20261025-040724-078-75d95e3409374525bc0e81b2d75af428 | 0/29184 | 0/164017 | 最终普通pre PASS |
| prologue-post-20261025-041046-527-fcc12899023a4215a88f790350ddc2ad | 0/26640 | -1073741795/171425 | 非超时FATAL，严格FAIL |
| models-20261025-041414-702-fb137472874745df866d76a46e937766 | 0/31362 | 0/565 | 最终普通models PASS |

12尝试＝1build失败＋11native；owner报告7PASS/4FAIL，其中一个PASS是已知误接受的故障注入反例，绝不等于七个好版本。另一故障注入被正确拒绝，不通过豁免把它改判正常成功。除REF pre的一个timeout，其余native均非超时；所有session终态后才更新文档。

两次完整Supply代码冻结不同，数据分别保留：初次35个ctor入口行＝10个bridge/load＋25个Legacy direct；最终30行＝5个bridge/load＋25个Legacy direct，最终COMPLETE rows30/loss0。相同源并不保证每次创建次数相同，不把两案写成35行一致。分类依据为实际托管栈，非native owner证明。

REF pre：35个sector行、无CAP/LOSS、1791 ENTER/1791 RETURN、无ERROR/FATAL；原Supply prefix=pre完整四case已返回，但全pre在后面的遍历样例超时，缺最终完成与完整性终态。不能称整个诊断通过，也不能回补v104坏serial1133来源。

普通post：22 enter/21 return，最后index21 `PrologueGuidanceRuntimeTests.MountedGuidanceDoesNotOverlapBossHud`未返回；171425ms原生FATAL。stderr一条FATAL ERROR、两条原负控WARNING，只有seamless和room-profile两条scope，缺flow-registry一条（额外/重复0），不是三条全缺。本案没有ProfileMissing证据。失败文件先报scope差集，不意味着只需补日志；anonymous `GodotObject.Finalize/Dispose`不允许套用另一进程的Sector serial。

执行入口保持：指定pwsh运行 `.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1 -Phase <表中phase> -SourceRows 1844`，REF案另加`-IdentifyRefCounted`。pre180000/post300000预算不变；没有sector-trace与REF混旗、扩时重试至安静、错误豁免或额外GC。

额外纯检查：实际sector owner gate 8控制，原census admission24、pre-finalizer admission157、full-post budget24、focused admission96均通过。它们不代替native完整流程，也不宣称所有历史helper绿色。

## 审查未裁定项：root的九项裁决与成本

| 未裁定项 | 裁决及成本 |
|---|---|
| 1. W输入与旧preview | 未改reader/controller，不重新认证；原真实输入ReviewFocus继续保留。 |
| 2. 物理首命中与接收资格 | 未改resolver/遮挡，不新增抓爪资格；后续策略必须用真实同cast目标证明，不能拿旧18条候选当LOS。 |
| 3. Intro→Playing | 未改准备/提交；旧readiness风险和正常流程未完成状态保留。 |
| 4. callback/deferred退出身份 | 未改对应执行链，不给历史专项续签；仍需最终同版验证。 |
| 5. LoadLevel自有清理/双异常 | 未改、不捆绑重构；原聚合异常与清理风险仍OPEN。 |
| 6. FATAL根因/native owner/构造完成 | 本轮只获创建入口记录；REF未复现FATAL、ordinary仍崩，明确不认定根因或修复。 |
| 7. 完整suite/双路线/原十一项/视听相机/真人硬件/发行 | root实际结果如表，post及REF超时保留；Main/Explore未重跑、保留旧失败，全部整项ACTIVE/NOTSEEN，不发布/合并。 |
| 8. 全源/封存及8变异的独立认证 | reviewer只认证六文件和指定日志；root与独立保管审计分别补足以下范围，不能称reviewer全工程认证。 |
| 9. 诊断对GC/调度/性能的影响 | 纯CLR不等于零扰动，不作概率、性能或稳定性结论；ordinary无此诊断开销也仍FATAL。 |

其他裁决：沿用用户指定脏checkout而不新建worktree/Git操作，代价是用显式快照/哈希而非干净HEAD归因；沿用Task6/15计划及ledger，不覆写旧BASE或删工作区。只做可证伪来源观察，不因一次安静修改生产缓存/GC/Dispose，代价是本轮没有资源修复或可玩内容交付。全套失败意味着不得task-done/finish/complete目标，没有暂停或阻塞标记。

## 保管、存储与下一动作

- 初五案24个有效清单对/24件sealed bin、后七案35对/42件均SHA匹配，共59有效对/66件；首个编译失败预期缺一个binary配对与六件sealed、无native，除此没有缺失/额外/漂移。
- 最终五案source清单完全相同；helper行数12/12/10/10/9，共同9项SHA一致，不称全部清单相同。最终五案前二REF、后三ordinary；临时no-op只在已标明两个负控案存在。
- root最终live1546runtime＋298imported＝1844、C#892，missing/extra/drift0；对v104只改5个测试/诊断C#及canonical一条收据要求，生产源/资源/难度/地形/图声不变，无新增删除源。当前普通Debug六件等于models封存件。
- 最终source SHA `E6B39BBE7E90D3148D62A4847220998AC8E5A0446E83CDEA74B729E8663DF53C`；helper SHA `B64F5B9D555CC789CF4B75559DF43B33A43E1126A3B15F4BC39AD8751A377A41`；bin清单 `38CA9682611FD493C9A9E9E6D41A3C37F5ED655A5506A3AB814C5B435A47183D`；canonical `A864A34C13ABD1787E5D12373D4C67EE6635FD0B921252F8BAA0FA1D589B23E8`。
- 最终contract SHA `34234F3C71D358F5D4F50AF09520F112F39BC0A8910EB9A5894DB88213C03389`；原audio bus仍 `CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`。
- branch `codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变。diff check0，仅169既有LF/CRLF提示；owned Godot/dotnet/MSBuild/VBCSCompiler数0。
- 仅正常汇报前运行一次既有媒体Check：UTC2026-10-24T20:16:16.8114696Z，当前860284字节、基线3508、增长856776、Notify=false。新媒体0；没有自动化/独立提醒，也未因容量停工。

下一路线实验已有明确边界，但本轮未实施：源码要求同owned Burst最终warning→shot→击败，却不要求shot必须早于K。可以只把`shotSeen`从早K前提移到该HP1目标的致死J前提，保留原每owned实际发射证明及全局危险/能量/完整J+6+2/24冷却/900迭代。先证明实际可cast/真实LOS的开火前机会被旧分支遗漏，再只发一次真实K；不能删除shot义务、凭旧候选当真实命中或承诺能省能胜利。该具体策略试验比继续添加预警常量更有价值。

资源创建记录可用于下一次真实FATAL的同进程serial关联；本轮没有新失败对象来源闭环。原十一项的地图/全部76普通+Boss/NPC/机关、正式资产动作/UI音频演出、正常双路与恢复继续出口、相机/视听/目标硬件真人/授权交付都仍需最终证据，不因本报告结项。
