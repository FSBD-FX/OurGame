# v078 · 非激活攻击框与原始失败保全

2026-10-24；工程仍为 `G:/knight/Knight`，完整十一项目标 ACTIVE。本轮是一次实际出生准入修正及失败取证推进，不是整关、美术或发行验收。用户最终更正“等一下，是没看到”继续有效；没有打开窗口、发送系统输入或追认显示通过。

## 事实、推理、风险

原 FirstCombat 无法启动的实际 native hit 已查到：015118 案在 physics13、出生探针(602,604)的既有查询中，两次命中原 Rust 的 AttackHitbox。原敌人位于(560,603.9982)、Telegraph，攻击形状启用、layer16，但 Monitoring=false、Monitorable=false。原生形状查询仍返回非激活攻击框。这不是已经证实的“等待太短”，也不需移动玩家或关掉居民。

独立原生测试先验证 active 身体/通道拒绝，再在 End 后的 inactive 正向断言实际 RED015443。最小生产修正只在既有 Query 中跳过“精确 Hitbox CLR 类型、精确 EnemyAttack layer、当前 Monitoring=false”；64命中截断拒绝仍先执行，未知 Area、子类、混合层、active、Player/Enemy/World、预留区域与 layer0 电磁机关检查保持。原 Hitbox.TryConnect 不改。015655及最终021829严格 PASS，保留原24初始敌群、玩家身体/Hurtbox延后和三个同层外来障碍负控。

这仅是当前自动接触快照，不是未来巡逻/攻击安全证书。真实 NodeAdded 期间让同一 inactive 攻击框 Begin，挂载后二次准入仍必须拒绝并只回滚本事务对象；End 后同槽重试成功。020224这些行为均成立，但本轮测试重复解除订阅产生 ERROR，故严格 FAIL，未豁免。修正为按订阅标记恰好解除一次后，020541及最终021939严格 PASS，原共享形状挂载后变异、重入、所有权、死亡恢复检查仍保留。

启动修正后，旧测试按 Rust 类型 Single 的假设被新增居民打破。测试现按真实 EncounterId 生成的 batch0_1 名称、无 ResidentBinding、原 saved scene/HP3 选择原波次，不删除居民。020503已真实 Playing、J 命中原敌人HP3→2，但教学断言仍失败。

021503失败点进一步显示：当前子节点中的第一个 Rust 确实就是原波次，不是居民；原敌人HP2、可见、屏内、dx233.32/dy3.93，但 LOS=false。居民 rock-wanderer 在dx99.42/dy3.93、HP3、LOS=true。它处于两者之间是遮挡候选，**尚无本次原射线首个命中物身份，不能直接把居民认定为具体 collider**。不能据此改生产 resolver、放宽 LOS 或移除居民，让不满足前提的正向UI测试变绿。当前 FirstCombat 仍 FAIL；原真实K、语言/缩放、遮挡/死亡退出、重试及底层objective后续断言未全部到达。生产 first-typed 对未来不同挂载顺序的歧义仍是待验证候选，不冒认本次根因。

## 原始异常保全与完整回归

RunPrologueTests 新增纯托管 WriteEarlyFailure：输出单行JSON的真实 Type/Message/ToString并 Flush，在原失败清理 await/drain 之前执行；日志写入异常单独返回，不能替换原主异常。原 GD.Print、清理、GC/finalizer、后续 PushError、75案例与180秒上限均不改。原 registry 的真实同步/异步异常身份及失败 TextWriter 合同先 RED015843，再 GREEN020112/最终022017；三个新增合同及原分类/变异检查一起保留。

021043 fullpost native -1073741795/160461ms、不是超时。现在保留下原始托管错误：PrologueFormalHudTests 资源标签夹具 expected Playing/actual Title，而不再只有异常类型。并发 finalizer 的 FATAL 在 stderr 中先于JSON出现；所以这不是“保证每次 native 崩溃前记录成功”，也没有修复资源根因。

HUD夹具仍是原V2/Development、SubViewport、最多6 ProcessFrame；只加失败时的一次状态/准备原因/布局/姿态/处理权限标量，不重跑准入、物理查询、加等待或改12组合/24标签。最终022403本案没有触发该失败行，原12组合/24标签通过；这次安静不能关闭前一次 Title 故障或证明六帧屏障可靠。

