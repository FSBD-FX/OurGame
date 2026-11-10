# v122 · 外来压制、真实反击交接与默认计数边界

完整原十一项目标仍 ACTIVE，Task15 未完成，真人显示确认仍为 NOTSEEN。本轮是自动纯键盘试玩消费者的修正，不是生产技能、难度、地图或正式美术升级。唯一工程继续为 `G:/knight/Knight`，不涉及另一个 UE 项目。

## 事实、推理与风险

事实：对物理 v121 基线，仅两个测试 C# 改动：Main 252+/2−、Placement 98+/0−，合计350+/2−；源码文件增删0，Source1844/C#892。生产玩家/敌人/机械爪、地图、HP、能量、原900预算、完整支撑与恢复门均不改。helper未改，没有额外resolver用于日志或强制锁靶。

推理：原来只抓住已完成首证据的Burst，不能减少另一只Rust持续封闭的反击窗口。本轮在首K和原短喷完成后，只对当前all-owner danger中真实同波Rust尝试一次合法压制；实际结果支持这一次战术能争取J窗口，但不证明整段战斗可行。

风险：K真实改变敌人的位置与后续攻防方向。不能据一次成功压制推断下一次近战安全，也不能把远程Active权限寿命当作命中ETA。当前完整Main/Explore仍失败，旧资源句柄/FATAL根因、正式视听相机、硬件真人与发行均未关闭。

## 实现与三组真实TDD

1. 首K在实际Idle冻结原单edge/sample/cast/target/application/Stunned/handoff证据。后续每K单独记录原始边沿区间、唯一新cast、同目标Latched、application+1、新发布效果、实际Idle、HP/parent及同一player；区间连续、cast和按键序列递增，pending/fault/abort或任何未记账K都拒绝。旧Bell/Moth路径也须记账；无edge/cast/app增长的拒绝prefix才可取消。没有改成`>=1`。
2. 唯一独立审查发现I1：foreign K结束后，测试驱动仍保持Grapple意图，J请求被翻译成额外K。新增真实保存玩家/输入驱动测试，空交接实现实际RED：J0/K1、采样J0/K1、AttackStarted0；最小GREEN只在事件外验证收据后释放primary、恢复Attack意图一次。回调只观察，不发送输入。
3. 根最终检查发现I2：冻结计数误及默认策略。六个字面量反例实际RED为默认总edge应3却为1；一布尔条件将冻结计数限定在Main显式post-shot策略。默认/Explore继续原总计数exact-one检查，旧终态函数未改。

最后专项GREEN同时通过新ledger30、范围6、实际J交接1，以及旧post-shot12/lethal7/8负例、终态反例和原TightWarning运行用例。纯合同不替代集成通关。

## 原生执行记录

证据根：`G:/knight/验证证据/october13-stable-claw`。下表短名均对应目录名中的同日期时间前缀；原始provenance、三sink、wait/PID契约、源码和binary都保留。17次构建均0警告/0错误；ExpectedFailure空、guards0、无超时，无额外Defines、fixed-FPS或movie。

| 案例短名 | 严格结果 | engine / ms | 实际边界 |
|---|---|---:|---|
| mixed061222 | FAIL | 1 / 421 | 新ledger合法后续K反例RED |
| mixed061531 | PASS | 0 / 1616 | ledger30及原mixed通过 |
| Main061645 | FAIL | 1 / 128939 | Rust压制成立，额外未记账K被拒绝 |
| mixed062409 | FAIL | 1 / 682 | I1真实J0/K1交接RED |
| mixed062516 | PASS | 0 / 1691 | I1采样J1/K0和真实AttackStarted1 |
| Main062631 | FAIL | 1 / 128654 | Burst真实致死J，后续Rust近战死亡 |
| Explore062908 | FAIL | 1 / 153344 | LiftBackLanding解析到其他抓点 |
| pre063212 | FAIL | 0 / 153381 | 7条Handle ERROR，不能以exit0取绿 |
| post063516 | PASS | 0 / 202389 | 原75/owner8，只有原4条scoped WARNING |
| models063909 | PASS | 0 / 392 | 普通模型回归 |
| mixed064030 | FAIL | 1 / 385 | I2默认edge应3却1 |
| mixed064156 | PASS | 0 / 1801 | 新范围6/ledger30/实际交接及旧用例 |
| Main064226 | FAIL | 1 / 129859 | 最终冻结版仍在7226死亡 |
| Explore064505 | FAIL | 1 / 153104 | 最终冻结版返程选错抓点 |
| pre064807 | PASS | 0 / 153412 | 最终普通前段，三sink安静 |
| post065108 | PASS | 0 / 198114 | 最终原75/owner8、普通完成标记全有 |
| models065458 | PASS | 0 / 405 | 最终普通模型回归 |

