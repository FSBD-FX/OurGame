# 冰封侧龛与两只幽灵巡逻候选 · v001

2026-10-17，Asia/Shanghai。权威checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。本轮是隔离地图候选及原生运动进展，不是正式入图或新试玩交付；完整目标ACTIVE。

## 事实、判断、风险

事实：用户确认当时正在使用其他窗口，与该次失焦清键相符；不能据此排除其他输入故障。继续无窗口检查，不发OS按键、不切前台、不自动补键。真实用户数据只做字节清单保护，不读取存档语义。

在同一个case中经真实builder生成、保存、重载Descent候选，以显式五区路径表挂载；新增三段围合石壁和两处有支撑的侧龛。两只计划内`lost-echo`幽灵读取实际保存子型叶，经过真实factory配置并进入原生Ready／Physics，各观察360帧，均到达巡逻两端、回到起点、持续落地且HP保持3，没有运行中写位置、补血或伪造物理步。

判断：候选地图132实体地形（正式保存地图仍127）下，全50身份重新调查得到43联合候选／7未解，比原地图41／9减少两处缺口。49身份曾经过factory配置、覆盖24子型，不等于49已选或已入图。只有这里两只幽灵取得实际巡逻运动证明；不能外推43个候选的全部AI／攻击／返回已通过。

风险：Player的完整40×72胶囊双向查询能通过60px侧道，36px反例会拒绝、恢复60px后再通过，但**没有Player实际运动或机械爪返程证明**。旧五份冰封主／备用出生记录身体保持可用，不是旧敌人完整飞行／战斗回归。父场景及运行时内嵌小地图尚未接上候选；全部canonical Placement仍null／24 SceneState仍Unimplemented／正式新增0。

本轮按`generate2dmap`的侧视地图／碰撞／物件分层指导复用引擎内灰盒，未生成新贴图、音频、最终族系美术或付费素材，也没有对外展示新媒体。

## 改动与先失败证据

生产改动只在`CanyonContentBuilder.BuildDescent`加入五个`Solid`，共用既有碰撞、同尺寸skin、结构标记与GripStone材质生成入口，没有手写tscn。

| 对象 | 世界矩形 x,y,w,h | 边界理由 |
| --- | --- | --- |
| FrozenOuterUpperWall | 1280,1800,36,280 | 上段岩壁围合 |
| FrozenSentryRecessWall | 1280,2080,8,140 | 为旧哨兵备用身体x1290留局部凹面；不是全高薄墙 |
| FrozenOuterLowerWall | 1280,2220,36,1140 | 下段岩壁围合 |
| FrozenUpperResidentShelf | 1316,2400,368,40 | 幽灵home1500,2368、左右64px完整身体支撑 |
| FrozenLowerResidentShelf | 1540,2880,320,40 | 幽灵home1712,2848；右端1860距现有井壁1920保留60px |

045713的真实反例先加载24实际叶缓存，再把原生可站稳的旧Rust Probe传给身体查询。旧实现错误接受且增加计数；现在测试私有`QueryBody`先校验actualActor实际SceneFilePath、唯一直接binding、profile身份／族系／职能／carrier，再用**同一个actor**原生查询，最后分别增加body身份／body子型集合。旧Probe在native-clear点1040,604仍被拒绝，body计数保持0，不借活动缓存24冒充身体24。

045821证明缺完整脚底支撑；050207扩大为15项实际失败（两脚底、三材质石壁、五保存碰撞和五候选小地图项）。只加入上述五Solid后050439这些项变为0失败／2支撑，但退出仍287 Shape RID，所以该案严格FAIL，不能称资源修复。

051133扩展真实运动测试时，旧哨兵1450,2151被“新增常驻”的TraversalLanding保护规则及错误ground=false地板相切政策拒绝。修正测试范围：旧五记录仅校验原生完整身体不被新地图切穿，精确落地哨兵才用ground=true；原Rust启动下落4px／Moth空中保留ground=false。**新幽灵完整巡逻仍检查全部100环境＋36旧出生保护**，未移动旧敌人或删除新角色的保护条件。该失败及此前资源诊断均保留。

051407是扩展运动／胶囊反例／全50调查首次严格GREEN；随后owner加强完整收据及产物存在门，最终051745在相同C#源码／binary再严格GREEN。不用这两次通过关闭历史匿名FATAL，亦不重复无变化资源诊断碰运气。

## 最终候选的原生结果

| 身份 | 实际X最小／最大 | 单帧最大X变化 | 落地／访问两端后返回 |
| --- | --- | ---: | --- |
| frozen-detour.01 | 1437.5962／1562.4038 | 0.8000488 | 均通过 |
| frozen-detour.02 | 1649.5962／1774.4038 | 0.8000488 | 均通过 |

全50实际身体／24子型、真实factory24子型；联合43选中／7未解，1616次走廊查询、20228联合节点，其中Gear单区域到原20000限额。没有放大描述／查询／候选／联合预算，也没有据此称地图无解。

