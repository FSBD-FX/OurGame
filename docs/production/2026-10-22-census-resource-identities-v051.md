# 人口调查资源身份诊断与普通构建复验 v051

2026-10-22，Asia/Shanghai。用户再次确认统一资源总目录 **G:/knight**，实际 Godot 工程 **G:/knight/Knight**。不恢复已删除的 G:/OurGameArchive，不切换到另一聊天的 UE 工程。本轮是排错进展，不是寿命修复、正式新人口入关或试玩交付；完整1–11及常驻Tasks1–4继续ACTIVE。

## 事实：本轮实现与测试先行

仅在 `REFCOUNTED_FATAL_IDENTITY` 诊断构建中，将真实 ResidentSubtypeProfile、临时 PrologueSectorDefinition 的创建来源/managed serial/native ID关联到既有Dispose ENTER/RETURN。静态记录只有字符串和数字，不保存Resource、Node、委托或闭包；主线程记录原生身份，finalizer只查已记录标量。补充package/save/environment/旧身体/首次Raw/首次查询边界，避免把“首身体日志前崩溃”误判成“未做身体查询”。

真实调查没有新增Dispose、GC、cache、KeepAlive、等待、退出drain或引擎/依赖升级；原runner退出drain保留原样。24类/50只/原生身体/危险区/联合搜索上限/地图物理不缩减。新增独立原生身份契约和当前owner严格诊断准入/三日志分类；诊断成功标记与provenance明确排除ordinary census/行为/整体资格。

- 当前owner AST准入先RED，再24行为例GREEN，执行实际生产函数及顶层混合参数拒绝，不是另写一份准入逻辑。
- 原生契约 `053509-267-85eb0c99` 因真实Profile serial=0/unknown_serial精确RED，engine1/564ms、guard0；不是编译失败或游戏崩溃根因证明。
- 添加关联后 `053754-173-845d86f6` 契约GREEN；最终同源 `054700-234-26c82435` 再次完成，engine0/534ms。真实2个Profile＋1个Definition具不同serial、各一组既有显式Dispose记录，释放后标量映射仍可查/native ID失效。**不证明托管包装已被GC回收。**
- 当前owner结果分类先RED，再51行为例GREEN；缺失/重复/混合模式标记、错误数量及三sink分别注入ERROR/WARNING/FATAL/断言/RID/ObjectDB/invalid handle均拒绝。旧实际producer＋passive stub分类器另21例GREEN，未改原严格分类器取绿。

24＋51＋21不是96项游戏玩法验收。新增测试源使实际inventory变为1708项/878 C#，没有放松数量guard。

## 一次改变证据目的的真实诊断

证据根 `G:/knight/验证证据/october13-stable-claw`；实际案 `resident-map-census-refcounted-identity-20261022-054222-433-2c7a774488734e929e0034c333a42f80`，engine0/24447ms、无超时、guard0。**崩溃未复现，没有实现寿命修复。**

50身体均有候选；活动联合48选中/2未解、1649 corridor checks、60 joint nodes、search_capped=true、21077条JSONL。全部真实保存叶/24子型/实际factory，actual_ready=false、placements_authored=0、motion_certified=false。最终仅诊断成功标记，behaviorAccepted=false/censusQualified=false。**48/2只属于此诊断案，不能当普通139地图资格或正式出生。**

独立日志审计：123条已登记身份＝五类Profile来源各24＋3临时Definition；2051次调用均唯一ENTER/RETURN、字段/时序对应。1973次finalizer在线程1，78次disposing=True在线程2（本案声明主线程）。已登记serial/native ID未复用。仍1928次unknown：931有serial但无来源（929 PresentationProfile＋2 BossTuning），997无serial。内建Shape/PackedScene/World2D等及部分项目Resource不在hooks覆盖内，configured actor没有独立Profile观察。

109组raw pointer地址复用中19组涉及已登记对象，后次ENTER都晚于前次RETURN；地址相同不能指认同对象/重复释放。RETURN指针归零不等于全部底层资源销毁。最早新增边界处finalizer为CanyonBasin构建开始后的BellBoltSentrySettings，记录均正常RETURN，不能回推旧FATAL对象；Raw Profile的disposing=True本身也不能识别调用者或证明借用资源误释放。

汇总强制验证fixture Profile24＋临时Definition3；另四类Profile各24是本案日志事实，不冒称全由汇总断言强制。本案含既有退出drain，未为本轮新增或加长。

## 恢复普通构建：10门6PASS／4严格FAIL

最终外层session30989已TERMINAL exit1。先独立身份契约诊断完成，再按表顺序逐项普通Rebuild；所有build exit0、无超时、guard0、命名豁免为空。诊断与普通binary不同，不合称同一binary。全部owned native终态后才改状态文档。

