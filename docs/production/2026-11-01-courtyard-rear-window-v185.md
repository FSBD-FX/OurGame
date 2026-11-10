# v185 — 院落夹击的实际抓取候选与锁线采样

## 事实

唯一工程G:/knight/Knight，原完整十一项目标继续ACTIVE。上一轮v184为实际进展：庭院单跳转移已过，完整Explore仍死于后续夹击。本轮严格限定已获准的隔离headless构建与测试，无GUI、OS键鼠、真实存档、付费、美术生成、采集、自动化、Git写入或删除；误发的三个视频不用。

只在既有`tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`加入被动记录，不写任何新输入、不加await或等待池，不改生产代码、地图、数值、目标角优先或原900外层预算。原counterObserver只增加旁路统计，原minimum fold不变；额外独立Left resolver每条采样一次，明确不是生产preview或K回执。列表只存标量文本，最多192条，在原finally输出；诊断输出故障不能替换原route失败。

实际命令：canonical `-Phase thruster-route-exploration-diagnostic -SourceRows 1870`。案例：`G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261101-031558-121-cf84931859f94de1879c1e3afbcf748b`。build28050ms/exit0；native255848ms/exit1，未超时，原600000ms不改。实际argv包含headless、隔离storage，无fixedFPS/movie；外部原始诊断严格否决，无guard差异或ExpectedFailure豁免。

100条真实原外层采样＋1条END，共101行，omitted0/faults0。omitted0仅表示没有容量截断；13945→13948之间的13946/13947两个内部等待边界未采，不能称逐physics连续覆盖，也不是完成节点处理后的同tick快照。原死亡仍在physics14024，HP1→0，然后正常死亡恢复触发4808.80px位置重置断言；不是任意传送缺陷，也不是完整Explore通过。

| 实际采样 | 同次所见 | 含义 |
|---|---|---|
| 13923–13925 | 生产preview已Left选到rear target且LOS真，但玩家有向左速度，front Telegraph1仅约.18s | 不能用“可选中”单独授权K |
| 13964–13970 | 玩家J结束；rear Telegraph2约1.02–.92s，front Recovery只余.30–.20s；独立Left始终精确选中rear | 原建议的恢复期偏晚，不足以直接保证旅行与回防 |
| 13982 | front首次采到Telegraph2/.65s；rear Telegraph2/.71666664s、未锁；接地、HP1/恢复0、J/L均false、Claw Idle；独立Left=rear/LOS真，但当前生产preview是右侧front | 这是可进一步试真实转向准备的候选，不是K授权回执 |
| 13991→13992 | 原外层采样从locked=false变为true，remaining约.5667→.55 | 真实观察到的锁线边界；不把pre-node采样编号冒充内部发布的精确时刻 |
| 14024 | rear预警剩.0166666、front Active2、L朝+X；随后rear发射并致死 | winner朝右不能兼顾后束；整线仍严格FAIL |

## 推理与下一实际动作

约424px的爪旅行需要约.24s，再加真实转向、Aim/Windup/采样及回防，不能只拿front剩余Recovery .30s来放行。应在**下一轮front实际Telegraph刚开始**时重新计算窗口，而不是硬编码13982或把Recovery与未来预警擅自相加。当前物理Left查询表明地形没有一概压过rear，但只有真实A/neutral后的同次生产preview才能授权K。

下一待实现控制：同一原900循环内，仅尝试一次真实松L/清旧动作、A取Left、松方向自然刹停；生产Idle发布捕获physics、输入序列、authority、pose和preview；发K前验证同一活rear目标、LOS、八方向和全部来源的现行余量。一个K边需实际Aim ACK、同cast/target/application+1、Latched→Released、rear真实Stunned及撤束/leaf撤权。收到实际消险后及时转右防前敌，不强等额外约28tick缩爪才回防；最终Idle仍须入账。不强锁、不增加等待、不改怪物和抓取规则。

这里仅建立候选和反证链，尚未加入任何K控制；不能称“已经存在完整安全窗口”“已经打断”“战斗修好”或正式交付。本轮也没有正式美术/动作/视听增量。

## 风险与验证边界

生产preview可因转向或惯性过期；独立查询不能冒充它。K会占用盾/攻击输入，前敌的时钟必须覆盖回防，且两侧不能只看winner。引入的额外查询确实有开销，本案255848ms不用于性能比较或目标硬件认证。原Main能量问题未改未重跑；历史资源根因、完整双路、正式美术动作/UI音频/硬件及真人验收仍OPEN。

## 审查与保管结果

唯一一次新鲜只读审查C0/I0/M1，无C/I修复、无二审。M1延期：END没有显式累计未观察physics数或outer-loop-only字段；已在报告明确13946/13947两帧缺口，不能据omitted0授权连续安全。保留现有源码，未把Minor再扩成新实现。

独立保管审计：1870source/9helpers/6bin/109user/continuation共5对manifest前后相同；283×2选定source、5×2helper、6bin实体逐SHA匹配。03:25:10+08当前全部1870源、6bin、9helper与本案一致。相比v184第五案仅Main测试CS变化，其余1869项一致，无生产源差异。当前Main SHA`40FD15A442C65FBC49BB2D766EEF69CE8C722C71910006936EA135ADA8E0D728`，Knight.dll SHA`7729B7421E13526AB5C9D7FB0E060E9CD862B3BBC431C67299C34E0F66C19203`。两owned PID45364/47616均不存在，未见复用。canonical保持`B13EDE3A2FAC2CEA6B13B1CAE0B10D1AAE13F05140BB6507BC0BB8D7A93A304A`，HEAD/index未变。

审查设下的边界逐项保留：未实现的K接受/效果与回防仍OPEN；cap/失效/输出故障没有动态负控，faults0/未触顶不能替代；本轮只核新案，未重跑Main或其他回归，不借v184绿案拼新全套；无merge/正式图形/真人/性能或历史原生资源根因资格。下一环节必须实测，不能以本轮静态/被动观察免除。

工作内手动媒体检查2026-10-31T19:27:25.9646421Z为7427750bytes/86files，Notify=false，基线3508未重设，无新增媒体或自动化。计划/进度：`docs/superpowers/plans/2026-11-01-courtyard-rear-interrupt.md`、`.superpowers/sdd/2026-11-01-courtyard-rear-interrupt/progress.md`。本轮进展是取证改变下一动作，不是玩法修复或交付；原失败与全部历史证据保留，完整十一项目标ACTIVE。
