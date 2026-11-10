# v210 · 来弹守向预测接线与精度修补，主线仍未清

## 事实、推理、风险

**事实：** 仅改现有Main与RangedPlacement两份测试CS。生产角色/敌人/地图/数值、原900、HP、精力、J/K、单跳/三弹/恢复要求均未改。原十一项目标继续ACTIVE；v210不是发布版本。

**推理：** v209已证明当前正clearance不能排除下一步来弹。现在仅在已完成postHop K3/Hop5的两次原扫描中，接入共享纯值历史及完整胶囊相对扫掠预测；Unknown/Incoming阻止更晚近战预警接管守向。原Active排序、Threat、SafeCounterSeconds与攻击门完全不借预测放宽。

**风险：** 这是自动试玩驾驶，不是玩家生产玩法修改。下一步恒速估计不是native TOI或未来运动保证；当前生命值未降、无source的ShieldResolved和退休仍不足以认证Burst17确切Guarded0。完整Main及原资源/正式视听/真人/硬件等质量门没有因此通过。

## 测试先行与接线

- 纯值几何包含线段相交与四个端点到对方线段距离，保留完整胶囊中轴/端帽、世界半径、相对角色速度、旋转/缩放与退化轴；无新增空间查询。
- 历史绑定actor/projectile/leaf/attack，只有相邻physics、递增sequence、相同dt/radius才计算；首样本、缺帧、重复/倒退、身份或配置变化为Unknown。历史只存标量，不保留Node/Resource。
- 27个独立字面量控制在stub下真实RED，随后GREEN，含实际Burst17反例；未接线GREEN不拼成最终接线版本资格。
- 政策在原Consider内、诊断回调之前运行，未消费passive observer的副作用。已精确departure-excluded的旧弹沿原规则，不借17处理情况忽略18或整个owner。
- 旧调用默认null；只有真实postHop Main outer/fresh-J传共享history。非线性/未知projectile保持Unknown，不伪造预测。

## 唯一审查与一次C/I修补

唯一fresh whole-change review **C0/I1/M0**。I1为世界坐标float终点舍入误判相切：capsule比例1.0002、中心X4635.4018555，circleX4629.40185546875、移动2.9995999336242676、半径1，正确为inclusive tangent，但先构造float世界终点得到错误Clear。

先加入该独立literal反例，d4b28400真实仅这一项RED；再把原始标量先提升为局部double，端点/变换轴/相交/距离均在局部double计算，6d27db38的28控制与旧实际混战fixture严格GREEN。没有加epsilon、放宽门、第二修补轮或二审。最终完整回归使用修正后的冻结源。

## 首次实际Main：生命保持、再多一刀，但仍FAIL

2b496058 native144305ms严格原900失败，玩家HP2，剩余RustHP1；旧v209对应HP1/Rust2。这是实测进展，不是完整通关。

- physics6921：Unknown1/Predicted1 veto生效，保持Left；6922允许Rust预警接管（仍余0.116666496），6923实际Right。
- Rust8真实回执：6929/原loop769/seq6900，transition61/epoch3/HP2，normal zero与真实Hitbox.Connected配对；6930/loop771实际返Left，原平台完整支撑。Rust9在7016/loop856另有回执。
- Burst18仍在6930/6932作为Active来源。无source的零伤ShieldResolved不能冒认18或17精确回执。19仅在unwind7063才发射，晚于原900结束7061；预算内只有其Telegraph，不计作已活弹扫描。
- 最后J命令7025/loop866，原outer/fresh门16383全通过，能量30.13325/clock0.64999896；实际Connected为原loop868，计数7。没有独立逐条Rust Connected物理时钟，不能把推定7029当独立事件收据。
- 命中后18.13325能量未回到原reserve24。剩余31决策中，869–873首败counter-window；874–899先败strike gap<8，后续门未求值。不能把短路未求值项计为通过。
- 完成的mixedBrakeStage4仍将signedGap<8时A/D压为0，覆盖原too-close退让；Rust走过玩家后末gap1.505。下一应核这份完成后移动约束的失效边界，同时保留精力/时间约束；不能保证只恢复退让就清原900。

## 最终验证记录

SourceRows1872，原build180s/policy55s/Main600s/pre180s/post300s；root串行，运行时source/helpers冻结，无ExpectedFailure/fixedFPS/movie。

| 案尾码 | 阶段 | build ms | native ms | 严格结果 |
|---|---|---:|---:|---|
|44de9889|初始policy stub|28692|882|真实RED|
|4535ec31|纯helper27controls/旧fixture|24954|3866|GREEN，未接线版本|
|2b496058|接线Main/精度修补前|29383|144305|原900 FAIL；HP2/Rust1|
|d4b28400|I1反例|27348|1256|仅translated tangent真实RED|
|6d27db38|最终policy/28controls|29950|3661|严格PASS|
|a521739e|最终精度源Main|29228|133079|原900 FAIL；HP2/Rust1/7次命中|
|d25d295d|最终actions|25910|9412|PASS|
|c8c12c80|最终keyboard|30173|20145|PASS；headless title8079亦PASS|
|3206d313|最终models|27142|510|爪模型子集PASS|
|df3591da|最终pre|27351|162330|native退出0/phaseOK，但5 Handle ERROR，严格FAIL|
|e43a7c1d|最终post|26414|199829|原300000内PASS|

