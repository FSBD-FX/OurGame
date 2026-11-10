# v147 · 玩家配置来源与普通回归（非发行版本）

2026-10-27。工程 G:/knight/Knight，Godot4.7.1/.NET9。原十一项目标 ACTIVE/NOTSEEN。本轮只补稳定性诊断，没有完成任何整项，不是新的美术、动作、玩法或通关交付。

## 事实与改动边界

v146短流程283b在23.849秒出现PlayerTuning serial1/hash56200037的终结器gchandle FATAL，没有该对象出生/赋值身份。未改引擎、释放政策、缓存、默认配置、物理参数、瞄准优先级、敌人或地图。

只在 REFCOUNTED_FATAL_IDENTITY 符号内：

- 真实PlayerTuning构造函数记录已有serial、托管hash/线程/有界堆栈；构造和终结器不新增原生查询。
- 六个原new表达式分别观察其返回的确切对象：Motor/Combat默认、各自Ready空值回退、timeline空值回退、coordinator空值回退。保留原分支懒求值，不新增保活。
- Motor/Combat在原空值回退之后、Validate之前观察实际持有配置。仅已声明主线程查询原生身份，失败时保留对象、否决完整性。
- 使用原有有界纯标量journal/身份表，普通构建排除全部新增钩子。

仍1857运行文件/897C#；原143实体、9残片、76普通敌人、Boss/NPC及十一项范围不变。

## 实际来源与推理边界

中间6d2b短流程0/6702ms，最终3036短流程0/6730ms；两个旧委托及原清理不变。两次目标Profile终结器均not_exercised，不能算自然终结器稳定性通过。

最终3036的PlayerTuning serial1/hash60249682出生堆栈为ScriptManagerBridge→ResourceLoader.Load/GD.Load→原Task5。Motor/Combat的四次Ready（两个真实玩家）持有该同一原生身份9223372067204761140，路径res://scenes/player/Player.tscn::Resource_y7r3h。

另四个构造默认serial11/12/119/120，各有独立出生/创建记录及自然Dispose(false) ENTER/RETURN；不是场景共享对象。场景共享配置本次在退出时Dispose(true)配对返回，不是其自然终结器覆盖。

**不能**从不同进程都叫serial1，将v146失败对象认定为本轮场景导出资源；hash不同，旧案缺来源链。也不能从本轮没有FATAL关闭历史资源问题。没有移除默认值、增加强引用、强制GC/drain或延长等待。

## 只读清理核对（不是根因结论）

其他测试存在把保存Player的Motor/Combat tuning直接加入Resource清理表再Dispose、未先Duplicate的路径：

- RangedWeaponRuntimeTests：GalleryLaserCarriedTrial加载CacheMode.Ignore，109–126加入；383失败清理、1558–1564 DisposeFixture。SteamMissileMovingTrial在495–510加入，831/835清理。
- BellBoltSentryRuntimeTests：341–360→593、640–659→780、900–916→1347的三个保存Player夹具。

共享HashSet收取两个别名，不证明对同一对象Dispose两次。这些路径**不在短流程的两个旧委托中**。Task5/GeneratedMainDeathOnly/TestSceneTeardown只释放level/audio，未找到PlayerTuning资源清理表或显式Dispose。限定生产消费者检索也未找到显式PlayerTuning释放；不覆盖任意反射/原生行为，不能排除生命周期问题。公有Dispose通常是disposing=True，旧FATAL是False，不能混同。

对照：BuildPlayer.cs:85/92及LandingImpactRuntimeTests.cs:394释放私有Duplicate，不是原导出对象。新的来源合同只显式Dispose自己的new资源；保存Player合同不Dispose借来的导出配置。

## 测试驱动与唯一复核

先原生RED9c4f（缺出生）、REDacc1（缺创建来源），再加最小钩子。15e6源生成器编译错误、80ec局部helper编译错误均无原生，不算行为RED。3bde在SceneTree._Initialize中过早检查Ready是夹具错误，不冒称生产故障；只给自有合同增加一帧，未改短流程。

唯一新复核C0/I2/M0，历史延期Minor不减：

1. I1旧断言按持有配置serial过滤，会漏掉提前创建后弃置的回退。改为独立统计整个role的创建次数，再关联持有对象。
2. I2缺coordinator回退实际执行。新控制通过真实保存Player/coordinator的可选依赖路径，分别验证缺依赖/正常依赖，检查真实timeline所持对象、role、身份和无多余回退。

