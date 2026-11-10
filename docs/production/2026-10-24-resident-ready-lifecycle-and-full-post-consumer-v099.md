# v099 · 保存敌人的入树生命周期与完整后半段时限消费

2026-10-24。唯一工程仍为 `G:/knight/Knight`，Godot/.NET，原十一项目标ACTIVE。用户最新更正为“没有看到窗口”；不把此前26秒当作可见性、教学遮挡复现或画面验收。本轮无前台窗口或系统键鼠输入，无新玩法、正式美术/声音、付费生成、自动化、Git写入或删除。

## 事实、推理、风险

- 事实：最终普通pre严格通过，native0/177730ms；普通post在300000ms时限内于169867ms真实FATAL，原75表21 enter/20 return，最后index20是 `MountedGuidancePassesPointerInputThroughEveryOverlayControl`。它不是超时。本案没有新的ProfileMissing/HUD启动失败证据，不套用v098低顶桥Title归因。
- 推理：v098完整原范围206055ms已超过旧180000ms工程总时限，应修正普通post的测试工作量预算；但增加时限不能修复资源。本轮原工厂再加真实入树/Ready/退树对照仍安静，只能缩小复现条件，不能判定资源所有权或关闭历史FATAL。
- 风险：普通资源崩溃、完整正常标题主线/探索、齿轮战斗与冷却、正式武器握持/动作/视听、相机、硬件真人及发行均OPEN。原76普通敌人＋Boss/NPC、地图/机关、所有原十一项稳定ID不缩减。不用测试收据或文档替代游戏完成。

## 改动与真实RED→GREEN

### 完整测试时限的实际消费

仅普通、非REF的 `prologue-post` 改为300000ms。普通pre及REF post仍180000，既有 `-MeasureFullPostBudget` 诊断仍600000；原runner、30组前置、75注册案例、后置、错误/警告/范围及保管链不删不放宽。provenance保存实际 `nativeBudgetMs`、旧180000、`fullPostBudgetCorrection=true`、`original180Accepted=false`、`performanceCertification=false`；旧失败不回写为通过。

新 `test-post-budget-consumer-v099.ps1` 执行当前AST中的真实 `$engineCapMs` 赋值及真实 `Run-Owned 'engine'` 调用，仅以桩截断进程启动边界，覆盖普通post/pre、大小写、REF/测量/原其他阶段和错误常量/调用连接。先观察旧180000的实际RED，再11项GREEN。旧预算测试24项仍通过，回执精度改为函数/准入覆盖，不再把它自身声称为完整deadline消费者覆盖。时限调整不等游戏帧性能认证。

### 真实保存叶的同步入树与退树

仅REF诊断新增 `resident-resource-ready-control` / `resident-resource-ready-repro`。两臂完整保留原432载体、24真实保存叶×20原工厂创建/配置/Free的480前缀，再经同一工厂创建一次 `scrap-scatter` / `canyon.v2.resident.low-ceiling-bridge.03`，即assignment481，仍在原512 trace上限内。

A直接离树Free；B同步Root.AddChild→真实Ready→Free，记录TreeEntered/Ready/TreeExiting/TreeExited及顺序。两者挂载期无物理帧、之后同一额外ProcessFrame，不创建替身Player/Flow、不改姿势/资源缓存、不显式Dispose借用资源。该变量包含树、Canvas/Physics注册、ExitTree及延迟队列，不是只测Ready函数；默认根挂载也不代表原世界父级/位置/实战。

精度纠正：原有退出drain本身已显式GC两次。本轮保留原调用、只加同stderr ENTER/RETURN界标，`original_gc=true added_gc=0`；不能称自然GC或新增“修复GC”。初始192025实际native0且432/480完成，但缺481，canonical严格FAIL，作为实现前覆盖RED保留，不称真实资源行为RED。

最终A/B均严格通过：

| 最终案 | 入树/Ready/退树/退出 | 顺序 | 物理计数前→后 | 原设置/Free后失效 |
| --- | --- | --- | --- | --- |
| control194257 | 0/0/0/0 | 0 | 108→108 | true/true |
| repro194513 | 1/1/1/1 | 1234 | 113→113 | true/true |

两臂原432/480及assignment1..481完整。严格收据先补scalar字符串类型的实际误收反例；一次新最终review又发现release480和BEGIN481之间缺时序连接。实际64项RED捕获4个错误变异（两臂各release480移到Ready481后、ENTER481移到BEGIN前），一次fix要求同stderr：release480 < BEGIN481 < ENTER481 < assignmentROW481 < ReadyROW481 < traceEnd < 原drainENTER < RETURN。最终64项使用上述最终A/B日志重新执行通过；没有复审或生产玩法修正。

