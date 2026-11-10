# v260 · 同态空中姿态交接

状态：本轮局部动作修补与同源19项回归完成，唯一独立终审C0/I0/M1、可保留；原十一项目标 ACTIVE，不是完整序章交付。

## 事实、推理、风险

真实保存 Formal 的中性零时间反例已实际失败：JumpStart/右朝向/近腿角直接改变 .28 rad，世界 ankle 6.1398973 px、sole 7.2562423 px 跳变。此前局部脚底测试没捕获此问题。根因为普通空中载体会换 Body/腿模板，但过渡边界只认移动状态改变。

Formal 现在同时跟踪**有效普通空中四态模板**，沿用原 locomotion pose、2/15 秒过渡和原计时。没有把 action、刚体载荷或 secondary 烘入 locomotion 历史；第一帧/cue reset 同步签名。不改 Motor、速度、重力、单跳、战斗窗口、图像或资源策略。

风险未合并：非零 AirJet 的 postmix 躯干/骨盆载荷到期清零，普通空中载荷另起响应，仍是独立缺口。真实自然交接实测 Body pose step 非零；本轮没有悄悄改成新接受标准，也没有把这称为完整连续动作或 Spine 品质。

## 已执行的局部证据

- RED `thruster-ground-gait-20261106-004722-675-1fc17341d98b43bfabae84abc070feb5`：fresh build quiet，真实消费者 AIR_CARRIER_TEMPLATE_RED，原失败保留。
- 首 GREEN `thruster-ground-gait-20261106-004935-783-77e9a1c0d2ce417c997a9a6fc6da2aee`：四空中态×双朝向8案、拒绝通道4案，零时间/重复/回退世界 feet、退出/正时间最终目标、刚性18+18和根不变；完整旧 ground-gait 分支继续通过。
- 加入真实 Player 自然交接后 GREEN `thruster-ground-gait-20261106-005157-007-1959b1955346456295c56ef49df5a3d4`：engine0/15454ms。双朝向各一次内部 A/D+Shift，实际空中、coast 自然到期、同 Fall、正时间、dash1/jump0、无返还，实际18+18/脚底与武器双握点、observer0。
- 该自然样本右朝向：旧 jet angle .000902486，新 ordinary angle .0018888834；真实 root step (7.044342,13)，扣除根位移后的近脚 step (.028564453,-.022155762)，Body pose step (.016768737,-.0259463)。左朝向另有独立样本；均非相邻世界脚底相等的伪断言，也不是审美阈值或硬件帧率结果。

## 当前宽回归

一次有限19阶段批次在本轮工作区 `run-current-regression-v260.ps1` 执行，全部生产/runner/宿主/资产冻结，已终态exit0、**19/19 PASS**。原预算不变，未知错误照常否决。models、presentation model、完整 ground-gait 加既有普通17门（包含 models，合计19），不复制旧审查批次或重跑 REF 试图刷绿。

前段144660ms/44案例与后段185893ms通过；后段4个原有精确负控警告单列、ERROR0，其他18项原生stdout/stderr警告/ERROR均0。Main252685ms/Explore334819ms均从标题、持久同Player到Boss/有支撑Completed出口/零恢复；激活/击败分别8/8和11/10，不等于全76覆盖。隔离存档暖13004ms、独立冷1707ms通过。正常显示、操作全覆盖、human和wholeGoal均false。

本轮 `audit-v260.json` 绑定19案、95保护文本对0差异；当前完整1898原生输入与一个源版本、当前6二进制及114封存/一个普通构建版本、20宿主依赖每路径一个hash全部匹配。仅本轮当前普通批次，不将旧RED、旧REF或旧helper版本冒称相同。两条路线各61物理帧保持、completionCount1。v259 pre/post/REF 的资源故障保持 OPEN；旧失败不会被新成功冲销。

## 唯一审查、裁决与下一步

唯一fresh审查gpt-6-astra/high只读完成：C0/I0/M1，局部修补可保留，整游戏不ready；无二审、无新C/I修补。M1延期：中途blend重复/回退、模板退出与状态同帧、无时间输入和非零action/secondary组合尚未新增直接运行覆盖；已有静态顺序合理不等于穷举证据。后续补少量确定性用例，不把该缺口写成已验。

完整审查与逐项成本见本轮工作区 `final-review-v260.md`；全部8个set-aside均有根代理裁决：非零postmix/正式美术与正常速度手感/全局回退时钟/全组合运行覆盖/历史原生故障/76全量与完整操作及硬件真人真实存档安全全部不授完成资格。audit是根代理实际重算，不冒称审查者逐hash独立重算。最初CR/no-index只读检查包装错误已如实改正，CR感知四文件均无诊断、exit1只表示文件有diff，未改源码换行或算作native证据。

下一生产目标：先修已实测的非零喷射postmix载荷交接，再继续正式连续动作设计与后续现稿审阅；不得为了保留本轮BodyAngle现状刻画测试而回退正确的postmix修补。正式画面/手感需当次获准的正常速度审阅，原十一项继续ACTIVE。

留档与容量：本轮没有新媒体；官方一次Check为7427750B/86files/Notifyfalse，不建自动化、不停工、不删除资料。保留脏树、before拷贝和全部RED/旧FAIL；未提交/发布/清理。

## 用户决定与未完成内容

U-20261105-01：暂找不到 V1 原分层稿，后续重新审阅现有候选；不扩搜、不沿用旧批准、不自动付费重画。当前程序主体/披风、正式动作设计、可感知惯性/专属动作/物理手感 R-I/R-C/R-P 仍 OPEN。三段误给视频作废。76全量、完整双线、正常视听/硬件/真人认可不由本测试代签。

无 GUI、OS 输入、付费、新媒体、额外原生采集、引擎/GC/cache 改策、真实存档实验、自动化、Git 写入、发布或删除。
