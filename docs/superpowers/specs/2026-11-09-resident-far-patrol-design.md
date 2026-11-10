# 游荡者远距可见停巡修补设计 · v333

原完整十一项ACTIVE。v332为实际进展：双时序记录原生执行、同版11案10PASS/1pre句柄FAIL，并发现本候选；不重跑该版找绿。只在G:/knight/Knight无窗口制作。

## 事实、推理与风险

RustShellSquire把LOS/同territory当seesPlayer；combat模型另用水平距离360决定Idle/Chase，patrol模型却直接因TargetPermitted进入Engaged。无其他力/Returning时可出现combat Idle、residence Engaged、方向0的停巡。原生012035已复现远距120步停巡；014026真实D撤离248步仍Engaged且零恢复巡逻，证明脱离追击同样受影响。

用户要求固定区域自主游荡。选择原生反例后仅协调patrol感知，不改变360、70攻击距离、48巡逻速度、95追击速度、警告/伤害/物理/数量。seesPlayer与_residentPermitted原域/LOS授权及combat Step保持，以保留已起手攻击在仅退远时完成的旧合同。

不选扩大检测范围或取消同域LOS授权，改变难度与攻击撤权；不选让所有居民始终追踪，也不借直接改AI状态或原生位置制造通过。这是持续制作授权下开发判断，不是用户批准新玩法或最终视觉。

## 原生反例与对照

独立World2D受控夹具：读取实际作者`low-ceiling-bridge.01`及DevelopmentV001真实rock-wanderer，通过已有binding/Profile admission。home(666,604)、territory[0,0,1280,720]、巡逻666→602→730→666不改。保存Entrance的实际`BridgeFarBank` body原transform/layer/880×84/top636不改。真实Player.tscn仅入树前设(1120,600)。入树后不写角色/敌人pose、velocity、HP或AI。

先核原生接地、同world、全树唯一真实Player、实际LOS、始终>360、原scene/profile/home与健康/零恢复，再断言至少120个native post-MoveAndSlide样本有>40px连续巡逻。旧实现应完整观察后因停巡FAIL，而不是因前置条件失败。增加真实撤离：Player从900以现有引擎键盘D移至至少1180后释放，持续180步；原居民必须从Engaged恢复Patrolling、经过作者home并继续向左，全身从未出territory所以不伪造Returning。近处Player(900,600)独立验证追击；贴近Player(720,600)独立验证原前摇/Active及真实Hitbox连接造成HP下降；未配置旧wave远处应仍Idle，不被新巡逻化。五案分别验收，不以贴近命中替代追击。键盘夹具不冒充OS输入或真人试玩。

实测修订依据：首个生产修补后far已移动，但900目标被追至约69.33距离反复起手而未命中。保存伤害矩形宽26、中心前移34、Player hurtbox半径20的水平极限约67，小于70起手范围。原“900静止目标必被命中”的测试前提未经证实，故拆开追击与明确可触及的贴近命中；真实空挥边界独立留作E08候选，未删除/冒称修复，不在本轮擅改攻击参数或碰撞形状。

共享场景shape/Profile/PackedScene全部借用，只Free自有实例与Dispose自建World2D；清理逐项执行并保留主错。无GC/cache/引擎或新增崩溃采集。独立夹具不证明manager自动出生/lease、全76行为、自然路线或最终美术。

## 验证范围

新独立phase`resident-patrol-range`原55秒cap、普通严格日志/必需唯一完成回执；原resident-patrol aggregate和原11指定phase+独立cold同版资格均保留。原native/模型near、LOS及territory撤权控制不能删除。只改patrol输入若产生新的未预期衔接，先查因，不削弱原门/改血/关敌。

一次fresh全增量审查、C/I一次因果TDD修补、Minor延期；当前所有资源错误、旧稀疏发布与原正式声画/硬件真人/美术/全部十一项不由此关闭。无GUI/OS/付费/新美术/自动化/Git/worktree/删除/真实存档实验。
