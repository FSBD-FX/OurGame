# 区域常驻敌群：原生单身体几何检查 · v001

2026-10-16 / Asia/Shanghai。这是实施中间证据，不是新增50已放置、地图完成或试玩验收。完整分配仍为原26＋新增50＝76普通、另1Boss；骨骸10和幽灵10必做，见[完整规划](2026-10-16-prologue-population-final-plan-v001.md)。

## 事实、判断、风险

- 事实：不可变50身份/24子型名册已实现；本轮新增单身体几何检查器及原生夹具。实际源码库存1652、真实保护库存109，canonical owner仍执行精确库存/源码/helper/二进制/副本守护。
- 判断：先建立真实地形检查能力，再填写50坐标；不能把可用原始地形数量或只测角色中心当成50个合法出生位置。
- 风险：单次静止身体无重叠不代表巡逻全程、冲锋/俯冲、射界、未来危险相位、玩家落点及跨区恢复安全。结果的`IsCompletePlacementProof`始终false，不能供manager直接当成全认证。

## 本轮实际实现与夹具边界

生产检查器：`scripts/prologue/residents/ResidentPlacementGeometry.cs`；原生夹具：`tests/integration/CanyonResidentPlacementTests.cs`；名册：`tools/build/prologue/CanyonResidentCatalog.cs`。玩家、旧敌AI/数值、保存场景、存档及素材未改。

精确五个保存sector挂载到自有独立World2D。挂载前只复制实例内形状，避免BellChain Ready把缓存门高180改240；挂载后接受并拥有实际Ready生成的Boss，停止节点回调后等待延后初始化，再核对碰撞快照稳定。原生确认127原始地形体、实际1Boss/0普通、无全树Player、缓存形状未变、真实地板/摆锤已登记。没有使用禁ProcessMode移除碰撞来伪造空地。

九种旧敌探针读取保存身体形状。只读源码核对其Ready/EnterTree/deferred/configuration目前不改该身体，但探针没有进入运行世界，不证明运动/出生/AI或投射物安全。几何夹具没有玩家，故也没有验证七个KillZone的六层ILevelRecovery祖先与死亡恢复。

28项包括：九种真实身体包围盒；保存地板完整支撑；两边有支撑但中央8px空隙；同一StaticBody的另一墙形不能被支撑排除；实体墙、真实攻击Area、65命中超过64容量严格拒绝；实际零layer电磁区；完整身体而非中心避让营地；奇异/TopLevel/非Canvas祖先/禁用/不支持/多身体形状拒绝；祖先平移＋旋转＋缩放实际变换；当前安全蒸汽/活塞空间通过但全相位/全行程预留拒绝。控制墙/空隙/高命中区在保存地图前提通过后独立添加，不冒充保存地图原有机关。

原生测试发现：保存电磁区`CollisionLayer=0, mask=2`，即使全layer查询也看不到，但真实Shape.Collide确实重叠。已增加对当前活跃实际电磁类型的准确形状检查，没有改游戏碰撞层。另临时去掉支撑Shape身份条件，原生只在`wall_on_same_support_body`失败；恢复原条件后28项全过，防止仅按body RID误排除墙。

所有资源处理遵循所有权：只释放夹具新建/复制shape、query/result/字典/数组backing及自有world；缓存PackedScene/源shape/借用Space不释放。未加GC、KeepAlive、缓存模式、诊断屏蔽或新drain规避。

## 新鲜原生证据

根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw/`。每案保留三raw、build/engine/provenance、前后源码/保护/helper/二进制库存及副本。失败仍是真失败，不作命名豁免。

| 案例目录 | 原生结果 | 含义 |
|---|---|---|
| resident-geometry-20261016-064314-637-8a21a43f54324bfcbc1a9db964823241 | exit1/1071ms，guards0 | 初始stub 27失败，另有两条reparent Owner警告；不是干净RED。修正夹具Owner后再测。 |
| resident-geometry-20261016-064442-192-19b28f03ebcc4b318cf15a5a27719948 | exit1/957ms，guards0 | 干净行为RED：原生前提通过，stub的27检查失败；非编译失败。 |
| resident-geometry-20261016-064737-011-ad07a79ebc734a6693ebdd10a0c064aa | exit1/1063ms，guards0 | 实现后仅实际零layer电磁区漏检；保留原因与原始证据。 |
| resident-geometry-20261016-065116-293-6a353aee8c3c4dec806d99c5ad031818 | exit0/1049ms，guards0 | 27通过，三raw无ERROR/WARNING/FATAL。 |
| resident-geometry-20261016-070353-108-964583780b5948f2b554448a46adc72e | exit1/1057ms，guards0 | 故意删除支撑shape身份后，仅同body另一墙反例失败。 |
| resident-geometry-20261016-070716-255-dc87a73f6bf1447aa19ad21cb76e9b0e | exit0/1049ms，guards0 | 恢复精确源码，最终28通过，三raw无诊断；fresh Rebuild 0警告/0错误。 |
| resident-allocation-20261016-070901-446-715f19b16ff64568987024f17cabc35f | exit0/394ms，guards0 | 最终源码名册17同检查错误输入通过；仍明确sites50/scenes50/spawned0未准备。scenes50是身份槽，不是50独立文件。 |
| prologue-pre-20261016-065545-542-5758a414d9064b82ba19c66368e2ba4c | exit1/88276ms，guards0 | 两条刻意无效PresentationProfile警告＋旧UpperPendulum/lower_to_mid_swing落错台，仍严格失败。 |
| prologue-post-20261016-065834-946-433932d913fb491ea8563cefff82adb8 | exit1/8009ms，guards0 | V2结构/材质/相机及盾132/150过后，旧BellChain夹具地板落稳和KillZone ILevelRecovery路径失败，未豁免。 |

最终几何源码SHA256：`9463D5FF333DFC80517EE2851696ADED8FF31ABDEE5475AB6FCC7B6DD7BCD1D5`，与形状身份反例前备份一致。完整pre/post运行时该生产文件相同；之后只新增第28项反例并恢复相同源码，没有声称旧失败已修复。

## 未完成与下一步

只读环境审计：五区38个Area，22个零layer，其中7恢复区＋1电磁是危险/恢复源；5股潜在蒸汽初始安全相位不能漏。4个运动实体为活塞、摆锤和两升降台；条件门/屏障另计。9残片、2补给、2检查点Area、Boss激活、11抓爪/控制目标不是危险，但有独立通行/保护需求；起点是第三恢复位置而非第三检查点Area。

下一步须形成全环境占用/保留工厂：7恢复、1电磁、5蒸汽全相位、4运动完整包络、条件门、3恢复位置/营地安全壳、残片/补给/Boss入口、跳跃/抓爪落点和旧敌活动需求。再逐点编写/原生验证全部50出生及完整巡逻/追击/返回路线，不足则通过builder扩展围合侧龛。

全部50实际位置当前仍null，24正式家族子型scene尚未实现。Task1未完成，Task2真实行为/Task3跨区名册/Task4全路性能媒体未开始。本轮无新增怪物/正式美术/可玩版本或录像，没有新Main/Explore、人测或稳定60FPS证据；全目标ACTIVE，所有历史资源/FATAL/旧档/十区/美术/性能/真人门继续保留。没有付费、Git写入、上传、自动化、安装/升级或清理删除。