| 普通phase | 20261022时间／GUID前8 | build ms | native ms／exit | 严格结果 |
|---|---|---:|---:|---|
| resident-subtype-profile | 054729-736-07271886 | 22719 | 781／0 | FAIL，stdout退出31 Shape RID |
| resident-scene-binding | 054758-456-b5dccded | 23340 | 7276／0 | FAIL，stdout退出154 Shape RID |
| resident-map-census | 054834-430-94614ea0 | 22569 | 7332／-1073741795 | FAIL，GCHandle原生FATAL |
| resident-gear-left-access | 054909-697-ec9d63a2 | 21935 | 18842／0 | PASS，沿用v050两独立Player leg的局部范围 |
| resident-void-stream | 054955-806-fde4174e | 22397 | 1444／0 | FAIL，stdout退出187 Shape RID |
| keyboard | 055024-906-cc6d78dd | 23761 | 9672／0 | PASS |
| actions | 055103-657-84dc22de | 21968 | 7080／0 | PASS |
| canyon-structure-v2 | 055138-450-84bbf899 | 25093 | 1345／0 | PASS，正式127 |
| prologue-pre | 055210-350-7f0c1af7 | 25740 | 100466／0 | PASS |
| prologue-post | 055424-812-ccdf1b21 | 26577 | 149959／0 | PASS |

post原75案例/0退役、8次同owner主场景1→1保持。四条精确故意负测warning在stderr/external镜像，不计成八个独立事件；不是当前139正常Main/Explore、真人、显示性能或最终视听验收。

三个RID失败案stderr为空，external只有阶段成功标记而无退出RID；错误在stdout，不能只看external就判clean。普通census再次报 `gchandle.is_released()`、cpp1788、GodotObject.Dispose/Finalize、0xC000001D；external镜像是同一次事件。普通版联合人数/JSONL未完成，不能拿诊断48/2替代，v049/v050 FATAL与历史metadata252/Handle/音频问题均未关闭。

## 推理：下一步与未证明范围

诊断改变时序/二进制，安静一次不表示修复。普通案再现，但仍无可归因坏对象；下一次资源实验须有可区分假设与反例，不原样抽绿，不用GC/drain/强保留/borrowed Dispose掩盖。

只读空间审计定位两未解：`canyon.v2.resident.archive-side-cave.03` bone-charger、`.04` bone-diver。Archive五只单独均有候选，Grow24包络互冲，联合最多3/5（15节点、未耗尽）；全局search_capped不是此处可放5只的证明，不能缩身体/活动域/净空。

下轮**未运行提案**：下层 `(1424,1776,496,24)`，上层floor `(1744,1240,224,24)`＋eastwall `(1944,1100,24,164)`，须显式Archive东界扩64至1984。下层台底距FrozenCeiling仅40px，不称台下可通；上层进入ControlledAbyss西缘，可能改变旧K选点/拉升。先真实保存Player从原路线进入、K稳定中继、实际取消/惯性落脚/完整支撑/返回及原Frozen下降口取得具体RED，再改真实实体；不注入姿态/速度或增加输入预算，算术候选不入正式图。本轮未制作/运行新地形。

正式127/候选139；50 Placement全null、24正式子型Unimplemented、正式新增0/50。原26＋新50＋Boss、骨骸10/幽灵10、全部机关/最终原创资产/UI动作音频演出、正常双路/存档/真人/60FPS/带版本Windows ZIP及授权GitHub交付要求不减。v050 camp helper的180到达＋尾8帧共享上限硬化仍待做，不能称完整BasinReturn/存档证明。

契约helper两次要硬化项暂留：首次Dispose抛出后cleanup再次抛出可能抢在Console.Error恢复前退出；call regex未显式要求匹配非空/非零。当前实际RED发生于成功Dispose后，不是这两个分支触发；真实census不用此StringWriter helper。不能称这两分支已测或已修。

## 保管与存储

独立复核最终11案55组清单对/20157条逐案记录、1863组实际before/after副本（1803源输入＋60 helper）、66份binary，无missing/extra/hash mismatch。源输入副本为选定子集，不是1708项全复制。22个build/engine PID/argv/contract/wait一致；当前重新枚举1708源/109用户项/6binary与post-after一致，真实userdata只核路径/长度/hash，未解析语义。

| 集合 | SHA256 |
|---|---|
| 11案共同source manifest | `135F7AEC71D11193FA245DEEA55245458FED8C1DD456AD0AEF458EFE1FFA5FF8` |
| 首案REF六binary manifest | `94F1B753E673BB3D1CD9A80309F3ADB5C1024EF1A4024E668115E8B7BC5B7C84` |
| 后10案ordinary六binary manifest | `53E49DBDE2D84CEBECF19D8E87F6C5C85B87C128E1F48A516343673ED8B26DF7` |
| ordinary Knight.dll | `1DE5A4A5323ED7EDDB35C1567576B3E92AC63FB0678CB6418F566FCFFA5E105E` |
| REF Knight.dll | `C06D069A0725BCCC310D29BA935B1D9EFB12620CBD0F4343544B21A83A50EEB2` |
| hash-only userdata manifest | `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB` |

两套binary仅DLL/PDB不同。commit保持 `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA保持 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`；没有Git写入、付费、前台/OS输入、自动化或删除。

无新图稿/视频或最终美术。一次手动媒体检查成功：current4838/base3508/growth1330字节，Notify=false；无重置基线/旧归档恢复/定时器。后续关键节点媒体仍存 `G:/knight/过程图稿与视频`，每新增20GiB只随汇报提醒、不停止工作。普通构建已恢复，所有失败原件保留。
