# v315 · 继续读档加载边界与攻击能量门槛

## 事实：当前资源错误的边界

工程仍为 `G:/knight/Knight`。v314 的原普通前段案 `prologue-pre-20261108-091514-429-ad45a410de064c3aa5030acfdfc06ad8` 保持严格失败：44 项执行完、engine 0，但五次 `Handle is not initialized`，不把 stderr 与 external 的重复记录算十次。

两条独立只读链核对确认，错误发生在第三根生命周期清理返回之后、第四根 Continue 重开的同步 `GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")` 内（`BasinSupplyRuntimeTests.CheckSupply:412`）；不是第三根首次 Load，也不是本案 staged AddChild。`sample` 只是日志的 site 标签，不是另一个场景文件。失败日志 external 278..439 是 Load 区间，五错为279/311/343/375/407；第四根尚未开始 Instantiate。

递归保存图静态核对为16个 tscn、108条不同 Script 路径；可定位 scripted Resource 数据子资源是7类／12个：父中的5 SectorDefinition及1 MinimapTerrainCatalog，Player中的PlayerTuning／MobilityModuleDefinition／PlayerEnergyTuning／PresentationProfile，Boss中的BossTuning／PresentationProfile。序列化ID不能充当实际运行对象身份，五错也不能分配给五个definition。新齿轮脚本在父→Basin依赖图内，但它继承Area2D，是Node脚本；同步Load不能证明其Node已经实例化。

当前同源正常Main／Explore确实共用同一PrologueLevel父入口。此前两个通过结果反驳“当前图每次加载必失败”，不证明任何新旧资源与重复清理后的问题无关。相机与当前劳工前置测试的所核回调均有退出撤销，未发现共享Tuning／Profile／Definition显式释放。相机测试的借用KinematicCollision2D使用using与生产借用策略不一致，尚无证据关联这五错，本次未改它。

## 一轮详细加载日志，不筛绿

首次 `-Phase prologue-pre -NativeVerbose` 被旧launcher准入拒绝，未启动引擎。为这项调查添加受限准入，只允许精确阶段和Phase／SourceRows／NativeVerbose三个绑定参数；任何额外选项即便显式false也拒绝，不改原生180秒、原44案例、普通编译符号、三日志错误门、存档保护或GC／缓存策略。新测试执行真实文件系统操作前的准入前缀：前置白名单调试不算RED，真正RED为旧入口拒绝“pre verbose”，随后最小改guard后31控制通过。新helper进入guard和冻结副本，诊断有独立case标签。

唯一新原生案：

`G:/knight/验证证据/october13-stable-claw/prologue-pre-resource-load-verbose-20261108-094933-521-7d6dc8aaae074854a020bcc43a635bb8`

- build退出0／27873ms；engine退出0／150609ms，未超时，原44案例和原退出清理均执行。
- stdout／stderr／external诊断计数均0，stderr为空，guardFailures=[]；严格诊断通过，但`verboseDiagnostic=true`、human=false、naturalRoute=false、wholeGoalAccepted=false。
- 本案第四根Load的external1927..2018、engine59329..59494ms记录正常父图再次加载，未复现五错。不能用此案给旧失败定位具体资源／句柄或关闭根因；没有第二轮重跑筛选。
- 1907输入前后SHA均`43F214C27588E654176DA593DF0C21741A5384D4B29A63A9C29A1B087D74F50B`，六产物均`6876CAFF88F67D954F27C07DA0B786F9545367165507C169BD67DA8AA737B0D4`，与v314失败同一游戏代码；helper前后`F5AE17D38BD06B418FDD9A1779A344859A3566E2D3CF3189159087B7531AD6CC`，真实用户存档清单前后`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`。

限定改动独立审查无明确C／I／M问题；审阅不代签实际native。旧canonical `test-exit-verbose-admission.ps1`的pre期望改为允许，其递归前缀白名单仍实际失败于既有“Unexpected executable command in read-only prefix”，没有声称全体PS helper绿色，也未顺带重构这个旧测试。

## 接续制作

资源根因仍开放；不能反复日志测试阻断其他已批准内容。接续验证既有候选 `Q-ATTACK-ENERGY-BOUNDARY-01`：ActionModes以Approved常数及0.0001容差判断，实际扣费模型以本次初始化设置严格比较，两者静态不一致。先在真实保存Player、游戏内J输入及公开能量消费路径上构造边界，观察RED后才修改生产规则。

## 已复现并修正攻击预算规则不一致

实际原生RED `actions-20261108-100044-603-cd5fba5caedd43db846628e63a33e2aa`：保存Player能量由公开heavy-guard消费控制至11.999947，成本12；自然落地／精度／新鲜J键沿／再生延迟检查均通过。旧逻辑CanAfford／CanBegin误为true，实际J不攻击、扣费0，又因RejectAttackRequest看到“CanBegin=true”而不发拒绝，attacks0／rejects0／reasonNone；在实际J后汇总三项不一致才严格失败，原始失败保留，不把夹具错误当RED。

