# 当前geometry匿名finalizer：限定路径所有权核对

本轮只读，不是资源修复或新原生采集。当前案为`G:/knight/验证证据/october13-stable-claw/resident-geometry-20261104-161554-680-84f5b1c1353f4b9b8f4f05c824a84536`，9547ms/exit-1073741795、无timeout。只知GodotObject.Finalize→GC.RunFinalizers中的gchandle.is_released，不能由最后BODY_CANDIDATE归因资源类或新CheckPatrol。

## 范围及源绑定

独立只读agent核对当前7文件SHA与该案source-before逐项一致：CanyonResidentPlacementTests、ResidentSceneBindingTests、ResidentEnvironmentInventory、ResidentPlacementGeometry、BuildResidentSubtypeScenes、MinimapTerrainCatalogBuilder、SceneBuildUtil。仅读fixture建立至首个low-ceiling-bridge.01候选的实际路径及直接依赖，未全库扫描、启动引擎、构建、调用GetState/GC/实例化、作保活/延迟/新dump实验。

下表C为[当前测试](../../tests/integration/CanyonResidentPlacementTests.cs)，G为[几何查询](../../scripts/prologue/residents/ResidentPlacementGeometry.cs)。行号是本次冻结快照定位，不承诺未来不变。

| 取得点 | 所有权与真实持有 | 释放/跨边界 |
| --- | --- | --- |
| C2159 World2D；Space | World私有，fixture `_world`持有；DirectSpaceState借用 | 跨Sync await及首候选，C2276最终Dispose World，此前未释放 |
| C2176五次GD.Load/PackedScene/sector | 共享加载来源未显式Dispose；实例由viewport→Scope及Sectors持有 | 最终viewport释放sector，跨await保持 |
| C2181原shape及Duplicate | 原shape借用存`_borrowedOriginals`，副本由`_ownedShapes`和collider持有 | 最终先viewport Free，等两次Process，再Dispose私有shape；借用仅Clear |
| C2208–2213 ray/结果、C2251 NativeHits | query/集合私有、命中node借用 | 同步using释放，返回标量ID，无await |
| C1848/1984环境与player-access探针 | Moth节点私有、shape/config借用；额外shape副本或矩形登记fixture.Own | Moth局部finally Free；临时置null的借用shape finally恢复；额外shape由fixture最终释放 |
| C1328 ArchiveResidentSpace | 敌探针字典、Player、离树Level方法持有；Catalog/Definitions/scene借用 | 同步方法各finally Free，Archive回执时节点已释放，无await |
| C1367 MinimapTerrainCatalogBuilder.Build | 新Catalog调用方using；借用Definition.Scene，每个sector临时实例化，shape只读 | builder逐sector finally Free，Catalog退出Dispose，仅几何标量返回，无显式SceneState |
| C1755/1507旧36预约 | 九类原始scene各一个探针，本轮probes字典持有；shape借用 | BodyCensus结束/异常finally Free；首候选仍持有；36是记录数，不是36实例，无await |
| C933 leafSource.Raw | leaf共享PackedScene，实例/Profile/shape由`leafSource._raw`持有 | 身份验证失败立即Free，成功保持至Run退出Dispose，不每候选释放 |
| G213/264/320/325/330 body查询 | self排除集合、footStrip、PhysicsQuery/结果包装私有；actor/floor/电场shape借用 | 同步using各自释放，body结果只含Rect/ID/issues，不持Resource或跨await |
| G154 archive corridor | 私有RectangleShape | 同步方法using释放，不逃逸 |

ResidentEnvironmentInventory.Capture读取现有node/shape并复制成矩形/字符串；children集合using，不取得或Dispose Resource/SceneState。GetPathTo等临时包装未全部显式释放，只能列为finalizer负载来源，**不是已证明双Dispose**。

## 不能跳过的更早来源

C655在fixture之前先执行BuildCandidatePackage：创建/保存24个私有Profile逐个using Dispose；BuildResidentSubtypeScenes取得Ignore输入、Duplicate profile、IgnoreDeep carrier和Ignore readback PackedScene，copy先Free再saved释放，actor先Free再carrier/profile/input出作用域。SceneBuildUtil.PackAndSave另建私有PackedScene与roundtrip copy，copy先Free再packed释放。

