# Quest Fragments and Boss Admission Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 提供9件任务残片/最低6件Boss准入、可靠保存与真实补收集回返路线。

**Architecture:** 独立残片目录限定计数集合；沿用稳定ID进度和原子保存；共享Boss准入结果被门、触发器和HUD读取。最小回溯与区域映射是数量门槛的必要部分，不用虚假奖励解决卡关。

**Tech Stack:** 当前Godot .NET/C#与已有builder、隔离fixture、保存存储器，不新增依赖。

**Spec:** `docs/superpowers/specs/2026-10-07-keyboard-collection-headlamp-fog-design.md`。

## Global Constraints

- 全关9件，Boss门最低要求6件；主路可取得6件，探索支路额外3件；任务残片采用接触收集，不是Q消费道具。
- 死亡不掉落、重复不加数、入场不消费、Boss重试不重收；只有新游戏重置。
- schema3→4，`CollectionRuleVersion=1`，新游戏`LegacyCollectionQuotaExempt=false`；保留schema1/2受限迁移与布局选择，不能改原始存档试验。
- 既有庭院封印、钟链条件与数量AND；保留旧合法神殿/Boss准入与已击败/完成恢复，不伪造ID。
- 保留连续7680×4320五分区、检查点ID/位置、普通单跳、620爪射程、原角色物理和安全出口；场景C# builder生成。
- 先完成keyboard计划Task1–3；统一用其`verify-feature.ps1 -Feature fragments`与外部case路径，不直接运行默认user://测试。

## Review Focus

- 区域/补给的发现ID：不能增加任务残片计数（Task 1）。
- 旧神殿存档、新档伪装在神殿位置、损坏ID：只按合法旧版本进度迁移，不能泛化豁免（Task 1）。
- 保存失败/重复接触同帧：不吃掉物件，不重复计数或声音（Task 2）。
- 已开庭院门却不足6件/从上方抓入：仍不能触发Boss（Task 3）。
- 漏捡后已到Boss前：真实键盘回返至少一个缺失区域，再回门前通关，不重置/传送（Task 3）。

---

### Task 1: Catalog, count and explicit migration

**Files:** Create `scripts/prologue/collections/QuestFragmentCatalog.cs`, `QuestFragmentProgressModel.cs`; Modify `scripts/save/PrologueSaveData.cs`, `PrologueSaveStore.cs`, `scripts/prologue/PrologueProgress.cs`, `seamless/CanyonWorldLayout.cs`; Test `tests/unit/QuestFragmentProgressTests.cs`, `tests/integration/QuestFragmentSaveTests.cs`, `tests/RunQuestFragmentTests.cs`。

**Interfaces:** `QuestFragmentDefinition(string StableId, CanyonRouteNodeId Region, bool Optional)`；目录只暴露9个ID、`RequiredCount=6`；`QuestFragmentProgressModel.CountCollected(IEnumerable<string> ids) -> int`和`HasQuota(ids, bool legacyExempt) -> bool`。保存/进度均携带同名`CollectionRuleVersion`、`LegacyCollectionQuotaExempt`，Reset设1/false，Restore/快照/ToSave均保留。

目录ID固定为`canyon.v2.fragment.`前缀加`entrance/rift/abyss-floor/gear-gallery/ascent-mid/courtyard`（6主路）与`archive/steam-loop/temple-upper`（3支路）。对应区域依次为LowCeilingBridge/StaggeredRift/ControlledAbyss/GearPistons/CounterweightLift/SealedCourtyard/ArchiveSideCave/SteamValves/SealedCourtyard；Optional而非区域名称决定主支路口径。实际位置从各自合法支撑和正常路线校准，不能按本文的区域名假定可达。

- [x] 写断言：`CountCollected(fragment1,fragment1,areaId,supplyId)==1`；5不达标、6达标、9计9；未知ID仍被Normalize拒绝；新游戏无豁免。
- [x] 写隔离旧schema3神殿、盆地、Boss击败、schema2合法映射、缺失布局与损坏字段case：神殿合法激活/战后才豁免，盆地不豁免，当前schema4仅改坐标不豁免。运行`pwsh -File .superpowers/sdd/2026-10-07-keyboard-collection-fog/verify-feature.ps1 -Feature fragments -ExpectedOutcome red`。
- [x] 实现目录计数和显式迁移，先校验原wire字段/稳定ID，再Normalize；迁移不补钟链/封印，不写源文件；schema4仍严格检查规则版本与ID。保持旧数据失败恢复/用户选择边界。
- [x] 同命令GREEN要求`QUEST_FRAGMENTS_OK`；验证死亡快照、卸载继续、跨进程保存读取、新游戏重置及原文件hash不变。
- [ ] 独立审查后只提交本任务文件，不随提交改用户存档。

