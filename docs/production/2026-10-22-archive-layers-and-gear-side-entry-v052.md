# 档案分层与齿轮独立侧抓 v052

2026-10-22，Asia/Shanghai。资源总根 **G:/knight**，实际 Godot 工程 **G:/knight/Knight**。内部候选制作，不是正式试玩交付；完整1–11／常驻Tasks1–4继续ACTIVE。

## 事实：本轮实际制作

用户批准了旧问题中的“补独立侧壁抓取入口”。原call的原始提问未在现存日志找到；本项目旧进度账明确把待选side-grab与Gear左棚被AbyssSafeBottom抢目标关联，因此此次限定齿轮左棚，不将批准转用档案区。用户意见记为[U-20261022-001](../玩家修改意见与世界设定.md)。

生产生成器新增四个独立Solid，均复用可替换TempleMetal占位skin、真实碰撞与已有抓取profile，没有生成新图像／声音或付费素材：

| 实体 | 世界矩形 x,y,w,h | 当前证据 |
|---|---|---|
| ArchiveResidentLowerFloor | 1424,1776,496,24 | 实际保存支撑／五槽联合几何／两个下层地面居民巡逻 |
| ArchiveResidentUpperFloor | 1736,1248,232,24 | 实际保存支撑／上层地面居民巡逻；Player进入仍失败 |
| ArchiveResidentUpperBackWall | 1944,1100,24,172 | 实际保存独立碰撞、skin与小地图 |
| GearWestClimbWall | 2280,3700,24,116 | 真实键盘抓侧面、借力越角、完整落棚及原出口 |

候选五区／可搬移父包／内嵌小地图为**143实体、16个新增字面矩形**；只重建Descent的夹具为135。正式保存世界仍127，正式新居民0/50、全50 Placement=null、24正式子型Unimplemented。候选资源及24占位叶不等于正式人口、最终美术或发布。

### 齿轮侧抓：真实同角色通路

在原camp→Abyss西墙／底板→GearWestLaunchShelf前缀后，实际D轴首击新壁(2280,3709.9336)／Left，不再抢旧顶板。第二真实K到达同一侧壁后，使用**既有的一次附着Space借力**（A+Space后切D），越墙进入棚上方，在x2363.484实际反向A刹车，x2371.993、y3755.933完整落在原左棚，再自然前进并刹车到原GearPistonOne出口。

最终同冻案065358-ac3e36e9：289观察／580逐physics检查，2 casts／2 Arrived／1 Cancel／1 Boost，普通跳跃0、短喷0、HP5→5、治疗0。实际40×72胶囊不缩；K到落棚／刹车共用原生300帧上限。越角AABB相交阶段逐物理帧检查真实胶囊对真实墙shape无相交，按physics ID去重得到4个样本；中心／全身越过墙东缘时再分别做实际native-clear查询。完整named-floor接触及三个脚底射线通过。

这是明确替换v050的跑动抓顶路线，**不是声称旧Boost0路线原样保留**，也不是新增二段跳。机关回调停止、居民不在场，不能推成占用战斗／正常世界／真人手感通过。入树前受控起点之后没有测试位置／速度／HP注入。

另一个独立Player leg保留原FrozenExit直接落营：一次落地、715.969落高、1点摔伤，HP5→4；随后同一角色camp↔Abyss返回保持HP4。它与后面的新侧抓Player是两个独立leg，不拼成同身份全程、完整BasinReturn或存档认证。

### 档案层：几何／地面巡逻通过，实际入口未完成

实际保存的五个完整活动走廊及Grow24联合占位通过，三个地面居民各360观察，HP3／3／2、治疗0并保持支撑与双向运动。上层64px身体最初越过区域顶边0.0662导致Returning不巡逻，已通过真实地板下移8px和后壁延长修正，未放宽Contains／碰撞／身体／速度。飞行／炮台两槽目前只做实际载体几何路径，不冒认攻击／运动验证。

