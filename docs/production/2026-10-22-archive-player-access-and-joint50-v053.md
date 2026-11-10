# 档案实际通路与联合50空间候选 v053

2026-10-22，Asia/Shanghai。统一总根 **G:/knight**，实际工程 **G:/knight/Knight**。内部制作进展，不是正式试玩／美术／全序章交付。完整1–11及常驻Tasks1–4继续ACTIVE。

## 事实：本轮完成了什么

### 1. 档案上层：保留真实失败，换用已有能力的可行路线

旧两K、零跳路线仍是失败证据。071335-e821643a 的实际 Obstructed 发布帧497，独立重建保留了当帧位置／速度／上一实际frame、实时LOS及真实TestMove：LOS=true、nearDock=false，预期位移(3.724,-.583)，碰撞体为 ArchiveResidentUpperFloor，实际法线Left，保存目标法线却是Up。它支持“下落惯性撞到台侧”的判断，但**不是控制器内部分支抓取，不能冒认唯一根因**。

没有改拉力、重力、身体尺寸或地形。新测试路线使用已获批的普通跳跃和机械爪：原ArchiveEntry完整支撑 → Space+D普通跳 → D+K抓已有上层背墙Left面 → 同cast实际Arrived → K取消 → 完整落上台 → 走入内部 → 原ArchiveShelf／ArchiveFloor／FrozenLeftCatch返回。

最终同冻073258-c650c653实际结果：

- 墙面anchor=(1944,1195.9757)，真实dock完整胶囊查询清空；上层内部停在(1855.558,1211.9877)，终点(1336.0747,2143.9595)。
- 同一实际Player，340次观察、345逐physics检查；HP5→5、治疗0；1普通跳／1cast／1Arrived／1Cancel，Boost0、短喷0、入树后位置／速度／HP写入0。
- 身体保持40×72，全部潜在危险域不缩；命名平台接触、完整横向支撑及三脚底射线必须真实通过。
- 共用原生300帧上限从Space前开始，含起跳／准入／到达／取消／落地；下降另加真实native120离台上限、fresh释放采样和逐physics neutral输入证明。

这明确替换了测试驱动的旧零跳路线，**不是说原两K零跳路线被原样修复**。世界机关回调停止、居民已移除；受控起点是原Entry，不是正常游戏开场。尚未证明占用战斗、下层玩家入口、完整Main／Explore、真人手感或最终视觉。三个地面居民仍各有360次实际巡逻观察；另两个槽的几何检查不能当作攻击／运动认证。

### 2. 当前143实体的联合空间调查首次完成50/50候选

073928-615e82e6为普通构建、严格PASS、native0／25.496秒：

| 项目 | 实际结果 |
|---|---:|
| 身份／子型 | 50／24 |
| 共同选择／未解 | 50／0 |
| corridor检查／joint搜索节点 | 1695／71 |
| 保存的可解析JSONL记录 | 21420 |
| 实际Ready／运动认证／正式写入 | false／false／0 |

JSONL在该案 `storage/candidate-leaf-geometry-diagnostics.jsonl`，SHA256=D8C5CEC0D5BDC761613B73EEEC65878B55862AE0B6F4646A46709709E06573CC，21420条全部独立解析通过。使用真实保存叶、实际factory与原生几何查询，证明本次有一组完整不冲突的候选活动包络；search_capped=true，不能推成全局最优、所有搜索都穷尽、真实50只在关卡中活动或每个专项巡逻坐标都与本次自动候选相同。

正式保存世界仍127实体，候选143／Descent单独重建135；正式新增0/50、全50 Placement=null、24正式子型Unimplemented。原26+新50+Boss、骨骸10／幽灵10等完整要求未削减。这次census没有崩溃，不足以关闭此前的匿名GCHandle FATAL。

### 3. 档案范围窄生产器已制作，但未发布

真实RED072130-785c7831先检测保存Archive仍640而要求704；不是编译错误或过时的donor计数失败。新增固定用途 `SteamMetadataSynchronizer.ApplyArchive`，只允许两个现有节点的三个字段：

