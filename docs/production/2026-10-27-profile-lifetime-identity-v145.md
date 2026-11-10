# v145 · 配置资源出生、持有与释放诊断

## 结论

本轮补齐了一条实际失败配置资源的出生、场景来源、配置、挂载和管理器持有链；修正了诊断自身三处归因问题。**这不是资源崩溃修复，也不是新的正式美术或完整关卡交付。**

最终普通构建九项 **5 PASS / 4 FAIL**：模型、键盘、齿轮弹板、防守专项、诊断排除专项通过；Main、Explore、pre、post 未通过。普通 post 再现原生 `gchandle.is_released()` / `GodotObject.Finalize()` FATAL。pre 虽退出0并输出结束标记，仍有两条未解释的句柄错误，不能算通过。

当前仍为 **KNIGHT / Godot.NET，G:/knight/Knight**；原十一项目标 ACTIVE，未缩减、未勾整项。用户最新选择“暂不方便，继续无窗口工作”继续生效：本轮没有显示游戏窗口、发送系统键鼠输入或取得新的真人/视觉认可，NOTSEEN 保留。没有修改另一个 UE 项目，没有付费生成、自动化、Git 写入或删除用户资料。

## 事实：本次实际改了什么

运行文件1857，其中C#897；新增一个全部受 `REFCOUNTED_FATAL_IDENTITY` 控制的诊断文件，修改九个已有C#及一个验证脚本。普通生产键位、瞄准优先级、盾角、攻击/能量、敌人、143实体、9残片和76普通敌人配置、地图及美术素材不变。

- 新增 `tests/diagnostics/ResidentProfileLifetimeTrace.cs`：最多8192事件、2048关联、32层嵌套、4096字符创建栈。记录行和作用域只保存托管标量、字符串及上下文；不持有Godot资源，不用原始指针认定身份。
- 在真实配置构造函数记录已有托管序号；构造期间不增加原生查询。原生身份仅在已声明主线程的非空检查通过后、`ConfigureActor`之前观察，不能将该记录称作配置完成或挂载成功。
- 准备作用域始于实际缓存获取之前，贯穿原有事务及清理。缓存命中/加载、配置返回、挂载/激活返回、原有检查通过、提交和释放分别记录，不重复执行原有授权或有效性判断。
- 挂载/激活可重入，返回后新增观察只用此前保存的actor ID。缓存清空只记录已保存的场景ID，不原生销毁借用资源，不通过ID重新创建包装对象。
- 缓存/管理器事件同时保留当前准备上下文和明确的 `SubjectOwner`。早期拒绝不会借用外层准备标识；`lease-release` 只在原有Remove/Detach完成后记录。
- 新增普通构建专项：从实际已加载程序集验证诊断类型不存在，且三个日志通道没有诊断事件。最终普通DLL/PDB已恢复，并非以诊断构建交付游戏。

未添加强引用保活、强制GC/终结器等待、额外游戏Load/Instantiate/GetState、引擎替换、缓存策略或资源生命周期修复。原有测试自己已有的清理流程不在此冒称新修复。

## 事实：实际失败资源的身份链

证据根：`G:/knight/验证证据/october13-stable-claw/`。

诊断 `prologue-post-refcounted-identity-20261027-183223-008-44d432c10f6346eab1d9a726b529114a` 的 `engine.stderr.log`：

| 位置 | 实际证据 |
| --- | --- |
| 5268–5271 | operation110、owner `44b7105740b546099352a65bc151de51`、epoch2/reset0、`low-ceiling-bridge.03`，加载期间创建Profile serial788/hash5775099。创建栈为ResourceLoader.Load→缓存Get→TrySpawnCore→协调器Prepare，3192字符未截断。 |
| 5273–5275 | 同一serial/native ID `9223373636495217497`，嵌入资源 `scrap-scatter.tscn::Resource_lut5t`，sourceScene `9223373636511993441` / cache767 / actor `1599724325922` / binding `1599975984026`。非空记录在配置之前。 |
| 5276、5286–5289、5299 | 同actor完成配置返回、挂载与激活原有检查，提交generation1、sector `1575363810172`。 |
| 6055、6058、6086 | generation1释放入口、generation2再次提交、generation2释放入口。**此案是修正I3前的旧格式，这两条旧release行只证明进入释放，不独立证明已Detach。** 后续generation2提交是不同的实证。 |
| 6089、6092、6114 | 原owner进入Dispose，实际 `_scenes.Clear()` 之后记录精确cache767/sourceScene清空，随后记录管理器缓存清理返回。 |
| 7382–7389 | 唯一未配对Dispose ENTER call1193，serial788/hash5775099，disposing=False，随后原生FATAL；158066ms，退出`-1073741795`，不是超时。 |

