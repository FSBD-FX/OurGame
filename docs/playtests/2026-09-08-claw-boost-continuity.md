# 机械爪助推姿态连续性 · 2026-09-08

接续 `ffe8280ea32e9628bd78e49aab602a38aa5a0342`，本地开发小修，不冻结 r3，不生成新包/标签/Release。
生产仅改 `PlayerPresentationCoordinator.cs` 的三处映射，没有付费生成、资产、场景、输入或物理改动。

## 判断与修复

- 事实：正常 Space 助推发布一帧 Boost，仍保持附着；下一物理 tick 回 Reel/Swing。
  同 tick 自动发布 Released 仅属于旧机关 Latched 路径，不是正常 Boost。
- 推理：Boost 在 action/secondary 映射中落到 None，Formal 清空抓附动作，下一帧重新进入；
  因此主要可见问题不是 Boost 当帧，而是随后手臂下垂再抬起。
- 风险：渲染可能跨过短状态；必须确认实际采样，不能仅靠源码推断。保持原跳跃/助推/冲刺边界。

修复把 Boost 并入现有 ThreadApply/Contact、secondary Contact 和 ThreadApplied cue。
后者原先错误落到枚举默认 AttackStarted；当前默认 Player 的 CueScenes 为空，不能声称原游戏必有攻击闪光。
新测试用独立非空 profile 验证可替换特效的正确路由，不修改缓存资源。

## RED / GREEN

证据目录：`E:/OurGame/Knight/.evidence/september08-claw-boost-continuity/`，最终 SHA 见其中 `CHECKPOINT.md`。

`ScheduledBoostKeepsTheRenderedAttachedAction` 加载实际 Player 场景，通过真实调度的 Modes/Claw/Motor
建立抓附并助推，Coordinator 的自动 `_Process` 发布动作，读取真实 Formal 手臂姿态。
fixture 停用 InputReader 后注入已采样 PlayerInputFrame；这不是实体键盘/InputMap 端到端测试。
没有手工调用 Present、注入 Boost、修改速度或摆放助推后的位置。相机仅为测试固定构图、zoom=2。
测试要求至少一次真实 Boost 呈现采样；低渲染率下若没采到，属于需单列的调度失败，不自动归为生产缺陷。

- `pose-red.log`：接受一次 Boost、实际呈现采样一次，action=None/None，NearArm 为 Identity；
  配置扩展的错误攻击 cue=1、抓附 cue=0，预期退出1。
- `spool-red.log`：纯时间线专项保持其余输入不变，稳定线轴角度 -0.33799997 短暂松到 -0.32791942，预期退出1。
- `movie-red.log`：硬件实际渲染再次复现。助推前手臂角度 -1.8010175，Boost 动作清空；
  下一 Swing 帧角度 -0.27960402，随后约六帧重新抬回。
- `focused-green.log` / `movie-green.log`：同一次 Boost 呈现 ThreadApply/Contact，手臂保持 -1.8010175，
  下一帧 -1.7894658；线轴仍 -0.33799997。配置扩展抓附 cue=1、错误攻击 cue=0。全部退出0。
  focused 同时覆盖原短跳、活动冲刺后松爪、释放上升惯性，不返还空中冲刺或添加二段跳。
- `boost-normal-window.log`：无固定帧/movie writer 的窗口专项也实际采到 Boost 并通过，无错误警告。

首次测试编译曾使用不存在的 Rotation 属性，改为现有 RotationRadians 后编译通过；这不计作 RED。
独立代码审查未发现新增 P0/P1/P2，事件/fixture/自有资源清理通过检查；按建议收窄测试调度注释。

## 运行与视觉复核

`pre-green.log`、`post-green.log` 均退出0，错误0、警告2/3，仍来自既有故意错误 visual/cue 根、
抛异常观察者、错误 room 根及拒绝 Completed→Recovering 的负向用例。
`ordinary-main.log` 普通主场景窗口240帧和 `headless-main.log` plain 启动均退出0、无错误警告。

`route-green.log` 是正常速度窗口完整自动路线，非固定帧、无录像写入、无失焦。
五区/四次复活/门/Boss重试/结算通过；回落18采样仍0超安全边距、0道具栏交叠，落地稳定12帧。
2773个有效进程回调采样：p50=16.664ms、p95=17.163ms、p99=23.480ms、max=47.628ms，
6次>33.3ms、0次>50ms。这不是GPU帧时、稳定60fps或真人手感认证。
默认完整路线没有附着后的第二次 Space，不能冒充助推专项；专项另有上面的真实控制器调度证据。

`boost-before.mp4` / `boost-after.mp4` 是 RTX3060 Vulkan 固定60fps的前后短录像，各83帧约1.38秒。
两份录像日志从 Boost 起的13条物理帧、状态、位置和速度记录按顺序完全相同；只移除 action/arm 字段比较。
不把这段轨迹等价扩大为全状态空间证明，也不以固定帧证明正常帧率。

主代理查看新旧65–76连续帧4×3图板、新第68帧原图；独立代理另查看两版第68帧原图：

- 旧68帧手臂明显下垂，随后逐渐重抬；新版保持抓附手臂，没有这段清空再进入。
- Boost 当帧67两版外观差异不明显，旧版仍保留前一帧 release overlay。
- 同一处瞄准驱动的左右翻转仍存在，不声称消除了翻转；样本未见新增遮脸、武器伸长、绳消失或严重伪影。
- 绳仍从身体/头附近显示，并非手掌出绳；固定zoom=2测试构图不是完整世界美术或真人验收。

视频 SHA256：

- before：`D1E15F4972B5BC4B39833F62C7BBA4345E8238849F18843A50B0E7B0E548912A`
- after：`988AE661353B306DE0E51D01DBEF8FFF2156155670F0A2CEF5D9C92377E344C0`

复现入口沿用 `RunGrappleReleaseTests.cs`：`--boost-presentation-only` 或 `--boost-spool-only`。
前后录像在前者增加硬件 Vulkan / `--fixed-fps 60 --write-movie <unique.avi>`；普通窗口专项不带这两个选项。
完整功能回归仍为 `RunPrologueTests.cs --phase=pre/post`，正常路线为 `RunRealtimePrologueProbe.cs --check-ascent-return`。

## 保留边界

这里只恢复抓附动作连续性，不是新的独立助推动画或力度优化。正式美术、真人初次上手/回摆舒适度、
普通解压启动/源码一致性、旧独立认证及真实商业数据仍缺；没有冻结 r3 或将进度推算为十万付费意愿人数。
下轮先检查首场敌人攻击预兆/伤害范围与场景UI遮挡的运行证据，避免继续反复扩环、挪HUD或盲加特效。
