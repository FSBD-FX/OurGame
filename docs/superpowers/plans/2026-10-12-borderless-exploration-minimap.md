# 无边框探索小地图 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在正式连续序章右上角提供不会隔墙泄露路线、保留已探索地形及已发现检查点的无边框局部小地图。

**Architecture:** 从关卡构建器的结构碰撞生成稳定地形目录；纯探索模型分别记录自由细格和可见前表面，消费有效的真实遮挡快照。流程控制器独占保存权限，HUD只读探索数据；既有头灯、门禁、输入和物理不为地图改变。

**Tech Stack:** 现有Godot 4.7.1 Mono／C#项目、Godot CanvasItem与局部纹理、System.Text.Json；沿用当前SDK，不增加外部依赖或生成美术。

**Spec:** `docs/superpowers/specs/2026-10-12-borderless-exploration-minimap-design.md`，用户已批准v001；2026-10-12独立只读计划审阅已完成，按下方勘误开始Native实施，当前没有可见小地图产品实装。开发执行方式由当前代理选定，不再为非关键流程选择暂停。

## Global Constraints

- 保持同一角色和连续世界；不修改移动、机械爪、机关、Boss条件、摔伤或9残片／最低6件规则。
- 候选探索细格8世界单位；候选半径160世界单位。两类位集分别最多1048576位／128KB，合计256KB。
- 每物理步最多64个候选，单候选最多256遮挡形状，合计最多16384次有限几何检查；未知／溢出停止新揭示。
- 1280×720基准260×170逻辑像素；提示下方至少12逻辑像素间距。测试1280／1920／4K及UI .8／1／1.5。
- 仅Playing及Boss战显示；标题、设置、暂停／恢复覆盖层和结算期间隐藏。禁止获取键盘焦点或拦截鼠标。
- schema4可选独立explorationMap／formatVersion1；合法旧档缺扩展为空图，坏扩展不能损坏合法核心进度。
- 不每格写盘，不改变原拾取先提交后变更事务；所有运行测试外部隔离，禁止默认user://综合入口。
- 不付费生成、不恢复自动化、不发布、不更改真实用户存档；本脏工作树保留他人改动。本轮任务“检查点”保存精确diff与测试收据，不自动git add／commit。
- 展示前保存原件和展示副本、来源／版本／状态索引到`E:/OurGame/Knight/过程图稿与视频`的新日期主题版本目录。

**经独立审阅采纳的必要勘误：** 允许局部视窗移出缓存覆盖范围时重建局部遮罩；否则没有新探索的旧路返回会缺图或错位。平滑移动仅修改采样变换，禁止每帧重建整图。此例外不新增探索授权。另将纯存档DTO提前到任务2（codec／IO留任务3），明确开门等遮挡变化的有界重试，以及texel完整footprint授权；不扩张显示或玩法范围。

## Review Focus

1. 已卸载区域与失效快照：完整静态目录不等于实际观察权限；跨缝／卸载／回返不泄露未看区域（任务2、4）。
2. 同格薄墙、非网格对齐与数值退化：既不能隔墙揭示，也不能让所有前表面永久消失（任务1、2）。
3. 可解析JSON中的地图类型错误／超长编码／revision变化：只丢地图扩展，核心档和门禁不变（任务3）。
4. 退出保存失败时按Esc、取消和连续重试：不瞬间解除暂停，不重复提交，不意外退出（任务4）。
5. 大UI、旧路快速回返及缓存边缘：地图不压提示／Boss预警，不错位，不用模糊插值泄露背面（任务5、6）。

## 文件边界与验证约定

新增运行代码集中于`scripts/presentation/minimap/`：`MinimapTerrainCatalog.cs`（只读Resource及值类型）、`MinimapVisibility.cs`（整区域遮挡判定）、`ExplorationMapModel.cs`（有界状态／队列）、`PrologueMinimapController.cs`（生命周期适配）、`PrologueMinimapView.cs`（只读显示）。保存扩展独立于模型，放`scripts/save/ExplorationMapSaveData.cs`和`ExplorationMapSaveCodec.cs`。构建提取放`tools/build/prologue/MinimapTerrainCatalogBuilder.cs`。

