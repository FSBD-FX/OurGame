# 蒸汽战斗与正常窗口复验 · v021

2026-10-19，Asia/Shanghai。完整用户1–11目标 ACTIVE，未完成；Task1–5已完成的专项不重做，Task6／Task7、正式内容和最终交付仍OPEN。本轮没有制作或采用新的美术资产。

## 事实、推理、风险

事实：024347同步记录证实该次致死来自Bell的实际攻击Hitbox。随后仅迁移自动键盘的Steam Bell操作策略，024812已无伤通过Steam三批敌人、分流阀和八块回程平台，首错推进到SteamRejoinTop。用户提供本轮前台时段后，同冻正常Windows Main严格通关；正常Explore却在更早的蒸汽沟下落中死亡，尚未进入Steam战斗。两次死亡不能混报来源。

推理：正常窗口下落与后台下落的喷口时序不同，旧Walk4040只保证到达目标后松键，不保证停稳。后台真实松键时vx260，之后落地x4085.6206，证实余速会继续带入右侧喷口区域；应先验证并迁移这处操作消费者，不能据后台碰巧安全关闭正常死亡。

风险：正常Explore仍未通关；当前最新诊断源也不等于正常Main获绿源，不能拼成同版双路成功。Top的同高重叠实体还有独立支持身份问题。没有真人手感／完整操作覆盖／显示稳定60FPS认证；新增常驻正式仍0/50、24canonical未实现，全部地图／最终视听／存档组合／五历史严格失败／音频2 ObjectDB／ZIP／GitHub继续OPEN。

## 六个原生案例

证据根为`E:/OurGame/Knight/.evidence/october13-stable-claw`。每案均fresh rebuild退出0、0警告0错误，source1695（含296 imported，不是1695个C#），无timeout，guards=[]。不使用ExpectedFailure或日志豁免。

| 目录 | build ms | engine退出／ms | 实际结果 |
| --- | ---: | --- | --- |
| thruster-route-exploration-diagnostic-20261019-024347-765-df76a495591d4eadbd9d8ce3909de11f | 27623 | 1／95577 | Bell致死来源诊断闭环；整Explore仍FAIL |
| thruster-route-exploration-diagnostic-20261019-024812-224-4caf66a4da194190a21ac748fb37e7de | 26096 | 1／119392 | Steam三批／分流／Rejoin1–8通过，Top支持失败 |
| ranged-route-main-20261019-025542-773-608b4798146f48b592213410d61d03af | 28391 | 0／199684 | 正常Windows Main／真实Boss／支撑出口／held D60通过 |
| ranged-route-exploration-20261019-025940-999-0d61ab70ab544889b10b1275a062619c | 27164 | 1／90039 | SteamTrenchFloor等待中HP0／恢复请求1，尚未进入Steam战斗 |
| thruster-route-exploration-diagnostic-20261019-030517-635-b534f9075be7491aa094a843d63fda87 | 28323 | 1／119303 | 下落无伤但诊断自身新增29个API ERROR；后段Top仍失败 |
| thruster-route-exploration-diagnostic-20261019-030905-799-fa6ab944b8ed4c2da8b3c76ed32943d0 | 29612 | 1／119554 | 29个API ERROR消失；下落无伤／三批及八台通过，唯一Top失败 |

## Bell来源与本地操作迁移

024347实际绑定Bell `554956754807`／攻击Hitbox `555074195293`，原Player Hurtbox `47009760802`。physics5614先HealthDamaged HP0，完整14行、2861字符未截断堆栈为Health→GuardedReceiver→Hurtbox→Hitbox.TryConnect／OnAreaEntered；同physics后继绑定Hitbox的Connected实际目标为原Player。配对oldJet在全部24行均Disabled／Hazard=false／Monitoring=false，因此可指认本案Bell，不能只凭旁边有怪猜测。

真实旧操作在Guard／Armor期继续J，且后续能量约.304时CanBeginGuard已true，generic旧.35阈值仍不举L。Armor可以累计余数，因此这是低效的额外耗能，不是证明伤害绝对为零。

