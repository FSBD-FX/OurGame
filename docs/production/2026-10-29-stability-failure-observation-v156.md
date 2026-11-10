# v156 · 先保留初始错误，再清理

这是稳定性诊断工具增量，不是新的美术、玩法或性能版本。工程仍为 `G:/knight/Knight`；完整原十一项目标继续 ACTIVE，未缩小为“让测试通过”。

## 已验证的改动

原 Stable runner 捕获异常后，先等待帧和执行现有终结器清理，最后才打印原异常；清理先 FATAL 时，初始失败可能被遮住。现将**同一次完整 GD.PushError 移到 catch 第一条语句**，早于 trace、await 和清理；原 cleanup、清理异常报告、GC 次数和 `Quit(1)` 保留，没有重复输出原异常或变更生产释放策略。

真实 sentinel 使用同一个 runner/catch：原顺序被实际日志合同拒绝，修改后确认初始错误在 cleanup ENTER 之前。原生进程仍 exit1，不能作为关卡成功；只有这个明确负控允许一个确切 sentinel，三日志中其他 ERROR/WARNING/FATAL、普通成功标记、超时、缺失/重复/错序记录均拒绝。

解析器另有98个字面正反例，直接执行真实函数，不 grep 源码或伪造原生通过。真实 Godot banner 的空行曾暴露解析器误拒绝：先新增正例观察 RED，再将错误行匹配限定为水平空白修正；原失败记录保留。最终纯测试命令 exit0，返回 `cases=98 parser_only=true native=false`；工具返回的合并文本已保存于 `.superpowers/sdd/2026-10-29-stability-failure-observation/pure-test-command-output.txt`，不是额外独立执行者或原生引擎成功证明。

## 耗时观察仍未获得运行覆盖

在原 pre 最后一次 traversal 调用前后，以及每个 fresh sample 进入、原 finally 清理完成之后，加入独立 OBS 行，记录原 room/segment/fraction、引擎毫秒和 physics/process 计数。不改旧收据、输入、调用顺序、等待或预算。

但本轮 pre 在更早的 Descent 测试失败，post 不经过该 legacy fresh-sample 路径：实际 OBS 数均为0。这里只能确认源码与编译，**不能**称已定位原超时、量出分段耗时或排除晚段停顿。

## 七次原生尝试与最终资格

证据根：`G:/knight/验证证据/october13-stable-claw`。七次构建均 exit0，原 focus55秒、pre180秒、post300秒上限保留，无 FATAL/泄漏豁免、补跑筛绿或证据覆盖。

| 实际 case | 原生结果 | 合同结果 |
| --- | --- | --- |
| failure-log-boundary-20261029-180756-040-69f5dd1c59784504965882adf41c4360 | exit1 / 664ms；原异常在清理之后 | 预期顺序 RED，保留 FAIL |
| failure-log-boundary-20261029-180955-599-f246a122472443fca994916485d72ceb | exit1 / 562ms；新顺序正确，但 banner 空行被解析器误拒绝 | FAIL，不重写 provenance |
| failure-log-boundary-20261029-181529-264-23edc739885543a196646b1e034fab17 | exit1 / 855ms；初始异常→ENTER→原 finalizer→exit | 严格**负控合同 PASS**，非游戏 PASS |
| recycling-traps-20261029-181646-998-05962f8c2f7c49deaef288c627ec2ef8 | exit0 / 41874ms；实际第二 fixture 的预期字形断言、绑定还原和原退出清理返回 | 普通 PASS |
| models-20261029-181833-555-a30394ae56b745369ff9878fa529219c | exit0 / 792ms | 普通 PASS |
| prologue-pre-20261029-182022-572-eb9f7ee5af75448a83bbe0366c862f16 | exit1 / 126899ms，无超时；`real second J is active` 断言失败 | 普通 FAIL |
| prologue-post-20261029-182337-081-6182ab3fbeb049eab6b09d2f8ece1c5b | exit0 / 218216ms；原75 executable＋owner8、四条确切负控 WARNING | 普通 PASS |

最终普通四阶段 **3 PASS / 1 FAIL**，另有一个真实负控合同 PASS。不能把它们合成“游戏全绿”。这次 recycling 通过，也不能证明 v155 的 gchandle FATAL、ShapeRID 或其他历史资源故障已修复；post 时间不同不构成性能改进或硬件验收。

## 下一处实际失败：第二次 J

