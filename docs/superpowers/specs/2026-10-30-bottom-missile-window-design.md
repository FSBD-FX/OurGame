# Bottom return missile window

v174真实Explore已完成战后返Gallery与Grip9；底部左腿11539被resident precision-missile steam-valves.05 attack2杀HP1→0，E1/盾off。原日志有预警11391、发射11469、起跳11536，但缺实际弹体/瞬时爆炸/退休位置。Bottom Leg只有A/D/Space，原KeyboardSource禁止ShieldHeld，L守向与Steer可能冲突，不能直接放宽该合同或凭空改数值。

先在原完整路线补**被动、有界**观察：原Bottom Tick前采样，初始已挂载Missile actor及其后新增弹体的真实事件、实际授权leaf、圆形几何与完整Hurtbox间隙；补齐ShotEmitted/ExplosionResolved/Retired瞬时边界。另保留原Basin steam encounter＋所有Basin resident的danger消费者结果，明确不冒称遍历全世界所有未来源。只读getter/事件，不能发输入、改HP/AI/弹体/时钟/睡眠/父节点；不持有leaf跨await，回调故障只记诊断错误，不替换原失败。最多512标量行，实际来源与geometry-valid分开，Infinity不得解释安全。

这一步不证明安全窗口存在。根据真实轨迹再选择合法现有策略：优先在原120flight内共享等待/飞行期限择机，不改变120/12、两次普通跳、两次K应用与Left→Right→Left切换顺序；若必须移动举盾，需独立证明方向/输入与原guard合同的等价保护，不能只删!ShieldHeld。不得用过时本体Recovery代替实际弹体退休，不通过Frame编码/硬编码旧physics或attack2来躲一次。

原v174真实死亡为行为RED；观察阶段预期原输入不变及原失败保留。若证据足以支持一个小策略，再用原完整Explore验证实际合法生存/支撑/无复位；否则据实保留未证实边界。唯一fresh review在本slice最终实现后，C/I一轮TDD，Minor延期。原Main/pre资源/全部十一项/正式视听硬件真人继续开放，无窗口OS/付费/新原生采集/自动化/Git/删除/真实存档写入。

## 已证事实与实现裁定

165556被动案同11539复现死亡：167行/0drop/0fault，flight圈8，撞地后在(3902.5906,4107.997)实际blast圈56、授权true、完整Hurtbox间隙0，然后damage/retire。原输入确实未变。

重要更正：KeyboardSource的!ShieldHeld只在Neutral/NeutralRight及control中使用，非flight全程Safety规则；原断言一个也不删。首个grounded launch保持原A/D+Space，确认后才允许仅针对原Basin扫描选出的真实授权近Missile，在批准150deg锥内选Up/Down+L（横向中性，不强制反向D），离开真实危险或落地即松L，由原12settle证明fresh neutral。未预测未来无害，不等待或重置120。

几何意图只是动作规划：有效圆间隙0..两倍真实Hurtbox高度、有限非零sourceDelta、符合PlayerShieldSettings.Approved锥才给纵向意图。实际source须同ActorRoot/world、alive、Missile，且原AppendAuthorized叶为活projectile/匹配danger attack/当前Circle。缺失/无效/Infinity不给意图。该距离是保守反应启发式，不是新伤害半径或安全保证；其他源仍保留。实际盾能否成功、短时横向中性后是否仍能落台，都必须由完整原生证明。

172922完整实测证实原Down+L在blast56时真实挡下damage0/HP1/recovery0/0，但第一接触退出Leg后x距目标3.9524超过原±3，原Settle不做位置修正。接触与站位须区分：允许在同一原Leg120内复用已有地面中性停点预测来达到原±3/≤2，再由未改的原12settle证明松键与完整支撑。无新增等待额度、数值、跳跃或恢复；这是同一盾响应的落台整合修复，不是允许放宽验收。
