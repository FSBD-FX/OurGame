# v226 · 攻击中普通分离的停止支撑

状态：原全部十一项及R-I/R-C/R-P继续ACTIVE/OPEN。只修改三个测试/验路C#，没有生产物理、键位、数值、地图或正式美术/动作增量。新原生行为RED→GREEN成立，最终同源14阶段13PASS/1Main真实死亡严格FAIL，不把本增量冒充完整通关。outer73065已终止exit0；该outer只封装最后12阶段，不替代先前Main失败。

## 事实、推理、风险

v225 Main899在Hop后，普通signedGap6.9285＜8的向外分离发生于J尚在攻击中，因此不属于要求`!IsAttacking`的冷却后退。真实A随后使完成Motor样本的胶囊左缘越过原平台。当前PlayerCombat横向J不锁运动、不改变Motor倍率；只有向下命中会反弹，盾和爪另有倍率/运动所有权。不能通过冻结攻击移动或增加摩擦掩盖消费者遗漏。

本次将“需要停止支撑检查”的分类保留旧cooldown，再加有限、非竖击几何的gap＜8普通分离。仍限定counterweight键盘、同Rust、原平台真实接触/完整胶囊、无盾防御/爪/短喷/外部动量、实际Ready运动快照/模块/倍率/步长及原899余额；正在攻击还要求既有AttackStarted回执的实际Horizontal方向，排除下劈反弹。复用两采样持键＋完整中立停止预测，不改生产力学。

普通gap＜8使strikeRange为false，且进行中的J不会重复开始，所以此处仍在原一次PhysicsFrames之后重新决策；新J的两帧朝向脉冲、盾释放等待与L方向键是不同输入所有权，不由本包络认证。当前平台/碰撞、HP2、24储备、跳数/退休/无恢复和原时限都未放宽。

## 原生测试先行

证据根`G:/knight/验证证据/october13-stable-claw`。

- 816ed44e：测试AttackConnected回调错误写成两个参数，实际为一个Hurtbox；编译失败、无引擎，单独保留，不能记行为RED。
- 88325976/native3060：保留旧cooldown-only分类时，实际保存Player已真实J一次，观察到hitbox Active→关闭但IsAttacking仍true的Recovery→自然Ended，实际倍率1。新bounded攻击臂仍持A7次/拒绝0，最小x416.66672、原平台完整支撑丢失，具名`MIXED_ATTACK_SPACING_SUPPORT_RED`。不是缺符号或假状态测试。
- ae3eaa3d/native8771：分类和真实调用者接入后，攻击臂A5/拒绝2、12个完成Motor样本、最小x433.00006，完整支撑保留。使用Main的PhysicsFrame pre-node节奏，实际一次J/零命中，无Space/Shift/K/救援/治疗/恢复；自然Active/Recovery/结束全观察，倍率始终1。无敌人夹具中的零命中不是“命中已完成”。旧未限臂仍A7丢支撑，原冷却和斜盾对照、11分类字面控制、旧时钟/真实J八帧和原mixed-policy保留通过。

新测试及调用分类之外，未改动作资源或力学。它证明实际运动边界，不证明角色已有优雅特色、正常速度视听或操控手感合格。

## 当前Main与更早受击

Main9c31ec7e关闭：build36319/exit0，native132866/exit1，原600秒内严格FAIL。Rust contacts2/returns_left1/faults0及Hop stage/cycle5/42样本/单跳/三计划弹体通过/未通过退休0保留；postHop K仍未尝试。

当前fatal在physics6816：居民Burst第16发使HP1→0，能量0、盾未激活，玩家(4622.7476,3423.9685)、速度(0,0)、on_floor=true；恢复1/0、fall_damage0，原监测抓到回检查点(1680,3960)的2991.17位移。与v225低台死亡不同，此次fatal边界在原平台高度，胶囊宽度仍落在[4600,4800]；这不是全程逐物理帧完整支撑证明，也不能改写为通关。

独立补核的限定轨迹：原决策6768–6816连续49条，对应完成物理帧6767–6815，没有此窗口内的等待缺帧；均on_floor=true/Y3423.9685，胶囊最小左缘4602.7476，比平台4600留2.7476。旧6771越界点的左缘从4599.47变为4603.9146。该窗口保存的完成位置已不重现旧越界，但不外推到帧内碰撞轨迹、其他时段或用户体验。