1193 ENTER/1192 RETURN；没有观察到788此前的显式/终结器Dispose，也没有另一个序号被观察映射到该native ID。这不排除未观察到的原生/引擎活动。

同slot/path的后一次operation134属于不同owner `e401…`、cache1161、新sourceScene；其setter收到null/serial0。不能把它的缺失字段等同于旧788，不能依据复用的原始指针认定同对象。FATAL后夹杂的新owner主线程清理也不能倒置为旧对象崩溃的原因。

## 推理与风险

事实支持将调查收窄到“旧管理器持有结束后的嵌入Profile终结器”，不支持断言资产损坏、引擎bug、重复Dispose、缓存清空是根因或强保活能修复。

修正标签后的完整诊断 `…185821-932-4b8f07fee4b84505adc27e14ac13a5a3` 没有FATAL，但180058ms触及既有180秒上限，保留8192行、1655对Dispose，原post仅走到case30，**仍FAIL**。第8192行在operation268的原始激光兵缓存命中，不是生命周期结束；未取得完整终止资格。

其中4742行是缓存命中，4634行是九个原始敌人来源的反复命中，占全部记录56.6%；遍历/相机前缀占3138行。不能以“没有再次崩溃”称修复，也不通过增大容量/时间或删失败记录补绿。下一诊断应隔离已存在的自然持有者周期，同时保留原完整post作为资格门。

## 唯一复核与一次C/I处理

唯一新上下文复核：C0 / I3 / M1。I1–I3均先在实际原生测试中得到具名RED，再修正。没有第二次复核。

| 条目 | 处理与证据 |
| --- | --- |
| I1 活跃准备覆盖真实缓存/管理器owner | 独立SubjectOwner标量，保留两种身份；真实缓存清空与管理器事件的跨owner控制RED→GREEN。 |
| I2 早期嵌套TrySpawn借外层准备身份 | 只在本次调用建立准备作用域后输出准备阶段拒绝；真实管理器早期unknown-slot控制RED→GREEN。 |
| I3 release记录先于实际解除持有 | 观察移到原有Remove/Detach之后，不移动或重复生产状态操作；真实native actor/Health/租约/TreeExiting控制RED→GREEN。 |
| V145-M1 延期 | PreparedActorId在operation0可能借到旧无作用域actor。正常manager先建立非零作用域；建立失败会先否决完整性，但未知身份返回仍需后续收紧。本轮不顺手修Minor；此前全部延期项继续保留。 |

原控制使用完整地图/真实Rust时，三个行为控制通过，但退出stdout有207 ShapeRID泄漏，严格FAIL。改为完全自有的五区、无shape原生世界及最小接口body+真实Health；不冒称敌人战斗或地图证明，不销毁借用素材、不加GC/等待。新fixture再次对三种错误进行原生突变RED，再恢复相同修正。最终完整新旧诊断合同 `…185648-690-5d5e918573e74964ac0cc912d1459244`：构建0警告/0错误，native0/1343ms，三通道均无ERROR/WARNING/FATAL，guard0。

## 最终普通构建九项

所有案例同一1857/897源码，重新普通Rebuild；未放宽错误、预算或原75+owner8后段要求。每次都保留stdout/stderr/external，而非只看退出码。

