# v150 · 前段资源故障边界调查（代码仍为v149）

2026-10-28，G:/knight/Knight。原十一项ACTIVE/NOTSEEN，当前不是新玩法或正式美术交付。上一轮v149是实际进展：边缘预警生产修复与具名RED→GREEN；本轮根据新复现的前段13 Handle ERROR调查其前置与资源身份，不把状态复述当进展。

本轮没有修改运行源码、场景、canonical/helper、资源释放策略、GC、帧数、watchdog或存档。没有可见窗口、OS键鼠、付费生成、自动化、安装、引擎替换、外部写入、Git变更或删除。只运行四个现成隔离入口，保存原始失败，最后恢复普通构建。

## 事实、推理、风险

**事实：** v149 ordinary pre 的13次异常都在 BasinSupply.OpenSample 的同步 AddChild 内，7次来自 NewGameDuringHealing 第一次打开、6次来自 ReturnToTitleDuringHealing 第一次打开，早于伤害/治疗回调。它是整个场景及相邻分区的同步挂载边界，不能据函数名字认定补给Area拥有坏句柄。

**本轮收窄：** 单独完整补给四案例/五根诊断安静完成；保留完整pre时，另一轮在第一 Camera 案例后便发生明确 ResidentSubtypeProfile 终结器FATAL，尚未进入 Supply。现成短前缀入口则在55秒原上限处超时，连 Supply 前的边界marker也未到达，不冒称它复现了补给错误。

**风险：** 诊断会改变日志量/时点；相似符号、同一次后续失败、静态类型匹配或安静重跑都不是因果闭环。既不据此认定所有缺陷是同一根因，也不排除当前版本时序/调用的影响。当前资源稳定性仍FAIL；不删除默认配置、不加保活、不强制GC、不改cache或放宽时限取绿。

## 四个原始案例

根 G:/knight/验证证据/october13-stable-claw/。

| 案例 | 原入口/模式 | 结果 |
| --- | --- | --- |
| basin-supply-lifetime-refcounted-identity-20261028-002842-034-4a63fd40927a4f85b1dd0f307bf2599e | 原完整Supply/身份符号 | exit0/13203ms，4 CASE_RETURN/5实例根，诊断完成，不等行为或故障修复认证 |
| prologue-pre-refcounted-identity-20261028-003059-539-fb4a6ebd8d004453bd98d1637464e801 | 原完整pre/身份符号 | exit-1073741795/25845ms，原生FATAL，未进入Supply；无超时 |
| basin-resource-pre-prefix-20261028-003800-698-dbae61fdd917427d9c459c052d273110 | 原前缀/普通构建 | TimedOut=true/55066ms，55秒原有host cap；最后为CurrentBasin负控，未出现BASIN_RESOURCE_BOUNDARY_ENTER；不增加预算重跑 |
| thruster-profile-trace-normal-20261028-004435-141-fe5c3d7246ae489c8528f12e9c33204d | 原普通程序集缺席验证 | exit0/432ms，实际加载程序集 lifetime/native-identity 类型缺席，普通二进制恢复 |

没有将 diagnostic-finished 或普通程序集缺席测试当整套PASS；本轮没有新完整Main/Explore、GPU、声音、正常速度或真人体验证据，v149普通pre13错及所有历史FATAL/双路线失败继续OPEN。

### 短Supply的实际身份证据

41条 PROFILE_LIFETIME_ROW：born11、player-tuning-created10、player-tuning-assigned10、manager-dispose-before5/after5。五根的Motor/Combat实际共享同一已记录导出Tuning serial1。这里只证明这一次的分配/赋值与结束；不能跨运行用serial1指认旧崩溃，也不能说“补给挂载永远没问题”。

### 完整pre的新FATAL证据

最后未返回的 REFCOUNTED_IDENTITY ENTER 为 **call142 / disposing=False / ResidentSubtypeProfile / thread1 / serial67 / managed_hash31414149**。已观察原生ID **9223372168623034457**、路径 **precision-missile.tscn::Resource_qv7df**，随后是 gchandle.is_released()，mono_object_disposed_baseref:1788 → GodotObject.Dispose(false) → ResidentSubtypeProfile.Dispose → Finalize。不能用同数字跨进程关联其他案例。

同一次1893条scalar journal中的精确关联：

- seq285：serial67真实出生，thread2、ResourceLoader.Load→ResidentOriginalSceneSources.Get，owner a9207bc37d844166a57f656970951ac8，op10，controlled-abyss.05。
- seq287：实际nonnull gate，binding132103802974、actor131852144646、scene9223372168639811674、cache15；后续配置/挂载/激活返回并commit一代租约。
- seq955：同一资源/nativeID在op34、steam-valves.05赋给binding192115905247、actor191864246975，同scene/cache；同样完成挂载/激活/commit。
- seq1493/1515：两actor的lease-release；这些仅是托管保管记录，绝不当native死亡证明。
- seq1525/1559：该owner dispose前/后清cache记录；不据时间邻近认定它错误释放了借用资源。
- seq1891–1893：另一个owner e810ed0abec843f4a2b79a2f1cb5ef17、op67重取同叶，source-valid-return的scene9223372771529068629/cache224；随后profile_missing=True、actual_state=not_evaluated的spawn_exception拒绝。它与thread1的call142交错；不能推成“两个调用为同一原生现场”或说已抓到最初释放者。