### Task 2: Transactional contact collection and HUD

**Files:** Create `scripts/prologue/collections/QuestFragment.cs`, `QuestFragmentVisual.cs`; Modify `PrologueGameFlowController.cs`, `PrologueLevelController.cs`, `scripts/ui/PrologueHudViewModel.cs`, `PrologueFormalHud.cs`, `assets/localization/prologue-ui.json`; Test `tests/integration/QuestFragmentCollectionTests.cs`, extend HUD/fragment runner。

**Interfaces:** `FragmentCollectionResult { Collected, AlreadyCollected, InvalidId, SaveFailed, Unavailable }`；`PrologueGameFlowController.TryCollectFragment(string stableId)`在候选保存成功后才提交权威进度/收集反馈；暂停/死亡/卸载等不可用状态与磁盘失败分开。`QuestFragment`为独立Area2D，仅接受玩家身体。HUD增加可选`FragmentCount`/`FragmentRequired`/`CollectionQuotaExempt`，不重排现有任务枚举。

- [x] 写`FullHealthStillCollects`、`DuplicateBodyCallbacksOnce`、`FailSaveKeepsFragmentRetryable`、`HudUsesRegistryCount`；保存故障fixture只针对case外部路径，断言失败后节点仍可收集，成功只一次反馈。运行fragments RED。
- [x] 实现接触收集与事务边界，不复用满血拒绝的SupplyCache；已有补给行为不改，记录持久化与事件来自同一结果。
- [x] 实现真实计数/缺额提示以及“旧进度已保留准入”，不把豁免显示为虚假6/6；视觉有可替换接口，暂用程序化残片，不调用付费生成。
- [x] 运行fragments GREEN与HUD/补给邻接回归，验证暂停、死亡同帧、卸载重挂、保存失败后重试。
- [ ] 实际四分辨率文字排版、世界拾取表现与持续动画视觉验收（模型/字符串通过不能代替）。
- [ ] 独立审查后只提交本任务文件。

### Task 3: Placement, physical admission and recoverable routes

**Files:** Create `scripts/prologue/collections/BossAdmissionModel.cs`, `tests/integration/QuestFragmentRouteTests.cs`; Modify `scripts/boss/BossArenaController.cs`, `tools/build/prologue/CanyonContentBuilder.cs`, `scripts/prologue/seamless/CanyonWorldLayout.cs`, `CanyonRouteGraph.cs`, `tests/unit/CanyonWorldLayoutTests.cs`, `tests/unit/PrologueCanyonStructureTests.cs`, `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`及对应生成sectors。

**Interfaces:** `BossAdmissionResult(bool Allowed, int MissingFragments, bool CourtyardReady, bool BellChainReady)`；`BossAdmissionModel.Evaluate(PrologueProgress progress)`是唯一准入判断，已击败Boss采用既有战后恢复分支。目录ID位置由builder绑定，各区域独立计数，缺失区域来自目录而非猜测。

- [x] 中央准入模型单独RED→GREEN：真实目录0/5/6/9、机关AND、旧豁免与caller战后分支，不代表实体门已接线。
- [x] 写6主路/3支路实际节点目录匹配断言，以及上层齿轮三块gallery合法玩家中心走廊区域映射。按原节点/局部走廊界定范围，不把虚空划成可走区。
- [ ] 写真实物理RED：5件且庭院/钟链已开仍挡门、不能从上方K绕入，6件正常进入；保存旧封印不能绕quota。至少一条Boss门前→遗漏残片区域→Boss门前全键盘补收集路线尚应失败并被保存。另验证当前schema无豁免、Boss门前0件时能从回返连通区取得至少6个不同残片；区域起点fixture与正常开局证明分开标注。
- [ ] 实装9个独立残片、最终门物理阻挡与开战二次判断；门前保留支撑。补必要回返抓面/通道及显式反向TraversalSegmentId；拓扑从“唯一双向捷径”改成明确允许的回返边目录，保留BossHall机关入口和SafeExit击败入口负例。不要删除绕门负例或改角色物理凑可达性。
- [ ] C#重建/实例化比较节点，再运行fragments GREEN及实际漏捡补齐Main/Explore/Boss出口；对每件必需物件检查完整胶囊可达/返回和存档恢复。
- [ ] 保留旧路线/旧冻结快照，另建本修改的来源版本并核对差异。录像展示前按项目保留规则归档，记录自动实跑和真人体验的不同证明范围。
- [ ] 更新README/目录资产状态，独立审查后只提交本任务文件。新地形大规模丰富化另按内容规格，不因本任务顺便扩大未批准范围。

