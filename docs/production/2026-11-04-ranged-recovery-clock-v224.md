# v224 · 恢复期的真实危险时钟与跳弹周期前提

状态：最后同源14阶段12PASS/2严格FAIL（Main、pre），并非完整关卡通过或美术验收。原全部十一项及R-I惯性/R-C动作特色/R-P物理手感继续ACTIVE/OPEN。本轮只改三个测试/验路C#文件，未改正式玩法、数值、地图、敌人、角色美术或输入规则。

## 事实、判断、风险

旧Main0e3db618在配重区899帧结束，Rust剩1HP。不能将它归因为盾卡住或过热：生产J花12，尾部仍16.13；混战驱动另外保留24并要求完整J .30s＋8帧。等冷却可用时共享窗口只有.2167，不能强行加一次J。清理前held/active与ReleaseAll之后shield=false是两个采样，不是状态矛盾。

旧观察器把ranged Recovery阶段终点当成新危险终点。实际模型每Step只处理当前阶段，Recovery→Idle→完整新Telegraph→Active；BeltBurst冻结T1.10，而不是Rust的.65。取消/休眠/reset可能丢弃剩余Recovery，所以不能信用r+T。本次只信用捕获T减相对1e-6容差、再向下1ULP；不加Recovery/Idle/样本时间，保留原完整J+8/24和所有真实在途弹体/Active-gap零窗口否决。

这是真实时钟消费修正，不是玩家获得新能力或新增物理表现。时钟只适用于当前正常挂载与fresh-read，不宣称actorID能防任意人工_Ready/跨代改配置。当前Telegraph仍取原剩余；其他敌人原规则保持。

## 已发生的失败与修补

证据根`G:/knight/验证证据/october13-stable-claw`；所有失败保留，无筛绿重跑。

- d1622332/b00fdeca：新夹具忘配置枪手arena且muzzle在viewport外，始终Idle，属于测试布置错误，不算预期行为RED。修正为显式隔离坐标/arena，正式场景未改。
- d25d8f27：真实保存Player+BeltBurst自然进入恢复，remaining=.33333334、T1.1、HP5/真实资源足够；旧安全时钟只有.3333，实际`RANGED_RECOVERY_CLOCK_RED`。模型正常/Cancel/Stun三退出及冻结设置控制先运行。
- bc99819d：修后真实J消费、命中、HP5、零新Active/发射、固定时序八帧检查通过，但新夹具漏掉driver.Dispose，后续原混战入口被单一输入所有权保护拒绝。修正测试finally后15f6e4ac严格PASS6569ms，包含原混战Rust格挡1/Burst三发格挡3。
- 第一批impact7阶段5PASS/2FAIL：混战资源/完整反击/早警告/紧边界/远程模型通过；Main4885475f145795ms与saved-actor8e6c2cf0失败。
- Main4885475f合法收尾ownedBurst提前到6574（旧6672），后续Hop在Rust返回前提未完成时就认领cycle5，再把ordinal13写入Hop账本；未发Space，子弹未通过即在owner Active时退休，原严格断言正确拒绝。**这次新现场HP基线是1，不是旧Main的2**；恢复0/0，不混抄历史摘要。
- saved-actor8e6c2cf0实际`STUNNED_REARM_RED`：新增括号误让已知Stunned也要求CanOwnAttack，但Stunned本来必须撤销攻击authority。恢复原已知无自有攻击的Stunned时钟，保持Idle/新Recovery的authority要求；不把旧原生测试改成接受0。ca7ed2f6严格PASS58506ms，原120000ms门未改。
- I2：保持stage1来源绑定，因为真实Rust左返租约依赖它；只把“认领新Telegraph周期”提前加上真实历史`Task10CompletedRustReturnValid`前提。未认领周期不借给Hop账本，但完整危险观察和防守仍保留。认领后原三发、通过/未通过退休、HP、原始跳数、Space与899时限一字不放宽。264f7524新前置门实际RED；a77ecf59严格PASS6450ms：20准入控制＋3模型退出＋真实J/命中/8帧＋旧混战全跑。仍须最终Main证明集成，不借纯模型代签。

## 唯一审查、裁决和延期

