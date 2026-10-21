# 真实子型场景点位检查与拒绝证据 v001

2026-10-17，Asia/Shanghai。权威checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。这是后台测试实现进展，不是新增50敌人可玩、正式美术或完整关卡交付；目标ACTIVE。

## 事实、判断、风险

事实：用户确认完整探索测试时在使用其他窗口，与该次失焦清键一致；不外推为全部输入问题已解决。本轮无窗口、不占键鼠、不读写真实存档语义。

旧点位调查读取旧Probe承载场景。现在同一个隔离case先构建24真实子型叶，再读取各叶导出身份和真实BodyCollider；旧26身份的36份主／备位置保留记录仍使用旧场景。活动模板只有经过原静态保护筛选后才生成临时Placement，以真实采样点和真实route territory交给实际factory；每次创建新的挂树前actor，用它执行原生双向走廊检查并释放。联合选中身份另逐一重新配置、原生复核。没有null→原点、重复配置缓存actor或替换旧敌人。

判断：043039原生检查完整通过，50身份读身体、24子型参与，47身份曾实际factory配置、24子型全覆盖，联合41候选／9未解。但最终同源同二进制几何复测在2112ms发生匿名析构FATAL，只执行到Archive收据，未到身体census；不能宣称稳定通过或资源已修复。

风险：47 configured包括随后原生查询被拒绝的尝试，不等于47个合法点位；JSONL中的stable_ids按route/subtype共享桶展开，也不是每个身份都执行了相同查询。真实Ready／AI运动／攻击／返航／玩家通路／最终50正式入图未完成。所有canonical Placement仍null，24 canonical SceneState仍Unimplemented，新增正式敌人0。

## 先失败的真实反例

042533在实际保存的新Rust叶BodyCollider仅X加9100；actor查询pose仍(1100,500)，独立真实薄墙在(10200,500)。旧入口错误返回clear身体(1082,468,36,64)，没有native_world；20734ms严格FAIL。改为实际保存叶后，字面身体(10182,468,36,64)正确撞墙并拒绝。未改Shape、墙或地图，也不先伪造Placement配置这个偏移控制。

新增四个拒绝控制：合法真实叶的null Placement；旧无binding承载场景保存到新叶文件名；同body／正确其他字段但错family；真实Scatter身体与完整正确Laser配置／路径。最后一项先证明两者在真实庭院点(5153,1211)均有相同clear字面身体(5128,1172,50,68)，再要求factory因实际weapon carrier不符而拒绝。另检查同点factory返回不同off-tree native actor，以及旧36保留记录不变。

一次开发构建CS0136（局部actor重名）已最小重命名修正；该构建失败不算行为RED。无生产资源／地图／战斗参数修改；SceneBuildUtil及builder实现保持上一轮版本。只抽出测试包构建入口、接通几何测试及加强owner收据。

## 真实未解点位：保留原因，不猜地图无解

记录来自043039的`storage/leaf-geometry-diagnostics.jsonl`：20594行／8798430字节／0解析失败，44个route/subtype桶、50个StableId。静态拒绝保存首命中KeepOut及bounds；原生拒绝保留raw Issues；outward失败后的backward明确not_attempted。保留原8192描述／96桶查询／3000总查询／16候选／20000联合节点限制，未提高预算。

R/K＝route越界／首命中KeepOut的静态拒绝记录数，不按共享桶身份数重复累计。raw Issues可一条包含多个，次数不等于拒绝记录数。

