# Annex Exploration Compatibility v252 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL superpowers:executing-plans。Root执行主要实现与全部build/native；独立worker只修已实际RED的fixture资源/数量口径。所有CS/host统一冻结后测试。一次fresh增量终审，C/I一次TDD修补、Minor延期，无二审/Git/删除。

**Goal:** 保留扩图前已探索的旧地形，保持新附室黑暗，恢复真实当前与历史存档验证，并修复当前连续测试资源所有权。

**Architecture:** 精确144→158目标认证与固定authoring delta，不做任意revision近似迁移。独立摘要固定的144历史值夹具与当前158目录分离；历史迁移测试不再从live目录逆推错误版本。当前实际场景五字段authoring检查保留全部现有结构/baffle，不伪称其catalog还是历史143。

**Tech Stack:** G:/knight/Knight Godot4.7.1/.NET9/C#，起始1894受检输入；新增两CS后以真实库存为准。

**Spec:** ../specs/2026-11-05-annex-exploration-compatibility-v252-design.md

## Global Constraints

- 仅headless/隔离storage，保schema4/layout2、原55/180/300/600s预算、HP/能量/899/单跳/射程/角度规则。
- 原十一项/76敌人/正式美术连续动作/R-I/R-C/R-P/真人视听范围不变，局部绿不完成目标。
- dirty原地，保所有失败与原件，无worktree/commit/清理、GUI/OS输入/付费/新媒体/额外dump/GC/cache/KeepAlive/引擎/自动化/外部发布/真实存档操作。
- 只认证spec给定144→158 delta的SHA/count/grid，既有B264/Shelf/Post沿原已支持链先到144；未知版本拒绝可选map，不制造核心进度。

## Review Focus

- 正确标签但不同目录几何/法线/grid/数量，不得迁移；Task1伪目标负控。
- 全满旧bitset不得点亮新房/新1440边；Task1全部cell/edge检查及空间接触反例。
- 冷读不得依赖暖进程或覆盖源；Task1旧源与重存SHA、独立PID/冷进程。
- 历史143/144不应误用新158，当前authoring也不能把158重标143；Task2独立fixture摘要及保现有结构的完整stage/reload。
- 连续fixture共享场景不得被借用者Dispose，数目检查不能把Area当structural或把body当shape；Task3原aggregate/原机关/原geometry实测，旧匿名故障不由此关闭。

---

### Task1: 精确当前探索迁移（root）

**Files:** 新增tests/unit/AnnexExplorationMapMigrationTests.cs、tests/fixtures/HistoricalMinimapCatalogFixture.cs；修改scripts/save/ExplorationMapSaveCodec.cs、tests/unit/ExplorationMapSaveTests.cs、tests/integration/ExplorationMapColdReadTests.cs、现有验证host的save收据/输入清单。

**Interfaces:** Consumes `CurrentRevisionCatalog()`、真实`PrologueSaveStore.LoadOrNew(path,2,catalog)`、`TryRead(JsonElement,catalog,out data,out diagnostic)`、spec固定摘要/两段映射。Produces `AnnexExplorationMapMigrationTests.Run(string root)`和`ColdRead(string root)`，以及`HistoricalMinimapCatalogFixture.Baffle()`返回独立owned值Resource，供Task2使用。运行时不读测试fixture或旧workspace。

- [ ] 先新增真实加载144稀疏旧wire→实际158目标测试，保持源SHA及所有核心字段；嵌入现有resident-save-compatibility暖/冷入口，不新增放宽phase。
- [ ] 运行原host `-Phase resident-save-compatibility -SourceRows <实际>`，Expected: native非timeout `ANNEX_MAP_MIGRATION_RED`，生产map=null。Root唯一native，worker先READY。
- [ ] 在Codec已知目标拒绝前增加独立Annex分支，认证158/13908/518400/960×540及完整目标SHA；Baffle144/12468直接进入，既有B264/Shelf/Post各校验原格式后复用原delta先到144，不能回退历史支持。新edges从零，旧0..6663原ID，6664..12467+1440；cells仅清除新room及新结构触及范围，含8px边缘接触，不新增1。
- [ ] 扩展实际测试：零/满/稀疏，全部新1440边暗，全部cell独立空间oracle；错源/格式/缺短长/规范Base64/padding/超限、伪目标几何/法线/count/grid、坏核心均拒绝，原core/source只读，重存后独立PID冷读。
- [ ] 新旧目录固体/边映射使用完整40-byte solid + mapped-owner/24-byte边唯一性作测试证据；期望不由生产mapper返回值构造。
- [ ] 三个实际历史wire经完整链到158，各加独立literal位图和重存冷读；只有144→158绿不能关闭本项。Task2当前factory所有权审计表若因新增具名形状失配，保全部path/type/size/native-ID/borrowed规则逐项同步，不绕过审计。
- [ ] 在Task2修好原历史夹具后复跑原save完整暖/冷，Expected:原全部receipt+新增ANNEX_MAP_MIGRATION_OK/ANNEX_MAP_COLD_OK唯一、exit0、无generic ERROR/WARNING/FATAL/泄漏、五保护清单无改。

