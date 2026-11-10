# v118 · 实际乘梯覆盖与 Skitter 查询所有权

2026-10-26；继续原 Task15 / 完整十一项，目标 ACTIVE。本轮是局部制作与验证进展，不是整关、正式美术、性能或交付完成。唯一工程 `G:/knight/Knight`；另一个 UE 项目不在范围内。用户最后的窗口反馈仍为 **NOTSEEN**。

## 事实

### 本轮实际改动

- `PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat` 原先漏掉 CanyonSkitter，只有 Pendulum 特定调用才传入补偿标记。真实保存的 scrap Charger 自动进入 Telegraph 时，selected/foreign 两种选择均被遗漏。新增一个 Telegraph/Rush 分支；不改危险排序、碰撞、AI、生命、能量或范围。
- 原 Charger 20 案保留，追加真实保存场景的 10 次危险观察：自动 Telegraph/Rush 的 selected/foreign 正例，paused、Recover、queued 负例。规范 owner 在旧 20 案收据之外要求新 10 次收据。
- Basin 登梯用实际攻击源确定护盾方向。只有更新的输入序列、准确的物理 ADWS/L、采样 L、实际盾和方向全部匹配，才允许守盾暂时接管方向；不反复重按被撤回的 D。保留原行走意图、成对释放、生命、完整支撑和全部原时限。
- 原 Bell 在乘梯期间连续正常运行但未攻击，旧检查却要求必须有攻击几何或实际接触。新增第三条严格静默路径：同一原 wave Bell、原 parent/生命、有效可处理、未暂停/删除，126 次真实 `FrameChanged` 覆盖原 126 步，严格物理帧连续对齐，入口/出口与所有回调均无 Active/Monitoring，原 contact/geometry/fault 计数均为零。原实际格挡和正攻击几何路径保留；每 tick 的玩家生命与完整站台支撑、200 步上限保留。订阅位于原外层 try 内，finally 解除。
- **生产修正**：`CanyonSkitter.WorldUnder/HasClearWorldPath` 的本次自建 ray query、self-RID 数组 backing、返回 Dictionary 使用明确作用域释放。保留原端点、World mask、areas=false、bodies=true、仅排自身 RID、返回条件。没有释放借用的 World、Space、节点、保存 Shape 或 profile。

### RED → GREEN 及证明范围

1. Charger 危险观察：014209 实际漏报 RED；014534 原 20 案及新增 10 次观察行为 GREEN，但 stdout 有 8 Shape RID，严格 FAIL。
2. 登梯：014902 实际 Bell hitbox 源比身体高 10px，Right 盾与真实入射相差约 84.15°，超出 75° 半弧，生命 2→1。修正后有 13 次实际 W/L 方向接管，生命 2→2。
3. 静默乘梯：021512 的实际证明为 steps/callbacks=126、start4414/end4540、first4414/last4539、无重复/缺帧/Active/fault，但旧条件仍拒绝。新纯条件的 023158 RED 是**完整四步正例被拒绝**，并非某个负例失败；023329 和最终 024323 均通过 1 正例/21 独立字面负例，以及原两例 owner 清理合同。
4. 查询所有权：023836 原生产函数在 64 次 short-support 同步调用中最大返回后 Object 净保留为 1，真实 RED；最小 using 修正后 024010 七组最大净保留均为 0，旧 20 案及新增 10 次观察也全部返回成功。
5. 计数口径：七组各 64 次＝**448 次方法调用**；其中六组测量真实 ray query，另一组是非有限路径提前拒绝，不能写成 448 次原生射线。预热和 fixture 自己的查询不计入这 448 次。ObjectCount 仅证明本次 query Object 的同步释放，不证明 Array/Dictionary 的底层内存或总内存无泄漏。
6. 024010 引擎 exit0，但 stdout 仍有 **8 Shape RID exit ERROR**，严格 FAIL。015616 单变量关闭新危险方法时只有原 20 案，stdout 为 4 Shape RID，规范 owner 也正确拒绝缺少新收据。8→4 仅支持相关性，不定位具体资源。没有 GC/cache/全局保活/借用资源 Dispose 或引擎补丁。

### 同冻结最终回归

根批次 handle61297 已 TERMINAL0，只表示六个子调用均终态，不表示六项都通过。全部运行来源为自动测试注入的游戏键盘事件，不是 OS 输入、真人游玩或正常显示性能证据。

