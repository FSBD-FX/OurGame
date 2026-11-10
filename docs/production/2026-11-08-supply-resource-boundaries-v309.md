# v309 · 补给资源生命周期边界定位

## 判断与范围

上一轮 v308 既修复了真实零时间动作采样问题，也留下完整前段的九条句柄错误，属于有进展但未通过整版验收。本轮优先推进 Q05 资源稳定性，不把动作专项通过视作故障消失。

工程仍为 `G:/knight/Knight`。只对原补给测试增加诊断，不改游戏逻辑、动力、地图、资产、存档或引擎版本；不打开窗口、不发系统输入、不采集额外原生转储、不创建自动化、不删除或提交 Git。原四个补给用例、五次原场景创建、原 teardown／帧等待和原严格错误门保留。

## 实际改动

唯一 C# 改动为 `tests/integration/BasinSupplyRuntimeTests.cs`：

- 补 `load-enter/return`、`instantiate-enter/return`、`cleanup-enter/return`，记录序号、用例、阶段、原已创建根数和 engine／physics／process 三个标量时钟。
- 独立 64 行上限、遗漏／故障计数，原 128 行 mount 观察不替换。计数器在原 Run 入口保存／归零，finally 恢复原值。
- `TraceLoadedSample` 只将原 `GD.Load` 表达式的借用返回值传回原 Instantiate 调用，不读取身份、不 Dispose、不放进字段／集合／闭包／缓存，也不跨等待保活。
- 日志／包装调用仍可能改变短暂对象存活与时序，不能声称完全无扰动，也不能把一次不复现当修复。没有以显式强引用、GC、额外等待或缓存切换“消除”错误。

`createdRoots` 是旧观察器已记录 `root-created` 的累计数，不是物理存活数；Instantiate 返回标记打印时尚未计入刚创建的根。`load-return` 也不证明非空或健康，`instantiate-enter` 只是调用前标记，`cleanup-return` 只证明原 await 正常返回。四计数器可恢复串行／严格嵌套状态，不支持重叠并发 Run；本轮只有 root 一个规范原生运行。

源码冻结 SHA256 `37B3371094A446DEE4E4771681DAE4D6210B96400969547ED08C0778224D22EB`。离线解析器与 validation-only 自测在 `.superpowers/sdd/2026-11-08-supply-resource-boundary/`，不属于编译输入，未改规范宿主或其错误放行合同。

## 一次普通原生前段

使用原规范 `-Phase prologue-pre -SourceRows 1901`，原件：

`G:/knight/验证证据/october13-stable-claw/prologue-pre-20261108-060045-456-19e3fccf4635444896c598f2b67ca60a/`

原生退出 0、141,191 ms，原 44 项完整返回，但新增 **11 条 `Handle is not initialized`**，所以原宿主严格 FAIL，未重复重跑刷绿、未使用 named-RED 豁免。保护门为空不冲销 ERROR。原 v308 九条失败原件继续保留，没有倒改结果。

实际 `BASIN_SUPPLY_RESOURCE_END` 为 30/64 行、sequence30、created_roots5、omitted0、faults0；原 mount 为 40/128、五根、omitted0、faults0。诊断完整不等于运行成功，更不代表资源根因已知。

根已实际执行新增真实案的离线解析：30 个新边界与唯一 END 在 stdout／external 中一致；五对 mount 区间内共十一条 ERROR，第二个 `title_reentry` 五条（external145/186/227/268/309）、最后一个 `check lifecycle=false damage=1` 六条（400/441/482/523/564/605）。Load／Instantiate／cleanup／区间外均 0。不能因最近的新 marker 叫 `instantiate-return` 就错报成实例化错误；分类联合使用旧 `mount-enter/return` 和原错误栈。

最终冻结版解析器已由根实际重跑，仍返回 `traceComplete=true`、`rawCasePassed=false`、`wholeGoalAccepted=false`，错误库存精确为 11、mount11、load／instantiate／cleanup／outside均0，两用例5／6条。这里的完整只指新增 30 个资源边界与 END，不保证可选 mount 标记全数，更不是原生放行；没有把修订前结果拼成修订版验证。