| 节点 | 生成值 |
|---|---|
| Gameplay/MicroScenes/ArchiveSideCave | local_bounds=(0,1176,704,624)；Position=(352,1488) |
| VisualRoot/Midground/ChallengeCompositions/ArchiveSideCave | CompositionBounds=(0,1176,704,624) |

Descent没有readability wash，不创建它；Composition自身Position仍0。283节点保存／回读、全部非白名单存储语义与深层资源／共享关系、persistent links、幂等、6个原子拒绝、碰撞负测、原Descent29／世界127实体和完整小地图字节／Revision／边界顺序检查均完成。

独立档案phase与完整原Steam phase都保留，原始错误仍有否决权；无ExpectedFailure。直接new donor形状按确切构造路径、类型、尺寸、互异native ID及与借入图不相交审计：Descent48=42Rect+6Circle，Basin45=39Rect+6Circle。已证明新建的资源才在finally释放；不Dispose正式借入Shape／Script／PackedScene。修复初始化在try外的清理缺口；工厂内部未返回的部分构造异常仍不冒认已验证。

`SaveAndValidate` 的局部直接new Definition改为scoped using，借入Scene不Dispose。这是已知拥有权内的改动，不是历史寿命故障根因结论。

**发布仍被阻止**：最终独立Archive与Steam两案均在成功标记后报266 Shape RID，严格FAIL。中间组合phase报252、早期RED报40；这些全部保留，计数本身不能识别资源拥有者。没有复制三个字段到正式Descent，没有更新独立640断言来冒充同步完成，更没有把候选143整体覆盖正式127。

## 事实：最终同冻17项原生回归

所有目录在 `G:/knight/验证证据/october13-stable-claw/`，名称由phase＋20261022＋下表时间＋GUID组成。原生exit0不覆盖原始日志错误。

| phase | 时间／GUID前缀 | native退出／ms | 严格结果 |
|---|---|---:|---|
| resident-archive-alcoves | 073258／c650c653 | 0／13952 | PASS |
| resident-archive-metadata | 073339／fed260ed | 0／4988 | FAIL：stdout266ShapeRID |
| resident-steam-metadata | 073418／329597b9 | 0／5675 | FAIL：stdout266ShapeRID |
| resident-gear-left-access | 073451／3f2102c4 | 0／19414 | PASS |
| resident-gear-alcoves | 073538／e2775c41 | 0／9980 | PASS |
| resident-parent | 073617／d067e413 | 0／2385 | PASS |
| resident-steam-alcove | 073650／dc860c11 | 0／10907 | PASS |
| resident-pendulum-alcove | 073731／4f61d7a9 | 0／19234 | PASS |
| resident-abyss-alcove | 073819／c0c2053b | 0／8757 | PASS |
| resident-frozen-player | 073855／859d69a9 | 0／5436 | PASS |
| resident-map-census | 073928／615e82e6 | 0／25496 | PASS，50空间候选非正式人口 |
| resident-void-stream | 074023／98632056 | 0／1448 | FAIL：stdout187ShapeRID |
| keyboard | 074059／9ee05aea | 0／9568 | PASS |
| actions | 074138／9b210cb1 | 0／7065 | PASS |
| canyon-structure-v2 | 074218／e0ef2264 | 1／887 | FAIL：Archive640／704 |
| prologue-pre | 074247／84db4cfe | 0／99559 | PASS，完整原scope |
| prologue-post | 074456／b8e19fe9 | 1／998 | FAIL：同Archive断言 |

最终owned会话8697终态exit1：**17=12PASS／5严格FAIL**，非16项。brief冻结前的16为计数笔误，以实际17个回执为准；没有删测试。Post在结构断言处停止，原75scope／8owner／4negative后续门未执行到，不继承旧版通过，也不放宽owner接纳范围。

### 保管链与当前文件

