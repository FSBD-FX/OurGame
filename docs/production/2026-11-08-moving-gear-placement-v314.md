# v314 · 往返齿轮正式地图接入

## 事实

原完整十一项目标保持 ACTIVE，工程仍为 `G:/knight/Knight`，未切 UE。本轮只做无窗口构建、测试和离线配置图；未打开游戏窗口、发送系统键鼠、付费生成、采集新dump、建自动化、改Git、删除资料或写真实玩家存档。Task 1 的运行内核见 v313；本轮推进 Task 2 保存地图与 Task 3 当前同版整路线门，不把受控站位或静态地图算正常声画／真人接受。

正式 `CanyonBasin.tscn` 已增加一个真实 `UpperGalleryMovingGear`，位于原 `UpperGearGalleryTwo`：世界起点(3400,3550)、终点(3400,3630)，半径24、周期3.2秒、伤害2、接触间隔.5秒。没有新增 solid、抓爪目标或改变玩家数值。158结构矩形、76普通敌人配置位、9残片、原门／支路／检查点均保留；这些数量不是同屏活体数、全部击败数或精美度保证。

生产 authoring 方法同时用于未来 `BuildBasin`，但本次不完整重建旧地图：实际 native 差异检查发现 full factory 会把旧3840宽背景重建成5120，属于本机关之外的变化。候选采用实际保存叶实例＋同一 authoring 操作，保存重载后比较，并由独立审查直接核对真实tscn，叶仅新增circle资源和gear／collision两节点。

原正式父场景本已外部引用 Basin，地形catalog没有变化，因此未强行重写它。候选实际加载当前父图，仅以新定义绑定独立候选叶；晋升后再冷加载未改写的原父场景完成相同原生测试。

晋升前核对所有祖先非reparse、当前叶及父hash与case的before原件一致，才正常更新唯一生成叶文件。旧叶保留在 `thruster-moving-gear-candidate-20261108-085608-941-08a973a6a095471b979abb1b27a32a1d/before/scenes/prologue/sectors/CanyonBasin.tscn`。

- 旧叶：`BCD9AEAF2C0C0D543C4A027778782FE6BFE360D58A31A8D029613067D9D482F9`。
- 新叶：`3DCDF1C28F8EC50D4952B023FE2BA3D8E4C865EEFF50779C0739ACDE1BB1AFA5`。
- 父保持：`D4AE2DC2F100B3C22908998BC0143D46033160BA2F96A590CE7BAAD64F63988E`。

## 已验证与正在验证

证据根为 `G:/knight/验证证据/october13-stable-claw/`；无命名错误豁免，所有失败原件保留。

| 阶段 | 结果 | case |
|---|---|---|
| 保存叶缺机关 RED | 1906输入，exit1／710ms，明确缺保存节点 | `thruster-moving-gear-layout-20261108-083426-963-1ade4566a8ad4dd5a56078113f16cce5` |
| 生成叶缺机关 RED | 1906输入，exit1／671ms，明确缺生成节点 | `thruster-moving-gear-candidate-20261108-083526-430-1b55feec37c849f3a36342095ed43726` |
| 首次保真检查 | FAIL；只加有界差异后查出0/-0表示及真正旧背景重建差异 | `083650-219-652b977…`、`083823-295-8c3349…`（同candidate前缀） |
| 候选原父图＋局部native | 1907输入，严格PASS／22484ms | `thruster-moving-gear-candidate-20261108-084625-427-a05c4760d3544678b86d297e12a7669a` |
| 审查I1复跑分支 RED | exit1／894ms，已含gear重载输入触发生产重复保护 | `thruster-moving-gear-candidate-20261108-085450-945-08fc51e192b1495ea36934bb0721d704` |
| I1一次修补＋完整候选GREEN | 1907输入，严格PASS／22635ms | `thruster-moving-gear-candidate-20261108-085608-941-08a973a6a095471b979abb1b27a32a1d` |
| 正式叶＋未改写原父图 | 1907输入，严格PASS／21717ms | `thruster-moving-gear-layout-20261108-085804-186-a26635ba6df946ef82f0fa4bcf534ba7` |
| 配置图缺轨道数据 RED | exit1／1347ms，明确拒绝只有锚点、无全轨道的数据 | `resident-map-preview-20261108-084843-157-bafa34f1ba2345569c15908d5d24e9ee` |
| 正式保存图数据GREEN | 1907输入，严格PASS／1439ms | `resident-map-preview-20261108-085937-874-5c35a8b00456409b82840afe4541b134` |
| 当前正常键序主线 | 1907输入，严格PASS／253831ms；详细结果继续核对 | `thruster-route-main-diagnostic-20261108-090200-725-8c44c7bb63294de99cdde5c7a497aba0` |
| 当前正常键序探索线 | 1907输入，严格PASS／333789ms；详细结果继续核对 | `thruster-route-exploration-diagnostic-20261108-090744-764-db9e7ce1f5654e56ae5e0a6aa7489dba` |
| 当前完整普通前段 | **严格FAIL**；engine虽exit0／162521ms，5条句柄错误被日志门拒绝 | `prologue-pre-20261108-091514-429-ad45a410de064c3aa5030acfdfc06ad8` |
| 当前完整普通后段 | 1907输入，严格PASS／196515ms，75项 | `prologue-post-20261108-091925-954-6d1d7712d2e94afc9b7a2401d6eb103b` |
| 最终同版gear内核 | 1907输入，严格PASS／4040ms | `thruster-moving-gear-20261108-092545-446-25abdfdf512044ce866d4c5690dfd5f6` |
| 最终同版旧机关 | 1907输入，严格PASS／25824ms | `recycling-traps-20261108-092623-494-795a6c93af4746a09273e34bea996f27` |
| 最终同版models | 1907输入，严格PASS／475ms | `models-20261108-092721-172-e54779e629a345af95f36f48f0afe7c0` |