本轮 pre 的早期错误 JSON 完整记录了 `DescentClawGuidanceRuntimeTests.Boundaries:329`。该文件未改，SHA256 为 `CF34C05688A81FC4088DC7653E3A0F645DA9BFD4C89D1F493B2D0DF9088D6E99`。

只读核对确认：

- 首次 J 的 `attacks==1` 已验证确实接受。
- 后续 W 仍按住，机械爪附着/取消及边界恢复已经运行；没有随后 Q/E/槽位切换，L 防御测试尚未执行。
- 第二次 J 是按下→`Frames(input,3)`→释放→`Settle`→检查 `IsAttacking`。
- `Frames` 数的是输入序列推进，不是完成的 Combat 处理或实际经过的 physics ticks；`Settle` 又等两个 physics 信号与三个 process 信号。
- 保存 Player 没有覆盖攻击时长，默认 `.06＋.10＋.14＝.30秒`；`IsAttacking` 是整个攻击生命周期，不仅是命中有效帧。

因此，已接受后结束/重置，与未启动/输入边沿被抑制/被权限拒绝，目前都没有被区分。仅凭检查时 J 已释放不能说“输入丢失”；也不能仅凭等待方式就宣布是测试时序问题。

下一步在**原第二次 J 命令与原断言**之间记录实际 AttackStarted 次数与时间、输入序列和 physics/process 时钟，以及断言处攻击、权限、拒绝原因、能量、模式/槽位、盾、爪、flow、HP 状态。保留原输入、等待、预算和断言，不额外补按 J、加等待或强制攻击。独立 `descent-guidance` 入口调用原委托，但不等于完整 pre 前缀，不保证能复现同一失败。

## 审查、取舍与留档

唯一 fresh 审查 `stability_failure_final_review_v156`：C0/I0/M0，认可本次限定诊断代码，不等于普通回归全绿或正式交付。未新增 Minor，也不关闭既有延期项。

本轮执行判断及代价：

1. 延续已批准的失败关闭目标，直接补可观测性，不重复进行引擎/美术决策；代价是本轮不增加可见玩法或修好历史原生故障。
2. 使用指定 dirty checkout，不提交、迁移或删除；一次新审查只覆盖本轮，不能替无关历史变更背书。
3. ENTER 放在整个原清理链的第一次 await 之前，保证检查的是“初始异常先于清理”，不只是在最终 GC 之前；增加一条测试日志，不增加等待或 GC。
4. 在独立 OBS 源码加入后，用同版最终批次验证真实负控，避免无变化的重复构建；此前 parser 误拒绝记录仍 FAIL，直到新鲜 canonical 通过才认定合同通过。
5. 不把第二次 J 失败归因于玩家或输入，也不自动修复判定；代价是仍需实际因果观察，pre 保持 FAIL。

审查未判断项的处置：OBS 运行覆盖和超时定位继续 OPEN；第二次 J 根因继续未知；历史 FATAL/ShapeRID 仍 OPEN；清理自身抛错/原生崩溃的动态表现未由正常 sentinel 清理证明；98例在审查后由 root 再次执行并保存工具返回文本，但没有第二独立执行者背书；完整十一项/Main/Explore/像素/正常速度 AV/硬件/真人验收均保留；无关 dirty 变更不由本轮审查验收。以上边界不从总目标删除。

35组 case 内前后 source/helpers/binaries/user/continuation 清单一致；最终1857源/897 C#与 v155 比较仅三份既有测试 CS 变化，生产源码/资产/地图未变。当前三份源与六个二进制均匹配最终清单，七个本轮原生 PID 已消失。详情保存在本轮 workspace 的 `custody-result.json`。

- Source manifest：`181BC789A2B5C8B658EF81CBF8677FFC0ABFF472F3C10AE7E9DDAA65FBDB5EA7`
- Binary manifest：`BA5F03BAF101C2AFC66B092987C373B5E8C3125CF2879DDA0B479BB6B3E308F0`
- Canonical：`21DEAE1CDB93E14FA278973E634365CD59B20D2BB7BB5D8D86276F27A74193D6`

HEAD/index 保持原值。无前台/系统键鼠、付费、新美术或媒体、再次 ProcDump、自动化、Git 发布或删除；真实存档未语义读取/改写。媒体手动 Check 为1,431,609 bytes / 16文件 / Notify=false，未到新增20GiB。全十一项、完整双路、正式音画动作 UI、硬件和用户游玩验收继续 ACTIVE/OPEN。
