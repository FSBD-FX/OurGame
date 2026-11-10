# v258 · 真实区域配置替换点身份关联

状态：有界REF诊断已实现并经过真实RED→GREEN；不是资源故障修复。当前普通全16加普通诊断排除门共17项单批全部严格PASS，已终态exit0；同版绑定完成，唯一新审查C0/I0/M2，两个Minor延期，无C/I修补或二审。此诊断任务收尾，不代表原十一项完成；全目标继续ACTIVE，带诊断7错误和历史FATAL不关闭。

## 实际改动

仅现有三个测试CS与canonical验证宿主：Task14NativeResourceIdentity在原probe scope同步观察现有world.Definitions，BasinSupplyRuntimeTests在原替换前后接入，RunResidentCensusIdentityContract新增精确提前小分流。没有改游戏生产能力、地图、物理、动作、美术、保存或难度，无新CS文件，源集合仍1897。

新记录包括root/world/index/sector/已有managed serial/native ID/stage。每次最多16字段、每scope64快照，错误或超限严格失败。复用现有纯标量registry，重复观察保首次身份并另记阶段；未登记仍unknown。此处的owner label只表示赋值关系，不证明资源真正生命周期所有权。

Supply只使用原来已经取得的world/root，在已有ConfigureLegacyWorld前后同步读取；不增加Load/GetState/Instantiate、Scene依赖遍历、GC、cache、KeepAlive或借用Resource跨await。观察异常调用既有ObserveWithOwnedRoot清理本次尚未交付root；不伪称所有异常路径零生命周期影响。日志/native字段读取仍会扰动时间。

## 实际验证，分开保存

证据根：G:/knight/验证证据/october13-stable-claw。

| 执行 | 实际结果 |
|---|---|
| PS实际AST准入 | 缺少真实函数RED→39 GREEN；补实际paramblock/early gate后最终51 GREEN |
| 原相关纯契约 | 26+157+84通过；合计318，无native |
| 首次窄合同193620-275-9ecfaebddb624b9aa91a18f1ea654e59 | 构建6错误：typed Godot Array非IDisposable；不是行为RED，原始失败保留 |
| 窄合同193807-645-e88c4e09363240a0a82089dc6f6b619e | build成功；native531ms真实RED：观察器缺失，guard0 |
| 窄合同194014-765-6f9303cba66348108853f23e1452e53a | 实现后native629ms GREEN；真实旧2/新1/重复1及unknown/首次保留/失效与数量上限 |
| 最终窄合同194226-640-fbd8da18e984492faefed2962c9a85da | 加root0/空stage/真实写入失败控制，native615ms PASS/guard0 |
| 一次pre REF194310-703-fea05033c4a343d89841ae8f5888c7b8 | native146995ms/exit0/无超时/guard0，44案例及原退出drain返回，仍因raw资源错误严格FAIL |
| 普通models恢复194615-479-fa86deb7bfdc4fb296f35114228cfb93 | native661ms PASS，恢复普通Rebuild |
| 当前普通17门 | 同一父句柄8360全部严格PASS，exit0；不是沿用v256资格 |

运行过程记录：句柄8360先返回models/state，再返回patrol等；当时后续尚未完成。全程同一批次，不因等待或旧失败重启；代码、宿主和资产保持冻结，仅更新文档。容量检查（2026-11-05T11:50:28Z及12:12:38Z）均7,427,750B/86files/Notify=false，无新增媒体或自动化。

后续同句柄更新：resident-patrol与resident-geometry亦通过，目前前4项PASS，resident-allocation已开始。其余及整批仍未验收，不宣称当前全绿。

最终同句柄状态：17项全部返回PASS，已终态exit0，不重启本批。Main200923-889-04e5f0f1ca3e4eb285be2763ae0128f8 native248912ms；Explore201414-730-51ed4881a9414b14b6c74a93340c641d native319459ms；排除门202025-008-5742beb529f14fafb69c39b9cbc7dc2f native536ms，实际loaded assembly无两类诊断类型、无新诊断事件。

### 当前普通结果（不是完整序章验收）

