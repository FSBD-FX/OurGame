# v121 · 开火后单次机械爪与严格隔离对照

2026-10-26；沿用原 Task15／完整十一项，目标 **ACTIVE**、用户窗口状态 **NOTSEEN**。前一目标轮为PROGRESS；本轮也为 **PROGRESS**：明确修订过时的测试调度，取得真实post-shot K接续，完成可辨别且不能冒充正式post的资源对照。两条完整路线和资源问题仍未关闭，不是正式视听、性能或交付完成。

## 实际范围与调度裁决

仅4个测试C#，对认证物理v120基线合计 **121新增／18删除**：Main40/13、Placement37/0、RunPrologue25/5、Task7Tests19/0。Source1844/C#892、0源码文件增删；生产代码、AI、地图、伤害/生命/能量、抓取排名/LOS、正式图声与公共helper均未改。

生产机制和v102原证明不要求K早于首Shot；v106才引入“必须使用最早pre-shot机会”的内部测试调度。旧策略的Stunned在own-Shot门开启前耗尽。根明确替换这个内部调度子目标，不称它是用户新增玩法，也不悄悄删除真实命中义务。

- 新可选 `postShotClawSchedule=false` 保留所有默认early调用；只有Main counterweight调用显式开启，运行时还要求原键盘counterweight条件。Explore不启用。
- 早期真实同目标preview仍记录一次K欠账；只有该captured owned Burst实际Shot后、原完整资格成立，才建立post-shot窗口并发唯一K。
- 旧 `Task10MixedClawCompletionBoundary`、8个缺失/重复/abort负例、终态检查不改。先消费真实application恰好+1、same-cast latch、Stunned、单sample/edge等完成收据，再在inner continue之前及outer等待路径使用新纯调度谓词。
- 新谓词区分欠账与窗口：未开窗口可在原900内等待；已开窗口必须已Shot，未完成则不能过期；真实完成赢同帧到期；abort、不一致的无欠账窗口或无窗口完成拒绝。
- 原14门的 `0x20` 原位检查 own Shot及欠着的K完成/stage4，fresh同实例重检也用同一纯条件；原ownership、HP1、Shot ordinal非零保留。没有第15位、额外CanBegin/CanAfford、预测或resolver查询。终态机会为早期 **OR** 后窗口，不能静默省略欠账。
- 原900、完整J时长＋6＋2tick、原24frame冷却、双AttackCost储备、全部危险源/旧弹、HP/支撑/零恢复、真实jet与终态都保持。

## TDD及真实主线接续

RED051223引擎1/412ms，实际断言同时揭露两个新谓词的错误：首Shot前/到来后/窗口内等待、非法pre-shot窗口或完成、无欠账/abort，以及欠着K或未handoff时仍允许致死J。RED051254引擎1/520ms，实际断言暴露prefix控制误拒正确arm、未拒pre/空格/未知arm/错误phase。两案ExpectedFailure空，严格失败原件保留，不以构建错误冒充断言RED。

GREEN051556严格PASS，0/1626ms：新policy12＋lethal7独立literal用例、旧8反例/终态及原TightWarning真实fixture全部执行。GREEN051629严格PASS，0/491ms：7个admission literal及原Task7边界/主异常保留合同通过。初始stub不在最终代码。

Main051657实际同一owned Burst `17372018641657`：

| 物理帧 | 原生收据 |
| --- | --- |
| 6814 | 首J将该敌HP2→1，绑定同实例回调 |
| 6868 | 该实例首个真实Shot，ordinal1；6879/6890继续发弹 |
| 6922 | 原危险/L释放后首次合法窗口，唯一K/cast11 |
| 6927–6928 | 同target17372102527655 Latched/Released，实际Stunned/application+1；消费者6928确认 |
| 6933 | 非abort guard handoff，玩家仍HP3 |
| 6953／6964 | 原jet实际接受一次／制动零速完整支撑，HP3 |

这证明post-shot接续成立，**不证明之后的致死J或完整Main成功**。K不是无故拖延：own Active/旧弹与resident Burst先占危险，6921正短窗时L仍held/active，到6922真实释放才发K。

