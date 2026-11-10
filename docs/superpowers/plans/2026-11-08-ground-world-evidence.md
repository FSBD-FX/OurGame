# Ground world evidence implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用正式Main/Explore真实实例与同帧绘制闭合v320正式world取证，不缩减原十一项。

**Architecture:** 被动test-only观察器接在Start后，测量核字面量控制先RED→GREEN，原finally统一退出。保持所有游戏与原路线逻辑不变。

**Tech Stack:** 现Godot4.7.1/.NET9/C#规范headless runner。

**Spec:** `docs/superpowers/specs/2026-11-08-ground-world-evidence-design.md`。

## Global Constraints

只G:/knight/Knight，无窗口/OS输入/付费/真档/dump/Git/删除/自动化/引擎cache/GC/预算更改。root唯一build/native/canonical owner；worker只写新partial，其他test文件由root接线。保留唯一脏树及原始失败，不用Git型SDD清理/提交脚本。

## Review Focus

- FramePublished吞观察器异常不能产生假通过；零样本失败。
- CurrentSector commit与实际contact可差一physics，不能伪造同步或放宽至静态列表。
- 真正的Gameplay碰撞与同frame实际脚底，而非同名外观或Gait字段自证。
- 回调只读、只在目标窗口索取旧sector；半程卸载及finally不污染原异常。
- 原路线/危险/HP/键盘与600s/pre/post门完全不变，不能拿局部替整关。

### Task 1: world被动证据与当前完整合版

**Files:** create `tests/integration/PrologueCanyonGeneratedMainRouteTests.GroundSeam.cs`；modify旧`PrologueCanyonGeneratedMainRouteTests.RunTask10`只声明/Start后赋值/正常return前AssertComplete/finally Dispose；canonical仅必要phase源码快照附新partial。已有`PlayerFormalVisualTests.GroundSeam.cs`追加same-sector仅属v320剩余验证，不改原门。

**Interfaces:** nested `private sealed class Task10GroundSeamEvidence : IDisposable`，ctor `(PrologueLevelController, SeamlessPrologueWorld, CharacterBody2D, string variant)`；`AssertComplete()`/`Dispose()`；private pure `MeasurePair`产生可比较stance脚数与最大world delta。callback缓存Exception，最终再抛。无新增生产入口。

- [x] test-only新partial建立字面量pair controls及被动native采集；MeasurePair暂返回default，root实际Main diagnostic/1911输入在CONTROL_RED fail（不是编译/游戏故障），原件保留。611f684...／engine1／3306ms，build0／guards[]，正例应2/0实际default；真实Title/Start已完成、尚未订阅。
- [x] root实现最小pair测量；同版Main98924b7...／248223ms与Explore e04f253...／313826ms从原Title流程到Boss/支撑出口通过，非空真实shape/stance/world交接有原始收据。它们不抵消恢复专项失败或代签正式观感。
- [ ] 同版ground（含same-sector）／models／rig／恢复／普通pre44/post75／保存暖冷严格通过；前后/跨案/current source-bin-helper-user无漂移。最终11指定phase10PASS/1恢复FAIL；21f895...的3Handle错误不以engine0或其他成功案冲销，本step/Task1保留OPEN。55对/66sealed/16helper/current1911与Debug6审计无漂移。
- [x] 一次仅本新增验证器增量独立review，不重审v320生产；C0/I0/M1，无修补轮/二审，Minor留账；README/稳定待办/两plan ledger已更新，媒体一次Check15,203,531bytes/145files/Notify=false。文档与审查完成不代表失败的Step3或原十一项完成。

## Rulings

用户已选择持续自主，仅关键缺信息才问；此是已约定实测方法，不新增玩法或风格决策。root继续原位执行，独立worker只写新增验证文件。预期1911（新增一CS）；若实际枚举不同，以严格清单查明原因，不凑数。v320 review已结束，不二审旧生产；本计划只有新增验证器的一次独立审查，避免用旧review给新test代签。代价是程序取证不能代签正常观感，失败时先定位测量/真实游戏/缺样区别。
