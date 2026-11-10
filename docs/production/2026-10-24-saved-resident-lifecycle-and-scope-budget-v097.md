# v097 · 真实保存叶生命周期与整套验证预算

2026-10-24。唯一工程 `G:/knight/Knight`，原完整十一项目标 ACTIVE。本轮补齐资源复现覆盖和严格验收，没有修改生产玩法、AI、人口、数值、美术或声音，资源崩溃仍未修复，不是试玩交付。用户“没看到窗口”有效；未开窗、未发送系统输入，不追认遮挡或画面验收。

## 事实、推理、风险

事实：旧资源复现只覆盖9个原始carrier的直接实例化，绕过24种新增敌人的保存Profile和原工厂。补齐真实保存叶、著录位置和工厂配置后，两种离树对照各完成480次创建/配置/释放，并在严格门修正后再次通过。最终parent对照也捕获了低顶桥`.03`散弹配置的一次自然终结且正常返回。普通pre两次通过；普通post一次原180秒超时，最后一次在163262ms发生真实配置丢失及FATAL。

推理：不能把该保存叶或Profile类型判成必然损坏；离树创建链在这些运行中不足以复现。实际Ready、关卡/流式所有者及跨用例寿命仍须比较。另有具体时钟证据显示现有完整post总预算存在覆盖缺陷，不应把所有超时都当成同一游戏故障。

风险：20批仍只出现24个配置serial，480次actor释放不是480次配置终结；离树不等于正常世界。同步诊断I/O和分配可扰动GC。真实FATAL、原双路/相机/正式视听/硬件真人/发行门没有关闭，不以局部绿色取代完整交付。

## 三文件变化与RED→GREEN

相对封存原始repro和v096帮助器，只有以下代码/测试变化：

- `tests/diagnostics/RunDetachedEnemyResourceRepro.cs`：完整保留原9×48/432控制；追加DevelopmentV001的24保存叶×20批。使用`ResidentAuthoredLayout.Load()`的真实Placement；scrap-scatter明确选`canyon.v2.resident.low-ceiling-bridge.03`。调用原`ResidentSceneBinding.Instantiate(saved,type,slot,true)`，成功返回的actor由本诊断释放；工厂抛错仍由工厂内部释放，不二次Free。没有新建Profile、使用ProbeScene别名、挂树、Ready、物理查询或新增GC/缓存/保活/借用Resource.Dispose。
- 现有assignment trace只记录原setter和原门已求值的字段；480低于原512上限，没有打开原生来源snapshot模式。每次释放输出operation/path/slot标量，作用域退出不保留actor/binding/Profile。旧退出drain保留，没有增加中途drain。
- canonical owner新增严格`Assert-DetachedResidentFactoryReceipt`，把480次成功原配置求值与释放一一关联，要求24路径各20次、精确失败slot、完整起止、同stderr内assignment→release次序。原一般assignment诊断仍允许合法拒绝/未求值行，只有“成功工厂完成”消费者收紧。
- 现有纯合同脚本追加12项正反例；原145项不删除，共157。

原生目标RED171445：原432已完成，但新工厂次数expected480/actual0，native1/1942ms；不是资源损坏RED。补齐后171921 native0/3234ms、parent172307 native0/3604ms，均为诊断绿色。

唯一fresh两文件审查发现Important：新消费者误收未求值行以及释放先于赋值的记录。root同意；先抽出未改语义的实际消费者，再运行12反例，亲见7项误收RED：未求值、释放早于trace、晚于trace、缺BEGIN、截断/畸形释放前缀、字符串batch。一次修正后157项GREEN，再跑两组真实480序列及完整普通pre/post/models。没有第二审查；第三个帮助器差异是这次修正的TDD覆盖，由root执行验证。

## 配置终结的实际范围

四个绿色诊断各有480 assignment、480 release，24个Profile serial各用20次；每案Profile24 ENTER/24 RETURN，23组disposing=True，1组disposing=False。前者主要发生在退出drain，不冒称自然终结。False对应：

| 案 | 自然终结serial/配置/slot |
|---|---|
| 171921 | 441 / scrap-heavy / gear-pistons.06 |
| parent172307 | 498 / bone-heavy / steam-valves.06 |
| 174505 | 432 / scrap-diver / controlled-abyss.04 |
| parent174546 | 447 / scrap-scatter / low-ceiling-bridge.03 |

最后一项给出同类真实保存配置自然终结的正常对照，但不是v096坏serial1808本身，也不是native owner或GCHandle原因证明。TRACE_END各480/480，unpaired/fault/truncated/after-source/rejection均0。

