# v184 — 院落普通转移已过，后续夹击仍失败

上一目标轮次属有效进展：可见激光ECHO实际接通，模型/前后段同版严格PASS；完整双路和原十一项并未完成。本轮仍仅执行用户明确允许的隔离headless构建/测试，不开图形窗口、不发OS输入、不动真存档、不做额外采集/付费/发布。唯一工程G:/knight/Knight，误发视频不用。

## 事实、推理、风险

事实：当前未改驾驶器先跑原完整Explore，`thruster-route-exploration-diagnostic-20261101-021458-329-e72bf59d03e448ddbe60c3327ed290fc`，native242923ms、exit1、无超时/guard差异，严格FAIL。院落已在真正地面开始原普通跳，physics13846 resident `sealed-courtyard.01`/precision-laser/attack1击杀，HP1→0、L未按/盾未开、EnergyRatio1、heat0；原恢复随后使RouteMonitor检测到位置重置。与v179为同类实际击杀，不凭ECHO新卡就判旧路线已解决。

推理：此处应先让原移动阶段使用已存在的危险/盾机制，不能把满能量漏挡归为“牵引力不足”或无因传送。正式原遭遇战消费者到跨台之后才启用；resident在此之前已经能够攻击。

风险：键盘盾向与A/D共享，向左后方挡激光会反向/减速，不能预先保证原步行预算内完成。当前试验只增加实际L/方向输入；如果原预算/支持/一跳合同失败，就应如实保留，不能加等待、修改盾/敌人、补血或追加跳跃凑过。

## 正确的原合同

原路线为CourtyardApproach支持→Walk5220/100→WaitSupport(CourtyardFloor)/90→CourtyardStep普通转移→Walk5580/100→原遭遇战。普通转移自身包含可选approach Walk/90、真实Space以及落地循环300、到达后尾3帧；不是“整个转移共300帧”。原断言要求同一活玩家、Playing、恢复请求/完成不变，真实完整Step支持，恰好一普通跳和零新dash。HP原来只记录，不是已有HP不变断言；本轮额外要求转移HP保持，须标为新安全检查。

## 最小实现试验

只改现有 `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`。复用原 `SwapTempleEntryPreObserverGuard` 明确输入消费者，原被动observer不取得输入权，也不重新Begin输入驾驶器。只在院落到Step/原Walk区间启用；每个原PhysicsFrame边界重新从当前mounted Temple的真实Actors扫描 `Task10ObserveEncounterDanger`，按原MixedDefendWindow在近发射/Active时、且可举盾/未攻击时，用原键盘适配器发L/真实方向。释放L恢复原移动意图，finally恢复旧hook/退订盾事件。没有新增等待、J、Shift、Space或改变原Task4函数。

记录同次危险actor/attack/phase/remaining、输入序列和请求变化；这是节点处理前的观察，不冒充本tick节点已完成。ShieldResolved只统计该scope中的实际Guarded事件，不伪称每个事件都有独立攻击ID归属。危险叶每次扫描释放列表，不跨帧保留节点授权。

## 实际试验与原失败保留

| 案例 | build/native ms | 严格结果 |
|---|---|---|
| Explore 021458 / e72bf59d | 24564/242923 | 原漏挡激光击杀，恢复位置变化，FAIL |
| Explore 022116 / b4af730b | 24559/243216 | 新L实际挡1次，HP1/恢复0，但持续左行使原walk90差约21像素，FAIL |
| actions 023055 / 91347ae3 | 29218/767 | opt-in空接口真实STATIONARY_ROUTE_GUARD_RED，FAIL |
| actions 023248 / 016eb361 | 27396/8085 | opt-in实际水平转向/松方向/自然刹停，左右两向恢复/取消及原动作合同PASS |
| Explore 023518 / 4b2e124c | 30123/244747 | 院落scope局部通过；后续Defeat正式战斗被第二发后方laser击杀，整线FAIL |

前三种失败不是超时或native资源崩溃，分别是死亡恢复、未达步行目标、真实输入行为反例。原始日志/失败与所有更早版本保留。

### 水平原地防守

