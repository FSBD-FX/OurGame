# v244 · 地面挥刃主动肘腕与实际双手握持

状态：本轮同源十五门已全部终态，12严格PASS/3FAIL；肘腕生产增量专项及一次审查通过，完整序章仍未验收。不是Git tag或正式发行。

## 本轮真正修改了什么

- 一个生产CS：[PlayerFormalVisual](../../scripts/presentation/PlayerFormalVisual.cs)。从原固定近腕(0,21)改为权威GroundAttack四阶段的内收、伸出、经过与回拢；复用既有presentation elapsed，以有界指数跟踪接续，不新增攻击/输入/物理计时。其他动作连续收回，死亡、恢复和显式reset清除旧负载。
- 先求解实际近腕，再将原图主握点绑定该手，最后求解副握持；不是只旋转整条手臂，也不是整图拉伸。原十槽、24+24链、腿/支撑、PNG字节/等比尺度、Combat/Motor/Hitbox/数值/输入均未改。
- 两个测试CS：[实际Draw与时间线](../../tests/integration/PlayerFormalVisualTests.cs)、[保存Player真实J](../../tests/integration/ThrusterPresentationRuntimeTests.cs)。旧握点断言从硬编码静止点改为实际腕点，同时新增独立形变反例，保留原图哈希、双手接触、固定链、原32连续预算。不是仅放宽期待值。

[设计](../superpowers/specs/2026-11-04-ground-attack-arm-design.md)、[计划](../superpowers/plans/2026-11-04-ground-attack-arm.md)、[过程账](../../.superpowers/sdd/2026-11-04-ground-attack-arm/progress.md)。以下数值是开发候选的技术证据，不是用户认可的优雅或最终幅度。

## 实际RED→GREEN

证据根统一为`G:/knight/验证证据/october13-stable-claw/`，下列名称均为该根下的完整case目录。

最初`thruster-rig-compatibility-20261104-182431-425-df6677938d5f4cc0be94f51cb6f45f91`与`thruster-presentation-rig-20261104-182509-704-bd05bd93d2ad43bcbf2b44597fedf9a1`因新测试对只读Phase赋值的CS0200失败；root改公开构造器后才继续，二者不计行为RED。

真正RED为`thruster-rig-compatibility-20261104-182645-982-96b301fd99fd469c949feaf8dbf2c8d7`与`thruster-presentation-rig-20261104-182738-920-7b44a2f76cf34d039efc1e6a36c4bd8e`：分别在真实Timeline和保存Player/J中观察到腕距min21/max21、肘移动0，四阶段到达但新关节要求失败。两者结束并确认无owned native/build后才改生产。

GREEN技术结果：

- 真实Timeline 12组/1075样本，左右×水平/Up×30/60/120采样；最小腕距18.94–19.07、最大23.68–23.74、肘位移2.73–2.95。不同采样率运动成立，held目标同elapsed等价、零时间冻结、五种接管、终态/reset均通过。不是动态目标逐时刻完全等价或硬件FPS证明。
- 原378副握持样本maxgap约0.0000078642、最大required reach43.405693，小于原48臂长；原32连续门max28.994555不扩。
- 保存Player实际J：左右各44个攻击发布样本，min约19.0636/max23.664、肘位移约2.747、双手maxgap约0.00000853；两臂固定、未缩放，攻击1/命中0、root不漂、HP不变、无跳/短喷且最终回稳。root不漂不单独等于足底绘制接触，原步态专项另验。

## 当前同版十五门

