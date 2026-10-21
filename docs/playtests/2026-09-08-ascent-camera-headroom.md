# 上升区提前看见抓点 · 2026-09-08

状态：在本地开发检查点 `d30acead7268ed82ec4ca988b8fdf92955d89c22` 上续作的小修，
不是冻结 r3、正式美术或新体验里程碑。r2 交付包和 GitHub 历史归档不变。
本轮没有新增素材、付费生成、场景重建、地图或角色物理改动。

## 事实、推理、风险

- 事实：旧主路线的第三发射点已经站稳，但下一抓点中心在屏幕 `y=-35.994507`，
  必须先普通跳跃才能用鼠标看见目标。旧原尺寸 34 秒帧也显示上方锚点被上缘裁切。
- 推理：这里首先是垂直构图不足，而非抓取距离、跳跃高度或教学字号问题。
  仅在上升区预留 120px 上方视野即可让该点在起跳前进入画面。
- 风险：上移相机会减少下方视野；更早看到目标会改变自动驾驶发射时刻。
  必须实跑原路线并看落台，不能改物理或加二段跳凑通过。主动中途松爪回落的首次体验仍待专项检查。

## 实现与测试

`SeamlessPrologueWorld` 保存作者设置的 Camera2D 局部 Position。
已提交当前扇区为 Ascent 时，使用已有限位平滑的指数权重向上预留 120px；
离开时平滑回到原值，检查点、新局和远距离切区共用立即复位路径。
world 单独离开树时还原原值，避免仍存活的玩家相机留下偏移。
不写 `Offset`（震动反馈使用它），不改 Zoom、地图边界、镜头限位定义或键位。

真实显示主路线在每个 Ascent 发射前先断言已站稳，再检查锚点中心处于视口 64px 内边距内。
检查发生在普通跳跃和左键发射之前。它不是整条攀升路径可见性的保证。

生命周期 fixture 使用非零作者位置 `(17,-23)`、非零静态 Offset `(9,-7)`，
验证相邻扇区平滑往返、预加载不提前偏移、远距离切区、检查点、新局及 world 独立出树。
独立复审指出原先退出测试是在新局清零后才销毁，且 `shake_unchanged` 标记过强；
现已补带偏移出树测试，并收窄标记为 `offset_unchanged`。没有直接测试活跃 CameraFeedback 的共存效果。

## 运行证据

证据目录：`E:/OurGame/Knight/.evidence/september08-ascent-camera-readability/`。
使用本机 Godot 4.7.1 Mono、.NET SDK 9.0.317，所有引擎/构建/录制串行执行。

| 检查 | 实际结果 |
| --- | --- |
| `standing-anchor-red.log` | 修复前真实窗口退出 1；第三站立锚点中心在屏外，第一/第二点通过 |
| `lifecycle-zero-headroom-red.log` | 保留新增局部位置逻辑，仅将 headroom 临时变为 0 的变异测试；进入 Ascent 没有开始上移，预期失败 |
| `exit-restore-mutation-red.log` | 临时删除退出还原行，实际 `(17,-143)` 不等于应有 `(17,-23)`，预期失败；finally 已恢复生产行 |
| 最终 .NET build | 退出 0，0 警告/错误 |
| `pre-final.log` / `post-final.log` | 退出 0，0 错误；分别 2/3 条刻意异常用例警告。post 包含最终 exit_tree/offset lifecycle 标记 |
| `realtime-green.log` | 正常显示、非固定帧、无 movie writer，time_scale=1；完整自动路线退出 0，无错误/警告/失焦 |
| `after-route.log` / `.avi` / `.mp4` | 硬件 RTX 3060 Vulkan 固定 60fps，2720 帧（45.33s），五区/三段抓附/四次复活/门/Boss 重试/结算通过，0 错误/警告 |
| `ordinary-main.log` | 无测试脚本的普通主场景窗口运行 240 帧退出 0，0 错误/警告；仅为启动检查 |

三个站立锚点的中心 Y：第一点 `310.00012` 不变（底部限位约束），第二点 `80.00012→124.06653`，
第三点 `-35.994507→84.00549`。后者精确比较采用同一站立条件的日志，不使用新旧同秒不同姿态的图像配准。
三次发射的 visibility_wait_frames 均为 0，实际抓附及支撑落地全部通过。

实时回调采样排除 120 个预热间隔，2589 个有效样本：p50=16.662ms、p95=17.194ms、
p99=26.968ms、max=44.455ms，8 次超过 33.3ms、0 次超过 50ms。
这是含自动驾驶/遥测开销的进程回调间隔，不是 GPU 帧时或稳定 60fps 认证；
上一轮 57.208ms 峰值仍保留，不能凭本轮较低峰值宣称已修复卡顿。Dummy 音频不证明听感。

pre 警告来自非法玩家视觉覆盖、非法 cue 根节点；post 来自故意抛错 observer、
非法房间视觉根节点、Completed→Recovering 拒绝。各项 RED 的 1 条错误是指定断言失败，不混入 GREEN 成绩。
`post-green.log` 是追加退出盲点测试前的中间通过结果，最终以 `post-final.log` 为准。

## 录像审看与边界

`after-route.mp4` 全流程保留；`ascent-headroom-12s.mp4` 为 27–39 秒原速节选，没有变速或重排。
主代理审看全程 3 秒间隔总览、27/31/35 秒起各 4 秒的 6fps 图板和原尺寸前后帧；
独立代理复核 31/35 秒图板及前后原尺寸帧。下一锚点和上方小台完整可见，角色及脚下平台仍可辨认，
样本内未见新明显遮挡或路径阻断。6fps 抽帧不能排除短暂镜头跳变，不是逐帧或真人操作验收。
旧 `before-anchor.png`（31.75s）已经是跳起后的在屏锚点，不能用于证明旧站立条件可见。
`before-forward.png` / `after-forward.png` 同取 34s，但发射时刻和姿态稍有差异，只用于构图对照。

完整 MP4 SHA256：`5051B713535491A0CA095845DAE02DF95DD9E9933EC19219E2570552798F8371`。
12 秒 MP4 SHA256：`5FE04F33C7DEFB8F8B54EB7DF3F7D1FC2877FBEECAECDBFE6CA45FAA1928A748`。

## 接续与重跑

下一轮先做第三锚点处站立瞄准、抓附、主动松手回落的正常速度检查；再根据画面决定是否改下方视野。
贴右、教学牌信息密度、背景/实体平台层级仍待处理。本轮不扩大范围。
普通解压启动/源码包一致性、既有完整独立认证、真实首次上手和审美仍未完成。

```text
dotnet build --nologo
Godot_console --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs --log-file <new-pre.log> -- --phase=pre
Godot_console --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs --log-file <new-post.log> -- --phase=post
Godot_console --path . --max-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/RunRealtimePrologueProbe.cs --log-file <new-realtime.log>
Godot_console --path . --display-driver windows --rendering-method mobile --rendering-driver vulkan --audio-driver Dummy --fixed-fps 60 --write-movie <new.avi> --script res://tests/RunTask9Tests.cs --log-file <new-movie.log> -- --generated-main
Godot_console --path . --max-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --quit-after 240 --log-file <new-start.log>
```

先设置 `DOTNET_ROOT=G:/godot/dotnet-sdk`、`DOTNET_ROOT_X64` 同值和 `DOTNET_ROLL_FORWARD=LatestMajor`。
只使用新外部证据路径；不运行会还原陈旧场景的旧录制包装脚本。无人类报告或新 Release 生成。