任务1建立`tests/RunMinimapTests.cs`与`.superpowers/sdd/2026-10-12-minimap/verify-minimap.ps1`。wrapper采用既有verify-feature的子进程隔离、用户／源码／六二进制守卫、case-local TEMP/TMP、实际exit与日志收据；不改变旧wrapper或预算。`-Phase`为visibility／model／save／lifecycle／hud／formal／performance，`-ExpectedOutcome`为red／green。普通phase运行55s硬限，构建180s；按Task6 prepared brief及执行台账既有ruling，新正常时间三臂performance诊断预先固定120s硬限（替代本计划最初180s／150s草案，收集不得超过硬限），保留partial，超时不能GREEN。save必须另起冷读回进程，不在同进程假装持久化。

下文命令前缀`VerifyMinimap`表示在工作树根PowerShell中定义一次：

```powershell
$taskPwsh='C:/Users/22499/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe'
function VerifyMinimap([string]$Phase,[string]$Outcome) {
  & $taskPwsh -NoProfile -File '.superpowers/sdd/2026-10-12-minimap/verify-minimap.ps1' -Phase $Phase -ExpectedOutcome $Outcome
  if ($LASTEXITCODE -ne 0) { throw 'Minimap verification did not meet expected outcome' }
}
```

每任务结束保留本任务路径限定diff、失败／成功case与守卫收据；编译错误只算编译失败，行为RED须唯一预期断言，不接受异常、FATAL或超时冒充RED。执行者先读规格、此计划与当前README；所有引擎／构建串行，运行期间不编辑受守卫源码。

---

### Task 1: 整细格／前表面的保守可见性

**Files:** Create上述`MinimapTerrainCatalog.cs`、`MinimapVisibility.cs`、runner／wrapper；Test `tests/unit/MinimapVisibilityTests.cs`。

**Interfaces:** 在`Knight.Presentation.Minimap`定义`MinimapSolid(Transform2D LocalToWorld, Rect2 LocalBounds)`、`MinimapBoundary(int ShapeId, Vector2 FromLocal, Vector2 ToLocal, Vector2 OutwardLocal)`值类型。`MinimapVisibility.CellAllowed(Vector2 actor, Rect2 cell, HeadlampOcclusionFrame frame)`和`BoundaryAllowed(Vector2 actor, MinimapSolid solid, MinimapBoundary boundary, HeadlampOcclusionFrame frame)`返回bool，不持有节点／写盘／分配射线查询。

- [x] Step 1：写测试并建立编译所需的保守false接口：`OpenCellAccepted`断言空场景合法整格true；`ThinWallInsideCellRejected`／`BlockerBetweenCornerRaysRejected`断言false；`AngledNonGridFrontAcceptedBackRejected`断言近前表面true、背面false；`TargetInteriorNeverExcluded`、`NaNOrSingularRejected`、`Shape257Rejected`断言false。
- [x] Step 2：运行`VerifyMinimap visibility red`，必须在OpenCellAccepted发生行为RED，保存收据。
- [x] Step 3：实现整格与actor构成的凸包对变换矩形内部的保守相交判定，double计算、有限性检查和固定栈空间，不用中心／角点几条射线代替全扫掠。前表面允许精确终点接触，但扫掠不能进入目标自身或其他形状内部；目标碰撞仍参与判定，不复用会排除整个目标的捷径。
- [x] Step 4：运行`VerifyMinimap visibility green`；合法镜像／旋转须正确判定，角色在墙内、零长段、退化或不确定变换／贴边均fail-closed，`PhysicsQueries==0`。
- [x] Step 5：记录限定diff、独立预期几何与RED/GREEN检查点；不改现有HeadlampOcclusionFrame语义。

### Task 2: 有界探索模型与授权记忆

**Files:** Create `ExplorationMapModel.cs`、纯DTO `scripts/save/ExplorationMapSaveData.cs`（定义提前，codec／IO仍任务3）；Test `tests/unit/ExplorationMapModelTests.cs`；扩展runner的model phase。

