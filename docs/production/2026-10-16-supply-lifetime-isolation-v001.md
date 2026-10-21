# 补给生命周期原生崩溃隔离 v001

2026-10-16，Asia/Shanghai。完整目标保持ACTIVE。本轮是诊断与验证入口补齐，不是资源修复、地图／美术提升或第一关交付。正式新增仍0／50，24正式族系场景仍未实现。

## 事实

上一轮真实pre211007在补给测试附近发生 `gchandle.is_released()` 原生FATAL。源码核对发现旧 `RunLifetimeBoundary` 只跑“治疗时NewGame重入”，没有覆盖完整补给Run后面的Title、死亡恢复、重新挂载Continue及Status替换；旧隔离通过不能证明这些边界正常。

本轮增加测试专用 `basin-supply-lifetime`，执行**原完整BasinSupplyRuntimeTests.Run**：NewGame重入→Title重入→CheckSupply(3,2,true)→CheckSupply(1,1,false)。保留原资源、输入、物理等待、teardown和自然继续，只在此phase启用具名scalar日志。记录四项CASE_ENTER／CASE_RETURN、ROOT_CREATED／OPEN_RETURN、五处释放入口／返回、Continue和Status；释放后只记录此前保存的root ID，不访问失效节点。没有新增C#源文件，库存仍1670。

### 隔离原生FATAL复现

`basin-supply-lifetime-20261016-214459-305-215f0eaace7e4f1398fc5a325662e50b`：10484ms／55000ms，exit−1073741795、非超时。无需Camera／Clearance／Claw前缀就复现同一 `mono_object_disposed_baseref:1788 → Dispose(bool) → Finalize → GC.RunFinalizers`，stderr另有0xC000001D。

- NewGame root46690993664与Title root101837702656各有RELEASE_RETURN和CASE_RETURN。
- 第三项CheckSupply(true)已进入，记录root150927836672创建，没有第三个OPEN_RETURN；完整Run／phase成功／退出drain均未发生。
- stdout以第三个ROOT_CREATED结束；external先写FATAL、随后写同一ROOT_CREATED。跨sink／线程不能建立精确总顺序，不能说第三个root就是出错对象或根据最后一行命名资源。

这证明前缀不是复现所必需，不证明治疗、Title、重新挂载或某个具体资源已被证明是根因。

### 一次有界类型记录：仍失败，不是修复

为同一已聚焦phase开放现有 `REFCOUNTED_FATAL_IDENTITY` 编译诊断，仍保留原base.Dispose调用、资源策略和55秒cap，不运行资源属性遍历／native ID collector。探针只记托管类型、hash、线程及已有NativeInstance字段值；这些会扰动观察时序，不能当作普通构建或资源修复。

新纯argv入口合同先失败，再最小开放：`.superpowers/sdd/2026-10-16-resident-population/supply-identity-admission-20261016-215048-359/` 保留red／green／legacy stdout和stderr。RED在4个正控后拒绝新phase，exit1；修改后新增10项与原22项均exit0／stderr空，原Main／Explore与拒绝边界保持。没有运行owner顶层或引擎来测试argv函数。

诊断case `basin-supply-lifetime-refcounted-identity-20261016-215228-891-345214b0a49646b0944974c779824f64`：engine exit0／10734ms，但stderr和external各镜像同8条 `Handle is not initialized` ERROR，严格FAIL。没有FATAL、WARNING或RID退出header，不能因流程跑完和STABLE_CLAW_OK就判绿。