| 完整case目录名 | engine ms | 严格结果 |
| --- | ---: | --- |
| thruster-rig-compatibility-20261104-182940-089-70cbfbff36114cb48cd698bee4cb7cbd | 1480 | PASS，原11formal叶＋2mantle及新控制 |
| thruster-presentation-rig-20261104-183025-023-f1e5603410bf412093718f78a71e4bf9 | 18703 | PASS，真实J与旧完整rig专项 |
| thruster-ground-gait-20261104-183215-202-c21d63df898b41b8bd61940a82452d60 | 13897 | PASS |
| actions-20261104-183315-451-33cdee3304ac4126892d522b2f987224 | 9640 | PASS |
| thruster-formal-claw-phases-20261104-183407-642-8825b13cd8174a56accb12b56d492966 | 5101 | PASS |
| thruster-recovery-compatibility-20261104-183456-961-2c241d1d936b40138cd29188ff5d477c | 13417 | PASS |
| thruster-recovery-feedback-20261104-183727-902-bb2a1ed6b14647b4bc7aac6cc9dcd8c1 | 4261 | PASS |
| prologue-pre-20261104-183816-585-4dd628f88abd4e39bc4ad86677acf668 | 180138 | FAIL，原180s超时 |
| prologue-post-20261104-184203-460-a6db9163b2d6448eabce00ba94593b2e | 203654 | PASS，原300s |
| resident-geometry-20261104-184610-762-e47fbe17723c4554b6073bf060c6d7f8 | 55087 | FAIL，原55s超时 |
| resident-save-compatibility-20261104-184808-597-57adea5e5eba41bdb1171ab602540bb5 | 23940 | PASS |
| models-20261104-184955-763-07ff6223b66c441bb2080efac1a34c86 | 987 | PASS，仅原爪模型/到达几何子集 |
| keyboard-20261104-185105-024-5f2fa0d200ce437ca4a79ddd64c799b0 | 22349 | PASS，另独立标题10040ms/exit0 |
| thruster-route-main-diagnostic-20261104-185238-005-462e5614b6ea43299ca673cba758a93b | 209874 | FAIL，原899配重余敌1 |
| thruster-route-exploration-diagnostic-20261104-185803-119-90e5fd4c608743ad9da314389a993247 | 435410 | PASS，经Boss/实体支撑出口，恢复0/0 |

除post四条原限定负控WARNING外，PASS案stderr均空；四条为故意抛错ClawFrameChanged观察器隔离、VisualScene非Node2D、两次Completed→Recovering拒绝，没有忽略额外ERROR。pre最后为BellChainMechanism到达采样.5，geometry已打印1687corridor census、新208差分及实际leaf边界回执，但都未在原限时完整结束，不能认定整门通过。本轮没有重跑筛绿/扩cap；未出现FATAL不等于历史匿名finalizer已修。

Main仍RustHP1、playerHP2、7次J均started/connected、3次爪，末gap18.279；[独立只读线索](2026-11-04-current-dual-route-lift-boundary.md)指向消费者双攻击储备24挡住一次费用12的保护窗口。随后所需防御储备未证，不能直接24→12。本次没有改该driver或游戏数值。

Explore真实JSON为automated、syntheticFixture=false、humanValidated/operationCoverageComplete/fullGoalAccepted均false。从title、同player47563408900，23次节点访问/11种挑战、11敌群激活10敌群击败，**不含counterweight击败**，不能当全敌群/全76实战证明。Boss28328060783156由实际攻击99致死，death/arena/completion事件与completionCount各1；Flow Completed、恢复请求/完成0/0。

出口实体floor(6080,1116,1600,40)支撑完整40×72胶囊；按D再保持60输入帧，位置(7340.751,1079.9324)及胶囊不变，终态物理已关闭、完成面板标记为真，save前后SHA均D68DEC7993D42D14D26D4F98E88C00B907350568CA6F6B52E552E87F08A56779。Velocity字段仍260是冻结后的保留值，不把它写成角色继续移动，也不以这些字段声称相机/UI实际像素已验。证据为该case/storage/task10-route-result.json及原stdout，不是手造截图。

## 审查、开发决策与保留边界

