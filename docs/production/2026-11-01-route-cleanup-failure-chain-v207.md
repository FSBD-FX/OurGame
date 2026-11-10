# v207 · 主线失败与清理失败的原异常保留

## 事实、推理、风险

**事实：** 只改G:/knight/Knight两份既有测试CS（Main与KeyboardActionRuntimeTests），生产玩法、输入/物理/地图/伤害数值和AudioDrain原函数均未改。原十一项ACTIVE，v207仅内部工作记录，不是发布版本。只做隔离headless构建/测试，无GUI/OS输入。

**推理：** v206当前Main传播的是AudioDrain错误，未记录原900断言。旧RunTask10的finally及外部using Dispose都可能替换先前异常；现在原失败与后续清理错误由同一有序finalizer保留。实际本轮Main仅出现原900失败，音频清理正常；不能据此断言上轮究竟是否有被遮蔽的原异常。

**风险：** 这修的是测试退出与诊断可靠性，不是游戏战斗、音频混音或正式美术。真实原生双故障本轮未出现；多故障保留以控制用例证明。Main未通关，pre又有不同的下降教学断言失败，整套不绿；历史资源、双路线、正式视听/硬件/真人/交付仍OPEN。

## 实现边界

`Task10FinalizeRoute(primary, cleanupSteps)` 保存原Exception对象而非仅message。零错误正常返回；单错误使用EDI保留原栈；多错误按primary→清理发生顺序建立AggregateException，不Flatten原来的aggregate。每个列出的清理都只尝试一次，同步或异步错误都不阻断后续清理。

RunTask10从原首个keyboard using获取点起进入受保护try，明确拥有keyboard/ranged/monitor，不再留下外层implicit Dispose替换最终错误。清理顺序为monitor→ranged→ReleaseAll（含位置读取）→原mounted audio/physics tail→持久化保护→driver。未成功mount时不新增off-tree native释放或fallback；未更改现有Dispose内部、构造器异常安全或AudioDrain策略。

原AudioDrain仍60frames/2000ms，TestSceneTeardown SHA26EF0C40未动，没有新增GC、延长超时、重复执行清理、吞音频异常或重跑筛绿。多个失败仍是严格失败，不转换成通过marker。

## 测试先行与唯一审查

- 16e1e803：初始失败尝试，8个失败条目，其中单cleanup栈控制本身使用`throw audio`重置了先前栈。保留此案，但不把这条错误oracle算作干净RED依据。
- 修正为真实faulted `Task.FromException(audio)`后，75fe3057在同一个旧stub下再次具名RED：7个真实失败条件，单cleanup原栈控制已通过。实现前未跳过复RED。
- 107ea391通过11控制和原真实键盘actions。控制涵盖success、primary-only原对象/原栈、单cleanup、primary＋音频类cleanup、多个sync/async cleanup、后续persistence/driver仍尝试、嵌套aggregate不Flatten、受控early/normal return。
- 唯一fresh whole-change review **C0/I0/M1**，无C/I修补、无二审。M1延期：受控返回是单独local try/finally，尚无直接覆盖RunTask10实际获取/释放接线的after-driver/after-ranged/early-return控制；当前接线静态审查未发现缺陷，但不能把控制矩阵当作完整集成ownership证明。旧v204两项覆盖仍未关闭。

## 实际Main结果：原失败完整保留，音频这次正常

7df087fd native139641ms/exit1/noTimeout/guards[]。stderr首个实际ERROR是counterweight-lift原900未清断言，保留DefeatTask10Encounter原抛出位置、DriveTask10NormalRoute及RunTask10调用帧，随后有EDI finalizer帧。玩家HP1、RustHP2，原loop899、原平台LiftBackLanding。仍未清完遭遇，不签Boss/出口。

音频本案实际通过：1frame/59ms；持久化109保护项/default application data未变。该自然运行只证明primary-only没有被清理流程丢弃，**不是自然原生双故障捕获，也不是AudioDrain故障修复**。双错误/多错误对象与顺序来自已标明的受控测试；v206 AudioDrain失败原案仍保留。

