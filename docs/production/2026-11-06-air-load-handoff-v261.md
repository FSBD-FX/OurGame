# v261 · 非零喷射载荷与足端交接

状态：实际生产动作修补与两轮分源证据已完成；最终源码19项为18PASS/1FAIL，整套未GREEN、计划Task2稳定性资格未完成。唯一代码审查C0/I0新增补丁缺陷/M1延期，仅可保留WIP。原十一项目标仍ACTIVE，非正式成品交付。

## 事实、修补与边界

v260只接住了locomotion模板。此次先对非零angle/骨盆/fold/trail的实际保存Formal写反例，零时间交接真实几何最大跳变9.120909px，engine1/656ms。原失败保留，不把构建失败当RED。

共同load现在只缓存最后实际使用的有限angle/竖直shift，不缓存最终pose、动作、secondary或喷射身份。在有效Jet→Ordinary边界，从已显示load和足端开始，沿现有presentation elapsed/2/15秒过渡到实时普通目标；稳定Jet/Ordinary继续原直通。公共Frame、Motor/重力/速度/单跳/攻防/VFX资格完全不改。共同angle按world-facing符号缓存，翻面只重表达这层。

真实两个生产值模型证实可出现零时间双inactive gap：Jet清lease而ordinary尚无新目标。之后即使render delta为正，只要physics tick相同，ordinary仍可inactive。因此当前实现对合格自由空中/无非法raw Active的缺样本，elapsed0冻结纯视觉共同load与脚端，正时间沿原2/15秒有限释放到中性/自由脚；普通目标到达时从当下显示姿态重新接续。公开AirJet/Mobility不恢复，也不续火焰。非法/地面/Dead/Recovery/抓爪/显式cue仍立即清旧历史，新Jet立即优先。首版“正时间无Ordinary就清除”的前提已被真实RED证伪并改正，不把合法缺样本伪称坏数据。强制替代/坏数据路径并未被授全局连续性资格。

## 已执行专项

- RED `thruster-ground-gait-20261106-015435-590-33f2f165499b4a7487473229922e5e44`：656ms，quiet fresh build，实际几何9.120909px失败。
- 首GREEN `thruster-ground-gait-20261106-015802-457-29bc36f638da498a81438664e020b510`：54案（三频率×双朝向×三EntryWeight×None/AirAttack/Shield与非零secondary），10槽/世界ankle/sole/刚性18+18/握持，期中冻结/回退、有限null、真实model gap及6取消控制；保留原ground-gait完整分支。
- 补完计划边界GREEN `thruster-ground-gait-20261106-020109-807-04b6a0cd7aa147069d1f026c81166d13`：同54案加9取消/非法源/抓爪、换基、复合退出与状态、cue重置对照，全原分支严格通过。
- 实际保存Player仍从内部A/D+Shift自然结束Coast、同Fall/正时间/dash1/jump0/不退款；旧“新Body必须马上等于raw Ordinary”的现状刻画已替换。第一Ordinary消费者Body pose step=(0,0)，十足端值用各帧Formal完整逆变换扣除真实根移动/根倾角后保持；之后实际继续20物理等待达到live Ordinary目标，非永久冻结。raw世界脚仍随根真实下落，不伪造世界脚底不动。左右双手有效接触误差约1.5e-5至3.2e-5px，非正常速度美感阈值。
- 第二个真实反例 `thruster-ground-gait-20261106-024108-850-7d1e115f245943ee956297477285a9a5`：真实生产模型同physics tick的正render step仍inactive，实际rig应有限释放到angle0.023211345却直接为0，AIR_POSITIVE_GAP_RED。此前第一阶段四源已另存before-positive-gap，所有失败/原19结果不覆盖。
- 修正后GREEN `thruster-ground-gait-20261106-024433-731-689ab30fa02945a7b60fe3f2e0a7e961`：同物理样本正时间gap、进入时重新捕获当前显示、三频率无目标有限归零与free feet/不复活旧来源、54非零action/rate/entry及8强制撤销/换基/复合/cue控制、自然Player和原完整ground-gait分支通过。该当前源码须重获宽门资格，不借第一阶段的构建证明。

## 当前宽回归

第一阶段有限19已终态17PASS/2FAIL，原audit-v261.json绑定95保护对/1898完整输入/6当前与114封存binary/20helper在该时点0差异；它是第一阶段冻结源证据，**不是后来正时间gap修补的当前资格**。两个正常自动路线均Boss/支撑Completed/61hold/0恢复；独立冷存档1633ms。第一次完整集未GREEN，计划和原十一项均未完成。