最小迁移只把已验证Gear Bell策略应用到keyboard Steam的Bell actor：实际Motion/module neutral入站、原55..64攻击带内60站位、两fresh neutral样本、真实威胁优先L、严格原目标K准入、只在实际Exposed且ready时J。原900／75／2-8-45、HP、AI、碰撞、伤害、零恢复不变；Sentry／Missile策略不变。未修改生产Player／敌人／地形。

024812实际Bell有两次应用、攻击被盾挡、玩家damage0；Sentry原900内597完成K2/J2，真实投射物连接被格挡；Missile真实telegraph1／shot1／两次玩家连接、敌HP2→1→0。Steam整组10started／10connected、4thread casts、HP1→1；分流阀原cast10完成Idle，Left接管／Right关闭；原4170站位48/112完成，恢复0/0。最终030905保留上述实际组完成及八台支持前缀，仍不是全Explore通过。

## 正常Windows Main与Explore

025542本次重新核对Player `47596963338`／Boss `564536543750`。由真实Title／原spawn进入，深井实际4.633秒；attack70于physics11500开始，11504真实伤害／败亡，Died与arena各1。原188预算内166帧到11683，在完整Boss地板支撑和门右侧完成；再held D60至11744，位置／body／velocity及存档字节不变，completion事件和计数各1、恢复0/0。

本次artifact为`storage/task10-route-result.json`，4350字节，SHA256 `8C768DBA89C2A6FC828889AF96AEFBEE9DEDAA7A29F5D0A33DF43D54A466055B`；完成及held保存SHA均`E19C58B0044F8A21A4C3304B387B855AB2C2A4308BED062F702867E086F37D2E`。46条远程事件逐字段匹配，Scatter／Burst／Laser各真实击败；资格为自动窗口Main完成，artifact的human／operationCoverageComplete／fullGoalAccepted仍false。

025940同源同六binary正常Explore：Steam下降131/170落Walkway，157/170停x2762.1328/v0，Valve100+1实际Up／cast5／原oldJet关闭、HP1不变。ShortcutStep4116支持通过后，D于physics5157实际按下、5174实际松开；WaitSteamTrenchFloor时5179记录同步DEATH RESET／HP0／(4058.3264,4134.583)，LastFallDamage0、applied未采样，恢复请求1／完成0。随后另一观测端点已移回营地，不能把断言时completion0说成后来永不恢复。没有当前致命DamageRequest来源闭环，不能借024347的Bell来源；此时远程实例0，Steam战斗未开始。

RightJet保存范围X4078..4142／Y4035..4185；上述Hurtbox横向已侵入约.3264px，且脚底尚未到Trench top4236。旧Valve关闭的是另一个Jet，分流器默认Right循环不要求HasActivated；几何和时序构成风险证据，但本正常案仍缺active／AreaEntered／fatal stack的唯一来源配对。

两窗口正常时间、Windows／1280×720／RTX3060 Laptop，非headless／movie／fixed-fps；Godot失焦0。OS sidecar各只ready/finally两点，前台PID22460均非own，未记录历史进程名，不能外推持续OS前台或归责操作者。用户前台时段已释放；非阻塞询问是否看到两轮，未答不当认可。

Main callback12778样本，p50/p95/p99/max14.196／25.381／35.732／342.650ms；Explore4647样本，为16.561／27.330／40.359／56.781ms。这是含遥测的callback间隔，不是GPU／present或稳定60FPS证明。

## 下落诊断与记录器自身修复

只在原Walk4040的100与WaitFloor的90两操作外包有界只读observer：priority100实际完成步、三Jet绑定AreaEntered、HealthDamaged／一次fatal stack。没有改控制、增加等待、关闭机关、替换receiver／filter或读真实用户存档语义。