## 全部十一案结果

证据根 `G:/knight/验证证据/october13-stable-claw`；表中时间短名对应唯一同日阶段目录。11次构建均exit0/错误0/警告0，全部headless。root唯一拥有构建/原生进程，存活期间源码、帮助器、文档全冻结。

| 时间/阶段 | native退出/毫秒 | 严格结果 |
|---|---:|---|
| 171445 REF detached | 1/1942 | 新覆盖目标RED，原432不等24保存叶。 |
| 171921 REF detached | 0/3234 | 诊断PASS，原432＋24×20。 |
| 172307 REF parent-loaded | 0/3604 | 诊断PASS，parent只加载未实例化。 |
| 172806 ordinary pre | 0/172254 | PASS。 |
| 173140 ordinary post | -1/180236 | 原180000总cap TIMEOUT；31 enter/30 return，最后index30，ERROR0。 |
| 173520 ordinary models | 0/560 | PASS，普通构建恢复。 |
| 174505 REF detached | 0/3728 | 新严格消费者下诊断PASS。 |
| 174546 REF parent-loaded | 0/4368 | 新严格消费者下诊断PASS。 |
| 174627 ordinary pre | 0/166208 | PASS。 |
| 174950 ordinary post | -1073741795/163262 | 非超时真实FATAL；HUD前置已因低顶桥`.03`ProfileMissing回Title，原75尚未进入。 |
| 175313 ordinary models | 0/2708 | PASS，最终普通构建。 |

总计8 strictPASS/3FAIL，不是整关完成比例。原始ERROR2＝首个覆盖断言1＋最后FATAL1；两post原有限定警告共4，不把backtrace或external镜像重复计数。最后还有一条已保存的托管失败详情：HUD要求Playing但实际Title；原生崩溃使该异常没有按普通退出路径完成，不能只保留外层scope mismatch而省掉配置失败和FATAL。

最终同冻结pre通过、post失败，原完整75/后置scope未通过。Main/Explore未改策略、未重复跑以求安静；旧交叉火力/E0失败及所有完整验收仍OPEN。最终纯157、其他96/26/33/11/24实际通过；负例仅在root原生全部terminal时执行。

## 查明的验证预算风险

`RunPostPhase`在75项注册前，先执行30个原suite/contract调用，包括实际物理遍历、跨区往返/恢复、UI/HUD等，不是轻量“注册准备”。构造75个delegate本身不提前实例化fixture，不能把约165秒归咎于delegate分类。

| 普通post | 注册起点engine_ms（各边界engine−elapsed恒定） | 最后注册边界 | 原终态 |
|---|---:|---|---|
| 本轮173140 | 165563 | index30 enter179406 / 注册已耗13843 | 180236超时；无Handle/FATAL。 |
| 历史154225 | 164803 | index30 enter178464 / 已耗13661 | 180097超时；无Handle/FATAL。 |
| 历史055917 | 160211 | index37 enter179902 / 已耗19691 | 180052超时，另有4Handle，仍FAIL。 |

173140的index30开始时，相对名义180000仅余594ms；历史055917同一case实际耗2303ms才返回。本轮还有45项未完成以及production-assets/accessibility/process-owner/退出drain，存在有证据的整套预算覆盖风险。owner Stopwatch与engine时钟起点不同，不能把二者相减当该case精确运行时长，也不能断言其hang。

三案封存的`RunPostPhase`方法文本与当前一致，但其callee未必同源，不作性能A/B。180000是旧helper整个native进程自定限值；只读检查20份已有设计spec、AGENTS/godot说明未找到用户给出的180秒全套验收要求。本轮仍保留该上限和全部timeout FAIL，没有修改预算。下一应基于原完整前置/75/后置scope，明确可测的执行预算而非一直在旧截止点重复；不能把预算修订用于豁免真实FATAL或翻写旧结果。最后174950在163262ms、注册前崩溃，与预算不足是两个问题。

另两个具体候选已只读否定为当前post的先行破坏源：`ResidentSavedBodyGeometryTests`的SetScript负控作用于每次新建私有Shape/Pack，且只由专项runner调用；`RangedVolleyDamageLedgerTests`的using GD.Load仅在ranged-ledger专项调用，均不在普通pre/post可达链。v082反证的是GetSlideCollision/KinematicCollision2D using，不混称同一操作，也不据`using`关键字单独推断资源所有权错误。

## 正式武器接入口：本轮未实装

