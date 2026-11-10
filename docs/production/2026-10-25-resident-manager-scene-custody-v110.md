# v110：关卡内常驻敌人的源场景持有

2026-10-25，KNIGHT / `G:/knight/Knight`。原十一项目标仍 ACTIVE，非正式交付版本。用户“等一下，是没看到”仍为 NOTSEEN，不认定看到了窗口、教学仍遮挡或画面已获认可。本轮无窗口、系统键鼠输入、新图稿或录屏。

## 事实

### 实际管理器 RED → 限定生产修复 → GREEN

此前 v109 隔离实验揭示了缓存资源在托管弱目标死亡、finalizer 尚未完成时再次使用的异常。本轮先在真实保存 Level 的 Title 阶段验证实际 `CanyonResidentManager`，不使用 FinalOnly、强制角色位置、复制碰撞形状或冻结敌人的夹具。

071228 案第一次出生及释放成功后，同一 live manager 再出生失败：实际 `canyon.v2.resident.low-ceiling-bridge.01` 的 Scene/Profile 均 weak-dead，ReloadProfileId=0、Committed=false、`profile_missing=True`；测试门正常进入/返回、没有超时，之后真实 FATAL。构建成功，故不是编译失败冒充行为 RED。本案未启用 REF，不由此单独断言崩溃对象的托管类型。

在修改生产代码前，先用现有 `ResidentOriginalSceneSources` 测试只持 PackedScene 的较小方案。实际24个 authored resident 叶、59个自定义托管资源、3个嵌套正式场景，跨两次 GC/finalizer 边界均保持相同有效身份；24次原工厂实例化匹配预先保存的 ProfileId，加载次数仍24。读取器只返回弱引用和标量，不直接强持59个资源、不 Dispose 借用 Resource/SceneState。59个资源按类型为：Profile24、Presentation15、ArchetypeTuning8、LaborerTuning3、SkitterSettings4、RangedSettings4、SentrySettings1。

这个正面对照是 cache-warm 图特征证明，不是全部24类冷进程首次加载证明。当前 PresentationProfile 的 VisualScene=null、CueScenes为空；3个实际嵌套场景来自 SceneState.GetNodeInstance，不能据此声称已覆盖未来非空数组/字典资源图。

生产仅修改两个文件：manager 构造时建立按当前 subtype 路径限定、延迟加载的源场景持有器；TrySpawn 从该持有器取同一保存场景后调用未改的原工厂；Dispose 只清除强引用，不销毁共享资源。没有静态永久缓存、生产 GC/finalizer 门、通用生产图遍历器、缓存模式/引擎版本变更、空配置回填或重试。原准入、碰撞、出生/恢复顺序保持。

最终072401案在两个独立正常 Level/manager 中，实际重生保持同 ProfileId，Scene/Profile均存活、Committed=true、门正常返回且无超时；退休 manager 本身仍被持有时，两个 Scene/Profile 实际均 weak-dead，TrySpawn拒绝。原正例/重复出生拒绝、默认占位拒绝及 FinalOnly拒绝保留。真实总数为6次出生，其中4次属于持有探针、1次新 manager 替换；不是“仅1次出生”。原 resident 完整生命周期/几何/重入及原源场景修改兼容性案随后通过。

### 独立严格回执

原 source-lifetime 六阶段/两赋值，以及 v109 source-resurrection 六阶段保持。成功 rooted-B 后才追加独立 `RESIDENT_SCENE_CUSTODY_COMPLETE`：24 roots / 59 managed / 3 nested / before59 / after59 / factories24 / loads24 / released24。标记仍明确 gameplay=false/resource_fix=false：该图特征探针自身不是整关或全资源故障修复证明。

先删除新标记，执行真实 canonical owner 分支，实际得到旧消费者误收的 RED；之后要求精确唯一标记和各计数，缺行、未知前缀、重复、59→58、3→2、加载25或释放23等反例均拒绝。最终重新执行66条纯合同、预算函数24条、实际预算赋值/启动接线11条，全部通过，均未启动游戏。没有放宽错误、警告、FATAL、退出码或原时限的否决规则。

## 全部十一原生案例

证据根 `G:/knight/验证证据/october13-stable-claw/`。十一构建均exit0，十一原生均非超时，严格7PASS/4FAIL；最终七个同源案例为4PASS/3FAIL，不拼成绿色完整suite。

