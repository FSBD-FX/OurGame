# 冰区玩家抓爪往返与停靠净空修正 · v001

2026-10-18，Asia/Shanghai。权威checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。这是隔离地图候选的局部运行进展，不是完整序章交付或正式地图发布。上一轮[冰封侧龛报告](2026-10-17-frozen-resident-alcoves-native-v001.md)原件保留；其中60px、query-only和20901行是当时的历史结果。

## 事实、推理、风险

事实：原60px侧道能容纳40px身体，却不能容纳现有机械爪的48px墙面停靠距离。墙x1920、停靠中心x1872、身体左缘1852，与旧平台右缘1860重叠8px；真实完整Player胶囊原生查询拒绝。只把下侧平台宽320收为300、左端1540不动，右缘变1840，侧道变80px，停靠身体留下12px净空。同一查询从False变True。

随后真实保存的同一个Player，经过游戏键码输入、原Motor／Claw及原生物理，完成起点→抓原墙→一次空格借力→抓原顶部→落新平台→沿侧道返回原出口。最终HP5→5、普通跳跃1、借力1、冲刺0、治疗0。没有运行中写角色位置／速度／血量，没有修改机械爪能力、跳跃、摔伤或地形碰撞政策。

推理：可以确认停靠净空缺陷及这一条局部往返路线；不能把平台修正说成修复长距离Travel。仅改平台后，旧操作脚本仍失败；后续根据真实墙面切向借力改用一次A＋空格、回停靠线再W／K，才完成路线。这个区别说明还需要正常关卡中的操作引导与手感验收。

风险：此测试停止了既有地图fixture的机关回调，Player自身输入／Motor／Claw仍运行。它是机关不活动的局部路线，不是正常Main／Explore、旧敌人完整AI、真人键鼠或实时显示60帧。完整回归仍4FAIL，正式地图仍127地形、父内嵌小地图未接线，50新增常驻敌人仍全部Placement=null／正式新增0，24canonical子型场景仍Unimplemented。

继续遵守用户确认的其他窗口使用边界：不启动前台游戏、不抢焦点、不发OS按键或自动补键。真实存档仅字节保护，不读取语义。完整目标未完成；本次goal工具实际返回`usageLimited`，未修改状态或冒称工具仍ACTIVE，也未新建／缩减目标。取消的自动化不恢复。

## 本轮实现范围

生产只改`CanyonContentBuilder.cs`中的`FrozenLowerResidentShelf`矩形为`1540,2880,300,40`。沿用`generate2dmap`的明确碰撞／同尺寸skin／结构分层原则，通过现有Solid入口生成，未手写tscn、付费生成或新增贴图。既有五块围合结构保留；第二只幽灵完整巡逻身体最右1794，距新右缘仍46px。

测试增加`resident-frozen-player` phase，真实builder保存132地形候选，以显式五区路径表挂载；使用真实`Player.tscn`，只在入树前指定起点1800,3244。每次驱动检查同一实例、实际输入序号递增、存活／无治疗和局部边界。落台检查实际slide collider身份及朝上法线，不用单纯坐标代替碰撞。

既有候选门扩为四条完整胶囊查询：自由移动线x1890及真实停靠线x1872，各上下两个方向；临时扩大fixture自有平台形状88px，令侧道缩为36px时必须拒绝，再精确恢复80px。只改fixture自有克隆，不释放或改写借用缓存形状。这个门本身仍是query-only，实际玩家运动由新phase单独证明。

owner保持focused55s、full180s、build180s，要求实际停靠净空收据、八个路线阶段、保存产物、正常退出及无原始错误；不允许命名失败豁免。旧类注释的query-only误述已在这次源码改动中修正，不能据此声称更多AI通过。

## 先失败与操作诊断

案例根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。以下失败原件全部保留；观察超时不当终态，所有原生／build消费者终态后才更新本报告。