已有获批半钟刀仍可复用，未改变PNG、未生成/付费。只读核到真实绘制的近臂末端约为formal局部(13,19)，当前程序武器起点约(12,7)，已有约12单位的握持落差。把母版握点(548,798)直接放到旧线起点会继承脱手问题；占位atlas pivot不能证明适用。故本轮没有为“已接入”数字把PNG硬塞到错误轴点。下一实际接入需先对正常Player、各动作段及左右镜像建立真实握持约束反例，再以原旋转/动作所有权接入部件；GPU像素/遮挡/正常速度及用户观感仍另验，不等于总装已批准。

## 唯一审查与root裁决

C0/I1/Minor0；Important如上已亲见RED→157GREEN并重跑原生，不复审。原有清理异常、观察时序及负例条件副作用等开放项不因本轮消失。

| 审查未判断项 | root裁决、理由及代价 |
|---|---|
| 1 实际W与旧preview | 未改读取/排名；仍需真实边界，不能用离树资源记录代替，路线资格不关闭。 |
| 2 物理首hit与资格 | 没有新LOS授权/first-hit实验；原遮挡事实保留，代价是目标选择仍待验。 |
| 3 Intro与Playing | 刻意离树未Ready，不是启动通过；普通post明确真实Title失败，不能放开准入。 |
| 4 callback/deferred | 没有挂树回调身份连续性证明；root Free成功不能替代同actor/parent/pose跨边界执行。 |
| 5 LoadLevel主错/清理错 | 未改loader；新返回根的finally不是所有caller安全证明，原注入义务保留。 |
| 6 历史FATAL/全世界因果 | 四个诊断quiet均不修历史；同类配置有正常对照，下一比Ready/实际退场，不定罪保存叶。 |
| 7 最终普通回归 | root按表保留pre PASS/post真实FATAL/models恢复；新consumer修正后仍未完整，不把focused拼绿。 |
| 8 正式美术/路线/视听/硬件真人 | 本轮没有新增运行资源或显示；全部原要求OPEN，手部差距不靠静态预览掩盖。 |
| 9 历史dirty整工程 | 审查仅两个指定封存基线；第三帮助器是重要修正TDD由root自审，未声称整checkout审过。 |

其他执行裁决：继续用户指定脏工程、root唯一写入/启动、不建工作树/提交；代价是必须依赖逐案冻结保管而不能按HEAD笼统归因。保留原432再加480只说明组合序列，不冒称纯24叶最小充分复现；未加入中途GC或无限循环。严门只收紧完成工厂，不破坏一般诊断拒绝行；误判代价由原正/负控和真实序列再跑承担。预算问题只列事实和后续工程判断，本轮不改分数、不延时求quiet。

## 最终冻结与留档

最终普通案`models-20261024-175313-901-e5ab4e2528c34d468b45f58ff4d14e63`。独立只读核验11案55对manifest逐字节一致、66个OWN bins各匹配自身清单，缺失/额外/重复/哈希不符0。完整live1480runtime＋296imported＝1776/C#892，新增/缺失/漂移0；普通models9项帮助器及最后REF11项均匹配，后者包含157合同脚本。当前bin实际6项/额外0，全部匹配最终普通自身清单；不由编译成功代替运行资格。

- source manifest：55687C0BA11E8961B71CB70B708CC448150A1E4E46B4A0840101094CB1ED137F
- helper manifest：7600D1CBD1D10371F2034339373AC6B264777C26E612C1678E3FA6FDD86D5FCF
- 最后REF helper11 manifest：D8C38695A883226D614FD70E580AB45C0957AC63E0801606FC0EC026F0B7CED3
- binary manifest：24051A2FEB904BD56C692FDA8941804D37948D85868C3905306A995D4CE58FAD
- 普通Knight.dll：CC2003EF8A2675CA63CC7DB60D35E66AFD9B31330F9523202D0B46040CD17063
- branch/HEAD/index仍为原codex分支/e7c70498…/21610FDC…，无Git写入。diff --check exit0、既有169条LF→CRLF提示，其他0。
- 媒体单次检查09:54:31.4671446Z，860284 bytes/基线3508/增长856776，Notify=false；无新图稿实录，不建自动化、不停止工作。G盘只读余量179.95GiB，不删除资料。

所有root原生/构建拥有者terminal后才更新本文与状态文档。本轮PROGRESS是实际保存叶覆盖、严格完成证据修正与时钟边界核实，不是游戏玩法/美术新增或崩溃修复。完整十一项、76普通＋Boss/NPC、全部机关、正式视听动作、完整双路/相机、硬件真人、版本发行均仍按原要求推进，不设task-done/目标完成或blocked。
