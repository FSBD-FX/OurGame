# KNIGHT 常驻敌人生命周期与血量保管 v041

2026-10-21。内部实施记录，不是新发行版。唯一资源总目录 `G:/knight`，工程 `G:/knight/Knight`。完整 1–11 目标仍 ACTIVE；新增敌人正式入关仍 **0/50**，不是本轮已交付 50 只。

## 事实：实际实现与验证范围

按已批准常驻设计与执行计划，增加四个实际 C# 文件。受保护源码/资源清单由 1695 行变为 1699 行，其中 C# 文件实际由 865 增为 869；`SourceRows=1699` 不是“1699 个 C# 文件”。前轮记录中将该清单称为 C# 数量的措辞不准确，以本次逐行分类为准：

- `scripts/prologue/residents/ResidentPopulationState.cs`：独立于可卸载场景的标量名册，记录稳定 ID、轮次、存活、已观察 HP/最大 HP，以及精确 sector-instance/actor-instance/generation lease。未观察的 HP 为 null，不假造 50 次出生。
- `scripts/prologue/residents/CanyonResidentManager.cs`：订阅真实 SectorMounted 后枚举已挂载邻区；按注册表中的实际区域和 `Gameplay/Actors` 直接父级接管角色，监听真实 Health/Defeated/TreeExiting 并取消订阅。重复接管同实例幂等；同一原生身体不能计成两个名额。
- `tests/unit/ResidentPopulationStateTests.cs`：9 项状态/旧 token/重复身份/卸载/新轮次/坏参数检查。
- `tests/integration/ResidentLifecycleRuntimeTests.cs`：使用保存的五区定义、真实 V2 注册/卸载/替换以及真实保存 Rust 角色。复用原 fixture 的独立 World2D、原等待/清理与显式自有形状副本；没有改缓存、借用资源处置、GC 或 drain 规则。

`HealthComponent` 增加 narrowly internal、一次性、仅全新未初始化且未入树组件可用的 HP 准备入口。正数 HP 必须匹配实际组件最大值；原生 Health.Ready 在普通初始化后消费快照，不伪造伤害/治疗/死亡事件。普通非居民分支仍执行原初始化；已使用、已 ResetFull 或在树内组件不能被此入口改写。

实际原生证据：保存 Rust 从 HP3 真正受伤为 HP2；真实卸载后记录仍为 alive/HP2，另一只真实死亡保留 dead/HP0。返回时未准备的满血替身被拒绝；新鲜角色准备快照后，其真实 Ready 为 HP2，damage/heal/death 三类事件数均为 0，然后接管成功。死亡角色不能因卸载/重载加入同一轮。

准备还带有纯标量 owner/epoch/slot 来源标记：真实旧轮次预备角色即使在新轮次先完成 Ready，也不能抢占尚未观察 HP 的新名额。已接管角色移除此标记；manager 不缓存未入树原生节点。新轮次显式清名册后，测试通过真实 ResetForCheckpoint 重置既有角色，再接管新 epoch。

另有未占用名额上的外国逻辑 World、同 ID 未注册区域、staged 区域负控，以及对应合法正控；角色排队删除时先失去接管资格，实际单体退出/区域退出不会被当作死亡；Dispose 后真实伤害不再修改名册。

## 实际 RED → GREEN

所有原件保留在 `G:/knight/验证证据/october13-stable-claw/`，下述时间为 case 名中的本地时间：

1. state `211315`：8 案 7 失败，合法第一次接管被拒绝；实现后 `211415` 通过原 8 案。
2. lifecycle `211624`：晚绑定区域数实际 0、要求 3；实现实际订阅/枚举后 `211812` 通过。
3. state `211940`、lifecycle `212037`：同一原生角色能占两名额；只加跨 slot 原生身份拒绝后 `212205` 通过。
4. lifecycle `212343`：不能准备真实卸载 HP；实现内部门控/原生 Ready 消费后 `212456` 通过。
5. lifecycle `212719`：旧轮次预备角色被错误接管到空的新轮次名额；加入精确来源标记后 `212829` 通过。

首次 owner 参数拼接语法错误发生在运行前，已修正，**不是行为 RED**。最终 owner AST 无误，两项真实调用验证 resident-state/lifecycle 的 named waiver 均在启动引擎前拒绝。

## 同冻最终八门：7 PASS / 1 strictFAIL

规范 owner 串行 fresh rebuild；所有构建 exit0、0 warnings/errors；各 engine 均 exit0、无超时、guardFailures 为空。**exit0 不等于严格通过**。

| 阶段 | 严格结果 | engine ms | 完整 case ID |
| --- | --- | ---: | --- |
| 状态9项＋原 Health3项 | PASS | 512 | `resident-state-20261021-213044-036-e2d1fe4dff3345aea5e651a20fc76ff7` |
| 真实生命周期/HP重载 | PASS | 1430 | `resident-lifecycle-20261021-213110-588-27b4d3da0c9441d183d1b8a7f72c7171` |
| 原真实流式来源10项 | PASS | 1340 | `resident-void-stream-20261021-213141-516-580966fe97624eebae8f3ec9ea90e6bc` |
| 原完整名册/17负控 | PASS | 534 | `resident-allocation-20261021-213208-807-f2e0c1d1fa2843658f393e11c1c77fc5` |
| 键盘 | PASS | 9699 | `keyboard-20261021-213236-386-0d54eeec056c4d86bed45fa78cedf4f9` |
| 动作 | PASS | 6963 | `actions-20261021-213315-211-3e9f2bfc1cf94ba18399869991d754e5` |
| 完整 pre | **FAIL** | 99760 | `prologue-pre-20261021-213347-798-24587a65a5574ad1b62137c9a1fd27b9` |
| 完整 post | PASS | 148046 | `prologue-post-20261021-213554-070-9f23e45a81a84b93b5046b19034a7c94` |

