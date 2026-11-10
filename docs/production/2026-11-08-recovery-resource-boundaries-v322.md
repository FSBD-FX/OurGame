# v322 · 恢复资源的阶段与已有对象身份（最终2通过／2严格失败）

## 事实与边界

原十一项仍active。本轮只继续Q05资源排查，没有修生产资源、改地图/动作/素材/数值。原v321恢复`21f895...`的3Handle ERROR严格FAIL保留；本轮一次受日志扰动的quiet不能关闭根因。当前仍只无窗口，无GUI/OS输入/付费/dump/真档/GC/cache/预算/Git/删除/自动化。

新test-only `tests/RecoveryResourceBoundaryTrace.cs`静态状态仅bool/int/string，不持有原生引用。原Load结果同步借用并直接返回，原生属性只在IsInstanceValid后读取；invalid保留先前标量ID，不用ID0冒充新对象。每行声明timing_perturbed=true/cause_proved=false；上限512，fault/omitted/空数据不能通过，Dispose不抛出覆盖原异常。没有GetScript/GetState/WeakRef/InstanceFromId或匿名子资源发现，也不新增跨await强引用。

旧runner仍按原四delegate顺序；Audio只加两次正式根加载/实例/清理收据；Seamless只在原foreach已得definition/Scene和原Pack/Free、原fixture创建/清理处观察。原QueueFree、1/2 ProcessFrames、Pack/Free、原场景路径和严格诊断全部保持。其他phase未启用trace，调用立即返回不读原生属性；新编译产物仍不能自动继承v321双路线整版资格。

## 诊断器自身TDD

`thruster-recovery-compatibility-20261108-145320-918-adb83342e54949dfa36141f180553d64`：1912输入／build0／32768ms／0warning/error，engine1／543ms／guards=[]，`RECOVERY_TRACE_CONTROL_RED` valid42实际0。ResolveKnownId当时故意return0，失败在Begin和正式根加载之前。这不是新游戏故障或原Handle的重现。root在terminal后仅实现valid?observed:known；三个字面量controls覆盖有效42、invalid保留42、未知0。

## 一次原恢复序列的实际收据

`thruster-recovery-compatibility-20261108-145458-598-8299f18e7b6b408b97fff6b6f0044e86`：build0／24410ms，engine PID42664／exit0／14940ms／无超时、failure=null／guards=[]。原55秒cap与全部delegate/成功marker保留，三sink警告/错误/Handle/泄漏0，stderr空。只运行一次，未重复筛绿；结论是此诊断版本本次未复现。

132普通JSON行＋1summary，sequence1..133：saved1=6、queue-fixture=64、visibility-fixture=54、saved2=8、publishers仅summary=1；faults0／omitted0／complete=true。summary的physics/process=0是刻意不在Dispose重读引擎时间，不冒充真实末帧。

实际根/资源：

- saved1：PackedScene `9223372084770506685`，正式root `47982839296`，cleanup返回时valid=false但knownId仍该root，随后才进入queue fixture。
- queue：level `254577477120`／world `257698040440`／Player `256993396226`；三对象在cleanup-enter有效，在return失效但保留各knownId。
- visibility：level `259845522944`／world `260717939322`／Player `260013295106`；三对象同样清理前有效、后失效。cleanup-return sequence122–124与下个load-enter125衔接，physics84/process38。
- saved2：PackedScene `9223372304803697063`，正式root `268016027136`；load-return126／instantiate-enter127／return128均完成。原fixture-enter/ready仍同root，最终cleanup-return132 invalid但ID保留。
- 两synthetic Player加载返回的PackedScene ID均`9223372293781064198`；两正式PrologueLevel返回的PackedScene ID不同。这只是本进程实际已有返回资源身份，不证明缓存健康、创建点、最后持有者或原报错对象归属。

手工fixture/内存PackedScene的空路径是预期，不是身份缺失或彼此同一对象。独立只读收据审计确认133行双sink逐行同、1..133连续，14组Pack/14组临时sector Free/10组初始definition→Scene/4组Load/8组knownID cleanup全精确配对；queue的3个及visibility的1个替换Pack不额外冒充definition-created。内部子Resource/CSharpScript、引擎oldGCHandle和最终化时刻不可见，不能据known root被释放推定所有Resource都正确释放。

## 当前身份与回归

当前source1912 manifest `8B6EBA62385BDCA2756403A00D967CEA5878A1D8B05F722BC20657D1A6D07EBA`，six-bin manifest `496F2A45B1437E68D98CF84654271C5268BCEDB20A65220735D405BC77964566`，普通9helper清单`EB531CFB64E033F0C4AA2F25B8690327A8B4DA58010D76F5E25B6DCE6C6B0F02`。新helper GREEN SHA `8ACCC5CB212A3BF63D0E9EE400484962CBE294AAC28935E3F7C619F857769A2A`。完整源集/current保护与全部回归终态随后核验，不以五个改动文件替代完整清单。

