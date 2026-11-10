# 玩家外力入口 · 只读契约核对

状态：当前源码勘察，不是已实施的物理升级或运行/观感验收。服务于用户R-P要求，不新增用户世界设定。

## 事实

PlayerMotor.ExternalImpulse当前是替换目标速度，不是按质量积分的冲量。直接调用链均依赖该语义：

| 调用 | 原契约 |
| --- | --- |
| PlayerCombat.OnHitboxConnected | 下劈命中用(当前横速, −DownBounceSpeed)，默认360；保留横向速度、重新设上弹，并重置落差跟踪。 |
| ThreadTarget2D.ApplyPull fallback | 无interaction接管且非轻敌receiver时，向目标的单位方向×PlayerPullSpeed，默认720；旧Thread在每cast进入Apply时一次执行。 |
| CanyonSpringboard→ApplyEnvironmentalLaunch | 初始化快照LaunchVelocity，默认(−620,−480)；先检查authority/取消爪、替换发射速度并保护向上段。Ready已拒绝非有限值及100…1200之外长度。 |

HealthComponent发布Direction.Normalized×Knockback向量，但当前玩家订阅者用于Hurt表现、取消爪/旧Thread、HUD，没有把该伤害向量接给Motor。不能把伤害日志中的Impulse字段当成实际速度已叠加，也不能将ExternalImpulse这些真实调用写成玩家受伤击退入口。蒸汽的Up×180伤害请求、固定齿轮Knockback0同样不等于已施加发射。

## 推理、边界与风险

不能统一把ExternalImpulse改成+=来声称更真实：旧下落/推进/摆荡速度会被叠入，破坏定速牵引、下劈上弹和弹板的replacement/authority契约。质量、力、冲量和游戏距离单位尚未标定；名称不能代替契约。

已有独立控制包括ThreadInteractionTests配置620的定向速度/接管不移动，PlayerTraversalInputTests下劈真实反弹但不补空中dash，CanyonMechanismRuntimeTests旧弹板不覆盖新authority与中断真实burn，ThrusterPlayerRuntimeTests旧pendingboost不能覆盖/叠入replacement；Vector2.Zero也用于合法停止/换authority，不能新增任意非零限制或速度上限。

两个后续入口分开保留：

1. 有限值输入的低风险保护：ExternalImpulse本身目前无NaN/Infinity前置拒绝，可沿用现有Finite判定，在任何velocity/revision/证据/dash/pulse变更前拒绝；合法零和非零replacement全部保留。必须先实际RED并检查所有相关状态无变更，不能只看抛异常。
2. 玩家受伤的可控外力：先明确采用delta-velocity还是replacement、护盾/无敌/地空/爪/推进交接、反向制动与恢复、重复ID去重和落伤来源，再设计接线与关卡回归。当前尚未定义完整行为契约，不在本次默默接伤害向量或新增质量参数。

来源：scripts/player/PlayerMotor.cs:484/500、PlayerCombat.cs:108、scripts/abilities/ThreadTarget2D.cs:45与ThreadAbility.cs:306、scripts/mechanisms/CanyonSpringboard.cs:100、scripts/combat/HealthComponent.cs:68，以及上述直接调用/测试只读核对。本次没有生产变更、native运行或新物理完成声明；正常速度惯性、重量感、完整动作及原十一项仍OPEN。
