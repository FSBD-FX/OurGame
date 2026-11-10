# v312 · 零时间刷新保留转身反应

## 事实：复现及最小修复

完整 KNIGHT 原十一项目标仍 ACTIVE。本轮推进 V03／V04／R-I 的动作状态正确性，而非继续增加资源诊断。实际工程为 `G:/knight/Knight`；未开窗口、发送系统输入、付费生成、采集新原生崩溃、创建自动化、改 Git、删除或写真实玩家存档。

`PlayerPresentationTimeline` 原用同一 `_previousFacing` 同时表示当前坐标基和最近运动样本朝向。`Step(input, 0)` 已立即反射弹簧，但 `SecondaryMotionModel.Step` 不积分零时间；随后 `_previousFacing` 被提交，下一正时间步骤不再检测到 Turn。结果是转身载荷被一次即时刷新吞掉。v308 的速度／落地历史修复没有涵盖这条独立朝向源。

最小反例：两个相同 timeline 各采样 Facing=1、零速度、接地、.02 秒；其中一个先刷新 Facing=-1、零时间；再让两者用同样 Facing=-1 推进 .02 秒。实际原生 RED 的 Mantle.Y 为直接路径 `0.02878301`、零刷新路径 `0`，与临界弹簧闭式推导相符。

唯一生产改动在 `scripts/presentation/PlayerPresentationCoordinator.cs`：增加私有 `_previousMotionFacing`，Turn 依据该正时间采样历史判断，仅 delta>0 时提交。原 `_previousFacing` 仍即时提交、仍即时反射位置和隐藏速度；不延迟坐标基，不排队或累加零时间往返的假转身。不改输入、力学、速度、技能、数值、碰撞、资源／GC／缓存或素材。

另外两份变化是测试：

- `tests/unit/PlayerPresentationTests.cs`：双朝向×四类非推进时间共 8 例，重复零刷新、下一正步全通道等价、后续一次性长尾；另有 2 例零时间来回翻面，不制造新冲量。首步 Mantle.Y 手算期望 `.028783012`；第二步有限脉冲期望 `.074925166`，来自 step-response(.04)−step-response(.02)，能拒绝两条 timeline 同时持续重放 Turn 的同错同绿。
- `tests/integration/PlayerFormalVisualTests.SecondaryFacing.cs`：保留原即时世界连续性和原容差；长尾改为比较两条具有相同最终朝向／转身源的 timeline，仍为 6 组、24 通道、840 个长尾检查。原测试将“一条转身、一条从不转身”误称双方无源，修复后不能沿用。新增两个真实 SecondaryMotionModel 的独立零源反射对照：先建立四通道非零位置／旋转和隐藏速度，再只反射一个、双方以零源积分；另保留 24 通道／840 检查，继续防止只反射位置、不反射动量的回归。没有删除或放宽原即时几何不变量。

## 原生证据

均用既有规范 runner `verify-claw-knight-v001.ps1 -Phase <phase> -SourceRows 1901`；无命名 RED、身份构建、录像或额外放行旗标。以下目录均在 `G:/knight/验证证据/october13-stable-claw/`。

| 阶段 | 原生结果 | 目录 |
|---|---|---|
| 测试先行 rig RED | exit1／615ms，精确 `SECONDARY_REFRESH_TURN_RED`，期望 Y=.02878301、实际0 | `thruster-rig-compatibility-20261108-071054-517-1ea9fabccd004ce0ba72576029384743` |
| 同源转身长尾 gait RED | exit1／10808ms，Mantle 世界Y期望 .17148456、实际 .09692428 | `thruster-ground-gait-20261108-071255-176-ffc4cf0f1ff64368bd3fe323c09065db` |
| 最终 rig | 严格 PASS／1252ms；8转身＋2往返及旧兼容用例 | `thruster-rig-compatibility-20261108-071745-248-9232d0df56514f3089c5dc82b977b7fa` |
| 最终 ground-gait | 严格 PASS／19477ms；两套长尾、实际 Draw 几何、旧步态链 | `thruster-ground-gait-20261108-071914-883-c2f61a044dcb4ef9be808e4b6f8f0408` |
| 最终普通 pre | 严格 PASS／146589ms，原44项 | `prologue-pre-20261108-072025-654-b5d5f7030cf9477dac870b4687d52dc9` |
| 最终普通 post | 严格 PASS／192429ms，原75项 | `prologue-post-20261108-072340-155-09cdb4cb537c40d1a72bce71a9409310` |