全部旧失败/基线保留。当前Explore未重跑，不继承旧探索通过资格。

最终Main重复同样的局部进展和原失败：玩家仍HP2，Rust1，signedGap1.505，原frame899；7次J全部Connected，同LiftBackLanding；原Hop5/一次Space/三发passed3/无恢复保持。Rust累计contacts4/returns_left2/faults0。音频本案5frame/267ms、persist109保护通过，不扩成资源稳定或整体通关。不能把精度修补前Main的具体来源ID借给最终Main，身份须按各自原始日志核对。

11次尝试为6限定PASS、2具名真实RED、3FAIL（两次Main、一次pre）；最终同源7阶段为5PASS/2FAIL，整套未绿。全部构建0警告/错误、原caps和严格错误门不变。post75有序同名/index边界、owner8/nodes1→1、finalizer/drain/phase齐；仅原4限定负控WARNING，无ERROR/FATAL。

pre的5次`Handle is not initialized`来自GCHandle.FromIntPtr→ScriptManagerBridge.SwapGCHandleForType；首个可见调用链为BasinSupplyRuntimeTests.OpenSample:525的AddChild→NewGameDuringHealingCannotSpendTheNewRun:360。进程0与phase完成不消除这些错误；不猜受影响对象或根因，不重复跑到无错、不增加GC/超时或更换引擎。

本轮用户另补三条正式质量反馈：角色像拼贴/缩放/突兀跳帧、双腿平移缺乏设计感，惯性/动态/物理缺少可感知表现。已纳入U-20261103-01及[动作与物理返工要求](2026-11-03-character-motion-rework-requirements-v001.md)、原M/V条目。本轮没有实际完成这项美术/手感返工；技术通过绝不抵消用户观感反馈，也不把Spine参照当工具采购授权。

## 裁定与未验范围

1. 保留dirty状态、所有失败与基线，沿用manual ledger，不Git/worktree/commit/删除；代价是没有clean-release资格。
2. 既有持续批准范围内inline/headless修测试驾驶，不新增GUI/OS输入、付费、自动化、运行时、真实存档或发布权限；代价是不构成玩家玩法/视觉验收。
3. 预测只作方向谨慎，不作安全clock/TOI/HP豁免；代价是必须由实际原生结果验收，预测Clear不保证未来安全。
4. 精确Burst17 Guarded0未取强身份配对；不搬代理receiver/新Node/事件进生产，代价是该子回执仍OPEN，不能由HP保持代替。
5. 静态审查不替代实际Rust方向/18/原900/完整Main证据；原900严格失败保留，代价是没有整路资格。
6. 所有最终回归在精度修正冻结源运行，不借旧helper GREEN拼接；代价是任何新失败仍须报告，不能称全绿。
7. 局部double修正通过真实反例，未宣称任意世界尺度或精确native接触时间；代价是仍需普通硬件/实际运行质量验收。
8. 无关dirty、历史pre/Handle/Audio、正式美术动作UI声音、普通显示真人/硬件、双路和原十一项未认证；代价是不能正式交付或保证付费人数。新增Minor0；旧延期不因此清除。

## 最终保管

独立增量审计累计11案55清单对、5448份选中源副本、126份helper、66份bin逐SHA一致。当前1872源、post专属10helpers、6bin与最后post零差异，最终7案同源同bin。2026-11-03 01:06:25 +08按保存creation/path核23owned（11build＋11engine＋1title）已结束，旧PID当时均不存在，KNIGHT工具进程0，无进程操作。

sourceManifest `04CCEEE4CCF3D7147AE20600AE255D1D0A3FFD652FD21E1828E78A9BA5C35F3A`；binaryManifest `82A31DA8A4E65BB391DB6041F8BAED4CD0731312D7173B72CE0AF7A7FF77057A`；DLL `ECB1371AAD862F9D83EC6C9560BD9F02AF00A9BAAE5E0FDC9D488B5DD596D397`；postHelpers `C5EF45802948AA866E2659414BDBE4BD8D22B0D980D459CAA292638C213429E7`。

Main `D7548A3C20BE92650FB4D625590BA030703A22D2B30A09888A34DE46A1867A49`，Placement `32E9B259D944CD121EB40A3EA4AF32EFB432143F9CAA1A16ACE02F1AD7F4597E`。Actions3D72845F/Teardown26EF0C40/canonical7A79C29A未改；HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`保持原值。diff-check无空白错误，仅LF→CRLF规范化提示。

pre的前3条错误处于newgame_reentry挂载，后2条处于title_reentry挂载（后者调用ReturnToTitleDuringHealingStillPersistsTheClaim:433），仍不推断未证对象/根因。Main、资源句柄及精确Burst17回执仍OPEN；原十一项目标ACTIVE。

媒体手动检查UTC17:03:11为7,427,750bytes/86files，Notify=false/NewThresholds0。没有新增过程媒体或自动化/停工，用户反馈只保存要求文档。原始失败仍保存在G:/knight/验证证据/october13-stable-claw各具名目录；没有付费、前台/OS输入、额外dump、引擎替换、真实存档、Git发布或资料删除。
