# v157 · 第二次攻击实测与完整路线的两个失败点

工程仍是 `G:/knight/Knight` 的 KNIGHT Godot.NET。完整原十一项目标 ACTIVE；本轮是测试可观测性和真实路线证据推进，**没有新增美术、生产玩法或完成整关交付**。原始失败不覆盖，难度、敌人、物理、伤害、能量、地图和通关要求不放宽。

## 实际改动与第二次 J

仅修改两份既有测试 C# 和 canonical 验证脚本。没有新增运行时文件，库存仍1857行／897 C#。在原 `DescentClawGuidanceRuntimeTests` 的同一个 `Attacked` 监听中保留原计数，追加标量时间戳；通过 live Func 读取最新值。原 J 按下→原3帧输入推进→松开→原 Settle→原攻击状态断言的命令、等待和断言均保留。

断言前读取真实 CombatModel 的攻击ID、elapsed、IsAttacking和实际 settings 总时长，并记录输入序列、physics/process、能量、盾、爪、模式、HP及引导。只在既有测试内反射，不为测试增加生产 getter、信号或 Reset/Tick。前后标量状态相等检查是**有界只读检查**，不证明反射、分配、日志对耗时绝对零影响，也不能补出未观察到的中间历史。

先用真实 CLR 模型做独立控制：缺少读取 helper 时实际 RED；随后非默认1.0秒设置下，未启动、攻击中、自然结束、Reset四状态各重复读取，原模型状态与下一攻击ID保持，实际 GREEN。这不是角色原生攻击根因证明。

在原独立 `descent-guidance` 和完整 pre 前缀里，第二次 J 都实际接受：原计数1→2，ID2；断言时 elapsed 分别为0.13333334／0.21666667秒，均小于实际0.30秒，仍在攻击中。HP5、Playing、槽位1、爪Idle、机械爪权限和引导成立，`ObserverReadOnly=true`。因此**上一轮的第二次 J 失败本轮没有复现，根因仍未知，没有做推测性修复**。松键后当前 AttackPressed=false 不构成输入丢失证据。

自然结束保留攻击ID及结束elapsed，而 Reset清零；但后来Reset可抹掉此前结束状态。单个末态快照不能证明整个中间过程。

## 前段超时：首次有真实晚段耗时记录

普通 pre 仍在原180000ms上限严格失败，实际等待180194ms，没有延时、豁免或静默重跑。这次 v156 新增的 OBS 实际运行：

- 原最后 traversal 进入时 engine_ms=166102。
- 实际16次样本进入、15次对应返回；没有孤立返回或重复配对。15个已返回样本累计13631引擎毫秒。
- 从166102到179829的13727引擎毫秒内 physics/process 和样本持续推进，不能说这一整段死锁。
- 最后返回是 UpperPendulum/high_to_mechanism_landing 的0.5样本；0.8样本在179829进入，随后 owner超时，没有成功/返回收据。
- 原28样本还剩13：上述0.8一例，以及 LowerBuried entry→arena、BellChain arrival→floor、BellChain floor→dais、FallenBellkeeper retry→floor各3例。

引擎毫秒和进程 owner 等待时钟起点不同，**不能据此计算“精确还剩14秒”**。只证明已观测晚段在推进，不证明剩余都会完成、最后一例内部停在哪一步或不存在性能问题。前段全套资格保持 FAIL。

## 同版完整 headless 正常计划双路

两条原有诊断入口均由真实标题、Godot键盘事件、正常路线计划开始；没有前缀跳转、传送、进度注入或降低零恢复要求。原600秒预算保留。它们是 headless normal-plan diagnostic，不是显示、OS键盘、失焦、FPS、正常速度视听或真人认可。

### Main：耗尽防御能量后的实际战死

本次主线133615ms失败：实际死亡后恢复到测试专用存档的盆地检查点 `(1680,3960)`，触发零恢复/位移断言。不是已证实的非法地图传送或缺失通路。Boss与实体支撑出口尚未到达。

只读核对的因果边界：

