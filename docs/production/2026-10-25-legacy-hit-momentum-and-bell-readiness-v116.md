# v116 · 旧式机械爪交互惯性修正与齿轮战斗事实

状态：一项生产物理缺陷实际RED→GREEN；完整序章与Task15仍未通过，原十一项继续ACTIVE。唯一工程仍是 `G:/knight/Knight`，不涉及UE。没有新游戏窗口/系统键鼠、正式图声、付费生成、自动化、Git写入、打包或删除；用户最近游戏显示结论仍为NOTSEEN。

## 1. 已修正的实际生产问题

批准的旧敌人/机关交互是“命中→效果→收回”，不把玩家拉入内部。原 `MechanicalClawController` 在这一分支从未安装真实tether，却在发布Released后调用通用地形释放投影，按点目标的瞄准法线改写caster实际速度。

只改该成功legacy分支：保留原authority/current-frame守卫，使用 `ClearGrappleConstraint()` 解除施放期约束，不再应用不存在的地形挂接释放冲量。真正地形释放/Boost/速度上限及安全投影路径不变，未调AI、地图、血量、能量或短喷参数。

真实TDD使用保存Player与保存BellChain、自动Input/Motor/Claw、真实A/D/K：

- 夹具仅在入树前布置，运行中无Player姿态/速度/外力写入、不关闭Motor。Away案A十次采样后真实D一次采样恢复Right；这一次有限反刹如实计入入射速度，不声称命中前全程未制动。
- K按下不是命中。-110与-75在adapter application0→1的同physics/sequence/cast/authority命中tick夹取速度；Opened/Latched/Released各回调运动也保持不变。真实碰撞、反向键、跳跃、短喷、伤害均被排除；恰一次副作用、Latched→Released、无持续挂绳。
- RED225246：Away命中前后 `(-130.53621,780)` 相同；Toward在physics1381/sequence23由 `(186.44337,678.3334)` 变成 `(-14.795761,673.91516)`，恰与旧通用释放投影一致。失败的是独立“不产生未授权caster刹停”断言，不是夹具前提或编译错误。
- GREEN225516：同Toward速度两分量均保持；原33项与接管测试仍完成。
- 最终232040再加入+100自动Motor观测，证明不只是发布瞬间的表象：随后实际速度 `(181.53726,709.1667)` 与空气指数阻力/重力/终端下落速度的独立算式一致，实际位置 `(65.1379,-358.16663)` 来自原生积分。Away也通过。没有调用被测Step/forecast函数来制造预期值。

上述只关闭这项已复现的无实际挂绳速度改写，不认证所有receiver、所有速度或完整物理系统。原33收据保持原范围，新增两案由原thruster-player入口在原33与接管测试之后另行执行。

## 2. 正常路线确实改变，不能强迫一次受击

新物理的首个Explore225946仍通过原齿轮反制189/210，但乘梯没有接触Bell攻击、HP保持；v115新加的“必须有一次左侧接触”断言因此失败。不能为了保留一个固定测试时序而改坏惯性、移动敌人或强迫角色受击。

消费者更正为：原受伤/身份/原Bell保留/200帧/完整甲板与其他预算不变；所有实际Connected都须真实挡下，或者零Connected时还须有正数Active采样对、连续相关物理边界、保存攻击/受击形状启用、无包围盒重叠/故障、HP不变的证据。可选策略名改为verifyRideDefense，仍仅原Explore分支启用。

230648与最终232422都得到13对连续Active边界、0重叠、0禁用、0Connected、fault=none、HP1→1，随后画廊与返回96/109成立。只读取实际Shape矩形与变换，额外DirectSpace查询/控制/等待帧均0。

这是**配对原生边界采样的包围盒证据，不是连续子步运动无碰撞证明**；也没有伪造新的左/右命中正例。真正接触仍由原Connected/盾active/Normal正伤害/HP不变共同证明。

