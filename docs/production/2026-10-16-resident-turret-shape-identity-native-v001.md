# 炮台保存碰撞形状身份诊断 v001

日期：2026-10-16。状态：诊断取得新信息；严格资源门仍 FAIL，完整目标 ACTIVE。

## 事实

本次只修改测试夹具、显式测试入口及证据保管脚本，没有修改生产敌人、地图、数值或资源释放策略。复用原有15项基础行为、5个保存场景和原来的物理帧/清理时序，没有另做简化夹具。

在每个保存角色挂载、原有6帧落地检查完成后，通过原生物理服务器读取其身体、受击区域、机械爪目标区域的 shape RID 标量。先检查每个原生 owner 只有1枚形状，再读 index0。没有读取借用 `CollisionShape2D.Shape`，没有保留新的 Shape2D 包装引用，也没有释放任何借用资源；测试自建形状的ID来自已经持有的自有引用。

| 保存场景 | 身体 RID | 受击 RID | 抓取目标 RID |
|---|---:|---:|---:|
| RivetScatterGunner | 146028888065 | 150323855362 | 154618822659 |
| BeltBurstGunner | 833223655428 | 837518622720 | 841813590022 |
| FurnaceMissileGunner | 1374389534727 | 1378684502021 | 1382979469321 |
| PrismLaserGunner | 1954210119690 | 1958505086984 | 1962800054284 |
| BellBoltSentry | 2534030704653 | 2538325671947 | 2542620639247 |

每个场景分别经过未配置固定站立、配置家点站立、原目标完整身体离开领地三个原有案例；各次 actor 和 World 实例ID不同，但上表中同一场景同一部位的形状RID在三次挂载间完全相同。共15个不同保存形状RID，另有25个不同测试自建形状RID，两集合交集为0。

原有15行为案例全部符合；15次清理均 `native_gone=True`，自有形状合计25次分配/25次显式释放，借用释放0。它们不是新增15只怪物，也不是正式地图人口验收。

## 原始证据与严格结果

原始目录：`E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-identity-20261016-180734-471-c88c08a8cabb4024822520028da4866a`。

命令：固定PowerShell运行 `.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 -Phase resident-turret-identity -SourceRows 1669`。新鲜Rebuild，无restore/共享编译；headless，非movie/固定帧/verbose；engine上限仍55000ms，未给予命名失败豁免。

- 构建exit0，0警告/0错误，构建stderr空。
- engine exit0，24887ms/55000ms，无超时；provenance guards0，human/naturalRoute/wholeGoalAccepted均false。
- 15身份快照、15 CASE_OK、15清理回执；foundation与identity终结回执均存在。
- engine stderr空；独立external原始日志无诊断。
- **engine stdout退出仍有 `15 RID allocations of type 'P12GodotShape2D' were leaked at exit` ERROR，故脚本exit1、provenance严格FAIL。** `STABLE_CLAW_OK`、engine0和external无错误不覆盖此失败。
- 源码/helper/真实用户库存/隔离继续存储/新鲜二进制五组before-after清单逐字相等；source1669、真实保护库存109。源码清单SHA256：`DB5B6ABC2B36C1CFA24351F6721225040B6A924EB201235427F3F5BC8DAD02CD`。

Root读过构建、provenance和三份原始日志，另有只读独立核对。Root最初手工清单核对误写了二进制清单文件名 `after-binaries.txt`；实际文件为 `binaries-after.txt`，更正读取路径后五组均相等。这不是原生测试失败或证据豁免。全部消费进程终止后才更新本报告。

## 推理与边界

可确认本次运行中的三个挂载形状由同场景多次实例共享，而不是每次测试世界各生成一套；它们与夹具自建形状不是同一批原生RID。这排除了“每次fixture独立产生三个保存shape”这一解释。

**不能把15个不同保存RID直接等同于匿名退出15RID。** 退出诊断未给出身份对应，数量吻合仍不是泄漏根因或修复证据。不能因此强行Dispose场景缓存或其借用子资源。

本快照仅覆盖挂载的保存身体/Hurtbox/ThreadTarget形状：四远程actor各自创建的私有 `_beamCollision` 是未挂载的查询形状，明确不在覆盖范围；Sentry之后产生的两枚动态bolt形状也不在本次挂载快照范围。此前针对动态bolt的所有权证据不能扩大到全部资源。

没有增加GC、cache模式、更长等待、退出排空、KeepAlive、借用Dispose、日志抑制、测试专用生产API或已销毁RID查询。RID非零并不证明其在销毁后还存活。

## 当前代码身份

| 文件 | SHA256 |
|---|---|
| `scripts/enemies/ranged/RangedWeaponEnemy.cs` | `AB32D85E3EF5AA9B349BCA6F53EEE013E326EDD64C77A8EE1A39448320904CE1` |
| `scripts/enemies/sentry/BellBoltSentry.cs` | `BCFC7BEE0680D68E6443CFE527D24A553D5BA19B3B1DD805F90A7DD50509CD59` |
| `tests/integration/ResidentTurretRuntimeTests.cs` | `CD3EDB12C73C76880BFEAE9E1D9A84944D922093A04560CD2DC41B8E152E0D49` |
| `tests/RunStableClawPullTests.cs` | `9A664D7F3D8314AA3CF22E5FD2E8CC5C7701C41F47BEB337BC3D277EE90EBE41` |
| `.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1` | `48CE627E82397F7D2D3A036CD39EDD53F4A6A108DC2470645977DB2A5C479489` |

生产两文件与[上一生命周期报告](2026-10-16-resident-turret-callback-lifetimes-native-v001.md)完全相同。此轮没有重跑完整pre/post或正常双路线；上一轮旧Upper错台/profile警告、旧BellChain落地/Recovery失败及历史资源/FATAL边界继续保留，不宣称新鲜回归通过。

## 下一步与完整目标

本次已回答有限的“同场景保存形状是否复用、是否与自建集合相交”问题；没有新的因果问题时不重跑同一身份阶段。后续资源定位若需要Resource实例身份，必须单独标注读取借用包装可能带来的观察扰动，不能悄悄改变释放合同。

继续原Task2实际负感知/配置原子性/外力/void/pending/Burst同帧多发撤权与完整墙前预警检查，以及最终职能、族系、全部50活动包络和人口manager；资源门继续OPEN，而不是靠清单计数或无区分力重跑拖住全部生产工作。

正式新增0、50 Placement全null、24正式族系场景未实现；骨骸10/幽灵10等本关要求不减，不能把原型称为最终族系。Task1最终50认证、完整Task2、Task3–4以及旧档/正常Main和Explore/有支撑出口/实际呈现60FPS/真人/最终美术/审阅短录像均未验收。

用户确认本次测试时使用其他窗口，与该次失焦清键相符，但不能解释更早失败录像。继续无窗口、不抢焦点或补键；本次无新媒体、美术生成、付费、Git写入/上传、自动化恢复、安装、删除或真实用户数据语义访问。完整目标保持ACTIVE。