| 完整案例目录 | Build ms | Engine exit／ms | 实际结果 |
| --- | ---: | --- | --- |
| resident-frozen-player-20261018-085427-786-497b6504e38c4a06a9b235b955e9fd4f | 31028 | 1／2103 | 过早松空格，角色落出局部边界 |
| resident-frozen-player-20261018-085613-533-82a39ad76b5a43bfa507e48a1803806f | 25614 | 1／1894 | 仅加观察：Windup后到anchor621.21＞620，真实OutOfRange |
| resident-frozen-player-20261018-085721-841-3727037efb444f588344ba70e43b5ef2 | 25768 | 1／2031 | 持续跳跃消除OutOfRange，但长Travel尚未到达就落出边界 |
| resident-frozen-player-20261018-085917-509-5f6f41e0e9e8463e90d145b3f246f48a | 25848 | 1／2793 | 短墙拉近实际到达；D＋空格选择向下切向，后续顶部Travel仍失败 |
| resident-frozen-player-20261018-090144-703-0cbaac935abb4afa857af0bffdab96c0 | 30341 | 1／1516 | 字面x1872完整胶囊原生净空RED：corridor_native_world |
| resident-frozen-player-20261018-090327-943-e74bcae4af2e4365b2d254749b845084 | 23091 | 1／2575 | 只收短平台：净空转True；旧D借力路线仍失败，两个问题分开归因 |
| resident-frozen-player-20261018-090457-636-3c40267e4b6348aca4aad6b86bee6e72 | 32155 | 0／5860 | 正确一次向上切向借力后首次严格GREEN，同角色完整局部往返 |

前四案使用原范围620／弹头1800／拉力720，没有加能力；这些操作脚本失败不能证明正常关卡不可能通过。090144→090327才是单一生产改动的净空RED→GREEN因果证据，后续操作调整不是对长Travel的生产修复。

## 最终同源码十门

| 完整案例目录 | Build ms | Engine exit／ms | 严格结果 |
| --- | ---: | --- | --- |
| resident-frozen-player-20261018-090732-974-3e5a70e594aa4d5898c72b0dacaada2d | 22820 | 0／5624 | GREEN：同角色抓墙／借力／抓顶／落台／返回 |
| resident-frozen-alcoves-20261018-090806-888-0e74b399840544b0a206a89665b9d210 | 24590 | 0／34548 | GREEN：两幽灵真实巡逻、四方向胶囊及36px反例、全50候选调查 |
| resident-geometry-20261018-090910-399-08924820284a4aa785201e299bbb4a98 | 24799 | 0／26190 | GREEN：正式旧127地图／41候选9未解，不是132候选的第二份验证 |
| resident-scene-binding-20261018-091004-933-7736bd69140a41ed97adb75e3271c648 | 24809 | 0／7282 | FAIL：行为符合，但stdout154 Shape RID |
| resident-subtype-profile-20261018-091041-152-e3cc1a08d5144b6dba79e43594fa9a79 | 27851 | 0／1072 | FAIL：行为符合，但stdout31 Shape RID |
| resident-allocation-20261018-091115-000-5327b30b082744d7898dd5d33163156b | 25259 | 0／540 | GREEN：原名册／全部未入图 |
| keyboard-20261018-091144-817-6ba1560ff08548abb5bf79cbf265d6e3 | 23287 | 0／9732 | GREEN：游戏输入链，不是OS／真人 |
| actions-20261018-091221-562-3a7226ee0e0c4dfa99c0afe14ec17039 | 24231 | 0／7122 | GREEN |
| prologue-pre-20261018-091255-991-90c2cddd6ae644ed9202bf06c57f5438 | 24455 | 1／89377 | FAIL：旧Upper lower_to_mid_swing错台；另2 profile WARNING |
| prologue-post-20261018-091454-227-06e0fb2d872644f7b80e64051145dbe4 | 24102 | 1／8232 | FAIL：旧BellChainWaitForFloor及一条ILevelRecovery ERROR |

最终十门6GREEN／4FAIL；本轮全部17案7GREEN／10FAIL。17次fresh ordinary Rebuild均0错误0警告，无timeout／guard失败／named waiver。外层session6846的exit0只代表串行终态，不代表全部通过。

独立只读审计完整读取最终十案30份raw，并按镜像行去重：共7个诊断事件＝2 warnings＋5 errors，两个RID错误共185个Shape，**不是185条错误**；退出RID行仅在stdout，其余由external镜像。pre两条警告来自Presentation负例（VisualRoot接口错误、Hurt cue不是Node2D），不能写成已证明正常美术损坏，也不据此将严格失败豁免。Upper实际sample0.5／149 of150、final264.99634,160.6107；post一条Recovery解析错误和一条Bell落地断言分别保留。所有native PID／contract一致，全部human／naturalRoute／wholeGoalAccepted为false。