局部 native 用真实保存父图／staged stream／保存 Player，唯一站位写入在 AddChild 之前。之后没有直接写位置、速度、HP或齿轮clock，也不用普通二段跳、Shift或K。两侧(3310,3640)／(3490,3640)真实胶囊均在原平台有向上碰撞接触；等齿轮升起后用真实A/D往返两趟，无伤／零恢复；错误时机接近产生真实gear DamageAttackIdentity及2点伤害，仍可退回安全侧。全过程保持原世界相机范围，完整40×72胶囊在viewport四边均留16px。

原生Fog使用真实 controller 的 FramePreDraw 订阅；headless测试明确人工提交该信号，不声称自然渲染。只遮当前下端圆、同时暴露原A圆片段的实墙能令gear提示清零，撤墙恢复；hidden／禁圆／暂停／排队退出同样撤销。它证明新圆不会沿用旧位置提示，不能证明像素美术或全部区域卸载重挂键序。

同版双路线的详细独立核对：Title真实Start输入、同进程持久Player、Boss／实体出口及完成事件各一次，恢复均0/0；普通跳连续14242／18111组前后采样，接受21／33次全部接地。离地Space边沿2／8个没有形成额外普通跳，辅助运动不混算。主线三门挂载缺口0；探索为1个既有受约束近侧卸载重绑epoch，不误报全程零缺口。准入是三面真实状态＋左侧授权通行，不签这两路线撞测了顶／右侧强闯。

两案完成后都再观察60次真实D保持，角色位置、胶囊、速度、完成次数和保存字节不变；physics停处理而非强制清零，原速度仍(260,0)。主线6片，探索7片；探索11组激活／10组击败，配重组未击败，不报全灭。主线Scatter／Burst／Laser、探索Missile／Laser均有实际预警／发射／受击及击败事件；不是每条线覆盖全部远程种类。主线完成／held-end／磁盘保存SHA均为`0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`，探索均为`24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`。同一数字Player ID在不同进程不解释成跨进程同一对象。

不过当前完整前段再次出现 `GCHandle.FromIntPtr`／`ScriptManagerBridge.SwapGCHandleForType` 的5条“Handle is not initialized”错误；canonical正确严格失败，没有因44项执行完或engine退出0而放行。它是历史同类错误重现，尚不能证明与本次gear相关或不相关，更不能签根因已修。原失败保留、不重跑筛绿；完整本版技术门仍未通过，继续后段及只读边界定位。

