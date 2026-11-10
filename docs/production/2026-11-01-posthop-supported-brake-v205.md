# v205 · 原平台有限退让合同已补，左侧几何不容纳，Main仍失败

## 事实、推理、风险

**事实：** 仅继续G:/knight/Knight，原全部十一项ACTIVE。本轮仅修改Main自动试玩与RangedWeaponPlacementRuntimeTests两个既有CS，没有改生产物理、技能、数值、地图、键位、角度优先、存档或正常敌人。v205是内部工作记录，不是发布版本。只做已授权的隔离无窗口构建/测试。

**推理：** v204第二次同Rust真实K返还后，generic近距退让在高速度时松键，仍靠惯性离开原平台。本轮用生产ThrusterDynamics/真实MotionSettings与Module规划有限force→reverse→neutral，要求整个包络在原floor完整胶囊支撑中心区间内，并在原900预算内完成。几何拒绝不等于已执行制动，更不等于已清遭遇。

**风险：** 当前Main仍失败，实际finite force/reverse/neutral尚未取得执行证明。普通前后段本次通过不消除历史Handle/finalizer/journal缺口；pre距原180000ms上限仅2326ms。完整当前Main+Explore、资源根因、正式视听/美术/动作/UI/音频、硬件/真人/交付仍OPEN，不保证付费人数。

## 修改与测试先行

- Test-only `TryTask10PostHopBackoffPlan` 取最短同侧force pulse，再按真实ground reverse制动至过零/阈值，最后真实neutral摩擦至阈值。反向同tick过零后可能已经产生反向速度，不能把BrakeReachedZero当作最终vx0；检查所有步的min/max/end。
- 只在真实第二Rust K及原attack-intent handoff之后、原generic A/D发送之前尝试一次。真实full support绑定原floor/contact/shape/transform，motion/module/scale=1、玩家/RustHP2、原两receipt、jump/dash/J/K计数守恒。scale变化/威胁/来源或原floor失效中断，不重开；原J的clock、reserve24、几何与原危险扫描不变。
- 每一步只占原900的一个外循环tick，实际新输入sequence/速度驱动force与reverse，松键后等真实neutral才交接；原剩余预算也必须容纳完整动作与8tick命令余量。没有增加等待池、第三K或数值补偿。
- 新控制不代表可无视真实碰撞/敌人移动；无原生证明时不称动作成功。

首轮缺实现控制38aecd38在实际native639ms具名 `MIXED_SUPPORTED_BRAKE_RED`，编译成功非RED编译错误。纯实现f5482a2b native3974ms通过28控制＋原真实mixed fixture。首轮Main4a142267 native165366ms仍FAIL：lease在loop720被拒，没有force/reverse/neutral；原generic仍滑至LiftAccessStepTwo。原始失败完整保留。

## 唯一审查：C0/I3/M0，一次修补，无二审

1. I1：force阶段对初始反向小动量的过零误记到reverse阶段。修复为force完毕重置reverseDone。新增左右±.5近静止反例。独立手算：force−9.75/−29.5，reverse残余+.125，neutral0，端点−29.375，右侧镜像；不是错误首版预期±29.62890625。
2. I2：原budget只算物理步，漏松键/中立交接。修复为完整steps+8≤budget，并测budget4拒绝、11接纳，不加原900。
3. I3：active lease被选中actor条件包围，actor失效早continue可绕过松键。新增真实consumer使用的sticky owner-stage校验，在invalid-actor continue之前使stage1..3→5并释放A/D；owner返回也不恢复。新增各active stage丢失/返回/失效控制。

三问题在9671bb5a native623ms全部具名RED；一次修补后b3da48b6 native3838ms通过41控制和原mixed fixture，未二审。M0，无本轮新Minor；旧v204两项专门coverage延期没有偷偷关闭。整个套件并非全绿：最终MainFAIL明确保留，不能以本段控制GREEN冒充项目完成。

## 最终Main：拒绝的真实几何已经量到