单次gpt-6-astra/high只读审查C0/I0/M0，无C/I修补、无延期Minor、无二审。Base=Head=`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，生产BEFORE保存在本计划过程目录，审查针对指定增量而非整条dirty历史。

全部决策及其成本：

1. 唯一指定dirty工程原位制作，不建worktree、不提交/重置/删除账；成本是不能宣称干净发行分支，需当前输入/二进制清单。
2. 采用开发暂定腕轨迹，不追记为用户风格批准；成本是后续正常速度可能需要重做幅度和美术。
3. 正常速度画面/音画/手感/真人继续OPEN；本次数学与接线不能代签美感，可能仍要返工。
4. 原十一项/pre/post/Main/Explore总门由root继续；窄审查不替代整关，既存红门仍阻断交付。
5. 其他dirty历史不追认合并安全；本次不做Git写入，不能称完整分支可发布。
6. 原链/secondary/recovery算法不重新批准；只验证本调用交互，旧故障仍需单独闭环。
7. 现实物理/硬件/发行稳定不从表现几何推出；R-I/R-C/R-P与设备门仍未验收。
8. 移动/连击/真实hit-pause/真实输入Up/系统接管全部端到端组合仍缺；typed样本不等于这些已通过，组合接续风险仍保留。
9. 动态目标在不同采样率下不宣称逐时刻严格相同；只证held等价与各采样率有效运动，正常速度仍需比较。

v243的接管当帧、同攻击翻面/Secondary Up组合两项延期没有被本次“手臂Up轨迹”自动关闭，覆盖对象不同。

## 其他只读推进与下一步

- [官方同commit崩溃路径](2026-11-04-resident-geometry-ownership-readonly-v001.md)已按原始空行核到cpp1788/C#100、137，与旧栈吻合；上游同类竞态为假设，不是KNIGHT资源对象/唯一根因证明。未加GC/保活/等待/运行时调参、未换引擎或新采集。
- 当前主体16件/披风4件PNG真实存在且匹配阶段清单，但批准评审图身体0/3、披风0/3匹配历史批准SHA；dressedMaster仅ready-for-review，smallScale/animationReady未开始。见[既有来源问题](2026-10-24-profile-use-serial-and-asset-provenance-v096.md)、[既有复用边界](../art/2026-10-26-existing-art-reuse-v134.md)。这不是“无图”或“只差接线”，也不是必须再付费生成的证据。
- 有界查找G美术源/工程docs的六批准hash与文件名只见原批准记录和当前清单/迁移索引。索引指向的六张旧E评审PNG仍在，与G当前副本相同SHA、均非批准SHA；不代表全盘不存在别本，不扫描大媒体。后续恢复入口为8月13日07:14身体、07:30披风批准时原始附件/备份及其同版本manifest/导出，或明确重新审阅当前稿，不能用重渲染冒充批准原件。

原目标保持完整：优先验证一击终结局部预算及完整门耗时/资源来源，再推进正式主体来源与完整动作链、UI/音画/硬件/正常试玩。本文不能关闭任何原十一整项，也未完成用户三项动作/惯性/物理返工。

## 最终保全核对

当前源1888项清单SHA256=`D71FC4295FE242FED62BE70AA6F9FA44FA90BB2B5122A545F296C61165269908`；六文件binary清单SHA256=`681FEFB7FEFEB3E504622AD58DF255BCFEC31D9EF7FD2E954A65562EC825D1FA`，二者均为清单而非发行包hash。root已实际逐案核15×5＝75组前后文本SHA相同；15案source-before和fresh-binaries清单分别同上述值；当前1888文件及6binary重新逐一hash，差异0。真实用户文件及continuation保护未变化；最后owned native/build为零。

全部会话91802、59418、70563、21157、74267均已终态；两个最初编译失败、两个真正RED及全部原始输出保留。协调循环exit0只表示走完各案，不是全绿证明；严格3FAIL如上。指定三CS的git diff --check通过，无临时生产回退。正常一次媒体检查2026-11-04T11:09:10Z为7427750字节/86文件、Notify=false，没有新增媒体或重置基线。

本轮无GUI/系统输入、付费、新过程媒体、额外原生采集、自动化、外部发布、真实存档修改或资料删除；20GiB只随正常汇报附带，不新建提醒任务。