真实七个未解身份如下；共享桶的stable_ids不能作为每个身份独立拒绝的证明。

| StableId后缀 | 原始停止原因 |
| --- | --- |
| controlled-abyss.01 | no_sampled_template |
| archive-side-cave.03 | candidate_set_unselected |
| archive-side-cave.04 | candidate_set_unselected |
| gear-pistons.06 | joint_budget |
| gear-pistons.07 | joint_budget |
| steam-valves.02 | candidate_set_unselected |
| pendulum-gallery.04 | candidate_set_unselected |

候选JSONL20901条／8901362字节，50唯一身份／44route×subtype桶、0解析错误；602条backward accepted按桶覆盖49身份／24子型，不等于43选中的身份集合。所有记录placement_certified及motion_certified仍false；两只幽灵运动证明在独立PATROL原生收据，不修改这些全50统计标记。

## 案例与严格回归

案例根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。完整目录如下，失败原件保留；外层串行exit0只表示消费者终态，不代表各门通过。

| 完整案例目录 | Build ms | Engine exit／ms | 严格结果 |
| --- | ---: | --- | --- |
| resident-frozen-alcoves-20261017-045713-044-17c504e902e94166819f32058eb9eb92 | 24277 | 1／1673 | FAIL：旧Probe查询未拒绝；282 Shape RID |
| resident-frozen-alcoves-20261017-045821-996-717db2b7c1464c49814a9079d2fed081 | 22537 | 1／1470 | FAIL：真实脚底无支撑；282 Shape RID |
| resident-frozen-alcoves-20261017-050207-589-7286613fe35644b893672a2fa648220f | 24027 | 1／1562 | FAIL：15真实几何项；282 Shape RID |
| resident-frozen-alcoves-20261017-050439-676-dfae5f26395d4ed0a7f4fc400d9a7af5 | 23985 | 0／1555 | FAIL：几何符合但287 Shape RID |
| resident-frozen-alcoves-20261017-051133-623-e911706d62454ef9bca27b379a312865 | 31452 | 1／2199 | FAIL：旧Sentry测试范围／相切政策错误 |
| resident-frozen-alcoves-20261017-051407-317-b2620c62b05c43a0bb35b6b05ef28863 | 23924 | 0／30431 | GREEN：原生两Ghost、胶囊反例、候选43／7 |
| resident-frozen-alcoves-20261017-051745-550-465683bfb278475ab7895f7061333a7e | 25659 | 0／29570 | GREEN：强化owner收据后最终候选门 |
| resident-geometry-20261017-051845-863-21cf3ba41b8c4b98969331361cc25b35 | 24525 | 0／24523 | GREEN：原127地形／原41候选9未解；不是132地图的另一份验证 |
| resident-scene-binding-20261017-051938-681-da46c2ec9d5b44d9a8b11facb301d9af | 22780 | 0／7178 | FAIL：行为符合，stdout154 Shape RID |
| resident-subtype-profile-20261017-052011-731-6e3033ab97494934909579a167e8ce0d | 22486 | 0／776 | FAIL：行为符合，stdout31 Shape RID |
| resident-allocation-20261017-052038-253-120cd7565c414a5ca3bdbd2e04a65e78 | 23623 | 0／513 | GREEN：原名册；仍全部未置 |
| keyboard-20261017-052105-764-c95900a4fabb45c19ac6d97551b27ae3 | 22610 | 0／9595 | GREEN：引擎输入，不是OS真人按键 |
| actions-20261017-052140-950-3ff4548e120e43cea141eeba4d0e01ff | 23425 | 0／7079 | GREEN |
| prologue-pre-20261017-052214-449-76d9684cbeca422795df8c134da057b5 | 23304 | 1／87992 | FAIL：旧Upper lower_to_mid_swing，另2 profile WARNING |
| prologue-post-20261017-052409-043-7d171ce078614f96bfc1953aad821791 | 30088 | 1／8668 | FAIL：旧BellChainWaitForFloor及一条ILevelRecovery ERROR |

最终同版九门5GREEN／4FAIL；全15案6GREEN／9FAIL。十五次fresh ordinary Rebuild均0W0E，无timeout／guard失败／named waiver。最终九门无Handle／FATAL不代表历史资源问题关闭；原154／31、Upper错台、Bell落地／Recovery依旧阻止完整验收。focused55s／full180s／build180s，未改变时限或加入verbose／fixedfps／movie／GC／cache／借用shape Dispose／额外drain。

## 产物保管与未发布边界

最终051745的`storage`中保留候选场景、小地图数据及拒绝记录；另保留24候选叶与24作者配置。不把早先051407产物哈希混入最终051745，因为保存UID／资源标识可变化。

| 最终051745产物 | 字节 | SHA256 |
| --- | ---: | --- |
| candidate-CanyonDescent.tscn | 77099 | EEA40694DF01C5D999972218B6B6306E03D243695C7157D001822871CA0D7104 |
| candidate-minimap.tres | 447112 | AE42F7EDCB4C087EA4CB0125EE93938C88AD4F0B2C6BEA57AC10E06A9A3A0650 |
| candidate-leaf-geometry-diagnostics.jsonl | 8901362 | A0FBD0B8E27B94D4252932076486998D8ED2D279B644B4ED7E68F626C7001A29 |