第二案的持续后退不是盾失效。只给测试输入适配器新增默认false的`stopForGuard`：先以真正的方向键让生产reader取得朝向；水平朝向匹配后松开A/D，继续真实L，让生产motor自然减速。原行走意图保留在现有`_directions`，松L才恢复；ReleaseAll可彻底取消，不保存/重放一份过期方向。垂直/斜向沿原实际WASD行为，不宣称全方向都能无方向键站定。

该选项只在院落scope启用，所有旧调用默认false。没有改玩家输入、盾、惯性或伤害生产代码；没有直接写角色位置/速度。`KeyboardActionRuntimeTests`在原保存Player/floor中分别验证左右两向，看到实际键盘采样、盾向、松A/D和真实站立刹停，随后确认松L恢复、ReleaseAll不重放。先加入无行为接口确实失败，再只实现driver行为得到GREEN；不是编译错误冒充RED。原J/K/Q仲裁、暂停J、L+Space不二段跳、持续L过热后的J仍全部执行通过。canonical只增加actions对driver文件的选定副本，不放宽任何验收规则。

### 第五案局部与全线的分界

完整原Explore实际走到该段：Step及Walk5580返回均HP1、恢复0/0，真实CourtyardStep朝上接触/完整胶囊支持，原跳数30→31、dash仍0；scope内236个原physics边界、25次L请求、1次Guarded事件。原approach90、landing300、tail3、Walk100不变。HP保持是新安全检查，原胶囊/单跳/恢复断言没有删除。

scope finally恢复旧hook、退订和松L后，才进入原`DefeatTask10Encounter`。之后physics14024，resident laser attack2造成HP1→0；当时真正danger winner是另一前方Squire Active2，盾朝+X，E0.9226664、heat0.084。因此转移段通过不等于整院落或整线通过，且新失败不是能量耗尽；位置变化是该次死亡恢复的结果，不是任意传送代码缺陷。

下一应处理正式战斗的前后夹击／走位或主动打断，不延长这段移动scope抢占另一战斗消费者，也不猜改敌人数值。本轮不新增该战斗策略。

### 后续只读核对：优先验证预警早段的合法K

独立源/日志核对（非新运行、非代码二审）：原第五Explore `engine.stdout.log:3986` 首次观察到resident laser attack2预警在physics13949，physics14024发射并致死，相隔75 physics ticks；13948/13949/14024边界玩家仍OnFloor。该observer只按attack记一次warning，不记录锁线字段或逐帧aim；75tick不是已证实的完整避线窗口，真实Space/K在此段也未获接受记录。预警记录玩家约(5573.998,1159.933)、laser(5153,1211)，点距约424.08、Left偏角约6.92°，落在原620射程/±22.5°锥内。但射线ClearWorldPath只检查World，爪的首撞还包括Enemy/ThreadTarget；同处生产Claw preview未记录，所以这不是“可抓中”的证明。

源合同表明两个敌人均有LightEnemy/真实Legacy adapter。后laser应用K会同步撤销攻击/beam/leaf授权，下一AI tick进入Stunned；前Squire则先被拉近，下一tick才关近战hitbox进入较短Stunned。因此下一候选优先针对后laser，不在两个Active已经相撞时才盲转方向，也不把抓前敌当作同步消险。

下一最小控制限定原院落batch0和900外层帧：前Squire有足够Recovery/预警余量、后laser处早段预警时，真实松L→A取得Left→松方向；同一次生产preview必须精确选中该laser、LOS/输入序列/pose新鲜，才发一个K。要求同cast/target、adapter应用+1、Latched→Released、真实Stunned与beam/leaf撤权，随后真实转+X防前敌，并保留全源危险/HP1/零恢复。误选/遮挡/不足则拒绝，不强锁、不加等待、不改变角优先/数值/地图。这个控制尚未实现或运行；K旅行时间、转向惯性和回防窗口都不能仅凭旧坐标授予安全。局部成功也不等于完整院落或Explore通过。

## 审查与证据边界

