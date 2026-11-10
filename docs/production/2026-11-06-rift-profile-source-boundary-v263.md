# v263 · Rift Flyer 的空值从哪一层出现

只读追溯，推进 Q05；**未修复资源崩溃，不是新一轮回归或试玩验收**。上一轮 v262 属于进展：真实失败取得了类型线索，普通输出已恢复。此次进一步定位下一条需要观测的边界，不重复同种探针。

## 事实：差异早于赋值

对同一份 v262 REF post 原始 `engine.stderr.log` 重新筛选。案例位于 `G:/knight/验证证据/october13-stable-claw/prologue-post-refcounted-identity-20261106-034813-587-9631532dae1545558fa415d09de3893b`；stderr SHA256 `61F6211958696AE8E9DD5FEEEAF23B9086648E360367D44176C208BE493E8C2B`。下表只比较同一 `canyon.v2.resident.staggered-rift.04` 首次取用 `rift-flyer.tscn` 的 load 路径；不把相邻槽的 cache hit 混为新加载。记录都在当前案例进程内。

| 操作 | 缓存 | load enter → valid return 行 | 其间 Profile 构造记录 | setter 结果 |
| --- | ---: | --- | --- | --- |
| 4 | 18 | 164 → 167 | 166，serial 42 | 170，非空 |
| 114 | 851 | 6450 → 6453 | 6452，881 | 6456，非空 |
| 138 | 1243 | 8785 → 8788 | 8787，1264 | 8791，非空 |
| 162 | 1343 | 9772 → 9784 | 9782，1358 | 9827，非空 |
| 186 | 1434 | 10818 → 10821 | 10820，1449 | 10824，非空 |
| 210 | 1591 | 12080 → 12083 | 12082，1612 | 12086，非空 |
| 234 | 1694 | 13071 → 13076 | 未观察到该操作的 Profile born | 13083，实际 managed null 一次 |

同一资源的另外六条 `.controlled-abyss.02` 赋值也非空；全案 Rift Flyer 为 13 条赋值记录、12 非空、1 空，不当作故障概率。op234 在管理器自身缓存是 load 路径，不是 cache hit；其 `source-valid-return` 只说明得到有效 PackedScene，**不说明子资源是全新创建、Profile 有效或引擎内部无缓存复用**。op234 同窗口的终结器记录属于其他具名资源，不能按时间拼接为 Rift Flyer 的 Profile 或 1610 的 owner。记录未观察到上限告警，但进程未完成整段诊断，不能以无 `born` 证明没有构造，更不能用它证明缓存失效或终结器致因。

## 推理：现有项目钩子看不到原始 Variant

只读助手 `/root/v263_local_bridge_trace` 检查本机普通 `Knight.dll`、`GodotSharp.dll` 的 IL 与 PDB（普通文件分别 SHA256 `7827D91E3C9A56ABD27FF9E9187DB460E4DBFAF512B24FF810A81915AC5072BD`、`7B1D06575DB78E4EA4353186BE5ADD207E43617B836DB24D65EC4088E6FAD18C`，不是宣称其为旧 REF 二进制）。生成的 `ResidentSceneBinding.SetGodotClassPropertyValue` 在 IL 15 先 `ConvertTo<ResidentSubtypeProfile>`，IL 20 后调用 setter。`CSharpInstanceBridge.Set` 先分派该分支，成功返回才不会走 `_Set`；普通 `_Set` 不是前置观察点。

PDB 的 GodotSharp `Core` 下：`Bridge/CSharpInstanceBridge.cs:52–72` 是分派/回退；`NativeInterop/VariantUtils.generic.cs:397–398` 为转换/类型转换；`VariantUtils.cs:489–499` 检查 Variant 类型、ObjectId 并查对象；`InteropUtils.cs:14–48` 包含托管绑定解析及可能创建绑定的路径。非 Object、零 ID、查不到原生对象、查不到托管包装均可到达真实 null；非空的错误托管类型会抛转换异常，不是静默 null。故不能将 op234 的 null 唯一归因于某一步，也不能将其等同 v262 的 EnemyArchetypeTuning 终结器或上轮普通 case21。

## 风险与后续选择

只读助手 `/root/basin_handle_boundary_readonly_v253` 核对现成 `prologue-profile-lifetime-pair -IdentifyRefCounted`：55 秒，执行原 FallContext + GeneratedMainDeath，确实能到该槽，但分类器验的是 scrap-scatter 两 owner，Rift Flyer 仍 `CaptureAfterSource=false`；没有实际转换输入。原有两案实证覆盖 `.staggered-rift.04`：`...20261027-194218-211-11283c8d3cc4456e8376786cec59e7db` 7212ms exit0，四条 Rift 赋值全非空；`...20261027-195847-293-283b4cba25d54c1eb21fc9641c4f0638` 23849ms、0xC000001D，先通过第一段两条 Rift 赋值，第二段早于再到该槽时 **PlayerTuning** 终结器失败。两案 1857 源清单和 sealed binary 清单相同，但 helper 清单不同，且均非本轮 1898 变体；不能当严格同配置对照，更不能移植旧类型或 PASS。本轮不重跑这条只会补下游结果的入口，也不用生成候选包的测试替代实际保存场景。

若未来批准扩大诊断，最窄有区分力的边界是**原生成分支内，在同一次转换前只读借用 Variant 的 Type/ObjectId 标量**。现有生成 override 占用该入口；不另加重复 override、不改变导出类型或全局关闭生成器、不在普通路径引入探针。需要先限定诊断构建替换并证明原赋值逻辑一致；非零 ID 仍不能单凭日志区分 ObjectDB 与托管绑定失败。`SceneState.GetNodePropertyValue`/`AsGodotObject` 是另一次观察，可能产生包装或绑定，不能补认原输入。此方案未实施，不引入新 dump、引擎、GC、cache、资源保活或自动补空；诊断线与已批准的动作连续性工作可以分开推进。

本轮重新逐项核对当前 1898 原生输入与 v262 源清单一致、6 普通输出与恢复案相同；没有构建、执行引擎或更改代码/资源。普通后段失败、G04/Q05、正式动作及十一项仍开放。媒体 one-shot 检查 10,491,889 B / 97 项、Notify=false，无新增媒体、删除或自动化。
