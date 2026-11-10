# v336 · Bell 地形射线的调用内原生资源释放

状态补记（2026-11-10据已终止原收据核对）：局部生产修补真实RED→GREEN，原十一测试最终9PASS/2FAIL，加Bell专项为10PASS/2FAIL。以下“当前同版回归进度”保留当时中途记录，终态见本补记；不再把已结束任务写成执行中。不是序章交付、资源根因修复或当前v337整版资格。

补记事实：pre `prologue-pre-20261109-043053-382-220afa2e5274406f92f3ac14eb524d06` 原生140474ms、44项全返回、严格PASS；post `prologue-post-20261109-043347-270-ac102ed056ec42c6a15220807743bdc7` 原生190279ms、75项/10suites、retired0、ERROR0、4条已分类预期warning，严格PASS。12案均终态/未超时/同385F源及7EA1产物；原Main899帧Bell未清和ExploreHP前提失败保留。后段全快照/复制的独立最终审计未完成，不凭手动部分检查增加既有审计累计数或称资源已修。原十一项制作、正式动作/声画/硬件/真人继续OPEN。

## 事实、推理与选择

本轮先核对底部Bell实际站位、K弱点与J接触，以及Motor/加减速代码。尚未找到可证明的Bell运动/碰撞缺陷，不擅自改物理、地形、玩家HP、能量或原路线时限。独立确认Bell每次HasClearTerrainPathTo创建临时原生query、exclude backing和返回Dictionary而没有调用内释放。

沿用现有Rust的所有权scope模式，但不照搬其mask、Area或自有collider遍历策略。实际Bell仍是World-only/Bodies=true/Areas=false/HitFromInside=true/排除自身RID，端点及所有攻击/弱点/伤害/数值保持原样。只在原生产私有方法里为三个自己创建的临时对象建立scope；不Dispose借用的World、SpaceState、shape或Tuning，也没有额外GC、等待、缓存、引擎或资源身份模式实验。

测试通过测试内reflection调用实际私有方法，没有新增生产test API。四组（左右×开放/真实墙阻）各64次同步查询，核真实返回、单次返回后ObjectCount的最大净增量≤0、物理/Process计数以及原身份/位置/状态/HP不变。RED的max1只证明该批次存在返回仍净留一个native Object，不把它外推为所有调用都留1或无界累计增长。ObjectCount直接覆盖native Object，不直接计Array/Dictionary backing，容器释放由scope源码审查支撑。测试批无新增显式GC/额外帧，不宣称没有自动GC，也不能冒充正常视听或自然整图。

## 实际证据

根目录：`G:/knight/验证证据/october13-stable-claw/`。

| 案例 | native结果 | 原生证据 |
| --- | --- | --- |
| resident-heavy-default-20261109-041157-133-5ccb31fa31ce4ab8b6e489fef0ca15e4 | exit1，995ms，严格FAIL | 开放右侧64次调用，max_retained_delta=1，精确BELL_SIGHT_QUERY_OWNERSHIP_RED，非编译/fixture前提失败 |
| resident-heavy-default-20261109-041322-312-7c73cf2882d24b7fa1eaba698962e6af | exit0，19769ms，严格PASS | 四组共256调用max0；全部旧Sight/armor/claw兼容完成，stderr/ERROR/WARNING0 |

GREEN中的四个原生伤害控制均先证明Active Area确有真实重叠；左右无遮挡各连接一次/伤害一次/HP5→4，隔墙各零连接/零伤害/HP保持5。原已承诺AttackId/Active→Recovery、丢失LOS不取消、固体平台与墙阻、filtered retry/默认hitbox行为均保留，不用对象计数替代实际碰撞证明。

独立审计两案共500快照对0差异/12份binary复制匹配；RED与GREEN源/产物不同，当前基线只计GREEN的250对/6复制。当前1927源清单SHA`385F1118C660A7CC8CAF0C2FED7DFDC3FB360639DC6F16DA1A8B520A762A9DE2`、六产物清单SHA`7EA1E7D18FA0223E83AE8982CC7BE5A45E2A720FB25782B58728D6FCC1E378DA`。109保护行/104实际文件及helper案内稳定。

## 当前同版回归进度

不更改旧路线、HP前提、输入策略、物理或验收标准。当前baseBell及原十一测试中的九个已结束，10案8PASS/2FAIL；独立累计2577快照对0差异/60份binary复制匹配。RED及旧v335不混入当前累计。

| 阶段 | native ms | 严格结果 |
| --- | --- | --- |
| base Bell | 19769 | PASS，上述完整专项 |
| models | 509 | PASS |
| Explore | 205040 | FAIL，原HP1入口检查 |
| rig | 38396 | PASS |
| presentation | 18486 | PASS |
| gait | 29866 | PASS |
| recovery | 14940 | PASS |
| actions | 10783 | PASS |
| save | 11427 | PASS，另独立cold1488 |
| Main | 94487 | FAIL，原899帧Bell未清场 |
| pre | 待终态 | 未定 |
| post | 待执行 | 未运行 |

save独立writer37796/reader40408、6只读witnesses、straight/Arc各4坏输入与精确恢复齐全，stderr0。Explore在首次上行entry要求HP1，实际同PlayerHP2/恢复0/0/dash0，尚未发出该跳跃；本次没有修或放宽这一前提，亦无最终route JSON。Main仍在原899帧gear-pistons底部Bell未击败：敌HP4、PlayerHP1、能量.095、7起手/5连接/3爪，缺最终JSON；不是超时/资源错误，也不能把局部query修补当作战斗问题已修。两条完整路线不签Boss/出口完成。

## 尚未证明

仅局部确定性资源生命周期得以修正。没有证据称本项导致或修复GCHandle/FATAL/ShapeRID、底部多敌战斗失败、Explore旧HP1入口或整个序章。没有为了通过去改HP前提、路线、能量、预算、怪物或验收标准；也不因新源变更重跑无关melee/Bone/Ghost筛绿。

一次fresh两文件审查已完成C0/I0/M0，两pin独立核实，无补修/二审；全部四条不作判断边界已在[review package](../../.superpowers/sdd/2026-11-09-bell-query-ownership/review-package.md)裁定仍OPEN。当前同版原十一测试、保存独立cold及前后段资源门已开始顺序执行；任何缺证/失败仍失败。正常速度声画/UI/相机、正式分层美术/优美连续动作、目标硬件/真人认可与全76仍OPEN。源/产物/保护数据/助手/审查pin和媒体容量须在真正终态核对，身份一致不等于游戏完整。

仅无窗口、无新美术/媒体/付费/GUI/OS输入/额外采集/自动化/Git写入/删除/真实存档实验。完整Goal不缩减、不提前完成。