源清单1709项／879 C#跨17案一致；只有5个源文件相对v052变化，没有新增／删除源清单项。独立审计85对前后清单、6060份选中source副本＋174份helper副本、102份binary、34组PID／完整argv／contract／wait均吻合；17次构建exit0、0warning/error、无timeout。选择副本不等于全部1709源文件实体备份或整库Git认证。

| 清单／文件 | SHA256 |
|---|---|
| source-after.txt | 015AF9B377087D675EB18860B56654F594E156B74309943E008E9B5623A28762 |
| helpers-after.txt（post9项） | 390740CB20BD9E7D7B577ABA7D1E7ED0A4E698E73DD61D20ECC28122BF8AA49D |
| helpers-after.txt（census10项） | 62074C24F3FD1919616C889A2EBAF04E13973199572094181B6026F7882CE6DB |
| helpers-after.txt（pre10项） | 460F5F2493988046295704C941D1A2AD11B9D238C44786690DF68DBF56E23ED9 |
| user-after.txt（109行，104文件只核哈希） | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| binaries-after.txt（6项） | 75DFBD8AC0479599358FFA31B9280ABE6EEB055343B2B3D29A540F9503B0BFCC |
| Knight.dll | 83AE222E2ABD6AAB34FF4DC3BBC6583927249F6EE8BB234DF802A8E6878D88AB |

Root在全部consumer终态后独立回查当前1709source／9helper／104真实用户文件哈希／6binary，差异0；没有读取真实存档语义。HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index SHA仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。普通构建，不用身份诊断二进制替代。

状态文档写入后再核1709source及三个helper清单的11个去重文件，差异仍0；诊断JSONL当前SHA与独立解析记录一致。工具读回目标状态仍active，未重建或缩减目标。

## 推理与审查边界

新路线避开了旧路线取消后持续下坠再抓台顶的冲突，已有能力足以完成本次上层访问；不需要据此调整全关拉力或新增二段跳。联合50候选证明地形容量已有实际进展，但仍缺每槽真实活动／攻击／返回、玩家接近及正常共存。

独立有限审查未发现已证实Critical或源码Important；资源原始错误仍为验收Important。其Minor初始化清理已修；审查不是整关、merge或正式发布批准。审查不覆盖的加强版路线与新Definition普通影响由本次实际回归补验；下层、占用战斗、正常双路、真人、正式人口、最终美术等全部继续OPEN，不因有限审查被删除。

同一审查者另做只读Minor修复复核：三个函数的顺序获取／已返回根清理补丁未发现新Critical／Important；不证明工厂内部异常恢复或RID修复，原失败和范围边界不变。

## 风险与下一步

1. 先用可归属的资源创建／释放证据定位metadata／void故障；不靠重复安静票、GC、缓存、KeepAlive、新drain、借入Dispose或引擎升级掩盖。当前census通过不关闭历史FATAL／音频／句柄问题。
2. 窄同步只有严格stage可接纳后才发布3字段，保留正式127物理／演员／进度／小地图，再做真实结构与完整post75／8owner／4negative。
3. 下层先做已有能力真实RED：旧ArchiveFloor左口→D+S选新台Left面→实际Arrived→Space+A借力→fresh D入96px夹层→原FrozenLeftCatch返回。现有dock候选x1376、身体右端1396，离Ice仅4px；右绕被旧AbyssBrakeLeft2与SupplyWall封住。薄24px侧面准入和旧Floor底角都需原生证据，不能把只读提案当可玩通路，也不借Gear侧壁授权增建Archive地形。
4. 之后完成全部50实际载体／巡逻／攻击／回家／生命周期／旧26共存、正式接线、最终统一视听／场景／UI、正常Main与Explore、存档死亡继续、720p至4K／目标机60FPS／真人试玩及唯一版本Windows与获授权GitHub交付。完整1–11不缩减。

历史identity契约cleanup／call-regex及v050 bottom180+8尾段共享预算问题仍未关闭。本轮无新付费、美术、音频、展示媒体、OS前台操作、Git写入、删除或自动化。手动媒体-Check成功：4838字节／基线3508／新增1330／Notify=false，不重置基线、不恢复旧归档。没有完成／暂停／阻塞目标。
