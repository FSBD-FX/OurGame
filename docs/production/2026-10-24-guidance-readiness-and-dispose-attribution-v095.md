# v095 · 教学恢复前提与资源释放归属

2026-10-24。本轮为局部诊断推进，不是整关完成或试玩交付。唯一工程仍为 `G:/knight/Knight`，完整十一项目标保持 ACTIVE。用户最终反馈“等一下，是没看到”有效：显示未确认，不追认教学遮挡或画面认可，不沿用旧授权自动开窗。

## 事实、推理与风险

事实：原教学恢复消费者只等三帧，实际仍为 Intro、居民准备未结束；原生断言复现后，改为等待真实 Playing 且准备完成，同一个原注册案例通过。REF 完整后半程另取得明确的 `ResidentSubtypeProfile.Dispose(false)` 崩溃栈，但资源来源和失效原因仍未知。普通 pre 有句柄错误，普通 post 超时，最终普通构建恢复。

推理：消费者的准备前提确有错误，可以修正；它与资源崩溃不能合并为一个原因。明确释放类型比匿名终结器栈更窄，但不能据类型、相邻日志或可复用的原生地址判定哪个场景资源损坏。

风险：诊断字段和日志会改变分配及执行时序，REF 的安静或失败都不直接证明普通构建因果；专项通过也不代表正常主线、探索、正式视听或真人验收完成。本轮不改生产物理、资源缓存/保活、人口、AI、难度或等待上限。

## 四文件变更与真实反例

- `tests/integration/PrologueGuidanceRuntimeTests.cs`：只在原 `MountedGuidanceRendersAndClearsWithoutHidingObjective` 的自有 try/finally 内，复用现有 `WaitForPlaying`，再输出 State/Pending/Failure/Rejected 标量。没有强制设 Playing、重新 StartNew 或绕过居民准备；原四次恢复上限、三帧恢复 settle、键位重绑定/还原、双语、隐藏与目标保持断言不变。其他 `LoadFixture` 消费者未改。
- `tests/diagnostics/Task14DisposeIdentification.cs`：仅 REF 符号内给 `MobilityModuleDefinition` 增加托管 serial 和 ENTER/base.Dispose 一次/RETURN。普通构建不含该字段或 override；无新原生身份查询、GC、缓存或包装器保活。未登记的原生身份保持 unknown。
- `tests/diagnostics/RunResidentCensusIdentityContract.cs`：真实创建并自有释放一个 module，验证同 hash/serial/call 的一对记录、构造 serial 分配和 wrapper pointer 清零。原三资源合同独立保留；显式 disposing=True 不冒充终结器行为证明。
- 规范 owner `verify-claw-knight-v001.ps1`：将 module 源纳入该诊断输入副本，并要求恰好一条新覆盖收据。原错误、警告、退出、FATAL、时限与合同门保持；没有新增测试阶段或放宽原75项。

教学真实 RED：151215 案在首轮恢复前为 `Intro/Pending=true`，Failure/Rejected 均空，断言失败。最小消费者修正后，151549 为 `Playing/Pending=false`，原完整案例通过。最终普通 post 中 index18 也真实进入并返回，保留双语/键位与恢复断言，不以专项替代整套。

module 覆盖真实 RED：152552 案原合同完成后，新 module 配对断言失败。补 REF override 后，152923 案 call11/serial22 同 hash，ENTER 非零 pointer、RETURN 零 pointer，native_identity=unknown；这是观察覆盖通过，不是资源修复。

## 释放覆盖与实际崩溃

只读源码及当前程序集核对：19种项目自定义 Resource 原为18种 REF 释放覆盖，漏项为早期 Player 路径已可加载的 MobilityModuleDefinition。本轮补齐到19种。另有2种编译内非 Resource 自定义 RefCounted（ExpectedWarningLogger、NativeErrorObserver），均已覆盖；后者未发现普通 post 前缀可达。条件 RANGED_SPAWN_IDENTITY 的第三种 Logger 未编入普通程序集，不把它算进上述2种。两个 managed finalizer witness 直接继承 System.Object，不走 native baseref。这不是引擎全部内置资源/句柄覆盖证明。