| 未解route/subtype；ID后缀 | R／K | outward拒绝记录 | 当前accepted模板 | 实际停止情况 |
| --- | ---: | ---: | ---: | --- |
| ControlledAbyss/lost-echo；controlled-abyss.01 | 274／31 | 11：start.native_world 4、end.native_world 7 | 0 | 无采样限额；未选中 |
| FrozenDetour/lost-echo；frozen-detour.01/.02 | 273／43 | 0，未进入原生 | 0 | 无采样限额；未选中 |
| ArchiveSideCave/bone-charger；archive-side-cave.03 | 230／23 | 0 | 3 | 当前候选集合未选中，不是无原生候选 |
| ArchiveSideCave/bone-diver；archive-side-cave.04 | 389／562 | 74：start 24、end 38、corridor_native_world 22 | 1 | 当前候选集合未选中 |
| GearPistons/scrap-heavy；gear-pistons.06 | 235／25 | 16，start/end各16 | 16 | option_limit；同一region联合搜索耗尽20000 |
| GearPistons/precision-heavy；gear-pistons.07 | 235／25 | 16，start/end各16 | 16 | option_limit；不是第二次独立region耗尽 |
| SteamValves/scrap-wanderer；steam-valves.02 | 155／90 | 0 | 12，共享桶两身份 | 当前候选集合未选中 |
| PendulumGallery/precision-laser；pendulum-gallery.04 | 215／58 | 0 | 16 | option_limit；当前候选集合未选中 |

Frozen首命中43条分别为frozen-detour-to-basin-camp launch14、旧frozen-lower首怪12、SteamDiverterFrozen LeftJet7／RightJet4、archive-to-frozen-detour landing6。Controlled首命中31条为abyss-floor残片15、abyss-to-basin-camp launch10、旧controlled-abyss第二怪5、basin-camp-to-abyss landing1。它们是首命中记录，不是所有相交来源或因果判定；精确SourceId保留在JSONL。

全文件586条backward accepted记录按桶展开覆盖47身份，不能与实际factory配置集合混同。停止记录31 option_limit、1 bucket_limit、2 joint_node_limit；没有description/global_limit。Frozen重复joint记录按桶仅计一次。下一应定位Controlled的原生具体collider；Frozen围合侧龛／通路保护；Archive/Steam/Pendulum具体联合占用witness；Gear候选冲突图，不能直接称地图无解或只增预算。

## 原生案例与最终回归

案例根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。完整目录与实际等待结果如下；所有失败原样保留，没有named waiver。

| 完整案例目录 | Build ms | Engine exit／ms | 严格结果 |
| --- | ---: | --- | --- |
| resident-geometry-20261017-042533-416-a79acb5e81b24d9ea4d6c897be34a90f | 28579 | 1／20734 | FAIL：真实9100保存叶被旧Probe错误替代 |
| resident-geometry-20261017-042936-081-5872a979750d4191852664ef42357530 | 20987 | 未启动 | FAIL：CS0136；无新binary，预期保留1个binary guard，不是行为RED |
| resident-geometry-20261017-043039-651-e1c926d41fb1455cb5ff382b889bb329 | 22953 | 0／23309 | GREEN：新实际叶／4拒绝／41候选9未解；原28身体／29走廊／51环境／57接近／solver4／distribution5等保持 |
| resident-geometry-20261017-043241-185-7916d2db1d7b4630a053f22da515afb0 | 23248 | -1073741795／2112 | FAIL：gchandle.is_released FATAL；Dispose→Finalize，没有对象身份，未到census |
| resident-scene-binding-20261017-043311-626-6db286c3a1ea485a87f9d61a8be96316 | 32436 | 0／7683 | FAIL：原24／迁移／所有权／7negative符合，但stdout154 Shape RID |
| resident-subtype-profile-20261017-043355-851-4c58aabc65784bb59c482d922d39d86f | 26547 | 0／1341 | FAIL：原配置／14negative符合，但stdout31 Shape RID |
| resident-allocation-20261017-043428-170-278bf1c4ea654ff99b54f80261ebedf6 | 26416 | 0／642 | GREEN：原50身份／24子型／17negative；仍全部未置 |
| keyboard-20261017-043459-616-8ce2025a85924220bb38dd1d93e391ee | 22070 | 0／9569 | GREEN；引擎输入，不是OS真人试玩 |
| actions-20261017-043534-510-bd31ca096bcf4ea689078fef047ed281 | 28413 | 0／7121 | GREEN |
| prologue-pre-20261017-043614-225-62735aadd8f84169b1fa6afd0a9e3ee9 | 20851 | 1／89145 | FAIL：旧Upper lower_to_mid_swing sample.5，final264.99634,160.6107／149-150；2 profile WARNING |
| prologue-post-20261017-043807-662-53bf45c0132c41b0ad01de9b47cb7c83 | 24950 | 1／8338 | FAIL：旧Bell落地540.0704,1196.0139；一条ILevelRecovery解析ERROR，external重复 |

