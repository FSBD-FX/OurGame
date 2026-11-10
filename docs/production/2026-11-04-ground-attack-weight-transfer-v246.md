# 地面挥刃重心接续 · v246

状态：一个生产表现文件＋四个测试文件增量。实际Draw与保存Player真实J均已观察RED→GREEN；最终同源十六门12PASS/4FAIL，默认Main仍失败。一次审查C0/I1/M1，I1完成一次局部预算修补但完整集成未闭合；不宣称完整动作、物理系统或序章完成。

## 事实、推理与风险

事实：原GroundAttack已有四阶段Body转角、主动肘腕及悬灯/系带响应，但同一满支撑静止条件下，实际髋关节没有攻击专属位移。修前typed与真实J均测得hip为零；本轮沿现有共同位移接点加入后移蓄力、前移压低及回收。真实J每侧44个发布观察样本满支撑、覆盖四期，脚底与刚性链保持，详见下文原始证据。

推理：现有证据支持这一程序重心源已进入真实绘制消费，不能证明正常速度幅度合适、完整身体动作已有特色或物理手感合格。另一个测试驾驶修正让默认键盘摆锤案选择原外来攻击防守；模型通过并不等于全场安全，最新Main已在后继Skitter必需举盾时因能量为零严格失败。

风险：本轮位移端点只是开发样段，不是人体/物理单位标定或用户新风格批准。腿18＋18、臂24＋24、十槽、纹理哈希/scale、root/碰撞/Motor/攻击时机、HP与原899/55s/180s/300s门均保留。原十一项保持ACTIVE；R-I可感知惯性、R-C角色动作语言、R-P物理对应/操控手感、正式分层美术、正常速度像素/音画及真人验收均OPEN。

## 实施范围

依据[重心设计](G:/knight/Knight/docs/superpowers/specs/2026-11-04-ground-attack-weight-transfer-design.md)、[重心计划](G:/knight/Knight/docs/superpowers/plans/2026-11-04-ground-attack-weight-transfer.md)、[摆锤路由设计](G:/knight/Knight/docs/superpowers/specs/2026-11-04-gallery-foreign-guard-routing-design.md)与[摆锤计划](G:/knight/Knight/docs/superpowers/plans/2026-11-04-gallery-foreign-guard-routing.md)，本轮五个代码文件分别承担以下责任：

| 文件 | 增量 |
| --- | --- |
| [PlayerFormalVisual.cs](G:/knight/Knight/scripts/presentation/PlayerFormalVisual.cs) | 唯一生产变化。原ApplyBodyLoad之后、LandingLoad之前接共同攻击骨盆位移；十槽同移，随后仍走原腿/双手握持求解。 |
| [PlayerFormalVisualTests.cs](G:/knight/Knight/tests/integration/PlayerFormalVisualTests.cs) | 既有RunBodyLoadOnly末尾新增实际Draw重心控制；读取真实关节/节点，不借私有目标函数生成期望。 |
| [ThrusterPresentationRuntimeTests.cs](G:/knight/Knight/tests/integration/ThrusterPresentationRuntimeTests.cs) | 复用原保存Player真实J观察器，保留原主动臂、握持、附件、回稳、HP/root与清理断言，追加hip/Body/实际脚底和刚性腿测量。 |
| [PrologueCanyonGeneratedMainRouteTests.cs](G:/knight/Knight/tests/integration/PrologueCanyonGeneratedMainRouteTests.cs) | 独立测试驾驶修正：仅将默认normal纳入原gallery foreignUp选择，原实际安全链和其他显式探针不变。 |
| [RangedWeaponModelTests.cs](G:/knight/Knight/tests/unit/RangedWeaponModelTests.cs) | 新增16行字面量真值组合，覆盖keyboard/gallery两个作用域及normal/explicit选择。 |

生产源只消费同一Action.Phase/NormalizedTime和既有elapsed，不加动作时钟、计数或输入。分段Smooth端点为起势(−1.4,1.4)、发力(1.8,2.0)、经过(.6,.8)、收势归零，采用30/sec指数追踪。X按锁定Aim与当前facing换算，纯竖向攻击不加横向驱动；同一攻击翻面先转换新位移缓存的坐标，不推进时间。