唯一fresh审查C0/I2/M2，无二审。两Important均进入一次修补；I1现存原生RED→GREEN；I2当前只有周期门专项通过，完整Main是否满足必须看末表。

开发取舍及代价：

1. 按用户自主推进授权直接选择现有Godot的有证据修正，不反复征求程序性选型；代价是开发选择不等于用户批准美术/手感。
2. 原dirty codex工程、三精确基线代替新worktree/commit/清理；代价是靠本地清单保管，不新增发布。
3. 捕获T而非阶段终点或r+T；代价是只保证当前正常挂载/fresh-read，不承诺任意人工重初始化。
4. 完整J+8、24能量、899、HP/恢复/爪角度优先/真实弹体否决都保留；代价是Main若仍失败必须报告，不能为绿灯让步。
5. 不接受“改原测试期望为0”的修法，保留已知无自有攻击Stunned的旧合同；代价是未来统一authority API仍需单独设计，不能把这视为通用失authority授时。
6. 完整运行/保管只按关闭案与最终当前绑定判断，不推断未结束批次；代价是没有全绿之前Task1不完成。
7. 人工Ready/remount跨代重配置不作泛化保证；代价是该类时钟代次覆盖尚欠。
8. 继续仅无窗口；代价是正常显示、美术、人手操控、硬件性能、R-I/R-C/R-P未验收。
9. 三个测试文件不能代表原十一项完成；代价是原全关/正式品质/资源根因继续OPEN。

延期Minor：

- 新夹具observer返回后leaves已清，26次veto只证明owner Active；真正Recovery旧弹体否决由原mixed-policy负控覆盖，新夹具没有独立分栏。
- 固定.30s攻击的等待循环没有在记录completed前立即断言结束；当前固定时序通过，但未来更长Tuning的“结束后完整八帧”证据还需加强，不能泛化。

## 最终当前证据（已终结，不等于全通过）

专项a77ecf59与root串行最终批次14482的另外13阶段均已终结，最后同源共14阶段12PASS/2FAIL。所有C#/helpers在整个批次结束前保持冻结。阶段专属原预算未改（默认55s、saved actor120s、pre180s/post300s/两路线600s），未用安静复跑覆盖失败。

| 阶段 | 案尾 | 实际native ms | 严格结果 |
|---|---|---:|---|
| 新时钟/周期20控＋原mixed-policy | a77ecf59 | 6450 | PASS |
| 完整Main计划 | 7ba87fa2 | 129197 | FAIL，真实死亡/恢复 |
| 混战资源 | 5d7f8266 | 16183 | PASS |
| 原混战反击 | f622a420 | 4115 | PASS |
| 原早警告反击 | 5c604f1d | 4244 | PASS |
| 原紧采样边界拒绝 | 97a114ac | 1935 | PASS |
| ranged-models | 0524242d | 541 | PASS |
| saved-actor | 2dd8d8a5 | 58393 | PASS，原120000门 |
| actions | 03eefe96 | 9415 | PASS |
| keyboard/独立title | af201fc5 | 20080/7715 | PASS，均非可见/真人 |
| models | 383f0953 | 431 | PASS |
| 原pre | 1243fa5f | 156181 | FAIL，exit0不消除3HandleERROR |
| 原post | a485774a | 209056 | PASS，原300000门/4精确限定负控警告 |
| 完整Explore计划 | 9ed9cb1a | 313941 | PASS，Boss/有支撑出口，非全质量/全操作 |

最后Main7ba87fa2已关闭：129197ms严格FAIL；这次不是旧899或提前Hop认领。Hop stage1/cycle0/shots0/issuedFalse，没有Space，尚未建立Rust返回。真实居民precision-burst第16发在physics6809使HP1→0，随后DEATH RESET，fall_height36/fall_damage0；恢复1/0，原无瞬移检查抓到从(4576.6646,3459.9336)回检查点(1680,3960)的2939.51位移。不得将它说成摔伤/陷阱或误报。