只在诊断分支临时引入“提前创建未使用Motor回退”和“省略coordinator记录”反例，bd8c原生同时报告两个指定RED。恢复原表达式后bf32完整新旧合同GREEN，0/1427ms，七个新收据齐全，三个输出无ERROR/WARNING/FATAL。无二次复核或顺手处理Minor。

## 二十次尝试与最终普通九项

证据根 G:/knight/验证证据/october13-stable-claw/，独立目录均不覆盖旧失败。

| 短ID | 阶段 | 原生结果/意义 |
|---|---|---|
| 9c4f | 出生反例 | 1/536ms，正确RED |
| acc1 | 创建来源反例 | 1/499ms，正确RED |
| 15e6 | 编译 | 4错误/0警告，无原生 |
| 3bde | 夹具过早检查Ready | 1/593ms，错误保留 |
| 0f57 | 初始合同 | 0/1427ms，非最终epoch |
| 80ec | 控制编译 | 2错误/0警告，无原生 |
| 9f4e | 复核前完整合同 | 0/1433ms |
| 6d2b | 中间短流程 | 0/6702ms，终结器未覆盖 |
| bd8c | 两复核反例 | 1/688ms，双指定RED |
| bf32 | 最终完整合同 | 0/1427ms，GREEN |
| 3036 | 最终短流程 | 0/6730ms，仅诊断完成 |
| 5f5a | 普通防守站位 | 0/1149ms，PASS |
| 35c0 | 普通models | 0/387ms，PASS |
| 24ba | 普通keyboard | 0/11882ms，PASS |
| 2938 | 普通机关 | 0/27431ms，PASS |
| 779b | 普通完整Main | 1/118087ms，FAIL |
| 979a | 普通完整Explore | 1/137111ms，FAIL |
| 9f02 | 普通pre | 0/150159ms，PASS |
| 0896 | 普通post | 0/196545ms，PASS |
| 7022 | 普通诊断排除 | 0/398ms，PASS |

18次clean build/原生、2次早期build失败；原生12 exit0/6 exit1，严格总计12PASS/8FAIL，包含有意RED，不能算玩家成功率。

最终普通九项 **7PASS/2FAIL**：

- Main死亡/恢复请求1/完成0，physics6617，位置(4753.8955,3423.9336)→检查点(1680,3960)，严格完整路线无恢复/瞬移检查失败。未绕过恢复或改伤害。
- Explore在Grip1 physics7747、原300窗口第94帧HP1→0、恢复1/0，严格失败。未改跳力、加隐式二段跳或增加测试预算。
- 两案都有明确测试ERROR，stderr/external原件保留。本轮无原生FATAL，不关闭历史FATAL。
- pre三个输出无诊断。post原75执行/0退役/75归类＋owner重复8通过，退出drain/phase收据完整；保留4条精确限定WARNING（seamless观察者故障1、room-profile1、flow-registry2），不是忽略任意警告。
- 普通实际加载程序集确认lifetime/native-identity类型不存在，不只根据构建开关推断。

完整最终目录（均相对上述证据根）：

    resident-census-identity-contract-refcounted-identity-20261027-211409-928-bf323611faa0487b872b23cc3a74e8fa
    prologue-profile-lifetime-pair-refcounted-identity-20261027-211554-487-30360a0fa1b04da4a401fdec2f0f6ec8
    thruster-stationary-guard-20261027-211633-703-5f5a1e348a274c3c870c2cb3558d3dfd
    models-20261027-211708-013-35c013d9c3264bd2bda2ef8240c6de9b
    keyboard-20261027-211738-091-24bacd3bf8c54e01ae5da03237918a72
    recycling-traps-20261027-211817-562-29388f92d56049a28a20f08c731fbfe7
    thruster-route-main-diagnostic-20261027-211910-141-779b5a84bdc645d495af2d7867fea44d
    thruster-route-exploration-diagnostic-20261027-212139-404-979ac18d65c94e72b47962354fae110c
    prologue-pre-20261027-212426-699-9f02896276e149aa8d1ed2772deb2438
    prologue-post-20261027-212726-783-08960838f5264ed4951f3766ddf339bf
    thruster-profile-trace-normal-20261027-213114-913-70228d5396434eb88a7a54c37ebf9e7b

## 有界保管与当前普通构建

只读审计截止2026-10-27T13:34:01.6935237Z，旧v146十六案另列：

