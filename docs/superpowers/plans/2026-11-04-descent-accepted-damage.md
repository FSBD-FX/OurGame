# 下降教学已接受伤害来源 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 归因v239真实Descent HP5→4，不改变原玩法或验收门。
**Architecture:** 仅测试文件内从同一Health.Damaged同步读取其已接受身份队尾，保存有限标量；保留原完整动作、等待和HP断言。来源为Motor时才识别落伤，不用当场尚未更新的LastFallDamageApplied推断。
**Tech Stack:** 现有Godot/.NET、C#、原生PowerShell owner。
**Spec:** `docs/production/2026-11-04-camera-passive-timing-v239.md` 的实际失败与下一入口，原十一项Q01/Q05及M04，不替代正式动作/惯性质量。

## Global Constraints

- 唯一G:/knight/Knight；仅无窗口/隔离存储/内部原键盘输入，根独占native，运行时不改源或harness。
- 不改Health/Motor/敌人/机关、难度、原HP5/恢复0、等待/输入/时限，不增加全局journal或付费/GUI/自动化/发布/删除。
- 保持1888源输入；在既有测试类内实现；事件finally解绑，未知来源/观察失败明确记录，不能冒认伤害原因。
- 现有codex脏分支原地推进，不复制worktree/提交或删证据。用户持续推进授权优先于重复计划确认。

## Review Focus

- 队首/旧攻击不能冒充本次来源，跨64条容量后仍读实际队尾。
- 无已接受身份及ResetFull后拒绝读取；重复/零伤害不产生新Damage事件；治愈不冒充伤害。
- 回调捕获的是实际扣血后的Health，不能把盾拒绝队列当接受队列。
- 观察器异常不改变原伤害/动作流程；所有订阅在失败与成功finally均解除。
- 场景/来源节点只读、局部使用，不跨await保留额外native包装；输出只作诊断，不能代签自然路线/真人。

### Task 1: 真实来源观察与归因

**Files:** `tests/integration/DescentClawGuidanceRuntimeTests.cs`；原owner不改。
**Interfaces:** 既有`RunObservationReadControl()`增加真实Health控制；私有`ReadAcceptedDamageIdentity(HealthComponent)`返回`DamageAttackIdentity`；`Run`局部订阅使用同一reader。

- [x] 先写反射发现reader及真实65次接受/两拒绝/治愈/reset控制，原reader不存在；原`descent-j-observation-control -SourceRows 1888`须build通过、行为RED。
- [x] 实现只读队尾reader，原控制及新控制GREEN；不变更生产Health的私有API。
- [x] 同Player从启动至finally记录stage、physics/process/input、HP、来源二元组、位置/速度、盾、爪、landingcount；按Motor ID识别落伤，记录合法源路径或明确未知。
- [x] 跑原focused Descent，必要时原完整pre上下文取证；源不明不猜修、不改门。保留所有失败。
- [x] 一次新范围独立审查；Important实际RED→GREEN一次修补，无二审。原pre/post及爪models子集完整尝试，报告全部终态及原未完成范围。
- [x] 更新执行账/README/原待办和实际报告，不据本观察勾原十一项或动作验收。

步骤执行不等于Task1完整通过：最终四门3PASS/1pre原180秒超时，旧HP4未重现/未归因，整套仍红，不签complete/发布。见v240报告。