独占batch94940已exit1：models9c151...通过；普通pre618a1f...在35579ms以-1073741795崩溃，无超时、guards=[]。stderr全文确认FATAL `gchandle.is_released()`／`mono_object_disposed_baseref`（csharp_script.cpp:1788），GodotObject.Dispose/Finalize→GC.RunFinalizers。最后已完成的前段边界是index0相机return；index1 Laborer输出sample/audio-drain后尚未return，不能写44项执行完或指认Laborer为失效对象。没有Resource ID可归因，未采集新dump。

post27618b...188296ms/75项/exit0，但root错误地传入了`-MeasureFullPostBudget`：该旗标实际cap600000、behaviorAccepted=false，而非原普通300000。此案仅作耗时诊断、**不计普通后段严格PASS**；批次外壳的PASS仅表示命令exit0，服从真实provenance的非验收声明。记录调用错误并保留原始案，不改canonical预算/原日志。后续I1修补后的新产物运行普通post，不使用此旗标，不把这次诊断拼成合版。当前正式动作/正常体验、原资源FATAL及十一项仍OPEN。

## 唯一独立审查与待修补

本诊断增量一次review C0/I1/M2，五SHA与精确BASE均匹配。I1真实存在：AssertComplete先判断，Dispose后输出summary；若这最后一次日志输出抛错，Fault之后没有断言，而canonical只要求旧成功marker，因此可以缺summary仍PASS。Root按实际效果接纳Important，在94940全部terminal后进入一次TDD修补，不在live改源、不二审。

首试`...150744-635-841f854ffec049658d19c7d596989c87`仅因CS0219未使用emitted被quiet-build gate拒绝，engine未启动，不计功能RED。去除该warning、仍保留旧“输出失败不拒绝”行为后，`...150908-808-8630992c74d9463c86d56303fc857599`build0/0warn/error，engine1/418ms／guards=[]，以`RECOVERY_TRACE_SUMMARY_OUTPUT_RED normal write failure must reject success`实际功能RED，原正式根尚未加载。

随后只在FinishCapture末尾加入normalCompletion的emitted&&faults0断言。正常AssertComplete现在显式先输出summary、再检查、再发布兼容成功；异常using Dispose仍no-throw并幂等，不重复输出summary。四个纯故障控制实际调用同一结束核心：write/serialize失败正常拒绝，write/serialize在unwind时保留原异常引用，不是mock调用次数或源文本自证。立即传入的纯输出函数无native捕获，但仍有托管分配/时序影响。

修后恢复`thruster-recovery-compatibility-20261108-151041-306-d4043d93d3314764bd6b2ce9432d4050`／engine0/16964ms/failure=null/guards=[]：controls marker成功、132+summary／fault0/omitted0，summary原stdout第170行已在compat171/core172之前，三sink无原生诊断。当前source1912 `4D0567A595E14549D47B7D98C92D721CDF1B86C9BA7119A2E61F8D12CE9EB4B1`／six-bin `D82BAAB481F7211BAE34CE0DAAAF140104E47789015A18C8CB795F49AC92E76A`；最终C#辅助类`tests/RecoveryResourceBoundaryTrace.cs` SHA `AC458685AA1A715674691EE7C681D1200B5BF68983909449A654AF8AAC8E3866`。这验证I1的诊断收尾行为，不是原native资源修复。独占18328最终exit1，四phase结果如下。

## I1修补后当前同版终态

所有case位于`G:/knight/验证证据/october13-stable-claw/`，无重复筛绿。

| 原phase | case | engine耗时ms | 当前严格结果 |
| --- | --- | ---: | --- |
| recovery | thruster-recovery-compatibility-20261108-151041-306-d4043d93d3314764bd6b2ce9432d4050 | 16964 | PASS |
| models | models-20261108-151130-024-09d6f356bd424aa79dc93d83ab46e304 | 396 | PASS |
| pre | prologue-pre-20261108-151200-390-47e680e4f855491f8efa0f6b5d1816ea | 143531 | FAIL，4 Handle ERROR |
| post | prologue-post-20261108-151451-353-e7df0f574ef24a98bff19918d06892ca | 186722 | FAIL，6 Handle ERROR |

四个engine都exit0/无超时/guards=[]，但前后段failure明确非null，不能用44/75及PHASE_OK冲销资源错误。普通cap分别55000/55000/180000/300000，后段没有误模式旗标，亦不签旧180秒或目标硬件性能。当前没有重跑正常Main/Explore或独立存档全专项，v321两路只能作为该版历史证据。