| 检查 | native毫秒 | 结果 |
|---|---:|---|
| models | 539 | PASS |
| resident-state / patrol / geometry / allocation | 638 / 45668 / 33106 / 618 | 全PASS |
| recycling-traps / canyon-structure-v2 | 25936 / 1800 | 全PASS |
| prologue-pre / post | 150434 / 183251 | 全PASS |
| ranged-models / keyboard / actions | 522 / 20404 / 9532 | 全PASS |
| save暖 / 独立冷读 | 12833 / 1751 | 全PASS |
| incinerator-traversal | 35382 | PASS |
| Main / Explore / ordinary exclusion | 248912 / 319459 / 536 | 全PASS |

17案51份ordinary stream无ERROR/FATAL，但post有4个已限定负控WARNING：throwing ClawFrameChanged observer、错误Room视觉根、两次Completed→Recovering拒绝；stderr/external是同批镜像。根已直接核对原Assert-ProloguePostNativeDiagnostics的四条精确multiset与三scope，而非放宽warning门；不能写成全raw零WARNING。

Main/Explore实际隔离JSON均startedFromTitle/persistentPlayer/bossDefeated/supportedAtCompletion/Completed，heldInputFrameCount60，恢复0/0；各Boss/arena/completion事件1次，保持位置与save hash不变。遭遇activation/defeat为8/8和11/10，不能当76个居民全量；automated且humanValidated/operationCoverageComplete/fullGoalAccepted=false。没有系统键鼠输入、正常显示或硬件/真人结论。

### 当前绑定

[根实际审计](../../.superpowers/sdd/2026-11-05-sector-assignment-identity-v258/audit-v258.json)与只读子任务独立重算相符：20案（17普通+最终小合同/一次REFpre/普通恢复）合计100保护清单对相等；所有source manifest一个版本，当前1897项/937CS/514testCS完整集合与逐SHA0差异。17普通+restore共18案的6binary一致且与当前6匹配；2REF案是另一单独一致版本，120封存binary逐文件匹配，不混进普通资格。18helper无冲突或当前漂移。相对v257源集合无增删，仅预期三个测试CS内容变化；不是旧资格拼接。审查后[根最终重算](../../.superpowers/sdd/2026-11-05-sector-assignment-identity-v258/final-binding-v258.json)仍全部无差异、120封存文件逐个核对、5 authored hash未变，分支/HEAD未变。审查员另独立核对5 authored及100保护对，并未第三次全量重算所有1897输入；不夸大为三重全量认证。

## 本次REF原始关联结论

只读解析与根直接计数相符：10快照/50字段行完整，五组同root/world的before→after各index/sector0–4；共35serial/35native ID一一对应，无冲突。前三次before共享本进程serial6–10、后两次共享1060–1064，after合计25个独立对象。共享是字段重复引用，不是身份错配，不与v257的serial1076跨进程混比。

这35对象均有自然Dispose ENTER/RETURN（70行），完整首次身份回显0差异；全1831 ENTER/1831 RETURN，0未返回/重复/错配。首次registry只代表首次观察，不是唯一owner。

严格失败的7个Handle ERROR头在stderr37648、37701、37754、37807、37860、37913、37966，external镜像不重复计数。均为Handle is not initialized→SwapGCHandleForType→Node.AddChild→DescentClawGuidanceRuntimeTests.Run559→RunPrePhase243。不是Supply.OpenSample；错误起始时无尚未返回的已记录Dispose区间，不能把最近的serial1068或任何新行当故障对象。本次WARNING/FATAL均0，但这不冲销旧FATAL，也不证明根因已消失。

完整性分别计：allocation终检Presentation585/损失0、Sector40/损失0；assignment与Dispose配对已完整。但既有PROFILE_LIFETIME_ROW达到8192上限且没有pre完整性END，22个stack长4096亦可能截断，不能称该journal零丢失或借用allocation的losses0。下一可用入口是出错Descent原AddChild前已取得字段的有界关联，以及单独显性化journal完整性缺口；尚未实施，不增cap或重跑求quiet。先完成当前普通17门及审查。

对本次前缀/Descent的限定只读源码排查未找到明确借用缓存资源被Dispose证据：Descent默认Load.Instantiate到AddChild之间无await/Free/Dispose，Camera释放自有根、Laborer shape为new、Claw capsule为Duplicate、Health为new节点，共享teardown没有释放加载源。不能据此证明native绑定健康，也不能因发现using就擅自改判其所有权。

独立只读产物：[角色真实运行素材和动作接入口](../art/player/2026-11-05-current-motion-asset-binding-readonly-v258.md)。它明确区分当前程序身体/披风、真实PNG附件与本地未审35层候选；不是新动作、恢复V1批准或视觉验收。上述普通17门与唯一审查现均终态，下一测量仍未实施，不把计划当实现。

