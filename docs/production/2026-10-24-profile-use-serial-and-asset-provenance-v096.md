# v096 · 原配置使用编号与获批素材来源

2026-10-24。工程仍为 `G:/knight/Knight`，完整十一项目标 ACTIVE。本轮新增的是诊断关联能力及来源核验，没有新增生产玩法、美术或音频，没有修复资源崩溃，也不是试玩交付。用户最终“没有看到窗口”有效；没有开窗、OS键鼠输入或画面认可。

## 事实、推理、风险

事实：真实原工厂反例证明旧记录漏掉非 scrap-scatter 居民。补齐 REF 观察后，同一个真实配置的两个叶场景/slot 使用及第三个 null 控制均正确记录。完整 REF 后半程由 serial＋hash 首次把失败 finalizer 连到一条原始使用：入口低顶桥 `.03` 的 scrap-scatter。普通 pre 仍有 Handle ERROR、普通 post 仍 FATAL，最终普通构建已恢复。

推理：可以围绕已命名配置的实际生命周期缩小复现；不能由“某次使用”断言独占/native owner，也不能将后一次 null 或相邻异常当作相同故障的原因。另有同叶正常释放对照，不能简单归咎于这个场景或这个类型。

风险：REF 字段和同步日志会扰动分配/时序；不完整 trace 不能获得全套资格。没有通过原完整回归、正常 Main/Explore、正式视听、相机、硬件/真人和发行门，所有原要求保留。没有用更长时限、GC、缓存/保活、回填配置或减少敌人求绿。

## 变更及先失败后修正

五文件差异相对 v095 封存基线，不是累积 dirty HEAD：

- `ResidentSceneBinding.cs`：仅 REF 内扩展原 setter 标量快照，加入 `LastManagedSerial`；原赋值仍最先执行。将既有 operation 覆盖所有 subtype 工厂调用，复用原短路门已经求值的 Profile，不再读一次 getter。普通属性、准入、ConfigureActor 和 catch/free 均未改变。
- `Task14NativeResourceIdentity.cs`：原 assignment 行加入 `FieldManagedSerial` 和 `CaptureAfterSource`；null/未求值记0，不借用前一条身份。只对原来 eligible 的 scrap-scatter null 累计 after-source 需求。
- 原工厂拒绝分支仍只有 scrap-scatter 调用既有拒绝后 GetState；没有因扩大 operation 而对其他 subtype 增加原生来源读取。
- `RunResidentCensusIdentityContract.cs`：同一自有 Profile/真实 PackedScene、两种非 scrap 叶和 slot，保留原 placeholder 拒绝；第三次故意清空 Profile，原 null 拒绝也保留。验证真实使用 serial、类型/hash、空值0和没有新增 after-source。原资源/赋值/来源/失败sink/cap/Presentation/Mobility全部合同保留。
- 规范 owner 及其纯合同：当前协议必须是 all-subtype、恰在24个已著录叶路径中、entry/row path+slot一致、serial为相应正整数或0，且只有原 scrap 允许 after-source。512上限、fault/loss/不完整否决和原75/退出/日志门不变；没有兼容旧行来冒称新协议已完整。

最初两次原生并非目标 RED：自有 PackedScene 保存成功却仍无 ResourcePath，被原路径门提前拒绝。补充原异常/实际路径后证实 source为空。仅在新建、已保存的 case-owned 私有路径上显式 TakeOverPath，保持同一 PackedScene/Profile，没有改共享/著录资源路径或增加游戏来源 Load。第三次才到达实际 `RESIDENT_SERIAL_SCOPE_RED`；随后最小观察变更使同一合同原生 GREEN。

GREEN162413：两个非 null 使用同 serial21/hash27776831，binding23/24、不同 leaf/slot；第三个 null 为 serial/hash0、binding25。三者 CaptureAfterSource=false，after-source0。该正控证明“多次使用而非独占owner”的记录能力，不冒称复现或修复了真实原生资源故障。