本次没有FATAL／Handle异常，不关闭历史匿名资源析构问题。154／31是各自stdout报告的唯一Shape数量，不因stderr／external镜像重复相加。pre／post的旧失败仍阻止完整验收；不抑制日志，不改GC／cache／KeepAlive／借用Shape.Dispose／退出drain，不换verbose／fixedfps／movie或扩大时限。

最终玩家八阶段收据：初始出口1800,3244；原墙拉近1872.0803,3081.8225；借力入侧道1874.5803,3029.4443；顶部到达1875.5803,2735.1472；新平台1800.6632,2843.932；沿侧道下落1899.9253,2974.161；空中回正并落原出口1807.6747,3243.9336；同原出口完成。最终136个**驱动观察sample**，不是逐原生帧认证；未称每一物理帧都取得完整脚底支撑。

## 两幽灵与全50候选的当前结果

两只幽灵仍分别实际巡逻360个观察物理帧：X范围1437.5962..1562.4038、1649.5962..1774.4038；最大单步0.8000488，落地／访问两端／回起点、HP3保持。只有这两只在本phase取得运动证明，不外推为50只通过。

全50身体身份／24子型参与，49身份曾factory配置、覆盖24子型；联合选中43／未解7。原预算下1616走廊查询、20228联合节点；Gear共享区域搜索到20000限额。20889条诊断（此前20901条不再当当前值），不是已完成数量，也不表示搜索上限之外无解。七个未解为：

| StableId后缀 | 原停止原因 |
| --- | --- |
| controlled-abyss.01 | no_sampled_template |
| archive-side-cave.03 | candidate_set_unselected |
| archive-side-cave.04 | candidate_set_unselected |
| gear-pistons.06 | joint_budget |
| gear-pistons.07 | joint_budget |
| steam-valves.02 | candidate_set_unselected |
| pendulum-gallery.04 | candidate_set_unselected |

共享桶stable_ids不是逐身份原生拒绝证据；配置过49不是选中49。所有全50诊断的placement_certified／motion_certified仍false，实际两幽灵及一玩家路线收据独立保留。

独立全量JSONL解析0坏行：候选20889条、backward accepted覆盖49身份、5条not_selected＋2条budget_stopped；正式旧地图20594条、对应47身份、7＋2。逐身份stdout核对七未解如上，旧地图另含frozen-detour.01／.02的no_sampled_template；两份均search_capped，不能把共享桶成员或搜索限额误作地图不可能性。

## 保存位置与保管链

最终090806的`storage`保留候选Descent、候选小地图及全50拒绝记录；090732另保留玩家测试所用Descent。它们是分别保存的真实产物，不能混用UID不同的文件哈希。24候选叶／24作者配置及失败日志仍在各case内，未覆盖上一轮。

| 产物 | 字节 | SHA256 |
| --- | ---: | --- |
| 090806/storage/candidate-CanyonDescent.tscn | 77081 | 14CB96A0780B40F4E411526A84A9DB1FDDBE702F850EB6748F3FA643249ACE19 |
| 090806/storage/candidate-minimap.tres | 446888 | BDEF7DFC23D33C6675462F3FEED6114CF6A687D5F129D787D7AF1ED8ED43258B |
| 090806/storage/candidate-leaf-geometry-diagnostics.jsonl | 8897207 | 20ABB12CC16E5FF2513F5D812549CABAA4258D1776D099ECE58A673B80D5E28E |
| 090732/storage/player-candidate-CanyonDescent.tscn | 77091 | 757B3CC067E574F65F240DB7E766A6458981A5454A1AC01BAA24B53132676BEF |

最终十案共同source/resource1675 manifest SHA256 `49B41A256BBC4AE47E306102D1D48B223849A6A33A43B98B13AED2491EC1039D`，binary6 manifest `9F9BFCE3DC37FDED59DE51282EBBB41019A9AB90DE5AC368922EA165448366FB`，Knight.dll `63DF77A85E6B01FB858CFEACB37E64E30F309D9EA5EE3CF541909A30EA11ACB4`。helper9 manifest `6A2D2CFEA8D32052D05930848C9E2C9A81A795ED739B26835B11D2DC8AC028E9`；pre增加一helper，manifest `B24955D1C0AAAF5432F5FF24063BED5575854F76A89474DAEB1C6D48C592339E`。

本轮前七案35份manifest对／1316份选中文件对／42份binary；最终十案50／1685／60；合计85／3001／102，逐一哈希匹配、差异0，每案source1675／真实用户保护109。当前三个C#及owner与最终090732前／后副本一致：