最终 fullpre022132 native1/114313ms，无本案Handle/FATAL，仍在同一首战教学断言失败。最终 fullpost022403在原180秒上限超时，native-1/180048ms，保留明确原始失败：index49 `CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart` 的“first-death recovery completes after leaving the damage callback”。原始失败已发生，不把随后超时当成唯一根因，也不增加预算。

新时钟终于产生实测：registry index0 enter engine_ms155403/elapsed1；index46 VFX enter176964/21562、return179111/23709；index48 return179477/24075；index49 enter179478/24076。前置工作确实已消耗约155秒，本案VFX约2147ms且返回；不能再把整个超时笼统归因VFX。49个成功返回不等于75项完成，剩余成本和失败恢复边界仍需分辨。

另一最终原生 resident-geometry022057 FAIL：`interaction-target:CanyonBasin/Gameplay/GrappleSurfaces/SteamValveControl/TargetCollision/Collision` 的玩家交互保护检查不符合预期。它已通过实际51环境检查，但后续 PlayerAccess 未完整通过；未证明由本轮 Query 改动引起，也不未经验证称作旧问题、删除断言或豁免。

## 全部原生记录

根：`G:/knight/验证证据/october13-stable-claw/`。每案目录以 phase-20261024-下列时间-唯一UUID命名，保留 provenance、三份原始日志、构建/原生进程边界、前后清单及实际源/二进制副本。

| phase / 时间 | native exit / ms | 严格结果 |
| --- | --- | --- |
| first-combat 014823 | 未启动 | 构建FAIL：错误猜测Bell命名空间，不是行为RED |
| first-combat 014957 | 未启动 | 再次命名空间构建FAIL；读实际源码后纠正 |
| first-combat 015118 | 1 / 2406 | 实际非激活Rust攻击框命中、Title FAIL |
| resident-auto-start 015443 | 1 / 911 | inactive形状正向断言实际RED |
| resident-auto-start 015655 | 0 / 26954 | 最小生产修正后严格PASS |
| post-boundary-contract 015843 | 1 / 509 | 真实主错误没有提前留存，RED |
| post-boundary-contract 020112 | 0 / 529 | 原registry及提前留存3合同PASS |
| first-combat 020148 | 1 / 4328 | 旧Single被合法居民实例打破 |
| resident-level-lifecycle 020224 | 0 / 4874 | 行为到达，但重复退订ERROR，严格FAIL |
| first-combat 020503 | 1 / 4790 | 原实例真实J/HP3→2成立，教学FAIL |
| resident-level-lifecycle 020541 | 0 / 4995 | 恰好退订一次后严格PASS |
| prologue-post 021043 | -1073741795 / 160461 | HUD Title主错留存及资源FATAL，FAIL |
| first-combat 021503 | 1 / 4890 | 原实例当前LOS=false；失败点实际候选标量 |
| resident-auto-start 021829 | 0 / 36327 | 最终原24/占位/active与inactive控制PASS |
| resident-level-lifecycle 021939 | 0 / 4893 | 最终事务及所有旧生命周期PASS |
| post-boundary-contract 022017 | 0 / 520 | 最终原registry与主错误合同PASS |
| resident-geometry 022057 | 1 / 2020 | SteamValve交互目标保护断言FAIL |
| prologue-pre 022132 | 1 / 114313 | 首战教学FAIL；本案无Handle/FATAL |
| prologue-post 022403 | -1 / 180048 | index49死亡恢复断言及原180秒超时，FAIL |
| models 022734 | 0 / 521 | 最后普通模型/产物PASS |

共20案：2构建失败、18原生中7严格PASS/11严格FAIL。最终同一冻结7专项为4PASS/3FAIL，不拼成整关全绿。另一次错误的 `-Phase post` 调用在 ValidateSet 被拒、没有owner/native案例；一次审计PowerShell管道语法错误纠正后重新执行，均不当行为RED。

## 一次独立审查与全部裁决

唯一 fresh gpt-6-astra/high 只读审查未证实 Critical/Important/Minor。不是整关批准，没有复审或修复轮空转。对所有 set-aside 项逐项裁决及错误成本：

