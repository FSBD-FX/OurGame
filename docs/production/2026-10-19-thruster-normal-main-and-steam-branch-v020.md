# 正常主线通关与蒸汽支路适配 · v020

2026-10-19，Asia/Shanghai。完整用户1–11目标 ACTIVE，未缩减、未完成。Task1–5专项状态不重做；Task6音频／最终视听、Task7全量消费者／正常双路和完整七任务复审继续OPEN。本轮只迁移自动验收操作及诊断，不是正式敌群、美术或地图制作完成。

## 事实、推理、风险

事实：用户提供了新的前台时段；同冻结版本的正常Windows Main严格通过，真实Title／同Player到Boss击败、有支撑出口、一次通关结算及held D60保存不变。正常Explore仍失败。随后释放前台，后台修正下降护栏与旧返站刹车：当前版蒸汽入口169/170、阀门100+1/20真实通过，但后继steam-valves接战HP归零并请求死亡恢复，整Explore仍严格FAIL。

推理：下降中真实平台接触使身体可能暂处保守安全带外；带内“不能越界”的制动护栏不能拒绝带外向内纠偏。返站旧2800公式也不能代表实际4000反向制动。应迁移操作消费者，保留真实碰撞、原时间和零恢复合同，不通过改地形、补血、关闭敌人或自动落错重试凑通关。

风险：正常Main成功属于下表B冻结版本；最后两次源修改后的D版本Main尚未重跑，不能把不同冻结的局部成功拼成同版双路通过。窗口资格不是真人手感、持续OS前台或60FPS认证。正式新常驻仍0/50、24canonical及完整地图／最终视听／UI／存档组合／ZIP／GitHub交付未完成，历史音频2 ObjectDB和五项严格失败不因本轮安静日志关闭。

## 七个原生案例

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。全部fresh build退出0、0警告／0错误；无timeout，guards=[]。失败原件保留，未使用ExpectedFailure或诊断豁免。

| 案例目录 | build ms | engine退出／ms | 结果 |
| --- | ---: | --- | --- |
| thruster-route-exploration-diagnostic-20261019-020300-806-ab80c895c3f347c897e0c331bb137a66 | 25066 | 1／82647 | 原操作真实contact诊断；松键余速经过活动GearPiston顶面，落回PistonOne，170耗尽 |
| thruster-route-exploration-diagnostic-20261019-021255-891-6cab1da195254b52930ad21f794f6ba4 | 24501 | 1／81607 | 加实际反刹后113/170在带外进入状态被peak护栏误拒；不是170超时 |
| ranged-route-main-20261019-021524-784-7d70b27824e143969273c6287e26c513 | 31868 | 0／199703 | 正常Windows Main完整路径／Boss／有支撑出口通过，非human／performance |
| ranged-route-exploration-20261019-021945-450-eacce1a4d07743a19acf5e70414504b2 | 28215 | 1／83514 | 同冻正常Explore在115/170、x2679.743/v12.500001的带外纠偏处误拒；未到Valve |
| thruster-route-exploration-diagnostic-20261019-022334-262-05af23ed46a84512b2cbfb076162bde3 | 27646 | 1／82544 | 下降143/170已full walkway；后继旧返站在170时vx50，尚未停稳 |
| thruster-route-exploration-diagnostic-20261019-022650-750-f49567fbefd84e4b82ed441029d61257 | 33268 | 1／97012 | 入口169/170与Valve真实通过；后继接战HP0、恢复请求1/0，整路FAIL |
| thruster-free-forecast-20261019-022941-494-4d308d6cf4764fa19646b07b43150a34 | 27702 | 0／695 | 同D源独立几何和／地面离散和／Reverse跨零与peak-net区别／旧2800负控通过 |

## 正常Main成功边界

同Player `47596963338`，Boss `563009818483`；attack70在physics11500发出，11504伤害／败亡，Boss Died与arena事件各1。physics11683在`(7340.7603,1079.9941)`实体出口右侧、完整Boss floor支撑上完成；held D60到11744，位置／身体／速度及完成存档hash保持，completion事件和计数各1，恢复0/0。

