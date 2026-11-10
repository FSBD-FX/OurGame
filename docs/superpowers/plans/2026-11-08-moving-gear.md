# Moving Gear Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将原T03移动齿轮做成真实碰撞、可读、可通行的连续关卡机关，保留原完整十一项。

**Architecture:** 纯值周期模型给出实际线性路径区间，派生CanyonFixedGear的Area在同一处理步骤查询精确胶囊并复用一次伤害消费。保留当前Fog发现和原生遮挡门，未来占用单独登记；隔离candidate通过后再重建正式leaf/parent。

**Tech Stack:** 当前Godot 4.7.1 .NET/C#、既有专用SDK及规范PowerShell runner；不换工具链。

**Spec:** docs/superpowers/specs/2026-11-08-moving-gear-design.md

## Global Constraints

- G:/knight/Knight唯一工程；root唯一build/native owner；只无窗口，无OS输入／真实存档／付费生成／新dump／Git／删除／新自动化。
- 固定齿轮默认overlap语义不变；不改原158solid、敌人/碎片/门、玩家数值、引擎缓存/GC或限时。
- Runtime设置：travel长度(0,512]、period[1,20]s、峰速≤600；本次候选r24、travel(0,80)、3.2s、damage2、cadence.5s。
- 原始失败保留；新phase严格查external/stdout/stderr，不命名豁免；当前编译输入基线1901，新增C#后按真实清单计数。
- 仅源节点创建的query/shape可释放；不Dispose借用碰撞shape；不把candidate/fixture/headless几何当正式声画/真人。

## Review Focus

- 一个delta跨完整周期、起止同点：仍应检测实际整条已走路径，Task1模型与native中点body反例。
- 玩家只落在胶囊AABB空角：不得受伤，Task1真实native负例。
- 同步Died/reset/queued或回调再入：旧段不能继续伤害，Task1epoch/单次消费和重入控制。
- 原生Area transform未跟随／父缩放／disabled：不准显示健康危险圆或错误伤害，Task1几何与Fog反控。
- 候选轨迹跨sector局部负y：不得丢相机／角色／占用；Task2保存controller与流送、Task3双路验证。

---

### Task 1: 真实往返运行内核与受控证据

**Files:** Create `scripts/mechanisms/MovingGearMotionModel.cs`, `scripts/mechanisms/CanyonMovingGear.cs`, `tests/unit/MovingGearMotionModelTests.cs`, `tests/integration/CanyonMechanismRuntimeTests.MovingGear.cs`. Modify `CanyonFixedGear.cs`, `ResidentEnvironmentInventory.cs`, `tests/RunThrusterCoreTests.cs`, canonical `verify-claw-knight-v001.ps1`（仅新增严格phase/input/receipts）。

**Interfaces:** `MovingGearMotionModel(Vector2,float)`, `.Step(double):MovingGearMotionFrame`, `.Reset()`, frame `.Offset/.Velocity/.SweepFromOffset/.SweepToOffset/.Phase01`; `CanyonMovingGear : CanyonFixedGear` exports `TravelOffset/PeriodSeconds`, public `CurrentMotion/HasValidMotionGeometry/MotionWorldBounds`; base protected `ProcessGearContacts(IEnumerable<Node2D>,double)` 与virtual danger/reset。

- [x] 写reflection test-only模型与runtime入口；缺类时断言 `MOVING_GEAR_MODEL_RED`。新增strict `thruster-moving-gear`，55s原core预算、不混flags；先按1903真实输入跑RED，再写生产代码。
- [x] 模型literal：travel(120,0),period1 → quarter offset60、half120、full0且full包络0..120；跨turnaround包络到B；负向同逻辑；30/60/120 partition、非法构造/时间、reset不残段。按spec实现，再原生跑同phase看到模型GREEN后再走runtime。
- [x] runtime fixture新类：保存Player，首尾不碰而delta整周期穿中点，应有真实GuardedDamage；空角不伤害；无重复、原iframe、reentrant、reset不扫旧段、暂停/隐藏/disabled/staged/queued和原static gear控制。危险必须使用同一real circle半径，不改HP凑过。
- [x] Fog canonical fixture使用同层子型，验证原墙/范围/清空；inventory返回全轨迹。只用owned query资源，退出及非法Ready清理。
- [x] 以真实输入数运行 `-Phase thruster-moving-gear` 及原 `-Phase recycling-traps`，完整pre/post；新关键markers恰一次、原始错误0。记录source/bin/helper哈希。原生门未满足不勾本任务。

### Task 2: 保存地图及controller接入

**Files:** `tools/build/prologue/CanyonContentBuilder.cs`（或同职责partial新文件）、Basin leaf、PrologueLevel父；Task1 integration partial加stage/placement/controller检查；当前map exporter注释/索引随真实点位更新。

**Interfaces:** 消费Task1的runtime、MotionWorldBounds；新增稳定节点名 `UpperGalleryMovingGear` 于Basin Mechanisms，无新增solid或目标。

- [x] 用现builder产出隔离Basin与父候选，断言只有一个新Area/r24/Travel(0,80)/3.2s/damage2，原surface/敌人域/9碎片/门保留；当前无该节点先实际RED。
- [x] 检查全相位包络、真实controller发现新子类、运动后旧位置清空、墙及pause/unload；局部负y仍有正确camera/streaming。
- [x] 自然玩家完整支持两安全站位，等上升后通过并返回，错误时间受伤，不加隐式二段跳、位置写入/救援/无敌。
- [x] 全部通过后才以builder同步更新正式leaf与parent，保留候选失败；重生成当前地图并保全来源。未入图阶段只标候选。

### Task 3: 合并资格及交付边界

**Files:** 原正常route必要的仅输入择时适配、README/总待办/production证据；不修改底层通关权限或旧挑战预算。

- [x] 原主线/自然碎片上下路在新障碍下真实按键择时通过，保原HP/零非预期恢复/原辅助权限，旧测试若不识别新危险须根据真实预警调整输入，不改HP/门/地图凑绿。
- [x] 当前同源完整Main/Explore从标题至Boss及实体出口；pre/post、存档兼容等按风险复验，不拼旧版二进制。v317最新7BD49／FAC8的Main／Explore／前44／后75／actions／typed recovery，加v316同版save冷读／models／新旧机关全部严格通过；仅程序资格，不关闭资源历史根因或正常视听／真人。
- [x] 一次独立增量审查；C/I仅一轮TDD修补，Minor明记，不新增重复审查席。
- [x] 更新README与稳定待办：已做／未入图／已原生／未正常视听/真人分别列出；无正式发布或Git操作。Goal保持未完成，原十一项不缩水。

## Execution rulings

用户已选择持续自主推进且仅关键缺信息提问。root在现唯一脏工作树内串行实现、构建、运行，子代理只读核查；不用会改Git或清理工作区的SDD脚本，不创建worktree/commit/删除scratch。以本计划独立ledger和规范case原始记录代替Git范围，不声称提交完成。