生产修复在两条 RED 均实际失败后才实施。两个中间 GREEN（071429 rig、071555 gait）也保留，但其后补强第二步独立期望；没有用中间版替代最终证据。最终四阶段同源同六二进制，failure=null、guardFailures=[]。

最终 gait 保留 `SECONDARY_FACING_DRAW_OK cases=6 channels=24`、固定披风上沿、单根翻面、不缩放子部件；原步态 Draw 为 540 样本、三采样率。它们是无窗口几何证据，`pixels=false`／`human=false`，不是用户看过画面。

最终 post 只有原合同四条负例警告：seamless 1、room-profile 1、flow-registry 2，无未解释 ERROR。192429ms 使用既有300000ms上限，不签旧180000ms或硬件性能。

本轮 pre 的 v311 解析器实际读回 exit0，180 行／90 对／5根，旧30／40观察保留，无原错误、stderr非空行0；不据此认为旧句柄故障根因已修。

最终输入清单 SHA256：

`F3F65CCDF151C9F87F2C74597E3351A30DAFCA6CC11AEB32A68DA2252A462007`

最终六二进制清单 SHA256：

`6D22C55AA1503B7D929AF1EC9C57FEA04B6A77B3F0B9DB97DE2D3874BC2D399E`

根逐项读回1901输入、10既有helper、6当前二进制，漂移为空；与v311相比恰为上述三份C#变化。没有更改规范runner或诊断解析器。

## 推理与验收边界

有一条正常操作的源码链可触发即时刷新：Motor 接受 Shift 前先 TryDetachForThruster，爪同步发布 Idle，Coordinator 的非Idle→Idle回调执行 `PresentCurrent(0f)`。此时朝向可从锚点方向切换为当前速度方向；右上锚点、向左切向速度是合法前置状态。同步链由源码核实，尚未在自然场景运行并观察该具体操作；模型反例和几何测试不能冒称正常窗口可见复现。

唯一独立源码审查 C0／I0／M0；它确认基变换／正时间历史分离及独立期望，不代签原生或正常观感。原生结果由根执行与读回。正式动作的设计感、整套连贯性、披风布料观感、A方向正式美术分层、正常视听／硬件／真人仍未验收；原十一项不勾完成。历史资源异常继续开放，不重跑筛绿，不把这次程序修复说成整套动作已精美完成。

## 后续内容与只读候选

继续补齐原地图／机关／敌人／美术与动作的实质内容，不以工具小修替代完整序章。T03 空中往返／摆动齿轮仍是原请求的开放项，现有摆锤不能自动算完成；新增前须核当前实体空间、通路、雾下预警与既有机关，不能堆入无依据的新总数。

本轮另一路只读核对未发现普通能量／热量 HUD 自身可信错显。留下候选 `Q-ATTACK-ENERGY-BOUNDARY-01`：ActionModes 的 `energy+.0001 >= 12` 与实际扣款严格 `energy>=12` 不一致，可能令扣款失败后 RejectAttackRequest 又因 CanBeginAttack=true 早退、HUD收到None。可复测输入为满能量后 `TrySpendHeavyGuard(88.00005f)`，静态float32余量约11.9999466。现有测试只验余量10；该候选未运行原生、未修复、未算本轮成果，后续先真实反例再决定。未把无正常来源的NaN UI输入当作玩家缺陷。

无新增过程图稿／视频；汇报前仅运行一次现有媒体 `-Check`，12,807,896字节、Notify=false，未达新增20GiB。无自动化或独立提醒，目标保持ACTIVE。
