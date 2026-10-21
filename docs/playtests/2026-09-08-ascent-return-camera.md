# 松爪回落的下方视野 · 2026-09-08

状态：基于 `be98aaac8e3279a747ede0eb6f7a319c3ad3c4d3` 的本地开发小修，
不是冻结 r3 或新体验里程碑。r2 包、历史 GitHub 标签和归档保持原样；没有新增素材或付费服务。

## 事实、推理、风险

- 事实：第三锚点处，站立抓附、按 W 收绳上升 180px 后主动松手，角色能够回到原 LandingTwo。
  但首次下降时平台顶面在屏幕 `y=706.3318/720`，底部仅余约 14px。
  18 个早期下降采样中有 11 个超出 48px 安全边距；这不是平台完全出屏，更不是回落物理失败。
- 推理：固定的上方 120px 预留在回落阶段仍生效，挤占了预判落点的空间。
  利用现有松爪惯性生命周期，在上升减速时平滑回中，可以不改运动轨迹地改善下方构图。
- 风险：不能让普通跳跃或新一轮瞄准丢失上方抓点；落地后恢复上方预留的回摆也需要检查。
  HUD 仍可能挡住局部地形，不能把“安全边距通过”说成整个平台完全无遮挡。

## 实现范围

`PlayerMotor.HasGrappleReleaseMomentum` 只读暴露既有标记，不改变标记生命周期或物理。
`SeamlessPrologueWorld` 只有在 Ascent、未落地、释放惯性有效，且抓爪为
Released / Retract / Idle / Failed 时，才将 120px 预留乘以 `Clamp(-velocityY / 240, 0, 1)`。
沿用原指数平滑；上升减速时回中，下降时目标为作者原位置。
落地或新的 Aim / Windup / Travel / 抓附恢复原上方目标，不瞬移。

Released/Retract 可能还保留 CurrentTarget，因此按真实相位判断，不等待绳子收完。
显式检查 IsOnFloor，避免刚落地这一帧 motor 标记尚未清除造成错误下视。
依赖缺失时保持旧上方视野；world 出树仍还原作者位置。
没有修改地形、跳跃、机械爪、缩放、限位定义、震动 Offset 或键位。

## 测试与证据口径

证据目录：`E:/OurGame/Knight/.evidence/september08-ascent-return-visibility/`。
Godot 4.7.1 Mono / .NET SDK 9.0.317；构建、引擎和录像串行运行。

专项入口必须显式携带 `--check-ascent-return`；默认全路线并不会自动做这次中途返回。
真实输入从入口连续到第三发射区，观察同一 cast 的 Windup、Latched、主动 Released，
禁止新增普通跳/助推/冲刺/复活，验证与 LandingTwo 的真实向上碰撞法线和连续 12 帧支撑。
随后用真实 A/D 重新对齐原发射区，继续完成余下攀升、门、Boss 重试和结算。
探针不写玩家位置、速度、运动状态或镜头。48px 判据只涵盖仍高出原站立位置至少 100px 的下降段。

默认 post 另包含相机消费者 fixture：用 motor 公开 API 建立空中释放状态，
通过真实调度的 InputReader / Modes / Claw 产生新的 Aim，再要求镜头连续三帧向上恢复且不瞬移。
fixture 暂停移动并停留在已发布的 Aim 相位，不冒充实际物理松爪；真实物理由上述专项路线覆盖。
独立复审提出这一分支盲点后，新增测试用删除状态白名单的变异证明确实会失败。

| 证据 | 结果与边界 |
| --- | --- |
| `return-baseline.log` | 初次诊断使用了错误台面取点 y=1714，不用于正式前后量化；保留以说明纠正过程 |
| `return-corrected-red.log` / `.avi` | 使用核实后的台面 y=1732；旧生产镜头下退出 1，指定边距断言失败，11/18 超边距；回落与同次抓取的物理断言均已通过 |
| `return-realtime-green.log` | 正常窗口、非固定帧、无 movie writer，完整自动路线退出 0；18 个早期下降采样全部在安全边距内，无错误/警告/失焦 |
| `return-route-green.log` / `.avi` / `.mp4` | RTX 3060 硬件 Vulkan 固定 60fps，2902 帧/48.37 秒；专项返回和完整五区路线通过，无错误/警告 |
| `pre-final.log` | pre 退出 0；0 错误，2 条刻意异常用例警告，释放惯性/活动冲刺/Travel 取消回归保留 |
| `post-before-mutation.log` | 第一次 Aim fixture 准备失败：同一续体里立即手工采样没有切换模式；不是生产镜头 RED，保留诊断记录 |
| `post-aim-scheduled.log` | 改为真实 physics 调度采样后，完整 post 通过；生产未为此调整 |
| `fresh-aim-whitelist-red.log` | 仅临时移除生产状态白名单，post 在新 Aim 镜头方向断言预期失败，退出 1；finally 已还原白名单并重新构建 |
| `post-final.log` | 恢复后的完整 post 退出 0，0 错误，3 条刻意异常用例警告；含原 lifecycle 与新的 FRESH_AIM 标记 |
| `ordinary-main.log` | 无测试脚本的普通主场景窗口运行 240 帧，退出 0，无错误/警告；仅为启动检查 |

