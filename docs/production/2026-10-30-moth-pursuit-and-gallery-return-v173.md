# v173 · 飞行怪追逐选择与战后返程前沿

## 已推进与尚未完成

原v172完整Explore在physics10270被必需波次Moth击杀。当前仅改变自动试玩的追逐消费者：可选常驻Moth必须处于原J攻击几何内，才能在有活波次时以距离挤掉波次目标；每帧重算，不删怪、不关闭AI、不永久屏蔽。所有来源继续进入原危险扫描。没有改变玩家爪瞄准夹角优先、K窗口/LOS、地图、HP/能量、伤害或时限。

本轮实际地图：`thruster-route-exploration-diagnostic-20261030-095957-361-c5dd4d5f846e4de08588af5597d11b24`。角色HP1保持、recovery0/0；实际split-level组完成，最后必需波次Moth实例24345216813658的HP0。不能把整组12次攻击/8次连接全部归给Moth，也没有逐击Moth专属伤害/死亡收据。该组总thread_casts7包含此前Bell；Moth观测为4次CastAt返回，不能混报。

随后完整路线仍FAIL：战斗后玩家(4045.772,1483.9337)，支撑为UpperBasinShortcutWinch/Barrier，而非完整SplitUpperGallery。Grip9准备在entry即被原守卫拒绝，jump count16未变、dash0、90/12/30准备预算尚未使用。不是角色死亡，也不是通关；下一应通过合法实际移动/普通跳跃返回横廊，再进入原准备，不写位置、不放宽支撑或恢复规则。

## 根因判断与取舍

- 原选择允许|dy|≤74，而原Moth K要求dy<−74；被选常驻怪在dy−70.55..−19.94、且J距离够不到时，现有策略无动作可执行。原完整arena同时包含双方，缩arena不是真实修复。
- 仅加入必要的**空间追逐资格**，不是“当前攻击必然成功”：竖J为dy[-74,-10]/|dx|≤36；横J为|dy|≤32、|dx|[8,64]。共享原J几何避免两个消费者漂移，非有限值拒绝。
- 不把能量、冷却、举盾或claw当前空锁作为追逐资格，避免资源瞬态改变选敌；实际动作许可、接触与伤害仍由原流程证明。代价：近处但此刻不能攻击的怪仍可能被选中，不能由几何资格冒称可伤/无害。
- 只在纯键盘split-level batch1启用；必需owned不筛，group0保留原pending，未造出新波次/完成。原900物理帧、全路600秒与所有完整支撑/无恢复守卫不变。

## 验证记录

证据根：`G:/knight/验证证据/october13-stable-claw`。

| 案例 | build/native ms | 结果与边界 |
|---|---:|---|
| mixed095424…a1c2512c | 46600 / 未启动 | 新fixture缺Moth命名空间CS0246；已修，非行为RED |
| mixed095552…19f56376 | 33682 / 927 | 精确MOTH_PURSUIT_REACH_RED；旧近resident选择及两source扫描前提先通过 |
| mixed095837…7b5fb7b3 | 38132 / 3811 | focused严格GREEN；18几何边界、近resident保留、pending和原mixed控制通过 |
| Explore095957…c5dd4d5f | 35839 / 225969 | 清原Moth组并存活；后续Grip9 entry支撑FAIL，整路不合格 |

新focused是保存场景/真实绑定的selector组件：自然出生owned首波、ghost-diver resident，输入只是虚拟查询位置，不移动或改HP。它证明选敌/全来源准入，不证明攻击接触、完整路线或真人手感。原mixed控制仍有实际rust_guards1/burst_guards3/玩家HP5→5，不能将这些借给新Moth组件。

唯一fresh review C0/I0/M1：当前vertical/horizontal实现正确，但18边界只验证union，没有单独固定两个输出分类；属未来防回归增强，延期，不修、不二审。若未来误换输出，可能改变上攻/横攻方向，不能称此风险已覆盖。review不认证巨大dirty历史、旧Ranged全文件历史、完整路线、资源稳定性或正式视听/硬件/真人。

同版普通资格结果：

- pre100709…fd2a2a13：build113782/native168526ms，原180000cap、脚本exit0/phaseOK，但stderr/external各同组14条Handle ERROR而严格FAIL，不算28个独立事件。栈经Supply.OpenSample5、Descent.Run8、InstantiateLevelFixture1；本案无InvalidDefinitions/FATAL，但不能关闭历史FATAL或推导根因。
- post152016…9f510350：build45202/native230779ms，原300000cap、75 enter/return各唯一0–74、owner8/nodes1→1齐全，限定4WARNING（observer1/profile1/flow2）完整保留，严格PASS。不是旧180资格或性能认证，也不是无警告日志。
- models152700…65fedf2f：build43031/native690ms，现有机械爪models入口exit0；不冒称覆盖全部游戏模型。
- Main153049…0d8e1c5d：build37906/native148939ms exit1，完整Main仍FAIL。physics6617真正前因是常驻precision-burst（counterweight-lift.04）attack12令玩家1→0、E0/盾off，recovery1/0；后续原“不得位置reset”守卫观测4753.8955,3423.9336→1680,3960而拒绝。不能从这条守卫单独称为无因瞬移bug，也不能以死亡后的合法复位当零恢复通关。

最终所选6阶段中focused/models/post通过，pre/Main/Explore失败；整个项目全suite、旧资源根因、正式美术/连续动作/音频/UI、目标硬件、用户游玩认可与原十一项均未认证。本轮没有素材或画质增量，driver推进不等于游戏已精美完成。

## 保管与用户更正

前三案14对清单/1434源实体/36helper/12bin、Explore新增5对/560实体/10helper/6bin全部逐件SHA匹配，案内零差；build失败缺binpair原样保留，不补造。原5个focused与2个Explore实际owned PID均终结。Explore终结时1865源/6bin/9helper与失败案一致，HEAD/index原值未变；后续普通回归的最终绑定另记，不能拿此时点作未来收据。

最终8尝试合计：1build FAIL，无native/binpair；7native严格3PASS/4FAIL（含保留行为RED），没有超时。39组清单对/3994份选定源实体/90份helper/42bin全部逐件SHA匹配，案内零差、未列实体/错误0。全部15个实际owned build/native PID均终止；最终当前1865源/6bin/最后Main的9helper等于最后**失败**案，仅证明同版绑定。HEAD/index仍为e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6 / 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。没有拿不同案的局部绿拼成整关资格。

下一资源诊断候选（尚未运行）：本次Supply同一AddChild内5条；Descent首Load内1条、后同一AddChild内8条，没有首业务栈落在Instantiate。既有TraceSectorDefinitions可分source-pre/instance-immediate/source-post/after-instantiate/before-add-child，但Load在source-pre之前，且不能给Supply5条具体资源命名。可在原pre顺序/180秒中仅加既有flag做一次有界对照；读取getter会扰动生命周期，所以通过不等于修复，不追加GC/保活/缓存替换或原生崩溃采集。先保留这些边界，不猜改生产生命周期。

媒体单次检查CurrentBytes7427750/files86、原基线3508、Notify=false，未增加关键节点媒体，也未建立自动化。

用户明确误发视频请求作废，本轮没有观看抽帧、形成风格结论或改美术要求。只撤回本次新建的51个临时副本/抽帧/工具文件（346179899 bytes）到回收站，可恢复；3个原C盘视频SHA核对未变。该材料不进入KNIGHT参考库。其余没有删除用户文件、付费生成、新窗口/OS输入、采集、自动化、Git或真实存档写入。
