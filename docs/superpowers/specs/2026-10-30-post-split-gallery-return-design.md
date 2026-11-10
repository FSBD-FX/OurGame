# Post-split ordinary Gallery return

v173真实完整Explore的split组已完成、playerHP1/recovery0/0，但人物在绞盘Barrier顶(4045.772,1483.9337)，不是Gallery。原Grip9 entry守卫因完整Gallery接触缺失而正确FAIL。已有静态几何：Barrier top1520，Gallery top1480、x4080..4360，右上Grip9左缘4260；不是另一堵不可过墙或需要普通空中二段跳。真实返回成功尚无证据。

实现一段test-only、纯键盘返程，插入TraverseTask10ShortcutBothDirections的StageGrip9之前。若已完整Gallery站稳则不额外行动；否则只接受完整Barrier向上支撑且位于Gallery左侧的真实起点，拒绝任意落点兜底。松旧输入并等待真实fresh neutral，然后只发一次D+Space，落到Gallery后松键、自然刹到两个速度轴≤2；最终有fresh neutral样本、完整胶囊和Gallery向上接触。

单一90实际physics预算包括松键/起跳/飞行/刹停；每次await前核剩余，不借原Grip9的90/12/30或延长全路600s。恰好1个AcceptedJump、无新增dash/J/K、同Player/driver、HP和恢复计数不变、clawIdle/NextCastId不变。没有位置/速度/模型/HP/能量/AI/碰撞写入；不用附着Boost或空中跳。危险敌人不被删除/休眠，伤害即使返程失败也如实保留。保持原observer/monitor/evidence，通过原observe回调逐帧观察，不重建输入驱动。全胶囊不能越进Grip9左侧顶块；约束仅是失败检查，不是钳位。

原v173完整地图失败就是行为RED。新方法补单跳/错误支撑/实际预算守卫，随后用原完整Explore证实返程及未改的Grip9准备；完整路线若后段仍失败，不宣称通关。既有Task4ReachSupport实际可能300+3尾段，故不直接借用它的表面300循环；使用短单跳状态段，防止嵌套预算逃逸。一次fresh review，C/I一轮TDD修正，Minor延期。全部原十一项、Main战斗、pre14Handle、正式视听/硬件真人不因返程通过关闭。

无付费/窗口/OS输入/原生崩溃采集/自动化/Git/删除/真实存档写入。误发视频不使用。