合计8严格PASS、9FAIL（包括3个有意TDD RED，未把它们伪装为成功）。全部无FATAL。两个post的WARNING各为stdout0/stderr4/external4，同一组镜像，不算8次独立故障；原scope多重集精确匹配。最终post索引0–74、75 enter/75 return、owner8次nodes1→1、普通完成标记各1、CONTROL标记0，原300000ms诊断预算未改，不作性能认证。

## 最终Main的实际链路与未过点

仅以下当前冻结case使用当前ID，不与061645/062631/v121混用：
`thruster-route-main-diagnostic-20261026-064226-710-c24419e4cdbf46b8b3288df6e1f6c5de`。

- 首K：6933实际Idle冻结edge11→12、cast11，按键采样6895、handoff6904；原短喷6964完成，HP3。
- 7164：当前wave Rust `17368763864375`、attack4，真实Target `17368864527543`，anchor `(4683.839,3427.9336)`。一次计划LOS=true/selected同目标，E24.266687、原储备24保留；不把Hitbox点当目标，也不硬编码旧实例ID。
- 7176：后续cast12真实收据edge12→13、sample1、application1、Latched/Effect/Idle均真、HP1→1、同parent `4758387561429`、无abort/failure。该局部HP基线不是恢复成早先HP3。
- 7177：事件外恢复Attack意图。当前共享安全窗`.48333332`、E30.33336，通过原完整J与fresh复验。
- 7182：同被捕获Burst `17369200071223` HP1→0、Defeated、真实致死J Connected；其own Shot证据ordinal6/7064，不借其他敌人的Shot。
- 7226：仍死亡。实际来源是同wave Rust的AttackHitbox `17368830970415`、attack5；不是precision-missile，也不是E0或过热。命中时L held/active均真、朝左，E7.600038/heat42.233217/overheated=false；危险winner仍为左侧resident precision-burst `4772379757085` 的round16。
- 最后ledger仍只有后续1笔、raw13=首12+后续1，pending/fault0；没有用额外K换取通过。原恢复否决记录request1/completion0，整Main严格FAIL。

冲量`(-186.71498,35.178352)`及既有防御方向合同支持右上近战穿过左盾的解释；源方向与左盾约169.33°，超出±75°。这不是已经证实的盾能量耗尽、chip或guard-break。缺失ShieldResolved不能单独解释为盾未开启，因为该事件只在Guarded/BrokeGuard发送。

Burst死后现有晚候选记录随旧mixedFinishIntent关闭，尚缺Rust attack5末段告警/世界Hitbox点的逐帧并行几何。下一先把同一次原危险遍历的被动记录覆盖到遭遇终态，再检验短时转盾/归还远侧防御或合法物理避让；不重试Rust K、不永久忽略左弹、不加等待池或降储备。

## Explore与资源边界

最终Explore停在LiftBackLanding计划方向选中别的抓点；8546已发生ghost-diver、HP2→1。日志中的AscentGrip2路径出现在这个错误解析行，不是Grip2阶段通过，更不是本轮已修Grip2。v121的bone-wanderer/8799属于另一case，不能合并。上行段弱出生绑定/真实AttackFrame事件的被动诊断只完成只读设计，未实施、未计完成。

前段063212的7条Handle ERROR仍严格FAIL。最终pre/post安静只证明这两次退出，不证明旧GCHandle/FATAL修复。当前只读工具核对缺少匹配Godot native符号和已验证的.NET9混合调试入口；VS存在不等于能读出同次native owner。未attach、安装/下载调试器或符号、猜C++偏移、改engine/GC/静态pin或dispose借用资源。完整资源稳定性继续OPEN。

过程口头更正已保留：最初将后续死亡猜成远程火力，实际归因是Rust近战；最初沿用上一轮“Grip2受击”，当前Explore实际更早在返程解析失败。最终结论只用对应case证据。

## 独立审查、根裁决及代价

唯一fresh reviewer `/root/v122_later_claw_review`，gpt-6-astra/high，仅读物理v121两文件差异与相关合同，无子审查/写入/native。C0/I1/新增M0；根按用户效果重评同意I1，另主动发现I2，均在同一次修复pass以真实RED→GREEN闭合。不派复审。整体suite仍红，不能标Task15或十一项complete。

以下17项是对reviewer所有Declined-to-judge的根裁决，不是丢弃范围：

