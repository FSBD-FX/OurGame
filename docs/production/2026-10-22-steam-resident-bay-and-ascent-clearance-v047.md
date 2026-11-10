# 蒸汽常驻侧层、惯性通行与上行净空 v047

2026-10-22，Asia/Shanghai。总目录 `G:/knight`，工程 `G:/knight/Knight`。本轮有真实生产代码、保存场景和原生验证进展；完整序章1–11及常驻计划Tasks1–4仍ACTIVE，不是成品试玩或发行。

## 事实：当前实际内容

- 地图生产器新增可独立替换skin/碰撞的 `SteamResidentBay`：世界矩形 **(3680,3912,200,24)**，复用TempleMetal灰盒，不是最终美术。
- 专项使用实际保存的 `scrap-wanderer` 占位叶、stable ID `canyon.v2.resident.steam-valves.02`，家点(3780,3880)，原36×64身体、±64巡逻；完整活动体(3698,3848,164,64)，Grow24为(3674,3824,212,112)。没有缩体、缩巡逻、提高搜索上限或删危险区。
- `SteamValves`实际源码区域改为世界(3040,3800,1160,520)，向上扩160；保留32px空气采样相位，但排序和联合选择仍须重新实测。整个Basin边界未改，旧波次arena仍(3040,3960,800,360)。这会改变整条1160宽区域的识别及背景范围，不只是新棚。
- 真实生成的Descent+Basin、另三份canonical组合为135地形：原127＋Frozen5＋Gear2＋Steam1；同源小地图和五快照父场景135、六文件搬移读回通过。**没有把135覆盖到正式127保存场景。**
- 原26身份/36个primary与alternate保留记录、完整canonical环境/潜在jet与移动机关保留区，均参与检查；原Player、机械爪、气力、重力/惯性参数没改。

主要源码：`tools/build/prologue/CanyonContentBuilder.cs`、`scripts/prologue/seamless/CanyonWorldLayout.cs`；新增专项 `tests/integration/CanyonResidentPlacementTests.Steam.cs`，沿用原原生fixture/保存叶/名册与严格owner。

## 从实际失败到当前位置

全部case在 `G:/knight/验证证据/october13-stable-claw/`，日期前缀20261022；未覆盖或删除失败原件。

1. `resident-steam-alcove-021856-551-290cd97b8258425d81dbe48a215321b2`、修订后的`022304-875-a29890241d0e48a49ddfc0c294c78633`，native均exit1/1287ms：真实完整身体超出旧区域且缺少支撑，随后才增加实体与区域空间，不以编译错误当RED。
2. 最初东侧棚(3860,3912,200,24)曾通过局部巡逻/单跳访问，但**另两个旧NE抓取窗口实际先撞棚**。`024342-680-c83941f3c6ee4d889567ce65294dab46` native1/1462ms，强化断言明确失败。没有把一条正射线冒称保住所有窗口。
3. `024518-843-097688b61f024df2bd18c695eacdaffd`的对角AABB试探过于保守，会把原3956平台也包入，不能据此唯一归因。换用真实40×72胶囊CastMotion并独立检查两端。`024708-375-4de991a7f8d144b286a9b439c0b86e07` native1/1567ms：canonical安全比例1，新东棚仅**0.34765625**。这是明确的新实体阻挡证据。
4. 整棚左移180，厚度/身体/巡逻/危险边界不变。当前`024925-070-8a81479ca30c4e96851452063102f7b3` native0/10630ms：canonical和candidate胶囊固定线扫都为1，双方三条旧post射线全部保留。**固定线扫不是实际K控制器上行或完整回环。**
5. 独立审查发现专项新建的五个临时定义未确定性释放，`022848-803-046d242e4a044b80add9721c88bcae30` native1/1528ms直接复现ID未释放。仅对自己新建的五个定义使用finally Dispose，借用的GD.Load场景不Dispose；当前真实断言五个owned ID失效、五个borrowed scene仍有效。不能据此关闭历史RID/Handle/音频问题。

原齿轮左棚三次连续测试也保留：最后`resident-gear-left-access-020036-031-b86f3d1467c94abbb09ccf7b5420f570` native1/7230ms，走西侧开口后D解析选中原AbyssSafeBottom底面，而非新左棚；第二cast/新棚/出口没有到达。左棚连续抵达仍OPEN，没有重复第四次盲改拉力或声明通行完成。

