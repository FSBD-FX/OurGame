# 完整序章·第一关生产验收（2026-08-18）

## 结论

序章第一关的产品内容与自动验收已经完成。当前版本包含完整六房间路线、上下分支、三类敌人、检查点与快速恢复、Bell Chain 永久捷径、隐藏壁龛、两阶段 Fallen Bellkeeper、结算与返回标题；正式角色/敌人/Boss/场景美术、动画、UI、引导、VFX、原创音频、设置、存档、双语与无障碍接口全部接入运行时。

最终产品树为 `e955bec`（此前产品修复包括 `cd33171` 音频余量、`1b33d14` 路线目标保持与 `2d8e6e4` / `e955bec` 的 720p 设置页布局）。本文件只记录证据，不把确定性演示当作真人通关。

## 工具与资产边界

- Godot：`4.7.1.stable.mono.official.a13da4feb`
- .NET SDK：`9.0.317`
- 生产清单：8 个必需类别，8/8 为 `final`、`Project-owned`
- 音频：44 个非空 48 kHz WAV 母带，固定种子本地合成
- 付费生成：0；本里程碑没有使用按次付费生成
- 被否决的 A2 穿戴角色与旧 idle sheet 仅保留为否决证据，未进入运行时

## 最终双门禁

在同一产品树上连续执行两次：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1
```

两次均满足：

- .NET 构建 0 警告、0 错误
- 20 个 leaf-to-root builder 全部成功，生成场景语义无漂移并恢复受保护字节
- `PROLOGUE_TESTS_OK`、`PROLOGUE_UI_RESOLUTION_OK`、`PROLOGUE_VISUAL_AUDIT_OK`、`PROLOGUE_PRODUCTION_AUDIT_OK`、`PROLOGUE_CRITICAL_PATH_OK`、`PROLOGUE_VERIFY_OK` 各精确出现一次
- 1280×720、1920×1080、2560×1440、3840×2160 四档 UI 均通过安全区、居中、容器与交互控件不相交检查
- 两轮确定性性能峰值分别为 24.85 ms、23.11 ms，低于 50 ms 预算；审计节点数为 358
- 负向 smoke 的 `FORCED_SMOKE_FAILURE` 被门禁主动制造并正确拒绝，不属于产品错误
- 正向日志没有脚本错误、解析错误、缺失资源或退出泄漏

## 最终录制

在上述门禁后的同一树运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1
```

结果：

| 证据 | 规格 | 大小 | 完成标记 |
| --- | --- | ---: | --- |
| `screenshots/prologue/prologue.avi` | 1280×720，60 FPS，2280 帧，38 秒 | 110,112,568 bytes | `PROLOGUE_PRESENTATION_COMPLETE frame=2280` |
| `screenshots/prologue/boss-retry.avi` | 1280×720，60 FPS，600 帧，10 秒 | 34,570,042 bytes | `BOSS_RETRY_PRESENTATION_COMPLETE frame=600` |
| `screenshots/prologue/prologue-frames/` | 2280 PNG + 38 秒 PCM | 逐帧审阅源 | `PROLOGUE_CAPTURE_OK` |
| `screenshots/prologue/boss-retry-frames/` | 600 PNG + 10 秒 PCM | 逐帧审阅源 | `PROLOGUE_CAPTURE_OK` |

两段录制日志均无 `ERROR`、`SCRIPT ERROR` 或 `Parse Error`。录制文件与逐帧目录是本机忽略证据，不纳入 Git。

## 视觉复核

`screenshots/prologue/final-review/` 保存了从最终逐帧证据复制出的 14 张代表图。已人工检查：

- 标题、设置、控制、暂停、恢复、HUD、Boss HUD、结算与返回标题均清晰可读
- 720p 设置页的返回按钮已移至标题栏左侧，不再遮挡语言下拉框
- 隐藏壁龛使用短暂提示，之后仍显示真实路线目标，不会把目标永久替换
- 六个房间、上下分支、Rust-Shell、Grave-Lamp Moth、Bell-Back Laborer 都有正式运行画面
- Bell Chain 金色菱形与机械丝端点一致；中性 `K` 打开永久捷径
- Boss 的开放阶段、装甲阶段、锤链提示、核心暴露、击败、结算均可辨认
- Boss 死亡后显示恢复反馈，回到检查点并以全新 Phase One 状态重试
- 画面中没有 Placeholder、Debug 或 Graybox 标识，也没有被否决的 A2 角色

## 音频复核

Master 总线安装 `AudioEffectHardLimiter`，测试要求至少 1 dB 输出余量。对最终 movie-writer PCM 的逐样本分析结果：

| 证据 | Peak | RMS | 高于 -0.9 dBFS 的样本 |
| --- | ---: | ---: | ---: |
| 完整旅程 38 秒 | -2.9382 dBFS | -21.80 dBFS | 0 |
| Boss 重试 10 秒 | -2.9382 dBFS | -20.56 dBFS | 0 |

44 个 WAV 母带均非空且为 48 kHz；总线、房间环境层、探索/Boss 音乐层、玩家/敌人/Boss/机关/UI/恢复/结算 cue 的运行时路径均由清单和集成测试覆盖。标题/结算停留段的安静区是演示脚本有意保留，不是资源缺失。

## 真人验收边界

自动与视觉生产验收已经结束，但 10–15 分钟“首次真人通关”仍必须由玩家本人完成。正式启动必须使用：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1
```

启动器只记录未暂停时间、路线、检查点、普通/Boss 死亡、重试时间、捷径/隐藏发现、恢复到可控时间与是否完成；不注入输入，也不会把确定性录制伪装为真人数据。玩家关闭游戏后，再读取 `latest.md` 并根据真实反馈决定是否关闭本目标。
