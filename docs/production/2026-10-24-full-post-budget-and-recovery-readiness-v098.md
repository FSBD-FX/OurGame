# v098 · 完整后半段预算测量与死亡恢复测试前提

2026-10-24。KNIGHT仍为 `G:/knight/Knight` 的Godot/.NET工程，原十一项目标ACTIVE。本轮是诊断及测试修正，不是新玩法、美术交付或整关完成。用户最新“等一下，是没看到”继续有效：旧26秒显示不算通过，也不算遮挡复现；未再开窗口或发送系统键鼠输入。

## 事实、推理、风险

- 事实：真实完整post原75项在扩预算测量中全部执行返回，native0/206055ms；普通pre仍有Handle错误，普通post再次在原75之前发生同低顶桥配置缺失及原生FATAL。不能把安静的测量案当成资源修复。
- 推理：完整post拥有30组前置、75注册案例及后置检查；原180000ms是整个测试进程的工程时限，不是单案/游戏帧性能指标。扩时只用于测量原完整范围，没有删案例、弱化错误或回写旧失败。测量另暴露了测试在Intro准备阶段直接杀死玩家的错误前提；生产准备拒绝不应为此移除。
- 风险：资源所有权/FATAL根因、普通全回归、正常标题主线/探索、正式视听、相机、真人可见性与目标硬件仍OPEN。原十一项稳定ID、76普通敌人＋Boss/NPC和机关范围不变。未重建自动化、付费生成、删除资料或进行Git写入。

## 最小改动与实际RED→GREEN

1. canonical `verify-claw-knight-v001.ps1` 新增 `-MeasureFullPostBudget`：仅大小写精确的普通 `prologue-post`，不能混入capture、verbose、identity、其他trace或named waiver。第一条有副作用之前拒绝错误组合；测量cap600000ms，原runner、30前置、原75、后置/错误/警告范围/保管链检查不变。provenance及终态明确 `behaviorAccepted=false`、`original180Accepted=false`、`performanceCertification=false`。测量结束不发普通接受回执。
2. 原预算实际表达式对已观察的index30进入179406ms＋历史同案返回2303ms不足：实际RED后建立21项纯测试；再加3项大小写控制，最终24项通过。小写普通pre/post仍180000ms，各原使用的规范小写预算不变。
3. 明确纠正账本早先的基线推断：v097基线三个分支本来就是`-ceq`，不是`-eq`。本轮有意把ValidateSet接受的三个大写变体改为相应普通预算：Lantern-visible30000、Basin-clearance90000、Resident-map-census120000，而非原fallback55000。它是命令参数一致性调整，不称所有拼写语义都未变；所有本轮native使用规范小写。
4. 第一轮扩预算post走到原index64 `RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder`，真实断言失败。新增窄 `thruster-formal-recovery` 只调用已有原四方法wrapper，普通55秒界限、named waiver拒绝、count4精确回执；不是替换原75或真人/GPU测试。
5. 窄RED183148实际伤前 `Intro/pending=True/HP5`，真实伤害已接受、HP0，但`RecoveryRequested=False`。`AllowsRecoveryRequest`在resident preparation期间明确拒绝。仅在四个原fixture既有3物理帧之后增加既有、有限、自然状态 `WaitForPlaying`；不强置状态、HP、姿势、AI或时间。原Dead→Recovery→ordinary、throwing observer隔离、攻击清除断言和原75注册表全部保留。窄GREEN183553伤前Playing/pendingFalse，伤后Recovering/requestTrue；四原方法全部完成，无ERROR/WARNING。

无生产C#、场景、美术或声音修改。新增C#仅是现有runner分支、上述测试等待和第一案边界标量；Source仍1776/C#892。

## 本轮8个canonical原生案（全部终态）

证据根：`G:/knight/验证证据/october13-stable-claw`。表内是严格wrapper结果，不用native exit0冒充通过；扩预算PASS也只为诊断。

