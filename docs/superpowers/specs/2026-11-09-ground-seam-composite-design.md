# 真实发布姿态与完整物理接触联合验收 · v334

完整原十一项ACTIVE，KNIGHT G:/knight/Knight，仅无窗口。v333是PROGRESS：已修巡逻且取得新实际双通道反例，原13阶段11PASS/2FAIL及旧失败不追认转绿。所有拥有的进程已终结。

## 事实、推理、风险

真实Explore P632/tick453→P633/tick455是连续两次实际正式发布；native同时保存453左面、454右面、455右面。正式far站脚两端约相差.0001世界单位，原严格门却在计算鞋底前因physics不邻接抛错。生产协调器只在_Process及少量零时间刷新时同步应用rig再发布，Motor没有逐步visual事件队列；gait本身按物理tick差累计。不能凭没有P454认定丢了生产通知，也不能用native root补造P454姿态或称像素已验。

用户本轮明确选择“按物理连续＋实际姿态连续校准”：每个物理步真实接触连续，且相邻实际发布姿态同一站脚位移仍≤原.15；替代逐物理步必有独立姿态。只改测试资格，不改生产时钟、物理、动作、输入、地形、敌群或资产。旧v333失败保持原结果。

## 方案与选择

1. 保持旧physics相邻视觉门：严格但要求两种时钟一一对应，会拒绝已观察的正常轮询形态；用户未选。
2. 完整native逐步证明AND相邻真实发布端点实测：用户选择。零补帧、原.15/同脚stance/固定骨长/真实Draw目标及世界/actor/shape身份门保留；成本是需要验证两个端点与native账本的同owner/revision/root及每个publication无遗漏。
3. 强制每物理步Present：增加未显示的姿态/可能重复推进动作时间，且改变生产管线，不采用。

联合门仅证明完整接触和实际发布端点，不声称未发布姿态、屏幕像素、任意时长stance episode或60FPS。缺少共同站脚/任何native步/连续publication/一致权限或root对应，均fail-closed。零时间或先于Motor的发布若无法对应原生完成步，也不得猜补或放宽；保留新失败后按原件查因。

## 实现边界

保留每个FramePublished快照，不再因重复physics tick提前丢弃真正发布；普通步行/左右面最小计数仍按不同physics tick，避免重复发布充数。只有真实左→右接触交接调用联合读器；相邻publication、递增physics、有限非倒退presentation time、两端Walking/同Motor/revision、原native逐步完整、root端点匹配及同脚stance/.15全部必需。PrintNativeHandoff提供同批值型steps并继续记native独立诊断，最后原NativeTraceComplete仍必过。

纯控制采用手算字面450/451/452接触交接及真实两发布，正例允许2physics gap；4px站脚突跳、丢中间step、漏publication、旧owner/revision/root、不同行走、零共同stance和坏时间等必须拒绝。另保留原所有单tick/16物理负控及旧fixture。

## 验证与交付

先缺实现功能RED，再最小读器实现和全部坏控GREEN；当前同版models、动作/rig/gait/恢复/保存含独立cold、前44/后75、完整原Main/Explore。原巡逻新专项已知6ShapeRID不因本测试规则修改关闭，本轮不重跑它筛绿或声称根因修复。严格日志/cap/伤害/输入/跳跃/数量均不松，source/binary/protected/helper及一次mediaCheck保留。一fresh全增量review，C/I一次因果TDD修补、Minor延期。无GUI/OS/付费/新素材/自动化/Git/worktree/删除/真实档实验/GCcache引擎新采集。
