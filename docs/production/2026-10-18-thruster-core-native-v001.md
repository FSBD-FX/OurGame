# 短喷模块与动力学核心 — 原生组件证据 v001

日期：2026-10-18，Asia/Shanghai。状态：**组件可进入Player接入；完整序章NEEDS_WORK**。用户“设立目标并完成1–11”的工具目标ACTIVE，范围未缩减，自动化不恢复。

## 事实

新增不可变移动模块／实际Resource验证与纯速度积分核心；尚未接入保存Player、Motor、UI或实际推进器效果。当前可玩角色仍旧Dash，不能把此报告当短喷试玩交付。

独立新runner无drain，全部实验由root自有原生owner串行运行；未抢焦点、未发送OS按键、未语义读取真实用户存档，无付费生成／Git提交／推送／发布。

| Fresh检查 | engine退出／耗时 | 结论 |
| --- | --- | --- |
| module | 0／663ms | Resource快照不热变，命名非法配置拒绝，自有资源释放 |
| dynamics | 0／668ms | 有限脉冲、继承余速、归一化、重力、阻力、互斥控制、反向仲裁、非法输入和时间收敛 |
| gravity-delta | 0／574ms | 旧重力贡献回归 |
| keyboard | 0／9837ms | 旧真实Godot键盘链回归，非真人 |
| actions | 0／7341ms | 旧动作／护盾回归 |
| prologue-pre | 1／90719ms | 旧Upper路线失败＋两个无效profile负例警告，严格FAIL |
| prologue-post | 1／8440ms | 旧Bell落地／恢复父链失败，严格FAIL |

每案fresh Rebuild成功、0警告/错误、1681源库存、`guardFailures=[]`；所有raw／source／helper／binary／用户库存保护保留在`E:/OurGame/Knight/.evidence/october13-stable-claw`。精确case与逐步RED→GREEN见[执行台账](../../.superpowers/sdd/2026-10-18-thruster-module-and-dynamics/progress.md)。新两phase不新增drain；旧phase既有退出流程不变。其它三资源失败未重跑，不关闭。

30/60/120Hz对独立连续拖曳＋有限喷力参考的末速误差为6.022736／3.104965／1.554932px/s，随步长减小。无阻力有限喷力总量1008px/s、燃烧0.18s亦逐Hz验证。不是解析轨迹、真实碰撞或显示FPS证明。制动已有0.1s→300及0.25s→−75单步负控，多Hz未饱和点覆盖随下一Player计划补。

## 推理与风险

一次独立fresh review未发现本组件阻断产品问题；明确要求真实Player接入后另验预算、爪、风、来源、碰撞、落地、换装与表现。GravityVelocityDelta包含无喷力基线的终速修正，ThrustVelocityDelta是有／无喷力裁剪后差值，不是长期速度来源账户；后续消费者必须按此合同使用。

旧可玩路径源码及正式地图哈希未变；广域失败与之前同类，不能归因于尚未接入的核心，也不能凭此关闭历史问题。Bell夹具非法初始位置和缺正式恢复父链已有静态证据，尚需有区分力原生验证，不擅改生产门或删除KillZone。

完整地图、新50常驻（正式仍0/50）、24正式子型、机关、最终视听、同版正常Main／Explore、旧档、真人和ZIP／GitHub交付仍全部按[总待办](2026-10-18-project-remaining-backlog-v001.md)推进。所有展示媒体仍须先归档；本组件阶段没有生成或展示新媒体。