**Interfaces:** Catalog提供`Rect2 WorldBounds`、`string Revision`、`int CellCount`、`int BoundaryCount`及`Rect2 Cell(int id)`／`MinimapSolid Solid(int id)`／`MinimapBoundary Boundary(int id)`。定义只读`MinimapObservation`值，携带`Vector2 ActorWorld`、`ulong PhysicsFrame`、`ulong WorldGeneration`、`bool CanReveal`、当前`HeadlampOcclusionFrame Occlusion`、当前已挂载区域`ReadOnlyMemory<Rect2> LoadedBounds`。模型提供`void Advance(in MinimapObservation observation)`、`bool IsCellExplored(int id)`、`bool IsBoundaryExplored(int id)`、`void Clear()`、`ulong RevisionCounter`、`ExplorationMapSaveData CaptureSave()`、`void Restore(ExplorationMapSaveData data)`；统计候选／形状检查／队列延迟，不在热路径生成日志。

- [x] Step 1：写`OnlyActualLivingPositionReveals`、`WholePatchWithin160`、`KnownSegmentNotWholeWall`、`TeleportDoesNotPaintLine`、`UnloadedSweepRejected`、`StaleQueuedPositionRechecked`、`RepeatPhysicsStampDoesNoWork`、`CapacityNeverWraps`、`StationaryDoorOpenRetriesRejectedCells`，初始模型返回未探索；断言空场景合法近格经Advance后true，远格／墙后／间路false；队列已处理完、actor及WorldGeneration不变时闭门拒绝→真实开门后有界重试可授权。
- [x] Step 2：运行`VerifyMinimap model red`，记录第一项合法近格仍false的行为RED。
- [x] Step 3：实现两固定byte位集及预分配近域候选／去重缓存；按距离近到远，有界分批，格／段全范围在160圆内才能授权。覆盖证明针对整个扫掠区域包含于当前有效挂载区域的并集，不能仅检查端点；未知／退休区域不授权。每物理stamp共64候选，重复绘制不增加预算；actor／worldGeneration变化重新验证队列，不缓存动态遮挡结果。对未授权近域候选保留固定容量重试队列，或等价的有界遮挡变化失效机制，不能因一次闭门拒绝而永久闲置；已完整授权且无候选才可闲置0分配。
- [x] Step 4：运行`VerifyMinimap model green`；增加`WarmedIdleAllocatesZero`、`Warmed64CandidatesAtMost4096Bytes`、`ChecksAtMost16384`与1048576位上限断言。报告实际队列延迟，不能把0.75s估计当硬保证或放宽遮挡。
- [x] Step 5：保留检查点和预算原样本；CaptureSave只在流程保存时复制，HUD读取模型不触发复制。

### Task 3: 可选存档扩展与独立冷读回

**Files:** Extend任务2已建立的纯DTO `scripts/save/ExplorationMapSaveData.cs`、Create `ExplorationMapSaveCodec.cs`；Modify `scripts/save/PrologueSaveData.cs`、`PrologueSaveStore.cs`的Normalize／ReadData／SaveAtomic；Test `tests/unit/ExplorationMapSaveTests.cs`、`tests/integration/ExplorationMapColdReadTests.cs`。

**Interfaces:** DTO含`FormatVersion=1`、`TerrainRevision`、`FreeCellsBase64`、`BoundarySegmentsBase64`。`ExplorationMapSaveCodec.TryRead(JsonElement root, MinimapTerrainCatalog catalog, out ExplorationMapSaveData data, out string diagnostic)`独立验证扩展；`Write(Utf8JsonWriter writer, ExplorationMapSaveData data)`只写可选explorationMap属性。`PrologueSaveStore.LoadOrNew(string path, int? requiredLayoutVersion=null, MinimapTerrainCatalog mapCatalog=null)`与`LoadFullWorld(string path, string legacyPath, MinimapTerrainCatalog mapCatalog=null)`末尾新增可选catalog参数并传到ReadData；缺目录的旧调用只忽略地图，不改变核心读回。`PrologueSaveData.ExplorationMap`用JsonIgnore，Normalize及PrologueProgress.ToSave保留独立副本；原核心JSON字段和版本不变。