| 当前文件 | SHA256 |
| --- | --- |
| tools/build/prologue/CanyonContentBuilder.cs | 6E39AE501EFD0CEF9DD7F10CA99D66BBD4EB4A8244765BB6BC71AFEEC8AC1A99 |
| tests/integration/CanyonResidentPlacementTests.cs | F864411FAACEC2050E30B0E1581A6CDC817C3D37B889B560EE284B26F78C32F4 |
| tests/RunStableClawPullTests.cs | 393C64CE12673066D85F998565247459B416F099B64344B94EAF1C20F2CEA4B1 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 739EA7113AD2FCE453F6C7366EA5183CC160C6A24AD2C01D3E89989625906F02 |

SceneBuildUtil保持`3EAACFA0C12C1AC99A3BA5691D29ABE9E38C9EF75BF2D98A9A6CDE6749CC0D44`。正式Descent仍`E091C51C8D46ED117FB6020DBED64750914B72D9544856E179A7CE0ACB9D1CD6`，正式PrologueLevel仍`3427696E7521BB6471E184CC0BD6F3CF343AC990462B2075A810E58357C19640`。没有把隔离候选直接覆盖正式资源。

## 限定范围独立复审与裁定

独立只读复审对照上轮051745保留副本及本轮四个范围文件，不将整个dirty checkout当本轮diff；未运行build／engine或修改文件。未发现可证明的新增Critical／Important，不是whole-plan／发布审查。

Minor（延期）：测试`casts`按Windup帧发布次数累计，不是唯一CastId；`casts>=2`单独使用较弱。当前路线另有两个不同真实目标到达断言，不因此成为虚假路线通过。下次必要C#改动时改为唯一CastId集合，不在最终运行后改源码冒用旧哈希。

逐项裁定复审保留的边界：

1. 不是虚拟通道放行：四方向完整胶囊及自有36px反例／恢复均有效；错误成本是停靠碰撞漏检，因此保留负例。
2. 不是假爪或假返程：不同实际目标、借力身份及新旧floor collider接触独立约束；错误成本是走错平台被算成功，因此不删接触断言。
3. 机关不活动是明确局部fixture边界，不是正常世界通过；错误成本是上线后危险时序阻路，所以正常世界仍OPEN。
4. 引擎键码覆盖实际游戏输入链，不是OS／真人手感证据；错误成本是焦点／上手问题遗漏，所以真人验收仍OPEN。
5. 存活／无治疗允许HP下降，不等于所有路线无伤；本次实际5→5，不能推广到全关。错误成本是错误承诺无伤。
6. 20px平台修正只证明停靠净空，不证明长Travel修复；旧脚本仍失败，操作调整另记。错误成本是把能力或引导缺陷误归地形而漏修。

## 下一步

先完成受保管的候选父场景／内嵌Minimap交接与旧活动兼容：父builder目前硬加载canonical五区并硬校验其ResourcePath，不能只覆写Descent和parent两文件跳过验证。候选应带全部五区的明确引用，以同五区重新生成并嵌入小地图；包搬移后按包绝对路径解析同级相对引用，实际挂载后确认引用来自新包，不借旧case外部candidate-minimap.tres冒充已接线。正常默认引用保持不变，不在通过哈希之后重写产物。

随后处理其余七处精确几何／联合冲突，完成全50真实活动／攻击／返程／玩家接近与world manager。Controlled侧棚200→240建议仍只读未执行，不改共享center2160边界。资源拥有者须可区分生命周期证据，不能凭匿名数量或偶然未复现关闭；旧Upper／Bell与恢复祖先仍需先复现再修。

旧26＋新50＝76普通＋1Boss、原生8／废弃12／精工10／骨骸10／幽灵10、24子型及ECHO一个指引人格保持。Task1最终50／Task2／Task3／Task4全部OPEN；正常Main及Explore同角色标题至Boss／有支撑出口、旧档、实时显示60帧、最终统一美术／音效、审阅归档短录像与真人反馈未完成。没有task-done／whole-plan完成／Git写入／上传／发布／自动化／新付费，也没有十万付费结果保证。

本轮没有对用户展示新图／动画／视频，故未新增媒体展示副本；之前的过程原件继续保留在`E:/OurGame/Knight/过程图稿与视频`，按该目录README及索引管理。