仅真实Grounded、Gait.Observed且Weight≥.999、双脚底有限，并且非Dash/Dead/Recovery时消费。丢支撑、死亡、恢复或显式转换重置立即清除这一源；有可靠支撑的正常取消平滑归零。旧Body攻击转角仍只应用一次，后续Landing/Air/Secondary及原求解相对顺序不重排。没有改真实力学、质量/冲量模型、关卡或正式素材。

## 重心TDD与测量修正

各案均由root唯一无窗口原生执行，`TimedOut=false`、`guardFailures=[]`、`sourceRows=1888`；这是各案provenance，不是最终当前全部输入/binary绑定。失败案全部保留。

| 案例 | 实测与结论 |
| --- | --- |
| [初次typed RED · 205052](G:/knight/验证证据/october13-stable-claw/thruster-ground-gait-20261104-205052-171-67ec2fc825fa4f73ab28172e18f7f616/engine.stdout.log) | exit1 / 10570ms；双朝向四阶段实际hip均(0,0)，原前置通过，缺少重心位移是行为失败。 |
| [初次真实J RED · 205138](G:/knight/验证证据/october13-stable-claw/thruster-presentation-rig-20261104-205138-648-89bf7e867c7441d89c526af06c8f3ddb/engine.stdout.log) | exit1 / 16620ms；每侧44/44满支撑、四期、soleGap0、刚性腿成立，hip均0。最初Body基线混入旧Idle呼吸/动作进入过渡，BodyHipGap右.64881706、左.42870522；不把这一误差当成新生产缺陷。 |
| [纠正基线后的真实J RED · 205531](G:/knight/验证证据/october13-stable-claw/thruster-presentation-rig-20261104-205531-442-2795e8cb8d4e4f589556a62c1ea62276/engine.stdout.log) | exit1 / 16354ms；两侧hip仍0，44/44支撑、四期、soleGap0、刚性腿成立；BodyHipGap为0/约2.37e−22，失败仍准确指向缺失主动hip位移。 |
| [含BodyLoad叠加的最终typed RED · 205917](G:/knight/验证证据/october13-stable-claw/thruster-ground-gait-20261104-205917-334-4a84cfc128c54dbb935ccfabb2fb48c2/engine.stderr.log) | exit1 / 10452ms；原hip0反例和新增非零BodyLoad叠加控制均真实失败，非构建错误。 |
| [typed GREEN · 210135](G:/knight/验证证据/october13-stable-claw/thruster-ground-gait-20261104-210135-830-995725963c51464688c00638c4f13529/engine.stdout.log) | exit0 / 13688ms、stderr空。双朝向8阶段组：起势约(−1.39653,1.39653)、发力(1.79208,1.99850)、经过(.60295,.80297)、收势(.00149,.00199)；实际脚底、刚性链、握点及各边界通过。 |
| [真实J GREEN · 210231](G:/knight/验证证据/october13-stable-claw/thruster-presentation-rig-20261104-210231-105-3ced859e489945979381e9a8039cb1ef/engine.stdout.log) | exit0 / 18706ms、stderr空。保存Player真实J、真实Motor/脚底、同发布帧，原握持与刚性链保留；实测范围见下表。 |

基线只改为从节点位置扣除固定authored rest及当期CurrentActionPose.Body.Position，排除原有呼吸/动作转场；`.002`容差未放宽，没有删掉旧动作或预先制造新hip目标。纠正后的实际J先再次RED，之后才写生产实现。

typed额外覆盖反向/纯向上锁定Aim、零elapsed与后撤时钟、固定held目标在30/60/120采样下的相同elapsed终点、可靠支撑取消、缺失/部分/非有限支撑、Dash/Dead/Recovery、显式reset，以及新骨盆源的世界翻面连续性。非零BodyLoad叠加时，十个兄弟节点相对差分均须跟随实际hip，原脚底/链长/握持控制继续运行；不把这些固定目标控制称为任意动态输入下的精确采样率等价。