最终同版八门3GREEN／5FAIL；全11案4GREEN／7FAIL。串行外层exit0只说明终态，不等于全通过。除那次已记录的构建失败，十次fresh Rebuild均0W0E；无timeout、其他guard失败。focused仍55s、fullpre/post180s、build180s，不延时掩盖失败。没有为了重现或消失而重复资源崩溃彩票。

043039及最终八案共同source manifest SHA256 `07E9B68F904CE80B405499AD7A93D60A37FA1731A05EBF9A6632B2F8EA516ED5`、binary6 manifest `E75EDA3C41CE3270C134E34FE247DB358348F1A4944538B25AF31656C5C5577B`。首次GREEN后owner仅增加实际叶收据要求，helper manifest从909C…变为最终9份`3EECF07DB00940A5CF40440C92072843C5365271BEAB03DD94FBB7F108238193`；pre10份为`3EE737B6C53307103A7CF692A9D1DF4DBF10A45CB33E7AC7289EFF1B348E17AB`。不混同helper版本。

11案每份完整库存1675／保护109；54份before-after manifest对、1869份before-after选中文件对、60份保留binary均独立核对0差异。真实userdata只hash。所有消费者终态后才编辑本报告／状态。

## 保存位置、边界与下一步

043039的storage保留24作者输入、24叶＋24作者输出，以及实际changed-body／unbound／wrong-family／wrong-gun控制文件。20594行拒绝记录与原生stdout原件未覆盖。

- JSONL SHA256：`4F0813B91C7B1E683CA30ED38E9E5AF74B869A198AC1477E0019BACDCC20B732`。
- stdout SHA256：`E119D2B3C1D891D2102DBFB5831CB3EA90345A21BAA762A5A85A4B87F4CE0092`。
- CanyonResidentPlacementTests.cs：`BD6FC3C4AFBDDA809355549FAD751C162E16E78C61AAB5C98D282F79BD8D4645`。
- ResidentSceneBindingTests.cs：`97EE648C037B9FD53F075CEFAFCE296859637E507CD89BEDB76B67CE028C71FD`。
- runner：`A9A578DB3E01534C44CE1537109FE9CF25191B98B6A7A30C08CD477501D5248A`。
- owner：`00304DFE22148F968CA9EBD6FF9EC0DA3B5FE98B06DF949C5DF38156CCB2F0D5`。

自查尚缺：身体身份计数与CheckBody目前分开，虽然当前源码实际用正确Raw，后续应合成测试私有QueryBody，校验被查询actualActor的真实SceneFilePath／binding，再计入独立bodyTypes；用“活动Raw已24但身体传旧Probe”的真实反例保护，不能仅靠计数或helper单测。静态记录尚不含所有碰撞来源，联合未选中尚缺具体重叠witness，原生raw issues尚无collider身份；before-Ready配置不证明实际运动／攻击／返航。

下一是这些最小证据缺口与必要的围合侧龛，再全50真实活动／攻击／玩家接近／保护及manager。匿名资源析构须有可区分的拥有者／生命期证据才修，不猜为某Shape、GC/cache、保存工具或计数原因；上一轮的私有PackedScene修复不关闭本次FATAL或154/31。

旧26＋新50＝76普通＋1Boss、Bone10／Ghost10、ECHO一指引人格不减。全Task1最终50／Task2／Task3／Task4均OPEN，不勾全部完成或whole-plan review。正常Main/Explore、旧档、真实显示60帧、最终美术／音效／归档录像审阅／真人反馈及原Upper/Bell失败门均保留。本轮无新图片／音频／视频展示、无付费／Git／发布／自动化／前台抢键；未把十万付费人数当作已证实成果。