| # | 根裁决 | 错判代价 |
|---|---|---|
| 1 | I1后真实J已成立，Main整路仍未完成 | 把局部反击冒称通关 |
| 2 | 30纯案之外另有实际输入采样/AttackStarted证据，二者不互替 | 自证测试掩盖无输入 |
| 3 | Bell/Moth代码已记账，但本次未走到全部后续实例，保留pending/未记账否决 | 漏发或重复K假绿 |
| 4 | 当前一次Rust压制形成安全窗，不推广为所有火力组合保证 | 后续玩家仍被夹击 |
| 5 | 未改W读取或目标排名，不宣称整体preview正确 | 错修排名破坏正常输入 |
| 6 | 当前实际K证明本次选靶，不当作所有遮挡/receiver的first-hit实验 | 把可见性与资格混淆 |
| 7 | 原Intro/Playing拒绝与提交合同保留，不由本轮替代全验 | 准备期输入被错误接受 |
| 8 | 新回调纯观察且有fault，旧deferred退出仍须原专项 | 释放后回调污染状态 |
| 9 | 未改LoadLevel清理/双异常合同，不新增通过结论 | 泄漏mounted owner或掩盖主异常 |
| 10 | full-support是实际边界采样，不宣称连续子步swept证明 | 漏掉中间碰撞 |
| 11 | 全源/helper无漂移由独立85对/实体/binary核验支持，不由代码review代替 | 混版测试 |
| 12 | 保持原900且后续仍失败，不宣称预算充分、不以加时求绿 | 将策略问题掩盖为时限 |
| 13 | native owner/GCHandle/FATAL保持OPEN，不据quiet修资源 | 隐藏真实崩溃 |
| 14 | 完整十一项/76普通+BossNPC/机关保持全部原稳定ID，FTR/取消/UE不纳入 | 缩小交付范围 |
| 15 | 无新正式图声/动作/UI/相机验收，资产表无新增正式项 | 以测试替代画面品质 |
| 16 | 无目标硬件性能或真人新确认，NOTSEEN保留 | 虚报可见性/FPS/体验 |
| 17 | 未具备发行资格，不提交/推送/打包冒称交付 | 对外发布未完成版本 |

四个继承Minor继续延期：energy-unwind早失败flush、将boundary samples称为swept的措辞、finally stage4 summary、fresh LateFact来源标注。分别可能损失早失败日志、夸大碰撞覆盖、混淆异常交接摘要、误读fresh与outer事实；本轮没有计为已修。

## 保管与收尾

根父会话67203/75734/76146/72319/82294/49481/24122/67085均terminal；owner退出码按各子case独立判断，父batch0仅代表执行完毕。独立custody `/root/v118_custody_audit`明确ENDED，最后命令9f1f4d/exit0，无live工具；34个记录native进程和KNIGHT Godot/dotnet/验证owner各0，之后才写本报告和README。

- 85/85 before/after manifest字节一致；8628实体副本（source8438/helper190）逐SHA匹配；102 sealed binary全匹配，缺失/extra/fault0。
- 最终GREEN064156＋最终5案的12份source清单同SHA；当前1844源/892C#、Debug6/6匹配最终models，无源增删改漂移。
- helper联集12条均单版本且当前匹配；HEAD/branch/index/owner/audio/productionClaw不变。34份user manifest同SHA `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`，未读真实存档语义。
- source SHA `F8F453311D65597C250ADEB8973D54AD0786251E3C2C4954E805C5DA99670E3A`。
- binary manifest SHA `B3AEC609EFBDECCD206A60B1F083463EA77DDF40DC3F7174105FBB82EB9A6F7D`。
- helper manifest SHA `A1AC76A1CBEEAF2061E4787C7AA4191B2FE6831CCBD6640BC2EE66C2E403B8F1`。
- Main SHA `4E306AD0FCA5360D9A9CF91977FAF135334C45B15245904227B9D1C33767EA2D`；Placement `1301CE31EFDA77CEC97568419BB804EE4FDAB549A051A86E3E6F7BF2E255871D`。

本轮唯一手动媒体Check UTC2026-10-25T22:59:39.1500787：Current860284/Base3508/Growth856776字节、Notify=false，没有新增20GiB档位。没有新过程图稿/实录/正式素材、前台或OS输入、付费、自动化、Git写入/推送、删除。G盘free182746021888字节为收尾只读观测，不触发清理。

下一继续当前两侧防御/走位、返程真实抓点和资源native owner证据；正式相机/视听/目标硬件/真人时段及最终授权交付仍须独立完成。目标不改名、不缩小、不标完成。

最终证据：[Main原始日志](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-064226-710-c24419e4cdbf46b8b3288df6e1f6c5de/engine.stdout.log)、[Explore原始错误](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-064505-219-9d7b30ff4a5c4b51a43da5de2c8dfe4b/engine.stderr.log)、[最终pre来源](G:/knight/验证证据/october13-stable-claw/prologue-pre-20261026-064807-281-99ec3ba8d3e74980b99d12ff0c40ee87/provenance.json)、[最终post来源](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-065108-560-643804ffc4dc493ba564ddea6acad553/provenance.json)、[最终专项来源](G:/knight/验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-064156-341-c546969a184d46058f2808cdac093c1a/provenance.json)、[最终models来源](G:/knight/验证证据/october13-stable-claw/models-20261026-065458-019-0ae6f04cc81544858d56b8553e15f2e8/provenance.json)。