6933后488条资源诊断无一次完整时长与原储备同时满足。原保存AttackCost=**12**、MaxEnergy100，双AttackCost储备为 **24**，不是48；此前发给审查者的“48”是根输入错误，已纠正，审查结论未引用该错误数字。6919 E23.86651、发K时23.066511；6933 Rust仅.13333315s，6950最大Safe.4<完整门约.43333334，6964 Safe.16666666/E16.266518。

安全时长单项达标段7003–7015、7107–7113、7146–7158、7320–7332的能量都小于24。晚ring实际7325–7332中Safe.5667→.45，但同期E12.133341→15.400011：可按同次复合门和同期标量归因其 `0x2` 失败，**不能写成末端单次CanAfford拒绝**，该时单次CanAfford诊断为true且 `0x2000` 未求值。7164 E24.266687时Safe仅.34999993，7177 E峰值30.33336时Safe.13333315。

最近96行7325–7420全outer_rejected/evaluated3/passed1/failed2/notEvaluated16380，无fault、无fresh分支；较早实际掩码已被覆盖，完整资源诊断不是它们的实际短路门。玩家7054被own Burst shot5伤至2，7078被resident precision-burst shot14伤至1，7420被其shot21致死，随后明确DEATH RESET。整案严格FAIL，0x20未在这96行执行，不凭纯合同绿宣称它实战放行过。

## 资源CONTROL：可区分，但本轮因果仍不成立

`KNIGHT_POST_PREFIX_CONTROL=omit-canyon-traversal`仅允许post精确字符串；null/空保持普通行为，pre/未知/空格/错误phase拒绝。实际runner只省略原 `PrologueCanyonTraversalTests.Run` 一次；其余prefix、原75顺序/名称、owner8、drain和失败清理不动。

CONTROL052107：engine0/80096ms、0ERROR；BEGIN/SKIPPED/RETURN各1，均明确original_post_accepted=false/behavior_accepted=false/resource_fix=false/whole_goal=false；普通PHASE_OK/TESTS_OK均0。原75/owner8、4个原WARNING/scope均完成。**原canonical helper未改，正确严格FAIL**；它不是合格post，也没有ExpectedFailure豁免。

根在控制后于finally清空该进程环境arm，再串行普通Explore/pre/post/models。普通post052919：engine0/195473ms，普通两marker各1、CONTROL三marker均0，原75/owner8/原4WARNING精确通过，严格PASS。八个最终候选案source-before SHA全部同一20DB…BBB7D。**两arm均quiet，所以没有旧遍历前缀的阳性因果证明**；不能因控制更快或某次安静就宣布owner/资源修复。

普通pre052611仍严格FAIL：engine0/157484ms，stderr/external各 **6条Handle ERROR**，stdout0，无WARNING/FATAL。全部 `SwapGCHandleForType→Node.AddChild→BasinSupplyRuntimeTests.OpenSample:463→CheckSupply:180`、continueSaved=false；首条Run:39 lifecycle=true/damage3，后5条Run:42 lifecycle=false/damage1。不是Continue重开或v119的StreamModel/Moth站点。实例创建和legacy配置已返回，错误在首次入树同步调用、该案伤害逻辑之前。

此普通pre的REF=false，root/assigned-profile/sector identity行均0，不能给出资源ID、rootID、managed serial或native owner。邻近Title清理只是顺序。旧REF Supply五root安静和v110 warm24叶/59资源不证明本次冷首次入树安全。下一需要同次零输入桥转换的实际native owner/对象ID、类/脚本或Resource路径与createWeak；若调试条件不支持，应保留证据缺口，不能用托管邻行日志或quiet重跑代替。没有GC/cache/全局pin/借用Dispose/引擎补丁或未经确认的native attach。

## Explore的独立实际结果，不冒称未实施的修复

Explore本轮从未启用Main post-shot策略，Traversal源码与v120完全相同（SHA A1F4186C4DD2B91F282428608AC1243AE4482AB0364C2C7212F264008921CCBA）。未实施x4979.925站位候选。本次单运行确实走得更远，但不能把未改代码的不同初始接触状态归功于新修复，也未证明重复性。