最终Explore9ed9cb1a为headless普通计划自动测试：build27410ms/exit0，native313941ms/exit0，在原600秒门内，无引擎/资源/泄漏诊断。实际产物为23路线节点、11挑战种类、11敌群激活/10击败（counterweight-lift不在击败表）、4机关、2方向捷径、2检查点；从标题的同一Player47546631684进入Boss28483669462797并到有支撑出口Completed，恢复0/0。Boss死亡/竞技场击败/完成各1次，末尾60持键帧位置与记录存档hash保持；109保护用户条目未变，音频drain3帧/44ms。`operationCoverageComplete/fullGoalAccepted/wholeGoalAccepted/humanValidated/normalDisplayAccepted/performanceCertification/OSOriginProved`仍false，`measured_OS_write_count=unavailable`，不是0写入或真人可见/全操作证明。

下一失败入口已有独立只读因果定位：6588普通冷却后退发A，6595松键后仍vx−256.667；6598完整胶囊左缘越过平台4600，6604才停。之后6634斜向防守的A+S覆盖规划移动中立，6644实际离台，6656落低台；6809持盾向Up却被左侧子弹致死。不是“规划移动False”就无横移，也不是IsOnFloor就完整支撑。下一任务必须实测完整停止包络、支撑面与斜向举盾输入，不能继续盲调J时机或放宽伤害/恢复/退休断言。当前不在本次审查一次修补之外追加第三套修法；原I2完整Main readiness仍OPEN。

没有新过程媒体、付费生成、系统输入、真实C存档写入、额外dump、运行时替换、自动化、Git写入/发布或删除。

前段1243fa5f已关闭：原44有序案例/16音频drain与最终phase OK完成，exit0/native156181ms在原180秒内，但3次真实`Handle is not initialized`令严格结果FAIL。三份相同栈均为GCHandle.FromIntPtr→SwapGCHandleForType1284→Node.AddChild848→BasinSupplyRuntimeTests.OpenSample525→ReturnToTitleDuringHealingStillPersistsTheClaim433；stderr1/42/83，external镜像135/176/217，不重复计成6次。没有通用放行、改phaseOK断言、重跑筛安静或新增dump；根因未证，不归因于本次时钟或宣称资源已修复。

后段a485774a完整75有序旧案例/19drain/8次PackedScene owner反复与节点1→1通过；4警告精确对应1 seam observer故意抛错、1非法room-profile、2 flow-registry拒绝，源文件/函数/范围/计数匹配。保留`original180Accepted=false/performanceCertification=false`，不能把209056ms称作原180秒通过。无额外资源错误或泄漏。

### 当前绑定与容量

根独立重哈当前1888源码/运行资源、14去重helper、6二进制，mismatch全0；不是重哈所有封存载荷。源码清单SHA256 `8E720F674905F1D917D1B5F9EDF5CF75841F6D1119FE807DBDAEEBD50E93EAA4`；二进制清单SHA256 `F07A5116EE17777CE84BB0B26A67F1160E56E582D46D47F9C8E6CF4051F9E33B`；实际Knight.dll `161087D3DD005F975608AA75F8A99558466187CF25687233182AD4D983303ECC`。

最终CIM本轮KNIGHT native/build/outer计数0。Git仍`codex/task6-certified-final-20260902T0555`，HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变。2026-11-03 17:18:43UTC（本地11月4日）媒体检查7,427,750bytes/86文件、Notify=false/NewThresholds=0，无新媒体；G剩129,369,219,072bytes，不做自动清理或提醒打断。

独立关闭案审计已核对最终14阶段70/70保存文本对、29/29唯一PID＋创建时间＋exe身份及14去重helper，保存清单hash与根当前独立绑定一致；保存文本审计本身不等于载荷或当前文件重哈。累计实际诊断恰为post4限定负控警告、pre3Handle错误及Main1瞬移断言，不筛除失败。Task1不写done，I1修复成立、I2周期提前认领修复不等于完整Main readiness关闭。下一为真实停止包络/平台支撑/斜向盾输入与前段句柄来源的独立复现；用户本次U-20261104-01重申惯性/特色动作/物理体验，原十一项/R-I/R-C/R-P/正式动作美术/全关/正常速度/硬件真人继续ACTIVE/OPEN。当前目标轮为PROGRESS，不将尚未完成等同blocked，也不标记全目标complete。
