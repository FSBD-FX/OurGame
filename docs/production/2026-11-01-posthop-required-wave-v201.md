# v201 · 实际必需目标优先已生效，清遭遇与完整主线仍未完成

## 事实与范围

继续 **G:/knight/Knight**，原全部十一项目标ACTIVE。上一轮v200是实际弹体尾端寿命修复；本轮只改两个测试CS的自动试玩选敌与证据，不改玩家解析器、敌人/伤害/能量/物理/地图、美术、键位、存档或测试预算。**本计划的“完成必需遭遇”目标没有达到**，不从局部选择器正确性宣布完成。

现行混选把owned与resident一起按距离取最近；原owned Burst死亡后，近处驻留枪手取代仍活必需Rust。新增默认关闭的 `preferLiveOwnedAfterHop`：先保留原精确Burst接续，再在原74px带中选择实际 `group.OwnsLiveActor`、正HP、有效/未排队、有限位置、直接actorRoot子节点的最近成员；没有合格成员回原分支。Main仅在counterweight/batch1、原mixed schedule、真实Hop5、fatal+defeated及玩家/Rust身份/HP/恢复权限保留时启用。

驻留者不删除、不冻结，仍进入原全来源危险扫描；原J/K条件、盾方向、唯一Hop/三弹/落地/HP/原900及最终通关门不变。既有原决策记录追加selected-owned ID/HP/body，不增加扫描、输入或等待池。

## 测试先行与当前实际结果

真实保存Rust/Burst对与实际绑定resident，使用虚拟查询坐标：先证明默认真的选近resident，忽略新开关时具名 `MIXED_REQUIRED_WAVE_PRIORITY_RED`。实现后同例选真实Rust；旧HP2 Burst接续仍先行，错root、伪装名字、74px带、pending wave、只读HP/位置及三个来源入危险观察均保持。临时resident/fake节点在finally同步释放，正常战斗fixture继续前已清理。**这是虚拟选择查询，不是Main历史站位重放或实战通过。**

### 改后的真实Main

当前Main `a1efae53` 原900内仍未清counterweight-lift，严格FAIL，玩家HP2、恢复0/0。但本轮已获得直接配对记录：

- physics6781 / loop624：owned Rust16030629890889，HP3，body(4665.298,3427.9336)；玩家(4620.1094,3423.9685)。
- physics7058 / loop899：仍为同owned Rust，HP2，body(4649.186,3427.9336)；玩家(4627.202,3423.9336)，完整支撑、距离21.984。
- 276条启用后的原决策记录都选该owned Rust；不推广到全900帧。已不再把最后resident HP2当作最后owned HP。
- 真实Hop5/唯一Space/三发passed/自然回能保留。三弹实际3退休/3端点，但journal `complete=false/cycle_retained=false`，不能算完整同周期资格通过。

## 反击阻点量化，不再猜单一原因

只分析现有276条原outer决策（两帧原J内部等待没有outer行），不加测试或选择性重跑。`Task10MixedCounterWindow`实际要求未攻击、safeCounter至少(.06+.10+.14)+8/60≈0.43333334秒、能量至少24（两次AttackCost12）。日志35% `energy_gate`不是这个门。

| 条件 | 原决策行数 |
|---|---:|
| clock达标 |49|
| energy≥24 |102|
| 两者同时达标 |11|
| 再要求当前未攻击 |1|
| Rust降到HP2后未攻击且clock/energy都达标 |0|

唯一合法交集6848（S0.65/E约37.8665）已真实发J，Rust3→2。6851–6860另外十条交集都在同一次J执行中，不是十次漏掉的机会。HP2非攻击且clock合格的38条里最大energy15.133247；HP2非攻击且energy≥24时最大clock0.33333325。7058末S0.09999997/E22.7333，两门都不足，不是几何或单次afford_attack拒绝。不能把分开出现的最大clock和最大能量拼成同一机会。

现有foreign Rust K已于6640接受cast13、6652保留完整receipt；旧分支要求selected==captured Burst且该owner尚未suppressed。当前selected是Rust且owner已登记，所以第二K未进入此branch；不能称第二K实际LOS或resolver拒绝。postHop W分支也只接受Moth。

生产普通Rust/Legacy adapter没有“用一次就禁用”的累计限制，但active cast中按K是取消；已经Stunned时再K不保证刷新timer。因此下一独立实验应验证第一次真实键盘K完成、原receipt保留、自然退出Stunned并进入新预警，再正常第二K→实际application/stun→回Idle/HP资源恢复守恒。**目前没有这项完整专项实证**；不能仅删HashSet或给Main加等待池来宣称获准/通关。能力证明之后仍需有限、阶段限定的调度合同。

## 当前同版回归

全部canonical、SourceRows1872、原build180s，运行时source/helpers冻结；未使用ExpectedFailure、fixedFPS/movie、扩大原时限或屏蔽错误。证据根 `G:/knight/验证证据/october13-stable-claw/`。