26394862 native141582ms、exit1/noTimeout、guards[]，原900仍未清同RustHP2，玩家HP2/恢复0。实际第二K返还保持，唯一posthop_brake记录为rejected；没有取得运动控制权或actual stop证明。

返还loop720/physics6880/sample6850的同一条被动原始记录：

```text
x=4623.61 actorX=4626.555 vx=0
原floor中心区间=[4619.9995,4780]
retained/support/corridor=true local threat=false
rawJump/rawDash=false currentAxis=0 scale=1
dt=1/60 safe=.3333333
run260 accel2200 friction1800 reverse4000
```

原危险前缀threat=True表示Burst预警存在，本地拒绝gate的threat=False表示尚未要求即时防御，两字段不是同一含义。未依据default plan零值推断错误原因，而是补齐实际输入、delta、几何、motion/module后判定。

当前瞬时左侧站位必须x≤actorX−8≈4618.5552，但原floor要求x≥4619.9995，两区间没有交集，相差约1.4443px。当前中心左余量约3.6104px；正常更长向左退让无法同时满足原8gap与完整原floor支撑。结论只限当前Rust位置，不把它冻结成永远不动。

后续仍走旧generic：先A加速到−260，松键继续滑动，最终落到较低LiftAccessStepTwo；原900结束enemy_health2。不是新控制执行后失败，也不是已经改好了游戏手感。

### 下一独立有界方案（本轮未实现）

应由实际平台空间选择两侧候选，而非固定“离敌人远的左侧”。独立float32生产公式推算：右侧5 force＋3 reverse＋1neutral为9tick/.15s候选，速度36.667/73.333/110/146.667/183.333→116.667/50/−9.167→0，包络约4623.6099..4635.5543，端点约4635.4014，静态右gap约8.8462。4force端点gap仅4.2324，不足8。还须另外容纳真实命令/中立确认的原外循环tick，不能借名义余量放行J。

**不是原生证明：** 右走将经过Rust当前中心；free-motion forecast不含真实body碰撞、敌人运动/挤压、攻击box，也未证明存在普通走穿Rust的fixture。下一轮须原生验证原floor每tick完整支撑、实际新sample、危险中断和跨边后的真实攻击方向，再沿原J全部门。没有实现右侧移动、移动敌人/玩家或冻结AI；未增加新K/重试/等待。

## 当前同版回归

canonical SourceRows1872；原build180s、policy/keyboard/actions/models55s、Main600s、pre180s、post300s。每次运行source/helper冻结，root串行build/native。证据根 `G:/knight/验证证据/october13-stable-claw/`。

|case时间/短ID|阶段|native ms|结果|
|---|---|---:|---|
|174958/38aecd38|缺实现控制|639|具名真实RED|
|175155/f5482a2b|纯算术/阶段策略|3974|28控制＋原mixed PASS|
|175354/4a142267|Main初次集成|165366|FAIL，lease被拒/原900未清|
|180119/9671bb5a|审查反例|623|3I具名真实RED|
|180224/b3da48b6|一次修补策略|3838|41控制＋原mixed PASS|
|180349/26394862|最终同源Main|141582|FAIL，原floor真实几何已量|
|180802/d6bca9dc|最终策略|4003|41控制＋原mixed PASS|
|180844/c520cb4b|最终键盘|20293|PASS；独立headless title8196ms亦PASS|
|180949/fdd42e3b|最终动作|9539|PASS|
|181031/2b6f6805|最终models|521|爪模型子集PASS|
|181105/75164fbe|最终完整pre|177674|原180000内PASS，仅余2326ms|
|181435/ff44c4da|最终完整post|210801|原300000内PASS|

合计12案8限定PASS/2真实RED/2MainFAIL。最终同源重复policy也计入时8案7PASS/1MainFAIL；最后六阶段batch exit0不是整个项目全绿。所有build无警告/错误；两RED及两Main各一ERROR，均严格失败，非ExpectedFailure。post原四限定负控WARNING保留，无ERROR/FATAL，完整finalizer/drain/phase链完成。Explore未重跑，旧通过不续作当前双路线资格。没有重复8192饱和identity-pre或放大任何时限/容量。