## 3. 反击为什么一直没有开始

被动诊断只包住原 `GearBellNeutralSupport` 的已有求值，保留短路顺序及副作用调用次数；evaluated/passed/failed区别未求值与false。最多24种唯一mask，另留最新标量和既有energy回调中的首次过热转换；不补调用support helper、resolver或物理查询，不改900帧、AI、HP、支撑门或输入。

最终Explore232422仍在physics5741被同case原wave Bell的attack10击杀；不是旧5740/5698收据回填。新观察共6种mask、fault=none：

- 首次过热physics5618/sample5589：Energy44.599583、Heat100、regen delay0.55/cool delay0.35，raw/sample L=true、active=false。
- 最近判断loop780/sample5588：evaluated1/passed0/failed1，即第一道完整支撑门为false，后续中立/距离等条件短路未求值；不是“所有条件都失败”。player x2812.7097、Bell x2752.4114、gap60.29834、速度0、support0，仍有威胁。终态最新判断loop899也如此。
- 第一失败门与过热已经有实际记录，但它们不是完整因果矩阵，也不能把floorId0单独解释成没有原生接触。

独立只读几何核对：Bell x2752.4114时，原lift完整40px胶囊中心只能2720–2800；左侧55–64的站位为2688.4114–2697.4114，右侧为2807.4114–2816.4114，两侧交集均空。原60px目标在完整支撑区外，不能靠调刹车解决。55–64来自旧驾驶策略，不是生产K射程或用户的硬性要求。

### 下一次未运行的最小候选

优先保持固定PistonTwo完整支撑 `[2864,3020]`：真实走向约2897并刹停，在lift升到合适高度且真实LOS成立时，让原70px/s Approach/90px GuardRange把Bell带向约2807；再真实回到约2867的合法近战站位。仅实际production preview、同target/cast/application/native Exposed成立后才K/J，不改AI或移动敌人。坐标是当前地图推导，不是可达证明；原900预算、实际支撑/全部威胁/刹车/六次伤害/敌人HP仍需验证。

原6HP/六次完整J至少1.8秒和72能量，诱导约55px理想至少0.79秒；这些只是下界，不保证15秒内可完成。上方右棚距头部约20px，不能把无障碍Jump/短喷平抛当作可行轨迹。远距K确在生产620范围内，但同样不等于preview/LOS成功；重复K也不会刷新同一次Exposed。先验证较小的固定平台诱导候选，而非盲喷或放宽full-support/55–64。

## 4. 资源错误再次出现，且坏参数可进一步收紧

最终pre进程exit0/161256ms，但stderr **7条Handle ERROR**，全部是 `FromIntPtr→SwapGCHandleForType:1284→AddChild→BasinSupply.OpenSample:463→CheckSupply:180` 的第一次Open：前2条属于Run39/lifecycle=true/damage3，后5条属于Run42/lifecycle=false/damage1。不是旧256重开路径，不能混案。

最终post进程exit0/203323ms，但stderr **4条同类ERROR、0 FATAL、4 WARNING**：

| stderr起始行 | 实际最深业务路径 |
|---:|---|
| 1 | PrologueSectorStreamModelTests.GeneratedLayoutOneSceneKeepsGeometryAndAddsExplicitNeighbors:67→PackedScene.Instantiate<T> |
| 45 | 同测试:71→PrologueSectorStreamModel.ctor:27→Definition.Validate:70→ValidateSceneRootTransform:121→PackedScene.Instantiate |
| 191 | GraveLampMothRuntimeTests.FormalScenePreservesGameplayAndReplaceableVisualContracts:257→PackedScene.Instantiate<T> |
| 246 | GraveLampMothRuntimeTests.RealPhysicsLocksSweptPathThroughLandingAndRecovery:34→PackedScene.Instantiate<T> |

