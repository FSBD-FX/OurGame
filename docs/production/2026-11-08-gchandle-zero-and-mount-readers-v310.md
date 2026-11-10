# v310 · 零句柄语义与挂载内的资源路径

## 事实与本轮进展

上一轮 v309 是有实测的新诊断进展，不是等待，但原前段 11 条错误仍严格失败。本轮没有再重跑普通前段筛选无错结果；先核对官方实现、本机运行时和应用所有权，再执行一个隔离的托管运行时对照。工程仍是 `G:/knight/Knight`，完整原十一项目标不变。

没有修改游戏 C#、测试 C#、地图、素材、设置、存档、缓存、GC 策略或引擎；没有窗口／系统输入／付费生成／额外原生崩溃采集／Git 变更／删除。唯一 Godot 进程是 `--headless --version` 的版本查询，返回 `4.7.1.stable.mono.official.a13da4feb`，不是一次新关卡测试。

GitHub CLI 不在当前 PATH 或 KNIGHT 工具目录，按 agent-reach 开发路由的降级原则改用现有 GitHub 只读 fetch；未安装工具或索取新授权。所有外部实现判断只依赖官方仓库，不依赖论坛推测。

## 官方代码与本地对照

1. [Godot 对应引擎提交的桥接函数](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs#L1279)先将传入值转为 GCHandle，之后才读取 Target；有效句柄的 Target 为空时有单独的正常失败分支。异常捕获会打印并返回 false，不能将本次异常简单等同于“Target 正常被回收”。该文件与 csharp_script.cpp 均已在根读回比对，实际引擎提交与 4.7.1-stable 标签内容逐字相同。
2. 本机规范宿主配置的 `G:/knight/Tools/dotnet-sdk` junction 指向 `dotnet-9.0.317`，仅有 8.0.30／9.0.19 shared runtime；CoreLib 产品版本为 `9.0.19+8381bdb01fe4a26e1f61370e779c78bb73ecc95b`。与此完整提交对应的 [.NET GCHandle 实现](https://github.com/dotnet/runtime/blob/8381bdb01fe4a26e1f61370e779c78bb73ecc95b/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/GCHandle.cs#L155)在 FromIntPtr 入口校验，[该校验](https://github.com/dotnet/runtime/blob/8381bdb01fe4a26e1f61370e779c78bb73ecc95b/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/GCHandle.cs#L186)对零值抛出日志所见未初始化异常；源码与 v9.0.19 标签逐字一致。
3. 隔离托管对照实际运行 `.NET 9.0.19`：新建且仅由对照持有的有效强句柄可往返；有效非零、空 Target 的弱句柄可正常读取 null；`IntPtr.Zero` 则抛 `System.InvalidOperationException`、`Handle is not initialized.`、HResult `-2146233079`。未调用强制 GC，也没有读取已释放的非零句柄。

对照源、独立项目、离线 NuGet 配置、二进制和读回结果保存在：

`G:/knight/验证证据/2026-11-08_gchandle_运行时对照_v310/`

实际命令为专用 SDK 对 `probe/Probe.csproj` 的 Release build（0 警告／0 错误，退出 0），再以规范宿主相同的 DOTNET_ROOT／ROOT_X64／MULTILEVEL_LOOKUP／ROLL_FORWARD／PROCESSOR_COUNT 设置执行 `probe/bin/Release/net9.0/Probe.dll`（退出 0）。`NuGet.Config` 清空来源，没有外部包；项目位于证据目录，不在 KNIGHT 的编译输入中。对照将 UI culture 设为 invariant 以核对英文消息，仅影响此纯托管控制进程。

实际加载 CoreLib SHA256 为 `18DD52B72D7726914249749376C2E635103AB25D7F4C5D37BB3A033166F2DF0E`。`result.json` 是根观察终端后转录的对照记录，明确 `failingNativeHandleCaptured=false`、`causeProved=false`、`wholeGoalAccepted=false`，不是 Godot 原生案或真实指针采集。

## 推理：缩小机制，不冒认对象

结合相符的错误栈、官方入口与本地对照，旧错误**支持传给桥接函数的是零句柄这一解释**，而不是仅凭中文“句柄未初始化”泛称任意 stale 资源。实际失败调用的 `oldGCHandlePtr`／`createWeak` 没有采集，不能将推理写成已读取的原生参数，也未确定造成该值的线程、资源、脚本或生命周期阶段。有效空 Target 与零句柄的区别有实测依据，但不证明本项目根因已经解决。

[匹配本机引擎提交的原生代码](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/csharp_script.cpp#L1847)有引用计数引起的强／弱句柄转换；[句柄数据](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/mono_gc_handle.h#L65)将释放状态和弱类型分别表示。部分回调的 released 判断并不相同，是进一步核对线索，不足以把“引擎必有 bug”或“补一个空判断就能安全修复”当结论。本轮没有补丁、更换或重新构建引擎，也没有绕过错误门。

## 当前应用路径的只读核对

- 没找到原补给四用例活跃路径对共享 PlayerTuning、PresentationProfile、PrologueSectorDefinition 或 PackedScene 显式 Dispose／Free 的证据。Player Motor／Combat 确实共享同一保存 PlayerTuning。
- `Residents.Dispose()` 的实际对象是纯 C# CanyonResidentManager，最终只清源字典，不 Dispose 字典中的 PackedScene；World 的定义字典清理也不是释放 Resource。
- Audio 的两个 WAV 释放来源是本实例 new 和 cached.Duplicate；Fog／小地图／HUD 的显式释放来源为本实例创建的图像／纹理／材质，而非共享加载的 Shader。不能把方法名 Dispose 就等同于共享 Tuning 误释放。
- 单独的 SceneWalk 诊断确有借用 SceneState 的 using，但不是当前原四用例所走入口；指导中的 GetSlideCollision using 又被 sample mode／禁用 guidance 排除。它们不构成当前故障的释放因果证明，未据此贸然改生产代码。

关键新路径：外层 AddChild 不仅包含“正式节点 Ready”。`SeamlessPrologueWorld._Ready` 的 typed Definitions 枚举／StreamModel 构造会逐个 `PrologueSectorDefinition.Validate()`；其 `ValidateSceneRootTransform` 已会离树 Instantiate 整区、检查根 Transform 再 Free。真正 staged Instantiate／AddChild 发生在之后。所以本次外层 mount 错误也可能暴露在这批验证实例化中，不能直接分配给某个正式节点的 Ready，更不能用“五个 definition”对应“五条错误”。

相关七个生成 setter 已从当前 Knight.pdb 的 EmbeddedSource 只读解析，对应源码哈希相符；它们直接 `ConvertTo<T>`／`ConvertToArray<T>` 再赋值，没有能标明首次重绑的现有前后标量记录。静态存在不证明本案具体 setter 被调用。

## 下一步与开放项

下一次原生观测应优先拆分“Definitions 验证中的离树 Instantiate／Free”和“正式 staged 挂载”，再决定是否需要 Ready 或 native→managed 转换边界；不从零句柄语义直接跳到 retain-all、GC、缓存或引擎补丁。仅观察实际原调用，不新增验证实例或跨帧资源保活，记录日志扰动风险。

本轮没有新 Godot 回归案；v309 的前段 FAIL／后段 PASS 原结果不变，不用纯 CLR 对照替代完整原生测试。资源对象身份、稳定性根因、正式美术动作、正常双路线／声画／硬件及真人仍未完成，完整目标保持 active。

汇报前根逐项读回当前 1,901 输入与六二进制，均与 v309 最终清单一致；实际 `V310_CURRENT_INPUT_AUDIT_OK source_rows=1901 binaries=6 game_changes=0 managed_controls=3 actual_native_handle=false whole_goal=false`。Probe Program SHA256 `69A908371AD99B3C0F3936E0913FCA26953BAE33FD78CFF06EF4369FCAC9176F`，Probe.dll `7CC7E93F8D0A9F38D831181A6C5D555AFCEB30938B66BF5C4C9C0CF18C897B60`，转录记录 `6808C64DE0631F4D75CA79C36046FF25DC6C6EC83463384F7102EC941A0F5FC5`。

本轮无新过程图稿或视频。正常汇报前仅运行一次既有媒体 `-Check`，12,807,896 字节、`Notify=false`，未达新增20GiB；没有自动化或独立提醒。

独立只读审阅 Critical 0／Important 0／Minor 1，未发现把对照冒认为 Godot 复现或引擎修复的表述。Minor：两个 owned handle 在 try 之前分配，若第二次分配失败，第一次不会进入 finally；当前成功运行没有走该失败路径。该边界仅属于隔离一次性 probe，非游戏资源释放归因，本轮保留并记账，不宣称已验证所有分配失败路径。审阅未独立读取官方源／构建运行日志／PDB／全部输入清单，相关证据由根核对，不扩大其审阅范围。
