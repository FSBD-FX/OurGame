# Headlamp Fog Implementation Plan

## 当前执行状态（2026-10-11；正常速度路线与性能排错后）

21:01媒体核验更新：更晚同生产二进制的正常时间Main/Explore已有184339ms／271968ms实际exit0，Main focus0／Explore focus2，callback p95为49.011／48.850ms，不是独立GPU/present或流畅验收。另加独立Mobile/Vulkan固定60完整Main电影actual0/449810ms，10805帧；不是正常速度／生产OpenGL一致性证明。原AVI、三MP4（10805/1200/1200帧完整解码）、14原尺寸帧、源码／二进制／guard和两正常时间记录已在过程媒体v002归档并限定独立复核；静帧仍NEEDS_WORK，连续视听／性能／正式暂停复活／缺额回返／历史句柄FATAL仍开放。下段79–82ms是较早版本历史数据，不当作最新测量。完整Task3与目标保持ACTIVE。

正常速度自动纯键盘Main/Explore v2完成Boss与有支撑出口，原地形窄条triangulation错误已实际RED→quad primitive→GPU GREEN，正常路线stderr0；两案均一次失焦，分类FOCUS_LIMITED，不是人工或连续焦点验收。p95约79–82ms、p99约130ms，性能仍不合要求。后续controller两次原55s超时保持FAIL，测试阶段计时后的约48s通过不证明根因修复；其他四专项及参数合同通过，未声称全项目综合通过。精确版本/边界见阶段记录；后续有界性能诊断见`.superpowers/sdd/2026-10-07-keyboard-collection-fog/realtime-performance-diagnostic-plan.md`。

- [x] 实时入口唯一无值keyboard flag/双variant/固定原守卫资格合同，正常速度Main/Explore终态见证（均FOCUS_LIMITED）。
- [x] 原生半径边界极窄平台提示绘制错误的真实GPU复现及最小生产修订，不改遮挡/范围/地图。
- [ ] 正常速度CPU/GPU/雾提交耗时归因及流畅度修订；controller两次超时根因与综合回归闭合。

以下是此前预警修订时点的记录，不把旧固定步通过扩大为当前全部验收：

正式雾已条件显示；原活塞/摆锤提前预警保护已接入，六项同版专项（CPU/native不是像素、controller pixels=false）及fresh Main/Explore通过。两路线为 `route-result-telegraph-main-20261011T150820958-94efdc677d68431d881db618fe4b2473`、`route-result-telegraph-explore-20261011T151135128-603eb32a0169420b98cc0ec8bc52a26b`，实际/wrapper0。旧路径诊断FAIL保留，不放宽过滤器。此前58.78秒失败诊断原片/15秒预览/八帧属于上一版本，已保存并抽帧审阅，不充当本轮预警视觉证据；整体NEEDS_WORK。完整目标ACTIVE，以下未完成验收项不能由专项通过替代：

- [x] 当前同源纯键盘Main/Explore完整通关与有支撑出口（headless固定60自动键盘；非整关像素/真人/实时验收）。
- [x] 原活塞/摆锤提前预警的同源保护实现、自然相位GPU正负控及正式保存controller接线/生命周期回归；不改变原机关时序或碰撞。
- [ ] 活塞/摆锤提前阻路预警在正式连续路线中的正常速度观感与可读性验收（隔离像素和headless路线不能替代）。
- [ ] 正常速度连续视听与实时性能；固定帧率录像不算FPS。
- [ ] 历史综合GCHandle失败根因与完整回归闭合。

最新证据见 `.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md`。后文旧hidden/待接入记录是历史，不是当前生产状态。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Boss前距离渐变迷雾由实际浮灯驱散，保留路线、目标、预警与界面可读性。

**Architecture:** 世界区域配置/实际灯具姿态生成只读雾帧；单个兼容canvas层渲染，受预算的遮挡采样约束驱雾。保护提示从现有权威攻击/抓爪帧投影，不复制战斗逻辑。设置仅调表现。

**Tech Stack:** 当前Godot .NET/C#与gl_compatibility；单canvas shader，无新依赖/体积雾。

