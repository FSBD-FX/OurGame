# v308 · 零时间刷新不吞掉惯性来源

## 事实、判断与范围

上一轮 v307 已实际修复并生成当前配置总图，属于进展，不是等待或完成整个目标。本轮继续原 V03/V04、R-I/R-C/R-P 的动作与物理表现返工，工程仍为 `G:/knight/Knight`。

源码核对发现：`PlayerPresentationCoordinator.PresentCurrent(0)` 用于机械爪清除旧表现及死亡／恢复等即时事件；`SecondaryMotionModel.Step(0)` 正确保留弹簧，却由外层 `PlayerPresentationTimeline.Step` 提前更新了速度及接地历史。下一次正时间更新便看不到应有的速度变化或落地边沿，附件少了一次惯性驱动力。零时间表示即时显示刷新，不是已经经过一段运动；原步态、制动及落地子模型已有相同的“不消耗时间”边界。

这是可复现的程序合同问题，不直接等同于用户反馈的全部观感根因。正常镜像本身、附件挂点换边、动作设计优雅程度是不同问题；没有因为本次修复就声称正式动作自然或 Spine 质量已完成。

## 实际改动与原生反例

仅两份现有 C# 输入改变：

- `scripts/presentation/PlayerPresentationCoordinator.cs`：运动速度、落地历史和首次运动样本只在正时间写回。朝向历史仍每次写回，因为弹簧基底在零时间换向时已经反射，不能下一次再反射。
- `tests/unit/PlayerPresentationTests.cs`：增加两个真实 Timeline 回归，分别针对速度变化与可靠落地记录。无额外时间的刷新本身必须保留四通道，下一次正时间必须与没有多插刷新的一组相同，并有字面非零阈值，防止双方全零假绿；速度组覆盖 0、负数、NaN、正无穷被规范化为不推进的情况。

不改 Motor、移动／制动／重力参数、碰撞、命中、技能权限、键位、地图、存档格式、图片或正式分层。没有窗口、系统键鼠、新崩溃采集、付费生成、自动化、Git 变更或删除。

以下原件均保存在 `G:/knight/验证证据/october13-stable-claw/`：

| 案例 | 实际结果 |
| --- | --- |
| `thruster-rig-compatibility-20261108-053857-609-cbba5888582d446f8c2318d2d077c57d` | 旧生产代码，原生退出 1、506 ms；精确失败为 `SECONDARY_REFRESH_ACCELERATION_RED`。 |
| `thruster-rig-compatibility-20261108-054056-574-bd3a7254b9fd4caca980d55b3e65e4b6` | 仅调整两个新测试的调用顺序，旧生产代码再次退出 1、395 ms；精确失败为 `SECONDARY_REFRESH_LANDING_RED`。 |
| `thruster-rig-compatibility-20261108-054219-401-b26b4e206ac54a028d23eeda96510c97` | 最小采样修复后严格通过，原生退出 0、1,153 ms；原时间线／姿态／握持／披风等兼容委托继续运行。 |
| `thruster-ground-gait-20261108-054343-401-25e8f474f29c47c2bbfe8c8c498efdf7` | 原生退出 0、19,398 ms；原步态、30/60/120Hz 回身及真实 Draw、刹车、落地与动作接续等原阶段严格通过。 |
| `models-20261108-054508-447-75c97e9821644e0d9e5705e12a6afc86` | 完整既有 models 阶段严格通过，原生退出 0、511 ms；不冒充完整场内或美术验收。 |
| `prologue-pre-20261108-054608-234-595398e27f534a83a9ae154cdeac3a49` | **严格失败**：原生退出 0、138,482 ms，44 项及退出标记完整，但 stderr 有九条 `Handle is not initialized`。成功标记不冲销诊断，保护门为空也不等于回归成功。 |
| `prologue-post-20261108-054956-811-4e7b21fd4772479baa059c9bdbda205f` | 普通后 75 项严格通过，原生退出 0、187,090 ms；只有原合同四条精确负例警告，退出清理与最终成功标记完整。沿用 300 秒门、不增加限时，未通过旧 180 秒性能门。 |

两个反例是原宿主严格失败，没有 named-RED 豁免；保护门均无失败，原件保留。测试为受控 Timeline／原生夹具，不是无初始设置的自然标题游玩，不声称已看见真实玩家恰好触发此事件序列。