| 案例目录 | build exit/ms | native exit/ms | 严格结果 |
| --- | --- | --- | --- |
| prologue-post-20261024-181405-970-d0e35a0d06a4495fa1174d2a12798ab9 | 0/31697 | 1/190857 | FAIL，65 enter/64 return，index64同步恢复断言 |
| thruster-formal-recovery-20261024-183148-408-778ddc319893484180557566640b290a | 0/27336 | 1/1864 | 实际RED，Intro/pending；断言及退出资源错误 |
| thruster-formal-recovery-20261024-183553-224-05baaf72884243188875e07dea858bd7 | 0/34276 | 0/13200 | PASS，四原恢复方法 |
| prologue-pre-20261024-183740-750-4ac0669e4c70414ab84499b7aca52587 | 0/29996 | 0/169618 | FAIL，行为/phase/drain在，但7条Handle ERROR |
| prologue-post-20261024-184313-188-3405678c8d8a40f99bdbf91c22370d27 | 0/26821 | 0/206055 | 扩预算诊断PASS，75 enter/75 return及完整后置；不是普通180秒通过 |
| prologue-pre-20261024-184803-857-a43982584461498181fc4694b4a8e13c | 0/29816 | 0/167483 | FAIL，行为/phase/drain在，但1条Handle ERROR |
| prologue-post-20261024-185311-862-c047438f13994904966d81bff76b1515 | 0/30986 | -1073741795/160576 | FAIL，非超时FATAL，原75为0/0 |
| models-20261024-185720-517-91230986b2c341b1ac21390c1b3cd4ac | 0/27975 | 0/658 | PASS，最终普通构建/模型回归 |

8案8次build0；严格3 PASS/5 FAIL，其中1 PASS仅扩预算测量。全部非超时；不能把本轮普通post的160576ms原生崩溃解释为180秒预算，也不能用扩预算安静案抹掉历史FATAL。

主要stderr ERROR共12：Handle8、FATAL1、恢复断言2、失败退出resource1；WARNING11（包括flow拒绝4），ShapeRID0。external镜像不重复累计；HUD失败detail不是第二条ERROR。正常post185311仍为 `low-ceiling-bridge.03` ProfileMissing→Title，随后`gchandle.is_released()`，未到原75。扩预算184313四条警告由seamless1、room-profile1、flow-registry2三个原限定回执覆盖。

## 纯合同、审查及裁决

最终纯合同157/96/26/33/11/24/24全部PASS，均不启动native；新预算24只是提取函数/准入行为证明，不能代替完整游戏或性能证据。一名新上下文只读最终审查：Critical0、Important0、Minor2；无复审、无修复pass、无merge。

延期Minor1：新预算测试有函数时直接测函数，没有执行当前`$engineCapMs`赋值/`Run-Owned`消费连接；当前连接已检查正确，但`actual_deadline_consumer=true`回执比实际覆盖宽。后续应增加错误调用参数/常量的可执行反例，当前不宣称已覆盖这类变异。

Minor2是基线口径不精确，已在本报告与账本更正为“显式大小写一致性调整”，不是代码追加修复。四fixture继承的成功尾部才清理、两项AttackStarted局部无超时仍是测试债务；失败会严格失败且由外层进程界限终止，不当作已解决或继续跑后续案例的许可。

以下是审查Declined逐项root裁决（未静默丢弃），以及本轮决策的代价：

1. W输入/目标排序未触及：保留既有未完成路线项，不用本轮恢复证明操作顺滑；若判断错，正常主线仍可失败。
2. 敌人遮挡/接收权限未触及：没有新first-hit归因，不追加爪优先级或无视实体；代价是相关路线问题仍需独立验证。
3. 生产准备前拒绝：已有源码和实际Intro/pending RED支持等待消费者自然Playing，不拆生产拒绝；不宣称完整重新认证生产准入，错误则会在实际恢复断言继续失败。
4. 回调/延迟退出身份：四案保留自然FramePublished及原断言，不新增姿势/节点替身；没有全场景Actor重入证据，误推将漏掉后续生命周期问题。
5. LoadLevel及异常清理：未改独立实现，未新增故障注入；继承fixture失败清理风险明确延期，不能据成功案声称失败路径安全。
6. 资源因果：206055安静案只作对照，不归属nativeowner、不称修复；代价是普通资源故障仍阻挡交付。
7. 普通验收：两pre都是严格FAIL，最终普通post是FATAL，不翻写为budget PASS；代价是本轮仍没有普通全回归资格。
8. 正常双路、完整人口实战、正式视听/相机/硬件真人/发行：均保持原十一项ACTIVE，不由75测试或模型通过替代，代价是尚不能交付最终序章。
9. 初始C#基线：审查时指定post181405目录缺两文件，不能假装已审过旧拷贝。root另找到与v097 full-source哈希一致的冻结拷贝并实际diff：runner仅新增formal-recovery分支；PlayerFormalVisualTests仅四等待及首案伤害前后标量。审查对当前新增代码的检查仍有效；精确旧拷贝查找不算第二次审查，错误会影响变更归属而非自动游戏验收。

