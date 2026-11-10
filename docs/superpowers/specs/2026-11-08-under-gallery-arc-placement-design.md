# 工坊横廊下的弧摆齿轮接入设计

状态：v331已由共享authoring生成并接入正式保存Basin，真实局部通行、导出及一次独立审查已有证据；同版20指定回归已终结为15PASS/5FAIL，探索线及其自然Arc通行通过，主线落脚采样不足、候选和前段终结器崩溃及两项元数据资源泄漏均保留，不能算整项完成。原完整十一项/T03仍OPEN。选择与参数是持续制作授权下的开发判断，不冒称用户已批准最终观感。

## 事实与目的

v330已提供独立`CanyonArcGear`内核，不等于正式关卡有新内容。保存的gallery顶面上方没有顶棚；`UpperGearGalleryTwo`实体world rect(3280,3676,240,40)的下表面y3716可承载支点。下层原`ShortcutBasinWalkway`顶部4116，正常Explore的原`Task4WalkTo(3800,270)`会从下经过；主线上层路线不改。当前正式straight是一个actor及其Collision，不是两个齿轮；入口另有两个fixed。

要交付的是工坊层一处真实悬吊运动：角色可低走通过，较高跳跃可能进入危险弧，保持上下层路线差异和可退路。它不是主线强制等待挑战，不以“看见一个装饰”冒称复杂度提升。正常可读性/美感仍需后续实际画面。

## 方案取舍

- 选：现有gallery2下表面挂独立Arc，不动地形/居民/旧straight。可维护真实支点、下层步行备选与上层避让；代价是主要约束下层跳跃，而非主线必经门。
- 不选：gallery顶面上方凭空悬吊，缺真实顶棚；另造天花板会改变当前通路/抓取与视觉范围。
- 不选：直接替换原straight或旧Pendulum；会回退已验证内容，也不能把既有对象重命名当新增机关。

## 精确保存配置

独立节点`CanyonBasin/Gameplay/Mechanisms/UpperGalleryUnderArcGear`，类型`CanyonArcGear`，一个同名约定`Collision`子节点，identity局部圆、r24、无新增solid/grapple proxy。

| 项 | 值 |
|---|---|
| Basin local起点 | (2040,356) |
| world起点 / pivot | (3320,3956) / (3400,3716) |
| PivotOffset | (80,-240) |
| SwingRadians / PeriodSeconds | -0.6435011 / 3.2 |
| Radius / Damage / ContactIntervalSeconds | 24 / 2 / .5 |
| 全未来危险包络（近似阅读值） | x3296..3504，y3932..3992.9822 |

实际数据由同一弧模型与几何导出，表中近似值不是窄相伤害epsilon。默认Damage5与满血5冲突，必须显式2；不改角色/其他机关数值。电机周期驱动，不标自由重力钟摆。

## 保存与消费边界

同一新增authoring操作供`BuildBasin`及独立候选叶复用。候选必须从当前保存叶实例增加该节点，保存/重载/节点数检查；不能完整重建旧leaf/backdrop或父UI。v314已实证full factory会把既有背景宽3840改5120。父原本外部引用Basin、无新增solid/terrain catalog变化则保持父文件字节，复测原父依赖而不强行重写。

只为独立Arc加精确存档donor路径`Gameplay/Mechanisms/UpperGalleryUnderArcGear/Collision`、Circle/r24，Basin61→62；保留身份/借用排除/所有旧坏输入控制。terrain158、encounter26、resident50、fragment9、straight1、Entrancefixed2都保持；不自动改存档schema/地图revision或写真实存档。

inventory已Arc先Fixed，fog基类当前圆已接；必须实测保存实例，不再加重复分支。离线配置图需独立`configuredArcGears`集合、真实pivot/起点/signed angle/周期/radius/damage/cadence/全弧包络与source hashes；不得用Ready-only属性、端点弦或AABB假作伤害形状。objects56→57、visual4/traversal21不变、logic31→32，旧straight记录仍1。只保全这个关键地图节点的少量输出，不采集普通调试视频。

## 原生资格

保存/生成缺节点应先真正RED；候选父图可只替换对应Basin definition，保留当前父图与其余依赖。晋升叶前核对祖先非reparse及当前before哈希，保留旧叶原件；使用生成产物而非手写tscn。

受控局部夹具与自然整线严格分开。Player起点只可在入树前设，入树后不能写位置/速度/HP/齿轮clock。局部隔离必须列出实际居民/encounter及伤害碰撞被关闭的名单，不能只停AI后忽略身体/攻击Area，更不能用隔离GREEN冒充正常Explore。起步前已损HP就失败，不补血。

实现实测补充：这条walkway另有原`SteamJet`，真实world中心(3460,4110)，不能误把阀门control的(3160,4040)当喷口位置。自然Explore按原流程先真实W+K关阀再经过；局部齿轮夹具只显式`DisableForRun`这一喷口并逐帧核对，保留其他机关/原地形。所谓低走安全仅指原阀门已关后的齿轮通行，不声称未解阀门时整条走廊无伤，也不在生产里取消蒸汽。

- 多相位双向A/D原走廊：自然等起步相位0/.25/.5/.75的窄带，并记录实际跨中点与回程相位；完整胶囊真实支撑、camera、同Player、HP满、accepted伤害0、恢复0、普通跳/喷/爪边沿0。相位覆盖以实际到达为准。
- 中心world(3400,4080)一次真实Space持跳：两个运动方向自然相位候选.15/.65，实际接受恰一次普通跳；观察真实Health已接受身份对应Arc runtimeID/LocalAttackID、2点伤及原walkway重新支撑/恢复0。短tap单物理采样另作独立对照；不将连续估算94px高度或51px余量当原生证据，不改iframe/cadence凑次数。
- 真实保存controller的雾、遮挡、暂停/卸载/重挂及完整未来占用，不扩大camera或安全区域；相对Player CCD仍不因此补齐。
- 普通Main/Explore、收集后回营再来、W+K阀门、原gallery爪及两种升降交接都保持旧门；自然Explore保留所有居民/旧机关，至少被动记跨下层实际相位/HP/伤害source，不加传送、跳喷或预算绕过。受控坏时机与自然整线可以分别过/败，不能互相替代。

## 风险

最邻resident占用距危险包络约35px、站立头顶余量约51px；静态AABB无相交不覆盖战斗追击/击退/动画或实际跳跃。K是否穿弧尚无证据，不承诺。原完整十一项、正常AV/美术/硬件真人/历史资源根因及全部旧Minor不缩减。仍只headless，禁止GUI/OS输入/付费新图/自动化/Git/删除/真档实验/GC缓存改动/额外崩溃采集。