- 四case、五次root创建／OPEN_RETURN、五次释放均返回，原完整Run与退出drain也返回。
- 首个Handle异常实际栈是ResourceLoader.Load→GD.Load→CreateSampleFixture:426→OpenSample:393→CheckSupply:190；**不是已证实PackedScene.Instantiate报错**。另外7条在Node.AddChild→OpenSample:397→同一Continue调用。
- stderr151 ENTER／151 RETURN，call1…151逐一配对；缺失／重复／字段冲突／未闭合／cap marker均0。False129对、True22对，7次嵌套最大深度2，没有配对冲突；这些本身不是double-free证据。
- 记录8类：BellBackLaborerTuning36、BossTuning1、EnemyArchetypeTuning12、PlayerEnergyTuning6、PlayerTuning11、PresentationProfile44、MinimapTerrainCatalog1、PrologueSectorDefinition40。所有native_id均unknown，地址复用不能冒充native身份。
- 没有得到“某一对象ENTER后未RETURN”的指认，built-in Resource也不在此类型覆盖内。不能再据匿名计数猜Dispose对象。

### 明确的所有权事实，不是嫌疑对象定罪

只读核对：FogCanvas自建ShaderMaterial、Image及两种ImageTexture；EnergyHeatVisual自建4个StyleBoxFlat，在Predelete释放。保存Shader／PackedScene／Profile／碰撞Shape为借用，不新增Dispose。此sample关闭GuidanceEnabled和ProcessMode，sample ApplySettings再次关闭；guidance WAV创建入口未启用，Shutdown的条件Dispose不能算WAV已创建／释放。普通音频仍启用，音频节点不等于WAV资源。AudioDrain标记早于root.QueueFree，不能替代整树释放返回证据。

## 普通构建恢复与回归

以下目录全部位于 `E:/OurGame/Knight/.evidence/october13-stable-claw/`。

| case目录 | engine耗时ms | 编译模式 | 严格结果 |
| --- | ---: | --- | --- |
| basin-supply-lifetime-20261016-214459-305-215f0eaace7e4f1398fc5a325662e50b | 10484 | 普通 | 原生FATAL，FAIL |
| basin-supply-lifetime-refcounted-identity-20261016-215228-891-345214b0a49646b0944974c779824f64 | 10734 | 类型记录诊断 | 8 Handle ERROR，FAIL，非FATAL |
| prologue-pre-20261016-215855-243-9ba5a7ed931042a3b699a84608482143 | 88307 | 普通 | Upper错台＋2 profile警告，FAIL |
| prologue-post-20261016-220100-292-82db256568674fe3b9964d9f482d0ba8 | 8116 | 普通 | BellChain落地／Recovery，FAIL |
| keyboard-20261016-220136-683-2730ae2f89764f0ea5d3d0633068c032 | 9583 | 普通 | GREEN |
| actions-20261016-220216-957-c074f55974524eb4b9ebddc5b85da419 | 6978 | 普通 | GREEN |

合计2严格GREEN／4严格FAIL；不是六次同构建。五个普通案二进制清单同SHA256 `7D947D4018AC5EE6BC1DE43ABA3B3B6DBD5AF3DAE568A0042FADBBB5A55C550C`，诊断案为 `5C2DFF40323BF3B2660AB3B6D599DDAACF40B48E36C175B1D00F903744873D12`。普通pre此次已越过补给，失败在旧Upper lower_to_mid_swing sample0.5：149／150步、final(264.99634,160.6107)、landingZoneCrossings0；此run无Handle／FATAL，但不能据单次缺席宣称修复。post仍final(540.0704,1196.0139)未落稳，六祖先ILevelRecovery无法解析。

普通四回归均fresh Rebuild不带诊断symbol。最终另以PE metadata只读检查Knight.dll，不加载游戏程序集／执行游戏代码，确认Task14DisposeProbe、Task14NativeResourceIdentity类型0，真实Supply测试类型在。DLL SHA256 `1B030F338C924820FE7737254A7ECC88ACA25281F670EEEF3E037ED39E475E4A`；证明原始normal-metadata.stdout.log／stderr.log在上述argv合同目录。诊断构建未留作默认版本。

六次build均exit0、0编译警告／错误、stderr空、cap180000ms；普通七参数Rebuild，诊断仅多指定symbol。专项cap55000ms、完整pre/post180000ms；全部无超时／guards失败，headless、非fixed／movie／verbose、无ExpectedFailure，human／naturalRoute／wholeGoalAccepted=false。

