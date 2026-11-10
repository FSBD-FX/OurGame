# v203 · 资源终结身份重核：仍有Handle错误，未修复

## 事实、推理与风险

**事实：** 上一目标轮是实际进展：同Rust两次正常键盘K与观察器I1修补已证，但v202最终pre原生fatal/Main未清。原十一项目标保持ACTIVE。本轮只在 **G:/knight/Knight** 使用现有工具排查，不改任何CS、场景、资源、项目配置、helper或生产数值。

新原生证据：一次 `prologue-pre -IdentifyRefCounted -SourceRows1872`，原180s上限、正常缓存、原camera前缀。保持源/helper冻结至终态，没有追加GC实验/新dump/前台或系统键鼠/付费/自动化。随后一次普通models恢复未开启身份日志的原构建，不以诊断程序集作为最终运行版本。

**推理：** 相机第三轮的 `load-enter` 只定位了当时未返回的主线程操作，不定位终结线程中失败的资源。当前日志证明某些相同资源家族能正常终结，也同时证明Handle错误仍在；两者不能互相抵消。

**风险：** 没有获得当前失败对象的可靠身份，不能修掉断言、取消释放、永久强引用所有资源、随意改变生产缓存或直接升级引擎来宣布成功。最终普通models只是爪模型子集，不能替代失败的完整pre/post/Main或正式视听、真人、硬件性能、十一项验收。

## 两项独立只读核查

1. 相机实际边界：`BasinAscentCameraRuntimeTests.cs:378`在每轮同一个完整Level的 `GD.Load` 前打印；不是单独CanyonAscent/high地形资源加载。第三轮尚未观察到instantiate/高位设置。前两轮已完成普通audio drain和cleanup-return；普通cleanup没有额外GC，也不代表全部RefCounted已经终结。
2. 当前相机路径未发现直接Dispose/Free借来的保存PackedScene、共享Shape、sector definition或resident profile。Motor查询、Ranged重复settings/beam、fog/minimap材料图像、音频生成/重复流等显式释放属于私有new/duplicate对象。world退出仍经导出Definitions和stream-model保留definition图，这是保留期观察，不是已证明的所有权错误。

v141/v161的真实碰撞快照不同、peer仍有效，不重启已经无证据支持的“GetSlideCollision返回同一个wrapper并互相释放”假设。v145/v146/v150曾记录Profile/PlayerTuning不同身份的自然fatal，但没有证明duplicate-dispose、缓存clear致因或引擎bug；v180一次历史采集没有产生dump，不推断有新采集授权。

## 新case与构建恢复

证据根：`G:/knight/验证证据/october13-stable-claw/`。

| case | build ms | native ms / cap | 严格结果 |
|---|---:|---:|---|
|prologue-pre-refcounted-identity-20261101-163223-691-0d6c5716185e4472815b7ee47c835649|32657|180050 /180000|FAIL：timeout，仍有3条ERROR，无完整完成链|
|models-20261101-163629-741-c280b279139544ffaea79b3d60f1ce65|29594|509 /55000|普通爪模型子集PASS|

build均0警告/错误，guards=[]。identity构建真实带 `REFCOUNTED_FATAL_IDENTITY`；ordinary构建原七参数、无该define。两种DLL不同属正常且分别保管。此次没有native FATAL，但**不是安静通过**，更不是v202 fatal已修。

## 实际终结调用，不从“未崩”猜稳定

identity stderr54656行：1314 ENTER/1314 RETURN，calls1–1314逐call/type/thread/hash/serial/disposing/原记录identity配对，0未配对/错配。RETURN raw pointer均零是base.Dispose后的记录状态，不能要求它仍等于ENTER地址。

1258条 `disposing=False` 自然终结调用已返回，另56条显式Dispose返回。自然返回含ResidentSubtypeProfile36、PlayerTuning189、PresentationProfile458及其他14家族。36Profile都有先前观察的native identity；PlayerTuning152有、37未知。当前保存共享PlayerTuning serial1曾观察，但没有其instrumented Dispose，本轮不能称它的终结得到验证。

例：Profile128/scrap-scatter::Resource_lut5t与Profile67/precision-missile::Resource_qv7df分别在call523/527正常返回。不能把不同进程中相同serial/native数字当同一对象，更不能因此抹去v150失败。

754条已观察identity/219唯一serial：本次记录没有同nativeID多serial或同serial多nativeID。36Profile终结记录没有Profile间pointer别名。所有已知identity ENTER中32组地址在不同serial重复使用，示例是先前PlayerTuning12已RETURN、之后Profile34取得同地址、但nativeID不同；这只是顺序地址复用，不是同时别名或重复wrapper的证明。

## 三条Handle错误仍未定位

stderr36562/36622/36736，external112/172/242镜像，共**3条唯一错误**，不是6条。

