# 常驻敌群：玩家交互保护与档案岩台改造 · v001

2026-10-16 / Asia/Shanghai。Task1中间实施证据，不是新增群或整关验收。权威checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`；HEAD/branch仍为`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6` / `codex/task6-certified-final-20260902T0555`。此前完整分配v001/v002及归档不覆盖。

## 事实、推理、风险

- 事实：实际交互保留新增19处，已知环境总数100；57项新检查及原51环境/28身体/29走廊通过。仅档案岩台持久扩宽，五区父小地图重新生成；实际键盘无窗口通到冰封支路，HP5、零恢复/位置写入。最新联合搜索41候选/9未解，实际新增actor仍0。
- 推理：旧builder的部分交互提示矩形位置已过时，不能拿它们认证玩家接近空间；单身体、保守走廊及搜索结果也不能替代真实巡逻、攻击、回返、J/K应对与完整玩家路线。最终50点应在真实行为/碰撞尺寸确定后回验。
- 风险：有界搜索不是容量上限；新增几何可能改变落台和旧敌视线，必须实际回归。正常Explore本轮失焦，用户确认同时使用其他窗口，不能把中止当游戏输入缺陷或地图通过证据。完整旧回归、正常60FPS、资源/旧档、美术/真人仍未验收。

## 一、实际玩家交互保留

生产变更在`ResidentEnvironmentInventory.cs`，增加`PlayerInteraction`保留类别；读取实际目标形状及完整祖先变换、实际门配置与手动阀世界交互半径，不依赖Build程序集或把所有远端目标视为Q设施。

| 来源 | 数量 | 保留依据 |
|---|---:|---|
| Mechanism ThreadTarget | 11 | 真实TargetCollision/物理形状，包括Ready生成Boss的潜在ChainTarget，即使当前Disabled |
| 两处压力门 | 6 | 实际Plate/Interior/AntiCrush世界矩形，不再重复应用祖先变换 |
| 捷径绞盘 | 1 | 实际声明的局部通道转世界，明确它不是原生碰撞体 |
| 手动蒸汽阀Q | 1 | 实际目标中心的世界80px半径，不随形状父级Scale膨胀 |
| **新增合计** | **19** | 原81仍独立验证，总100 |

57项包括字面世界bounds、整个身体占用拒绝、完整祖先旋转/缩放、缺Area/缺形状/未知形状/错误receiver/门配置/手动阀配对喷口等失败关闭与恢复。远端无喷口CourtyardSealLinkage仍合法，不因不存在Q而拒绝。Boss ChainTarget世界矩形(6758,1006,64,64)由保存BossSpawn(1600,360)、LostTemple偏移(5120,720)、目标偏移(70,-42)及R32独立推得；初审漏计它造成两条计数断言失败，已保留该反例，不排除Boss凑18。

这些只保护放置几何，不是已证明所有玩家视线/接近/远程攻击不会跨保护区；`IsCompletePlacementProof=false`保持。

## 二、分散候选，而非缩减活动范围

测试工具保留有界模板描述，按4×4区域格确定性轮转，再对每个候选调用原生完整走廊检查。18条实际native-clear路径构成独立控制：原首16条集中重叠只能容1，分散短表能容3；异高度、反序输入确定性、保留所有描述及空输入亦覆盖，先行为RED再GREEN。

128px游荡、192px冲锋中心全带、64px重装、128×96飞行闭环、64px栖停＋192px俯冲/返航等模板未缩小；全部50身份参与。限制仍为候选16、桶检查阈值96、全局3000、区域求解20000、描述8192。当前桶成对采样在阈值边界可能多一次，未宣称精确最大调用上限已证明；不得因此把它当容量证明。新顺序并非所有区单调改善，不能合并不同搜索案的互相冲突集合。

## 三、持久地图改造及真实玩家局部回归

只改`CanyonContentBuilder`中ArchiveShelf：世界(1560,1420,160,40)→**(1480,1420,240,40)**，右边1720和顶部1420不变；保留左内墙1328到新台左沿的152px间隔。没有手改生成tscn，也未添加Frozen候选台。

原生五项检查先失败于原Shelf、192px中心带和父小地图新台，原左侧玩家通道/原目录一致性通过；builder扩宽后五项通过。实际Skitter探针完整192px水平带双向无阻、原100环境/36旧主备出生保留仍参与。原生净空并不等于实际新敌AI已执行。

已有Descent→Parent构建owner新增：fresh Rebuild；父构建严格消费已接受Descent case的manifest/副本/当前SHA；10独立正负控制先RED后GREEN；完整project UID守护；三engine cache文件只测量/复制、不删除/重置；五区/schema/minimap输入冻结。两阶段仍非原子事务，失败保留不自动回滚。两个实际生成案均engine0/guards0、UID不变、cacheChanged=false、用户保护109不变。

父目录现127 solids/11160 boundaries，revision：`C3049F33BBC860BF474EA32E89B5BB408E2979DC2B70AD8B40FC27B15447DD84`。原尺寸/来源备份位于`.superpowers/sdd/2026-10-16-resident-population/before-archive-space-20261016`及各构建案before目录。

旧局部键盘助手试图走到x1520离台，改造后该点仍在台上，严格失败保留。只把这个测试的目的地改为x1440，原80帧预算、真实输入/地板接触不变；没有传送玩家或放宽断言。最终真实标题启动→左墙/顶盖/跳坑/抓顶/跨口回返→ArchiveEntry→扩宽Shelf→ArchiveFloor→FrozenLeftCatch通过；Shelf位置(1622.2135,1383.964)，Floor(1426.084,1603.9779)，Frozen(1348.0751,2143.9526)，HP5、positionwrites=0、recovery=0。这是headless自动键盘局部物理证据，不是真人、完整新版Explore或渲染验收。

## 四、完整目标50的当前联合候选，不是实装人数

| 区域 | 要求新增 | 当前候选 | 未解 |
|---|---:|---:|---:|
| 外峡入口 | 3 | 3 | 0 |
| 错层裂谷 | 5 | 5 | 0 |
| 深渊 | 5 | 4 | 1 |
| 档案洞 | 5 | 3 | 2 |
| 冰封绕路 | 6 | 4 | 2 |
| 齿轮区 | 7 | 5 | 2 |
| 蒸汽侧线 | 6 | 5 | 1 |
| 配重井 | 5 | 5 | 0 |
| 钟摆廊 | 4 | 3 | 1 |
| 错层横廊 | 3 | 3 | 0 |
| 庭院 | 1 | 1 | 0 |
| **合计** | **50** | **41** | **9** |

1553走廊调用/20228联合搜索节点、search_capped=true。未解稳定ID后缀：controlled-abyss.01；archive-side-cave.03/.04；frozen-detour.01/.02；gear-pistons.06/.07；steam-valves.02；pendulum-gallery.04。原始stdout保存所有路点/bounds/类型或失败原因。50正式Placement仍null，24正式族系场景未实现；实际可玩仍原26普通＋1Boss，不能把探针叫成骨骸/幽灵已实现。

冰封侧台的只读方案尚未实施。静态中间空隙不保证实际下落能穿过；残余横向速度、旧Moth、蒸汽/底部出口和原路回返都须真实测试，不将两个静态候选直接追加成43已认证。

## 五、失焦、录像与全部回归边界

普通时间1280×720 Explore在physics290 application_focus_out后raw D清空，291撤销所有权并严格失败，未到新岩台/Boss/出口。事件式OS观察记录到前台转移；用户随后明确答“是，正在使用其他窗口”。二者吻合正常失焦清键边界，不据此证明生产输入缺陷、是谁/何种程序造成全部过往失败或已修复输入。保留失败，不补键、不抢前台、不立即重跑；不操作其他应用。

更早fixed60的UI录像独立失败于RiftMiddleCatch，不能将后来用户回答倒灌为它的因果证据。1513帧/25.217秒片段实际录制33.557秒，非实时60FPS；抽帧18/20/22/24秒看见角色停在上层右沿、HP5，敌人继续移动，未到新岩台。原因未证实。失败原案/AVI/原速MP4/4viewport/4采样在展示前已保存于`E:/OurGame/Knight/过程图稿与视频/2026-10-16-档案岩台改造-录像失败诊断-v001`，323项102493480字节逐项大小/SHA核对；原始归档仅最终汇总字段求和报错，媒体/索引已核验，不重跑覆盖，另存完成核验及Root视觉审阅。连续音频听感未验收，画面仍NEEDS_WORK。

最终fresh完整pre仍严格FAIL：旧`UpperPendulum/lower_to_mid_swing sample=.5`落到下层，149/150、目标区crossings0；两条刻意无效PresentationProfile警告。完整post仍严格FAIL：旧`AssertBellChainGateBehavior`落地(540.0704,1196.0139)失败，KillZone无法解析六层祖先`ILevelRecovery`。本次无Handle未初始化/FATAL，不是历史问题修复证明。原有runner清理不变，没有GC/cache/KeepAlive/blanket Dispose/延时或诊断豁免。

新版结构33同检查负控、名册17负控和动作专项通过。所有新case源码1653（runtime1357＋imported296）、保护109、helper/二进制/副本守护0；失败原案照常保留。新版本正常Main/Explore、呈现60FPS、旧档/资源、最终美术、全十区/新增全50、真人及成功证明录像继续OPEN，目标ACTIVE。

## 六、执行顺序修正，不改变最终范围

源码事实：Task1几何可用ProbeScene运行，但最终认证需要Task2的真实族系身体/攻击/行为包络；`ValidateSceneReadiness`目前是失败关闭的未完成报告，连Implemented分支仍返回`scene.native_behavior_proof_required`，并没有可当完成证书的成功通道。若把全部最终点位认证设为Task2开工条件，就会产生流程循环。

因此Task1保留未完成，先进入Task2 opt-in真实行为开发，再回验全部50点，之后Task3才允许正式mount。先交付Rust实际无玩家地面巡逻/崖边转向/区外返航/真实感知/拉拽受击保留与默认wave回归；之后Skitter锁定冲锋、Moth飞行/俯冲/遮挡、重装/炮台及骨骸幽灵差异。新模型单测不能替代真实actor行为，更不能标全部Task2完成。睡眠必须撤销hurtbox/hitbox/projectile权限而不回血，默认未配置行为保持。

代价：后续若真实身体/攻击尺寸不同，41候选也必须重查，可能需要更多围合空间；这比探针先写成正式50点后被新行为推翻更可靠。全部50/五族/六职能/NPC、玩家技能/旧存档/正常通关/性能/视觉要求均不减少，不要求用户重复批准已确定范围。

## 原始证据定位

以下case均在`E:/OurGame/Knight/.evidence/october13-stable-claw/`，含provenance、fresh build、before/after库存、三raw和二进制：

| case | 结果/边界 |
|---|---|
| resident-geometry-20261016-085858-943-aed1cff3b0ae40138b65d99aa6673d17 | 交互缺失RED，1/1152ms；旧51通过 |
| resident-geometry-20261016-091512-980-111e5bbdd02e427a9ca2329fbdc8cbb0 | 仅漏Boss目标计数失败，1/1152ms |
| resident-geometry-20261016-092033-480-3f2273c6c2ab424e915d37130455b2db | 57交互/旧项通过，0/19973ms |
| resident-geometry-20261016-092353-954-1e96135bc48645f9a2a46675edc8afe9 | 分散排序三行为RED，1/20713ms |
| resident-geometry-20261016-092622-577-2e9af640e92c453287cedc6e18787cde | 分散5项通过，40候选，0/18936ms |
| resident-geometry-20261016-093020-142-38ea658e2a14401a94b3607f65cb12ae | 原保存岩台三行为RED，1/1630ms |
| resident-geometry-20261016-093759-569-bf601831f7e448aea0a7aacdd8d8763d | 最终5Archive/57交互/51环境/28身体/29走廊/4联合/5分散通过；41候选，0/20047ms |
| entrance-enclosure-20261016-093914-723-f4faac32f6dd4fa99a511792f67b2d45 | 旧x1520助手目的地不离新台，严格FAIL |
| ui-entrance-enclosure-20261016-094130-950-038b40e6a1a240a39a2e1292cbd30820 | 录像未到新台，1/33557ms；失败已归档 |
| entrance-enclosure-20261016-094940-326-b489239ddb9a43aa88c09aa338f51088 | 实际键盘无窗口局部通行，0/27045ms |
| ranged-route-exploration-20261016-095253-551-876e8885270d402cb686f68898202f27 | 普通Explore失焦，1/7422ms，不验收 |
| prologue-pre-20261016-100324-477-3b1672d1bcc54aa9bb2fe76f5490ddee | 完整pre旧Upper/profile失败，1/88491ms |
| prologue-post-20261016-100606-635-f81eecb2f10b4fb2932a919783776248 | 完整post旧BellChain/recovery失败，1/8133ms |
| canyon-structure-v2-20261016-100719-842-360236578740427c8aea8f2b09dfb269 | 结构33负控通过，0/1249ms |
| resident-allocation-20261016-100832-899-2bdb3228d5b94334b6bbb13eff2ad06d | 分配17负控通过、实际新增0，0/397ms |
| actions-20261016-100919-177-8569f0dd11ed4fe4868c254fcceb3f34 | 动作专项通过，0/7104ms |

构建在`E:/OurGame/Knight/.evidence/october15-entrance-pilot/`：`descent-build-20261016-093523-731-7dd55d2a65ed490fae2b30601783090f`、`parent-build-20261016-093644-951-e684646a3a6d45558018018b8278640c`。独立构建输入10项控制RED/GREEN在`.evidence/october16-map-owner-contract/20261016-093330-142-65b75692e48e48f7b576e3d6e6576c65` / `20261016-093510-398-0f26ae59245f45088f4888beeea9e346`。

最终文件SHA256：EnvInventory `A25A4BA4BBC77CC633218227B4AD9C5ACAFE33F02F7D97E13458EAEDAD6A80A3`；Geometry `1509E979B4E00737B505DB772C50564C0B887AABBB799239C649E352BA95397B`；ContentBuilder `33A7B0FD930ADA808C0C231652CA37DE0F1978D7464EB3C3BC144B80E2BBD943`；Descent `E091C51C8D46ED117FB6020DBED64750914B72D9544856E179A7CE0ACB9D1CD6`；PrologueLevel `3427696E7521BB6471E184CC0BD6F3CF343AC990462B2075A810E58357C19640`。无故意损坏源码遗留；没有付费生成/Git写入/上传/自动化/安装升级/删除或真实userdata语义读取。