- [x] Step 1：写`ValidMapRoundTrips`、`LegacyNoMapRemainsEmpty`及核心不变对照；测试当前schema4加地图对象／null／字符串／数组／未知格式／revision／超长base64／错误位数／非零padding。断言合法核心的检查点、残片、豁免、Boss及WasRecovered不变；整文件坏JSON仍按原规则恢复保护。
- [x] Step 2：运行`VerifyMinimap save red`，合法地图保存读回应因扩展尚未接入丢失而行为失败；不得拿编译失败作RED。
- [x] Step 3：先原样验证／反序列化核心，再在原始JSON中单独读取地图。写出由原SaveAtomic产生相同核心字段，再用codec追加可选属性，不给地图模块路径或writer权限。编码先限长度再解码，两位集精确匹配目录和padding；读取不重写源文件。所有schema1／2／3原迁移保持地图为空。
- [x] Step 4：运行`VerifyMinimap save green`，强制写失败／目录阻塞时旧文件hash及内存模型不变；两个独立进程写／读，另用旧核心DTO忽略地图再保存，证明只丢地图而不丢核心。非法扩展诊断非阻塞且有界。
- [x] Step 5：保存冷进程exit、原文件hash、负控和兼容检查点；真实用户槽位不参与测试。

### Task 4: 正式观察、生命周期与安全退出

**Files:** Create `PrologueMinimapController.cs`；Modify `scripts/presentation/fog/PrologueFogController.cs`（捕获后只读通知）、`scripts/prologue/PrologueGameFlowController.cs`（地图事务／退出）、`ProloguePauseMenu.cs`（退出不提前Close）、实际暂停UI构建`tools/build/prologue/BuildPrologueLevel.cs`的BuildPauseMenu及其生成场景（打包后重实例化）；Test `tests/integration/MinimapLifecycleTests.cs`。只读核查确认`BuildPrologueUi.cs`生成标题／设置／操作页而非PauseMenu，不为错误文件名新增另一套暂停界面。

**Interfaces:** 在Fog文件定义只读`FogOcclusionObservation(HeadlampOcclusionFrame Frame, Vector2 ActorWorld, ulong PhysicsFrame, ulong WorldInstanceId)`；Fog新增`event Action<FogOcclusionObservation> ObservationPresented`，在真实当前捕获结束后发布现有frame，无订阅者时不创建额外快照。Controller绑定同级level、flow、fog及view，只订阅／退订自有节点，维护模型；其适配器补齐任务2的LoadedBounds／CanReveal，world实例或挂载集合变更递增本地WorldGeneration。提供只读`MinimapTerrainCatalog Catalog`、`void StartNewExploration()`、`void RestoreExploration(ExplorationMapSaveData data)`、`ExplorationMapSaveData CaptureExploration()`。Flow在InitializeAfterLevelReady读取正式controller的Catalog并传给任务3两个loader，再恢复地图；不依赖兄弟节点Ready顺序初始化Catalog。Flow增加`bool TrySaveBeforeQuit(out string failure)`返回实际已提交结果，成功才执行保存的原QuitHandler；失败对话框提供重试／不保存退出／取消，不调用地图自行保存。

- [ ] Step 1：写`PauseNeverReveals`、`RealDeathKeepsMap`、`ContinueOnlyDurableMap`、`NewGameClearsMap`、`RetiredWorldObservationRejected`、`FragmentFailedCommitKeepsBothStates`、`QuitFailureRetryCancelStaysPaused`。退出失败断言tree.Paused和菜单IsOpen始终true，Quit调用0；重复重试一次请求一次写，取消不写、不清图。
- [ ] Step 2：运行`VerifyMinimap lifecycle red`，在正式生命周期未接入或退出提前Close处取得预期断言失败。
- [ ] Step 3：通知携带实际存活角色与当前physics stamp，不按摄像机／头灯朝向揭示。挂载覆盖从`SeamlessPrologueWorld.Definitions`和`TryGetMountedSector`提取到固定缓冲区，不每帧调用会分配的MountedSectorIds列表；边界／退休标识在节点退订时作废。Playing恢复首有效观察才重新揭示，Pause／死亡／标题不复用LastOcclusionFrame。地图数据加入原checkpoint／mechanism／fragment／completion候选；原PersistProgress只把地图快照加到独立保存候选，SaveAtomic成功后才更新Save里的地图字段，不扩大修订原核心失败语义。原有fragment候选仍先提交后变更，失败不把残片记到内存。
- [ ] Step 4：实现暂停Quit路径不提前Close；保存失败弹出键盘可操作对话框，文案明确“不保存退出将丢失上次成功保存后的探索”。弹框Esc等同取消并吞掉该次菜单关闭动作，不解暂停；无保存成功不假退出。运行`VerifyMinimap lifecycle green`，测试正常Quit／重试成功恰好一次调用原处理，结算返回标题不另造菜单流程。
- [ ] Step 5：运行既有隔离`verify-feature.ps1 -Feature formalinput -ExpectedOutcome green`及`-Feature keyboard -ExpectedOutcome green`；保存本任务检查点，错误不靠自动重发按键掩盖。

