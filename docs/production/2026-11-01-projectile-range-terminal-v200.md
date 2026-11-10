# v200 · 真实射程尾端驻留修复，完整主线仍FAIL

## 事实

项目仍是 **G:/knight/Knight**，原完整十一项目标ACTIVE。上一轮v199有真实单跳/三弹/HP/落地进展，但Main原900未清。本轮优先处理实际游戏中的弹体寿命故障，不再先调自动战斗策略。

只改两个现有CS：`scripts/enemies/ranged/RangedWeaponProjectile.cs` 与 `tests/integration/RangedWeaponProjectileRuntimeTests.cs`。没有改速度、射程、伤害、半径、敌人/地图、玩家键位/力学、机械爪排序、存档、正式美术或动作。全部构建与原生测试仅headless、隔离存储；没有窗口/系统输入/付费/额外崩溃采集/定时自动化/Git写入发布/删除。撤销的三个视频未使用。

### 原生复现，不是只凭代码猜测

同440速度、560最大射程、半径5、固定方向(.9276568,-.37343386)，实际引擎调度六例（Burst/Scatter/Missile × 两个原点）：

- 小坐标(0,0)：三类都正常560、退休一次；导弹爆炸一次。
- 关卡大坐标(4533,3459)：三类均停在(5052.487,3249.874)、实际Travelled559.99994，24个静止尾样本；仍processing/CanProcess、未暂停、Active/remaining0、未退休，导弹也未爆炸。
- 原样观察100个await pair，本案实际101次post-node样本、physics差100、累计1.6833秒；不把await pair普遍等同physics tick，不称独立World2D或真人操作。

首案是具名真实断言RED，不是编译或超时错误。第二案保留相同生产源，先增加九个末五像素接触控制（3类×角色命中/前置薄墙/同距墙优先），九例全过，再在同一大坐标终结断言RED。

### 修复方式与实测范围

生产端先用double计算该tick请求位移，并与开始时剩余射程比较；`rangeSpentThisTick` 只在原生Sweep、实际安全端点、Travelled/age推进、World/Hurtbox及ledger交付之后消费，复用原FinishFlight。没有增大epsilon，没有普遍按零位移退休，没有把Travelled写成560，也没改导弹年龄裁剪、导引、爆炸范围/LOS及回调撤权顺序。

最终六例全部退休一次，导弹爆炸一次，静止尾样本0；小坐标Travelled560、大坐标仍真实559.99994。九个末段接触控制及原有整个projectile phase均过。新测试只检查1.25秒前未退休与固定观察结束时终结，**不是精确退休tick及所有有限delta的证明**。

## 当前同版回归

所有案SourceRows1872，原build180s，source/helpers冻结至native终结；未加ExpectedFailure、fixedFPS/movie、额外恢复/补键或放宽门限。原始证据在 `G:/knight/验证证据/october13-stable-claw/`。

| 案（20261101后的时间/尾码） | build ms | native ms | 结论 |
|---|---:|---:|---|
| projectile134128 /40c46d99 |30087|11463|真实六例终结RED |
| projectile134446 /63839ba7 |29016|11713|九接触控制过，终结仍RED |
| projectile134610 /665fee0c |29375|11909|原projectile及新6+9控制严格PASS |
| Main134744 /02f9b9e0 |30177|148467|原900未清，严格FAIL |
| ranged-models135749 /bde0f581 |30528|522|严格PASS |
| native-geometry135828 /e772af10 |29796|3096|原16真实样本等完整phase，严格PASS |
| mixed-policy135908 /bccb5186 |30885|3841|原合同与v199控制，严格PASS |
| actions135950 /84bfd7f0 |25597|9481|动作phase严格PASS |
| models140032 /b461bd3f |25982|502|机械爪模型子集严格PASS |
| pre140105 /e068c0e3 |26438|177763|原180000预算严格PASS，仅余2237ms |
| post140436 /bf689700 |26140|209009|原300000预算严格PASS |
| Explore140839 /eb53ddb3 |27632|324622|原600000预算严格PASS |

共 **9限定PASS、2真实RED、1完整Main FAIL**，不是全绿。所有build0warning/error，除两RED与Main各1原始断言ERROR外没有新增错误/FATAL。post原10suites/75同名有序enter→return、owner重复8次/nodes1→1，只有原四限定负控WARNING；没有屏蔽异常或扩大本轮预算。pre仅剩2.237秒，不能宣称稳定裕量足够，更不作性能资格。

Explore同一player47546631684从title出发、11挑战类别、真实Boss fatal99、一次死亡/完成；physics18140实体支撑越出口，完成后D60帧，身体、计数和隔离端点存档SHA不变，恢复0/0。不证明OS写入次数。operationCoverageComplete/humanValidated/fullGoalAccepted以及normal-display/performance仍false。