- Steam真实guard在8120，8121收据：contact1/transition56/fault0、3/112；8171出口53/112、HP2→2、jump12/dash0。
- Step8 accepted于(4999.1772,3563.925)，三帧尾端后Top入口(5009.1777,3563.925)/vx170、接触Step8。旧coplanar分支71/300至(4965.144,3563.925)、V0、完整胶囊/exactTop、HP2、jump20/nextcast16保持。
- 原preflight实际选中LiftBackLandingGrip，anchor(4716,3314.781)、352.3429距离；8510唯一K/cast16，8608在(4767.921,3423.9673)实际Landing接触/静止/HP2。
- Grip1自然收爪20/60＋原准备32，总52/60；8683真实K/cast18，8782落在(4463.868,3283.9885)、HP2/nextcast19。Grip2先10/40接近＋1/12停稳，完整Grip1支撑，再真实Space/jump20→21，尚未发K。
- 新FAIL：physics8799/input8771，HP2→1，`canyon.v2.resident.counterweight-lift.01`、`bone-wanderer.tscn` 的RustShellSquire/AttackHitbox，actor6165811434494/source6165878543362/attack1，玩家(4449.924,3239.9045)、冲量(89.39364,-167.65672)，盾未持/未激活、E1/heat0。原17/300 HP门拒绝，nextcast19不变。出口后仅这一条accepted damage，没有重现v120 ghost-diver命中。

伤害向量只证明接触时来源约左下方；尚缺准备/起跳前后的敌人/hitbox位置、同次attack阶段和剩余时间。Grip2 helper原本拒绝ShieldHeld，不能直接加S+L就声称保持原合同。后续需明确实际攻防输入所有权、释放和原300/HP/唯一Space/K边界，不补血、删敌或追加等待池。

## 全部十案，同冻结与严格结果

目录根 `G:/knight/验证证据/october13-stable-claw`，日期均20261026。RED父47523、GREEN父12136、最终父32233均TERMINAL0，分别只表示2、3、5个子调用结束，不表示子项全通过。

| 时间/阶段 | strict | engine / ms | 保留边界 |
| --- | --- | --- | --- |
| 051223 mixed RED | FAIL | 1 / 412 | 两新调度纯谓词真实断言RED |
| 051254 prefix RED | FAIL | 1 / 520 | admission真实断言RED |
| 051556 mixed GREEN | PASS | 0 / 1626 | policy12/lethal7＋旧合同/真实TightWarning |
| 051629 prefix GREEN | PASS | 0 / 491 | admission7＋原Task7合同 |
| 051657 Main | FAIL | 1 / 136537 | post-shot K/jet绿，后续混合火力死亡 |
| 052107 CONTROL post | FAIL | 0 / 80096 | 有意无普通marker，不算post资格 |
| 052256 Explore | FAIL | 1 / 163219 | 新骨居民伤害/Grip2原HP门 |
| 052611 pre | FAIL | 0 / 157484 | 6Handle ERROR，不被engine0覆盖 |
| 052919 ordinary post | PASS | 0 / 195473 | 原75/owner8＋原warning scopes |
| 053307 models | PASS | 0 / 520 | 普通模型/恢复构建 |

总计 **4严格PASS／6FAIL**，含保留的两RED及不授普通资格的CONTROL，不是玩家关卡通过率。10次构建0警告/0错误，ExpectedFailure空、guards0、SourceRows1844、无超时。各RED/Main/Explore原ERROR按stdout/stderr/external为0/1/1；pre0/6/6；两post ERROR0、WARNING0/4/4；其余0。三sink均无FATAL，但历史FATAL不据本轮quiet关闭。post原300000ms工作量预算保持，不追认旧180000ms或称硬件性能。

## 一次新鲜审查及全部根裁决

`/root/v121_postshot_control_review`认证4文件物理基线及最终hash：**Critical0／Important0／新增Minor0**，根依实际影响复核同意；只保留限定测试增量，不批准整脏分支、合并/发行/Task15/目标完成，无修复轮或第二次review。4项继承Deferred：energy-unwind早失败flush、swept边界措辞、finally stage4 handoff摘要、fresh late事实继承outer mask/dxdy来源标签；仍未修复/关闭。