最终资源身份只读核验：A的op481 Profile为serial437/hash18123955、B为serial437/hash33105105，两者BindingSerial1615；数值属于各自进程，不跨进程当同一对象。各在 `9+24k(k=0…19)` 及481共21次/21个不同binding使用，不是第481根独占。A精确Dispose call1110在stderr9412→9413、B call1135在9462→9463，均disposing=True/thread2且在各自原drain RETURN(9349/9355)之后。Setter/Field一致、Calls1/Null0/Fault0、GateMissingfalse；这不是目标的Dispose(false)或历史FATAL复现。

## 全部11个canonical终态

证据根 `G:/knight/验证证据/october13-stable-claw`。表内目录完整；strict不是native exit0的别名，六个Ready诊断通过也不是普通玩法资格。

| 目录 | build exit/ms | native exit/ms | 严格结果 |
| --- | --- | --- | --- |
| resident-resource-ready-repro-refcounted-identity-20261024-192025-307-7d9572bd52784f95b0ea42451a0f8a2a | 0/32448 | 0/3131 | FAIL，真实覆盖RED，缺481 |
| resident-resource-ready-control-refcounted-identity-20261024-192320-292-b7bd2f47e4544534b0346d57792d5a64 | 0/29996 | 0/4450 | 诊断PASS |
| resident-resource-ready-repro-refcounted-identity-20261024-192617-529-b204e438d92e4db290ecbf0f8d6dd7b6 | 0/26895 | 0/3239 | 诊断PASS |
| resident-resource-ready-control-refcounted-identity-20261024-193018-106-9fc4e73bba64493ab96d0365fcb36d85 | 0/25794 | 0/3217 | 诊断PASS |
| resident-resource-ready-repro-refcounted-identity-20261024-193208-478-8327e2bee81a41bdbf242f273071d74f | 0/29504 | 0/3138 | 诊断PASS |
| prologue-pre-20261024-193451-204-8f8f1debc5b54ce2b77e29b19717ef6f | 0/35304 | -1/180213 | FAIL，真实执行watchdog超时＋5 Handle |
| resident-resource-ready-control-refcounted-identity-20261024-194257-968-a624a90c8b9b44d48ef9d6c8bde13352 | 0/24472 | 0/3037 | 最终A诊断PASS |
| resident-resource-ready-repro-refcounted-identity-20261024-194513-595-6d006566c2674622a7c0f445c5f0d044 | 0/28540 | 0/3228 | 最终B诊断PASS |
| prologue-pre-20261024-194632-320-57a7ade3eba24b0ea8ca077e6f0869e2 | 0/27865 | 0/177730 | 最终普通pre PASS |
| prologue-post-20261024-195208-434-e960473155c34679bda60eb330e918ea | 0/25614 | -1073741795/169867 | FAIL，cap300000内FATAL，75表21/20 |
| models-20261024-195622-860-5c51d9a0f13c4d3392cfef37e08d9f20 | 0/26583 | 0/534 | 最终普通models PASS |

11 build全部exit0；native9个exit0、1个实际超时、1个FATAL异常。严格8 PASS/3 FAIL（PASS含6诊断）。主要ERROR10＝Handle9＋FATAL1，WARNING2为既有observer/room-profile且原限定scope在，ShapeRID0；external镜像不重复累计。超时案停在AwakeningCourtyard遍历sample0.5，无phaseOK/drain；不是观察等待超时，也没有因此自动重启。最终post有4 Handle＋FATAL，不具备完整75/后置资格。

最终纯合同157/96/26/33/11/24/24/11/64全部exit0；64明确使用最终194257/194513日志。纯测试不启动native，不代替正式完整双路。最后普通models重建还原无REF二进制，非诊断入口留作游玩。

## 一次最终review与root逐项裁决

新上下文只读review：Critical0、Important1、Minor0。Important时序连接已实际RED→一次fix→GREEN；普通post仍失败，不称全部回归绿色或目标完成。没有第二次review。以下10项Declined均保留root裁决和错误代价：

1. W输入/reader/controller/preview排序未动，保留旧路线工作；本轮不是正常键盘第一输入证明，代价是相关路线仍可能失败。
2. 敌人实体遮挡/receiver准入未动，没有新first-hit因果；不改变穿透或优先级，代价是未解决的爪目标问题仍在。
3. 完整Intro→Playing/恢复生产语义不在本次delta，沿用v098的真实等待消费者；不能据Ready诊断重新认证准备拒绝，代价是完整回归仍需守门。
4. 零物理根挂载＋一次Free后机会不覆盖原世界父级/位置/physics/全部延迟回调，不能声称实战生命周期完成；代价是Quiet A/B可能遗漏真正崩溃条件。
5. LoadLevel及primary＋cleanup异常实现未改、未加故障注入；继承清理/局部无界signal债务未关，代价是失败可能只能由外层进程界限结束。
6. 资源FATAL/nativeowner因果未证明；A/B安静不能归因或宣称修复，代价是仍阻挡稳定交付。
7. review派发时普通pre/post/models尚未最终完成，以root真实终态为准：pre与models通过、postFATAL；代价是不授完整普通回归资格。
8. review未独立消费最终B终态，root已实际严格重跑最终B及64并另行只读保管核验，不冒充复审；代价是该案结论只限实际收据合同。
9. review未完整核完源/bin/index保管链，另行只读audit与root独立rehash处理，不声称review覆盖；代价是若不一致须撤销对应构建归属。
10. 原正常Main/Explore、人口实战、正式视听/相机、真人硬件与发行仍OPEN；不从模型/诊断通过勾原十一项，代价是不能最终交付。