| 最终阶段 | 严格结果 | 引擎 / ms | 实际边界 |
| --- | --- | --- | --- |
| First-Reel contract 024323 | PASS | 0 / 523 | 1 正例/21 负例及原 owner2 |
| Main 024400 | FAIL | 1 / 140433 | 玩家死亡后的恢复位置变化触发 no-reset 门 |
| Explore 024652 | FAIL | 1 / 158544 | 乘梯/Gear 通过，Steam 原900结束仍未击败 |
| pre 025003 | PASS | 0 / 165011 | 原完整前段，0 ERROR/0 WARNING |
| post 025318 | PASS | 0 / 198664 | 原75项各一次 enter/return；owner8，节点1→1 |
| models 025706 | PASS | 0 / 531 | 普通模型回归/构建恢复 |

post 为原已修正的 300000ms 工作量预算；不追认旧 180000ms，也不当作性能认证。post 为 0 ERROR、4 个原精确限定负控 WARNING（throwing observer、错误 VisualRoot、两次 Completed→Recovering 拒绝）；不是忽略新警告。

## 两条正常路线仍未闭合

### Main：先受伤死亡，后恢复，不是已证实的瞬移故障

最终 Main 的原始 accepted-damage 记录：resident precision-burst 在 physics7067/7089 令生命 3→2→1；physics7114，wave `counterweight-lift_batch1_2` BeltBurstGunner 的实际第4发令 1→0，impulse=(-119.85809,5.834301)。该帧 E=0、heat=.8784975、未过热、L held/active=false、站地速度0。actor model attack3 与 ShotOrdinal4/伤害 LocalAttackId4 不混作一个编号。

随后有明确 DEATH RESET，fall_damage=0；生产 `OnPlayerDied→RequestRespawn→RecoverInCurrentPhysicsFrame` 先恢复生命/能量，再 RestoreCheckpoint。日志同7114出现生命5/能量1，7115才从(4751.553,3423.933)回到(1680,3960)。no-reset 断言先于恢复完成而失败，不能凭 completion0 判定恢复或存档故障。下一步应处理真实混合火力和能量/攻防策略，不删除 no-reset 断言或修改正常复活流程。

### Explore：Gear 已过；Steam 存在战斗重开和错失 K 窗口

023414 和最终 024652 的实际乘梯保持生命2；Gear 为 9 次J、11次连接、4次K，玩家生命2→2，无伤害。原居民/波次/机关仍活动。

Steam 初始 Bell19043062912775 在 physics7286/loop0 绑定；loop39–50已 ready、无威胁、E1，却直到 loop150 才首次K。前6J击败该 Bell 后，physics7696已有 batch1 Sentry，但选敌仍转向 resident Charger。physics7907/loop585 又出现新 batch0 Bell20875671771022；最后两J只作用于这个新 Bell，剩4HP。**不能把8J描述成同一个Bell只掉2HP。**末次第二J后 E=.075998，曝光结束时 .1133316<.12，所以第三J确实付不起；没有玩家伤害或过热。

生产 Arena 为x3040…3840，离界会退休并重置 batch，回入 Activation 再开 batch0。新 Bell 时玩家从右侧回入 Activation 与此相符；但尚无离界/Retire 的精确帧与输入，触发原因只列推理，未实施生产重置改动。Steam 的旧 frame%75 K节拍确实错过准备窗口。

下一最小候选：仅让 Steam **选中的 Bell**使用已有 actual-ready K机会，不传入 gearLureBell 误开 PistonTwo诱导；保留同一次 PlanSelects、实际 CastAt、目标/application/曝光/生命/无威胁/支撑与原900，并被动记录现有 batch/selected/active/位置/按键变化，捕获真实重开边界。该候选尚未实现，不预先宣称通关。

## 全部17案与失败保留

目录根：`G:/knight/验证证据/october13-stable-claw`。以下日期均为20261026；时间前缀唯一对应该根下的完整 case 目录，其 provenance、三份原始 sink、源/帮助器/用户哈希和封存二进制保留。

