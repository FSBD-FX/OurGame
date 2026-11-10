# Bottom control preparation

本聊天原十一项范围与正常纯键盘路线不变。v175实际Explore175016…0fb87efd：attack3先Shot11651，第二control11659开始、11665已ACK，11697爆炸杀HP1→0，未到Idle。两个普通跳和落台已通过；新问题是把已在飞行的危险带进全中性、不可防御的操作阶段。

方案裁定（实现者选择，不冒称用户逐项批准）：不删Switch的Neutral/方向/两K/两跳/HP/恢复/120合同，不削敌或给血；不在Walk返回后新建等待池。只在纯键盘return-control这一次Walk的原90帧内，把“移动到位＋静止纵向盾＋当前全源危险清除”作为同一准备阶段，随后原Settle12证明释放L与fresh neutralRight，再执行原Switch。此前持续完成的用户指令与本次目标续行已要求自主推进，此处是原验收输入策略修复，不是新玩法设计，故不再索要重复设计/执行模式确认。

动作仍用v175即时授权Missile纵向意图（不是安全证明），且只在完整Step支撑、原目标±3和≤2速度后才停止Steer、松A/D/Space并申请W/S+L。每帧仍走原Tick/observe/Guard链，同一phaseFrames最多90，不重置或借Leg120。观察失效、来源缺失、未知Active、重叠或观察fault不能准入；GuardIntent为零也不能准入。每轮独立新扫原Basin encounter＋resident的全部危险候选，只有有效读取且当前无Threat才结束准备；现有active-gap/live-tail规则不变，无授权叶、actor Recovery或旧日志都不单独授予许可。叶列表不跨await。

这里证明的是当前危险清除，不声称未来120帧绝对安全。实际弹体Retired、盾active/Guarded与HP保持只能由当前完整原生收据关联，诊断观察器不参与动作许可；完整Switch必须另外实际完成handoff/Idle/两应用总结。新危险或不适合纵向盾的危险继续占原90预算并可能诚实失败，不编造延迟或忽略它。

原真实死亡为整合RED；另加9个字面准入控制：有效空危险允许；读取无效、Active/Telegraph、缺来源、未知Active、重叠、fault、带Infinity安全时钟但仍有Threat均拒绝。先inert false实际RED，再最小实现、旧mixed控制、原完整Explore。唯一fresh全slice审查，C/I一轮TDD修复、Minor延期；原pre/Main/资源错误、正式图声UI/硬件真人均OPEN。无窗口/OS输入/付费/新原生采集/自动化/Git/删除/真实存档写入。
