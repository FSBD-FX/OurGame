# 机械爪松手惯性 · 2026-09-08 开发检查

状态：r2 之后的本地开发修复，不是已冻结 r3，不是新的体验里程碑。
已交付基线仍为 r2 / `1da26b6bd4732ef1967dc0a01de0b726c5b8938f`。
没有新素材、付费生成、玩法键位变更、普通二段跳或远端发布。

## 事实、推理、风险

- 事实：真实控制器调度中，抓附助推后松爪会把 -360 px/s 的上升速度按普通短跳规则截断；
  另一个顺序问题是冲刺已经开始后松爪，错误的惯性保护会延长冲刺结束后的上升尾段。
- 推理：机械爪释放应继承碰撞后实际速度一次，不应等同于松开普通跳跃，也不能叠加第二份隐藏推力。
  新普通跳跃、冲刺（包括已在进行的冲刺）、外力和落地必须恢复各自原有规则。
- 风险：更完整的抛出轨迹会改变落点。驾驶器只能通过实际输入和落地检查适配，不可传送角色凑通过；
  自动路线成功不说明初学者理解了收绳、助推和松手时机。

## 改动

`MechanicalClawController` 仅在刚结束的实际抓附具有有效速度修正时授予释放惯性保护。
`PlayerMotor` 继承实际身体速度、清除抓附约束，不重复叠加切向量；未抓住时取消 Travel 不获保护。
`PlayerMotionModel` 将保护与普通短跳分开，并在落地、新跳跃、新冲刺、正在进行的冲刺时清除。
`PlayerMotionState.Reset` 和外力也清除它，保持一次空中冲刺限制。

新增单元和真实调度用例覆盖“先冲刺再松爪”，并接入专项入口和完整 pre 回归。
正常松爪、Travel 取消与普通短跳恢复的既有用例保留。路线驾驶适配真实惯性轨迹和受支撑落地，
没有改地图、传送角色或改变生产失焦清键规则。

## 运行证据

证据根目录：`E:/OurGame/Knight/.evidence/september08-r3-motion-diagnostics/`。
`heartbeat-0524` 是上一段修复证据，`heartbeat-0545` 是本次续验；目录名不是精确启动时间。

| 检查 | 结果 |
| --- | --- |
| 冲刺顺序单元 RED/GREEN | `heartbeat-0524/active-dash-release-red.log`：预期 -199.16667，实际 -589.1667，失败；对应 green 通过 |
| 真实调度 RED/GREEN | `scheduled-dash-release-red.log` 同样复现错误尾速；恢复修复后的 green 出现 `GRAPPLE_ACTIVE_DASH_RELEASE_OK` |
| 完整 pre / post | `heartbeat-0524/pre-final.log` / `post-final.log` 退出 0，错误 0；警告分别 2/3 条，均来自明确负向测试 |
| 开发检查点前复跑 | `heartbeat-0545/pre-checkpoint.log` / `post-checkpoint.log` 再次退出 0，错误 0、负向测试警告 2/3；不是完整独立认证 |
| 本次重新构建 / 专项 | .NET 构建退出 0、0 警告/错误；`heartbeat-0545/grapple-focused.log` 覆盖 Travel、冲刺交错、正常松爪与短跳恢复，退出 0 |
| 正常速度全路线 | `heartbeat-0524/realtime-normal-window.log` 非固定帧、没有 movie writer，退出 0；五区、四次复活、三段上升抓附、开门、Boss 重试与结算通过 |
| 本次正常速度复跑 | `heartbeat-0545/realtime-confirm.log` 同样条件退出 0、错误/警告 0，无失焦事件；不是第二次完整独立认证 |
| 固定帧显示录像 | `heartbeat-0545/route-motion.log` / `.avi` / `.mp4`：硬件 Vulkan、1280×720、60fps、2736 帧（45.60s），全路线退出 0，错误/警告 0 |
| 普通入口 | `heartbeat-0545/ordinary-main.log`：不带测试脚本，正常窗口主场景 240 帧退出 0，无错误/警告；不是完整普通启动通关 |
| 测试窗口防误用 | `heartbeat-0545/focus-guard-cleanup.log`：窗口仍有焦点时按预期拒绝 background 模式，退出 1，仅有该拒绝错误；不再出现断开未连接信号错误 |

