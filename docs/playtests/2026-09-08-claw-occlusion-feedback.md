# 机械爪遮挡连接反馈 · 2026-09-08

基于 `47a6678a0b6d98fc7e9e14f49b08f2a125e3c0ee` 的本地开发小修，不是冻结 r3 或体验里程碑。
不生成交付 ZIP、标签或 GitHub Release，不改写 r1/r2，不增加付费资源。

## 判断与实现

- 事实：上一轮第三上升抓点连续96个物理采样仍在收绳，但绳线被清空。模型保持抓附和约束，
  绳线却将 EffectiveVisibility=false 当作清空条件；这不是教学卡滞留。
- 推理：连接仍有效时应保留可辨认反馈，并区分完整可见、被遮挡、已经松手。
- 风险：直接取消可见性判断会把整条绳画到地形前面；自动脱钩则会改变既有通关动作。
  单向平台内部的原准入射线还可能返回可见，不能只修 false 分支。

生产只改三个文件：MechanicalClawTargetResolver、ThreadLineVisual、ThreadShuttleVisual。
模型、控制器、约束、输入、跳跃/冲刺、伤害/能量、相机、地图、HUD与资源保持原样。

附着时额外执行一次只读视觉射线，共用原 mask、自身排除和目标归属规则。
原准入/模型射线保持 HitFromInside=false；视觉查询使用 true，不将结果写回模型。
绳线遇到首个非目标遮挡体时，改画一条无横向弯折的青色线，端点沿命中法线退4px，
末段24px渐隐；不画遮挡之后的绳段。未遮挡时恢复原三股绳。
源点在遮挡体内部、距离命中点不超过8px、或超距且没有可靠截断点时，只保留局部提示。
角色附近的青色双弧收绳环随实际绳长转动，保留既有远端锁定菱形；没有新增失败叉、目标或文字。

Line 的 IsConnectionObscured 同时覆盖截断和只显示局部提示的情况。
父 Shuttle 在绘制时读取其自有 Line 最新状态，不依赖两个 frame handler 的订阅顺序。
真正松手、失败/重置、目标卸载后清理遮挡标记和渐隐；旧 ThreadCast 展示接口不显示新局部提示。
这是连接可读性改进，不是绳索绕角/物理包裹系统，也不声称所有完整绳的弯折都经过连续碰撞检测。

## RED / GREEN 与回归

外部证据：`E:/OurGame/Knight/.evidence/september08-claw-occlusion-feedback/`。

- `occlusion-focused-red.log`：实际抓附后插入世界碰撞体，先确认目标、cast、牵引和收绳仍有效，
  再在“被遮挡附着仍应有近侧连接线”断言正确失败，退出1。
- `occlusion-route-red.log`：正常窗口的真实输入主路线也正确失败，并较早在第一上升平台 LandingOne 复现。
  首碰点(3150,1918.7507)，frame 起点(3111.496,1968.732)。不是失焦或不可达。
  失败跑包含异常退出成本，其315.137ms最大回调间隔保留，不冒充正常通关性能数据。
- 第一版 `occlusion-focused-green.log` 与 `occlusion-route-green.log` 通过；原第三段96个不可见采样中
  92个有截断绳、4个只有本地连接提示。实际首碰确认 LandingThree，而非仅凭旧位置推断。
- 独立审查指出 P2：原模型射线忽略源点内部时可能恢复可见，第一版只在 false 时查询视觉遮挡，
  因而漏检内部。`occlusion-inside-red.log` 先验证真实模型 EffectiveVisibility=true，再在 cue-only 断言失败。
  修复为所有附着帧都查询视觉遮挡，保持模型不动；`occlusion-focused-final.log` 退出0，无错误/警告。
- 新测试接入既有 RunGrappleReleaseTests 和完整 pre；覆盖真实首次截断、法线余量、渐隐/恢复、松手、
  目标卸载、自身与目标排除、1020px超距、掠射角、源点在碰撞体内部及附着/非附着状态边界。
  冻结 fixture 的手动移位和直接 Present 明确只作消费端测试，不冒充自然游玩轨迹。
- `pre-final.log` / `post-final.log` 退出0、错误0，警告2/3均为既有刻意异常用例。
  原地形、气力/盾过热、抓附助推、冲刺与复活/场景/Boss回归未被删减。

## 最终运行与画面复核

`occlusion-route-final.log`：正常窗口、非固定帧、无 movie writer、无失焦，退出0、错误/警告0。
完整五区、4次复活、门、Boss重试与结算通过；主动松手回落仍是18个下降采样0个超安全边距，
0个道具栏交叠，落地稳定12帧。只说明该专项，不外推为全关所有HUD无遮挡。

| 上升段 | 视觉遮挡采样 | 截断绳 | 仅局部提示 | 原模型可见但视觉遮挡 |
| --- | ---: | ---: | ---: | ---: |
| one | 63 | 51 | 12 | 10 |
| two | 90 | 78 | 12 | 10 |
| three | 117 | 92 | 25 | 21 |

