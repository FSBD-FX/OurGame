# T01 · 大型熔炉／销毁坑当前源码缺口

2026-11-05只读核对；没有制作、构建、运行、改地图、美术生成或新增授权。不是按旧待办直接猜未实现。

## 事实

1. 当前未找到专用大型熔炉／销毁坑的运行节点、伤害区或有效生成器。现行`KillZone.OnBodyEntered`只请求FALL RESET，不扣HP/触发真正死亡，不能计为T01已完成。见`scripts/world/KillZone.cs:25`。
2. 当前真正生成链是`PrologueCanyonBuildParts.Build → CanyonContentBuilder.Build → AddFallRecovery`；不能修改遗留旧布局方法后声称改了实际关卡。见`tools/build/prologue/PrologueCanyonBuildParts.cs:40`。
3. 入口StartFloor与BridgeFarBank之间现有X320–400/Y636的80px缝，不能冒充“大型”。此次未核实到能容纳大型坑且不切断路线的现有区域，大型候选坐标未知。见`tools/build/prologue/CanyonContentBuilder.cs:272`。
4. 真实伤害`HealthComponent.ReceiveDamage(DamageRequest)`到HP0才Died，且会拒绝无敌期/重复attackID/已死亡请求；不能只送一次便假定必死，不能绕为普通恢复。现有`PrologueLevelController.OnPlayerDied`接DEATH RESET。见`scripts/combat/HealthComponent.cs:59`。
5. 正常玩家证明已有模板：菜单开始、原玩家真实键盘到达，观测Damaged/Died/RecoveryRequested/Completed，核对HP0/同一玩家/合法检查点/满血恢复/可继续操作，不注入伤害、改HP或传送。见`tests/integration/PrologueCanyonGeneratedMainRouteTests.EarlyDeath.cs:42`。

## 推理与下一生产增量

T01仍是原十一项中的必做内容，不因技术回归17PASS或普通Fall Reset存在就勾完成。需要先核实足够大的空间和正常避险路线，再做明确可见的销毁区及真实一次死亡/恢复链。小缝机制试件至多是原型，不作为大型陷阱的替代终态。

必要native证据：正常标题/键盘路线首次成功避开并继续；主动进入由真实区域伤害导致一次HP0/Died/DEATH RESET/恢复；同一玩家复活后可再通行；原FALL RESET和安全路线不回归。须另外核对迷雾下边界与警告、无敌帧/重复进入/多个碰撞形、检查点前后和正式视听，不预设无条件绕过无敌帧为已批准规则。

## 风险／范围

地图选址仍缺，不擅定新大型坐标、切断现路线、以普通恢复假装死亡或把80px缝交差。此为只读覆盖核对，不承诺当前已实装，不新增付费、窗口/系统输入、媒体、自动化、Git或外发授权。
