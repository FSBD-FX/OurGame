# v220 · 退出音频对象身份与标题生命周期对照

状态：只读取证和两次现有无窗口诊断完成，**没有生产修复**。原十一项目标、v219完整严格回归的三项失败、角色惯性/动作/物理手感均仍OPEN。本轮没有改C#、harness、引擎或玩法数值。

## 事实

原始证据根为 `G:/knight/验证证据/october13-stable-claw`。两案同一1879源码/二进制，批次88916已终态；构建原180秒、原生原55秒，均headless，无fixed-FPS/movie/系统输入。

| 案件 | Build ms | Native ms / exit | 结果及范围 |
|---|---:|---:|---|
| title-resource-entry-20261103-184743-305-46edb7ce9f79478eaa3423ee266662f9 | 34869 | 8282 / 0 | 严格PASS；原独立入口连续两次挂载/关闭标题，0ERROR/0WARNING/0FATAL；仅诊断，不算显示/菜单/玩法或资源已修复 |
| thruster-world-jet-last-leaf-20261103-184835-635-b5718599489340dab30ab15418b9b429 | 39415 | 1281 / 0 | 严格FAIL；原最后叶子、原输入/帧数/同步Root.Free/直接Quit不改；1条WARNING报告2实例泄漏，无ERROR/FATAL或超时 |

本次实际身份链，而不是历史相同数量的猜测：

- Root释放前：自有循环WAV ID=`9223372074251191918`，Root=`27397195264`、Audio=`31792825939`、ThrusterVoice=`37195089631`。
- Shutdown已Stop并将所有播放器Stream设null、尚未Dispose自有副本时：同一WAV refs=2。
- Root.Free后：Root、Audio、voice均已无效，WAV仍valid。
- 引擎退出实际泄漏：同一WAV `9223372074251191918` refs=1，以及AudioStreamPlaybackWAV `9223372074267969246` refs=1。没有丢失/未Free节点的证据。

现有生产代码在 `PrologueAudioDirector.Shutdown` 中已经停止播放器、清除Stream并仅Dispose自有WAV；`StartThruster`借用cached dash.wav并Duplicate，未Dispose该借用导入资源。本路径没有GetStreamPlayback或手动Dispose播放对象。因而不能再随意加一轮Stop/Dispose，或释放共享导入资源来碰运气。

标题对照只说明当前同版独立两次挂载能正常结束。v219 keyboard的原3条Handle错误发生在前置键盘测试之后、第二次标题Mount的GD.Load/Instantiate及后续wrapper finalizer；本对照未包含此前上下文，**不能关闭该错误**。pre的180210ms超时也未重跑或放宽。

## 引擎源码核对与判断

本机只读版本输出为 `4.7.1.stable.mono.official.a13da4feb`。GitHub CLI不在PATH，使用内置网页与只读HTTP访问官方4.7.1 tag，没有安装工具或修改runtime。

同版本[AudioStreamPlayer停止实现](https://github.com/godotengine/godot/blob/4.7.1-stable/scene/audio/audio_stream_player_internal.cpp)会将停止转交AudioServer，再清除播放器自身的播放引用；[AudioServer实现](https://github.com/godotengine/godot/blob/4.7.1-stable/servers/audio/audio_server.cpp)把仍在播放的对象标成待淡出删除，混音与主线程清理随后完成；其[状态机说明](https://github.com/godotengine/godot/blob/4.7.1-stable/servers/audio/audio_server.h)也明确区分停止请求、混音完成与最终回收。官方仓库[相关退出音频问题](https://github.com/godotengine/godot/issues/76745)为相同机制的背景线索，不能代替本地对象身份取证。

推理：本地已排除“该Root/voice尚未释放”和“Shutdown没调用Stop/自有Dispose”的简单假设；同一owned WAV＋Playback在立即Quit边界的存活，与AudioServer尚持播放引用的异步关闭窗口一致。这是当前最强归因，不冒充已读取AudioServer内部列表/原生调用栈，也不外推成此前所有Handle/FATAL均由此导致。未证明裸同步Free→Quit的资源释放已修复，原严格失败保留。

## 风险与下一步

正常游戏的 `PrologueGameFlowController.OnQuit` 目前取消居民准备后直接Quit；仅在节点退出时Shutdown，不能保证异步音频回收已完成。下一步应在真实退出入口补有界、可观察的关闭握手：先撤销新音频/延迟会话的产生权，再停止并解除音频引用，确认所需回收边界后才Quit；重复退出请求必须幂等，失败不能伪造成功。具体实现与单独TDD方案另定，**本轮尚未实施**。

不能采取：屏蔽警告、禁用headless音频、Dispose借用播放对象、改engine、加GC或用固定sleep把旧用例跑绿。若新增异步正常退出流程，要保留原立即Quit叶子作为未改的诊断；不把它悄悄换成另一条路径，也不因此宣称旧严格回归已通过。窗口关闭接管必须只属于实际应用根，避免多个测试Flow/隔离标题观察器互相争抢；暂停/退出/取消/存档行为需保持明确。

另一条待办为键盘前置上下文的具体资源身份，不能因独立标题安静就换CacheMode、全局强持所有Resource或认为是同一个音频故障。动作后续仍需支撑相位耦合的起跑/制动/转身与正常速度验收，不由资源诊断替代。

## 保全与当前绑定

两案各4份before/after文本对和fresh-binaries/after一对，共10/10相同；四个保存的build/native PID与合同一致、wait均终态。第一次只读审计误用不存在的binaries-before文件名而中止，随后按实际fresh-binaries命名纠正，只重做元数据读取，未重跑原生或修改证据。

当前1879源码/11个helper并集/6二进制逐项匹配，0不一致；源码清单SHA256仍 `6C50D5C9DD14BF79D6D30ECF84AE200649DC14E1EE788428E20E76360F0CE27B`，Knight.dll仍 `57B1F2CC023CEDF17F59790718E261CBFF4767800194BD55916FC76FDA2B52BA`。未重新哈每个封存源码副本。批次后CIM无KNIGHT Godot/dotnet进程。

没有新过程图稿或录屏、付费生成、窗口显示、真实存档写入、Git发布/删除、自动化或额外崩溃dump。正常汇报前流程内20GiB检查为7427750 bytes/86 files、Notify=false/NewThresholds=0，不产生独立提醒或停工。
