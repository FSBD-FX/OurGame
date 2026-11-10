# v331 · 工坊弧摆齿轮已接入，整版资格未通过

## 事实

KNIGHT仍在`G:/knight/Knight`，原完整十一项目标ACTIVE。上一轮v330独立内核已通过限定同版15phase+cold；本轮是下一真实地图内容，不把旧资格自动续签到新源。仍只无窗口构建/测试，无GUI/OS按键/付费美术/自动化/Git写入/删除/真档实验/GC或cache/引擎改动/新dump。

正式Basin现已增加独立`Gameplay/Mechanisms/UpperGalleryUnderArcGear`及一个Collision：local(2040,356)、pivotOffset(80,-240)、signed angle-.6435011、period3.2、r24、Damage2、cadence.5。锚点world(3400,3716)在实际gallery2下表面，完整包络x3296..3504/y3932..3992.9822；不是凭空悬挂或把旧straight/摆锤改名。上层原straight1、入口fixed2、全部158地形/76配置位/9片不改。

当前叶SHA`A0385C934DF17FDD74EDAEEB1F29F9AFB4BBAFC24FFA953362FAD40A7D449EF3`，由已验证候选生成器产物通过apply_patch完整generated diff接入，并逐字节hash确认；旧叶3DCDF1...保留在所有before原件。父`PrologueLevel.tscn`仍`D4AE2DC2F100B3C22908998BC0143D46033160BA2F96A590CE7BAAD64F63988E`，未发布含case绝对路径的候选父图。独立serialized引用语义审计：389旧节点及其引用resource数据保持，仅新增Arc两节点与script/circle；随机ID重排不算内容变化。

## 已有真实局部证据

证据根`G:/knight/验证证据/october13-stable-claw/`，原失败均保留。

- 保存缺节点RED：`thruster-arc-gear-layout-20261108-221903-698-7328715ef7d2478b89a734522c6238f8`，1/620ms。
- 生成缺节点RED：`thruster-arc-gear-candidate-20261108-222006-925-32f86643064b46968bebd0a5bc309202`，1/564ms。
- 最小共享authoring后detached GREEN：同candidate前缀`222312-280-d851f9b302c841859aa7b519a32394f7`，0/624ms；这不算原生通路验收。
- 扩展夹具build失败`223039-036-1e2466b9fd6041a19df30a7571794691`为Walk重名作用域错误，非功能RED；改三处测试方法限定名后继续，没有生产物理修补。
- 首次走廊无伤断言失败`223208-781-f15cfede7d894ae7bf15e70d5cd8ec41`，进一步仅加标量身份记录的`223349-023-4e862cab1fa846378624955600f32743`证明HP5→4来自原CanyonSteamJet而不是Arc。
- 过早隔离蒸汽被durable control重启的`223633-753-20855f23512f4f02834e16bd0493f585`保留。源码确证fixture mount监听先于Level后置ApplyProgress(false)，所以夹具只在真实Playing提交后关闭这一个其他伤害源；不改生产生命周期或补血。
- 完整候选父图/局部原生：`thruster-arc-gear-candidate-20261108-224004-585-b662d18203fd4d8eaa59772d660cfd79` strict PASS、0/26659ms，原55000cap，stderr0/ERROR/WARNING0。四起步相位约.005/.255/.505/.755、八次实际跨中点/双向回返，HP5/无伤/零恢复/无跳喷爪；两次持跳各接受一次普通跳、真实Arc身份与contactID匹配、HP5→3、落回原walkway/恢复0；短tap一次跳无伤HP5。实际原相机完整40×72胶囊及原始边界保持，当前圆Fog/真实墙/隐藏/暂停通过，不签像素。

该候选247快照对/六实际产物复制0差异，source1923为`24C8C8547CB55A30A0D3DC6220DBBFA22E8BC511238FBD7392BB4C4FC20D50ED`、six`6B36F1C8E94D1C8AD5978BF1CC65A013ED63FE099511D95ABB87B4FDADBB5754`，user109/helper9前后稳定。此后测试元数据/host门及正式叶已变，不能外推这个身份为本轮最终程序集。

受控夹具明确FinalOnly（无额外50自动居民）、post-staged禁用10 encounter groups/3 actor容器，并关闭其实际Area/body碰撞；只另隔离一处SteamJet。正常Explore本来先真实W+K关原阀门再经过，**局部通过不证明未关阀时整条路无伤，也不证明与全部敌人共存**。其他机关与地形/Player/Input/Fog/Camera保留；普通Main/Explore必须另跑不隔离的实际原路线。

## 保存所有权与尚未通过

原`resident-steam-metadata-20261108-222417-248-48aa56c0c6f04639be6aac8d3b2e98e9`真实拦住新Arc未审计shape（expected61 actual62）；仅补具名Arc Circle/r24路径，保留distinct nativeID/borrowed排除。旧straight四反控保留并另运行Arc对应四反控（最终实际save门待执行）。

