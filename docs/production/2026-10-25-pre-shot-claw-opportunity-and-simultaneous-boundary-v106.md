# v106 · 开火前抓爪机会与同帧完成边界

这是开发记录，不是发行版本、完整关卡交付或视觉验收。工程仍为 `G:/knight/Knight`，另一个 UE 项目不在范围内。原十一项稳定 ID、76 普通敌人＋Boss/NPC、机关、正常双路线、正式视听/相机、真人硬件及发行要求全部保留，目标 ACTIVE。用户最终更正为“没看到窗口”；本轮没有开窗、系统键鼠输入、付费生成、自动化、Git 写入、打包或删除。

## 事实、推理、风险

事实：本轮只改变两个测试消费者，没有改生产角色、敌人、数值、地形或正式资源。旧主线消费者在首击后，明明生产抓爪已经发布同一目标的可见预览，却仍等它开火才出爪。新增独立观测先在实际主线触发 `MIXED_EARLY_K_RED`，再允许一次真实早 K；最终同版主线真实 application/Stunned 成立、HP3 保持，但后续仍耗尽能量并死亡。完整前后段、原四混编案例、counter 和 models 通过，正常 Main/Explore 失败。

推理：早 K 的合法机会确实存在，但“让 K 提前”本身不足以形成完整反击窗口，不能据此宣布实战体验改善。新的 J 前提仍要求首次 J 捕获的同一 owned Burst 真正开火；不能用其他敌人开火或 group 完成补证。同步边界控制用例证明了另一个验证误判，已经完成 RED→GREEN，但不代表自然关卡实际发生过该边界。

风险：当前仍未证明正常主线与探索通关，历史资源句柄/FATAL 原因未闭环，正式画面与相机未由用户看到。一次完整 post 安静不等于资源修复；这些计时也不是目标硬件 FPS 或真人游玩证明。不得缩减原十一项来结项。

## 实际改动与保持不变的约束

- `PrologueCanyonGeneratedMainRouteTests.cs`：从原 `ClawFrameChanged` 的 Idle 发布点复制生产 `KeyboardPreview` 的目标、Exists/LOS、physics/input sequence；这是 pre-Motor 观测，外层最多一 physics 步旧值且 sequence 必须一致，不调用第二次 resolver/ray，不强置目标。
- 观测机会独立于发 K。一次真实 K 的 sample/edge、同 cast/target Latched、adapter application＋1、Stunned、Idle 交接和原 HP/支撑/零额外跳喷恢复约束保留。提前 K 不豁免实际命中或终态义务。
- 首击捕获增加 `group.OwnsLiveActor`。Shot/Defeated 只订该 Burst；致死 J 在进入分支及盾牌释放等待后的 fresh 边界重新核同实例、ownership、HP1 和真实 Shot ordinal。终态独立要求该实例的 Shot、致死 J 接触及 Defeated，不能只看全局连接数或 group 完成。允许生产同步 Defeated 先于 AttackConnected；清理仍成对解除订阅。
- `RangedWeaponPlacementRuntimeTests.cs`：原 mixed 消费者入口增加同帧成功/窗口到期正例和八个缺失、重复、已中止证据负例；调用的完成边界也由真实 Main 消费。没有新测试阶段或生产辅助接口。
- 原 900 外层预算、完整 J＋6＋2 tick、24 帧冷却、原能量/保留量、全部危险与旧弹扫描均不变；不因本轮失败继续猜调阈值、回血、加能量、删敌人或忽略日志。

## 原生尝试与终态

全部案保留在 `G:/knight/验证证据/october13-stable-claw/`。表中毫秒为各自原始 wait，不混合不同版本或身份。12 次尝试＝2 编译失败＋10 原生；原生 5 严格 PASS / 5 FAIL，最终七案同一源为 5 PASS / 2 FAIL。串行批命令最终 exit0 只表示完成循环，不表示所有 phase 通过。