第一段首碰LandingOne、第二段CatchThree；第三段射线起点(3791.7048,1694.8181)，
实际首碰LandingThree下表面(3737.0593,1474)。该起点来自 frame.CasterWorldPosition，
不是晚一个 Motor 步骤后的 body.GlobalPosition，避免将一帧采样差错判为碰撞矛盾。

2772个有效进程回调样本：p50=16.663ms、p95=17.190ms、p99=23.687ms、max=42.803ms，
9次>33.3ms、0次>50ms。采样含自动输入/遥测成本，不代表GPU帧时、稳定60fps、零额外开销或真人认证。
保留历史57.208ms及本轮失败跑峰值，不与成功样本混算。

`occlusion-movie-final.log` 与 `ordinary-main.log` 均退出0、无错误/警告。
前者为RTX3060硬件Vulkan固定60fps、完整2902帧约48.37秒，后者为无测试脚本的普通窗口240帧启动检查。
完整视频 `occlusion-final.mp4`，原速32–42秒片段 `claw-occlusion-10s.mp4`；不是新交付包或真人操作。
上一轮诊断、首版候选、最终版各582条loop trace，去掉仅显示字段后逐条比较无差异：
保留的position/velocity/physics/state/input/rope/correction/LOS/contacts等字段一致，不声称无穷状态空间的物理证明。

主代理审看新旧37.3秒原尺寸图、35/38.5/38.7/39.1秒图、33/37秒各4秒6fps图板、
3秒间隔总览及47.3秒起12fps结算图板。独立代理复核新旧37.3、35/38.5/38.7/39.1及33/37图板。
大图板在查看工具中缩小，细节结论使用1280×720原尺寸图。

- 37.3秒：旧图整条绳消失，新图近侧绳延伸至平台下缘前淡出，没有贯穿平台，附着文字仍可见。
- 38.5秒：内部起点不再显示首版候选的穿台长绳，只保留局部环与远端标记。
  **局部环偏小、贴近手部装饰，不能称为无需教学也足够醒目的独立提示。**
- 38.7秒：主/独立明确复核完整三股绳与“已抓住”同时存在，补足解除遮挡后仍附着的恢复证据。
  39.1秒已经松手站台，不把它错误标为恢复LOS；35秒为专项主动松手后的清理证据。
- 样本内未见新增严重挡物或错误红色失败标记。平台穿入动作在修改前已存在，本轮不凭截图判断物理设计好坏。

以上是抽帧审查，不是连续逐60fps播放或真人上手验收。下一轮优先检查角色收绳/抓附的动作与局部提示辨识度，
结合现有手部/肩部/悬浮灯造型减少混淆；不靠加教学卡、搬HUD或改牵引强度代替。
独立代码审查发现的inside P2已复现并闭环，复审无新P0/P1/P2；不把代码通过代替上述视觉剩余项。

完整视频SHA256：`4702D869CFFF68B456DB1FB7928B79120BFEF542E21070BCD9FE940312A0E8FC`。
短片SHA256：`83F73EE4D732139FB0DE03F4C8AA2E42ED1004366365C2DE923CE7AD85310B23`。

## 验证命令

命令均在活动工作树运行；使用本机 Godot 4.7.1 Mono 和 .NET SDK9。
先配置 DOTNET_ROOT / DOTNET_ROOT_X64 为 `G:/godot/dotnet-sdk`，
DOTNET_ROLL_FORWARD=LatestMajor，并把该 SDK 加入当前进程 PATH。

```text
dotnet build --nologo
godot --headless --path . --fixed-fps 60 --script res://tests/RunGrappleReleaseTests.cs
godot --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs -- --phase=pre
godot --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs -- --phase=post
godot --path . --max-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/RunRealtimePrologueProbe.cs -- --check-ascent-return
godot --path . --display-driver windows --rendering-method mobile --rendering-driver vulkan --audio-driver Dummy --fixed-fps 60 --write-movie <unique.avi> --script res://tests/RunTask9Tests.cs -- --generated-main --check-ascent-return --trace-grapple
```

完整日志路径、最终录像哈希与提交由同一外部目录的 CHECKPOINT.md 绑定。
第一版录像 occlusion-first-candidate.mp4 保留，用于区分 P2 修复前后，不作为最终验收。

## 保留边界

每个附着物理帧多一次射线；性能须看最终正常路线，不能由 focused GREEN 推断。
自动路线不代表真人上手或操控感受，固定帧录制不代表正常帧率。
不以此小修宣告整关精致、正式美术完成或达到商业目标；正式审美、真人反馈、旧独立认证、
普通解压启动/源码一致性与真实市场数据仍缺。无关导入元数据、音频总线和 artifacts 保留。