**Spec:** `docs/superpowers/specs/2026-10-07-keyboard-collection-headlamp-fog-design.md`。

## Global Constraints

- 近处清楚、远处更浓；头顶灯位置跟随实际灯具姿态，方向跟随键盘瞄准/最后朝向，平滑转向。
- BossHall平滑减雾，SafeExit清楚；不能按整个LostTemple分区清雾。营地/检查点舒服可见。
- 世界、雾层1、经可读性筛选的关键提示层2、已有HUD5、Fade10、Pause20。
- 无灯具消耗/开关/升级、无雾伤害；不改敌人感知、运动、碰撞或620抓爪射程，不照透墙。
- 首版不用屏幕纹理模糊/大量独立粒子；未测量前不承诺帧率或称为精美完成。
- 依赖keyboard计划方向接口和fragments计划局部区域映射；外部case隔离使用共享verify脚本，不生成付费素材或改原用户设置。

## Review Focus

- 角色快速转向/爪预览与灯具平滑转向不同步：不能盲选目标（Task 2/3）。
- 镜头摇动、缩放与分辨率切换：雾跟世界灯位，不固定于屏幕中心（Task 2）。
- 低顶隔墙与窄开口：光锥遮挡不能泄露隔室（Task 2）。
- LostTemple庭院与BossHall边界：只在实际Boss区减雾（Task 1）。
- 减雾设置、暂停/复活：可读性提升不改变伤害/射程，暂停雾动画不继续（Task 3）。

---

### Task 1: Region profiles and fog state model

**Files:** Create `scripts/presentation/fog/PrologueFogProfile.cs`, `PrologueFogModel.cs`; Test `tests/unit/PrologueFogModelTests.cs`, `tests/RunHeadlampFogTests.cs`。

**Interfaces:** `PrologueFogProfile(float ClearRadius, float OuterRadius, float MaxOpacity, float BeamRange, float BeamHalfAngleDegrees)`；`PrologueFogModel.EvaluateOpacity(Vector2 worldPoint, Vector2 actorPosition, Vector2 lampPosition, Vector2 lampDirection, PrologueFogProfile profile) -> float`。区域按`CanyonRouteNodeId`读取，只读模型不修改进度。

- [x] 写`NearClearFarHeavier`、`BeamReducesFog`、`OnlyBossHallReduces`、`RestAreaReadable`：固定profile断言近处0、远处<=MaxOpacity且递增、光锥内少于同距锥外；庭院不等同BossHall。数值fixture为180/620/.72/620/22.5，仅作起始调参。
- [x] 接入runner后执行`pwsh -File .superpowers/sdd/2026-10-07-keyboard-collection-fog/verify-feature.ps1 -Feature fog -ExpectedOutcome red`，取得模型行为RED。
- [x] 实现连续smoothstep距离/角度权重、有限值验证、区域混合；默认探索.55、深井.72、入口.28、营地/安全出口.10、BossHall.18，作为可配置候选而非用户验证数值。
- [x] 同命令GREEN要求`HEADLAMP_FOG_OK`；边界连续、未知/走廊区域安全默认，不因未匹配返回全黑。
- [ ] 独立审查后只提交自有文件。审查已完成；尚无本项Git提交，不因测试通过标记已提交。

### Task 2: Real lamp, occlusion and world-aligned renderer

**Files:** Create `scripts/presentation/fog/HeadlampOcclusionSampler.cs`, `scripts/presentation/fog/PrologueFogController.cs`, `assets/shaders/prologue-fog.gdshader`; Modify `scripts/presentation/PlayerFormalVisual.cs`, `tools/build/prologue/BuildPrologueLevel.cs`及其生成的`scenes/prologue/PrologueLevel.tscn`；Test `tests/integration/HeadlampFogRuntimeTests.cs`。

**Interfaces:** `PlayerFormalVisual.LanternWorldPosition -> Vector2`只读实际Lantern节点位置；`HeadlampOcclusionSampler.Capture() -> HeadlampOcclusionFrame`读取当前World PhysicsBody的native矩形shape owners，完整变换/Disabled/流式激活状态进入不可变快照，`CanSee(origin, point)`精确局部slab遮挡。实际空间查询为0，仍满足原最多32条预算。`PrologueFogController.CurrentProfile`与`LampWorldPosition`只读。渲染只读取数据，不操纵玩家/攻击。