## 实际运动与玩家通行

当前保存Scrap专项360次PhysicsFrame→ProcessFrame观测：X3717.5962～3842.4038、最大相邻二维步长0.8000488；两端与回家均实际发生，完整64px身体保持支撑，HP3、治疗0。360是观测次数，不冒称精确360个无遗漏物理tick。

同一实际保存Player只在入树前设置一次初始位置：旧 `ShortcutStep3796` → A向西离台、D反刹 → 新棚 → D离棚、A反刹 → 原 `ShortcutStep4116`。新棚实际停稳(3771.994,3875.9336)、速度0。最终 **80次输入观察、162次独立逐physics检查，HP5→5，跳跃/冲刺/爪/治疗均0**。没有挂载后位置、速度或HP写入；全40×72身体每physics都避开原完整Hazard/MovingOccupancy，包含潜在jet，而非只看终点血量。

边界：环境callbacks停止；居民先释放再走Player；不是occupied战斗、活动机关完整周期、营地起步、完整上行、OS真人操作、正常世界或性能证明。旧3956→3796高差160的正式回返需要原sideboost，不可描述为普通单跳。

## 当前全50联合空间结果

同版`resident-map-census-025135-888-d1e0d63b0a89420eaaba27b6c2829421`：**47候选／3未解**，1641次corridor检查、76个joint节点、20999条诊断；body50/子型24、配置候选50/24，`actual_ready=false`。搜索仍capped，未证明地图不可能，也不是47只完成。

未解保持：

- `archive-side-cave.03`：bone-charger。
- `archive-side-cave.04`：bone-diver。
- `pendulum-gallery.04`：precision-laser。

新棚联合选择是 **Steam.01、home(3770,3880)**；专项运动是 **Steam.02、home(3780,3880)**，身份不同且差10px，不能继承精确位置的运动认证。联合Steam.02回到旧(3322,4084)，BoneHeavy保留(3620,4074)，Missile变为(4133,3927)，这些仍需各自实际活动/攻击/玩家应对验证。

所有50正式Placement仍null、24正式子型仍Unimplemented、正式新增0；原26＋新50＝76普通＋Boss1、Bone10/Ghost10包含其中，完整范围不减。

## 同源最终B组回归

统一命令为现有owner `verify-claw-knight-v001.ps1 -Phase <phase> -SourceRows 1704`；标准普通Rebuild，无诊断编译常量、无ExpectedFailure豁免、无movie/fixedFps。Steam专项＋session9788串行余十一项，同一source/binary。共 **10PASS／2严格FAIL**。

| Phase | 20261022 case后缀 | Build ms | Native ms | Strict |
|---|---|---:|---:|---|
| resident-steam-alcove | 024925-070-8a81479ca30c4e96851452063102f7b3 | 30014 | 10630 | PASS |
| resident-map-census | 025135-888-d1e0d63b0a89420eaaba27b6c2829421 | 26466 | 25821 | PASS |
| resident-parent | 025235-604-91d5e85fd7d44ceeb8c0c42a7f102ae1 | 23328 | 2579 | PASS |
| resident-gear-alcoves | 025305-652-997b8d26864949f1bf66540074205d7d | 23267 | 9806 | PASS |
| resident-abyss-alcove | 025343-193-f1ae77c299ba4806acded83b4702323d | 26837 | 8878 | PASS |
| resident-frozen-player | 025422-836-295c5c33ced84b93ad387e490d1a75c6 | 22638 | 5324 | PASS |
| resident-allocation | 025454-081-a42fa6f41962465a9740c268b71d8f2e | 22274 | 524 | PASS |
| resident-void-stream | 025520-458-296a8c2c15754bcfa36b44023129e28e | 21704 | 1573 | FAIL |
| keyboard | 025547-079-60aa4a249cd3418b821e3d57e24d81b6 | 23593 | 9581 | PASS |
| actions | 025623-536-fde128c1a0874c23823b60d5e3431202 | 21775 | 7080 | PASS |
| prologue-pre | 025655-512-4eac7b6341614512b80a6ed8a5571b36 | 21957 | 99788 | PASS |
| prologue-post | 025900-841-4da7049055474818b413a14be44660b7 | 21721 | 876 | FAIL |