### Task 5: 无边框HUD与局部显示缓存

**Files:** Create `PrologueMinimapView.cs`、`scripts/presentation/minimap/minimap-mask.gdshader`；Modify `scripts/ui/PrologueFormalHud.cs`、`PrologueHudViewModel.cs`的layout、`WorldTargetMarkerVisual.cs`的HUD保护清单及`tools/build/prologue/BuildPrologueLevel.cs`的HUD构造；Test `tests/integration/MinimapHudTests.cs`。

**Interfaces:** layout新增`MinimapRect`；View提供`Present(ExplorationMapModel model, Vector2 actorWorld, Vector2 facing, double delta)`、`ClearPresentation()`，不接收save路径／写盘接口。样式参数集中：基准260×170、世界视野宽1280单位（高度按实际矩形比例）、边缘羽化12逻辑像素、已知空间低饱和暗色／已知边界浅色；这些表现默认值不是最终美术验收。

- [x] Step 1：写`AllNineViewportScalePairsAvoidHud`（1280×720／1920×1080／3840×2160 × .8／1／1.5）、`UnknownRemainsBlack`、`AuthorizedSegmentOnly`、`CacheWindowReturnIsAligned`、`KeyboardAndMouseIgnored`、`HiddenOutsidePlaying`；包含右侧资源区移位、Boss预警和指导卡片保护断言。
- [x] Step 2：运行`VerifyMinimap hud red`，取得缺地图矩形／显示的明确断言失败。
- [x] Step 3：按现有layout缩放口径计算MinimapRect，从PromptRect下方12间距开始，再避让右侧资源区和其余受保护区域；空间不足同比缩小地图。MouseFilter Ignore／FocusMode None，注册实际可见地图矩形为HUD保护区。只绘制玩家与已发现检查点；检查点实际位置对应的整细格已由合法当前／保存探索授权才可显示，旧路记忆自然保留标记。激活本身不突破遮挡，不从旧档的最后一个检查点推测全部历史，不从全目录生成黑暗内图标。
- [x] Step 4：实现≤256×192局部遮罩缓存；预留覆盖边缘，平滑跟随只改UV／坐标，探索dirty／revision／缓存越界才重建。未知遮罩最近邻采样，禁止羽化把内部黑暗透出；每texel对齐世界8单位授权格，或仅当整个world footprint均获授权才点亮，不能只验中心。增加非对齐缓存原点／薄墙／越界返回的黑暗像素负控；仅外围透明渐隐。地形线从获准边界向已知自由侧裁切，不能因一段已知显示全墙；旧区域离开视窗再回来从记忆恢复。运行`VerifyMinimap hud green`并保留原尺寸像素断言。
- [x] Step 5：保存布局与像素检查点，不把测试定位画面当正常路线／最终美术。

Task5独立复核C0/I0/M1 Approved（v002精确冻结；见执行台账）。Minor：shader越缓存边缘仅有源码检查，缺专门渲染负控；明确转入Task6 fixture-only补证和最终复核，非删除该像素要求。正式地形／正常路线／性能／媒体不由Task5关闭。

### Task 6: 同源地形生成与正式运行交付