pre完整stderr四条均为SwapGCHandleForType，全部在DescentClawGuidanceRuntimeTests.InstantiateLevelFixture:535的原GD.Load，不是四次已知Load或四对象；无Instantiate错误栈、无FATAL。最后Supply root `3601531406599`／world `3602236052486`／flow `3611799063751`，确实有cleanup-return与Supply/三个trace END，再进入Descent index5。这个Supply是保存Main后改成Basin/Layout1的旧样板配置，不是未经改写的正式Layout2世界。await栈的Supply100/CheckSupply446/Teardown77是清理返回后的continuation，不是仍在QueueFree本体报错。

post完整六条ERROR均读回：一条LegacyTeachingSectorFixture ctor:5383的GD.Load、四条同构造器的Instantiate（不同原样段消费者）、一条SetGodotObjectPtr→Dispose/Finalize；都是Handle未初始化，不等于六个对象。stderr/external是同六条，不能合计十二。另4条原负控warning与旧scope一致（seamless1、room-profile1、flow-registry2），不把它们混入未解释错误。当前无FATAL，但初候选618a1f的原生FATAL仍单列、不关闭。

对于618a1f，最后`PROLOGUE_AUDIO_DRAIN_OK`并不代表root清理已返回：TestSceneTeardown:65还等1帧，74 QueueFree，75–76再等2帧；normal Laborer样段之后还可能进入blocked样段的早期Load，现有日志无法二选一。Camera旧root `626410917336`清理已明确返回，但不能将它代作未记录的Laborer root或失效Resource。新Seamless/两个Audio test hook不在pre前两项实际调用链（不是只看无trace推断），仍不排除程序集变化/时序影响。

I1本身功能RED→GREEN证据成立；但整个计划要求的suite仍2PASS/2FAIL，Task1及计划不写complete，不用局部修补宣称完整资源问题已修。

## 全量与保管核验

本turn共11个case：两功能CONTROL_RED、一仅编译warning拦截、第一候选4案（2PASS/1FATAL FAIL/1非验收诊断）、I1最终4案（2PASS/2ERROR FAIL）。可存在的54对source/bin/helpers/user/continuation before-after逐字节同；841f854因quiet-build失败无fresh-binaries成功pair，原guard明确保留，不补造缺失清单。60个封存binary实际hash匹配，12个不同编排helper路径与当前一致，非跨不同候选程序集强求同产物。

最终四案source/bin/user/continuation跨案一致，20对保护及24个sealed均属当前同产物。root完整重新枚举1912源/所有当前Debug6逐项零漂移；v321→最终仅3旧测试CS（runner/Audio/Seamless）改变+1新增C#trace，无生产/资产/地图/导入变动。canonical source附项作为独立脚本变化，没有改预算或旧门。

名称区别已直接查明：编排脚本`.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1`始终SHA `8D26451D6BFA114C42E97B63BB9EAEB94617455A604AE45B63AB554D70E6DD3E`；C#trace辅助类为AC458685...，在source-before第1649行和before/tests副本均匹配。二者不是互相冲突的helper版本。独立收据worker初次将两个名字误当同文件；root用原路径/源清单/副本hash纠正，不把该误读写成实际native版本不符。

当前真用户109行清单仍匹配SHA `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`；没有owned Godot/.NET测试/构建进程，现有godot-ai attach不触碰。源代码、失败、原始日志均保留；没有额外dump、窗口、付费、自动化或新媒体。最终唯一媒体Check=15,203,531bytes/145files/baseline3508/Notify=false，不停工、不独立通知。

下一安全边界：Supply侧已有完整return收据，最小缺口仅Descent index5 enter→原Load-enter→原Load-return/Instantiate前后。不直接打开既有trace-sector-definitions旗标，因为那会改走PackedScene局部和GetState路径，不是最小无资源策略变化的观察。还须处理post旧sector载入/实例与终结器链，原对象归属/共同根因仍未确定。原十一项、正式美术动作、正常视听/硬件真人继续active/open。

Minor延期M322-1：新增pre/post源码附项在thruster块内不可达，该phase不由此复制新helper源码，但全1912源inventory与另一路Seamless快照仍在，非全保护失效。M322-2：512为数据行、另加1summary共513，当前132+1有界。不顺手扩修两Minor，亦不遗忘M320/M321。

审查边界裁决：旧3Error不是3对象；内部Script/子Resource、旧句柄、原native故障原因均未知；成功summary只证记录，仍独立查native严格门；IsInstanceValid到读取的native竞态目前未证，托管fault与旧诊断veto保持；部分fault身份不作归因；当前单次顺序使用不自动扩成并发/嵌套Begin支持；CONTROL_RED不是游戏bug；pre/post及整版仍按root实际终态。尤其cleanup后再读会延后原wrapper最后使用点，JIT/async生命周期可能被延长，即使没有字段/集合保留也不能排除，进一步限制quiet的因果解释。