累计Build285574ms、Native182460ms，不是端到端工时。全部build exit0、0警告/错误；所有case无超时、guard0、豁免空。除post native1外其他native0；退出0不覆盖严格失败。

两项失败：

1. **Void退出资源故障仍在**：stdout一条ERROR，报告187个`P12GodotShape2D` RID；stderr空，external无诊断。不是187条ERROR，不能因行为成功或exit0关闭。
2. **正式保存区域标记与新代码不一致**：B组post在`AssertV2Markers:558`实际失败，期望local(1760,200,1160,520)，保存marker仍(1760,360,1160,360)。stderr E1/external镜像E1、stdout E0，所有W/scope0。wrapper的“scope receipts differ”是提前终止的次级症状，不能当日志误报或套用旧4警告；后续75案没有完成。A组较早还有旧graph测试literal未迁移的独立失败，已改literal后才显露本项真实保存metadata问题；A组10/2原件亦保留，未拼成当前通过。

正式127保存metadata不一致有实际入口/区域识别风险；在生产同步及实际验收前，不把本工作版本作为新试玩交付。历史Handle/FATAL/154或31RID/音频故障亦不因本次pre安静关闭。

## Custody与储存

独立只读审计：24份source清单均1704行／874 C#；60组source/helper/user/continuation/binary前后清单不变，4282份留存输入实体、每例六binary及终态live均无hash差。104个真实user文件只hash核验，其中存档/设置3个，不是104个存档；continuation为EMPTY、0条输入。PID、完整启动命令、wait与provenance一致。

```
source 11100866F45F522B09FF43468F30BEE419FBCEB847B2E5F1C02C82CACDFF4CB2
helper9 BF1EDAE398128DE48B5EEB9ED374BFBFB263BCA7C90A13CC0B3D3174720CFCE9
pre-helper10 D07D7327AC6BF790F003CBD92CAEAF234F4A5029A2C4F0FA5B9F69416234F598
binary6 596E39151980F4A45771C0D0CD659081E63B82B69128D8D6B83FF92E4A697ED0
```

HEAD仍`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。owner AST0、scoped diff check0，仅既有CRLF advisory。所有owned native终态后才更新这些文档。

没有付费美术、OS输入、Git写入、ZIP/发布、新自动化、删除或引擎/依赖/GC/cache/borrowed Dispose workaround。没有新展示图稿/实录；手动一次媒体检查4838字节／基线3508／新增1330，Notify=false，未重置基线、不重建旧归档。

## 推理、风险与下一步

1. 先用窄C#生产入口同步 **3个既有节点的5项metadata/非碰撞视觉字段**，不运行会整体发布候选物理的BuildCanyonBasin。marker.local_bounds和Position；readability.Position和Polygon；macro.CompositionBounds。Macro自身Position保持0，防止双重偏移。
2. 从原canonical副本＋新builder detached donor，仅复制上述allowlist到唯一隔离输出，SaveAndValidate；验证原Basin26／全图127碰撞、所有非allowlist字段/节点/旧actor/controls/checkpoints以及小地图SerializedValues/Revision完全不变。它会改变非碰撞背景绘制范围，不能称像素完全不变。验证后才生产该五字段、重跑真正有输入变化的完整post，不改owner豁免。
3. 再验证新增层的实际K连续上行、占用战斗/活动机关，修齿轮左棚路线，解决Archive2/Pendulum1与全50实际身份/HP/攻击/返航/卸载/重置历史，之后才正式接入场景和manager。
4. 完整目标仍包括最终原创统一角色/敌人/场景/UI/动作/VFX/音频/明确CG、同版正常Main+Explore/Boss/支撑出口/存档旧档、720p–4K/正常显示真实60FPS、连续视听及真人试玩、Windows ZIP和获授权GitHub归档。不能缩为本轮空间候选或自动测试，不能承诺十万付费人数。

本轮地图技能使承重层、skin、碰撞和场景metadata保持分离，测试先行抓出了真实旧爪路径阻挡；代价是新增区域会影响分类/背景与后续遭遇，必须继续整体验证。内部sweep断言有.9999容差，但严格外层要求字面1，本批实测确为1；这是低优先级一致性项，不借它冒认实际爪运动。
