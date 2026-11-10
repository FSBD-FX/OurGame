# 竖直落地的分段披风惯性修复

沿用已批准2026-08-17完整制作规格2.2（连续布料／落地余振，禁止仅整图拉伸旋转）、原V03/V04和用户判断后持续推进授权。不是新角色、原画或审美方案；系既有链路bugfix，实现系数由开发选择，不再次索要相同范围批准。

事实：真实Timeline已有速度、面对、grounded边沿与landingSpeed/650的LandingImpulse。正式rig会做SoftLand/HardLand整层姿势；现有SecondaryMotionModel披风却只读X/turn，因此纯竖直起落的分段Map始终Identity。不得夸称披风完全不动。

取舍：不新增布料物理世界、不整层拉伸、不新建逐帧贴图；采用现有有界临界阻尼Y通道，接入竖直速度的相对拖拽及原单帧落地信号。X/角度驱动、spring频率、7px纵向/12px横向及.42弧度上限不变。采用速度比例.006、落地比例7为开发初值，落地后自然衰减；不添加新的计时器、逐帧状态或Resource。

验证用真实PlayerPresentationTimeline和借用保存Player的Tuning；挂载真实Formal叶，观察实际MapMantlePoint而非整层矩阵。两朝向×软/硬落地，纯竖直帧后下摆非零余振、上沿固定、落地信号相对同速度airborne控制确实生效，300静止帧后收敛；原旧两组Draw/映射/镜像/动作归属不删。无窗口几何通过不等正常速度或像素、用户认可。

边界：G:/knight/Knight、1857/897，不改玩法、键位、落伤、Tuning、敌人优先级、资源寿命、十slot/资产；只改模型与既有测试，canonical仅补模型源实体选拷。禁止付费生成、前台/系统输入、再次ProcDump、Git写入、删除或真实存档操作。原十一项目标active、历史FATAL和完整双路失败保留。
