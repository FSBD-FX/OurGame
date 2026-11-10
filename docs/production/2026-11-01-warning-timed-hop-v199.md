# v199 · 按预警择时的真实单跳，完整主线仍未通过

## 事实

仅继续 **G:/knight/Knight**。用户 U-20261101-01 允许保留真实同阶段零伤格挡与左转，再按枪手预警择时单跳。原十一项目标仍 ACTIVE；不调整生产玩法、敌人数值、物理、地图、美术、键位、存档或测试预算。本次只改两个现有测试 CS 与文档，不是游戏美术交付。

### 实际单跳证据

第一次新 Main53270508：真实接触6714、左转6715，当前receipt6723正常过期，未被历史事实续期。相邻真实弹体位置得到6739提示，6740发一次Space，6741真实接受跳跃。三发13/14/15于6756/6767/6775越过**完整当前胶囊**，6780落地、6781稳定并自然回能至原24门。HP始终2，rawSpace1、acceptedJump+1、无额外冲刺/爪/攻击，3passed/0retired-unpassed/0fault。不是“未受伤所以推测安全”。

该 Main 后续仍死亡恢复，严格FAIL。已飞离的13/15仍多次赢得危险选择，旧13最终驻留端点(5052.487,3249.874)、Travelled559.99994，自动举盾耗至0；后续Rust7/6888及新弹16/6909造成死亡。新弹16绝不因原三发记账而被忽略。

### 精确旧弹判断修正后的 Main

测试驾驶仅在Hop5、原玩家/HP/无恢复及三弹记账保留时，提供发射时捕获的object/leaf/ordinal/aim/radius。每次原危险扫描重新验证当前完整变换胶囊；玩家若移动到旧弹前方，历史passed不能继续豁免。仅固定方向Burst、精确原三叶可申请排除，其他来源、警告、未知、新弹及原Active gap保留。被排除叶仍进入被动候选日志，独立 `DepartureExcluded`，不冒用Rust的 `OrientationConsumed`。外层与fresh-J均消费当前胶囊。

最终 Main116f87a8 再次完成一跳三弹/HP2/自然回能/落地5。6782实际发W准备，6783原生产预览新鲜，但选中合法地形AscentGrip2，未选Moth，因此没有发K，按原一次尝试进入stage3。不是乱抓或跳过K验收。该时刻四只Moth均不在Up的π/8半锥与620范围组合内；换方向的LOS/可抓性未证，不称排序缺陷。

**完整主线仍严格FAIL**：原900战斗循环未清encounter，HP2、恢复0/0。末selected4900171813990是固定resident precision-burst，HP2；不是最后的owned波次单位。真正owned Burst16032827706685已6673死亡，group.live1剩余单位末态身份未记录，不能混同。`.223`是归一化22.3/100，不是能量0；单次 `afford_attack=True`，但两次AttackCost24及完整反击时间窗未同时满足。早期“ownedBurstHP2/能量耗尽”的简写为误读，此处及ledger已纠正。

## 当前同版验证

全部canonical、SourceRows1872、原build180s；运行时冻结source/helpers，无ExpectedFailure、fixedFPS、movie、预算扩容或错误屏蔽。原失败均保留在 `G:/knight/验证证据/october13-stable-claw/`。

| 案（20261101后的时间/尾码） | build ms | native ms | 结果 |
|---|---:|---:|---|
| policy123032 /32535b6d |31665|1095|历史完成事实stub真实行为RED |
| policy123319 /8a903c8e |27180|983|实际来弹cue stub真实行为RED |
| policy123646 /83703f8a |29657|3744|17完成+19cue+5trigger及旧控制，严格PASS |
| Main123913 /53270508 |25896|133804|一跳三发通过，后续死亡恢复严格FAIL |
| policy125946 /838dfe82 |29070|1009|当前胶囊旧弹策略stub行为RED |
| policy130208 /3e2d3a94 |29684|2916|真实保存叶扫描未消费策略行为RED |
| policy130420 /3cdcbce8 |29413|3841|24标量边界及真实叶扫描严格PASS |
| Main130524 /116f87a8 |33469|135108|原900未清，HP2/恢复0/0，严格FAIL |
| actions131538 /0451ab38 |29672|9520|原动作阶段严格PASS |
| models131626 /ef22ac30 |31863|537|机械爪模型子集严格PASS |
| pre131706 /ae14948a |23939|166232|原180000预算严格PASS |
| post132023 /be2c1dab |24194|203921|原300000预算严格PASS |
| Explore132418 /3e2a7c5f |30099|324716|原600000预算严格PASS |