随后root只修改两个生产文件：EnergyController新增只读CanAffordAttack，复用已初始化的同一_model.TrySpendAttack来计算投影，但不Apply投影状态；ActionMode改用这个查询。没有改模型、Tuning、默认消耗、能量恢复、防盾、攻击窗口、连击、移动或存档。这样既去掉临界容差分歧，也不再以Approved常数误判合法替代配置。只读查询使用当前初始化设置快照，与实际扣费相同；不承诺修改Tuning导出值即可热切换能量模型。

普通actions GREEN `actions-20261108-100157-849-db02fb35fae64b02ab4f9fa017d5b8bf`，engine0／10900ms、严格无原生诊断／guard空，原KEYBOARD_ACTION_RUNTIME_OK和退出清理也执行。新增六个字面量期望案例均来自实际保存Player、新建自有Tuning、真实游戏内J和公开消费：

| 配置与实际能量 | 期望／实际结果 |
|---|---|
| max100/cost12，11.999947 | 不可支付；不攻击、不扣费，1次InsufficientEnergy拒绝 |
| max100/cost12，12 | 可支付；1次攻击，扣12至0 |
| max100/cost12，12.000053 | 可支付；1次攻击，扣12，剩0.00005340576 |
| max64/cost16，15.99995 | 不可支付；不攻击、不扣费，1次InsufficientEnergy拒绝 |
| max64/cost6，6 | 可支付；1次攻击，扣6至0 |
| max64/cost0，0 | 合法零消耗；1次攻击、扣费0 |

每例16轮同步双getter轮询没有消费、状态发布或攻击／拒绝事件；此为16轮，而非总共仅16次getter读取。共享保存Tuning前后设置相同，新自有Tuning在树清理后Dispose。没有反射或直接写私有能量、HP、速度、位置来伪造按J后的结果；初始离树摆位仅为受控夹具，非正常路线声称。系统键鼠未使用。

当前修复源码1907输入manifest为`25044A8EEE00F3A4BAA6186984D289159431528EB5DC6870384FC35922A00B45`、六产物manifest为`56DBE48ED38E8818B6599CDFF0F8A2F13C4B94E6CA91552E27BBD706FB1BF088`。这是新产物，不借前述43F214旧版诊断或v314双路线给本版代签完整验收。完整普通前／后段、模型及独立审查另记后续结果。

限定生产／新增测试独立审查C0／I0／M0：两份v314生产基线hash与RED清单一致，测试新增helper／调用以外逐行保留原文本；busy／空槽／盾阻挡门不变。审查不代签native，六案例不外推初始化后替换Tuning、正成本完全空能量、多次新J键沿或全动作。新查询使用captured模型之结论来自代码和六种Ready前配置，不声称已经验证所有热切换。

后续完整普通pre `prologue-pre-20261108-100308-202-4edcfd1d33f34be8ba6797200da1787f`，engine0／146028ms、原44案例和退出清理通过；三原生日志零诊断、stderr空、guards=[]，source及六产物前后均与上述新版本一致。旧资源故障本轮未复现，仍不关闭根因。一次临时离线读回因Get-Content空文件返回null报错，已改为ReadAllText重读并逐项核对三个sink；这不是引擎错误或额外原生重跑，也不以该脚本外层exit0当检查通过。

最终普通post `prologue-post-20261108-100622-889-cd40206f08ca465489510b10efe9835d`，engine0／191024ms，原75案例、retired=0、8次父场景寿命及退出清理完成，严格通过。stderr／external各记录同四条原合同负例警告（observer1、RoomPresentationProfile1、Completed→Recovering拒绝2），不算八条独立警告；ERROR／FATAL均0，stdout诊断0。沿用既有300000ms，不新增预算，也不签旧180秒性能。source／six-bin前后与上述新版本相同，真实用户存档清单不变，guards=[]。

最终models `models-20261108-101026-984-0d6d2b1a57ea4c0b99aa5c6f08fb7647`，engine0／499ms，三日志零诊断、guard空、同source／six-bin。根退出后逐项再读当前1907输入／6产物／9个本案helper，0差异。四个当前普通阶段actions／pre／post／models全部严格通过；与旧版本verbose诊断分开，不签本版完整Main／Explore、正式画面／连续动作／音频体验、硬件性能或真人，也不关闭历史资源根因。

## 留档与下一步

本轮没有新过程图稿或视频。正常汇报前只运行一次既有媒体`-Check`：15,203,531字节／145文件、Notify=false，未达新增20GiB；没有恢复自动化或独立提醒。

`Q-ATTACK-ENERGY-BOUNDARY-01`的上述受测矛盾已修正，不能扩称所有攻击／防守或UI完成。原十一项继续active，moving-gear Task3仍待当前整体验证及正常体验；先补尚缺的机关多shape／回调失效证据，再继续敌人造型区分与正式连续动作制作。资源句柄未知对象不通过keep-alive-all、强制GC、缓存模式、sleep或引擎改补丁猜修。正式角色仍按批准A外观基准、B结构参考及既有肩背／悬浮灯功能约定，未生成或接入新的未审美术。

仅无窗口，未打开游戏GUI、发送系统输入、付费生成、原生dump、改引擎／GC／缓存／预算、写真实存档、Git发布、删除或恢复自动化。原十一项和moving-gear Task3保持未完成。