- lift接近阶段居民 precision-burst、ghost-diver先造成HP4→3→2；真实 Moth K/cast11及眩晕应用成功，不是缺少K。
- physics6529最后一次J决策时能量0.24266548、反击窗约0.60秒，真实攻击6530后剩0.12733215。
- physics6583护盾确实阻挡，能量0.01933218；6588维持消耗由0.0033321774降到0，仍按住防御但盾失活，未过热。
- 6593 wave Burst弹体造成HP2→1；6617实际居民 `canyon.v2.resident.counterweight-lift.04`／`precision-burst.tscn` 的局部弹体12造成HP1→0，随即 `DEATH RESET`。它不是 ranged timeline 的 AttackId4，不能混用身份。
- 最终恢复计数1/0、5条伤害归因、0归因缺口。早于恢复已发生真实死亡。

下一项候选是只改变自动试玩最后低能量J的准入，按真实防御维持消耗及外来弹体危险时间计算储备，再与同一路线比较；**尚未设计/实施/验证，不称修复**。旧日志 `energy_gate=false` 只是0.35诊断比较，不是实际准入门，不能直接把0.35硬接为规则。不能用加血、关闭居民、放宽恢复或随意阈值代替。

### Explore：抓爪飞行中受阻，不是掉血或失去平台

探索177473ms失败于“ascent clearance walk retains HP, original support and the single real admitted cast”的组合断言。已明确实际失败的是爪有效性：

- stdout1944–1945：physical K只接受一次，physics9438／reader sample9408，目标AscentGrip9Grip。
- stdout1952：physics9451，Travel／Failure=None／cast28，目标正确；HP1、仍站在AscentGrip8。
- stdout1953：physics9455，Released／Failure=Obstructed／release=Obstructed，同cast28，但目标清空；HP仍1、同Grip8支撑仍在。位置 `(4725.3145,1603.9601)`，速度 `(-169,0)`，向左行走，没有K或跳跃按住。
- 原断言中的 Failure==None 和 CurrentTarget==castPost 两项实际不成立；不是cast身份改变。最后伤害在physics2772，边界附近没有新伤害；全程4条归因、0缺口、恢复0/0。

冻结源码 `PrologueCanyonGeneratedMainRouteTests.cs:947–948` 调用原 Task4ReachSupport(AscentGrip9)。原 traversal 在同次抓爪Travel中向左走出Grip8旧抓点胶囊包络，随后才自动Reel；失败发生在这个原clearance过程中。预检曾正确命中Grip9、射程511.49/620，但不能证明后续移动扫掠无遮挡。角色脚下接触日志**不能识别机械爪实际撞到的对象**。

下一项是保留同一角色、单次K、原clearance输入/等待/预算和零恢复门，只被动记录生产爪在9451–9455首次报告阻挡时已有的 collider身份/路径、扫掠端点、hit point/normal。尚未取得该实际见证，**不先改地图、物理或忽略遮挡**。

## 本轮全部八次原生尝试

证据根：`G:/knight/验证证据/october13-stable-claw`。下表保留完整案例名，八次构建均exit0。最终七阶段4 PASS／3 FAIL，另有早期控制RED一例；不是整关全绿。

| case | build ms | native结果 | 严格结果 |
| --- | ---: | --- | --- |
| descent-j-observation-control-20261029-190649-271-5b2c0d9efd7e41ffb4cae79c7b01f8d5 | 39505 | exit1／555ms；missing helper | RED／FAIL |
| descent-j-observation-control-20261029-191051-211-bd85c21c36304bf78454e78a3f44a515 | 41412 | exit0／587ms | PASS |
| descent-guidance-20261029-191404-648-72d420e7ebe54004aa64b1c4e8077723 | 34968 | exit0／14949ms | PASS |
| prologue-pre-20261029-191504-943-032bf59363ae4c129b747dcf2494b791 | 32383 | exit−1／180194ms，超时 | FAIL |
| models-20261029-192227-322-0f2d941b29db4516b904767508fcf2f4 | 39428 | exit0／819ms | PASS |
| prologue-post-20261029-192320-040-ca88b037fea04d1ab01a5fac42241bd8 | 40858 | exit0／226015ms | PASS |
| thruster-route-main-diagnostic-20261029-192758-550-5556ca80e55f4dff8add6a64dc189d72 | 30997 | exit1／133615ms，无超时 | FAIL |
| thruster-route-exploration-diagnostic-20261029-193053-003-8c1789783e7745dba77e1b7f23135ba1 | 42506 | exit1／177473ms，无超时 | FAIL |

post保留原75 executable＋owner8，含原4条确切负控WARNING，不称“无任何警告”。本轮未重现历史资源FATAL不等已修好FATAL／ShapeRID，也不构成性能认证。