`224210-675-aa811cd598f6436a9a8b40a8c327c6db`已过62形状审计，但旧Basin31(actual45)断言失败：14个body是v251已存熔炉附室，不是Arc新增地形。只同步当前元数据45/全catalog158（含既有baffle）及host精确标签，不改历史fixture或生产地形/codec；全set/字节相等/物理坏输入仍保留。

最新`resident-steam-metadata-20261108-224554-139-3970c9a7690d47468b9acd182cdbd969`所有语义/62shape/158catalog/六负例及staged回执通过、engine0/6534ms，**但engine.stdout末尾ERROR328个GodotShape2D RID泄漏，strict FAIL**，不是Green或根因已修。只读限定所有权审计未找出可证明的direct-new形状漏释放：两批62owned都实际Dispose/ID无效；私有Ignore重载内部wrapper是未证线索，cached borrowed shapes不得任意Dispose。不增加GC/cache/资源identity模式试验，不静默重跑筛绿；此项继续OPEN。

## 当前后续与风险

正式叶复测`thruster-arc-gear-layout-20261108-224945-549-c902578e8fc34b13af88d54af27a2d5e` strict PASS26472/55000ms，实际未改写父D4AE→正式BasinA038依赖，不是候选parent。其后冻结最新1923源DF4FEF.../六产物953D35...，同版实际新Arc layout232607/26550ms、旧straight layout232511/23347ms及models/新旧齿轮/旧机关已通过；不能借此签后续整个版本。

离线配置图：`G:/knight/过程图稿与视频/2026-11-08_悬吊弧摆齿轮地图_v331/`保留original/presentation JSON、SVG、3200×1800 PNG、HTML及renderer/sourceversion索引，原图不覆盖；根已查看实际PNG。A01关联新M20，旧R01/M19与熔炉H01/M18保持。真实signed圆弧/支点/两端r24，不用弦或AABB代伤害形状；57对象/32逻辑锚点不是陷阱总数。13实际packed坏配置拒绝、最终renderer的52数据控制、23出版控制及旧volume19控制通过；这是配置证据，不是普通游戏像素。native map230939/final quick232815实际出口与payload详见ledger/原件，最新source/code整体仍需审计。

唯一fresh审查C0/I0/M2，五项focus/14pins/实际旧节点与resource无漂移核准；不进行二审，也不据此关闭原T03/完整十一项。两个Minor延期：M331-1 camera只检查起步、通行终点和落地样点，尚非逐帧/跳跃顶点覆盖；M331-2完整弧与实际158solid无交集由本次独立审计支持，缺持久侵入负例回归。这不是已发现裁切或穿墙；旧所有Minor保留。

快速11phase批次35228已终结并独立审计：严格8PASS/3FAIL，2716对快照差异0、66份实际binary复制匹配。candidate232706-772870a8出现`gchandle.is_released/GodotObject.Finalize`原生FATAL、native-1073741795/31239ms；八次walk与第一次heldjump及其audio drain之后中断，不能声称该案七夹具完整。Steam232853-bb124e2a engine0/6598ms但stdout244shapeRID ERROR；Archive232932-4ca7cbfe engine0/4931ms但stdout308RID ERROR。源原因仍未知，不额外采集/改GC/cache/忽略错误或重跑筛绿。map232815实际2439ms严格PASS、13项真实packed坏配置拒绝、payload SHA`9D452BA9CC87A432F404FA41D98E0CF2BF699C6453B4B53B0553B3C266D90531`。save233010严格PASS12205ms及独立cold0/1584ms，writer48016与reader48664、六只读witnesses成立；straight四项和Arc四项donor反控均实际执行并exact_restore。旧127→143继续仍false，不扩大兼容结论。

## 最终同版回归：15通过，5失败

35228与25823两个拥有的批次均已真正终结；没有遗留KNIGHT原生进程，没有重跑筛绿。以下20组全为同一1923源/六产物，独立cold另列，不拼借上一版本结果。

| 阶段 | 原生耗时ms | 严格结果 |
| --- | ---: | --- |
| models | 531 | PASS |
| arc-gear | 6345 | PASS |
| moving-gear | 5710 | PASS |
| recycling-traps | 26563 | PASS |
| moving-gear-layout | 23347 | PASS |
| arc-gear-layout | 26550 | PASS |
| arc-gear-candidate | 31239 | FAIL：终结器FATAL，未完成全部夹具 |
| map-preview | 2439 | PASS |
| steam-metadata | 6598 | FAIL：stdout244个Shape RID泄漏ERROR |
| archive-metadata | 4931 | FAIL：stdout308个Shape RID泄漏ERROR |
| save-compatibility | 12205 | PASS，独立cold另1584ms通过 |
| rig-compatibility | 38412 | PASS |
| presentation-rig | 18460 | PASS |
| ground-gait | 30034 | PASS |
| recovery-compatibility | 16152 | PASS |
| actions | 10859 | PASS |
| prologue-pre | 54882 | FAIL：第三个用例内FATAL，enter3/return2 |
| prologue-post | 195012 | PASS：10suites/75项，原四条限定负例WARNING |
| route-main | 258067 | FAIL：落脚交接缺相邻物理样本450→454 |
| route-exploration | 321974 | PASS：完整路线JSON、Boss及实体出口 |