2026-10-10实现裁定：32角度的稀疏轮廓会漏掉已有16px薄台和36px小立柱，因此不以插值射线授权透光。改用实际native owner几何，同时约束角色清楚圈和头灯光束；仅支持当前矩形World PhysicsBody，未知几何/256容量溢出明确fail-closed。无节点的直接PhysicsServer body不在现有作者管线范围；不能宣称任意引擎几何全覆盖。未来renderer每帧必须使用新快照，旧frame不可变并不允许跨帧复用失效门状态。

- [ ] 写实际Lantern偏移/摆动、镜头变化、墙体切光、进出BossHall、暂停/复活的fixture；清楚圈与光束不得照亮墙后独立房间。运行fog RED。
- [ ] 实现getter、世界变换到viewport的雾层与遮挡轮廓。查询只针对真实遮挡碰撞，复用样本/有限预算；光方向平滑但方向切换时唯一爪候选须有清楚的短提示，不能盲选或自动跳目标。
- [ ] C#父builder添加CanvasLayer1雾层，保存前节点计数/重新实例化对比；不改PlayerFormalLayer轮廓或未验收披风姿态。
- [ ] 运行fog GREEN，在1280×720及当前设置支持的高分辨率正常窗口比较帧间隔/查询数；不把固定帧率电影当性能结果。
- [ ] 独立审查后只提交自有文件；shader/环境可见改善尚须Task3实际录像验收。

### Task 3: Protected cues, reduced fog and full route proof

2026-10-11残片保存失败反馈增量：扩展原Fragments消费者，仅读原QuestFragment.LastResult及Status本地化文字/字体；附近240且同帧原中心/提示LOS有效时最近一个来源独占固定像素短条，底缘距屏幕底120×resolutionScale。真实FileShare锁盘失败保持disk/live/HUD0和原保存SHA，原Player再次接触才成功一次；中英文完整GPU字形、1.5镜头缩放、真实saved消费者、降层/墙/距离/pause/source与overlay卸载重入均通过。最终同版GPU `093203-176-2c8bc0d1b7154d95bd9cb4b91837bf30`、controller `093450-359-5cb7db4101e547ff93d877047c3a6b5c`、CPU `093543-341-b806faef8a9b445e8e25a7cd805451a7`、raster `093605-188-5f884efdc1164f209681fa41fd9cfd81`、Q GPU `093621-301-976db87c9317423b85bf7f3609ddd8b4`、独立Q `093658-503-bf833ffb7d774df4835c4dcca0dfb799` actual/wrapper0。fresh Main `093823236-ca34cf7511e649d19275d7f16ffcec24` 与Explore `094335415-efb6b3c6d1ee47419fd279e8a3ef24a4` 均完整自动路线通过，Boss及有支撑出口/D60一次结算；不是human或实时视效/FPS。残片综合 `093300-070-84a6205d7d9c468b9b19d0e0f60592f0` 则Handle加载异常与释放FATAL同案FAIL，未执行独立跨进程读回、不重试掩盖，不能以专项替代。未改builder/父scene/机关/保存条件，正式fog仍hidden；Task3及完整目标保持ACTIVE，资源寿命根因、更广0/4跨区补六、正常速度全UI/性能及新归档审阅录像仍保留。各RED/FAIL与guard见headlamp-fog-progress。

