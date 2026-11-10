# Ground seam continuity implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复真实共面地图换支撑时的脚步突跳，保留原十一项。

**Architecture:** 先原生真实Player／保存碰撞／引擎键盘跨缝反例；再由Coordinator提供已核实的纯值支撑连续性，Gait仅在安全共面交接中转换坐标基并保留摆脚。没有新物理或素材。

**Tech Stack:** 现Godot4.7.1／.NET9／C#，规范headless runner。

**Spec:** `docs/superpowers/specs/2026-11-08-ground-seam-design.md`。

## Global Constraints

- 唯一G:/knight/Knight，仍仅无窗口，无付费／OS键鼠／真档／dump／Git／删除／自动化／引擎cache/GC/预算变化。
- root独占production／canonical／build/native；test-only worker只拥有新增partial，不能改原hook。
- 原唯一脏树保留，不运行会commit／清理的SDD脚本；case快照与本ledger保存前后范围。

## Review Focus

- 不同碰撞身份但同几何的连续步态不能重置；actual Draw而非只字段。
- 旧源消失／disabled／queued／不在树／几何变化不能继续被踩。
- 真缺口／台阶／坡面／动态支撑不被错误拼成连续地板。
- 镜像／零时间／相位回绕不能制造假结论，原固定腿长及已有收缩负控保留。
- 自然输入与同帧观察、隔离生命周期／借用资源不被测试装置破坏。

### Task 1: 共面交接实际反例与最小修复

**Files:** create `tests/integration/PlayerFormalVisualTests.GroundSeam.cs`；原`PlayerFormalVisualTests.RunGroundGaitOnly`增加一次调用；canonical仅原`thruster-ground-gait`/pre/post源快照增加该partial及必要现有源。生产候选仅`PlayerGroundGait.cs`与`PlayerPresentationCoordinator.cs`，待实际RED明确接口再在此计划补记签名。

**Interfaces:** tests `public static Task RunGroundSeamNative(SceneTree tree)`；消费保存Player、保存sector矩形／父offset、KeyboardRouteInputDriver、Coordinator.FramePublished／正式LegSoleLocal。旧`PlayerGroundSample`／`PlayerGroundGait.Step`与真实slide查询保持生产边界。

实际RED后的生产接口：同文件新增纯值`PlayerGroundSupportObservation(bool Observed, ulong SupportId, Transform2D SupportTransform, Rect2 SupportBounds)`，`PlayerGroundSample.PreviousSupport`；Coordinator私有`RevalidateGroundCarry`每新physics按ID重新核实原生静态矩形／单shape／层／tree／queued／geometry与bounds。Gait私有`CanContinueAcrossSeam`／`CoplanarConnected`仅平移单位基、顶同.001／连接.001且当前脚／摆脚端点被两面覆盖时rebase坐标，不改相位／进度／world脚。保留一块纯值旧支撑直至所有脚与有效摆脚端点在新面，期间每拍新proof匹配，丢失或geometry改变按原安全reset。记录不持有native对象／Resource；真实平台与坡面仍走原分支。

- [x] 新partial先实现真实保存碰撞跨缝观测：两不同owner、真实键盘位移／接地、匹配发布与Draw，跨缝已有stance sole不能仅因owner换而瞬移或相位归零；生产不改。
- [x] root规范`-Phase thruster-ground-gait -SourceRows 1910`保留原完整phase；实际功能RED／失败原件留存。1d52eaa...／engine1／18954ms，near stance4.000122px、far24.173668px，fixtureguards先过，不是编译或前条件错。
- [x] 依据实际输入链补记最小生产接口，再实现经过原生live/geometry验证的连续支撑交接；同phaseGREEN，保留既有bounds缩小／无支撑／真实爪／空中动作／固定骨长所有旧检查。原test adffafa...GREEN；冻结扩展ee36b59...1910／28496ms完整原ground phase亦GREEN。
- [ ] 补左右、真实旧支撑撤权、缺口／台阶／移动／bounds负控及零时间边界，实际原生通过；同版models／rig／恢复／普通pre/post／保存冷读回归。v320九phase严格PASS；v321同sector保存几何cfb3d64...／29887ms（standing jump0）与正式world Main/Explore两路线亦已通过，但当前完整11指定phase为10PASS/1恢复资源FAIL，不可用旧版恢复PASS代签。详见`2026-11-08-ground-world-evidence.md`，本step与Task1仍不写complete，不静默删掉原要求。phase-wrap／swing专门回归另为M1延期。
- [x] 一次独立增量审查；C/I仅一轮实际RED→GREEN修补，Minor留账不二审；README／稳定待办／production与媒体一次Check更新，原十一项不勾完成。C0/I0/M1，无修补轮或二审；媒体15,203,531bytes/145files/Notify=false。

## Execution rulings

用户已选择持续自主，仅关键缺信息提问；此属已要求的连续动作修补，用现有Godot与程序骨架，不重新征求已经明确的技术路线。root原位执行生产；独立test-only写新文件不等于把整个task交另一执行器。选择保留原ground-gait聚合phase而非新建更小phase：旧本轮前基线约20s，55s原门容纳短跨缝，若超时按真实失败处理，不扩预算或去掉旧用例。实测结果若表明应改方案，先记因果／接口ruling，不盲改测试期待。