原始Archive上层尝试仍失败：原Entry→旧下表面K实际Arrived／Cancel，第二K正确锁定新上台Up面，却在Reel中Obstructed。逐状态日志保留；精确触发碰撞体／分支仍未采到，不能把“侧角与下落惯性吻合”的推断当确定根因。下层Player入口／出口、上层完整进入返回及占用战斗均OPEN。

## 测试先行与关键反例

原始证据均在 G:/knight/验证证据/october13-stable-claw，时间前缀下带唯一GUID目录；不覆盖失败：

- Archive061135-165ec42f：真实缺支撑／越界RED后才加三实体。061822-90270d8f记录上层HP保持但X不动和真实Y1207.9338，真实8px修正后062050-617e294e StageA通过；不是Player入口证明。
- Gear063941-757973a1：真实保存地图缺独立墙的RED，之后才加墙。
- 064315-4338f235：路线前的测试期望误写piston135×135；实际生产／正式保存132×32、行程100，真实潜在包络132×132。只纠正三个误替换的测试字面量，未缩危险区。
- 064443-1d6fc1ae：实际选侧壁／Arrived／Space成功，但一直D越过有限棚并碰完整piston潜在区，严格FAIL。
- 064736-cc566ac6：旧“全AABB同时高于无限墙顶且完全到墙外”见证不成立，刹车未执行；elapsed实际18492ms，账中旧18436记载已更正。
- 064937-d0468d37：真实中心2307.1416,3664.246及全身2324.4846,3667.3296时native-clear均True、AABB-above-top均False，证明原见证过强，但两点本身不证明全程。随后补逐physics真实墙shape检查，不借此忽略穿墙；才允许真正执行反向刹车。
- 065201-7b9c5149局部通过，之后仅将角落观察计数按Engine physics ID去重；最终资格取下表065358同冻案，不借前一版通过冒认。

实际owner准入24项和严格census诊断／普通判定51项均通过。没有重试无变化census抽绿、GC/cache/KeepAlive／新增drain／借用Dispose／引擎升级或伤害豁免；既有runner退出drain未改。

## 最终同冻结15门：10PASS／5严格FAIL

外层session22936 TERMINAL exit1；全部案终态、无超时、guards=0、命名豁免空、1709源／资源（879 C#）同冻。耗时ms；仅engine0不等于严格通过。

| phase | 20261022时间 / GUID前8 | native exit / ms | 严格结果 |
|---|---|---:|---|
| resident-gear-left-access | 065358 / ac3e36e9 | 0 / 19337 | PASS |
| resident-gear-alcoves | 065445 / 7097ebd0 | 0 / 10172 | PASS |
| resident-parent | 065529 / ac1f8ce3 | 0 / 2380 | PASS |
| resident-steam-alcove | 065602 / 4f05df95 | 0 / 10600 | PASS |
| resident-pendulum-alcove | 065644 / 36517e11 | 0 / 19294 | PASS |
| resident-abyss-alcove | 065734 / 627f7cda | 0 / 9064 | PASS |
| resident-frozen-player | 065812 / 6a205758 | 0 / 5423 | PASS |
| resident-archive-alcoves | 065848 / c4bd1e56 | 1 / 12002 | FAIL：第二K未Arrived |
| resident-map-census | 065928 / e0d208f2 | -1073741795 / 9853 | FAIL：GCHandle FATAL |
| resident-void-stream | 070005 / 51d8c7d3 | 0 / 1451 | FAIL：退出187 Shape RID |
| keyboard | 070034 / 57ecbb14 | 0 / 9681 | PASS |
| actions | 070109 / a7af19ac | 0 / 7086 | PASS |
| canyon-structure-v2 | 070147 / 0daea59a | 1 / 967 | FAIL：Archive640/704 |
| prologue-pre | 070219 / 6d274972 | 0 / 99967 | PASS |
| prologue-post | 070431 / 47fff117 | 1 / 1529 | FAIL：同一Archive结构断言，未到75案例 |

失败解释：