这些变量没有明确的重复显式Dispose，但调用点不能证明加载图中的脚本子资源、默认构造值和替换后旧wrapper的native关系。它们可能晚些终结，不能因FATAL出现在首候选附近就排除或直接指认。

## 旧证据复用及结论边界

旧`prologue-pre-refcounted-identity-20261023-220340-916-95e4b48f08af41728fa07757d1e7667b`记录RangedWeaponSettings managed_serial101/call52/disposingFalse，只有ENTER后fatal，native_identity仍unknown。558次reader返回/九路径各62/471个native PackedScene身份证明来源更替，不是磁盘次数或内部Settings所有权。不能将旧序号搬到当前匿名案。

旧manager source custody/postmount案`resident-level-lifecycle-20261023-222330-199-3248c30ff0ed4e60b6349885c422ade6`通过的是当时挂载/独立owner/失效重取/失败重试/不Dispose借用资源合同；并不覆盖当前BodyCensus里的九来源探针字典，也不证明finalizer已修。

**当前限定路径没有找到明确双重Dispose或释放后继续用借用对象；具体失败所有者仍未知。** 没有显式GetState路径不等于引擎内部没有SceneState。首候选后的Body/ActivityCensus与新20组CheckPatrol尚无当前成功回执，不能拿日志末行指定故障点。

下一资源侧最小动作只可先查已有identity日志中是否已有“更早builder／已Free探针／仍持有探针”的同serial来源；没有就保持未知。不要反复统计九路径身份，也不要凭猜测增加GC/保活/Dispose/延时。新原生崩溃采集、引擎替换或范围外调试仍须分别授权；其他可做的正式开发与回归不因未知而停止。

## 2026-11-04 既存日志的同serial来源关联结果

仅对上文Oct23原identity案的engine.stderr.log检索managed_serial=101、managed_hash=30542218及原始指针0x0000023308C3F940：同serial/hash/指针只出现于3502行的call52 ENTER，仍为native_identity=unknown，没有相匹配的OBSERVED或来源记录。因此这份既存日志不足以把失败wrapper关联到更早builder、已Free探针或仍持有探针；不能用相邻reader记录或其他案的相同序号补齐身份。本次读取并未识别当前v243两个匿名FATAL的具体对象，也没有新增采集、构建、原生测试或尝试资源修复。后续不要重复本次无关联检索当作新证据，新增采集或引擎变更仍保持原授权边界。

## 官方同commit源码与上游问题核对（2026-11-04，只读）

运行日志版本a13da4feb与[官方版本提交](https://github.com/godotengine/godot/commit/a13da4feb)及version.py一致。直接HTTPS读取该commit原始文本、保留空行计数后，csharp_script.cpp的1785为mono_object_disposed_baseref、1788为released-handle检查；GodotObject.base.cs的100为finalizer调用、137为refcounted释放入口，与既存栈一致。网页解析行号会去空行，不能把解析器的1656误认为二进制/源码不匹配。[C++原文](https://raw.githubusercontent.com/godotengine/godot/a13da4feb/modules/mono/csharp_script.cpp)，[C#原文](https://raw.githubusercontent.com/godotengine/godot/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/GodotObject.base.cs)。

该前置检查早于此函数内的断信号、引用计数递减和释放锁调用；interop先取当前CSharpInstance再进入它。因此单凭这条栈，不能证明项目某处显式重复Dispose、不能识别资源拥有者，也不能证明一定只有引擎原因。[interop原文](https://raw.githubusercontent.com/godotengine/godot/a13da4feb/modules/mono/glue/runtime_interop.cpp)。

[上游#83762](https://github.com/godotengine/godot/issues/83762)仍有近期同类终结器/资源复活报告；贡献者10月28日说明线程安全修复仍有剩余环节。该讨论支持把引擎生命周期竞态列为待核假设，不证明KNIGHT是同一对象/根因。未采用讨论中的静态保活、强制GC、运行时调参或非正式引擎补丁；没有新增原生采集，也没有修改引擎/项目资源策略。下一资源修复必须据本地对象来源和完整原门验证，不以升级、拆测试筛绿或移除检查代替。