| 目录 | 构建ms | 原生exit / ms | 严格结果 |
| --- | ---: | ---: | --- |
| `resident-development-published-20261025-071228-607-6507f754a5ba4e908c45229fe3581cac` | 28892 | -1073741795 / 8207 | FAIL，实际manager RED/FATAL |
| `resident-resource-source-rooted-refcounted-identity-20261025-071550-730-6fcc3995915c49eaa7bc9f52e62ef354` | 28311 | 0 / 1028 | PASS，24图特征当时人工核对 |
| `resident-development-published-20261025-071933-415-621b41e679e040689d220a79ec105ba6` | 29431 | 0 / 2436 | PASS，限定生产持有后 |
| `resident-resource-source-rooted-refcounted-identity-20261025-072014-686-3d979cae7b7648ec8194b164713cd917` | 24789 | 0 / 1298 | PASS，新严格图回执 |
| `resident-development-published-20261025-072401-927-202db0387daa45388088cc7be109a14e` | 28615 | 0 / 2684 | PASS，最终双manager/退休观察 |
| `resident-level-lifecycle-20261025-072442-904-0dd3455839d84ae3b16d7cc2a1c9d206` | 33821 | 0 / 5450 | PASS，原兼容性断言 |
| `thruster-route-main-diagnostic-20261025-072810-160-ec3b18d4256f43a9a355f6aa9594e243` | 31633 | 1 / 140089 | FAIL，死亡后的路线合同 |
| `thruster-route-exploration-diagnostic-20261025-073114-884-740e3a79ee0a4bdfb6bb3a758c67f90c` | 28717 | 1 / 55134 | FAIL，冰区死亡 |
| `prologue-pre-20261025-073248-668-166cb79b10284d78a6a6458250fff81f` | 28714 | 0 / 165551 | FAIL，仍1条Handle错误 |
| `prologue-post-20261025-073613-768-6b66dd0a5a0047a5b5e423ff3db9f573` | 31868 | 0 / 212011 | PASS，原75及owner8次 |
| `models-20261025-074030-065-23b1d59618004e569d31d4b2564376f9` | 37459 | 0 / 804 | PASS，恢复普通Debug |

Main 完成低顶桥/裂隙/深井及齿轮战斗回执，经过 Basin 检查点、横廊交接和 LiftBackLanding，真实 Moth 抓爪及交接成立。但 counterweight-lift 中 resident precision-burst 连续扣血，7068时持盾却因E0未激活，最终7115被原wave BeltBurst attack4打至0。随后DEATH RESET把角色从(4751.553,3423.933)送回保存的Basin检查点(1680,3960)，触发原无重置位移断言。它不是独立于死亡的无故传送证据；也未完成整条主线。

Explore 完成桥/裂隙/档案侧洞/frozen-upper，在frozen-lower死亡。2056有一条3→2伤害归因缺口，2192实际落伤2→1；2861原wave Moth attack3在盾按住但E0未激活时造成最后一击。观察器在任何恢复请求时立即锁定ZERO_RECOVERY_RED，随后路线中止；request1/completion0本身不证明生产恢复逻辑坏了。

两条路线各仅1条实际断言ERROR，没有WARNING/FATAL/配置为空或无效资源特征；只能说明这两次限定运行未再遇资源错误。pre仍在 `BasinAscentCameraRuntimeTests.CheckPosition:98` 的AddChild路径出现1条GCHandle.FromIntPtr/SwapGCHandleForType错误；虽然exit0且最后打印phase完成，严格仍FAIL，资源总问题不能关闭。

post独立数得原75 ENTER/RETURN完全配对、index0–74、retired0、owner重复8次；三预期负例警告scope为seamless1/room-profile1/flow-registry2，无新豁免。stderr和external是同四条警告的两个副本，不能计作八个独立警告。212011ms在原已批准300000ms post预算内，不是性能认证或180000ms通过。

## 推理

实际live-manager RED及相同源持有方案的GREEN，支持这个范围内的生产修复：常驻敌人源场景应跟随关卡管理器，而非随一次演员释放而失去托管根。它没有证明所有历史资源错误同因；pre反证了“全问题已修好”的说法。

两路死亡证明当前自动策略尚不能完成原要求，不足以推出人类必然过不了，也不授权删除敌人、改HP/能量/窗口、跳过原波次或降低关卡要求。下一步应核对死亡前的实际能量/输入/攻击机会，并单独补Explore伤害来源缺口，而不是再猜一个无依据时间常量。

## 风险、单次审查与逐项裁决

单次全新只读六文件增量审查 C0/I0/Minor1，无第二审查或新修复回合。Minor延期：退休检查实际打印ProfileAlive=false，但断言只要求Scene死亡/退休拒绝，未来单独Profile强持有可能漏报。当前没有观察到该泄漏；后续若把Profile退休作为严格保证，应补私有单所有者反例，而不声称共享资源全局必回收。