前段第三项`BasinClawTargetTests.Run`未返回，不能报44项通过；两个FATAL原生退出均-1073741795，不能从最后出现的测试名推断故障对象。后段原cap300000ms，stderr23551字节非空、ERROR0；两条正常键盘线原cap600000ms。所有案例未超时、guards空；这不覆盖表中的五项严格失败。

Main确实到Boss死亡和Completed实体出口，普通跳21/21接地、观察器恢复0/0、完成事件/次数1、held60位置/body/save保持；但`WORLD_GROUND_SEAM_INSUFFICIENT`实际拒绝450→454，stderr7949字节/ERROR1，完整route JSON未生成，`whole_route_GREEN=False`。不能借这些局部回执签收整条主线。此案不是已发现Handle/FATAL，问题在当前落脚证据条件未满足。

Explore完整JSON通过，标题开始、同player到Boss及有支撑出口；普通跳33/33接地、恢复0/0、完成事件/次数1、held60保持，velocity原样(260,0)。实际save/terminal/held SHA一致：`24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`。encounter激活11/击败10不等于逐76全清。

新自然Arc通行回执在stdout/external各一次且相等：x3158.9849→3800.0842，HP1→1，跨中点1次；相位区间.5208329083→.5624995744、physics5930→5938，Arc已接受伤害0。`original_valve_closed=true`、`combat_isolated=false`、`input_strategy_changed=false`、`actual_saved_arc=true`。这是实际原阀门已关闭后的正常探索观察，不是隔离七夹具，也不证明未关阀/任意相位/全部战斗组合安全。

## 完整性核验与下一入口

独立审计20案合计5100对before/after快照差异0、120份真实binary复制匹配；helpers每案实际9–13行，合并16个唯一工具身份，均与当前匹配。当前重新枚举1923源、六产物，与20案after清单完整相同；14个review pins、未改父D4AE、地图归档renderer AE79一致。受保护用户清单109行（含104个实际文件）字节哈希保持，不读取真实存档语义。

封存清单文件SHA为source `DF4FEFCBD5CE24727D53A3A5E99FDE59F48020701C6BCF19ADF3AE56B8BA12AA`、six-binary `953D35BD65C20F33593BF6D349FC861CCFCBB884305B2A1BA815008854ABBF59`、user `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`。第一次最终审计命令误将LF规范化、去末尾换行的字符串SHA与整个清单文件SHA比较，因表示法不一致失败；限定读取现有封存文件确认原因后，修正为完整规范化清单逐项对照通过，未触发游戏修改/构建/原生重试。原失败和正确的两类SHA分开保存在[current identity audit](../../.superpowers/sdd/2026-11-08-under-gallery-arc-placement/current-identity-audit-v002.json)。

只读源码追踪确认：实际视觉只在`FramePublished`后采样；本案只有P450/P454两个发布，不是观察器漏掉三条已发布事件。`ProcessFrame`里的新root＋旧pose不能当成新视觉，`PhysicsFrame`又是节点前边界。下一合理入口是有界的post-Motor真实contact账本与实际发布视觉双通道诊断，保留缺发布事实及旧INSUFFICIENT，不插值/主动Present/放宽原门，更不据此猜修生产动作。资源所有权根因仍无已证候选，不擅动GC/cache/引擎/借用资源或新增崩溃采集。

## 同步核实的敌人边界

当前保存Level确实选择开发ResidentContent并自动接入50个稳定ID，保留旧26个encounter名额；固定home/territory/路线已配置，五族8/12/10/10/10、六职能16/7/8/6/9/4不是另加敌人的缺位清单。24子型目前绑定`v001`开发占位场景，不能报成24套正式美术。v291全50自动出生及单身份跨区HP/死亡保持是受控证据，不是全76实际发现→攻击→脱战→Returning→巡逻及跨区组合验收；E03文字已据实际绑定校准，其他完整目标不缩减。来源见`ResidentSceneRegistry.cs`、`ResidentAuthoredLayout.cs`、`CanyonResidentCatalog.cs`和原E01–E05待办。

原T03的正常速度可读性/正式美术、相对Player CCD、完整敌人共存/全部相位、硬件真人、历史资源根因与原完整十一项继续OPEN；Task1完整严格门未满足，不写complete，不缩减/暂停原目标。关键配置图已经留档，无游戏窗口/录像/付费生成。2026-11-08T16:00:14Z（北京时间11月9日）仅一次媒体Check：18,882,711字节/182文件、新增18,879,203字节，未到20GiB，Notify=false；不建自动化、不停工、不打包或删除。详见[设计](../superpowers/specs/2026-11-08-under-gallery-arc-placement-design.md)、[计划](../superpowers/plans/2026-11-08-under-gallery-arc-placement.md)和[执行ledger](../../.superpowers/sdd/2026-11-08-under-gallery-arc-placement/progress.md)。
