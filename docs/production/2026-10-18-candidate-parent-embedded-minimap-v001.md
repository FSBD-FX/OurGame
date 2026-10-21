# 候选父场景与内嵌小地图交接 · v001

2026-10-18，Asia/Shanghai。权威 checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。这是已批准地图重构的隔离接线进展，不是完整序章、正式地图发布或最终美术交付。[前轮冰区玩家往返报告](2026-10-18-frozen-player-roundtrip-and-dock-clearance-v001.md)原件保留，SHA256仍为`D35FFB585CDDC900888C8DF09A4E562F72CDB72D482D42DB69DEDD941B3F6DBA`。

## 事实、推理、风险

事实：真实父场景现在可接收显式完整五区路径表，由同一批五区构建内嵌小地图。候选保存时只复制五个自有PackedScene快照到父场景内；五个作者叶文件另行保留。六文件原字节搬移后，实际加载的五区资源均来自新父场景的五个不同内嵌快照，实际SectorId／合同、全量重建catalog、132地形、五个冰区字面矩形及三个检查点一致。

真实父场景在独立World2D／SubViewport中执行Ready，隔离存档设置在入树前配置，保留Title、不自动开始。实际Minimap Model消费同一内嵌catalog；实际挂载Descent的300×40平台位于1690,2900，原生射线命中它。最终检查还验证实际Player存活且唯一；真实双Player、待删除副本、移除后恢复正例及真实伤害归零负例均使用同一判定。

推理：这完成了候选地图和父内嵌小地图的受检交接，避免只保存独立小地图却运行旧父catalog。但快照不是活引用：修改作者叶后必须检查并重新生成父场景。包仍引用本项目的Player／UI／脚本等资源，不是独立可发布游戏包。

风险：最终十一门仍6GREEN／5FAIL。正式保存地图仍127地形，正式父场景未覆盖；50新增身份仍全部Placement=null、正式新增0，24canonical子型仍Unimplemented。候选43联合选中／7未解、49曾配置不是49已选；两幽灵运动及一玩家局部路线不能推广为全50完整活动、正常Main／Explore或真人手感。完整目标未完成；本轮goal工具实际返回null，未据普通“继续”新建、缩减或冒称目标已完成。旧文档ACTIVE／usageLimited为历史时点。

用户确认使用其他窗口，继续不抢焦点、不启动前台游戏、不发OS按键／自动补键。真实userdata仅哈希清单保护109行，不读取语义。无新素材、付费、媒体展示、Git写入、上传、发布或自动化恢复；因此没有新图稿／视频可供归档，原媒体归档目录仍为`E:/OurGame/Knight/过程图稿与视频`。

## 实现与保存格式裁定

生产修改仅`tools/build/prologue/BuildPrologueLevel.cs`：抽出实际图构建`Build(optional five paths)`，显式表必须完整且每项为res://或绝对路径；默认仍加载原五区、127地形。Populate／Validate使用同一表，构建失败释放自有根。原`_Initialize`正式保存事务和原GC finally保持；没有新GC／缓存／借用Shape.Dispose／drain政策。

新增候选专用`SaveCandidate`只接受树外自有根及新的外部绝对输出，拒绝已有文件、项目及userdata路径；复制各Scene后只清自有副本ResourcePath，保存前再次重建catalog核对全部字节。未TakeOverPath，未修改缓存源Scene，未手写tscn或保存后重写路径。