## 当前前段失败：不是超时，也未量到失败子条件

801abe24 native164361ms/exit1/noTimeout。实际断言位于DescentClawGuidanceRuntimeTests.Run:553，调用RunPrePhase:231：

```text
same playerId && requests==0 && recoveries==0 && HP==5
&& CurrentObjective==ReachBasin
```

五项至少一项false，但现有错误文本未记录当时逐项值，不能直接宣布“摔伤”“发生恢复”或“角色变了”。此前551的GroundedIdle已证明最多180个真实输入tick内连续6次指定LowerCatch向上contact＋ClawIdle；552又Settle了2physics/3process才检查553。旧second-J日志HP5早于后续盾/爪/Space，不能作为终态HP证据。

AudioDrain本pre实际通过2frames/276ms，failure finalizer/drain也完成。该fixture与当前pre前缀不调用新Task10FinalizeRoute或KeyboardActionRuntimeTests.Run，只用未改的静态KeyEvent/Frames工具。这个调用范围事实不等于已证明回归原因或没有间接时序影响。

下一安全动作：在原553边界记录身份、请求/完成、HP、objective五项真实标量及当前position/velocity/claw/floor，不加tick/query/input；需要时再按现有damage/fall事件归因。在获得数值前，不改HP门、不加settle、不重置目标或猜修复。与Main双来源转盾、历史AudioDrain问题分别保留，不能互相代替。

## 当前同版回归

SourceRows1872，原build180s/actions与keyboard/policy/models55s/Main600s/pre180s/post300s；root串行，运行时source/helpers冻结。

|case时间/短ID|阶段|native ms|严格结果|
|---|---|---:|---|
|192538/16e1e803|初次控制|625|失败尝试，含1错误oracle|
|192710/75fe3057|修正oracle后控制|514|具名真实RED，7条件|
|192920/107ea391|纯finalizer＋原actions|9486|11controls＋原真实actions PASS|
|193213/7df087fd|完整Main|139641|FAIL，原900未清，原失败完整保留|
|194301/6f61f702|最终actions|9497|PASS|
|194350/568f0ebd|最终keyboard|20528|PASS；独立headless title7897ms亦PASS|
|194457/b195a8b3|最终mixed policy|3832|PASS|
|194537/81f0a5c7|最终models|503|爪模型子集PASS|
|194613/801abe24|最终完整pre|164361|FAIL，下降教学553断言，不是timeout|
|194934/35805639|最终完整post|205168|原300000内PASS|

10案共6限定PASS、1干净RED、1含错误oracle的失败尝试、Main/pre各1FAIL。最终六阶段batch为5PASS/1FAIL、exit1；加同源Main为七阶段5PASS/2FAIL。早期纯helper GREEN107e与集成Main之间另有源码变化，不拿它拼最终同版资格。

post原75同名/index有序对、owner8/nodes1→1、完整finalizer/exitdrain/phase通过；仅原四限定WARNING（observer1/profile1/Completed→Recovering2），无ERROR/FATAL。pre有自己的early-failure JSON与实际ERROR，不能沿用v206超时分类。所有构建0warning/error，失败不由expectedFailure豁免。

Explore未跑，不借旧通过签当前双路。完整序章原十一项、当前完整Main/Explore、资源稳定、正式视听与真人验收都未完成。

## 全部裁定与审查未验范围