030517错误地在Monitoring=false时调用GetOverlappingAreas，引擎报29个ERROR而不抛C#，所以faults0不能证明quiet，关闭行的False也不是可靠无重叠证据。终态后只修观察API条件：Monitoring=true才查询，关闭时写unavailable_monitoring_off。030905完整日志实际确认29个ERROR全部消失，不是命名豁免；仍保留唯一Top行为FAIL。

修正前后相同12条位置／速度／输入前缀：entry5172为3969.3013/v84.2832；Walk返回5190为4041.71、vx260／vy308.3334，原始D已松但采样仍上一Move=1；随后fresh neutral仍vx246.49663；5201真实落TrenchFloor在4085.6206/v188.79874。HP1、damage0、postMotor30、12/64无省略。该次Right入区时为safe，不替代正常窗口的死亡归因。

Top保存矩形[4940,3600,180,40]，完全包含Step8的[4970,3600,150,40]，不是再高一级。R20完整支撑要求中心4960..5100；失败4954.695在左界外5.305px。原helper只近中心松键并要求departed和Top这个实体的接触，尚缺精确entry／松键／首落地contact边界，不能把相同Y或Step8当Top支持来凑通过。

## 保管链与后续

六案五日志行数依次为7/0/533/41/574、7/0/621/37/658、7/0/2356/0/2356、7/0/509/41/550、7/0/634/528/1162、7/0/634/37/671；均完整审计，external逐行合流关系核对。030517的30个ERROR为29诊断API＋1Top，不把external副本重复算60；其他失败各只有对应断言，绿色无独立诊断。没有其他Handle／ObjectDB／FATAL／WARNING；历史故障未跑到不关闭。

各案source/helper/user109/binary6四对与continuation字节不变，实际选中副本后台212／正常226两侧及六binary均匹配。正常保护109精确符合旧keyboard基线、四byte pin与SDK实际绑定。

| 冻结 | source清单SHA256 | binary清单SHA256 |
| --- | --- | --- |
| 024347诊断 | 51BD65CF88F798C09A7404D8C8F12DA8A17CFDA394131BB65E3F8D2805996B29 | 4537C3ACCCEC64AD82F85F49DF06430F506D70673B8706DCDC1AF21A46E55DE0 |
| 024812＋正常双路 | 34E60B42B03F90DD40648F27D41913AB25F1BBAC81198587DEE5EE7B29625B4D | A3E4703455C80D636AE54CFB2EFD7EA3544C285A44A70B4F8C57AA55F74DC845 |
| 030517旧观察API | F8993BD0271871FDD89EF060CCDD2D52B6B7633752DCF8A80AD2395DCA6CD0FB | 7AA08D860C050B204E550479D2963482E047EDF9BEE3F2E80E2CB0E9543521A0 |
| 030905当前诊断 | FACE7719826E673013E38F5C3BEC8D5BB1331570B50AAD1B37C4D3A76AC228EF | 375670940BF63436E89A32E9D4CD087DE7E9F0DCD91AA30669CE0EA52B3428B4 |

当前Knight.dll `37BFEC7268FE2F718D2E4F155D328324358628B40701E12645564D236EEDC933`，PDB `005769F78F93D019C90FB313C0A706654A8BB283C2E3F725E6F402D48C588959`；正式Player仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。后两冻结仅测试诊断变化，但未借较早Main绿色认证当前全源。

下一先以真实胶囊／RightJet固定危险足迹建立下落安全反例，再用实际Motion/module迁移原100/90内的键盘控制，禁止增加等待／改喷口钟／伤害／关闭碰撞／重试。然后补Top三个边界诊断，保原300和真实named支持；两处各有证据后，另择时段重验当前同冻正常Main／Explore。全量旧消费者、五失败／音频、存档和最终制作／交付按总账继续，不只追容易通过的局部。

全部owned消费者已终态，CIM无owned Godot/dotnet/MSBuild/VBCS后才更新文档。E约5.85GiB、F28.09GiB、G356.09GiB；未来新增证据存G仍未获答复，未改根目录或移删旧件。无新媒体、付费生成、Git写入、ZIP、发布或自动化；没有可冒称完整交付的证明视频。
