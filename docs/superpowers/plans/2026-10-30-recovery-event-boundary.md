# Recovery Event Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 真实定位并消除首战恢复检查的未解释完成事件失败，不修改恢复语义凑绿。
**Architecture:** 原完整FirstCombat类直接调用；测试局部观察原异步边界。必要修复只基于实际路径，复用现有生产信号和准备上限。
**Tech Stack:** Godot4.7.1.NET/C#、原canonical pwsh7 owner。
**Spec:** `docs/superpowers/specs/2026-10-30-recovery-event-boundary-design.md`

## Global Constraints

- 原完整十一项ACTIVE，唯一KNIGHT G根；不换引擎/缓存/GC策略。
- 保留原55s独立/180s前段/300s后段上限和严格错误门，不用ExpectedFailure/时间扩额。
- 无新窗口/系统输入/付费/自动化/采集/Git/删除/真实存档语义；root唯一编译/原生owner，运行时源/helper冻结。

## Review Focus

1. 请求未被接受却进入盲等：实际请求计数和flow/Pending读数必须区分。
2. 正确恢复仍准备与真实错误混淆：保留完整Failure、流程、实例ID和事件。
3. 诊断回调抛错或产生重入/新await：只读、有界、自己吞记观测错误，不影响生产事件消费。
4. 重复/迟到完成、错误sector/player：原精确两次事件和身份/HP断言仍在。
5. 独立PASS覆盖组合FAIL：直接完整原类与完整pre/post分别记录，旧失败不可擦除。

### Task 1: 原恢复边界与有据修复

Files: `tests/integration/FirstCombatGuidanceRuntimeTests.cs`（局部Snapshot/Settle可选观察回调）；canonical追加实际恢复依赖的选定实体，不新增运行阶段；只有确认生产缺陷才另列最小生产修改。
Interfaces: 消费真实RecoveryRequested/RecoveryCompleted、已有Level/Flow/ResidentCoordinator只读状态；不产生虚假完成或新的产品API。

- [x] 原独立first-combat033749 fcb3d37fc5f84646865db9fa0bb16c84 native0/8808ms PASS；不覆盖v163 preFAIL。
- [x] 原await边界加只读观察，完整pre一次，不改原等待/计数。034324 de7dd77192f244048f0d534257712230 native0/170379ms，20行观察/0错误；旧失败原因仍未知，不算修复。
- [x] v165实际失败快照支持后，真实未接受请求控制RED→完成/拒绝条件GREEN；仅修消费者，不猜改生产。
- [x] 最终first-combat/models/pre/post实际执行：3PASS/1FAIL，pre更早Supply资源终结器FATAL；较早pre超时保留，未全绿。
- [x] 唯一C0/I1/M0审查，I1失败边界快照实际RED→GREEN，无二审；59清单对/实体/bin、README/backlog/手动容量检查记录于v166。

实现与验证步骤已执行，但套件仍红，不记Task完整成功/可合并或整关完成。新私有generation全覆盖、生产恢复整体、自然路线和正式视听仍未获证明，资源根因保持开放。

沿用用户持续执行授权，root原地inline及只读代理，不重复问执行方式；不提交/创建worktree/删除workspace。原全目标验收尚未成立。