纯 owner 消费反例也实际先 FAIL：all-scope/serial 正向行被旧协议拒绝；修正后145例PASS（原107预期控制＋14 serial/scope＋24实际声明路径）。所有原 null、未求值、字段不同、额外来源、截断、fault和cap负例均保留。

## 本次实际失败配置的关联

证据：`G:/knight/验证证据/october13-stable-claw/prologue-post-refcounted-identity-20261024-162742-777-ab62c16543c1444285c572bdfa890a69`。

slot 为 `canyon.v2.resident.low-ceiling-bridge.03`，叶为 `res://scenes/enemies/residents/v001/scrap-scatter.tscn`：

| 原操作 | 同一 slot/叶的不同观察 | 后续释放记录 |
|---|---|---|
| 254 | serial1738/hash37566995，原门非 null | call1766 成对返回，近处正常对照。 |
| 272 | binding1809；setter与原门均为 serial1808/hash12677791，Calls1/NullCalls0/Faults0 | call1837 在 Dispose(false) 未返回。 |
| 292 | 新 binding1881；setter一次且传入null，field serial/hash0 | 不能把这个null对象关联成1808。 |

原使用在 stderr4746，未返回在4913；FATAL栈明确命中 `ResidentSubtypeProfile.Dispose(bool):277` 的 base.Dispose。全案 assignment292 ENTER/292完整锚定JSON，operation唯一；缺行、上下文、setter/field身份错配及fault均0，未到512 cap，但没有 TRACE_END，所以不是完整 trace 资格。

Dispose1837 ENTER/1836 RETURN，仅call1837未返回；其他配对的type/hash/serial/thread/disposing全部一致。ResidentProfile153/152，Mobility0/0。该slot/叶有9条使用：此前7次非null涉及5个wrapper均正常返回；serial1395曾用于operations182/206/230，最后一个正常call1702，证明不能从多次使用中选一条当唯一owner。

operation292 的 after-source JSON 被拼在 `Fatal error.` 后面，不是独立锚定合格行；它也只是后续第二次桥接。没有把混合行规范化为成功收据，没有据此判断原Variant/原生ID/GCHandle释放原因，亦不证明292的null由1808释放造成。

下一资源调查应比较这个真实保存叶、原配置包装器以及正常对照在 Load/Instantiate/退场间的生命周期，不重复按地址/邻行猜测。现有 `RunDetachedEnemyResourceRepro` 只覆盖9个旧carrier路径，不覆盖本次24子型的保存Profile叶；旧432离树安静不能给这一对象续签。新的精确反例尚未实施。

## 全部运行结果

共同证据根 `G:/knight/验证证据/october13-stable-claw`，时间短名对应唯一阶段目录。全部headless/无超时，8次构建exit0、错误/警告0。root为唯一构建/原生拥有者，每个拥有者存活时源、帮助器和文档全部冻结。

| 时间/阶段 | 原生退出/毫秒 | 严格结果 |
|---|---:|---|
| 161012 REF identity contract | 1/750 | setup FAIL，控制场景未过原路径门，不是目标RED。 |
| 161411 REF identity contract | 1/633 | setup FAIL，实际source为空；增补事实，未先改生产。 |
| 161855 REF identity contract | 1/636 | 实际 serial-scope RED；原placeholder/null拒绝已成立。 |
| 162413 REF identity contract | 0/817 | 诊断GREEN，真实3次工厂；resource_fix/native_owner_proved均false。 |
| 162742 REF prologue-post | -1073741795/177232 | 非超时FATAL1，26 enter/25 return，最后index25 RealCriticalPathRetargetsThroughShortcutAndBossStates未返。 |
| 163115 ordinary prologue-pre | 0/167758 | strict FAIL，Handle ERROR1；phaseOK/drain不能豁免。 |
| 163438 ordinary prologue-post | -1073741795/176822 | 非超时FATAL1，30 enter/29 return，最后index29 PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit未返。 |
| 163809 ordinary models | 0/543 | strict PASS，普通构建恢复。 |

