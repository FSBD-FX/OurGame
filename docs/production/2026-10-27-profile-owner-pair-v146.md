# v146 · 双持有者短流程与玩家配置终结器

## 结论

本轮获得一次23.849秒的短流程原生FATAL，失败对象是PlayerTuning，而非上轮追踪的ResidentSubtypeProfile。另一次相同短流程确实观察到目标Profile自然终结器返回。它们改变下一步排查，但不证明相同根因、确定性复现或资源修复。

最终普通九项7 PASS / 2 FAIL；Main和Explore仍死亡恢复。pre本轮通过，post原75＋owner8及四条精确预期warning通过；旧句柄错误、旧FATAL与本轮短流程FATAL全部保留，不能因一次完整通过而关闭。

KNIGHT / Godot.NET仍在G:/knight/Knight，1857运行文件/897个C#。原十一项ACTIVE/NOTSEEN、未缩减或勾整项。用户继续无窗口的选择生效：无游戏窗口、系统键鼠、真人认可、新美术/动作/手感或过程媒体。无付费、自动化、UE/引擎切换、Git写入、资料删除或真实存档语义读写。

## 新入口与测试

只改已有条件编译Census runner、canonical脚本，新增纯PowerShell合同助手；没有新运行文件/UID，也没有修改生产键位、瞄准优先级、伤害、盾、能量、地图、敌人和美术。

独立阶段prologue-profile-lifetime-pair必须使用REF身份模式；普通模式、大小写变体、混合标记与豁免在构建前拒绝。按顺序调用两段原有、不变的方法：

1. ProloguePlaytestRecorderTests.RunTask5FallContextContracts。
2. PrologueLevelRuntimeTests.RunGeneratedMainDeathOnly。

保留原输入夹具、恢复及音频清理，不冒称纯键盘正常通关。只临时设置原post同样的fallback标量并在finally恢复。没有新增资源pin、主场景静态根、Load/Instantiate/GetState、GC/终结器等待、分配诱饵、清理日程或更大watchdog。移除前缀是隔离变量，不替代完整post资格。

助手从实际canonical AST提取函数，验证无符号64位ID、合法跨owner共享、自然终结器覆盖及错误证据。终止进程后才分类目标；not_exercised与returned_not_stability均不是稳定通过。

## 实际正对照1128

证据根G:/knight/验证证据/october13-stable-claw/。案例prologue-profile-lifetime-pair-refcounted-identity-20261027-194218-211-11283c8d3cc4456e8376786cec59e7db：构建0警告/0错误，native0/7212ms，1532行，三通道无ERROR/WARNING/FATAL。

engine.stderr.log中第一目标为Profile serial36/hash30308814/native9223372143809531120，owner569e6e438a564834a4f14cfb8576df77、epoch2/reset0、operation3。

| 行 | 实际事实 |
| --- | --- |
| 103–110 | Load出生、赋值及非空gate；actor107038641356、binding107290299637、sourceScene9223372143826308337/cache15。 |
| 111–135 | 配置、挂载、激活检查，generation1提交，准备结束。 |
| 890/893/921 | generation1解除持有，generation2提交并解除持有；不是原生死亡证明。 |
| 924/927/949 | owner开始清理，精确场景缓存引用清空，缓存清理返回。 |
| 1063–1064 | 同serial/hash/native的自然终结器call63 ENTER＋RETURN，thread1。 |
| 1113–1119 | 第二owner145a…随后加载同类型，serial132/hash37888963/native9223372292807986800、sourceScene9223372292824763588/cache126、非空gate。 |

A的终结器发生在B已加载rock/ridge、operation26仍活跃时，但在B的scrap加载前返回。v145失败788则在后次空字段/拒绝后才终结并FATAL。时序不同，不等于加载或缓存清空是原因。修正后的判读器也已重新读取1128实际原始日志，仍为returned_not_stability，不只字面测试通过。

## 新失败283b

案例prologue-profile-lifetime-pair-refcounted-identity-20261027-195847-293-283b4cba25d54c1eb21fc9641c4f0638：构建0/0，native -1073741795 /23849ms，非超时，严格FAIL，pairVerdict=null。

- 37 ENTER/36 RETURN；stderr950唯一未配对call37：PlayerTuning serial1/hash56200037、disposing=False/thread1。
- 951–957为PlayerTuning.Dispose:116→base.Dispose→Finalize后的gchandle.is_released FATAL。
- 原owner a932…的缓存清理返回在949；stdout已经进入第二方法，但不同流不能证明第二次Load的精确时点。没有第二owner的journal行。
- serial1没有出生栈、赋值身份、原生映射或更早Dispose。native_identity=unknown只表示未记录，不表示没有原生对象。
- 它不是旧Profile788。类型不同也不自动证明共同根因、坏资产、重复Dispose或居民缓存错误。