实际 REF post153524：1050 ENTER、1049 RETURN，所有已配对行的 type/hash/serial/thread/disposing 相符；无 cap 标记。唯一未返回为 call1050：`ResidentSubtypeProfile`、serial846、hash37465572、thread1、raw pointer `0x0000025A86FDA4E0`。紧随的 FATAL 栈明确包含该类 override 的 base.Dispose 行277和 GodotObject.Finalize，故不只是日志邻近猜测。

该案尚未进入 legacy registry，边界行0，不是停在 index18。serial846/hash37465572 没有原 allocation、leaf、slot 或 native ID 记录；四条 scrap-scatter 赋值 hash 均不同。相同 raw pointer 此前被另外9个对象使用且均正常配对，不能跨地址复用拼所有权。另有56个该类型 finalizer 正常返回，反驳“这个类一释放就坏”。Mobility 源码与封存 DLL 确含 override，但该案 disposal 行0；不证明未分配，更不证明它是或不是上游原因。

本地 GodotSharp/CoreLib 的 IL 检查把 `FromIntPtr → SwapGCHandleForType` 的这一抛错入口收窄为传入旧 handle 为0，不能据此命名资源来源或最初释放者。官方已有相似报告，但外部版本与案例不是本地根因：[Godot #112067](https://github.com/godotengine/godot/issues/112067)、[Godot #112943](https://github.com/godotengine/godot/issues/112943)。[引擎所宣告提交的 C# native 源](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/csharp_script.cpp)说明了释放断言及弱/强 handle 转换分支；没有把远端文本与本地二进制逐指令对应，不据此改引擎。agent-reach 的 Exa 元数据调用失败、gh 不可用后才用官方网页只读后备；无安装、登录或外部写入。

下一最小反证：在原实例化/赋值路径登记真实 Profile serial/hash 与既有 slot/leaf 字符串，对照同路径正常释放者；不额外 Load/GetState/身份查询/GC/保活。该下一步尚未实施，不先回填配置或更换缓存策略。

## 全部八次原生运行

证据根为 `G:/knight/验证证据/october13-stable-claw`；时间短名唯一对应以下完整阶段目录。全部为 headless/human=false，8次构建 exit0、错误/警告/超时均0。root 是唯一构建/原生拥有者，每个拥有者存活时所有源、帮助器和文档均冻结。

| 时间/阶段 | 原生退出/毫秒 | 严格结果 |
|---|---:|---|
| 151215 thruster-teaching-ui | 1/4269 | FAIL，真实 Playing 前提 RED，1条目标断言 ERROR。 |
| 151549 thruster-teaching-ui | 0/4706 | PASS，原完整挂载教学恢复案例。 |
| 152552 resident-census-identity-contract REF | 1/622 | FAIL，真实 module 释放覆盖 RED，1条目标断言 ERROR。 |
| 152923 resident-census-identity-contract REF | 0/935 | 诊断合同 PASS，resource_fix=false。 |
| 153524 prologue-post REF | -1073741795/173651 | FAIL，非超时；6 Handle＋1 FATAL，2条原有警告，registry尚未开始。 |
| 153903 ordinary prologue-pre | 0/166843 | FAIL；虽有 phaseOK，仍6条 Handle ERROR，不能归零或豁免。 |
| 154225 ordinary prologue-post | -1/180097 | TIMEOUT，原cap180000；ERROR0但不等完成，31 enter/30 return，最后index30 HeadlessAutomationDoesNotCreateHumanEvidence进入未返回。 |
| 154606 ordinary models | 0/533 | PASS，普通构建恢复，不替代原75或正常双路。 |

合计3严格PASS/5FAIL；原始主要 ERROR15＝12 Handle＋2目标断言＋1 FATAL，ShapeRID0。external 是镜像，不重复合计。两次 post 的2条 WARNING 均为已有故意 observer fault 与 RoomPresentationProfile 错误根类型。REF owner 首先报告 scope 不完整不能遮蔽真实原生 FATAL；批处理捕获失败后总 exit0也不代表八案全通过。

最终同源/帮助器冻结的普通 pre/post 均严格失败。普通 post index18 已真实完成准备、恢复、键位和双语断言，原18处额外 Title→Recovering 警告未复现；不泛化为所有准备/恢复路径已修。未重跑策略未变的 Main/Explore，既有交叉火力及E0死亡、完整双路/相机/正式AV/真人硬件/发行资格仍开放。

最终纯合同实际107/96/26/33/11/24均PASS。11与24负例当前在构建/原生前拒绝；沿用v094“整个owner负例在守卫回归时可能有副作用”的Minor，只在root原生全部终结后运行，不将当前安静称为永久纯函数保证。

## 唯一审查及root逐项裁决

一次独立只读审查，只比较本轮四文件与封存基线，C0/I0/Minor1；未做第二次审查，无Critical/Important修正回合。Minor：等待帮助器若抛错，后面的新 JSON 不会输出 LastRejectedTransition；原异常仍包含 state/pending/failure/未准备居民，不会误通过。延期补失败路径标量，代价是诊断细节较少，不改等待或准入。继承的清理、时序、负例副作用等开放事项未被覆盖。

| 审查保留项 | root裁决、理由与代价 |
|---|---|
| 1 实际W与旧preview | 不关闭；本轮仅InputMap图标重绑定，不是硬件/读取边界/排名证明，后续路线证据仍需补。 |
| 2 实体遮挡与receiver资格 | 不关闭；没有新的单次原生first-hit或施放授权证据，保留原LOS规则而非二次解析通行。 |
| 3 提交前拒绝 | 本消费者现在等真实准备；未新证所有提交前拒绝，不把新等待替代原拒绝合同，代价为完整生命周期仍待测。 |
| 4 callback/deferred退出 | 未触及；真实actor/parent/pose/sequence跨执行边界仍需验证，不以flag替代执行。 |
| 5 loader/清理异常保全 | 继承LoadFixture catch或caller finally的清理异常可替换主异常，未修也未注入；新等待在自有try内不是广义LoadLevel闭合，代价为失败诊断风险仍在。 |
| 6 finalizer/崩溃所有权 | module正控只有显式释放；root另审实际REF得到ResidentProfile类/serial，但来源/因果未知，不将正控或地址等同native寿命。 |
| 7 完整类型覆盖 | 审查者未独立重建19类；由另一只读IL/源码审计得到19 Resource＋2编译内Logger边界，不扩成所有Godot资源或handle已安全。 |
| 8 不利观察路径 | 本轮未逐一注入module sink/cap/base异常，现有logger容错检查不等全故障执行；显式释放失败后的清理重试也未获实验保证，保留风险而不额外扰动当前冻结。 |
| 9 全回归/普通构建 | 采用上表实际失败和末普通models恢复；40对保管另核，不以focused替代同冻结完整回归。 |
| 10 原75/Main/Explore/物理人口/视听真人与十一项 | 全部维持原要求和ACTIVE，显示NOTSEEN；没有新资产/玩法内容或整关完成比例，后续交付仍需原完整验证。 |

## 最终冻结、保管与留档

最终普通案 `models-20261024-154606-713-9c5e7a47605347588769d96d11e78440`。独立只读审计8案40对清单逐字节一致，48份 OWN 二进制均匹配各自清单，缺失/未配对/不符/额外均0；不跨案推断DLL相同。全案源1776/C#892；帮助器数依次9/9/12/12/12/10/9/9。

最终live完整枚举1480 runtime＋296 imported＝1776/C#892，新增/缺失/哈希漂移0；9项帮助器全匹配，实际6二进制匹配6/额外0。user清单104个FILE只做保管比较，不读取真实存档语义或称存档行为验证。

- source manifest SHA256：89649F008F35A997EE9C2964FB23F391B1171A3D240B4563FD53A0DE4CC50362
- helper manifest SHA256：DF27DA2A35756FD25E130A28F781C4619E31ECAEC697027A89421BFA537325BF
- binary manifest SHA256：F272E69143999B912BFB055BB925C0FFF7720C01EC87286654DC1E67AC3DAEA0
- Knight.dll SHA256：201A3EEC282C200997093FABF0B475329181C5BC86CA96D40001D950B44A8A1C
- branch仍codex/task6-certified-final-20260902T0555；HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6；index仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。无Git写入。
- diff --check exit0，169条既有LF→CRLF提示、其他0。
- 媒体单次检查2026-10-24T07:48:38.9244386Z：当前860284 bytes、基线3508、增长856776、Notify=false。没有新过程图片/实录，不建立自动化或独立提醒。

所有root原生拥有者均已终结后才更新本文及状态文档。未新增正式美术/音频/地图或玩法内容，无窗口/OS输入/付费/自动化/打包/提交推送/删除。原十一项、用户显示/试玩、正式视听、目标硬件与版本交付未完成，不执行task-done/finish或工作区清理。