合计2 strictPASS/6FAIL；原生主要ERROR6＝setup断言2＋目标断言1＋FATAL2＋Handle1，ShapeRID0。WARN4为两post各自原observer/room-profile两条，限定回执在；external镜像不重复合计。两个post均无最终phaseOK/drain；外层首先报告scope不完整，不得遮蔽原生FATAL。总批处理捕获各案失败后exit0不等于八案全过。

最终同源/帮助器冻结的普通pre/post均FAIL。未改变也未重复跑Main/Explore旧策略；交叉火力/E0死亡、完整原75/双路、相机及正式视听/真人性能/发行均OPEN。最终纯合同145/96/26/33/11/24全部实际PASS；负例只在所有原生拥有者终结后运行，继承的“整个owner负例在守卫回归时可能产生副作用”限制仍保留。

## 正式素材的独立只读核验

正常 `Player.tscn` 使用十层 `PlayerFormalVisual`。当前只有Lamp加载独立获批PNG；Body/Mantle/Weapon等仍程序绘制，构建器的player-layers.png是填矩形的占位，不是获批部件。

早期获批body-001静态合成图仍在 `docs/art/player/reference/thread-spool-bearer-body.png`，SHA256 `0358BFEA48D6FF79271A5B14AE03B0396361748E60EB2D9D36EF6A696D7B1B44` 与生成记录一致；含披风/武器，不能直接装到单Body层造成重复。

迁移保留源 `G:/knight/美术源文件/迁移留存/2026-10-24_complete-layered-001_未发布` 中35/35层文件与当前阶段manifest一致，但身体/胸背/披风/灯丝四阶段现存评审图均与review-state批准图哈希不同。旧final-manifest另有16层不符。阶段文件自洽、同名或approved状态都不能跨版本证明当前层就是当时批准的字节；本轮未找回这四阶段的完整批准链，也不据此宣称它们必然未批准或直接覆盖为被拒稿。

武器握持阶段例外：当前三张评审图逐一匹配批准哈希，三层又匹配该阶段manifest。均1402×1122 RGBA，位于该保留源的layers/：

| 文件 | SHA256 |
|---|---|
| half-bell-cleaver.png | 06E657BB4E3831473688E5CFE7B6BB4FF2FD2A80D77C4EE17514D4B2325C325A |
| left-grip-overlay.png | 058DB41E707619C25D1416DD5EDA4BD6BC00F8407DE2EBDF18B373D8FA7A6EBE |
| right-grip-overlay.png | AA83FADEF3D3DE26B3D004D7353D959FB4ED26CAE10C76CCC68A807ADAC37BD4 |

母版朝右/基线1080，左右握持坐标548,798与835,798不是运行时pivot。现Weapon节点(14,7)、z9，由PlayerFormalLayer.DrawWeapon画线，PlayerFormalVisual.ApplyPose施加动作变换。占位atlas的[8,136,96,112]/pivot[18,58]没有证明适用于此母版。

因此后续可优先做“获批半钟刀单件→现有Weapon节点”的真实运行时接入；必须实际验证alpha裁切、轴点、缩放、动作/翻转与可读性。不是用静态预览替代游戏资产，不顺手混入批准链缺失的身体/手部或两遮挡片，不恢复被否决A2/dressedMaster，也不称组合或动作已获批。此次仅来源/代码核验，未复制进assets、未改绘制、未做视觉验收或付费生成。

## 唯一审查与root裁决

一次fresh只读五文件审查C0/I0/Minor0，无修正回合/复审。审查者另执行纯收据后缀118例通过，不包括27项argv/顶层准入；root实际全145通过，不能把118说成审查者独立跑完145。继承v095失败snapshot、清理异常、时序与负例副作用等Minor/开放项保留。