## Main事实与下一步

Main驾驶文件保持v199的2E407684，真实Hop5、唯一Space、三发passed、自然回能/落地/HP2保留；W一次预览选地形，未发后续K。原900未清，HP2/恢复0/0，没有为通过跳过断言或重新补跳。

生产修复后该案旧三弹已真实3退休/3端点、记录无drop/fault并在末端关闭；但journal **cycle_retained=false、complete=false**，不能称完整同周期资格通过。局部尾端寿命修复不等于这项诊断合同或整路通过。

通过现有raw及所有权门可继续定位，无需只为确认身份重跑Main：

- stdout1187绑定wave Rust16034891303523；1671的非default epoch仍记录该actor/physics7058/fault0。
- `MixedGuardPublication`更新前通过 `MixedGuardRetained`，包括同对象、group.OwnsLiveActor、Health>0；否则清epoch。末段live1与这个活绑定对象一致。
- 另一个wave Burst16035327511320已6672 Defeated/HP0；末selected4897370018863是固定resident，不是该必需存活Rust。
- Rust的精确末HP没有同ID的直接快照；共享last_damaged_health2不能冒认。已证HP>0，HP2仅为事件重建候选。玩家receipt中的Health2也不是敌人HP。

下一轮在已有原决策边界补最少的必需Rust位置/HP与原counter门关联，必要时调整自动试玩的必需目标选择；不先改变敌人难度、能量或爪角度排序，不增扫描/等待池，不只为已有身份证据再跑整线。正式UI阅读轨道移动遮挡、正式美术/动作/声音/真人与全部十一项仍未完成。

## 独立审查、延期和裁定

唯一fresh全变更审查 **C0/I0/M2**，无C/I修补或二审。

- M1延期：`fake_travel=false` 的自动测试声明强于559.9..560.001数值oracle，它也会容忍伪写560。本次源码与真实559.99994日志证实没有伪写，但不把该测试当普遍证明；后续可累计实际位移作独立oracle。
- M2延期：未直接锁住“double比较vs先float舍入”的边界，也未覆盖不同轴ULP和精确退休tick。当前固定440/560六例及1.25早退检查只证明本例尾驻留，不泛化。
- 旧v199混合危险组合覆盖/陈旧fresh-capsule日志标记及更早延期保留，本轮不顺手修改。

裁定与代价：

1. 先修真实生产尾驻留，不先改驾驶策略；代价是需要三个角色与接触回归，Main仍可独立失败。
2. 保留现有codex分支和冻结档案，不新建worktree/提交/删除；代价是不产生发布或合并资格。
3. 改动范围是能正常前进后产生的射程尾量化，不普遍解决首tick就小于世界ULP的运动；remaining仍基于实际位移。代价是极端坐标/微步仍需单独定义支持范围或累计预算语义，不能错误地遇零位移就退休。
4. 非均匀/移动父节点及全部精度组合未native认证；TopLevel原流程不变不能代替实测。代价是不作跨全部配置物理保证。
5. Main/Explore和聚合的资格由原始终态及独立保管给出，不由源码审查猜测；代价是当前Main失败必须继续保留。
6. 正常视觉、真人、全十一项、历史资源/存档根因和脏基线其他改动均不被本补丁认证；代价是不宣称完整交付、任意资源问题已修或十万付费玩家保证。
7. 保留实际canonical执行与档案，不为写完成标记再跑相同失败，也不Git发布/删workspace；代价是仅内部修复记录，正式版本仍待原全部门。

## 最终保管

12案60清单对、5934选中source副本、136helper副本、72bin逐SHA匹配；24owned按创建时间/路径身份均终结，PID均不存在。06:16:51UTC当前1872source/Explore同consumer9helpers/6bin匹配，不强求不同consumer的helper清单一致。

Projectile SHA `235BF55E1A644C6ABF7C006EDB1A05E361CA19FAE864C5C981B4335713FB09B6`；test `2ACFF58AB61794C336E9DF90143745C49A53EA9AA113C86C5166DFBC83BEDF47`；sourceManifest `EE071722E85F29B57CD3C63F0B74E7E6E5278A8E89CF2F1E33ABC2B4118E7F04`；DLL `84BD4FCE8AB7C048D0200CDAA0DB1BBECA686104B0A3619A84ED971D2CE3DC7C`。Main2E407/PlacementD952/ActionsEE5/MotorCB65/GeometryA359/canonical7A79及HEAD e7c70498/index21610/原codex分支不变。

无新增过程媒体或正式美术，06:16:28UTC手动媒体检查7,427,750bytes/86files，Notify=false，无新20GiB档位；没有建立定时通知。原工程、真实C存档与所有失败证据保留，目标继续ACTIVE。