第二阶段确有新的生产修补与真实RED→GREEN，使用独立run-positive-gap-regression-v261.ps1与positive-gap-regression-results-v261.json执行最终源码19门，绝不覆盖原19或因观察超时重复启动。handle38544已终态exit1，**18PASS/1FAIL**；前段146380ms/44项通过，后段159901ms再次匿名gchandle终结器FATAL，在Task7 case20返回、case21指引与BossHUD检查进入后，未有21返回；这不是普通HUD重叠断言，也不是仅收据工具问题。原两负控警告已执行，后续完整scope未到达，严格FAIL保留。其他18项无原生警告/错误。不能因首版同类故障就排除当前源的间接影响。

最终源`positive-gap-audit-v261.json`独立核对完整19有序阶段、95保护对0差异、当前1898完整输入/一个源版本、当前6及114封存/一个普通二进制版本、20宿主依赖每路径同一hash；与首版audit分开不混用。Main249976ms/Explore319156ms到Boss/支撑Completed/61hold/0恢复，激活-击败8/8与11/10非76全量；隔离存档暖12830/独立冷1623ms通过。正常显示/human/full operations/fullGoal均false。

因宽门未GREEN，本计划Task2及完整计划不标完成；当前补丁只作工作版本接受一次独立代码审查，不交付、不发布、不把未知资源崩溃变成例外。下一仍需当前源码资源身份/生命周期核对与正式动作/艺术审阅，额外原生dump、引擎/GC/cache策略等须另行授权。没有新重试或自动化。

本轮有限19阶段按原严格宿主/预算执行，生产/runner/宿主/资产冻结。未终态前不声称当前整套通过；未知错误照常否决，不刷绿、不复制旧版本资格。当前pre engine0xC000001D/21888ms、post0xC000001D/15732ms均出现gchandle.is_released终结器FATAL，post另被缺少完整scope收据严格拦截，均保留FAIL；不因症状同类就声称根因相同。其余阶段按原有限批次继续，绝不因此重启整批。旧v259资源FATAL/Handle失败不因局部成功关闭。

独立只读核对：pre停在首个Camera suite、第二个Ascent高位fixture mount-return之后，**没有完成整项pre案例**；post完成结构/material及首个Basin低位12个相机采样，音频drain后FATAL，drain不等于整个fixture清理完成。post尚无到达后续warning-scope阶段的证据，不能把缺收据解释成observer动作已跑却漏报。新增ApplyAirLoad会在保存Player的Formal._Ready路径调用，不能以“字段全是标量”排除间接影响；当前分支和具体被终结对象仍未知。无同源REF身份可转授，旧serial1037不得套用。详见本轮`native-failure-readonly-v261.md`。因此宽回归资格仍未获授，本轮不能宣称稳定成品或整个计划全部通过。

## 后续仍需完成

唯一fresh审查只读完成，未确认新增C/I代码缺陷；但最终post失败仍是Important验收阻塞，不是已证实的补丁归因。M1延期：54组稳定action只证明该握持时接触不丢，Shield分支跳过副手断言不等于已证明释放；后续需handoff中AirAttack→Shield→None序列，核对主手与副手释放终点。不得把grips=true理解为完整释放路径通过。全部5个set-aside与逐项裁决/成本在本轮final-review-v261.md，无二审/新补丁修补。

这修复了指定非零边界的几何合同，不是正式动作设计或Spine式精美度验收。全身转向、全局回退时钟恢复、所有强制替代/坏数据路径、完整动作语言/R-I/R-C/R-P与正式主体/披风仍需进一步制作和正常速度审阅。全76/完整操作/硬件/真人/真实存档安全资格未授予。V1原稿暂缺，现稿聊天重审已获准但外观方向选择未答复；不转授旧批准、不自动付费或接入。原图4份与展示副本4份保全，当前只显示A/B。

最后普通一次容量Check：10490030B/96files/Notifyfalse，未到新20GiB档，不建自动化、不停工或删除。保留整个脏树、两阶段源码/全部RED/FAIL/审计；无提交/发布。下一优先现有同源资源身份/生命周期证据定位；无对象身份前不猜资源类或套用旧serial，也不改门禁/GC/cache/引擎来掩盖。若需要额外原生dump等另行授权，当前不擅自扩大。

U-20261106-01新授权允许在聊天展示已保存候选，已无损复制4原图及4展示副本并先展示A/B，来源索引在`G:/knight/过程图稿与视频/2026-11-06_角色现稿重审_v001/source-index.json`。这是现有图稿重审，不是新生成图、批准、游戏接入或质量验收；外观方向还待用户答复，旧批准不转授。按用户chat-only范围，不新开生成面板。

无GUI/OS输入/新生成或录制媒体/额外dump/引擎或GC/cache/资源策略/真实存档实验/自动化/Git写入/删除或发布；新增保全仅上述授权原图副本。