1. Archive第二K仍未完成；实际三个巡逻通过不能覆盖Player入口失败。
2. 联合census再次在fixture143后发生gchandle.is_released／mono_object_disposed_baseref:1788／Finalize／C000001D。没有完成本版联合选择或完整JSONL，**不得报告当前143的48/2、47/3或50全合格**。匿名对象根因未定。
3. void十行为满足、native0，但stdout有一条187 Shape RID泄漏，严格FAIL。其他安静阶段不关闭它、旧31／154／252 RID／Handle／音频故障。
4. Archive源码区域已从640扩至704，正式保存标记及独立接受字面量仍640；真实布局／生成数据接线尚未完成。不能只改断言迎合704。
5. post native1同样在Archive布局断言失败；owner随后报缺精确scope receipt是后果。**本版原75案例／8次owner及四条故意warning未执行到，不沿用v051通过**。pre通过不覆盖post。

## 推理、裁定与风险

- 旧批准限定GearLeft：依据旧进度账，不伪称已找到原call；若用户指的是另一平台，需要更正位置，不能扩大到全关。
- 物理地图用独立Solid／skin／minimap，不做浮空假anchor；若设计不适合正常占用战斗，仍需改实际地形，不缩身体或危险区。
- 侧抓使用既有一次Boost而非提高拉力／重力豁免；实际A+Space→D及反向刹车已测，但可理解性／提示与真人手感未验，不宣称易上手已完成。
- 以原生胶囊和连续墙角检查替代过保守无限平面AABB门；仍保实际完整落棚、全部潜在危险和原出口，不把两个端点查询当全程。
- piston字面量修复以实际生产及保存数据为依据，绝不把135改写成新较小生产危险区。
- 独立有限范围审查以063941 before镜像和当前源码／065358证据为基准，未发现成立的Critical／Important／Minor。未审正常世界共存、真人／可见表现、整关、15门最终全通过、Archive／资源／存档／正式发布；root裁定全部保留OPEN，代价是不能交付这版为成品。不是完整计划最终审查或合并批准。
- 历史identity契约cleanup／call-regex小项及v050 bottom180＋尾8帧共享上限仍待后续；本轮未宣称修复。

## 下一步与保存

先完成Archive真实入口／上下层返回（现有能力、完整胶囊与危险区），再以窄生成器同步已接受区域字段、保原127物理／演员／检查点／小地图语义，重新完整结构／post；不能一次复制候选143覆盖正式127。继续定位资源失败的具体身份，再全50实际运动／攻击／玩家接近、正式叶和manager接线。完整原26＋新50＋Boss、骨骸10／幽灵10及全部最终视听、正常双路、真人60FPS／发行仍未完成，1–11目标不缩减、不标完成。

工程 G:/knight/Knight；本报告、意见账、README和原始证据保留在G:/knight内。不恢复旧归档项目，不创建自动化，不删除资源，不提交／推送Git，不操作前台或真实存档语义。本轮无新选定过程图／短片、付费资产或可见试玩交付。

同冻source清单SHA256 **21D3BFA2D0E63C96788D2D5C7C1538210DF08961AA29CAC875F540FE9A816767**；六binary清单 **E46719FFF9AE06C6CDB7A3693CEE658D2BA358296EEF7E72C714BA51097BDBB3**，Knight.dll **A24FA70682499AD5BEBF1E0827EB1495F88E7FB5D9B689D5992B4BF38F186582**。post九helper清单 **CEB0F22481C7298A16C80C9DA6EA7F17311098A6806106E9F17C02474B11A97F**；真实userdata109行／104文件hash-only清单 **BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB**。末案后实际1709源／879CS、9helper、104真实用户文件、6binary逐项复核零差。具体保管链复核补记见下。

独立保管链复核：15案75组清单前后相同；实际选择副本5,304份源码＋154份helper、90份保留binary逐份匹配，30组build／engine PID完整argv／contract／wait／provenance一致，无超时。15次构建均exit0、0警告／错误。保存的是选定源码子集，不是1709项全部实体文件或整repo备份；真实用户数据只做hash，不读取存档语义。

HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index SHA仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C；不把清单核验冒认整repo/Git证书。所有owned native结束后才写状态文档。

手动媒体检查current4838／baseline3508／新增1330字节，Notify=false；不重置基线，不恢复自动化，达到20GiB也仅随正常汇报提示而不停工。

