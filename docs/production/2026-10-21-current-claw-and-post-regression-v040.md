# KNIGHT 当前机械爪与完整程序回归 v040

2026-10-21。这是内部验证记录号，不是新的可玩发行版。资源总目录为 `G:/knight`，实际 Godot 工程为 `G:/knight/Knight`；不恢复已删除的旧归档项目。存储收尾以 [v039](2026-10-21-knight-e-retirement-v039.md) 为准。

完整 1–11 目标仍 ACTIVE。本轮只修测试兼容性、测试资源所有权和严格日志资格，没有改玩法力学、输入、碰撞、地图、素材或存档，没有付费生成、自动化、Git 提交/推送、新 ZIP 或新发行标签。

## 事实：本轮实际结果

以下均由同一个规范 owner 串行 fresh rebuild/native 执行，真实 C# 清单为 1695，列出的通过案均 exit 0、无超时、guardFailures 为空。原始日志与失败原件均保留在 `G:/knight/验证证据/october13-stable-claw/`。

| 检查 | 引擎耗时 | 原始 case |
|---|---:|---|
| 原三项机械爪呈现委托 | 4488 ms | `thruster-formal-claw-phases-20261021-201514-555-468106df23f148089d5d98f7de0602b8` |
| 五项普通游戏流程 | 3119 ms | `completion-flow-20261021-202159-058-965086bbc96c4583bf938413da14115c` |
| 原存档迁移负面检查 | 3860 ms | `completion-migration-20261021-202828-310-523aed91ee86442da2179c571b78fc57` |
| 完整 pre | 99939 ms | `prologue-pre-20261021-204436-970-352a3554e57b4594817509e2a3031a77` |
| 键盘 | 9594 ms | `keyboard-20261021-204643-739-1815a8a14f6a478aa7773c6720b5083a` |
| 动作 | 7112 ms | `actions-20261021-204721-889-8f90cce8eb8a4411b019837d25c9990a` |
| 原暂停案例后的共享场景所有权 | 980 ms | `thruster-main-scene-owner-20261021-205504-621-e035e37d1313454cb77b01ea1b8cb987` |
| 最新完整 post | 147218 ms | `prologue-post-20261021-205532-692-d8ebd8e7ef1047e0ad5903701cf5171a` |

pre/键盘/动作检查发生在最后一个 UI 测试借用资源修正之前；最新 post 与所有权专项在该修正后执行。生产源码、地图和资源没有因此改变，但不把这些不同测试快照冒称最终发行版的同冻全套验收。

最新 post 实际完成原 75 个 registry 案例，零退休；继续完成生产资源检查、Accessibility.Run 和末尾原主场景连续八次实例化/释放，节点基线为 1→1。stdout 无警告/错误，stderr 与外部镜像各保留四条已由真实负面测试产生、分作用域核验的警告；三路均无 ERROR/FATAL、资源在用或退出泄漏诊断。它不是“全日志零警告”，也不是正常玩家双路线、渲染性能或美术质量证明。

## 修正内容与推理

### 机械爪：测试与已批准规则对齐

旧断言在 Latched 后一帧要求 Swing，实际角色尚未接近停靠点；旧松键取消也与“命中后自动牵引、再次按下取消”冲突。本轮保留原委托、真实 Player/Motor/完整胶囊/碰撞、180 步上限及原等待，验证自动 Reel、真实移动超过 124 px 后到达、松键保持同一 CastId、显式取消和攻击模式取消。

七阶段 Clip 在原步进的两次 ProcessFrame 返回后读取，不在呈现尚未更新的同步状态事件内抢读。脱钩后先独立验证真实悬空和低竖速，再要求 Action=None、Locomotion/实际 Clip=Apex；不能要求回到施放前的初始化 Idle。没有改拉力或传送角色换取通过。

### post：修正错误的警告作用域

原 post 将“存档迁移”的六次 Title→Playing 和一次 Completed→Recovering 规则套到了普通流程 registry。迁移原规则与所有负面案例完整保留，并已独立运行通过；当前 registry 实际有手动恢复请求和真实 Health.Died 两次完成后恢复拒绝，应严格核验 Completed×2。

