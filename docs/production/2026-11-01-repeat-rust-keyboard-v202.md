# v202 · 同一普通近战兵两次正常键盘机械爪，完整主线仍未认证

## 事实、推理与风险

**事实：** 只继续 G:/knight/Knight。原十一项总目标ACTIVE；本轮只改 `tests/integration/KeyboardClawRuntimeTests.cs`，生产玩法、地图、敌人数值、伤害/能量、角度优先解析器、Main调度、真实存档、canonical helper都未改。v202是诊断记录号，不是已交付的正式游戏版本。

独立场景使用原保存Player和普通RustShellSquire、原ThreadTarget/adapter与真实长平地。位置只在挂树前设定；挂树后不写pose/HP、不冻AI、不直接Step模型或调用拉拽、不重置claw/adapter、不治疗/补能。两次均由同一KeyboardRouteInputDriver发实际Godot键盘K事件，非系统键鼠或真人操作。

**推理：** 这解决了“普通Rust是否在一次交互后不可再抓”的能力缺证，而不是证明Main重复抓取策略已经成立。源码没有一次性禁用，与本轮正向实证吻合；无需为此新增玩家机制或放宽敌人数值。已经Stunned时再次K会不会刷新计时，本轮没有测试和认证。

**风险：** 此处PlayerHP5、RustHP3、满能量、平地、单一敌人。不能替代Main HP2混战、其他枪手压力、地形/近身碰撞、低能量、安全反击门、恢复控制器或完整通关的证据。完整Main最近v201仍原900内未清；Explore最近v201通过，不贴作本轮新资格。正式美术/连续动作/UI/音频/正常速度视听、目标硬件性能和用户游玩认可继续OPEN。

## 实际两次交互

最初keyboard1564dd06与审查修补后的keyboardbfb41be9均严格通过；初次是既有正确行为的characterization PASS，不伪造生产bug或RED→GREEN。

最终bfb41be9：同Player87476405768、Rust89003132562、root87392519680、target89103796026、adapter89154127680。

| 交互 | cast | sampled K→Idle input sequence | Idle physics | adapter累计 |
|---|---:|---:|---:|---:|
| 第一次 |1|17→37|726|0→1|
| 第二次 |2|53→64|753|1→2|

第一份readonly receipt在第二份完成时逐值相同。第二次开始前必须先观察第一次Stunned自然退出、原机械爪完整Idle及新Telegraph；新warning physics741、remaining0.65。每次production preview、locked ID、实际receiver都指向同Rust；各一个新K样本/Latched/Released，实际新Stunned累计2，正常回Idle累计2，NextCastId逐次增加且rope/residency清空。全程HP5/3、伤害/治疗/死亡/攻击连接/普通跳/短喷均0，能量保持满值，无盾或J补救。

## 唯一审查及一次TDD修补

fresh whole-change review：**C0/I1/M0**。I1确实成立：原 `complete=true` 后回调提前返回，等自然stun退出/新warning期间可能漏记失败或无K的违规输入；下一次开始又清每次局部fault。

先将同一观察逻辑提取成实际fixture消费的test-only observer，用控制值流“第一Idle→TargetLost/无K的AttackPressed→干净第二cast”复现。keyboard2e6c38af实际输出两种retention=false并在具名 `REPEAT_RUST_OBSERVER_GAP_RED` 失败。此为观察器seam的控制值，不冒充原生演员实际发生失败或自动发送额外J。

最小修补把experiment级Fault/总K样本数与每次receipt分离：每次真实claw publication都先观察，所有新inputsequence均检查违规动作；Fault和总数单调保留，不随receipt完成/第二cast开始重置。每次receipt仍只捕获一次。bfb41be9两负例retention=true、干净流可过/恰两K，并再次完成真实两次交互。无二审、无新增Minor；旧v199–201等延期项继续保留。

## canonical结果与保留失败

全部命令：原canonical `verify-claw-knight-v001.ps1 -Phase <phase> -SourceRows 1872`。原build180s，keyboard/model/actions55s、pre180s、post300s均不扩大；未用ExpectedFailure、fixedFPS/movie或错误屏蔽，source/helper在每个native运行时冻结。证据根 `G:/knight/验证证据/october13-stable-claw/`。

| 案时间/尾码 | phase | native ms | 结果 |
|---|---|---:|---|
|154733/1564dd06|keyboard|20454，独立title8272|初始characterization严格PASS|
|154951/65c294da|models|633|爪模型子集PASS|
|155031/373810d3|actions|9509|PASS|
|155307/444e14b4|pre|180044|原180000超时FAIL|
|155642/05c84f5e|post|173820|原生fatal FAIL|
|160128/2e6c38af|keyboard|2477|I1具名观察缺口RED|
|160317/bfb41be9|keyboard|20169，独立title7988|I1控制GREEN＋真实两次K严格PASS|
|160419/40969573|models|509|最终源爪模型子集PASS|
|160457/48ef53f5|actions|9468|最终源PASS|
|160620/17ce792c|pre|27099|最终源原生fatal FAIL，无Timeout|
|160723/f773bee4|post|210797|最终源原300000预算严格PASS|