### 保存Player真实J的实际范围

下表是每侧44个发布观察样本，均满支撑、覆盖四阶段；不是44个独立物理帧，也不是正常速度像素审阅。X为该日志的相对hip量，Y为压低量，长度单位沿当前程序场景。

| 朝向 | hip X最小 / 最大 | 最大压低 | 脚底最大误差 | Body与hip共同位移误差 |
| --- | --- | --- | --- | --- |
| 右 | −.92976856 / .9875221 | 1.7002602 | 0 | 2.5484035e−6 |
| 左 | −.9296894 / .98737335 | 1.7000923 | 9.536743e−7 | 2.149076e−6 |

原根位置、HP、实际J阶段/同Formal身份、固定腿长和旧双握点断言均保留。这些数据证明新共同位移已到实际消费者，不签幅度美感、完整人物重心/发力语言或脚掌最终绘制质量。

## 独立测试驾驶修正与默认Main

既有摆锤Skitter分支在实验旗标关闭时曾把通用外来攻击防守覆盖为false。本轮仅提取并启用 `keyboard && gallery && (normalPlan || explicitProbe)`；只替换foreignUp首选择条件，原同Actor/攻击/骨骸俯冲来源、正上方.92、静止、完整AscentGrip5支撑/走廊及generic条件保留。contact/earlyCounter/laser/neutral/brake/bait仍按各自旧旗标，不随normal一并开启。

| 案例 | 结果 |
| --- | --- |
| [gallery模型RED · 204105](G:/knight/验证证据/october13-stable-claw/ranged-models-20261104-204105-951-21537d8b82554c3fa1682af5637e15eb/engine.stderr.log) | exit1 / 542ms，唯一预期失败为keyboard/gallery/normal真、explicit假未选；不是编译失败。 |
| [gallery模型GREEN · 205009](G:/knight/验证证据/october13-stable-claw/ranged-models-20261104-205009-364-879f6c9e721343178f8c4bb586e7283d/engine.stdout.log) | exit0 / 537ms、stderr空；16组合/双作用域及原回充完整来源、发射/fault否决控制保留。selector回执明确 `safety_authorization=false`。 |
| [默认Main · 210330](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-210330-687-716c480b6aec4bd695754b3658fca1b2/provenance.json) | exit1 / 159998ms，非超时、guardFailures空。默认foreignEnabled=True、explicit=False；后继自身Skitter必需防守严格失败，详见下文；不算完整Main通过。 |

该修补前默认案已记录外来防守request8057，实际Up+L在8058采样并启盾，来源标识4888880747687/攻击2，HP2；最后接受伤害是此前配重区ghost-diver的序号3，没有新的序号4。只读核对已结束：最后同源Active样本8079，随后没有该击的Connected/来源绑定格挡回执；不能凭“未损血”或配置标签认证实际接触盾。

[本案原严格错误](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-210330-687-716c480b6aec4bd695754b3658fca1b2/engine.stderr.log)为physics8234/loop515自身Skitter required guard未采样/维持：held=True但active=False、HP2、energy0、heat.063、overheated=False。它发生在原899预算用尽之前，不能沿用v245“899末敌HP1/playerHP1”的旧结论，也不通过扩时或降HP门解决。失败位置与资源状态不等于已查明全部生产根因。

## 开发判断、审查与未完成项

[重心账本](G:/knight/Knight/.superpowers/sdd/2026-11-04-ground-attack-weight-transfer/progress.md)与[路由账本](G:/knight/Knight/.superpowers/sdd/2026-11-04-gallery-foreign-guard-routing/progress.md)保留开发判断：继续既定Godot/dirty工程，不重问已确定的项目/方法选择，不Git写操作；具体位移和路由不是新风格/玩法批准。实际J复用原ThrusterPresentationRuntimeTests观察器，不另造synthetic fixture冒充真实输入，代价是多一个测试文件增量。