相同短流程既有自然返回，也有FATAL。后者是新增有效失败证据，不是判读器把成功误判为失败；不能靠前者筛绿。

## 官方源码核对：事实、推理、风险

agent-reach只读流程解析到精确官方提交a13da4feb8d8aefc283c3763d33a2f170a18d541。本机无gh，改用官方网页与内存HTTP，未安装/认证/落盘下载。实际原始文本复核行号，不把网页归一化行号当仓库行号。

1788行检查原生保存的handle是否为空，不是资源文件有效性检查。[入口](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/csharp_script.cpp#L1785-L1819)、[handle定义](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/mono_gc_handle.h#L58-L69)。

基类终结器进入Dispose(false)，显式处置抑制终结器，原生调用之前有已处置和关联target检查。[基类](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/glue/GodotSharp/GodotSharp/Core/GodotObject.base.cs#L98-L150)、[原生调用者](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/glue/runtime_interop.cpp#L167-L193)。

强弱handle转换也有清空后再赋值的路径，所以入口不变量违反不等于重复Dispose或已确认竞态。本机没有转换历史，不宣布引擎bug或修复。[转换代码](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/csharp_script.cpp#L1855-L1906)。

## 唯一复核和一次C/I处理

本轮新范围唯一复核C0/I4/M0，不重复复核v145。四个当前实际canonical反例均具名RED→一次修正GREEN，无第二次复核。

| 项 | 修正 |
| --- | --- |
| I1 格式错误/未知行被忽略 | 枚举全部journal/disposal命名空间，已知Kind与完整消耗标量schema；合法非目标资源允许。 |
| I2 身份弱关联 | 复用原assignment验证并联结实际assignment/serial/hash；准备epoch/reset/slot/path一致，终结器同call同thread；拒绝字符串/浮点/范围错误。 |
| I3 重复提交复用释放 | 唯一custody键，一对一release；额外release/重复commit拒绝。不同generation允许，不强加缓存清空与解除持有的先后。 |
| I4 混入普通资格标记 | 三个通道任一混入普通通关、post完成或其他runner资格标记均拒绝。 |

原新旧诊断合同0303仍通过。没有新增Minor，v145-M1及所有历史延期继续OPEN；完整稳定性、前缀独立、任意GC时序、物理死亡、根因及正式视听/硬件真人不因此获认证。

## 16次尝试索引及最终九项

日期均20261027，时间/UUID唯一标识目录。前七案不冒充最终普通分数：

| 阶段 · 时间/UUID | 结果 |
| --- | --- |
| pair · 194005-487-54358209d49a4721ad13c82ff9b59acb | 缺mode源路径preflightFAIL，无build/native，非RED。 |
| pair · 194047-240-9bc998e7c9ba4763923ac99fc24fc1ff | 干净build，PROFILE_PAIR_NATIVE_RED，native1/533ms。 |
| pair · 194218-211-11283c8d3cc4456e8376786cec59e7db | 自然目标返回，native0/7212ms，不等稳定。 |
| pair · 195847-293-283b4cba25d54c1eb21fc9641c4f0638 | PlayerTuning1 FATAL，23849ms。 |
| census-contract · 195944-495-0303efa9867044dd9a2501b9d99c73d2 | 原诊断合同PASS，native0/1336ms。 |
| normal-absence · 200013-343-a7f84322f6754513a70e749a401e6c44 | PASS，native0/402ms。 |
| models · 200041-868-21f3e284576a4a849acf52da96f78372 | PASS，native0/412ms。 |

普通DLL/PDB与v145不同，未猜差异来源或套旧分数，按计划重跑整套九项：

| 阶段 · 时间/UUID | 结果 | native耗时 |
| --- | --- | --- |
| stationary · 200409-092-2f47e012d924403a8d33cc12da1957c3 | PASS | 1256ms |
| models · 200440-572-977e1cb7fab643afa8123a1db7507d6c | PASS | 636ms |
| keyboard · 200514-188-b8d762e8d0e84d32b89d1d08c6dc6dc8 | PASS | 11906ms |
| traps · 200604-733-ec66c76f5e1c4df8912476e3d0440fa6 | PASS | 28020ms |
| Main · 200702-997-19ad39f87ba94a1f91a408ce5f91784a | FAIL，physics6617恢复1/0 | 128490ms |
| Explore · 200943-807-fba5fc517b4f4048b0fb7efec3e5a31c | FAIL，Grip1 physics7747/HP1→0/恢复1/0 | 138100ms |
| pre · 201231-773-1e4d5d18e6a44061a57e76b705ae3ae9 | PASS，三通道干净 | 153204ms |
| post · 201534-025-1dec411806fd40088e68b07871502af3 | PASS，75已执行/0退役/75已分类＋owner8 | 200044ms |
| absence · 201930-984-76f4cc7431444bdca895551f61ba045e | PASS，加载程序集无诊断类型 | 420ms |

post四条warning由seamless1、room-profile1、flow-registry2三个精确作用域覆盖，无ERROR/FATAL。原300秒完整普通门未放宽。两条路线109项字节保护通过，不等真人或正常显示证据。

总计1案preflightFAIL、15次干净build/native，native11次退出0/4次非0；严格11PASS/5FAIL，含有意RED、短流程FATAL、路线失败，不是玩家通关率或稳定性统计。

## 保管、普通模式、媒体

独立只读核对截止2026-10-27T12:24:40.4926845Z，与v145的22案分开：60组前后source/helper/user/continuation清单＋15组binary清单相同；7072源码副本＋178helper副本与自己的清单一致，90份封存二进制一致；30个记录的PID+CreationTime身份终止，0超时/0散列不符。

最终13×1857/897与当前独立路径集/散列一致，125条helper记录一致；最后11普通案例66份二进制与当前一致，两个诊断epoch分别保存。5435只有136份无manifest可核验的partial-after源码，无build/native，明确排除统计，不伪补/删除。两段委托及teardown与v145字节相同；不证明其他Godot全局空闲或整项目正确。

- source manifest：D04D1B93242F0C6A4609501FFABDF262DC8D4A77FC8C1A28DABD8E12A1F707F1
- normal binary manifest：100EC6354FBFFD60AB9AB9312268EA60B83E4200E0114022676F074286E23103
- Knight.dll：D5A1346A1A929EB315160F50C42CCE00B1E20FFFD65C7167E040826DFFFCDA37
- Knight.pdb：6CD0BA59D0D1EBE0A3B559A6AD7E0EA50CB9BDD4BC6095A9C6FD57CB850504C8
- canonical：67B6053533B49F86225D5DE7BA56B17E8272C65032163446530D39765F92EE66
- pair helper：0FBEFE42619935D1D6B86D3BAFEBC6BA504ED089BEAA7B596342902B1A41421E
- Census runner：0C6F9E8B125E1E8FECCBE6B3702EFDF8A40F98902CA1CB8F96DBE1CFC1281F9C
- index：21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C
- HEAD：e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6

无新媒体。2026-10-27T12:26:32.7486961Z手动Check：1431609字节/16文件、基线3508、增长1428101、Notify=false；不重置/建定时任务。

## 裁决、风险与下一步

1. 上轮和本轮都是PROGRESS：新短流程失败与自然释放对照改变下一步，不是只重述状态。原十一项继续ACTIVE。
2. 选择独立观察入口而非猜资源修复；移除前缀的代价是干净结果不能反证完整post，失败不能直接等同旧788原因。无加cap/GC/保活筛绿。
3. 只规定每个release晚于自己的commit，不规定clear与release通用次序；允许跨owner共享ID。代价是仍需原生转换历史解释故障。
4. 四项Important实证修正；复核范围外的原生命周期、普通玩法、历史脏改动、正式视听/硬件真人等不冒称重新认证；旧Minor都保留，不删工作记录、不用工作树/提交/清理默认步骤覆盖用户约束。
5. 因普通二进制变化完成九项新回归，七项通过不关闭本轮FATAL和全十一项。无第二复核。

下一先辨认PlayerTuning1来自哪里。保存的Player.tscn包含Resource_y7r3h并供Motor/Combat共用，Motor、Combat和表现timeline也有默认/后备new PlayerTuning。早期serial与邻近其他资源栈不能区分。

最小下一证据是托管构造栈、当前实际new返回及既有Ready赋值边界的安全标量身份，区分加载export、被覆盖默认与真正fallback。构造期间不加原生查询，不增遍历/GetState、强根或GC；不猜删默认值/修引擎。之后才依据实证修复，再推进两条路线。

正式美术、连续动作、UI/音频、丰富地图/敌人机关、完整Boss与有支撑出口、存档兼容、长期资源稳定、目标硬件及真人认可都仍是原十一项交付要求，本轮没有缩成诊断任务或宣布完成。