本轮其他root Ruling：仅增加独立有限预算测量、不悄悄放大普通post；等自然准备、不猜更多固定帧；保留ValidateSet大小写一致性调整但纠正历史描述。代价分别是普通回归仍可能截断、真实准备失败仍失败、少见大写CLI参数的时限发生显式变化。

两份初始C#来源：

- `thruster-free-forecast-20261024-051906-122-dbc2ff8ad7134159986e8d40ddcbeae7/after/tests/RunThrusterCoreTests.cs`，SHA256 `E7785E330AC11BB09D801E7598C2A58B6499055BC9CDD0EFC444231D49B09DB6`。
- `thruster-formal-claw-phases-20261021-201514-555-468106df23f148089d5d98f7de0602b8/after/tests/integration/PlayerFormalVisualTests.cs`，SHA256 `C21EBD06E621612379225E7B13C5F65B2CB929DF8C253500C4F12F2C59A5B15F`。

## 最终保管链

独立只读核验8案40对manifest、48个OWN二进制全部匹配，无缺失/额外/重复/哈希不符。当前完整源1480＋imported296＝1776/C#892，新增/缺失/漂移0；最终models的9个helper及扩预算184313的10个helper均与当前一致；实际6个bin无额外项、全部匹配。所有native终态后才更新本报告及README/待办/意见/账本，未在owner运行中修改源、helper或文档。

| 范围 | SHA256 |
| --- | --- |
| 最终source manifest | CA718258BBC8870FBAB2DB79C65D553A0AB55146F49744012267F13DCB82210C |
| models helpers9 | 6D795EA7EE37663889882A023C5BF39D6646B713ABD18342D50181B3845FDC0C |
| expanded budget helpers10 | D147995FD98C2BB9FF777D792137F5439647E814B3783DFE4AA22D2714BE46D8 |
| budget pure | 62E812442DC81EBF76A163D009EF736CC046F59C9EEB7E4ABA2119595E0B9013 |
| binary manifest | 629EE7E2CB840D122EFA5EA7C58C6CF292F44CCAB93CB967B4CF587ADAA2ED53 |
| Knight.dll | F68BC099FC99C02195485406980278FAF3218ACA5834D552EBD86C5F558CD3FC |
| Git index | 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C |

branch `codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`均未改变。普通最终构建已恢复；没有把诊断mode留作游戏入口。

root在上述文档更新后再次逐项读取1776源及10个扩预算helper，漂移均0；index仍同SHA，无owned Godot/dotnet。`git diff --check` exit0，169条既有LF→CRLF提示、其他消息0；不把换行提示当作新增功能/测试错误。

## 存储与下一步

所有root owner已经终态，无遗留Godot/dotnet；未进行Git提交/推送/分支/索引改动。2026-10-24T11:00:02.9979392Z只执行一次本项目媒体检查：Current860284/Base3508/Growth856776 bytes，Notify=false；没有新增媒体、频繁录制或恢复自动化。

下一步是把完整测试的总工作量与普通测试时限明确对齐，同时继续对同保存敌人叶的Ready/ExitTree路径作最小资源复现，并单独补fixture失败释放/有限信号等待；这些尚未实施，不能称资源修复。正式武器握持落差、完整主线/探索与正式视听的剩余工作按原十一项继续，不把测试工作替换成最终交付。