关键顺序纠正为先原BodyLoad、后新共同位移、再LandingLoad。原BodyLoad只绕hip转非腿附件，若把新位移提前会让上身向量单独旋转而相对腿漂移；新增十节点BodyLoad叠加控制约束这一风险，不重排旧阶段。缓存位移翻面换坐标只保护本新骨盆源，不能代签旧全身翻面连续性。

唯一独立最终审查 `weight_gallery_final_review_v246` 已返回，root复评C0/I1/M1，[审查包](G:/knight/Knight/.superpowers/sdd/2026-11-04-ground-attack-weight-transfer/review-package.md)包含两任务及全部RED。I1按下节一次真实RED→GREEN修补，没有二审；局部预算修复不自动关闭完整Main失败。生产及gallery有起始快照；两个身体测试没有初始全文快照，不冒称逐差审完全部dirty历史。全部开发裁决、证明上限及成本见[完整账本](G:/knight/Knight/.superpowers/sdd/2026-11-04-ground-attack-weight-transfer/progress.md)。

继承v243两项Minor仍延期：接管当帧未由三秒退出控制证明；旧全身同一次攻击翻面/Timeline向上组合未独立补全。新增骨盆局部翻面/向上控制不自动关闭它们。

## 唯一审查I1的局部修补与原Main仍失败

一次独立审查root复评C0/I1/M1。I1为真实默认Main资源接续阻塞，不是已证明的新Body生产缺陷：原选中Skitter会保留10.8点防守预算，但攻击外国resident时仅检查12点攻击费，三次J花掉36，之后必须防守时能量为0。

旧预算helper接三处fresh消费者后，[27字面量真实RED](G:/knight/验证证据/october13-stable-claw/ranged-models-20261104-212502-733-bc5aea3d473d426ea6370ac687f263ec/engine.stderr.log)647ms先检出17.3/12.3等错误准入，再加入原储备/所需非法值拒绝，[GREEN](G:/knight/验证证据/october13-stable-claw/ranged-models-20261104-212922-172-09310d98deb04a6caf32b3456c5ee66e/provenance.json)524ms、stderr空。没有改游戏费率、伤害或时限；不是第二审或额外开启探针。

[修补后正常Main](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-213001-118-84ce0a7d6cf04038981ad990bb0af466/provenance.json)153639ms仍严格FAIL。它实际拒绝三次能量17.333284/17.333288/17.799955的resident攻击（当前owned见证HP2、required22.8），没有再触旧loop515能量0的失败，玩家保持HP2；但原899内整场仍未清完，不能签完整集成。外来俯冲目前只有同源Active/Up+L采样及HP2，没有来源绑定Connected/格挡结果，不能据此认证那次攻击已接触盾。

退场actor确认为ownedSkitter18957281004315；原899末physics8624的缓存HP0属于它，不是下一只敌人。新PrismLaserGunner20240469592882直到清理physics8626才BOUND；代码先松键并等待3清理帧才断言，因此不能说已在原899内打过激光兵。其摘要health=-1是DefeatedHealth初始哨兵，实际HP未记，warning/shot/contact/death均0。末段sample8594 vx260，清理后sample8597输入中立/vx170、胶囊右缘越平台6.262；原末段A/D未记录，不能断言具体漏键。下一证据入口是清敌后无目标换批/实际输入保持/制动与边缘，而不是激光门或放宽899。

新增Minor延期：非零锁定Aim运行四阶段及非Dash落地压缩叠加尚无专门actual几何控制，旧零Aim/禁用状态不代签；继承v243两Minor仍OPEN。审查未判定的完整路线、pre失血、geometry FATAL/全局退出、全dirty历史、全输入矩阵/绑定、性能、正式视听/真实惯性/真人等均保留，root最终绑定另核，不据局部GREEN代签完整十一项。

## 最终同版十六门

17955已实际终态；它的exit0只说明逐案执行循环完成。最终资格为I1后的模型/Main加后续十四门，共12PASS/4FAIL；全部build exit0、sourceRows1888、engine非超时且guardFailures空，失败按原语义保留。旧短门/TDD案不混入本集合，未因红门重跑筛绿或扩预算。