| 审查保留项 | root裁决、理由及代价 |
|---|---|
| 1 实际W/旧preview | 未触及输入读取/排名，不关闭；后续仍需真实读取边界，不用serial记录替代按键资格。 |
| 2 物理遮挡/receiver | 未触及LOS/目标授权，不关闭；代价是原正常路线目标选择仍待实证。 |
| 3 Intro/真实Playing | 原准备拒绝未改，不重新认证全部生命周期；v095单消费者通过不泛化，完整回归失败保留。 |
| 4 callback/deferred退出 | 未新测同actor/parent/pose/sequence跨真实边界，不关闭，不能用detached标志代替执行。 |
| 5 LoadLevel主/清理异常 | 未改loader，自有新控制不等所有caller清理已保主错；继承风险及故障注入义务仍在。 |
| 6 native所有权/Variant/GCHandle原因 | serial只证明真实使用；本次命名slot/叶不授nativeowner或因果，代价是还须精确生命周期反例，不先改缓存。 |
| 7 REF/普通全回归/原75 | root按上表实际FAIL及models恢复裁定，不以focused替代；仍不能交付。 |
| 8 正式视听/相机/真人/硬件/发行 | 显示NOTSEEN、无对应新执行，全部OPEN；素材核验不是观感或付费人数证据。 |
| 9 全保管/145纯合同 | 独立custody另核40对/48bins及live全源，root实际145；不扩大审查者118子集或读取存档语义。 |
| 10 观察器分配/I/O/清理异常全集 | 结构和原sink/cap合同保持，但未新证所有时序/异常；观察可扰动，当前trace无END仍FAIL，不能称普遍安全。 |

继续既有用户选择的脏工程，不另建工作树/提交，原十一项没有task-done/完成行。选择原值serial而非构造栈是为了覆盖缓存中早已存在的wrapper；代价是未被原工厂使用的wrapper仍不能关联。本轮私有TakeOverPath只修测试前提；保留Scrap来源读范围与512 cap是为了不凭诊断额外读取改变故障，若上限不足仍须报告不完整而非扩上限求绿。

## 最终保管与留档

最终普通案 `models-20261024-163809-424-776aeb50800d4ee7a759739295d48951`。8案40对清单逐字节一致，48份OWN bins按各自fresh清单全部匹配，缺失/额外/重复键/不符0，不跨案假设DLL相同。完整live1480runtime＋296imported＝1776/C#892，新增/缺失/哈希漂移0；helper9匹配，bin6匹配/额外0。真实user清单只保管比对，不作存档语义验收。

状态文档写回后，root再次逐项读回1776源及最终REF案12项帮助器，缺失/哈希漂移0；因此新增协议测试帮助器也覆盖在当前读回中，而不只核普通models的9项。index仍未改。

- source manifest：56DAB28A573F8B26FFC2B9242AF09676D5982BF8CE4FEA046BC8774C6DC860C0
- helper manifest：C2AD06CF79483D287FC9B84EEA86101375BF2C5652287D7DFEE5B410BAC2BA24
- binary manifest：3DEA097A2DDAD98A23BD80A3BC7EF3EF8AEDDC17592E65DD98670075CCA265FB
- Knight.dll：2111B06B8B59ED5B1B1D7153653066AA6C5DFB478B65882D419C6BE2098EBE44
- branch仍codex/task6-certified-final-20260902T0555，HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C；无Git写入。
- diff --check exit0，仅169条既有LF→CRLF提示、其他0。
- 媒体单次检查2026-10-24T08:44:40.5099132Z：当前860284 bytes、基线3508、增长856776，Notify=false，无新图稿/实录，不建自动化。

原生拥有者均terminal后才更新状态文档。没有自动弹窗、OS输入、新正式资产/玩法、付费、自动化、打包/推送或删除。目标保持完整十一项ACTIVE，下一步是命名资源的精确生命周期复现，并推进已核清来源的正式单件素材接入；不是把目标缩成诊断工具通过。