曾试过RelativePaths转发，但真实搬移仍加载原绝对来源。安装版本Godot提交a13da4feb的[官方资源保存源码](https://github.com/godotengine/godot/blob/a13da4feb/scene/resources/resource_format_text.cpp#L1759)显示外部资源表直接写resource.get_path；RelativePaths用于另一fallback分支。这是依据实际失败和源码作出的候选包格式决策，不把旧失败豁免为通过。临时`SceneBuildUtil`改动已完全恢复，最终SHA仍`3EAACFA0C12C1AC99A3BA5691D29ABE9E38C9EF75BF2D98A9A6CDE6749CC0D44`。agent-reach只读Jina尝试失败后使用官方源码，没有安装或升级工具。

测试新增`resident-parent` phase和owner收据／严格诊断门。存活审查缺口及Frozen观察失败均先保留真实RED，再改测试；没有改Player／Motor／Claw能力、输入映射、跳跃、摔伤或地图几何。

## 准备阶段及先失败记录

案例根统一为`E:/OurGame/Knight/.evidence/october13-stable-claw`。下表目录名完整；原始stdout／stderr／external、契约、before／after、二进制及storage保留，不覆盖旧版本。

| 完整案例目录 | Build ms／exit | Engine ms／exit | 结果及边界 |
| --- | --- | --- | --- |
| resident-parent-20261018-094823-923-a5acc18b9e2748389187ea9489194aed | 18553／1 | 未启动 | CS0117 OwnWorld2D不存在；不是行为RED |
| resident-parent-20261018-094911-180-c86785b8e3e74ad79cb7313a11e34912 | 28552／0 | 1692／1 | 真RED：旧Populate忽略显式五路径，仍取正式Entrance |
| resident-parent-20261018-095128-820-f3d1aff10f0b4277acd2ee55b321d398 | 21828／0 | 1482／1 | 新Build五路径通过；搬移仍旧来源，测试finally错误audio-drain树外根，掩盖原断言并泄漏 |
| resident-parent-20261018-095227-007-01f59c83abb04a55b6fdcc6e51d64d8a | 20621／0 | 1444／1 | 只修测试自有树外清理；清洁搬移RED，无RID |
| resident-parent-20261018-095320-976-0fdef0e529194a59961a0452a1a4d046 | 26173／0 | 1861／1 | 只转发RelativePaths仍失败，无RID；停止猜flag |
| resident-parent-20261018-095803-182-8da80532ea774e10a7379739f2b7d3df | 24348／0 | 2412／0 | 五个自有Scene快照后首次严格GREEN；尚缺存活／唯一性证明 |

095128的测试清理错误另留audio异常和Body／Area／Shape／Viewport／Texture／Text／Font及Canvas／CanvasItem／ObjectDB／资源退出信息；这是失败产物，不是匿名资源拥有者的因果证据。只修树外根清理后095227保留原搬移RED，未修改通用teardown或隐藏退出诊断。

## 首次同源码十一门

| 完整案例目录 | Build ms | Engine ms／exit | 严格状态 |
| --- | ---: | --- | --- |
| resident-parent-20261018-095917-475-d5d8a06a7b9c4c8ea5753bb5e523b6b1 | 22148 | 2413／0 | GREEN，仅原窄父场景门 |
| resident-frozen-player-20261018-095947-023-63da20679f894eb89fe60747961c1f2f | 24038 | 5755／1 | FAIL，short_native_wall_pull观察失败 |
| resident-frozen-alcoves-20261018-100020-269-88ef8bf4144c4bff9e852e3e053aedfc | 26028 | 34691／0 | FAIL，一次独立Handle事件 |
| resident-geometry-20261018-100125-814-756a26576376416ba3ee99723d8bc728 | 24032 | 25395／0 | GREEN，原127／41候选9未解 |
| resident-scene-binding-20261018-100219-419-ff914ccf23ec405284e5c3b27bf372cc | 24092 | 7171／0 | FAIL，stdout154 Shape RID |
| resident-subtype-profile-20261018-100253-827-888be8ade3fc4a36aa1608479fb8592d | 25662 | 808／0 | FAIL，stdout31 Shape RID |
| resident-allocation-20261018-100323-430-e2295a3d2c4542d3a4f32c84532dc1a2 | 23549 | 529／0 | GREEN，spawned=0 |
| keyboard-20261018-100350-644-f2d16b537a31455595e31ac3764948cd | 23202 | 9731／0 | GREEN，非OS／真人 |
| actions-20261018-100426-910-442cff92c8bb485cb06aab8b1f816b25 | 24252 | 7229／0 | GREEN |
| prologue-pre-20261018-100502-110-7b4439b9672b4aa992171d5a494079ec | 33896 | 89271／1 | FAIL，旧Upper＋2 profile警告 |
| prologue-post-20261018-100709-947-489024da3ef34034a411bd4c977cbd07 | 25060 | 8146／1 | FAIL，KillZone Recovery解析＋旧Bell落地 |

首次5GREEN／6FAIL；独立只读三路审计为9事件＝7 ERROR＋2 WARNING，共16次行出现（含镜像），不是16个事件。退出两RID行仅stdout出现。session7930 exit0只代表串行终态。

## 审查修正与Frozen观察诊断

限定只读审查发现一个Important：Player引用相等不证明存活／唯一。真实保存Player独立挂入拥有的viewport，不绑定主父场景Recovery；用Health.ApplyDamage造成真实0HP，旧共用判定仍接受，取得RED。随后只改测试判定：先检查节点有效、入树、非queued，再检查实际Health生命及完整SceneTree player分组恰好一个／同实例；不先过滤死亡或待删除副本。另用第二个真实保存Player、queued副本及原生移除后的正例控制唯一性。主父场景仍Title，没有损伤／重置主角色。

Frozen原测试在等待Step之后采样短暂CurrentFrame.Arrived。失败日志已经实际Reel→Swing，但脚本错过到达窗口，继续按D直至120观察上限。生产模型只有真实input.Arrived才从Reel进入首次Swing，后续Swing仍发布当前位置的Arrived；这支持观察时序诊断，不支持增强牵引能力。

修正仅在ClawFrameChanged记录真实Arrived＋Swing＋无Failure＋非零CastId＋精确目标实例／runtime ID，按cast／target去重。Until仍要求同一当前cast、CurrentTarget、runtime ID、Swing、无Failure及当前姿态界限，不能拿旧到达事件跨cast冒充。回调只记录，不注入按键；原Until后release-D保留。Windup改为唯一CastId集合，要求两个不同真实cast／到达及一次boost，前轮延期Minor一并闭合。

| 完整案例目录 | Build ms | Engine ms／exit | 严格状态 |
| --- | ---: | --- | --- |
| resident-parent-20261018-100822-482-db10e8d211dc482484e5e785df4b23ee | 24473 | 1930／1 | RED，旧判定错误接受真实0HP Player，唯一独立ERROR |
| resident-parent-20261018-101001-622-a316e00ba7cb4698bc61543787e2d4ad | 28392 | 2689／0 | GREEN，存活／双实例／queued／移除正负控和真实父Ready |
| resident-frozen-player-20261018-101059-414-b50002cd91c2496a82fae825d9b19486 | 29573 | 5665／0 | GREEN，两个真实到达及局部往返，132观察samples |

限定复审确认原Important和Windup Minor闭合，未发现本scope新增Critical／Important；不是whole-plan或发布审查。观察收据证明“本cast曾真实到达且当前仍正确附着／姿态合格”，不声称当前仍处于arrival envelope。当前Player／Input／Motor／Tuning／Claw三文件、保存Player及CanyonContentBuilder的八个哈希均与首次串行一致。

## 最终同源码十一门

| 完整案例目录 | Build ms | Engine ms／exit | 严格状态 |
| --- | ---: | --- | --- |
| resident-parent-20261018-101204-467-26f219b41d6243679f4b3c17acc23753 | 23864 | 2312／0 | GREEN，真实Ready／内嵌catalog／native新平台／存活唯一Player |
| resident-frozen-player-20261018-101237-546-e7b3786798ca4c3d8a06e062abda01fb | 27301 | 5508／0 | GREEN，真实两到达、同角色局部往返 |
| resident-frozen-alcoves-20261018-101314-113-69800760c9964d649e78e2ec5020a7f1 | 27551 | 32016／0 | FAIL，三个独立Handle事件，功能收据不能覆盖 |
| resident-geometry-20261018-101418-014-2f25a37e94114ba8a6046759c90c94ca | 25137 | 26127／0 | GREEN，原127不是候选132 |
| resident-scene-binding-20261018-101513-273-8e78d06cd1ef4d24b8ae863511002840 | 26717 | 7291／0 | FAIL，stdout154 Shape RID |
| resident-subtype-profile-20261018-101551-201-fdeecf6b3c054d13939bb49ba945d728 | 25798 | 815／0 | FAIL，stdout31 Shape RID |
| resident-allocation-20261018-101621-078-0c86273e975c46e19685f897c469f6de | 26112 | 500／0 | GREEN，50身份／24子型／17负控，仍spawned0 |
| keyboard-20261018-101651-087-128026044a904f68aa2d1588670c9129 | 27749 | 9715／0 | GREEN |
| actions-20261018-101732-589-0069ef968e304a4a97750f54c6be5562 | 27336 | 7126／0 | GREEN，过热仍可攻击，气力实际扣减 |
| prologue-pre-20261018-101811-506-d0ac8397e4494c619cbebc217c558ac1 | 27168 | 89772／1 | FAIL，Upper lower_to_mid_swing＋两profile警告 |
| prologue-post-20261018-102013-427-df0836227b5e4105ab10646ebe79d8f4 | 26161 | 8436／1 | FAIL，旧Bell落地／Recovery解析 |

最终6GREEN／5FAIL；本轮31案14GREEN／17FAIL，其中一个编译失败未启动engine，其余30次普通fresh Rebuild均0警告0错误。没有timeout／guard失败／named waiver；focused55s、full180s、build180s不变。session34110 exit0只代表串行终态；所有自有build／native消费者终态后才写本报告。其他线程Unreal构建及godot-ai attach未干预。

独立只读三路完整审计：最终10个独立事件＝8 ERROR＋2 WARNING，8种完整headline（三个Handle共享一种），stdout2／stderr8／external镜像8，合计18次出现不是18个事件。两RID行只在stdout，其余九案external与stdout＋stderr完整行袋一致。pre的两profile警告来自负例，不据此证明正式视觉损坏，也不豁免严格门；Upper sample0.5／149 of150／落区穿越0。post两ERROR分别为KillZone无法解析ILevelRecovery及Bell WaitForFloor实体落地断言，坐标540.0704,1196.0139。各案provenance.json中的human／naturalRoute／wholeGoalAccepted仍为false，build／engine contract不含这三个字段。

最终Player八阶段：初始1800,3244；原墙1872.0803,3081.8225；借力1874.5803,3029.4443；顶端1875.5803,2735.1472；新台1800.6632,2843.932；侧道1899.9253,2974.161；回正／原出口1807.6747,3243.9336。真实arrival cast IDs为1、2；HP5→5、普通跳1／借力1／冲刺0／治疗0，总136个驱动观察sample，不是逐原生帧证书。机关回调不活动的局部fixture，不是正常世界、OS输入或真人录像。

候选调查JSONL8,897,207字节／20889行全部可解析，覆盖50 IDs／24子型／11区域，placement_certified及motion_certified全false。联合43／未解7不变：controlled-abyss.01、archive-side-cave.03／.04、gear-pistons.06／.07、steam-valves.02、pendulum-gallery.04。Gear20000搜索限额不是无解证明。两个真实幽灵巡逻收据仍保留，但此phase严格FAIL；三次Handle栈同为GCHandle.FromIntPtr→SwapGCHandleForType→GD.Load→CensusLeafSource.Configure，不猜拥有者或归因于父场景改造。

## 保管链与产物

根独立复核：准备六案29份manifest对／1140份选中文件对／30份binary；首次十一＋三个修正案70／2443／84；最终十一55／1875／66。合计31案154／5458／180，逐一哈希匹配、差异0。真实保护清单仅比较字节，未读userdata语义。每案source1675、user109；helper一般9，pre10。

最终十一共同source manifest SHA256为`A661ADF8E995E9B86F7707F5EFD79272C7EAC4A1DDAA3F6FE3B2D5307E50031C`；binary manifest为`2466096CD8871D06714BDF98689D7DD033195345786764055A7289039A25CC7F`；Knight.dll为`F0F2CBDE73ABE358A2E5FC26C05712DABADBAE17378CF8174B036A9ADD8CFB81`。helper9 manifest为`CB6F9BDC4ACD6AB731325B9D17F6CF26EAD9357302D43191B8A160B7A10CDFE5`，pre helper10为`45AB9A6C4B8BDBCF6488C47B0271E0432CF4267D7974EC4E0961169B42B02404`。

最终父场景case的`storage/candidate-parent-source`和`storage/candidate-parent-relocated`六对文件，根再次独立检查SHA完全一致：

| 文件 | 字节 | SHA256 |
| --- | ---: | --- |
| CanyonEntrance.tscn | 15487 | 364FA90445E23D228C99F76585CD72BB76372752DFFC188AA7DA0C02C0F2930D |
| CanyonDescent.tscn | 77083 | F7FE0D064485950AB17DA2745CEE25B0F8457BC3790B215ED11DBDF0A6551B15 |
| CanyonBasin.tscn | 73075 | 86FEAF2AE20B76ED4CD477654E6734DDD85F846C389E6C6EA400F0B0C48AE19C |
| CanyonAscent.tscn | 104954 | E1508AD874F81919C473194816FB4759C9E082564F908051CF3638F0EE6AC35F |
| LostTemple.tscn | 49544 | 7C928B52CEC2CA48202C6CF4A68088CF1B327D6CCE088E090C2AFE649BC34CD6 |
| PrologueLevel.tscn | 898731 | A9B2CA44254FBB9CDFD76803F5CD9CA9703112AEF08930B86C8E199A484A4AFF |

当前最终源码与该批before／after相同：BuildPrologueLevel `0673A4A481063B378A130F1B00FE7716A5B03DC0E95FFDFEA95C9F5D2DB72592`；CanyonResidentPlacementTests `CF4BDC53F0D5954C56561302DA537B5E5AC9F13BEC78247F06793F6198A630E1`；RunStableClawPullTests `E13F92C672D7DA24D4E9071AAE39619D0C134B6C04772D6F352E9E2D57E2F443`；owner `14AB3C23B9F9D91D5B801BFB6F5A91B828F3F11B156D36671C012EBE47321F86`。owner AST错误0，限定diff whitespace检查0。

正式Descent仍`E091C51C8D46ED117FB6020DBED64750914B72D9544856E179A7CE0ACB9D1CD6`；正式父场景仍`3427696E7521BB6471E184CC0BD6F3CF343AC990462B2075A810E58357C19640`。没有覆盖正式资源或把隔离产物当发布版本。

## 独立审查裁定与后续

1. 接受Important并以真实死亡RED／唯一性正负控闭合；不是只改成功marker。误判成本是失去角色仍被报可玩。
2. 接受Windup唯一cast修正；事件记录与当前附着／姿态合并，不把Swing本身当到达。误判成本是漏掉真实操作失败或用旧cast冒充。
3. 五个自有内嵌快照是明确候选包决策，不是未检查外部路径的fallback。代价是编辑叶后须受检重建；不承诺热更新或独立游戏包。
4. 树外测试清理修正符合自有根生命周期，不豁免资源门；SceneBuildUtil尝试已恢复。匿名Handle和154／31 RID仍未关闭。
5. 保留正常Main／Explore、旧档、活跃机关／旧AI、显示帧率、完整美术／音效、真人和短录像全部门。Title Ready不替代正常通关；不作whole-plan／Task1–4完成标记。

下一步先在同一132候选验证旧Frozen四个敌人身份的实际活动兼容（上Moth／Sentry、下Rust／Moth；Sentry有主／替代位置，所以五条记录不是五个同时活跃敌人）。准备建议为上段两控＋下段一控，真实玩家、原AI／投射物／碰撞，不改旧出生点或生产参数；当前未实施／未运行。随后处理七个逐身份缺口、全50活动／攻击／返航／玩家接近及world-lifetime manager。资源问题需有区分力的拥有者／生命周期隔离，不改GC／cache／借用Dispose或反复等偶然绿。全关旧26＋新50＝76普通＋1Boss、骨骸10／幽灵10和ECHO一个引导身份保持。