计划状态（2026-10-10 14:00）：Task1/2功能、中央准入模型与九处保存摆放/静态胶囊支撑专项GREEN，见`.superpowers/sdd/2026-10-07-keyboard-collection-fog/quest-fragments-task3-progress.md`；正常键盘Main到庭院自然拾取6主路，但剩余工匠未击败、角色在高台侧边，整关失败（没有玩家死亡证据）。Task2视觉、实体门/开战二次检查、实际补收集回返与三支路整关尚未完成；不以静态支撑代替物理路线。脏工作树未全整理，提交checkbox保留，不进行全量暂存/发布。

计划状态增量（2026-10-11 05:32）：实体三面有限准入罩/同帧六件/旧记录/复活/战后恢复专项及fresh Main/Explore已获原生自动化GREEN；独立真实标题漏捡gear→五件被挡→W+K/Space补temple-upper→六件同门→Boss/有支撑出口/保持D60亦GREEN，case `route-result-natural-five-six-20261011T052731120-ca93aab8040e4f6590368e3dd8b88b62`，不是fixture/人工验收。仍未完成0/4件等广泛补收集、显式反向图边/每件全部走法、综合加载句柄FAIL及正式雾/视觉/性能/归档录像，因此Task3上述组合checkbox和提交项继续保留；不按单个scenario降低整体验收。详细source/proof/scope见阶段记录。

计划状态增量（2026-10-11；盆地原生回返）：先取得真实向上W+K穿区时FALL RESET、HP5/恢复1的失败工件；仅在builder补有限BasinReturnSeam并重生Descent场景后，`route-result-basin-return-20261011T101554045-8c1420ed68c347bdb6b147ed0babc858`完成真实营地→原Left4西抓面→完整底台拾取→营地，当前schema地域fixture0→1、HP5/恢复0、0普通跳/冲刺。旧Temple七段、fresh Main `101812644-39a5304542de4a95ab70d48438dcf11f`及Explore `102149024-e6a110d25b7a4bb58f1637290ccb27ef`也同版通过；没有扩大原时限或改变角色/爪数值。下一步先连接同一Player的Temple→Split→17阶捷径→Camp→Abyss，再闭合其余合法残片与回门；0/4→6的组合checkbox仍不勾。现有单段TraversalSegment只支持单跳/Jump|Thread及源区局部assist，不能如实表示已证明的多支撑walk/drop和跨区抓面；须明确扩展路线合同，而非造假源区目标、强迫无用跳跃或把region fixture当自然全程。综合句柄FAIL、正式迷雾/实时感官/归档录像仍开放，目标ACTIVE。

计划状态增量（2026-10-11 10:58；连续链尚未通过）：同Player Temple→Split→17阶→Walkway已实走，五个保留失败案暴露独立SteamJet、低台近边起跳和GearEntry回返缝问题；喷口自然等待与低台单跳已在后案通过，最远ShortcutCampReturnStep/HP5。GearEntry最后单跳同帧两侧碰撞并受Left90伤害，40px缝等于角色宽度，伤害源ID待核实；未到Camp/未0→2，不认证0→6。最新原独立Basin和Temple在同715源码版本GREEN，不与较早714源码的完整Main/Explore混称同版。witness89/预算30纯内存GREEN；来源/用户数据/冻结保护稳定。本增量只改测试和记录，生产地图未改。下一窄片按阶段记录ruling采集实际伤害身份、检查最小净空修订与明确防御走法，保留原角色物理/时限/危险条件；Task3组合checkbox与完整goal继续未完成。

计划状态增量（2026-10-11 11:40；连续链与同版完整回归GREEN）：已用实际SourceRuntimeId确认最终伤害来自原ShieldLane，先净空RED再由C# builder仅把PistonOne左沿2420→2440/宽300→280（右/顶/底不变），60px缝保留原40px角色及危险条件。连续地域链`route-result-return-brake-20261011T112959054-f8ac5bab15df4959b68367f3f7386969`真实同Player走完Temple七段→17阶→Camp→Abyss→Camp，31完整支撑、HP5/恢复0、三次普通跳、原lane9次右向零伤害防御加其他真实敌人guard1，实际隔离存档仅courtyard+abyss-floor、0→2，Temple检查点保留。fresh Main `113233792-2570aa1885c54a5694a01aede8cadef3`、Explore `113553553-480b3f02fc964bf19ddababc496799af`build/engine/wrapper0、从真实标题完成Boss/支撑出口各1、D60保存hash保持；715source/42scenes/109protected/12frozen和六binary三案同版。Explore含双向捷径、支路6→7及补给返回，非补六fixture。97个纯内存witness再次GREEN；旧失败完整保留。Task3的更广0/4→6、显式多段跨区合同、综合句柄FAIL、正式雾/实时视听性能/归档录像/最终整分支审查仍开放，组合checkbox不勾，全目标ACTIVE。