post原75项executed/classified75、retired0、150 enter/return，以及owner8次nodes1→1的末收据均完成，但不覆盖stderr严格失败。原四WARNING仍按三个既有scope对账，未增加豁免。历史FATAL未因此关闭。

只读本地真实DLL/PDB/IL得到新事实：

- 桥签名为 `Godot.Bridge.ScriptManagerBridge.SwapGCHandleForType(nint oldGCHandlePtr, nint* outNewGCHandlePtr, godot_bool createWeak)`，token0x06009B41；没有native owner参数。匹配Debug portable PDB GUID为b0889cba-af46-4b0d-b96f-c9afeeeadfeb，含源行1281–1310的局部名，无嵌入源码/SourceLink。
- 当前9.0.19 CoreLib版本9.0.1926.36724，FromIntPtr token0x060070DF调用ThrowIfInvalid token0x060070E9；后者只有输入0分支抛HandleIsNotInitialized，构造器仅赋字段。桥IL直接传oldGCHandlePtr。因此这11条异常路径的旧参数**必为0**，这是唯一分支的必然推断，不是寄存器现场采样。
- native owner、为何传0、是否释放/初始化问题仍未知；11条不等于11个不同owner，不能据此声称GC根因或忽略0输入错误。
- 本机有VS2022 Community17.13及NetCore调试组件，但未启动/attach；未发现现成cdb/SOS/dotnet-dump/ClrMD，native引擎无已发现的匹配PDB。未来隔离断点可尝试在catch之前取参数，但零句柄未取得target时仍不能自动映射owner；outNew地址也不是owner。没有改引擎、资源持有、GC策略、服务或调试权限。

本次核验的运行库SHA：GodotSharp.dll `20D411FE2703334D855F0493596B29FBA3BEBE28C20D6DBDA55681A2B93AFD3D`，GodotSharp.pdb `993647C429FF34471B9544E7CD2742D3D11C730B13C0672A6D8E35EC91352106`，CoreLib `18DD52B72D7726914249749376C2E635103AB25D7F4C5D37BB3A033166F2DF0E`。均在 `G:/knight/Tools` 的现用版本下，未被修改。

## 5. 十案实际结果

所有目录位于 `G:/knight/验证证据/october13-stable-claw/`。十个build均exit0/stderr空；所有build/engine非timeout、guards空。只有最后六案是同一最终冻结组。总strict3/10，最终组2/6；batch16132 exit0仅代表批次结束。

| 实际目录 | Engine exit / ms | strict结果 |
|---|---:|---|
| thruster-player-20261025-225246-033-d3aec63b603f4579a97166c548abc5a1 | 1 / 23484 | 动量RED，FAIL |
| thruster-player-20261025-225516-136-137cb6435f3a44cda2f56689d256878a | 0 / 23438 | 原33/接管/新两案PASS |
| thruster-route-exploration-diagnostic-20261025-225946-428-6147ce9647b8456d9bd62b599d1073ec | 1 / 86836 | 新物理下旧强制接触门FAIL |
| thruster-route-exploration-diagnostic-20261025-230648-139-a8e1b032cbbe478d8335acbdcc42fe2e | 1 / 109493 | 分离/往返局部通过，后续5740死亡FAIL |
| thruster-player-20261025-232040-883-f8e5b9afa134461c9b4a1c0ef50eb6e6 | 0 / 23497 | 最终原33/接管/命中后实际积分PASS |
| thruster-route-main-diagnostic-20261025-232135-903-d140b248e0dc40499e90fceede713303 | 1 / 136253 | 最终Main旧死亡恢复/3117.98px位置断言FAIL |
| thruster-route-exploration-diagnostic-20261025-232422-916-d8fd4d133a40489fa2cca16feab43da7 | 1 / 114524 | 最终Explore5741死亡FAIL |
| prologue-pre-20261025-232652-340-2e8cd52cedae4c02aa01f7ca350da1c0 | 0 / 161256 | 7 Handle ERROR，严格FAIL |
| prologue-post-20261025-233004-540-48e45e11489f40c685dd0684400e61ff | 0 / 203323 | 4 Handle ERROR，严格FAIL |
| models-20261025-233358-919-a17170bf6cc0493aa7dfc3a2723c014d | 0 / 508 | 最终普通Debug/models PASS |