- 第一、二条：SwapGCHandleForType，经Supply的Load和Instantiate原路径进入。
- 第三条：SetGodotObjectPtr→internal_refcounted_disposed→GodotObject.Dispose/Finalize→GC.RunFinalizers。没有给出对象类型、serial、nativeID。
- 全部19个项目Resource子类已有对应诊断hook；“栈没显示项目override”不能独自证明它一定属于未插桩类型（当前项目Optimize=true，且日志/回调边界有不确定性）。

进一步按日志顺序核对，而不只看最终配对数：在三条ERROR开始时，所有记录线程的open ENTER区间均为0；第三条也没有终结线程1的open disposing=False调用。第一/二条发生在call467 PlayerTuning821返回之后，第三条在call482 PlayerTuning824返回之后；不能把“最近一条返回记录”指定为报错对象。当前只能说**没有捕获到可归属的调用括号身份**，不是证明所有base.Dispose内部无错误。

## 日志覆盖已饱和，不能再照样跑整段

lifetime journal恰8192行，sequence1–8192，最后stderr38949为op287/controlled-abyss.04的preparation-exit；38950已出现下一Profile gate但没有新journal row，后半段出生/保管/释放链不完整。22个创建栈达到4096字符边界，有潜在裁剪。没有INCOMPLETE文本也不能算完整：timeout先于成功时AssertComplete。

后续静态核查更正：上述“成功时AssertComplete”不能泛指pre。`RunPrologueTests`当前identity-pre成功路径只检查allocation完整性，并未调用lifetime journal的AssertComplete；post成功路径才调用。故pre即便没有超时，也不能从当前完成标记推导journal完整。此为独立待修的诊断放行缺口；本v203仍严格FAIL，未被误签成功。下一候选是首失事件即时标记并封存观察窗口、成功pre也验证同一sticky状态，而非提高容量或按错误重新清空历史。

相机三轮仍有独立完整证据：普通sequence1–30/cameraOK；身份边界36、source witness omitted0。三个保存source wrapper在load/instantiate/configure/return/mount阶段weak-alive，到了原有eight-frame-return就weak-dead，后续保持false。只证明托管弱可达性变化，不证明nativePackedScene死亡、过早释放或失败资源身份。

Supply四例/五root及40阶段返回；后续到UpperPendulum/lower_to_mid_swing进入后原180s超时。它不能补齐已饱和的lifetime尾账。**不提高8192/4096/时间上限、不再重复完整identity-pre来等一次绿色**；下一定位应收窄观察窗口或增强准确的绑定/释放归属，而完整普通pre仍保留为最终验收门。

## 官方源代码核对（不是本机根因证明）

本机header为4.7.1/a13da4feb，与[官方4.7.1发布说明](https://godotengine.org/article/maintenance-release-godot-4-7-1/)的提交标记相符；不以此代替本机二进制/符号逐字节匹配。

该提交的[CSharpInstance终结路径](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/csharp_script.cpp)先检查句柄状态，再进行引用释放；断言本身没有提供“Dispose调用次数”的证据。其强弱句柄转换也有独立的目标存活分支。故不能从相同错误文本直接推出项目重复释放或某一个邻近资源致因。[GodotObject托管Dispose路径](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/GodotObject.base.cs)和[native互操作入口](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/glue/runtime_interop.cpp)仅用于理解边界；没有修改引擎/绑定库，也没有已测出的引擎修复。

官方仓库已有[同类历史问题83762](https://github.com/godotengine/godot/issues/83762)，但不同版本的同文本报告不能认作KNIGHT当前根因或升级即可解决的依据。GitHub CLI在本机不可用，本次只读浏览官方源码/发布页；未做外部写入。

## 保管、裁定及继续项

两案10清单对/954选中source副本/28helper副本/12bin/4owned逐SHA匹配。08:41:25UTC四创建/路径身份终结且PID不存在；08:41:26UTC当前1872源项/9个models helpers/6bin与普通恢复案一致。

sourceManifest `513F1E6BBDA942D42B28EE7C02E694DC9A3F815B5A2628FC7D5E79FD1D086397`；identity DLL `FC31BBE9AAD34D48F819847A9AEA0990DDA1AD46ED6D85E2708D79A5DF6F6FD6`；ordinary DLL `FBAAC26F31591243A9D9E6A7A0FC0CFF9FB3B0B00AB0449629E795149340DA16`，ordinary binaryManifest `F3F392E96915114EA59579D0B0A752F0C8CBB60797614CE47A22EB611904370E`。

裁定：只排查与恢复普通构建，不基于时间相邻修生产资源/改引擎或压制错误；代价是没有根因修复与正式完成声明。诊断超时/日志饱和/Handle3ERROR全部保留；不能把模型子集或项目Resource终结返回泛化为完整验收。保留原十一项/当前双路线/视听美术动作UI音频/硬件真人/交付全部原门，继续仅无窗口工作；任何额外原生采集和引擎替换仍需单独授权。

08:48:56UTC手动媒体检查7,427,750bytes/86files、Notify=false/NewThresholds0；未新增过程图稿/视频或达到新20GiB，不建立独立提醒/自动化。全部新报告与证据留在KNIGHT，未删除历史资料或操作真实存档。