### Task2: 历史夹具与实际authoring解耦（root）

**Files:** tests/unit/FirstAscentMapMigrationTests.cs及Task1 fixture；不改实际保存场景。

**Interfaces:** Consumes `HistoricalMinimapCatalogFixture.Baffle()`固定旧354960-byte/SHA数据；Produces原Grip8/Historical/Post正负控及独立冷读保持原期望，实际stage仍检查真实现有场景。

- [ ] 保留已实际RED的旧144 current-target失败；独立导出保全旧Main嵌入catalog，gzip压缩值写入测试CS，加载解压后核完整SHA，非运行时文件依赖、不拷贝全旧场景外部引用。
- [ ] Grip8/Historical目标改从该旧144值夹具逆推，CandidateCatalog明确为历史Post143，不从实际158叶重建后重标。所有旧143/144 literal位图/负控/28控制不放宽。
- [ ] StageArtifacts仍从当前真实叶/parent作五字段stage与reload/link比对，但不删除当前Ascent baffle；其实际当前158 catalog必须匹配spec新SHA，不把它称历史Post143。两输出仅在隔离storage，无canonical写。
- [ ] 运行Task1原save暖冷，Expected:原历史所有正反控与当前迁移同时通过；若原控制报错，按实际证据修fixture，不能删case/receipt。

### Task3: 连续资源与新增结构数（独立worker，root验证）

**Files:** worker仅tests/integration/CanyonIncineratorLayoutTests.cs、CanyonMechanismRuntimeTests.cs、CanyonResidentPlacementTests.cs；root独占host收据。不得改生产资源生命周期、排序、缓存、GC、KeepAlive或shape销毁策略。

**Interfaces:** Consumes上一轮真实aggregate disposed RED、recycling143→158和geometry raw143→157 RED；Produces保持原物理/伤害/33/208/20控制的测试修正，root同源复跑。

- [ ] 核对borrowed GD.Load场景被helper using释放与后续World Definitions仍引用同一对象的调用链；只撤销不属于helper的显式Dispose，实例仍按原finally Free。保留连续RunBypass→RunDeath，不拆进程筛绿。
- [ ] 只同步实际新增14结构后的count：完整shape158、rawbody157分别处理；保持危险Area非结构断言，不能扩大legal sites/删208/20/native前提。
- [ ] root依次跑原 `thruster-incinerator-traversal`、`recycling-traps`、`resident-geometry`，Expected:前两严格PASS，geometry必须保真实终态（若旧FATAL/跨态问题再现继续FAIL，不重跑碰运气）。不得由count通过宣称资源根因修好。

### Task4: 当前整体证据与唯一审查

- [ ] 一次fresh精确增量终审，最强可用模型/高推理；按spec与本plan及全部Ruling。C/I仅一次具名RED→GREEN修补，Minor/全部declined逐项记录，无二审。
- [ ] 当前同源完整save暖冷、结构/keyboard/models、机制/连续aggregate、普通pre/post与Main/Explore保原契约运行，记录所有失败，不拿历史绿复用当前资格。
- [ ] 前后五组保护清单及当前完整源集合/binary/helper重核；关键媒体按正常汇报一次Check，不建自动化。更新README/原稳定backlog/用户反馈/production report，原目标ACTIVE、不清理workspace。

自审：任务1提供Task2独立旧值资源，Task2为Task1原历史入口解除旧current假设；两者一起根本修复save，而非只让旧测试过。Task3独立文件可并行准备，所有CS在native前统一冻结。实际当前authoring目标158与历史迁移目标143分开记录。用户持续直接推进偏好覆盖重复设计确认/worktree/commit/删除步骤，选择标明为开发裁决。