与 v308 不同，当前同步 Load／Instantiate 区间初核没有记录 ERROR。这不证明载入的全部原生脚本句柄健康，也不排除更早加载／释放的影响；它只改变下一次需关注的时序边界。11 条日志不能当成 11 个已识别资源。

本案源码清单为 `DC9245150366E7092A46B1025E90F6B3B85E8F9A20FF8B0300712E965BCD0B1A`，六二进制清单 `1E9D7CD32B7D15B945CCAA243502C8AF00504E7E8AC15197230E4BF0E849647F`。与 v308 最终版只差上述诊断测试文件，不把当前案与旧版二进制混称同版整体验收。

## 同版后段与审阅

`G:/knight/验证证据/october13-stable-claw/prologue-post-20261108-060422-115-265e78ff97d84fef8b37e9420fedddcc/` 普通后 75 项严格通过：原生退出 0、189,525 ms，原四条精确负例警告，退出清理与最终标记完整。沿用原 300 秒门，不作旧 180 秒性能认证。完整前段仍 FAIL，最终两项原生为 1 PASS／1 FAIL，不合称整版通过。

两案源码／六二进制清单相同，各五保护对共十对无变；根逐项核对当前 1,901 项输入、六当前二进制，前 44／后 75 对边界索引、名称和进出顺序完整。实际终端 `V309_FINAL_SOURCE_AUDIT_OK native_pass=1 native_fail=1 protected_pairs=10 source_rows=1901 current_binaries=6 changed_files=1 pre_boundaries=44 post_boundaries=75 whole_goal=false`。这是身份／完整性核对，不冲销前段的 11 条 ERROR。

独立 C# 只读审阅 Critical 0／Important 0／Minor 0，仅认可诊断；特别指出同步借用值和 JSON 日志仍可扰动时序、load-return 不是资源健康、createdRoots 不是活体数。没有完整 v308 文本，未冒称审阅完成了精确历史全 diff。

PS 首版只读审阅 Critical 0／Important 1／Minor 1：I 为同 scope 的 mount 两侧可能跨 cleanup／下一次 load 错配，已交原作者以行为 RED 修补；M 为自测恒传 ValidationOnly，不能独立证明 rawCasePassed 的 ERROR 门禁，但当前实际门禁代码正确。本轮明确记录该非阻断测试缺口，不能把已有测试宣传成完整门禁覆盖。

I 已一次修补：mount 两侧之间只要存在任何原始 resource-boundary 行（含坏 JSON）就不成对；任一侧有 rootId 时两侧均须为正 uint64 且相等，否则保留区间外错误、不伪造 AddChild 区间。作者先复现跨 cycle 错误分类的 RED，再修正；根实际跑最终 71 条 validation-only 断言全部通过，并重新执行当前真实案 CLI。旧 v308 实读反控 exit1，缺30边界／END，九条 ERROR 仍完整保留为 mount5／outside4，不伪造旧日志新边界。没有第二次审阅来抵消首次问题，M1仍明确延期。

最终 Reader SHA256 `4E0296569EB3270BF2B0010E07999693A6041E7837C37E38EE4B333385FA1E41`，Tests `6583EFA035B80C4BD1024F5862FE8E7C6EE5F8AD312CAEC8962C38A8E574460B`。原首版 `2AC09142…14DF3CE`／`35944690…F6E7EF0E` 是审查时点，不作为修订后资格。工具只读三个固定原始文件、打印 JSON，无文件写入或运行游戏。

## 仍开放

诊断链已实际闭合，但生产资源错误尚未修复，也未确认首个失效资源身份。下一步须分辨挂载时首先发生重绑的资源／脚本边界，不能将“本次同步 Load 区间无 ERROR”理解为 loader 已被排除。原十一项、正式美术／动作／声画、正常双路线、硬件和真人验收继续 active；本轮没有生成新过程图稿或实录。

正常汇报前只运行一次既有媒体 `-Check`，当前 12,807,896 字节、`Notify=false`，未达新增 20 GiB。没有建立自动化或独立提醒，没有因容量阈值停工。