共 **7限定PASS、4真实RED、2完整Main FAIL**，不是全绿。所有build均0警告/错误。post原10suites/75同名有序enter→return及owner8重复；仅4个限定负控WARNING（故意observer异常、非Node2D fallback、两次拒绝非法状态转换），其他严格门不降低。

真实保存叶扫描控制注入虚拟查询胶囊及已完成passed历史，只证明接线/边界，不替代实际Main通过。旧current epoch/receipt、12立即跳trigger、8真实结果判定控制均保留。机械爪模型子集不称全部游戏模型。

Explore从标题起、同一个player47546631684，11挑战种类、Boss实际fatal attack99、单次死亡/完成、实体支撑出口；完成后D保持60帧身体和隔离存档SHA不变，恢复0/0。不证明OS写入次数。`operationCoverageComplete=false`、`humanValidated=false`、`fullGoalAccepted=false`，不能替代失败Main、正常渲染、手感或性能证书。

## 审查、裁定与风险

唯一fresh whole-change审查C0/I0/M1，无C/I修补或二审。

- M1延期：真实扫描尚未直接覆盖“旧叶被排除，而同owner新弹/警告或另一actor同时保留”的混合候选。标量有新ordinal/错ID拒绝，不夸大成组合覆盖；现源码逐叶实现未发现错误。
- 自检日志Minor延期：旧 `fresh_capsule_unchanged_null=true` 字面标记在本次fresh-J实际供给胶囊后陈旧，不能据它声称没读胶囊；接线与本报告为准。旧v198 await-pair命名及GuardedDamage setup等延期继续保留。
- 裁定1：过期current receipt之外保留真实完成历史，不续期当前凭证；代价仍须独立验证单跳真实结果。
- 裁定2：实际相邻来弹位移作有限择时，不依赖绝对帧号/作者速度；代价缺失或过晚必须失败，不补键。
- 裁定3：测试驾驶逐叶、逐当前几何处理旧弹，不改生产寿命；代价相对位置变化必须撤销排除，新弹不可跟随豁免。
- 裁定4：fresh-J故意增加当前胶囊，不声称保持旧null几何；代价需当前回归与明确日志资格。
- 裁定5：先真实Main再最终聚合；代价中间policy绿不能代替整路，所有失败保留。
- 裁定6：审查不独立验收Main，事实来自Root原始终态及独立保管；代价不取得独立真人或玩法认可。
- 裁定7：完整十一项、正式显示/美术/动作/UI/声音、性能硬件、历史资源/存档问题继续OPEN；本次不认证未改内容。
- 裁定8：冻结档案和canonical实测作完成记录，不额外重跑同一失败写标记，不Git提交/删workspace；代价不产生发布/合并资格。

端点驻留559.99994已真实观测；float32在大坐标可能使剩余位移加回后为零，与代码数值吻合，但内部路径尚未native复现，不能称根因关闭。后续独立复用 `RangedWeaponProjectileRuntimeTests`，以真实Launch/owner/ledger、实际physics调度、大小坐标对照验证，不在本轮偷偷加阈值。

## 保管与下一步

13案65清单对、6510选中source副本、146helper副本、78bin逐SHA一致；26owned按创建/路径身份均终结且PID不存在。05:33:14UTC最终当前1872source/Explore同consumer9helpers/6bin匹配，consumer helpers差异保留，不强求跨阶段相同。

Main SHA `2E40768405FE606D722BF2E4AC5328E991264C3D77378D3CD79359337231E46F`；Placement `D95200A5A6D31CC8E5C4142EB60D738E361E30966062224A44C93634DDCFB665`；sourceManifest `BFCCEC35E616F753504CCDF92D039ED097FE0F7E420BE79ED3F1AC1DAD4E7E4F`；DLL `FFC3B240E823556340D6D94C98CFDAA6DD781AE0BF3CE17C36BB1D85F864D7E7`。MotorCB65/GeometryA359/canonical7A79/HEAD e7c70498/index21610不变。

下一步在原postHop首次换目标及899终态，补有界被动“选中者是否owned/剩余owned ID类型HP位置/本次原danger与反击门”关联快照，不新增scan、输入、等待。若必需目标确有合法窗口而驾驶追逐optional resident，再调整目标策略；否则假说应被否证。不猜是W或能量单因。Up目标、生产端点寿命是独立事实；固定UI阅读轨道真实移动遮挡检验仍待推进。

无新媒体、GUI/系统键鼠、付费生成、额外崩溃采集、自动化、Git写入/发布、删除或真实C存档写入；撤销的三个视频未分析使用。05:32:32UTC手动媒体检查7,427,750bytes/86files，Notify=false，无新20GiB档位。保留计划、ledger、所有失败，原目标不标完成。