一次最终只读审查C0/I0/M1，无C/I修复、无二审。M1延期：取消反例目前在松L/确认恢复后才ReleaseAll，未直接覆盖仍激活stopForGuard时清键；实现清理已核对，但未声称该动态路径有反例证据。

前5案全部真实headless、无fixed-FPS/movie、无超时；build cap180000、Explore cap600000、actions cap55000。所有source1870/helper9/user109/continuation/bin6清单前后相同，选定Explore283源或actions219源的before/after实体及5helper/6bin逐SHA相同，原10个owned进程身份在核对时均不存在。source清单1870不等于归档了1870实体；当前变化也没有要求等于旧案。

## 最终同源回归与保管

| 最终案例 | build/native ms | 严格结果 |
|---|---|---|
| actions 024641 / a9f16d1b | 27862/8101 | PASS，原动作＋两向原地盾/恢复/取消实际执行 |
| models 024723 / e82c0acc | 32122/391 | PASS，仅机械爪模型子集 |
| pre 024801 / 2eee5628 | 29530/160149 | PASS，原180000ms，无ERROR/WARNING/FATAL |
| post 025117 / 5abc1811 | 24620/195522 | PASS，原300000ms，不是旧180秒资格或性能认证 |

post原75 enter/75 return、index0–74逐项同名配对、owner8 nodes1→1均核实。仅原4限定WARNING（seamless1、room-profile1、flow2），不声称运行零警告，无ERROR/FATAL。本次pre/post安静通过不关闭历史Finalizer/Handle根因。

全部9案：4严格FAIL、5严格PASS，9/9构建成功，所有引擎实际headless、无fixed-FPS/movie/超时。45对manifest、4458个源实体、94helper、54bin逐SHA匹配。source1870是清单覆盖，不是归档了1870实体；不同阶段选定实体数单独核算。

当前1870源/6binary/最终post10helper全部与最终案相同，最终actions/models/pre/post也与第三Explore同源同bin。当前相对本轮起点的源清单差异仅3个测试CS，无production CS差异；canonical文本仅新增actions纳入driver选定副本的一行。

当前SHA：主路线测试`56B74047367B61D57B0BE2C985278F054D00CD64E37C010D2818837751364EF7`；动作测试`19B969B25C2EEAEA3FC14A2E3E370E02C8FA133F84CBF6972F31BFB529D75616`；输入适配器`06D238F337893EBB819E237DD01D1EA8716AD3D4E1405EED920237EBC9477F7F`；canonical`B13EDE3A2FAC2CEA6B13B1CAE0B10D1AAE13F05140BB6507BC0BB8D7A93A304A`。

18个旧owned身份全部结束：17 PID不存在，旧PID41376已被不同路径/较晚创建的XboxGameBarWidgets.exe复用，未按旧PID误杀。HEAD`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未动。

工作内手动媒体检查2026-10-31T18:57:25.8023429Z为7427750bytes/86files/Notify=false，基线3508未重设；无新媒体、自动化、付费、窗口/OS、采集、Git写入、删除或真实存档操作。

所有改动仅测试/驾驶/校验工具，无本轮正式美术或生产数值增量。完整Explore本轮仍严格FAIL；原完整十一项、完整Main/Explore、历史资源根因和正式视听/硬件/真人继续ACTIVE/OPEN。下一推进正式院落夹击：先核对两个真实Active几何与盾向冲突，再验证原输入预算内的地面起跳避线或合法K打断；不能只把danger winner换成另一侧就假定双向都能挡。

## 独立Main问题（未改）

只读复核v176 Main：最后J在E约24.27时消费12，.55秒回能延迟还剩.10秒又举L；随后32个盾采样耗尽、heat约58.92未过热，最终resident Burst击杀。当前后续K/短喷/脱压hop要求owned Burst先被J降HP1/击杀，但该原案owned Burst始终HP2，分支实际从未启动，不能说这些措施用了仍失败。

下一Main候选应在真实目标预览/LOS/全源窗口成立后验证前置合法K，而非再次猜一个储备常数。v142的36E延期等策略已实际失败撤回；此轮不重做。Main本轮未改也未重跑，这项仍OPEN。