pre 功能断言返回、stdout 虽有 PHASE_OK，但 stderr 实际有 **5 条 Handle is not initialized**，均为 `SwapGCHandleForType → Node.AddChild → BasinSupplyRuntimeTests.OpenSample:459`。外部日志镜像这同 5 条，不是另外 5 条。没有警告或超时；严格失败没有豁免。不把该位置历史上出现过视为已证明与本轮变化无关，原因归属仍需诊断，也不原样重跑直到偶然通过。

post 实际执行原 75 registry 案、零退休；随后按原 runner 完成生产资源检查和 Accessibility.Run，再完成原同 owner 主场景 8 次实例化/释放、节点 1→1。没有独立 Accessibility 完成标记，因此不能把 HUD 的 `12 combinations / 24 labels` 标记当作全方位可访问性验收。stderr/外部镜像各保留同 4 条故意负面警告，按 seamless1/room-profile1/flow-registry2 三作用域核验；没有 ERROR、Handle、ObjectDB、RID 或资源在用诊断。不是全日志零警告，也不是正常玩家双路线/渲染性能证明。

独立只读证据审计：40/40 对 source/helper/user/binary/continuation 清单逐字节一致；全部 8 案共享同 1699-row source SHA256 `9BA835D00DD56E3C23B76C368CA65459582846836D1EA8DE1BEF568A1D4D0ED6`。共同 9 项 helper 相同；pre 仅额外纳入原 `test-clearance-native-red-admission.ps1`，不是共同 helper 漂移。user109、binary6、continuation1 的前后及跨案保持。实际 native PID/contract/wait/provenance 相符。所有消费者已终态才更新本记录。

## 推理、取舍和风险

- 提前做必要生命周期底座，不让七个未解决出生点阻止独立状态实现；代价是必须另外证明正式装载器真的消费这些规则，不能以 fixture 冒称全50接入。
- 使用全新组件的专用 HP 准备，不靠 ApplyDamage/RestoreHealth/反射凑数；代价是需要对全部承载类型、原生初始化和真实恢复边界补证。目前真实 HP 重载只证明 Rust；九类 Ready 的只读审查显示没有随后 ResetFull，但静态结论不能替代九类原生证明。
- 保留现有脏的权威 G checkout、无 Git 写入/清理；代价是用明确的冻结清单和原始案保管链证明本轮范围，不宣称整个工作树干净。
- 旧轮次准备标记用于拒绝旧工作，不代替完整原子出生事务。pending actor 在失败时的销毁、重复 manager 的唯一 owner、实际碰撞/保护区准入、正式身体/profile 身份、睡眠/显形/姿态连续性都仍须完成。

## 尚未完成及下一步

1. 将唯一 manager 实际接入 Level 的 new run、旧档首次初始化，以及真实目的区/有效 departing 实例恢复闭包。不能只监听可能早于敌人实际 reset 的 RecoveryCompleted；不能 actor/manager 双重重置。
2. 正式原子出生、50 个真实位置/活动/玩家接近证明、7 个空间缺口、24 个正式族系资源和全50跨区身份历史；原26+新50=76普通+1Boss、骨骸10/幽灵10等数量不减。名册本轮仍输出 50 sites/50 scenes unresolved、spawned0。
3. 本次完整 pre 的5句柄错误；历史154/31等资源失败也未因此关闭。完整 post 通过不能替代这些门。
4. 恢复音效快速退出专项最近失败仍是 [v040](2026-10-21-current-claw-and-post-regression-v040.md) 保留的3 WAV＋3 PlaybackWAV；本轮未改音频/未重新执行它，不宣布修复。只读调查得到下一次标量追踪方案：从实际既有 GD.Load 返回值记录 ID/路径/voice，覆盖音乐与SFX/自有创建，Stop/null 只记既有标量；不新增 Stream getter、GetStreamPlayback、实例反查、借用 Dispose 或等待。它还不能证明匿名第三 WAV 或播放对象配对。
5. 另一 Task10 演出/双语入口、最终美术/UI/动作/音频/演出、丰富机关、同冻正常 Main/Explore、真人、720p–4K/真实60FPS、Windows ZIP 和经授权 GitHub 新版本归档仍 OPEN。

没有新截图/录屏或素材生成：本轮无可交付的视觉变化；后续关键媒体仍只保存在 `G:/knight/过程图稿与视频`，不恢复旧归档目录。没有付费、自动化、前台模拟按键、安装升级、删除、真实用户存档语义读取、Git提交/推送、新包或新标签。全计划 Tasks1–4 仍未完成，不写 task-done/整计划验收通过标记。
