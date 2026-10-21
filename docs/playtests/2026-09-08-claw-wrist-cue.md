# 机械爪手腕连接提示 · 2026-09-08

接续 `aa1d6b8ea775bcece4e5df280f640e57f5d0f1e4`，本地开发小修，不是冻结 r3 或重大体验版本。
不生成 ZIP、标签或 GitHub Release，不改写 r1/r2，没有付费资源。

## 判断与范围

- 事实：旧37.3/38.5秒运行图中，局部青环容易混入肩部装饰；Shuttle 的 z=0，
  Body/Mantle/NearArm/Weapon 分别为4/6/8/9，固定偏移也不跟随近侧手腕。
- 推理：只把角色侧提示放到独立绘制层，并绑定真实手端，可改善连接状态辨识。
- 风险：环过大会挡住面部、武器或平台；整节点提层会连同绳索和远端标记一起提前绘制。
  需保留原遮挡规则、身体比例和物理起点，实际运行审看后才判断效果。

生产范围仅 `ThreadShuttleVisual.cs`：owned `ConnectionCue` 子节点 z=10，父节点及 Line 的层级不变。
双弧半径9→12px，增加深色窄描边、去掉中心金点，不填黑圆盘。
绘制时读取 `ConnectionHandPath` 指向的近侧手臂，局部(0,21)是现有24px手臂的手端；
继承真实瞄准、姿态混合和左右镜像。可选手臂路径缺失时保留角色侧退化提示。
旋转仍只由实际绳长驱动；原附着/遮挡白名单、恢复视线/松手/失败/卸载清理不变。
这是手部状态环，不是新的实体绳索起点；没有把物理 caster 移到手腕或增加假连接线。

## 验证记录

证据目录：`E:/OurGame/Knight/.evidence/september08-claw-wrist-cue/`。

- `raster-red.log`：硬件Vulkan实际渲染原角色，左右×水平/朝上四姿态，真实手腕8–15px环带内
  新增可辨青色像素分别为0/0/0/12，低于60；角色本身已渲染，不是空白图误判。预期退出1。
- 初版 `raster-green.log`：同口径为130/135/131/135，退出0；静止绳长0像素变化，
  绳长170→161时环带变化81/93/82/88，松手/Clear的额外青色像素均为0。
- Probe直接提供类型化展示帧、加载真实Formal场景并用SubViewport读回像素。
  这是展示消费端测试，不冒充真实控制输入、完整人物动作、全屏无遮挡或真人辨识验收。
- `lifecycle-red.log`：旧Formal脱树但仍存活时，新手腕附近为0像素；RequestReady重入后
  child从2增至3、owned cue身份不再唯一，两个独立问题均复现，退出1。
- 修复后 `raster-final.log` 退出0、无错误/警告：新rig手腕135像素，旧rig仍存活且不在树中；
  重入仍是同一cue、child=2，并再次验证实际手环绘制。只有一个Draw订阅，绘制时重新解析当前手臂路径。
  独立复审确认两项P2闭环，无新阻断问题。

## 最终运行与审看

最终生产修改后重新运行 `focused-verified.log`、`pre-verified.log`、`post-verified.log`，
全部退出0、错误0、警告0/2/3。后两组警告仍来自刻意异常fixture：错误VisualScene根、
错误Hurt cue根、抛异常观察者、错误Room根、Completed→Recovering拒绝；没有删除负向用例。
`ordinary-main.log` 普通窗口240帧与 `headless-main.log` 普通headless启动均退出0、无错误警告。

`route-final.log` 为正常窗口、非固定帧、无movie writer、无失焦的完整自动路线，退出0、无错误警告。
五区/4次复活/门/Boss重试/结算通过；三段视觉遮挡仍为63/90/117采样，
第三段92截断/25局部提示。松手返回专项仍18个下降采样0超安全边距、0道具栏交叠，落地稳定12帧。
2773个有效进程回调样本p50=16.661ms、p95=17.159ms、p99=23.455ms、max=44.948ms，
8次>33.3ms、0次>50ms；不是稳定60fps/GPU帧时或真人认证，不混用历史峰值。

`movie-final.log` 为RTX3060硬件Vulkan固定60fps完整路线，退出0、无错误警告。
`wrist-cue-final.mp4` 共2902帧约48.37秒；`claw-wrist-10s.mp4` 是32–42秒原速节选。
与上轮最终录像日志的634条完整 `GRAPPLE_TRACE` 按顺序逐行完全相同，包含输入、位置、速度、
状态、可见性、绳长、约束、接触及显示状态字段；不扩大为全状态空间的物理等价证明。

主/独立审看新旧37.3/38.5秒原尺寸图，新35/38.7原图、33/37秒起各4秒6fps图板和左右展示样本；
主代理另审47.3秒起1秒12fps结算图板。大图板被工具缩小，局部细节判断以1280原图为准。

- 37.3：短绳仍正确截断，手环与骨白手臂和青色衣服明显分离。
- 38.5：没有短绳辅助的内部起点状态，手环仍可辨出；没有新增遮脸、吞掉武器轮廓或盖平台顶边。
- 35：松手后手环消失；38.7：仍附着的完整三股绳恢复时手环消失。
- 环跟随样本中的手部，左右展示也对应正确侧；未逐60fps连续审片，不证明全部动作或真人理解。
- 短绳仍从身体附近发出，与外侧手环不是同一起点。只能称为手部状态提示，不能称为手掌出绳或完美衔接。

完整视频SHA256：`AB82B21D13ACADB22C91509E72DFCBEF50A91105ED01AFAB7EB64A0EF4F79633`。
短片SHA256：`4B160AD1C467194BEBF7724F58D90A97E0ED6AFA707411F92CDEBE53036A125F`。
本地开发提交及验证日志绑定见证据目录的 `CHECKPOINT.md`。

新增图像测试命令（与项目 `godot.md` 相同的.NET环境，先build）：

```text
godot --path . --display-driver windows --rendering-method mobile --rendering-driver vulkan --audio-driver Dummy --fixed-fps 60 --script res://tests/presentation/ClawConnectionReadabilityProbe.cs --log-file <unique.log> -- --output=<external-absolute-directory>
```

功能回归使用原 `RunGrappleReleaseTests.cs`、`RunPrologueTests.cs --phase=pre/post`；
正常窗口使用 `RunRealtimePrologueProbe.cs --check-ascent-return`，没有fixed-fps/write-movie。
录像使用 `RunTask9Tests.cs --generated-main --check-ascent-return --trace-grapple` 及Vulkan movie writer。

## 后续边界

姿态映射另发现既有缺口：Latched→ThreadLatch，Reel/Swing→ThreadApply，Boost落到None。
本轮未顺手修改；下一轮应先用真实助推输入复现并检查视觉连续性，再决定最小姿态修复。
不要把青环增强当成已解决人物全部动作或正式美术。

正式美术、真人初次上手/回摆舒适度、旧独立认证、普通解压启动/源码一致性与真实商业数据仍未完成。
不以自动路线或像素数量推算十万付费意愿人数。保留无关导入、音频总线、artifacts和此前测试UID。