三个真实远程角色完成，46条native事件逐条与receipt对应。资格原件是`AUTOMATED_REALTIME_KEYBOARD_MAIN_COMPLETE_NOT_HUMAN`；现有资格不含持续OS前台证明，也不覆盖所有操作。artifact明确`operationCoverageComplete=false`／`humanValidated=false`／`fullGoalAccepted=false`，不能把本案当短喷全操作或整个序章交付。

`storage/task10-route-result.json`4350字节，SHA256 `C1BCAF698653562B66ECC900E7533CAEAEE80960FB9049921DA9F8891B63DDB5`。完成与held后的隔离save SHA均`4058DFBF6551BF3A80F16DD518EECDC25E3600D25048D9FD2F0477204D402566`；未声称OS写入次数，也未读取真实用户存档语义。

## 蒸汽入口：实际RED、适用域与后继返站

原操作的020300记录：松A后的真实raw/sample已neutral，横速仍−253.1583；obs73实际活动GearPiston顶面接触，obs80摩擦停2640.061，obs128落PistonOne直到170。HP1不变。仅加诊断不是修复。

B版本下降使用实际Motion/module有限剩余neutral端点和Reverse原方向ForwardDistance，以真实A/D制动；原gap／170／HP／无跳喷爪不变。021255实际记录27条Lift侧接触，最终113/170、x2685.441/v6.666667触发计算peak护栏。正常021945则是GearPiston右侧12条接触、没有Lift侧接触；115/170已有contacts[]、x2679.743/v12.500001。不能把两案接触链混报，也不能将计算peak当额外实测轨迹峰值。

C版本修正：仅在当前身体已处于保守安全带内时检查逆刹峰值不越界；带外仍通过真实A/D向内纠偏。两实际带外literal正例、两带内越界负例与一合法例保留。有限预算neutral预测改名`neutralEndpointX`，不称停点／跨carry准确轨迹。每完成物理tick禁止PistonOne任何上表面支撑，包括部分胶囊支撑，避免隐藏落错重试；原full walkway和170继续严格检查。

022334下降已143/170实际full walkway，但后继旧`v²/(2*GroundDeceleration)`返站在170时仍vx50。该后继错误不是下降护栏未修、也不是Valve错误。

D版本只迁移后继返站：actual ground Reverse的ForwardDistance决定D释放边界，先松D、再真实A逆刹；跨零即松A/D，之后仍必须fresh sampled-neutral且|vx/vy|≤2，不能把跨零当停稳。原共享170、x2760±8／完整支撑、原Valve100+20不变。

实际022650：143/170落台2676.7144/v−12.771707；164/170开始A逆刹，预测峰2760.005、Reverse4步；169/170真实2759.9438/v0完成。随后Valve100+1/20停3161.2698/v0，原Up准确选中，cast5执行Latched→Released→Retract→Idle，关闭原paired jet；max displacement0、HP1、跳喷计数和恢复均不变。这些是当前本案局部通过，不是完整Explore通过。

## 当前Explore首失败与下一诊断

022650随后到ShortcutStep4116，进入steam-valves batch0。frame201在`(3504.0386,4199.9336)`、shield held/active=false时Damaged remaining0；下一行同步pre-recovery为reason=`DEATH RESET`、HP0、v0、LastFallHeight120.00073、LastFallDamage0，DamageApplied未采样。physics5614恢复请求1／完成0，触发唯一zero-recovery合同ERROR；observer记录违规，不是死亡原因。

源码PlayerMotor唯一LastFallDamage赋值在当前ConsumeLanding的ReceiveDamage之前，且ReceiveDamage须landing.Damage>0；结合该同步0值，可排除本次Motor落地结算直接致死。不能据此指认Bell或某Hitbox为唯一伤害源；致命DamageRequest来源尚未记录。当前SteamSnapshot要求trackedSentry非空，漏掉了更早batch0 Bell阶段。

下一先在既有actor换绑／Health观察接缝补有界、只读的Steam Bell phase、Hitbox.Connected、shield／energy／输入序列和pairedJet状态记录。Connected发生在Health回调后且格挡也会产生，必须与Damaged配对；无来源连接时标unattributed，不能自动归Bell。不开产品专用测试钩子、不替换receiver／filter、不改HP、AI、碰撞或原零恢复合同。诊断后再决定是否迁移该generic站位／防守消费者，不预先猜修。