| 阶段与案例后缀 | 结果 | native / 耗时 | 说明 |
| --- | --- | --- | --- |
| stationary-guard · `190154-993-6b4ae26e850b4de58ef9f41e0399d9e1` | PASS | 0 /1260ms | 原固定支撑/真实W/L及负控。 |
| models · `190230-184-54cd643fc4054582b328a7cf4f13ab77` | PASS | 0 /506ms | 模型合同。 |
| keyboard · `190301-625-9991e05a41584c219b6f6f1ffa8d6e45` | PASS | 0 /11903ms | 原键盘回归。 |
| recycling-traps · `190345-104-5bde83e0f7d1453cb3829b4bdd4a9d2d` | PASS | 0 /28154ms | 原齿轮/弹板/引导回归。 |
| Main · `190443-323-3652df7b62a5490999d960986f407f8c` | FAIL | 1 /120122ms | 混战死亡/恢复，physics6617，recoveries1/0，不满足无恢复整程。 |
| Explore · `190718-023-c155fb923469428a8c8c21ea19f6b79a` | FAIL | 1 /143566ms | AscentGrip1骨骸命中死亡，physics7732，recoveries1/0。 |
| pre · `191013-914-2607cc5fb270489595291802790513a0` | FAIL | 0 /161174ms | 虽有结束标记，仍有两条Handle is not initialized错误，stderr/external镜像，不重复算成四条。 |
| post · `191326-419-4799d071edd84c9ba7057bf1715ce38c` | FAIL | -1073741795 /155682ms | 原生gchandle/Finalize FATAL，完成门缺失；保留既有两条已进入的预期warning作用域，不能当完整post。 |
| profile-trace-normal · `191637-739-eab51bdd0db5428f874e073549497f95` | PASS | 0 /425ms | 实际加载程序集无诊断类型，三通道无诊断事件。 |

## 全部22次尝试索引

日期前缀均为20261027；下表前十二行为`resident-census-identity-contract-refcounted-identity-`，44d4与0e81另标。其余十案见上表与诊断段落。

| 时间/UUID | 原始结果 |
| --- | --- |
| 182443-475-3e8df14100d14ce2946ad06ccfa098a2 | 编译通过，实际PROFILE_LIFETIME_BIRTH_RED，native1。 |
| 182657-143-00ead7dd11a94043a469e9656cc5fef3 | 构建FAIL：9处CS0104/0警告，无native；不是行为RED。 |
| 182821-943-106b5e6575374f73b55ae3a499954db0 | 新旧合同PASS，native0/1082ms。 |
| 182938-849-b571c889deec4fc7b1c4e22368fe8c0f | 实际PROFILE_LIFETIME_CACHE_RED，native1/619ms。 |
| 183059-939-71f3116c0d68465ba864918f6c31e6bd | 新旧合同PASS，native0/971ms。 |
| prologue-post-refcounted-identity · 183223-008-44d432c10f6346eab1d9a726b529114a | FATAL，精确788身份链，非超时。 |
| thruster-profile-trace-normal · 183552-030-0e81380ed6234755b83e928afbd7c32b | 普通排除PASS，native0/416ms。 |
| 184725-522-775d4c74c4ad4e04a17dbcca1d470c3d | I1/I2/I3实际具名RED，native1/1160ms。 |
| 184829-358-fb0cc427a8f24525ae65418bf94e31ac | 行为通过但207ShapeRID泄漏，严格FAIL，native0/2273ms。 |
| 185413-159-808dff2f165b458ebe0340e9d0dfe9ed | 构建FAIL：3处CS1061及1条CS0219警告，无native；不是行为RED。 |
| 185536-169-f8470248137947dfb03d1d7ac9f91a57 | 完全自有小fixture三种突变均具名RED，无附加原生泄漏。 |
| 185648-690-5d5e918573e74964ac0cc912d1459244 | 恢复修正后完整诊断合同PASS，native0/1343ms。 |

共20次成功构建（均0警告/0错误）、2次构建失败；20次native，退出0为11次、非0为9次。严格PASS9/FAIL13，包括有意的RED、构建失败与诊断超时；这不是玩家通关率或崩溃率。

## 保管、普通模式与存储

