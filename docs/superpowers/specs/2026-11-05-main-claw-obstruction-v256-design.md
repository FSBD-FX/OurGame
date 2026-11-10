# v256 · 当前完整Main的原机械爪拒绝身份

## 事实 / 推理 / 风险

v255当前普通Main已过gallery，但Grip9 cast25在8867被接纳、8884 Released/Obstructed/target空；HP1及Grip8接地仍保持。没有原拒绝branch/collider身份，不能由Obstructed猜LOS/stall/TestMove，也不能说与新反击时序无关。现有3处条件编译日志可读取原查询结果，不需新增查询/物理改动；canonical host只准contact-blockers、Explore和受控Grip9，未准完整Main。

下一只增加exact `thruster-route-main-diagnostic`的trace准入，原符号`CLAW_OBSTRUCTION_IDENTITY`。日志分配可能影响时序，所以本轮trace仅诊断，不替普通Main资格。若未再现，保留真实结果，不用小fixture或固定帧替代完整运行。资源Handle/FATAL仍OPEN，本轮不猜改资源。

## 完成首次测量后的有证据扩展（Task2）

实际172250 trace在8885/cast25命中原LOS拒绝门（losFalse、saved_contact/stalledFalse）。当前collider4916160500924精确等于当前8831接受伤害源ActorId，scene lost-echo.tscn/slot canyon.v2.resident.split-level.02；不是旧ID推测。现有ClearTask10Grip8Ghost仅在Exploration/keyboard/Grip8后调用，Main漏接。普通models已Rebuild恢复，源和6binary与v255相同、10保护对0diff，初始诊断epoch封存。

按已有意图保留真实移动敌人遮挡；不去掉碰撞/LOS、不改墙/牵引力。新增纯选择`Task10Grip8GhostClearanceRoute(bool keyboard,bool main,bool exploration,int gripIndex)`，keyboard且index8且Main或Exploration才进入原清障流程。独立literal先以旧Explore-only行为RED，后最小GREEN。原函数不改：唯一当前healthy saved lost-echo、原Grip8完整接地/HP/同player、clawIdle、0新增K/jump/jet/recovery，原600physics；实际Recovery/场景/攻击发布/双攻击能量/真实J-Hurtbox相交、foreign窗口、释放后fresh复查、真实每击减1HP、自然死亡撤碰撞仍严格。选择true不是战斗许可，不保证机会存在或Main必过。

仅新增这两个既有测试CS的helper/调用及单元；不改游戏逻辑/数值或原清障函数。当前普通Main一次验证actual清障、Grip9与后续Boss/支撑出口；其余当前同源14阶段各一次，所有失败保留，旧16不代签。无符号当前二进制与初始trace epoch分开，不把不同来源合并为同源全绿。

## 行为要求

- canonical host早准入与独立Get-ClawBuildArguments两处同步增加一个exact阶段；原3正例不丢、大小写/未知/后缀/其他模式拒绝，8种混旗标/waiver仍由早门拒绝，builder独立拒绝自己拥有的REF/metadata组合。
- 普通Main仍exact7个Rebuild/no-restore argv，无符号；trace仍exact8个，仅追加CLAW_OBSTRUCTION_IDENTITY。普通运行任何3种CLAW_NATIVE身份日志仍严格拒绝；trace provenance保持behaviorAccepted=false及wholeGoalAccepted=false，原错误/失败/cap不放宽。
- 测试解析真正canonical host，只执行真实ParamBlock+早if、命名纯build函数及已有限定元数据/污染门，不执行整个launcher，不进行构建/native/存档写。手写literal期望和实际RED→GREEN。
- 仅root编辑host/测试并运行native；有owned作业时所有源/宿主冻结。SourceRows1897，无新CS/UID。实际trace一次完整Main，按caster/cast/target/physics关联gate与适用LOS/PULL；PULL_HIT本身可能合法，不能单独归因，也不强求短路未执行的所有hook。

## 边界

唯一G:/knight/Knight，保脏树/原件/失败，无Git写/删除。仅已授权headless/internal keyboard/隔离存储，无GUI/OS输入/付费/新媒体/dump/GC/cache/KeepAlive/引擎或实际存档实验/自动化/发布。完整原十一项、正式美术连续动作/R-I/R-C/R-P、双线正常版与严格suite/正常速度/硬件/真人仍OPEN。新数据决定下一实际修复，当前不预设地形/牵引参数改动。