审查拒绝替root认证的范围逐项裁决及误判代价：

1. 前一Level包装对象finalizer仍pending时，新manager首次缓存取用仍OPEN。GC-drained替换不是此交错证明；误判会漏掉首次加载崩溃。
2. 全引擎/cache问题未修复，pre仍失败；误判会交付不稳定版本。
3. 图特征为warm-cache，不追认24类冷启动；误判会遗漏首次赋值边界。
4. 实际live-manager探针覆盖首个rock slot，24类是离树原工厂图证明；误判会把形状/Ready/战斗全覆盖偷换成资源计数。
5. Main以本轮死亡FAIL裁决，不因局部抓爪/交接通过改绿；误判会遗漏完整出口。
6. Explore以本轮死亡FAIL裁决，伤害归因缺口保持；误判会掩盖实际伤害路径。
7. 普通pre FAIL/post PASS/models PASS分别保留，不用exit0或历史绿色拼接；误判会隐藏真实ERROR。
8. W实际reader/controller与旧preview排序本轮未改，不续签资格；误判会漏输入时序故障。
9. 首物理遮挡与receiver资格无新认证、无第二resolver授权；误判会掩盖穿墙/错选。
10. Title准备阶段的manager探针不是实际Playing准备完成；误判会绕过开局门。
11. callback/deferred退出的actor/parent/pose/sequence义务未变，不由资源持有续签；误判会漏重入和退树故障。
12. LoadLevel调用方try前清理、主异常与清理异常同时保留义务未改；误判会遗留节点或丢主错误。
13. 原十一项、76普通敌人＋Boss/NPC、完整正常双路、正式视听/相机、目标硬件/真人、授权版本交付均保持ACTIVE；用户NOTSEEN。误判会把headless合同当画面/手感/完成体验。
14. 持有量按实际使用的有限路径有界，但真实内存/长局性能未测。误判会忽略驻留成本；不能因仅保存24条路径就报告RAM/FPS合格。

## 保管、源增量与未变边界

独立核对十一案55/55前后清单对、66/66封存二进制SHA，缺失/漂移/额外/格式错0。最终七案source清单同为1844项，SHA `4A23DCAEE39CE86EC6FE5828CEC5922424829FCB3081439AB015AFC307A9D8B4`；live1844/1844、普通Debug6/6匹配。C#892，无新增/删除源文件。相对v109仅以下四个C#改变，另改canonical owner及现有纯合同帮助器：

| 文件 | 当前SHA256 |
| --- | --- |
| `scripts/prologue/residents/CanyonResidentManager.cs` | `0E44C5854CD2F1D41FBEFE3E1058942F0EBDC8E1832D14CAE626B8D8DE0513BA` |
| `scripts/prologue/residents/CanyonResidentManager.Spawn.cs` | `0FDA76DC26F590B37AF8E361FD858934D5851ED16895F4396964208CF7F7B44A` |
| `tests/diagnostics/RunDetachedEnemyResourceRepro.cs` | `88212CE3BAD2EB3A31D2CBF5C2AB72BAE24D5B0759D5A65C9D781E8B19D10CBE` |
| `tests/integration/ResidentDevelopmentPackageTests.cs` | `D917C4763B3FDBDECDBF1495B9CDB3B74F6F2E62D0E1B90A8D6AAE8D5EEEF8E6` |

owner SHA `309DD6C42FB5E96F27390711FF91D2D419890CF17BA256869E2D2A22961255F2`。原Manager实体快照SHA2B20B10E…已核；Spawn旧实体没有找到，只在内存逆转单行得到SHA57519420…、与v109inventory精确匹配，未伪造旧文件。原Main测试不变。HEAD e7c70498…、index21610FDC…、音频总线CAD39DF…保持；没有Git写入/标签/Release、删除、付费生成、自动化或真实存档语义读写。

所有root构建/原生句柄及最终source审计结束，确认工程Godot/dotnet进程0后才更新文档。v110是开发记录号，不是发布号。媒体只执行既有一次Check，UTC2026-10-24T23:42:25.4272582：Current860284/Baseline3508/Growth856776 bytes，Notify=false，新媒体0；没有后台通知或停工。

文档更新后diff-check exit0，既有LF→CRLF提示169/其他输出0；四项当前C#SHA及Git index再次精确匹配，没有再启动原生或改动候选代码。

原十一项目标继续ACTIVE。后续优先核对两条正常路线的战斗能量/输入机会与Explore伤害归因，同时保留首次跨manager缓存取用和pre句柄故障的资源边界；不重复无变化绿色测试充进度，不把本次窄生产修复替代正式视听/相机、真人硬件或完整交付。
