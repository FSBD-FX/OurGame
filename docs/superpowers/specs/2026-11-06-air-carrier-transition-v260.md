# v260 同态空中姿态模板交接修复

## 判断

事实：正式 PlayerFormalVisual 按 Locomotion.State 变化建立过渡，LocomotionTarget 又会在有效 OrdinaryAir 的四个空中状态撤下旧 Body/NearLeg/FarLeg 模板；同为 Fall 的 AirJet→OrdinaryAir 因此可能直接换腿角。旧 RunDraw 只验局部脚底，不能排除世界脚底跳变。用户 U-20261105-01 暂缺 V1 原图，后续重审现有稿，不继承旧批准。

推理：必须将“实际采用普通空中模板”作为姿态过渡边界；捕获原 locomotion 层，不捕获最终混合动作/二级/喷射载荷，避免重复叠加。

风险：postmix 非零 AirJet 躯干偏转/骨盆偏移撤销、根倾角和动作变化是另外的通道；本修复不能自动证明它们连续，更不能代表正式美术、设计感和物理手感已经合格。

## 目标与约束

- 同为 JumpStart/Rise/Apex/Fall 时，普通空中模板的有效进入/退出也从上次显示的 locomotion 姿态进入既有 2/15 秒过渡。
- 零时间/重复/回退时间不推进；第一帧、显式 cue reset、旧无时间兼容保持原合同。invalid、Grounded、AirJet 优先、ThreadLatch/Apply 和非四态不得误认模板已接管。
- 仍以未叠加 action/secondary/load 的 _locomotionPose 为过渡起点；不改输入、Motor、重力/速度/单跳/碰撞/战斗窗口、素材或资源生命周期策略。
- 使用真实保存 Formal 节点与真实 Player 内部键盘（无 OS 输入）验证。左右朝向、中性零时间几何以及自然非零短喷到期分开报告；18+18 刚性腿、握持与根不被表现层移动。
- 当前源集 1898，HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6；原地脏树保留。无 GUI、付费、采集、新自动化、引擎/GC/cache 策略、真实存档、Git 写入或删除。
- 完整原十一项 ACTIVE；既有 v259 pre/post/REF 资源失败不因本修复关闭。正常速度美术、全 76 遭遇、硬件和真人验收仍 OPEN。

## 验证边界

中性相同时间输入在没有新增载荷时应保留实际节点角与世界 ankle/sole（.002 px 数值容差），正时间最终到达原目标，不通过拉伸/夹紧掩盖。自然非零交接记录逐帧世界脚底与身体位移，不用任意“美感阈值”替代正常速度审阅，也不称完整非零通道已经修复。