独立只读有界核对截止2026-10-27T11:20:20.5430993Z：88组source/helper/user/continuation前后清单字节相同，10348选定源码副本＋300helper副本与自己的清单一致；20组fresh/after binary清单及120份封存二进制一致，42个记录的PID+CreationTime身份均终止，1次超时，0散列不符。两个构建失败案只有四组清单/选定副本，无成功fresh/native材料，明确保留缺口，不伪补。

最后10案的10×1857/897与当前独立路径集合及散列相同；96条helper记录/14独立路径一致。最后九个普通案例54份二进制都与当前普通构建一致，诊断二进制只对它自己的清单核验，不混交付。

关键SHA256：

```text
source manifest D928A2309527761631517C50DE9304C1FFA5E1098C4EE14326DB51A817C3C27D
canonical C1CF3931AFCAA266CA246860A50BCB25C6772C6C16323E34EF80ACE39B04280F
normal binary manifest 8AA4F5F35F17742C8C8CAEB5CD9BD9810BA9E13D76FB071CCAA4FEF55E1881D9
normal Knight.dll C5E7F885E1ABD2E09C1B472752AF191E5AC81E95D16D37369674BE80CCC996B8
normal Knight.pdb B4851C9521B6E465C44C78B355A94312586032F110E3225CEA697BC0B8BF0077
diagnostic Knight.dll 5190F51E7FFFC967D2050057E358DE07A00DEE3121F44A1A0407B45B58ACCD90
diagnostic Knight.pdb 7E9EEB28F216832FB44CEBBEDC940435411CCA25AB56B72BD3EA5A234C0D55D6
Main source CB07B3E59759319DA22042306D55FF23CBD553EC02A2A9EC0C0D6FC8C1D02A6C
BasinCounterweight source A9964E414ED4A1C30AE2B776C45969F652803AB5B56A1A0357821E8E9482D08C
index 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C
HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
```

只证明本批记录的保管/同源，不证明其他进程全局空闲、历史材料已补齐或游戏通过。没有读写真实存档语义；原保护清单字节比较保留。

没有新过程媒体。手动容量检查2026-10-27T11:21:28.3891054Z：1431609字节/16文件，基线3508，增长1428101，Notify=false，不重置、不重建自动化。

## 裁决、延期与下一步

1. 沿用已授权的KNIGHT/Godot继续执行，不重新切换UE、请求已决定的项目选择或设计批准；不用技能默认的工作树/提交/删除步骤覆盖用户约束。
2. 本次选择诊断而非猜测资源修复。记录会扰动时序，所以诊断PASS也不替代普通post；实际根因仍OPEN，成本是暂时没有解决玩家崩溃。
3. 三项Important以标量归因与正确阶段修正，不改变生产状态机。完整地图控制的退出失败保留，改用完全自有最小native夹具；成本是它不能证明完整地图/敌人战斗，需要另跑完整资格。
4. 唯一复核未判断的原生根因、未来结果、全十一项/正式视听/硬件真人、无基线历史脏改动均继续OPEN。未测任意sink调度/死锁或GC时序扰动，不声称覆盖；只验证了指定抛错sink控制。V145-M1及所有旧Minor保守延期，不把它们缩成“只剩一项”。
5. Task1/2诊断功能已实装并局部验证，Task3完整资格仍未完成；计划目录及所有失败保留，不删除工作记录，不标目标完成。

下一最小隔离建议：单独调用既有不变的 `ProloguePlaytestRecorderTests.RunTask5FallContextContracts`，随后 `PrologueLevelRuntimeTests.RunGeneratedMainDeathOnly`，观察自然创建、恢复和owner退役/重新加载。4b8f对应两段仅1534行，能避免大量不相关缓存命中先耗尽记录；它是移除前缀的隔离变量，干净结果不能否定完整post的前缀/时序依赖。若目标终结器未自然执行，只能写“未覆盖该调度”，不能写稳定性通过。不得原样复用会强制GC和保活的source-rooted/unrooted实验，不增大cap/预算掩盖问题。

之后继续Main混战、Explore Grip1预警与已有Grip2掩体的真实自然可达路线。正式美术/连续动作/UI/音频、完整Boss及有支撑出口、存档兼容、目标硬件与真人体验，以及全部原十一项，仍是交付要求而非本轮完成项。