| 时间 / 阶段 | strict | engine / ms | 原因或范围 |
| --- | --- | --- | --- |
| 014209 Charger | FAIL | 1 / 43207 | 真实危险漏报；stdout8 ShapeRID |
| 014534 Charger | FAIL | 0 / 44440 | 行为20+10绿；stdout8 ShapeRID |
| 014902 Explore | FAIL | 1 / 86399 | 登梯受伤2→1 |
| 015616 Charger控制 | FAIL | 0 / 42558 | 原20-only；4 ShapeRID/正确拒缺新marker |
| 020544 Explore | FAIL | 1 / 87161 | 安静乘梯缺原攻击正证 |
| 021512 Explore | FAIL | 1 / 88963 | 全126回调证据已到，旧门仍拒绝 |
| 023158 contract RED | FAIL | 1 / 518 | 完整四步正例被拒绝 |
| 023329 contract GREEN | PASS | 0 / 501 | 1/21及原owner2 |
| 023414 Explore | FAIL | 1 / 155842 | Gear过、Steam未击败 |
| 023836 query RED | FAIL | 1 / 796 | 净保留1；4 ShapeRID |
| 024010 query行为GREEN | FAIL | 0 / 44440 | 净保留0/20+10过；8 ShapeRID |
| 024323 final contract | PASS | 0 / 523 | 同冻结合同 |
| 024400 final Main | FAIL | 1 / 140433 | 致死后恢复触发原no-reset门 |
| 024652 final Explore | FAIL | 1 / 158544 | 同冻结Steam仍未通 |
| 025003 final pre | PASS | 0 / 165011 | 完整原前段 |
| 025318 final post | PASS | 0 / 198664 | 原75/owner8 |
| 025706 final models | PASS | 0 / 531 | 普通模型 |

合计 **5严格PASS / 12FAIL**；17次构建均exit0、0警告/0错误；全部无超时、ExpectedFailure为空、guardFailures为空、SourceRows1844。失败反例不当作资格，exit0不覆盖原始资源 ERROR。

## 执行裁决及代价

- Ruling：沿用用户脏 checkout 与原 Task15，不新建目标、BASE、worktree或Git提交。依据是已确认KNIGHT范围；代价是不可把本轮归因扩成整个脏分支资格，依靠物理前后快照限定。
- Ruling：先修真实遗漏的 Charger carrier，而非猜“候选排序输掉”。selected/foreign真实RED支持；代价是更早战斗时序会变化，必须保留新路线失败。
- Ruling：登梯允许的是明确采样的盾方向所有权，不是输入丢失补键。代价是未知/过期方向仍会失败，不能偷偷重放D。
- Ruling：不强迫AI攻击来满足测试；完整126原生回调允许第三静默证明。代价是它不证明攻击拦截，旧接触与攻击几何门保留。
- Ruling：20-only控制严格缺收据失败；8/4资源数量不做资源身份推断。代价是资源退出故障仍开，不以局部绿色给完整测试续签。
- Ruling：自有query释放须真实ObjectCount RED→GREEN，只处理自己新建对象。代价是Array/Dictionary总分配、ShapeRID及GCHandle仍未闭合。
- Ruling：保留Steam900与Main无重置门，不加血/伤害/时间、不删敌或关AI。代价是双路继续FAIL，但失败反映实际玩法/策略缺口。
- Ruling：当前Main的位移失败按原始受伤/死亡/恢复链解释，不无证据改存档。代价是必须另测后续恢复完成，现案不能给其完整资格。
- Ruling：所有root native与父批次期间冻结源码/帮助器/文档；独立custody ENDED后才更新这些记录。代价是验证期间不并行改游戏，保证同冻结可归因。
- Ruling：过程媒体只执行报告前一次手动Check，不建自动化、不暂停20GiB流程、不新增普通调试录屏。代价是本轮无新视觉证据，不追认用户已看到。

## 一次独立审查及全部 Declined 裁决

`/root/v118_native_coverage_query_review`，fresh gpt-6-astra/high；仅只读一次，无子review、修改、构建或前台。认证四C#与owner物理基线/当前hash，Critical0、Important0、新Minor0；没有Important修复pass或复审。非merge、非全目标认证。