共同源码／资源库存清单SHA256 `39011993ACAFE066EA001B4C51C36150E0E91C37AAB2EDCBEFA00E6239AF685D`；source1670、真实保护109、continuation1、binary6，1670不是C#文件数量。source／helper／user／continuation／binary共30对before-after相同。helper数量分别9／12／10／9／9／9，各自前后相同；owner在首案后增加诊断准入，不称六案helper字节全部相同。真实userdata仅哈希、不读写语义。

root读取新原生stdout、错误栈及元数据／合同／30对守护；所有三raw全文机械扫描，长类型记录／重复轨迹只作有界展示、不是逐值人工审查。独立只读证据审计覆盖六案，确认配对／诊断／保管链；不是code-review或whole-plan批准。全部消费者终态／owned0后才更新文档。

| 本轮改变文件 | SHA256 |
| --- | --- |
| tests/integration/BasinSupplyRuntimeTests.cs | 018D1D99A940C191558A4436FDA39B8FE3878146454805FB3B0EE3799935F307 |
| tests/RunStableClawPullTests.cs | 08ACE3DDC1E24E12FFB4EC3D393B6B9FC8012EE4178A9222BD8AA682094880DF |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 8208373E87203FB3772EA8F1D421EBDC82090FD2DAE844F4DFD5D5343FBF9719 |
| .superpowers/sdd/2026-10-16-resident-population/test-supply-lifetime-identity-admission.ps1 | 9B44EB3AB7F9A5B3E61B7C0F56690145F59165AD4B4D87B24FF78B923A5DCAC2 |

## 推理

隔离已把下一行动从全pre的无差别重跑缩到多次场景生命周期；前缀非必需已获真实失败证明。类型记录没有找到失配释放对象，且改变了错误表现，不能把它作为修复或清洁资源基线。当前不存在可据此安全实施的具体资源Dispose补丁。

下一最小有区分力的对照：保留原OpenSample与原teardown，连续5次挂载／释放sample（完整Supply同为5棵root），**不请求伤害／治疗或额外重入回调**。若无治疗时仍出现相同FATAL或Handle错误，可证明治疗回调不是出现对应故障的必要条件；其他失败不支持这个结论。若不失败，只说明该对照未复现，分配量／时序不同，不能反推治疗就是根因。此新对照尚未实现／运行，不把方案当进展或验收；不再重复同一151对类型记录或空名册。

## 风险与完整范围

- 原生FATAL、Handle、Shape RID、旧Upper／BellChain均OPEN；不强制GC、KeepAlive、改cache、Dispose借用资源、增加退出等待、压制日志、豁免错误或擅自升级工具链。
- 没有新增生产玩法、敌人／地图／美术／声音或正式出生。前轮Bone Heavy／Ghost Wanderer机制保留；剩余Bone Rust／Skitter／Moth、Ghost Moth Flyer／Diver／Sentry六族系承载、24正式场景、ALL50活动／玩家接近／world-lifetime manager仍必做。Bone4＋Ghost4＝8子型、人口骨骸10／幽灵10保持，不恢复错误的“剩余7／Ghost三远程”摘要。
- 正式Placement全null／新增0；旧26＋1Boss、目标76普通＋1Boss、14区域／5族／6职能与ECHO1身份／友方身体0不变。Task1最终50、完整Task2、Task3–4未完成，不打勾或作最终whole-plan审查。
- 9残片／6入Boss、纯键盘Q/J/K/L、墙顶机械爪、检查点／旧档／回返／支撑出口、正常Main＋Explore／实际呈现60FPS／最终视听／真人／审阅归档短录像仍须全范围验证，目标不缩减。
- 继续无窗口、不抢用户键鼠；无新媒体／付费生成／Git写入／版本固定／上传／自动化／安装／删除，素材表无新增，旧过程图稿、录像和报告保留。