以下覆盖审查全部17条Declined，根逐项裁决与错误代价：

| 事项 | 根裁决 | 若错的代价 |
| --- | --- | --- |
| 1 完整双路/Task15/十一项 | 两路FAIL，原范围ACTIVE | 把实际死路冒作交付 |
| 2 K后致死J | 只认实际K/jet，不称J完成 | 漏掉仍无法结束的战斗 |
| 3 Top/返程/ghost | 未实施几何候选；本次不同接触态不算修复，新增bone伤害保留 | 将一次波动当稳定安全路线 |
| 4 资源owner/修复 | 两arm quiet无阳性因果，pre6ERROR开放 | 未定位的崩溃/寿命风险漏报 |
| 5 最终同冻回归 | 已补八案同source；完整pre/Main/Explore仍失败 | 局部测试绿替代完整验证 |
| 6 全源保管 | 已独立50对/4946副本/60sealed核验，只界定本轮 | 将未审历史改动归因本轮 |
| 7 重复性/硬件/频率 | 一次局部序列不认证重复性/FPS | 对现实玩家稳定性过度承诺 |
| 8 W/排名/first-hit全面 | 原production未改，无二次resolver放行，不全面续签 | 陈旧/遮挡问题仍可能存在 |
| 9 全部Intro/准备流程 | 保留原Ready/早拒绝，未扩大测试 | 未覆盖流程竞态 |
| 10 回调/销毁/延迟故障注入 | 原合同保持，非全重入证明 | 特殊异常清理仍可失败 |
| 11 LoadLevel双异常注入 | 本轮无loader改动，不称重新验证 | 主异常/清理异常可能仍有边界 |
| 12 早诊断分支与env组合 | storage-only/catch-only在普通phase前返回，不走新skip或发post marker；未授组合资格 | 把未认证混合诊断当普通post |
| 13 编排env恢复 | 根finally清空，普通post控制marker0/普通marker1提供实际证明 | 继承控制arm误跳普通前缀 |
| 14 四项旧Minor | 全继续Deferred | 日志/失败摘要仍有精度问题 |
| 15 全76/BossNPC/地图机关/正式AV相机 | 原稳定ID全保留，未勾整项 | 遗漏批准内容或伪称正式美术完成 |
| 16 真人/首读/审美/硬件输入 | NOTSEEN不变，headless不是人体验收 | 玩家难理解或看不见的问题被忽略 |
| 17 脏分支发行/UE/真存档/付费/自动化/删除 | 不碰，不纳新资格 | 跨项目、数据或费用副作用 |

执行Ruling：显式替换v106内部强制早K，而非弱化真实一次K收据；代价是新的相位/位置未必存在合法J，实际失败照留。Ruling：早OR后机会持续欠账、0x20与fresh/终态同时约束；代价是没有合法后窗会失败而非静默跳K。Ruling：控制只略一个原调用且不发普通成功marker，helper不改；代价是即使quiet仍为严格FAIL，不能以其缩小正式范围。Ruling：只复用同源已结束Main，不重复凑绿；代价是不认证重复性。Ruling：用v120完整source清单认证更早Task7实物快照，不因最新case没该副本就丢失基线；代价是必须逐hash匹配，不能靠旧文件名。

## 独立保管与最终身份

`/root/v118_custody_audit`明确 **v121 audit ENDED**；末命令ac40cd同步exit0，无运行handle/子会话；20个记录native结束，CIM KNIGHT Godot/dotnet/owner各0。根父handle由根实际终态证明，审计不冒称跨会话检查。所有文件在rootowner/批次间隙及审计运行期间冻结，ENDED后才写本次最终文档。