pre 的两条警告是非法玩家视觉覆盖与非法 cue 根节点；post 的三条是刻意抛错的 observer、
非法房间视觉根节点、结算后拒绝复活。不能把含这些警告的运行写成“零警告”。
早先 focus 日志中的失焦中断与双错误保留，后续成功不能抹掉历史失败。
background 模式仍不能在本机保证隔离焦点；正常前台窗口是目前成功路径。

首个正常速度采样排除 120 个预热间隔，有效 2609 个进程回调间隔：p50/p95/p99 为
16.669/17.121/24.515 ms，最大 44.555 ms；超过 33.3ms 共 6 次、超过 50ms 为 0。
采样包括自动路线、测试框架与遥测开销，不是 GPU 帧时或稳定 60fps 认证；Dummy 音频不证明听感。
`max_step=376.18` 包含刻意死亡后的检查点复位，不可直接称为正常移动瞬移。

本次复跑排除同样 120 个预热间隔，有效 2610 个回调间隔：p50/p95/p99 为
16.660/17.110/21.984 ms，最大 57.208 ms；超过 33.3ms 共 5 次、超过 50ms 为 1。
保留这个较差的峰值，不挑选前一次较好数字声称无卡顿；尚未定位该单次峰值来源。

## 录像审看

复核 `overview.png` 的全程分段画面，以及 `ascent-27.png`、`ascent-31.png`、`ascent-35.png`：
后三张各取原录像 27/31/35 秒起的 4 秒，以每秒 6 帧、从左到右再换行排列。
另保留原速导出的 `ascent-motion-12s.mp4`（原录像 27–39 秒），无变速或镜头重排。

画面可见三段抓附、上升、松手到落地的连续位置变化；青色绳索、黄铜抓点与支撑边缘可分辨，
未见本轮新增的遮挡整个角色、空白场景或卡在第一段上升区。
这是录像抽帧审看，不是逐一观看全部 60fps 帧，更不是真人操作验收；不能凭 6fps 采样排除微小顿挫。
角色仍显小、场景由重复平直平台构成、教学文字偏密。保留这些下一轮视觉/上手改进机会，
不把现有简化程序化美术称为正式视觉完成。

独立只读复审也指出：约 31.67–31.83 秒锚点紧贴上缘，33.5–34.3 秒人物接近右边缘；
上升区教学牌与路线竞争注意力，实体平台与背景横梁的层次可强化。
这些是下一轮应在原生分辨率实机确认的构图线索，不凭缩略图断言字号不可读或镜头抖动。

## 可重跑命令与边界

使用 Godot 4.7.1 Mono、.NET SDK 9.0.317；运行前设置 `DOTNET_ROOT` 为本机 SDK 目录，
`DOTNET_ROLL_FORWARD=LatestMajor`。下面的 `Godot_console` 指已安装的 Mono console 可执行文件。
日志/录像每次使用新的外部目录，不能覆盖旧证据，也不能用会还原陈旧场景的旧录制包装脚本。

```text
dotnet build --nologo
Godot_console --headless --path . --script res://tests/RunGrappleReleaseTests.cs
Godot_console --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs -- --phase=pre
Godot_console --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs -- --phase=post
Godot_console --path . --max-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/RunRealtimePrologueProbe.cs
Godot_console --path . --display-driver windows --rendering-method mobile --rendering-driver vulkan --audio-driver Dummy --fixed-fps 60 --write-movie <new-output.avi> --script res://tests/RunTask9Tests.cs -- --generated-main
Godot_console --path . --max-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --quit-after 240
```

尚待：普通解压启动/源码交付一致性、既有完整独立认证、真实首次游玩与审美反馈。
本轮不冻结版本、不创建标签/Release，不上传未审查工作树。既有 r1/r2 和用户无关改动保留。
下一轮应转向教学信息层级或上升区落点可读性等玩家可感知主题，不再反复尝试同一后台焦点方案。