post实际cap300000ms，不等于旧180000ms通过；任何上述原生耗时也不是硬件FPS认证。pre覆盖原MechanicalClawRuntime、ThreadInteraction和Traversal合同，但含ERROR不得称整套回归通过。本轮没有重签全AutoPull独立组合矩阵。

## 6. 一次审查、两项延期及十八项root边界裁决

一次fresh review核验五个实际旧实体/当前实体及全部增量：C0/I0/新增M1，没有纠正性修复或第二次审查。所有最终测试真实执行后仍保留失败，不调用task-done。

Deferred minors：

- 新增：Basin失败信息仍写“proves swept attack separation”，比实际配对边界证据更强。相邻代码注释和本报告已明确不证明连续子步；它是剩余诊断文案问题，不是新玩法缺陷，本轮不借机改Minor。代价：读失败信息可能过度理解证据。
- 继承：energy-unwind提前异常可能丢汇总，仍延期；新first-overheat/latest记录不算关闭它。代价：某些早失败诊断信息不足。

以下18项对应审查者全部Declined-to-judge，root逐项裁决及代价：

1. 最终冻结：按六案真实结果，只有player/models严格PASS。代价：误把退出0或批次0当全绿。
2. 正常双路：Main/Explore仍FAIL，不关闭Task15。代价：跳过真实通关障碍。
3. readiness/过热：只采用新原生mask/首过热与几何事实；后继战术仍未运行，不宣布完整因果已闭合。代价：错误修正站位/热量。
4. 连续子步分离：明确未证明，不从包围盒并集外推。代价：虚报碰撞覆盖。
5. 新左右正接触：当前零接触，不复用历史正例冒充本版双向命中。代价：漏方向边界。
6. 其他攻击/交叉火力/异常几何：保留各自验收，本次只一只保留Bell的ride。代价：遗漏组合失败。
7. 所有legacy接收者/速度：真实新夹具限BellChain和低于cap速度；现有Moth路线是有限补充，不声称穷尽。代价：其他业务交互仍可有问题。
8. 高速地形释放/Boost：生产路径未改，最终原生pre含原相关行为但严格FAIL，不能整体认证。代价：未测组合漏项。
9. 全局W/preview排序：未改排序，只用实际输入/目标证据，不重签全方位。代价：其他方向仍可失败。
10. 全first-hit/receiver资格：无新增授权性resolver，不借局部重签全部遮挡。代价：非法目标/遮挡漏检。
11. 全Intro→Playing：准入未改、原门保留，不重签所有夹具。代价：准备态假通过。
12. 全callback/deferred矩阵：保留authority/frame护栏和原33；新case不覆盖所有重入，旧独立合同仍需维护。代价：卸载/替换回调漏项。
13. LoadLevel自有清理/双异常：未改，不以局部解除约束替代。代价：丢主错或泄漏根节点。
14. 完整诊断历史：明确24唯一mask、最新快照和首次转换是有限记录，未求值不填false，fault明确输出。代价：遗漏中间历史。
15. 继承unwind Minor：仍延期，保持FAIL，不宣称补齐。代价：早失败缺诊断。
16. Handle/全部内存/FPS：新11条零输入推断缩小未知，但owner/根因/FATAL/总体安全仍OPEN。代价：盲保活、过滤错误或虚报性能。
17. 全树/二进制：以另一次独立全量审计补足，不能由五文件窄审查代替。代价：跨版本拼接。
18. 原十一项/正式视听相机/硬件真人/发行：全部继续ACTIVE/NOTSEEN。代价：提前交付未验作品。