- 五对manifest **50/50逐字节一致**；实副本4946（source4836/helper110）逐份SHA匹配，missing/extra/fault0；sealed binary60/60一致。
- 当前Source1844/C#892和Debug6/6匹配最终models，漂移0。对物理v120只有4测试C#的121+/18−、0源码增删。
- Task7旧实物为`post-boundary-contract-20261024-022017-695-24f8ed0d45914b6ebacdbcf7f6afb8b8/before/tests/unit/Task7CaseRegistryTests.cs`，SHA `A0576D60BE5CD60408FCC4603F63B750BB4E4E45C7FFC2CC1111C165BEF1061E`与v120模型清单第1791行完全相同，其余3基线在v120 post045202/before。
- helper联集12条各单版本、当前均匹配；额外ranged helper本轮纳入保全，不是本轮新建/改写，SHA26C0F390BB1D2FFD7957DA3246A86762EC201A6EB8393AF4A88C103FD7817824。owner仍886AEA3D4D90054D4C567F8527F15C61DCE12D23573E08286FFFBF3BDE284C70；HEAD/branch/index/audio/productionClaw不变。
- 20份既有user manifest同SHA BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB；未读取真实存档内容。

最终source `20DB7420F4D683BEB79F3982546C7348D6319E17075A0CD0D16937BAADFBBB7D`；binary `0913DDC04ED36968D8DD188C2094C0F22E800D1A2DCA2387CE53329755F82E89`；helper `A1AC76A1CBEEAF2061E4787C7AA4191B2FE6831CCBD6640BC2EE66C2E403B8F1`。当前Main EF2EADDB0D9A201DE448EC0C88A225BD15F37AB6F88763711021774C7B3E2906；Placement E6572091861BA05BE3F0E7FEF445691E1B0CD95ED29A6D537210778DF7F08762；RunPrologue4BEB8E296B71656AB5AF5EC13C31E083C077E5B4CD5D964AB695A3209CD11A17；Task7Tests1E5E7DB910FA27731F5EDE045C94F32D12A0CDD150931568F32E837A982A85C7。

## 后续与完整交付边界

下一处理真正降低外来持续防御负担的合法走位/压制，使完整反击时长与原24储备在同pass交叠。若允许后续实际K，应先在stage4冻结首K精确单edge/sample/cast/target/application/Stunned/HP/handoff收据，再为后续每次K独立记账；不能简单把整个战斗edges==1改成>=1，也不能降储备或删敌。该后续尚未实现。

Explore下一先取骨居民同次准备/起跳攻防事实与输入所有权，保留单次Space/K、HP与300；已有Top/返程不因单次顺利而关闭。资源下一在不改资源策略前取得实际native owner；本轮没有进行native attach，条件或权限不足时保留缺口并继续其他安全制作，不猜pin/GC修复。

没有新增正式美术/声音、游戏图稿/实录、前台/OS输入、付费、自动化、Git写入/提交/推送、删除或真实存档语义读取。资产表无新增正式项。本轮唯一手动媒体Check UTC2026-10-25T21:42:35.9472051：Current860284/基线3508/增长856776字节、Notify=false，无新20GiB档位，不停工、不建独立通知。

完整原十一项、全部76普通+Boss/NPC/机关、正常同版双路、死亡恢复继续/出口、正式分层美术/动作/UI音频/批准演出、相机正常速度审阅、目标硬件/真人键盘及授权包装GitHub仍须逐项真实闭合。FTR/取消/另一UE排除；测试通过不等于视觉或整关完成，不标Task15或目标完成。

原始证据：[Main日志](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-051657-838-57210c0c208841c88b73e5caed4dbc15/engine.stdout.log)、[CONTROL来源](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-052107-132-240d25981a54483399906cbbabae156f/provenance.json)、[普通post来源](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-052919-398-739a42cbb37e4b618a35b206b1d63f91/provenance.json)、[pre原始错误](G:/knight/验证证据/october13-stable-claw/prologue-pre-20261026-052611-993-d073c707b63b491bb818796e209c811c/engine.stderr.log)、[Explore日志](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-052256-247-77a40245119944daa514fb47ec22507c/engine.stdout.log)、[最终models来源](G:/knight/验证证据/october13-stable-claw/models-20261026-053307-997-19c7ae795a804aa394bcdb65c770350b/provenance.json)。