其他root裁决：修正工程总时限而不弱化原范围；保留明确的原GC而不新增GC/cache补丁；只选一个真实工厂末端入树变量，不做批量猜测阈值；标量字段及同流时序严格否决，日志不全仍FAIL。成本分别是测试可能运行更久、旧资源故障可能继续、不能涵盖完整世界、历史失败不能靠native0洗白。

## 来源线索，不是修复依据

只读核对官方后，发现Godot仍开放的[资源缓存/GC句柄问题83762](https://github.com/godotengine/godot/issues/83762)描述了缓存重新取回资源与托管回收/强弱句柄切换之间的可能竞态；[另一个缓存实例化报告112067](https://github.com/godotengine/godot/issues/112067)资源类型不同，不能直接套到本工程。安装版本对应的[官方源码a13da4feb](https://raw.githubusercontent.com/godotengine/godot/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/csharp_script.cpp)可作为静态路径线索，但不是本次动态根因证明。

因此下一步应让实际失败与缓存/回收/再引用边界相连，而非重复Ready安静对照。既有[v075缓存实验](2026-10-24-cache-boundaries-and-regression-evidence-v075.md)中Reuse控制本就安静，IgnoreDeep安静不能当修复；不直接加常驻pin、GC、缓存切换或升级引擎。本轮没有改这些生产策略，也没有安装工具/改授权。研究检索工具不可用时已告知降级为现有只读网页检索。

## 最终保管链、媒体及未完成项

独立只读核验11案55对before/after manifests、66个OWN封存bin全部匹配。最终源完整1480＋imported296＝1776/C#892，当前missing/extra/drift0；models9、最终Ready12、post10 helpers与当前匹配，实际bin6/extra0。所有root原生owner终态后才写本报告/READMEs/总待办/意见/账本，期间冻结源、帮助器和文档；最终报告后另作rootrehash，不回写旧证据。

| 范围 | SHA256 |
| --- | --- |
| 最终source manifest | 17BAB1989E38A623B2F8F17EDDF218DBA5AFC631FE1B2C20592BFBCF5CD985F8 |
| models helpers9 | 0F1A45671CF174AC1406AAB656C66253D4F1247214ED57BA7CF5B9969C9FC131 |
| 最终Ready helpers12 | 2A54A09870DA7060511EFF61FEF0AA627CA8C2327BC14EE16C124ECD3C7C621F |
| 普通post helpers10 | 55DD8E47D365AF51AEC6F2D27A8BD8F8CA62E9AD51F83BE3AC24ECE40846DC7F |
| Ready纯64 | FAB1EDD105767F06A895F4142E485C75862B590E17B3E1B87B602588815CA7AE |
| 真实deadline消费者纯11 | B04013330882F906AB258ED6E0161100ABECED3B4D9E91ADE09B10AC3ABD9AEE |
| 预算纯24 | 06CAC47AE98FFA197B8B8EAA7D010B66396B2542EE9DF0786C78E8F9BC17B1B3 |
| canonical | ED71BCFCDB771D8B20BAF09953DC42C9EE02E9D4C61981A45B6A8EF2D8467B64 |
| bin manifest | BC7D4ED4C7CF6C272D213E1D763C7F361FFE85ECCDFDD3AD7AD8101ADF7DA61C |
| Knight.dll | 2871AC422E2DFF9750C5A04BBAC2474CC21C172EB3D5665880480405763C67FF |
| Git index | 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C |

branch `codex/task6-certified-final-20260902T0555` / HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`不变。2026-10-24T12:06:43.5210332Z仅一次媒体检查：Current860284/Base3508/Growth856776 bytes，Notify=false；无新增媒体、不恢复自动化，不把20GiB当停工条件。

文档更新后root独立逐项rehash1776源及三个最终helper范围的13个唯一文件，漂移0；index仍同SHA，无root拥有的Godot/dotnet进程。`git diff --check` exit0，169条既有LF→CRLF提示、其他消息0。没有以文档更新触发新构建或重写当案终态。

原十一项总目标仍ACTIVE。下一依赖为资源寿命的真实因果复现及普通全回归，再接当前主线/探索的战斗和移动验证；获批武器握持约12单位落差、正式身体/披风批准版本链、正式UI/动作/音频和真人显示验收仍在原清单，不用这次测试工作替代。