既有v311只读parser完整解析180行／90对／5根，`traceComplete=true`、`rawCasePassed=false`、`causeProved=false`。5条错误均落在 `check lifecycle=true damage=3` 的sample Load日志区间（external行278..439，错误279/311/343/375/407），不是本次staged AddChild区间。同步日志顺序不能识别具体失效对象或证明因果；这比沿用旧v309挂载定位更准确，下一次调查应保留本次Load边界。

## 配置图与保全

`G:/knight/过程图稿与视频/2026-11-08_往返齿轮地图_v314/` 包含 original／presentation 各一份 JSON、SVG、3200×1800 PNG、HTML，原件另含renderer，`source-version-status.json`逐文件绑定来源／大小／SHA。原v307图不覆盖。根已查看实际PNG：R01关联M19，画两端实际半径圆＋中心线；保守AABB仅列HTML，不画成实际伤害区。H01仍关联旧M18；31逻辑锚点、56对象记录不混算为独立陷阱数。

数据SHA：`28917BED3172638208648F1974B267DC353A1AA93C1C2A40437A853793757BA7`。

最终9阶段：8严格PASS／1严格FAIL（普通pre），不是全绿。9案前后1907输入清单均SHA：`43F214C27588E654176DA593DF0C21741A5384D4B29A63A9C29A1B087D74F50B`；六产物前后清单均为`6876CAFF88F67D954F27C07DA0B786F9545367165507C169BD67DA8AA737B0D4`，所有guardFailures为空。前段的日志失败没有被同源／无漂移掩盖。

后段沿用既有300000ms上限，只有原合同seamless1／room-profile1／flow-registry2四条负例警告，无额外错误；196515ms不能签旧180000ms或硬件性能。它不能冲销同源前段FAIL。

PS先有真实RED：旧renderer对38种非法轨道错误接纳；修补后纯数据40控全过，夹具明确非native、不复制成功provenance。根对真实新JSON另跑1正／59反自测，并对实际出图跑18出版控制（原12＋6轨道）全部通过；旧熔炉范围19控制也全部通过（`storage/v314-root-old-volume-01/newvolume-validation-only-results.json`）。根的出版回执在最终map案 `storage/v314-root-map-controls-01/review-controls.json`。

## 推理、决策代价与风险

1. 保留当前叶和父，仅增加新机关，而不顺带重建旧背景。依据是native比对确有背景变化；代价是旧背景覆盖问题仍待其独立视觉任务，不冒称已修。
2. 原父已有外部叶引用且catalog无变，只更新受检叶并原样复测父。代价是隔离候选父依赖其保留叶，不把这份候选当自包含发行包。
3. 仅将独立离线PS交给worker，与root独占C#/canonical/引擎分开；PS不在native源码/helper清单内，最终单独冻结／实测／只读复核。代价是必须另外维护并核对PS与媒体来源哈希，不能用native通过替代它。

一次C#接入审查C0/I1/M1：I1为正式叶含gear后候选入口无法复跑，已真实已含节点输入RED→仅缺节点才authoring→完整候选GREEN，保留生产重复保护；不进行重复审查。Minor为指纹比较不涵盖所有Godot内建存储属性、匿名资源及数组，本轮有真实tscn独立比对，不将该测试名称外推为未来全部保真保证。

新增PS子域单次审查C0/I0/M1：内部validation-only夹具创建前没有单独拒绝祖先junction；当前固定普通路径无此情况，生产renderer仍拒绝reparse、禁止覆盖。此边界如实延期，不授权任意新路径写入。v313两项Minor（多body-shape去重、伤害callback内queue/remove原生分支）也未冲销。

历史间歇资源根因、全部相对高速玩家连续碰撞、全图／全键序／76活体覆盖、正式A方向分层和动作设计、布料、正常视听／性能／真人仍未验收。本轮内核／地图程序通过不等于T03正式观感或原十一项完成，不保证十万付费玩家。Task2程序接入完成，Task3因完整pre门未过仍开放；下一步优先沿本次sample Load区间调查资源稳定性，不未经证据改引擎／缓存／GC或反复重跑筛绿。目标保持ACTIVE，不作发行、GitHub快照或玩家验收。

过程媒体仅此关键地图节点及明确标识的出版测试夹具，没有游戏录屏或批量帧。正常汇报前仅执行一次现有 `-Check`：15,203,531字节／145文件，Notify=false，未达新增20GiB；未建自动化或进行删除／重新打包。
