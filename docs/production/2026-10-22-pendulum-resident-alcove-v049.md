# 钟摆机械侧龛与完整玩家往返 v049

2026-10-22，Asia/Shanghai。唯一资源根 `G:/knight`，Godot 工程 `G:/knight/Knight`。本轮有实际地图生产代码和原生运行进展；前一轮路径确认本身不算游戏制作。完整序章1–11及常驻Tasks1–4继续ACTIVE，尚非正式试玩／最终美术交付。

## 已实际制作并验证

沿用已批准的围合地图方向，在Ascent钟摆区东侧增加三个独立实体，外观仍为可替换TempleMetal占位，不是最终素材。碰撞、外观、小地图分开，不把背景画当作可通行地形。

| 实体 | 世界矩形 x,y,w,h |
|---|---|
| PendulumResidentFloor | 4936,2520,184,40 |
| PendulumResidentRoof | 4992,2344,128,40 |
| PendulumResidentBackWall | 5096,2384,24,176 |

左侧开口、内净高136，顶板与完整钟摆占位下缘约隔33.56。原机关危险包络、角色身体、拉力、运动参数、旧敌人占位及检查预算均未缩减。实际保存precision-laser身体50×68、碰撞局部Y=-5，在home(5028,2491)具有完整支撑；实际配置炮台360次观察保持HP2、无回血和位置注入。未放入攻击目标，因此不是炮台战斗证明。

同一个真实保存Player，入树前仅设一次受控起点(4392,2804)，之后没有姿态／速度写入：

1. 原Grip3→Grip4：真实W+D/K、同一次施放清原柱、实际Arrived、K取消、自然完整落脚。
2. 原平台真实制动到入口，一次普通Space+D跳上新地板，继续D走进顶板下方至(5030.571,2483.9333)，不能只落在外唇。
3. A离开、反向键制动、落回原Grip4，再经真实原K转移到Grip5。

最终候选局部证明426次观察／427次逐physics检查，HP5→5、回血0、普通跳跃1、两次匹配Arrived／Cancel、冲刺与借力Boost均0。每次原机械爪转移包含准备／施放／自然落地，共享原300 native-tick上限；Grip5准备40＋settle12也按实际physics限制。原127世界做同一连续两K对照，跳跃0。完整40×72胶囊每physics避开全部潜在危险，落脚有中心及双边原生射线，不以接触一角算完整支撑。

这是隔离、机关停止／炮台访问前移除的受控局部路线；不是机关全活动＋敌人占用战斗、正常Main／Explore、真人操作或60FPS认证。

共享保存流程已实际生成Descent、Basin、Ascent三候选叶，五区候选从135增至138实体。可搬移六文件父包实际内嵌五区及138小地图，独立核对原5冰区＋2齿轮＋1蒸汽＋3钟摆共11矩形，真实Ready与唯一存活Player通过。Pendulum专项只在内存重建／核对地图，没有另存独立minimap.tres；真正保存的是父包内嵌地图。正式场景仍127，未整体发布候选138，正式新增仍0/50、全部Placement=null、24正式子型Unimplemented。

## 失败先行与复核修正

以下日期前缀均20261022，原件保存在 `G:/knight/验证证据/october13-stable-claw`，不是与最终批次拼绿：

| case后缀 | 真实结果 |
|---|---|
| 040541-949-c19b59dcc0c4444987d25fe0d54b79d5 | native1/1268ms：新驱动在Aim当帧误要求Windup；不是缺地图的行为RED |
| 040720-715-942fa83440e24f68b48ef8ebd3e87c49 | 原两K完整通过，实际新laser点失败incomplete_support；这是加实体前的功能RED |
| 040853-599-68ec7256952445faaa26e22a693163c2 | 支撑／138实体已通过；炮台夹具给两个巡逻点被真实配置拒绝，改为唯一home，未放宽产品验证 |
| 040951-210-23cfe534122e4ae795d2e4d9df084adc | 首版严格PASS，但只证明访问外唇，不能替代后面的完整入龛门 |
| 041102-168-c046bc71fa92458798cd1ed56b7c4c42 | 加入真实完整入顶板检查后native1/15674ms，角色停在4992.458，准确抓住证明缺口 |
| 041203-204-cd827bd4620842be8b671921bb19202a | 真实D前进及原惯性预测制动至5030.571，完整入龛／返回／旧K全通过，native0/19301ms，严格PASS |

测试修正只涉及真实输入采样和制动：K按下进入Aim，后续tick才开始施放，不能要求一次性K边沿与NextCastId递增同帧；观察者逐physics记录边沿。沿用原自由运动预测提前松键、按当前位置选择A或D，而非走到目标后才忽略惯性。没有改产品抓爪、增加预算、假造目标或注入速度。

## 同冻结版本13门：10PASS／3严格FAIL

外层session58573已经TERMINAL exit1，全部build通过、timeout=false、guards=0、无命名豁免。以下case均20261022，耗时单位ms。