post 现在分别核验原故意抛错 observer×1、原错误场景类型×1、原终态恢复拒绝×2。消息/来源/函数仍由原 ExpectedWarningLogger 观察，匹配语义是消息精确、来源 EndsWith、方法 Contains，未冒称更强的全限定匹配。

规范 owner 的 post 专属资格门逐个检查 stdout、stderr、外部日志，不把镜像重复累加：三份作用域收据在 stdout/外部日志各一次、stderr 不得有；stdout 不得有诊断；两个警告 sink 各严格 1/1/2，多/少/未知警告、任一 ERROR/FATAL 均失败。原始文本不删除、不静音；pre、正常路线和迁移规则未放宽。旧门对合法负面测试输出的实际 RED 保留；60 个独立正/负控制通过，包括小写额外警告/伪收据的 RED→修正。它们是操作者合同测试，不是 native source/function 或玩法证明。

### 控制说明与共享主场景

当前 Accessibility.Run 的旧 Q/E/LMB/RMB 断言改为独立字面值核验 J/K/L/Q 与完整抓爪帮助，保留手柄切换、装备槽、本地化、HUD、缩放、减闪和震屏检查。原清理及等待移入 finally；另一 Task10 演出/双语入口未修改。

最后的主场景终检发现暂停测试用 `using` 释放 GD.Load 借用的缓存 PackedScene。窄原生入口证明两处引用确为同一对象：有效行为 RED `205246-624-a07b6033c91e4db0b1475dfb0b3c2db1` 中双方 ID 同为 `9223372083612878740`，原案例返回后原 owner 无效。修正只移除借用场景的 using，仍释放自己实例化的节点。GREEN 中同一 owner 返回后有效，八次实例化/释放与节点基线通过；没有重载、复制、CacheMode、GC/drain 或延时补偿。更早 `205017` 的新入口未入树错误保留，不能算有效行为 RED；新入口只补一次正常 ProcessFrame setup，原暂停案例等待未改。

独立代码审查覆盖本轮指定备份差异及纯 PS 门禁，未发现阻断问题；不覆盖整个 dirty checkout 或游戏质量。对首轮 RED 与最新 post 的 1695 行清单实际比较，仅五个测试 C# 文件有变化。

## 风险与剩余工作

- 恢复音频专项仍 strict FAIL。原样 verbose 案 `thruster-recovery-feedback-20261021-201243-316-8a648c3bafad44b28a01180f6fc174a2` 功能通过、引擎 exit 0，但退出有三 WAV＋三 PlaybackWAV、两资源仍在用；具名为 recovery-start/end，尚不能把路径对应到对象 ID 或命名第三 WAV。完整 post 通过不自动关闭该独立故障。下一步仅在现有 Load/Stop/null 处采标量归属，不 Dispose 借用音频、不按 ID 回查资源、不加 GC/drain 掩盖。
- `Task10ProductionEnglishFlowsToPause` 另有旧模式文字及旧完成出口前提，保持 OPEN。当前 Accessibility.Run 通过不代表该入口或正式出口真人验收通过。
- 正式新居民仍 0/50、24 canonical 族系资源未完成，管理器、最终出生位、机关组合与地图丰富度、最终统一美术/动作/音频/演出都未因此完成。旧资源专项未重验的项目不自动关闭。
- 下一生产切片可按已批准计划实现 ResidentPopulationState、真实 world/sector 实例与 epoch 绑定。必须处理先订阅再补枚举、卸区精确解绑、新局已挂区以及 deferred recovery reset；现有接口没有无副作用任意 HP 恢复，不得假称跨卸区存活状态完整。未就绪的资源和出生点继续被严格 loader 拒绝，不能以准备代码声称 50 只已入关。
- 正常 Main/Explore 全程、OS/真人输入、720p–4K 正常显示、稳定 60FPS、最终视听与发布 ZIP/GitHub 版本仍是独立门槛。既有 V1 未改写，也没有十万付费人数承诺。

本轮没有新过程媒体；只在关键功能/重大视觉节点保留少量媒体，目录仍为 `G:/knight/过程图稿与视频`。20 GiB 仅工作内手动检查并随正常汇报提示，不停工、不自动打包/删除、不建立自动化。详细失败顺序和备份见 `.superpowers/sdd/2026-10-21-current-claw-presentation/ledger.md`。