| reviewer设下的边界 | root裁决 | 如果判断错误的代价 |
| --- | --- | --- |
| ShapeRID拥有者 | 仍开，024010严格FAIL | 退出泄漏或运行寿命风险漏报 |
| 历史GCHandle/FATAL | 没有新因果证据，不据quiet关闭 | 正常运行仍可能崩溃 |
| 容器/总内存 | 只声明query Object零保留，不扩张内存保证 | 将局部计数错当全局稳定 |
| Steam换Bell/重开 | 原始身份链证实，精确离界触发待测 | 修错伤害/能量而漏实际重开 |
| 大范围危险优先级 | 不改，仅补真实carrier | 其他并发火力策略仍可能失败 |
| first-hit/receiver | 原授权/LOS未改，不用第二resolver放行 | 未覆盖的遮挡问题仍可能存在 |
| Intro/preparation | 原实际Playing屏障/早拒绝保留 | 未覆盖的准备/重入竞态 |
| 既有任意teardown故障 | 沿用自有fixture清理，不扩大旧清理改造 | 继承清理失败可能遮蔽错误 |
| quiet等于成功格挡 | 明确不等同；只证全程实际安静运行 | 错把不攻击当盾碰撞资格 |
| 连续子步扫掠 | 未证明；旧swept措辞Minor继续延期 | 诊断措辞可能过强，不放宽门 |
| 默认callbacks0日志 | requiredFalse/quietFalse不授资格 | 下游误读标签为执行证明 |
| energy-unwind/handoff标签 | 两个旧Minor继续延期 | 失败展开或最终摘要精度不足 |
| 最终同冻结回归/custody | 已补六案及独立终态审计，保留Main/Explore失败 | 局部绿不能替代双路 |
| 原十一项/76+BossNPC/正式AV/硬件真人/发行 | 原范围全部保留ACTIVE/FTR排除，不勾总完成 | 过早交付或遗漏批准范围 |
| 用户可见接受 | NOTSEEN，没有前台/真人证明 | 将无窗口结果冒作用户认可 |

三项 inherited Deferred minors：energy-unwind flush 的早期失败展开；几何错误文案的“swept”；`native_handoff=(stage==4)`在await失败时可过度描述。均未用来放宽验收，未顺带改它们。

## 独立保管审计与最终身份

`/root/v118_custody_audit` 已明确 **ENDED**，最后只读exec同步exit0，无遗留handle；34个记录native进程消失，CIM KNIGHT Godot/dotnet/验证owner均0。根单独持有批次61297 TERMINAL0证据，审计没有冒称跨会话检查这个handle。

- 五对manifest每案完整字节比对：85/85一致，共170份；不是仅比一行marker。
- 实际before/after副本8534份，source8354+helper180，逐个SHA匹配，缺失/extra/fault均0；102/102封存binary匹配。
- 最终Source1844/C#892和Debug6/6逐项匹配，当前漂移0。对v117仅四C#变化，0新增/删除：Skitter、ResidentCharger测试、Basin测试、Main测试；owner为唯一变化的公共helper。
- 本轮helper联集12路径，各单版本且当前全匹配；First-Reel helper是本轮新增纳入联集，不是本轮新建文件。原用户清单34份同SHA，未读取真实存档语义。
- HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，branch `codex/task6-certified-final-20260902T0555`，index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 未改。
- audio `CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`、productionClaw `6DAF77B293D1F7F04FFA05E31990143E4BED8D82B463B0BFFF492C009CC3F4AE` 未改。

最终source manifest `4E6501D2EC59B05380A22937A88D761712DECD4B8E7F7A8451D66B118B485952`；binarymanifest `28E24FDBC93DB348714545D8BEF9BE2AFE09CDA86263DA31D8603EE0F030FF6C`；helpermanifest `A1AC76A1CBEEAF2061E4787C7AA4191B2FE6831CCBD6640BC2EE66C2E403B8F1`；canonical owner `886AEA3D4D90054D4C567F8527F15C61DCE12D23573E08286FFFBF3BDE284C70`。

`git diff --check` exit0，原170条CRLF提示/其他0；没有Git写入/commit/push。G剩余184504983552字节（观察值，不固定承诺）。

## 媒体、资源与剩余交付

本轮没有新增正式美术/声音/图稿或实录，没有付费生成、系统键鼠/前台、自动化、删除或真实存档语义读取。资产表没有新增正式项。一次手动媒体Check：UTC2026-10-25T19:07:52.5082322，Current860284/基线3508/增长856776字节，Notify=false，无新20GiB档位；不建独立通知、不停工。

原稳定ID与全部十一项保持完整：正常Main/Explore、Boss出口、76普通敌人与NPC/机关有效性、死亡恢复继续、正式分层/动作/相机视听、目标硬件、真人键盘、授权包装与GitHub留档均须对应最终同一版本证明。测试通过不等于正式视觉或整关完成；本轮分类 **PROGRESS**，不标Task15或目标完成。

最终证据：[models](G:/knight/验证证据/october13-stable-claw/models-20261026-025706-843-784607b07f47483dacafa8b42386145a/provenance.json)、[post](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-025318-886-2f29f06c3d4f4c778a90ac2b18fbe241/provenance.json)、[Main伤害原始日志](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-024400-495-f70edd0ebb7a43da8f2c1741c76fc4be/engine.stdout.log)、[Explore原始日志](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-024652-399-d8f2e2f6246d4fdeaff6752e6c27c638/engine.stdout.log)。