2026-10-11原门边界增量：独立layer2 Barriers由C#父builder保存，controller只收mounted八原固定路径，读取同Player/同fresh native snapshot。seal/chain/长shortcut/admission三shape/独立Entry与Exit/两pressure Door仅显示当前启用矩形的中性薄边；实际receiver/条件与deferred提交后撤cue，同body其他shape和普通墙仍挡住整quad。保存controller `085700-059-e86419f3e6654ec5bcefc8245bd32559`、CPU `085829-356-a798b70e43c14ab4805f28a7cb47e819`、GPU `085838-945-99816ed5a0a64f1e8b0d9119c37b2662`、raster `085913-052-5239e3d8bdd9491aa062944ed2deedd7`、Q GPU `085920-597-1677e386d150496b9ad6e426b7df86de`、独立Q `085931-593-2c68bd7b1725479491a23c2f6b2b8954`各自actual/wrapper0且source744同版；独立产物审计通过。父node134→135、ext42→43、sub13及旧node属性/共享均不变。fresh Main `090022458-c3347e33425d4198a8d3f8844ca0974e` 与Explore `090342940-c0f809405611416196ea6251fbf49441`实际/wrapper0，标题同角色→Boss→有支撑出口、恢复0/0、D60一次结算；Explore真实6→7/补给1→3/5/双向捷径。所有fixture路径/重力/墙内pose前提FAIL保留，不改生产碰撞/地形/伤害/保存。上述非human/正常战斗全路线/视效FPS，原074719键盘综合FATAL仍FAIL；独立Q不抵消。正式fog继续hidden，Task3/完整goal保持ACTIVE，保存失败文字、更广0/4跨区补收集、资源释放根因、正常速度性能/视听与新归档审阅录像仍待闭合；详见headlamp-fog-progress。

2026-10-11运动几何增量：原CounterweightLift/GearPiston/PendulumHazard的当前native矩形中性薄边已由父builder保存独立layer2 MovingBodies，controller仅mounted原来源/同fresh frame。CPU face predicate支持完整局部affine面、inside/背面拒绝、同body其他shape及整quad遮挡；GPU使用三原saved源自然PhysicsProcess（非AdvanceForTest/注入Frame）验证实际位移/旋转/新旧像素、disabled和KeepActive等状态。保存controller `081248-729-f589cf5837cc49039f6cff25727c93d9`、CPU `081437-110-6e6b0e6aa2db4cd4a17c66f19d930229`、GPU `081503-323-3dd4f71b28f94ed09bd7a7a927ef626b`、raster `081556-642-a4fdf046af2143dc8368130883ebae32`、Q GPU `081617-780-37f7bf2ca03c47b793323a4ce4cb88a8`、独立正式Q `081656-929-a25b894572834ccc8d43a60bd6cf44c0`在同一741项source快照各自actual/wrapper0；旧080251/080707只是各自通过的不同source历史。fresh Main `081817193-0feba682772342a6bec7cb6946f3706e`及Explore `082307576-f8c55583ba6e4bf49a4564107fd3757d`同版自动键盘均从标题同角色完成Boss/有支撑出口，恢复0/0，D60保持Completed一次；不是human、正常战斗全路线或视效/FPS证据。保留全部fixture前提FAIL和裁定，不改机关HP/时序/碰撞，不将通用HazardActive当伤害；此前键盘综合074719 FATAL仍FAIL，独立Q不抵消。正式雾hidden，完整目标ACTIVE，资源释放根因、其余边界/保存失败UI/广泛缺额回返/正常速度性能与归档录像仍待闭合。

2026-10-11压板门增量：原visual新增不可变共享ClosingPaint，原_Draw和独立layer2 PressureGates按同一原gate自然倒计时绘制关闭线/五tick，consumer验证原disabled native owner与节点变换、整quad遮挡，不修改原门时序/防夹/碰撞或保存。父builder仅新增脚本/一个节点，旧132节点/13子资源共享不变；post-pack GPU `074536-463-1f8ec4f2304840b49ec99851d1410e05`、saved controller `074418-496-9d60a56591e8478f837323c09eeaca83`、CPU/native及原门完整regression `074514-513-0f3cb531add24d04bfc87c847810bfad`、raster/Q GPU通过。键盘综合 `074719-732-da41fdcd768f41ebb09f4cdf9a5d0984` 在sample标记后发生RefCounted finalizer原生FATAL actual -1073741795，必跑formal Q未执行；独立FormalInteraction `075109-109-d9a0efa942834105b7fe7e127cd4dad4`通过不改变该FAIL。源/scene/用户/binary守卫稳定。当前没有新完整Main/Explore或归档媒体，旧route不借为本次版本。正式雾继续hidden，剩余移动几何/门边界、保存失败UI、广泛缺额回返、资源释放定向调查、正常速度性能/视听和归档审阅录像仍待完成；Task3不勾完成，完整目标ACTIVE。详细fixture前提FAIL、裁定、同版本审计与官方源码边界见headlamp-fog-progress。