最终九案共同source1675 manifest SHA256 `455F03A0DCF930D6E232A2818CDA9E6F5A440174AB063FE6222E3DC61F4DC721`，binary6 manifest `840C00F9845890C6E3BA5DB6E9053570F24A34C8B152B20EB2E8D132CA9DD6D6`，Knight.dll `966C1DC00CC697C937F2B66462786BE8EE15F6300E4870BD5E0C7DAC6E558323`。最终owner helper manifest `A6C5DDBDC40E77BD86BB6C5E31ACE5440EA7CC2D86F3982A02A959F2A5C4EA60`，pre额外helper为`B814CF048EC8B23A4058D11A78C6B5AC32D5607BDECA72C22ADA454AFBA46245`；051407的较早owner不同，明确分版。

当前生产builder SHA256 `7109913D6D3E41E252EF973293515C85145650316FF0AE8B7B916174EAFF057E`；placement test `D57E5432E3FDDC036B42DF97E3F1980BD37B296667808A33B7645F5EA6D75776`；runner `1A9B02EF8A166D26E8163E9E38ACDC8C2083FFCA1B960AF2191A989409ACDB86`；owner `C77CDC1ED58D56AEE73D74EC52CE880BC5CD7EECFEE5B1FF0FD7C8EE6D685A26`。SceneBuildUtil保持上一轮`3EAACFA0C12C1AC99A3BA5691D29ABE9E38C9EF75BF2D98A9A6CDE6749CC0D44`，没有尝试猜测资源拥有者修复。

十五案75份before／after manifest对、2625份选中文件对及90份保留binary逐一核对，全部一致；其中前六案30／1128／36，最终九案45／1497／54。每案source1675／用户保护109。final九案27份raw完整机械扫描、wait／provenance及native PID对应；重复stderr／external不重复计数。所有消费者终态后才更新文档；当前三个C#范围文件及owner也与最终051745前后副本一致。owner PowerShell AST为0错误，文档diff／尾部空白检查通过（Git另报README将来LF→CRLF的提示，不是测试错误）。

正式Descent仍70710字节／SHA256 `E091C51C8D46ED117FB6020DBED64750914B72D9544856E179A7CE0ACB9D1CD6`，parent仍465498字节／`3427696E7521BB6471E184CC0BD6F3CF343AC990462B2075A810E58357C19640`。生产builder与正式保存资源暂有待发布差异，不能运行builder直接覆盖正式资源来跳过门。候选五区路径表只供本案；candidate-minimap.tres是同候选碰撞生成的数据证据，**不是运行时父Minimap已接线**。

## 限定范围代码复审与裁定

独立只读复审对照045713真实旧副本和051745最终副本，而不是把dirty checkout全部HEAD差异当本轮；没有执行程序。范围内未发现可证实的新增严重／重要缺陷，只支持“五块隔离候选地形、两幽灵实际巡逻、原子身份查询”的窄结论，不是全Task／整游戏完成或发布审查。

一条Minor：`CanyonResidentPlacementTests.cs:23`旧类注释仍称全类query-only／no resident AI，与新增phase不符；保留为下一次C#改动时修正的文档项。本轮不在最终运行证据后改变源码再冒用旧哈希。

逐项确认复审未裁为新增缺陷的边界：canonical／父Minimap未接线是明确隔离候选，**仍需交接**；Player真实运动／爪返程／旧飞行AI不能静态推断失败，但**仍需实测**；43／7／全50战斗／manager不是本小增量声明，**仍全部OPEN**；154／31及Upper／Bell是已存在的未关闭失败，**不得豁免**。接受这些范围区分，不删除任何完整交付条件。

## 下一步与验收边界

下一先实际玩家抓爪／返回、旧敌人活动兼容与受保管父场景／内嵌Minimap候选路径交接，再处理其余七缺口、全50活动／攻击／接近及world manager。ControlledAbyss侧棚只读建议为隔离候选宽200→240、左端1920不动／中心2020→2040，保现有centerCorridor2160..2320；它尚未执行，不能称真实下落4–6秒或Ghost运动通过，不改共享layout或直接发布。

匿名资源析构须取得可区分的对象／拥有者／生命期证据再修，不把Shape数量、首次GREEN或本次未复现当根因。正常Main／Explore同角色标题至Boss及实体出口、旧档、实时显示60帧、最终统一美术／音效、审阅归档录像与真人反馈均未完成，Task1最终50／Task2／Task3／Task4全部OPEN，无task-done或whole-plan完成审查。

旧26＋新50＝76普通＋1Boss、原生8／废弃12／精工10／骨骸10／幽灵10和ECHO一个指导人格保持。取消的自动化不恢复；无新付费、Git写入、上传或发布，不把十万付费人数当作已证实结果。