## 裁定与未验边界

1. 保留当前dirty checkout、全部原始失败/基线/中间源，不Git/worktree/commit/删除/发布——明确授权边界；代价是无clean-branch/版本交付资格。
2. 用可达原strike-gap的最短有限pulse，不假定任意1force够用——保留真实惯性；代价是当前敌位置只供规划，不能证明未来碰撞或J成立。
3. 已授权inline持续执行，不重复问程序性确认——没有扩展玩法/权限；代价是不能据此推断新视觉/品味/人体验收。
4. 拒绝后不加第三K、额外等待或降低门限——不掩盖原MainFAIL；代价是本轮实际有限移动门仍OPEN。
5. 直接用原canonical和保留式ledger代替会提交/删除的skill wrapper——用户禁Git写入/删除优先；代价是没有task-done/merge资格，未完成Task1不勾完成。
6. 审查让root判断nativeMain——root已核实际两次FAIL，拒绝不是成功；代价是无通关资格。
7. 审查未签最终suite/Explore——root核最终六阶段PASS、Explore延期；代价是当前双路OPEN，不能用专项代全关。
8. 审查不含旧dirty变化——只审两个v205增量，不覆盖全checkout；代价是旧问题没有被签字关闭。
9. 未改生产/地图/数值/selector——test-driver规则不推广为玩家手感改良；代价是没有新正式美术/动作效果。
10. 资源Handle/finalizer/journal根因独立未修——普通pre/post此次PASS不替代定位；代价是稳定性/泄漏验收继续OPEN。
11. v204旧deferred coverage原样保留——本轮41controls非首receipt捕获的完整替代；代价是两项旧覆盖风险仍在。
12. 原十一项、视听/硬件/真人由原门验收——本地测试不作正式完成；代价是当前不可交付为完整序章。
13. custody/进程/容量/版本由root单独核——只引用终结进程与匹配清单；代价是不把审查当运行保管或发布证明。

## 最终保管与容量

独立只读核账：12案60清单对/5932选中源副本/138helper副本/72bin逐SHA相符。UTC10:23:33当前1872源项、post consumer10helpers、6bin零差异。最后七阶段(Main＋最终六回归)为6PASS/1FAIL；若另计同源较早b3da重复policy则8案7PASS/1FAIL，不把重复通过增加验收范围。

UTC10:23:41，25旧owned创建/路径身份全部终结：12build＋12engine＋1独立headless title。PID59728已复用为Python、54808已复用为conhost，均不同创建/路径，未操作；不把PID复用当旧Godot存活或声称所有PID都不存在。已知KNIGHT Godot/dotnet路径活进程0。

sourceManifest `CBACBB84871AB84AE337A12DEF07727B13CE342A28CDE6460A5E89F2B7FA06D0`，binaryManifest `513F980184E956D89D42BA4989B2269A2B295B0E70FD9068E1AB81970E3D4619`，DLL `6189AB820C20484393D4AB17AFF5639AD6600EEC57688D4BCD44DFD565BA230E`；postHelpers `C5EF45802948AA866E2659414BDBE4BD8D22B0D980D459CAA292638C213429E7`。

Main `6BE849C2A6056DE78B48058660F042809F3A1250B7C4A92E892A480353F6A4E3`；Placement `18BC024CF42AB87AD6BF1EC3E1A4A9C683ECA64E0AACD665F9E38F633C32580E`。既有Keyboard/Actions/Projectile/ProjectileTests/Motor/Camera/Geometry及canonical生产/测试边界逐hash未变。HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，branch `codex/task6-certified-final-20260902T0555`，index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`，均未变。

手动媒体检查UTC10:23:54为7,427,750bytes/86files，Notify=false/NewThresholds0，未到新20GiB；不建立自动化、不因容量提示停工。G盘本轮读取余135.04GiB，无删除或迁移需要。

无GUI/前台/系统输入、真实C存档写入、新收费生成、额外原生采集、运行时更换、定时自动化、Git写入发布或删除；没有新正式美术/媒体。原十一项目标继续ACTIVE。