2026-10-11持续伤害区增量：独立layer2 ShieldLane已由C#父builder保存，controller只批量提交当前mounted原Gameplay/ShieldLane和同一fresh native遮挡快照。原伤害在PhysicsProcess轮询身体，故停回调/不能process/Monitoring关闭/无Player mask/零伤害即撤提示；layer0和Monitorable=false不是无伤依据。仅真实矩形内的短橙边、整笔画可见hull，不制造预警/安全窗。GPU缺类、saved缺节点和Monitoring删除负控均实际RED→恢复GREEN；最终GPU `070952-890-5183b9a75fb342c8801127879a49983c`、保存controller `071117-188-9f9b54cf828843a8ab9dc545a59aaad9`及CPU/raster/Q GPU/必跑formal Q通过。fresh Main `071530501-c1a9ebbf41e04fc284460d36549ea837` 与Explore `071849861-61e5ba79403b4cd2bcc0cfcd30fa1c82` 完整自动回归GREEN，非真人、正式视觉或实时性能。原131节点/13子资源共享不变。下一批补原压板门共享关闭预警和三类实际移动几何，不能按通用HazardActive判断伤害；正式雾继续hidden，完整Task3及目标仍未完成。所有失败/fixture裁定/来源保护见headlamp-fog-progress。

2026-10-11真实蒸汽窄片：新增独立layer2 Steam，正式controller按当前mounted来源批量提交五个原CanyonSteamJet，同一native遮挡快照；断续黄预警/较粗橙危险均画在真实rectangle内并检查完整笔画hull，不改原机关时序/碰撞/伤害。Disabled+Remove旧状态实际RED后修复，KeepActive合法状态保留。GPU覆盖standalone+Trench两喷口，保存controller验证另含Frozen两喷口的全部五源；切换、停机、暂停/重入撤提示。父builder只增Steam节点/脚本，原130节点与13子资源共享不变。最终GPU `063946-130-4a8170f17ed34671b52c98999843f5f1`、保存controller `065019-450-9c84fda74379433fabe5d821b24dd0e9`、CPU/raster/Q GPU/必跑formal Q均GREEN；fresh Main `064156761-45d53fae23cc40e0ad50a9155a96e4c7`及Explore `064457500-4df014ab2e34450288e26c2c263e3c49`完整自动回归GREEN，非真人/最终美术/实时性能。正式雾仍hidden；下一片优先真实ShieldLane及压板门关闭预警，其余机关/全部必要提示、广泛缺额回返、GCHandle综合FAIL及正常速度性能/归档录像保留，Task3不勾完成。详细RED/FAIL/GREEN与未覆盖边界见headlamp-fog-progress。

2026-10-11静态落脚短边缘窄片：原saved平台/配对native矩形、同帧body/owner/index快照与完整patch可见性保护已接独立layer2 Terrain；贴顶薄挡和1px假边缘实际RED→GREEN。父builder受控生成仅新增节点/脚本、原129节点与13子资源共享不变。post-pack GPU `060105-454-144dd146cad24eb5b93591c01ab3dddd`、saved controller `060003-280-199f8eb22ef640768590ffafdec38c33`、CPU/raster/键盘formal Q/Q GPU通过；fresh Main `060254276-cdfff38935b64e2ca6b96ede61cac509`与Explore `060620280-9acc5c93bb474299a9f165a10b16c52f`均完整自动化GREEN，非人类/正式视觉或实时性能。正式雾仍hidden；仅当前水平静态支撑，不覆盖移动机关或保证落地无伤。直接API过期/伪member和同body第二owner独立负控为deferred minor；其余机关/必要提示、广泛缺额回返、GCHandle综合FAIL根因及正常速度性能/归档录像保留，Task3不勾完成。下一窄片只读映射为5个真实SteamJet，不能按装饰齿轮/ICanyonTelegraphSource的handoff补造伤害语义；详见headlamp-fog-progress。