| 案（20261101后时间/尾码） | build ms | native ms | 结果 |
|---|---:|---:|---|
| policy143241 /e124a482 |30518|1833|具名实际行为RED |
| policy143420 /07efabc1 |30085|3743|完整mixed-policy严格PASS |
| Main143628 /a1efae53 |31941|167767|原900未清，严格FAIL |
| actions145206 /54d736d9 |28032|9406|动作阶段严格PASS |
| models145250 /d04e1e54 |30138|531|机械爪模型子集严格PASS |
| pre145327 /2eded8e5 |31175|175605|原180000预算严格PASS |
| post145702 /bc4394ed |29754|214571|原300000预算严格PASS |
| Explore150114 /9d62d648 |28201|328740|原600000预算严格PASS |

共 **6限定PASS、1真实RED、1完整Main FAIL**，不是全部通过。build均0warning/error；RED和Main各保留1原断言ERROR；post仅原四限定负控WARNING，其他阶段无ERROR/WARNING/FATAL。post原75同名/index有序enter→return、owner重复8次/nodes1→1和完整退出均过。pre只证明本次门限内通过，不称稳定性能改善。当前未单独重跑未改的ranged-native-geometry/model阶段，不把v200旧二进制资格贴到本轮。

Explore同player47546631684从title、11挑战类别、真实Boss fatal99，physics18142实体支撑越出口；完成一次，保持D60帧身体与端点存档SHA不变、恢复0/0。不证明OS输入/写入次数，coverage/human/fullGoal/normal-display/performance均未认证；探索线不能替代失败Main。

## 审查、延期和裁定

唯一fresh全变更审查 **C0/I0/M2**，没有C/I修补或二审；审查明确清遭遇目标未达成。

- M1延期：新opt-in正例选择的Rust也是第一个owned child，不能独立抓住“错误返回第一个而不是最近”的mutation。当前距离比较正确；后续应加不传continuation、查询更靠近第二个owned的原位控制。
- M2延期：Main启用门缺Hop/fatal/defeated/身份失效的逐项负向组合。实际正确路径及当前源码conjunction/MixedGuardRetained检查已证，不扩大成完整负向覆盖。
- 旧v200位移oracle/精确退休时机，v199混合危险组合/陈旧日志标记及更早延期继续保留，不顺手改。

裁定与代价：

1. 仅局部自动追击策略，不改玩家目标排序或豁免optional危险；代价是全部攻击时钟仍可令Main失败，确实未完成收尾。
2. 用真实保存演员作虚拟query，不用未知6899 Rust body或hitbox冒充实战位置；代价是必须看当前真实Main，现已直接取得HP/body，但未通过通关。
3. 沿现codex分支与冻结档案，不Git helper/worktree/提交/删除；代价是无发布/合并资格。
4. 当前聚合资格由Root实际终态与独立保管给出，不能借旧v200或源码审查信心；代价是Main失败与局部计划目标OPEN持续保留。
5. 十一项、正式视听/真人/性能、历史资源/存档、脏基线其他改动、任意地图变换/极端配置不由本轮认证；代价是所有原未完成验收仍需逐项完成，而非被排除出目标。
6. 保留已有canonical实测与档案，不为写完成标记重跑相同失败或使用要求Git/删除的task-done流程；代价是不写本计划完成，更不改原完整目标的成功标准。

## 保管与下一步

8案40清单对、4004选中source副本、88helper副本、48bin逐SHA一致；16owned按创建/路径身份终结且PID不存在。07:11:10UTC当前1872源清单项/Explore同consumer9helpers/6bin一致；不同consumer helper清单分别保留。

Main `ABBFC64F78F47CB71D8B2A4FF38C8FD0FDF7F958F8F277998C64420F819F143B`；Placement `4AEFF9C55463B02DF6B87B61E356C0D2B187D46E3DCD7B6989C8596996303A8A`；sourceManifest `BF81F9F6B23FD954C612A1A363420908EA2068CC76E4B265B5BDC04571F95FEA`；DLL `5458FA772CA56E13C2A6BE28222F0CC29A277FE175EEF92C3373B2C45DBF8599`。生产Projectile235BF/弹体测试2ACF/ActionsEE5/MotorCB65/GeometryA359/canonical7A79/HEAD e7c70498/index21610/原codex分支不变。

下一步先做第二次普通Rust K的真实能力检验，再决定有限postHop中断调度；不预认重复K刷新stun，不删旧一次性策略限制或改反击储备。完整Main及本计划清遭遇目标OPEN；原十一项继续ACTIVE。

无新正式美术/动作/过程媒体、GUI/系统键鼠、付费生成、额外崩溃采集、定时自动化、Git写入/发布、删除或真实存档操作。07:10:03UTC手动媒体检查7,427,750bytes/86files，Notify=false，无新20GiB档位；不建立提醒自动化。所有失败、计划及ledger保留。
