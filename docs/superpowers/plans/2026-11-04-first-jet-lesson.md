# 首次短喷教学 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成 G04 的真实首次短喷机会与成功接线，而非只改说明文字。

**Architecture:** 生产 GuidanceController 持有当前 Player/Motor 标量身份与 journal 游标；GuidanceModel 在既有优先级内选择无目标固定卡。Motor 仍是短喷唯一权威，不添加运动回调或物理调参。

**Tech Stack:** 现有 Godot/.NET/C#，现有 stable-claw 无窗口原生 owner。

**Spec:** `docs/superpowers/specs/2026-11-04-first-jet-lesson-design.md`

## Global Constraints

- 工程仅 `G:/knight/Knight`，原十一项范围不缩减。
- 不改 Completion 枚举/持久化、动力/伤害/人数/地图/单跳或原验收预算。
- 只有内部 Godot 键盘事件和隔离存储；无 GUI/系统输入/付费/自动化/发布/删除。
- 不在原生进程存活期间编辑其源或 harness；根是唯一原生执行者。

## Review Focus

- 暂停/死亡/重置间残留 Started 不得补学；已学会不被普通恢复抹去。
- 新局和替换 owner 必须基线化，journal 断档不伪造成功。
- 近战/组合机关/激光不能被可选短喷提示盖住。
- 双语和实时改键应呈现完整短句且零世界框；不更改主线目标。
- 拒绝/旧按键不是 accepted；实际 remapped key 接到真实 Motor 后才结束卡。

### Task 1: 生产首次教学与生命周期

**Files:** 修改 `scripts/prologue/guidance/PrologueGuidanceContext.cs`、`PrologueGuidanceModel.cs`、`PrologueGuidanceController.cs`、`assets/localization/prologue-ui.json`；测试为 `tests/unit/PrologueGuidanceModelTests.cs`、`tests/integration/KeyboardTeachingRuntimeTests.cs`。

**Interfaces:** context 增加默认 false 的 `ThrusterOpportunity`；controller 从现有 `ReadThrusterTransitionsAfter(ulong)` 消费当前有效 Started，传给模型，不公开测试写状态入口。

- [x] 先扩展既有真实 entry fixture：在原 BridgeFarBank 支撑布置人物后，自动提示应有 ECHO 推进器标题；用现有 API 断言，避免以编译错误代替行为 RED。
- [x] 根运行 `verify-claw-knight-v001.ps1 -Phase thruster-teaching-entry -SourceRows 1888`；实际在缺失首次 context 的行为断言失败。
- [x] 实装数据字段、生产只读学习状态、优先级与中英短句；不改 Motor。
- [x] 增加双语/custom glyph/危险与完成关闭模型控制；真实 fixture 覆盖暂停、改键、拒绝、实际成功及新局。
- [x] 执行 `thruster-teaching`、`thruster-teaching-ui`、`thruster-player` 和完整 `prologue-pre`/`prologue-post`；两条默认诊断路线均实际尝试。最终7PASS/2FAIL，不把原超时或Main余敌改签通过。
- [x] 一次独立代码审查；根随后从更宽运行发现Important引路冲突，实际RED→一次修补，完整当前轮保留全部失败，不为筛绿放宽门。
- [x] 更新 README、G04待办和实际证据报告，不勾整项或原十一项完成。

任务状态：本轮实施/验证步骤已执行，技术行为局部已证；当前完整资格未全绿，不能将以上步骤勾选解释为G04、原十一项、正常速度视听或正式交付完成。未覆盖边界及所有裁决见执行账与v238报告。

## 执行裁决

- 在现有用户指定的唯一 KNIGHT 工程和 `codex/` 分支原地工作；不建立新的 worktree/工程副本，不处理无关脏改动。
- 根实现生产并独占原生测试；子代理只写明确拥有的 runtime 测试文件或只读审查。保留本计划及证据，不自动提交/发布或删除工作区。