早先6489仍由居民连发枪手ordinal10造成HP2→1，不是当时planner winner Rust。它在6476从左下(4533,3459)发出；6489实际持盾Right、能量40.533207/heat29.983397且活动，属于方向/多源问题，不是那时能量耗尽。当前记录只证明普通Active按较小形状间距选Rust；不是Telegraph近Rust特殊覆盖。近战源正式回执直到6556才绑定，6477/6488泛用零伤格挡来源unknown，不能证明Rust attack3已花费。

因此原postHop HP2/24条件仍保留，不改成HP1取绿；不能无证忽略Rust3或调整危险优先级。下一独立工作需要在早期原扫描/真实碰撞边界保存完整候选形状/身份、实际键与来源明确的格挡/接触/已处理身份，先区分已花费攻击和真正交叉同时危险。Hitbox membership先于receiver，ShieldResolved早于receiver记账，Connected在receiver成功之后，来源关联不可凭同帧猜测。本轮未实现该新采集，也没增加dump/改引擎。

## 当前冻结回归与历史故障边界

最终14阶段均关闭，13PASS/1Main严格FAIL；Explore35b89关闭native343674ms/exit0，原600秒内。前段510573a6为native172075ms/exit0，原180秒内，44个有序enter/return、16音频drain、最终drain/phase OK全过，无Handle/ERROR/WARNING/资源/泄漏文本。这不把v225超时或历史Handle改写为修复。

| 同源阶段 | 案尾 | native ms | 严格结果 |
|---|---|---:|---|
| 新攻击支撑＋原mixed-policy | ae3eaa3d | 8771 | PASS |
| 正常Main完整计划 | 9c31ec7e | 132866 | FAIL，真实死亡/恢复 |
| 混战资源 | 2dc79a62 | 16096 | PASS |
| 原反击 | fc3ba72a | 4130 | PASS |
| 原早预警反击 | 0302399e | 4098 | PASS |
| 原紧采样边界拒绝 | 2b4726b1 | 1792 | PASS |
| ranged-models | 9a06e53e | 404 | PASS |
| saved-actor | 0d230882 | 58388 | PASS，原120秒门 |
| actions | c5295d03 | 9415 | PASS |
| keyboard/独立title | 26fb1062 | 20063/7715 | PASS，无窗口不可见/非真人 |
| models原机械爪子集 | ed117838 | 527 | PASS，不签全部力学单测 |
| 原pre | 510573a6 | 172075 | PASS，原180秒门 |
| 原post | f5347e39 | 220942 | PASS，原300秒门 |
| 正常Explore完整计划 | 35b89ce2 | 343674 | PASS，Boss/支撑出口恢复0/0，非全部敌群/全操作或真人 |

保存计时对照：Traversal进入从v225175769变147762ms，早28007；原三个慢fixture分别61265→45939、31495→26386、15244→11321ms。只是跨度变化，不能认定本次停止分类改善了性能。中途12案60/60、13案65/65是当时已关闭案的保存审计，不与最终14案混计。

后段f534实际build26672/native220942ms、exit0，原300000预算内；75有序案例/19音频drain/8次owner重复/节点1→1通过。四条负控警告精确为ClawFrameChanged observer故意异常1、非法VisualRoot profile1、Completed→Recovering拒绝2，函数/案例/scope计数匹配，无额外warning/error/资源或泄漏。`original180Accepted=false`，不是180秒通过或硬件性能认证。当前13案65/65保存文本对、27/27原生身份、14去重helper与Green源/二进制清单一致。

最终Explore35b89：build PID45860/36969ms，native PID58736/343674ms，均exit0且原预算内；六帧85ms音频drain，109条保护项不变，无额外error/warning/资源/泄漏。标题起步、同Player47546631684、Boss击败和实体支撑出口到Completed，恢复0/0、完成一次、继续持键60帧位置及记录的存档哈希不变。实际Godot内部键盘事件1306次按下（J99/K89/L59），无生成鼠标或旧action事件；OS写次数不可用，未证OS输入。23节点/11挑战种类、11激活/10击败（不含配重遭遇击败）、4机关/双向捷径/2检查点不是全敌群或全操作覆盖；`operationCoverageComplete=false`、`fullGoalAccepted=false`，不是正常显示/硬件/真人验收。