计划状态增量（2026-10-11 11:56；合同表达能力）：图校验经RED→GREEN允许精确Abyss补捡目录候选，同时保留唯一实际持久shortcut及BossHall/SafeExit负例；AuthoredEdges未变，不宣称新边已接线。GroundTransfer经能力/实际builder/旧packed三次RED→GREEN，保持旧enum0/1/2，只把已实走的Abyss→Camp前向合同改为无特殊能力的walk/drop。Descent仅该元数据语义行变化，无几何/碰撞变动，节点271保存再实例化一致；合同专项 `returncontracts-20261011-115549-496-f36fd88f04f64352a6b7081c50cdca61`GREEN。新的718源码版本物理回归待执行；跨区实际命中点/620px和多段合同、完整0/4→6、旧aggregate句柄FAIL及正式雾/视听等未完成，不勾Task3组合项。

计划状态增量（2026-10-11 12:12）：上述718source/42scenes版本的chain `ground-chain-20261011T115928975-a66ba4e2360c44389153b8fd5adb2c00`、Main `ground-main-20261011T120215924-0648d2add885493696d228133befaf9b`、Explore `ground-explore-20261011T120651216-2102ec3278e74b92b037df1b6780e62a`三案均终端build/engine/wrapper0，stderr空、四清单前后及跨案相同、六binary相同、用户109与冻结12保持。chain真实同Player31支撑/HP5/恢复0、fixture0→2；两完整路线真实标题同角色→Boss→有支撑出口→D60一次结算，Explore原双向shortcut、支路6→7/HP1→3均保持。自动输入不等于human或实时视听验收，更广0/4→6及跨区多段合同、综合句柄FAIL、正式雾/正常速度性能与归档录像仍开放，组合checkbox不勾、目标ACTIVE。下一窄片先如实表达原Camp→Abyss Thread-only跨区命中点和自然重力落地，不改620px/角色物理或造源区替身。

计划状态增量（2026-10-11 12:39）：Thread-only ClawDrop合同经能力/builder/packed RED→GREEN、原生W/K实际命中与落台验证后，已只发布精确Basin→Abyss反向边。静态全18方向生成合同专项`returncontracts-20261011-122724-716-c592b41161fa441e8fbc49bff9d0c058`GREEN；最终同版published-chain122922/Main123121/Explore123446均build/engine/wrapper0、四清单及六binary跨案一致/前后保持。chain仍0→2地域fixture、31支撑/HP5/恢复0；Main/Explore标题同角色→Boss→有支撑出口→D60一次结算/保存保持，Explore6→7/HP1→3及支路双向保持。完整case/哈希/实际事件/scope见Task3阶段记录12:39。下一窄片同Player地域fixture连续取得六个真实残片，不能用分开的正向路线证明替代；自然0/4→6/回同门、Temple多段合同、aggregate句柄FAIL、正式雾/正常速度视听性能/归档录像仍未完成，组合checkbox不勾、完整goal ACTIVE。

计划状态增量（2026-10-11 13:37）：同Player地域fixture已实际连续0→6回原Temple地面，case `route-result-six-grip9-20261011T132237772-3fa5f067f8ac46ce91dbde7fbf495275` build/engine/wrapper0；旧31段及真实前两件保持，后四件均原native接触/一次反馈/即时实际写盘，HP5/恢复0/非legacy。仅修正test-only首次战斗和Grip9站位衔接，不重置敌人/ID或改物理/预算。当前719源码/42场景的fresh Main `six-main-20261011T132710970-f8f5590b0b6a47d8b261c27cc742ad48` 与Explore `six-explore-20261011T133040641-546e048433404087aeddae4a476851ae`也真实标题同角色→Boss→完整支撑出口→D60一次结算，三案四清单及六binary同版/稳定。全部实际退出、ID/hash/范围在Task3阶段记录13:37；零件地域fixture不代替自然更广0/4缺额漏捡/Temple合同、aggregate句柄FAIL或正式雾/视听性能/归档录像。Task3组合checkbox仍不勾、完整goal ACTIVE，无新媒体/付费/自动化/提交或发布。
