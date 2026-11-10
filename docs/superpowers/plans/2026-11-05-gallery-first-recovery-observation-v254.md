# Gallery first-recovery observation v254 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 取得Main第一HP2反击窗口的同次全源危险证据，不推测授权反击。
**Architecture:** 真实已完成字符串的24条有界first-prefix；现有决策后仅读标量并单次flush。旧HP1、源扫描和驾驶原样。
**Tech Stack:** 既有C#测试、Godot/.NET headless verifier，无依赖。
**Spec:** docs/superpowers/specs/2026-11-05-gallery-first-recovery-observation-v254-design.md

## Global Constraints

- 唯一G:/knight/Knight；原地脏树，不Git写/删除/改真实存档。
- 不改生产游戏/动力/HP/能量/选角/输入/await/899/预算/旧mask与HP1日志。
- 仅headless，根唯一build/native，作者运行前冻结，无GUI/付费/新媒体/额外dump/自动化。
- 新记录不得额外原生扫描、ObjectFromId或调用有副作用预算函数，不反向影响决定。

## Review Focus

- 第一窗口不得被后面填满后覆盖：超过24条仍保原24顺序。
- 禁用/不eligible/非HP2/空行不得伪造实际观察：真实列表拒绝控制。
- 字符串已完成才能保存：拒绝空/null，输入行/原列表可核；不保native对象。
- 原有HP1日志不得挤掉：分开容量、flush一次，实际两个完整路线核查旧回执仍在。
- 诊断异常不得中断或授权输入：异常隔离、fault计数，沿原决策/await逐字比较。

### Task 1: Bounded evidence and one same-build diagnosis

**Files:** tests/integration/PrologueCanyonGeneratedMainRouteTests.cs；tests/unit/RangedWeaponModelTests.cs。保持1897输入，无新CS/UID。代码worker拥有这两文件；root只拥有doc/运行，二者不并写。
**Interfaces:** `Task10CaptureGalleryHp2Decision(List<string>,bool,bool,int,string) -> bool`；原remote Run调用新增真实列表测试，新receipt独立不改旧固定数。

- [x] 保存before、Spec/Plan阅读、写真实列表测试和stub，交root在ranged-models观察行为RED。
- [x] RED确认后实现纯有界collector和局部JSON/单次flush，原HP1/游戏/决定不改。
- [x] 作者冻结，root跑ranged-models、models、keyboard、actions各一次及当前Main/Explore各一次；资源现有control/pre-prefix各一次。原严格失败保持、不扩caps、不重跑筛绿。
- [x] 若control通过而pre-prefix真实崩溃，批终态后补一个既有basin-resource-claw-prefix作“后两前置”区分；不重跑失败、不改cap，不能从一次quiet推断根因。
- [x] 分析首HP2窗口完整danger与原完整反击窗；记录实值反对/支持假设，不能凭eligible直接改J。
- [x] 原始保护对、当前源/二进制/宿主绑定核对，唯一fresh增量审查；C/I一次TDD修补，Minor延期，裁决完整保留。
- [x] README/原待办/反馈/报告更新，单次媒体Check；全目标继续ACTIVE，不发布/清理。

已执行步骤不表示整套绿色：当前9阶段7PASS/2FAIL，Main899/pre-prefixFATAL仍欠。局部诊断已交接，不重做已完成的观察器；后续用独立修正计划推进真实J/新鲜复查与资源归属。原十一项目标不缩减、不标完成。