| phase | case后缀 | build | native | 严格结果 |
|---|---|---:|---:|---|
| resident-pendulum-alcove | 041416-698-f010b9f9ded34486b2b184e563add444 | 29474 | 19401 | PASS |
| resident-map-census | 041511-402-08c464503ce04f239a7c5b10df31b01d | 23755 | 7325 | FAIL，native崩溃 |
| resident-parent | 041547-999-d526e4f266114d2eb302780880d49682 | 23124 | 2378 | PASS |
| resident-steam-alcove | 041619-169-51a8f116de134098b48e0afb27503da2 | 23416 | 10514 | PASS |
| resident-gear-left-access | 041659-020-b70538a35d864c6da7ae173132696860 | 23724 | 7181 | FAIL，实际选错目标 |
| resident-gear-alcoves | 041735-874-e8fd4c474ff547cc82b0843421d1f650 | 23040 | 9781 | PASS |
| resident-allocation | 041814-058-133e3233814041c892254cd03320dd26 | 22048 | 502 | PASS |
| resident-void-stream | 041841-865-3f3178b6607c45068e6a0855aadb38e1 | 22945 | 1443 | FAIL，退出187 ShapeRID |
| keyboard | 041911-952-8e2c7bef760f41bfb225d488615a6804 | 22442 | 9573 | PASS |
| actions | 041949-023-f14278dc98684e8bb2f5a2b6e121b725 | 28525 | 7082 | PASS |
| canyon-structure-v2 | 042029-872-0fa26e73b1f745d88c305b27ff6477ff | 24247 | 1347 | PASS |
| prologue-pre | 042100-909-681c3b8f75754d5fa48f7e3a8ef6ddb6 | 22844 | 99868 | PASS |
| prologue-post | 042308-994-776acbdce26e4793bcf855aefb922393 | 23298 | 147435 | PASS |

post保原75案例／0退役、8次同owner生命期1→1；四条精确故意负测warning仍按原来源／次数限定，不豁免任何ERROR或FATAL。该通过不代表正常新138世界的Main＋Explore通过。

三个失败的首实际原因：

- **联合census**：第一身体候选后 `gchandle.is_released()` finalizer FATAL，exit **-1073741795 / C000001D**，没有完成联合选择，也没有生成本案完整JSONL。当前138版不得写48/2或沿用旧135版47/3为本版成功数；匿名根因仍未知，保留日志，不做原样反复重跑抽绿。
- **Gear左通路**：角色(1923.0718,3691.7795)真实D前检命中旧AbyssSafeBottom下表面而非新棚。Resolver先比角度再比距离；旧−9.84375°比新+11.25°更贴轴。下一步可在原90上限内等待真实resolver选中新top，删除盲两Step，但还未实施／实测，之后施放下落与完整电区仍须原样检查。
- **void**：10行为结果满足且engine0，但stdout明确187 ShapeRID泄漏，仍严格FAIL。不能用engine0或其他阶段安静关闭它、metadata252或历史Handle／FATAL／音频故障。

## 版本与保管边界

本批真实1707项源／资源、其中877 C#，所有案before/after保持；source manifest SHA256 `691142F157F3532A6BEFDB04D9F855A0F597E25EEDC6B7BA255353D82C2AA088`。共同9 helper manifest `BE039444DF6A47944D853DE52BC99A3B81AA828E04DB629C53CCAF71AAB2E555`；pre多一个原合同helper的10项manifest `272AFC46997AF7240191277747371F34A081EB611A528F6A1AE2A91DF3FD046F`，不是漂移。六二进制manifest `86A47C9DB7BA6945692F38198CC419A9C397E8ED5EF26CA340A9AE73A352B575`，Knight.dll `A7FD80BAF4B55CF836A8FCB612951C2F7B8FA0F25759EA13CDF95932B19A5AF8`。

真实userdata只hash保管，109行manifest `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`。HEAD仍e7c70498、index SHA256仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。所有owned native终态后才改本文及状态文档；没有Git写入、付费生成、前台／OS输入、自动化、删除、借入资源Dispose或GC/cache/drain/等待掩盖错误。

独立只读审计闭合65组before/after清单、4618份留存输入实体、78份六二进制副本，均零漂移；26次实际PID／完整argv与合同匹配，13次engine wait与provenance吻合。终态当前1707源、10helper、104个userdata文件及6运行二进制再次hash零差；109行还包含2目录和3根记录，continuation为空0条，不冒称1个文件。Census的一行ERROR:FATAL在stderr及external镜像出现，不计成两个独立错误；post四warning的stderr／external镜像也不计成八个事件。其余九个通过phase三日志无ERROR/WARNING/FATAL。

复核的留存小缺口：Pendulum独立文本输入包没有显式复制CanyonWorldLayout.cs，但全1707源manifest包含其SHA并前后固定；不把它说成完整文本回放包，后续扩充owner时补入。独立minimap.tres未保存，不能写成已保存；六文件父包的真实内嵌地图已单独验证。

本轮无新图稿／实录，未生成庞大媒体。手动容量检查成功：4838字节、基线3508、增长1330、Notify=false；没有重建自动化或旧归档项目。

## 下一步与完整范围

优先追踪联合调查的真实资源身份／所有权，不伪装成几何不可能或忽略崩溃；继续左棚实际选点／拉升／返回。Archive剩余两位不是载体零候选，而是五个完整活动包络共同容纳不足；只读提出的新上下侧层仍是待原生查询的工程候选，80净高、与Frozen间56净距、164横跨均不能静态算可达。既定50／24／五族系、原26＋新50＋Boss、骨骸10／幽灵10，以及完整1–11最终美术、UI声音演出、正常双路、真实试玩／60FPS、资源错误关闭和可运行版本交付全部保留。