FATAL发生前仅出现第一个Camera案例/重试保留居民的实际收据和原audio drain，没有BASIN_SUPPLY记录。因此“只改补给”不是目前可靠修复路径。thread编号属于此次托管线程，不用async栈中的ThreadPool字样擅自断言Godot调用在错误OS线程。

## 两路只读核对

profile_fatal_recon、supply_scene_handle_recon_v150已结束，均未改文件或运行进程。

- 原pre前缀是 Camera→CurrentLaborerClearance→ClawTarget→HealthRestore。ConfigureLegacyWorld只是替换实例Definitions；没有发现借用PlayerTuning/Profile/PackedScene的显式Dispose。CurrentLaborer释放的blockerShape是该测试自己new的资源。
- Camera里using GetSlideCollision并非已证错误；现有快照合同已验证两次getter产生不同原生快照且持有peer不失效，不能据怀疑撤掉清理。
- 对十个直接相关保存场景224个脚本赋值作静态核对，未见错误native基类/SetScript重绑；7处直接继承差异有合法继承链。相同脚本的父场景override不是已证错误。
- Player本体故意是无脚本CharacterBody2D，行为在其子节点。PlayerTuning确实为Motor/Combat共享subresource；Profile、mobility/energy tuning、五sector definitions、Minimap catalog及样段敌人配置均有明确脚本映射。
- 原生错误Logger没有offending ObjectID/Node参数；现有SwapGCHandleForType签名也没有owner。最后一个NodeAdded只是调度邻近，不能指认为出错owner。捕获带对象Variant的局部变量可能保活，未将它当被动观察引入。
- 历史FileListAbsolute中的迁移路径只是构建账本，不是当前隔离二进制绑定过期的证据。

## 官方记录的有限核对

使用agent-reach路由：本机gh不可用，mcporter Exa元数据失败后以网页只读后备，仅采用Godot官方仓库。没有下载运行陌生复现。

- [#112067](https://github.com/godotengine/godot/issues/112067)当前页面open/confirmed，有同类缓存资源弱handle转换错误；不是本项目owner证明。
- [#124587](https://github.com/godotengine/godot/issues/124587)页面open/needs testing，报告4.7.2也可发生，维护者要求MRP；其建议仍不是此项目已证/已发布修复，不据此升级引擎。
- [#112943](https://github.com/godotengine/godot/issues/112943)页面closed/archived；关闭状态不能被当成该FATAL已修复。

以上仅支持保持原生生命周期调查，不支持项目已经安全、具体资源损坏、跨案例统一因果或盲改引擎。

## 后续权限与保管

PATH以及两个常见SDK/WinDbg位置未找到现成原生调试器，不宣称整盘不存在。已向用户提出异步选择：是否允许准备微软官方原生调试工具并只对隔离KNIGHT进程做一次崩溃采集，可能需手动安装。回复前不安装、不改系统策略、不替换引擎，不把询问当授权。这个资源路径需要新的诊断能力，但不将整个十一项目标缩成一个调试任务或擅自暂停/完成。

只读独立 fog_edge_custody_v149 本轮审计已结束：四案20清单对逐字节一致，1864选定源码副本+54helper副本+24二进制符合各自清单，0不符/未列名；不是全源实体保管。每案source1857/user元数据109行，不读取真实存档正文。Supply Dispose ENTER/RETURN为147/147，Pre为142/141；call142没有RETURN，不以Console最后一行替代关联。8组合同/native/wait PID一致、8确切PID查询全不存在，含超时引擎PID19304；超时不称自然完成，也不称全机空闲。

当前独立枚举仍1857/897、v149 source A1F04FDC0E316E599ACF48419E396797C431233B263F6058FEEE4A0F88C9F9D8；canonical6A7ED7DD79C7A358489F747635CFC0DDAF9D518D8E2A4BE6B2641F439F150D0C。6份当前普通二进制与v149-final及最后normal manifest C717075226151A11AE08F1A35F9E173A636179CD4BE1234776B8E3CB75E5B6C1相符，身份构建保持独立epoch7EE4A7B7…，不混为同一普通程序集。HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6/index21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C未变。实际类型缺席验证通过，不等全局资源释放证明。

本轮结论是新证据改变下一调查边界：资源FATAL可在Supply之前出现，不能继续仅围绕治疗回调/补给Area补丁；静态核对未给出可靠C#所有权修复。没有新的实现计划或生产补丁，所以不虚构TDD/新最终代码审查；两路代码核对与独立保管审计均为只读。原十一项及所有正式交付要求保持完整、ACTIVE，工具权限询问未答复不等于全目标暂停。

手动媒体Check于2026-10-27T16:47:20Z成功：1431609 bytes/16文件、新基线3508、增长1428101、Notify=false。无新图稿/视频或自动化。