| 案目录 | build exit / ms | native exit / ms | 结果 |
|---|---:|---:|---|
| `thruster-route-main-diagnostic-20261025-043305-654-40b8d36348c449b1a742108d8c45ade6` | 0 / 31202 | 1 / 147119 | 实际早机会到期 RED；非超时 |
| `thruster-route-main-diagnostic-20261025-043811-590-659eb020ef7742cca4095c0059abbb37` | 1 / 24396 | 未启动 | Shot lambda 过早调用捕获未初始化状态的日志函数，CS0165/CS8079；删除该多余日志，保留真实标量。不是行为 RED |
| `thruster-route-main-diagnostic-20261025-044019-078-0227b901554c4f7e81268855bb97531c` | 0 / 25836 | 1 / 139630 | 早 K 成功，后续死亡触发位置重置断言 |
| `ranged-mixed-counter-20261025-044515-080-da1337845099484f8e30e1c57f5bb060` | 1 / 27243 | 未启动 | tuple 名推断导致 6 CS8123＋6 CS1061；显式声明 tuple 数组类型。不是行为 RED |
| `ranged-mixed-counter-20261025-044628-977-b208ae62e8d34acbb4e31c1b13259218` | 0 / 32651 | 1 / 579 | `MIXED_K_SIMULTANEOUS_RED`，审查反例真实拒绝原错误顺序 |
| `ranged-mixed-resource-20261025-044756-223-3412e608bfc4494caa437c48dbc34190` | 0 / 26452 | 0 / 16221 | 原四组 mixed 严格 PASS，新增边界用例亦执行 |
| `ranged-mixed-counter-20261025-044847-597-c876004bb0ee41ce8d0d32dedb368064` | 0 / 29240 | 0 / 4178 | 边界 GREEN＋原实际 counter 案 PASS |
| `thruster-route-main-diagnostic-20261025-044928-894-d4f0286845dc43faa870e914d1a96be6` | 0 / 30921 | 1 / 138043 | Main FAIL，死亡恢复1/0，非 FATAL、非超时 |
| `thruster-route-exploration-diagnostic-20261025-045227-505-2d5945f6dcd14176af0b5c53a73e6880` | 0 / 28466 | 1 / 56313 | Explore FAIL，physics2861 恢复1/0，非超时 |
| `prologue-pre-20261025-045402-095-6075b640cc234a10b24aba1a41a65140` | 0 / 36157 | 0 / 168209 | 原完整 pre 严格 PASS |
| `prologue-post-20261025-045738-212-b9200af3be3c4c418cd3f3ca6e67b135` | 0 / 29377 | 0 / 209201 | 原75 ENTER/RETURN各75、index0..74；原 owner 重复8次、三警告 scope 齐全，严格 PASS |
| `models-20261025-050146-405-3cf1fb5716e047998f97edb06085bd01` | 0 / 26079 | 0 / 572 | 普通 Debug 恢复/模型严格 PASS |

post 使用已有普通总 cap300000ms，不是旧180000ms通过，也不是新增测量放宽。三个预期 scope 为 seamless1、room-profile1、flow-registry2，共四条故意负测试 WARNING；没有豁免新错误。所有原生均非超时、普通无 REF 构建；本轮未做新的资源归因诊断。

## 抓取与后续死亡的证据边界

首次 RED 案：first J6814；生产 Idle preview6827/sample6799 选中 target17380843460464、Exists/LOS均 True；外层6828/HP3/能量51.666576/安全时间.4333333 记录真实机会，旧策略未发 K，之后触发具名 RED。

中间 Main 案 `044019` 的同 owned Burst 为17383007721559：K6829、Latched/Released6834、application/Stunned6835、Idle6839，HP3 与能量51.666576 保持。K 收回后280条连续决策没有缺 tick，目标与攻击距离保持，但无一个 `mixed_counter_ready=True`，K 后 J 命令0次。能量≥24时最大安全时间仅.33333325，不足完整门.43333334；6937首次低24，6949才发首 shot，6996盾事件能量归零。7087–7089虽出现完整时间窗，却已 E0。

该中间案真实伤害：7064 HP3→2、7086 HP2→1 均由 resident precision-burst4777228374996 的 shot13/15；7118由同 owned Burst 的 shot4 致死。不能把当时 danger winner Rust 当实际伤害来源，也不能把 shot-before-lethal 说成本案唯一阻断。

最终同版 Main 是另一进程/身份，独立证明6828真实早机会、6834同 cast11 application/Stunned/HP3；随后7117恢复1/0、原位置断言捕获3085.19跳变。未用中间案身份给最终案续签。新同实例致死 J 终态接线尚未在完整主线获得正向通关证明；保留待验证。

## 一次审查、裁决与代价

一次 fresh-context 只读审查 C0/I1/M0，无第二次审查。I1：原早窗口到期断言先于已有真实成功回执消费，成功与窗口关闭同帧可误拒绝。先提取原错误顺序，真实 native579ms 触发反例，再仅将机会到期判断移到真实完成消费之后；最终 mixed 原生 GREEN。八负例继续拒绝缺 application、重复 application、缺 Stunned、错 latch、缺/重复 sample、重复 edge、已 abort。整 suite 仍有 Main/Explore FAIL，故不运行 task-done/finish，不称全修复完成。