## 审查与本轮全部取舍

唯一 fresh源审查 `second_j_witness_final_review_v157`：C0/I0/M0；不需代码修复/二审。认可限定测试增量，不验收完整关卡。未新增Minor，所有既有延期项原样保留。

执行判断与代价：

1. 延续用户已批准的完整质量目标，先让真实故障可区分，不重开引擎/美术选择；代价是本轮没有可见新玩法或美术。
2. 采用测试内精确只读反射，不加生产API/信号，不保留模型/资源引用跨await；代价是私有字段耦合和日志/分配耗时，不能宣称绝对无影响。
3. 无窗口正常计划测试继续工作，但不启动未经当前确认的前台/系统输入，也不盲改正常显示入口陈旧的SourceRows1695门；代价是显示、失焦、AV、FPS与真人验收仍OPEN。
4. 第二次J两次阳性也不修不存在的已证根因，不把松键后的false边沿判为丢键；代价是旧J原因及完整pre资格继续未知/失败。
5. 原生执行中仅允许忽略目录内MD审查包/ledger记账，源/helper/bin/Git/input冻结；当时两个route清单确认无MD运行输入。利用并行审查但不把未结束测试算通过；代价是root必须独立接收最终双路失败。

审查所有未判断项的root处置：

1. 旧第二J根因/修好：不关闭；当前两阳性不消除旧失败，后续仍按原证据核验，代价是不能宣布输入/攻击已彻底稳定。
2. 中间Reset/自然结束的全部历史：不推断；末态快照范围有限，代价是无信号的中间态仍未知。
3. 绝对零副作用/时序：拒绝作此认证；只保留实际有界标量检查，代价是未排除observer扰动。
4. 全pre、缺失样本、内部停点和性能：保持FAIL/OPEN；15/28已完成不补余13，代价是前段完整资格未通过。
5. Main Boss/出口及死亡修好：保持FAIL；只核实耗能/弹体/死亡/检查点链，候选reserve尚待真实对照，代价是主线未通关。
6. Explore终态：审查时由root收尾，不回填为PASS；当前真实FAIL已记录，下一被动阻挡见证，不猜地形，代价是完整探索未通过。
7. 历史FATAL/ShapeRID/资源所有权：保持OPEN，不改引擎/缓存/GC/强根/Dispose试绿；代价是稳定性不能交付。
8. 显示/OS/失焦/AV/硬件/FPS/真人/全部十一项：均保留原门，代价是本轮不能声称精美完成、正式通过或发行。

## 保管、保存与边界

独立只读核对本轮8案80份清单，40/40对 source/user/helpers/continuation/bin 均逐字节一致；各案 `guardFailures=[]`，engine.wait与provenance一致。root再按最后源清单实际哈希全部1857项，0缺失/漂移，897 C#；与v156最终清单相比只两份测试CS变化。当前两CS、canonical和Debug6bin均匹配最终清单及保留bin。八个本轮原生PID均已消失，未杀其他进程。

明确保管缺口：最后Explore案例没有保存 DescentClawGuidanceRuntimeTests 的before/after两份实体副本（阶段选择复制范围），虽然两侧清单行存在且当前SHA匹配；不能称“两CS实体副本全齐”，不事后回填。该源码的最终实体保存在本轮descent/pre案例中。continuation对是实际存在的2字节空行文件，不算实质库存一项。

- 最终source清单SHA256：`7A32005BC818A2C261E34AB28D8313EA8E00F6507B5C3AE6FBC5CC3F42B98233`
- 最终binary清单SHA256：`34AD9D65E8D1DA2CE2C8AAE7EE7B3BE1F684D2848286C66D64ECE1726F0E74D6`
- canonical SHA256：`9CC2A0337E86D1BEEF12360338461EB2D8C4E3A6233C6253385A7FFD94199F83`
- HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`／index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`保持。

本轮workspace `custody-result.json`保留核验范围。没有新图稿/视频、付费、前台/OS键鼠、再次原生崩溃采集、自动化、Git发布或删除；真实存档没有语义读取/改写。正常汇报前手动媒体Check：1,431,609 bytes／16文件／Notify=false，未跨新增20GiB。完整十一项、正式角色/场景/怪物美术、动作、音效、UI、正常双路、硬件和用户游玩认可继续ACTIVE/OPEN，不以源码审查或无窗口通过替代。