## 窗口、焦点与性能

用户明确提供新的前台时段；正常双路结束后已释放，其后全headless，无OS补键、自动抢焦点或再开窗口。是否实际看到了两次自动操作另行询问，未收到回复不视为认可或阻塞。

两案真实Windows/windowed1280×720、timeScale1／physics60，无headless／movie／fixed-fps。Godot均focus_loss0；OS sidecar各仅ready/finally两点，均foreground PID22460≠own。Main两点process/physics为0/4与12816/11754，Explore为0/4与4275/4816；无历史前台进程名，不推全程或用户责任。自动窗口路线资格与持续OS前台／真人见证分开。

Main callback12694个，p50/p95/p99/max＝14.320/25.918/36.289/307.505ms；Explore4153个，为17.152/27.880/41.731/53.671ms。均是带遥测的callback时长，不是GPU/present或60FPS认证。sidecar未发现独立observer/emission失败，不将有限tail／未武装pointer当全历史。

## 证据链与冻结身份

五日志（build stdout/stderr、engine stdout/stderr、external）全部完整核对：020300为7/0/478/39/517；021255为7/0/478/41/519；Main为7/0/2356/0/2356；正常Explore为7/0/492/41/533；022334为7/0/479/39/518；022650为7/0/492/41/533；forecast为7/0/4/0/4。失败各只有上述ERROR及同栈，无额外Handle／ObjectDB／FATAL；绿色无独立诊断。正常Explore external在stdout第291行后插入stderr，其余逐行关系已核，不将所有sink概称字节拼接。

全部source1695（含296 imported，不是1695个C#）、helper／user109／6 binaries及continuation前后保持；正常protected109精确匹配keyboard基线和四byte pins。路线headless各212 actual selected＝207源码+5helper，正常各226＝213+13；forecast166＝161+5，均核实际副本及清单，六binary实际副本匹配。正常两案helper17仅行序不同，排序后同内容，不称内容变化。

| 冻结 | source inventory SHA256 | binary inventory SHA256 |
| --- | --- | --- |
| A／020300 | E5FB62E74203D84BD180DACCA5D0407F112E2D86D2DE9266F1A99CBA8AE5DC63 | EF44A537C270DB29F8D47DB6451376D7BFD434460AF7EB7E81F8C0130498CB7D |
| B／021255及正常双路 | 2587B9DAC8026D005DD49C746BA59A09BCA5FF6AEB41029ED26FEEA130EDD420 | 4877991D53E3EAF650B0FF4F7BAC4865DBF97450EF63C96764BA2360214B6ADC |
| C／022334 | 4284D52ED42CCE9D6D22E7273E86C9E5FBE799E646DE90916FBA2D7E18BEF201 | F62C286730C1D84052802655E6619B7FB7729B7DBCC6D753BC8EEE6E4FF3E89E |
| D／022650与forecast | B5CD625F54118AB2CD458D2E295B334140DCEE4BF208CB3018C4A95A5EA5C834 | 294DF8E81A77726EE65B0F815DFB08FC8F2EA14EBCABD5EECE4AEC44F303BC77 |

D的Knight.dll为`26E1B2086C3C771D7DCA4B47DB2D27AC38EF47743ED8ACD5A8E74AF6DD12BD59`，PDB为`91155B464FB1CF85F7E9618F58DA8EB3AF33C0123D3EF0DF6D8A3A3C7E910470`；helper/user/continuation分别仍1BF1160E…／BDDA0FBE…／7EB70257…。正式Player SHA仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。

所有owned消费者已终态，CIM复查无owned Godot/dotnet/MSBuild/VBCS后才更新本文／README／总账。E剩约6.17GiB，F31.94GiB，G356.09GiB；未来新增证据存G的请求未答，未改路径／移删旧件。无新媒体、付费生成、Git写入／ZIP／自动化；没有可冒称最终证明的视频。本检查点不完成Task6、Task7或用户1–11。