**Files:** Create `tools/build/prologue/MinimapTerrainCatalogBuilder.cs`、`tests/integration/MinimapCatalogTests.cs`、`tests/integration/MinimapFormalTests.cs`、`tests/presentation/MinimapPerformanceProbe.cs`；Modify `tools/build/prologue/CanyonContentBuilder.cs`的Solid、`tools/build/prologue/BuildPrologueLevel.cs`的PopulateCanyonDefinitions后目录接入；生成`scenes/prologue/PrologueLevel.tscn`及受Solid元数据影响的5个sector场景；更新README／意见台账／进度与媒体索引。

**Interfaces:** `MinimapTerrainCatalogBuilder.Build(IReadOnlyList<PrologueSectorDefinition> definitions)`返回Catalog。Solid标记结构性body；提取未挂树的packed sector矩形shape、完整transform及definition.WorldOffset，按sector／nodepath／shape／face／segment排序生成稳定ID，边界每段≤8世界单位。revision用规范化几何与8单位格参数SHA256，不能用实例ID。现有门／机关／移动平台不因StaticBody类型被误收录。

- [ ] Step 1：写`PackedCatalogMatchesAuthoredShapes`、`DoorsMovingBodiesExcluded`、`DeterministicRevisionAndSegmentIds`、`GeometryChangeInvalidatesOnlyMap`、`SavedFormalSceneContainsMap`；在尚未生成地图目录的正式场景上运行`VerifyMinimap formal red`，断言缺正式接入口。
- [ ] Step 2：实现目录提取、容量失败保护和正式接入；构建按现有顺序运行BuildCanyonEntrance／Descent／Basin／Ascent／LostTemple再BuildPrologueLevel。均用`G:/godot/dotnet-sdk/dotnet.exe build`后的受守卫自有headless Godot脚本进程，每脚本55s；不手改tscn，不运行旧默认存档综合入口。打包后重新实例化核对目录／NodePath／采样世界坐标，不仅检查源码。
- [ ] Step 3：运行`VerifyMinimap formal green`及visibility／model／save／lifecycle／hud所有GREEN；既有保护提示GPU、门禁、残片冷读回和正式输入回归保持。实际按键验证Q/J/K/L/Esc；跨区卸载返回旧路、真实死亡／Continue以及Boss后有支撑出口分别留证。自动ParseInputEvent属于合成物理键事件，不称真人硬件验收；遇既有raw持键失败保留FAIL，先补测试侧事件时序，不重试到GREEN。
- [ ] Step 4：运行`VerifyMinimap performance green`：局部模型预热后闲置0分配、64候选≤4KB，当前机器p95≤.5ms／p99≤1ms。正常时间地图关／开／恢复各120预热＋600有效间隔，focus／overflow0；恢复基线p95与初始差≤10%，开启≤两基线较大者1.05倍才授予预算通过。记录全部样本、设备、焦点、测量开销和实际揭示延迟，失败／partial不授予FPS或整关流畅。
- [ ] Step 5：完成实际原速短片并审阅角色跟随、黑暗边界、HUD避让和旧路回返，先归档原件／展示副本／来源版本状态索引再给用户看。正常Main／Explore路线或原目标缺口未通过时保持ACTIVE，明确局部完成范围；更新README的已建／剩余／资源表，不把规划敌人／场景扩充算实装。任务限定diff交独立最终复核，不自动发布。

## 自检记录与执行交接

### Task 8: 当前萤火虫支撑搜索分配修正（追加）

**Files／Interfaces／exact contracts:** `.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-8-brief.md`。限定一个生产方法及私有helper、原live traversal测试、新局部测量runner和守卫launcher；保持既有公开接口和原共享Descendants。

- [x] 先取得当前正式保存场景四个实际方法各64次有界分配Baseline，未改生产；root据实际数值裁定是否进入单一候选。
- [x] 真实嵌套／宽fixture的public支撑查询分配具名RED；所有原live选择／次序／移除覆盖保持。
- [x] 只去支撑搜索递归iterator分配，原深度优先实时判断不变；GREEN及同边界优化后局部测量。
- [x] 原firefly完整focused suite、mounted guidance及保护区隔离回归，范围快照与限定Spec／quality复核；Task8自身无正常三臂预算重跑或FPS／整体完成结论。