Reviewer 的全部 Declined 项由 root 如下裁决，没有静默豁免：

| 项 | root 裁决及错误代价 |
|---|---|
| 当前 Main 运行结果 | root 实际跑了最终 Main，明确 FAIL；不把审查替代运行，否则会虚报通关 |
| 完整 W 输入/排名正确性 | 生产不变；本轮仅认证所记录 preview 来源，旧全局输入/排名风险仍保留，否则会把一个点扩大成全关正确 |
| 新的物理首命中遮挡归属 | 本轮不加 ray；真实同目标 K 命中可证明本次可达，不推断其他首命中实体，否则会误修排名/碰撞 |
| Intro/preparation→Playing | 未改准备协议；完整原 pre/post 已运行，不扩大到所有未来入口，否则会掩盖准备态假测试 |
| detached callback/deferred 退出 | 未改该链；不以本轮 K 认可所有脱树行为，否则会漏实际回调或恢复错误 |
| LoadLevel 自有清理与双异常 | 未改，不捆绑新重构；原故障拥有者与聚合合同仍保留，否则会因早抛异常泄漏挂载对象 |
| 完整双路/pre/post/models/资源根因 | root 最终七案如表，双路失败明确保留；一次无 FATAL 不关闭历史故障，否则会把时序扰动当修复 |
| 正式图声/相机/真人/硬件/原十一项 | 全部仍 OPEN/NOTSEEN，未重开窗；否则会将无窗口验证冒称用户所见和最终验收 |

其他裁决：沿用用户指定脏非 main checkout，不新建 worktree/Git 写入，代价是用显式源/二进制封存归因；用 apply_patch 更新原计划 ledger、不覆写旧 BASE、不删工作区。早 K 只改变测试策略，最终 owned Shot 证明不删，代价是当前战斗仍可能没有合法反击；资源没有明确因果则不改生产缓存/GC/Dispose，代价是资源稳定性未关闭。没有 deferred Minor。

## 保管与存储

- 初五案23有效 manifest pair/18 sealed bin，最终七案35 pair/42 bin，累计58/58、60/60匹配。两个 build-only 案按预期各缺一个 binary pair及六 sealed、无 native，不计额外丢失；其余无缺失/漂移。
- 最终七案 source 清单相同。helper行数10/10/9/9/10/10/9，九共同项逐 SHA 相同；每案自己的 before/after 都一致，不称所有 helper 清单完全一样。
- root 最后重枚举并 hash live1546＋imported298＝1844、C#892，missing/extra/drift0；对v105仅上述两个测试 C# 变化，没有新增/删除源，生产数值/场景/资源原样。九 helper与当前普通 Debug六件逐 SHA 匹配最终 models。
- source清单 SHA `2F347F3E515FB2AB335FE5440E4FFB88FBFDB001EBB66A51FD4BC31860142844`；helper清单 `B64F5B9D555CC789CF4B75559DF43B33A43E1126A3B15F4BC39AD8751A377A41`；bin清单 `C27A91FD98D350E33333CF420322C52E5C13DCEFB7B8F3D5EF5610D526948881`。
- 原 audio SHA `CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`、branch `codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变；gitdir/common同为`.git`、无superproject。diff check0，仅169既有LF/CRLF提示，其他0。所有 owned engine/build 已终态。
- 仅一次既有媒体 Check：UTC2026-10-24T21:05:08.3850861Z，Current860284/Base3508/Growth856776字节、Notify=false，新媒体0，无自动化或独立提醒。G盘当次约189320339456字节可用；未因容量停工、迁移或删除。

## 下一真实验证

不再靠提前 K 或继续调 warning 常量声称能赢。先验证原正常 Main 的 stage4 之后，真实 Shift 短喷＋放键＋反向制动能否在原900循环内形成完整合法 J 窗。已有 `ThrusterPlayerRuntimeTests.ForceCoastAndBrake` 是实际键盘输入入口，但当前 landing 仅200宽、明显窄于arena3840..5120；放Shift不截短已接受burn，L瞄准又会覆盖方向。必须重新证明实际AcceptedDash＋1、采样轴/速度、完整支撑、arena与HP/零恢复，不能直接移植平地14tick滑行或添加等待池。该候选未实施、未保证可赢；也不能据此豁免原同owned/K/Shot/全部外来危险或完整J证明。

原十一项中的地图/全部敌人与机关、正式分层动作与视听UI、恢复继续/实体出口、相机/真人硬件/授权发行等最终证据仍需完成；本轮只闭合早机会观测与验证边界，没有替换最终目标。