- 80/80源码/helper/user/continuation前后清单对相等；9380源码副本＋270helper副本匹配各自清单和选定路径集合。
- 18 fresh/after二进制清单对、108封存二进制一致；两build失败不伪补fresh/native证据。
- 38个记录的自有PID＋CreationTime身份结束，0timeout/0hash mismatch；不是对其他进程作空闲声明。
- 最终11案各1857路径/897C#与当前精确一致，独立枚举路径无差异；107相应epoch helper条目一致；九普通54个二进制条目与当前普通构建一致，诊断epoch另列。

    source manifest 814C2740E834749A63D9BF6FE00F3CC1B74F689AB8A855510F8010FF0126E1A3
    normal binary manifest 4602DFEE254D0CB81FA43B7C40A0BCDD89756A8ECD502AFFD7A90238BA22A04D
    canonical 155EB543ED41A38AF63FFDFBAE6D3D30602628AAB71990AF329ABE574E78DA4F
    Motor 9161427BF1548B0D19F0D850DC45D137A3B4A5215B6EF7C70010E93FF9B1C53B
    Combat 1F850A2F0A8FD4380C8918AAF34D224C6317FCF1880786BB644BE2EB8FCBDC7E
    Coordinator 103C1D2F9C3B22A0C7048EE9BDE4CCF0B8C9E946F89B62FA6505B3237079355B
    LifetimeTrace 3CF67E7FFAF65A1546B65527A3B8CBAABA416AC65E8E6C4A6CA20D12E728D3AF
    DisposeIdentification 511925184314ED4BBFA81D9E8DCB1B83E203EEBF2AC69A64A6FD0BB6829B18CA
    CensusRunner 2A3611E448ED2572CDB0857C1BD1490B9305B0048BAF190CB60C35DBF5F4DC95
    Knight.dll 5A1F365A90E8FB4B25446F122C8CCB4D3E9B685DA31E52311B489BE0339933ED
    Knight.pdb 96A9EE57EC6748D4C52CE6ABC2A1216DC41B95F4E0DF98A3105DF0C9CBEFF918
    Knight.deps.json 214598669CA8B8883DAFE876E7EA3DE11E15D9828EB3FA4294E828CF24F23964
    Knight.runtimeconfig.json C2BD1DEFE29583AB9DB5A1110B36B3537214BCB56321F076ABFFBB9956852722
    GodotSharp.dll 7B1D06575DB78E4EA4353186BE5ADD207E43617B836DB24D65EC4088E6FAD18C
    GodotSharpEditor.dll EADA28133A15ED75B9E892EBF1C73C026AF6034262BA71838A204C656FD3AD08
    HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
    index 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C

旧两个委托、TestSceneTeardown、Main/Basin、身份桥、pair literal helper均未改。分支仍codex/task6-certified-final-20260902T0555，无提交/push/发行标签等Git写入。

## 执行取舍（按ledger顺序）

1. 沿用户委托无窗口执行，不重复问技术设计；只诊断，不猜改引擎/default/保活，不按技能默认开worktree/Git/删除。代价：本轮不是根因修复。
2. 源生成器不能正确复制嵌入导出初始化表达式内的条件指令，改为完整属性声明分支，普通声明仍原表达式，不编辑生成物。代价：需普通实际程序集排除证明。
3. 仅自有合同等待ProcessFrame后检查真实Ready，原短流程不加帧。代价：合同不能替代原流程时序证明。
4. 不跨运行合并serial1，也不从已回收默认对象猜历史失败来源。代价：旧失败来源/共同根因仍未知。
5. 复核后最终epoch改变，完整合同GREEN后再运行一次最终短流程及原普通九项。代价：多一次有界验证，不反复筛绿。
6. 复核未判定边界逐项保留：普通重建/排除/九项必须实测（已执行）；原十一项/路线/视觉真人/媒体保管各自独立；原生根因与稳定性未证；跨运行身份未证；删默认/释放政策无依据；历史问题/脏树不重认证；旧classifier由root实际AST重跑而非冒称reviewer独立运行。代价：未证事项继续OPEN。无新增Minor，历史延期不减。

## 留档与下一步

无显示窗口、系统输入、付费生成、新媒体、自动化、真实存档语义读取或删除。手工媒体Check于2026-10-27T13:32:58.4657667Z：1431609bytes/16files，baseline3508/growth1428101，Notify=false，不重置。G盘当时约154.05GiB空闲，只代表当时。

继续两条完整路线混战/抓点失败和资源生命周期因果，不降低敌人数值或伪造稳定通过。新来源记录可供下次实际失败关联；其他夹具清理是独立核验候选，不能归为短流程根因。正式美术、连续动作、UI/音频/相机、目标硬件与真人试玩及交付仍待独立验收；用户NOTSEEN保留。