## 后续核验与开放项

本轮最终五个正向阶段为 **4 PASS／1 FAIL**，不合称整版通过。前段九条错误均经 `ScriptManagerBridge.SwapGCHandleForType`，首次显示 `ResourceLoader.Load → GD.Load → BasinSupplyRuntimeTests.CreateSampleFixture:731`，上游为 `ReturnToTitleDuringHealingStillPersistsTheClaim`；这是错误发生位置，不是实际失效资源身份或根因。历史已在其他案出现相同错误类，不能仅凭相似栈证明同一对象或与本轮修复无关。代码与原生输入在运行期间冻结，不通过增加超时、跳过原测试、重复刷绿或豁免诊断取得结果。

原始栈进一步分为同一次 Load 内四条（stderr 1/45/89/133）和随后同一次 AddChild 内五条（177/218/259/300/341），均在 Supply 第二个 `title_reentry` 内，不是九次调用或九个已识别对象。首四条早于本根 `root-created` 与本根旧定义配置，不能泛称“Instantiate 时报错”；但也不排除前一根／旧配置影响。Supply 为外层 index4、41,639–53,662 ms；新增 `PlayerPresentationTests.Run` 直到 index23、113,958–113,990 ms 才执行，因此两个新增单元测试的 `PlayerTuning.Dispose` 尚未执行，不可能是首次错误的先行原因。生产 Timeline 早已由真实 Player 使用，不能据此排除本轮生产改动。首错没有独立毫秒时间戳，不编造更精细时间。

下一次测试改动优先补原路径的标量 `cleanup-return`、`Load enter/return`、`Instantiate enter/return` 时间／范围边界，不加入等待、缓存策略、GC、对象扫描或强引用来掩盖原问题；尚无依据实施资源修复。

与 v307 最终 1,901 项输入清单相比，仅上述两个 C# 文件变化。本轮 rig／步态／models 的源码清单 SHA256 为 `76DC9054B44FAC74B3DDE35181AF04F4E4EC3A82A008816FEA4C9B18751CCD60`，六二进制清单为 `B6B20BF40C5DF0BA64C587C0875CD8C56075362EA39F6B725C01069B0B1AF368`；当前完整自然主线／探索线尚未以本版重跑，不能借用 v305 不同编译产物补签。

汇报前根实际复核五案源码／产物一致、各五保护对共 25 对无变，逐项核对当前 1,901 个输入及六二进制；前 44／后 75 对边界索引／名称／进出顺序完整，前段诊断库存精确九条。终端 `V308_FINAL_AUDIT_OK strict_pass=4 strict_fail=1 protected_pairs=25 source_rows=1901 current_binaries=6 pre_boundaries=44 post_boundaries=75 pre_errors=9 whole_goal=false`。一致性证明不是无错误证明，前段仍 FAIL。

独立只读代码审阅 Critical 0／Important 0／Minor 1：新测试均先建立正时间历史，尚未单独约束首次零刷新不建立运动历史，以及重复零刷新／朝向组合。现有旧回身原生专项通过，但不冒称填补了全部新组合；本轮记录该非阻断补测项，不在原生运行中修改冻结代码。

审阅另保留一个尚无真实调用证据的共享状态风险：`ResolveLocomotion` 的无跳跃离地标志仍可能在零时间接地快照清除。若零时间短暂接地后立即重新低速腾空，应核对 Fall／Apex 与真实跳跃观察的接续；不能把这个构造序列写成已观察玩家回归，也不能因此擅改跳跃规则。

只读勘察还指出两个后续待分辨的交接边界：非零短喷 root lean 进入动作时归零、空中载荷落地撤权时不含该载荷的局部混合。它们还没有本轮真实 Draw 反例，不直接写成已验证故障、更未修复。另有同 tick／零时间步态资格撤销的缓存顺序候选待验证；已解决的重复落地／长帧不补播和制动有效性旧项不重复报 bug。

完整正式身体／披风、家族敌人造型、角色动作语言、正常声画／操控及硬件／真人验收仍开放；历史间歇原生资源故障的根因也未关闭。原十一项与目标保持 active，不以一次程序惯性修补替代用户体验。

本轮未新增过程图稿或实录。正常汇报前只执行一次既有媒体 `-Check`，当前 12,807,896 字节、`Notify=false`，没有达到新增 20 GiB；不创建自动化、不停止工作。