其他root裁决：维持用户选定dirty codex checkout/唯一写入与原生拥有者，依靠快照归因，不创建worktree/新BASE/提交。物理修正后先运行真实Explore而不复述旧失败时序；强制一次接触不是用户要求，消费者仅在有额外事实时允许成功避开，不改敌人/HP/预算。最终原生、审查及保管审计结束后才写文档；代价是未完成项目仍不可发行，不以本报告替代任务完成合同。

## 7. 实物保管与当前文件

独立只读审计已结束：10案50对/100清单、60个封存二进制、5106份before/after复制输入实体全部对应，missing/drift/malformed/duplicate/extra均0。final6 source/bin manifest完全同冻；当前1844源/C#892及Debug6/6逐实体SHA一致。20组native PID/contract argv及wait/provenance对应。各案user metadata109、continuation0，未读真实存档正文。

相对v115只改变五C#，无add/delete：一份生产Controller及四份测试/入口。旧实体按实际存在的副本认证：Controller/Main取v115 `models-20261025-222400-844-4fd7603527474317a845e82478cd3475/before`；Basin/Core入口取 `thruster-route-exploration-diagnostic-20261025-221425-285-14f132a42fa2463794a20c255a7e871c/before`；ThrusterPlayerRuntime取 `thruster-player-20261025-053648-417-1116255e15e94d6a965737ba8d9e5562/before`，SHA7B7A60CD0F0A8EB4115CCC91D0883611B51B787249343B7FB749634FD9A97DC6与v115 manifest一致。缺少的models复制件没有被当成实体。

- 最终source manifest：`64D89F1C6D08B880960F36714DEB516C705597DB4810E9A7AF6A920D44BCDFDC`。
- 最终binary manifest：`403DEFA8CCF411934D9E462B2C4F8488A2D8C11223BCFFC62973D986EBB8E68B`。
- Controller：`6DAF77B293D1F7F04FFA05E31990143E4BED8D82B463B0BFFF492C009CC3F4AE`。
- Basin测试：`664E054102CEAB666170488F0560BA2BA42D7B2E9BED823AD54B7DF217B7CB32`。
- Main测试：`F0B8C909AAE8D463F7C6EF4C49A3C06CABC92D9CE63D282E8A3E1ED2259F43DF`。
- Thruster测试：`878F161095706B2CD564519320CAA36D7A73506B3F7F2398DCF4B74EF3589C3D`。
- Core入口：`681C1F1F8AD515DAF946083F9381C622AE0C328CF0B6B7CCF6585EC63FC73FF3`。
- owner：`309DD6C42FB5E96F27390711FF91D2D419890CF17BA256869E2D2A22961255F2`。
- index：`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。
- 音频布局：`CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`。
- HEAD：`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`。

所有helper/工具与v115对应案完整一致，所有自有Godot/dotnet已终止。仅一次既有手动媒体检查UTC15:46:08.9998154：Current860284、Baseline3508、Growth856776字节，Notify=false，没有新游戏媒体；20GiB规则不阻塞工作、不重建自动化。

文档写入后再核验：报告十案路径/exit/ms/timeout/guards与实际provenance一致，strict3PASS7FAIL；最终Explore13对/0重叠/原HP/96帧返回/attack10在5741致死均直接重读。五C#、owner、index、音频及最终source/bin manifest完整SHA不变；全局diff-check exit0，原LF→CRLF提示170、其他诊断0，自有Godot/dotnet仍0，HEAD与完整目标ACTIVE不变。

下一步：原900内真实验证固定PistonTwo诱导/合法近战站位；资源调查保留零参数的新证据，后续需要前台调试时段时再询问。原十一项稳定ID、76只普通敌人＋Boss/NPC、地图机关、正式分层美术/动作/UI音频、正常双路/恢复/相机/目标硬件真人/版本交付均不缩减、不据局部测试勾整项。