独立保存审计增补最后Explore：五对均相等（1888源/109用户/9helper/0非空续读/6二进制）；其9 helper已在Green且不扩14去重并集。最终14案70/70保存文本对、29/29原生身份/参数/契约匹配，当前13PASS/1MainFAIL。该保存审计不重算历史payload；与下方root当前文件绑定是两件事。

另只读核查了[此前一次隔离采集](2026-10-28-native-crash-capture-v001.md)与`G:/knight/原生诊断/2026-10-28_单次原生崩溃采集_v001`索引：那次post正常退出，TargetExited=true/DumpCount=0/Dumps=[]/OneAttempt=true，没有可供离线分析的dump。许可按一次尝试已使用，未自行重试。[v104](2026-10-25-first-hit-engagement-and-sector-finalizer-v104.md)旧finalizer对象指针不是Swap的oldGCHandlePtr，[v203](2026-11-01-resource-identity-revalidation-v203.md)也未关联到此次对象/脚本/old_ptr/createWeak。两类错误不混同，当前根因仍未证实。

## 审查与取舍

唯一fresh审查C0/I0/M1，无严重项修补/二审。延期Minor：共享断言说明仍写“no attack”，新臂实际要求oneJ，应将来改“no additional attack”；实际条件和日志均精确要求一次，不借文字隐藏失败。

开发裁决及代价：

1. 沿用Godot/无窗口/内部真实事件/root串行及只读分工；不代表用户批准美术或实际操控。
2. 在现dirty codex工程保存三精确基线，不worktree/commit/清理；代价是只有本地保管，没有发布归档。
3. 只修普通分离停止消费，不冻结运动/改摩擦/放宽HP2；代价是早期交叉火力未修。
4. 未证Rust3花费保持unknown，原6489实伤保留；代价是更早安全性仍待来源证据。
5. 当前Main死亡继续FAIL，不用保住平台代签；代价是完整主线未就绪。
6. 最终14只按关闭当前案例/绑定判断，不预填；代价是未运行不能算合格。
7. 原十一项、HP2/24/899和资源关闭门不缩减；代价是整项目标与Task1验收未完成。
8. 新J朝向等待和斜向L是其他实际输入所有权；代价是本增量不保证它们的移动安全。
9. Down反弹、爪、短喷及外部动量不纳入此自由地面预测；代价是无一般物理保证。
10. 生产物理/输入/地图/美术和继承脏改动不混进三基线审查；代价是它们仍须自己的资格。
11. 正式动作、美术/R-I/C/P、正常速度/硬件真人无证据继续OPEN；代价是不能给观感或手感通过结论。

## 当前文件绑定与留档

全部14案/outer73065明确终止后，root独立读取28份source-before/after及28份fresh-binaries/binaries-after清单：source文本唯一SHA256 `68319FDAEBFD12404E1394FD0B001E58B52C9339679453E848D49211AA64613E`，bin文本唯一`599279C69EFC5D2E4058B796FB53075551CD48B3604149860194202B9FA9DEA0`。重新计算当前1888个选定源码/导入实体、14个去重helper及6个Debug二进制，三组差异均0；当前Knight.dll `5C2B2EEA6CFF2DF8951A5BCDEAF2FA16D40CE965E18F651889A2478543992AB2`。这是当前文件绑定，不声称全部历史payload重算，也不把RED/编译失败混入最终同源14案。

Git仍branch `codex/task6-certified-final-20260902T0555` / HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6` / index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。CIM排除本只读shell后自有Godot/dotnet/SDD脚本进程0，不停止其他应用。

一次性容量检查2026-11-03T19:36:09.1711755UTC（本地11月4日）：过程媒体7,427,750bytes/86files，相对基线增长7,424,242，Notify=false；无新截图/录屏，G可用127,981,109,248bytes。没有建立监控或自动化。

原全部十一项、Task1最终验收、R-I/R-C/R-P及正常速度/正式视听/硬件真人仍ACTIVE/OPEN。下一为早期真实接触身份/候选危险的有界观察，而非放宽HP2或猜改防御优先级；正式动作和物理手感另须完整样段。没有GUI/系统按键、付费生成、新媒体、真实C存档写入、额外采集、运行时替换、自动化、Git写入/发布或删除。
