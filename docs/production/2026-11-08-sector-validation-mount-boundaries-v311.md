# v311 · 区域验证与正式挂载分开观测

## 事实：本轮做了什么

继续 KNIGHT 原完整十一项目标，工程仍为 `G:/knight/Knight`。本轮推进 Q05 的资源故障诊断，不是新地图、美术或完整交付。仅无窗口构建与测试，没有游戏窗口、系统输入、付费生成、新原生崩溃采集、自动化、Git 操作、删除或真实玩家存档写入。

v310 发现外层 AddChild 内还有 StreamModel 的离树验证。为区分这些路径，本轮在两个现有生产 C# 文件的原同步调用旁添加可选标量观测，默认未订阅时直接返回；在原 Supply 测试内临时订阅，退出后恢复。没有添加或删除 Instantiate、Validate、Free、AddChild、等待、缓存、GC 或资源保活操作；不读取新的资源身份，不留 Node／Resource 引用，不改变原异常处理。

四个编译输入发生变化：

- `scripts/prologue/seamless/PrologueSectorDefinition.cs`：可选 observer、sink 故障计数，以及原验证 Instantiate／Free 的进入和成功返回标记。
- `scripts/prologue/seamless/SeamlessPrologueWorld.cs`：原 Definitions 枚举、StreamModel 构造、staged Instantiate／AddChild 的标记。
- `tests/integration/BasinSupplyRuntimeTests.cs`：保存／恢复订阅及标量栈，保留原四用例、五个根、旧 30 资源边界和 40 挂载观察。
- `tests/unit/PrologueSectorStreamModelTests.cs`：真实 dispatcher 的空订阅、标量顺序及抛错 sink 隔离测试。

新日志 `BASIN_SECTOR_OPERATION` 只含序号、操作号、scope、kind、进入／返回、区域编号、累计创建根数及引擎时钟。累计根数不是当前存活数量。同步操作抛出不补写 return，未闭合记录不能冒认通过。日志仍会扰动时序，不是资源寿命修复。

## 原生 RED 与最终同版回归

使用原规范 runner `verify-claw-knight-v001.ps1 -Phase <phase> -SourceRows 1901`，未使用 ExpectedFailure、身份构建或放宽错误检查。

| 案例 | 真实结果 | 证据目录后缀 |
|---|---|---|
| 先只加测试的 prologue-post RED | 编译成功；原生 exit 1，121448 ms；`godot.external.log:314` 精确出现 `SECTOR_OPERATION_DIAGNOSTIC_RED` 缺 API 断言 | `prologue-post-20261108-064333-484-5f8d066050124230af685811b50e96d6` |
| 最终普通 prologue-pre | 严格 PASS，44 项，exit 0，150369 ms | `prologue-pre-20261108-064806-340-60aca9ed11b141b1af0c721bc48706ba` |
| 最终普通 prologue-post | 严格 PASS，原 75 项及新增 dispatcher 测试，exit 0，194825 ms | `prologue-post-20261108-065158-357-0f77f4aabc21490bb27a4e49ebfdd1bf` |

以上目录均在 `G:/knight/验证证据/october13-stable-claw/`。RED 的宿主最终报 scope receipts 不完整，原日志中的首个断言才是本次预期失败原因；不把宿主末句误当游戏根因。没有删除失败案或用命名 RED 豁免它。

最终前、后段各 provenance 的 failure 为 null、guardFailures 为空；后段只有原合同 4 条负例警告：seamless 1、room-profile 1、flow-registry 2。当前后段沿用既有 300000 ms 上限，194825 ms 不满足历史 180000 ms，`original180Accepted=false`、`performanceCertification=false`。本轮未改时间上限，不签性能。

最终两案 1901 输入清单逐字相同，SHA256：

`A2EA547D6BB37B1137EB1A00F6C1C23E7971FC9A32A59F7D6146019E8DF2397E`

六二进制清单也相同，SHA256：

`213E91369F7DEAB18277E8F5C7EBC80BAC4691101EAD3F9C90275DB044A7E27F`

根最终实际逐项读回 1901 输入、10 个既有 helper、6 个当前二进制，漂移为空。与 v309 相比只有上述四个编译输入不同，不能混用 v309 产物替代本轮验证。