2026-10-11 01:50攻击保护窄片：保存父scene新增独立layer2 Attacks，controller仅mounted固定Actors五类live实体和arena.CurrentBoss的固定原AttackTelegraph，单次batch共享fresh native快照。真实Moth自然锁定Capsule→Active Circle→Recovery GPU、同场双Sentry/queued祖先删除即时撤cue通过；保存controller真实Moth暂停/恢复/leaf改名退场和受控Boss Sweep/Reset通过。仅收Sentry、排除activeBoss两负控实际RED并精确恢复；排队删除旧cue另先RED后最小修复。fresh Main013939853及Explore014233519均完整自动路线GREEN，非人工或正式雾视觉/性能。正式fog继续hidden；Q/数量实体门和安全补六/其他提示/实时性能/归档录像仍未完成，Task3整体不勾选。详见headlamp-fog-progress，保留全部失败历史，无Git提交。

2026-10-11 00:56近距残片窄片实际状态：独立小cue、正式layer2父builder/保存scene接入和共享fresh快照已实现。真实GPU近远/层级/墙/原节点离树重入/原Player一次接触事务/真实新局重置、saved controller及收集跨进程回归GREEN，详见headlamp-fog-progress。真实测试发现QuestFragment入离树订阅缺陷，已最小修复并以缺Flow重连负控验证；不改收集事务。240仍可调候选，formal fog继续hidden；全部关键提示、实体quota/安全补六、实时性能/归档录像未完成，Task3整体不勾。以下为开始该窄片时的历史裁定。

2026-10-11近距残片窄片裁定（待实现/验收）：独立consumer防止PresentAttacks.Clear互擦，读取真实saved QuestFragment及其原AvailableVisual/同world/当前IsCollected，使用同FramePreDraw fresh native遮挡快照；不做全世界group扫描，仅mounted sectors固定QuestFragments目录。采用独立可替换小型残片提示而非复制整幅艺术/phase，不修改收集事务或QuestFragmentVisual。FragmentCueRadius=240作为可配置候选（角色clear180之外60px过渡外沿），不是已批准/验证最终数值；180内通常alpha0，不能以降层像素相同虚称雾上保护负控。真实GPU需检验近距、同层降至fog下、16px墙、远距、原节点detach/回装与原Player原生接触的一次磁盘/HUD提交后旧cue消失。正式layer2父builder与saved接线另行验证，未完成前formal fog继续hidden；小cue不等于保存失败文字/机关/落点或Q场景对象全部可读。

2026-10-10 19:26阶段：单一真实Sentry攻击提示三组GPU像素（含相机、Clear、16px墙、None/detach/free）已RED→GREEN，故意透墙负控被检出并精确恢复，最终case192404；普通world预告共用原几何、保护副本在FramePreDraw即时绘制。ReducedFog仅默认值/旧设置兼容/Normalize/真实atomic store往返通过（191455），UI与renderer未接。正式雾保持隐藏，剩余关键提示/多威胁与Active覆盖/完整路线和录像性能仍待完成，不勾Task3整体。

**Files:** Create `scripts/presentation/fog/FogReadabilityOverlay.cs`; Modify `scripts/prologue/guidance/CanyonCombatTelegraphAdapter.cs`, `scripts/ui/WorldTargetMarkerVisual.cs`, `scripts/settings/GameSettingsData.cs`, `scripts/ui/PrologueSettingsMenu.cs`, `assets/localization/prologue-ui.json`, `tools/build/ui/BuildPrologueUi.cs`及其生成菜单；Test `tests/integration/FogReadabilityRuntimeTests.cs`, `tests/integration/HeadlampFogRuntimeTests.cs`与`tests/presentation/ContinuousCanyonRouteProbe.cs`。

**Interfaces:** overlay消费已有`AttackPresentationFrame`及MechanicalClawFrame/分辨率投影，不能复制伤害判定；只显示当前相关威胁/唯一有效候选，不把远处全部敌人/锚点提到雾上。设置增加`bool ReducedFog=false`，Normalize/保存保持，ReduceFlashes停用雾动态但不改逻辑。