最后一次 .NET 构建退出 0，0 警告/错误。pre 的警告来自非法视觉覆盖、非法 cue 根节点；
post 来自抛错 observer、非法房间视觉根节点、Completed→Recovering 拒绝。
录制后只补相机分支测试和记录；最终生产代码已恢复为与成功录像一致的版本。

同轨迹首次下降的台面屏幕 Y 从 `706.3318` 到 `643.73486`，下方余量增加约 62.60px。
两份固定帧日志中的玩家世界位置和速度一致；不将不同路线时刻的画面误作物理改善。
旧日志名 `hidden_early_descent` 实指安全边距外采样，当前已改名 `outside_safe_margin`，判据不变。

实时探针排除 120 个预热间隔，有效回调样本 2776：p50=16.661ms、p95=17.284ms、
p99=23.932ms、max=42.044ms，5 次超过 33.3ms，0 次超过 50ms。
这是自动驾驶及遥测环境下的进程回调间隔，不是 GPU 帧时、稳定 60fps 或真人手感认证。
新增回落支段使路线更长，不与旧路线样本池合并；历史 57.208ms 峰值仍保留。
Dummy 音频不证明听感；没有新增真人报告或商业样本。

## 视觉审查与下一步

主代理和独立代理查看同 34.85s 的前后原尺寸帧、33/37 秒起各 4 秒的 6fps 图板和全程总览。
平台主体和主要落点更清楚，样本中角色保持可见，后续攀升及转场未见新增阻断。
底部 1–4 栏会盖住平台左端一小段，主要落点不在遮挡区；说明牌和简化美术也仍有改善空间。
落地后平台屏幕位置继续调整，符合恢复上方预留，但抽帧不能代替真人原速判断回摆是否舒服。
没有直接逐 60fps 连续观看；保留此限制，不宣称最终镜头手感验收。

`return-camera-10s.mp4` 为完整录像 32–42 秒原速节选，没有变速或重排；
完整路线末尾包含测试场景销毁画面，短片不含该尾部。
完整 MP4 SHA256：`4D43C9211ADEA2D8E6639BEC181A9C22731AE9FCB15A01DFB4ADAC16F87D697C`。
10 秒 MP4 SHA256：`CD1D3ED9C7B100130B41A6D170AF8D80F96448ECD0655DBFF08609A533B3DFAB`。

下一项优先处理道具栏/教学卡与落点之间的层级，先用运行构图决定局部调整，不继续盲目移动相机。
真人落地停顿再抓取的原速感受、普通解压启动/源码一致性、既有独立认证和正式审美仍待完成。

## 重跑

设置 `DOTNET_ROOT`、`DOTNET_ROOT_X64` 为 `G:/godot/dotnet-sdk`，
`DOTNET_ROLL_FORWARD=LatestMajor`，将该 SDK 目录置于 PATH 前端。

```text
dotnet build --nologo
Godot_console --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs --log-file <new-pre.log> -- --phase=pre
Godot_console --headless --path . --fixed-fps 60 --script res://tests/RunPrologueTests.cs --log-file <new-post.log> -- --phase=post
Godot_console --path . --max-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/RunRealtimePrologueProbe.cs --log-file <new-realtime.log> -- --check-ascent-return
Godot_console --path . --display-driver windows --rendering-method mobile --rendering-driver vulkan --audio-driver Dummy --fixed-fps 60 --write-movie <new.avi> --script res://tests/RunTask9Tests.cs --log-file <new-movie.log> -- --generated-main --check-ascent-return
Godot_console --path . --max-fps 60 --display-driver windows --rendering-method gl_compatibility --audio-driver Dummy --quit-after 240 --log-file <new-start.log>
```