总计 **11案：7限定PASS、1观察器真实RED、3完整回归FAIL**；最终源五阶段4限定PASS/1pre FAIL。**不能宣称whole suite green或任务正式完成**。I1局部RED→GREEN成立，但全套修补放行仍受pre资源失败阻挡；不追加二审或另一次修补来把这一失败藏掉。

第一轮pre到LowerBuriedMachinery entry_to_arena/sample0.2时原上限超时，无完成/drain。第一轮post真实exit -1073741795、无Timeout；stderr101为 `ERROR: FATAL: Condition "gchandle.is_released()" is true.`，原生栈为mono_object_disposed_baseref/GodotObject.Finalize/GC.RunFinalizers。只有18完整Task7边界对及index18单enter，没有owner8、flow-registry和最终完成。不是仅凭“missing scope”就归因，也不把exit0/build绿替代原生结果。没有新增dump/崩溃采集或认定本fixture是根因。

最终源pre在camera fixture sequence21/cycle2/CanyonAscent high=true的load-enter后，27099ms以同一gchandle/Finalize/GC.RunFinalizers原生fatal退出 -1073741795，并非180s超时。最终post通过不消除前两次fatal或超时，更不证明资源根因已修。下一轮优先做现有工具内的资源寿命根因核验，再继续有限Main调度，避免重复长测当修复。

## 裁定、边界与后续

1. 沿现codex脏checkout留档，不Git/worktree mutation、提交、合并或cleanup：遵守用户限制；代价是无合并/发布资格，全部失败/源素材继续保留。
2. 初次正向PASS按既有能力characterization记录，不制造缺陷；代价是不能称生产功能修复。I1观察器才有真实seam RED→GREEN。
3. 审查未判断已stun刷新、Main HP2/多源/低能量/恢复、有限Main调度/完整序章、总回归保管/交付、历史脏改动/旧Minor、真人OS/视听：这些要么由Root实际聚合，要么保持原目标OPEN；代价是不能从本局部报告推导正式完成。
4. 不为完成标记重跑同一已冻结失败、增加预算、借旧二进制或用要求Git/删除的task-done helper；一次C/I修补后按新源实际回归，旧失败仍计入总数。

下一阶段须先看当前原生回归是否仍有资源错误，再为Main另立**阶段限定、有限次数**的重复Rust K合同：保留Hop5/唯一Space/三弹/HP2/恢复0、同actor与原receipt、新自然warning、production fresh preview/LOS、原全来源危险扫描及原900，后续实际新cast/application/Stunned/Idle也需独立回执。不能简单删suppressed-owner HashSet、无限重抓或取消反击energy24/clock门。上述是下一方案，不是本轮实现或通关。

最终post原75同名/index有序enter→return、owner8/nodes1→1、原四限定负控WARNING及完整finalizer/drain/phase尾链通过；不作为性能稳定证明。pre fatal只有一条ERROR记录内嵌FATAL，不双算成两次故障。

保管：11案55清单对、5220选中source副本、130helper副本、66bin逐SHA一致。24owned＝11build＋11主engine＋2独立title-entry；08:16:53UTC按creation/path身份全部终结，24PID均不存在，KNIGHT工具完整路径活Godot/dotnet为0。08:16:44UTC当前1872源项/6bin一致；consumer helpers分别keyboard11/pre10/post10、去重13，不能把13当每案helper数。失败案也有完整冻结保管，不等行为通过。

最终sourceManifest `513F1E6BBDA942D42B28EE7C02E694DC9A3F815B5A2628FC7D5E79FD1D086397`；binaryManifest `F3F392E96915114EA59579D0B0A752F0C8CBB60797614CE47A22EB611904370E`；DLL `FBAAC26F31591243A9D9E6A7A0FC0CFF9FB3B0B00AB0449629E795149340DA16`。

当前源码KeyboardClaw AB320F46459F2CDD6C7B8513334830221C42BC40741E137C7625093D102B58C4；Main ABBFC64F/Placement4AEFF9C5/生产Projectile235BF/ActionsEE5/MotorCB65/GeometryA359/canonical7A79、HEAD e7c70498、index21610保持未改。08:14:26UTC手动媒体检查7,427,750bytes/86files、Notify=false/NewThresholds0，未到新20GiB；不建自动化。无新正式美术/图稿视频/付费/窗口或系统输入/额外采集/定时自动化/Git写入发布/删除/真实存档操作。
