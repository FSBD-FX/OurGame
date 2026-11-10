# Task10首次提示就绪边界 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 用真实同步输入/事件证明首次提示缺失的原因，再修复正确的边界，保留原生产提示覆盖。
**Architecture:** 测试局部有限观察，优先复用实际Playing和输入采样边界；未证明前不修改生产消费者。原两个Task10方法仍从真实组件/输入产生提示。
**Tech Stack:** 当前Godot/.NET、C#、既有native PowerShell owner。
**Spec:** ../specs/2026-11-04-task10-cue-readiness-design.md

## Global Constraints

唯一根G:/knight/Knight；原1888输入与时限/HP/敌人/存档保护不变。仅无窗口内部输入，原失败保留；无GUI/付费/系统输入/dump/自动化/发布/删除。所有native运行期间冻结源和harness。

## Review Focus

- 真的Playing但无新输入样本：不得把读到的旧边沿当首次操作。
- 启动结束重启Motor：固定夹具必须发生于真实完成之后，不通过禁用其他消费者造假。
- 重绑或相同拒绝重入：不得种假基线、补播或降低精确次数。
- 生产publisher仍用legacy兼容路线：不得丢16种cue或混淆纯键盘正式玩法。
- 原生失败/资源与宽门：不能用小专项代签完整同版套件或用户观感。

### Task 1: 证明首次输入并迁移真实就绪前提

**Files:** tests/integration/PrologueAudioDirectorTests.cs；若证据需要其他文件，先在账中说明。
**Interfaces:** 原Task10ActionCueBaselineAndReentryAreExact/Task10ProductionPublishersReachDirectors签名与runner不改；消费既有WaitForPlaying及PlayerInputReader.CurrentFrameSequence，局部事件finally解绑。

- [x] 原断言不变，只加入有限同步状态/事件，根运行原thruster-recovery-compatibility取得因果RED。
- [x] 根据实际断点最小修复；若Intro前提成立，等待真实Playing和新样本后再固定Motor，保持原精确断言。必要的第二方法前提迁移分别据实记录。
- [x] 原专项GREEN后，一次独立范围审查；本次C0/I0/M0，无修补/延期Minor，无二审。
- [x] 当前同版跑原pre/post、资源/存档/恢复/模型与Main/Explore，读终态和保护绑定，不筛失败。
- [x] 更新README/原稳定待办/反馈与实际报告；完成度仍按全部十一项，保持ACTIVE。

步骤执行完毕，不等于Task1整体验收或完整目标通过：当前十门7PASS/3FAIL（geometry原生FATAL、未改顶部恢复反馈Intro前提、Main899）。保留全部失败，原十一项与R-I/R-C/R-P不缩减，不签complete。