1. 原dirty checkout及所有原始失败/基线保留，不Git/worktree/commit/删除；manual ledger替代会提交或删除的skill wrapper。代价是没有clean-branch/发布资格。
2. 已批准inline持续推进，不重复问程序性确认。代价是不推断新的玩法/视觉/真人认可。
3. 保护范围从原首个using获取点开始，不简单移除using后只保护monitor之后。代价是test-only try范围变宽，当前接线须回归且直接ownership用例仍延期。
4. 不改AudioDrain/backend/GC/等待上限，不加fallback free。代价是音频失败仍可能复现，不能签稳定性或声称音频修复。
5. 首次错误oracle修正并复RED，初案保留。代价是增加一次失败尝试，不能把其八条件全作产品缺陷证据。
6. 审查未判断Main战斗/受伤/转盾：root实跑仍原900FAIL，代价是没有通关资格。
7. Explore/current dual-route未验：延期，代价是当前双路OPEN。
8. 原生route＋audio同时失败本轮未观察：只签受控对象/顺序，代价是没有当前自然双故障证明。
9. mixer根因及长期audio/resource稳定未签：普通本轮audio通过不消除历史失败，代价是后续仍需真实时序证据。
10. 既有Dispose内部、部分构造失败不重做：只保护成功获取对象并保留实际抛错，代价是这些旧实现没有被全面认证。
11. off-tree释放及原first-driver边界之前的setup沿既有范围：无新fallback，代价是不宣称所有构造/未mount路径已无泄漏。
12. hostile/custom list枚举、并发修改、永不完成的task和进程fatal不作为通用scheduler承诺：真实caller私有固定delegate array，代价是helper不是通用故障调度器。
13. 无关dirty/其他route入口/旧deferred未审：代价不是全checkout审查或其他错误保留路径的统一修复。
14. 审查未执行回归：root实际最终七阶段5PASS/2FAIL，代价是不能以C0/I0或11controls宣称全绿。
15. custody/process/index由root单独核：不把代码审查当运行或发布回执，代价是独立证据不足处不能签字。
16. 正式美术/音色/普通显示/硬件/真人/原十一项/release仍原门：代价是不可正式交付或保证十万付费人数。
17. M1直接ownership接线控制延期，不顺手改Minor；旧v204覆盖保留。代价是未来接线遗漏/重复Dispose可能逃过当前矩阵，不能冒称集成coverage完成。
18. 当前pre只定位到五项合取，不猜HP/恢复归因。代价是下一需要原边界标量再决定修法，不能把失败归到某个未量到条件。

## 最终保管与容量

独立只读增量核账：10案50清单对/4752选中源副本/110helper副本/60bin逐SHA全相符。UTC11:56:07当前1872源项、post consumer10helpers、6bin零差异；最后六回归与Main7df同源同DLL，早期107e未混作最终source资格。保存argv均headless、无fixedFPS/movie，不代表显示/OS输入或真人资格。

UTC11:56:48按creation/path核21 owned身份全部终结（10build＋10engine＋1独立title），当时21旧PID均不存在，已知KNIGHT Godot/dotnet路径活进程0，无进程操作。

sourceManifest `521B19C07A400EEA986AE5D5CBF6B2DA1C1DD53B3D402AD8CFF2BD50AB3AF1AE`；binaryManifest `D629966F27152251CEDFD721FF7D9B035AEF831EE98379E252E143B517DD6B6C`；DLL `A5540235E50661185E841DCCFDD4D74924071A2A8892863811E7BA22902F858F`；postHelpers `C5EF45802948AA866E2659414BDBE4BD8D22B0D980D459CAA292638C213429E7`。

Main `C1ED9D16290D37161E8DCA32293154754D32E98CF8FA1EEDCFC7DE08B986D208`、Actions `74AD31EABE0191A5760B16BD26621A2D9F441D3AD745920667DF3B1FA63150AD`；Teardown `26EF0C403CF435312C69394E29B82D31396845455160386AF1633443890A6E26`、canonical `7A79C29AEBC02A9226B5ACA987D1C7B26FAF5CE5A4AF534CFC12E2123669BE19` 未变。HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、branch `codex/task6-certified-final-20260902T0555`、index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 未变。

媒体手动检查UTC11:55:13为7,427,750bytes/86files，Notify=false/NewThresholds0，未到新20GiB；不建立自动化或停工。未增加正式图稿/实录，所有旧失败/基线保留。

无新收费生成、正式媒体、GUI/前台/系统键鼠、真实C存档修改、额外原生采集、运行时更换、定时自动化、Git写入发布或资料删除；原全部十一项目标继续ACTIVE。