## 新诊断日志与只读解析器

本次原四 Supply 用例留下 180 行／90 对操作／5 个根，open、omitted、faults、observer_faults 均 0。进入次数为 Definitions 枚举 5、StreamModel 5、validation Instantiate 25／Free 25、staged Instantiate 15／AddChild 15。旧 30／40 行观察及原用例顺序仍在。

新增工具位于 `.superpowers/sdd/2026-11-08-sector-operation-boundary/`：

- `Read-SectorOperationBoundary.ps1`：只读既有 case 的 provenance、stdout、external、stderr，按真实嵌套和同 root-cycle 区间归属原错误，不能跨 scope、根或畸形边界拼接。缺标记、缺返回、乱序、重复号、END 矛盾、fault 等均拒绝完整性。staged Instantiate 后必须由同区域 AddChild 一一消费，不硬编码总 staged 数。
- `Test-SectorOperationBoundary.ps1`：最终 87 个纯文本断言。合成数据只作 validation-only，不是原生案例或游戏通过证据。

初始自测缺实现、FATAL 漏判和 JSON Int64 元数据反例的 RED／GREEN 由 worker 实际执行；根独立执行最终自测。独立审查还实际证实一次 Important：原解析器允许 Instantiate 区 2、AddChild 区 3。原 worker 唯一修补轮先以该反例得到 `expected=False actual=True`，再补 pending 消费检查及孤立／重复／未消费／覆盖和多次合法配对；根复读修补并再次执行最终 87 断言通过。没有为修 PS 重跑原生或另开审查席。

根对最终工具实际 CLI 读回：

- 当前 pre：exit 0，traceComplete=true、rawCasePassed=true，180 行／90 对／5 根，原错误 0、stderr 非空行 0。
- 旧 v309 pre：exit 1，traceComplete=false、rawCasePassed=false；新标记 0，保留 11 条 external 及对应 11 条 stderr 错误、451 条非空 stderr。它们是同案不同日志来源，不伪称 22 次独立故障；11 条仍只归外层 mount-unscoped，不能倒推旧案的具体新阶段。

工具退出 0 **仅表示 trace 完整**；rawCasePassed 另查原错误及原 provenance。wholeGoalAccepted、causeProved 恒为 false。原 v309 两工具 SHA 均未变化。

新工具最终 SHA256：

- Read：`028E1DCA14CD80458DCE7219A03F235D5A9D8B00D1C3141CBB5A7BF68912AE6E`
- Test：`BAF948823D19DA6497CF92AF3A24E4F5DB8AA17BF6477219991B7897670B3341`

## 推理、风险与仍未完成

当前同版前后段通过，说明本次观测代码未在这两轮暴露测试回归；**没有复现旧句柄错误，所以不能据此指认 validation 或 staged，也不能说资源根因已修**。不重复原生筛无错结果，不用日志数量或 marker END 代替玩法／稳定性验收。

唯一独立审查初始 C0／I1／M1，I1 已按上文唯一 TDD 轮处置并由根验证，不冒称重新获得 C0／I0／M0。Minor 保留：dispatcher 单测不直接覆盖真实 scene 操作抛出、cleanup 或跨 await 恢复；对应同步标记位置已静态核对，最终普通回归通过也不等于这些负例均已执行。

订阅为 ThreadStatic，依赖当前串行、回 Godot 主线程的测试；共享测试计数不支持并发 Run。递归 observer、跨线程恢复及同线程无关操作插入不在安全保证内；当前 sink 不主动调用场景操作。原生进程崩溃不能保证 finally／END，缺 END 仍是无效证据。未据此修改线程、引擎、缓存或 GC 策略。

下一步在后续实际验证中沿用这套边界；若故障重现，先依据完整配对原日志定位具体调用，再决定应用／桥接路径。正式美术分层、正常速度动作观感、当前正常双路线／硬件性能／视听和真人验收等原十一项仍开放。A 只是已批准外观方向；没有接入未批准稿件或新敌人造型，不对本轮签完整序章／可发布版本。

本轮没有新过程图稿或视频；正常汇报前仅执行一次现有媒体 `-Check`：12,807,896 字节、Notify=false，未达新增 20 GiB。没有新建自动化、独立提醒或停止工作。