Task8限定复核C0/I0/M0 Approved，root已解决跨任务⚠项；仅完成支撑搜索分配任务，完整目标与Task4/6整体门不关闭。根代理另行依据新生产版本的实测收益裁定一次原strict正常时间验证，见`post-task-8-normal-validation-brief-v001.md`；不得改变旧v003失败或以原版反复采集择优。

自检：Task7→8只提供分配相关线索，不继承assembly／因果或性能PASS；Task8与既有live fixture共同消费public TryResolveSupport且不改合同；其局部测量复用真实四方法边界但不启动旧四臂runner。具体交叉表及ruling见ledger。既有全目标未完成门不关闭，不把unsafe aggregate为通用TDD要求强行运行。

2026-10-12 Task7完成限定交接：新增诊断四文件Spec／quality独立C0/I0/M0；首正常仪器时序错误原样INVALID保留，真实containment RED→GREEN后唯一获准修后案2163 raw端点、root独立复算和最终库存均有效。只完成诊断工具与取证，不完成根因／性能／整体游戏；原严格FAIL／INVALID及Task4/6未接受门保持。实际多数Adapter层级遍历已为index循环，剩余shelf循环收益未知；下一步针对当前代码量化非地图更新分配，不用旧测量假称已找到新主因。证据／跨范围校验与全部ruling保留于本计划ledger，完整目标ACTIVE，不删除SDD或Git发布。

2026-10-12 Task7诊断续片：v003功能与同版正常双路线、原六邻项已取得运行证据；首次严格正常性能比较因恢复基线漂移而INVALID／FAIL，Task6性能门保持开放。为确定下一项可验证假说，追加仅测试侧的有界帧诊断，不修改生产玩法、画面、原计时器或原预算。四个新文件范围、单次正常三臂721端点、真实RED／GREEN及完整隔离守卫见`.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-7-brief.md`。已派发fresh实施者独占code/build/engine lease，root只维护文档与审查。诊断assembly与v003正式候选分开，完整原失败保留；采集完成不等于性能通过或原生历史问题已修。结果后只据证据选一个后续假说，不重复到绿色、不降低内容负载、不启动全局ETW或安装工具。完整目标仍ACTIVE，原美术／丰富度／连续视听和真人体验缺口不变。

2026-10-12 08:08执行顺序勘误（不改变规格或验收）：Task4初版审阅I1已用真实RED及最小结果helper修正，独立fix-only C0/I0/M0 Approved；13生命周期／save+cold／formalinput通过。但完整keyboard退出原生FATAL导致coldQ未启动，先前fragments首加载ERROR也未闭合，Task4验收保持未完成。允许先开展依赖已审API的Task5只读显示层；Task4冻结输入／六binary及全部FAIL保留，根代理继续独立诊断，最终集成仍必须原严格回归＋正常路线／媒体／性能通过。窄诊断clean不算修复，禁止过滤／GC猜测修复／反复跑到绿；若判断错，成本为HUD或跨版本复现返工，不把未证根因当引擎缺陷，不降低交付门槛。

2026-10-12主代理逐项自检：显示／输入→任务5；整格和前表面→1/2；生命周期与存档→3/4；同源结构→6；容量／局部与正常性能→2/6；退出失败→4；原始媒体保留→6。五条Review Focus均有对应具名测试；跨任务接口统一，检查点不冒充git提交。补齐loader的可选catalog参数、Catalog读取时机和原PersistProgress地图字段的提交边界，避免codec缺目录或扩大核心失败语义。已纠正“只按探索dirty更新”与局部跟随的矛盾，作为上文待审勘误保留，未在批准规格中静默改写。

2026-10-12独立计划复核：Critical0；三项Important（DTO顺序、静止开门重试、texel跨授权边界）已在对应任务修订，一项Minor（合法变换措辞）已澄清。复核不授予实现／视觉／性能验收。决定Native执行：六项紧密依赖同一目录、模型与流程接口，由当前代理连续实施、独立复核；不把非关键执行方式当缺少用户信息。现有性能／输入验收不因本计划而宣告完成。检查点不用commit代替；所有失败与范围快照留存。