1. **任何崩溃前必留主错**：只声称进入托管catch后的改善；先前GD.Print及并发finalizer仍可能先崩。若判断过宽会漏失关键错误；故FATAL继续OPEN。
2. **FirstCombat resolver/LOS修复**：本轮不改；当前原Rust的LOS前提为false，具体首hit未知。若判断错误，首战教学可能继续不准，必须真实碰撞/有效UI夹具再核，不关居民换绿。
3. **FormalHUD就绪修复**：没有实现，最终一次标签通过不关闭旧Title。错误成本是偶发进入或文字测量失败，下一查失败时真实Flow/准备边界，而不是加睡眠。
4. **未来危险/手工伤害语义**：精确过滤仅当前automatic快照，TryConnect保持；当前未找到生产外部直接调用者。未来手工/排队接触可能需额外约束，不能将快照当完整安全证书。
5. **完整移动/恢复所有权**：本轮未改旧LoadLevel/地图/力量或原正常Main/Explore，仍全部待验。错误成本是正常游玩仍可能卡死或恢复错误；不以定点测试交付。
6. **缺少部分v077全文件基线**：所选models快照缺Task7CaseRegistryTests和ResidentSpawnRuntimeTests，只审明确新增部分/当前源码；不伪造可独立重建的全文件diff。错误成本是对已有脏改动归因有限，由各案前后快照约束。
7. **最终批次资格**：以root实际结果为准，4/7而非全通过；资源、正式AV、真人/硬件/发行全OPEN。若错误接受将发布已知失败，故不打包发行/推送或标目标完成。

此前执行裁决同时保留：使用用户指定现有codex脏checkout而不建新worktree/提交/删计划，成本是只能靠快照界定修改；临时hazard标量仅既有hit、不额外query/Load，原件保留后移除以免长期IO扰动；当前首战失败不拿未证实的first-typed歧义作生产修复；机械爪没有新增第四个猜测阈值。旧3253落点从静止不能满足原Walk60回返上界，Gallery残片自然遗漏还需保全，故原150内实际全支撑+同核制动预测的候选尚未实现。错误成本是正常路线仍未闭合，不能减掉原回返/残片/敌人要求冒充完成。没有待延期Minor。

## 保管、普通构建与完整目标

98对现存 source/helper/user/continuation/binary 前后清单逐字节一致，2构建失败缺fresh-binary pair如实保留，不能写100/100。18原生共108份封存二进制逐项匹配；最终live1776源/892C#、9个辅助路径及6份二进制漂移0。

- source manifest SHA256：`92844EBAE05E7056FC8C9C419697EE3FF40EF121FFA21C341163D070599B9E1C`
- binary manifest SHA256：`F5ACFB8263FA555F075BA2BC7125E7343C6FC8B69E699903F8A81C83A2F6E5B1`
- 普通Knight.dll SHA256：`2AAE63E361E45FFC8D7FB08088F22860DD6F4606ED25020101E86693DD639A6A`
- Git HEAD仍`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、branch仍`codex/task6-certified-final-20260902T0555`；index SHA256仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。

最后普通构建保留、root构建及原生进程全部结束；无新定义常量诊断构建、付费、美术/音频、过程媒体、自动化、Git写入、真实存档语义读取或用户资料删除。现有owner入口96例、实际旧producer日志消费33例重新PASS；这是消费者回归，不是本轮新正常路线证明。

文档同步后再次live核对漂移0/index不变。额外只读检查一度错误地用命令局部`core.autocrlf=false`，把Windows现有CRLF普遍报为行尾空白；没有据此批量改写文件或仓库设置。随后按CR-at-EOL语义复查，不将这次配置性输出冒充游戏回归失败或未经记录的修复。

正常汇报前只手动检查一次媒体：UTC18:32:00 current860284/base3508/growth856776 bytes、Notify=false，未到新20GiB档位。没有建立通知/自动化，也不因阈值停工。

原完整十一项、正常标题Main/Explore同冻结、76普通敌人及Boss/ECHO实战、全部机关、完整正式分层美术/动作/UI/声音/演出、正常速度AV、人类键盘、目标硬件性能及版本交付继续OPEN。Task13只完成hazard与托管错误取证/局部修正，未完成整组回归和路线。

下一最小步骤：首战失败的真实首hit/合法controlled UI前提；index49失败时恢复提交状态与原预算来源；SteamValve精确保护区域反例。机械爪实际Gallery支撑/制动/旧回返与残片合同、正常双路和正式AV继续推进，不被上述诊断替代。再次显示窗口须先约新的方便时段，不继承旧26秒授权。