| 原严格门／当前case | ms | 结果 |
| --- | ---: | --- |
| [ranged-models 212922](G:/knight/验证证据/october13-stable-claw/ranged-models-20261104-212922-172-09310d98deb04a6caf32b3456c5ee66e/provenance.json) | 524 | PASS；16路由/27预算及原来源负控。 |
| [Main 213001](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-213001-118-84ce0a7d6cf04038981ad990bb0af466/provenance.json) | 153639 | FAIL；原899未清整场，HP2；旧Skitter退场，下一批只在清理帧到达，见上节。 |
| [rig compatibility 213509](G:/knight/验证证据/october13-stable-claw/thruster-rig-compatibility-20261104-213509-065-39b111bc9fd24fc39570f2ddcbb3c5a7/provenance.json) | 1593 | PASS。 |
| [ground gait 213552](G:/knight/验证证据/october13-stable-claw/thruster-ground-gait-20261104-213552-853-312019911d174963b261137e58bf8624/provenance.json) | 13673 | PASS；新八phase/支撑/时钟/十节点重叠及原控制。 |
| [saved Player rig 213642](G:/knight/验证证据/october13-stable-claw/thruster-presentation-rig-20261104-213642-813-1cf78345064c4003a803626ff738d5ad/provenance.json) | 18511 | PASS；双向各44/44支撑、四期真实J，旧握持/arm/secondary/settle保留。 |
| [actions 213737](G:/knight/验证证据/october13-stable-claw/actions-20261104-213737-514-a582fa5f865447e293d2fc84dcdafb5c/provenance.json) | 9508 | PASS。 |
| [formal claw phases 213824](G:/knight/验证证据/october13-stable-claw/thruster-formal-claw-phases-20261104-213824-962-a928e408bbd54245837550c80afc19ef/provenance.json) | 4873 | PASS。 |
| [recovery compatibility 213905](G:/knight/验证证据/october13-stable-claw/thruster-recovery-compatibility-20261104-213905-967-154c1021d5dc4560a3b206ba3f9b077f/provenance.json) | 11539 | FAIL；跨sector Task10 VFX登记1而非0，见下节证明边界。 |
| [recovery feedback 213951](G:/knight/验证证据/october13-stable-claw/thruster-recovery-feedback-20261104-213951-641-5e02f5828a114e499cbeaffd1bd2ab93/provenance.json) | 3398 | PASS；不代签另一恢复fixture。 |
| [pre 214028](G:/knight/验证证据/october13-stable-claw/prologue-pre-20261104-214028-331-54fdd2783bbf4a77ad773bc9c4ff786b/provenance.json) | 22452 | FAIL；gchandle.is_released匿名终结器FATAL。 |
| [post 214127](G:/knight/验证证据/october13-stable-claw/prologue-post-20261104-214127-581-383914da72204df2b4c893db4e596b28/provenance.json) | 180353 | PASS，原300秒；原4条有意负控warning保留，非全游戏零warning声明。 |
| [geometry 214505](G:/knight/验证证据/october13-stable-claw/resident-geometry-20261104-214505-344-2d655311ca3c48e1982c9983cd6a0d35/provenance.json) | 8238 | FAIL；同匿名FATAL，只有run.143/package789.233ms两钟，不签根因/性能修复。 |
| [save compatibility 214549](G:/knight/验证证据/october13-stable-claw/resident-save-compatibility-20261104-214549-588-d8e60807f9a6475697a82bfc1d37c4fd/provenance.json) | 12704 | PASS。 |
| [keyboard 214641](G:/knight/验证证据/october13-stable-claw/keyboard-20261104-214641-663-e24b2ff4594346cc8138fdb2bef94380/provenance.json) | 20267 | PASS；独立标题7806ms/exit0。内部按键，不是OS/真人。 |
| [claw models 214744](G:/knight/验证证据/october13-stable-claw/models-20261104-214744-238-25b5f4ccc1db4d94b6b6a60d4c498767/provenance.json) | 516 | PASS；原爪/抵达模型子集，不是全模型总验收。 |
| [Explore 214822](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261104-214822-153-261f85ce0f6f425bbf7e48f7de655821/provenance.json) | 323028 | PASS；普通标题/同Player→Boss→实体出口，恢复0/0；不代表全敌群或真人。 |

