# v169 — 独立标题边界与用户实际可见确认

## 当前结果

用户已对`call_722b37faa19c4a7fa4961c9d4649caaa`回答“能看到标题页”。这次仅标记**标题显示成功**，不表示开始游戏、接受美术/玩法或全版通过。窗口当前仍由用户关闭，源与helper保持冻结，不发送OS输入、抢前台或自动关闭。

实际live：`G:/knight/验证证据/title-hold/live-20261030-065044-992-d6d926a0155848e5b3a732fa00c6e419`。

- PID30588，创建2026-10-30T06:50:45.906105+08:00；Session1，标题Knight (DEBUG)，window handle5904568，Responding=true。
- 2512ms获得真实ISOLATED_TITLE_READY；Start/Continue禁用，live无自动开局/计时退出/输入驱动。保存/设置/recorder均在该案storage下，非真实玩家存档。
- launch.json明确`isolatedPreview=true`、`ordinaryKeyboardQualified=false`、`fullSuiteQualified=false`；原始receipt中的userVisible/human仍为false，保留启动时原意，不覆盖。用户后续可见确认由本记录单独绑定。
- 当前未收到关闭/退出证据，不能提前宣称清理完成或exit0。未截图或录屏。

## 事实：诊断与新许可

v168 keyboard062131虽Ready/quit正确且有Title合同，仍有5条Handle ERROR，原普通放行失败保留。只读图/源码核查未发现Title静态配置被项目手动Dispose后再用；错误栈仅能定位第二次AddChild，不能识别具体Resource或确定第一次Close是原因。默认new配置可能有丢弃分配，但没有证据将5错对应到它们，本轮不猜改生产生命周期。

canonical新增`title-resource-entry`，新进程直接运行**原有**RunIsolatedTitleHold.cs --contract-only，原两挂载/180physics/starts0/closes2和三通道隔离/清理不变。仅排除先前keyboard测试前缀，不改C#、引擎、缓存、GC或加原生采集。

| 案例（G:/knight/验证证据/october13-stable-claw） | build ms | native ms | 有限结果 |
|---|---:|---:|---|
| title-resource-entry-20261030-063651-354-1f48fe0b90eb422399c6ca3fff4fcf77 | 27648 | 7967 | 原独立合同完成，尚未冻结新增预览helpers，不用作本次live资格 |
| title-resource-entry-20261030-064427-775-29a5302dd26e4985a82b0b03a961340a | 27393 | 7763 | 当前helper冻结及严格三sink/唯一contract＋ENTRY＋END通过；仅显式preview资格 |

两案不是稳定性修复。当前1863全源/六bin与之前失败keyboard案字节相同；一次独立进程无错误不能证明keyboard前缀有因果，也不排除偶发故障。

用户随后对`call_8af549c12230491ea62c0a26408d1377`明确选择“允许先看隔离标题预览”，已告知普通键盘预检仍失败、预览仍可能退出。仅此许可允许增加显式`-IsolatedPreview`，不降低正式序章/普通资格；默认Get-TitleQualification仍拒绝诊断case。诊断原始provenance的behaviorAccepted/resourceFixed/liveQualified=false全保留，未洗成普通PASS。

## 实现与验证

只改三个PS：canonical的具名诊断入口与helper冻结、show工具显式预览分支、原test-launch-plan追加预览状态/传播/拒绝合同。没有新C#或生产玩法/资源变更。

PS新validator缺功能RED→当前GREEN；原普通模式与新的显式预览/默认不启动/无显示许可或资格失败拒绝通过。对最终真实case执行-VerifiedPreviewCase也通过：普通默认拒绝、显式预览验证当前全源/六bin/封存实体/11helper及原始三sink，再许可显示。

唯一fresh审查C0/I0/M0。实际采用FunctionsOnly→Get-TitleLaunchMode true/true→当前GetQ -IsolatedPreview→Start-TitleWindow -IsolatedPreview复用刚结束且未变化的资格；Start内部再次检查，不重编译不变代码或反复筛绿。新live只一次。

## 官方源码与边界

通过agent-reach的GitHub只读路线核对同版本官方固定提交；本机无gh，使用官方网页/原始文本后端，不安装或认证。源码显示失败入口先取得旧handle再读目标、分配替换handle；项目日志不包含该对象身份，因此不能从此异常反推损坏资产或项目双重释放。[同版本ScriptManagerBridge](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs)、[原生引用计数转换](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/csharp_script.cpp)。这里不宣布引擎bug或修复。

v168的资源专项/keyboard错误、pre超时、完整Main/Explore失败仍OPEN。正式美术、雾后画面/连续动作/手感/音频/硬件性能/真人游玩认可及原十一项全部未以标题可见确认关闭。下一继续真正的资源与前段耗时定位；live未关闭前不修改运行文件。

保管核对：两诊断案10组前后清单对、876份选定源实体、24helper实体、12封存bin全部SHA一致，未列0；4owned构建/native PID已终结。live启动source/bin两清单与最终案一致，launch/startup/三日志齐备；PID30588仍alive/Responding，关闭和清理不提前记完成。当前1863源/六bin/11helper一致，HEAD/index未变。未知8760与桥接进程未触碰。

## 后续关闭观察

下一继续时独立Get-Process与CIM均找不到原PID30588，stdout新增`PROLOGUE_AUDIO_DRAIN_OK frames=4 elapsed_ms=67`及`ISOLATED_TITLE_END result=0`，stderr空。说明live收到关闭请求并完成脚本清理、随后进程结束；不是助手自动关窗/发键或kill。原启动helper已释放Process句柄，未留操作系统exit-code收据，因此不伪称采集到native exit0；本次脚本END0与终止事实分别保留。运行文件解除本次live冻结，继续制作；资源稳定性/普通验收仍OPEN。