完整计划：[v258](../superpowers/plans/2026-11-05-sector-assignment-identity-v258.md)；执行账：[progress](../../.superpowers/sdd/2026-11-05-sector-assignment-identity-v258/progress.md)；[唯一审查及全部Declined裁决](../../.superpowers/sdd/2026-11-05-sector-assignment-identity-v258/final-review-v258.md)。本页是有界制作记录，不是正式交付证明。

## 唯一审查与延期

唯一fresh审查C0/I0/M2；根按用户影响复核后保持评级、保留有界增量，无第二审查和额外native。两项新Minor只记账延期，旧延期不关闭：

- M1：独立合同设置阶段在try前创建自有root/world/fresh/数组，清理Free/Clear可能跳过后续步骤或遮蔽primary。当前成功案未见泄漏，原Supply已有异常清理不受影响。后续应早建立拥有保护、逐步登记/全部尝试清理与primary/cleanup保全，再做设置和清理故障注入。代价：诊断异常可能多退出噪声或缺首因。
- M2：原生合同未断言snapshot header/Sequence；当前实现与实际10/50协议正确，未来改坏header/count/sequence可能漏检。后续应独立断言3header、sequence1/2/3、count2/1/1及每组索引/行数，再补缺失重复及错值负控。代价：未来快照分组退化的覆盖欠账。

## 全部裁决及代价（执行顺序，共16项）

1. 沿既有Native原地直接推进，不重复询问已定路线；用户要求判断后执行。代价：脏树隔离有限，保before/不动Git。
2. 用独立窄合同，不跑旧完整census、不把控制对象塞原pre前缀；避免额外GetState和新资源批次。代价：新增准入/合同维护。
3. 7Handle根因仍未知，不从相邻日志指定对象。代价：资源错误未修。
4. 不认历史FATAL同因，不跨进程套serial。代价：旧崩溃仍OPEN。
5. 不认观察零扰动，诊断与普通资格分开。代价：无时序等价证明。
6. PROFILE_LIFETIME上限/无END不认证全量，不借用其他losses0。代价：事件完整性未知。
7. 35配对身份不认证所有Resource生命周期。代价：其他资源释放仍欠审计。
8. 不宣称全异常无泄漏，M1独立控制缺口延期。代价：诊断异常可能遮首因或多噪声。
9. 审查员未第三次全量重算1897/18/120；根与只读子任务已实际重算。代价：不是三份独立全量审计。
10. 普通17PASS不等于全部断言语义复审。代价：未覆盖行为仍可能有缺陷。
11. 两路线遭遇ID不当76全量。代价：完整居民体验覆盖仍OPEN。
12. headless不代正常显示/硬件/真人，不越GUI/OS权限。代价：后续还需真实授权验收。
13. 无正式美术/连续动作认可，现稿须重审、旧批准不转授。代价：美术/动作实质返工仍欠。
14. 隔离存档不等于真实玩家存档无损认证，不做真实正文实验。代价：无真实玩家样本认证。
15. 原十一项整体不完成、不缩目标。代价：剩余制作/验收继续。
16. 不发布、不宣称整游戏ready；REF失败/历史故障及开放项尚存。代价：暂不能正式交付。

审查后容量Check（2026-11-05T12:41:34Z）仍7,427,750B/86files/Notify=false。本轮未建立自动化，未清理/覆盖原件、任务workspace或Git状态。展示现有候选图稿的单次异步询问尚待答复，未展示或自动接入；当前仍只无窗口工作。

## 原目标与资源表

| 资源 | 来源与状态 |
|---|---|
| 标量观察/小合同/PS准入 | 本地测试基础设施，before与各失败原件保留；不是新游戏资产 |
| 正式图稿、实录、音频、动作、美术生成 | 本轮无新增；V1源暂缺，后续重新审阅现稿，旧批准不转授 |

原十一项、正式身体/披风与连续动作、惯性特色物理R-I/R-C/R-P、76居民全量、正常显示从标题/硬件/真人认可保持OPEN。三段误给视频仍作废。只headless/internal engine keyboard/隔离存储，无GUI/OS/付费/新媒体/dump/真实存档实验/引擎GC-cache策略/自动化/Git/删除或发布。