最终真实J测得右向hipX−.9301138…+.9877634、最大压低1.7005844，左向−.9296894…+.98737335/1.7000923；左右脚底误差0/9.54e−7，Body与hip共同源差约2.63e−6/2.15e−6，刚性腿与双握持通过。它们是实际节点/关节测量，不是正常速度审美或刚体力学认证。

## 特效登记失败的证明上限

recovery case先通过pause0与death/recovery0；随后模式切换645、移动647，只在648等待物理CurrentSector改变，652立即检查VFX登记。world在PhysicsProcess换区，audio在Process轮询后通知VFX整体clear，存在跨阶段观察窗口；audio650的0不含mode cues，不能证明clear已经执行。VFX的ActiveTask10EffectCount是字典key数，burst到期会QueueFree但key不随移除，因此1也不能证明实际仍有一个可见节点。

当前没有剩余cue/transition/eventKind、节点存活或Present/Clear先后回执。“最后模式burst在idle clear前被观察”只是有源码依据的候选解释，不是已证根因，更没有数据连接到新Body代码。下一解除冻结后只在645/648前后加入有界physics/process、旧新sector、clear序号、keys及节点valid/inside/queued和事件顺序诊断；不靠新增等待、改断言或重试消除失败。原前段与几何的独立匿名FATAL继续未解释，不据另一案例安静关闭。

## 当前输入、隔离保护与边界

root在所有native终态后核对16案各5组保护清单（source/helper/user/continuation/binary），80组前后SHA全部相同。跨16案输入清单SHA统一为`5CDEFD52BE3C0F73384FC467238F4184760B2A00C8356CFAC7D687E2397121DF`，六binary清单统一为`8A9CCF2D7DD468FF4FCF31AB1FDB33C6D697E81FA63611B6D546BFB2AD3627A7`；当前1888输入及`.godot/mono/temp/bin/Debug`六文件另逐一重读，差异0。这是受保护集合与清单绑定，不是全磁盘未变、完整Git历史审计或发行ZIP签名。本轮执行会话已终态，未发现仍运行的本次原生case进程，不处理其他Godot实例。

探索[结果JSON](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261104-214822-153-261f85ce0f6f425bbf7e48f7de655821/storage/task10-route-result.json)为automated、synthetic=false，human/operationCoverage/fullGoal均false。Player47563408900贯穿普通title/23访问/11挑战，11激活仅10击败（缺counterweight）；Boss28366715490073 Died一次，实体出口18141完成，持D60拍到18202仍受(6080,1116,1600,40)地板完整支撑，位置(7341.191,1079.9327)、胶囊40×72，completion/event均1。完成后Player物理关闭，velocity260为冻结字段；完成与持键末隔离存档SHA均D68DEC7993D42D14D26D4F98E88C00B907350568CA6F6B52E552E87F08A56779。它不覆盖全部76实战/所有操作或正常速度视听。

普通媒体一次Check为7,427,750bytes/86文件、Notify=false，无新过程媒体/自动化。原分支/HEAD未改，无发布或删除。三个新/继承Minor与所有审查暂不判断范围仍保留，完整原十一项及R-I/R-C/R-P不关闭。

本轮无GUI/系统键鼠、付费生成、新媒体、额外崩溃采集、引擎更换、自动化、发布、删除或真实存档改写。任何物理只读调用图只能作为后续设计依据，本轮没有据此实施Motor/受力模型改造。[用户动作与物理返工要求](G:/knight/Knight/docs/production/2026-11-03-character-motion-rework-requirements-v001.md)及原十一项继续生效，正式全身分层资源、完整连续动作样段、正常速度视听与实际操控/用户认可仍未完成。
