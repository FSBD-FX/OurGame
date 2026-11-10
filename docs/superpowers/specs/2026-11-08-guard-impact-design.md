# 普通成功格挡的双臂接招与回稳

## 范围与判断

原V03/V04/R-C要求攻防有明确发力和收势。当前有举盾／持盾／放盾姿态，但成功Normal正面挡为零伤，不经过Health.Hurt，ShieldResolved未进入正式双臂。此为已核实的制作缺口，不伪称新逻辑bug或开发者已看过坏画面。仍仅无窗口，不接新图／付费／OS输入／真实存档，不改伤害、能量、击退、输入或物理。

采用真实ShieldResolved→Coordinator→Formal的正交表现事件，不复用Hurt（会误带受伤取消/姿态），不只加盾面闪光（不能补上肢承力）。具体3px、25/s是root暂定动作参数，不是用户逐值批准或最终审美。

## 行为

- 仅Guarded=true、DamageAmount=0、SpendHeavyEnergy=false、BrokeGuard=false的正常结果；忽略背击、重挡、不可挡和无效/卸载/暂停来源。
- Formal新增生产接口`PresentGuardImpact(ulong sourceId,ulong transitionId,Vector2 aim)`及真实撤权使用的`ClearGuardImpact()`，只存纯值、无新Resource。按controller ID＋单调transition去重；同帧多个有效挡有界，不由外部伤害数字放大。
- 原PresentationTimeSeconds的正elapsed推进世界空间临界阻尼弹簧；omega25，单次速度增量为`-aim*3*25*e`（单次理想峰3px），总速度长度限255、位移长度限6px，限位处去掉继续向外的速度，避免密集有效攻击无限堆积。重复零时钟发布不能推进或重放。支持的水平翻面不反转已存在的世界位移。
- 将世界offset投到实际近／远臂局部；近臂全量、远臂.55倍，均乘`1-secondaryGripWeight`；先求近腕、保持原武器主握点／副握点顺序和两段24px骨长。不是整体图片缩放、人物根位移或新物理击退。
- 松盾／攻击／爪接管从已显示位移回零；同clock接管不跳。死亡、恢复、源解绑即清零，不让旧事件复活。无PresentationTime的旧兼容输入不得偷偷累计新事件时间。

## 验证与非目标

保存Player的真实Hurtbox.Receive→正常格挡事件→实际ArmWrist/Elbow和原武器握点，对照相同未受击Player；首先实际RED显示没有接招区别。随后左右、30/60/120Hz受控表现时钟、同身份重复／新来源、零时间、回稳、放盾／接管与撤权、HP/根/velocity/骨长原契约。普通pre/post及原rig／models保持严格门和预算。

不以几何通过代签正常速度优雅、正式角色美术、全部动作／R-I/R-C/R-P或全关。下身另一候选是相邻support切换重置摆脚；其源码反例已保存于本轮production，尚需验证当前地形与旧支撑撤权，不能简单删clamp或保留已销毁支撑。