- [ ] 写`NearbyTelegraphSurvivesFog`、`HiddenRoomNotRevealed`、`ClawCandidateMatchesCast`、`ReducedFogDoesNotChangeDamageOrRange`；设置旧文件缺字段用false，所有数据用隔离路径。运行fog RED。
- [ ] 实现layer2保护呈现及设置选项，中英文UI同步；原世界攻击/绳线仍权威，动作结束/卸载立即退保护提示。
- [ ] 运行fog GREEN和完整键盘Main/Explore，检查深井、蒸汽、低顶、多预警、长抓、营地、庭院/Boss门、出口。对暂停、死亡和继续重复检查。
- [ ] 正常窗口录像/实时数据保留源码来源，展示前在新过程目录保存原片、展示副本、原尺寸帧及索引；审阅连续片段，不能只凭抽帧宣称动作/声音通过。
- [ ] 更新README实测/局限与资产表，独立审查后只提交本任务自有文件。

历史状态（2026-10-10 16:05）：Task1纯模型及小角度精度补控已实际RED→GREEN并独立审查。Task2实际Lantern getter、实时native矩形遮挡快照分别取得行为RED→GREEN，case `fog-20261010-160542-270-1b581a99d6ee4731afb800b4ef90450a`引擎exit0/三个OK/stderr空/source及用户保护稳定，独立只读复核无阻断；当时尚无shader。

最新状态（2026-10-10 17:36）：Task2新增独立`PrologueFogCanvas`、兼容shader、共享只读区域场与GPU像素runner，10组实际GREEN `fog-20261010-173326-502-c542a115d9c14d78b27255b7f5281aa3`；包含同节点重入修复、1280/1920、非对角矩阵、双原生墙/实时门快照、深井/庭院/出口渐变、有限投影溢出保守拒绝。四类shader破坏负控均实际失败，恢复后最终GREEN；CPU/native最新`173358-690-e9d300fa90d74a2ba12e116dc4e9350b`通过。投影guard只证明乘加范围保护，不宣称任意后续GPU数值绝对安全。正式controller/builder、Task3提示层/设置/整关/录像与实时性能仍未完成，故Task2/3整体不勾完成。后续keyboard/fragments聚合出现未定因GCHandle原生资源异常，当前全套不GREEN，详见阶段记录。无付费生成、自动化、提交或发布。

后续正式接入预检（2026-10-10 18:12；只读方案，未实现/未运行）：最小真实scene测试从保存的PrologueLevel与隔离IO装配开始，不在fixture现场调用builder或注入替代fog。分为Title→Playing实际layer1/灯位；Coordinator应用pose后到FramePreDraw间的灯位/相机/zoom同帧投影；Pause/Resume/RecoveryCompleted之后新位置与新native快照。子Ready早于level.Player赋值，必须latebind；Profile可替换visual，不能无条件缓存VisualRoot/Formal。物理像素映射沿用已测`(viewport.GetFinalTransform()*world.GetCanvasTransform()).AffineInverse()`；同时断言Coordinator.FrameObserverFailureCount为0，不能让下游异常被吞后误过。Ruling: Task3提示未就绪前正式雾保持关闭 — 现RoomFormalVisual保护区域只影响ForegroundOccluder，不是fog保护提示；成本为正式视觉稍后开启，避免遮住首关预警和落点。以上预检不勾Task2/3。

2026-10-10 18:38：上述最小saved-scene接入已实现并取得真实RED→GREEN。`183225-701-41b044f2d2334acaba3b2d854f142235`缺controller actual1，父builder准备`183558-959-7a11283d51934b9bb08c4045d3821913`pack/reinstantiate通过且仅允许父scene变化，最终`183701-156-3f9c66b83b3d4359842fc5decb251af9`actual0/stderr空/源与用户保护稳定。覆盖当前resolved Formal/Override、同帧灯位/镜头、native新快照、暂停/复活及observer0，正式Canvas仍hidden。方向平滑、controller同实例重入、全场可读性/设置/录像和实时性能尚无此次实跑证据，Task2/3整体仍不勾完成；详细SHA/边界在headlamp-fog-progress记录。
